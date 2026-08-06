using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Matrix;

namespace S2.Routing.Traffic;

/// <summary>Where a Traveller's next Segment comes from. R2's open axis.</summary>
/// <remarks>
/// <c>adr/0041</c> settled <i>attribution</i> and explicitly did not settle this: <i>"searched per
/// Trip or shared per origin-destination pair is a performance axis with no correctness content, and
/// spike S2 measures it."</i> R2 adds a third rung the ADR's own mechanism implies — see
/// <see cref="NextHopTable"/> — and reports that the axis has <b>some</b> correctness content after
/// all, because two of the three rungs aim a Traveller at a District representative rather than at
/// where it is actually going.
/// </remarks>
internal enum PathSource
{
    /// <summary>One A* per Leg, over the real <c>(Segment, offset)</c> Access Points.</summary>
    Searched,

    /// <summary>One cached route per ordered District pair. <c>03 §3.3</c>'s store.</summary>
    Shared,

    /// <summary>No path at all: one arc per <c>(node, District)</c>, read at every crossing.</summary>
    NextHop,
}

/// <summary>
/// Travellers advancing over the Road Graph, incrementing the Segment they enter and decrementing the
/// one they leave — <c>adr/0041</c>'s direct attribution, running.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a traffic simulator and <c>plans/0010</c> forbids it becoming one.</b> No Lanes, no
/// IDM, no Overlaps, no Switch Lanes: milestone 6 owns those and <c>adr/0016</c> already settles their
/// shape. A Traveller here is a token that consumes its arc's free-flow traversal time and moves on.
/// <b>Crucially there is no feedback</b> — the arc costs are the static congested field the routes
/// were computed on, so a jam does not slow the Travellers in it. That is a deliberate omission: a
/// feedback loop would put an unargued shape inside every lag figure R2b reports, and the lag is what
/// R2b exists to measure.
/// </para>
/// <para>
/// <b>Advance is by traversal time, not by one arc per Tick, and that changes a number the corpus
/// currently assumes.</b> <c>adr/0041</c> prices direct attribution from <i>"a vehicle crosses about
/// one Segment per Tick"</i>, and names the rate as its own revisit trigger: <i>"If the Segment turns
/// out much shorter than a block — S2 owns the road-density figure that decides it — the crossing rate
/// rises and this should be re-priced before it is re-argued."</i> R0 measured the density and the
/// mean Segment is 128 m at the placeholder rung; a 32-Tile Street at 50 km/h takes <b>0.87</b> Ticks,
/// not one. So the crossing rate is a quantity S2 can measure rather than assume, and R2 reports it
/// beside every cost that scales on it.
/// </para>
/// <para>
/// <b>The fleet is held at constant size.</b> A Traveller that arrives is immediately replaced by a
/// new one on a fresh O-D pair, so <i>vehicles in flight</i> is the swept axis rather than an emergent
/// quantity. That is what makes R2a's surface readable: the two attribution schemes scale on
/// independent axes, and a fleet that drifted in size would move both at once.
/// </para>
/// </remarks>
internal sealed class Fleet
{
    /// <summary>
    /// Crossings one Traveller may make in one Tick before the advance loop gives up.
    /// </summary>
    /// <remarks>
    /// A zero-cost arc would otherwise spin forever. The count of Travellers that hit the bound is
    /// reported rather than swallowed — a guard that fires silently is how a graph defect becomes a
    /// published figure, which this spike has already caught itself doing twice.
    /// </remarks>
    private const int CrossingsPerTickBound = 64;

    private readonly RoadGraph _graph;
    private readonly Districts _districts;
    private readonly int[] _arcTicks;
    private readonly RouteStore? _routes;
    private readonly NextHopTable? _nextHop;
    private readonly PathSource _source;
    private readonly ulong _seed;

    // Travellers, structure of arrays. The same layout the core's tables use.
    private readonly int[] _route;
    private readonly int[] _step;
    private readonly int[] _node;
    private readonly int[] _target;
    private readonly int[] _pair;
    private readonly int[] _arc;
    private readonly int[] _residual;

