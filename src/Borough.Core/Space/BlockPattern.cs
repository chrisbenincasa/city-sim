namespace Borough.Core.Space;

using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

/// <summary>
/// Which of a block's four faces a parcel fronts.
/// </summary>
/// <remarks>
/// <b>The order is <see cref="LotSubdivider"/>'s existing call order and moving it moves the State
/// Hash.</b> Lots are created in this order, so it is the order of the rows in the table and therefore
/// of everything folded from them.
/// </remarks>
public enum BlockFace : byte
{
    /// <summary>The horizontal Segment at the block's own lattice row. The block takes its north side.</summary>
    South = 0,

    /// <summary>The horizontal Segment one row up. The block takes its south side.</summary>
    North = 1,

    /// <summary>The vertical Segment at the block's own lattice column. The block takes its east side.</summary>
    West = 2,

    /// <summary>The vertical Segment one column along. The block takes its west side.</summary>
    East = 3,
}

/// <summary>
/// <b>How a block is subdivided</b> — one of three real subdivisions of a real city block.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 3, and <c>adr/0025</c>'s <em>"Lot subdivision must vary by band"</em>
/// becoming a thing that can vary.</b> Before this the subdivider had one shape hard-coded into it —
/// four faces, a strip each, dead ground in the middle — and that shape was not wrong, it was
/// <em>the only one</em>. ***A city made of one subdivision looks like one place whatever else varies
/// in it.***
/// </para>
/// <para>
/// ⚠ <b>THE SET IS OPEN AND THREE IS A STARTING POINT.</b> What is decided is not the number but that
/// <b>a pattern declares its own exhaustiveness claim</b> — see <see cref="BlockPatterns.Exhaustive"/>
/// — because that is what lets a suburban block keep its leftover ground without weakening the test
/// that refuses leftover ground everywhere else.
/// </para>
/// <para>
/// 🔴 <b>The mitre was refused and the refusal is the useful part.</b> Four faces each taking a
/// triangle to the block's centre tiles a square exactly and keeps every face's Lots — and
/// <b>buildings are not built that way</b>. At a real corner one street wins and the cross street's
/// terrace begins after the corner building. ***A geometry that tiles is not thereby a geometry that
/// is built.***
/// </para>
/// </remarks>
public enum BlockPattern : byte
{
    /// <summary>
    /// <b>Plots on all four faces, ground left over between the back fences.</b> A suburban block.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>NOT EXHAUSTIVE, DELIBERATELY, and it is the only one of the three that is not.</b> The
    /// leftover ground is <em>correct</em> here: back gardens do not meet, and there is scrub between
    /// them. ⚠ <b>This is what the subdivider did before patterns existed</b>, so it is the default
    /// and a world that never chooses a pattern is the world that was there yesterday.
    /// </remarks>
    Detached = 0,

    /// <summary>
    /// <b>One pair of streets takes the whole block; plots meet along the centre line.</b> A British
    /// terrace, a Portland 200 ft block, Manhattan.
    /// </summary>
    /// <remarks>
    /// ✅ <b>Exhaustive.</b> The cross streets get <b>gable ends</b> — the terrace's end wall faces
    /// them — so they carry no Address at all, which is the one place in this set where a face with a
    /// Street on it yields nothing.
    /// </remarks>
    BackToBack = 1,

    /// <summary>
    /// <b>All four faces; the winning pair takes a shallow strip and the losing pair splits the middle
    /// band down the centre.</b> Barcelona, Paris, Berlin.
    /// </summary>
    /// <remarks>
    /// ✅ <b>Exhaustive for ANY winner-depth</b>, which is what made the depth a free number and
    /// <c>plans/0053</c> <b>Q1</b> a real question. It is derived rather than chosen — see
    /// <see cref="BlockPatterns.StripTiles"/>.
    /// </remarks>
    Perimeter = 2,
}

