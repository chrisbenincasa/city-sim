namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// The city's own balance sheet: one row, for ever, holding the head of the treasury's Bins.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because a city-wide Bin is one no Building owns.</b> <c>RuleEngine</c> refused
/// <c>Scope.Global</c> on exactly that ground — <em>"where it would live is an entity decision"</em> —
/// and <c>adr/0114</c> is that decision: a Bin's owner is discriminated, and one of the four kinds is
/// the treasury. This table is the row the treasury's Bin list hangs off, and nothing more.
/// </para>
/// <para>
/// <b>One row, allocated in the constructor, on <see cref="RulesetTrailTable"/>'s precedent and for
/// its reason.</b> The treasury exists from world creation, so slot <see cref="Slot"/> means the same
/// thing in a world that has never collected a penny and in one that has collected a million, and
/// nothing that reads this table needs a liveness branch.
/// </para>
/// <para>
/// <b>It holds no balance, and that is <c>adr/0114</c> rather than an omission.</b> Money the treasury
/// holds lives in a Bin — because a Bin is the only thing a Rule can blame and the only thing a
/// blocked Rule can wait on — so a <c>Money</c> column here would be the second copy of a fact,
/// reachable by nothing. What the row carries is the two ends of an intrusive list, which is the one
/// thing a Bin's owner has to supply.
/// </para>
/// <para>
/// ⚠ <b>Both are <see cref="Disposition.Derived"/>, matching <see cref="BuildingTable.BinHead"/>.</b>
/// Membership is a pure function of <see cref="BinTable.OwnerKind"/> and the order carries no meaning,
/// so the list is an index over saved truth rather than truth itself. Saving it would put a second,
/// hashable spelling of the same fact in the file.
/// </para>
/// </remarks>
[Table]
public sealed class TreasuryTable
{
    /// <summary>The row the treasury lives at, for the life of the world.</summary>
    public const int Slot = 0;

    private readonly Rows<Treasury> _rows;

    /// <summary>Builds the table and allocates its one row.</summary>
    public TreasuryTable()
    {
        _rows = new Rows<Treasury>("treasury", 1, Buffering.OneCopy);

        BinHead = _rows.Derived<int>("bin_head", Touch.PerTick);
        BinTail = _rows.Derived<int>("bin_tail");

        _rows.Seal();

        _rows.Allocate();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Treasury> Rows => _rows;

    /// <summary>Head of the treasury's Bins.</summary>
    public Column<int> BinHead { get; }

    /// <summary>Tail of the treasury's Bins, so a new one appends rather than push-fronts.</summary>
    public Column<int> BinTail { get; }
}
