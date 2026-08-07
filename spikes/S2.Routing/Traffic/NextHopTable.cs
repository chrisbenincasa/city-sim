using S2.Routing.Graph;
using S2.Routing.Matrix;

namespace S2.Routing.Traffic;

/// <summary>
/// The forward graph's arcs indexed by the node they <i>arrive at</i>, plus each arc's source.
/// </summary>
/// <remarks>
/// <b>A backward search needs an index the forward CSR does not carry.</b> The graph is grouped by
/// source node, which answers <i>what can I reach from here</i>; a next-hop table asks the opposite
/// question — <i>which arc, from here, leads toward that destination</i> — and answering it is a
/// Dijkstra rooted at the destination running over incoming arcs. Nothing about the graph changes;
/// this is a second view of the same arcs, built once and reused across the District sweep.
/// </remarks>
internal sealed class ReverseArcs
{
    private ReverseArcs(int[] start, int[] arc, int[] source)
    {
        Start = start;
        Arc = arc;
        Source = source;
    }

    /// <summary>CSR offsets by target node, length <c>Nodes + 1</c>.</summary>
    public int[] Start { get; }

    /// <summary>Forward arc indices, grouped by the node they arrive at.</summary>
    public int[] Arc { get; }

    /// <summary>The node each forward arc departs from. The forward CSR only stores the target.</summary>
    public int[] Source { get; }

    public static ReverseArcs Of(RoadGraph graph)
    {
        var source = new int[graph.Arcs];
        for (int node = 0; node < graph.Nodes; node++)
        {
            for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
            {
                source[arc] = node;
            }
        }

        var start = new int[graph.Nodes + 1];
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            start[graph.ArcTarget[arc] + 1]++;
        }

        for (int node = 0; node < graph.Nodes; node++)
        {
            start[node + 1] += start[node];
        }

        var cursor = (int[])start.Clone();
        var into = new int[graph.Arcs];
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            into[cursor[graph.ArcTarget[arc]]++] = arc;
        }

        return new ReverseArcs(start, into, source);
    }
}

/// <summary>
/// One arc per <c>(node, District)</c>: from this node, the first arc of the shortest route to that
/// District's representative. The third rung of R2's path-source ladder — <b>a router that stores no
/// path at all</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This rung exists because <c>adr/0041</c> moved the question and <c>plans/0010</c> was written
/// before it.</b> The plan retires R4 <i>"if Statistical Trips need no concrete path"</i>. Under
/// <c>adr/0041</c> a vehicular Traveller increments the Segment it enters and decrements on exit, so
/// what it needs every Tick is a <b>next Segment</b> — which is not the same object as a path. A
/// next-hop table supplies exactly that and stores nothing per Trip, so the many-to-many argument the
/// plan expected to have evaporated is instead the argument this rung <i>is</i>. Measuring only
/// searched-against-shared would have answered a question the design had already moved past, which is
/// the precise failure the prescribed order exists to avoid.
/// </para>
/// <para>
/// <b>It is a distance-vector table computed centrally, and that is the honest description.</b> DSDV
/// would converge to the same next-hop assignment by exchanging vectors between neighbours; this
/// builds it by <c>n</c> backward Dijkstras because R2 is pricing the <i>shape</i> — resident size,
/// read cost, and the error induced by aiming at a representative — and not DSDV's convergence, which
/// is R4's and R5's subject. <b>So a good number here does not settle R4; it makes R4 worth running.</b>
/// </para>
/// <para>
/// <b>Its error profile is not the shared route's, and that is the finding this rung was worth
/// building for.</b> A shared District-pair route is coarse at <i>both</i> ends — a Traveller must
/// reach the origin representative before the stored route means anything. A next-hop table is
/// followed from wherever the Traveller actually is, so it is <b>exact on the origin side and coarse
/// only on the destination side</b>. The two coarse rungs are therefore not two spellings of one
/// approximation, and R2.1 reports the induced detour separately for each.
/// </para>
/// </remarks>
internal sealed class NextHopTable
{
    private readonly RoadGraph _graph;
    private readonly ReverseArcs _reverse;
    private readonly int _nodes;

