using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

public sealed class LocalLayoutReaderTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(62002);

    [Fact]
    public void Preview_and_parcel_paint_use_the_merged_saved_site_without_moving_buildings()
    {
        var (world, lots) = LocalLayoutTests.Fixture();
        var plan = LocalLayoutTests.Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        Assert.True(new Simulation(world, Key).CommitLocalLayout(plan, out var building).Accepted);
        var lot = world.Buildings.Lot[world.Buildings.Rows.Resolve(building)];
        LocalLot saved = LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(lot));
        var preview = new Parcel[LotSubdivider.PreviewCapacity(world, 0, 0)];
        ulong before = world.HashState();
        int count = LotSubdivider.Preview(world, 0, 0, preview);
        Assert.Single(preview.Take(count), p => p.East.Raw == plan.Site.X && p.North.Raw == plan.Site.Y
            && p.Wide.Raw == plan.Site.Width && p.Deep.Raw == plan.Site.Height);
        Assert.Equal(before, world.HashState());
        var a = world.LandPermissions.At(plan.Site.X, plan.Site.Y);
        var b = world.LandPermissions.At(plan.Site.X + 12, plan.Site.Y);
        Assert.Equal(1, LotSubdivider.PaintParcelAt(world, new Tiles(plan.Site.X + 18), new Tiles(plan.Site.Y + 8), LotTable.Trade));
        Assert.Equal(a with { Uses = LotTable.Trade }, world.LandPermissions.At(plan.Site.X, plan.Site.Y));
        Assert.Equal(b with { Uses = LotTable.Trade }, world.LandPermissions.At(plan.Site.X + 12, plan.Site.Y));
        Assert.Equal(saved, LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(lot)));
        Assert.Equal(3, world.HousingBuildings.Count(world));
        Assert.Equal(PermissionRefusal.Use, world.ConstructionPermission(world.Lots.Rows.Resolve(lot), LotTable.Housing));
        Assert.Equal(LotTable.Housing, world.LandPermissions.At(world.LotGround(world.Lots.Rows.Resolve(lots[3])).X, plan.Site.Y).Uses);
    }

    [Fact]
    public void Street_removal_restoration_and_new_cross_street_preserve_saved_occupied_ground()
    {
        var (world, lots) = LocalLayoutTests.Fixture();
        var plan = LocalLayoutTests.Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        Assert.True(new Simulation(world, Key).CommitLocalLayout(plan, out var building).Accepted);
        var merged = world.Buildings.Lot[world.Buildings.Rows.Resolve(building)];
        var saved = new[] { lots[0], merged, lots[4] }.Select(l => LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(l))).ToArray();
        ulong permissions = 0;
        world.PermissionRectangles.Rows.Fold(ref permissions);
        var file = new MemorySave();
        SaveFile.Write(world, 62002, file);
        var restored = SaveFile.Read(file, world.Rules, out _);
        Assert.Equal(world.HousingBuildings.Count(world), restored.HousingBuildings.Count(restored));
        foreach (var city in new[] { world, restored })
        {
            int peak = 0;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                Assert.True(city.Roads.BulldozeStreet(0, 0, StreetAxis.East));
                city.RebuildDerived();
                LotSubdivider.Resubdivide(city);
                Assert.False(city.Lots.HasFrontage(city.Lots.Rows.Resolve(merged)));
                city.Roads.LayStreet(0, 0, StreetAxis.North);
                city.RebuildDerived();
                LotSubdivider.Resubdivide(city);
                Assert.True(city.Roads.LayStreet(0, 0, StreetAxis.East));
                city.RebuildDerived();
                LotSubdivider.Resubdivide(city);
                city.RebuildParcels();
                Assert.True(city.Lots.HasFrontage(city.Lots.Rows.Resolve(merged)));
                foreach (var neighbour in saved) Assert.Equal(neighbour, LocalLot.Read(city.Lots, city.Lots.Rows.Resolve(neighbour.Handle)));
                AssertDisjoint(city);
                ulong current = 0;
                city.PermissionRectangles.Rows.Fold(ref current);
                Assert.Equal(permissions, current);
                if (cycle > 0) Assert.Equal(peak, city.Lots.Rows.SlotCount);
                peak = city.Lots.Rows.SlotCount;
            }
            new Simulation(city, Key).CheckEndOfRun();
        }
        Assert.Equal(world.HashState(), restored.HashState());
    }

    [Fact]
    public void Vacant_trade_ground_counts_lots_not_painted_tiles_and_rebuilds_from_geography()
    {
        var (world, lots) = LocalLayoutTests.Fixture();
        int lot = world.Lots.Rows.Resolve(lots[1]);
        int before = DistrictWatershed.Field(world.Lots, world.BuildingsInCells).Sum();
        Assert.Equal(PermissionRefusal.None, world.PaintUsePermissions(world.LotGround(lot), LotTable.Trade));
        Assert.Equal(before + 1, DistrictWatershed.Field(world.Lots, world.BuildingsInCells).Sum());
        world.RebuildDerived();
        Assert.Equal(before + 1, DistrictWatershed.Field(world.Lots, world.BuildingsInCells).Sum());
        world.PaintUsePermissions(world.LotGround(lot), 0);
        Assert.Equal(before, DistrictWatershed.Field(world.Lots, world.BuildingsInCells).Sum());
    }

    [Fact]
    public void Refused_block_paint_allocates_no_block_and_changes_no_saved_state()
    {
        var (world, _) = LocalLayoutTests.Fixture();
        int blocks = world.Blocks.Rows.SlotCount;
        int capacity = world.Blocks.Rows.Capacity;
        ulong before = world.HashState();
        Assert.Equal(Rows.NoSlot, world.ZoneBlock(-1, 0, LotTable.Housing));
        Assert.Equal(blocks, world.Blocks.Rows.SlotCount);
        Assert.Equal(capacity, world.Blocks.Rows.Capacity);
        Assert.Equal(before, world.HashState());
    }

    private static void AssertDisjoint(World world)
    {
        for (int a = 0; a < world.Lots.Rows.SlotCount; a++)
            for (int b = a + 1; b < world.Lots.Rows.SlotCount; b++)
                if (world.Lots.Rows.IsLive(a) && world.Lots.Rows.IsLive(b)) Assert.False(World.Overlaps(world.LotGround(a), world.LotGround(b)));
    }
}
