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

    /// <summary>Lists a newly created Car Park against the Segment its Address names.</summary>
    /// <remarks>
    /// <b>The door half of the rebuild above, and the two must agree by construction rather than by
    /// inspection</b> — both go through <see cref="IndexList.InsertOrdered"/>, so a list accumulated
    /// across a run and a list rebuilt from the same rows come out in the same order whatever order
    /// the rows arrived in. That equality is what <c>DerivedRebuildAuditTests</c> asserts, and it is
    /// the reason this is not simply a whole rebuild triggered on supply change: a rebuild is
    /// <c>O(Car Parks)</c> and a Building is placed every few Ticks.
    /// </remarks>
    public void Add(CarParkTable carParks, RoadSegmentTable segments, int slot)
    {
        ArgumentNullException.ThrowIfNull(carParks);
        ArgumentNullException.ThrowIfNull(segments);

        Grow(segments);

        // A Car Park with no resolvable Segment is in no list, exactly as Rebuild leaves it. That is
        // adr/0079's severed Address rather than a leak -- the row is live supply that no shed can
        // reach, and a Street returning re-indexes it on the next rebuild.
        if (carParks.Rows.IsLive(slot)
            && segments.Rows.TryResolve(carParks.WhereSegment[slot], out int segment))
        {
            new IndexList(_head, _tail, carParks.SegmentNext).InsertOrdered(segment, slot);
        }
    }

    /// <summary>Unlists a Car Park before its row is freed.</summary>
    /// <remarks>
    /// <b>Before the free rather than after, because the walk needs the row's Address</b> — a freed
    /// row's <c>WhereSegment</c> is whatever the next allocation writes, so unlisting afterwards
    /// would search the wrong Segment's list and leave the entry dangling for that next allocation
    /// to be inserted into twice. It is the same ordering constraint <c>BuildingsInCells.Remove</c>
    /// carries a few lines further down the same method, and for the same reason.
    /// </remarks>
    public void Remove(CarParkTable carParks, RoadSegmentTable segments, int slot)
    {
        ArgumentNullException.ThrowIfNull(carParks);
        ArgumentNullException.ThrowIfNull(segments);

        if (segments.Rows.TryResolve(carParks.WhereSegment[slot], out int segment)
            && (uint)segment < (uint)Segments)
        {
            new IndexList(_head, _tail, carParks.SegmentNext).Remove(segment, slot);
        }
    }

    /// <summary>Widens the head and tail arrays to cover every Segment slot that exists.</summary>
    /// <remarks>
    /// The Road Graph's slot count only grows within a world, so this never shrinks and never
    /// reorders — a Segment's index is its slot, and an index that moved would move every list.
    /// </remarks>
    private void Grow(RoadSegmentTable segments)
    {
        int wanted = segments.Rows.SlotCount;

        if (wanted <= Segments)
        {
            return;
        }

        if (_head.Length < wanted)
        {
            Array.Resize(ref _head, wanted);
            Array.Resize(ref _tail, wanted);
        }

        Segments = wanted;
    }
}
