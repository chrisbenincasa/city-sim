using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Entities;

/// <summary>Carves genuinely free ground and preserves saved realised parcels.</summary>
public static class LotSubdivider
{
    public static int SubdivideAt(World world, Tiles east, Tiles north, ushort zone) => PaintAt(world, east, north, zone);

    public static int PaintAt(World world, Tiles east, Tiles north, ushort zone)
    {
        ArgumentNullException.ThrowIfNull(world);
        var streets = world.Roads.Streets;
        if (streets.Blocks <= 0 || east.Raw < 0 || north.Raw < 0) { return 0; }
        int column = streets.Lattice.LineAt(east.Raw), row = streets.Lattice.LineAt(north.Raw);
        if (!world.ZoneBlock(column, row, zone)) { return 0; }
        return zone == 0 ? 0 : CarveBlock(world, column, row);
    }

    public static bool Contains(Parcel parcel, Tiles east, Tiles north) =>
        parcel.Geometry.Contains(Fixed.FromInt(east.Raw), Fixed.FromInt(north.Raw));

    public static int PreviewCapacity(World world, int column, int row)
    {
        int count = world.Rules.Lots.ParcelCeiling(BlockGround.At(world.Roads.Lattice, column, row));
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
            if (OnBlock(world, slot, column, row, out _)) { count++; }
        return count;
    }

