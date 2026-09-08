using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Persistence;
using Borough.Tests.Space;

namespace Borough.Tests.Movement;

public sealed class RouteWorkerTests
{
    [Fact]
    public void Small_networks_keep_the_serial_route_path()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(1000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var sim = new Simulation(world, key) { RouteWorkerCount = 8 };
        for (int i = 0; i < 512; i++)
        {
            sim.Step(default);
            Assert.Equal(default, sim.LastRouteBatch);
        }
        Assert.True(sim.Trips.Drain().Completed.Sum > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Trip_start_rechecks_inputs_and_parking_before_using_prepared_routes(bool exhaustParking)
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var control = new World(1000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(control, key, Ticks.Zero);
        var file = new MemorySave();
        SaveFile.Write(control, 0, file);
        var subject = SaveFile.Read(file, loaded.Ruleset!, out _);
        var serial = new TripEngine(control);
        var parallel = new TripEngine(subject);
        int household = control.Households.Rows.Resolve(control.Citizens.HouseholdOf[0]);
        int from = control.Buildings.Rows.Resolve(control.Households.Dwelling[household]);
        int to = -1;
        for (int b = 0; b < control.Buildings.Rows.SlotCount; b++)
        {
            if (control.Buildings.Rows.IsLive(b) && control.PedestrianAccessPoint(b).Exists
                && control.PedestrianAccessPoint(b).Segment != control.PedestrianAccessPoint(from).Segment)
            { to = b; break; }
        }
        Assert.True(to >= 0);
        var mode = exhaustParking ? TravelMode.Car : TravelMode.Foot;
        var batch = new RouteBatch(subject.Roads, 8);
        ulong before = subject.HashState();
        parallel.PrepareRoutes(batch, 0, 0, from, to, mode);
        batch.Run();
        Assert.Equal(before, subject.HashState());
        Assert.True(batch.Prepared > 0);
        if (exhaustParking)
        {
            Assert.True(control.CarParks.Rows.LiveCount > 0);
            for (int p = 0; p < control.CarParks.Rows.SlotCount; p++)
            { control.CarParks.Capacity[p] = 0; subject.CarParks.Capacity[p] = 0; }
        }
        else { (from, to) = (to, from); }
        var expected = serial.Start(0, from, to, mode, TripPurpose.Commute, Ticks.Zero);
        var actual = parallel.StartPrepared(0, from, to, mode, Ticks.Zero, batch, 0);
        Assert.Equal(expected, actual);
        Assert.Equal(control.HashState(), subject.HashState());
        Assert.Equal(0, batch.Used);
        if (exhaustParking) { Assert.Equal(TripFate.ExceededCommuteBudget, actual); }
        else { Assert.True(batch.Fallback > 0); }
    }

    [Fact]
    public void Completion_order_does_not_change_paths_and_stale_requests_are_refused()
    {
        var graph = RoadFixtures.Chain(24);
        var batch = new RouteBatch(graph, 8);
        var from = Address.On(0, new Tiles(4), StreetSide.Left);
        var to = Address.On(22, new Tiles(20), StreetSide.Right);
        for (int i = 0; i < 32; i++) { batch.Add(i, 0, TravelMode.Foot, from, to, TravelTime.Zero); }
        using var lastCompleted = new ManualResetEventSlim();
        var threads = new HashSet<int>();
        batch.WorkerStarting = worker =>
        {
            lock (threads) { threads.Add(Environment.CurrentManagedThreadId); }
            if (worker == 0) { Assert.True(lastCompleted.Wait(TimeSpan.FromSeconds(15))); }
        };
        batch.WorkerCompleted = worker => { if (worker == 7) { lastCompleted.Set(); } };
        batch.Run();
        Assert.True(threads.Count > 1);
        var scratch = new WalkScratch();
        var cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, TravelTime.Zero, scratch, recordPath: true);
        var arcs = new int[scratch.ArcsTo(scratch.Arrived, [])];
        scratch.ArcsTo(scratch.Arrived, arcs);
        Assert.NotEmpty(arcs);
        for (int i = 0; i < 32; i++)
        {
            var route = Assert.IsType<RouteBatch.PreparedRoute>(batch.Take(i, 0, TravelMode.Foot, from, to, TravelTime.Zero));
            Assert.Equal(cost, route.Cost);
            Assert.Equal(arcs, route.Arcs.ToArray());
        }
        Assert.Null(batch.Take(0, 0, TravelMode.Car, from, to, TravelTime.Zero));
        Assert.Null(batch.Take(0, 0, TravelMode.Foot, to, from, TravelTime.Zero));
        Assert.Null(batch.Take(0, 0, TravelMode.Foot, from, to, TravelTime.FromTicks(1)));
        graph.RebuildDerived();
        Assert.Null(batch.Take(0, 0, TravelMode.Foot, from, to, TravelTime.Zero));
        batch.Clear();
        Assert.Null(batch.Take(0, 0, TravelMode.Foot, from, to, TravelTime.Zero));
    }

    [Fact]
    public void Save_reload_and_worker_changes_preserve_the_continuing_city()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(1);
        var world = new World(4000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var control = new Simulation(world, key) { RouteWorkerCount = 8 };
        long usedBeforeSave = 0;
        for (int i = 0; i < 600; i++)
        { control.Step(default); usedBeforeSave += control.LastRouteBatch.Used; }
        Assert.True(usedBeforeSave > 0);
        var file = new MemorySave();
        SaveFile.Write(world, 0, file);
        var restored = SaveFile.Read(file, loaded.Ruleset!, out _);
        var resumed = new Simulation(restored, key);
        Assert.Equal(world.HashState(), restored.HashState());
        long usedAfterLoad = 0;
        for (int i = 0; i < 2048; i++)
        {
            if (i == 64) { resumed.RouteWorkerCount = 8; control.RouteWorkerCount = 2; }
            if (i == 1024) { resumed.RouteWorkerCount = 2; control.RouteWorkerCount = 8; }
            if (i == 1536) { resumed.RouteWorkerCount = 1; control.RouteWorkerCount = 1; }
            control.Step(default); resumed.Step(default);
            usedAfterLoad += resumed.LastRouteBatch.Used;
            Assert.Equal(world.HashState(), restored.HashState());
        }
        Assert.True(usedAfterLoad > 0);
        control.CheckEndOfRun(); resumed.CheckEndOfRun();
        Assert.Throws<ArgumentOutOfRangeException>(() => control.RouteWorkerCount = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => control.RouteWorkerCount = 9);
    }

    [Theory]
    [InlineData(2, 512)]
    [InlineData(8, 2048)]
    public void Parallel_commute_routes_preserve_every_tick_and_are_actually_consumed(int workers, int ticks)
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var builder = new InputLogBuilder(0, new WorldConfiguration(4000), 0);
        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        var log = builder.Build();
        var serial = Replay.Start(log, loaded.Ruleset!);
        var parallel = Replay.Start(log, loaded.Ruleset!);
        parallel.RouteWorkerCount = workers;
        long prepared = 0, used = 0;
        for (int tick = 0; tick < ticks; tick++)
        {
            var now = new Ticks((ulong)tick);
            var input = new TickInput(log.At(now), log.RulesetHashAt(now));
            serial.Step(input); parallel.Step(input);
            Assert.Equal(serial.World.HashState(), parallel.World.HashState());
            prepared += parallel.LastRouteBatch.Prepared;
            used += parallel.LastRouteBatch.Used;
        }
        Assert.True(prepared > 0 && used > 0, $"Prepared {prepared}, consumed {used} routes.");
        serial.CheckEndOfRun(); parallel.CheckEndOfRun();
    }
}
