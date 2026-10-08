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

    private bool OnReservedGround((long East, long North) at, int half)
    {
        var tile = (new Tiles((int)IntegerMath.ShiftRight(at.East, Fixed.FractionalBits)),
            new Tiles((int)IntegerMath.ShiftRight(at.North, Fixed.FractionalBits)));
        foreach (int segment in Roads.Residency.Near(tile.Item1, tile.Item2, new Tiles(half + 1)))
        {
            if (Roads.Segments.Rows.IsLive(segment) && (RoadKind)Roads.Segments.Kind[segment] != RoadKind.FootPath
                && Roads.Segments.Centerline[segment].DistanceTo(at.East, at.North) <= half * Fixed.One)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Live Lots whose parcel the Street's paved width crosses, in slot order.</summary>
    /// <remarks>
    /// The paved width is one rectangle per Tile of length, two half-widths deep and turned to the
    /// tangent there. A rectangle centered within a half-width of a Segment's centerline lies on that
    /// Segment's reserved ground, which every parcel beside it already includes, so it clears
    /// nothing. A Street that joins or crosses another at an angle therefore does not reach the Lots
    /// across it.
    /// </remarks>
    public List<int> LotsUnder(StreetArc line)
    {
        var under = new List<int>();
        int half = Rules.Lots.StreetHalfWidthTiles;
        if (half <= 0) { return under; }

        int steps = (int)IntegerMath.FloorDiv((long)line.Length + Fixed.One - 1, Fixed.One);
        var paved = new List<OrientedRectangle>(steps);
        int west = int.MaxValue, south = int.MaxValue, east = int.MinValue, north = int.MinValue;
        for (int step = 0; step < steps; step++)
        {
            int offset = Larger(0, Smaller(step * Fixed.One, line.Length - Fixed.One));
            if (OnReservedGround(line.PointAt(Smaller(offset + (Fixed.One >> 1), line.Length)), half)) { continue; }
            var point = line.PointAt(offset);
            var tangent = line.TangentAt(offset);
            var piece = new OrientedRectangle(
                (int)(point.East + (long)tangent.North * half), (int)(point.North - (long)tangent.East * half),
                tangent.East, tangent.North, 1, 2 * half);
            paved.Add(piece);
            LandRectangle bounds = piece.Bounds;
            west = Smaller(west, bounds.X);
            south = Smaller(south, bounds.Y);
            east = Larger(east, bounds.X + bounds.Width);
            north = Larger(north, bounds.Y + bounds.Height);
        }

        if (paved.Count == 0) { return under; }

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
    /// renamed and the original follows it. A Leg's first hop starts at its own Address and its last
    /// hop ends at one, so either keeps only the half it drives when that Address lies on the other
    /// side of the split. Addresses must already have moved. A Vehicle already on the hop keeps the
    /// arrival time it was priced at for the whole Segment.
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
                    bool first = hop == Legs.RouteHead[leg] - 1, last = hop == Legs.RouteTail[leg] - 1;
                    Handle<RoadSegment> start = Legs.FromSegment[leg], end = Legs.ToSegment[leg];
                    bool near = forward ? !(first && start == to) : !(last && end == to);
                    bool far = forward ? !(last && end == from) : !(first && start == from);
                    if (near && far)
                    {
                        int added = RouteHops.Rows.Resolve(RouteHops.Create(
                            Roads.Segments, Roads.Segments.Rows.Resolve(forward ? to : from), forward));
                        if (!forward) { RouteHops.Segment[hop] = to; }
                        Legs.Route(RouteHops).InsertAfter(leg, hop, added);
                    }
                    else if (far)
                    {
                        RouteHops.Segment[hop] = to;
                    }
                }

                hop = next;
            }
        }
    }
}
