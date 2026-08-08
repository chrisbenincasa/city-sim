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
/// <b><see cref="Shortfall"/> is a number the waiter already computed</b> (<c>02 §4.1</c>): the
/// amount that was missing when it failed, <c>(min × amount) − available</c>. It is what makes the
/// queue mean anything — draining by shortfall wakes the one waiter an arrival actually satisfies,
/// where waking everybody would push the whole list into Phase 2 and let the sorted settle order pick
/// a permanent winner.
/// </para>
/// <para>
/// <b>It may go stale, and nothing special is needed.</b> If the waiter's own Bins moved while it
/// slept, it re-checks atomicity in Phase 2, fails, and resubscribes.
/// </para>
/// </remarks>
[Table]
public sealed class RuleInstanceTable
{
    private readonly Rows<RuleInstance> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="buildings">The table this one's <see cref="Building"/> handles address.</param>
    /// <param name="bins">The table this one's <see cref="WaitingOn"/> handles address.</param>
    public RuleInstanceTable(int capacity, BuildingTable buildings, BinTable bins)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(bins);

        _rows = new Rows<RuleInstance>("rule_instance", capacity, Buffering.OneCopy);

        Building = _rows.SavedHandle("building", buildings.Rows);
        Rule = _rows.Saved<RuleId>("rule");
        NextTick = _rows.Saved<Ticks>("next_tick", Touch.PerTick);
        WaitingOn = _rows.SavedHandle("waiting_on", bins.Rows, Touch.PerTick);
        Blocked = _rows.Saved<Blocking>("blocked", Touch.PerTick);
        Shortfall = _rows.Saved<int>("shortfall");
        Reported = _rows.Saved<ConditionId>("reported", Touch.PerTick);
        QueueNext = _rows.Saved<int>("queue_next", Touch.PerTick);
        RuleNext = _rows.Derived<int>("rule_next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<RuleInstance> Rows => _rows;

    /// <summary>The Building running this Rule.</summary>
    public HandleColumn<Building> Building { get; }

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

    /// <summary>What was missing when it failed. Meaningful only while <see cref="WaitingOn"/> is set.</summary>
    public Column<int> Shortfall { get; }

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

    /// <summary>The shared link: through a Bin's wait list, or through an Event Wheel bucket.</summary>
    public Column<int> QueueNext { get; }

    /// <summary>Link through the owning Building's list of the Rules it runs.</summary>
    public Column<int> RuleNext { get; }

    /// <summary>True when this row is asleep on a Bin rather than armed on the Wheel.</summary>
    public bool IsWaiting(int slot) => Blocked[slot] != Blocking.Nothing;

    /// <summary>Allocates a Rule Instance. Arming it is <see cref="World"/>'s.</summary>
    internal Handle<RuleInstance> Create(Handle<Building> building, RuleId rule)
    {
        Handle<RuleInstance> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Building[slot] = building;
        Rule[slot] = rule;

        return handle;
    }
}
