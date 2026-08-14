using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// A fixed-capacity, four-way set-associative store of computed routes, keyed by <b>a pair of
/// nodes</b> — <c>adr/0012</c>'s caching bullet, built rather than quoted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Keyed by pair and never by agent, which is the whole of <c>adr/0012</c>.</b> Sharing a
/// <em>computed route</em> between two Citizens travelling the same pair is fine; sharing the
/// <em>choice of destination</em> is not. Nothing in this class knows a Citizen exists.
/// </para>
/// <para>
/// <b>Nodes and not Buildings, and the endpoint is the caller's to choose.</b> R6.1a: the naive
/// choice — always a Segment's <c>a</c> end — costs exactly <b>2×</b> the nearest end on every rung,
/// geometrically, and the fix is one comparison at insert with the key space unchanged. That
/// comparison belongs at the call site, because only the caller holds the Address the route is
/// really between. ⚠ <b>A caller that picks its endpoint inconsistently gets a correct route and a
/// halved hit rate</b>, and nothing here can detect it.
/// </para>
/// <para>
/// <b>Four ways, indexed on the high bits.</b> Conflict misses fall 20.0% → 10.6% → <b>3.8%</b> →
/// 1.4% across 1, 2, 4 and 8 ways against a fully-associative bound of 0.0%, and four ways recovers
/// most of the gap at four contiguous probes. The high-bit index is a **robustness** fix rather than
/// a throughput one — level-or-worse on random keys, worth 31.2% → 21.7% on a concentrated
/// destination pool — which is the case a city produces and a uniform draw does not.
/// </para>
/// <para>
/// <b>The removal test is exact and is the first reader the per-Segment Epoch has ever had.</b> An
/// entry stores each Segment's handle and the Epoch it was computed under; a lookup rejects the entry
/// if any handle has gone stale or any Epoch has moved. That is containment — <i>does this route
/// contain that Segment</i> — rather than geometry, so there is no interval during which a Traveller
/// may drive through a bulldozed road. <b>The handle is load-bearing and the Epoch alone would not
/// do</b>: a freed slot is recycled and a new Segment opens at Epoch 1, so a stored Epoch of 1 on a
/// recycled slot is a false hit.
/// </para>
/// <para>
/// <b>What it is allowed to be wrong about is <see cref="RouteStaleness"/>, and that is a switch
/// because it is measurable.</b> See that type.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, not a <c>[Table]</c>, and it never joins <c>World._tables</c></b>
/// — <see cref="RoutingPartition"/>'s argument and <c>plans/0020</c>'s reason, unchanged. ⚠ <b>Under
/// <see cref="RouteStaleness.Exact"/> that is also true of its <em>contents</em></b>: a hit returns
/// what a miss would compute, so an empty cache and a warm one produce the same city and the save
/// says nothing about it. Under the other two rungs it is not, and they would need saving before
/// anything in the Tick may read them.
/// </para>
/// </remarks>
public sealed class RouteCache
{
    /// <summary>Entries per set. R6's measured knee — see the type's remarks.</summary>
    public const int Ways = 4;

    /// <summary>An empty slot, and a length no real route can have.</summary>
    private const int Empty = -1;

    private readonly int _sets;
    private readonly int _stride;
    private readonly RouteStaleness _policy;

    private readonly ulong[] _keyOrigin;
    private readonly ulong[] _keyDestination;
    private readonly int[] _length;
    private readonly ulong[] _used;

    private readonly int[] _segment;
    private readonly Handle<RoadSegment>[] _handle;
    private readonly uint[] _epoch;

    /// <summary>
    /// Where a route too long for the store is extracted to, so the caller still gets a correct one.
    /// </summary>
    /// <remarks>
    /// <b>Outside the store, so an uncacheable route never evicts a cacheable one.</b> It is
    /// overwritten by the next overflow, which is the same lifetime a hit's span has, so the contract
    /// a caller sees is uniform: the span is valid until the next call either way.
    /// </remarks>
    private int[] _overflow = [];

