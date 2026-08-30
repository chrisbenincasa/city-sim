namespace Borough.Core.Rules;

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
/// Wheel, which is what carries an occasion whose next firing genuinely varies. ***Owed a
/// <c>plans/0013</c> row*** (<c>adr/0073</c>); the corpus freeze is why it has none.
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

    private int _tickAttended;
    private int _tickUnreached;
    private int _tickNoService;

    /// <param name="world">The world whose Households attend.</param>
    /// <param name="trips">The one door a Trip is created through.</param>
    public ServiceEngine(World world, TripEngine trips)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(trips);

        _world = world;
        _trips = trips;
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

        // Ordered by Need id rather than by anything meaningful, and it must stay fixed: both passes
        // create Trips, and swapping them renumbers every Trip id the State Hash folds.
        AttendAll(Need.Education, radius, needs, trips, tick);
        AttendAll(Need.Health, radius, needs, trips, tick);
    }

    /// <summary>One Day's attendance for one Need, across the whole population.</summary>
    private void AttendAll(
        Need need, Cells radius, NeedRuleset needs, TripRuleset trips, Ticks tick)
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

        HouseholdTable households = _world.Households;

        for (int slot = 0; slot < households.Rows.SlotCount; slot++)
        {
            if (!households.Rows.IsLive(slot))
            {
                continue;
            }

            AttendOne(slot, need, depth, radius, needs, trips, tick);
        }
    }

    /// <summary>One Household's occasion: send somebody, or record that nobody could go.</summary>
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
    /// </remarks>
    private void AttendOne(
        int slot, Need need, Column<int> depth, Cells radius, NeedRuleset needs, TripRuleset trips,
        Ticks tick)
    {
        int traveller = Traveller(slot, need);

        if (traveller < 0)
        {
            return;
        }

        TravelMode mode = _world.ModeOf(traveller);

        if (!_world.Buildings.Rows.TryResolve(
                _world.Households.Dwelling[slot], out int home))
        {
            return;
        }

        if (!Nearest(home, mode, radius, trips, out int provider))
        {
            RuleEngine.Write(depth, slot, depth[slot] - needs.DegradeOf(need), needs.Floor);
            return;
        }

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
    /// The first service Building this Household can reach inside the Budget, satisficing.
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
    /// <b>Satisficing survives the missing key</b>: the walk stops on the first <c>Fast</c> rung
    /// rather than ranking the set, so what is taken is the first good-enough school met in slot
    /// order and never the best one in the city. <c>adr/0017</c>, and the same break
    /// <c>TryEmploy</c> makes.
    /// </para>
    /// <para>
    /// ⚠ <b>The box is a straight-line bound on a network distance</b>, so it over-supplies
    /// candidates and never under-supplies them — <c>EmploymentEngine.Radius</c>'s argument, and the
    /// reason the second stage is a real route rather than a tightened box.
    /// </para>
    /// </remarks>
    private bool Nearest(
        int home, TravelMode mode, Cells radius, TripRuleset trips, out int provider)
    {
        provider = Rows.NoSlot;

        if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[home], out int lot))
        {
            return false;
        }

        Cells east = CellGrid.ToCells(_world.Lots.East[lot]);
        Cells north = CellGrid.ToCells(_world.Lots.North[lot]);

        if (!CellGrid.Contains(east, north))
        {
            return false;
        }

        Address door = _world.AccessPoint(home, mode);

        // adr/0079's hole: nobody starts a walk from a door that is not on a Segment. Not an
        // unreachable school -- an unreachable doorstep -- so it is neither of the two counters.
        if (!door.Exists)
        {
            return false;
        }

        CellRect box = CellRect.At(east, north).Dilate(radius).Clamp();
        CommuteRung best = CommuteRung.Fast;
        bool found = false;
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

            if (!trips.TryRung(cost, out CommuteRung rung))
            {
                continue;
            }

            if (!found || rung < best)
            {
                found = true;
                best = rung;
                provider = candidate;
            }

            if (rung == CommuteRung.Fast)
            {
                break;
            }
        }

        if (!found)
        {
            // The two failures are counted apart because they are two different cities: a city with
            // no schools, and a city whose schools are behind an Arterial.
            if (inBox)
            {
                _tickUnreached++;
            }
            else
            {
                _tickNoService++;
            }
        }

        return found;
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
