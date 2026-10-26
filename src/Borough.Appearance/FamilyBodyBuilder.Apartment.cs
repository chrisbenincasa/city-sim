using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The apartment forms: one Building's whole frontage, varied by roof and plan. They follow
/// <c>scripts/art/apartment-forms.py</c>, and the parapet form is the base body.
/// </summary>
/// <remarks>
/// A side shared with a neighbor is blank, and a pitched roof stops at it behind a party upstand.
/// The walls follow the family's bay rules. The mansard and the set-back top draw about 15% less of
/// their top storey, and the centre gable, bay front and forecourt up to 9% less floor than the
/// Building holds, inside the drawing's 20% (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>How far a centre gable's middle steps forward of its wings.</summary>
    public const float CentreGableMetres = 1.2f;

    /// <summary>How far a forecourt stands back from the Lot line.</summary>
    public const float ForecourtMetres = 3f;

    /// <summary>How far a set-back top storey stands back from the street wall.</summary>
    public const float SetbackMetres = 2.4f;

    private const float ApartmentEaves = .45f;
    private const float ApartmentVerge = .3f;
    private const float ApartmentParapet = .7f;

    /// <summary>A stretch of the street wall from <paramref name="From"/> to <paramref name="To"/>, measured from its left end.</summary>
    private readonly record struct Segment(float From, float To, float SetBack);

    /// <summary>A front gable over the middle of a pitched roof, <paramref name="Half"/> wide either side of its ridge.</summary>
    private readonly record struct CrossGable(float Centre, float Half, float Front);

    /// <summary>The form a Building draws. A Building of one storey has no top storey to set back or roof over.</summary>
    internal static ApartmentForm Drawn(ApartmentBody apartment, int storeys) =>
        storeys < 2 && apartment.Form is ApartmentForm.Mansard or ApartmentForm.Setback ? ApartmentForm.Parapet : apartment.Form;

    private static FamilyBodyMesh BuildApartment(FamilyBody body, ApartmentBody apartment, float frontage, float depth, int storeys,
        AttachedSides attached, TerracePlace place)
    {
        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body);
        float x = frontage / 2f, y = depth / 2f;
        float pitch = place.PitchDegrees > 0 ? place.PitchDegrees : body.GableDegrees;
        int bays = Bays(frontage, body.BayMetres);
        float bay = frontage / bays;
        float height = storeys * Storey;
        Segment[] whole = [new(0f, frontage, 0f)];
        switch (Drawn(apartment, storeys))
        {
            case ApartmentForm.Hipped or ApartmentForm.SideGable:
                ApartmentWalls(w, body, x, y, whole, storeys, attached);
                Pitched(w, x, -y, y, height, pitch, apartment.Form == ApartmentForm.Hipped, attached, null);
                break;
            case ApartmentForm.CentreGable when CentreGableBays(bays, bay, depth) is { } mid:
                float u0 = (bays - mid) / 2f * bay, u1 = u0 + (mid * bay);
                ApartmentWalls(w, body, x, y, [new(0f, u0, CentreGableMetres), new(u0, u1, 0f), new(u1, frontage, CentreGableMetres)], storeys, attached);
                Pitched(w, x, -y + CentreGableMetres, y, height, pitch, hip: true, attached, new CrossGable(-x + ((u0 + u1) / 2f), (u1 - u0) / 2f, -y));
                break;
            case ApartmentForm.CentreGable:
                ApartmentWalls(w, body, x, y, whole, storeys, attached);
                Pitched(w, x, -y, y, height, pitch, hip: true, attached, null);
                break;
            case ApartmentForm.Mansard:
                ApartmentMansard(w, body, x, y, storeys, attached, bays, bay);
                break;
            case ApartmentForm.BayFront:
                Segment[] set = [new(0f, frontage, BaySetBack)];
                Found street = ApartmentWalls(w, body, x, y, set, storeys, attached);
                foreach (int at in BayFrontBays(body, bays, bay))
                {
                    float centre = -x + ((at + .5f) * bay), bw = MathF.Min(3.2f, bay + .6f);
                    var glazing = new (float, float)[storeys];
                    for (int k = 0; k < storeys; k++) glazing[k] = ((k * Storey) + .95f, (k * Storey) + 2.65f);
                    Canted(w, centre, bw, -y + BaySetBack, -y, PlinthHeight, height, glazing);
                    w.Box(new Vector3(centre - (bw / 2f), -y, 0f), new Vector3(centre + (bw / 2f), -y + BaySetBack, PlinthHeight), "plinth");
                }

                FlatRoof(w, body, x, y, set, height, street, attached);
                break;
            case ApartmentForm.Forecourt:
                float wing = Math.Max(2, (int)MathF.Round(bays / 4f)) * bay;
                Segment[] court = [new(0f, wing, 0f), new(wing, frontage - wing, ForecourtMetres), new(frontage - wing, frontage, 0f)];
                FlatRoof(w, body, x, y, court, height, ApartmentWalls(w, body, x, y, court, storeys, attached), attached);
                w.Box(new Vector3(-x + wing, -y, 0f), new Vector3(x - wing, -y + ForecourtMetres, .14f), "plinth");
                break;
            case ApartmentForm.Setback:
                Setback(w, body, x, y, storeys, attached, bays, bay);
                break;
        }

        return mesh;
    }

    /// <summary>
    /// How many bays a centre gable spans: about a third of the frontage, leaving an even number of
    /// bays for the wings, or <c>null</c> where the roof is too shallow to hold its gable.
    /// </summary>
    private static int? CentreGableBays(int bays, float bay, float depth)
    {
        int widest = Math.Max(1, (int)((depth - CentreGableMetres - 1.6f) / bay));
        int mid = Math.Min(Math.Max(2, (int)MathF.Round(bays / 3f)), widest);
        if ((bays - mid) % 2 != 0) mid += mid > 2 || mid + 1 > widest ? -1 : 1;
        return mid >= 1 && mid < bays && mid * bay <= depth - CentreGableMetres - 1.6f ? mid : null;
    }

    /// <summary>
    /// The bays a bay front's canted bays stand on: one in from each end, and two more about the
    /// middle on a frontage of ten bays or more. A canted bay wider than its bay moves one bay inward
    /// where it would overhang a street door outside it.
    /// </summary>
    private static int[] BayFrontBays(FamilyBody body, int bays, float bay)
    {
        if (bays < 3) return [];
        int[] picks = bays < 10 ? [1, bays - 2] : [1, (bays / 2) - 1, (bays / 2) + (bays % 2), bays - 2];
        BayKind[] ground = body.Street.Ground.Over(bays);
        if (MathF.Min(3.2f, bay + .6f) > bay)
        {
            for (int i = 0; i < picks.Length; i++)
            {
                int at = picks[i], inward = at < bays / 2f ? 1 : -1;
                if (ground[at - inward] == BayKind.Door) picks[i] = at + inward;
            }
        }

        return [.. picks.Where(at => at > 0 && at < bays - 1).Distinct().Order()];
    }

    /// <summary>
    /// The walls of a plan whose street wall runs in <paramref name="segments"/>, each at its own set-back,
    /// with a blank return wherever two meet at different set-backs. The back wall runs the whole frontage.
    /// </summary>
    /// <returns>What the street wall's bays leave for the roof: its stair stacks.</returns>
    private static Found ApartmentWalls(Writer w, FamilyBody body, float x, float y, ReadOnlySpan<Segment> segments, int storeys,
        AttachedSides attached)
    {
        float frontage = 2f * x, height = storeys * Storey;
        var street = new Found();
        foreach (Segment s in segments)
        {
            var face = new Face(new Vector3(-x, -y + s.SetBack, 0f), Vector3.UnitX, -Vector3.UnitY);
            Facade(w, body, face, frontage, height, storeys, body.Street, isStreet: true, street, from: s.From, to: s.To);
            w.Box(new Vector3(-x + s.From, -y + s.SetBack - .05f, 0f), new Vector3(-x + s.To, y + .05f, PlinthHeight), "plinth");
        }

        for (int i = 1; i < segments.Length; i++)
        {
            float a = segments[i - 1].SetBack, b = segments[i].SetBack;
            if (MathF.Abs(a - b) < 1e-3f) continue;
            float at = -x + segments[i].From, lo = MathF.Min(a, b), hi = MathF.Max(a, b);
            Face turn = a < b
                ? new Face(new Vector3(at, -y + lo, 0f), Vector3.UnitY, Vector3.UnitX)
                : new Face(new Vector3(at, -y + hi, 0f), -Vector3.UnitY, -Vector3.UnitX);
            w.Facade(turn, hi - lo, height, []);
        }

        var party = new WallRule(BayRow.Blank, BayRow.Blank);
        Facade(w, body, new Face(new Vector3(x, y, 0f), -Vector3.UnitX, Vector3.UnitY), frontage, height, storeys, body.Back, isStreet: false, new Found());
        float left = segments[0].SetBack, right = segments[^1].SetBack;
        Facade(w, body, new Face(new Vector3(-x, y, 0f), -Vector3.UnitY, -Vector3.UnitX), (2f * y) - left, height, storeys,
            attached.HasFlag(AttachedSides.Left) ? party : body.Side, isStreet: false, new Found());
        Facade(w, body, new Face(new Vector3(x, -y + right, 0f), Vector3.UnitY, Vector3.UnitX), (2f * y) - right, height, storeys,
            attached.HasFlag(AttachedSides.Right) ? party : body.Side, isStreet: false, new Found());

        if (body.PartyLine)
        {
            foreach (Segment s in segments)
            {
                if (s.From >= x || s.To <= x) continue;
                w.Box(new Vector3(-.15f, -y + s.SetBack - .12f, PlinthHeight), new Vector3(.15f, -y + s.SetBack, height), "wall-end");
            }

            w.Box(new Vector3(-.15f, y, PlinthHeight), new Vector3(.15f, y + .12f, height), "wall-end");
        }

        return street;
    }

    /// <summary>
    /// A roof deck behind a coped parapet that follows the street wall's set-backs, with the roof kit
    /// behind the rearmost. The coping overhangs a free end and stops at a shared one.
    /// </summary>
    private static void FlatRoof(Writer w, FamilyBody body, float x, float y, ReadOnlySpan<Segment> segments, float height, Found street,
        AttachedSides attached)
    {
        float top = height + (body.ParapetMetres > 0f ? body.ParapetMetres : ApartmentParapet);
        float left = attached.HasFlag(AttachedSides.Left) ? -x : -x - .02f, right = attached.HasFlag(AttachedSides.Right) ? x : x + .02f;
        float deepest = 0f;
        foreach (Segment s in segments)
        {
            float f = -y + s.SetBack;
            deepest = MathF.Max(deepest, s.SetBack);
            w.Box(new Vector3(-x + s.From + .1f, f + .1f, height - .2f), new Vector3(-x + s.To - .1f, y - .1f, height + .02f), "membrane");
            w.Box(new Vector3(-x + s.From, f, height), new Vector3(-x + s.To, f + .25f, top), "wall");
            float from = s.From > 0f ? -x + s.From - .02f : left, to = s.To < 2f * x ? -x + s.To + .02f : right;
            w.Box(new Vector3(from, f - .05f, top), new Vector3(to, f + .3f, top + .08f), "trim");
        }

        for (int i = 1; i < segments.Length; i++)
        {
            float a = segments[i - 1].SetBack, b = segments[i].SetBack;
            if (MathF.Abs(a - b) < 1e-3f) continue;
            float at = -x + segments[i].From, lo = -y + MathF.Min(a, b), hi = -y + MathF.Max(a, b);
            w.Box(new Vector3(at - .125f, lo, height), new Vector3(at + .125f, hi + .25f, top), "wall");
            w.Box(new Vector3(at - .15f, lo, top), new Vector3(at + .15f, hi + .3f, top + .08f), "trim");
        }

        w.Box(new Vector3(-x, y - .25f, height), new Vector3(x, y, top), "wall");
        w.Box(new Vector3(left, y - .3f, top), new Vector3(right, y + .05f, top + .08f), "trim");
        w.Box(new Vector3(-x, -y + segments[0].SetBack, height), new Vector3(-x + .25f, y, top), "wall");
        w.Box(new Vector3(left, -y + segments[0].SetBack, top), new Vector3(-x + .27f, y, top + .08f), "trim");
        w.Box(new Vector3(x - .25f, -y + segments[^1].SetBack, height), new Vector3(x, y, top), "wall");
        w.Box(new Vector3(x - .27f, -y + segments[^1].SetBack, top), new Vector3(right, y, top + .08f), "trim");

        RoofKit(w, body, x, -y + deepest, y, height, street);
    }

    /// <summary>
    /// A ridge along the street over the range from <paramref name="front"/> back to <paramref name="back"/>.
    /// A free end hips or shows a gable, and an end against a neighbor stops at the party wall. A hipped
    /// roof free at both ends and deeper than it is wide turns its ridge to run back from the street.
    /// </summary>
    private static void Pitched(Writer w, float x, float front, float back, float height, float degrees, bool hip,
        AttachedSides attached, CrossGable? gable, float eaves = ApartmentEaves)
    {
        float E = eaves;
        float t = MathF.Tan(degrees * MathF.PI / 180f);
        float half = (back - front) / 2f, cy = (front + back) / 2f, top = height + (half * t), low = height - (E * t);
        float ya = front - E, yb = back + E;
        bool left = attached.HasFlag(AttachedSides.Left), right = attached.HasFlag(AttachedSides.Right);
        if (hip && !left && !right && half > x)
        {
            TurnedHip(w, x, front, back, height, t, gable, E);
            return;
        }

        (float xa, float ra) = left ? (-x, -x) : hip ? (-x - E, -x + half) : (-x - ApartmentVerge, -x - ApartmentVerge);
        (float xb, float rb) = right ? (x, x) : hip ? (x + E, x - half) : (x + ApartmentVerge, x + ApartmentVerge);
        if (ra > rb) ra = rb = (ra + rb) / 2f;

        var forward = new Vector3(0f, -1f, 1f);
        if (gable is { } g)
        {
            var apex = new Vector3(g.Centre, front + g.Half, height + (g.Half * t));
            float xe0 = g.Centre - g.Half - E, xe1 = g.Centre + g.Half + E;
            Outward(w, "roof", forward, [new(xa, ya, low), new(xe0, ya, low), apex, new(g.Centre, cy, top), new(ra, cy, top)]);
            Outward(w, "roof", forward, [new(xe1, ya, low), new(xb, ya, low), new(rb, cy, top), new(g.Centre, cy, top), apex]);
            FrontGableOn(w, g, front, height, low, t, E);
        }
        else
        {
            Outward(w, "roof", forward, [new(xa, ya, low), new(xb, ya, low), new(rb, cy, top), new(ra, cy, top)]);
        }

        Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, yb, low), new(xa, yb, low), new(ra, cy, top), new(rb, cy, top)]);
        foreach ((bool shared, float side, float xe, float r) in (ReadOnlySpan<(bool, float, float, float)>)[(left, -x, xa, ra), (right, x, xb, rb)])
        {
            float sign = MathF.Sign(side);
            if (hip && !shared) Outward(w, "roof", new Vector3(sign, 0f, 1f), [new(xe, yb, low), new(xe, ya, low), new(r, cy, top)]);
            else Outward(w, "wall-end", new Vector3(sign, 0f, 0f), [new(side, front, height), new(side, back, height), new(side, cy, top)]);

            if (shared)
            {
                float edge = half + .15f;
                Vector2[] profile =
                [
                    new(cy - edge, height - .3f), new(cy + edge, height - .3f), new(cy + edge, top - (edge * t) + .25f),
                    new(cy, top + .25f), new(cy - edge, top - (edge * t) + .25f),
                ];
                w.Prism(profile, MathF.Min(side, side - (sign * .15f)), MathF.Max(side, side - (sign * .15f)), "wall-end");
            }
            else if (!hip)
            {
                Outward(w, "trim", new Vector3(sign, 0f, 0f), [new(xe, ya, low + .2f), new(xe, cy, top + .2f), new(xe, cy, top), new(xe, ya, low)]);
                Outward(w, "trim", new Vector3(sign, 0f, 0f), [new(xe, cy, top + .2f), new(xe, yb, low + .2f), new(xe, yb, low), new(xe, cy, top)]);
            }
        }

        Gutters(w, xa, xb, ya, yb, low);
        foreach (int k in (ReadOnlySpan<int>)[1, 3]) Chimney(w, -x + (x * k / 2f), cy + .6f, top - 1.2f, top + 1.1f);
    }

    /// <summary>A hipped roof whose ridge runs back from the street, over a Building free at both ends.</summary>
    private static void TurnedHip(Writer w, float x, float front, float back, float height, float t, CrossGable? gable,
        float E = ApartmentEaves)
    {
        float top = height + (x * t), low = height - (E * t);
        float xa = -x - E, xb = x + E, ya = front - E, yb = back + E, r0 = front + x, r1 = back - x;
        Outward(w, "roof", new Vector3(-1f, 0f, 1f), [new(xa, yb, low), new(xa, ya, low), new(0f, r0, top), new(0f, r1, top)]);
        Outward(w, "roof", new Vector3(1f, 0f, 1f), [new(xb, ya, low), new(xb, yb, low), new(0f, r1, top), new(0f, r0, top)]);
        Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, yb, low), new(xa, yb, low), new(0f, r1, top)]);
        var forward = new Vector3(0f, -1f, 1f);
        if (gable is { } g)
        {
            var apex = new Vector3(g.Centre, front + g.Half, height + (g.Half * t));
            Outward(w, "roof", forward, [new(xa, ya, low), new(g.Centre - g.Half - E, ya, low), apex, new(0f, r0, top)]);
            Outward(w, "roof", forward, [new(g.Centre + g.Half + E, ya, low), new(xb, ya, low), new(0f, r0, top), apex]);
            FrontGableOn(w, g, front, height, low, t, E);
        }
        else
        {
            Outward(w, "roof", forward, [new(xa, ya, low), new(xb, ya, low), new(0f, r0, top)]);
        }

        Gutters(w, xa, xb, ya, yb, low);
        foreach (int k in (ReadOnlySpan<int>)[1, 3]) Chimney(w, .6f, front + ((back - front) * k / 4f), top - 1.2f, top + 1.1f);
    }

    /// <summary>A front gable whose ridge runs back into the main roof's front slope, with an attic window where it is tall enough.</summary>
    private static void FrontGableOn(Writer w, CrossGable g, float front, float height, float low, float t, float E = ApartmentEaves)
    {
        float gt = height + (g.Half * t), ye = g.Front - E, ya = front - E;
        float xe0 = g.Centre - g.Half - E, xe1 = g.Centre + g.Half + E;
        var ridge = new Vector3(g.Centre, front + g.Half, gt);
        Outward(w, "roof", new Vector3(-1f, 0f, 1f), [new(xe0, ye, low), new(xe0, ya, low), ridge, new(g.Centre, ye, gt)]);
        Outward(w, "roof", new Vector3(1f, 0f, 1f), [new(xe1, ya, low), new(xe1, ye, low), new(g.Centre, ye, gt), ridge]);
        Outward(w, "wall", -Vector3.UnitY, [new(g.Centre - g.Half, g.Front, height), new(g.Centre + g.Half, g.Front, height), new(g.Centre, g.Front, gt)]);
        foreach (float xe in (ReadOnlySpan<float>)[xe0, xe1])
        {
            Outward(w, "trim", -Vector3.UnitY, [new(xe, ye - .02f, low), new(g.Centre, ye - .02f, gt), new(g.Centre, ye - .02f, gt + .2f), new(xe, ye - .02f, low + .2f)]);
        }

        if (gt - height > 2.2f)
        {
            float gw = MathF.Min(1.2f, g.Half * .5f);
            w.Box(new Vector3(g.Centre - (gw / 2f) - .1f, g.Front - .07f, height + .45f), new Vector3(g.Centre + (gw / 2f) + .1f, g.Front, height + 1.85f), "trim");
            w.Box(new Vector3(g.Centre - (gw / 2f), g.Front - .09f, height + .55f), new Vector3(g.Centre + (gw / 2f), g.Front - .06f, height + 1.75f), "glass");
        }
    }

    private static void Gutters(Writer w, float xa, float xb, float ya, float yb, float low)
    {
        w.Box(new Vector3(xa, ya, low - .18f), new Vector3(xb, ya + .22f, low), "metal");
        w.Box(new Vector3(xa, yb - .22f, low - .18f), new Vector3(xb, yb, low), "metal");
    }

    /// <summary>A stack with three pots on it.</summary>
    private static void Chimney(Writer w, float x, float y, float z0, float z1)
    {
        w.Box(new Vector3(x - .5f, y - .45f, z0), new Vector3(x + .5f, y + .45f, z1), "wall-end");
        foreach (float dx in (ReadOnlySpan<float>)[-.25f, 0f, .25f])
        {
            w.Box(new Vector3(x + dx - .1f, y - .1f, z1), new Vector3(x + dx + .1f, y + .1f, z1 + .4f), "wall-end");
        }
    }

    /// <summary>The top storey inside a slate mansard lit by a dormer a bay, over walls a storey lower.</summary>
    private static void ApartmentMansard(Writer w, FamilyBody body, float x, float y, int storeys, AttachedSides attached, int bays, float bay)
    {
        float eaves = (storeys - 1) * Storey;
        ApartmentWalls(w, body, x, y, [new(0f, 2f * x, 0f)], storeys - 1, attached);
        BayKind[] upper = body.Street.Upper.Over(bays);
        var front = new List<float>();
        var back = new List<float>();
        for (int i = 0; i < bays; i++)
        {
            float c = -x + ((i + .5f) * bay);
            back.Add(c);
            if (upper[i] != BayKind.Blank) front.Add(c);
        }

        MansardRoof(w, x, y, eaves, attached, [.. front], [.. back], MathF.Min(.75f, (bay / 2f) - .45f));
    }

    /// <summary>
    /// A slate mansard from <paramref name="eaves"/> up a storey, hipped at each free end and closed by
    /// an upstand at each shared one, with a dormer at each centre given for the street and back slopes.
    /// </summary>
    private static void MansardRoof(Writer w, float x, float y, float eaves, AttachedSides attached,
        ReadOnlySpan<float> front, ReadOnlySpan<float> back, float dw)
    {
        const float K = MansardInset;
        float crown = eaves + Storey;
        bool left = attached.HasFlag(AttachedSides.Left), right = attached.HasFlag(AttachedSides.Right);
        float ka = left ? 0f : K, kb = right ? 0f : K;
        float xa = left ? -x : -x - .08f, xb = right ? x : x + .08f;
        Outward(w, "roof", new Vector3(0f, -1f, 1f), [new(xa, -y - .08f, eaves), new(xb, -y - .08f, eaves), new(x - kb, -y + K, crown), new(-x + ka, -y + K, crown)]);
        Outward(w, "roof", new Vector3(0f, 1f, 1f), [new(xb, y + .08f, eaves), new(xa, y + .08f, eaves), new(-x + ka, y - K, crown), new(x - kb, y - K, crown)]);
        foreach ((bool shared, float side) in (ReadOnlySpan<(bool, float)>)[(left, -x), (right, x)])
        {
            float sign = MathF.Sign(side);
            if (shared)
            {
                Outward(w, "wall-end", new Vector3(sign, 0f, 0f), [new(side, -y, eaves), new(side, y, eaves), new(side, y - K, crown), new(side, -y + K, crown)]);
                Vector2[] profile =
                [
                    new(-y - .1f, eaves - .3f), new(y + .1f, eaves - .3f), new(y + .1f, eaves + .25f),
                    new(y - K + .05f, crown + .3f), new(-y + K - .05f, crown + .3f), new(-y - .1f, eaves + .25f),
                ];
                w.Prism(profile, MathF.Min(side, side - (sign * .15f)), MathF.Max(side, side - (sign * .15f)), "wall-end");
            }
            else
            {
                float xs = side + (sign * .08f), xi = side - (sign * K);
                Outward(w, "roof", new Vector3(sign, 0f, 1f), [new(xs, y + .08f, eaves), new(xs, -y - .08f, eaves), new(xi, -y + K, crown), new(xi, y - K, crown)]);
            }
        }

        float ca = left ? -x : -x + ka - .05f, cb = right ? x : x - kb + .05f;
        w.Box(new Vector3(-x + ka, -y + K, crown - .1f), new Vector3(x - kb, y - K, crown + .1f), "membrane");
        w.Box(new Vector3(ca, -y + K - .05f, crown + .1f), new Vector3(cb, -y + K + .2f, crown + .22f), "trim");
        w.Box(new Vector3(ca, y - K - .2f, crown + .1f), new Vector3(cb, y - K + .05f, crown + .22f), "trim");
        w.Box(new Vector3(-x, -y - .45f, eaves - .35f), new Vector3(x, -y, eaves), "trim");
        w.Box(new Vector3(-x, y, eaves - .25f), new Vector3(x, y + .3f, eaves), "trim");

        foreach ((float face, float sign) in (ReadOnlySpan<(float, float)>)[(-y, 1f), (y, -1f)])
        {
            foreach (float c in sign > 0f ? front : back)
            {
                float glass = face + (sign * .35f), behind = face + (sign * (K + .3f));
                float lo = MathF.Min(glass, behind), hi = MathF.Max(glass, behind);
                w.Box(new Vector3(c - dw, lo, eaves + .25f), new Vector3(c + dw, hi, eaves + 2.5f), "trim");
                float pane = glass - (sign * .03f);
                w.Box(new Vector3(c - dw + .15f, pane - .01f, eaves + .55f), new Vector3(c + dw - .15f, pane + .01f, eaves + 2.2f), "glass");
                w.Box(new Vector3(c - dw - .15f, lo - .12f, eaves + 2.45f), new Vector3(c + dw + .15f, hi, eaves + 2.65f), "roof");
            }
        }

        foreach (int k in (ReadOnlySpan<int>)[1, 3]) Chimney(w, -x + (x * k / 2f), 0f, crown - .2f, crown + 1.4f);
    }

    /// <summary>
    /// The top storey set back from the street behind a railed roof terrace. Every other street
    /// opening on it is a door onto the terrace.
    /// </summary>
    private static void Setback(Writer w, FamilyBody body, float x, float y, int storeys, AttachedSides attached, int bays, float bay)
    {
        float frontage = 2f * x, eaves = (storeys - 1) * Storey, back = -y + SetbackMetres;
        Found street = ApartmentWalls(w, body, x, y, [new(0f, frontage, 0f)], storeys - 1, attached);
        w.Box(new Vector3(-x + .1f, -y + .1f, eaves - .2f), new Vector3(x - .1f, back, eaves + .05f), "membrane");
        w.Box(new Vector3(-x, -y - .3f, eaves - .3f), new Vector3(x, -y + .15f, eaves + .12f), "trim");
        w.Box(new Vector3(-x + .05f, -y + .02f, eaves + 1.05f), new Vector3(x - .05f, -y + .1f, eaves + 1.12f), "metal");
        int posts = Math.Max(2, (int)MathF.Round(frontage / 1.5f));
        for (int i = 1; i < posts; i++)
        {
            float at = -x + (frontage * i / posts);
            w.Box(new Vector3(at - .03f, -y + .02f, eaves + .05f), new Vector3(at + .03f, -y + .08f, eaves + 1.05f), "metal");
        }

        List<Opening> onto = TopOpenings(body, body.Street.Upper, bays, bay);
        for (int i = 0; i < onto.Count; i += 2)
        {
            if (onto[i].Kind == OpeningKind.Window) onto[i] = onto[i] with { Kind = OpeningKind.Door };
        }

        onto = onto.ConvertAll(o => o.Kind == OpeningKind.Door ? o with { V = .15f, H = 2.3f } : o);
        w.Facade(new Face(new Vector3(-x, back, eaves), Vector3.UnitX, -Vector3.UnitY), frontage, Storey, onto, wall: "wall-end");
        w.Facade(new Face(new Vector3(x, y, eaves), -Vector3.UnitX, Vector3.UnitY), frontage, Storey, TopOpenings(body, body.Back.Upper, bays, bay), wall: "wall-end");
        w.Facade(new Face(new Vector3(-x, y, eaves), -Vector3.UnitY, -Vector3.UnitX), y - back, Storey, [], wall: "wall-end");
        w.Facade(new Face(new Vector3(x, back, eaves), Vector3.UnitY, Vector3.UnitX), y - back, Storey, [], wall: "wall-end");
        FlatRoof(w, body, x, y, [new(0f, frontage, SetbackMetres)], storeys * Storey, street, attached);
    }

    /// <summary>One storey of a wall's upper bays, opened as a ground storey opens them.</summary>
    private static List<Opening> TopOpenings(FamilyBody body, BayRow row, int bays, float bay)
    {
        var openings = new List<Opening>();
        BayKind[] kinds = row.Over(bays);
        for (int b = 0; b < bays; b++) Openings(openings, body.Openings, kinds[b], b * bay, bay, 0f, ground: true, Storey);
        openings.RemoveAll(o => !(o.U > 0f && o.U + o.W < bays * bay && o.V > 0f && o.V + o.H < Storey));
        return openings;
    }

    /// <summary>A convex polygon, wound so its face looks the way <paramref name="outward"/> leans.</summary>
    /// <remarks>A corner that repeats the one before it, as a hip's ridge ends do where they meet, is dropped.</remarks>
    private static void Outward(Writer w, string part, Vector3 outward, ReadOnlySpan<Vector3> points)
    {
        Span<Vector3> kept = stackalloc Vector3[points.Length];
        int n = 0;
        foreach (Vector3 p in points)
        {
            if (n == 0 || Vector3.DistanceSquared(p, kept[n - 1]) > 1e-8f) kept[n++] = p;
        }

        if (n > 1 && Vector3.DistanceSquared(kept[0], kept[n - 1]) <= 1e-8f) n--;
        if (n < 3) return;
        kept = kept[..n];
        Vector3 normal = Vector3.Cross(kept[1] - kept[0], kept[^1] - kept[0]);
        if (Vector3.Dot(normal, outward) < 0f) kept.Reverse();
        w.Polygon(part, kept);
    }
}
