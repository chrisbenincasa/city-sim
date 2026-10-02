using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Appearance;
using Borough.Core.Entities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws a Building whose Appearance Family has a body as that body, generated at the Building's
/// own size, in place of its massing. <c>ui family-bodies on|off [NEAR]</c>.
/// </summary>
/// <remarks>
/// Bodies sharing a mesh share an <see cref="InstanceLayer"/>, batched by chunk. A chunk within the
/// near band draws near bodies. Mid-rise and tower families draw simplified bodies beyond it, with
/// a per-Building cross-fade at the edge; low-rise families retain their chunk-switched massing boxes.
/// A Tower's podium and shaft are one body over the whole site; any other Building that draws as
/// several massing wings keeps its massing.
/// </remarks>
public partial class Main
{
    /// <summary>
    /// The parts a library's materials are matched to, by name or by name-and-hyphen prefix. A more
    /// specific name stands before the name it extends, so <c>wall-end</c> never resolves as
    /// <c>wall</c>.
    /// </summary>
    private static readonly string[] BodyParts =
    [
        "wall-end", "wall", "trim", "roof-new", "roof", "membrane", "glass", "door", "frame", "metal",
        "plinth", "storefront", "sign", "awning", "solar", "spandrel", "reveal", "paving", "planter",
        "tree",
    ];

    private static readonly string[] RoofParts = ["roof-new", "roof", "membrane"];

    /// <summary>
    /// The parts a far box takes its wall colour from, the most wall-like first. A tower shaft has
    /// no wall part at all — its facade is spandrels and fins — so a body without one is painted in
    /// the surface that stands for it rather than in a default grey.
    /// </summary>
    private static readonly string[] WallParts = ["wall", "spandrel", "trim", "wall-end"];

    private readonly Dictionary<ulong, PlacedBody> _placedBodies = [];
    private readonly Dictionary<ulong, BodyNeighbours> _bodyNeighbours = [];
    private readonly Dictionary<BodyKey, BodyShape> _familyBodyMeshes = [];
    private readonly Dictionary<(ArrayMesh Mesh, bool Abandoned), InstanceLayer> _bodyLayers = [];
    private readonly Dictionary<string, Dictionary<string, Material>> _bodyLibraries = [];
    private readonly Dictionary<string, Material> _libraryMaterials = [];
    private readonly Dictionary<Material, (Material Painted, float Mean)> _paintedMaterials = [];
    private readonly Dictionary<Material, Color> _meanColours = [];
    private readonly HashSet<Vector2I> _nearChunks = [];
    private readonly List<ulong> _bodyLayerIds = [];
    private Dictionary<(int East, int North), List<int>>? _footprints;
    private ShaderMaterial? _derelictBody;
    private bool _familyBodies;
    private bool _reportFamilyBodies;
    private float _bodyNearMetres = BodyNearMetres;

    /// <summary>How far an abandoned body's own materials give way to <see cref="Derelict"/>.</summary>
    private const float DerelictBodyShare = 0.75f;

    /// <summary>PROVISIONAL reach of the body band, from the camera to the nearest point of a chunk.</summary>
    private const float BodyNearMetres = 500f;

    /// <summary>The side of the square, in Tiles, that <see cref="Footprints"/> files Buildings under.</summary>
    private const int FootprintSquareTiles = 64;

    /// <summary>
    /// A Tower Lot's own plan, in the frame the body builder takes, so frontage runs along the Street.
    /// </summary>
    private readonly record struct TowerSite(int Storeys, int PodiumStoreys, float ShaftFrontage, float ShaftDepth);

    /// <summary>Where a body probed for side neighbours, and the Buildings it found there; 0 is none.</summary>
    private readonly record struct BodyNeighbours(int Slot, Vector3 LeftProbe, Vector3 RightProbe, ulong LeftId, ulong RightId);

    /// <summary>A generated body, and the linear mean colours its far box is painted with.</summary>
    private sealed record BodyShape(ArrayMesh Mesh, Color Wall, Color Roof, FamilyBody Body);

    private readonly record struct PlacedBody(
        ulong Id,
        InstanceLayer NearLayer,
        BodyShape NearShape,
        InstanceLayer? FarLayer,
        BodyShape? FarShape,
        Transform3D At);

