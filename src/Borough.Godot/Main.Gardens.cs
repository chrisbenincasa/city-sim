using System;
using System.Collections.Generic;
using Q16 = Borough.Core.Arithmetic.Fixed;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws the ground behind houses and the middle of each housing block as gardens. A block draws
/// either private gardens that run to its centre line or shorter gardens round a shared green. The
/// city holds none of this; the shell invents it from each block's Lots, so it moves no State Hash.
/// Furniture comes from the Blender modules in <c>assets/garden</c>, which
/// <c>scripts/art/garden.py</c> authors.
/// </summary>
public partial class Main
{
    private const int GreenPercent = 40;
    private const float GreenMargin = 10f;
    private const float GreenLeast = 36f;
    private const float FencePanel = 2f;

    // A 1 m fence is under a pixel beyond this, and a whole city of them costs over 100 ms of GPU time a frame.
    private const float GardenPropDistance = 2000f;

    private enum GardenLayer
    {
        Ground, Hedge, Tree, Shed, Greenhouse, VegBed, Table, Bench, Play, Flowerbed, Fence, Railing,
    }

    private static readonly string[] GardenModules =
        ["shed", "greenhouse", "veg-bed", "table", "bench", "play", "flowerbed", "fence-panel", "railing-panel"];

    private static readonly Color[] LawnTones =
        [Hex(0x4c7a34), Hex(0x557f3a), Hex(0x5f8a40), Hex(0x4a7032), Hex(0x6b8a45), Hex(0x7a8a4a)];

    private static readonly Color[] StoneTones = [Hex(0xb8b2a6), Hex(0xa8a196), Hex(0xc4bdb0), Hex(0x9d988e)];
    private static readonly Color HedgeTone = Hex(0x35512b);
    private static readonly Color WallTone = Hex(0x8e5a40);
    private static readonly Color GreenLawn = Hex(0x6a9a48);
    private static readonly Color PlaySurface = Hex(0xa0523a);

    private readonly InstanceLayer[] _gardenLayers = new InstanceLayer[12];
    private readonly List<ulong>[] _gardenIds = new List<ulong>[12];
    private readonly List<(ulong Id, Transform3D Where, Color What)>[] _gardenPieces = new List<(ulong, Transform3D, Color)>[12];
    private readonly List<Transform3D> _gardenGround = [];

    private enum House { South, North, West, East }

    private readonly record struct Area(float West, float South, float East, float North)
    {
        public float Wide => East - West;
        public float Deep => North - South;
        public bool Usable => Wide >= 1f && Deep >= 1f;
    }

    private readonly record struct GardenLot(int Slot, ulong Id, BlockFace Face, Area Parcel, Area Footprint, bool Occupied);

