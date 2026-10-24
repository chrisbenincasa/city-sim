using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The house forms. Every proportion, material and paint is drawn from a seed, so a street of one form
/// is still a street of different houses.
/// </summary>
/// <remarks>
/// It follows <c>scripts/art/house-families.py</c>. A part name carries its dressing: <c>wall@bricks-088#d6cfbf</c>
/// is the wall part in a library texture painted d6cfbf, and <c>trim#eeeeea</c> is the library model's
/// own trim painted eeeeea. Surfaces split on the texture only; the paint goes to vertex colour.
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>How many different draws a house body is built from. The shell shares a mesh between equal seeds.</summary>
    public const int HouseSeeds = 256;

    /// <summary>A villa needs this much frontage for its bays.</summary>
    public const float VillaLeastFrontage = 24f;

    /// <summary>The library textures a house body can be drawn in.</summary>
    public static readonly string[] HouseTextures =
    [
        "bricks-088", "bricks-051", "bricks-066", "bricks-085", "painted-plaster-wall", "plaster-001",
        "clay-roof-tiles-02", "roofing-tiles-007", "roofing-tiles-008",
    ];

    private static readonly string[] HouseWalls = ["d6cfbf", "c9b79c", "b9ad97", "8e9b86", "a7aeb0", "c4a48a", "d3c6a0", "9aa7b4", "b48f7a", "cfd3c6"];
    private static readonly string[] HouseShingles = ["353e44", "4d463f", "5a3a32", "3d4a52"];
    private static readonly string[] HouseTrims = ["d6cfbf", "eeeeea", "4a4f52", "c9bfa8"];
    private static readonly string[] HouseFrames = ["f0efe9", "2f4a3c", "33373a", "e6dcc4", "6a2f2a"];
    private static readonly string[] HouseDoors = ["2f4a5c", "6a2f2a", "2f4a3c", "4f5253", "7a5a2c", "24303a", "8a7a3a"];
    private static readonly string[] NaturalBricks = ["bricks-051", "bricks-066", "bricks-085"];
    private static readonly string[] Renders = ["painted-plaster-wall", "plaster-001"];

    private const float WindowSill = .75f;
    private const float DoorSill = .3f;
    private const float VillaSetBack = 1.5f;
    private static readonly float WindowHeight = MathF.Min(1.9f, Storey - 1.3f);
    private static readonly float DoorHeight = MathF.Min(2.75f, Storey - .75f);

    /// <param name="seed">Which draw, from 0 to <see cref="HouseSeeds"/>.</param>
    public static FamilyBodyMesh BuildHouse(FamilyBody body, float frontage, float depth, int storeys, int seed)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.House is not { } house) throw new ArgumentException("Only a house body is built with BuildHouse().", nameof(body));
        if (house.Variant == HouseVariant.Villa && frontage < VillaLeastFrontage)
        {
            throw new ArgumentOutOfRangeException(nameof(frontage), frontage, $"A villa needs {VillaLeastFrontage} m of frontage.");
        }

        var mesh = new FamilyBodyMesh();
        var writer = new Writer(mesh, body);
        HouseLook look = HouseLook.Draw(house.Variant, seed, frontage, depth, storeys);
        switch (house.Variant)
        {
            case HouseVariant.Terrace:
                Terrace(writer, look);
                break;
            case HouseVariant.FrontGables:
                FrontGables(writer, look);
                break;
            case HouseVariant.Villa:
                Villa(writer, look);
                break;
            default:
                Detached(writer, look, house.Variant);
                break;
        }

        return mesh;
    }

    /// <summary>A part name with its dressing; see the class remarks.</summary>
    internal static string Dressed(string part, string? texture, string? paint) =>
        (texture is null ? part : $"{part}@{texture}") + (paint is null ? string.Empty : $"#{paint}");

    private enum WallScheme
    {
        PaintedBrick,
        Brick,
        Render,
        BrickAndRender,
    }

    private sealed record HouseModule(float Width, string Wall, string Door, bool Mirror);

    private sealed class HouseLook
    {
        public float W, D, H;
        public int Storeys;
        public required string Roof, Trim, Frame, End;
        public string? Ground;
        public float Pitch, WindowWidth;
        public List<HouseModule> Modules = [];
        public bool PairedAttic;
        public string Door = string.Empty, BayWall = string.Empty;
        public int Bays;
        public float BayWidth, Spacing, BayPitch;
        public float WingPitch;
        public int Side;
        public bool Dormer;

        public static readonly string Plinth = Dressed("plinth", null, "9d988e");

        public string Stack => Ground ?? End;

        public static HouseLook Draw(HouseVariant form, int seed, float frontage, float depth, int storeys)
        {
            var r = new HouseDraw((ulong)seed);
            string roof = r.Pick(4) switch
            {
                0 => Dressed("roof", "clay-roof-tiles-02", null),
                1 => Dressed("roof", "roofing-tiles-007", null),
                2 => Dressed("roof", "roofing-tiles-008", null),
                _ => Dressed("roof", null, r.Pick(HouseShingles)),
            };
            var scheme = (WallScheme)r.Pick(4);
            string natural = r.Pick(NaturalBricks);
            bool perHouse = r.Chance(.65f) && form != HouseVariant.Villa;
            string shared = r.Pick(HouseWalls);

            string Wall()
            {
                string paint = perHouse ? r.Pick(HouseWalls) : shared;
                return scheme switch
                {
                    WallScheme.PaintedBrick => Dressed("wall", "bricks-088", paint),
                    WallScheme.Brick => Dressed("wall", natural, null),
                    WallScheme.Render => Dressed("wall", r.Pick(Renders), paint),
                    _ => Dressed("wall", "painted-plaster-wall", paint),
                };
            }

            var look = new HouseLook
            {
                W = frontage,
                D = depth,
                H = storeys * Storey,
                Storeys = storeys,
                Roof = roof,
                Ground = scheme == WallScheme.BrickAndRender ? Dressed("wall", natural, null) : null,
                Trim = Dressed("trim", null, r.Pick(HouseTrims)),
                Frame = Dressed("frame", null, r.Pick(HouseFrames)),
                WindowWidth = r.Range(1.0f, 1.3f),
                End = Wall(),
            };

            if (form == HouseVariant.Villa)
            {
                look.Pitch = r.Range(24f, 34f);
                look.BayWall = r.Chance(.5f) ? Wall()
                    : scheme != WallScheme.Brick ? Dressed("wall", "bricks-088", r.Pick(HouseWalls))
                    : look.End;
                look.Bays = frontage < 36f ? 1 : frontage >= 48f ? r.Pick([2, 2, 3]) : 2;
                look.BayWidth = r.Range(5.5f, 7.5f);
                look.Spacing = r.Range(3.2f, 4.0f);
                look.BayPitch = r.Range(35f, 50f);
                look.Door = Dressed("door", null, r.Pick(HouseDoors));
                return look;
            }

            look.Pitch = form switch
            {
                HouseVariant.Terrace => r.Range(22f, 40f),
                HouseVariant.FrontGables => r.Range(30f, 45f),
                HouseVariant.Hipped => r.Range(22f, 35f),
                HouseVariant.HalfHip => r.Range(38f, 48f),
                HouseVariant.CrossGable => r.Range(28f, 40f),
                HouseVariant.Saltbox => r.Range(32f, 42f),
                _ => r.Range(30f, 40f),
            };
            int count = Math.Max(2, (int)MathF.Round(frontage / r.Range(6.0f, 8.5f)));
            if (frontage < SingleHouseFrontage || IsDetached(form)) count = 1;
            bool pairs = r.Chance(.5f);
            for (int i = 0; i < count; i++)
            {
                look.Modules.Add(new HouseModule(frontage / count, Wall(), Dressed("door", null, r.Pick(HouseDoors)), pairs && i % 2 == 1));
            }

            look.PairedAttic = r.Chance(.5f);
            if (IsDetached(form))
            {
                look.WingPitch = r.Range(40f, 45f);
                look.Side = r.Chance(.5f) ? 1 : -1;
                look.Dormer = r.Chance(.6f);
            }

            return look;
        }
    }

    /// <summary>SplitMix64 over a house seed. Shell-only, so floats are fine here.</summary>
    private struct HouseDraw(ulong seed)
    {
        private ulong _state = (seed * 0x9E3779B97F4A7C15UL) ^ 0x5DEECE66DUL;

        private ulong Next()
        {
            ulong z = _state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private float Unit() => (Next() >> 40) / (float)(1UL << 24);

        public float Range(float low, float high) => low + ((high - low) * Unit());

        public bool Chance(float p) => Unit() < p;

        public int Pick(int count) => (int)(Next() % (ulong)count);

        public T Pick<T>(T[] items) => items[Pick(items.Length)];
    }

    private static void Terrace(Writer w, HouseLook s)
    {
        float W = s.W, D = s.D, H = s.H;
        float x = -W / 2f;
        foreach (HouseModule m in s.Modules)
        {
            var front = new Face(new Vector3(x, -D / 2f, 0f), Vector3.UnitX, -Vector3.UnitY);
            Elevation(w, s, front, m.Width, HouseFront(m, s), m.Wall, m.Door);
            Elevation(w, s, new Face(new Vector3(x + m.Width, D / 2f, 0f), -Vector3.UnitX, Vector3.UnitY), m.Width, HouseBack(m, s), m.Wall, m.Door);
            x += m.Width;
        }

        HouseEnds(w, s, D, s.End);
        HousePlinth(w, s);

        float slope = MathF.Tan(s.Pitch * MathF.PI / 180f);
        const float Eaves = .4f, Thick = .2f;
        float half = (W / 2f) + (Eaves / 2f), y = (D / 2f) + Eaves;
        float low = H - (Eaves * slope), top = H + (D / 2f * slope);
        ExtrudeX(w, s.Roof, [new(-y, low), new(0f, top), new(0f, top + Thick), new(-y, low + Thick)], -half, half);
        ExtrudeX(w, s.Roof, [new(0f, top), new(y, low), new(y, low + Thick), new(0f, top + Thick)], -half, half);
        foreach (float sign in (ReadOnlySpan<float>)[-1f, 1f])
        {
            float gx = sign * W / 2f;
            Shape(w, s.End, [new(gx, -D / 2f, H), new(gx, D / 2f, H), new(gx, 0f, top)], new Vector3(sign, 0f, 0f));
        }

        float Roof(float distance) => top + Thick - (MathF.Abs(distance) * slope);
        float edge = (D / 2f) + .15f;
        x = -W / 2f;
        for (int i = 0; i < s.Modules.Count; i++)
        {
            if (i > 0)
            {
                ExtrudeX(w, s.Stack,
                [
                    new(-edge, H - .3f), new(edge, H - .3f), new(edge, Roof(edge) + .25f),
                    new(0f, Roof(0f) + .25f), new(-edge, Roof(edge) + .25f),
                ], x - .15f, x + .15f);
                w.Box(new Vector3(x - .45f, .4f, Roof(.9f) - .6f), new Vector3(x + .45f, 1.3f, Roof(0f) + 1.1f), s.Stack);
            }

            x += s.Modules[i].Width;
        }
    }

    private static void FrontGables(Writer w, HouseLook s)
    {
        float W = s.W, D = s.D, H = s.H;
        float tan = MathF.Tan(s.Pitch * MathF.PI / 180f);
        const float Eaves = .35f, Thick = .2f;
        float x = -W / 2f;
        foreach (HouseModule m in s.Modules)
        {
            float width = m.Width;
            var front = new Face(new Vector3(x, -D / 2f, 0f), Vector3.UnitX, -Vector3.UnitY);
            Elevation(w, s, front, width, HouseFront(m, s), m.Wall, m.Door);
            Elevation(w, s, new Face(new Vector3(x + width, D / 2f, 0f), -Vector3.UnitX, Vector3.UnitY), width, HouseBack(m, s), m.Wall, m.Door);

            float rise = width / 2f * tan, cx = x + (width / 2f), top = H + rise;
            foreach (float sign in (ReadOnlySpan<float>)[-1f, 1f])
            {
                float gy = sign * D / 2f;
                Shape(w, m.Wall, [new(x, gy, H), new(x + width, gy, H), new(cx, gy, top)], new Vector3(0f, sign, 0f));
            }

            float low = H - (Eaves * tan);
            ChevronY(w, s.Roof, x - Eaves, cx, x + width + Eaves, low, top, 0f, Thick, (-D / 2f) - .4f, (D / 2f) + .4f);
            ChevronY(w, s.Trim, x - Eaves - .02f, cx, x + width + Eaves + .02f, low, top, -.12f, .02f, (-D / 2f) - .48f, (-D / 2f) - .38f);

            float light = MathF.Min(1.3f, rise * .45f);
            ReadOnlySpan<float> lights = s.PairedAttic ? [cx - .95f, cx + .15f] : [cx - .45f];
            float lightWidth = s.PairedAttic ? .8f : .9f;
            foreach (float a in lights)
            {
                w.Box(new Vector3(a, (-D / 2f) - .03f, H + .45f), new Vector3(a + lightWidth, -D / 2f, H + .45f + light), "glass");
                w.Box(new Vector3(a - .08f, (-D / 2f) - .1f, H + .38f), new Vector3(a + lightWidth + .08f, -D / 2f, H + .45f), s.Trim);
            }

            float stack = cx + (width * .18f);
            w.Box(new Vector3(stack - .4f, D * .15f, H + (rise * .4f)), new Vector3(stack + .4f, (D * .15f) + .8f, top + 1f), s.Stack);
            x += width;
        }

        HouseEnds(w, s, D, s.End);
        HousePlinth(w, s);
    }

    private static void Villa(Writer w, HouseLook s)
    {
        float W = s.W, D = s.D, H = s.H;
        float d = D - VillaSetBack, yFront = (-D / 2f) + VillaSetBack;
        int n = Math.Max(1, (int)MathF.Round(W / s.Spacing));
        float[] columns = [.. Enumerable.Range(0, n).Select(i => (-W / 2f) + (W * (i + .5f) / n))];
        float bay = s.BayWidth;
        float[] centres = s.Bays switch
        {
            1 => [0f],
            2 => [-W / 4f, W / 4f],
            _ => [-W / 3f, 0f, W / 3f],
        };
        float doorX = centres.Length > 1 ? (centres[0] + centres[1]) / 2f : -W / 4f;

        float start = -W / 2f;
        foreach ((float a, float b) in centres.Select(c => (c - (bay / 2f), c + (bay / 2f))).Append((W / 2f, W / 2f)))
        {
            if (a - start > 1f) VillaFront(start, a);
            start = b;
        }

        var back = new List<Opening>();
        foreach (float c in columns)
        {
            for (int k = 0; k < s.Storeys; k++) back.Add(Window((W / 2f) - c - (s.WindowWidth / 2f), k, s.WindowWidth));
        }

        Elevation(w, s, new Face(new Vector3(W / 2f, D / 2f, 0f), -Vector3.UnitX, Vector3.UnitY), W, back, s.End, s.Door);
        Elevation(w, s, new Face(new Vector3(-W / 2f, D / 2f, 0f), -Vector3.UnitY, -Vector3.UnitX), d, Ends(d, s), s.End, s.Door);
        Elevation(w, s, new Face(new Vector3(W / 2f, yFront, 0f), Vector3.UnitY, Vector3.UnitX), d, Ends(d, s), s.End, s.Door);

        const float Eaves = .45f;
        float tan = MathF.Tan(s.Pitch * MathF.PI / 180f);
        float y0 = yFront - Eaves, y1 = (D / 2f) + Eaves, cy = (y0 + y1) / 2f, half = (y1 - y0) / 2f;
        float x0 = (-W / 2f) - Eaves, x1 = (W / 2f) + Eaves;
        float low = H - (Eaves * tan), ridge = low + (half * tan);
        Vector3 r0 = new(MathF.Min(x0 + half, 0f), cy, ridge), r1 = new(MathF.Max(x1 - half, 0f), cy, ridge);
        Vector3 c0 = new(x0, y0, low), c1 = new(x1, y0, low), c2 = new(x1, y1, low), c3 = new(x0, y1, low);
        Vector3 middle = (c0 + c1 + c2 + c3 + r0 + r1) / 6f;
        Hinted([c0, c1, r1, r0]);
        Hinted([c2, c3, r0, r1]);
        Hinted([c3, c0, r0]);
        Hinted([c1, c2, r1]);
        Hinted([c3, c2, c1, c0]);

        float bayTan = MathF.Tan(s.BayPitch * MathF.PI / 180f);
        foreach (float c in centres)
        {
            float bx0 = c - (bay / 2f);
            var openings = new List<Opening>();
            for (int k = 0; k < s.Storeys; k++) openings.Add(Window((bay / 2f) - 1.4f, k, 2.8f));
            Elevation(w, s, new Face(new Vector3(bx0, -D / 2f, 0f), Vector3.UnitX, -Vector3.UnitY), bay, openings, s.BayWall, s.Door);
            w.Box(new Vector3(bx0 - .1f, -D / 2f, 0f), new Vector3(bx0 + .1f, yFront, H), s.BayWall);
            w.Box(new Vector3(bx0 + bay - .1f, -D / 2f, 0f), new Vector3(bx0 + bay + .1f, yFront, H), s.BayWall);

            float rise = bay / 2f * bayTan, top = H + rise;
            Shape(w, s.BayWall, [new(bx0, -D / 2f, H), new(bx0 + bay, -D / 2f, H), new(c, -D / 2f, top)], -Vector3.UnitY);
            const float BayEaves = .3f;
            float bayLow = H - (BayEaves * bayTan);
            float reach = MathF.Min((D / 2f) + yFront + ((ridge - H) / tan) + 1f, D * .6f);
            ChevronY(w, s.Roof, bx0 - BayEaves, c, bx0 + bay + BayEaves, bayLow, top, 0f, .2f, (-D / 2f) - .35f, (-D / 2f) + reach);
            ChevronY(w, s.Trim, bx0 - BayEaves - .02f, c, bx0 + bay + BayEaves + .02f, bayLow, top, -.12f, .02f, (-D / 2f) - .43f, (-D / 2f) - .33f);
            w.Box(new Vector3(c - .5f, (-D / 2f) - .03f, H + .5f), new Vector3(c + .5f, -D / 2f, H + .5f + MathF.Min(1.4f, rise * .5f)), "glass");
        }

        foreach (float sx in (ReadOnlySpan<float>)[(-W / 2f) + (W * .28f), (W / 2f) - (W * .28f)])
        {
            w.Box(new Vector3(sx - .45f, cy - .45f, ridge - 1.2f), new Vector3(sx + .45f, cy + .45f, ridge + 1.1f), s.Stack);
        }

        HousePlinth(w, s);

        void VillaFront(float a, float b)
        {
            var openings = new List<Opening>();
            foreach (float c in columns)
            {
                if (!(a + .9f < c && c < b - .9f)) continue;
                for (int k = MathF.Abs(c - doorX) > 1.3f ? 0 : 1; k < s.Storeys; k++)
                {
                    openings.Add(Window(c - a - (s.WindowWidth / 2f), k, s.WindowWidth));
                }
            }

            if (a < doorX && doorX < b) openings.Add(new Opening(doorX - a - .55f, DoorSill, 1.1f, DoorHeight, OpeningKind.Door));
            Elevation(w, s, new Face(new Vector3(a, yFront, 0f), Vector3.UnitX, -Vector3.UnitY), b - a, openings, s.End, s.Door);
        }

        void Hinted(ReadOnlySpan<Vector3> points)
        {
            Vector3 centre = Vector3.Zero;
            foreach (Vector3 p in points) centre += p;
            Shape(w, s.Roof, points, (centre / points.Length) - middle);
        }
    }

    private static Opening Window(float u, int storey, float width) =>
        new(u, (storey * Storey) + WindowSill, width, WindowHeight, OpeningKind.Window);

    private static List<Opening> HouseFront(HouseModule m, HouseLook s, int? storeys = null)
    {
        float w = m.Width;
        int levels = storeys ?? s.Storeys;
        float Flip(float u, float width) => m.Mirror ? w - u - width : u;
        var openings = new List<Opening> { new(Flip(.7f, 1f), DoorSill, 1f, DoorHeight, OpeningKind.Door) };
        float ground = MathF.Min(s.WindowWidth * 1.5f, w * .3f);
        openings.Add(new Opening(Flip((w * .62f) - (ground / 2f), ground), WindowSill, ground, WindowHeight, OpeningKind.Window));
        ReadOnlySpan<float> centres = w >= 6.5f ? [w * .28f, w * .72f] : [w * .6f];
        for (int k = 1; k < levels; k++)
        {
            foreach (float c in centres) openings.Add(Window(Flip(c - (s.WindowWidth / 2f), s.WindowWidth), k, s.WindowWidth));
        }

        return openings;
    }

    private static List<Opening> HouseBack(HouseModule m, HouseLook s, int? storeys = null)
    {
        float w = m.Width;
        var openings = new List<Opening>
        {
            new((w * .25f) - .5f, DoorSill, 1f, 2.5f, OpeningKind.Door),
            Window((w * .7f) - (s.WindowWidth / 2f), 0, s.WindowWidth),
        };
        for (int k = 1; k < (storeys ?? s.Storeys); k++)
        {
            foreach (float c in (ReadOnlySpan<float>)[.3f, .7f]) openings.Add(Window((w * c) - (s.WindowWidth / 2f), k, s.WindowWidth));
        }

        return openings;
    }

    private static List<Opening> Ends(float depth, HouseLook s, int? storeys = null)
    {
        var openings = new List<Opening>();
        for (int k = 0; k < (storeys ?? s.Storeys); k++)
        {
            foreach (float c in (ReadOnlySpan<float>)[.3f, .7f]) openings.Add(Window((depth * c) - .5f, k, 1f));
        }

        return openings;
    }

    private static void HouseEnds(Writer w, HouseLook s, float depth, string wall)
    {
        float W = s.W, D = s.D;
        Elevation(w, s, new Face(new Vector3(-W / 2f, D / 2f, 0f), -Vector3.UnitY, -Vector3.UnitX), depth, Ends(depth, s), wall, s.Trim);
        Elevation(w, s, new Face(new Vector3(W / 2f, -D / 2f, 0f), Vector3.UnitY, Vector3.UnitX), depth, Ends(depth, s), wall, s.Trim);
    }

    private static void HousePlinth(Writer w, HouseLook s) =>
        w.Box(new Vector3((-s.W / 2f) - .05f, (-s.D / 2f) - .05f, 0f), new Vector3((s.W / 2f) + .05f, (s.D / 2f) + .05f, PlinthHeight), HouseLook.Plinth);

    /// <summary>A wall with its openings, split at the first floor where the ground storey is brick.</summary>
    private static void Elevation(Writer w, HouseLook s, Face face, float width, List<Opening> openings, string wall, string door, float? height = null)
    {
        float h = height ?? s.H;
        if (s.Ground is { } ground && h > Storey)
        {
            w.Facade(face, width, Storey, openings.FindAll(o => o.V < Storey), ground, s.Trim, door, s.Frame);
            var upper = new Face(face.Point(0f, Storey), face.Along, face.Outward);
            w.Facade(upper, width, h - Storey, [.. openings.Where(o => o.V >= Storey).Select(o => o with { V = o.V - Storey })], wall, s.Trim, door, s.Frame);
        }
        else
        {
            w.Facade(face, width, h, openings, wall, s.Trim, door, s.Frame);
        }
    }

    /// <summary>One slope of a pitched roof on each side of the apex, each a convex slab running along Y.</summary>
    private static void ChevronY(Writer w, string part, float left, float apex, float right, float low, float top,
        float below, float above, float y0, float y1)
    {
        ExtrudeY(w, part, [new(left, low + below), new(apex, top + below), new(apex, top + above), new(left, low + above)], y0, y1);
        ExtrudeY(w, part, [new(apex, top + below), new(right, low + below), new(right, low + above), new(apex, top + above)], y0, y1);
    }

    /// <summary>A solid from <paramref name="x0"/> to <paramref name="x1"/> over a convex (y, z) profile in either winding.</summary>
    private static void ExtrudeX(Writer w, string part, ReadOnlySpan<Vector2> profile, float x0, float x1)
    {
        Span<Vector3> cap = stackalloc Vector3[profile.Length];
        for (int i = 0; i < profile.Length; i++) cap[i] = new Vector3(x0, profile[i].X, profile[i].Y);
        Extrude(w, part, cap, new Vector3(x1 - x0, 0f, 0f));
    }

    /// <summary>A solid from <paramref name="y0"/> to <paramref name="y1"/> over a convex (x, z) profile in either winding.</summary>
    private static void ExtrudeY(Writer w, string part, ReadOnlySpan<Vector2> profile, float y0, float y1)
    {
        Span<Vector3> cap = stackalloc Vector3[profile.Length];
        for (int i = 0; i < profile.Length; i++) cap[i] = new Vector3(profile[i].X, y0, profile[i].Y);
        Extrude(w, part, cap, new Vector3(0f, y1 - y0, 0f));
    }

    private static void Extrude(Writer w, string part, ReadOnlySpan<Vector3> cap, Vector3 offset)
    {
        int n = cap.Length;
        Vector3 centre = offset / 2f;
        foreach (Vector3 p in cap) centre += p / n;
        Span<Vector3> far = stackalloc Vector3[n];
        for (int i = 0; i < n; i++) far[i] = cap[i] + offset;
        Shape(w, part, cap, -offset);
        Shape(w, part, far, offset);
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            Vector3 mid = (cap[i] + cap[j] + far[i] + far[j]) / 4f;
            Shape(w, part, [cap[i], cap[j], far[j], far[i]], mid - centre);
        }
    }

    /// <summary>A convex polygon in either winding, turned to face <paramref name="outward"/>.</summary>
    private static void Shape(Writer w, string part, ReadOnlySpan<Vector3> corners, Vector3 outward)
    {
        Span<Vector3> distinct = stackalloc Vector3[corners.Length];
        int count = 0;
        foreach (Vector3 corner in corners)
        {
            if (count == 0 || corner != distinct[count - 1]) distinct[count++] = corner;
        }

        if (count > 1 && distinct[count - 1] == distinct[0]) count--;
        if (count < 3) return;
        ReadOnlySpan<Vector3> points = distinct[..count];
        Vector3 normal = Vector3.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 a = points[i], b = points[(i + 1) % points.Length];
            normal += new Vector3((a.Y - b.Y) * (a.Z + b.Z), (a.Z - b.Z) * (a.X + b.X), (a.X - b.X) * (a.Y + b.Y));
        }

        if (Vector3.Dot(normal, outward) >= 0f)
        {
            w.Polygon(part, points);
            return;
        }

        Span<Vector3> reversed = stackalloc Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) reversed[i] = points[points.Length - 1 - i];
        w.Polygon(part, reversed);
    }
}
