using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

public sealed partial class World
{
    /// <summary>
    /// Lays a freeform Street and brings everything that names a Segment along with it.
    /// </summary>
    /// <remarks>
    /// Addresses past a split move to the Segment the split created. The new Street seals its
    /// ground, fronts the unfronted Lots beside it, and the land is re-lotted.
    /// </remarks>
    public StreetLayRefusal LayStreet(int aEast, int aNorth, int bEast, int bNorth, int sagitta)
    {
        var splits = new List<SegmentSplit>();
        var laid = new List<int>();
        StreetLayRefusal refusal = Roads.LayStreet(aEast, aNorth, bEast, bNorth, sagitta, splits, laid);
        if (refusal != StreetLayRefusal.None) { return refusal; }

        foreach (SegmentSplit split in splits)
        {
            Space.Frontage.Split(Lots, Roads.Segments, split.Original, split.Created, split.Retained);
            Handle<RoadSegment> from = Roads.Segments.Rows.At(split.Original), to = Roads.Segments.Rows.At(split.Created);
            MoveAddresses(CarParks.Rows, CarParks.WhereSegment, CarParks.WhereOffset, from, to, split.Retained);
            MoveAddresses(Trips.Rows, Trips.OriginSegment, Trips.OriginOffset, from, to, split.Retained);
            MoveAddresses(Trips.Rows, Trips.DestinationSegment, Trips.DestinationOffset, from, to, split.Retained);
            MoveAddresses(Legs.Rows, Legs.FromSegment, Legs.FromOffset, from, to, split.Retained);
            MoveAddresses(Legs.Rows, Legs.ToSegment, Legs.ToOffset, from, to, split.Retained);
        }

        foreach (int segment in laid) { StreetSealing.Seal(Layers, Roads.Segments.Centerline[segment]); }

        Space.Frontage.AttachTo(Lots, Roads, laid.ToArray());
        Frontage.Rebuild(Lots);
        CarParksOnSegments.Rebuild(CarParks, Roads.Segments);
        LotSubdivider.Resubdivide(this);
        return StreetLayRefusal.None;
    }

    private static void MoveAddresses<TRow>(Rows<TRow> rows, HandleColumn<RoadSegment> segment, Column<Tiles> offset,
        Handle<RoadSegment> from, Handle<RoadSegment> to, Tiles retained) where TRow : unmanaged
    {
        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!rows.IsLive(slot) || segment[slot] != from || offset[slot].Raw <= retained.Raw) { continue; }
            segment[slot] = to;
            offset[slot] = new Tiles(offset[slot].Raw - retained.Raw);
        }
    }
}
