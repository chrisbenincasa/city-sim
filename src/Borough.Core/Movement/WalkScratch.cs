using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Core.Movement;

/// <summary>
/// The reusable state of one walk search: tentative costs, a visit stamp, and a binary heap.
/// </summary>
/// <remarks>
/// <para>
/// <b>Arrays beside the search rather than a <c>[Table]</c>, and the test is disposition rather than
/// shape</b> — the same call <see cref="RoadArcs"/> and <c>CellResidency</c> make. This is storage
/// that is never saved and never hashed, so it cannot have the defect <c>BOR0901</c> exists to
/// detect. It is scratch: discarded logically at the start of every query.
/// </para>
/// <para>
/// <b>Grown to the largest graph seen and never shrunk</b>, which is the posture the Rule engine's
/// scratch buffers take. Bounded by the size of the world rather than by elapsed time, so
/// <c>adr/0006</c> is satisfied structurally rather than by an invariant watching it.
/// </para>
/// <para>
/// <b>One instance per caller, never shared across threads.</b> Tick Phase 4 is <em>permitted
/// parallel</em>, and a shared scratch would make the result depend on which thread reached it
/// first — a divergence rather than a race, which is the failure mode <c>adr/0004</c> says is worst
/// because the tools report success.
/// </para>
/// </remarks>
public sealed class WalkScratch
{
    private TravelTime[] _distance = [];
    private int[] _stamp = [];
    private bool[] _settled = [];

    private TravelTime[] _heapCost = [];
    private int[] _heapNode = [];
    private int _heapCount;

    private int _generation;
    private int _nodes;

    /// <summary>Whether this search is recording predecessors. See <see cref="Begin"/>.</summary>
    private bool _recording;

    /// <summary>The Arc a node was reached by, valid only where the stamp is current.</summary>
    private int[] _via = [];

    /// <summary>The node a node was reached from, or <see cref="NoNode"/> at a seed.</summary>
    /// <remarks>
    /// <b>Both the Arc and the node it came from, rather than the Arc alone.</b> An Arc knows its
    /// target and its Segment but not its source — a node's Arcs are a contiguous slice on the node,
    /// so recovering the source from an Arc index would mean searching the adjacency backwards. Two
    /// arrays is the cheaper half of that trade and it is paid only when recording.
    /// </remarks>
    private int[] _previous = [];

    /// <summary>The end of a path — the seed a walk back through predecessors stops at.</summary>
    public const int NoNode = -1;

    /// <summary>There is no route to read: the node was never settled, or nothing was recorded.</summary>
    /// <remarks>
    /// <b>Distinct from a length of zero, which is a real answer.</b> A seed node is reached by no
    /// Segments at all, and that is the correct route for a journey that begins where it ends.
    /// </remarks>
    public const int NoPath = -1;

    /// <summary>How many nodes the last search relaxed. For a test, and for a cost report.</summary>
    public int Relaxed { get; private set; }

    /// <summary>
    /// Which of <see cref="Search"/>'s two targets the answer came through, or <see cref="NoNode"/>.
    /// </summary>
    /// <remarks>
    /// <b>The route ends somewhere specific and the cost does not say where.</b> A walk leaves its
    /// Segment by either endpoint and enters the destination's by either, so <see cref="Search"/>
    /// returns the cheapest of four combinations — and a caller wanting the path needs to know which
    /// one won before it can walk the predecessors back. A walk never asks; a vehicle does, because
    /// the Segment it arrives on decides which side of the road it parks.
    /// </remarks>
    public int Arrived { get; private set; } = NoNode;

    /// <summary>
    /// Clears the state for a graph of <paramref name="nodeCount"/> nodes.
    /// </summary>
    /// <remarks>
    /// <b>A stamp rather than a clear, which is what keeps a query proportional to the part of the
    /// graph it touches.</b> Zeroing the distance array would make every walk between two
    /// neighbouring Buildings cost a pass over all ~30,000 nodes — turning the cheapest query in the
    /// city into the most expensive one, and doing it invisibly, since the answer would still be
    /// right. Instead each entry records the generation that wrote it, and an entry from an older
    /// generation reads as unvisited.
    /// </remarks>
    public void Begin(int nodeCount, bool recordPath = false)
    {
        _recording = recordPath;

        if (recordPath && _via.Length < nodeCount)
        {
            _via = new int[nodeCount];
            _previous = new int[nodeCount];
        }

        if (_distance.Length < nodeCount)
        {
            _distance = new TravelTime[nodeCount];
            _stamp = new int[nodeCount];
            _settled = new bool[nodeCount];
            _heapCost = new TravelTime[nodeCount + 1];
            _heapNode = new int[nodeCount + 1];
        }

        _nodes = nodeCount;
        _heapCount = 0;
        Relaxed = 0;

        // The stamps are never cleared, so the counter has to be the thing that cannot collide. At
        // the ceiling the array is wiped once and the count restarts — a cost paid every two billion
        // searches, which is the alternative to a per-query clear and is the reason this is correct
        // rather than merely fast.
        if (_generation == int.MaxValue)
        {
            Array.Clear(_stamp);
            _generation = 0;
        }

        _generation++;
    }

