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
/// cuts triangular blocks, a dead-end spur inside one, and an open curve, then zones its blocks and
/// the curve's sides. <see cref="Closure"/> later joins the curve's end to the lattice Street at
/// north 128. <see cref="EmptyGround"/> repeats the layouts on ground with no lattice.
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
        350 zone 48 112 1
        350 zone 16 144 1
        350 zone 40 140 1
        350 zone 50 150 1
        350 zone 50 176 1
        350 zone 58 166 1
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

    /// <summary>
    /// A continuous chain driven through the shell's Street tool on an empty <c>minimal.toml</c>
    /// world: a straight Street east, then a continuous one turning north from its end.
    /// </summary>
    private const string Chain = """
        borough-log 1
        seed 0x0000000000000000
        citizens 1000
        ruleset 0x91EC749C49011D57
        --
        0 ground 0 0 0
        260 street 1000 1000 1038 1000 0
        264 street 1038 1000 1076 1029 -529414
        """;

    [Fact]
    public void A_continuous_street_leaves_along_the_tangent_the_street_before_it_ends_on()
    {
        World world = RunTo(InputLogCodec.FromText(Chain), Rules(), 270).World;
        int[] live = Enumerable.Range(0, world.Roads.Segments.Rows.SlotCount).Where(world.Roads.Segments.Rows.IsLive).ToArray();
        StreetArc first = world.Roads.Segments.Centerline[live.Single(s => world.Roads.Segments.Centerline[s].IsStraight)];
        StreetArc second = world.Roads.Segments.Centerline[live.Single(s => !world.Roads.Segments.Centerline[s].IsStraight)];

        var leaving = first.TangentAt(first.Length);
        var joining = second.TangentAt(0);

        Assert.Equal(first.B, second.A);
        Assert.InRange(joining.East - leaving.East, -Fixed.One / 256, Fixed.One / 256);
        Assert.InRange(joining.North - leaving.North, -Fixed.One / 256, Fixed.One / 256);
    }

    private const string EmptyGround = """
        borough-log 1
        seed 0x0000000000000000
        citizens 1000
        ruleset 0x91EC749C49011D57
        --
        0 ground 0 0 0
        314 street 100 100 164 100 0
        318 street 164 100 190 144 0
        322 street 190 144 135 177 0
        326 street 135 177 100 100 0
        330 street 135 100 140 138 0
        338 street 250 100 250 160 -786432
        344 street 100 250 177 250 0
        348 street 177 250 101 319 0
        352 street 101 319 100 250 0
        356 street 250 250 327 250 0
        360 street 327 250 327 314 0
        364 street 250 250 250 314 0
        374 zone 130 130 1
        376 zone 135 92 1
        378 zone 182 118 1
        380 zone 248 130 1
        382 zone 268 130 1
        384 zone 115 265 1
        386 zone 290 258 1
        388 zone 322 285 1
        390 zone 258 285 1
        392 zone 290 242 1
        394 street 250 314 327 314 0
        396 zone 290 300 1
        """;

    [Fact]
    public void Zone_on_empty_ground_lots_every_layout_and_a_closure_turns_strips_into_a_block()
    {
        InputLog log = InputLogCodec.FromText(EmptyGround);
        Simulation simulation = RunTo(log, Rules(), 394);
        World world = simulation.World;

        int Near(int east, int north) => Enumerable.Range(0, world.Lots.Rows.SlotCount).Count(lot =>
            world.Lots.Rows.IsLive(lot) && OrientedRectangle.FromBounds(new LandRectangle(east - 48, north - 48, 96, 96)).Overlaps(world.Lots.Parcel(lot)));
        Assert.True(Near(140, 135) > 0, "loop with a spur");
        Assert.True(Near(250, 130) > 0, "open curve");
        Assert.True(Near(125, 275) > 0, "triangle");
        Assert.True(Near(290, 280) > 0, "U before closure");
        Assert.Equal(0, world.LandPermissions.At(290, 300).Uses);

        Assert.False(world.TryZoneGround(new Tiles(290), new Tiles(300), out ZoneGround open) && open.Face >= 0);
        simulation.Step(new TickInput(log.At(new Ticks(394)), log.RulesetHashAt(new Ticks(394))));
        simulation.Step(new TickInput([], log.RulesetHashAt(new Ticks(395))));
        Assert.True(world.TryZoneGround(new Tiles(290), new Tiles(300), out ZoneGround closed));
        Assert.True(closed.Face >= 0 && world.Roads.Faces.IsClosed(closed.Face));
        simulation.Step(new TickInput(log.At(new Ticks(396)), log.RulesetHashAt(new Ticks(396))));
        Assert.Equal(LotTable.Housing, world.LandPermissions.At(290, 300).Uses);

        int[] live = Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive).ToArray();
        foreach (int a in live)
            Assert.DoesNotContain(live, b => b > a && world.Lots.Parcel(a).Overlaps(world.Lots.Parcel(b)));
    }

    /// <summary>
    /// Batch gestures driven through the shell's Street tool on an empty <c>minimal.toml</c> world:
    /// a 2 × 2 grid of 32-Tile blocks, then a straight and a curved Street each with a parallel copy
    /// at the default 32-Tile offset.
    /// </summary>
    private const string Batch = """
        borough-log 1
        seed 0x0000000000000000
        citizens 1000
        ruleset 0x91EC749C49011D57
        --
        0 ground 0 0 0
        262 street 1000 1000 1064 1000 0
        262 street 1000 1032 1064 1032 0
        262 street 1000 1064 1064 1064 0
        262 street 1000 1000 1000 1064 0
        262 street 1032 1000 1032 1064 0
        262 street 1064 1000 1064 1064 0
        270 street 1200 1000 1264 1000 0
        270 street 1200 1032 1264 1032 0
        280 street 1400 1000 1464 1000 786432
        280 street 1379 1024 1485 1024 1303538
        """;

    [Fact]
    public void A_grid_gesture_closes_its_blocks_and_parallel_copies_keep_their_offset()
    {
        World world = RunTo(InputLogCodec.FromText(Batch), Rules(), 290).World;
        StreetArc[] lines = Enumerable.Range(0, world.Roads.Segments.Rows.SlotCount).Where(world.Roads.Segments.Rows.IsLive)
            .Select(s => world.Roads.Segments.Centerline[s]).ToArray();
        StreetArc Starting(int east, int north) =>
            lines.Single(l => l.A == ((long)east * Fixed.One, (long)north * Fixed.One));

        int[] blocks = [.. new[] { (1_016, 1_016), (1_048, 1_016), (1_016, 1_048), (1_048, 1_048) }.Select(b =>
        {
            Assert.True(world.TryZoneGround(new Tiles(b.Item1), new Tiles(b.Item2), out ZoneGround ground));
            Assert.True(ground.Face >= 0 && world.Roads.Faces.IsClosed(ground.Face));
            return ground.Face;
        })];
        Assert.Equal(4, blocks.Distinct().Count());

        StreetArc straight = Starting(1_200, 1_000), straightCopy = Starting(1_200, 1_032);
        Assert.Equal(32L * Fixed.One, straight.DistanceTo(straightCopy.A.East, straightCopy.A.North));
        Assert.Equal(32L * Fixed.One, straight.DistanceTo(straightCopy.B.East, straightCopy.B.North));

        StreetArc curve = Starting(1_400, 1_000), curveCopy = Starting(1_379, 1_024);
        Assert.InRange(curveCopy.Center.East - curve.Center.East, -Fixed.One, Fixed.One);
        Assert.InRange(curveCopy.Center.North - curve.Center.North, -Fixed.One, Fixed.One);
        Assert.InRange(curveCopy.Radius - curve.Radius, 31L * Fixed.One, 33L * Fixed.One);
    }

    private static Dictionary<ulong, (OrientedRectangle Parcel, int Building)> Snapshot(World world) =>
        Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive)
            .ToDictionary(s => world.Lots.Rows.IdAt(s), s => (world.Lots.Parcel(s), world.Lots.BuildingOn(s)));
}
