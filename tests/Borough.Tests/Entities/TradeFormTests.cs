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
    [InlineData(0, 0, new[] { BlockPattern.CarParkCentre })]
    [InlineData(1, 3, new[] { BlockPattern.CarParkCentre })]
    [InlineData(2, 5, new[] { BlockPattern.CarParkCentre })]
    [InlineData(2, 3, new[] { BlockPattern.ShopHouseParade, BlockPattern.Supermarket })]
    [InlineData(4, 5, new[] { BlockPattern.ShopHouseParade, BlockPattern.DeckedSupermarket })]
    [InlineData(3, 3, new[] { BlockPattern.ShopHouseParade })]
    [InlineData(1, 1, new[] { BlockPattern.ShopHouseParade })]
    public void A_band_draws_every_form_of_its_tier_and_nothing_else(
        byte band, int bandCount, BlockPattern[] tier)
    {
        var drawn = new HashSet<BlockPattern>();

        for (int column = 0; column < 64; column++)
        {
            drawn.Add(BlockPatterns.TradeForm(band, bandCount, Key, column, 0));
        }

        Assert.Equal(tier.ToHashSet(), drawn);
    }

    [Theory]
    [InlineData(BlockPattern.ShopHouseParade, BlockPattern.Perimeter)]
    [InlineData(BlockPattern.CarParkCentre, BlockPattern.CarParkCentre)]
    [InlineData(BlockPattern.Supermarket, BlockPattern.CarParkCentre)]
    [InlineData(BlockPattern.DeckedSupermarket, BlockPattern.CarParkCentre)]
    public void A_trade_form_carves_as_its_housing_pattern(BlockPattern form, BlockPattern carve)
    {
        Assert.Equal(carve, BlockPatterns.CarveAs(form));
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

    [Fact]
    public void Traded_raises_supermarkets_with_surface_and_deck_parking_at_world_creation()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "traded.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        // The middle bands hold trade blocks only in a city this large, and this seed draws both
        // parking forms there.
        const int citizens = 30_000;
        WorldKey key = WorldKey.FromSeed(0x7EAD_ED00_0000_0001UL);
        var world = new World(citizens, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero, citizens);

        int surface = 0, decked = 0;

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.IsSupermarket(building))
            {
                AssertSupermarket(world, building, ref surface, ref decked);
            }
        }

        Assert.True(surface > 0, "traded.toml raised no supermarket with surface parking.");
        Assert.True(decked > 0, "traded.toml raised no decked supermarket.");
    }

    private static void AssertSupermarket(World world, int building, ref int surface, ref int decked)
    {
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
        BlockPattern form = world.Lots.PatternOf(lot);
        if (form == BlockPattern.DeckedSupermarket) { decked++; } else { surface++; }

        var units = new List<int>();
        foreach (int unit in world.BuildingUnits.Walk(building))
        {
            units.Add(unit);
        }

        int only = Assert.Single(units);
        Assert.True(world.Units.Anchor[only] != 0, $"supermarket {building}'s one Unit is not its anchor.");
        Assert.False(world.Units.IsVacant(only), $"supermarket {building} stands with its store vacant.");
        Assert.Equal((byte)BlockFace.South, world.Units.Side[only]);
        Assert.Equal(1, world.Lots.Storeys[lot]);

        Assert.True(world.TryDeclaredHousing(world.Buildings.Kind[building], building, out int homes));
        Assert.Equal(0, homes);
        Assert.False(world.HasRoomForHousehold(building));

        int level = TownSupermarket.Stalls(
            world.Lots.ParcelNorth[lot].Raw, world.Lots.ParcelDeep[lot].Raw, world.Lots.FootprintNorth[lot].Raw,
            world.Lots.FootprintWide[lot].Raw, world.Lots.FootprintDeep[lot].Raw,
            world.Rules.Lots.StreetHalfWidthTiles, world.Rules.Parking.Stalls).Stalls;
        Assert.True(level > 0, $"supermarket {building} has no room for a stall.");
        Assert.True(world.TryDeclaredParking(world.Buildings.Kind[building], building, out int spaces));
        Assert.Equal(level * TownSupermarket.Levels(form), spaces);
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
