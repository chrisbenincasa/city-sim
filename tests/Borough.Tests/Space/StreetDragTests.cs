using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// <see cref="StreetGrid.Between"/>: what a drag from one Tile to another asks the lattice for, and
/// that the diagonals a generated city already contains are not Streets.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>THERE ARE NO DIAGONAL STREETS AND THE REFUSAL NEVER REACHED THE PLAYER.</b>
/// <see cref="StreetAxis"/> declares exactly two values and <c>adr/0077</c> refuses a spline by
/// name, so <em>how do I build a diagonal road</em> has the answer <b>you cannot</b> — which under
/// <c>adr/0070</c> is the one classification that counts as evidence. The tool did not say so: a
/// drag laid one Segment near where it started and reported nothing at all. <c>plans/0045</c>
/// row 23.
/// </para>
/// <para>
/// ⚠ <b>The second class here is the evidence half and it is a different kind of test.</b> The rows
/// above are arithmetic on a grid; <see cref="StreetDiagonalTests"/> lays a network and looks at it,
/// because the row's sharpest claim is that <em>the generated world DOES contain diagonals</em> and
/// that claim is about the generator rather than about the aim.
/// </para>
/// </remarks>
public sealed class StreetDragTests
{
    /// <summary>The lattice these run against, in Tiles between intersections.</summary>
    /// <remarks>
    /// <b>32, which is what every shipped <c>[roads] block_tiles</c> states</b> — the same choice
    /// <see cref="StreetAimTests"/> makes, and for its reason: nothing here lays a Segment, so the
    /// generator's cost does not arise and the real number is free.
    /// </remarks>
    private const int Block = 32;

    /// <summary>A block well inside the map, so no clamp is in play. Its south-west Tile.</summary>
    private const int Origin = 8_192;

    /// <summary>The column and row that <see cref="Origin"/> stands at.</summary>
    private const int At = Origin / Block;

    /// <summary>
    /// A point just inside a block's south face — <b>an <see cref="StreetAxis.East"/> edge, and
    /// unambiguously so.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Off the midpoint on purpose.</b> A block's dead centre is equidistant from all four
    /// faces and answers on a stated tie-break (<see cref="StreetAimTests"/>), so a drag test that
    /// used it would be asserting the tie-break rather than the classification.
    /// </remarks>
    private const int SouthAlongEast = 16;

    /// <summary>And how far north of that face it sits. Nearer the face than any other.</summary>
    private const int SouthAlongNorth = 4;

    [Theory]

    // Both ends in one block and near one face: the ordinary click, which is the only drag a click
    // can honour. ⚠ The two ends are NOT the same Tile -- a hand that moves four Tiles between press
    // and release has still clicked, and this is the row that says so.
    [InlineData(0, 0, 0, 0, StreetDrag.OneEdge, 1)]
    [InlineData(0, 0, 4, 3, StreetDrag.OneEdge, 1)]

    // A straight run along the south faces, three blocks east: four edges, and the count is the
    // sentence's whole content.
    [InlineData(0, 0, 3 * Block, 0, StreetDrag.OneLine, 4)]
    [InlineData(3 * Block, 0, 0, 0, StreetDrag.OneLine, 4)]

    // The same block, one end near the south face and the other near the west: two axes, and the
    // shortest drag in the set that cannot be one edit.
    [InlineData(0, 0, -SouthAlongEast + 4, -SouthAlongNorth + 16, StreetDrag.TwoAxes, 0)]

    // The dog-leg: both ends are east edges and they are on different row lines, so there is no
    // straight run between them either.
    [InlineData(0, 0, 0, 3 * Block, StreetDrag.TwoAxes, 0)]

    // 🔴 THE DIAGONAL ITSELF, which is the row's own question: a drag two blocks east and two north.
    [InlineData(0, 0, 2 * Block, 2 * Block, StreetDrag.TwoAxes, 0)]
    [InlineData(0, 0, -8 * Block, -8 * Block, StreetDrag.TwoAxes, 0)]
    public void A_drag_is_classified_by_what_the_lattice_could_lay(
        int fromEast, int fromNorth, int toEast, int toNorth, StreetDrag drag, int streets)
    {
        StreetGrid lattice = Lattice();

        Assert.Equal(
            (drag, streets),
            lattice.Between(
                new Tiles(Origin + SouthAlongEast + fromEast),
                new Tiles(Origin + SouthAlongNorth + fromNorth),
                new Tiles(Origin + SouthAlongEast + toEast),
                new Tiles(Origin + SouthAlongNorth + toNorth)));
    }

