using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

/// <summary>
/// plans/0072 D15, D23 and D24 — a Business's revenue, its expenses and the Day's profit between
/// them, on a simplified accrual basis.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The claim under test is not that subtraction works.</b> It is that a sale is recognised
/// where the Goods are <em>delivered</em>, that stock carries its cost until it is sold rather than
/// being deducted where it was bought, that a wage is an expense of the Day it was <em>earned</em>
/// rather than of the payday it was paid on, and that ***none of this moves a single unit of
/// money***.
/// </para>
/// <para>
/// <b>The arithmetic is driven through <see cref="BusinessAccounts"/> directly and the behaviour
/// through a run</b>, which is the split the worked example forces: D15's illustration names four
/// exact figures and no shipped world produces them, so a run can show that the mechanism fires and
/// only a direct call can show that it fires with the right numbers.
/// </para>
/// </remarks>
public sealed class BusinessAccountsTests
{
    /// <summary>
    /// The Day the direct-arithmetic tests work on, chosen far past anything the fixture's own run
    /// reaches so that no figure below can be a leftover.
    /// </summary>
    private const int Base = 3_000;

    private static Ticks Day(int day) => new((ulong)((long)day * Ticks.PerDay));

    /// <summary>A live Business with a Goods Bin of its own, and that Bin.</summary>
    private static (int Business, int Bin) Trader(World world)
    {
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            for (Handle<Bin> at = world.Businesses.BinHead[slot];
                 world.Bins.Rows.TryResolve(at, out int bin);
                 at = world.Bins.OwnerNext[bin])
            {
                if (world.Bins.OwnerKind[bin] == BinOwnerKind.Business
                    && !world.Rules.IsConserved(world.Bins.Resource[bin]))
                {
                    return (slot, bin);
                }
            }
        }

        Assert.Fail("no Business in this world owns a Goods Bin, so there is no stock to account for");

