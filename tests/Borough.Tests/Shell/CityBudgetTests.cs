using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Tests.Shell;

/// <summary>
/// The arithmetic behind the budget panel: the Day roll, the running totals and the residual.
/// </summary>
/// <remarks>
/// <b>The panel itself is a <c>Control</c> and is invisible to this suite</b>, which is the whole
/// reason <c>CityBudget</c> is a separate Godot-free file. What can be wrong quietly is here — a Day
/// attributed to its neighbour, a running total counted from an assumed zero, or a residual that
/// reports nothing wrong because it was never computed against the balance.
/// </remarks>
public sealed class CityBudgetTests
{
    private static TreasuryFlows In(long withheld) => new(withheld, 0, 0, 0, 0, 0, 0);

    private static TreasuryFlows Out(long policy) => new(0, 0, 0, 0, policy, 0, 0);

    [Fact]
    public void A_day_closes_on_the_boundary_tick_and_carries_that_ticks_own_sweeps()
    {
        var budget = new CityBudget(new Ticks(0), 0);

        for (ulong tick = 1; tick < (ulong)Ticks.PerDay; tick++)
        {
            budget.Account(In(1), new Ticks(tick));
        }

        Assert.Equal(0, budget.Days);
        Assert.Equal(0, budget.Yesterday.Withheld);

        // The boundary Tick's own payday belongs to the Day it closes, which is where IncomeDump
        // puts it: the Census observes AFTER the step, so its row at a boundary includes that Tick.
        budget.Account(In(100), new Ticks(Ticks.PerDay));

        Assert.Equal(1, budget.Days);
        Assert.Equal((ulong)Ticks.PerDay, budget.ClosedAt);
        Assert.Equal(2_147, budget.Yesterday.Withheld);
        Assert.Equal(2_147, budget.Running.Withheld);
    }

    [Fact]
    public void A_new_day_does_not_inherit_the_closed_ones_figures()
    {
        var budget = new CityBudget(new Ticks(0), 0);

        budget.Account(In(500), new Ticks(Ticks.PerDay));
        budget.Account(In(7), new Ticks(Ticks.PerDay + 1));

        Assert.Equal(500, budget.Yesterday.Withheld);
        Assert.Equal(507, budget.Running.Withheld);

        budget.Account(In(3), new Ticks(Ticks.PerDay * 2));

        Assert.Equal(10, budget.Yesterday.Withheld);
        Assert.Equal(510, budget.Running.Withheld);
        Assert.Equal(2, budget.Days);
    }

    [Fact]
    public void The_opening_tick_is_not_a_day_boundary_however_it_divides()
    {
        var budget = new CityBudget(new Ticks(Ticks.PerDay * 3), 0);

        budget.Account(In(9), new Ticks(Ticks.PerDay * 3));

        Assert.Equal(0, budget.Days);
        Assert.Equal(9, budget.Running.Withheld);
        Assert.Equal(0, budget.Yesterday.Withheld);
    }

    [Fact]
    public void A_running_total_is_counted_from_the_opening_balance_and_not_from_zero()
    {
        var budget = new CityBudget(new Ticks(4_096), 1_000_000);

        budget.Account(In(400), new Ticks(4_097));
        budget.Account(Out(150), new Ticks(4_098));

        Assert.Equal(400, budget.Running.Income);
        Assert.Equal(150, budget.Running.Expenditure);
        Assert.Equal(0, budget.Residual(1_000_250));
    }

    [Fact]
    public void A_balance_the_flows_do_not_explain_shows_as_a_residual_in_both_directions()
    {
        var budget = new CityBudget(new Ticks(0), 100);

        budget.Account(In(50), new Ticks(1));

        Assert.Equal(0, budget.Residual(150));
        Assert.Equal(25, budget.Residual(175));
        Assert.Equal(-30, budget.Residual(120));
    }

    [Fact]
    public void Every_one_of_the_seven_columns_is_carried_apart()
    {
        var budget = new CityBudget(new Ticks(0), 0);
        var flows = new TreasuryFlows(1, 2, 4, 8, 16, 32, 64);

        budget.Account(flows, new Ticks(Ticks.PerDay));

        Assert.Equal(flows, budget.Yesterday);
        Assert.Equal(flows, budget.Running);
        Assert.Equal(1 + 2 + 4 + 8, budget.Running.Income);
        Assert.Equal(16 + 32 + 64, budget.Running.Expenditure);
    }

