using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

/// <summary>
/// <b>The profit tax, collected out of a Business's till at the Day boundary.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0072</c>, amnesty row 33 phase B. <see cref="BusinessTaxTests"/> owns the schedule
/// arithmetic on its own; these run the same claims through <c>BusinessTaxEngine.Sweep</c>, where the
/// money actually moves — <b>one till debit against one treasury credit of the same size</b>.
/// </para>
/// <para>
/// ⚠ <b>Most of them drive the sweep themselves rather than letting <see cref="Simulation.Step"/>
/// reach a Day boundary, on <c>WageWithholdingTests</c>' reasoning exactly.</b> A stepped boundary
/// Tick moves money for a dozen other reasons — the rates Rule fires, a Policy sweeps, a payday
/// lands — so a treasury that rose by the tax could not be told from a treasury that rose by the tax
/// and a rates bill. The fixture still <em>steps</em> to build a city with tills in it; what it does
/// not do is step across the boundary it is measuring.
/// </para>
/// <para>
/// 🔴 <b>The books of every other Business are closed on the same Day at zero before each test
/// begins</b>, so the sweep's own reading is about the one trade the test set up. A silent
/// contribution from some other grocer would make every exact figure here a coincidence.
/// </para>
/// <para>
/// ⚠ <b>The phase ordering is NOT tested here</b> — that is
/// <see cref="BusinessTaxPhaseOrderTests"/>, and it is the half that a driven sweep structurally
/// cannot see.
/// </para>
/// </remarks>
public sealed class BusinessTaxSweepTests
{
    /// <summary>
    /// The Day these work on, chosen far past anything the fixture's own warm-up reaches.
    /// </summary>
    /// <remarks>
    /// <c>BusinessAccountsTests.Base</c>'s trick and its reason: every figure asserted below is the
    /// test's own, because the first recognition it makes rolls the accumulators off whatever the
    /// warm-up left behind.
    /// </remarks>
    private const int Base = 3_000;

    /// <summary>How long the fixture steps before a Business owns a till.</summary>
    /// <remarks>
    /// <b>A populated world holds no shopfront</b> — the Zone Rules raise them over the first Days —
    /// so a fixture asserting against Tick 0 would be asserting against a city with no trade in it.
    /// </remarks>
    private const int WarmDays = 3;

    private static Ticks Day(int day) => new((ulong)((long)day * Ticks.PerDay));

    // ---- a profitable Day ------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>One till debit against one treasury credit of the same size, and the supply does not
    /// move.</b>
    /// </summary>
    /// <remarks>
    /// <b>A tax MOVES Money and never issues it</b> (<c>adr/0024</c>). <c>MoneySupply.Issued</c> is
    /// read at both ends rather than left to <c>Invariant.MoneyIsConserved</c>, because the two say
    /// different things: the invariant compares the walk against the issue, so a collection that
    /// credited the treasury <em>and</em> issued the money for it would satisfy it.
    /// </remarks>
    [Fact]
    public void A_profitable_business_pays_and_the_treasury_receives_exactly_that()
    {
        Books books = Start();

        // 400,000 of revenue against 100,000 of wages: a Day's profit of 300,000 exactly.
        Earn(books, revenue: 400_000, wages: 100_000);
        Fill(books, till: 5_000_000);

        long due = BusinessTax.DueOn(300_000, books.Schedule);

        Assert.True(due > 0, "the schedule took nothing, so this test asserts nothing.");

        long till = books.World.Bins.LevelAt(books.Till);
        long treasury = books.World.Bins.LevelAt(books.Treasury);
        Money issued = books.World.MoneySupply.Issued[MoneySupplyTable.Slot];

        ProfitTaxReading reading = books.Engine.Sweep(Day(Base + 1));

        Assert.Equal(due, reading.Due);
        Assert.Equal(due, reading.Collected);
        Assert.Equal(1, reading.Taxable);
        Assert.Equal(1, reading.Paying);
        Assert.Equal(0, reading.Underpaying);
        Assert.Equal(0, reading.Tilless);

        Assert.Equal(due, till - books.World.Bins.LevelAt(books.Till));
        Assert.Equal(due, books.World.Bins.LevelAt(books.Treasury) - treasury);

        // The debit and the credit are the same number, stated rather than inferred from the two
        // lines above, and nothing was issued to make them balance.
        Assert.Equal(
            till - books.World.Bins.LevelAt(books.Till),
            books.World.Bins.LevelAt(books.Treasury) - treasury);
        Assert.Equal(issued, books.World.MoneySupply.Issued[MoneySupplyTable.Slot]);

        books.Sim.CheckEndOfRun();
    }