    private readonly int[] _nextArc;

    /// <summary>
    /// Settled free-flow cost from each node to each District, or <c>null</c> when nobody asked.
    /// </summary>
    /// <remarks>
    /// <b>Retained only on request, because it doubles the table.</b> R2 and R4 publish this
    /// structure's resident size and the DSDV memory tripwire divides by it, so making every caller
    /// pay for R8's array would move a figure the corpus has already read. See
    /// <see cref="DistanceOf"/> for what R8 needs it for.
    /// </remarks>
    private readonly int[]? _distance;

    private readonly int[] _cost;
    private readonly int[] _touched;
    private readonly int[] _closed;

    private int[] _heapKey;
    private int[] _heapNode;
    private int _heapCount;
    private int _generation;

    private NextHopTable(RoadGraph graph, ReverseArcs reverse, int districts, bool retainDistance)
    {
        _graph = graph;
        _reverse = reverse;
        _nodes = graph.Nodes;

        _nextArc = new int[(long)districts * graph.Nodes <= int.MaxValue
            ? districts * graph.Nodes
            : throw new ArgumentOutOfRangeException(nameof(districts), "table exceeds one array")];

        _cost = new int[graph.Nodes];
        _touched = new int[graph.Nodes];
        _closed = new int[graph.Nodes];
        _heapKey = new int[4096];
        _heapNode = new int[4096];

        Array.Fill(_nextArc, -1);

        if (retainDistance)
        {
            _distance = new int[_nextArc.Length];

            // Impassable rather than zero, so an unreached node reads as *no route* and not as
            // *already there*. A zero fill here would make every District look adjacent to every
            // node it cannot reach, which is a scoring bug that presents as a Traveller confidently
            // diverting into a component it can never leave.
            Array.Fill(_distance, RoadGraph.Impassable);
        }
    }

    /// <summary>Nodes settled by the last backward search. The build's own work figure.</summary>
    public int NodesSettled { get; private set; }

    /// <summary><c>districts × nodes × 4 B</c>. The axis <c>adr/0006</c> would fail on first.</summary>
    /// <remarks>
    /// <b>The distance column is deliberately not in here.</b> R2 and R4 report this quantity and the
    /// DSDV memory tripwire divides by it; adding a column R8 asked for would change a figure that
    /// has already been read and argued over. See <see cref="DistanceResidentBytes"/>.
    /// </remarks>
    public long ResidentBytes => (long)_nextArc.Length * sizeof(int);

    /// <summary>What retaining the free-flow distances costs, or zero where they were not retained.</summary>
    public long DistanceResidentBytes => _distance is null ? 0 : (long)_distance.Length * sizeof(int);

    /// <summary>
    /// The arc to take from <paramref name="node"/> toward <paramref name="district"/>, or <c>-1</c>
    /// where no route exists.
    /// </summary>
    /// <remarks>
    /// <b>District-major, so following one route is a strided walk rather than a random one.</b> The
    /// alternative layout — node-major — would put a Traveller's successive reads
    /// <c>districts</c> apart instead of <c>nodes</c> apart, which is the same miss on a large table
    /// and a worse one on a small. R1 measured the same cliff on the same machine and R2.1 quotes it.
    /// </remarks>
    public int Of(int node, int district) => _nextArc[(district * _nodes) + node];

