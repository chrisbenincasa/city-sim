using System.Collections.Generic;
using System.Linq;
using Borough.Appearance;
using Borough.Core.Entities;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws a Building whose Appearance Family has a body as that body, generated at the Building's
/// own size, in place of its massing. <c>ui family-bodies on|off</c>.
/// </summary>
/// <remarks>
/// A Building that draws as several massing wings keeps its massing.
/// </remarks>
public partial class Main
{
    private static readonly string[] BodyParts =
    [
        "wall-end", "wall", "trim", "roof-new", "roof", "membrane", "glass", "door", "frame", "metal",
        "plinth", "storefront", "sign", "awning", "solar",
    ];

    private readonly Dictionary<ulong, Node3D> _familyBodyNodes = [];
    private readonly Dictionary<ulong, BodyNeighbours> _bodyNeighbours = [];
    private readonly Dictionary<(string Family, int Frontage, int Depth, int Storeys, AttachedSides Attached), ArrayMesh> _familyBodyMeshes = [];
    private readonly Dictionary<string, Dictionary<string, Material>> _bodyLibraries = [];
    private ShaderMaterial? _derelictBody;
    private bool _familyBodies;
    private bool _reportFamilyBodies;

    /// <summary>How far an abandoned body's own materials give way to <see cref="Derelict"/>.</summary>
    private const float DerelictBodyShare = 0.75f;

    /// <summary>Where a body probed for side neighbours, and the Buildings it found there; 0 is none.</summary>
    private readonly record struct BodyNeighbours(int Slot, Vector3 LeftProbe, Vector3 RightProbe, ulong LeftId, ulong RightId);

    private void FamilyBodyStudy(string[] words)
    {
        if (words.Length != 2 || words[1] is not ("on" or "off"))
        {
            _refused = "family-bodies on|off";
            return;
        }

        _familyBodies = words[1] == "on";
        _reportFamilyBodies = _familyBodies;
        _world.Changes!.Invalidate();
    }

    private void PlaceFamilyBodies()
    {
        foreach (Node3D node in _familyBodyNodes.Values) node.QueueFree();
        _familyBodyNodes.Clear();
        _bodyNeighbours.Clear();
        for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
        {
            if (_world.Buildings.Rows.IsLive(slot)) PlaceFamilyBody(slot);
        }

        _reportFamilyBodies = false;
    }

    private void RemoveFamilyBody(ulong id)
    {
        if (_familyBodyNodes.Remove(id, out Node3D? node)) node.QueueFree();
        _bodyNeighbours.Remove(id);
    }

    /// <returns><c>true</c> where the Building now draws as its family's body.</returns>
    private bool PlaceFamilyBody(int slot)
    {
        if (!_familyBodies) return false;
        if (FamilyOf(slot).Family is not { Body: { } body } family) return false;

        using IEnumerator<Massing> parts = Buildings(slot).GetEnumerator();
        if (!parts.MoveNext()) return false;
        Massing one = parts.Current;
        if (parts.MoveNext() || _exactBodies.ContainsKey(one.Id)) return false;

        Vector3 size = one.Body.Basis.Scale;
        float faceEast = (one.Reads.R * 2f) - 1f;
        float faceSouth = (one.Reads.G * 2f) - 1f;
        bool facesNorthSouth = Mathf.Abs(faceSouth) > 0.5f;
        float frontage = facesNorthSouth ? size.X : size.Z;
        float depth = facesNorthSouth ? size.Z : size.X;
        int storeys = Mathf.Max(1, Mathf.RoundToInt(size.Y / StoreyMetres));
        float turn = Mathf.Atan2(faceEast, faceSouth);
        AttachedSides attached = Attached(slot, one.Body.Origin, turn, frontage, out BodyNeighbours neighbours);

        var node = new MeshInstance3D
        {
            Mesh = BodyMesh(family.Id, body, frontage, depth, storeys, attached),
            Position = one.Body.Origin with { Y = 0f },
            Rotation = new Vector3(0f, turn, 0f),
        };
        Dress(node, one);
        AddChild(node);
        _familyBodyNodes[one.Id] = node;
        _bodyNeighbours[one.Id] = neighbours;
        if (_reportFamilyBodies)
        {
            GD.Print($"family_body\t{family.Id}\tbuilding {one.Id}\t{frontage}x{depth} m\t{storeys} storeys\tattached {attached}"
                + $"\ttile {Mathf.RoundToInt(one.Body.Origin.X / MetresPerTile)} {Mathf.RoundToInt(-one.Body.Origin.Z / MetresPerTile)}"
                + (one.Abandoned ? "\tabandoned" : ""));
        }

        return true;
    }

