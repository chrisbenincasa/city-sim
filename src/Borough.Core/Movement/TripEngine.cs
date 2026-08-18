using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Instruments;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Tick phase 4: Travellers advance along their Legs, Trips end, and every ended Trip is counted
/// before its rows go back.
/// </summary>
/// <remarks>
/// <para>
/// <b>Phase 4 has been an empty method since the Tick was written</b>, and <c>adr/0080</c> is why it
/// is filled now rather than when a generator arrives: nothing here depends on <em>which</em> pairs
/// travel. The cursor, the Fate, the counting and the release are the same machinery for a commanded
/// Trip and for a generated one, and building them against the commanded kind means the generator
/// arrives to a phase that already works rather than to a phase and a second code path.
/// </para>
/// <para>
/// <b>One sweep counts and frees, and Phase 0's own failures flow through it.</b> A Trip can end
/// before it ever moves — <c>adr/0079</c>'s Building with no front door, or an origin and destination
/// with no walkable route between them — and those are resolved where they are discovered, in
/// <c>Simulation.ApplyTrip</c>. They are <em>not</em> counted or freed there. Everything that ends
/// goes back through the sweep below, so there is exactly one place that reads a Fate and exactly one
/// place that releases a row, which is what makes <c>02</c>'s <i>no Trip without a Fate</i> checkable
/// and <c>adr/0006</c> satisfiable in one reading.
/// </para>
/// <para>
/// <b>The Fate reaches the Census before the row does back</b>, which is <c>plans/0021</c> task 2's
/// standing warning: <i>"a completed Trip's Fate must reach the Census before the row is freed, or
/// the only durable record of a failure is gone."</i> The counters are <b>flows</b> rather than
/// levels, on slice 7 task 9's precedent — read as a sum and a peak over the interval, and the
/// reading drains them.
/// </para>
/// <para>
/// <b>What this does not do is attribute volume.</b> <c>adr/0041</c> increments a Segment on entry
/// and decrements on exit, and only for <b>vehicular</b> Legs — <i>"walk Legs still contribute
/// nothing"</i>, because <c>CONTEXT.md</c> → Fidelity keeps pedestrians out of Stress entirely.
/// Milestone 5b resolves walk Legs and no others, so the attribution has nothing to attribute; it is
/// a separate change rather than a line here that would never execute.
/// </para>
/// </remarks>
public sealed class TripEngine
{
    private readonly World _world;

    private RuleFlow _completedFlow;
    private RuleFlow _walkLegFlow;
    private RuleFlow _driveLegFlow;
    private int _tickWalkLegs;
    private int _tickDriveLegs;
    private RuleFlow _noRouteFlow;
    private RuleFlow _overBudgetFlow;
    private RuleFlow _strandedFlow;

    /// <summary>
    /// The cost histogram's flows, one per <see cref="TripCostBucket"/>, and this Tick's counts.
    /// </summary>
    /// <remarks>
    /// <b>Two arrays because the fold is per Tick and the bucketing is per Trip.</b> A
    /// <see cref="RuleFlow"/>'s peak is <em>the largest single Tick</em>, so a bucket incremented
    /// directly would record a peak of one for ever. The per-Tick counts are folded in
    /// <see cref="Advance"/>, which runs after generation in the same phase and after Phase 0's
    /// commands — so a commanded Trip and a generated one land in the same interval, as they should.
    /// </remarks>
    private readonly RuleFlow[] _costFlows = new RuleFlow[Buckets];

    private readonly int[] _tickCosts = new int[Buckets];

    /// <summary>How many bands the cost histogram has.</summary>
    private const int Buckets = (int)TripCostBucket.ThirtyTwoMinutesOrMore + 1;

    /// <summary>
    /// The Dijkstra's scratch, reused across every Trip this engine starts.
    /// </summary>
    /// <remarks>
    /// <b>One per engine rather than one per call</b>, which is what keeps <see cref="Start"/>
    /// allocation-free on the hot path — the commute generator calls it once per departing Citizen
    /// and there are as many of those a Day as there are workers.
    /// </remarks>
    private readonly WalkScratch _walk = new();

    /// <param name="world">The world whose Travellers this advances.</param>
    public TripEngine(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
    }