    /// <summary>A run along the north axis, which is the other half of <see cref="StreetDrag.OneLine"/>.</summary>
    /// <remarks>
    /// <b>Separate from the theory above because the aim has to be moved to a west face to reach
    /// it</b>, and folding that into the same offsets would have made every row of the table read
    /// against two origins.
    /// </remarks>
    [Fact]
    public void A_run_along_the_north_axis_counts_its_edges_too()
    {
        StreetGrid lattice = Lattice();

        Assert.Equal(
            (StreetDrag.OneLine, 3),
            lattice.Between(
                new Tiles(Origin + 4),
                new Tiles(Origin + 16),
                new Tiles(Origin + 4),
                new Tiles(Origin + 16 + (2 * Block))));
    }

    /// <summary>
    /// No drag anywhere in a block of blocks is ever called a run when it needs both axes.
    /// </summary>
    /// <remarks>
    /// <b>The property the theory's rows sample, stated as a bound.</b> A re-derivation that gets the
    /// listed points right and confuses <em>same axis</em> with <em>same line</em> passes the table
    /// above and fails here — which is exactly the mistake the axis rule made before row 22, one
    /// question further out.
    /// </remarks>
    [Fact]
    public void A_drag_that_needs_both_axes_is_never_called_a_run()
    {
        StreetGrid lattice = Lattice();

        for (int east = 0; east < 4 * Block; east += 5)
        {
            for (int north = 0; north < 4 * Block; north += 5)
            {
                (int fromColumn, int fromRow, StreetAxis fromAxis) =
                    lattice.NearestEdge(new Tiles(Origin), new Tiles(Origin));

                (int toColumn, int toRow, StreetAxis toAxis) =
                    lattice.NearestEdge(new Tiles(Origin + east), new Tiles(Origin + north));

                bool online = fromAxis == toAxis
                    && (fromAxis == StreetAxis.East ? fromRow == toRow : fromColumn == toColumn);

                (StreetDrag drag, _) = lattice.Between(
                    new Tiles(Origin),
                    new Tiles(Origin),
                    new Tiles(Origin + east),
                    new Tiles(Origin + north));

                Assert.Equal(
                    online,
                    drag is StreetDrag.OneEdge or StreetDrag.OneLine);

                // ⚠ AN EDGE IS A TRIPLE AND NOT A PAIR, which this assertion got wrong first
                // time and the loop caught: the south face and the west face of one block share
                // (column, row) and differ only in axis, so a pair comparison called them one edge.
                Assert.Equal(
                    (fromColumn, fromRow, fromAxis) == (toColumn, toRow, toAxis),
                    drag == StreetDrag.OneEdge);
            }
        }
    }

    /// <summary>
    /// A world with no lattice classifies nothing rather than dividing by its block size.
    /// </summary>
    /// <remarks>
    /// <b>The same world <see cref="StreetAimTests"/> ends on</b>, and the same division of labour:
    /// this answers <see cref="StreetDrag.NoLattice"/> and <c>Refusal.ConnectWorldHasNoLattice</c> is
    /// the rule. ***An aim belongs to the hand and a refusal belongs to the city.***
    /// </remarks>
    [Fact]
    public void A_world_with_no_lattice_classifies_no_drag()
    {
        var lattice = new StreetGrid();

        Assert.Equal(
            (StreetDrag.NoLattice, 0),
            lattice.Between(
                new Tiles(Origin), new Tiles(Origin), new Tiles(Origin + 64), new Tiles(Origin)));
    }

    /// <summary>An empty lattice at <see cref="Block"/> Tiles — the geometry and no Segments.</summary>
    /// <remarks>
    /// <b>No Segment is laid because none is asked about.</b> <see cref="StreetGrid.Between"/> reads
    /// the grid spacing and never the index, which is what lets it answer on the <c>--empty</c> world
    /// before the first Street exists.
    /// </remarks>
    private static StreetGrid Lattice()
    {
        RoadGraph graph = new(RoadFixtures.Roads(blockTiles: Block, arterials: 0));

        graph.RebuildDerived();

        return graph.Streets;
    }
}

