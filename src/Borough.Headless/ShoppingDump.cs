using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Headless;

internal static class ShoppingDump
{
    internal static int Run(Options options, TextWriter output)
    {
        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out _) || !rules.Shopping.Runs)
        { output.WriteLine("--shopping needs a Ruleset declaring [shopping]."); return 2; }
        WorldKey key = WorldKey.FromSeed(options.Seed);
        var world = new World(options.Citizens, rules, key);
        var sim = new Simulation(world, key) { VerifyDecideWritesNothing = options.DecideGuard };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        long outings = 0, searches = 0, purchases = 0, bought = 0, delivered = 0, lost = 0;
        long attended = 0, paid = 0;
        output.WriteLine("day weekday outings route-searches purchases bought delivered cargo lost work-ticks wages");
        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            sim.Step(default);
            ShoppingReading r = sim.Shopping.Last;
            outings += r.Outings; searches += r.Searches; purchases += r.Purchases;
            bought += r.Bought; delivered += r.Delivered; lost += r.Lost;
            paid += sim.LastPayroll.Paid;
            for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
            {
                if (world.Citizens.Rows.IsLive(c)
                    && (CitizenActivity)world.Citizens.Activity[c] == CitizenActivity.AtWork
                    && Borough.Core.Movement.WorkSchedule.OnDuty(world, c, new Ticks(tick))) { attended++; }
            }
            if ((tick + 1) % Ticks.PerDay == 0 || tick + 1 == options.Ticks)
            {
                long cargo = 0;
                for (int row = 0; row < world.Shopping.Rows.SlotCount; row++)
                { if (world.Shopping.Rows.IsLive(row)) { cargo += world.Shopping.Cargo[row]; } }
                ulong day = tick / Ticks.PerDay;
                string weekday = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }[(int)(day % 7)];
                output.WriteLine($"{day} {weekday} {outings} {searches} {purchases} {bought} {delivered} {cargo} {lost} {attended} {paid}");
                outings = searches = purchases = bought = delivered = lost = attended = paid = 0;
            }
        }
        sim.CheckEndOfRun();
        return 0;
    }
}
