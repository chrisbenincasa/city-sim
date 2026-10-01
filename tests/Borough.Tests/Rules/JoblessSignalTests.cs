using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

public sealed class JoblessSignalTests
{
    [Fact]
    public void A_citizen_who_finds_every_post_full_waits_from_the_first_refusal()
    {
        (World world, Simulation simulation) = SchoolingTests.JobsCity(requiresTier: 0);

        simulation.Step(default);

        var firstSince = new Dictionary<int, ulong>();

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (IsWaiting(world, slot))
            {
                firstSince[slot] = world.Citizens.NoVacancySince[slot].Raw;
            }
        }

        Assert.NotEmpty(firstSince);

        simulation.Step(default);
        simulation.Step(default);

        int waiting = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (IsWaiting(world, slot) && firstSince.TryGetValue(slot, out ulong since))
            {
                Assert.Equal(since, world.Citizens.NoVacancySince[slot].Raw);
                waiting++;
            }
        }

        Assert.True(waiting > 0, "nobody who found every post full on the first Tick was still waiting.");
        Assert.True(simulation.Employment.Drain().NoVacancy.Sum > 0, "the census counted no NoVacancy conclusion.");
    }

    [Fact]
    public void Joblessness_alone_raises_a_trade_when_the_rule_reads_it()
    {
        Assert.Equal(0, ShopsRaised(jobless: false));
        Assert.True(ShopsRaised(jobless: true) > 0, "no shop was raised on the jobless signal.");
    }

    [Fact]
    public void A_vacant_unit_reopens_when_its_district_waits_for_work()
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "pictured.toml"));
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "pictured.toml");
        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0);
        var world = new World(2_000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        Step(simulation, Ticks.PerDay);

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out _))
            {
                if (world.Bins.Rows.TryResolve(world.Businesses.Balance[slot], out int till))
                {
                    world.MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(world.Bins.LevelAt(till));
                }

                world.DestroyBusiness(world.Businesses.Rows.At(slot));
            }
        }

        simulation.Zoning.Drain();
        Step(simulation, 2 * Ticks.PerDay);

        Assert.True(simulation.Zoning.Drain().Reopened.Sum > 0, "no vacant Unit reopened.");
        simulation.CheckEndOfRun();
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    private static bool IsWaiting(World world, int slot) =>
        world.Citizens.Rows.IsLive(slot)
        && world.Citizens.Employment[slot] == (byte)EmploymentState.NoVacancy;

    // Market demand is priced out of reach, so any shop raised here was raised on the jobless signal.
    private static int ShopsRaised(bool jobless)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "provisioned.toml"))
            .Replace(
                "build_threshold_days = 1\n",
                "build_threshold_days = 1000000\n" + (jobless ? "jobless_threshold_days = 1\n" : string.Empty),
                StringComparison.Ordinal);

        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "provisioned.toml");
        Assert.True(loaded.Ok, loaded.Describe());

        Ruleset rules = loaded.Ruleset!;
        var key = WorldKey.FromSeed(0x9A0FEDU);
        var world = new World(2_000, rules, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        byte shopfront = rules.ZoneRules.ToArray().Single(definition => definition.ReadsDemand).Kind;
        int before = Count(world, shopfront);

        for (int tick = 0; tick < 24_576; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        simulation.CheckEndOfRun();

        return Count(world, shopfront) - before;
    }

    private static int Count(World world, byte kind)
    {
        int count = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == kind)
            {
                count++;
            }
        }

        return count;
    }
}
