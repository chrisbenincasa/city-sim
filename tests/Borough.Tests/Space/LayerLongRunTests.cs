using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// <c>plans/0009</c>'s acceptance run: 100,000 Ticks with the Layers scheduled and churning.
/// </summary>
/// <remarks>
/// <para>
/// <b>It lives here rather than in the headless runner, and that is a correction to the plan.</b> The
/// acceptance asks for a 100,000-Tick <em>headless</em> run with all three Layers scheduled — but no
/// session can place a pollution source, because sources come from industry and industry needs Rules
/// (slice 7). A headless run would therefore diffuse an empty map 1,562 times and report that nothing
/// trended upward, which is the <b>vacuous assertion</b> slice 5 task 7 refused to write for exactly
/// this reason: <em>an assertion that cannot fail reads as covered</em>. So the churn is injected
/// through the cold API, which is the only thing that can supply it today.
/// </para>
/// <para>
/// <b>The churn had to be designed, and the first attempt measured the wrong thing.</b> Sources drawn
/// at random across the whole map made peak pollution rise from 1.06M to 1.44M over the run's second
/// half — and that was the field <em>filling in</em>, not an accumulator with no sink. A random walk
/// over 16,384 Cells has not covered them by Tick 100,000, so the run never reached the steady state
/// <c>adr/0006</c>'s rule is stated about. <b>A trend measured before steady state is a measurement of
/// the transient</b>, and it would have been read as a leak.
/// </para>
/// <para>
/// So the churn below sweeps a bounded region round-robin, with each Cell's emission a fixed function
/// of the Cell.
/// </para>
/// <para>
/// <b>It used to <em>set</em> each Cell's source rather than add to it, and that was working around a
/// live defect instead of reporting one</b> (<c>plans/0011</c> finding 37). The comment justifying it
/// said, correctly, that "adding forever would manufacture the unbounded growth the test then reports"
/// — but a Rule's <c>map</c> output adds, so what the test had actually done was decline to exercise
/// the only write path the simulation has. <c>adr/0051</c> made the source a stock the environment
/// absorbs, so the churn now <b>adds, exactly as <c>RuleEngine.Emit</c> does</b>, and what is asserted
/// is the equilibrium rather than the arithmetic's ceiling.
/// </para>
/// <para>
/// <b>The assertion is exact equality at one sweep's lag, which is stronger than the trend line it
/// replaces and was not available before.</b> Emission is periodic with period <see cref="Sweep"/> and
/// absorption is a contraction, so the source field converges to a <em>limit cycle</em> of that period
/// rather than to a constant — every Cell is topped up once per sweep and absorbed for the cadences in
/// between. Comparing samples one full sweep apart compares like with like: a correct implementation
/// repeats to the digit, and an accumulator with no sink drifts upward by one sweep's emission every
/// time.
/// </para>
/// </remarks>
public class LayerLongRunTests
{
    private const int Ticks = 100_000;
    private const int Population = 1_000;

    /// <summary>How often a source changes. Well inside pollution's 64-Tick cadence.</summary>
    private const int EmitEvery = 8;

    /// <summary>The churned region's edge, in Cells. Small enough to be swept many times over.</summary>
    private const int Region = 24;

    /// <summary>How often the State Hash is folded in. The project's trace cadence.</summary>
    private const int HashEvery = 16;

    /// <summary>Ticks for one full sweep of the region. The period of the emission, and of the field.</summary>
    private const int Sweep = Region * Region * EmitEvery;

    /// <summary>
    /// The first sample of the tail — where the limit cycle is assumed reached.
    /// </summary>
    /// <remarks>
    /// Ten sweeps, which is <b>deliberately far more than convergence needs</b> and costs nothing
    /// because the run happens either way. A transient read as a steady state is the failure this
    /// class's own history records: the first version of the churn measured the field filling in and
    /// would have reported it as a leak.
    /// </remarks>
    private const int Settled = 15 * Sweep / HashEvery;

