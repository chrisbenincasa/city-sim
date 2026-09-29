using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

public sealed class TradeFormTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x7EAD_ED00_0000_0004UL);

    [Theory]
    [InlineData(0, 0, BlockPattern.CarParkCentre)]
    [InlineData(1, 3, BlockPattern.CarParkCentre)]
    [InlineData(2, 3, BlockPattern.ShopHouseParade)]
    [InlineData(3, 3, BlockPattern.ShopHouseParade)]
    [InlineData(1, 1, BlockPattern.ShopHouseParade)]
    public void A_band_draws_from_its_tier(byte band, int bandCount, BlockPattern expected)
    {
        for (int column = 0; column < 4; column++)
        {
            Assert.Equal(expected, BlockPatterns.TradeForm(band, bandCount, Key, column, 0));
        }
    }

    [Fact]
    public void A_shop_house_carves_as_a_perimeter_block()
    {
        Assert.Equal(BlockPattern.Perimeter, BlockPatterns.CarveAs(BlockPattern.ShopHouseParade));
        Assert.Equal(BlockPattern.CarParkCentre, BlockPatterns.CarveAs(BlockPattern.CarParkCentre));
    }

    [Fact]
    public void Traded_raises_centres_and_shop_houses_at_world_creation()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "traded.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        // Trade blocks reach the outer band only once the city is large enough to have one.
        var world = new World(8_000, loaded.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero, 8_000);

        int centres = 0, shopHouses = 0, upstairs = 0, first = -1;

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.IsTradeCentre(building))
            {
                centres++;
                continue;
            }

            if (!world.IsShopHouse(building))
            {
                continue;
            }

            shopHouses++;

            var units = new List<int>();
            foreach (int unit in world.BuildingUnits.Walk(building))
            {
                units.Add(unit);
            }

            int only = Assert.Single(units);
            Assert.False(world.Units.IsVacant(only), $"shop-house {building} stands with its shop vacant.");
            Assert.Equal(0, world.Units.FirstStorey[only]);

            Assert.True(world.TryDeclaredHousing(world.Buildings.Kind[building], building, out int homes));
            Assert.InRange(world.Occupants.Length(building), 0, homes);
            upstairs += homes;
            first = first < 0 ? building : first;
        }

        Assert.True(centres > 0, "traded.toml raised no car-park centre.");
        Assert.True(shopHouses > 0, "traded.toml raised no shop-house.");
        Assert.True(upstairs > 0, "no shop-house has a home above its shop.");

        AssertHouseholdsTakeOnlyTheUpperFloors(world, first);
    }

    private static void AssertHouseholdsTakeOnlyTheUpperFloors(World world, int shopHouse)
    {
        Assert.True(world.TryDeclaredHousing(world.Buildings.Kind[shopHouse], shopHouse, out int homes));
        Handle<Building> building = world.Buildings.Rows.At(shopHouse);

        while (world.HasRoomForHousehold(shopHouse))
        {
            world.CreateHousehold(building, lifeStage: 0);
        }

        Assert.Equal(homes, world.Occupants.Length(shopHouse));
        Assert.Equal(1, world.BuildingBusinesses.Length(shopHouse));
    }
}