    /// <summary>
    /// Reads the interval's Trip Fates and resets them.
    /// </summary>
    /// <remarks>
    /// <b>The reading drains, and that is what makes these flows rather than levels.</b> A table
    /// counter is a <em>level</em> — how many rows there are — and asking twice gives the same answer.
    /// A Fate is an <em>event</em>: asking twice must not count it twice, so the interval belongs to
    /// whoever reads and the counter belongs to nobody in between.
    /// </remarks>
    public TripActivity Drain()
    {
        var activity = new TripActivity(
            _completedFlow,
            _noRouteFlow,
            _overBudgetFlow,
            _strandedFlow,
            new TripCostProfile(
                _costFlows[0], _costFlows[1], _costFlows[2], _costFlows[3],
                _costFlows[4], _costFlows[5], _costFlows[6]),
            _walkLegFlow,
            _driveLegFlow);

        _completedFlow = default;
        _noRouteFlow = default;
        _overBudgetFlow = default;
        _strandedFlow = default;
        _walkLegFlow = default;
        _driveLegFlow = default;

        Array.Clear(_costFlows);

        return activity;
    }

    /// <summary>
    /// Puts one Citizen on the road between two Addresses: the Trip, its Leg, its Fate if it has one
    /// already, and its Traveller if it has not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One door, and it is here because there are two callers now rather than because there might
    /// be.</b> <c>CommandKind.Trip</c> was the only way a Trip could exist for the whole of milestone
    /// 5b (<c>adr/0080</c>), and 5b-bis's commute generator is the second — so the body that decides
    /// what a Trip <em>is</em> moved out of <c>Simulation.ApplyTrip</c> on the day the second caller
    /// appeared. <c>TripPurpose.Commanded</c>'s own rule says why the alternative is worse: nothing
    /// downstream may branch on the purpose, and two copies of the creation path are exactly the
    /// branch, written in the one place a reader would not look for one.
    /// </para>
    /// <para>
    /// ⚠ <b>The mode <em>is</em> read, and it is the one parameter here that is.</b> It prices the
    /// journey, selects the subgraph the search runs over, and is stamped on the Leg — so a drive and
    /// a walk between the same two Addresses are different costs, may have different reachability, and
    /// are distinguishable afterwards. That last one is what <c>adr/0041</c>'s volume attribution
    /// needs: only a vehicular Leg increments a Segment's count.
    /// </para>
    /// <para>
    /// <b>The purpose is a parameter and nothing in here reads it.</b> It is written to the row and
    /// counted; the Fate, the Budget test and the cursor are identical for every purpose, which is
    /// the invariant this method exists to make structural rather than remembered.
    /// </para>
    /// <para>
    /// <b>Both endpoints may be holes and that is a Fate rather than a refusal</b> (<c>adr/0079</c>):
    /// a Building outlives its frontage, so an Address with no Segment is a
    /// <see cref="TripFate.NoRouteFound"/> the model reports. The caller has already decided the two
    /// Buildings are the ones it meant.
    /// </para>
    /// </remarks>
    /// <param name="citizen">The slot of the Citizen travelling.</param>
    /// <param name="from">Where the journey starts.</param>
    /// <param name="to">Where it is meant to end.</param>
    /// <param name="mode">How the journey is made. Stamped on the Leg and used to price it.</param>
    /// <param name="purpose">Why it is being made, recorded and never branched on.</param>
    /// <param name="tick">The Tick the journey starts on.</param>
    /// <returns>
    /// The Fate the Trip already has, or <see cref="TripFate.InFlight"/> if it set off.
    /// </returns>
    public TripFate Start(
        int citizen, int fromBuilding, int toBuilding, TravelMode mode, TripPurpose purpose,
        Ticks tick)
    {
        TripRuleset rules = _world.Rules.Trips;

        Address from = _world.PedestrianAccessPoint(fromBuilding);
        Address to = _world.PedestrianAccessPoint(toBuilding);

        Handle<Trip> trip = _world.Trips.Create(_world.Roads.Segments, purpose, from, to);
        int tripSlot = _world.Trips.Rows.Resolve(trip);

        if (!from.Exists || !to.Exists)
        {
            // Impassable rather than uncounted, so that the cost histogram's buckets sum to the Trips
            // that were made. adr/0079's hole is a journey nobody can take, and there is no honest
            // duration for it -- which is what the sentinel means.
            _tickCosts[(int)BucketOf(TravelTime.Impassable)]++;
            _world.ResolveTrip(citizen, tripSlot, TripFate.NoRouteFound);

            return TripFate.NoRouteFound;
        }

        // adr/0008: a car commute is never one Leg, it is at minimum walk -> drive -> walk. The two
        // flanking walks run from the pedestrian Access Point to the vehicle one, which are equal by
        // construction today (World.VehicleAccessPoint) and therefore cost zero -- session F's named
        // placeholder, and the retrofit at milestone 8 is one endpoint swap because a parking Bin has
        // an Address and so does a Building. The trap that ADR warns about is a *fallback* from an
        // exhausted Parking Shed, which no Shed exists to produce.
        Span<Address> waypoints = stackalloc Address[4];
        Span<TravelMode> modes = stackalloc TravelMode[3];
        int legCount = Itinerary(fromBuilding, toBuilding, from, to, mode, waypoints, modes);

        TravelTime cost = TravelTime.Zero;
        int head = Rows.NoSlot;
        int tail = Rows.NoSlot;

        for (int i = 0; i < legCount; i++)
        {
            bool driving = modes[i] == TravelMode.Car;

            TravelTime step = WalkRouting.Cost(
                _world.Roads, modes[i], waypoints[i], waypoints[i + 1], rules.CrossingCost, _walk,
                recordPath: driving);

            // Eagerly, per adr/0075: every Leg of a Trip is created at Trip creation, which is what
            // makes mean-Legs-per-Trip countable at all. The impassable one is created too, because
            // the Leg that failed is the diagnosis.
            int legSlot = _world.Legs.Rows.Resolve(
                _world.Legs.Create(
                    _world.Roads.Segments, modes[i], waypoints[i], waypoints[i + 1], step));

            _world.Trips.Append(_world.Legs, tripSlot, legSlot);

            if (driving)
            {
                _tickDriveLegs++;

                // Read immediately, because _walk is one reusable scratch and the next iteration
                // overwrites it. adr/0041 needs this route every Tick the Traveller is moving, which
                // is why it goes into a saved table rather than being looked up again later.
                if (!step.IsImpassable)
                {
                    RecordRoute(legSlot, waypoints[i], waypoints[i + 1]);
                }
            }
            else
            {
                _tickWalkLegs++;
            }

            if (head == Rows.NoSlot)
            {
                head = legSlot;
            }

            tail = legSlot;
            cost = step.IsImpassable || cost.IsImpassable
                ? TravelTime.Impassable
                : cost + step;
        }

        // Counted here rather than at completion, so a Trip refused for its length is in the
        // distribution with the cost that refused it. A histogram of completed Trips would be censored
        // twice -- once by the assignment pass's Budget upstream, and again by the Fate.
        _tickCosts[(int)BucketOf(cost)]++;

        if (cost.IsImpassable)
        {
            _world.ResolveTrip(citizen, tripSlot, TripFate.NoRouteFound, tail);
            return TripFate.NoRouteFound;
        }

        // The Budget is judged on the whole Trip before anybody sets off, which is what a budget is:
        // a person who can see the journey is too long does not make two thirds of it and stop. The
        // Legs exist either way, because adr/0075 creates every Leg at Trip creation and the cost of
        // the journey not taken is the diagnosis -- "32 can't reach a job inside their Commute
        // Budget" needs the number that failed, not just the count.
        if (!rules.WithinBudget(cost))
        {
            _world.ResolveTrip(citizen, tripSlot, TripFate.ExceededCommuteBudget, tail);
            return TripFate.ExceededCommuteBudget;
        }

        // Floor, and a sub-Tick walk therefore arrives on the Tick it departed. That is adr/0071 being
        // taken literally rather than a rounding convenience: travel time is sub-Tick and Q16.16 is a
        // scale, so a walk across the street genuinely costs less than one integration step. Phase 4
        // runs after Phase 0 in the same Tick, so such a Trip is created and completed without ever
        // being observed in flight -- which is correct, and is why the in-flight count is not a proxy
        // for the number of Trips a run made.
        //
        // The *first* Leg's cost, not the Trip's: the Traveller is a cursor and AdvanceTravellers
        // prices each hop as it reaches it (adr/0075).
        int travellerSlot = _world.Travellers.Rows.Resolve(
            _world.Travellers.Create(_world.Citizens.Rows.At(citizen), trip, head, tick));

        _world.Travellers.ArrivesAt[travellerSlot] = BeginLeg(travellerSlot, head, tick);

        return TripFate.InFlight;
    }

