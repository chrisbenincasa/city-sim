using System.Collections.Generic;
using System.Linq;
using Borough.Appearance;
using Borough.Core.Entities;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws a Building whose Appearance Family has a body as that body, generated at the Building's
/// own size, in place of its massing. <c>ui family-bodies on|off [NEAR]</c>.
/// </summary>
/// <remarks>
/// Bodies sharing a mesh share an <see cref="InstanceLayer"/>, batched by chunk. A chunk within the
/// near band draws bodies; beyond it, the same Buildings draw as massing boxes in their family's
/// colours and roof. Body and box layers decide by the same chunk key, so a Building is drawn once.
/// A Building that draws as several massing wings keeps its massing.
/// </remarks>
public partial class Main
{
    private static readonly string[] BodyParts =
    [
        "wall-end", "wall", "trim", "roof-new", "roof", "membrane", "glass", "door", "frame", "metal",
        "plinth", "storefront", "sign", "awning", "solar",
    ];

    private static readonly string[] RoofParts = ["roof-new", "roof", "membrane"];

    private readonly Dictionary<ulong, PlacedBody> _placedBodies = [];
    private readonly Dictionary<ulong, BodyNeighbours> _bodyNeighbours = [];
    private readonly Dictionary<(string Family, int Frontage, int Depth, int Storeys, AttachedSides Attached, int Paint), BodyShape> _familyBodyMeshes = [];
    private readonly Dictionary<(ArrayMesh Mesh, bool Abandoned), InstanceLayer> _bodyLayers = [];
    private readonly Dictionary<string, Dictionary<string, Material>> _bodyLibraries = [];
    private readonly Dictionary<string, Material> _libraryMaterials = [];
    private readonly Dictionary<Material, (Material Painted, float Mean)> _paintedMaterials = [];
    private readonly Dictionary<Material, Color> _meanColours = [];
    private readonly HashSet<Vector2I> _nearChunks = [];
    private readonly List<ulong> _bodyLayerIds = [];
    private Dictionary<(int East, int North), int>? _footprintTiles;
    private ShaderMaterial? _derelictBody;
    private bool _familyBodies;
    private bool _reportFamilyBodies;
    private float _bodyNearMetres = BodyNearMetres;

    /// <summary>How far an abandoned body's own materials give way to <see cref="Derelict"/>.</summary>
    private const float DerelictBodyShare = 0.75f;

    /// <summary>PROVISIONAL reach of the body band, from the camera to the nearest point of a chunk.</summary>
    private const float BodyNearMetres = 500f;

    /// <summary>Where a body probed for side neighbours, and the Buildings it found there; 0 is none.</summary>
    private readonly record struct BodyNeighbours(int Slot, Vector3 LeftProbe, Vector3 RightProbe, ulong LeftId, ulong RightId);

    /// <summary>A generated body, and the linear mean colours its far box is painted with.</summary>
    private sealed record BodyShape(ArrayMesh Mesh, Color Wall, Color Roof, FamilyBody Body);

    private readonly record struct PlacedBody(InstanceLayer Layer, BodyShape Shape);

    /// <summary><c>ui family-bodies on|off [NEAR]</c>; NEAR is the body band's reach in metres.</summary>
    private void FamilyBodyStudy(string[] words)
    {
        float near = BodyNearMetres;
        if (words.Length is < 2 or > 3 || words[1] is not ("on" or "off")
            || (words.Length == 3 && (!float.TryParse(words[2], System.Globalization.CultureInfo.InvariantCulture, out near) || near < 0f)))
        {
            _refused = "family-bodies on|off [NEAR]";
            return;
        }

        _familyBodies = words[1] == "on";
        _bodyNearMetres = near;
        _reportFamilyBodies = _familyBodies;
        _nearChunks.Clear();
        foreach (InstanceLayer layer in new[] { _buildings, _roofs, _hips, _pairedRoofs, _parapets })
        {
            layer.Multimesh.Near = _familyBodies ? NearChunk : null;
            layer.Multimesh.Repartition();
        }

        foreach (InstanceLayer layer in _bodyLayers.Values) layer.Multimesh.Repartition();

        _world.Changes!.Invalidate();
    }

