using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// The wake path held against a whole city, every Tick, rather than against a fixture.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>Invariant.WaiterIsBlockedByTheBinItNames</c> runs at end of run, and that is what hid this
/// for nine days.</b> <c>plans/0003</c> queue item 8 was filed on 2026-08-13 against exactly the world
/// below, having observed the invariant fire on four Bins and read as stable at Ticks 512, 2052 and
/// 4096 — from which it concluded a waiter was parked <em>for ever</em> and reasoned its way to a
/// trigger gap in <c>adr/0063</c>'s live wake predicate. It was one episode, 3,901 Ticks long, sampled
/// three times from inside. ***A defect sampled only at the end of a run cannot be told from a long
/// one***, and the two candidate repairs the item reserved — a Readout edge nobody issues, or a
/// cadence sweep that <c>adr/0033</c> forbids — were both answers to a question the evidence never
/// asked.
/// </para>
/// <para>
/// <b>The cause was queue item 11 and there was never a second defect.</b> <see cref="RuleEngine.Apply"/>
/// settles in shuffle order, so a producer's deposit can land before a consumer's failure is stopped;
/// <see cref="World.Drain"/> then walks a queue the consumer has not joined, and
/// <see cref="RuleEngine"/>'s stop parks it on a Bin already holding what it asked for. Nothing
/// re-examines a Bin that was not written to, so in a city where the next deposit is Days away the
/// waiter sleeps through them. Item 8's <em>falling requirement</em> hypothesis is refuted: it was
/// what remained after two others were eliminated, and the race was not among the candidates because
/// it had not been found yet.
/// </para>
/// <para>
/// <b>This checks every Tick because the end of a run is the one moment the defect was absent.</b>
/// With the drain in <c>RuleEngine.Stop</c> removed, the run below reports a mis-parked waiter on
/// 3,903 of its 4,096 Ticks and is <em>clean at the last one</em> — so an end-of-run assertion here
/// would pass against the bug it exists to catch.
/// </para>
/// </remarks>
public sealed class WaitListWakeTests
{
    /// <summary>Queue item 8's recorded reproduction, run against the wake path it accused.</summary>
    /// <remarks>
    /// <para>
    /// <b>The capacities are the reproduction and the rest of the file is the shipped one.</b> Item 8
    /// records <c>rulesets/minimal.toml</c> with the Goods amounts at ×4 — which is what ships — and
    /// the Bin capacities left at their old 12 and 1. That makes <c>consume</c>'s requirement
    /// (<c>amount</c> 4 × <c>occupants</c> 3 = 12) exactly the capacity, which is the observed
    /// signature: level 12, headroom 0, requirement 12, a waiter asking for precisely what is there.
    /// The shipped 48 and 4 leave slack and do not provoke it, which is why the queue could carry it.
    /// </para>
    /// <para>
    /// <b>4,096 Ticks rather than the 20,480 the diagnosis was taken over.</b> The episode opens at
    /// Tick 2, so the margin is four hundredfold and the long run bought only the streak length. An
    /// assertion pays for what it catches.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_waiter_in_a_starving_city_sleeps_on_a_bin_that_satisfies_it()
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(Reproduction(), "item-8-reproduction.toml");

        Assert.NotNull(loaded.Ruleset);

        InputLogBuilder builder = new(0UL, new WorldConfiguration(4_000), 0UL);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, loaded.Ruleset!);

        // Off because it folds the whole world twice a Tick and this test steps 4,096 of them. It
        // guards Phase 2, and the defect here is in Phase 3.
        simulation.VerifyDecideWritesNothing = false;

        World world = simulation.World;

        for (int step = 0; step < 4_096; step++)
        {
            Ticks tick = simulation.Tick;

            simulation.Step(new TickInput(log.At(tick), log.RulesetHashAt(tick)));

            int misparked = Misparked(world);

            Assert.True(
                misparked == 0,
                $"{misparked} waiter(s) are asleep on a Bin that satisfies them at Tick "
                + $"{simulation.Tick.Raw}. RuleEngine.Stop's drain is the wake they are owed.");
        }

        // The city has to have been putting waiters on Bins for the loop above to have meant anything.
        // Without this the test passes just as well on a world where nothing ever blocks.
        Assert.True(
            Parked(world) > 0,
            "no Rule Instance is parked on any Bin, so the wake path was never exercised.");
    }

    /// <summary><c>rulesets/minimal.toml</c> with the two Bin capacities item 8 names.</summary>
    private static string Reproduction()
    {
        string shipped = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));

        string reproduction = shipped
            .Replace(
                "{ resource = \"sundries\", capacity = 48 }",
                "{ resource = \"sundries\", capacity = 12 }",
                StringComparison.Ordinal)
            .Replace(
                "{ resource = \"repairs\",  capacity = 4 }",
                "{ resource = \"repairs\",  capacity = 1 }",
                StringComparison.Ordinal);

        Assert.NotEqual(shipped, reproduction);

        return reproduction;
    }

    /// <summary>How many waiters the end-of-run invariant would report, counted without throwing.</summary>
    private static int Misparked(World world)
    {
        int bad = 0;

        bad += Misparked(world, world.SupplyWaiters, Blocking.Supply);
        bad += Misparked(world, world.SpaceWaiters, Blocking.Space);

        return bad;
    }

    private static int Misparked(World world, IndexList waiters, Blocking blocking)
    {
        int bad = 0;

        long[] claims = new long[world.Bins.Rows.SlotCount];

        RuleEngine.AccumulateClaims(world, blocking, claims);

        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            if (!world.Bins.Rows.IsLive(bin))
            {
                continue;
            }

            // The invariant's own predicate rather than a second copy of it. This method restated the
            // walk -- every waiter or only the head, and against which level -- and queue item 14
            // changed both halves of that answer. RuleEngine.BinStillBlocks, which it used to call,
            // no longer exists.
            if (WorldInvariants.HeadThatShouldHaveWoken(world, waiters, blocking, claims, bin)
                != Rows.NoSlot)
            {
                bad++;
            }
        }

        return bad;
    }

    /// <summary>Every waiter on either list, whether or not its Bin still blocks it.</summary>
    private static int Parked(World world)
    {
        int parked = 0;

        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            if (!world.Bins.Rows.IsLive(bin))
            {
                continue;
            }

            foreach (int _ in world.SupplyWaiters.Walk(bin))
            {
                parked++;
            }

            foreach (int _ in world.SpaceWaiters.Walk(bin))
            {
                parked++;
            }
        }

        return parked;
    }
}
