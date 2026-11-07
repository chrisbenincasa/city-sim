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
/// A mansion is the exception: it stands back by its family's yard, deep where that leaves the house its
/// least depth and shallow otherwise, and <see cref="DressMansion"/> draws its grounds.
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
    private InstanceLayer _frontTrees = null!;

    private readonly List<InstanceValue> _frontGroundEdits = [];
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

    private enum FrontKit { Hedge, Wall, Railing, Picket, Pier, GateIron, GateTimber, PathFlags, PathTiled, Doorstep, Border, Fountain, Lamp, Mailbox, Car }

    /// <summary>The <c>assets/front-yard</c> modules <c>scripts/art/front-yard.py</c> authors, in <see cref="FrontKit"/> order.</summary>
    private static readonly string[] FrontKitModules =
        ["hedge", "wall", "railing", "picket", "pier", "gate-iron", "gate-timber", "path-flags", "path-tiled", "doorstep", "border",
         "fountain", "lamp", "mailbox", "car"];

    private static readonly string[] FrontKitPainted = ["brick", "paint", "flag", "bloom"];

    private readonly InstanceLayer[] _frontKit = new InstanceLayer[FrontKitModules.Length];
    private readonly List<InstanceValue>[] _frontKitEdits = [.. FrontKitModules.Select(_ => new List<InstanceValue>())];

    /// <summary>How far a body moves back from its footprint's front, how deep its front yard is, and its street side's draw.</summary>
    private readonly record struct FrontSetback(float Recess, float Yard, bool Dwelling, bool Shop, uint Street);

    private void CreateFrontYardLayers()
    {
        _frontGround = Layer(Colors.White, Vector3.One, perInstance: true, casts: false);
        var (_, middle, _) = TreeMeshes();
        _frontTrees = Layer(Colors.White, middle, perInstance: true);
        for (int i = 0; i < FurnitureModules.Length; i++)
            _furniture[i] = Layer(Colors.White, Commit(Load($"forecourt/{FurnitureModules[i]}", FurniturePainted).Corners), perInstance: true);
        for (int i = 0; i < FrontKitModules.Length; i++)
            _frontKit[i] = Layer(Colors.White, Commit(Load($"front-yard/{FrontKitModules[i]}", FrontKitPainted).Corners), perInstance: true);

        foreach (InstanceLayer layer in (InstanceLayer[])[_frontGround, _frontTrees, .. _furniture, .. _frontKit])
        {
            layer.Multimesh.NearOnly = true;
            BodyResidency(layer);
        }
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> FrontYardLayers()
    {
        yield return ("front-yard-ground", _frontGround, true, null);
        yield return ("front-yard-tree", _frontTrees, true, null);
        for (int i = 0; i < FurnitureModules.Length; i++) yield return ($"forecourt-{FurnitureModules[i]}", _furniture[i], true, null);
        for (int i = 0; i < FrontKitModules.Length; i++) yield return ($"front-yard-{FrontKitModules[i]}", _frontKit[i], true, null);
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
        MansionYard? estate = FamilyOf(building).Family?.Body?.House?.Yard;
        return FrontSetbackOf(lot, kind == "dwelling" && !shop, shop, depth, estate, _world.Buildings.Rows.IdAt(building));
    }

    private static bool DrawsShops(FamilyBody? body) =>
        body is not null && (body.Shopfront || body.Shop is not null || body.Midrise is { Shops: true });

    /// <returns>No setback where the Lot has no Street address.</returns>
    /// <param name="estate">A mansion's yard, which it draws whole: deep while that leaves the house its least depth, otherwise shallow.</param>
    private FrontSetback FrontSetbackOf(int lot, bool dwelling, bool shop, float depth, MansionYard? estate = null, ulong id = 0)
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
        if (dwelling && estate is { } yard)
        {
            uint h = (uint)id * 0x9E3779B1u;
            h ^= h >> 15;
            float room = depth - yard.LeastHouse + gap;
            (float low, float high) = room >= yard.Deep.Low ? yard.Deep : yard.Shallow;
            float reach = Mathf.Max(gap, Mathf.Min(low + ((high - low) * (h % 9 / 8f)), room));
            float back = Mathf.Floor((reach - gap) / FrontStepMetres) * FrontStepMetres;
            return new FrontSetback(back, back + gap, dwelling, shop, mix);
        }

        float recess = Mathf.Clamp(target - gap, 0f, depth * FrontRecessShare);
        recess = Mathf.Floor(recess / FrontStepMetres) * FrontStepMetres;
        return new FrontSetback(recess, Mathf.Min(recess + gap, FrontYardMostMetres), dwelling, shop, mix);
    }

    /// <summary>Draws a Building's front yard in front of its massing box, which already stands back by the setback.</summary>
    private void PlaceFrontYard(int building, Massing one)
    {
        ulong id = one.Id;
        _frontGroundEdits.Clear();
        _frontTreeEdits.Clear();
        foreach (List<InstanceValue> edits in _furnitureEdits) edits.Clear();
        foreach (List<InstanceValue> edits in _frontKitEdits) edits.Clear();
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
            List<FamilyDoor> doors = DoorsOf(id, w, near);
            int storeys = Mathf.Max(1, Mathf.RoundToInt(size.Y / StoreyMetres));

            if (setback.Dwelling && MansionOf(building, id, frontage, depth, storeys) is { } estate)
            {
                List<FamilyDoor> all = _placedBodies.TryGetValue(id, out PlacedBody placed)
                    ? [.. placed.NearShape.Doors.Where(d => MathF.Abs(d.Across) <= w)]
                    : [];
                DressMansion(draw, w, near, far, id, all, estate.Plan, estate.Grounds, estate.Units);
            }
            else if (!setback.Dwelling || setback.Yard < HedgedYardLeastMetres)
            {
                _frontGroundEdits.Add(draw.Box(-w, w, near, far, 0f, .1f, stone));
                if (setback.Shop) DressForecourt(draw, w, near, far, id, doors);
            }
            else
            {
                _frontGroundEdits.Add(draw.Box(-w, w, near, far, 0f, .06f, LawnTones[pick % LawnTones.Length]));
                DressGarden(draw, w, near, far, id, setback.Street, doors, Mathf.RoundToInt(size.Y / StoreyMetres));
            }
            if (_reportFamilyBodies) GD.Print($"front_yard\tbuilding {id}\tsetback {setback.Recess}\tyard {setback.Yard}\t{(setback.Dwelling ? "garden" : "paved")}\tdoors {doors.Count}");
        }

        _frontGround.Multimesh.Replace(id, _frontGroundEdits);
        _frontTrees.Multimesh.Replace(id, _frontTreeEdits);
        for (int i = 0; i < _furniture.Length; i++) _furniture[i].Multimesh.Replace(id, _furnitureEdits[i]);
        for (int i = 0; i < _frontKit.Length; i++) _frontKit[i].Multimesh.Replace(id, _frontKitEdits[i]);
        if (_frontGroundEdits.Count > 0) _frontYardIds.Add(id);
        else _frontYardIds.Remove(id);
    }

    /// <summary>A mansion Building's plan, grounds and Units; its plan is <c>null</c> where it draws a massing box.</summary>
    private (MansionPlan? Plan, MansionGrounds Grounds, int Units)? MansionOf(int building, ulong id, float frontage, float depth, int storeys)
    {
        if (FamilyOf(building).Family?.Body is not { House: { } home } || !FamilyBodyBuilder.IsMansion(home.Variant)) return null;
        int units = BuildingFacts.TryOf(_world, _names, building, out BuildingFacts facts) ? facts.Units : 1;
        MansionGrounds grounds = units > 1 ? MansionGrounds.Apartments : MansionGrounds.OneHousehold;
        MansionPlan? plan = _placedBodies.ContainsKey(id)
            ? FamilyBodyBuilder.PlanMansion(home.Variant, frontage, depth, storeys, FamilyPicker.House(_world.Key, id), grounds)
            : null;
        return (plan, grounds, units);
    }

    // A door further behind the body's front than this opens onto a court of its own, not the yard.
    private const float DoorReachMetres = 4f;

    /// <summary>The street doors of a Building's body that open onto its front yard, west to east; none where it draws no body.</summary>
    private List<FamilyDoor> DoorsOf(ulong id, float w, float near) =>
        _placedBodies.TryGetValue(id, out PlacedBody body)
            ? [.. body.NearShape.Doors.Where(d => MathF.Abs(d.Across) <= w && d.Forward > near - DoorReachMetres)]
            : [];

    private enum FrontBoundary { Hedge, Wall, Railing, Picket }

    private static readonly FrontBoundary[] AttachedBoundaries =
    [
        FrontBoundary.Railing, FrontBoundary.Railing, FrontBoundary.Railing, FrontBoundary.Wall, FrontBoundary.Wall, FrontBoundary.Wall,
        FrontBoundary.Hedge, FrontBoundary.Hedge, FrontBoundary.Hedge, FrontBoundary.Picket,
    ];

    private static readonly FrontBoundary[] DetachedBoundaries =
    [
        FrontBoundary.Hedge, FrontBoundary.Hedge, FrontBoundary.Hedge, FrontBoundary.Hedge, FrontBoundary.Picket, FrontBoundary.Picket, FrontBoundary.Picket,
        FrontBoundary.Wall, FrontBoundary.Wall, FrontBoundary.Railing,
    ];

    private static readonly Color[] BrickTones = [Hex(0x9a4f38), Hex(0x7a4a36), Hex(0xb0623f), Hex(0xc9a46a), Hex(0xb89a72), Hex(0xd8d0bc)];
    private static readonly Color[] GateTones = [Hex(0x1e2123), Hex(0x1e2123), Hex(0x2f4a3c), Hex(0x24303a), Hex(0xe8e4da)];
    private static readonly Color[] PicketTones = [Hex(0xe8e4da), Hex(0xe8e4da), Hex(0xd6cfbf), Hex(0x9aa58a), Hex(0x8d9194)];

    private const float FrontBayMetres = 2f;

    // A picket fence reads as a cottage garden, so taller blocks take railings instead.
    private const int PicketMostStoreys = 2;
    private const float PierHalfMetres = .3f;
    private const float GateLift = .08f;

    // The garden kit is authored 1.2 times real size to match the bodies; topiary comes from the forecourt kit at real size.
    private const float GardenTopiaryScale = 1.2f;

    // Below this sill a body's door opens straight onto the ground, so it takes no step.
    private const float DoorstepLeastSill = .2f;

    /// <summary>
    /// A dwelling's front garden. Each street side shares one boundary, a hedge, a low wall, railings on
    /// a dwarf wall or a picket fence, and one house in four picks its own. Every street door gets a gate
    /// in the boundary and a path straight to its step, so paths line up with the doors the body draws.
    /// A massing box gets gates every <see cref="GateEveryMetres"/>, and a body without street doors gets
    /// an unbroken boundary. Attached houses draw the
    /// boundary down their west party line too, and deep gardens plant trees between the paths.
    /// </summary>
    private void DressGarden(YardDraw draw, float w, float near, float far, ulong id, uint street, List<FamilyDoor> doors, int storeys)
    {
        uint h = (uint)id * 0x9E3779B1u;
        h ^= h >> 13;
        bool left = _bodyNeighbours.TryGetValue(id, out BodyNeighbours neighbours) && neighbours.LeftId != 0;
        bool right = neighbours.RightId != 0;
        uint style = h % 4 == 0 ? h >> 4 : street >> 3;
        FrontBoundary boundary = (left || right ? AttachedBoundaries : DetachedBoundaries)[style % 10];
        if (boundary == FrontBoundary.Picket && storeys > PicketMostStoreys) boundary = FrontBoundary.Railing;
        bool piers = boundary is FrontBoundary.Wall or FrontBoundary.Railing;
        FrontKit run = boundary switch
        {
            FrontBoundary.Wall => FrontKit.Wall,
            FrontBoundary.Railing => FrontKit.Railing,
            FrontBoundary.Picket => FrontKit.Picket,
            _ => FrontKit.Hedge,
        };
        float half = boundary switch { FrontBoundary.Hedge => .42f, FrontBoundary.Picket => .07f, _ => .22f };
        float line = far - .05f - half;
        float shade = .8f + ((h >> 7) % 5 * .05f);
        Color paint = boundary switch
        {
            FrontBoundary.Hedge => new Color(shade, shade, shade),
            FrontBoundary.Picket => PicketTones[(style >> 4) % (uint)PicketTones.Length],
            _ => BrickTones[(style >> 4) % (uint)BrickTones.Length],
        };
        FrontKit? gate = boundary switch
        {
            FrontBoundary.Picket => FrontKit.GateTimber,
            FrontBoundary.Hedge => (h >> 5) % 2 == 0 ? FrontKit.GateTimber : null,
            _ => FrontKit.GateIron,
        };
        Color gatePaint = boundary == FrontBoundary.Picket ? paint : GateTones[(h >> 9) % (uint)GateTones.Length];
        FrontKit path = piers && (style >> 8) % 2 == 0 ? FrontKit.PathTiled : FrontKit.PathFlags;
        Color flag = StoneTones[(h >> 11) % (uint)StoneTones.Length];
        bool topiary = (h >> 15) % 4 == 0;
        bool border = boundary != FrontBoundary.Picket && (h >> 17) % 2 == 0;
        Color bloom = BloomTones[(h >> 19) % (uint)BloomTones.Length];

        if (doors.Count == 0 && !_placedBodies.ContainsKey(id))
        {
            int count = Math.Max(1, Mathf.RoundToInt(w * 2f / GateEveryMetres));
            for (int k = 0; k < count; k++) doors.Add(new FamilyDoor(-w + (w * 2f * (k + .5f) / count), near, 1f, 0f));
        }

        var gaps = new List<(float From, float To)>();
        foreach (FamilyDoor door in doors)
        {
            float wide = Mathf.Clamp(door.Width, 1f, 2.4f);
            float open = Mathf.Max(wide * .5f + .06f, .6f);
            float reach = open + (piers ? (PierHalfMetres * 2f) - .05f : .05f);
            if (gaps.Count > 0 && door.Across - reach < gaps[^1].To + .3f)
            {
                gaps[^1] = (gaps[^1].From, door.Across + reach);
            }
            else
            {
                gaps.Add((door.Across - reach, door.Across + reach));
            }

            if (gate is { } leaf && wide <= 1.2f) Kit(leaf, draw.Module(door.Across, line, GateLift, 0f, gatePaint));
            if (piers)
            {
                for (int side = -1; side <= 1; side += 2)
                    Kit(FrontKit.Pier, draw.Module(door.Across + (side * (open + PierHalfMetres - .03f)), line, 0f, 0f, paint));
            }

            float from = door.Forward;
            if (door.Sill >= DoorstepLeastSill)
            {
                float step = Mathf.Max(1f, (wide + .4f) / 1.4f);
                Kit(FrontKit.Doorstep, draw.Module(door.Across, door.Forward + .3f, 0f, 0f, Colors.White, step));
                from += .6f;
                if (topiary)
                {
                    for (int side = -1; side <= 1; side += 2)
                        Furnish(Furniture.Topiary, draw.Module(door.Across + (side * ((step * .7f) + .42f)), door.Forward + .42f, 0f, 0f, Colors.White,
                            GardenTopiaryScale, GardenTopiaryScale, GardenTopiaryScale));
                }
            }

            int bays = Math.Max(1, Mathf.RoundToInt(far - from));
            float bay = (far - from) / bays;
            for (int k = 0; k < bays && bay > .1f; k++)
                Kit(path, draw.Module(door.Across, from + (bay * (k + .5f)), 0f, 0f, flag, wide, bay));
        }

        float start = -w;
        if (piers && !left)
        {
            Kit(FrontKit.Pier, draw.Module(-w + PierHalfMetres, line, 0f, 0f, paint));
            start += PierHalfMetres * 2f;
        }
        float end = w;
        if (piers && !right)
        {
            Kit(FrontKit.Pier, draw.Module(w - PierHalfMetres, line, 0f, 0f, paint));
            end -= PierHalfMetres * 2f;
        }

        float at = start;
        foreach ((float from, float to) in gaps)
        {
            BoundaryRun(draw, run, at, from, line, paint, sideways: false);
            if (border) BoundaryRun(draw, FrontKit.Border, at + .3f, from - .15f, line - half - .42f, bloom, sideways: false);
            at = to;
        }
        BoundaryRun(draw, run, at, end, line, paint, sideways: false);
        if (border) BoundaryRun(draw, FrontKit.Border, at + .15f, end - .3f, line - half - .42f, bloom, sideways: false);

        if (left) BoundaryRun(draw, run, near + .1f, line - half, -w + half, paint, sideways: true);

        if (far - near >= YardTreeLeastMetres)
        {
            float middle = (near + line - half) * .5f;
            float prior = -w;
            for (int k = 0; k <= gaps.Count; k++)
            {
                float next = k < gaps.Count ? gaps[k].From : w;
                if (next - prior >= 5f)
                {
                    float tone = .88f + ((h + (uint)k) % 7 * .04f);
                    _frontTreeEdits.Add(draw.Tree((prior + next) * .5f, middle, 5.5f + ((h + (uint)k) % 3), new Color(tone, tone, tone)));
                }
                if (k < gaps.Count) prior = gaps[k].To;
            }
        }
    }

    private void Kit(FrontKit module, InstanceValue placed) => _frontKitEdits[(int)module].Add(placed);

    /// <summary>Bays of a 2 m boundary module from one point to another, each stretched to fit; sideways runs toward the Street.</summary>
    private void BoundaryRun(YardDraw draw, FrontKit module, float from, float to, float line, Color paint, bool sideways)
    {
        float length = to - from;
        if (length < .4f) return;
        int bays = Math.Max(1, Mathf.RoundToInt(length / FrontBayMetres));
        float bay = length / bays;
        for (int k = 0; k < bays; k++)
        {
            float along = from + (bay * (k + .5f));
            Kit(module, sideways
                ? draw.Module(line, along, 0f, Mathf.Pi * .5f, paint, bay / FrontBayMetres)
                : draw.Module(along, line, 0f, 0f, paint, bay / FrontBayMetres));
        }
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

    // Screen centers stay this far from each door.
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
    /// or not, and from 2.4 m shade them with parasols. Screens leave each door clear. Some shops also put
    /// out an A-board, and some flank the door nearest their middle with potted topiaries.
    /// </summary>
    /// <param name="doors">The body's street doors; none means one door mid-frontage.</param>
    private void DressForecourt(YardDraw draw, float w, float near, float far, ulong id, List<FamilyDoor> doors)
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

        List<float> entrances = doors.Count > 0 ? [.. doors.Select(d => d.Across)] : [0f];
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
                        foreach (float door in entrances)
                        {
                            if (Mathf.Abs(c - door) < DoorGapMetres) c = door + (c < door ? -DoorGapMetres : DoorGapMetres);
                        }

                        bool clear = !entrances.Exists(door => Mathf.Abs(c - door) < DoorGapMetres - .01f);
                        if (clear && Mathf.Abs(c - middle) <= reach) Furnish(Furniture.CafeScreen, draw.Module(c, far - .25f, PavingTop, 0f, canvas));
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
            float door = entrances.MinBy(MathF.Abs);
            for (int side = -1; side <= 1; side += 2)
                Furnish(Furniture.Topiary, draw.Module(Mathf.Clamp(door + (side * 1.3f), -w + .4f, w - .4f), near + .4f, PavingTop, 0f, Colors.White));
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
        _frontTrees.Multimesh.Replace(id, []);
        foreach (InstanceLayer layer in _furniture) layer.Multimesh.Replace(id, []);
        foreach (InstanceLayer layer in _frontKit) layer.Multimesh.Replace(id, []);
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

        /// <summary>
        /// A whole module, its street side toward the Street, turned about the upright by <paramref name="turn"/>
        /// and stretched by <paramref name="wide"/> along its own X and <paramref name="deep"/> along its depth.
        /// </summary>
        public InstanceValue Module(float u, float s, float y, float turn, Color paint, float wide = 1f, float deep = 1f, float tall = 1f)
        {
            (float sin, float cos) = MathF.SinCos(turn);
            Vector3 across = (Across * cos) + (Forward * sin);
            Vector3 forward = (Forward * cos) - (Across * sin);
            Vector3 at = Origin + (Across * u) + (Forward * s) + (Vector3.Up * y);
            return new InstanceValue(new Transform3D(new Basis(across * wide, Vector3.Up * tall, forward * deep), at), paint);
        }

        public InstanceValue Tree(float u, float s, float tall, Color shade)
        {
            float broad = tall * .6f;
            Vector3 at = Origin + (Across * u) + (Forward * s) + (Vector3.Up * .05f);
            return new InstanceValue(new Transform3D(Basis.FromScale(new Vector3(broad, tall, broad)), at), shade);
        }
    }
}
