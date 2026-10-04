using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

public sealed class SegmentResidencyTests
{
    [Fact]
    public void Curved_and_off_lattice_segments_are_found_once_without_shared_query_scratch()
    {
        var graph = new RoadGraph(RoadFixtures.Roads(blockTiles: 0, arterials: 0));
        int curve = Add(graph, 64, 128, 192, 128);
        graph.Segments.Sagitta[curve] = SubTiles.FromTiles(new Tiles(24));
        int angled = Add(graph, 126, 152, 134, 168);
        graph.RebuildDerived();

        int[] nearby = Collect(graph.Residency.Near(new Tiles(128), new Tiles(155), new Tiles(5)));
        Assert.Contains(curve, nearby);
        Assert.Contains(angled, nearby);
        Assert.Equal(nearby.Length, nearby.Distinct().Count());
        Assert.Empty(Collect(graph.Residency.Near(new Tiles(512), new Tiles(512), new Tiles(5))));
        Assert.Empty(Collect(graph.Residency.In(CellRect.Empty)));
        Assert.Equal(2, Collect(graph.Residency.In(CellRect.World)).Length);

        int count = 0;
        foreach (int slot in graph.Residency.In(CellRect.World))
        {
            Assert.Equal(nearby, Collect(graph.Residency.Near(new Tiles(128), new Tiles(155), new Tiles(5))));
            Assert.True(slot == curve || slot == angled);
            count++;
        }
        Assert.Equal(2, count);
        int[] before = Collect(graph.Residency.In(CellRect.World));
        graph.RebuildDerived();
        Assert.Equal(before, Collect(graph.Residency.In(CellRect.World)));

        graph.Segments.VolumeForward[curve] = 10;
        var source = new LineSource(new Tiles(5), Fixed.One);
        var traffic = new TrafficPresence();
        traffic.Rebuild(graph, source.Range);
        int level = LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155));
        Assert.True(level > 0);
        Assert.Equal(level, LineSourceQueries.Noise(graph, source, new Tiles(128), new Tiles(155), traffic));
        Assert.Equal(0, LineSourceQueries.NearRoadPollution(graph, source, new Tiles(512), new Tiles(512), traffic));
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
    public void Traffic_presence_uses_the_same_cells_on_an_uneven_lattice()
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
        Assert.True(row > 0, "the fixture must expose the old FloorDiv/LineAt mismatch");
        int north = graph.Lattice.EdgeOf(row);
        int slot = Add(graph, 0, north, graph.Lattice.EdgeOf(1), north);
        graph.RebuildDerived();
        graph.Segments.VolumeForward[slot] = 10;
        var traffic = new TrafficPresence();
        traffic.Rebuild(graph, source.Range);
        int level = LineSourceQueries.Noise(graph, source, new Tiles(1), new Tiles(north));
        Assert.True(level > 0);
        Assert.True(traffic.Near(graph, new Tiles(1), new Tiles(north), source.Range));
        Assert.Equal(level, LineSourceQueries.Noise(graph, source, new Tiles(1), new Tiles(north), traffic));
        Assert.Equal(level, LineSourceQueries.NearRoadPollution(graph, source, new Tiles(1), new Tiles(north), traffic));
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