    private void PlaceFamilyBodies()
    {
        foreach ((ulong id, PlacedBody placed) in _placedBodies) placed.Layer.Multimesh.Replace(id, []);
        _placedBodies.Clear();
        _bodyNeighbours.Clear();
        foreach (((ArrayMesh _, bool abandoned), InstanceLayer layer) in _bodyLayers)
        {
            if (abandoned) layer.MaterialOverlay = _washing == Wash.None ? DerelictBody() : null;
        }

        if (!_familyBodies) return;

        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        _footprintTiles = FootprintTiles();
        for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
        {
            if (_world.Buildings.Rows.IsLive(slot)) PlaceFamilyBody(slot);
        }

        _footprintTiles = null;
        GD.Print($"family_bodies\t{_placedBodies.Count}\t{_familyBodyMeshes.Count} meshes\t{_bodyLayers.Count} layers"
            + $"\t{System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds:F1} ms");
        _reportFamilyBodies = false;
    }

    private void RemoveFamilyBody(ulong id)
    {
        if (_placedBodies.Remove(id, out PlacedBody placed)) placed.Layer.Multimesh.Replace(id, []);
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
        int paint = FamilyPicker.Paint(family, _world.Key, one.Id);

        BodyShape shape = BodyMesh(family, body, frontage, depth, storeys, attached, paint);
        InstanceLayer layer = BodyLayer(shape.Mesh, one.Abandoned);
        var at = new Transform3D(new Basis(Vector3.Up, turn), one.Body.Origin with { Y = 0f });
        layer.Multimesh.Replace(one.Id, [new InstanceValue(at, Colors.White, Tint(one))]);
        _placedBodies[one.Id] = new PlacedBody(layer, shape);
        _bodyNeighbours[one.Id] = neighbours;
        if (_reportFamilyBodies)
        {
            GD.Print($"family_body\t{family.Id}\tbuilding {one.Id}\t{frontage}x{depth} m\t{storeys} storeys\tattached {attached}\tpaint {paint}"
                + $"\ttile {Mathf.RoundToInt(one.Body.Origin.X / MetresPerTile)} {Mathf.RoundToInt(-one.Body.Origin.Z / MetresPerTile)}"
                + (one.Abandoned ? "\tabandoned" : ""));
        }

        return true;
    }

    /// <summary>
    /// A body's instance custom data: its Building's overlay colour under a Building wash, or the
    /// share of <see cref="Derelict"/> an abandoned body's overlay greys it by.
    /// </summary>
    /// <remarks>
    /// It is custom data because a painted material takes its albedo from COLOR, which a MultiMesh
    /// multiplies by the instance colour.
    /// </remarks>
    private Color Tint(Massing one)
    {
        if (_washing != Wash.None) return BuildingWash ? one.Paint with { A = 1f } : Colors.White;
        return one.Abandoned ? Derelict.SrgbToLinear() with { A = DerelictBodyShare } : Colors.White;
    }

    /// <summary>
    /// The colour a bodied Building's far box is drawn in: the family's own material under no wash,
    /// greyed as its body is when abandoned.
    /// </summary>
    private Color FarPaint(Massing one, Color family, Color massing)
    {
        if (_washing != Wash.None) return massing;
        return one.Abandoned ? family.Lerp(Derelict.SrgbToLinear(), DerelictBodyShare) : family;
    }


