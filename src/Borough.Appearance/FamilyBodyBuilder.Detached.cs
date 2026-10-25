using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The detached house forms: one house on its Lot, varied by roof and plan. They follow
/// <c>scripts/art/house-forms.py</c>.
/// </summary>
/// <remarks>
/// The L-plan and the rear outshot draw less floor than the Building's footprint and storeys hold, and
/// the side wing draws more, reaching <see cref="SideWingReach"/> into the side garden. The drawing
/// allows a low-rise house up to about 20% either way (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>Below this frontage a terrace or front-gable body is one house rather than a row.</summary>
    public const float SingleHouseFrontage = 12f;

    /// <summary>How far a side wing stands past the house into its side garden.</summary>
    public const float SideWingReach = 3f;

    private const float HouseEaves = .35f;

    internal static bool IsDetached(HouseVariant variant) => variant is HouseVariant.Hipped or HouseVariant.HalfHip
        or HouseVariant.CrossGable or HouseVariant.Saltbox or HouseVariant.Gambrel or HouseVariant.LPlan
        or HouseVariant.RearOutshot or HouseVariant.SideWing;

    private static void Detached(Writer w, HouseLook s, HouseVariant form)
    {
        float X = s.W / 2f, Y = s.D / 2f, H = s.H;
        HouseModule m = s.Modules[0];
        switch (form)
        {
            case HouseVariant.Hipped or HouseVariant.HalfHip:
                {
                    Block(w, s, m, -X, X, -Y, Y, s.Storeys);
                    bool alongY = s.D >= s.W;
                    HipGable(w, s, s.End, alongY, 0f, 0f, alongY ? X : Y, alongY ? Y : X, H, s.Pitch,
                        form == HouseVariant.Hipped ? 1f : .35f, front: true, back: true);
                    break;
                }

            case HouseVariant.CrossGable:
                {
                    Block(w, s, m, -X, X, -Y, Y, s.Storeys);
                    HipGable(w, s, s.End, alongY: false, 0f, 0f, Y, X, H, s.Pitch, 0f, front: true, back: true);
                    float a = MathF.Min(2.4f, X * .6f);
                    HipGable(w, s, m.Wall, alongY: true, s.Side * (X - a), -Y / 2f, a, Y / 2f, H, s.WingPitch, 0f, front: true, back: false);
                    break;
                }

            case HouseVariant.Saltbox:
                Saltbox(w, s, m, X, Y);
                break;
            case HouseVariant.Gambrel:
                Gambrel(w, s, m, X, Y);
                break;
            case HouseVariant.LPlan:
                LPlan(w, s, m, X, Y);
                break;
            case HouseVariant.RearOutshot:
                {
                    float split = Y - MathF.Min(4f, s.D / 3f);
                    Block(w, s, m, -X, X, -Y, split, s.Storeys);
                    Block(w, s, m, -X, X, split, Y, 1, front: false);
                    HipGable(w, s, s.End, alongY: false, 0f, (-Y + split) / 2f, (split + Y) / 2f, X, H, s.Pitch, 0f, front: true, back: true);
                    LeanTo(w, s, -X, X, split, Y, Storey + 1.3f, Storey);
                    break;
                }

            default:
                {
                    Block(w, s, m, -X, X, -Y, Y, s.Storeys);
                    HipGable(w, s, s.End, alongY: false, 0f, 0f, Y, X, H, s.Pitch, 0f, front: true, back: true);
                    float x0 = s.Side > 0 ? X : -X - SideWingReach, x1 = x0 + SideWingReach;
                    float y0 = -Y + MathF.Min(2.5f, s.D * .2f), y1 = Y - MathF.Min(1f, s.D * .1f);
                    Block(w, s, m, x0, x1, y0, y1, 1, ends: false, door: false);
                    float inner = s.Side > 0 ? x0 : x1, outer = s.Side > 0 ? x1 : x0;
                    LeanToAcross(w, s, inner, outer, y0, y1, Storey + 1.2f, Storey);
                    break;
                }
        }
    }

    /// <summary>The walls of one rectangular volume, its street face looking down -Y.</summary>
    private static void Block(Writer w, HouseLook s, HouseModule m, float x0, float x1, float y0, float y1, int storeys,
        bool front = true, bool back = true, bool ends = true, bool door = true)
    {
        float width = x1 - x0, depth = y1 - y0, height = storeys * Storey;
        HouseModule module = m with { Width = width, Mirror = false };
        if (front)
        {
            List<Opening> openings = HouseFront(module, s, storeys);
            if (!door) openings.RemoveAll(o => o.Kind == OpeningKind.Door);
            Elevation(w, s, new Face(new Vector3(x0, y0, 0f), Vector3.UnitX, -Vector3.UnitY), width, openings, m.Wall, m.Door, height);
        }

        if (back)
        {
            List<Opening> openings = HouseBack(module, s, storeys);
            if (!door) openings.RemoveAll(o => o.Kind == OpeningKind.Door);
            Elevation(w, s, new Face(new Vector3(x1, y1, 0f), -Vector3.UnitX, Vector3.UnitY), width, openings, m.Wall, m.Door, height);
        }

        if (ends)
        {
            List<Opening> windows = depth >= 3.5f ? Ends(depth, s, storeys) : [];
            Elevation(w, s, new Face(new Vector3(x0, y1, 0f), -Vector3.UnitY, -Vector3.UnitX), depth, windows, s.End, s.Trim, height);
            Elevation(w, s, new Face(new Vector3(x1, y0, 0f), Vector3.UnitY, Vector3.UnitX), depth, windows, s.End, s.Trim, height);
        }

        w.Box(new Vector3(x0 - .05f, y0 - .05f, 0f), new Vector3(x1 + .05f, y1 + .05f, PlinthHeight), HouseLook.Plinth);
    }

    /// <summary>
    /// A roof whose ridge runs along Y (or X) through (<paramref name="cx"/>, <paramref name="cy"/>) over a
    /// wall rectangle 2A across and 2L along. k = 0 is a gable, 1 a full hip, and between is a half-hip.
    /// </summary>
    private static void HipGable(Writer w, HouseLook s, string wall, bool alongY, float cx, float cy, float A, float L, float H,
        float pitch, float k, bool front, bool back)
    {
        Vector3 P(float a, float l, float z) => alongY ? new Vector3(cx + a, cy + l, z) : new Vector3(cx + l, cy + a, z);
        Vector3 Dir(float a, float l, float z) => alongY ? new Vector3(a, l, z) : new Vector3(l, a, z);

        float t = MathF.Tan(pitch * MathF.PI / 180f);
        float top = H + (A * t), low = H - (HouseEaves * t);
        float ae = A + HouseEaves, le = L + HouseEaves;
        float zk = k >= 1f ? low : top - (k * (top - low));
        float ak = k >= 1f ? ae : (top - zk) / t;
        float ry = MathF.Max(0f, le - ak);

        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            Sheet(w, s.Roof, [P(side * ae, -le, low), P(side * ak, -le, zk), P(0f, -ry, top), P(0f, ry, top), P(side * ak, le, zk), P(side * ae, le, low)],
                Dir(side, 0f, 1f));
        }

        foreach (float end in (ReadOnlySpan<float>)[-1f, 1f])
        {
            if (ak > 1e-3f) Sheet(w, s.Roof, [P(-ak, end * le, zk), P(ak, end * le, zk), P(0f, end * ry, top)], Dir(0f, end, 1f));
            bool wanted = alongY ? (end < 0f ? front : back) : true;
            if (!wanted || zk <= H + 1e-3f) continue;
            float aw = MathF.Min(A, (top - zk) / t);
            Shape(w, wall, [P(-A, end * L, H), P(A, end * L, H), P(aw, end * L, zk), P(-aw, end * L, zk)], Dir(0f, end, 0f));
        }
    }

    private static void Saltbox(Writer w, HouseLook s, HouseModule m, float X, float Y)
    {
        int rearStoreys = Math.Max(1, s.Storeys - 1);
        float H = s.H, rearH = rearStoreys * Storey;
        Block(w, s, m, -X, X, -Y, Y, s.Storeys, back: false, ends: false);
        Block(w, s, m, -X, X, -Y, Y, rearStoreys, front: false);

        float t = MathF.Tan(s.Pitch * MathF.PI / 180f);
        float ridge = -.3f * Y, top = H + ((Y + ridge) * t), rearLow = rearH + .5f;
        float fall = (top - rearLow) / (Y - ridge);
        float ends = X + .3f;
        Sheet(w, s.Roof, [new(-ends, -Y - HouseEaves, H - (HouseEaves * t)), new(ends, -Y - HouseEaves, H - (HouseEaves * t)),
            new(ends, ridge, top), new(-ends, ridge, top)], new Vector3(0f, -1f, 1f));
        Sheet(w, s.Roof, [new(-ends, ridge, top), new(ends, ridge, top), new(ends, Y + HouseEaves, rearLow - (HouseEaves * fall)),
            new(-ends, Y + HouseEaves, rearLow - (HouseEaves * fall))], new Vector3(0f, 1f, 1f));
        foreach (float f in (ReadOnlySpan<float>)[-1f, 1f])
        {
            float x = f * X;
            Shape(w, s.End, [new(x, -Y, rearH), new(x, Y, rearH), new(x, Y, rearLow), new(x, ridge, top), new(x, -Y, H)], new Vector3(f, 0f, 0f));
        }
    }

    /// <summary>One storey fewer of wall; the top floor lives inside the gambrel, lit from the ends and often a shed dormer.</summary>
    private static void Gambrel(Writer w, HouseLook s, HouseModule m, float X, float Y)
    {
        int wallStoreys = Math.Max(1, s.Storeys - 1);
        float H = wallStoreys * Storey;
        Block(w, s, m, -X, X, -Y, Y, wallStoreys);

        float lower = MathF.Tan(66f * MathF.PI / 180f), upper = MathF.Tan(24f * MathF.PI / 180f);
        const float Run = 1.6f, Eaves = .45f;
        float ak = Y - Run, zk = H + (Run * lower), top = zk + (ak * upper), low = H - (Eaves * .55f), ends = X + .25f;
        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            Sheet(w, s.Roof, [new(-ends, side * (Y + Eaves), low), new(ends, side * (Y + Eaves), low), new(ends, side * ak, zk), new(-ends, side * ak, zk)],
                new Vector3(0f, side, .5f));
            Sheet(w, s.Roof, [new(-ends, side * ak, zk), new(ends, side * ak, zk), new(ends, 0f, top), new(-ends, 0f, top)], new Vector3(0f, side, 2f));
        }

        foreach (float f in (ReadOnlySpan<float>)[-1f, 1f])
        {
            float x = f * X;
            Shape(w, s.End, [new(x, -Y, H), new(x, Y, H), new(x, ak, zk), new(x, 0f, top), new(x, -ak, zk)], new Vector3(f, 0f, 0f));
            foreach (float c in (ReadOnlySpan<float>)[-.3f, .3f])
            {
                float y = c * Y, o = x + (f * .06f);
                w.Box(new Vector3(MathF.Min(x, o + (f * .04f)), y - .62f, H + .78f), new Vector3(MathF.Max(x, o + (f * .04f)), y + .62f, H + 2.62f), s.Trim);
                w.Box(new Vector3(MathF.Min(x, o + (f * .07f)), y - .5f, H + .9f), new Vector3(MathF.Max(x, o + (f * .07f)), y + .5f, H + 2.5f), "glass");
            }
        }

        if (!s.Dormer) return;
        float dx = X - 1f, face = -(Y - .35f), dz = zk + .2f;
        w.Box(new Vector3(-dx, face, H), new Vector3(dx, -1.2f, dz), s.End);
        Sheet(w, s.Roof, [new(-dx - .25f, face - .4f, dz + .05f), new(dx + .25f, face - .4f, dz + .05f), new(dx + .25f, -1f, top - .9f), new(-dx - .25f, -1f, top - .9f)],
            new Vector3(0f, -.3f, 1f));
        foreach (float c in (ReadOnlySpan<float>)[-.5f, .5f])
        {
            float wx = c * dx;
            w.Box(new Vector3(wx - .72f, face - .06f, H + .78f), new Vector3(wx + .72f, face, H + 2.42f), s.Trim);
            w.Box(new Vector3(wx - .6f, face - .09f, H + .9f), new Vector3(wx + .6f, face - .06f, H + 2.3f), "glass");
        }
    }

    /// <summary>The street face steps back beside a full-height gabled wing, inside the footprint.</summary>
    private static void LPlan(Writer w, HouseLook s, HouseModule m, float X, float Y)
    {
        float wing = MathF.Min(4.4f, s.W * .55f), recess = -Y + 1.6f;
        (float wx0, float wx1) = s.Side < 0 ? (-X, -X + wing) : (X - wing, X);
        Block(w, s, m, -X, X, recess, Y, s.Storeys, door: false);
        Block(w, s, m, wx0, wx1, -Y, recess + .01f, s.Storeys, back: false);
        HipGable(w, s, s.End, alongY: false, 0f, (recess + Y) / 2f, (Y - recess) / 2f, X, s.H, s.Pitch, 0f, front: true, back: true);
        HipGable(w, s, m.Wall, alongY: true, (wx0 + wx1) / 2f, -Y / 2f, wing / 2f, Y / 2f, s.H, s.WingPitch, 0f, front: true, back: false);
    }

    /// <summary>A mono-pitch roof falling from <paramref name="y0"/> to <paramref name="y1"/>, with its two end walls.</summary>
    private static void LeanTo(Writer w, HouseLook s, float x0, float x1, float y0, float y1, float high, float low)
    {
        Sheet(w, s.Roof, [new(x0 - .2f, y0, high), new(x1 + .2f, y0, high), new(x1 + .2f, y1 + .3f, low - .15f), new(x0 - .2f, y1 + .3f, low - .15f)],
            new Vector3(0f, 1f, 2f));
        foreach (float x in (ReadOnlySpan<float>)[x0, x1])
        {
            Shape(w, s.End, [new(x, y0, Storey), new(x, y1, Storey), new(x, y1, low), new(x, y0, high)], new Vector3(x == x0 ? -1f : 1f, 0f, 0f));
        }
    }

    /// <summary>A mono-pitch roof over a side wing, falling from the house wall at <paramref name="inner"/> to <paramref name="outer"/>.</summary>
    private static void LeanToAcross(Writer w, HouseLook s, float inner, float outer, float y0, float y1, float high, float low)
    {
        float reach = outer + (MathF.Sign(outer - inner) * .3f);
        Sheet(w, s.Roof, [new(inner, y0 - .2f, high), new(inner, y1 + .2f, high), new(reach, y1 + .2f, low - .15f), new(reach, y0 - .2f, low - .15f)],
            new Vector3(MathF.Sign(outer - inner), 0f, 2f));
        foreach (float y in (ReadOnlySpan<float>)[y0, y1])
        {
            Shape(w, s.End, [new(inner, y, Storey), new(outer, y, Storey), new(outer, y, low), new(inner, y, high)], new Vector3(0f, y == y0 ? -1f : 1f, 0f));
        }
    }

    /// <summary>A roof sheet seen from both sides, so its eaves read from below as well as above.</summary>
    private static void Sheet(Writer w, string part, ReadOnlySpan<Vector3> corners, Vector3 outward)
    {
        Shape(w, part, corners, outward);
        Shape(w, part, corners, -outward);
    }
}