    /// <summary>Opens the search at a node with a starting cost — the walk out to an endpoint.</summary>
    public void Seed(int node, TravelTime cost) => Relax(node, cost, NoNode, NoNode);

    /// <summary>
    /// Runs the search and returns the cheapest total to either destination endpoint, or
    /// <see cref="TravelTime.Impassable"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Dijkstra rather than A*, and the reason is that this search has two sources and two
    /// targets.</b> A* wants an admissible heuristic toward <em>one</em> goal; the walk out of a
    /// Segment may leave by either end and enter the destination's by either, so the four
    /// combinations are resolved by seeding both origins and reading both targets. On the foot
    /// subgraph at a Building's scale the settled set is small, which is what makes the plain search
    /// affordable — and if a measurement ever says otherwise, the heuristic to add is straight-line
    /// distance over the node grid, which <c>RoadNodeTable</c> already stores.
    /// </para>
    /// <para>
    /// <b>The stopping rule is the standard one and is worth stating, because a walk search is
    /// otherwise unbounded on a connected graph.</b> Every Arc cost is non-negative, so once the
    /// cheapest unsettled node already costs at least as much as the best complete answer, nothing
    /// remaining can improve it. Without that the search settles the whole city to answer a question
    /// about two adjacent streets.
    /// </para>
    /// <para>
    /// <b>Ties break on the node slot</b>, which is what makes the result identical across runs. Two
    /// equal-cost frontier nodes are a coin toss the heap would otherwise resolve by insertion order,
    /// and insertion order is a function of the allocation history of the graph.
    /// </para>
    /// </remarks>
    /// <param name="graph">The Road Graph to search.</param>
    /// <param name="mode">Which subgraph to traverse.</param>
    /// <param name="targetA">One endpoint of the destination Segment.</param>
    /// <param name="targetB">The other.</param>
    /// <param name="inA">What it costs to walk in from <paramref name="targetA"/>.</param>
    /// <param name="inB">What it costs to walk in from <paramref name="targetB"/>.</param>
    public TravelTime Search(
        RoadGraph graph, TravelMode mode, int targetA, int targetB, TravelTime inA, TravelTime inB)
    {
        ArgumentNullException.ThrowIfNull(graph);

        RoadNodeTable nodes = graph.Nodes;
        RoadArcs arcs = graph.Arcs;

        TravelTime best = TravelTime.Impassable;
        TravelTime cheapestEntry = inA < inB ? inA : inB;

        Arrived = NoNode;

        while (_heapCount > 0)
        {
            TravelTime cost = _heapCost[0];
            int node = _heapNode[0];

            PopRoot();

            if (_settled[node])
            {
                continue;
            }

            _settled[node] = true;
            Relaxed++;

            if (node == targetA && cost + inA < best)
            {
                best = cost + inA;
                Arrived = node;
            }

            if (node == targetB && cost + inB < best)
            {
                best = cost + inB;
                Arrived = node;
            }

            // Nothing still on the frontier can beat what we already hold.
            if (!best.IsImpassable && cost + cheapestEntry >= best)
            {
                break;
            }

            int start = nodes.ArcStart[node];
            int count = nodes.ArcCount[node];

            for (int i = start; i < start + count; i++)
            {
                if (!arcs.Admits(i, mode))
                {
                    continue;
                }

                TravelTime step = arcs.TimeFor(i, mode);

                if (step.IsImpassable)
                {
                    continue;
                }

                Relax(arcs.Target[i], cost + step, i, node);
            }
        }

        return best;
    }