    /// <summary>
    /// Everything a generated mesh depends on, in centimetres where it is a length. A Tower's podium
    /// and shaft are in the key, so two towers of the same height with different podiums are two
    /// meshes.
    /// </summary>
    private readonly record struct BodyKey(
        string Family, int Frontage, int Depth, int Storeys, AttachedSides Attached, int Paint,
        int PodiumStoreys, int ShaftFrontage, int ShaftDepth, bool Ring, FamilyBodyDetail Detail);

    /// <summary>Everything a Building's body is placed from, read from the World.</summary>
    private readonly record struct BodyRequest(
        Massing One, AppearanceFamily Family, FamilyBody Body, float Frontage, float Depth, int Storeys,
        AttachedSides Attached, int Paint, float Turn, BodyNeighbours Neighbours, TowerSite? Site, Vector3 Origin, bool Ring)
    {
        public BodyKey Key(FamilyBodyDetail detail) => new(Family.Id, Mathf.RoundToInt(Frontage * 100f),
            Mathf.RoundToInt(Depth * 100f), Storeys, Attached, Paint, Site?.PodiumStoreys ?? 0,
            Mathf.RoundToInt((Site?.ShaftFrontage ?? 0f) * 100f),
            Mathf.RoundToInt((Site?.ShaftDepth ?? 0f) * 100f), Ring, detail);
    }

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
        _bodiesButton?.SetPressedNoSignal(_familyBodies);
        _bodyNearMetres = near;
        _reportFamilyBodies = _familyBodies;
        _nearChunks.Clear();
        foreach (InstanceLayer layer in new[] { _buildings, _roofs, _hips, _pairedRoofs, _parapets })
        {
            layer.Multimesh.Near = _familyBodies ? NearChunk : null;
            layer.Multimesh.Repartition();
        }

        foreach (InstanceLayer layer in _bodyLayers.Values) layer.Multimesh.Repartition();

