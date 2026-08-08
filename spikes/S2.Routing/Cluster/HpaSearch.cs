using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Cluster;

/// <summary>Which shortcut, if any, answered a query without entering the abstract graph.</summary>
internal enum Bypass
{
    /// <summary>None. The query was answered by the hierarchy.</summary>
    None,

    /// <summary>Origin and goal share a Segment. The answer is one partial traversal.</summary>
    SameSegment,

    /// <summary>The Segments meet at a node. The answer is two partial traversals through it.</summary>
    AdjacentSegment,
}

/// <param name="Found">Whether a route exists at all.</param>
/// <param name="CostTicks">Travel time, Q16.16 Ticks, including both offset remainders.</param>
/// <param name="Bypass">Which shortcut answered it, if any.</param>
/// <param name="NodesExpanded">Concrete nodes settled — both insertions.</param>
/// <param name="PortalsExpanded">Abstract nodes settled.</param>
/// <param name="ArcsRelaxed">Concrete arcs examined — both insertions.</param>
/// <param name="EdgesRelaxed">Abstract edges examined. The density the abstraction bought.</param>
internal readonly record struct HpaOutcome(
    bool Found,
    int CostTicks,
    Bypass Bypass,
    int NodesExpanded,
    int PortalsExpanded,
    int ArcsRelaxed,
    int EdgesRelaxed);

/// <summary>
/// HPA\* over the cluster grid: insert the origin and the goal, search the abstract graph, and — on
/// demand — refine the abstract path back down to arcs.
/// </summary>
/// <remarks>
/// <para>
/// <b>The query shape is <c>(Segment, offset) → (Segment, offset)</c>, exactly as the denominator's
/// is.</b> Inserting an Access Point is not inserting a node: the origin enters through both
/// endpoints of its own Segment at their partial costs, and the goal is left through either of its
/// own endpoints plus the remainder. The arithmetic is <see cref="SegmentEntry"/>'s, shared with the
/// flat search, so a detour reported here is the hierarchy's and not a second implementation's.
/// </para>
/// <para>
/// <b>The two bypasses are mandatory rather than an optimisation</b> (<c>plans/0010</c> R3): with
/// five Buildings on a Segment, a share of walk Legs never leave their own Segment or its neighbour,
/// and routing those through the abstract graph costs more than the answer. They are also the first
/// figure the corpus has on how local a walk Leg is — reported as a curve against origin-destination
/// distance, because S2 still has no Leg distribution to weight it with and R0's precedent is that
/// inventing one bakes a guess in where a measurement will look like it stood.
/// </para>
/// <para>
/// <b>Refinement is separated from the query and timed apart</b>, because the two have different
/// customers. A cost alone answers nothing the travel-time matrix does not already answer more
/// cheaply (R1); what <c>adr/0041</c> needs every Tick is a <b>next Segment</b>, which falls out of
/// the origin insertion for free because that insertion is a concrete search. The full arc sequence
/// is a third thing again, and it is the one R2's rungs are priced against.
/// </para>
/// </remarks>
internal sealed class HpaSearch
{
    public const int Unreachable = ClusterSearch.Unreachable;

    private readonly RoadGraph _graph;
    private readonly Clusters _clusters;
    private readonly AbstractGraph _abstract;
    private readonly int[] _arcCost;

    // The cost basis the two Access Point remainders are priced against. This is _arcCost, and the
    // only reason it is a separate field is that it may deliberately be null: R5.5 found every
    // remainder call passing null, which reads graph.ArcCarTicks — the pristine array — while a storm
    // deletes into a shadow clone, so the hierarchy returned routes down bulldozed roads. Retained as
    // a selectable mode so the repair has a control that moves, per Unroutable being worthless as a
    // zero nobody has seen non-zero.
    private readonly int[]? _entryCost;

    private readonly ClusterSearch _forward;
    private readonly ClusterSearch _backward;
    private readonly ClusterSearch _refine;

