using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Input;

/// <summary>
/// <c>demolish</c> with <see cref="DemolishTarget.Street"/>: one Segment goes, its bare Nodes go
/// with it, and the Lots it fronted follow <c>adr/0079</c>.
/// </summary>
public sealed class StreetRemovalTests
{
    private const ulong ScenarioEnd = 400;

    private static readonly Command Spur =
        Command.Demolish(new Tiles(41), new Tiles(105), DemolishTarget.Street);

    private static Simulation Scenario() => FreeformStreetsAcceptanceTests.RunTo(
        InputLogCodec.FromText(FreeformStreetsAcceptanceTests.Scenario), FreeformStreetsAcceptanceTests.Rules(), ScenarioEnd);

    private static readonly InputLog Log = InputLogCodec.FromText(FreeformStreetsAcceptanceTests.Scenario);

    private static void Step(Simulation simulation, params Command[] commands) =>
        simulation.Step(new TickInput(commands, Log.RulesetHashAt(Ticks.Zero)));

    private static int[] Fronting(World world, int segment) =>
        [.. Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(lot => world.Lots.Rows.IsLive(lot) && world.Lots.FrontageOn(lot) == segment)];

    private static (int East, int North) TileAt(StreetArc line, int offset)
    {
        var point = line.PointAt(offset);
        return ((int)IntegerMath.ShiftRight(point.East, Fixed.FractionalBits), (int)IntegerMath.ShiftRight(point.North, Fixed.FractionalBits));
    }

    /// <summary>Asserts the removal outcome for every Lot that fronted the removed Segment.</summary>
    private static void AssertFollowsFrontageRule(World world, IEnumerable<(ulong Lot, bool Occupied)> fronted)
    {
        foreach ((ulong id, bool occupied) in fronted)
        {
            int slot = Enumerable.Range(0, world.Lots.Rows.SlotCount).FirstOrDefault(
                s => world.Lots.Rows.IsLive(s) && world.Lots.Rows.IdAt(s) == id, -1);
            if (!occupied)
            {
                Assert.True(slot < 0, $"vacant Lot {id} lost its Street and must be freed.");
                continue;
            }

            Assert.True(slot >= 0, $"occupied Lot {id} must keep standing.");
            Assert.True(world.Lots.BuildingOn(slot) >= 0);
            Assert.False(world.Lots.HasFrontage(slot));
        }
    }

    [Fact]
    public void Removing_a_dead_end_spur_frees_its_far_node_and_keeps_the_halves_it_split()
    {
        Simulation simulation = Scenario();
        World world = simulation.World;
        int spur = world.StreetAt(Spur.East, Spur.North);
        Assert.True(spur >= 0);
        Assert.Equal((50L * Fixed.One, 114L * Fixed.One), world.Roads.Segments.Centerline[spur].B);
        var removed = world.Roads.Segments.Rows.At(spur);
        var fronted = Fronting(world, spur).Select(l => (world.Lots.Rows.IdAt(l), world.Lots.BuildingOn(l) >= 0)).ToArray();
        Assert.NotEmpty(fronted);
        int segments = world.Roads.Segments.Rows.LiveCount, nodes = world.Roads.Nodes.Rows.LiveCount;

        Assert.Equal(Refusal.None, simulation.Refuses(Spur));
        Step(simulation, Spur);

        Assert.Equal(0, simulation.CommandsRefused);
        Assert.False(world.Roads.Segments.Rows.TryResolve(removed, out _));
        Assert.Equal(segments - 1, world.Roads.Segments.Rows.LiveCount);
        Assert.Equal(nodes - 1, world.Roads.Nodes.Rows.LiveCount);
        Assert.True(world.StreetAt(new Tiles(32), new Tiles(96)) >= 0);
        AssertFollowsFrontageRule(world, fronted);
    }

    [Fact]
    public void Removing_an_occupied_lattice_street_leaves_its_buildings_standing_unfronted()
    {
        Simulation simulation = Scenario();
        World world = simulation.World;
        int segment = Enumerable.Range(0, world.Roads.Segments.Rows.SlotCount).First(s =>
            world.Roads.Segments.Rows.IsLive(s) && world.Roads.Segments.Centerline[s].IsStraight
            && Fronting(world, s).Any(l => world.Lots.BuildingOn(l) >= 0)
            && Fronting(world, s).Any(l => world.Lots.BuildingOn(l) < 0));
        StreetArc line = world.Roads.Segments.Centerline[segment];
        (int east, int north) = TileAt(line, line.Length / 2);
        var command = Command.Demolish(new Tiles(east), new Tiles(north), DemolishTarget.Street);
        Assert.Equal(segment, world.StreetAt(command.East, command.North));
        var fronted = Fronting(world, segment).Select(l => (world.Lots.Rows.IdAt(l), world.Lots.BuildingOn(l) >= 0)).ToArray();
        int buildings = world.Buildings.Rows.LiveCount;

        Step(simulation, command);

        Assert.Equal(0, simulation.CommandsRefused);
        Assert.False(world.Roads.Segments.Rows.IsLive(segment) && world.Roads.Segments.Centerline[segment].A == line.A);
        Assert.Equal(buildings, world.Buildings.Rows.LiveCount);
        AssertFollowsFrontageRule(world, fronted);
    }

