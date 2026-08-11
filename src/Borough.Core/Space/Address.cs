using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// Which side of a Segment a place sits on — <b>left or right of the Segment's forward direction,
/// which is fixed A→B by its endpoints</b> (<c>adr/0074</c>).
/// </summary>
/// <remarks>
/// <b>An enumeration and not a coordinate, which is what keeps geometry out of the simulation.</b>
/// A Segment's endpoints already fix its direction — the fact <c>adr/0072</c> rests on — so left and
/// right are well-defined without a spline, an angle or a normal. <c>CONTEXT.md</c> → Road Graph:
/// <i>"The simulation never sees a spline"</i>, and this is one of the places it would have had to.
/// </remarks>
public enum StreetSide : byte
{
    /// <summary>Left of the A→B direction. The odd side, by the analogy the name comes from.</summary>
    Left = 0,

    /// <summary>Right of the A→B direction.</summary>
    Right = 1,
}

/// <summary>
/// <b>A location on the Road Graph: a Segment, an offset along it, and which side of it.</b> Never a
/// Node. (<c>CONTEXT.md</c> → Address, <c>adr/0074</c>.)
/// </summary>
/// <remarks>
/// <para>
/// <b>The value every query about <em>where something is</em> takes and returns.</b> A Building's
/// Access Point is a Building's Address, a Leg runs from one Address to another, and a parking Bin
/// will have one. <b>The word is chosen because a street address is literally this triple</b> — a
/// distance along a street plus an odd or even side.
/// </para>
/// <para>
/// <b>An Address is an offset along a Segment and never a Node, and the reason is arithmetic rather
/// than taste.</b> Five Buildings share a Segment at the working figures, so promoting Addresses to
/// Nodes would split every Segment five ways and put the Road Graph at 150,000–300,000 Segments
/// instead of ~30,000. A routing query is therefore <c>Address → Address</c>, which is the query shape
/// everything downstream must be measured on.
/// </para>
/// <para>
/// <b>It holds a slot rather than a <c>Handle</c>, and that is a consequence of being derived.</b>
/// Frontage is <c>(derived AND rebuilt)</c> (<c>adr/0078</c>), so an Address is reconstructed from
/// saved state on every load and every Epoch bump and never outlives the graph it was read off. A
/// handle would carry a generation this value has no way to check and no reason to hold — and would
/// invite exactly the staleness <see cref="None"/> exists to make impossible.
/// </para>
/// <para>
/// <b><see cref="None"/> is a value and not a null, which is <c>adr/0079</c>'s requirement.</b> A
/// Building whose last Street was bulldozed has no Address, and that state must be nameable at the
/// write site rather than inferred from a handle that fails to resolve. The plus-one encoding is
/// <see cref="Entities.LotTable.BuildingSlot"/>'s, for the identical reason: zero-filled storage must
/// read as <em>absent</em> rather than as <em>Segment slot 0</em> — the first Street in the city,
/// silently claimed by every unfronted Building.
/// </para>
/// </remarks>
public readonly record struct Address
{
    private readonly int _segmentPlusOne;

    private Address(int segmentPlusOne, Tiles offset, StreetSide side)
    {
        _segmentPlusOne = segmentPlusOne;
        Offset = offset;
        Side = side;
    }

    /// <summary>
    /// <b>No Address at all</b> — the state of a Building with no frontage (<c>adr/0079</c>).
    /// </summary>
    /// <remarks>
    /// Distinct from an Address at offset zero on Segment zero, which is an ordinary place a Building
    /// can genuinely stand.
    /// </remarks>
    public static Address None => default;

    /// <summary>How far along the Segment, from its A endpoint.</summary>
    public Tiles Offset { get; }

    /// <summary>Which side of the Segment's A→B direction.</summary>
    public StreetSide Side { get; }

    /// <summary>Whether this names a place at all.</summary>
    public bool Exists => _segmentPlusOne != 0;

    /// <summary>
    /// The Segment's slot, or <see cref="Tables.Rows.NoSlot"/> when there is no Address.
    /// </summary>
    /// <remarks>Absent decodes to <see cref="Tables.Rows.NoSlot"/> on its own: stored zero, minus one.</remarks>
    public int Segment => _segmentPlusOne - 1;

    /// <summary>Names a place on a Segment.</summary>
    public static Address On(int segmentSlot, Tiles offset, StreetSide side) =>
        new(segmentSlot + 1, offset, side);
}
