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

    /// <summary>How many nodes the last search relaxed. For a test, and for a cost report.</summary>
    public int Relaxed { get; private set; }

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
    public void Begin(int nodeCount)
    {
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
    public void Seed(int node, TravelTime cost) => Relax(node, cost);

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
    public TravelTime Search(RoadGraph graph, int targetA, int targetB, TravelTime inA, TravelTime inB)
    {
        ArgumentNullException.ThrowIfNull(graph);

        RoadNodeTable nodes = graph.Nodes;
        RoadArcs arcs = graph.Arcs;

        TravelTime best = TravelTime.Impassable;
        TravelTime cheapestEntry = inA < inB ? inA : inB;

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

            if (node == targetA)
            {
                best = Cheaper(best, cost + inA);
            }

            if (node == targetB)
            {
                best = Cheaper(best, cost + inB);
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
                if (!arcs.Admits(i, TravelMode.Foot))
                {
                    continue;
                }

                TravelTime step = arcs.FootTime[i];

                if (step.IsImpassable)
                {
                    continue;
                }

                Relax(arcs.Target[i], cost + step);
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

                Relax(arcs.Target[i], cost + step);
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

    private static TravelTime Cheaper(TravelTime left, TravelTime right) =>
        right < left ? right : left;

    /// <summary>Improves a node's tentative cost, pushing it onto the heap if it moved.</summary>
    private void Relax(int node, TravelTime cost)
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
