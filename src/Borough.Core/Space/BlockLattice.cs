using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// <b>Where the Street lattice's lines stand, and which block a Tile is in</b> — the two operations
/// <c>[roads] block_tiles</c> was standing in for everywhere at once.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><c>block_tiles</c> IS NOT A NUMBER THIS PROJECT READS. IT IS TWO FUNCTIONS NOBODY NAMED.</b>
/// Every one of its read sites is <c>line × block_tiles</c> — <em>where does this line stand</em> —
/// or <c>FloorDiv(tile, block_tiles)</c> — <em>which block is this Tile in</em>. <c>plans/0045</c>
/// row 25 says the work is <i>finding out how many places believe there is one</i>, and this is what
/// they turned out to believe: not that blocks are 32 Tiles, but that <b>the answer to both
/// questions is arithmetic on one integer</b>. ***A uniform lattice is not a value anywhere; it is
/// the shape of the two expressions that ask about it.***
/// </para>
/// <para>
/// <b>So the spacing is a table and the two questions are methods.</b>
/// <see cref="EdgeOf"/> is the first, <see cref="LineAt"/> the second, and
/// <see cref="WidthOf"/> is the difference the second one used to be unable to express. A lattice
/// whose lines are evenly spaced answers exactly as the arithmetic did — which is what makes the
/// change to this shape provable rather than argued: the State Hash does not move
/// (<c>adr/0100</c>).
/// </para>
/// <para>
/// ⚠ <b>The lines are SEPARABLE and that is a design constraint rather than a convenience.</b> One
/// spacing along each axis, so every line runs the full width or height of the map and the
/// intersections stay addressable as <c>(column, row)</c>. **That is what every caller of
/// <see cref="StreetGrid"/> and <see cref="RoadGraph"/> already assumes**, and it is also a real
/// morphology rather than a compromise — Manhattan's avenues and streets are two different spacings
/// laid across each other. A lattice of arbitrary quadrilaterals would be a different data structure
/// and a different row.
/// </para>
/// <para>
/// ⚠ <b><see cref="Nominal"/> is not <see cref="WidthOf"/> and the distinction is the whole survey.</b>
/// Some callers want <em>this block's width</em>; some want <em>a block, as a length</em> — a search
/// radius, a reach, an overlap margin. The second kind is not wrong to be uniform and must not be
/// mechanically rewritten into the first, so both are on the type and each site says which it meant.
/// </para>
/// <para>
/// ⚠ <b>The reverse table is O(1) and costs 64 KB.</b> <see cref="LineAt"/> is on the path
/// <see cref="LineSourceQueries"/> and <see cref="TrafficPresence"/> walk, so a binary search over
/// the lines would put a nine-step loop where a divide used to be. Against
/// <see cref="StreetGrid"/>'s own megabyte of node index this is noise, and it is stated here rather
/// than discovered later.
/// </para>
/// </remarks>
public sealed class BlockLattice
{
    /// <summary>The Tile each line stands on, in line order. Length <see cref="Lines"/>.</summary>
    private int[] _edge = [];

    /// <summary>The block each Tile is in, by Tile. Length <see cref="CellGrid.WorldTiles"/> + 1.</summary>
    private int[] _line = [];

    /// <summary>Lines along one edge of the map. Zero where the world has no roads.</summary>
    public int Lines { get; private set; }

    /// <summary>Blocks along one edge of the map — one fewer than the lines.</summary>
    public int Blocks => Lines > 0 ? Lines - 1 : 0;

    /// <summary>
    /// <b>A block as a LENGTH</b> — <c>[roads] block_tiles</c> itself, and not any particular block's
    /// width.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Read this where a block is a unit of distance and <see cref="WidthOf"/> where it is a
    /// piece of ground.</b> A reach, a search radius, an overlap margin and a diagonal are the first
    /// kind; a Segment's length, a Lot's frontage and a parcel's carve are the second. ***Confusing
    /// them is how a uniform lattice hides inside a non-uniform one.***
    /// </remarks>
    public int Nominal { get; private set; }

    /// <summary>
    /// <b>The narrowest block on the lattice</b> — <see cref="Nominal"/> where the lines are evenly
    /// spaced.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THIS IS WHAT A SPATIAL WINDOW MUST BE SIZED IN, AND IT IS THE ONE PLACE THE UNIFORM
    /// ASSUMPTION FAILS SILENTLY.</b> <see cref="LineSourceQueries"/> converts a range in Tiles into
    /// a window in blocks by <c>CeilDiv(range, block)</c>, whose correctness rests on <em>a block IS
    /// the lattice pitch</em>. On a lattice with a block narrower than the nominal one, a window
    /// sized by the nominal covers less ground than it believes and the query returns a quiet, wrong
    /// answer — ***the failure a smaller number cannot announce.*** So a window is sized by the
    /// narrowest block and never by the mean.
    /// </remarks>
    public int Narrowest { get; private set; }

