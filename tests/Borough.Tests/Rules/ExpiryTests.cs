using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

public sealed class ExpiryTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);
    private static readonly RuleId FillingTen = new(2);
    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0002UL);

    private const byte Kind = 1;
    private const ulong Cycle = 16;

    private static Ruleset Declaring(bool breadSpoils = true)
    {
        RuleDefinition[] rules =
        [
            Definition(ApplyCount.Band(1, 1), outputs: 0),
            Definition(ApplyCount.Band(10, 10), outputs: 1),
        ];

        return new Ruleset(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules: rules,
            kinds: [new KindDefinition(0, 2, 0, rules.Length)],
            inputs: [],
            outputs: [new Term(new BinRef(Scope.Local, Bread), 1)],
            emissions: [],
            bins: [
                new BinDeclaration(Flour, BinCapacity.Of(100)),
                new BinDeclaration(Bread, BinCapacity.Of(20))],
            kindRules: [new RuleId(1), FillingTen],
            zoneRules: [])
        {
            ResourceShelfLives = breadSpoils ? [default, new ShelfLife(Cycle, 2)] : [],
        };
    }

    private static RuleDefinition Definition(ApplyCount band, int outputs) =>
        new(Kind, 8, band, RuleId.None, false, default, ConditionId.None, 0, 0, 0, outputs, 0, 0);

    private static (World World, Handle<Building> Building) Built(Ruleset? rules = null)
    {
        var world = new World(1_000, rules ?? Declaring());
        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);

        return (world, world.Buildings.Create(world.Lots, lot, Kind));
    }

    internal static World Spoiling()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 7, new Ticks(1));
        world.SpoilExpired(new Ticks(Cycle));
        world.Deposit(bread, 4, new Ticks(Cycle + 1));

        return world;
    }

    private static Handle<Bin> BreadBin(World world, Handle<Building> building) =>
        world.CreateBin(building, Bread);

    private static long Level(World world, Handle<Bin> bin) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(bin));

    private static int RowOf(World world, Handle<Bin> bin) =>
        world.Expiries.RowOf(world.Bins, world.Bins.Rows.Resolve(bin));

    [Fact]
    public void Stock_spoils_after_its_cycles_and_the_row_reports_it()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 5, new Ticks(1));
        Assert.Equal(0, world.SpoilExpired(new Ticks(Cycle)));
        Assert.Equal(0, world.SpoilExpired(new Ticks(Cycle + 3)));

        world.Deposit(bread, 3, new Ticks(Cycle + 1));
        Assert.Equal(5, world.SpoilExpired(new Ticks(2 * Cycle)));

        Assert.Equal(3, Level(world, bread));
        Assert.Equal(5, world.Expiries.Spoiled[RowOf(world, bread)]);
        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void A_non_expiring_resource_gets_no_row()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> flour = world.CreateBin(building, Flour);

        world.Deposit(flour, 40, new Ticks(1));

        Assert.Equal(Rows.NoSlot, RowOf(world, flour));
        Assert.Equal(0, world.Expiries.Rows.LiveCount);
    }

    [Fact]
    public void Withdrawal_takes_the_oldest_stock_first()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 5, new Ticks(1));
        world.SpoilExpired(new Ticks(Cycle));
        world.Deposit(bread, 3, new Ticks(Cycle + 1));
        world.Withdraw(bread, 4, new Ticks(Cycle + 2));

        Assert.Equal(1, world.SpoilExpired(new Ticks(2 * Cycle)));
        Assert.Equal(3, Level(world, bread));
    }

    [Fact]
    public void A_bin_nobody_writes_to_still_spoils_on_schedule()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 6, new Ticks(1));

        for (ulong tick = 2; tick <= 4 * Cycle; tick++)
        {
            world.SpoilExpired(new Ticks(tick));
            Assert.Equal(tick < 2 * Cycle ? 6 : 0, Level(world, bread));
        }
    }

    [Fact]
    public void Spoilage_wakes_a_producer_waiting_for_space()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 20, new Ticks(1));

        Handle<RuleInstance> producer = world.CreateRuleInstance(building, FillingTen, Ticks.Zero, delay: 1);
        world.Wheel.PopDue(new Ticks(1));
        world.Subscribe(producer, bread, Blocking.Space);

        world.SpoilExpired(new Ticks(Cycle));
        Assert.True(world.RuleInstances.IsWaiting(world.RuleInstances.Rows.Resolve(producer)));

        world.SpoilExpired(new Ticks(2 * Cycle));
        Assert.False(world.RuleInstances.IsWaiting(world.RuleInstances.Rows.Resolve(producer)));
    }

    [Fact]
    public void A_mid_cycle_save_reloads_to_the_same_ages()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 7, new Ticks(1));
        world.SpoilExpired(new Ticks(Cycle));
        world.Deposit(bread, 4, new Ticks(Cycle + 5));

        var save = new MemorySave();
        SaveFile.Write(world, 1, save);
        World copy = SaveFile.Read(save, world.Rules, out _);

        Assert.Equal(world.HashState(), copy.HashState());
        Assert.Equal(RowOf(world, bread), RowOf(copy, bread));

        Assert.Equal(7, world.SpoilExpired(new Ticks(2 * Cycle)));
        Assert.Equal(7, copy.SpoilExpired(new Ticks(2 * Cycle)));
        Assert.Equal(world.HashState(), copy.HashState());
        copy.Invariants.RunEndOfRun(copy);
    }

    [Fact]
    public void A_ruleset_that_stops_spoiling_frees_the_rows()
    {
        (World world, Handle<Building> building) = Built();
        Handle<Bin> bread = BreadBin(world, building);

        world.Deposit(bread, 7, new Ticks(1));
        Assert.Equal(1, world.Expiries.Rows.LiveCount);

        world.Adopt(Declaring(breadSpoils: false), 2, new Ticks(2), Key);

        Assert.Equal(0, world.Expiries.Rows.LiveCount);
        Assert.Equal(Rows.NoSlot, RowOf(world, bread));
        Assert.Equal(7, Level(world, bread));
        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void The_loader_converts_minutes_to_ticks()
    {
        Ruleset rules = Load("""
            [[resource]]
            name = "bread"
            family = "good"
            shelf_life_cycles = 3
            shelf_life_cycle_minutes = 720
            """);

        Assert.Equal(new ShelfLife(Ticks.PerDay / 2, 3), rules.ShelfLifeOf(new ResourceId(1)));
    }

    [Theory]
    [InlineData("good", "shelf_life_cycles = 2")]
    [InlineData("good", "shelf_life_cycle_minutes = 60")]
    [InlineData("good", "shelf_life_cycles = 5\nshelf_life_cycle_minutes = 60")]
    [InlineData("money", "shelf_life_cycles = 2\nshelf_life_cycle_minutes = 60")]
    public void The_loader_refuses_an_incomplete_or_impossible_shelf_life(string family, string keys)
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            $"[[resource]]\nname = \"bread\"\nfamily = \"{family}\"\n{keys}\n", "test.toml");

        Assert.False(result.Ok);
    }

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
