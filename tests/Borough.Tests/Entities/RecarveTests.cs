using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Re-platting a block, and the ratchet that makes it terminate.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 4, and <b>Q3</b> answered.</b> <em>What stops carve and re-carve
/// oscillating?</em> — a ratchet. A pattern may only be replaced by one claiming <b>strictly more</b>
/// of the block's ground, which is monotone, bounded above by the block's own area, and therefore
/// terminating. ***A block cannot re-plat more times than it has ground to give.***
/// </para>
/// <para>
/// ⚠ <b>Hysteresis was the obvious answer and it was refused.</b> A hysteresis band is a width, a
/// width is a number, and a hash-bearing number invented to damp a mechanism is exactly what
/// <c>adr/0052</c> wants a ratifier for.
/// </para>
/// </remarks>
public sealed class RecarveTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static World Populated(string file = "banded.toml", int citizens = 4_000)
    {
        var world = new World(citizens, Shipped(file));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>The first lattice square with Streets on it that nothing has claimed a face of.</summary>
    private static (int Column, int Row) Uncarved(World world)
    {
        StreetGrid streets = world.Roads.Streets;

        for (int row = 0; row < streets.Blocks; row++)
        {
            for (int column = 0; column < streets.Blocks; column++)
            {
                int south = streets.Horizontal(column, row);
                int north = streets.Horizontal(column, row + 1);

                if (south == Rows.NoSlot || north == Rows.NoSlot
                    || world.Frontage.Claimed(south, StreetSide.Left)
                    || world.Frontage.Claimed(north, StreetSide.Right))
                {
                    continue;
                }

                return (column, row);
            }
        }

        throw new InvalidOperationException("every block with Streets on it is already carved.");
    }

    /// <summary>Every live Lot standing on one block.</summary>
    private static List<int> LotsOn(World world, int column, int row)
    {
        var found = new List<int>();

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && Frontage.BlockOf(
                    world.Roads.Streets, world.Lots.East[slot], world.Lots.North[slot],
                    (StreetSide)world.Lots.Side[slot], out int at, out int on)
                && at == column && on == row)
            {
                found.Add(slot);
            }
        }

        return found;
    }

    /// <summary>Carves a block Detached and then bands it up to the top of the ladder.</summary>
    /// <remarks>
    /// ⚠ <b><c>banded.toml</c> declares TWO bands against five rungs, so its two are the two ENDS</b>
    /// — <see cref="BlockPattern.Detached"/> and <see cref="BlockPattern.Slab"/>, with nothing
    /// between. <b>That is not this file's business to fix</b>: its header argues why it has two
    /// bands and the argument is about <c>admits</c> rather than about patterns, so
    /// <c>rulesets/platted.toml</c> is where the whole ladder is walked. ***What this class asserts is
    /// the gable end***, which the slab has for the same reason the terrace does.
    /// </remarks>
    private static (int Column, int Row) Upzoned(World world)
    {
        (int column, int row) = Uncarved(world);

        // Band 1 is the bottom rung, so this carves Detached -- selection happens at the first carve.
        world.BandBlock(column, row, 1);
        LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing);

        Assert.Equal(BlockPattern.Detached, PatternOn(world, column, row));

        // And now the player paints a denser band on it, which is the upzone.
        world.BandBlock(column, row, 2);

        return (column, row);
    }

    private static BlockPattern PatternOn(World world, int column, int row) =>
        world.PatternOf(world.BlockIndex.Slot(column, row), out _);

    /// <summary>
    /// 🔴 <b>An upzoned block that is entirely vacant re-plats.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is <c>adr/0025</c>'s redevelopment endgame, end to end.</b> The player paints a denser
    /// band on a block; nothing happens while it is built; the block empties; and the re-plat lays the
    /// pattern the new band asks for. ⚠ <b>The band and the pattern are two different quantities</b> —
    /// the band caps which kinds may build and the pattern divides the ground — and the re-plat is
    /// where the first reaches the second.
    /// </remarks>
    [Fact]
    public void An_upzoned_vacant_block_re_plats()
    {
        World world = Populated();

        (int column, int row) = Upzoned(world);

        int before = LotsOn(world, column, row).Count;

        Assert.True(before > 0, "the block carved nothing, so this re-plats nothing.");

        int created = LotSubdivider.RecarveBlock(world, column, row);

        Assert.True(created > 0, "the re-plat laid no Lots.");
        Assert.Equal(BlockPattern.Slab, PatternOn(world, column, row));

        // A slab turns its gable ends to the cross streets, so it lays fewer Addresses on more
        // ground. The count is not asserted -- see BlockPatternTests -- but the face set is.
        foreach (int lot in LotsOn(world, column, row))
        {
            Assert.Equal(0, world.Lots.North[lot].Raw % world.Roads.Streets.BlockTiles);
        }
    }

    /// <summary>
    /// 🔴 <b>One standing Building refuses the whole block.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>02 §2.2</c>'s <em>"only vacant land re-parcels"</em> taken at the block's granularity</b>,
    /// because a re-plat moves every boundary on the block and there is no such thing as moving half of
    /// them. That is <c>adr/0025</c>'s <em>"upzoning a built block does nothing until its Buildings go,
    /// which is how redevelopment becomes a real endgame activity rather than a formality"</em>.
    /// </remarks>
    [Fact]
    public void A_single_standing_building_refuses_the_re_plat()
    {
        World world = Populated();

        (int column, int row) = Upzoned(world);

        List<int> lots = LotsOn(world, column, row);

        Assert.NotEmpty(lots);

        world.CreateBuilding(world.Lots.Rows.At(lots[0]), 1, Ticks.Zero, Key);

        Assert.Equal(0, LotSubdivider.RecarveBlock(world, column, row));
        Assert.Equal(BlockPattern.Detached, PatternOn(world, column, row));

        // And every Lot it had is still there, which is the half of the rule that protects the
        // Addresses rather than the Buildings on them.
        Assert.Equal(lots, LotsOn(world, column, row));
    }

    /// <summary>
    /// 🔴 <b>THE RATCHET: a pattern claiming no more ground never replaces one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the termination argument as an assertion.</b> Downzoning a re-platted block asks for
    /// <see cref="BlockPattern.Detached"/>, which sits below the terrace standing on it, and the
    /// re-plat refuses. ⚠ <b>Below on the LADDER and no longer by ground claimed</b> — see
    /// <c>BlockPatterns.Ladder</c>, where a pattern with a courtyard in it claims less than one it is
    /// denser than and the area proxy inverts. <b>The slab is the block standing here</b>, because two
    /// declared bands are the two ends of a five-rung ladder.
    /// </para>
    /// <para>
    /// <b>It is also what a real city does.</b> Re-platting is an intensification — a block re-divided
    /// to get more out of it. ***Nobody re-plats a block in order to use less of it***; land that stops
    /// being wanted is abandoned, not re-surveyed into bigger lots, and abandonment is a different
    /// mechanism with a different name.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_pattern_claiming_less_ground_never_replaces_one()
    {
        World world = Populated();

        (int column, int row) = Upzoned(world);

        Assert.True(LotSubdivider.RecarveBlock(world, column, row) > 0);
        Assert.Equal(BlockPattern.Slab, PatternOn(world, column, row));

        // Down again. The band moves; the ground does not.
        world.BandBlock(column, row, 1);

        Assert.Equal(0, LotSubdivider.RecarveBlock(world, column, row));
        Assert.Equal(BlockPattern.Slab, PatternOn(world, column, row));
    }

    /// <summary>
    /// 🔴 <b>A re-plat run twice does nothing the second time</b>, which is the whole of <b>Q3</b>.
    /// </summary>
    /// <remarks>
    /// <b>An oscillating pair would fail this on the second call</b>, and a pair with hysteresis would
    /// pass it only inside its band. The ratchet passes it everywhere, because the ground a block
    /// claims never goes down and a re-plat needs it to go up.
    /// </remarks>
    [Fact]
    public void A_re_plat_run_twice_does_nothing_the_second_time()
    {
        World world = Populated();

        (int column, int row) = Upzoned(world);

        Assert.True(LotSubdivider.RecarveBlock(world, column, row) > 0);

        List<int> after = LotsOn(world, column, row);

        for (int again = 0; again < 4; again++)
        {
            Assert.Equal(0, LotSubdivider.RecarveBlock(world, column, row));
            Assert.Equal(after, LotsOn(world, column, row));
        }
    }

    /// <summary>
    /// <b>A block nobody has carved does not re-plat</b>, because it has no historical fact to move.
    /// </summary>
    /// <remarks>
    /// <b>The one-based column is what makes this checkable.</b> Zero means <em>nobody has decided</em>
    /// rather than <see cref="BlockPattern.Detached"/>, so a banded-but-uncarved block waits for its
    /// first carve to select rather than being re-platted out of a pattern it never had.
    /// </remarks>
    [Fact]
    public void An_uncarved_block_does_not_re_plat()
    {
        World world = Populated();

        (int column, int row) = Uncarved(world);

        world.BandBlock(column, row, 2);

        Assert.Equal(0, LotSubdivider.RecarveBlock(world, column, row));
        Assert.Empty(LotsOn(world, column, row));

        // And when it is carved, it takes the band's pattern straight away rather than needing a
        // re-plat to get there.
        Assert.True(LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing) > 0);
        Assert.Equal(BlockPattern.Slab, PatternOn(world, column, row));
    }

    /// <summary>
    /// <b>A bandless world never re-plats anything</b>, which is why step 4 changed no behaviour.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Step 4 DID move the State Hash, and not through this.</b> The move is the <c>pattern</c>
    /// column's one-based encoding — every carved block went from <c>0</c> to <c>1</c> — and the carve
    /// itself is unchanged, which <c>GoldenSessionCoverageTests</c>' exact Lot counts hold.
    /// </remarks>
    [Fact]
    public void A_bandless_world_re_plats_nothing()
    {
        World world = Populated("minimal.toml", 1_000);

        for (int slot = 0; slot < world.Blocks.Rows.SlotCount; slot++)
        {
            if (world.Blocks.Rows.IsLive(slot))
            {
                Assert.Equal(
                    0,
                    LotSubdivider.RecarveBlock(
                        world, world.Blocks.LatticeColumn[slot], world.Blocks.LatticeRow[slot]));
            }
        }
    }
}
