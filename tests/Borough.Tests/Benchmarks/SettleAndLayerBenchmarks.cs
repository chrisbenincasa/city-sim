using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// What Phase 3 costs, which is the sharper of [`0013`]'s two blank rows.
/// </summary>
/// <remarks>
/// <para>
/// <b>Phase 3 is not Phase 2 again.</b> It re-checks each intent — one more atomicity walk, already
/// priced at 82.84 ns by <see cref="RuleEngineBenchmarks"/> — and then does two things that phase does
/// not: it **sorts** the intents into the settle order, which is `O(n log n)` over the due set, and it
/// writes. The sort is why this row was flagged rather than assumed: every other cost in the engine is
/// linear in the due count, and one that is not changes the shape of the whole ledger at scale.
/// </para>
/// <para>
/// <b>This one has to rebuild per iteration, and there was no way around it.</b> Phase 2 could be
/// timed a million times over one arrangement because <c>adr/0037</c> makes it read-only; Phase 3 is
/// the phase that writes, so the second invocation would settle an already-settled Tick — and in the
/// failing case would double-subscribe, which the write-site invariant catches by throwing.
/// <c>IterationSetup</c> with one invocation per iteration is the honest shape, and BenchmarkDotNet
/// excludes the setup from the measurement.
/// </para>
/// <para>
/// <b>Two loads, because they exercise different halves.</b> A firing Tick pays the re-check, the Bin
/// writes and the re-arm; a starving Tick pays the re-check and a subscription. Both pay the sort, and
/// the sort is what the three population rungs are there to expose.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 5, iterationCount: 40, invocationCount: 1)]
public class RuleSettleBenchmarks
{
    private Simulation _simulation = null!;
    private Ticks _due;

    [Params(1_000, 10_000, 100_000)]
    public int Buildings { get; set; }

    [Params(RuleEngineBenchmarks.Load.Fires, RuleEngineBenchmarks.Load.Starves)]
    public RuleEngineBenchmarks.Load Shape { get; set; }