        // The fade's ring ends at the band's reach, so a new NEAR moves it.
        ApplyFarFade();
        _world.Changes!.Invalidate();
    }

    private void PlaceFamilyBodies()
    {
        foreach ((ulong id, PlacedBody placed) in _placedBodies)
        {
            placed.NearLayer.Multimesh.Replace(id, []);
            placed.FarLayer?.Multimesh.Replace(id, []);
        }
        _placedBodies.Clear();
        _bodyNeighbours.Clear();
        OverlayAbandonedBodies();
        if (!_familyBodies) return;

        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        _footprints = Footprints();
        var requests = new List<BodyRequest>();
        for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
        {
            if (_world.Buildings.Rows.IsLive(slot) && RequestBody(slot) is { } request) requests.Add(request);
        }

        _footprints = null;
        BuildBodyMeshes(requests);
        foreach (BodyRequest request in requests) PlaceBody(request);

        GD.Print($"family_bodies\t{_placedBodies.Count}\t{_familyBodyMeshes.Count} meshes\t{_bodyLayers.Count} layers"
            + $"\t{System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds:F1} ms");
        _reportFamilyBodies = false;
    }

    /// <summary>
    /// Keeps the placed bodies through a pass that changed no geometry beyond the changed
    /// Buildings. Their bodies and their neighbours' are placed again; <see cref="Massings"/>
    /// retints the rest.
    /// </summary>
    private void RefreshFamilyBodies(ReadOnlySpan<int> changed)
    {
        OverlayAbandonedBodies();
        if (!_familyBodies) return;

        var changedIds = new HashSet<ulong>();
        var placed = new List<int>();
        foreach (int slot in changed)
        {
            if (_renderedBuildings.TryGetValue(slot, out (ulong Id, bool Drawn) old))
            {
                RemoveFamilyBody(old.Id);
                changedIds.Add(old.Id);
            }

            if (!_world.Buildings.Rows.IsLive(slot)) continue;
            ulong id = IdAt(slot);
            changedIds.Add(id);
            placed.Add(slot);
            RemoveFamilyBody(id);
            PlaceFamilyBody(slot);
        }

        RefreshNeighbourBodies(changedIds, placed);
    }

    private void OverlayAbandonedBodies()
    {
        foreach (((ArrayMesh _, bool abandoned), InstanceLayer layer) in _bodyLayers)
        {
            if (abandoned) layer.MaterialOverlay = _washing == Wash.None ? DerelictBody() : null;
        }
    }

    private void RemoveFamilyBody(ulong id)
    {
        if (_placedBodies.Remove(id, out PlacedBody placed))
        {
            placed.NearLayer.Multimesh.Replace(id, []);
            placed.FarLayer?.Multimesh.Replace(id, []);
        }
        _bodyNeighbours.Remove(id);
    }

    /// <returns><c>true</c> where the Building now draws as its family's body.</returns>
    private bool PlaceFamilyBody(int slot)
    {
        if (RequestBody(slot) is not { } request) return false;
        PlaceBody(request);
        return true;
    }

    private BodyRequest? RequestBody(int slot)
    {
        if (!_familyBodies) return null;
        if (FamilyOf(slot).Family is not { Body: { } body } family) return null;

        using IEnumerator<Massing> parts = Buildings(slot).GetEnumerator();
        if (!parts.MoveNext()) return null;
        Massing one = parts.Current;
        if (_exactBodies.ContainsKey(one.Id)) return null;

        // THE PODIUM IS MASSING 0 AND IT CARRIES THE WHOLE SITE, so a Tower's placement, frontage,
        // depth and facing come off the same box every other Building's do. Its height does not,
        // because a Tower's storeys are the Lot's and the podium box holds only the first few.
        Vector3 size = one.Body.Basis.Scale;
        float faceEast = (one.Reads.R * 2f) - 1f;
        float faceSouth = (one.Reads.G * 2f) - 1f;
        bool facesNorthSouth = Mathf.Abs(faceSouth) > 0.5f;
        float frontage = facesNorthSouth ? size.X : size.Z;
        float depth = facesNorthSouth ? size.Z : size.X;
        TowerSite? site = body.Tower is null ? null : TowerSiteOf(slot, facesNorthSouth);
        if (body.Tower is not null && site is null) return null;
        Vector3 origin = one.Body.Origin;
        float turn = Mathf.Atan2(faceEast, faceSouth);
        bool ring = false;
        if (site is null && parts.MoveNext())
        {
            if (body.Midrise is null || RingOf(slot) is not { } footprint) return null;

            // Every outer face of a ring is a street face, so the ring is built facing south.
            ring = true;
            (origin, frontage, depth, turn) = (footprint.Centre, footprint.Wide, footprint.Deep, 0f);
        }

        int storeys = site?.Storeys ?? Mathf.Max(1, Mathf.RoundToInt(size.Y / StoreyMetres));
        AttachedSides attached = Attached(slot, origin, turn, frontage, out BodyNeighbours neighbours);
        int paint = FamilyPicker.Paint(family, _world.Key, one.Id);
        return new BodyRequest(one, family, body, frontage, depth, storeys, attached, paint, turn, neighbours, site, origin, ring);
    }

    private void PlaceBody(BodyRequest request)
    {
        Massing one = request.One;
        BodyShape nearShape = BodyMesh(request, FamilyBodyDetail.Near);
        InstanceLayer nearLayer = BodyLayer(nearShape.Mesh, one.Abandoned, far: false);
        var at = new Transform3D(new Basis(Vector3.Up, request.Turn), request.Origin with { Y = 0f });
        nearLayer.Multimesh.Replace(one.Id, [new InstanceValue(at, Colors.White, Tint(one))]);

        BodyShape? farShape = null;
        InstanceLayer? farLayer = null;
        if (request.Body.Midrise is not null || request.Body.Tower is not null)
        {
            farShape = BodyMesh(request, FamilyBodyDetail.Far);
            farLayer = BodyLayer(farShape.Mesh, one.Abandoned, far: true);
            farLayer.Multimesh.Replace(one.Id, [new InstanceValue(at, Colors.White, Tint(one), Far: true)]);
        }

        _placedBodies[one.Id] = new PlacedBody(one.Id, nearLayer, nearShape, farLayer, farShape, at);
        _bodyNeighbours[one.Id] = request.Neighbours;
        if (_reportFamilyBodies)
        {
            GD.Print($"family_body\t{request.Family.Id}\tbuilding {one.Id}\t{request.Frontage}x{request.Depth} m\t{request.Storeys} storeys"
                + $"\tattached {request.Attached}\tpaint {request.Paint}"
                + $"\ttile {Mathf.RoundToInt(one.Body.Origin.X / MetresPerTile)} {Mathf.RoundToInt(-one.Body.Origin.Z / MetresPerTile)}"
                + (request.Site is { } t ? $"\tpodium {t.PodiumStoreys}\tshaft {t.ShaftFrontage}x{t.ShaftDepth} m" : "")
                + (request.Ring ? "\tring" : "")
                + (one.Abandoned ? "\tabandoned" : ""));
        }
    }

    /// <summary>
    /// A Tower Lot's own plan, read from the same <see cref="BuildingPlan.Tower"/> partition the
    /// Massings and the Lot's floor area come from.
    /// </summary>
    /// <returns><c>null</c> where the Lot is gone or its block raises no Tower.</returns>
    private TowerSite? TowerSiteOf(int slot, bool facesNorthSouth)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[slot], out int lot)
            || lots.PatternOf(lot) != BlockPattern.Tower) return null;

        int storeys = Mathf.Max(1, lots.Storeys[lot]);
        BuildingPlan.TowerForm form = BuildingPlan.Tower(
            lots.FootprintWide[lot].Raw, lots.FootprintDeep[lot].Raw, storeys, lots.PodiumStoreys[lot]);
        float eastWest = form.ShaftWide * MetresPerTile, southNorth = form.ShaftDeep * MetresPerTile;
        return new TowerSite(storeys, form.PodiumStoreys,
            facesNorthSouth ? eastWest : southNorth,
            facesNorthSouth ? southNorth : eastWest);
    }

    /// <summary>A hollow Lot's whole footprint in metres, read from the same <see cref="BuildingPlan.Hollow"/> call its wings are.</summary>
    /// <returns><c>null</c> where the Lot is gone or its footprint is solid.</returns>
    private (Vector3 Centre, float Wide, float Deep)? RingOf(int slot)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[slot], out int lot)) return null;
        int wide = lots.FootprintWide[lot].Raw, deep = lots.FootprintDeep[lot].Raw;
        if (!BuildingPlan.Hollow(lots.PatternOf(lot), wide, deep, out _, out _)) return null;
        float east = (lots.FootprintEast[lot].Raw + (wide * 0.5f)) * MetresPerTile;
        float north = (lots.FootprintNorth[lot].Raw + (deep * 0.5f)) * MetresPerTile;
        return (new Vector3(east, 0f, -north), wide * MetresPerTile, deep * MetresPerTile);
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
    /// <remarks>
    /// ⚠ The cap sits on THIS box's top and not at its height above the ground. The two agree for
    /// a box standing on the ground and part company for a Tower's shaft, which starts at its
    /// podium's top.
    /// </remarks>
    private static Massing FarMassing(Massing one, FamilyBody body)
    {
        Vector3 plan = one.Body.Basis.Scale;
        Vector3 at = one.Body.Origin;
        float top = at.Y + (plan.Y * 0.5f);
        if (body.GableDegrees <= 0f)
        {
            if (body.ParapetMetres <= 0f) return one with { Cap = Cap.Flat };
            var tray = Basis.FromScale(new Vector3(plan.X + (CopingMetres * 2f), body.ParapetMetres, plan.Z + (CopingMetres * 2f)));
            return one with { Cap = Cap.Parapet, Roof = new Transform3D(tray, at with { Y = top + (body.ParapetMetres * 0.5f) }) };
        }

        if (one.Cap is Cap.Gable or Cap.Hip or Cap.PairedGable) return one;

        float span = Mathf.Min(plan.X, plan.Z), ridge = Mathf.Max(plan.X, plan.Z);
        float rise = RoofHeight(Cap.Gable, Mathf.Tan(Mathf.DegToRad(body.GableDegrees)) * span * 0.5f);
        Basis capped = CapBasis(Cap.Gable, plan.X > plan.Z, span, ridge, rise);
        return one with { Cap = Cap.Gable, Roof = new Transform3D(capped, at with { Y = top + (rise * 0.5f) }) };
    }

    private InstanceLayer BodyLayer(ArrayMesh mesh, bool abandoned, bool far)
    {
        if (_bodyLayers.TryGetValue((mesh, abandoned), out InstanceLayer? found)) return found;

        var layer = new InstanceLayer();
        layer.Multimesh.Mesh = mesh;
        layer.Multimesh.UseCustomData = true;
        layer.Multimesh.Near = far && _farFade ? NoChunkNear : NearChunk;
        layer.Multimesh.NearOnly = !far;
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
        if (_footprints is not null)
        {
            float square = MetresPerTile * FootprintSquareTiles;
            if (!_footprints.TryGetValue((Mathf.FloorToInt(point.X / square), Mathf.FloorToInt(-point.Z / square)), out List<int>? near)) return -1;
            foreach (int other in near)
            {
                if (other != slot && Covers(other, point)) return other;
            }

            return -1;
        }

        BuildingTable table = _world.Buildings;
        for (int other = 0; other < table.Rows.SlotCount; other++)
        {
            if (other != slot && table.Rows.IsLive(other) && Covers(other, point)) return other;
        }

        return -1;
    }

    /// <summary>
    /// The live Building slots whose footprint overlaps each square of <see cref="FootprintSquareTiles"/>,
    /// in slot order, for a whole-city placement pass.
    /// </summary>
    private Dictionary<(int East, int North), List<int>> Footprints()
    {
        var squares = new Dictionary<(int East, int North), List<int>>();
        LotTable lots = _world.Lots;
        BuildingTable table = _world.Buildings;
        for (int slot = 0; slot < table.Rows.SlotCount; slot++)
        {
            if (!table.Rows.IsLive(slot) || !lots.Rows.TryResolve(table.Lot[slot], out int lot)) continue;
            int x = lots.FootprintEast[lot].Raw, y = lots.FootprintNorth[lot].Raw;
            int lastEast = Mathf.FloorToInt((x + lots.FootprintWide[lot].Raw - 1) / (float)FootprintSquareTiles);
            int lastNorth = Mathf.FloorToInt((y + lots.FootprintDeep[lot].Raw - 1) / (float)FootprintSquareTiles);
            for (int east = Mathf.FloorToInt(x / (float)FootprintSquareTiles); east <= lastEast; east++)
            {
                for (int north = Mathf.FloorToInt(y / (float)FootprintSquareTiles); north <= lastNorth; north++)
                {
                    if (!squares.TryGetValue((east, north), out List<int>? square)) squares[(east, north)] = square = [];
                    square.Add(slot);
                }
            }
        }

        return squares;
    }

    private bool Covers(int building, Vector3 point)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[building], out int lot)) return false;
        float east = point.X / MetresPerTile, north = -point.Z / MetresPerTile;
        int x = lots.FootprintEast[lot].Raw, y = lots.FootprintNorth[lot].Raw;
        return east >= x && east < x + lots.FootprintWide[lot].Raw && north >= y && north < y + lots.FootprintDeep[lot].Raw;
    }

    /// <summary>Generates the surfaces of every body the requests need and no mesh yet holds, across worker threads.</summary>
    private void BuildBodyMeshes(List<BodyRequest> requests)
    {
        Build(FamilyBodyDetail.Near, requests);
        long farStart = System.Diagnostics.Stopwatch.GetTimestamp();
        int farMeshes = Build(FamilyBodyDetail.Far,
            requests.Where(request => request.Body.Midrise is not null || request.Body.Tower is not null));
        GD.Print($"far_body_meshes\t{farMeshes}\t{System.Diagnostics.Stopwatch.GetElapsedTime(farStart).TotalMilliseconds:F1} ms");

        int Build(FamilyBodyDetail detail, IEnumerable<BodyRequest> source)
        {
            var missing = new List<BodyRequest>();
            var seen = new HashSet<BodyKey>();
            foreach (BodyRequest request in source)
            {
                BodyKey key = request.Key(detail);
                if (!_familyBodyMeshes.ContainsKey(key) && seen.Add(key)) missing.Add(request);
            }

            var surfaces = new List<BodySurface>[missing.Count];
            System.Threading.Tasks.Parallel.For(0, missing.Count,
                i => surfaces[i] = BodySurfaces(missing[i], detail));
            for (int i = 0; i < missing.Count; i++) BodyMesh(missing[i], detail, surfaces[i]);
            return missing.Count;
        }
    }

    private readonly record struct BodySurface(string Part, Godot.Collections.Array Arrays, int VertexCount);

    /// <summary>A body's surfaces with tangents and no colour. Touches no scene or rendering server, so any thread may call it.</summary>
    private static List<BodySurface> BodySurfaces(BodyRequest request, FamilyBodyDetail detail)
    {
        var surfaces = new List<BodySurface>();
        FamilyBodyMesh built = request switch
        {
            { Site: { } tower } => FamilyBodyBuilder.BuildTower(request.Body, request.Frontage, request.Depth,
                request.Storeys, tower.PodiumStoreys, tower.ShaftFrontage, tower.ShaftDepth, detail),
            { Body.Midrise: not null } => FamilyBodyBuilder.BuildMidrise(request.Body, request.Frontage, request.Depth,
                request.Storeys, request.Ring, request.Attached, detail),
            _ => FamilyBodyBuilder.Build(request.Body, request.Frontage, request.Depth, request.Storeys, request.Attached),
        };
        foreach ((string part, ShellMesh source) in built.Parts)
        {
            var positions = new Vector3[source.VertexCount];
            var normals = new Vector3[source.VertexCount];
            var uvs = new Vector2[source.VertexCount];
            var uv2s = new Vector2[source.VertexCount];
            for (int i = 0; i < source.VertexCount; i++)
            {
                System.Numerics.Vector3 p = source.Positions[i], n = source.Normals[i];
                positions[i] = new Vector3(p.X, p.Y, p.Z);
                normals[i] = new Vector3(n.X, n.Y, n.Z);
                uvs[i] = new Vector2(source.Uvs[i].X, source.Uvs[i].Y);
                uv2s[i] = new Vector2(source.Uv2s[i].X, source.Uv2s[i].Y);
            }

            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = positions;
            arrays[(int)Mesh.ArrayType.Normal] = normals;
            arrays[(int)Mesh.ArrayType.TexUV] = uvs;
            arrays[(int)Mesh.ArrayType.TexUV2] = uv2s;
            arrays[(int)Mesh.ArrayType.Index] = source.Indices.ToArray();
            var tool = new SurfaceTool();
            tool.CreateFromArrays(arrays);
            tool.GenerateTangents();
            surfaces.Add(new BodySurface(part, tool.CommitToArrays(), source.VertexCount));
        }

        return surfaces;
    }

    private BodyShape BodyMesh(BodyRequest request, FamilyBodyDetail detail, List<BodySurface>? surfaces = null)
    {
        BodyKey key = request.Key(detail);
        if (_familyBodyMeshes.TryGetValue(key, out BodyShape? cached)) return cached;

        (AppearanceFamily family, FamilyBody body, int paint) = (request.Family, request.Body, request.Paint);
        Dictionary<string, Material> library = BodyLibrary(body.Library);
        var mesh = new ArrayMesh();
        IReadOnlyDictionary<string, Paint>? scheme = paint >= 0 ? family.Paints![paint].Parts : null;
        IReadOnlyDictionary<string, Color>? farPaints = detail == FamilyBodyDetail.Far
            ? FarPaintColors(body, library, scheme)
            : null;
        FamilyBodyFarFacade? farFacade = detail == FamilyBodyDetail.Far
            ? FamilyBodyBuilder.FarFacadeDescription(body)
            : null;
        FarWindowFactoryInput? farInput = farFacade is null
            ? null
            : new FarWindowFactoryInput(family.Id, farFacade.Variant, farPaints!, farFacade.Cells);
        var reads = new Dictionary<string, Color>();
        foreach ((string part, Godot.Collections.Array arrays, int vertexCount) in surfaces ?? BodySurfaces(request, detail))
        {
            if (part == "far-facade" && farInput is not null)
            {
                Material farMaterial = FadingFacade(FarWindowMaterial(farInput));
                Vector2[] cellIds = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
                Dictionary<int, FamilyBodyFarCell> cells = farInput.CellTable.ToDictionary(cell => cell.Id);
                var colors = new Color[vertexCount];
                Color wallPaint = farInput.PaintSrgb.GetValueOrDefault("wall", new Color(.65f, .65f, .62f));
                for (int i = 0; i < colors.Length; i++)
                {
                    int cell = Mathf.RoundToInt(cellIds[i].X);
                    string paintPart = cells.GetValueOrDefault(cell)?.PaintPart ?? "wall";
                    colors[i] = farInput.PaintSrgb.GetValueOrDefault(paintPart, wallPaint).SrgbToLinear();
                }

                arrays[(int)Mesh.ArrayType.Color] = colors;
                reads[part] = wallPaint.SrgbToLinear();
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
                mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, farMaterial);
                continue;
            }

            bool dressed = body.Materials.TryGetValue(part, out string? texture);
            Material material = dressed ? LibraryMaterial(texture!)
                : library.GetValueOrDefault(part) ?? new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.8f, 0.78f) };
            bool paintable = !dressed || _textureLibrary!.Paintable.Contains(texture!);
            Color? tint = dressed ? new Color(1f, 1f, 1f, 0f) : null;
            reads[part] = MeanColour(material);
            if (paintable && scheme is not null && scheme.TryGetValue(part, out Paint colour))
            {
                if (dressed)
                {
                    tint = Color.Color8(colour.R, colour.G, colour.B);
                }
                else if (material is BaseMaterial3D authored)
                {
                    (material, tint) = Painted(authored, colour, family.Id, part);
                }

                reads[part] = Color.Color8(colour.R, colour.G, colour.B).SrgbToLinear();
            }

            if (tint is { } t) arrays[(int)Mesh.ArrayType.Color] = Enumerable.Repeat(t, vertexCount).ToArray();

            // Each side of the fade takes a copy of its material, because a far-window option and a
            // low-rise near body, which has no far form to cross-fade with, dress themselves from
            // the same library.
            bool bodied = body.Midrise is not null || body.Tower is not null;
            material = detail == FamilyBodyDetail.Far ? FadeMaterial(material)
                : bodied ? FadingNear(material)
                : material;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
        }

        Color fallback = MeanColour(null);
        Color wall = WallParts.Where(reads.ContainsKey).Select(part => reads[part]).DefaultIfEmpty(fallback).First();
        Color roof = RoofParts.Where(reads.ContainsKey).Select(part => reads[part]).DefaultIfEmpty(fallback).First();
        var shape = new BodyShape(mesh, wall, roof, body);
        _familyBodyMeshes[key] = shape;
        return shape;
    }

    private Dictionary<string, Color> FarPaintColors(FamilyBody body,
        Dictionary<string, Material> library, IReadOnlyDictionary<string, Paint>? scheme)
    {
        string[] parts = ["wall", "wall-end", "trim", "glass", "frame", "spandrel"];
        var colors = new Dictionary<string, Color>();
        foreach (string part in parts)
        {
            bool dressed = body.Materials.TryGetValue(part, out string? texture);
            bool present = dressed || library.ContainsKey(part) || (scheme?.ContainsKey(part) ?? false);
            if (!present) continue;
            Material material = dressed ? LibraryMaterial(texture!)
                : library.GetValueOrDefault(part) ?? new StandardMaterial3D { AlbedoColor = new Color(.8f, .8f, .78f) };
            Color color = MeanColour(material).LinearToSrgb();
            bool paintable = !dressed || _textureLibrary!.Paintable.Contains(texture!);
            if (paintable && scheme is not null && scheme.TryGetValue(part, out Paint paint))
            {
                color = Color.Color8(paint.R, paint.G, paint.B);
            }

            colors[part] = color;
        }

        colors.TryAdd("wall", new Color(.65f, .65f, .62f));
        return colors;
    }

    /// <summary>A material's mean albedo in linear light, its texture sampled on a 32 × 32 grid.</summary>
    private Color MeanColour(Material? material)
    {
        (Texture2D? albedo, Color tint) = material switch
        {
            BaseMaterial3D surface => (surface.AlbedoTexture, surface.AlbedoColor.SrgbToLinear()),
            ShaderMaterial shader => (shader.GetShaderParameter("albedo_map").As<Texture2D>(), Colors.White),
            _ => (null, new Color(0.6f, 0.6f, 0.58f)),
        };
        if (material is not (BaseMaterial3D or ShaderMaterial)) return tint;
        if (_meanColours.TryGetValue(material, out Color found)) return found;

        Color mean = Colors.White;
        if (albedo?.GetImage() is { } image)
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

    /// <summary>
    /// A texture library entry's albedo, normal and roughness maps as one <c>library-body.gdshader</c>
    /// material. The part's vertex colour carries its paint.
    /// </summary>
    private Material LibraryMaterial(string texture)
    {
        if (_libraryMaterials.TryGetValue(texture, out Material? found)) return found;

        string stem = TextureLibraryDirectory + texture;
        var material = new ShaderMaterial { ResourceName = texture, Shader = GD.Load<Shader>("res://library-body.gdshader") };
        material.SetShaderParameter("albedo_map", GD.Load<Texture2D>(stem + "-albedo.jpg"));
        material.SetShaderParameter("normal_map", GD.Load<Texture2D>(stem + "-normal.jpg"));
        if (ResourceLoader.Exists(stem + "-roughness.jpg")) material.SetShaderParameter("roughness_map", GD.Load<Texture2D>(stem + "-roughness.jpg"));
        material.SetShaderParameter("mean_luminance", _textureLibrary!.MeanLuminance[texture]);

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
