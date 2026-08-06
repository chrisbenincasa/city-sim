using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Routing;

/// <summary>The result of one search.</summary>
/// <param name="Found">Whether a route exists at all. For a walk this is <b>Severance</b>.</param>
/// <param name="CostTicks">Total travel time, Q16.16 Ticks, including both offset remainders.</param>
/// <param name="NodesExpanded">Nodes settled. The work figure every ratio in S2 divides by.</param>
/// <param name="ArcsRelaxed">Arcs examined.</param>
/// <param name="PathSegments">Segments in the returned route, excluding the two partial ends.</param>
internal readonly record struct SearchOutcome(
    bool Found,
    int CostTicks,
    int NodesExpanded,
    int ArcsRelaxed,
    int PathSegments);

/// <summary>
/// R0's denominator: <i>"one uncached point-to-point search — A*, integer costs, no hierarchy and no
/// cache."</i> Every later figure in S2 is reported against this, never against a Tick budget.
/// </summary>
/// <remarks>
/// <para>
/// <b>Dijkstra is not a second implementation.</b> <see cref="HeuristicKind.None"/> makes this same
/// loop plain Dijkstra, which is what lets R0 report the two things the plan asks for on <i>the same
/// query</i>: the ratio of A*'s expansions to Dijkstra's — the denominator's own quality — and
/// whether A*'s returned path cost matches Dijkstra's, which is the admissibility column. Two
/// implementations could disagree for reasons that had nothing to do with the heuristic.
/// </para>
/// <para>
/// <b>The heuristic targets both goal endpoints, and that is what makes it admissible at all.</b> The
/// goal is a point <i>interior</i> to a Segment, so the only ways to reach it are through one of that
/// Segment's two nodes. Therefore
/// <c>cost(n → goal) = min over endpoints E of [cost(n → E) + remainder(E)]</c>, and bounding each
/// term below gives an admissible <c>h</c>. Interpolating a coordinate for the goal point and
/// measuring to that instead would have been wrong on exactly the Segments that matter — a freeform
/// Arterial's length is its arc length, so a fraction along it is not a fraction of the way between
/// its endpoints.
/// </para>
/// <para>
/// <b>Bootstrap and expansion are separate methods so the harness can time them apart.</b> R0 owes
/// that split: a node-to-node denominator measures a query the game never issues, and once the real
/// query shape is restored, later tasks need to tell fixed overhead from work.
/// </para>
/// </remarks>
internal sealed class PointToPoint
{
    /// <summary>
    /// Larger than any real path cost and small enough that <c>g + h</c> cannot overflow.
    /// </summary>
    /// <remarks>
    /// <c>2²⁹</c> Q16.16 Ticks is 8,192 Ticks — a whole Day — against a walk across the entire map
    /// at 1,582 Ticks, so nothing real comes near it; and two of them still fit in an <c>int</c>, so
    /// the <c>g + h</c> of a node whose heuristic is unreachable cannot wrap into a small number and
    /// sort to the front of the heap. Spelled as a shift rather than as <c>int.MaxValue / 4</c>
    /// because <c>BOR0203</c> is loaded here and is right to be: a constant-folded division is still
    /// a division whose rounding nobody stated.
    /// </remarks>
    private const int Unreachable = 1 << 29;

    private readonly RoadGraph _graph;

    private readonly int[] _cost;
    private readonly int[] _touched;
    private readonly int[] _closed;
    private readonly int[] _cameFromArc;

    private int[] _heapKey;
    private int[] _heapNode;
    private int _heapCount;

    private int _generation;

    // Bootstrap state, carried from Bootstrap into Expand.
    private Modes _mode;
    private HeuristicKind _kind;
    private int _ticksPerTile;
    private int _goalNodeA;
    private int _goalNodeB;
    private int _remainderA;
    private int _remainderB;
    private int _directCost;

    /// <summary>
    /// Congested car costs per arc, or <c>null</c> for R0's free-flow graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Added by R1, and it changes no figure R0 published.</b> R0's denominator ran against the
    /// free-flow graph and still does — this field is null on that path and every branch below reduces
    /// to what it was. What R1 needs is the same search over the arc costs the matrix was built on,
    /// because <b>the matrix's error must be measured against the query the game issues on the
    /// network the matrix describes</b>; comparing a congested matrix entry to a free-flow search
    /// would report the congestion as the abstraction's error.
    /// </para>
    /// <para>
    /// <b>The heuristic stays admissible without being touched.</b> It is a lower bound on the
    /// <i>free-flow</i> time, and congestion only ever increases an arc's cost — BPR's multiplier is
    /// <c>1 + α(v/c)^β ≥ 1</c> — so a bound on the cheaper graph is a bound on this one. R0's
    /// `Chebyshev` verdict therefore carries over unchanged, and did not need re-deriving.
    /// </para>
    /// </remarks>
    private readonly int[]? _congestedCarTicks;

