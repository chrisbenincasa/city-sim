using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// Where export income goes on <c>rulesets/exported.toml</c>, 400 Citizens, seed 0, 120 Days:
/// the shipped file, then with owner payouts, then with owner payouts and land sales.
/// </summary>
/// <remarks>
/// The variants are edited in memory, so the measurement adds no file to <c>rulesets/</c>.
/// <c>plans/export-base.md</c> records what it printed.
/// </remarks>
public sealed class ExportProfitTests(ITestOutputHelper output)
{
    private const int Days = 120;

    private const string Payouts = "pay_period_days = 7\npays_owner_above_pay_periods = 2\n";

    private const string LandSales =
        "[lots]\ndemolition_price_per_tile = 1024\nconstruction_charge_per_floor_tile = 256\n"
        + "buyer_reserve_per_member = 1024\n";

    private static string Edit(string toml, string anchor, string replacement, int expected)
    {
        int found = toml.Split(anchor).Length - 1;
        Assert.True(found == expected, $"Expected {expected} of '{anchor.TrimEnd()}' in exported.toml, found {found}.");
        return toml.Replace(anchor, replacement, StringComparison.Ordinal);
    }

    [Theory]
    [Trait(Tier.Key, Tier.Instrument)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Export_income_and_where_it_settles(bool payouts, bool landSales)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "exported.toml"))
            .ReplaceLineEndings("\n");

        if (payouts)
        {
            toml = Edit(toml, "pay_period_days = 7\n", Payouts, expected: 2);
        }

        if (landSales)
        {
            toml = Edit(toml, "[lots]\n", LandSales, expected: 1);
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "exported.toml");
        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        output.WriteLine($"payouts={payouts} landSales={landSales}");
        output.WriteLine("day firms households treasury issued exported drawn ownedLots buildings households#");

        long drawn = 0;

        for (int day = 1; day <= Days; day++)
        {
            for (int tick = 0; tick < Ticks.PerDay; tick++)
            {
                simulation.Step(TickInput.Empty);

                // Zero on every Tick but a payroll Tick.
                drawn += simulation.LastPayroll.Drawn;
            }

            if (day % 10 != 0 && day > 3)
            {
                continue;
            }

            MoneyLedger ledger = MoneyLedger.Of(world);
            long exported = world.Hinterlands.ExportedYesterday[HinterlandTable.SlotOf(MapEdge.North)].Raw
                + world.Hinterlands.ExportedYesterday[HinterlandTable.SlotOf(MapEdge.East)].Raw;
            int owned = 0;

            for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            {
                if (world.Lots.Rows.IsLive(lot) && !world.Lots.IsCityHeld(lot))
                {
                    owned++;
                }
            }

            output.WriteLine(
                $"{day} {ledger.Businesses} {ledger.Households} {ledger.Treasury} "
                + $"{world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw} {exported} {drawn} {owned} "
                + $"{world.Buildings.Rows.LiveCount} {world.Households.Rows.LiveCount}");
        }

        simulation.CheckEndOfRun();
    }
}
