namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// The Attended half of <c>adr/0032</c>: once a Day, a Household travels to a service Building, or
/// finds it cannot.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Services are delivered by Trips, not by coverage.</b> <c>02 §2.4</c> once specified
/// coverage as a decaying distance field; the single case that killed it is ***a school across an
/// uncrossable Arterial is 200 m away and there is no route.*** This asks the Road Graph.
/// </para>
/// <para>
/// <b>Only the ATTENDED mode lives here.</b> <c>adr/0032</c> sorts Services by <em>who moves</em>:
/// Attended (the Household travels), Dispatched (the Service travels) and Networked (nobody moves).
/// The other two are <c>adr/0070</c> <em>unbuilt</em>, and <c>RulesetLoader.ReadServes</c> refuses
/// their names so the absence is legible rather than silent.
/// </para>
/// <para>
/// 🔴 <b>THE PASS IS TWO PASSES, AND WHAT MADE IT TWO IS ADMISSION RATHER THAN COST.</b> A school
/// has places, so somebody is turned away — and the order the population is walked in <em>is</em>
/// the order the places are given out. Walking it in slot order gave them out by <b>Household
/// age</b>: a slot is allocated when a Household is created, so ***the oldest families in the city
/// took the school every Day and the newest never did.*** So the first pass <see cref="Ask">asks</see>
/// every Household how far its nearest service Building is, the occasions are
/// <see cref="Order">ordered by that distance</see>, and the second pass <see cref="Serve">serves</see>
/// them in that order. <b>The family living nearest a school is admitted first, and the family
/// turned away is the one living furthest from any of them.</b>
/// </para>
/// <para>
/// ⚠ <b>THE ORDER IS OVER OCCASIONS AND NOT OVER ONE SCHOOL'S APPLICANTS, and the two come apart.</b>
/// Each family is keyed by the distance to <em>its own</em> nearest school, so a family whose
/// nearest is full can be admitted at a further one ahead of a family living closer to that further
/// one. Closing that gap is deferred acceptance — a rejected applicant re-keyed on its next
/// candidate and re-queued, displacing whoever it beats — and ***that costs a route per
/// displacement*** where this costs a sort. What is left is a mis-ordering between two families at
/// one school; what it replaces was a mis-ordering across the whole city, on a property that has
/// nothing to do with schools.
/// </para>
/// <para>
/// 🔴 <b>AND IT ENDS THE SATISFICING BREAK, WHICH IS A DESIGN CHANGE AND NOT A TIDY-UP.</b> The
/// walk used to stop at the first <c>Fast</c>-rung candidate in slot order (<c>adr/0017</c>) and
/// never rank the set; it now routes every candidate in the box and takes the cheapest.
/// ***Nothing can admit nearest-first without knowing who is nearest*** — the key is a distance, and
/// once it has been paid for, a family walking past its nearest school to a further one it happened
/// to meet earlier in slot order is a deliberate absurdity rather than a saving. ⚠ <b>What makes the
/// break affordable to lose is <see cref="Reach"/>'s own argument for having no <c>candidates</c>
/// key</b>: service Buildings are placed by hand, one verb at a time, so the set being ranked is
/// bounded by the player and not by the city. The blow-up <c>adr/0017</c> exists to refuse is a
/// Household ranking the city; this ranks what the player has built.
/// </para>
/// <para>
/// 🔴 <b>THE FAILURE IS PER-OCCASION HERE AND THAT IS NOT A REGRESSION OF THE THING
/// <c>RuleEngine.RefreshNeed</c> FIXED.</b> The bought Needs had to become a duration because ***a
/// success is an event and a failure is a state***: a blocked Rule is put to sleep on its Bin by
/// <c>RuleEngine.Stop</c>, so a shortage nothing ends buys <em>one</em> failed occasion and then
/// silence. <b>An attended Need has no such subscription.</b> Its occasion is this sweep, the sweep
/// is daily, and it visits a Household whose school is unreachable on every one of those Days — so
/// the per-occasion step already <em>is</em> the per-Day rate the Ruleset authors, and a second
/// duration mechanism would double-count it. ***The asymmetry was a property of how the occasion
/// arrived, not of Needs.***
/// </para>
/// <para>
/// ⚠ <b>A whole-population pass once a Day</b>, which is <c>RuleEngine.SweepNeeds</c>' shape and its
/// precedent. ***It starts every school Trip in the city on one Tick***, and that spike is real:
/// <c>CommuteEngine</c> avoids the equivalent by partitioning on a Shift start hour
/// (<c>adr/0101</c>), and a school day has no hours key to partition on. The successor is the Event
/// Wheel, which is what carries an occasion whose next firing genuinely varies. <b><c>plans/0013</c>
/// carries a row for it as of 2026-08-31</b> (<c>adr/0073</c>), filed against the <em>spike</em> rather
/// than the pass — ***a mean over the Day is the wrong denominator for a cost paid inside one Tick.***
/// </para>
/// </remarks>
public sealed class ServiceEngine
{
    private readonly World _world;
    private readonly TripEngine _trips;