    /// <summary>The collection is what the schedule says and not a flat share of the profit.</summary>
    /// <remarks>
    /// <b>Both bands, driven through the engine rather than through <c>BusinessTax.DueOn</c>.</b>
    /// A sweep that read a <em>balance</em> instead of a Day's profit, or that applied the upper rate
    /// to the whole figure, would satisfy every other test in this file and fail these two rows.
    /// </remarks>
    [Theory]
    // Inside the lower band: revenue less wages lands under the threshold.
    [InlineData(50_000L, 10_000L)]
    // Across it: the lower band is charged at the lower rate and only the excess at the upper.
    [InlineData(900_000L, 100_000L)]
    public void The_bill_is_the_schedule_read_against_the_days_profit(long revenue, long wages)
    {
        Books books = Start();

        Earn(books, revenue, wages);
        Fill(books, till: 10_000_000);

        long due = BusinessTax.DueOn(revenue - wages, books.Schedule);
        long treasury = books.World.Bins.LevelAt(books.Treasury);

        ProfitTaxReading reading = books.Engine.Sweep(Day(Base + 1));

        Assert.Equal(due, reading.Collected);
        Assert.Equal(due, books.World.Bins.LevelAt(books.Treasury) - treasury);
    }

    // ---- a loss ----------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 A loss is simply an untaxed Day: nothing is taken and nothing is carried forward.
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0072</c> D26, and the second half is the one worth having.</b> A schedule that ran
    /// its arithmetic over a negative profit would produce a negative bill — the treasury paying the
    /// trade — and a design that carried the loss would need a saved column and a sink for it. What
    /// this asserts is that the Day after a loss pays <em>in full</em>, which is the consequence of
    /// the daily cadence and the thing a carry-forward would change.
    /// </remarks>
    [Fact]
    public void A_loss_making_business_pays_nothing_and_carries_nothing_forward()
    {
        Books books = Start();

        // 40,000 of revenue against 100,000 of wages: a Day 60,000 in the red.
        Earn(books, revenue: 40_000, wages: 100_000);
        Fill(books, till: 5_000_000);

        long till = books.World.Bins.LevelAt(books.Till);
        long treasury = books.World.Bins.LevelAt(books.Treasury);

        ProfitTaxReading loss = books.Engine.Sweep(Day(Base + 1));

        Assert.Equal(default, loss);
        Assert.Equal(till, books.World.Bins.LevelAt(books.Till));
        Assert.Equal(treasury, books.World.Bins.LevelAt(books.Treasury));

        // The next Day is profitable and pays the whole of its own bill: the loss above bought it
        // nothing.
        Zero(books, Base + 1);
        Earn(books, revenue: 400_000, wages: 100_000, day: Base + 1);

        ProfitTaxReading profit = books.Engine.Sweep(Day(Base + 2));

        Assert.Equal(BusinessTax.DueOn(300_000, books.Schedule), profit.Collected);
    }

    /// <summary>A Day that broke exactly even owes nothing and is not counted as taxable.</summary>
    [Fact]
    public void A_day_that_broke_even_owes_nothing()
    {
        Books books = Start();

        Earn(books, revenue: 250_000, wages: 250_000);
        Fill(books, till: 5_000_000);

        Assert.Equal(default, books.Engine.Sweep(Day(Base + 1)));
    }

