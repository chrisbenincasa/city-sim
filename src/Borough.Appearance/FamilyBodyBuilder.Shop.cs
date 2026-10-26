using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The shop-house forms: shops on the ground storey and flats above, varied by period roof and plan.
/// They follow <c>scripts/art/shop-forms.py</c>.
/// </summary>
/// <remarks>
/// A parade shares its form, pitch and walls through the run's draw. Each shop unit draws its own
/// shopfront style, paint, awning and sign from the Building's draw, so a shopfront's period is
/// independent of its Building's. A free side on a block corner turns the shopfront onto the side
/// street and takes the form's corner: a splay, a rounded corner, or a turret. A part name carries its
/// dressing as a house body's does. The mansard and set-back top draw about 15% less of their top
/// storey than the Building holds, inside the drawing's 20% (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>The library textures a shop body can be drawn in.</summary>
    public static readonly string[] ShopTextures =
    [
        "brick-wall-001", "bricks-051", "bricks-085", "bricks-088", "painted-plaster-wall", "sheet-metal-004",
        "roofing-tiles-003", "clay-roof-tiles-02", "bitumen",
    ];

    /// <summary>How wide one shop unit is drawn, near enough. A frontage holds whole units.</summary>
    public const float ShopUnitMetres = 6f;

    private const float Fascia0 = 2.95f, Fascia1 = 3.45f;

    /// <summary>How deep a pitched roof runs back from the street before a flat rear range takes over.</summary>
    private const float ShopRoofDepth = 10f;

    private static readonly string[] ShopPaints =
        ["2f4a3c", "6a2f2a", "24303a", "1f2124", "e6dcc4", "2e5a5a", "8a7a3a", "4f5253", "7a3b52", "b8b0a0"];

    private static readonly string[] AwningPaints = ["7e3a2f", "2f4a3c", "3a4a6a", "c9b79c", "5a3a32", "8a7a3a"];
    private static readonly string[] FlatDoors = ["2f4a5c", "6a2f2a", "2f4a3c", "4f5253", "7a5a2c", "24303a"];

    /// <summary>Whether a shop form draws a pitched roof, and so takes a pitch.</summary>
    internal static bool ShopPitched(ShopForm form) =>
        form is ShopForm.VictorianGable or ShopForm.GabledOriel or ShopForm.TurretCorner or ShopForm.PantileHipped;

    /// <summary>The form a Building draws. A Building of one storey has no top storey to roof over or set back.</summary>
    public static ShopForm Drawn(ShopBody shop, int storeys) =>
        storeys < 2 && shop.Form is ShopForm.Mansard or ShopForm.SetbackModern ? ShopForm.MainStreet : shop.Form;

    private enum FrontStyle : byte
    {
        Timber,
        Iron,
        Deco,
        Aluminium,
        Glass,
    }

    private enum Upper : byte
    {
        Sash,
        Arched,
        Oriel,
        Steel,
        Ribbon,
        Tall,
        Shutter,
    }

    private enum Cut : byte
    {
        Square,
        Splay,
        GroundSplay,
        Round,
    }

    private enum Edge : byte
    {
        Front,
        Corner,
        Right,
        Back,
        Left,
    }

    /// <summary>What a form draws besides its roof, and the walls a parade draws from.</summary>
    private sealed record ShopPeriod(Upper Upper, Cut Cut, float CutMetres, string[] Walls, string Roof, string[] Bands, float Eaves = .45f);

    private static readonly Dictionary<ShopForm, ShopPeriod> Periods = new()
    {
        [ShopForm.VictorianGable] = new(Upper.Sash, Cut.GroundSplay, 1.6f,
            ["brick-wall-001", "bricks-085", "bricks-088#c2ae82", "bricks-088#b9a47c"], "roofing-tiles-003", ["e3dccb", "d8d2c4"]),
        [ShopForm.GabledOriel] = new(Upper.Oriel, Cut.Square, 0f,
            ["bricks-085", "brick-wall-001", "bricks-088#b48f7a"], "clay-roof-tiles-02", ["e3dccb", "d6cdb8"]),
        [ShopForm.TurretCorner] = new(Upper.Sash, Cut.Splay, 2.6f,
            ["bricks-085", "brick-wall-001", "bricks-051"], "roofing-tiles-003", ["e3dccb", "d9cfb6"]),
        [ShopForm.MainStreet] = new(Upper.Arched, Cut.GroundSplay, 2.2f,
            ["bricks-051", "bricks-085", "brick-wall-001", "bricks-088#b9a47c"], "bitumen", ["ddd6c4", "c9bfa8"]),
        [ShopForm.Mansard] = new(Upper.Sash, Cut.Square, 0f,
            ["painted-plaster-wall#d6d0c0", "painted-plaster-wall#cfc6b0", "painted-plaster-wall#c9c2b4", "painted-plaster-wall#d3c8ae", "painted-plaster-wall#bfc4bd"],
            "roofing-tiles-003", ["f0ece2"]),
        [ShopForm.InterwarCurve] = new(Upper.Steel, Cut.Round, 4f,
            ["bricks-088#b9a48a", "bricks-088#a58f74", "bricks-085"], "bitumen", ["ece6d6", "e2dcc8"]),
        [ShopForm.SixtiesCanopy] = new(Upper.Ribbon, Cut.Square, 0f,
            ["bricks-088#c9b48a", "bricks-088#b8ad96", "bricks-051"], "bitumen", ["c9c6bd", "b9b6ad"]),
        [ShopForm.SetbackModern] = new(Upper.Tall, Cut.Square, 0f,
            ["bricks-051", "brick-wall-001", "bricks-088#a7aeb0", "bricks-088#6f6a62"], "bitumen", ["4a4f52", "33373a"]),
        [ShopForm.PantileHipped] = new(Upper.Shutter, Cut.Square, 0f,
            ["painted-plaster-wall#d6bf96", "painted-plaster-wall#d9c9a8", "painted-plaster-wall#cfa98a", "painted-plaster-wall#c9b79c", "painted-plaster-wall#d4cdbd"],
            "clay-roof-tiles-02", ["ece6d6"], 1f),
    };

    private static readonly string[] Panels = ["8fb3c4", "d8c36a", "b9c4a0", "c78a6a"];
    private static readonly string[] Shutters = ["3e5a44", "5c7a8a", "8a4a3a"];
    private static readonly string[] Zincs = ["6b7175", "4f5b5f", "5d5a55"];

    /// <param name="corner">The Building stands on a block corner, so a free side faces a side street.</param>
    /// <param name="seed">The Building's own draw, out of <see cref="HouseSeeds"/>; it picks each shopfront.</param>
    /// <param name="run">The parade's draw, out of <see cref="HouseSeeds"/>; it picks the walls.</param>
    public static FamilyBodyMesh BuildShop(FamilyBody body, float frontage, float depth, int storeys, AttachedSides attached,
        bool corner, int seed, int run, TerracePlace place = default)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.Shop is not { } shop) throw new ArgumentException("Only a shop body is built with BuildShop().", nameof(body));

        ShopForm form = Drawn(shop, storeys);
        ShopPeriod period = Periods[form];
        var parade = new HouseDraw((ulong)run);
        string wall = parade.Pick(period.Walls);
        int hash = wall.IndexOf('#', StringComparison.Ordinal);
        string walls = hash < 0 ? Dressed("wall", wall, null) : Dressed("wall", wall[..hash], wall[(hash + 1)..]);
        var look = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["wall"] = walls,
            ["wall-end"] = walls,
            ["roof"] = Dressed("roof", period.Roof == "bitumen" ? "roofing-tiles-003" : period.Roof, null),
            ["trim"] = Dressed("trim", null, parade.Pick(period.Bands)),
            ["membrane"] = Dressed("membrane", "bitumen", null),
            ["metal"] = Dressed("metal", null, "55595c"),
            ["plinth"] = Dressed("plinth", null, "8f8c86"),
            ["frame"] = Dressed("frame", null, "ece8dc"),
            ["door"] = Dressed("door", null, parade.Pick(FlatDoors)),
        };
        string panel = Dressed("trim", null, parade.Pick(Panels));
        string shutter = Dressed("door", null, parade.Pick(Shutters));
        string zinc = Dressed("wall", "sheet-metal-004", parade.Pick(Zincs));

        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body) { Look = look };
        var s = new ShopSite(w, form, period, frontage / 2f, depth / 2f, storeys, attached, corner, new HouseDraw((ulong)seed), panel, shutter, zinc);
        float pitch = place.PitchDegrees > 0 ? place.PitchDegrees
            : shop.PitchDegrees is { } range ? (range.Low + range.High) / 2f : 30f;
        s.Walls();
        float height = storeys * Storey;
        if (ShopPitched(form)) s.RearDeck(height);
        switch (form)
        {
            case ShopForm.VictorianGable:
                Pitched(w, s.X, -s.Y, s.RoofBack, height, pitch, hip: false, attached, null);
                break;
            case ShopForm.GabledOriel:
                Pitched(w, s.X, -s.Y, s.RoofBack, height, pitch, hip: false, attached, null);
                float t = MathF.Tan(pitch * MathF.PI / 180f);
                foreach ((float u0, float u1) in ShopUnits(frontage))
                {
                    float half = ((u1 - u0) / 2f) - ApartmentEaves - .1f;
                    FrontGableOn(w, new CrossGable(-s.X + ((u0 + u1) / 2f), half, -s.Y), -s.Y, height, height - (ApartmentEaves * t), t);
                }

                break;
            case ShopForm.TurretCorner:
                Pitched(w, s.X, -s.Y, s.RoofBack, height, pitch, s.Hips, attached, null);
                if (s.CornerRight) s.Turret(right: true, height);
                if (s.CornerLeft) s.Turret(right: false, height);
                break;
            case ShopForm.PantileHipped:
                Pitched(w, s.X, -s.Y, s.RoofBack, height, pitch, s.Hips, attached, null, period.Eaves);
                break;
            case ShopForm.MainStreet or ShopForm.InterwarCurve or ShopForm.SixtiesCanopy:
                s.FlatRoof(height);
                break;
            case ShopForm.Mansard:
                s.Mansard();
                break;
            case ShopForm.SetbackModern:
                s.SetbackTop();
                break;
        }

        return mesh;
    }

    /// <summary>The shop units a frontage holds, as stretches from its left end.</summary>
    internal static (float From, float To)[] ShopUnits(float frontage)
    {
        int n = Math.Max(1, (int)MathF.Round(frontage / ShopUnitMetres));
        var units = new (float, float)[n];
        for (int i = 0; i < n; i++) units[i] = (frontage * i / n, frontage * (i + 1) / n);
        return units;
    }

    /// <summary>One Building's shop-house body as it is drawn: its plan, its walls and its period details.</summary>
    private sealed class ShopSite(Writer w, ShopForm form, ShopPeriod period, float x, float y, int storeys, AttachedSides attached,
        bool corner, HouseDraw draw, string panel, string shutter, string zinc)
    {
        private HouseDraw _draw = draw;
        private string _lastPaint = ShopPaints[0];

        public float X => x;

        public float Y => y;

        /// <summary>Where a pitched roof's back slope ends. Behind it a deep Building's rear range has a flat roof.</summary>
        public float RoofBack => MathF.Min(y, -y + ShopRoofDepth);

        /// <summary>Whether a hip fits: free at both ends, or wide enough that its hips meet behind the party wall.</summary>
        public bool Hips => attached == AttachedSides.None || x >= (RoofBack + y) / 2f;

        public void RearDeck(float height)
        {
            if (RoofBack >= y) return;
            w.Box(new Vector3(-x, RoofBack, height - .2f), new Vector3(x, y, height + .05f), "membrane");
            w.Box(new Vector3(-x, y - .25f, height), new Vector3(x, y, height + .5f), "wall");
            if (!attached.HasFlag(AttachedSides.Left)) w.Box(new Vector3(-x, RoofBack, height), new Vector3(-x + .25f, y - .25f, height + .5f), "wall");
            if (!attached.HasFlag(AttachedSides.Right)) w.Box(new Vector3(x - .25f, RoofBack, height), new Vector3(x, y - .25f, height + .5f), "wall");
        }

        public bool CornerLeft => corner && !attached.HasFlag(AttachedSides.Left);

        public bool CornerRight => corner && !attached.HasFlag(AttachedSides.Right);

        private bool Party(Edge side) => side is Edge.Left or Edge.Right && Shared(side);

        private bool Shared(Edge side) => side == Edge.Left ? attached.HasFlag(AttachedSides.Left) : attached.HasFlag(AttachedSides.Right);

        /// <summary>The storeys the walls rise to: one short of the Building under a mansard or set-back top.</summary>
        private float Top => (form is ShopForm.Mansard or ShopForm.SetbackModern ? storeys - 1 : storeys) * Storey;

        /// <summary>The footprint, anticlockwise from above, with the side each edge from a corner faces.</summary>
        public List<(Vector2 At, Edge Edge)> Plan(bool ground)
        {
            Cut cut = period.Cut == Cut.GroundSplay ? (ground ? Cut.Splay : Cut.Square) : period.Cut;
            float c = MathF.Min(period.CutMetres, MathF.Min(x, y));
            var plan = new List<(Vector2, Edge)>();
            if (CornerLeft && cut == Cut.Splay)
            {
                plan.Add((new Vector2(-x, -y + c), Edge.Corner));
                plan.Add((new Vector2(-x + c, -y), Edge.Front));
            }
            else if (CornerLeft && cut == Cut.Round)
            {
                for (int k = 0; k < 4; k++) plan.Add((Arc(new Vector2(-x + c, -y + c), c, MathF.PI + (k * MathF.PI / 8f)), Edge.Corner));
                plan.Add((new Vector2(-x + c, -y), Edge.Front));
            }
            else
            {
                plan.Add((new Vector2(-x, -y), Edge.Front));
            }

            if (CornerRight && cut == Cut.Splay)
            {
                plan.Add((new Vector2(x - c, -y), Edge.Corner));
                plan.Add((new Vector2(x, -y + c), Edge.Right));
            }
            else if (CornerRight && cut == Cut.Round)
            {
                for (int k = 0; k < 4; k++) plan.Add((Arc(new Vector2(x - c, -y + c), c, (1.5f * MathF.PI) + (k * MathF.PI / 8f)), Edge.Corner));
                plan.Add((new Vector2(x, -y + c), Edge.Right));
            }
            else
            {
                plan.Add((new Vector2(x, -y), Edge.Right));
            }

            plan.Add((new Vector2(x, y), Edge.Back));
            plan.Add((new Vector2(-x, y), Edge.Left));
            return plan;
        }

        private static Vector2 Arc(Vector2 centre, float r, float angle) => centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r);

        private static Face Along(Vector2 p, Vector2 q, float z, out float length)
        {
            Vector2 d = q - p;
            length = d.Length();
            d /= length;
            return new Face(new Vector3(p.X, p.Y, z), new Vector3(d.X, d.Y, 0f), new Vector3(d.Y, -d.X, 0f));
        }

        private static IEnumerable<(Vector2 P, Vector2 Q, Edge Edge)> Edges(List<(Vector2 At, Edge Edge)> plan)
        {
            for (int i = 0; i < plan.Count; i++) yield return (plan[i].At, plan[(i + 1) % plan.Count].At, plan[i].Edge);
        }

        private static Face Sub(Face face, float u) => face with { Origin = face.Point(u, 0f) };

        public void Walls()
        {
            List<(Vector2 At, Edge Edge)> ground = Plan(ground: true), upper = Plan(ground: false);
            int unit = 0;
            foreach ((Vector2 p, Vector2 q, Edge side) in Edges(ground))
            {
                Face face = Along(p, q, 0f, out float n);
                switch (side)
                {
                    case Edge.Front:
                        foreach ((float u0, float u1) in ShopUnits(n)) ShopUnit(Sub(face, u0), u1 - u0, doorLeft: unit++ % 2 == 0);
                        break;
                    case Edge.Corner:
                        CornerGround(face, n);
                        break;
                    case Edge.Left or Edge.Right when !Shared(side):
                        SideGround(face, n, wrap: side == Edge.Left ? CornerLeft : CornerRight, frontFirst: side == Edge.Right);
                        break;
                    case Edge.Back:
                        w.Facade(face, n, Storey, [new Opening(1f, PlinthHeight, 1f, 2.3f, OpeningKind.Door), new Opening(n - 2.6f, .95f, 1.4f, 1.5f, OpeningKind.Window)]);
                        break;
                    default:
                        w.Facade(face, n, Storey, []);
                        break;
                }

                if (!Party(side)) w.Slab(face, 0f, n, 0f, PlinthHeight, -.04f, .1f, "plinth");
            }

            if (period.Cut == Cut.GroundSplay) GroundSplayCorners();
            float high = Top - Storey;
            if (high <= 0f) return;
            foreach ((Vector2 p, Vector2 q, Edge side) in Edges(upper))
            {
                Face face = Along(p, q, Storey, out float n);
                switch (side)
                {
                    case Edge.Front when period.Upper == Upper.Oriel:
                        foreach ((float u0, float u1) in ShopUnits(n)) Oriel(Sub(face, u0), u1 - u0, high);
                        break;
                    case Edge.Front:
                        foreach ((float u0, float u1) in ShopUnits(n)) UpperWall(Sub(face, u0), u1 - u0, high, null);
                        break;
                    case Edge.Corner:
                        UpperWall(face, n, high, 1);
                        break;
                    case Edge.Left or Edge.Right when !Shared(side):
                        UpperWall(face, n, high, Math.Max(1, (int)MathF.Round(n / 4.5f)));
                        break;
                    case Edge.Back:
                        UpperWall(face, n, high, null);
                        break;
                    default:
                        w.Facade(face, n, high, []);
                        break;
                }
            }

            if (form is ShopForm.TurretCorner or ShopForm.Mansard or ShopForm.InterwarCurve)
            {
                foreach (float z in (ReadOnlySpan<float>)[Storey - .02f, Top - .35f])
                {
                    foreach ((Vector2 p, Vector2 q, Edge side) in Edges(upper))
                    {
                        if (Party(side)) continue;
                        Face face = Along(p, q, 0f, out float n);
                        w.Slab(face, 0f, n, z, z + .25f, -.06f, 0f, "trim");
                    }
                }
            }

            if (form == ShopForm.VictorianGable)
            {
                if (CornerRight) w.Box(new Vector3(x, -y + 4f, Storey + .6f), new Vector3(x + .02f, y - 2f, Top - .5f), Dressed("trim", null, "c9b994"));
                if (CornerLeft) w.Box(new Vector3(-x - .02f, -y + 4f, Storey + .6f), new Vector3(-x, y - 2f, Top - .5f), Dressed("trim", null, "c9b994"));
            }
        }

        /// <summary>A soffit and a column, or a corbel, under the upper storeys where a splay cuts only the ground storey.</summary>
        private void GroundSplayCorners()
        {
            float c = MathF.Min(period.CutMetres, MathF.Min(x, y));
            foreach ((bool here, float sign) in (ReadOnlySpan<(bool, float)>)[(CornerLeft, -1f), (CornerRight, 1f)])
            {
                if (!here) continue;
                float ex = sign * x, ix = sign * (x - c);
                Outward(w, "trim", -Vector3.UnitZ, [new(ix, -y, Storey - .02f), new(ex, -y, Storey - .02f), new(ex, -y + c, Storey - .02f)]);
                if (form == ShopForm.MainStreet)
                {
                    float a = ex - (sign * .45f), b = ex - (sign * .05f);
                    w.Box(new Vector3(MathF.Min(a, b), -y + .05f, PlinthHeight), new Vector3(MathF.Max(a, b), -y + .45f, Storey - .02f), "frame");
                    float a2 = ex - (sign * .55f), b2 = ex + (sign * .05f);
                    w.Box(new Vector3(MathF.Min(a2, b2), -y - .05f, 0f), new Vector3(MathF.Max(a2, b2), -y + .55f, .45f), "trim");
                }
                else
                {
                    float a = ex - (sign * .35f);
                    w.Box(new Vector3(MathF.Min(a, ex), -y, Storey - .7f), new Vector3(MathF.Max(a, ex), -y + .35f, Storey - .02f), "trim");
                }
            }
        }

        private FrontStyle DrawStyle()
        {
            int at = _draw.Pick(9);
            return at switch { < 3 => FrontStyle.Timber, < 5 => FrontStyle.Aluminium, < 7 => FrontStyle.Glass, 7 => FrontStyle.Iron, _ => FrontStyle.Deco };
        }

        /// <summary>One shopfront across a ground-storey stretch: door, display window, frame and fascia.</summary>
        private void ShopUnit(Face face, float width, bool doorLeft)
        {
            FrontStyle style = DrawStyle();
            string paint = _draw.Pick(ShopPaints);
            _lastPaint = paint;
            string door = Dressed("door", null, paint);
            string frame = Dressed("frame", null, style == FrontStyle.Timber ? paint : style is FrontStyle.Aluminium ? "c8ccce" : "33373a");
            string timber = Dressed("trim", null, paint);
            string fascia = Dressed("trim", null, style == FrontStyle.Deco ? "141618" : paint);
            float p = style is FrontStyle.Timber or FrontStyle.Iron ? .3f : .15f;
            float dw = MathF.Min(1.1f, MathF.Max(.9f, width * .22f));
            float riser = style switch { FrontStyle.Timber => .7f, FrontStyle.Iron => .45f, FrontStyle.Deco => .55f, FrontStyle.Aluminium => .5f, _ => .3f };
            float top = style switch { FrontStyle.Iron or FrontStyle.Glass => 3.15f, FrontStyle.Aluminium => 2.75f, FrontStyle.Deco => 2.85f, _ => 2.9f };
            float du = doorLeft ? p + .05f : width - p - .05f - dw;
            (float su0, float su1) = doorLeft ? (du + dw + .15f, width - p - .05f) : (p + .05f, du - .15f);
            var openings = new List<Opening> { new(du, PlinthHeight, dw, top - PlinthHeight - .05f, OpeningKind.Door) };
            bool window = su1 - su0 > .6f;
            if (window) openings.Add(new Opening(su0, riser, su1 - su0, top - riser, OpeningKind.Shop));
            w.Facade(face, width, Storey, openings, door: door, frame: frame);
            w.Slab(face, du + .15f, du + dw - .15f, 1.2f, top - .5f, Reveal + .03f, Reveal + .04f, "glass");
            w.Slab(face, du, du + dw, top - .4f, top - .35f, Reveal, Reveal + .05f, frame);
            if (window)
            {
                w.Slab(face, su0 - .02f, su1 + .02f, PlinthHeight, riser + .02f, -.05f, Reveal, style == FrontStyle.Deco ? fascia : timber);
                w.Slab(face, su0, su1, top - .45f, top - .38f, Reveal - .08f, Reveal, frame);
            }

            switch (style)
            {
                case FrontStyle.Timber:
                    foreach (float a in (ReadOnlySpan<float>)[0f, width - p])
                    {
                        w.Slab(face, a, a + p, PlinthHeight, Fascia0, -.12f, 0f, timber);
                        w.Slab(face, a + .02f, a + p - .02f, Fascia0 - .2f, Fascia1 + .1f, -.32f, 0f, timber);
                    }

                    float post = doorLeft ? du + dw : du - .15f;
                    w.Slab(face, post, post + .15f, PlinthHeight, top, -.06f, 0f, timber);
                    w.Slab(face, p, width - p, Fascia0, Fascia1, -.2f, 0f, fascia);
                    w.Slab(face, 0f, width, Fascia1, Fascia1 + .12f, -.35f, 0f, timber);
                    break;
                case FrontStyle.Iron:
                    foreach (float a in (ReadOnlySpan<float>)[0f, width - .22f]) w.Slab(face, a, a + .22f, PlinthHeight, 3.35f, -.1f, 0f, frame);
                    w.Slab(face, 0f, width, 3.2f, 3.4f, -.12f, 0f, frame);
                    if (form != ShopForm.SixtiesCanopy) w.Slab(face, .5f, width - .5f, Storey + .05f, Storey + .6f, -.08f, 0f, fascia);
                    break;
                case FrontStyle.Deco:
                    w.Slab(face, 0f, width, 2.9f, 3.4f, -.12f, 0f, fascia);
                    w.Slab(face, 0f, width, 2.86f, 2.9f, -.13f, 0f, Dressed("metal", null, "c9ccce"));
                    break;
                case FrontStyle.Aluminium:
                    w.Slab(face, 0f, width, 2.8f, 3.05f, -.08f, 0f, fascia);
                    break;
                case FrontStyle.Glass:
                    w.Slab(face, .15f, width - .15f, 3.2f, 3.4f, -.06f, 0f, fascia);
                    break;
            }

            if (form != ShopForm.SixtiesCanopy && style is not FrontStyle.Deco && _draw.Chance(.5f))
            {
                string awning = Dressed("trim", null, _draw.Pick(AwningPaints));
                float a0 = .25f, a1 = width - .25f, z = style == FrontStyle.Timber ? Fascia0 - .05f : 2.75f;
                Vector3 outward = face.Outward + new Vector3(0f, 0f, 2.7f);
                Outward(w, awning, outward, [face.Point(a0, z), face.Point(a1, z), face.Point(a1, z - .55f, -1.5f), face.Point(a0, z - .55f, -1.5f)]);
                w.Slab(face, a0, a1, z - .8f, z - .55f, -1.52f, -1.48f, awning);
            }

            if (form != ShopForm.SixtiesCanopy && _draw.Chance(.35f))
            {
                float a = doorLeft ? width - .1f : .1f;
                w.Slab(face, a - .03f, a + .03f, 3.9f, 3.95f, -.8f, 0f, "metal");
                w.Slab(face, a - .03f, a + .03f, 3.3f, 3.85f, -.75f, -.25f, fascia);
            }
        }

        /// <summary>A free side wall's ground storey: a shop window turning a corner, then the flats' door.</summary>
        /// <param name="frontFirst">The wall runs back from the street, so its street end is at U = 0.</param>
        private void SideGround(Face face, float width, bool wrap, bool frontFirst)
        {
            float At(float u, float span) => frontFirst ? u : width - u - span;
            float shop = MathF.Min(3.2f, width * .3f);
            bool door = width - 3.2f > (wrap ? shop + .9f : .3f);
            var openings = new List<Opening>();
            if (wrap) openings.Add(new Opening(At(.5f, shop), .7f, shop, 2.2f, OpeningKind.Shop));
            if (door)
            {
                openings.Add(new Opening(At(width - 3.2f, 1f), PlinthHeight, 1f, 2.3f, OpeningKind.Door));
                openings.Add(new Opening(At(width - 1.8f, .9f), 1f, .9f, 1.4f, OpeningKind.Window));
            }

            w.Facade(face, width, Storey, openings);
            if (door) w.Slab(face, At(width - 3.5f, 1.6f), At(width - 3.5f, 1.6f) + 1.6f, 2.75f, 2.9f, -.6f, 0f, "trim");
            if (!wrap) return;
            string timber = Dressed("trim", null, _lastPaint);
            w.Slab(face, At(.2f, shop + .6f), At(.2f, shop + .6f) + shop + .6f, Fascia0, Fascia1, -.2f, 0f, timber);
            w.Slab(face, At(0f, shop + 1f), At(0f, shop + 1f) + shop + 1f, Fascia1, Fascia1 + .12f, -.35f, 0f, timber);
        }

        /// <summary>A splayed or rounded corner's ground storey: a door where the splay is wide enough, display glass where not.</summary>
        private void CornerGround(Face face, float width)
        {
            string timber = Dressed("trim", null, _lastPaint);
            if (width > 1.8f)
            {
                float dw = MathF.Min(1.2f, width - .7f);
                w.Facade(face, width, Storey, [new Opening((width - dw) / 2f, PlinthHeight, dw, 2.55f, OpeningKind.Door)], door: Dressed("door", null, _lastPaint));
                w.Slab(face, ((width - dw) / 2f) + .15f, ((width + dw) / 2f) - .15f, 1.2f, 2.45f, Reveal + .03f, Reveal + .04f, "glass");
            }
            else
            {
                w.Facade(face, width, Storey, [new Opening(.1f, .7f, width - .2f, 2.1f, OpeningKind.Shop)], frame: timber);
            }

            w.Slab(face, 0f, width, Fascia0, Fascia1, -.2f, 0f, timber);
        }

        private (float W, float H, float Sill) UpperSize => period.Upper switch
        {
            Upper.Arched => (1.1f, 2f, .7f),
            Upper.Steel => (2.2f, 1.5f, .95f),
            Upper.Ribbon => (0f, 1.45f, .95f),
            Upper.Tall => (1.5f, 2.6f, .15f),
            Upper.Shutter => (1.1f, 1.8f, .8f),
            _ => (1.1f, 1.9f, .75f),
        };

        /// <summary>An upper wall from the first floor to the eaves, with each window dressed in the form's period.</summary>
        private void UpperWall(Face face, float width, float height, int? count)
        {
            int rows = Math.Max(1, (int)MathF.Round(height / Storey));
            (float ow, float oh, float sill) = UpperSize;
            var openings = new List<Opening>();
            if (period.Upper == Upper.Ribbon)
            {
                if (width > 1.6f)
                {
                    for (int k = 0; k < rows; k++) openings.Add(new Opening(.4f, (k * Storey) + sill, width - .8f, oh, OpeningKind.Window));
                }
            }
            else
            {
                int n = count ?? Math.Max(1, (int)MathF.Round(width / 2.9f));
                ow = MathF.Min(ow, (width / n) - .6f);
                if (ow > .5f)
                {
                    for (int k = 0; k < rows; k++)
                    {
                        for (int i = 0; i < n; i++) openings.Add(new Opening((width * (i + .5f) / n) - (ow / 2f), (k * Storey) + sill, ow, oh, OpeningKind.Window));
                    }
                }
            }

            w.Facade(face, width, height, openings);
            foreach (Opening o in openings) Dress(face, o);
            if (period.Upper == Upper.Ribbon)
            {
                for (int k = 0; k < rows; k++) w.Slab(face, .4f, width - .4f, (k * Storey) + .1f, (k * Storey) + sill, -.02f, 0f, panel);
            }
        }

        /// <summary>Heads, hoods, rails and shutters around one upper window.</summary>
        private void Dress(Face face, Opening o)
        {
            float u = o.U, v = o.V, ow = o.W, oh = o.H;
            switch (period.Upper)
            {
                case Upper.Sash or Upper.Oriel:
                    w.Slab(face, u - .12f, u + ow + .12f, v + oh, v + oh + .22f, -.04f, 0f, "trim");
                    w.Slab(face, u, u + ow, v + (oh * .5f) - .03f, v + (oh * .5f) + .03f, Reveal - .07f, Reveal - .02f, "frame");
                    break;
                case Upper.Arched:
                    w.Slab(face, u - .15f, u + ow + .15f, v + oh, v + oh + .3f, -.08f, 0f, "trim");
                    w.Slab(face, u + (ow / 2f) - .14f, u + (ow / 2f) + .14f, v + oh - .05f, v + oh + .42f, -.1f, 0f, "trim");
                    w.Slab(face, u, u + ow, v + (oh * .55f) - .03f, v + (oh * .55f) + .03f, Reveal - .07f, Reveal - .02f, "frame");
                    break;
                case Upper.Shutter:
                    foreach (float a in (ReadOnlySpan<float>)[u - .55f, u + ow]) w.Slab(face, a + .03f, a + .52f, v - .02f, v + oh + .02f, -.06f, -.02f, shutter);
                    w.Slab(face, u - .15f, u + ow + .15f, v + .95f, v + 1f, -.45f, -.4f, "metal");
                    w.Slab(face, u - .2f, u + ow + .2f, v - .12f, v, -.5f, 0f, "trim");
                    break;
                case Upper.Tall:
                    w.Slab(face, u + .05f, u + ow - .05f, v + .95f, v + 1f, Reveal - .06f, Reveal - .02f, "metal");
                    break;
            }
        }

        /// <summary>A canted oriel over a shop on the first floor, with sashes on the storeys above.</summary>
        private void Oriel(Face face, float width, float height)
        {
            float centre = face.Origin.X + (width / 2f), bw = MathF.Min(3f, width - .8f);
            if (bw >= 1.6f)
            {
                Canted(w, centre, bw, -y, -y - .65f, Storey + .2f, (2f * Storey) - .3f, [(Storey + .75f, Storey + 2.75f)]);
                w.Box(new Vector3(centre - (bw / 2f) + .2f, -y - .5f, Storey - .15f), new Vector3(centre + (bw / 2f) - .2f, -y, Storey + .2f), "trim");
            }

            if (height > Storey + .1f)
            {
                var above = new Face(face.Origin + new Vector3(0f, 0f, Storey), face.Along, face.Outward);
                w.Facade(face, width, Storey, []);
                UpperWall(above, width, height - Storey, null);
            }
            else
            {
                w.Facade(face, width, height, []);
            }
        }

        /// <summary>A membrane deck behind a parapet, a corbelled cornice or a thin fascia, with a roof hatch.</summary>
        public void FlatRoof(float height)
        {
            List<(Vector2 At, Edge Edge)> plan = Plan(ground: false);
            Span<Vector3> deck = stackalloc Vector3[plan.Count];
            for (int i = 0; i < plan.Count; i++) deck[i] = new Vector3(plan[i].At.X, plan[i].At.Y, height + .02f);
            Outward(w, "membrane", Vector3.UnitZ, deck);
            if (form == ShopForm.SixtiesCanopy)
            {
                foreach ((Vector2 p, Vector2 q, Edge side) in Edges(plan))
                {
                    Face face = Along(p, q, 0f, out float n);
                    w.Slab(face, 0f, n, height - .35f, height + .25f, Party(side) ? 0f : -.5f, 0f, "trim");
                    if (side == Edge.Front || side is Edge.Left or Edge.Right && !Shared(side)) w.Slab(face, 0f, n, 3.05f, 3.3f, -2f, 0f, "trim");
                }

                if (CornerRight) w.Box(new Vector3(x, -y - 2f, 3.05f), new Vector3(x + 2f, -y, 3.3f), "trim");
                if (CornerLeft) w.Box(new Vector3(-x - 2f, -y - 2f, 3.05f), new Vector3(-x, -y, 3.3f), "trim");
            }
            else
            {
                float parapet = form == ShopForm.MainStreet ? .9f : .7f;
                foreach ((Vector2 p, Vector2 q, Edge side) in Edges(plan))
                {
                    Face face = Along(p, q, 0f, out float n);
                    w.Slab(face, 0f, n, height, height + parapet, 0f, .25f, "wall");
                    w.Slab(face, 0f, n, height + parapet, height + parapet + .08f, Party(side) ? 0f : -.05f, .3f, "trim");
                    bool street = side is Edge.Front or Edge.Corner || side is Edge.Left or Edge.Right && !Shared(side);
                    if (form == ShopForm.MainStreet && street)
                    {
                        w.Slab(face, 0f, n, height - .55f, height - .4f, -.1f, 0f, "wall");
                        w.Slab(face, 0f, n, height - .4f, height - .25f, -.2f, 0f, "wall");
                        w.Slab(face, 0f, n, height - .25f, height - .1f, -.32f, 0f, "wall");
                        w.Slab(face, 0f, n, height - .1f, height + .05f, -.42f, 0f, "trim");
                    }

                    if (form == ShopForm.InterwarCurve && side == Edge.Corner)
                    {
                        w.Slab(face, 0f, n, height + parapet, height + parapet + 1.4f, 0f, .25f, "wall");
                        w.Slab(face, 0f, n, height + parapet + 1.4f, height + parapet + 1.5f, -.05f, .3f, "trim");
                    }
                }

                if (form == ShopForm.MainStreet && x >= 4f)
                {
                    float pw = MathF.Min(4f, x * .7f), top = height + parapet;
                    w.Box(new Vector3(-pw, -y, top), new Vector3(pw, -y + .3f, top + .9f), "wall");
                    w.Box(new Vector3(-pw + .4f, -y - .04f, top + .15f), new Vector3(pw - .4f, -y, top + .75f), "trim");
                    w.Box(new Vector3(-pw - .05f, -y - .05f, top + .9f), new Vector3(pw + .05f, -y + .35f, top + 1f), "trim");
                }

                if (form == ShopForm.InterwarCurve)
                {
                    float c = MathF.Min(period.CutMetres, MathF.Min(x, y));
                    foreach ((bool here, float sign) in (ReadOnlySpan<(bool, float)>)[(CornerLeft, -1f), (CornerRight, 1f)])
                    {
                        if (!here) continue;
                        float px = sign * (x - (c * .4f)), py = -y + (c * .4f);
                        w.Box(new Vector3(px - .04f, py - .04f, height + parapet), new Vector3(px + .04f, py + .04f, height + parapet + 5f), "metal");
                    }
                }
            }

            w.Box(new Vector3(-.8f, 1.5f, height), new Vector3(.8f, 2.5f, height + .5f), "metal");
        }

        /// <summary>The top storey inside a slate mansard, a dormer over each upper window, over walls a storey lower.</summary>
        public void Mansard()
        {
            var front = new List<float>();
            foreach ((float u0, float u1) in ShopUnits(2f * x))
            {
                int n = Math.Max(1, (int)MathF.Round((u1 - u0) / 2.9f));
                for (int i = 0; i < n; i++) front.Add(-x + u0 + ((u1 - u0) * (i + .5f) / n));
            }

            w.Box(new Vector3(-x, -y - .45f, Top - .35f), new Vector3(x, -y, Top), "trim");
            MansardRoof(w, x, y, Top, attached, [.. front], [.. front], .6f);
        }

        /// <summary>The top storey 2.4 m behind a glazed terrace, clad in zinc, over walls a storey lower.</summary>
        public void SetbackTop()
        {
            float eaves = Top, back = -y + SetbackMetres, frontage = 2f * x;
            w.Box(new Vector3(-x + .1f, -y + .1f, eaves - .2f), new Vector3(x - .1f, back, eaves + .05f), "membrane");
            w.Box(new Vector3(-x, -y - .2f, eaves - .3f), new Vector3(x, -y + .15f, eaves + .12f), "trim");
            w.Box(new Vector3(-x + .1f, -y + .02f, eaves + .12f), new Vector3(x - .1f, -y + .06f, eaves + 1.1f), "glass");
            int n = Math.Max(1, (int)MathF.Round(frontage / 2.6f));
            var onto = new List<Opening>();
            for (int i = 0; i < n; i++) onto.Add(new Opening((frontage * (i + .5f) / n) - .8f, .15f, 1.6f, 2.6f, OpeningKind.Window));
            w.Facade(new Face(new Vector3(-x, back, eaves), Vector3.UnitX, -Vector3.UnitY), frontage, Storey, onto, wall: zinc);
            w.Facade(new Face(new Vector3(x, y, eaves), -Vector3.UnitX, Vector3.UnitY), frontage, Storey, [], wall: zinc);
            w.Facade(new Face(new Vector3(-x, y, eaves), -Vector3.UnitY, -Vector3.UnitX), y - back, Storey, [], wall: zinc);
            w.Facade(new Face(new Vector3(x, back, eaves), Vector3.UnitY, Vector3.UnitX), y - back, Storey, [], wall: zinc);
            float roof = eaves + Storey;
            w.Box(new Vector3(-x, back, roof - .1f), new Vector3(x, y, roof + .02f), "membrane");
            float l = attached.HasFlag(AttachedSides.Left) ? x : x + .15f, r = attached.HasFlag(AttachedSides.Right) ? x : x + .15f;
            w.Box(new Vector3(-l, back - .15f, roof - .1f), new Vector3(r, y + .15f, roof + .2f), zinc);
        }

        /// <summary>An octagonal turret over a splayed corner, a storey above the eaves, under a slate spire.</summary>
        public void Turret(bool right, float height)
        {
            float c = MathF.Min(period.CutMetres, MathF.Min(x, y));
            float sign = right ? 1f : -1f;
            var centre = new Vector2(sign * (x - (c / 2f) + .25f), -y + (c / 2f) - .25f);
            float r = (c / 2f) + .35f, z0 = height - .3f, z1 = height + 3f, z2 = height + 7.2f;
            Span<Vector2> ring = stackalloc Vector2[8];
            for (int k = 0; k < 8; k++) ring[k] = Arc(centre, r, (MathF.PI / 8f) + (k * MathF.PI / 4f));
            for (int k = 0; k < 8; k++)
            {
                Vector2 p = ring[k], q = ring[(k + 1) % 8];
                Face face = Along(p, q, z0, out float n);
                w.Facade(face, n, z1 - z0, [new Opening(.25f, .9f, n - .5f, 1.7f, OpeningKind.Window)]);
                w.Slab(face, 0f, n, z1 - z0 - .3f, z1 - z0, -.08f, 0f, "trim");
                Vector2 ep = centre + ((p - centre) * 1.12f), eq = centre + ((q - centre) * 1.12f);
                Vector2 mid = ((ep + eq) / 2f) - centre;
                Outward(w, "roof", new Vector3(mid.X, mid.Y, 1f), [new(ep.X, ep.Y, z1), new(eq.X, eq.Y, z1), new(centre.X, centre.Y, z2)]);
            }

            w.Box(new Vector3(centre.X - .06f, centre.Y - .06f, z2), new Vector3(centre.X + .06f, centre.Y + .06f, z2 + 1f), "metal");
        }
    }
}
