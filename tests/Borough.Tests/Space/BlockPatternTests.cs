using Borough.Core.Determinism;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// A pattern is a partition function, and this is the partition tested.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 3.</b> Three patterns, each a real subdivision of a real city block, and
/// the point of having three is that <b>none of them is privileged</b>. So there is no single
/// correctness rule here: ***overlap is a defect in all three; leftover ground is a defect in two of
/// them and the point of the third.***
/// </para>
/// <para>
/// <b>Every test walks Tiles rather than checking arithmetic.</b> A partition claim is about ground,
/// and the cheapest honest way to check ground is to paint it and count — a formula re-derived in a
/// test is the implementation written twice.
/// </para>
/// </remarks>
public sealed class BlockPatternTests
{
    /// <summary>The shipped lattice, which is the case every other figure in the corpus is quoted at.</summary>
    private const int ShippedBlockTiles = 32;

    /// <summary>The shipped <c>lots_per_segment</c> — <c>adr/0078</c>'s <em>five Buildings share a Segment</em>.</summary>
    private const int ShippedLotsPerSegment = 5;

    private static readonly BlockPattern[] All =
    [
        BlockPattern.Detached, BlockPattern.Perimeter, BlockPattern.BackToBack,
        BlockPattern.Courtyard, BlockPattern.Slab, BlockPattern.Tower,
    ];

    /// <summary>Every parcel one pattern yields for one block.</summary>
    private static Parcel[] Carve(BlockPattern pattern, int blockTiles, int lotsPerSegment,
        int column = 3, int row = 4)
    {
        var into = new Parcel[BlockPatterns.Ceiling(lotsPerSegment)];
        int count = BlockPatterns.Carve(pattern, column, row, blockTiles, lotsPerSegment, into);

        return into[..count];
    }

    /// <summary>
    /// How many times each Tile of a block is claimed, indexed <c>[east, north]</c> block-locally.
    /// </summary>
    private static int[,] Paint(
        Parcel[] parcels, int blockTiles, int column, int row)
    {
        var claims = new int[blockTiles, blockTiles];

        foreach (Parcel parcel in parcels)
        {
            for (int east = 0; east < parcel.Wide.Raw; east++)
            {
                for (int north = 0; north < parcel.Deep.Raw; north++)
                {
                    int localEast = parcel.East.Raw + east - (column * blockTiles);
                    int localNorth = parcel.North.Raw + north - (row * blockTiles);

                    Assert.InRange(localEast, 0, blockTiles - 1);
                    Assert.InRange(localNorth, 0, blockTiles - 1);

                    claims[localEast, localNorth]++;
                }
            }
        }

        return claims;
    }

    /// <summary>
    /// Whether every face this pattern carries actually got an Address on this block.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE ONE PRECONDITION EVERY EXHAUSTIVENESS CLAIM HERE IS CONDITIONAL ON.</b> A Segment's
    /// Lots alternate sides by parity and the corner reservation then filters by offset, so <b>one
    /// parity can lose every offset it had</b> — and a face with no Address has nothing to give its
    /// ground to. ⚠ <b>It is not confined to <c>lots_per_segment = 1</c></b>, which is what
    /// <c>plans/0053</c> step 3 first recorded: at <c>block_tiles = 12</c> with 3 Lots a Segment the
    /// reservation is 3 deep, the offsets are 2, 6 and 10, and the east face's parity holds only 2 and
    /// 10 — both outside the reach. See <see cref="A_face_with_no_address_breaks_an_exhaustive_pattern"/>.
    /// </remarks>
    private static bool FillsEveryFace(BlockPattern pattern, int blockTiles, int lotsPerSegment)
    {
        int carried = BlockPatterns.Carries(pattern, BlockFace.West) ? 4 : 2;

        return Carve(pattern, blockTiles, lotsPerSegment)
            .Select(parcel => parcel.Face)
            .Distinct()
            .Count() == carried;
    }

