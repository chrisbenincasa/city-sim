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

    /// <summary>Head of the member list — see <see cref="CitizenTable.MemberNext"/>.</summary>
    public Column<int> MemberHead { get; }

    /// <summary>Tail of the member list.</summary>
    public Column<int> MemberTail { get; }

    /// <summary>Money on hand.</summary>
    public Column<Money> Money { get; }

    /// <summary>Money set aside.</summary>
    public Column<Money> Savings { get; }
}
