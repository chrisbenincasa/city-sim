namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// People. The largest table by row count, and the one whose per-Tick width the Event Wheel's
/// argument rests on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three columns are <see cref="Touch.PerTick"/> and the rest are not, which is the whole
/// point.</b> S4 task 2 found the corpus's <em>"on the order of 40 bytes hot"</em> was never one
/// number: the row is 13 B under the per-Tick reading — <c>next_event_tick</c>, the Wheel link and
/// the current activity — and 51 B under the working-set one. The two size different things: the
/// per-Tick figure sizes the Wheel drain and the wake gather, the working-set figure sizes the world
/// and the save copy. K0 then measured that only 13% of the world is addressable on an ordinary Tick,
/// which is the Event Wheel's premise made arithmetic.
/// </para>
/// <para>
/// <b><c>wheel_next</c> is declared here and used in slice 9, and its disposition is provisional.</b>
/// It is the intrusive link into an Event Wheel bucket, and it is <see cref="Disposition.Derived"/>
/// because the bucket a Citizen sits in is a pure function of <c>next_event_tick</c> — the Wheel is
/// an index over saved state, not state. <b>That is true of membership and is unproven of order.</b>
/// Under the rule <c>05 §3</c> now states — a list may be derived only if its <em>order</em> is
/// recoverable, not merely its membership — this holds only if nothing observable depends on the
/// order Citizens are woken in within one Tick. Phase 2 is read-only so Decide cannot be
/// order-dependent, and <c>02 §8</c> rule 5 settles contested outcomes by shuffle rather than by
/// arrival; slice 9 has to make that claim explicitly rather than inherit it, and if it fails this
/// column becomes <see cref="Disposition.Saved"/> exactly as the Bin wait list did.
/// </para>
/// </remarks>
[Table]
public sealed class CitizenTable
{
    private readonly Rows<Citizen> _rows;

    /// <param name="capacity">Initial slot count. The population; 1M is a floor rather than a cap.</param>
    /// <param name="households">The table this one's household handles address.</param>
    /// <param name="buildings">The table this one's workplace handles address.</param>
    public CitizenTable(int capacity, HouseholdTable households, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(households);
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Citizen>("citizen", capacity, Buffering.OneCopy);

        NextEventTick = _rows.Saved<Ticks>("next_event_tick", Touch.PerTick);
        WheelNext = _rows.Derived<int>("wheel_next", Touch.PerTick);
        Activity = _rows.Saved<byte>("activity", Touch.PerTick);

        HouseholdOf = _rows.SavedHandle("household", households.Rows);
        // Severable, and slice 10 is what forced the declaration. A Citizen's Workplace is somebody
        // else's Building, so demolition can free it out from under this handle — and a workplace
        // that no longer resolves is the job no longer existing, which is the fact and not a break
        // in it. The reverse index this note once said did not exist is `WorkerNext` below, built by
        // milestone 5b-bis task 2; it does not make the handle unseverable, because it is derived
        // *from* the handle and a demolition still leaves the Citizen pointing at a freed row.
        Workplace = _rows.SavedHandle(
            "workplace", buildings.Rows, reference: Reference.Severable);
        Experience = _rows.Saved<long>("experience");
        SkillTier = _rows.Saved<byte>("skill_tier");
        Employment = _rows.Saved<byte>("employment");
        MemberNext = _rows.Derived<int>("member_next");
        WorkerNext = _rows.Derived<int>("worker_next");

        Age = _rows.Saved<ushort>("age", Touch.Cold);
        Health = _rows.Saved<byte>("health", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Citizen> Rows => _rows;

    /// <summary>The Tick this Citizen next wakes on. The Event Wheel's bucket key.</summary>
    public Column<Ticks> NextEventTick { get; }

    /// <summary>Link in the Event Wheel bucket. Rebuilt from <see cref="NextEventTick"/>.</summary>
    public Column<int> WheelNext { get; }

    /// <summary>What the Citizen is doing. What a wake mutates.</summary>
    public Column<byte> Activity { get; }

    /// <summary>The Household this Citizen belongs to.</summary>
    /// <remarks>
    /// Named for the relationship rather than for the type, because <c>Household</c> alone would
    /// shadow the entity tag inside this file and read as though the column held one.
    /// </remarks>
    public HandleColumn<Household> HouseholdOf { get; }

    /// <summary>Where this Citizen works, or the unset handle, <c>default</c>.</summary>
    public HandleColumn<Building> Workplace { get; }

    /// <summary>Accumulated on-the-job experience. An i64 accumulator, per <c>05 §3</c>'s widths.</summary>
    public Column<long> Experience { get; }

    /// <summary>Which Skill Tier. Resolved through the Ruleset.</summary>
    public Column<byte> SkillTier { get; }

    /// <summary>Employment state.</summary>
    public Column<byte> Employment { get; }

    /// <summary>Link in the Household's member list.</summary>
    public Column<int> MemberNext { get; }

    /// <summary>
    /// Link in the Workplace's worker list — see <see cref="BuildingTable.WorkerHead"/>.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Disposition.Derived"/>, and the order is recoverable rather than merely the
    /// membership</b>, which is the test <c>05 §3</c> states and the one a list has to pass to be
    /// declared this way. Membership follows from <see cref="Workplace"/>; the order follows because
    /// the rebuild inserts by monotonic id rather than appending, so a Building that lost a worker
    /// and gained another into the recycled slot lists them in the same order either way.
    /// </remarks>
    public Column<int> WorkerNext { get; }

    /// <summary>Age, in Days.</summary>
    public Column<ushort> Age { get; }

    /// <summary>Health.</summary>
    public Column<byte> Health { get; }
}
