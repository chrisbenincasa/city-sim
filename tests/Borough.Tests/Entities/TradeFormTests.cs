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
    [InlineData(3, 3, new[] { BlockPattern.ShopHouseParade, BlockPattern.HighStreetBlock })]
    [InlineData(1, 1, new[] { BlockPattern.ShopHouseParade, BlockPattern.HighStreetBlock })]
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
    [InlineData(BlockPattern.HighStreetBlock, BlockPattern.Perimeter)]
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_centre_gives_up_a_side_strip_of_pads_that_tiles_its_block(bool east)
    {
        BlockGround ground = BlockGround.Square(32);
        const int half = 1;
        var centre = new Parcel(BlockFace.South, StreetSide.Left, new Tiles(2),
            new Tiles(0), new Tiles(0), new Tiles(32), new Tiles(32));
        Span<Parcel> parcels = stackalloc Parcel[4];

        int count = PadSite.Split(centre, ground, half, east, parcels);

        Assert.Equal(1 + PadSite.Pads, count);
        Assert.Equal(32 - PadSite.DepthTiles - half, parcels[0].Wide.Raw);
        Assert.Equal(32 * 32, parcels[..count].ToArray().Sum(parcel => parcel.AreaTiles));
        Assert.InRange(parcels[0].Offset.Raw, parcels[0].East.Raw, parcels[0].East.Raw + parcels[0].Wide.Raw - 1);

        for (int pad = 1; pad < count; pad++)
        {
            Parcel each = parcels[pad];
            Assert.Equal(east ? BlockFace.East : BlockFace.West, each.Face);
            Assert.Equal(east ? 32 - each.Wide.Raw : 0, each.East.Raw);
            Assert.InRange(each.Offset.Raw, each.North.Raw, each.North.Raw + each.Deep.Raw - 1);

            var foot = PadSite.Footprint(each, ground, half);
            Assert.Equal(PadSite.DepthTiles - PadSite.ForecourtTiles, foot.Wide.Raw);
            Assert.Equal(east ? 32 - half - PadSite.DepthTiles : half + PadSite.ForecourtTiles, foot.East.Raw);
            Assert.True(foot.Deep.Raw > 0);

            (int carEast, int carNorth, int along, int toward) =
                PadSite.CarPark(foot.East.Raw, foot.North.Raw, foot.Wide.Raw, each.Face);
            Assert.Equal(east ? foot.East.Raw : half, carEast);
            Assert.Equal(PadSite.DepthTiles, along);
            Assert.Equal(foot.North.Raw, carNorth + toward);
        }
    }

    [Fact]
    public void Traded_raises_pad_sites_beside_its_centres_at_world_creation()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "traded.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        var world = new World(8_000, loaded.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero, 8_000);

        int pads = 0;

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.IsPadSite(building))
            {
                continue;
            }

            pads++;
            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
            Assert.Equal(1, world.Lots.Storeys[lot]);

            var units = new List<int>();
            foreach (int unit in world.BuildingUnits.Walk(building))
            {
                units.Add(unit);
            }

            int only = Assert.Single(units);
            Assert.False(world.Units.IsVacant(only), $"pad {building} stands with its shop vacant.");
            Assert.Equal(world.Lots.FootprintTiles(lot), world.Units.Floor[only]);

            Assert.True(world.TryDeclaredHousing(world.Buildings.Kind[building], building, out int homes));
            Assert.Equal(0, homes);
            Assert.False(world.HasRoomForHousehold(building));

            Assert.True(world.TryDeclaredParking(world.Buildings.Kind[building], building, out int spaces));
            Assert.Equal(PadSite.Stalls(world.Lots.FootprintWide[lot].Raw, world.Rules.Parking.Stalls).Stalls, spaces);
            Assert.True(spaces > 0, $"pad {building} has no stalls.");
        }

        Assert.True(pads > 0, "traded.toml raised no pad site.");
    }

    [Fact]
    public void Traded_raises_every_kept_form_in_its_bands_in_one_city()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "traded.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        const int citizens = 30_000;
        WorldKey key = WorldKey.FromSeed(0x7EAD_ED00_0000_0001UL);
        var world = new World(citizens, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero, citizens);

        // traded.toml's five bands land on rungs 0, 1, 2, 3 and 5. A mixed block reads as band 0.
        BlockPattern[] low = [BlockPattern.CarParkCentre, BlockPattern.PadSite];
        BlockPattern[] middle = [BlockPattern.ShopHouseParade, BlockPattern.Supermarket, BlockPattern.DeckedSupermarket];
        BlockPattern[] high = [BlockPattern.ShopHouseParade, BlockPattern.HighStreetBlock];
        BlockPattern[][] tierOfBand = [low, low, low, middle, middle, high];

        var raised = new HashSet<BlockPattern>[3] { [], [], [] };

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.Buildings.Rows.IsLive(building)
                || !world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot))
            {
                continue;
            }

            BlockPattern form = world.Lots.PatternOf(lot);
            if (!low.Concat(middle).Concat(high).Contains(form))
            {
                continue;
            }

            LandPermissionSummary ground = world.LandPermissions.Summary(world.LotGround(lot));
            int band = ground.MixedIntensity ? 0 : ground.Band;
            BlockPattern[] tier = tierOfBand[band];
            Assert.True(tier.Contains(form), $"band {band} raised a {form} on Lot {lot}.");
            raised[tier == low ? 0 : tier == middle ? 1 : 2].Add(form);
        }

        Assert.Equal(low.ToHashSet(), raised[0]);
        Assert.Equal(middle.ToHashSet(), raised[1]);
        Assert.Equal(high.ToHashSet(), raised[2]);
    }

    [Fact]
    public void Traded_raises_high_street_blocks_with_a_department_store_on_the_south_face()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "traded.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        var world = new World(8_000, loaded.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero, 8_000);

        int stores = 0, shopHouses = 0;

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.Buildings.Rows.IsLive(building)
                || !world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot)
                || world.Lots.PatternOf(lot) != BlockPattern.HighStreetBlock)
            {
                continue;
            }

            if (world.IsShopHouse(building))
            {
                Assert.False(world.IsDepartmentStore(building));
                shopHouses++;
                continue;
            }

            Assert.True(world.IsDepartmentStore(building), $"high-street Building {building} is neither form.");
            AssertDepartmentStore(world, building, lot);
            stores++;
        }

        Assert.True(stores > 0, "traded.toml raised no department store.");
        Assert.True(shopHouses > 0, "no high-street block has shop-houses on its other faces.");
    }

    private static void AssertDepartmentStore(World world, int building, int lot)
    {
        int wide = world.Lots.FootprintWide[lot].Raw;
        int deep = world.Lots.FootprintDeep[lot].Raw;
        byte storeys = world.Lots.Storeys[lot];
        Assert.True(wide > 2 * DepartmentStore.CornerTiles, $"store {building} is only {wide} Tiles wide.");
        Assert.Equal(world.Lots.ParcelDeep[lot].Raw, deep);
        Assert.Equal(world.Lots.ParcelNorth[lot], world.Lots.FootprintNorth[lot]);

        var units = new List<int>();
        foreach (int unit in world.BuildingUnits.Walk(building))
        {
            units.Add(unit);
            Assert.Equal(storeys, world.Units.Storeys[unit]);
            Assert.Equal(0, world.Units.FirstStorey[unit]);
            Assert.Equal((byte)BlockFace.South, world.Units.Side[unit]);
            Assert.False(world.Units.IsVacant(unit), $"store {building} stands with a Unit vacant.");
        }

        Assert.Equal(DepartmentStore.MaxUnits, units.Count);
        Assert.Equal(1, units.Count(unit => world.Units.Anchor[unit] != 0));
        Assert.Equal(wide * deep * storeys, units.Sum(unit => world.Units.Floor[unit]));

        Assert.True(world.TryDeclaredHousing(world.Buildings.Kind[building], building, out int homes));
        Assert.Equal(0, homes);
        Assert.False(world.HasRoomForHousehold(building));
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
