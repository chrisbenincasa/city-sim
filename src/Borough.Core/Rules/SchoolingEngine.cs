using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>
/// What a Household is doing, as distinct from what it is made of.
/// </summary>
/// <remarks>
/// <b><c>CONTEXT.md</c> → <i>Life Stage</i> keeps these apart by name</b>: <em>"Studying and
/// Unemployment are states, not stages — an In Education Household is 1–2 adults with no children,
/// which is a Young Household exactly, so it fails the definition of a stage."</em> ***This is what
/// keeps the stage table from accumulating rows that are really occupations.***
/// </remarks>
public enum HouseholdState : byte
{
    /// <summary>Ordinary: works, or looks for work.</summary>
    None = 0,

    /// <summary>
    /// Formed with a childhood good enough for a university and not yet asked whether it will go.
    /// </summary>
    Considering = 1,

    /// <summary>
    /// Occupies a dwelling, consumes, and <b>supplies no labour</b> — the design's first Household
    /// that is a net fiscal cost by design.
    /// </summary>
    InEducation = 2,
}

/// <summary>
/// The Day sweep that runs the university: who enrols, who pays, who graduates and who drops out.
/// </summary>
/// <remarks>
/// <para>
/// <b>The only route to Skill Tier 3.</b> <c>CONTEXT.md</c> → <i>Schooling</i> says so, and
/// <c>adr/0104</c> keeps the credential a wall for the reason that makes it worth having: if time
/// alone reached the top tier a patient player would never build a school, Office would staff itself,
/// and schooling would degrade from a structural requirement to an accelerator.
/// </para>
/// <para>
/// 🔴 <b>PUBLIC AND PRIVATE DIFFER ONLY IN WHICH SCARCITY RATIONS THEM.</b> A public university is
/// free and rationed by <em>places</em> — floor area over <c>[capacity] floor_tiles_per_place</c>,
/// the ceiling schools already have. A private one charges and is rationed by <em>money</em>.
/// ***The degree is identical out of both***: a private degree that was better would let money buy a
/// category, and <c>CONTEXT.md</c> → <i>Skill Tier</i> says the 2 → 3 cut earns its place precisely
/// <em>"because a category is not a quantity"</em>.
/// </para>
/// <para>
/// ⚠ <b>A public university costs nothing to run here, and that absence is stated rather than built.</b>
/// Funding a service out of the treasury belongs to <c>plans/0045</c> row 32 by name, and under
/// <c>adr/0070</c> nothing in this mechanism may reason from a public university being free.
/// </para>
/// <para>
/// ⚠ <b>Enrolment is decided ONCE, on the first Day a formed Household has a dwelling.</b> A Household
/// forms unhoused — <c>World.FormHousehold</c> joins the Unplaced Pool — so <em>is a university in
/// reach?</em> is unanswerable at formation and cannot live there. And a Household that is asked
/// repeatedly would be a Household that quits its job the year a university opens, which is a
/// different mechanism from a school-leaver's decision. ***So a university takes a generation to pay
/// off***, which is the lag the design says education should have.
/// </para>
/// <para>
/// ⚠ <b>Both passes walk every live Household once a Day.</b> An intrusive student list was
/// considered and refused: <c>ServiceEngine.Collect</c> already walks the same table on the same Day
/// boundary, so the walk is paid for, and a derived index would owe
/// <c>DerivedRebuildAuditTests</c> a fixture in exchange for a saving nothing has measured a need
/// for. ***A structure that lives outside the world is not derived state, however it is declared.***
/// </para>
/// </remarks>
internal sealed class SchoolingEngine
{
    private readonly World _world;
    private readonly WalkScratch _walk = new();

    private int[] _universities = [];
    private int[] _enrolled = [];
    private int _universityCount;

    private int _tickEnrolledPublic;
    private int _tickEnrolledPrivate;
    private int _tickGraduated;
    private int _tickDroppedOut;
    private int _tickTurnedAway;
    private int _tickStudying;
    private long _tickTuition;

    public SchoolingEngine(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
    }

