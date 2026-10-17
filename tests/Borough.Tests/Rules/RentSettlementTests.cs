using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Rent arrears are settled where income LANDS rather than at the next Day boundary, and the arrears
/// clock stops where the debt does — <c>plans/money-circulation.md</c> L16.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="RentCollectionTests"/> covers the Day sweep; this covers the repayment path.</b> The
/// two are different mechanisms with different failure modes: a sweep that charges correctly can
/// still collect a debt a Day late, and a Day late is long enough for the tenant to spend the money,
/// for a Policy to levy it, or for the deadline to end a tenancy that had already paid up.
/// </para>
/// <para>
/// 🔴 <b>Income is delivered by a real engine and not by <c>World.Endow</c>.</b> A
/// <c>[[policy]]</c> paying from the treasury is the one income path a hand-built world can schedule
/// on an exact Tick, which is what these tests need — the claim is about WHEN a debt falls, so a
/// test that moved the money itself would prove nothing about the wiring. The waged city in
/// <see cref="RentPaydayTests"/> covers the wage and sale paths at city scale.
/// </para>
/// </remarks>
public sealed class RentSettlementTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x2026_10_18UL);

    private const int Rate = 10;
    private const int Share = 4;
    private const int Tenancies = 2;
    private const int Daily = Share * Rate;
    private const int Deadline = 3;

    /// <summary>
    /// Half past Day 1, and the interval the Policy below fires on. <b>Not a Day boundary</b>, which
    /// is the whole point: every assertion about mid-Day settlement is made at a Tick the rent sweep
    /// cannot have run on, because the sweep returns on its first line off the boundary.
    /// </summary>
    private const int MidDay = Ticks.PerDay + (Ticks.PerDay / 2);

    private static readonly ResourceId Cash = new(1);

    /// <summary>
    /// <paramref name="pays"/> money to each member of <paramref name="subject"/> every
    /// <see cref="MidDay"/> Ticks, out of the treasury.
    /// </summary>
    private static Ruleset Rules(PolicySubject subject, int pays, byte trade = 0) => new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)
        {
            Houses = true,
            Premises = trade != 0,
            Business = trade,
            RentPerFloorTile = new Money(Rate),
        }],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: [])
    {
        Placement = new PlacementRuleset(0, 0, 0, 20, 0),
        Capacity = new CapacityRuleset(Share, 0, 0),
        Rent = new RentRuleset(Deadline),
        BusinessKinds = trade == 0
            ? []
            : [new BusinessKindDefinition { ShiftStartEarliestHour = 8, ShiftStartLatestHour = 8 }],
        Policies = pays == 0
            ? []
            : [new PolicyDefinition(
                subject, MidDay, ApplyCount.Band(1, 1), Scope.Global, Scope.Local, Cash, pays)],
    };

    private static Handle<Lot> Ground(World world, Tiles east) => world.Lots.Create(
        east, Tiles.Zero, zone: 0, wide: new Tiles(Share * Tenancies), deep: new Tiles(1));

    private static Handle<Building> Raise(World world, Handle<Lot> lot) =>
        world.CreateBuilding(lot, 1, Ticks.Zero, Key);

    /// <summary>Steps until <paramref name="tick"/> has been processed.</summary>
    private static void RunPast(World world, Simulation simulation, long tick)
    {
        while (world.Tick.Raw <= (ulong)tick)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    private static long Level(World world, Handle<Bin> bin) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(bin));

    private static long Treasury(World world) =>
        world.Bins.LevelAt(world.FindTreasuryBin(Cash));

    /// <summary>
    /// 🔴 <b>F1(b): a Household's income pays its rent debt the moment it lands.</b>
    /// </summary>
    /// <remarks>
    /// The Policy pays at <see cref="MidDay"/>, which is neither a Day boundary nor a Tick the rent
    /// sweep runs on. ***The purse is empty again immediately***, so nothing else in the city could
    /// have been paid out of that money first — which is the half of L16 a Day-boundary collection
    /// cannot deliver.
    /// </remarks>
    [Fact]
    public void A_households_income_pays_its_rent_debt_where_it_lands()
    {
        Ruleset rules = Rules(PolicySubject.Household, pays: Daily);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        world.EndowTreasury(new Money(100 * Daily));

        Handle<Household> tenant = world.CreateHousehold(
            Raise(world, Ground(world, Tiles.Zero)), lifeStage: 0);

        int slot = world.Households.Rows.Resolve(tenant);

        // Day 1's boundary charged a Day's rent into an empty purse, so a Day is owed from Day 1.
        RunPast(world, simulation, Ticks.PerDay);

        Assert.Equal(Daily, world.Households.RentDebt[slot].Raw);
        Assert.Equal(1, world.Households.RentDebtSinceDay[slot]);

        RunPast(world, simulation, MidDay);

        // Mid-Day, and the next boundary is still a thousand Ticks away.
        Assert.NotEqual(0UL, world.Tick.Raw % (ulong)Ticks.PerDay);
        Assert.True(world.Tick.Raw < 2UL * (ulong)Ticks.PerDay);

        Assert.Equal(0, world.Households.RentDebt[slot].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[slot]);
        Assert.False(world.Households.IsBehindOnRent(slot));
        Assert.Equal(0, Level(world, world.Households.Balance[slot]));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>F1(a): a Business's income pays its rent debt where it lands, mid-Day.</b>
    /// </summary>
    /// <remarks>
    /// A shop sells over the counter at any Tick, so a till credited mid-Day has the rest of the Day
    /// to spend the money on stock, wages or an owner draw. The Policy stands in for the sale here
    /// because it can be scheduled on an exact Tick; the sale path itself is walked by the waged city
    /// in <see cref="RentPaydayTests"/>.
    /// </remarks>
    [Fact]
    public void A_businesss_income_pays_its_rent_debt_where_it_lands()
    {
        Ruleset rules = Rules(PolicySubject.Business, pays: Daily, trade: 1);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        world.EndowTreasury(new Money(100 * Daily));

        Handle<Household> resident = world.CreateHousehold(
            Raise(world, Ground(world, Tiles.Zero)), lifeStage: 0);

        world.Endow(resident, new Money(100 * Daily));

        int premises = world.Buildings.Rows.Resolve(
            world.Households.Dwelling[world.Households.Rows.Resolve(resident)]);

        int trade = Rows.NoSlot;

        foreach (int business in world.BuildingBusinesses.Walk(premises))
        {
            trade = business;
            break;
        }

        Assert.NotEqual(Rows.NoSlot, trade);
        Assert.Equal(Daily, world.BusinessRent(trade).Raw);

        RunPast(world, simulation, Ticks.PerDay);

        Assert.True(world.Businesses.IsBehindOnRent(trade));
        Assert.Equal(1, world.Businesses.RentDebtSinceDay[trade]);

        RunPast(world, simulation, MidDay);

        Assert.NotEqual(0UL, world.Tick.Raw % (ulong)Ticks.PerDay);

        // One Day was owed -- Day 0's charge was covered by the Policy firing on Tick 0, which runs
        // ahead of rent in phase 6 -- and the mid-Day payment cleared it where it landed.
        Assert.Equal(0, world.Businesses.RentDebt[trade].Raw);
        Assert.Equal(0, world.Businesses.RentDebtSinceDay[trade]);
        Assert.False(world.Businesses.IsBehindOnRent(trade));

        // The till is empty again, so nothing in the rest of the Day could have been bought with it.
        Assert.Equal(0, Level(world, world.Businesses.Balance[trade]));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>F1(c): repaying the old debt restarts the clock, so the old deadline cannot evict.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ***This is the case a single <c>rent + debt</c> total gets wrong.*** A tenant that settles
    /// every Day it was behind and then goes short on the NEXT Day's fresh charge is one Day behind,
    /// not four — but a sweep that adds the two together sees "still owing" and leaves the original
    /// Day standing, which evicts on a deadline the tenant has already paid off.
    /// </para>
    /// <para>
    /// The Policy pays a Day's rent mid-Day, so this tenant clears Day 1's shortfall during Day 1
    /// and then goes short again on Day 2's fresh charge. ***The clock must read Day 2.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Repaying_the_old_debt_restarts_the_arrears_clock()
    {
        Ruleset rules = Rules(PolicySubject.Household, pays: Daily);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        world.EndowTreasury(new Money(1_000 * Daily));

        Handle<Household> tenant = world.CreateHousehold(
            Raise(world, Ground(world, Tiles.Zero)), lifeStage: 0);

        int slot = world.Households.Rows.Resolve(tenant);

        // Day 1 went short, and the mid-Day payment settled it in full during Day 1.
        RunPast(world, simulation, MidDay);

        Assert.Equal(0, world.Households.RentDebt[slot].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[slot]);

        // Day 2's charge finds the purse empty again, so a NEW run of short Days starts on Day 2.
        RunPast(world, simulation, 2 * Ticks.PerDay);

        Assert.Equal(Daily, world.Households.RentDebt[slot].Raw);
        Assert.Equal(2, world.Households.RentDebtSinceDay[slot]);

        // 🔴 THE OLD DEADLINE PASSES AND NOTHING HAPPENS. Day 1 plus the deadline is Day 4, and a
        // clock left standing on Day 1 would end the tenancy here -- over Days this tenant paid for.
        RunPast(world, simulation, (1 + Deadline) * Ticks.PerDay);

        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]));
        Assert.Equal(0, world.UnplacedPool.Count);

        // Still genuinely in arrears rather than come out even, and inside the deadline throughout.
        Assert.True(world.Households.IsBehindOnRent(slot));

        int since = world.Households.RentDebtSinceDay[slot];
        long today = (long)world.Tick.Raw / Ticks.PerDay;

        Assert.InRange(today - since, 0, Deadline - 1);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>F2: a debt past <c>long.MaxValue</c> saturates and still evicts at the deadline.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loader puts no ceiling on <c>rent_per_floor_tile</c>, so a city can run up arrears that no
    /// <c>long</c> can hold. ***A wrapped total reads as nothing owed***, which before this fix
    /// skipped the payment AND the deadline for the rest of the city's life — a debt with no sink,
    /// which <c>adr/0130</c> forbids.
    /// </para>
    /// <para>
    /// <b>The column is seeded rather than accrued</b>, because reaching long's range by charging
    /// needs a daily figure so large that the rent arithmetic itself would be the thing under test.
    /// The property being fixed is about the CARRY, so the carry is what this sets up.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_debt_past_longs_range_saturates_and_still_evicts()
    {
        Ruleset rules = Rules(PolicySubject.Household, pays: 0);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        Handle<Household> tenant = world.CreateHousehold(
            Raise(world, Ground(world, Tiles.Zero)), lifeStage: 0);

        int slot = world.Households.Rows.Resolve(tenant);

        world.Households.RentDebt[slot] = new Money(long.MaxValue - 1);
        world.Households.RentDebtSinceDay[slot] = 0;

        // One Day short of the deadline: still housed, and the debt saturated instead of wrapping.
        RunPast(world, simulation, (Deadline - 1) * Ticks.PerDay);

        Assert.Equal(long.MaxValue, world.Households.RentDebt[slot].Raw);
        Assert.True(world.Households.RentDebt[slot].Raw > 0, "the debt wrapped negative.");
        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]));

        RunPast(world, simulation, Deadline * Ticks.PerDay);

        // The deadline reads the DEBT and not its size, so saturation caps the number without
        // capping the consequence.
        Assert.False(world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]));
        Assert.Equal(1, world.UnplacedPool.Count);
        Assert.Equal(0, world.Households.RentDebt[slot].Raw);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Landowner that owes rent settles out of the rent it receives, which is L16 without an
    /// exception for the one income that happens to be rent.
    /// </summary>
    /// <remarks>
    /// <b>The chain is bounded at one hop</b> (<c>RentAccounts.PassRentOn</c>), so this asserts the
    /// hop rather than a cascade: the tenant pays its Landowner, and the Landowner's own arrears are
    /// settled out of that payment within the same Tick.
    /// </remarks>
    [Fact]
    public void A_landowner_that_owes_rent_settles_out_of_the_rent_it_receives()
    {
        Ruleset rules = Rules(PolicySubject.Household, pays: 0);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        // The tenant's Lot, held by a Landowner who lives on a second Lot the city still holds -- so
        // the Landowner collects rent on one Lot and owes it on the other.
        Handle<Lot> rented = Ground(world, Tiles.Zero);
        Handle<Lot> owned = Ground(world, new Tiles(128));

        Handle<Household> tenant = world.CreateHousehold(Raise(world, rented), lifeStage: 0);
        Handle<Household> landlord = world.CreateHousehold(Raise(world, owned), lifeStage: 0);

        world.Lots.Landowner[world.Lots.Rows.Resolve(rented)] = landlord;

        int tenantSlot = world.Households.Rows.Resolve(tenant);
        int landlordSlot = world.Households.Rows.Resolve(landlord);

        // The tenant can always pay; the Landowner never can out of its own pocket.
        world.Endow(tenant, new Money(100 * Daily));

        Assert.Equal(Daily, world.ResidentRent(tenantSlot).Raw);
        Assert.Equal(Daily, world.ResidentRent(landlordSlot).Raw);

        RunPast(world, simulation, Deadline * Ticks.PerDay);

        // Every Day the tenant's rent arrived and went straight out again to the city, so the
        // Landowner is never in arrears and is never evicted despite owning nothing of its own.
        Assert.Equal(0, world.Households.RentDebt[landlordSlot].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[landlordSlot]);
        Assert.Equal(0, Level(world, world.Households.Balance[landlordSlot]));
        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[landlordSlot]));
        Assert.Equal(0, world.UnplacedPool.Count);

        // 🔴 ONE DAY'S RENT REACHES THE CITY PER DAY, not two, and that is the mechanism working.
        // The tenant's payment is not city income -- it is the Landowner's -- and the Landowner
        // hands the same money straight on to the city for the Lot it lives on. Four boundaries
        // passed, so the city collected four Days of the one tenancy it is Landowner of.
        Assert.Equal((Deadline + 1) * Daily, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Household holding its own Lot has a standing debt written off rather than paying itself.
    /// </summary>
    /// <remarks>
    /// <b>Unreachable on this branch and guarded anyway</b>, because slice 2 adds purchase: a tenant
    /// that buys the Lot under its own dwelling while behind on the rent would otherwise be both
    /// sides of the transfer. ***Money must not move onto one Bin*** — the withdrawal and the
    /// deposit would wake its wait list over a net of nothing — so the claim is written off, which is
    /// the answer eviction already gives it.
    /// </remarks>
    [Fact]
    public void A_household_holding_its_own_lot_does_not_pay_itself()
    {
        Ruleset rules = Rules(PolicySubject.Household, pays: 0);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> lot = Ground(world, Tiles.Zero);
        Handle<Household> owner = world.CreateHousehold(Raise(world, lot), lifeStage: 0);

        int slot = world.Households.Rows.Resolve(owner);

        world.Lots.Landowner[world.Lots.Rows.Resolve(lot)] = owner;
        world.Endow(owner, new Money(10 * Daily));

        // A debt carried in from before it held the Lot.
        world.Households.RentDebt[slot] = new Money(7 * Daily);
        world.Households.RentDebtSinceDay[slot] = 0;

        long before = Level(world, world.Households.Balance[slot]);

        RunPast(world, simulation, Deadline * Ticks.PerDay);

        // A resident Landowner is charged nothing (L7) and the standing debt was written off, so no
        // money moved at all and the deadline never ran out on it.
        Assert.Equal(0, world.Households.RentDebt[slot].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[slot]);
        Assert.Equal(before, Level(world, world.Households.Balance[slot]));
        Assert.Equal(0, Treasury(world));
        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]));

        simulation.CheckEndOfRun();
    }
}
