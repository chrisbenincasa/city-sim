using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Appearance;
using Q16 = Borough.Core.Arithmetic.Fixed;
using Borough.Core.Entities;
using Borough.Core.Space;
using Godot;
using Plane = System.Numerics.Vector2;

namespace Borough.Shell;

/// <summary>
/// Stands each family body back from its street edge and draws the strip in front as a front yard.
/// A dwelling stands 3–5 m from the street edge behind a lawn, a hedge or low wall and paths; every
/// other kind stands 1–2 m back behind paving. One distance holds along each side of a Segment, so
/// attached bodies keep one street line. A body moves back only by what its footprint lacks, and by
/// at most a fifth of its depth, which is the drawing's floor allowance (<c>docs/07-the-drawing.md</c>).
/// The city holds none of this, so it moves no State Hash.
/// </summary>
public partial class Main
{
    private const float DwellingFrontMetres = 3f;
    private const int DwellingFrontSteps = 5;
    private const float ShopFrontMetres = 1f;
    private const int ShopFrontSteps = 3;
    private const float FrontStepMetres = .5f;
    private const float FrontRecessShare = .2f;

    // The yard never reaches past this from the body, so it cannot run over parking or a forecourt the Lot draws.
    private const float FrontYardMostMetres = 6f;

    private const float HedgedYardLeastMetres = 1.5f;
    private const float YardTreeLeastMetres = 3f;
    private const float GateEveryMetres = 12f;

    private InstanceLayer _frontGround = null!;
    private InstanceLayer _frontHedges = null!;
    private InstanceLayer _frontTrees = null!;

    private readonly List<InstanceValue> _frontGroundEdits = [];
    private readonly List<InstanceValue> _frontHedgeEdits = [];
    private readonly List<InstanceValue> _frontTreeEdits = [];
    private readonly HashSet<ulong> _frontYardIds = [];

    private enum Furniture
    {
        CafeTable, Umbrella, Planter, PlanterGreen, PlanterBloom, Bench, Bollard, BikeRack, Bicycle, ABoard, TreeGrate,
        CafeScreen, Topiary,
    }

    /// <summary>The <c>assets/forecourt</c> modules <c>scripts/art/forecourt.py</c> authors, in <see cref="Furniture"/> order.</summary>
    private static readonly string[] FurnitureModules =
    [
        "cafe-table", "umbrella", "planter", "planter-green", "planter-bloom", "bench", "bollard", "bike-rack", "bicycle",
        "a-board", "tree-grate", "cafe-screen", "topiary",
    ];

    private static readonly string[] FurniturePainted = ["canvas", "planter", "bloom", "paint"];

    private readonly InstanceLayer[] _furniture = new InstanceLayer[FurnitureModules.Length];
    private readonly List<InstanceValue>[] _furnitureEdits = [.. FurnitureModules.Select(_ => new List<InstanceValue>())];

    /// <summary>How far a body moves back from its footprint's front, and how deep its front yard is.</summary>
    private readonly record struct FrontSetback(float Recess, float Yard, bool Dwelling, bool Shop);

    private void CreateFrontYardLayers()
    {
        _frontGround = Layer(Colors.White, Vector3.One, perInstance: true, casts: false);
        _frontHedges = Layer(Colors.White, Vector3.One, perInstance: true);
        var (_, middle, _) = TreeMeshes();
        _frontTrees = Layer(Colors.White, middle, perInstance: true);
        for (int i = 0; i < FurnitureModules.Length; i++)
            _furniture[i] = Layer(Colors.White, Commit(Load($"forecourt/{FurnitureModules[i]}", FurniturePainted).Corners), perInstance: true);

        foreach (InstanceLayer layer in (InstanceLayer[])[_frontGround, _frontHedges, _frontTrees, .. _furniture])
        {
            layer.Multimesh.NearOnly = true;
            BodyResidency(layer);
        }
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> FrontYardLayers()
    {
        yield return ("front-yard-ground", _frontGround, true, null);
        yield return ("front-yard-hedge", _frontHedges, true, null);
        yield return ("front-yard-tree", _frontTrees, true, null);
        for (int i = 0; i < FurnitureModules.Length; i++) yield return ($"forecourt-{FurnitureModules[i]}", _furniture[i], true, null);
    }

    /// <summary>A Building's setback, or none for a Tower, a courtyard ring or a Lot with no Street address.</summary>
    private FrontSetback SetbackOf(int building)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.TryResolve(_world.Buildings.Lot[building], out int lot)) return default;
        BlockPattern pattern = lots.PatternOf(lot);
        OrientedRectangle footprint = lots.Footprint(lot);
        if (pattern == BlockPattern.Tower || BuildingPlan.Hollow(pattern, footprint.Wide, footprint.Deep, out _, out _)) return default;

