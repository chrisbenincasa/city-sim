using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Core.Parking;

/// <summary>
/// The reusable state of one Parking Shed query: distances in <see cref="Tiles"/>, a visit stamp, a
/// binary heap, the Segments the ball touched, and the nearest few Car Parks it kept.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="Movement.WalkScratch"/>'s posture in a different currency, and the currency is the
/// reason it is a second type rather than a mode on the first.</b> A walk search accumulates
/// <c>TravelTime</c> because a walk's answer is what it costs; a shed accumulates <see cref="Tiles"/>
/// because membership is geometric — <c>[parking] radius_metres</c> is a distance, and a Car Park is
/// in the shed or is not however fast anybody walks. Merging them behind a flag would put a
/// <em>unit</em> in a parameter, which is the mistake <c>adr/0094</c> caught twice in one build.
/// </para>
/// <para>
/// <b>Grown to the largest graph seen and never shrunk</b>, bounded by the size of the world rather
/// than by elapsed time, so <c>adr/0006</c> is satisfied structurally. <b>One instance per caller,
/// never shared across threads</b> — <c>WalkScratch</c>'s reason exactly.
/// </para>
/// </remarks>
public sealed class ShedScratch
{
    /// <summary>A node the ball never reached. Distinct from a distance of zero, which is the seed.</summary>
    public static Tiles Unreached => new(int.MaxValue);

    private Tiles[] _distance = [];
    private int[] _stamp = [];
    private bool[] _settled = [];

    private Tiles[] _heapCost = [];
    private int[] _heapNode = [];
    private int _heapCount;

    private int[] _segmentStamp = [];
    private int[] _doneStamp = [];
    private int[] _touched = [];
    private int _touchedCount;

    private Tiles[] _keptDistance = [];
    private int[] _keptSlot = [];
    private int _keptCount;
    private int _keep;

    private int _generation;
    private int _nodes;
    private int _segments;

    /// <summary>How many nodes the last ball settled. The witness <c>adr/0083</c> asks for.</summary>
    public int Settled { get; private set; }

    /// <summary>The Segments the last ball touched, in the order it met them.</summary>
    /// <remarks>
    /// <b>Order of meeting is not order of distance</b>, and nothing downstream may assume otherwise —
    /// the ordering of the answer is <see cref="Offer"/>'s job, on <c>(distance, slot)</c>, precisely
    /// so that it does not depend on this.
    /// </remarks>
    public ReadOnlySpan<int> Touched => _touched.AsSpan(0, _touchedCount);

    /// <summary>Clears the state for a graph of this size, keeping at most <paramref name="keep"/>.</summary>
    /// <remarks>
    /// <b>A stamp rather than a clear</b>, for <c>WalkScratch.Begin</c>'s reason: a shed touches a few
    /// dozen nodes of a graph with hundreds of thousands, and zeroing the arrays would make the
    /// cheapest query in the city cost a pass over all of it.
    /// </remarks>
    public void Begin(int nodeCount, int segmentCount, int keep)
    {
        if (_distance.Length < nodeCount)
        {
            _distance = new Tiles[nodeCount];
            _stamp = new int[nodeCount];
            _settled = new bool[nodeCount];
            _heapCost = new Tiles[nodeCount + 1];
            _heapNode = new int[nodeCount + 1];
        }

        if (_segmentStamp.Length < segmentCount)
        {
            _segmentStamp = new int[segmentCount];
            _doneStamp = new int[segmentCount];
            _touched = new int[segmentCount];
        }

        if (_keptDistance.Length < keep)
        {
            _keptDistance = new Tiles[keep];
            _keptSlot = new int[keep];
        }

        _nodes = nodeCount;
        _segments = segmentCount;
        _heapCount = 0;
        _touchedCount = 0;
        _keptCount = 0;
        _keep = keep;
        Settled = 0;

        // WalkScratch's ceiling case, and it has to clear every stamp array because they are all read
        // against the same counter.
        if (_generation == int.MaxValue)
        {
            Array.Clear(_stamp);
            Array.Clear(_segmentStamp);
            Array.Clear(_doneStamp);
            _generation = 0;
        }

        _generation++;
    }

    /// <summary>Opens the ball at a node, or improves it, so long as it is inside the radius.</summary>
    public void Seed(int node, Tiles reached, Tiles radius)
    {
        if ((uint)node >= (uint)_nodes || reached > radius)
        {
            return;
        }

        bool seen = _stamp[node] == _generation;

        if (seen && (_settled[node] || _distance[node] <= reached))
        {
            return;
        }

        if (!seen)
        {
            _stamp[node] = _generation;
            _settled[node] = false;
        }

        _distance[node] = reached;

        Push(reached, node);
    }

    /// <summary>Takes the nearest unsettled node, or reports the ball is finished.</summary>
    public bool TryTake(out int node, out Tiles reached)
    {
        while (_heapCount > 0)
        {
            reached = _heapCost[0];
            node = _heapNode[0];

            PopRoot();

            if (!_settled[node])
            {
                return true;
            }
        }

        node = -1;
        reached = Unreached;

        return false;
    }