    /// <summary>
    /// Writes the Segments a drive Leg crosses into <see cref="World.RouteHops"/>, in order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The origin and destination Segments are hops too, and leaving them out would break the
    /// conservation invariant rather than merely lose detail.</b> <see cref="WalkScratch.PathTo"/>
    /// hands back the Segments of the <em>arcs between the two endpoint nodes</em>; the vehicle also
    /// spends time on the part of its own Segment before the first node and the part of the
    /// destination's after the last. <c>Invariant.SegmentVolumeIsConserved</c> asserts that total
    /// volume equals the number of in-flight vehicular Travellers, so a Traveller that is briefly on
    /// <em>no</em> Segment fails it — the vehicle has to be somewhere every Tick.
    /// </para>
    /// <para>
    /// ⚠ <b>An empty route with two distinct end Segments is normal, not a failure.</b> Two Segments
    /// that share a node need no arc between them, and the same-Segment case never reaches the search
    /// at all. ***Absent is not zero and it is not impassable.***
    /// </para>
    /// </remarks>
    private void RecordRoute(int legSlot, Address from, Address to)
    {
        RoadArcs arcs = _world.Roads.Arcs;
        RoadSegmentTable segments = _world.Roads.Segments;
        RouteHopTable hops = _world.RouteHops;

        int length = _walk.Arrived == WalkScratch.NoNode
            ? WalkScratch.NoPath
            : _walk.ArcsTo(_walk.Arrived, []);

        if (length <= 0)
        {
            // No Arc between the two ends -- the same Segment, or two that meet at a node. Nothing
            // determines a direction, so both endpoint hops take the convention. RouteHopTable.Forward
            // carries the argument for why that is stated rather than hidden.
            AppendHop(legSlot, from.Segment, forward: true);
            AppendHop(legSlot, to.Segment, forward: true);

            return;
        }

        Span<int> route = length <= 64 ? stackalloc int[length] : new int[length];

        _walk.ArcsTo(_walk.Arrived, route);

        // The vehicle leaves its own Segment by whichever node the first Arc departs from, and that
        // node is the end of the first Arc's Segment that the Arc does not point at. Reading it back
        // out this way costs one comparison and saves carrying a second node list out of the search.
        int firstSegment = arcs.Segment[route[0]];
        int exit = OtherEnd(firstSegment, arcs.Target[route[0]]);

        AppendHop(legSlot, from.Segment, IsNodeB(from.Segment, exit));

        foreach (int arc in route)
        {
            AppendHop(legSlot, arcs.Segment[arc], IsNodeB(arcs.Segment[arc], arcs.Target[arc]));
        }

        // Arriving: the vehicle enters the destination Segment at the last Arc's target and drives
        // away from it, so this one is the mirror of the departure and not a copy of it.
        AppendHop(legSlot, to.Segment, !IsNodeB(to.Segment, arcs.Target[route[^1]]));

        int OtherEnd(int segment, int node) =>
            IsNodeB(segment, node)
                ? (_world.Roads.Nodes.Rows.TryResolve(segments.NodeA[segment], out int a) ? a : node)
                : (_world.Roads.Nodes.Rows.TryResolve(segments.NodeB[segment], out int b) ? b : node);

        bool IsNodeB(int segment, int node) =>
            segment >= 0 && segments.Rows.IsLive(segment)
            && _world.Roads.Nodes.Rows.TryResolve(segments.NodeB[segment], out int b) && b == node;

        void AppendHop(int leg, int segment, bool forward)
        {
            if (segment < 0 || !segments.Rows.IsLive(segment))
            {
                return;
            }

            // A route that doubles back on the Segment it is already on is one hop, not two. The
            // search can return the origin's Segment as its first arc when the vehicle leaves by one
            // endpoint and the route runs back past the other, and a Traveller cannot occupy the same
            // Segment twice in a row without leaving it -- which is what the volume pair would claim.
            int tail = _world.Legs.RouteTail[leg];

            if (tail != 0 && segments.Rows.TryResolve(hops.Segment[tail - 1], out int last)
                && last == segment)
            {
                return;
            }

            _world.Legs.AppendHop(
                hops, leg, hops.Rows.Resolve(hops.Create(segments, segment, forward)));
        }
    }

