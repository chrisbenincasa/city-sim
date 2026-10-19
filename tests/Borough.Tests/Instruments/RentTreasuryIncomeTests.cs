using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Instruments;

/// <summary>
/// Rent that reaches the treasury is treasury income, and it appears in the flow accounting the
/// budget and the income report are built on.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>It was money arriving by a path no column watched.</b> A city-held Lot's rent raises the
/// treasury balance, and until this counter existed <see cref="TreasuryFlows.Income"/> stayed flat
/// while it did — so on <c>rulesets/renting.toml</c>, the one shipped file that charges rent, the
/// whole of the city's income read as an unexplained residual. ***That is the same defect
/// <c>MoneyFlowCounter.RuleToTreasury</c> was added to close***, one mechanism along.
/// </para>
/// <para>
/// ⚠ <b><c>Simulation.LastRent.Collected</c> cannot stand in for it and the control tests below say
/// why.</b> That reading counts rent a private Landowner received and mutual debt a ring cancelled,
/// neither of which crossed the treasury's edge — and it is only published on a Day boundary, where
/// arrears are settled all Day.
/// </para>
/// <para>
/// <b>Hand-built worlds</b>, on <c>RentCollectionTests</c>' precedent, so each test states the rate,
/// the floor and the balances it reasons about.
/// </para>
/// </remarks>
public sealed class RentTreasuryIncomeTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x2026_10_17UL);

    private const int Rate = 10;
    private const int Share = 4;
    private const int Tenancies = 2;
    private const int Daily = Share * Rate;
    private const int Deadline = 3;

    /// <summary>Half past Day 1, and the Tick the Policy below pays on. <b>Not a Day boundary.</b></summary>
    private const int MidDay = Ticks.PerDay + (Ticks.PerDay / 2);

    private static readonly ResourceId Cash = new(1);

    /// <summary>
    /// <paramref name="pays"/> money to each Household every <see cref="MidDay"/> Ticks, out of the
    /// treasury. Zero states no Policy at all.
    /// </summary>
    private static Ruleset Rules(int pays = 0) => new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)
        {
            Houses = true,
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
        Policies = pays == 0
            ? []
            : [new PolicyDefinition(
                PolicySubject.Household, MidDay, ApplyCount.Band(1, 1),
                Scope.Global, Scope.Local, Cash, pays)],
    };

    private static Handle<Lot> Ground(World world, Tiles east) => world.Lots.Create(
        east, Tiles.Zero, zone: 0, wide: new Tiles(Share * Tenancies), deep: new Tiles(1));

    private static Handle<Building> Raise(World world, Handle<Lot> lot) =>
        world.CreateBuilding(lot, 1, Ticks.Zero, Key);

    private static long Treasury(World world) =>
        world.Bins.LevelAt(world.FindTreasuryBin(Cash));

    private static long Purse(World world, int household) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Households.Balance[household]));

    /// <summary>Steps until <paramref name="tick"/> has been processed.</summary>
    private static void RunPast(World world, Simulation simulation, long tick)
    {
        while (world.Tick.Raw <= (ulong)tick)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>One Household on city-held ground, holding <paramref name="endowment"/>.</summary>
    private static (World World, Simulation Simulation, int Tenant) CityHeld(
        long endowment, int pays = 0)
    {
        var world = new World(1_000, Rules(pays), Key);
        var simulation = new Simulation(world, Key);

        Handle<Household> tenant = world.CreateHousehold(
            Raise(world, Ground(world, Tiles.Zero)), lifeStage: 0);

        if (endowment > 0)
        {
            world.Endow(tenant, new Money(endowment));
        }

        return (world, simulation, world.Households.Rows.Resolve(tenant));
    }

    /// <summary>
    /// 🔴 <b>A city-held Lot's rent is reported as treasury income, on the Day it is collected.</b>
    /// </summary>
    /// <remarks>
    /// <b>The identity is the assertion and not the figure.</b> The treasury opened empty
    /// (<c>adr/0116</c>) and nothing else here moves money, so the balance after one Day's charge has
    /// to equal income less expenditure — which is exactly what the shell's budget and
    /// <c>IncomeDump</c> hold themselves to.
    /// </remarks>
    [Fact]
    public void Rent_collected_by_the_city_is_treasury_income()
    {
        (World world, Simulation simulation, int tenant) = CityHeld(endowment: 10 * Daily);

        simulation.Step(TickInput.Empty);

        TreasuryFlows flows = simulation.DrainTreasuryFlows();

        Assert.Equal(Daily, flows.Rent);
        Assert.Equal(Daily, flows.Income);
        Assert.Equal(0, flows.Expenditure);

        // Nothing is left unexplained: the balance IS the income.
        Assert.Equal(Daily, Treasury(world));
        Assert.Equal(Treasury(world), flows.Income - flows.Expenditure);

        // A drain is a drain, so the next reading covers an interval in which no rent moved.
        Assert.Equal(0, simulation.DrainTreasuryFlows().Rent);

        Assert.Equal(9 * Daily, Purse(world, tenant));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>Arrears repaid MID-DAY reach the same counter, which is the half a Day-boundary reading
    /// could not carry.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A debt is settled where income lands (<c>plans/money-circulation.md</c> L16), so rent reaches
    /// the treasury on Ticks the rent sweep never runs on. ***A counter folded only on a Day boundary
    /// would miss every unit of this***, and a reader holding the balance against the flows would see
    /// the Policy's payment go out and nothing come back.
    /// </para>
    /// <para>
    /// The Policy pays the two Days owed at <see cref="MidDay"/> and the repayment hands the whole of
    /// it straight back, so the treasury ends where it started while two gross flows of 80 crossed
    /// its edge. <b>A netted counter would read as a city that did nothing.</b>
    /// </para>
    /// </remarks>
    [Fact]
    public void Arrears_repaid_mid_day_reach_the_treasury_rent_counter()
    {
        (World world, Simulation simulation, int tenant) =
            CityHeld(endowment: 0, pays: 2 * Daily);

        // ⚠ The treasury is founded AFTER the first two boundaries and not before them. The Policy
        // fires on Tick 0 as well as at MidDay, so a treasury with money in it from the start would
        // pre-fund the tenant and there would be no debt left for the mid-Day payment to settle.
        RunPast(world, simulation, Ticks.PerDay);

        Assert.Equal(2 * Daily, world.Households.RentDebt[tenant].Raw);
        Assert.Equal(0, Treasury(world));

        world.EndowTreasury(new Money(2 * Daily));

        long opening = Treasury(world);

        RunPast(world, simulation, MidDay);

        // Off a boundary, so the rent sweep did not run on the Tick the assertions describe.
        Assert.NotEqual(0UL, world.Tick.Raw % (ulong)Ticks.PerDay);

        TreasuryFlows flows = simulation.DrainTreasuryFlows();

        Assert.Equal(2 * Daily, flows.Rent);
        Assert.Equal(2 * Daily, flows.PolicyOut);
        Assert.Equal(2 * Daily, flows.Income);
        Assert.Equal(2 * Daily, flows.Expenditure);

        // Back where it started, and the purse is empty: every unit the Policy paid out came back as
        // rent on the Tick it landed.
        Assert.Equal(opening, Treasury(world));
        Assert.Equal(0, Purse(world, tenant));
        Assert.Equal(0, world.Households.RentDebt[tenant].Raw);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// ⚠ <b>The control: rent a PRIVATE Landowner receives is not treasury income.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the line <c>Simulation.LastRent.Collected</c> cannot draw.</b> That reading counts
    /// every unit of rent discharged anywhere in the city, and most of a city's Lots need not be the
    /// city's — so using it as an income line would credit the treasury with money that went into a
    /// Household's purse. ***The balance would then be short of the flows by exactly the private
    /// rent***, which a reader would read as money leaking out of the treasury.
    /// </remarks>
    [Fact]
    public void Rent_a_private_landowner_receives_is_not_treasury_income()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> rented = Ground(world, Tiles.Zero);
        Handle<Lot> owned = Ground(world, new Tiles(128));

        Handle<Household> tenant = world.CreateHousehold(Raise(world, rented), lifeStage: 0);
        Handle<Household> landlord = world.CreateHousehold(Raise(world, owned), lifeStage: 0);

        // A resident Landowner of its own Lot is charged nothing, so no rent in this city is the
        // city's and the treasury must stay out of the figures entirely.
        world.Lots.Landowner[world.Lots.Rows.Resolve(rented)] = landlord;
        world.Lots.Landowner[world.Lots.Rows.Resolve(owned)] = landlord;

        world.Endow(tenant, new Money(10 * Daily));

        int landlordSlot = world.Households.Rows.Resolve(landlord);

        simulation.Step(TickInput.Empty);

        TreasuryFlows flows = simulation.DrainTreasuryFlows();

        // Rent WAS collected -- a Day's worth, into a private purse -- and none of it is income.
        Assert.Equal(Daily, simulation.LastRent.Collected);
        Assert.Equal(Daily, Purse(world, landlordSlot));

        Assert.Equal(0, flows.Rent);
        Assert.Equal(0, flows.Income);
        Assert.Equal(0, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// ⚠ <b>The second control: debt a ring of Landowners CANCELS is not treasury income either.</b>
    /// </summary>
    /// <remarks>
    /// A cancellation discharges a mutual claim and moves no Money at all
    /// (<c>RentChainTests</c>), so it belongs in no column of an account about what crossed the
    /// treasury's edge. ***It is the larger half of <c>LastRent.Collected</c> on a city with rings in
    /// it***, which is the other reason that reading cannot be an income line.
    /// </remarks>
    [Fact]
    public void Debt_a_ring_cancels_is_not_treasury_income()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> first = Ground(world, Tiles.Zero);
        Handle<Lot> second = Ground(world, new Tiles(128));

        Handle<Household> a = world.CreateHousehold(Raise(world, first), lifeStage: 0);
        Handle<Household> b = world.CreateHousehold(Raise(world, second), lifeStage: 0);

        // Each holds the ground the other lives on, so the claims go round a ring.
        world.Lots.Landowner[world.Lots.Rows.Resolve(first)] = b;
        world.Lots.Landowner[world.Lots.Rows.Resolve(second)] = a;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);

        world.Endow(a, new Money(1));
        world.Households.RentDebt[aSlot] = new Money(10 * Daily);
        world.Households.RentDebt[bSlot] = new Money(10 * Daily);

        simulation.Step(TickInput.Empty);

        TreasuryFlows flows = simulation.DrainTreasuryFlows();

        Assert.True(simulation.LastRent.Cancelled > 0, "nothing cancelled, so this proved nothing.");

        Assert.Equal(0, flows.Rent);
        Assert.Equal(0, flows.Income);
        Assert.Equal(0, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// The Census carries the same figure, so the headless income report and the shell budget read
    /// one number.
    /// </summary>
    /// <remarks>
    /// <b>A Census and <c>Simulation.DrainTreasuryFlows</c> are two readers of one drain</b>, so a
    /// run has exactly one of them — this is the Census side, and the tests above are the shell's.
    /// Summed across readings rather than taken off the last one, because a flow reading covers the
    /// interval since the previous one.
    /// </remarks>
    [Fact]
    public void The_census_reports_the_rent_the_treasury_collected()
    {
        (World world, Simulation simulation, _) = CityHeld(endowment: 10 * Daily);

        var census = new Census(world, 8);

        census.Observe(simulation);

        for (int day = 0; day < 4; day++)
        {
            for (int tick = 0; tick < Ticks.PerDay; tick++)
            {
                simulation.Step(TickInput.Empty);
            }

            census.Observe(simulation);
        }

        long rent = 0;

        foreach (CensusSample sample in census
            .Series(Metric.Of(MoneyFlowCounter.Rent, Aggregate.Sum), new Ticks(4 * Ticks.PerDay))
            .Samples.Span)
        {
            rent += sample.Value;
        }

        Assert.Equal(Treasury(world), rent);
        Assert.True(rent > 0, "no rent reached the treasury, so this proved nothing.");

        simulation.CheckEndOfRun();
    }
}
