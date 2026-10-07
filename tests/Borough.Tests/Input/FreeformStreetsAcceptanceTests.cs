using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Rules;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Persistence;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Input;

/// <summary>
/// The freeform Streets acceptance scenario, driven through the shell's Street tool on
/// <c>minimal.toml</c> at 1,000 Citizens and replayed here from its Input Log.
/// </summary>
/// <remarks>
/// The log clears lattice Streets west of Tile 64 between north 64 and 192, lays a diagonal that
/// cuts triangular blocks, a dead-end spur inside one, and an open curve, then zones the eight
/// blocks. <see cref="Closure"/> later joins the curve's end to the lattice Street at north 128.
/// </remarks>
public sealed class FreeformStreetsAcceptanceTests
{
    private const string Scenario = """
        borough-log 1
        seed 0x0000000000000000
        citizens 1000
        ruleset 0x91EC749C49011D57
        --
        0 populate 0 0 0
        310 connect 32 64 3
        310 connect 32 64 15
        312 connect 0 96 2
        312 connect 0 96 6
        314 connect 0 160 2
        314 connect 0 160 6
        316 connect 0 128 3
        316 connect 0 128 7
        318 connect 0 192 2
        318 connect 0 192 6
        326 street 0 128 64 64 0
        334 street 32 96 50 114 0
        344 street 64 176 46 158 -325258
        350 zone 16 80 1
        350 zone 48 80 1
        350 zone 16 112 1
        350 zone 48 112 1
        350 zone 16 144 1
        350 zone 48 144 1
        350 zone 16 176 1
        350 zone 48 176 1
        """;

    private const ulong ClosureTick = 400;

    private static readonly Command Closure =
        Command.Street(new Tiles(46), new Tiles(158), new Tiles(47), new Tiles(128), SubTiles.Zero);

    private static Ruleset Rules() =>
        RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;

    private static Simulation RunTo(InputLog log, Ruleset rules, ulong tick)
    {
        Simulation simulation = Replay.Start(log, rules);
        for (ulong at = 0; at < tick; at++)
        {
            var now = new Ticks(at);
            simulation.Step(new TickInput(log.At(now), log.RulesetHashAt(now)));
        }

        return simulation;
    }

    [Fact]
    public void Closing_a_loop_keeps_every_lot_it_does_not_cross_and_lots_never_overlap()
    {
        Ruleset rules = Rules();
        InputLog log = InputLogCodec.FromText(Scenario);
        Simulation simulation = RunTo(log, rules, ClosureTick);
        World world = simulation.World;
        Assert.True(StreetArc.TryCreate(Closure.East.Raw, Closure.North.Raw, Closure.EndEast.Raw, Closure.EndNorth.Raw,
            0, out StreetArc closure));
        var crossed = world.LotsUnder(closure).Select(world.Lots.Rows.IdAt).ToHashSet();
        var before = Snapshot(world);
        Assert.Contains(Enumerable.Range(0, world.Lots.Rows.SlotCount), s => world.Lots.Rows.IsLive(s)
            && world.Lots.FrontageOn(s) >= 0 && !world.Roads.Segments.Centerline[world.Lots.FrontageOn(s)].IsStraight);

        simulation.Step(new TickInput([Closure], log.RulesetHashAt(new Ticks(ClosureTick))));

        var after = Snapshot(world);
        Assert.Single(crossed);
        foreach (var (id, lot) in before)
        {
            if (crossed.Contains(id)) { Assert.False(after.ContainsKey(id)); continue; }
            Assert.True(after.TryGetValue(id, out var kept), $"Lot {id} was not crossed and must survive.");
            Assert.Equal((lot.Parcel, lot.Building), (kept.Parcel, kept.Building));
        }
        Assert.NotEmpty(after.Keys.Except(before.Keys));

        int[] live = Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive).ToArray();
        foreach (int a in live)
        {
            Assert.DoesNotContain(live, b => b > a && world.Lots.Parcel(a).Overlaps(world.Lots.Parcel(b)));
            int segment = world.Lots.FrontageOn(a);
            if (segment < 0) continue;
            Assert.InRange(world.Lots.FrontageOffset[a].Raw * Fixed.One, 0, world.Roads.Segments.Centerline[segment].Length);
        }
    }

    [Fact]
    public void The_scenario_replays_saves_reloads_and_runs_on_any_worker_count_to_one_city()
    {
        Ruleset rules = Rules();
        InputLog log = InputLogCodec.FromText(Scenario);
        Simulation serial = RunTo(log, rules, ClosureTick);
        Simulation parallel = Replay.Start(log, rules);
        parallel.RouteWorkerCount = 4;
        for (ulong at = 0; at < ClosureTick; at++)
        {
            var now = new Ticks(at);
            parallel.Step(new TickInput(log.At(now), log.RulesetHashAt(now)));
        }
        Assert.Equal(serial.World.HashState(), parallel.World.HashState());

        var file = new MemorySave();
        SaveFile.Write(serial.World, 0, file);
        var resumed = new Simulation(SaveFile.Read(file, rules, out _), WorldKey.FromSeed(0));

        for (ulong at = ClosureTick; at < ClosureTick + 200; at++)
        {
            Command[] commands = at == ClosureTick ? [Closure] : [];
            var input = new TickInput(commands, log.RulesetHashAt(new Ticks(at)));
            serial.Step(input); parallel.Step(input); resumed.Step(input);
            Assert.Equal(serial.World.HashState(), parallel.World.HashState());
            Assert.Equal(serial.World.HashState(), resumed.World.HashState());
        }
        serial.CheckEndOfRun(); parallel.CheckEndOfRun(); resumed.CheckEndOfRun();
    }

    private static Dictionary<ulong, (OrientedRectangle Parcel, int Building)> Snapshot(World world) =>
        Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive)
            .ToDictionary(s => world.Lots.Rows.IdAt(s), s => (world.Lots.Parcel(s), world.Lots.BuildingOn(s)));
}