    /// <summary>
    /// The waypoints and per-Leg modes of one journey — <b>one Leg on foot, three by car</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0008</c>'s <c>walk → drive → walk</c>, built rather than deferred, and the two
    /// flanking Legs are zero-length today.</b> They run from the pedestrian Access Point to the
    /// vehicle one, which <see cref="World.VehicleAccessPoint"/> makes the same Address until
    /// milestone 8 gives a car somewhere else to be. **Building them now is what keeps that retrofit
    /// to one endpoint swap** — the Trip table is sized on the real Leg count, `mean Legs per Trip` is
    /// a real number, and `AdvanceTravellers` walks a cursor that has somewhere to go.
    /// </para>
    /// <para>
    /// ⚠ <b>The trap <c>adr/0008</c> names is a <em>fallback</em>, and it is not this.</b> A zero-length
    /// parking walk reached because a Parking Shed query <em>failed</em> pays the player for
    /// under-building parking: a full car park costs less than an empty one. That cannot occur here,
    /// because there is no Shed and nothing queries one — the walk is to the car's <em>only</em> place,
    /// not to a fallback from a better one. <b>What survives of the warning is that zero is inside the
    /// range of legitimate answers</b>, so the placeholder cannot announce itself, and the only defence
    /// is that this method is where a reader is sent.
    /// </para>
    /// </remarks>
    private int Itinerary(
        int fromBuilding, int toBuilding, Address from, Address to, TravelMode mode,
        Span<Address> waypoints, Span<TravelMode> modes)
    {
        if (mode != TravelMode.Car)
        {
            waypoints[0] = from;
            waypoints[1] = to;
            modes[0] = mode;

            return 1;
        }

        waypoints[0] = from;
        waypoints[1] = _world.VehicleAccessPoint(fromBuilding);
        waypoints[2] = _world.VehicleAccessPoint(toBuilding);
        waypoints[3] = to;

        modes[0] = TravelMode.Foot;
        modes[1] = TravelMode.Car;
        modes[2] = TravelMode.Foot;

        return 3;
    }

