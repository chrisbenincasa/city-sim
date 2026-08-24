using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 10 task 4b: the second Occupant kind the build has never had (<c>adr/0113</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The table's whole content is a balance, so the tests are about where that balance can be and
/// what happens to it.</b> A Business is not modelled here — no inputs, no outputs, no employment,
/// no market — and asserting the absence of those would be asserting the schedule rather than the
/// build.
/// </para>
/// <para>
/// <b>The load-bearing pair is demolition.</b> A Household evicted from a demolished Building goes to
/// the Unplaced Pool with its money; a Business goes to the <b>unpremised pool</b> with its money.
/// Either way no money leaves the world, and <see cref="Invariant.MoneyIsConserved"/> is what says so.
/// ⚠ <b>This said *a Business has no pool, so the row survives holding a severed premises handle*
/// until milestone 25 task 5</b> (<c>adr/0142</c>). The severed handle is still true and it is no
/// longer the whole answer — ***the row now has somewhere to be, and a bound on how long it may be
/// there.*** <c>UnpremisedPoolTests</c> owns the pool and the sink; this class keeps the money.
/// </para>
/// </remarks>
public sealed class BusinessTests
{
    private static (World World, Handle<Building> Premises) Built()
    {
        // A Ruleset that names money, because since adr/0114 a balance is a Bin and a Bin exists only
        // for a declared Resource -- so on a moneyless file a Business has no balance to test.
        var world = new World(1_000, Moneyed());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);

        return (world, building);
    }

    /// <summary>
    /// Moves money between two actors' Bins. <b>The transfer, in the only spelling available.</b>
    /// </summary>
    /// <remarks>
    /// <b>Two halves of one movement, and neither touches the money supply</b>, which is what makes a
    /// transfer conserving by construction rather than by arithmetic that happens to cancel. Since
    /// <c>adr/0114</c> both halves are Bin writes, so both drain a wait list — a payee who was short
    /// wakes on the arrival, and a payer's own output waiters wake on the space. That is the property
    /// a pair of column writes did not have, and it is why the Rule engine can fail on money at all.
    /// </remarks>
    private static void Transfer(
        World world,
        Handle<Household> from,
        Handle<Business> to,
        long amount)
    {
        world.Withdraw(
            world.Households.Balance[world.Households.Rows.Resolve(from)], amount, world.Tick);

        world.Deposit(
            world.Businesses.Balance[world.Businesses.Rows.Resolve(to)], amount, world.Tick);
    }

    /// <summary>A shipped Ruleset, loaded as the runner loads it. All five name money.</summary>
    private static Ruleset Moneyed()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    [Fact]
    public void A_business_occupies_its_building_and_opens_with_nothing()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Business> business = world.CreateBusiness(premises);
        int slot = world.Businesses.Rows.Resolve(business);

        Assert.Equal(Money.Zero, world.BalanceOf(business));
        Assert.False(world.Businesses.Balance[slot].IsNone);
        Assert.Equal(
            premises,
            world.Businesses.Building[slot]);

        int[] listed = [.. world.BuildingBusinesses.Walk(world.Buildings.Rows.Resolve(premises))];
        Assert.Equal<int[]>([slot], listed);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// The occupant lists stay separate: a Household is in one and a Business in the other.
    /// </summary>
    /// <remarks>
    /// <b>This is the assertion <c>adr/0113</c>'s <em>homogeneous list</em> claim reduces to.</b> One
    /// polymorphic list holding both row types would make these two walks the same walk, and every
    /// reader of either would have to discriminate.
    /// </remarks>
    [Fact]
    public void A_household_and_a_business_occupy_one_building_through_two_lists()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Household> household = world.CreateHousehold(premises, lifeStage: 1);
        Handle<Business> business = world.CreateBusiness(premises);

        int buildingSlot = world.Buildings.Rows.Resolve(premises);

        Assert.Equal<int[]>(
            [world.Households.Rows.Resolve(household)],
            [.. world.Occupants.Walk(buildingSlot)]);

        Assert.Equal<int[]>(
            [world.Businesses.Rows.Resolve(business)],
            [.. world.BuildingBusinesses.Walk(buildingSlot)]);
    }

    /// <summary>
    /// A Business's balance is counted by the conservation walk, so money appearing in one is caught.
    /// </summary>
    /// <remarks>
    /// <b>The negative that proves the walk was added.</b> Before task 4b this write would have been
    /// invisible: the money is in a table the check did not visit, which reads as a world holding less
    /// than it was issued and would have been reported as money <em>destroyed</em> somewhere else.
    /// </remarks>
    [Fact]
    public void Money_written_straight_into_a_business_is_caught()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Business> business = world.CreateBusiness(premises);

        // Deposit and nothing else: an ordinary Bin write whose only fault is that the money supply
        // did not move with it. See MoneyConservationTests.Poke.
        world.Deposit(
            world.Businesses.Balance[world.Businesses.Rows.Resolve(business)],
            750,
            world.Tick);

        Violation caught = Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation;

        Assert.Equal(Invariant.MoneyIsConserved, caught.Invariant);
        Assert.Equal(750, caught.Other);
    }

    /// <summary>
    /// A Household paying a Business conserves money, and the check stays quiet.
    /// </summary>
    /// <remarks>
    /// <b>The transfer this milestone exists to make representable</b>, in the one spelling available
    /// before there is a price to pay: two column writes that sum to zero. It needs no door, because
    /// moving money between two actors creates none — which is why <see cref="World.Endow"/> is not
    /// widened to reach a Business.
    /// </remarks>
    [Fact]
    public void A_household_paying_a_business_conserves_money()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Household> household = world.CreateHousehold(premises, lifeStage: 1);
        world.Endow(household, new Money(1_000));

        Handle<Business> business = world.CreateBusiness(premises);

        Transfer(world, household, business, 400);

        Assert.Equal(new Money(600), world.BalanceOf(household));
        Assert.Equal(new Money(400), world.BalanceOf(business));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Demolition unlists a Business and keeps it, with its balance, holding a severed handle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The conservative half, and it is the half that did not change.</b> Freeing the row would
    /// take a balance out of the world through a demolition — the hole <c>adr/0024</c> exists to
    /// close — so the row survives with its money whatever else happens to it.
    /// </para>
    /// <para>
    /// ⚠ <b>This remark said *what becomes of a Business with no premises is undesigned*
    /// (<c>adr/0070</c>) until 2026-08-23</b>, and <c>adr/0142</c> answered it: the Business joins the
    /// unpremised pool and emigrates if nothing tenants it. ***The assertions below are unchanged and
    /// still pass***, which is the useful thing about having written the conservative half — the
    /// answer arrived without contradicting what had been checked. <c>UnpremisedPoolTests</c> asserts
    /// the pool membership; this test asserts what a demolition must never do.
    /// </para>
    /// </remarks>
    [Fact]
    public void Demolition_unlists_a_business_and_destroys_neither_it_nor_its_money()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Household> household = world.CreateHousehold(premises, lifeStage: 1);
        world.Endow(household, new Money(1_000));

        Handle<Business> business = world.CreateBusiness(premises);
        int slot = world.Businesses.Rows.Resolve(business);

        Transfer(world, household, business, 600);

        world.DestroyBuilding(premises, Ticks.Zero);

        Assert.True(world.Businesses.Rows.IsLive(slot));
        Assert.Equal(new Money(600), world.BalanceOf(business));
        Assert.False(world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out _));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// The maintained Business list is what a rebuild from saved state produces, demolition included.
    /// </summary>
    /// <remarks>
    /// <b>The <c>(derived AND rebuilt)</c> claim, checked rather than declared.</b> A derived list
    /// folds into no hash, so a write path that disagrees with <see cref="World.RebuildDerived"/> is
    /// invisible to replay, to the golden baseline and to save/reload alike. Demolition is in the
    /// fixture on purpose: it is the one path that unlinks, and draining rather than clearing is what
    /// makes the two agree — a cleared list leaves each row's <c>BuildingNext</c> pointing at its old
    /// sibling, which no rebuild would ever produce.
    /// </remarks>
    [Fact]
    public void The_maintained_business_list_is_what_a_rebuild_produces()
    {
        (World world, Handle<Building> kept) = Built();

        Handle<Lot> second = world.Lots.Create(new Tiles(9), new Tiles(9), zone: 1);
        Handle<Building> demolished = world.Buildings.Create(world.Lots, second, kind: 1);

        world.CreateBusiness(kept);
        world.CreateBusiness(demolished);
        world.CreateBusiness(kept);

        world.DestroyBuilding(demolished, Ticks.Zero);

        int keptSlot = world.Buildings.Rows.Resolve(kept);
        int[] maintained = [.. world.BuildingBusinesses.Walk(keptSlot)];

        world.RebuildDerived();

        Assert.Equal<int[]>(maintained, [.. world.BuildingBusinesses.Walk(keptSlot)]);
        Assert.Equal(2, maintained.Length);
    }
}