    public PointToPoint(RoadGraph graph, int[]? congestedCarTicks = null)
    {
        _graph = graph;
        _congestedCarTicks = congestedCarTicks;
        _cost = new int[graph.Nodes];
        _touched = new int[graph.Nodes];
        _closed = new int[graph.Nodes];
        _cameFromArc = new int[graph.Nodes];
        _heapKey = new int[1024];
        _heapNode = new int[1024];
    }

    /// <summary>The cost of traversing a whole arc, congested if this search was given costs.</summary>
    private int ArcCost(int arc) =>
        _mode == Modes.Foot ? _graph.ArcFootTicks[arc]
        : _congestedCarTicks is null ? _graph.ArcCarTicks[arc]
        : _congestedCarTicks[arc];

    /// <summary>
    /// Seeds the open set from the origin Access Point and resolves the goal's two remainders.
    /// </summary>
    /// <remarks>
    /// Generation stamping rather than clearing: at thousands of queries over a 66,000-node graph, a
    /// per-query <c>Array.Clear</c> would be most of the measurement. The counter makes the reset
    /// <c>O(1)</c> and it is the same trick the core uses for the same reason.
    /// </remarks>
    public void Bootstrap(AccessPoint origin, AccessPoint goal, Modes mode, HeuristicKind kind)
    {
        _generation++;
        _heapCount = 0;
        _mode = mode;
        _kind = kind;
        _ticksPerTile = Heuristic.TicksPerTile(mode);

        _goalNodeA = _graph.SegmentNodeA[goal.Segment];
        _goalNodeB = _graph.SegmentNodeB[goal.Segment];

        int goalLength = _graph.SegmentLengthTiles[goal.Segment];
        _remainderA = PartialCost(goal.Segment, _goalNodeA, goal.OffsetTiles);
        _remainderB = PartialCost(goal.Segment, _goalNodeB, goalLength - goal.OffsetTiles);

        _directCost = SameSegmentCost(origin, goal);

        int originA = _graph.SegmentNodeA[origin.Segment];
        int originB = _graph.SegmentNodeB[origin.Segment];
        int originLength = _graph.SegmentLengthTiles[origin.Segment];

        // To reach node A from a point on the Segment is to travel in the B→A direction, so the
        // arc that must permit the mode is the one leaving B. The asymmetry is the whole reason
        // the graph is directed.
        Seed(originA, PartialCost(origin.Segment, originB, origin.OffsetTiles));
        Seed(originB, PartialCost(origin.Segment, originA, originLength - origin.OffsetTiles));
    }

    /// <summary>Runs the search. Call after <see cref="Bootstrap"/>.</summary>
    public SearchOutcome Expand()
    {
        int best = _directCost;
        int bestEndpoint = -1;
        int expanded = 0;
        int relaxed = 0;

        while (_heapCount > 0)
        {
            (int key, int node) = Pop();

            if (key >= best)
            {
                break;
            }

            if (_closed[node] == _generation)
            {
                continue;
            }

            _closed[node] = _generation;
            expanded++;

            int here = _cost[node];

            if (node == _goalNodeA && _remainderA < Unreachable && here + _remainderA < best)
            {
                best = here + _remainderA;
                bestEndpoint = node;
            }

            if (node == _goalNodeB && _remainderB < Unreachable && here + _remainderB < best)
            {
                best = here + _remainderB;
                bestEndpoint = node;
            }

            for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
            {
                relaxed++;

                int step = ArcCost(arc);
                if (step == RoadGraph.Impassable)
                {
                    continue;
                }

                int next = _graph.ArcTarget[arc];
                if (_closed[next] == _generation)
                {
                    continue;
                }

                int candidate = here + step;
                if (_touched[next] == _generation && _cost[next] <= candidate)
                {
                    continue;
                }

                _cost[next] = candidate;
                _touched[next] = _generation;
                _cameFromArc[next] = arc;

                int estimate = Estimate(next);
                if (estimate < Unreachable)
                {
                    Push(candidate + estimate, next);
                }
            }
        }

        if (best >= Unreachable)
        {
            return new SearchOutcome(Found: false, 0, expanded, relaxed, 0);
        }

        return new SearchOutcome(
            Found: true,
            CostTicks: best,
            NodesExpanded: expanded,
            ArcsRelaxed: relaxed,
            PathSegments: CountPath(bestEndpoint));
    }

    // --- The query shape's own arithmetic ---------------------------------------------------------

    /// <summary>
    /// The cost of the partial run between a Segment's endpoint and a point <paramref name="tiles"/>
    /// along it, travelling <i>away from</i> <paramref name="from"/>.
    /// </summary>
    private int PartialCost(int segment, int from, int tiles)
    {
        int arc = ArcAlong(from, segment);
        if (arc < 0)
        {
            return Unreachable;
        }

        int step = ArcCost(arc);
        if (step == RoadGraph.Impassable)
        {
            return Unreachable;
        }

        int free = RoadGraph.TraversalTicks(
            tiles, _graph.SegmentFreeFlow[segment], _mode, _graph.ArcModes[arc]);

        if (_congestedCarTicks is null || _mode == Modes.Foot || free == 0)
        {
            return free;
        }

        // Congestion applies to the partial run at the same rate as to the whole arc. Taking the
        // delay from the arc's own two costs rather than re-deriving it from volume keeps the
        // bootstrap consistent with the search by construction: a partial traversal of a jammed
        // Segment must not be priced at free flow, or the query shape would understate exactly the
        // Segments the VDF exists to describe.
        int whole = _graph.ArcCarTicks[arc];
        return whole <= 0 ? free : Fixed.Mul(free, Fixed.Div(step, whole));
    }

