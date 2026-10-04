using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

public sealed class SegmentResidencyTests
{
    [Fact]
    public void Curved_and_off_lattice_segments_are_found_once_within_the_query_box()
    {
        RoadGraph graph = CurvedAndAngled(out int curve, out int angled);
        int[] nearby = Collect(graph.Residency.Near(new Tiles(128), new Tiles(155), new Tiles(5)));
        Assert.Contains(curve, nearby);
        Assert.Contains(angled, nearby);
        Assert.Equal(nearby.Length, nearby.Distinct().Count());
        Assert.Empty(Collect(graph.Residency.Near(new Tiles(512), new Tiles(512), new Tiles(5))));
        Assert.Empty(Collect(graph.Residency.In(CellRect.Empty)));
        Assert.Equal(2, Collect(graph.Residency.In(CellRect.World)).Length);
    }

    [Fact]
    public void Nested_queries_and_rebuilds_preserve_the_candidate_order()
    {
        RoadGraph graph = CurvedAndAngled(out _, out _);
        int[] nearby = Collect(graph.Residency.Near(new Tiles(128), new Tiles(155), new Tiles(5)));
        int[] before = Collect(graph.Residency.In(CellRect.World));
        int count = 0;
        foreach (int slot in graph.Residency.In(CellRect.World))
        {
            Assert.Equal(nearby, Collect(graph.Residency.Near(new Tiles(128), new Tiles(155), new Tiles(5))));
            Assert.Contains(slot, before);
            count++;
        }
        Assert.Equal(before.Length, count);
        graph.RebuildDerived();
        Assert.Equal(before, Collect(graph.Residency.In(CellRect.World)));
    }

