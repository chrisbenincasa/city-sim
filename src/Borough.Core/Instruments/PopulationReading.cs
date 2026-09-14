namespace Borough.Core.Instruments;

using Borough.Core.Entities;

/// <summary>Everyone who joined or left the city over a single Day, by the door they used.</summary>
/// <remarks>
/// <b>Households and people are counted separately because neither derives from the other.</b> A
/// child leaving home creates a Household and adds nobody; an admission creates one Household and
/// several people; a dissolution removes one Household and everybody in it.
/// </remarks>
/// <param name="Births">People born here.</param>
/// <param name="Admissions">People admitted through a gate.</param>
/// <param name="ScenarioAdditions">People an explicit instruction created.</param>
/// <param name="Departures">People who emigrated.</param>
/// <param name="IllnessDeaths">People who died of an untreated illness.</param>
/// <param name="DissolutionPeople">People who went with a Household whose last Life Stage ended.</param>
/// <param name="ScenarioRemovals">People an explicit instruction removed.</param>
/// <param name="HouseholdsCreated">Households an explicit instruction created.</param>
/// <param name="HouseholdsFormed">Households the city formed when children left home.</param>
/// <param name="HouseholdsAdmitted">Households admitted through a gate.</param>
/// <param name="HouseholdsDeparted">Households that emigrated.</param>
/// <param name="HouseholdsDissolved">Households whose last Life Stage ended.</param>
/// <param name="HouseholdsRemoved">Households an explicit instruction removed.</param>
public readonly record struct PopulationFlows(
    long Births,
    long Admissions,
    long ScenarioAdditions,
    long Departures,
    long IllnessDeaths,
    long DissolutionPeople,
    long ScenarioRemovals,
    long HouseholdsCreated,
    long HouseholdsFormed,
    long HouseholdsAdmitted,
    long HouseholdsDeparted,
    long HouseholdsDissolved,
    long HouseholdsRemoved)
{
    /// <summary>People who arrived, however they arrived.</summary>
    public long PeopleIn => Births + Admissions + ScenarioAdditions;

    /// <summary>People who left, however they left.</summary>
    public long PeopleOut =>
        Departures + IllnessDeaths + DissolutionPeople + ScenarioRemovals;
}