    /// <summary>
    /// A bodied Building's far box, roofed as its body is: a pitched family keeps a pitched cap
    /// and a flat family wears its parapet.
    /// </summary>
    private static Massing FarMassing(Massing one, FamilyBody body)
    {
        Vector3 plan = one.Body.Basis.Scale;
        Vector3 at = one.Body.Origin;
        if (body.GableDegrees <= 0f)
        {
            if (body.ParapetMetres <= 0f) return one with { Cap = Cap.Flat };
            var tray = Basis.FromScale(new Vector3(plan.X + (CopingMetres * 2f), body.ParapetMetres, plan.Z + (CopingMetres * 2f)));
            return one with { Cap = Cap.Parapet, Roof = new Transform3D(tray, at with { Y = plan.Y + (body.ParapetMetres * 0.5f) }) };
        }

        if (one.Cap is Cap.Gable or Cap.Hip or Cap.PairedGable) return one;

        float span = Mathf.Min(plan.X, plan.Z), ridge = Mathf.Max(plan.X, plan.Z);
        float rise = RoofHeight(Cap.Gable, Mathf.Tan(Mathf.DegToRad(body.GableDegrees)) * span * 0.5f);
        Basis capped = CapBasis(Cap.Gable, plan.X > plan.Z, span, ridge, rise);
        return one with { Cap = Cap.Gable, Roof = new Transform3D(capped, at with { Y = plan.Y + (rise * 0.5f) }) };
    }

    private InstanceLayer BodyLayer(ArrayMesh mesh, bool abandoned)
    {
        if (_bodyLayers.TryGetValue((mesh, abandoned), out InstanceLayer? found)) return found;

        var layer = new InstanceLayer();
        layer.Multimesh.Mesh = mesh;
        layer.Multimesh.UseCustomData = true;
        layer.Multimesh.Near = NearChunk;
        layer.Multimesh.NearOnly = true;
        layer.InstanceParameters["body_ink"] = Colors.White;
        layer.MaterialOverride = _washing == Wash.None ? null : BuildingWash ? _categorical : _muted;
        if (abandoned && _washing == Wash.None) layer.MaterialOverlay = DerelictBody();
        AddChild(layer);
        _bodyLayers[(mesh, abandoned)] = layer;
        return layer;
    }

    private ShaderMaterial DerelictBody() =>
        _derelictBody ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://derelict-body.gdshader") };

    /// <summary>Whether a chunk lies within the body band of the camera, with 15% hysteresis.</summary>
    private bool NearChunk(Vector2I key)
    {
        float size = InstanceLayer.ChunkMetres;
        Vector3 eye = _camera.GlobalPosition;
        var nearest = new Vector3(
            Mathf.Clamp(eye.X, key.X * size, (key.X + 1) * size), 0f, Mathf.Clamp(eye.Z, key.Y * size, (key.Y + 1) * size));
        float reach = _bodyNearMetres * (_nearChunks.Contains(key) ? 1.15f : 1f);
        bool near = eye.DistanceSquaredTo(nearest) <= reach * reach;
        if (near) _nearChunks.Add(key);
        else _nearChunks.Remove(key);
        return near;
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
        if (_footprintTiles is not null)
        {
            var tile = (Mathf.FloorToInt(point.X / MetresPerTile), Mathf.FloorToInt(-point.Z / MetresPerTile));
            return _footprintTiles.TryGetValue(tile, out int found) && found != slot ? found : -1;
        }

        BuildingTable table = _world.Buildings;
        for (int other = 0; other < table.Rows.SlotCount; other++)
        {
            if (other != slot && table.Rows.IsLive(other) && Covers(other, point)) return other;
        }

        return -1;
    }

    /// <summary>The live Building slot on each footprint Tile, for a whole-city placement pass.</summary>
    private Dictionary<(int East, int North), int> FootprintTiles()
    {
        var tiles = new Dictionary<(int East, int North), int>();
        LotTable lots = _world.Lots;
        BuildingTable table = _world.Buildings;
        for (int slot = 0; slot < table.Rows.SlotCount; slot++)
        {
            if (!table.Rows.IsLive(slot) || !lots.Rows.TryResolve(table.Lot[slot], out int lot)) continue;
            int x = lots.FootprintEast[lot].Raw, y = lots.FootprintNorth[lot].Raw;
            for (int east = x; east < x + lots.FootprintWide[lot].Raw; east++)
            {
                for (int north = y; north < y + lots.FootprintDeep[lot].Raw; north++) tiles.TryAdd((east, north), slot);
            }
        }

        return tiles;
    }

