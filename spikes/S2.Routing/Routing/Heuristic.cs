using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Routing;

/// <summary>The rungs of the heuristic ladder. <c>plans/0010</c> R0 requires all four measured.</summary>
internal enum HeuristicKind
{
    /// <summary>No heuristic. Plain Dijkstra, and the ground truth every other rung is judged against.</summary>
    None,

    /// <summary><c>|dx| + |dy|</c>. Exact on a 4-connected grid; an <b>over</b>estimate off it.</summary>
    Manhattan,

    /// <summary><c>max + (√2−1)·min</c>. Exact on an 8-connected grid; an <b>over</b>estimate off it.</summary>
    Octile,

    /// <summary><c>max(|dx|, |dy|)</c>. A lower bound on Euclidean by construction, on any graph.</summary>
    Chebyshev,

    /// <summary>The exact floor of the Euclidean distance. A lower bound on any path, on any graph.</summary>
    EuclideanFloor,
}

/// <summary>
/// The heuristic ladder, and the reason it is a ladder rather than a choice.
/// </summary>
/// <remarks>
/// <para>
/// <b>A distance heuristic on a time-cost graph is admissible only when divided by the map's maximum
/// free-flow speed</b>, and <c>plans/0010</c> names the consequence before any measurement: since
/// Arterials are the fast edges on a network that is mostly slow Streets, <i>"that division makes the
/// tightest-looking heuristic nearly uninformative where most of the graph is."</i> A car search is
/// divided by the Arterial's 90 km/h while nearly every edge it expands is a 50 km/h Street, so the
/// bound is loose by 1.8× almost everywhere. That is not a defect in the implementation; it is the
/// price of routing on time, and R0 exists partly to price it.
/// </para>
/// <para>
/// <b>Two of the four rungs are not safely admissible and the generator is built to expose it.</b>
/// <c>adr/0014</c> snaps Streets to the Tile grid, so Manhattan is exact there — but Arterials are
/// freeform at arbitrary angles, so a diagonal Arterial makes the true network distance
/// <i>shorter</i> than Manhattan, the heuristic overestimates, and A* silently returns a non-optimal
/// path. <c>CONTEXT.md</c> → Arterial calls them <i>"deliberately rare"</i>, which makes the tight
/// metrics admissible <i>almost</i> always — and almost always is the trap, because <b>a non-optimal
/// path is a different Trip and therefore a different city.</b>
/// </para>
/// <para>
/// <b>None of them needs a square root except the one that is exact</b>, and that one uses the
/// spike's own integer implementation. Nothing enters the substrate.
/// </para>
/// </remarks>
internal static class Heuristic
{
    /// <summary><c>(√2 − 1)</c> in Q16.16, for Octile. 0.41421356 × 65536 = 27,146.</summary>
    private const int OctileDiagonal = 27_146;

    /// <summary>
    /// The metric's estimate of the distance between two Tile coordinates, in Tiles.
    /// </summary>
    public static int Distance(HeuristicKind kind, int ax, int ay, int bx, int by)
    {
        int dx = IntegerMath.Abs(ax - bx);
        int dy = IntegerMath.Abs(ay - by);

        int low = dx < dy ? dx : dy;
        int high = dx < dy ? dy : dx;

        return kind switch
        {
            HeuristicKind.None => 0,
            HeuristicKind.Manhattan => dx + dy,
            HeuristicKind.Octile => high + ((low * OctileDiagonal) >> 16),
            HeuristicKind.Chebyshev => high,
            HeuristicKind.EuclideanFloor => IntegerGeometry.SqrtFloor(((long)dx * dx) + ((long)dy * dy)),
            _ => 0,
        };
    }

    /// <summary>
    /// Ticks per Tile at the fastest speed available to the mode — the heuristic's divisor, inverted
    /// once per query so the inner loop never divides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is where routing on time is actually expensive, and it was not obvious until it was
    /// measured.</b> Dividing a distance by a speed is what makes a distance metric a lower bound on
    /// a <i>time</i> — but the search evaluates <c>h</c> against <b>both</b> goal endpoints for every
    /// node it pushes, and <c>Fixed.Div</c> routes through <c>IntegerMath.FloorDiv</c>, which costs a
    /// <c>/</c> and a <c>%</c>. That is <b>four 64-bit hardware divisions per node</b>, and in the
    /// first capture it was most of the denominator: 231 ns per expansion, against a CSR walk that
    /// should cost tens.
    /// </para>
    /// <para>
    /// Inverting once per query turns each of those into one multiply. <b>It stays admissible</b>,
    /// which is the only property that matters here: the reciprocal is floored and the product is
    /// floored, so both roundings go down and the estimate can only under-shoot. It costs about two
    /// parts in ten thousand of tightness, which the ladder's expansion counts can see and its
    /// optimality column cannot.
    /// </para>
    /// <para>
    /// <b>Worth recording beyond this spike.</b> The corpus commits to a time-valued cost function on
    /// the SC4 argument, and this is the first measurement of what that commitment costs in the inner
    /// loop. The answer is *nothing, if you invert once per query, and a great deal if you do not*.
    /// </para>
    /// </remarks>
    public static int TicksPerTile(Modes mode) => Fixed.Div(Fixed.One, MaximumSpeed(mode));

    /// <summary>The estimated travel time, Q16.16 Ticks, for a distance in Tiles.</summary>
    public static int Ticks(int distanceTiles, int ticksPerTile) =>
        distanceTiles == 0 ? 0 : Fixed.Mul(Fixed.FromInt(distanceTiles), ticksPerTile);

    /// <summary>The fastest a mode may move anywhere on this map. The heuristic's divisor.</summary>
    public static int MaximumSpeed(Modes mode) =>
        mode == Modes.Foot ? Units.WalkFreeFlow : Units.ArterialFreeFlow;
}
