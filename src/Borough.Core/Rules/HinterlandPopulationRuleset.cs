using Borough.Core.Quantities;

namespace Borough.Core.Rules;

/// <summary>
/// <c>[immigration]</c>: the four durations that make the Outside a stock rather than a caller.
/// </summary>
/// <remarks>
/// All four durations are required together. Absence retains explicit caller-supplied arrivals.
/// </remarks>
public readonly record struct ImmigrationRuleset(
    bool Stated,
    int ReconsiderDays,
    int RecoveryDays,
    int QueueWaitDays,
    int QueueReconsiderDays)
{
    /// <summary>A Ruleset whose arrivals all come from a caller. Every shipped file but one.</summary>
    public static ImmigrationRuleset None => default;

    /// <summary>
    /// The four durations, refused where one of them would describe half a circuit.
    /// </summary>
    /// <param name="reconsiderDays">How often a Household outside weighs the city again.</param>
    /// <param name="recoveryDays">
    /// How long the Outside takes to return to its resting stock. Zero freezes it in both
    /// directions, which is the isolated depletion world and not a shipped one.
    /// </param>
    /// <param name="queueWaitDays">The longest a willing family waits at a gate it cannot enter.</param>
    /// <param name="queueReconsiderDays">How often that family reviews the wait.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A duration is negative, a required one is zero, or the review is not shorter than the wait.
    /// </exception>
    public static ImmigrationRuleset From(
        int reconsiderDays, int recoveryDays, int queueWaitDays, int queueReconsiderDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reconsiderDays);
        ArgumentOutOfRangeException.ThrowIfNegative(recoveryDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(queueWaitDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(queueReconsiderDays);

        // A review at or beyond the wait it reviews never runs: the family's patience expires
        // first, so the key would load, read as a cadence and fire exactly never.
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(queueReconsiderDays, queueWaitDays);

        return new ImmigrationRuleset(
            true, reconsiderDays, recoveryDays, queueWaitDays, queueReconsiderDays);
    }

    /// <summary>Whether the Outside refills towards its resting stock.</summary>
    public bool Recovers => Stated && RecoveryDays > 0;

    /// <summary>How often a Household outside weighs the city again, in Ticks.</summary>
    public int ReconsiderTicks => Stated ? ReconsiderDays * Ticks.PerDay : 0;

    /// <summary>How long the Outside takes to return to its resting stock, in Ticks.</summary>
    public int RecoveryTicks => Stated ? RecoveryDays * Ticks.PerDay : 0;

    /// <summary>The longest a willing family waits at a gate it cannot enter, in Ticks.</summary>
    public int QueueWaitTicks => Stated ? QueueWaitDays * Ticks.PerDay : 0;

    /// <summary>How often a waiting family reviews the wait, in Ticks.</summary>
    public int QueueReconsiderTicks => Stated ? QueueReconsiderDays * Ticks.PerDay : 0;
}

/// <summary>
/// One <c>[[hinterland.population]]</c> entry: how many Households of one exact composition stand
/// behind a map edge at world creation.
/// </summary>
/// <remarks>
/// Opening count is also the recovery target. Exact composition fields keep the value
/// unmanaged; individual prospects are drawn from the aggregate stock.
/// </remarks>
public readonly record struct HinterlandPopulationDefinition
{
    /// <summary>The Life Stage every Household of this composition is in. Never zero.</summary>
    public byte Stage { get; init; }

    /// <summary>How many of its adults hold Skill Tier 1.</summary>
    public int AdultsTier1 { get; init; }

    /// <summary>How many of its adults hold Skill Tier 2.</summary>
    public int AdultsTier2 { get; init; }

    /// <summary>How many of its adults hold Skill Tier 3.</summary>
    public int AdultsTier3 { get; init; }

    /// <summary>How many children it carries.</summary>
    public int Children { get; init; }

    /// <summary>Which third of the Hinterland's purse range it arrives holding.</summary>
    public int MoneyBand { get; init; }

    /// <summary>
    /// How many Households of this composition stand behind the edge, and the count it recovers to.
    /// </summary>
    public int Households { get; init; }

    /// <summary>
    /// Whether a file declared this composition, as against returns having created it.
    /// </summary>
    /// <remarks>
    /// Authored zero-target rows persist when empty; return-only rows retire.
    /// </remarks>
    public bool Authored { get; init; }

    /// <summary>How many adults one Household of this composition holds.</summary>
    public int Adults => AdultsTier1 + AdultsTier2 + AdultsTier3;

    /// <summary>How many people one Household of this composition holds.</summary>
    public int Members => Adults + Children;

    /// <summary>How many people the whole group holds.</summary>
    public long People => (long)Households * Members;
}
