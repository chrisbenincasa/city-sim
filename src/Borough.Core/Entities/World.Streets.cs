using Borough.Core.Arithmetic;
using Borough.Core.Movement;
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
    /// Addresses past a split move to the Segment the split created, and every planned route over
    /// a split Segment gains a hop for the created half. The new Street seals its ground, fronts the
    /// unfronted Lots beside it, and the land is re-lotted.
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
            SplitRouteHops(from, to);
        }

        foreach (int segment in laid) { StreetSealing.Seal(Layers, Roads.Segments.Centerline[segment]); }

        Space.Frontage.AttachTo(Lots, Roads, laid.ToArray());
        Frontage.Rebuild(Lots);
        CarParksOnSegments.Rebuild(CarParks, Roads.Segments);
        LotSubdivider.Resubdivide(this);
        return StreetLayRefusal.None;
    }

    /// <summary>Live Lots whose parcel the Street's paved width crosses, in slot order.</summary>
    /// <remarks>
    /// The paved width is one rectangle per Tile of length, two half-widths deep and turned to the
    /// tangent there. The last rectangle ends exactly at the far end, so a Street ending on another
    /// does not reach the Lots across it.
    /// </remarks>
    public List<int> LotsUnder(StreetArc line)
    {
        var under = new List<int>();
        int half = Rules.Lots.StreetHalfWidthTiles;
        if (half <= 0) { return under; }

        int steps = (int)IntegerMath.FloorDiv((long)line.Length + Fixed.One - 1, Fixed.One);
        var paved = new OrientedRectangle[steps];
        int west = int.MaxValue, south = int.MaxValue, east = int.MinValue, north = int.MinValue;
        for (int step = 0; step < steps; step++)
        {
            int offset = Larger(0, Smaller(step * Fixed.One, line.Length - Fixed.One));
            var point = line.PointAt(offset);
            var tangent = line.TangentAt(offset);
            paved[step] = new OrientedRectangle(
                (int)(point.East + (long)tangent.North * half), (int)(point.North - (long)tangent.East * half),
                tangent.East, tangent.North, 1, 2 * half);
            LandRectangle bounds = paved[step].Bounds;
            west = Smaller(west, bounds.X);
            south = Smaller(south, bounds.Y);
            east = Larger(east, bounds.X + bounds.Width);
            north = Larger(north, bounds.Y + bounds.Height);
        }

        var box = new LandRectangle(Larger(0, west), Larger(0, south), east - Larger(0, west), north - Larger(0, south));
        for (int slot = 0; slot < Lots.Rows.SlotCount; slot++)
        {
            if (!Lots.Rows.IsLive(slot)) { continue; }
            OrientedRectangle parcel = Lots.Parcel(slot);
            if (!Overlaps(box, parcel)) { continue; }
            foreach (OrientedRectangle piece in paved)
            {
                if (piece.Overlaps(parcel)) { under.Add(slot); break; }
            }
        }

        return under;
    }

    private static int Smaller(int a, int b) => a < b ? a : b;

    private static int Larger(int a, int b) => a > b ? a : b;

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

    /// <summary>Gives every route over <paramref name="from"/> a hop over the half split off into <paramref name="to"/>.</summary>
    /// <remarks>
    /// <paramref name="from"/> keeps the part nearer its NodeA. A forward hop crosses it and then
    /// <paramref name="to"/>; a backward hop crosses <paramref name="to"/> first, so that hop is
    /// renamed and the original follows it. A Vehicle already on the hop keeps the arrival time it
    /// was priced at for the whole Segment.
    /// </remarks>
    private void SplitRouteHops(Handle<RoadSegment> from, Handle<RoadSegment> to)
    {
        for (int leg = 0; leg < Legs.Rows.SlotCount; leg++)
        {
            if (!Legs.Rows.IsLive(leg)) { continue; }
            int hop = Legs.RouteHead[leg] - 1;
            while (hop >= 0)
            {
                int next = RouteHops.Next[hop] - 1;
                if (RouteHops.Segment[hop] == from)
                {
                    bool forward = RouteHops.Forward[hop] != 0;
                    int added = RouteHops.Rows.Resolve(RouteHops.Create(
                        Roads.Segments, Roads.Segments.Rows.Resolve(forward ? to : from), forward));
                    if (!forward) { RouteHops.Segment[hop] = to; }
                    Legs.Route(RouteHops).InsertAfter(leg, hop, added);
                }

                hop = next;
            }
        }
    }
}
