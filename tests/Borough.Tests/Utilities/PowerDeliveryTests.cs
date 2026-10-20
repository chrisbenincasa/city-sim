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
/// Utilities slice 1 — Power made in one District, delivered to another, and the brownout between.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every world here is <c>rulesets/powered.toml</c> on one ridge</b>: eight blocks in a line with
/// tall Cells at each end, which the watershed reads as two Districts joined by a single crossing
/// Segment. The plant stands in the west District and the consumers in the east, so nothing the east
/// burns can have come from anywhere but over that link.
/// </para>
/// <para>
/// <b>The fractions are asserted as arithmetic over the flow columns rather than as measured
/// magnitudes.</b> A Rule under a brownout applies <c>floor(count × delivered)</c> times, so a test
/// reads <c>Delivered</c> and computes what the output has to be. Moving a rate in the fixture then
/// moves the expectation with it instead of breaking the test.
/// </para>
/// </remarks>
public sealed class PowerDeliveryTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    /// <summary>The delivered Utility, and <c>powered.toml</c>'s first <c>[[resource]]</c>.</summary>
    private static readonly ResourceId Power = new(1);

    private static readonly ResourceId Lumens = new(2);
    private static readonly ResourceId Widget = new(3);

    // Kinds are one-based: zero is the kind no Ruleset declares.
    private const byte Dwelling = 1;
    private const byte Plant = 2;
    private const byte Works = 3;

    private const int Cells = 8;
    private const int PeakCells = 2;
    private const int Tall = 8;
    private const int Short = 3;

    /// <summary>Households put in the one dwelling that draws power, and so its occupancy.</summary>
    private const int Occupants = 2;

    /// <summary>The fixture's greedy Rule takes its <c>max</c> whenever the Bin has room.</summary>
    private const int FabricateMax = 4;

    /// <summary>The fixture's Rule periods, in Ticks.</summary>
    private const int LightingRate = 32;
    private const int FabricateRate = 8;

    private static Ruleset Rules()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    /// <summary>What a world is asked to be, so one builder serves every case below.</summary>
    /// <param name="WorksBesideThePlant">
    /// Put the greedy consumer in the plant's own District, which is what makes the loss visible: a
    /// District serving only itself keeps exactly its production less its road length.
    /// </param>
    /// <param name="ExtraCrossings">Further Segments over the one boundary, each one a link.</param>
    /// <param name="ExtraRoadTiles">
    /// Further road Tiles inside the plant's District, laid as Segments that join nothing, so the
    /// only thing that moves is the loss.
    /// </param>
    /// <param name="ExtraRoadTilesEast">
    /// The same inside the far District, which generates nothing, so its loss falls on what the
    /// links bring in rather than on any production.
    /// </param>
    private readonly record struct Shape(
        bool WorksBesideThePlant = false,
        int ExtraCrossings = 0,
        int ExtraRoadTiles = 0,
        int ExtraRoadTilesEast = 0);

    private sealed class City
    {
        public required World World { get; init; }

        public required Simulation Simulation { get; init; }

        /// <summary>The District the plant stands in.</summary>
        public required int Making { get; init; }

        /// <summary>The District the occupied dwelling stands in.</summary>
        public required int Burning { get; init; }

        /// <summary>The greedy consumer's Building slot.</summary>
        public required int Factory { get; init; }

        /// <summary>The occupied dwelling's Building slot.</summary>
        public required int Home { get; init; }

        /// <summary>The plant's Building slot.</summary>
        public required int Generator { get; init; }
    }

    private static City Build(Shape shape = default)
    {
        Ruleset rules = Rules();
        var world = new World(400, rules, Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };

        Lay(world);

        int generator = Rows.NoSlot;
        int factory = Rows.NoSlot;
        int home = Rows.NoSlot;

        for (int cell = 0; cell < Cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= Cells - PeakCells;
            int standing = 0;

            // The west peak carries the plant and the east peak the consumers. Every other Building
            // is an empty dwelling, which draws nothing because its apply count is its occupancy.
            if (cell == 0)
            {
                generator = Raise(world, cell, standing++, Plant);
            }

            if (cell == (shape.WorksBesideThePlant ? 0 : Cells - 1))
            {
                factory = Raise(world, cell, standing++, Works);
            }

            if (cell == Cells - 1)
            {
                home = Raise(world, cell, standing++, Dwelling);

                for (int occupant = 0; occupant < Occupants; occupant++)
                {
                    world.CreateHousehold(world.Buildings.Rows.At(home), lifeStage: 1);
                }
            }

            for (int filler = standing; filler < (peak ? Tall : Short); filler++)
            {
                Raise(world, cell, filler, Dwelling);
            }
        }

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Equal(2, world.Districts.Rows.LiveCount);

        int making = DistrictOf(world, generator);
        int burning = DistrictOf(world, home);

        Assert.NotEqual(Rows.NoSlot, making);
        Assert.NotEqual(making, burning);

        if (shape.ExtraCrossings > 0 || shape.ExtraRoadTiles > 0 || shape.ExtraRoadTilesEast > 0)
        {
            Widen(world, shape);

            Assert.Equal(2, world.Districts.Rows.LiveCount);
            Assert.Equal(making, DistrictOf(world, generator));
            Assert.Equal(burning, DistrictOf(world, home));
        }

        return new City
        {
            World = world,
            Simulation = simulation,
            Making = making,
            Burning = burning,
            Factory = factory,
            Home = home,
            Generator = generator,
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
    /// Lays the Segments a <see cref="Shape"/> asks for, after the Lots so that no Building moves.
    /// </summary>
    private static void Widen(World world, Shape shape)
    {
        int block = world.Rules.Roads.BlockTiles;
        int boundary = Boundary(world);

        for (int extra = 0; extra < shape.ExtraCrossings; extra++)
        {
            // Both ends inside the two Cells the one crossing already joins, so this is a second
            // link over the same boundary rather than a second boundary.
            Join(
                world,
                new Tiles((boundary * block) + 16),
                new Tiles(8 + (extra * 2)),
                new Tiles(((boundary + 1) * block) + 16),
                new Tiles(8 + (extra * 2)),
                block);
        }

        for (int laid = 0; laid < shape.ExtraRoadTiles; laid += block)
        {
            // Both ends in the plant's own Cell, so this joins no District to any other and its
            // Tiles land on the plant's District.
            Join(
                world,
                new Tiles(8),
                new Tiles(8 + (laid * 2 / block)),
                new Tiles(24),
                new Tiles(8 + (laid * 2 / block)),
                block);
        }

        for (int laid = 0; laid < shape.ExtraRoadTilesEast; laid += block)
        {
            // The same inside the far peak's own Cell, which holds no plant.
            Join(
                world,
                new Tiles(((Cells - 1) * block) + 8),
                new Tiles(8 + (laid * 2 / block)),
                new Tiles(((Cells - 1) * block) + 24),
                new Tiles(8 + (laid * 2 / block)),
                block);
        }

        world.Roads.RebuildDerived();
        world.RebuildDerived();
        world.EvaluateDistricts();
    }

    private static void Join(
        World world, Tiles fromEast, Tiles fromNorth, Tiles toEast, Tiles toNorth, int length) =>
        world.Roads.Segments.Create(
            world.Roads.Nodes.Create(fromEast, fromNorth),
            world.Roads.Nodes.Create(toEast, toNorth),
            new Tiles(length),
            RoadKind.Street,
            TravelMode.Any,
            TravelMode.Any);

    /// <summary>
    /// Puts one Building on a Lot of its own in <paramref name="cell"/>, offset by
    /// <paramref name="index"/> so a Cell can hold several.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><paramref name="now"/> is the Tick the Building's Rules are armed from</b>, so a caller
    /// raising something mid-run passes the Tick it is on. Arming from zero puts the Rule in a Wheel
    /// bucket that has already been collected, and nothing runs it until the Wheel wraps.
    /// </remarks>
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

    /// <summary>The lower Cell index of the one neighboring pair in different Districts.</summary>
    private static int Boundary(World world)
    {
        int block = world.Rules.Roads.BlockTiles;

        for (int cell = 0; cell < Cells - 1; cell++)
        {
            int here = DistrictAt(world, new Tiles((cell * block) + 4), Tiles.Zero);
            int next = DistrictAt(world, new Tiles(((cell + 1) * block) + 4), Tiles.Zero);

            if (here != next)
            {
                return cell;
            }
        }

        throw new InvalidOperationException("the ridge has no District boundary.");
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

    private static long Level(World world, int buildingSlot, ResourceId resource) =>
        world.Bins.LevelAt(world.FindBin(buildingSlot, resource));

    /// <summary>What one firing applies when <paramref name="count"/> applications are wanted.</summary>
    private static long Applied(int count, Ratio delivered) =>
        IntegerMath.MulDivFloor(count, delivered.Raw, Fixed.One);

    /// <summary>
    /// A world a Day into delivering Power, stopped on the Tick its second settle runs. The fixture
    /// <c>FactorioTests</c> and <c>DerivedRebuildAuditTests</c> reach for.
    /// </summary>
    /// <remarks>
    /// Stopping here is what makes it a fixture for both. <see cref="UtilityFlowTable.Produced"/> and
    /// <see cref="UtilityFlowTable.Demanded"/> hold a whole Day and have not been rolled, and the next
    /// <c>Step</c> is the settle that reads them.
    /// </remarks>
    internal static (World World, Simulation Simulation) Settling()
    {
        City city = Build();

        StepTo(city.Simulation, 2 * Ticks.PerDay);

        return (city.World, city.Simulation);
    }

    /// <inheritdoc cref="Settling"/>
    internal static World Delivering() => Settling().World;

    // ---- the link ------------------------------------------------------------------------------

    /// <summary>
    /// Power made in one District reaches a consumer in the other, over the one road joining them.
    /// </summary>
    /// <remarks>
    /// <b>The consuming District's own production is asserted to be zero</b>, which is what makes
    /// this a test of the link and not of a plant: every unit the east burns crossed the boundary.
    /// </remarks>
    [Fact]
    public void Power_reaches_the_far_district_over_the_adjacency()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        int making = Flow(world, city.Making);
        int burning = Flow(world, city.Burning);

        Assert.True(world.UtilityFlows.Supplied[making] > 0, "the plant made nothing");
        Assert.Equal(0, world.UtilityFlows.Asked[making]);
        Assert.Equal(0, world.UtilityFlows.Supplied[burning]);
        Assert.True(world.UtilityFlows.Asked[burning] > 0, "nothing in the east drew power");
        Assert.True(world.UtilityFlows.Received[burning] > 0, "nothing crossed the boundary");
        Assert.True(Delivered(world, city.Burning) > Ratio.Zero);

        long before = Level(world, city.Factory, Widget);

        AfterSettling(city.Simulation, day: 1);

        Assert.True(Level(world, city.Factory, Widget) > before, "the far works made nothing");
        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A District meets its own consumers before it exports, so a short producer sends nothing.
    /// </summary>
    /// <remarks>
    /// <b>Consumers in both Districts and one plant between them.</b> The greedy works stands beside
    /// the plant and outdraws it, so the west has no surplus left and the east's dwelling goes dark —
    /// which is the settle's first step holding rather than the max-flow's.
    /// </remarks>
    [Fact]
    public void Local_use_comes_before_the_links()
    {
        City city = Build(new Shape(WorksBesideThePlant: true));
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        int making = Flow(world, city.Making);
        int burning = Flow(world, city.Burning);

        Assert.True(world.UtilityFlows.Asked[making] > 0, "nothing in the west drew power");
        Assert.True(world.UtilityFlows.Asked[burning] > 0, "nothing in the east drew power");

        // Short of its own demand after the loss, so there is nothing to export.
        Assert.True(world.UtilityFlows.Received[making] > 0);
        Assert.True(world.UtilityFlows.Received[making] < world.UtilityFlows.Asked[making]);

        Assert.Equal(0, world.UtilityFlows.Supplied[burning]);
        Assert.Equal(0, world.UtilityFlows.Received[burning]);
        Assert.Equal(Ratio.Zero, Delivered(world, city.Burning));
        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Both Rule shapes degrade to the delivered fraction, each over one period of its own rate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The per-occupant Rule is the one that could not have degraded before this slice.</b> A
    /// derived apply count is a band of one — <c>n</c> applications or none — so a Utility that
    /// blocked would have stopped the Rule outright. It applies <c>floor(occupancy × delivered)</c>
    /// times instead, and the greedy Rule beside it <c>floor(max × delivered)</c>.
    /// </para>
    /// <para>
    /// The window below is one lighting period long and sits inside a single Day, so it holds exactly
    /// one lighting firing and four of the works', all of them under the fraction read before it.
    /// </para>
    /// </remarks>
    [Fact]
    public void Both_rule_shapes_run_at_the_delivered_fraction()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        Ratio delivered = Delivered(world, city.Burning);

        Assert.True(delivered > Ratio.Zero, "the east was delivered nothing");
        Assert.True(delivered < Ratio.One, "the fixture is not short of power");

        long lighting = Applied(Occupants, delivered);
        long fabricate = Applied(FabricateMax, delivered);

        Assert.True(lighting > 0, $"the dwelling applies nothing at {delivered.Raw}/65536");
        Assert.True(fabricate > 0, $"the works applies nothing at {delivered.Raw}/65536");
        Assert.True(lighting < Occupants, "the dwelling is not degraded");
        Assert.True(fabricate < FabricateMax, "the works is not degraded");

        long lumens = Level(world, city.Home, Lumens);
        long widgets = Level(world, city.Factory, Widget);

        StepTo(city.Simulation, (long)world.Tick.Raw + LightingRate);

        Assert.Equal(lumens + lighting, Level(world, city.Home, Lumens));
        Assert.Equal(
            widgets + (fabricate * (LightingRate / FabricateRate)),
            Level(world, city.Factory, Widget));

        // Neither consumer is stopped. A delivered Utility never blocks, so a shortage shows as a
        // smaller output and never as a complaint.
        AssertNotStopped(world, city.Home);
        AssertNotStopped(world, city.Factory);
        city.Simulation.CheckEndOfRun();
    }

    private static void AssertNotStopped(World world, int buildingSlot)
    {
        foreach (int instance in world.BuildingRules.Walk(buildingSlot))
        {
            Assert.Equal(Blocking.Nothing, world.RuleInstances.Blocked[instance]);
            Assert.Equal(ConditionId.None, world.RuleInstances.Reported[instance]);
        }
    }

    // ---- the road ------------------------------------------------------------------------------

    /// <summary>
    /// Demolishing the one joining Street leaves the far District short from the next Day on, and
    /// laying it again brings the delivery back. Both edits go through the player's own door.
    /// </summary>
    /// <remarks>
    /// <b>The outage and the recovery belong in one test.</b> A world that was never short proves
    /// nothing about restoring one, and a fraction that returns to the number it left says more
    /// than a fraction that is merely above zero.
    /// </remarks>
    [Fact]
    public void Removing_and_relaying_the_joining_road_starves_and_then_restores_the_far_district()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        Ratio joined = Delivered(world, city.Burning);

        Assert.True(joined > Ratio.Zero);

        int block = world.Rules.Roads.BlockTiles;
        int boundary = Boundary(world);
        var west = new Tiles(boundary * block);
        var east = new Tiles((boundary + 1) * block);

        Issue(city.Simulation, Command.Demolish(
            new Tiles(east.Raw - 1), Tiles.Zero, DemolishTarget.Street));

        AfterSettling(city.Simulation, day: 1);

        int burning = Flow(world, city.Burning);

        Assert.True(world.UtilityFlows.Asked[burning] > 0, "the east stopped asking");
        Assert.Equal(0, world.UtilityFlows.Received[burning]);
        Assert.Equal(Ratio.Zero, Delivered(world, city.Burning));

        long dark = Level(world, city.Home, Lumens);
        long idle = Level(world, city.Factory, Widget);

        AfterSettling(city.Simulation, day: 2);

        // A whole Day with no power. The consumers apply nothing and neither one stops, because a
        // delivered Utility never blocks.
        Assert.Equal(dark, Level(world, city.Home, Lumens));
        Assert.Equal(idle, Level(world, city.Factory, Widget));
        AssertNotStopped(world, city.Home);
        AssertNotStopped(world, city.Factory);

        Issue(city.Simulation, Command.Street(west, Tiles.Zero, east, Tiles.Zero, SubTiles.Zero));

        AfterSettling(city.Simulation, day: 3);

        Assert.Equal(joined, Delivered(world, city.Burning));
        Assert.True(world.UtilityFlows.Received[Flow(world, city.Burning)] > 0);

        AfterSettling(city.Simulation, day: 4);

        Assert.True(Level(world, city.Home, Lumens) > dark, "the dwelling stayed dark");
        Assert.True(Level(world, city.Factory, Widget) > idle, "the works stayed idle");
        city.Simulation.CheckEndOfRun();
    }

    /// <summary>Applies one command on the next Tick and refuses to proceed if the world refused it.</summary>
    private static void Issue(Simulation simulation, Command command)
    {
        int refused = simulation.CommandsRefused;

        simulation.Step(new TickInput([command], rulesetHash: 0));

        Assert.Equal(refused, simulation.CommandsRefused);
    }

    /// <summary>A second Segment over the same boundary doubles the link and raises the fraction.</summary>
    [Fact]
    public void A_second_crossing_road_raises_the_delivered_fraction()
    {
        City narrow = Build();
        City wide = Build(new Shape(ExtraCrossings: 1));

        AfterSettling(narrow.Simulation, day: 0);
        AfterSettling(wide.Simulation, day: 0);

        Ratio one = Delivered(narrow.World, narrow.Burning);
        Ratio two = Delivered(wide.World, wide.Burning);

        Assert.True(one > Ratio.Zero);
        Assert.True(two > one, $"one crossing delivered {one.Raw}, two delivered {two.Raw}");

        narrow.Simulation.CheckEndOfRun();
        wide.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A plant raised in the short District during the run restores full delivery the next Day.
    /// </summary>
    /// <remarks>
    /// The plant <b>goes up mid-run rather than at Tick zero</b>, which is the whole claim. A world
    /// built with two plants would only show that enough supply delivers everything, and what has
    /// to hold is that a browned-out District recovers. The network picks up a plant that did not
    /// exist when it was last settled, and the consumers return to their full counts without having
    /// stopped.
    /// </remarks>
    [Fact]
    public void A_plant_raised_in_the_short_district_restores_full_delivery()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        Assert.True(Delivered(world, city.Burning) < Ratio.One, "the east is not short");

        long lumens = Level(world, city.Home, Lumens);
        long widgets = Level(world, city.Factory, Widget);
        long opened = (long)world.Tick.Raw;

        StepTo(city.Simulation, opened + LightingRate);

        long shortLumens = Level(world, city.Home, Lumens) - lumens;
        long shortWidgets = Level(world, city.Factory, Widget) - widgets;

        // The east peak's ninth Building, armed from the Tick it is raised on.
        Raise(world, Cells - 1, Tall, Plant, world.Tick);

        AfterSettling(city.Simulation, day: 1);

        Assert.Equal(city.Burning, DistrictOf(world, city.Home));
        Assert.Equal(Ratio.One, Delivered(world, city.Burning));

        lumens = Level(world, city.Home, Lumens);
        widgets = Level(world, city.Factory, Widget);
        opened = (long)world.Tick.Raw;

        StepTo(city.Simulation, opened + LightingRate);

        Assert.Equal(Occupants, Level(world, city.Home, Lumens) - lumens);
        Assert.Equal(
            FabricateMax * (LightingRate / FabricateRate),
            Level(world, city.Factory, Widget) - widgets);
        Assert.True(Level(world, city.Home, Lumens) - lumens > shortLumens);
        Assert.True(Level(world, city.Factory, Widget) - widgets > shortWidgets);

        AssertNotStopped(world, city.Home);
        AssertNotStopped(world, city.Factory);
        city.Simulation.CheckEndOfRun();
    }

    // ---- what a Day keeps ----------------------------------------------------------------------

    /// <summary>
    /// A settle rolls the Day's counters and zeroes them, so a surplus is not banked for tomorrow.
    /// </summary>
    [Fact]
    public void Unused_power_does_not_carry_over()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        int making = Flow(world, city.Making);

        Assert.True(
            world.UtilityFlows.Supplied[making] > world.UtilityFlows.Received[making],
            "the west sent everything it made");
        Assert.Equal(0, world.UtilityFlows.Produced[making]);
        Assert.Equal(0, world.UtilityFlows.Demanded[making]);

        // The plant goes. Were a Day's surplus banked, the east would run on it tomorrow.
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[city.Generator]);

        city.Simulation.Step(new TickInput(
            [Command.Demolish(world.Lots.East[lot], Tiles.Zero, DemolishTarget.Building)],
            rulesetHash: 0));

        Assert.Equal(0, city.Simulation.CommandsRefused);

        AfterSettling(city.Simulation, day: 1);

        Assert.Equal(0, world.UtilityFlows.Supplied[Flow(world, city.Making)]);
        Assert.Equal(Ratio.Zero, Delivered(world, city.Burning));
        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A District's network loses its road length every Day, so laying more road inside it delivers
    /// less — exactly <c>loss_per_road_tile</c> per Tile.
    /// </summary>
    [Fact]
    public void Loss_grows_with_road_length()
    {
        var bare = new Shape(WorksBesideThePlant: true);
        int added = 2 * Rules().Roads.BlockTiles;

        City thin = Build(bare);
        City paved = Build(bare with { ExtraRoadTiles = added });

        AfterSettling(thin.Simulation, day: 0);
        AfterSettling(paved.Simulation, day: 0);

        int one = Flow(thin.World, thin.Making);
        int two = Flow(paved.World, paved.Making);

        Assert.Equal(thin.World.UtilityFlows.Supplied[one], paved.World.UtilityFlows.Supplied[two]);
        Assert.Equal(thin.World.UtilityFlows.Asked[one], paved.World.UtilityFlows.Asked[two]);
        Assert.Equal(
            added * thin.World.Rules.LossPerRoadTile(Power),
            thin.World.UtilityFlows.Received[one] - paved.World.UtilityFlows.Received[two]);

        thin.Simulation.CheckEndOfRun();
        paved.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Road laid in a District that generates nothing lowers what it is delivered, because the loss
    /// is a first claim on imports and not only on local production.
    /// </summary>
    /// <remarks>
    /// <b>The far District's own production is asserted to be zero</b>, which is what separates this
    /// from <see cref="Loss_grows_with_road_length"/>. Every unit it burns crossed the boundary, so
    /// the only thing the extra Tiles can take it out of is the inflow.
    /// </remarks>
    [Fact]
    public void Loss_grows_with_road_length_in_an_importing_district()
    {
        int added = 2 * Rules().Roads.BlockTiles;

        City thin = Build();
        City paved = Build(new Shape(ExtraRoadTilesEast: added));

        AfterSettling(thin.Simulation, day: 0);
        AfterSettling(paved.Simulation, day: 0);

        int one = Flow(thin.World, thin.Burning);
        int two = Flow(paved.World, paved.Burning);

        Assert.Equal(0, thin.World.UtilityFlows.Supplied[one]);
        Assert.Equal(0, paved.World.UtilityFlows.Supplied[two]);
        Assert.Equal(thin.World.UtilityFlows.Asked[one], paved.World.UtilityFlows.Asked[two]);
        Assert.Equal(
            added * thin.World.Rules.LossPerRoadTile(Power),
            thin.World.UtilityFlows.Received[one] - paved.World.UtilityFlows.Received[two]);
        Assert.True(
            Delivered(paved.World, paved.Burning) < Delivered(thin.World, thin.Burning),
            "the longer importing District was delivered the same fraction");

        thin.Simulation.CheckEndOfRun();
        paved.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Day's counters saturate at <see cref="UtilityFlowTable.Bound"/> rather than wrapping, and
    /// the settle holds at the bound.
    /// </summary>
    /// <remarks>
    /// <b>A Ruleset the loader accepts can reach it.</b> An output amount and an apply count may
    /// each be <c>int.MaxValue</c>, which posts 4.6 × 10¹⁸ in one firing — so a third firing used to
    /// carry <c>Produced</c> past <c>long.MaxValue</c> and leave a District generating a negative
    /// amount of Power, with no invariant to notice.
    /// </remarks>
    [Fact]
    public void A_days_counters_saturate_rather_than_wrapping()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        int making = Flow(world, city.Making);
        int burning = Flow(world, city.Burning);

        for (int firing = 0; firing < 3; firing++)
        {
            world.UtilityFlows.Produce(making, int.MaxValue, int.MaxValue);
            world.UtilityFlows.Demand(burning, int.MaxValue, int.MaxValue);
        }

        Assert.Equal(UtilityFlowTable.Bound, world.UtilityFlows.Produced[making]);
        Assert.Equal(UtilityFlowTable.Bound, world.UtilityFlows.Demanded[burning]);

        AfterSettling(city.Simulation, day: 1);

        Assert.Equal(UtilityFlowTable.Bound, world.UtilityFlows.Supplied[making]);
        Assert.Equal(UtilityFlowTable.Bound, world.UtilityFlows.Asked[burning]);

        // The settle solved a flow network with a quintillion on both sides of it and wrote nothing
        // negative and nothing above whole.
        Assert.InRange(world.UtilityFlows.Received[burning], 1, UtilityFlowTable.Bound);
        Assert.InRange(world.UtilityFlows.Received[making], 0, UtilityFlowTable.Bound);
        Assert.InRange(Delivered(world, city.Burning).Raw, 0, Fixed.One);
        Assert.InRange(Delivered(world, city.Making).Raw, 0, Fixed.One);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A reload that stops delivering Power closes the networks, so putting the delivery back cannot
    /// serve a Day of production that predates the edit.
    /// </summary>
    /// <remarks>
    /// <b>The plant is demolished while the delivery is off</b>, which is what makes the last
    /// assertion about banked history rather than about generation. A network that kept its counters
    /// across the edit would serve the part-Day it was holding when the file changed, to a city with
    /// no plant standing in it.
    /// </remarks>
    [Fact]
    public void A_reload_that_removes_delivery_cannot_be_reversed_into_power()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        Assert.True(Delivered(world, city.Burning) > Ratio.Zero, "the east was never lit");

        // Far enough into the Day for the plant to have fed the network since the last settle, so
        // there is history to resurrect.
        StepTo(city.Simulation, (long)world.Tick.Raw + (4 * FabricateRate));

        Assert.True(
            world.UtilityFlows.Produced[Flow(world, city.Making)] > 0, "the plant fed nothing");

        world.Adopt(Undelivered(), contentHash: 0, world.Tick, Key);

        // Nothing holds a counter any more: the networks went, and so did the Pool rows that
        // addressed them.
        Assert.Equal(0, world.UtilityFlows.Rows.LiveCount);
        Assert.Equal(DistrictMarkets.NoRow, world.Markets.Row(world, city.Burning, Power));

        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[city.Generator]);

        Issue(city.Simulation, Command.Demolish(
            world.Lots.East[lot], Tiles.Zero, DemolishTarget.Building));

        // Two Day boundaries with no delivered Utility in force at all.
        AfterSettling(city.Simulation, day: 2);

        world.Adopt(Rules(), contentHash: 0, world.Tick, Key);

        int reopened = Flow(world, city.Burning);

        Assert.Equal(Ratio.Zero, world.UtilityFlows.Delivered[reopened]);
        Assert.Equal(0, world.UtilityFlows.Supplied[reopened]);
        Assert.Equal(0, world.UtilityFlows.Produced[reopened]);

        AfterSettling(city.Simulation, day: 3);

        // The first settle under the restored file. No plant stands, so nothing was made and the
        // east is served nothing.
        Assert.Equal(0, world.UtilityFlows.Supplied[Flow(world, city.Making)]);
        Assert.Equal(0, world.UtilityFlows.Received[Flow(world, city.Burning)]);
        Assert.Equal(Ratio.Zero, Delivered(world, city.Burning));

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// <c>powered.toml</c> with Power's delivery taken out — no <c>link_per_crossing</c>, no plant
    /// Rule and no <c>pool</c> term.
    /// </summary>
    /// <remarks>
    /// <b>The <c>pool</c> terms have to go with the key, and so does the import.</b> A pool term
    /// naming a Utility that states no <c>link_per_crossing</c> is refused at load, and so are an
    /// <c>import_per_connection</c> and a <c>[[hinterland]]</c> price on an undelivered one — so a
    /// file that kept any of them would not be a legal reload and the hole this covers would be
    /// unreachable.
    /// </remarks>
    private static Ruleset Undelivered()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        int generate = toml.IndexOf("[[rule]]\nname    = \"generate\"", StringComparison.Ordinal);
        int lighting = toml.IndexOf("[[rule]]\nname    = \"lighting\"", StringComparison.Ordinal);

        Assert.InRange(generate, 0, lighting);

        RulesetLoadResult loaded = RulesetLoader.Parse(
            (toml[..generate] + toml[lighting..])
                .Replace("storage = 0", "", StringComparison.Ordinal)
                .Replace("link_per_crossing = 2560", "", StringComparison.Ordinal)
                .Replace("loss_per_road_tile = 1", "", StringComparison.Ordinal)
                .Replace("import_per_connection = 2560", "", StringComparison.Ordinal)
                .Replace(
                    "{ resource = \"power\",  price = 2 },",
                    "",
                    StringComparison.Ordinal)
                .Replace(
                    "inputs  = [ { scope = \"pool\", resource = \"power\", amount = 4 } ]",
                    "inputs  = []",
                    StringComparison.Ordinal),
            "unpowered.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    // ---- the equivalences ----------------------------------------------------------------------

    [Fact]
    public void A_replayed_run_reaches_the_same_state()
    {
        ulong Run()
        {
            City city = Build();

            AfterSettling(city.Simulation, day: 1);
            city.Simulation.CheckEndOfRun();

            return city.World.HashState();
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void The_networks_survive_save_and_reload()
    {
        City city = Build();

        AfterSettling(city.Simulation, day: 0);

        var file = new MemorySave();
        SaveFile.Write(city.World, 1, file);
        World reloaded = SaveFile.Read(file, city.World.Rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = true };

        Assert.Equal(SaveHeader.Current, header.FormatVersion);
        Assert.Equal(city.World.HashState(), reloaded.HashState());
        Assert.Equal(city.World.UtilityFlows.Rows.LiveCount, reloaded.UtilityFlows.Rows.LiveCount);

        // The Pool-to-network index is derived, so a load has to have rebuilt it for the fraction to
        // be addressable at all.
        Assert.Equal(Delivered(city.World, city.Burning), Delivered(reloaded, city.Burning));

        for (int tick = 0; tick <= Ticks.PerDay; tick++)
        {
            city.Simulation.Step(TickInput.Empty);
            resumed.Step(TickInput.Empty);
            Assert.Equal(city.World.HashState(), reloaded.HashState());
        }

        city.Simulation.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    /// <summary>
    /// The route worker count does not change the settle. <b>Nobody commutes in this world</b> —
    /// <c>powered.toml</c> declares no trade and no job — so this holds the claim for the networks
    /// rather than exercising the parallel loop, which is <c>RouteWorkerTests</c>' job.
    /// </summary>
    [Fact]
    public void The_thread_count_does_not_change_the_state()
    {
        ulong Run(int workers)
        {
            City city = Build();
            city.Simulation.RouteWorkerCount = workers;

            AfterSettling(city.Simulation, day: 1);
            city.Simulation.CheckEndOfRun();

            return city.World.HashState();
        }

        Assert.Equal(Run(1), Run(4));
    }
}
