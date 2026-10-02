using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// The Employment feedback acceptance run: <c>pictured.toml</c>, 2,000 Citizens, seed 0, 40 Days.
/// On Day 10 three in four premised Businesses go bankrupt.
/// </summary>
public sealed class EmploymentRecoveryTests(ITestOutputHelper output)
{
    private const int ShockDay = 10;
    private const int RecoveryDays = 5;
    private const int LastDay = 40;
    private const int BarPercent = 75;

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Employment_recovers_from_a_mass_bankruptcy_and_holds()
    {
        (int before, int[] employed) = Run(Pictured());

        Assert.True(HoldsTheBar(before, employed), Describe("with the jobless signal", before, employed));
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Without_the_jobless_signal_the_same_world_does_not_hold_the_bar()
    {
        string toml = Pictured().Replace("jobless_threshold_days = 4\n", string.Empty, StringComparison.Ordinal);
        (int before, int[] employed) = Run(toml);

        Assert.False(HoldsTheBar(before, employed), Describe("without the jobless signal", before, employed));
    }

    private static string Pictured()
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "pictured.toml"));

        Assert.Contains("jobless_threshold_days = 4\n", toml, StringComparison.Ordinal);

        return toml;
    }

    private static bool HoldsTheBar(int before, int[] employed)
    {
        long bar = (long)before * BarPercent;

        for (int day = ShockDay + RecoveryDays; day <= LastDay; day++)
        {
            if ((long)employed[day] * 100 < bar)
            {
                return false;
            }
        }

        return true;
    }

    private string Describe(string run, int before, int[] employed)
    {
        string series = string.Join(", ", Enumerable.Range(ShockDay, LastDay - ShockDay + 1)
            .Select(day => $"{day}:{employed[day]}"));

        output.WriteLine($"{run}: {before} employed before the shock; by Day {series}");

        return $"{run}: {before} employed before the shock, {BarPercent}% bar from Day "
            + $"{ShockDay + RecoveryDays}; by Day {series}";
    }

    private static (int Before, int[] Employed) Run(string toml)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "pictured.toml");
        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0);
        var world = new World(2_000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        int before = 0;
        int[] employed = new int[LastDay + 1];

        for (int day = 1; day <= LastDay; day++)
        {
            for (int tick = 0; tick < Ticks.PerDay; tick++)
            {
                simulation.Step(TickInput.Empty);
            }

            if (day == ShockDay)
            {
                before = Employed(world);
                Assert.True(BankruptThreeInFour(world) > 0, "no premised Business stood to bankrupt.");
            }

            employed[day] = Employed(world);
        }

        simulation.CheckEndOfRun();

        return (before, employed);
    }

    /// <summary>Winds up three in four premised Businesses as <c>WageEngine</c> does.</summary>
    private static int BankruptThreeInFour(World world)
    {
        int failed = 0;
        int seen = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || !world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out _)
                || seen++ % 4 == 0)
            {
                continue;
            }

            if (world.Bins.Rows.TryResolve(world.Businesses.Balance[slot], out int till))
            {
                world.MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(world.Bins.LevelAt(till));
            }

            world.DestroyBusiness(world.Businesses.Rows.At(slot));
            failed++;
        }

        return failed;
    }

    private static int Employed(World world)
    {
        int employed = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Businesses.Rows.TryResolve(world.Citizens.Workplace[slot], out int business)
                && world.Buildings.Rows.TryResolve(world.Businesses.Building[business], out _))
            {
                employed++;
            }
        }

        return employed;
    }
}
