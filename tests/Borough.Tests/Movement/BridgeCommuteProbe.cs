using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

using Xunit.Abstractions;

using static Borough.Tests.Movement.BridgeCity;

namespace Borough.Tests.Movement;

/// <summary>
/// Measures row 30's bridge fixture (<see cref="BridgeCity"/>) through a removal and a repair.
/// </summary>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class BridgeCommuteProbe(ITestOutputHelper output)
{
    private const int RemovedOnDay = 2;
    private const int Days = 19;
    private const int ShippedDeadlineDays = 10;
    private const int ShippedCutOffDays = 14;

    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Repaired_on_day_5() => Run(rebuiltOnDay: 5, dismissAfterDays: ShippedDeadlineDays);

    [Fact]
    public void Repaired_on_day_12() => Run(rebuiltOnDay: 12, dismissAfterDays: ShippedDeadlineDays);

    [Fact]
    public void Never_repaired() => Run(rebuiltOnDay: null, dismissAfterDays: ShippedDeadlineDays);

    [Fact]
    public void Never_repaired_without_a_deadline() => Run(rebuiltOnDay: null, dismissAfterDays: null);

    /// <summary>
    /// Whether the price the Commute Budget judges moves when the road it crosses is loaded.
    /// </summary>
    [Fact]
    public void The_budget_test_prices_free_flow_only()
    {
        Simulation simulation = Start(BridgeRules());
        World world = simulation.World;

        StepTo(simulation, LateMorning(1));

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

    private void Run(int? rebuiltOnDay, int? dismissAfterDays)
    {
        Simulation simulation = Start(BridgeRules(dismissAfterDays, dismissAfterDays is null ? null : ShippedCutOffDays));
        World world = simulation.World;

        _output.WriteLine(
            $"bridge removed Day {RemovedOnDay} 23:00; "
            + (rebuiltOnDay is { } d ? $"rebuilt Day {d} 23:00; " : "never rebuilt; ")
            + (dismissAfterDays is { } days ? $"dismissal after {days} Days, abandonment after {ShippedCutOffDays} cut off" : "no dismissal or abandonment"));

        Assert.False(IsFar(world, Gate(world)), "the gate stands on the far side, so the fixture cuts off the wrong half.");

        List<Handle<Citizen>> crossers = [];
        long completed = 0, noRoute = 0, overBudget = 0;

        for (int day = 0; day < Days; day++)
        {
            (long Completed, long NoRoute, long OverBudget) before = (0, 0, 0);

            if (day == RemovedOnDay)
            {
                before = StepTo(simulation, LateEvening(day));
                crossers = Crossers(world);

                _output.WriteLine(
                    $"Day {day}: {crossers.Count} crossers, {world.Travellers.Rows.LiveCount} Travellers in flight at removal");

                RemoveBridge(simulation);
            }
            else if (day == rebuiltOnDay)
            {
                before = StepTo(simulation, LateEvening(day));
                RebuildBridge(simulation);
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
        int alive = 0, stillCrossing = 0, sameSide = 0, jobless = 0, clocked = 0, failedToday = 0, completedToday = 0;

        foreach (Handle<Citizen> crosser in crossers)
        {
            if (!world.Citizens.Rows.TryResolve(crosser, out int citizen))
            {
                continue;
            }

            alive++;
            clocked += world.Citizens.CommuteFailedSince[citizen] != Ticks.Zero ? 1 : 0;

            if (!world.Businesses.Rows.IsValid(world.Citizens.Workplace[citizen]))
            {
                jobless++;
            }
            else if (Crosses(world, citizen))
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
            + $"jobless {jobless}, clock running {clocked}; last trip today failed {failedToday}, "
            + $"completed {completedToday} | city trips completed {completed}, no route {noRoute}, "
            + $"over budget {overBudget} | far Buildings {far}, cut off {cutOff}, abandoned {abandoned}");
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
}