    // ---- a short till ----------------------------------------------------------------------------

    /// <summary>
    /// 🔴 A short till pays what it has, the rest is forgiven, and nothing goes negative.
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0072</c> D25, and the assertion that matters is the one about the NEXT Day.</b>
    /// Carrying the shortfall would need a saved arrears column and a sink for it, or it is a
    /// magnitude trending upward at steady state on a trade nobody can ever collect from — which
    /// <c>adr/0006</c> forbids outright. D5 settled the identical question on the Citizen side:
    /// unpaid wages generate no collectible tax debt.
    /// </remarks>
    [Fact]
    public void A_short_till_pays_what_it_has_and_goes_no_lower()
    {
        Books books = Start();

        Earn(books, revenue: 900_000, wages: 100_000);

        long due = BusinessTax.DueOn(800_000, books.Schedule);

        Assert.True(due > 2, "the bill is too small to be made short of.");

        // Deliberately short: the till holds a third of the bill and nothing else.
        long held = due / 3;

        Fill(books, till: held);

        long treasury = books.World.Bins.LevelAt(books.Treasury);
        Money issued = books.World.MoneySupply.Issued[MoneySupplyTable.Slot];

        ProfitTaxReading reading = books.Engine.Sweep(Day(Base + 1));

        Assert.Equal(due, reading.Due);
        Assert.Equal(held, reading.Collected);
        Assert.Equal(1, reading.Paying);
        Assert.Equal(1, reading.Underpaying);

        // No collection may push a till negative, and this one was emptied rather than overdrawn.
        Assert.Equal(0, books.World.Bins.LevelAt(books.Till));
        Assert.Equal(held, books.World.Bins.LevelAt(books.Treasury) - treasury);
        Assert.Equal(issued, books.World.MoneySupply.Issued[MoneySupplyTable.Slot]);

        // 🔴 FORGIVEN AND NOT CARRIED. The next Day is assessed on its own profit alone, so a trade
        // that could not pay yesterday is not chased for it today.
        Zero(books, Base + 1);
        Earn(books, revenue: 400_000, wages: 100_000, day: Base + 1);
        Fill(books, till: 5_000_000);

        Assert.Equal(
            BusinessTax.DueOn(300_000, books.Schedule),
            books.Engine.Sweep(Day(Base + 2)).Due);
    }

    /// <summary>An empty till pays nothing at all and is still reported as short.</summary>
    [Fact]
    public void An_empty_till_pays_nothing_and_is_still_reported()
    {
        Books books = Start();

        Earn(books, revenue: 400_000, wages: 100_000);
        Fill(books, till: 0);

        long treasury = books.World.Bins.LevelAt(books.Treasury);

        ProfitTaxReading reading = books.Engine.Sweep(Day(Base + 1));

        Assert.Equal(BusinessTax.DueOn(300_000, books.Schedule), reading.Due);
        Assert.Equal(0, reading.Collected);
        Assert.Equal(0, reading.Paying);
        Assert.Equal(1, reading.Underpaying);
        Assert.Equal(treasury, books.World.Bins.LevelAt(books.Treasury));
    }

    // ---- a Day is assessed once ------------------------------------------------------------------

    /// <summary>
    /// A Day whose figures still stand on the next boundary is not assessed a second time.
    /// </summary>
    /// <remarks>
    /// <b>The sweep reads <em>yesterday</em>, so a row that recognised nothing on the Day between is
    /// stamped with a Day the next sweep does not ask about.</b> That is what stops a trade with one
    /// quiet Day being taxed twice on the same profit — and it falls out of
    /// <c>BusinessTable.ProfitOn</c>'s Day comparison rather than out of an "assessed already" column,
    /// which is why there is no such column to test.
    /// </remarks>
    [Fact]
    public void A_days_profit_is_assessed_once_and_never_again()
    {
        Books books = Start();

        Earn(books, revenue: 400_000, wages: 100_000);
        Fill(books, till: 5_000_000);

        Assert.True(books.Engine.Sweep(Day(Base + 1)).Collected > 0);

        long till = books.World.Bins.LevelAt(books.Till);

        // Nothing happens on Day Base + 1, so the row still carries Day Base's figures.
        Assert.Equal(Base, books.World.Businesses.TradingDay[books.Business]);

        Assert.Equal(default, books.Engine.Sweep(Day(Base + 2)));
        Assert.Equal(till, books.World.Bins.LevelAt(books.Till));
    }

