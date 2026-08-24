using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>
/// Tick phase 6, ahead of the Zone Rules: the Unplaced Pool draining into vacant declared capacity
/// in Buildings that already stand. <c>02 §5.2</c> step 2, and <c>adr/0069</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Until this existed, <c>World.Place</c> had exactly one caller and it was inside
/// <c>ZoneRuleEngine.Create</c>.</b> Nothing put a Household into a Building that already stood, so
/// the only way to be housed was for somebody to raise you a house — one Household deep, once, at
/// construction. Of <c>02 §5.2</c>'s six steps only step 5 existed, and the missing one was read as a
/// missing *number* by two ledger entries before it was read as a missing *mechanism*.
/// </para>
/// <para>
/// <b>The ordering is the decision.</b> <c>02 §1.1</c> calls the phase ordering the determinism
/// contract, and running ahead of the Zone Rules is what makes their create predicate a statement
/// about **vacancy** rather than about population: a Household still in the Pool when a developer
/// looks is one the standing stock could not house. A developer does not build while there are empty
/// flats. That is strictly stronger than what it replaces — the old predicate read a Pool that
/// construction drained one Household at a time, so a wide sample could build ahead of demand by up
/// to the sample size within one trigger.
/// </para>
/// <para>
/// <b>It is blind, on <c>adr/0054</c>'s existing reasoning and not on a new one.</b> Acceptance needs
/// rent, a commute and a tolerance; none exists, so any member would take any dwelling. This moves
/// that same draw to a second site rather than extending the argument, and <c>02 §5.2</c> step 2b's
/// hard filter — *affordable? at least one reachable job in budget?* — lands **here** when the choice
/// model arrives, instead of being retrofitted into a Zone Rule where it has no business being.
/// </para>
/// <para>
/// <b>Sampled rather than exhaustive, which is <c>02 §5.3</c>: sampling is a behaviour model and not
/// an optimisation.</b> A Household considers <c>candidates</c> dwellings and takes the first with
/// room; finding none, it waits for its next occasion. Nothing is recorded about why, because a
/// refusal reason is milestone 9a's and there is no reason to record while the filter is blind.
/// </para>
/// </remarks>
public sealed class PlacementEngine
{
    private readonly World _world;
    private readonly WorldKey _key;
    private readonly Movement.TripEngine _trips;

    /// <summary>Where the Pool sample is written. Grown to the widest sample, then reused.</summary>
    private int[] _sample = [];

    private int _tickConsidered;
    private int _tickPlaced;
    private int _tickDeparted;
    private int _tickRetired;
    private int _tickFounded;
    private int _tickPremised;

    private RuleFlow _consideredFlow;
    private RuleFlow _placedFlow;
    private RuleFlow _departedFlow;
    private RuleFlow _retiredFlow;
    private RuleFlow _foundedFlow;
    private RuleFlow _premisedFlow;

    /// <param name="world">The tables this drains, and the Ruleset it drains under. Not copied.</param>
    /// <param name="key">The world seed, as the draws' first coordinate.</param>
    /// <param name="trips">
    /// Where the move-in Trip is started. <b>Placement owns it because placement is what supplies the
    /// missing endpoint</b> — <c>adr/0129</c>: an arrival joins the Pool at a gate and the Trip
    /// happens when somebody gives it a destination, which is here and nowhere else.
    /// </param>
    public PlacementEngine(World world, WorldKey key, Movement.TripEngine trips)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(trips);