    private ulong _clock;
    private uint _seenVersion;
    private int _rotor;

    /// <param name="entries">
    /// How many routes the store holds. Rounded down to a multiple of <see cref="Ways"/>.
    /// </param>
    /// <param name="maxSegments">
    /// The longest route an entry can hold. A longer one is returned and not stored — see
    /// <see cref="TooLong"/>.
    /// </param>
    /// <param name="policy">What an entry may be wrong about after a road is added.</param>
    public RouteCache(int entries, int maxSegments, RouteStaleness policy)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(entries, Ways);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSegments, 1);

        _sets = IntegerMath.FloorDiv(entries, Ways);
        _stride = maxSegments;
        _policy = policy;

        int slots = _sets * Ways;

        _keyOrigin = new ulong[slots];
        _keyDestination = new ulong[slots];
        _length = new int[slots];
        _used = new ulong[slots];

        _segment = new int[slots * _stride];
        _handle = new Handle<RoadSegment>[slots * _stride];
        _epoch = new uint[slots * _stride];

        Array.Fill(_length, Empty);
    }

    /// <summary>Lookups served from the store without searching.</summary>
    public int Hits { get; private set; }

    /// <summary>Lookups that had to search. The sum of a cold key, an eviction and an invalidation.</summary>
    public int Misses { get; private set; }

    /// <summary>Entries rejected because a Segment they contain was removed or edited.</summary>
    /// <remarks>
    /// <b>Counted separately from a plain miss because it is the only one the player caused.</b> A
    /// cold miss is the store warming up and an eviction is the store being too small; this is a road
    /// edit reaching a route, and it is the quantity the staleness rungs differ in.
    /// </remarks>
    public int Invalidations { get; private set; }

    /// <summary>Entries displaced by a new one landing in a full set.</summary>
    public int Evictions { get; private set; }

    /// <summary>Routes returned but not stored, because they exceed the stride.</summary>
    /// <remarks>
    /// <b>An uncacheable route is a permanent miss, so this is a bound on the achievable hit rate and
    /// not a detail.</b> It is reported rather than absorbed because the stride is a chosen number and
    /// this is the count that says whether it was chosen too small.
    /// </remarks>
    public int TooLong { get; private set; }

    /// <summary>Entries dropped by <see cref="Rotate"/> rather than by an edit or an eviction.</summary>
    public int Rotated { get; private set; }

    /// <summary>Whole-store discards under <see cref="RouteStaleness.Exact"/>.</summary>
    public int Flushes { get; private set; }

    /// <summary>How many entries currently hold a route.</summary>
    public int Resident
    {
        get
        {
            int count = 0;

            for (int slot = 0; slot < _length.Length; slot++)
            {
                if (_length[slot] != Empty)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>The staleness rung this store was built with.</summary>
    public RouteStaleness Policy => _policy;

    /// <summary>The longest route an entry can hold.</summary>
    public int Stride => _stride;

    /// <summary>
    /// The route between two nodes, from the store if it is there and correct, by searching if not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The answer is the same either way and that is the contract.</b> A caller cannot tell a hit
    /// from a miss except by reading the counters, which is what lets <see cref="RouteStaleness.Exact"/>
    /// stay invisible to the State Hash.
    /// </para>
    /// <para>
    /// <b>Returns a span into the store, valid until the next call.</b> No allocation, and no copy for
    /// a caller that walks the route and discards it — which is every caller, because a Traveller
    /// holds a cursor rather than a list (<c>adr/0075</c>, amended).
    /// </para>
    /// </remarks>
    /// <param name="graph">The Road Graph to search, and to validate against.</param>
    /// <param name="origin">Node slot to start at.</param>
    /// <param name="destination">Node slot to end at.</param>
    /// <param name="mode">Which subgraph to traverse.</param>
    /// <param name="scratch">Reusable search state; one per caller, never shared across threads.</param>
    /// <param name="route">The Segments crossed, origin first. Empty if there is no route.</param>
    /// <returns>Whether a route exists at all.</returns>
    public bool Find(
        RoadGraph graph,
        int origin,
        int destination,
        TravelMode mode,
        WalkScratch scratch,
        out ReadOnlySpan<int> route)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(scratch);

        if (_policy == RouteStaleness.Exact && graph.Version != _seenVersion)
        {
            Clear();
            _seenVersion = graph.Version;
        }

        if (!graph.Nodes.Rows.IsLive(origin) || !graph.Nodes.Rows.IsLive(destination))
        {
            route = default;

            return false;
        }

        ulong from = graph.Nodes.Rows.IdAt(origin);
        ulong to = graph.Nodes.Rows.IdAt(destination);
        int set = SetOf(from, to);
        int first = set * Ways;

        for (int slot = first; slot < first + Ways; slot++)
        {
            if (_length[slot] == Empty || _keyOrigin[slot] != from || _keyDestination[slot] != to)
            {
                continue;
            }

            if (!Valid(graph, slot))
            {
                Drop(slot);
                Invalidations++;

                break;
            }

            _clock++;
            _used[slot] = _clock;
            Hits++;
            route = _segment.AsSpan(slot * _stride, _length[slot]);

            return true;
        }

        Misses++;

        return Insert(graph, origin, destination, mode, scratch, from, to, first, out route);
    }

    /// <summary>
    /// Drops <paramref name="count"/> entries, advancing a cursor around the store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The addition answer, and it is the only one available.</b> A new Segment is on no existing
    /// route, so containment has nothing to match and there is no sound targeted test — S2 R1.7
    /// measured proximity missing <b>309 of 429</b> changed entries on a central edit, and missing them
    /// silently. Dropping entries blindly and letting traffic recompute them is untargeted and
    /// therefore cannot miss anything.
    /// </para>
    /// <para>
    /// <b>A cursor rather than a draw, because the store must be swept and not sampled.</b> A random
    /// selection leaves a tail of entries the rotation has never reached, which is the hole the
    /// rotation exists to close; a cursor guarantees every slot is visited once per period. It is also
    /// why the period rather than the rate is the authored quantity — <c>store ÷ period</c> is what
    /// this is called with.
    /// </para>
    /// <para>
    /// ⚠ <b>Called by nothing in the Tick, and deliberately so at 5c task 4.</b> A rotation is only a
    /// teaching mechanism where traffic refills what it empties; wiring it in ahead of a route
    /// consumer would measure a cache being deleted.
    /// </para>
    /// </remarks>
    public void Rotate(int count)
    {
        int slots = _length.Length;

        for (int i = 0; i < count && i < slots; i++)
        {
            int slot = _rotor;
            _rotor = _rotor + 1 == slots ? 0 : _rotor + 1;

            if (_length[slot] == Empty)
            {
                continue;
            }

            Drop(slot);
            Rotated++;
        }
    }

    /// <summary>Discards every entry. The <see cref="RouteStaleness.Exact"/> rung's whole mechanism.</summary>
    public void Clear()
    {
        Array.Fill(_length, Empty);
        Flushes++;
    }

    /// <summary>Zeroes the counters without touching the contents. For a measurement window.</summary>
    public void ResetCounters()
    {
        Hits = 0;
        Misses = 0;
        Invalidations = 0;
        Evictions = 0;
        TooLong = 0;
        Rotated = 0;
        Flushes = 0;
    }

    /// <summary>
    /// The set a key falls in — <b>the high bits of the mix, not the low ones</b>.
    /// </summary>
    /// <remarks>
    /// <b>A robustness fix rather than a throughput one, and it only shows up on the input a city
    /// actually produces.</b> On random keys a high-bit index is level-or-worse; on a concentrated
    /// destination pool — everybody commuting to the same few blocks — it is worth 31.2% → 21.7%
    /// conflict misses. A uniform draw cannot see the difference, which is why the spike nearly did
    /// not make the change.
    /// </remarks>
    private int SetOf(ulong origin, ulong destination)
    {
        // splitmix64's finaliser. A named integer mix rather than a hash-map's, because
        // object.GetHashCode() is banned outright in Core and a shift-xor written inline would be a
        // fourth undocumented mixing function in this tree.
        ulong mixed = origin * 0x9E3779B97F4A7C15UL;
        mixed ^= destination + 0x9E3779B97F4A7C15UL + (mixed << 6) + (mixed >> 2);
        mixed ^= mixed >> 30;
        mixed *= 0xBF58476D1CE4E5B9UL;
        mixed ^= mixed >> 27;
        mixed *= 0x94D049BB133111EBUL;
        mixed ^= mixed >> 31;

        return (int)((mixed >> 32) % (uint)_sets);
    }

    /// <summary>Whether every Segment an entry names is still the Segment it was computed over.</summary>
    private bool Valid(RoadGraph graph, int slot)
    {
        int at = slot * _stride;
        int length = _length[slot];
        Rows<RoadSegment> rows = graph.Segments.Rows;

        for (int i = 0; i < length; i++)
        {
            if (!rows.IsValid(_handle[at + i])
                || graph.Segments.Epoch[_segment[at + i]] != _epoch[at + i])
            {
                return false;
            }
        }

        return true;
    }

    private void Drop(int slot) => _length[slot] = Empty;

    /// <summary>Searches, stores if it fits, and hands back the route either way.</summary>
    private bool Insert(
        RoadGraph graph,
        int origin,
        int destination,
        TravelMode mode,
        WalkScratch scratch,
        ulong from,
        ulong to,
        int first,
        out ReadOnlySpan<int> route)
    {
        int victim = Victim(first);
        int at = victim * _stride;

        scratch.Begin(graph.Nodes.Rows.SlotCount, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.Search(graph, mode, destination, destination, TravelTime.Zero, TravelTime.Zero);

        if (scratch.Arrived != destination)
        {
            route = default;

            return false;
        }

        int length = scratch.PathTo(graph.Arcs, destination, _segment.AsSpan(at, _stride));

        if (length > _stride)
        {
            // Returned and not stored, so this pair costs a search every time it is asked for. That
            // is a permanent hole in the achievable hit rate rather than a slow path, which is why it
            // is counted rather than absorbed.
            TooLong++;

            if (_overflow.Length < length)
            {
                _overflow = new int[length];
            }

            scratch.PathTo(graph.Arcs, destination, _overflow);
            route = _overflow.AsSpan(0, length);

            return true;
        }

        if (_length[victim] != Empty)
        {
            Evictions++;
        }

        for (int i = 0; i < length; i++)
        {
            int segment = _segment[at + i];
            _handle[at + i] = graph.Segments.Rows.At(segment);
            _epoch[at + i] = graph.Segments.Epoch[segment];
        }

        _keyOrigin[victim] = from;
        _keyDestination[victim] = to;
        _length[victim] = length;
        _clock++;
        _used[victim] = _clock;

        route = _segment.AsSpan(at, length);

        return true;
    }

    /// <summary>
    /// The slot a new entry takes: an empty one, else the least recently used of the four.
    /// </summary>
    /// <remarks>
    /// <b>Four contiguous probes on a cache line the entry already occupies</b>, which is why four ways
    /// costs what one way costs. The counter is a monotonic use clock rather than a timestamp, because
    /// there is no clock in <c>Borough.Core</c> — <c>DateTime</c> and <c>Stopwatch</c> are banned
    /// outright, and a Tick would make eviction order depend on when a lookup happened rather than on
    /// what happened.
    /// </remarks>
    private int Victim(int first)
    {
        int oldest = first;

        for (int slot = first; slot < first + Ways; slot++)
        {
            if (_length[slot] == Empty)
            {
                return slot;
            }

            if (_used[slot] < _used[oldest])
            {
                oldest = slot;
            }
        }

        return oldest;
    }
}
