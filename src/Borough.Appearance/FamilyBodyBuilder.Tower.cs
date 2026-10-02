using System.Numerics;

namespace Borough.Appearance;

public static partial class FamilyBodyBuilder
{
    private const float TowerReferenceSite = 116f;
    private const float TowerRecess = .3f;

    internal sealed record TowerFacadeParameters(
        float Spandrel,
        string SpandrelPart,
        float Out,
        float FinEvery,
        float FinWidth,
        float FinFront,
        float FinBack,
        string FinPart);

    internal sealed record TowerWingSummary(
        string Name,
        int Storeys,
        float X0,
        float Y0,
        float X1,
        float Y1,
        float BaseMetres,
        float TopMetres);

    internal sealed record TowerLayoutSummary(
        IReadOnlyList<TowerWingSummary> Wings,
        float ShaftFloor,
        float DrawnFloor,
        float TopMetres);

    internal static IReadOnlyDictionary<string, TowerFacadeParameters> TowerFacades { get; } =
        new Dictionary<string, TowerFacadeParameters>(StringComparer.Ordinal)
        {
            ["banded"] = new(.9f, "trim", .35f, 7.25f, .25f, -.35f, TowerRecess, "trim"),
            ["curtain"] = new(1.2f, "spandrel", .05f, 1.5f, .08f, -.25f, TowerRecess, "frame"),
            ["ribbon"] = new(1.4f, "wall", 0f, 1.75f, .08f, TowerRecess - .1f, TowerRecess, "frame"),
            ["spine"] = new(.6f, "wall", 0f, 2.9f, .5f, -.6f, TowerRecess, "wall"),
            ["podium"] = new(1.3f, "wall-end", 0f, 6f, .6f, 0f, TowerRecess, "wall-end"),
        };

