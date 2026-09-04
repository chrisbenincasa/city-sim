using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// <see cref="StreetGrid.NearestEdge"/>: which lattice edge a point in a block is nearest, and that
/// the Tile naming it survives the city's own snap.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>THIS IS THE TEST THAT WOULD HAVE CAUGHT IT.</b> The Street tool floored a click to the
/// block's south-west corner and split the axis on a diagonal, so ***a click near a block's
/// top-right corner laid a Street at its bottom-left*** — and the playtest that found forty Segments
/// from forty clicks missed it entirely, because all forty were on face midpoints and a face
/// midpoint is the one place the two rules agree. <c>plans/0045</c> row 22.
/// </para>
/// <para>
/// <b>Thirteen points in one block, and the set is chosen to separate the two rules rather than to
/// cover the block.</b> Four face midpoints and four points just inside the faces pass under the old
/// rule and the new one alike; four corners are where the old rule named an edge of the wrong corner;
/// and the centre is the one place there is no right answer, so the tie-break is pinned instead.
/// </para>
/// </remarks>
public sealed class StreetAimTests
{
    /// <summary>The lattice these run against, in Tiles between intersections.</summary>
    /// <remarks>
    /// <b>32, which is what every shipped <c>[roads] block_tiles</c> states</b> — the size the defect
    /// was seen at, rather than <see cref="RoadFixtures"/>'s widened 512. Nothing here lays a
    /// Segment, so the generator's cost does not arise and the real number is free.
    /// </remarks>
    private const int Block = 32;

    /// <summary>A block well inside the map, so no clamp is in play. Its south-west Tile.</summary>
    private const int Origin = 8_192;

    /// <summary>The column and row that <see cref="Origin"/> stands at.</summary>
    private const int At = Origin / Block;

    [Theory]

    // The four corners, each a few Tiles in from its own intersection. THE OLD RULE ANSWERED AN EDGE
    // OF THE SOUTH-WEST CORNER for three of these four, and the whole defect is in this block of
    // rows. ⚠ The offsets are deliberately unequal: a corner reached by the same distance on both
    // axes is a genuine tie between two edges, which is a different question and is asked below.
    [InlineData(3, 5, At, At, StreetAxis.North)]
    [InlineData(29, 5, At + 1, At, StreetAxis.North)]
    [InlineData(5, 29, At, At + 1, StreetAxis.East)]
    [InlineData(29, 27, At + 1, At, StreetAxis.North)]

    // The four face midpoints -- the only points the old rule got right, and the reason it survived
    // a forty-click playtest.
    [InlineData(16, 0, At, At, StreetAxis.East)]
    [InlineData(16, 32, At, At + 1, StreetAxis.East)]
    [InlineData(0, 16, At, At, StreetAxis.North)]
    [InlineData(32, 16, At + 1, At, StreetAxis.North)]

    // Just inside each face, where the answer is unambiguous and the margin is one Tile.
    [InlineData(16, 1, At, At, StreetAxis.East)]
    [InlineData(16, 31, At, At + 1, StreetAxis.East)]
    [InlineData(1, 16, At, At, StreetAxis.North)]
    [InlineData(31, 16, At + 1, At, StreetAxis.North)]
    public void A_click_resolves_to_the_edge_it_is_nearest(
        int alongEast, int alongNorth, int column, int row, StreetAxis axis)
    {
        StreetGrid streets = Lattice();

        Assert.Equal(
            (column, row, axis),
            streets.NearestEdge(
                new Tiles(Origin + alongEast), new Tiles(Origin + alongNorth)));
    }

    /// <summary>
    /// The dead centre is equidistant from all four faces and gets the stated order, not a coin.
    /// </summary>
    /// <remarks>
    /// <b>There is no right answer here and that is the point of pinning one.</b> A tie broken by the
    /// comparison's incidental direction is a tie broken by whoever last edited the loop, and the same
    /// click would then lay different Streets in two builds — which is exactly the surprise this row
    /// exists to remove.
    /// </remarks>
    [Fact]
    public void The_centre_of_a_block_falls_to_the_south_face()
    {
        StreetGrid streets = Lattice();

        Assert.Equal(
            (At, At, StreetAxis.East),
            streets.NearestEdge(
                new Tiles(Origin + (Block / 2)), new Tiles(Origin + (Block / 2))));
    }