    /// <summary>
    /// The cost of never leaving the Segment, when origin and goal share one. Not the same-Segment
    /// <i>bypass</i> R3 owes — that is an optimisation over the abstract graph. This is correctness:
    /// without it a search whose goal is behind its origin on the same run of road would report the
    /// cost of driving round the block.
    /// </summary>
    private int SameSegmentCost(AccessPoint origin, AccessPoint goal)
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
        int from = forward ? _graph.SegmentNodeA[origin.Segment] : _graph.SegmentNodeB[origin.Segment];
        int tiles = forward
            ? goal.OffsetTiles - origin.OffsetTiles
            : origin.OffsetTiles - goal.OffsetTiles;

        return PartialCost(origin.Segment, from, tiles);
    }

    private int ArcAlong(int node, int segment)
    {
        for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
        {
            if (_graph.ArcSegment[arc] == segment)
            {
                return arc;
            }
        }

        return -1;
    }

    private void Seed(int node, int cost)
    {
        if (cost >= Unreachable)
        {
            return;
        }

        if (_touched[node] == _generation && _cost[node] <= cost)
        {
            return;
        }

        _cost[node] = cost;
        _touched[node] = _generation;
        _cameFromArc[node] = -1;

        int estimate = Estimate(node);
        if (estimate < Unreachable)
        {
            Push(cost + estimate, node);
        }
    }

    /// <summary>
    /// <c>h(n) = min over goal endpoints E of [ distance(n, E) / maxSpeed + remainder(E) ]</c>.
    /// </summary>
    private int Estimate(int node)
    {
        if (_kind == HeuristicKind.None)
        {
            return 0;
        }

        int x = _graph.NodeX[node];
        int y = _graph.NodeY[node];

        int viaA = _remainderA >= Unreachable
            ? Unreachable
            : Heuristic.Ticks(
                Heuristic.Distance(_kind, x, y, _graph.NodeX[_goalNodeA], _graph.NodeY[_goalNodeA]),
                _ticksPerTile) + _remainderA;

        int viaB = _remainderB >= Unreachable
            ? Unreachable
            : Heuristic.Ticks(
                Heuristic.Distance(_kind, x, y, _graph.NodeX[_goalNodeB], _graph.NodeY[_goalNodeB]),
                _ticksPerTile) + _remainderB;

        return viaA < viaB ? viaA : viaB;
    }

    private int CountPath(int endpoint)
    {
        if (endpoint < 0)
        {
            return 0;
        }

        int segments = 0;
        int node = endpoint;
        while (_cameFromArc[node] >= 0)
        {
            int arc = _cameFromArc[node];
            segments++;

            int target = _graph.ArcTarget[arc];
            int a = _graph.SegmentNodeA[_graph.ArcSegment[arc]];
            int b = _graph.SegmentNodeB[_graph.ArcSegment[arc]];
            node = target == a ? b : a;
        }

        return segments;
    }

    // --- Binary heap. Lazy deletion; a duplicate is cheaper than a decrease-key. -------------------

    private void Push(int key, int node)
    {
        if (_heapCount == _heapKey.Length)
        {
            Array.Resize(ref _heapKey, _heapKey.Length * 2);
            Array.Resize(ref _heapNode, _heapNode.Length * 2);
        }

        int i = _heapCount++;
        _heapKey[i] = key;
        _heapNode[i] = node;

        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (_heapKey[parent] <= _heapKey[i])
            {
                break;
            }

            Swap(parent, i);
            i = parent;
        }
    }

    private (int Key, int Node) Pop()
    {
        (int key, int node) = (_heapKey[0], _heapNode[0]);

        _heapCount--;
        _heapKey[0] = _heapKey[_heapCount];
        _heapNode[0] = _heapNode[_heapCount];

        int i = 0;
        while (true)
        {
            int left = (i << 1) + 1;
            if (left >= _heapCount)
            {
                break;
            }

            int smallest = left;
            int right = left + 1;
            if (right < _heapCount && _heapKey[right] < _heapKey[left])
            {
                smallest = right;
            }

            if (_heapKey[i] <= _heapKey[smallest])
            {
                break;
            }

            Swap(i, smallest);
            i = smallest;
        }

        return (key, node);
    }

    private void Swap(int a, int b)
    {
        (_heapKey[a], _heapKey[b]) = (_heapKey[b], _heapKey[a]);
        (_heapNode[a], _heapNode[b]) = (_heapNode[b], _heapNode[a]);
    }
}