    /// <summary>
    /// Settles every node reachable from whatever was seeded, in <paramref name="mode"/>, and leaves
    /// the costs behind for <see cref="CostTo"/> to read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same search as <see cref="Search"/> with the stopping rule removed, which is exactly
    /// what a matrix row is.</b> A one-to-all fill and a point-to-point query are the same algorithm
    /// differing only in when they are allowed to stop, so this shares the heap, the stamp and the
    /// distance array rather than forking them. Forking would put two Dijkstras in the tree whose
    /// tie-breaks could drift apart, and a tie-break that drifts is a divergence the State Hash would
    /// report long after the edit that caused it.
    /// </para>
    /// <para>
    /// <b>Mode-parameterised where <see cref="Search"/> is foot-only, and the asymmetry is
    /// deliberate.</b> A walk query has one mode by construction; a matrix has a row set per mode it
    /// is asked for. <see cref="RoadArcs"/> already carries both traversal times and a
    /// <see cref="RoadArcs.TimeFor"/>, so this costs a parameter rather than a second graph.
    /// </para>
    /// <para>
    /// <b>An unreachable node is left unsettled and <see cref="CostTo"/> reports it
    /// <see cref="TravelTime.Impassable"/> — which is a fact rather than an estimate.</b> Two
    /// partitions in different components of the mode subgraph produce an Impassable entry, so a
    /// matrix built on this can distinguish <em>too far</em> from <em>no route at all</em>. That
    /// distinction is what keeps <c>03 §3.7</c>'s Severance visible to a consumer that never runs the
    /// walk.
    /// </para>
    /// </remarks>
    /// <param name="graph">The Road Graph to search.</param>
    /// <param name="mode">Which subgraph to traverse.</param>
    public void SettleAll(RoadGraph graph, TravelMode mode)
    {
        ArgumentNullException.ThrowIfNull(graph);

        RoadNodeTable nodes = graph.Nodes;
        RoadArcs arcs = graph.Arcs;

        while (_heapCount > 0)
        {
            TravelTime cost = _heapCost[0];
            int node = _heapNode[0];

            PopRoot();

            if (_settled[node])
            {
                continue;
            }

            _settled[node] = true;
            Relaxed++;

            int start = nodes.ArcStart[node];
            int count = nodes.ArcCount[node];

            for (int i = start; i < start + count; i++)
            {
                if (!arcs.Admits(i, mode))
                {
                    continue;
                }

                TravelTime step = arcs.TimeFor(i, mode);

                if (step.IsImpassable)
                {
                    continue;
                }

                Relax(arcs.Target[i], cost + step, i, node);
            }
        }
    }

    /// <summary>
    /// What the last search settled a node at, or <see cref="TravelTime.Impassable"/> if it never
    /// reached it.
    /// </summary>
    /// <remarks>
    /// <b>Settled rather than merely relaxed, and the difference is correctness rather than
    /// pedantry.</b> A node on the frontier holds a tentative cost that a later pop may improve, so
    /// reading one mid-search would return a number that is right in form and too large in value.
    /// After <see cref="SettleAll"/> every reachable node is settled, so the distinction is invisible
    /// there — it is stated because this accessor is also readable after <see cref="Search"/>, which
    /// stops early and leaves most of the graph exactly in that state.
    /// </remarks>
    public TravelTime CostTo(int node) =>
        (uint)node < (uint)_nodes && _stamp[node] == _generation && _settled[node]
            ? _distance[node]
            : TravelTime.Impassable;

    /// <summary>
    /// The Segments a settled node was reached by, origin first — <b>the route, recovered after the
    /// fact from the predecessors the search left behind</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Recovered rather than accumulated, which is what keeps a route free until somebody asks for
    /// one.</b> Dijkstra already knows the whole tree of cheapest paths by the time it stops; the only
    /// thing needed to read one out is which Arc each node was reached by, and that is one store per
    /// improvement rather than a growing list per frontier node. A per-node path list would be
    /// <c>O(nodes × path length)</c> of copying for an answer exactly one node needs.
    /// </para>
    /// <para>
    /// <b>Segments and not Arcs, because a Segment is what the rest of the simulation is denominated
    /// in.</b> Volume is attributed per Segment (<c>adr/0041</c>), the Epoch that invalidates a route
    /// is per Segment (<c>adr/0012</c>), and an Arc is the directed half of one. The direction is
    /// recoverable from the order of the list, so nothing is lost and the caller stops needing to know
    /// the adjacency exists.
    /// </para>
    /// <para>
    /// <b>A path that does not fit is not written at all, and the length is returned either way.</b> A
    /// truncated route is a <em>different</em> route rather than a partial answer — it ends somewhere
    /// the traveller was only passing through — so a caller that ignored a return value would get a
    /// plausible journey to the wrong place. Measure, then write.
    /// </para>
    /// <para>
    /// ⚠ <b>Only meaningful when <see cref="Begin"/> was told to record.</b> Without it the
    /// predecessor arrays hold whatever a previous recording search left, and the stamp cannot tell
    /// the difference because the stamp is written by the cost side. A caller that forgets gets a
    /// route from another journey — so this returns <see cref="NoPath"/> rather than guessing.
    /// </para>
    /// </remarks>
    /// <param name="arcs">The adjacency the search ran over.</param>
    /// <param name="node">A node the search settled — <see cref="Arrived"/>, usually.</param>
    /// <param name="segments">Where to write the Segment slots. May be empty to measure first.</param>
    /// <returns>
    /// How many Segments the route crosses — <c>0</c> at a seed — or <see cref="NoPath"/> when the
    /// search never settled <paramref name="node"/> or was not recording.
    /// </returns>
    public int PathTo(RoadArcs arcs, int node, Span<int> segments)
    {
        ArgumentNullException.ThrowIfNull(arcs);

        if (!_recording || (uint)node >= (uint)_nodes || _stamp[node] != _generation
            || !_settled[node])
        {
            return NoPath;
        }

        int length = 0;

        for (int cursor = node; _previous[cursor] != NoNode; cursor = _previous[cursor])
        {
            length++;
        }

        if (segments.Length < length)
        {
            return length;
        }

        // Backwards from the destination, written forwards into the span, so the caller reads the
        // route in the order it is travelled.
        int write = length;

        for (int cursor = node; _previous[cursor] != NoNode; cursor = _previous[cursor])
        {
            write--;
            segments[write] = arcs.Segment[_via[cursor]];
        }

        return length;
    }

