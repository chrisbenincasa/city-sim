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
/// <b>How a block is subdivided</b> — one of six real subdivisions of a real city block.
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
/// ⚠ <b>THE SET IS OPEN AND THE NUMBER HAS ALREADY MOVED — IT OPENED AT THREE AND
/// <see cref="BlockPatterns.Count"/> IS SIX.</b> ⚠ <b>Two sentences here went on saying <em>three</em>
/// for as long as it took to add <see cref="Courtyard"/>, <see cref="Slab"/> and
/// <see cref="Tower"/></b>, which is what a count written in prose beside a count written in code
/// does. ***A number stated in two places is a number that will disagree with itself***, so read
/// <see cref="BlockPatterns.Count"/> and never this sentence. What is decided is not the number but that
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
    /// 🔴 <b>NOT EXHAUSTIVE, DELIBERATELY, and it is one of two that are not.</b> The other is
    /// <see cref="Courtyard"/>, whose hole is a courtyard rather than scrub. The
    /// leftover ground is <em>correct</em> here: back gardens do not meet, and there is scrub between
    /// them. ⚠ <b>This is what the subdivider did before patterns existed</b>, so it is the default
    /// and a world that never chooses a pattern is the world that was there yesterday.
    /// </remarks>
    Detached = 0,

    /// <summary>
    /// <b>Plots on all four faces and no ground left over.</b> A perimeter block — unbroken frontage
    /// the whole way round, and the middle <em>taken</em> rather than kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS COMMENT WAS <see cref="BackToBack"/>'s, PASTED VERBATIM, AND IT DESCRIBED THE
    /// OPPOSITE GEOMETRY.</b> It read <em>"one pair of streets takes the whole block"</em> and
    /// <em>"the cross streets … carry no Address at all"</em> — while
    /// <see cref="BlockPatterns.Carries"/> says in its own remark that <see cref="BackToBack"/> is
    /// <b>the only</b> pattern that refuses a face, and this one refuses none: it falls to that
    /// method's <c>_ => true</c>. ***Two members sharing one description are two members nobody can
    /// tell apart, and the wrong one is the one nobody reads twice.*** Found three times
    /// independently before anyone opened the switch it contradicts.
    /// </para>
    /// <para>
    /// <b>Four faces, two depths, and the deep pair is what closes the block.</b>
    /// <see cref="BlockPatterns.DepthTiles"/>: south and north take
    /// <see cref="BlockPatterns.StripTiles"/>; west and east take a <b>half-block each</b> and meet
    /// along the centre line, consuming the middle band the strips left behind. ⚠ <b>Which pair goes
    /// deep is arbitrary in the same way the seam's side is</b>, and is recorded here rather than
    /// argued — what is not arbitrary is that one pair must, because two shallow strips leave a hole
    /// and this form has none.
    /// </para>
    /// <para>
    /// ✅ <b>Exhaustive</b>, and the contrast that carries the form is with <see cref="Courtyard"/>
    /// rather than with the terrace: both build on all four faces, and this one <em>fills</em> the
    /// middle where that one <em>keeps</em> it. ⚠ <b>They are not a coarse and a fine version of one
    /// shape</b> — a courtyard is a room, and this block has none.
    /// </para>
    /// </remarks>
    Perimeter = 1,

    /// <summary>
    /// <b>One pair of streets takes the whole block; plots meet along the centre line.</b> A British
    /// terrace, a Portland 200 ft block, Manhattan.
    /// </summary>
    /// <remarks>
    /// ✅ <b>Exhaustive.</b> The cross streets get <b>gable ends</b> — the terrace's end wall faces
    /// them — so they carry no Address at all, which is the one place in this set where a face with a
    /// Street on it yields nothing.
    /// </remarks>
    BackToBack = 2,

    /// <summary>
    /// <b>Four buildings round a hole.</b> One parcel a face, a third of the block deep, and the
    /// middle third left open. A Vienna Hof, a Berlin Blockrand, a London mansion block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE FIRST PATTERN THAT IS NOT A ROW OF PLOTS.</b> Every rung below it divides a face
    /// among the Addresses on that face; this one gives the <b>whole face to one Address</b>. See
    /// <see cref="BlockPatterns.ParcelsPerFace"/> — until it existed the carve laid
    /// <c>lots_per_segment</c> parcels a face whatever the pattern, so ***every pattern produced the
    /// same number of buildings and only their depth differed.***
    /// </para>
    /// <para>
    /// ⚠ <b>NOT exhaustive, and for the opposite reason to <see cref="Detached"/>.</b> Detached's
    /// leftover is scrub between back fences; this one's is a <b>courtyard</b>, which is the point of
    /// the form. Both are holes and neither is a defect, which is why the test asks each pattern
    /// against its own claim.
    /// </para>
    /// </remarks>
    Courtyard = 3,

    /// <summary>
    /// <b>The block is two buildings.</b> One pair of streets, one parcel each, meeting along the
    /// centre line. A tower slab, a superblock, a mill.
    /// </summary>
    /// <remarks>
    /// ✅ <b>Exhaustive, and the coarsest partition the set can express</b> — half a block behind one
    /// Address. It is <see cref="BackToBack"/>'s faces and depths with
    /// <see cref="BlockPatterns.ParcelsPerFace"/> of one, which is what says the coarsening is a
    /// property of its own and not a fifth geometry.
    /// </remarks>
    Slab = 4,

    /// <summary>
    /// <b>One building on a fraction of the block, and the rest of the ground left open.</b> A tower
    /// in a plaza — Lever House, a Barbican point block, half of Hong Kong.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE FIRST FORM IN THIS SET THAT HOUSES PEOPLE BY GOING UP.</b> Every one above it takes
    /// more of the block to hold more people; this one takes <em>less</em> and stands taller for it,
    /// which is only expressible at all because <c>plans/0058</c> made a rung name a plot ratio.
    /// ***Under the old reading — a rung being a storey count — this form was two storeys on a small
    /// parcel, which is a shed.***
    /// </para>
    /// <para>
    /// ⚠ <b>NOT exhaustive, and it is the least exhaustive thing here</b> — half the block each way
    /// is a <b>quarter</b> of its ground, so three quarters is left open. That is the form and not a
    /// defect: the open ground <em>is</em> the plaza, in the same way <see cref="Courtyard"/>'s hole
    /// is the courtyard and <see cref="Detached"/>'s is scrub. ⚠ <b>The fraction is priced by the
    /// top of the ladder rather than chosen for its looks</b> — see
    /// <see cref="BlockPatterns.DepthTiles"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>One face, one Address, and the address is on the kerb.</b> A tower with its own street
    /// door is the ordinary arrangement; nothing here models a forecourt, and the setback machinery
    /// that would draw one is per-parcel rather than per-form.
    /// </para>
    /// </remarks>
    Tower = 5,
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
    public static bool Exhaustive(BlockPattern pattern) =>
        pattern is BlockPattern.BackToBack or BlockPattern.Perimeter or BlockPattern.Slab;

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
    /// <b>Two terms, both derived.</b> The first is one plot's frontage, so a strip that deep makes a
    /// square-ish plot — <b>a plot is about as deep as it is wide</b>, which is the only shape claim
    /// in this file and the only one a real block supports without a survey. <c>blockTiles ÷ 4</c>
    /// caps it, so the middle band never closes: at that cap the two strips take half the block and
    /// the interior keeps the other half.
    /// </para>
    /// <para>
    /// 🔴 <b>THE FIRST TERM WAS <c>blockTiles ÷ lotsPerSegment</c> AND THAT IS THE ADDRESS SPACING,
    /// NOT A PLOT'S FRONTAGE.</b> A Segment's Addresses split between the two blocks that share it by
    /// parity (<see cref="Frontage.SideOf"/>), so a face carries about <em>half</em> of them and each
    /// parcel is about <b>twice</b> the spacing wide. ***The file's one shape claim was false in the
    /// shipped city by a factor of 1.8***: at <c>block_tiles = 32</c> and
    /// <c>lots_per_segment = 5</c> the strip came out 6 Tiles deep behind parcels measured 10 and 11
    /// Tiles wide — read off the draw list, 40 × 24 m and 44 × 24 m. The term is
    /// <c>2 × blockTiles ÷ lotsPerSegment</c> now, and at the shipped lattice ***the quarter cap is
    /// what binds***, so the depth is 8. <c>plans/0045</c> row 24.
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing may write this as a Ruleset key.</b> <c>adr/0078</c> refused a depth key and the
    /// refusal stands — <b>what is refused is an AUTHORED depth</b>, and this is a consequence of two
    /// keys that already exist.
    /// </para>
    /// </remarks>
    public static int StripTiles(int blockTiles, int lotsPerSegment) =>
        StripTiles(blockTiles, blockTiles, lotsPerSegment);

    /// <inheritdoc cref="StripTiles(int, int)"/>
    /// <remarks>
    /// 🔴 <b>THE TWO TERMS MEASURE DIFFERENT AXES AND THE ONE-ARGUMENT FORM CANNOT SAY SO.</b> The
    /// frontage term is twice the Address spacing <b>along</b> the face; the quarter cap is a
    /// fraction of the block <b>across</b> it. They were one number while every block was square,
    /// which is exactly <c>plans/0045</c> row 25's assumption — ***a derivation that reads two
    /// extents off one variable is not wrong about the number and is wrong about the quantity.***
    /// </remarks>
    /// <param name="alongTiles">How far the face runs, which sets a plot's frontage.</param>
    /// <param name="acrossTiles">How deep the block is behind it, which the cap is a quarter of.</param>
    /// <param name="lotsPerSegment">Addresses a Segment holds.</param>
    public static int StripTiles(int alongTiles, int acrossTiles, int lotsPerSegment)
    {
        if (lotsPerSegment <= 0)
        {
            return 0;
        }

        // TWICE the Address spacing, because a face carries every other Address and the parcel
        // behind one of them spans the gap its neighbour on the far side left.
        int frontage = IntegerMath.FloorDiv(2 * alongTiles, lotsPerSegment);
        int quarter = IntegerMath.FloorDiv(acrossTiles, 4);

        return frontage < quarter ? frontage : quarter;
    }

    /// <summary>
    /// <b>How many parcels one carried face is divided into</b>, or <c>0</c> for one per Address.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE THING THE PATTERN SET COULD NOT SAY, AND THE REASON EVERY BLOCK LOOKED THE SAME
    /// SIZE.</b> <see cref="Carve"/> used to walk <c>index &lt; lotsPerSegment</c> on every face of
    /// every pattern, so a pattern could vary <em>which faces carry</em> and <em>how deep they
    /// reach</em> and never <em>how many</em>. ***At the shipped 32 and 5 that made every block in
    /// every pattern carry the same eight parcels***, and a block holding two large buildings was not
    /// expressible — not because nobody wrote it down, but because the count was pinned to
    /// <c>lots_per_segment</c>, which is one number for the whole world.
    /// </para>
    /// <para>
    /// ⚠ <b>THE ADDRESSES DO NOT MOVE; FEWER OF THEM ARE KEPT.</b> A parcel still carries a real
    /// Address at a real <c>Frontage</c> offset, so <c>adr/0074</c>'s side-of-street and the door on
    /// the drawing are untouched. What a coarse pattern does is give one Address the ground that
    /// several would have divided — which is <c>adr/0025</c>'s <b>Amalgamation</b> route, arriving as
    /// geometry rather than as a verb.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a CEILING and not a promise.</b> A face carrying fewer Addresses than this asks for
    /// gets one parcel each; the coarsening can only ever join, never split.
    /// </para>
    /// </remarks>
    public static int ParcelsPerFace(BlockPattern pattern) =>
        pattern is BlockPattern.Courtyard or BlockPattern.Slab or BlockPattern.Tower ? 1 : 0;

    /// <summary>Whether a pattern lays Addresses on this face at all.</summary>
    /// <remarks>
    /// <b><see cref="BlockPattern.BackToBack"/> is the only one that refuses a face</b>, and it refuses
    /// the cross streets because a terrace shows them its gable end. ⚠ <b>The face still has a Street
    /// on it and still has a block on the other side</b>, which may lay its own Lots there under its
    /// own pattern — a Segment's two sides are subdivided independently and always were.
    /// </remarks>
    public static bool Carries(BlockPattern pattern, BlockFace face) =>
        pattern switch
        {
            // ⚠ ONE face and not two, which is what makes it one Building rather than two. The pair
            // below take the north face as well and meet along the centre line; a tower has no
            // second half to meet.
            BlockPattern.Tower => face is BlockFace.South,
            BlockPattern.BackToBack or BlockPattern.Slab => face is BlockFace.South or BlockFace.North,
            _ => true,
        };

    /// <summary>
    /// <b>How deep the ground behind a face runs, in Tiles.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every depth here is what its pattern MEANS, and none of them is a chosen length.</b>
    /// ⚠ <b>This paragraph said <em>three patterns, two depths</em> and the switch below has six
    /// arms</b> — the three fractions were added underneath it with their arguments in inline
    /// comments, and the summary was never re-read against them. The three named next are the three
    /// that share <see cref="StripTiles"/> and the half-block; <see cref="BlockPattern.Courtyard"/>,
    /// <see cref="BlockPattern.Tower"/> and <see cref="BlockPattern.Slab"/> carry their own.
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
    public static int DepthTiles(
        BlockPattern pattern, BlockFace face, int blockTiles, int lotsPerSegment) =>
        DepthTiles(pattern, face, blockTiles, blockTiles, lotsPerSegment);

    /// <inheritdoc cref="DepthTiles(BlockPattern, BlockFace, int, int)"/>
    /// <remarks>
    /// ⚠ <b>Every fraction here is of the ACROSS extent</b> — a half, a third, a quarter of the
    /// ground the depth runs into — and only the strip's frontage term reads the along one.
    /// <c>plans/0045</c> row 25.
    /// </remarks>
    public static int DepthTiles(
        BlockPattern pattern, BlockFace face, int alongTiles, int acrossTiles, int lotsPerSegment)
    {
        int half = IntegerMath.FloorDiv(acrossTiles, 2);
        int far = acrossTiles - half;

        return pattern switch
        {
            BlockPattern.Detached => StripTiles(alongTiles, acrossTiles, lotsPerSegment),
            BlockPattern.BackToBack or BlockPattern.Slab => face == BlockFace.South ? half : far,
            BlockPattern.Perimeter => face is BlockFace.South or BlockFace.North
                ? StripTiles(alongTiles, acrossTiles, lotsPerSegment)
                : face == BlockFace.West ? half : far,

            // A THIRD, so the hole is a third of the block across and the frame is a third each
            // side. It is the same class of statement as StripTiles' quarter cap -- a fraction of
            // what the player drew, not a length -- and it is what makes the form a COURTYARD block
            // rather than a deep-plan one: at a half the frame closes and the hole is gone.
            BlockPattern.Courtyard => IntegerMath.FloorDiv(acrossTiles, 3),

            // A HALF EACH WAY, WHICH IS A QUARTER OF THE BLOCK'S GROUND -- the same class of
            // statement as Courtyard's third and StripTiles' quarter cap, a fraction of what the
            // player drew rather than a length. ⚠ IT IS THE LARGEST FRACTION THAT STILL LEAVES THE
            // MAJORITY OF THE BLOCK OPEN, which is what makes the form a tower rather than a slab,
            // and the ceiling on it is arithmetic rather than taste: a rung names a plot ratio, so
            // a form on a NINTH of its block (a third each way) has to stand nine times the ratio
            // -- 174 storeys at the top rung of `storeys_per_rung = 3`, which is 609 m and absurd.
            // ***The footprint of the tallest form is what prices the top of the ladder***, and a
            // third was measured before a half was chosen. plans/0059.
            BlockPattern.Tower => IntegerMath.FloorDiv(acrossTiles, 2),
            _ => StripTiles(alongTiles, acrossTiles, lotsPerSegment),
        };
    }

    /// <summary>How much of a block one pattern claims, in Tiles.</summary>
    /// <remarks>
    /// <b>Carved rather than derived from a formula, because the formula would be the partition
    /// written twice.</b> It has to be what the carve actually produces and not what the geometry says
    /// it should. ⚠ <b>It is HALF of <see cref="Ladder"/>'s quantity and was once the whole of it</b> —
    /// the ratchet compared claimed ground until a pattern with a hole in it claimed less than the
    /// pattern it was denser than.
    /// </remarks>
    public static int ClaimedTiles(BlockPattern pattern, int blockTiles, int lotsPerSegment)
    {
        int ceiling = Ceiling(lotsPerSegment);

        if (ceiling <= 0)
        {
            return 0;
        }

        Span<Parcel> parcels = ceiling <= 64 ? stackalloc Parcel[64] : new Parcel[ceiling];

        int count = Carve(default, pattern, blockTiles, lotsPerSegment, parcels);
        int claimed = 0;

        for (int i = 0; i < count; i++)
        {
            claimed += parcels[i].AreaTiles;
        }

        return claimed;
    }

    /// <summary>How many Addresses one pattern lays on a block.</summary>
    /// <remarks>
    /// <b>The other half of <see cref="Ladder"/>'s quantity, and the half that carries the coarse
    /// patterns.</b> Two patterns that both tile their block claim the same ground, and what separates
    /// them is how finely they divide it — which is the whole difference between a terrace and a slab.
    /// </remarks>
    public static int AddressCount(BlockPattern pattern, int blockTiles, int lotsPerSegment)
    {
        int ceiling = Ceiling(lotsPerSegment);

        if (ceiling <= 0)
        {
            return 0;
        }

        Span<Parcel> parcels = ceiling <= 64 ? stackalloc Parcel[64] : new Parcel[ceiling];

        return Carve(default, pattern, blockTiles, lotsPerSegment, parcels);
    }

    /// <summary>
    /// <b>Which pattern a density band gets</b> — <c>plans/0053</c> step 4's selection, and the whole
    /// of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>POSITIONAL, and that is what keeps it free of an authored number.</b> Bands are declared
    /// least intense first and <see cref="Ladder"/> puts the patterns in the same order, so the
    /// mapping is two ordinals against each other. A Ruleset declaring one band gets the top rung;
    /// two get both ends; five get every rung; none gets <see cref="BlockPattern.Detached"/>
    /// everywhere, which is the city that was there yesterday.
    /// </para>
    /// <para>
    /// 🔴 <b>BOTH ENDS OF THE LADDER ARE REACHABLE AT EVERY BAND COUNT, and that is a fix rather than
    /// a restatement.</b> The scaling was <c>(band-1) × Count / bandCount</c>, which lands the top
    /// band one rung short of the top — at the shipped four bands of <c>banded.toml</c> it reached
    /// rungs 0, 1, 2 and 3 of five and ***the coarsest pattern in the set was unreachable by every
    /// Ruleset that could be written***. It divides by <c>bandCount - 1</c> now, so the last band is
    /// the last rung by construction. ⚠ <b>A single declared band takes the TOP</b>: band <c>0</c>
    /// already holds the bottom, so a Ruleset naming exactly one band is naming the thing that is not
    /// the default, and giving it rung 0 would make the declaration inert.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>WHAT THIS DOES NOT READ IS THE POINT OF <c>plans/0053</c> <b>Q2</b>.</b> That plan wants
    /// selection to see <em>"the land value and Building density already standing around it"</em>,
    /// which is what would break a band's ring into something continuous. ***Every candidate rule for
    /// reading them needs cut points nobody has derived***, and a cut point invented here would be a
    /// hash-bearing number with no ratifier. <b>So this reads the band and stops</b>, and the city it
    /// makes is banded rather than varied. That is an improvement on one pattern everywhere and it is
    /// not the end state.
    /// </para>
    /// </remarks>
    public static BlockPattern ForBand(
        byte band,
        int bandCount,
        int blockTiles,
        int lotsPerSegment,
        Determinism.WorldKey key,
        int column,
        int row,
        int spread)
    {
        if (band == 0 || bandCount <= 0)
        {
            return BlockPattern.Detached;
        }

        // Bands are one-based, and both ladders are indexed from their TOP end down rather than from
        // their bottom up -- which is what makes the last band the last rung whatever the two lengths
        // are. A single declared band divides by nothing and takes the top; see the remarks.
        int rung = bandCount <= 1
            ? Count - 1
            : IntegerMath.FloorDiv((band - 1) * (Count - 1), bandCount - 1);

        rung += Scatter(key, column, row, spread);

        Span<BlockPattern> ladder = stackalloc BlockPattern[Count];

        Ladder(blockTiles, lotsPerSegment, ladder);

        return ladder[rung < 0 ? 0 : rung >= Count ? Count - 1 : rung];
    }

    /// <summary>
    /// <b>How far off its band's rung one block's form is drawn</b>, symmetrically, in rungs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS WHAT MAKES A DENSITY A DISTRIBUTION RATHER THAN A VALUE.</b> Without it
    /// <see cref="ForBand"/> is a total order indexed by a band, so one band gets one form for ever
    /// and a whole ring of the city is the same shape — which is exactly what the drawing showed
    /// (<c>plans/0057</c> <b>F1</b>: the boundaries fall on streets, because they are the only thing
    /// there is). ***A city is not one form per density; it is a mix around one.***
    /// </para>
    /// <para>
    /// ⚠ <b>SYMMETRIC, AND IT COSTS THE THING A ONE-SIDED DRAW WOULD KEEP.</b> Drawing only upward
    /// would never make a dense band sparser, which sounds safer and quietly biases every world
    /// denser than its Ruleset says. A symmetric draw leaves the band's rung as the <em>centre</em>
    /// of what the band means, which is what a band was always claiming to be. ⚠ <b>The clamp at each
    /// end is not symmetric</b> and cannot be: a band at rung 0 has nothing below it, so the bottom
    /// band skews up and the top band skews down. That is a property of a bounded ladder and not of
    /// this draw.
    /// </para>
    /// <para>
    /// 🔴 <b>IT DRAWS ON THE BLOCK AND NEVER ON THE BAND, AND THE RE-CARVE RATCHET IS WHY.</b>
    /// <c>LotSubdivider.RecarveBlock</c> refuses a re-plat that would move a block <em>down</em> the
    /// ladder, and it can only refuse what it can compare — so selection has to be monotone in the
    /// band for one piece of ground. A fixed offset added to a rung that rises with the band still
    /// rises with the band, and a clamp is monotone too. ***A draw that saw the band would make
    /// upzoning a block and watching it get sparser writeable***, which is the failure the ratchet's
    /// whole remark is about.
    /// </para>
    /// <para>
    /// ⚠ <b>Absent means 0 and 0 is the old behaviour exactly</b> — no draw is taken, so a world that
    /// does not state <c>[lots] pattern_spread</c> selects the form it selected before this existed.
    /// The key is opted into rather than defaulted under everybody, which is the shape
    /// <c>[lots] storeys_per_rung</c> established (<c>plans/0056</c>).
    /// </para>
    /// </remarks>
    private static int Scatter(Determinism.WorldKey key, int column, int row, int spread)
    {
        if (spread <= 0)
        {
            return 0;
        }

        // The ground's own coordinates as the entity id, which is PurposeTag.BlockPattern's whole
        // remark: a block cleared and zoned again is re-carved the same way, and a recycled row
        // cannot re-plat land nobody touched.
        ulong patch = ((ulong)(uint)column << 32) | (uint)row;
        ulong draw = Determinism.Randomness.Draw(
            key, patch, Quantities.Ticks.Zero, Determinism.PurposeTag.BlockPattern);

        // A different bit range from the one LotSubdivider's storey jitter reads, because the two
        // draws carry different tags and must not be correlated by sharing a window as well.
        return (int)(((draw >> 16) & 0xFFFF) % (ulong)((2 * spread) + 1)) - spread;
    }

    /// <summary>
    /// <b>The pattern ladder for one world's lattice</b>, least intense first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE LADDER IS A FUNCTION OF THE TWO RULESET KEYS AND IT IS NOT THE ENUM ORDER.</b> It was
    /// the enum order, derived by sorting on ground claimed and asserted to hold at every block size,
    /// and ***that held by luck across three patterns and broke the day the set grew to five***. Two
    /// counter-examples, both measured rather than argued: <see cref="BlockPattern.Courtyard"/> claims
    /// <b>880</b> Tiles of a 32-Tile block against <see cref="BlockPattern.BackToBack"/>'s
    /// <b>1,024</b>, because its middle third is a courtyard — so claimed ground ranks the denser form
    /// lower. And <b>21 of the 73 reachable lattices invert</b> under ground-per-Address too: at
    /// <c>lots_per_segment = 4</c> a terrace and a courtyard block carry four Addresses each, so the
    /// comparison collapses back onto area and the courtyard sorts below.
    /// </para>
    /// <para>
    /// <b>The quantity is FLOOR AREA PER ADDRESS — how many people stand behind one door — and it
    /// reduces to the Address count.</b> Since <c>plans/0058</c> a rung names a plot ratio, so the
    /// floor area a pattern puts on a block is <c>ratio × blockTiles²</c> and the pattern cancels
    /// out: ***every form at one rung houses the same number of people.*** Divide that by the doors
    /// and the ratio cancels too, leaving <c>1 ÷ addresses</c>. **Fewer doors is denser**, and there
    /// is no cut point and nothing chosen. <see cref="ClaimedTiles"/> survives only as the tie-break.
    /// </para>
    /// <para>
    /// 🔴 <b>IT WAS GROUND PER ADDRESS AND THAT WAS A PROXY THAT BROKE ON THE SIXTH FORM.</b> Land
    /// behind a door stood in for people behind a door, which holds for five forms that all house
    /// people by going <em>back</em> — and <see cref="BlockPattern.Tower"/> houses them <em>up</em>.
    /// A tower claims a quarter of its block behind one door, so the proxy ranked it **between a
    /// suburb and a terrace** while the thing it was standing in for ranked it top.
    /// ***A proxy is only visible as one when something arrives that it is wrong about***, and
    /// <c>plans/0058</c> is what made the real quantity computable at all. <c>plans/0059</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>The five keep the order they had</b> at the shipped lattice — 8, 8, 5, 4 and 2 doors —
    /// so this replaced the quantity without moving the ladder it produced. ⚠ <b>And it is still not
    /// the enum order</b>: 28 of the 84 reachable lattices depart from it, so the ladder is still
    /// computed for the lattice in hand rather than fixed once. <c>block_tiles</c> and
    /// <c>lots_per_segment</c> are world-creation keys, so within a world this is a constant and the
    /// ratchet in <c>LotSubdivider.RecarveBlock</c> still compares two positions on one ladder.
    /// </para>
    /// <para>
    /// ⚠ <b>Ties fall back to the enum order, and the sort is stable so that they do.</b> On a lattice
    /// where two patterns are genuinely indistinguishable — <c>block_tiles = 4</c> puts
    /// <see cref="BlockPattern.BackToBack"/> and <see cref="BlockPattern.Slab"/> at 8 Tiles a door
    /// each — an unstable sort would order them by whatever the comparison happened to visit first.
    /// ***A tie is not a reason to invent a difference***, and the declaration order is the one
    /// arbitrary thing already in the file.
    /// </para>
    /// </remarks>
    public static void Ladder(int blockTiles, int lotsPerSegment, Span<BlockPattern> into)
    {
        Span<int> claimed = stackalloc int[Count];
        Span<int> addresses = stackalloc int[Count];

        for (int i = 0; i < Count; i++)
        {
            into[i] = (BlockPattern)i;
            claimed[i] = ClaimedTiles((BlockPattern)i, blockTiles, lotsPerSegment);
            addresses[i] = AddressCount((BlockPattern)i, blockTiles, lotsPerSegment);
        }

        // Insertion sort over five elements, and stable because the comparison is strict -- an equal
        // pair never swaps, so the enum order survives a tie.
        for (int i = 1; i < Count; i++)
        {
            for (int j = i; j > 0 && Sparser(into[j], into[j - 1], claimed, addresses); j--)
            {
                (into[j], into[j - 1]) = (into[j - 1], into[j]);
            }
        }
    }

    /// <summary>
    /// <b>How many storeys a Building on a block of this pattern stands</b>, before the per-parcel
    /// draw.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE RUNG NAMES A PLOT RATIO AND NOT A STOREY COUNT, AND THE STOREYS FALL OUT OF THE
    /// GROUND THE FORM DECLINED TO TAKE.</b> <c>ratio = rung × step + 2</c> is floor area per unit of
    /// <em>block</em>, and a pattern claiming <c>claimed</c> of a <c>blockTiles²</c> block needs
    /// <c>ratio × block ÷ claimed</c> storeys to deliver it. ***So a form that spreads out is short
    /// and a form that stands back is tall, at the same density***, which is the sentence this
    /// function exists to make true.
    /// </para>
    /// <para>
    /// 🔴 <b>IT RETURNED <c>rung × step + 2</c> AS A STOREY COUNT AND THAT IS WHY THE CITY COULD ONLY
    /// BUILD OUT.</b> A storey count applied to whatever footprint the pattern happened to carve made
    /// height and footprint the same decision, and the ladder's own quantity — ground behind one front
    /// door — is a <em>plan-view</em> measure that cannot see a form housing people upward at all.
    /// ***A tower claims little ground, so under the old reading it sorted below a bungalow and came
    /// out two storeys tall.*** <c>plans/0057</c> found it; <c>plans/0058</c> is this.
    /// </para>
    /// <para>
    /// ⚠ <b>The floor of two stopped being a clamp and became a consequence</b>, and the argument it
    /// carried is unchanged: <em>a building with no upper floor is a shed</em>. The ratio's floor is
    /// 2 and no form claims more than its whole block, so two is what a form covering everything
    /// gets and everything else is taller. See <see cref="Floor"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>What moves on a world that states nothing.</b> At <c>storeys_per_rung = 1</c> and the
    /// shipped lattice the ladder was 2, 3, 4, 5, 6 and is now 3, 3, 4, 5, 6 — one storey, on the
    /// sparsest rung, because <see cref="BlockPattern.Detached"/> claims 624 Tiles of a 1,024-Tile
    /// block and a plot ratio of 2 on 61% of the ground is three floors. ***Every hash in the project
    /// moves and no Ruleset key had to change for it to.***
    /// </para>
    /// <para>
    /// 🔴 <b>THE STEP WAS 1 AND NOBODY HAD CHOSEN IT, WHICH IS DIFFERENT FROM ITS BEING DERIVED.</b>
    /// The floor of two carries an argument and <see cref="Rung"/> carries a derivation; the
    /// <em>distance between rungs</em> carried neither, and its consequence was a ceiling nothing in
    /// the corpus states: <c><see cref="Count"/> - 1 + 2</c>, plus a jitter of one, is <b>seven
    /// storeys — 24.5 m — and that was the tallest Building this design could produce on any world
    /// at any population.</b> ***The height of the tallest thing in the city was set by how many ways
    /// there are to subdivide a block***, which is a geometry decision that was never about height.
    /// <c>plans/0056</c> made it <c>[lots] storeys_per_rung</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>Absent means 1, and 1 is what every file that does not state it gets</b> — so this is a
    /// key a world opts into rather than a number that moved under everybody. ⚠ <b>The FLOOR is
    /// still two and does not scale</b>: a shed is a shed whatever the step is, and multiplying the
    /// floor would raise the suburb rather than lengthen the ladder.
    /// </para>
    /// <para>
    /// ⚠ <b>It follows the ladder and therefore follows the lattice.</b> The rungs reorder at
    /// <c>lots_per_segment = 4</c> — see <see cref="Ladder"/> — so on such a world the courtyard
    /// block is the shorter of the two. That is the ladder being honest rather than this being
    /// wrong: whichever form gives one door less ground is the denser one.
    /// </para>
    /// <para>
    /// ⚠ <b>THE LADDER'S QUANTITY IS UNCHANGED AND WAS NOT THE DEFECT.</b> Ground behind one door is
    /// a fine ordering of the five forms that exist, all of which house people by going back. What
    /// was wrong was reading its <em>index</em> as a height. ***A sixth form that houses people
    /// upward would still sort wrongly here***, and that is <c>plans/0058</c>'s open question rather
    /// than something this change fixes.
    /// </para>
    /// </remarks>
    public static int Storeys(
        BlockPattern pattern, int blockTiles, int lotsPerSegment, int storeysPerRung)
    {
        int step = storeysPerRung < 1 ? 1 : storeysPerRung;
        int ratio = (Rung(pattern, blockTiles, lotsPerSegment) * step) + 2;
        int claimed = ClaimedTiles(pattern, blockTiles, lotsPerSegment);

        if (claimed <= 0 || blockTiles <= 0)
        {
            return Floor;
        }

        // long, because a large block times a large ratio leaves 32 bits. Nothing here is a
        // Q16.16 quantity -- it is a count of storeys -- so the width is the only question.
        long storeys = IntegerMath.FloorDiv(
            (long)ratio * blockTiles * blockTiles, claimed);

        return storeys < Floor ? Floor : storeys > byte.MaxValue ? byte.MaxValue : (int)storeys;
    }

    /// <summary>The shortest Building any pattern can produce. <b>A floor and not a default.</b></summary>
    /// <remarks>
    /// ⚠ <b>It is now unreachable from above rather than clamped to.</b> The plot ratio's own floor
    /// is 2 and no pattern claims more than its whole block, so <c>ratio × block ÷ claimed</c> is at
    /// least 2 for every form that claims anything. ***The clamp survives for the degenerate lattice
    /// and not for the design***, which is the difference between a floor that is argued and a floor
    /// that is enforced.
    /// </remarks>
    private const int Floor = 2;

    /// <summary>Where one pattern sits on this lattice's <see cref="Ladder"/>.</summary>
    /// <remarks>
    /// 🔴 <b>THIS IS WHAT THE RE-CARVE RATCHET COMPARES</b>, and what <see cref="ForBand"/> indexes
    /// into. They read one function, so a selection that could go <em>down</em> as the band goes up is
    /// not merely tested against — it is unwriteable.
    /// </remarks>
    public static int Rung(BlockPattern pattern, int blockTiles, int lotsPerSegment)
    {
        Span<BlockPattern> ladder = stackalloc BlockPattern[Count];

        Ladder(blockTiles, lotsPerSegment, ladder);

        for (int i = 0; i < Count; i++)
        {
            if (ladder[i] == pattern)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>Whether <paramref name="left"/> puts FEWER people behind one door than <paramref name="right"/>.</summary>
    /// <remarks>
    /// <b>An integer comparison and no longer a cross-multiplication</b>, because the quantity
    /// reduced: floor area per Address is <c>ratio × blockTiles² ÷ addresses</c> and every term but
    /// the last is the same for both sides. ***The exactness the cross-multiplication was buying is
    /// now free.*** <see cref="Ladder"/> carries the derivation.
    /// ⚠ <b>Ground survives as the tie-break and only as that</b> — two forms with the same number of
    /// doors hold the same number of people, so the one on less ground is the more urban of the two,
    /// and a tie is broken rather than invented.
    /// ⚠ <b>A pattern laying NO Address sorts to the top</b>, where a band of 1 will not select it,
    /// rather than to the bottom where it would be every dense band's answer.
    /// </remarks>
    private static bool Sparser(
        BlockPattern left, BlockPattern right, ReadOnlySpan<int> claimed, ReadOnlySpan<int> addresses)
    {
        int leftAddresses = addresses[(int)left];
        int rightAddresses = addresses[(int)right];

        if (leftAddresses == 0 || rightAddresses == 0)
        {
            return rightAddresses == 0 && leftAddresses != 0;
        }

        // 🔴 MORE DOORS IS SPARSER, and the ground is now only the tie-break. See the remarks.
        return leftAddresses != rightAddresses
            ? leftAddresses > rightAddresses
            : claimed[(int)left] < claimed[(int)right];
    }

    /// <summary>How many patterns there are. <b>Open by construction</b> — see <see cref="BlockPattern"/>.</summary>
    public const int Count = 6;

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
        Determinism.WorldKey key, BlockPattern pattern, int blockTiles,
        int lotsPerSegment, Span<Parcel> into) =>
        Carve(key, pattern, BlockGround.Square(blockTiles), lotsPerSegment, into);

    /// <inheritdoc cref="Carve(Determinism.WorldKey, BlockPattern, int, int, Span{Parcel})"/>
    /// <remarks>
    /// 🔴 <b>A BLOCK IS TWO EXTENTS AND A PLACE, AND IT USED TO BE ONE NUMBER.</b> Four faces off a
    /// single <c>blockTiles</c> is a square block by construction, and the origin was
    /// <c>column × blockTiles</c> — both true of an evenly spaced lattice and of nothing else.
    /// <see cref="BlockGround"/> carries the four numbers, and every derivation below says which of
    /// them it is reading. <c>plans/0045</c> row 25.
    /// </remarks>
    public static int Carve(
        Determinism.WorldKey key, BlockPattern pattern, BlockGround ground,
        int lotsPerSegment, Span<Parcel> into)
    {
        if (ground.Wide <= 0 || ground.Deep <= 0 || lotsPerSegment <= 0)
        {
            return 0;
        }

        int written = 0;

        // plans/0045 row 24. Hoisted out of the face loop -- four stackallocs in a loop is a stack
        // overflow waiting for a coarse Ruleset, and lotsPerSegment is the most parcels any one face
        // can carry, so one buffer serves all four.
        Span<int> widths = lotsPerSegment <= 32 ? stackalloc int[32] : new int[lotsPerSegment];

        // ⚠ ONE MODULE FOR THE BLOCK, taken off its NARROWER side -- BlockGround.Least's own remark.
        // Tait's structure is regular within a block and varying between them, so a module per face
        // would be the wrong half of the survey; the narrower side is what keeps the short face able
        // to divide by it at all.
        int unit = UnitTiles(key, ground.Column, ground.Row, ground.Least);

        for (BlockFace face = BlockFace.South; face <= BlockFace.East; face++)
        {
            if (!Carries(pattern, face))
            {
                continue;
            }

            StreetSide side = SideOf(face);
            (int low, int high) = ReachTiles(pattern, face, ground, lotsPerSegment);

            // How far this face runs, which is what its Addresses are spaced along and what its
            // parcels divide. ⚠ NOT the block, and not the other axis.
            int along = ground.Along(face);

            // How many Addresses this face carries, which is what its ground divides among. A first
            // pass rather than a running count: a parcel's width needs the total, and the total is
            // not known until every index has been tested.
            int count = 0;

            for (int index = 0; index < lotsPerSegment; index++)
            {
                if (Frontage.SideOf(index) == side
                    && Within(Frontage.OffsetOf(index, lotsPerSegment, along).Raw, low, high))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                continue;
            }

            int depth = DepthTiles(pattern, face, along, ground.Across(face), lotsPerSegment);
            int reach = high - low;

            // How many parcels this face is actually cut into. A coarse pattern joins Addresses; it
            // can never split one, so the ask is capped by what the face carries.
            int wanted = ParcelsPerFace(pattern);
            int groups = wanted <= 0 || wanted >= count ? count : wanted;
            int placed = 0;
            int last = -1;

            // plans/0045 row 24. The face's ground divides into multiples of the BLOCK's module
            // rather than into equal shares of lots_per_segment -- so a face shows two widths and
            // the module varies from block to block. Computed once per face because a parcel's left
            // edge is the sum of the ones before it.
            Widths(key, ground.Column, ground.Row, face, unit, reach, groups, widths);

            for (int index = 0; index < lotsPerSegment; index++)
            {
                Tiles offset = Frontage.OffsetOf(index, lotsPerSegment, along);

                if (Frontage.SideOf(index) != side || !Within(offset.Raw, low, high))
                {
                    continue;
                }

                // Which parcel this Address falls in. At groups == count it is one each, which is
                // the arithmetic the three original patterns had and reproduces them exactly.
                int group = IntegerMath.FloorDiv(placed * groups, count);

                placed++;

                // ⚠ ONLY THE FIRST ADDRESS OF A GROUP LAYS THE PARCEL, and the ones behind it are
                // simply not kept -- there is no Lot, so there is no Address with nowhere to stand.
                // The parcel takes the WHOLE group's slice, which is what makes it bigger rather
                // than merely fewer.
                if (group == last)
                {
                    continue;
                }

                last = group;

                // The left edge is the sum of the widths before it, so the slices abut exactly and
                // the face tiles however the modules fell -- Widths is what guarantees the total.
                int from = low;

                for (int before = 0; before < group; before++)
                {
                    from += widths[before];
                }

                int wide = Narrow(pattern, along, widths[group], ref from);

                into[written++] = Rectangle(face, ground, side, offset, from, wide, depth);
            }
        }

        return written;
    }

    /// <summary>
    /// <b>The block's own plot module, in Tiles</b> — the width every plot on it is a multiple of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A PLOT WIDTH IS A MULTIPLE OF ITS OWN BLOCK'S UNIT AND NOT A FIFTH OF EVERY SEGMENT.</b>
    /// <c>[lots] lots_per_segment</c> is one number for the whole map and it was sizing two different
    /// things: the Addresses a Segment holds, which is <c>adr/0078</c>'s <b>routing-graph</b>
    /// argument — five a Segment is what holds the graph near 30,000 Segments rather than 150,000 —
    /// and the width of the ground behind one, which that argument was never about. ***The five was
    /// chosen to size the graph and was being used to size plots.*** ⚠ <b>The routing argument is
    /// untouched</b>: five <em>on average</em> is compatible with widths that vary, and nothing in
    /// the 30,000 bound needs them equal. <c>plans/0045</c> row 24.
    /// </para>
    /// <para>
    /// <b>A power-of-two fraction of the block, drawn between two adjacent ones.</b> Tait's survey of
    /// 49 Scottish blocks supplies the <em>structure</em> — widths quantised rather than continuous,
    /// the unit block-specific and varying about 2.2× between blocks, regular within a block and
    /// varying between them. 🔴 <b>IT DOES NOT SUPPLY THE STEP, and importing one would make a
    /// Borough plot width a claim about Scotland</b> (<c>plans/0012</c> <b>Cause 5</b>). What supplies
    /// the step here is the grid: a block is a power of two Tiles, so halves and quarters are exact
    /// where thirds are not, and the two adjacent fractions <c>÷8</c> and <c>÷16</c> are a <b>2×</b>
    /// spread — the nearest thing the grid can express to the survey's 2.2 without borrowing its
    /// number.
    /// </para>
    /// <para>
    /// ⚠ <b>WHY TWO FRACTIONS AND NOT SIX, WHICH IS THE QUESTION A SURVEY CANNOT ANSWER.</b> Measured
    /// on <c>minimal.toml</c> at 1,000 Citizens with the draw list's <c>scale</c> row: at the distance
    /// the whole city is read from — the eye 1,000 m out, tilt 40° — <b>one Tile of frontage is 5.35
    /// pixels</b>, and at the distance a player edits from (400 m) it is <b>13.4</b>. A face carries
    /// <b>two or three</b> parcels at the shipped lattice, so a block can show at most three widths
    /// in a row; ***a set of six classes is a distribution nobody can see in a row of three***, and
    /// four of them would sit inside the 5-pixel threshold at the reading distance. <b>Two classes a
    /// face and two modules a map</b> is what those two numbers buy, and it is derived from this
    /// build rather than from a survey.
    /// </para>
    /// <para>
    /// ⚠ <b>Floored, and it stays honest off a power of two.</b> A block that is not a power of two
    /// Tiles gets a floored module and the exactness argument lapses — what does not lapse is the
    /// tiling, because <see cref="Widths"/> gives the last parcel the remainder.
    /// </para>
    /// </remarks>
    public static int UnitTiles(Determinism.WorldKey key, int column, int row, int blockTiles)
    {
        if (blockTiles <= 0)
        {
            return 0;
        }

        // The ground's own coordinates as the entity id -- PurposeTag.PlotUnit's whole remark, and
        // BlockPattern's one level up: a block cleared and zoned again is re-platted the same way.
        ulong patch = ((ulong)(uint)column << 32) | (uint)row;
        ulong draw = Determinism.Randomness.Draw(
            key, patch, Quantities.Ticks.Zero, Determinism.PurposeTag.PlotUnit);

        int unit = IntegerMath.ShiftRight(blockTiles, (int)(3 + (draw & 1)));

        return unit > 0 ? unit : 1;
    }

    /// <summary>
    /// <b>How one face's ground divides among its parcels</b>, in Tiles, quantised to the block's
    /// module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every parcel takes a whole number of modules and the spare ones are dealt to a contiguous
    /// run.</b> A face holding <c>units</c> modules across <c>groups</c> parcels gives each
    /// <c>units ÷ groups</c> and hands the remainder out one apiece — so a face shows exactly
    /// <b>two</b> widths, which is what two or three parcels in a row can express. ⚠ <b>Contiguous
    /// rather than alternating</b>, because a terrace of larger houses at one end is a shape and
    /// a wide-narrow-wide comb is a pattern nobody builds.
    /// </para>
    /// <para>
    /// 🔴 <b>IT TILES THE FACE EXACTLY, WHICH IS NOT A ROUNDING CONVENIENCE.</b>
    /// <see cref="Exhaustive"/> patterns claim to leave no ground over, and a partition that
    /// quantised and then stopped would leave a sliver at one end of every face of every one of them.
    /// ***The last parcel absorbs <c>reach mod unit</c>***, which is under one module and is what the
    /// end of a real terrace does.
    /// </para>
    /// <para>
    /// ⚠ <b>It falls back to the even split when the module is too coarse for the face</b> — fewer
    /// modules than parcels — rather than refusing or dropping a parcel. That is the coarse-Ruleset
    /// case and it reproduces exactly what the carve did before this existed.
    /// </para>
    /// </remarks>
    /// <param name="key">The world key the module and the deal are drawn from.</param>
    /// <param name="column">The block's column.</param>
    /// <param name="row">The block's row.</param>
    /// <param name="face">Which face is being platted — <b>in the deal's entity id.</b></param>
    /// <param name="unit">The block's module, from <see cref="UnitTiles"/>.</param>
    /// <param name="reach">How many Tiles of face the parcels divide.</param>
    /// <param name="groups">How many parcels divide it.</param>
    /// <param name="into">Filled with <paramref name="groups"/> widths, summing to <paramref name="reach"/>.</param>
    public static void Widths(
        Determinism.WorldKey key, int column, int row, BlockFace face,
        int unit, int reach, int groups, Span<int> into)
    {
        if (groups <= 0)
        {
            return;
        }

        int units = unit > 0 ? IntegerMath.FloorDiv(reach, unit) : 0;

        if (units < groups)
        {
            // The even split, which is what the carve did before a module existed. FloorDiv on both
            // edges rather than a width times an index, so the slices abut exactly.
            for (int at = 0; at < groups; at++)
            {
                into[at] = IntegerMath.FloorDiv((at + 1) * reach, groups)
                    - IntegerMath.FloorDiv(at * reach, groups);
            }

            return;
        }

        int baseUnits = IntegerMath.FloorDiv(units, groups);
        int spare = units - (baseUnits * groups);

        // The face in the id, not only the block: PurposeTag.PlotWidths' own remark. Four faces
        // keyed alike would take their spare modules at one position and a block would read as four
        // copies of one terrace.
        ulong patch = ((ulong)(uint)column << 32) | (uint)row;
        ulong draw = Determinism.Randomness.Draw(
            key, patch ^ ((ulong)face << 60), Quantities.Ticks.Zero,
            Determinism.PurposeTag.PlotWidths);

        int from = (int)(draw % (ulong)groups);

        for (int at = 0; at < groups; at++)
        {
            into[at] = baseUnits * unit;
        }

        for (int given = 0; given < spare; given++)
        {
            into[(from + given) % groups] += unit;
        }

        // The end of the terrace takes what the modules did not reach -- under one module, and the
        // reason an Exhaustive pattern still leaves no ground over.
        into[groups - 1] += reach - (units * unit);
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
        BlockFace face, BlockGround ground, StreetSide side, Tiles offset,
        int from, int wide, int depth)
    {
        // ⚠ THE BLOCK'S OWN ORIGIN, which was `column × blockTiles` -- the lattice answers it now,
        // and the two subtractions below measure back from the block's own far edge rather than from
        // one span used for both axes. plans/0045 row 25.
        int baseEast = ground.East;
        int baseNorth = ground.North;

        return face switch
        {
            BlockFace.South => new Parcel(
                face, side, offset,
                new Tiles(baseEast + from), new Tiles(baseNorth),
                new Tiles(wide), new Tiles(depth)),

            BlockFace.North => new Parcel(
                face, side, offset,
                new Tiles(baseEast + from), new Tiles(baseNorth + ground.Deep - depth),
                new Tiles(wide), new Tiles(depth)),

            BlockFace.West => new Parcel(
                face, side, offset,
                new Tiles(baseEast), new Tiles(baseNorth + from),
                new Tiles(depth), new Tiles(wide)),

            _ => new Parcel(
                face, side, offset,
                new Tiles(baseEast + ground.Wide - depth), new Tiles(baseNorth + from),
                new Tiles(depth), new Tiles(wide)),
        };
    }

    /// <summary>
    /// <b>Shrinks a parcel inside its own slice</b>, centring what is left. Every pattern but
    /// <see cref="BlockPattern.Tower"/> keeps the whole slice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT IS THE ONE THING A TOWER NEEDS THAT NO OTHER FORM DOES.</b> Every pattern above it
    /// says how <em>deep</em> a parcel runs and lets the face's own division say how <em>wide</em> —
    /// which is right for a row of plots and wrong for a form whose whole claim is that it does not
    /// use the frontage it was given. ***A tower with <c>ParcelsPerFace</c> of one and no narrowing
    /// is a slab***, because one parcel over one face is the face.
    /// </para>
    /// <para>
    /// ⚠ <b>Centred, so the open ground is a plaza on both sides rather than a gap at one end.</b>
    /// The consequence is that the Address sits off the footprint: a Lot's position is its door on
    /// the kerb and its footprint is its parcel, and the two are already different quantities
    /// (<c>plans/0052</c>). ***A tower is where that gap first becomes visible*** — the door is at
    /// the first Address on the face and the building is in the middle of the block's frontage.
    /// </para>
    /// </remarks>
    private static int Narrow(BlockPattern pattern, int alongTiles, int span, ref int from)
    {
        if (pattern != BlockPattern.Tower)
        {
            return span;
        }

        // ⚠ HALF OF THE FACE'S OWN EXTENT. A tower is a half each way, so the half it takes ALONG
        // the face is a fraction of that face and not of the block's other axis.
        int wide = DepthTiles(pattern, BlockFace.South, alongTiles, alongTiles, 0);

        if (wide >= span || wide <= 0)
        {
            return span;
        }

        from += IntegerMath.FloorDiv(span - wide, 2);

        return wide;
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
        BlockPattern pattern, BlockFace face, int blockTiles, int lotsPerSegment) =>
        ReachTiles(pattern, face, BlockGround.Square(blockTiles), lotsPerSegment);

    /// <inheritdoc cref="ReachTiles(BlockPattern, BlockFace, int, int)"/>
    public static (int Low, int High) ReachTiles(
        BlockPattern pattern, BlockFace face, BlockGround ground, int lotsPerSegment)
    {
        if (face is BlockFace.South or BlockFace.North)
        {
            return (0, ground.Wide);
        }

        // 🔴 THE RESERVATION IS THE SOUTH FACE'S OWN DEPTH AND NO LONGER StripTiles. They were the
        // same number for all three original patterns, which is why the coupling was invisible --
        // Detached and Perimeter both take a strip on the east-west pair. Courtyard does not: it
        // reaches a THIRD of the block, and a reservation still set to the strip let the north and
        // south parcels run under the east and west ones. ***Two rectangles overlapped by 16 Tiles a
        // block, and the partition test is what would have caught it.***
        // ⚠ THE SOUTH FACE'S DEPTH, which runs north-south -- so it is measured across the block's
        // DEEP extent while the reach it trims runs along the same one. plans/0045 row 25.
        int corner = DepthTiles(
            pattern, BlockFace.South, ground.Wide, ground.Deep, lotsPerSegment);

        return (corner, ground.Deep - corner);
    }
}