/// <summary>
/// The ground one Lot holds, and where its Address sits on the Street.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>A Lot is an Address and owns no ground (<c>adr/0078</c>), and this does not change that.</b>
/// The parcel is <b>derived</b>, on the epoch, from the block's saved pattern and the lattice — exactly
/// as frontage is derived from the Lot's saved position. ***No depth is authored anywhere***, which is
/// the refusal <c>adr/0078</c> made and which stands.
/// </para>
/// <para>
/// <b><see cref="East"/> and <see cref="North"/> are the parcel's south-west corner in absolute
/// Tiles</b>, and <see cref="Wide"/> and <see cref="Deep"/> are its extent from there — so the parcel
/// covers <c>[East, East + Wide) × [North, North + Deep)</c>. <b>Both are measured on the map's axes
/// and neither is relative to the face</b>: a west-face parcel is <em>Wide</em> in the direction it
/// runs back from its Street, because that direction is east.
/// </para>
/// </remarks>
/// <param name="Face">Which face of the block this parcel fronts.</param>
/// <param name="Side">Which side of that face's Segment it stands on.</param>
/// <param name="Offset">How far along the Segment the Address sits.</param>
/// <param name="East">The parcel's west edge, in absolute Tiles.</param>
/// <param name="North">The parcel's south edge, in absolute Tiles.</param>
/// <param name="Wide">Its extent eastward.</param>
/// <param name="Deep">Its extent northward.</param>
public readonly record struct Parcel(
    BlockFace Face,
    StreetSide Side,
    Tiles Offset,
    Tiles East,
    Tiles North,
    Tiles Wide,
    Tiles Deep)
{
    /// <summary>The ground this parcel holds, in Tiles.</summary>
    public int AreaTiles => Wide.Raw * Deep.Raw;

    /// <summary>Where the Address sits — on the Street, which is never inside the parcel.</summary>
    /// <remarks>
    /// <b>The same two expressions <see cref="LotSubdivider"/> already used</b>, moved here so that the
    /// position and the ground behind it are produced by one function and cannot disagree.
    /// </remarks>
    public (Tiles East, Tiles North) Address(int column, int row, int blockTiles) =>
        Face is BlockFace.South or BlockFace.North
            ? (new Tiles((column * blockTiles) + Offset.Raw),
               new Tiles((Face == BlockFace.South ? row : row + 1) * blockTiles))
            : (new Tiles((Face == BlockFace.West ? column : column + 1) * blockTiles),
               new Tiles((row * blockTiles) + Offset.Raw));
}

/// <summary>
/// <b>A pattern is a partition function.</b> Given a block, it says which of its faces carry Addresses
/// and how the ground behind them divides.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 3.</b> Everything here is a pure function of the lattice square, the
/// Ruleset's <c>block_tiles</c> and <c>lots_per_segment</c>, and the pattern — <b>no world state, no
/// randomness and no clock</b>. That is what lets the parcel be derived rather than saved, and it is
/// why re-deriving a block after a load produces the ground it had before.
/// </para>
/// <para>
/// 🔴 <b>ONE NUMBER GOVERNS ALL THREE PATTERNS AND IT IS DERIVED.</b> See
/// <see cref="StripTiles"/>. Everything else in this class is a consequence of what a pattern
/// <em>means</em>: <em>plots meet along the centre line</em> is a half-block, and <em>the losing pair
/// splits the middle band</em> is the rest.
/// </para>
/// </remarks>
public static class BlockPatterns
{
    /// <summary>How many parcels a block can yield, whatever its pattern.</summary>
    /// <remarks>
    /// <b>Four faces at <c>lots_per_segment</c> each</b> — the ceiling rather than the count, and what
    /// a caller sizes a buffer to. No pattern reaches it: the two sides of a Segment split its Lots by
    /// parity, so a block claims about half of each face.
    /// </remarks>
    public static int Ceiling(int lotsPerSegment) => 4 * (lotsPerSegment > 0 ? lotsPerSegment : 0);

