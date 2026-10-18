using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>A block spanning several lattice squares carves as one block.</summary>
public sealed class MergedBlockTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static World Populated()
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "rowhouses.toml"));
        Ruleset rules = result.Ruleset ?? throw new InvalidOperationException(result.Describe());
        var world = new World(1_000, rules);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        return world;
    }

    // A 2×2 patch whose twelve lattice edges all stand and whose ground carries no Lot or paint.
    private static (int Column, int Row) EmptyPatch(World world)
    {
        StreetGrid streets = world.Roads.Streets;
        for (int row = 0; row + 1 < streets.Blocks; row++)
        {
            for (int column = 0; column + 1 < streets.Blocks; column++)
            {
                if (Whole(streets, column, row) && Bare(world, column, row)) { return (column, row); }
            }
        }
        throw new InvalidOperationException("no bare 2×2 patch of standing Streets.");
    }

    private static bool Whole(StreetGrid streets, int column, int row)
    {
        for (int i = 0; i < 2; i++)
        {
            for (int line = 0; line < 3; line++)
            {
                if (streets.Horizontal(column + i, row + line) == Rows.NoSlot
                    || streets.Vertical(column + line, row + i) == Rows.NoSlot) { return false; }
            }
        }
        return true;
    }

    private static bool Bare(World world, int column, int row)
    {
        BlockGround a = BlockGround.At(world.Roads.Lattice, column, row);
        BlockGround b = BlockGround.At(world.Roads.Lattice, column + 1, row + 1);
        var area = new LandRectangle(a.East, a.North, b.East + b.Wide - a.East, b.North + b.Deep - a.North);
        if (world.LandPermissions.Summary(area).AnyUses != 0) { return false; }
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (world.Lots.Rows.IsLive(lot) && Overlaps(area, world.Lots.ParcelBounds(lot))) { return false; }
        }
        return true;
    }

    private static bool Overlaps(LandRectangle a, LandRectangle b) =>
        a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;

    [Fact]
    public void A_two_by_two_block_carves_both_Segments_of_every_face_and_leaves_its_middle_free()
    {
        World world = Populated();
        StreetGrid streets = world.Roads.Streets;
        (int column, int row) = EmptyPatch(world);

        Assert.True(world.Roads.BulldozeStreet(column, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row, StreetAxis.North));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row + 1, StreetAxis.North));
        world.RebuildDerived();

        BlockGround block = streets.Block(column + 1, row + 1);
        Assert.Equal((column, row), (block.Column, block.Row));
        Assert.Equal(2 * streets.BlockTiles, block.Wide);
        Assert.Equal(2 * streets.BlockTiles, block.Deep);

        int first = world.Lots.Rows.SlotCount;
        Assert.True(LotSubdivider.SubdivideBlock(world, column + 1, row + 1, LotTable.Housing) > 0);

        var fronted = new HashSet<(BlockFace, int)>();
        var middle = new LandRectangle(block.East + (block.Wide / 2) - 2, block.North + (block.Deep / 2) - 2, 4, 4);
        for (int lot = first; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot)) { continue; }
            Assert.True(Frontage.BlockOf(streets, world.Lots.East[lot], world.Lots.North[lot],
                (StreetSide)world.Lots.Side[lot], out int at, out int on, out BlockFace face));
            Assert.Equal((column, row), (at, on));
            int segment = world.Lots.FrontageOn(lot);
            fronted.Add((face, segment));
            Assert.Equal(segment, Frontage.Locate(streets, world.Lots.East[lot], world.Lots.North[lot], out Tiles along));
            Assert.Equal(along, world.Lots.FrontageOffset[lot]);

            Assert.False(Overlaps(middle, world.Lots.ParcelBounds(lot)));
        }

        Assert.Equal(8, fronted.Count);
    }

    [Fact]
    public void The_generator_lays_merged_blocks_inside_the_town_beyond_its_dense_core_and_carves_them()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "rowhouses.toml");
        string text = File.ReadAllText(path);
        int roads = text.IndexOf("[roads]\n", StringComparison.Ordinal);
        Assert.True(roads >= 0);
        text = text.Insert(roads + "[roads]\n".Length, "dense_core_percent = 30\nmerged_block_percent = 100\n");
        RulesetLoadResult result = RulesetLoader.Parse(text, "rowhouses.toml");
        Assert.True(result.Ok, result.Describe());
        var world = new World(1_000, result.Ruleset!);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        StreetGrid streets = world.Roads.Streets;
        int merged = 0, carved = 0;
        for (int row = 0; row < streets.Blocks; row++)
        {
            for (int column = 0; column < streets.Blocks; column++)
            {
                BlockGround block = streets.Block(column, row);
                if (block.Column != column || block.Row != row || block.Wide == streets.Lattice.WidthOf(column)) { continue; }
                Assert.Equal(2 * streets.BlockTiles, block.Wide);
                Assert.Equal(2 * streets.BlockTiles, block.Deep);
                merged++;
                if (LotSubdivider.PatternOn(world, column, row, out _)) { carved++; }
            }
        }

        Assert.True(merged > 0, "the generator laid no merged block.");
        Assert.True(carved > 0, "no merged block carries Lots.");
        int west = int.MaxValue, east = -1, south = int.MaxValue, north = -1;
        for (int row = 0; row < streets.Blocks; row++)
        {
            for (int column = 0; column < streets.Blocks; column++)
            {
                if (streets.Horizontal(column, row) == Rows.NoSlot) { continue; }
                west = Math.Min(west, column); east = Math.Max(east, column + 1);
                south = Math.Min(south, row); north = Math.Max(north, row);
            }
        }
        int middleColumn = (west + east) / 2, middleRow = (south + north) / 2;
        BlockGround middle = streets.Block(middleColumn, middleRow);
        Assert.Equal(streets.Lattice.WidthOf(middleColumn), middle.Wide);
        Assert.Equal(streets.Lattice.WidthOf(middleRow), middle.Deep);
    }

    [Fact]
    public void A_group_an_Arterial_crosses_keeps_its_grid_lines()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "rowhouses.toml");
        string text = File.ReadAllText(path).Replace(
            "arterial_count                 = 0", "arterial_count                 = 4", StringComparison.Ordinal);
        int roads = text.IndexOf("[roads]\n", StringComparison.Ordinal);
        text = text.Insert(roads + "[roads]\n".Length, "merged_block_percent = 100\n");
        RulesetLoadResult result = RulesetLoader.Parse(text, "rowhouses.toml");
        Assert.True(result.Ok, result.Describe());
        var world = new World(1_000, result.Ruleset!);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        StreetGrid streets = world.Roads.Streets;
        RoadGraph graph = world.Roads;
        int checkedNodes = 0, merged = 0;
        for (int segment = 0; segment < graph.Segments.Rows.SlotCount; segment++)
        {
            if (!graph.Segments.Rows.IsLive(segment) || graph.Segments.Kind[segment] != (byte)RoadKind.Arterial) { continue; }
            int node = graph.Nodes.Rows.Resolve(graph.Segments.NodeA[segment]);
            int column = streets.Lattice.LineAt(graph.Nodes.East[node].Raw);
            int row = streets.Lattice.LineAt(graph.Nodes.North[node].Raw);
            if (column < 0 || row < 0 || column >= streets.Blocks || row >= streets.Blocks) { continue; }
            Assert.Equal(streets.Lattice.WidthOf(column), streets.Block(column, row).Wide);
            checkedNodes++;
        }
        for (int row = 0; row < streets.Blocks; row++)
        {
            for (int column = 0; column < streets.Blocks; column++)
            {
                if (streets.Block(column, row).Wide != streets.Lattice.WidthOf(column)) { merged++; }
            }
        }

        Assert.True(checkedNodes > 0, "no Arterial Junction stands on the lattice.");
        Assert.True(merged > 0, "the generator merged nothing.");
    }
}
