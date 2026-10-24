using System.Diagnostics;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Borough.Tests.Persistence;
using Xunit.Abstractions;

namespace Borough.Tests.ReviewProbes;

// Sweep 03 investigator A probes. Copied in by run.sh, run, then removed.
public sealed class VerifierProbeA(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // SegmentVolumeTests' fixture: declining.toml with every Household owning a car.
    private static Ruleset Driving(string file = "declining.toml")
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file))
            + "\n[households]\ncar_ownership_percent = 100\n";
        RulesetLoadResult result = RulesetLoader.Parse(toml, "probe.toml");
        Assert.True(result.Ok, result.Describe());
        return result.Ruleset!;
    }

    private static Simulation Start(Ruleset rules, int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(population), GoldenFixtures.RulesetHash);
        Simulation simulation = Replay.Start(builder.Build(), rules);
        simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], rulesetHash: 0));
        return simulation;
    }

    private static int FirstStreetCarrying(World world, bool backward)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (segments.Rows.IsLive(slot) && (RoadKind)segments.Kind[slot] == RoadKind.Street
                && (backward ? segments.VolumeBackward[slot] : segments.VolumeForward[slot] + segments.VolumeBackward[slot]) > 0)
            {
                return slot;
            }
        }
        return Rows.NoSlot;
    }

    private static (int Slot, int Volume) MinVolume(World world)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        (int slot, int volume) min = (Rows.NoSlot, 0);
        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot)) { continue; }
            int v = Math.Min(segments.VolumeForward[slot], segments.VolumeBackward[slot]);
            if (v < min.volume) { min = (slot, v); }
        }
        return min;
    }

    // P1. A player bulldozes a Street with a car on it, saves, and loads. The shell's load path runs
    // CheckEndOfRun on the loaded world (Main.Menu.cs:224, CityPreparation.cs:53).
    [Fact]
    public void P1_bulldozing_under_a_car_then_saving_makes_the_load_check_throw()
    {
        Simulation simulation = Start(Driving(), 4_000);
        World world = simulation.World;
        int occupied = Rows.NoSlot;
        for (int tick = 0; tick < 512 && occupied == Rows.NoSlot; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            occupied = FirstStreetCarrying(world, backward: false);
        }
        Assert.NotEqual(Rows.NoSlot, occupied);

        // Before the edit the whole-world tier is green.
        simulation.CheckEndOfRun();

        int carried = world.Roads.Segments.VolumeForward[occupied] + world.Roads.Segments.VolumeBackward[occupied];
        world.RemoveStreet(occupied); // ApplyDemolish's Street branch (Simulation.cs:1764)

        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        World restored = SaveFile.Read(file, world.Rules, out SaveHeader header);
        var loaded = new Simulation(restored, header.Key) { VerifyDecideWritesNothing = false };

        var thrown = Assert.Throws<InvariantViolationException>(() => loaded.CheckEndOfRun());
        _output.WriteLine($"P1 Tick {world.Tick.Raw}: bulldozed Segment {occupied} carrying {carried}; load check: {thrown.Message}");
        Assert.Equal(Invariant.SegmentVolumeIsConserved, thrown.Violation.Invariant);

        // And it is transient: the cars leave the dead hop and the sum recovers.
        for (int tick = 0; tick < 2_048; tick++) { simulation.Step(new TickInput([], rulesetHash: 0)); }
        simulation.CheckEndOfRun();
        _output.WriteLine($"P1 recovered by Tick {world.Tick.Raw}");
    }

    // P2. A player lays a Street across one that has a car on it travelling backward. SplitRouteHops
    // re-points the car's current hop (World.Streets.cs:202/207), so its Leave decrements the new half
    // while the original keeps the increment. The summed invariant cannot see it.
    [Fact]
    public void P2_splitting_under_a_backward_car_drifts_volume_and_every_invariant_stays_green()
    {
        Simulation simulation = Start(Driving(), 4_000);
        World world = simulation.World;
        RoadSegmentTable segments = world.Roads.Segments;
        RoadNodeTable nodes = world.Roads.Nodes;

        int tries = 0;
        bool laid = false;
        int target = Rows.NoSlot;
        for (int tick = 0; tick < 1_024 && !laid; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            target = FirstStreetCarrying(world, backward: true);
            if (target == Rows.NoSlot) { continue; }

            int a = nodes.Rows.Resolve(segments.NodeA[target]), b = nodes.Rows.Resolve(segments.NodeB[target]);
            int ae = nodes.East[a].Raw, an = nodes.North[a].Raw, be = nodes.East[b].Raw, bn = nodes.North[b].Raw;
            int me = (ae + be) / 2, mn = (an + bn) / 2;
            StreetLayRefusal refusal = an == bn
                ? world.LayStreet(me, mn - 13, me, mn + 13, 0)
                : world.LayStreet(me - 13, mn, me + 13, mn, 0);
            tries++;
            _output.WriteLine($"P2 Tick {world.Tick.Raw}: Segment {target} backward={segments.VolumeBackward[target]} lay at ({me},{mn}) -> {refusal}");
            laid = refusal == StreetLayRefusal.None;
        }
        Assert.True(laid, $"no lay succeeded after {tries} tries");

        (int Slot, int Volume) worst = (Rows.NoSlot, 0);
        world.Invariants.Collect = true;
        for (int tick = 0; tick < 1_024; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            (int Slot, int Volume) now = MinVolume(world);
            if (now.Volume < worst.Volume) { worst = now; }
        }
        world.Invariants.RunEndOfRun(world);
        int drivers = 0;
        for (int t = 0; t < world.Travellers.Rows.SlotCount; t++)
        {
            if (world.Travellers.Rows.IsLive(t) && (TravelMode)world.Legs.Mode[world.Travellers.CurrentLeg[t]] != TravelMode.Foot) { drivers++; }
        }
        _output.WriteLine($"P2 split Segment {target}: forward={segments.VolumeForward[target]} backward={segments.VolumeBackward[target]}; "
            + $"new half {worst.Slot}: forward={segments.VolumeForward[worst.Slot]} backward={segments.VolumeBackward[worst.Slot]}; vehicular travellers now {drivers}");
        _output.WriteLine($"P2 Tick {world.Tick.Raw}: most negative direction volume {worst.Volume} on Segment {worst.Slot}; "
            + $"violations collected over staggered+end-of-run: {world.Invariants.Collected.Count}");
        Assert.True(worst.Volume < 0, "no direction went negative");
        Assert.Empty(world.Invariants.Collected);
    }

    [Fact]
    public void V1_bulldozing_a_drifted_half_breaks_conservation_for_good()
    {
        Simulation simulation = Start(Driving(), 4_000);
        World world = simulation.World;
        RoadSegmentTable segments = world.Roads.Segments;
        RoadNodeTable nodes = world.Roads.Nodes;
        bool laid = false;
        for (int tick = 0; tick < 1_024 && !laid; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            int target = FirstStreetCarrying(world, backward: true);
            if (target == Rows.NoSlot) { continue; }
            int a = nodes.Rows.Resolve(segments.NodeA[target]), b = nodes.Rows.Resolve(segments.NodeB[target]);
            int ae = nodes.East[a].Raw, an = nodes.North[a].Raw, be = nodes.East[b].Raw, bn = nodes.North[b].Raw;
            int me = (ae + be) / 2, mn = (an + bn) / 2;
            laid = (an == bn ? world.LayStreet(me, mn - 13, me, mn + 13, 0) : world.LayStreet(me - 13, mn, me + 13, mn, 0)) == StreetLayRefusal.None;
        }
        Assert.True(laid);
        (int Slot, int Volume) worst = (Rows.NoSlot, 0);
        for (int tick = 0; tick < 1_024; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            (int Slot, int Volume) now = MinVolume(world);
            if (now.Volume < worst.Volume) { worst = now; }
        }
        simulation.CheckEndOfRun();
        _output.WriteLine($"V1 Tick {world.Tick.Raw}: drifted half {worst.Slot} volume {worst.Volume}; end-of-run green before bulldoze");
        world.RemoveStreet(worst.Slot);
        int failures = 0, checks = 0;
        for (int tick = 0; tick < 4_096; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            if (tick % 256 != 255) { continue; }
            checks++;
            try { simulation.CheckEndOfRun(); }
            catch (InvariantViolationException e) { failures++; if (failures == 1) { _output.WriteLine("V1 first: " + e.Message); } }
        }
        _output.WriteLine($"V1 Tick {world.Tick.Raw}: after bulldozing the drifted half, {failures}/{checks} end-of-run checks threw");
    }

    // P3. ParkingOccupancyIsConserved is a sum: an occupancy on the wrong Car Park passes.
    [Fact]
    public void P3_occupancy_counted_on_the_wrong_car_park_passes()
    {
        var world = new World(1_000);
        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);
        world.CreateCitizen(household);

        int a = world.CarParks.Rows.Resolve(world.CarParks.Create(world.Buildings.Rows.At(0), default, Tiles.Zero, StreetSide.Right, capacity: 1));
        int b = world.CarParks.Rows.Resolve(world.CarParks.Create(world.Buildings.Rows.At(0), default, Tiles.Zero, StreetSide.Right, capacity: 1));

        // The holder names A; the occupancy was moved on B.
        world.Citizens.ParkedIn[0] = world.CarParks.Rows.At(a);
        world.CarParks.Move(b, 1);

        world.Invariants.RunEndOfRun(world);
        _output.WriteLine($"P3 A occupied {world.CarParks.Occupied[a]}/1 with 1 holder, B occupied {world.CarParks.Occupied[b]}/1 with 0 holders: end-of-run green");
    }

    // M1. Cost of the staggered tier per Tick and of CheckEndOfRun, on a stepped world.
    [Theory]
    [InlineData("declining.toml", 4_000, true)]
    [InlineData("minimal.toml", 4_000, false)]
    public void M1_invariant_tier_costs(string file, int population, bool cars)
    {
        Ruleset rules;
        if (cars) { rules = Driving(file); }
        else
        {
            RulesetLoadResult result = RulesetLoader.Parse(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file)), file);
            Assert.True(result.Ok, result.Describe());
            rules = result.Ruleset!;
        }

        Simulation simulation = Start(rules, population);
        World world = simulation.World;

        var step = Stopwatch.StartNew();
        const int warm = 2_048;
        for (int tick = 0; tick < warm; tick++) { simulation.Step(new TickInput([], rulesetHash: 0)); }
        step.Stop();

        // The three registered sweeps, called slice by slice without advancing the world, so the
        // wheel never falls behind the clock. Same work RunStaggered does on each Tick.
        const int cycles = 20;
        int slices = world.Invariants.Slices;
        InvariantRegistry registry = world.Invariants;
        var staggered = Stopwatch.StartNew();
        for (int i = 0; i < cycles * slices; i++)
        {
            int slice = i % slices;
            WorldInvariants.HouseholdsLiveSomewhereReal(world, slice, slices, registry);
            WorldInvariants.CitizensBelongToARealHousehold(world, slice, slices, registry);
            WorldInvariants.SegmentsAreWellFormed(world, slice, slices, registry);
        }
        staggered.Stop();

        simulation.CheckEndOfRun(); // warm the JIT
        const int walks = 10;
        var endOfRun = Stopwatch.StartNew();
        for (int i = 0; i < walks; i++) { simulation.CheckEndOfRun(); }
        endOfRun.Stop();

        double perStepUs = step.Elapsed.TotalMicroseconds / warm;
        double perSliceUs = staggered.Elapsed.TotalMicroseconds / (cycles * slices);
        double perWalkMs = endOfRun.Elapsed.TotalMilliseconds / walks;
        _output.WriteLine(
            $"M1 {file} pop={population} cars={cars} Tick={world.Tick.Raw} citizens={world.Citizens.Rows.LiveCount} "
            + $"households={world.Households.Rows.LiveCount} segments={world.Roads.Segments.Rows.LiveCount} "
            + $"bins={world.Bins.Rows.LiveCount} rules={world.RuleInstances.Rows.LiveCount} routeWorkers={simulation.RouteWorkerCount} | "
            + $"mean Step {perStepUs:F1} us over first {warm} Ticks (incl. staggered) | "
            + $"staggered slice {perSliceUs:F2} us ({slices} slices, {cycles} cycles) | "
            + $"CheckEndOfRun {perWalkMs:F2} ms (mean of {walks})");
    }
}
