using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 27 task 8: a Household founds a Business (<c>adr/0145</c>).
/// </summary>
public sealed class FoundingTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out = output;

    private static Ruleset Load(string file)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    private static (World World, Simulation Simulation) City(string file, ulong seed, int citizens)
    {
        var key = WorldKey.FromSeed(seed);
        var world = new World(citizens, Load(file));

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, new Simulation(world, key));
    }

    /// <summary>
    /// The shipped founding Ruleset creates Businesses, and no other shipped file does.
    /// </summary>
    [Fact]
    public void The_shipped_founding_ruleset_creates_businesses_and_taxed_creates_none()
    {
        (World founded, Simulation running) = City("founded.toml", 0xF0DDU, 2_000);

        for (int tick = 0; tick < 4_096; tick++)
        {
            running.Step(TickInput.Empty);
        }

        PlacementActivity activity = running.Placement.Drain();

        _out.WriteLine($"founded.toml: founded={activity.Founded.Sum} retired={activity.Retired.Sum} live={founded.Businesses.Rows.LiveCount} pool={founded.UnpremisedPool.Count}");

        Assert.True(
            activity.Founded.Sum > 0,
            "founded.toml founded no Business in 4,096 Ticks. It is the only shipped file with a "
            + "[founding] table, and if nothing is founded either the means test refuses every "
            + "Household -- check [households] opening_balance against founding_band -- or the pass "
            + "is not running.");

        // The control. taxed.toml is founded.toml minus the three added tables, so if IT founds
        // something the founding is coming from somewhere other than [founding].
        (World control, Simulation controlRunning) = City("taxed.toml", 0xF0DDU, 2_000);

        for (int tick = 0; tick < 4_096; tick++)
        {
            controlRunning.Step(TickInput.Empty);
        }

        Assert.Equal(0, control.Businesses.Rows.LiveCount);
    }

    /// <summary>
    /// The founding channel has a sink, and a run long enough to reach it drains the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is <c>adr/0006</c>'s obligation for the channel milestone 27 task 8 added</b>, and
    /// it is the reason the loader refuses <c>[founding]</c> in a file with no
    /// <c>gives_up_after_days</c>. Nothing tenants a Business, so ***every shop founded here leaves
    /// eventually*** and the pool's size is what must stay bounded.
    /// </para>
    /// <para>
    /// <b>The bound is patched down rather than the run being made long.</b> The shipped file waits
    /// <b>30 Days</b> — 61,440 Ticks — so an honest test of the sink at the shipped value would be a
    /// two-minute assertion, which <c>TierBudgetTests</c> is right to dislike. Editing the text and
    /// parsing it is <c>ReachFailureTests</c>'s trick, and the thing under test is the mechanism
    /// rather than the number.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_founded_business_nothing_tenants_eventually_leaves()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "founded.toml"));

        Assert.Contains("gives_up_after_days = 30", toml, StringComparison.Ordinal);

        RulesetLoadResult loaded = RulesetLoader.Parse(
            toml.Replace("gives_up_after_days = 30", "gives_up_after_days = 1", StringComparison.Ordinal),
            "founded-impatient.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0xF0DDU);
        var world = new World(2_000, loaded.Ruleset!);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var running = new Simulation(world, key);

        for (int tick = 0; tick < 8_192; tick++)
        {
            running.Step(TickInput.Empty);
        }

        PlacementActivity activity = running.Placement.Drain();

        _out.WriteLine(
            $"impatient: founded={activity.Founded.Sum} retired={activity.Retired.Sum} "
            + $"pool={world.UnpremisedPool.Count}");

        Assert.True(activity.Founded.Sum > 0, "nothing was founded, so the sink is untested.");

        Assert.True(
            activity.Retired.Sum > 0,
            $"{activity.Founded.Sum} Businesses were founded and NONE was retired over 8,192 Ticks "
            + "with a one-Day bound. Nothing tenants a Business, so the unpremised pool has an inflow "
            + "and no outflow -- which is the adr/0006 hole the gives_up_after_days refusal exists to "
            + "prevent, reached anyway.");

        // The pool is BOUNDED rather than empty, which is the claim adr/0006 actually makes: the
        // drain rate is proportional to the stock, so the size settles rather than the pool emptying.
        Assert.True(
            world.UnpremisedPool.Count < activity.Founded.Sum,
            $"the pool holds {world.UnpremisedPool.Count} of {activity.Founded.Sum} ever founded, so "
            + "nothing is actually leaving it.");
    }
}
