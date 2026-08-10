namespace Borough.Core.Rules;

/// <summary>
/// An integer store of one Resource on one Building, constrained to <c>[0, capacity]</c>.
/// </summary>
/// <remarks>
/// Empty for the reason <see cref="Entities.Citizen"/> is empty: under structure-of-arrays there is
/// no row struct, and this type exists to make <c>Handle&lt;Bin&gt;</c> a different type from
/// <c>Handle&lt;Building&gt;</c>. The columns are on <see cref="BinTable"/>.
/// </remarks>
public readonly struct Bin;

/// <summary>
/// One Bin Rule on one Building — the row carrying where that Rule has got to.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is in exactly one of two states at every moment</b> (<c>CONTEXT.md</c> → Rule Instance):
/// <em>armed</em>, scheduled on the Event Wheel for the Tick its <c>rate</c> re-armed it to, or
/// <em>waiting</em>, on the wait list of the one Bin it was short of. <c>02 §4.1</c> says a Rule that
/// fires re-arms and a Rule that fails subscribes <em>instead of</em> re-arming, so a row that could
/// be both would be a Rule that polls and subscribes at once — the defect subscription exists to
/// remove. The two states therefore share one link column, which makes it unrepresentable rather
/// than checked.
/// </para>
/// <para>
/// <b>Its life is its Building's.</b> Created when the Building is built, freed when the Building is
/// demolished, and never allocated or freed in between — so shortage <em>churn</em>, which
/// <c>02 §4.1</c> identifies as the real cost driver ahead of chain depth, costs no rows at all.
/// </para>
/// </remarks>
public readonly struct RuleInstance;

/// <summary>
/// One Tick's slot on the Event Wheel. See <see cref="EventWheel"/> for why it is a table.
/// </summary>
public readonly struct WheelBucket;

/// <summary>
/// One Ruleset transition this world survived, and what it cost.
/// </summary>
/// <remarks>
/// <b>The third row type in the project that is not a thing in the world</b> — a row of
/// <see cref="RulesetTrailTable"/> is a piece of the world's <em>history</em> rather than of its
/// present, which is what <c>05 §7</c> asks for and why it is state rather than a log line. Nothing
/// ever holds a <c>Handle&lt;RulesetTrailEntry&gt;</c>: entries slide down a slot when the window
/// fills, for <see cref="Entities.Unplaced"/>'s reason.
/// </remarks>
public readonly struct RulesetTrailEntry;

/// <summary>
/// Why a Rule Instance is asleep, and therefore which of a Bin's two wait lists it is on.
/// </summary>
/// <remarks>
/// <b><c>adr/0045</c>'s <em>blocking</em>, as a discriminator.</b> The ADR generalises the two failure
/// modes into one word — <em>refill if the Bin was short, drain if it was a full output</em> — and
/// this is that word made checkable. The members are named for what the waiter <em>needs</em> rather
/// than for what went wrong, so they read against <see cref="BinTable.LevelAt"/> and
/// <see cref="BinTable.HeadroomAt"/>, which are the two quantities a drain actually compares a
/// shortfall against. <see cref="Nothing"/> is the armed state and is zero, so a freshly allocated row
/// is armed-shaped rather than asleep on the Bin at slot zero.
/// </remarks>
public enum Blocking : byte
{
    /// <summary>Not asleep. The row is armed on the Event Wheel instead.</summary>
    Nothing = 0,

    /// <summary>Asleep on an input Bin that was short, waiting for its level to rise.</summary>
    Level = 1,

    /// <summary>Asleep on an output Bin that was full, waiting for headroom to appear.</summary>
    Headroom = 2,
}

/// <summary>
/// Which Bin Rule. A dense small integer, assigned by the Ruleset at load.
/// </summary>
/// <remarks>
/// <b>Not an enum, for <see cref="Tables.ResourceId"/>'s reason.</b> The set of Rules is content, and
/// the core is a stable interpreter for whatever the Ruleset declares (<c>adr/0015</c>). What the
/// core needs is that the identifiers are dense and small; what they <em>mean</em> is resolved in
/// <c>Borough.Formats</c>, which is also the only place a Rule has a name (<c>adr/0048</c>).
/// </remarks>
public readonly record struct RuleId(ushort Raw)
{
    /// <summary>The unset Rule. Zero is never a declared Rule id.</summary>
    public static RuleId None => new(0);

    /// <summary>True when this names no Rule.</summary>
    public bool IsNone => Raw == 0;
}
