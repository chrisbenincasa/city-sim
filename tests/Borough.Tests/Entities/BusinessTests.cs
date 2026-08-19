using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

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
/// the Unplaced Pool with its money; a Business has no pool, so the row survives holding a severed
/// premises handle. Either way no money leaves the world, and
/// <see cref="Invariant.MoneyIsConserved"/> is what says so.
/// </para>
/// </remarks>
public sealed class BusinessTests
{
    private static (World World, Handle<Building> Premises) Built()
    {
        var world = new World(1_000);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);

        return (world, building);
    }

    [Fact]
    public void A_business_occupies_its_building_and_opens_with_nothing()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Business> business = world.CreateBusiness(premises);
        int slot = world.Businesses.Rows.Resolve(business);

        Assert.Equal(Money.Zero, world.Businesses.Money[slot]);
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

        world.Businesses.Money[world.Businesses.Rows.Resolve(world.CreateBusiness(premises))]
            = new Money(750);

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
        world.Endow(household, new Money(1_000), Money.Zero);

        int payer = world.Households.Rows.Resolve(household);
        int payee = world.Businesses.Rows.Resolve(world.CreateBusiness(premises));

        world.Households.Money[payer] -= new Money(400);
        world.Businesses.Money[payee] += new Money(400);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Demolition unlists a Business and keeps it, with its balance, holding a severed handle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The worker branch's answer rather than the Household branch's, and the money is why.</b> A
    /// Household is evicted to the Unplaced Pool because there is somewhere for it to go. There is no
    /// pool for a Business, and freeing the row would take its balance out of the world through a
    /// demolition — the hole <c>adr/0024</c> exists to close.
    /// </para>
    /// <para>
    /// ⚠ <b>What becomes of a Business with no premises is undesigned</b> (<c>adr/0070</c>), and this
    /// asserts the conservative half only: nothing is destroyed and nothing is left on a list. The
    /// design question is filed in <c>plans/0002</c> §C beside the departing Household's balance,
    /// which is the same question with a different subject.
    /// </para>
    /// </remarks>
    [Fact]
    public void Demolition_unlists_a_business_and_destroys_neither_it_nor_its_money()
    {
        (World world, Handle<Building> premises) = Built();

        Handle<Household> household = world.CreateHousehold(premises, lifeStage: 1);
        world.Endow(household, new Money(1_000), Money.Zero);

        int slot = world.Businesses.Rows.Resolve(world.CreateBusiness(premises));

        world.Households.Money[world.Households.Rows.Resolve(household)] -= new Money(600);
        world.Businesses.Money[slot] += new Money(600);

        world.DestroyBuilding(premises, Ticks.Zero);

        Assert.True(world.Businesses.Rows.IsLive(slot));
        Assert.Equal(new Money(600), world.Businesses.Money[slot]);
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
