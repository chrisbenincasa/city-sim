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
/// 🔴 ⚠ <b>IT HAS TWO EXITS AND ONLY ONE OF THEM IS GUARANTEED, and this paragraph said it had one
/// until 2026-08-26.</b> It read <em>"IT SHIPS WITH ONE EXIT AND THAT EXIT IS THE SINK. Nothing
/// tenants a Business, because nothing creates one — <c>World.CreateBusiness</c> has no <c>src/</c>
/// caller"</em>, which was true when it was written and was falsified by milestone 27's placement pass
/// (<c>adr/0147</c>) and founding channel (<c>adr/0145</c>). <c>CreateBusiness</c> has **two**
/// production callers and <see cref="Rules.PlacementEngine"/> premises pool members into any standing
/// Building with room. ⚠ <b>It is <c>adr/0093</c>'s shape exactly — right about where to look, wrong
/// about the trigger</b>, and <c>plans/0012</c> is where it was filed.
/// </para>
/// <para>
/// 🔴 <b>WHY THAT ONE MATTERED MORE THAN A STALE COMMENT: it named the wrong sink.</b> A reader sizing
/// <c>adr/0006</c> off it would conclude the pool drains only by emigration, and ***in every world that
/// exists the exit it denied is the only one that has ever fired*** — 7,165 premisings against
/// <b>zero</b> give-ups over 131,072 Ticks on <c>rulesets/founded.toml</c>. <b>The bound is still
/// required and is not weakened by this</b>: tenanting drains the pool only while vacant premises
/// exist, so it is the city cooperating rather than a bound, and <c>adr/0142</c>'s give-up is the exit
/// that holds when it does not.
/// </para>
/// <para>
/// 🔴 <b>THREE columns, against the Unplaced Pool's four, and this paragraph counted TWO and named the
/// wrong absence until 2026-08-26.</b> It said <em>"<c>Gate</c> is absent because a Business has no
/// arrival door"</em> — and <see cref="Gate"/> is right there, allocated by this constructor, holding
/// the gate a Business arrived through or <c>default</c> if it was founded here. ***A doc comment that
/// enumerates a table's columns is a second copy of the constructor***, which is the thing
/// <c>CLAUDE.md</c> warns about arriving inside one file, and it is why this now describes what each
/// column is FOR rather than counting them.
/// </para>
/// <para>
/// <b>The one genuine absence is <c>Considered</c>, and its stated reason has expired.</b> It was
/// absent because <em>"nothing looks at premises on a Business's behalf — so the counter would read
/// identically zero in every world, for ever"</em>. <c>PlacementEngine.Tenant</c> now looks at
/// <c>[placement] candidates</c> premises per seeker, so the counter would read something. ⚠ <b>The
/// column is still not here, and that is now an open question rather than a settled absence</b> —
/// <c>plans/0002</c> §B. ***An absence whose argument has expired is not the same as an absence
/// somebody re-decided***, and recording which of the two this is was the whole of the debt.
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
    /// <param name="buildings">The table this one's <see cref="Gate"/> handles address.</param>
    public UnpremisedTable(int capacity, BusinessTable businesses, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(businesses);
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Unpremised>("unpremised", capacity, Buffering.OneCopy);

        Business = _rows.SavedHandle("business", businesses.Rows);
        Gate = _rows.SavedHandle("gate", buildings.Rows);
        Since = _rows.Saved<int>("since");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Unpremised> Rows => _rows;

    /// <summary>The Business seeking premises.</summary>
    public HandleColumn<Business> Business { get; }

    /// <summary>The gate this Business arrived through, or <c>default</c> if it was founded here.</summary>
    /// <remarks>
    /// <para>
    /// <b>Declared 2026-08-24 by <c>adr/0145</c>, and this table's own argument is what retired its
    /// absence.</b> It was left out because a Business had no arrival door, on the ground that ***a
    /// column meaningless for every one of its rows is worse than one meaningless for half of them***.
    /// <c>adr/0145</c> gives a Business <b>two</b> ways in — founded by a Household, or arriving
    /// through a gate — so it is now meaningful for half, which is the standard this table already
    /// holds <see cref="UnplacedTable.Gate"/> to. ***The absence was right when it was written and its
    /// stated reason is what ends it.***
    /// </para>
    /// <para>
    /// <b>A default handle is the founded case and is not a hole</b>, exactly as it is on the Unplaced
    /// Pool: a founder is inside the city and came from no gate at all. ⚠ <b>It belongs to the SPELL
    /// and not to the Business</b>, which is <see cref="Since"/>'s property and is stated there — so a
    /// Business that arrived through a gate, was tenanted, and was later orphaned begins its second
    /// spell reading <c>default</c>, because *this* spell started at no gate. ***The column answers
    /// where this search began, never where this Business came from.***
    /// </para>
    /// <para>
    /// <b>Nothing on <see cref="Entities.Business"/> records the channel</b>, and that is deliberate:
    /// once it is in the pool a founded shop and an immigrant shop are the same kind of tenant looking
    /// for the same premises, and <see cref="World.Place"/>'s commercial half will treat them
    /// identically.
    /// </para>
    /// </remarks>
    public HandleColumn<Building> Gate { get; }

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
    /// <param name="gate">The gate it arrived through, or <c>default</c> if a Household founded it.</param>
    /// <param name="now">The Tick the spell begins.</param>
    public int Join(
        BusinessTable businesses, Handle<Business> business, Handle<Building> gate, Ticks now)
    {
        ArgumentNullException.ThrowIfNull(businesses);

        Handle<Unpremised> row = _rows.Allocate();
        int position = _rows.Resolve(row);

        Business[position] = business;

        // Default for a founded Business, which is the ordinary case rather than a hole -- see Gate.
        Gate[position] = gate;

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
