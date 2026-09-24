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
/// A body is drawn only with no overlay showing, since its materials carry no wash. A Building
/// that draws as several massing wings keeps its massing.
/// </remarks>
public partial class Main
{
    private static readonly string[] BodyParts =
    [
        "wall-end", "wall", "trim", "roof-new", "roof", "membrane", "glass", "door", "frame", "metal",
        "plinth", "storefront", "sign", "awning", "solar",
    ];

    private readonly Dictionary<ulong, Node3D> _familyBodyNodes = [];
    private readonly Dictionary<(string Family, int Frontage, int Depth, int Storeys, AttachedSides Attached), ArrayMesh> _familyBodyMeshes = [];
    private readonly Dictionary<string, Dictionary<string, Material>> _bodyLibraries = [];
    private bool _familyBodies;
    private bool _reportFamilyBodies;

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
        for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
        {
            if (_world.Buildings.Rows.IsLive(slot)) PlaceFamilyBody(slot);
        }

        _reportFamilyBodies = false;
    }

    private void RemoveFamilyBody(ulong id)
    {
        if (_familyBodyNodes.Remove(id, out Node3D? node)) node.QueueFree();
    }

    /// <returns><c>true</c> where the Building now draws as its family's body.</returns>
    private bool PlaceFamilyBody(int slot)
    {
        if (!_familyBodies || _washing != Wash.None) return false;
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
        AttachedSides attached = Attached(slot, one.Body.Origin, turn, frontage);

        var node = new MeshInstance3D
        {
            Mesh = BodyMesh(family.Id, body, frontage, depth, storeys, attached),
            Position = one.Body.Origin with { Y = 0f },
            Rotation = new Vector3(0f, turn, 0f),
        };
        AddChild(node);
        _familyBodyNodes[one.Id] = node;
        if (_reportFamilyBodies)
        {
            GD.Print($"family_body\t{family.Id}\tbuilding {one.Id}\t{frontage}x{depth} m\t{storeys} storeys\tattached {attached}"
                + $"\ttile {Mathf.RoundToInt(one.Body.Origin.X / MetresPerTile)} {Mathf.RoundToInt(-one.Body.Origin.Z / MetresPerTile)}");
        }

        return true;
    }

    /// <summary>
    /// The side walls another Building's footprint touches, found half a metre outside the middle of
    /// each wall. Left and right are as seen from the street, the body's +Z.
    /// </summary>
    /// <remarks>
    /// A body reads its neighbours only when it is placed, so a neighbour raised later leaves its
    /// shared wall windowed until the next full pass.
    /// </remarks>
    private AttachedSides Attached(int slot, Vector3 centre, float turn, float frontage)
    {
        var right = new Vector3(Mathf.Cos(turn), 0f, -Mathf.Sin(turn));
        float reach = (frontage / 2f) + .5f;
        AttachedSides attached = AttachedSides.None;
        if (Covered(slot, centre - (right * reach))) attached |= AttachedSides.Left;
        if (Covered(slot, centre + (right * reach))) attached |= AttachedSides.Right;
        return attached;
    }

    private bool Covered(int slot, Vector3 point)
    {
        float east = point.X / MetresPerTile, north = -point.Z / MetresPerTile;
        BuildingTable table = _world.Buildings;
        LotTable lots = _world.Lots;
        for (int other = 0; other < table.Rows.SlotCount; other++)
        {
            if (other == slot || !table.Rows.IsLive(other) || !lots.Rows.TryResolve(table.Lot[other], out int lot)) continue;
            int x = lots.FootprintEast[lot].Raw, y = lots.FootprintNorth[lot].Raw;
            if (east >= x && east < x + lots.FootprintWide[lot].Raw && north >= y && north < y + lots.FootprintDeep[lot].Raw) return true;
        }

        return false;
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
