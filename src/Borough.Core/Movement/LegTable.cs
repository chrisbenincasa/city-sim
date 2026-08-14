using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// One Leg. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct Leg;

/// <summary>
/// The Legs of every Trip — <b>one mode-homogeneous span: a mode, two Addresses, a travel time, and
/// the next Leg</b> (<c>CONTEXT.md</c> → Leg). <b>A Leg is the plan.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A car commute is never fewer than three Legs</b> — <c>walk → drive → walk</c> — because
/// Buildings connect to the pedestrian network rather than directly to the Road Graph. The multi-Leg
/// structure exists from the first line of code; retrofitting it is not an incremental change, and
/// walking rather than transit is the decision that makes it irreversible (<c>adr/0008</c>).
/// </para>
/// <para>
/// <b>A Leg stores a cost and never a path, and that is what makes <c>adr/0008</c> affordable.</b>
/// For a <b>walk</b> Leg the route is searched, <c>distance / speed</c> is taken, and the Segment
/// list is discarded — nothing downstream reads it, because <c>CONTEXT.md</c> → Fidelity keeps
/// pedestrians out of Stress entirely and a walk Leg therefore increments no Segment volume. For a
/// <b>drive</b> Leg the path already has a home that is not the Leg: <c>adr/0060</c> puts it in the
/// shared route cache keyed by <c>(origin node, destination node, variant)</c>, reached through an
/// index on the <b>Citizen</b>. Storing a path here would be a third copy of a route the design has
/// twice decided to share, and it would multiply <c>adr/0008</c>'s <i>"roughly triples"</i> by a
/// route length instead of by a fixed record.
/// </para>
/// <para>
/// <b>Every Leg of a Trip is created at once, when the Trip is created.</b> Materialising the next
/// Leg on arrival at the previous one would contradict <c>CONTEXT.md</c> → Trip's <i>"an ordered
/// sequence of Legs"</i>, make a Trip unreportable until it completes, and make <c>plans/0002</c>
/// §B-17 unmeasurable — there would be no instant at which a Trip has all its Legs to count.
/// </para>
/// <para>
/// <b><see cref="TravelTime"/> is <c>(saved AND hashed)</c>, and this is the one place the structure
/// departs from <c>adr/0064</c>'s pattern.</b> That ADR's lesson is that a Ruleset number copied onto
/// a row at creation and never re-derived is a defect. A Leg's travel time is <em>not</em> such a
/// number: it is the answer to a search over a graph state that no longer exists once the graph
/// changes, and the search is not retained. <b>It cannot be rebuilt, so it is saved</b> — and the
/// case where the Ruleset moves underneath it is the same case a bulldozed road creates, which the
/// Epoch and the <see cref="TripFate.Stranded"/> Fate already cover.
/// </para>
/// <para>
/// <b>Mode lives here, and the Leg is mode-homogeneous by definition</b> — a Leg boundary <em>is</em>
/// a mode transition. That is what makes <c>03 §6.4</c>'s promise good: <i>"a bus is a Leg type
/// inserted into machinery that already handles Legs"</i>, without transit existing or ever being
/// built.
/// </para>
/// </remarks>
[Table]
public sealed class LegTable
{
    private readonly Rows<Leg> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="segments">The table this one's Address handles address.</param>
    public LegTable(int capacity, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _rows = new Rows<Leg>("leg", capacity, Buffering.OneCopy);

        Mode = _rows.Saved<byte>("mode");

        // Severable for the reason the Trip's endpoints are: a Segment going away underneath a
        // planned Leg is the *stranded* Fate, which is state the design models, not corruption.
        FromSegment = _rows.SavedHandle(
            "from_segment", segments.Rows, Touch.Wake, Reference.Severable);
        FromOffset = _rows.Saved<Tiles>("from_offset");
        FromSide = _rows.Saved<byte>("from_side");

        ToSegment = _rows.SavedHandle(
            "to_segment", segments.Rows, Touch.Wake, Reference.Severable);
        ToOffset = _rows.Saved<Tiles>("to_offset");
        ToSide = _rows.Saved<byte>("to_side");

        Time = _rows.Saved<TravelTime>("travel_time");
        Next = _rows.Saved<int>("next");

        RouteHead = _rows.Saved<int>("route_head");
        RouteTail = _rows.Saved<int>("route_tail");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Leg> Rows => _rows;

    /// <summary>Which single <see cref="TravelMode"/> this Leg is travelled in.</summary>
    /// <remarks>
    /// <b>One mode and never a mask</b>, which is what <i>mode-homogeneous</i> means. The column is
    /// the same <c>byte</c> the Arc's mask uses so the two compare without a conversion, but a Leg
    /// carrying two bits is a Leg that should have been two Legs.
    /// </remarks>
    public Column<byte> Mode { get; }

    /// <summary>The Leg's origin Address — its Segment.</summary>
    public HandleColumn<RoadSegment> FromSegment { get; }

    /// <summary>The Leg's origin Address — its offset.</summary>
    public Column<Tiles> FromOffset { get; }

    /// <summary>The Leg's origin Address — its <see cref="StreetSide"/>.</summary>
    public Column<byte> FromSide { get; }

    /// <summary>The Leg's destination Address — its Segment.</summary>
    public HandleColumn<RoadSegment> ToSegment { get; }

    /// <summary>The Leg's destination Address — its offset.</summary>
    public Column<Tiles> ToOffset { get; }

    /// <summary>The Leg's destination Address — its <see cref="StreetSide"/>.</summary>
    public Column<byte> ToSide { get; }

    /// <summary>
    /// What this Leg costs to travel, in Q16.16 Ticks. <b>The cost, and never the path.</b>
    /// </summary>
    public Column<TravelTime> Time { get; }

    /// <summary>The following Leg of the same Trip, encoded — see <see cref="IndexList"/>.</summary>
    public Column<int> Next { get; }

    /// <summary>
    /// The first Segment of this Leg's route, encoded — see <see cref="RouteHopTable"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Zero for every walk Leg, and for a drive Leg whose journey never left its Segment.</b>
    /// <see cref="Time"/>'s summary still says <i>the cost, and never the path</i> and that is still
    /// true of what a Leg <em>is</em>: this is a head index into another table, not a path in a
    /// column. What changed at 5c task 6 is that a drive Leg's route acquired a reader —
    /// <c>adr/0041</c> attributes Segment volume on entry — and a reader every Tick cannot be served
    /// from an evicting cache.
    /// </para>
    /// <para>
    /// ⚠ <b>An empty route is not a broken one.</b> <see cref="WalkRouting.Cost"/> answers two cases
    /// in closed form and never reaches the search — a journey along one Segment, and one refused by
    /// the reachability test — so ***absent is not zero and it is not impassable***. A volume pass
    /// that read an empty route as <em>no Segments to credit</em> would drop every same-Segment drive
    /// in the city.
    /// </para>
    /// </remarks>
    public Column<int> RouteHead { get; }

    /// <summary>The last Segment of this Leg's route, encoded.</summary>
    public Column<int> RouteTail { get; }

    /// <summary>This Leg's route, as a list over <paramref name="hops"/>.</summary>
    public IndexList Route(RouteHopTable hops)
    {
        ArgumentNullException.ThrowIfNull(hops);

        return new IndexList(RouteHead, RouteTail, hops.Next);
    }

    /// <summary>Adds one Segment to the end of a Leg's route.</summary>
    public void AppendHop(RouteHopTable hops, int leg, int hop) => Route(hops).Append(leg, hop);

    /// <summary>
    /// Allocates a Leg. <b>Link it to its Trip with <see cref="TripTable.Append"/></b>; a Leg that
    /// exists in no Trip's list is unreachable and will never be freed.
    /// </summary>
    public Handle<Leg> Create(
        RoadSegmentTable segments, TravelMode mode, Address from, Address to, TravelTime time)
    {
        ArgumentNullException.ThrowIfNull(segments);

        Handle<Leg> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Mode[slot] = (byte)mode;

        FromSegment[slot] = Held(segments, from);
        FromOffset[slot] = from.Offset;
        FromSide[slot] = (byte)from.Side;

        ToSegment[slot] = Held(segments, to);
        ToOffset[slot] = to.Offset;
        ToSide[slot] = (byte)to.Side;

        Time[slot] = time;
        Next[slot] = 0;

        return handle;
    }

    /// <summary>
    /// The Leg's origin Address, or <see cref="Address.None"/> if its Segment has been bulldozed.
    /// </summary>
    /// <remarks>
    /// <b>This is the boundary the whole representation turns on.</b> The Segment is stored as a
    /// handle, because a saved column must fold the target's monotonic id rather than a slot index;
    /// an <see cref="Address"/> carries a slot, because it is read off the live graph and used within
    /// the Tick that read it. Resolving here converts one into the other <em>and</em> is the single
    /// place a vanished road is noticed — which is why the failure is
    /// <c>adr/0079</c>'s named absence rather than a throw or a stale reference. The caller turns it
    /// into <see cref="TripFate.Stranded"/>.
    /// </remarks>
    public Address From(int slot, RoadSegmentTable segments) =>
        Read(segments, FromSegment[slot], FromOffset[slot], (StreetSide)FromSide[slot]);

    /// <inheritdoc cref="From"/>
    /// <summary>The Leg's destination Address, or <see cref="Address.None"/>.</summary>
    public Address To(int slot, RoadSegmentTable segments) =>
        Read(segments, ToSegment[slot], ToOffset[slot], (StreetSide)ToSide[slot]);

    /// <summary>The handle a place's slot corresponds to, or the unset handle for no place.</summary>
    private static Handle<RoadSegment> Held(RoadSegmentTable segments, Address address) =>
        address.Exists ? segments.Rows.At(address.Segment) : default;

    /// <summary>Three columns back into one value, refusing a Segment that is no longer there.</summary>
    private static Address Read(
        RoadSegmentTable segments, Handle<RoadSegment> segment, Tiles offset, StreetSide side)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return segments.Rows.TryResolve(segment, out int slot)
            ? Address.On(slot, offset, side)
            : Address.None;
    }
}
