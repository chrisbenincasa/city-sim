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
/// <b>The assertion is exact equality across the tail rather than a trend line, and the Ruleset is
/// what earns that.</b> <c>rulesets/minimal.toml</c> settles into a cycle whose period is
/// <c>consume</c>'s rate: the Bin sits at its ceiling, <c>restock</c> sleeps on headroom, one draw
/// wakes it, it refills and sleeps again. Every 32 Ticks each Building therefore performs the same
/// three due Rules and the same five evaluations, whatever offset the arming stagger gave it — a
/// phase shift moves when a Building does its work and not how much. Summed over an interval that is
/// a whole number of periods, the counters are not merely flat: they are <em>identical</em>, and an
/// implementation that leaked one subscription or walked one extra rung fails on the first sample
/// after the transient rather than on a slope somebody has to argue about.
/// </para>
/// <para>
/// <b>A trend line was the alternative and it is weaker here for the reason
/// <c>LayerLongRunTests</c> found</b>: a slope fitted over a run that has not reached steady state
/// measures the transient, and the honest fix in both cases was to build a fixture that converges to
/// something known. This one converges by Tick 40 or so; the tail starts far later than that.
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
        RuleActivity[] readings = Run(out World world);
        RuleActivity[] tail = readings[(Settle / ReadEvery)..];

        // The run has to have done something, or every line below passes over nothing. This is the
        // vacuity slice 5 task 7 refused to ship an assertion into, stated rather than assumed.
        Assert.True(tail[0].Due.Sum > 0, "no Rule came due in the whole run.");
        Assert.True(tail[0].Evaluations.Sum > tail[0].Due.Sum, "no Rule was evaluated twice.");

        for (int i = 1; i < tail.Length; i++)
        {
            Assert.True(
                tail[i].Evaluations.Sum == tail[0].Evaluations.Sum,
                $"Rule evaluations moved from {tail[0].Evaluations.Sum} to {tail[i].Evaluations.Sum} "
                + $"per {ReadEvery} Ticks, with due Rule Instances at {tail[i].Due.Sum} against "
                + $"{tail[0].Due.Sum}. Evaluations rising against a flat due is chain walking growing "
                + "without the city growing, which is adr/0006 arriving as a flow.");

            Assert.True(
                tail[i].Due.Sum == tail[0].Due.Sum,
                $"due Rule Instances moved from {tail[0].Due.Sum} to {tail[i].Due.Sum} per "
                + $"{ReadEvery} Ticks. The city is the same size throughout, so the scheduled load "
                + "should be too: a Rule is re-armed once per firing and subscribes instead of "
                + "re-arming when it fails.");

            // The peak is the burst the sum cannot see (02 §4, task 9). A run whose sum held while
            // its worst Tick doubled is a run that concentrated the same work into fewer Ticks.
            Assert.True(
                tail[i].Evaluations.Peak == tail[0].Evaluations.Peak,
                $"the worst Tick moved from {tail[0].Evaluations.Peak} evaluations to "
                + $"{tail[i].Evaluations.Peak}, with the interval's total unchanged. Burstiness under "
                + "this design is authored, so a peak that moves on its own is the stagger collapsing.");
        }

        // The collection half, as far as this slice can carry it: a Rule Instance's life is its
        // Building's, so with no Building created or demolished these are constants. The **slots**
        // half of the assertion is re-filed to slice 10, which is what makes rows churn.
        Assert.Equal(world.Bins.Rows.LiveCount, world.Bins.Rows.SlotCount);
        Assert.Equal(world.RuleInstances.Rows.LiveCount, world.RuleInstances.Rows.SlotCount);

        // The end-of-run tier, run for real. It throws by default, so reaching the next line passes.
        new Simulation(world, WorldKey.FromSeed(GoldenFixtures.Seed)).CheckEndOfRun();
    }

    private static RuleActivity[] Run(out World world)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);

        world = new World(Population, LayerRuleset.Default, GoldenFixtures.Rules());

        Simulation simulation = new(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken), which is what
            // --no-decide-guard exists for on the long runs. The guard's own correctness is covered
            // by the tests written for it.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

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