    /// <summary>
    /// Advances every Traveller whose current Leg has completed, then counts and releases every
    /// Trip that has ended.
    /// </summary>
    public void Advance(Ticks tick)
    {
        AdvanceTravellers(tick);
        ReleaseEnded();
        CloseTick();
    }

    /// <summary>Rolls this Tick's cost buckets into their flows and resets them.</summary>
    /// <remarks>
    /// <b>Called from <see cref="Advance"/> rather than from <see cref="Start"/>, because a Tick is
    /// what a flow's peak is denominated in</b> and <see cref="Start"/> has no idea whether it is the
    /// last call of the Tick. Phase 4 is the last phase that creates a Trip, so this is where the Tick
    /// closes.
    /// </remarks>
    private void CloseTick()
    {
        _walkLegFlow = _walkLegFlow.Fold(_tickWalkLegs);
        _driveLegFlow = _driveLegFlow.Fold(_tickDriveLegs);
        _tickWalkLegs = 0;
        _tickDriveLegs = 0;

        for (int bucket = 0; bucket < Buckets; bucket++)
        {
            _costFlows[bucket] = _costFlows[bucket].Fold(_tickCosts[bucket]);
            _tickCosts[bucket] = 0;
        }
    }

    /// <summary>
    /// Which band of the cost histogram a journey of <paramref name="cost"/> falls in.
    /// </summary>
    /// <remarks>
    /// <b>A geometric ladder in clock minutes, and an impassable cost lands in the last band.</b>
    /// <see cref="TripCostBucket"/> carries the argument for both: the ruler must not be denominated
    /// in the Commute Budget, and <em>a journey nobody would make</em> covers the impossible one,
    /// which <see cref="TripCounter.NoRouteFound"/> already counts exactly.
    /// </remarks>
    internal static TripCostBucket BucketOf(TravelTime cost)
    {
        if (cost < TravelTime.FromMinutes(1)) { return TripCostBucket.UnderOneMinute; }
        if (cost < TravelTime.FromMinutes(2)) { return TripCostBucket.UnderTwoMinutes; }
        if (cost < TravelTime.FromMinutes(4)) { return TripCostBucket.UnderFourMinutes; }
        if (cost < TravelTime.FromMinutes(8)) { return TripCostBucket.UnderEightMinutes; }
        if (cost < TravelTime.FromMinutes(16)) { return TripCostBucket.UnderSixteenMinutes; }
        if (cost < TravelTime.FromMinutes(32)) { return TripCostBucket.UnderThirtyTwoMinutes; }

        return TripCostBucket.ThirtyTwoMinutesOrMore;
    }

    /// <summary>
    /// Puts a Traveller at the start of a Leg and returns the Tick it next needs attention.
    /// </summary>
    /// <remarks>
    /// <b>A walk Leg is priced once and a drive Leg is priced per Segment</b>, and the asymmetry is
    /// <c>adr/0041</c>'s rather than a shortcut: volume is attributed for vehicular Legs only, because
    /// <c>CONTEXT.md</c> → Fidelity keeps pedestrians out of Stress entirely, and <c>03 §3.7</c> makes
    /// that permanent by saying a pedestrian network does not saturate. A walk therefore has nothing to
    /// price against and nothing to attribute, so it costs what it was planned to cost.
    /// </remarks>
    private Ticks BeginLeg(int travellerSlot, int legSlot, Ticks tick)
    {
        TravellerTable travellers = _world.Travellers;
        LegTable legs = _world.Legs;

        int head = (TravelMode)legs.Mode[legSlot] == TravelMode.Foot
            ? -1
            : legs.RouteHead[legSlot] - 1;

        if (head < 0)
        {
            travellers.CurrentHop[travellerSlot] = Rows.NoSlot;

            return Arrive(travellerSlot, legs.Time[legSlot], tick);
        }

        travellers.CurrentHop[travellerSlot] = head;

        return Arrive(travellerSlot, Enter(head), tick);
    }

