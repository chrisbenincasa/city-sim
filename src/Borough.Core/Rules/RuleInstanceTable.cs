namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The Rule Instances: one row per (Building, Bin Rule), carrying where that Rule has got to.
/// </summary>
/// <remarks>
/// <para>
/// <b>The two states share <see cref="QueueNext"/>, and <see cref="Blocked"/> is the
/// discriminator.</b> A set <see cref="Blocked"/> means the row is on one of
/// <see cref="WaitingOn"/>'s two wait lists; <see cref="Blocking.Nothing"/> means it is on the Event
/// Wheel bucket for <see cref="NextTick"/>. There is no third state and no state where it is on both,
/// because there is only one link to be on a list with.
/// </para>
/// <para>
/// <b>A subscription records <em>which</em> Bin and <em>why</em>, and never <em>how much</em></b>
/// (<c>adr/0063</c>). There was a <c>shortfall</c> column here — the deficit the waiter computed while
/// failing, <c>(min × amount) − available</c> — and it was deleted rather than retyped: nothing
/// downstream ever read it as an entitlement, it was a filter deciding whether to re-arm, and the Bin
/// plus the Ruleset in force answer the same question at the moment the drain asks it. Storing it made
/// the answer a fact about the past, so a tuning reload that halved a Rule's input never reached the
/// Building starving for the old amount. See <see cref="Rules.RuleEngine.Requirement"/>.
/// </para>
/// <para>
/// <b>Nothing here can go stale, which is the point.</b> A waiter whose own Bins moved while it slept
/// re-checks atomicity in Phase 2, fails, and resubscribes; a waiter whose Ruleset moved under it is
/// measured against the numbers in force, because there is no number of its own to measure against.
/// </para>
/// </remarks>
[Table]
public sealed class RuleInstanceTable
{
    private readonly Rows<RuleInstance> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="buildings">The table this one's <see cref="Building"/> handles address.</param>
    /// <param name="bins">The table this one's <see cref="WaitingOn"/> handles address.</param>
    /// <param name="households">The table this one's <see cref="Household"/> handles address.</param>
    /// <param name="businesses">The table this one's <see cref="Business"/> handles address.</param>
    public RuleInstanceTable(
        int capacity,
        BuildingTable buildings,
        BinTable bins,
        HouseholdTable households,
        BusinessTable businesses)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(bins);
        ArgumentNullException.ThrowIfNull(households);
        ArgumentNullException.ThrowIfNull(businesses);

        _rows = new Rows<RuleInstance>("rule_instance", capacity, Buffering.OneCopy);