/// <summary>
/// The city's population account, with the Day in progress and the last complete Day beside it.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="Residual"/> is an account check and never an explanation.</b> A zero says the live
/// rows and the classified flows agree about how many people are here; it says nothing whatever about
/// whether the city is thriving. A non-zero says a door wrote no entry, which is a defect rather than
/// a finding about the city.
/// </para>
/// <para>
/// <b>The Pool figures are people the city has already let in.</b> Somebody in the Unplaced Pool is
/// admitted and looking for a home, which is a different state from waiting outside a full door —
/// that one is <see cref="HinterlandReading.QueueHouseholds"/>. A reader that labels both
/// <em>unplaced</em> has lost the distinction the two counts exist for.
/// </para>
/// </remarks>
/// <param name="Day">The Day <paramref name="Today"/> counts.</param>
/// <param name="People">People the account says are here.</param>
/// <param name="Households">Households the account says are here.</param>
/// <param name="LivePeople">Citizen rows actually standing.</param>
/// <param name="LiveHouseholds">Household rows actually standing.</param>
/// <param name="PeopleAtDayStart">What the account said when this Day opened.</param>
/// <param name="HouseholdsAtDayStart">What it said about Households when this Day opened.</param>
/// <param name="PeopleAtPreviousDayStart">What it said when the last complete Day opened.</param>
/// <param name="HouseholdsAtPreviousDayStart">The same, for Households.</param>
/// <param name="PoolHouseholds">Admitted Households still looking for a home.</param>
/// <param name="PoolPeople">The people in them.</param>
/// <param name="PoolOldestWait">How long the longest-searching one has looked, in Ticks.</param>
/// <param name="Today">The Day in progress.</param>
/// <param name="Yesterday">The last complete Day.</param>
/// <param name="Ever">Every Day there has been, which is what the counters hold outright.</param>
public readonly record struct PopulationReading(
    int Day,
    long People,
    long Households,
    long LivePeople,
    long LiveHouseholds,
    long PeopleAtDayStart,
    long HouseholdsAtDayStart,
    long PeopleAtPreviousDayStart,
    long HouseholdsAtPreviousDayStart,
    int PoolHouseholds,
    long PoolPeople,
    ulong PoolOldestWait,
    PopulationFlows Today,
    PopulationFlows Yesterday,
    PopulationFlows Ever)
{
    /// <summary>Which Day's worth of a counter a figure is taken from.</summary>
    private enum Window
    {
        Today,
        Yesterday,
        Ever,
    }

    /// <summary>Live Citizen rows less what the account expects. Zero, or somebody is unaccounted.</summary>
    public long Residual => LivePeople - People;

    /// <summary>The same check over Households.</summary>
    public long HouseholdResidual => LiveHouseholds - Households;

    /// <summary>How the city's population has moved today.</summary>
    public long PeopleToday => People - PeopleAtDayStart;

    /// <summary>How it moved over the last complete Day.</summary>
    public long PeopleYesterday => PeopleAtDayStart - PeopleAtPreviousDayStart;

    /// <summary>Reads the account.</summary>
    /// <param name="world">The world to read, which this leaves exactly as it found it.</param>
    /// <returns>The account as it stands.</returns>
    public static PopulationReading Of(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        PopulationLedgerTable ledger = world.PopulationLedger;
        UnplacedTable pool = world.UnplacedPool;

        long poolPeople = 0;
        ulong oldest = 0;

        for (int position = 0; position < pool.Count; position++)
        {
            if (world.Households.Rows.TryResolve(pool.At(position), out int household))
            {
                poolPeople += world.Members.Length(household);
            }

            ulong waited = world.Tick.Raw - (ulong)pool.Since[position];

            if (waited > oldest)
            {
                oldest = waited;
            }
        }

        return new PopulationReading(
            ledger.FlowDay[PopulationLedgerTable.Slot],
            ledger.People,
            ledger.Households,
            world.Citizens.Rows.LiveCount,
            world.Households.Rows.LiveCount,
            ledger.PeopleAtDayStart,
            ledger.HouseholdsAtDayStart,
            ledger.PeopleAtPreviousDayStart,
            ledger.HouseholdsAtPreviousDayStart,
            pool.Count,
            poolPeople,
            oldest,
            Flows(ledger, Window.Today),
            Flows(ledger, Window.Yesterday),
            Flows(ledger, Window.Ever));
    }

    private static PopulationFlows Flows(PopulationLedgerTable ledger, Window window)
    {
        int slot = PopulationLedgerTable.Slot;

        return new PopulationFlows(
            Figure(ledger.Births, ledger.BirthsAtDayStart, ledger.BirthsAtPreviousDayStart),
            Figure(ledger.Admissions, ledger.AdmissionsAtDayStart, ledger.AdmissionsAtPreviousDayStart),
            Figure(
                ledger.ScenarioAdditions,
                ledger.ScenarioAdditionsAtDayStart,
                ledger.ScenarioAdditionsAtPreviousDayStart),
            Figure(ledger.Departures, ledger.DeparturesAtDayStart, ledger.DeparturesAtPreviousDayStart),
            Figure(
                ledger.IllnessDeaths,
                ledger.IllnessDeathsAtDayStart,
                ledger.IllnessDeathsAtPreviousDayStart),
            Figure(
                ledger.DissolutionPeople,
                ledger.DissolutionPeopleAtDayStart,
                ledger.DissolutionPeopleAtPreviousDayStart),
            Figure(
                ledger.ScenarioRemovals,
                ledger.ScenarioRemovalsAtDayStart,
                ledger.ScenarioRemovalsAtPreviousDayStart),
            Figure(
                ledger.HouseholdsCreated,
                ledger.HouseholdsCreatedAtDayStart,
                ledger.HouseholdsCreatedAtPreviousDayStart),
            Figure(
                ledger.HouseholdsFormed,
                ledger.HouseholdsFormedAtDayStart,
                ledger.HouseholdsFormedAtPreviousDayStart),
            Figure(
                ledger.HouseholdsAdmitted,
                ledger.HouseholdsAdmittedAtDayStart,
                ledger.HouseholdsAdmittedAtPreviousDayStart),
            Figure(
                ledger.HouseholdsDeparted,
                ledger.HouseholdsDepartedAtDayStart,
                ledger.HouseholdsDepartedAtPreviousDayStart),
            Figure(
                ledger.HouseholdsDissolved,
                ledger.HouseholdsDissolvedAtDayStart,
                ledger.HouseholdsDissolvedAtPreviousDayStart),
            Figure(
                ledger.HouseholdsRemoved,
                ledger.HouseholdsRemovedAtDayStart,
                ledger.HouseholdsRemovedAtPreviousDayStart));

        long Figure(
            Tables.Column<long> total,
            Tables.Column<long> dayStart,
            Tables.Column<long> previousDayStart) => window switch
        {
            Window.Today => total[slot] - dayStart[slot],
            Window.Yesterday => dayStart[slot] - previousDayStart[slot],
            _ => total[slot],
        };
    }
}