    private int _tick;

    public Fleet(
        RoadGraph graph,
        Districts districts,
        int[] arcTicks,
        PathSource source,
        RouteStore? routes,
        NextHopTable? nextHop,
        int size,
        ulong seed)
    {
        _graph = graph;
        _districts = districts;
        _arcTicks = arcTicks;
        _source = source;
        _routes = routes;
        _nextHop = nextHop;
        _seed = seed;

        _route = new int[size];
        _step = new int[size];
        _node = new int[size];
        _target = new int[size];
        _pair = new int[size];
        _arc = new int[size];
        _residual = new int[size];

        Volume = new int[graph.Volume.Length];
        InFlight = new int[districts.Count * districts.Count];

        for (int traveller = 0; traveller < size; traveller++)
        {
            _arc[traveller] = -1;
            _pair[traveller] = -1;
            Spawn(traveller);
        }
    }

    /// <summary>Travellers in flight. Constant by construction — see the class remarks.</summary>
    public int Size => _route.Length;

    /// <summary>
    /// Direct attribution's volume column, Q16.16 Travellers present, indexed as the graph's own is.
    /// </summary>
    public int[] Volume { get; }

    /// <summary>
    /// The ordered District-pair counter the aggregate scheme reads. Maintained here because
    /// maintaining it <i>is</i> part of that scheme's cost and pricing it elsewhere would flatter it.
    /// </summary>
    public int[] InFlight { get; }

    /// <summary>Segment boundaries crossed by the whole fleet during the last <see cref="Advance"/>.</summary>
    public long Crossings { get; private set; }

    /// <summary>Travellers that reached <see cref="CrossingsPerTickBound"/> in one Tick. Expected zero.</summary>
    public long Bounded { get; private set; }

    /// <summary>Travellers that arrived and were replaced during the last <see cref="Advance"/>.</summary>
    public long Arrivals { get; private set; }

    /// <summary>
    /// Optional capture of <c>(arc left, arc entered)</c> pairs during <see cref="Advance"/>, so
    /// R2a can time direct attribution's inner loop over a <b>real</b> crossing distribution.
    /// </summary>
    /// <remarks>
    /// <b>The distribution is the measurement.</b> <c>adr/0041</c> prices the scheme as <i>"order
    /// 80,000 increment/decrement pairs per Tick into a ~30,000-entry array of about 120 KB that sits
    /// in L2"</i>, and whether it does sit in L2 depends entirely on how scattered the indices are.
    /// Timing the loop over synthetic indices would answer a question about a random-number generator.
    /// </remarks>
    public int[]? CrossingLog { get; set; }

    /// <summary>Entries written to <see cref="CrossingLog"/> by the last <see cref="Advance"/>.</summary>
    public int CrossingLogCount { get; private set; }

    /// <summary>
    /// One Tick. Every Traveller consumes one Tick of travel time, crossing whatever boundaries that
    /// buys, incrementing on entry and decrementing on exit.
    /// </summary>
    public void Advance()
    {
        _tick++;
        Crossings = 0;
        Arrivals = 0;
        Bounded = 0;
        CrossingLogCount = 0;

        for (int traveller = 0; traveller < _route.Length; traveller++)
        {
            int budget = Fixed.One;
            int crossings = 0;

            while (budget > 0)
            {
                if (_residual[traveller] > budget)
                {
                    _residual[traveller] -= budget;
                    break;
                }

                budget -= _residual[traveller];

                if (++crossings > CrossingsPerTickBound)
                {
                    Bounded++;
                    _residual[traveller] = Fixed.One;
                    break;
                }

                int left = _arc[traveller];
                Leave(traveller);

                if (!Step(traveller))
                {
                    Arrivals++;
                    Spawn(traveller);
                }

                if (CrossingLog is not null && CrossingLogCount + 2 <= CrossingLog.Length)
                {
                    CrossingLog[CrossingLogCount++] = left;
                    CrossingLog[CrossingLogCount++] = _arc[traveller];
                }

                Crossings++;
            }
        }
    }

