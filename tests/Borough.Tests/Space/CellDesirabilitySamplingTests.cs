using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 9 task 4 — how a Cell reduces a Tile-resolution composition to one number.
/// </summary>
/// <remarks>
/// <b>The decision these pin is not the one the task set out to make.</b> The plan expected a
/// quadrature order justified by convergence; the measurement found the area mean of a line-source
/// field does not converge at all, because the Segments sit on the Cell's own edges. What survived is
/// a weaker and truer claim — the sample set <em>defines</em> the Cell's value, and what a sample
/// order has to preserve is the <b>ordering between Cells</b>, which is the only thing anything reads.
/// </remarks>
public sealed class CellDesirabilitySamplingTests
{
    private static readonly DesirabilityWeights Weights = DesirabilityWeights.Default;

    private static RoadGraph Lattice(int blocks)
    {
        RoadGraph graph = new(RoadFixtures.Roads(blockTiles: 32, arterials: 0));
        Handle<RoadNode>[,] nodes = new Handle<RoadNode>[blocks + 1, blocks + 1];

        for (int north = 0; north <= blocks; north++)
        {
            for (int east = 0; east <= blocks; east++)
            {
                nodes[east, north] = graph.Nodes.Create(new Tiles(east * 32), new Tiles(north * 32));
            }
        }

        for (int north = 0; north <= blocks; north++)
        {
            for (int east = 0; east <= blocks; east++)
            {
                if (east < blocks)
                {
                    Link(graph, nodes[east, north], nodes[east + 1, north]);
                }

                if (north < blocks)
                {
                    Link(graph, nodes[east, north], nodes[east, north + 1]);
                }
            }
        }

        graph.RebuildDerived();

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            graph.Segments.VolumeForward[slot] = 40;
        }