    /// <summary>
    /// The shell's account agrees with the instrument, run against run, on the demonstration world.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Two readers of one run would each see a movement once</b> — a drain is a drain — so this
    /// runs the Ruleset twice and holds the two readings against each other. ⚠ <b>The Census's rows
    /// and this account do NOT line up Day for Day</b>: <c>IncomeDump</c> observes at Tick 0 before
    /// stepping and this opens there without a reading, so the comparison is over the whole run.
    /// </remarks>
    [Fact]
    public void The_shells_account_totals_what_a_census_of_the_same_run_totals()
    {
        const int citizens = 500;
        const ulong ticks = 8_192;

        Ruleset rules = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "taxing.toml")).Ruleset!;

        TreasuryFlows accounted = Accounted(rules, citizens, ticks);
        TreasuryFlows observed = Observed(rules, citizens, ticks);

        Assert.Equal(observed, accounted);
        Assert.True(accounted.Income > 0, "the demonstration world must move money to be a test");
    }

    /// <summary>
    /// An undrained run holds every Tick of itself, so the shell's first reading would carry it.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>CityPreparation</c> steps every Tick up to <c>--start-at</c> on its own thread and
    /// nothing drains the engines while it does.</b> The accumulators therefore arrive holding the
    /// whole fast-forward, and a shell that started accounting without discarding it would credit
    /// its first Tick with every payday, levy and subsidy of however many Days it skipped — against
    /// an opening balance that already contains all of them. ***The residual would be the pre-roll,
    /// negative, on the first reading a player ever saw.*** <c>Main.OpenBudget</c>'s discarded drain
    /// is what stops it, and this is the measurement it rests on rather than the argument.
    /// </remarks>
    [Fact]
    public void An_undrained_run_accumulates_its_whole_history_into_one_reading()
    {
        const int citizens = 500;
        const ulong ticks = 4_096;

        Ruleset rules = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "taxing.toml")).Ruleset!;

        var key = WorldKey.FromSeed(1);
        var world = new World(citizens, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (ulong tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        TreasuryFlows held = simulation.DrainTreasuryFlows();

        Assert.True(
            held.Income > 0,
            "a run nobody drained must hold its history, or there is nothing to discard");

        // And the drain is exhaustive: asking twice does not hand the same movements over again.
        Assert.Equal(default, simulation.DrainTreasuryFlows());

        // What it held is what a per-Tick account of the same run would have totalled.
        Assert.Equal(held, Accounted(rules, citizens, ticks));
    }

    private static TreasuryFlows Accounted(Ruleset rules, int citizens, ulong ticks)
    {
        var key = WorldKey.FromSeed(1);
        var world = new World(citizens, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        var budget = new CityBudget(world.Tick, world.TreasuryBalance()?.Raw ?? 0);

        simulation.DrainTreasuryFlows();

        for (ulong tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
            budget.Account(simulation.DrainTreasuryFlows(), world.Tick);
        }

        Assert.Equal(0, budget.Residual(world.TreasuryBalance()?.Raw ?? 0));

        return budget.Running;
    }

    private static TreasuryFlows Observed(Ruleset rules, int citizens, ulong ticks)
    {
        var key = WorldKey.FromSeed(1);
        var world = new World(citizens, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        var census = new Census(world, (int)(ticks / (ulong)Ticks.PerDay) + 2);

        census.Observe(simulation);

        for (ulong tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);

            if (simulation.Tick.Raw % (ulong)Ticks.PerDay == 0)
            {
                census.Observe(simulation);
            }
        }

        census.Observe(simulation);

        var window = new Ticks(ticks);

        return new TreasuryFlows(
            Sum(census, MoneyFlowCounter.Withheld, window),
            Sum(census, MoneyFlowCounter.ProfitTax, window),
            Sum(census, MoneyFlowCounter.ToTreasury, window),
            Sum(census, MoneyFlowCounter.RuleToTreasury, window),
            Sum(census, MoneyFlowCounter.FromTreasury, window),
            Sum(census, MoneyFlowCounter.RuleFromTreasury, window),
            Sum(census, MoneyFlowCounter.Subsidy, window));
    }

    private static long Sum(Census census, MoneyFlowCounter counter, Ticks window)
    {
        long total = 0;

        foreach (CensusSample sample in
            census.Series(Metric.Of(counter, Aggregate.Sum), window).Samples.Span)
        {
            total += sample.Value;
        }

        return total;
    }
}
