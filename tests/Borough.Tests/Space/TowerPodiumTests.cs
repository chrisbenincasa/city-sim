using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>A Tower's podium is drawn per Lot from the Ruleset's range, and the shaft pays for it.</summary>
public sealed class TowerPodiumTests
{
    private const int BlockTiles = 32;
    private const int LotsPerSegment = 5;
    private const int StoreysPerRung = 3;

    [Fact]
    public void A_podium_partitions_the_tower_it_is_given()
    {
        BuildingPlan.TowerForm form = BuildingPlan.Tower(31, 29, 18, podiumStoreys: 6);

        Assert.Equal(6, form.PodiumStoreys);
        Assert.Equal(12, form.ShaftStoreys);
        Assert.Equal(
            (31 * 29 * 6) + (15 * 14 * 12),
            BuildingPlan.FloorTiles(BlockPattern.Tower, 31, 29, 18, podiumStoreys: 6));
    }

    [Fact]
    public void A_taller_podium_buys_a_shorter_shaft_at_the_same_plot_ratio()
    {
        int low = BlockPatterns.Storeys(BlockPattern.Tower, BlockTiles, LotsPerSegment, StoreysPerRung, 2);
        int high = BlockPatterns.Storeys(BlockPattern.Tower, BlockTiles, LotsPerSegment, StoreysPerRung, 8);
        int shaft = (BlockTiles / 2) * (BlockTiles / 2);

        Assert.True(high < low, $"{high} storeys on an 8-storey podium, {low} on a 2-storey one");
        int lowFloor = BuildingPlan.FloorTiles(BlockPattern.Tower, BlockTiles, BlockTiles, low, 2);
        int highFloor = BuildingPlan.FloorTiles(BlockPattern.Tower, BlockTiles, BlockTiles, high, 8);
        Assert.InRange(highFloor - lowFloor, -shaft, shaft);
    }

    [Fact]
    public void A_ruleset_without_a_range_keeps_the_two_storey_podium()
    {
        var rules = new LotRuleset(LotsPerSegment, 1);
        for (int east = 0; east < 64; east += 7)
        {
            Assert.Equal(BuildingPlan.TowerPodiumStoreys, rules.PodiumOn(WorldKey.FromSeed(3), new Tiles(east), new Tiles(east * 3)));
        }
    }

    [Fact]
    public void A_range_draws_every_podium_within_it_and_varies_by_parcel()
    {
        var rules = new LotRuleset(LotsPerSegment, 1, MinTowerPodiumStoreys: 2, MaxTowerPodiumStoreys: 8);
        var seen = new bool[9];
        for (int east = 0; east < 400; east += 32)
        {
            for (int north = 0; north < 400; north += 32)
            {
                byte podium = rules.PodiumOn(WorldKey.FromSeed(3), new Tiles(east), new Tiles(north));
                Assert.InRange(podium, 2, 8);
                seen[podium] = true;
            }
        }

        Assert.True(seen[2], "no 2-storey podium drawn");
        Assert.True(seen[8], "no 8-storey podium drawn");
    }

    [Fact]
    public void The_range_loads_refuses_nonsense_and_is_fixed_at_world_creation()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        string ranged = text.Replace("[lots]\n", "[lots]\nmin_tower_podium_storeys = 3\nmax_tower_podium_storeys = 7\n",
            StringComparison.Ordinal);
        Assert.NotEqual(text, ranged);

        Ruleset original = RulesetLoader.Parse(text, "original").Ruleset!;
        Ruleset loaded = RulesetLoader.Parse(ranged, "ranged").Ruleset!;
        Assert.Equal((2, 2), (original.Lots.MinTowerPodiumStoreys, original.Lots.MaxTowerPodiumStoreys));
        Assert.Equal((3, 7), (loaded.Lots.MinTowerPodiumStoreys, loaded.Lots.MaxTowerPodiumStoreys));

        Assert.Null(RulesetLoader.Parse(ranged.Replace("max_tower_podium_storeys = 7", "max_tower_podium_storeys = 13",
            StringComparison.Ordinal), "too tall").Ruleset);
        Assert.Null(RulesetLoader.Parse(ranged.Replace("max_tower_podium_storeys = 7", "max_tower_podium_storeys = 2",
            StringComparison.Ordinal), "inverted").Ruleset);

        var world = new World(100, original);
        Assert.Throws<NotSupportedException>(() => world.Adopt(loaded, 2, Ticks.Zero, WorldKey.FromSeed(1)));
    }
}
