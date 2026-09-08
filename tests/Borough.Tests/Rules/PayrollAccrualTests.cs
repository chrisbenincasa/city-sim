using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Tests.Persistence;
using Borough.Formats;

namespace Borough.Tests.Rules;

public sealed class PayrollAccrualTests
{
    [Theory]
    [InlineData(12)]
    [InlineData(24)]
    public void Accrual_matches_individual_entitlement_through_a_week_and_employment_changes(int hours)
    {
        var (world, sim) = ShoppingTests.Start();
        for (int t = 0; t < 2048; t++) { sim.Step(default); }
        int[] workers = Enumerable.Range(0, world.Citizens.Rows.SlotCount)
            .Where(c => world.Citizens.Rows.IsLive(c) && !world.Citizens.Workplace[c].IsNone).ToArray();
        Assert.NotEmpty(workers);
        foreach (int c in workers)
        {
            world.Citizens.Activity[c] = (byte)CitizenActivity.AtWork;
            world.Citizens.EarnedWage[c] = 0;
            world.Citizens.WageRemainder[c] = 0;
        }
        var employer = world.Citizens.Workplace[workers[^1]];
        var premises = world.Businesses.Building[world.Businesses.Rows.Resolve(employer)];
        long accrued = 0;
        var expected = new (long Earned, long Remainder)[workers.Length];
        for (int t = 0; t < 7 * Ticks.PerDay; t++)
        {
            if (t == Ticks.PerDay)
            { world.Dismiss(world.Citizens.Rows.At(workers[0])); }
            if (t == 2 * Ticks.PerDay)
            {
                world.Employ(world.Citizens.Rows.At(workers[0]),
                    world.Citizens.Workplace[workers[^1]], Ticks.Zero);
            }
            if (t == 3 * Ticks.PerDay)
            {
                world.Unpremise(employer, world.Tick);
                world.RebuildDerived();
                Assert.Equal(0, world.Citizens.CommuteBucket[workers[^1]]);
            }
            if (t == 4 * Ticks.PerDay)
            {
                var save = new MemorySave();
                SaveFile.Write(world, 1, save);
                world = SaveFile.Read(save, world.Rules, out _);
                world.Premise(employer, premises);
                Assert.NotEqual(0, world.Citizens.CommuteBucket[workers[^1]]);
            }
            if (t == 5 * Ticks.PerDay)
            {
                string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"));
                var rules = RulesetLoader.Parse(text
                    .Replace("shift_start_earliest_hour = 6", "shift_start_earliest_hour = 23")
                    .Replace("shift_start_latest_hour   = 10", "shift_start_latest_hour   = 23")
                    .Replace("work_days = 31", "")
                    .Replace("shift_hours_min = 6", $"shift_hours_min = {hours}")
                    .Replace("shift_hours_max = 10", $"shift_hours_max = {hours}")
                    .Replace("4096", "2048"), "night-payroll.toml");
                Assert.True(rules.Ok, rules.Describe());
                world.Adopt(rules.Ruleset!, 2, world.Tick, world.Key);
            }
            if (t == 6 * Ticks.PerDay)
            { world.Citizens.Activity[workers[^1]] = (byte)CitizenActivity.AtHome; }
            Ticks tick = new((ulong)t);
            for (int i = 0; i < workers.Length; i++)
            {
                int c = workers[i];
                long earned = world.Citizens.EarnedWage[c], remainder = world.Citizens.WageRemainder[c];
                if ((CitizenActivity)world.Citizens.Activity[c] == CitizenActivity.AtWork
                    && !CivicEngine.TooIllToWork(world, c) && WorkSchedule.OnDuty(world, c, tick))
                {
                    int job = world.Businesses.Rows.Resolve(world.Citizens.Workplace[c]);
                    var trade = world.Rules.BusinessKind(world.Businesses.Kind[job]);
                    long length = (long)world.Rules.Jobs.ShiftLengthOf(world.Key, world.Citizens.Rows.IdAt(c)).Raw;
                    long scaled = remainder + trade.WagePerDay;
                    long whole = IntegerMath.FloorDiv(scaled, length);
                    earned = Math.Min(earned + whole, (long)trade.WagePerDay * trade.PayPeriodDays);
                    remainder = scaled % length;
                    accrued += whole;
                }
                expected[i] = (earned, remainder);
            }
            WorkSchedule.Accrue(world, tick);
            for (int i = 0; i < workers.Length; i++)
            {
                Assert.Equal(expected[i], (world.Citizens.EarnedWage[workers[i]], world.Citizens.WageRemainder[workers[i]]));
            }
        }
        Assert.True(accrued > 0);
    }
}
