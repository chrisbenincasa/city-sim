using S2.Routing.Cluster;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Storm;

/// <summary>What one cache lookup did.</summary>
internal enum Lookup
{
    /// <summary>The key was not resident.</summary>
    Miss,

    /// <summary>Resident, and the Epoch rung said it was still good.</summary>
    Hit,

    /// <summary>Resident, and the Epoch rung said it might be stale. The cost the ladder is about.</summary>
    Stale,
}

/// <summary>
/// A fixed-capacity, origin-destination-keyed route cache, revalidated against an
/// <see cref="EpochClock"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>adr/0012</c> permits route caching keyed by origin-destination pair, never by agent, and
/// <c>adr/0006</c> calls exactly that shape dangerous</b> — its reversal criteria are <i>"Nothing."</i>
/// So this is fixed-capacity by construction: no dictionary, no growth, nothing that scales with
/// elapsed time. A cache that grows is not a cache.
/// </para>
/// <para>
/// <b>The key is the node pair, not the Access Point pair, and R5 states it beside every figure.</b>
/// <c>adr/0012</c> says only <i>"keyed by origin-destination pair"</i>, written before anyone knew an
/// Access Point is a <c>(Segment, offset)</c>. Keyed on those the space is Buildings² ≈ 2.25 × 10¹⁰
/// and the hit rate is approximately zero; keyed on the endpoints of the origin and destination
/// Segments it collapses to nodes², and the five Buildings sharing a Segment share one entry instead
/// of minting five. <b>Hit rate is a property of the key before it is a property of the
/// distribution.</b> <b>R6 owns this decision</b> — R5 adopts the node-pair key because a
/// Buildings²-keyed cache has no hit rate to compare rungs with, and says so rather than presenting
/// the choice as settled.
/// </para>
/// <para>
/// <b>Eviction is direct-mapped, which is not <c>adr/0017</c>'s pattern and is deliberately not
/// hiding.</b> <c>adr/0017</c> shows fixed capacity with least-used eviction and nobody has written
/// it down for routes; that is R6's to decide. Direct mapping lowers the absolute hit rate against a
/// least-used policy, so <b>no absolute here should be quoted as the hit rate a route cache
/// achieves</b>. It applies identically to all three Epoch rungs, which is what makes the ladder
/// comparison — R5's actual question — unaffected by it.
/// </para>
/// <para>
/// <b>A route longer than the slot is refused rather than truncated, and the refusals are counted.</b>
/// A truncated route is a wrong route that looks like a cached one. The count is expected to read
/// zero and is printed anyway, which is this spike's fifth instance of the argument for reporting a
/// quantity you expect to be boring.
/// </para>
/// </remarks>
internal sealed class RouteCache
{
    private readonly RoadGraph _graph;
    private readonly Clusters _clusters;
    private readonly int _capacity;
    private readonly int _maxArcs;
    private readonly int _maxClusters;

    private readonly long[] _key;
    private readonly bool[] _occupied;
    private readonly int[] _arc;
    private readonly int[] _arcCount;
    private readonly int[] _cluster;
    private readonly int[] _clusterCount;
    private readonly int[] _stampGlobal;
    private readonly int[] _stampCluster;
    private readonly int[] _stampSegment;

    public RouteCache(
        RoadGraph graph, Clusters clusters, int capacity, int maxArcs, int maxClusters)
    {
        _graph = graph;
        _clusters = clusters;
        _capacity = capacity;
        _maxArcs = maxArcs;
        _maxClusters = maxClusters;

        _key = new long[capacity];
        _occupied = new bool[capacity];
        _arc = new int[(long)capacity * maxArcs <= int.MaxValue ? capacity * maxArcs : 0];
        _arcCount = new int[capacity];
        _cluster = new int[capacity * maxClusters];
        _clusterCount = new int[capacity];
        _stampGlobal = new int[capacity];
        _stampCluster = new int[capacity];
        _stampSegment = new int[capacity];
    }

    /// <summary>Routes refused because they exceeded the slot. Expected zero; printed regardless.</summary>
    public int Refused { get; private set; }

    /// <summary>Entries evicted by a colliding key. The direct-mapped policy's own cost.</summary>
    public int Evicted { get; private set; }

    /// <summary>
    /// Occupied slots discarded by <see cref="Expire"/>. <b>Reported, not charged</b> — the
    /// rotation's real bill is the misses it causes, which the Tick timings already carry. It is
    /// printed so a rotation that is expiring nothing (or expiring an empty cache, which looks
    /// identical in the hit column) can be told apart from one that is working.
    /// </summary>
    public int Expired { get; private set; }

    /// <summary>Revalidation words read on the last lookup — 1, clusters crossed, or path length.</summary>
    public int LastRevalidationWork { get; private set; }

    public long ResidentBytes =>
        ((long)_key.Length * sizeof(long))
        + ((long)_arc.Length + _cluster.Length + (_capacity * 5)) * sizeof(int)
        + _capacity;

    /// <summary>
    /// The node pair a query keys on. <b>Canonicalised on Segment endpoint A</b>, so two Access
    /// Points on the same Segment share an entry — which is the whole point of the key.
    /// </summary>
    public long KeyOf(AccessPoint origin, AccessPoint destination) =>
        ((long)_graph.SegmentNodeA[origin.Segment] << 32)
        | (uint)_graph.SegmentNodeA[destination.Segment];

    /// <summary>
    /// The arcs stored in a slot, in travel order. <b>For the addition measurement</b>, which has to
    /// price what a rung declared valid against what the graph now actually offers.
    /// </summary>
    public ReadOnlySpan<int> ArcsAt(int slot) =>
        _arc.AsSpan(slot * _maxArcs, _arcCount[slot]);

