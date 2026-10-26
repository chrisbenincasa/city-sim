using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The dressed workshop forms, each one period's roof and plan. They were authored in
/// <c>scripts/art/workshop-forms.py</c>, and this file is their reference.
/// </summary>
/// <remarks>
/// A run shares its form, pitch, walls, roof and trim through the run's draw. Each Building draws its
/// own doors, their paint, its sign and its layout from its own draw. The builder's yard leaves its
/// cart shed's ground storey open 2.4 m deep, about 5% of a two-storey Building's floor, inside the
/// drawing's 20% (<c>docs/07-the-drawing.md</c>). The panel shed is the block body that
/// <see cref="Build"/> draws.
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>The library textures a dressed workshop body can be drawn in.</summary>
    public static readonly string[] WorkshopTextures =
    [
        "brick-wall-001", "bricks-051", "bricks-066", "bricks-085", "bricks-088", "painted-plaster-wall", "sheet-metal-004",
        "corrugated-iron-02", "rough-block-wall", "wood-siding-005", "wood-siding-012", "clay-roof-tiles-02", "roofing-tiles-003",
        "bitumen",
    ];

    /// <summary>Whether a workshop form draws a pitched roof, and so takes a pitch.</summary>
    internal static bool WorkshopPitched(WorkshopForm form) =>
        form is WorkshopForm.BuildersYard or WorkshopForm.Monitor or WorkshopForm.PortalShed;

    /// <summary>The walls and trim a run draws from, and its roof covering.</summary>
    private sealed record WorkshopPeriod(string[] Walls, string[] Trims, string? Roof, float Pitch);

    private static readonly Dictionary<WorkshopForm, WorkshopPeriod> WorkshopPeriods = new()
    {
        [WorkshopForm.BuildersYard] = new(["bricks-085", "brick-wall-001"], ["e3dccb", "d8d2c4"], "clay-roof-tiles-02", 26f),
        [WorkshopForm.Monitor] = new(["bricks-051", "bricks-085", "brick-wall-001"], ["d8d2c4", "c9c6bd"], "roofing-tiles-003", 18f),
        [WorkshopForm.InterwarGarage] = new(
            ["painted-plaster-wall#d6d0c0", "painted-plaster-wall#d3d6c8", "painted-plaster-wall#d9cfb4"], ["3a4a3c", "6a2f2a", "24303a"],
            "corrugated-iron-02#4f5b5f", 0f),
        [WorkshopForm.ParapetRoller] = new(["bricks-066", "bricks-051", "bricks-088#8a6a52"], ["c9c6bd", "b9b6ad"], null, 0f),
        [WorkshopForm.PortalShed] = new(
            ["sheet-metal-004#6f7f86", "sheet-metal-004#8a9599", "sheet-metal-004#5c6b5a", "sheet-metal-004#b8b8b0"], ["2f4a5c", "6a2f2a", "3a4a3c"],
            "sheet-metal-004#5d6266", 8f),
    };

    private static readonly string[] WorkshopDoors = ["2f4a3c", "6a2f2a", "24303a", "2e5a5a", "8a7a3a", "4f5253", "7a3b52", "3a4a6a", "5a3a32"];
    private static readonly string[] WorkshopRollers = ["8d9194", "5d6a6e", "2f4a5c", "6a2f2a", "b9b6ad", "3a4a3c", "a8923e", "4f5b5f"];
    private static readonly string[] WorkshopSigns = ["24303a", "6a2f2a", "2f4a3c", "d9d0b8", "1f2124", "2e5a5a"];
    private static readonly string[] WorkshopBands = ["c9c6bd", "bdb9ae"];
    private static readonly string[] WorkshopBoards = ["wood-siding-005", "wood-siding-012"];
    private static readonly float[] GarageCentres = [.5f, .5f, .3f, .7f];
    private static readonly float[] GarageDoors = [5f, 5.6f, 6.2f];
    private static readonly float[] RollerWidths = [4f, 4.5f];
    private static readonly int[] WideRollers = [1, 2, 2, 3];
    private static readonly int[] NarrowRollers = [1, 2];

    /// <param name="seed">The Building's own draw, out of <see cref="HouseSeeds"/>; it picks the doors, the sign and the layout.</param>
    /// <param name="run">The run's draw, out of <see cref="HouseSeeds"/>; it picks the walls and trim.</param>
    public static FamilyBodyMesh BuildWorkshop(FamilyBody body, float frontage, float depth, int storeys, AttachedSides attached,
        int seed, int run, TerracePlace place = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.Workshop is not { Form: not WorkshopForm.PanelShed } workshop)
        {
            throw new ArgumentException("Only a dressed workshop body is built with BuildWorkshop().", nameof(body));
        }

        WorkshopForm form = workshop.Form;
        WorkshopPeriod period = WorkshopPeriods[form];
        var shared = new HouseDraw((ulong)run);
        string walls = Specified("wall", shared.Pick(period.Walls));
        string trim = Dressed("trim", null, shared.Pick(period.Trims));
        string band = Dressed("trim", null, shared.Pick(WorkshopBands));
        string board = Dressed("wall", shared.Pick(WorkshopBoards), null);
        var own = new HouseDraw((ulong)seed);
        bool rolls = form is WorkshopForm.InterwarGarage or WorkshopForm.ParapetRoller or WorkshopForm.PortalShed;
        var look = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["wall"] = walls,
            ["wall-end"] = walls,
            ["roof"] = Specified("roof", period.Roof ?? "roofing-tiles-003"),
            ["trim"] = trim,
            ["membrane"] = Dressed("membrane", "bitumen", null),
            ["metal"] = Dressed("metal", null, "55595c"),
            ["plinth"] = Dressed("plinth", null, "6f6a62"),
            ["frame"] = Dressed("frame", null, "33373a"),
            ["door"] = Dressed("door", null, own.Pick(rolls ? WorkshopRollers : WorkshopDoors)),
        };
        string sign = Dressed("trim", null, own.Pick(WorkshopSigns));

        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body) { Look = look };
        float pitch = place.PitchDegrees > 0 ? place.PitchDegrees
            : workshop.PitchDegrees is { } range ? (range.Low + range.High) / 2f : period.Pitch;
        var site = new WorkshopSite(w, frontage / 2f, depth / 2f, storeys * Storey, attached, own,
            new WorkshopDressing(sign, band, board, Dressed("wall", "rough-block-wall", "9a958c"),
                Dressed("trim", null, "4a3a2c"), Dressed("trim", null, "a08660")));
        switch (form)
        {
            case WorkshopForm.BuildersYard:
                site.BuildersYard(pitch);
                break;
            case WorkshopForm.Monitor:
                site.Monitor(pitch);
                break;
            case WorkshopForm.InterwarGarage:
                site.InterwarGarage();
                break;
            case WorkshopForm.ParapetRoller:
                site.ParapetRoller();
                break;
            case WorkshopForm.PortalShed:
                site.PortalShed(pitch);
                break;
        }

        return mesh;
    }

    /// <summary>A part dressed from a <c>texture</c> or <c>texture#paint</c> spec.</summary>
    private static string Specified(string part, string spec)
    {
        int hash = spec.IndexOf('#', StringComparison.Ordinal);
        return hash < 0 ? Dressed(part, spec, null) : Dressed(part, spec[..hash], spec[(hash + 1)..]);
    }

    /// <summary>The dressed parts a workshop draws besides its look: the Building's sign, and the run's band, boarding, dado and timber.</summary>
    private sealed record WorkshopDressing(string Sign, string Band, string Board, string Dado, string Timber, string Planks);

    /// <summary>A rectangle of a workshop's plan, and which of its sides stand against a party wall.</summary>
    private readonly record struct Plot(float X0, float X1, float Y0, float Y1, bool Left, bool Right)
    {
        public float Wide => X1 - X0;

        public float Deep => Y1 - Y0;
    }

    private enum RoofEnd : byte
    {
        Party,
        Gable,
        Hip,
    }

    /// <summary>One workshop Building as it is drawn, in the frame its Lot is centred on.</summary>
    private sealed class WorkshopSite(Writer w, float x, float y, float height, AttachedSides attached, HouseDraw draw, WorkshopDressing d)
    {
        private HouseDraw _draw = draw;

        private Plot Whole => new(-x, x, -y, y, attached.HasFlag(AttachedSides.Left), attached.HasFlag(AttachedSides.Right));

        private int Floors => Math.Max(1, (int)MathF.Round(height / Storey));

        private static Face FaceOf(Vector2 p, Vector2 q, float z, out float n)
        {
            Vector2 a = q - p;
            n = a.Length();
            a /= n;
            return new Face(new Vector3(p.X, p.Y, z), new Vector3(a.X, a.Y, 0f), new Vector3(a.Y, -a.X, 0f));
        }

        /// <summary>The four walls anticlockwise from above: front, right, back, left.</summary>
        private static (Face Face, float Length, bool Party)[] Sides(Plot b, float z = 0f)
        {
            Face front = FaceOf(new(b.X0, b.Y0), new(b.X1, b.Y0), z, out float nf);
            Face right = FaceOf(new(b.X1, b.Y0), new(b.X1, b.Y1), z, out float nr);
            Face back = FaceOf(new(b.X1, b.Y1), new(b.X0, b.Y1), z, out float nb);
            Face left = FaceOf(new(b.X0, b.Y1), new(b.X0, b.Y0), z, out float nl);
            return [(front, nf, false), (right, nr, b.Right), (back, nb, false), (left, nl, b.Left)];
        }

        /// <summary>The openings that fit a wall, each cut short where it would rise into the eaves.</summary>
        private static List<Opening> Fit(List<Opening> openings, float width, float top)
        {
            var kept = new List<Opening>();
            foreach (Opening o in openings)
            {
                float h = MathF.Min(o.H, top - .2f - o.V);
                if (o.U < .05f || o.U + o.W > width - .05f || h < .6f) continue;
                kept.Add(o with { H = h });
            }

            return kept;
        }

        private static List<Opening> WindowsAlong(float n, float v, float ow, float oh, float step, float skipFrom = 0f, float skipTo = 0f)
        {
            int count = Math.Max(1, (int)MathF.Round(n / step));
            var openings = new List<Opening>();
            for (int i = 0; i < count; i++)
            {
                float c = n * (i + .5f) / count;
                if (skipFrom <= c && c <= skipTo) continue;
                if (c - (ow / 2f) > .2f && c + (ow / 2f) < n - .2f) openings.Add(new Opening(c - (ow / 2f), v, ow, oh, OpeningKind.Window));
            }

            return openings;
        }

        /// <summary>A window on each storey above the ground one.</summary>
        private List<Opening> Upper(float n, float sill, float ow, float oh, float step, float skipFrom = 0f, float skipTo = 0f)
        {
            var openings = new List<Opening>();
            for (int k = 1; k < Floors; k++) openings.AddRange(WindowsAlong(n, (k * Storey) + sill, ow, oh, step, skipFrom, skipTo));
            return openings;
        }

        private void Bars(Face face, Opening o, int cols, int rows)
        {
            for (int i = 1; i < cols; i++)
            {
                float u = o.U + (o.W * i / cols);
                w.Slab(face, u - .03f, u + .03f, o.V, o.V + o.H, Reveal - .07f, Reveal - .02f, "frame");
            }

            for (int j = 1; j < rows; j++)
            {
                float v = o.V + (o.H * j / rows);
                w.Slab(face, o.U, o.U + o.W, v - .03f, v + .03f, Reveal - .07f, Reveal - .02f, "frame");
            }
        }

        private void Dress(Face face, List<Opening> openings, int cols, int rows, bool lintel)
        {
            foreach (Opening o in openings)
            {
                if (o.Kind != OpeningKind.Window) continue;
                if (cols > 1 || rows > 1) Bars(face, o, cols, rows);
                if (lintel) w.Slab(face, o.U - .15f, o.U + o.W + .15f, o.V + o.H, o.V + o.H + .25f, -.04f, 0f, "trim");
            }
        }

        /// <summary>Front and back with their openings, each free side with its own, and each party side blank.</summary>
        private (Face Face, float Length, bool Party)[] Walls(Plot b, float top, List<Opening> front, List<Opening> back,
            Func<float, List<Opening>> side, int cols, int rows, bool lintel = false)
        {
            (Face Face, float Length, bool Party)[] sides = Sides(b);
            for (int i = 0; i < 4; i++)
            {
                (Face face, float n, bool party) = sides[i];
                List<Opening> openings = party ? [] : Fit(i switch { 0 => front, 2 => back, _ => side(n) }, n, top);
                w.Facade(face, n, top, openings);
                Dress(face, openings, cols, rows, lintel);
                if (!party) w.Slab(face, 0f, n, 0f, PlinthHeight, -.04f, .1f, "plinth");
            }

            return sides;
        }

        private void RearDeck(Plot b, float from, float top, float parapet = .5f)
        {
            if (from >= b.Y1) return;
            w.Box(new Vector3(b.X0, from, top - .2f), new Vector3(b.X1, b.Y1, top + .05f), "membrane");
            w.Box(new Vector3(b.X0, b.Y1 - .25f, top), new Vector3(b.X1, b.Y1, top + parapet), "wall");
            if (!b.Left) w.Box(new Vector3(b.X0, from, top), new Vector3(b.X0 + .25f, b.Y1 - .25f, top + parapet), "wall");
            if (!b.Right) w.Box(new Vector3(b.X1 - .25f, from, top), new Vector3(b.X1, b.Y1 - .25f, top + parapet), "wall");
        }

        /// <summary>
        /// A ridge along the street over a rectangle anywhere in the plan. A party end stops at its wall,
        /// a gable end shows a verge, and a hip on a party wall stops at it. Hips on a rectangle deeper than
        /// it is wide turn the ridge to run back from the street.
        /// </summary>
        private void Ridge(float x0, float x1, float front, float back, float top, float degrees, RoofEnd left, RoofEnd right,
            bool leftParty, bool rightParty, float eaves)
        {
            float t = MathF.Tan(degrees * MathF.PI / 180f), low = top - (eaves * t);
            float ya = front - eaves, yb = back + eaves, cy = (front + back) / 2f, half = (back - front) / 2f;
            float Edge(RoofEnd end, bool party, float wall, float sign) =>
                end == RoofEnd.Party || party ? wall : wall + (sign * (end == RoofEnd.Hip ? eaves : ApartmentVerge));
            float xa = Edge(left, leftParty, x0, -1f), xb = Edge(right, rightParty, x1, 1f);
            if (left == RoofEnd.Hip && right == RoofEnd.Hip && half > (x1 - x0) / 2f)
            {
                float cx = (x0 + x1) / 2f, r = (x1 - x0) / 2f, peak = top + (r * t), r0 = front + r, r1 = back - r;
                Outward(w, "roof", new Vector3(-1f, 0f, 1f), [new(xa, yb, low), new(xa, ya, low), new(cx, r0, peak), new(cx, r1, peak)]);
                Outward(w, "roof", new Vector3(1f, 0f, 1f), [new(xb, ya, low), new(xb, yb, low), new(cx, r1, peak), new(cx, r0, peak)]);
                Outward(w, "roof", new Vector3(0f, -1f, 1f), [new(xa, ya, low), new(xb, ya, low), new(cx, r0, peak)]);
                Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, yb, low), new(xa, yb, low), new(cx, r1, peak)]);
                Gutters(w, xa, xb, ya, yb, low);
                return;
            }

            float ridge = top + (half * t);
            float ra = left == RoofEnd.Hip ? x0 + half : xa, rb = right == RoofEnd.Hip ? x1 - half : xb;
            if (ra > rb) ra = rb = (ra + rb) / 2f;
            Outward(w, "roof", new Vector3(0f, -1f, 1f), [new(xa, ya, low), new(xb, ya, low), new(rb, cy, ridge), new(ra, cy, ridge)]);
            Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, yb, low), new(xa, yb, low), new(ra, cy, ridge), new(rb, cy, ridge)]);
            foreach ((RoofEnd end, float wall, float xe, float r, float sign) in
                (ReadOnlySpan<(RoofEnd, float, float, float, float)>)[(left, x0, xa, ra, -1f), (right, x1, xb, rb, 1f)])
            {
                if (end == RoofEnd.Hip)
                {
                    Outward(w, "roof", new Vector3(sign, 0f, 1f), [new(xe, yb, low), new(xe, ya, low), new(r, cy, ridge)]);
                    continue;
                }

                Outward(w, "wall-end", new Vector3(sign, 0f, 0f), [new(wall, front, top), new(wall, back, top), new(wall, cy, ridge)]);
                if (end != RoofEnd.Gable) continue;
                Outward(w, "trim", new Vector3(sign, 0f, 0f), [new(xe, ya, low + .2f), new(xe, cy, ridge + .2f), new(xe, cy, ridge), new(xe, ya, low)]);
                Outward(w, "trim", new Vector3(sign, 0f, 0f), [new(xe, cy, ridge + .2f), new(xe, yb, low + .2f), new(xe, yb, low), new(xe, cy, ridge)]);
            }

            Gutters(w, xa, xb, ya, yb, low);
        }

        /// <summary>A hipped brick yard house beside a boarded cart shed, open on posts with a loft and hoist above.</summary>
        public void BuildersYard(float pitch)
        {
            Plot b = Whole;
            float ow = MathF.Min(9f, b.Wide * .4f), houseTop = height + 2f, recess = 2.4f;
            bool houseLeft = _draw.Chance(.5f);
            Plot house = houseLeft ? b with { X1 = b.X0 + ow, Right = false } : b with { X0 = b.X1 - ow, Left = false };
            Plot shed = houseLeft ? b with { X0 = b.X0 + ow, Left = true } : b with { X1 = b.X1 - ow, Right = true };

            (Face Face, float Length, bool Party)[] sides = Sides(house);
            for (int i = 0; i < 4; i++)
            {
                (Face face, float m, bool party) = sides[i];
                bool inner = i == (houseLeft ? 1 : 3);
                List<Opening> openings = [];
                if (i == 0)
                {
                    openings.Add(new Opening((m / 2f) - .55f, PlinthHeight, 1.1f, 2.6f, OpeningKind.Door));
                    foreach (float c in (ReadOnlySpan<float>)[1.8f, m - 1.8f]) openings.Add(new Opening(c - .6f, 1f, 1.2f, 2f, OpeningKind.Window));
                    for (int k = 1; k < Floors; k++)
                    {
                        foreach (float c in (ReadOnlySpan<float>)[1.8f, m / 2f, m - 1.8f]) openings.Add(new Opening(c - .6f, (k * Storey) + 1.3f, 1.2f, 2f, OpeningKind.Window));
                    }
                }
                else if (!party && !inner)
                {
                    openings.AddRange(WindowsAlong(m, 1f, 1.2f, 2f, 3.2f));
                    openings.AddRange(Upper(m, 1.3f, 1.2f, 2f, 3.2f));
                }

                openings = Fit(openings, m, houseTop);
                w.Facade(face, m, houseTop, openings);
                Dress(face, openings, 2, 3, lintel: true);
                if (!party) w.Slab(face, 0f, m, 0f, PlinthHeight, -.04f, .1f, "plinth");
                if (i == 0)
                {
                    w.Slab(face, (m / 2f) - 1f, (m / 2f) + 1f, 3f, 3.18f, -.7f, 0f, "trim");
                    if (Floors > 1) w.Slab(face, 1f, m - 1f, Storey + .3f, Storey + 1f, -.08f, 0f, d.Sign);
                }
            }

            float houseBack = MathF.Min(house.Y1, house.Y0 + 10f);
            Ridge(house.X0, house.X1, house.Y0, houseBack, houseTop, pitch + 8f, RoofEnd.Hip, RoofEnd.Hip, house.Left, house.Right, ApartmentEaves);
            RearDeck(house, houseBack, houseTop);

            float sw = shed.Wide;
            if (sw < 3f) return;
            int bays = Math.Max(2, (int)MathF.Round(sw / 4f));
            float bay = sw / bays;
            int stores = Math.Max(1, bays / 3);
            var store = new bool[bays];
            for (int k = 0; k < stores; k++) store[_draw.Pick(bays)] = true;
            Face inside = FaceOf(new(shed.X0, shed.Y0 + recess), new(shed.X1, shed.Y0 + recess), 0f, out float gm);
            var doors = new List<Opening>();
            for (int i = 0; i < bays; i++)
            {
                if (store[i]) doors.Add(new Opening((bay * (i + .5f)) - 1.2f, PlinthHeight, 2.4f, 2.8f, OpeningKind.Door));
            }

            doors = Fit(doors, gm, Storey);
            w.Facade(inside, gm, Storey, doors, wall: d.Board);
            for (int i = 0; i <= bays; i++)
            {
                float px = Math.Clamp(shed.X0 + (bay * i), shed.X0 + .15f, shed.X1 - .15f);
                w.Box(new Vector3(px - .14f, shed.Y0 + .05f, 0f), new Vector3(px + .14f, shed.Y0 + .33f, Storey - .3f), d.Timber);
            }

            w.Box(new Vector3(shed.X0, shed.Y0, Storey - .3f), new Vector3(shed.X1, shed.Y0 + .35f, Storey), d.Timber);
            w.Box(new Vector3(shed.X0, shed.Y0, Storey - .32f), new Vector3(shed.X1, shed.Y0 + recess, Storey - .28f), d.Board);
            for (int i = 0; i < bays; i++)
            {
                if (store[i]) continue;
                float xa = shed.X0 + (bay * i) + .5f;
                int piles = 2 + _draw.Pick(4);
                for (int k = 0; k < piles; k++)
                {
                    w.Box(new Vector3(xa, shed.Y0 + .8f, .1f + (k * .32f)), new Vector3(xa + bay - 1f, shed.Y0 + recess - .1f, .1f + (k * .32f) + .28f), d.Planks);
                }
            }

            Face loft = FaceOf(new(shed.X0, shed.Y0), new(shed.X1, shed.Y0), Storey, out float um);
            int hoist = _draw.Pick(bays);
            var loftOpenings = new List<Opening> { new((bay * (hoist + .5f)) - .7f, .2f, 1.4f, 2.4f, OpeningKind.Door) };
            for (int i = 0; i < bays; i++)
            {
                if (i != hoist) loftOpenings.Add(new Opening((bay * (i + .5f)) - .6f, 1.1f, 1.2f, 1.2f, OpeningKind.Window));
            }

            loftOpenings = Fit(loftOpenings, um, height - Storey);
            w.Facade(loft, um, height - Storey, loftOpenings, wall: d.Board);
            if (height - Storey > 2.2f)
            {
                float hx = shed.X0 + (bay * (hoist + .5f));
                w.Box(new Vector3(hx - .09f, shed.Y0 - .9f, height - .45f), new Vector3(hx + .09f, shed.Y0 + .1f, height - .27f), d.Timber);
            }

            Face back = FaceOf(new(shed.X1, shed.Y1), new(shed.X0, shed.Y1), 0f, out float bm);
            w.Facade(back, bm, height, Fit(Upper(bm, .9f, 1.2f, 1.2f, 4f), bm, height), wall: d.Board);
            foreach ((bool party, float ex, float sign) in (ReadOnlySpan<(bool, float, float)>)[(shed.Left, shed.X0, -1f), (shed.Right, shed.X1, 1f)])
            {
                if (party) continue;
                Vector2 rear = new(ex, shed.Y1), mouth = new(ex, shed.Y0 + recess), street = new(ex, shed.Y0);
                Face ground = sign < 0f ? FaceOf(rear, mouth, 0f, out float gn) : FaceOf(mouth, rear, 0f, out gn);
                w.Facade(ground, gn, Storey, [], wall: d.Board);
                Face above = sign < 0f ? FaceOf(rear, street, Storey, out float an) : FaceOf(street, rear, Storey, out an);
                w.Facade(above, an, height - Storey, [], wall: d.Board);
            }

            float shedBack = MathF.Min(shed.Y1, shed.Y0 + 8f);
            Ridge(shed.X0, shed.X1, shed.Y0, shedBack, height, pitch,
                houseLeft ? RoofEnd.Party : RoofEnd.Gable, houseLeft ? RoofEnd.Gable : RoofEnd.Party, shed.Left, shed.Right, .4f);
            RearDeck(shed with { Left = true, Right = true }, shedBack, height);
            if (!shed.Left && !houseLeft) w.Box(new Vector3(shed.X0, shedBack, height), new Vector3(shed.X0 + .25f, shed.Y1 - .25f, height + .5f), "wall");
            if (!shed.Right && houseLeft) w.Box(new Vector3(shed.X1 - .25f, shedBack, height), new Vector3(shed.X1, shed.Y1 - .25f, height + .5f), "wall");
        }

        /// <summary>A low side gable with a glazed clerestory monitor along its ridge, brick piers and steel windows.</summary>
        public void Monitor(float pitch)
        {
            Plot b = Whole;
            float wide = b.Wide, deep = b.Deep, t = MathF.Tan(pitch * MathF.PI / 180f);
            float m = MathF.Min(4f, deep * .3f), mh = 1.4f, eaves = .4f, yc = (b.Y0 + b.Y1) / 2f;
            float zm = height + (((deep / 2f) - (m / 2f)) * t), top = zm + mh + (m / 2f * t);
            int nb = Math.Max(3, (int)MathF.Round(wide / 4f));
            float pb = wide / nb;
            int door = 1 + _draw.Pick(nb - 2);
            var front = new List<Opening>();
            for (int i = 0; i < nb; i++)
            {
                float c = pb * (i + .5f);
                front.Add(i == door
                    ? new Opening(c - 1.7f, PlinthHeight, 3.4f, 4f, OpeningKind.Door)
                    : new Opening(c - 1.3f, 1f, 2.6f, 2.6f, OpeningKind.Window));
            }

            front.AddRange(Upper(wide, 1f, 2.6f, 2f, pb));
            front.RemoveAll(o => o.V > Storey && MathF.Abs(o.U + (o.W / 2f) - (pb * (door + .5f))) < 1f && o.V < 4.4f);
            List<Opening> Side(float n) => [.. WindowsAlong(n, 1f, 2.6f, 2.6f, 4f), .. Upper(n, 1f, 2.6f, 2f, 4f)];
            (Face Face, float Length, bool Party)[] sides = Walls(b, height, front, Side(wide), Side, 4, 5, lintel: true);
            foreach (int k in (ReadOnlySpan<int>)[0, 2])
            {
                (Face face, float n, _) = sides[k];
                for (int i = 0; i <= nb; i++)
                {
                    float u = Math.Clamp(pb * i, .3f, n - .3f);
                    w.Slab(face, u - .3f, u + .3f, 0f, height, -.15f, 0f, "wall");
                }

                w.Slab(face, 0f, n, height - .5f, height - .2f, -.18f, 0f, "trim");
            }

            if (Floors > 1) w.Slab(sides[0].Face, (wide / 2f) - 2.5f, (wide / 2f) + 2.5f, Storey + .2f, Storey + .8f, -.1f, 0f, "trim");
            float xa = b.Left ? b.X0 : b.X0 - .3f, xb = b.Right ? b.X1 : b.X1 + .3f;
            float ya = b.Y0 - eaves, yb = b.Y1 + eaves, low = height - (eaves * t), fm = yc - (m / 2f), bm = yc + (m / 2f);
            Outward(w, "roof", new Vector3(0f, -1f, 1f), [new(xa, ya, low), new(xb, ya, low), new(xb, fm, zm), new(xa, fm, zm)]);
            Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, yb, low), new(xa, yb, low), new(xa, bm, zm), new(xb, bm, zm)]);
            var glazing = new List<Opening> { new(.3f, .15f, wide - .6f, mh - .3f, OpeningKind.Window) };
            foreach ((float yy, float sign) in (ReadOnlySpan<(float, float)>)[(fm, -1f), (bm, 1f)])
            {
                Face face = sign < 0f
                    ? new Face(new Vector3(b.X0, yy, zm), Vector3.UnitX, -Vector3.UnitY)
                    : new Face(new Vector3(b.X1, yy, zm), -Vector3.UnitX, Vector3.UnitY);
                w.Facade(face, wide, mh, glazing, wall: "wall-end");
                Bars(face, glazing[0], Math.Max(2, (int)MathF.Round(wide / 1.2f)), 1);
            }

            float me = .3f, mz = zm + mh - (me * t);
            Outward(w, "roof", new Vector3(0f, -1f, 1f), [new(xa, fm - me, mz), new(xb, fm - me, mz), new(xb, yc, top), new(xa, yc, top)]);
            Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, bm + me, mz), new(xa, bm + me, mz), new(xa, yc, top), new(xb, yc, top)]);
            foreach ((float ex, float sign, bool party) in (ReadOnlySpan<(float, float, bool)>)[(b.X0, -1f, b.Left), (b.X1, 1f, b.Right)])
            {
                var outward = new Vector3(sign, 0f, 0f);
                Outward(w, "wall", outward, [new(ex, b.Y0, height), new(ex, b.Y1, height), new(ex, bm, zm), new(ex, fm, zm)]);
                Outward(w, "wall", outward, [new(ex, fm, zm), new(ex, bm, zm), new(ex, bm, zm + mh), new(ex, yc, top), new(ex, fm, zm + mh)]);
                if (party) w.Box(new Vector3(MathF.Min(ex, ex - (sign * .24f)), b.Y0, height - .2f), new Vector3(MathF.Max(ex, ex - (sign * .24f)), b.Y1, height + .3f), "wall");
            }

            Gutters(w, xa, xb, ya, yb, low);
            if (_draw.Chance(.4f))
            {
                float cx = b.X0 + (wide * (_draw.Chance(.5f) ? .2f : .8f));
                w.Box(new Vector3(cx - .8f, b.Y1 - 1.8f, 0f), new Vector3(cx + .8f, b.Y1 - .2f, top + 3.5f), "wall");
                w.Box(new Vector3(cx - 1f, b.Y1 - 2f, top + 3.1f), new Vector3(cx + 1f, b.Y1, top + 3.5f), "trim");
            }
        }

        /// <summary>A bow roof behind a stepped rendered parapet, with a name panel, speed lines and a canopied vehicle door.</summary>
        public void InterwarGarage()
        {
            Plot b = Whole;
            float wide = b.Wide, deep = b.Deep, rise = deep * .18f;
            float centre = wide * _draw.Pick(GarageCentres);
            float gw = MathF.Min(_draw.Pick(GarageDoors), wide - 1f);
            centre = Math.Clamp(centre, (gw / 2f) + .3f, wide - (gw / 2f) - .3f);
            var front = new List<Opening> { new(centre - (gw / 2f), PlinthHeight, gw, 4.2f, OpeningKind.Roller) };
            foreach (float c in (ReadOnlySpan<float>)[centre - (gw / 2f) - 3.2f, centre + (gw / 2f) + 3.2f])
            {
                if (c > 2.2f && c < wide - 2.2f) front.Add(new Opening(c - 1.9f, .6f, 3.8f, 2.6f, OpeningKind.Shop));
            }

            front.AddRange(Upper(wide, 1f, 2.4f, 1.6f, 4f, centre - (gw / 2f) - 1.5f, centre + (gw / 2f) + 1.5f));
            List<Opening> Side(float n) => [.. WindowsAlong(n, 1.2f, 2.4f, 1.6f, 4f), .. Upper(n, 1f, 2.4f, 1.6f, 4f)];
            List<Opening> back = [.. Upper(wide, 1f, 2.4f, 1.6f, 4f, (wide / 2f) - 3f, (wide / 2f) + 3f), new((wide / 2f) - 2.2f, PlinthHeight, 4.4f, 4f, OpeningKind.Roller)];
            (Face face, float n, _) = Walls(b, height, front, back, Side, 4, 3)[0];
            for (int i = 0; i < 3; i++)
            {
                float z = height - .9f + (i * .25f);
                w.Slab(face, 0f, n, z, z + .08f, -.06f, 0f, "trim");
            }

            w.Slab(face, 0f, n, height, height + 1.3f, -.02f, .3f, "wall");
            w.Slab(face, 0f, n, height + 1.3f, height + 1.4f, -.06f, .32f, "trim");
            float sw = MathF.Min(12f, n * .38f);
            float s0 = Math.Clamp(centre - (sw / 2f), 0f, n - sw);
            float sc = s0 + (sw / 2f);
            w.Slab(face, s0, s0 + sw, height + 1.3f, height + 2.4f, -.08f, .3f, "wall");
            w.Slab(face, sc - (sw / 4f), sc + (sw / 4f), height + 2.4f, height + 3.1f, -.1f, .3f, "wall");
            w.Slab(face, s0 + .5f, s0 + sw - .5f, height + 1.45f, height + 2.25f, -.12f, 0f, d.Sign);
            foreach (float u in (ReadOnlySpan<float>)[s0, s0 + sw - .4f, 0f, n - .4f])
            {
                bool flanks = MathF.Abs(u + .2f - sc) < sw;
                w.Slab(face, u, u + .4f, 0f, height + (flanks ? 2.8f : 1.8f), -.2f, 0f, "wall");
            }

            w.Slab(face, Math.Max(0f, centre - (gw / 2f) - .4f), Math.Min(n, centre + (gw / 2f) + .4f), 4.6f, 4.75f, -1.6f, 0f, "trim");

            const int N = 14;
            Span<Vector2> arc = stackalloc Vector2[N + 1];
            for (int k = 0; k <= N; k++) arc[k] = new Vector2(b.Y0 + (deep * k / N), height + (rise * MathF.Sin(MathF.PI * k / N)));
            float xa = b.Left ? b.X0 : b.X0 - .25f, xb = b.Right ? b.X1 : b.X1 + .25f;
            for (int k = 0; k < N; k++)
            {
                Vector2 p = arc[k], q = arc[k + 1];
                Outward(w, "roof", new Vector3(0f, p.Y - q.Y, q.X - p.X), [new(xa, p.X, p.Y), new(xb, p.X, p.Y), new(xb, q.X, q.Y), new(xa, q.X, q.Y)]);
            }

            Span<Vector3> end = stackalloc Vector3[N + 1];
            foreach ((bool party, float ex, float sign) in (ReadOnlySpan<(bool, float, float)>)[(b.Left, b.X0, -1f), (b.Right, b.X1, 1f)])
            {
                if (party) continue;
                for (int k = 0; k <= N; k++) end[k] = new Vector3(ex, arc[k].X, arc[k].Y);
                Outward(w, "wall", new Vector3(sign, 0f, 0f), end);
            }

            if (wide > 5f)
            {
                for (int k = 1; k < N; k += 3)
                {
                    w.Box(new Vector3(b.X0 + 2f, arc[k].X - .4f, arc[k].Y + .02f), new Vector3(b.X1 - 2f, arc[k].X + .4f, arc[k].Y + .08f), "glass");
                }
            }

            w.Box(new Vector3(b.X0, b.Y1 - .2f, height), new Vector3(b.X1, b.Y1, height + .3f), "wall");
        }

        /// <summary>Brown brick under a flat parapet, with roller doors, an office door under a canopy and a high strip window.</summary>
        public void ParapetRoller()
        {
            Plot b = Whole;
            float wide = b.Wide;
            int rollers = wide > 30f ? _draw.Pick(WideRollers) : _draw.Pick(NarrowRollers);
            float rw = _draw.Pick(RollerWidths);
            while (rollers > 0 && (rollers * (rw + 1f)) + 6f > wide) rollers--;
            int office = _draw.Pick(rollers + 1);
            var widths = new List<float>();
            for (int i = 0; i <= rollers; i++) widths.Add(i == office ? 5f : rw);
            float gap = (wide - widths.Sum()) / (widths.Count + 1);
            var front = new List<Opening>();
            float u = gap, canopy = -1f;
            for (int i = 0; i < widths.Count; i++)
            {
                if (i == office)
                {
                    front.Add(new Opening(u, PlinthHeight, 1f, 2.4f, OpeningKind.Door));
                    if (widths[i] > 4.5f && gap > 0f) front.Add(new Opening(u + 1.5f, .9f, 3.4f, 1.5f, OpeningKind.Window));
                    canopy = u;
                }
                else
                {
                    front.Add(new Opening(u, PlinthHeight, widths[i], 4.4f, OpeningKind.Roller));
                }

                u += widths[i] + gap;
            }

            front.Add(new Opening(.8f, 5.2f, wide - 1.6f, .9f, OpeningKind.Window));
            List<Opening> Side(float n) => [new(.8f, 5.2f, n - 1.6f, .9f, OpeningKind.Window), new((n / 2f) - .5f, PlinthHeight, 1f, 2.4f, OpeningKind.Door)];
            List<Opening> back = [new(.8f, 5.2f, wide - 1.6f, .9f, OpeningKind.Window), new(wide * .3f, PlinthHeight, 4f, 4.4f, OpeningKind.Roller)];
            (Face Face, float Length, bool Party)[] sides = Walls(b, height, front, back, Side, 1, 1);
            foreach ((Face face, float n, bool party) in sides)
            {
                if (party) continue;
                if (height > 5.2f) w.Slab(face, 0f, n, 4.75f, 5f, -.08f, 0f, d.Band);
                w.Slab(face, 0f, n, height, height + .9f, -.02f, .25f, "wall");
                w.Slab(face, 0f, n, height + .9f, height + 1f, -.08f, .3f, d.Band);
            }

            Face street = sides[0].Face;
            if (canopy >= 0f) w.Slab(street, Math.Max(0f, canopy - .4f), canopy + 1.4f, 2.8f, 2.95f, -1.2f, 0f, d.Band);
            if (height > 6.9f)
            {
                float at = _draw.Range(1f, MathF.Max(1f, wide - 9f));
                w.Slab(street, at, MathF.Min(wide, at + 8f), 6.3f, 6.85f, -.06f, 0f, d.Sign);
            }

            w.Box(new Vector3(b.X0, b.Y0, height - .2f), new Vector3(b.X1, b.Y1, height + .02f), "membrane");
            if (b.Deep > 14f)
            {
                for (int i = 0; i < (int)(wide / 8f); i++)
                {
                    float rx = b.X0 + 4f + (i * 8f);
                    if (rx + 1.3f >= b.X1) continue;
                    w.Box(new Vector3(rx - 1.2f, b.Y0 + 4f, height), new Vector3(rx + 1.2f, b.Y0 + 7f, height + .35f), "glass");
                    w.Box(new Vector3(rx - 1.2f, b.Y1 - 7f, height), new Vector3(rx + 1.2f, b.Y1 - 4f, height + .35f), "glass");
                }
            }

            float vx = b.X0 + (wide * .6f), vy = (b.Y0 + b.Y1) / 2f;
            w.Box(new Vector3(vx - .3f, vy - .3f, height), new Vector3(vx + .3f, vy + .3f, height + .9f), "metal");
        }

        /// <summary>A low-pitch gable front in profiled sheet over a block dado, with sectional doors and a trade counter.</summary>
        public void PortalShed(float pitch)
        {
            Plot b = Whole;
            float wide = b.Wide, t = MathF.Tan(pitch * MathF.PI / 180f), top = height + (x * t);
            int doors = wide > 30f && _draw.Chance(.6f) ? 2 : 1;
            while (doors > 0 && (doors * 6f) + 7f > wide) doors--;
            bool counterFirst = _draw.Chance(.5f);
            var widths = new List<float>();
            for (int i = 0; i < doors; i++) widths.Add(5f);
            if (counterFirst) widths.Insert(0, 6f);
            else widths.Add(6f);
            int counterAt = counterFirst ? 0 : widths.Count - 1;
            float gap = MathF.Max(0f, (wide - widths.Sum()) / (widths.Count + 1));
            var front = new List<Opening>();
            float u = gap, counter = 0f;
            for (int i = 0; i < widths.Count; i++)
            {
                if (i == counterAt)
                {
                    front.Add(new Opening(u + .2f, PlinthHeight, 2.6f, 2.7f, OpeningKind.Shop));
                    front.Add(new Opening(u + 3.2f, .9f, 2.6f, 1.6f, OpeningKind.Window));
                    front.Add(new Opening(u + .2f, 3.9f, 5.6f, 1.2f, OpeningKind.Window));
                    counter = u;
                }
                else
                {
                    front.Add(new Opening(u, PlinthHeight, 5f, 5f, OpeningKind.Roller));
                }

                u += widths[i] + gap;
            }

            List<Opening> Side(float n) => [new((n / 2f) - .5f, PlinthHeight, 1f, 2.3f, OpeningKind.Door)];
            List<Opening> back = [new((wide / 2f) - 2.5f, PlinthHeight, 5f, 5f, OpeningKind.Roller)];
            (Face Face, float Length, bool Party)[] sides = Walls(b, height, front, back, Side, 1, 1);
            for (int i = 0; i < 4; i++)
            {
                (Face face, float n, bool party) = sides[i];
                if (party) continue;
                List<Opening> through = Fit(i switch { 0 => front, 2 => back, _ => Side(n) }, n, height);
                through.Sort((p, q) => p.U.CompareTo(q.U));
                float from = 0f;
                foreach (Opening o in through)
                {
                    if (o.V >= 2.2f) continue;
                    if (o.U - from > .05f) w.Slab(face, from, o.U, 0f, 2.2f, -.1f, 0f, d.Dado);
                    from = MathF.Max(from, o.U + o.W);
                }

                if (n - from > .05f) w.Slab(face, from, n, 0f, 2.2f, -.1f, 0f, d.Dado);
                w.Slab(face, 0f, n, 2.2f, 2.3f, -.13f, 0f, "trim");
            }

            Face street = sides[0].Face;
            w.Slab(street, Math.Max(0f, counter - .3f), Math.Min(wide, counter + 6.3f), 3.05f, 3.25f, -1.5f, 0f, "trim");
            if (height > 6.5f) w.Slab(street, counter, Math.Min(wide, counter + 6f), 5.4f, 6.4f, -.1f, 0f, d.Sign);
            foreach (Opening o in Fit(front, wide, height))
            {
                if (o.Kind == OpeningKind.Roller) w.Slab(street, o.U - .15f, o.U + o.W + .15f, o.V + o.H, o.V + o.H + .2f, -.06f, 0f, "trim");
            }

            float ba = b.Left ? b.X0 : b.X0 - .1f, bb = b.Right ? b.X1 : b.X1 + .1f;
            foreach ((float gy, float sign) in (ReadOnlySpan<(float, float)>)[(b.Y0, -1f), (b.Y1, 1f)])
            {
                var outward = new Vector3(0f, sign, 0f);
                Outward(w, "wall", outward, [new(b.X0, gy, height), new(b.X1, gy, height), new(0f, gy, top)]);
                float near = gy + (sign * .05f), far = gy + (sign * .15f);
                foreach ((float ea, float ez) in (ReadOnlySpan<(float, float)>)[(ba, height - .05f), (bb, height - .05f)])
                {
                    Outward(w, "trim", outward, [new(ea, far, ez), new(0f, far, top + .02f), new(0f, far, top + .27f), new(ea, far, ez + .25f)]);
                    Outward(w, "trim", new Vector3(0f, 0f, 1f), [new(ea, near, ez + .25f), new(ea, far, ez + .25f), new(0f, far, top + .27f), new(0f, near, top + .27f)]);
                }
            }

            float ya = b.Y0 - .25f, yb = b.Y1 + .25f;
            float xa = b.Left ? b.X0 : b.X0 - .3f, xb = b.Right ? b.X1 : b.X1 + .3f;
            float la = b.Left ? height : height - (.3f * t), lb = b.Right ? height : height - (.3f * t);
            Outward(w, "roof", new Vector3(-1f, 0f, 1f), [new(xa, yb, la), new(xa, ya, la), new(0f, ya, top), new(0f, yb, top)]);
            Outward(w, "roof", new Vector3(1f, 0f, 1f), [new(0f, yb, top), new(0f, ya, top), new(xb, ya, lb), new(xb, yb, lb)]);
            if (b.Deep > 4f)
            {
                foreach ((float f0, float f1) in (ReadOnlySpan<(float, float)>)[(.3f, .38f), (.62f, .7f)])
                {
                    foreach (float sign in (ReadOnlySpan<float>)[-1f, 1f])
                    {
                        float za = height + ((1f - f1) * x * t) + .03f, zb = height + ((1f - f0) * x * t) + .03f;
                        float xa0 = sign * f0 * x, xb0 = sign * f1 * x;
                        Outward(w, "glass", new Vector3(sign, 0f, 1f),
                            [new(xa0, b.Y0 + 1.5f, zb), new(xb0, b.Y0 + 1.5f, za), new(xb0, b.Y1 - 1.5f, za), new(xa0, b.Y1 - 1.5f, zb)]);
                    }
                }
            }

            w.Box(new Vector3(-.25f, ya, top - .05f), new Vector3(.25f, yb, top + .08f), "trim");
            foreach ((bool party, float ex, float sign, float lo) in (ReadOnlySpan<(bool, float, float, float)>)[(b.Left, b.X0, -1f, la), (b.Right, b.X1, 1f, lb)])
            {
                float reach = party ? -.3f : .5f, z = party ? height : lo;
                float a = ex, c = ex + (sign * reach);
                w.Box(new Vector3(MathF.Min(a, c), ya, z - .3f), new Vector3(MathF.Max(a, c), yb, z - .05f), party ? "metal" : "trim");
            }
        }
    }
}