    /// <summary>A Tick that is not a Day's first collects nothing.</summary>
    [Fact]
    public void Nothing_is_collected_away_from_a_day_boundary()
    {
        Books books = Start();

        Earn(books, revenue: 400_000, wages: 100_000);
        Fill(books, till: 5_000_000);

        long till = books.World.Bins.LevelAt(books.Till);

        Assert.Equal(default, books.Engine.Sweep(new Ticks(Day(Base + 1).Raw + 1)));
        Assert.Equal(default, books.Engine.Sweep(new Ticks(Day(Base + 1).Raw - 1)));
        Assert.Equal(till, books.World.Bins.LevelAt(books.Till));
    }

    // ---- a world that levies nothing -------------------------------------------------------------

    /// <summary>
    /// 🔴 A world stating no <c>[business_tax]</c> is not written to at all, so its State Hash does
    /// not move.
    /// </summary>
    /// <remarks>
    /// <b><c>WageEngine.Pay</c>'s <c>bool levies</c> guard and its reason.</b> An untaxed city must
    /// not have its State Hash moved by a tax it does not have — ***the absence of a mechanism has to
    /// be indistinguishable from the build before the mechanism existed***, or every Ruleset in the
    /// tree re-records its hashes for a table none of them state.
    /// </remarks>
    [Fact]
    public void A_world_stating_no_business_tax_is_untouched()
    {
        Books books = Start(taxed: false);

        Assert.False(books.Schedule.Levies);

        Earn(books, revenue: 900_000, wages: 100_000);
        Fill(books, till: 5_000_000);

        ulong hash = books.World.HashState();
        long till = books.World.Bins.LevelAt(books.Till);
        long treasury = books.World.Bins.LevelAt(books.Treasury);

        Assert.Equal(default, books.Engine.Sweep(Day(Base + 1)));
        Assert.Equal(default, books.Engine.DrainCollected());

        Assert.Equal(hash, books.World.HashState());
        Assert.Equal(till, books.World.Bins.LevelAt(books.Till));
        Assert.Equal(treasury, books.World.Bins.LevelAt(books.Treasury));
    }

    // ---- the flow the Census drains --------------------------------------------------------------

    /// <summary>What the sweeps took accumulates between drains, and a drain resets it.</summary>
    /// <remarks>
    /// <b>A flow has no value at an instant</b> — <c>PolicyEngine</c>'s precedent, and the reason a
    /// Census sampling the last reading would keep one Day of however many its cadence covered.
    /// </remarks>
    [Fact]
    public void The_collection_accumulates_between_drains_and_a_drain_resets_it()
    {
        Books books = Start();

        Earn(books, revenue: 400_000, wages: 100_000);
        Fill(books, till: 5_000_000);

        long first = books.Engine.Sweep(Day(Base + 1)).Collected;

        Zero(books, Base + 1);
        Earn(books, revenue: 400_000, wages: 100_000, day: Base + 1);

        long second = books.Engine.Sweep(Day(Base + 2)).Collected;

        Assert.True(first > 0 && second > 0);

        MoneyFlow flow = books.Engine.DrainCollected();

        Assert.Equal(first + second, flow.Sum);
        Assert.Equal(first > second ? first : second, flow.Peak);

        // Drained, so a second reading of the same interval is empty rather than a repeat.
        Assert.Equal(default, books.Engine.DrainCollected());
    }

    // ---- a whole run -----------------------------------------------------------------------------