    /// <summary>
    /// 🔴 <b>NO TILE IS CLAIMED TWICE, BY ANY PATTERN.</b>
    /// </summary>
    /// <remarks>
    /// <b>The one rule that is the same for all three</b>, and the reason a partition function is the
    /// right shape for this at all. Overlap is what <c>plans/0049</c> <b>F21</b> was: two faces beside
    /// one junction each laid a Lot, both were correct <em>as Addresses</em>, and the shell had to
    /// invent which of them owned the ground. ***Two inventions landed on one patch.***
    /// </remarks>
    [Theory]
    [InlineData(BlockPattern.Detached)]
    [InlineData(BlockPattern.Perimeter)]
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Courtyard)]
    [InlineData(BlockPattern.Slab)]
    public void No_tile_is_claimed_twice(BlockPattern pattern)
    {
        int[,] claims = Paint(
            Carve(pattern, ShippedBlockTiles, ShippedLotsPerSegment),
            ShippedBlockTiles, 3, 4);

        for (int east = 0; east < ShippedBlockTiles; east++)
        {
            for (int north = 0; north < ShippedBlockTiles; north++)
            {
                Assert.True(
                    claims[east, north] <= 1,
                    $"{pattern} claims ({east}, {north}) {claims[east, north]} times.");
            }
        }
    }

    /// <summary>
    /// 🔴 <b>A pattern that says it tiles the block tiles the block, and one that does not is left
    /// alone.</b>
    /// </summary>
    /// <remarks>
    /// <b>The exhaustiveness test asserts each pattern against its OWN claim, never against one
    /// rule.</b> A single rule would have had to pick a side and would have been wrong about the other
    /// two: leftover ground is a defect in <see cref="BlockPattern.BackToBack"/> and
    /// <see cref="BlockPattern.Perimeter"/> and it is <em>the point</em> of
    /// <see cref="BlockPattern.Detached"/>, where back gardens do not meet and there is scrub between
    /// them.
    /// </remarks>
    [Theory]
    [InlineData(BlockPattern.Detached)]
    [InlineData(BlockPattern.Perimeter)]
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Courtyard)]
    [InlineData(BlockPattern.Slab)]
    public void An_exhaustive_pattern_leaves_no_ground(BlockPattern pattern)
    {
        int[,] claims = Paint(
            Carve(pattern, ShippedBlockTiles, ShippedLotsPerSegment),
            ShippedBlockTiles, 3, 4);

        int unclaimed = 0;

        for (int east = 0; east < ShippedBlockTiles; east++)
        {
            for (int north = 0; north < ShippedBlockTiles; north++)
            {
                if (claims[east, north] == 0)
                {
                    unclaimed++;
                }
            }
        }

        if (BlockPatterns.Exhaustive(pattern))
        {
            Assert.Equal(0, unclaimed);
        }
        else
        {
            Assert.True(
                unclaimed > 0,
                $"{pattern} claims every Tile, so it is exhaustive and says it is not.");
        }
    }

    /// <summary>
    /// <b>The leftover ground of a detached block is the interior and nothing else.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Non-exhaustive is a weak claim on its own</b> — a pattern that dropped one parcel would
    /// satisfy it. What <see cref="BlockPattern.Detached"/> actually claims is that the leftover is a
    /// single square in the middle, the ground between the back fences, and this is that stated as an
    /// assertion.
    /// </para>
    /// <para>
    /// ⚠ <b>The share is REPORTED and not asserted.</b> At the shipped 32 Tiles and 5 Lots the strip
    /// is 6 deep, so 400 of 1,024 Tiles are scrub — <b>39%</b> — and that is large for a suburb.
    /// ***It is a consequence of the shipped block size rather than a number anybody chose***, and
    /// pinning it here would make a derived quantity look ratified.
    /// </para>
    /// </remarks>
    [Fact]
    public void Detached_leaves_the_interior_and_only_the_interior()
    {
        int[,] claims = Paint(
            Carve(BlockPattern.Detached, ShippedBlockTiles, ShippedLotsPerSegment),
            ShippedBlockTiles, 3, 4);

        int strip = BlockPatterns.StripTiles(ShippedBlockTiles, ShippedLotsPerSegment);

        for (int east = 0; east < ShippedBlockTiles; east++)
        {
            for (int north = 0; north < ShippedBlockTiles; north++)
            {
                bool interior = east >= strip && east < ShippedBlockTiles - strip
                    && north >= strip && north < ShippedBlockTiles - strip;

                Assert.True(
                    interior == (claims[east, north] == 0),
                    interior
                        ? $"({east}, {north}) is interior and claimed."
                        : $"({east}, {north}) is not interior and is unclaimed.");
            }
        }
    }

    /// <summary>
    /// <b>Back-to-back lays nothing on the cross streets</b>, which is a terrace's gable end.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The one place in this set where a face with a Street on it yields no Address at all.</b>
    /// ⚠ <b>The Segment is untouched and the block across it is unaffected</b> — a Segment's two sides
    /// are subdivided independently and always were, so the terrace's neighbour may still front the
    /// street the terrace turns its back on.
    /// </remarks>
    [Fact]
    public void Back_to_back_carries_no_address_on_a_cross_street()
    {
        Parcel[] parcels = Carve(BlockPattern.BackToBack, ShippedBlockTiles, ShippedLotsPerSegment);

        Assert.NotEmpty(parcels);
        Assert.DoesNotContain(parcels, parcel => parcel.Face is BlockFace.West or BlockFace.East);

        // And the other two patterns do, which is what makes the absence a property of this one.
        Assert.Contains(
            Carve(BlockPattern.Perimeter, ShippedBlockTiles, ShippedLotsPerSegment),
            parcel => parcel.Face is BlockFace.West or BlockFace.East);
    }

    /// <summary>
    /// <b>The pattern decides the shape and never the Address</b> — every pattern puts a Lot in the
    /// same place, when it puts one there at all.
    /// </summary>
    /// <remarks>
    /// <b>This is what keeps a re-carve from moving standing Buildings sideways.</b> An Address is a
    /// Segment, an offset and a side (<c>adr/0074</c>); none of the three is a property of the
    /// pattern, so a block re-carved under a different pattern reuses the Addresses it kept and
    /// changes only the ground behind them.
    /// </remarks>
    [Fact]
    public void Every_pattern_puts_a_shared_face_s_addresses_in_the_same_place()
    {
        Parcel[] detached = Carve(BlockPattern.Detached, ShippedBlockTiles, ShippedLotsPerSegment);
        Parcel[] perimeter = Carve(BlockPattern.Perimeter, ShippedBlockTiles, ShippedLotsPerSegment);

        Assert.Equal(
            detached.Select(parcel => parcel.Address(3, 4, ShippedBlockTiles)),
            perimeter.Select(parcel => parcel.Address(3, 4, ShippedBlockTiles)));

        // Back-to-back drops two faces, and the ones it keeps are in the same places as the others'.
        Parcel[] terrace = Carve(BlockPattern.BackToBack, ShippedBlockTiles, ShippedLotsPerSegment);

        Assert.Equal(
            detached
                .Where(parcel => parcel.Face is BlockFace.South or BlockFace.North)
                .Select(parcel => parcel.Address(3, 4, ShippedBlockTiles)),
            terrace.Select(parcel => parcel.Address(3, 4, ShippedBlockTiles)));
    }

    /// <summary>
    /// <b>An Address stands on the ground behind it</b>, which nothing else here checks.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is a property rather than an arithmetic identity, and it could fail.</b> The offsets
    /// come from <c>Frontage.OffsetOf</c>, which spaces Lots along the whole Segment; the parcels
    /// divide a face's ground evenly among the Addresses that survived the corner filter. <b>Nothing
    /// makes those two agree by construction</b> — they agree because both are monotone in the same
    /// index, and this is the assertion that would notice if one stopped being.
    /// </remarks>
    [Theory]
    [InlineData(BlockPattern.Detached)]
    [InlineData(BlockPattern.Perimeter)]
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Courtyard)]
    [InlineData(BlockPattern.Slab)]
    public void An_address_falls_inside_its_own_parcel(BlockPattern pattern)
    {
        foreach (Parcel parcel in Carve(pattern, ShippedBlockTiles, ShippedLotsPerSegment))
        {
            int along = parcel.Face is BlockFace.South or BlockFace.North
                ? parcel.East.Raw - (3 * ShippedBlockTiles)
                : parcel.North.Raw - (4 * ShippedBlockTiles);

            int width = parcel.Face is BlockFace.South or BlockFace.North
                ? parcel.Wide.Raw
                : parcel.Deep.Raw;

            Assert.InRange(parcel.Offset.Raw, along, along + width);
        }
    }

    /// <summary>
    /// 🔴 <b>A carried face with NO Address leaves its ground unclaimed, and this is where that
    /// happens.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is a real limit and it is asserted rather than papered over.</b> A Segment's Lots
    /// alternate sides by parity, so at <c>lots_per_segment = 1</c> one parity takes the only Lot and
    /// the opposite side of every face gets none. An exhaustive pattern then leaves the ground behind
    /// that face unclaimed, and the claim it makes is false <em>on that block</em>.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>AND IT IS NOT CONFINED TO ONE LOT A SEGMENT, WHICH IS HOW <c>plans/0053</c> STEP 3
    /// FIRST RECORDED IT.</b> The <b>corner reservation</b> reaches it too: at <c>block_tiles = 12</c>
    /// with 3 Lots a Segment the reservation is 3 deep, the offsets are 2, 6 and 10, and <b>the east
    /// face's parity holds only 2 and 10 — both outside the reach</b>. ***So the case is a property of
    /// the interaction between parity and the corner filter, not of a degenerate key value***, and
    /// <see cref="The_ladder_is_people_per_address_descending"/> found it by inverting.
    /// </para>
    /// <para>
    /// ⚠ <b>This is <c>plans/0053</c> <b>Q5</b>'s evidence</b> — <em>does <c>lots_per_segment</c>
    /// survive as a world number?</em> — and the answer it points at is that a pattern's exhaustiveness
    /// is conditional on a Ruleset it does not own. ***The finding is recorded here because the test
    /// that found it is the cheapest place to keep it.***
    /// </para>
    /// </remarks>
    [Fact]
    public void A_face_with_no_address_breaks_an_exhaustive_pattern()
    {
        Parcel[] parcels = Carve(BlockPattern.Perimeter, ShippedBlockTiles, lotsPerSegment: 1);

        // One parity takes the only Lot on a Segment, so two of the four faces are empty.
        Assert.Equal(2, parcels.Length);

        int[,] claims = Paint(parcels, ShippedBlockTiles, 3, 4);
        int unclaimed = 0;

        for (int east = 0; east < ShippedBlockTiles; east++)
        {
            for (int north = 0; north < ShippedBlockTiles; north++)
            {
                if (claims[east, north] == 0)
                {
                    unclaimed++;
                }
            }
        }

        Assert.True(
            unclaimed > 0,
            "lots_per_segment = 1 no longer empties a face, so this limit has been fixed and this "
            + "test is the thing that should tell you plans/0053 Q5 can be closed.");
    }

    /// <summary>
    /// 🔴 <b>THE LADDER IS DERIVED PER LATTICE, AND THIS IS WHERE THAT STOPPED BEING THE ENUM
    /// ORDER.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The quantity is floor area per Address — how many people stand behind one door — and it
    /// reduces to the door count.</b> <c>BlockPatterns.Ladder</c> sorts on it and <c>ForBand</c>
    /// indexes the result, so this asserts the sort is a real total order rather than asserting any
    /// particular order came out.
    /// </para>
    /// <para>
    /// 🔴 <b>IT WAS GROUND PER ADDRESS UNTIL <c>plans/0059</c>, AND THAT WAS A PROXY.</b> Land behind
    /// a door stood in for people behind a door, which is true of five forms that all house people by
    /// going back and false of <see cref="BlockPattern.Tower"/>, which claims a quarter of its block
    /// and sorted between a suburb and a terrace. ⚠ <b>The five keep the order they had</b>, so this
    /// change replaced the quantity without moving the ladder it produced at the shipped lattice —
    /// ***which is the strongest evidence available that the proxy was a good one right up until it
    /// was not.***
    /// </para>
    /// <para>
    /// 🔴 <b>WHAT THIS TEST USED TO SAY WAS THAT THE ENUM ORDER <em>IS</em> THE INTENSITY ORDER AT
    /// EVERY BLOCK SIZE, AND THAT IS FALSE.</b> It held across three patterns and broke at five.
    /// <b>21 of the 73 reachable lattices invert</b>, and they are not a fringe: every one of them is
    /// <c>lots_per_segment = 4</c>, where <see cref="BlockPattern.BackToBack"/> and
    /// <see cref="BlockPattern.Courtyard"/> carry four Addresses each — so the comparison collapses
    /// onto claimed area and the courtyard, which has a hole in it by design, sorts below the terrace
    /// it is denser than. ***A property asserted to be lattice-independent turned out to be a function
    /// of the lattice***, and the repair was to compute it per lattice rather than to narrow the
    /// claim.
    /// </para>
    /// <para>
    /// ⚠ <b>It still checks the shipped lattice separately</b>, because that one order is what every
    /// picture of this city is drawn from and a silent change to it is a change to what the player
    /// sees.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_ladder_is_people_per_address_descending()
    {
        int swept = 0;

        for (int blockTiles = 4; blockTiles <= 64; blockTiles += 4)
        {
            for (int lotsPerSegment = 2; lotsPerSegment <= 8; lotsPerSegment++)
            {
                if (lotsPerSegment > blockTiles)
                {
                    continue;
                }

                var ladder = new BlockPattern[BlockPatterns.Count];

                BlockPatterns.Ladder(blockTiles, lotsPerSegment, ladder);

                // A permutation: every pattern appears once, so no rung is unreachable and no pattern
                // is unselectable. That is what ForBand's indexing rests on.
                Assert.Equal(All.OrderBy(pattern => pattern), ladder.OrderBy(pattern => pattern));

                for (int rung = 0; rung < BlockPatterns.Count; rung++)
                {
                    Assert.Equal(rung, BlockPatterns.Rung(ladder[rung], blockTiles, lotsPerSegment));
                }

                // And it is sorted on the quantity it says it is: doors DESCENDING, with claimed
                // ground breaking a tie. Floor area per Address is what the ladder means and floor
                // area is `ratio * block` for every pattern (plans/0058 F5), so the comparison
                // reduces to the door count and needs no cross-multiplication any more.
                for (int rung = 1; rung < BlockPatterns.Count; rung++)
                {
                    (int below, int belowDoors) = Ground(ladder[rung - 1], blockTiles, lotsPerSegment);
                    (int above, int aboveDoors) = Ground(ladder[rung], blockTiles, lotsPerSegment);

                    if (belowDoors == 0 || aboveDoors == 0)
                    {
                        continue;
                    }

                    Assert.True(
                        belowDoors > aboveDoors || (belowDoors == aboveDoors && below <= above),
                        $"at block {blockTiles}, {lotsPerSegment} per Segment {ladder[rung - 1]} has "
                        + $"{belowDoors} door(s) on {below} Tiles and sits below {ladder[rung]}'s "
                        + $"{aboveDoors} on {above}.");
                }

                swept++;
            }
        }

        Assert.True(swept > 50, $"only {swept} combinations were reachable, so this swept nothing.");
    }

    /// <summary>
    /// <b>The shipped lattice's ladder, written out</b> — the one order every picture of this city is
    /// drawn from.
    /// </summary>
    /// <remarks>
    /// <b>Addresses at 32 Tiles and 5 Lots a Segment</b>: Detached 8, Perimeter 8, BackToBack 5,
    /// Courtyard 4, Slab 2, Tower 1. ⚠ <b>The figures are this lattice's and travel nowhere</b> — see
    /// <see cref="The_ladder_is_people_per_address_descending"/>, where a third of the range puts the
    /// middle pair the other way round. ⚠ <b>Detached and Perimeter TIE on doors here</b> and are
    /// separated by claimed ground, 624 against 1,024 — the tie-break earning its place at the one
    /// lattice every picture is drawn from.
    /// </remarks>
    [Fact]
    public void The_shipped_lattice_climbs_from_a_suburb_to_a_tower()
    {
        var ladder = new BlockPattern[BlockPatterns.Count];

        BlockPatterns.Ladder(ShippedBlockTiles, ShippedLotsPerSegment, ladder);

        Assert.Equal(
            [
                BlockPattern.Detached, BlockPattern.Perimeter, BlockPattern.BackToBack,
                BlockPattern.Courtyard, BlockPattern.Slab, BlockPattern.Tower,
            ],
            ladder);
    }

    /// <summary>Ground claimed and Addresses laid, for one pattern on one lattice.</summary>
    private static (int Claimed, int Addresses) Ground(
        BlockPattern pattern, int blockTiles, int lotsPerSegment) =>
        (BlockPatterns.ClaimedTiles(pattern, blockTiles, lotsPerSegment),
            BlockPatterns.AddressCount(pattern, blockTiles, lotsPerSegment));

    /// <summary>
    /// <b>A band's position picks a pattern's position</b>, and there is no number in between.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two ordinals against each other</b>, which is what keeps <c>plans/0053</c> step 4's
    /// selection free of an authored cut point. ⚠ <b>Both ENDS are reachable at every band count</b>,
    /// which is what the scaling was changed to guarantee — it divided by <c>bandCount</c> and
    /// therefore stopped one rung short of the top, so at the four bands
    /// <c>rulesets/banded.toml</c> declares ***the coarsest pattern in the set could not be selected
    /// by any Ruleset that could be written.***
    /// </para>
    /// <para>
    /// ⚠ <b>Band <c>0</c> is NO BAND and takes <see cref="BlockPattern.Detached"/></b>, which is the
    /// city that was there before patterns existed and is what every shipped Ruleset but one still
    /// builds.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_bands_position_picks_a_patterns_position()
    {
        var ladder = new BlockPattern[BlockPatterns.Count];

        BlockPatterns.Ladder(ShippedBlockTiles, ShippedLotsPerSegment, ladder);

        BlockPattern For(byte band, int bandCount) =>
            BlockPatterns.ForBand(
                band, bandCount, ShippedBlockTiles, ShippedLotsPerSegment,
                WorldKey.FromSeed(1), 0, 0, spread: 0);

        // No band at all, however many are declared.
        Assert.Equal(BlockPattern.Detached, For(0, 0));
        Assert.Equal(BlockPattern.Detached, For(0, 3));

        // One band takes the top: band 0 already holds the bottom, so a lone declaration that landed
        // on rung 0 would be a band that changes nothing.
        Assert.Equal(ladder[^1], For(1, 1));

        // Two bands take the two ends.
        Assert.Equal(ladder[0], For(1, 2));
        Assert.Equal(ladder[^1], For(2, 2));

        // Five take every rung, in order.
        for (byte band = 1; band <= BlockPatterns.Count; band++)
        {
            Assert.Equal(ladder[band - 1], For(band, BlockPatterns.Count));
        }

        // The shipped four reach both ends and skip one in the middle.
        Assert.Equal(ladder[0], For(1, 4));
        Assert.Equal(ladder[^1], For(4, 4));

        // And a band past the end of what was declared cannot fall off the ladder.
        Assert.Equal(ladder[^1], For(9, 3));
    }

    /// <summary>
    /// <b>A denser band never gets a less intense pattern</b>, whatever the two ladder lengths are.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is what the re-carve ratchet rests on.</b> <c>LotSubdivider.RecarveBlock</c> re-plats
    /// only onto a pattern claiming strictly more ground, so a selection that could go <em>down</em> as
    /// the band goes up would make upzoning a block a no-op that looked like a bug. ***The ratchet and
    /// the selection have to agree about which way is up, and only this checks that they do.***
    /// </remarks>
    [Fact]
    public void A_denser_band_never_gets_a_less_intense_pattern()
    {
        // 🔴 EVERY SPREAD AND SEVERAL BLOCKS, WHICH IS THE POINT OF THE SWEEP SINCE plans/0058.
        // The scatter draw is what could break this, and it cannot only because it depends on the
        // block and never on the band -- so a fixed offset rides a rising rung. A draw that read the
        // band would fail here on some block at some spread, which is the whole reason to sweep both
        // rather than to assert the property once at spread 0 and call the ratchet safe.
        for (int spread = 0; spread < BlockPatterns.Count; spread++)
        for (int block = 0; block < 32; block++)
        for (int bandCount = 1; bandCount <= 12; bandCount++)
        {
            int last = -1;

            for (byte band = 1; band <= bandCount; band++)
            {
                BlockPattern here = BlockPatterns.ForBand(
                    band, bandCount, ShippedBlockTiles, ShippedLotsPerSegment,
                    WorldKey.FromSeed(7), block, block * 3, spread);
                int rung = BlockPatterns.Rung(here, ShippedBlockTiles, ShippedLotsPerSegment);

                Assert.True(
                    rung >= last,
                    $"spread {spread}, block {block}: band {band} of {bandCount} gets {here} at "
                    + $"rung {rung}, below rung {last}.");

                last = rung;
            }
        }
    }

    /// <summary>
    /// <b>The partition holds across the Ruleset's whole range, not only at the shipped numbers.</b>
    /// </summary>
    /// <remarks>
    /// <b>Both keys are tuning data</b> — <c>[roads] block_tiles</c> and <c>[lots] lots_per_segment</c>
    /// — so a claim asserted at one pair of values is a claim about a fixture. ⚠ <b>Blocks a face
    /// cannot fill are excluded and named</b>: see
    /// <see cref="A_face_with_no_address_breaks_an_exhaustive_pattern"/>.
    /// </remarks>
    [Fact]
    public void The_partition_holds_across_the_rulesets_range()
    {
        int swept = 0;

        for (int blockTiles = 4; blockTiles <= 64; blockTiles += 4)
        {
            for (int lotsPerSegment = 2; lotsPerSegment <= 8; lotsPerSegment++)
            {
                if (lotsPerSegment > blockTiles)
                {
                    continue;
                }

                foreach (BlockPattern pattern in All)
                {
                    Parcel[] parcels = Carve(pattern, blockTiles, lotsPerSegment);

                    // A face nothing fronts cannot be tiled, and that is the limit above rather than
                    // a defect in the partition.
                    if (!FillsEveryFace(pattern, blockTiles, lotsPerSegment))
                    {
                        continue;
                    }

                    int[,] claims = Paint(parcels, blockTiles, 3, 4);
                    int unclaimed = 0;

                    for (int east = 0; east < blockTiles; east++)
                    {
                        for (int north = 0; north < blockTiles; north++)
                        {
                            Assert.True(
                                claims[east, north] <= 1,
                                $"{pattern} at block {blockTiles}, {lotsPerSegment} per Segment "
                                + $"claims ({east}, {north}) twice.");

                            if (claims[east, north] == 0)
                            {
                                unclaimed++;
                            }
                        }
                    }

                    if (BlockPatterns.Exhaustive(pattern))
                    {
                        Assert.Equal(0, unclaimed);
                    }

                    swept++;
                }
            }
        }

        Assert.True(swept > 100, $"only {swept} combinations were reachable, so this swept nothing.");
    }
}