    private static Color Hex(int rgb) =>
        new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f).SrgbToLinear();

    private void CreateGardenLayers()
    {
        _gardenLayers[(int)GardenLayer.Ground] = Layer(Colors.White, Vector3.One, perInstance: true, casts: false);
        _gardenLayers[(int)GardenLayer.Hedge] = Layer(Colors.White, Vector3.One, perInstance: true);
        var (near, middle, far) = TreeMeshes();
        _gardenLayers[(int)GardenLayer.Tree] = Layer(Colors.White, far, perInstance: true);
        near.SurfaceSetMaterial(0, far.SurfaceGetMaterial(0));
        middle.SurfaceSetMaterial(0, far.SurfaceGetMaterial(0));
        _gardenLayers[(int)GardenLayer.Tree].DetailDistance = 4000f;
        _gardenLayers[(int)GardenLayer.Tree].Gated = true;
        _gardenLayers[(int)GardenLayer.Tree].Details = [(400f, near), (1500f, middle)];

        for (int i = 0; i < GardenModules.Length; i++)
        {
            _gardenLayers[(int)GardenLayer.Shed + i] = Layer(Colors.White, Commit(Load($"garden/{GardenModules[i]}").Corners), perInstance: true);
            _gardenLayers[(int)GardenLayer.Shed + i].DetailDistance = GardenPropDistance;
            _gardenLayers[(int)GardenLayer.Shed + i].Gated = true;
        }

        for (int i = 0; i < _gardenIds.Length; i++)
        {
            _gardenIds[i] = [];
            _gardenPieces[i] = [];
        }
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> GardenLayers()
    {
        yield return ("garden-ground", _gardenLayers[(int)GardenLayer.Ground], true, _gardenIds[(int)GardenLayer.Ground]);
        yield return ("garden-hedge", _gardenLayers[(int)GardenLayer.Hedge], true, _gardenIds[(int)GardenLayer.Hedge]);
        yield return ("garden-tree", _gardenLayers[(int)GardenLayer.Tree], true, _gardenIds[(int)GardenLayer.Tree]);
        for (int i = 0; i < GardenModules.Length; i++)
        {
            int layer = (int)GardenLayer.Shed + i;
            yield return ($"garden-{GardenModules[i]}", _gardenLayers[layer], true, _gardenIds[layer]);
        }
    }

    /// <summary>Whether a Lot's back ground is drawn as a garden, which replaces its outbuilding.</summary>
    private bool Gardened(int lot) => GardenLotAt(lot) is not null;

    private GardenLot? GardenLotAt(int slot)
    {
        LotTable lots = _world.Lots;
        if (!lots.Rows.IsLive(slot)) return null;
        if (lots.PatternOf(slot) is not (BlockPattern.Detached or BlockPattern.Perimeter or BlockPattern.Courtyard)) return null;
        OrientedRectangle parcel = lots.Parcel(slot);
        if (parcel.AxisEastQ16 != Q16.One || parcel.AxisNorthQ16 != 0) return null;
        if (!Frontage.BlockOf(_world.Roads.Streets, lots.East[slot], lots.North[slot], (StreetSide)lots.Side[slot],
            out _, out _, out BlockFace face)) return null;

        return new GardenLot(slot, lots.Rows.IdAt(slot), face, Metres(ParcelFrame(slot)), Metres(FootprintFrame(slot)),
            !lots.IsVacant(slot));
    }

    private static Area Metres(LandRectangle tiles) =>
        new(tiles.X * MetresPerTile, tiles.Y * MetresPerTile,
            (tiles.X + tiles.Width) * MetresPerTile, (tiles.Y + tiles.Height) * MetresPerTile);

    /// <summary>Works out every garden and green, and the ground they clear of wild trees.</summary>
    private void PlanGardens()
    {
        foreach (var pieces in _gardenPieces) pieces.Clear();
        _gardenGround.Clear();

        var blocks = new SortedDictionary<(int Column, int Row), List<GardenLot>>();
        LotTable lots = _world.Lots;
        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (GardenLotAt(slot) is not { } lot) continue;
            Frontage.BlockOf(_world.Roads.Streets, lots.East[slot], lots.North[slot], (StreetSide)lots.Side[slot],
                out int column, out int row);
            if (!blocks.TryGetValue((column, row), out var members)) blocks[(column, row)] = members = [];
            members.Add(lot);
        }

        var fences = new HashSet<(int, int, int, int)>();
        foreach (var ((column, row), members) in blocks)
        {
            if (ZoneInterior(column, row) is not { } land) continue;
            PlanBlock(column, row, BlockLand(land), members, fences);
        }
    }

    private void FillGardens()
    {
        for (int i = 0; i < _gardenLayers.Length; i++)
        {
            Fill(_gardenLayers[i], _gardenPieces[i], _gardenIds[i]);
        }
    }

    /// <summary>Writes again each gated layer whose camera has moved beyond what it was built for.</summary>
    private void Regate()
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        bool wrote = false;
        Vector3 eye = _camera.GlobalPosition;
        if (_trees.Stale(eye) || _rocks.Stale(eye))
        {
            Scatter();
            wrote = true;
        }
        if (_kerbs.Stale(eye))
        {
            Fill(_kerbs, Kerbs(), _kerbIds);
            wrote = true;
        }
        for (int i = 0; i < _gardenLayers.Length; i++)
        {
            if (!_gardenLayers[i].Stale(eye)) continue;
            Fill(_gardenLayers[i], _gardenPieces[i], _gardenIds[i]);
            wrote = true;
        }
        if (!wrote) return;
        double elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _regates++;
        _regateMilliseconds += elapsed;
        _regateMaximum = Math.Max(_regateMaximum, elapsed);
    }

    private static Area BlockLand(Transform3D interior)
    {
        Vector3 half = interior.Basis.Scale * .5f;
        return new Area(interior.Origin.X - half.X, -interior.Origin.Z - half.Z,
            interior.Origin.X + half.X, -interior.Origin.Z + half.Z);
    }

    private void PlanBlock(int column, int row, Area land, List<GardenLot> members, HashSet<(int, int, int, int)> fences)
    {
        float west = land.West, east = land.East, south = land.South, north = land.North;
        bool anyWest = false, anyEast = false, anySouth = false, anyNorth = false;
        foreach (GardenLot lot in members)
        {
            switch (lot.Face)
            {
                case BlockFace.South: south = Math.Max(south, lot.Parcel.North); anySouth = true; break;
                case BlockFace.North: north = Math.Min(north, lot.Parcel.South); anyNorth = true; break;
                case BlockFace.West: west = Math.Max(west, lot.Parcel.East); anyWest = true; break;
                case BlockFace.East: east = Math.Min(east, lot.Parcel.West); anyEast = true; break;
            }
        }

        var interior = new Area(west, south, east, north);
        bool open = interior.Usable;
        ulong blockKey = (1UL << 63) | ((ulong)(uint)column << 32) | (uint)row;
        ulong draw = Randomness.Draw(_world.Key, blockKey, Ticks.Zero, PurposeTag.AppearanceInterior);
        bool lived = members.Exists(m => m.Occupied);
        bool green = lived && open && interior.Wide >= GreenLeast && interior.Deep >= GreenLeast && (int)(draw % 100) < GreenPercent;
        var shared = green
            ? new Area(west + GreenMargin, south + GreenMargin, east - GreenMargin, north - GreenMargin)
            : default;

        float middleX = (west + east) * .5f, middleY = (south + north) * .5f;
        float band = interior.Wide * .25f;
        bool across = anySouth || anyNorth;
        float bandWest = !anyWest ? west : across ? middleX - band : anyEast ? middleX : east;
        float bandEast = !anyEast ? east : across ? middleX + band : anyWest ? middleX : west;
        float reachSouth = anyNorth ? middleY : north;
        float reachNorth = anySouth ? middleY : south;

        foreach (GardenLot lot in members)
        {
            if (!lot.Occupied) continue;
            var draws = new GardenDraw(Randomness.Draw(_world.Key, lot.Id, Ticks.Zero, PurposeTag.AppearanceGarden));
            Area p = lot.Parcel, f = lot.Footprint;
            (Area back, Area share, House house) = lot.Face switch
            {
                BlockFace.South => (new Area(p.West, f.North, p.East, open ? south : p.North),
                    !open ? default : green
                        ? new Area(Math.Max(p.West, west), south, Math.Min(p.East, east), shared.South)
                        : new Area(Math.Max(p.West, bandWest), south, Math.Min(p.East, bandEast), reachSouth),
                    House.South),
                BlockFace.North => (new Area(p.West, open ? north : p.South, p.East, f.South),
                    !open ? default : green
                        ? new Area(Math.Max(p.West, west), shared.North, Math.Min(p.East, east), north)
                        : new Area(Math.Max(p.West, bandWest), reachNorth, Math.Min(p.East, bandEast), north),
                    House.North),
                BlockFace.West => (new Area(f.East, p.South, open ? west : p.East, p.North),
                    !open ? default : green
                        ? new Area(west, Math.Max(p.South, shared.South), shared.West, Math.Min(p.North, shared.North))
                        : new Area(west, Math.Max(p.South, south), bandWest, Math.Min(p.North, north)),
                    House.West),
                _ => (new Area(open ? east : p.West, p.South, f.West, p.North),
                    !open ? default : green
                        ? new Area(shared.East, Math.Max(p.South, shared.South), east, Math.Min(p.North, shared.North))
                        : new Area(bandEast, Math.Max(p.South, south), east, Math.Min(p.North, north)),
                    House.East),
            };

            bool facing = house is House.South or House.North;
            bool whole = share.Usable && (facing
                ? share.West <= back.West + .01f && share.East >= back.East - .01f
                : share.South <= back.South + .01f && share.North >= back.North - .01f);
            if (whole)
            {
                Garden(lot.Id, Join(back, share), house, draws, fences, farFence: !green);
                continue;
            }

            Garden(lot.Id, back, house, draws, fences);
            if (share.Usable) Garden(lot.Id, share, house, draws, fences, farFence: !green);
        }

        if (green && shared.Usable) Green(blockKey, shared, new GardenDraw(draw), fences);
    }

    private static Area Join(Area a, Area b) =>
        new(Math.Min(a.West, b.West), Math.Min(a.South, b.South), Math.Max(a.East, b.East), Math.Max(a.North, b.North));

    private void Put(GardenLayer layer, ulong id, Transform3D where, Color paint) =>
        _gardenPieces[(int)layer].Add((id, where, paint));

    private void Slab(GardenLayer layer, ulong id, Area area, float bottom, float top, Color paint)
    {
        if (area.Wide <= 0f || area.Deep <= 0f) return;
        Put(layer, id, new Transform3D(Basis.FromScale(new Vector3(area.Wide, top - bottom, area.Deep)),
            new Vector3((area.West + area.East) * .5f, (bottom + top) * .5f, -(area.South + area.North) * .5f)), paint);
    }

    private void Module(GardenLayer layer, ulong id, float east, float north, float turn) =>
        Put(layer, id, new Transform3D(YawScaled(turn, Vector3.One), new Vector3(east, 0f, -north)), Colors.White);

    private static float Facing(House house) => house switch
    {
        House.South => 0f,
        House.North => Mathf.Pi,
        House.East => Mathf.Pi / 2f,
        _ => -Mathf.Pi / 2f,
    };

    /// <summary>A point <paramref name="d"/> metres from the house edge and <paramref name="u"/> across.</summary>
    private static (float East, float North) Toward(Area g, House house, float d, float u) => house switch
    {
        House.South => (g.West + u, g.South + d),
        House.North => (g.West + u, g.North - d),
        House.West => (g.West + d, g.South + u),
        _ => (g.East - d, g.South + u),
    };

    private static Area Band(Area g, House house, float d0, float d1, float u0, float u1)
    {
        var (ax, ay) = Toward(g, house, d0, u0);
        var (bx, by) = Toward(g, house, d1, u1);
        return new Area(Math.Min(ax, bx), Math.Min(ay, by), Math.Max(ax, bx), Math.Max(ay, by));
    }

    // A garden backing onto a shared green leaves its far edge to the green's railing.
    private void Garden(ulong id, Area g, House house, GardenDraw r, HashSet<(int, int, int, int)> fences, bool farFence = true)
    {
        if (!g.Usable) return;
        _gardenGround.Add(new Transform3D(Basis.FromScale(new Vector3(g.Wide, 1f, g.Deep)),
            new Vector3((g.West + g.East) * .5f, 0f, -(g.South + g.North) * .5f)));

        bool across = house is House.South or House.North;
        float depth = across ? g.Deep : g.Wide;
        float width = across ? g.Wide : g.Deep;
        float turn = Facing(house);

        Slab(GardenLayer.Ground, id, new Area(g.West + .1f, g.South + .1f, g.East - .1f, g.North - .1f), 0f, .06f, r.Pick(LawnTones));
        Color stone = r.Pick(StoneTones);
        float patio = Math.Min(3.5f, depth * .4f);
        Slab(GardenLayer.Ground, id, Band(g, house, 0f, patio, .3f, width - .3f), 0f, .09f, stone);

        if (depth > 8f)
        {
            float u = r.Range(1f, Math.Max(1.1f, width - 2f));
            Slab(GardenLayer.Ground, id, Band(g, house, patio, depth - 2.5f, u, u + .9f), 0f, .08f, stone);
        }

        if (depth > 9f && width >= 3.6f && r.Chance(.8f))
        {
            var (x, y) = Toward(g, house, depth - 2f, r.Range(1.8f, Math.Max(1.9f, width - 1.8f)));
            Module(GardenLayer.Shed, id, x, y, turn);
        }

        if (depth > 14f && width >= 4.8f && r.Chance(.45f))
        {
            float d0 = r.Range(patio + 2f, depth - 8f);
            float u0 = r.Range(2.4f, Math.Max(2.5f, width - 2.4f));
            int beds = 2 + r.Below(3);
            for (int k = 0; k < beds; k++)
            {
                var (x, y) = Toward(g, house, d0 + (k * 1.6f) + .5f, u0);
                Module(GardenLayer.VegBed, id, x, y, turn);
            }
        }

        if (depth > 16f && width >= 4.4f && r.Chance(.25f))
        {
            var (x, y) = Toward(g, house, r.Range(patio + 3f, depth - 7f), width - 2.2f);
            Module(GardenLayer.Greenhouse, id, x, y, turn);
        }

        if (depth > 20f && r.Chance(.5f))
        {
            float d0 = r.Range(depth * .45f, depth - 6f);
            Slab(GardenLayer.Hedge, id, Band(g, house, d0, d0 + .5f, .4f, width - .4f), 0f, 1.5f, HedgeTone);
        }

        int trees = depth > 10f ? r.Below(3) + (int)(depth * width / r.Range(90f, 160f)) : r.Below(4) == 0 ? 1 : 0;
        for (int k = 0; k < trees; k++)
        {
            var (x, y) = Toward(g, house, r.Range(Math.Min(depth * .5f, depth - 2f), Math.Max(depth * .5f, depth - 2f)),
                width < 4f ? width * .5f : r.Range(2f, width - 2f));
            Tree(id, x, y, depth < 12f ? .8f : 1f, r);
        }

        if (r.Chance(.3f))
        {
            var (x, y) = Toward(g, house, patio * .5f + .3f, width * .5f);
            Module(GardenLayer.Table, id, x, y, turn);
        }

        int kind = r.Below(8);
        var (ax0, ay0) = Toward(g, house, 0f, 0f);
        var (ax1, ay1) = Toward(g, house, depth, 0f);
        var (bx0, by0) = Toward(g, house, 0f, width);
        var (bx1, by1) = Toward(g, house, depth, width);
        Boundary(id, ax0, ay0, ax1, ay1, kind, fences);
        Boundary(id, bx0, by0, bx1, by1, kind, fences);
        if (farFence) Boundary(id, ax1, ay1, bx1, by1, kind, fences);
    }

    private void Tree(ulong id, float east, float north, float scale, GardenDraw r)
    {
        float tall = (6f + r.Range(0f, 4f)) * scale;
        float broad = tall * r.Range(.5f, .7f);
        float shade = .88f + r.Range(0f, .24f);
        Put(GardenLayer.Tree, id, new Transform3D(
            YawScaled(r.Range(0f, Mathf.Tau), new Vector3(broad, tall, broad)),
            new Vector3(east, .05f, -north)), new Color(shade, shade, shade));
    }

    /// <summary>A boundary between two gardens: timber fence, hedge or wall. A shared edge is drawn once.</summary>
    private void Boundary(ulong id, float x0, float y0, float x1, float y1, int kind, HashSet<(int, int, int, int)> fences)
    {
        float length = MathF.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));
        if (length < .5f) return;
        var key = x0 < x1 || (x0 == x1 && y0 <= y1)
            ? ((int)MathF.Round(x0 * 4f), (int)MathF.Round(y0 * 4f), (int)MathF.Round(x1 * 4f), (int)MathF.Round(y1 * 4f))
            : ((int)MathF.Round(x1 * 4f), (int)MathF.Round(y1 * 4f), (int)MathF.Round(x0 * 4f), (int)MathF.Round(y0 * 4f));
        if (!fences.Add(key)) return;

        bool eastward = MathF.Abs(x1 - x0) >= MathF.Abs(y1 - y0);
        if (kind < 5)
        {
            int panels = Math.Max(1, (int)MathF.Ceiling(length / FencePanel));
            float stretch = length / (panels * FencePanel);
            Basis turn = YawScaled(eastward ? 0f : Mathf.Pi / 2f, new Vector3(stretch, 1f, 1f));
            for (int k = 0; k < panels; k++)
            {
                float t = (k + .5f) / panels;
                Put(GardenLayer.Fence, id, new Transform3D(turn, new Vector3(x0 + ((x1 - x0) * t), 0f, -(y0 + ((y1 - y0) * t)))), Colors.White);
            }
            return;
        }

        float thick = kind < 7 ? .6f : .25f;
        float high = kind < 7 ? 1.5f : 1.4f;
        var area = eastward
            ? new Area(Math.Min(x0, x1), y0 - (thick * .5f), Math.Max(x0, x1), y0 + (thick * .5f))
            : new Area(x0 - (thick * .5f), Math.Min(y0, y1), x0 + (thick * .5f), Math.Max(y0, y1));
        Slab(GardenLayer.Hedge, id, area, 0f, high, kind < 7 ? HedgeTone : WallTone);
    }

    /// <summary>A shared green: lawn, cross paths, a planted centre, edge trees, a play corner and benches, behind railings.</summary>
    private void Green(ulong id, Area g, GardenDraw r, HashSet<(int, int, int, int)> fences)
    {
        _gardenGround.Add(new Transform3D(Basis.FromScale(new Vector3(g.Wide, 1f, g.Deep)),
            new Vector3((g.West + g.East) * .5f, 0f, -(g.South + g.North) * .5f)));
        float cx = (g.West + g.East) * .5f, cy = (g.South + g.North) * .5f;
        Color stone = StoneTones[2];
        Slab(GardenLayer.Ground, id, g, 0f, .06f, GreenLawn);
        Slab(GardenLayer.Ground, id, new Area(g.West, cy - 1.2f, g.East, cy + 1.2f), 0f, .08f, stone);
        Slab(GardenLayer.Ground, id, new Area(cx - 1.2f, g.South, cx + 1.2f, g.North), 0f, .08f, stone);
        Slab(GardenLayer.Ground, id, new Area(cx - 4f, cy - 4f, cx + 4f, cy + 4f), 0f, .09f, stone);
        Module(GardenLayer.Flowerbed, id, cx, cy, 0f);

        int quadrant = r.Below(4);
        float px = cx + ((quadrant & 1) == 0 ? 1 : -1) * g.Wide * .25f;
        float py = cy + ((quadrant & 2) == 0 ? -1 : 1) * g.Deep * .25f;
        Slab(GardenLayer.Ground, id, new Area(px - 5f, py - 4f, px + 5f, py + 4f), 0f, .1f, PlaySurface);
        Module(GardenLayer.Play, id, px, py, 0f);

        foreach (var (x, y, turn) in new[] { (cx - 6f, cy + 2.6f, 0f), (cx + 6f, cy - 2.6f, Mathf.Pi), (cx + 2.6f, cy + 6f, -Mathf.Pi / 2f), (cx - 2.6f, cy - 6f, Mathf.Pi / 2f) })
        {
            Module(GardenLayer.Bench, id, x, y, turn);
        }

        for (float k = 3f; k < g.Wide - 2f; k += 7f)
        {
            foreach (var (x, y) in new[] { (g.West + k, g.South + 3f), (g.West + k, g.North - 3f) })
            {
                if (MathF.Abs(x - cx) > 3f && MathF.Abs(x - px) + MathF.Abs(y - py) > 8f && r.Chance(.7f)) Tree(id, x, y, 1.1f, r);
            }
        }

        for (float k = 3f; k < g.Deep - 2f; k += 7f)
        {
            foreach (var (x, y) in new[] { (g.West + 3f, g.South + k), (g.East - 3f, g.South + k) })
            {
                if (MathF.Abs(y - cy) > 3f && MathF.Abs(x - px) + MathF.Abs(y - py) > 8f && r.Chance(.7f)) Tree(id, x, y, 1.1f, r);
            }
        }

        Railing(id, g.West, g.South, g.East, g.South, fences);
        Railing(id, g.West, g.North, g.East, g.North, fences);
        Railing(id, g.West, g.South, g.West, g.North, fences);
        Railing(id, g.East, g.South, g.East, g.North, fences);
    }

    private void Railing(ulong id, float x0, float y0, float x1, float y1, HashSet<(int, int, int, int)> fences)
    {
        float length = MathF.Abs(x1 - x0) + MathF.Abs(y1 - y0);
        fences.Add(((int)MathF.Round(x0 * 4f), (int)MathF.Round(y0 * 4f), (int)MathF.Round(x1 * 4f), (int)MathF.Round(y1 * 4f)));
        bool eastward = MathF.Abs(x1 - x0) >= MathF.Abs(y1 - y0);
        int panels = Math.Max(1, (int)MathF.Ceiling(length / FencePanel));
        float stretch = length / (panels * FencePanel);
        Basis turn = YawScaled(eastward ? 0f : Mathf.Pi / 2f, new Vector3(stretch, 1f, 1f));
        for (int k = 0; k < panels; k++)
        {
            float t = (k + .5f) / panels;
            Put(GardenLayer.Railing, id, new Transform3D(turn, new Vector3(x0 + ((x1 - x0) * t), 0f, -(y0 + ((y1 - y0) * t)))), Colors.White);
        }
    }

    /// <summary>SplitMix64 over one appearance draw, so a garden's furniture is fixed by its Lot's id.</summary>
    private sealed class GardenDraw(ulong seed)
    {
        private ulong _state = seed;

        private ulong Next()
        {
            ulong z = _state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public float Unit() => (Next() >> 40) / (float)(1UL << 24);

        public float Range(float low, float high) => low + (Unit() * (high - low));

        public bool Chance(float p) => Unit() < p;

        public int Below(int n) => (int)(Next() % (ulong)n);

        public Color Pick(Color[] from) => from[Below(from.Length)];
    }
}