    /// <summary>
    /// The settled cost from <paramref name="node"/> to <paramref name="district"/> under the arc
    /// costs the table was built on, Q16.16 Ticks, or <see cref="RoadGraph.Impassable"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the lagged half of <c>adr/0046</c>'s model and R8 cannot score a branch without
    /// it.</b> Sight compares the live cost of the next <c>N</c> arcs down two branches; two branches
    /// that have travelled different distances are not comparable, and comparing the raw sums biases
    /// the choice toward whichever branch happens to have shorter Segments. Adding the free-flow
    /// remainder from where each lookahead ends makes them comparable — and it is exactly what
    /// <c>adr/0046</c> describes, <i>"a live view of what is in front plus a lagged expectation of
    /// the rest."</i>
    /// </para>
    /// <para>
    /// <b>Also the reachability test the diversion filter needs</b>, and a better one than
    /// <see cref="Of"/>: the arc at a District's own representative is <c>-1</c>, so a branch landing
    /// exactly on the destination would read as unreachable through <see cref="Of"/> and be discarded
    /// — the one alternative that cannot be wrong.
    /// </para>
    /// <para>
    /// Throws where the table was built without distances rather than returning a plausible number.
    /// </para>
    /// </remarks>
    public int DistanceOf(int node, int district) =>
        _distance is null
            ? throw new InvalidOperationException("table was built without distances")
            : _distance[(district * _nodes) + node];

    /// <summary>
    /// Builds the table: one backward Dijkstra per District, over the arc costs supplied.
    /// </summary>
    /// <param name="retainDistance">
    /// Keep each node's settled cost to each District as well as its first arc. Off by default, and
    /// the default is what every caller before R8 gets — see <see cref="DistanceOf"/>.
    /// </param>
    public static NextHopTable Build(
        RoadGraph graph,
        ReverseArcs reverse,
        Districts districts,
        int[] arcTicks,
        bool retainDistance = false)
    {
        ArgumentNullException.ThrowIfNull(districts);

        var table = new NextHopTable(graph, reverse, districts.Count, retainDistance);

        for (int district = 0; district < districts.Count; district++)
        {
            int representative = districts.Representative[district];
            if (representative >= 0)
            {
                table.Run(representative, district, arcTicks);
            }
        }

        return table;
    }

    /// <summary>
    /// One destination's column: a Dijkstra rooted at <paramref name="target"/> relaxing arcs
    /// <i>backwards</i>, so the arc recorded at a node is the first step of that node's route.
    /// </summary>
    public void Run(int target, int district, int[] arcTicks)
    {
        _generation++;
        _heapCount = 0;
        NodesSettled = 0;

        int column = district * _nodes;

        _cost[target] = 0;
        _touched[target] = _generation;
        _nextArc[column + target] = -1;

        if (_distance is not null)
        {
            _distance[column + target] = 0;
        }

        Push(0, target);

        while (_heapCount > 0)
        {
            (_, int node) = Pop();

            if (_closed[node] == _generation)
            {
                continue;
            }

            _closed[node] = _generation;
            NodesSettled++;

            int here = _cost[node];

            for (int slot = _reverse.Start[node]; slot < _reverse.Start[node + 1]; slot++)
            {
                int arc = _reverse.Arc[slot];
                int step = arcTicks[arc];
                if (step == RoadGraph.Impassable)
                {
                    continue;
                }

                int from = _reverse.Source[arc];
                if (_closed[from] == _generation)
                {
                    continue;
                }

                int candidate = here + step;
                if (_touched[from] == _generation && _cost[from] <= candidate)
                {
                    continue;
                }

                _cost[from] = candidate;
                _touched[from] = _generation;

                // The arc that got us here backwards is, forwards, the first step out of `from`.
                _nextArc[column + from] = arc;

                // Written in step with the arc, so the two can never disagree about which route the
                // distance belongs to. A node relaxed again before it closes overwrites both.
                if (_distance is not null)
                {
                    _distance[column + from] = candidate;
                }

                Push(candidate, from);
            }
        }
    }

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
        int key = _heapKey[0];
        int node = _heapNode[0];

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

            int right = left + 1;
            int smaller = right < _heapCount && _heapKey[right] < _heapKey[left] ? right : left;

            if (_heapKey[i] <= _heapKey[smaller])
            {
                break;
            }

            Swap(i, smaller);
            i = smaller;
        }

        return (key, node);
    }

    private void Swap(int a, int b)
    {
        (_heapKey[a], _heapKey[b]) = (_heapKey[b], _heapKey[a]);
        (_heapNode[a], _heapNode[b]) = (_heapNode[b], _heapNode[a]);
    }
}