/// <summary>
/// The diagonals a generated city already contains — <b>what they are, and that no Street is one.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>THE ROW'S SHARPEST CLAIM IS THAT THE PLAYER HAS ALREADY SEEN ONE.</b> <c>--morphology</c>
/// reports six occupied compass bins on <c>rulesets/minimal.toml</c> where a pure lattice has four,
/// and the two extra ones are 45° and 225° — <b>one foot path counted forwards and backwards.</b>
/// ***A player who has seen a diagonal on screen will reasonably ask for the tool that made it***,
/// and these assertions are what say the honest answer is <em>no tool did</em> rather than
/// <em>not yet</em>.
/// </para>
/// <para>
/// ⚠ <b>An assertion and not an instrument.</b> Nothing here quotes a count into a document: what is
/// asserted is that the set is non-empty, that every member of it is a
/// <see cref="RoadKind.FootPath"/>, and that not one of them is on the lattice. The morphology
/// figures stay in the mode that produces them.
/// </para>
/// </remarks>
public sealed class StreetDiagonalTests
{
    /// <summary>
    /// The extent these lay over, in Tiles. <b>A quarter of the map, which is enough blocks for the
    /// per-thousand rate to draw some.</b>
    /// </summary>
    private const int ExtentTiles = 4_096;

    /// <summary>Every diagonal Segment in a generated city is a foot path, and there is at least one.</summary>
    [Fact]
    public void The_generator_lays_diagonals_and_every_one_of_them_is_a_foot_path()
    {
        RoadGraph graph = Laid();
        RoadSegmentTable segments = graph.Segments;
        int diagonals = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot) || !IsDiagonal(graph, slot))
            {
                continue;
            }

            diagonals++;

            Assert.Equal(RoadKind.FootPath, (RoadKind)segments.Kind[slot]);
        }

        Assert.True(diagonals > 0, "no diagonal Segment was laid, so the row's premise is stale");
    }

    /// <summary>
    /// Not one diagonal is on the Street lattice — <b>so no aim can ever name one.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is what makes the shell's sentence true rather than merely plausible.</b>
    /// <see cref="StreetGrid.NearestEdge"/> answers with a lattice edge and
    /// <see cref="StreetGrid.SegmentOn"/> looks it up in the index, so a diagonal that had somehow
    /// earned a lattice place would be bulldozable by a click that claimed to be aiming at a Street.
    /// </remarks>
    [Fact]
    public void No_diagonal_is_on_the_street_lattice()
    {
        RoadGraph graph = Laid();
        StreetGrid streets = graph.Streets;
        var offLattice = new HashSet<int>();

        for (int index = 0; index < streets.OffLatticeCount; index++)
        {
            offLattice.Add(streets.OffLatticeAt(index));
        }

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot) || !IsDiagonal(graph, slot))
            {
                continue;
            }

            Assert.Contains(slot, offLattice);
        }
    }

    /// <summary>Whether a Segment's two endpoints differ on <b>both</b> axes.</summary>
    /// <remarks>
    /// <b>The geometric definition and not the kind</b>, deliberately: asking <em>is it a foot
    /// path</em> and then asserting it is one would be a tautology, and what the row claims is about
    /// the bearing a player sees.
    /// </remarks>
    private static bool IsDiagonal(RoadGraph graph, int slot)
    {
        RoadNodeTable nodes = graph.Nodes;

        if (!nodes.Rows.TryResolve(graph.Segments.NodeA[slot], out int a)
            || !nodes.Rows.TryResolve(graph.Segments.NodeB[slot], out int b))
        {
            return false;
        }

        return nodes.East[a] != nodes.East[b] && nodes.North[a] != nodes.North[b];
    }

    /// <summary>A generated network at the shipped foot-path rate.</summary>
    private static RoadGraph Laid()
    {
        World world = new(citizens: 100, RoadFixtures.With(RoadFixtures.Roads(arterials: 0)));

        RoadGenerator.LayInto(world.Roads, WorldKey.FromSeed(0x5E_5E_5E), ExtentTiles);
        world.Roads.RebuildDerived();

        return world.Roads;
    }
}