        Building = _rows.SavedHandle("building", buildings.Rows);
        Household = _rows.SavedHandle("household", households.Rows);
        Business = _rows.SavedHandle("business", businesses.Rows);
        Rule = _rows.Saved<RuleId>("rule");
        NextTick = _rows.Saved<Ticks>("next_tick", Touch.PerTick);
        WaitingOn = _rows.SavedHandle("waiting_on", bins.Rows, Touch.PerTick);
        Blocked = _rows.Saved<Blocking>("blocked", Touch.PerTick);
        Reported = _rows.Saved<ConditionId>("reported", Touch.PerTick);
        StarvedSince = _rows.Saved<Ticks>("starved_since", Touch.PerTick);
        QueueNext = _rows.Saved<int>("queue_next", Touch.PerTick);
        RuleNext = _rows.Derived<int>("rule_next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<RuleInstance> Rows => _rows;

    /// <summary>The premises this Rule runs on. <b>Set for every row, tenant's Rules included.</b></summary>
    /// <remarks>
    /// <b>It stopped being the subject and stayed the place</b> (<c>adr/0141</c>). A tenant's Rule
    /// still emits under a footprint and still reads a Readout scoped to a Building, so a Rule
    /// Instance needs the premises whoever runs it — which is why this column is unconditional and
    /// <see cref="Household"/> is the one that may be unset.
    /// </remarks>
    public HandleColumn<Building> Building { get; }

    /// <summary>
    /// The Occupant running this Rule, or the unset handle when the premises run it themselves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The subject, and unset <em>is</em> the answer rather than a missing one</b>
    /// (<c>adr/0141</c>): a Rule whose Bins all belong to the premises is the premises' Rule, and
    /// there is no tenant to name. <c>Ruleset.RuleDefinition.Tenancy</c> decides which, at load,
    /// from the Rule's own <c>local</c> terms.
    /// </para>
    /// <para>
    /// <b>Typed to <see cref="Entities.Household"/> rather than to an Occupant, which is
    /// <c>CONTEXT.md</c> → Occupant's own shape.</b> An Occupant is *"a concept spanning two lists
    /// rather than a type"*, and the Building already carries two homogeneous lists rather than one
    /// polymorphic one so that every handle stays typed and lint 7 is satisfied without a
    /// discriminated union. ✅ <b>The Business got its own column at milestone 26</b> — see
    /// <see cref="Business"/>. **This paragraph said *"which is milestone 27"* and milestone 27
    /// closed without it** (<c>plans/0041</c> **G39**), which is `adr/0093`'s failure mode in a doc
    /// comment: ***a citation to a closed milestone is a promise nothing kept.***
    /// </para>
    /// <para>
    /// <b>A tenant's Rule Instance lives exactly as long as the tenancy.</b> It is created when the
    /// Household moves in and freed when it moves out, which is what keeps this handle from ever
    /// dangling and is the same rule the tenant's Bins follow — their ceiling is the premises'
    /// (<c>adr/0141</c>), so neither can outlive the tenancy that gave it one.
    /// </para>
    /// </remarks>
    public HandleColumn<Household> Household { get; }

    /// <summary>
    /// The Business running this Rule, or the unset handle when a Household or the premises run it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The third subject</b> (<c>adr/0166</c>), and it is a <em>second</em> Occupant rather than a
    /// second kind of premises: a Business takes one of the kind's <c>occupants</c> slots exactly as
    /// a Household does (<c>adr/0147</c>). <see cref="Rules.RuleDefinition.Tenancy"/> decides which
    /// of the three at load, from the Rule's own <c>local</c> terms.
    /// </para>
    /// <para>
    /// ⚠ <b>Two unset handles is the premises' Rule; exactly one set is that Occupant's; both set is
    /// unreachable.</b> The loader refuses a Rule whose local terms address two owners, so the
    /// discriminant stays sound without a tag column — which is what keeps
    /// <c>CONTEXT.md</c> → Occupant's *"a concept spanning two lists rather than a type"* true of
    /// three lists as well.
    /// </para>
    /// <para>
    /// <b>Typed to <see cref="Entities.Business"/> for the reason <see cref="Household"/> is typed to
    /// a Household</b>: every handle stays typed, and lint 7 is satisfied without a discriminated
    /// union.
    /// </para>
    /// <para>
    /// 🔴 <b>This is the column milestone 27 task 9 built and reverted with a
    /// <c>StaleHandleException</c></b>, and what makes it safe now is not this table. <c>adr/0142</c>
    /// makes <em>unpremised</em> a legitimate steady state with <c>Businesses.Building</c> severable,
    /// so a Rule Instance naming premises unconditionally could outlive them. Milestone 25's tenancy
    /// rule removes it: ***a Business's Rule Instances and Bins are created when it takes premises and
    /// destroyed when it loses them***, so the row cannot outlive the premises it names.
    /// <b>Task 9 built the column before the tenancy rule existed to copy</b> (<c>plans/0041</c>
    /// **G39**).
    /// </para>
    /// </remarks>
    public HandleColumn<Business> Business { get; }

    /// <summary>Which Bin Rule of the Ruleset. Resolved by the loader, never a string here.</summary>
    public Column<RuleId> Rule { get; }

    /// <summary>The Tick this is armed for. Meaningful only while <see cref="WaitingOn"/> is unset.</summary>
    public Column<Ticks> NextTick { get; }

    /// <summary>The Bin this is asleep on, or the unset handle when it is armed instead.</summary>
    public HandleColumn<Bin> WaitingOn { get; }

    /// <summary>
    /// Why it is asleep, and therefore which of <see cref="WaitingOn"/>'s two lists it is on.
    /// </summary>
    public Column<Blocking> Blocked { get; }

    /// <summary>
    /// The condition the last <c>on_fail</c> walk terminated on, or <see cref="ConditionId.None"/>
    /// when the chain has not failed.
    /// </summary>
    /// <remarks>
    /// A reporting terminal records here and leaves the chain failed (<c>adr/0045</c>). Without it a
    /// terminal is a Rule that does nothing and succeeds, which re-arms the head on <c>rate</c> and
    /// walks the chain for ever — the polling defect the subscription model exists to remove, and the
    /// one the corpus's own worked example contained.
    /// </remarks>
    public Column<ConditionId> Reported { get; }

    /// <summary>
    /// The Tick this Rule went short of an input and has been short ever since, or
    /// <see cref="Ticks"/> zero when it is not starving.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The failure pressure clock (<c>adr/0053</c>, as amended), and it is here rather than on the
    /// Building for two reasons the Building cannot express.</b> The threshold is authored in
    /// <em>missed firings</em> and a <c>rate</c> belongs to a Rule, so a kind running one Rule at 8
    /// Ticks and another at 32 means two different things by <em>three missed firings</em>. And two
    /// Rules that began failing at different moments are two durations, of which the Building's
    /// pressure is the longest — computed where the sample reads it and stored nowhere.
    /// </para>
    /// <para>
    /// <b>Only <see cref="Blocking.Supply"/> sets it.</b> A Rule stopped on
    /// <see cref="Blocking.Space"/> has a Bin that is <em>full</em>, which is what a well-supplied
    /// Building with nobody to sell to looks like, and <c>02 §5.9</c> names input starvation as the
    /// source. Waking and failing short again does not restart the clock, because the Rule never
    /// fired; only firing clears it, which is what makes this a duration of continuous starvation
    /// rather than a time since the last complaint.
    /// </para>
    /// <para>
    /// <b>Tick 0 is the sentinel and it is sound.</b> A Rule Instance is armed uniform over
    /// <c>[1, rate]</c>, so none can come due — or fail — at Tick 0. The one value a real starvation
    /// cannot carry is the one that means it is not starving.
    /// </para>
    /// </remarks>
    public Column<Ticks> StarvedSince { get; }

    /// <summary>True when this Rule has been short of an input continuously since some Tick.</summary>
    public bool IsStarving(int slot) => StarvedSince[slot] != default;

    /// <summary>The shared link: through a Bin's wait list, or through an Event Wheel bucket.</summary>
    public Column<int> QueueNext { get; }

    /// <summary>Link through the owning Building's list of the Rules it runs.</summary>
    public Column<int> RuleNext { get; }

    /// <summary>True when this row is asleep on a Bin rather than armed on the Wheel.</summary>
    public bool IsWaiting(int slot) => Blocked[slot] != Blocking.Nothing;

    /// <summary>Allocates a Rule Instance, healthy and owing nothing. Arming it is <see cref="World"/>'s.</summary>
    /// <remarks>
    /// <b>The failure state is written rather than assumed, for <see cref="BinTable.Create"/>'s
    /// reason.</b> A recycled slot carries its predecessor's columns, and the two that would survive
    /// demolition are the two this task added a consumer for: a stale <see cref="Reported"/> gives a
    /// new Building a condition it never reached, and a stale <see cref="StarvedSince"/> condemns it
    /// on the Tick it is built, at an age it has not lived. <see cref="Blocked"/> and
    /// <see cref="WaitingOn"/> are cleared on the way out by <c>World.Unlink</c> and are written here
    /// anyway, because a door that leaves half the row to somebody else's tidiness is the arrangement
    /// this class was already bitten by.
    /// </remarks>
    /// <param name="building">The premises. Set whoever runs the Rule.</param>
    /// <param name="rule">Which Bin Rule of the Ruleset.</param>
    /// <param name="tenant">
    /// The Occupant running it, or the unset handle for a Rule the premises run themselves.
    /// <b>Written here rather than left to the caller</b>, for the reason the remarks give: a
    /// recycled slot carries its predecessor's tenant, and inheriting one is a Rule that draws from
    /// a family who moved out.
    /// </param>
    internal Handle<RuleInstance> Create(
        Handle<Building> building,
        RuleId rule,
        Handle<Household> tenant = default,
        Handle<Business> trader = default)
    {
        Handle<RuleInstance> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Building[slot] = building;
        Household[slot] = tenant;
        Business[slot] = trader;
        Rule[slot] = rule;
        Blocked[slot] = Blocking.Nothing;
        WaitingOn[slot] = default;
        Reported[slot] = ConditionId.None;
        StarvedSince[slot] = default;

        return handle;
    }
}
