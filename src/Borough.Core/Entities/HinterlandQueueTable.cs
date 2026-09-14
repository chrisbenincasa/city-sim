namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// The families waiting outside the gates, one row each.
/// </summary>
/// <remarks>
/// Each row reserves one Household in its composition without moving people or Money.
/// Admission and review lists are saved separately: slot order cannot reconstruct join order.
/// The caller releases the reservation when the row leaves either list.
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

        // A queued reservation requires its composition row to stay live.
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
    /// Retained unchanged through every review and admission.
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
    /// At most one comparison per Tick, whether triggered by a scheduled review or an open gate.
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
    /// The caller reserves one Household in the composition before joining.
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
    /// The caller must spend or release the reservation before removing the queue row.
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
