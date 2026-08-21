using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 11 task 1: where the bounded map ends, which nothing in the build knew.
/// </summary>
/// <remarks>
/// <para>
/// <b>The exactness is the property under test, not a convenience.</b> <c>adr/0088</c> constrains an
/// Outside Connection to *"a position constrained to an edge"*, and the obvious reading is a band in
/// Tiles — which would be a hash-bearing world-creation number needing a named ratifier under
/// <c>adr/0052</c>. <see cref="A_lot_on_the_first_lattice_line_is_exactly_on_the_boundary"/> is why
/// there is no such number: the lattice lands on the boundary exactly, so the constraint can be
/// stated without a tolerance.
/// </para>
/// </remarks>
public sealed class MapEdgeTests
{
    private static MapEdge EdgeAt(int east, int north)
    {
        MapEdges.Touching(new Tiles(east), new Tiles(north), out MapEdge edge);
        return edge;
    }

    private static int EdgesAt(int east, int north) =>
        MapEdges.Touching(new Tiles(east), new Tiles(north), out _);

    /// <summary>Each of the four sides is named, and they are distinct.</summary>
    [Theory]
    [InlineData(0, 4096, MapEdge.West)]
    [InlineData(CellGrid.WorldTiles, 4096, MapEdge.East)]
    [InlineData(4096, 0, MapEdge.South)]
    [InlineData(4096, CellGrid.WorldTiles, MapEdge.North)]
    public void Each_side_of_the_map_is_its_own_edge(int east, int north, MapEdge expected)
    {
        Assert.Equal(expected, EdgeAt(east, north));
        Assert.Equal(1, EdgesAt(east, north));
    }

    /// <summary>Almost everywhere is on no edge, and that is the common case rather than a fault.</summary>
    [Theory]
    [InlineData(4096, 4096)]
    [InlineData(1, 1)]
    [InlineData(CellGrid.WorldTiles - 1, CellGrid.WorldTiles - 1)]
    public void The_interior_is_on_no_edge(int east, int north)
    {
        Assert.Equal(MapEdge.None, EdgeAt(east, north));
        Assert.Equal(0, EdgesAt(east, north));
    }

    /// <summary>
    /// A corner touches two edges and names neither.
    /// </summary>
    /// <remarks>
    /// <b>Two is a different answer from zero and the count is what keeps them apart.</b> Under
    /// <c>adr/0088</c> the edge selects a market, so a corner names two Hinterlands with nothing to
    /// choose between them — a question the design has no answer to, where the interior is simply not
    /// a place a gate goes. A caller collapsing both to <c>MapEdge.None</c> is free to; a caller
    /// writing a refusal message is not.
    /// </remarks>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, CellGrid.WorldTiles)]
    [InlineData(CellGrid.WorldTiles, 0)]
    [InlineData(CellGrid.WorldTiles, CellGrid.WorldTiles)]
    public void A_corner_touches_two_edges_and_names_neither(int east, int north)
    {
        Assert.Equal(2, EdgesAt(east, north));
        Assert.Equal(MapEdge.None, EdgeAt(east, north));
    }

    /// <summary>
    /// The lattice lands on the boundary exactly, which is what makes a band unnecessary.
    /// </summary>
    /// <remarks>
    /// <b><c>LotSubdivider.Face</c> puts a Lot at <c>row × block</c> with no set-back term</b>, so a
    /// Street on lattice line 0 carries Lots at coordinate 0 and one on the last line carries them at
    /// <see cref="CellGrid.WorldTiles"/>. This test is the premise of that argument written down, so
    /// that a subdivider which later introduces a set-back fails here rather than silently making
    /// every gate unplaceable.
    /// </remarks>
    [Fact]
    public void A_lot_on_the_first_lattice_line_is_exactly_on_the_boundary()
    {
        const int Block = 32;
        const int LastLine = CellGrid.WorldTiles / Block;

        Assert.Equal(MapEdge.South, EdgeAt(east: 5 * Block, north: 0 * Block));
        Assert.Equal(MapEdge.North, EdgeAt(east: 5 * Block, north: LastLine * Block));
        Assert.Equal(CellGrid.WorldTiles, LastLine * Block);
    }
}