    /// <summary>Realised parcels first, then proposed parcels only where no saved Lot owns ground.</summary>
    public static int Preview(World world, int column, int row, Span<Parcel> into)
    {
        var streets = world.Roads.Streets;
        if (column < 0 || row < 0 || column >= streets.Blocks || row >= streets.Blocks) { return 0; }
        BlockGround ground = BlockGround.At(streets.Lattice, column, row);
        int count = 0;
        var lots = world.Lots;
        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!OnBlock(world, slot, column, row, out BlockFace face)) { continue; }
            int offset = face is BlockFace.South or BlockFace.North ? lots.East[slot].Raw - ground.East : lots.North[slot].Raw - ground.North;
            if (count == into.Length) { throw new ArgumentException("Preview buffer is smaller than PreviewCapacity.", nameof(into)); }
            into[count++] = new(face, (StreetSide)lots.Side[slot], new Tiles(offset), lots.Parcel(slot));
        }
        Span<int> sides = stackalloc int[4];
        ground = ground with { Patch = Sides(world, column, row, sides) };
        int ceiling = world.Rules.Lots.ParcelCeiling(ground);
        Span<Parcel> proposed = ceiling <= 128 ? stackalloc Parcel[128] : new Parcel[ceiling];
        BlockPattern pattern = Pattern(world, column, row, ground.Patch);
        int carved = world.Rules.Lots.Carve(world.Key, pattern, ground, proposed);
        ClaimOrder(world, sides, proposed[..carved]);
        for (int i = 0; i < carved; i++)
        {
            Parcel parcel = proposed[i];
            if (sides[(int)parcel.Face] == Rows.NoSlot || !Claim(world, ground, BlockPatterns.FormOf(pattern, parcel.Face), ref parcel)) { continue; }
            if (count == into.Length) { throw new ArgumentException("Preview buffer is smaller than PreviewCapacity.", nameof(into)); }
            into[count++] = parcel;
        }
        return count;
    }

    /// <summary>Read-only hit test, including realised parcels whose Street has disappeared.</summary>
    public static bool ParcelAt(World world, Tiles east, Tiles north, out Parcel parcel)
    {
        parcel = default;
        var streets = world.Roads.Streets;
        if (streets.Blocks <= 0 || east.Raw < 0 || north.Raw < 0 || east.Raw >= CellGrid.WorldTiles || north.Raw >= CellGrid.WorldTiles) { return false; }
        int column = streets.Lattice.LineAt(east.Raw), row = streets.Lattice.LineAt(north.Raw);
        if (column >= streets.Blocks || row >= streets.Blocks) { return false; }
        int ceiling = PreviewCapacity(world, column, row);
        Span<Parcel> parcels = ceiling <= 128 ? stackalloc Parcel[128] : new Parcel[ceiling];
        int count = Preview(world, column, row, parcels);
        for (int i = 0; i < count; i++)
            if (Contains(parcels[i], east, north)) { parcel = parcels[i]; return true; }
        return false;
    }

    public static int PaintParcelAt(World world, Tiles east, Tiles north, ushort zone)
    {
        if (!ParcelAt(world, east, north, out Parcel parcel)) { return 0; }
        LandRectangle area = Ground(parcel);
        LandPermissionSummary before = world.LandPermissions.Summary(area);
        if (world.PaintUsePermissions(area, zone) != PermissionRefusal.None) { return 0; }
        int column = world.Roads.Lattice.LineAt(east.Raw), row = world.Roads.Lattice.LineAt(north.Raw);
        // Only subdivision of free ground can add rows; existing realised parcels never move.
        CarveBlock(world, column, row);
        return before.CommonUses == zone && before.AnyUses == zone ? 0 : 1;
    }

    internal static int CornerTiles(int blockTiles, int lotsPerSegment) => BlockPatterns.StripTiles(blockTiles, lotsPerSegment);

    /// <param name="form">A pattern that replaces the one the block's band and anchor would draw.</param>
    public static int SubdivideBlock(World world, int column, int row, ushort zone, BlockPattern? form = null)
    {
        if (!world.ZoneBlock(column, row, zone)) { return 0; }
        return CarveBlock(world, column, row, zone == LotTable.Trade && world.Rules.Lots.TradeFormsByBand, form);
    }

    // Only world creation draws a trade form. The Zone Rule engine cannot build on one, so a block
    // zoned for trade in play keeps the housing ladder.
    private static BlockPattern Pattern(World world, int column, int row, ulong patch, bool tradeForm = false)
    {
        LandPermissionSummary permission = world.LandPermissions.Summary(world.BlockGroundRectangle(column, row));
        byte band = permission.MixedIntensity ? (byte)0 : permission.Band;
        return tradeForm
            ? BlockPatterns.TradeForm(
                band, world.Rules.Bands.Length, world.Rules.Lots.TradeFormWeights, world.Key, patch)
            : BlockPatterns.ForBand(band, world.Rules.Bands.Length, world.Roads.Streets.BlockTiles,
                world.Rules.Lots.LotsPerSegment, world.Key, patch, world.Rules.Lots.PatternSpread);
    }

    private static int CarveBlock(World world, int column, int row, bool tradeForm = false, BlockPattern? form = null)
    {
        var plots = new List<Plot>();
        GatherBlock(world, column, row, plots, tradeForm, form);
        return Settle(world, plots);
    }

    // A candidate plot. A lattice plot keeps its block ground; a rotated plot keeps its front address.
    private readonly record struct Plot(ulong SegmentId, StreetSide Side, int Offset, int Order, int Segment,
        BlockPattern Form, Parcel Parcel, BlockGround Ground, Tiles AddressEast, Tiles AddressNorth, bool Rotated);

    private static void GatherBlock(World world, int column, int row, List<Plot> into, bool tradeForm = false, BlockPattern? form = null)
    {
        LandRectangle area = world.BlockGroundRectangle(column, row);
        if (!area.IsValid || world.LandPermissions.Summary(area).AnyUses == 0) { return; }
        Span<int> sides = stackalloc int[4];
        BlockGround ground = BlockGround.At(world.Roads.Streets.Lattice, column, row) with { Patch = Sides(world, column, row, sides) };
        BlockPattern pattern = form ?? Pattern(world, column, row, ground.Patch, tradeForm);
        int ceiling = world.Rules.Lots.ParcelCeiling(ground);
        Span<Parcel> parcels = ceiling <= 128 ? stackalloc Parcel[128] : new Parcel[ceiling];
        int count = world.Rules.Lots.Carve(world.Key, pattern, ground, parcels);
        for (int i = 0; i < count; i++)
        {
            Parcel parcel = parcels[i];
            int segment = sides[(int)parcel.Face];
            if (segment == Rows.NoSlot) { continue; }
            into.Add(new Plot(SegmentId(world, segment), parcel.Side, parcel.Offset.Raw, into.Count, segment,
                BlockPatterns.FormOf(pattern, parcel.Face), parcel, ground, default, default, false));
        }
    }

    // New plots claim ground by Segment id, then side, then offset along the Segment, across every
    // carver feeding one pass.
    private static int Settle(World world, List<Plot> plots)
    {
        plots.Sort(static (a, b) =>
            a.SegmentId != b.SegmentId ? a.SegmentId.CompareTo(b.SegmentId)
            : a.Side != b.Side ? ((byte)a.Side).CompareTo((byte)b.Side)
            : a.Offset != b.Offset ? a.Offset.CompareTo(b.Offset)
            : a.Order.CompareTo(b.Order));
        int created = 0;
        foreach (Plot plot in plots)
        {
            if (plot.Rotated ? CreateRotated(world, plot) : CreateLattice(world, plot)) { created++; }
        }
        if (created > 0) { world.LotsAdmitting.Invalidate(); }
        return created;
    }

    private static bool CreateLattice(World world, Plot plot)
    {
        Parcel parcel = plot.Parcel;
        BlockGround ground = plot.Ground;
        if (!Claim(world, ground, plot.Form, ref parcel)) { return false; }
        LandPermissionSummary permission = world.LandPermissions.Summary(Ground(parcel));
        // A parcel can be selected/painted before it is zoned; unzoned free ground stays unplatted.
        if (permission.AnyUses == 0) { return false; }
        var address = parcel.Address(ground);
        int slot = world.Lots.Rows.Resolve(world.Lots.Create(address.East, address.North, permission.CommonUses, parcel.Side));
        world.Lots.Front(slot, world.Roads.Segments.Rows.At(plot.Segment), parcel.Offset);
        world.Lots.ParcelEastQ16[slot] = Fixed.FromInt(parcel.East.Raw); world.Lots.ParcelNorthQ16[slot] = Fixed.FromInt(parcel.North.Raw);
        world.Lots.ParcelWide[slot] = parcel.Wide; world.Lots.ParcelDeep[slot] = parcel.Deep;
        var foot = world.Rules.Lots.Footprint(world.Key, parcel, ground, plot.Form);
        world.Lots.FootprintEastQ16[slot] = Fixed.FromInt(foot.East.Raw); world.Lots.FootprintNorthQ16[slot] = Fixed.FromInt(foot.North.Raw);
        world.Lots.FootprintWide[slot] = foot.Wide; world.Lots.FootprintDeep[slot] = foot.Deep;
        world.Lots.Storeys[slot] = world.Rules.Lots.Height(world.Key, parcel, plot.Form, world.Roads.Streets.BlockTiles);
        world.Lots.PodiumStoreys[slot] = world.Rules.Lots.PodiumOn(world.Key, parcel.East, parcel.North);
        world.Lots.Pattern[slot] = (byte)((byte)plot.Form + 1);
        world.Frontage.Claim(plot.Segment, parcel.Side);
        return true;
    }

    private static bool CreateRotated(World world, Plot plot)
    {
        var rules = world.Rules.Lots;
        OrientedRectangle ground = plot.Parcel.Geometry;
        if (!ClaimRotated(world, plot.AddressEast, plot.AddressNorth, plot.Side, ref ground)) { return false; }
        LandPermissionSummary here = world.LandPermissions.Summary(ground.Bounds);
        if (here.AnyUses == 0) { return false; }

        int slot = world.Lots.Rows.Resolve(world.Lots.Create(plot.AddressEast, plot.AddressNorth, here.CommonUses, plot.Side));
        world.Lots.Front(slot, world.Roads.Segments.Rows.At(plot.Segment), plot.Parcel.Offset);
        world.Lots.ParcelEastQ16[slot] = ground.EastQ16; world.Lots.ParcelNorthQ16[slot] = ground.NorthQ16;
        world.Lots.AxisEastQ16[slot] = ground.AxisEastQ16; world.Lots.AxisNorthQ16[slot] = ground.AxisNorthQ16;
        world.Lots.ParcelWide[slot] = new Tiles(ground.Wide); world.Lots.ParcelDeep[slot] = new Tiles(ground.Deep);
        OrientedRectangle foot = rules.Footprint(world.Key, ground);
        world.Lots.FootprintEastQ16[slot] = foot.EastQ16; world.Lots.FootprintNorthQ16[slot] = foot.NorthQ16;
        world.Lots.FootprintWide[slot] = new Tiles(foot.Wide); world.Lots.FootprintDeep[slot] = new Tiles(foot.Deep);
        var parcel = plot.Parcel with { Geometry = ground };
        world.Lots.Storeys[slot] = rules.Height(world.Key, parcel, plot.Form, world.Roads.Streets.BlockTiles);
        world.Lots.PodiumStoreys[slot] = rules.PodiumOn(world.Key, parcel.East, parcel.North);
        world.Lots.Pattern[slot] = (byte)((byte)plot.Form + 1);
        world.Frontage.Claim(plot.Segment, plot.Side);
        return true;
    }

    private static LandRectangle Ground(Parcel p) => new(p.East.Raw, p.North.Raw, p.Wide.Raw, p.Deep.Raw);

    // New plots claim ground by Segment id, then side, then offset along the Segment.
    private static void ClaimOrder(World world, ReadOnlySpan<int> sides, Span<Parcel> parcels)
    {
        for (int i = 1; i < parcels.Length; i++)
        {
            for (int j = i; j > 0 && Before(world, sides, parcels[j], parcels[j - 1]); j--)
            {
                (parcels[j], parcels[j - 1]) = (parcels[j - 1], parcels[j]);
            }
        }
    }

    private static bool Before(World world, ReadOnlySpan<int> sides, Parcel left, Parcel right)
    {
        ulong a = SegmentId(world, sides[(int)left.Face]), b = SegmentId(world, sides[(int)right.Face]);
        if (a != b) { return a < b; }
        if (left.Side != right.Side) { return left.Side < right.Side; }
        return left.Offset.Raw < right.Offset.Raw;
    }

    private static ulong SegmentId(World world, int segment) =>
        segment == Rows.NoSlot ? ulong.MaxValue : world.Roads.Segments.Rows.IdAt(segment);

    // A plot overlapping a standing Lot loses depth from its back edge, one Tile at a time, down to
    // [lots] min_plot_depth_tiles; if it is still not free, it is dropped. Trade forms never shrink,
    // because their footprints are laid out on the whole block rather than on the parcel.
    private static bool Claim(World world, BlockGround ground, BlockPattern form, ref Parcel parcel)
    {
        if (Free(world, parcel, ground)) { return true; }
        int minimum = world.Rules.Lots.MinPlotDepthTiles;
        if (minimum == 0 || (int)form >= BlockPatterns.Count) { return false; }
        bool horizontal = parcel.Face is BlockFace.South or BlockFace.North;
        int depth = horizontal ? parcel.Deep.Raw : parcel.Wide.Raw;

        // ponytail: one overlap scan per Tile of depth. Compute the cut from the blocking Lots if
        // carving shows up in a profile.
        for (int shallower = depth - 1; shallower >= minimum; shallower--)
        {
            Parcel candidate = Shallower(parcel, shallower);
            if (Free(world, candidate, ground)) { parcel = candidate; return true; }
        }
        return false;
    }

    private static Parcel Shallower(Parcel p, int depth) => p.Face switch
    {
        BlockFace.South => new(p.Face, p.Side, p.Offset, p.East, p.North, p.Wide, new Tiles(depth)),
        BlockFace.North => new(p.Face, p.Side, p.Offset, p.East, new Tiles(p.North.Raw + p.Deep.Raw - depth), p.Wide, new Tiles(depth)),
        BlockFace.West => new(p.Face, p.Side, p.Offset, p.East, p.North, new Tiles(depth), p.Deep),
        _ => new(p.Face, p.Side, p.Offset, new Tiles(p.East.Raw + p.Wide.Raw - depth), p.North, new Tiles(depth), p.Deep),
    };

    private static bool Free(World world, Parcel parcel, BlockGround ground)
    {
        var address = parcel.Address(ground);
        LandRectangle candidate = Ground(parcel);
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)) { continue; }
            if (World.Overlaps(candidate, world.Lots.Parcel(slot))
                || (world.Lots.East[slot] == address.East && world.Lots.North[slot] == address.North && world.Lots.Side[slot] == (byte)parcel.Side)) { return false; }
        }
        return true;
    }

    private static bool OnBlock(World world, int slot, int column, int row, out BlockFace face)
    {
        face = default;
        return world.Lots.Rows.IsLive(slot) && Frontage.BlockOf(world.Roads.Streets, world.Lots.East[slot], world.Lots.North[slot],
            (StreetSide)world.Lots.Side[slot], out int c, out int r, out face) && c == column && r == row;
    }

    // The Street Segment on each side of the block, and the block's anchor as a draw patch. A block
    // that is exactly one closed face takes the face's anchor. Any other block takes the Street
    // lying along each side, which carves its open sides as roadside strips, and anchors on the
    // lowest Segment id among them.
    private static ulong Sides(World world, int column, int row, Span<int> sides)
    {
        int face = FaceSides(world, column, row, sides);
        if (face >= 0)
        {
            var (id, side) = world.Roads.Faces.Anchor(face);
            return Patch(id, side);
        }

        RoadGraph roads = world.Roads;
        BlockGround ground = BlockGround.At(roads.Lattice, column, row);
        int west = ground.East, south = ground.North, east = ground.East + ground.Wide, north = ground.North + ground.Deep;
        sides[(int)BlockFace.South] = StreetAlong(roads, west, south, east, south);
        sides[(int)BlockFace.North] = StreetAlong(roads, west, north, east, north);
        sides[(int)BlockFace.West] = StreetAlong(roads, west, south, west, north);
        sides[(int)BlockFace.East] = StreetAlong(roads, east, south, east, north);
        ulong patch = ulong.MaxValue;
        for (BlockFace each = BlockFace.South; each <= BlockFace.East; each++)
        {
            if (sides[(int)each] == Rows.NoSlot) { continue; }
            byte side = BlockPatterns.SideOf(each) == StreetSide.Left ? (byte)0 : (byte)1;
            ulong candidate = Patch(roads.Segments.Rows.IdAt(sides[(int)each]), side);
            if (candidate < patch) { patch = candidate; }
        }
        return patch;
    }

    // The straight Street whose ends are exactly one edge of a block, in either direction.
    private static int StreetAlong(RoadGraph roads, int fromEast, int fromNorth, int toEast, int toNorth)
    {
        (long East, long North) from = (Fixed.FromInt(fromEast), Fixed.FromInt(fromNorth));
        (long East, long North) to = (Fixed.FromInt(toEast), Fixed.FromInt(toNorth));
        var middle = (East: new Tiles(IntegerMath.FloorDiv(fromEast + toEast, 2)), North: new Tiles(IntegerMath.FloorDiv(fromNorth + toNorth, 2)));
        foreach (int segment in roads.Residency.Near(middle.East, middle.North, Tiles.Zero))
        {
            if ((RoadKind)roads.Segments.Kind[segment] != RoadKind.Street) { continue; }
            StreetArc line = roads.Segments.Centerline[segment];
            if (line.IsStraight && ((line.A == from && line.B == to) || (line.A == to && line.B == from))) { return segment; }
        }
        return Rows.NoSlot;
    }

    private static ulong Patch(ulong segmentId, byte side) => (segmentId << 1) | side;

    private static int FaceSides(World world, int column, int row, Span<int> sides)
    {
        RoadGraph roads = world.Roads;
        BlockGround ground = BlockGround.At(roads.Lattice, column, row);
        long west = Fixed.FromInt(ground.East), south = Fixed.FromInt(ground.North);
        long east = Fixed.FromInt(ground.East + ground.Wide), north = Fixed.FromInt(ground.North + ground.Deep);
        int face = roads.Faces.Find(Fixed.FromInt(ground.East + IntegerMath.FloorDiv(ground.Wide, 2)),
            Fixed.FromInt(ground.North + IntegerMath.FloorDiv(ground.Deep, 2)));
        if (face < 0 || roads.Faces.Boundary(face).Length != 4) { return -1; }
        foreach (int arc in roads.Faces.Boundary(face))
        {
            int segment = roads.Arcs.Segment[arc];
            StreetArc line = roads.Segments.Centerline[segment];
            var (from, to) = roads.Faces.SideOf(arc) == 0 ? (line.A, line.B) : (line.B, line.A);
            BlockFace side;
            if (!line.IsStraight) { return -1; }
            else if (from.East == west && from.North == south && to.East == east && to.North == south) { side = BlockFace.South; }
            else if (from.East == east && from.North == south && to.East == east && to.North == north) { side = BlockFace.East; }
            else if (from.East == east && from.North == north && to.East == west && to.North == north) { side = BlockFace.North; }
            else if (from.East == west && from.North == north && to.East == west && to.North == south) { side = BlockFace.West; }
            else { return -1; }
            sides[(int)side] = segment;
        }
        return face;
    }

    /// <summary>The pattern a block's standing Lots were carved with, or false when it has none.</summary>
    /// <remarks>
    /// Read from the Lots' saved forms. A car-park centre's pads report the centre. Otherwise the
    /// densest form on the ladder wins.
    /// </remarks>
    public static bool PatternOn(World world, int column, int row, out BlockPattern pattern)
    {
        pattern = BlockPattern.Detached;
        bool carved = false;
        int tiles = world.Roads.Streets.BlockTiles, perSegment = world.Rules.Lots.LotsPerSegment;
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!OnBlock(world, lot, column, row, out _)) { continue; }
            BlockPattern form = world.Lots.PatternOf(lot);
            if (form == BlockPattern.PadSite) { form = BlockPattern.CarParkCentre; }
            if (!carved || BlockPatterns.Rung(form, tiles, perSegment) > BlockPatterns.Rung(pattern, tiles, perSegment)) { pattern = form; }
            carved = true;
        }
        return carved;
    }

    /// <summary>Explicit whole-vacant-block replat. Street edits do not invoke this operation.</summary>
    public static int RecarveBlock(World world, int column, int row)
    {
        LandRectangle area = world.BlockGroundRectangle(column, row);
        if (!area.IsValid || !PatternOn(world, column, row, out BlockPattern old)) { return 0; }
        LandPermissionSummary permission = world.LandPermissions.Summary(area);
        if (permission.MixedPermissions || old == BlockPattern.CarParkCentre) { return 0; }
        Span<int> sides = stackalloc int[4];
        BlockPattern wanted = BlockPatterns.ForBand(permission.Band, world.Rules.Bands.Length, world.Roads.Streets.BlockTiles,
            world.Rules.Lots.LotsPerSegment, world.Key, Sides(world, column, row, sides), world.Rules.Lots.PatternSpread);
        if (BlockPatterns.Rung(wanted, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment)
            <= BlockPatterns.Rung(old, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment)) { return 0; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (OnBlock(world, lot, column, row, out _) && !world.Lots.IsVacant(lot)) { return 0; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (OnBlock(world, lot, column, row, out _)) { world.Lots.Rows.Free(world.Lots.Rows.At(lot)); }
        world.Frontage.Rebuild(world.Lots);
        world.LotsAdmitting.Invalidate();
        return CarveBlock(world, column, row, form: wanted);
    }

    public static (int Created, int Freed) Resubdivide(World world)
    {
        int freed = 0, created = 0;
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || world.Lots.HasFrontage(slot) || !world.Lots.IsVacant(slot)) { continue; }
            world.Lots.Rows.Free(world.Lots.Rows.At(slot)); freed++;
        }
        if (freed > 0)
        {
            world.LotsAdmitting.Invalidate();
            world.Frontage.Rebuild(world.Lots);
        }
        // Permission geometry outlives Lots. Visit ground on either side of
        // every live Street; no paint is copied from an old Lot during restoration.
        var roads = world.Roads;
        var visited = new byte[roads.Streets.Blocks * roads.Streets.Blocks];
        var plots = new List<Plot>();
        for (int segment = 0; segment < roads.Segments.Rows.SlotCount; segment++)
        {
            if (!roads.Segments.Rows.IsLive(segment) || (RoadKind)roads.Segments.Kind[segment] != RoadKind.Street) { continue; }
            int a = roads.Nodes.Rows.Resolve(roads.Segments.NodeA[segment]), b = roads.Nodes.Rows.Resolve(roads.Segments.NodeB[segment]);
            int column = roads.Lattice.LineAt(roads.Nodes.East[a].Raw), row = roads.Lattice.LineAt(roads.Nodes.North[a].Raw);
            RelotBlock(world, column, row, visited, plots);
            if (roads.Nodes.North[a] == roads.Nodes.North[b]) { RelotBlock(world, column, row - 1, visited, plots); }
            else { RelotBlock(world, column - 1, row, visited, plots); }
        }
        GatherFreeSides(world, plots);
        created += Settle(world, plots);
        return (created, freed);
    }
    private static void RelotBlock(World world, int column, int row, Span<byte> visited, List<Plot> plots)
    {
        int width = world.Roads.Streets.Blocks;
        if ((uint)column >= (uint)width || (uint)row >= (uint)width) { return; }
        int at = row * width + column;
        if (visited[at] != 0) { return; }
        visited[at] = 1;
        GatherBlock(world, column, row, plots);
    }

    // A Street the lattice carver owns is straight and is a block edge of the lattice.
    private static bool LatticeOwned(World world, int segment)
    {
        RoadGraph roads = world.Roads;
        if (!roads.Segments.Centerline[segment].IsStraight
            || !roads.Nodes.Rows.TryResolve(roads.Segments.NodeA[segment], out int a)) { return false; }
        int column = roads.Lattice.LineAt(roads.Nodes.East[a].Raw), row = roads.Lattice.LineAt(roads.Nodes.North[a].Raw);
        return roads.Streets.Horizontal(column, row) == segment || roads.Streets.Vertical(column, row) == segment;
    }

    private static void GatherFreeSides(World world, List<Plot> plots)
    {
        RoadGraph roads = world.Roads;
        int arcs = roads.Arcs.Count, count = 0;
        var keys = new ulong[arcs];
        var picked = new int[arcs];
        for (int arc = 0; arc < arcs; arc++)
        {
            int segment = roads.Arcs.Segment[arc];
            if (roads.Faces.FaceOf(arc) < 0 || (RoadKind)roads.Segments.Kind[segment] != RoadKind.Street
                || LatticeOwned(world, segment)) { continue; }
            keys[count] = Patch(roads.Segments.Rows.IdAt(segment), roads.Faces.SideOf(arc));
            picked[count++] = arc;
        }
        Array.Sort(keys, picked, 0, count);
        for (int i = 0; i < count; i++)
        {
            if (i == 0 || keys[i] != keys[i - 1]) { GatherSide(world, picked[i], plots); }
        }
    }

    // A side facing a closed face draws its pattern on that face's anchor. An open roadside draws on
    // its own Segment side. Whole-block forms need a lattice square, so here they carve as Perimeter.
    private static void GatherSide(World world, int arc, List<Plot> into)
    {
        RoadGraph roads = world.Roads;
        var rules = world.Rules.Lots;
        int blockTiles = roads.Streets.BlockTiles;
        if (!rules.Runs || blockTiles <= 0) { return; }

        int segment = roads.Arcs.Segment[arc], face = roads.Faces.FaceOf(arc);
        byte sideBit = roads.Faces.SideOf(arc);
        StreetSide side = sideBit == 0 ? StreetSide.Left : StreetSide.Right;
        StreetArc line = roads.Segments.Centerline[segment];
        int width = IntegerMath.FloorDiv(2 * blockTiles, rules.LotsPerSegment);
        int capacity = SegmentSide.Count(line, width);
        if (capacity == 0) { return; }

        Span<SidePlot> plots = capacity <= 64 ? stackalloc SidePlot[64] : new SidePlot[capacity];
        int cut = SegmentSide.Cut(line, side, width, BlockPatterns.StripTiles(blockTiles, rules.LotsPerSegment),
            rules.StreetHalfWidthTiles, plots);
        LandPermissionSummary permission = world.LandPermissions.Summary(Around(plots[..cut]));
        if (permission.AnyUses == 0) { return; }

        ulong patch;
        if (roads.Faces.IsClosed(face))
        {
            var (id, anchorSide) = roads.Faces.Anchor(face);
            patch = Patch(id, anchorSide);
        }
        else
        {
            patch = Patch(roads.Segments.Rows.IdAt(segment), sideBit);
        }
        BlockPattern form = BlockPatterns.ForBand(permission.MixedIntensity ? (byte)0 : permission.Band,
            world.Rules.Bands.Length, blockTiles, rules.LotsPerSegment, world.Key, patch, rules.PatternSpread);
        if (form is not (BlockPattern.Detached or BlockPattern.BackToBack or BlockPattern.Perimeter)) { form = BlockPattern.Perimeter; }
        bool housing = rules.Plots.Applies(form);
        if (housing) { width = rules.Plots.FrontageTiles; }
        int depth = housing ? rules.Plots.DepthTiles
            : BlockPatterns.DepthTiles(form, BlockFace.South, blockTiles, rules.LotsPerSegment);
        if (SegmentSide.Count(line, width) > plots.Length) { plots = new SidePlot[SegmentSide.Count(line, width)]; }
        cut = SegmentSide.Cut(line, side, width, depth, rules.StreetHalfWidthTiles, plots);

        ulong segmentId = roads.Segments.Rows.IdAt(segment);
        for (int i = 0; i < cut; i++)
        {
            var front = line.PointAt(plots[i].Offset.Raw * Fixed.One);
            into.Add(new Plot(segmentId, side, plots[i].Offset.Raw, into.Count, segment, form,
                new Parcel(BlockFace.South, side, plots[i].Offset, plots[i].Geometry), default,
                new Tiles((int)IntegerMath.ShiftRight(front.East, Fixed.FractionalBits)),
                new Tiles((int)IntegerMath.ShiftRight(front.North, Fixed.FractionalBits)), true));
        }
    }

    private static bool ClaimRotated(World world, Tiles east, Tiles north, StreetSide side, ref OrientedRectangle ground)
    {
        int minimum = world.Rules.Lots.MinPlotDepthTiles;
        for (int depth = ground.Deep; depth == ground.Deep || depth >= minimum && minimum > 0; depth--)
        {
            OrientedRectangle candidate = ground with { Deep = depth };
            if (FreeRotated(world, east, north, side, candidate)) { ground = candidate; return true; }
            if (minimum == 0) { return false; }
        }
        return false;
    }

    private static bool FreeRotated(World world, Tiles east, Tiles north, StreetSide side, OrientedRectangle ground)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)) { continue; }
            if (ground.Overlaps(world.Lots.Parcel(slot))
                || (world.Lots.East[slot] == east && world.Lots.North[slot] == north && world.Lots.Side[slot] == (byte)side)) { return false; }
        }
        return true;
    }

    private static LandRectangle Around(ReadOnlySpan<SidePlot> plots)
    {
        LandRectangle around = default;
        foreach (SidePlot plot in plots)
        {
            LandRectangle b = plot.Geometry.Bounds;
            if (!b.IsValid) { continue; }
            if (!around.IsValid) { around = b; continue; }
            int west = b.X < around.X ? b.X : around.X, south = b.Y < around.Y ? b.Y : around.Y;
            int east = b.X + b.Width > around.X + around.Width ? b.X + b.Width : around.X + around.Width;
            int north = b.Y + b.Height > around.Y + around.Height ? b.Y + b.Height : around.Y + around.Height;
            around = new LandRectangle(west, south, east - west, north - south);
        }
        return around;
    }
}