    [Fact]
    public void Curved_sources_are_audible_with_or_without_a_traffic_summary()
    {
        RoadGraph graph = CurvedAndAngled(out int curve, out _);
        graph.Segments.VolumeForward[curve] = 10;
        var source = new LineSource(new Tiles(5), Fixed.One);
        var traffic = new TrafficPresence();
        traffic.Rebuild(graph, new Tiles(8));
        int level = LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155));
        Assert.True(level > 0);
        Assert.Equal(level, LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155), new TrafficPresence()));
        Assert.Equal(level, LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155), traffic));
        Assert.Equal(level, LineSourceQueries.NearRoadPollution(graph, source, new Tiles(128), new Tiles(155), traffic));
    }

    [Fact]
    public void A_silent_traffic_summary_skips_all_centerline_reads()
    {
        RoadGraph graph = CurvedAndAngled(out int curve, out _);
        var traffic = new TrafficPresence();
        traffic.Rebuild(graph, new Tiles(8));
        Assert.False(traffic.AnyTraffic);

        // A poisoned derived centerline makes any accidental distance read fail.
        graph.Segments.Centerline[curve] = StreetArc.At(-32_768, -32_768);
        var source = new LineSource(new Tiles(5), Fixed.One);
        Assert.Throws<OverflowException>(() => LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155)));
        Assert.Equal(0, LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155), traffic));
        Assert.Equal(0, LineSourceQueries.NearRoadPollution(graph, source, new Tiles(128), new Tiles(155), traffic));
    }

    [Fact]
    public void Boundary_endpoints_and_clipped_queries_use_the_edge_cell()
    {
        var graph = new RoadGraph(RoadFixtures.Roads(blockTiles: 0, arterials: 0));
        int southwest = Add(graph, 0, 0, 64, 0);
        int northeast = Add(graph, CellGrid.WorldTiles - 64, CellGrid.WorldTiles,
            CellGrid.WorldTiles, CellGrid.WorldTiles);
        graph.RebuildDerived();
        Assert.Equal([southwest], Collect(graph.Residency.Near(Tiles.Zero, Tiles.Zero, Tiles.Zero)));
        Assert.Equal([northeast], Collect(graph.Residency.Near(
            new Tiles(CellGrid.WorldTiles), new Tiles(CellGrid.WorldTiles), Tiles.Zero)));
        Assert.Empty(Collect(graph.Residency.Near(new Tiles(-64), new Tiles(-64), Tiles.Zero)));
        Assert.Empty(Collect(graph.Residency.Near(
            new Tiles(CellGrid.WorldTiles + 64), new Tiles(CellGrid.WorldTiles + 64), Tiles.Zero)));
        Assert.Equal([southwest], Collect(graph.Residency.In(new Tiles(-32), new Tiles(-32), new Tiles(64), new Tiles(64))));
    }

    [Fact]
    public void Traffic_summaries_agree_with_direct_queries_on_an_uneven_lattice()
    {
        var graph = new RoadGraph(RoadFixtures.Roads(blockTiles: 32, blockSpread: 28, arterials: 0),
            WorldKey.FromSeed(0x5EA1U));
        var source = new LineSource(new Tiles(8), Fixed.One);
        int row = 0;
        for (int line = 1; line < graph.Lattice.Blocks; line++)
        {
            if (line != IntegerMath.FloorDiv(graph.Lattice.EdgeOf(line), 32))
            {
                row = line;
                break;
            }
        }
        Assert.True(row > 0, "the fixture must expose the FloorDiv/LineAt mismatch");
        int north = graph.Lattice.EdgeOf(row);
        int slot = Add(graph, 0, north, graph.Lattice.EdgeOf(1), north);
        graph.RebuildDerived();
        graph.Segments.VolumeForward[slot] = 10;
        var traffic = new TrafficPresence();
        traffic.Rebuild(graph, new Tiles(8));
        int level = LineSourceQueries.Noise(graph, source, new Tiles(1), new Tiles(north));
        Assert.True(level > 0);
        Assert.Equal(level, LineSourceQueries.Noise(graph, source, new Tiles(1), new Tiles(north), traffic));
        Assert.Equal(level, LineSourceQueries.NearRoadPollution(graph, source, new Tiles(1), new Tiles(north), traffic));
    }

    [Fact]
    public void A_silent_cell_mask_skips_distance_reads_when_other_cells_have_traffic()
    {
        RoadGraph graph = CurvedAndAngled(out int curve, out _);
        int distant = Add(graph, 1024, 1024, 1088, 1024);
        graph.RebuildDerived();
        graph.Segments.VolumeForward[distant] = 1;
        var traffic = new TrafficPresence();
        var source = new LineSource(new Tiles(31), Fixed.One);
        traffic.Rebuild(graph, source.Range);
        Assert.True(traffic.AnyTraffic);
        Assert.False(traffic.Near(new Tiles(128), new Tiles(155)));

        graph.Segments.Centerline[curve] = StreetArc.At(-32_768, -32_768);
        Assert.Throws<OverflowException>(() => LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155)));
        Assert.Equal(0, LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155), traffic));
    }

    [Fact]
    public void A_smaller_mask_range_falls_back_to_the_exact_query()
    {
        RoadGraph graph = CurvedAndAngled(out int curve, out _);
        graph.Segments.VolumeForward[curve] = 10;
        var traffic = new TrafficPresence();
        traffic.Rebuild(graph, Tiles.Zero);
        var source = new LineSource(new Tiles(5), Fixed.One);
        Assert.False(traffic.Covers(source.Range));
        int exact = LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155));
        Assert.True(exact > 0);
        Assert.Equal(exact, LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155), traffic));
    }

    [Fact]
    public void Guarded_land_value_targets_equal_the_unguarded_field_walk()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        var world = new World(1000, Borough.Tests.Golden.GoldenFixtures.Rules(), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        RoadGraph graph = world.Roads;
        int moving = Enumerable.Range(0, graph.Segments.Rows.SlotCount)
            .First(graph.Segments.Rows.IsLive);
        graph.Segments.VolumeForward[moving] = 5;
        MapLayers layers = world.Layers;
        DesirabilityWeights weights = world.Rules.Layers.Desirability;
        var presence = new TrafficPresence();
        presence.Rebuild(graph, weights.NoiseSource.Range);
        Assert.True(presence.AnyTraffic);
        Assert.Contains(false, presence.Mask.ToArray());

        layers.SetLandValueTargets(graph);
        LayerCellTable cells = layers.Cells;
        bool nonzero = false;
        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot)) { continue; }
            int exact = layers.CellDesirability(graph, weights, cells.East[slot], cells.North[slot]);
            Assert.Equal(exact, cells.LandValueTarget[slot]);
            nonzero |= exact != 0;
        }
        Assert.True(nonzero);
    }

    [Fact]
    public void Seeded_point_queries_match_a_brute_force_distance_scan()
    {
        var random = new Random(0x5EA1);
        var graph = new RoadGraph(RoadFixtures.Roads(blockTiles: 0, arterials: 0));
        for (int i = 0; i < 96; i++)
        {
            int aEast = random.Next(0, CellGrid.WorldTiles + 1);
            int aNorth = random.Next(0, CellGrid.WorldTiles + 1);
            int bEast = Math.Clamp(aEast + random.Next(-256, 257), 0, CellGrid.WorldTiles);
            int bNorth = Math.Clamp(aNorth + random.Next(-256, 257), 0, CellGrid.WorldTiles);
            if (aEast == bEast && aNorth == bNorth) { bEast = aEast == 0 ? 1 : aEast - 1; }
            int slot = Add(graph, aEast, aNorth, bEast, bNorth);
            long dx = bEast - aEast, dy = bNorth - aNorth;
            int chord = (int)IntegerMath.SqrtFloor((dx * dx) + (dy * dy));
            int sagitta = i % 3 == 0 ? 0 : random.Next(0, (chord * Fixed.One / 6) + 1);
            graph.Segments.Sagitta[slot] = new SubTiles(i % 2 == 0 ? sagitta : -sagitta);
            graph.Segments.VolumeForward[slot] = i % 4 == 1 ? 1 : 0;
        }
        graph.RebuildDerived();
        var traffic = new TrafficPresence();
        Assert.Contains(graph.Segments.Centerline.Span.ToArray(), arc => !arc.IsStraight);
        int nonemptyQueries = 0;
        for (int query = 0; query < 256; query++)
        {
            int east, north;
            if (query < 192)
            {
                StreetArc arc = graph.Segments.Centerline[query % 96];
                var point = arc.PointAt(query % 2 == 0 ? 0 : arc.Length / 2);
                east = Math.Clamp((int)IntegerMath.FloorDiv(point.East, Fixed.One), 0, CellGrid.WorldTiles);
                north = Math.Clamp((int)IntegerMath.FloorDiv(point.North, Fixed.One), 0, CellGrid.WorldTiles);
            }
            else
            {
                east = random.Next(0, CellGrid.WorldTiles + 1);
                north = random.Next(0, CellGrid.WorldTiles + 1);
            }
            int range = query % 4 == 0 ? 0 : random.Next(1, 257);
            if (query % 16 == 1) { range = 31; }
            if (query % 16 == 2) { range = 32; }
            if (query % 16 == 3) { range = 63; }
            traffic.Rebuild(graph, new Tiles(range));
            int[] candidates = Collect(graph.Residency.Near(new Tiles(east), new Tiles(north), new Tiles(range)));
            Assert.Equal(candidates.Length, candidates.Distinct().Count());

            // The index supplies a conservative set; exact distance rejects its extra candidates.
            int[] indexed = candidates.Where(slot => WithinRange(graph, slot, east, north, range)).Order().ToArray();
            var scanned = new List<int>();
            for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
            {
                if (graph.Segments.Rows.IsLive(slot) && WithinRange(graph, slot, east, north, range))
                {
                    scanned.Add(slot);
                }
            }
            Assert.Equal(scanned, indexed);
            if (indexed.Any(slot => graph.Segments.VolumeForward[slot] > 0))
            {
                Assert.True(traffic.Near(new Tiles(east), new Tiles(north)));
            }
            if (indexed.Length > 0) { nonemptyQueries++; }
        }
        Assert.True(nonemptyQueries > 96);
    }

    private static bool WithinRange(RoadGraph graph, int slot, int east, int north, int range) =>
        graph.Segments.Centerline[slot].DistanceTo((long)east * Fixed.One, (long)north * Fixed.One)
            <= range * Fixed.One;

    private static RoadGraph CurvedAndAngled(out int curve, out int angled)
    {
        var graph = new RoadGraph(RoadFixtures.Roads(blockTiles: 0, arterials: 0));
        curve = Add(graph, 64, 128, 192, 128);
        graph.Segments.Sagitta[curve] = SubTiles.FromTiles(new Tiles(24));
        angled = Add(graph, 126, 152, 134, 168);
        graph.RebuildDerived();
        return graph;
    }

    private static int Add(RoadGraph graph, int aEast, int aNorth, int bEast, int bNorth)
    {
        Handle<RoadNode> a = graph.Nodes.Create(new Tiles(aEast), new Tiles(aNorth));
        Handle<RoadNode> b = graph.Nodes.Create(new Tiles(bEast), new Tiles(bNorth));
        Handle<RoadSegment> segment = graph.Segments.Create(a, b, new Tiles(128), RoadKind.Street,
            TravelMode.Any, TravelMode.Any);
        return graph.Segments.Rows.Resolve(segment);
    }

    private static int[] Collect(SegmentResidency.Query query)
    {
        var slots = new List<int>();
        foreach (int slot in query) { slots.Add(slot); }
        return slots.ToArray();
    }
}
