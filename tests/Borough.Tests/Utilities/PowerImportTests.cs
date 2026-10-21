using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Utilities;

/// <summary>
/// Utilities slice 4 — a District buying its shortfall at an Outside Connection, and the treasury
/// paying for it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The ridge is <see cref="PowerDeliveryTests"/>' and the plant is optional.</b> Eight blocks in
/// a line with tall Cells at each end read as two Districts joined by one crossing Segment, and a
/// gate takes a filler's place so that raising one leaves the Building count and the watershed
/// exactly where they were. Dropping the plant is what makes an import the only source a District
/// has, so a world with neither plant nor gate delivers nothing at all.
/// </para>
/// <para>
/// <b>Every bill is asserted as arithmetic over the flow columns.</b> A test reads
/// <see cref="UtilityFlowTable.Imported"/> and computes what the treasury must have paid for it,
/// so moving the fixture's price or its capacity moves the expectation with it.
/// </para>
/// <para>
/// ⚠ <b>The gate on the west edge stands at <c>east = 0, north = 16</c>, not at the origin.</b> A
/// Lot on two edges touches neither (<c>MapEdge.Touching</c> answers <c>None</c> at a corner) and
/// <c>Invariant.OutsideConnectionStandsOnOneEdge</c> refuses it, while the ridge itself runs along
/// <c>north = 0</c> and is already on the south edge.
/// </para>
/// </remarks>
public sealed class PowerImportTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    /// <summary>The delivered Utility, and <c>powered.toml</c>'s first <c>[[resource]]</c>.</summary>
    private static readonly ResourceId Power = new(1);

    // Kinds are one-based: zero is the kind no Ruleset declares.
    private const byte Dwelling = 1;
    private const byte Plant = 2;
    private const byte Works = 3;
    private const byte Gate = 4;

    private const int Cells = 8;
    private const int PeakCells = 2;
    private const int Tall = 8;
    private const int Low = 3;

    /// <summary>Households put in the one dwelling that draws power, and so its occupancy.</summary>
    private const int Occupants = 2;

    /// <summary>What the west Hinterland below charges, dearer than the south's on purpose.</summary>
    private const long Dear = 5;

    /// <summary>A second Hinterland, on another edge, selling Power dearer than the south does.</summary>
    /// <remarks>
    /// <b>Two prices need two edges.</b> The loader admits one <c>[[hinterland]]</c> per edge, so a
    /// fixture asking which gate is bought first has to put the dear one somewhere else.
    /// <para>
    /// ⚠ <b>Both Goods are priced here at the south Hinterland's own figures, and leaving them out
    /// would not have been free.</b> <c>Ruleset.ImportCeiling</c> is the minimum across <em>every</em>
    /// declared Hinterland and counts an unpriced Resource as zero, while <c>RefuseUnpricedGoods</c>
    /// only asks for a price at <em>some</em> Hinterland — so an edge pricing nothing but Power would
    /// load clean and make both Goods free for ever in the Pools. Nothing here buys a Good, so the
    /// fixture would still pass; ***a fixture that holds only because nothing exercises it is the
    /// one the next world copies.***
    /// </para>
    /// </remarks>
    private const string WestHinterland = """

        [[hinterland]]
        edge = "west"
        emigrant_balance_min = 0
        emigrant_balance_max = 0
        prices = [
            { resource = "lumens", price = 500 },
            { resource = "power",  price = 5 },
        ]
        """;

    private static Ruleset Rules() => Variant();

    /// <summary><c>powered.toml</c>, optionally with one text edit and one appended table.</summary>
    private static Ruleset Variant(string appended = "", string from = "", string to = "")
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        if (from.Length > 0)
        {
            Assert.Contains(from, toml, StringComparison.Ordinal);

            toml = toml.Replace(from, to, StringComparison.Ordinal);
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(toml + appended, "powered.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    /// <summary>The fixture with a dearer second edge to buy from.</summary>
    private static Ruleset TwoPrices() => Variant(WestHinterland);

    /// <summary>The same, with a gate that can bring in more than a link can carry.</summary>
    private static Ruleset WidePort() => Variant(
        WestHinterland, "import_per_connection = 2560", "import_per_connection = 5120");

    /// <summary>The fixture founded with <paramref name="balance"/> instead of its own figure.</summary>
    private static Ruleset Funded(long balance) =>
        Variant(from: "opening_balance = 4194304", to: $"opening_balance = {balance}");

    /// <summary>What a world is asked to be, so one builder serves every case below.</summary>
    /// <param name="Plant">Raise the plant in the west, which is the only local source there is.</param>
    /// <param name="Works">Raise the greedy consumer in the east, which outruns one link.</param>
    /// <param name="SouthGates">Outside Connections in the east District, on the south edge.</param>
    /// <param name="WestGate">
    /// One Outside Connection in the west District, on the west edge, so an import has to travel a
    /// link to reach the consumers.
    /// </param>
    /// <param name="StrandedGate">
    /// One Outside Connection on the south edge at <see cref="Stranded"/>, raised after the
    /// watershed ran so that it stands in no District.
    /// </param>
    private readonly record struct Shape(
        bool Plant = false,
        bool Works = false,
        int SouthGates = 0,
        bool WestGate = false,
        bool StrandedGate = false);

    /// <summary>
    /// Where a gate stands to be on a priced edge and in no District.
    /// </summary>
    /// <remarks>
    /// <b>On the south edge, which is the edge that prices Power</b>, so a gate here is skipped for
    /// want of a District and not for want of a price. The ridge occupies the first 256 Tiles and
    /// its two Districts cover only the Cells it built, so 8,192 is an empty Cell with no road
    /// frontage — a Cell the next watershed would make a third District of, which is why the gate
    /// is raised after that has run and the premise is asserted rather than assumed.
    /// </remarks>
    private const int Stranded = 8_192;

    private sealed class City
    {
        public required World World { get; init; }

        public required Simulation Simulation { get; init; }

        /// <summary>The District the consumers stand in.</summary>
        public required int Burning { get; init; }

        /// <summary>The District at the ridge's west end.</summary>
        public required int Western { get; init; }
    }

    private static City Build(Shape shape, Ruleset? rules = null)
    {
        var world = new World(400, rules ?? Rules(), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };

        Lay(world);

        int home = Rows.NoSlot;

        for (int cell = 0; cell < Cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= Cells - PeakCells;
            int standing = 0;

            // Every Building a Shape does not ask for is an empty dwelling, which draws nothing
            // because its apply count is its occupancy. A gate takes one of their places rather
            // than adding a Building, so the watershed reads the same ridge in every world here.
            if (cell == 0)
            {
                if (shape.Plant)
                {
                    Raise(world, cell, standing++, Plant);
                }

                if (shape.WestGate)
                {
                    RaiseAt(world, Tiles.Zero, new Tiles(16), Gate);
                    standing++;
                }
            }

            if (cell == Cells - 1)
            {
                if (shape.Works)
                {
                    Raise(world, cell, standing++, Works);
                }

                home = Raise(world, cell, standing++, Dwelling);

                for (int occupant = 0; occupant < Occupants; occupant++)
                {
                    world.CreateHousehold(world.Buildings.Rows.At(home), lifeStage: 1);
                }

                for (int gate = 0; gate < shape.SouthGates; gate++)
                {
                    Raise(world, cell, standing++, Gate);
                }
            }

            for (int filler = standing; filler < (peak ? Tall : Low); filler++)
            {
                Raise(world, cell, filler, Dwelling);
            }
        }

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Equal(2, world.Districts.Rows.LiveCount);

        // Raised after the watershed, because a Cell holding a Building always ends up in some
        // District once it has run. The Cell-to-District index is what the gate loop reads, and
        // RebuildDerived restores it rather than re-deriving it, so the gate stays unplaced until
        // phase 6 of the next revisit Tick.
        if (shape.StrandedGate)
        {
            RaiseAt(world, new Tiles(Stranded), Tiles.Zero, Gate);

            world.RebuildParcels();
            world.RebuildDerived();
        }

        int burning = DistrictOf(world, home);
        int western = DistrictAt(world, Tiles.Zero, Tiles.Zero);

        Assert.NotEqual(Rows.NoSlot, burning);
        Assert.NotEqual(Rows.NoSlot, western);
        Assert.NotEqual(western, burning);

        return new City
        {
            World = world,
            Simulation = simulation,
            Burning = burning,
            Western = western,
        };
    }

    /// <summary>The ridge: a chain of Nodes a block apart along <c>north = 0</c>.</summary>
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

    /// <summary>
    /// Puts one Building on a Lot of its own in <paramref name="cell"/>, offset by
    /// <paramref name="index"/> so a Cell can hold several.
    /// </summary>
    private static int Raise(World world, int cell, int index, byte kind)
    {
        int block = world.Rules.Roads.BlockTiles;

        return RaiseAt(
            world, new Tiles((cell * block) + 4 + (index * 2)), Tiles.Zero, kind);
    }

    /// <summary>Puts one Building on a Lot of its own at an explicit Tile.</summary>
    private static int RaiseAt(World world, Tiles east, Tiles north, byte kind) =>
        world.Buildings.Rows.Resolve(world.CreateBuilding(
            world.Lots.Create(east, north, zone: 1), kind, Ticks.Zero, Key));

    private static int DistrictOf(World world, int buildingSlot)
    {
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[buildingSlot]);

        return DistrictAt(world, world.Lots.East[lot], world.Lots.North[lot]);
    }

    private static int DistrictAt(World world, Tiles east, Tiles north)
    {
        Handle<District> district = world.DistrictsInCells.Of(
            world.DistrictCells, CellGrid.ToCells(east), CellGrid.ToCells(north));

        return world.Districts.Rows.TryResolve(district, out int slot) ? slot : Rows.NoSlot;
    }

    /// <summary>The network row for one District's Power, which every world here has.</summary>
    private static int Flow(World world, int districtSlot)
    {
        int row = world.UtilityFlowOfPool(world.Markets.Row(world, districtSlot, Power));

        Assert.NotEqual(Rows.NoSlot, row);

        return row;
    }

    private static Ratio Delivered(World world, int districtSlot) =>
        world.UtilityFlows.Delivered[Flow(world, districtSlot)];

    private static long Imported(World world, int districtSlot) =>
        world.UtilityFlows.Imported[Flow(world, districtSlot)];

    private static long Paid(World world, int districtSlot) =>
        world.UtilityFlows.ImportPaid[Flow(world, districtSlot)].Raw;

    /// <summary>What the city has left to buy with.</summary>
    private static long Purse(World world)
    {
        foreach (int bin in world.TreasuryBins.Walk(TreasuryTable.Slot))
        {
            return world.Bins.LevelAt(bin);
        }

        throw new InvalidOperationException("the world has no treasury Bin.");
    }

    /// <summary>Money issued into this world, net of everything that has left it.</summary>
    private static long Issued(World world) =>
        world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

    /// <summary>
    /// The cheapest price any Hinterland charges for Power, which is what a south-edge gate pays.
    /// </summary>
    private static long Cheap(World world) => world.Rules.ImportCeiling(Power).Raw;

    private static long Capacity(World world) => world.Rules.ImportPerConnection(Power);

    private static void StepTo(Simulation simulation, long tick)
    {
        while ((long)simulation.World.Tick.Raw < tick)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>
    /// Steps through the settle that closes <paramref name="day"/> and no further, so the Day's
    /// production, demand and bill are readable.
    /// </summary>
    private static void AfterSettling(Simulation simulation, int day) =>
        StepTo(simulation, (((long)day + 1) * Ticks.PerDay) + 1);

    // ---- what a gate brings in -----------------------------------------------------------------

    /// <summary>
    /// A District with no plant anywhere in the city is served entirely by its own gate, and the
    /// treasury pays the Outside for every unit of it.
    /// </summary>
    /// <remarks>
    /// <b>Both Districts are asserted to have produced nothing</b>, which is what makes this a test
    /// of the import and not of a plant: every unit the east burns came through the door. The money
    /// leaves the treasury Bin and the money supply by the same figure in the same settle, because
    /// the Outside holds no balance for it to arrive in.
    /// </remarks>
    [Fact]
    public void A_gate_serves_a_district_with_no_plant_in_the_city()
    {
        City city = Build(new Shape(SouthGates: 1));
        World world = city.World;
        long purse = Purse(world);
        long issued = Issued(world);

        AfterSettling(city.Simulation, day: 0);

        int burning = Flow(world, city.Burning);
        long imported = world.UtilityFlows.Imported[burning];

        Assert.Equal(0, world.UtilityFlows.Supplied[burning]);
        Assert.Equal(0, world.UtilityFlows.Supplied[Flow(world, city.Western)]);
        Assert.True(imported > 0, "the gate brought in nothing");
        Assert.Equal(0, Imported(world, city.Western));

        // Under one gate's capacity, so the whole demand and the whole road loss are covered.
        Assert.True(imported < Capacity(world), $"{imported} units is the capacity, not the need");
        Assert.Equal(Ratio.One, Delivered(world, city.Burning));
        Assert.Equal(world.UtilityFlows.Asked[burning], world.UtilityFlows.Received[burning]);

        long bill = imported * Cheap(world);

        Assert.Equal(bill, Paid(world, city.Burning));
        Assert.Equal(purse - bill, Purse(world));
        Assert.Equal(issued - bill, Issued(world));
        Assert.Equal(bill, city.Simulation.DrainTreasuryFlows().UtilityImport);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// The import covers what the plant and the link could not, and nothing beyond it.
    /// </summary>
    /// <remarks>
    /// <b>The same world is built twice, with and without the gate.</b> The gate takes a filler's
    /// place, so both cities have the same Buildings, the same watershed and the same demand — and
    /// what the gated one receives above the bare one is exactly what it imported. The figure is
    /// under one gate's capacity, so the shortfall bounded the purchase rather than the door.
    /// </remarks>
    [Fact]
    public void Local_power_and_the_link_come_before_the_import()
    {
        City bare = Build(new Shape(Plant: true, Works: true));
        City gated = Build(new Shape(Plant: true, Works: true, SouthGates: 1));

        AfterSettling(bare.Simulation, day: 0);
        AfterSettling(gated.Simulation, day: 0);

        int one = Flow(bare.World, bare.Burning);
        int two = Flow(gated.World, gated.Burning);
        long imported = gated.World.UtilityFlows.Imported[two];

        Assert.Equal(bare.World.UtilityFlows.Asked[one], gated.World.UtilityFlows.Asked[two]);
        Assert.Equal(0, bare.World.UtilityFlows.Imported[one]);
        Assert.Equal(
            bare.World.UtilityFlows.Supplied[Flow(bare.World, bare.Western)],
            gated.World.UtilityFlows.Supplied[Flow(gated.World, gated.Western)]);

        Assert.True(imported > 0, "the gate brought in nothing");
        Assert.True(
            imported < Capacity(gated.World),
            "the gate's capacity bounded the purchase, so this says nothing about the order");
        Assert.Equal(
            imported,
            gated.World.UtilityFlows.Received[two] - bare.World.UtilityFlows.Received[one]);

        bare.Simulation.CheckEndOfRun();
        gated.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// One gate brings in <c>import_per_connection</c> and no more, and a second one raises the
    /// delivered fraction.
    /// </summary>
    [Fact]
    public void A_second_gate_raises_the_delivered_fraction()
    {
        City one = Build(new Shape(Works: true, SouthGates: 1));
        City two = Build(new Shape(Works: true, SouthGates: 2));

        AfterSettling(one.Simulation, day: 0);
        AfterSettling(two.Simulation, day: 0);

        Ratio narrow = Delivered(one.World, one.Burning);

        Assert.Equal(Capacity(one.World), Imported(one.World, one.Burning));
        Assert.True(narrow > Ratio.Zero, "one gate delivered nothing");
        Assert.True(narrow < Ratio.One, "one gate covered the whole demand");

        Assert.True(
            Imported(two.World, two.Burning) > Capacity(two.World),
            "the second gate brought in nothing");
        Assert.True(
            Delivered(two.World, two.Burning) > narrow,
            $"one gate delivered {narrow.Raw} and two delivered the same");
        Assert.Equal(Ratio.One, Delivered(two.World, two.Burning));

        // Each unit is charged at its own gate's price, and both gates are on the same edge.
        Assert.Equal(
            Imported(two.World, two.Burning) * Cheap(two.World), Paid(two.World, two.Burning));

        one.Simulation.CheckEndOfRun();
        two.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// The cheapest gate is bought out before a dearer one sells anything.
    /// </summary>
    /// <remarks>
    /// <b>One gate on each of two edges, at two prices, and a shortfall bigger than either one can
    /// cover.</b> The bill therefore says which door the first units came through: cheapest-first
    /// pays the south price for a full capacity and the west price for the remainder, and the
    /// reverse order would cost strictly more for exactly the same units.
    /// </remarks>
    [Fact]
    public void The_cheapest_gate_is_bought_out_first()
    {
        City city = Build(new Shape(Works: true, SouthGates: 1, WestGate: true), TwoPrices());
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        long capacity = Capacity(world);
        long cheap = Cheap(world);
        long imported = Imported(world, city.Burning);

        Assert.True(cheap < Dear, $"the south price is {cheap} and the west {Dear}");
        Assert.Equal(0, Imported(world, city.Western));
        Assert.True(
            imported > capacity, "one gate covered the shortfall, so the order never mattered");

        long paid = Paid(world, city.Burning);

        Assert.Equal((capacity * cheap) + ((imported - capacity) * Dear), paid);
        Assert.True(
            paid < (capacity * Dear) + ((imported - capacity) * cheap),
            $"{imported} units cost {paid}, which is the dear gate first");
        Assert.Equal(paid, city.Simulation.DrainTreasuryFlows().UtilityImport);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Two gates at the same price are bought in the order they were raised, and a recycled
    /// Building slot does not reorder them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The second gate is raised into the slot a demolished filler left</b>, so it holds a lower
    /// Building slot than the gate raised before it. A tiebreak on the slot would buy the newer
    /// gate out first; the tiebreak is the monotonic Building id, which <c>CLAUDE.md</c>'s rule for
    /// a handle's identity says is the thing a recyclable slot is not.
    /// </para>
    /// <para>
    /// <b>Both Districts are short and the purse buys 50 units</b>, far under one gate's capacity
    /// and under either District's shortfall — so whichever gate is reached first takes all of it
    /// and the second finds nothing left to spend. Which District was served is the answer.
    /// </para>
    /// </remarks>
    [Fact]
    public void Equal_priced_gates_are_bought_in_the_order_they_were_raised()
    {
        var world = new World(400, Funded(101), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };

        Lay(world);

        int block = world.Rules.Roads.BlockTiles;
        int spare = Rows.NoSlot;
        int older = Rows.NoSlot;

        for (int cell = 0; cell < Cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= Cells - PeakCells;
            int standing = 0;

            // A greedy consumer at each end, so both Districts are short and neither link can
            // carry one's shortfall to the other.
            if (cell == 0)
            {
                Raise(world, cell, standing++, Works);
                spare = Raise(world, cell, standing++, Dwelling);
            }

            if (cell == Cells - 1)
            {
                Raise(world, cell, standing++, Works);
                older = Raise(world, cell, standing++, Gate);
            }

            for (int filler = standing; filler < (peak ? Tall : Low); filler++)
            {
                Raise(world, cell, filler, Dwelling);
            }
        }

        world.DestroyBuilding(world.Buildings.Rows.At(spare), Ticks.Zero);

        int newer = Raise(world, 0, Tall, Gate);

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Equal(2, world.Districts.Rows.LiveCount);
        Assert.Equal(spare, newer);
        Assert.True(newer < older, $"the newer gate holds slot {newer} and the older {older}");
        Assert.True(
            world.Buildings.Rows.IdAt(newer) > world.Buildings.Rows.IdAt(older),
            "the gates were not raised in the order this test depends on");

        int burning = DistrictAt(world, new Tiles(((Cells - 1) * block) + 4), Tiles.Zero);
        int western = DistrictAt(world, Tiles.Zero, Tiles.Zero);

        Assert.NotEqual(Rows.NoSlot, burning);
        Assert.NotEqual(western, burning);

        AfterSettling(simulation, day: 0);

        long cheap = Cheap(world);

        // The older gate stands in the east, and it is the one that sold.
        Assert.Equal(101 / cheap, Imported(world, burning));
        Assert.Equal(0, Imported(world, western));
        Assert.Equal((101 / cheap) * cheap, Paid(world, burning));
        Assert.Equal(0, Paid(world, western));
        Assert.Equal(101 % cheap, Purse(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A link bounds what a gate in the next District can deliver, so a wide door does not widen
    /// the road.
    /// </summary>
    /// <remarks>
    /// <b>The one gate stands in the west District, which asks for nothing.</b> Its units are
    /// charged to the east, which is where the shortfall is — there is no haulage charge and no
    /// share for the District that holds the door.
    /// </remarks>
    [Fact]
    public void An_import_travels_the_links_to_a_district_with_no_gate()
    {
        City city = Build(new Shape(Works: true, WestGate: true), WidePort());
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        long imported = Imported(world, city.Burning);

        Assert.Equal(0, Imported(world, city.Western));
        Assert.Equal(0, Paid(world, city.Western));
        Assert.True(imported > 0, "nothing reached the east");
        Assert.Equal(world.Rules.LinkPerCrossing(Power), imported);
        Assert.True(imported < Capacity(world), "the gate was the narrower of the two");
        Assert.Equal(imported * Dear, Paid(world, city.Burning));
        Assert.True(Delivered(world, city.Burning) < Ratio.One, "the east is not short");

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>A city with no Outside Connection imports nothing and spends nothing.</summary>
    [Fact]
    public void A_city_with_no_gate_imports_nothing()
    {
        City city = Build(new Shape(Plant: true, Works: true));
        World world = city.World;
        long purse = Purse(world);
        long issued = Issued(world);

        AfterSettling(city.Simulation, day: 0);

        Assert.Equal(0, Imported(world, city.Burning));
        Assert.Equal(0, Paid(world, city.Burning));
        // The treasury still moves: it funds the plant, pays the works' contract and collects the
        // tariff. None of that leaves the city, so the money supply holds.
        TreasuryFlows flows = city.Simulation.DrainTreasuryFlows();

        Assert.Equal(0, flows.UtilityImport);
        Assert.Equal(purse + flows.Income - flows.Expenditure, Purse(world));
        Assert.Equal(issued, Issued(world));

        // The brownout slice 1 demonstrates, unchanged by a pass that found no door to buy at.
        Assert.True(Delivered(world, city.Burning) > Ratio.Zero);
        Assert.True(Delivered(world, city.Burning) < Ratio.One);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A gate on an edge whose Hinterland states no price for the Utility imports nothing.
    /// </summary>
    /// <remarks>
    /// <b>The same world twice, and the only difference is the <c>prices</c> entry.</b> Under the
    /// shipped file the west edge has no <c>[[hinterland]]</c> at all, so <c>TryEdgeImportPrice</c>
    /// answers nothing for it and the gate is passed over before its District is ever looked up;
    /// <see cref="TwoPrices"/> adds the price and the same door sells. ⚠ <b>The gate is asserted to
    /// stand in a District</b>, because that is the other reason a gate is skipped and a test that
    /// did not separate them would pass on either.
    /// </remarks>
    [Fact]
    public void A_gate_on_an_unpriced_edge_imports_nothing()
    {
        City city = Build(new Shape(Works: true, WestGate: true));
        World world = city.World;
        long purse = Purse(world);
        long issued = Issued(world);

        foreach (HinterlandDefinition hinterland in world.Rules.Hinterlands)
        {
            Assert.NotEqual(MapEdge.West, hinterland.Edge);
        }

        Assert.Equal(city.Western, DistrictAt(world, Tiles.Zero, new Tiles(16)));

        AfterSettling(city.Simulation, day: 0);

        Assert.Equal(0, Imported(world, city.Burning));
        Assert.Equal(0, Imported(world, city.Western));
        Assert.Equal(0, Paid(world, city.Burning));
        Assert.Equal(0, Paid(world, city.Western));
        Assert.Equal(purse, Purse(world));
        Assert.Equal(issued, Issued(world));
        Assert.Equal(0, city.Simulation.DrainTreasuryFlows().UtilityImport);
        Assert.True(
            Delivered(world, city.Burning) < Ratio.One,
            "the east is not short, so there was nothing for the gate to have sold");

        // The same door, priced. Without this the assertions above hold on a fixture whose gate
        // never stood anywhere a price could have reached it.
        City priced = Build(new Shape(Works: true, WestGate: true), TwoPrices());

        AfterSettling(priced.Simulation, day: 0);

        Assert.True(Imported(priced.World, priced.Burning) > 0, "the priced gate sold nothing");

        city.Simulation.CheckEndOfRun();
        priced.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A gate standing in no District imports nothing, however well priced its edge is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>There is no node for the units to arrive at.</b> A gate is a source attached to the
    /// District holding its Lot, and carriage inside the city is the links' business — so a door on
    /// ground the watershed has not claimed is a door onto nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>The window is real and it is a Day wide.</b> The Cell-to-District index covers the Cells
    /// the last evaluation built, a Cell holding no other Building joins no District until the next
    /// one, and the settle runs at the top of the Day Tick while the re-evaluation is in phase 6 of
    /// it — so the first settle after a gate opens an empty Cell passes it over. The edge is priced
    /// and the city is short, which is what leaves the District lookup as the only reason.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_gate_in_no_district_imports_nothing()
    {
        City city = Build(new Shape(Works: true, StrandedGate: true));
        World world = city.World;
        long purse = Purse(world);
        long issued = Issued(world);

        Assert.True(Cheap(world) > 0, "the south edge no longer prices Power");
        Assert.Equal(Rows.NoSlot, DistrictAt(world, new Tiles(Stranded), Tiles.Zero));

        AfterSettling(city.Simulation, day: 0);

        Assert.Equal(0, Imported(world, city.Burning));
        Assert.Equal(0, Imported(world, city.Western));
        Assert.Equal(0, Paid(world, city.Burning));
        Assert.Equal(0, Paid(world, city.Western));
        Assert.Equal(purse, Purse(world));
        Assert.Equal(issued, Issued(world));
        Assert.Equal(0, city.Simulation.DrainTreasuryFlows().UtilityImport);
        Assert.True(
            Delivered(world, city.Burning) < Ratio.One,
            "the east is not short, so there was nothing for the gate to have sold");

        city.Simulation.CheckEndOfRun();
    }

    // ---- what the treasury can pay for ---------------------------------------------------------

    /// <summary>
    /// The treasury buys what it can afford at the gate's price and the rest of the shortfall goes
    /// unserved, with nothing owed afterwards.
    /// </summary>
    /// <remarks>
    /// <b>1,001 rather than a round figure</b>, so the division is visibly a floor: the city buys
    /// 500 units at two apiece and keeps the odd unit it cannot spend. An empty treasury is the
    /// same rule at zero — <b>no credit</b>, because the Outside gives nothing away.
    /// </remarks>
    [Theory]
    [InlineData(1001)]
    [InlineData(0)]
    public void The_treasury_buys_what_it_can_afford_and_borrows_nothing(long balance)
    {
        City city = Build(new Shape(Works: true, SouthGates: 1), Funded(balance));
        World world = city.World;

        Assert.Equal(balance, Purse(world));

        AfterSettling(city.Simulation, day: 0);

        long cheap = Cheap(world);
        long affordable = balance / cheap;

        Assert.True(affordable < Capacity(world), "the purse is not the binding constraint");
        Assert.Equal(affordable, Imported(world, city.Burning));
        Assert.Equal(affordable * cheap, Paid(world, city.Burning));
        Assert.Equal(balance % cheap, Purse(world));
        Assert.True(Delivered(world, city.Burning) < Ratio.One, "the shortfall was covered");

        // A second Day on an empty purse buys nothing and leaves no debt behind.
        AfterSettling(city.Simulation, day: 1);

        Assert.Equal(0, Imported(world, city.Burning));
        Assert.Equal(0, Paid(world, city.Burning));
        Assert.Equal(balance % cheap, Purse(world));

        city.Simulation.CheckEndOfRun();
    }

    // ---- the equivalences ----------------------------------------------------------------------

    [Fact]
    public void A_replayed_run_reaches_the_same_state()
    {
        ulong Run()
        {
            City city = Build(new Shape(Works: true, SouthGates: 1, WestGate: true), TwoPrices());

            AfterSettling(city.Simulation, day: 1);
            city.Simulation.CheckEndOfRun();

            return city.World.HashState();
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void The_imports_survive_save_and_reload()
    {
        City city = Build(new Shape(Works: true, SouthGates: 1, WestGate: true), TwoPrices());

        AfterSettling(city.Simulation, day: 0);

        Assert.True(Imported(city.World, city.Burning) > 0, "nothing was imported to save");

        var file = new MemorySave();
        SaveFile.Write(city.World, 1, file);
        World reloaded = SaveFile.Read(file, city.World.Rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = true };

        Assert.Equal(SaveHeader.Current, header.FormatVersion);
        Assert.Equal(city.World.HashState(), reloaded.HashState());
        Assert.Equal(Imported(city.World, city.Burning), Imported(reloaded, city.Burning));
        Assert.Equal(Paid(city.World, city.Burning), Paid(reloaded, city.Burning));
        Assert.Equal(Purse(city.World), Purse(reloaded));

        for (int tick = 0; tick <= Ticks.PerDay; tick++)
        {
            city.Simulation.Step(TickInput.Empty);
            resumed.Step(TickInput.Empty);
            Assert.Equal(city.World.HashState(), reloaded.HashState());
        }

        city.Simulation.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    [Fact]
    public void The_thread_count_does_not_change_the_state()
    {
        ulong Run(int workers)
        {
            City city = Build(new Shape(Works: true, SouthGates: 1, WestGate: true), TwoPrices());
            city.Simulation.RouteWorkerCount = workers;

            AfterSettling(city.Simulation, day: 1);
            city.Simulation.CheckEndOfRun();

            return city.World.HashState();
        }

        Assert.Equal(Run(1), Run(4));
    }
}
