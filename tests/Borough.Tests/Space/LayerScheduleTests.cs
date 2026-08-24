using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// The staggered schedule, and the measurement that settles what kind of number a cadence is.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §1.2</c> files Map Layer diffusion cadence under <em>tuning</em>. <c>plans/0009</c> doubts
/// it and asks for the argument. <c>adr/0043</c> says not to have the argument</b> — <em>a claim a
/// measurement could settle must not be settled by argument</em> — so the claim is typed first. It is
/// <b>measurable</b>: the number that would refute <em>the cadence is free to tune</em> is a State
/// Hash, and the machine that produces it is this file.
/// </para>
/// <para>
/// <b>The result is below and it is not a formality.</b> Five claims in the corpus have been measured
/// false so far and two of them sat in rows <c>plans/0002</c> marks fully argued, so a documented
/// classification is not evidence that anybody checked.
/// </para>
/// </remarks>
public class LayerScheduleTests
{
    private const int Population = 1_000;
    private const int Ticks = 400;

    [Fact]
    public void The_default_cadences_are_what_the_design_documents_state()
    {
        // 02 §2.4: pollution every 64 Ticks, land value every 256.
        Assert.Equal(64, LayerSchedule.Default.IndustrialPollution.Period);
        Assert.Equal(256, LayerSchedule.Default.LandValue.Period);
    }

    /// <summary>
    /// Sealing is due once a Day, and it was due <b>never</b> until milestone 24 task 4.
    /// </summary>
    /// <remarks>
    /// This test asserted the opposite for two milestones, on the reasoning that Sealing changes on
    /// build rather than on a clock. That is true of it <em>rising</em> and false of it
    /// <em>recovering</em>: ground heals whether or not anything is built anywhere, so the pass that
    /// heals it needs a clock. <c>CONTEXT.md</c> → Sealing states the intent in <b>Days</b>, so the
    /// period is a Day and the tau is a count of Days with nothing to convert.
    /// </remarks>
    [Fact]
    public void Sealing_is_due_once_a_Day()
    {
        int due = 0;

        for (ulong tick = 0; tick < Borough.Core.Quantities.Ticks.PerDay; tick++)
        {
            if (LayerSchedule.Default.IsDue(Layer.Sealing, new Ticks(tick)))
            {
                due++;
                Assert.Equal(48UL, tick);
            }
        }

        Assert.Equal(1, due);
        Assert.Equal(Borough.Core.Quantities.Ticks.PerDay, LayerSchedule.Default.Sealing.Period);
    }

    /// <summary>
    /// No Tick carries both Layers, which is the whole content of the word <em>staggered</em>.
    /// </summary>
    /// <remarks>
    /// <c>05 §9</c>: a spike every 64 Ticks is a visible stutter, and the same work spread across those
    /// 64 Ticks is not. Asserted over a full land-value cycle, because the collision the stagger
    /// prevents happens only on Ticks divisible by both periods — four times a Day, which is exactly
    /// often enough to be a reproducible hitch and rare enough to be blamed on something else.
    /// </remarks>
    [Fact]
    public void No_Tick_is_due_for_more_than_one_Layer()
    {
        for (ulong tick = 0; tick < 2_048; tick++)
        {
            Ticks at = new(tick);

            int due = (LayerSchedule.Default.IsDue(Layer.IndustrialPollution, at) ? 1 : 0)
                + (LayerSchedule.Default.IsDue(Layer.LandValue, at) ? 1 : 0)
                + (LayerSchedule.Default.IsDue(Layer.Sealing, at) ? 1 : 0);

            Assert.True(due <= 1, $"Tick {tick} is due for {due} Layers.");
        }
    }

    [Fact]
    public void A_cadence_fires_on_its_offset_and_nowhere_else_in_the_cycle()
    {
        LayerCadence cadence = new(Period: 64, Offset: 16);

        for (ulong tick = 0; tick < 256; tick++)
        {
            Assert.Equal(tick % 64 == 16, cadence.IsDue(new Ticks(tick)));
        }
    }

