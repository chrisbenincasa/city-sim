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
/// ⚠ <b>"Exact" is a claim about what it counts and not about what it reads, and the difference was
/// measured on 2026-08-18 (milestone 8 task 7).</b> A new test elsewhere in this suite that allocated
/// ~300 MB made <b>two unrelated allocation assertions fail</b> — including
/// <c>QuantityTests.Arithmetic_on_quantities_allocates_nothing</c>, over arithmetic that cannot
/// allocate at all — and reducing that test's allocation made them green again, over four full runs.
/// The counter is served out of a per-thread allocation context, and the working hypothesis is that a
/// collection forced by <em>another</em> thread flushes it; ***the mechanism is not verified and the
/// causation is***. ***An allocation assertion is exact in isolation and perturbable in a suite that
/// runs in parallel***, so the obligation lands on every <em>other</em> test: a test that allocates
/// heavily is not a local decision here.
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
        Handle<Building> building = world.Buildings.Create(world.Lots, default, kind: 0);

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
