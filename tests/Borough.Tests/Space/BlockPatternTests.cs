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
        [BlockPattern.Detached, BlockPattern.BackToBack, BlockPattern.Perimeter];

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
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Perimeter)]
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
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Perimeter)]
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
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Perimeter)]
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
    /// <see cref="The_enum_order_is_the_derived_intensity_order"/> found it by inverting.
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
    /// 🔴 <b>THE LADDER IS THE ENUM ORDER, AND THIS DERIVES IT RATHER THAN ASSERTING IT.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>BlockPatterns.ForBand</c> maps a band's position onto the pattern's position</b>, so the
    /// enum order has to BE the intensity order or the mapping is nonsense. ***The enum order is a
    /// declaration and an intensity order is a fact***, and this is what holds the two together.
    /// </para>
    /// <para>
    /// <b>Sort by ground claimed, then by Address count, both ascending.</b>
    /// <see cref="BlockPattern.Detached"/> claims least because its interior is scrub; the other two
    /// both tile their block and are separated by how finely they divide it —
    /// <see cref="BlockPattern.BackToBack"/> gives up its cross streets and
    /// <see cref="BlockPattern.Perimeter"/> keeps them.
    /// </para>
    /// <para>
    /// ⚠ <b>Across the Ruleset's range and not at the shipped pair</b>, because the ordering is what
    /// the ratchet in <c>LotSubdivider.RecarveBlock</c> compares and a world may be tuned.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_enum_order_is_the_derived_intensity_order()
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

                // 🔴 The same exclusion the range sweep takes, and it is load-bearing here rather
                // than tidy. At block 12 with 3 Lots a Segment the east face loses both its offsets to
                // the corner reservation, Perimeter drops that half-band, and it claims 108 Tiles
                // against BackToBack's 144 -- SO THE LADDER INVERTS. The ordering is a property of
                // patterns that fill their faces, and where they cannot the selection is choosing
                // between shapes that are not what they say they are.
                if (All.Any(pattern => !FillsEveryFace(pattern, blockTiles, lotsPerSegment)))
                {
                    continue;
                }

                BlockPattern[] derived = [.. All
                    .OrderBy(pattern => BlockPatterns.ClaimedTiles(pattern, blockTiles, lotsPerSegment))
                    .ThenBy(pattern => BlockPatterns.AddressCount(pattern, blockTiles, lotsPerSegment))];

                Assert.True(
                    All.SequenceEqual(derived),
                    $"at block {blockTiles}, {lotsPerSegment} per Segment the derived order is "
                    + string.Join(", ", derived.Select(pattern =>
                        $"{pattern} ({BlockPatterns.ClaimedTiles(pattern, blockTiles, lotsPerSegment)} "
                        + $"tiles, {BlockPatterns.AddressCount(pattern, blockTiles, lotsPerSegment)} addresses)")));

                swept++;
            }
        }

        Assert.True(swept > 50, $"only {swept} combinations were reachable, so this swept nothing.");
    }

    /// <summary>
    /// <b>A band's position picks a pattern's position</b>, and there is no number in between.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two ordinals against each other</b>, which is what keeps <c>plans/0053</c> step 4's
    /// selection free of an authored cut point. A Ruleset declaring two bands gets the bottom and the
    /// top of the ladder; one declaring three gets all of it.
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
        // No band at all, however many are declared.
        Assert.Equal(BlockPattern.Detached, BlockPatterns.ForBand(0, 0));
        Assert.Equal(BlockPattern.Detached, BlockPatterns.ForBand(0, 3));

        // Two bands take the two ends of the ladder.
        Assert.Equal(BlockPattern.Detached, BlockPatterns.ForBand(1, 2));
        Assert.Equal(BlockPattern.BackToBack, BlockPatterns.ForBand(2, 2));

        // Three take all of it.
        Assert.Equal(BlockPattern.Detached, BlockPatterns.ForBand(1, 3));
        Assert.Equal(BlockPattern.BackToBack, BlockPatterns.ForBand(2, 3));
        Assert.Equal(BlockPattern.Perimeter, BlockPatterns.ForBand(3, 3));

        // And a band past the end of what was declared cannot fall off the ladder.
        Assert.Equal(BlockPattern.Perimeter, BlockPatterns.ForBand(9, 3));
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
        for (int bandCount = 1; bandCount <= 12; bandCount++)
        {
            var last = BlockPattern.Detached;

            for (byte band = 1; band <= bandCount; band++)
            {
                BlockPattern here = BlockPatterns.ForBand(band, bandCount);

                Assert.True(
                    here >= last,
                    $"band {band} of {bandCount} gets {here} where band {band - 1} got {last}.");

                last = here;
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