    /// <summary>
    /// Whether this pattern claims to tile its block completely.
    /// </summary>
    /// <remarks>
    /// 🔴 ⚠ <b>THE EXHAUSTIVENESS TEST ASSERTS EACH PATTERN AGAINST ITS OWN CLAIM, NEVER AGAINST ONE
    /// RULE.</b> Overlap is a defect in all three; <b>leftover ground is a defect in two of them and
    /// the point of the third</b>. A single rule would have had to pick one of those and would have
    /// been wrong about the other two.
    /// </remarks>
    public static bool Exhaustive(BlockPattern pattern) => pattern != BlockPattern.Detached;

    /// <summary>
    /// <b>How deep a shallow strip is, in Tiles</b> — the one authored-looking number in this file,
    /// and it is not authored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0053</c> Q1, ANSWERED, and the answer was already in the code wearing another
    /// name.</b> <c>LotSubdivider.CornerTiles</c> reserved this much ground at each end of the
    /// north–south faces so that the corner belongs to one face, and <b>its own doc-comment said the
    /// reservation was standing in for a depth the class had none of</b>. ***It is the depth.*** The
    /// corner reservation and the strip are one quantity seen from two directions, so the formula
    /// lives here now and that method delegates to it — naming them separately is how they would have
    /// drifted.
    /// </para>
    /// <para>
    /// <b>Two terms, both derived.</b> <c>blockTiles ÷ lotsPerSegment</c> is one Lot's frontage, so a
    /// strip that deep makes a square-ish plot — <b>a plot is about as deep as it is wide</b>, which is
    /// the only shape claim in this file and the only one a real block supports without a survey.
    /// <c>blockTiles ÷ 4</c> caps it, so the middle band never closes: at that cap the two strips take
    /// half the block and the interior keeps the other half.
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing may write this as a Ruleset key.</b> <c>adr/0078</c> refused a depth key and the
    /// refusal stands — <b>what is refused is an AUTHORED depth</b>, and this is a consequence of two
    /// keys that already exist.
    /// </para>
    /// </remarks>
    public static int StripTiles(int blockTiles, int lotsPerSegment)
    {
        if (lotsPerSegment <= 0)
        {
            return 0;
        }

        int frontage = IntegerMath.FloorDiv(blockTiles, lotsPerSegment);
        int quarter = IntegerMath.FloorDiv(blockTiles, 4);

        return frontage < quarter ? frontage : quarter;
    }

    /// <summary>Whether a pattern lays Addresses on this face at all.</summary>
    /// <remarks>
    /// <b><see cref="BlockPattern.BackToBack"/> is the only one that refuses a face</b>, and it refuses
    /// the cross streets because a terrace shows them its gable end. ⚠ <b>The face still has a Street
    /// on it and still has a block on the other side</b>, which may lay its own Lots there under its
    /// own pattern — a Segment's two sides are subdivided independently and always were.
    /// </remarks>
    public static bool Carries(BlockPattern pattern, BlockFace face) =>
        pattern != BlockPattern.BackToBack || face is BlockFace.South or BlockFace.North;

    /// <summary>
    /// <b>How deep the ground behind a face runs, in Tiles.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three patterns, two depths, and every one of them is what the pattern MEANS.</b>
    /// <see cref="BlockPattern.Detached"/> takes a strip on every face.
    /// <see cref="BlockPattern.BackToBack"/>'s plots <em>meet along the centre line</em>, which is a
    /// half-block. <see cref="BlockPattern.Perimeter"/>'s losing pair <em>splits the middle band down
    /// the centre</em>, which is also a half-block, while its winning pair keeps the strip.
    /// </para>
    /// <para>
    /// ⚠ <b>The half is FLOORED and the far face takes the remainder</b>, so an odd block tiles
    /// exactly and the seam falls on one side. <b>Which side is arbitrary and is recorded as
    /// arbitrary</b> — the same standing as the east–west corner rule it sits beside. What is not
    /// arbitrary is that some rule must exist, because a Tile cannot belong to two parcels.
    /// </para>
    /// </remarks>
    public static int DepthTiles(BlockPattern pattern, BlockFace face, int blockTiles, int lotsPerSegment)
    {
        int half = IntegerMath.FloorDiv(blockTiles, 2);
        int far = blockTiles - half;

        return pattern switch
        {
            BlockPattern.Detached => StripTiles(blockTiles, lotsPerSegment),
            BlockPattern.BackToBack => face == BlockFace.South ? half : far,
            BlockPattern.Perimeter => face is BlockFace.South or BlockFace.North
                ? StripTiles(blockTiles, lotsPerSegment)
                : face == BlockFace.West ? half : far,
            _ => StripTiles(blockTiles, lotsPerSegment),
        };
    }

