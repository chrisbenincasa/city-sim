namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// People sharing a dwelling and finances. Holds the money, and the member list.
/// </summary>
/// <remarks>
/// <para>
/// <b>Economics is <see cref="Touch.Cold"/> — entirely, and that is adr/0004's concrete case.</b>
/// Income, expenses, savings, purchases made and purchases missed are touched when a Household
/// transacts or when a panel inspects it, never on an ordinary Tick. It is what makes <em>ship with
/// role and routine, leave room for economics</em> real rather than aspirational: the later layer
/// extends a cold set of columns instead of restructuring a hot loop.
/// </para>
/// <para>
/// <b>Unemployment is not a column here.</b> S4 task 2 made it a derived readout — <em>a Household
/// where no contained Citizen holds a job</em> — walked through the member list. Should a profile
/// ever want it cached, it is declared <see cref="Disposition.Derived"/> with an invariant asserting
/// it matches the walk, because a stale bit is a Household that believes it is employed and never
/// seeks work: silent, and hash-bearing.
/// </para>
/// </remarks>
[Table]
public sealed class HouseholdTable
{
    private readonly Rows<Household> _rows;

    /// <param name="capacity">Initial slot count. 360 Households per 1,000 Citizens, per S4 task 2.</param>
    /// <param name="buildings">The table this one's dwelling handles address.</param>
    public HouseholdTable(int capacity, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Household>("household", capacity, Buffering.OneCopy);

        Dwelling = _rows.SavedHandle("dwelling", buildings.Rows);
        LifeStage = _rows.Saved<byte>("life_stage");
        DwellingNext = _rows.Derived<int>("dwelling_next");
        PoolSlot = _rows.Derived<int>("pool_slot");
        MemberHead = _rows.Derived<int>("member_head");
        MemberTail = _rows.Derived<int>("member_tail");
        Money = _rows.Saved<Money>("money", Touch.Cold);
        Savings = _rows.Saved<Money>("savings", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Household> Rows => _rows;

    /// <summary>The Building this Household lives in.</summary>
    public HandleColumn<Building> Dwelling { get; }

    /// <summary>Which of adr/0011's five Life Stages. Resolved through the Ruleset.</summary>
    public Column<byte> LifeStage { get; }

    /// <summary>Link in the dwelling's occupant list.</summary>
    public Column<int> DwellingNext { get; }

    /// <summary>
    /// This Household's position in the Unplaced Pool, <b>plus one</b>; zero means housed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived, and rebuilt from the Pool rather than the other way round</b> — the Pool table is
    /// the saved side, because a member is chosen by position and a position has to survive a reload.
    /// This is the reverse index, and it exists for the same reason <see cref="LotTable.BuildingSlot"/>
    /// does: without it, <em>is this Household in the Pool</em> is a walk, and <c>02 §10</c>'s
    /// staggered tier may only ask <c>O(1)</c> questions.
    /// </para>
    /// <para>
    /// <b>Plus-one encoded because a zeroed row must read as <em>housed</em>.</b> Slots are zero-filled
    /// on growth and on free, so position 0 and <em>no position</em> would otherwise be the same value
    /// — and every Household in a freshly grown table would claim to be at the front of the queue.
    /// </para>
    /// </remarks>
    public Column<int> PoolSlot { get; }

    /// <summary>Head of the member list — see <see cref="CitizenTable.MemberNext"/>.</summary>
    public Column<int> MemberHead { get; }

    /// <summary>Tail of the member list.</summary>
    public Column<int> MemberTail { get; }

    /// <summary>Money on hand.</summary>
    public Column<Money> Money { get; }

    /// <summary>Money set aside.</summary>
    public Column<Money> Savings { get; }

    /// <summary>Whether this Household is in the Unplaced Pool.</summary>
    public bool IsUnplaced(int slot) => PoolSlot[slot] != 0;

    /// <summary>Where in the Pool it is, or <see cref="Rows.NoSlot"/> if it is housed.</summary>
    public int PoolPosition(int slot) => PoolSlot[slot] - 1;

    /// <summary>Records that this Household is now at <paramref name="position"/> in the Pool.</summary>
    public void EnterPool(int slot, int position) => PoolSlot[slot] = position + 1;

    /// <summary>Records that this Household is no longer in the Pool.</summary>
    public void LeavePool(int slot) => PoolSlot[slot] = 0;
}
