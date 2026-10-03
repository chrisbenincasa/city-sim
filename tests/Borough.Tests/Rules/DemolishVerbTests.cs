using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 17 task 4: <c>Demolish</c> is <c>01 §2</c>'s sixth verb, and it clears abandoned stock.
/// </summary>
/// <remarks>
/// <para>
/// <b>The verb ships over the narrow half of its own scope, and the tests are shaped by which half.</b>
/// <c>adr/0091</c> makes clearing <em>occupied</em> ground a compulsory purchase paid at market value
/// off the land value Map Layer, and refuses to compose that price — so the wide half is designed,
/// named and unbuilt, blocked on the land value target. Abandoned stock needs no compensation term
/// because there is nobody left in it to compensate, and that is what could ship.
/// </para>
/// <para>
/// ⚠ <b>The refusal is asserted as hard as the success, and it is the more important of the two.</b>
/// A verb that quietly demolished an occupied Building would be a free bulldozer, and <c>adr/0091</c>'s
/// whole argument is that the price is what makes clearing a decision rather than a button. ***An
/// absence a later sitting may reason from has to read <em>refused</em> rather than <em>missing</em>***
/// (<c>adr/0070</c>), which here means the exception names the successor.
/// </para>
/// <para>
/// <b>This is not the sink and does not claim to be.</b> A shell falls on its own after
/// <c>[[building]] collapses_after_days</c> (<c>adr/0172</c>) — a player is not a sink, and the
/// measurement that settled it is a city that dies with every invariant green. What this verb buys is
/// the Lot back sooner.
/// </para>
/// </remarks>
public sealed class DemolishVerbTests
{
    private static readonly ResourceId Repairs = new(1);

    private const byte House = 1;
    private const ushort Housing = 1;

    /// <summary>How long the starving Rule waits between firings.</summary>
    private const uint Rate = 8;