    private readonly int[] _cost;
    private readonly int[] _touched;
    private readonly int[] _closed;
    private readonly int[] _fromEdge;

    private readonly List<int> _portalPath = [];

    private int[] _heapKey;
    private int[] _heapPortal;
    private int _heapCount;
    private int _generation;

    // Query state, kept so a refinement can be asked for after the fact without re-searching.
    private AccessPoint _origin;
    private AccessPoint _goal;
    private int _goalNodeA;
    private int _goalNodeB;
    private int _remainderA;
    private int _remainderB;
    private int _ticksPerTile;
    private int _bestPortal = -1;
    private int _directEndpoint = -1;

    public HpaSearch(
        RoadGraph graph,
        Clusters clusters,
        AbstractGraph abstractGraph,
        ReverseArcs reverse,
        int[] arcCost,
        bool pristineEntrySeeding = false)
    {
        _graph = graph;
        _clusters = clusters;
        _abstract = abstractGraph;
        _arcCost = arcCost;
        _entryCost = pristineEntrySeeding ? null : arcCost;

        _forward = new ClusterSearch(graph, clusters, reverse, arcCost);
        _backward = new ClusterSearch(graph, clusters, reverse, arcCost);
        _refine = new ClusterSearch(graph, clusters, reverse, arcCost);

        _cost = new int[abstractGraph.Portals];
        _touched = new int[abstractGraph.Portals];
        _closed = new int[abstractGraph.Portals];
        _fromEdge = new int[abstractGraph.Portals];
        _heapKey = new int[1024];
        _heapPortal = new int[1024];
        _ticksPerTile = Heuristic.TicksPerTile(Modes.Car);
    }

    /// <summary>
    /// Answers one query. Car only — the hierarchy is built over car costs, and R3's bypass column is
    /// what it has to say about walk Legs.
    /// </summary>
    public HpaOutcome Run(AccessPoint origin, AccessPoint goal)
    {
        _origin = origin;
        _goal = goal;
        _bestPortal = -1;
        _directEndpoint = -1;

        int same = SegmentEntry.SameSegmentCost(_graph, _entryCost, Modes.Car, origin, goal);
        if (same < Unreachable)
        {
            return new HpaOutcome(true, same, Bypass.SameSegment, 0, 0, 0, 0);
        }

        int adjacent = AdjacentCost(origin, goal);
        if (adjacent < Unreachable)
        {
            return new HpaOutcome(true, adjacent, Bypass.AdjacentSegment, 0, 0, 0, 0);
        }

        return Hierarchical(origin, goal);
    }

    /// <summary>Whether a query would take a bypass, without answering it. For the share column.</summary>
    public Bypass BypassFor(AccessPoint origin, AccessPoint goal, Modes mode)
    {
        if (SegmentEntry.SameSegmentCost(_graph, _entryCost, mode, origin, goal) < Unreachable)
        {
            return Bypass.SameSegment;
        }

        return SharedNode(origin.Segment, goal.Segment) >= 0 ? Bypass.AdjacentSegment : Bypass.None;
    }

