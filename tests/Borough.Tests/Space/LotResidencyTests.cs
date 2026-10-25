using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// The Lot index behind <see cref="LotSubdivider"/>'s free-ground test. A Cell is 32 Tiles wide.
/// </summary>
public sealed class LotResidencyTests
{
    [Fact]
    public void A_lot_crossing_a_cell_edge_is_found_from_the_next_cell()
    {
        var world = new World(1000);
        int lot = Lot(world, east: 28, north: 4, wide: 8);
        Lot(world, east: 200, north: 200, wide: 4);

        Assert.Equal([lot], Near(world, 34, 4, 35, 5));
    }

    [Fact]
    public void A_lot_created_without_registering_is_found()
    {
        var world = new World(1000);
        int lot = world.Lots.Rows.Resolve(world.Lots.Create(new Tiles(10), new Tiles(10), LotTable.Housing,
            wide: new Tiles(4), deep: new Tiles(4)));

        Assert.Equal([lot], Near(world, 12, 12, 13, 13));
    }

    [Fact]
    public void A_freed_lot_is_gone_and_its_recycled_slot_answers_once()
    {
        var world = new World(1000);
        int first = Lot(world, east: 10, north: 10, wide: 4);
        world.Lots.Rows.Free(world.Lots.Rows.At(first));
        Assert.Empty(Near(world, 10, 10, 14, 14));

        int second = Lot(world, east: 12, north: 12, wide: 4);
        Assert.Equal(first, second);
        Assert.Equal([second], Near(world, 10, 10, 16, 16));
    }

    [Fact]
    public void Stale_entries_stay_bounded_by_the_live_lots()
    {
        var world = new World(1000);
        Lot(world, east: 0, north: 0, wide: 4);
        for (int i = 0; i < 1000; i++)
        {
            int lot = Lot(world, east: 40, north: 40, wide: 4);
            world.Lots.Rows.Free(world.Lots.Rows.At(lot));
        }

        Assert.True(world.LotsInCells.EntryCount <= 2 * (world.Lots.Rows.LiveCount + 1) + 64);
        Assert.Empty(Near(world, 40, 40, 44, 44));
    }

    [Fact]
    public void A_rebuild_answers_as_the_maintained_index_did()
    {
        var world = new World(1000);
        Lot(world, east: 5, north: 5, wide: 30);
        Lot(world, east: 70, north: 5, wide: 6);
        int freed = Lot(world, east: 90, north: 90, wide: 6);
        world.Lots.Rows.Free(world.Lots.Rows.At(freed));
        int[] maintained = Near(world, 0, 0, 120, 120);

        world.LotsInCells.Rebuild(world.Lots);

        Assert.Equal(maintained, Near(world, 0, 0, 120, 120));
    }

    private static int Lot(World world, int east, int north, int wide)
    {
        int slot = world.Lots.Rows.Resolve(world.Lots.Create(new Tiles(east), new Tiles(north), LotTable.Housing,
            wide: new Tiles(wide), deep: new Tiles(4)));
        world.LotsInCells.Add(world.Lots, slot);
        return slot;
    }

    private static int[] Near(World world, long west, long south, long east, long north)
    {
        var found = new List<int>();
        foreach (int slot in world.LotsInCells.Near(world.Lots, west, south, east, north)) { found.Add(slot); }
        found.Sort();
        return [.. found];
    }
}
