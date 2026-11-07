using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Core.Space;
using Query = Borough.Core.Evidence.Evidence;
using Borough.Formats;
using Borough.Headless;
using Borough.Tests.Persistence;

namespace Borough.Tests.Utilities;

public sealed class UtilityBillingTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);
    private static readonly ResourceId Power = new(1);
    private static readonly ResourceId Token = new(2);
    private static readonly ResourceId Currency = new(3);

    internal const string Tariff = """

        [[policy]]
        name = "power_tariff"
        tool = "tariff"
        utility = "power"
        transfer = { from = "local", to = "global", resource = "money", amount = 3 }
        """;

    internal const string Source = """
        [[resource]]
        name = "power"
        family = "utility"
        storage = 0
        link_per_crossing = 120

        [[resource]]
        name = "token"
        family = "utility"

        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "water"
        family = "utility"
        storage = 0
        link_per_crossing = 120

        [[building]]
        name = "home"
        houses = true
        bins = [ { resource = "token", capacity = 1000000, owner = "occupant" } ]

        [[rule]]
        name = "draw"
        kind = "home"
        rate = 1
        apply = { min = 4, max = 4 }
        inputs = [ { scope = "pool", resource = "power", amount = 2 } ]
        outputs = [ { scope = "local", resource = "token", amount = 1 } ]

        [districts]
        prominence_percent = 50
        revisit_ticks = 2048
        hysteresis_percent = 50
        migrate_cells = 16

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 0
        """;

    internal static Ruleset Load(string text)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(text, "billing.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        return loaded.Ruleset!;
    }

    private sealed record City(World World, Simulation Simulation, int Building, Handle<Bin> Purse, int Output, int Business);

    private static City Build(bool business = false, bool billed = true, long balance = 1000000, int perUnit = 2, bool multi = false)
    {
        string text = Source.Replace("resource = \"power\", amount = 2", $"resource = \"power\", amount = {perUnit}", StringComparison.Ordinal);
        if (business)
        {
            text = text.Replace("houses = true", "premises = true\nbusiness = \"shop\"", StringComparison.Ordinal)
                .Replace("owner = \"occupant\"", "owner = \"business\"", StringComparison.Ordinal)
                + "\n[[business]]\nname = \"shop\"\n";
        }

        if (multi)
        {
            text = text.Replace("inputs = [", "inputs = [ { scope = \"pool\", resource = \"water\", amount = 1 },", StringComparison.Ordinal);
            text += Tariff.Replace("power", "water", StringComparison.Ordinal);
        }

        var world = new World(20, Load(text + (billed ? Tariff : "")), Key);
        Handle<Lot> lot = world.Lots.Create(new Tiles(4), Tiles.Zero, zone: 1);
        Handle<Building> building = world.CreateBuilding(lot, 1, Ticks.Zero, Key);
        int b = world.Buildings.Rows.Resolve(building);
        int firm = Rows.NoSlot;
        Handle<Bin> purse;
        int output;
        if (business)
        {
            firm = world.Buildings.BusinessHead[b] - 1;
            purse = world.Businesses.Balance[firm];
            output = Rows.NoSlot;
            world.EndowTreasury(new Money(balance));
            world.Withdraw(world.Bins.Rows.At(world.FindTreasuryBin(Currency)), balance, world.Tick);
            world.Deposit(purse, balance, world.Tick);
        }
        else
        {
            Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);
            int h = world.Households.Rows.Resolve(household);
            purse = world.Households.Balance[h];
            output = Rows.NoSlot;
            world.Endow(household, new Money(balance));
        }

        output = world.FindLocalBin(world.Buildings.RuleHead[b] - 1, Token);
        var district = world.Districts.Create(new Cells(0), new Cells(0));
        world.DistrictCells.Create(new Cells(0), new Cells(0), district);
        world.RebuildDerived();
        world.FitDistrictPools();
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };
        simulation.Step(default);
        SetDelivery(world, Ratio.One);
        simulation.DrainTreasuryFlows();
        return new City(world, simulation, b, purse, output, firm);
    }

    private static void SetDelivery(World world, Ratio delivered)
    {
        for (int row = 0; row < world.UtilityFlows.Rows.SlotCount; row++)
        {
            if (world.UtilityFlows.Rows.IsLive(row)) { world.UtilityFlows.Delivered[row] = delivered; }
        }
    }

    internal static Simulation Metered(bool multi = false)
    {
        City city = Build(multi: multi);
        city.Simulation.Step(default);
        return city.Simulation;
    }

    internal static World Billed() => Metered(multi: true).World;

    private static UtilityPayments Payments(City city)
    {
        int row = city.World.UtilityBills.Find(city.World.Buildings, city.Building, city.World.Rules.ResourceKey(Power));
        return row == Rows.NoSlot ? default : city.World.UtilityBills.Payments[row];
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_draw_pays_units_received_and_businesses_book_the_expense(bool business)
    {
        City city = Build(business);
        long opening = city.World.Bins.LevelAt(city.World.Bins.Rows.Resolve(city.Purse));
        long output = city.World.Bins.LevelAt(city.Output);
        city.Simulation.Step(default);
        long applications = city.World.Bins.LevelAt(city.Output) - output;
        long expected = applications * 2 * 3;
        Assert.Equal(4, applications);
        Assert.Equal(opening - expected, city.World.Bins.LevelAt(city.World.Bins.Rows.Resolve(city.Purse)));
        Assert.Equal(expected, Payments(city).Paid);
        Assert.Equal(0, Payments(city).Unpaid);
        Assert.Equal(expected, city.Simulation.DrainTreasuryFlows().Tariff);
        if (business) { Assert.Equal(expected, city.World.Businesses.DayExpense[city.Business]); }
    }

    [Fact]
    public void A_brownout_bills_floor_scaled_units_and_zero_applications_bill_nothing()
    {
        City city = Build();
        SetDelivery(city.World, new Ratio(32768));
        city.Simulation.Step(default);
        Assert.Equal(12, Payments(city).Paid);
        Assert.Equal(2, city.World.Bins.LevelAt(city.Output));
        SetDelivery(city.World, Ratio.Zero);
        city.Simulation.Step(default);
        Assert.Equal(12, Payments(city).Paid);
        Assert.Equal(2, city.World.Bins.LevelAt(city.Output));
    }

    [Fact]
    public void A_rule_drawing_two_utilities_pays_each_at_the_fired_count()
    {
        City city = Build(multi: true);
        city.Simulation.Step(default);
        Assert.Equal(24, Payments(city).Paid);
        Assert.Equal(2, city.World.UtilityBills.Rows.LiveCount);
        Assert.Equal((12L, 0L), city.World.UtilityBills.PriorOn(city.World.Buildings,
            city.Building, city.World.Rules.ResourceKey(new ResourceId(4)), 1));
        Assert.Equal(36, city.Simulation.DrainTreasuryFlows().Tariff);
    }

    [Fact]
    public void A_bill_product_saturates_instead_of_overflowing()
    {
        City city = Build(balance: 5, perUnit: int.MaxValue);
        city.World.Policies.Govern(0, int.MaxValue);
        city.Simulation.Step(default);
        Assert.Equal(5, Payments(city).Paid);
        Assert.Equal(long.MaxValue - 5, Payments(city).Unpaid);
    }

    [Fact]
    public void A_short_purse_pays_its_level_and_waives_the_remainder()
    {
        City city = Build(balance: 5);
        city.Simulation.Step(default);
        Assert.Equal(0, city.World.Bins.LevelAt(city.World.Bins.Rows.Resolve(city.Purse)));
        Assert.Equal(5, Payments(city).Paid);
        Assert.Equal(19, Payments(city).Unpaid);
        Assert.Equal(4, city.World.Bins.LevelAt(city.Output));
    }

    [Fact]
    public void Govern_changes_the_next_bill_and_negative_prices_charge_zero()
    {
        City city = Build();
        city.Simulation.Step(new TickInput([Command.Govern(0, 7)], 0));
        Assert.Equal(56, Payments(city).Paid);
        city.Simulation.Step(new TickInput([Command.Govern(0, -1)], 0));
        Assert.Equal(56, Payments(city).Paid);
        Assert.Equal(0, Payments(city).Unpaid);
    }

    [Fact]
    public void An_untariffed_utility_moves_no_money_and_is_distinguishable_from_a_zero_price()
    {
        City city = Build(billed: false);
        city.Simulation.Step(default);
        Assert.Equal(0, city.World.UtilityBills.Rows.LiveCount);
        Assert.Equal(0, city.Simulation.DrainTreasuryFlows().Tariff);
        Assert.False(Assert.Single(Query.OfBuilding(city.World, city.World.Buildings.Rows.At(city.Building)).Utilities.ToArray()).HasTariff);
        City zero = Build();
        zero.World.Policies.Govern(0, 0);
        zero.Simulation.Step(default);
        UtilityEvidence evidence = Assert.Single(Query.OfBuilding(zero.World, zero.World.Buildings.Rows.At(zero.Building)).Utilities.ToArray());
        Assert.True(evidence.HasTariff);
        Assert.Equal(0, evidence.Paid);
    }

    [Fact]
    public void Billing_conserves_money_and_the_treasury_flow_explains_each_interval()
    {
        City city = Build(balance: 1000);
        long previous = 0;
        for (int tick = 0; tick < Ticks.PerDay * 3; tick++)
        {
            SetDelivery(city.World, Ratio.One);
            city.Simulation.Step(default);
            TreasuryFlows flows = city.Simulation.DrainTreasuryFlows();
            long treasury = city.World.Bins.LevelAt(city.World.FindTreasuryBin(Currency));
            Assert.Equal(treasury - previous, flows.Income - flows.Expenditure);
            previous = treasury;
        }

        city.Simulation.CheckEndOfRun();
        Assert.Equal(1000, previous);
        Assert.True(Payments(city).Unpaid > 0);
    }

    [Fact]
    public void Saved_counters_and_yesterdays_evidence_resume_identically()
    {
        City city = Build(balance: 5);
        city.Simulation.Step(default);
        var file = new MemorySave();
        SaveFile.Write(city.World, 1, file);
        World loaded = SaveFile.Read(file, city.World.Rules, out SaveHeader header);
        var resumed = new Simulation(loaded, header.Key) { VerifyDecideWritesNothing = true };
        Assert.Equal(city.World.HashState(), loaded.HashState());
        for (int tick = 0; tick <= Ticks.PerDay; tick++)
        {
            city.Simulation.Step(default);
            resumed.Step(default);
            Assert.Equal(city.World.HashState(), loaded.HashState());
        }

        UtilityEvidence evidence = Assert.Single(Query.OfBuilding(loaded, loaded.Buildings.Rows.At(city.Building)).Utilities.ToArray());
        Assert.True(evidence.HasTariff);
        Assert.Equal(5, evidence.Paid);
        Assert.True(evidence.Unpaid > 19);
        using var output = new StringWriter();
        EvidenceDump.Utilities(output, RulesetLoader.Parse(Source + Tariff, "billing.toml").Names,
            Query.OfBuilding(loaded, loaded.Buildings.Rows.At(city.Building)));
        Assert.Contains("paid yesterday", output.ToString(), StringComparison.Ordinal);
        string line = output.ToString().Split('\n').Single(row => row.StartsWith("  power ", StringComparison.Ordinal));
        string[] cells = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("5", cells[^2]);
        Assert.Equal(evidence.Unpaid.ToString(System.Globalization.CultureInfo.InvariantCulture), cells[^1]);
    }

    [Fact]
    public void Lazy_counters_roll_across_consecutive_days_and_gaps()
    {
        City city = Build();
        var bills = city.World.UtilityBills;
        ulong key = city.World.Rules.ResourceKey(Power);
        bills.Record(city.World.Buildings, city.Building, key, 1, 5, 7);
        Assert.Equal((5L, 7L), bills.PriorOn(city.World.Buildings, city.Building, key, 2));
        bills.Record(city.World.Buildings, city.Building, key, 2, 11, 13);
        Assert.Equal((5L, 7L), bills.PriorOn(city.World.Buildings, city.Building, key, 2));
        Assert.Equal((0L, 0L), bills.PriorOn(city.World.Buildings, city.Building, key, 4));
        bills.Record(city.World.Buildings, city.Building, key, 4, 17, 19);
        Assert.Equal((0L, 0L), bills.PriorOn(city.World.Buildings, city.Building, key, 4));
    }

    [Fact]
    public void Removing_delivery_retires_its_billing_history()
    {
        City city = Build();
        city.Simulation.Step(default);
        Assert.Equal(1, city.World.UtilityBills.Rows.LiveCount);
        string undelivered = Source.Replace("link_per_crossing = 120", "", StringComparison.Ordinal)
            .Replace("inputs = [ { scope = \"pool\", resource = \"power\", amount = 2 } ]", "inputs = []", StringComparison.Ordinal);
        city.World.Adopt(Load(undelivered), 0, city.World.Tick, Key);
        Assert.Equal(0, city.World.UtilityBills.Rows.LiveCount);
        Assert.Equal(0, city.World.Buildings.UtilityBillHead[city.Building]);
        city.World.Adopt(Load(Source + Tariff), 0, city.World.Tick, Key);
        Assert.Equal(0, city.World.UtilityBills.Rows.LiveCount);
    }

    [Fact]
    public void Recycled_billing_rows_rebuild_in_the_same_order()
    {
        City city = Build();
        city.Simulation.Step(default);
        World world = city.World;
        Handle<Lot> lot = world.Lots.Create(new Tiles(8), Tiles.Zero, zone: 1);
        int other = world.Buildings.Rows.Resolve(world.CreateBuilding(lot, 1, world.Tick, Key));
        world.UtilityBills.Record(world.Buildings, other, world.Rules.ResourceKey(Power), 0, 1, 0);
        world.DestroyBuilding(world.Buildings.Rows.At(city.Building), world.Tick);
        world.UtilityBills.Record(world.Buildings, other, world.Rules.ResourceKey(new ResourceId(4)), 0, 1, 0);
        int[] heads = world.Buildings.UtilityBillHead.Span.ToArray();
        int[] next = world.UtilityBills.Next.Span.ToArray();
        ulong hash = world.HashState();
        world.RebuildDerived();
        Assert.Equal(hash, world.HashState());
        Assert.Equal(heads, world.Buildings.UtilityBillHead.Span.ToArray());
        Assert.Equal(next, world.UtilityBills.Next.Span.ToArray());
    }

    [Fact]
    public void Demolition_frees_bills_and_a_recycled_building_has_no_old_evidence()
    {
        City city = Build();
        city.Simulation.Step(default);
        Assert.Equal(1, city.World.UtilityBills.Rows.LiveCount);
        city.World.DestroyBuilding(city.World.Buildings.Rows.At(city.Building), city.World.Tick);
        Assert.Equal(0, city.World.UtilityBills.Rows.LiveCount);
        Handle<Lot> lot = city.World.Lots.Create(new Tiles(8), Tiles.Zero, zone: 1);
        Handle<Building> replacement = city.World.CreateBuilding(lot, 1, city.World.Tick, Key);
        int building = city.World.Buildings.Rows.Resolve(replacement);
        Assert.Equal(city.Building, building);
        Assert.Equal(Rows.NoSlot, city.World.UtilityBills.Find(city.World.Buildings, building, city.World.Rules.ResourceKey(Power)));
        city.World.RebuildDerived();
        Assert.Equal(Rows.NoSlot, city.World.UtilityBills.Find(city.World.Buildings, building, city.World.Rules.ResourceKey(Power)));
    }
}
