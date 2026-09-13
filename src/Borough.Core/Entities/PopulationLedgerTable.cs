namespace Borough.Core.Entities;

using Borough.Core.Tables;

/// <summary>
/// Where every person in this city came from and where everyone who left it went: one row, for ever.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="MoneySupplyTable"/>'s argument about people.</b> <em>Nobody is created or destroyed
/// except at a named door</em> cannot be checked from a snapshot — a count of the live Citizens is
/// just as consistent after somebody has been freed by accident as before. What makes it checkable is
/// an opening figure and a classified flow per door, held apart from the rows, so that the check
/// compares two quantities arrived at differently. Summing the tables to produce the anchor is the
/// failure that invariant found at milestone 10 task 1: a check that recomputes the producer's own
/// expression checks that a write happened and never what was written.
/// </para>
/// <para>
/// <b>A door is a reason and not a row operation.</b> Freeing a Citizen row is one implementation with
/// several callers, and they mean different things: a fertility birth, an admission at a gate, an
/// illness death, a Household that emigrated, a Household that dissolved when its last stage ended,
/// and a fixture bulldozing rows. Which one it was cannot be recovered afterwards, so the reason is
/// recorded where the decision is made — <c>World.Bear</c>, <c>World.DieCitizen</c>,
/// <c>World.Depart</c>, <c>World.Dissolve</c> — and the unaccounted cleanup those call is private.
/// </para>
/// <para>
/// ⚠ <b><see cref="Sealed"/> exists because world creation is not a flow.</b> A synthesised city is
/// built by the same public doors a fixture uses, so its setup counts as scenario creation until the
/// first <c>Simulation.Step</c> folds those entries into the opening figures and seals the row. After
/// that the same call is an ordinary recorded event. Loading a save cannot seal it again: what the
/// save carries is a sealed row.
/// </para>
/// <para>
/// ⚠ <b>Households and people are counted separately, and neither is derivable from the other.</b>
/// A child leaving home creates a Household and adds nobody; an arrival creates one Household and
/// several people; a dissolution removes one Household and everybody in it. Two equations hold at
/// once, and a single counter could not state either of them.
/// </para>
/// </remarks>
[Table]
public sealed class PopulationLedgerTable
{
    /// <summary>The row the account lives at, for the life of the world.</summary>
    public const int Slot = 0;

    private readonly Rows<PopulationLedger> _rows;

