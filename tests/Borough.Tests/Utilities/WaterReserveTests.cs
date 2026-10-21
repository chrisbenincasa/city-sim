using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Utilities;

/// <summary>
/// Utilities slice 2 — Water kept in tower Bins between Days, and what the settle does with it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every world here is <c>rulesets/watered.toml</c> on <c>PowerDeliveryTests</c>' ridge</b>: eight
/// blocks in a line with tall Cells at each end, which the watershed reads as two Districts joined by
/// a single crossing Segment. The pump stands in the west District and the dwellings that draw in the
/// east, so nothing the east consumes can have come from anywhere but over that link or out of a
/// tower.
/// </para>
/// <para>
/// <b>Every Cell is filled to the same Building count whatever a case asks for</b>, so the watershed
/// is the same two Districts in all of them and a pump, a tower and an empty dwelling are
/// interchangeable as far as the boundary is concerned.
/// </para>
/// <para>
/// <b>The quantities are computed from the Ruleset and the road graph rather than written down.</b> A
/// District's road loss is its credited Tiles times <c>loss_per_road_tile</c>, which a test reads out
/// of its own <see cref="DistrictAdjacency"/> — so moving a block size or a rate moves the
/// expectation with it instead of breaking the test.
/// </para>
/// </remarks>
public sealed class WaterReserveTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(11);

    /// <summary>The delivered Utility, and <c>watered.toml</c>'s first <c>[[resource]]</c>.</summary>
    private static readonly ResourceId Water = new(1);

    // Kinds are one-based: zero is the kind no Ruleset declares.
    private const byte Dwelling = 1;
    private const byte Pump = 2;
    private const byte Tower = 3;

    private const int Cells = 8;
    private const int PeakCells = 2;
    private const int Tall = 8;
    private const int Short = 3;

    /// <summary>Households put in each drawing dwelling, and so its occupancy.</summary>
    private const int Occupants = 2;

    /// <summary><c>[[building]] tower</c>'s declared Bin ceiling.</summary>
    private const long TowerCapacity = 65536;

    /// <summary>What one pump feeds the network per firing — <c>pump_water</c>'s output amount.</summary>
    private const long PumpPerFiring = 16;

    /// <summary>
    /// What one pump feeds the network in a Day — <c>rate 8</c> firings of
    /// <see cref="PumpPerFiring"/>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A Day's figure is this or one firing short of it</b>, because a Rule Instance is armed at
    /// a phase of its own and the Day boundary cuts its period wherever it falls.
    /// </remarks>
    private const long PumpPerDay = Ticks.PerDay / 8 * PumpPerFiring;

    /// <summary>What one occupant draws in a Day — <c>rate 32</c>, <c>amount 4</c>.</summary>
    private const long DrawPerOccupantPerDay = Ticks.PerDay / 32 * 4;

    /// <summary>What one crossing Segment carries in a Day.</summary>
    private const long LinkPerCrossing = 2560;

    /// <summary>
    /// A tower ceiling whose District total overflows <see cref="long"/>, which is the regime the
    /// proportional split has to divide by an exact basis rather than a saturated one.
    /// </summary>
    /// <remarks>
    /// <c>adr/0065</c> permits any positive <see cref="long"/> ceiling and the loader refuses nothing
    /// above one, so two of these in a District is a legal Ruleset. Each one is past
    /// <see cref="UtilityFlowTable.Bound"/> on its own, and two of them overflow <see cref="long"/>
    /// between them.
    /// </remarks>
    private const long VastCapacity = 5_000_000_000_000_000_000;

    private static Ruleset Rules() => Rules(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "watered.toml")));

    private static Ruleset Rules(string toml)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "watered.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    /// <summary><c>watered.toml</c> with the tower ceiling moved and nothing else touched.</summary>
    private static Ruleset Towering(long capacity)
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "watered.toml"));
        string moved = toml.Replace(
            $"capacity = {TowerCapacity}",
            $"capacity = {capacity}",
            StringComparison.Ordinal);

        Assert.NotEqual(toml, moved);

        return Rules(moved);
    }

    /// <summary>What a world is asked to be, so one builder serves every case below.</summary>
    /// <param name="Pumping">Put a pump in the west District. Without one the city makes no water.</param>
    /// <param name="Homes">Drawing dwellings in the east District, each holding <see cref="Occupants"/>.</param>
    /// <param name="WestTowers">Tower Buildings beside the pump.</param>
    /// <param name="EastTowers">Tower Buildings beside the dwellings.</param>
    private readonly record struct Shape(
        bool Pumping = true,
        int Homes = 1,
        int WestTowers = 0,
        int EastTowers = 0);

    private sealed class City
    {
        public required World World { get; init; }

        public required Simulation Simulation { get; init; }

        /// <summary>The District the pump stands in.</summary>
        public required int West { get; init; }

        /// <summary>The District the drawing dwellings stand in.</summary>
        public required int East { get; init; }

        /// <summary>The pump's Building slot, or <see cref="Rows.NoSlot"/>.</summary>
        public required int Pump { get; init; }

        /// <summary>The tower Buildings in each District, in the order they went up.</summary>
        public required int[] WestTowers { get; init; }

        public required int[] EastTowers { get; init; }
    }

    private static City Build(Shape shape = default, Ruleset? rules = null)
    {
        var world = new World(400, rules ?? Rules(), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };

        Lay(world);

        int pump = Rows.NoSlot;
        var westTowers = new List<int>();
        var eastTowers = new List<int>();
        int home = Rows.NoSlot;

        for (int cell = 0; cell < Cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= Cells - PeakCells;
            int standing = 0;

            if (cell == 0)
            {
                if (shape.Pumping)
                {
                    pump = Raise(world, cell, standing++, Pump);
                }

                for (int tower = 0; tower < shape.WestTowers; tower++)
                {
                    westTowers.Add(Raise(world, cell, standing++, Tower));
                }
            }

            if (cell == Cells - 1)
            {
                for (int dwelling = 0; dwelling < shape.Homes; dwelling++)
                {
                    home = Raise(world, cell, standing++, Dwelling);

                    for (int occupant = 0; occupant < Occupants; occupant++)
                    {
                        world.CreateHousehold(world.Buildings.Rows.At(home), lifeStage: 1);
                    }
                }

                for (int tower = 0; tower < shape.EastTowers; tower++)
                {
                    eastTowers.Add(Raise(world, cell, standing++, Tower));
                }
            }

            // Every Cell reaches the same count in every shape, which is what keeps the watershed
            // out of the variables a case is changing.
            for (int filler = standing; filler < (peak ? Tall : Short); filler++)
            {
                Raise(world, cell, filler, Dwelling);
            }
        }

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Equal(2, world.Districts.Rows.LiveCount);

        int west = DistrictAt(world, new Tiles(4), Tiles.Zero);
        int east = DistrictAt(world, new Tiles(((Cells - 1) * world.Rules.Roads.BlockTiles) + 4), Tiles.Zero);

        Assert.NotEqual(Rows.NoSlot, west);
        Assert.NotEqual(west, east);

        if (home != Rows.NoSlot)
        {
            Assert.Equal(east, DistrictOf(world, home));
        }

        if (pump != Rows.NoSlot)
        {
            Assert.Equal(west, DistrictOf(world, pump));
        }

        return new City
        {
            World = world,
            Simulation = simulation,
            West = west,
            East = east,
            Pump = pump,
            WestTowers = [.. westTowers],
            EastTowers = [.. eastTowers],
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

    /// <inheritdoc cref="PowerDeliveryTests"/>
    private static int Raise(World world, int cell, int index, byte kind, Ticks? now = null)
    {
        int block = world.Rules.Roads.BlockTiles;

        Handle<Lot> lot = world.Lots.Create(
            new Tiles((cell * block) + 4 + (index * 2)), Tiles.Zero, zone: 1);

        return world.Buildings.Rows.Resolve(
            world.CreateBuilding(lot, kind, now ?? Ticks.Zero, Key));
    }

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

    /// <summary>The network row for one District's Water, which every world here has.</summary>
    private static int Flow(World world, int districtSlot)
    {
        int row = world.UtilityFlowOfPool(world.Markets.Row(world, districtSlot, Water));

        Assert.NotEqual(Rows.NoSlot, row);

        return row;
    }

    private static Ratio Delivered(World world, int districtSlot) =>
        world.UtilityFlows.Delivered[Flow(world, districtSlot)];

    /// <summary>
    /// What a District's roads lose in a Day — its credited Tiles times
    /// <c>loss_per_road_tile</c>, taken off the same adjacency the settle derives.
    /// </summary>
    private static long Owed(World world, int districtSlot)
    {
        var adjacency = new DistrictAdjacency();

        adjacency.Compute(world);

        return adjacency.RoadTilesOf(districtSlot) * world.Rules.LossPerRoadTile(Water);
    }

    private static void StepTo(Simulation simulation, long tick)
    {
        while ((long)simulation.World.Tick.Raw < tick)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>
    /// Steps through the settle that closes <paramref name="day"/> and no further, so the Day's
    /// production and demand are readable and the Day it governs has run exactly one Tick.
    /// </summary>
    private static void AfterSettling(Simulation simulation, int day) =>
        StepTo(simulation, (((long)day + 1) * Ticks.PerDay) + 1);

    /// <summary>Applies one command on the next Tick and refuses to proceed if the world refused it.</summary>
    private static void Issue(Simulation simulation, Command command)
    {
        int refused = simulation.CommandsRefused;

        simulation.Step(new TickInput([command], rulesetHash: 0));

        Assert.Equal(refused, simulation.CommandsRefused);
    }

    private static void Demolish(City city, int buildingSlot)
    {
        int lot = city.World.Lots.Rows.Resolve(city.World.Buildings.Lot[buildingSlot]);

        Issue(city.Simulation, Command.Demolish(
            city.World.Lots.East[lot], Tiles.Zero, DemolishTarget.Building));
    }

    /// <summary>The Bin slot of one tower's Water store.</summary>
    private static int Store(World world, int towerSlot)
    {
        int bin = world.FindBin(towerSlot, Water);

        Assert.NotEqual(Rows.NoSlot, bin);
        Assert.Equal(BinOwnerKind.Building, world.Bins.OwnerKind[bin]);

        return bin;
    }

    private static long Level(World world, int towerSlot) =>
        world.Bins.LevelAt(Store(world, towerSlot));

    /// <summary>Puts a tower's store at <paramref name="units"/>, whichever side it is on.</summary>
    private static void Fill(World world, int towerSlot, long units)
    {
        int bin = Store(world, towerSlot);
        long standing = world.Bins.LevelAt(bin);

        if (units > standing)
        {
            world.Deposit(world.Bins.Rows.At(bin), units - standing, world.Tick);
        }
        else if (units < standing)
        {
            world.Withdraw(world.Bins.Rows.At(bin), standing - units, world.Tick);
        }

        Assert.Equal(units, world.Bins.LevelAt(bin));
    }

    /// <summary>The towers' Bin slots in ascending slot order, which is the order a split uses.</summary>
    private static int[] Stores(World world, int[] towers)
    {
        int[] bins = [.. towers.Select(tower => Store(world, tower))];

        Array.Sort(bins);

        return bins;
    }

    /// <summary>
    /// What a proportional split of <paramref name="total"/> over <paramref name="basis"/> places in
    /// each Bin: the floor share, then one unit each in ascending slot order until nothing is left.
    /// </summary>
    private static long[] Split(long total, long[] basis)
    {
        // Int128, because the basis total overflows a long once the Bin ceilings are large enough —
        // which is the whole point of the two cases that use VastCapacity.
        Int128 whole = basis.Aggregate(Int128.Zero, (running, part) => running + part);
        long[] shares = [.. basis.Select(part => IntegerMath.MulDivFloor(total, part, whole))];
        long remainder = total - shares.Sum();

        for (int at = 0; at < shares.Length && remainder > 0; at++)
        {
            if (basis[at] > shares[at])
            {
                shares[at]++;
                remainder--;
            }
        }

        Assert.Equal(0, remainder);

        return shares;
    }

    // ---- filling and emptying ------------------------------------------------------------------

    /// <summary>
    /// A Day's surplus crosses the link into the far District's tower, up to what the link has left
    /// after the consumers have been served.
    /// </summary>
    [Fact]
    public void A_surplus_refills_a_reserve_over_the_link()
    {
        City city = Build(new Shape(EastTowers: 1));
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        int west = Flow(world, city.West);
        int east = Flow(world, city.East);
        long demand = Occupants * DrawPerOccupantPerDay;
        long owed = Owed(world, city.East);

        // A Day's pumping, less at most the one firing whose phase fell outside it.
        Assert.InRange(world.UtilityFlows.Supplied[west], PumpPerDay - PumpPerFiring, PumpPerDay);
        Assert.Equal(demand, world.UtilityFlows.Asked[east]);
        Assert.Equal(demand, world.UtilityFlows.Received[east]);
        Assert.Equal(Ratio.One, Delivered(world, city.East));

        // The consumers' draw and the road loss went over the link first; the refill got what was
        // left of that one crossing's Day.
        long filled = LinkPerCrossing - demand - owed;

        Assert.True(filled > 0, "the link carries nothing past the consumers in this fixture");
        Assert.Equal(filled, world.UtilityFlows.ToReserve[east]);
        Assert.Equal(filled, Level(world, city.EastTowers[0]));
        Assert.Equal(0, world.UtilityFlows.FromReserve[east]);

        // The producing District holds no tower, so nothing of its own was stored.
        Assert.Equal(0, world.UtilityFlows.ToReserve[west]);
        Assert.Equal(0, world.UtilityFlows.FromReserve[west]);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A reserve carries the city through an outage at full delivery, browns it out when it runs dry,
    /// and fills again when the pump comes back.
    /// </summary>
    /// <remarks>
    /// <b>The outage, the brownout and the recovery belong in one test.</b> A reserve that merely
    /// drains says nothing about what happens when it is empty, and a delivery that returns to the
    /// fraction it left says more than one that is merely above zero.
    /// </remarks>
    [Fact]
    public void A_reserve_covers_an_outage_until_it_is_empty_and_refills_after_it()
    {
        City city = Build(new Shape(EastTowers: 1));
        World world = city.World;
        int tower = city.EastTowers[0];

        // The pump goes on the last Tick of the Day it fed, so the outage is a whole Day long from
        // the first settle on. ⚠ A pump demolished after that settle fires once more in the Day the
        // settle opened, which leaves its own District short of its road loss — and a neighbor's
        // reserve covering 176 units of somebody else's leak is a different test from this one.
        StepTo(city.Simulation, Ticks.PerDay - 1);
        Demolish(city, city.Pump);

        AfterSettling(city.Simulation, day: 0);

        long demand = Occupants * DrawPerOccupantPerDay;
        long need = demand + Owed(world, city.East);

        Assert.True(Level(world, tower) > 0, "the reserve never filled");

        // Two Days' worth, so the drain is watchable inside a test rather than over a hundred Days.
        Fill(world, tower, 2 * need);

        AfterSettling(city.Simulation, day: 1);

        int east = Flow(world, city.East);

        Assert.Equal(0, world.UtilityFlows.Supplied[east]);
        Assert.Equal(need, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(demand, world.UtilityFlows.Received[east]);
        Assert.Equal(Ratio.One, Delivered(world, city.East));
        Assert.Equal(need, Level(world, tower));

        AfterSettling(city.Simulation, day: 2);

        Assert.Equal(need, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(Ratio.One, Delivered(world, city.East));
        Assert.Equal(0, Level(world, tower));

        AfterSettling(city.Simulation, day: 3);

        // Dry, and the loss is still owed before anybody is served, so the District is delivered
        // nothing rather than a fraction.
        Assert.Equal(0, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(0, world.UtilityFlows.Received[east]);
        Assert.Equal(Ratio.Zero, Delivered(world, city.East));
        Assert.Equal(0, Level(world, tower));

        // A new pump in the west, armed from the Tick it is raised on.
        Raise(world, 0, Tall, Pump, world.Tick);

        AfterSettling(city.Simulation, day: 4);

        Assert.Equal(Ratio.One, Delivered(world, city.East));
        Assert.True(world.UtilityFlows.ToReserve[east] > 0, "the reserve did not refill");
        Assert.Equal(world.UtilityFlows.ToReserve[east], Level(world, tower));

        city.Simulation.CheckEndOfRun();
    }

    // ---- the order of the claims ---------------------------------------------------------------

    /// <summary>
    /// A District empties its own towers before it touches a neighbor's, so a city with reserve at
    /// both ends moves nothing over the link.
    /// </summary>
    [Fact]
    public void A_district_drains_its_own_reserve_before_a_neighbors()
    {
        City city = Build(new Shape(Pumping: false, WestTowers: 1, EastTowers: 1));
        World world = city.World;

        Fill(world, city.WestTowers[0], TowerCapacity);
        Fill(world, city.EastTowers[0], TowerCapacity);

        AfterSettling(city.Simulation, day: 0);

        int west = Flow(world, city.West);
        int east = Flow(world, city.East);
        long need = (Occupants * DrawPerOccupantPerDay) + Owed(world, city.East);
        long westOwed = Owed(world, city.West);

        Assert.True(westOwed > 0, "the west District is credited no road");

        Assert.Equal(0, world.UtilityFlows.Supplied[east]);
        Assert.Equal(need, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(Ratio.One, Delivered(world, city.East));
        Assert.Equal(TowerCapacity - need, Level(world, city.EastTowers[0]));

        // The west gave up nothing, though its roads are credited a loss. The east was covered at
        // home so pass 2 had no deficit to export into, and a full tower takes no refill for the
        // roads to tax.
        Assert.Equal(0, world.UtilityFlows.FromReserve[west]);
        Assert.Equal(0, world.UtilityFlows.ToReserve[west]);
        Assert.Equal(TowerCapacity, Level(world, city.WestTowers[0]));

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A District with no reserve of its own is served out of a neighbor's, and the link is what
    /// bounds how much of it arrives.
    /// </summary>
    /// <remarks>
    /// <b>Six drawing dwellings, so the demand is deliberately past one crossing's Day.</b> A city
    /// whose deficit fits under the link would show the export and never the bound.
    /// </remarks>
    [Fact]
    public void A_neighbors_reserve_exports_over_the_link_up_to_its_capacity()
    {
        City city = Build(new Shape(Pumping: false, Homes: 6, WestTowers: 1));
        World world = city.World;

        Fill(world, city.WestTowers[0], TowerCapacity);

        AfterSettling(city.Simulation, day: 0);

        int west = Flow(world, city.West);
        int east = Flow(world, city.East);
        long demand = 6 * Occupants * DrawPerOccupantPerDay;
        long owed = Owed(world, city.East);
        long westOwed = Owed(world, city.West);

        Assert.Equal(demand, world.UtilityFlows.Asked[east]);
        Assert.True(demand + owed > LinkPerCrossing, "the fixture's demand fits under one crossing");

        // One crossing's Day, all of it out of the west's towers, and the road loss comes off it
        // before anybody is served.
        Assert.Equal(LinkPerCrossing, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(LinkPerCrossing - owed, world.UtilityFlows.Received[east]);
        Assert.True(Delivered(world, city.East) < Ratio.One, "the east is not short");
        Assert.True(Delivered(world, city.East) > Ratio.Zero, "the east got nothing");

        // Nothing left the west's towers but the export. It has no consumer and no plant, so its
        // own roads are paid out of a refill, and a full tower takes none.
        Assert.True(westOwed > 0, "the west District is credited no road");
        Assert.Equal(0, world.UtilityFlows.FromReserve[west]);
        Assert.Equal(TowerCapacity - LinkPerCrossing, Level(world, city.WestTowers[0]));

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Production is spent before any reserve, so a District a plant can reach keeps its towers full.
    /// </summary>
    [Fact]
    public void Production_is_spent_before_a_reserve()
    {
        City city = Build(new Shape(EastTowers: 1));
        World world = city.World;

        Fill(world, city.EastTowers[0], TowerCapacity);

        AfterSettling(city.Simulation, day: 0);

        int east = Flow(world, city.East);
        long demand = Occupants * DrawPerOccupantPerDay;

        Assert.Equal(demand, world.UtilityFlows.Received[east]);
        Assert.Equal(Ratio.One, Delivered(world, city.East));

        // Nothing came out of the tower and nothing went in: it was already full.
        Assert.Equal(0, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(0, world.UtilityFlows.ToReserve[east]);
        Assert.Equal(TowerCapacity, Level(world, city.EastTowers[0]));

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A District with no consumer and no plant loses nothing while its towers stand full, however
    /// much road it is credited.
    /// </summary>
    /// <remarks>
    /// <b>Leakage is a toll on flow.</b> Stored water does not decay, so a District moving no water
    /// pays nothing — and a full tower is a District that can accept no refill to be taxed on. The
    /// west is credited road and holds the whole reserve, which is what makes this an assertion
    /// rather than a tautology.
    /// </remarks>
    [Fact]
    public void A_full_reserve_with_no_demand_and_no_production_loses_nothing()
    {
        City city = Build(new Shape(Pumping: false, Homes: 0, WestTowers: 1));
        World world = city.World;

        Fill(world, city.WestTowers[0], TowerCapacity);

        AfterSettling(city.Simulation, day: 0);

        int west = Flow(world, city.West);
        int east = Flow(world, city.East);

        Assert.True(Owed(world, city.West) > 0, "the west District is credited no road");
        Assert.True(Owed(world, city.East) > 0, "the east District is credited no road");

        Assert.Equal(0, world.UtilityFlows.Asked[west]);
        Assert.Equal(0, world.UtilityFlows.Supplied[west]);
        Assert.Equal(0, world.UtilityFlows.FromReserve[west]);
        Assert.Equal(0, world.UtilityFlows.ToReserve[west]);
        Assert.Equal(TowerCapacity, Level(world, city.WestTowers[0]));

        // Nobody draws, so the fraction is whole: a District with no consumers reports no shortfall.
        Assert.Equal(Ratio.One, Delivered(world, city.West));

        // The east holds no tower and takes nothing either, which is slice 1's behavior unchanged.
        Assert.Equal(0, world.UtilityFlows.FromReserve[east]);
        Assert.Equal(0, world.UtilityFlows.Received[east]);

        AfterSettling(city.Simulation, day: 1);

        // A week of this would read the same. The level is what it was raised to.
        Assert.Equal(0, world.UtilityFlows.FromReserve[west]);
        Assert.Equal(TowerCapacity, Level(world, city.WestTowers[0]));

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A District with no consumer and no plant pays its road loss out of the refill it takes, and
    /// only the remainder reaches its towers.
    /// </summary>
    /// <remarks>
    /// <b>The east draws nothing here</b> — no dwelling in it holds a Household — so every unit that
    /// crosses the link is refill, and what lands in the tower is a crossing's Day less the east's
    /// own road loss. Compare
    /// <see cref="A_surplus_refills_a_reserve_over_the_link"/>, where consumers are served out of the
    /// same crossing first.
    /// </remarks>
    [Fact]
    public void A_refilling_reserve_pays_its_road_loss_out_of_the_refill()
    {
        City city = Build(new Shape(Homes: 0, EastTowers: 1));
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        int east = Flow(world, city.East);
        long owed = Owed(world, city.East);
        long filled = LinkPerCrossing - owed;

        Assert.True(owed > 0, "the east District is credited no road");
        Assert.Equal(0, world.UtilityFlows.Asked[east]);

        // One crossing's Day arrived, the roads took theirs off the top, and the tower kept the rest.
        Assert.Equal(filled, world.UtilityFlows.ToReserve[east]);
        Assert.Equal(filled, Level(world, city.EastTowers[0]));
        Assert.Equal(0, world.UtilityFlows.FromReserve[east]);

        AfterSettling(city.Simulation, day: 1);

        // The toll is a Day's road length again, not a one-off on the first refill.
        Assert.Equal(filled, world.UtilityFlows.ToReserve[east]);
        Assert.Equal(2 * filled, Level(world, city.EastTowers[0]));

        city.Simulation.CheckEndOfRun();
    }

    // ---- several towers ------------------------------------------------------------------------

    /// <summary>
    /// A District's drain comes out of its towers in proportion to what each one holds, with the
    /// remainder landing in ascending Bin slot order.
    /// </summary>
    /// <remarks>
    /// <b>The three levels are coprime enough to leave a remainder</b>, which is the half of the rule
    /// a proportional split with clean numbers would never exercise.
    /// </remarks>
    [Fact]
    public void Several_towers_drain_in_proportion_with_the_remainder_in_slot_order()
    {
        City city = Build(new Shape(Pumping: false, EastTowers: 3));
        World world = city.World;
        long[] standing = [1000, 2000, 3001];

        int[] bins = Stores(world, city.EastTowers);

        for (int at = 0; at < bins.Length; at++)
        {
            world.Deposit(world.Bins.Rows.At(bins[at]), standing[at], world.Tick);
        }

        AfterSettling(city.Simulation, day: 0);

        int east = Flow(world, city.East);
        long total = world.UtilityFlows.FromReserve[east];

        Assert.Equal((Occupants * DrawPerOccupantPerDay) + Owed(world, city.East), total);
        Assert.True(total < standing.Sum(), "the whole reserve drained, so no share was partial");

        long[] expected = Split(total, standing);

        for (int at = 0; at < bins.Length; at++)
        {
            Assert.Equal(standing[at] - expected[at], world.Bins.LevelAt(bins[at]));
        }

        // The split placed the settle's figure and no more: what left the Bins is what was served.
        Assert.Equal(total, standing.Sum() - bins.Sum(bin => world.Bins.LevelAt(bin)));

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A District's refill goes into its towers in proportion to the room each one has, with the
    /// remainder landing in ascending Bin slot order.
    /// </summary>
    [Fact]
    public void Several_towers_fill_in_proportion_with_the_remainder_in_slot_order()
    {
        City city = Build(new Shape(Homes: 0, WestTowers: 3));
        World world = city.World;
        long[] room = [1000, 2000, 3001];

        int[] bins = Stores(world, city.WestTowers);

        for (int at = 0; at < bins.Length; at++)
        {
            world.Deposit(
                world.Bins.Rows.At(bins[at]), TowerCapacity - room[at], world.Tick);
        }

        AfterSettling(city.Simulation, day: 0);

        int west = Flow(world, city.West);
        long total = world.UtilityFlows.ToReserve[west];

        Assert.InRange(world.UtilityFlows.Supplied[west], PumpPerDay - PumpPerFiring, PumpPerDay);
        Assert.Equal(world.UtilityFlows.Supplied[west] - Owed(world, city.West), total);
        Assert.True(total < room.Sum(), "the towers filled up, so no share was partial");

        long[] expected = Split(total, room);

        for (int at = 0; at < bins.Length; at++)
        {
            Assert.Equal(TowerCapacity - room[at] + expected[at], world.Bins.LevelAt(bins[at]));
        }

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A tower's level stays inside its own Bin over a run that fills it to the ceiling and empties
    /// it again.
    /// </summary>
    /// <remarks>
    /// <b>One tower opens one unit short of its ceiling</b>, so its first refill has to stop at the
    /// capacity rather than at what the network offered — which is the case
    /// <see cref="Borough.Core.Invariants.Invariant.BinLevelIsWithinCapacity"/> would catch at the
    /// deposit and the one a split rounding upwards reaches.
    /// </remarks>
    [Fact]
    public void Tower_levels_stay_within_their_bins()
    {
        City city = Build(new Shape(WestTowers: 1, EastTowers: 2));
        World world = city.World;
        int brimming = Stores(world, city.EastTowers)[0];

        world.Deposit(world.Bins.Rows.At(brimming), TowerCapacity - 1, world.Tick);

        // The pump's own District is given no room, so the Day's surplus has to cross the link and
        // reach the tower that is one unit short of its ceiling.
        Fill(world, city.WestTowers[0], TowerCapacity);

        for (int day = 0; day < 4; day++)
        {
            AfterSettling(city.Simulation, day);
            AssertTowersAreBounded(world, city);
        }

        Assert.Equal(TowerCapacity, world.Bins.LevelAt(brimming));

        Demolish(city, city.Pump);

        for (int day = 4; day < 8; day++)
        {
            AfterSettling(city.Simulation, day);
            AssertTowersAreBounded(world, city);
        }

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Towers whose levels overflow <see cref="long"/> between them still drain in proportion, rather
    /// than emptying the first one and leaving the rest alone.
    /// </summary>
    /// <remarks>
    /// <b>The split's divisor is the District's basis total, and the figure the settle works in
    /// saturates at <see cref="UtilityFlowTable.Bound"/>.</b> Two equal towers past that bound would
    /// divide by the bound, make each share the whole drain, and hand it all to the lower slot — so
    /// this is the case that separates an exact divisor from a saturated one. The levels are
    /// <em>equal</em> on purpose: an even split is the one answer no rounding rule can reach by
    /// accident.
    /// </remarks>
    [Fact]
    public void Towers_past_the_counters_bound_still_drain_in_proportion()
    {
        City city = Build(new Shape(Pumping: false, EastTowers: 2), Towering(VastCapacity));
        World world = city.World;
        int[] bins = Stores(world, city.EastTowers);

        foreach (int bin in bins)
        {
            world.Deposit(world.Bins.Rows.At(bin), VastCapacity, world.Tick);
        }

        long[] standing = [.. bins.Select(_ => VastCapacity)];

        Assert.True(
            standing.Aggregate(Int128.Zero, (running, part) => running + part) > long.MaxValue,
            "the fixture's towers fit in a long between them, so they test nothing");

        AfterSettling(city.Simulation, day: 0);

        long total = world.UtilityFlows.FromReserve[Flow(world, city.East)];

        Assert.Equal((Occupants * DrawPerOccupantPerDay) + Owed(world, city.East), total);

        long[] expected = Split(total, standing);

        // Half each, and the odd unit to the lower slot. The saturated divisor gave the lower slot
        // the whole drain and the higher slot nothing.
        Assert.NotEqual(total, expected[0]);

        for (int at = 0; at < bins.Length; at++)
        {
            Assert.Equal(VastCapacity - expected[at], world.Bins.LevelAt(bins[at]));
        }

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// <see cref="Towers_past_the_counters_bound_still_drain_in_proportion"/> in the filling
    /// direction, where the basis is the room each tower has rather than what it holds.
    /// </summary>
    [Fact]
    public void Towers_past_the_counters_bound_still_fill_in_proportion()
    {
        City city = Build(new Shape(Homes: 0, WestTowers: 2), Towering(VastCapacity));
        World world = city.World;
        int[] bins = Stores(world, city.WestTowers);

        // Empty, so each tower's room is its whole ceiling and the two overflow a long between them.
        long[] room = [.. bins.Select(bin => world.Bins.SpaceAt(bin))];

        Assert.Equal([VastCapacity, VastCapacity], room);

        AfterSettling(city.Simulation, day: 0);

        long total = world.UtilityFlows.ToReserve[Flow(world, city.West)];

        Assert.True(total > 0, "the Day's production reached no tower");

        long[] expected = Split(total, room);

        Assert.NotEqual(total, expected[0]);

        for (int at = 0; at < bins.Length; at++)
        {
            Assert.Equal(expected[at], world.Bins.LevelAt(bins[at]));
        }

        city.Simulation.CheckEndOfRun();
    }

    private static void AssertTowersAreBounded(World world, City city)
    {
        foreach (int tower in city.WestTowers.Concat(city.EastTowers))
        {
            int bin = Store(world, tower);

            Assert.InRange(world.Bins.LevelAt(bin), 0, world.Bins.Capacity[bin]);
        }
    }

    // ---- the equivalences ----------------------------------------------------------------------

    /// <summary>
    /// A save taken part way through a drain resumes into the same settles, Tick for Tick.
    /// </summary>
    /// <remarks>
    /// <b>Mid-drain rather than at rest</b>, which is what makes it a test of the reserve: a tower
    /// holding a partial level, a network with a Day of history behind it and no plant standing is
    /// the state a reload has to reproduce for the next settle to agree.
    /// </remarks>
    [Fact]
    public void A_save_taken_mid_drain_resumes_identically()
    {
        City city = Build(new Shape(EastTowers: 2));
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        Fill(world, city.EastTowers[0], 5000);
        Fill(world, city.EastTowers[1], 3001);
        Demolish(city, city.Pump);

        AfterSettling(city.Simulation, day: 1);

        int east = Flow(world, city.East);

        Assert.True(world.UtilityFlows.FromReserve[east] > 0, "nothing was drawn from the reserve");
        Assert.True(Level(world, city.EastTowers[0]) > 0, "the reserve is already empty");

        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        World reloaded = SaveFile.Read(file, world.Rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = true };

        Assert.Equal(SaveHeader.Current, header.FormatVersion);
        Assert.Equal(world.HashState(), reloaded.HashState());
        Assert.Equal(
            world.UtilityFlows.FromReserve[east], reloaded.UtilityFlows.FromReserve[east]);

        for (int tick = 0; tick <= Ticks.PerDay; tick++)
        {
            city.Simulation.Step(TickInput.Empty);
            resumed.Step(TickInput.Empty);
            Assert.Equal(world.HashState(), reloaded.HashState());
        }

        // The settle the loop crossed drew again, and both worlds drew the same amount from the same
        // Bins.
        Assert.True(world.UtilityFlows.FromReserve[east] > 0, "the resumed Day drew nothing");
        Assert.Equal(Level(world, city.EastTowers[0]), Level(reloaded, city.EastTowers[0]));
        Assert.Equal(Level(world, city.EastTowers[1]), Level(reloaded, city.EastTowers[1]));

        city.Simulation.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    /// <summary>
    /// Two runs of the same world from the same seed reach the same state. <b>A rerun rather than an
    /// Input Log replay</b>, because the fixture is built by hand rather than by Commands, so there is
    /// no log to replay — what it holds is that nothing in the settle reads anything but the state.
    /// </summary>
    [Fact]
    public void A_rerun_from_the_same_seed_reaches_the_same_state()
    {
        ulong Run()
        {
            City city = Build(new Shape(WestTowers: 1, EastTowers: 2));

            AfterSettling(city.Simulation, day: 1);
            city.Simulation.CheckEndOfRun();

            return city.World.HashState();
        }

        Assert.Equal(Run(), Run());
    }

    /// <summary>
    /// The route worker count does not change the settle. <b>Nobody commutes in this world</b> —
    /// <c>watered.toml</c> declares no trade and no job — so this holds the claim for the reserve
    /// rather than exercising the parallel loop.
    /// </summary>
    [Fact]
    public void The_thread_count_does_not_change_the_state()
    {
        ulong Run(int workers)
        {
            City city = Build(new Shape(WestTowers: 1, EastTowers: 2));
            city.Simulation.RouteWorkerCount = workers;

            AfterSettling(city.Simulation, day: 1);
            city.Simulation.CheckEndOfRun();

            return city.World.HashState();
        }

        Assert.Equal(Run(1), Run(4));
    }

    /// <summary>
    /// A reload that stops delivering Water closes the networks and leaves every tower holding what
    /// it held, as a plain local Bin.
    /// </summary>
    /// <remarks>
    /// <b>The two halves are one claim.</b> The network rows have to go, on slice 1's rule — a
    /// retired Utility's counters would otherwise serve a Day of production that predates the edit.
    /// The stock must not, because it never stood in those rows: a tower's Bin belongs to its
    /// Building and an undelivered Utility's local Bin is still legal.
    /// </remarks>
    [Fact]
    public void A_reload_that_removes_delivery_leaves_the_towers_holding_their_water()
    {
        City city = Build(new Shape(EastTowers: 1));
        World world = city.World;
        int tower = city.EastTowers[0];

        AfterSettling(city.Simulation, day: 0);

        long held = Level(world, tower);

        Assert.True(held > 0, "the tower never filled");

        world.Adopt(Undelivered(), contentHash: 0, world.Tick, Key);

        Assert.Equal(0, world.UtilityFlows.Rows.LiveCount);
        Assert.Equal(DistrictMarkets.NoRow, world.Markets.Row(world, city.East, Water));

        // The Bin is the Building's and stays the Building's, with its ceiling and its contents.
        Assert.Equal(held, Level(world, tower));

        AfterSettling(city.Simulation, day: 1);

        // A whole Day with nothing settling. Nothing drew on the reserve and nothing leaked out of it.
        Assert.Equal(held, Level(world, tower));
        Assert.Equal(0, world.UtilityFlows.Rows.LiveCount);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// <c>watered.toml</c> with Water's delivery taken out — no <c>storage</c>, no
    /// <c>link_per_crossing</c> and neither <c>pool</c> Rule.
    /// </summary>
    /// <remarks>
    /// <b>Both Rules go with the keys.</b> A <c>pool</c> term naming a Utility that states no
    /// <c>link_per_crossing</c> is refused at load, and <c>watered.toml</c>'s drawing Rule has no
    /// other term to keep it legal once its input is gone. ⚠ <b>The tower's <c>bins</c> entry
    /// stays</b>, which is the point of the reload: an undelivered Utility may be stored locally.
    /// </remarks>
    private static Ruleset Undelivered()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "watered.toml"));

        int rules = toml.IndexOf("[[rule]]", StringComparison.Ordinal);
        int districts = toml.IndexOf("[districts]", StringComparison.Ordinal);

        Assert.InRange(rules, 0, districts);

        RulesetLoadResult loaded = RulesetLoader.Parse(
            (toml[..rules] + toml[districts..])
                .Replace("storage = \"reserve\"", "", StringComparison.Ordinal)
                .Replace("link_per_crossing = 2560", "", StringComparison.Ordinal)
                .Replace("loss_per_road_tile = 1", "", StringComparison.Ordinal),
            "unwatered.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }
}