    /// <summary>
    /// Money is conserved across a run of the shipped world that levies the tax.
    /// </summary>
    /// <remarks>
    /// <b>The shipped file rather than the fixture</b>, because what is under test is that the
    /// collection is a <em>transfer</em> in the city as it actually runs: a walk over every Bin
    /// against the issue at the end, and the issue itself untouched from beginning to end.
    /// </remarks>
    [Fact]
    public void A_run_of_the_taxing_world_conserves_money_and_collects_something()
    {
        (World world, Simulation sim, _) = City(taxed: true);

        Money issued = world.MoneySupply.Issued[MoneySupplyTable.Slot];
        long collected = 0;

        for (int tick = 0; tick < 6 * Ticks.PerDay; tick++)
        {
            sim.Step(default);
            collected += sim.LastProfitTax.Collected;
        }

        Assert.True(collected > 0, "nothing was ever collected, so the run proves nothing.");
        Assert.Equal(issued, world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        sim.CheckEndOfRun();
    }

    /// <summary>A save taken mid-run resumes identically, Tick for Tick.</summary>
    /// <remarks>
    /// <b>The collection holds no state of its own and this is what says so.</b> Nothing is saved for
    /// it — the profit it reads is <c>BusinessTable</c>'s Day accumulators, which
    /// <c>BusinessAccountsTests</c> already round-trips — so a resumed run that drifted would mean
    /// the sweep had acquired a memory nobody declared.
    /// </remarks>
    [Fact]
    public void A_save_across_a_day_boundary_resumes_identically()
    {
        (World world, Simulation sim, _) = City(taxed: true);

        // Part-way into a Day, so the reload has a boundary ahead of it to collect on.
        for (int tick = 0; tick < (3 * Ticks.PerDay) + 500; tick++)
        {
            sim.Step(default);
        }

        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        World restored = SaveFile.Read(file, world.Rules, out SaveHeader header);
        var resumed = new Simulation(restored, header.Key);

        Assert.Equal(world.HashState(), restored.HashState());

        long collected = 0;

        for (int tick = 0; tick < 2 * Ticks.PerDay; tick++)
        {
            sim.Step(default);
            resumed.Step(default);

            collected += resumed.LastProfitTax.Collected;

            Assert.Equal(world.HashState(), restored.HashState());
        }

        Assert.True(collected > 0, "the resumed run crossed no collection, so nothing was compared.");
    }

    // ---- the fixture -----------------------------------------------------------------------------

    /// <summary>One trade, its till, the treasury, and a sweep to drive at them.</summary>
    private sealed record Books(
        World World,
        Simulation Sim,
        BusinessTaxEngine Engine,
        BusinessTaxSchedule Schedule,
        int Business,
        int Till,
        int Treasury);

    /// <summary>Posts a Day's revenue and wage bill on the subject trade, and nowhere else.</summary>
    private static void Earn(Books books, long revenue, long wages, int day = Base)
    {
        BusinessAccounts.Serve(books.World, books.Business, revenue, Day(day));
        BusinessAccounts.Wages(books.World, books.Business, wages, Day(day));

        Assert.Equal(revenue - wages, books.World.Businesses.ProfitOn(books.Business, (ushort)day));
    }

    /// <summary>
    /// Closes every live Business's books on <paramref name="day"/> at zero.
    /// </summary>
    /// <remarks>
    /// <b>So the sweep's reading is about the one trade the test set up.</b> The warm-up leaves real
    /// grocers holding real Day figures, and a contribution from one of them would make every exact
    /// figure in this file a coincidence that held until the placement order changed.
    /// </remarks>
    private static void Zero(Books books, int day)
    {
        for (int slot = 0; slot < books.World.Businesses.Rows.SlotCount; slot++)
        {
            if (books.World.Businesses.Rows.IsLive(slot))
            {
                BusinessAccounts.Serve(books.World, slot, 0, Day(day));
            }
        }
    }

    /// <summary>
    /// Moves the subject's till to exactly <paramref name="till"/>, conserving Money.
    /// </summary>
    /// <remarks>
    /// <b>Through the world's doors and a transfer rather than a write into the column</b>, so
    /// <c>Invariant.MoneyIsConserved</c> stays checkable across the setup as well as across the
    /// sweep — <c>WageWithholdingTests.SetTill</c>'s reason exactly.
    /// </remarks>
    private static void Fill(Books books, long till)
    {
        World world = books.World;
        long level = world.Bins.LevelAt(books.Till);

        if (level == till)
        {
            return;
        }

        if (level > till)
        {
            world.Withdraw(world.Bins.Rows.At(books.Till), level - till, world.Tick);
            world.Deposit(world.Bins.Rows.At(books.Treasury), level - till, world.Tick);

            return;
        }

        long missing = till - level;
        int household = Rows.NoSlot;

        for (int slot = 0; slot < world.Households.Rows.SlotCount && household < 0; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Bins.Rows.TryResolve(world.Households.Balance[slot], out _))
            {
                household = slot;
            }
        }

        Assert.True(household != Rows.NoSlot, "no Household in this world holds a purse.");

        world.Endow(world.Households.Rows.At(household), new Money(missing));
        world.Withdraw(world.Households.Balance[household], missing, world.Tick);
        world.Deposit(world.Bins.Rows.At(books.Till), missing, world.Tick);
    }

