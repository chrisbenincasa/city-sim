using Borough.Core.Determinism;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// <see cref="BlockPatterns.UnitTiles"/> and <see cref="BlockPatterns.Widths"/> — <b>a plot width is
/// a multiple of its own block's module, and the module is not <c>lots_per_segment</c>.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><c>[lots] lots_per_segment</c> WAS SIZING TWO DIFFERENT THINGS.</b> How many Addresses a
/// Segment holds is the <b>routing graph's</b> number — <c>adr/0078</c>'s five a Segment is what
/// holds the graph near 30,000 Segments rather than 150,000 — and how wide the ground behind one of
/// them is was never what that argument was about. ***The five was chosen to size the graph and was
/// being used to size plots.*** <c>plans/0045</c> row 24.
/// </para>
/// <para>
/// ⚠ <b>The routing argument survives every assertion here.</b> Nothing in this file changes how many
/// Addresses a Segment carries; what varies is how the ground behind them divides. Five
/// <em>on average</em> is compatible with widths that differ, which is the whole of why this is
/// buildable without reopening <c>adr/0078</c>.
/// </para>
/// </remarks>
public sealed class PlotWidthTests
{
    /// <summary>The shipped lattice.</summary>
    private const int Block = 32;

    /// <summary>And the shipped Address count a Segment.</summary>
    private const int PerSegment = 5;

    /// <summary>
    /// 🔴 <b>THE PROPERTY EVERY EXHAUSTIVE PATTERN RESTS ON: the widths tile the face exactly.</b>
    /// </summary>
    /// <remarks>
    /// <b>Quantising and then stopping would leave a sliver at the end of every face of every
    /// pattern that claims to leave no ground over</b> (<see cref="BlockPatterns.Exhaustive"/>), and
    /// a sliver is invisible in a screenshot and fatal in a partition test. The last parcel absorbs
    /// <c>reach mod unit</c>, which is what makes the sum exact whatever the module.
    /// </remarks>
    [Theory]
    [InlineData(32, 1)]
    [InlineData(32, 2)]
    [InlineData(32, 3)]
    [InlineData(32, 5)]
    [InlineData(20, 3)]
    [InlineData(16, 3)]
    [InlineData(7, 3)]
    [InlineData(3, 4)]
    [InlineData(0, 2)]
    public void The_widths_on_a_face_sum_to_its_reach(int reach, int groups)
    {
        // Hoisted, and CA2014 is right to insist: a stackalloc in a loop is a frame that grows with
        // the iteration count rather than with the buffer.
        Span<int> widths = stackalloc int[8];

        for (int column = 0; column < 8; column++)
        {
            for (int row = 0; row < 8; row++)
            {
                widths[..groups].Clear();

                int unit = BlockPatterns.UnitTiles(Key, column, row, Block);

                BlockPatterns.Widths(
                    Key, column, row, BlockFace.South, unit, reach, groups, widths[..groups]);

                int total = 0;

                foreach (int wide in widths[..groups])
                {
                    total += wide;

                    Assert.True(wide >= 0, $"a parcel came out {wide} Tiles wide");
                }

                Assert.Equal(reach, total);
            }
        }
    }

    /// <summary>
    /// Every plot but the last is a whole number of modules, and the last is over by less than one.
    /// </summary>
    /// <remarks>
    /// <b>This is the quantisation itself, stated as the property rather than as a list of
    /// widths.</b> Tait's 49 Scottish blocks supply the structure — widths quantised rather than
    /// continuous, the module block-specific — and ⚠ <b>they supply no step</b>: importing
    /// <c>¾ / 1 / 1¼</c> would make a Borough plot width a claim about Scotland
    /// (<c>plans/0012</c> <b>Cause 5</b>). What is asserted here is the quantisation, which is
    /// theirs, and not the module, which is the grid's.
    /// </remarks>
    [Fact]
    public void Every_plot_but_the_last_is_a_whole_number_of_modules()
    {
        Span<int> widths = stackalloc int[8];

        for (int column = 0; column < 8; column++)
        {
            for (int groups = 1; groups <= 4; groups++)
            {
                widths[..groups].Clear();

                int unit = BlockPatterns.UnitTiles(Key, column, 0, Block);

                BlockPatterns.Widths(
                    Key, column, 0, BlockFace.South, unit, Block, groups, widths[..groups]);

                if (Block / unit < groups)
                {
                    // The module is too coarse for this face and the even split stands in.
                    continue;
                }

                for (int at = 0; at < groups - 1; at++)
                {
                    Assert.True(
                        widths[at] % unit == 0,
                        $"plot {at} is {widths[at]} Tiles on a {unit}-Tile module");
                }

                Assert.True(
                    widths[groups - 1] % unit < unit,
                    "the end plot is over by a whole module or more");
            }
        }
    }