    /// <summary>
    /// Replaces <paramref name="count"/> Travellers with ones bound for <paramref name="destination"/>.
    /// R2b's jam: a surge into one District, which is the monocentric morning peak R1 modelled.
    /// </summary>
    /// <remarks>
    /// <b>Replaces rather than adds</b>, so the surge is a change in <i>where</i> the fleet is going
    /// and not in how large it is. A surge that grew the fleet would move direct attribution's cost
    /// and the jam's severity together, and R2b could not then say which of the two a lag figure was
    /// responding to.
    /// </remarks>
    public void Surge(int count, int destination)
    {
        int bound = count < _route.Length ? count : _route.Length;

        for (int traveller = 0; traveller < bound; traveller++)
        {
            Leave(traveller);
            Retire(traveller);
            Spawn(traveller, destination);
        }
    }

    /// <summary>Total volume, which must equal the fleet size in Q16.16 — <c>adr/0041</c>'s invariant.</summary>
    public long TotalVolume()
    {
        long total = 0;
        for (int i = 0; i < Volume.Length; i++)
        {
            total += Volume[i];
        }

        return total;
    }

    /// <summary>Travellers currently on no arc at all. The invariant's slack, and it should be zero.</summary>
    public int Unplaced()
    {
        int count = 0;
        for (int traveller = 0; traveller < _arc.Length; traveller++)
        {
            if (_arc[traveller] < 0)
            {
                count++;
            }
        }

        return count;
    }

    private void Leave(int traveller)
    {
        if (_arc[traveller] >= 0)
        {
            Volume[_graph.VolumeIndex(_arc[traveller])] -= Fixed.One;
            _arc[traveller] = -1;
        }
    }

    private void Enter(int traveller, int arc)
    {
        _arc[traveller] = arc;
        _residual[traveller] = _arcTicks[arc] == RoadGraph.Impassable ? Fixed.One : _arcTicks[arc];
        Volume[_graph.VolumeIndex(arc)] += Fixed.One;
    }

    private void Retire(int traveller)
    {
        if (_pair[traveller] >= 0)
        {
            InFlight[_pair[traveller]]--;
            _pair[traveller] = -1;
        }
    }

    /// <summary>Moves onto the next arc. False when the Traveller has arrived.</summary>
    private bool Step(int traveller)
    {
        if (_source == PathSource.NextHop)
        {
            // A District with no car-reachable node has no representative, so a Traveller drawn into
            // one has nowhere to start. Reported as an arrival rather than indexed with -1: Districts
            // already counts the empty ones and R1 prints that count.
            if (_node[traveller] < 0)
            {
                return false;
            }

            // Arrival is tested BEFORE entering, and the order is not cosmetic. Entering the last
            // arc and then reporting arrival in the same call leaves that arc incremented and never
            // decremented — the Traveller is respawned, and its volume stays on the road forever.
            // That is precisely the defect `adr/0041` names as its own invariant's reason: *"a
            // Traveller that vanishes without decrementing destroys the reading permanently, which is
            // an adr/0006-class defect that presents as a road that looks busy forever."* It was
            // written this way first, and the reading it produced was a v/c of 883 — see the
            // conservation check the report now prints, which is what caught it.
            if (_node[traveller] == _districts.Representative[_target[traveller]])
            {
                return false;
            }

            int arc = _nextHop!.Of(_node[traveller], _target[traveller]);
            if (arc < 0)
            {
                return false;
            }

            _node[traveller] = _graph.ArcTarget[arc];
            Enter(traveller, arc);
            return true;
        }

        int route = _route[traveller];
        if (route < 0 || _step[traveller] >= _routes!.Length(route))
        {
            return false;
        }

        Enter(traveller, _routes.ArcAt(route, _step[traveller]++));
        return true;
    }

