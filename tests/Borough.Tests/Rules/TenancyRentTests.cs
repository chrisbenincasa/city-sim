using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// One rent figure per tenancy — <c>plans/money-circulation.md</c> L7.
/// </summary>
/// <remarks>
/// <b>What a per-kind price could not say.</b> Rent was a property of the kind, so every dwelling of
/// one kind charged the same wherever it stood and the term cancelled across every candidate a
/// Household was shown. It is a rate per floor Tile now, charged on the floor a tenancy holds and
/// adjusted by the land value at its Lot, so the city's ground is in the price.
/// </remarks>
public sealed class TenancyRentTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x2026_10_17UL);

    /// <summary>Money per floor Tile per Day.</summary>
    private const int Rate = 10;

    /// <summary>Floor Tiles one tenancy holds here, which is also the capacity rate.</summary>
    private const int Share = 4;

    /// <summary>Tenancies per dwelling, so a pair of seekers fits in one of them.</summary>
    private const int Tenancies = 2;

    /// <summary>
    /// How far each dwelling is from the corner centre, in Tiles. <b>Equal for both</b>, and two
    /// Cells apart so land value can be raised under one of them alone.
    /// </summary>
    private const int Away = 64;

    private static Ruleset Rules(int reconsiderTicks = 0) => new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)
        {
            Houses = true,
            RentPerFloorTile = new Money(Rate),
        }],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: [])
    {
        // Sharp enough that the dearer candidate's softmax weight underflows to zero, so the choice
        // is the argmax and the test reads a preference rather than a draw.
        Placement = new PlacementRuleset(4, 4, 64, 0, reconsiderTicks)
        {
            MuPercent = 100_000,
            CentralityTilesPerUnit = 2048,
            RentPerUnit = 1,
        },
        Capacity = new CapacityRuleset(Share, 0, 0),
    };

    /// <summary>
    /// Two dwellings of one kind, equally central, with <paramref name="seeking"/> Households in the
    /// Pool and one whole unit of land value under the second.
    /// </summary>
    private static (World World, Simulation Simulation, int Cheap, int Dear) Pair(
        int seeking, int reconsiderTicks = 0)
    {
        var world = new World(1_000, Rules(reconsiderTicks), Key);
        var simulation = new Simulation(world, Key);

        // The Pool is only reachable by housing somebody and unplacing them, so the seekers come
        // out of a shelter that is then demolished.
        Handle<Lot> seed = world.Lots.Create(
            Tiles.Zero, Tiles.Zero, zone: 0,
            wide: new Tiles(Share * (seeking > 0 ? seeking : 1)), deep: new Tiles(1));
        Handle<Building> shelter = world.CreateBuilding(seed, 1, Ticks.Zero, Key);

        for (int i = 0; i < seeking; i++)
        {
            world.Unplace(world.CreateHousehold(shelter, lifeStage: 0));
        }

        world.DestroyBuilding(shelter, Ticks.Zero);

        int cheap = Dwelling(world, new Tiles(0), new Tiles(Away));
        int dear = Dwelling(world, new Tiles(Away), new Tiles(0));

        Raise(world, dear);

        return (world, simulation, cheap, dear);
    }

    private static int Dwelling(World world, Tiles east, Tiles north)
    {
        Handle<Lot> lot = world.Lots.Create(
            east, north, zone: 0, wide: new Tiles(Share * Tenancies), deep: new Tiles(1));

        return world.Buildings.Rows.Resolve(world.CreateBuilding(lot, 1, Ticks.Zero, Key));
    }

    /// <summary>Drives the land value at a Building's Lot to one whole unit.</summary>
    private static void Raise(World world, int building)
    {
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
        Cells east = CellGrid.ToCells(world.Lots.East[lot]);
        Cells north = CellGrid.ToCells(world.Lots.North[lot]);

        world.Layers.SetLandValueTarget(east, north, Fixed.One);

        while (world.Layers.LandValue(east, north) < Fixed.One)
        {
            world.Layers.DriftLandValue();
        }
    }

    private static int Housed(World world, int building) => world.Occupants.Length(building);

    /// <summary>
    /// The same kind on dearer ground is a dearer tenancy, and the floor it is charged on is the
    /// Building's over the tenancies it declares.
    /// </summary>
    [Fact]
    public void One_kind_on_two_land_values_is_two_rents()
    {
        (World world, _, int cheap, int dear) = Pair(seeking: 0);

        Assert.Equal(Share, world.HouseholdFloorOf(cheap));
        Assert.Equal(Share, world.HouseholdFloorOf(dear));

        Assert.Equal(Share * Rate, world.HouseholdRent(cheap).Raw);

        // One whole unit of land value doubles the rate, as it doubles a demolition price.
        Assert.Equal(2 * Share * Rate, world.HouseholdRent(dear).Raw);
    }

    /// <summary>Shown two equally central dwellings, a Household takes the cheaper ground.</summary>
    /// <remarks>
    /// <b>The behaviour a per-kind price could not produce.</b> Both candidates are the same kind at
    /// the same distance from the centre, so centrality and the kind's rate cancel and the only term
    /// left is the land value under each Lot. Both are affordable, so what is read here is the
    /// preference and not the filter.
    /// </remarks>
    [Fact]
    public void Placement_prefers_the_cheaper_ground_when_centrality_is_equal()
    {
        (World world, Simulation simulation, int cheap, int dear) = Pair(seeking: Tenancies);

        for (int i = 0; i < Tenancies; i++)
        {
            world.Endow(world.UnplacedPool.At(i), new Money(4 * Share * Rate));
        }

        for (int tick = 0; tick < 64; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.Equal(Tenancies, Housed(world, cheap));
        Assert.Equal(0, Housed(world, dear));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Household that could pay on cheap ground is priced out of the dear Lot, which is the hard
    /// filter reading the same figure.
    /// </summary>
    [Fact]
    public void A_purse_between_the_two_rents_reaches_only_the_cheaper_dwelling()
    {
        (World world, Simulation simulation, int cheap, int dear) = Pair(seeking: 1);

        world.Endow(world.UnplacedPool.At(0), new Money(Share * Rate));

        for (int tick = 0; tick < 64; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(1, Housed(world, cheap));
        Assert.Equal(0, Housed(world, dear));
    }

    /// <summary>Land value of minus one or below prices a tenancy at zero rather than negative.</summary>
    [Fact]
    public void Land_value_at_minus_one_prices_the_tenancy_at_zero()
    {
        (World world, _, int cheap, _) = Pair(seeking: 0);

        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[cheap]);
        Cells east = CellGrid.ToCells(world.Lots.East[lot]);
        Cells north = CellGrid.ToCells(world.Lots.North[lot]);

        world.Layers.SetLandValueTarget(east, north, -Fixed.One);

        while (world.Layers.LandValue(east, north) > -Fixed.One)
        {
            world.Layers.DriftLandValue();
        }

        Assert.Equal(0, world.HouseholdRent(cheap).Raw);
    }

    /// <summary>A kind that states no rate is free to occupy, whatever it stands on.</summary>
    [Fact]
    public void A_kind_with_no_rate_is_free_on_dear_ground()
    {
        Ruleset free = new(
            resources: [ResourceFamily.Money],
            rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0) { Houses = true }],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: [])
        {
            Placement = Rules().Placement,
            Capacity = new CapacityRuleset(Share, 0, 0),
        };

        var world = new World(1_000, free, Key);

        int dear = Dwelling(world, new Tiles(Away), new Tiles(0));

        Raise(world, dear);

        Assert.Equal(0, world.HouseholdRent(dear).Raw);
    }

    /// <summary>
    /// A Business pays for the floor of the Unit it holds, which is the other tenancy the one figure
    /// serves.
    /// </summary>
    /// <remarks>
    /// <c>chosen.toml</c> rather than a hand-built trade, because what is being read is a premised
    /// Business in a world that raised one for itself.
    /// </remarks>
    [Fact]
    public void A_business_pays_for_the_floor_of_its_unit()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "chosen.toml");
        RulesetLoadResult loaded = RulesetLoader.Load(path);

        Assert.Empty(loaded.Refusals);

        var world = new World(1_000, loaded.Ruleset!, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int found = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot) || world.UnitFloorOf(slot) == 0)
            {
                continue;
            }

            int building = world.Buildings.Rows.Resolve(world.Businesses.Building[slot]);
            long rate = world.Rules.Kind(world.Buildings.Kind[building]).RentPerFloorTile.Raw;

            Assert.Equal(rate * world.UnitFloorOf(slot), world.BusinessRent(slot).Raw);
            Assert.Equal(
                world.TenancyRent(building, world.UnitFloorOf(slot)).Raw,
                world.BusinessRent(slot).Raw);

            found++;

            if (found == 8)
            {
                break;
            }
        }

        Assert.True(found > 0, "chosen.toml's populated city premised no Business.");
    }
}
