using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// The Cell grid, the Chunk grid, and the strict-multiple property the split was bought with.
/// </summary>
/// <remarks>
/// <b><c>adr/0034</c> split two decisions that shared one number.</b> The Cell is a design constant
/// whose size changes the State Hash; the Chunk is a performance partition whose size does not. The
/// split cost almost nothing precisely because the Chunk is a strict multiple of the Cell — every
/// conversion is a shift and no boundary can disagree with another. These tests are that claim, since
/// it is the one that would quietly stop being true when somebody moves the Chunk.
/// </remarks>
public class CellGridTests
{
    [Fact]
    public void The_Cell_is_thirty_two_Tiles_and_the_map_is_a_whole_number_of_them()
    {
        Assert.Equal(32, CellGrid.TilesPerCell);
        Assert.Equal(1 << CellGrid.TilesPerCellShift, CellGrid.TilesPerCell);
        Assert.Equal(4_096, CellGrid.WorldTiles);
        Assert.Equal(128, CellGrid.WorldCells);
        Assert.Equal(CellGrid.WorldTiles, CellGrid.WorldCells * CellGrid.TilesPerCell);
    }

    /// <summary>
    /// The Chunk is a strict multiple of the Cell, which is what makes every conversion a shift.
    /// </summary>
    /// <remarks>
    /// <b>This test outlives the current 1:1.</b> It asserts the property rather than the value, so
    /// moving the Chunk to 8 or 16 Cells — which S2 R3 has already narrowed it toward — keeps it green,
    /// and setting it to something that is not a power of two breaks it. That is the failure worth
    /// catching: a Chunk of 3 Cells still <em>works</em>, and every index conversion silently becomes
    /// a division whose rounding nobody stated.
    /// </remarks>
    [Fact]
    public void The_Chunk_is_a_strict_power_of_two_multiple_of_the_Cell()
    {
        Assert.Equal(1 << ChunkGrid.CellsPerChunkShift, ChunkGrid.CellsPerChunk);
        Assert.Equal(ChunkGrid.CellsPerChunk * CellGrid.TilesPerCell, ChunkGrid.TilesPerChunk);
        Assert.True(ChunkGrid.TilesPerChunk >= 32, "02 §2.1: a Chunk is at least 32×32 Tiles.");
    }

    /// <summary>
    /// Every Cell in a Chunk maps back to that Chunk. The boundaries cannot disagree.
    /// </summary>
    [Fact]
    public void Cells_and_Chunks_agree_about_every_boundary()
    {
        for (int cell = 0; cell < CellGrid.WorldCells; cell++)
        {
            Chunks chunk = ChunkGrid.ToChunks(new Cells(cell));
            Cells corner = ChunkGrid.ToCells(chunk);

            Assert.True(corner.Raw <= cell);
            Assert.True(cell < corner.Raw + ChunkGrid.CellsPerChunk);
        }
    }

    /// <summary>
    /// Tile to Cell floors rather than truncating, so Tile −1 is in Cell −1.
    /// </summary>
    /// <remarks>
    /// The distinction only shows west and south of the origin, which is off the map — but a kernel
    /// offset routinely produces a negative coordinate, and a conversion that truncated would fold
    /// Tiles −31 through 31 into one Cell twice the size of every other.
    /// </remarks>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(31, 0)]
    [InlineData(32, 1)]
    [InlineData(-1, -1)]
    [InlineData(-32, -1)]
    [InlineData(-33, -2)]
    public void A_Tile_falls_in_the_Cell_below_it(int tile, int cell)
    {
        Assert.Equal(cell, CellGrid.ToCells(new Tiles(tile)).Raw);
    }

    /// <summary>A range in metres becomes the Cell extent that covers it, rounded up.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(128, 1)]
    [InlineData(129, 2)]
    [InlineData(1_024, 8)]
    [InlineData(10_000, 79)]
    public void A_range_in_metres_rounds_up_to_Cells(int metres, int cells)
    {
        Assert.Equal(cells, CellGrid.FromMetres(metres).Raw);
    }

    [Fact]
    public void Off_map_coordinates_are_outside_rather_than_wrapped()
    {
        Assert.True(CellGrid.Contains(Cells.Zero, Cells.Zero));
        Assert.True(CellGrid.Contains(
            new Cells(CellGrid.WorldCells - 1), new Cells(CellGrid.WorldCells - 1)));

        Assert.False(CellGrid.Contains(new Cells(-1), Cells.Zero));
        Assert.False(CellGrid.Contains(Cells.Zero, new Cells(-1)));
        Assert.False(CellGrid.Contains(new Cells(CellGrid.WorldCells), Cells.Zero));
    }

    /// <summary>A dilated halo clamps to the map without folding to the wrong rectangle.</summary>
    [Fact]
    public void A_halo_at_the_origin_clamps_to_the_quadrant_that_is_on_the_map()
    {
        CellRect halo = CellRect.At(Cells.Zero, Cells.Zero).Dilate(new Cells(8)).Clamp();

        Assert.Equal(0, halo.East.Raw);
        Assert.Equal(0, halo.North.Raw);
        Assert.Equal(9, halo.Width.Raw);
        Assert.Equal(9, halo.Height.Raw);
    }

    [Fact]
    public void A_rectangle_entirely_off_the_map_clamps_to_nothing()
    {
        CellRect off = new(new Cells(-100), new Cells(-100), new Cells(10), new Cells(10));

        Assert.True(off.Clamp().IsEmpty);
    }

    [Fact]
    public void A_union_covers_both_rectangles_and_everything_between()
    {
        CellRect first = CellRect.At(new Cells(3), new Cells(4));
        CellRect second = CellRect.At(new Cells(10), new Cells(2));
        CellRect both = first.Union(second);

        Assert.True(both.Contains(new Cells(3), new Cells(4)));
        Assert.True(both.Contains(new Cells(10), new Cells(2)));
        Assert.Equal(8, both.Width.Raw);
        Assert.Equal(3, both.Height.Raw);
    }

    [Fact]
    public void Union_with_nothing_changes_nothing()
    {
        CellRect one = CellRect.At(new Cells(5), new Cells(6));

        Assert.Equal(one, one.Union(CellRect.Empty));
        Assert.Equal(one, CellRect.Empty.Union(one));
    }
}
