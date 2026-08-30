using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>One in-flight Citizen, placed.</summary>
public readonly record struct VisibleAgent(
    Handle<Citizen> Citizen,
    SubTiles East,
    SubTiles North,
    TravelMode Mode,
    TripPurpose Purpose);

/// <summary>
/// <c>05 §2</c>'s <c>visible_agents(aabb, alpha)</c> — the transform half of the hot surface,
/// beside <see cref="MapLayers.LayerCells"/>.
/// </summary>
/// <remarks>
/// ⚠ <b>A walk and a drive are placed by different means, and the asymmetry is the cursor's.</b> A
/// foot Leg is priced once and holds no Segment, so its Traveller is interpolated between its Leg's
/// two Addresses — a straight line the walker did not walk. A drive holds its Segment and is placed
/// at the midpoint: <see cref="TravellerTable.ArrivesAt"/> is the <em>hop's</em>, and no entry
/// instant is stored, so a position within it would be invented. <c>alpha</c> therefore moves the
/// walkers and cannot move the drivers.
/// </remarks>
public static class VisibleAgents
{
    /// <summary>Fills <paramref name="into"/> with the Travellers inside <paramref name="area"/>.</summary>
    /// <remarks>Truncates rather than throwing, for <see cref="MapLayers.LayerCells"/>'s reason.</remarks>
    public static int In(World world, CellRect area, Ratio alpha, Span<VisibleAgent> into)
    {
        ArgumentNullException.ThrowIfNull(world);

        TravellerTable travellers = world.Travellers;
        LegTable legs = world.Legs;
        CellRect box = area.Clamp();
        int written = 0;

        for (int slot = 0; slot < travellers.Rows.SlotCount && written < into.Length; slot++)
        {
            if (!travellers.Rows.IsLive(slot))
            {
                continue;
            }

            int leg = travellers.CurrentLeg[slot];
            var mode = (TravelMode)legs.Mode[leg];

            if (!TryPlace(world, slot, leg, mode, alpha, out SubTiles east, out SubTiles north))
            {
                continue;
            }

            Cells atEast = CellGrid.ToCells(east.ToTilesFloor());
            Cells atNorth = CellGrid.ToCells(north.ToTilesFloor());

            if (atEast < box.East || atEast >= box.EastEnd
                || atNorth < box.North || atNorth >= box.NorthEnd)
            {
                continue;
            }

            into[written++] = new VisibleAgent(
                travellers.Citizen[slot],
                east,
                north,
                mode,
                Purpose(world, travellers.Trip[slot]));
        }

        return written;
    }

    /// <summary>Where a Traveller is, or false where the graph beneath it has gone.</summary>
    private static bool TryPlace(
        World world, int slot, int leg, TravelMode mode, Ratio alpha,
        out SubTiles east, out SubTiles north)
    {
        TravellerTable travellers = world.Travellers;
        int hop = travellers.CurrentHop[slot];

        if (mode != TravelMode.Foot && hop != Rows.NoSlot)
        {
            return TryMidpoint(world, world.RouteHops.Segment[hop], out east, out north);
        }

        LegTable legs = world.Legs;
        RoadSegmentTable segments = world.Roads.Segments;

        if (!TryPoint(world, legs.From(leg, segments), out SubTiles fromEast, out SubTiles fromNorth)
            || !TryPoint(world, legs.To(leg, segments), out SubTiles toEast, out SubTiles toNorth))
        {
            east = default;
            north = default;

            return false;
        }

        Ratio along = Travelled(travellers.ArrivesAt[slot], world.Tick, legs.Time[leg], alpha);

        east = Lerp(fromEast, toEast, along);
        north = Lerp(fromNorth, toNorth, along);

        return true;
    }

    /// <summary>How much of a Leg is behind the Traveller, clamped into its own duration.</summary>
    private static Ratio Travelled(Ticks arrivesAt, Ticks tick, TravelTime cost, Ratio alpha)
    {
        if (cost.Raw <= 0 || cost.IsImpassable)
        {
            return Ratio.One;
        }

        long remaining = arrivesAt.Raw > tick.Raw ? (long)(arrivesAt.Raw - tick.Raw) : 0;
        long left = Fixed.FromInt((int)Smaller(remaining, int.MaxValue >> Fixed.FractionalBits))
            - alpha.Raw;

        if (left <= 0)
        {
            return Ratio.One;
        }

        int done = Fixed.One - Fixed.Div((int)left, cost.Raw);

        return done <= 0 ? Ratio.Zero : done >= Fixed.One ? Ratio.One : new Ratio(done);
    }

    /// <summary>The smaller of two, without <c>Math</c>, which the core does not have.</summary>
    private static long Smaller(long left, long right) => left < right ? left : right;

    /// <summary>The centre of a Segment, which is where a driver is known to be.</summary>
    private static bool TryMidpoint(
        World world, Handle<RoadSegment> segment, out SubTiles east, out SubTiles north)
    {
        east = default;
        north = default;

        if (!world.Roads.Segments.Rows.TryResolve(segment, out int slot))
        {
            return false;
        }

        return TryEnds(world, slot, Ratio.FromFraction(1, 2), out east, out north);
    }

    /// <summary>An Address as a point: its Segment's line, taken to the Address's own offset.</summary>
    private static bool TryPoint(World world, Address address, out SubTiles east, out SubTiles north)
    {
        east = default;
        north = default;

        if (!address.Exists)
        {
            return false;
        }

        Tiles length = world.Roads.Segments.LengthTiles[address.Segment];
        Ratio along = length.Raw <= 0
            ? Ratio.Zero
            : Ratio.FromFraction(address.Offset.Raw, length.Raw);

        return TryEnds(world, address.Segment, along, out east, out north);
    }

    /// <summary>A point a given way along a Segment's own two Nodes.</summary>
    private static bool TryEnds(
        World world, int segment, Ratio along, out SubTiles east, out SubTiles north)
    {
        east = default;
        north = default;

        RoadSegmentTable segments = world.Roads.Segments;
        RoadNodeTable nodes = world.Roads.Nodes;

        if (!nodes.Rows.TryResolve(segments.NodeA[segment], out int a)
            || !nodes.Rows.TryResolve(segments.NodeB[segment], out int b))
        {
            return false;
        }

        east = Lerp(
            SubTiles.FromTiles(nodes.East[a]), SubTiles.FromTiles(nodes.East[b]), along);
        north = Lerp(
            SubTiles.FromTiles(nodes.North[a]), SubTiles.FromTiles(nodes.North[b]), along);

        return true;
    }

    /// <summary>Fixed-point interpolation between two positions.</summary>
    private static SubTiles Lerp(SubTiles from, SubTiles to, Ratio along) =>
        new(Fixed.Lerp(from.Raw, to.Raw, along.Raw));

    /// <summary>A Trip's Purpose, or the unset value where the Trip has gone.</summary>
    private static TripPurpose Purpose(World world, Handle<Trip> trip) =>
        world.Trips.Rows.TryResolve(trip, out int slot)
            ? (TripPurpose)world.Trips.Purpose[slot]
            : default;
}
