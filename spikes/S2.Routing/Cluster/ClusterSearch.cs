using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Cluster;

/// <summary>
/// Dijkstra confined to one or two clusters, forwards or backwards. HPA\*'s only concrete search.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three jobs, one implementation, and that is deliberate</b> — it builds the abstract graph's
/// intra-edges, it repairs them after an edit, and it inserts the origin and the goal at query time.
/// R2's harness published two rungs that agreed to the last digit because the experiment had quietly
/// removed the difference it existed to measure; the counter-discipline is to let the numbers differ
/// only where the design differs, and three copies of a bounded Dijkstra would be three places for a
/// difference to come from.
/// </para>
/// <para>
/// <b>Backwards is not an optimisation.</b> Inserting the goal asks <c>cost(portal → goal)</c>, and a
/// forward search cannot answer it — see <see cref="ReverseArcs"/>.
/// </para>
/// <para>
/// <b>Confinement is what makes the hierarchy a hierarchy, and it is also where its detour comes
/// from.</b> A route whose true optimum leaves the origin's cluster and comes back is not visible to
/// this search, so HPA\* returns the best route that respects the partition. That is the standard
/// suboptimality, and R3 measures it rather than quoting the literature's figure for it.
/// </para>
/// </remarks>
internal sealed class ClusterSearch
{
    public const int Unreachable = SegmentEntry.Unreachable;

    private readonly RoadGraph _graph;
    private readonly Clusters _clusters;
    private readonly ReverseArcs _reverse;
    private readonly int[] _arcCost;

    private readonly int[] _cost;
    private readonly int[] _touched;
    private readonly int[] _closed;
    private readonly int[] _cameFromArc;

    private int[] _heapKey;
    private int[] _heapNode;
    private int _heapCount;

    private int _generation;
    private int _allowedA;
    private int _allowedB;
    private bool _backward;

    public ClusterSearch(RoadGraph graph, Clusters clusters, ReverseArcs reverse, int[] arcCost)
    {
        _graph = graph;
        _clusters = clusters;
        _reverse = reverse;
        _arcCost = arcCost;
        _cost = new int[graph.Nodes];
        _touched = new int[graph.Nodes];
        _closed = new int[graph.Nodes];
        _cameFromArc = new int[graph.Nodes];
        _heapKey = new int[1024];
        _heapNode = new int[1024];
    }

    /// <summary>Nodes settled by the last <see cref="Run"/>.</summary>
    public int Expanded { get; private set; }

    /// <summary>Arcs examined by the last <see cref="Run"/>. Beside the clock, never instead.</summary>
    public int Relaxed { get; private set; }

    /// <summary>
    /// Starts a search confined to <paramref name="clusterA"/> and, if it is not <c>-1</c>,
    /// <paramref name="clusterB"/>. Two are allowed because an Access Point's Segment may straddle a
    /// cluster boundary, so its two endpoints can sit on opposite sides of one.
    /// </summary>
    public void Begin(int clusterA, int clusterB, bool backward)
    {
        _generation++;
        _heapCount = 0;
        _allowedA = clusterA;
        _allowedB = clusterB;
        _backward = backward;
        Expanded = 0;
        Relaxed = 0;
    }

    public void Seed(int node, int cost)
    {
        if (cost >= Unreachable || !Allowed(node))
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
        Push(cost, node);
    }

    /// <summary>Exhausts the confined region. There is no goal — every caller wants many answers.</summary>
    public void Run()
    {
        while (_heapCount > 0)
        {
            (int key, int node) = Pop();

            if (_closed[node] == _generation || key > _cost[node])
            {
                continue;
            }

            _closed[node] = _generation;
            Expanded++;

            int here = _cost[node];

            if (_backward)
            {
                for (int slot = _reverse.IncomingStart[node]; slot < _reverse.IncomingStart[node + 1]; slot++)
                {
                    int arc = _reverse.Incoming[slot];
                    Relax(_reverse.Source[arc], arc, here);
                }
            }
            else
            {
                for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
                {
                    Relax(_graph.ArcTarget[arc], arc, here);
                }
            }
        }
    }

    /// <summary>The cost to (or, backwards, from) a node, or <see cref="Unreachable"/>.</summary>
    public int CostOf(int node) => _touched[node] == _generation ? _cost[node] : Unreachable;

    /// <summary>The arc a node was reached by, or <c>-1</c> for a seed. The refinement's thread.</summary>
    public int CameFromArc(int node) => _touched[node] == _generation ? _cameFromArc[node] : -1;

    /// <summary>The node on the other end of an arc, in the direction this search walks.</summary>
    public int Previous(int arc) => _backward ? _graph.ArcTarget[arc] : _reverse.Source[arc];

    private void Relax(int next, int arc, int here)
    {
        Relaxed++;

        int step = _arcCost[arc];
        if (step == RoadGraph.Impassable || !Allowed(next) || _closed[next] == _generation)
        {
            return;
        }

        int candidate = here + step;
        if (_touched[next] == _generation && _cost[next] <= candidate)
        {
            return;
        }

        _cost[next] = candidate;
        _touched[next] = _generation;
        _cameFromArc[next] = arc;
        Push(candidate, next);
    }

    private bool Allowed(int node)
    {
        int cluster = _clusters.OfNode[node];
        return cluster == _allowedA || cluster == _allowedB;
    }

    // --- Binary heap. Lazy deletion, as PointToPoint's is and for the same reason. -----------------

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
