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
/// of the Cell. The source field therefore converges to a known constant after one full sweep, and the
/// assertion becomes <b>exact equality across the tail</b> rather than a trend line — which an
/// accumulating implementation fails on the first sample after convergence.
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

    /// <summary>Ticks for one full sweep of the region, after which the source field is settled.</summary>
    private const int Sweep = Region * Region * EmitEvery;

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

        // 2. No magnitude trends upward. Past the first full sweep the source field is settled, so a
        //    correct implementation reports the identical peak at every later sample. An accumulating
        //    one, or a halo that left a stale contribution behind, does not.
        int[] tail = peaks[((Sweep / HashEvery) + 1)..];

        for (int i = 1; i < tail.Length; i++)
        {
            Assert.True(
                tail[i] == tail[0],
                $"peak pollution moved from {tail[0]} to {tail[i]} after the sources had settled. "
                + "adr/0003 extends adr/0006 to quantities: no quantity accumulates without bound.");
        }

        Assert.True(tail[0] > 0, "the run diffused nothing, so it asserted nothing.");

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
    /// Sweeps the region round-robin, setting each Cell's emission to a fixed function of the Cell.
    /// </summary>
    /// <remarks>
    /// <b>Set, not added, and round-robin, not random. Both were needed and neither was obvious.</b>
    /// Adding forever would manufacture the unbounded growth the test then reports; drawing the Cell
    /// at random leaves the region uncovered for far longer than the run, so the field never settles
    /// and the transient reads as a leak. Sweeping means the source field is a known constant after
    /// <see cref="Sweep"/> Ticks, and every Cell is rewritten many times over — which is what exercises
    /// the incremental halo rather than merely filling the map once.
    /// </remarks>
    private static void Churn(World world, int emission)
    {
        int index = emission % (Region * Region);
        Cells east = new(index % Region);
        Cells north = new(index / Region);

        // A fixed function of the Cell, so repeated sweeps converge rather than wander. Mixed rather
        // than linear so the field has structure — a smooth ramp would hide a transposed kernel.
        int amount = (int)(Randomness.Mix((ulong)index + 0xB0110C6UL) % 2_000);

        int slot = world.Layers.Residency.Slot(east, north);
        int already = slot == CellResidency.NotResident
            ? 0
            : world.Layers.Cells.PollutionSource[slot];

        world.Layers.EmitPollution(east, north, amount - already);
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
