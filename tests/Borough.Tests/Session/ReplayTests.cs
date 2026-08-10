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
    /// <b>Replay equivalence over a session in which Rules actually fire</b> — slice 7's own
    /// acceptance line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every other test in this file replays a log against <see cref="Ruleset.Empty"/>, which was the
    /// only thing available until task 10a and which asks nothing of the Rule engine at all. This one
    /// replays the golden session under the golden Ruleset, so the counter-based settle-order draw,
    /// the arming stagger, the wait lists and the Wheel are all inside the loop being checked.
    /// </para>
    /// <para>
    /// <b>The shuffle is what makes this worth a test rather than a formality.</b> Phase 3 orders
    /// contested intents by a draw, and a draw taken from anything but the world key, the entity and
    /// the Tick would reproduce within a run and diverge between two — which is precisely the failure
    /// this file exists to catch and precisely the failure that no single run can see.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_runs_of_one_log_agree_when_rules_fire()
    {
        InputLog log = Golden.GoldenFixtures.Session();
        var ticks = new Ticks(Golden.GoldenFixtures.Ticks);

        ulong[] first = Replay.Run(log, ticks, hashEvery: 1, Golden.GoldenFixtures.Rules());
        ulong[] second = Replay.Run(log, ticks, hashEvery: 1, Golden.GoldenFixtures.Rules());

        Assert.Equal(first, second);

        // And the Rules moved the city, so the agreement above is over something. Without this the
        // test would pass identically against a Ruleset the world never loaded.
        Assert.NotEqual(
            first,
            Replay.Run(log, ticks, hashEvery: 1, Ruleset.Empty));
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

    /// <summary>
    /// A command moves the hash.
    /// </summary>
    /// <remarks>
    /// <b>This asserted the flat half too, and a trace can no longer state it.</b> The Tick is a saved
    /// and hashed column now (<see cref="Borough.Core.Entities.ClockTable"/>), so every sample of every
    /// trace differs from the one before it by construction and <c>trace[3] == trace[4]</c> is not a
    /// claim about the city any more.
    /// <para>
    /// <b>The claim itself is not lost, it moved to where it can be stated honestly</b> —
    /// <c>SimulationTests.A_tick_with_no_commands_changes_nothing_but_the_clock</c>, which folds every
    /// table but the clock. A replay trace was always a proxy for it; the World is where the state is.
    /// What the trace still proves is what the test below it needs and this one keeps: two runs that
    /// diverge do so at exactly the Tick they diverged, because both carry the same clock.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_command_moves_the_hash()
    {
        // Commands land on Ticks 0, 1 and 2, so samples 0..2 each move on their own account.
        ulong[] trace = Replay.Run(Log(), new Ticks(8), hashEvery: 1);

        Assert.NotEqual(trace[0], trace[1]);
        Assert.NotEqual(trace[1], trace[2]);
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