    /// <summary>
    /// Appends the last query's arcs to <paramref name="into"/> in travel order, and returns how many
    /// were added. Zero for a bypass, which never leaves its own Segments.
    /// </summary>
    public int Refine(List<int> into)
    {
        int before = into.Count;

        if (_directEndpoint >= 0)
        {
            AppendForwardPrefix(into, _directEndpoint);
            return into.Count - before;
        }

        if (_bestPortal < 0)
        {
            return 0;
        }

        // The abstract path, read back from its tail. Reversed once at the end rather than built
        // forwards, because the abstract search knows only where each portal was reached from.
        var portals = _portalPath;
        portals.Clear();
        int portal = _bestPortal;
        while (portal >= 0)
        {
            portals.Add(portal);
            int edge = _fromEdge[portal];
            portal = edge < 0 ? -1 : SourceOf(edge);
        }

        portals.Reverse();

        AppendForwardPrefix(into, _abstract.PortalNode[portals[0]]);

        for (int i = 0; i + 1 < portals.Count; i++)
        {
            int edge = _fromEdge[portals[i + 1]];

            if (_abstract.EdgeArc[edge] >= 0)
            {
                into.Add(_abstract.EdgeArc[edge]);
                continue;
            }

            if (_abstract.HasPaths && _abstract.EdgePathLength[edge] > 0)
            {
                // The stored path, copied. This is the whole of what a path store buys, and R3
                // measured the alternative below at 2.5× more per query at 16 Chunks than at 8.
                int[] arena = _abstract.PathArena[_clusters.OfNode[_abstract.PortalNode[portals[i]]]];
                int start = _abstract.EdgePathStart[edge];

                for (int k = 0; k < _abstract.EdgePathLength[edge]; k++)
                {
                    into.Add(arena[start + k]);
                }

                continue;
            }

            AppendIntra(into, _abstract.PortalNode[portals[i]], _abstract.PortalNode[portals[i + 1]]);
        }

        AppendBackwardSuffix(into, _abstract.PortalNode[_bestPortal]);
        return into.Count - before;
    }

    // --- The bypasses -----------------------------------------------------------------------------

    private int AdjacentCost(AccessPoint origin, AccessPoint goal)
    {
        SharedNodes(origin.Segment, goal.Segment, out int first, out int second);

        int best = Through(origin, goal, first);
        int other = Through(origin, goal, second);

        return other < best ? other : best;
    }

    private int Through(AccessPoint origin, AccessPoint goal, int node)
    {
        if (node < 0)
        {
            return Unreachable;
        }

        int toNode = SegmentEntry.CostToEndpoint(_graph, _entryCost, Modes.Car, origin, node);
        int fromNode = SegmentEntry.CostFromEndpoint(_graph, _entryCost, Modes.Car, node, goal);

        return toNode >= Unreachable || fromNode >= Unreachable ? Unreachable : toNode + fromNode;
    }

    /// <summary>The nodes two Segments have in common, as up to two endpoints, or <c>-1</c>.</summary>
    private void SharedNodes(int first, int second, out int one, out int two)
    {
        int a1 = _graph.SegmentNodeA[first];
        int b1 = _graph.SegmentNodeB[first];
        int a2 = _graph.SegmentNodeA[second];
        int b2 = _graph.SegmentNodeB[second];

        one = a1 == a2 || a1 == b2 ? a1 : -1;
        two = b1 == a2 || b1 == b2 ? b1 : -1;
    }

    private int SharedNode(int first, int second)
    {
        SharedNodes(first, second, out int one, out int two);
        return one >= 0 ? one : two;
    }

    // --- The hierarchy ----------------------------------------------------------------------------