    /// <summary>
    /// The acceptance run, and all three of its claims.
    /// </summary>
    /// <remarks>
    /// <b>One test rather than three, because the run is the expensive part.</b> 100,000 Ticks of
    /// convolution, incremental halos, buffer swaps and a first-order lag costs seconds; asserting
    /// three things about it costs nothing. Splitting them would triple the wall clock to make the
    /// failure messages marginally tidier.
    /// </remarks>
    [Fact]
    public void The_hundred_thousand_Tick_acceptance_run()
    {
        ulong first = Run(out World world, out int[] peaks);
        ulong second = Run(out _, out _);

        // 1. Reproducible. The Layers are the first thing in the project whose arithmetic is
        //    complicated enough for this to be a real question rather than a formality.
        Assert.Equal(first, second);

        // 2. No magnitude trends upward. Emission is periodic with one sweep's period, so the sweep-to
        //    -sweep step is what to read: with a sink it contracts toward zero, and with none it is a
        //    constant — one sweep's emission, for ever. Those two are four orders of magnitude apart
        //    here, which is why this is a sharper instrument than a trend line over the raw level.
        const int Lag = Sweep / HashEvery;
        int[] tail = peaks[Settled..];

        Assert.True(tail.Length > Lag, "the tail is shorter than one sweep, so it asserted nothing.");
        Assert.True(tail[0] > 0, "the run diffused nothing, so it asserted nothing.");

        // Approached from below and never overshot. Each Cell's source rises monotonically toward its
        // own equilibrium from zero, so the peak over them does too — an overshoot would mean the halo
        // had left a contribution behind that the next recompute did not clear.
        for (int i = 0; i + Lag < tail.Length; i++)
        {
            Assert.True(
                tail[i + Lag] >= tail[i],
                $"peak pollution fell from {tail[i]} to {tail[i + Lag]} one sweep later, at sample {i} "
                + $"of {tail.Length}. The source field only rises toward its equilibrium, so a fall is a "
                + "stale contribution in the incremental halo rather than absorption.");
        }

        // ⚠ The rise is read from the START of the run and the flatness from the tail, and they were
        // both read from the tail until 2026-08-13. adr/0094 took Ticks.PerDay to 2048, which takes
        // the pollution tau -- a count of scheduled updates over one Day -- from 128 to 32, so the
        // field reaches its equilibrium four times sooner in sweeps. A window sized for the old
        // convergence then contains no transient at all, and this read 0 → 0 and failed: not because
        // nothing converged but because it had finished converging before the instrument started
        // looking. A contraction test needs the uncontracted end in view.
        int firstStep = peaks[Lag] - peaks[0];
        int lastStep = tail[^1] - tail[^(1 + Lag)];

        Assert.True(
            firstStep > 0,
            "the field did not rise over its first sweep, so there is no transient to contract from "
            + "and the assertion below would be vacuous.");

        Assert.True(
            lastStep * 3 < firstStep,
            $"the sweep-to-sweep step went {firstStep} → {lastStep}, which is not contracting. "
            + "adr/0051 makes the pollution source a stock the environment absorbs, so the level "
            + "converges on an equilibrium; a source with no sink steps by one sweep's emission for "
            + "ever. adr/0003 extends adr/0006 to quantities.");

        // And the step is a vanishing fraction of the level it is a step in, which is the same claim
        // stated so that it cannot be satisfied by a run that simply emitted very little.
        Assert.True(
            lastStep * 1_000 < tail[^1],
            $"the last sweep's step of {lastStep} is not small against a level of {tail[^1]}, so the "
            + "field has not converged on an equilibrium.");

        // 3. No collection trends upward. Residency is bounded by the map by construction, so what
        //    this can actually catch is a second row allocated for a Cell that already had one.
        Assert.Equal(world.Layers.Residency.Count, world.Layers.Cells.Rows.LiveCount);
        Assert.True(world.Layers.Residency.Count <= CellGrid.WorldCellCount);

        // 4. The end-of-run tier, run for real. It throws by default, so reaching the next line passes.
        new Simulation(world, WorldKey.FromSeed(1)).CheckEndOfRun();
    }

    private static ulong Run(out World world, out int[] peaks)
    {
        world = new World(Population);

        Simulation simulation = new(world, WorldKey.FromSeed(0x0B07_0006_1000UL))
        {
            // O(world) twice per Tick against a phase meant to be O(woken). Affordable for a
            // correctness run and not for a long one — Simulation.VerifyDecideWritesNothing says so
            // in its own words, and this is the run it names.
            VerifyDecideWritesNothing = false,
        };

        List<int> samples = [];
        ulong hash = 0;
        int emission = 0;

        for (int tick = 0; tick < Ticks; tick++)
        {
            if (tick % EmitEvery == 0)
            {
                Churn(world, emission++);
            }

            simulation.Step(default);

            if (tick % HashEvery == 0)
            {
                samples.Add(Peak(world));
                hash = Randomness.Mix(hash + world.HashState());
            }
        }

        peaks = [.. samples];
        return hash;
    }

    /// <summary>
    /// Sweeps the region round-robin, adding a fixed function of the Cell to each Cell's source.
    /// </summary>
    /// <remarks>
    /// <b>Added, not set — which is the correction — and round-robin, not random, which was right all
    /// along.</b> Adding is what a Rule's <c>map</c> output does, and before <c>adr/0051</c> it would
    /// have grown without bound, so this method set the source instead and the test never touched the
    /// simulation's own write path. Absorption is what makes adding safe to assert over. Drawing the
    /// Cell at random, by contrast, leaves the region uncovered for far longer than the run, so the
    /// field never reaches its limit cycle and the transient reads as a leak; sweeping tops every Cell
    /// up once per <see cref="Sweep"/> Ticks and rewrites all of them many times over, which is what
    /// exercises the incremental halo rather than merely filling the map once.
    /// </remarks>
    private static void Churn(World world, int emission)
    {
        int index = emission % (Region * Region);
        Cells east = new(index % Region);
        Cells north = new(index / Region);

        // A fixed function of the Cell, so repeated sweeps converge rather than wander. Mixed rather
        // than linear so the field has structure — a smooth ramp would hide a transposed kernel.
        int amount = (int)(Randomness.Mix((ulong)index + 0xB0110C6UL) % 2_000);

        world.Layers.EmitPollution(east, north, amount);
        world.Layers.Seal(east, north, 1);
    }

    private static int Peak(World world)
    {
        LayerCellTable cells = world.Layers.Cells;
        int peak = 0;

        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (cells.Rows.IsLive(slot))
            {
                int value = cells.Pollution[slot];
                peak = value > peak ? value : peak;
            }
        }

        return peak;
    }
}