    /// <summary>
    /// Covers a body in its Building's overlay colour, mutes it under a ground overlay, or greys it
    /// when abandoned, as the massing is drawn.
    /// </summary>
    private void Dress(MeshInstance3D node, Massing one)
    {
        if (_washing != Wash.None)
        {
            node.MaterialOverride = BuildingWash ? _categorical : _muted;
            if (BuildingWash) node.SetInstanceShaderParameter("body_ink", one.Paint.LinearToSrgb() with { A = 1f });
            return;
        }

        if (!one.Abandoned) return;
        _derelictBody ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://derelict-body.gdshader") };
        node.MaterialOverlay = _derelictBody;
        node.SetInstanceShaderParameter("derelict", Derelict with { A = DerelictBodyShare });
    }

    /// <summary>
    /// The side walls another Building's footprint touches, found half a metre outside the middle of
    /// each wall. Left and right are as seen from the street, the body's +Z.
    /// </summary>
    private AttachedSides Attached(int slot, Vector3 centre, float turn, float frontage, out BodyNeighbours neighbours)
    {
        var right = new Vector3(Mathf.Cos(turn), 0f, -Mathf.Sin(turn));
        float reach = (frontage / 2f) + .5f;
        bool deepEast = Mathf.Abs(right.Z) > .5f;
        Vector3 leftProbe = centre - (right * reach), rightProbe = centre + (right * reach);
        int left = Covering(slot, leftProbe), rightSide = Covering(slot, rightProbe);
        AttachedSides attached = AttachedSides.None;
        if (left >= 0) attached |= AttachedSides.Left | (Crosswise(slot, left, deepEast) ? AttachedSides.LeftCrosswise : 0);
        if (rightSide >= 0) attached |= AttachedSides.Right | (Crosswise(slot, rightSide, deepEast) ? AttachedSides.RightCrosswise : 0);
        neighbours = new BodyNeighbours(slot, leftProbe, rightProbe, IdAt(left), IdAt(rightSide));
        return attached;
    }

    /// <summary>
    /// Re-places each body whose side wall touched a changed Building, or now falls inside a placed
    /// one. A removed Building's Lot may already be freed, so the body's remembered neighbour ids
    /// find the walls it leaves bare.
    /// </summary>
    private void RefreshNeighbourBodies(HashSet<ulong> changed, List<int> placed)
    {
        var stale = new List<(ulong Id, int Slot)>();
        foreach ((ulong id, BodyNeighbours seen) in _bodyNeighbours)
        {
            if (changed.Contains(id)) continue;
            if (changed.Contains(seen.LeftId) || changed.Contains(seen.RightId)
                || placed.Exists(slot => Covers(slot, seen.LeftProbe) || Covers(slot, seen.RightProbe)))
            {
                stale.Add((id, seen.Slot));
            }
        }

        foreach ((ulong id, int slot) in stale)
        {
            RemoveFamilyBody(id);
            PlaceFamilyBody(slot);
        }
    }

    private ulong IdAt(int slot) => slot >= 0 ? _world.Buildings.Rows.IdAt(slot) : 0;

