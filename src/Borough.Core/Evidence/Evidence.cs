namespace Borough.Core.Evidence;

using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// <c>02 §9</c>'s question surface: one entry point per subject, assembled on a click.
/// </summary>
/// <remarks>
/// <para>
/// <b>An assembler rather than a store</b>, which is milestone 6's D2. Most of <c>02 §9</c> needs no
/// accumulator — a Building's occupants and Bin levels are live state and a Rule's pressure is two
/// columns and a subtraction — so the default is to read the world and re-derive, and a stored answer
/// needs an argument each time: <em>the entity holding it will not exist when the question is
/// asked</em>. Exactly one thing here meets that test today, and it is
/// <see cref="CondemnationTrailTable"/>.
/// </para>
/// <para>
/// <b>Ids and numbers, never strings</b> (<c>adr/0002</c>). Every kind, condition, Resource and
/// activity below leaves as the id the Ruleset gave it, and the host resolves it. The leak this guards
/// against is not <c>using Godot;</c> — it is a method that returns a formatted string because a panel
/// wanted one.
/// </para>
/// <para>
/// <b>Static and <c>World</c>-taking, on <c>Readouts.Read</c>'s precedent</b>, which is the other
/// pure per-entity read in the project and is documented as <em>the set a shell may enumerate to
/// build an inspector</em>. Nothing here is stored on the <c>World</c>, because none of it is state.
/// </para>
/// <para>
/// ⚠ <b>Every method here allocates and none may be called from <c>step()</c>.</b> That is the
/// hot/cold axis <see cref="ColdPathAttribute"/> records, and it is what buys the arrays. The cost is
/// paid once by a human who is waiting.
/// </para>
/// </remarks>
public static class Evidence
{
    /// <summary>
    /// Assembles <c>02 §9</c>'s Building answer.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="building">Which Building.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static BuildingEvidence OfBuilding(World world, Handle<Building> building)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Buildings.Rows.Resolve(building);
        byte kind = world.Buildings.Kind[slot];

        bool declared = world.Rules.Declares(kind);
        int occupancy = declared ? world.Rules.Kind(kind).Occupants : 0;
        int jobs = declared ? world.Rules.Kind(kind).Jobs : 0;

        Handle<Household>[] occupants = new Handle<Household>[world.Occupants.Length(slot)];
        int at = 0;

        foreach (int household in world.Occupants.Walk(slot))
        {
            occupants[at++] = world.Households.Rows.At(household);
        }

        Handle<Citizen>[] workers = new Handle<Citizen>[world.Workers.Length(slot)];
        at = 0;

        foreach (int citizen in world.Workers.Walk(slot))
        {
            workers[at++] = world.Citizens.Rows.At(citizen);
        }

        BinEvidence[] bins = new BinEvidence[world.BuildingBins.Length(slot)];
        at = 0;

        foreach (int bin in world.BuildingBins.Walk(slot))
        {
            bins[at++] = new BinEvidence(
                world.Bins.Resource[bin], world.Bins.LevelAt(bin), world.Bins.Capacity[bin]);
        }

        RuleEvidence[] rules = new RuleEvidence[world.BuildingRules.Length(slot)];
        Ticks now = world.Tick;
        long pressure = 0;
        at = 0;

        foreach (int instance in world.BuildingRules.Walk(slot))
        {
            RuleEvidence evidence = ReadRule(world, instance, now);

            if (evidence.MissedFirings > pressure)
            {
                pressure = evidence.MissedFirings;
            }

            rules[at++] = evidence;
        }

