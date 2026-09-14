namespace Borough.Core.Entities;

using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// One row per map edge: the population standing behind it, as a total and as a set of flows.
/// </summary>
/// <remarks>
/// Four saved rows hold edge-wide flows, sequences and queue heads. Gate and composition
/// lists are derived; lifetime counters survive retirement of the groups that generated them.
/// </remarks>
[Table]
public sealed class HinterlandTable
{
    /// <summary>How many edges a bounded map has, and therefore how many rows this table holds.</summary>
    public const int Edges = 4;

    private readonly Rows<Hinterland> _rows;

    /// <summary>Builds the four rows and stamps each with its edge.</summary>
    public HinterlandTable(HinterlandPopulationTable groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        _rows = new Rows<Hinterland>("hinterland", Edges, Buffering.OneCopy);

        Edge = _rows.Saved<byte>("edge", Touch.Cold);
        GroupHead = _rows.Derived<int>("group_head", Touch.Cold);
        GroupTail = _rows.Derived<int>("group_tail", Touch.Cold);

        ReplenishedHouseholds = _rows.Saved<long>("replenished_households", Touch.Cold);
        ReplenishedPeople = _rows.Saved<long>("replenished_people", Touch.Cold);
        ReturnedHouseholds = _rows.Saved<long>("returned_households", Touch.Cold);
        ReturnedPeople = _rows.Saved<long>("returned_people", Touch.Cold);
        AdmittedHouseholds = _rows.Saved<long>("admitted_households", Touch.Cold);
        AdmittedPeople = _rows.Saved<long>("admitted_people", Touch.Cold);
        TurnoverHouseholds = _rows.Saved<long>("turnover_households", Touch.Cold);
        TurnoverPeople = _rows.Saved<long>("turnover_people", Touch.Cold);

        Sequence = _rows.Saved<ulong>("sequence", Touch.Cold);
        LastGate = _rows.Saved<ulong>("last_gate", Touch.Cold);

        // Severable, because the group it names can be retired under it. The cursor is advanced past
        // a group being freed, and a handle that outlived one anyway names a row nobody has to find.
        GroupCursor =
            _rows.SavedHandle("group_cursor", groups.Rows, Touch.Cold, Reference.Severable);

        AdmitHead = _rows.Saved<int>("admit_head", Touch.Cold);
        AdmitTail = _rows.Saved<int>("admit_tail", Touch.Cold);
        ReviewHead = _rows.Saved<int>("review_head", Touch.Cold);
        ReviewTail = _rows.Saved<int>("review_tail", Touch.Cold);

        GateHead = _rows.Derived<int>("gate_head", Touch.Cold);
        GateTail = _rows.Derived<int>("gate_tail", Touch.Cold);

        FlowDay = _rows.Saved<int>("flow_day", Touch.Cold);

        OccasionsToday = _rows.Saved<int>("occasions_today", Touch.Cold);
        OccasionsYesterday = _rows.Saved<int>("occasions_yesterday", Touch.Cold);
        RequestedToday = _rows.Saved<int>("requested_today", Touch.Cold);
        RequestedYesterday = _rows.Saved<int>("requested_yesterday", Touch.Cold);
        NoConnectionToday = _rows.Saved<int>("no_connection_today", Touch.Cold);
        NoConnectionYesterday = _rows.Saved<int>("no_connection_yesterday", Touch.Cold);
        NoSampleToday = _rows.Saved<int>("no_sample_today", Touch.Cold);
        NoSampleYesterday = _rows.Saved<int>("no_sample_yesterday", Touch.Cold);
        StayedOutsideToday = _rows.Saved<int>("stayed_outside_today", Touch.Cold);
        StayedOutsideYesterday = _rows.Saved<int>("stayed_outside_yesterday", Touch.Cold);
        WillingToday = _rows.Saved<int>("willing_today", Touch.Cold);
        WillingYesterday = _rows.Saved<int>("willing_yesterday", Touch.Cold);
        AdmittedToday = _rows.Saved<int>("admitted_today", Touch.Cold);
        AdmittedYesterday = _rows.Saved<int>("admitted_yesterday", Touch.Cold);
        QueuedToday = _rows.Saved<int>("queued_today", Touch.Cold);
        QueuedYesterday = _rows.Saved<int>("queued_yesterday", Touch.Cold);
        ExpiredToday = _rows.Saved<int>("expired_today", Touch.Cold);
        ExpiredYesterday = _rows.Saved<int>("expired_yesterday", Touch.Cold);
        ReviewedToday = _rows.Saved<int>("reviewed_today", Touch.Cold);
        ReviewedYesterday = _rows.Saved<int>("reviewed_yesterday", Touch.Cold);
        ChangedMindToday = _rows.Saved<int>("changed_mind_today", Touch.Cold);
        ChangedMindYesterday = _rows.Saved<int>("changed_mind_yesterday", Touch.Cold);
        ConnectionLostToday = _rows.Saved<int>("connection_lost_today", Touch.Cold);
        ConnectionLostYesterday = _rows.Saved<int>("connection_lost_yesterday", Touch.Cold);
        ReplenishedToday = _rows.Saved<int>("replenished_today", Touch.Cold);
        ReplenishedYesterday = _rows.Saved<int>("replenished_yesterday", Touch.Cold);
        TurnoverToday = _rows.Saved<int>("turnover_today", Touch.Cold);
        TurnoverYesterday = _rows.Saved<int>("turnover_yesterday", Touch.Cold);

        _rows.Seal();

        // MoneySupplyTable's line and its reason: the row count and the columns have to agree, so the
        // fixed rows go through the allocator rather than around it.
        for (int slot = 0; slot < Edges; slot++)
        {
            _rows.Allocate();
            Edge[slot] = (byte)EdgeAt(slot);
        }
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Hinterland> Rows => _rows;

    /// <summary>Which edge this row is. Never <see cref="MapEdge.None"/>.</summary>
    public Column<byte> Edge { get; }

    /// <summary>The first composition standing behind this edge, in ascending slot order.</summary>
    /// <remarks>
    /// The derived composition list is in slot order; GroupCursor rotates its starting point.
    /// </remarks>
    public Column<int> GroupHead { get; }

    /// <summary>The last composition standing behind this edge.</summary>
    public Column<int> GroupTail { get; }

    /// <summary>Households the Outside has added here, over the life of the world.</summary>
    public Column<long> ReplenishedHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> ReplenishedPeople { get; }

    /// <summary>Households that have left the city for here, over the life of the world.</summary>
    public Column<long> ReturnedHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> ReturnedPeople { get; }

    /// <summary>Households this edge has sent into the city, over the life of the world.</summary>
    public Column<long> AdmittedHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> AdmittedPeople { get; }

    /// <summary>
    /// Households the Outside has dropped from here because it held more than it rests at.
    /// </summary>
    /// <remarks>
    /// Counts external population turnover, separately from city deaths and departures.
    /// </remarks>
    public Column<long> TurnoverHouseholds { get; }

    /// <summary>People those Households hold.</summary>
    public Column<long> TurnoverPeople { get; }

    /// <summary>
    /// How many families this edge has ever presented. The next one's identity comes off it.
    /// </summary>
    /// <remarks>
    /// Monotonic per-edge prospect sequence; increment with overflow checking before drawing identity.
    /// </remarks>
    public Column<ulong> Sequence { get; }

    /// <summary>The monotonic id of the gate that last admitted somebody here.</summary>
    /// <remarks>
    /// A monotonic Building id, so recycling a gate slot cannot move the admission cursor.
    /// </remarks>
    public Column<ulong> LastGate { get; }

    /// <summary>Where the next Tick's walk over this edge's compositions starts.</summary>
    /// <remarks>
    /// Severable: an expired cursor restarts at the edge list head.
    /// </remarks>
    public HandleColumn<HinterlandPopulation> GroupCursor { get; }

    /// <summary>The first family waiting for room here. Served oldest first.</summary>
    public Column<int> AdmitHead { get; }

    /// <summary>The last family waiting for room here.</summary>
    public Column<int> AdmitTail { get; }

    /// <summary>The waiting family whose scheduled review falls due soonest.</summary>
    public Column<int> ReviewHead { get; }

    /// <summary>The waiting family whose scheduled review falls due last.</summary>
    public Column<int> ReviewTail { get; }

    /// <summary>The first Outside Connection standing on this edge, in ascending slot order.</summary>
    /// <remarks>
    /// Derived from live gates whose Lots resolve to this edge.
    /// </remarks>
    public Column<int> GateHead { get; }

    /// <summary>The last Outside Connection standing on this edge.</summary>
    public Column<int> GateTail { get; }

    /// <summary>Which Day the <c>Today</c> counters below are counting.</summary>
    public Column<int> FlowDay { get; }

    /// <summary>Families this edge has presented today, of its own accord.</summary>
    public Column<int> OccasionsToday { get; }

    /// <summary>What <see cref="OccasionsToday"/> held at the end of the last complete Day.</summary>
    public Column<int> OccasionsYesterday { get; }

    /// <summary>
    /// How many of today's occasions an <c>Arrive</c> command asked for.
    /// </summary>
    /// <remarks>
    /// Explicit requests are also occasions. This counter identifies their subset; do not add it to
    /// occasions.
    /// </remarks>
    public Column<int> RequestedToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> RequestedYesterday { get; }

    /// <summary>Occasions today that found no gate on this edge to look through.</summary>
    public Column<int> NoConnectionToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> NoConnectionYesterday { get; }

    /// <summary>Occasions today that sampled the city and found nothing they could live in.</summary>
    public Column<int> NoSampleToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> NoSampleYesterday { get; }

    /// <summary>Occasions today that compared the city with home and stayed at home.</summary>
    public Column<int> StayedOutsideToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> StayedOutsideYesterday { get; }

    /// <summary>Occasions today that chose the city.</summary>
    public Column<int> WillingToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> WillingYesterday { get; }

    /// <summary>Households admitted through this edge's gates today.</summary>
    public Column<int> AdmittedToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> AdmittedYesterday { get; }

    /// <summary>Willing families that joined the queue today because no door had room.</summary>
    public Column<int> QueuedToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> QueuedYesterday { get; }

    /// <summary>Waiting families that gave up today at the end of the authored wait.</summary>
    public Column<int> ExpiredToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> ExpiredYesterday { get; }

    /// <summary>Scheduled reviews performed today for families already waiting.</summary>
    public Column<int> ReviewedToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> ReviewedYesterday { get; }

    /// <summary>Waiting families that reconsidered today and went home.</summary>
    public Column<int> ChangedMindToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> ChangedMindYesterday { get; }

    /// <summary>Waiting families cancelled today because the edge lost every gate.</summary>
    public Column<int> ConnectionLostToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> ConnectionLostYesterday { get; }

    /// <summary>Households the Outside added here today, recovering towards its resting count.</summary>
    public Column<int> ReplenishedToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> ReplenishedYesterday { get; }

    /// <summary>Households dropped here today for standing above the resting count.</summary>
    public Column<int> TurnoverToday { get; }

    /// <inheritdoc cref="OccasionsYesterday"/>
    public Column<int> TurnoverYesterday { get; }

    /// <summary>
    /// Moves every edge's Day counters on, if <paramref name="day"/> is not the Day they count.
    /// </summary>
    /// <remarks>
    /// Roll once before input on each new Day, including Days with no arrivals.
    /// </remarks>
    public void RollDay(int day)
    {
        for (int slot = 0; slot < Edges; slot++)
        {
            if (FlowDay[slot] == day)
            {
                continue;
            }

            FlowDay[slot] = day;

            Roll(OccasionsToday, OccasionsYesterday, slot);
            Roll(RequestedToday, RequestedYesterday, slot);
            Roll(NoConnectionToday, NoConnectionYesterday, slot);
            Roll(NoSampleToday, NoSampleYesterday, slot);
            Roll(StayedOutsideToday, StayedOutsideYesterday, slot);
            Roll(WillingToday, WillingYesterday, slot);
            Roll(AdmittedToday, AdmittedYesterday, slot);
            Roll(QueuedToday, QueuedYesterday, slot);
            Roll(ExpiredToday, ExpiredYesterday, slot);
            Roll(ReviewedToday, ReviewedYesterday, slot);
            Roll(ChangedMindToday, ChangedMindYesterday, slot);
            Roll(ConnectionLostToday, ConnectionLostYesterday, slot);
            Roll(ReplenishedToday, ReplenishedYesterday, slot);
            Roll(TurnoverToday, TurnoverYesterday, slot);
        }
    }

    private static void Roll(Column<int> today, Column<int> yesterday, int slot)
    {
        yesterday[slot] = today[slot];
        today[slot] = 0;
    }

    /// <summary>Which row holds <paramref name="edge"/>'s population.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="MapEdge.None"/>, which has no row.</exception>
    public static int SlotOf(MapEdge edge)
    {
        if (edge == MapEdge.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(edge), "the interior of the map is not behind an edge and has no Hinterland.");
        }

        return (int)edge - 1;
    }

    /// <summary>Which edge a row holds the population of.</summary>
    public static MapEdge EdgeAt(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, Edges);

        return (MapEdge)(slot + 1);
    }
}