    /// <summary>A warmed city with one trade's books under the test's control.</summary>
    private static Books Start(bool taxed = true)
    {
        (World world, Simulation sim, WorldKey _) = City(taxed);

        for (int tick = 0; tick < WarmDays * Ticks.PerDay; tick++)
        {
            sim.Step(default);
        }

        int business = Rows.NoSlot;
        int till = Rows.NoSlot;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount && business < 0; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Bins.Rows.TryResolve(world.Businesses.Balance[slot], out int bin))
            {
                business = slot;
                till = bin;
            }
        }

        Assert.True(business != Rows.NoSlot, "no Business in this city holds a till.");

        int treasury = world.FindTreasuryBin(world.Bins.Resource[till]);

        Assert.NotEqual(Rows.NoSlot, treasury);

        var books = new Books(
            world,
            sim,
            new BusinessTaxEngine(world),
            world.Rules.BusinessTax,
            business,
            till,
            treasury);

        Zero(books, Base);

        return books;
    }

    /// <summary><c>rulesets/taxing.toml</c>, with or without the table under test.</summary>
    /// <remarks>
    /// <b>The shipped file rather than one authored inline</b>, because it is the only world that
    /// states <c>[business_tax]</c> and the one a reader will run. ⚠ <b>The untaxed variant is the
    /// same file with the table CUT</b>, so the two differ in nothing else — which is what makes
    /// <see cref="A_world_stating_no_business_tax_is_untouched"/> a statement about the table rather
    /// than about two different cities.
    /// </remarks>
    private static (World World, Simulation Sim, WorldKey Key) City(bool taxed)
    {
        // ⚠ WITHOUT THE CATALOGUE. The shipped file declares a 25% profit-tax relief on the grocer
        // (change 9), so every assertion below about what the BANDS charge would be an assertion
        // about the bands and a relief together -- and the file also declares a subsidy, which is a
        // second claimant on the treasury these tests never meant to have. ShippedTaxing says why
        // the strip is by `tool` rather than by name.
        string text = ShippedTaxing.WithoutCatalogue(ShippedTaxing.Text());

        if (!taxed)
        {
            // The LAST occurrence: the header names the table several times before declaring it.
            int at = text.LastIndexOf("[business_tax]", StringComparison.Ordinal);

            Assert.True(at > 0, "rulesets/taxing.toml no longer declares [business_tax].");

            text = text[..at];
        }

        RulesetLoadResult parsed = RulesetLoader.Parse(text, "taxing.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        var key = WorldKey.FromSeed(1);
        var world = new World(1_000, parsed.Ruleset!, key);
        var sim = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, sim, key);
    }
}
