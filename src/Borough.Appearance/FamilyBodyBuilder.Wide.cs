using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The contemporary and mid-century wide house forms: a row of houses on a 12-84 m Lot, varied by roof
/// and plan. Each deep form stands its street form on a double-pile front over the first part of the Lot,
/// with flat-roofed outriggers running back to the end.
/// </summary>
/// <remarks>
/// <c>scripts/art/wide-house-forms.py</c> drew the options; this file is the reference. The metal roofs and
/// panels are flat paint standing in for library textures. The deep plan draws about 80% of the floor its
/// footprint and storeys hold, within the drawing's allowance (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    private static readonly string[] Metals = ["34383b", "4a4f53", "6e7377"];
    private static readonly string[] WidePanels = ["3a3e41", "5b6064", "8a8d86", "c8c3b8"];
    private static readonly string[] Whites = ["d8d6d0", "d6d2c8", "cfcac0"];
    private static readonly string[] ContemporaryDoors = ["c9a227", "2f4a5c", "b4472f", "2b2e31", "6b8f71", "d9d4c8"];
    private static readonly string[] MidCenturyDoors = ["c45a3b", "d8c27a", "2f4a5c", "6f8f7a", "eeeeea", "2b2e31"];
    private static readonly string[] MidCenturyPanels = ["a9c3cf", "d8c27a", "c45a3b", "eeeeea", "6f8f7a"];
    private static readonly string WidePlinth = Dressed("plinth", null, "6f6d68");
    private static readonly string Deck = Dressed("membrane", "bitumen", null);

    internal static bool IsWideForm(HouseVariant variant) => variant is HouseVariant.StaggeredGables
        or HouseVariant.FlatSetback or HouseVariant.Sawtooth or HouseVariant.Pavilions or HouseVariant.Span
        or HouseVariant.Butterfly or HouseVariant.Monopitch or HouseVariant.LowHip or HouseVariant.DeepFlat
        or HouseVariant.DeepGables or HouseVariant.DeepSawtooth or HouseVariant.DeepSpan;

    private static bool IsContemporary(HouseVariant variant) => variant is HouseVariant.StaggeredGables
        or HouseVariant.FlatSetback or HouseVariant.Sawtooth or HouseVariant.Pavilions or HouseVariant.DeepFlat
        or HouseVariant.DeepGables or HouseVariant.DeepSawtooth;

    /// <summary>The roof pitch in whole degrees a house of this form takes from the draw <paramref name="pitchSeed"/>; 0 for a flat roof.</summary>
    public static int HousePitchDegrees(HouseVariant variant, int pitchSeed, float frontage, float depth, int storeys) =>
        (int)MathF.Round(IsWideForm(variant) ? WidePitch(variant, pitchSeed)
            : HouseLook.Draw(variant, pitchSeed, frontage, depth, storeys).Pitch);

    private static float WidePitch(HouseVariant variant, int seed)
    {
        (float low, float high) = variant switch
        {
            HouseVariant.StaggeredGables or HouseVariant.Pavilions or HouseVariant.DeepGables => (38f, 45f),
            HouseVariant.Sawtooth or HouseVariant.DeepSawtooth => (14f, 22f),
            HouseVariant.Span or HouseVariant.DeepSpan => (12f, 16f),
            HouseVariant.Butterfly => (8f, 14f),
            HouseVariant.Monopitch => (8f, 12f),
            HouseVariant.LowHip => (18f, 24f),
            _ => (0f, 0f),
        };
        return high == 0f ? 0f : new HouseDraw(((ulong)seed << 8) | 9).Range(low, high);
    }

    private static FamilyBodyMesh BuildWide(FamilyBody body, HouseVariant form, float frontage, float depth, int storeys, int seed, int pitch)
    {
        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body);
        WideLook s = WideLook.Draw(form, seed, frontage, depth, storeys) with { Pitch = WidePitch(form, pitch) };
        switch (form)
        {
            case HouseVariant.DeepFlat:
                DeepWide(w, s, HouseVariant.FlatSetback);
                break;
            case HouseVariant.DeepGables:
                DeepWide(w, s, HouseVariant.StaggeredGables);
                break;
            case HouseVariant.DeepSawtooth:
                DeepWide(w, s, HouseVariant.Sawtooth);
                break;
            case HouseVariant.DeepSpan:
                DeepWide(w, s, HouseVariant.Span);
                break;
            default:
                StreetWide(w, s, form);
                break;
        }

        return mesh;
    }

    private static void StreetWide(Writer w, WideLook s, HouseVariant form)
    {
        switch (form)
        {
            case HouseVariant.StaggeredGables:
                StaggeredGables(w, s);
                break;
            case HouseVariant.FlatSetback:
                FlatSetback(w, s);
                break;
            case HouseVariant.Sawtooth:
                Sawtooth(w, s);
                break;
            case HouseVariant.Pavilions:
                Pavilions(w, s);
                break;
            case HouseVariant.Span:
                Span(w, s);
                break;
            case HouseVariant.Butterfly:
                Butterfly(w, s);
                break;
            case HouseVariant.Monopitch:
                Monopitch(w, s);
                break;
            default:
                LowHip(w, s);
                break;
        }
    }

    private enum UpperWindows
    {
        Wide,
        Pair,
        Full,
    }

    /// <summary>One wide house draw. <see cref="Y0"/> and <see cref="Y1"/> bound the street form, which a deep plan stops short of the Lot's back.</summary>
    private sealed record WideLook
    {
        public required int Seed { get; init; }
        public required float W { get; init; }
        public required float D { get; init; }
        public required float Y0 { get; init; }
        public required float Y1 { get; init; }
        public required int Storeys { get; init; }
        public required bool Contemporary { get; init; }
        public required string Roof { get; init; }
        public required string Trim { get; init; }
        public required string Frame { get; init; }
        public required string Ground { get; init; }
        public required string Upper { get; init; }
        public required string End { get; init; }
        public required string Accent { get; init; }
        public required string Fin { get; init; }
        public required string[] Doors { get; init; }
        public required float Eaves { get; init; }
        public required bool Panels { get; init; }
        public required UpperWindows Windows { get; init; }
        public float Pitch { get; init; }

        public float H => Storeys * Storey;

        public static WideLook Draw(HouseVariant form, int seed, float frontage, float depth, int storeys)
        {
            var r = new HouseDraw(((ulong)seed << 8) | 1);
            string brick = Dressed("wall", r.Pick(NaturalBricks), null);
            bool contemporary = IsContemporary(form);
            string roof, trim, frame, ground, upper, accent;
            string[] doors;
            if (contemporary)
            {
                roof = Dressed("trim", null, r.Pick(Metals));
                string timber = Dressed("wall", r.Chance(.5f) ? "wood-siding-011" : "hinoki-planks", null);
                string render = Dressed("wall", "painted-plaster-wall", r.Pick(Whites));
                string panel = Dressed("trim", null, r.Pick(WidePanels));
                (ground, upper) = r.Pick(6) switch
                {
                    0 => (brick, brick),
                    1 => (render, render),
                    2 => (timber, timber),
                    3 => (brick, timber),
                    4 => (render, panel),
                    _ => (brick, panel),
                };
                frame = Dressed("frame", null, r.Pick(["2b2e31", "2b2e31", "eeeeea", "6b5a45"]));
                trim = Dressed("trim", null, "3a3d40");
                doors = Sample(ref r, ContemporaryDoors, 3);
                accent = upper != timber ? timber : panel;
            }
            else
            {
                roof = Dressed("roof", r.Chance(.5f) ? "roofing-tiles-007" : "roofing-tiles-008", null);
                ground = upper = r.Chance(.75f) ? brick : Dressed("wall", "painted-plaster-wall", "d8d6d0");
                accent = r.Pick(3) switch
                {
                    0 => Dressed("wall", "clay-roof-tiles-02", null),
                    1 => Dressed("wall", "wood-siding-005", null),
                    _ => Dressed("trim", null, r.Pick(MidCenturyPanels)),
                };
                frame = Dressed("frame", null, r.Pick(["eeeeea", "eeeeea", "2b2e31"]));
                trim = Dressed("trim", null, "eeeeea");
                doors = Sample(ref r, MidCenturyDoors, 3);
            }

            return new WideLook
            {
                Seed = seed,
                W = frontage,
                D = depth,
                Y0 = -depth / 2f,
                Y1 = depth / 2f,
                Storeys = storeys,
                Contemporary = contemporary,
                Roof = roof,
                Trim = trim,
                Frame = frame,
                Ground = ground,
                Upper = upper,
                End = upper,
                Accent = accent,
                Fin = brick,
                Doors = doors,
                Eaves = contemporary ? .05f : .45f,
                Panels = !contemporary,
                Windows = (UpperWindows)r.Pick(3),
            };
        }

        private static string[] Sample(ref HouseDraw r, string[] paints, int count)
        {
            string[] pool = [.. paints];
            for (int i = 0; i < count; i++)
            {
                int j = i + r.Pick(pool.Length - i);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return [.. pool.Select(p => Dressed("door", null, p)).Take(count)];
        }
    }

    private static HouseDraw Proportions(WideLook s) => new(((ulong)s.Seed << 8) | 5);

    private static (int Count, float Width) WideHouses(float frontage, float width)
    {
        int n = frontage >= 12f ? Math.Max(2, (int)MathF.Round(frontage / width)) : 1;
        return (n, frontage / n);
    }

    private static void Cuboid(Writer w, Vector3 a, Vector3 b, string part) => w.Box(Vector3.Min(a, b), Vector3.Max(a, b), part);

    /// <summary>A wall in the look's ground and upper dressing, with framed windows and a base course.</summary>
    private static void WideFace(Writer w, WideLook s, Face face, float width, float height, List<Opening> openings, string door)
    {
        if (s.Upper != s.Ground && height > Storey)
        {
            w.Facade(face, width, Storey, openings.FindAll(o => o.V < Storey), s.Ground, s.Trim, door, s.Frame);
            var upper = new Face(face.Point(0f, Storey), face.Along, face.Outward);
            w.Facade(upper, width, height - Storey, [.. openings.Where(o => o.V >= Storey).Select(o => o with { V = o.V - Storey })],
                s.Upper, s.Trim, door, s.Frame);
        }
        else
        {
            w.Facade(face, width, height, openings, s.Ground, s.Trim, door, s.Frame);
        }

        foreach (Opening o in openings)
        {
            if (o.Kind != OpeningKind.Window) continue;
            float a = o.U, b = o.U + o.W, v = o.V, h = o.V + o.H;
            w.Slab(face, a, b, v, v + .07f, Reveal - .06f, Reveal, s.Frame);
            w.Slab(face, a, b, h - .07f, h, Reveal - .06f, Reveal, s.Frame);
            w.Slab(face, a, a + .07f, v, h, Reveal - .06f, Reveal, s.Frame);
            w.Slab(face, b - .07f, b, v, h, Reveal - .06f, Reveal, s.Frame);
        }

        w.Slab(face, -.02f, width + .02f, 0f, .15f, -.03f, 0f, WidePlinth);
    }

    private static List<Opening> ContemporaryFront(float w, int storeys, UpperWindows windows, bool mirror, bool door)
    {
        float F(float u, float width) => mirror ? w - u - width : u;
        var openings = new List<Opening>();
        float x = .5f;
        if (door)
        {
            openings.Add(new Opening(F(x, 1.1f), .15f, 1.1f, 2.4f, OpeningKind.Door));
            openings.Add(new Opening(F(x + 1.2f, .45f), .15f, .45f, 2.4f, OpeningKind.Window));
            x += 1.9f;
        }

        float ground = w - x - .5f;
        if (ground >= 1f) openings.Add(new Opening(F(x, ground), .45f, ground, 2.1f, OpeningKind.Window));
        for (int k = 1; k < storeys; k++)
        {
            float v = k * Storey;
            if (windows == UpperWindows.Full && w >= 5f)
            {
                float b = MathF.Min(w - 2f, 3.2f);
                openings.Add(new Opening(F((w - b) / 2f, b), v + .15f, b, 2.9f, OpeningKind.Window));
            }
            else if (windows == UpperWindows.Pair && w >= 5.5f)
            {
                openings.Add(new Opening(F((w * .2f) - .45f, .9f), v + .2f, .9f, 2.8f, OpeningKind.Window));
                openings.Add(new Opening(F((w * .62f) - 1.1f, 2.2f), v + .7f, 2.2f, 1.6f, OpeningKind.Window));
            }
            else
            {
                float b = MathF.Min(w - 1.6f, 4.2f);
                openings.Add(new Opening(F((w - b) / 2f, b), v + .6f, b, 1.8f, OpeningKind.Window));
            }
        }

        return openings;
    }

    private static List<Opening> MidCenturyFront(float w, int storeys, bool mirror, bool door)
    {
        float F(float u, float width) => mirror ? w - u - width : u;
        var openings = new List<Opening>();
        float x = .5f;
        if (door)
        {
            openings.Add(new Opening(F(x, 1f), .15f, 1f, 2.3f, OpeningKind.Door));
            x += 1.3f;
        }

        float ground = w - x - .5f;
        if (ground >= 1f) openings.Add(new Opening(F(x, ground), .75f, ground, 1.55f, OpeningKind.Window));
        for (int k = 1; k < storeys; k++) openings.Add(new Opening(F(.5f, w - 1f), (k * Storey) + .95f, w - 1f, 1.35f, OpeningKind.Window));
        return openings;
    }

    private static List<Opening> WideBack(float w, int storeys, bool contemporary)
    {
        float b = MathF.Min(w - 1.4f, 4f), upper = MathF.Min(w - 2f, 3f);
        List<Opening> openings = contemporary
            ? [new((w - b) / 2f, .15f, b, 2.4f, OpeningKind.Window)]
            : [new(.5f, .15f, 1f, 2.3f, OpeningKind.Door), new(1.8f, .75f, w - 2.3f, 1.55f, OpeningKind.Window)];
        for (int k = 1; k < storeys; k++) openings.Add(new Opening((w - upper) / 2f, (k * Storey) + .7f, upper, 1.6f, OpeningKind.Window));
        return openings;
    }

    /// <summary>A row of <paramref name="n"/> houses from <paramref name="x0"/>, fronts facing -Y at <paramref name="y0"/>.</summary>
    private static void WideRow(Writer w, WideLook s, int n, float width, float x0, float y0, float y1, int storeys,
        bool front = true, bool back = true, bool left = true, bool right = true, bool door = true, float z0 = 0f, int first = 0)
    {
        float H = storeys * Storey;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + (i * width);
            int k = first + i;
            string paint = s.Doors[k % s.Doors.Length];
            if (front)
            {
                var face = new Face(new Vector3(x, y0, z0), Vector3.UnitX, -Vector3.UnitY);
                List<Opening> openings = s.Contemporary
                    ? ContemporaryFront(width, storeys, s.Windows, k % 2 == 1, door)
                    : MidCenturyFront(width, storeys, k % 2 == 1, door);
                WideFace(w, s, face, width, H, openings, paint);
                foreach (Opening o in openings)
                {
                    if (o.Kind == OpeningKind.Door)
                    {
                        if (!s.Contemporary) w.Slab(face, o.U - .4f, o.U + o.W + .4f, 2.5f, 2.62f, -1f, 0f, s.Trim);
                        else if (k % 2 == 0) w.Slab(face, o.U - .3f, o.U + 1.95f, 2.62f, 2.72f, -.9f, 0f, s.Trim);
                        else w.Slab(face, o.U - .85f, o.U + o.W + .3f, 2.62f, 2.72f, -.9f, 0f, s.Trim);
                    }
                    else if (s.Contemporary && o.V > Storey && o.H > 2.5f)
                    {
                        w.Slab(face, o.U, o.U + o.W, o.V + .95f, o.V + 1f, -.08f, 0f, s.Frame);
                    }
                }

                if (s.Panels)
                {
                    for (int f = 1; f < storeys; f++) w.Slab(face, .45f, width - .45f, (f * Storey) - .95f, (f * Storey) + .95f, -.03f, 0f, s.Accent);
                }
            }

            if (back)
            {
                var face = new Face(new Vector3(x + width, y1, z0), -Vector3.UnitX, Vector3.UnitY);
                WideFace(w, s, face, width, H, WideBack(width, storeys, s.Contemporary), paint);
            }
        }

        float d = y1 - y0;
        var windows = new List<Opening>();
        if (d >= 4f && s.Contemporary)
        {
            for (int f = 0; f < storeys; f++) windows.Add(new Opening((d * .5f) - .45f, (f * Storey) + .5f, .9f, 2f, OpeningKind.Window));
        }

        if (left) WideFace(w, s, new Face(new Vector3(x0, y1, z0), -Vector3.UnitY, -Vector3.UnitX), d, H, windows, s.Doors[0]);
        if (right) WideFace(w, s, new Face(new Vector3(x0 + (n * width), y0, z0), Vector3.UnitY, Vector3.UnitX), d, H, windows, s.Doors[0]);
    }

    private static void Parapet(Writer w, WideLook s, float x0, float x1, float y0, float y1, float z, string? material = null)
    {
        const float Height = .45f, Wall = .25f, Over = .03f;
        string wall = material ?? s.Upper;
        w.Box(new Vector3(x0, y0, z - .05f), new Vector3(x1, y1, z + .02f), Deck);
        w.Box(new Vector3(x0, y0, z), new Vector3(x1, y0 + Wall, z + Height), wall);
        w.Box(new Vector3(x0, y1 - Wall, z), new Vector3(x1, y1, z + Height), wall);
        w.Box(new Vector3(x0, y0, z), new Vector3(x0 + Wall, y1, z + Height), wall);
        w.Box(new Vector3(x1 - Wall, y0, z), new Vector3(x1, y1, z + Height), wall);
        float top = z + Height, coping = top + .06f;
        w.Box(new Vector3(x0 - Over, y0 - Over, top), new Vector3(x1 + Over, y0 + Wall + Over, coping), s.Trim);
        w.Box(new Vector3(x0 - Over, y1 - Wall - Over, top), new Vector3(x1 + Over, y1 + Over, coping), s.Trim);
        w.Box(new Vector3(x0 - Over, y0 - Over, top), new Vector3(x0 + Wall + Over, y1 + Over, coping), s.Trim);
        w.Box(new Vector3(x1 - Wall - Over, y0 - Over, top), new Vector3(x1 + Over, y1 + Over, coping), s.Trim);
    }

    /// <summary>A tall window up the face of a front gable standing at <paramref name="y"/>.</summary>
    private static void GableGlass(Writer w, WideLook s, float cx, float y, float half)
    {
        float top = s.H + ((half - .75f) * MathF.Tan(s.Pitch * MathF.PI / 180f)) - .35f, low = s.H + .3f;
        if (top <= s.H + 1f) return;
        w.Box(new Vector3(cx - .75f, y - .03f, low), new Vector3(cx + .75f, y, top), "glass");
        w.Box(new Vector3(cx - .75f, y - .06f, low), new Vector3(cx + .75f, y - .03f, low + .07f), s.Frame);
        w.Box(new Vector3(cx - .75f, y - .06f, top - .07f), new Vector3(cx + .75f, y - .03f, top), s.Frame);
        w.Box(new Vector3(cx - .75f, y - .06f, low), new Vector3(cx - .68f, y - .03f, top), s.Frame);
        w.Box(new Vector3(cx + .68f, y - .06f, low), new Vector3(cx + .75f, y - .03f, top), s.Frame);
    }

    /// <summary>One steep front gable to each house, no eaves, every other house set back from the street.</summary>
    private static void StaggeredGables(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(5.5f, 7f));
        float step = r.Range(1.2f, 2f);
        int first = r.Pick(2);
        for (int i = 0; i < n; i++)
        {
            float x0 = (-s.W / 2f) + (i * width);
            bool back = n > 1 && i % 2 != first;
            float y0 = s.Y0 + (back ? step : 0f);
            WideRow(w, s, 1, width, x0, y0, s.Y1, s.Storeys, left: !back || i == 0, right: !back || i == n - 1, first: i);
            HipGable(w, s.Roof, s.End, alongY: true, x0 + (width / 2f), (y0 + s.Y1) / 2f, width / 2f, (s.Y1 - y0) / 2f, s.H, s.Pitch, 0f,
                front: true, back: true, s.Eaves);
            GableGlass(w, s, x0 + (width / 2f), y0, width / 2f);
        }
    }

    /// <summary>
    /// Flat roofed. At three storeys the top storey stands back behind a roof terrace; below that, framed
    /// boxes stand out round every other upper window.
    /// </summary>
    private static void FlatSetback(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(6f, 8f));
        float X = s.W / 2f;
        int low = s.Storeys >= 3 ? s.Storeys - 1 : s.Storeys;
        float lowH = low * Storey;
        WideRow(w, s, n, width, -X, s.Y0, s.Y1, low);
        Parapet(w, s, -X, X, s.Y0, s.Y1, lowH);
        if (low < s.Storeys)
        {
            float back = r.Range(1.8f, 2.6f);
            WideLook top = s with { Ground = s.Accent, Upper = s.Accent, Windows = UpperWindows.Full };
            WideRow(w, top, n, width, -X, s.Y0 + back, s.Y1, 1, door: false, z0: lowH);
            w.Box(new Vector3(-X - .3f, s.Y0 + back - .4f, lowH + Storey), new Vector3(X + .3f, s.Y1 + .3f, lowH + Storey + .25f), s.Trim);
            w.Box(new Vector3(-X + .3f, s.Y0 + .1f, lowH + .5f), new Vector3(X - .3f, s.Y0 + .14f, lowH + 1.05f), "glass");
            for (int i = 1; i < n; i++)
            {
                float x = -X + (i * width);
                w.Box(new Vector3(x - .04f, s.Y0 + .25f, lowH), new Vector3(x + .04f, s.Y0 + back, lowH + 1.8f), s.Accent);
            }

            return;
        }

        for (int i = 0; i < n; i += 2)
        {
            var face = new Face(new Vector3(-X + (i * width), s.Y0, 0f), Vector3.UnitX, -Vector3.UnitY);
            foreach (Opening o in ContemporaryFront(width, s.Storeys, s.Windows, i % 2 == 1, door: true))
            {
                if (o.V <= Storey) continue;
                float a = o.U, b = o.U + o.W, v = o.V, h = o.V + o.H;
                w.Slab(face, a - .2f, b + .2f, v - .2f, v, -.5f, 0f, s.Accent);
                w.Slab(face, a - .2f, b + .2f, h, h + .2f, -.5f, 0f, s.Accent);
                w.Slab(face, a - .2f, a, v, h, -.5f, 0f, s.Accent);
                w.Slab(face, b, b + .2f, v, h, -.5f, 0f, s.Accent);
                break;
            }
        }
    }

    /// <summary>A mono-pitch to each house, all rising the same way, a clerestory in each step.</summary>
    private static void Sawtooth(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(6f, 7.5f));
        float d = r.Chance(.5f) ? 1f : -1f;
        float rise = width * MathF.Tan(s.Pitch * MathF.PI / 180f), e = s.Eaves, H = s.H, y0 = s.Y0, y1 = s.Y1;
        WideRow(w, s, n, width, -s.W / 2f, y0, y1, s.Storeys);
        for (int i = 0; i < n; i++)
        {
            float a = (-s.W / 2f) + (i * width), b = a + width;
            (float lo, float hi) = d > 0f ? (a, b) : (b, a);
            Sheet(w, s.Roof, [new(lo, y0 - e, H), new(hi, y0 - e, H + rise), new(hi, y1 + e, H + rise), new(lo, y1 + e, H)], Vector3.UnitZ);
            Shape(w, s.End, [new(lo, y0, H), new(hi, y0, H), new(hi, y0, H + rise)], -Vector3.UnitY);
            Shape(w, s.End, [new(lo, y1, H), new(hi, y1, H), new(hi, y1, H + rise)], Vector3.UnitY);
            Shape(w, s.End, [new(hi, y0, H), new(hi, y1, H), new(hi, y1, H + rise), new(hi, y0, H + rise)], new Vector3(d, 0f, 0f));
            int step = d > 0f ? i + 1 : i;
            if (step > 0 && step < n && rise > 1.2f)
            {
                float x = hi + (d * .02f);
                Cuboid(w, new Vector3(x, y0 + .6f, H + .3f), new Vector3(x + (d * .03f), y1 - .6f, H + rise - .3f), "glass");
            }
        }
    }

    /// <summary>Front-gabled pavilions joined by flat-roofed links set back from the street. The doors are in the links.</summary>
    private static void Pavilions(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        float link = r.Range(2.8f, 3.4f), recess = r.Range(1.6f, 2.4f);
        int count = Math.Max(1, (int)MathF.Round((s.W + link) / (r.Range(7f, 8.5f) + link)));
        float pavilion = count > 1 ? (s.W - ((count - 1) * link)) / count : s.W - link;
        int linkStoreys = s.Storeys <= 2 ? s.Storeys : s.Storeys - 1;
        WideLook linked = s with { Ground = s.Accent, Upper = s.Accent, Windows = UpperWindows.Wide };
        float x = -s.W / 2f;
        int k = 0;
        for (int i = 0; i < count; i++)
        {
            WideRow(w, s, 1, pavilion, x, s.Y0, s.Y1, s.Storeys, door: k == 0 && count > 1, first: k);
            HipGable(w, s.Roof, s.End, alongY: true, x + (pavilion / 2f), (s.Y0 + s.Y1) / 2f, pavilion / 2f, (s.Y1 - s.Y0) / 2f, s.H,
                s.Pitch, 0f, front: true, back: true, s.Eaves);
            GableGlass(w, s, x + (pavilion / 2f), s.Y0, pavilion / 2f);
            x += pavilion;
            k++;
            if (i == count - 1 && count > 1) break;
            bool last = i == count - 1;
            WideRow(w, linked, 1, link, x, s.Y0 + recess, s.Y1, linkStoreys, left: false, right: last, first: k);
            Parapet(w, linked, x, x + link, s.Y0 + recess, s.Y1, linkStoreys * Storey);
            x += link;
            k++;
        }
    }

    /// <summary>A shallow side gable over narrow houses, brick cross walls standing forward between them, panels under ribbon windows.</summary>
    private static void Span(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(5.2f, 6.4f));
        float X = s.W / 2f, e = s.Eaves, H = s.H, t = MathF.Tan(s.Pitch * MathF.PI / 180f);
        WideRow(w, s, n, width, -X, s.Y0, s.Y1, s.Storeys);
        HipGable(w, s.Roof, s.End, alongY: false, 0f, (s.Y0 + s.Y1) / 2f, (s.Y1 - s.Y0) / 2f, X, H, s.Pitch, 0f, front: true, back: true, e);
        foreach ((float y, float f) in (ReadOnlySpan<(float, float)>)[(s.Y0, -1f), (s.Y1, 1f)])
        {
            float face = y + (f * e);
            w.Box(new Vector3(-X - e, face - .03f, H - (e * t) - .28f), new Vector3(X + e, face + .03f, H - (e * t) + .02f), s.Trim);
            for (int i = 0; i <= n; i++)
            {
                float x = -X + (i * width);
                Cuboid(w, new Vector3(x - .13f, y, 0f), new Vector3(x + .13f, y + (f * .4f), H - .15f), s.Fin);
            }
        }
    }

    /// <summary>An inverted roof to each house, falling to a valley on the house's center line.</summary>
    private static void Butterfly(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(6.5f, 8f));
        const float E = .45f;
        float t = MathF.Tan(s.Pitch * MathF.PI / 180f), rise = width / 2f * t, H = s.H, y0 = s.Y0, y1 = s.Y1;
        WideRow(w, s, n, width, -s.W / 2f, y0, y1, s.Storeys);
        for (int i = 0; i < n; i++)
        {
            float a = (-s.W / 2f) + (i * width), b = a + width, c = (a + b) / 2f;
            float ea = i == 0 ? E : 0f, eb = i == n - 1 ? E : 0f;
            float za = H + rise + (ea * t), zb = H + rise + (eb * t);
            Sheet(w, s.Roof, [new(a - ea, y0 - E, za), new(c, y0 - E, H), new(c, y1 + E, H), new(a - ea, y1 + E, za)], Vector3.UnitZ);
            Sheet(w, s.Roof, [new(c, y0 - E, H), new(b + eb, y0 - E, zb), new(b + eb, y1 + E, zb), new(c, y1 + E, H)], Vector3.UnitZ);
            foreach (float y in (ReadOnlySpan<float>)[y0, y1])
            {
                var outward = new Vector3(0f, MathF.Sign(y - ((y0 + y1) / 2f)), 0f);
                Shape(w, s.End, [new(a, y, H), new(c, y, H), new(a, y, H + rise)], outward);
                Shape(w, s.End, [new(c, y, H), new(b, y, H), new(b, y, H + rise)], outward);
            }

            w.Box(new Vector3(a - ea, y0 - E - .04f, H - .2f), new Vector3(b + eb, y0 - E, H + .02f), s.Trim);
        }

        foreach (float x in (ReadOnlySpan<float>)[-s.W / 2f, s.W / 2f])
        {
            Shape(w, s.End, [new(x, y0, H), new(x, y1, H), new(x, y1, H + rise), new(x, y0, H + rise)], new Vector3(MathF.Sign(x), 0f, 0f));
        }
    }

    /// <summary>One low mono-pitch rising toward the street, a clerestory band of timber and glass under its high edge.</summary>
    private static void Monopitch(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(5.5f, 7f));
        float X = s.W / 2f, H = s.H, y0 = s.Y0, y1 = s.Y1, depth = y1 - y0;
        float t = MathF.Min(MathF.Tan(s.Pitch * MathF.PI / 180f), 3f / depth), rise = depth * t;
        WideRow(w, s, n, width, -X, y0, y1, s.Storeys);
        for (int i = 0; i < n; i++)
        {
            var face = new Face(new Vector3(-X + (i * width), y0, H), Vector3.UnitX, -Vector3.UnitY);
            List<Opening> openings = rise > 1.2f ? [new(.4f, .3f, width - .8f, rise - .6f, OpeningKind.Window)] : [];
            w.Facade(face, width, rise, openings, s.Accent, s.Trim, s.Doors[0], s.Frame);
        }

        foreach (float x in (ReadOnlySpan<float>)[-X, X])
        {
            Shape(w, s.End, [new(x, y0, H), new(x, y1, H), new(x, y0, H + rise)], new Vector3(MathF.Sign(x), 0f, 0f));
        }

        const float Fore = .7f, Aft = .35f;
        float high = H + rise + (Fore * t);
        Sheet(w, s.Roof, [new(-X - .3f, y0 - Fore, high), new(X + .3f, y0 - Fore, high), new(X + .3f, y1 + Aft, H - (Aft * t)),
            new(-X - .3f, y1 + Aft, H - (Aft * t))], Vector3.UnitZ);
        w.Box(new Vector3(-X - .3f, y0 - Fore - .04f, high - .3f), new Vector3(X + .3f, y0 - Fore, high + .02f), s.Trim);
        for (int i = 0; i <= n; i++)
        {
            float x = -X + (i * width);
            w.Box(new Vector3(x - .13f, y0 - .4f, 0f), new Vector3(x + .13f, y0, H - .1f), s.Fin);
        }
    }

    /// <summary>A low hipped roof with deep eaves over the whole row, the upper storey hung with tiles or boards and a canopy along the ground floor.</summary>
    private static void LowHip(Writer w, WideLook s)
    {
        HouseDraw r = Proportions(s);
        (int n, float width) = WideHouses(s.W, r.Range(6f, 7.5f));
        float X = s.W / 2f;
        WideRow(w, s with { Upper = s.Accent, Panels = false }, n, width, -X, s.Y0, s.Y1, s.Storeys);
        HipGable(w, s.Roof, s.End, alongY: false, 0f, (s.Y0 + s.Y1) / 2f, (s.Y1 - s.Y0) / 2f, X, s.H, s.Pitch, 1f, front: true, back: true, .75f);
        w.Box(new Vector3(-X + .2f, s.Y0 - 1f, 2.55f), new Vector3(X - .2f, s.Y0, 2.67f), s.Trim);
    }

    /// <summary>The street form over a double-pile front, flat-roofed outriggers running back to the end of the Lot.</summary>
    private static void DeepWide(Writer w, WideLook s, HouseVariant street)
    {
        float pile = 2f * Math.Clamp(s.D * .37f, 8f, 13f);
        float back = s.Y0 + pile, end = s.D / 2f;
        StreetWide(w, s with { Y1 = back }, street);
        if (end - back <= 1.5f) return;

        int storeys = Math.Max(1, s.Storeys - 1);
        (int n, float width) = WideHouses(s.W, 6.5f);
        for (int k = 1; k < n; k += 2) Outrigger((k * width) - (width / 2f), (k * width) + (width / 2f));
        if (n % 2 == 1) Outrigger((n * width) - (width / 2f), n * width);

        void Outrigger(float a, float b)
        {
            float x0 = (-s.W / 2f) + a, x1 = (-s.W / 2f) + b;
            WideRow(w, s, 1, x1 - x0, x0, back - .01f, end, storeys, front: false);
            Parapet(w, s, x0, x1, back - .01f, end, storeys * Storey);
        }
    }
}
