using System.Numerics;

namespace Borough.Appearance;

/// <summary>Which grounds a mansion's body is dressed for.</summary>
public enum MansionGrounds : byte
{
    /// <summary>Several Households: side doors on the end walls, parking beside the house.</summary>
    Apartments,

    /// <summary>One Household: one front door, a gated drive and a garage.</summary>
    OneHousehold,
}

/// <summary>The plan a mansion's masses make.</summary>
public enum MansionPlanKind : byte
{
    Bar,
    UFront,
    URear,
    H,
    T,
    LFront,
    Ell,
    Angled,
    Court,
}

/// <summary>What one mass of a mansion is.</summary>
public enum MansionPart : byte
{
    Main,
    Arm,
    Garage,
    Rear,
    Hub,
    Wing,
    Arcade,

    /// <summary>A lower wing beside the main block, set back from its front.</summary>
    Flank,

    /// <summary>A one-storey link from the house to a garage.</summary>
    Breezeway,
}

/// <summary>
/// One rectangular mass of a mansion in the body's frame: x along the Street, y back from it, before it
/// turns by <paramref name="Turn"/> radians about (<paramref name="PivotX"/>, <paramref name="PivotY"/>).
/// </summary>
public readonly record struct MansionMass(MansionPart Part, float X0, float X1, float Y0, float Y1, int Storeys,
    float Turn = 0f, float PivotX = 0f, float PivotY = 0f);

/// <summary>A mansion's plan: its masses, the side its garage stands on, and the x of its front door.</summary>
public sealed record MansionPlan(MansionPlanKind Kind, MansionMass[] Masses, int GarageSide, float Entry, float Front);