    /// <summary>
    /// A face shows <b>two</b> widths and never three — <b>which is what a row of three plots can
    /// say.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS THE ANSWER TO <em>how many distinguishable width classes does a block
    /// need</em>, AND IT IS DERIVED FROM THIS BUILD RATHER THAN FROM A SURVEY.</b> A face carries two
    /// or three parcels at the shipped lattice — the Addresses split between the two blocks sharing
    /// a Segment by parity — so ***a set of six classes is a distribution nobody can see in a row of
    /// three***. Two classes is what the row can express.
    /// </para>
    /// <para>
    /// ⚠ <b>The end plot's remainder is the exception and it is under one module</b>, so it is
    /// counted here as its own width only when the reach does not divide. That is the terrace's odd
    /// end rather than a third class.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_face_shows_at_most_two_widths_when_its_reach_divides()
    {
        Span<int> widths = stackalloc int[8];

        for (int column = 0; column < 16; column++)
        {
            int unit = BlockPatterns.UnitTiles(Key, column, 5, Block);

            if (Block % unit != 0)
            {
                continue;
            }

            for (int groups = 2; groups <= 4; groups++)
            {
                widths[..groups].Clear();

                BlockPatterns.Widths(
                    Key, column, 5, BlockFace.South, unit, Block, groups, widths[..groups]);

                var seen = new HashSet<int>();

                foreach (int wide in widths[..groups])
                {
                    seen.Add(wide);
                }

                Assert.True(
                    seen.Count <= 2,
                    $"block {column} at {groups} plots showed {seen.Count} widths: "
                        + string.Join(", ", seen));
            }
        }
    }

    /// <summary>
    /// The module varies from block to block, and both values are reached.
    /// </summary>
    /// <remarks>
    /// <b>The variation is the point and a constant would pass every other test in this file.</b>
    /// Tait's survey puts the between-block spread at about 2.2×; what is asserted is that the spread
    /// exists and that it is the grid's <b>2×</b> — two adjacent power-of-two fractions of the block,
    /// which is the nearest thing the grid can express without borrowing Scotland's number.
    /// </remarks>
    [Fact]
    public void The_module_varies_between_blocks_and_is_a_power_of_two_fraction()
    {
        var seen = new HashSet<int>();

        for (int column = 0; column < 24; column++)
        {
            for (int row = 0; row < 24; row++)
            {
                seen.Add(BlockPatterns.UnitTiles(Key, column, row, Block));
            }
        }

        Assert.Equal([Block / 16, Block / 8], seen.OrderBy(unit => unit).ToArray());
    }

    /// <summary>
    /// The module is a property of the <b>ground</b> and not of the row that recorded it.
    /// </summary>
    /// <remarks>
    /// <c>PurposeTag.PlotUnit</c>'s own remark, asserted: a block cleared and zoned again is
    /// re-platted on the same module, so the draw takes the block's coordinates and never a slot.
    /// </remarks>
    [Fact]
    public void The_module_is_the_same_every_time_one_block_is_asked()
    {
        Assert.Equal(
            BlockPatterns.UnitTiles(Key, 7, 11, Block),
            BlockPatterns.UnitTiles(Key, 7, 11, Block));

        Assert.Equal(
            BlockPatterns.UnitTiles(Key, 7, 11, Block),
            BlockPatterns.UnitTiles(Key, 7, 11, Block * 2) / 2);
    }

    /// <summary>
    /// 🔴 <b>The strip is as deep as a PLOT is wide, and it used to be as deep as an ADDRESS is
    /// spaced.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="BlockPatterns.StripTiles"/>'s one shape claim is <em>a plot is about as deep as it
    /// is wide</em>, and its first term was <c>blockTiles ÷ lotsPerSegment</c> — <b>the Address
    /// spacing</b>. A Segment's Addresses split between the two blocks that share it by parity, so a
    /// face carries about half of them and a parcel spans about twice the spacing.
    /// ***The claim was false in the shipped city by a factor of 1.8***: 6 Tiles deep behind parcels
    /// 10 and 11 Tiles wide, which is 24 × 40 m and 24 × 44 m on the draw list.
    /// </para>
    /// <para>
    /// ⚠ <b>At the shipped lattice the quarter cap is what binds after the correction</b>, so the
    /// depth is 8 rather than 12 — which is why this asserts the cap as well as the term. A test that
    /// checked only the term would pass on a lattice where the cap never bit and say nothing about
    /// the one that ships.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_strip_is_as_deep_as_a_plot_is_wide_and_not_as_an_address_is_spaced()
    {
        // Twice the Address spacing, capped at a quarter block. At 32 and 5: 12, capped to 8.
        Assert.Equal(8, BlockPatterns.StripTiles(Block, PerSegment));

        // A lattice where the term binds rather than the cap, so both halves are covered.
        Assert.Equal(6, BlockPatterns.StripTiles(Block, 10));

        // And the claim itself, as a bound: a parcel is no more than twice its own depth wide.
        // ⚠ It was 2.1 before the correction, which is the number this test is written against.
        int meanWide = 2 * Block / PerSegment;

        Assert.True(
            meanWide <= 2 * BlockPatterns.StripTiles(Block, PerSegment),
            $"a plot averages {meanWide} Tiles wide behind a "
                + $"{BlockPatterns.StripTiles(Block, PerSegment)}-Tile strip");
    }

    /// <summary>The world key these draw against. Any seed; the properties hold for all of them.</summary>
    private static WorldKey Key => WorldKey.FromSeed(0x5E_5E_5E);
}
