using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 6a: where <see cref="WaterGenerator"/> puts water, and which way it drains.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0034</c>, <c>adr/0157</c>, <c>adr/0160</c>. The claims under test are the graph's, not the
/// coastline's: <b>one key and one sea level give one map for ever</b>; <b>the water graph is
/// acyclic and every chain terminates off the map</b>; and <b>a Ruleset with no <c>[water]</c> has no
/// water at all</b>.
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts a share, a body count or a coastline shape.</b> How much of a map is wet
/// falls out of the key's own field and the level together, so pinning a figure would make a
/// re-baseline out of every future change to the noise. The <em>measurement</em> lives in
/// <see cref="WaterMeasurementTests"/> and is an instrument, because re-running it re-derives a
/// constant rather than asking whether the city is correct.
/// </para>
/// <para>
/// ⚠ <b>The acyclicity test is the one that would not have been written without reading the
/// generator.</b> A downstream edge is found by walking downhill from a body's lowest rim, and
/// "downhill" is only obviously acyclic until two bodies sit in one basin. It is asserted rather than
/// argued.
/// </para>
/// </remarks>
public sealed class WaterTests
{
    private const int Citizens = 1_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(24_006);

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static World Generated(WorldKey key, string file = "coastal.toml")
    {
        World world = new(Citizens, Load(file), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return world;
    }

    /// <summary>How many rows a table holds, walked in index order.</summary>
    private static int Live<T>(Rows<T> rows)
        where T : unmanaged
    {
        int live = 0;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot))
            {
                live++;
            }
        }

        return live;
    }

    /// <summary>The same key lays the same water, down to which body drains into which.</summary>
    [Fact]
    public void One_key_gives_one_sea()
    {
        World first = Generated(Key);
        World again = Generated(Key);

        Assert.Equal(Live(first.Water.Rows), Live(again.Water.Rows));
        Assert.Equal(Live(first.WaterCells.Rows), Live(again.WaterCells.Rows));
        Assert.Equal(first.WaterInCells.Count, again.WaterInCells.Count);

        for (int slot = 0; slot < first.WaterCells.Rows.SlotCount; slot++)
        {
            if (!first.WaterCells.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.Equal(first.WaterCells.East[slot], again.WaterCells.East[slot]);
            Assert.Equal(first.WaterCells.North[slot], again.WaterCells.North[slot]);
        }
    }

    /// <summary>
    /// A Ruleset that states no <c>[water]</c> has none — and that is a world, not a hole.
    /// </summary>
    [Fact]
    public void A_ruleset_with_no_water_table_has_no_water()
    {
        World world = Generated(Key, "minimal.toml");

        Assert.Equal(0, Live(world.Water.Rows));
        Assert.Equal(0, Live(world.WaterCells.Rows));
        Assert.Equal(0, world.WaterInCells.Count);
    }

    /// <summary>A world with <c>[water]</c> has some, on any key.</summary>
    /// <remarks>
    /// <b>Every key has a coast, and that is what the realised-range reading buys.</b> A level fixed
    /// against the theoretical ceiling would drown one key and leave the next dry; this asserts the
    /// property that choice exists for, rather than the amount it produces.
    /// </remarks>
    [Theory]
    [InlineData(1UL)]
    [InlineData(24_006UL)]
    [InlineData(770_413UL)]
    [InlineData(ulong.MaxValue)]
    public void Every_key_has_a_coast(ulong seed)
    {
        World world = Generated(WorldKey.FromSeed(seed));

        Assert.True(
            Live(world.Water.Rows) > 0,
            $"seed {seed} laid no Water Body at all. The sea level is a percent of the range THIS "
            + "world realised, so every key should have a coast -- a key with none means the "
            + "self-normalising reading has stopped being self-normalising.");
    }

    /// <summary>Every wet Cell is indexed, and every indexed Cell is wet.</summary>
    [Fact]
    public void The_index_and_the_rows_agree()
    {
        World world = Generated(Key);

        Assert.Equal(Live(world.WaterCells.Rows), world.WaterInCells.Count);

        for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
        {
            if (!world.WaterCells.Rows.IsLive(slot))
            {
                continue;
            }

            Cells east = world.WaterCells.East[slot];
            Cells north = world.WaterCells.North[slot];

            Assert.True(world.WaterInCells.IsWet(east, north));
            Assert.Equal(slot, world.WaterInCells.Slot(east, north));
        }
    }

    /// <summary>
    /// The index rebuilds from the saved coordinates alone — what a load does.
    /// </summary>
    [Fact]
    public void The_index_rebuilds_from_the_rows()
    {
        World world = Generated(Key);

        WaterResidency rebuilt = new();
        rebuilt.Rebuild(world.WaterCells);

        Assert.Equal(world.WaterInCells.Count, rebuilt.Count);

        for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
        {
            if (!world.WaterCells.Rows.IsLive(slot))
            {
                continue;
            }

            Cells east = world.WaterCells.East[slot];
            Cells north = world.WaterCells.North[slot];

            Assert.Equal(world.WaterInCells.Slot(east, north), rebuilt.Slot(east, north));
        }
    }

    /// <summary>
    /// Every chain of outflows terminates off the map, and none of them is a cycle.
    /// </summary>
    /// <remarks>
    /// <b>Walked with a step budget rather than a visited set</b>, because a cycle is exactly what
    /// this is looking for and a budget catches one without a structure that would hide it. The
    /// budget is the body count: a path longer than that has visited some body twice.
    /// </remarks>
    [Fact]
    public void Every_outflow_reaches_the_map_edge()
    {
        World world = Generated(Key);
        int bodies = Live(world.Water.Rows);

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot))
            {
                continue;
            }

            int at = slot;
            int steps = 0;

            while (!world.Water.Downstream[at].IsNone)
            {
                at = world.Water.Rows.Resolve(world.Water.Downstream[at]);
                steps++;

                Assert.True(
                    steps <= bodies,
                    $"the outflow chain from body {slot} is longer than the {bodies} bodies in the "
                    + "world, so it has visited one twice. The water graph has a cycle in it, and a "
                    + "cycle means a Water Body eventually drains into itself.");
            }
        }
    }

    /// <summary>No body drains into itself, which the table refuses outright.</summary>
    [Fact]
    public void A_body_cannot_drain_into_itself()
    {
        // A generated world's own table, because WaterBodyTable now holds a handle into the Bin
        // table and building one standalone means building the Building and Lot tables under it.
        WaterBodyTable bodies = Generated(Key).Water;
        Handle<WaterBody> body = bodies.Create();

        Assert.Throws<ArgumentException>(() => bodies.DrainsInto(body, body));
    }

    /// <summary>A sea level outside 1–99 is refused, at both ends and for different reasons.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    [InlineData(101)]
    public void A_sea_level_outside_the_band_is_refused(int percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WaterRuleset.From(percent));
    }

    /// <summary>The shipped coastal Ruleset loads, and it is the only file that states a sea.</summary>
    [Fact]
    public void The_shipped_coastal_ruleset_states_a_sea_and_the_others_do_not()
    {
        Assert.True(Load("coastal.toml").Water.Stated);
        Assert.False(Load("minimal.toml").Water.Stated);
        Assert.False(Load("varied.toml").Water.Stated);
    }
}