/// <summary>
/// The US mansion forms. Each style is a parts grammar drawn per Lot: a main block, arms that stand
/// forward or run back, an angled wing on a hub, a rear ell, and the style's own porticos, turrets,
/// towers and porches. The front yard is the shell's; this body fills the footprint behind it.
/// </summary>
/// <remarks>
/// <c>scripts/art/mansion-forms.py</c> is the reference. It runs the same plan grammar on the same draw,
/// and <c>art/mansion-forms/bodies.json</c> holds its plans, which the tests hold this file to. A mansion
/// may draw much less floor than its Building holds, because its grounds are part of its look
/// (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>Whether a house form is a mansion.</summary>
    public static bool IsMansion(HouseVariant variant) => variant >= HouseVariant.MansionColonial;

    private static readonly MansionPlanKind[][] MansionPlans =
    [
        [MansionPlanKind.UFront, MansionPlanKind.H, MansionPlanKind.T, MansionPlanKind.URear],
        [MansionPlanKind.LFront, MansionPlanKind.Ell, MansionPlanKind.T],
        [MansionPlanKind.Angled, MansionPlanKind.Ell, MansionPlanKind.LFront],
        [MansionPlanKind.UFront, MansionPlanKind.LFront, MansionPlanKind.H, MansionPlanKind.T],
        [MansionPlanKind.Court, MansionPlanKind.Angled, MansionPlanKind.LFront, MansionPlanKind.T],
        [MansionPlanKind.T, MansionPlanKind.LFront, MansionPlanKind.Ell],
        [MansionPlanKind.UFront, MansionPlanKind.URear, MansionPlanKind.T, MansionPlanKind.H],
        [MansionPlanKind.LFront, MansionPlanKind.Ell, MansionPlanKind.T],
        [MansionPlanKind.LFront, MansionPlanKind.T, MansionPlanKind.Ell],
        [MansionPlanKind.Angled, MansionPlanKind.LFront, MansionPlanKind.UFront, MansionPlanKind.T],
        [MansionPlanKind.Angled, MansionPlanKind.LFront, MansionPlanKind.Ell],
    ];

    // The least frontage, in metres, each style keeps for its main block between arms.
    private static readonly float[] MansionLeastMain = [12f, 13f, 11f, 14f, 14f, 13f, 12f, 13f, 12f, 13f, 13f];

    // How far a style's porch, portico or porte-cochere stands in front of its main block.
    private static readonly float[] MansionStoop = [3f, 1.6f, 2.8f, 5f, 1.5f, 2f, 2.8f, 1.6f, 2.8f, 1.6f, 1.8f];

    // The main block's widest frontage, arms over it included, is this many times the house depth plus
    // 2 m a storey over two, kept between 16 m and the style's most.
    private static readonly float[] MansionAspect = [1.6f, 1.5f, 1.3f, 1.7f, 1.7f, 1.8f, 1.6f, 1.8f, 1.5f, 1.6f, 1.45f];
    private static readonly float[] MansionMostMain = [30f, 28f, 24f, 34f, 34f, 34f, 30f, 32f, 28f, 30f, 32f];

    // The share of the frontage the house leaves free that falls on its garage side.
    private static readonly float[] MansionGarageShare = [.5f, .7f, .75f, .6f, .6f, .65f, .5f, .8f, .7f, .75f, .8f];

    private const float MansionEaves = .45f;

    // The shallowest main block a plan with wings keeps. Deep yards leave many houses 8-13 m deep.
    private const float LeastMain = 6.5f;

    private static int StyleOf(HouseVariant variant) => variant - HouseVariant.MansionColonial;

    private static bool GarageArm(HouseVariant style) => style is HouseVariant.MansionColonial or HouseVariant.MansionRegency
        or HouseVariant.MansionChateau or HouseVariant.MansionNeoEclectic;

    private static bool Symmetric(HouseVariant style) => style is HouseVariant.MansionColonial or HouseVariant.MansionRegency;

    /// <summary>The widest a mansion's main block draws on a footprint <paramref name="depth"/> deep.</summary>
    public static float MainBlockCap(HouseVariant style, float depth, int storeys)
    {
        int index = StyleOf(style);
        return Math.Clamp((MansionAspect[index] * depth) + (2f * Math.Max(0, storeys - 2)), 16f, MansionMostMain[index]);
    }

    private static bool LowArms(HouseVariant style) => style is HouseVariant.MansionPrairie or HouseVariant.MansionMidCentury
        or HouseVariant.MansionCalifornia;

    /// <summary>The masses a mansion of this style stands in on a footprint <paramref name="frontage"/> by <paramref name="depth"/>.</summary>
    public static MansionPlan PlanMansion(HouseVariant style, float frontage, float depth, int storeys, int seed, MansionGrounds grounds)
    {
        if (!IsMansion(style)) throw new ArgumentException("Only a mansion form has a mansion plan.", nameof(style));
        var r = new HouseDraw(((ulong)seed << 8) | 0x4D);
        float aw = r.Range(7f, 9.5f), proj = r.Range(3.5f, 6f), md = r.Range(10f, 13f), rd = r.Range(5f, 8f), ew = r.Range(7f, 9.5f);
        float th = r.Range(30f, 42f) * MathF.PI / 180f, lw = r.Range(9.5f, 12f), wd = r.Range(6.5f, 8f);
        int gside = r.Pick(2) == 0 ? -1 : 1;
        int drop = r.Pick(3) == 2 ? -1 : 0;

        int index = StyleOf(style);
        float hw = frontage / 2f, hd = depth / 2f;
        float margin = MathF.Min(3f, frontage * .06f);
        float lane = grounds == MansionGrounds.OneHousehold && frontage >= 32f ? MathF.Max(margin, 3.5f) : margin;
        float lx0 = -hw + (gside < 0 ? lane : margin), lx1 = hw - (gside > 0 ? lane : margin), span = lx1 - lx0;
        float yf = -hd, yb = hd;
        float least = MansionLeastMain[index], leastU = least + (style == HouseVariant.MansionChateau ? 8f : 0f);
        float stoop = MathF.Min(MansionStoop[index], depth * .25f);
        aw = MathF.Min(aw, MathF.Max(6f, span * .22f));
        proj = MathF.Max(stoop, MathF.Min(proj, depth * .3f));
        float hs = wd + 1.2f;
        (float sin, float cos) = MathF.SinCos(th);
        float reserve = (hs / 2f) + ((lw + (hs / 2f)) * cos) + (wd / 2f * sin);
        float lift = MathF.Max(stoop, ((lw + (hs / 2f)) * sin) + (wd / 2f * cos) - (hs / 2f));

        float rearRoom = MathF.Min(4f, depth * .25f);
        float cap = MainBlockCap(style, depth, storeys);
        float box = MathF.Min(span, cap), pair = MathF.Min(aw, MathF.Max(5f, box * .25f));
        float angledMain = MathF.Min(span - reserve, cap), courtMain = MathF.Min(span - (2f * pair), cap);
        float Core(MansionPlanKind p) => p switch
        {
            MansionPlanKind.Angled => angledMain + reserve,
            MansionPlanKind.Court => courtMain + (2f * pair),
            _ => box,
        };
        bool Ok(MansionPlanKind p) => p switch
        {
            MansionPlanKind.UFront => box - (2f * pair) >= leastU && depth - proj >= LeastMain,
            MansionPlanKind.H => box - (2f * pair) >= leastU && depth >= 12f && depth - proj - rearRoom >= LeastMain,
            MansionPlanKind.URear => box - (2f * aw) >= 6f && depth >= 12f && depth - stoop - rearRoom >= LeastMain,
            MansionPlanKind.LFront => box - aw >= least && depth - proj >= LeastMain,
            MansionPlanKind.Court => Core(p) >= 34f && courtMain >= least && depth - proj >= LeastMain,
            MansionPlanKind.Angled => angledMain >= least && depth - lift >= MathF.Max(LeastMain, hs),
            MansionPlanKind.T or MansionPlanKind.Ell => depth >= 12f && depth - stoop - rearRoom >= LeastMain,
            _ => true,
        };

        MansionPlanKind[] order = MansionPlans[index];
        MansionPlanKind plan = MansionPlanKind.Bar;
        for (int i = 0; i < order.Length; i++)
        {
            MansionPlanKind candidate = order[(seed + i) % order.Length];
            if (!Ok(candidate)) continue;
            plan = candidate;
            break;
        }

        if (plan is MansionPlanKind.UFront or MansionPlanKind.H or MansionPlanKind.Court) aw = pair;
        float free = span - Core(plan);
        float toGarage = Symmetric(style) ? free / 2f : free * MansionGarageShare[index];
        float x0 = lx0 + (gside > 0 ? free - toGarage : toGarage), x1 = x0 + Core(plan);

        bool forward = plan is MansionPlanKind.UFront or MansionPlanKind.H or MansionPlanKind.LFront or MansionPlanKind.Court;
        bool rear = plan is MansionPlanKind.H or MansionPlanKind.URear or MansionPlanKind.T or MansionPlanKind.Ell;
        float front = plan == MansionPlanKind.Angled ? yf + lift : forward ? yf + proj : yf + stoop;
        float back = MathF.Min(front + md, rear ? yb - rearRoom : yb);
        if (back - front < 6f) back = MathF.Min(yb, front + 6f);
        float rearEnd = MathF.Min(back + rd, yb);
        int armStoreys = Math.Max(1, storeys + (LowArms(style) ? -1 : drop));
        bool single = grounds == MansionGrounds.OneHousehold;

        float mx0 = plan == MansionPlanKind.Court ? x0 + aw : x0, mx1 = plan == MansionPlanKind.Court ? x1 - aw : x1;
        var arms = new List<MansionMass>();
        MansionPart ArmPart(int side) => single && side == gside && (GarageArm(style) || plan is MansionPlanKind.H or MansionPlanKind.URear)
            ? MansionPart.Garage
            : MansionPart.Arm;
        switch (plan)
        {
            case MansionPlanKind.UFront or MansionPlanKind.H or MansionPlanKind.Court:
                {
                    float end = plan == MansionPlanKind.H ? rearEnd : back;
                    arms.Add(new MansionMass(ArmPart(-1), x0, x0 + aw, yf, end, armStoreys));
                    arms.Add(new MansionMass(ArmPart(1), x1 - aw, x1, yf, end, armStoreys));
                    if (plan == MansionPlanKind.Court) arms.Add(new MansionMass(MansionPart.Arcade, x0 + aw, x1 - aw, yf, yf + 3.2f, 1));
                    break;
                }

            case MansionPlanKind.URear:
                arms.Add(new MansionMass(ArmPart(-1), x0, x0 + aw, back - 1.5f, rearEnd, armStoreys));
                arms.Add(new MansionMass(ArmPart(1), x1 - aw, x1, back - 1.5f, rearEnd, armStoreys));
                break;
            case MansionPlanKind.LFront:
                {
                    int side = style == HouseVariant.MansionNeoEclectic ? gside : -gside;
                    arms.Add(side < 0
                        ? new MansionMass(ArmPart(side), x0, x0 + aw, yf, back, armStoreys)
                        : new MansionMass(ArmPart(side), x1 - aw, x1, yf, back, armStoreys));
                    break;
                }

            case MansionPlanKind.Angled:
                {
                    int side = single ? gside : -gside;
                    float xe = side < 0 ? x0 + reserve : x1 - reserve;
                    (mx0, mx1) = side < 0 ? (xe, x1) : (x0, xe);
                    float ha = MathF.Min(xe - (side * .3f), xe + (side * hs)), hb = MathF.Max(xe - (side * .3f), xe + (side * hs));
                    float pcx = (ha + hb) / 2f, pcy = front + (hs / 2f);
                    float reach = lw + (hs / 2f);
                    arms.Add(new MansionMass(MansionPart.Hub, ha, hb, front, front + hs, storeys));
                    arms.Add(new MansionMass(single ? MansionPart.Garage : MansionPart.Wing, MathF.Min(pcx, pcx + (side * reach)), MathF.Max(pcx, pcx + (side * reach)),
                        pcy - (wd / 2f), pcy + (wd / 2f), armStoreys, -side * th, pcx, pcy));
                    break;
                }
        }

        if (plan is MansionPlanKind.T or MansionPlanKind.Ell && rearEnd - back >= 3f)
        {
            float width = MathF.Min(ew, (mx1 - mx0) * .6f);
            float at = plan == MansionPlanKind.T ? ((mx0 + mx1) / 2f) - (width / 2f) : gside > 0 ? mx0 + .8f : mx1 - .8f - width;
            arms.Add(new MansionMass(MansionPart.Rear, at, at + width, back - 1.5f, rearEnd, Math.Max(1, storeys - 1)));
        }

        Flanks(arms, style, single, gside, x0, x1, lx0, lx1, front, back, yb, storeys);

        bool garaged = arms.Exists(a => a.Part == MansionPart.Garage);
        if (single && !garaged && yb - back >= 5f)
        {
            float room = plan == MansionPlanKind.T ? ((mx1 - mx0) / 2f) - (MathF.Min(ew, (mx1 - mx0) * .6f) / 2f) - .5f : 7.5f;
            float wide = MathF.Min(7.5f, room);
            if (wide >= 5f)
            {
                (float g0, float g1) = gside > 0 ? (mx1 - wide, mx1) : (mx0, mx0 + wide);
                arms.Add(new MansionMass(MansionPart.Garage, g0, g1, back - .5f, MathF.Min(back + 7f, yb), 1));
            }
        }

        if (single && !arms.Exists(a => a.Part == MansionPart.Garage))
        {
            int outer = arms.FindIndex(a => a.Part == MansionPart.Arm && (gside > 0 ? a.X1 >= x1 - .01f : a.X0 <= x0 + .01f));
            if (outer >= 0)
            {
                arms[outer] = arms[outer] with { Part = MansionPart.Garage };
            }
            else if (mx1 - mx0 - 7.5f >= least)
            {
                (float g0, float g1) = gside > 0 ? (mx1 - 7.5f, mx1) : (mx0, mx0 + 7.5f);
                arms.Add(new MansionMass(MansionPart.Garage, g0, g1, front + 1.5f, MathF.Min(front + 8.5f, MathF.Max(back, front + 6f)), 1));
                if (gside > 0) mx1 = g0;
                else mx0 = g1;
            }
        }

        float entry = (mx0 + mx1) / 2f;
        if (plan == MansionPlanKind.LFront)
        {
            MansionMass arm = arms[0];
            entry = arm.X0 <= mx0 + .01f ? (arm.X1 + mx1) / 2f : (mx0 + arm.X0) / 2f;
        }

        return new MansionPlan(plan, [new MansionMass(MansionPart.Main, mx0, mx1, front, back, storeys), .. arms], gside, entry, front);
    }

    /// <summary>
    /// Fills the frontage beside the house: a lower wing set back on each side for a symmetric style,
    /// and otherwise a garage, a breezeway to a garage, or a lower wing on the garage side and a lower
    /// wing on the other side where it has room. What is left stays garden.
    /// </summary>
    private static void Flanks(List<MansionMass> arms, HouseVariant style, bool single, int gside, float x0, float x1, float lx0, float lx1,
        float front, float back, float yb, int storeys)
    {
        float step = MathF.Min(2.5f, (back - front) * .25f);
        float y0 = front + step, y1 = MathF.Min(yb, MathF.Max(y0 + 4f, back - .5f));
        int low = Math.Max(1, storeys - 1);
        bool garaged = arms.Exists(a => a.Part == MansionPart.Garage);
        float toGarage = gside > 0 ? lx1 - x1 : x0 - lx0, away = gside > 0 ? x0 - lx0 : lx1 - x1;
        MansionMass Beside(MansionPart part, int side, float gap, float width, float b0, float b1, int levels) => side > 0
            ? new MansionMass(part, x1 + gap, x1 + gap + width, b0, b1, levels)
            : new MansionMass(part, x0 - gap - width, x0 - gap, b0, b1, levels);

        if (Symmetric(style))
        {
            float width = MathF.Min(9f, MathF.Min(toGarage, away));
            if (width < 5f) return;
            arms.Add(Beside(single && !garaged ? MansionPart.Garage : MansionPart.Flank, gside, 0f, width, y0, y1, low));
            arms.Add(Beside(MansionPart.Flank, -gside, 0f, width, y0, y1, low));
            return;
        }

        if (single && !garaged && toGarage >= 6.5f)
        {
            float link = toGarage >= 9.5f ? 3f : 0f;
            float width = MathF.Min(8.4f, toGarage - link);
            if (link > 0f) arms.Add(Beside(MansionPart.Breezeway, gside, 0f, link, y0 + 1f, MathF.Min(yb, y0 + 4.5f), 1));
            arms.Add(Beside(MansionPart.Garage, gside, link, width, y0, MathF.Min(yb, y0 + 7f), 1));
        }
        else if (!garaged && toGarage >= 5f)
        {
            arms.Add(Beside(MansionPart.Flank, gside, 0f, MathF.Min(8f, toGarage), y0, y1, low));
        }

        if (away >= 7f) arms.Add(Beside(MansionPart.Flank, -gside, 0f, MathF.Min(7f, away), y0, y1, low));
    }

    private enum MansionRoof : byte
    {
        Hip,
        Gable,
        Mansard,
        Flat,
        Shed,
    }

    private sealed class MansionLook
    {
        public HouseVariant Style;
        public required string Wall, Roof, Trim, Frame, Door, Stack;
        public string? Ground;
        public string End = string.Empty, Accent = string.Empty, Shutter = string.Empty, Metal = Dressed("metal", null, "1e2123");
        public MansionRoof Main, Arms;
        public float Pitch, ArmPitch, Eaves = MansionEaves;
        public float Bay = 3.2f, WindowWidth = 1.2f, WindowHeight = 1.9f, Sill = .75f;
        public bool Shutters, HalfTimber, Dormers, Bands, Cornice, Portico, TallPortico;
        public int CrossGables, Chimneys;
        public bool Gabled => Main is MansionRoof.Gable;

        public static MansionLook Draw(HouseVariant style, int seed)
        {
            var r = new HouseDraw(((ulong)seed << 8) | 0x4E);
            string natural = r.Pick(NaturalBricks);
            return style switch
            {
                HouseVariant.MansionColonial => Colonial(r, natural),
                HouseVariant.MansionTudor => Tudor(r),
                HouseVariant.MansionQueenAnne => QueenAnne(r),
                HouseVariant.MansionChateau => Chateau(r),
                HouseVariant.MansionSpanish => Spanish(r),
                HouseVariant.MansionPrairie => Prairie(r),
                HouseVariant.MansionRegency => Regency(r),
                HouseVariant.MansionMidCentury => MidCentury(r),
                HouseVariant.MansionFarmhouse => Farmhouse(r),
                HouseVariant.MansionNeoEclectic => NeoEclectic(r),
                _ => California(r),
            };
        }

        private static MansionLook Colonial(HouseDraw r, string natural)
        {
            string wall = r.Pick(3) switch
            {
                0 => Dressed("wall", natural, null),
                1 => Dressed("wall", "bricks-088", r.Pick(["d8d6d0", "d6cfbf", "cfcac0"])),
                _ => Dressed("wall", "white-planks-clean", r.Pick(["d8d6d0", "cfd3c6", "c9cfd0", "d3c6a0"])),
            };
            MansionRoof roof = r.Chance(.55f) ? MansionRoof.Gable : MansionRoof.Hip;
            float pitch = r.Range(30f, 36f);
            return new MansionLook
            {
                Style = HouseVariant.MansionColonial,
                Wall = wall,
                End = wall,
                Roof = r.Pick([Dressed("roof", null, "353e44"), Dressed("roof", null, "4d463f"), Dressed("roof", "roofing-tiles-008", null)]),
                Trim = Dressed("trim", null, "d8d6d0"),
                Frame = Dressed("frame", null, "d6d2c8"),
                Door = Dressed("door", null, r.Pick(["2f4a5c", "6a2f2a", "1e2123", "2f4a3c"])),
                Stack = Dressed("wall-end", natural, null),
                Shutter = Dressed("frame", null, r.Pick(["1e2123", "2f4a3c", "24303a"])),
                Main = roof,
                Arms = roof,
                Pitch = pitch,
                ArmPitch = pitch,
                Bay = 3f,
                WindowWidth = 1.1f,
                WindowHeight = 2f,
                Shutters = r.Chance(.7f),
                Dormers = roof == MansionRoof.Gable && r.Chance(.6f),
                Cornice = true,
                Portico = true,
                TallPortico = r.Chance(.75f),
                Chimneys = 2,
            };
        }

        private static MansionLook Tudor(HouseDraw r)
        {
            string ground = r.Pick([Dressed("wall", "bricks-066", null), Dressed("wall", "bricks-085", null), Dressed("wall", "stone-wall", null)]);
            bool timber = r.Chance(.6f);
            string upper = timber ? Dressed("wall", "plaster-001", r.Pick(["d8d0bc", "d6cfbf", "cfc4b0"])) : ground;
            return new MansionLook
            {
                Style = HouseVariant.MansionTudor,
                Wall = upper,
                Ground = ground,
                End = upper,
                Roof = r.Pick([Dressed("roof", null, "3d4a52"), Dressed("roof", "roofing-tiles-007", null), Dressed("roof", null, "4d463f")]),
                Trim = Dressed("trim", null, "3a2f28"),
                Frame = Dressed("frame", null, "2b2e31"),
                Door = Dressed("door", null, r.Pick(["5a3a28", "4a3a2a", "3a2f28"])),
                Stack = Dressed("wall-end", "bricks-066", null),
                Main = MansionRoof.Gable,
                Arms = MansionRoof.Gable,
                Pitch = r.Range(46f, 52f),
                ArmPitch = r.Range(50f, 55f),
                Bay = 2.8f,
                WindowWidth = 1f,
                WindowHeight = 1.7f,
                HalfTimber = timber,
                CrossGables = 1 + r.Pick(2),
                Chimneys = 1,
            };
        }

        private static MansionLook QueenAnne(HouseDraw r)
        {
            string wall = Dressed("wall", "white-planks-clean", r.Pick(["b48f7a", "8e9b86", "9aa7b4", "d3c6a0", "c4a48a", "a7aeb0"]));
            return new MansionLook
            {
                Style = HouseVariant.MansionQueenAnne,
                Wall = wall,
                End = Dressed("wall-end", "wood-siding-005", null),
                Roof = r.Pick([Dressed("roof", null, "4d463f"), Dressed("roof", null, "5a3a32"), Dressed("roof", null, "353e44")]),
                Trim = Dressed("trim", null, r.Pick(["d8d6d0", "d6cfbf"])),
                Frame = Dressed("frame", null, "d6d2c8"),
                Door = Dressed("door", null, r.Pick(["6a2f2a", "2f4a3c", "7a5a2c"])),
                Stack = Dressed("wall-end", "bricks-051", null),
                Main = MansionRoof.Gable,
                Arms = MansionRoof.Gable,
                Pitch = r.Range(45f, 50f),
                ArmPitch = r.Range(48f, 52f),
                Bay = 2.8f,
                WindowWidth = 1f,
                WindowHeight = 2f,
                CrossGables = 1,
                Chimneys = 2,
            };
        }

        private static MansionLook Chateau(HouseDraw r)
        {
            string wall = r.Pick([Dressed("wall", "stone-wall", null), Dressed("wall", "rock-wall-16", null), Dressed("wall", "rough-block-wall", "d0c8b8")]);
            float pitch = r.Range(56f, 62f);
            return new MansionLook
            {
                Style = HouseVariant.MansionChateau,
                Wall = wall,
                End = wall,
                Roof = Dressed("roof", null, r.Pick(["2b2e31", "353e44", "3d4a52"])),
                Trim = Dressed("trim", null, "d8d0bc"),
                Frame = Dressed("frame", null, "d6d2c8"),
                Door = Dressed("door", null, "3a2f28"),
                Stack = Retag(wall, "wall-end"),
                Main = MansionRoof.Hip,
                Arms = MansionRoof.Hip,
                Pitch = pitch,
                ArmPitch = pitch,
                Bay = 3f,
                WindowWidth = 1.2f,
                WindowHeight = 2.2f,
                Dormers = true,
                Cornice = true,
                Chimneys = 2,
            };
        }

        private static MansionLook Spanish(HouseDraw r)
        {
            string wall = Dressed("wall", "painted-plaster-wall", r.Pick(["d8d0bc", "d8d6d0", "d6c9b0", "d3c6a0"]));
            MansionRoof roof = r.Chance(.6f) ? MansionRoof.Hip : MansionRoof.Gable;
            return new MansionLook
            {
                Style = HouseVariant.MansionSpanish,
                Wall = wall,
                End = wall,
                Roof = r.Pick([Dressed("roof", "clay-roof-tiles-02", null), Dressed("roof", "roofing-tiles-003", null)]),
                Trim = Retag(wall, "trim"),
                Frame = Dressed("frame", null, "3a2f28"),
                Door = Dressed("door", null, "5a3a28"),
                Stack = Retag(wall, "wall-end"),
                Main = roof,
                Arms = roof,
                Pitch = r.Range(20f, 24f),
                ArmPitch = r.Range(20f, 24f),
                Bay = 3.4f,
                WindowWidth = 1.1f,
                WindowHeight = 1.9f,
                Chimneys = 1,
            };
        }

        private static MansionLook Prairie(HouseDraw r)
        {
            bool brick = r.Chance(.5f);
            string brickWall = Dressed("wall", "bricks-085", null);
            return new MansionLook
            {
                Style = HouseVariant.MansionPrairie,
                Wall = brick ? brickWall : Dressed("wall", "plaster-001", r.Pick(["d3c6a0", "cfc4b0", "c9bfa8"])),
                Ground = brickWall,
                End = brickWall,
                Roof = r.Pick([Dressed("roof", "roofing-tiles-008", null), Dressed("roof", null, "5a4a3a"), Dressed("roof", null, "4d463f")]),
                Trim = Dressed("trim", null, "c9bfa8"),
                Frame = Dressed("frame", null, r.Pick(["5a3a28", "2f3a2a"])),
                Door = Dressed("door", null, "5a3a28"),
                Stack = Dressed("wall-end", "bricks-085", null),
                Main = MansionRoof.Hip,
                Arms = MansionRoof.Hip,
                Pitch = r.Range(14f, 18f),
                ArmPitch = r.Range(14f, 18f),
                Eaves = 1.8f,
                Bay = 4.6f,
                WindowWidth = 3.6f,
                WindowHeight = 1.5f,
                Bands = true,
                Chimneys = 1,
            };
        }

        private static MansionLook Regency(HouseDraw r)
        {
            string wall = r.Chance(.6f) ? Dressed("wall", "bricks-088", "d8d6d0") : Dressed("wall", "painted-plaster-wall", "d8d6d0");
            MansionRoof roof = r.Chance(.55f) ? MansionRoof.Mansard : MansionRoof.Hip;
            return new MansionLook
            {
                Style = HouseVariant.MansionRegency,
                Wall = wall,
                End = wall,
                Roof = Dressed("roof", null, r.Pick(["2b2e31", "353e44"])),
                Trim = Dressed("trim", null, "d8d6d0"),
                Frame = Dressed("frame", null, "1e2123"),
                Door = Dressed("door", null, r.Pick(["1e2123", "2f4a3c", "2f4a5c"])),
                Stack = Retag(wall, "wall-end"),
                Main = roof,
                Arms = roof == MansionRoof.Mansard ? MansionRoof.Mansard : MansionRoof.Hip,
                Pitch = r.Range(22f, 26f),
                ArmPitch = r.Range(22f, 26f),
                Bay = 3.4f,
                WindowWidth = 1.3f,
                WindowHeight = 2.4f,
                Cornice = true,
                Portico = true,
                TallPortico = true,
                Chimneys = 2,
            };
        }

        private static MansionLook MidCentury(HouseDraw r)
        {
            MansionRoof roof = r.Chance(.5f) ? MansionRoof.Gable : MansionRoof.Flat;
            float pitch = r.Range(10f, 14f);
            return new MansionLook
            {
                Style = HouseVariant.MansionMidCentury,
                Wall = r.Pick([Dressed("wall", "wood-siding-005", null), Dressed("wall", "hinoki-planks", null), Dressed("wall", "stained-pine", null)]),
                Accent = r.Pick([Dressed("wall-end", "stone-wall", null), Dressed("wall-end", "rock-wall-16", null)]),
                Roof = Dressed("roof", null, r.Pick(["4a4f53", "34383b"])),
                Trim = Dressed("trim", null, r.Pick(["d8d6d0", "2b2e31"])),
                Frame = Dressed("frame", null, r.Pick(["2b2e31", "6e7377"])),
                Door = Dressed("door", null, r.Pick(MidCenturyDoors)),
                Stack = Dressed("wall-end", "stone-wall", null),
                Main = roof,
                Arms = roof,
                Pitch = pitch,
                ArmPitch = pitch,
                Eaves = 1.4f,
                Bay = 3f,
                WindowWidth = 2.4f,
                WindowHeight = 2.6f,
                Sill = .2f,
                Chimneys = 1,
            };
        }

        private static MansionLook Farmhouse(HouseDraw r)
        {
            return new MansionLook
            {
                Style = HouseVariant.MansionFarmhouse,
                Wall = Dressed("wall", "white-planks-clean", r.Pick(["d8d6d0", "d8d6d0", "d6d2c8", "3a3e41"])),
                End = Dressed("wall", "white-planks-clean", "d8d6d0"),
                Roof = Dressed("roof", "standing-seam", "34383b"),
                Trim = Dressed("trim", null, "2b2e31"),
                Frame = Dressed("frame", null, "2b2e31"),
                Door = Dressed("door", null, r.Pick(ContemporaryDoors)),
                Stack = Dressed("wall-end", "stone-wall", null),
                Main = MansionRoof.Gable,
                Arms = MansionRoof.Gable,
                Pitch = r.Range(40f, 45f),
                ArmPitch = r.Range(42f, 48f),
                Bay = 3f,
                WindowWidth = 1.3f,
                WindowHeight = 2.2f,
                CrossGables = r.Pick(2),
                Chimneys = 1,
            };
        }

        private static MansionLook NeoEclectic(HouseDraw r)
        {
            string wall = Dressed("wall", "plaster-001", r.Pick(["d3c6a0", "cfc4b0", "d6cfbf", "c9bfa8"]));
            return new MansionLook
            {
                Style = HouseVariant.MansionNeoEclectic,
                Wall = wall,
                End = wall,
                Accent = r.Pick([Dressed("wall-end", "stone-wall", null), Dressed("wall-end", "rock-wall-16", null)]),
                Roof = Dressed("roof", null, r.Pick(["4d463f", "3d4a52", "5a4a3a"])),
                Trim = Dressed("trim", null, r.Pick(["d8d6d0", "d6cfbf"])),
                Frame = Dressed("frame", null, "d6d2c8"),
                Door = Dressed("door", null, r.Pick(["5a3a28", "3a2f28"])),
                Stack = Dressed("wall-end", "stone-wall", null),
                Main = MansionRoof.Hip,
                Arms = MansionRoof.Hip,
                Pitch = r.Range(36f, 40f),
                ArmPitch = r.Range(36f, 40f),
                Bay = 3.2f,
                WindowWidth = 1.3f,
                WindowHeight = 2f,
                CrossGables = 2,
                Chimneys = 1,
            };
        }

        private static MansionLook California(HouseDraw r)
        {
            return new MansionLook
            {
                Style = HouseVariant.MansionCalifornia,
                Wall = r.Pick([Dressed("wall", "hinoki-planks", null), Dressed("wall", "stained-pine", null)]),
                End = Dressed("wall", "plaster-001", "d8d6d0"),
                Accent = Dressed("wall-end", "stone-wall", null),
                Roof = Dressed("roof", "standing-seam", r.Pick(["4a4f53", "34383b"])),
                Trim = Dressed("trim", null, r.Pick(["2b2e31", "d8d6d0"])),
                Frame = Dressed("frame", null, "2b2e31"),
                Door = Dressed("door", null, r.Pick(ContemporaryDoors)),
                Stack = Dressed("wall-end", "stone-wall", null),
                Main = r.Chance(.5f) ? MansionRoof.Shed : MansionRoof.Flat,
                Arms = MansionRoof.Shed,
                Pitch = r.Range(8f, 12f),
                ArmPitch = r.Range(8f, 12f),
                Eaves = 1.6f,
                Bay = 3f,
                WindowWidth = 2.4f,
                WindowHeight = 2.6f,
                Sill = .2f,
            };
        }
    }

    /// <summary>A dressed part name with its texture and paint moved onto another part.</summary>
    private static string Retag(string dressed, string part)
    {
        int at = dressed.IndexOfAny(['@', '#']);
        return at < 0 ? part : part + dressed[at..];
    }

    /// <summary>A mass's own frame: the body frame turned about a pivot.</summary>
    private readonly record struct MassFrame(float Px, float Py, float Cos, float Sin)
    {
        public static MassFrame Of(MansionMass mass)
        {
            (float sin, float cos) = MathF.SinCos(mass.Turn);
            return new MassFrame(mass.PivotX, mass.PivotY, cos, sin);
        }

        public Vector3 P(float x, float y, float z)
        {
            float dx = x - Px, dy = y - Py;
            return new Vector3(Px + (dx * Cos) - (dy * Sin), Py + (dx * Sin) + (dy * Cos), z);
        }

        public Vector3 D(float x, float y, float z) => new((x * Cos) - (y * Sin), (x * Sin) + (y * Cos), z);
    }

    private static FamilyBodyMesh BuildMansion(FamilyBody body, HouseVariant style, float frontage, float depth, int storeys, int seed,
        MansionGrounds grounds)
    {
        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body);
        MansionPlan plan = PlanMansion(style, frontage, depth, storeys, seed, grounds);
        MansionLook s = MansionLook.Draw(style, seed);
        var r = new HouseDraw(((ulong)seed << 8) | 0x4F);
        int flip = 0;
        foreach (MansionMass mass in plan.Masses)
        {
            if (mass.Part == MansionPart.Arcade)
            {
                Arcade(w, s, mass);
                continue;
            }

            if (mass.Part == MansionPart.Hub && style == HouseVariant.MansionQueenAnne)
            {
                float radius = (mass.X1 - mass.X0) / 2f;
                Turret(w, s, (mass.X0 + mass.X1) / 2f, (mass.Y0 + mass.Y1) / 2f, radius, (mass.Storeys * Storey) + 1.5f, radius * 1.6f);
                continue;
            }

            int levels = mass.Part == MansionPart.Hub && style == HouseVariant.MansionSpanish ? mass.Storeys + 1 : mass.Storeys;
            MassWalls(w, s, plan, mass with { Storeys = levels }, grounds, flip);
            MassRoof(w, s, mass with { Storeys = levels }, flip);
            flip++;
        }

        MansionMass main = plan.Masses[0];
        Features(w, s, plan, main, r, frontage, depth);
        return mesh;
    }

    /// <summary>The walls of one mass, with the windows, doors and garage doors its place in the plan asks for.</summary>
    private static void MassWalls(Writer w, MansionLook s, MansionPlan plan, MansionMass m, MansionGrounds grounds, int flip)
    {
        MassFrame f = MassFrame.Of(m);
        float wide = m.X1 - m.X0, deep = m.Y1 - m.Y0, height = m.Storeys * Storey;
        bool california = s.Style == HouseVariant.MansionCalifornia;
        string wall = california ? (flip % 3) switch { 0 => s.Wall, 1 => s.End, _ => s.Accent } : s.Wall;
        string? ground = california ? null : s.Ground;
        var front = new Face(f.P(m.X0, m.Y0, 0f), f.D(1f, 0f, 0f), f.D(0f, -1f, 0f));
        var back = new Face(f.P(m.X1, m.Y1, 0f), f.D(-1f, 0f, 0f), f.D(0f, 1f, 0f));
        var left = new Face(f.P(m.X0, m.Y1, 0f), f.D(0f, -1f, 0f), f.D(-1f, 0f, 0f));
        var right = new Face(f.P(m.X1, m.Y0, 0f), f.D(0f, 1f, 0f), f.D(1f, 0f, 0f));

        bool main = m.Part == MansionPart.Main;
        bool single = grounds == MansionGrounds.OneHousehold;
        List<Opening> frontOpenings = Grid(s, wide, m.Storeys, main ? plan.Entry - m.X0 : null);
        int? garage = m.Part == MansionPart.Garage ? GarageApron(plan)?.Facing : null;
        if (garage == 0)
        {
            frontOpenings = [.. MansionGarageDoors(wide, wide >= 8.4f ? 3 : 2), .. frontOpenings.Where(o => o.V >= Storey)];
        }
        else if (m.Part is MansionPart.Arm && s.Style == HouseVariant.MansionFarmhouse && wide >= 5f)
        {
            frontOpenings = [new Opening((wide / 2f) - 1.6f, s.Sill, 3.2f, height - 1.4f, OpeningKind.Window)];
        }

        List<Opening> backOpenings = Grid(s, wide, m.Storeys, main ? wide * .3f : null);
        List<Opening> leftOpenings = Grid(s, deep, m.Storeys, null);
        List<Opening> rightOpenings = Grid(s, deep, m.Storeys, null);
        (float least, float most) = Extent(plan);
        bool leftOuter = m.Turn != 0f ? m.X0 < m.PivotX - 1f : m.X0 <= least + .01f;
        bool rightOuter = m.Turn != 0f ? m.X1 > m.PivotX + 1f : m.X1 >= most - .01f;
        if (garage is { } facing && facing != 0)
        {
            float length = MathF.Min(deep - 1f, 10.4f);
            List<Opening> doors = [.. MansionGarageDoors(length, length >= 8.4f ? 3 : 2).Select(o => o with { U = o.U + ((deep - length) / 2f) })];
            if (facing > 0) rightOpenings = [.. doors, .. rightOpenings.Where(o => o.V >= Storey)];
            else leftOpenings = [.. doors, .. leftOpenings.Where(o => o.V >= Storey)];
        }
        else if (!single && m.Part is MansionPart.Main or MansionPart.Arm or MansionPart.Wing or MansionPart.Flank
            && deep >= (m.Part == MansionPart.Flank ? 4f : 6f))
        {
            Opening side = new(1.6f, DoorSill, 1.1f, DoorHeight, OpeningKind.Door);
            if (leftOuter) leftOpenings = [.. leftOpenings.Where(o => o.V > Storey || o.U + o.W < deep - 3.2f), new Opening(deep - 2.7f, DoorSill, 1.1f, DoorHeight, OpeningKind.Door)];
            if (rightOuter) rightOpenings = [.. rightOpenings.Where(o => o.V > Storey || o.U > 3.2f), side];
        }

        string sides = s.Style == HouseVariant.MansionMidCentury && m.Part != MansionPart.Main ? s.Accent : wall;
        MansionElevation(w, s, front, wide, height, frontOpenings, wall, ground);
        MansionElevation(w, s, back, wide, height, backOpenings, wall, ground);
        MansionElevation(w, s, left, deep, height, leftOpenings, sides, ground);
        MansionElevation(w, s, right, deep, height, rightOpenings, sides, ground);
        w.Beam(f.P((m.X0 + m.X1) / 2f, (m.Y0 + m.Y1) / 2f, 0f), f.P((m.X0 + m.X1) / 2f, (m.Y0 + m.Y1) / 2f, PlinthHeight),
            f.D((wide / 2f) + .05f, 0f, 0f), f.D(0f, (deep / 2f) + .05f, 0f), HouseLook.Plinth);

        if (s.Shutters) MansionShutters(w, s, front, frontOpenings);
        if (s.HalfTimber && m.Storeys > 1) HalfTimber(w, s, front, wide, height, frontOpenings);
        if (s.Bands || s.Cornice)
        {
            foreach (Face face in (ReadOnlySpan<Face>)[front, back, left, right])
            {
                float length = face.Along == front.Along || face.Along == back.Along ? wide : deep;
                if (s.Cornice) w.Slab(face, -.2f, length + .2f, height - .35f, height, -.22f, 0f, s.Trim);
                if (!s.Bands) continue;
                for (int k = 1; k < m.Storeys; k++) w.Slab(face, -.08f, length + .08f, (k * Storey) - .15f, (k * Storey) + .12f, -.1f, 0f, s.Trim);
                w.Slab(face, -.08f, length + .08f, s.Sill - .12f, s.Sill, -.08f, 0f, s.Trim);
            }
        }
    }

    /// <summary>
    /// Where a single household's garage doors open: the middle of the face they stand in, and the way it
    /// looks, 0 for the Street and -1 or 1 for the west or east. A garage whose street face stands clear
    /// of the main block opens to the Street, and any other to its side.
    /// </summary>
    public static (float X, float Y, int Facing)? GarageApron(MansionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        MansionMass main = plan.Masses[0];
        foreach (MansionMass m in plan.Masses)
        {
            if (m.Part != MansionPart.Garage) continue;
            if (m.Turn != 0f)
            {
                MassFrame f = MassFrame.Of(m);
                Vector3 end = f.P(plan.GarageSide > 0 ? m.X1 : m.X0, (m.Y0 + m.Y1) / 2f, 0f);
                return (end.X, end.Y, plan.GarageSide);
            }

            bool clear = m.Y0 < main.Y0 - .5f || m.X1 <= main.X0 + .01f || m.X0 >= main.X1 - .01f;
            return clear ? ((m.X0 + m.X1) / 2f, m.Y0, 0) : (plan.GarageSide > 0 ? m.X1 : m.X0, (m.Y0 + m.Y1) / 2f, plan.GarageSide);
        }

        return null;
    }

    /// <summary>The least and most x of the plan's upright main block, arms and hub.</summary>
    private static (float Least, float Most) Extent(MansionPlan plan)
    {
        float least = float.MaxValue, most = float.MinValue;
        foreach (MansionMass m in plan.Masses)
        {
            if (m.Turn != 0f || m.Part is not (MansionPart.Main or MansionPart.Arm or MansionPart.Hub or MansionPart.Flank)) continue;
            least = MathF.Min(least, m.X0);
            most = MathF.Max(most, m.X1);
        }

        return (least, most);
    }

    private static List<Opening> Grid(MansionLook s, float width, int storeys, float? door)
    {
        var openings = new List<Opening>();
        if (width < s.WindowWidth + .8f) return openings;
        int bays = Math.Max(1, (int)(width / s.Bay));
        float bay = width / bays;
        float ww = MathF.Min(s.WindowWidth, bay - .6f);
        for (int k = 0; k < storeys; k++)
        {
            for (int i = 0; i < bays; i++)
            {
                float u = (i + .5f) * bay;
                if (k == 0 && door is { } d && MathF.Abs(u - d) < (bay * .5f) + .9f) continue;
                float v = (k * Storey) + (k == 0 ? s.Sill : MathF.Min(s.Sill, .75f));
                float h = MathF.Min(s.WindowHeight, Storey - (v - (k * Storey)) - .35f);
                openings.Add(new Opening(u - (ww / 2f), v, ww, h, OpeningKind.Window));
            }
        }

        if (door is { } at)
        {
            float dw = s.Style is HouseVariant.MansionRegency or HouseVariant.MansionChateau or HouseVariant.MansionNeoEclectic ? 1.8f : 1.2f;
            float u = Math.Clamp(at - (dw / 2f), .3f, width - dw - .3f);
            openings.Add(new Opening(u, DoorSill, dw, MathF.Min(2.6f, Storey - .6f), OpeningKind.Door));
        }

        return openings;
    }

    private static List<Opening> MansionGarageDoors(float length, int count)
    {
        float door = MathF.Min(2.8f, (length - .6f) / count - .5f);
        float pitch = (length - .6f) / count;
        return [.. Enumerable.Range(0, count).Select(i => new Opening(.3f + (i * pitch) + ((pitch - door) / 2f), 0f, door, 2.4f, OpeningKind.Roller))];
    }

    /// <summary>A wall with its openings, its ground storey in its own material where the style has one.</summary>
    private static void MansionElevation(Writer w, MansionLook s, Face face, float width, float height, List<Opening> openings, string wall, string? ground)
    {
        if (ground is { } lower && lower != wall && height > Storey)
        {
            w.Facade(face, width, Storey, openings.FindAll(o => o.V < Storey), lower, s.Trim, s.Door, s.Frame);
            var upper = new Face(face.Point(0f, Storey), face.Along, face.Outward);
            w.Facade(upper, width, height - Storey, [.. openings.Where(o => o.V >= Storey).Select(o => o with { V = o.V - Storey })],
                wall, s.Trim, s.Door, s.Frame);
        }
        else
        {
            w.Facade(face, width, height, openings, wall, s.Trim, s.Door, s.Frame);
        }
    }

    private static void MansionShutters(Writer w, MansionLook s, Face face, List<Opening> openings)
    {
        foreach (Opening o in openings)
        {
            if (o.Kind != OpeningKind.Window || o.W > 1.6f) continue;
            float leaf = o.W * .5f;
            w.Slab(face, o.U - leaf - .05f, o.U - .05f, o.V, o.V + o.H, -.06f, 0f, s.Shutter);
            w.Slab(face, o.U + o.W + .05f, o.U + o.W + leaf + .05f, o.V, o.V + o.H, -.06f, 0f, s.Shutter);
        }
    }

    /// <summary>Dark battens over the upper storeys, kept clear of the windows.</summary>
    private static void HalfTimber(Writer w, MansionLook s, Face face, float width, float height, List<Opening> openings)
    {
        w.Slab(face, 0f, width, Storey - .1f, Storey + .12f, -.06f, 0f, s.Trim);
        w.Slab(face, 0f, width, height - .22f, height, -.06f, 0f, s.Trim);
        for (float u = .5f; u < width - .3f; u += 1.1f)
        {
            if (openings.Exists(o => o.V >= Storey && u + .1f > o.U - .05f && u - .1f < o.U + o.W + .05f)) continue;
            w.Slab(face, u - .09f, u + .09f, Storey + .12f, height - .22f, -.05f, 0f, s.Trim);
        }
    }

    private static void MassRoof(Writer w, MansionLook s, MansionMass m, int flip)
    {
        MassFrame f = MassFrame.Of(m);
        float height = m.Storeys * Storey;
        bool main = m.Part == MansionPart.Main;
        MansionRoof kind = main ? s.Main : s.Arms;
        float pitch = main ? s.Pitch : s.ArmPitch;
        float wide = m.X1 - m.X0, deep = m.Y1 - m.Y0;
        float cx = (m.X0 + m.X1) / 2f, cy = (m.Y0 + m.Y1) / 2f;
        bool alongY = m.Part switch
        {
            MansionPart.Main => deep > wide,
            _ when m.Turn != 0f => false,
            MansionPart.Hub or MansionPart.Breezeway => false,
            _ => deep >= wide * .8f,
        };
        if (m.Part == MansionPart.Garage && m.Storeys == 1 && kind is MansionRoof.Mansard) kind = MansionRoof.Hip;
        if (m.Part == MansionPart.Hub) kind = s.Style == HouseVariant.MansionCalifornia ? MansionRoof.Flat : MansionRoof.Hip;
        if (m.Part is MansionPart.Flank or MansionPart.Breezeway && kind == MansionRoof.Shed) kind = MansionRoof.Flat;
        float A = alongY ? wide / 2f : deep / 2f, L = alongY ? deep / 2f : wide / 2f;
        string end = s.End.Length > 0 ? s.End : s.Wall;
        switch (kind)
        {
            case MansionRoof.Gable:
                FramedHipGable(w, s.Roof, end, f, alongY, cx, cy, A, L, height, pitch, 0f, s.Eaves);
                break;
            case MansionRoof.Hip:
                FramedHipGable(w, s.Roof, end, f, alongY, cx, cy, A, L, height, m.Part == MansionPart.Hub ? pitch + 4f : pitch, 1f, s.Eaves);
                break;
            case MansionRoof.Mansard:
                Mansard(w, s, f, m.X0, m.X1, m.Y0, m.Y1, height);
                break;
            case MansionRoof.Flat:
                Overhang(w, s, f, m.X0, m.X1, m.Y0, m.Y1, height);
                break;
            case MansionRoof.Shed:
                Shed(w, s, f, m.X0, m.X1, m.Y0, m.Y1, height, pitch, flip % 2 == 0);
                break;
        }
    }

    /// <summary>The hip-gable roof of <see cref="HipGable(Writer, string, string, bool, float, float, float, float, float, float, float, bool, bool, float)"/>, drawn in a turned frame.</summary>
    private static void FramedHipGable(Writer w, string roof, string wall, MassFrame f, bool alongY, float cx, float cy, float A, float L, float H,
        float pitch, float k, float eaves)
    {
        Vector3 P(float a, float l, float z) => alongY ? f.P(cx + a, cy + l, z) : f.P(cx + l, cy + a, z);
        Vector3 Dir(float a, float l, float z) => alongY ? f.D(a, l, z) : f.D(l, a, z);

        float t = MathF.Tan(pitch * MathF.PI / 180f);
        float top = H + (A * t), low = H - (eaves * t);
        float ae = A + eaves, le = L + eaves;
        float zk = k >= 1f ? low : top - (k * (top - low));
        float ak = k >= 1f ? ae : (top - zk) / t;
        float ry = MathF.Max(0f, le - ak);

        foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
        {
            Sheet(w, roof, [P(side * ae, -le, low), P(side * ak, -le, zk), P(0f, -ry, top), P(0f, ry, top), P(side * ak, le, zk), P(side * ae, le, low)],
                Dir(side, 0f, 1f));
        }

        foreach (float end in (ReadOnlySpan<float>)[-1f, 1f])
        {
            if (ak > 1e-3f) Sheet(w, roof, [P(-ak, end * le, zk), P(ak, end * le, zk), P(0f, end * ry, top)], Dir(0f, end, 1f));
            if (zk <= H + 1e-3f) continue;
            float aw = MathF.Min(A, (top - zk) / t);
            Shape(w, wall, [P(-A, end * L, H), P(A, end * L, H), P(aw, end * L, zk), P(-aw, end * L, zk)], Dir(0f, end, 0f));
        }
    }

    /// <summary>Steep lower slopes on every side under a low hip, the top storey inside them.</summary>
    private static void Mansard(Writer w, MansionLook s, MassFrame f, float x0, float x1, float y0, float y1, float H)
    {
        const float Rise = 2.8f, Inset = 1.05f, Kick = .25f;
        Vector3[] outer = [f.P(x0 - Kick, y0 - Kick, H), f.P(x1 + Kick, y0 - Kick, H), f.P(x1 + Kick, y1 + Kick, H), f.P(x0 - Kick, y1 + Kick, H)];
        Vector3[] inner = [f.P(x0 + Inset, y0 + Inset, H + Rise), f.P(x1 - Inset, y0 + Inset, H + Rise), f.P(x1 - Inset, y1 - Inset, H + Rise),
            f.P(x0 + Inset, y1 - Inset, H + Rise)];
        Vector3 middle = f.P((x0 + x1) / 2f, (y0 + y1) / 2f, H);
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            Vector3 mid = (outer[i] + outer[j]) / 2f;
            Sheet(w, s.Roof, [outer[i], outer[j], inner[j], inner[i]], (mid - middle) with { Z = 1f });
        }

        bool alongY = y1 - y0 > x1 - x0;
        float A = (alongY ? x1 - x0 : y1 - y0) / 2f - Inset, L = (alongY ? y1 - y0 : x1 - x0) / 2f - Inset;
        if (A > .5f) FramedHipGable(w, s.Roof, s.Wall, f, alongY, (x0 + x1) / 2f, (y0 + y1) / 2f, A, L, H + Rise, 12f, 1f, 0f);

        int bays = Math.Max(1, (int)((x1 - x0) / s.Bay));
        for (int i = 0; i < bays; i++)
        {
            float u = x0 + ((i + .5f) * (x1 - x0) / bays);
            Dormer(w, s, f, u, y0 - Kick + .35f, H + .3f, .75f, 1.6f, 60f);
        }
    }

    private static void Overhang(Writer w, MansionLook s, MassFrame f, float x0, float x1, float y0, float y1, float H)
    {
        float e = s.Eaves;
        w.Beam(f.P((x0 + x1) / 2f, (y0 + y1) / 2f, H), f.P((x0 + x1) / 2f, (y0 + y1) / 2f, H + .4f),
            f.D(((x1 - x0) / 2f) + e, 0f, 0f), f.D(0f, ((y1 - y0) / 2f) + e, 0f), s.Trim);
        w.Beam(f.P((x0 + x1) / 2f, (y0 + y1) / 2f, H + .4f), f.P((x0 + x1) / 2f, (y0 + y1) / 2f, H + .45f),
            f.D(((x1 - x0) / 2f) + e - .15f, 0f, 0f), f.D(0f, ((y1 - y0) / 2f) + e - .15f, 0f), Dressed("membrane", "bitumen", null));
    }

    /// <summary>A mono-pitch roof rising across the mass, its high side a band of clerestory glass.</summary>
    private static void Shed(Writer w, MansionLook s, MassFrame f, float x0, float x1, float y0, float y1, float H, float pitch, bool risesBack)
    {
        float t = MathF.Tan(pitch * MathF.PI / 180f), e = s.Eaves;
        float rise = (y1 - y0) * t;
        float lowY = risesBack ? y0 : y1, highY = risesBack ? y1 : y0, dir = risesBack ? 1f : -1f;
        float zLow = H - (e * t), zHigh = H + rise + (e * t);
        Vector3 a = f.P(x0 - e, lowY - (dir * e), zLow), b = f.P(x1 + e, lowY - (dir * e), zLow);
        Vector3 c = f.P(x1 + e, highY + (dir * e), zHigh), d = f.P(x0 - e, highY + (dir * e), zHigh);
        Sheet(w, s.Roof, [a, b, c, d], f.D(0f, -dir, 1f));
        foreach (float x in (ReadOnlySpan<float>)[x0, x1])
        {
            Shape(w, s.Wall, [f.P(x, y0, H), f.P(x, y1, H), f.P(x, highY == y1 ? y1 : y0, H + rise)], f.D(x == x0 ? -1f : 1f, 0f, 0f));
        }

        Vector3 g0 = f.P(x0 + .2f, highY, H + .1f), g1 = f.P(x1 - .2f, highY, H + .1f);
        Shape(w, "glass", [g0, g1, g1 with { Z = H + rise - .15f }, g0 with { Z = H + rise - .15f }], f.D(0f, dir, 0f));
    }

    /// <summary>A gabled dormer whose face stands at <paramref name="y"/>, its sill at <paramref name="z"/>.</summary>
    private static void Dormer(Writer w, MansionLook s, MassFrame f, float x, float y, float z, float half, float tall, float pitch)
    {
        const float Deep = 2.4f;
        Vector3 c0 = f.P(x, y + (Deep / 2f), z), c1 = f.P(x, y + (Deep / 2f), z + tall);
        w.Beam(c0, c1, f.D(half, 0f, 0f), f.D(0f, Deep / 2f, 0f), s.End.Length > 0 ? s.End : s.Wall);
        w.Beam(f.P(x, y - .03f, z + .2f), f.P(x, y - .03f, z + tall - .15f), f.D(half - .18f, 0f, 0f), f.D(0f, .03f, 0f), "glass");
        FramedHipGable(w, s.Roof, s.End.Length > 0 ? s.End : s.Wall, f, alongY: true, x, y + (Deep / 2f), half, Deep / 2f, z + tall, pitch, 0f, .15f);
    }

    /// <summary>A round tower of <paramref name="sides"/> faces under a cone, windows on its street half.</summary>
    private static void Turret(Writer w, MansionLook s, float cx, float cy, float radius, float height, float rise, int sides = 12)
    {
        Span<Vector3> ring = stackalloc Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            (float sn, float cs) = MathF.SinCos(2f * MathF.PI * i / sides);
            ring[i] = new Vector3(cx + (radius * cs), cy + (radius * sn), 0f);
        }

        var centre = new Vector3(cx, cy, 0f);
        for (int i = 0; i < sides; i++)
        {
            Vector3 a = ring[i], b = ring[(i + 1) % sides];
            Vector3 outward = ((a + b) / 2f) - centre;
            Shape(w, s.Wall, [a, b, b with { Z = height }, a with { Z = height }], outward);
            Shape(w, s.Trim, [a with { Z = height - .3f }, b with { Z = height - .3f }, b with { Z = height }, a with { Z = height }], outward);
            if (outward.Y < -.2f * radius)
            {
                Vector3 n = Vector3.Normalize(outward) * .04f;
                for (float z = .9f; z + 1.8f < height - .3f; z += Storey)
                {
                    Vector3 p = Vector3.Lerp(a, b, .25f) + n, q = Vector3.Lerp(a, b, .75f) + n;
                    Shape(w, "glass", [p with { Z = z }, q with { Z = z }, q with { Z = z + 1.8f }, p with { Z = z + 1.8f }], outward);
                }
            }

            float e = radius + .35f;
            (float s0, float c0) = MathF.SinCos(2f * MathF.PI * i / sides);
            (float s1, float c1) = MathF.SinCos(2f * MathF.PI * (i + 1) / sides);
            Vector3 low0 = new(cx + (e * c0), cy + (e * s0), height - .2f), low1 = new(cx + (e * c1), cy + (e * s1), height - .2f);
            Sheet(w, s.Roof, [low0, low1, new Vector3(cx, cy, height + rise)], outward with { Z = radius });
        }
    }

    private static void Column(Writer w, string part, float cx, float cy, float radius, float z0, float z1, int sides = 8)
    {
        Span<Vector3> ring = stackalloc Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            (float sn, float cs) = MathF.SinCos((2f * MathF.PI * i / sides) + (MathF.PI / sides));
            ring[i] = new Vector3(cx + (radius * cs), cy + (radius * sn), z0);
        }

        var centre = new Vector3(cx, cy, z0);
        for (int i = 0; i < sides; i++)
        {
            Vector3 a = ring[i], b = ring[(i + 1) % sides];
            Shape(w, part, [a, b, b with { Z = z1 }, a with { Z = z1 }], ((a + b) / 2f) - centre);
        }
    }

    private static void Box(Writer w, string part, float x0, float x1, float y0, float y1, float z0, float z1) =>
        w.Box(new Vector3(MathF.Min(x0, x1), MathF.Min(y0, y1), z0), new Vector3(MathF.Max(x0, x1), MathF.Max(y0, y1), z1), part);

    /// <summary>A one-storey arcade: piers under a lean-to tile roof, closing a court to the Street.</summary>
    private static void Arcade(Writer w, MansionLook s, MansionMass m)
    {
        float wide = m.X1 - m.X0;
        int bays = Math.Max(2, (int)(wide / 3.2f));
        for (int i = 0; i <= bays; i++)
        {
            float x = m.X0 + (i * wide / bays);
            Box(w, s.Wall, x - .3f, x + .3f, m.Y0, m.Y0 + .6f, 0f, Storey - .3f);
        }

        Box(w, s.Wall, m.X0, m.X1, m.Y0, m.Y0 + .6f, Storey - 1f, Storey - .3f);
        Sheet(w, s.Roof, [new(m.X0, m.Y0 - .4f, Storey - .4f), new(m.X1, m.Y0 - .4f, Storey - .4f), new(m.X1, m.Y1, Storey + .6f), new(m.X0, m.Y1, Storey + .6f)],
            new Vector3(0f, -1f, 2f));
    }

    /// <summary>The style's own pieces on the main block: porticos, porches, turrets, towers, gables, chimneys and dormers.</summary>
    private static void Features(Writer w, MansionLook s, MansionPlan plan, MansionMass main, HouseDraw r, float frontage, float depth)
    {
        float H = main.Storeys * Storey;
        float y0 = main.Y0;
        float entry = plan.Entry;
        float stoop = main.Y0 - (-depth / 2f);
        (float v0, float v1) = VisibleFront(plan);
        float visible = v1 - v0;
        bool alongY = main.Y1 - main.Y0 > main.X1 - main.X0;
        float A = alongY ? (main.X1 - main.X0) / 2f : (main.Y1 - main.Y0) / 2f;
        float ridge = H + (A * MathF.Tan(s.Pitch * MathF.PI / 180f));
        var identity = new MassFrame(0f, 0f, 1f, 0f);

        switch (s.Style)
        {
            case HouseVariant.MansionColonial or HouseVariant.MansionRegency:
                {
                    float half = Math.Clamp(visible * .17f, 2.2f, 4.4f);
                    float tall = s.TallPortico ? H : Storey;
                    float out_ = MathF.Min(2.6f, stoop - .2f);
                    int columns = s.Style == HouseVariant.MansionRegency ? 6 : 4;
                    for (int i = 0; i < columns; i++)
                    {
                        float x = entry - half + .3f + (i * ((2f * half) - .6f) / (columns - 1));
                        Column(w, s.Trim, x, y0 - out_ + .35f, s.Style == HouseVariant.MansionRegency ? .17f : .26f, PlinthHeight, tall - .5f);
                    }

                    Box(w, s.Trim, entry - half - .2f, entry + half + .2f, y0 - out_, y0, tall - .5f, tall);
                    Box(w, HouseLook.Plinth, entry - half - .4f, entry + half + .4f, y0 - out_ - .3f, y0, 0f, PlinthHeight);
                    if (s.Style == HouseVariant.MansionColonial)
                    {
                        FramedHipGable(w, s.Roof, s.Trim, identity, alongY: true, entry, y0 - (out_ / 2f) + .3f, half + .1f, (out_ / 2f) + .3f, tall, 26f, 0f, .2f);
                    }
                    else
                    {
                        Box(w, s.Trim, entry - half - .2f, entry + half + .2f, y0 - out_, y0, tall, tall + .6f);
                    }

                    if (s.Dormers && s.Gabled)
                    {
                        float t = MathF.Tan(s.Pitch * MathF.PI / 180f);
                        int count = Math.Max(2, (int)(visible / 7f));
                        for (int i = 0; i < count; i++)
                        {
                            float x = v0 + ((i + .5f) * visible / count);
                            if (MathF.Abs(x - entry) < half + 1f) continue;
                            Dormer(w, s, identity, x, y0 + 1.1f, H + (1.1f * t) - .3f, .8f, 1.7f, 45f);
                        }
                    }

                    break;
                }

            case HouseVariant.MansionTudor:
                {
                    CrossGables(w, s, plan, main, v0, v1, entry, r, s.CrossGables, MansionRoof.Gable, 1.4f);
                    float x = r.Chance(.5f) ? main.X0 + 2.5f : main.X1 - 2.5f;
                    Box(w, s.Stack, x - .9f, x + .9f, y0 - .6f, y0 + .4f, 0f, ridge + 1.4f);
                    for (int k = 0; k < 2; k++) Box(w, s.Stack, x - .75f + (k * .85f), x - .1f + (k * .85f), y0 - .4f, y0 + .2f, ridge + 1.4f, ridge + 2.1f);
                    break;
                }

            case HouseVariant.MansionQueenAnne:
                {
                    bool hub = plan.Kind == MansionPlanKind.Angled;
                    float side = plan.Masses.Length > 1 && plan.Masses[1].X0 < main.X0 + 1f ? 1f : -1f;
                    float tx = side > 0 ? v1 - 1.6f : v0 + 1.6f;
                    if (!hub) Turret(w, s, tx, y0 - .4f, 2.4f, H + 1.6f, 4.2f);
                    float p0 = hub ? v0 : side > 0 ? v0 : v0 + 3.6f, p1 = hub ? v1 : side > 0 ? v1 - 3.6f : v1;
                    Porch(w, s, p0, p1, y0, MathF.Min(2.6f, stoop - .2f));
                    CrossGables(w, s, plan, main, v0, v1, entry, r, 1, MansionRoof.Gable, 1f);
                    break;
                }

            case HouseVariant.MansionChateau:
                {
                    foreach (MansionMass arm in plan.Masses)
                    {
                        if (arm.Part is not (MansionPart.Arm or MansionPart.Garage) || arm.Y0 > y0 - 1f) continue;
                        float corner = arm.X0 < main.X0 + 1f ? arm.X0 + .4f : arm.X1 - .4f;
                        Turret(w, s, corner, arm.Y0 + .4f, 2f, (arm.Storeys * Storey) + 2f, 6f);
                    }

                    if (plan.Kind is MansionPlanKind.Bar or MansionPlanKind.T or MansionPlanKind.Ell)
                    {
                        Turret(w, s, main.X0 + 1.8f, y0 + .4f, 2.2f, H + 2f, 6f);
                    }

                    float half = 2.6f, out_ = MathF.Min(4.8f, stoop - .2f);
                    foreach (float x in (ReadOnlySpan<float>)[entry - half, entry + half])
                    {
                        foreach (float y in (ReadOnlySpan<float>)[y0 - out_ + .4f, y0 - .4f]) Box(w, s.Stack, x - .35f, x + .35f, y - .35f, y + .35f, 0f, Storey);
                    }

                    Box(w, s.Trim, entry - half - .5f, entry + half + .5f, y0 - out_, y0, Storey, Storey + .5f);
                    Box(w, s.Stack, entry - half - .5f, entry + half + .5f, y0 - out_, y0 - out_ + .3f, Storey + .5f, Storey + 1.2f);
                    int count = Math.Max(2, (int)(visible / 5f));
                    for (int i = 0; i < count; i++)
                    {
                        float x = v0 + ((i + .5f) * visible / count);
                        Dormer(w, s, identity, x, y0 + .05f, H - .4f, .9f, 2.4f, 62f);
                    }

                    break;
                }

            case HouseVariant.MansionSpanish:
                {
                    float tw = 4.6f, out_ = MathF.Min(1f, stoop - .1f);
                    float tx = Math.Clamp(entry - 4f, v0 + (tw / 2f), v1 - (tw / 2f));
                    var tower = new MansionMass(MansionPart.Hub, tx - (tw / 2f), tx + (tw / 2f), y0 - out_, y0 - out_ + tw, main.Storeys + 1);
                    MassWalls(w, s, plan, tower, MansionGrounds.OneHousehold, 0);
                    FramedHipGable(w, s.Roof, s.Wall, identity, alongY: false, tx, y0 - out_ + (tw / 2f), tw / 2f, tw / 2f, (main.Storeys + 1) * Storey, 26f, 1f, .5f);
                    float a0 = MathF.Max(v0, tx + (tw / 2f) + .5f), a1 = MathF.Min(v1, a0 + MathF.Max(8f, visible * .45f));
                    if (plan.Kind != MansionPlanKind.Court && a1 - a0 > 6f)
                    {
                        Arcade(w, s, new MansionMass(MansionPart.Arcade, a0, a1, y0 - MathF.Min(1.4f, stoop), y0, 1));
                    }

                    for (float x = v0 + 2f; x < v1 - 2f; x += 7f)
                    {
                        Box(w, s.Metal, x - 1f, x + 1f, y0 - .7f, y0, Storey + .2f, Storey + .3f);
                        Box(w, s.Metal, x - 1f, x + 1f, y0 - .72f, y0 - .66f, Storey + .3f, Storey + 1.2f);
                    }

                    break;
                }

            case HouseVariant.MansionPrairie:
                {
                    float x = (main.X0 + main.X1) / 2f + (r.Chance(.5f) ? -3f : 3f);
                    Box(w, s.Stack, x - 1.6f, x + 1.6f, (main.Y0 + main.Y1) / 2f - .6f, (main.Y0 + main.Y1) / 2f + .6f, H, ridge + 1f);
                    Box(w, s.Trim, x - 1.9f, x + 1.9f, (main.Y0 + main.Y1) / 2f - .9f, (main.Y0 + main.Y1) / 2f + .9f, ridge + 1f, ridge + 1.25f);
                    float half = 2.4f, out_ = MathF.Min(1.8f, stoop - .2f);
                    Box(w, s.Stack, entry - half - .5f, entry - half + .1f, y0 - out_, y0 - out_ + .6f, 0f, 1f);
                    Box(w, s.Stack, entry + half - .1f, entry + half + .5f, y0 - out_, y0 - out_ + .6f, 0f, 1f);
                    break;
                }

            case HouseVariant.MansionMidCentury or HouseVariant.MansionCalifornia:
                {
                    Box(w, s.Trim, entry - 2.2f, entry + 2.2f, y0 - MathF.Min(1.5f, stoop), y0, Storey - .3f, Storey - .1f);
                    if (s.Style == HouseVariant.MansionMidCentury)
                    {
                        float x = main.X0 + 1.6f;
                        Box(w, s.Accent, main.X0 - .2f, main.X0 + 3.2f, y0 - .2f, main.Y1 + .2f, 0f, H + .6f);
                        Box(w, s.Stack, x - .8f, x + .8f, (main.Y0 + main.Y1) / 2f - .6f, (main.Y0 + main.Y1) / 2f + .6f, H, H + 2.2f);
                    }

                    break;
                }

            case HouseVariant.MansionFarmhouse:
                {
                    Porch(w, s, v0, v1, y0, MathF.Min(2.6f, stoop - .2f));
                    CrossGables(w, s, plan, main, v0, v1, entry, r, s.CrossGables, MansionRoof.Gable, .8f);
                    float x = r.Chance(.5f) ? main.X0 + 1.5f : main.X1 - 1.5f;
                    Box(w, s.Stack, x - .7f, x + .7f, (main.Y0 + main.Y1) / 2f - .5f, (main.Y0 + main.Y1) / 2f + .5f, H, ridge + .9f);
                    break;
                }

            case HouseVariant.MansionNeoEclectic:
                {
                    CrossGables(w, s, plan, main, v0, v1, entry, r, s.CrossGables, MansionRoof.Hip, 1.2f);
                    float half = 2.4f, out_ = MathF.Min(1.4f, stoop - .1f);
                    var portal = new Face(new Vector3(entry - half, y0 - out_, 0f), Vector3.UnitX, -Vector3.UnitY);
                    w.Facade(portal, 2f * half, H + 1.2f,
                        [new Opening(half - .9f, DoorSill, 1.8f, 2.6f, OpeningKind.Door), new Opening(half - .8f, Storey + .3f, 1.6f, H - Storey - .2f, OpeningKind.Window)],
                        s.Accent, s.Trim, s.Door, s.Frame);
                    foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
                    {
                        float x = entry + (side * half);
                        Shape(w, s.Accent, [new(x, y0 - out_, 0f), new(x, y0, 0f), new(x, y0, H + 1.2f), new(x, y0 - out_, H + 1.2f)], new Vector3(side, 0f, 0f));
                    }

                    FramedHipGable(w, s.Roof, s.Accent, identity, alongY: true, entry, y0 - (out_ / 2f) + 1f, half, (out_ / 2f) + 1f, H + 1.2f, 48f, 0f, .3f);
                    break;
                }
        }

        if (s.Chimneys > 0 && s.Style is HouseVariant.MansionColonial or HouseVariant.MansionRegency or HouseVariant.MansionChateau
            or HouseVariant.MansionQueenAnne or HouseVariant.MansionSpanish or HouseVariant.MansionNeoEclectic)
        {
            float cy = (main.Y0 + main.Y1) / 2f;
            float tall = ridge + (s.Style == HouseVariant.MansionChateau ? 2.4f : 1.2f);
            if (s.Main == MansionRoof.Mansard) tall = H + 4f;
            ReadOnlySpan<float> xs = s.Chimneys == 1 ? [main.X1 - ((main.X1 - main.X0) * .3f)]
                : s.Gabled && !alongY ? [main.X0 + .9f, main.X1 - .9f] : [main.X0 + ((main.X1 - main.X0) * .25f), main.X1 - ((main.X1 - main.X0) * .25f)];
            foreach (float x in xs)
            {
                Box(w, s.Stack, x - .7f, x + .7f, cy - .9f, cy + .9f, H - 1f, tall);
                Box(w, s.Trim, x - .8f, x + .8f, cy - 1f, cy + 1f, tall, tall + .2f);
            }
        }
    }

    /// <summary>The part of the main block's street face no arm stands in front of.</summary>
    private static (float Low, float High) VisibleFront(MansionPlan plan)
    {
        MansionMass main = plan.Masses[0];
        float low = main.X0, high = main.X1;
        foreach (MansionMass arm in plan.Masses)
        {
            if (arm.Part is not (MansionPart.Arm or MansionPart.Garage) || arm.Y0 > main.Y0 - .5f) continue;
            if (arm.X0 <= low + .01f) low = MathF.Max(low, arm.X1);
            if (arm.X1 >= high - .01f) high = MathF.Min(high, arm.X0);
        }

        return (low, high);
    }

    /// <summary>Gabled or hipped bays standing forward of the main block, clear of the door.</summary>
    private static void CrossGables(Writer w, MansionLook s, MansionPlan plan, MansionMass main, float v0, float v1, float entry, HouseDraw r,
        int count, MansionRoof roof, float reach)
    {
        float visible = v1 - v0;
        if (count <= 0 || visible < 9f) return;
        float stoop = main.Y0 - plan.Masses.Min(m => m.Y0);
        reach = MathF.Min(reach, MathF.Max(0f, main.Y0 - (plan.Front - MansionStoop[StyleOf(s.Style)]) - .2f));
        if (reach < .4f && stoop < .4f) reach = .4f;
        float width = MathF.Min(r.Range(4.6f, 6f), visible / (count + 1.5f));
        float pitch = s.ArmPitch + (roof == MansionRoof.Gable ? 4f : 0f);
        for (int i = 0; i < count; i++)
        {
            float x = count == 1 ? (entry - v0 > v1 - entry ? v0 + (width / 2f) + .6f : v1 - (width / 2f) - .6f)
                : i == 0 ? v0 + (width / 2f) + .6f : v1 - (width / 2f) - .6f;
            if (MathF.Abs(x - entry) < (width / 2f) + 1f) continue;
            var bay = new MansionMass(MansionPart.Arm, x - (width / 2f), x + (width / 2f), main.Y0 - reach, main.Y0 + 2f, main.Storeys);
            var front = new Face(new Vector3(bay.X0, bay.Y0, 0f), Vector3.UnitX, -Vector3.UnitY);
            List<Opening> openings = Grid(s, width, main.Storeys, null);
            MansionElevation(w, s, front, width, main.Storeys * Storey, openings, s.Wall, s.Ground);
            foreach (float side in (ReadOnlySpan<float>)[-1f, 1f])
            {
                float sx = side < 0 ? bay.X0 : bay.X1;
                Shape(w, s.Wall, [new(sx, bay.Y0, 0f), new(sx, main.Y0, 0f), new(sx, main.Y0, main.Storeys * Storey), new(sx, bay.Y0, main.Storeys * Storey)],
                    new Vector3(side, 0f, 0f));
            }

            if (s.HalfTimber) HalfTimber(w, s, front, width, main.Storeys * Storey, openings);
            FramedHipGable(w, s.Roof, s.End.Length > 0 ? s.End : s.Wall, new MassFrame(0f, 0f, 1f, 0f), alongY: true, x, (bay.Y0 + bay.Y1) / 2f,
                width / 2f, (bay.Y1 - bay.Y0) / 2f, main.Storeys * Storey, pitch, roof == MansionRoof.Hip ? 1f : 0f, s.Eaves);
        }
    }

    /// <summary>A ground-storey porch on posts under a lean-to roof across the main block's front.</summary>
    private static void Porch(Writer w, MansionLook s, float x0, float x1, float y0, float deep)
    {
        if (x1 - x0 < 3f || deep < 1.2f) return;
        float front = y0 - deep;
        Box(w, HouseLook.Plinth, x0, x1, front, y0, 0f, PlinthHeight + .1f);
        int posts = Math.Max(2, (int)((x1 - x0) / 2.6f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float x = x0 + .2f + (i * (x1 - x0 - .4f) / (posts - 1));
            Box(w, s.Trim, x - .1f, x + .1f, front + .1f, front + .3f, PlinthHeight, Storey - .4f);
        }

        Box(w, s.Trim, x0, x1, front, front + .4f, Storey - .55f, Storey - .35f);
        Sheet(w, s.Roof, [new(x0 - .2f, front - .3f, Storey - .4f), new(x1 + .2f, front - .3f, Storey - .4f), new(x1 + .2f, y0, Storey + .35f),
            new(x0 - .2f, y0, Storey + .35f)], new Vector3(0f, -1f, 2f));
    }
}