    /// <summary>Builds a tower body over its whole site footprint.</summary>
    /// <param name="frontage">The site width along the street, in metres.</param>
    /// <param name="depth">The site depth away from the street, in metres.</param>
    /// <param name="storeys">The Building's total storeys.</param>
    /// <param name="podiumStoreys">The podium storeys, clamped to the Building's total.</param>
    /// <param name="shaftFrontage">The simulation shaft's frontage, in metres.</param>
    /// <param name="shaftDepth">The simulation shaft's depth, in metres.</param>
    /// <remarks>
    /// Whole-storey wing rounding can change the requested shaft floor area. For 112-120 metre sites
    /// whose requested shaft spans half each site dimension, 1-70 shaft storeys and 0-8 podium storeys,
    /// the difference stays within the larger of one percent or one requested shaft storey's area.
    /// </remarks>
    public static FamilyBodyMesh BuildTower(FamilyBody body, float frontage, float depth, int storeys,
        int podiumStoreys, float shaftFrontage, float shaftDepth)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.Tower is null) throw new ArgumentException("A tower body must declare a tower variant.", nameof(body));

        TowerPlan plan = PlanTower(body.Tower.Variant, frontage, depth, storeys, podiumStoreys, shaftFrontage, shaftDepth);
        var mesh = new FamilyBodyMesh();
        var writer = new Writer(mesh, body);
        Podium(writer, frontage, depth, plan.PodiumStoreys, plan.Wings);
        foreach (TowerWing wing in plan.Wings)
        {
            DrawWing(writer, wing, plan.Wings);
            if (wing.Name.EndsWith("-front", StringComparison.Ordinal))
            {
                Terrace(writer, wing.Rect, wing.Top);
            }
        }

        return mesh;
    }

    internal static TowerLayoutSummary SolveTowerLayout(TowerVariant variant, float frontage, float depth,
        int storeys, int podiumStoreys, float shaftFrontage, float shaftDepth)
    {
        TowerPlan plan = PlanTower(variant, frontage, depth, storeys, podiumStoreys, shaftFrontage, shaftDepth);
        float drawn = plan.Wings.Sum(w => w.Rect.Area * w.Storeys);
        float top = plan.Wings.Count == 0 ? plan.PodiumStoreys * Storey : plan.Wings.Max(w => w.Top);
        TowerWingSummary[] wings =
        [
            .. plan.Wings.Where(w => w.Style != "reveal").Select(w => new TowerWingSummary(
                w.Name, w.Storeys, w.Rect.X0, w.Rect.Y0, w.Rect.X1, w.Rect.Y1, w.Z0, w.Top)),
        ];
        return new TowerLayoutSummary(wings, plan.ShaftFloor, drawn, top);
    }

    private static TowerPlan PlanTower(TowerVariant variant, float frontage, float depth, int storeys,
        int podiumStoreys, float shaftFrontage, float shaftDepth)
    {
        int total = Math.Max(0, storeys);
        int podium = Math.Clamp(podiumStoreys, 0, total);
        int shaftStoreys = total - podium;
        float shaftFloor = shaftFrontage * shaftDepth * shaftStoreys;
        List<TowerWing> wings = shaftStoreys > 0
            ? VariantWings(variant, frontage, depth, shaftStoreys, shaftFloor)
            : [];
        wings.RemoveAll(w => w.Storeys <= 0);
        Stack(wings, podium * Storey);
        return new TowerPlan(podium, shaftFloor, wings);
    }

    private static List<TowerWing> VariantWings(TowerVariant variant, float frontage, float depth,
        int shaftStoreys, float shaftFloor)
    {
        float sx = frontage / TowerReferenceSite;
        float sy = depth / TowerReferenceSite;
        Rect Scale(float x0, float y0, float x1, float y1) => new(x0 * sx, y0 * sy, x1 * sx, y1 * sy);

        switch (variant)
        {
            case TowerVariant.Point:
                {
                    Rect[] rects =
                    [
                        Scale(-44f, -44f, -15f, -15f), Scale(15f, -44f, 44f, -15f),
                    Scale(15f, 15f, 44f, 44f), Scale(-44f, 15f, -15f, 44f),
                ];
                    int wingStoreys = RoundStoreys(shaftFloor / rects.Sum(r => r.Area));
                    return [.. rects.Select((rect, i) => Wing($"t{i}", rect, wingStoreys, "banded", crown: true))];
                }
            case TowerVariant.SteppedPoint:
                {
                    (float X, float Y, TowerSide Side, double Share)[] layout =
                    [
                        (-44f, -44f, TowerSide.West, 1.15), (15f, -44f, TowerSide.South, .85),
                    (15f, 15f, TowerSide.East, 1.05), (-44f, 15f, TowerSide.North, .95),
                ];
                    var wings = new List<TowerWing>();
                    for (int i = 0; i < layout.Length; i++)
                    {
                        (float x, float y, TowerSide side, double share) = layout[i];
                        SpineAndFront(wings, $"t{i}", x, y, side, shaftFloor * (float)(share / 4d), sx, sy);
                    }

                    return wings;
                }
            case TowerVariant.L:
                {
                    Rect street = Scale(-48f, -48f, 48f, -28f);
                    Rect back = Scale(-48f, -28f, -28f, 48f);
                    int streetStoreys = RoundStoreys(shaftStoreys * .75);
                    int backStoreys = RoundStoreys((shaftFloor - (street.Area * streetStoreys)) / back.Area);
                    return
                    [
                        Wing("street-arm", street, streetStoreys, "ribbon", crown: true),
                    Wing("back-arm", back, backStoreys, "ribbon", crown: true),
                ];
                }
            case TowerVariant.H:
                {
                    Rect west = Scale(-40f, -40f, -20f, 40f);
                    Rect east = Scale(20f, -40f, 40f, 40f);
                    Rect link = Scale(-20f, -10f, 20f, 10f);
                    int wingStoreys = RoundStoreys(shaftFloor / (west.Area + east.Area + link.Area));
                    return
                    [
                        Wing("west-bar", west, wingStoreys, "curtain", crown: true),
                    Wing("east-bar", east, wingStoreys, "curtain", crown: true),
                    Wing("link", link, wingStoreys, "curtain"),
                ];
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(variant));
        }
    }

    private static void SpineAndFront(List<TowerWing> wings, string name, float referenceX, float referenceY,
        TowerSide spineSide, float target, float sx, float sy)
    {
        float x0 = referenceX * sx, y0 = referenceY * sy;
        float x1 = (referenceX + 29f) * sx, y1 = (referenceY + 29f) * sy;
        float spineX = 17f * sx, spineY = 17f * sy;
        Rect spine;
        Rect front;
        switch (spineSide)
        {
            case TowerSide.West:
                spine = new Rect(x0, y0, x0 + spineX, y1);
                front = new Rect(x0 + spineX, y0, x1, y1);
                break;
            case TowerSide.East:
                spine = new Rect(x1 - spineX, y0, x1, y1);
                front = new Rect(x0, y0, x1 - spineX, y1);
                break;
            case TowerSide.South:
                spine = new Rect(x0, y0, x1, y0 + spineY);
                front = new Rect(x0, y0 + spineY, x1, y1);
                break;
            case TowerSide.North:
                spine = new Rect(x0, y1 - spineY, x1, y1);
                front = new Rect(x0, y0, x1, y1 - spineY);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(spineSide));
        }

        float towerArea = (x1 - x0) * (y1 - y0);
        int spineStoreys = RoundStoreys((target / towerArea) * 1.2);
        int frontStoreys = Math.Max(1, RoundStoreys((target - (spine.Area * spineStoreys)) / front.Area));
        int highStoreys = Math.Max(0, spineStoreys - frontStoreys - 1);
        wings.Add(Wing($"{name}-front", front, frontStoreys, "curtain"));
        wings.Add(Wing($"{name}-spine-low", spine, frontStoreys, "spine"));
        wings.Add(Wing($"{name}-reveal", spine, 1, "reveal", inset: .8f));
        wings.Add(Wing($"{name}-spine-high", spine, highStoreys, "spine", crown: true, soffit: true));
    }

    private static int RoundStoreys(double value) => (int)Math.Round(value, MidpointRounding.ToEven);

    private static TowerWing Wing(string name, Rect rect, int storeys, string style, bool crown = false,
        float inset = 0f, bool soffit = false) => new(name, rect, storeys, style, crown, inset, soffit);

    private static void Stack(List<TowerWing> wings, float @base)
    {
        for (int i = 0; i < wings.Count; i++)
        {
            TowerWing wing = wings[i];
            wing.Z0 = @base;
            for (int belowIndex = i - 1; belowIndex >= 0; belowIndex--)
            {
                TowerWing below = wings[belowIndex];
                if (!below.Rect.Contains(wing.Rect)) continue;
                wing.Z0 = below.Top;
                below.Capped = false;
                break;
            }
        }
    }

    private static void DrawWing(Writer writer, TowerWing wing, List<TowerWing> wings)
    {
        Rect rect = wing.Rect;
        float z0 = wing.Z0, z1 = wing.Top;
        if (wing.Style == "reveal")
        {
            float inset = wing.Inset;
            Box(writer, new Vector3(rect.X0 + inset, rect.Y0 + inset, z0),
                new Vector3(rect.X1 - inset, rect.Y1 - inset, z1), "reveal");
            return;
        }

        foreach (TowerSide sideName in TowerSides)
        {
            TowerFace side = Side(rect, sideName);
            var cuts = new SortedSet<float> { 0f, side.Length };
            foreach (TowerWing other in wings)
            {
                if (ReferenceEquals(other, wing)) continue;
                if (Touching(side, rect, other.Rect) is not { } touch) continue;
                cuts.Add(touch.Low);
                cuts.Add(touch.High);
            }

            float[] positions = [.. cuts];
            for (int i = 0; i + 1 < positions.Length; i++)
            {
                float a = positions[i], b = positions[i + 1];
                var covers = new List<(float Low, float High)>();
                foreach (TowerWing other in wings)
                {
                    if (ReferenceEquals(other, wing) || other.Style == "reveal") continue;
                    if (Touching(side, rect, other.Rect) is not { } touch
                        || touch.Low > a + 1e-4f || b - 1e-4f > touch.High) continue;
                    covers.Add((other.Z0, other.Top));
                }

                foreach ((float low, float high) in Subtract((z0, z1), covers))
                {
                    TowerFacade(writer, side, a, b, low, high, z0, wing.Style);
                    if (wing.Capped && MathF.Abs(high - z1) < 1e-4f)
                    {
                        TowerFacadeParameters look = TowerFacades[wing.Style];
                        writer.Slab(side.Face, a, b, z1, z1 + 1.2f, -look.Out, .3f, look.SpandrelPart);
                        writer.Slab(side.Face, a, b, z1 + 1.2f, z1 + 1.3f, -look.Out - .05f, .35f, "trim");
                    }
                }
            }
        }

        if (wing.Capped)
        {
            Box(writer, new Vector3(rect.X0 + .1f, rect.Y0 + .1f, z1 - .2f),
                new Vector3(rect.X1 - .1f, rect.Y1 - .1f, z1 + .05f), "membrane");
        }

        if (wing.Soffit)
        {
            Box(writer, new Vector3(rect.X0 - .05f, rect.Y0 - .05f, z0),
                new Vector3(rect.X1 + .05f, rect.Y1 + .05f, z0 + .3f), "trim");
        }

        if (wing.Crown) Crown(writer, rect, z1);
    }

    private static void TowerFacade(Writer writer, TowerFace side, float u0, float u1, float z0, float z1,
        float floor0, string style)
    {
        TowerFacadeParameters look = TowerFacades[style];
        Plane(writer, side.Face, "glass", u0, u1, z0, z1, TowerRecess);
        int first = RoundStoreys((z0 - floor0) / Storey);
        int last = RoundStoreys((z1 - floor0) / Storey);
        for (int storey = first; storey < last; storey++)
        {
            float z = floor0 + (storey * Storey);
            writer.Slab(side.Face, u0, u1, z, z + look.Spandrel, -look.Out, TowerRecess + .05f,
                look.SpandrelPart);
        }

        int count = RoundStoreys(side.Length / look.FinEvery);
        for (int i = 1; i < count; i++)
        {
            float u = side.Length * i / count;
            if (!(u0 < u - (look.FinWidth / 2f) && u + (look.FinWidth / 2f) < u1)) continue;
            writer.Slab(side.Face, u - (look.FinWidth / 2f), u + (look.FinWidth / 2f), z0, z1,
                look.FinFront, look.FinBack, look.FinPart);
        }
    }

    private static List<(float Low, float High)> Subtract((float Low, float High) span,
        List<(float Low, float High)> covers)
    {
        List<(float Low, float High)> pieces = [span];
        foreach ((float coverLow, float coverHigh) in covers)
        {
            var left = new List<(float Low, float High)>();
            foreach ((float low, float high) in pieces)
            {
                (float Low, float High)[] candidates =
                [
                    (low, Math.Min(high, coverLow)),
                    (Math.Max(low, coverHigh), high),
                ];
                foreach ((float candidateLow, float candidateHigh) in candidates)
                {
                    if (candidateHigh - candidateLow > 1e-4f) left.Add((candidateLow, candidateHigh));
                }
            }

            pieces = left;
        }

        return pieces;
    }

    private static (float Low, float High)? Touching(TowerFace side, Rect rect, Rect other)
    {
        float otherLine;
        float ownLine;
        switch (side.Side)
        {
            case TowerSide.South: otherLine = other.Y1; ownLine = rect.Y0; break;
            case TowerSide.North: otherLine = other.Y0; ownLine = rect.Y1; break;
            case TowerSide.East: otherLine = other.X0; ownLine = rect.X1; break;
            case TowerSide.West: otherLine = other.X1; ownLine = rect.X0; break;
            default: throw new ArgumentOutOfRangeException(nameof(side));
        }

        if (MathF.Abs(otherLine - ownLine) > 1e-3f) return null;
        float low;
        float high;
        float start;
        if (side.Side is TowerSide.South or TowerSide.North)
        {
            float overlapLow = Math.Max(rect.X0, other.X0), overlapHigh = Math.Min(rect.X1, other.X1);
            if (side.Side == TowerSide.South)
            {
                low = overlapLow;
                high = overlapHigh;
                start = rect.X0;
            }
            else
            {
                low = rect.X1 - overlapHigh;
                high = rect.X1 - overlapLow;
                start = 0f;
            }
        }
        else
        {
            float overlapLow = Math.Max(rect.Y0, other.Y0), overlapHigh = Math.Min(rect.Y1, other.Y1);
            if (side.Side == TowerSide.East)
            {
                low = overlapLow;
                high = overlapHigh;
                start = rect.Y0;
            }
            else
            {
                low = rect.Y1 - overlapHigh;
                high = rect.Y1 - overlapLow;
                start = 0f;
            }
        }

        float u0 = low - start, u1 = high - start;
        return u1 - u0 > 1e-3f ? (u0, u1) : null;
    }

    private static void Crown(Writer writer, Rect rect, float z)
    {
        const float setback = 2.5f, height = 4f;
        Rect screen = new(rect.X0 + setback, rect.Y0 + setback, rect.X1 - setback, rect.Y1 - setback);
        Box(writer, new Vector3(screen.X0 + .3f, screen.Y0 + .3f, z),
            new Vector3(screen.X1 - .3f, screen.Y1 - .3f, z + height - .3f), "spandrel");
        foreach (TowerSide sideName in TowerSides)
        {
            TowerFace side = Side(screen, sideName);
            for (int blade = 0; blade < 7; blade++)
            {
                float v = z + .4f + (blade * .5f);
                writer.Slab(side.Face, 0f, side.Length, v, v + .12f, -.1f, .3f, "metal");
            }

            foreach (float u in (ReadOnlySpan<float>)[0f, side.Length])
            {
                writer.Slab(side.Face, Math.Max(u - .15f, 0f), Math.Min(u + .15f, side.Length),
                    z, z + height, -.1f, .3f, "metal");
            }

            writer.Slab(side.Face, 0f, side.Length, z + height - .3f, z + height, -.15f, .3f, "metal");
        }
    }

    private static void Terrace(Writer writer, Rect source, float z)
    {
        Rect rect = new(source.X0 + .4f, source.Y0 + .4f, source.X1 - .4f, source.Y1 - .4f);
        Box(writer, new Vector3(rect.X0, rect.Y0, z), new Vector3(rect.X1, rect.Y1, z + .2f), "paving");
        bool longX = rect.Width >= rect.Depth;
        for (int edge = 0; edge < 2; edge++)
        {
            if (longX)
            {
                float y = edge == 0 ? rect.Y0 + .6f : rect.Y1 - 1.8f;
                Box(writer, new Vector3(rect.X0 + .6f, y, z + .2f),
                    new Vector3(rect.X1 - .6f, y + 1.2f, z + .9f), "planter");
            }
            else
            {
                float x = edge == 0 ? rect.X0 + .6f : rect.X1 - 1.8f;
                Box(writer, new Vector3(x, rect.Y0 + .6f, z + .2f),
                    new Vector3(x + 1.2f, rect.Y1 - .6f, z + .9f), "planter");
            }
        }

        foreach (float t in (ReadOnlySpan<float>)[.2f, .5f, .8f])
        {
            Vector3 at = longX
                ? new Vector3(rect.X0 + (rect.Width * t), rect.Y0 + 1.2f, z + 3.1f)
                : new Vector3(rect.X0 + 1.2f, rect.Y0 + (rect.Depth * t), z + 3.1f);
            Tree(writer, at, 1.6f);
        }
    }

    private static void Podium(Writer writer, float frontage, float depth, int storeys, List<TowerWing> wings)
    {
        if (storeys <= 0) return;
        float halfX = frontage / 2f, halfY = depth / 2f;
        Rect rect = new(-halfX, -halfY, halfX, halfY);
        float top = storeys * Storey;
        Box(writer, new Vector3(-halfX - .05f, -halfY - .05f, 0f),
            new Vector3(halfX + .05f, halfY + .05f, .3f), "plinth");
        foreach (TowerSide sideName in TowerSides)
        {
            TowerFace side = Side(rect, sideName);
            Plane(writer, side.Face, "glass", 0f, side.Length, .3f, 3f, TowerRecess + .1f);
            int mullions = RoundStoreys(side.Length / 3f);
            for (int i = 1; i < mullions; i++)
            {
                float u = side.Length * i / mullions;
                writer.Slab(side.Face, u - .05f, u + .05f, .3f, 3f, TowerRecess, TowerRecess + .1f, "frame");
            }

            int piers = Math.Max(1, RoundStoreys(side.Length / 12f));
            for (int i = 0; i <= piers; i++)
            {
                float u = Math.Min(Math.Max(side.Length * i / piers, .4f), side.Length - .4f);
                writer.Slab(side.Face, u - .4f, u + .4f, .3f, 3f, 0f, TowerRecess + .1f, "wall-end");
            }

            writer.Slab(side.Face, 0f, side.Length, 3f, Storey, -.15f, TowerRecess + .1f, "trim");
            if (storeys > 1) TowerFacade(writer, side, 0f, side.Length, Storey, top, Storey, "podium");
            writer.Slab(side.Face, 0f, side.Length, top, top + 1.1f, 0f, .3f, "wall-end");
            writer.Slab(side.Face, 0f, side.Length, top + 1.1f, top + 1.2f, -.05f, .35f, "trim");
            foreach (float u in (ReadOnlySpan<float>)[side.Length * .25f, side.Length * .75f])
            {
                writer.Slab(side.Face, u - 3f, u + 3f, 3.1f, 3.25f, -2.2f, 0f, "trim");
                writer.Slab(side.Face, u - 1.2f, u + 1.2f, .3f, 2.6f,
                    TowerRecess - .05f, TowerRecess + .05f, "door");
            }
        }

        Box(writer, new Vector3(-halfX + .3f, -halfY + .3f, top - .2f),
            new Vector3(halfX - .3f, halfY - .3f, top), "membrane");
        Box(writer, new Vector3(-halfX + .3f, -halfY + .3f, top),
            new Vector3(halfX - .3f, halfY - .3f, top + .15f), "paving");
        Rect ring = new(-halfX + 2f, -halfY + 2f, halfX - 2f, halfY - 2f);
        foreach (TowerSide sideName in TowerSides)
        {
            TowerFace side = Side(ring, sideName);
            writer.Slab(side.Face, 2f, side.Length - 2f, top + .15f, top + .85f, 0f, 1.4f, "planter");
            for (int i = 1; i < 12; i++)
            {
                float u = side.Length * i / 12f;
                Vector3 at = side.Face.Point(u, top + 3f, .7f);
                if (wings.Any(w => w.Rect.Near(at.X, at.Y, 3f))) continue;
                Tree(writer, at, 1.8f);
            }
        }
    }

    private static void Tree(Writer writer, Vector3 centre, float radius)
    {
        Vector3 top = centre + new Vector3(0f, 0f, radius);
        Vector3 bottom = centre - new Vector3(0f, 0f, radius);
        Vector3 east = centre + new Vector3(radius, 0f, 0f);
        Vector3 north = centre + new Vector3(0f, radius, 0f);
        Vector3 west = centre - new Vector3(radius, 0f, 0f);
        Vector3 south = centre - new Vector3(0f, radius, 0f);
        writer.Polygon("tree", [top, east, north]);
        writer.Polygon("tree", [top, north, west]);
        writer.Polygon("tree", [top, west, south]);
        writer.Polygon("tree", [top, south, east]);
        writer.Polygon("tree", [bottom, north, east]);
        writer.Polygon("tree", [bottom, west, north]);
        writer.Polygon("tree", [bottom, south, west]);
        writer.Polygon("tree", [bottom, east, south]);
    }

    private static void Plane(Writer writer, Face face, string part, float u0, float u1, float z0, float z1,
        float depth) => writer.Polygon(part,
        [face.Point(u0, z0, depth), face.Point(u1, z0, depth), face.Point(u1, z1, depth), face.Point(u0, z1, depth)]);

    private static void Box(Writer writer, Vector3 low, Vector3 high, string part)
    {
        if (high.X - low.X < 1e-4f || high.Y - low.Y < 1e-4f || high.Z - low.Z < 1e-4f) return;
        writer.Box(low, high, part);
    }

    private static TowerFace Side(Rect rect, TowerSide side) => side switch
    {
        TowerSide.South => new(side, new Face(new Vector3(rect.X0, rect.Y0, 0f), Vector3.UnitX, -Vector3.UnitY), rect.Width),
        TowerSide.East => new(side, new Face(new Vector3(rect.X1, rect.Y0, 0f), Vector3.UnitY, Vector3.UnitX), rect.Depth),
        TowerSide.North => new(side, new Face(new Vector3(rect.X1, rect.Y1, 0f), -Vector3.UnitX, Vector3.UnitY), rect.Width),
        TowerSide.West => new(side, new Face(new Vector3(rect.X0, rect.Y1, 0f), -Vector3.UnitY, -Vector3.UnitX), rect.Depth),
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static readonly TowerSide[] TowerSides =
        [TowerSide.South, TowerSide.East, TowerSide.North, TowerSide.West];

    private enum TowerSide : byte
    {
        South,
        East,
        North,
        West,
    }

    private readonly record struct Rect(float X0, float Y0, float X1, float Y1)
    {
        public float Width => X1 - X0;

        public float Depth => Y1 - Y0;

        public float Area => Width * Depth;

        public bool Contains(Rect other) =>
            X0 - 1e-3f <= other.X0 && other.X1 <= X1 + 1e-3f
            && Y0 - 1e-3f <= other.Y0 && other.Y1 <= Y1 + 1e-3f;

        public bool Near(float x, float y, float margin) =>
            X0 - margin < x && x < X1 + margin && Y0 - margin < y && y < Y1 + margin;
    }

    private readonly record struct TowerFace(TowerSide Side, Face Face, float Length);

    private sealed class TowerWing(
        string name,
        Rect rect,
        int storeys,
        string style,
        bool crown,
        float inset,
        bool soffit)
    {
        public string Name { get; } = name;

        public Rect Rect { get; } = rect;

        public int Storeys { get; } = storeys;

        public string Style { get; } = style;

        public bool Crown { get; } = crown;

        public float Inset { get; } = inset;

        public bool Soffit { get; } = soffit;

        public float Z0 { get; set; }

        public float Top => Z0 + (Storeys * Storey);

        public bool Capped { get; set; } = true;
    }

    private sealed record TowerPlan(int PodiumStoreys, float ShaftFloor, List<TowerWing> Wings);
}
