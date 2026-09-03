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
    /// 🔴 <b>NOT EXHAUSTIVE, DELIBERATELY, and it is one of two that are not.</b> The other is
    /// <see cref="Courtyard"/>, whose hole is a courtyard rather than scrub. The
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
        pattern is BlockPattern.Courtyard or BlockPattern.Slab ? 1 : 0;

    /// <summary>Whether a pattern lays Addresses on this face at all.</summary>
    /// <remarks>
    /// <b><see cref="BlockPattern.BackToBack"/> is the only one that refuses a face</b>, and it refuses
    /// the cross streets because a terrace shows them its gable end. ⚠ <b>The face still has a Street
    /// on it and still has a block on the other side</b>, which may lay its own Lots there under its
    /// own pattern — a Segment's two sides are subdivided independently and always were.
    /// </remarks>
    public static bool Carries(BlockPattern pattern, BlockFace face) =>
        pattern is not (BlockPattern.BackToBack or BlockPattern.Slab)
        || face is BlockFace.South or BlockFace.North;

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
            BlockPattern.BackToBack or BlockPattern.Slab => face == BlockFace.South ? half : far,
            BlockPattern.Perimeter => face is BlockFace.South or BlockFace.North
                ? StripTiles(blockTiles, lotsPerSegment)
                : face == BlockFace.West ? half : far,

            // A THIRD, so the hole is a third of the block across and the frame is a third each
            // side. It is the same class of statement as StripTiles' quarter cap -- a fraction of
            // what the player drew, not a length -- and it is what makes the form a COURTYARD block
            // rather than a deep-plan one: at a half the frame closes and the hole is gone.
            BlockPattern.Courtyard => IntegerMath.FloorDiv(blockTiles, 3),
            _ => StripTiles(blockTiles, lotsPerSegment),
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

        int count = Carve(pattern, 0, 0, blockTiles, lotsPerSegment, parcels);
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

        return Carve(pattern, 0, 0, blockTiles, lotsPerSegment, parcels);
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
    public static BlockPattern ForBand(byte band, int bandCount, int blockTiles, int lotsPerSegment)
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

        Span<BlockPattern> ladder = stackalloc BlockPattern[Count];

        Ladder(blockTiles, lotsPerSegment, ladder);

        return ladder[rung >= Count ? Count - 1 : rung];
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
    /// <b>The quantity is GROUND PER ADDRESS, ascending — how much land stands behind one door.</b> It
    /// is derived rather than chosen and it needs no cut point. ⚠ <b>What changed is not the
    /// quantity but where it is read</b>: the ladder is now computed for the lattice in hand instead
    /// of being fixed once and asserted to be lattice-independent, which it is not. <c>block_tiles</c>
    /// and <c>lots_per_segment</c> are world-creation keys, so within a world this is a constant and
    /// the ratchet in <c>LotSubdivider.RecarveBlock</c> still compares two positions on one ladder.
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
    /// <b>The rung times the step plus two, and there is no table.</b> Two is the floor — a building
    /// with no upper floor is a shed — and the rest is <see cref="Rung"/>, so ***height rises with
    /// density because it is the same quantity***, not because anybody wrote a height beside each
    /// pattern. At <c>storeys_per_rung = 1</c> and the shipped lattice that is a two-storey suburb,
    /// a three-storey perimeter block, a four-storey terrace, a five-storey courtyard block and a
    /// six-storey slab.
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
    /// wrong: whichever form gives one door less ground is the taller one.
    /// </para>
    /// </remarks>
    public static int Storeys(
        BlockPattern pattern, int blockTiles, int lotsPerSegment, int storeysPerRung) =>
        (Rung(pattern, blockTiles, lotsPerSegment) * (storeysPerRung < 1 ? 1 : storeysPerRung)) + 2;

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

    /// <summary>Whether <paramref name="left"/> gives one Address MORE ground than <paramref name="right"/> does.</summary>
    /// <remarks>
    /// <b>Cross-multiplied rather than divided</b>, so the comparison is exact at every lattice and
    /// there is no rounding to argue about — and it stays inside <c>adr/0003</c> without reaching for
    /// <c>Borough.Core.Arithmetic</c> at all. ⚠ <b>A pattern laying NO Address sorts to the top</b>,
    /// where nothing selects it, rather than to the bottom where a band of 1 would.
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

        long lhs = (long)claimed[(int)left] * rightAddresses;
        long rhs = (long)claimed[(int)right] * leftAddresses;

        return lhs != rhs ? lhs < rhs : claimed[(int)left] < claimed[(int)right];
    }

    /// <summary>How many patterns there are. <b>Open by construction</b> — see <see cref="BlockPattern"/>.</summary>
    public const int Count = 5;

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

            // How many parcels this face is actually cut into. A coarse pattern joins Addresses; it
            // can never split one, so the ask is capped by what the face carries.
            int wanted = ParcelsPerFace(pattern);
            int groups = wanted <= 0 || wanted >= count ? count : wanted;
            int placed = 0;
            int last = -1;

            for (int index = 0; index < lotsPerSegment; index++)
            {
                Tiles offset = Frontage.OffsetOf(index, lotsPerSegment, blockTiles);

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

                // FloorDiv on both edges rather than a width times an index: the slices then abut
                // exactly and the last one absorbs the remainder, so a reach that does not divide by
                // the count still tiles.
                int from = low + IntegerMath.FloorDiv(group * reach, groups);
                int to = low + IntegerMath.FloorDiv((group + 1) * reach, groups);

                into[written++] = Rectangle(face, column, row, blockTiles, side, offset, from, to - from, depth);
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

        // 🔴 THE RESERVATION IS THE SOUTH FACE'S OWN DEPTH AND NO LONGER StripTiles. They were the
        // same number for all three original patterns, which is why the coupling was invisible --
        // Detached and Perimeter both take a strip on the east-west pair. Courtyard does not: it
        // reaches a THIRD of the block, and a reservation still set to the strip let the north and
        // south parcels run under the east and west ones. ***Two rectangles overlapped by 16 Tiles a
        // block, and the partition test is what would have caught it.***
        int corner = DepthTiles(pattern, BlockFace.South, blockTiles, lotsPerSegment);

        return (corner, blockTiles - corner);
    }
}
