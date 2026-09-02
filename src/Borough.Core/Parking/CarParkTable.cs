using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Parking;

/// <summary>
/// One Car Park. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct CarPark;

/// <summary>
/// Every Car Park in the city — <b>a Building's parking provision: an <see cref="Address"/>, a
/// capacity, and how much of it is occupied</b> (<c>CONTEXT.md</c> → Car Park).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is not a <c>BinTable</c> row, and the distinction was in <c>CONTEXT.md</c> before the word
/// was</b> (<c>adr/0120</c>). Four structural mismatches against the shared name: a Bin is located
/// only by <c>Handle&lt;Building&gt;</c>, its Resource is a Good, it carries two wait lists in a
/// mechanism where <em>nothing about parking ever waits</em> (<c>adr/0009</c>), and <c>CONTEXT.md</c>
/// → Supply and Space reserves the <c>Bin</c> type for Goods and Money by name — using <em>a full
/// Parking Shed</em> as its own worked example of a ceiling that is not a Bin. <c>adr/0068</c>'s test
/// is what separates them: <b>a Bin has a consumer and a parked car has a holder</b>, which puts this
/// on employment's side of the line — <c>[capacity] floor_tiles_per_job</c> — and makes its
/// over-capacity rule a <b>dismissal</b>.
/// </para>
/// <para>
/// <b>There is no wait list and that is the saving, not an omission.</b> A car whose nearest Car Park
/// is full does not sleep until one frees — the Parking Shed widens and the longer walk <em>is</em>
/// the cost. The Bin's two most expensive features are the two parking has no use for.
/// </para>
/// <para>
/// <b>The space is held by the <see cref="Citizen"/></b> (<c>adr/0119</c>) — see
/// <c>CitizenTable.ParkedIn</c>. Not by a Trip and not by a Traveller, both of which are freed when
/// the journey ends, and a car is parked when no journey is happening at all. Not by the Household
/// either: <see cref="World.ModeOf"/> drives <em>every member</em> of a car-owning Household, so a
/// Household of three workers parks three cars and one column would overwrite two of them.
/// </para>
/// </remarks>
[Table]
public sealed class CarParkTable
{
    private readonly Rows<CarPark> _rows;

    /// <param name="capacity">Initial slot count.</param>
    /// <param name="buildings">The table this one's <see cref="Owner"/> handles address.</param>
    /// <param name="segments">The table this one's <see cref="WhereSegment"/> handles address.</param>
    public CarParkTable(int capacity, BuildingTable buildings, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(segments);

        _rows = new Rows<CarPark>("car_park", capacity, Buffering.OneCopy);

        Owner = _rows.SavedHandle("owner", buildings.Rows);

        // LegTable.From's three-column pattern, and it is not a style choice: an Address holds a
        // Segment *slot*, and a saved slot index folds the city's whole demolition history into the
        // State Hash, so two runs building the same city would disagree. Address.cs says so at
        // length. Severable for LegTable's reason -- a Street bulldozed under a Car Park is
        // adr/0079's named absence rather than corruption, and Address.None is what the shed sees.
        WhereSegment = _rows.SavedHandle(
            "where_segment", segments.Rows, Touch.Wake, Reference.Severable);
        WhereOffset = _rows.Saved<Tiles>("where_offset");
        WhereSide = _rows.Saved<byte>("where_side");

        Capacity = _rows.Derived<int>("capacity");
        Occupied = _rows.Saved<int>("occupied", Touch.PerTick);

        SegmentNext = _rows.Derived<int>("segment_next");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<CarPark> Rows => _rows;

    /// <summary>The Building this Car Park belongs to.</summary>
    /// <remarks>
    /// <b>Not how the Car Park is located</b> — that is <see cref="AddressAt"/>. A Bin is located by
    /// its owner and nothing else, which is exactly the mismatch <c>adr/0120</c> cites first; keeping
    /// the Address a real column is what leaves a Segment-held Car Park needing no new one.
    /// </remarks>
    public HandleColumn<Building> Owner { get; }

    /// <summary>The Segment this Car Park sits on. See <see cref="AddressAt"/>.</summary>
    public HandleColumn<RoadSegment> WhereSegment { get; }

    /// <summary>How far along <see cref="WhereSegment"/>, from its A endpoint.</summary>
    public Column<Tiles> WhereOffset { get; }

    /// <summary>Which side of <see cref="WhereSegment"/>'s A→B direction.</summary>
    public Column<byte> WhereSide { get; }

    /// <summary>
    /// How many Vehicles this Car Park holds when full.
    /// </summary>
    /// <remarks>
    /// <b>Derived and rebuilt, never saved and hashed</b> (<c>adr/0068</c>, <c>adr/0064</c>,
    /// <c>adr/0120</c>). It is a pure function of the Building's <b>floor area</b> and the Ruleset in
    /// force — <c>[capacity] floor_tiles_per_parking_space</c>, on a kind that states
    /// <c>parked</c> — so it is rebuilt at world load and at every Ruleset swap, and a retuned
    /// provision therefore reaches every Building standing rather than only the next one raised.
    /// <see cref="World.RebuildCapacities"/> is the one writer. ⚠ <b>The second multiplicand is new
    /// as of <c>plans/0053</c> step 3</b>: this was a per-kind count, so every Building of a kind
    /// parked the same number of cars whatever it stood on.
    /// </remarks>
    public Column<int> Capacity { get; }

    /// <summary>
    /// How many Vehicles are parked here. Read freely; the writers are <see cref="World"/>'s.
    /// </summary>
    /// <remarks>
    /// <b>Saved and hashed, where <see cref="Capacity"/> is derived</b>, and the pair is the same
    /// split a Bin's level and ceiling have. Occupancy is a fact about the city; the ceiling is a fact
    /// about the file.
    /// </remarks>
    public Column<int> Occupied { get; }

    /// <summary>
    /// The next Car Park on the same Segment. The element side of <see cref="CarParkResidency"/>.
    /// </summary>
    /// <remarks>
    /// <b>A Car Park sits on exactly one Segment, which is what makes an intrusive list legal here
    /// and illegal for the shed itself.</b> The Parking Shed is many-to-many — one Car Park is within
    /// walking distance of many Buildings — so it cannot thread a single <c>next</c> through this
    /// table. This index is one-to-many and can.
    /// </remarks>
    public Column<int> SegmentNext { get; }

    /// <summary>
    /// Where this Car Park is, or <see cref="Address.None"/> if its Street has been bulldozed.
    /// </summary>
    /// <remarks>
    /// <b>Assembled at the read boundary</b>, which is the one place a severed Segment handle becomes
    /// <c>adr/0079</c>'s named absence rather than a stale reference. <c>Address.cs</c> prescribes
    /// this shape and <c>LegTable</c> is the precedent.
    /// </remarks>
    public Address AddressAt(RoadSegmentTable segments, int slot)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return segments.Rows.TryResolve(WhereSegment[slot], out int segmentSlot)
            ? Address.On(segmentSlot, WhereOffset[slot], (StreetSide)WhereSide[slot])
            : Address.None;
    }

