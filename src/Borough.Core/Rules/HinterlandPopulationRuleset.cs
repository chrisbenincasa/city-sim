using Borough.Core.Quantities;

namespace Borough.Core.Rules;

/// <summary>
/// <c>[immigration]</c>: the four durations that make the Outside a stock rather than a caller.
/// </summary>
/// <remarks>
/// <para>
/// <b>Absence is the mode this build already had, in which every arrival is an <c>Arrive</c>
/// command.</b> A world that states the table gets people who present themselves out of a counted
/// population; a world that omits it gets the caller-supplied flow unchanged. There is no third
/// behaviour and no default to argue, which is <see cref="PlacementRuleset.MuPercent"/>'s polarity
/// and the same reason.
/// </para>
/// <para>
/// ⚠ <b>All four are stated together or the table is not stated at all.</b> They describe one
/// circuit — how often somebody reconsiders the city, how fast the Outside refills, how long a
/// willing family waits at a full gate and how often it reviews that wait — and a file stating
/// three of them has described half a mechanism with nothing to say what the fourth means.
/// </para>
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
/// <para>
/// <b>A composition is a storage key and never a domain actor.</b> It is the ordinary word for
/// <em>who this family is made of</em> — a Life Stage, so many adults at each Skill Tier, so many
/// children, and which third of the Hinterland's purse range they carry. <c>CONTEXT.md</c>'s
/// <i>Terms we deliberately do not use</i> bans Cohort by name, and this is not one: nothing here
/// decides anything together, and a group is split the moment one of its Households leaves.
/// </para>
/// <para>
/// ⚠ <b>The three adult counts are three fields rather than an array, and that is
/// <c>[[hinterland]] prices</c>'s decision arriving again.</b> A <c>record struct</c> in
/// <c>Borough.Core</c> satisfies <c>unmanaged</c> under <c>05 §4</c> lint 7, so an array inside one
/// would give it reference equality and an array's default hash code. The file authors
/// <c>adults_by_tier = [1, 1, 0]</c> and the loader spreads it across the three.
/// </para>
/// <para>
/// ⚠ <b>The count is the resting target as well as the opening stock</b>, which is why a file may
/// author zero: it declares a composition the Outside keeps none of but will hold returns in, and
/// recovers back to empty. A group nobody authored and returns created has a target of zero by the
/// same rule.
/// </para>
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
    /// <b>An authored zero and a return-only group are different rows and the flag is the only
    /// thing that separates them.</b> Both hold no stock; an authored one persists empty because
    /// its target is a decision, and a return-only one is freed once nothing stands in it.
    /// </remarks>
    public bool Authored { get; init; }

    /// <summary>How many adults one Household of this composition holds.</summary>
    public int Adults => AdultsTier1 + AdultsTier2 + AdultsTier3;

    /// <summary>How many people one Household of this composition holds.</summary>
    public int Members => Adults + Children;

    /// <summary>How many people the whole group holds.</summary>
    public long People => (long)Households * Members;
}
