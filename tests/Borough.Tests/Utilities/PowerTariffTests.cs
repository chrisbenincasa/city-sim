using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Utilities;

/// <summary>
/// Utilities slice 5 — who pays for the Power <c>rulesets/powered.toml</c> delivers, and where that
/// money goes afterwards.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every world here is the fixture's circuit on one street.</b> Two plants make more Power than
/// the street draws, so the delivered fraction is whole and a bill is the full price of a Day; a
/// world built with one plant is short, and that is how a brownout is read. One District, because a
/// second one would put a link between the plants and the consumers and the shortage would become
/// the subject.
/// </para>
/// <para>
/// <b>The money goes round, and the numbers that hold it level are the Ruleset's.</b> The treasury
/// pays each plant per declared post, the plant pays that to whoever holds the post, the Household
/// the worker lives in pays the tariff on the light it draws, and the city's yard is paid its
/// running cost and billed the same amount. Every Day therefore ends where it began, which is what
/// <see cref="The_circuit_runs_two_hundred_days_without_a_holder_draining_or_hoarding"/> watches.
/// </para>
/// <para>
/// <b>The expected bills are arithmetic over the Ruleset rather than measured magnitudes.</b> A Day
/// holds <c>Ticks.PerDay / rate</c> firings, a firing draws <c>amount × applications</c> units, and
/// the price is read from the Policy — so moving a rate in the fixture moves the expectation with it
/// instead of breaking the test.
/// </para>
/// <para>
/// ⚠ <b>No Day before the second is read.</b> The Wheel arms each Rule from the Tick its Building
/// went up, so the opening Day holds fewer firings than the Days after it, and a Household that has
/// not reached a payday yet has nothing to pay with.
/// </para>
/// </remarks>
public sealed class PowerTariffTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(11);

    /// <summary><c>powered.toml</c>'s delivered Utility, and its first <c>[[resource]]</c>.</summary>
    private static readonly ResourceId Power = new(1);

    private static readonly ResourceId Money = new(3);

    // Kinds are one-based: zero is the kind no Ruleset declares.
    private const byte Dwelling = 1;
    private const byte Plant = 2;
    private const byte Works = 3;

    /// <summary>Households in the dwelling, and so the count each one's lighting Rule applies.</summary>
    private const int Occupants = 2;

    /// <summary>Power one application of <c>lighting</c> draws.</summary>
    private const int LumenPower = 2;

    /// <summary>Power one application of <c>machinery</c> draws.</summary>
    private const int MachineryPower = 4;

    /// <summary>What <c>machinery</c> applies when the Power and the treasury both allow it.</summary>
    private const int MachineryMax = 4;

    /// <summary>The fixture's Rule periods, in Ticks.</summary>
    private const int LightingRate = 32;

    private const int MachineryRate = 8;

    /// <summary>Plants in a world that makes more Power than the street draws.</summary>
    private const int Ample = 2;

    /// <summary>The Cells the street runs through.</summary>
    private const int Cells = 4;

    /// <summary>Empty dwellings per Cell, raised so the watershed reads a settlement.</summary>
    private const int Fillers = 4;

    private static Ruleset Rules()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    private sealed class City
    {
        public required World World { get; init; }

        public required Simulation Simulation { get; init; }

        /// <summary>The occupied dwelling's Building slot.</summary>
        public required int Home { get; init; }

        /// <summary>The works' Building slot.</summary>
        public required int Yard { get; init; }

        /// <summary>The works' Business slot, which holds the till its Rule is billed against.</summary>
        public required int Contractor { get; init; }

        /// <summary>The plants' Business slots, one post each.</summary>
        public required int[] Generators { get; init; }

        /// <summary>The dwelling's Household slots. The first <c>Generators</c> of them earn.</summary>
        public required int[] Tenants { get; init; }
    }

    /// <summary>
    /// A street with <paramref name="plants"/> plants, one dwelling of two Households and the city's
    /// yard. One Household holds each plant's single post.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Citizens are hired by hand</b>, which is <c>ServiceStaffingTests.Hire</c>'s reason.
    /// The assignment pass makes the staff a function of a route and a Commute Budget, and no
    /// assertion here is about either. One worker per plant, because a plant's Lot is a Tile across
    /// and <c>floor_tiles_per_job</c> is 1.
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing is endowed and no money is issued here.</b> Every balance in the circuit is
    /// filled by the Ruleset's own transfers out of its opening treasury, so what these tests read is
    /// the Ruleset's arithmetic and not a figure the fixture chose.
    /// </para>
    /// <para>
    /// <paramref name="guard"/> folds every column of every table twice a Tick against a phase meant
    /// to be <c>O(woken)</c>, which is <c>--no-decide-guard</c>'s reason and is why the long run
    /// turns it off. Every short world here keeps it.
    /// </para>
    /// </remarks>
    private static City Build(int plants = Ample, bool guard = true)
    {
        var world = new World(400, Rules(), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = guard };

        Lay(world);

        var generators = new int[plants];

        for (int at = 0; at < plants; at++)
        {
            generators[at] = Trade(world, Raise(world, cell: 0, index: at, Plant));
        }

        int home = Raise(world, cell: 1, index: 0, Dwelling);
        var tenants = new int[Occupants];

        for (int at = 0; at < Occupants; at++)
        {
            Handle<Household> household = world.CreateHousehold(
                world.Buildings.Rows.At(home), lifeStage: 1);

            tenants[at] = world.Households.Rows.Resolve(household);

            if (at < plants)
            {
                world.Employ(
                    world.CreateCitizen(household),
                    world.Businesses.Rows.At(generators[at]),
                    Ticks.Zero);
            }
        }

        int yard = Raise(world, cell: 2, index: 0, Works);

        for (int cell = 0; cell < Cells; cell++)
        {
            for (int filler = 0; filler < Fillers; filler++)
            {
                Raise(world, cell, filler + Ample, Dwelling);
            }
        }

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();

        // One District, so every consumer draws on the network the plants feed with no link between.
        Assert.Equal(1, world.Districts.Rows.LiveCount);

        return new City
        {
            World = world,
            Simulation = simulation,
            Home = home,
            Yard = yard,
            Contractor = Trade(world, yard),
            Generators = generators,
            Tenants = tenants,
        };
    }

    /// <summary>The street: a chain of Nodes a block apart along <c>north = 0</c>.</summary>
    private static void Lay(World world)
    {
        int block = world.Rules.Roads.BlockTiles;
        Handle<RoadNode> previous = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int step = 1; step <= Cells; step++)
        {
            Handle<RoadNode> next = world.Roads.Nodes.Create(new Tiles(step * block), Tiles.Zero);

            world.Roads.Segments.Create(
                previous, next, new Tiles(block), RoadKind.Street, TravelMode.Any, TravelMode.Any);

            previous = next;
        }

        world.Roads.RebuildDerived();
    }

    private static int Raise(World world, int cell, int index, byte kind)
    {
        int block = world.Rules.Roads.BlockTiles;

        Handle<Lot> lot = world.Lots.Create(
            new Tiles((cell * block) + 4 + (index * 2)), Tiles.Zero, zone: 1);

        return world.Buildings.Rows.Resolve(world.CreateBuilding(lot, kind, Ticks.Zero, Key));
    }

    /// <summary>The Business a premises holds, which its kind declares at construction.</summary>
    private static int Trade(World world, int buildingSlot)
    {
        int business = world.Buildings.BusinessHead[buildingSlot] - 1;

        Assert.NotEqual(Rows.NoSlot, business);

        return business;
    }

    // ---- what the fixture says -----------------------------------------------------------------

    /// <summary>Money per unit of Power received, as the Policy in force prices it.</summary>
    private static long Tariff(World world)
    {
        int policy = TariffPolicy(world);

        Assert.InRange(policy, 0, world.Rules.Policies.Length - 1);

        return world.Policies.AmountOf(policy, world.Rules.Policies[policy]);
    }

    /// <summary>The <c>power_tariff</c> Policy's index, which is what <c>Govern</c> names.</summary>
    private static int TariffPolicy(World world) => world.Rules.TariffOf(Power);

    /// <summary>
    /// Units of Power the dwelling draws in a whole Day at full delivery. Each Household runs its own
    /// Instance and each Instance applies the Building's occupancy, so the count is the square.
    /// </summary>
    private static long LightingUnits() =>
        (long)Occupants * Occupants * LumenPower * (Ticks.PerDay / LightingRate);

    /// <summary>Units of Power the works draws in a whole Day at full delivery.</summary>
    private static long MachineryUnits() =>
        (long)MachineryMax * MachineryPower * (Ticks.PerDay / MachineryRate);

    /// <summary>What one firing applies when <paramref name="count"/> applications are wanted.</summary>
    private static long Applied(int count, Ratio delivered) =>
        IntegerMath.MulDivFloor(count, delivered.Raw, Fixed.One);

    private static long Wage(World world, int businessSlot) =>
        world.Rules.BusinessKind(world.Businesses.Kind[businessSlot]).WagePerDay;

    // ---- what the world holds ------------------------------------------------------------------

    private static long Balance(World world, Handle<Bin> purse) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(purse));

    private static long Treasury(World world) =>
        world.Bins.LevelAt(world.FindTreasuryBin(Money));

    /// <summary>What a Building paid and had waived for Power on the Day before the current one.</summary>
    private static (long Paid, long Unpaid) BilledYesterday(World world, int buildingSlot) =>
        world.UtilityBills.PriorOn(
            world.Buildings,
            buildingSlot,
            world.Rules.ResourceKey(Power),
            BusinessAccounts.DayOf(world.Tick));

    private static Ratio Delivered(World world)
    {
        int row = world.UtilityFlowOfPool(world.Markets.Row(world, 0, Power));

        Assert.NotEqual(Rows.NoSlot, row);

        return world.UtilityFlows.Delivered[row];
    }

    private static void StepTo(Simulation simulation, long tick)
    {
        while ((long)simulation.World.Tick.Raw < tick)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>
    /// Steps to the first Tick of <paramref name="day"/> and empties the treasury's flow columns, so
    /// the Day before it is closed and readable and the next drain covers whole Days only.
    /// </summary>
    private static void OpeningOf(City city, int day)
    {
        StepTo(city.Simulation, ((long)day * Ticks.PerDay) + 1);
        city.Simulation.DrainTreasuryFlows();
    }

    /// <summary>The treasury's movements since the last drain, which is one Day here.</summary>
    private static TreasuryFlows Drain(City city) => city.Simulation.DrainTreasuryFlows();

    // ---- the bills -----------------------------------------------------------------------------

    /// <summary>
    /// A Household pays the tariff on every unit its Rule received, out of its own balance, and the
    /// treasury is credited the same money.
    /// </summary>
    [Fact]
    public void A_household_pays_the_tariff_on_the_units_its_rules_received()
    {
        City city = Build();
        World world = city.World;

        OpeningOf(city, day: 2);

        Assert.Equal(Ratio.One, Delivered(world));

        long expected = LightingUnits() * Tariff(world);
        (long paid, long unpaid) = BilledYesterday(world, city.Home);

        Assert.Equal(expected, paid);
        Assert.Equal(0, unpaid);

        // The Day's light cost each Household exactly its Day's wage, so both hold one Day's pay
        // again at the Day's opening and neither is building a balance up or running one down.
        long wage = Wage(world, city.Generators[0]);

        Assert.Equal(expected / Occupants, wage);

        foreach (int tenant in city.Tenants)
        {
            Assert.Equal(wage, Balance(world, world.Households.Balance[tenant]));
        }

        // The whole Day's tariff income, the dwelling's share of it and the yard's.
        OpeningOf(city, day: 3);
        StepTo(city.Simulation, (4 * Ticks.PerDay) + 1);

        Assert.Equal(expected + (MachineryUnits() * Tariff(world)), Drain(city).Tariff);
        Assert.Equal(expected, BilledYesterday(world, city.Home).Paid);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Business pays the tariff out of its own till, and books it as the Day's expense.
    /// </summary>
    /// <remarks>
    /// <b>The works is a Business because its Rule addresses a business-owned Bin</b>, and that is
    /// the whole reason the Bin is there, because a premises Rule drawing a tariffed Utility has no
    /// payer and is refused at load. The contract the city pays it per application is its bill at the
    /// declared tariff, so its till is flat and what it was charged is the only trace its Day leaves.
    /// </remarks>
    [Fact]
    public void A_business_pays_the_tariff_from_its_own_till()
    {
        City city = Build();
        World world = city.World;

        OpeningOf(city, day: 2);

        long expected = MachineryUnits() * Tariff(world);
        (long paid, long unpaid) = BilledYesterday(world, city.Yard);

        Assert.Equal(expected, paid);
        Assert.Equal(0, unpaid);
        Assert.Equal(0, Balance(world, world.Businesses.Balance[city.Contractor]));

        // Booked through the Business's own accounts, so a profit tax would see what Power cost it.
        StepTo(city.Simulation, (long)world.Tick.Raw + (2 * MachineryRate));

        Assert.True(
            world.Businesses.DayExpense[city.Contractor] > 0,
            "the works was billed and booked no expense");

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Governing the tariff up raises the next whole Day's bill by the same factor, and the part a
    /// payer cannot cover is waived rather than owed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both halves belong here.</b> The bill is the governable number multiplied by units
    /// received, so doubling the number doubles the charge — but no income in this city doubled with
    /// it, so the half neither payer can meet appears in the waived counter instead of as a debt
    /// nothing here could collect.
    /// </para>
    /// <para>
    /// ⚠ <b>The Day the edit lands on is skipped.</b> A Rule fires on the Day's opening Ticks too, so
    /// the Day a <c>Govern</c> arrives on holds firings at both prices; the Day after it is the first
    /// whole Day at the new one.
    /// </para>
    /// </remarks>
    [Fact]
    public void Governing_the_tariff_up_raises_the_next_days_bill()
    {
        City city = Build();
        World world = city.World;

        OpeningOf(city, day: 2);

        long before = Tariff(world);
        (long paid, long waived) = BilledYesterday(world, city.Home);

        Assert.Equal(LightingUnits() * before, paid);
        Assert.Equal(0, waived);

        city.Simulation.Step(new TickInput(
            [Command.Govern(TariffPolicy(world), (int)(before * 2))], rulesetHash: 0));

        Assert.Equal(before * 2, Tariff(world));

        OpeningOf(city, day: 4);

        (long dearer, long unpaid) = BilledYesterday(world, city.Home);
        (long yardPaid, long yardUnpaid) = BilledYesterday(world, city.Yard);

        Assert.Equal(LightingUnits() * before * 2, dearer + unpaid);
        Assert.Equal(MachineryUnits() * before * 2, yardPaid + yardUnpaid);

        // Each payer hands over everything it has and the rest is waived, so what reaches the
        // treasury is its unchanged income rather than the doubled charge.
        Assert.Equal(LightingUnits() * before, dearer);
        Assert.Equal(MachineryUnits() * before, yardPaid);
        Assert.True(unpaid > 0, "the dwelling met a doubled tariff out of an unchanged wage");
        Assert.True(yardUnpaid > 0, "the works met a doubled tariff out of an unchanged contract");

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A browned-out Day bills less than a Day of full supply, in the proportion the Rules applied
    /// in, and no key in the Ruleset says so.
    /// </summary>
    /// <remarks>
    /// <b>Two worlds rather than one city twice</b>, so that the only difference is the Power. A city
    /// that lost a plant mid-run would also have lost a post and a wage, and its bill would move for
    /// two reasons at once. The charge is per unit received and a brownout is in the units, so the
    /// ratio falls out of <c>floor(count × delivered)</c> with no key of its own.
    /// </remarks>
    [Fact]
    public void A_brownout_bills_less_than_a_day_of_full_supply()
    {
        City ample = Build();
        City starved = Build(plants: 1);

        OpeningOf(ample, day: 2);
        OpeningOf(starved, day: 2);

        Ratio delivered = Delivered(starved.World);

        Assert.Equal(Ratio.One, Delivered(ample.World));
        Assert.True(delivered > Ratio.Zero, "the short world was delivered nothing");
        Assert.True(delivered < Ratio.One, "the short world is not short");

        (long fullHome, _) = BilledYesterday(ample.World, ample.Home);
        (long shortHome, long shortWaived) = BilledYesterday(starved.World, starved.Home);
        (long fullYard, _) = BilledYesterday(ample.World, ample.Yard);
        (long shortYard, long shortYardWaived) = BilledYesterday(starved.World, starved.Yard);

        // The works' contract scales with the fired count, so the whole of its smaller bill is still
        // paid. The dwelling's wage does not scale, so what it does not spend stays in its balance.
        Assert.Equal(0, shortYardWaived);
        Assert.Equal(
            MachineryUnits() * Applied(MachineryMax, delivered) / MachineryMax, shortYard);
        Assert.True(shortYard < fullYard, "the short works was billed a full Day");

        Assert.Equal(
            LightingUnits() * Applied(Occupants, delivered) / Occupants,
            shortHome + shortWaived);
        Assert.True(shortHome < fullHome, "the short dwelling was billed a full Day");

        ample.Simulation.CheckEndOfRun();
        starved.Simulation.CheckEndOfRun();
    }

    // ---- where the money goes ------------------------------------------------------------------

    /// <summary>
    /// The plant is paid its funding per declared post out of the treasury, and pays it on as the
    /// wage that lets its worker's Household pay for its Power.
    /// </summary>
    /// <remarks>
    /// <b>The funding is per post and not per worker</b>, which is <c>school_funding</c>'s shape.
    /// The grant lands whether the post is filled or not, so an understaffed plant shows a rising
    /// till beside an empty payroll and names hiring as the cause rather than money.
    /// </remarks>
    [Fact]
    public void The_plant_is_paid_its_funding_and_pays_it_out_in_wages()
    {
        City city = Build();
        World world = city.World;

        OpeningOf(city, day: 2);

        int plant = city.Generators[0];
        long wage = Wage(world, plant);

        Assert.Equal(1, world.Workers.Length(plant));
        Assert.True(wage > 0, "the plant's trade pays nothing");

        // The grant and the payroll are the same figure on the same Day, so the till is level and
        // what stands in it is one Day's wage waiting for the next payday.
        Assert.Equal(wage, Balance(world, world.Businesses.Balance[plant]));

        long treasury = Treasury(world);
        long tenant = Balance(world, world.Households.Balance[city.Tenants[0]]);

        Assert.Equal(wage, tenant);

        OpeningOf(city, day: 3);

        Assert.Equal(wage, Balance(world, world.Businesses.Balance[plant]));
        Assert.Equal(tenant, Balance(world, world.Households.Balance[city.Tenants[0]]));
        Assert.Equal(treasury, Treasury(world));

        StepTo(city.Simulation, (4 * Ticks.PerDay) + 1);

        TreasuryFlows flows = Drain(city);

        Assert.Equal(wage * city.Generators.Length, flows.PolicyOut);
        Assert.Equal(flows.Income, flows.Expenditure);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Two hundred Days of billing conserve money, trip no invariant and leave every holder in the
    /// circuit where it started, so nothing drains and nothing hoards.
    /// </summary>
    /// <remarks>
    /// <b>The bound is asserted as equality at the Day boundary rather than as a band.</b> A circuit
    /// whose transfers are equal by construction has a fixed point, so a drift of any size is a
    /// defect and not noise — and every holder is read at the same point of every Day, so the
    /// within-Day sawtooth cannot be mistaken for one.
    /// </remarks>
    [Fact]
    public void The_circuit_runs_two_hundred_days_without_a_holder_draining_or_hoarding()
    {
        const int Days = 220;

        City city = Build(guard: false);
        World world = city.World;

        OpeningOf(city, day: 2);

        long treasury = Treasury(world);
        long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
        long till = Balance(world, world.Businesses.Balance[city.Generators[0]]);
        long tenant = Balance(world, world.Households.Balance[city.Tenants[0]]);
        long charged = (LightingUnits() + MachineryUnits()) * Tariff(world);

        Assert.True(treasury > 0, "the treasury opened empty");

        for (int day = 3; day <= Days; day++)
        {
            StepTo(city.Simulation, ((long)day * Ticks.PerDay) + 1);

            TreasuryFlows flows = Drain(city);

            Assert.Equal(charged, flows.Tariff);
            Assert.Equal(flows.Income, flows.Expenditure);
            Assert.Equal(treasury, Treasury(world));
            Assert.Equal(issued, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);
            Assert.Equal(till, Balance(world, world.Businesses.Balance[city.Generators[0]]));
            Assert.Equal(tenant, Balance(world, world.Households.Balance[city.Tenants[0]]));
            Assert.Equal(0, Balance(world, world.Businesses.Balance[city.Contractor]));
        }

        Assert.Equal(LightingUnits() * Tariff(world), BilledYesterday(world, city.Home).Paid);
        Assert.Equal(MachineryUnits() * Tariff(world), BilledYesterday(world, city.Yard).Paid);
        Assert.Equal(Ratio.One, Delivered(world));

        city.Simulation.CheckEndOfRun();
    }
}