    /// <summary>
    /// <b>THE MEASUREMENT. Two cadences produce two hash traces, so a cadence is not tuning.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>05 §4</c>: <em>a change is an optimisation if the State Hash is unchanged, and a design
    /// change otherwise, however it was motivated.</em> Two worlds identical in every respect but the
    /// diffusion period, driven by identical emissions, produce different State Hashes at Ticks
    /// between the two schedules' firings. So the cadence is a <b>design change</b> and
    /// <c>02 §1.2</c>'s tuning row is wrong.
    /// </para>
    /// <para>
    /// <b>This is the same welding failure <c>adr/0034</c> found in Chunk size, one document later</b>,
    /// and it is worth naming as such: <c>05 §4</c>'s hash rule is only as good as somebody running it
    /// against each number <em>by name</em>. Nobody had run it against this one.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_diffusion_cadences_produce_two_hash_traces()
    {
        ulong[] slow = Trace(new LayerSchedule(new LayerCadence(64, 0), new LayerCadence(256, 16), LayerSchedule.Default.Sealing));
        ulong[] fast = Trace(new LayerSchedule(new LayerCadence(32, 0), new LayerCadence(256, 16), LayerSchedule.Default.Sealing));

        Assert.NotEqual(slow, fast);
    }

    /// <summary>
    /// <b>And the divergence is transient, which is the finding rather than a caveat.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Once emissions stop and both cadences have fired, the two fields are identical — because a Layer
    /// is a convolution of its sources and not a function of its own history. So the cadence does not
    /// change what the field <em>settles to</em>; it changes <b>when a source's contribution becomes
    /// visible to a Rule that reads the Cell</b>. That is precisely the sentence <c>plans/0009</c>
    /// wrote, now with a number under it.
    /// </para>
    /// <para>
    /// <b>It is still hash-bearing, and the transience makes it worse rather than better.</b> A city
    /// is never in the settled state — sources change as it builds, which is the entire game — so the
    /// Rules that read these Cells read the transient every time. A defect that vanishes once you stop
    /// playing is not a defect that vanishes.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_settled_field_is_the_same_at_either_cadence()
    {
        World slow = Build(new LayerSchedule(new LayerCadence(64, 0), new LayerCadence(256, 16), LayerSchedule.Default.Sealing));
        World fast = Build(new LayerSchedule(new LayerCadence(32, 0), new LayerCadence(256, 16), LayerSchedule.Default.Sealing));

        foreach (World world in (World[])[slow, fast])
        {
            Emit(world, round: 0);
            Emit(world, round: 1);
            world.Layers.DiffusePollution();
        }

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Assert.Equal(
                    slow.Layers.Pollution(new Cells(east), new Cells(north)),
                    fast.Layers.Pollution(new Cells(east), new Cells(north)));
            }
        }
    }

    /// <summary>A world's State Hash on every Tick of a run whose sources keep changing.</summary>
    private static ulong[] Trace(LayerSchedule schedule)
    {
        World world = Build(schedule);
        Simulation simulation = new(world, WorldKey.FromSeed(0x0B07_0006UL));

        ulong[] trace = new ulong[Ticks];

        for (int tick = 0; tick < Ticks; tick++)
        {
            // Sources change while the city runs, which is the ordinary case and the only one where
            // the cadence can be observed. A run that emitted once before Tick 0 would let both
            // schedules settle before the first sample and would report the cadence as free.
            if (tick % 50 == 10)
            {
                Emit(world, tick / 50);
            }

            simulation.Step(default);
            trace[tick] = world.HashState();
        }

        return trace;
    }

    private static World Build(LayerSchedule schedule) => new(Population, schedule);

    private static void Emit(World world, int round)
    {
        for (int i = 0; i < 4; i++)
        {
            ulong draw = Randomness.Mix((((ulong)round + 1) << 24) + (ulong)i + 0xF00DUL);

            world.Layers.EmitPollution(
                new Cells((int)(draw % CellGrid.WorldCells)),
                new Cells((int)((draw >> 16) % CellGrid.WorldCells)),
                1 + (int)((draw >> 32) % 300));
        }
    }
}
