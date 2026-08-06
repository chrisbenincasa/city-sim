using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Routing;

/// <summary>
/// The arithmetic of entering and leaving the network at an Access Point, shared by every searcher
/// in this spike.
/// </summary>
/// <remarks>
/// <para>
/// <b>Extracted by R3, and the reason is the one <see cref="PointToPoint"/> already states about
/// Dijkstra.</b> That class argues against a second implementation of the search because <i>"two
/// implementations could disagree for reasons that had nothing to do with the heuristic"</i>. R3
/// compares a hierarchy against the flat search on the same queries and publishes the difference as
/// a detour, so the same argument applies with more force: a second copy of the
/// <c>(Segment, offset)</c> arithmetic would put a second candidate explanation under every
/// non-zero figure in the correctness column.
/// </para>
/// <para>
/// Nothing here is new. Every method is <see cref="PointToPoint"/>'s, unchanged, with the mode and
/// the congested cost array passed rather than read from a field.
/// </para>
/// </remarks>
internal static class SegmentEntry
{
    /// <summary>
    /// Larger than any real path cost and small enough that <c>g + h</c> cannot overflow. See
    /// <see cref="PointToPoint"/> for why it is spelled as a shift.
    /// </summary>
    public const int Unreachable = 1 << 29;

    /// <summary>The cost of traversing a whole arc, congested if costs were supplied.</summary>
    public static int ArcCost(RoadGraph graph, int[]? congestedCarTicks, Modes mode, int arc) =>
        mode == Modes.Foot ? graph.ArcFootTicks[arc]
        : congestedCarTicks is null ? graph.ArcCarTicks[arc]
        : congestedCarTicks[arc];

    /// <summary>The arc leaving <paramref name="node"/> along <paramref name="segment"/>, or -1.</summary>
    public static int ArcAlong(RoadGraph graph, int node, int segment)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            if (graph.ArcSegment[arc] == segment)
            {
                return arc;
            }
        }

        return -1;
    }

    /// <summary>
    /// The cost of the partial run between a Segment's endpoint and a point <paramref name="tiles"/>
    /// along it, travelling <i>away from</i> <paramref name="from"/>.
    /// </summary>
    public static int PartialCost(
        RoadGraph graph, int[]? congestedCarTicks, Modes mode, int segment, int from, int tiles)
    {
        int arc = ArcAlong(graph, from, segment);
        if (arc < 0)
        {
            return Unreachable;
        }

        int step = ArcCost(graph, congestedCarTicks, mode, arc);
        if (step == RoadGraph.Impassable)
        {
            return Unreachable;
        }

        int free = RoadGraph.TraversalTicks(
            tiles, graph.SegmentFreeFlow[segment], mode, graph.ArcModes[arc]);

        if (congestedCarTicks is null || mode == Modes.Foot || free == 0)
        {
            return free;
        }

        // Congestion applies to the partial run at the same rate as to the whole arc — see
        // PointToPoint.PartialCost for why the delay is taken from the arc's own two costs rather
        // than re-derived from volume.
        int whole = graph.ArcCarTicks[arc];
        return whole <= 0 ? free : Fixed.Mul(free, Fixed.Div(step, whole));
    }

    /// <summary>
    /// The cost of never leaving the Segment, when origin and goal share one.
    /// </summary>
    /// <remarks>
    /// <b>This is correctness, and R3's same-Segment <i>bypass</i> is a different thing built on
    /// it.</b> Without this a search whose goal is behind its origin on the same run of road
    /// reports the cost of driving round the block. The bypass is the decision not to enter the
    /// abstract graph at all once this returns a finite answer.
    /// </remarks>
    public static int SameSegmentCost(
        RoadGraph graph, int[]? congestedCarTicks, Modes mode, AccessPoint origin, AccessPoint goal)
    {
        if (origin.Segment != goal.Segment)
        {
            return Unreachable;
        }

        if (origin.OffsetTiles == goal.OffsetTiles)
        {
            return 0;
        }

        bool forward = goal.OffsetTiles > origin.OffsetTiles;
        int from = forward ? graph.SegmentNodeA[origin.Segment] : graph.SegmentNodeB[origin.Segment];
        int tiles = forward
            ? goal.OffsetTiles - origin.OffsetTiles
            : origin.OffsetTiles - goal.OffsetTiles;

        return PartialCost(graph, congestedCarTicks, mode, origin.Segment, from, tiles);
    }

    /// <summary>
    /// The cost from an Access Point to one of its own Segment's endpoints, or
    /// <see cref="Unreachable"/> if <paramref name="node"/> is not an endpoint of it.
    /// </summary>
    /// <remarks>
    /// The direction is what makes this non-obvious: to reach node A from a point on the Segment is
    /// to travel in the B→A direction, so the arc that must permit the mode is the one leaving B.
    /// The asymmetry is the whole reason the graph is directed.
    /// </remarks>
    public static int CostToEndpoint(
        RoadGraph graph, int[]? congestedCarTicks, Modes mode, AccessPoint point, int node)
    {
        int a = graph.SegmentNodeA[point.Segment];
        int b = graph.SegmentNodeB[point.Segment];
        int length = graph.SegmentLengthTiles[point.Segment];

        if (node == a)
        {
            return PartialCost(graph, congestedCarTicks, mode, point.Segment, b, point.OffsetTiles);
        }

        if (node == b)
        {
            return PartialCost(
                graph, congestedCarTicks, mode, point.Segment, a, length - point.OffsetTiles);
        }

        return Unreachable;
    }

    /// <summary>
    /// The cost from one of a Segment's endpoints to an Access Point on it — the goal remainder.
    /// </summary>
    public static int CostFromEndpoint(
        RoadGraph graph, int[]? congestedCarTicks, Modes mode, int node, AccessPoint point)
    {
        int a = graph.SegmentNodeA[point.Segment];
        int b = graph.SegmentNodeB[point.Segment];
        int length = graph.SegmentLengthTiles[point.Segment];

        if (node == a)
        {
            return PartialCost(graph, congestedCarTicks, mode, point.Segment, a, point.OffsetTiles);
        }

        if (node == b)
        {
            return PartialCost(
                graph, congestedCarTicks, mode, point.Segment, b, length - point.OffsetTiles);
        }

        return Unreachable;
    }
}
