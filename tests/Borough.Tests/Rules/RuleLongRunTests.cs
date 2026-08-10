using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0011</c>'s acceptance run: 100,000 Ticks of a city with a Ruleset in force.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the <em>flow</em> half of slice 5 task 7's trend assertion, and the other half is not
/// this slice's.</b> The assertion was written as <em>a positive trend in <c>slots</c> with
/// <c>live</c> flat</em> — table rows churning — and finding 2 established that a Rule Instance's life
/// is its Building's and that subscription allocates nothing at all. So no Ruleset can make a table's
/// slot count trend: what churns rows is Buildings arriving and being demolished, which is Zone Rules
/// and slice 10. What the Rule engine can carry is <c>evaluations</c> rising against a flat
/// <c>due</c>, which is <c>adr/0006</c>'s shape arriving as a flow rather than as a collection —
/// chain walking growing without the city growing.
/// </para>
/// <para>
/// <b>This assertion used to be exact equality, and slice 10 task 7 took that away by making the
/// city move.</b> It was earned: <c>rulesets/minimal.toml</c> settled into a cycle whose period was
/// <c>consume</c>'s rate, every Building did the same work every 32 Ticks whatever offset the arming
/// stagger gave it, and summed over a whole number of periods the counters were not merely flat but
/// <em>identical</em>. That rested on one premise stated in the original comment — <em>no Building
/// created or demolished</em> — and a <c>[[zone_rule]]</c> that condemns and rebuilds is precisely
/// the thing that falsifies it. The premise was not wrong; it expired, which is worth distinguishing.
/// </para>
/// <para>
/// <b>What replaced it is bounded rather than exact, because the churn is aperiodic.</b> Buildings
/// are condemned in loose cohorts — the sampler is the pacemaker and it is not in phase with anything
/// — so the interval totals wobble by tens of percent with no trend underneath. Two things are still
/// exactly true and are asserted as such: <c>slots</c> does not move, which is <c>adr/0006</c>'s
/// collection half reaching the one place nothing has ever been able to check it, and <c>live</c>
/// does, which is what stops the first assertion passing over a run in which nothing happened.
/// </para>
/// <para>
/// <b>The collection half arrived here early.</b> <c>plans/0014</c> gives it to task 10; task 7 is
/// what made rows churn, and a long-run test left asserting <c>LiveCount == SlotCount</c> after
/// demolition exists would have been red rather than merely weak. Task 10 owns the sharper form — a
/// reading interval that is a whole number of the Zone Rule's trigger period, so the assertion is not
/// about the sampling phase.
/// </para>
/// </remarks>
public class RuleLongRunTests
{
    private const int Ticks = 100_000;
    private const int Population = 1_000;

    /// <summary>
    /// The interval a reading covers. <b>A whole number of <c>consume</c>'s 32-Tick periods</b>, so
    /// the sums compare exactly; an interval that cut a period in half would wobble by the remainder
    /// and force the assertion back down to a trend line.
    /// </summary>
    private const int ReadEvery = 2_048;

    /// <summary>
    /// How much of the run is discarded as the transient. The Bin fills from empty in about forty
    /// Ticks; this is two orders of magnitude of headroom over that, and it costs the assertion
    /// nothing because what follows is 47 identical readings.
    /// </summary>
    private const int Settle = 4_096;