    /// <summary>
    /// Adds a cost to a Traveller's clock, keeping the sub-Tick remainder.
    /// </summary>
    /// <remarks>
    /// <b>The remainder is the point</b> — see <see cref="TravellerTable.Carry"/>. Flooring each hop
    /// independently would make a Street free, because a 32-Tile Street at 50 km/h costs 0.22 Ticks.
    /// </remarks>
    private Ticks Arrive(int travellerSlot, TravelTime cost, Ticks tick)
    {
        TravellerTable travellers = _world.Travellers;

        TravelTime total = cost.IsImpassable ? cost : travellers.Carry[travellerSlot] + cost;
        Ticks whole = total.ToTicksFloor();

        travellers.Carry[travellerSlot] = total.IsImpassable
            ? TravelTime.Zero
            : total - TravelTime.FromTicks((int)whole.Raw);

        return tick + whole;
    }

    /// <summary>
    /// Puts a vehicle onto a Segment and returns what that Segment costs it, <b>loaded</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The increment happens before the pricing, so a vehicle is part of the congestion it meets.</b>
    /// The alternative — price, then join — makes the first vehicle onto an empty Segment free and every
    /// later one pay for it, which is an ordering artefact rather than a fact about the road.
    /// </para>
    /// <para>
    /// ⚠ <b>Free-flow is the Segment's, not the Arc's.</b> <see cref="RoadArcs"/> holds a per-Arc time
    /// that already takes the minimum against walking pace for the foot mode; a car reads the Segment's
    /// own <see cref="RoadSegmentTable.FreeFlow"/> directly, which is the quantity capacity and the
    /// volume-delay function are both defined over.
    /// </para>
    /// </remarks>
    private TravelTime Enter(int hopSlot)
    {
        RouteHopTable hops = _world.RouteHops;
        RoadSegmentTable segments = _world.Roads.Segments;

        if (!segments.Rows.TryResolve(hops.Segment[hopSlot], out int segment))
        {
            // The Segment was bulldozed under a planned route. Nothing to attribute and nothing to
            // charge; the Trip's Fate is a question for whoever builds TripFate.Stranded, and today
            // nothing produces it. Charging a cost here would invent a delay for a road that is gone.
            return TravelTime.Zero;
        }

        bool forward = hops.Forward[hopSlot] != 0;

        if (forward)
        {
            segments.VolumeForward[segment]++;
        }
        else
        {
            segments.VolumeBackward[segment]++;
        }

        TravelTime freeFlow = segments.FreeFlowOver(segment);

        return _world.Rules.Traffic.Apply(freeFlow, segments.LoadAt(segment, forward, freeFlow));
    }

    /// <summary>Takes a vehicle off a Segment. The exact mirror of <see cref="Enter"/>'s increment.</summary>
    /// <remarks>
    /// <b>Nothing is priced here</b>, which is what keeps <c>Invariant.SegmentVolumeIsConserved</c>
    /// checkable: the two writes are a matched pair keyed on the same stored direction bit, so a
    /// Traveller can never be decremented off a Segment it was incremented onto in the other direction.
    /// </remarks>
    private void Leave(int hopSlot)
    {
        RouteHopTable hops = _world.RouteHops;
        RoadSegmentTable segments = _world.Roads.Segments;

        if (!segments.Rows.TryResolve(hops.Segment[hopSlot], out int segment))
        {
            return;
        }

        if (hops.Forward[hopSlot] != 0)
        {
            segments.VolumeForward[segment]--;
        }
        else
        {
            segments.VolumeBackward[segment]--;
        }
    }