    /// <summary>
    /// 🔴 <b>The claim the shell's fix rests on: the city's floor cannot move the aim.</b>
    /// </summary>
    /// <remarks>
    /// <c>Simulation.ApplyConnect</c> takes <c>FloorDiv(tile, block_tiles)</c> of whatever Tile the
    /// Command names — <c>adr/0014</c>'s snap, and untouched by row 22 because moving it would change
    /// what every already-recorded <c>.borough</c> log replays to. ***So the shell's aim is only
    /// honest if the Tile it sends floors back to the edge it chose***, which is what this walks every
    /// Tile of one block to check.
    /// </remarks>
    [Fact]
    public void The_tile_an_edge_is_addressed_by_floors_back_to_that_edge()
    {
        StreetGrid streets = Lattice();

        for (int alongEast = 0; alongEast < Block; alongEast++)
        {
            for (int alongNorth = 0; alongNorth < Block; alongNorth++)
            {
                (int column, int row, _) = streets.NearestEdge(
                    new Tiles(Origin + alongEast), new Tiles(Origin + alongNorth));

                (Tiles east, Tiles north) = streets.IntersectionTile(column, row);

                Assert.Equal(column, IntegerMath.FloorDiv(east.Raw, Block));
                Assert.Equal(row, IntegerMath.FloorDiv(north.Raw, Block));
            }
        }
    }

    /// <summary>
    /// No click anywhere in the block is ever further than half a block from the edge it names.
    /// </summary>
    /// <remarks>
    /// <b>The property the old rule failed, stated as a bound rather than as a list of points.</b>
    /// Flooring answered an edge up to a whole block away — 31 Tiles at the far corner — and
    /// <em>nearest</em> is worth having only if it is nearest. A bound catches a re-derivation that
    /// gets eight of the twelve rows above right and the ninth wrong.
    /// </remarks>
    [Fact]
    public void No_click_is_further_than_half_a_block_from_the_edge_it_names()
    {
        StreetGrid streets = Lattice();

        for (int alongEast = 0; alongEast < Block; alongEast++)
        {
            for (int alongNorth = 0; alongNorth < Block; alongNorth++)
            {
                (int column, int row, StreetAxis axis) = streets.NearestEdge(
                    new Tiles(Origin + alongEast), new Tiles(Origin + alongNorth));

                // The perpendicular distance to the edge's own line: an east edge runs along a row,
                // so what is measured is the northward gap, and the other way for a north edge.
                int away = axis == StreetAxis.East
                    ? IntegerMath.Abs(Origin + alongNorth - (row * Block))
                    : IntegerMath.Abs(Origin + alongEast - (column * Block));

                Assert.True(
                    away <= Block / 2,
                    $"({alongEast}, {alongNorth}) named an edge {away} Tiles away");
            }
        }
    }

    /// <summary>A world with no lattice answers <see cref="Rows.NoSlot"/> rather than dividing by it.</summary>
    /// <remarks>
    /// <b>The <c>--empty</c> world before the first Street exists is not this case.</b> That world has
    /// a lattice and no Segments on it; this is a Ruleset that states no <c>[roads] block_tiles</c> at
    /// all, which <c>Refusal.ConnectWorldHasNoLattice</c> is the sentence for.
    /// </remarks>
    [Fact]
    public void A_world_with_no_lattice_names_no_edge()
    {
        var streets = new StreetGrid();

        Assert.Equal(
            (Core.Tables.Rows.NoSlot, Core.Tables.Rows.NoSlot, StreetAxis.East),
            streets.NearestEdge(new Tiles(Origin), new Tiles(Origin)));
    }

    /// <summary>An empty lattice at <see cref="Block"/> Tiles — the geometry and no Segments.</summary>
    /// <remarks>
    /// <b>No Segment is laid because none is asked about.</b> Every assertion here is about which
    /// edge a point names, which is arithmetic on the grid spacing — <see cref="StreetGrid.SegmentOn"/>
    /// is what the shell asks afterwards, and it is tested where the graph is.
    /// </remarks>
    private static StreetGrid Lattice()
    {
        RoadGraph graph = new(RoadFixtures.Roads(blockTiles: Block, arterials: 0));

        graph.RebuildDerived();

        return graph.Streets;
    }
}
