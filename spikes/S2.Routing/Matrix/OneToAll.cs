using S2.Routing.Graph;

namespace S2.Routing.Matrix;

/// <summary>
/// One-to-all Dijkstra over a supplied arc-cost array. The matrix's build kernel.
/// </summary>
/// <remarks>
/// <para>
/// <b>One search per District, not one per pair, and the graph being directed is what makes that
/// sound.</b> A forward one-to-all from District <c>i</c>'s representative yields the whole of row
/// <c>i</c> — every <c>matrix[i][j]</c> at once — so a cold build is <c>n</c> searches rather than
/// <c>n²</c>. Nothing here may be halved by symmetry: <c>plans/0010</c>'s gate section is explicit
/// that <i>"the travel-time matrix is asymmetric, so nothing may halve it by symmetry and R1's
/// resident-size figures stand at the full <c>n²</c> either way"</i>, and the reverse row would need
/// a backward search over the reverse graph to obtain.
/// </para>
/// <para>
/// <b>No heuristic, and that is not an oversight.</b> A\* prunes toward one goal; a one-to-all has
/// <c>n − 1</c> of them and settles the whole graph regardless. R0's <c>Chebyshev</c> verdict governs
/// the point-to-point denominator and does not reach here — which is worth saying because it makes
/// the build cost independent of District count per search, so the cold-build curve is linear in
/// <c>n</c> by construction and any departure from that is an artefact.
/// </para>
/// <para>
/// <b>Generation stamping rather than clearing</b>, for the same reason <c>PointToPoint</c> does it:
/// at thousands of searches over a 16,697-node graph a per-search <c>Array.Clear</c> would be a
/// visible share of the measurement.
/// </para>
/// </remarks>
internal sealed class OneToAll
{
    /// <summary>
    /// Larger than any real path cost and small enough that costs may be summed without wrapping.
    /// The same value and the same argument as <c>PointToPoint</c>'s.
    /// </summary>
    public const int Unreachable = 1 << 29;

    private readonly RoadGraph _graph;
    private readonly int[] _cost;
    private readonly int[] _touched;
    private readonly int[] _closed;
    private readonly int[] _cameFromArc;

    private int[] _heapKey;
    private int[] _heapNode;
    private int _heapCount;
    private int _generation;

    public OneToAll(RoadGraph graph)
    {
        _graph = graph;
        _cost = new int[graph.Nodes];
        _touched = new int[graph.Nodes];
        _closed = new int[graph.Nodes];
        _cameFromArc = new int[graph.Nodes];
        _heapKey = new int[4096];
        _heapNode = new int[4096];
    }

    /// <summary>Nodes settled by the last <see cref="Run"/>. The build's own work figure.</summary>
    public int NodesSettled { get; private set; }

    /// <summary>Settles every node reachable from <paramref name="source"/>.</summary>
    public void Run(int source, int[] arcTicks)
    {
        _generation++;
        _heapCount = 0;
        NodesSettled = 0;

        _cost[source] = 0;
        _touched[source] = _generation;
        _cameFromArc[source] = -1;
        Push(0, source);

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

            for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
            {
                int step = arcTicks[arc];
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
                Push(candidate, next);
            }
        }
    }

    /// <summary>The settled cost at a node, or <see cref="Unreachable"/>.</summary>
    public int CostOf(int node) =>
        _touched[node] == _generation && _closed[node] == _generation ? _cost[node] : Unreachable;

    /// <summary>
    /// Segments on the settled path back to the source. Walked for the route-size figure, never
    /// inside a timed build.
    /// </summary>
    public int PathSegments(int node)
    {
        if (_touched[node] != _generation)
        {
            return 0;
        }

        int segments = 0;
        int at = node;
        while (_cameFromArc[at] >= 0)
        {
            int arc = _cameFromArc[at];
            segments++;

            int a = _graph.SegmentNodeA[_graph.ArcSegment[arc]];
            int b = _graph.SegmentNodeB[_graph.ArcSegment[arc]];
            at = _graph.ArcTarget[arc] == a ? b : a;

            if (segments > _graph.Segments)
            {
                break;
            }
        }

        return segments;
    }

    /// <summary>
    /// Whether the settled path back to the source passes through any node the predicate marks.
    /// The sound dirty-region test, and the one that needs the route to exist.
    /// </summary>
    public bool PathTouches(int node, bool[] dirtyNode)
    {
        if (_touched[node] != _generation)
        {
            return false;
        }

        int at = node;
        int steps = 0;
        while (true)
        {
            if (dirtyNode[at])
            {
                return true;
            }

            if (_cameFromArc[at] < 0 || steps++ > _graph.Segments)
            {
                return false;
            }

            int arc = _cameFromArc[at];
            int a = _graph.SegmentNodeA[_graph.ArcSegment[arc]];
            int b = _graph.SegmentNodeB[_graph.ArcSegment[arc]];
            at = _graph.ArcTarget[arc] == a ? b : a;
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