    private HpaOutcome Hierarchical(AccessPoint origin, AccessPoint goal)
    {
        _goalNodeA = _graph.SegmentNodeA[goal.Segment];
        _goalNodeB = _graph.SegmentNodeB[goal.Segment];
        _remainderA = SegmentEntry.CostFromEndpoint(_graph, _entryCost, Modes.Car, _goalNodeA, goal);
        _remainderB = SegmentEntry.CostFromEndpoint(_graph, _entryCost, Modes.Car, _goalNodeB, goal);

        int originA = _graph.SegmentNodeA[origin.Segment];
        int originB = _graph.SegmentNodeB[origin.Segment];

        (int originCluster, int originSecond) = ClusterPair(originA, originB);
        (int goalCluster, int goalSecond) = ClusterPair(_goalNodeA, _goalNodeB);

        _forward.Begin(originCluster, originSecond, backward: false);
        _forward.Seed(originA, SegmentEntry.CostToEndpoint(_graph, _entryCost, Modes.Car, origin, originA));
        _forward.Seed(originB, SegmentEntry.CostToEndpoint(_graph, _entryCost, Modes.Car, origin, originB));
        _forward.Run();

        _backward.Begin(goalCluster, goalSecond, backward: true);
        _backward.Seed(_goalNodeA, _remainderA);
        _backward.Seed(_goalNodeB, _remainderB);
        _backward.Run();

        int best = Unreachable;

        // The origin's own confined search may already hold the answer, when the two Access Points
        // share a cluster. It is not a special case in the algorithm — it is the same confinement
        // the hierarchy applies everywhere, arriving early.
        int reachedA = _forward.CostOf(_goalNodeA);
        if (reachedA < Unreachable && _remainderA < Unreachable && reachedA + _remainderA < best)
        {
            best = reachedA + _remainderA;
            _directEndpoint = _goalNodeA;
        }

        int reachedB = _forward.CostOf(_goalNodeB);
        if (reachedB < Unreachable && _remainderB < Unreachable && reachedB + _remainderB < best)
        {
            best = reachedB + _remainderB;
            _directEndpoint = _goalNodeB;
        }

        _generation++;
        _heapCount = 0;

        SeedPortals(originCluster);
        if (originSecond >= 0)
        {
            SeedPortals(originSecond);
        }

        int expanded = 0;
        int relaxed = 0;

        while (_heapCount > 0)
        {
            (int key, int portal) = Pop();

            if (key >= best)
            {
                break;
            }

            if (_closed[portal] == _generation)
            {
                continue;
            }

            _closed[portal] = _generation;
            expanded++;

            int here = _cost[portal];
            int exit = _backward.CostOf(_abstract.PortalNode[portal]);

            if (exit < Unreachable && here + exit < best)
            {
                best = here + exit;
                _bestPortal = portal;
                _directEndpoint = -1;
            }

            for (int edge = _abstract.EdgeStart[portal]; edge < _abstract.EdgeStart[portal + 1]; edge++)
            {
                relaxed++;

                int step = _abstract.EdgeCost[edge];
                if (step >= Unreachable)
                {
                    continue;
                }

                int next = _abstract.EdgeTarget[edge];
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
                _fromEdge[next] = edge;

                int estimate = Estimate(_abstract.PortalNode[next]);
                if (estimate < Unreachable)
                {
                    Push(candidate + estimate, next);
                }
            }
        }

        int nodes = _forward.Expanded + _backward.Expanded;
        int arcs = _forward.Relaxed + _backward.Relaxed;

        return best >= Unreachable
            ? new HpaOutcome(false, 0, Bypass.None, nodes, expanded, arcs, relaxed)
            : new HpaOutcome(true, best, Bypass.None, nodes, expanded, arcs, relaxed);
    }

    private (int First, int Second) ClusterPair(int nodeA, int nodeB)
    {
        int first = _clusters.OfNode[nodeA];
        int second = _clusters.OfNode[nodeB];
        return (first, second == first ? -1 : second);
    }

    private void SeedPortals(int cluster)
    {
        for (int portal = _abstract.FirstPortalOf(cluster);
             portal < _abstract.FirstPortalOf(cluster) + _abstract.PortalCountOf(cluster);
             portal++)
        {
            int entry = _forward.CostOf(_abstract.PortalNode[portal]);
            if (entry >= Unreachable)
            {
                continue;
            }

            if (_touched[portal] == _generation && _cost[portal] <= entry)
            {
                continue;
            }

            _cost[portal] = entry;
            _touched[portal] = _generation;
            _fromEdge[portal] = -1;

            int estimate = Estimate(_abstract.PortalNode[portal]);
            if (estimate < Unreachable)
            {
                Push(entry + estimate, portal);
            }
        }
    }

