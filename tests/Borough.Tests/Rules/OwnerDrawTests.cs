using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// A founded Business pays its owner what its till holds above <c>pays_owner_above_pay_periods</c>
/// pay periods of wages.
/// </summary>
/// <remarks>
/// <c>insolvent.toml</c> pays its grocers weekly. Each test funds one grocer, lets the job search
/// staff it, and names as its only owner a Household with no wage, so its purse moves only by the
/// draw.
/// </remarks>
public sealed class OwnerDrawTests
{
    private const int Population = 2_000;
    private const int Reserve = 2;
    private const long Funding = 1_000_000;

    [Fact]
    public void A_profitable_business_pays_its_owner_and_keeps_its_reserve()
    {
        (World world, Simulation simulation, int grocer, int owner) = Owned(Reserve, owned: true);
        int till = world.Bins.Rows.Resolve(world.Businesses.Balance[grocer]);
        int purse = world.Bins.Rows.Resolve(world.Households.Balance[owner]);
        long drawn = 0;

        for (int tick = 0; tick < 8 * Ticks.PerDay && drawn <= 0; tick++)
        {
            long before = world.Bins.LevelAt(purse);

            simulation.Step(default);
            drawn = world.Bins.LevelAt(purse) - before;

            if (drawn > 0)
            {
                Assert.Equal(Reserve * PayPeriod(world, grocer) * Bill(world, grocer), world.Bins.LevelAt(till));
            }
        }

        Assert.True(drawn > 0, "no payday paid the owner.");
        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void A_business_short_of_its_reserve_pays_nothing()
    {
        (_, Simulation simulation, _, _) = Owned(reserve: 1_000, owned: true);

        Assert.Equal(0, DrawnOverAWeek(simulation));
    }

    [Fact]
    public void A_business_with_no_owner_pays_a_housed_household_it_draws()
    {
        (World world, Simulation simulation, int grocer, _) = Owned(Reserve, owned: false);

        Assert.True(DrawnOverAWeek(simulation) > 0, "no payday paid an owner.");
        Assert.True(
            world.Households.Rows.TryResolve(world.Businesses.Owner[grocer], out int owner),
            "the grocer drew no owner.");
        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[owner]));
        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void Without_the_key_no_business_pays_its_owner()
    {
        (_, Simulation simulation, _, _) = Owned(reserve: 0, owned: true);

        Assert.Equal(0, DrawnOverAWeek(simulation));
    }

    private static long DrawnOverAWeek(Simulation simulation)
    {
        long drawn = 0;

        for (int tick = 0; tick < 8 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
            drawn += simulation.LastPayroll.Drawn;
        }

        return drawn;
    }

    private static long PayPeriod(World world, int grocer) =>
        world.Rules.BusinessKind(world.Businesses.Kind[grocer]).PayPeriodDays;

    private static long Bill(World world, int grocer)
    {
        long wage = world.Rules.BusinessKind(world.Businesses.Kind[grocer]).WagePerDay;
        long bill = 0;

        foreach (int worker in world.Workers.Walk(grocer))
        {
            bill += WorkSchedule.Graded(world, worker, wage);
        }

        return bill;
    }

    /// <summary>
    /// A funded grocer after three Days of the job search, owned by a Household with no worker when
    /// <paramref name="owned"/> is set.
    /// </summary>
    private static (World, Simulation, int Grocer, int Owner) Owned(int reserve, bool owned)
    {
        (World world, Simulation simulation) = Populated(reserve);

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        int grocer = Grocer(world);

        world.Deposit(world.Businesses.Balance[grocer], Funding, simulation.Tick);
        world.MoneySupply.Issued[MoneySupplyTable.Slot] += new Money(Funding);

        for (int tick = 0; tick < 3 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        Assert.True(world.Workers.Walk(grocer).MoveNext(), "the job search staffed nobody.");

        int owner = Workless(world);

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            world.Businesses.Owner[slot] = default;
        }

        if (owned)
        {
            world.Businesses.Owner[grocer] = world.Households.Rows.At(owner);
        }

        // A payday during the job search can already have drawn the till down to its reserve.
        world.Deposit(world.Businesses.Balance[grocer], Funding, simulation.Tick);
        world.MoneySupply.Issued[MoneySupplyTable.Slot] += new Money(Funding);

        return (world, simulation, grocer, owner);
    }

    /// <summary>
    /// The synthetic city on <c>insolvent.toml</c>, its grocers paying a draw above
    /// <paramref name="reserve"/> pay periods when it is positive.
    /// </summary>
    private static (World, Simulation) Populated(int reserve)
    {
        string text = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "insolvent.toml"));

        if (reserve > 0)
        {
            text = text.Replace(
                "goes_bankrupt_after_days_in_arrears = 21\n",
                $"goes_bankrupt_after_days_in_arrears = 21\npays_owner_above_pay_periods = {reserve}\n",
                StringComparison.Ordinal);
        }

        RulesetLoadResult parsed = RulesetLoader.Parse(text, "insolvent.toml");

        Assert.True(parsed.Ok, parsed.Describe());
        Assert.Equal(reserve, parsed.Ruleset!.BusinessKind(2).PaysOwnerAbovePayPeriods);

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, parsed.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        return (world, simulation);
    }

    private static int Workless(World world)
    {
        for (int household = 0; household < world.Households.Rows.SlotCount; household++)
        {
            if (!world.Households.Rows.IsLive(household)
                || world.Households.Balance[household].IsNone)
            {
                continue;
            }

            bool working = false;

            foreach (int member in world.Members.Walk(household))
            {
                working |= !world.Citizens.Workplace[member].IsNone;
            }

            if (!working)
            {
                return household;
            }
        }

        Assert.Fail("every Household in the fixture has a worker.");
        return -1;
    }

    private static int Grocer(World world)
    {
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Businesses.Kind[slot] != 0
                && world.Rules.BusinessKind(world.Businesses.Kind[slot]).WagePerDay > 0
                && !world.Businesses.Balance[slot].IsNone)
            {
                return slot;
            }
        }

        Assert.Fail("the fixture has no grocer with a till.");
        return -1;
    }
}
