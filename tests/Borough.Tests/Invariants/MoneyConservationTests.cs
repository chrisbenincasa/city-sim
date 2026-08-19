using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Invariants;

/// <summary>
/// Milestone 10 task 4: <c>adr/0031</c>'s conservation invariant, which the corpus specifies in three
/// documents and had built in none.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim is about doors, so the tests are about doors.</b> Every failure below is money that
/// moved without <see cref="World.Endow"/> moving — written straight into a column, or freed with the
/// row it was sitting on — and the one test that must <em>not</em> fire is a transfer, which moves two
/// balances and no supply. A check that could not tell those apart would be an equality on a constant
/// and nothing more.
/// </para>
/// <para>
/// <b>The exactness is a property of the schedule and it expires</b> (<c>plans/0033</c> F5). Money's
/// only source and sink is the Outside Connection, which is milestone <b>11</b>, so the supply is
/// fixed for the whole of milestone 10 and the assertions below are equalities. When the gate lands
/// they become an equality against a supply the gate has moved, and none of them changes shape.
/// </para>
/// </remarks>
public sealed class MoneyConservationTests
{
    /// <summary>One Building on one Lot, one Household in it. No money anywhere.</summary>
    /// <remarks>
    /// <b>It loads a Ruleset that names money, because since <c>adr/0114</c> a moneyless one is a
    /// world in which none of these tests can fail.</b> A balance is a Bin and a Bin exists only for a
    /// declared Resource, so on a moneyless file every sum below is zero on both sides and every
    /// negative case is unwritable. <c>TreasuryFromAFileTests</c> owns the moneyless world.
    /// </remarks>
    private static World Built(Ruleset? rules = null)
    {
        World world = new(1_000, rules ?? Moneyed());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);

        // Two, because an overflow test needs two places to put long.MaxValue and one Bin cannot hold
        // it twice -- Deposit refuses above the ceiling, and the ceiling is what makes the sum the
        // only thing that can run away.
        world.CreateHousehold(building, lifeStage: 1);
        world.CreateHousehold(building, lifeStage: 1);

