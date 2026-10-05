using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// A block is derived from the Streets around it. Its zoning lives in the permission paint and its
/// pattern in its Lots' saved forms.
/// </summary>
public sealed class BlockTests
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

    private static World Populated(string file = "minimal.toml", int citizens = 1_000)
    {
        var world = new World(citizens, Shipped(file));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>Every lattice square that has Lots on it, with the pattern they were carved by.</summary>
    private static List<(int Column, int Row, BlockPattern Pattern)> Carved(World world)
    {
        var found = new List<(int, int, BlockPattern)>();
        int squares = world.Roads.Streets.Blocks;

        for (int row = 0; row < squares; row++)
        {
            for (int column = 0; column < squares; column++)
            {
                if (LotSubdivider.PatternOn(world, column, row, out BlockPattern pattern))
                {
                    found.Add((column, row, pattern));
                }
            }
        }

        return found;
    }

    private static LandPermissionSummary Paint(World world, int column, int row) =>
        world.LandPermissions.Summary(world.BlockGroundRectangle(column, row));

    /// <summary>
    /// 🔴 <b>Zoning land with no Street on any face keeps the zoning anyway.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §2.2</c>'s third rule stands. The land yields no Lots, but the paint survives, so a Street
    /// laid later finds land that knows what it was painted for.
    /// </remarks>
    [Fact]
    public void Zoning_land_the_network_cannot_reach_still_records_the_block()
    {
        World world = Populated();

        int blocks = world.Roads.Streets.Blocks;

        // Far outside the generated lattice, which is sized to the city rather than to the map -- so
        // this block is ON the lattice and has no Street on any face, which is exactly the case.
        int column = blocks - 2;
        int row = blocks - 2;

        Assert.Equal(0, Paint(world, column, row).AnyUses);

        int carved = LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing);

        Assert.Equal(0, carved);

        Assert.Equal(LotTable.Housing, Paint(world, column, row).AnyUses);
    }

    /// <summary><b>Re-zoning overwrites rather than accumulating</b>, because a Zone is the whole payload.</summary>
    [Fact]
    public void Re_zoning_a_block_replaces_its_permission_set()
    {
        World world = Populated();

        int blocks = world.Roads.Streets.Blocks;

        Assert.True(world.ZoneBlock(blocks - 3, blocks - 3, LotTable.Housing));
        Assert.Equal(LotTable.Housing, Paint(world, blocks - 3, blocks - 3).AnyUses);

        Assert.True(world.ZoneBlock(blocks - 3, blocks - 3, LotTable.Trade));
        Assert.Equal(LotTable.Trade, Paint(world, blocks - 3, blocks - 3).AnyUses);
    }

    /// <summary>
    /// <b>A block off the lattice paints nothing and does not throw.</b>
    /// </summary>
    [Fact]
    public void Zoning_off_the_lattice_records_nothing()
    {
        World world = Populated();

        int before = world.PermissionRectangles.Rows.LiveCount;

        Assert.False(world.ZoneBlock(-1, 0, LotTable.Housing));
        Assert.False(world.ZoneBlock(0, int.MaxValue, LotTable.Housing));

        Assert.Equal(before, world.PermissionRectangles.Rows.LiveCount);
    }

    /// <summary><b>A world whose Ruleset declares no band carries band 0 everywhere.</b></summary>
    /// <remarks>
    /// <b>Which is the state of every shipped file but one</b>, and it is what keeps <c>plans/0053</c>
    /// step 2 from being an edit to twelve Rulesets. Band 0 is <em>no band</em> and admits everything.
    /// </remarks>
    [Fact]
    public void A_bandless_world_carries_no_band_on_any_block()
    {
        World world = Populated();

        Assert.False(world.Rules.HasBands);

        foreach ((int column, int row, _) in Carved(world))
        {
            Assert.Equal(0, Paint(world, column, row).Band);
        }
    }

    /// <summary>
    /// 🔴 <b>The generator paints bands as concentric rings, densest at the middle.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the assertion that makes bands more than a parsed table.</b> It checks the two things
    /// that would each be silently true of a broken layout: that every band declared actually appears
    /// somewhere, and that <b>the middle is denser than the edge</b> — a generator painting one band on
    /// everything would pass a count check and fail this.
    /// </para>
    /// <para>
    /// ⚠ <b>It asserts an ORDERING and never a boundary.</b> Where a ring falls is derived from the
    /// lattice's half-span and the band count, so a figure here would be a number nobody chose and
    /// nothing could ratify — see <c>SyntheticCity.BandAt</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_generator_paints_the_densest_band_in_the_middle()
    {
        World world = Populated("banded.toml", 4_000);

        Assert.True(world.Rules.HasBands);

        // Every carved block, by band. The generator raster-scans and stops once it has land enough,
        // so the window it painted is not the whole lattice -- which is why nothing below reads a
        // lattice coordinate directly.
        var byBand = new Dictionary<byte, List<(int Column, int Row)>>();

        foreach ((int column, int row, _) in Carved(world))
        {
            LandPermissionSummary paint = Paint(world, column, row);
            byte band = paint.MixedIntensity ? (byte)0 : paint.Band;

            if (!byBand.TryGetValue(band, out List<(int Column, int Row)>? found))
            {
                found = [];
                byBand[band] = found;
            }

            found.Add((column, row));
        }

        // Every declared band appears. A layout that collapsed to one band would still have carved
        // blocks and still have recorded a value, and only this notices.
        Assert.Equal(world.Rules.Bands.Length, byBand.Count);

        // And nothing carries band 0, because this Ruleset declares bands and the generator paints
        // one on every block it carves.
        Assert.DoesNotContain((byte)0, byBand.Keys);

        byte densest = byBand.Keys.Max();
        List<(int Column, int Row)> middle = byBand[densest];

        // The centre is taken from the densest band's own blocks rather than from the lattice, so
        // this asserts the SHAPE and never reproduces the generator's arithmetic. A test that
        // recomputed firstColumn and the half-span would pass against a copy of the bug.
        int centreColumn = (int)middle.Average(block => block.Column);
        int centreRow = (int)middle.Average(block => block.Row);

        double Reach(byte band) => byBand[band].Average(block =>
            Math.Max(
                Math.Abs(block.Column - centreColumn),
                Math.Abs(block.Row - centreRow)));

        // The ORDERING, which is the whole claim: a sparser band sits further out than the band
        // above it. ⚠ A distance is never asserted -- where a ring falls is derived from the
        // lattice's half-span and the band count, so a figure here would be a number nobody chose
        // and nothing could ratify. See SyntheticCity.BandAt.
        for (byte band = densest; band > 1; band--)
        {
            Assert.True(
                Reach((byte)(band - 1)) > Reach(band),
                $"band {band - 1} sits at a mean reach of {Reach((byte)(band - 1)):F2} and band "
                + $"{band} at {Reach(band):F2}, so the rings are not densest at the middle.");
        }
    }

    /// <summary>
    /// 🔴 <b>The block's pattern decides what the subdivider carves on it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>plans/0053</c> step 3, read through the subdivider rather than around it.</b>
    /// <c>BlockPatternTests</c> asserts that the partition function partitions; this asserts that
    /// anything calls it. ***A partition function nothing carves through is a geometry library.***
    /// </para>
    /// <para>
    /// <b>The claim is that the two differ, not by how much.</b> How many Lots a face carries falls
    /// out of <c>lots_per_segment</c> and the parity split, so a count here would be a fixture's
    /// number pinned in a test — and the thing under test is that the pattern reached the carve at
    /// all.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_blocks_pattern_changes_what_the_subdivider_carves()
    {
        World detached = Populated();
        World terrace = Populated();

        // A block that HAS Streets on its faces and that the generator has not carved yet -- it
        // stops as soon as it has land enough for the population, so there is always one.
        (int column, int row) = Uncarved(detached);

        // Back-to-back turns its gable ends to the cross streets, so it carries no Address on them.
        int carvedDetached = LotSubdivider.SubdivideBlock(detached, column, row, LotTable.Housing);
        int carvedTerrace = LotSubdivider.SubdivideBlock(terrace, column, row, LotTable.Housing, BlockPattern.BackToBack);

        Assert.True(carvedDetached > 0, "the detached block carved nothing, so this compares nothing.");
        Assert.True(
            carvedTerrace < carvedDetached,
            $"back-to-back carved {carvedTerrace} Lots and detached carved {carvedDetached}, so "
            + "the pattern did not reach the carve.");

        // And what it did carve is on the two faces it keeps. A Lot's Side plus its position is what
        // Frontage.BlockOf inverts, so this reads the face the same way the world does.
        for (int lot = 0; lot < terrace.Lots.Rows.SlotCount; lot++)
        {
            if (!terrace.Lots.Rows.IsLive(lot)
                || !Frontage.BlockOf(
                    terrace.Roads.Streets, terrace.Lots.East[lot], terrace.Lots.North[lot],
                    (StreetSide)terrace.Lots.Side[lot], out int at, out int on)
                || at != column || on != row)
            {
                continue;
            }

            // A horizontal face sits exactly on a lattice row line; a vertical one does not.
            Assert.Equal(
                0,
                terrace.Lots.North[lot].Raw % terrace.Roads.Streets.BlockTiles);
        }
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

    /// <summary>
    /// <b>A block in a bandless world is carved Detached</b>, which is the shape the subdivider had
    /// before patterns existed.
    /// </summary>
    [Fact]
    public void A_bandless_worlds_blocks_are_carved_detached()
    {
        World world = Populated();

        List<(int Column, int Row, BlockPattern Pattern)> carved = Carved(world);

        Assert.NotEmpty(carved);
        Assert.All(carved, block => Assert.Equal(BlockPattern.Detached, block.Pattern));
    }
}