    /// <summary>Builds the account at zero, which is what an unbuilt world holds.</summary>
    public PopulationLedgerTable()
    {
        _rows = new Rows<PopulationLedger>("population_ledger", 1, Buffering.OneCopy);

        Sealed = _rows.Saved<byte>("sealed", Touch.Cold);

        OpeningCityPeople = _rows.Saved<long>("opening_city_people", Touch.Cold);
        OpeningCityHouseholds = _rows.Saved<long>("opening_city_households", Touch.Cold);
        OpeningOutsidePeople = _rows.Saved<long>("opening_outside_people", Touch.Cold);
        OpeningOutsideHouseholds = _rows.Saved<long>("opening_outside_households", Touch.Cold);

        Births = _rows.Saved<long>("births", Touch.Cold);
        Admissions = _rows.Saved<long>("admissions", Touch.Cold);
        ScenarioAdditions = _rows.Saved<long>("scenario_additions", Touch.Cold);
        Departures = _rows.Saved<long>("departures", Touch.Cold);
        IllnessDeaths = _rows.Saved<long>("illness_deaths", Touch.Cold);
        DissolutionPeople = _rows.Saved<long>("dissolution_people", Touch.Cold);
        ScenarioRemovals = _rows.Saved<long>("scenario_removals", Touch.Cold);

        HouseholdsCreated = _rows.Saved<long>("households_created", Touch.Cold);
        HouseholdsFormed = _rows.Saved<long>("households_formed", Touch.Cold);
        HouseholdsAdmitted = _rows.Saved<long>("households_admitted", Touch.Cold);
        HouseholdsDeparted = _rows.Saved<long>("households_departed", Touch.Cold);
        HouseholdsDissolved = _rows.Saved<long>("households_dissolved", Touch.Cold);
        HouseholdsRemoved = _rows.Saved<long>("households_removed", Touch.Cold);

        _rows.Seal();

        // One row for the life of the world, never freed. MoneySupplyTable's line and its reason.
        _rows.Allocate();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<PopulationLedger> Rows => _rows;

    /// <summary>Whether the founding figures have been taken. Written once, by the first Tick.</summary>
    public Column<byte> Sealed { get; }

    /// <summary>How many people the city was founded with.</summary>
    public Column<long> OpeningCityPeople { get; }

    /// <summary>How many Households it was founded with.</summary>
    public Column<long> OpeningCityHouseholds { get; }

    /// <summary>How many people stood behind the map's edges at world creation.</summary>
    /// <remarks>
    /// <b>Filled in at construction from validated Ruleset content, and retaken at the seal.</b> The
    /// Outside is not built by the public doors, so the construction pass states the whole of it. The
    /// retake exists for the narrow window a fixture has before the first Tick, where a Departure or a
    /// return can cross an edge that the construction figure knows nothing about.
    /// </remarks>
    public Column<long> OpeningOutsidePeople { get; }

    /// <summary>How many Households stood behind them.</summary>
    public Column<long> OpeningOutsideHouseholds { get; }

    /// <summary>People born here. <c>World.Bear</c>, and nothing else.</summary>
    public Column<long> Births { get; }

    /// <summary>People admitted through a gate.</summary>
    public Column<long> Admissions { get; }

    /// <summary>
    /// People created by an explicit instruction rather than by a mechanism.
    /// </summary>
    /// <remarks>
    /// <b>World creation before the seal, and a fixture or a scenario command after it.</b> It is not
    /// a birth: nobody's fertility produced them and no Hinterland lost them, so folding them in with
    /// either would make the other two counts unreadable.
    /// </remarks>
    public Column<long> ScenarioAdditions { get; }

    /// <summary>People who emigrated. <c>World.Depart</c>, and nothing else.</summary>
    public Column<long> Departures { get; }

    /// <summary>People who died of an untreated illness. <c>World.DieCitizen</c>.</summary>
    public Column<long> IllnessDeaths { get; }

    /// <summary>People who went with a Household whose last Life Stage ended.</summary>
    public Column<long> DissolutionPeople { get; }

    /// <summary>People removed by an explicit instruction rather than by a mechanism.</summary>
    public Column<long> ScenarioRemovals { get; }

    /// <summary>Households created directly — world creation, a fixture, a scenario.</summary>
    public Column<long> HouseholdsCreated { get; }

    /// <summary>Households the city formed itself, when children left home.</summary>
    public Column<long> HouseholdsFormed { get; }

    /// <summary>Households admitted through a gate.</summary>
    public Column<long> HouseholdsAdmitted { get; }

    /// <summary>Households that emigrated.</summary>
    public Column<long> HouseholdsDeparted { get; }

    /// <summary>Households whose last Life Stage ended.</summary>
    public Column<long> HouseholdsDissolved { get; }

    /// <summary>Households removed by an explicit instruction.</summary>
    public Column<long> HouseholdsRemoved { get; }

    /// <summary>How many people the account says are alive in the city.</summary>
    public long People =>
        OpeningCityPeople[Slot] + Births[Slot] + Admissions[Slot] + ScenarioAdditions[Slot]
        - Departures[Slot] - IllnessDeaths[Slot] - DissolutionPeople[Slot] - ScenarioRemovals[Slot];

    /// <summary>How many Households the account says are live in the city.</summary>
    public long Households =>
        OpeningCityHouseholds[Slot] + HouseholdsCreated[Slot] + HouseholdsFormed[Slot]
        + HouseholdsAdmitted[Slot] - HouseholdsDeparted[Slot] - HouseholdsDissolved[Slot]
        - HouseholdsRemoved[Slot];

    /// <summary>
    /// Folds every setup entry into the opening figures, once.
    /// </summary>
    /// <remarks>
    /// <b>It folds the counters rather than counting the rows, and that is what gives the invariant
    /// teeth.</b> Reading the live tables here would make the opening figure agree with them by
    /// construction, so a setup door that forgot to record itself would be laundered into the anchor
    /// and nothing would ever fire. Folding what was recorded means a missing setup write shows up as
    /// a mismatch on the first Tick.
    /// </remarks>
    /// <returns>Whether this call was the one that sealed it.</returns>
    public bool Seal()
    {
        if (Sealed[Slot] != 0)
        {
            return false;
        }

        OpeningCityPeople[Slot] = People;
        OpeningCityHouseholds[Slot] = Households;

        Births[Slot] = 0;
        Admissions[Slot] = 0;
        ScenarioAdditions[Slot] = 0;
        Departures[Slot] = 0;
        IllnessDeaths[Slot] = 0;
        DissolutionPeople[Slot] = 0;
        ScenarioRemovals[Slot] = 0;

        HouseholdsCreated[Slot] = 0;
        HouseholdsFormed[Slot] = 0;
        HouseholdsAdmitted[Slot] = 0;
        HouseholdsDeparted[Slot] = 0;
        HouseholdsDissolved[Slot] = 0;
        HouseholdsRemoved[Slot] = 0;

        Sealed[Slot] = 1;

        return true;
    }
}