    [Fact]
    public void Opening_a_closed_block_keeps_its_occupied_lots_and_frees_its_vacant_ones()
    {
        Simulation simulation = FreeformStreetsAcceptanceTests.RunTo(
            InputLogCodec.FromText(FreeformStreetsAcceptanceTests.EmptyGround), FreeformStreetsAcceptanceTests.Rules(), 397);
        World world = simulation.World;
        Step(simulation, new Command(CommandKind.People, default, default));
        Assert.True(world.TryZoneGround(new Tiles(290), new Tiles(300), out ZoneGround closed));
        Assert.True(closed.Face >= 0 && world.Roads.Faces.IsClosed(closed.Face));

        var command = Command.Demolish(new Tiles(288), new Tiles(314), DemolishTarget.Street);
        int closing = world.StreetAt(command.East, command.North);
        Assert.True(closing >= 0);
        var fronted = Fronting(world, closing).Select(l => (world.Lots.Rows.IdAt(l), world.Lots.BuildingOn(l) >= 0)).ToArray();
        Assert.Contains(fronted, f => f.Item2);
        var occupied = Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .Where(l => world.Lots.Rows.IsLive(l) && world.Lots.BuildingOn(l) >= 0)
            .ToDictionary(world.Lots.Rows.IdAt, world.Lots.Parcel);

        Step(simulation, command);

        Assert.Equal(0, simulation.CommandsRefused);
        Assert.False(world.TryZoneGround(new Tiles(290), new Tiles(300), out ZoneGround open) && open.Face >= 0
            && world.Roads.Faces.IsClosed(open.Face));
        AssertFollowsFrontageRule(world, fronted);
        foreach (var (id, parcel) in occupied)
        {
            int slot = Enumerable.Range(0, world.Lots.Rows.SlotCount).Single(s => world.Lots.Rows.IsLive(s) && world.Lots.Rows.IdAt(s) == id);
            Assert.Equal(parcel, world.Lots.Parcel(slot));
        }
    }

    [Fact]
    public void A_street_removal_survives_the_log_and_replays_saves_reloads_and_runs_on_any_worker_count_to_one_city()
    {
        InputLog log = InputLogCodec.FromText(FreeformStreetsAcceptanceTests.Scenario + $"\n{ScenarioEnd} demolish 41 105 1");
        Command read = log.At(new Ticks(ScenarioEnd)).ToArray().Single();
        Assert.Equal((CommandKind.Demolish, Spur.East, Spur.North, (ushort)DemolishTarget.Street),
            (read.Kind, read.East, read.North, read.Zone));

        Ruleset rules = FreeformStreetsAcceptanceTests.Rules();
        Simulation serial = FreeformStreetsAcceptanceTests.RunTo(log, rules, ScenarioEnd);
        Simulation parallel = Replay.Start(log, rules);
        parallel.RouteWorkerCount = 4;
        for (ulong at = 0; at < ScenarioEnd; at++)
        {
            var now = new Ticks(at);
            parallel.Step(new TickInput(log.At(now), log.RulesetHashAt(now)));
        }

        var file = new MemorySave();
        SaveFile.Write(serial.World, 0, file);
        var resumed = new Simulation(SaveFile.Read(file, rules, out _), WorldKey.FromSeed(0));

        for (ulong at = ScenarioEnd; at < ScenarioEnd + 100; at++)
        {
            var now = new Ticks(at);
            var input = new TickInput(log.At(now), log.RulesetHashAt(now));
            serial.Step(input); parallel.Step(input); resumed.Step(input);
            Assert.Equal(serial.World.HashState(), parallel.World.HashState());
            Assert.Equal(serial.World.HashState(), resumed.World.HashState());
        }

        Assert.Equal(0, serial.CommandsRefused);
        serial.CheckEndOfRun(); parallel.CheckEndOfRun(); resumed.CheckEndOfRun();
    }
}
