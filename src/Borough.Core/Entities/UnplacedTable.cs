namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The Unplaced Pool: the Households currently seeking housing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four columns, and each one arrived when a mechanism read it.</b> `CONTEXT` → Unplaced Pool
/// describes four entry routes, recorded refusal reasons, a give-up counter and a Departure; this
/// table held a membership and nothing else until milestone 11 task 4, on <c>adr/0054</c>'s rule that
/// naming the rest before there is a reader is the trespass. <see cref="Gate"/> came with the arrival
/// door at task 4; <see cref="Since"/> and <see cref="Considered"/> came with the Departure at task 7.
/// <b>The refusal reasons still have no reader and are milestone 16's</b>, with the comparison
/// (<c>adr/0128</c>) — <c>PlacementEngine</c> never refuses anything, it fails to find room, so a
/// refusal count would be identically zero.
/// </para>
/// <para>
/// <b><see cref="Gate"/> is on the membership rather than on the Household, and the placement is
/// forced rather than chosen</b> (<see href="../../docs/adr/0129-the-pool-waits-at-the-gate-and-an-arrivals-trip-is-the-move-in.md">adr/0129</see>).
/// It is known at arrival, because that is where throughput binds; it is needed again at placement,
/// as the move-in Trip's origin; and the interval between the two is exactly one spell in the Pool.
/// ⚠ <b>A lifetime column on the Household was considered and is wrong</b>: two of the Pool's four
/// entry routes have no gate at all — a Household the city generated itself when a Mature Family's
/// children left home, and one evicted when its Building was demolished. ***A column that is
/// meaningless for half its rows is a column describing something else.***
/// </para>
/// <para>
/// <b>It is a table rather than a list threaded through the Households, and the reason is save and
/// reload.</b> The obvious cheaper shape is an intrusive list rebuilt in
/// <see cref="World.RebuildDerived"/> from <em>which Households have no dwelling</em> — no new table,
/// no new saved state. But a member is chosen from the Pool by <em>position</em>, and a rebuilt list
/// is in slot order while a live one is in arrival order, so the same save would rehouse a different
/// Household after a reload. That is a hash divergence the save/reload test would catch and nothing
/// else would, and it is cheaper to not have it than to detect it.
/// </para>
/// <para>
/// <b>Live rows are dense — always <c>[0, Count)</c> — which is what makes an unbiased draw
/// <c>O(1)</c>.</b> That is not a property <see cref="Rows{T}"/> gives for free; it is bought by
/// <see cref="Leave"/> moving the last member into the vacated position and freeing the last slot
/// instead of the middle one. The free list is LIFO, so the slot freed is the one the next
/// <see cref="Join"/> takes back, and the live range never develops a hole. An invariant asserts it
/// rather than this paragraph being trusted.
/// </para>
/// <para>
/// <b>A row's slot is therefore not an identity, and no handle to one is ever issued.</b> Membership
/// moves between slots as the Pool churns, so a stored <c>Handle&lt;Unplaced&gt;</c> would name
/// whichever Household happened to be swapped into that position — the recycled-index defect
/// <c>02 §8</c> rule 5 describes, with the recycling done deliberately. The Household handle in the
/// column is the identity, and it is the never-reused kind.
/// </para>
/// </remarks>
[Table]
public sealed class UnplacedTable
{
    private readonly Rows<Unplaced> _rows;

