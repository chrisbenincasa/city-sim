using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// Create, free and reuse — and the stale handle being <b>detected</b> rather than silently resolved.
/// </summary>
/// <remarks>
/// adr/0004: in a deterministic simulation a dangling read is a <em>divergence</em>, not a crash. Two
/// runs disagree, the State Hash reports it, and nothing points at the cause. The generation counter
/// is what turns that into an exception at the site of the mistake, so these tests are the ones that
/// prove the counter is wired rather than merely present.
/// </remarks>
public class RowLifecycleTests
{
    private const int Population = 1_000;

    [Fact]
    public void A_freed_slot_is_reused_and_the_old_handle_is_stale()
    {
        var world = new World(Population);

        Handle<Lot> first = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 0);
        int slot = world.Lots.Rows.Resolve(first);

        world.Lots.Rows.Free(first);
        Handle<Lot> second = world.Lots.Create(new Tiles(3), new Tiles(4), zone: 1);

        Assert.Equal(slot, world.Lots.Rows.Resolve(second));

        Assert.False(world.Lots.Rows.IsValid(first));
        Assert.Throws<StaleHandleException>(() => world.Lots.Rows.Resolve(first));
    }

    /// <summary>
    /// The property that makes the id a legal sort key where the index is not.
    /// </summary>
    [Fact]
    public void The_monotonic_id_is_never_reused_even_though_the_slot_is()
    {
        var world = new World(Population);

        Handle<Lot> first = world.Lots.Create(Tiles.Zero, Tiles.Zero, zone: 0);
        int slot = world.Lots.Rows.Resolve(first);
        ulong firstId = world.Lots.Rows.IdAt(slot);

        world.Lots.Rows.Free(first);
        Handle<Lot> second = world.Lots.Create(Tiles.Zero, Tiles.Zero, zone: 0);

        Assert.Equal(slot, world.Lots.Rows.Resolve(second));
        Assert.NotEqual(firstId, world.Lots.Rows.IdAt(slot));
    }

    [Fact]
    public void The_unset_handle_resolves_to_nothing()
    {
        var world = new World(Population);

        Assert.True(default(Handle<Lot>).IsNone);
        Assert.False(world.Lots.Rows.IsValid(default));
        Assert.Throws<StaleHandleException>(() => world.Lots.Rows.Resolve(default));
    }

    /// <summary>
    /// Freeing zeroes the row, which is what lets the hash fold every slot with no liveness branch.
    /// </summary>
    [Fact]
    public void A_freed_row_holds_zeroes_rather_than_its_previous_occupant()
    {
        var world = new World(Population);

        Handle<Lot> lot = world.Lots.Create(new Tiles(7), new Tiles(9), zone: 3);
        int slot = world.Lots.Rows.Resolve(lot);
        world.Lots.Rows.Free(lot);

        Assert.Equal(Tiles.Zero, world.Lots.East[slot]);
        Assert.Equal(Tiles.Zero, world.Lots.North[slot]);
        Assert.Equal(0, world.Lots.Zone[slot]);
        Assert.False(world.Lots.Rows.IsLive(slot));
    }

    [Fact]
    public void Live_and_slot_counts_track_the_free_list()
    {
        var world = new World(Population);

        Handle<Lot> a = world.Lots.Create(Tiles.Zero, Tiles.Zero, zone: 0);
        world.Lots.Create(Tiles.Zero, Tiles.Zero, zone: 0);

        Assert.Equal(2, world.Lots.Rows.LiveCount);
        Assert.Equal(2, world.Lots.Rows.SlotCount);

        world.Lots.Rows.Free(a);

        Assert.Equal(1, world.Lots.Rows.LiveCount);
        Assert.Equal(2, world.Lots.Rows.SlotCount);

        world.Lots.Create(Tiles.Zero, Tiles.Zero, zone: 0);

        Assert.Equal(2, world.Lots.Rows.LiveCount);
        Assert.Equal(2, world.Lots.Rows.SlotCount);
    }

    /// <summary>Growth past the initial capacity keeps every column in step.</summary>
    [Fact]
    public void A_table_grows_without_losing_a_column()
    {
        var world = new World(Population);
        int initial = world.Lots.Rows.Capacity;

        for (int i = 0; i <= initial; i++)
        {
            world.Lots.Create(new Tiles(i), new Tiles(-i), zone: (byte)(i & 0xFF));
        }

        Assert.True(world.Lots.Rows.Capacity > initial);

        for (int slot = 0; slot <= initial; slot++)
        {
            Assert.Equal(new Tiles(slot), world.Lots.East[slot]);
            Assert.Equal(new Tiles(-slot), world.Lots.North[slot]);
        }
    }

    /// <summary>
    /// The schema cannot be edited once rows exist — a column added late would be zeroed for every
    /// existing row while the rest of the table believed them populated.
    /// </summary>
    [Fact]
    public void A_sealed_table_refuses_a_late_column()
    {
        var world = new World(Population);

        Assert.Throws<InvalidOperationException>(() => world.Lots.Rows.Saved<int>("late"));
    }

    [Fact]
    public void Two_columns_cannot_share_a_name()
    {
        var rows = new Rows<Lot>("probe", 8);
        rows.Saved<int>("duplicate");

        Assert.Throws<ArgumentException>(() => rows.Derived<int>("duplicate"));
    }

    [Fact]
    public void An_unsealed_table_refuses_to_allocate()
    {
        var rows = new Rows<Lot>("probe", 8);

        Assert.Throws<InvalidOperationException>(() => rows.Allocate());
    }
}
