using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>One Segment of one Leg's route. Empty for the reason <see cref="Traveller"/> is empty.</summary>
public readonly struct RouteHop;

/// <summary>
/// The Segments a drive Leg crosses, in the order they are crossed — <b>the route a
/// <see cref="TravellerTable">Traveller</see> is executing, and the only structure that can say which
/// Segment it is on right now</b>.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>This narrows <c>adr/0075</c>'s <em>a Leg stores a cost, never a path</em>, and 5c task 6 is
/// where that had to give.</b> That ADR's argument is sound for the case it was written about — a
/// <b>walk</b> Leg's Segments are read by nothing, so retaining them would be pure cost — and it homes
/// a drive path in <c>adr/0060</c>'s shared route cache instead. What it did not anticipate is that
/// <c>adr/0041</c> attributes volume <em>on Segment entry</em>, so an in-flight drive Traveller needs
/// its route <b>every Tick, reliably</b>, and 5c task 4's cache is fixed-capacity and evicts. **A cache
/// entry disappearing under a moving vehicle would strand it on a Segment it never leaves**, which is
/// an <c>adr/0006</c>-class volume leak that presents as a road busy forever.
/// </para>
/// <para>
/// <b>So the two structures answer different questions and both survive.</b> The cache answers <i>what
/// is the route between these two nodes</i> and is shared, evicting and optional. This answers <i>what
/// is <b>this</b> Traveller doing</i>, is per-Leg, and is freed when the Trip ends.
/// ***A shared cache is an optimisation and an executing plan is state.***
/// </para>
/// <para>
/// <b>An intrusive index list, per <c>CLAUDE.md</c></b> — a head and tail on the
/// <see cref="LegTable">Leg</see>, a <see cref="Next"/> here, both in flat arrays. There is no
/// per-Leg collection object and no variable-length column.
/// </para>
/// <para>
/// ⚠ <b><see cref="Segment"/> is <see cref="Reference.Severable"/> and the route is <em>not</em>
/// repaired when one goes.</b> A Segment bulldozed under a moving Traveller is
/// <see cref="TripFate.Stranded"/> — state the design models rather than corruption — and repairing
/// the route here would be a re-search, which <c>adr/0061</c> forbids by name for the diversion case
/// and which nothing has decided for this one.
/// </para>
/// <para>
/// <b>Saved rather than derived, and the reason is that a route is not a function of its endpoints
/// alone.</b> It is a function of the endpoints <em>and the graph at the moment it was planned</em>.
/// A reload that recomputed it would silently re-plan every journey in flight against a graph the
/// player may have edited since, which is a different city under <c>05 §4</c> — and it would do so
/// with no symptom, because both routes are valid.
/// </para>
/// <para>
/// <b>Its sink is the Trip's release</b> (<c>adr/0006</c>): <see cref="TripEngine"/> pops a Leg's hops
/// before freeing the Leg, the same way it pops a Trip's Legs before freeing the Trip. Bounded by
/// in-flight drive Legs × route length, which is a population figure and not an elapsed-time one.
/// </para>
/// </remarks>
[Table]
public sealed class RouteHopTable
{
    private readonly Rows<RouteHop> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="segments">The table this one's handles address.</param>
    public RouteHopTable(int capacity, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _rows = new Rows<RouteHop>("route_hop", capacity, Buffering.OneCopy);

        Segment = _rows.SavedHandle("segment", segments.Rows, Touch.PerTick, Reference.Severable);
        Forward = _rows.Saved<byte>("forward");
        Next = _rows.Saved<int>("next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<RouteHop> Rows => _rows;

    /// <summary>The Segment crossed at this point in the route.</summary>
    public HandleColumn<RoadSegment> Segment { get; }

    /// <summary>
    /// <c>1</c> if the Segment is crossed from <see cref="RoadSegmentTable.NodeA"/> toward
    /// <see cref="RoadSegmentTable.NodeB"/>, <c>0</c> the other way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Stored rather than derived from the arc, because an arc slot is not stable and this table is
    /// saved.</b> <see cref="RoadArcs"/> is rebuilt whenever the graph is edited, so an arc index in a
    /// saved row would address a different arc after a reload. A Segment handle survives, and the
    /// direction is one bit that is known at planning time and never changes afterwards.
    /// </para>
    /// <para>
    /// ⚠ <b>The two endpoint hops take the direction of the arc beside them, and a route with no arcs
    /// at all is recorded as forward.</b> A vehicle starts part-way along its own Segment and leaves by
    /// one of its two nodes, so the direction of that first crossing is decided by which node the first
    /// arc departs from — derivable, and derived. When origin and destination share a Segment, or sit
    /// on two Segments meeting at a node, there is no arc and nothing determines a direction; forward is
    /// then a convention rather than a measurement, and it is stated here so that a later reader does
    /// not mistake the resulting forward bias on short journeys for a property of the city.
    /// </para>
    /// </remarks>
    public Column<byte> Forward { get; }

    /// <summary>The next hop of the same route, encoded — see <see cref="IndexList"/>.</summary>
    public Column<int> Next { get; }

    /// <summary>
    /// Allocates one hop. <b>Link it with <see cref="LegTable.AppendHop"/></b>; a hop in no Leg's
    /// list is unreachable and will never be freed.
    /// </summary>
    public Handle<RouteHop> Create(RoadSegmentTable segments, int segmentSlot, bool forward)
    {
        ArgumentNullException.ThrowIfNull(segments);

        Handle<RouteHop> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Segment[slot] = segments.Rows.At(segmentSlot);
        Forward[slot] = forward ? (byte)1 : (byte)0;
        Next[slot] = 0;

        return handle;
    }
}
