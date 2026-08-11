using Borough.Core;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// What one trigger of <see cref="ZoneRuleBenchmarks"/> actually costs in Lots evaluated, and that
/// nothing it times can change the world it times.
/// </summary>
/// <remarks>
/// <para>
/// <b>The tripwire divides two timings, so the thing that has to be equal is what each timing did.</b>
/// A per-trigger cost at 256,000 Lots divided by one at 256 Lots says nothing about Zone size if the
/// two triggers evaluated different numbers of Lots — the ratio would then be measuring the sample,
/// which is the one quantity the task holds fixed. Holding it fixed is now the fixture's own work
/// rather than the Ruleset's, because <c>adr/0059</c> derives the sample from the Lot count, so the
/// counts are checked rather than assumed equal.
/// </para>
/// <para>
/// <b>The second assertion is the one that makes the benchmark legitimate at all.</b> A trigger is
/// repeatable only if it leaves the world alone; the moment one builds or demolishes, every later
/// invocation in the iteration is timing a different city, and BenchmarkDotNet will happily average
/// them. So the fixture is checked for inertness at every rung, in the counters that would show it.
/// </para>
/// <para>
/// <b>These assert the fixture, not the engine.</b> If a change to the sweep moves them, the number
/// to correct is the one published in <c>plans/0014 §9</c>, and this failing is the notice that it
/// moved.
/// </para>
/// </remarks>
public sealed class ZoneRuleBenchmarkFixtureTests
{
    /// <summary>Triggers per reading — the benchmark's Tick advances, so one Tick is not the unit.</summary>
    private const int Triggers = 64;

    /// <summary>Runs <see cref="Triggers"/> triggers exactly as the benchmark's loop does.</summary>
    private static ZoneActivity Swept(int lots)
    {
        Simulation simulation = ZoneRuleFixture.Arrange(lots);

        for (ulong tick = 1; tick <= Triggers; tick++)
        {
            simulation.Zoning.Sweep(new Ticks(tick));
        }

        return simulation.Zoning.Drain();
    }

    /// <summary>The interval is 1, so a Tick and a trigger are the same thing.</summary>
    [Theory]
    [InlineData(ZoneRuleFixture.SmallestZone)]
    [InlineData(2_560)]
    [InlineData(25_600)]
    public void Every_tick_triggers(int lots)
    {
        Assert.Equal(Triggers, Swept(lots).Triggers.Sum);
    }

    /// <summary>
    /// Nothing the benchmark times can change the world it times, which is what makes an invocation
    /// repeatable.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are checked because they fail for different reasons.</b> Creation is stopped by
    /// <c>adr/0055</c>'s permission term — the Rule admits a bit no Lot carries — and demolition by a
    /// threshold nothing reaches. Either one silently coming true would turn a flat column into a
    /// measurement of construction, and the timing would still look plausible.
    /// </remarks>
    [Theory]
    [InlineData(ZoneRuleFixture.SmallestZone)]
    [InlineData(2_560)]
    [InlineData(25_600)]
    public void The_sweep_leaves_the_world_alone(int lots)
    {
        ZoneActivity activity = Swept(lots);

        Assert.Equal(0, activity.Created.Sum);
        Assert.Equal(0, activity.Demolished.Sum);
    }

    /// <summary>
    /// Both branches of the sample are exercised at every rung, so neither timing is a statement about
    /// half the mechanism.
    /// </summary>
    [Theory]
    [InlineData(ZoneRuleFixture.SmallestZone)]
    [InlineData(2_560)]
    [InlineData(25_600)]
    public void Both_branches_are_entered(int lots)
    {
        ZoneActivity activity = Swept(lots);

        Assert.True(activity.Vacant.Sum > 0, "no vacant Lot was evaluated; the create branch is untimed");
        Assert.True(activity.Occupied.Sum > 0, "no occupied Lot was evaluated; the condemn branch is untimed");
    }

    /// <summary>
    /// <b>The denominator and the numerator evaluate the same number of Lots</b>, exactly.
    /// </summary>
    /// <remarks>
    /// <b>It used to be a one-sided bound and task 11c made it an equality.</b> While the sampler
    /// discarded a repeated draw, the smallest Zone could evaluate fewer Lots per trigger than the
    /// largest — collisions being likelier in a small population — which would have flattered the
    /// ratio by shrinking its divisor's work, so the bound was stated at 97% on that end. Sampling is
    /// now with replacement (<c>adr/0059</c>, and <c>ZoneSample</c> says why), so a trigger evaluates
    /// its sample and nothing about the Zone's size can change that. Stating it here is what lets
    /// <c>plans/0014 §9</c> publish a ratio as a per-trigger cost rather than as a cost per
    /// <em>evaluated Lot</em>, which would need this number in the divisor too.
    /// </remarks>
    [Fact]
    public void The_smallest_zone_evaluates_what_the_largest_does()
    {
        ZoneActivity smallest = Swept(ZoneRuleFixture.SmallestZone);
        ZoneActivity largest = Swept(25_600);

        long small = smallest.Vacant.Sum + smallest.Occupied.Sum;
        long large = largest.Vacant.Sum + largest.Occupied.Sum;

        Assert.Equal(Triggers * ZoneRuleFixture.Sample, (int)large);
        Assert.Equal(large, small);
    }
}