    /// <summary>Rebuilds the world and runs Phases 1 and 2, none of which is measured.</summary>
    /// <remarks>
    /// <b>The collection at the end is load-bearing, and the first capture was unpublishable without
    /// it.</b> Building a 100,000-Building world allocates heavily, and the collection that debt
    /// triggers was landing inside the measured <see cref="Apply"/> — error bars wider than the means
    /// at the small rungs, and a 1,000-Building row reading 2.223 ms mean against a 1.517 ms median.
    /// Collecting here moves that cost into the setup, where BenchmarkDotNet excludes it. It is the
    /// standard hazard of <c>IterationSetup</c> over an allocating arrangement and it is worth the
    /// three lines.
    /// </remarks>
    [IterationSetup]
    public void Arrange()
    {
        (_simulation, _due) = RuleEngineFixture.Arrange(Shape, Buildings);

        _simulation.Rules.CollectDue(_due);
        _simulation.Rules.Evaluate(_due);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>One Tick's Phase 3: sort, re-check, and apply or subscribe.</summary>
    [Benchmark(Description = "settle")]
    public void Apply() => _simulation.Rules.Apply(_due);
}

/// <summary>
/// A whole Tick of the engine at steady state, over a Rule with no terms — the fixed per-due-Rule
/// cost, and the only trustworthy reading of <b>the sort</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because <see cref="RuleSettleBenchmarks"/> could not be made to hold still.</b>
/// Rebuilding a 100,000-Building world per iteration means the measured <c>Apply</c> is the first
/// code to write to freshly allocated arrays, so it pays that world's page faults; moving the
/// collection into the setup narrowed the error bars and did not fix the shape, and the medians
/// scaled 1.5× then 15.5× across rungs a decade apart. <b>A cost that tracks world size rather than
/// work is not a measurement of the work.</b> Those figures are recorded as an upper bound and
/// nothing is derived from them.
/// </para>
/// <para>
/// <b>A no-term Rule at rate 1 removes the rebuild entirely.</b> It touches no Bin, so a Tick leaves
/// the world bit-identical and <c>Step</c> can run thousands of times over one arrangement, warm and
/// faulted in. What it measures is every cost a due Rule pays regardless of its terms — and that
/// includes the intent sort, which is the one part of the engine that is not linear in the due count
/// and the reason this row was flagged in the first place.
/// </para>
/// <para>
/// <b>The Decide guard is off, and it must be said out loud.</b> It is <c>O(world)</c> and costs 490%
/// of a Tick at 1M on its own; leaving it on would have measured it instead of the engine. Every
/// other benchmark here calls the engine directly and never meets it.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class RuleTickBenchmarks
{
    private Simulation _simulation = null!;

    [Params(1_000, 10_000, 100_000)]
    public int Buildings { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_simulation, _) = RuleEngineFixture.Arrange(RuleEngineBenchmarks.Load.Idle, Buildings);

        _simulation.VerifyDecideWritesNothing = false;

        // Warm the arrangement: the first Tick faults in the tables and arms every Rule at rate 1,
        // after which every Tick is the same Tick.
        for (int i = 0; i < 4; i++)
        {
            _simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>All eight phases, with every Rule due, firing and re-arming.</summary>
    [Benchmark(Description = "tick, every Rule due")]
    public void Step() => _simulation.Step(TickInput.Empty);
}

/// <summary>
/// What a Map Layer's diffusion costs — [`0013`]'s other blank row, and the one with no excuse at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>Built in slice 6 and never priced.</b> S0a's 0.112 ms empty Tick contains *"the Layer
/// schedule"* — the check for whether a Layer is due — and not a single convolution, because the world
/// it stepped had no sources in it. So the one phase of the eight that is finished has been sitting in
/// the ledger as an unknown.
/// </para>
/// <para>
/// <b>Both operations are idempotent, so neither needs a rebuild.</b> Diffusion is a pure recompute of
/// the Pollution field from the Source field, so running it a thousand times over one arrangement does
/// the same work every time and leaves the same answer — the property slice 6 established when it
/// proved incremental re-diffusion bit-identical to a full recompute.
/// </para>
/// <para>
/// <b>The sweep is over the dirty rectangle, and the first draft swept the wrong thing.</b> Sweeping
/// the emitter count produced a column that fell as sources rose — S2's <em>an artefact that varies
/// with the swept axis is not distinguishable from a result</em> — because the kernel radius is 8
/// Cells, one emitter makes 289 resident, and a 128×128 map saturates after a few dozen scattered
/// sources. <c>MapLayerFixtureTests</c> holds that claim. **So residency is not a lever the city has**,
/// and what is left driving the cost is how much of the map changed since the last diffusion, which is
/// what the cadence trades against.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class MapLayerBenchmarks
{
    private World _world = null!;

    /// <summary>
    /// The side, in Cells, of the region dirtied between diffusions. 128 is the whole map.
    /// </summary>
    /// <remarks>
    /// The real quantity is the dirty rect <em>dilated by the kernel radius</em>, so even a side of 1
    /// recomputes 17×17. That is the floor of the incremental path rather than a defect: a source's
    /// influence extends to the halo, so a halo is the smallest honest unit of recomputation.
    /// </remarks>
    [Params(1, 8, 32, 128)]
    public int DirtySide { get; set; }

    /// <summary>
    /// Scatters emitters evenly, by a stride coprime with the Cell count so no rung clusters.
    /// </summary>
    /// <remarks>
    /// <b>The first fixture used <c>WorldCellCount / emitters</c> and degenerated to a stride of one</b>
    /// at the top rung, laying every source in a contiguous run — a different experiment wearing the
    /// same parameter. 6,151 is prime, and the Cell count is a power of two, so every odd stride walks
    /// the whole grid before repeating.
    /// </remarks>
    internal static void Scatter(World world, int emitters)
    {
        for (int i = 0; i < emitters; i++)
        {
            int cell = (i * 6_151) % CellGrid.WorldCellCount;

            world.Layers.EmitPollution(
                new Cells(cell % CellGrid.WorldCells), new Cells(cell / CellGrid.WorldCells), 64);
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World(1_000, LayerRuleset.Default);

        // Past the saturation knee by a wide margin, so the timings describe a full Layer and no rung
        // is measuring residency growth instead of the axis it names.
        Scatter(_world, 1_024);

        _world.Layers.RediffusePollution();
    }

    /// <summary>The whole map, which is what a Ruleset reload or a load from a save pays.</summary>
    [Benchmark(Description = "diffuse, whole map")]
    public void Full() => _world.Layers.RediffusePollution();

    /// <summary>
    /// What a Tick on the diffusion cadence pays, given a region of that size changed under it.
    /// </summary>
    /// <remarks>
    /// The marks are rectangle unions and are nothing; what is timed is the dilated recompute, which
    /// is slice 6's incremental path and the one the schedule runs every 64 Ticks.
    /// </remarks>
    [Benchmark(Description = "diffuse, dirty region")]
    public void Incremental()
    {
        _world.Layers.MarkPollutionDirty(Cells.Zero, Cells.Zero);
        _world.Layers.MarkPollutionDirty(new Cells(DirtySide - 1), new Cells(DirtySide - 1));
        _world.Layers.DiffusePollution();
    }
}
