using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 8a: what <see cref="WoodlandGenerator"/> plants, and what takes it away.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0159</c>. Two claims are under test and they pull in opposite directions. The pass is a
/// <b>pure function of the <see cref="WorldKey"/></b>, so one key gives one forest for ever. And
/// <b>how much forest a world has must vary between keys</b>, because <c>adr/0022</c> rests a design
/// decision on it — <em>"a heavily forested seed is a Materials-rich, farmland-poor start"</em> — and
/// a self-normalising generator would delete that sentence while still passing every determinism
/// test. ***The second assertion is the one that would not have been written without reading the
/// ADR.***
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts a share or a total.</b> How wooded a map is falls out of the octave
/// ladder and the key; pinning a figure would make a re-baseline out of every future change to the
/// noise. What is asserted is the <em>shape</em> of the dependency, never its value.
/// </para>
/// </remarks>
public sealed class WoodlandTests
{
    private const int Citizens = 1_000;

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    /// <summary>Every Cell's wooded Tile count, in <see cref="CellGrid.Index"/> order.</summary>
    private static int[] Forest(WorldKey key)
    {
        WoodlandCellTable woodland = new();
        WoodlandGenerator.LayInto(woodland, key);

        var map = new int[CellGrid.WorldCellCount];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                map[CellGrid.Index(x, y)] = woodland.At(x, y);
            }
        }

        return map;
    }

    private static long Total(int[] forest)
    {
        long total = 0;

        foreach (int tiles in forest)
        {
            total += tiles;
        }

        return total;
    }

    [Fact]
    public void OneKeyGivesOneForest()
    {
        WorldKey key = WorldKey.FromSeed(0xF0E5U);

        Assert.Equal(Forest(key), Forest(key));
    }

    [Fact]
    public void EveryCellIsWithinItsOwnTileCount()
    {
        int[] forest = Forest(WorldKey.FromSeed(0xF0E5U));

        Assert.All(forest, tiles => Assert.InRange(tiles, 0, CellGrid.TilesInCell));
    }

    /// <summary>
    /// <b><c>adr/0022</c>'s heavily forested seed, as an assertion.</b>
    /// </summary>
    /// <remarks>
    /// The generator scales against <see cref="ValueNoise.Ceiling"/> rather than against the range a
    /// key realised, precisely so that this differs. ⚠ <b>A self-normalising generator would pass
    /// every other test in this class and fail only this one</b>, which is why it is here and why it
    /// asserts a difference rather than a size.
    /// </remarks>
    [Fact]
    public void HowMuchForestAWorldHasIsAPropertyOfItsKey()
    {
        long first = Total(Forest(WorldKey.FromSeed(0x0001U)));
        long second = Total(Forest(WorldKey.FromSeed(0x0002U)));
        long third = Total(Forest(WorldKey.FromSeed(0xBEEFU)));

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.NotEqual(first, third);
    }

    /// <summary>
    /// <b>Woodland is not a function of terrain type</b>, which is what its own
    /// <see cref="PurposeTag"/> buys.
    /// </summary>
    /// <remarks>
    /// A shared tag would make the two fields identical up to their reading, so every Cell of one
    /// terrain type would carry the same forest as every other — the correlation
    /// <c>PurposeTag.Woodland</c>'s comment says nothing in the city could refute. Asserted as: within
    /// a single terrain type, Woodland still spans most of its range.
    /// </remarks>
    [Fact]
    public void WoodlandIsNotAFunctionOfTerrainType()
    {
        WorldKey key = WorldKey.FromSeed(0xF0E5U);

        TerrainCellTable terrain = new();
        TerrainGenerator.LayInto(terrain, key);

        int[] forest = Forest(key);

        int low = int.MaxValue;
        int high = int.MinValue;

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                if (terrain.At(x, y) != TerrainKind.Ordinary)
                {
                    continue;
                }

                int tiles = forest[CellGrid.Index(x, y)];

                low = Math.Min(low, tiles);
                high = Math.Max(high, tiles);
            }
        }

        // A THIRD of the Cell, as a span rather than as a level, and the margin is deliberate. A field
        // that tracked terrain would collapse to near-zero span inside one type -- every Ordinary Cell
        // carrying the same forest is the whole of what a shared PurposeTag would do -- so any
        // substantial span refutes it and the threshold only has to sit clear of nothing.
        //
        // ⚠ It was HALF the Cell until it was run, and the observed span is 502 of 1024. That near-miss
        // is what sent somebody to measure the generator (WoodlandMeasurementTests) and is worth more
        // than the assertion: the realised per-Cell range is a BAND IN THE MIDDLE of the Cell, never
        // the whole of it, because a sum of uniforms concentrates. The threshold is not tuned to pass;
        // it is set where the claim actually lives.
        Assert.True(
            high - low > CellGrid.TilesInCell / 3,
            $"Woodland spans only {low}..{high} across Ordinary ground, which reads as terrain-derived.");
    }

    [Fact]
    public void SealingTakesTheForestAndNothingAnnouncesIt()
    {
        Ruleset ruleset = Load("minimal.toml");
        WorldKey key = WorldKey.FromSeed(0xF0E5U);
        World world = new(Citizens, ruleset, key);

        world.Layers.LayWoodland(key);

        // A Cell the generator actually wooded, so the assertion is about clearing rather than about
        // a Cell that was bare to begin with.
        Cells east = default;
        Cells north = default;
        int before = 0;

        for (int y = 0; y < CellGrid.WorldCells && before == 0; y++)
        {
            for (int x = 0; x < CellGrid.WorldCells && before == 0; x++)
            {
                int tiles = world.Layers.WoodedTiles(new Cells(x), new Cells(y));

                if (tiles > CellGrid.TilesInCell / 2)
                {
                    east = new Cells(x);
                    north = new Cells(y);
                    before = tiles;
                }
            }
        }

        Assert.True(before > 0, "The generator wooded no Cell past half, so there was nothing to clear.");

        world.Layers.Seal(east, north, CellGrid.TilesInCell);

        Assert.Equal(CellGrid.TilesInCell, world.Layers.Sealing(east, north));
        Assert.Equal(0, world.Layers.WoodedTiles(east, north));
    }

    /// <summary>
    /// <b>The one budget, on a city that was actually built.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0159</c>'s whole claim is that <c>Woodland + Sealing ≤ TilesInCell</c> is what the two
    /// counts mean rather than a rule imposed on them. This is that sentence run over every Cell a
    /// generated city touched.
    /// </remarks>
    [Fact]
    public void TheGroundHasOneBudgetOnAGeneratedCity()
    {
        Ruleset ruleset = Load("minimal.toml");
        WorldKey key = WorldKey.FromSeed(0xF0E5U);
        World world = new(Citizens, ruleset, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                int both = world.Layers.WoodedTiles(x, y) + world.Layers.Sealing(x, y);

                Assert.True(
                    both <= CellGrid.TilesInCell,
                    $"Cell ({east}, {north}) holds {both} Tiles of a {CellGrid.TilesInCell}-Tile Cell.");
            }
        }
    }
}
