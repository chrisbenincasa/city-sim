using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Tests.Session;

/// <summary>
/// CI lint 5: two runs of one Input Log produce identical State Hash sequences.
/// </summary>
/// <remarks>
/// This is the property every later debugging tool is downstream of. Bisection, save/reload
/// equivalence and the golden-hash regression are all consumers, and since <c>adr/0037</c> deleted
/// the double buffer, crash forensics is <em>entirely</em> replay plus the log.
/// </remarks>
public sealed class ReplayTests
{
    private const int TickCount = 32;

    [Fact]
    public void Two_runs_of_one_log_produce_identical_traces()
    {
        InputLog log = Log();

        ulong[] first = Replay.Run(log, new Ticks(TickCount), hashEvery: 1);
        ulong[] second = Replay.Run(log, new Ticks(TickCount), hashEvery: 1);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// Two logs differing in one command produce different cities. Stated as a test because the
    /// interesting failure is the opposite one — a replay that ignores part of its log and passes the
    /// equality test above by reproducing the same wrong thing twice.
    /// </summary>
    [Fact]
    public void A_different_log_produces_a_different_trace()
    {
        ulong[] first = Replay.Run(Log(), new Ticks(TickCount), hashEvery: 1);
        ulong[] second = Replay.Run(Log(eastOffset: 1), new Ticks(TickCount), hashEvery: 1);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void The_trace_samples_at_the_requested_cadence()
    {
        InputLog log = Log();

        Assert.Equal(TickCount, Replay.Run(log, new Ticks(TickCount), hashEvery: 1).Length);
        Assert.Equal(TickCount / 4, Replay.Run(log, new Ticks(TickCount), hashEvery: 4).Length);
    }

    [Fact]
    public void A_command_moves_the_hash_and_an_idle_tick_does_not()
    {
        // Commands land on Ticks 0, 1 and 2, so samples 0..2 move and everything after is flat.
        ulong[] trace = Replay.Run(Log(), new Ticks(8), hashEvery: 1);

        Assert.NotEqual(trace[0], trace[1]);
        Assert.NotEqual(trace[1], trace[2]);
        Assert.Equal(trace[3], trace[4]);
        Assert.Equal(trace[4], trace[7]);
    }

    [Fact]
    public void Running_past_the_last_command_is_ordinary()
    {
        InputLog log = Log();

        Assert.Equal(3UL, log.Horizon.Raw);
        Assert.Equal(1_000, Replay.Run(log, new Ticks(1_000), hashEvery: 1).Length);
    }

    /// <summary>
    /// <b>The bisection property, proven rather than assumed.</b> A single mutated field mid-run must
    /// diverge the trace at <em>exactly</em> the Tick it happened — that is what makes "the first
    /// differing hash names the Tick the bug entered" a usable claim instead of a hopeful one.
    /// </summary>
    [Fact]
    public void A_mutation_mid_run_diverges_the_trace_at_exactly_the_expected_sample()
    {
        const int MutateAfter = 5;

        ulong[] clean = ByHand(mutateAfterSample: -1);
        ulong[] disturbed = ByHand(mutateAfterSample: MutateAfter);

        for (int i = 0; i <= MutateAfter; i++)
        {
            Assert.Equal(clean[i], disturbed[i]);
        }

        Assert.NotEqual(clean[MutateAfter + 1], disturbed[MutateAfter + 1]);
    }

    /// <summary>
    /// The world a log describes is built from the log alone — there is no separate initial-state
    /// file, because state arriving from somewhere the log does not describe is state no divergence
    /// can be attributed to.
    /// </summary>
    [Fact]
    public void A_replay_starts_from_an_empty_world_at_tick_zero()
    {
        Simulation simulation = Replay.Start(Log(), Ruleset.Empty);

        Assert.Equal(0UL, simulation.Tick.Raw);
        Assert.Equal(0, simulation.World.Lots.Rows.LiveCount);
        Assert.Equal(0, simulation.World.Citizens.Rows.LiveCount);
    }

    /// <summary>
    /// Steps a log by hand so a saved field can be disturbed between two Ticks, which
    /// <see cref="Replay.Run"/> deliberately gives no way to do.
    /// </summary>
    private static ulong[] ByHand(int mutateAfterSample)
    {
        InputLog log = Log();
        Simulation simulation = Replay.Start(log, Ruleset.Empty);
        var trace = new List<ulong>();

        for (int i = 0; i < TickCount; i++)
        {
            Ticks tick = simulation.Tick;
            simulation.Step(new TickInput(log.At(tick), log.RulesetHashAt(tick)));

            trace.Add(simulation.World.HashState());

            if (i == mutateAfterSample)
            {
                simulation.World.Lots.Zone[0] += 1;
            }
        }

        return [.. trace];
    }

    /// <summary>Three Zone commands on three consecutive Ticks, then silence.</summary>
    private static InputLog Log(int eastOffset = 0)
    {
        InputLogBuilder builder = new(
            seed: 0x0B07_0000_0000_0001UL,
            new WorldConfiguration(64),
            rulesetHash: 0);

        for (int i = 0; i < 3; i++)
        {
            builder.Append(
                new Ticks((ulong)i),
                new Command(CommandKind.Zone, new Tiles(i + eastOffset), new Tiles(i), zone: 1));
        }

        return builder.Build();
    }
}