    /// <summary>
    /// Moves each arrived Traveller onto its next Leg, or ends its Trip when there is none.
    /// </summary>
    /// <remarks>
    /// <b>The next Leg's arrival is computed here rather than inside <see cref="TravellerTable.Advance"/>,
    /// and the asymmetry is deliberate.</b> The table's job is the cursor — follow the Leg's own
    /// <c>next</c> and move — and a cost is a question about the Ruleset in force and the Tick, which
    /// a table has no business holding. So the caller peeks the same link the cursor is about to
    /// follow. It is one extra array read and it keeps <c>adr/0075</c>'s split intact: the Leg is the
    /// plan, the Traveller is the cursor, and neither prices anything.
    /// </remarks>
    private void AdvanceTravellers(Ticks tick)
    {
        TravellerTable travellers = _world.Travellers;
        LegTable legs = _world.Legs;
        TripTable trips = _world.Trips;

        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            if (!travellers.Rows.IsLive(slot))
            {
                continue;
            }

            // A Traveller advances for as long as it has Tick left, rather than once per Tick, and
            // this loop is the whole of the sub-Tick model actually running.
            //
            // It used to be a single step: the body ended by moving to the next SLOT, so a Traveller
            // crossed at most one Segment a Tick however cheap the crossing was. That is adr/0041's
            // *a vehicle crosses about one Segment per Tick* -- a sentence that ADR states as
            // FOLLOWING FROM TicksPerDay = 8192, and which adr/0094 retired by moving the constant to
            // 2048. A Street is 0.22 Ticks now, so the true rate is ~4.6, and the floor of one made
            // every drive about 5.6x its own quoted cost: `hops + floor(total cost)` where the Leg
            // was priced at `total cost`. Task 6 found this premise expired and repaired the ONE site
            // it was looking at, the volume/capacity ratio. This is the other site. A premise that
            // expires retires every place resting on it, and finding one of them is not finding them.
            //
            // It terminates on structure rather than on time, which is what makes a zero-cost hop
            // safe: every iteration consumes a hop or a Leg, and both lists are finite, so a route
            // that costs nothing is walked to its end inside one Tick and the Traveller is freed.
            while (travellers.ArrivesAt[slot] <= tick)
            {
                int hop = travellers.CurrentHop[slot];

                if (hop != Rows.NoSlot)
                {
                    Leave(hop);

                    int nextHop = _world.RouteHops.Next[hop] - 1;

                    if (nextHop >= 0)
                    {
                        // Still on the same Leg. The cursor moves a Segment, not a Leg, which is the
                        // whole of what the Segment cursor buys: the Leg's cost was a plan and this is
                        // the journey actually happening, priced Segment by Segment as it is met.
                        travellers.CurrentHop[slot] = nextHop;
                        travellers.ArrivesAt[slot] = Arrive(slot, Enter(nextHop), tick);
                        continue;
                    }

                    travellers.CurrentHop[slot] = Rows.NoSlot;
                }

                int current = travellers.CurrentLeg[slot];
                int encoded = legs.Next[current];

                if (encoded != 0)
                {
                    travellers.CurrentLeg[slot] = encoded - 1;
                    travellers.ArrivesAt[slot] = BeginLeg(slot, encoded - 1, tick);
                    continue;
                }

                // No further Legs: the plan is exhausted, so the journey is over. Completed is the
                // Fate for *reaching the destination*, and reaching it is what running out of Legs
                // means -- adr/0076's rule that a Fate names the journey rather than what happened at
                // the far end.
                if (trips.Rows.TryResolve(travellers.Trip[slot], out int trip))
                {
                    trips.Resolve(trip, TripFate.Completed);
                }

                // Recorded whether or not the Trip row is still there, and read off the Traveller
                // rather than out of the branch above: the Citizen made the journey either way, and
                // a Trip freed out from under its Traveller is a defect this must not swallow by
                // silently forgetting whose journey it was. milestone 6 task 7.
                if (_world.Citizens.Rows.TryResolve(travellers.Citizen[slot], out int who))
                {
                    _world.RecordTripFate(who, TripFate.Completed);
                }

                travellers.Rows.Free(travellers.Rows.At(slot));
                break;
            }
        }
    }

    /// <summary>
    /// Counts every ended Trip and gives its rows back — the Legs first, then the Trip.
    /// </summary>
    /// <remarks>
    /// <b>Popped rather than walked, because the walk follows links in the rows being released.</b>
    /// <see cref="IndexList.PopFront"/> unlinks before the row is freed, so the list is never read
    /// through a slot that has gone back to the allocator.
    /// </remarks>
    private void ReleaseEnded()
    {
        TripTable trips = _world.Trips;
        LegTable legs = _world.Legs;
        RouteHopTable hops = _world.RouteHops;
        IndexList legList = trips.LegList(legs);

        int completed = 0;
        int noRoute = 0;
        int overBudget = 0;
        int stranded = 0;

        for (int slot = 0; slot < trips.Rows.SlotCount; slot++)
        {
            if (!trips.Rows.IsLive(slot))
            {
                continue;
            }

            var fate = (TripFate)trips.Fate[slot];

            if (fate == TripFate.InFlight)
            {
                continue;
            }

            switch (fate)
            {
                case TripFate.Completed: completed++; break;
                case TripFate.NoRouteFound: noRoute++; break;
                case TripFate.ExceededCommuteBudget: overBudget++; break;
                case TripFate.Stranded: stranded++; break;
                default: break;
            }

            for (int leg = legList.PopFront(slot); leg != Rows.NoSlot; leg = legList.PopFront(slot))
            {
                // The route before the Leg, on the same argument the Legs-before-the-Trip loop makes:
                // the walk follows links in the rows being released, so PopFront unlinks before the
                // row goes back to the allocator. This is adr/0006's sink for RouteHopTable and the
                // only one it has.
                IndexList route = legs.Route(hops);

                for (int hop = route.PopFront(leg); hop != Rows.NoSlot; hop = route.PopFront(leg))
                {
                    hops.Rows.Free(hops.Rows.At(hop));
                }

                legs.Rows.Free(legs.Rows.At(leg));
            }

            Release(slot);
        }

        _completedFlow = _completedFlow.Fold(completed);
        _noRouteFlow = _noRouteFlow.Fold(noRoute);
        _overBudgetFlow = _overBudgetFlow.Fold(overBudget);
        _strandedFlow = _strandedFlow.Fold(stranded);
    }

    /// <summary>
    /// Gives a Trip row back, having checked it carries a Fate.
    /// </summary>
    /// <remarks>
    /// <b>The only place a Trip row is freed, and the check is here rather than at the caller so that
    /// it stays that way.</b> <c>02 §10</c>'s <i>no Trip without a Fate</i> — see
    /// <see cref="Invariant.TripHasAFate"/>. The loop above cannot violate it, which is the point: the
    /// guard exists for the second release site, not for the first.
    /// </remarks>
    /// <remarks>
    /// <b>Internal rather than private so the violation can be written.</b> No path through the public
    /// API reaches this with an unresolved Trip — that is what the guard is for — so a test confined to
    /// <see cref="Advance"/> could only assert that the guard never fires, which is a different claim
    /// and is equally true of a guard that is not there.
    /// </remarks>
    internal void Release(int slot)
    {
        _world.Invariants.Require(
            (TripFate)_world.Trips.Fate[slot] != TripFate.InFlight,
            Invariant.TripHasAFate,
            slot);

        _world.Trips.Rows.Free(_world.Trips.Rows.At(slot));
    }
}

