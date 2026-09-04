namespace Borough.Core.Space;

/// <summary>
/// <b>One block's ground</b> — where it stands and how big it is, which are four numbers and used to
/// be one.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><see cref="BlockPatterns.Carve"/> TOOK A SINGLE <c>blockTiles</c> AND PRODUCED FOUR FACES,
/// SO A BLOCK WAS SQUARE BY CONSTRUCTION.</b> That is true of an evenly spaced lattice and of
/// nothing else, and it is the assumption <c>plans/0045</c> row 25 removes: on a lattice whose lines
/// vary, block <c>(c, r)</c> is <see cref="BlockLattice.WidthOf"/> <c>(c)</c> across by
/// <c>WidthOf(r)</c> deep, and the two need not be equal. ***A carve given one number cannot say
/// which of the two it was given.***
/// </para>
/// <para>
/// <b>A struct rather than four more parameters</b>, because the carve already takes a key, a
/// pattern and an Address count, and a ten-parameter geometry call is where a width and a depth get
/// swapped without anything noticing. It also carries the block's own origin Tile, which
/// <see cref="BlockPatterns.Carve"/> used to compute as <c>column × blockTiles</c> — the other half
/// of the same assumption.
/// </para>
/// <para>
/// ⚠ <b><see cref="Square"/> is the LADDER's block and not a particular one.</b>
/// <see cref="BlockPatterns.ClaimedTiles"/>, <see cref="BlockPatterns.AddressCount"/> and
/// <see cref="BlockPatterns.Ladder"/> rank the patterns against each other, and a ranking that moved
/// from block to block would not be a property of the pattern set at all — the same argument
/// <c>plans/0060</c> makes for keeping the ladder off the world seed. So they carve a nominal square
/// block and say so.
/// </para>
/// </remarks>
/// <param name="Column">The block's lattice column, which is what its module is drawn on.</param>
/// <param name="Row">The block's lattice row.</param>
/// <param name="East">The Tile its west edge stands on.</param>
/// <param name="North">The Tile its south edge stands on.</param>
/// <param name="Wide">How far it runs east, in Tiles.</param>
/// <param name="Deep">How far it runs north, in Tiles.</param>
public readonly record struct BlockGround(
    int Column, int Row, int East, int North, int Wide, int Deep)
{
    /// <summary>The block at <c>(column, row)</c> on a lattice.</summary>
    public static BlockGround At(BlockLattice lattice, int column, int row)
    {
        ArgumentNullException.ThrowIfNull(lattice);

        return new BlockGround(
            column,
            row,
            lattice.EdgeOf(column),
            lattice.EdgeOf(row),
            lattice.WidthOf(column),
            lattice.WidthOf(row));
    }

    /// <summary>
    /// A nominal square block at the origin — <b>the ladder's block</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Read the type's own remark before using this.</b> It is right for a comparison between
    /// patterns and wrong for a carve on the ground, and the two are one method call apart.
    /// </remarks>
    public static BlockGround Square(int blockTiles) =>
        new(0, 0, 0, 0, blockTiles, blockTiles);

    /// <summary>The smaller of the two extents.</summary>
    /// <remarks>
    /// <b>What the plot module is a fraction of</b>, so that one block has one module however
    /// oblong it is — Tait's <em>regular within a block</em>, which a per-face module would break.
    /// ⚠ <b>The narrower side and not the mean</b>: a module taken off the long side would be too
    /// coarse for the short face to divide at all.
    /// </remarks>
    public int Least => Wide < Deep ? Wide : Deep;

    /// <summary>How far the face runs along, in Tiles.</summary>
    public int Along(BlockFace face) =>
        face is BlockFace.South or BlockFace.North ? Wide : Deep;

    /// <summary>How far the depth behind the face runs into the block, in Tiles.</summary>
    public int Across(BlockFace face) =>
        face is BlockFace.South or BlockFace.North ? Deep : Wide;
}