    /// <param name="capacity">Initial slot count. The Pool is empty in a healthy city.</param>
    /// <param name="households">The table this one's <see cref="Household"/> handles address.</param>
    /// <param name="buildings">The table this one's <see cref="Gate"/> handles address.</param>
    public UnplacedTable(int capacity, HouseholdTable households, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(households);
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Unplaced>("unplaced", capacity, Buffering.OneCopy);

        Household = _rows.SavedHandle("household", households.Rows);
        Gate = _rows.SavedHandle("gate", buildings.Rows);
        Since = _rows.Saved<int>("since");
        Considered = _rows.Saved<int>("considered");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Unplaced> Rows => _rows;

    /// <summary>The Household seeking housing.</summary>
    public HandleColumn<Household> Household { get; }

    /// <summary>
    /// The Outside Connection this member arrived at, or a default handle for the three entry routes
    /// that have no gate.
    /// </summary>
    /// <remarks>
    /// <b>A default handle is the ordinary case and not a hole.</b> A Household evicted by a
    /// demolition, one the city generated itself, and one that decided to move all enter the Pool
    /// through <see cref="World.Unplace"/> and came from nowhere outside — so the column reads
    /// <c>default</c> and <see cref="World.Place"/> gives their move-in no gate to start from. Only
    /// <see cref="World.Arrive"/> writes a live handle here.
    /// </remarks>
    public HandleColumn<Building> Gate { get; }

    /// <summary>
    /// The Tick this spell in the Pool began, which is what the give-up bound is measured from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A Tick rather than a count of occasions, and <c>adr/0130</c>'s title is the reason</b> —
    /// the Pool's bound is a <em>duration</em>. The engine derives how many looks that buys from
    /// <see cref="Rules.PlacementRuleset.RevisitTicks"/>; nothing bounds on the derived number.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>Bounding on occasions would have put the sink behind a random draw.</b>
    /// <c>PlacementEngine</c> draws its sample rather than sweeping the Pool, so a Household that
    /// happens not to be picked accrues no occasions — and under an occasion bound it would wait for
    /// ever, in a Pool that is growing, which is precisely the
    /// <see href="../../docs/adr/0006-no-collection-grows-with-elapsed-time.md">adr/0006</see> hole
    /// this column exists to close. ***A bound whose clock only advances when you are lucky is not a
    /// bound.***
    /// </para>
    /// <para>
    /// <b>It is per spell rather than per Household</b>, which follows from the column being on the
    /// membership: a Household housed, later evicted and back in the Pool starts its clock again.
    /// That is the right reading — it is looking again, not still looking — and it is the same reason
    /// <see cref="Gate"/> is here rather than on the Household.
    /// </para>
    /// </remarks>
    public Column<int> Since { get; }

    /// <summary>
    /// How many real dwellings this member has looked at during this spell. <b>Evidence, and not the
    /// bound.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>00-vision.md</c>'s flagship Evidence line is two numbers and this is the first of
    /// them</b> — <i>"Considered 20 dwellings over 4 months"</i>. One bounds and one describes, and
    /// the describing one is what tells a Household that saw plenty and took none from a Household
    /// that was offered nothing at all.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>It counts dwellings, not looks.</b> A candidate draw that lands on a vacant Lot
    /// considered nothing, and counting it would make this read as *the city showed them twenty
    /// homes* in a city with no homes — which is the exact failure the unhoused channel exists to
    /// diagnose, reported as its own opposite. ***A counter that cannot read zero in its headline
    /// case is measuring the mechanism rather than the city.***
    /// </para>
    /// </remarks>
    public Column<int> Considered { get; }

    /// <summary>
    /// How many Households are in the Pool, which is also the exclusive bound on a position.
    /// </summary>
    /// <remarks>
    /// <b>This is the demand signal</b>, and `CONTEXT` → Unplaced Pool is explicit that it replaces
    /// the RCI meter other city builders carry. A Zone Rule's create predicate reads it as
    /// <em>somebody would take this</em>; it is deliberately not a scalar anything can set.
    /// </remarks>
    public int Count => _rows.LiveCount;

    /// <summary>The Household at <paramref name="position"/> in the Pool.</summary>
    public Handle<Household> At(int position) => Household[position];

    /// <summary>The gate the member at <paramref name="position"/> arrived at, if any.</summary>
    public Handle<Building> GateAt(int position) => Gate[position];

    /// <summary>Adds a Household to the Pool, and returns where it landed.</summary>
    /// <remarks>
    /// <para>
    /// <b>It appends, and that is inherited rather than enforced here.</b> Density needs the allocator
    /// to hand back exactly <c>Count - 1</c>, which holds because <see cref="Rows{T}"/>'s free list is
    /// <b>LIFO</b> and <see cref="Leave"/> only ever frees the last slot: the frees run downwards from
    /// the high-water mark, so the pops run back upwards through the same slots. <b>Nothing in
    /// <c>Rows</c> promises LIFO</b> — it is an implementation detail this table depends on — which is
    /// why the position is returned rather than discarded, and why <see cref="World.Unplace"/> checks
    /// it at the write site. A bitmap or FIFO allocator would break density silently, and the
    /// whole-world check would not see it until the end of the run.
    /// </para>
    /// <para>
    /// <b>The Household table arrives as a parameter rather than as a field</b>, which is task 2's
    /// pattern and the same constraint behind it: a <c>[Table]</c> type may hold only its declared
    /// Columns and its own <c>Rows</c> (<c>BOR0901</c>). The effect is the one that matters — the two
    /// halves of the relation are written by one call, so there is no door through which a membership
    /// and its reverse index can disagree.
    /// </para>
    /// </remarks>
    /// <param name="households">The Household table, so the membership and its reverse index are written together.</param>
    /// <param name="household">The Household joining.</param>
    /// <param name="gate">
    /// The Outside Connection it arrived at, or a default handle. <b>Required rather than defaulted,
    /// because three of the four entry routes have no gate and the fourth must not be able to forget
    /// one</b> — a defaulted parameter makes the gateless case the one you get by writing nothing,
    /// which is exactly the caller <c>adr/0129</c> needs to be explicit.
    /// </param>
    public int Join(
        HouseholdTable households, Handle<Household> household, Handle<Building> gate, Ticks now)
    {
        ArgumentNullException.ThrowIfNull(households);

        Handle<Unplaced> row = _rows.Allocate();
        int position = _rows.Resolve(row);

        Household[position] = household;
        Gate[position] = gate;

        // The spell's clock starts here and not at the Household's founding: a Household evicted
        // years into a run has not been looking for years.
        Since[position] = (int)now.Raw;
        Considered[position] = 0;

        households.EnterPool(households.Rows.Resolve(household), position);

        return position;
    }

    /// <summary>
    /// Removes the member at <paramref name="position"/> and returns it, keeping the Pool dense.
    /// </summary>
    /// <remarks>
    /// <b>The last member is moved into the hole and the last slot is freed</b>, which is the whole
    /// of the density argument. Freeing <paramref name="position"/> directly would leave a dead slot
    /// inside the live range, and a draw over <see cref="Count"/> would then name it — so a Lot that
    /// qualified would silently fail to be built on, at a rate set by how much the Pool had churned.
    /// That defect looks exactly like a city that grows slowly, which is the family
    /// <c>ZoneSampleTests</c> was written against.
    /// </remarks>
    public Handle<Household> Leave(HouseholdTable households, int position)
    {
        ArgumentNullException.ThrowIfNull(households);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, Count);

        int last = Count - 1;
        Handle<Household> leaving = Household[position];
        Handle<Household> moved = Household[last];

        // EVERY column moves together, and the count is why this comment names no number: a swap
        // that carried the membership and left one column behind gives the moved Household somebody
        // else's history. The gate case is the loudest -- its move-in Trip would start at a gate it
        // never came through, and every such Trip is a legitimate-looking journey between two real
        // Addresses -- but Since is the dangerous one, because inheriting an older spell's clock
        // makes a Household give up for somebody else's waiting, and nothing about the result looks
        // wrong. That is this table's own slot-is-not-an-identity remark arriving once per column.
        Household[position] = moved;
        Gate[position] = Gate[last];
        Since[position] = Since[last];
        Considered[position] = Considered[last];
        _rows.Free(_rows.At(last));

        // Clear the leaver first, then re-point the mover. Reversed, a Household leaving from the
        // last position — where it *is* the mover — would clear the entry it had just been given.
        households.LeavePool(households.Rows.Resolve(leaving));

        if (position != last)
        {
            households.EnterPool(households.Rows.Resolve(moved), position);
        }

        return leaving;
    }
}
