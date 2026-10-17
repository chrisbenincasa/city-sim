using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Tests.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// The daily rent charge, the debt a short tenant carries and eviction at the deadline —
/// <c>plans/money-circulation.md</c> L7's charge, L8, L16 and L17.
/// </summary>
/// <remarks>
/// <para>
/// <b>Rent was a price nothing collected.</b> Slice 3 made it one figure per tenancy and said so in
/// <c>World.TenancyRent</c>'s own remark; this is the slice that charges it. Two properties make the
/// mechanism safe to run in a city, and every test here holds one of them: ***money only ever moves
/// between two holders***, and ***the debt is bounded by the deadline***.
/// </para>
/// <para>
/// <b>Hand-built worlds rather than a shipped Ruleset</b>, on <see cref="TenancyRentTests"/>'
/// precedent, so each test states the rate, the floor and the balances it reasons about. The loader's
/// half is <see cref="RentRefusalTests"/>, and the one case a hand-built world cannot reach — a
/// tenant living off a wage paid every seven Days — is <see cref="RentPaydayTests"/>, because it
/// needs an employer with a pay period and staff.
/// </para>
/// </remarks>
public sealed class RentCollectionTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x2026_10_17UL);

    /// <summary>Money per floor Tile per Day.</summary>
    private const int Rate = 10;

    /// <summary>Floor Tiles one tenancy holds, which is also the capacity rate.</summary>
    private const int Share = 4;

    /// <summary>Tenancies in every dwelling raised here, so a neighbour fits beside the first.</summary>
    private const int Tenancies = 2;

    /// <summary>What one tenancy pays a Day on ground worth nothing.</summary>
    private const int Daily = Share * Rate;

    /// <summary>Days of arrears the fixtures allow before the tenancy ends.</summary>
    private const int Deadline = 3;

    private static readonly ResourceId Cash = new(1);

    private static Ruleset Rules(int deadline = Deadline, byte trade = 0) => new(
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
        // gives_up_after_days is stated because the loader refuses rent without it. Nothing here runs
        // long enough for the Pool's own bound to bind.
        Placement = new PlacementRuleset(0, 0, 0, 20, 0),
        Capacity = new CapacityRuleset(Share, 0, 0),
        Rent = new RentRuleset(deadline),
        BusinessKinds = trade == 0
            ? []
            : [new BusinessKindDefinition { ShiftStartEarliestHour = 8, ShiftStartLatestHour = 8 }],
    };

    /// <summary>
    /// A dwelling of <see cref="Tenancies"/> tenancies on city-held ground, with one Household in it
    /// holding <paramref name="endowment"/>.
    /// </summary>
    private static (World World, Simulation Simulation, int Lot, int Tenant) Tenancy(
        long endowment, int deadline = Deadline, byte trade = 0)
    {
        var world = new World(1_000, Rules(deadline, trade), Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> lot = Ground(world, Tiles.Zero);
        Handle<Household> tenant = world.CreateHousehold(Raise(world, lot), lifeStage: 0);

        if (endowment > 0)
        {
            world.Endow(tenant, new Money(endowment));
        }

        return (world, simulation, world.Lots.Rows.Resolve(lot), world.Households.Rows.Resolve(tenant));
    }

    private static Handle<Lot> Ground(World world, Tiles east) => world.Lots.Create(
        east, Tiles.Zero, zone: 0, wide: new Tiles(Share * Tenancies), deep: new Tiles(1));

    private static Handle<Building> Raise(World world, Handle<Lot> lot) =>
        world.CreateBuilding(lot, 1, Ticks.Zero, Key);

    /// <summary>Steps until the world's clock reaches <paramref name="day"/>, charging on the way.</summary>
    private static void RunToDay(World world, Simulation simulation, int day)
    {
        while (world.Tick.Raw < (ulong)day * (ulong)Ticks.PerDay)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    private static long Treasury(World world) =>
        world.Bins.LevelAt(world.FindTreasuryBin(Cash));

    private static long Purse(World world, int household) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Households.Balance[household]));

    /// <summary>
    /// A Household holding <paramref name="lot"/> and living on its own second Lot, so it collects
    /// rent and — as a resident Landowner — pays none.
    /// </summary>
    private static int Landlord(World world, int lot)
    {
        Handle<Lot> own = Ground(world, new Tiles(128));
        Handle<Household> owner = world.CreateHousehold(Raise(world, own), lifeStage: 0);

        world.Lots.Landowner[world.Lots.Rows.Resolve(own)] = owner;
        world.Lots.Landowner[lot] = owner;

        return world.Households.Rows.Resolve(owner);
    }

    /// <summary>
    /// A city-held Lot's rent is treasury income, charged once a Day out of the tenant's purse.
    /// </summary>
    [Fact]
    public void A_city_held_lots_rent_goes_to_the_treasury()
    {
        (World world, Simulation simulation, int lot, int tenant) = Tenancy(endowment: 10 * Daily);

        Assert.True(world.Lots.IsCityHeld(lot));
        Assert.Equal(Daily, world.ResidentRent(tenant).Raw);

        // Tick zero is a Day boundary, so three boundaries have passed by the start of Day 3.
        RunToDay(world, simulation, 3);

        Assert.Equal(3 * Daily, Treasury(world));
        Assert.Equal((10 - 3) * Daily, Purse(world, tenant));
        Assert.False(world.Households.IsBehindOnRent(tenant));

        simulation.CheckEndOfRun();
    }

    /// <summary>A Landowner receives its tenant's rent, and the treasury receives nothing.</summary>
    [Fact]
    public void A_landowner_receives_the_rent_and_the_treasury_does_not()
    {
        (World world, Simulation simulation, int lot, int tenant) = Tenancy(endowment: 10 * Daily);
        int landlord = Landlord(world, lot);

        RunToDay(world, simulation, 2);

        Assert.Equal(2 * Daily, Purse(world, landlord));
        Assert.Equal((10 - 2) * Daily, Purse(world, tenant));
        Assert.Equal(0, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A resident Landowner pays nothing for its own tenancy and its neighbour still pays it, which
    /// is <c>L7</c>'s exemption being per tenancy rather than per Building.
    /// </summary>
    [Fact]
    public void A_resident_landowner_pays_none_and_its_neighbour_pays_it()
    {
        (World world, Simulation simulation, int lot, int owner) = Tenancy(endowment: 10 * Daily);

        Handle<Household> next = world.CreateHousehold(world.Households.Dwelling[owner], lifeStage: 0);
        int neighbour = world.Households.Rows.Resolve(next);

        world.Endow(next, new Money(10 * Daily));
        world.Lots.Landowner[lot] = world.Households.Rows.At(owner);

        Assert.Equal(0, world.ResidentRent(owner).Raw);
        Assert.Equal(Daily, world.ResidentRent(neighbour).Raw);

        RunToDay(world, simulation, 2);

        // The owner paid nothing and collected twice; the neighbour paid twice.
        Assert.Equal((10 + 2) * Daily, Purse(world, owner));
        Assert.Equal((10 - 2) * Daily, Purse(world, neighbour));
        Assert.Equal(0, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A tenant short of its rent pays what it holds, owes the rest and saves the Day the run of
    /// shortfalls began.
    /// </summary>
    [Fact]
    public void A_short_tenant_pays_what_it_holds_and_owes_the_rest()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(endowment: Daily + 1);

        RunToDay(world, simulation, 2);

        // Day 0 took the whole Day's rent; Day 1 took the single unit left and owed the remainder.
        Assert.Equal(0, Purse(world, tenant));
        Assert.Equal(Daily - 1, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(1, world.Households.RentDebtSinceDay[tenant]);
        Assert.Equal(Daily + 1, Treasury(world));

        // Day 2 adds a whole Day to the debt and leaves the Day it began standing.
        RunToDay(world, simulation, 3);

        Assert.Equal((2 * Daily) - 1, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(1, world.Households.RentDebtSinceDay[tenant]);

        simulation.CheckEndOfRun();
    }

    /// <summary>Income settles the debt before the Day's own charge, and clearing it clears the Day.</summary>
    [Fact]
    public void Later_income_pays_the_debt_first_and_clearing_it_clears_the_day()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(endowment: 0);

        RunToDay(world, simulation, 2);

        Assert.Equal(2 * Daily, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[tenant]);

        // Enough for the two Days owed and the Day the next boundary charges.
        world.Endow(world.Households.Rows.At(tenant), new Money(3 * Daily));

        RunToDay(world, simulation, 3);

        Assert.Equal(0, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[tenant]);
        Assert.Equal(0, Purse(world, tenant));
        Assert.Equal(3 * Daily, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Past the deadline the Household is evicted to the Unplaced Pool, and the debt ends with the
    /// tenancy — which is what bounds it.
    /// </summary>
    [Fact]
    public void A_household_behind_past_the_deadline_is_evicted_and_owes_nothing_after()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(endowment: 0);

        // Day 0 starts the run and Days 1 and 2 extend it, so the deadline falls on Day 3.
        RunToDay(world, simulation, Deadline);

        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[tenant]));
        Assert.Equal(Deadline * Daily, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, world.UnplacedPool.Count);

        RunToDay(world, simulation, Deadline + 1);

        Assert.True(world.Households.IsUnplaced(tenant));
        Assert.Equal(1, world.UnplacedPool.Count);
        Assert.Equal(world.Households.Rows.At(tenant), world.UnplacedPool.At(0));

        // The cap. The tenancy ended, so the arrears ended with it rather than following the
        // Household into the Pool and on to its next home.
        Assert.Equal(0, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[tenant]);
        Assert.False(world.Households.IsBehindOnRent(tenant));

        // Nothing is charged after the eviction, and the treasury never received a unit: the debt was
        // never money.
        RunToDay(world, simulation, Deadline + 4);

        Assert.Equal(0, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>A tenant whose income arrives before the deadline is not evicted.</summary>
    /// <remarks>
    /// <b>The same world as the eviction above, differing only in a deposit</b>, so the pair holds
    /// that the deadline tests the arrears and not the Day.
    /// </remarks>
    [Fact]
    public void A_tenant_paid_in_time_is_not_evicted()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(endowment: 0);

        RunToDay(world, simulation, Deadline);

        Assert.True(world.Households.IsBehindOnRent(tenant));

        // Settles everything owed and the charge for the Day the deadline would have fallen on.
        world.Endow(world.Households.Rows.At(tenant), new Money((Deadline + 1) * Daily));

        RunToDay(world, simulation, Deadline + 1);

        Assert.False(world.Households.IsUnplaced(tenant));
        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.False(world.Households.IsBehindOnRent(tenant));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// An evicted Landowner keeps its Lots and their rent while it waits in the Unplaced Pool —
    /// <c>L17</c>.
    /// </summary>
    [Fact]
    public void An_evicted_landowner_still_receives_its_lots_rent()
    {
        (World world, Simulation simulation, int lot, int tenant) = Tenancy(endowment: 10 * Daily);
        int landlord = Landlord(world, lot);

        RunToDay(world, simulation, 1);

        long collected = Purse(world, landlord);

        Assert.Equal(Daily, collected);

        world.Unplace(world.Households.Rows.At(landlord));

        Assert.True(world.Households.IsUnplaced(landlord));
        Assert.Equal(world.Households.Rows.At(landlord), world.Lots.Landowner[lot]);

        // Still the Landowner, so the next Days' rent still reaches it in the Pool, and the treasury
        // still receives nothing for ground the city does not hold.
        RunToDay(world, simulation, 3);

        Assert.Equal(collected + (2 * Daily), Purse(world, landlord));
        Assert.Equal(0, Treasury(world));
        Assert.False(world.Households.IsBehindOnRent(tenant));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Business short of its rent past the deadline becomes unpremised, and its debt ends with the
    /// tenancy as a Household's does.
    /// </summary>
    [Fact]
    public void A_business_behind_past_the_deadline_becomes_unpremised()
    {
        (World world, Simulation simulation, _, int resident) = Tenancy(
            endowment: 100 * Daily, trade: 1);

        // The kind names a trade, so CreateBuilding instantiated one in the Building's second Unit.
        int trade = Trader(world, world.Buildings.Rows.Resolve(world.Households.Dwelling[resident]));

        // The shop holds a Unit of the same floor as a tenancy, so it is charged the same figure --
        // and it opened with no grant, so it has nothing to pay it with.
        Assert.Equal(Daily, world.BusinessRent(trade).Raw);
        Assert.Equal(0, world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Businesses.Balance[trade])));

        RunToDay(world, simulation, Deadline);

        Assert.False(world.Businesses.IsUnpremised(trade));
        Assert.Equal(Deadline * Daily, world.Businesses.RentDebt[trade].Raw);

        RunToDay(world, simulation, Deadline + 1);

        Assert.True(world.Businesses.IsUnpremised(trade));
        Assert.Equal(1, world.UnpremisedPool.Count);
        Assert.Equal(0, world.Businesses.RentDebt[trade].Raw);
        Assert.Equal(0, world.Businesses.RentDebtSinceDay[trade]);

        // The resident went on paying throughout, so the treasury holds its rent and nothing else.
        Assert.Equal((Deadline + 1) * Daily, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Ruleset stating no <c>[rent]</c> charges nothing, leaves both columns zero and reports no
    /// rent evidence at all.
    /// </summary>
    [Fact]
    public void Without_the_key_nothing_is_charged()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(
            endowment: 10 * Daily, deadline: 0);

        Assert.False(world.Rules.Rent.Collects);
        Assert.Null(Core.Evidence.Evidence.RentOf(world, world.Households.Rows.At(tenant)));

        RunToDay(world, simulation, 8);

        Assert.Equal(10 * Daily, Purse(world, tenant));
        Assert.Equal(0, Treasury(world));
        Assert.Equal(0, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[tenant]);
        Assert.Equal(default, simulation.LastRent);

        simulation.CheckEndOfRun();
    }

    /// <summary>Evidence reports the Day the shortfall began, and the host spells the sentence.</summary>
    [Fact]
    public void Evidence_reports_the_day_the_shortfall_began()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(endowment: Daily);

        RunToDay(world, simulation, 3);

        RentEvidence rent = Assert.IsType<RentEvidence>(
            Core.Evidence.Evidence.RentOf(world, world.Households.Rows.At(tenant)));

        Assert.True(rent.Behind);
        Assert.Equal(Daily, rent.Daily.Raw);
        Assert.Equal(2 * Daily, rent.Debt.Raw);

        // Day 0's rent was met in full, so the run of shortfalls starts at Day 1.
        Assert.Equal(1, rent.SinceDay);
    }

    /// <summary>
    /// The debt and the Day survive a save and a reload, so a reloaded city evicts on the Day the
    /// running one would have.
    /// </summary>
    [Fact]
    public void A_reload_keeps_the_debt_and_the_day_it_began()
    {
        (World world, Simulation simulation, _, int tenant) = Tenancy(endowment: Daily);

        RunToDay(world, simulation, 2);

        Assert.True(world.Households.IsBehindOnRent(tenant));

        var save = new MemorySave();

        SaveFile.Write(world, 0, save);

        World restored = SaveFile.Read(save, world.Rules, out _);
        var resumed = new Simulation(restored, Key);

        Assert.Equal(
            world.Households.RentDebt[tenant].Raw, restored.Households.RentDebt[tenant].Raw);
        Assert.Equal(
            world.Households.RentDebtSinceDay[tenant], restored.Households.RentDebtSinceDay[tenant]);
        Assert.Equal(world.HashState(), restored.HashState());

        // Both evict on the same Day, which is what saving the Day buys. Day 0's rent was met in
        // full, so the run of shortfalls starts at Day 1 and the deadline falls on Day 4.
        while (world.Tick.Raw < (ulong)(Deadline + 2) * (ulong)Ticks.PerDay)
        {
            simulation.Step(TickInput.Empty);
            resumed.Step(TickInput.Empty);

            // Every Day boundary rather than every Tick, which is where this sweep can diverge and
            // is what keeps the assertion inside the budget.
            if (world.Tick.Raw % Ticks.PerDay == 0)
            {
                Assert.Equal(world.HashState(), restored.HashState());
            }
        }

        Assert.Equal(world.HashState(), restored.HashState());

        Assert.True(world.Households.IsUnplaced(tenant));
        Assert.True(restored.Households.IsUnplaced(tenant));
    }

    /// <summary>The one Business standing in a Building.</summary>
    private static int Trader(World world, int premises)
    {
        foreach (int business in world.BuildingBusinesses.Walk(premises))
        {
            return business;
        }

        Assert.Fail("no Business took premises in this Building.");

        return Core.Tables.Rows.NoSlot;
    }
}
