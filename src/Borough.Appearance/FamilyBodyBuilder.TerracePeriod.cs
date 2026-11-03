using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The contemporary and mid-century attached house forms. They follow <c>scripts/art/rowhouse-periods.py</c>
/// and dress themselves from the wide house looks, drawn once for the whole terrace.
/// </summary>
/// <remarks>
/// Each house keeps the terrace contract: a blank wall on each shared side, its own roof end over a shared
/// wall, an upstand where its roof meets one, and nothing past a party line. The tile-hung eaves stop
/// at 0.5 m, which keeps the hipped end and the back eave on the Lot.
/// </remarks>
public static partial class FamilyBodyBuilder
{
    private const float TerraceStep = 1.2f;
    private const float TerraceSetBack = 2.2f;
    private const float UpstandThickness = .15f;

    private static void PeriodTerrace(Writer w, TerraceBody terrace, float x, float y, int storeys, AttachedSides attached,
        TerracePlace place, float pitch)
    {
        WideLook s = TerraceLook(terrace.Form, place.Seed, 2f * x, 2f * y, storeys, pitch);
        var house = new TerraceHouse(w, s, x, -y, y, attached, place.Index);
        switch (terrace.Form)
        {
            case TerraceForm.EavelessGable:
                EavelessGable(house);
                break;
            case TerraceForm.RoofTerrace:
                RoofTerrace(house);
                break;
            case TerraceForm.Sawtooth:
                SawtoothRow(house, new HouseDraw(((ulong)place.Seed << 8) | 11).Chance(.5f) ? 1f : -1f);
                break;
            case TerraceForm.BoxFront:
                BoxFront(house);
                break;
            case TerraceForm.CrossWall:
                CrossWall(house);
                break;
            case TerraceForm.Butterfly:
                ButterflyRow(house);
                break;
            case TerraceForm.ParapetInfill:
                ParapetInfill(house);
                break;
            case TerraceForm.MansardZinc:
                MansardZinc(house);
                break;
            default:
                TileHung(house);
                break;
        }
    }

    private static WideLook TerraceLook(TerraceForm form, int seed, float frontage, float depth, int storeys, float pitch)
    {
        WideLook s = WideLook.Draw(new TerraceBody(form, null).Contemporary, seed, frontage, depth, storeys) with { Pitch = pitch };
        switch (form)
        {
            case TerraceForm.BoxFront:
                return s with { Windows = UpperWindows.Full };
            case TerraceForm.Butterfly:
                string metal = new HouseDraw(((ulong)seed << 8) | 6).Pick(Metals);
                string edge = Dressed("trim", null, metal);
                return s with
                {
                    Panels = false,
                    Upper = Dressed("wall", "painted-plaster-wall", "d8d6d0"),
                    Roof = Dressed("roof", "standing-seam", metal),
                    Edges = new RoofEdges(edge, edge, edge),
                };
            case TerraceForm.TileHung:
                bool tiles = new HouseDraw(((ulong)seed << 8) | 12).Chance(.5f);
                return s with { Panels = false, Upper = tiles ? Dressed("wall", "clay-roof-tiles-02", null) : Dressed("wall", "wood-siding-005", null) };
            default:
                return s;
        }
    }

    /// <summary>One house of a terrace: its look, its walls' bounds and the sides it shares.</summary>
    private sealed record TerraceHouse(Writer W, WideLook S, float X, float Y0, float Y1, AttachedSides Attached, int Index)
    {
        public bool Left => Attached.HasFlag(AttachedSides.Left);

        public bool Right => Attached.HasFlag(AttachedSides.Right);

        public float H => S.H;
    }

    /// <summary>The street and garden walls by the look's period, windows on a free end and a blank wall on each shared side.</summary>
    private static void PeriodWalls(TerraceHouse h, WideLook s, float y0, int storeys, bool door = true, float z0 = 0f)
    {
        float x = h.X, y1 = h.Y1, d = y1 - y0, height = storeys * Storey;
        WideRow(h.W, s, 1, 2f * x, -x, y0, y1, storeys, left: !h.Left, right: !h.Right, door: door, z0: z0, first: h.Index);
        if (h.Left) h.W.Facade(new Face(new Vector3(-x, y1, z0), -Vector3.UnitY, -Vector3.UnitX), d, height, [], s.End);
        if (h.Right) h.W.Facade(new Face(new Vector3(x, y0, z0), Vector3.UnitY, Vector3.UnitX), d, height, [], s.End);
    }

