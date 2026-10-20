using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

public sealed class DistrictAdjacencyTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static World Empty(bool districts = true)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));

        if (districts)
        {
            text += "\n[[hinterland]]\nedge = \"south\"\nemigrant_balance_min = 0\n"
                + "emigrant_balance_max = 0\nprices = [ { resource = \"sundries\", price = 500 }, "
                + "{ resource = \"repairs\", price = 500 } ]\n"
                + "[districts]\nprominence_percent = 50\nrevisit_ticks = 2048\n"
                + "hysteresis_percent = 50\nmigrate_cells = 16\n";
        }

        RulesetLoadResult parsed = RulesetLoader.Parse(text, "district-adjacency");
        Assert.True(parsed.Ruleset is not null, parsed.Describe());
        return new World(100, parsed.Ruleset!, Key);
    }

    private static World Ridge(bool bridge = false)
    {
        World world = Empty();
        int block = world.Rules.Roads.BlockTiles;
        Handle<RoadNode> previous = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int step = 1; step <= 8; step++)
        {
            Handle<RoadNode> next = world.Roads.Nodes.Create(new Tiles(step * block), Tiles.Zero);
            world.Roads.Segments.Create(
                previous, next, new Tiles(block), RoadKind.Street, TravelMode.Any, TravelMode.Any);
            previous = next;
        }

        world.Roads.RebuildDerived();

        for (int cell = 0; cell < 8; cell++)
        {
            bool peak = cell < 2 || cell >= 6;

            if (bridge && !peak)
            {
                continue;
            }

            for (int i = 0; i < (peak ? 8 : 3); i++)
            {
                Handle<Lot> lot = world.Lots.Create(
                    new Tiles((cell * block) + 4 + (i * 2)), Tiles.Zero, zone: 1);
                world.CreateBuilding(lot, kind: 0, Ticks.Zero, Key);
            }
        }

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();
        Assert.Equal(2, world.Districts.Rows.LiveCount);
        return world;
    }

    private static int DistrictAt(World world, int node)
    {
        Handle<District> district = world.DistrictsInCells.Of(
            world.DistrictCells,
            CellGrid.ToCells(world.Roads.Nodes.East[node]),
            CellGrid.ToCells(world.Roads.Nodes.North[node]));
        return world.Districts.Rows.TryResolve(district, out int slot) ? slot : Rows.NoSlot;
    }

    private static int Crossing(World world)
    {
        RoadSegmentTable segments = world.Roads.Segments;

        for (int segment = 0; segment < segments.Rows.SlotCount; segment++)
        {
            if (!segments.Rows.IsLive(segment))
            {
                continue;
            }

            int a = DistrictAt(world, world.Roads.Nodes.Rows.Resolve(segments.NodeA[segment]));
            int b = DistrictAt(world, world.Roads.Nodes.Rows.Resolve(segments.NodeB[segment]));

            if (a != Rows.NoSlot && b != Rows.NoSlot && a != b)
            {
                return segment;
            }
        }

        throw new InvalidOperationException("The ridge has no crossing Segment.");
    }

    private static void Connect(World world, int a, int b, int length)
    {
        world.Roads.Segments.Create(
            world.Roads.Nodes.Rows.At(a), world.Roads.Nodes.Rows.At(b), new Tiles(length),
            RoadKind.Street, TravelMode.Any, TravelMode.Any);
    }

    [Fact]
    public void Ridge_has_one_crossing_and_credits_road_tiles_to_the_first_assigned_end()
    {
        World world = Ridge();
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);

        DistrictAdjacency.Link link = Assert.Single(adjacency.Links.ToArray());
        Assert.True(link.Lower < link.Higher);
        Assert.Equal(1, link.Crossings);

        long[] expected = new long[world.Districts.Rows.SlotCount];
        RoadSegmentTable segments = world.Roads.Segments;
        long total = 0;

        for (int segment = 0; segment < segments.Rows.SlotCount; segment++)
        {
            int a = DistrictAt(world, world.Roads.Nodes.Rows.Resolve(segments.NodeA[segment]));
            int b = DistrictAt(world, world.Roads.Nodes.Rows.Resolve(segments.NodeB[segment]));
            int credited = a != Rows.NoSlot ? a : b;
            total += segments.LengthTiles[segment].Raw;

            if (credited != Rows.NoSlot)
            {
                expected[credited] += segments.LengthTiles[segment].Raw;
            }
        }

        long assigned = 0;

        for (int district = 0; district < expected.Length; district++)
        {
            Assert.Equal(expected[district], adjacency.RoadTilesOf(district));
            assigned += adjacency.RoadTilesOf(district);
        }

        Assert.Equal(256, total);
        Assert.Equal(total, assigned);
        Assert.True(assigned <= total);
    }

    [Fact]
    public void Removing_the_crossing_clears_a_reused_result()
    {
        World world = Ridge();
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);
        Assert.Single(adjacency.Links.ToArray());
        world.Roads.RemoveSegment(Crossing(world));
        adjacency.Compute(world);
        Assert.Empty(adjacency.Links.ToArray());
        Assert.Equal(224, adjacency.RoadTilesOf(0) + adjacency.RoadTilesOf(1));
    }

    [Fact]
    public void A_second_crossing_adds_capacity_and_uses_the_saved_length()
    {
        World world = Ridge();
        int crossing = Crossing(world);
        RoadSegmentTable segments = world.Roads.Segments;
        int a = world.Roads.Nodes.Rows.Resolve(segments.NodeA[crossing]);
        int b = world.Roads.Nodes.Rows.Resolve(segments.NodeB[crossing]);
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);
        long before = adjacency.RoadTilesOf(DistrictAt(world, a));

        Connect(world, b, a, 47);
        adjacency.Compute(world);

        Assert.Equal(2, Assert.Single(adjacency.Links.ToArray()).Crossings);
        Assert.Equal(before, adjacency.RoadTilesOf(DistrictAt(world, a)));
        Assert.Equal(303, adjacency.RoadTilesOf(0) + adjacency.RoadTilesOf(1));
    }

    [Fact]
    public void Unbuilt_cells_contract_to_one_component_with_the_smaller_boundary_count()
    {
        World world = Ridge(bridge: true);
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);
        Assert.Equal(1, Assert.Single(adjacency.Links.ToArray()).Crossings);
        Assert.Equal(160, adjacency.RoadTilesOf(0) + adjacency.RoadTilesOf(1));

        // Three Segments touch the western District and two touch the eastern District.
        Connect(world, 0, 3, 96);
        Connect(world, 1, 4, 96);
        Connect(world, 6, 4, 64);
        adjacency.Compute(world);

        Assert.Equal(2, Assert.Single(adjacency.Links.ToArray()).Crossings);
        Assert.Equal(416, adjacency.RoadTilesOf(0) + adjacency.RoadTilesOf(1));
        Assert.True(adjacency.RoadTilesOf(0) + adjacency.RoadTilesOf(1) <= world.Roads.RoadLengthTiles());

        world.Roads.RemoveSegment(3);
        adjacency.Compute(world);
        Assert.Equal(1, Assert.Single(adjacency.Links.ToArray()).Crossings);
    }

    [Fact]
    public void Disconnected_unassigned_components_do_not_link_the_banks()
    {
        World world = Ridge(bridge: true);
        world.Roads.RemoveSegment(3);
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);
        Assert.Empty(adjacency.Links.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_world_without_districts_has_no_links_or_credited_tiles(bool enabled)
    {
        World world = Empty(enabled);
        Handle<RoadNode> a = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);
        Handle<RoadNode> b = world.Roads.Nodes.Create(new Tiles(32), Tiles.Zero);
        world.Roads.Segments.Create(a, b, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(Ridge());
        adjacency.Compute(world);
        Assert.Empty(adjacency.Links.ToArray());
        Assert.Equal(0, adjacency.RoadTilesOf(0));
        Assert.Equal(0, adjacency.RoadTilesOf(1));
    }

    [Fact]
    public void Results_are_sorted_unique_and_unchanged_by_derived_rebuild()
    {
        World world = Empty();
        Handle<District>[] districts = new Handle<District>[3];

        for (int district = 0; district < districts.Length; district++)
        {
            districts[district] = world.Districts.Create(new Cells(district), new Cells(0));
            world.DistrictCells.Create(new Cells(district), new Cells(0), districts[district]);
            world.Roads.Nodes.Create(new Tiles(district * 32), Tiles.Zero);
        }

        world.DistrictsInCells.Rebuild(world.DistrictCells);
        Connect(world, 2, 0, 64);
        Connect(world, 2, 1, 32);
        Connect(world, 1, 0, 32);
        Connect(world, 0, 2, 64);
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);

        ulong state = world.HashState();
        adjacency.Compute(world);
        Assert.Equal(state, world.HashState());

        DistrictAdjacency.Link[] expected =
        [
            new(0, 1, 1),
            new(0, 2, 2),
            new(1, 2, 1)
        ];
        Assert.Equal(expected, adjacency.Links.ToArray());
        Assert.Equal(64, adjacency.RoadTilesOf(0));
        Assert.Equal(32, adjacency.RoadTilesOf(1));
        Assert.Equal(96, adjacency.RoadTilesOf(2));

        world.RebuildDerived();
        adjacency.Compute(world);
        Assert.Equal(expected, adjacency.Links.ToArray());
        Assert.Equal(64, adjacency.RoadTilesOf(0));
        Assert.Equal(32, adjacency.RoadTilesOf(1));
        Assert.Equal(96, adjacency.RoadTilesOf(2));
    }

    [Fact]
    public void Direct_crossings_and_separate_components_sum_for_each_touching_pair()
    {
        World world = Empty();

        for (int district = 0; district < 3; district++)
        {
            Handle<District> handle = world.Districts.Create(new Cells(district), new Cells(0));
            world.DistrictCells.Create(new Cells(district), new Cells(0), handle);
            world.Roads.Nodes.Create(new Tiles(district * 32), Tiles.Zero);
        }

        for (int node = 3; node < 6; node++)
        {
            world.Roads.Nodes.Create(new Tiles(node * 32), Tiles.Zero);
        }

        world.DistrictsInCells.Rebuild(world.DistrictCells);
        Connect(world, 2, 0, 64);
        Connect(world, 0, 2, 64);
        Connect(world, 3, 4, 32);
        Connect(world, 0, 3, 96);
        Connect(world, 0, 4, 128);
        Connect(world, 4, 1, 96);
        Connect(world, 2, 3, 32);
        Connect(world, 2, 4, 64);
        Connect(world, 2, 3, 32);
        Connect(world, 0, 5, 160);
        Connect(world, 2, 5, 96);
        var adjacency = new DistrictAdjacency();
        adjacency.Compute(world);

        DistrictAdjacency.Link[] expected =
        [
            new(0, 1, 1),
            new(0, 2, 5),
            new(1, 2, 1)
        ];
        Assert.Equal(expected, adjacency.Links.ToArray());
        Assert.Equal(world.Roads.RoadLengthTiles() - 32,
            adjacency.RoadTilesOf(0) + adjacency.RoadTilesOf(1) + adjacency.RoadTilesOf(2));
    }
}
