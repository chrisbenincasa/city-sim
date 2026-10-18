using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;
using static Borough.Tests.Entities.LandSaleTests;

namespace Borough.Tests.Entities;

public sealed class LandDepartureTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private const string Sales = "construction_charge_per_floor_tile = 2\nbuyer_reserve_per_member = 10";

    private static long Issued(World world) => world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

    private static Handle<Household> Landowner(World world, Handle<Lot> lot, long savings)
    {
        var owner = Household(world, lot, savings);
        world.Unplace(owner);
        world.Lots.Landowner[world.Lots.Rows.Resolve(lot)] = owner;
        return owner;
    }

    [Fact]
    public void A_departing_landowners_lot_passes_to_its_tenant_and_the_price_leaves_the_city()
    {
        var (world, lots) = City(Sales);
        int lot = world.Lots.Rows.Resolve(lots[0]);
        var tenant = Household(world, lots[0], 100_000);
        var owner = Landowner(world, lots[0], 50);
        long price = world.LotPrice(lot).Raw;
        Assert.True(price > world.LandPrice(lot).Raw, "A built Lot's price includes its floor.");
        long issued = Issued(world);

        world.Depart(owner);

        Assert.Equal(tenant, world.Lots.Landowner[lot]);
        Assert.Equal(100_000 - price, Balance(world, tenant));
        Assert.Equal(issued - 50 - price, Issued(world));
        Conserved(world);
    }

    [Fact]
    public void A_tenant_who_cannot_pay_gives_way_to_any_household_but_never_the_seller()
    {
        var (world, lots) = City(Sales);
        int lot = world.Lots.Rows.Resolve(lots[0]);
        var tenant = Household(world, lots[0], 1);
        var stranger = Household(world, lots[0], 100_000);
        world.Unplace(stranger);
        var owner = Landowner(world, lots[0], 1_000_000);
        long price = world.LotPrice(lot).Raw;

        world.Depart(owner);

        Assert.Equal(stranger, world.Lots.Landowner[lot]);
        Assert.Equal(1, Balance(world, tenant));
        Assert.Equal(100_000 - price, Balance(world, stranger));
        Conserved(world);
    }

    [Fact]
    public void With_no_buyer_the_lot_passes_to_the_city_unpaid()
    {
        var (world, lots) = City(Sales);
        int lot = world.Lots.Rows.Resolve(lots[0]);
        var tenant = Household(world, lots[0], 1);
        var owner = Landowner(world, lots[0], 50);
        long treasury = world.TreasuryBalance()!.Value.Raw;
        long issued = Issued(world);

        world.Depart(owner);

        Assert.True(world.Lots.IsCityHeld(lot));
        Assert.Equal(1, Balance(world, tenant));
        Assert.Equal(treasury, world.TreasuryBalance()!.Value.Raw);
        Assert.Equal(issued - 50, Issued(world));
        Conserved(world);
    }

    [Fact]
    public void Every_lot_a_departing_landowner_holds_is_sold()
    {
        var (world, lots) = City(Sales);
        var tenant = Household(world, lots[0], 100_000);
        var owner = Landowner(world, lots[0], 50);
        int vacant = world.Lots.Rows.Resolve(lots[1]);
        world.Lots.Landowner[vacant] = owner;
        long price = world.LotPrice(world.Lots.Rows.Resolve(lots[0])).Raw + world.LandPrice(vacant).Raw;
        long issued = Issued(world);

        world.Depart(owner);

        Assert.Equal(tenant, world.Lots.Landowner[world.Lots.Rows.Resolve(lots[0])]);
        Assert.Equal(tenant, world.Lots.Landowner[vacant]);
        Assert.Equal(issued - 50 - price, Issued(world));
        Conserved(world);
    }

    [Fact]
    public void Without_land_sales_departure_still_reverts_the_lot_unpaid()
    {
        var (world, lots) = City(keys: "");
        int lot = world.Lots.Rows.Resolve(lots[0]);
        var tenant = Household(world, lots[0], 100_000);
        var owner = Landowner(world, lots[0], 50);
        long issued = Issued(world);

        world.Depart(owner);

        Assert.True(world.Lots.IsCityHeld(lot));
        Assert.Equal(100_000, Balance(world, tenant));
        Assert.Equal(issued - 50, Issued(world));
    }

    [Fact]
    public void The_city_offers_its_built_lots_on_the_cadence()
    {
        var (world, lots) = City(Sales + "\ncity_sale_interval_days = 2");
        int lot = world.Lots.Rows.Resolve(lots[0]), other = world.Lots.Rows.Resolve(lots[4]);
        var tenant = Household(world, lots[0], 100_000);
        long price = world.LotPrice(lot).Raw + world.LotPrice(other).Raw;
        long treasury = world.TreasuryBalance()!.Value.Raw;

        world.SellCityLots(new Ticks((ulong)Ticks.PerDay));
        world.SellCityLots(new Ticks((ulong)(2 * Ticks.PerDay) + 1));
        Assert.True(world.Lots.IsCityHeld(lot));

        world.SellCityLots(new Ticks((ulong)(2 * Ticks.PerDay)));

        Assert.Equal(tenant, world.Lots.Landowner[lot]);
        Assert.Equal(tenant, world.Lots.Landowner[other]);
        Assert.Equal(treasury + price, world.TreasuryBalance()!.Value.Raw);
        Assert.Equal(100_000 - price, Balance(world, tenant));
        for (int i = 1; i <= 3; i++) { Assert.True(world.Lots.IsCityHeld(world.Lots.Rows.Resolve(lots[i]))); }
        Conserved(world);
    }

    [Fact]
    public void A_tenant_who_can_pay_is_always_drawn_ahead_of_an_equal_stranger()
    {
        var (world, lots) = City(Sales);
        int lot = world.Lots.Rows.Resolve(lots[0]);
        var tenant = Household(world, lots[0], 10_000_000);
        var stranger = Household(world, lots[0], 10_000_000);
        world.Unplace(stranger);
        for (ulong day = 0; day < 16; day++)
        {
            world.Lots.Landowner[lot] = default;
            Assert.True(world.SellLot(lot, Rows.NoSlot, new Ticks(day * (ulong)Ticks.PerDay)));
            Assert.Equal(tenant, world.Lots.Landowner[lot]);
        }
        Assert.Equal(10_000_000, Balance(world, stranger));
        Conserved(world);
    }

    [Fact]
    public void Without_the_cadence_key_the_city_keeps_its_lots()
    {
        var (world, lots) = City(Sales);
        Household(world, lots[0], 100_000);
        ulong before = world.HashState();

        world.SellCityLots(Ticks.Zero);

        Assert.True(world.Lots.IsCityHeld(world.Lots.Rows.Resolve(lots[0])));
        Assert.Equal(before, world.HashState());
    }

    [Fact]
    public void A_tenants_standing_debt_is_paid_to_whoever_holds_the_lot_after_a_sale()
    {
        var rules = new Ruleset(
            resources: [ResourceFamily.Money], rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0) { Houses = true, RentPerFloorTile = new Money(10) }],
            inputs: [], outputs: [], emissions: [], bins: [], kindRules: [], zoneRules: [])
        {
            Placement = new PlacementRuleset(0, 0, 0, 20, 0),
            Capacity = new CapacityRuleset(4, 0, 0),
            Rent = new RentRuleset(3),
            Lots = LotRuleset.None with { DemolitionPricePerTile = new Money(1), BuyerReservePerMember = Money.Zero },
        };
        var key = WorldKey.FromSeed(5);
        var world = new World(1_000, rules, key);
        var simulation = new Simulation(world, key);
        var lot = world.Lots.Create(Tiles.Zero, Tiles.Zero, zone: 0, wide: new Tiles(8), deep: new Tiles(1));
        var building = world.CreateBuilding(lot, 1, Ticks.Zero, key);
        var buyer = world.CreateHousehold(building, lifeStage: 0);
        var debtor = world.CreateHousehold(building, lifeStage: 0);
        world.Endow(buyer, new Money(100_000));
        while (world.Tick.Raw <= (ulong)Ticks.PerDay) { simulation.Step(TickInput.Empty); }
        int debtorSlot = world.Households.Rows.Resolve(debtor);
        long debt = world.Households.RentDebt[debtorSlot].Raw;
        Assert.True(debt > 0);
        int row = world.Lots.Rows.Resolve(lot);

        Assert.True(world.SellLot(row, Rows.NoSlot, world.Tick));
        Assert.Equal(buyer, world.Lots.Landowner[row]);
        long before = Balance(world, buyer);
        world.Endow(debtor, new Money(debt));
        world.Arrears.SettleHousehold(debtorSlot, world.Tick);

        Assert.Equal(0, world.Households.RentDebt[debtorSlot].Raw);
        Assert.Equal(before + debt, Balance(world, buyer));
        Conserved(world);
    }

    [Theory]
    [InlineData("city_sale_interval_days = 0", true, "out of range")]
    [InlineData("city_sale_interval_days = 65536", true, "out of range")]
    [InlineData("city_sale_interval_days = 7", false, "without land sales")]
    public void Loader_refuses_an_invalid_or_unsupported_sale_cadence(string key, bool sales, string reason)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        string lots = sales ? Sales + "\n" + key : key;
        var result = RulesetLoader.Parse(text.Replace("[lots]", "[lots]\n" + lots, StringComparison.Ordinal), "sale-cadence.toml");
        Assert.False(result.Ok);
        Assert.Contains("city_sale_interval_days", result.Describe(), StringComparison.Ordinal);
        Assert.Contains(reason, result.Describe(), StringComparison.Ordinal);
    }

    private static (Ruleset Rules, InputLog Log) Generated()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"));
        text = text.Replace("[lots]", "[lots]\ndemolition_price_per_tile = 400\n" + Sales + "\ncity_sale_interval_days = 1", StringComparison.Ordinal);
        var parsed = RulesetLoader.Parse(text, "land-departure.toml");
        Assert.True(parsed.Ok, parsed.Describe());
        var builder = new InputLogBuilder(62002, new WorldConfiguration(400), ContentHash.Of(System.Text.Encoding.UTF8.GetBytes(text)));
        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        var encoded = new StringWriter();
        InputLogCodec.Write(encoded, builder.Build());
        return (parsed.Ruleset!, InputLogCodec.Read(new StringReader(encoded.ToString())));
    }

    private static int Owned(World world)
    {
        int owned = 0;
        for (int row = 0; row < world.Lots.Rows.SlotCount; row++)
        {
            if (world.Lots.Rows.IsLive(row) && !world.Lots.IsVacant(row) && !world.Lots.IsCityHeld(row)) { owned++; }
        }
        return owned;
    }

    private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private static long Time(Action action)
    {
        long start = Stopwatch.GetTimestamp();
        action();
        return Stopwatch.GetTimestamp() - start;
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Departure_and_city_sale_scans_on_a_large_generated_city()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        var enabledRules = RulesetLoader.Parse(text.Replace("[lots]", "[lots]\nconstruction_charge_per_floor_tile = 1\ncity_sale_interval_days = 1", StringComparison.Ordinal), "departure-instrument.toml");
        var absentRules = RulesetLoader.Parse(text, "departure-baseline.toml");
        Assert.True(enabledRules.Ok, enabledRules.Describe());
        Assert.True(absentRules.Ok, absentRules.Describe());
        var key = WorldKey.FromSeed(62002);
        var enabled = new World(100_000, enabledRules.Ruleset!, key);
        var absent = new World(100_000, absentRules.Ruleset!, key);
        SyntheticCity.PopulateInto(enabled, key, Ticks.Zero);
        SyntheticCity.PopulateInto(absent, key, Ticks.Zero);
        Assert.Equal(absent.Households.Rows.SlotCount, enabled.Households.Rows.SlotCount);

        var leaving = new List<int>();
        var built = new List<int>();
        for (int row = 0; row < enabled.Households.Rows.SlotCount && leaving.Count < 64; row++)
        { if (enabled.Households.Rows.IsLive(row) && enabled.Buildings.Rows.TryResolve(enabled.Households.Dwelling[row], out _)) { leaving.Add(row); } }
        for (int row = 0; row < enabled.Lots.Rows.SlotCount && built.Count < 32; row++)
        { if (enabled.Lots.Rows.IsLive(row) && !enabled.Lots.IsVacant(row)) { built.Add(row); } }
        foreach (World world in new[] { enabled, absent })
        { foreach (int row in leaving) { world.Unplace(world.Households.Rows.At(row)); } }

        long worstPoorPass = 0, totalPoorPass = 0;
        for (int i = 0; i < 32; i++)
        {
            var tick = new Ticks((ulong)(i + 1) * (ulong)Ticks.PerDay);
            long a = 0, b = 0;
            for (int turn = 0; turn < 2; turn++)
            {
                if (((i + turn) & 1) == 0) { a = Time(() => enabled.SellCityLots(tick)); }
                else { b = Time(() => absent.SellCityLots(tick)); }
            }
            worstPoorPass = long.Max(worstPoorPass, a);
            totalPoorPass += a - b;
        }
        Assert.All(Enumerable.Range(0, enabled.Lots.Rows.SlotCount), r => Assert.True(!enabled.Lots.Rows.IsLive(r) || enabled.Lots.IsCityHeld(r)));

        long worstScan = 0, totalScan = 0;
        for (int i = 0; i < 32; i++)
        {
            int row = leaving[i];
            long a = 0, b = 0;
            for (int turn = 0; turn < 2; turn++)
            {
                if (((i + turn) & 1) == 0) { a = Time(() => enabled.Depart(enabled.Households.Rows.At(row))); }
                else { b = Time(() => absent.Depart(absent.Households.Rows.At(row))); }
            }
            worstScan = long.Max(worstScan, a - b);
            totalScan += a - b;
        }

        foreach (World world in new[] { enabled, absent })
        {
            for (int row = 0; row < world.Households.Rows.SlotCount; row++)
            { if (world.Households.Rows.IsLive(row)) { world.Endow(world.Households.Rows.At(row), new Money(10_000)); } }
            for (int i = 0; i < 32; i++) { world.Lots.Landowner[built[i]] = world.Households.Rows.At(leaving[32 + i]); }
        }
        long saleStep = 0;
        long worstSale = 0, totalSale = 0;
        for (int i = 0; i < 32; i++)
        {
            int row = leaving[32 + i];
            long a = 0, b = 0;
            for (int turn = 0; turn < 2; turn++)
            {
                if (((i + turn) & 1) == 0) { a = Time(() => enabled.Depart(enabled.Households.Rows.At(row))); }
                else { b = Time(() => absent.Depart(absent.Households.Rows.At(row))); }
            }
            worstSale = long.Max(worstSale, a - b);
            totalSale += a - b;
        }
        Assert.All(built, lot => Assert.False(enabled.Lots.IsCityHeld(lot)));
        int stepSeller = 0;
        while (!enabled.Households.Rows.IsLive(stepSeller)) { stepSeller++; }
        enabled.Lots.Landowner[built[0]] = enabled.Households.Rows.At(stepSeller);
        saleStep = Time(() => enabled.SellOnDeparture(stepSeller, enabled.Tick));
        long maximumPass = Time(() => enabled.MaximumCapitalHousehold());
        long lotScan = Time(() => enabled.SellOnDeparture(stepSeller, enabled.Tick));
        int sold = built[1];
        long priceRead = Time(() => enabled.LotPrice(sold));

        int cityBuilt = Enumerable.Range(0, enabled.Lots.Rows.SlotCount).Count(r => enabled.Lots.Rows.IsLive(r) && !enabled.Lots.IsVacant(r) && enabled.Lots.IsCityHeld(r));
        long richPass = Time(() => enabled.SellCityLots(new Ticks(40UL * (ulong)Ticks.PerDay)));
        int unsold = Enumerable.Range(0, enabled.Lots.Rows.SlotCount).Count(r => enabled.Lots.Rows.IsLive(r) && !enabled.Lots.IsVacant(r) && enabled.Lots.IsCityHeld(r));
        long settledPass = Time(() => enabled.SellCityLots(new Ticks(41UL * (ulong)Ticks.PerDay)));
        Conserved(enabled);

        output.WriteLine($"100,000-target generated minimal city; seed 62002; Citizens={enabled.Citizens.Rows.LiveCount}; Households={enabled.Households.Rows.LiveCount}; Lots={enabled.Lots.Rows.LiveCount}; threads=1; host load uncontrolled.");
        output.WriteLine($"Poor-city Day pass, nothing affordable, 32 interleaved pairs: enabled worst={Ms(worstPoorPass):F3} ms; mean paired difference={Ms(totalPoorPass) / 32:F3} ms.");
        output.WriteLine($"Departure holding no Lot, 32 interleaved pairs: worst paired difference={Ms(worstScan):F3} ms; mean paired difference={Ms(totalScan) / 32:F3} ms.");
        output.WriteLine($"Departure selling one built Lot, 32 interleaved pairs: worst paired difference={Ms(worstSale):F3} ms; mean paired difference={Ms(totalSale) / 32:F3} ms.");
        output.WriteLine($"SellOnDeparture alone, one built Lot to a tenant={Ms(saleStep):F3} ms; Household maximum-capital pass={Ms(maximumPass):F3} ms; Lot scan holding nothing={Ms(lotScan):F3} ms; one Lot price={Ms(priceRead):F3} ms.");
        output.WriteLine($"First rich Day pass selling {cityBuilt - unsold} of {cityBuilt} city-held built Lots={Ms(richPass):F3} ms; next pass with {unsold} left={Ms(settledPass):F3} ms.");
    }

    private static List<Handle<Lot>> CityHeldBuilt(World world)
    {
        var held = new List<Handle<Lot>>();
        for (int row = 0; row < world.Lots.Rows.SlotCount; row++)
        {
            if (world.Lots.Rows.IsLive(row) && !world.Lots.IsVacant(row) && world.Lots.IsCityHeld(row)) { held.Add(world.Lots.Rows.At(row)); }
        }
        return held;
    }

    private static int SoldSince(World world, List<Handle<Lot>> held) =>
        held.Count(lot => world.Lots.Rows.TryResolve(lot, out int row) && !world.Lots.IsCityHeld(row));

    private static void Enrich(World world)
    {
        for (int row = 0; row < world.Households.Rows.SlotCount; row++)
        {
            if (world.Households.Rows.IsLive(row)) { world.Endow(world.Households.Rows.At(row), new Money(1_000_000)); }
        }
    }

    [Fact]
    public void A_generated_citys_lots_move_to_households_as_buyers_appear_and_replay_agrees()
    {
        var (rules, log) = Generated();
        const int days = 3;
        ulong[] first = Replay.Run(log, new Ticks((ulong)(days * Ticks.PerDay)), Ticks.PerDay, rules);
        Simulation simulation = Replay.Start(log, rules);
        var second = new List<ulong>();
        Replay.Trace(simulation, log, new Ticks((ulong)Ticks.PerDay), Ticks.PerDay, second);
        List<Handle<Lot>> unsold = CityHeldBuilt(simulation.World);
        Assert.True(unsold.Count > 0, "Day 0's prices must leave some Lots with the city.");
        Assert.True(Owned(simulation.World) > 0, "Day 0's offer must sell some Lots.");
        Replay.Trace(simulation, log, new Ticks((ulong)((days - 1) * Ticks.PerDay)), Ticks.PerDay, second);
        Assert.Equal(first, second);
        Assert.Equal(unsold.Count, CityHeldBuilt(simulation.World).Count(unsold.Contains));

        Enrich(simulation.World);
        Replay.Trace(simulation, log, new Ticks((ulong)Ticks.PerDay), Ticks.PerDay, second);

        Assert.True(SoldSince(simulation.World, unsold) > unsold.Count / 2,
            $"{SoldSince(simulation.World, unsold)} of {unsold.Count} kept Lots sold once buyers appeared.");
        Conserved(simulation.World);
    }

    [Fact]
    public void City_sales_match_across_worker_counts_and_midrun_save_reload()
    {
        var (rules, log) = Generated();
        Simulation serial = Replay.Start(log, rules);
        serial.RouteWorkerCount = 1;
        serial.VerifyDecideWritesNothing = true;
        Replay.Trace(serial, log, new Ticks((ulong)Ticks.PerDay / 2), 256, []);
        World world = serial.World;
        List<Handle<Lot>> unsold = CityHeldBuilt(world);
        Enrich(world);
        var file = new MemorySave();
        SaveFile.Write(world, 17, file);
        World copy = SaveFile.Read(file, rules, out _);
        var parallel = new Simulation(copy, copy.Key) { RouteWorkerCount = 8, VerifyDecideWritesNothing = true };
        var serialHashes = new List<ulong>();
        var parallelHashes = new List<ulong>();
        Replay.Trace(serial, log, new Ticks((ulong)Ticks.PerDay / 4), 256, serialHashes);
        Replay.Trace(parallel, log, new Ticks((ulong)Ticks.PerDay / 4), 256, parallelHashes);
        var saved = new MemorySave();
        SaveFile.Write(parallel.World, 17, saved);
        World reloaded = SaveFile.Read(saved, rules, out _);
        parallel = new Simulation(reloaded, reloaded.Key) { RouteWorkerCount = 8, VerifyDecideWritesNothing = true };
        Replay.Trace(serial, log, new Ticks((ulong)Ticks.PerDay / 4 + 1), 256, serialHashes);
        Replay.Trace(parallel, log, new Ticks((ulong)Ticks.PerDay / 4 + 1), 256, parallelHashes);
        Assert.Equal(serialHashes, parallelHashes);
        Assert.Equal(world.HashState(), parallel.World.HashState());
        Assert.True(SoldSince(world, unsold) > 0, "The Day 1 offer must sell after the reload.");
        Conserved(world);
    }
}