        _world = world;
        _key = key;
        _trips = trips;
    }

    /// <summary>
    /// Reads what placement did since the last call, and resets the counters.
    /// </summary>
    /// <remarks>
    /// <b>Two flows rather than one, because the interesting quantity is the gap between them.</b>
    /// *Considered* against *placed* is the housing shortage as a rate: a Pool that is being looked at
    /// and not housed is a city out of dwellings, where a Pool that is not being looked at is a
    /// mechanism that has stopped. A single *placed* counter reads identically in both cases, which is
    /// the shape slice 7 task 9 already found once — <c>evaluations − due</c> — and it is worth not
    /// finding a third time.
    /// </remarks>
    public PlacementActivity Drain()
    {
        var activity = new PlacementActivity(
            _consideredFlow, _placedFlow, _departedFlow, _retiredFlow, _foundedFlow,
            _premisedFlow);

        _consideredFlow = default;
        _placedFlow = default;
        _departedFlow = default;
        _retiredFlow = default;
        _foundedFlow = default;
        _premisedFlow = default;

        return activity;
    }

    /// <summary>
    /// Runs one pass if this Tick's interval divides, housing whom it can.
    /// </summary>
    /// <param name="tick">The Tick being run: the trigger test and the draws' key.</param>
    public void Place(Ticks tick)
    {
        PlacementRuleset placement = _world.Rules.Placement;

        if (!placement.Runs || tick.Raw % placement.Interval != 0)
        {
            CloseTick();
            return;
        }

        int pool = _world.UnplacedPool.Count;

        if (pool == 0)
        {
            // ⚠ NOT a return. The two pools are independent collections that happen to share a
            // trigger, and returning here would make the unpremised sink conditional on there being
            // unhoused HOUSEHOLDS -- so a city that housed everybody would stop retiring shops, and
            // adr/0006's bound would hold only while the other pool was non-empty.
            Tenant(tick);
            Retire(tick);
            Found(tick);
            CloseTick();
            return;
        }

        Span<int> into = Scratch(placement.SampleFor(pool));
        int drawn = DrawPool(into, tick, pool);

        for (int i = 0; i < drawn; i++)
        {
            _tickConsidered++;

            // The positions were all drawn against the Pool as it stood at the top of the pass, and
            // Place shrinks it -- swapping the last member into the vacated position. So a position
            // may now be past the end (nobody), or may name somebody other than whoever was drawn
            // (the swapped-in member). The first is skipped and the second is accepted: both are
            // still a draw over the Pool, and re-drawing to keep the sample exact would key the
            // second draw off how many earlier ones happened to succeed. Bounding first is not a
            // nicety -- At past the end hands back a default Handle and Resolve throws on it.
            int position = into[i];

            if (position >= _world.UnplacedPool.Count)
            {
                continue;
            }

            Handle<Household> seeker = _world.UnplacedPool.At(position);
            int slot = _world.Households.Rows.Resolve(seeker);

            if (TryHouse(seeker, slot, tick))
            {
                _tickPlaced++;
                continue;
            }

            // The bound is tested AFTER the attempt, not before it. A Household past its duration
            // that would have found a home this occasion is housed rather than sent away -- "failed
            // repeatedly, gave up" is the channel's own wording, and giving up in front of an empty
            // dwelling is not that. It also keeps the check off the path of every successful
            // placement, where it could only ever say no.
            if (GivesUp(position, tick))
            {
                _world.Depart(seeker);
                _tickDeparted++;
            }
        }

        Tenant(tick);
        Retire(tick);
        Found(tick);
        CloseTick();
    }

    /// <summary>
    /// Sends unpremised Businesses past the give-up bound out of the city.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The unpremised pool's whole mechanism today, and it is a SINK with no source beside it</b>
    /// (<see href="../../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md">adr/0142</see>,
    /// milestone 25 task 5). The Household half of this pass tries to house somebody and only then
    /// asks whether they have given up; this half has nothing to try, because ***nothing tenants a
    /// Business*** — <c>World.CreateBusiness</c> has no production caller and the placement pass that
    /// would is milestone <b>27</b>'s. So the bound is asked directly, and it is the only exit.
    /// </para>
    /// <para>
    /// <b>It rides the SAME trigger, the SAME sample derivation and the SAME bound, and every one of
    /// those is a decision not to introduce a number.</b> A second cadence, a second sample rule or a
    /// second duration would each be hash-bearing and owed a ratifier
    /// (<c>adr/0052</c>) — and no world contains a Business to ratify one against, which is
    /// <see href="../../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md">adr/0144</see>'s
    /// second half arriving as code. ***The shared bound is a declared stand-in, not a claim that the
    /// two patiences are equal.***
    /// </para>
    /// <para>
    /// <b>Sampled rather than swept, which is what keeps it inside <c>adr/0006</c>.</b> The per-member
    /// probability of being drawn is <c>interval ÷ revisit_ticks</c> — constant, independent of the
    /// pool's size, because <see cref="PlacementRuleset.SampleFor"/> scales the sample with the pool —
    /// so the drain rate is proportional to the stock and the pool's <em>size</em> is bounded. ⚠ <b>An
    /// individual Business's wait is not bounded</b>, which is <see cref="GivesUp"/>'s own remark and
    /// the same two-claims distinction: only the first is what <c>adr/0006</c> asks. Sweeping the whole
    /// pool would make the sink <c>O(pool)</c> per trigger, which is <c>02 §10</c>'s frequency sort
    /// answering the question.
    /// </para>
    /// <para>
    /// ⚠ <b>The bound is read from <c>[placement]</c>, so a Ruleset with no gate in it retires
    /// nobody.</b> That is <c>adr/0130</c>'s <em>absent means nobody ever gives up</em> reaching a
    /// second collection — coherent here for the same reason: with nothing creating a Business the
    /// pool has no inflow, and a pool with no inflow needs no sink.
    /// </para>
    /// </remarks>
    private void Retire(Ticks tick)
    {
        int pool = _world.UnpremisedPool.Count;

        if (pool == 0)
        {
            return;
        }

        PlacementRuleset placement = _world.Rules.Placement;
        Span<int> into = Scratch(placement.SampleFor(pool));

        for (int draw = 0; draw < into.Length; draw++)
        {
            ulong entity = Randomness.Mix((ulong)(uint)draw << 32);
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.UnpremisedDraw);

            into[draw] = (int)(value % (ulong)(uint)pool);
        }

        for (int i = 0; i < into.Length; i++)
        {
            // Bounded against the CURRENT count for DrawPool's stated reason: the positions were all
            // drawn against the pool as it stood at the top, and Depart shrinks it by swapping the
            // last member into the vacated slot. A position past the end names nobody, and one that
            // now names somebody else is still a draw over the pool.
            int position = into[i];

            if (position >= _world.UnpremisedPool.Count)
            {
                continue;
            }

            if (!GivesUpOnPremises(position, tick))
            {
                continue;
            }

            _world.Depart(_world.UnpremisedPool.At(position));
            _tickRetired++;
        }
    }

    /// <summary>
    /// Whether the unpremised pool member at <paramref name="position"/> has waited out the bound.
    /// </summary>
    /// <remarks>
    /// <b><see cref="GivesUp"/> with a different table, and the shared <c>[placement]</c> duration is
    /// <c>adr/0144</c>'s stand-in.</b> It is not folded into one method taking a span, because the two
    /// <c>Since</c> columns belong to two tables and a shared accessor would be a polymorphic read for
    /// the sake of four lines — <c>adr/0143</c>'s trade at a much smaller scale, and refused the same
    /// way.
    /// </remarks>
    private bool GivesUpOnPremises(int position, Ticks tick)
    {
        PlacementRuleset placement = _world.Rules.Placement;

        if (!placement.GivesUp)
        {
            return false;
        }

        // Widened before subtracting, for GivesUp's reason: Since is an int column and Tick.Raw is a
        // ulong.
        long waited = (long)tick.Raw - _world.UnpremisedPool.Since[position];

        return waited >= placement.GivesUpAfterTicks;
    }

    /// <summary>
    /// Looks at <c>candidates</c> Buildings and moves <paramref name="seeker"/> into the first with
    /// room.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>First rather than best, and that is the whole of the choice model there is.</b> With no
    /// rent, no commute and no tolerance every candidate with room scores identically, so a ranking
    /// would be a sort over equal keys — which is a hash-bearing tie-break nobody has argued. When
    /// <c>02 §5.4</c> arrives it replaces this line and the candidate list stays.
    /// </para>
    /// <para>
    /// <b>The draw is over <em>Lots</em> rather than over Buildings, and the difference is not
    /// cosmetic.</b> A Lot is a place in the city and the Lot table's slot count is the size of the
    /// city; the Building table's is a <em>recycling</em> table whose freed slots are an artefact of
    /// storage. Drawing over Buildings made <c>candidates</c> mean something the file could not
    /// state — under the shipped Ruleset roughly 55% of Building slots stand freed at any instant, so
    /// three looks bought about 1.3 real ones, and lowering the demolition rate would have silently
    /// raised the effective candidate count. Over Lots, a look that lands on a vacant one found
    /// nothing, which is a thing that happens to somebody looking for somewhere to live.
    /// </para>
    /// </remarks>
    private bool TryHouse(Handle<Household> seeker, int slot, Ticks tick)
    {
        int candidates = _world.Rules.Placement.Candidates;
        int lots = _world.Lots.Rows.SlotCount;

        if (lots == 0)
        {
            return false;
        }

        // Read once, above the loop, and it stays valid for the whole call: the Pool only churns
        // inside Place, which is the last thing this method does. Re-reading it per candidate would
        // be free and correct today, and would silently stop being correct the day anything between
        // here and Place removes a member.
        int position = _world.Households.PoolPosition(slot);

        for (int look = 0; look < candidates; look++)
        {
            // Keyed on the Household's monotonic id rather than its Pool position, so that who a
            // family looks at does not change because somebody ahead of them was housed. The look
            // ordinal separates the candidates within one occasion; the shift count is constant,
            // which is what keeps BOR0204 quiet.
            ulong entity = Randomness.Mix(
                _world.Households.Rows.IdAt(slot) ^ ((ulong)(uint)look << 32));

            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.PlacementCandidate);

            int lot = (int)(value % (ulong)(uint)lots);

            if (!_world.Lots.Rows.IsLive(lot))
            {
                continue;
            }

            // A vacant Lot is a look that found nothing, which is a real thing to happen to somebody
            // looking for somewhere to live and is why the draw is over Lots at all.
            int building = _world.Lots.BuildingOn(lot);

            if (building == Rows.NoSlot)
            {
                continue;
            }

            // Counted HERE and not after the room test, because a full dwelling is one this family
            // looked at and could not have -- which is the whole content of "considered 20 dwellings"
            // in a city with a housing shortage. Counting only the ones with room would make the
            // Evidence line read zero in exactly the city it exists to describe, and a look that
            // found a vacant Lot is still not counted, because nobody was shown a home.
            _world.UnplacedPool.Considered[position]++;

            if (!_world.HasRoom(building))
            {
                continue;
            }

            // Read BEFORE Place, which is the whole of the ordering constraint here: Place consumes
            // the Pool membership, and UnplacedTable.Leave swaps the last member into the vacated
            // position -- so a gate read afterwards is somebody else's origin, and the Trip it
            // produced would be a legitimate journey between two real Addresses.
            Handle<Building> gate = _world.UnplacedPool.GateAt(position);

            _world.Place(seeker, _world.Buildings.Rows.At(building));
            MoveIn(slot, gate, building, tick);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Whether the member at <paramref name="position"/> has been looking longer than it will look.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A duration against a start Tick, which is the whole mechanism</b>
    /// (<see href="../../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md">adr/0130</see>).
    /// The Ruleset states how long a Household keeps looking; how many looks that buys falls out of
    /// the placement cadence and nothing bounds on it. Authored the other way round, retuning
    /// <c>[placement]</c> would silently change how long families wait for a home, which is
    /// <c>adr/0059</c> one level down.
    /// </para>
    /// <para>
    /// ⚠ <b>The bound is measured, not counted, but it is <em>tested</em> on an occasion</b> — so a
    /// Household leaves on its first look after the duration expires rather than on the Tick it
    /// expires.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>And the lateness has no upper bound, only an expectation, because
    /// <see cref="DrawPool"/> draws WITH REPLACEMENT.</b> A pass takes <c>sample</c> independent
    /// uniform draws over the Pool and deduplicates nothing, so a revisit period is a <em>rate</em>
    /// and not a coverage guarantee: over one period each member is looked at about once, and about
    /// <c>1/e</c> of them are not looked at at all. An individual Household therefore waits a
    /// geometric number of periods past its duration. ***A revisit period says how often somebody is
    /// looked at on average, and never that everybody has been.***
    /// </para>
    /// <para>
    /// <b><c>adr/0006</c> is satisfied anyway, and by the sample rather than by the bound.</b> The
    /// per-member probability of being drawn is <c>interval ÷ revisit_ticks</c> — a constant,
    /// independent of the Pool's size, because <see cref="PlacementRuleset.SampleFor"/> scales the
    /// sample with the Pool. So the drain rate is proportional to the stock and the Pool's *size* is
    /// bounded even though one Household's *wait* is not. ⚠ <b>Those are two different claims and
    /// only the first is what <c>adr/0006</c> asks.</b> Whether the second one should also hold is
    /// filed rather than decided here — a rotating cursor would buy it, and it is a hash-bearing
    /// change to placement that this task does not own (<c>adr/0073</c>).
    /// </para>
    /// <para>
    /// <b>Sweeping the whole Pool every pass would remove the lag and is refused</b>: it makes the
    /// sink <c>O(pool)</c> per trigger to buy a Household leaving sooner, which is <c>02 §10</c>'s
    /// frequency sort answering the question.
    /// </para>
    /// <para>
    /// <b>Absent means nobody gives up</b>, and the loader guarantees that is only reachable in a
    /// Ruleset with no gate in it — where the Pool has no inflow and <c>adr/0006</c> is satisfied by
    /// the same absence that satisfied it before the gate existed.
    /// </para>
    /// </remarks>
    private bool GivesUp(int position, Ticks tick)
    {
        PlacementRuleset placement = _world.Rules.Placement;

        if (!placement.GivesUp)
        {
            return false;
        }

        // Widened to long before subtracting, because Since is an int column and Tick.Raw is a
        // ulong. int.MaxValue Ticks is about 2,900 in-world years, so the column is not a bound
        // anybody reaches; doing the arithmetic narrow would be a defect waiting on a run nobody
        // makes, which is cheaper to not write than to test for.
        long waited = (long)tick.Raw - _world.UnplacedPool.Since[position];

        return waited >= placement.GivesUpAfterTicks;
    }

    /// <summary>
    /// Sends a newly-housed Household's people from the gate they arrived at to their new home.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is <c>adr/0023</c>'s arrival Trip, made in the order the build can make it</b>
    /// (<c>adr/0129</c>). A Household that has just been placed is the first moment both endpoints
    /// exist: the gate has been waiting on the Pool membership since it arrived, and the dwelling is
    /// what this pass has only now chosen.
    /// </para>
    /// <para>
    /// <b>A default gate is the ordinary case and produces nothing.</b> Three of the Pool's four
    /// entry routes have no gate — a Household the city generated itself, one evicted by a
    /// demolition, one that decided to move — so most placements in a running city take this branch.
    /// ***An internal move is a real journey and a different one***, and giving it
    /// <see cref="Movement.TripPurpose.Immigration"/> would file re-housing as immigration in every readout
    /// that reads by purpose.
    /// </para>
    /// <para>
    /// <b>One Trip per Citizen rather than one per Household</b>, because <c>adr/0075</c> makes a
    /// Traveller a cursor over a <em>Citizen's</em> journey and there is no such thing as a Household
    /// on the road. It is also what makes the congestion real: a Household of four arriving is four
    /// Vehicles on the network under <c>adr/0098</c>'s per-Household mode, and collapsing them to one
    /// would understate the thing this task exists to produce.
    /// </para>
    /// <para>
    /// ⚠ <b>The Fate is not inspected and the Trip is not retried.</b> A move-in that exceeds the
    /// Commute Budget is <c>adr/0089</c> rather than a failure to handle — the map is sized by how
    /// many Commute Budgets fit across it, so a far gate is outside one by construction — and the
    /// Household is housed either way. ***Placement decides where somebody lives; the Trip records
    /// how they got there.*** Reacting to the Fate would make housing depend on travel, which is the
    /// acceptance model at milestone 16.
    /// </para>
    /// </remarks>
    private void MoveIn(int household, Handle<Building> gate, int dwelling, Ticks tick)
    {
        if (!_world.Rules.Trips.Runs || !_world.Buildings.Rows.TryResolve(gate, out int origin))
        {
            return;
        }

        // Walk rather than follow MemberNext by hand: the column is 1-based, so 0 is the terminator
        // and a raw read is both off by one and unbounded -- 0 passes a >= 0 test, which walks off
        // the end of this Household and into Citizen slot 0's list.
        foreach (int citizen in _world.Members.Walk(household))
        {
            _trips.Start(
                citizen, origin, dwelling, _world.ModeOf(citizen), Movement.TripPurpose.Immigration, tick);
        }
    }

    /// <summary>
    /// Fills <paramref name="into"/> with Pool positions this pass considers.
    /// </summary>
    /// <remarks>
    /// <b>A draw rather than the front of the queue</b>, which is <c>02 §8</c> rule 5 for the reason
    /// <c>World.Place</c> already states: a Pool that does not fully drain is what a housing shortage
    /// <em>is</em>, and under any fixed order the same Households would stay unhoused for the life of
    /// the city with nothing in any readout to explain why. The Pool is dense, so every position is a
    /// live member and nothing is discarded here.
    /// </remarks>
    private int DrawPool(Span<int> into, Ticks tick, int pool)
    {
        for (int draw = 0; draw < into.Length; draw++)
        {
            ulong entity = Randomness.Mix((ulong)(uint)draw << 32);
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.PoolDraw);

            into[draw] = (int)(value % (ulong)(uint)pool);
        }

        return into.Length;
    }

    /// <summary>A scratch span of at least <paramref name="length"/>, reused between passes.</summary>
    private Span<int> Scratch(int length)
    {
        if (_sample.Length < length)
        {
            _sample = new int[length];
        }

        return _sample.AsSpan(0, length);
    }

    /// <summary>
    /// Pooled Businesses look for premises, and the ones that find room take them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The middle of the mechanism, and the debt the build named for itself</b> (<c>adr/0147</c>).
    /// <see cref="Retire"/>'s own remark said <em>"nothing tenants a Business … the placement pass
    /// that would is milestone 27's"</em>, and complained that ***this half has nothing to try***
    /// where the Household half tries to house somebody and only then asks whether they gave up.
    /// <b>It has something to try now, and it runs FIRST for that exact reason</b> — a Business that
    /// could take premises this Tick must not be retired before it is offered any.
    /// </para>
    /// <para>
    /// <b>It introduces no number.</b> Same trigger, same sample derivation, same candidate count as
    /// the Household pass — which is <see cref="Retire"/>'s standing argument unchanged: a second
    /// cadence would be hash-bearing and owed a ratifier (<c>adr/0052</c>), and the world that would
    /// ratify one is the world this pass is building. ***A shared cadence is a declared stand-in and
    /// not a claim that the two patiences are equal.***
    /// </para>
    /// <para>
    /// <b>Any Building with room, and that is <c>adr/0147</c> rather than an omission.</b> A Business
    /// carries its own <c>jobs</c> now, so it needs no special kind to sit in — and no shipped Ruleset
    /// declares a workplace kind, which is why <c>jobs</c> sat on <c>dwelling</c> in the first place.
    /// ⚠ <b>The room it takes is a Household's room</b>: <see cref="Entities.World.Tenants"/> counts
    /// both kinds against one ceiling, so ***a city that fills with shops houses fewer people.***
    /// </para>
    /// </remarks>
    private void Tenant(Ticks tick)
    {
        int pool = _world.UnpremisedPool.Count;

        if (pool == 0)
        {
            return;
        }

        int lots = _world.Lots.Rows.SlotCount;

        if (lots == 0)
        {
            return;
        }

        PlacementRuleset placement = _world.Rules.Placement;
        Span<int> into = Scratch(placement.SampleFor(pool));

        for (int draw = 0; draw < into.Length; draw++)
        {
            ulong entity = Randomness.Mix((ulong)(uint)draw << 32);
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.PremisesDraw);

            into[draw] = (int)(value % (ulong)(uint)pool);
        }

        for (int i = 0; i < into.Length; i++)
        {
            // Bounded against the CURRENT count for Retire's stated reason: the positions were drawn
            // against the pool as it stood at the top, and Premise shrinks it by swapping the last
            // member into the vacated slot.
            int position = into[i];

            if (position >= _world.UnpremisedPool.Count)
            {
                continue;
            }

            Handle<Business> seeker = _world.UnpremisedPool.At(position);
            int slot = _world.Businesses.Rows.Resolve(seeker);
            ulong id = _world.Businesses.Rows.IdAt(slot);

            for (int look = 0; look < placement.Candidates; look++)
            {
                // Keyed on the Business's monotonic id rather than its pool position, so who a shop
                // looks at does not change because an unrelated Business left the pool ahead of it.
                ulong entity = Randomness.Mix(id ^ ((ulong)(uint)look << 32));
                ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.PremisesCandidate);

                int lot = (int)(value % (ulong)(uint)lots);

                if (!_world.Lots.Rows.IsLive(lot))
                {
                    continue;
                }

                int building = _world.Lots.BuildingOn(lot);

                if (building == Rows.NoSlot || !_world.HasRoom(building))
                {
                    continue;
                }

                _world.Premise(seeker, _world.Buildings.Rows.At(building));
                _tickPremised++;
                break;
            }
        }
    }

    /// <summary>
    /// Housed Households consider founding a Business, and the ones that can afford it do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0145</c>'s founding channel and its amendment's trigger</b> — ***a Household founds
    /// on its own MEANS and never on the city's NEED.*** Nothing here reads how many Businesses exist,
    /// how many premises are vacant or whether anything went unsold: a family with savings opens a
    /// shop, and whether the city needed one is answered afterwards by whether placement finds it
    /// premises. ⚠ <b>A condition that read a shortage would be the RCI meter this design refuses</b>,
    /// however it was spelled.
    /// </para>
    /// <para>
    /// <b>LAST in the pass, after <see cref="Retire"/>, and the order is load-bearing twice.</b>
    /// <see cref="Scratch"/> hands out ONE shared buffer, so the three sampling steps are kept apart
    /// by sequencing rather than by separate arrays — each finishes with its span before the next
    /// asks for one. ⚠ <b>That is a real constraint on this method and it is undocumented at
    /// <see cref="Scratch"/> itself</b>; moving this call earlier would alias a live span. And
    /// semantically: founding before retiring would put brand-new Businesses into the pool that
    /// <see cref="Retire"/> then samples and cannot possibly retire, spending draws on rows whose
    /// clock started this Tick.
    /// </para>
    /// <para>
    /// <b>Drawn over SLOTS rather than over live Households, and the sample is derived from the slot
    /// count to match.</b> Then every live housed Household has probability
    /// <c>interval ÷ reconsider_ticks</c> of being asked, exactly as authored — where deriving the
    /// sample from the live count and rejecting dead slots would silently scale the realised rate by
    /// the live fraction. ⚠ <b>Rejection therefore costs draws and never bias</b>, which is the
    /// property worth having.
    /// </para>
    /// <para>
    /// <b>With replacement, on <c>DrawPool</c>'s precedent</b>: about <c>1/e</c> of Households go
    /// unlooked-at in any period. ***That is a rate and not coverage***, which is the correction
    /// <c>[placement] revisit_ticks</c> already carries and which applies here word for word.
    /// </para>
    /// </remarks>
    private void Found(Ticks tick)
    {
        FoundingRuleset founding = _world.Rules.Founding;

        if (!founding.Runs)
        {
            return;
        }

        int slots = _world.Households.Rows.SlotCount;

        if (slots == 0)
        {
            return;
        }

        uint interval = _world.Rules.Placement.Interval;
        int trades = _world.Rules.BusinessKindCount;

        // The loader refuses [founding] in a file declaring no trade, so this is a Ruleset built in
        // code rather than loaded. Returning is right rather than throwing: a pass is not the place
        // to diagnose a malformed Ruleset, and founding nothing is the honest consequence.
        if (trades == 0)
        {
            return;
        }

        Span<int> into = Scratch(founding.SampleFor(slots, interval));

        for (int draw = 0; draw < into.Length; draw++)
        {
            ulong entity = Randomness.Mix((ulong)(uint)draw << 32);
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.FoundingDraw);

            into[draw] = (int)(value % (ulong)(uint)slots);
        }

        for (int i = 0; i < into.Length; i++)
        {
            int slot = into[i];

            if (!_world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            Handle<Household> household = _world.Households.Rows.At(slot);

            // Housed only. An unhoused Household is in the Unplaced Pool looking for somewhere to
            // live, and founding a shop out of that queue would put one Household in two searches at
            // once -- adr/0145's amendment states the restriction and this is it.
            if (!_world.Buildings.Rows.IsValid(_world.Households.Dwelling[slot]))
            {
                continue;
            }

            // THE MEANS TEST, and the whole of the trigger. No shop count, no vacancy, no demand.
            if (_world.BalanceOf(household).Raw < founding.FoundingBand.Raw)
            {
                continue;
            }

            // Uniform over the declared trades, on its own tag -- see PurposeTag.FoundingTrade. Drawn
            // on the HOUSEHOLD's monotonic id rather than the sample index, because this is a decision
            // about a known Household rather than a choice of position: two draws of the same slot in
            // one pass must not open two different trades for one reason and the same trade for
            // another.
            ulong pick = Randomness.Draw(
                _key,
                Randomness.Mix(_world.Households.Rows.IdAt(slot)),
                tick,
                PurposeTag.FoundingTrade);

            var kind = (byte)((pick % (ulong)(uint)trades) + 1);

            _world.Found(household, kind, founding.FoundingBand, tick);
            _tickFounded++;
        }
    }

    /// <summary>Folds this Tick's counts into the flows and resets them.</summary>
    private void CloseTick()
    {
        _consideredFlow = _consideredFlow.Fold(_tickConsidered);
        _placedFlow = _placedFlow.Fold(_tickPlaced);
        _departedFlow = _departedFlow.Fold(_tickDeparted);
        _retiredFlow = _retiredFlow.Fold(_tickRetired);
        _foundedFlow = _foundedFlow.Fold(_tickFounded);
        _premisedFlow = _premisedFlow.Fold(_tickPremised);

        _tickConsidered = 0;
        _tickPlaced = 0;
        _tickDeparted = 0;
        _tickRetired = 0;
        _tickFounded = 0;
        _tickPremised = 0;
    }
}

