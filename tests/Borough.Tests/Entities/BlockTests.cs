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

    /// <summary>
    /// 🔴 <b>A plot overlapping a standing Lot loses depth from its back edge, or is dropped.</b>
    /// </summary>
    /// <remarks>
    /// A probe world carves the block with nothing in the way to find one south-face plot. Its twin
    /// gets a standing Lot over that plot's back, from the minimum depth onward, and then carves.
    /// </remarks>
    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    public void A_plot_overlapping_a_standing_lot_shrinks_to_the_minimum_depth_or_is_dropped(int minimum)
    {
        const int Depth = 5;
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        string stated = minimum == 0 ? text
            : text.Replace("[lots]\n", $"[lots]\nmin_plot_depth_tiles = {minimum}\n", StringComparison.Ordinal);
        Ruleset rules = RulesetLoader.Parse(stated, "min-depth.toml").Ruleset!;

        World probe = new(1_000, rules);
        SyntheticCity.PopulateInto(probe, Key, Ticks.Zero);
        (int column, int row) = Uncarved(probe);
        LotSubdivider.SubdivideBlock(probe, column, row, LotTable.Housing);

        int target = -1;
        for (int lot = 0; lot < probe.Lots.Rows.SlotCount; lot++)
        {
            if (probe.Lots.Rows.IsLive(lot)
                && Frontage.BlockOf(probe.Roads.Streets, probe.Lots.East[lot], probe.Lots.North[lot],
                    (StreetSide)probe.Lots.Side[lot], out int at, out int on, out BlockFace face)
                && at == column && on == row && face == BlockFace.South
                && probe.Lots.ParcelBounds(lot).Height > Depth)
            {
                target = lot;
                break;
            }
        }

        Assert.True(target >= 0, "the probe carved no south-face plot deeper than the minimum.");
        LandRectangle plot = probe.Lots.ParcelBounds(target);

        World twin = new(1_000, rules);
        SyntheticCity.PopulateInto(twin, Key, Ticks.Zero);
        twin.Lots.Create(new Tiles(plot.X), new Tiles(plot.Y + Depth), LotTable.Housing, StreetSide.Left,
            new Tiles(plot.Width), new Tiles(plot.Height - Depth));
        LotSubdivider.SubdivideBlock(twin, column, row, LotTable.Housing);

        int found = -1;
        for (int lot = 0; lot < twin.Lots.Rows.SlotCount; lot++)
        {
            if (twin.Lots.Rows.IsLive(lot) && twin.Lots.East[lot] == probe.Lots.East[target]
                && twin.Lots.North[lot] == probe.Lots.North[target] && twin.Lots.Side[lot] == probe.Lots.Side[target])
            {
                found = lot;
            }
        }

        if (minimum == 0)
        {
            Assert.Equal(-1, found);
            return;
        }

        Assert.True(found >= 0, "the overlapping plot was dropped rather than shrunk.");
        Assert.Equal(new LandRectangle(plot.X, plot.Y, plot.Width, Depth), twin.Lots.ParcelBounds(found));
    }

    /// <summary>
    /// <b>A block with an open side carves roadside strips on the sides that have a Street.</b>
    /// </summary>
    /// <remarks>
    /// A square beyond the generated lattice's top row has a Street on its south side only, so it lies
    /// in no closed face. The grid's own lookup is the oracle for which side that is.
    /// </remarks>
    [Fact]
    public void An_open_sided_block_carves_only_the_sides_with_a_street()
    {
        World world = Populated();
        StreetGrid streets = world.Roads.Streets;
        (int column, int row) = (-1, -1);

        for (int r = 0; r < streets.Blocks && column < 0; r++)
        {
            for (int c = 0; c < streets.Blocks; c++)
            {
                if (streets.Horizontal(c, r) != Rows.NoSlot && streets.Horizontal(c, r + 1) == Rows.NoSlot
                    && streets.Vertical(c, r) == Rows.NoSlot && streets.Vertical(c + 1, r) == Rows.NoSlot
                    && !world.Frontage.Claimed(streets.Horizontal(c, r), StreetSide.Left))
                {
                    (column, row) = (c, r);
                    break;
                }
            }
        }

        Assert.True(column >= 0, "no square with a Street on its south side alone.");
        Assert.True(LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing) > 0);

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (world.Lots.Rows.IsLive(lot)
                && Frontage.BlockOf(streets, world.Lots.East[lot], world.Lots.North[lot],
                    (StreetSide)world.Lots.Side[lot], out int at, out int on, out BlockFace face)
                && at == column && on == row)
            {
                Assert.Equal(BlockFace.South, face);
                Assert.Equal(world.Roads.Segments.Rows.IdAt(streets.Horizontal(column, row)),
                    world.Roads.Segments.Rows.IdAt(world.Lots.FrontageOn(lot)));
            }
        }
    }

    /// <summary><b>New plots claim ground by Segment id, then side, then offset.</b></summary>
    [Fact]
    public void A_carve_creates_its_lots_in_claim_order()
    {
        World world = Populated();
        (int column, int row) = Uncarved(world);
        int first = world.Lots.Rows.SlotCount;

        Assert.True(LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing) > 1);

        var order = new List<(ulong Segment, byte Side, int Offset)>();
        for (int lot = first; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (world.Lots.Rows.IsLive(lot))
            {
                order.Add((world.Roads.Segments.Rows.IdAt(world.Lots.FrontageOn(lot)), world.Lots.Side[lot],
                    world.Lots.FrontageOffset[lot].Raw));
            }
        }

        Assert.True(order.Select(o => o.Segment).Distinct().Count() > 1, "the carve fronted one Segment only.");
        Assert.Equal(order.OrderBy(o => o.Segment).ThenBy(o => o.Side).ThenBy(o => o.Offset), order);
    }

    /// <summary><b>A curved Street's sides are cut into plots that turn with the Street.</b></summary>
    /// <remarks>
    /// The Street stands alone off the lattice, so both its sides are open roadsides and nothing else
    /// competes for the ground.
    /// </remarks>
    [Theory]
    [InlineData("minimal.toml")]
    [InlineData("rowhouses.toml")]
    public void A_curved_streets_sides_carve_rotated_plots_that_do_not_overlap(string file)
    {
        var world = new World(1_000, Shipped(file));
        RoadGraph roads = world.Roads;
        int segment = roads.Segments.Rows.Resolve(roads.Segments.Create(
            roads.Nodes.Create(new Tiles(1_001), new Tiles(1_003)), roads.Nodes.Create(new Tiles(1_101), new Tiles(1_003)),
            new Tiles(100), RoadKind.Street, TravelMode.Any, TravelMode.Any));
        roads.Segments.Sagitta[segment] = SubTiles.FromTiles(new Tiles(12));
        roads.RebuildDerived();
        Assert.Equal(PermissionRefusal.None,
            world.PaintUsePermissions(new LandRectangle(960, 960, 180, 100), LotTable.Housing));

        Assert.True(LotSubdivider.Resubdivide(world).Created > 0);

        var lots = new List<int>();
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (world.Lots.Rows.IsLive(lot)) { lots.Add(lot); }
        }

        Assert.All(lots, lot => Assert.Equal(segment, world.Lots.FrontageOn(lot)));
        Assert.Equal([0, 1], lots.Select(lot => (int)world.Lots.Side[lot]).Distinct().Order());
        Assert.Contains(lots, lot => world.Lots.AxisNorthQ16[lot] != 0);
        Assert.All(lots, lot => Assert.NotEqual(0, world.Lots.Pattern[lot]));
        foreach (int a in lots)
        {
            Assert.DoesNotContain(lots, b => b != a && world.Lots.Parcel(a).Overlaps(world.Lots.Parcel(b)));
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
