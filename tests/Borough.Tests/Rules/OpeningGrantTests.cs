using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>[[business]] opening_grant</c> on <c>rulesets/granted.toml</c>: the treasury capitalizes a
/// Business a Zone Rule opens, and opens none it cannot pay for.
/// </summary>
public sealed class OpeningGrantTests
{
    private const long Grant = 65_536;
    private const long Opening = 4_194_304;
    private const byte Shopfront = 2;
    private const byte Grocer = 2;
    private const int Ground = 48;

    private static readonly WorldKey Key = WorldKey.FromSeed(0);

    private static string Fixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "granted.toml"));

    private static Ruleset Parse(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "granted.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static Ruleset WithTreasury(long balance) => Parse(Fixture().Replace(
        $"opening_balance = {Opening}\n", $"opening_balance = {balance}\n", StringComparison.Ordinal));

    private static Handle<Building> Raise(World world, bool zoned)
    {
        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), zone: 1, wide: new Tiles(Ground), deep: new Tiles(Ground));

        return world.CreateBuilding(lot, Shopfront, Ticks.Zero, Key, zoned);
    }

    private static int CountBusinesses(World world) => world.Businesses.Rows.LiveCount;

    private static long BalanceOfOnly(World world)
    {
        int slot = 0;

        while (!world.Businesses.Rows.IsLive(slot))
        {
            slot++;
        }

        return world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Businesses.Balance[slot]));
    }

    [Fact]
    public void The_fixture_declares_the_grant_and_an_absent_key_pays_nothing()
    {
        Ruleset granted = Parse(Fixture());
        Ruleset shopping = ShoppingTests.Rules();

        Assert.Equal(new Money(Grant), granted.BusinessKind(Grocer).OpeningGrant);
        Assert.Equal(Money.Zero, shopping.BusinessKind(Grocer).OpeningGrant);
    }

    [Fact]
    public void A_negative_grant_is_refused_at_load()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            Fixture().Replace($"opening_grant = {Grant}\n", "opening_grant = -1\n", StringComparison.Ordinal),
            "granted.toml");

        Assert.False(result.Ok);
        Assert.Contains("opening_grant is -1", result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_zone_raised_business_is_paid_its_grant_out_of_the_treasury()
    {
        var world = new World(1_000, Parse(Fixture()), Key);

        Raise(world, zoned: true);

        Assert.Equal(1, CountBusinesses(world));
        Assert.Equal(Grant, BalanceOfOnly(world));
        Assert.Equal(new Money(Opening - Grant), world.TreasuryBalance());
        Assert.Equal(new Money(Opening), world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void A_business_raised_outside_a_zone_rule_opens_with_nothing()
    {
        var world = new World(1_000, Parse(Fixture()), Key);

        Raise(world, zoned: false);

        Assert.Equal(1, CountBusinesses(world));
        Assert.Equal(0, BalanceOfOnly(world));
        Assert.Equal(new Money(Opening), world.TreasuryBalance());
    }

    [Fact]
    public void A_treasury_short_of_the_grant_raises_the_building_without_its_trade()
    {
        var world = new World(1_000, WithTreasury(Grant - 1), Key);

        Handle<Building> building = Raise(world, zoned: true);

        Assert.True(world.Buildings.Rows.TryResolve(building, out _));
        Assert.Equal(0, CountBusinesses(world));
        Assert.Equal(new Money(Grant - 1), world.TreasuryBalance());

        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void A_treasury_holding_exactly_the_grant_pays_it()
    {
        var world = new World(1_000, WithTreasury(Grant), Key);

        Raise(world, zoned: true);

        Assert.Equal(1, CountBusinesses(world));
        Assert.Equal(Money.Zero, world.TreasuryBalance());
    }

    [Fact]
    public void A_founded_business_is_capitalized_by_its_founder_and_receives_no_grant()
    {
        var world = new World(400, Parse(Fixture()), Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int founder = Unemployed(world);
        Money treasury = world.TreasuryBalance()!.Value;

        Handle<Business> founded = world.Found(
            world.Citizens.Rows.At(founder), Grocer, new Money(1_000), Ticks.Zero);

        int slot = world.Businesses.Rows.Resolve(founded);

        Assert.Equal(1_000, world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Businesses.Balance[slot])));
        Assert.Equal(treasury, world.TreasuryBalance());
    }

    [Fact]
    public void Zone_rules_pay_grants_in_a_run_and_money_is_conserved()
    {
        var world = new World(400, Parse(Fixture()), Key);
        var sim = new Simulation(world, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        long granted = Run(sim, 4 * Ticks.PerDay);

        Assert.True(granted > 0, "no Zone Rule opened a Business in four Days");
        Assert.Equal(0, granted % Grant);

        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_granted_run_replays_and_resumes_from_a_save_identically()
    {
        var world = new World(400, Parse(Fixture()), Key);
        var sim = new Simulation(world, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var twin = new World(400, Parse(Fixture()), Key);
        var replay = new Simulation(twin, Key);
        SyntheticCity.PopulateInto(twin, Key, Ticks.Zero);

        long granted = Run(sim, 2 * Ticks.PerDay);
        Run(replay, 2 * Ticks.PerDay);

        Assert.True(granted > 0, "no Zone Rule opened a Business before the save");
        Assert.Equal(world.HashState(), twin.HashState());

        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        World restored = SaveFile.Read(file, world.Rules, out SaveHeader header);
        var resumed = new Simulation(restored, header.Key);

        Assert.Equal(world.HashState(), restored.HashState());

        long after = Run(sim, 2 * Ticks.PerDay);
        long resumedAfter = Run(resumed, 2 * Ticks.PerDay);

        Assert.Equal(after, resumedAfter);
        Assert.Equal(world.HashState(), restored.HashState());

        resumed.CheckEndOfRun();
    }

    private static long Run(Simulation sim, int ticks)
    {
        long granted = 0;

        for (int t = 0; t < ticks; t++)
        {
            sim.Step(default);
            granted += sim.Zoning.DrainGrants().Sum;
        }

        return granted;
    }

    private static int Unemployed(World world)
    {
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot) && world.Citizens.Workplace[slot].IsNone)
            {
                return slot;
            }
        }

        Assert.Fail("every Citizen is employed, so nobody can found a Business");

        return 0;
    }
}