        return graph;
    }

    private static void Link(RoadGraph graph, Handle<RoadNode> from, Handle<RoadNode> to)
    {
        graph.Segments.Create(
            from, to, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);
    }

    private static int Mean(MapLayers layers, RoadGraph graph, Cells east, Cells north, int order)
    {
        int stride = IntegerMath.FloorDiv(CellGrid.TilesPerCell, order);
        Tiles originEast = CellGrid.ToTiles(east) + new Tiles(IntegerMath.FloorDiv(stride, 2));
        Tiles originNorth = CellGrid.ToTiles(north) + new Tiles(IntegerMath.FloorDiv(stride, 2));
        int total = 0;

        for (int up = 0; up < order; up++)
        {
            for (int across = 0; across < order; across++)
            {
                total += layers.Desirability(
                    graph,
                    Weights,
                    originEast + new Tiles(across * stride),
                    originNorth + new Tiles(up * stride));
            }
        }

        return IntegerMath.RoundDiv(total, order * order);
    }

    /// <summary>
    /// Builds the varied world the ordering test needs: a lattice whose Segments carry different
    /// volumes, and two pollution sources of different strength.
    /// </summary>
    private static (RoadGraph Graph, MapLayers Layers) Varied(int blocks)
    {
        RoadGraph graph = Lattice(blocks);
        MapLayers layers = new(LayerRuleset.Default);

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            graph.Segments.VolumeForward[slot] = 4 + (slot * 3 % 61);
        }

        layers.EmitPollution(new Cells(1), new Cells(1), 4000);
        layers.EmitPollution(new Cells(4), new Cells(3), 1500);
        layers.Step(Ticks.Zero, graph, TerrainRuleset.None);

        return (graph, layers);
    }

    /// <summary>
    /// <b>The premise of the whole sampling decision, pinned rather than asserted in a comment.</b>
    /// </summary>
    /// <remarks>
    /// A Cell is 32 Tiles and the fixture's <c>block_tiles</c> is 32, so the lattice lines land on
    /// Cell edges. That makes the two obvious single samples the two worst ones — the centre is the
    /// furthest Tile from every road in the Cell and the corner is a junction. ⚠ <b>Neither is a
    /// small error</b>: they bracket the four-sample mean from opposite sides.
    /// </remarks>
    [Fact]
    public void A_cells_centre_is_its_quietest_tile_and_its_corner_its_loudest()
    {
        RoadGraph graph = Lattice(4);
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(1);
        Cells north = new(1);

        int centre = layers.Desirability(
            graph,
            Weights,
            CellGrid.ToTiles(east) + new Tiles(16),
            CellGrid.ToTiles(north) + new Tiles(16));
        int corner = layers.Desirability(graph, Weights, CellGrid.ToTiles(east), CellGrid.ToTiles(north));
        int sampled = layers.CellDesirability(graph, Weights, east, north);

        Assert.True(
            corner < sampled && sampled < centre,
            $"corner {corner} < sampled {sampled} < centre {centre}: the two single samples bracket");
    }

    /// <summary>
    /// <b>There is no limit to converge on, and this test exists so that nobody re-derives one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reasoning that was written first — <em>four quadrant centres is the lowest order that
    /// estimates the Cell's mean well enough</em> — is <b>wrong, and the measurement said so</b>
    /// (<c>adr/0043</c>, run rather than argued). A line source falls off with distance and the
    /// Segments sit on the Cell's own edges, so the integrand is unbounded on the boundary: the area
    /// mean of the noise term does not converge, it just keeps getting louder as the sample set moves
    /// closer to the edges.
    /// </para>
    /// <para>
    /// ⚠ <b>So the sample set DEFINES the Cell's value; it does not estimate one.</b> Raising the
    /// order is not refining an answer, it is asking a different question. What makes order 2
    /// defensible is the test below, and it is about rank rather than about magnitude.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_area_mean_of_a_line_source_does_not_converge_with_sample_order()
    {
        RoadGraph graph = Lattice(4);
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(1);
        Cells north = new(1);
        int[] orders = [1, 2, 4, 8];
        int[] means = [.. orders.Select(order => Mean(layers, graph, east, north, order))];

        for (int step = 1; step < means.Length; step++)
        {
            int moved = IntegerMath.Abs(means[step] - means[step - 1]);
            int floor = IntegerMath.FloorDiv(IntegerMath.Abs(means[step - 1]), 100);

            Assert.True(
                moved > floor,
                $"order {orders[step - 1]} to {orders[step]} moved {moved}, over 1% of "
                + $"{means[step - 1]}; if this ever stops being true the field has acquired a term "
                + "that is smooth inside a Cell and the sampling decision can be reopened");
        }
    }

    /// <summary>
    /// <b>The ratifier, and it is reachable today.</b> Land value is read by comparison and never
    /// absolutely, so what a sample order has to preserve is the <em>ordering</em> between Cells.
    /// </summary>
    /// <remarks>
    /// Measured on this world: <b>615 of 630 Cell pairs order identically</b> under order 2 and order
    /// 8, and every pair that disagrees is a pair the two orders put within 1% of each other — a tie
    /// broken differently rather than a rank inverted. That is what makes order 2 a scale choice
    /// rather than a design choice, and it is the check <c>MapLayers.DesirabilitySamplesPerAxis</c>
    /// cites. ⚠ <b>It says nothing about the magnitudes</b>, which the test above shows do not settle.
    /// </remarks>
    [Fact]
    public void The_ordering_between_cells_survives_the_sample_order()
    {
        (RoadGraph graph, MapLayers layers) = Varied(6);
        List<(int Coarse, int Fine)> cells = [];

        for (int north = 0; north < 6; north++)
        {
            for (int east = 0; east < 6; east++)
            {
                cells.Add((
                    Mean(layers, graph, new Cells(east), new Cells(north), 2),
                    Mean(layers, graph, new Cells(east), new Cells(north), 8)));
            }
        }

        int pairs = 0;
        int inverted = 0;

        for (int a = 0; a < cells.Count; a++)
        {
            for (int b = a + 1; b < cells.Count; b++)
            {
                pairs++;

                if (cells[a].Coarse < cells[b].Coarse == cells[a].Fine < cells[b].Fine)
                {
                    continue;
                }

                inverted++;

                int apart = IntegerMath.Abs(cells[a].Fine - cells[b].Fine);
                int tie = IntegerMath.FloorDiv(IntegerMath.Abs(cells[a].Fine), 100);

                Assert.True(
                    apart <= tie,
                    $"cells {a} and {b} order differently and are {apart} apart, over the 1% tie "
                    + $"band of {tie}: that is a rank inversion and not a tie broken differently");
            }
        }

        Assert.True(
            inverted * 20 <= pairs,
            $"{inverted} of {pairs} pairs order differently; the measured figure is 15 of 630");
    }

    /// <summary>
    /// <b>The producer walks the rows that exist and creates none</b>, which is what keeps it out of
    /// <c>adr/0006</c>'s way. ⚠ The consequence is real: a Cell with roads and no row has no land
    /// value at all rather than a low one.
    /// </summary>
    [Fact]
    public void The_producer_creates_no_rows()
    {
        RoadGraph graph = Lattice(4);
        MapLayers layers = new(LayerRuleset.Default);

        layers.EmitPollution(new Cells(1), new Cells(1), 400);

        int before = layers.Cells.Rows.LiveCount;

        layers.SetLandValueTargets(graph);

        Assert.Equal(before, layers.Cells.Rows.LiveCount);
    }

    /// <summary>
    /// <b>The target is set and then stepped toward, in one call, on the cadence Tick.</b> Land value
    /// leaves zero on the first Tick the schedule is due for it and not a cadence later.
    /// </summary>
    [Fact]
    public void The_cadence_tick_retargets_before_it_drifts()
    {
        (RoadGraph graph, MapLayers layers) = Varied(4);
        Cells east = new(1);
        Cells north = new(1);

        Assert.Equal(0, layers.LandValue(east, north));

        Ticks due = new(16);

        Assert.True(layers.Schedule.IsDue(Layer.LandValue, due));

        layers.Step(due, graph, TerrainRuleset.None);

        int moved = layers.LandValue(east, north);

        Assert.True(moved < 0, $"land value stepped toward a negative target, not {moved}");
        Assert.True(
            moved > layers.CellDesirability(graph, Weights, east, north),
            "and it lagged rather than snapping: one step of the lag, not the whole gap");
    }
}
