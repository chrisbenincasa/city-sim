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
/// What owns a <see cref="Bin"/>. The discriminator <c>adr/0114</c> puts on
/// <see cref="BinTable.Owner"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The four are enumerated because <c>adr/0114</c> enumerated them, not because four are built.</b>
/// Money an actor holds lives in a Bin, and the actors that hold money are a Building, a Household, a
/// Business and the treasury. <see cref="Household"/> and <see cref="Business"/> are declared and
/// throw by name where they would be resolved — <c>plans/0033</c> tasks 4b and 5 bring them — on
/// <see cref="Scope.Pool"/>'s precedent, that a named hole is better than a case that silently falls
/// through.
/// </para>
/// <para>
/// <b>It is <c>(saved AND hashed)</c>, and that is <c>adr/0114</c>'s own consequence rather than a
/// default.</b> A handle column folds the target row's monotonic never-reused id, so two owners of
/// <em>different kinds</em> sharing an id would fold identically. Here the kind is a second folded
/// column beside <see cref="BinTable.Owner"/> rather than a field inside the handle, which keeps
/// <see cref="Tables.HandleColumn{TTarget}"/> single-target and its dangling check honest.
/// </para>
/// <para>
/// ⚠ <b>This is also what makes an unset <see cref="BinTable.Owner"/> readable.</b> A treasury Bin is
/// nobody's Building, so its owner handle is <c>default</c> — and before this column existed that was
/// indistinguishable from a Building Bin whose owner was never written. Session F's rule: a
/// placeholder whose value sits inside the range of legitimate answers cannot announce itself. The
/// kind announces it, and <see cref="Building"/> beside an unset handle is now a detectable defect
/// rather than an invisible one.
/// </para>
/// </remarks>
public enum BinOwnerKind : byte
{
    /// <summary>
    /// Reserved, and never a live Bin's owner. A zeroed row must be recognisable rather than reading
    /// as whichever owner happened to be declared first — <c>CommandKind.None</c>'s reason.
    /// </summary>
    None = 0,

    /// <summary>A Building. Every Bin in the build before <c>plans/0033</c> task 1.</summary>
    Building = 1,

    /// <summary>A Household. <b>Declared and not yet owned</b> — <c>plans/0033</c> task 5.</summary>
    Household = 2,

    /// <summary>A Business. <b>Declared and not yet owned</b> — <c>plans/0033</c> task 4b.</summary>
    Business = 3,

    /// <summary>
    /// The city's own balance sheet. <b>A singleton</b>, so it carries no owner id: there is one
    /// treasury, and its Bins hang off <see cref="Entities.TreasuryTable"/>'s single row.
    /// </summary>
    Treasury = 4,
}

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
/// One Building this world condemned, and the condition that condemned it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The fourth row type in the project that is not a thing in the world</b>, and
/// <see cref="RulesetTrailEntry"/>'s sibling: a row of <see cref="CondemnationTrailTable"/> is a piece
/// of the world's <em>history</em> rather than of its present. Nothing ever holds a
/// <c>Handle&lt;CondemnationTrailEntry&gt;</c> — entries slide down a slot when the window fills, for
/// <see cref="Entities.Unplaced"/>'s reason.
/// </para>
/// <para>
/// <b>It is named for what the build does and not for what the design says.</b> <c>CONTEXT.md</c> →
/// Failure Pressure calls the end state <em>abandoned</em> and <c>adr/0091</c> leaves an abandoned
/// Building <em>standing</em> on its Lot; <see cref="ZoneRuleEngine"/>'s condemn path calls
/// <see cref="Entities.World.DestroyBuilding"/>, which frees it. <c>plans/0012</c> records that
/// divergence and <c>06</c> milestone 17 owns it, so naming this <em>abandonment</em> would be naming a
/// row for a mechanism nobody has built yet (<c>adr/0070</c>). It records condemnations because
/// condemnation is what happens.
/// </para>
/// </remarks>
public readonly struct CondemnationTrailEntry;

/// <summary>
/// Why a Rule Instance is asleep, and therefore which of a Bin's two wait lists it is on.
/// </summary>
/// <remarks>
/// <b><c>adr/0045</c>'s <em>blocking</em>, as a discriminator.</b> The ADR generalises the two failure
/// modes into one word — <em>refill if the Bin was short, drain if it was a full output</em> — and
/// this is that word made checkable. The members are named for what the waiter <em>needs</em> rather
/// than for what went wrong, so they read against <see cref="BinTable.LevelAt"/> and
/// <see cref="BinTable.SpaceAt"/>, which are the two quantities a drain actually compares a
/// shortfall against. <see cref="Nothing"/> is the armed state and is zero, so a freshly allocated row
/// is armed-shaped rather than asleep on the Bin at slot zero.
/// <para>
/// <b>There is one quantity here and two ways to be stopped by it, which is the whole of this type.</b>
/// A Bin holds a <c>level</c> against a <c>capacity</c>; <see cref="Supply"/> is that quantity read from
/// the bottom and <see cref="Space"/> is the same quantity read from the top
/// (<c>capacity − level</c>, which is <see cref="BinTable.SpaceAt"/>). <em>Empty</em> and <em>full</em>
/// are therefore not two conditions but one condition seen from two ends, and which end you are stopped
/// at depends entirely on whether your Rule was trying to take out or to put in.
/// </para>
/// <para>
/// <b>Both members were renamed on 2026-08-14 and the old names are worth knowing, because one of them
/// had escaped into an ADR meaning its opposite.</b> These were <c>Level</c> and <c>Headroom</c>: exact
/// against the code and hard to read against the city, since <em>blocked on Level</em> does not say
/// which side of the level, and <em>headroom</em> is a word about numeric range that this file also uses
/// in that sense. <c>adr/0097</c> then wrote <em>a stock failure</em> for a workplace with no free slot
/// — the <see cref="Space"/> bound — while the rest of the corpus uses <em>stock</em> for the contents,
/// which is the <see cref="Supply"/> bound. <b>Name a bound, never a level</b>: a name that does not say
/// which end it means will eventually be quoted meaning the other one.
/// </para>
/// </remarks>
public enum Blocking : byte
{
    /// <summary>Not asleep. The row is armed on the Event Wheel instead.</summary>
    Nothing = 0,

    /// <summary>
    /// <b>There is not enough in the Bin to take.</b> Asleep on an <em>input</em> Bin that was short,
    /// waiting for its level to rise — a bakery with no flour. Woken by a deposit.
    /// </summary>
    Supply = 1,

    /// <summary>
    /// <b>There is not enough room in the Bin to put.</b> Asleep on an <em>output</em> Bin that was
    /// full, waiting for room to appear — a bakery whose shelf of bread nobody has bought. Woken by a
    /// withdrawal, and never by a deposit, which is why the two lists cannot be merged.
    /// </summary>
    Space = 2,
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
