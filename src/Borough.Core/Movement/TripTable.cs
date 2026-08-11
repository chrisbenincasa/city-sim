using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// One Trip. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct Trip;

/// <summary>
/// The city's Trips — <b>a journey with a Trip Purpose, an origin and destination Address, an
/// ordered sequence of Legs, and a Trip Fate</b> (<c>CONTEXT.md</c> → Trip).
/// </summary>
/// <remarks>
/// <para>
/// <b>Trips are first-class objects rather than transient calculations, because a failed Trip must
/// be reportable.</b> That is the whole reason the row exists — a calculation that returned
/// <i>unreachable</i> and vanished would leave the city unable to say <em>which</em> people cannot
/// reach <em>which</em> shops, which is the one thing this milestone is for. <c>LEGIBLE CAUSE</c>
/// </para>
/// <para>
/// <b>A Trip owns the purpose and the Fate; a <see cref="LegTable">Leg</see> owns the plan; a
/// <see cref="TravellerTable">Traveller</see> owns the cursor</b> (<c>adr/0075</c>). The split is
/// three-way and the corpus had drawn it as two. What makes the third row exist is an invariant
/// rather than tidiness: <c>CONTEXT.md</c> → Promotion/Demotion requires that <i>conserved
/// quantities live on the Citizen record, never on the embodiment — <b>a Traveller is a view, not an
/// owner</b></i>. Putting every durable thing on a row that outlives the journey and every transient
/// thing on a row that is released makes that true by construction rather than by discipline.
/// </para>
/// <para>
/// <b>An Address is three columns and not one</b>, here and on the Leg. A column of
/// <see cref="Address"/> would fold the Segment handle's <em>slot index</em> into the State Hash,
/// and <see cref="HandleColumn{TTarget}"/> exists because that is identity rather than value. So the
/// Segment goes through <c>SavedHandle</c> — folded by the target's monotonic never-reused id — and
/// the offset and side fold as the plain values they are.
/// </para>
/// <para>
/// ⚠ <b>This table needs a sink, and <i>Trips are transient</i> does not supply one.</b>
/// <c>adr/0006</c> is satisfied by the Fate reaching the Census <em>before</em> the row is freed;
/// otherwise the only durable record of a failure is destroyed by the mechanism meant to bound the
/// table. The Fate counters are <em>flows</em> rather than levels, so they follow slice 7 task 9's
/// precedent — read as a sum and a peak over the interval, and the reading drains them.
/// </para>
/// </remarks>
[Table]
public sealed class TripTable
{
    private readonly Rows<Trip> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="segments">The table this one's Address handles address.</param>
    public TripTable(int capacity, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _rows = new Rows<Trip>("trip", capacity, Buffering.OneCopy);

        Purpose = _rows.Saved<byte>("purpose");

        // Severable on both endpoints: an Address outlives its Building and outlives its Segment,
        // and a Segment going away underneath a Trip is the *stranded* Fate rather than a break in
        // referential integrity. Declaring these Required would make the whole-world handle walk
        // report every Trip caught by a demolition as corruption.
        OriginSegment = _rows.SavedHandle(
            "origin_segment", segments.Rows, Touch.Wake, Reference.Severable);
        OriginOffset = _rows.Saved<Tiles>("origin_offset");
        OriginSide = _rows.Saved<byte>("origin_side");

        DestinationSegment = _rows.SavedHandle(
            "destination_segment", segments.Rows, Touch.Wake, Reference.Severable);
        DestinationOffset = _rows.Saved<Tiles>("destination_offset");
        DestinationSide = _rows.Saved<byte>("destination_side");

        LegHead = _rows.Saved<int>("leg_head");
        LegTail = _rows.Saved<int>("leg_tail");

        Fate = _rows.Saved<byte>("fate");
        FailingLeg = _rows.Saved<int>("failing_leg");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Trip> Rows => _rows;

    /// <summary>Which <see cref="TripPurpose"/> this Trip serves.</summary>
    public Column<byte> Purpose { get; }

    /// <summary>The origin Address's Segment.</summary>
    public HandleColumn<RoadSegment> OriginSegment { get; }

    /// <summary>The origin Address's offset along its Segment.</summary>
    public Column<Tiles> OriginOffset { get; }

    /// <summary>The origin Address's <see cref="StreetSide"/>.</summary>
    public Column<byte> OriginSide { get; }

    /// <summary>The destination Address's Segment.</summary>
    public HandleColumn<RoadSegment> DestinationSegment { get; }

    /// <summary>The destination Address's offset along its Segment.</summary>
    public Column<Tiles> DestinationOffset { get; }

    /// <summary>The destination Address's <see cref="StreetSide"/>.</summary>
    public Column<byte> DestinationSide { get; }

    /// <summary>The first Leg, encoded — see <see cref="IndexList"/>.</summary>
    public Column<int> LegHead { get; }

    /// <summary>The last Leg, encoded. What makes appending a Leg O(1).</summary>
    public Column<int> LegTail { get; }

    /// <summary>Which <see cref="TripFate"/> ended this Trip, or <see cref="TripFate.InFlight"/>.</summary>
    public Column<byte> Fate { get; }

    /// <summary>
    /// The zero-based index of the Leg that failed, or <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <b>One field, and it is the difference between <i>no route found</i> and <i>no route found on
    /// Leg 1 of 3, on foot, from here to there</i>.</b> <c>adr/0008</c>'s last consequence demands it
    /// by name: <i>"a Trip Fate of no route found for a destination 50m away must be diagnosable, not
    /// mysterious."</i>
    /// <para>
    /// It is the Leg's <em>ordinal within the Trip</em> rather than its slot, because the number is
    /// for a person reading a report and <i>Leg 1 of 3</i> is what a person can act on. The slot is
    /// recoverable by walking the list and is meaningless once the row is freed.
    /// </para>
    /// </remarks>
    public Column<int> FailingLeg { get; }

    /// <summary>
    /// The Trip's Legs, in order.
    /// </summary>
    /// <remarks>
    /// <b>The Leg table is passed in rather than held, and <c>BOR0901</c> is why.</b> A
    /// <c>[Table]</c> holding a reference to another table is storage that is neither saved nor
    /// derived, which is the declaration hole the analyser exists to close — it caught this on the
    /// first build. The list is therefore composed at the call site from the two tables that own its
    /// columns, which is the shape <c>World.Occupants</c> and <c>World.BuildingBins</c> already have.
    /// </remarks>
    public IndexList LegList(LegTable legs)
    {
        ArgumentNullException.ThrowIfNull(legs);

        return new IndexList(LegHead, LegTail, legs.Next);
    }

    /// <summary>
    /// Allocates a Trip between two Addresses, in flight and with no Legs.
    /// </summary>
    /// <remarks>
    /// <b>The Fate opens at <see cref="TripFate.InFlight"/> and the failing Leg at
    /// <see cref="Rows.NoSlot"/></b>, which are both the zero-filled defaults and are written anyway.
    /// A recycled slot carries the previous occupant's values until something overwrites them, and a
    /// Trip that inherited a <see cref="TripFate.Completed"/> from a dead predecessor would satisfy
    /// <c>02</c>'s <i>no Trip without a Fate</i> assertion while never having been resolved.
    /// </remarks>
    public Handle<Trip> Create(
        RoadSegmentTable segments, TripPurpose purpose, Address origin, Address destination)
    {
        ArgumentNullException.ThrowIfNull(segments);

        Handle<Trip> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Purpose[slot] = (byte)purpose;

        OriginSegment[slot] = origin.Exists ? segments.Rows.At(origin.Segment) : default;
        OriginOffset[slot] = origin.Offset;
        OriginSide[slot] = (byte)origin.Side;

        DestinationSegment[slot] =
            destination.Exists ? segments.Rows.At(destination.Segment) : default;
        DestinationOffset[slot] = destination.Offset;
        DestinationSide[slot] = (byte)destination.Side;

        LegHead[slot] = 0;
        LegTail[slot] = 0;

        Fate[slot] = (byte)TripFate.InFlight;
        FailingLeg[slot] = Tables.Rows.NoSlot;

        return handle;
    }

    /// <summary>
    /// The origin Address, or <see cref="Address.None"/> if its Segment has been bulldozed.
    /// </summary>
    /// <remarks>See <see cref="LegTable.From"/> for why the resolve happens here and not at write.</remarks>
    public Address Origin(int slot, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return segments.Rows.TryResolve(OriginSegment[slot], out int segment)
            ? Address.On(segment, OriginOffset[slot], (StreetSide)OriginSide[slot])
            : Address.None;
    }

    /// <inheritdoc cref="Origin"/>
    /// <summary>The destination Address, or <see cref="Address.None"/>.</summary>
    public Address Destination(int slot, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return segments.Rows.TryResolve(DestinationSegment[slot], out int segment)
            ? Address.On(segment, DestinationOffset[slot], (StreetSide)DestinationSide[slot])
            : Address.None;
    }

    /// <summary>
    /// Appends a Leg to the end of the Trip's sequence.
    /// </summary>
    /// <remarks>
    /// <b>Appending rather than pushing, because a Trip is an <em>ordered</em> sequence of Legs</b>
    /// and <c>walk → drive → walk</c> read backwards is a different journey. This is
    /// <see cref="IndexList"/>'s stated reason for appending at the tail, reaching its fourth
    /// consumer.
    /// </remarks>
    public void Append(LegTable legs, int trip, int leg) => LegList(legs).Append(trip, leg);

    /// <summary>
    /// Records how a Trip ended, and which Leg ended it.
    /// </summary>
    /// <remarks>
    /// <b>The one door a Fate goes through</b>, so that <c>02</c>'s <i>no Trip without a Fate</i> has
    /// a single write site to assert at rather than a rule every caller has to remember. Resolving a
    /// Trip twice is refused rather than tolerated: the second Fate would overwrite the first, and
    /// the first is the one that is true.
    /// </remarks>
    /// <param name="slot">The Trip.</param>
    /// <param name="fate">How the journey ended. Never <see cref="TripFate.InFlight"/>.</param>
    /// <param name="failingLeg">
    /// The ordinal of the Leg that failed, or <see cref="Rows.NoSlot"/> when the Trip completed.
    /// </param>
    public void Resolve(int slot, TripFate fate, int failingLeg = Tables.Rows.NoSlot)
    {
        if (fate == TripFate.InFlight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fate), "a Trip is resolved to an outcome, never back to in flight.");
        }

        if ((TripFate)Fate[slot] != TripFate.InFlight)
        {
            throw new InvalidOperationException(
                $"trip in slot {slot} already ended as {(TripFate)Fate[slot]}.");
        }

        Fate[slot] = (byte)fate;
        FailingLeg[slot] = failingLeg;
    }
}
