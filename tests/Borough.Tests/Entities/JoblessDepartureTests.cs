using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;
using Xunit.Abstractions;

namespace Borough.Tests.Entities;

public sealed class JoblessDepartureTests(ITestOutputHelper output)
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    // Stage bytes follow declaration order in jobs-attract.toml.
    private const byte Young = 1;
    private const byte Family = 2;

    private static string Fixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"))
            .ReplaceLineEndings("\n");

    private static Ruleset Reluctant(int familyLeavingCosts)
    {
        string text = Fixture().Replace(
            "next          = \"mature_family\"\n",
            $"next          = \"mature_family\"\nleaving_costs_rent = {familyLeavingCosts}\n",
            StringComparison.Ordinal);
        return JobsAttractTests.Rules(text);
    }

    [Fact]
    public void Leaving_costs_default_to_zero_and_a_negative_figure_is_refused()
    {
        Ruleset rules = JobsAttractTests.Rules();
        Assert.Equal(0, rules.LeavingCost(Young));
        Assert.Equal(0, rules.LeavingCost(Family));

        Assert.True(Reluctant(1_200).LeavingCost(Family) > 0);

        string text = Fixture().Replace(
            "next          = \"mature_family\"\n",
            "next          = \"mature_family\"\nleaving_costs_rent = -1\n",
            StringComparison.Ordinal);
        RulesetLoadResult result = RulesetLoader.Parse(text, "negative.toml");
        Assert.Null(result.Ruleset);
        Assert.Contains("leaving_costs_rent", result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_city_that_loses_its_employers_loses_Households_families_last()
    {
        Ruleset rules = Reluctant(1_200);
        (World closed, Simulation closing) = Run(rules, closeEmployers: true, out Cohort[] cohort);
        (World open, Simulation _) = Run(rules, closeEmployers: false, out Cohort[] control);
        (World eager, Simulation _) = Run(Reluctant(0), closeEmployers: true, out Cohort[] unreluctant);

        long left = closing.Placement.Drain().Left.Sum;
        int youngKept = Kept(closed, cohort, Young);
        int familyKept = Kept(closed, cohort, Family);
        int closedKept = Kept(closed, cohort, 0);
        int openKept = Kept(open, control, 0);
        int eagerFamilyKept = Kept(eager, unreluctant, Family);
        output.WriteLine(
            $"left {left}; kept young {youngKept}%, family {familyKept}% "
            + $"({eagerFamilyKept}% with no leaving cost); "
            + $"all {closedKept}% without employers, {openKept}% with them");

        Assert.True(left > 0);
        Assert.True(closedKept < openKept, $"kept {closedKept}% closed, {openKept}% open");
        Assert.True(familyKept > youngKept, $"families kept {familyKept}%, young {youngKept}%");
        Assert.True(familyKept > eagerFamilyKept, $"families kept {familyKept}%, {eagerFamilyKept}% eager");
        closing.CheckEndOfRun();
    }

    [Fact]
    public void Without_a_jobs_weight_nobody_leaves_for_work()
    {
        Ruleset rules = JobsAttractTests.Rules(
            Fixture().Replace("jobs_rent = 240", "jobs_rent = 0", StringComparison.Ordinal));
        (World _, Simulation simulation) = Run(rules, closeEmployers: true, out _);
        Assert.Equal(0, simulation.Placement.Drain().Left.Sum);
    }

    [Fact]
    public void Save_reload_while_Households_leave_continues_identically()
    {
        (World world, Simulation live) = Run(JobsAttractTests.Rules(), closeEmployers: true, out _);
        Ruleset rules = world.Rules;
        var file = new MemorySave();
        SaveFile.Write(world, 7, file);
        World reloaded = SaveFile.Read(file, rules, out _);
        var loaded = new Simulation(reloaded, Key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 8 };
        live.Placement.Drain();

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            live.Step(default);
            loaded.Step(default);
            Assert.Equal(world.HashState(), reloaded.HashState());
        }

        Assert.True(live.Placement.Drain().Left.Sum > 0);
    }

    private static (World, Simulation) Run(Ruleset rules, bool closeEmployers, out Cohort[] cohort)
    {
        World world = JobsAttractTests.City(rules: rules);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 1 };

        for (int tick = 0; tick < Ticks.PerDay; tick++) { simulation.Step(default); }

        if (closeEmployers)
        {
            for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
            {
                if (world.Businesses.Rows.IsLive(business))
                { world.DestroyBusiness(world.Businesses.Rows.At(business)); }
            }
        }

        cohort = Housed(world);
        for (int tick = 0; tick < 2 * Ticks.PerDay; tick++) { simulation.Step(default); }
        return (world, simulation);
    }

    private readonly record struct Cohort(Handle<Household> Household, byte Stage);

    private static Cohort[] Housed(World world)
    {
        var housed = new List<Cohort>();
        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]))
            { housed.Add(new Cohort(world.Households.Rows.At(slot), world.Households.LifeStage[slot])); }
        }
        return [.. housed];
    }

    /// <summary>The percent of a stage's cohort still in the city; stage zero means every stage.</summary>
    private static int Kept(World world, Cohort[] cohort, byte stage)
    {
        int whole = 0, kept = 0;
        foreach (Cohort member in cohort)
        {
            if (stage != 0 && member.Stage != stage) { continue; }
            whole++;
            if (world.Households.Rows.TryResolve(member.Household, out _)) { kept++; }
        }
        Assert.True(whole > 0, $"the cohort has no Households in stage {stage}");
        return kept * 100 / whole;
    }
}