    /// <summary>Marks a node settled, so its distance is final.</summary>
    public void Settle(int node)
    {
        _settled[node] = true;
        Settled++;
    }

    /// <summary>What the ball settled a node at, or <see cref="Unreached"/>.</summary>
    /// <remarks>
    /// <b>Settled rather than merely relaxed</b> — a frontier node's distance is tentative, so reading
    /// one would give an answer right in form and too large in value. This is read after the ball
    /// stops, when every reachable node inside the radius is settled.
    /// </remarks>
    public Tiles DistanceAt(int node) =>
        (uint)node < (uint)_nodes && _stamp[node] == _generation && _settled[node]
            ? _distance[node]
            : Unreached;

    /// <summary>Records that the ball met a Segment. Idempotent.</summary>
    public void Touch(int segment)
    {
        if ((uint)segment >= (uint)_segments || _segmentStamp[segment] == _generation)
        {
            return;
        }

        _segmentStamp[segment] = _generation;
        _touched[_touchedCount] = segment;
        _touchedCount++;
    }

    /// <summary>
    /// Offers a Car Park to the kept set, which holds the nearest <c>keep</c> of everything offered.
    /// </summary>
    /// <remarks>
    /// <b>Ordered on <c>(distance, slot)</c> and never on arrival</b>, which is what makes the answer a
    /// function of the graph and the Addresses rather than of the order the ball happened to meet
    /// Segments in. An insertion sort over a handful of entries beats a heap comfortably at this size
    /// and is the only shape that keeps the whole set sorted for the caller.
    /// </remarks>
    public void Offer(Tiles reached, int slot)
    {
        if (_keep == 0)
        {
            return;
        }

        if (_keptCount == _keep && !Precedes(reached, slot, _keptDistance[_keptCount - 1], _keptSlot[_keptCount - 1]))
        {
            return;
        }

        int at = _keptCount == _keep ? _keptCount - 1 : _keptCount;

        while (at > 0 && Precedes(reached, slot, _keptDistance[at - 1], _keptSlot[at - 1]))
        {
            _keptDistance[at] = _keptDistance[at - 1];
            _keptSlot[at] = _keptSlot[at - 1];
            at--;
        }

        _keptDistance[at] = reached;
        _keptSlot[at] = slot;

        if (_keptCount < _keep)
        {
            _keptCount++;
        }
    }

    /// <summary>
    /// Whether the kept set is full, so <see cref="KeptWorst"/> is a bound the ball may stop on.
    /// </summary>
    /// <remarks>
    /// <b>Full rather than non-empty</b>, and the difference is the whole correctness of the early
    /// exit: a kept set with room in it will accept anything the ball finds next, however far away, so
    /// its last member bounds nothing. A caller that keeps nothing (<c>keep == 0</c>) is never full,
    /// which is what makes an uncapped query walk the whole radius exactly as it did before.
    /// </remarks>
    public bool KeptFull => _keep > 0 && _keptCount == _keep;

    /// <summary>How far the last kept Car Park is. Read only when <see cref="KeptFull"/>.</summary>
    public Tiles KeptWorst => _keptDistance[_keptCount - 1];

    /// <summary>Whether this Segment's Car Parks have already been offered.</summary>
    /// <remarks>
    /// <b>A second stamp rather than a flag on <see cref="Touched"/></b>, because the two facts differ:
    /// a Segment is <em>touched</em> when the ball meets it and <em>done</em> when both its endpoints are
    /// settled and its Car Parks have been priced. Every done Segment is touched and the reverse is
    /// false — the ones left over are exactly what the pass after the ball is for.
    /// </remarks>
    public bool Done(int segment) =>
        (uint)segment < (uint)_segments && _doneStamp[segment] == _generation;

    /// <summary>Records that this Segment's Car Parks have been offered, so nothing offers them twice.</summary>
    public void Finish(int segment)
    {
        if ((uint)segment < (uint)_segments)
        {
            _doneStamp[segment] = _generation;
        }
    }

    /// <summary>Writes the kept Car Parks, nearest first, and reports how many.</summary>
    public int Write(Span<int> into)
    {
        for (int i = 0; i < _keptCount; i++)
        {
            into[i] = _keptSlot[i];
        }

        return _keptCount;
    }

    /// <summary>How far the <paramref name="index"/>th kept Car Park is. For a measurement.</summary>
    public Tiles KeptAt(int index) => _keptDistance[index];

    private void Push(Tiles cost, int node)
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

        Tiles cost = _heapCost[_heapCount];
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

    /// <summary>Nearer first, and the lower slot when two are equally near.</summary>
    /// <remarks>
    /// <c>WalkScratch.Precedes</c>' reason, and it bites harder here: a generated city is a grid of
    /// identical Streets with a Car Park on every Building, so <em>most</em> comparisons in a shed are
    /// ties.
    /// </remarks>
    private static bool Precedes(Tiles leftCost, int leftSlot, Tiles rightCost, int rightSlot) =>
        leftCost == rightCost ? leftSlot < rightSlot : leftCost < rightCost;
}
