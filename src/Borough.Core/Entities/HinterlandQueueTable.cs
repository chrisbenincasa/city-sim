namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// The families waiting outside the gates, one row each.
/// </summary>
/// <remarks>
/// <para>
/// <b>A row here reserves a Household inside its group's stock and transfers nobody</b>
/// (<c>plans/0073</c> D5). The people are still behind the edge and still counted there; what the
/// reservation buys is that the same family cannot be drawn twice, once by the queue it is standing
/// in and once by a fresh occasion. Admission spends the reservation and the stock together;
/// cancelling spends neither.
/// </para>
/// <para>
/// <b>Two lists, both saved, both doubly linked.</b> The admission order is the order the gates
/// serve, and the review order is the order the timed reconsiderations fall due — a family sits in
/// both at once and leaves both together, which is <see cref="LinkedIndexList"/>'s reason for
/// existing. Saved rather than derived because arrival order is recoverable from nothing else: a
/// rebuild would put the queue in slot order and serve a different family after a reload.
/// </para>
/// <para>
/// <b>The composition is read through <see cref="Group"/> and never copied here.</b> Two records of
/// who a family is made of can disagree, and the one on the group row is the one the stock is keyed
/// by. What the row does hold is what a later Tick cannot recompute — the purse this family drew and
/// the identity its preferences come from — so a review is the same family facing a changed city.
/// </para>
/// </remarks>
[Table]
public sealed class HinterlandQueueTable
{
    private readonly Rows<Waiting> _rows;

    /// <param name="capacity">Initial slot count. Empty in a city whose gates keep up.</param>
    /// <param name="groups">The table this one's <see cref="Group"/> handles address.</param>
    public HinterlandQueueTable(int capacity, HinterlandPopulationTable groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        _rows = new Rows<Waiting>("waiting", capacity, Buffering.OneCopy);

        // Required rather than Severable: a group holding a reservation is a group with stock
        // standing in it, and retirement refuses both. A dangling handle here would be a family
        // waiting on an Outside that has forgotten it.
        Group = _rows.SavedHandle("group", groups.Rows, Touch.Cold);

        Purse = _rows.Saved<Money>("purse", Touch.Cold);
        Identity = _rows.Saved<ulong>("identity", Touch.Cold);

        Since = _rows.Saved<Ticks>("since", Touch.Cold);
        Reviewed = _rows.Saved<Ticks>("reviewed", Touch.Cold);
        Compared = _rows.Saved<Ticks>("compared", Touch.Cold);

        AdmitPrevious = _rows.Saved<int>("admit_previous", Touch.Cold);
        AdmitNext = _rows.Saved<int>("admit_next", Touch.Cold);
        ReviewPrevious = _rows.Saved<int>("review_previous", Touch.Cold);
        ReviewNext = _rows.Saved<int>("review_next", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Waiting> Rows => _rows;

    /// <summary>The stock row this family is one Household of, and reserved in.</summary>
    public HandleColumn<HinterlandPopulation> Group { get; }

    /// <summary>What it drew to compare with, and what it will arrive holding.</summary>
    /// <remarks>
    /// <b>Drawn once, at the occasion, and carried unchanged through every review.</b> A purse
    /// redrawn on reconsideration would make waiting a lottery over wealth, and the affordability
    /// filter would then be testing somebody else.
    /// </remarks>
    public Column<Money> Purse { get; }

    /// <summary>Whose tastes the comparison is made with, across the whole of its wait.</summary>
    public Column<ulong> Identity { get; }

    /// <summary>When it joined the queue. The wait is measured from here and never reset.</summary>
    public Column<Ticks> Since { get; }

    /// <summary>When its last scheduled review fell due.</summary>
    public Column<Ticks> Reviewed { get; }

    /// <summary>
    /// The last Tick on which it compared the city with home.
    /// </summary>
    /// <remarks>
    /// <b>One comparison per Tick, whichever path asks for it.</b> A family whose review falls due on
    /// the Tick a gate opens is considered by both, and drawing twice would give it two chances at a
    /// choice the model says it makes once.
    /// </remarks>
    public Column<Ticks> Compared { get; }

    /// <summary>The family ahead of it in the admission order, encoded.</summary>
    public Column<int> AdmitPrevious { get; }

    /// <summary>The family behind it in the admission order, encoded.</summary>
    public Column<int> AdmitNext { get; }

    /// <summary>The family ahead of it in the review order, encoded.</summary>
    public Column<int> ReviewPrevious { get; }

    /// <summary>The family behind it in the review order, encoded.</summary>
    public Column<int> ReviewNext { get; }

    /// <summary>The order an edge's gates serve, oldest first.</summary>
    public LinkedIndexList Admissions(HinterlandTable hinterlands)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        return new LinkedIndexList(
            hinterlands.AdmitHead, hinterlands.AdmitTail, AdmitPrevious, AdmitNext);
    }

    /// <summary>The order an edge's timed reconsiderations fall due, soonest first.</summary>
    public LinkedIndexList Reviews(HinterlandTable hinterlands)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        return new LinkedIndexList(
            hinterlands.ReviewHead, hinterlands.ReviewTail, ReviewPrevious, ReviewNext);
    }

    /// <summary>Puts a willing family at the back of both of its edge's lists.</summary>
    /// <remarks>
    /// ⚠ <b>The caller reserves the stock.</b> The reservation is a count on the group row and this
    /// table does not own it — the same division <see cref="HinterlandPopulationTable.Open"/> has
    /// with the composition index.
    /// </remarks>
    /// <returns>The row's slot.</returns>
    public int Join(
        HinterlandTable hinterlands,
        MapEdge edge,
        Handle<HinterlandPopulation> group,
        Money purse,
        ulong identity,
        Ticks now)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        int slot = _rows.Resolve(_rows.Allocate());
        int owner = HinterlandTable.SlotOf(edge);

        Group[slot] = group;
        Purse[slot] = purse;
        Identity[slot] = identity;
        Since[slot] = now;
        Reviewed[slot] = now;
        Compared[slot] = now;

        Admissions(hinterlands).Append(owner, slot);
        Reviews(hinterlands).Append(owner, slot);

        return slot;
    }

    /// <summary>Takes a family out of both lists and frees its row.</summary>
    /// <remarks>
    /// ⚠ <b>The caller releases the reservation</b>, for <see cref="Join"/>'s reason. Whether the
    /// Household it was holding was admitted or handed back is exactly what this table cannot see.
    /// </remarks>
    public void Leave(HinterlandTable hinterlands, MapEdge edge, int slot)
    {
        ArgumentNullException.ThrowIfNull(hinterlands);

        int owner = HinterlandTable.SlotOf(edge);

        Admissions(hinterlands).Remove(owner, slot);
        Reviews(hinterlands).Remove(owner, slot);

        _rows.Free(_rows.At(slot));
    }
}