    /// <summary>Which side of a face's Segment the block behind it stands on.</summary>
    /// <remarks>
    /// <b>Four constants, and they need no geometry.</b> A horizontal Segment runs A→B eastward, so
    /// <see cref="StreetSide.Left"/> is its north side; a vertical one runs northward, so Left is its
    /// west side. A block therefore takes Left of its south face, Right of its north face, Right of
    /// its west face and Left of its east face.
    /// </remarks>
    public static StreetSide SideOf(BlockFace face) =>
        face is BlockFace.South or BlockFace.East ? StreetSide.Left : StreetSide.Right;

    /// <summary>
    /// <b>The whole partition of one block</b>, in the order its Lots are created.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the partition function <c>plans/0053</c> step 3 names.</b> It reads no world state:
    /// a lattice square, the Ruleset's two keys and a pattern, and it is deterministic and total.
    /// </para>
    /// <para>
    /// ⚠ <b>THE ORDER IS HASH-BEARING.</b> Faces in <see cref="BlockFace"/> order, and within a face
    /// ascending Segment index — which is exactly what <c>LotSubdivider</c> did before this existed,
    /// so <see cref="BlockPattern.Detached"/> reproduces the previous carve Lot for Lot.
    /// </para>
    /// <para>
    /// 🔴 <b>A face's ground divides evenly among the Addresses ON IT, and the count is a property of
    /// the Segment rather than of the block.</b> A Segment's Lots alternate sides by parity, so an
    /// odd <c>lots_per_segment</c> gives one side more than the other and a block's four faces carry
    /// different numbers. ***That asymmetry is old and visible*** — at the shipped 32 and 5 a block
    /// carries 3, 2, 2 and 1 — and the parcels inherit it, because the ground behind two Addresses
    /// divides in two.
    /// </para>
    /// <para>
    /// ⚠ <b>A carried face with NO Address leaves its ground unclaimed, and an exhaustive pattern is
    /// then not exhaustive on that block.</b> It happens at <c>lots_per_segment = 1</c>, where one
    /// parity takes every Lot on a Segment and the opposite side of every face gets none. ***This is
    /// a real limit and it is named rather than papered over*** — see <c>plans/0053</c> <b>Q5</b>,
    /// which asks whether <c>lots_per_segment</c> survives as a world number at all.
    /// </para>
    /// </remarks>
    /// <returns>How many parcels were written. Never more than <see cref="Ceiling"/>.</returns>
    public static int Carve(
        BlockPattern pattern, int column, int row, int blockTiles, int lotsPerSegment,
        Span<Parcel> into)
    {
        if (blockTiles <= 0 || lotsPerSegment <= 0)
        {
            return 0;
        }

        int written = 0;

        for (BlockFace face = BlockFace.South; face <= BlockFace.East; face++)
        {
            if (!Carries(pattern, face))
            {
                continue;
            }

            StreetSide side = SideOf(face);
            (int low, int high) = ReachTiles(pattern, face, blockTiles, lotsPerSegment);

            // How many Addresses this face carries, which is what its ground divides among. A first
            // pass rather than a running count: a parcel's width needs the total, and the total is
            // not known until every index has been tested.
            int count = 0;

            for (int index = 0; index < lotsPerSegment; index++)
            {
                if (Frontage.SideOf(index) == side
                    && Within(Frontage.OffsetOf(index, lotsPerSegment, blockTiles).Raw, low, high))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                continue;
            }

            int depth = DepthTiles(pattern, face, blockTiles, lotsPerSegment);
            int reach = high - low;
            int placed = 0;

            for (int index = 0; index < lotsPerSegment; index++)
            {
                Tiles offset = Frontage.OffsetOf(index, lotsPerSegment, blockTiles);

                if (Frontage.SideOf(index) != side || !Within(offset.Raw, low, high))
                {
                    continue;
                }

                // FloorDiv on both edges rather than a width times an index: the slices then abut
                // exactly and the last one absorbs the remainder, so a reach that does not divide by
                // the count still tiles.
                int from = low + IntegerMath.FloorDiv(placed * reach, count);
                int to = low + IntegerMath.FloorDiv((placed + 1) * reach, count);

                into[written++] = Rectangle(face, column, row, blockTiles, side, offset, from, to - from, depth);
                placed++;
            }
        }

        return written;
    }