    /// <summary>Whether the neighbour's footprint spans a different stretch of this Building's depth.</summary>
    private bool Crosswise(int slot, int neighbour, bool deepEast)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[slot], out int own)
            || !lots.Rows.TryResolve(_world.Buildings.Lot[neighbour], out int other)) return false;
        return deepEast
            ? lots.FootprintEast[own] != lots.FootprintEast[other] || lots.FootprintWide[own] != lots.FootprintWide[other]
            : lots.FootprintNorth[own] != lots.FootprintNorth[other] || lots.FootprintDeep[own] != lots.FootprintDeep[other];
    }

    /// <returns>The Building slot whose footprint covers the point, or -1.</returns>
    private int Covering(int slot, Vector3 point)
    {
        BuildingTable table = _world.Buildings;
        for (int other = 0; other < table.Rows.SlotCount; other++)
        {
            if (other != slot && table.Rows.IsLive(other) && Covers(other, point)) return other;
        }

        return -1;
    }

    private bool Covers(int building, Vector3 point)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[building], out int lot)) return false;
        float east = point.X / MetresPerTile, north = -point.Z / MetresPerTile;
        int x = lots.FootprintEast[lot].Raw, y = lots.FootprintNorth[lot].Raw;
        return east >= x && east < x + lots.FootprintWide[lot].Raw && north >= y && north < y + lots.FootprintDeep[lot].Raw;
    }

    private ArrayMesh BodyMesh(string family, FamilyBody body, float frontage, float depth, int storeys, AttachedSides attached)
    {
        var key = (family, Mathf.RoundToInt(frontage * 100f), Mathf.RoundToInt(depth * 100f), storeys, attached);
        if (_familyBodyMeshes.TryGetValue(key, out ArrayMesh? cached)) return cached;

        Dictionary<string, Material> library = BodyLibrary(body.Library);
        var mesh = new ArrayMesh();
        foreach ((string part, ShellMesh source) in FamilyBodyBuilder.Build(body, frontage, depth, storeys, attached).Parts)
        {
            var tool = new SurfaceTool();
            tool.Begin(Mesh.PrimitiveType.Triangles);
            for (int i = 0; i < source.VertexCount; i++)
            {
                System.Numerics.Vector3 n = source.Normals[i];
                System.Numerics.Vector2 uv = source.Uvs[i];
                System.Numerics.Vector3 p = source.Positions[i];
                tool.SetNormal(new Vector3(n.X, n.Y, n.Z));
                tool.SetUV(new Vector2(uv.X, uv.Y));
                tool.AddVertex(new Vector3(p.X, p.Y, p.Z));
            }

            foreach (int index in source.Indices) tool.AddIndex(index);
            tool.GenerateTangents();
            tool.SetMaterial(library.GetValueOrDefault(part) ?? new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.8f, 0.78f) });
            tool.Commit(mesh);
        }

        _familyBodyMeshes[key] = mesh;
        return mesh;
    }

    /// <summary>The materials of an authored model, by the part each one dresses.</summary>
    private Dictionary<string, Material> BodyLibrary(string library)
    {
        if (_bodyLibraries.TryGetValue(library, out Dictionary<string, Material>? found)) return found;

        var materials = new Dictionary<string, Material>();
        Node scene = GD.Load<PackedScene>($"res://assets/{library}.glb").Instantiate();
        foreach (MeshInstance3D instance in scene.FindChildren("*", nameof(MeshInstance3D), true, false).OfType<MeshInstance3D>().Prepend(scene as MeshInstance3D).OfType<MeshInstance3D>())
        {
            for (int surface = 0; surface < instance.Mesh.GetSurfaceCount(); surface++)
            {
                Material material = instance.Mesh.SurfaceGetMaterial(surface);
                string? part = BodyParts.FirstOrDefault(p => material.ResourceName == p || material.ResourceName.StartsWith(p + "-", System.StringComparison.Ordinal));
                if (part is not null) materials.TryAdd(part, material);
            }
        }

        scene.Free();
        _bodyLibraries[library] = materials;
        return materials;
    }
}
