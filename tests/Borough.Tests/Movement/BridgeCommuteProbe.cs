using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// Row 30's bridge fixture, measured before either failure clock exists.
/// </summary>
/// <remarks>
/// Two lattice districts joined by a single one-block Street. The gate stands on the near side, so
/// removing the bridge cuts the far side off from the Outside Connection and fails every commute
/// that crosses it. See <c>plans/decline-recovery.md</c>.
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class BridgeCommuteProbe(ITestOutputHelper output)
{
    private const ulong Seed = 0xB121_D6E0_0000_0030UL;
    private const int BlockTiles = 32;
    private const int DistrictBlocks = 2;
    private const int BridgeColumn = DistrictBlocks;
    private const int FarColumn = DistrictBlocks + 1;
    private const int Population = 1_000;
    private const int RemovedOnDay = 2;
    private const int Days = 16;

    private static readonly WorldKey Key = WorldKey.FromSeed(Seed);
    private static readonly Tiles BridgeEast = new((BridgeColumn * BlockTiles) + (BlockTiles / 2));

    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Repaired_on_day_5() => Run(rebuiltOnDay: 5);

    [Fact]
    public void Repaired_on_day_12() => Run(rebuiltOnDay: 12);

    [Fact]
    public void Never_repaired() => Run(rebuiltOnDay: null);

    /// <summary>
    /// Whether the price the Commute Budget judges moves when the road it crosses is loaded.
    /// </summary>
    [Fact]
    public void The_budget_test_prices_free_flow_only()
    {
        Simulation simulation = Start();
        World world = simulation.World;

        StepTo(simulation, (1 * Ticks.PerDay) + (Ticks.PerDay / 4));

        int bridge = world.StreetAt(BridgeEast, new Tiles(0));
        (int home, int work) = FirstCrossing(world);

        Address from = world.VehicleAccessPoint(home);
        Address to = world.VehicleAccessPoint(work);
        RoadSegmentTable segments = world.Roads.Segments;
        TravelTime crossing = world.Rules.Trips.CrossingCost;

        TravelTime idle = WalkRouting.Cost(world.Roads, TravelMode.Car, from, to, crossing, new WalkScratch());

        int forward = segments.VolumeForward[bridge];
        int backward = segments.VolumeBackward[bridge];

        segments.VolumeForward[bridge] = 10_000;
        segments.VolumeBackward[bridge] = 10_000;

        TravelTime loaded = WalkRouting.Cost(world.Roads, TravelMode.Car, from, to, crossing, new WalkScratch());
        TravelTime freeFlow = segments.FreeFlowOver(bridge);
        TravelTime driven = world.Rules.Traffic.Apply(freeFlow, segments.LoadAt(bridge, true, freeFlow));

        segments.VolumeForward[bridge] = forward;
        segments.VolumeBackward[bridge] = backward;

        _output.WriteLine(
            $"planned car commute: idle bridge {idle.Raw}, bridge at 10,000 vehicles {loaded.Raw} (TravelTime raw)");
        _output.WriteLine(
            $"bridge in flight: free-flow {freeFlow.Raw}, at 10,000 vehicles {driven.Raw} (TravelTime raw)");

        Assert.Equal(idle, loaded);
        Assert.True(driven > freeFlow, "the loaded bridge costs a driver no more than an empty one.");
    }

    private void Run(int? rebuiltOnDay)
    {
        Simulation simulation = Start();
        World world = simulation.World;

        int gate = Gate(world);

        _output.WriteLine(
            $"gate on the {(IsFar(world, gate) ? "far" : "near")} side; bridge removed Day {RemovedOnDay} 23:00; "
            + (rebuiltOnDay is { } d ? $"rebuilt Day {d} 23:00" : "never rebuilt"));

        Assert.False(IsFar(world, gate), "the gate stands on the far side, so the fixture cuts off the wrong half.");

        List<Handle<Citizen>> crossers = [];
        long completed = 0, noRoute = 0, overBudget = 0;

        for (int day = 0; day < Days; day++)
        {
            int lateEvening = (day * Ticks.PerDay) + (Ticks.PerDay * 18 / 24);
            (long Completed, long NoRoute, long OverBudget) before = (0, 0, 0);

            if (day == RemovedOnDay)
            {
                before = StepTo(simulation, lateEvening);
                crossers = Crossers(world);

                _output.WriteLine(
                    $"Day {day}: {crossers.Count} crossers, {world.Travellers.Rows.LiveCount} Travellers in flight at removal");

                Issue(simulation, Command.Demolish(BridgeEast, new Tiles(0), DemolishTarget.Street));
            }
            else if (day == rebuiltOnDay)
            {
                before = StepTo(simulation, lateEvening);
                Issue(simulation, Lay(BridgeColumn, 0, StreetAxis.East));
            }

            (long c, long n, long o) = StepTo(simulation, (day + 1) * Ticks.PerDay);
            c += before.Completed;
            n += before.NoRoute;
            o += before.OverBudget;
            completed += c;
            noRoute += n;
            overBudget += o;

            if (day >= RemovedOnDay)
            {
                Report(world, day, crossers, c, n, o);
            }
        }

        _output.WriteLine($"run total: completed {completed}, no route {noRoute}, over budget {overBudget}");
    }

    private void Report(World world, int day, List<Handle<Citizen>> crossers, long completed, long noRoute, long overBudget)
    {
        int alive = 0, stillCrossing = 0, sameSide = 0, jobless = 0, failedToday = 0, completedToday = 0;

        foreach (Handle<Citizen> crosser in crossers)
        {
            if (!world.Citizens.Rows.TryResolve(crosser, out int citizen))
            {
                continue;
            }

            alive++;

            if (!world.Businesses.Rows.TryResolve(world.Citizens.Workplace[citizen], out int business))
            {
                jobless++;
            }
            else if (IsFar(world, HomeOf(world, citizen)) != IsFar(world, PremisesOf(world, business)))
            {
                stillCrossing++;
            }
            else
            {
                sameSide++;
            }

            if (world.Citizens.LastTripEndedDay[citizen] == day)
            {
                var fate = (TripFate)world.Citizens.LastTripFate[citizen];
                failedToday += fate is TripFate.NoRouteFound or TripFate.ExceededCommuteBudget ? 1 : 0;
                completedToday += fate == TripFate.Completed ? 1 : 0;
            }
        }

        (int far, int cutOff, int abandoned) = FarSide(world);

        _output.WriteLine(
            $"Day {day,2}: crossers alive {alive}, still crossing {stillCrossing}, now same side {sameSide}, "
            + $"jobless {jobless}; last trip today failed {failedToday}, completed {completedToday} | "
            + $"city trips completed {completed}, no route {noRoute}, over budget {overBudget} | "
            + $"far Buildings {far}, cut off {cutOff}, abandoned {abandoned}");
    }

    private static Simulation Start()
    {
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Population), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), Rules());

        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(new TickInput(Streets(), rulesetHash: 0));
        simulation.Step(new TickInput(Zoning(), rulesetHash: 0));

        SyntheticCity.PeopleInto(simulation.World, Key, Ticks.Zero);

        return simulation;
    }

    private static (long Completed, long NoRoute, long OverBudget) StepTo(Simulation simulation, long tick)
    {
        long completed = 0, noRoute = 0, overBudget = 0;

        while ((long)simulation.World.Tick.Raw < tick)
        {
            simulation.Step(new TickInput(default, rulesetHash: 0));
            simulation.Employment.Drain();

            TripActivity trips = simulation.Trips.Drain();
            completed += trips.Completed.Sum;
            noRoute += trips.NoRouteFound.Sum;
            overBudget += trips.ExceededCommuteBudget.Sum;
        }

        return (completed, noRoute, overBudget);
    }

    private static void Issue(Simulation simulation, Command command)
    {
        long refused = simulation.CommandsRefused;
        int segments = simulation.World.Roads.Segments.Rows.LiveCount;

        simulation.Step(new TickInput([command], rulesetHash: 0));

        Assert.Equal(refused, simulation.CommandsRefused);
        Assert.NotEqual(segments, simulation.World.Roads.Segments.Rows.LiveCount);
    }

    private static List<Handle<Citizen>> Crossers(World world)
    {
        List<Handle<Citizen>> crossers = [];

        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            if (world.Citizens.Rows.IsLive(citizen)
                && world.Businesses.Rows.TryResolve(world.Citizens.Workplace[citizen], out int business)
                && IsFar(world, HomeOf(world, citizen)) != IsFar(world, PremisesOf(world, business)))
            {
                crossers.Add(world.Citizens.Rows.At(citizen));
            }
        }

        return crossers;
    }

    private static (int Home, int Work) FirstCrossing(World world)
    {
        foreach (Handle<Citizen> crosser in Crossers(world))
        {
            int citizen = world.Citizens.Rows.Resolve(crosser);
            int business = world.Businesses.Rows.Resolve(world.Citizens.Workplace[citizen]);

            return (HomeOf(world, citizen), PremisesOf(world, business));
        }

        Assert.Fail("nobody works across the bridge, so there is no commute to price.");

        return default;
    }

    private static (int Far, int CutOff, int Abandoned) FarSide(World world)
    {
        int gateComponent = CarComponent(world, world.VehicleAccessPoint(Gate(world)));
        int far = 0, cutOff = 0, abandoned = 0;

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.Buildings.Rows.IsLive(building) || !IsFar(world, building))
            {
                continue;
            }

            far++;
            cutOff += CarComponent(world, world.VehicleAccessPoint(building)) != gateComponent ? 1 : 0;
            abandoned += world.Buildings.AbandonedSince[building] != Ticks.Zero ? 1 : 0;
        }

        return (far, cutOff, abandoned);
    }

    private static int CarComponent(World world, Address address)
    {
        if (!address.Exists || !world.Roads.Segments.Rows.IsLive(address.Segment))
        {
            return -1;
        }

        int node = world.Roads.Nodes.Rows.Resolve(world.Roads.Segments.NodeA[address.Segment]);

        return world.Roads.Nodes.CarComponent[node];
    }

    private static int Gate(World world)
    {
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.Buildings.Rows.IsLive(building) && world.IsOutsideConnection(world.Buildings.Kind[building]))
            {
                return building;
            }
        }

        Assert.Fail("no gate was raised, so nothing defines what the far side is cut off from.");

        return Rows.NoSlot;
    }

    private static bool IsFar(World world, int building) =>
        world.Lots.East[world.Lots.Rows.Resolve(world.Buildings.Lot[building])].Raw >= FarColumn * BlockTiles;

    private static int HomeOf(World world, int citizen) =>
        world.Buildings.Rows.Resolve(
            world.Households.Dwelling[world.Households.Rows.Resolve(world.Citizens.HouseholdOf[citizen])]);

    private static int PremisesOf(World world, int business) =>
        world.Buildings.Rows.Resolve(world.Businesses.Building[business]);

    private static Command[] Streets()
    {
        List<Command> commands = [];

        Grid(commands, 0);
        Grid(commands, FarColumn);
        commands.Add(Lay(BridgeColumn, 0, StreetAxis.East));

        return [.. commands];
    }

    private static void Grid(List<Command> into, int at)
    {
        for (int row = 0; row <= DistrictBlocks; row++)
        {
            for (int column = at; column < at + DistrictBlocks; column++)
            {
                into.Add(Lay(column, row, StreetAxis.East));
            }
        }

        for (int column = at; column <= at + DistrictBlocks; column++)
        {
            for (int row = 0; row < DistrictBlocks; row++)
            {
                into.Add(Lay(column, row, StreetAxis.North));
            }
        }
    }

    private static Command[] Zoning()
    {
        List<Command> commands = [];

        foreach (int at in (int[])[0, FarColumn])
        {
            for (int column = at; column < at + DistrictBlocks; column++)
            {
                for (int row = 0; row < DistrictBlocks; row++)
                {
                    commands.Add(new Command(CommandKind.Zone, Middle(column), Middle(row), zone: 1));
                }
            }
        }

        return [.. commands];
    }

    private static Ruleset Rules()
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "congested.toml"))
            .Replace("\n[layers]\n", "gives_up_after_days = 120\n\n[layers]\n", StringComparison.Ordinal)
            + "\n[[building]]\nname = \"port\"\narrivals_per_day = 1\n";

        RulesetLoadResult result = RulesetLoader.Parse(toml, "bridge.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static Command Lay(int column, int row, StreetAxis axis) => new(
        CommandKind.Connect,
        new Tiles(column * BlockTiles),
        new Tiles(row * BlockTiles),
        new ConnectPayload(axis, ConnectAction.Lay, RoadKind.Street).Encode());

    private static Tiles Middle(int block) => new((block * BlockTiles) + (BlockTiles / 2));
}
