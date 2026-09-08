using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Headless;

internal static class ProfileDump
{
    internal static int Run(Options options, TextWriter output, TextReader? input = null)
    {
        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out _)) { return 2; }
        var key = WorldKey.FromSeed(options.Seed);
        ulong rulesetHash = RulesetFile.HashOf(options.RulesetPath!);
        World world;
        Simulation sim;
        if (options.ProfileLoadPath is { } load)
        {
            using var stream = File.OpenRead(load);
            world = SaveFile.Read(new SaveSource(stream), rules, out var header);
            if (header.RulesetInForce != rulesetHash || header.Key != key)
            { output.WriteLine("Profiling save must match the supplied Ruleset and seed."); return 2; }
            if (world.Tick.Raw > options.WarmupTicks)
            { output.WriteLine("--warmup-ticks must not precede the saved Tick."); return 2; }
            sim = new Simulation(world, key) { VerifyDecideWritesNothing = options.DecideGuard };
        }
        else
        {
            world = new World(options.Citizens, rules, key);
            sim = new Simulation(world, key) { VerifyDecideWritesNothing = options.DecideGuard };
            SyntheticCity.PopulateInto(world, key, Ticks.Zero, options.ProfilePopulation);
        }
        using var servicesLog = new StringWriter();
        if (options.ProfileServices && !ProfileServices.Place(world, key, servicesLog))
        { output.Write(servicesLog.ToString()); return 2; }
        sim.RouteWorkerCount = options.RouteWorkers;
        ulong loadedTick = world.Tick.Raw;
        output.WriteLine(JsonSerializer.Serialize(new
        {
            type = "conditions", ruleset = options.RulesetPath,
            rulesetSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(options.RulesetPath!))),
            options.Seed, options.Citizens, options.WarmupTicks, options.Ticks, options.DecideGuard, options.ProfileWait, options.ProfileWork, options.ProfileReuse, options.ProfileServices, options.ProfilePopulation,
            options.ProfileLoadPath, options.ProfileSavePath, loadedTick,
            startingCitizens = world.Citizens.Rows.LiveCount,
            network = Network(world),
            coreAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(World).Assembly.Location))),
            assemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProfileDump).Assembly.Location))),
            configuration = typeof(ProfileDump).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            processorCount = Environment.ProcessorCount,
            serverGc = System.Runtime.GCSettings.IsServerGC,
            payrollAttribution = Borough.Core.Movement.WorkSchedule.PayrollAttributionEnabled,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            stepThreads = options.RouteWorkers,
            threading = "one coordinator; route worker limit includes caller; all world writes remain serial",
            allocationScope = "process-wide allocation delta during Step, including workers",
            routeWorkScope = "synchronous queries only; routeBatch reports prepared/used/fallback queries",
            reuseScope = "Trip, Shopping and civic searches; shared FIFO across categories; cold at capture start",
            payrollWorkScope = "diagnostic population scan immediately before accrual; intrusive, not function-internal counters",
            timing = "Step only; setup, counters, hashes and end invariants excluded"
        }));
        output.Write(servicesLog.ToString());
        while (world.Tick.Raw < options.WarmupTicks)
        {
            sim.Step(default);
            if (world.Tick.Raw % Ticks.PerDay == 0)
            {
                output.WriteLine(JsonSerializer.Serialize(new { type = "warmup", tick = world.Tick.Raw,
                    citizens = world.Citizens.Rows.LiveCount, buildings = world.Buildings.Rows.LiveCount }));
                output.Flush();
            }
        }
        sim.Trips.Drain(); sim.Rules.Drain();
        if (options.ProfileWait)
        {
            output.WriteLine(JsonSerializer.Serialize(new { type = "ready", pid = Environment.ProcessId, tick = world.Tick.Raw }));
            output.Flush();
            if ((input ?? Console.In).ReadLine() != "go") { output.WriteLine("Expected 'go' on stdin after attaching the profiler."); return 2; }
        }
        var reuse = options.ProfileReuse ? new RouteReuseWindows() : null;
        for (ulong remaining = options.Ticks; remaining > 0;)
        {
            int count = (int)Math.Min((ulong)Ticks.PerDay, remaining);
            output.WriteLine(JsonSerializer.Serialize(Measure(sim, count, options.ProfileWork ? output : null, reuse)));
            if (reuse is not null) { output.WriteLine(JsonSerializer.Serialize(new { type = "crossTickReuse", cumulative = reuse.Reading() })); }
            output.WriteLine(JsonSerializer.Serialize(ProfileServices.Read(sim)));
            output.Flush();
            remaining -= (ulong)count;
        }
        sim.CheckEndOfRun();
        if (options.ProfileSavePath is { } save)
        {
            using var stream = new FileStream(save, FileMode.CreateNew, FileAccess.Write);
            SaveFile.Write(world, rulesetHash, new SaveSink(stream));
        }
        output.WriteLine(JsonSerializer.Serialize(new { type = "end", tick = world.Tick.Raw, hash = world.HashState().ToString("X16"), invariants = "passed" }));
        return 0;
    }

    internal static object Network(World world)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes);
        var graph = world.Roads;
        for (int n = 0; n < graph.Nodes.Rows.SlotCount; n++)
        {
            writer.Write(graph.Nodes.Rows.IsLive(n));
            if (!graph.Nodes.Rows.IsLive(n)) { continue; }
            writer.Write(graph.Nodes.East[n].Raw); writer.Write(graph.Nodes.North[n].Raw);
        }
        for (int s = 0; s < graph.Segments.Rows.SlotCount; s++)
        {
            writer.Write(graph.Segments.Rows.IsLive(s));
            if (!graph.Segments.Rows.IsLive(s)) { continue; }
            writer.Write(graph.Nodes.Rows.Resolve(graph.Segments.NodeA[s]));
            writer.Write(graph.Nodes.Rows.Resolve(graph.Segments.NodeB[s]));
            writer.Write(graph.Segments.LengthTiles[s].Raw);
            writer.Write(graph.Segments.ModesForward[s]); writer.Write(graph.Segments.ModesBackward[s]);
            writer.Write(graph.Segments.FreeFlow[s].Raw); writer.Write(graph.Segments.CapacityPerDay[s]);
        }
        return new { nodes = graph.Nodes.Rows.LiveCount, segments = graph.Segments.Rows.LiveCount,
            sha256 = Convert.ToHexString(SHA256.HashData(bytes.ToArray())) };
    }

    internal static Sample Measure(Simulation sim, int count, TextWriter? workOutput = null, RouteReuseWindows? reuse = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        bool instrumented = workOutput is not null || reuse is not null;
        var shoppingWork = !instrumented ? null : new ShoppingWork();
        var routes = !instrumented ? null : new RouteWork();
        var shoppingRoutes = !instrumented ? null : new RouteWork();
        var civicRoutes = !instrumented ? null : new RouteWork();
        var estimateReuse = workOutput is null ? null : new RouteReuseProbe();
        var shoppingReuse = workOutput is null ? null : new RouteReuseProbe();
        var otherReuse = workOutput is null ? null : new RouteReuseProbe();
        void Attach(RouteWork? work, RouteReuseProbe? tickProbe)
        {
            if (work is null) { return; }
            work.ObserveSearchWith((graph, mode, from, to) =>
            { tickProbe?.Observe(graph, mode, from, to); reuse?.Observe(graph, mode, from, to); });
            work.ObserveFinishedSearchWith(reuse is null ? null : reuse.Finish);
        }
        Attach(shoppingWork?.Estimates, estimateReuse);
        Attach(shoppingRoutes, shoppingReuse);
        Attach(routes, otherReuse);
        Attach(civicRoutes, null);
        sim.Civic.RouteWork = civicRoutes;
        sim.Shopping.Work = shoppingWork;
        sim.Trips.RouteWork = routes;
        sim.Trips.ShoppingRouteWork = shoppingRoutes;
        var payroll = workOutput is null ? null : new ProfilePayroll(sim.World);
        sim.PayrollStarting = payroll is null ? null : payroll.Read;
        var payrollClock = workOutput is not null && Borough.Core.Movement.WorkSchedule.PayrollAttributionEnabled
            ? new PayrollClock() : null;
        sim.PayrollMeasuring = payrollClock is null ? null : payrollClock.Select;
        var phases = workOutput is null ? null : new PhaseClock();
        sim.PhaseCompleted = phases is null ? null : phases.Mark;
        var civicEvents = new long[Enum.GetValues<CareEventKind>().Length];
        sim.Civic.EventObserved = kind => civicEvents[(int)kind]++;
        var elapsed = new double[count];
        var world = sim.World;
        var growth = workOutput is null ? null : new TableGrowthProbe(world.Tables);
        ulong start = world.Tick.Raw, worstTick = start;
        long allocated = 0, outings = 0, purchases = 0, delivered = 0, paid = 0;
        long routePrepared = 0, routeUsed = 0, routeFallback = 0;
        long vehicleTicks = 0, loadedSegmentTicks = 0, congestedSegmentTicks = 0;
        int peakTravellers = 0, peakVehicles = 0, peakLoadRaw = 0;
        int gen0 = 0, gen1 = 0, gen2 = 0;
        double total = 0, worst = 0;
        for (int i = 0; i < count; i++)
        {
            shoppingWork?.Reset(); routes?.Reset(); shoppingRoutes?.Reset(); civicRoutes?.Reset();
            estimateReuse?.Reset(); shoppingReuse?.Reset(); otherReuse?.Reset();
            int before0 = GC.CollectionCount(0), before1 = GC.CollectionCount(1), before2 = GC.CollectionCount(2);
            long before = GC.GetTotalAllocatedBytes(precise: true);
            phases?.Begin();
            long clock = Stopwatch.GetTimestamp();
            long ended = TimedStep(sim);
            double ms = Stopwatch.GetElapsedTime(clock, ended).TotalMilliseconds;
            long tickAllocated = GC.GetTotalAllocatedBytes(precise: true) - before;
            var batch = sim.LastRouteBatch;
            routePrepared += batch.Prepared; routeUsed += batch.Used; routeFallback += batch.Fallback;
            allocated += tickAllocated;
            gen0 += GC.CollectionCount(0) - before0;
            gen1 += GC.CollectionCount(1) - before1;
            gen2 += GC.CollectionCount(2) - before2;
            elapsed[i] = ms; total += ms;
            if (ms > worst) { worst = ms; worstTick = start + (ulong)i; }
            var shopping = sim.Shopping.Last;
            outings += shopping.Outings; purchases += shopping.Purchases; delivered += shopping.Delivered;
            paid += sim.LastPayroll.Paid;
            peakTravellers = Math.Max(peakTravellers, world.Travellers.Rows.LiveCount);
            var roads = world.Roads.Segments;
            int vehicles = 0;
            for (int s = 0; s < roads.Rows.SlotCount; s++)
            {
                if (!roads.Rows.IsLive(s)) { continue; }
                int forward = roads.VolumeForward[s], backward = roads.VolumeBackward[s];
                vehicles += forward + backward;
                int volume = Math.Max(forward, backward);
                if (volume == 0) { continue; }
                loadedSegmentTicks++;
                int load = roads.LoadOf(s, volume, roads.FreeFlowOver(s)).Raw;
                peakLoadRaw = Math.Max(peakLoadRaw, load);
                if (load > Ratio.One.Raw) { congestedSegmentTicks++; }
            }
            vehicleTicks += vehicles;
            peakVehicles = Math.Max(peakVehicles, vehicles);
            if (workOutput is not null)
            {
                workOutput.WriteLine(JsonSerializer.Serialize(new { type = "work", tick = start + (ulong)i,
                    ms, allocated = tickAllocated, travellers = world.Travellers.Rows.LiveCount, vehicles,
                    shopping, shoppingWork, shoppingRoutes, civicRoutes, payrollWork = payroll!.Last,
                    payrollTiming = payrollClock?.Last, payrollCalibration = payrollClock?.Calibrate(),
                    otherRoutes = routes, phaseMs = phases!.Reading(),
                    tableGrowth = growth!.Read(), routeBatch = batch,
                    routeReuse = new { estimates = estimateReuse!.Reading(), shopping = shoppingReuse!.Reading(),
                        other = otherReuse!.Reading() } }));
            }
        }
        sim.Civic.EventObserved = null;
        sim.Civic.RouteWork = null;
        sim.PhaseCompleted = null;
        sim.PayrollStarting = null;
        sim.PayrollMeasuring = null;
        sim.Shopping.Work = null; sim.Trips.RouteWork = null; sim.Trips.ShoppingRouteWork = null;
        Array.Sort(elapsed);
        int employed = 0;
        for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
            if (world.Citizens.Rows.IsLive(c) && !world.Citizens.Workplace[c].IsNone) { employed++; }
        var trips = sim.Trips.Drain();
        var rules = sim.Rules.Drain();
        return new Sample(start, count, world.Citizens.Rows.LiveCount, world.Buildings.Rows.LiveCount,
            employed, total / count, elapsed[(int)Math.Ceiling(count * .95) - 1],
            elapsed[(int)Math.Ceiling(count * .99) - 1], worst, worstTick, allocated,
            peakTravellers, peakVehicles, vehicleTicks, loadedSegmentTicks, congestedSegmentTicks,
            peakLoadRaw / 65536.0, trips.Completed.Sum, trips.NoRouteFound.Sum,
            trips.ExceededCommuteBudget.Sum, trips.DriveLegs.Sum, trips.WalkLegs.Sum,
            outings, purchases, delivered, paid, rules.Evaluations.Sum,
            gen0, gen1, gen2, GC.GetTotalMemory(false), Environment.WorkingSet, routePrepared, routeUsed, routeFallback,
            Enum.GetValues<CareEventKind>().ToDictionary(k => k.ToString(), k => civicEvents[(int)k]));
    }

    private sealed class PhaseClock
    {
        private readonly long[] _elapsed = new long[8];
        private long _previous;
        public void Begin() => _previous = Stopwatch.GetTimestamp();
        public void Mark(TickPhase phase)
        {
            long now = Stopwatch.GetTimestamp();
            _elapsed[(int)phase] = now - _previous;
            _previous = now;
        }
        public Dictionary<string, double> Reading() => Enum.GetValues<TickPhase>()
            .ToDictionary(p => p.ToString(), p => _elapsed[(int)p] * 1000.0 / Stopwatch.Frequency);
    }

    // Keep a visible frame when the JIT inlines Simulation.Step. Reading the clock after Step
    // also prevents a tail call from erasing this attribution boundary.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TimedStep(Simulation sim)
    {
        sim.Step(default);
        return Stopwatch.GetTimestamp();
    }

    internal sealed record Sample(ulong StartTick, int Ticks, int Citizens, int Buildings, int Employed,
        double MeanMs, double P95Ms, double P99Ms, double MaxMs, ulong MaxTick, long AllocatedBytes,
        int PeakTravellers, int PeakVehicles, long VehicleTicks, long LoadedSegmentTicks,
        long CongestedSegmentTicks, double PeakVolumeCapacity, long CompletedTrips, long NoRoute,
        long OverBudget, long DriveLegs, long WalkLegs, long ShoppingOutings, long Purchases,
        long DeliveredGoods, long Wages, long RuleEvaluations, int Gen0Collections, int Gen1Collections,
        int Gen2Collections, long ManagedHeapBytes, long WorkingSetBytes,
        long RoutePrepared, long RouteUsed, long RouteFallback, Dictionary<string, long> CivicEvents);
}
