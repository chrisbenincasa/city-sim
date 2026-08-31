using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 9: the Hazard Region — which Cells a flood reaches, and how deep.
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Hazard Region, <c>01 §5.2</c>, <c>adr/0157</c>. The claims under test are the
/// derivation's: <b>a flood reaches dry ground below the flood level and nothing else</b>; <b>the
/// depth is the level minus the ground, so it is always positive</b>; <b>a world that omits the key
/// has no rows rather than rows of depth zero</b>; and <b>one key gives one Hazard Region for
/// ever</b>.
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts a share, and <c>adr/0157</c> is why that matters more than usual.</b>
/// That ADR's own revisit trigger is *floodplain depth turns out not to be sparse* — so how much of a
/// map floods is a figure the storage decision rests on, and it lives in
/// <see cref="FloodplainMeasurementTests"/> where re-running it re-derives a constant. Pinning it here
/// would make a re-baseline out of every future change to the noise.
/// </para>
/// <para>
/// ✅ <b>Something fires on a Hazard Region as of <c>plans/0045</c> row 12</b>, and this paragraph
/// said the opposite from milestone 24 until then. What is asserted here is still only the
/// DERIVATION — where a flood could go — and the mechanism that goes there lives in
/// <see cref="DisasterTests"/>. ⚠ <b>The split is worth keeping</b>: a test that generated a world
/// AND ran a flood over it would fail for two unrelated reasons and name neither.
/// </para>
/// </remarks>
public sealed class FloodplainTests
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

    private static int Live<T>(Rows<T> rows)
        where T : unmanaged
    {
        int live = 0;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot)) { live++; }
        }

        return live;
    }

    /// <summary>
    /// ⚠ <b>No wet Cell is Hazard Region.</b> Ground already under water is not ground a player can
    /// build on and lose, and the whole point of the overlay is that it prices what you might site
    /// there.
    /// </summary>
    [Fact]
    public void A_wet_Cell_is_not_floodplain()
    {
        World world = Generated(Key);

        Assert.True(Live(world.Flood.Rows) > 0, "coastal.toml produced no floodplain at all");

        for (int slot = 0; slot < world.Flood.Rows.SlotCount; slot++)
        {
            if (!world.Flood.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.False(
                world.WaterInCells.IsWet(world.Flood.East[slot], world.Flood.North[slot]),
                $"the Cell at slot {slot} is both wet and floodplain");
        }
    }

    /// <summary>
    /// A depth is the flood level over the ground, so <b>it is always positive and never zero.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A depth of zero would be a Cell exactly at the flood level, which is not flooded</b> — it
    /// gets no row. That is <c>adr/0123</c> at row granularity: the absent case is an absence rather
    /// than a value, and <see cref="FloodCellTable.Create"/> refuses the zero outright.
    /// </remarks>
    [Fact]
    public void Every_depth_is_positive()
    {
        World world = Generated(Key);

        for (int slot = 0; slot < world.Flood.Rows.SlotCount; slot++)
        {
            if (!world.Flood.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.True(world.Flood.Depth[slot] > 0, $"slot {slot} has depth {world.Flood.Depth[slot]}");
        }
    }

    /// <summary>
    /// <b>Exactly the dry Cells below the flood level, and no others</b> — the derivation, restated
    /// against the height field the generator read.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It recomputes the level rather than reading one off the world, because the world stores
    /// no height</b> (<c>adr/0157</c>). The level is a percent of the range THIS world realised, so
    /// the test has to take the same two passes over the same field the generator did — which is also
    /// what makes it a check of the rule rather than of the code.
    /// </remarks>
    [Fact]
    public void The_floodplain_is_the_dry_ground_below_the_flood_level()
    {
        World world = Generated(Key);
        int[] height = ValueNoise.Field(Key, PurposeTag.TerrainType);

        int low = height[0];
        int high = height[0];

        for (int cell = 1; cell < height.Length; cell++)
        {
            if (height[cell] < low) { low = height[cell]; }
            if (height[cell] > high) { high = height[cell]; }
        }

        int level = low + IntegerMath.RoundDiv(
            (high - low) * world.Rules.Water.FloodLevelPercent, 100);

        var expected = new Dictionary<int, int>();

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            var east = new Cells(cell % CellGrid.WorldCells);
            var north = new Cells(IntegerMath.FloorDiv(cell, CellGrid.WorldCells));

            if (world.WaterInCells.IsWet(east, north) || height[cell] >= level)
            {
                continue;
            }

            expected[cell] = level - height[cell];
        }

        Assert.Equal(expected.Count, Live(world.Flood.Rows));

        for (int slot = 0; slot < world.Flood.Rows.SlotCount; slot++)
        {
            if (!world.Flood.Rows.IsLive(slot))
            {
                continue;
            }

            int cell = CellGrid.Index(world.Flood.East[slot], world.Flood.North[slot]);

            Assert.True(expected.TryGetValue(cell, out int depth), $"Cell {cell} should not flood");
            Assert.Equal(depth, world.Flood.Depth[slot]);
        }
    }

    /// <summary>
    /// ⚠ <b>A world that omits the key has NO ROWS, not rows of depth zero.</b> <c>adr/0123</c>, and a
    /// steep coast is a world rather than a gap.
    /// </summary>
    [Fact]
    public void A_world_that_states_no_flood_level_has_no_Hazard_Region()
    {
        Assert.Equal(0, Live(Generated(Key, "minimal.toml").Flood.Rows));
    }

    /// <summary>One key and one flood level give one Hazard Region for ever.</summary>
    [Fact]
    public void One_key_gives_one_Hazard_Region()
    {
        Assert.Equal(
            Generated(Key).Flood.Fingerprint(),
            Generated(Key).Flood.Fingerprint());

        Assert.NotEqual(
            Generated(Key).Flood.Fingerprint(),
            Generated(WorldKey.FromSeed(770_413)).Flood.Fingerprint());
    }

    /// <summary>
    /// The Hazard Region survives a save, and has to be saved rather than derived to do it.
    /// </summary>
    [Fact]
    public void The_Hazard_Region_survives_a_save()
    {
        World world = Generated(Key);
        Ruleset rules = Load("coastal.toml");

        var file = new MemorySave();
        SaveFile.Write(world, 0x0BAD_F00D_0BAD_F00DUL, file);

        World loaded = SaveFile.Read(file, rules, out _);

        Assert.Equal(world.Flood.Fingerprint(), loaded.Flood.Fingerprint());
    }
}