    /// <summary>The widest block on the lattice.</summary>
    /// <remarks>
    /// The counterpart for anything that must reach across a whole block rather than step over one.
    /// </remarks>
    public int Widest { get; private set; }

    /// <summary>Whether the lines are evenly spaced.</summary>
    /// <remarks>
    /// <b>Recorded rather than tested for</b>, so a caller that genuinely needs to know can ask
    /// without walking the table — and so a test can assert that the uniform case still is one.
    /// </remarks>
    public bool Uniform { get; private set; }

    /// <summary>A lattice with no lines at all, which is <c>RoadRuleset.None</c>'s world.</summary>
    public static BlockLattice None => new();

    /// <summary>
    /// The evenly spaced lattice <c>block_tiles</c> has always described.
    /// </summary>
    /// <remarks>
    /// <b>The identity case, and it is what makes routing every call site through this type
    /// provable.</b> <c>EdgeOf(line)</c> is <c>line × blockTiles</c> and <c>LineAt(tile)</c> is
    /// <c>FloorDiv(tile, blockTiles)</c>, exactly as the expressions it replaces were written, so
    /// the golden baseline is the proof that nothing was mistranscribed across the sites.
    /// </remarks>
    public static BlockLattice Even(int blockTiles)
    {
        var lattice = new BlockLattice();

        if (blockTiles <= 0)
        {
            return lattice;
        }

        int lines = IntegerMath.FloorDiv(CellGrid.WorldTiles, blockTiles) + 1;

        lattice.Nominal = blockTiles;
        lattice.Narrowest = blockTiles;
        lattice.Widest = blockTiles;
        lattice.Lines = lines;
        lattice.Uniform = true;
        lattice._edge = new int[lines];
        lattice._line = new int[CellGrid.WorldTiles + 1];

        for (int line = 0; line < lines; line++)
        {
            lattice._edge[line] = line * blockTiles;
        }

        for (int tile = 0; tile <= CellGrid.WorldTiles; tile++)
        {
            lattice._line[tile] = IntegerMath.FloorDiv(tile, blockTiles);
        }

        return lattice;
    }

    /// <summary>The Tile line <paramref name="line"/> stands on.</summary>
    /// <remarks>
    /// ⚠ <b>Extrapolated past the last line rather than clamped</b>, because callers address the
    /// block beyond the lattice's far edge — <c>RoadGenerator.Layout.Reach</c>'s extra block is Lots
    /// and not roads — and a clamp there would silently stack them on the edge. Past the end it
    /// continues at <see cref="Nominal"/>, which is the spacing the ground beyond a lattice has.
    /// </remarks>
    public int EdgeOf(int line)
    {
        if (Lines <= 0)
        {
            return 0;
        }

        if (line < 0)
        {
            return line * Nominal;
        }

        return line < Lines
            ? _edge[line]
            : _edge[Lines - 1] + ((line - (Lines - 1)) * Nominal);
    }

    /// <summary>The block <paramref name="tile"/> stands in.</summary>
    /// <remarks>
    /// <b>Floored, so a Tile on a line belongs to the block east or north of it</b> — which is
    /// <c>FloorDiv</c>'s own answer and the one <c>Simulation.ApplyConnect</c>'s snap is stated in
    /// terms of. Outside the map it extrapolates for the reason <see cref="EdgeOf"/> does.
    /// </remarks>
    public int LineAt(int tile)
    {
        if (Lines <= 0)
        {
            return 0;
        }

        if (tile < 0)
        {
            return IntegerMath.FloorDiv(tile, Nominal);
        }

        return tile < _line.Length
            ? _line[tile]
            : Blocks + IntegerMath.FloorDiv(tile - _edge[Lines - 1], Nominal);
    }

    /// <summary>
    /// How many lines stand within <paramref name="extentTiles"/> of the line on
    /// <paramref name="fromTile"/>, counting that one.
    /// </summary>
    /// <remarks>
    /// <b><c>FloorDiv(extent, block) + 1</c>, written out.</b> Two callers computed it that way —
    /// <c>RoadGenerator.Layout</c>'s node grid and <c>SyntheticCity.PavedTiles</c>' block span — and
    /// the divide is the uniform assumption spelled as arithmetic. Walked rather than divided, once
    /// per lattice at world creation.
    /// </remarks>
    public int LinesIn(int fromTile, int extentTiles)
    {
        if (Nominal <= 0)
        {
            return 1;
        }

        int from = EdgeOf(LineAt(fromTile));
        int line = LineAt(fromTile);
        int lines = 1;

        while (EdgeOf(line + lines) - from <= extentTiles)
        {
            lines++;
        }

        return lines;
    }

    /// <summary>How wide block <paramref name="block"/> is, in Tiles.</summary>
    public int WidthOf(int block) => EdgeOf(block + 1) - EdgeOf(block);

    /// <summary>The Tile line <paramref name="line"/> stands on, as a <see cref="Tiles"/>.</summary>
    public Tiles TileOf(int line) => new(EdgeOf(line));
}
