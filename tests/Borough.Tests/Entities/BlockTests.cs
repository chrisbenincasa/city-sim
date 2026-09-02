using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// A block is a row, and it remembers what it was zoned for after its Lots are gone.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 1.</b> Before this table a block was a <c>(column, row)</c> index into
/// <see cref="StreetGrid"/> with no state, so a per-block decision had nowhere to live — which is why
/// five different subdivision patterns proposed in one sitting each turned into a world constant.
/// </para>
/// <para>
/// <b>The discharge these tests are written against is named in <c>LotSubdivider.Relot</c>'s own
/// remarks</b>: <em>"a block that was zoned and then lost every Lot has forgotten it was zoned, and a
/// Street run back through it yields nothing until the player zones again. That is a real limitation
/// and it is named here rather than hidden."</em>
/// </para>
/// </remarks>
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

    /// <summary>Every live Block's lattice position, for comparing an index against its rows.</summary>
    private static List<(int Column, int Row, ushort Zone)> Live(World world)
    {
        var found = new List<(int, int, ushort)>();

        for (int slot = 0; slot < world.Blocks.Rows.SlotCount; slot++)
        {
            if (world.Blocks.Rows.IsLive(slot))
            {
                found.Add((
                    world.Blocks.LatticeColumn[slot],
                    world.Blocks.LatticeRow[slot],
                    world.Blocks.Zone[slot]));
            }
        }

        return found;
    }

    /// <summary><b>A generated city carves Blocks</b>, which is the fixture the rest of this rests on.</summary>
    [Fact]
    public void A_generated_city_records_the_blocks_it_carved()
    {
        World world = Populated();

        Assert.NotEmpty(Live(world));

        // The index and the rows are two spellings of one fact, so they agree or one of them is a lie.
        Assert.Equal(world.Blocks.Rows.LiveCount, world.BlockIndex.Count);

        foreach ((int column, int row, _) in Live(world))
        {
            Assert.NotEqual(BlockResidency.NotResident, world.BlockIndex.Slot(column, row));
        }
    }

    /// <summary>
    /// 🔴 <b>Zoning land with no Street on any face records the block anyway.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the whole discharge.</b> <c>02 §2.2</c>'s third rule stands untouched — the land
    /// yields <em>no Lots at all</em> — but the intent now survives it, so a Street laid later finds
    /// land that knows what it was painted for. Before this, the command was forgotten the instant it
    /// returned zero.
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

        Assert.Equal(BlockResidency.NotResident, world.BlockIndex.Slot(column, row));

        int carved = LotSubdivider.SubdivideBlock(world, column, row, LotTable.Housing);

        Assert.Equal(0, carved);

        int slot = world.BlockIndex.Slot(column, row);

        Assert.NotEqual(BlockResidency.NotResident, slot);
        Assert.Equal(LotTable.Housing, world.Blocks.Zone[slot]);
    }

    /// <summary><b>Re-zoning overwrites rather than accumulating</b>, because a Zone is the whole payload.</summary>
    [Fact]
    public void Re_zoning_a_block_replaces_its_permission_set()
    {
        World world = Populated();

        int blocks = world.Roads.Streets.Blocks;

        int first = world.ZoneBlock(blocks - 3, blocks - 3, LotTable.Housing);
        Assert.Equal(LotTable.Housing, world.Blocks.Zone[first]);

        int second = world.ZoneBlock(blocks - 3, blocks - 3, LotTable.Trade);

        // The SAME row, re-pointed. A second row for one lattice square is what BlockResidency.Occupy
        // refuses outright, and this is the path that would otherwise have produced one.
        Assert.Equal(first, second);
        Assert.Equal(LotTable.Trade, world.Blocks.Zone[second]);
        Assert.Equal(1, CountAt(world, blocks - 3, blocks - 3));
    }

    private static int CountAt(World world, int column, int row) =>
        Live(world).Count(block => block.Column == column && block.Row == row);

    /// <summary>
    /// <b>A block off the lattice gets no row and no throw</b>, which is the residency's boundary rule.
    /// </summary>
    [Fact]
    public void Zoning_off_the_lattice_records_nothing()
    {
        World world = Populated();

        int before = world.Blocks.Rows.LiveCount;

        Assert.Equal(Rows.NoSlot, world.ZoneBlock(-1, 0, LotTable.Housing));
        Assert.Equal(Rows.NoSlot, world.ZoneBlock(0, int.MaxValue, LotTable.Housing));

        Assert.Equal(before, world.Blocks.Rows.LiveCount);
    }

    /// <summary>
    /// 🔴 <b><c>RebuildDerived</c> reproduces the index exactly</b>, which is what makes it derived.
    /// </summary>
    /// <remarks>
    /// <b>The one test that would catch the index going stale on a load.</b> It is not
    /// <c>DerivedRebuildAuditTests</c>' business — that audits declared <em>columns</em>, and this is an
    /// array beside the table, exactly like the frontage claim mask. ⚠ <b>And it is the test that would
    /// fail if the residency were sized from a constant</b> rather than from
    /// <see cref="StreetGrid.Span"/>, because a lattice at a non-shipped <c>block_tiles</c> would index
    /// into the wrong length.
    /// </remarks>
    [Fact]
    public void The_block_index_is_reproduced_by_a_rebuild()
    {
        World world = Populated();

        List<(int, int, ushort)> before = Live(world);
        int count = world.BlockIndex.Count;

        world.RebuildDerived();

        Assert.Equal(count, world.BlockIndex.Count);
        Assert.Equal(before, Live(world));

        foreach ((int column, int row, _) in before)
        {
            int slot = world.BlockIndex.Slot(column, row);

            Assert.NotEqual(BlockResidency.NotResident, slot);
            Assert.Equal(column, world.Blocks.LatticeColumn[slot]);
            Assert.Equal(row, world.Blocks.LatticeRow[slot]);
        }
    }
}
