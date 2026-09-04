using Borough.Core.Arithmetic;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// <see cref="BlockLattice"/> — <b><c>block_tiles</c> was two functions and not a number, and this
/// is the pair of them.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>EVERY ASSERTION HERE IS THE IDENTITY CASE, ON PURPOSE.</b> <c>plans/0045</c> row 25 says
/// the work is <i>finding out how many places believe there is one</i> — and the answer turned out
/// to be that they all believe the same two things: <c>line × block_tiles</c> and
/// <c>FloorDiv(tile, block_tiles)</c>. ***So the first thing this type has to be is the arithmetic
/// it replaces***, exactly, or the seventy-odd sites it was threaded through moved a city nobody
/// meant to move. The State Hash is the other half of that proof and this is the readable half.
/// </para>
/// <para>
/// ⚠ <b>The properties are stated over the WHOLE map and not over a sample</b>, because a
/// transcription error at one line is exactly the shape of defect a sample misses — and the map is
/// 16,384 Tiles, which is a loop and not a cost.
/// </para>
/// </remarks>
public sealed class BlockLatticeTests
{
    /// <summary>The shipped lattice.</summary>
    private const int Block = 32;

    /// <summary>
    /// 🔴 <b>THE PROOF THE REFACTOR MOVED NOTHING: both methods are the expressions they replaced.</b>
    /// </summary>
    [Theory]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(512)]
    [InlineData(30)]
    public void An_even_lattice_answers_exactly_as_the_arithmetic_did(int block)
    {
        BlockLattice lattice = BlockLattice.Even(block);

        Assert.Equal(IntegerMath.FloorDiv(CellGrid.WorldTiles, block) + 1, lattice.Lines);

        for (int line = 0; line < lattice.Lines; line++)
        {
            Assert.Equal(line * block, lattice.EdgeOf(line));
        }

        for (int tile = 0; tile <= CellGrid.WorldTiles; tile++)
        {
            Assert.Equal(IntegerMath.FloorDiv(tile, block), lattice.LineAt(tile));
        }
    }

    /// <summary>
    /// The two questions are inverses: a Tile is in the block its own line starts.
    /// </summary>
    /// <remarks>
    /// <b>This is the property, and it is the one that will still be here when the lines stop being
    /// evenly spaced.</b> Everything else in this file is the identity case; this is the invariant
    /// the identity case is a special case of.
    /// </remarks>
    [Fact]
    public void A_tile_lies_between_its_own_line_and_the_next()
    {
        BlockLattice lattice = BlockLattice.Even(Block);

        for (int tile = 0; tile < CellGrid.WorldTiles; tile++)
        {
            int line = lattice.LineAt(tile);

            Assert.True(
                lattice.EdgeOf(line) <= tile && tile < lattice.EdgeOf(line + 1),
                $"Tile {tile} landed in block {line}, which runs "
                    + $"{lattice.EdgeOf(line)}..{lattice.EdgeOf(line + 1)}");
        }
    }

    /// <summary>The widths tile the map, so no ground falls between two blocks.</summary>
    [Fact]
    public void The_widths_sum_to_the_span_the_lines_cover()
    {
        BlockLattice lattice = BlockLattice.Even(Block);

        long total = 0;

        for (int block = 0; block < lattice.Blocks; block++)
        {
            Assert.True(lattice.WidthOf(block) > 0, $"block {block} came out {lattice.WidthOf(block)}");

            total += lattice.WidthOf(block);
        }

        Assert.Equal(lattice.EdgeOf(lattice.Lines - 1) - lattice.EdgeOf(0), total);
    }

    /// <summary>
    /// ⚠ <b>Past the last line it CONTINUES rather than clamping</b>, because callers address the
    /// block beyond the lattice.
    /// </summary>
    /// <remarks>
    /// <c>RoadGenerator.Layout.Reach</c> and <c>SyntheticCity.Subdivide</c> both reach one block past
    /// the extent — the block beyond the east edge has that edge's Segments as its west face, so it
    /// carries Lots. ***A clamp there would stack every out-of-range address on the last line***,
    /// which is a silent collision rather than an error.
    /// </remarks>
    [Fact]
    public void It_continues_past_both_ends_rather_than_clamping()
    {
        BlockLattice lattice = BlockLattice.Even(Block);

        Assert.Equal(lattice.Lines * Block, lattice.EdgeOf(lattice.Lines));
        Assert.Equal((lattice.Lines + 3) * Block, lattice.EdgeOf(lattice.Lines + 3));
        Assert.Equal(-Block, lattice.EdgeOf(-1));
        Assert.Equal(-1, lattice.LineAt(-1));
        Assert.Equal(-2, lattice.LineAt(-Block - 1));
        Assert.Equal(lattice.Lines, lattice.LineAt(CellGrid.WorldTiles + Block));
    }

    /// <summary>
    /// <see cref="BlockLattice.LinesIn"/> is <c>FloorDiv(extent, block) + 1</c>, written out.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 31)]
    [InlineData(0, 32)]
    [InlineData(0, 1024)]
    [InlineData(2048, 544)]
    [InlineData(8192, 4096)]
    public void The_line_count_in_an_extent_is_the_division_it_replaced(int from, int extent)
    {
        BlockLattice lattice = BlockLattice.Even(Block);

        Assert.Equal(IntegerMath.FloorDiv(extent, Block) + 1, lattice.LinesIn(from, extent));
    }

    /// <summary>
    /// A window sized in blocks is sized by <see cref="BlockLattice.Narrowest"/>, which is the
    /// nominal one here and will not always be.
    /// </summary>
    [Fact]
    public void An_even_lattice_is_its_own_narrowest_and_widest_block()
    {
        BlockLattice lattice = BlockLattice.Even(Block);

        Assert.True(lattice.Uniform);
        Assert.Equal(Block, lattice.Nominal);
        Assert.Equal(Block, lattice.Narrowest);
        Assert.Equal(Block, lattice.Widest);
    }

    /// <summary>A world with no roads answers rather than dividing by zero.</summary>
    /// <remarks>
    /// <c>RoadRuleset.None</c> is a real shipped world — <c>--empty</c> boots one — and every guard
    /// in the project spells it <c>block_tiles &lt;= 0</c>. This is that guard's other side.
    /// </remarks>
    [Fact]
    public void A_world_with_no_roads_has_no_lines_and_answers_anyway()
    {
        foreach (BlockLattice lattice in new[] { BlockLattice.None, BlockLattice.Even(0), BlockLattice.Even(-8) })
        {
            Assert.Equal(0, lattice.Lines);
            Assert.Equal(0, lattice.Blocks);
            Assert.Equal(0, lattice.Nominal);
            Assert.Equal(0, lattice.EdgeOf(4));
            Assert.Equal(0, lattice.LineAt(4_096));
            Assert.Equal(1, lattice.LinesIn(0, 4_096));
        }
    }
}