        return (0, 0);
    }

    /// <summary>A trader whose books are empty, so every figure below is the test's own.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The city has to be stepped before a Business owns a Bin of its own</b>, so the fixture
    /// cannot simply assert that a freshly populated world is empty — it has to <em>empty</em> one.
    /// Both books are closed on <see cref="Base"/> and every test works on a later Day, so the first
    /// recognition each one makes rolls the accumulators and the figures asserted are its own.
    /// </para>
    /// <para>
    /// <b>The Bin is emptied through the same door a sale uses</b> — selling its whole contents
    /// takes the whole cost, which is the zero-residue property the tests below are about, used here
    /// rather than asserted.
    /// </para>
    /// </remarks>
    private static (World World, Simulation Sim, int Business, int Bin) Books()
    {
        var (world, sim) = ShoppingTests.Start();

        int stepped = 0;

        while (!HasTrader(world) && stepped++ < Ticks.PerDay)
        {
            sim.Step(default);
        }

        (int business, int bin) = Trader(world);

        BusinessAccounts.Deliver(world, business, bin, quantity: 1, level: 1, payment: 0, Day(Base));
        BusinessAccounts.Wages(world, business, 1, Day(Base));

        Assert.Equal(0, world.Bins.CostAt(bin));
        Assert.Equal(Base, world.Businesses.TradingDay[business]);

        return (world, sim, business, bin);
    }

    private static bool HasTrader(World world)
    {
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            for (Handle<Bin> at = world.Businesses.BinHead[slot];
                 world.Bins.Rows.TryResolve(at, out int bin);
                 at = world.Bins.OwnerNext[bin])
            {
                if (world.Bins.OwnerKind[bin] == BinOwnerKind.Business
                    && !world.Rules.IsConserved(world.Bins.Resource[bin]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // ---- the arithmetic ---------------------------------------------------------------------------

    /// <summary>
    /// A sale is revenue, and the share of what the stock cost is the expense against it.
    /// </summary>
    [Fact]
    public void A_sale_recognises_revenue_and_the_matching_share_of_what_the_stock_cost()
    {
        var (world, _, business, bin) = Books();

        // 10 units for 40, so a quarter of them cost 10.
        BusinessAccounts.Stock(world, bin, 40);
        BusinessAccounts.Deliver(world, business, bin, quantity: 4, level: 10, payment: 25, Day(Base + 3));

        Assert.Equal(25, world.Businesses.DayRevenue[business]);

        // FloorDiv(40 * 4, 10).
        Assert.Equal(16, world.Businesses.DayExpense[business]);
        Assert.Equal(9, world.Businesses.ProfitOn(business, BusinessAccounts.DayOf(Day(Base + 3))));

        // And the six units still standing keep what they cost.
        Assert.Equal(24, world.Bins.CostAt(bin));
    }

    /// <summary>
    /// 🔴 <b><c>plans/0072</c> D15's worked example, to the digit.</b> A shop buys 100 units for
    /// 100, sells 40 of them for 80 and incurs 20 in gross wages: profit is 80 − 40 − 20 = 20, and
    /// the remaining stock carries a cost of 60.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are asserted and the second is the one that matters.</b> Deducting the whole
    /// purchase where it was made would report a loss of 40 on this Day and overstate profit on
    /// every later sale — the same total over the two Days, in the wrong Days, which is exactly what
    /// a per-Day band (D23) charges differently.
    /// </remarks>
    [Fact]
    public void The_worked_example_from_D15_reports_a_profit_of_twenty_and_stock_worth_sixty()
    {
        var (world, _, business, bin) = Books();

        BusinessAccounts.Stock(world, bin, 100);
        BusinessAccounts.Deliver(world, business, bin, quantity: 40, level: 100, payment: 80, Day(Base + 5));
        BusinessAccounts.Wages(world, business, 20, Day(Base + 5));

        Assert.Equal(80, world.Businesses.DayRevenue[business]);
        Assert.Equal(60, world.Businesses.DayExpense[business]);
        Assert.Equal(20, BusinessAccounts.ProfitOn(world, business, Day(Base + 5)));
        Assert.Equal(60, world.Bins.CostAt(bin));
    }

    /// <summary>
    /// The accumulators reset at the Day boundary and the stock's cost does not.
    /// </summary>
    /// <remarks>
    /// ***That is the whole of D15's accrual in one assertion pair.*** The Day is a window on
    /// trading; the cost of unsold stock is a property of the stock and outlives every window.
    /// </remarks>
    [Fact]
    public void Unsold_stock_keeps_its_cost_across_a_day_boundary_and_the_day_starts_level()
    {
        var (world, _, business, bin) = Books();

        BusinessAccounts.Stock(world, bin, 100);
        BusinessAccounts.Deliver(world, business, bin, quantity: 40, level: 100, payment: 80, Day(Base + 5));
        BusinessAccounts.Wages(world, business, 20, Day(Base + 5));

        Assert.Equal(20, BusinessAccounts.ProfitOn(world, business, Day(Base + 5)));

        // Day 6 has traded nothing yet, and reading it says so without disturbing Day 5.
        Assert.Equal(0, BusinessAccounts.ProfitOn(world, business, Day(Base + 6)));
        Assert.Equal(20, BusinessAccounts.ProfitOn(world, business, Day(Base + 5)));

        // The first recognition of the new Day is what rolls the pair.
        BusinessAccounts.Wages(world, business, 20, Day(Base + 6));

        Assert.Equal(0, world.Businesses.DayRevenue[business]);
        Assert.Equal(20, world.Businesses.DayExpense[business]);
        Assert.Equal(-20, BusinessAccounts.ProfitOn(world, business, Day(Base + 6)));

        // And the 60 units nobody bought still cost what they cost.
        Assert.Equal(60, world.Bins.CostAt(bin));
    }

    /// <summary>
    /// Two purchases at different prices blend, and selling the lot leaves the Bin's cost at exactly
    /// zero.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The residue is the thing most likely to be wrong</b>, which is why the arithmetic here
    /// is chosen to round badly at every step: 11 spent on 10 units does not divide, and neither
    /// does any of the three draws against it. The final assertion is that ***nothing is left behind
    /// on an empty Bin***, because a residue would be cost belonging to units that no longer exist
    /// and the next purchase would average it into stock it was never paid for.
    /// </remarks>
    [Fact]
    public void Blended_stock_sells_down_to_exactly_zero_cost_with_no_residue()
    {
        var (world, _, business, bin) = Books();

        // 3 units for 10, then 7 units for 1: 10 units carrying 11.
        BusinessAccounts.Stock(world, bin, 10);
        BusinessAccounts.Stock(world, bin, 1);

        Assert.Equal(11, world.Bins.CostAt(bin));

        // FloorDiv(11 * 3, 10) = 3, leaving 8 on 7 units.
        BusinessAccounts.Deliver(world, business, bin, quantity: 3, level: 10, payment: 0, Day(Base + 1));
        Assert.Equal(3, world.Businesses.DayExpense[business]);
        Assert.Equal(8, world.Bins.CostAt(bin));

        // FloorDiv(8 * 4, 7) = 4, leaving 4 on 3 units.
        BusinessAccounts.Deliver(world, business, bin, quantity: 4, level: 7, payment: 0, Day(Base + 1));
        Assert.Equal(7, world.Businesses.DayExpense[business]);
        Assert.Equal(4, world.Bins.CostAt(bin));

        // The last three take everything that is left, and 3 + 4 + 4 is the 11 that was paid.
        BusinessAccounts.Deliver(world, business, bin, quantity: 3, level: 3, payment: 0, Day(Base + 1));
        Assert.Equal(11, world.Businesses.DayExpense[business]);
        Assert.Equal(0, world.Bins.CostAt(bin));
    }

    /// <summary>
    /// The same claim under the worst case for it: a thousand units sold one at a time.
    /// </summary>
    /// <remarks>
    /// <b>A thousand floors against a cost that does not divide by any of them.</b> What is asserted
    /// is conservation rather than any individual draw: every unit of cost that went in comes out as
    /// expense, and the Bin closes at zero.
    /// </remarks>
    [Fact]
    public void Selling_one_unit_at_a_time_expenses_the_whole_cost_and_nothing_more()
    {
        var (world, _, business, bin) = Books();

        BusinessAccounts.Stock(world, bin, 997);

        for (int left = 1000; left > 0; left--)
        {
            BusinessAccounts.Deliver(world, business, bin, quantity: 1, level: left, payment: 0, Day(Base + 2));
        }

        Assert.Equal(0, world.Bins.CostAt(bin));
        Assert.Equal(997, world.Businesses.DayExpense[business]);
    }

    /// <summary>
    /// A loss is representable: expense over revenue is a negative profit, not a zero and not a
    /// throw (<c>plans/0072</c> D26).
    /// </summary>
    [Fact]
    public void Expense_exceeding_revenue_gives_a_negative_profit()
    {
        var (world, _, business, bin) = Books();

        BusinessAccounts.Stock(world, bin, 500);
        BusinessAccounts.Deliver(world, business, bin, quantity: 50, level: 100, payment: 10, Day(Base + 9));
        BusinessAccounts.Wages(world, business, 400, Day(Base + 9));

        // 10 in, 250 of stock and 400 of wages out.
        Assert.Equal(10, world.Businesses.DayRevenue[business]);
        Assert.Equal(650, world.Businesses.DayExpense[business]);
        Assert.Equal(-640, BusinessAccounts.ProfitOn(world, business, Day(Base + 9)));
    }

    /// <summary>
    /// Buying stock is not an expense, and holding it over a Day boundary does not make it one.
    /// </summary>
    /// <remarks>
    /// ***This is the assertion that separates accrual from cash accounting***, and it is the half
    /// D15 says would otherwise "report a loss of 40 and overstate profit on later sales".
    /// </remarks>
    [Fact]
    public void Buying_stock_moves_no_profit_at_all()
    {
        var (world, _, business, bin) = Books();

        BusinessAccounts.Stock(world, bin, 100);

        // It landed on the stock and nowhere else: the accumulators were not even rolled, because
        // buying is not a recognition event at all.
        Assert.Equal(100, world.Bins.CostAt(bin));
        Assert.Equal(Base, world.Businesses.TradingDay[business]);
        Assert.Equal(0, BusinessAccounts.ProfitOn(world, business, Day(Base + 7)));

        // And on the Day it was bought, the purchase is still not an expense -- only the wage the
        // fixture posted is.
        Assert.Equal(-1, world.Businesses.ProfitOn(business, Base));
    }

    /// <summary>
    /// A service earns revenue with no cost of goods, because no Goods left.
    /// </summary>
    [Fact]
    public void A_service_is_revenue_with_no_cost_of_goods()
    {
        var (world, _, business, bin) = Books();

        BusinessAccounts.Stock(world, bin, 100);
        BusinessAccounts.Serve(world, business, 70, Day(Base + 4));

        Assert.Equal(70, world.Businesses.DayRevenue[business]);
        Assert.Equal(0, world.Businesses.DayExpense[business]);

        // The stock is untouched, which is the difference between a service and a free sale.
        Assert.Equal(100, world.Bins.CostAt(bin));
    }

    // ---- the behaviour, in a run -----------------------------------------------------------------

    /// <summary>
    /// 🔴 Wages are expensed on <b>every</b> Day and not only on paydays (<c>plans/0072</c> D15
    /// against D23).
    /// </summary>
    /// <remarks>
    /// <b>The employer under test is chosen for having a pay period longer than a Day</b>, so most
    /// of the Days counted here are Days on which no money moved at all — and every one of them
    /// still carries the wage bill of the work done on it. ***A payday-shaped expense would leave
    /// six Days in seven looking like pure profit.***
    /// </remarks>
    [Fact]
    public void Wages_reach_the_ledger_on_every_day_and_not_only_on_paydays()
    {
        var (world, sim) = ShoppingTests.Start();

        // Long enough to employ people and for the first pay period to have come and gone.
        for (int t = 0; t < 3 * Ticks.PerDay; t++)
        {
            sim.Step(default);
        }

        int employer = -1;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount && employer < 0; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Businesses.WorkerHead[slot] != 0
                && world.Rules.BusinessKind(world.Businesses.Kind[slot]).WagePerDay > 0
                && world.Rules.BusinessKind(world.Businesses.Kind[slot]).PayPeriodDays > 1)
            {
                employer = slot;
            }
        }

        Assert.True(employer >= 0, "no live Business employs anybody on a pay period longer than a Day");

        int period = world.Rules.BusinessKind(world.Businesses.Kind[employer]).PayPeriodDays;
        int expensed = 0;
        int days = 0;

        for (int day = 0; day < 8; day++)
        {
            // world.Tick is the Tick about to run, and this one is a Day boundary -- so this single
            // Step is the payroll sweep, and the figure read straight after it is the wage bill it
            // accrued for the Day now starting.
            sim.Step(default);

            days++;

            if (!world.Businesses.Rows.IsLive(employer))
            {
                break;
            }

            if (world.Businesses.DayExpense[employer] > 0)
            {
                expensed++;
            }

            for (int t = 1; t < Ticks.PerDay; t++)
            {
                sim.Step(default);
            }
        }

        Assert.Equal(days, expensed);

        Assert.True(
            days > period,
            $"the run covered {days} Days against a pay period of {period}, so every Day sampled "
            + "could have been a payday and the claim is untested. Lengthen the run.");
    }

    /// <summary>
    /// A real sale in a running city recognises revenue, without anybody reaching into the columns.
    /// </summary>
    [Fact]
    public void A_shop_selling_over_the_counter_accumulates_revenue()
    {
        var (world, sim) = ShoppingTests.Start();

        long most = 0;

        for (int t = 0; t < 4 * Ticks.PerDay; t++)
        {
            sim.Step(default);

            // Sampled rather than summed, because the accumulators roll: what is asserted is that
            // some trade held a positive Day's revenue at some point, not a total over the run.
            if (t % 16 != 0)
            {
                continue;
            }

            for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
            {
                if (world.Businesses.Rows.IsLive(slot) && world.Businesses.DayRevenue[slot] > most)
                {
                    most = world.Businesses.DayRevenue[slot];
                }
            }
        }

        Assert.True(
            most > 0,
            "no Business recognised any revenue over six Days of a world whose Households shop. "
            + "Either nothing was ever delivered or the sale sites are not posting.");
    }

    /// <summary>
    /// 🔴 <b>Accounting moves no money.</b> The supply does not move and the conservation invariant
    /// stays green over a run with every recognition site live.
    /// </summary>
    /// <remarks>
    /// <b><c>MoneySupply.Issued</c> is read at both ends rather than trusted to the invariant</b>,
    /// because the two say different things: the invariant compares the walk against the issue, so
    /// a bug that credited a till <em>and</em> issued the money for it would satisfy it. What must
    /// hold here is stronger — the issue itself is untouched, because nothing in this row is
    /// entitled to create or destroy a unit.
    /// </remarks>
    [Fact]
    public void Recognising_revenue_and_expense_moves_no_money_at_all()
    {
        var (world, sim) = ShoppingTests.Start();

        Money opening = world.MoneySupply.Issued[MoneySupplyTable.Slot];

        for (int t = 0; t < 4 * Ticks.PerDay; t++)
        {
            sim.Step(default);
        }

        Assert.Equal(opening, world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        sim.CheckEndOfRun();

        // And the claim stated directly rather than only through a run: every recognition method
        // fired at once, against a snapshot of every Bin in the world.
        (int business, int bin) = Trader(world);
        long[] levels = new long[world.Bins.Rows.SlotCount];

        for (int slot = 0; slot < levels.Length; slot++)
        {
            levels[slot] = world.Bins.Rows.IsLive(slot) ? world.Bins.LevelAt(slot) : 0;
        }

        BusinessAccounts.Stock(world, bin, 5_000);
        BusinessAccounts.Deliver(world, business, bin, quantity: 3, level: 9, payment: 900, world.Tick);
        BusinessAccounts.Serve(world, business, 40, world.Tick);
        BusinessAccounts.Wages(world, business, 60, world.Tick);

        Assert.Equal(opening, world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        for (int slot = 0; slot < levels.Length; slot++)
        {
            long now = world.Bins.Rows.IsLive(slot) ? world.Bins.LevelAt(slot) : 0;

            Assert.Equal(levels[slot], now);
        }
    }

    /// <summary>
    /// A save and a reload bring back the accumulators and every Bin's cost.
    /// </summary>
    /// <remarks>
    /// <b>The columns are asserted by value and not only by hash.</b> A hash comparison proves the
    /// pair round-trips together; it does not prove that either is the number the run left behind,
    /// which is what a reader of a Day's profit after a reload is relying on.
    /// </remarks>
    [Fact]
    public void A_save_and_reload_preserves_the_days_figures_and_every_stock_cost()
    {
        var (world, sim) = ShoppingTests.Start();

        for (int t = 0; t < 3 * Ticks.PerDay; t++)
        {
            sim.Step(default);
        }

        (int business, int bin) = Trader(world);

        BusinessAccounts.Stock(world, bin, 4_242);
        BusinessAccounts.Deliver(
            world, business, bin, quantity: 1, level: 8, payment: 777, world.Tick);
        BusinessAccounts.Wages(world, business, 111, world.Tick);

        ushort day = world.Businesses.TradingDay[business];
        long revenue = world.Businesses.DayRevenue[business];
        long expense = world.Businesses.DayExpense[business];
        long cost = world.Bins.CostAt(bin);
        ulong id = world.Businesses.Rows.IdAt(business);
        ulong binId = world.Bins.Rows.IdAt(bin);

        Assert.True(revenue > 0 && expense > 0 && cost > 0);

        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        World restored = SaveFile.Read(file, world.Rules, out _);

        Assert.Equal(world.HashState(), restored.HashState());

        // Slots are stable across a save, and the ids are asserted so that a reshuffle would say so
        // rather than silently comparing two different rows.
        Assert.Equal(id, restored.Businesses.Rows.IdAt(business));
        Assert.Equal(binId, restored.Bins.Rows.IdAt(bin));

        Assert.Equal(day, restored.Businesses.TradingDay[business]);
        Assert.Equal(revenue, restored.Businesses.DayRevenue[business]);
        Assert.Equal(expense, restored.Businesses.DayExpense[business]);
        Assert.Equal(cost, restored.Bins.CostAt(bin));
    }
}