    /// <summary>
    /// A point on a roof edge 0.15 m in from <paramref name="end"/> toward <paramref name="other"/>, so
    /// the verge trim drawn from it stays inside a party line.
    /// </summary>
    private static Vector3 Upslope(Vector3 end, Vector3 other) => end + ((other - end) * (.15f / MathF.Abs(other.X - end.X)));

    /// <summary>A party upstand of a convex (y, z) profile standing inside the house on each shared side.</summary>
    private static void PartyUpstands(TerraceHouse h, ReadOnlySpan<Vector2> profile)
    {
        if (h.Left) h.W.Prism(profile, -h.X, -h.X + UpstandThickness, h.S.End);
        if (h.Right) h.W.Prism(profile, h.X - UpstandThickness, h.X, h.S.End);
    }

    /// <summary>A steep front gable to each house with no eaves, glazed in the gable. Every other house stands back.</summary>
    private static void EavelessGable(TerraceHouse h)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0 + (h.Index % 2 == 1 ? TerraceStep : 0f), y1 = h.Y1, H = h.H;
        PeriodWalls(h, s, y0, s.Storeys);
        float t = MathF.Tan(s.Pitch * MathF.PI / 180f), top = H + (x * t), cy = (y0 + y1) / 2f, le = ((y1 - y0) / 2f) + s.Eaves;
        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            bool shared = side < 0f ? h.Left : h.Right;
            float over = shared ? 0f : s.Eaves, xe = side * (x + over), low = H - (over * t);
            Vector3 lowA = new(xe, cy - le, low), lowB = new(xe, cy + le, low), highA = new(0f, cy - le, top), highB = new(0f, cy + le, top);
            Sheet(h.W, s.Roof, [lowA, lowB, highB, highA], new Vector3(side, 0f, 1f));
            if (shared) (lowA, lowB) = (Upslope(lowA, highA), Upslope(lowB, highB));
            SlopeEdges(h.W, s.Edges, lowA, lowB, highA, highB, ridge: side > 0f, gutter: !shared);
        }

        Shape(h.W, s.End, [new(-x, y0, H), new(x, y0, H), new(0f, y0, top)], -Vector3.UnitY);
        Shape(h.W, s.End, [new(-x, y1, H), new(x, y1, H), new(0f, y1, top)], Vector3.UnitY);
        GableGlass(h.W, s, 0f, y0, x);
    }

    /// <summary>A flat roof with the top storey set back behind a glass rail, and screens between the terraces.</summary>
    private static void RoofTerrace(TerraceHouse h)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0, y1 = h.Y1;
        int low = Math.Max(1, s.Storeys - 1);
        float lowH = low * Storey;
        PeriodWalls(h, s, y0, low);
        Parapet(h.W, s, -x, x, y0, y1, lowH, flush: h.Attached);
        WideLook top = s with { Ground = s.Accent, Upper = s.Accent, Windows = UpperWindows.Full };
        PeriodWalls(h, top, y0 + TerraceSetBack, 1, door: false, z0: lowH);
        float xa = h.Left ? -x : -x - .3f, xb = h.Right ? x : x + .3f;
        h.W.Box(new Vector3(xa, y0 + TerraceSetBack - .4f, lowH + Storey), new Vector3(xb, y1 + .3f, lowH + Storey + .25f), s.Trim);
        h.W.Box(new Vector3(-x + .3f, y0 + .1f, lowH + .5f), new Vector3(x - .3f, y0 + .14f, lowH + 1.05f), "glass");
        if (h.Left) h.W.Box(new Vector3(-x + .25f, y0 + .25f, lowH), new Vector3(-x + .33f, y0 + TerraceSetBack, lowH + 1.8f), s.Accent);
        if (h.Right) h.W.Box(new Vector3(x - .33f, y0 + .25f, lowH), new Vector3(x - .25f, y0 + TerraceSetBack, lowH + 1.8f), s.Accent);
    }

    /// <summary>
    /// A mono-pitch to each house, rising the way <paramref name="rising"/> says for the whole terrace,
    /// with a clerestory in its step. The step's coping stays inside a party line.
    /// </summary>
    private static void SawtoothRow(TerraceHouse h, float rising)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0, y1 = h.Y1, H = h.H, e = s.Eaves, depth = y1 - y0;
        float rise = 2f * x * MathF.Tan(s.Pitch * MathF.PI / 180f);
        PeriodWalls(h, s, y0, s.Storeys);
        (float lo, float hi) = rising > 0f ? (-x, x) : (x, -x);
        bool sharedLow = rising > 0f ? h.Left : h.Right, sharedHigh = rising > 0f ? h.Right : h.Left;
        Vector3 lowA = new(lo, y0 - e, H), lowB = new(lo, y1 + e, H), highA = new(hi, y0 - e, H + rise), highB = new(hi, y1 + e, H + rise);
        Sheet(h.W, s.Roof, [lowA, highA, highB, lowB], Vector3.UnitZ);
        (Vector3 edgeA, Vector3 edgeB) = sharedLow ? (Upslope(lowA, highA), Upslope(lowB, highB)) : (lowA, lowB);
        (Vector3 ridgeA, Vector3 ridgeB) = sharedHigh ? (Upslope(highA, lowA), Upslope(highB, lowB)) : (highA, highB);
        SlopeEdges(h.W, s.Edges, edgeA, edgeB, ridgeA, ridgeB, ridge: false, gutter: !sharedLow);
        Shape(h.W, s.End, [new(lo, y0, H), new(hi, y0, H), new(hi, y0, H + rise)], -Vector3.UnitY);
        Shape(h.W, s.End, [new(lo, y1, H), new(hi, y1, H), new(hi, y1, H + rise)], Vector3.UnitY);

        Face step = rising > 0f
            ? new Face(new Vector3(hi, y0, H), Vector3.UnitY, Vector3.UnitX)
            : new Face(new Vector3(hi, y1, H), -Vector3.UnitY, -Vector3.UnitX);
        List<Opening> clerestory = rise > 1f ? [new Opening(.6f, .3f, depth - 1.2f, rise - .6f, OpeningKind.Stair)] : [];
        h.W.Facade(step, depth, rise, clerestory, s.End, s.Trim, s.Doors[0], s.Frame);
        float inner = hi - (rising * (sharedHigh ? .24f : .12f)), outer = sharedHigh ? hi : hi + (rising * .12f);
        Cuboid(h.W, new Vector3(inner, y0 - e, H + rise), new Vector3(outer, y1 + e, H + rise + .08f), s.Edges.Cap);
    }

    /// <summary>A flat roof behind a thin coping, the upper storeys framed by a box standing out from the street wall.</summary>
    private static void BoxFront(TerraceHouse h)
    {
        WideLook s = h.S;
        float x = h.X, H = h.H, width = 2f * x;
        PeriodWalls(h, s, h.Y0, s.Storeys);
        Parapet(h.W, s, -x, x, h.Y0, h.Y1, H, height: .3f, flush: h.Attached);
        var face = new Face(new Vector3(-x, h.Y0, 0f), Vector3.UnitX, -Vector3.UnitY);
        const float A = .3f, T = .22f;
        float b = width - A, v0 = Storey - .1f, v1 = H - .25f;
        h.W.Slab(face, A, b, v0, v0 + T, -.65f, 0f, s.Accent);
        h.W.Slab(face, A, b, v1 - T, v1, -.65f, 0f, s.Accent);
        h.W.Slab(face, A, A + T, v0, v1, -.65f, 0f, s.Accent);
        h.W.Slab(face, b - T, b, v0, v1, -.65f, 0f, s.Accent);
    }

    /// <summary>
    /// A shallow side gable with deep eaves, brick cross walls standing forward on the party lines, and
    /// panels under ribbon windows.
    /// </summary>
    private static void CrossWall(TerraceHouse h)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0, y1 = h.Y1, H = h.H;
        PeriodWalls(h, s, y0, s.Storeys);
        StreetRidge(h, s.Eaves, hips: false);
        foreach ((float a, float b) in (ReadOnlySpan<(float, float)>)[(-x, -x + (h.Left ? .15f : .26f)), (x - (h.Right ? .15f : .26f), x)])
        {
            h.W.Box(new Vector3(a, y0 - .45f, 0f), new Vector3(b, y0, H - .15f), s.Fin);
            h.W.Box(new Vector3(a, y1, 0f), new Vector3(b, y1 + .3f, H - .15f), s.Fin);
        }
    }

    /// <summary>
    /// A ridge along the street over the house. A free end overhangs, or hips where <paramref name="hips"/>;
    /// a shared end stops on the party wall, carries an upstand and shows its gable above a lower neighbor.
    /// </summary>
    private static void StreetRidge(TerraceHouse h, float eaves, bool hips)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0, y1 = h.Y1, H = h.H;
        float t = MathF.Tan(s.Pitch * MathF.PI / 180f), A = (y1 - y0) / 2f, cy = (y0 + y1) / 2f;
        float top = H + (A * t), low = H - (eaves * t), free = hips ? eaves : .3f;
        float xa = h.Left ? -x : -x - free, xb = h.Right ? x : x + free;
        float ra = xa + (hips && !h.Left ? A + eaves : 0f), rb = xb - (hips && !h.Right ? A + eaves : 0f);

        // A hip longer than the house meets the far end: the middle where both ends are free, else the shared wall.
        if (ra > rb) ra = rb = h.Left == h.Right ? (ra + rb) / 2f : h.Right ? rb : ra;
        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            float ye = cy + (side * (A + eaves));
            Sheet(h.W, s.Roof, [new(xa, ye, low), new(xb, ye, low), new(rb, cy, top), new(ra, cy, top)], new Vector3(0f, side, 1f));
            Cuboid(h.W, new Vector3(xa, ye, low - .25f), new Vector3(xb, ye - (side * .06f), low + .03f), s.Trim);
        }

        foreach ((bool shared, float end, float ridge, float side) in (ReadOnlySpan<(bool, float, float, float)>)[(h.Left, xa, ra, -1f), (h.Right, xb, rb, 1f)])
        {
            if (hips && !shared)
            {
                Sheet(h.W, s.Roof, [new(end, cy - A - eaves, low), new(end, cy + A + eaves, low), new(ridge, cy, top)], new Vector3(side, 0f, 1f));
            }
            else
            {
                float wall = side * x;
                Shape(h.W, s.End, [new(wall, y0, H), new(wall, y1, H), new(wall, cy, top)], new Vector3(side, 0f, 0f));
            }
        }

        float edge = A + .15f;
        PartyUpstands(h, [new(cy - edge, H - .3f), new(cy + edge, H - .3f), new(cy + edge, top - (edge * t) + .25f), new(cy, top + .25f),
            new(cy - edge, top - (edge * t) + .25f)]);
    }

    /// <summary>An inverted roof to each house, falling from its party walls to a valley on its center line.</summary>
    private static void ButterflyRow(TerraceHouse h)
    {
        WideLook s = h.S;
        const float E = .45f;
        float x = h.X, y0 = h.Y0, y1 = h.Y1, H = h.H;
        float t = MathF.Tan(s.Pitch * MathF.PI / 180f), rise = x * t, top = H + rise;
        PeriodWalls(h, s, y0, s.Storeys);
        float ea = h.Left ? 0f : E, eb = h.Right ? 0f : E;
        float za = top + (ea * t), zb = top + (eb * t);
        Sheet(h.W, s.Roof, [new(-x - ea, y0 - E, za), new(0f, y0 - E, H), new(0f, y1 + E, H), new(-x - ea, y1 + E, za)], new Vector3(1f, 0f, 1f));
        Sheet(h.W, s.Roof, [new(0f, y0 - E, H), new(x + eb, y0 - E, zb), new(x + eb, y1 + E, zb), new(0f, y1 + E, H)], new Vector3(-1f, 0f, 1f));
        foreach (float y in (ReadOnlySpan<float>)[y0, y1])
        {
            float f = y == y0 ? -1f : 1f, yo = y + (f * E);
            var outward = new Vector3(0f, f, 0f);
            Shape(h.W, s.Upper, [new(-x, y, H), new(0f, y, H), new(-x, y, top)], outward);
            Shape(h.W, s.Upper, [new(0f, y, H), new(x, y, H), new(x, y, top)], outward);
            Shape(h.W, s.Trim, [new(-x - ea, yo, za - .3f), new(0f, yo, H - .3f), new(0f, yo, H + .02f), new(-x - ea, yo, za + .02f)], outward);
            Shape(h.W, s.Trim, [new(0f, yo, H - .3f), new(x + eb, yo, zb - .3f), new(x + eb, yo, zb + .02f), new(0f, yo, H + .02f)], outward);
        }

        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            float wall = side * x;
            Shape(h.W, s.End, [new(wall, y0, H), new(wall, y1, H), new(wall, y1, top), new(wall, y0, top)], new Vector3(side, 0f, 0f));
        }

        PartyUpstands(h, [new(y0 - E, top - .3f), new(y1 + E, top - .3f), new(y1 + E, top + .2f), new(y0 - E, top + .2f)]);
    }

    /// <summary>
    /// A low roof along the street with deep eaves, hipped at a free end, over an upper storey hung with
    /// tiles or boards and a canopy along the ground floor.
    /// </summary>
    private static void TileHung(TerraceHouse h)
    {
        const float Eaves = .5f;
        float x = h.X;
        PeriodWalls(h, h.S, h.Y0, h.S.Storeys);
        StreetRidge(h, Eaves, hips: true);
        h.W.Box(new Vector3(-x + (h.Left ? 0f : .2f), h.Y0 - 1f, 2.55f), new Vector3(x - (h.Right ? 0f : .2f), h.Y0, 2.67f), h.S.Trim);
    }

    /// <summary>
    /// The parapet form's massing as contemporary infill: a plain parapet in the wall material under a thin
    /// metal coping. The roof deck stands inside the walls, so its edges never share a wall's plane.
    /// </summary>
    private static void ParapetInfill(TerraceHouse h)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0, y1 = h.Y1, H = h.H, top = H + TerraceParapetMetres;
        PeriodWalls(h, s, y0, s.Storeys);
        h.W.Box(new Vector3(-x + .15f, y0 + .25f, H - .2f), new Vector3(x - .15f, y1 - .25f, H + .05f), Deck);
        h.W.Box(new Vector3(-x, y0, H), new Vector3(x, y0 + .25f, top), s.Upper);
        h.W.Box(new Vector3(-x, y1 - .25f, H), new Vector3(x, y1, H + .5f), s.Upper);
        foreach ((float side, bool shared) in (ReadOnlySpan<(float, bool)>)[(-x, h.Left), (x, h.Right)])
        {
            float inner = side - (MathF.Sign(side) * (shared ? .15f : .25f));
            h.W.Box(new Vector3(MathF.Min(side, inner), y0 + .25f, H), new Vector3(MathF.Max(side, inner), y1 - .25f, H + (shared ? .6f : .5f)), s.End);
        }

        h.W.Box(new Vector3(-x, y0 - .04f, top), new Vector3(x, y0 + .29f, top + .06f), s.Edges.Cap);
    }

    /// <summary>
    /// The mansard form's massing as contemporary: the top storey clad in the look's standing-seam metal,
    /// lit by a flush framed box a bay, with a metal band at the eaves and no cornice.
    /// </summary>
    private static void MansardZinc(TerraceHouse h)
    {
        WideLook s = h.S;
        float x = h.X, y0 = h.Y0, y1 = h.Y1;
        int low = Math.Max(1, s.Storeys - 1);
        float eaves = low * Storey, k = MansardInset, top = eaves + Storey, crown = top + .35f, cy = (y0 + y1) / 2f;
        PeriodWalls(h, s, y0, low);
        float xa = h.Left ? -x : -x - .1f, xb = h.Right ? x : x + .1f;
        Sheet(h.W, s.Roof, [new(xa, y0 - .05f, eaves), new(xb, y0 - .05f, eaves), new(xb, y0 + k, top), new(xa, y0 + k, top)], new Vector3(0f, -1f, 1f));
        Sheet(h.W, s.Roof, [new(xa, y1 + .05f, eaves), new(xb, y1 + .05f, eaves), new(xb, y1 - k, top), new(xa, y1 - k, top)], new Vector3(0f, 1f, 1f));
        Sheet(h.W, s.Roof, [new(xa, y0 + k, top), new(xb, y0 + k, top), new(xb, cy, crown), new(xa, cy, crown)], new Vector3(0f, -1f, 3f));
        Sheet(h.W, s.Roof, [new(xa, y1 - k, top), new(xb, y1 - k, top), new(xb, cy, crown), new(xa, cy, crown)], new Vector3(0f, 1f, 3f));
        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            float wall = side * x;
            Shape(h.W, s.End, [new(wall, y0, eaves), new(wall, y1, eaves), new(wall, y1 - k, top), new(wall, cy, crown), new(wall, y0 + k, top)],
                new Vector3(side, 0f, 0f));
        }

        h.W.Box(new Vector3(-x, y0 - .1f, eaves - .12f), new Vector3(x, y0, eaves), s.Edges.Cap);
        int bays = Math.Max(1, (int)MathF.Round(2f * x / 4f));
        float bay = 2f * x / bays, face = y0 + .15f;
        for (int i = 0; i < bays; i++)
        {
            float c = -x + ((i + .5f) * bay), half = MathF.Min(1.3f, (bay / 2f) - .35f);
            h.W.Box(new Vector3(c - half, face, eaves + .15f), new Vector3(c + half, y0 + k + .3f, eaves + 2.75f), s.Edges.Cap);
            h.W.Box(new Vector3(c - half + .12f, face - .03f, eaves + .3f), new Vector3(c + half - .12f, face, eaves + 2.6f), "glass");
        }

        PartyUpstands(h, [new(y0 - .1f, eaves - .3f), new(y1 + .1f, eaves - .3f), new(y1 + .1f, eaves + .25f), new(y1 - k + .05f, top + .25f),
            new(cy, crown + .25f), new(y0 + k - .05f, top + .25f), new(y0 - .1f, eaves + .25f)]);
    }
}