    /// <summary>Improves a node's tentative cost, pushing it onto the heap if it moved.</summary>
    private void Relax(int node, TravelTime cost, int arc, int from)
    {
        if (cost.IsImpassable || (uint)node >= (uint)_nodes)
        {
            return;
        }

        bool seen = _stamp[node] == _generation;

        if (seen && (_settled[node] || _distance[node] <= cost))
        {
            return;
        }

        if (!seen)
        {
            _stamp[node] = _generation;
            _settled[node] = false;
        }

        _distance[node] = cost;

        // Only when the caller asked for a path. A predecessor write per relaxation is two array
        // stores on the hot walk path for a record only a vehicle reads, and a walk Leg discards its
        // route by decision rather than by omission (adr/0075). Opt-in rather than a second scratch
        // type, so there is one Dijkstra in the tree and its tie-breaks cannot drift apart.
        if (_recording)
        {
            _via[node] = arc;
            _previous[node] = from;
        }

        Push(cost, node);
    }

    /// <summary>
    /// Pushes an entry. <b>Lazy deletion — an improved node is pushed again and the stale copy is
    /// skipped when it surfaces</b>, which is why the heap is sized for pushes rather than for nodes.
    /// </summary>
    private void Push(TravelTime cost, int node)
    {
        if (_heapCount == _heapCost.Length)
        {
            Array.Resize(ref _heapCost, _heapCost.Length * 2);
            Array.Resize(ref _heapNode, _heapNode.Length * 2);
        }

        int i = _heapCount;
        _heapCount++;

        while (i > 0)
        {
            int parent = (i - 1) >> 1;

            if (!Precedes(cost, node, _heapCost[parent], _heapNode[parent]))
            {
                break;
            }

            _heapCost[i] = _heapCost[parent];
            _heapNode[i] = _heapNode[parent];
            i = parent;
        }

        _heapCost[i] = cost;
        _heapNode[i] = node;
    }

    private void PopRoot()
    {
        _heapCount--;

        if (_heapCount == 0)
        {
            return;
        }

        TravelTime cost = _heapCost[_heapCount];
        int node = _heapNode[_heapCount];

        int i = 0;

        while (true)
        {
            int left = (i << 1) + 1;

            if (left >= _heapCount)
            {
                break;
            }

            int child = left;
            int right = left + 1;

            if (right < _heapCount
                && Precedes(_heapCost[right], _heapNode[right], _heapCost[left], _heapNode[left]))
            {
                child = right;
            }

            if (!Precedes(_heapCost[child], _heapNode[child], cost, node))
            {
                break;
            }

            _heapCost[i] = _heapCost[child];
            _heapNode[i] = _heapNode[child];
            i = child;
        }

        _heapCost[i] = cost;
        _heapNode[i] = node;
    }

    /// <summary>
    /// The heap order: cheaper first, and <b>the lower node slot when two costs are equal</b>.
    /// </summary>
    /// <remarks>
    /// The tie-break is not a nicety. Equal-cost frontier entries are the common case on a grid of
    /// identical Streets — which is exactly what <c>[roads] block_tiles</c> generates — so without a
    /// total order the settled order would follow insertion order, and two runs building the same
    /// city with different allocation histories would take different routes at the same cost.
    /// </remarks>
    private static bool Precedes(TravelTime leftCost, int leftNode, TravelTime rightCost, int rightNode) =>
        leftCost == rightCost ? leftNode < rightNode : leftCost < rightCost;
}
