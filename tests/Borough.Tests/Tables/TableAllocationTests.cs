using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// Table operations allocate nothing once the arrays exist.
/// </summary>
/// <remarks>
/// <para>
/// <b>A test rather than a BenchmarkDotNet memory diagnoser, which plans/0007 named.</b> The
/// diagnoser <em>reports</em> allocation; this <em>fails</em> on it, and the difference matters for a
/// property that will be regressed by a stray closure in a year's time rather than today.
/// <c>GC.GetAllocatedBytesForCurrentThread</c> is exact and needs no dependency, which also keeps
/// adr/0003's rule about arguing for anything entering the core's orbit unspent on a benchmark
/// harness. A benchmark project is still the right home for <em>timings</em>, and this is not one.
/// </para>
/// <para>
/// The claim being pinned is narrow and is the one adr/0006 cares about: <b>steady state allocates
/// nothing</b>. Growth allocates, deliberately and once per doubling; it is excluded by pre-growing
/// the table before the measured loop.
/// </para>
/// </remarks>
public class TableAllocationTests
{
    private const int Rows = 512;

    [Fact]
    public void Allocate_free_and_reuse_allocate_no_managed_memory()
    {
        var world = new World(10_000);
        Span<Handle<Lot>> handles = new Handle<Lot>[Rows];

        // Reach steady state first: every array is at its final size and the free list is warm.
        Churn(world, handles);

        long before = GC.GetAllocatedBytesForCurrentThread();
        Churn(world, handles);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
    }

    [Fact]
    public void Hashing_the_world_allocates_no_managed_memory()
    {
        var world = new World(10_000);

        for (int i = 0; i < Rows; i++)
        {
            world.Lots.Create(new Tiles(i), new Tiles(i), zone: 0);
        }

        world.HashState();

        long before = GC.GetAllocatedBytesForCurrentThread();
        world.HashState();
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
    }

    [Fact]
    public void Walking_an_intrusive_list_allocates_no_managed_memory()
    {
        var world = new World(10_000);
        Handle<Building> building = world.Buildings.Create(default, kind: 0);

        for (int i = 0; i < 64; i++)
        {
            world.CreateHousehold(building, lifeStage: 0);
        }

        int buildingSlot = world.Buildings.Rows.Resolve(building);

        long before = GC.GetAllocatedBytesForCurrentThread();

        int seen = 0;
        foreach (int slot in world.Occupants.Walk(buildingSlot))
        {
            seen += slot;
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
        Assert.True(seen > 0);
    }

    private static void Churn(World world, Span<Handle<Lot>> handles)
    {
        for (int i = 0; i < handles.Length; i++)
        {
            handles[i] = world.Lots.Create(new Tiles(i), new Tiles(i), zone: (byte)(i & 0xFF));
        }

        for (int i = 0; i < handles.Length; i++)
        {
            world.Lots.Rows.Free(handles[i]);
        }
    }
}