/// <summary>What the placement pass did over a Census interval.</summary>
/// <remarks>
/// <b>Three flows, and the third is a different <em>kind</em> of quantity from the first two.</b>
/// `CONTEXT` → Departure: *"Departure rate is a distinct demand signal from Pool size: Pool size is a
/// stock of latent demand, departure rate is a flow measuring how badly the city is failing to convert
/// its own attractiveness into capacity. A city can have a large Pool and be healthy, or a small Pool
/// and be in crisis; only the flow distinguishes them."* Reporting the Pool without this reports the
/// stock and calls it the diagnosis.
/// </remarks>
/// <para>
/// ⚠ <b><see cref="Retired"/> is the fourth and it counts a DIFFERENT POOL.</b> The first three are
/// the Unplaced Pool's — Households — and this one is the unpremised pool's Businesses
/// (<c>adr/0142</c>, milestone 25 task 5). ***They share a pass and they are not summable***: a reader
/// adding Departed to Retired would be adding families to shops. It is here rather than in an activity
/// record of its own because the pass is one pass and splitting the record would imply a second
/// trigger.
/// </para>
/// <param name="Considered">Pool members looked at.</param>
/// <param name="Placed">Of those, the ones that found a dwelling.</param>
/// <param name="Departed">Of those, the ones that gave up and left (<c>adr/0130</c>).</param>
/// <param name="Retired">
/// Unpremised <b>Businesses</b> that gave up and emigrated. ⚠ <b>Not a subset of
/// <paramref name="Considered"/></b>, which counts Households.
/// </param>
public readonly record struct PlacementActivity(
    RuleFlow Considered, RuleFlow Placed, RuleFlow Departed, RuleFlow Retired, RuleFlow Founded,
    RuleFlow Premised);
