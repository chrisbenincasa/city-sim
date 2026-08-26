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
/// ⚠⚠ <b>AMENDED 2026-08-20 by milestone 10's gate, and the amendment is that IT IS INTERMITTENT.</b>
/// The same tree ran the whole unfiltered suite three times and came back <b>red, green, green</b> —
/// <c>ZoneRuleTriggerTests.Sweeping_allocates_nothing_after_the_first_trigger</c>, <b>7,896 bytes</b>
/// over 500 Steps that allocate nothing, then twice clean. The paragraph above is about a case where
/// causation was <em>measured</em>, and nothing here withdraws it; what it does not say, and a reader
/// would take from it, is that a failure of this shape has a cause you can go and remove.
/// </para>
/// <para>
/// ⚠ <b>The methodological half is the part to keep, because it nearly went wrong.</b> Milestone 10
/// suspected its own new 100,000-Tick test, moved it aside, ran the suite, and got a green — which
/// looks exactly like a fix and is worth <b>nothing</b>. ***One green run cannot tell "the cause was
/// removed" from "the intermittent did not fire",*** and restoring the test produced a second green,
/// which is what actually settled it. Milestone 8 ran <b>four</b> full suites for precisely this
/// reason and said so; a reader who takes the conclusion without the protocol repeats the near-miss.
/// ⚠ <b>The suspicion was quantitative and still wrong</b>: the new test allocates <b>52,360,328</b>
/// bytes and the <c>RuleLongRunTests</c> already beside it allocates <b>51,769,064</b>, so it doubled
/// nothing and added one more of a thing the suite already had several of.
/// </para>
/// <para>
/// <b>And a bound is now visible that milestone 8 could not see with two sightings.</b> Every
/// discrepancy ever recorded is under <b>8,192</b> bytes — 5,672, 5,696, 6,768, 7,896 — which is the
/// gen0 <em>allocation context</em>, the per-thread buffer whose used portion this counter adds in.
/// So the hypothesis sharpens to <em>a collection forced by another thread retires this thread's
/// context, and the counter jumps by at most one context</em>. ⚠ <b>Four samples is a pattern and not
/// a bound</b>, and <c>adr/0043</c> reaches this directly: the measurement has never been run, so no
/// document may state it. Filed to <see href="../../plans/0002-open-questions.md">the ledger</see>
/// §B with the machine that would settle it — <c>GC.CollectionCount</c> recorded either side of the
/// measured window — because ***what rests on it is a choice and not a fix***: bounded, and these
/// assertions can stay exact when no collection occurred; unbounded, and they do not belong in a
/// parallel suite at all.
/// </para>
/// <para>
/// ⚠⚠ <b>AMENDED AGAIN 2026-08-20 by the machine above actually being run, and the amendment is that
/// A COLLECTION IN THE WINDOW IS NOT NEARLY SUFFICIENT — while the question itself is STILL OPEN.</b>
/// Six assertion-tier runs and then <b>six full unfiltered runs on a second machine</b> (Apple M4 Pro,
/// 10P+4E, Release, workstation GC) have now produced <b>96 readings and not one firing</b>. Five of
/// those windows <em>contained</em> collections and every one read <b>exactly zero bytes</b> — the
/// worst of them <b>thirteen gen0 collections</b>, another carrying a <b>gen2</b>. The hypothesis this
/// remark carried was <em>a collection forced by another thread retires this thread's context and the
/// counter jumps</em>; ***the sufficiency half is refuted, and refuted with room to spare.***
/// </para>
/// <para>
/// ⚠ <b>What survives is the weaker <em>a jump requires a collection</em>, and twelve runs have not
/// tested it.</b> That claim is about jumps, and <b>nothing jumped</b>, so ninety-six zeroes are
/// silent on it. ⚠ <b>Nor are twelve green runs evidence the fault is gone.</b> This is this remark's
/// own protocol arriving at n=12 and still biting: at a per-run rate <c>p</c> the chance of six clean
/// runs is <c>(1−p)⁶</c>, which excludes only <c>p</c> above <b>~39%</b> at 95% — and the rate
/// actually observed on the reference machine is about <b>one run in three</b>, which this sample does
/// not exclude. ***A rare event's absence is a statement about the sample size before it is a statement
/// about the machine.*** <b>So neither branch may be taken</b>: the assertions are not yet known to be
/// safe, and they are not yet known to be unsafe. <c>adr/0043</c> holds rather than being discharged.
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

        long jitMethods = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
        long jitIl = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();
        Churn(world, handles);
        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Check(
            "TableAllocationTests.Allocate_free_and_reuse_allocate_no_managed_memory", before, after, gen0, gen1, gen2, jitMethods, jitIl);
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

        long jitMethods = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
        long jitIl = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();
        world.HashState();
        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Check(
            "TableAllocationTests.Hashing_the_world_allocates_no_managed_memory", before, after, gen0, gen1, gen2, jitMethods, jitIl);
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

        long jitMethods = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true);
        long jitIl = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true);
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();

        int seen = 0;
        foreach (int slot in world.Occupants.Walk(buildingSlot))
        {
            seen += slot;
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Check(
            "TableAllocationTests.Walking_an_intrusive_list_allocates_no_managed_memory", before, after, gen0, gen1, gen2, jitMethods, jitIl);
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