        return new BuildingEvidence(
            building, kind, world.Buildings.Lot[slot], declared, occupancy, jobs,
            occupants, workers, bins, rules, pressure);
    }

    /// <summary>
    /// Assembles <c>02 §9</c>'s Citizen answer.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="citizen">Which Citizen.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static CitizenEvidence OfCitizen(World world, Handle<Citizen> citizen)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Citizens.Rows.Resolve(citizen);
        Handle<Household> household = world.Citizens.HouseholdOf[slot];

        Handle<Building> home = world.Households.Rows.TryResolve(household, out int householdSlot)
            ? world.Households.Dwelling[householdSlot]
            : default;

        // Severable, so an unresolvable workplace is the job having stopped existing rather than a
        // broken handle -- CitizenTable.Workplace says so at the declaration. Reported as unset,
        // which is what the simulation itself believes.
        Handle<Building> workplace = world.Buildings.Rows.IsValid(world.Citizens.Workplace[slot])
            ? world.Citizens.Workplace[slot]
            : default;

        return new CitizenEvidence(
            citizen,
            household,
            home,
            workplace,
            world.Citizens.Activity[slot],
            world.Citizens.PlannedCommute[slot],
            world.Citizens.ReachFailures[slot],
            InFlightTrip(world, citizen));
    }

    /// <summary>
    /// Assembles <c>02 §9</c>'s Lot answer — <em>why it is vacant</em>.
    /// </summary>
    /// <param name="world">The world to read.</param>
    /// <param name="lot">Which Lot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The handle does not resolve.</exception>
    public static LotEvidence OfLot(World world, Handle<Lot> lot)
    {
        ArgumentNullException.ThrowIfNull(world);

        int slot = world.Lots.Rows.Resolve(lot);
        bool vacant = world.Lots.IsVacant(slot);

        VacancyReason reason = VacancyReason.None;

        if (vacant)
        {
            if (!world.Lots.HasFrontage(slot))
            {
                reason |= VacancyReason.NoFrontage;
            }

            if (world.UnplacedPool.Count == 0)
            {
                reason |= VacancyReason.NobodySeeking;
            }

            if (!AdmittedByAnyRule(world, slot))
            {
                reason |= VacancyReason.NotZoned;
            }
        }

        int building = world.Lots.BuildingOn(slot);

        return new LotEvidence(
            lot,
            world.Lots.Zone[slot],
            vacant,
            world.Lots.AddressOf(slot),
            reason,
            LastCondemnation(world, lot),
            building == Rows.NoSlot ? default : world.Buildings.Rows.At(building));
    }

    /// <summary>
    /// One Rule Instance's state, with <c>02 §9</c>'s <em>last ran and whether it succeeded</em>
    /// derived from it.
    /// </summary>
    /// <remarks>
    /// <b>The derivation rests on a structural invariant rather than on a stored timestamp</b>: a Rule
    /// Instance is armed on the Wheel or asleep on a wait list, never both, and <c>EventWheel.Arm</c>
    /// is the only writer of <c>NextTick</c>. Armed means the last firing worked and re-armed at
    /// <c>+rate</c>; asleep means it did not, and left <c>NextTick</c> at the Tick it fired on. See
    /// <see cref="RuleEvidence"/> for the one case this cannot separate.
    /// </remarks>
    private static RuleEvidence ReadRule(World world, int instance, Ticks now)
    {
        RuleId id = world.RuleInstances.Rule[instance];
        uint rate = world.Rules.Rule(id).Rate;
        var blocked = (Blocking)world.RuleInstances.Blocked[instance];
        bool succeeded = blocked == Blocking.Nothing;
        Ticks due = world.RuleInstances.NextTick[instance];

        Ticks lastRan = succeeded && due.Raw >= rate
            ? new Ticks(due.Raw - rate)
            : due;

        Ticks starvedSince = world.RuleInstances.StarvedSince[instance];

        // adr/0053: pressure is in MISSED FIRINGS rather than in Ticks, so that a Ruleset which
        // retunes every rate does not silently retune every Building's lifespan. Zero rather than a
        // division by zero on a rate the loader should already have refused.
        //
        // ZoneRuleEngine.Condemn deliberately does NOT divide -- it cross-multiplies, because the
        // quotient is an answer it throws away. Here the quotient IS the answer, so the division is
        // spelled rather than avoided, and FloorDiv states the rounding (05 §4 lint 3). Both operands
        // are non-negative, so floor and truncation agree; naming the rounding is the point.
        long missed = starvedSince.Raw == 0 || rate == 0
            ? 0
            : IntegerMath.FloorDiv((long)(now.Raw - starvedSince.Raw), rate);

        return new RuleEvidence(
            id,
            lastRan,
            succeeded,
            blocked,
            world.RuleInstances.Reported[instance],
            starvedSince,
            rate,
            missed);
    }

    /// <summary>
    /// Whether any <c>[[zone_rule]]</c> in the Ruleset in force admits this Lot's zone bits.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The predicate is re-expressed here rather than called</b>, because
    /// <c>ZoneRuleEngine.Create</c> is private and mutates — it raises a Building and bumps a counter
    /// — so there is nothing to call. The clause copied is
    /// <c>(Lots.Zone[lot] &amp; definition.Admits) == 0</c>, and it is a pure column read that touches
    /// no randomness. <b>A copied predicate is a second copy of a fact</b>
    /// (<c>plans/0012</c> <em>Cause 1</em>), so a test asserts the two agree rather than this comment
    /// asserting it.
    /// </remarks>
    private static bool AdmittedByAnyRule(World world, int lotSlot)
    {
        ushort zone = world.Lots.Zone[lotSlot];

        foreach (ZoneRuleDefinition definition in world.Rules.ZoneRules)
        {
            if ((zone & definition.Admits) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The most recent surviving trail entry for this Lot, if there is one.
    /// </summary>
    /// <remarks>
    /// <b>Walked backwards, because the trail is dense and chronological</b> — entries slide down
    /// rather than rotating through a cursor, so the last matching index is the most recent
    /// condemnation. Slot 0 is the aggregate and carries no identity, which is why the walk stops
    /// above it: ***attribution decays to magnitude***, and an entry that has aged out is
    /// indistinguishable from one that never happened.
    /// </remarks>
    private static CondemnationEvidence? LastCondemnation(World world, Handle<Lot> lot)
    {
        CondemnationTrailTable trail = world.CondemnationTrail;

        for (int index = trail.Count - 1; index >= 0; index--)
        {
            int slot = trail.EntrySlot(index);

            if (trail.Lot[slot].Equals(lot))
            {
                return new CondemnationEvidence(
                    trail.Tick[slot], trail.Kind[slot], trail.Condition[slot]);
            }
        }

        return null;
    }

    /// <summary>
    /// The Trip this Citizen is on, found by scanning the Travellers.
    /// </summary>
    /// <remarks>
    /// <b>A scan rather than an index, and deliberately.</b> A reverse Citizen-to-Traveller column
    /// would be state the simulation never reads, maintained every Tick for a question asked on a
    /// click — which is the hot path paying for the cold one, and the wrong way round. The scan is
    /// bounded by the Travellers in flight, not by the population.
    /// </remarks>
    private static TripEvidence? InFlightTrip(World world, Handle<Citizen> citizen)
    {
        TravellerTable travellers = world.Travellers;

        for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
        {
            if (!travellers.Rows.IsLive(slot) || !travellers.Citizen[slot].Equals(citizen))
            {
                continue;
            }

            if (!world.Trips.Rows.TryResolve(travellers.Trip[slot], out int trip))
            {
                continue;
            }

            return new TripEvidence(
                travellers.Trip[slot],
                (TripPurpose)world.Trips.Purpose[trip],
                (TripFate)world.Trips.Fate[trip],
                travellers.ArrivesAt[slot]);
        }

        return null;
    }
}
