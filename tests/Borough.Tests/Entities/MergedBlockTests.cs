using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Tests.Persistence;
using Borough.Appearance;
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
    public void A_carved_two_by_two_block_previews_from_any_square_and_survives_save_and_reload()
    {
        World world = Populated();
        StreetGrid streets = world.Roads.Streets;
        (int column, int row) = EmptyPatch(world);

        Assert.True(world.Roads.BulldozeStreet(column, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row, StreetAxis.North));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row + 1, StreetAxis.North));
        world.RebuildDerived();

        int first = world.Lots.Rows.SlotCount;
        int carved = LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing);
        Assert.True(carved > 0);

        for (int square = 0; square < 4; square++)
        {
            int at = column + (square & 1), on = row + (square >> 1);
            var into = new Parcel[LotSubdivider.PreviewCapacity(world, at, on)];
            Assert.Equal(carved, LotSubdivider.Preview(world, at, on, into));
        }

        var save = new MemorySave();
        SaveFile.Write(world, 0, save);
        World restored = SaveFile.Read(save, world.Rules, out _);

        Assert.Equal(world.HashState(), restored.HashState());
        BlockGround block = restored.Roads.Streets.Block(column + 1, row + 1);
        Assert.Equal((column, row, 2 * streets.BlockTiles), (block.Column, block.Row, block.Wide));
        for (int lot = first; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot)) { continue; }
            Assert.Equal(world.Lots.FrontageOn(lot), restored.Lots.FrontageOn(lot));
            Assert.Equal(world.Lots.FrontageOffset[lot], restored.Lots.FrontageOffset[lot]);
        }

        var simulation = new Simulation(world, Key);
        var resumed = new Simulation(restored, Key);
        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(TickInput.Empty);
            resumed.Step(TickInput.Empty);
        }
        Assert.Equal(world.HashState(), restored.HashState());
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, false)]
    [InlineData(2, 2, true)]
    public void A_Lot_on_an_inner_grid_line_of_a_merged_block_is_not_a_corner(int east, int north, bool corner)
    {
        World world = Populated();
        StreetGrid streets = world.Roads.Streets;
        (int column, int row) = EmptyPatch(world);

        Assert.True(world.Roads.BulldozeStreet(column, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row + 1, StreetAxis.East));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row, StreetAxis.North));
        Assert.True(world.Roads.BulldozeStreet(column + 1, row + 1, StreetAxis.North));
        world.RebuildDerived();

        // Each index counts squares from the block's south-west; 2 places the parcel's far edge on the block's.
        BlockGround block = streets.Block(column, row);
        int wide = 8, deep = 10, square = streets.BlockTiles;
        int x = east == 2 ? block.East + block.Wide - wide : block.East + (east * square);
        int y = north == 2 ? block.North + block.Deep - deep : block.North + (north * square);
        int slot = world.Lots.Rows.Resolve(world.Lots.Create(new Tiles(x), new Tiles(y), LotTable.Housing));
        world.Lots.ParcelEastQ16[slot] = Fixed.FromInt(x);
        world.Lots.ParcelNorthQ16[slot] = Fixed.FromInt(y);
        world.Lots.ParcelWide[slot] = new Tiles(wide);
        world.Lots.ParcelDeep[slot] = new Tiles(deep);

        Assert.Equal(corner, BuildingFacts.IsCorner(streets, world.Lots, slot));
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
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot)) { continue; }
            int segment = Frontage.Locate(streets, world.Lots.East[lot], world.Lots.North[lot], out Tiles along);
            if (segment == Rows.NoSlot || segment != world.Lots.FrontageOn(lot)) { continue; }
            Assert.Equal(along, world.Lots.FrontageOffset[lot]);
        }
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
