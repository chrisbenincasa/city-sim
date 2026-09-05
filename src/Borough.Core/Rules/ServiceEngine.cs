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
/// 🔴 <b>A SCHOOL HAS PLACES, SO SOMEBODY IS TURNED AWAY — AND WHO, IS A DESIGN DECISION THIS
/// MECHANISM MAKES EVERY DAY.</b> It is a <b>matching</b>: <see cref="Collect">collect</see> every
/// family's reachable schools in its own order of preference, <see cref="Match">match</see> families
/// to places by deferred acceptance, and <see cref="Apply">apply</see> the result. What that buys is
/// a property rather than a number — the matching is <b>stable</b>, so ***there is no family and
/// school that would both rather have each other than what they got.***
/// </para>
/// <para>
/// 🔴 <b>TWO WRONG ANSWERS CAME BEFORE IT AND BOTH ARE WORTH KEEPING.</b> The first walked the
/// population in <b>slot order</b>, which is the order Households were created in, so ***the oldest
/// families in the city took the school every Day and the newest never did*** — a queue nobody
/// joined. The second keyed each family by the distance to <em>its own</em> nearest school and served
/// the queue nearest-first, which sounds right and is not: a family whose nearest school is full is
/// admitted at a further one, ***so it was ranked by the journey it did not make.***
/// </para>
/// <para>
/// ⚠ <b>THE SECOND ONE WAS MEASURED BEFORE IT WAS ARGUED ABOUT, AND THAT IS THE REASON THIS EXISTS.</b>
/// <c>ServiceAdmission</c> counts the blocking pairs — a family turned away that is nearer to a school
/// than somebody that school admitted — and on <c>schooled.toml</c> at 2,000 Citizens it read
/// <b>zero at one school, 55 of 55 at two, 4 of 6 at four and zero at eight</b>. Zero at both ends and
/// total in the middle: ***a mechanism with no scarcity in it cannot be unfair***, and the middle is
/// where a player builds. <c>plans/0054</c> <b>F6a</b> holds the readings.
/// </para>
/// <para>
/// <b>Deferred acceptance costs no routes over what the nearest-first ordering already paid.</b>
/// The walk in <see cref="Collect"/> visited every candidate in the box in order to find the nearest,
/// so keeping the whole list costs storage and a sort per family and never a second search; the
/// proposals themselves are heap operations. ***What the expensive-sounding algorithm actually added
/// was memory.***
/// </para>
/// <para>
/// 🔴 <b>AND IT ENDED THE SATISFICING BREAK, WHICH IS A DESIGN CHANGE AND NOT A TIDY-UP.</b> The walk
/// used to stop at the first <c>Fast</c>-rung candidate in slot order and never rank the set. ⚠ <b>What
/// makes that affordable to lose is <see cref="Candidates"/>'s own argument for having no
/// <c>candidates</c> key</b>: service Buildings are placed by hand, one verb at a time, so the set
/// being ranked is bounded by the player and not by the city. <c>adr/0017</c>'s first consequence is
/// work bounded by a Ruleset constant rather than by the size of the city, and that still holds here.
/// </para>
/// <para>
/// 🔴 <b>AN ADR IS OWED FOR THE MATCHING AND CANNOT BE WRITTEN.</b> <c>adr/0017</c> governs how an
/// actor comes to know its options and when it switches between them; ***how a full provider rations a
/// scarce place is a question it does not reach***, and no other record does either. <c>plans/0045</c>
/// standing order 1 freezes new ADRs, so the debt is filed at <c>plans/0054</c> <b>F6d</b> rather than
/// paid. ⚠ <b>Read that before extending this to a second rationing site</b> — a clinic, a job, a
/// dwelling — because the argument for doing it this way has never been written down.
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

    // ---- the occasions, and the candidate lists belonging to them ---------------------------------
    //
    // Parallel arrays rather than an array of structs. SCRATCH AND NEVER STATE, on _services'
    // argument: rebuilt from the world every pass, so nothing here is saved, hashed, or a column
    // somebody has to remember to rebuild on load.

    /// <summary>The Household whose occasion this is.</summary>
    private int[] _slots = [];

    /// <summary>Who makes the journey — <see cref="Traveller"/>'s answer, kept rather than re-asked.</summary>
    private int[] _travellers = [];

    /// <summary>The Building the journey starts at.</summary>
    private int[] _homes = [];

    /// <summary>Where the family ended up, or <see cref="Rows.NoSlot"/> if every door was full.</summary>
    private int[] _settled = [];

    /// <summary>Where this occasion's slice of <see cref="_candidates"/> begins.</summary>
    private int[] _candStart = [];

    /// <summary>How long that slice is.</summary>
    private int[] _candCount = [];

    /// <summary>How far down its own list this family has proposed.</summary>
    private int[] _cursor = [];

    /// <summary>Occasions waiting to propose. A stack, because the order it is drained in cannot
    /// change the matching — that is the theorem, and it is why nothing here is sorted.</summary>
    private int[] _pending = [];

    /// <summary>
    /// Every reachable school of every occasion, packed <c>(cost, service index)</c> and sorted
    /// within each slice.
    /// </summary>
    /// <remarks>
    /// <b>One flat array with per-occasion slices, rather than a list per Household.</b> A
    /// per-entity collection object is what <c>CLAUDE.md</c> forbids in the core, and this is the
    /// same reason arriving in scratch: ***the total is what is bounded, not the per-family count.***
    /// </remarks>
    private long[] _candidates = [];

    /// <summary>How many occasions named each gathered service. Sizes its held set.</summary>
    private int[] _demand = [];

    /// <summary>Where each gathered service's held set begins in <see cref="_held"/>.</summary>
    private int[] _heldStart = [];

    /// <summary>How many families it is holding.</summary>
    private int[] _heldCount = [];

    /// <summary>How many it may hold — see <see cref="Seats"/>, which is not the declared places.</summary>
    private int[] _heldCapacity = [];

    /// <summary>
    /// The held families, as one max-heap per school over its slice, packed
    /// <c>(cost, occasion)</c>.
    /// </summary>
    private long[] _held = [];

    private int _occasionCount;
    private int _candidateCount;
    private int _pendingCount;

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
    /// <b>Collect, match, apply.</b> The middle step needs every answer before it can decide
    /// anything — ***a matching is not a property any one Household has*** — and the last step must
    /// come after it, because a place taken early would change an answer the first step had already
    /// given.
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

        Collect(need, depth, radius, needs, trips, day);
        Match(day);

        for (int entry = 0; entry < _occasionCount; entry++)
        {
            Apply(entry, need, depth, needs, tick, day);
        }
    }

    /// <summary>
    /// Step one: every Household that has an occasion, and every school it could reach, in its own
    /// order of preference.
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
    /// 🔴 <b>THE THREE FAILURES THAT DO NOT DEPEND ON A PLACE ARE SETTLED HERE.</b> No school in the
    /// box, no route inside the Budget and no doorstep to leave from are all true before anybody has
    /// been admitted to anything, so ***a family that was never going to attend must not enter the
    /// matching at all.*** Only <see cref="Miss.Full"/> survives into <see cref="Match"/>, because it
    /// is the only one another family can cause.
    /// </para>
    /// <para>
    /// ⚠ <b>Fullness is NOT consulted here, and that is what makes the preference list a preference
    /// list.</b> A family's ranking of schools is a property of where it lives; a school being full
    /// is a property of who else applied. ***Mixing them would make the list depend on the answer it
    /// is an input to.***
    /// </para>
    /// </remarks>
    private void Collect(
        Need need, Column<int> depth, Cells radius, NeedRuleset needs, TripRuleset trips, int day)
    {
        _occasionCount = 0;
        _candidateCount = 0;

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

            if (!households.Rows.IsLive(slot)
                || !_world.Buildings.Rows.TryResolve(households.Dwelling[slot], out int home))
            {
                continue;
            }

            int start = _candidateCount;
            Miss miss = Candidates(home, _world.ModeOf(traveller), radius, trips);

            if (miss != Miss.None)
            {
                _candidateCount = start;
                Fail(miss, need, depth, slot, needs);
                continue;
            }

            Enqueue(slot, traveller, home, start, _candidateCount - start);
        }

        _ = day;
    }

    /// <summary>
    /// Every service Building this Household can reach inside the Budget, cheapest first.
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
    /// <b>The whole list rather than the cheapest one, because a rejected family needs somewhere to
    /// go next.</b> The routes are the same routes either way — the walk already visited every
    /// candidate in the box in order to find the nearest — so what a list costs over a minimum is
    /// ***the storage and the sort, and never a second search.***
    /// </para>
    /// <para>
    /// ⚠ <b>Packed as <c>(cost, service index)</c> in one <c>long</c>, and the packing is the sort.</b>
    /// <see cref="Gather"/> walks Building slots ascending, so a service index ascends with its
    /// Building slot and the low half breaks a tie by the older school. One <c>Span.Sort</c> over the
    /// slice therefore orders the family's preferences with no payload array and no comparer.
    /// </para>
    /// <para>
    /// ⚠ <b>The box is a straight-line bound on a network distance</b>, so it over-supplies
    /// candidates and never under-supplies them — <c>EmploymentEngine.Radius</c>'s argument, and the
    /// reason the second stage is a real route rather than a tightened box.
    /// </para>
    /// </remarks>
    private Miss Candidates(int home, TravelMode mode, Cells radius, TripRuleset trips)
    {
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
        int start = _candidateCount;
        bool inBox = false;

        for (int i = 0; i < _serviceCount; i++)
        {
            int candidate = _services[i];

            if (candidate == home || !Within(box, candidate))
            {
                continue;
            }

            inBox = true;

            TravelTime cost = WalkRouting.Cost(
                _world.Roads, mode, door, _world.AccessPoint(candidate, mode), trips.CrossingCost,
                _walk);

            // The rung is the CEILING and not the choice: it refuses a school outside the Commute
            // Budget, and what survives is ranked by cost. Rung and cost order the same set the same
            // way, so a best-rung comparison was only ever the coarser spelling of this one.
            if (!trips.TryRung(cost, out _))
            {
                continue;
            }

            GrowCandidates(_candidateCount + 1);

            // A constant shift count -- BOR0204 reports a computed one, and this is checkable by eye.
            // A route's cost is non-negative and a service index is a position in a list, so neither
            // half can run into the other.
            _candidates[_candidateCount++] = ((long)cost.Raw << 32) | (uint)i;
        }

        if (_candidateCount == start)
        {
            return inBox ? Miss.Unreached : Miss.NoService;
        }

        _candidates.AsSpan(start, _candidateCount - start).Sort();

        return Miss.None;
    }

    /// <summary>
    /// Step two: the matching. <b>Families propose, schools hold the nearest and reject the rest.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>DEFERRED ACCEPTANCE, AND WHAT IT BUYS IS A PROPERTY RATHER THAN A NUMBER.</b> The
    /// matching it produces is <b>stable</b>: there is no family and school that would both rather
    /// have each other than what they got. The ordering it replaced — serve the occasions
    /// nearest-first — was not, and the gap was measured before this was built rather than argued
    /// about (<c>plans/0054</c> <b>F6a</b>): at two scarce schools ***every one of the fifty-five
    /// families turned away was nearer to a school than somebody that school admitted***, by up to
    /// 12.3 minutes of walk against a 20-minute Fast rung.
    /// </para>
    /// <para>
    /// 🔴 <b>WHY THAT ORDERING FAILED, WHICH IS THE THING WORTH REMEMBERING.</b> It keyed a family by
    /// the distance to its <em>nearest</em> school and then admitted it wherever a place was left —
    /// ***so a family was ranked by the journey it did not make.*** The two agree only while first
    /// choices are free, which is exactly the case where the ordering does not matter.
    /// </para>
    /// <para>
    /// <b>It terminates, and the argument is Gale-Shapley's.</b> A family only ever moves forward
    /// through its own preference list — a school that rejected it is behind its cursor for ever —
    /// so the loop is bounded by the total number of candidates, which is fixed before it starts.
    /// A displaced family resumes from where it stood rather than beginning again.
    /// </para>
    /// <para>
    /// ⚠ <b>It is skipped entirely in a world with no <c>[capacity] floor_tiles_per_place</c></b>,
    /// where nothing is ever full and every family takes its first choice. ***A matching with no
    /// scarcity in it is the identity***, and every Ruleset shipped before that key is that world.
    /// </para>
    /// <para>
    /// 🔴 <b>AN ADR IS OWED AND CANNOT BE WRITTEN.</b> <c>adr/0017</c> governs how an actor comes to
    /// know its options and when it switches; ***how a full provider rations a scarce place is a
    /// question it does not reach***, and no other record does either. What this does touch is that
    /// ADR's first consequence — work per decision bounded by a Ruleset constant — because a family
    /// may propose more than once. ⚠ <b>The bound is the PLAYER'S school count and not the city's</b>,
    /// which is the same argument that lets this pass have no <c>candidates</c> key at all, so the
    /// scaling objection does not land; the record is owed anyway. <c>plans/0045</c> standing order
    /// 1 freezes it, and <c>plans/0054</c> <b>F6d</b> holds the debt.
    /// </para>
    /// </remarks>
    private void Match(int day)
    {
        for (int entry = 0; entry < _occasionCount; entry++)
        {
            _settled[entry] = Rows.NoSlot;
        }

        if (_occasionCount == 0 || _world.Rules.Capacity.FloorTilesPerPlace <= 0)
        {
            Unbounded();
            return;
        }

        Seats(day);

        // Every family starts pending at the head of its own list. Descending so that the stack pops
        // ascending, which costs nothing and makes a trace readable in Household order.
        _pendingCount = 0;

        for (int entry = _occasionCount - 1; entry >= 0; entry--)
        {
            _cursor[entry] = 0;
            Push(entry);
        }

        while (_pendingCount > 0)
        {
            Propose(_pending[--_pendingCount]);
        }

        Settle();
    }

    /// <summary>Every family takes its first choice, which is the matching when nothing is full.</summary>
    private void Unbounded()
    {
        for (int entry = 0; entry < _occasionCount; entry++)
        {
            if (_candCount[entry] > 0)
            {
                _settled[entry] = _services[(int)_candidates[_candStart[entry]]];
            }
        }
    }

    /// <summary>
    /// One family walking down its list until a school holds it, or until the list runs out.
    /// </summary>
    /// <remarks>
    /// <b>A rejection advances the cursor and never resets it</b>, which is what bounds the loop.
    /// The three outcomes are held, displaced-somebody-and-held, and rejected — and only the second
    /// puts another family back on the stack.
    /// </remarks>
    private void Propose(int entry)
    {
        while (_cursor[entry] < _candCount[entry])
        {
            long candidate = _candidates[_candStart[entry] + _cursor[entry]];
            _cursor[entry]++;

            int service = (int)candidate;
            int start = _heldStart[service];

            // The school's own ranking, and it is a DIFFERENT key from the family's: the cost is the
            // same walk, but the low half is the occasion rather than the service. So a school breaks
            // a tie by the older Household and a family breaks one by the older school, and each is a
            // total order over the set it ranks.
            long seat = (candidate & unchecked((long)0xFFFF_FFFF_0000_0000)) | (uint)entry;

            if (_heldCount[service] < _heldCapacity[service])
            {
                Hold(start, service, seat);
                return;
            }

            if (_heldCapacity[service] == 0 || seat >= _held[start])
            {
                continue;
            }

            // The furthest family the school is holding, which is the heap's root. It goes back on
            // the stack with its cursor already past this school, so it can never propose here again.
            int displaced = (int)_held[start];

            _held[start] = seat;
            SiftDown(start, _heldCount[service], 0);

            Push(displaced);

            return;
        }
    }

    /// <summary>Sizes each school's held set, and the sizing is not the school's capacity.</summary>
    /// <remarks>
    /// <b>The smaller of what the school has left today and how many families named it.</b> A school
    /// standing on a very large floor can declare more places than the city has children, and
    /// allocating for the declaration would size a buffer by the ground rather than by the
    /// population. ⚠ <b>Places LEFT and not places declared</b> — <c>BuildingTable.AttendedToday</c>
    /// is a per-Day meter that something else may already have moved, so the pass asks what is
    /// remaining rather than assuming it runs first.
    /// </remarks>
    private void Seats(int day)
    {
        GrowServices(_serviceCount);

        for (int i = 0; i < _serviceCount; i++)
        {
            _demand[i] = 0;
        }

        for (int i = 0; i < _candidateCount; i++)
        {
            _demand[(int)_candidates[i]]++;
        }

        int seats = 0;

        for (int i = 0; i < _serviceCount; i++)
        {
            int building = _services[i];
            int places = _world.DeclaredPlaces(building);

            if (_world.Buildings.AttendedDay[building] == day)
            {
                places -= _world.Buildings.AttendedToday[building];
            }

            if (places < 0)
            {
                places = 0;
            }

            _heldStart[i] = seats;
            _heldCount[i] = 0;
            _heldCapacity[i] = places < _demand[i] ? places : _demand[i];

            seats += _heldCapacity[i];
        }

        GrowHeld(seats);
    }

    /// <summary>Reads the matching out of the held sets and into <see cref="LastProviders"/>.</summary>
    private void Settle()
    {
        for (int i = 0; i < _serviceCount; i++)
        {
            int start = _heldStart[i];

            for (int j = 0; j < _heldCount[i]; j++)
            {
                _settled[(int)_held[start + j]] = _services[i];
            }
        }
    }

    /// <summary>
    /// Step three: one matched occasion — send somebody, or record that every door was full.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>APPLIED IN HOUSEHOLD SLOT ORDER, AND THAT ORDER NO LONGER MEANS ANYTHING.</b> It used to
    /// be the admission order and therefore the whole of who got a place; the matching decided that
    /// before this method runs, so what is left is a fixed order for creating Trips in.
    /// ***An arbitrary order is only a defect while something depends on it.***
    /// </para>
    /// <para>
    /// <b>An unmatched family here is <see cref="Miss.Full"/> and can be nothing else</b>, for
    /// <see cref="Collect"/>'s reason: it reached the matching, so it had somewhere it could go, and
    /// the only thing that could have kept it out is somebody nearer.
    /// </para>
    /// </remarks>
    private void Apply(int entry, Need need, Column<int> depth, NeedRuleset needs, Ticks tick, int day)
    {
        int slot = _slots[entry];
        int provider = _settled[entry];

        if (provider == Rows.NoSlot)
        {
            Fail(Miss.Full, need, depth, slot, needs);
            return;
        }

        int traveller = _travellers[entry];
        TravelMode mode = _world.ModeOf(traveller);

        _world.TakeServicePlace(provider, day);

        // ⚠ ACTIVITY IS DELIBERATELY NOT WRITTEN, and the enum deliberately does not grow. A school
        // run's RETURN journey is unbuilt -- exactly where the commute stood at 5b-bis -- so an
        // `AtSchool` value would strand every child in the city on the first Day, with nothing to
        // send them home. CitizenActivity's own remark says the set grows when a generator does; the
        // generator that grows it is the return, not this one. Meanwhile `AtHome` is where a child
        // who went to school and came back actually is at the end of the Day.
        _trips.Start(traveller, _homes[entry], provider, mode, TripPurpose.School, tick);

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

    // ---- the heap, one max-heap per school ---------------------------------------------------------
    //
    // ⚠ A HEAP AND NOT A SCAN, and the difference is not micro-optimisation. A school rejects by
    // evicting its FURTHEST held family, so the only question it ever asks its held set is "who is
    // worst" -- and a linear scan for that would be paid once per rejection, which is the quantity
    // this whole mechanism is made of. The root of a max-heap answers it in one read.
    //
    // Keyed on (cost, occasion) packed into a long, so the max is the furthest family and a tie goes
    // to the later Household. That is the school's ranking spelled once, in the ordering of a
    // primitive, rather than in a comparer somebody has to keep consistent with the family's.

    private void Hold(int start, int service, long seat)
    {
        int at = _heldCount[service]++;
        _held[start + at] = seat;

        while (at > 0)
        {
            // A constant shift, and `at` is positive here, so this is the parent index exactly.
            int parent = (at - 1) >> 1;

            if (_held[start + parent] >= _held[start + at])
            {
                break;
            }

            (_held[start + parent], _held[start + at]) = (_held[start + at], _held[start + parent]);
            at = parent;
        }
    }

    private void SiftDown(int start, int count, int at)
    {
        while (true)
        {
            int left = (at * 2) + 1;

            if (left >= count)
            {
                return;
            }

            int larger = left;
            int right = left + 1;

            if (right < count && _held[start + right] > _held[start + left])
            {
                larger = right;
            }

            if (_held[start + at] >= _held[start + larger])
            {
                return;
            }

            (_held[start + larger], _held[start + at]) = (_held[start + at], _held[start + larger]);
            at = larger;
        }
    }

    // ---- the buffers -------------------------------------------------------------------------------

    private void Push(int entry)
    {
        GrowOccasions(_pendingCount + 1);
        _pending[_pendingCount++] = entry;
    }

    /// <summary>Records one occasion and the slice of the candidate list belonging to it.</summary>
    private void Enqueue(int slot, int traveller, int home, int start, int count)
    {
        GrowOccasions(_occasionCount + 1);

        _slots[_occasionCount] = slot;
        _travellers[_occasionCount] = traveller;
        _homes[_occasionCount] = home;
        _candStart[_occasionCount] = start;
        _candCount[_occasionCount] = count;
        _settled[_occasionCount] = Rows.NoSlot;

        _occasionCount++;
    }

    /// <summary>Sizes the per-occasion buffers together, so an index means the same thing in each.</summary>
    /// <remarks>
    /// <b><see cref="_pending"/> is sized here too and it is not an occasion buffer</b> — it is a
    /// stack of occasion indices, and a family can be on it once, so its ceiling is the occasion
    /// count and it can share the growth.
    /// </remarks>
    private void GrowOccasions(int needed)
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
        Array.Resize(ref _settled, size);
        Array.Resize(ref _candStart, size);
        Array.Resize(ref _candCount, size);
        Array.Resize(ref _cursor, size);
        Array.Resize(ref _pending, size);
    }

    private void GrowCandidates(int needed)
    {
        if (_candidates.Length >= needed)
        {
            return;
        }

        int size = _candidates.Length == 0 ? 256 : _candidates.Length;

        while (size < needed)
        {
            size *= 2;
        }

        Array.Resize(ref _candidates, size);
    }

    private void GrowServices(int needed)
    {
        if (_demand.Length >= needed)
        {
            return;
        }

        int size = _demand.Length == 0 ? 8 : _demand.Length;

        while (size < needed)
        {
            size *= 2;
        }

        Array.Resize(ref _demand, size);
        Array.Resize(ref _heldStart, size);
        Array.Resize(ref _heldCount, size);
        Array.Resize(ref _heldCapacity, size);
    }

    private void GrowHeld(int needed)
    {
        if (_held.Length >= needed)
        {
            return;
        }

        int size = _held.Length == 0 ? 64 : _held.Length;

        while (size < needed)
        {
            size *= 2;
        }

        Array.Resize(ref _held, size);
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
            if ((CitizenActivity)_world.Citizens.Activity[member] == CitizenActivity.AtHome
                && (!wantsChild || _world.Citizens.Age[member] == 0))
            {
                return member;
            }
        }

        return -1;
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
