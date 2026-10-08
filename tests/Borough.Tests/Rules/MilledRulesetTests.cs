using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Rules;

public sealed class MilledRulesetTests
{
    private const byte Mill = 1;
    private const byte Grocer = 2;
    private const int Days = 14;

    [Fact]
    public void Mills_sell_flour_to_grocers_who_sell_to_households_and_every_trade_pays_its_staff()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "milled.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        long millRevenue = 0;
        long grocerRevenue = 0;
        long paid = 0;
        int bankrupt = 0;

        for (int day = 0; day < Days; day++)
        {
            long paidToday = 0;
            for (int t = 0; t < Ticks.PerDay; t++)
            {
                sim.Step(default);
                paidToday = Math.Max(paidToday, sim.LastPayroll.Paid);
                bankrupt += sim.LastPayroll.Bankrupted;
            }

            paid += paidToday;
            millRevenue += Revenue(world, Mill);
            grocerRevenue += Revenue(world, Grocer);
        }

        Assert.True(millRevenue > 0, "no grocer bought flour from a mill");
        Assert.True(grocerRevenue > 0, "no Household bought from a grocer");
        Assert.True(paid > 0, "no payroll was paid");
        Assert.True(Staffed(world, Mill) > 0, "no mill has staff");
        Assert.True(Staffed(world, Grocer) > 0, "no grocer has staff");
        Assert.Equal(0, bankrupt);
        sim.CheckEndOfRun();
    }

    private static long Revenue(World world, byte kind)
    {
        long total = 0;
        for (int b = 0; b < world.Businesses.Rows.SlotCount; b++)
        {
            if (world.Businesses.Rows.IsLive(b) && world.Businesses.Kind[b] == kind) { total += world.Businesses.DayRevenue[b]; }
        }

        return total;
    }

    private static int Staffed(World world, byte kind)
    {
        int staffed = 0;
        for (int b = 0; b < world.Businesses.Rows.SlotCount; b++)
        {
            if (world.Businesses.Rows.IsLive(b) && world.Businesses.Kind[b] == kind && world.Workers.Length(b) > 0) { staffed++; }
        }

        return staffed;
    }
}