        return world;
    }

    private static Handle<Household> First(World world) => world.Households.Rows.At(0);

    /// <summary>
    /// Puts money in a Household's balance <em>without</em> the door. <b>The defect, spelled.</b>
    /// </summary>
    /// <remarks>
    /// <b>It is <c>World.Deposit</c> and nothing else, which is the point.</b> Depositing into a Bin
    /// is an ordinary, correct, wait-list-draining write that every Rule will make; what makes it a
    /// defect here is only that <see cref="MoneySupplyTable.Issued"/> did not move with it. There is
    /// no illegal call to write — the failure this invariant catches is a <em>missing second half</em>,
    /// not a bad first one, and a test that reached past the API to produce it would be testing a
    /// state the build cannot reach.
    /// </remarks>
    private static void Poke(World world, Handle<Household> household, long amount) =>
        world.Deposit(
            world.Households.Balance[world.Households.Rows.Resolve(household)],
            amount,
            world.Tick);

    private static Violation CaughtAtEnd(World world) =>
        Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation;

    /// <summary>
    /// A world nobody has endowed holds nothing and owes nothing, and the check passes.
    /// </summary>
    /// <remarks>
    /// <b>Correct and temporarily trivial, which is 5b's distinction and the good side of it.</b> No
    /// production writer sets a Household's money — the milestone 10 survey found the only writers in
    /// the tree were test fixtures — so every world the simulation can build on its own is founded on
    /// zero, and both sides of the equality are zero with it. Nothing about the assertion's shape is
    /// wrong; it becomes load-bearing the day task 5 gives the balance sheet a writer, with no edit.
    /// </remarks>
    [Fact]
    public void A_world_nobody_has_endowed_holds_nothing_and_passes()
    {
        World world = Built();

        Assert.Equal(Money.Zero, world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Endowing moves the balance and the supply of record together.</summary>
    [Fact]
    public void An_endowment_moves_the_balance_and_the_supply_of_record_together()
    {
        World world = Built();

        world.Endow(First(world), new Money(1_000));

        Assert.Equal(new Money(1_000), world.BalanceOf(First(world)));
        Assert.Equal(new Money(1_000), world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Money written straight into a Household is caught, and the discrepancy says which way.
    /// </summary>
    /// <remarks>
    /// <b>This is the failure the invariant exists for and it is the one no other check can see.</b>
    /// <c>MoneyIsRepresentable</c> is content — a thousand pounds is a perfectly representable sum —
    /// and no per-Tick check fires, because the write is individually legal at the write site. What is
    /// wrong is a relation between two tables, which is what the whole-world tier is for.
    /// </remarks>
    [Fact]
    public void Money_written_straight_into_a_household_is_caught()
    {
        World world = Built();

        Poke(world, First(world), 1_000);

        Violation caught = CaughtAtEnd(world);

        Assert.Equal(Invariant.MoneyIsConserved, caught.Invariant);
        Assert.Equal(1_000, caught.Other);
    }

    /// <summary>
    /// Money freed with the Household it sat on is caught, and the discrepancy runs the other way.
    /// </summary>
    /// <remarks>
    /// <b><c>World.DestroyHousehold</c> has no production caller today, which is why nothing has ever
    /// burnt money.</b> That is an absence rather than a guarantee (<c>adr/0070</c>): the first caller
    /// is a design decision about what happens to a departing Household's balance, and this check is
    /// what makes the omission report itself instead of showing up as a money supply that quietly
    /// drains.
    /// </remarks>
    [Fact]
    public void Money_freed_with_its_household_is_caught()
    {
        World world = Built();

        Handle<Household> household = First(world);
        world.Endow(household, new Money(1_000));

        world.DestroyHousehold(household);

        Violation caught = CaughtAtEnd(world);

        Assert.Equal(Invariant.MoneyIsConserved, caught.Invariant);
        Assert.Equal(-1_000, caught.Other);
    }

    /// <summary>
    /// A Household paying the treasury conserves money, and the check stays quiet.
    /// </summary>
    /// <remarks>
    /// <b>The test that makes the other three mean something.</b> An invariant comparing a sum to a
    /// constant passes trivially in a world where nothing moves; what it has to do is pass in a world
    /// where money moves and fail in one where money appears. This is the shape of task 5's whole
    /// circuit — <c>local</c> money out, <c>global</c> money in — arriving early enough to prove the
    /// Bin side of the walk is real.
    /// </remarks>
    [Fact]
    public void A_household_paying_the_treasury_conserves_money()
    {
        World world = Built();

        int bin = Assert.Single<int>([.. world.TreasuryBins.Walk(TreasuryTable.Slot)]);

        world.Endow(First(world), new Money(1_000));

        world.Withdraw(world.Households.Balance[0], 400, world.Tick);
        world.Deposit(world.Bins.Rows.At(bin), 400, world.Tick);

        Assert.Equal(400, world.Bins.LevelAt(bin));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A sum that has run away reports as unrepresentable rather than as unconserved.
    /// </summary>
    /// <remarks>
    /// <b>An ordering claim, asserted rather than left to the registration list.</b> An overflowed sum
    /// is also unequal to its anchor, so both checks have something to say and only one of them names
    /// the bug. <c>RunEndOfRun</c> throws on the first violation and <c>MoneyIsRepresentable</c> is
    /// registered first; moving either registration silently swaps the diagnosis, which is what this
    /// test is here to stop.
    /// </remarks>
    [Fact]
    public void A_sum_that_has_run_away_reports_as_unrepresentable()
    {
        World world = Built();

        Poke(world, First(world), long.MaxValue);
        Poke(world, world.Households.Rows.At(1), long.MaxValue);

        Assert.Equal(Invariant.MoneyIsRepresentable, CaughtAtEnd(world).Invariant);
    }

    /// <summary>
    /// A negative endowment is refused, because money leaving the world is the gate's business.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0003</c> makes <c>Money</c> signed, so nothing else would notice.</b> A negative
    /// endowment is arithmetically consistent — the balance and the supply would both fall and the
    /// invariant would hold — which is exactly why the refusal is at the door: the check cannot tell a
    /// withdrawal made through the wrong door from a legitimate one, and there is no legitimate one
    /// until milestone 11.
    /// </remarks>
    [Fact]
    public void A_negative_endowment_is_refused()
    {
        World world = Built();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => world.Endow(First(world), new Money(-1)));
    }

    /// <summary>A shipped Ruleset, loaded as the runner loads it. All five name money.</summary>
    private static Ruleset Moneyed()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