    /// <summary>One Day of the university, or nothing at all where no Ruleset declares one.</summary>
    public SchoolingReading Sweep(Ticks tick)
    {
        _tickEnrolledPublic = 0;
        _tickEnrolledPrivate = 0;
        _tickGraduated = 0;
        _tickDroppedOut = 0;
        _tickTurnedAway = 0;
        _tickStudying = 0;
        _tickTuition = 0;

        if (!_world.Rules.Schooling.Enrols || tick.Raw % (ulong)Ticks.PerDay != 0UL)
        {
            return default;
        }

        int day = (int)IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);

        Gather();
        Continue(day, tick);
        Enrol(day, tick);

        return new SchoolingReading(
            _tickEnrolledPublic, _tickEnrolledPrivate, _tickGraduated, _tickDroppedOut,
            _tickTurnedAway, _tickStudying, _tickTuition);
    }

    /// <summary>The universities standing today, and how many students each already holds.</summary>
    /// <remarks>
    /// <b>Occupancy is counted rather than stored</b>, for the reason the class remark gives: the
    /// walk is already paid for, and a stored count is a derived structure owing a rebuild.
    /// ⚠ <b>A place is held for the whole course and not for a Day</b>, which is why
    /// <c>World.TakeServicePlace</c> — a daily debit — is the wrong instrument here.
    /// </remarks>
    private void Gather()
    {
        _universityCount = 0;

        BuildingTable buildings = _world.Buildings;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (!buildings.Rows.IsLive(slot) || buildings.IsAbandoned(slot)
                || !_world.Rules.Kind(buildings.Kind[slot]).IsUniversity)
            {
                continue;
            }

            if (_universityCount == _universities.Length)
            {
                int size = _universities.Length == 0 ? 4 : _universities.Length * 2;

                Array.Resize(ref _universities, size);
                Array.Resize(ref _enrolled, size);
            }

            _enrolled[_universityCount] = 0;
            _universities[_universityCount++] = slot;
        }
    }

    /// <summary>
    /// Pass one: charge today's tuition, count who is where, and end the courses that end today.
    /// </summary>
    /// <remarks>
    /// <b>Charged and counted before anybody new is admitted</b>, so a student who drops out this
    /// morning frees the place somebody takes this afternoon. The order is the whole reason this is
    /// two passes rather than one.
    /// </remarks>
    private void Continue(int day, Ticks tick)
    {
        HouseholdTable households = _world.Households;

        for (int slot = 0; slot < households.Rows.SlotCount; slot++)
        {
            if (!households.Rows.IsLive(slot)
                || (HouseholdState)households.State[slot] != HouseholdState.InEducation)
            {
                continue;
            }

            // The course itself stopped existing. Demolishing a university is not a refund and not a
            // degree; the students go back to looking for work with the tier they arrived with.
            if (!_world.Buildings.Rows.TryResolve(households.University[slot], out int university)
                || _world.Buildings.IsAbandoned(university))
            {
                Leave(slot, graduated: false);
                _tickDroppedOut++;

                continue;
            }

            if (!Charge(slot, university, tick))
            {
                Leave(slot, graduated: false);
                _tickDroppedOut++;

                continue;
            }

            if (day >= households.StateEndsDay[slot])
            {
                Leave(slot, graduated: true);
                _tickGraduated++;

                continue;
            }

            Occupy(university);
            _tickStudying++;
        }
    }

    /// <summary>Pass two: every Household that has not yet decided, and has somewhere to decide from.</summary>
    private void Enrol(int day, Ticks tick)
    {
        HouseholdTable households = _world.Households;
        SchoolingRuleset schooling = _world.Rules.Schooling;

        for (int slot = 0; slot < households.Rows.SlotCount; slot++)
        {
            if (!households.Rows.IsLive(slot)
                || (HouseholdState)households.State[slot] != HouseholdState.Considering
                || !_world.Buildings.Rows.TryResolve(households.Dwelling[slot], out int home))
            {
                continue;
            }

            // 🔴 PUBLIC FIRST AND THEN PRIVATE, because free beats paid and satisficing beats
            // optimising (adr/0017). It is also what makes underbuilding public capacity legible: the
            // private system is visibly the overflow, and a city that never fills its public places
            // never sends anybody to a private one.
            int chosen = Nearest(slot, home, paying: false, tick);
            bool paying = false;

            if (chosen == Rows.NoSlot)
            {
                chosen = Nearest(slot, home, paying: true, tick);
                paying = chosen != Rows.NoSlot;
            }

            // ⚠ DECIDED ONCE. Whatever the answer, this Household has now asked -- see the class
            // remark. A school-leaver who found nothing goes to work, and the next university this
            // city builds is for the next generation.
            households.State[slot] = (byte)HouseholdState.None;

            if (chosen == Rows.NoSlot)
            {
                _tickTurnedAway++;

                continue;
            }

            households.State[slot] = (byte)HouseholdState.InEducation;
            households.StateEndsDay[slot] = day + schooling.UniversityDays;
            households.University[slot] = _world.Buildings.Rows.At(chosen);

            Occupy(chosen);

            // A student supplies no labour, and this is where that becomes true rather than where it
            // is described. Employment needs a dwelling and so does enrolment, so a newly-housed
            // adult can already hold a job by the time this pass reaches them.
            foreach (int member in _world.Members.Walk(slot))
            {
                _world.Dismiss(_world.Citizens.Rows.At(member));
                _world.Citizens.Employment[member] = (byte)EmploymentState.Studying;
            }

            if (paying)
            {
                _tickEnrolledPrivate++;
            }
            else
            {
                _tickEnrolledPublic++;
            }
        }
    }

    /// <summary>
    /// The cheapest university of the wanted kind that this Household can reach, has room for, and
    /// where it charges, can afford outright.
    /// </summary>
    /// <remarks>
    /// <b>Every standing university is looked at and there is no <c>candidates</c> key</b>, which is
    /// <c>ServiceEngine.Candidates</c>' argument arriving unchanged: service Buildings are placed by
    /// hand, one verb at a time, so their count is bounded by the player rather than by the city, and
    /// a sample of three over a set of two is a Household failing to notice the only university in
    /// town.
    /// </remarks>
    private int Nearest(int slot, int home, bool paying, Ticks tick)
    {
        TripRuleset trips = _world.Rules.Trips;

        if (!trips.HasCommuteBudget)
        {
            return Rows.NoSlot;
        }

        int student = FirstAdult(slot);

        if (student < 0)
        {
            return Rows.NoSlot;
        }

        TravelMode mode = _world.ModeOf(student);
        Address door = _world.AccessPoint(home, mode);

        if (!door.Exists)
        {
            return Rows.NoSlot;
        }

        int best = Rows.NoSlot;
        TravelTime bestCost = default;

        for (int i = 0; i < _universityCount; i++)
        {
            int university = _universities[i];

            if (Till(university, out _, out int tuition) != paying || !HasRoom(university, i))
            {
                continue;
            }

            if (paying && !CanAfford(slot, tuition))
            {
                continue;
            }

            TravelTime cost = WalkRouting.Cost(
                _world.Roads, mode, door, _world.AccessPoint(university, mode), trips.CrossingCost,
                _walk);

            if (!trips.TryRung(cost, out _))
            {
                continue;
            }

            if (best == Rows.NoSlot || cost.Raw < bestCost.Raw)
            {
                best = university;
                bestCost = cost;
            }
        }

        _ = tick;

        return best;
    }

    /// <summary>Whether this university has a place left today.</summary>
    /// <remarks>
    /// <b>Absent <c>[capacity] floor_tiles_per_place</c> means NO CEILING</b>, which is
    /// <c>World.HasServicePlace</c>'s rule and is kept identical here so that one Ruleset key governs
    /// both ends of the same idea.
    /// ⚠ <b>The key is asked and the count is not, because zero is two different cities</b> —
    /// <c>World.DeclaredPlaces</c>'s own warning, and since understaffing scales that count a
    /// university whose trade folded reports zero places. Reading the count for the absence would
    /// make an unstaffed university <em>unbounded</em>, which is the opposite of what it is.
    /// </remarks>
    private bool HasRoom(int university, int index)
    {
        if (_world.Rules.Capacity.FloorTilesPerPlace <= 0)
        {
            return true;
        }

        return _enrolled[index] < _world.DeclaredPlaces(university);
    }

    private void Occupy(int university)
    {
        for (int i = 0; i < _universityCount; i++)
        {
            if (_universities[i] == university)
            {
                _enrolled[i]++;

                return;
            }
        }
    }

    /// <summary>
    /// The Business tenanting this university and what it charges a Day, or a public university.
    /// </summary>
    private bool Till(int university, out int business, out int tuition)
    {
        foreach (int tenant in _world.BuildingBusinesses.Walk(university))
        {
            BusinessKindDefinition trade = _world.Rules.BusinessKind(_world.Businesses.Kind[tenant]);

            if (trade.Charges)
            {
                business = tenant;
                tuition = trade.TuitionPerDay;

                return true;
            }
        }

        business = Rows.NoSlot;
        tuition = 0;

        return false;
    }

    /// <summary>
    /// Whether this Household can carry the whole course.
    /// </summary>
    /// <remarks>
    /// <b>The whole course and not a fraction of it, so no second number is authored.</b> A drop-out
    /// then means the money went somewhere else — a Household that fell behind on food and rent while
    /// a member studied — which is a failure with a cause the city can name. ***A reserve fraction
    /// would make drop-out a knob rather than a consequence.***
    /// </remarks>
    private bool CanAfford(int slot, int tuition)
    {
        if (!_world.Bins.Rows.TryResolve(_world.Households.Balance[slot], out int purse))
        {
            return false;
        }

        return _world.Bins.LevelAt(purse) >= (long)tuition * _world.Rules.Schooling.UniversityDays;
    }

    /// <summary>Takes one Day's tuition, and says whether it was there to take.</summary>
    /// <remarks>
    /// <b>Through <c>World.Withdraw</c> and <c>World.Deposit</c> rather than <c>BinTable.Move</c></b>,
    /// so both writes drain their wait lists — <c>WageEngine.Pay</c>'s reason, and the same one holds:
    /// nothing subscribes to a balance today, and going round the doors would make that permanent.
    /// </remarks>
    private bool Charge(int slot, int university, Ticks tick)
    {
        if (!Till(university, out int business, out int tuition) || tuition <= 0)
        {
            return true;
        }

        if (!_world.Bins.Rows.TryResolve(_world.Households.Balance[slot], out int purse)
            || !_world.Bins.Rows.TryResolve(_world.Businesses.Balance[business], out int till))
        {
            return true;
        }

        if (_world.Bins.LevelAt(purse) < tuition)
        {
            return false;
        }

        _world.Withdraw(_world.Bins.Rows.At(purse), tuition, tick);
        _world.Deposit(_world.Bins.Rows.At(till), tuition, tick);

        // plans/0072 D15: a Day of teaching is DELIVERED here, so it is revenue here. A service and
        // not a Good -- no Bin falls, so there is no cost of goods to set against it, which is the
        // one shipped sale with a single leg. Whatever the university spends on teaching it reaches
        // the ledger as wages, on the Day those were earned.
        BusinessAccounts.Serve(_world, business, tuition, tick);

        _tickTuition += tuition;

        return true;
    }

    /// <summary>Ends a course, with or without the degree at the end of it.</summary>
    private void Leave(int slot, bool graduated)
    {
        HouseholdTable households = _world.Households;

        households.State[slot] = (byte)HouseholdState.None;
        households.StateEndsDay[slot] = 0;
        households.University[slot] = default;

        foreach (int member in _world.Members.Walk(slot))
        {
            if (_world.Citizens.Age[member] == 0)
            {
                continue;
            }

            if (graduated)
            {
                _world.Citizens.SkillTier[member] = SchoolingRuleset.TopTier;
            }

            // Back to concluding nothing, so the next employment occasion decides what to say about
            // them rather than leaving `Studying` standing on somebody who has stopped.
            _world.Citizens.Employment[member] = (byte)EmploymentState.None;
        }
    }

    private int FirstAdult(int slot)
    {
        foreach (int member in _world.Members.Walk(slot))
        {
            if (_world.Citizens.Age[member] != 0)
            {
                return member;
            }
        }

        return -1;
    }
}

/// <summary>What one Day of the university did.</summary>
public readonly record struct SchoolingReading(
    int EnrolledPublic,
    int EnrolledPrivate,
    int Graduated,
    int DroppedOut,
    int TurnedAway,
    int Studying,
    long Tuition);