    public Lookup TryGet(long key, EpochRung rung, EpochClock clock, out int slot)
    {
        slot = Slot(key);
        LastRevalidationWork = 0;

        if (!_occupied[slot] || _key[slot] != key)
        {
            return Lookup.Miss;
        }

        return Valid(slot, rung, clock) ? Lookup.Hit : Lookup.Stale;
    }

    public void Insert(long key, List<int> arcs, EpochRung rung, EpochClock clock)
    {
        if (arcs.Count > _maxArcs)
        {
            Refused++;
            return;
        }

        int slot = Slot(key);

        if (_occupied[slot] && _key[slot] != key)
        {
            Evicted++;
        }

        _occupied[slot] = true;
        _key[slot] = key;
        _arcCount[slot] = arcs.Count;

        int arcBase = slot * _maxArcs;
        for (int i = 0; i < arcs.Count; i++)
        {
            _arc[arcBase + i] = arcs[i];
        }

        // The distinct clusters the route crosses, recorded at insert. Storing them is what makes
        // the per-cluster rung O(clusters crossed) rather than O(path length) — deriving them from
        // the arcs at lookup time would cost exactly what the per-Segment rung costs and the rung
        // would have no reason to exist. The storage is the rung's real price and it is reported.
        int clusterBase = slot * _maxClusters;
        int clusterCount = 0;
        int previous = -1;

        foreach (int arc in arcs)
        {
            int cluster = _clusters.OfNode[_graph.ArcTarget[arc]];

            if (cluster == previous)
            {
                continue;
            }

            previous = cluster;

            bool seen = false;
            for (int i = 0; i < clusterCount; i++)
            {
                if (_cluster[clusterBase + i] == cluster)
                {
                    seen = true;
                    break;
                }
            }

            if (!seen && clusterCount < _maxClusters)
            {
                _cluster[clusterBase + clusterCount++] = cluster;
            }
        }

        _clusterCount[slot] = clusterCount;
        _stampGlobal[slot] = clock.Global;
        _stampCluster[slot] = MaxClusterVersion(slot, clock);
        _stampSegment[slot] = MaxSegmentVersion(slot, clock);
    }

    /// <summary>
    /// Expire a contiguous slice of slots — the TTL, spelled as a <b>rotation</b> rather than as a
    /// per-entry timestamp.
    /// </summary>
    /// <remarks>
    /// <para>
    /// R5.4 measured a hole no Epoch rung closes: a route computed before a road existed cannot
    /// contain that road, so nothing the per-Segment rung watches will ever move and the entry is
    /// stale <b>permanently</b>. R5.4's option C is to invalidate on a cadence instead, which bounds
    /// staleness in time without needing to detect anything. This is that, and the design decision
    /// worth naming is the spelling.
    /// </para>
    /// <para>
    /// <b>A rotation rather than an expiry check.</b> A per-entry timestamp tested at lookup lets
    /// every entry inserted during a busy Tick expire during one later Tick, which is a spike — and
    /// R5.3 has already measured that this cache's exposure is the <i>worst</i> Tick rather than the
    /// mean one. Sweeping a fixed-width window makes the refresh rate constant by construction:
    /// expire <c>capacity / period</c> slots a Tick and every entry is refreshed within
    /// <c>period</c> Ticks, with the cost flat. It is also the shape R4.7 already priced for the
    /// other consumer, so the two rotations are one mechanism rather than two.
    /// </para>
    /// <para>
    /// <b>The cost is not modelled here and must not be.</b> Expiring a slot costs a write; what it
    /// actually costs is the <i>miss</i> the next lookup on that key takes, and that lands in the
    /// Tick timings and the miss column where the storm can charge for it. A rotation priced by
    /// counting evictions would be a rotation priced by argument.
    /// </para>
    /// </remarks>
    public void Expire(int fromSlot, int count)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = (fromSlot + i) % _capacity;

            if (_occupied[slot])
            {
                _occupied[slot] = false;
                Expired++;
            }
        }
    }

    /// <summary>
    /// <b>Versions are monotonic, so a maximum is a sound summary and a cheap one.</b> If any
    /// watched counter has moved since the stamp was taken, the maximum over the same set has
    /// increased; if none has, it has not. That is what lets an entry hold one word per rung instead
    /// of a vector, and it is exact rather than probabilistic.
    /// </summary>
    private bool Valid(int slot, EpochRung rung, EpochClock clock)
    {
        switch (rung)
        {
            case EpochRung.Global:
                LastRevalidationWork = 1;
                return _stampGlobal[slot] == clock.Global;

            case EpochRung.PerCluster:
                LastRevalidationWork = _clusterCount[slot];
                return MaxClusterVersion(slot, clock) == _stampCluster[slot];

            default:
                LastRevalidationWork = _arcCount[slot];
                return MaxSegmentVersion(slot, clock) == _stampSegment[slot];
        }
    }

    private int MaxClusterVersion(int slot, EpochClock clock)
    {
        int clusterBase = slot * _maxClusters;
        int max = 0;

        for (int i = 0; i < _clusterCount[slot]; i++)
        {
            int version = clock.ClusterVersion[_cluster[clusterBase + i]];
            max = version > max ? version : max;
        }

        return max;
    }

    private int MaxSegmentVersion(int slot, EpochClock clock)
    {
        int arcBase = slot * _maxArcs;
        int max = 0;

        for (int i = 0; i < _arcCount[slot]; i++)
        {
            int version = clock.SegmentVersion[_graph.ArcSegment[_arc[arcBase + i]]];
            max = version > max ? version : max;
        }

        return max;
    }

    private int Slot(long key)
    {
        ulong mixed = (ulong)key * 0x9E37_79B9_7F4A_7C15UL;
        mixed ^= mixed >> 29;
        return (int)(mixed % (ulong)_capacity);
    }
}
