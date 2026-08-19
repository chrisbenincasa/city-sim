using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Parking;

/// <summary>
/// Which Car Parks sit on a given Road Segment. The reverse index from a Street to the parking on it.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="Space.BuildingResidency"/>'s shape one table over, and it exists for the same reason:
/// the index did not exist and the consumer needs it.</b> A Car Park stores where it is — an
/// <c>Address</c>, and therefore a Segment — and nothing could ask a Segment what stands on it. The
/// Parking Shed's ball walks the Road Graph and meets <em>Segments</em>, so without this it would have
/// to scan every Car Park in the city per Building.
/// </para>
/// <para>
/// <b>An intrusive index list whose owner is a Segment slot.</b> The element side is
/// <see cref="CarParkTable.SegmentNext"/>, a column, because an element is a row. The head and tail
/// are the two arrays below rather than columns on <see cref="RoadSegmentTable"/>, because
/// <c>BOR0901</c> rejects storage in a <c>[Table]</c> type that is not a declared column and a
/// Segment's parking is not a fact about the Segment — a Street knows nothing about the Buildings
/// beside it, which is <c>adr/0078</c>'s direction of derivation.
/// </para>
/// <para>
/// <b>This is legal where the shed itself is not, and the difference is the whole reason both exist.</b>
/// A Car Park is on exactly <em>one</em> Segment, so a single <c>next</c> per Car Park expresses the
/// membership completely. A Parking Shed is <b>many-to-many</b> — one Car Park is within walking
/// distance of many Buildings — so no <c>next</c> on <see cref="CarParkTable"/> can express it, and
/// <c>IndexList</c>'s own list of three intended consumers is wrong about this one.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, and it passes <c>05 §3</c>'s test rather than merely wanting to.</b>
/// A Car Park's Segment is a saved handle, and <see cref="IndexList.InsertOrdered"/> puts each list in
/// ascending slot order however the rows arrived — so a rebuild reproduces the <em>order</em> and not
/// merely the membership. Appending would reproduce creation order, which diverges the moment the free
/// list recycles a slot, with nothing to report it.
/// </para>
/// </remarks>
public sealed class CarParkResidency
{
    private int[] _head = [];
    private int[] _tail = [];

    /// <summary>How many Segments this index currently covers.</summary>
    public int Segments { get; private set; }

    /// <summary>Rebuilds the whole index from the Car Parks' saved Addresses.</summary>
    /// <remarks>
    /// <b>Wholesale rather than incrementally</b>, which is what makes the ordered insert above sound:
    /// a list accumulated across a run and a list rebuilt from the same rows must agree, and the only
    /// cheap way to guarantee that is for the rebuild to be the definition.
    /// </remarks>
    public void Rebuild(CarParkTable carParks, RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(carParks);
        ArgumentNullException.ThrowIfNull(segments);

        Segments = segments.Rows.SlotCount;

        if (_head.Length < Segments)
        {
            _head = new int[Segments];
            _tail = new int[Segments];
        }

        Array.Clear(_head, 0, Segments);
        Array.Clear(_tail, 0, Segments);
        carParks.SegmentNext.Span.Clear();

        IndexList list = new(_head, _tail, carParks.SegmentNext);

        for (int slot = 0; slot < carParks.Rows.SlotCount; slot++)
        {
            // A severed Segment handle is a Car Park whose Street was bulldozed. It is live supply
            // with no Address, so it is in no Segment's list and no shed will find it -- which is
            // adr/0079's named absence rather than a leak, and the Car Park is still there to be
            // re-indexed if a Street returns.
            if (carParks.Rows.IsLive(slot)
                && segments.Rows.TryResolve(carParks.WhereSegment[slot], out int segment))
            {
                list.InsertOrdered(segment, slot);
            }
        }
    }

    /// <summary>Walks the Car Parks on <paramref name="segment"/>, in ascending slot order.</summary>
    public IndexListWalk On(int segment, CarParkTable carParks)
    {
        ArgumentNullException.ThrowIfNull(carParks);

        IndexList list = new(_head, _tail, carParks.SegmentNext);

        return list.Walk(segment);
    }

    /// <summary>Whether any Car Park sits on <paramref name="segment"/>.</summary>
    public bool Any(int segment) =>
        (uint)segment < (uint)Segments && _head[segment] != 0;
}