/// <summary>
/// What Tick phase 4 did over a Census interval: one flow per <see cref="TripFate"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>All four, including the two milestone 5b cannot produce, and the zeroes are informative.</b>
/// <c>adr/0076</c> closes the Fate set at four, so a counter each is the shape that needs no edit when
/// the missing conditions arrive. <see cref="TripFate.ExceededCommuteBudget"/> is structurally
/// unreachable until the Commute Budget exists as <c>[trips]</c> Ruleset data — milestone 5b-bis,
/// <c>adr/0081</c> — and <see cref="TripFate.Stranded"/> needs a Segment to vanish under a Trip in
/// flight, which <c>CommandKind.Connect</c>'s bulldoze can do today.
/// </para>
/// <para>
/// <b>A reading that shows four zeroes and a reading taken over a Tick with no Trips in it are the
/// same value</b>, which is worth knowing before drawing a conclusion from one: these count Trips that
/// <em>ended</em> in the interval, not Trips that existed.
/// </para>
/// </remarks>
public readonly record struct TripActivity(
    RuleFlow Completed,
    RuleFlow NoRouteFound,
    RuleFlow ExceededCommuteBudget,
    RuleFlow Stranded,
    TripCostProfile Costs = default,
    RuleFlow WalkLegs = default,
    RuleFlow DriveLegs = default);

/// <summary>
/// The Trip cost histogram over one Census interval: one flow per <see cref="TripCostBucket"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Seven named fields rather than an array, because a Census family is a fixed shape and an array
/// is a variable one.</b> Every other activity record here is positional and read by name at the
/// write site, which is what makes a mis-ordered write a compiler error rather than a silently wrong
/// column — and a histogram is exactly where an off-by-one would be least visible, since every bucket
/// holds a plausible count.
/// </para>
/// <para>
/// <b>The buckets sum to the Trips created in the interval</b>, including the ones refused for their
/// length and the ones with no front door. That property is what makes the family readable beside
/// <see cref="TripActivity"/>'s Fates, whose four counters sum to the Trips that <em>ended</em>.
/// </para>
/// </remarks>
public readonly record struct TripCostProfile(
    RuleFlow UnderOneMinute,
    RuleFlow UnderTwoMinutes,
    RuleFlow UnderFourMinutes,
    RuleFlow UnderEightMinutes,
    RuleFlow UnderSixteenMinutes,
    RuleFlow UnderThirtyTwoMinutes,
    RuleFlow ThirtyTwoMinutesOrMore)
{
    /// <summary>One band's flow.</summary>
    public RuleFlow this[TripCostBucket bucket] => bucket switch
    {
        TripCostBucket.UnderOneMinute => UnderOneMinute,
        TripCostBucket.UnderTwoMinutes => UnderTwoMinutes,
        TripCostBucket.UnderFourMinutes => UnderFourMinutes,
        TripCostBucket.UnderEightMinutes => UnderEightMinutes,
        TripCostBucket.UnderSixteenMinutes => UnderSixteenMinutes,
        TripCostBucket.UnderThirtyTwoMinutes => UnderThirtyTwoMinutes,
        TripCostBucket.ThirtyTwoMinutesOrMore => ThirtyTwoMinutesOrMore,
        _ => throw new ArgumentOutOfRangeException(nameof(bucket)),
    };
}