    [Fact]
    public void The_hundred_thousand_Tick_acceptance_run()
    {
        RuleActivity[] readings =
            Run(out World world, out Opening opening, out Simulation simulation);
        RuleActivity[] tail = readings[(Settle / ReadEvery)..];

        // The run has to have done something, or every line below passes over nothing. This is the
        // vacuity slice 5 task 7 refused to ship an assertion into, stated rather than assumed.
        Assert.True(tail[0].Due.Sum > 0, "no Rule came due in the whole run.");
        Assert.True(tail[0].Evaluations.Sum > tail[0].Due.Sum, "no Rule was evaluated twice.");

        long early = Mean(tail[..(tail.Length / 2)]);
        long late = Mean(tail[(tail.Length / 2)..]);

        Assert.True(
            late <= early + (early / 8),
            $"Rule evaluations drifted upward across the tail: {early} per {ReadEvery} Ticks over "
            + $"the first half against {late} over the second. A city of bounded size cannot "
            + "produce steadily more work with elapsed time, which is adr/0006 arriving as a flow.");

        // The band is one direction only. A city that quietened down is not this test's business:
        // adr/0006 is about growth, and demanding symmetry would fail the run where a cohort of
        // Buildings happened to be mid-demolition at the last reading.
        Assert.True(
            Max(tail) <= Min(tail) * 2,
            $"the interval total ranged from {Min(tail)} to {Max(tail)} across the tail. Cohort "
            + "churn wobbles it; a factor of two is a different shape.");

        // The collection half, and slice 10 task 7 is what made it testable: before demolition
        // existed nothing could free a row, so `slots` could not have trended whatever the Ruleset
        // said. A rising slot count against a bounded city is freed rows not being reused — the table
        // growing to the high-water mark of a *cycle* rather than to the size of the city.
        Assert.Equal(opening.Bins, world.Bins.Rows.SlotCount);
        Assert.Equal(opening.Instances, world.RuleInstances.Rows.SlotCount);

        // And the guard that keeps the two lines above from passing over a run in which nothing was
        // ever demolished, which is how they would read if the Ruleset lost its [[zone_rule]].
        Assert.True(
            world.Bins.Rows.LiveCount < opening.Bins,
            $"every Bin slot is still live after {Ticks} Ticks, so nothing was ever demolished and "
            + "the slot-count assertions above are vacuous.");

        // The end-of-run tier, run for real. It throws by default, so reaching the next line passes.
        //
        // On the Simulation that ran, not a fresh one over the same world. A new Simulation's _tick is
        // 0, so every violation this tier reported was stamped Tick 0 on a world 100,000 Ticks old —
        // harmless while nothing read the stamp, and wrong the moment something did.
        // AnArmedRowIsDueWithinOnePeriod is the first end-of-run invariant to be relative to now, and
        // it read every armed row as due a whole run in the future.
        simulation.CheckEndOfRun();
    }

    private static long Mean(RuleActivity[] readings)
    {
        long total = 0;

        foreach (RuleActivity reading in readings)
        {
            total += reading.Evaluations.Sum;
        }

        return total / readings.Length;
    }

    private static long Min(RuleActivity[] readings)
    {
        long least = long.MaxValue;

        foreach (RuleActivity reading in readings)
        {
            if (reading.Evaluations.Sum < least)
            {
                least = reading.Evaluations.Sum;
            }
        }

        return least;
    }

    private static long Max(RuleActivity[] readings)
    {
        long most = 0;

        foreach (RuleActivity reading in readings)
        {
            if (reading.Evaluations.Sum > most)
            {
                most = reading.Evaluations.Sum;
            }
        }

        return most;
    }

    /// <summary>The city's size, read once before anything has been demolished.</summary>
    private readonly record struct Opening(int Bins, int Instances, int Buildings);

    private static RuleActivity[] Run(
        out World world, out Opening opening, out Simulation simulation)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);

        world = new World(Population, GoldenFixtures.Rules());

        simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken), which is what
            // --no-decide-guard exists for on the long runs. The guard's own correctness is covered
            // by the tests written for it.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        // The high-water mark, taken before a single Building has been demolished. Every row the run
        // frees must be handed back out rather than appended beside, so these are what the run may
        // not exceed — and because the populator builds one Building per Lot, they are the size of
        // the city rather than a number this test chose.
        opening = new Opening(
            world.Bins.Rows.SlotCount,
            world.RuleInstances.Rows.SlotCount,
            world.Buildings.Rows.LiveCount);

        List<RuleActivity> readings = [];

        for (int tick = 0; tick < Ticks; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % ReadEvery == 0)
            {
                readings.Add(simulation.Rules.Drain());
            }
        }

        return [.. readings];
    }
}
