using System.Numerics;

namespace Borough.Appearance;

/// <summary>Where a house stands in its terrace, counted from the face's start, and the pitch its terrace shares.</summary>
/// <param name="PitchDegrees">The terrace's pitch, or 0 for the midpoint of the family's range.</param>
/// <param name="Index">The house's place in its terrace. Odd houses lay their bays from the right.</param>
/// <param name="Houses">How many Lots the terrace spans.</param>
public readonly record struct TerracePlace(int PitchDegrees, int Index, int Houses);

/// <summary>
/// The attached house forms: one house between its side walls, varied by roof and plan. They follow
/// <c>scripts/art/rowhouse-forms.py</c>.
/// </summary>
/// <remarks>
/// Each house closes against a neighbor of any form. It draws its own roof ends over a shared wall
/// and an upstand where its roof meets one, and a taller neighbor shows its end above a lower roof.
/// The walls follow the family's bay rules. The mansard draws about 6% less floor than its Building
/// holds and the bay front about 7% less, inside the drawing's 20% (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    /// <summary>How far a parapet roof's street parapet stands above the roof deck.</summary>
    public const float TerraceParapetMetres = .9f;

    /// <summary>How far a bay front's wall stands behind the Lot line.</summary>
    public const float BaySetBack = .9f;

    private const float MansardInset = 1.1f;
    private const float TerraceEaves = .35f;
    private const float FreeVerge = .3f;

    private static FamilyBodyMesh BuildTerrace(FamilyBody body, TerraceBody terrace, float frontage, float depth, int storeys,
        AttachedSides attached, TerracePlace place)
    {
        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body);
        float x = frontage / 2f, y = depth / 2f;
        float pitch = place.PitchDegrees > 0 ? place.PitchDegrees : body.GableDegrees;
        bool mirror = place.Index % 2 == 1;
        int bays = Bays(frontage, body.BayMetres);
        int wallStoreys = terrace.Form == TerraceForm.Mansard && storeys > 1 ? storeys - 1 : storeys;
        float height = wallStoreys * Storey;
        float front = terrace.Form == TerraceForm.BayFront && bays > 1 ? -y + BaySetBack : -y;

        TerraceWalls(w, body, x, front, y, height, wallStoreys, attached, mirror);
        switch (terrace.Form)
        {
            case TerraceForm.SideGable:
                Gable(w, body, frontage, depth, height, attached, pitch);
                break;
            case TerraceForm.BayFront:
                Gable(w, body, frontage, y - front, height, attached, pitch, (front + y) / 2f);
                BayWindows(w, body, x, y, front, height, bays, mirror);
                break;
            case TerraceForm.Parapet:
                ParapetRoof(w, body, x, y, height, attached);
                break;
            case TerraceForm.Mansard:
                Mansard(w, body, x, y, height, attached, bays);
                break;
            case TerraceForm.FrontGable:
                FrontGable(w, body, x, y, height, attached, pitch, 0);
                break;
            case TerraceForm.PairedGable:
                FrontGable(w, body, x, y, height, attached, pitch, PairHalf(place, attached));
                break;
        }

        return mesh;
    }

    /// <summary>
    /// Which half of a pair's gable a house draws: 1 where the ridge stands on its right wall, -1 on
    /// its left, and 0 for a whole gable where its partner is missing.
    /// </summary>
    internal static int PairHalf(TerracePlace place, AttachedSides attached)
    {
        bool left = attached.HasFlag(AttachedSides.Left), right = attached.HasFlag(AttachedSides.Right);
        if (place.Index % 2 == 0 && place.Index + 1 < place.Houses && right && !attached.HasFlag(AttachedSides.RightCrosswise)) return 1;
        if (place.Index % 2 == 1 && left && !attached.HasFlag(AttachedSides.LeftCrosswise)) return -1;
        return 0;
    }

    private static void TerraceWalls(Writer w, FamilyBody body, float x, float front, float y, float height, int storeys,
        AttachedSides attached, bool mirror)
    {
        float width = 2f * x, depth = y - front;
        var street = new Face(new Vector3(-x, front, 0), Vector3.UnitX, -Vector3.UnitY);
        var back = new Face(new Vector3(x, y, 0), -Vector3.UnitX, Vector3.UnitY);
        var left = new Face(new Vector3(-x, y, 0), -Vector3.UnitY, -Vector3.UnitX);
        var right = new Face(new Vector3(x, front, 0), Vector3.UnitY, Vector3.UnitX);
        var party = new WallRule(BayRow.Blank, BayRow.Blank);
        var found = new Found();
        Facade(w, body, street, width, height, storeys, body.Street, isStreet: true, found, mirror);
        Facade(w, body, back, width, height, storeys, body.Back, isStreet: false, found, mirror);
        Facade(w, body, left, depth, height, storeys, attached.HasFlag(AttachedSides.Left) ? party : body.Side, isStreet: false, found);
        Facade(w, body, right, depth, height, storeys, attached.HasFlag(AttachedSides.Right) ? party : body.Side, isStreet: false, found);

        float plinthLeft = attached.HasFlag(AttachedSides.Left) ? -x : -x - .05f;
        float plinthRight = attached.HasFlag(AttachedSides.Right) ? x : x + .05f;
        w.Box(new Vector3(plinthLeft, front - .05f, 0), new Vector3(plinthRight, y + .05f, PlinthHeight), "plinth");
    }

    /// <summary>
    /// A two-storey canted bay that reaches the Lot line before the set-back wall, with a pier on each
    /// side wall closing the recess. A house of one bay has no room beside its door, so it carries an
    /// oriel over the door on its first floor instead, and its wall stays on the line.
    /// </summary>
    private static void BayWindows(Writer w, FamilyBody body, float x, float y, float front, float height, int bays, bool mirror)
    {
        float bay = 2f * x / bays;
        if (bays == 1)
        {
            float ow = MathF.Min(2.4f, 2f * x - .8f);
            float z0 = Storey + .2f, z1 = MathF.Min(2f * Storey, height) - .2f;
            Canted(w, 0f, ow, -y, -y - .6f, z0, z1, [(Storey + .5f, z1 - .35f)]);
            w.Box(new Vector3((-ow / 2f) - .1f, -y - .72f, z0 - .25f), new Vector3((ow / 2f) + .1f, -y, z0), "trim");
            return;
        }

        foreach (float side in (ReadOnlySpan<float>)[-x, x])
        {
            float inner = side < 0 ? side + .25f : side - .25f;
            w.Box(new Vector3(MathF.Min(side, inner), -y, 0f), new Vector3(MathF.Max(side, inner), front, height), "wall");
        }

        int at = mirror ? bays - 2 : 1;
        float centre = -x + ((at + .5f) * bay);
        float bw = MathF.Min(3.4f, bay - .4f);
        float top = MathF.Min(2f * Storey, height);
        Canted(w, centre, bw, front, -y, PlinthHeight, top, [(.95f, 2.65f), (Storey + .75f, Storey + 2.6f)]);
        w.Box(new Vector3(centre - (bw / 2f), -y, 0f), new Vector3(centre + (bw / 2f), front, PlinthHeight), "plinth");
    }

    /// <summary>
    /// A bay of three walls from <paramref name="wall"/> out to <paramref name="face"/>, canted at its
    /// sides, glazed in each band of <paramref name="glazing"/> and capped with a lid.
    /// </summary>
    private static void Canted(Writer w, float centre, float width, float wall, float face, float z0, float z1,
        ReadOnlySpan<(float Low, float High)> glazing)
    {
        float out_ = wall - face;
        float fw = (width / 2f) - out_ * .55f;
        Vector2[] plan = [new(centre - (width / 2f), wall), new(centre - fw, face), new(centre + fw, face), new(centre + (width / 2f), wall)];
        for (int i = 0; i < 3; i++)
        {
            Vector2 a = plan[i], b = plan[i + 1];
            w.Polygon("wall", [new(a.X, a.Y, z0), new(b.X, b.Y, z0), new(b.X, b.Y, z1), new(a.X, a.Y, z1)]);
            Vector2 along = Vector2.Normalize(b - a);
            var outward = new Vector2(along.Y, -along.X) * .03f;
            float inset = MathF.Min(.15f, Vector2.Distance(a, b) * .12f);
            Vector2 p = a + (along * inset) + outward, q = b - (along * inset) + outward;
            foreach ((float low, float high) in glazing)
            {
                float v0 = MathF.Max(low, z0 + .1f), v1 = MathF.Min(high, z1 - .1f);
                if (v1 > v0) w.Polygon("glass", [new(p.X, p.Y, v0), new(q.X, q.Y, v0), new(q.X, q.Y, v1), new(p.X, p.Y, v1)]);
            }
        }

        w.Box(new Vector3(centre - (width / 2f) - .1f, face - .12f, z1), new Vector3(centre + (width / 2f) + .1f, wall, z1 + .25f), "trim");
    }

    /// <summary>A flat roof behind a street parapet, with a bracketed cornice under it.</summary>
    private static void ParapetRoof(Writer w, FamilyBody body, float x, float y, float height, AttachedSides attached)
    {
        float top = height + TerraceParapetMetres;
        w.Box(new Vector3(-x, -y, height - .2f), new Vector3(x, y, height + .05f), "roof");
        w.Box(new Vector3(-x, -y, height), new Vector3(x, -y + .25f, top), "wall");
        w.Box(new Vector3(-x, y - .25f, height), new Vector3(x, y, height + .5f), "wall");
        foreach ((float side, bool shared) in (ReadOnlySpan<(float, bool)>)[(-x, attached.HasFlag(AttachedSides.Left)), (x, attached.HasFlag(AttachedSides.Right))])
        {
            float inner = side - (MathF.Sign(side) * (shared ? .15f : .25f));
            w.Box(new Vector3(MathF.Min(side, inner), -y + .25f, height), new Vector3(MathF.Max(side, inner), y - .25f, height + (shared ? .6f : .5f)), "wall");
        }

        w.Box(new Vector3(-x, -y - .45f, height - .45f), new Vector3(x, -y, height - .2f), "trim");
        w.Box(new Vector3(-x, -y - .06f, top), new Vector3(x, -y + .32f, top + .1f), "trim");
        int brackets = Math.Max(2, (int)MathF.Round(2f * x / .9f));
        for (int i = 0; i < brackets; i++)
        {
            float at = -x + (2f * x * (i + .5f) / brackets);
            w.Box(new Vector3(at - .06f, -y - .4f, height - .95f), new Vector3(at + .06f, -y, height - .45f), "trim");
        }

        if (body.Chimney && attached.HasFlag(AttachedSides.Right))
        {
            w.Box(new Vector3(x - .4f, -y + 3f, height), new Vector3(x + .4f, -y + 3.9f, top + 1f), "wall-end");
        }

        w.Box(new Vector3(-.45f, y - 3f, height), new Vector3(.45f, y - 2.1f, height + .45f), "trim");
    }

    /// <summary>A storey inside a steep mansard over the walls, with a dormer a bay and a cornice at the eaves.</summary>
    private static void Mansard(Writer w, FamilyBody body, float x, float y, float eaves, AttachedSides attached, int bays)
    {
        bool left = attached.HasFlag(AttachedSides.Left), right = attached.HasFlag(AttachedSides.Right);
        float k = MansardInset, top = eaves + Storey, crown = top + .35f;
        float xa = left ? -x : -x - .1f, xb = right ? x : x + .1f;
        w.Polygon("roof", [new(xa, -y - .05f, eaves), new(xb, -y - .05f, eaves), new(xb, -y + k, top), new(xa, -y + k, top)]);
        w.Polygon("roof", [new(xb, y + .05f, eaves), new(xa, y + .05f, eaves), new(xa, y - k, top), new(xb, y - k, top)]);
        w.Polygon("roof", [new(xa, -y + k, top), new(xb, -y + k, top), new(xb, 0f, crown), new(xa, 0f, crown)]);
        w.Polygon("roof", [new(xb, y - k, top), new(xa, y - k, top), new(xa, 0f, crown), new(xb, 0f, crown)]);
        w.Polygon("wall", [new(-x, y, eaves), new(-x, -y, eaves), new(-x, -y + k, top), new(-x, 0f, crown), new(-x, y - k, top)]);
        w.Polygon("wall", [new(x, -y, eaves), new(x, y, eaves), new(x, y - k, top), new(x, 0f, crown), new(x, -y + k, top)]);
        w.Box(new Vector3(-x, -y - .4f, eaves - .3f), new Vector3(x, -y, eaves), "trim");
        w.Box(new Vector3(-x, y, eaves - .2f), new Vector3(x, y + .25f, eaves), "trim");

        float bay = 2f * x / bays, face = -y + .3f;
        for (int i = 0; i < bays; i++)
        {
            float c = -x + ((i + .5f) * bay);
            w.Box(new Vector3(c - .7f, face, eaves + .25f), new Vector3(c + .7f, -y + k + .4f, eaves + 2.45f), "trim");
            w.Box(new Vector3(c - .48f, face - .03f, eaves + .55f), new Vector3(c + .48f, face, eaves + 2.15f), "glass");
            w.Box(new Vector3(c - .03f, face - .05f, eaves + .55f), new Vector3(c + .03f, face, eaves + 2.15f), "frame");
            w.Box(new Vector3(c - .85f, face - .15f, eaves + 2.4f), new Vector3(c + .85f, -y + k + .4f, eaves + 2.6f), "roof");
        }

        Vector2[] profile =
        [
            new(-y - .1f, eaves - .3f), new(y + .1f, eaves - .3f), new(y + .1f, eaves + .25f), new(y - k + .05f, top + .25f),
            new(0f, crown + .25f), new(-y + k - .05f, top + .25f), new(-y - .1f, eaves + .25f),
        ];
        if (left) w.Prism(profile, -x, -x + .15f, "wall");
        if (right) w.Prism(profile, x - .15f, x, "wall");
        if (body.Chimney && right) w.Box(new Vector3(x - .4f, .3f, top - .5f), new Vector3(x + .4f, 1.2f, crown + 1.1f), "wall-end");
    }

    /// <summary>
    /// A ridge running back from the street. <paramref name="half"/> 0 draws a whole gable; 1 the left
    /// half of a gable whose ridge stands on the right wall, and -1 the right half of one on the left.
    /// A free end overhangs; a shared one stops at the wall, where neighboring gables meet in a valley.
    /// </summary>
    private static void FrontGable(Writer w, FamilyBody body, float x, float y, float height, AttachedSides attached, float degrees, int half)
    {
        float t = MathF.Tan(degrees * MathF.PI / 180f);
        float cx = half * x;
        float ya = -y - TerraceEaves, yb = y + TerraceEaves;
        float run = half == 0 ? x : 2f * x;
        float top = height + (run * t);
        if (half <= 0) Slope(-x, -1f, attached.HasFlag(AttachedSides.Left));
        if (half >= 0) Slope(x, 1f, attached.HasFlag(AttachedSides.Right));
        w.Polygon("wall", [new(-x, -y, height), new(x, -y, height), new(cx, -y, top)]);
        w.Polygon("wall", [new(x, y, height), new(-x, y, height), new(cx, y, top)]);

        // The attic window keeps 0.1 m of gable wall beside its frame's top corner.
        float off = half == 0 ? 0f : .7f;
        float glazed = (top - height) - ((off + .55f + .1f) * t) - .52f;
        if (glazed >= .6f)
        {
            float gx = cx - (half * off), gh = MathF.Min(1.3f, glazed);
            w.Box(new Vector3(gx - .55f, -y - .07f, height + .4f), new Vector3(gx + .55f, -y, height + .52f + gh), "trim");
            w.Box(new Vector3(gx - .43f, -y - .09f, height + .52f), new Vector3(gx + .43f, -y - .06f, height + .4f + gh), "glass");
        }

        if (half == 1 && body.Chimney)
        {
            w.Box(new Vector3(cx - .4f, -y + 3f, top - .8f), new Vector3(cx + .4f, -y + 3.9f, top + .9f), "wall-end");
        }

        void Slope(float outer, float side, bool shared)
        {
            float over = shared ? 0f : FreeVerge;
            float xe = outer + (side * over), low = height - (over * t);
            Vector3[] sheet = side < 0
                ? [new(xe, yb, low), new(xe, ya, low), new(cx, ya, top), new(cx, yb, top)]
                : [new(xe, ya, low), new(xe, yb, low), new(cx, yb, top), new(cx, ya, top)];
            TwoSided(w, "roof", sheet);
            foreach (float at in (ReadOnlySpan<float>)[ya - .03f, yb + .03f])
            {
                TwoSided(w, "trim", [new(xe, at, low - .05f), new(cx, at, top - .05f), new(cx, at, top + .15f), new(xe, at, low + .15f)]);
            }
        }
    }

    private static void TwoSided(Writer w, string part, ReadOnlySpan<Vector3> points)
    {
        Span<Vector3> back = stackalloc Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) back[i] = points[points.Length - 1 - i];
        w.Polygon(part, points);
        w.Polygon(part, back);
    }
}