    private bool Covers(int building, Vector3 point)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[building], out int lot)) return false;
        float east = point.X / MetresPerTile, north = -point.Z / MetresPerTile;
        int x = lots.FootprintEast[lot].Raw, y = lots.FootprintNorth[lot].Raw;
        return east >= x && east < x + lots.FootprintWide[lot].Raw && north >= y && north < y + lots.FootprintDeep[lot].Raw;
    }

    private BodyShape BodyMesh(AppearanceFamily family, FamilyBody body, float frontage, float depth, int storeys, AttachedSides attached, int paint)
    {
        var key = (family.Id, Mathf.RoundToInt(frontage * 100f), Mathf.RoundToInt(depth * 100f), storeys, attached, paint);
        if (_familyBodyMeshes.TryGetValue(key, out BodyShape? cached)) return cached;

        Dictionary<string, Material> library = BodyLibrary(body.Library);
        var mesh = new ArrayMesh();
        IReadOnlyDictionary<string, Paint>? scheme = paint >= 0 ? family.Paints![paint].Parts : null;
        var reads = new Dictionary<string, Color>();
        foreach ((string part, ShellMesh source) in FamilyBodyBuilder.Build(body, frontage, depth, storeys, attached).Parts)
        {
            bool dressed = body.Materials.TryGetValue(part, out string? texture);
            Material material = dressed ? LibraryMaterial(texture!)
                : library.GetValueOrDefault(part) ?? new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.8f, 0.78f) };
            bool paintable = !dressed || _textureLibrary!.Paintable.Contains(texture!);
            Color? tint = null;
            reads[part] = MeanColour(material);
            if (paintable && scheme is not null && scheme.TryGetValue(part, out Paint colour) && material is BaseMaterial3D authored)
            {
                (material, tint) = Painted(authored, colour, family.Id, part);
                reads[part] = Color.Color8(colour.R, colour.G, colour.B).SrgbToLinear();
            }

            var tool = new SurfaceTool();
            tool.Begin(Mesh.PrimitiveType.Triangles);
            for (int i = 0; i < source.VertexCount; i++)
            {
                if (tint is { } t) tool.SetColor(t);
                System.Numerics.Vector3 n = source.Normals[i];
                System.Numerics.Vector2 uv = source.Uvs[i];
                System.Numerics.Vector3 p = source.Positions[i];
                tool.SetNormal(new Vector3(n.X, n.Y, n.Z));
                tool.SetUV(new Vector2(uv.X, uv.Y));
                tool.AddVertex(new Vector3(p.X, p.Y, p.Z));
            }

            foreach (int index in source.Indices) tool.AddIndex(index);
            tool.GenerateTangents();
            tool.SetMaterial(material);
            tool.Commit(mesh);
        }

        Color fallback = MeanColour(null);
        Color wall = reads.GetValueOrDefault("wall", fallback);
        Color roof = RoofParts.Where(reads.ContainsKey).Select(part => reads[part]).DefaultIfEmpty(fallback).First();
        var shape = new BodyShape(mesh, wall, roof, body);
        _familyBodyMeshes[key] = shape;
        return shape;
    }

    /// <summary>A material's mean albedo in linear light, its texture sampled on a 32 × 32 grid.</summary>
    private Color MeanColour(Material? material)
    {
        if (material is not BaseMaterial3D surface) return new Color(0.6f, 0.6f, 0.58f);
        if (_meanColours.TryGetValue(material, out Color found)) return found;

        Color tint = surface.AlbedoColor.SrgbToLinear();
        Color mean = Colors.White;
        if (surface.AlbedoTexture?.GetImage() is { } image)
        {
            if (image.IsCompressed()) image.Decompress();
            const int Grid = 32;
            float r = 0f, g = 0f, b = 0f;
            for (int y = 0; y < Grid; y++)
            {
                for (int x = 0; x < Grid; x++)
                {
                    Color texel = image.GetPixel(((2 * x) + 1) * image.GetWidth() / (2 * Grid), ((2 * y) + 1) * image.GetHeight() / (2 * Grid)).SrgbToLinear();
                    r += texel.R; g += texel.G; b += texel.B;
                }
            }

            mean = new Color(r / (Grid * Grid), g / (Grid * Grid), b / (Grid * Grid));
        }

        Color colour = new(tint.R * mean.R, tint.G * mean.G, tint.B * mean.B);
        _meanColours[material] = colour;
        return colour;
    }

    /// <summary>
    /// A copy of an authored material that takes its colour from the vertices, and the vertex colour
    /// that paints it. A photograph's mean linear luminance divides out, so the paint reads true.
    /// </summary>
    /// <remarks>Vertex colours stop at 1, so a paint brighter than its photograph's mean is clamped.</remarks>
    private (Material Painted, Color Tint) Painted(BaseMaterial3D authored, Paint paint, string family, string part)
    {
        if (!_paintedMaterials.TryGetValue(authored, out (Material Painted, float Mean) found))
        {
            var copy = (BaseMaterial3D)authored.Duplicate();
            copy.AlbedoColor = Colors.White;
            copy.VertexColorUseAsAlbedo = true;
            found = (copy, MeanLuminance(authored.AlbedoTexture));
            _paintedMaterials[authored] = found;
        }

        Color linear = Color.Color8(paint.R, paint.G, paint.B).SrgbToLinear();
        var tint = new Color(linear.R / found.Mean, linear.G / found.Mean, linear.B / found.Mean);
        if (tint.R > 1f || tint.G > 1f || tint.B > 1f)
        {
            GD.PushWarning($"family '{family}' paints {part} brighter than its photograph allows; the paint is clamped.");
            tint = new Color(Mathf.Min(tint.R, 1f), Mathf.Min(tint.G, 1f), Mathf.Min(tint.B, 1f));
        }

        return (found.Painted, tint);
    }

    /// <returns>The mean linear luminance of an albedo photograph, or 1 where there is none.</returns>
    private static float MeanLuminance(Texture2D? albedo)
    {
        if (albedo?.GetImage() is not { } image) return 1f;
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgb8);
        float sum = 0f;
        int count = 0;
        for (int y = 0; y < image.GetHeight(); y += 3)
        {
            for (int x = 0; x < image.GetWidth(); x += 3, count++)
            {
                Color c = image.GetPixel(x, y).SrgbToLinear();
                sum += (0.2126f * c.R) + (0.7152f * c.G) + (0.0722f * c.B);
            }
        }

        return sum / count;
    }

    /// <summary>A texture library entry's albedo, normal and roughness maps as one material.</summary>
    private Material LibraryMaterial(string texture)
    {
        if (_libraryMaterials.TryGetValue(texture, out Material? found)) return found;

        string stem = TextureLibraryDirectory + texture;
        var material = new StandardMaterial3D
        {
            ResourceName = texture,
            AlbedoTexture = GD.Load<Texture2D>(stem + "-albedo.jpg"),
            NormalEnabled = true,
            NormalTexture = GD.Load<Texture2D>(stem + "-normal.jpg"),
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };
        if (ResourceLoader.Exists(stem + "-roughness.jpg"))
        {
            material.Roughness = 1f;
            material.RoughnessTexture = GD.Load<Texture2D>(stem + "-roughness.jpg");
        }

        _libraryMaterials[texture] = material;
        return material;
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
