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
        east.Raw >= parcel.East.Raw && east.Raw < parcel.East.Raw + parcel.Wide.Raw
        && north.Raw >= parcel.North.Raw && north.Raw < parcel.North.Raw + parcel.Deep.Raw;

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
            into[count++] = new(face, (StreetSide)lots.Side[slot], new Tiles(offset), lots.ParcelEast[slot], lots.ParcelNorth[slot], lots.ParcelWide[slot], lots.ParcelDeep[slot]);
        }
        int ceiling = world.Rules.Lots.ParcelCeiling(ground);
        Span<Parcel> proposed = ceiling <= 128 ? stackalloc Parcel[128] : new Parcel[ceiling];
        int carved = world.Rules.Lots.Carve(world.Key, Pattern(world, column, row), ground, proposed);
        for (int i = 0; i < carved; i++)
        {
            if (SegmentOf(streets, proposed[i].Face, column, row) == Rows.NoSlot || !Free(world, proposed[i], ground)) { continue; }
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
        return CarveBlock(world, column, row);
    }

    private static BlockPattern Pattern(World world, int column, int row)
    {
        int block = world.BlockIndex.Contains(column, row) ? world.BlockIndex.Slot(column, row) : Rows.NoSlot;
        BlockPattern pattern = world.PatternOf(block, out bool chosen);
        LandPermissionSummary permission = world.LandPermissions.Summary(world.BlockGroundRectangle(column, row));
        return chosen ? pattern : BlockPatterns.ForBand(permission.MixedIntensity ? (byte)0 : permission.Band,
            world.Rules.Bands.Length, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment,
            world.Key, column, row, world.Rules.Lots.PatternSpread);
    }

    private static int CarveBlock(World world, int column, int row)
    {
        LandRectangle area = world.BlockGroundRectangle(column, row);
        if (!area.IsValid || world.LandPermissions.Summary(area).AnyUses == 0) { return 0; }
        var streets = world.Roads.Streets;
        BlockGround ground = BlockGround.At(streets.Lattice, column, row);
        BlockPattern pattern = Pattern(world, column, row);
        int ceiling = world.Rules.Lots.ParcelCeiling(ground);
        Span<Parcel> parcels = ceiling <= 128 ? stackalloc Parcel[128] : new Parcel[ceiling];
        int count = world.Rules.Lots.Carve(world.Key, pattern, ground, parcels), created = 0;
        for (int i = 0; i < count; i++)
        {
            Parcel parcel = parcels[i];
            int segment = SegmentOf(streets, parcel.Face, column, row);
            if (segment == Rows.NoSlot || !Free(world, parcel, ground)) { continue; }
            LandPermissionSummary permission = world.LandPermissions.Summary(Ground(parcel));
            // A parcel can be selected/painted before it is zoned; unzoned free ground stays unplatted.
            if (permission.AnyUses == 0) { continue; }
            var address = parcel.Address(ground);
            Handle<Lot> lot = world.Lots.Create(address.East, address.North, permission.CommonUses, parcel.Side);
            int slot = world.Lots.Rows.Resolve(lot);
            world.Lots.FrontageSlot[slot] = segment + 1;
            world.Lots.FrontageOffset[slot] = parcel.Offset;
            world.Lots.ParcelEast[slot] = parcel.East; world.Lots.ParcelNorth[slot] = parcel.North;
            world.Lots.ParcelWide[slot] = parcel.Wide; world.Lots.ParcelDeep[slot] = parcel.Deep;
            var foot = world.Rules.Lots.Footprint(world.Key, parcel, ground, pattern);
            world.Lots.FootprintEast[slot] = foot.East; world.Lots.FootprintNorth[slot] = foot.North;
            world.Lots.FootprintWide[slot] = foot.Wide; world.Lots.FootprintDeep[slot] = foot.Deep;
            world.Lots.Storeys[slot] = world.Rules.Lots.Height(world.Key, parcel, pattern, streets.BlockTiles);
            world.Lots.Pattern[slot] = (byte)((byte)pattern + 1);
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
            if (World.Overlaps(candidate, world.LotGround(slot))
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

    private static int SegmentOf(StreetGrid streets, BlockFace face, int column, int row) => face switch
    {
        BlockFace.South => streets.Horizontal(column, row),
        BlockFace.North => streets.Horizontal(column, row + 1),
        BlockFace.West => streets.Vertical(column, row),
        _ => streets.Vertical(column + 1, row),
    };

    /// <summary>Explicit whole-vacant-block replat. Street edits do not invoke this operation.</summary>
    public static int RecarveBlock(World world, int column, int row)
    {
        if (!world.BlockIndex.Contains(column, row)) { return 0; }
        int slot = world.BlockIndex.Slot(column, row);
        if (slot == Rows.NoSlot) { return 0; }
        BlockPattern old = world.PatternOf(slot, out bool chosen);
        LandPermissionSummary permission = world.LandPermissions.Summary(world.BlockGroundRectangle(column, row));
        if (!chosen || permission.MixedPermissions) { return 0; }
        BlockPattern wanted = BlockPatterns.ForBand(permission.Band, world.Rules.Bands.Length, world.Roads.Streets.BlockTiles,
            world.Rules.Lots.LotsPerSegment, world.Key, column, row, world.Rules.Lots.PatternSpread);
        if (BlockPatterns.Rung(wanted, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment)
            <= BlockPatterns.Rung(old, world.Roads.Streets.BlockTiles, world.Rules.Lots.LotsPerSegment)) { return 0; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (OnBlock(world, lot, column, row, out _) && !world.Lots.IsVacant(lot)) { return 0; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (OnBlock(world, lot, column, row, out _)) { world.Lots.Rows.Free(world.Lots.Rows.At(lot)); }
        world.PatternBlock(column, row, wanted);
        world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
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
            world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
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
