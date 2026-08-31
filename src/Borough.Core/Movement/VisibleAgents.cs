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
/// <para>
/// <b>A walk and a drive are placed by different means, and the asymmetry is the cursor's.</b> A
/// drive holds the Segment it is on and is placed at its midpoint —
/// <see cref="TravellerTable.ArrivesAt"/> is the <em>hop's</em> and no entry instant is stored, so a
/// position within it would be invented. A walk is priced once and its cursor holds no Segment at
/// all, so it is placed along <b>its Leg's own recorded route</b>, at the share of the route's
/// length its elapsed time has bought. <c>alpha</c> therefore moves the walkers and cannot move the
/// drivers.
/// </para>
/// <para>
/// ⚠ <b>Time buys distance at one rate because a pedestrian has one pace</b> — <c>RoadArcs</c> takes
/// the minimum against walking pace for the foot mode, so no Segment on a walk is faster than
/// another and the time fraction <em>is</em> the distance fraction.
/// </para>
/// <para>
/// 🔴 <b>THE STRAIGHT LINE THIS REPLACED WAS DOCUMENTED AND STILL FOOLED EVERYONE.</b> Its own
/// remark said <em>a straight line the walker did not walk</em>; the shell drew the walkers floating
/// in the middle of the blocks and that was the first time anybody minded. ***A caveat in a remark
/// is not a caveat anybody has seen.***
/// </para>
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
        Ratio along = Travelled(travellers.ArrivesAt[slot], world.Tick, legs.Time[leg], alpha);

        if (TryAlongRoute(world, leg, along, out east, out north))
        {
            return true;
        }

        // No route: an impassable Leg, or one whose Segments were bulldozed under it. The two
        // Addresses are all that is left, and a straight line between them is where the walker
        // WOULD be if the city still had a path for them.
        if (!TryPoint(world, legs.From(leg, segments), out SubTiles fromEast, out SubTiles fromNorth)
            || !TryPoint(world, legs.To(leg, segments), out SubTiles toEast, out SubTiles toNorth))
        {
            east = default;
            north = default;

            return false;
        }

        east = Lerp(fromEast, toEast, along);
        north = Lerp(fromNorth, toNorth, along);

        return true;
    }

    /// <summary>
    /// A point the given share of the way along a Leg's recorded route, measured in Tiles walked.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Two passes over the hop list rather than one.</b> The first totals the route's length,
    /// which is not stored anywhere; the second finds the hop the share lands in. Storing the total
    /// would be a column that exists for the renderer.
    /// </remarks>
    private static bool TryAlongRoute(
        World world, int leg, Ratio along, out SubTiles east, out SubTiles north)
    {
        east = default;
        north = default;

        RouteHopTable hops = world.RouteHops;
        RoadSegmentTable segments = world.Roads.Segments;
        long total = 0;

        foreach (int hop in world.Legs.Route(hops).Walk(leg))
        {
            if (segments.Rows.TryResolve(hops.Segment[hop], out int segment))
            {
                total += segments.LengthTiles[segment].Raw;
            }
        }

        if (total <= 0)
        {
            return false;
        }

        long wanted = IntegerMath.ShiftRight(total * along.Raw, Fixed.FractionalBits);
        long walked = 0;
        int last = Rows.NoSlot;
        int lastHop = Rows.NoSlot;

        foreach (int hop in world.Legs.Route(hops).Walk(leg))
        {
            if (!segments.Rows.TryResolve(hops.Segment[hop], out int segment))
            {
                continue;
            }

            long length = segments.LengthTiles[segment].Raw;

            last = segment;
            lastHop = hop;

            if (walked + length < wanted)
            {
                walked += length;
                continue;
            }

            return TryEnds(world, segment, Into(hops, hop, wanted - walked, length), out east, out north);
        }

        // The share landed past the end, which rounding at the last hop can do.
        return last != Rows.NoSlot
            && TryEnds(world, last, Into(hops, lastHop, 1, 1), out east, out north);
    }

    /// <summary>
    /// How far into one hop's Segment a walker is, in the direction the hop was crossed.
    /// </summary>
    /// <remarks>
    /// The hop records which end it was entered by, so a route that doubles back reads the Segment
    /// in the direction the walker actually crossed it.
    /// </remarks>
    private static Ratio Into(RouteHopTable hops, int hop, long into, long length)
    {
        Ratio share = length <= 0
            ? Ratio.Zero
            : Clamp(new Ratio((int)IntegerMath.FloorDiv(
                IntegerMath.ShiftLeft(into, Fixed.FractionalBits), length)));

        return hops.Forward[hop] != 0 ? share : Ratio.One - share;
    }

    /// <summary>A share held inside its own Segment.</summary>
    private static Ratio Clamp(Ratio value) =>
        value.Raw <= 0 ? Ratio.Zero : value.Raw >= Fixed.One ? Ratio.One : value;

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