    /// <summary>
    /// A kind whose one Rule draws on a Bin nothing fills, so every Building is condemned, and whose
    /// shell then stands for a full Day.
    /// </summary>
    /// <remarks>
    /// <b>A Day is the shortest a Ruleset may author</b> (<c>adr/0168</c>), and it is what gives these
    /// tests a window to click in: the whole point of a shell is that it has an extent, and a fixture
    /// that collapsed it on the sweep that found it would have nothing to demolish.
    /// </remarks>
    private static Ruleset Declining() =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 0, 0, 0),
            ],
            kinds:
            [
                new KindDefinition(0, 1, 0, 1)
                {
                    CondemnAfterTicks = 4 * (int)Rate, CollapsesAfterDays = 1, Houses = true, Premises = true,
                },
            ],
            inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],

            // A Zone Rule that condemns and never builds -- adr/0055's permission set scopes what a
            // Rule BUILDS and never which Lots it looks at. Without it the same Rule raises a new
            // Building on the Lot this verb just cleared, and every count below would say nothing.
            zoneRules: [new ZoneRuleDefinition(House, 1, 4, 4)]);

    /// <summary>Four houses in a row, one Household each, at Tiles <c>(0..3, 0)</c>.</summary>
    private static (World World, Simulation Simulation) Built(int houses = 4)
    {
        var world = new World(1_000, Declining());
        var simulation = new Simulation(world, WorldKey.FromSeed(0x0DE_C0DE_D0_11_5AEDUL))
        {
            // O(world) twice per Tick against a phase meant to be O(woken). These tests walk two
            // whole Days to watch a shell stand and fall, and with the guard on that was 16 s of a
            // 42 s assertion tier -- for a check whose own correctness is covered by the tests
            // written for it.
            VerifyDecideWritesNothing = false,
        };

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), Housing);
            Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

            world.CreateHousehold(building, lifeStage: 0);
        }

        return (world, simulation);
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>Issues one <c>demolish</c> at the named Tile on the next Tick.</summary>
    private static void Demolish(Simulation simulation, int east, int north)
    {
        Command[] commands =
        [
            new Command(CommandKind.Demolish, new Tiles(east), new Tiles(north)),
        ];

        simulation.Step(new TickInput(commands, 0));
    }

    /// <summary>How many Buildings are live and not standing empty.</summary>
    private static int Standing(World world)
    {
        int standing = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Buildings.IsAbandoned(slot))
            {
                standing++;
            }
        }

        return standing;
    }

    /// <summary>How many abandoned shells are still on their Lots.</summary>
    private static int Shells(World world)
    {
        int shells = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.IsAbandoned(slot))
            {
                shells++;
            }
        }

        return shells;
    }

    /// <summary>Runs until every Building in the fixture has been abandoned.</summary>
    /// <remarks>
    /// <b>Bounded by the collapse duration rather than by a round number.</b> A shell stands for one
    /// Day and then falls on its own, so a fixture that ran "long enough" could be looking at a city
    /// the clock had already cleared — and the test would pass for the wrong reason.
    /// </remarks>
    private static (World World, Simulation Simulation) Abandoned(int houses = 4)
    {
        (World world, Simulation simulation) = Built(houses);

        for (int i = 0; i < Ticks.PerDay && Shells(world) < houses; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(houses, Shells(world));
        Assert.Equal(0, Standing(world));

        return (world, simulation);
    }

    // ---- the verb -------------------------------------------------------------------------------

    /// <summary>A shell the player clears leaves its Lot, and its neighbours are untouched.</summary>
    [Fact]
    public void Demolishing_an_abandoned_building_clears_its_lot()
    {
        (World world, Simulation simulation) = Abandoned();

        Demolish(simulation, east: 2, north: 0);

        Assert.Equal(3, Shells(world));

        // The Lot is the point: a shell occupies one, and adr/0069 builds only on a vacant Lot, so a
        // demolition that freed the Building and left the Lot occupied would clear nothing that
        // matters. This is the assertion the verb exists to satisfy.
        Assert.True(VacantAt(world, east: 2));

        Assert.False(VacantAt(world, east: 1));
        Assert.False(VacantAt(world, east: 3));
    }

    /// <summary>Whether the Lot at this Tile carries no Building.</summary>
    private static bool VacantAt(World world, int east)
    {
        LotTable lots = world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (lots.Rows.IsLive(slot) && lots.East[slot].Raw == east && lots.North[slot].Raw == 0)
            {
                return lots.IsVacant(slot);
            }
        }

        return false;
    }

    /// <summary>
    /// An occupied Building in a world with no demolition price is cleared free, and its Household
    /// goes to the Unplaced Pool.
    /// </summary>
    [Fact]
    public void Demolishing_an_occupied_building_without_a_price_evicts_its_household()
    {
        (World world, Simulation simulation) = Built();

        Demolish(simulation, east: 1, north: 0);

        Assert.Equal(3, Standing(world));
        Assert.True(VacantAt(world, east: 1));
        Assert.Equal(1, world.UnplacedPool.Count);
    }

    // ---- the price ------------------------------------------------------------------------------

    private const long PricePerTile = 1_000;

    /// <summary>
    /// <c>minimal.toml</c> with a demolition price, populated, with a treasury of
    /// <paramref name="treasury"/> and one whole unit of land value under the first Building holding
    /// Households and no Business. A displaced Business can leave the city on the same
    /// Tick, so its balance cannot be read back afterward.
    /// </summary>
    private static (World World, Simulation Simulation, int Building) Priced(long treasury)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"))
            .Replace("[lots]\n", $"[lots]\ndemolition_price_per_tile = {PricePerTile}\n", StringComparison.Ordinal);
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "minimal.toml");

        Assert.Empty(loaded.Refusals);

        var key = WorldKey.FromSeed(0x0DE_C0DE_D0_11_5AEDUL);
        var world = new World(1_000, loaded.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.EndowTreasury(new Money(treasury));

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Occupants.IsEmpty(slot) && world.BuildingBusinesses.IsEmpty(slot))
            {
                int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[slot]);
                Cells east = CellGrid.ToCells(world.Lots.East[lot]);
                Cells north = CellGrid.ToCells(world.Lots.North[lot]);

                world.Layers.SetLandValueTarget(east, north, Fixed.One);

                while (world.Layers.LandValue(east, north) < Fixed.One)
                {
                    world.Layers.DriftLandValue();
                }

                return (world, simulation, slot);
            }
        }

        Assert.Fail("minimal.toml populated no Building with Households and no Business.");

        return default;
    }

    /// <summary>
    /// The treasury pays the price to the displaced in equal shares, and no money is created or
    /// destroyed.
    /// </summary>
    [Fact]
    public void Demolishing_an_occupied_building_pays_its_price_to_the_displaced()
    {
        (World world, Simulation simulation, int building) = Priced(treasury: 100_000_000);

        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
        long tiles = (long)world.Lots.FootprintWide[lot].Raw * world.Lots.FootprintDeep[lot].Raw;
        long price = world.DemolitionPrice(building).Raw;

        // One whole unit of land value doubles the base price.
        Assert.Equal(2 * tiles * PricePerTile, price);

        var households = new List<Handle<Household>>();
        foreach (int household in world.Occupants.Walk(building))
        {
            households.Add(world.Households.Rows.At(household));
        }

        long[] before = [.. households.Select(h => world.BalanceOf(h).Raw)];
        long treasury = world.TreasuryBalance()!.Value.Raw;
        Money issued = world.MoneySupply.Issued[MoneySupplyTable.Slot];

        simulation.DrainTreasuryFlows();
        Demolish(simulation, world.Lots.East[lot].Raw, world.Lots.North[lot].Raw);

        long[] after = [.. households.Select(h => world.BalanceOf(h).Raw)];
        long share = price / before.Length;
        long[] gains = [.. after.Zip(before, (a, b) => a - b)];

        Assert.Equal(price, gains.Sum());
        Assert.All(gains, gain => Assert.InRange(gain, share, share + 1));
        TreasuryFlows flows = simulation.DrainTreasuryFlows();

        Assert.Equal(price, flows.Compensation);
        Assert.Equal(issued, world.MoneySupply.Issued[MoneySupplyTable.Slot]);
        Assert.All(households, h => Assert.True(world.Households.Rows.TryResolve(h, out _)));
        Assert.Equal(treasury + flows.Income - flows.Expenditure, world.TreasuryBalance()!.Value.Raw);
    }

    /// <summary>Businesses are paid an equal share beside Households.</summary>
    [Fact]
    public void Businesses_share_the_price_with_households()
    {
        (World world, _, _) = Priced(treasury: 1_000_000);

        int building = -1;
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount && building < 0; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Occupants.IsEmpty(slot) && !world.BuildingBusinesses.IsEmpty(slot))
            {
                building = slot;
            }
        }

        Assert.True(building >= 0, "minimal.toml populated no Building with Households and a Business.");

        var households = new List<Handle<Household>>();
        foreach (int household in world.Occupants.Walk(building))
        {
            households.Add(world.Households.Rows.At(household));
        }

        var businesses = new List<Handle<Business>>();
        foreach (int business in world.BuildingBusinesses.Walk(building))
        {
            businesses.Add(world.Businesses.Rows.At(business));
        }

        long[] Balances() =>
            [.. households.Select(h => world.BalanceOf(h).Raw), .. businesses.Select(b => world.BalanceOf(b).Raw)];

        long[] before = Balances();
        long price = (before.Length * 100) + 1;

        world.PayDisplaced(building, new Money(price), world.Tick);

        long[] gains = [.. Balances().Zip(before, (a, b) => a - b)];

        Assert.Equal(price, gains.Sum());
        Assert.Equal(101, gains[0]);
        Assert.All(gains.Skip(1), gain => Assert.Equal(100, gain));
    }

    /// <summary>A treasury short of the price demolishes nothing.</summary>
    [Fact]
    public void A_treasury_short_of_the_price_is_refused_and_removes_nothing()
    {
        (World world, Simulation simulation, int building) = Priced(treasury: 0);

        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
        int held = world.Occupants.Length(building);

        Command demolish = new(CommandKind.Demolish, world.Lots.East[lot], world.Lots.North[lot]);

        Assert.Equal(Refusal.DemolishTreasuryCannotPay, simulation.Refuses(demolish));

        Demolish(simulation, world.Lots.East[lot].Raw, world.Lots.North[lot].Raw);

        Assert.Equal(1, simulation.CommandsRefused);
        Assert.True(world.Buildings.Rows.IsLive(building));
        Assert.Equal(held, world.Occupants.Length(building));
    }

    /// <summary>A Tile with nothing on it is refused rather than resolved to a neighbour.</summary>
    /// <remarks>
    /// <b><c>ApplyTrip</c>'s rule, and it binds harder here.</b> <c>[lots] lots_per_segment</c> is
    /// five, so a block carries up to twenty Lots — a verb that answered <em>the Building in this
    /// block</em> would demolish somebody else's house on a mistyped coordinate, and a mistyped
    /// command must not be indistinguishable from the one somebody meant.
    /// </remarks>
    [Fact]
    public void Demolishing_empty_ground_is_refused()
    {
        (_, Simulation simulation) = Abandoned();

        Assert.Equal(
            Refusal.DemolishNoBuildingOnThatTile,
            simulation.Refuses(new Command(CommandKind.Demolish, new Tiles(40), new Tiles(0))));
    }

    /// <summary>
    /// The shell falls on its own clock, and the verb only makes it sooner.
    /// </summary>
    /// <remarks>
    /// <b>The control for <c>adr/0172</c>, stated as a test rather than left in the ADR.</b> Both
    /// cities end with the Lot vacant; what differs is when. ⚠ <b>If this ever fails by the clock
    /// stopping, the verb has become the sink</b> — and the measurement in `adr/0172` is what that
    /// costs: a city that dies with `adr/0006` green from end to end.
    /// </remarks>
    [Fact]
    public void The_verb_is_a_shortcut_through_the_collapse_clock_and_not_a_replacement_for_it()
    {
        (World cleared, Simulation player) = Abandoned(houses: 1);

        Demolish(player, east: 0, north: 0);

        Assert.Equal(0, Shells(cleared));

        (World unattended, Simulation nobody) = Abandoned(houses: 1);

        Assert.Equal(1, Shells(unattended));

        Run(nobody, Ticks.PerDay + 8);

        Assert.Equal(0, Shells(unattended));
    }
}
