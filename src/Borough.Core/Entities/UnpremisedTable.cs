namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The unpremised pool: the Businesses currently looking for premises.
/// </summary>
/// <remarks>
/// <para>
/// <b>The Unplaced Pool's sibling, built to
/// <see href="../../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md">adr/0142</see>'s
/// rule that the bound goes in on the day the collection does.</b> A Business orphaned by
/// <see cref="World.DestroyBuilding"/> joins here, waits, and — if nothing tenants it — leaves the
/// city through <see cref="World.Depart(Handle{Business})"/> with its money, which is subtracted from
/// <c>MoneySupply.Issued</c>. ***The money is neither destroyed nor confiscated; it is exported.***
/// </para>
/// <para>
/// 🔴 ⚠ <b>IT SHIPS WITH ONE EXIT AND THAT EXIT IS THE SINK.</b> Nothing tenants a Business, because
/// nothing <em>creates</em> one — <c>World.CreateBusiness</c> has no <c>src/</c> caller and milestone
/// <b>27 task 8</b> is the first pass that would. So a Business that enters this pool today leaves it
/// only by giving up. ***That is the collection being bounded, not the mechanism being finished***:
/// <c>adr/0142</c>'s *"a pool plus a placement pass is what not auto-tenanting looks like"* describes
/// two halves and this is the one <c>adr/0006</c> requires first. The placement half is <b>unbuilt</b>
/// (<c>adr/0070</c>), and the day it lands it draws from the same sample this pool is already swept
/// by.
/// </para>
/// <para>
/// <b>TWO columns, against the Unplaced Pool's four, and each absence is a decision.</b>
/// <c>Gate</c> is absent because a Business has no arrival door: a Household carries a balance in
/// from its Hinterland's band, and what capitalises a Business is <em>unanswered</em> and owed a
/// ratifier (<c>plans/0002</c> §D2, milestone 27 task 8). ***A column meaningless for every one of its
/// rows is worse than one meaningless for half of them***, which is the argument <see cref="UnplacedTable"/>
/// already makes against a lifetime column, one step further along.
/// </para>
/// <para>
/// <b><c>Considered</c> is absent for that column's OWN stated reason, used in reverse.</b> It counts
/// premises actually looked at, and nothing looks at premises on a Business's behalf — so the counter
/// would read identically zero in every world, for ever. Its remark on the Unplaced Pool is that a
/// counter which cannot read zero in its headline case is measuring the mechanism rather than the
/// city; ***a counter that can read NOTHING BUT zero is not measuring at all.*** It arrives with the
/// placement pass that gives it something to count.
/// </para>
/// <para>
/// <b>Density, the swap-with-last in <see cref="Leave"/>, the LIFO dependency in <see cref="Join"/>,
/// and slot-is-not-an-identity all hold here exactly as they hold on <see cref="UnplacedTable"/></b>,
/// for the same reasons and with the same consequences if broken. That table's remarks are the
/// argument; this one does not restate them, because ***a second copy of a rationale is the copy that
/// drifts***.
/// </para>
/// </remarks>
[Table]
public sealed class UnpremisedTable
{
    private readonly Rows<Unpremised> _rows;

    /// <param name="capacity">Initial slot count. The pool is empty in a healthy city.</param>
    /// <param name="businesses">The table this one's <see cref="Business"/> handles address.</param>
    public UnpremisedTable(int capacity, BusinessTable businesses)
    {
        ArgumentNullException.ThrowIfNull(businesses);

        _rows = new Rows<Unpremised>("unpremised", capacity, Buffering.OneCopy);

        Business = _rows.SavedHandle("business", businesses.Rows);
        Since = _rows.Saved<int>("since");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Unpremised> Rows => _rows;

    /// <summary>The Business seeking premises.</summary>
    public HandleColumn<Business> Business { get; }

    /// <summary>
    /// The Tick this spell in the pool began, which is what the give-up bound is measured from.
    /// </summary>
    /// <remarks>
    /// <b>A Tick rather than a count of occasions</b>, which is <c>adr/0130</c>'s title —
    /// the bound is a <em>duration</em> — and the argument is
    /// <see cref="UnplacedTable.Since"/>'s in full: a bound whose clock only advances when you are
    /// drawn is not a bound. ⚠ <b>It is per spell rather than per Business</b>, so a Business
    /// tenanted, later orphaned and back here starts its clock again.
    /// </remarks>
    public Column<int> Since { get; }

    /// <summary>How many Businesses are in the pool, and the exclusive bound on a position.</summary>
    /// <remarks>
    /// ⚠ <b>This is NOT a demand signal and must not be read as one.</b> The Unplaced Pool's count is
    /// <c>CONTEXT</c> → Unplaced Pool's replacement for the RCI meter — <em>somebody would take
    /// this</em> — and a Zone Rule's create predicate reads it. ***Nothing reads this one***, and a
    /// Zone Rule that did would be building shops because shops had been demolished.
    /// </remarks>
    public int Count => _rows.LiveCount;

    /// <summary>The Business at <paramref name="position"/> in the pool.</summary>
    public Handle<Business> At(int position) => Business[position];

    /// <summary>Adds a Business to the pool, and returns where it landed.</summary>
    /// <param name="businesses">The Business table, so the membership and its reverse index are written together.</param>
    /// <param name="business">The Business joining.</param>
    /// <param name="now">The Tick the spell begins.</param>
    public int Join(BusinessTable businesses, Handle<Business> business, Ticks now)
    {
        ArgumentNullException.ThrowIfNull(businesses);

        Handle<Unpremised> row = _rows.Allocate();
        int position = _rows.Resolve(row);

        Business[position] = business;

        // The spell's clock starts here and not at the Business's founding: a shop orphaned years
        // into a run has not been looking for years.
        Since[position] = (int)now.Raw;

        businesses.EnterPool(businesses.Rows.Resolve(business), position);

        return position;
    }

    /// <summary>
    /// Removes the member at <paramref name="position"/> and returns it, keeping the pool dense.
    /// </summary>
    public Handle<Business> Leave(BusinessTable businesses, int position)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, Count);

        int last = Count - 1;
        Handle<Business> leaving = Business[position];
        Handle<Business> moved = Business[last];

        // Every column moves together, and Since is the one that bites: inheriting an older spell's
        // clock makes a Business give up for somebody else's waiting, and nothing about the result
        // looks wrong. UnplacedTable.Leave's remark, which is where the reasoning lives.
        Business[position] = moved;
        Since[position] = Since[last];
        _rows.Free(_rows.At(last));

        // Clear the leaver first, then re-point the mover: reversed, a Business leaving from the last
        // position -- where it *is* the mover -- would clear the entry it had just been given.
        businesses.LeavePool(businesses.Rows.Resolve(leaving));

        if (position != last)
        {
            businesses.EnterPool(businesses.Rows.Resolve(moved), position);
        }

        return leaving;
    }
}