        string? kind = _names.KindId(_world.Buildings.Kind[building]);
        bool shop = kind == "shopfront" || DrawsShops(FamilyOf(building).Family?.Body);
        float depth = (LotGeometry.Front(_world, lot).Y != 0 ? footprint.Deep : footprint.Wide) * MetresPerTile;
        return FrontSetbackOf(lot, kind == "dwelling" && !shop, shop, depth);
    }

    private static bool DrawsShops(FamilyBody? body) =>
        body is not null && (body.Shopfront || body.Shop is not null || body.Midrise is { Shops: true });

    /// <returns>No setback where the Lot has no Street address.</returns>
    private FrontSetback FrontSetbackOf(int lot, bool dwelling, bool shop, float depth)
    {
        LotTable lots = _world.Lots;
        Address address = lots.AddressOf(lot);
        if (!address.Exists) return default;

        (long east, long north) = _world.Roads.Segments.Centerline[address.Segment].PointAt(Q16.FromInt(address.Offset.Raw));
        var street = new Plane(east / (float)Q16.One, north / (float)Q16.One);
        OrientedRectangle footprint = lots.Footprint(lot);
        Plane front = LotGeometry.Front(_world, lot);
        Plane middle = LotGeometry.Point(footprint,
            new Plane(footprint.Wide * (1f + front.X) * .5f, footprint.Deep * (1f + front.Y) * .5f));
        Plane outward = Plane.Normalize(LotGeometry.Direction(footprint, front));
        float half = (RoadKind)_world.Roads.Segments.Kind[address.Segment] == RoadKind.Arterial
            ? ArterialWidthMetres * .5f
            : StreetWidthMetres * .5f;
        float gap = Mathf.Max(0f, (Plane.Dot(street - middle, outward) * MetresPerTile) - half);

        uint mix = ((uint)address.Segment * 0x9E3779B1u) ^ ((uint)address.Side * 0x85EBCA77u);
        mix ^= mix >> 15;
        float target = dwelling
            ? DwellingFrontMetres + (mix % DwellingFrontSteps * FrontStepMetres)
            : ShopFrontMetres + (mix % ShopFrontSteps * FrontStepMetres);
        float recess = Mathf.Clamp(target - gap, 0f, depth * FrontRecessShare);
        recess = Mathf.Floor(recess / FrontStepMetres) * FrontStepMetres;
        return new FrontSetback(recess, Mathf.Min(recess + gap, FrontYardMostMetres), dwelling, shop);
    }

    /// <summary>Draws a Building's front yard in front of its massing box, which already stands back by the setback.</summary>
    private void PlaceFrontYard(int building, Massing one)
    {
        ulong id = one.Id;
        _frontGroundEdits.Clear();
        _frontHedgeEdits.Clear();
        _frontTreeEdits.Clear();
        foreach (List<InstanceValue> edits in _furnitureEdits) edits.Clear();
        FrontSetback setback = SetbackOf(building);
        if (setback.Yard >= FrontStepMetres)
        {
            Vector3 size = one.Body.Basis.Scale;
            float faceEast = (one.Reads.R * 2f) - 1f, faceSouth = (one.Reads.G * 2f) - 1f;
            int lot = _world.Lots.Rows.Resolve(_world.Buildings.Lot[building]);
            bool alongA = LotGeometry.Front(_world, lot).Y != 0;
            float frontage = alongA ? size.X : size.Z, depth = alongA ? size.Z : size.X;
            var forward = new Vector3(faceEast, 0f, faceSouth).Normalized();
            var across = new Vector3(forward.Z, 0f, -forward.X);
            float w = frontage * .5f, near = depth * .5f, far = near + setback.Yard;
            var draw = new YardDraw(one.Body.Origin with { Y = 0f }, forward, across);
            int pick = (int)(id % 997);
            Color stone = StoneTones[pick % StoneTones.Length];

            if (!setback.Dwelling || setback.Yard < HedgedYardLeastMetres)
            {
                _frontGroundEdits.Add(draw.Box(-w, w, near, far, 0f, .1f, stone));
                if (setback.Shop) DressForecourt(draw, w, near, far, id);
            }
            else
            {
                _frontGroundEdits.Add(draw.Box(-w, w, near, far, 0f, .06f, LawnTones[pick % LawnTones.Length]));
                _frontGroundEdits.Add(draw.Box(-w, w, near, near + 1.2f, 0f, .09f, stone));
                int gates = Math.Max(1, Mathf.RoundToInt(frontage / GateEveryMetres));
                float pitch = frontage / gates;
                bool wall = pick % 5 < 2;
                float edge = far - .2f, hedgeIn = edge - (wall ? .3f : .7f), high = wall ? .6f : 1.1f;
                float from = -w + .2f;
                for (int k = 0; k < gates; k++)
                {
                    float gate = -w + (pitch * (k + .5f));
                    _frontGroundEdits.Add(draw.Box(gate - .8f, gate + .8f, near + 1.2f, far, 0f, .09f, stone));
                    _frontHedgeEdits.Add(draw.Box(from, gate - 1f, hedgeIn, edge, 0f, high, wall ? WallTone : HedgeTone));
                    from = gate + 1f;
                    if (k > 0 && setback.Yard >= YardTreeLeastMetres)
                    {
                        float shade = .88f + ((pick + k) % 7 * .04f);
                        _frontTreeEdits.Add(draw.Tree(-w + (pitch * k), (near + 1.2f + hedgeIn) * .5f, 5.5f + ((pick + k) % 3), new Color(shade, shade, shade)));
                    }
                }
                _frontHedgeEdits.Add(draw.Box(from, w - .2f, hedgeIn, edge, 0f, high, wall ? WallTone : HedgeTone));
            }
            if (_reportFamilyBodies) GD.Print($"front_yard\tbuilding {id}\tsetback {setback.Recess}\tyard {setback.Yard}\t{(setback.Dwelling ? "garden" : "paved")}");
        }

        _frontGround.Multimesh.Replace(id, _frontGroundEdits);
        _frontHedges.Multimesh.Replace(id, _frontHedgeEdits);
        _frontTrees.Multimesh.Replace(id, _frontTreeEdits);
        for (int i = 0; i < _furniture.Length; i++) _furniture[i].Multimesh.Replace(id, _furnitureEdits[i]);
        if (_frontGroundEdits.Count > 0) _frontYardIds.Add(id);
        else _frontYardIds.Remove(id);
    }

    private const float ForecourtLeastMetres = .9f;
    private const float ForecourtBayMetres = 6f;
    private const float CafeLeastMetres = 1.4f;
    private const float UmbrellaLeastMetres = 2.4f;
    private const float BenchLeastMetres = 1.2f;
    private const float BikeLeastMetres = 1f;
    private const float TopiaryLeastMetres = 1.2f;
    private const float StreetTreeLeastMetres = 1.4f;
    private const float StandEveryMetres = 1.8f;
    private const float PavingTop = .1f;
    private const float ScreenPanelMetres = 1.8f;
    private const float ScreenGapMetres = .6f;

    // Screen centers stay this far from the frontage middle, where the topiary pair also assumes the door.
    private const float DoorGapMetres = 1.8f;

    // Kept clear along the shopfront so doors open onto a walkway.
    private const float ShopWalkMetres = 1.2f;

    private static readonly Color[] PlanterTones = [Hex(0x3b3f42), Hex(0xa65a3a), Hex(0x8a6a48), Hex(0x9a978f), Hex(0xd8d2c4)];
    private static readonly Color[] CanvasTones = [Hex(0xe8e2d0), Hex(0x2f5d50), Hex(0x9a3b32), Hex(0x2d4a6b), Hex(0xd9a441)];
    private static readonly Color[] BloomTones = [Hex(0xc8506a), Hex(0xe0b040), Hex(0xe8e4dc), Hex(0x8a5ab0), Hex(0xd8703a)];
    private static readonly Color[] BikeTones = [Hex(0x2d4a6b), Hex(0x9a3b32), Hex(0x2f5d50), Hex(0xe8e2d0), Hex(0x23272a), Hex(0xd9a441)];

    private enum ForecourtEdge { Planters, Trees, Benches, Bollards, Bikes }

    /// <summary>
    /// Street furniture on a shop's paved forecourt. Each shop picks one edge treatment along the street
    /// edge, set at its bay lines. Forecourts from 1.4 m deep may set out café tables, screened from the street
    /// or not, and from 2.4 m shade them with parasols. Some shops also put out an A-board,
    /// and some flank their middle with potted topiaries.
    /// </summary>
    private void DressForecourt(YardDraw draw, float w, float near, float far, ulong id)
    {
        float deep = far - near;
        if (deep < ForecourtLeastMetres) return;

        uint h = (uint)id * 0x9E3779B1u;
        h ^= h >> 13;
        var edge = (ForecourtEdge)(h % 5);
        if (edge == ForecourtEdge.Trees && deep < StreetTreeLeastMetres) edge = ForecourtEdge.Planters;
        if (edge == ForecourtEdge.Bikes && deep < BikeLeastMetres) edge = ForecourtEdge.Bollards;
        bool cafe = deep >= CafeLeastMetres && (h >> 3) % 5 < 2;
        if (edge == ForecourtEdge.Benches && (cafe || deep < BenchLeastMetres)) edge = ForecourtEdge.Planters;
        bool umbrellas = deep >= UmbrellaLeastMetres && (h >> 5) % 3 != 0;
        bool screens = cafe && (h >> 19) % 2 == 0;
        bool signs = (h >> 7) % 2 == 0;
        bool topiary = deep >= TopiaryLeastMetres && w >= 2.5f && (h >> 16) % 3 == 0;
        Color planter = PlanterTones[(h >> 9) % (uint)PlanterTones.Length];
        Color canvas = CanvasTones[(h >> 11) % (uint)CanvasTones.Length];
        Color bloom = BloomTones[(h >> 14) % (uint)BloomTones.Length];

        float frontage = w * 2f;
        int bays = Math.Max(1, Mathf.RoundToInt(frontage / ForecourtBayMetres));
        float pitch = frontage / bays;
        bool cafeBays = cafe && pitch >= 3.6f;

        for (int k = 0; k <= bays; k++)
        {
            float u = Mathf.Clamp(-w + (pitch * k), -w + .7f, w - .7f);
            bool end = k == 0 || k == bays;
            uint pick = h + (uint)(k * 7);
            switch (edge)
            {
                case ForecourtEdge.Planters:
                    Planter(draw, u, far - .4f, planter, k % 2 == 0 ? bloom : (Color?)null, pick);
                    break;
                case ForecourtEdge.Trees when !end:
                    Furnish(Furniture.TreeGrate, draw.Module(u, far - .8f, 0f, 0f, Colors.White));
                    _frontTreeEdits.Add(draw.Tree(u, far - .8f, 4.5f + (pick % 3 * .5f), Colors.White));
                    break;
                case ForecourtEdge.Trees:
                    Planter(draw, u, far - .4f, planter, null, pick);
                    break;
                case ForecourtEdge.Benches when !end:
                    Furnish(Furniture.Bench, draw.Module(u, far - .4f, PavingTop, Mathf.Pi, Colors.White));
                    break;
                case ForecourtEdge.Bollards when !screens:
                    for (int b = -1; b <= 1; b += 2)
                        Furnish(Furniture.Bollard, draw.Module(Mathf.Clamp(u + (b * .8f), -w + .2f, w - .2f), far - .25f, PavingTop, 0f, Colors.White));
                    break;
                case ForecourtEdge.Bikes when !end && pitch >= 3f:
                    float stands = far - .55f;
                    Furnish(Furniture.BikeRack, draw.Module(u, stands, PavingTop, 0f, Colors.White));
                    int parked = 1 + (int)(pick % 3);
                    for (int b = 0; b < parked; b++)
                    {
                        float at = u + ((b % 2 == 0 ? -.5f : .5f) * StandEveryMetres);
                        float side = b < 2 ? -.13f : .13f;
                        Furnish(Furniture.Bicycle, draw.Module(at, stands + side, PavingTop, b % 2 == 0 ? 0f : Mathf.Pi,
                            BikeTones[(pick + (uint)b) % (uint)BikeTones.Length]));
                    }
                    break;
            }
        }

        for (int k = 0; k < bays; k++)
        {
            float middle = -w + (pitch * (k + .5f));
            uint pick = h + (uint)(k * 13);
            if (cafeBays)
            {
                float s = deep >= UmbrellaLeastMetres ? Mathf.Max(near + ShopWalkMetres + .4f, far - (screens ? 1.4f : 1.1f)) : far - (screens ? .85f : .55f);
                int sets = pitch >= 5.4f ? 2 : 1;
                for (int t = 0; t < sets; t++)
                {
                    float u = sets == 1 ? middle : middle + ((t - .5f) * 2.6f);
                    float jitter = (((pick >> t) % 5) - 2f) * .06f;
                    Furnish(Furniture.CafeTable, draw.Module(u, s, PavingTop, jitter, Colors.White));
                    // One parasol a bay, because two tables 2.6 m apart would close their canopies into one band.
                    if (umbrellas && t == 0) Furnish(Furniture.Umbrella, draw.Module(middle, s, PavingTop, 0f, canvas));
                }

                if (screens)
                {
                    int panels = Math.Max(1, (int)((pitch - (2f * ScreenGapMetres)) / ScreenPanelMetres));
                    float reach = (pitch * .5f) - (ScreenPanelMetres * .5f) - .3f + .01f;
                    for (int p = 0; p < panels; p++)
                    {
                        float c = middle + ((p - ((panels - 1) * .5f)) * ScreenPanelMetres);
                        if (Mathf.Abs(c) < DoorGapMetres) c = Mathf.Sign(c) * DoorGapMetres;
                        if (c != 0f && Mathf.Abs(c - middle) <= reach) Furnish(Furniture.CafeScreen, draw.Module(c, far - .25f, PavingTop, 0f, canvas));
                    }
                }
            }
            else if (signs && k % 2 == 0)
            {
                Furnish(Furniture.ABoard, draw.Module(middle + 1.4f, far - .5f, PavingTop, ((pick % 5) - 2f) * .12f, canvas));
            }
        }

        if (topiary)
        {
            for (int side = -1; side <= 1; side += 2)
                Furnish(Furniture.Topiary, draw.Module(side * 1.3f, near + .4f, PavingTop, 0f, Colors.White));
        }
    }

    private void Furnish(Furniture module, InstanceValue placed) => _furnitureEdits[(int)module].Add(placed);

    private void Planter(YardDraw draw, float u, float s, Color body, Color? bloom, uint pick)
    {
        float shade = .9f + (pick % 5 * .05f);
        Furnish(Furniture.Planter, draw.Module(u, s, PavingTop, 0f, body));
        Furnish(Furniture.PlanterGreen, draw.Module(u, s, PavingTop, pick % 2 == 0 ? 0f : Mathf.Pi, new Color(shade, shade, shade)));
        if (bloom is { } tone) Furnish(Furniture.PlanterBloom, draw.Module(u, s, PavingTop, pick % 2 == 0 ? 0f : Mathf.Pi, tone));
    }

    private void PlaceFrontYards()
    {
        var kept = new HashSet<ulong>();
        var rows = _world.Buildings.Rows;
        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot) && PlaceFrontYard(slot)) kept.Add(rows.IdAt(slot));
        }

        _frontYardIds.RemoveWhere(id =>
        {
            if (kept.Contains(id)) return false;
            RemoveFrontYard(id);
            return true;
        });
    }

    /// <returns>Whether the Building now has a front yard.</returns>
    private bool PlaceFrontYard(int building)
    {
        ulong id = _world.Buildings.Rows.IdAt(building);
        bool modules = _world.IsSupermarket(building) || _world.IsDepartmentStore(building) || _world.IsPadSite(building)
            || _world.IsSalesYard(building) || _world.IsPrecinct(building) || _world.IsMarketHall(building);
        if (!modules && !_shelled.Contains(id) && !_exactBodies.ContainsKey(id))
        {
            foreach (Massing one in Buildings(building))
            {
                PlaceFrontYard(building, one);
                return _frontYardIds.Contains(id);
            }
        }

        RemoveFrontYard(id);
        return false;
    }

    private void RemoveFrontYard(ulong id)
    {
        _frontYardIds.Remove(id);
        _frontGround.Multimesh.Replace(id, []);
        _frontHedges.Multimesh.Replace(id, []);
        _frontTrees.Multimesh.Replace(id, []);
        foreach (InstanceLayer layer in _furniture) layer.Multimesh.Replace(id, []);
    }

    /// <summary>Boxes and trees in a body's own frame: across its frontage, forward toward the Street.</summary>
    private readonly record struct YardDraw(Vector3 Origin, Vector3 Forward, Vector3 Across)
    {
        public InstanceValue Box(float u0, float u1, float s0, float s1, float y0, float y1, Color paint)
        {
            Vector3 centre = Origin + (Across * ((u0 + u1) * .5f)) + (Forward * ((s0 + s1) * .5f)) + (Vector3.Up * ((y0 + y1) * .5f));
            var basis = new Basis(Across * Mathf.Max(u1 - u0, .01f), Vector3.Up * (y1 - y0), Forward * Mathf.Max(s1 - s0, .01f));
            return new InstanceValue(new Transform3D(basis, centre), paint);
        }

        public InstanceValue At(float u, float s, float wide, float deep, float y0, float y1, Color paint) =>
            Box(u - (wide * .5f), u + (wide * .5f), s - (deep * .5f), s + (deep * .5f), y0, y1, paint);

        /// <summary>A whole forecourt module, its street side toward the Street, turned about the upright by <paramref name="turn"/>.</summary>
        public InstanceValue Module(float u, float s, float y, float turn, Color paint)
        {
            (float sin, float cos) = MathF.SinCos(turn);
            Vector3 across = (Across * cos) + (Forward * sin);
            Vector3 forward = (Forward * cos) - (Across * sin);
            Vector3 at = Origin + (Across * u) + (Forward * s) + (Vector3.Up * y);
            return new InstanceValue(new Transform3D(new Basis(across, Vector3.Up, forward), at), paint);
        }

        public InstanceValue Tree(float u, float s, float tall, Color shade)
        {
            float broad = tall * .6f;
            Vector3 at = Origin + (Across * u) + (Forward * s) + (Vector3.Up * .05f);
            return new InstanceValue(new Transform3D(Basis.FromScale(new Vector3(broad, tall, broad)), at), shade);
        }
    }
}