    /// <summary>Whether an offset falls on the stretch of face a pattern lays Addresses along.</summary>
    /// <remarks>
    /// <b>Inclusive of both ends, matching the corner filter this replaced.</b> An offset exactly on
    /// the corner reservation's edge is the first Address past the corner rather than the last one
    /// inside it.
    /// </remarks>
    private static bool Within(int offset, int low, int high) => offset >= low && offset <= high;

    /// <summary>One parcel's absolute rectangle, from its face and its slice of that face.</summary>
    /// <remarks>
    /// <b>The face decides which axis the slice runs along and which the depth runs along</b>, and
    /// nothing else here differs between the four. A north or east face measures its depth back from
    /// the far edge, which is the only place a subtraction appears.
    /// </remarks>
    private static Parcel Rectangle(
        BlockFace face, int column, int row, int blockTiles, StreetSide side, Tiles offset,
        int from, int wide, int depth)
    {
        int baseEast = column * blockTiles;
        int baseNorth = row * blockTiles;

        return face switch
        {
            BlockFace.South => new Parcel(
                face, side, offset,
                new Tiles(baseEast + from), new Tiles(baseNorth),
                new Tiles(wide), new Tiles(depth)),

            BlockFace.North => new Parcel(
                face, side, offset,
                new Tiles(baseEast + from), new Tiles(baseNorth + blockTiles - depth),
                new Tiles(wide), new Tiles(depth)),

            BlockFace.West => new Parcel(
                face, side, offset,
                new Tiles(baseEast), new Tiles(baseNorth + from),
                new Tiles(depth), new Tiles(wide)),

            _ => new Parcel(
                face, side, offset,
                new Tiles(baseEast + blockTiles - depth), new Tiles(baseNorth + from),
                new Tiles(depth), new Tiles(wide)),
        };
    }

    /// <summary>
    /// <b>Which stretch of a face carries Addresses</b>, as an offset range along the Segment.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE CORNER BELONGS TO ONE PAIR OF FACES AND THE OTHER PAIR YIELDS.</b> A Lot is an
    /// Address and owns no ground, so nothing noticed while the ground was notional: both faces beside
    /// a junction laid a Lot and both were correct <em>as Addresses</em>. What has no answer is which
    /// of them the LAND belongs to. East–west keeps; north–south begins after the corner, which is
    /// what a real corner does.
    /// </remarks>
    public static (int Low, int High) ReachTiles(
        BlockPattern pattern, BlockFace face, int blockTiles, int lotsPerSegment)
    {
        if (face is BlockFace.South or BlockFace.North)
        {
            return (0, blockTiles);
        }

        int corner = StripTiles(blockTiles, lotsPerSegment);

        return (corner, blockTiles - corner);
    }
}