    /// <summary>
    /// The same <c>h</c> the flat search uses, evaluated at a portal's node.
    /// </summary>
    /// <remarks>
    /// <b>It stays admissible over the abstract graph.</b> An abstract edge's cost is the cost of a
    /// real path between two real nodes, so it is at least the straight-line bound between them; a
    /// heuristic that lower-bounds every concrete path therefore lower-bounds every abstract one.
    /// Using <c>Chebyshev</c> rather than a tighter metric is R0's decision, and it is quoted here
    /// unchanged so that the wall-clock difference between the two searches is the hierarchy.
    /// </remarks>
    private int Estimate(int node)
    {
        int x = _graph.NodeX[node];
        int y = _graph.NodeY[node];

        int viaA = _remainderA >= Unreachable
            ? Unreachable
            : Heuristic.Ticks(
                Heuristic.Distance(
                    HeuristicKind.Chebyshev, x, y, _graph.NodeX[_goalNodeA], _graph.NodeY[_goalNodeA]),
                _ticksPerTile) + _remainderA;

        int viaB = _remainderB >= Unreachable
            ? Unreachable
            : Heuristic.Ticks(
                Heuristic.Distance(
                    HeuristicKind.Chebyshev, x, y, _graph.NodeX[_goalNodeB], _graph.NodeY[_goalNodeB]),
                _ticksPerTile) + _remainderB;

        return viaA < viaB ? viaA : viaB;
    }

    // --- Refinement -------------------------------------------------------------------------------

    private int SourceOf(int edge)
    {
        // The edge CSR is grouped by source portal, so a binary search over EdgeStart recovers it.
        // Kept out of the query's hot path deliberately: only a refinement ever needs it, and
        // storing a source per edge would put a column into the resident-size figure that no query
        // reads.
        int low = 0;
        int high = _abstract.Portals - 1;

        while (low < high)
        {
            int mid = low + ((high - low + 1) >> 1);
            if (_abstract.EdgeStart[mid] <= edge)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    private void AppendForwardPrefix(List<int> into, int node)
    {
        int before = into.Count;
        int at = node;

        while (_forward.CameFromArc(at) >= 0)
        {
            int arc = _forward.CameFromArc(at);
            into.Add(arc);
            at = _forward.Previous(arc);
        }

        into.Reverse(before, into.Count - before);
    }

    private void AppendBackwardSuffix(List<int> into, int node)
    {
        int at = node;

        while (_backward.CameFromArc(at) >= 0)
        {
            int arc = _backward.CameFromArc(at);
            into.Add(arc);
            at = _backward.Previous(arc);
        }
    }

    private void AppendIntra(List<int> into, int from, int to)
    {
        // The fallback, for an abstract graph built without a path store. An intra-cluster edge
        // holds a cost and not a path, so this is the search that recovers one — and it is the
        // whole of what refinement costs beyond the two insertions.
        _refine.Begin(_clusters.OfNode[from], -1, backward: false);
        _refine.Seed(from, 0);
        _refine.Run();

        int before = into.Count;
        int at = to;

        while (_refine.CameFromArc(at) >= 0)
        {
            int arc = _refine.CameFromArc(at);
            into.Add(arc);
            at = _refine.Previous(arc);
        }

        into.Reverse(before, into.Count - before);
    }

    // --- Binary heap ------------------------------------------------------------------------------

    private void Push(int key, int portal)
    {
        if (_heapCount == _heapKey.Length)
        {
            Array.Resize(ref _heapKey, _heapKey.Length * 2);
            Array.Resize(ref _heapPortal, _heapPortal.Length * 2);
        }

        int i = _heapCount++;
        _heapKey[i] = key;
        _heapPortal[i] = portal;

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

    private (int Key, int Portal) Pop()
    {
        (int key, int portal) = (_heapKey[0], _heapPortal[0]);

        _heapCount--;
        _heapKey[0] = _heapKey[_heapCount];
        _heapPortal[0] = _heapPortal[_heapCount];

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

        return (key, portal);
    }

    private void Swap(int a, int b)
    {
        (_heapKey[a], _heapKey[b]) = (_heapKey[b], _heapKey[a]);
        (_heapPortal[a], _heapPortal[b]) = (_heapPortal[b], _heapPortal[a]);
    }
}
