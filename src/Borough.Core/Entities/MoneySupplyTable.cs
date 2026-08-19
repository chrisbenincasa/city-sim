namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// How much money has been issued into this world: one row, for ever, and the anchor
/// <c>adr/0031</c>'s conservation invariant is an equality against.
/// </summary>
/// <remarks>
/// <para>
/// <b>Conservation needs a second quantity, and this is it.</b> <em>Nothing is created or destroyed
/// except at the gate</em> cannot be checked from a snapshot — a sum of every balance in the city is
/// just as consistent after a Household has been freed with its money in it as before. What makes the
/// claim checkable is a total that moves only where money is allowed to enter, held apart from the
/// balances, so that the check compares two things that were arrived at differently. Summing the
/// balances to produce the anchor would be the failure milestone 10 task 1 found: an invariant that
/// recomputes the producer's own expression checks that the write happened and never what was written.
/// </para>
/// <para>
/// <b>One row and no owner, because the supply is a property of the world.</b> The
/// <see cref="ClockTable"/> shape, for that table's stated reason: storage beside the columns is what
/// <c>BOR0901</c> is an error for, and a value that must survive a save has to be in the field
/// declaration or the State Hash has a coverage hole. <see cref="Rows.Saved{T}"/> puts it in both.
/// </para>
/// <para>
/// ⚠ <b>It is not the treasury's balance and it does not live on <see cref="TreasuryTable"/>.</b>
/// <c>01 §6</c> separates them by name where it prices a trade deficit — <em>"a different bill — the
/// money supply, not the treasury"</em> — and the separation is structural rather than editorial: at
/// the founding the treasury holds <b>nothing</b> (<c>adr/0116</c>) and the whole supply is in
/// Households. A column here also has to be <see cref="Disposition.Saved"/>, and
/// <c>World._tables</c>'s own remark records why <see cref="TreasuryTable"/> is kept out of the hash
/// composition: both of its columns are derived, and 5a found that a wholly-derived table folds its
/// allocation history rather than any state.
/// </para>
/// <para>
/// <b>It is not the founding balance either</b>, which <c>adr/0116</c> leaves open as a ratio this
/// milestone holds neither side of. This column records what a world <em>was</em> given, whatever that
/// turns out to be; it chooses nothing, so it is not a hash-bearing number under <c>adr/0052</c> and
/// has no ratifier to name.
/// </para>
/// </remarks>
[Table]
public sealed class MoneySupplyTable
{
    /// <summary>The row the supply lives at, for the life of the world.</summary>
    public const int Slot = 0;

    private readonly Rows<MoneySupply> _rows;

    /// <summary>Builds the supply at zero, which is what a world nobody has endowed holds.</summary>
    public MoneySupplyTable()
    {
        _rows = new Rows<MoneySupply>("money_supply", 1, Buffering.OneCopy);

        Issued = _rows.Saved<Money>("issued", Touch.Cold);

        _rows.Seal();

        // One row for the life of the world, never freed -- ClockTable's reason for using the
        // allocator rather than bypassing it: the row count and the columns have to agree.
        _rows.Allocate();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<MoneySupply> Rows => _rows;

    /// <summary>
    /// Money that has entered this world, net of anything that has left it.
    /// </summary>
    /// <remarks>
    /// <b>Written by <see cref="World.Endow"/> and by nothing else today</b>, which is what makes it
    /// an anchor rather than a second copy of the balances. Milestone <b>11</b> gives it its second
    /// writer — the Outside Connection is money's only source and sink (<c>CONTEXT.md</c> → Money), so
    /// the gate moves this and the invariant is unchanged. Until then it moves at the founding alone,
    /// and the check is an exact equality rather than a sum with a flow term.
    /// </remarks>
    public Column<Money> Issued { get; }
}