    /// <summary>Reused between passes: the service Buildings standing in this world, by slot.</summary>
    /// <remarks>
    /// <b>Gathered ONCE a pass, and not kept between passes.</b> Per Household it would be
    /// <c>O(households × buildings)</c> a Day; as a saved index it would be derived state whose
    /// rebuild somebody has to write, which is the hole <c>DerivedRebuildAuditTests</c> exists to
    /// find. Per pass is <c>O(buildings)</c> a Day and needs neither.
    /// </remarks>
    private int[] _services = [];

    /// <summary>Reused across every route this pass asks for. A per-candidate allocation would be
    /// one in the daily pass's inner loop.</summary>
    private readonly WalkScratch _walk = new();

    private int _serviceCount;

    // ---- the occasions, between Ask and Serve ---------------------------------------------------
    //
    // Parallel arrays rather than an array of structs, so the sort moves _order alone and never a
    // payload. SCRATCH AND NEVER STATE, on _services' argument: rebuilt from the world every pass,
    // so nothing here is saved, hashed, or a column somebody has to remember to rebuild on load.

    /// <summary>The Household whose occasion this is.</summary>
    private int[] _slots = [];

    /// <summary>Who makes the journey — <see cref="Traveller"/>'s answer, kept rather than re-asked.</summary>
    private int[] _travellers = [];

    /// <summary>The Building the journey starts at.</summary>
    private int[] _homes = [];

    /// <summary>The nearest service Building, decided before anybody had taken a place.</summary>
    private int[] _providers = [];

    /// <summary>Where the family actually went, or <see cref="Rows.NoSlot"/> if it was turned away.</summary>
    private int[] _settled = [];

    /// <summary>The sort key: that distance, with the Household slot underneath it.</summary>
    private long[] _keys = [];

    /// <summary>Indices into the four arrays above, and the only thing the sort moves.</summary>
    private int[] _order = [];

    private int _occasionCount;

    /// <summary>Which Need <see cref="LastProviders"/> describes.</summary>
    private Need _lastNeed;

    private int _tickAttended;
    private int _tickUnreached;
    private int _tickNoService;
    private int _tickFull;

    /// <param name="world">The world whose Households attend.</param>
    /// <param name="trips">The one door a Trip is created through.</param>
    public ServiceEngine(World world, TripEngine trips)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(trips);

