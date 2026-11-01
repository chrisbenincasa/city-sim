using System;
using System.Collections.Generic;
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

    /// <summary>How far a body moves back from its footprint's front, and how deep its front yard is.</summary>
    private readonly record struct FrontSetback(float Recess, float Yard, bool Dwelling, bool Shop);

    private void CreateFrontYardLayers()
    {
        _frontGround = Layer(Colors.White, Vector3.One, perInstance: true, casts: false);
        _frontHedges = Layer(Colors.White, Vector3.One, perInstance: true);
        var (_, middle, _) = TreeMeshes();
        _frontTrees = Layer(Colors.White, middle, perInstance: true);
        foreach (InstanceLayer layer in (InstanceLayer[])[_frontGround, _frontHedges, _frontTrees])
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
        if (_frontGroundEdits.Count > 0) _frontYardIds.Add(id);
        else _frontYardIds.Remove(id);
    }

    private const float ForecourtLeastMetres = .9f;
    private const float ForecourtBayMetres = 6f;
    private const float CafeLeastMetres = 1.4f;
    private const float UmbrellaLeastMetres = 2.4f;
    private const float BenchLeastMetres = 1.8f;
    private const float StreetTreeLeastMetres = 3f;

    // Kept clear along the shopfront so doors open onto a walkway.
    private const float ShopWalkMetres = 1.2f;

    private static readonly Color[] PlanterTones = [Hex(0x3b3f42), Hex(0xa65a3a), Hex(0x8a6a48), Hex(0x9a978f)];
    private static readonly Color[] CanvasTones = [Hex(0xe8e2d0), Hex(0x2f5d50), Hex(0x9a3b32), Hex(0x2d4a6b), Hex(0xd9a441)];
    private static readonly Color[] BloomTones = [Hex(0xc8506a), Hex(0xe0b040), Hex(0xe8e4dc), Hex(0x8a5ab0)];
    private static readonly Color PlantingTone = Hex(0x4d6b35);
    private static readonly Color StreetMetal = Hex(0x2b2e30);
    private static readonly Color BenchTimber = Hex(0x8a6a48);
    private static readonly Color GrateTone = Hex(0x4a4a46);

    private enum ForecourtEdge { Planters, Trees, Benches, Bollards }

    /// <summary>
    /// Street furniture on a shop's paved forecourt. Each shop picks one edge treatment, set at the
    /// bay lines along the street edge. Forecourts from 1.4 m deep may add café tables, and from 2.4 m their umbrellas.
    /// </summary>
    private void DressForecourt(YardDraw draw, float w, float near, float far, ulong id)
    {
        float deep = far - near;
        if (deep < ForecourtLeastMetres) return;

        uint h = (uint)id * 0x9E3779B1u;
        h ^= h >> 13;
        var edge = (ForecourtEdge)(h % 4);
        if (edge == ForecourtEdge.Trees && deep < StreetTreeLeastMetres) edge = ForecourtEdge.Planters;
        bool cafe = deep >= CafeLeastMetres && (h >> 2) % 5 < 2;
        if (edge == ForecourtEdge.Benches && (cafe || deep < BenchLeastMetres)) edge = ForecourtEdge.Planters;
        bool umbrellas = deep >= UmbrellaLeastMetres && (h >> 5) % 3 != 0;
        bool signs = (h >> 7) % 2 == 0;
        Color planter = PlanterTones[(h >> 9) % (uint)PlanterTones.Length];
        Color canvas = CanvasTones[(h >> 11) % (uint)CanvasTones.Length];
        Color bloom = BloomTones[(h >> 14) % (uint)BloomTones.Length];

        float frontage = w * 2f;
        int bays = Math.Max(1, Mathf.RoundToInt(frontage / ForecourtBayMetres));
        float pitch = frontage / bays;
        float kerbSide = far - .4f;

        for (int k = 0; k <= bays; k++)
        {
            float u = Mathf.Clamp(-w + (pitch * k), -w + .7f, w - .7f);
            bool end = k == 0 || k == bays;
            switch (edge)
            {
                case ForecourtEdge.Planters:
                    Planter(draw, u, kerbSide, planter, k % 2 == 0 ? bloom : PlantingTone);
                    break;
                case ForecourtEdge.Trees when !end:
                    _frontGroundEdits.Add(draw.At(u, far - .8f, 1.2f, 1.2f, .1f, .12f, GrateTone));
                    _frontTreeEdits.Add(draw.Tree(u, far - .8f, 4.5f + ((h + (uint)k) % 3 * .5f), Colors.White));
                    break;
                case ForecourtEdge.Trees:
                    Planter(draw, u, kerbSide, planter, PlantingTone);
                    break;
                case ForecourtEdge.Benches when !end:
                    Bench(draw, u, far - .45f);
                    break;
                case ForecourtEdge.Bollards:
                    for (int b = -1; b <= 1; b += 2)
                        _frontHedgeEdits.Add(draw.At(Mathf.Clamp(u + (b * .8f), -w + .2f, w - .2f), far - .25f, .16f, .16f, 0f, .9f, StreetMetal));
                    break;
            }
        }

        for (int k = 0; k < bays; k++)
        {
            float middle = -w + (pitch * (k + .5f));
            if (cafe && pitch >= 3.6f)
            {
                float s = deep >= UmbrellaLeastMetres ? Mathf.Max(near + ShopWalkMetres + .4f, far - 1.1f) : far - .55f;
                int sets = pitch >= 5.4f ? 2 : 1;
                for (int t = 0; t < sets; t++)
                    CafeSet(draw, sets == 1 ? middle : middle + ((t - .5f) * 2.6f), s, umbrellas, canvas);
            }
            else if (signs && k % 2 == 0)
            {
                _frontHedgeEdits.Add(draw.At(middle + 1.4f, far - .3f, .55f, .3f, 0f, .85f, canvas));
            }
        }
    }

    private void Planter(YardDraw draw, float u, float s, Color body, Color top)
    {
        _frontHedgeEdits.Add(draw.At(u, s, 1.2f, .5f, 0f, .55f, body));
        _frontHedgeEdits.Add(draw.At(u, s, 1.1f, .42f, .55f, .9f, PlantingTone));
        if (top != PlantingTone) _frontHedgeEdits.Add(draw.At(u, s, .8f, .3f, .9f, 1f, top));
    }

    private void Bench(YardDraw draw, float u, float s)
    {
        _frontHedgeEdits.Add(draw.At(u, s, 1.7f, .45f, .42f, .48f, BenchTimber));
        _frontHedgeEdits.Add(draw.At(u, s - .2f, 1.7f, .06f, .48f, .9f, BenchTimber));
        for (int side = -1; side <= 1; side += 2)
            _frontHedgeEdits.Add(draw.At(u + (side * .7f), s, .08f, .42f, 0f, .42f, StreetMetal));
    }

    private void CafeSet(YardDraw draw, float u, float s, bool umbrella, Color canvas)
    {
        _frontHedgeEdits.Add(draw.At(u, s, .7f, .7f, .72f, .76f, StreetMetal));
        _frontHedgeEdits.Add(draw.At(u, s, .08f, .08f, 0f, .72f, StreetMetal));
        for (int side = -1; side <= 1; side += 2)
        {
            float seat = u + (side * .62f);
            _frontHedgeEdits.Add(draw.At(seat, s, .42f, .42f, .44f, .48f, StreetMetal));
            _frontHedgeEdits.Add(draw.At(seat + (side * .19f), s, .04f, .42f, .48f, .9f, StreetMetal));
            _frontHedgeEdits.Add(draw.At(seat, s, .06f, .06f, 0f, .44f, StreetMetal));
        }
        if (!umbrella) return;
        _frontHedgeEdits.Add(draw.At(u, s, .05f, .05f, .76f, 2.3f, StreetMetal));
        _frontHedgeEdits.Add(draw.At(u, s, 2.2f, 2.2f, 2.2f, 2.3f, canvas));
        _frontHedgeEdits.Add(draw.At(u, s, 1.1f, 1.1f, 2.3f, 2.42f, canvas));
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

        public InstanceValue Tree(float u, float s, float tall, Color shade)
        {
            float broad = tall * .6f;
            Vector3 at = Origin + (Across * u) + (Forward * s) + (Vector3.Up * .05f);
            return new InstanceValue(new Transform3D(Basis.FromScale(new Vector3(broad, tall, broad)), at), shade);
        }
    }
}
