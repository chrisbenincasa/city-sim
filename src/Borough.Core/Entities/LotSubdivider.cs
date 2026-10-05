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
        if (world.ZoneBlock(column, row, zone) == Rows.NoSlot) { return 0; }
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
        int carved = world.Rules.Lots.Carve(world.Key, Pattern(world, column, row, ground.Patch), ground, proposed);
        for (int i = 0; i < carved; i++)
        {
            if (sides[(int)proposed[i].Face] == Rows.NoSlot || !Free(world, proposed[i], ground)) { continue; }
            if (count == into.Length) { throw new ArgumentException("Preview buffer is smaller than PreviewCapacity.", nameof(into)); }
            into[count++] = proposed[i];
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

    public static int SubdivideBlock(World world, int column, int row, ushort zone)
    {
        if (world.ZoneBlock(column, row, zone) == Rows.NoSlot) { return 0; }
        return CarveBlock(world, column, row, zone == LotTable.Trade && world.Rules.Lots.TradeFormsByBand);
    }

    // Only world creation draws a trade form. The Zone Rule engine cannot build on one, so a block
    // zoned for trade in play keeps the housing ladder.
    private static BlockPattern Pattern(World world, int column, int row, ulong patch, bool tradeForm = false)
    {
        int block = world.BlockIndex.Contains(column, row) ? world.BlockIndex.Slot(column, row) : Rows.NoSlot;
        BlockPattern pattern = world.PatternOf(block, out bool chosen);
        if (chosen) { return pattern; }
        LandPermissionSummary permission = world.LandPermissions.Summary(world.BlockGroundRectangle(column, row));
        byte band = permission.MixedIntensity ? (byte)0 : permission.Band;
        return tradeForm
            ? BlockPatterns.TradeForm(
                band, world.Rules.Bands.Length, world.Rules.Lots.TradeFormWeights, world.Key, patch)
            : BlockPatterns.ForBand(band, world.Rules.Bands.Length, world.Roads.Streets.BlockTiles,
                world.Rules.Lots.LotsPerSegment, world.Key, patch, world.Rules.Lots.PatternSpread);
    }

    private static int CarveBlock(World world, int column, int row, bool tradeForm = false)
    {
        LandRectangle area = world.BlockGroundRectangle(column, row);
        if (!area.IsValid || world.LandPermissions.Summary(area).AnyUses == 0) { return 0; }
        Span<int> sides = stackalloc int[4];
        var streets = world.Roads.Streets;
        BlockGround ground = BlockGround.At(streets.Lattice, column, row) with { Patch = Sides(world, column, row, sides) };
        BlockPattern pattern = Pattern(world, column, row, ground.Patch, tradeForm);
        int ceiling = world.Rules.Lots.ParcelCeiling(ground);
        Span<Parcel> parcels = ceiling <= 128 ? stackalloc Parcel[128] : new Parcel[ceiling];
        int count = world.Rules.Lots.Carve(world.Key, pattern, ground, parcels), created = 0;
        for (int i = 0; i < count; i++)
        {
            Parcel parcel = parcels[i];
            int segment = sides[(int)parcel.Face];
            if (segment == Rows.NoSlot || !Free(world, parcel, ground)) { continue; }
            LandPermissionSummary permission = world.LandPermissions.Summary(Ground(parcel));
            // A parcel can be selected/painted before it is zoned; unzoned free ground stays unplatted.
            if (permission.AnyUses == 0) { continue; }
            var address = parcel.Address(ground);
            Handle<Lot> lot = world.Lots.Create(address.East, address.North, permission.CommonUses, parcel.Side);
            int slot = world.Lots.Rows.Resolve(lot);
            world.Lots.Front(slot, world.Roads.Segments.Rows.At(segment), parcel.Offset);
            world.Lots.ParcelEastQ16[slot] = Fixed.FromInt(parcel.East.Raw); world.Lots.ParcelNorthQ16[slot] = Fixed.FromInt(parcel.North.Raw);
            world.Lots.ParcelWide[slot] = parcel.Wide; world.Lots.ParcelDeep[slot] = parcel.Deep;
            BlockPattern form = BlockPatterns.FormOf(pattern, parcel.Face);
            var foot = world.Rules.Lots.Footprint(world.Key, parcel, ground, form);
            world.Lots.FootprintEastQ16[slot] = Fixed.FromInt(foot.East.Raw); world.Lots.FootprintNorthQ16[slot] = Fixed.FromInt(foot.North.Raw);
            world.Lots.FootprintWide[slot] = foot.Wide; world.Lots.FootprintDeep[slot] = foot.Deep;
            world.Lots.Storeys[slot] = world.Rules.Lots.Height(world.Key, parcel, form, streets.BlockTiles);
            world.Lots.PodiumStoreys[slot] = world.Rules.Lots.PodiumOn(world.Key, parcel.East, parcel.North);
            world.Lots.Pattern[slot] = (byte)((byte)form + 1);
            world.Frontage.Claim(segment, parcel.Side);
            created++;
        }
        if (created > 0)
        {
            world.PatternBlock(column, row, pattern);
            world.RefreshBlockPermissions(world.BlockIndex.Slot(column, row));
            world.LotsAdmitting.Invalidate();
        }
        return created;
    }

    private static LandRectangle Ground(Parcel p) => new(p.East.Raw, p.North.Raw, p.Wide.Raw, p.Deep.Raw);

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
    // that is exactly one closed face takes the face's anchor. Any other block takes the lattice
    // Segment on each side, which carves its open sides as roadside strips, and anchors on the
    // lowest Segment id among them.
    private static ulong Sides(World world, int column, int row, Span<int> sides)
    {
        int face = FaceSides(world, column, row, sides);
        if (face >= 0)
        {
            var (id, side) = world.Roads.Faces.Anchor(face);
            return Patch(id, side);
        }

        var streets = world.Roads.Streets;
        sides[(int)BlockFace.South] = streets.Horizontal(column, row);
        sides[(int)BlockFace.North] = streets.Horizontal(column, row + 1);
        sides[(int)BlockFace.West] = streets.Vertical(column, row);
        sides[(int)BlockFace.East] = streets.Vertical(column + 1, row);
        ulong patch = ulong.MaxValue;
        for (BlockFace each = BlockFace.South; each <= BlockFace.East; each++)
        {
            if (sides[(int)each] == Rows.NoSlot) { continue; }
            byte side = BlockPatterns.SideOf(each) == StreetSide.Left ? (byte)0 : (byte)1;
            ulong candidate = Patch(world.Roads.Segments.Rows.IdAt(sides[(int)each]), side);
            if (candidate < patch) { patch = candidate; }
        }
        return patch;
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

    /// <summary>Explicit whole-vacant-block replat. Street edits do not invoke this operation.</summary>
    public static int RecarveBlock(World world, int column, int row)
    {
        if (!world.BlockIndex.Contains(column, row)) { return 0; }
        int slot = world.BlockIndex.Slot(column, row);
        if (slot == Rows.NoSlot) { return 0; }
        BlockPattern old = world.PatternOf(slot, out bool chosen);
        LandPermissionSummary permission = world.LandPermissions.Summary(world.BlockGroundRectangle(column, row));
        if (!chosen || permission.MixedPermissions || old == BlockPattern.CarParkCentre) { return 0; }
        Span<int> sides = stackalloc int[4];
        BlockPattern wanted = BlockPatterns.ForBand(permission.Band, world.Rules.Bands.Length, world.Roads.Streets.BlockTiles,
            world.Rules.Lots.LotsPerSegment, world.Key, Sides(world, column, row, sides), world.Rules.Lots.PatternSpread);
        if (BlockPatterns.Rung(wanted, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment)
            <= BlockPatterns.Rung(old, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment)) { return 0; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (OnBlock(world, lot, column, row, out _) && !world.Lots.IsVacant(lot)) { return 0; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (OnBlock(world, lot, column, row, out _)) { world.Lots.Rows.Free(world.Lots.Rows.At(lot)); }
        world.PatternBlock(column, row, wanted);
        world.Frontage.Rebuild(world.Lots);
        world.LotsAdmitting.Invalidate();
        return CarveBlock(world, column, row);
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
        // Permission geometry outlives both Lots and Block rows. Visit ground on either side of
        // every live Street; no paint is copied from an old Lot or block summary during restoration.
        var roads = world.Roads;
        var visited = new byte[roads.Streets.Blocks * roads.Streets.Blocks];
        for (int segment = 0; segment < roads.Segments.Rows.SlotCount; segment++)
        {
            if (!roads.Segments.Rows.IsLive(segment) || (RoadKind)roads.Segments.Kind[segment] != RoadKind.Street) { continue; }
            int a = roads.Nodes.Rows.Resolve(roads.Segments.NodeA[segment]), b = roads.Nodes.Rows.Resolve(roads.Segments.NodeB[segment]);
            int column = roads.Lattice.LineAt(roads.Nodes.East[a].Raw), row = roads.Lattice.LineAt(roads.Nodes.North[a].Raw);
            created += RelotBlock(world, column, row, visited);
            created += roads.Nodes.North[a] == roads.Nodes.North[b]
                ? RelotBlock(world, column, row - 1, visited) : RelotBlock(world, column - 1, row, visited);
        }
        return (created, freed);
    }
    private static int RelotBlock(World world, int column, int row, Span<byte> visited)
    {
        int width = world.Roads.Streets.Blocks;
        if ((uint)column >= (uint)width || (uint)row >= (uint)width) { return 0; }
        int at = row * width + column;
        if (visited[at] != 0) { return 0; }
        visited[at] = 1;
        return CarveBlock(world, column, row);
    }
}