    private void Spawn(int traveller) => Spawn(traveller, destination: -1);

    private void Spawn(int traveller, int destination)
    {
        Retire(traveller);
        _step[traveller] = 0;
        _residual[traveller] = 0;

        int count = _districts.Count;

        if (_source == PathSource.Searched)
        {
            // The pool is drawn from, not searched into. RouteStore.ForSearchedPool says why, and
            // the reason is the rung's own headline: 400 searches per simulated Tick at R0's
            // denominator is 170 ms of harness time per 15.6 ms of Tick budget.
            int route = Draw(traveller, CounterHash.Purpose.TravellerRespawn, _routes!.Count);
            for (int attempt = 0; attempt < 8 && _routes.Length(route) == 0; attempt++)
            {
                route = Draw(traveller, CounterHash.Purpose.TravellerOrigin, _routes.Count, attempt);
            }

            _route[traveller] = route;
            _pair[traveller] = PairOf(route);
        }
        else
        {
            int from = Draw(traveller, CounterHash.Purpose.TravellerOrigin, count);
            int to = destination >= 0
                ? destination
                : Draw(traveller, CounterHash.Purpose.TravellerDestination, count);

            for (int attempt = 0; attempt < 8 && _districts.Representative[from] < 0; attempt++)
            {
                from = Draw(traveller, CounterHash.Purpose.SurgeOrigin, count, attempt);
            }

            _pair[traveller] = (from * count) + to;

            if (_source == PathSource.NextHop)
            {
                // Spawned at a real node, NEVER at the origin District's representative, and the
                // distinction is the entire point of this rung. A next-hop table is followed from
                // wherever the Traveller actually is; starting it at the representative would make it
                // walk the shared route and the two rungs would measure one thing. They did, in the
                // capture before this line existed — Shared and NextHop reported byte-identical peaks,
                // which is the tell that two independent measurements are not independent.
                _target[traveller] = to;
                _route[traveller] = -1;
                _node[traveller] = -1;

                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int segment = Draw(
                        traveller, CounterHash.Purpose.SurgeOrigin, _graph.Segments, attempt);
                    int candidate = _graph.SegmentNodeA[segment];

                    if (_nextHop!.Of(candidate, to) >= 0)
                    {
                        _node[traveller] = candidate;
                        break;
                    }
                }
            }
            else
            {
                _route[traveller] = _pair[traveller];
            }
        }

        if (_pair[traveller] >= 0)
        {
            InFlight[_pair[traveller]]++;
        }

        // Placed onto its first arc immediately, so the volume invariant holds at every Tick
        // boundary rather than only after the first advance.
        if (!Step(traveller))
        {
            _arc[traveller] = -1;
            _residual[traveller] = Fixed.One;
        }
    }

    private int PairOf(int route)
    {
        // A pooled searched route is not keyed by District pair, so the aggregate counter it feeds
        // is derived from where the route actually starts and ends. That derivation is free here and
        // it is what keeps the two schemes reading the same load rather than two different ones.
        if (_routes!.Length(route) == 0)
        {
            return -1;
        }

        int first = _routes.ArcAt(route, 0);
        int last = _routes.ArcAt(route, _routes.Length(route) - 1);

        int fromSegment = _graph.ArcSegment[first];
        int from = _districts.OfNode[_graph.SegmentNodeA[fromSegment]];
        int to = _districts.OfNode[_graph.ArcTarget[last]];

        return (from * _districts.Count) + to;
    }

    private int Draw(int traveller, CounterHash.Purpose purpose, int bound, int salt = 0) =>
        CounterHash.Below(
            // Both operands cast through an unsigned type before widening. Sign extension cannot
            // change a value here — the Tick and the salt are non-negative by construction — so this
            // silences CS0675 without moving a single drawn number.
            CounterHash.Of(_seed, (ulong)traveller, ((ulong)(uint)_tick << 8) | (uint)salt, purpose),
            bound);
}