        _world = world;
        _trips = trips;
    }

    /// <summary>Why an occasion found nobody to send its traveller to.</summary>
    /// <remarks>
    /// <b>Four values against three counters, and the fourth is the point.</b>
    /// <see cref="Doorstep"/> degrades the Need and increments nothing: it is <c>adr/0079</c>'s hole
    /// — a dwelling whose Address is on no Segment — and it is neither *there is no school* nor *the
    /// network is severed*. ***A counter that absorbed it would report a defect in the subdivider as
    /// a defect in the city's schools.***
    /// </remarks>
    private enum Miss
    {
        /// <summary>A provider was found. Not a miss.</summary>
        None,

        /// <summary>The journey could not start: no Lot, off the Cell grid, or no Address.</summary>
        Doorstep,

        /// <summary>No service Building for this Need stood inside the box at all.</summary>
        NoService,

        /// <summary>One stood, and the Road Graph could deliver nobody inside the Budget.</summary>
        Unreached,

        /// <summary>One stood, somebody could reach it, and it had no place left today.</summary>
        Full,
    }

    /// <summary>School runs completed on the Tick just closed.</summary>
    public int Attended => _tickAttended;

    /// <summary>
    /// Occasions where a service Building stood in range and the Road Graph could not deliver
    /// anybody to it within the Commute Budget.
    /// </summary>
    /// <remarks>
    /// <b>Counted apart from <see cref="NoService"/> because they are different cities</b> — no
    /// schools against schools nobody can reach. The second is <c>adr/0032</c>'s Severance:
    /// <em>"a Family cannot solve it by driving further."</em>
    /// </remarks>
    public int Unreached => _tickUnreached;

    /// <summary>Occasions where no service Building stood within the box at all.</summary>
    public int NoService => _tickNoService;

    /// <summary>
    /// Occasions where a service Building stood in range, the Road Graph could deliver somebody to
    /// it, and <b>it had no place left today</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The third city, and it is counted apart for <see cref="Unreached"/>'s reason rather than
    /// by analogy with it.</b> No school, a school nobody can reach and a school that is full are
    /// three diagnoses asking the player for three different things — build one, mend the network,
    /// build ANOTHER one — and ***a mechanism that cannot tell them apart is telling the player to
    /// guess.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is zero in every world that states no <c>[capacity] floor_tiles_per_place</c></b>,
    /// which is every Ruleset shipped before that key existed. A zero here is *no ceiling in this
    /// city* as often as it is *nobody was turned away*, so read it beside the rate.
    /// </para>
    /// <para>
    /// 🔴 <b>WHO it names moved when admission stopped being slot order.</b> The families turned
    /// away are the ones living furthest from any service Building, which is a sentence a player can
    /// act on — where *the oldest Households take the places* was a sentence about the save file.
    /// </para>
    /// </remarks>
    public int Full => _tickFull;

    // ---- the assignment, for an instrument ------------------------------------------------------

    /// <summary>
    /// Which Need the arrays below describe — <see cref="Need.Education"/> unless a world serves
    /// Health and nothing else.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The two Needs share one set of buffers</b>, so a world serving both leaves Health's pass
    /// standing here and Education's overwritten. That is the reason this property exists rather
    /// than the reader assuming: ***a readout that cannot say what it is of is a readout of
    /// whatever ran last.***
    /// </remarks>
    public Need LastNeed => _lastNeed;

    /// <summary>The Households that had an occasion on the last Day this pass ran, in slot order.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>AN INSTRUMENT SURFACE, AND IT EXISTS BECAUSE EVERY OTHER READING OF THIS MECHANISM IS
    /// A COUNT.</b> <see cref="Attended"/>, <see cref="Full"/>, <see cref="Unreached"/> and
    /// <see cref="NoService"/> say how many; ***nothing said WHICH***, so the day admission stopped
    /// being slot order the <c>--school</c> panel printed byte-for-byte the same output it had
    /// printed before. A change of identity is invisible to a tally of quantity, which is the same
    /// blind spot that let one school serve a whole city at a reported 100%.
    /// </para>
    /// <para>
    /// ⚠ <b>It holds only the occasions that reached the QUEUE</b>, which is every Household that
    /// could route to something. A family with no school in its box, no route inside the Budget, or
    /// no Address to leave from failed in <see cref="Ask"/> and is not here — those three are
    /// settled before the ordering exists and no ordering could have changed them.
    /// </para>
    /// <para>
    /// ⚠ <b>Valid on the Tick the pass ran and meaningless on any other</b>, exactly as the four
    /// counters are: the buffers are scratch, not state, so nothing saves them, nothing hashes them
    /// and a reload does not rebuild them.
    /// </para>
    /// </remarks>
    public ReadOnlySpan<int> LastHouseholds => _slots.AsSpan(0, _occasionCount);

    /// <summary>
    /// Where each of <see cref="LastHouseholds"/> went, or <see cref="Rows.NoSlot"/> for a family
    /// turned away at a full door.
    /// </summary>
    /// <remarks>
    /// <b>Parallel to <see cref="LastHouseholds"/> by index and not by slot.</b> A
    /// <see cref="Rows.NoSlot"/> here is <see cref="Full"/> and can be nothing else, for
    /// <see cref="LastHouseholds"/>' reason.
    /// </remarks>
    public ReadOnlySpan<int> LastProviders => _settled.AsSpan(0, _occasionCount);

    /// <summary>
    /// Runs one Day's attendance: every Household that has somebody to send, sending them.
    /// </summary>
    /// <remarks>
    /// <b>Three front-door refusals, all silence rather than a throw</b>, on
    /// <c>CommuteEngine.Generate</c>'s rule: no attendance rates, no Commute Budget to judge reach
    /// against, or not a Day boundary. None is a defective Ruleset — every file shipped before this
    /// mechanism is the first of them.
    /// </remarks>
    /// <param name="tick">The Tick being stepped.</param>
    public void Attend(Ticks tick)
    {
        _tickAttended = 0;
        _tickUnreached = 0;
        _tickNoService = 0;
        _tickFull = 0;

        NeedRuleset needs = _world.Rules.Needs;

        if (!needs.Attends || tick.Raw % Ticks.PerDay != 0)
        {
            return;
        }

        TripRuleset trips = _world.Rules.Trips;

        if (!trips.HasCommuteBudget)
        {
            return;
        }

        Cells radius = EmploymentEngine.Radius(trips.CommuteBudget, _world.Rules.Roads.WalkSpeed);

        // World.TryArrive's line, for its reason: FloorDiv rather than raw '/' because BOR0203 is an
        // error for the second, and stating the rounding is the rule even where a Tick count never
        // crosses zero. Exact here, because this pass only runs on a Day boundary.
        int day = (int)IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);

        // Ordered by Need id rather than by anything meaningful, and it must stay fixed: both passes
        // create Trips, and swapping them renumbers every Trip id the State Hash folds.
        AttendAll(Need.Education, radius, needs, trips, tick, day);
        AttendAll(Need.Health, radius, needs, trips, tick, day);
    }

    /// <summary>One Day's attendance for one Need, across the whole population.</summary>
    /// <remarks>
    /// <b>Ask, order, serve.</b> The three steps are separate because the middle one needs every
    /// answer before it can decide anything — ***an ordering is not a property any one Household
    /// has*** — and because a place taken in the third would otherwise change an answer the first
    /// had already given.
    /// </remarks>
    private void AttendAll(
        Need need, Cells radius, NeedRuleset needs, TripRuleset trips, Ticks tick, int day)
    {
        // 🔴 THE GATE IS THE RULESET AND NEVER THE CITY, AND THE FIRST SPELLING OF THIS GOT IT
        // WRONG. It returned here when `Gather` found no standing school -- which conflates two
        // states that are not alike at all: A RULESET THAT DECLARES NO SCHOOLS HAS NO MECHANISM, AND
        // A CITY THAT HAS BUILT NONE HAS FAILED. Under the old spelling a player who never placed a
        // school got no degrade at all, so the one city the verb exists to punish was the one city
        // where Education stayed pinned at zero -- ***the mechanism rewarding the player for not
        // using it.***
        //
        // ⚠ It was caught by rulesets/schooled.toml's HEADER, which stated the opposite of what this
        // did: "a run of this file with no service command has a school kind, an education rate, and
        // not one school -- every Household with a child fails its occasion every Day." That is
        // adr/0093 from the inside, and the sentence was right.
        //
        // The rates and the kinds are asked separately because a Ruleset can legally have one without
        // the other in a fixture -- the loader refuses the pair in a file, and a Ruleset composed in
        // code is not a file. Neither alone is a city where this Need can move.
        if (needs.DegradeOf(need) <= 0 || !_world.Rules.ServesAny(need))
        {
            return;
        }

        // Gathers into an EMPTY list where nothing stands, which is what makes the paragraph above
        // true rather than merely stated: every occasion then finds no school in its box, is counted
        // as such, and degrades.
        Gather(need);

        Column<int>? depth = _world.Households.NeedColumn(need);

        if (depth is null)
        {
            return;
        }

        _lastNeed = need;

        Ask(need, depth, radius, needs, trips, day);
        Order();

        for (int i = 0; i < _occasionCount; i++)
        {
            Serve(_order[i], need, depth, radius, needs, trips, tick, day);
        }
    }

    /// <summary>
    /// Pass one: every Household that has an occasion, and how far its nearest provider is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A Household with nobody to send has NO OCCASION, and that is the load-bearing
    /// distinction in this method.</b> A childless Household is not badly schooled — it is not
    /// schooled at all, and there is nothing for a school to do for it. Degrading it would make
    /// <see cref="HouseholdTable.Education"/> a reading of the city's <em>demographics</em> wearing
    /// the name of its schools, and every world without <c>[[life_stage]]</c> would report a
    /// universal schooling crisis. ***An absent occasion is not a failed one.***
    /// </para>
    /// <para>
    /// ⚠ <b>An unplaced Household has no occasion either, for
    /// <c>EmploymentEngine.Home</c>'s reason</b>: attendance is anchored at a dwelling, so somebody
    /// in the Unplaced Pool has no origin to travel from. The queue they are in is the housing one.
    /// </para>
    /// <para>
    /// 🔴 <b>THE THREE FAILURES THAT DO NOT DEPEND ON A PLACE ARE SETTLED HERE, and settling them
    /// here is what keeps the ordering honest.</b> No school in the box, no route inside the Budget
    /// and no doorstep to leave from are all true before anybody has been admitted to anything, so
    /// ***a family that was never going to attend must not sit in a queue ahead of one that was.***
    /// Only <see cref="Miss.Full"/> survives into pass two, because it is the only one an earlier
    /// occasion can cause.
    /// </para>
    /// </remarks>
    private void Ask(
        Need need, Column<int> depth, Cells radius, NeedRuleset needs, TripRuleset trips, int day)
    {
        _occasionCount = 0;

        HouseholdTable households = _world.Households;

        for (int slot = 0; slot < households.Rows.SlotCount; slot++)
        {
            if (!households.Rows.IsLive(slot))
            {
                continue;
            }

            int traveller = Traveller(slot, need);

            if (traveller < 0)
            {
                continue;
            }

            if (!_world.Buildings.Rows.TryResolve(households.Dwelling[slot], out int home))
            {
                continue;
            }

            // Asked WITHOUT the places, so this answer is the city's geometry and nothing else: the
            // key has to mean "how far this family lives from a school" on every Day, and a key that
            // moved because somebody else got in first would be sorting on the previous Day's luck.
            Miss miss = Reach(
                home, _world.ModeOf(traveller), radius, trips, day, respectPlaces: false,
                out int provider, out TravelTime cost);

            if (miss != Miss.None)
            {
                Fail(miss, need, depth, slot, needs);
                continue;
            }

            Enqueue(slot, traveller, home, provider, cost);
        }
    }

    /// <summary>Puts the occasions in admission order: nearest first, oldest Household on a tie.</summary>
    /// <remarks>
    /// <para>
    /// <b>The key is <c>(distance, slot)</c> packed into one <c>long</c>, and the packing is what
    /// makes the tie-break a stated rule rather than an artefact.</b> <c>Span.Sort</c> is
    /// introspective and therefore unstable; equal keys would come out in whatever order the
    /// partitioning left them, which is deterministic but is nobody's decision. Ties are common
    /// here — two families on one Segment are the same distance from the same school — so the
    /// second field is not a formality. ***Slot order decides a tie and no longer decides the
    /// queue.***
    /// </para>
    /// <para>
    /// ⚠ <b>Allocation-free</b>, which is <c>RuleEngine.Apply</c>'s reason for the same overload:
    /// the key span sorts the payload span beside it, and both are the reused buffers.
    /// </para>
    /// </remarks>
    private void Order() =>
        _keys.AsSpan(0, _occasionCount).Sort(_order.AsSpan(0, _occasionCount));

    /// <summary>Pass two: one occasion, in admission order — send somebody, or record that nobody could go.</summary>
    /// <remarks>
    /// <para>
    /// <b>The place is taken here and not in <see cref="Ask"/></b>, so the tally cannot depend on
    /// anything the ordering has not already decided — and the family behind this one in
    /// <em>distance</em> order meets a school that has already been attended today rather than one
    /// that is about to be.
    /// </para>
    /// <para>
    /// ⚠ <b>A full first choice is not a failure; the family walks on.</b> That second walk is the
    /// only route this method pays for, and it is paid by nobody in the ordinary world where nothing
    /// is full. A <see cref="Reach"/> that failed on a full candidate would turn a family away with
    /// a school standing empty next door.
    /// </para>
    /// </remarks>
    private void Serve(
        int entry, Need need, Column<int> depth, Cells radius, NeedRuleset needs, TripRuleset trips,
        Ticks tick, int day)
    {
        int slot = _slots[entry];
        int traveller = _travellers[entry];
        int home = _homes[entry];
        int provider = _providers[entry];

        TravelMode mode = _world.ModeOf(traveller);

        if (!_world.HasServicePlace(provider, day))
        {
            // Pass one already proved something reachable stands in this box, so the only miss this
            // walk can come back with is Full -- every other one was settled before the queue formed.
            Miss miss = Reach(
                home, mode, radius, trips, day, respectPlaces: true, out provider, out _);

            if (miss != Miss.None)
            {
                Fail(miss, need, depth, slot, needs);
                return;
            }
        }

        _settled[entry] = provider;

        _world.TakeServicePlace(provider, day);

        // ⚠ ACTIVITY IS DELIBERATELY NOT WRITTEN, and the enum deliberately does not grow. A school
        // run's RETURN journey is unbuilt -- exactly where the commute stood at 5b-bis -- so an
        // `AtSchool` value would strand every child in the city on the first Day, with nothing to
        // send them home. CitizenActivity's own remark says the set grows when a generator does; the
        // generator that grows it is the return, not this one. Meanwhile `AtHome` is where a child
        // who went to school and came back actually is at the end of the Day.
        _trips.Start(traveller, home, provider, mode, TripPurpose.School, tick);

        RuleEngine.Write(depth, slot, depth[slot] + needs.RecoverOf(need), needs.Floor);
        _tickAttended++;
    }

    /// <summary>A failed occasion: degrade the Need, and count the city it names.</summary>
    /// <remarks>
    /// <b><see cref="Miss.Doorstep"/> degrades and counts nowhere</b> — see <see cref="Miss"/>. The
    /// degrade is unconditional because a Need does not care why nobody went.
    /// </remarks>
    private void Fail(Miss miss, Need need, Column<int> depth, int slot, NeedRuleset needs)
    {
        RuleEngine.Write(depth, slot, depth[slot] - needs.DegradeOf(need), needs.Floor);

        if (miss == Miss.Full)
        {
            _tickFull++;
        }
        else if (miss == Miss.Unreached)
        {
            _tickUnreached++;
        }
        else if (miss == Miss.NoService)
        {
            _tickNoService++;
        }
    }

    /// <summary>Records one occasion and its key, growing the buffers as one.</summary>
    private void Enqueue(int slot, int traveller, int home, int provider, TravelTime cost)
    {
        Grow(_occasionCount + 1);

        _slots[_occasionCount] = slot;
        _travellers[_occasionCount] = traveller;
        _homes[_occasionCount] = home;
        _providers[_occasionCount] = provider;
        _settled[_occasionCount] = Rows.NoSlot;

        // Distance in the high half and the Household slot in the low half, so one comparison
        // orders both fields. A constant shift count -- BOR0204 reports a computed one, and this is
        // checkable by eye. A route's cost is non-negative and a slot is an index, so neither half
        // can run into the other.
        _keys[_occasionCount] = ((long)cost.Raw << 32) | (uint)slot;
        _order[_occasionCount] = _occasionCount;

        _occasionCount++;
    }

    /// <summary>Sizes the six occasion buffers together, so an index means the same thing in each.</summary>
    private void Grow(int needed)
    {
        if (_slots.Length >= needed)
        {
            return;
        }

        int size = _slots.Length == 0 ? 64 : _slots.Length;

        while (size < needed)
        {
            size *= 2;
        }

        Array.Resize(ref _slots, size);
        Array.Resize(ref _travellers, size);
        Array.Resize(ref _homes, size);
        Array.Resize(ref _providers, size);
        Array.Resize(ref _settled, size);
        Array.Resize(ref _keys, size);
        Array.Resize(ref _order, size);
    }

    /// <summary>
    /// Who makes this journey, or <c>-1</c> if nobody in this Household does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A child for Education and anybody for Health</b>, which is the one place the two Attended
    /// Needs differ and is why they are one engine rather than one method. <c>adr/0032</c>:
    /// <em>"A clinic is visited routinely"</em> — by whoever needs it — where a school is attended by
    /// the children the Household has or has not got.
    /// </para>
    /// <para>
    /// ⚠ <b>The HEAD of the member list, and it is a stable choice rather than an arbitrary one</b> —
    /// <c>IndexList.InsertOrdered</c> keeps it ascending by slot however it was filled, so a rebuild
    /// picks the same traveller as the run that saved it and the Trip ids the State Hash folds come
    /// out the same.
    /// </para>
    /// <para>
    /// 🔴 <b>A child is <c>Age == 0</c> AND a Ruleset that declares <c>[[life_stage]]</c>, and the
    /// second half is what stops this being nonsense.</b> <c>CitizenTable.Age</c> is written only by
    /// a world with demographics; in every other world every Citizen carries zero. Without the guard
    /// this would read a city of adults as a city of children, which is
    /// <c>World.IsOfWorkingAge</c>'s finding arriving from the other side — ***the column's zero
    /// means <em>child</em> in one world and <em>this world has no demographics</em> in all the
    /// others.***
    /// </para>
    /// </remarks>
    private int Traveller(int slot, Need need)
    {
        bool wantsChild = need == Need.Education;

        if (wantsChild && !_world.Rules.DeclaresLifeStages)
        {
            return -1;
        }

        foreach (int member in _world.Members.Walk(slot))
        {
            if (!wantsChild || _world.Citizens.Age[member] == 0)
            {
                return member;
            }
        }

        return -1;
    }

    /// <summary>
    /// The nearest service Building this Household can reach inside the Budget, and why not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every gathered service is looked at and there is no <c>candidates</c> key, which is the one
    /// place this pass departs from <c>EmploymentEngine.TryEmploy</c>'s shape.</b> A job search
    /// samples because jobs are everywhere — a box around a dwelling holds hundreds — and a sample is
    /// how <c>adr/0017</c>'s satisficing is spelled there. ***Service Buildings are placed by hand,
    /// one verb at a time, so their count is bounded by the player rather than by the city*** — and a
    /// sample of three over a set of two would be a Household failing to notice the only school in
    /// town. A number that cannot be derived and is not designer-facing belongs in the instrument
    /// (<c>adr/0164</c>), and here it belongs nowhere at all.
    /// </para>
    /// <para>
    /// 🔴 <b>THE CHEAPEST CANDIDATE AND NOT THE FIRST GOOD-ENOUGH ONE.</b> This used to break the
    /// walk on the first <c>Fast</c> rung, which is <c>adr/0017</c> spelled without a sampler — and
    /// in a city small enough for everything to be Fast that is *the first school in slot order, for
    /// everybody, for ever.* The class remark holds the argument; the short version is that
    /// nearest-first admission needs a distance, and a build that has already paid for one may not
    /// then pretend it does not know.
    /// </para>
    /// <para>
    /// ⚠ <b>Ties go to the lower Building slot</b>, because the scan keeps the incumbent on an equal
    /// cost. That is <c>Gather</c>'s order, which is the table's, which is fixed for a given world —
    /// so two equidistant schools split the city the same way on every Day and after every reload.
    /// </para>
    /// <para>
    /// ⚠ <b>The box is a straight-line bound on a network distance</b>, so it over-supplies
    /// candidates and never under-supplies them — <c>EmploymentEngine.Radius</c>'s argument, and the
    /// reason the second stage is a real route rather than a tightened box.
    /// </para>
    /// <para>
    /// 🔴 <b><paramref name="respectPlaces"/> is what makes this one method and not two.</b> Pass one
    /// asks it <c>false</c> and gets the city's geometry; pass two asks it <c>true</c> and gets what
    /// is left. ⚠ <b>Fullness is tested AFTER the route in both</b>, which is the expensive order and
    /// the only honest one: fullness is <c>O(1)</c> and a route is not, but asking the cheap question
    /// first would file a school behind an Arterial under <em>full</em> and ***the player would build
    /// a second school to fix a road.***
    /// </para>
    /// </remarks>
    private Miss Reach(
        int home, TravelMode mode, Cells radius, TripRuleset trips, int day, bool respectPlaces,
        out int provider, out TravelTime cost)
    {
        provider = Rows.NoSlot;
        cost = TravelTime.Impassable;

        if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[home], out int lot))
        {
            return Miss.Doorstep;
        }

        Cells east = CellGrid.ToCells(_world.Lots.East[lot]);
        Cells north = CellGrid.ToCells(_world.Lots.North[lot]);

        if (!CellGrid.Contains(east, north))
        {
            return Miss.Doorstep;
        }

        Address door = _world.AccessPoint(home, mode);

        // adr/0079's hole: nobody starts a walk from a door that is not on a Segment. Not an
        // unreachable school -- an unreachable doorstep -- so it is none of the three counters.
        if (!door.Exists)
        {
            return Miss.Doorstep;
        }

        CellRect box = CellRect.At(east, north).Dilate(radius).Clamp();
        bool inBox = false;
        bool sawFull = false;

        for (int i = 0; i < _serviceCount; i++)
        {
            int candidate = _services[i];

            if (candidate == home || !Within(box, candidate))
            {
                continue;
            }

            inBox = true;

            TravelTime candidateCost = WalkRouting.Cost(
                _world.Roads, mode, door, _world.AccessPoint(candidate, mode), trips.CrossingCost,
                _walk);

            // The rung is the CEILING and no longer the choice: it refuses a school outside the
            // Commute Budget, and the cheapest of what survives is what is taken. Rung and cost order
            // the same set the same way, so the old best-rung comparison was the coarser spelling of
            // this one.
            if (!trips.TryRung(candidateCost, out _))
            {
                continue;
            }

            if (respectPlaces && !_world.HasServicePlace(candidate, day))
            {
                sawFull = true;
                continue;
            }

            if (provider == Rows.NoSlot || candidateCost < cost)
            {
                provider = candidate;
                cost = candidateCost;
            }
        }

        if (provider != Rows.NoSlot)
        {
            return Miss.None;
        }

        // The three failures are named apart because they are three different cities: a city with no
        // schools, a city whose schools are behind an Arterial, and a city whose schools are full.
        // Each asks the player for a different thing, and a counter that merged any two of them would
        // be telling them to guess which.
        //
        // ⚠ `full` is tested FIRST because it is the most specific claim of the three: this family
        // found a school, could reach it, and was turned away at the door.
        return sawFull ? Miss.Full
            : inBox ? Miss.Unreached
            : Miss.NoService;
    }

    /// <summary>Whether this Building's Lot falls inside the box.</summary>
    private bool Within(CellRect box, int building)
    {
        if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[building], out int lot))
        {
            return false;
        }

        return box.Contains(
            CellGrid.ToCellsClamped(_world.Lots.East[lot]),
            CellGrid.ToCellsClamped(_world.Lots.North[lot]));
    }

    /// <summary>Collects the standing Buildings attended for this Need.</summary>
    /// <remarks>
    /// ⚠ <b>An ABANDONED service Building is not gathered.</b> <c>adr/0091</c> leaves a shell
    /// standing on its Lot, and a shell is the ruin of a school rather than a school — so a city
    /// whose schools have all been condemned reports <em>no school in range</em>, which is the true
    /// sentence. This is <c>PlacementEngine.TryHouse</c>'s shell rule arriving one mechanism along.
    /// </remarks>
    private void Gather(Need need)
    {
        _serviceCount = 0;

        BuildingTable buildings = _world.Buildings;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (!buildings.Rows.IsLive(slot)
                || buildings.IsAbandoned(slot)
                || _world.Rules.ServedBy(buildings.Kind[slot]) != need)
            {
                continue;
            }

            if (_serviceCount == _services.Length)
            {
                Array.Resize(ref _services, _services.Length == 0 ? 8 : _services.Length * 2);
            }

            _services[_serviceCount++] = slot;
        }
    }
}
