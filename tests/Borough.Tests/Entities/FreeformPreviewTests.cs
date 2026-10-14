using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

public sealed class FreeformPreviewTests
{
    private static Ruleset Rules() =>
        RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;

    private static (World World, Simulation Simulation) Empty(params Command[] streets) => Empty(Rules(), streets);

    private static (World World, Simulation Simulation) Empty(Ruleset rules, params Command[] streets)
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(0, rules, key);
        var simulation = new Simulation(world, key);
        simulation.Step(new TickInput([new Command(CommandKind.Ground, default, default)], 0));
        foreach (Command street in streets) { simulation.Step(new TickInput([street], 0)); }
        return (world, simulation);
    }

    private static Command Street(int aEast, int aNorth, int bEast, int bNorth, int sagittaTiles = 0) =>
        Command.Street(new Tiles(aEast), new Tiles(aNorth), new Tiles(bEast), new Tiles(bNorth),
            new SubTiles(sagittaTiles * Fixed.One));

    private static Command Zone(int east, int north, ushort zone = LotTable.Housing) =>
        new(CommandKind.Zone, new Tiles(east), new Tiles(north), zone);

    private static Command[] Loop(params (int East, int North)[] corners) =>
        [.. corners.Select((a, i) => Street(a.East, a.North, corners[(i + 1) % corners.Length].East, corners[(i + 1) % corners.Length].North))];

    private static readonly Command[] RightTriangle = Loop((100, 100), (164, 100), (100, 164));

    private static readonly Command[] Rectangle = Loop((100, 100), (190, 100), (190, 150), (100, 150));

    // The corner at (200, 100) is atan(60 / 100), about 31 degrees.
    private static readonly Command[] SharpTriangle = Loop((100, 100), (200, 100), (100, 160));

    private static readonly Command[] OpenCorner = [Street(300, 300, 364, 300), Street(300, 300, 300, 364)];

    // One face over four lattice squares: lattice Streets on its outside, none inside.
    private static readonly Command[] LatticeRing = Loop((128, 128), (160, 128), (192, 128), (192, 160), (192, 192), (160, 192), (128, 192), (128, 160));

    // Lattice Streets on two sides, closed by a freeform diagonal.
    private static readonly Command[] LatticeWedge = Loop((128, 128), (160, 128), (192, 128), (128, 192), (128, 160));

    [Fact]
    public void A_right_angle_corner_previews_the_lots_its_paint_creates() =>
        PreviewMatchesCommit(Rectangle, [], (140, 125));

    [Fact]
    public void A_right_triangle_previews_the_lots_its_paint_creates() =>
        PreviewMatchesCommit(RightTriangle, [], (110, 110));

    [Fact]
    public void A_sharp_triangle_previews_the_lots_its_paint_creates() =>
        PreviewMatchesCommit(SharpTriangle, [], (120, 115));

    [Fact]
    public void An_open_corner_previews_the_lots_its_paint_creates() =>
        PreviewMatchesCommit(OpenCorner, [], (310, 310));

    [Fact]
    public void A_street_side_previews_the_lots_its_paint_creates() =>
        PreviewMatchesCommit([Street(300, 100, 364, 100, 6)], [], (332, 112));

    [Fact]
    public void Ground_beside_standing_lots_previews_only_the_lots_still_free() =>
        PreviewMatchesCommit([.. RightTriangle, Street(164, 100, 164, 164), Street(164, 164, 100, 164)], [Zone(110, 110)], (150, 150));

    [Fact]
    public void A_face_over_several_lattice_squares_previews_every_lot_its_paint_creates()
    {
        (World world, _) = Empty(LatticeRing);
        StreetGrid streets = world.Roads.Streets;
        Assert.NotEqual(Rows.NoSlot, streets.Horizontal(4, 4));
        Assert.NotEqual(Rows.NoSlot, streets.Vertical(6, 5));
        Assert.True(world.TryZoneGround(new Tiles(128), new Tiles(128), out ZoneGround ground));
        Assert.Equal(new LandRectangle(128, 128, 64, 64), ground.Bounds);

        PreviewMatchesCommit(LatticeRing, [], (128, 128));
    }

    [Fact]
    public void A_face_partly_on_the_lattice_previews_the_lots_its_paint_creates() =>
        PreviewMatchesCommit(LatticeWedge, [], (140, 140));

    [Fact]
    public void A_whole_block_stroke_previews_each_command_after_the_ones_before_it() =>
        PreviewMatchesCommit(LatticeRing, [], (128, 128), (160, 128), (128, 160), (160, 160));

    // Each block's paint is one record, so a ninth block exceeds a budget of eight.
    [Fact]
    public void A_whole_block_stroke_previews_the_paints_the_record_budget_refuses()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "freeform-preview-eight-records.toml");
        File.WriteAllText(path, File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"))
            .Replace("max_records = 1048576", "max_records = 8", StringComparison.Ordinal));
        Ruleset eight = RulesetLoader.Load(path).Ruleset!;
        Assert.Equal(8, eight.PermissionRecordLimit);

        var grid = new List<Command>();
        for (int line = 128; line <= 224; line += 32)
            for (int from = 128; from < 224; from += 32)
            {
                grid.Add(Street(from, line, from + 32, line));
                grid.Add(Street(line, from, line, from + 32));
            }
        var blocks = new List<(int, int)>();
        for (int north = 128; north < 224; north += 32)
            for (int east = 128; east < 224; east += 32) { blocks.Add((east, north)); }

        int created = PreviewMatchesCommit(eight, [.. grid], [], [.. blocks]);
        Assert.True(created < PreviewMatchesCommit(Rules(), [.. grid], [], [.. blocks]));
    }

    // The shell keys its preview cache on these two numbers instead of the Tick.
    [Fact]
    public void An_erase_that_only_frees_paint_records_still_changes_the_preview_key()
    {
        (World world, Simulation simulation) = Empty(RightTriangle);
        simulation.Step(new TickInput([Zone(110, 110)], 0));
        var rows = world.PermissionRectangles.Rows;
        (ulong ids, int live) = (rows.NextId, rows.LiveCount);

        simulation.Step(new TickInput([Zone(110, 110, 0)], 0));

        Assert.Equal(ids, rows.NextId);
        Assert.NotEqual(live, rows.LiveCount);
    }

    [Fact]
    public void Erasing_previews_no_lots()
    {
        (World world, _) = Empty(RightTriangle);
        var plots = new List<Parcel>();
        Assert.Equal(0, LotSubdivider.PreviewPaint(world, [(new Tiles(110), new Tiles(110))], 0, plots));
        Assert.Empty(plots);
    }

    private static void PreviewMatchesCommit(Command[] streets, Command[] before, params (int East, int North)[] points) =>
        PreviewMatchesCommit(Rules(), streets, before, points);

    private static int PreviewMatchesCommit(Ruleset rules, Command[] streets, Command[] before, params (int East, int North)[] points)
    {
        (World world, Simulation simulation) = Empty(rules, streets);
        foreach (Command command in before) { simulation.Step(new TickInput([command], 0)); }
        var standing = new HashSet<ulong>(Live(world).Select(world.Lots.Rows.IdAt));
        ulong hash = world.HashState();

        var plots = new List<Parcel>();
        int previewed = LotSubdivider.PreviewPaint(world, [.. points.Select(p => (new Tiles(p.East), new Tiles(p.North)))],
            LotTable.Housing, plots);

        Assert.Equal(hash, world.HashState());
        simulation.Step(new TickInput([.. points.Select(p => Zone(p.East, p.North))], 0));
        var created = Live(world).Where(lot => !standing.Contains(world.Lots.Rows.IdAt(lot)))
            .OrderBy(world.Lots.Rows.IdAt)
            .Select(lot => (world.Lots.Parcel(lot), (StreetSide)world.Lots.Side[lot])).ToArray();
        Assert.NotEmpty(created);
        Assert.Equal(created.Length, previewed);
        Assert.Equal(created, plots.Select(p => (p.Geometry, p.Side)));
        return created.Length;
    }

    private static IEnumerable<int> Live(World world) =>
        Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive);
}
