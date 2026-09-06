using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

public sealed class StreetClearanceTests
{
    [Theory]
    [InlineData(4, 7, 1)]
    [InlineData(32, 48, 1)]
    [InlineData(32, 48, 2)]
    public void Every_pattern_reserves_street_ground_even_with_zero_setback(int width, int depth, int reserve)
    {
        var ground = new BlockGround(2, 3, 64, 96, width, depth);
        var rules = new LotRuleset(3, 0, StreetHalfWidthTiles: reserve);
        var parcels = new Parcel[BlockPatterns.Ceiling(3)];
        for (int pattern = 0; pattern < BlockPatterns.Count; pattern++)
        {
            int count = BlockPatterns.Carve(WorldKey.FromSeed(1), (BlockPattern)pattern, ground, 3, parcels);
            for (int i = 0; i < count; i++)
            {
                var foot = rules.Footprint(WorldKey.FromSeed(1), parcels[i], ground);
                if (foot.Wide.Raw == 0 || foot.Deep.Raw == 0) continue;
                Assert.InRange(foot.East.Raw, ground.East + reserve,
                    ground.East + width - reserve - foot.Wide.Raw);
                Assert.InRange(foot.North.Raw, ground.North + reserve,
                    ground.North + depth - reserve - foot.Deep.Raw);
            }
        }
    }

    [Fact]
    public void Street_width_loads_and_cannot_change_under_existing_buildings()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        var original = RulesetLoader.Parse(text, "original").Ruleset!;
        var wider = RulesetLoader.Parse(text.Replace("street_half_width_tiles = 1",
            "street_half_width_tiles = 2", StringComparison.Ordinal), "wider").Ruleset!;
        Assert.Equal(2, wider.Lots.StreetHalfWidthTiles);
        var world = new World(100, original);
        Assert.Throws<NotSupportedException>(() => world.Adopt(wider, 2, Ticks.Zero, WorldKey.FromSeed(1)));
        Assert.Same(original, world.Rules);
        Assert.Null(RulesetLoader.Parse(text.Replace("street_half_width_tiles = 1",
            "street_half_width_tiles = 0", StringComparison.Ordinal), "invalid").Ruleset);
    }

    [Theory]
    [InlineData("minimal.toml", 0)]
    [InlineData("minimal.toml", 1)]
    [InlineData("congested.toml", 1)]
    public void Footprints_clear_all_four_street_edges_before_and_after_rebuild(string file, int seed)
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));
        var world = new World(4_000, loaded.Ruleset!);
        SyntheticCity.PopulateInto(world, WorldKey.FromSeed((ulong)seed), Ticks.Zero);
        Check(world);
        world.RebuildDerived();
        Check(world);
        world.RebuildParcels();
        Check(world);
    }

    private static void Check(World world)
    {
        int checkedLots = 0;
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            var lots = world.Lots;
            if (!lots.Rows.IsLive(slot) || lots.FootprintTiles(slot) == 0) continue;
            Assert.True(Frontage.BlockOf(world.Roads.Streets, lots.East[slot], lots.North[slot],
                (StreetSide)lots.Side[slot], out int column, out int row, out _));
            var ground = BlockGround.At(world.Roads.Streets.Lattice, column, row);
            Assert.InRange(lots.FootprintEast[slot].Raw, ground.East + 1,
                ground.East + ground.Wide - 1 - lots.FootprintWide[slot].Raw);
            Assert.InRange(lots.FootprintNorth[slot].Raw, ground.North + 1,
                ground.North + ground.Deep - 1 - lots.FootprintDeep[slot].Raw);
            checkedLots++;
        }
        Assert.True(checkedLots > 0);
    }
}