    /// <summary>How many more Vehicles this Car Park can take.</summary>
    /// <remarks>
    /// <b>May return a negative, and a caller assuming otherwise is wrong rather than unlucky.</b>
    /// <see cref="Capacity"/> comes from the Ruleset in force, so a lowered provision can land under
    /// the current <see cref="Occupied"/> — and unlike a Bin, which is left to drain because it has a
    /// consumer, the overflow here is <b>dismissed</b> (<c>adr/0120</c>). Between the reload and the
    /// dismissal this reads negative.
    /// </remarks>
    public int SpaceAt(int slot) => Capacity[slot] - Occupied[slot];

    /// <summary>Allocates an empty Car Park on a Building at an Address.</summary>
    /// <remarks>
    /// <b><see cref="Occupied"/> is written rather than assumed, because a recycled slot carries its
    /// predecessor's contents.</b> <see cref="Rows{T}.Allocate"/> hands back a free slot without
    /// clearing any column, and demolition is what makes that reachable: the next Building raised on
    /// a cleared Lot would open with the condemned one's cars still in its Car Park — capacity
    /// destroyed from nothing, which reads as a busy city rather than as a defect.
    /// </remarks>
    internal Handle<CarPark> Create(
        Handle<Building> owner, Handle<RoadSegment> segment, Tiles offset, StreetSide side, int capacity)
    {
        Handle<CarPark> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Owner[slot] = owner;
        WhereSegment[slot] = segment;
        WhereOffset[slot] = offset;
        WhereSide[slot] = (byte)side;
        Capacity[slot] = capacity;
        Occupied[slot] = 0;

        return handle;
    }

    /// <summary>
    /// Writes a Car Park's derived ceiling. <see cref="World.RebuildCapacities"/> is the only caller.
    /// </summary>
    /// <remarks>
    /// <see cref="Capacity"/> is a public <see cref="Column{T}"/> and this method still exists for
    /// <c>BinTable.SetCapacity</c>'s reason: it is where the one writer lives, so the derivation has
    /// a single spelling and a search for it finds one site rather than an assignment anybody could
    /// have made.
    /// </remarks>
    internal void SetCapacity(int slot, int vehicles) => Capacity[slot] = vehicles;

    /// <summary>
    /// Moves the occupancy. The one writer, <see langword="internal"/> so that an acquire or a release
    /// cannot happen without <see cref="World"/> pairing it with the holder's column.
    /// </summary>
    /// <remarks>
    /// <b>The pairing is what <c>adr/0084</c>'s two invariants are about</b>, and the reason this is
    /// not a public setter is that an unpaired write is the <c>adr/0006</c>-class leak they exist to
    /// catch: occupancy destroyed here is destroyed for ever and recovers by nothing.
    /// </remarks>
    internal void Move(int slot, int delta) => Occupied[slot] += delta;
}
