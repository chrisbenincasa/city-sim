using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Invariants;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;
using Borough.Tests.Space;

namespace Borough.Tests.Entities;

public sealed class LandSaleTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static readonly WorldKey Key = WorldKey.FromSeed(62002);
    private const string Zone = """
        [[zone_rule]]
        name = "housing"
        kind = "dwelling"
        zone = 0
        interval = 1
        revisit_ticks = 1
        """;
    private const string Currency = """
        [[resource]]
        name = "money"
        family = "money"
        """;

    internal static (World World, Handle<Lot>[] Lots) City(string keys = "construction_charge_per_floor_tile = 2\nbuyer_reserve_per_member = 10", string extra = "")
    {
        var (world, lots) = LocalLayoutTests.Fixture(extra: Zone + "\n" + Currency + "\n" + extra,
            edit: text => text.Replace("[lots]", "[lots]\ndemolition_price_per_tile = 3\n" + keys, StringComparison.Ordinal));
        foreach (var lot in lots)
        { world.PaintFormPermissions(world.LotGround(world.Lots.Rows.Resolve(lot)), false, 0); }
        return (world, lots);
    }

    internal static Handle<Household> Household(World world, Handle<Lot> home, long savings, int members = 1)
    {
        var household = world.CreateHousehold(world.Buildings.Rows.At(world.Lots.BuildingOn(world.Lots.Rows.Resolve(home))), 0);
        for (int i = 0; i < members; i++) { world.CreateCitizen(household); }
        world.Endow(household, new Money(savings));
        return household;
    }

    internal static long Balance(World world, Handle<Household> household) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Households.Balance[world.Households.Rows.Resolve(household)]));

    internal static void Conserved(World world)
    {
        WorldInvariants.MoneyIsConserved(world, world.Invariants);
        Assert.DoesNotContain(world.Invariants.Collected, violation => violation.Invariant == Invariant.MoneyIsConserved);
    }

    [Fact]
    public void Savings_buy_and_build_but_a_poorer_household_is_never_drawn()
    {
        var (world, lots) = City();
        var wealthy = Household(world, lots[0], 100_000);
        var poor = Household(world, lots[0], 1);
        world.Unplace(poor);
        long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
        long treasury = world.TreasuryBalance()!.Value.Raw;
        long land = 0, charge = 0;
        for (int i = 1; i <= 3; i++)
        {
            int row = world.Lots.Rows.Resolve(lots[i]);
            land += world.LandPrice(row).Raw;
            charge += world.ConstructionCharge(world.Lots.FloorTiles(row)).Raw;
        }
        var zoning = new ZoneRuleEngine(world, Key);
        for (ulong tick = 0; tick < 32; tick++) { zoning.Sweep(new Ticks(tick)); }
        for (int i = 1; i <= 3; i++)
        {
            int row = world.Lots.Rows.Resolve(lots[i]);
            Assert.False(world.Lots.IsVacant(row));
            Assert.Equal(wealthy, world.Lots.Landowner[row]);
        }
        Assert.Equal(1, Balance(world, poor));
        Assert.Equal(100_000 - land - charge, Balance(world, wealthy));
        Assert.Equal(treasury + land, world.TreasuryBalance()!.Value.Raw);
        Assert.Equal(issued - charge, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);
        Conserved(world);
    }

    [Fact]
    public void No_savings_leaves_zoned_lots_vacant_with_no_capital_and_evidence_tracks_new_savings()
    {
        var (world, lots) = City();
        var seeker = Household(world, lots[0], 0);
        world.Unplace(seeker);
        ulong before = world.HashState();
        new ZoneRuleEngine(world, Key).Sweep(Ticks.Zero);
        Assert.Equal(before, world.HashState());
        for (int i = 1; i <= 3; i++)
        {
            Assert.True(world.Lots.IsVacant(world.Lots.Rows.Resolve(lots[i])));
            Assert.True(Core.Evidence.Evidence.OfLot(world, lots[i]).Reason.HasFlag(VacancyReason.NoCapital));
        }
        world.Endow(seeker, new Money(100_000));
        Assert.False(Core.Evidence.Evidence.OfLot(world, lots[1]).Reason.HasFlag(VacancyReason.NoCapital));
        Conserved(world);
    }

    [Fact]
    public void Unzoned_and_unfronted_vacant_lots_do_not_report_no_capital()
    {
        var (world, lots) = City();
        world.Unplace(Household(world, lots[0], 0));
        int unzoned = world.Lots.Rows.Resolve(lots[1]);
        world.PaintPermissions(world.LotGround(unzoned), default);
        var reason = Core.Evidence.Evidence.OfLot(world, lots[1]).Reason;
        Assert.True(reason.HasFlag(VacancyReason.NotZoned));
        Assert.False(reason.HasFlag(VacancyReason.NoCapital));
        var unfronted = world.Lots.Create(new Tiles(1000), new Tiles(1000), LotTable.Housing);
        int row = world.Lots.Rows.Resolve(unfronted);
        Assert.Equal(PermissionRefusal.None, world.PaintPermissions(world.LotGround(row), new(LotTable.Housing, 0)));
        Assert.Equal(PermissionRefusal.None, world.ConstructionPermission(row, LotTable.Housing));
        reason = Core.Evidence.Evidence.OfLot(world, unfronted).Reason;
        Assert.False(reason.HasFlag(VacancyReason.NotZoned));
        Assert.True(reason.HasFlag(VacancyReason.NoFrontage));
        Assert.False(reason.HasFlag(VacancyReason.NoCapital));
    }

    [Fact]
    public void An_empty_seeker_pool_does_not_report_no_capital_for_legacy_housing()
    {
        var (world, lots) = City();
        var seeker = Household(world, lots[0], 0);
        Assert.False(Core.Evidence.Evidence.OfLot(world, lots[1]).Reason.HasFlag(VacancyReason.NoCapital));
        world.Unplace(seeker);
        Assert.True(Core.Evidence.Evidence.OfLot(world, lots[1]).Reason.HasFlag(VacancyReason.NoCapital));
    }

    [Fact]
    public void Batch_lot_evidence_matches_individual_reads_and_refreshes_after_income()
    {
        var (world, lots) = City();
        var owner = Household(world, lots[0], 0);
        world.Lots.Landowner[world.Lots.Rows.Resolve(lots[1])] = owner;
        ulong before = world.HashState();
        foreach (var evidence in Core.Evidence.Evidence.OfLots(world))
        {
            Assert.Equal(Core.Evidence.Evidence.OfLot(world, evidence.Lot), evidence);
        }
        Assert.Equal(before, world.HashState());
        world.Endow(owner, new Money(100_000));
        foreach (var evidence in Core.Evidence.Evidence.OfLots(world))
        {
            Assert.Equal(Core.Evidence.Evidence.OfLot(world, evidence.Lot), evidence);
            Assert.False(evidence.Reason.HasFlag(VacancyReason.NoCapital));
        }
    }

    private static Array CapitalIndex(World world) => (Array)typeof(World)
        .GetField("_landCapital", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(world)!;

    [Fact]
    public void Finance_preflight_and_demand_trigger_without_purchases_do_not_build_the_sorted_index()
    {
        var (world, lots) = City(extra: """
            [[zone_rule]]
            name = "trade"
            kind = "dwelling"
            zone = 0
            interval = 1
            revisit_ticks = 1
            build_threshold_days = 2
            cooldown_days = 2
            """);
        Household(world, lots[0], 100_000);
        int lot = world.Lots.Rows.Resolve(lots[1]);
        ulong before = world.HashState();
        world.BeginLandSaleSweep();
        Assert.True(world.CanFinanceConstruction(lot, world.Lots.FloorTiles(lot)));
        Assert.Empty(CapitalIndex(world));
        world.EndLandSaleSweep();
        new ZoneRuleEngine(world, Key).Sweep(Ticks.Zero);
        Assert.Empty(CapitalIndex(world));
        Assert.Equal(before, world.HashState());
    }

    [Fact]
    public void Refused_capital_does_not_claim_demand_or_start_the_construction_cooldown()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"));
        var parsed = RulesetLoader.Parse(text.Replace("[lots]", "[lots]\nconstruction_charge_per_floor_tile = 1000000", StringComparison.Ordinal), "capital-demand.toml");
        Assert.True(parsed.Ok, parsed.Describe());
        var world = new World(400, parsed.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        var simulation = new Simulation(world, Key);
        for (int tick = 0; tick < 2048; tick++) { simulation.Step(default); }
        var zoning = simulation.Zoning;
        var definition = world.Rules.ZoneRules[1];
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var marketFor = typeof(ZoneRuleEngine).GetMethod("MarketFor", flags)!;
        int lot = Enumerable.Range(0, world.Lots.Rows.SlotCount).First(row => world.Lots.Rows.IsLive(row)
            && world.Lots.IsVacant(row) && (int)marketFor.Invoke(zoning, [definition.Kind, row])! >= 0);
        world.PaintUsePermissions(world.LotGround(lot), definition.Admits);
        Assert.Equal(PermissionRefusal.None, world.ConstructionPermission(lot, definition.Admits));
        int market = (int)marketFor.Invoke(zoning, [definition.Kind, lot])!;
        var demand = (long[])typeof(ZoneRuleEngine).GetField("_demand", flags)!.GetValue(zoning)!;
        var claimed = (long[])typeof(ZoneRuleEngine).GetField("_claimed", flags)!.GetValue(zoning)!;
        long threshold = (long)definition.BuildThresholdDays * Ticks.PerDay;
        demand[market] = threshold;
        claimed[market] = 0;
        world.DistrictPools.LastRaised[market] = Ticks.Zero;
        var create = typeof(ZoneRuleEngine).GetMethod("Create", flags)!;
        var at = new Ticks(4 * Ticks.PerDay);
        Assert.False(world.CanFinanceConstruction(lot, world.Lots.FloorTiles(lot)));
        create.Invoke(zoning, [definition, lot, at]);
        Assert.True(world.Lots.IsVacant(lot));
        Assert.Equal(0, claimed[market]);
        Assert.Equal(Ticks.Zero, world.DistrictPools.LastRaised[market]);
        int buyer = Enumerable.Range(0, world.Households.Rows.SlotCount).First(world.Households.Rows.IsLive);
        world.Endow(world.Households.Rows.At(buyer), world.LandPrice(lot) + world.ConstructionCharge(world.Lots.FloorTiles(lot)));
        create.Invoke(zoning, [definition, lot, at]);
        Assert.False(world.Lots.IsVacant(lot));
        Assert.Equal(threshold, claimed[market]);
        Assert.Equal(at, world.DistrictPools.LastRaised[market]);
        Conserved(world);
    }

    [Fact]
    public void Owner_payment_refreshes_the_maximum_without_building_an_index()
    {
        var (world, lots) = City();
        int first = world.Lots.Rows.Resolve(lots[1]);
        long charge = world.ConstructionCharge(world.Lots.FloorTiles(first)).Raw;
        var owner = Household(world, lots[0], charge + 20);
        var buyer = Household(world, lots[0], charge + 10);
        world.Lots.Landowner[first] = owner;
        world.BeginLandSaleSweep();
        Assert.Equal(world.Households.Rows.Resolve(owner), world.MaximumCapitalHousehold());
        Assert.True(world.FinanceConstruction(first, world.Lots.FloorTiles(first), world.Tick));
        Assert.Empty(CapitalIndex(world));
        Assert.Equal(world.Households.Rows.Resolve(buyer), world.MaximumCapitalHousehold());
        world.EndLandSaleSweep();
        Conserved(world);
    }

    [Fact]
    public void A_stale_index_entry_invalidates_and_rebuilds_instead_of_faulting()
    {
        var (world, lots) = City();
        var buyer = Household(world, lots[0], 100_000);
        int lot = world.Lots.Rows.Resolve(lots[1]);
        int household = world.Households.Rows.Resolve(buyer);
        world.BeginLandSaleSweep();
        Assert.Equal(household, world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true));
        world.Endow(buyer, new Money(1));
        world.PayConstruction(household, Rows.NoSlot, new Money(100), new Money(100), world.Tick);
        Assert.True(world.CanFinanceConstruction(lot, world.Lots.FloorTiles(lot)));
        Assert.True(world.FinanceConstruction(lot, world.Lots.FloorTiles(lot), world.Tick));
        world.EndLandSaleSweep();
        Assert.Equal(buyer, world.Lots.Landowner[lot]);
        Conserved(world);
    }

    [Fact]
    public void Land_price_ignores_displacement_and_built_lot_price_includes_all_floor_tiles()
    {
        var (world, lots) = City();
        int vacant = world.Lots.Rows.Resolve(lots[1]);
        Assert.True(world.LandPrice(vacant).Raw > 0);
        Assert.Equal(world.LandPrice(vacant), world.LotPrice(vacant));
        var building = world.CreateBuilding(lots[1], 1, Ticks.Zero, Key);
        int row = world.Buildings.Rows.Resolve(building);
        Assert.Equal(Money.Zero, world.DemolitionPrice(row));
        Assert.Equal(world.LandPrice(vacant) + world.ConstructionCharge(world.FloorTilesOf(row)), world.LotPrice(vacant));
        Household(world, lots[1], 0);
        Assert.Equal(world.LandPrice(vacant), world.DemolitionPrice(row));
    }

    [Fact]
    public void The_owner_builds_without_buying_its_own_land_and_keeps_a_reserve_for_every_member()
    {
        var (world, lots) = City();
        int lot = world.Lots.Rows.Resolve(lots[1]);
        long charge = world.ConstructionCharge(world.Lots.FloorTiles(lot)).Raw;
        var owner = Household(world, lots[0], charge + 20, members: 2);
        world.Lots.Landowner[lot] = owner;
        long treasury = world.TreasuryBalance()!.Value.Raw;
        Assert.True(world.FinanceConstruction(lot, world.Lots.FloorTiles(lot), world.Tick));
        Assert.Equal(owner, world.Lots.Landowner[lot]);
        Assert.Equal(20, Balance(world, owner));
        Assert.Equal(treasury, world.TreasuryBalance()!.Value.Raw);
        Conserved(world);
    }

    [Fact]
    public void An_owner_short_of_its_reserve_sells_to_a_buyer_who_builds()
    {
        var (world, lots) = City();
        int lot = world.Lots.Rows.Resolve(lots[1]);
        long charge = world.ConstructionCharge(world.Lots.FloorTiles(lot)).Raw;
        long land = world.LandPrice(lot).Raw;
        var owner = Household(world, lots[0], charge + 19, members: 2);
        var buyer = Household(world, lots[0], land + charge + 10);
        world.Lots.Landowner[lot] = owner;
        long treasury = world.TreasuryBalance()!.Value.Raw;
        Assert.True(world.FinanceConstruction(lot, world.Lots.FloorTiles(lot), world.Tick));
        Assert.Equal(buyer, world.Lots.Landowner[lot]);
        Assert.Equal(charge + 19 + land, Balance(world, owner));
        Assert.Equal(10, Balance(world, buyer));
        Assert.Equal(treasury, world.TreasuryBalance()!.Value.Raw);
        Conserved(world);
    }

    [Fact]
    public void Both_keys_absent_preserve_free_unowned_construction()
    {
        var (world, lots) = City(keys: "");
        var seeker = Household(world, lots[0], 0);
        world.Unplace(seeker);
        Assert.False(world.Rules.Lots.LandSales);
        Assert.False(Core.Evidence.Evidence.OfLot(world, lots[1]).Reason.HasFlag(VacancyReason.NoCapital));
        var zoning = new ZoneRuleEngine(world, Key);
        for (ulong tick = 0; tick < 32; tick++) { zoning.Sweep(new Ticks(tick)); }
        Assert.False(world.Lots.IsVacant(world.Lots.Rows.Resolve(lots[1])));
        Assert.True(world.Lots.IsCityHeld(world.Lots.Rows.Resolve(lots[1])));
    }

    [Theory]
    [InlineData("construction_charge_per_floor_tile = 0")]
    [InlineData("buyer_reserve_per_member = 0")]
    public void Either_explicit_zero_enables_land_sales(string key)
    {
        var (world, lots) = City(key);
        var buyer = Household(world, lots[0], 100_000);
        int row = world.Lots.Rows.Resolve(lots[1]);
        Assert.True(world.Rules.Lots.LandSales);
        Assert.True(world.FinanceConstruction(row, world.Lots.FloorTiles(row), world.Tick));
        Assert.Equal(buyer, world.Lots.Landowner[row]);
        Conserved(world);
    }

    [Fact]
    public void The_draw_reaches_both_equally_eligible_buyers_and_excludes_an_underfunded_family()
    {
        var (world, lots) = City();
        var a = Household(world, lots[0], 100_000);
        var b = Household(world, lots[0], 100_000);
        Household(world, lots[0], 219, members: 2);
        int aRow = world.Households.Rows.Resolve(a), bRow = world.Households.Rows.Resolve(b);
        int pickedA = 0, pickedB = 0;
        for (int i = 0; i < 128; i++)
        {
            var lot = world.Lots.Create(new Tiles(i), new Tiles(128), LotTable.Housing);
            int picked = world.ConstructionBuyer(world.Lots.Rows.Resolve(lot), Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true);
            Assert.True(picked == aRow || picked == bRow);
            if (picked == aRow) { pickedA++; } else { pickedB++; }
        }
        Assert.InRange(pickedA, 40, 88);
        Assert.InRange(pickedB, 40, 88);
    }

    [Fact]
    public void A_buyer_draw_matches_after_loading_the_pre_purchase_state()
    {
        var (world, lots) = City();
        Household(world, lots[0], 100_000);
        Household(world, lots[0], 100_000);
        var file = new MemorySave();
        SaveFile.Write(world, 17, file);
        World loaded = SaveFile.Read(file, world.Rules, out _);
        int lot = world.Lots.Rows.Resolve(lots[1]);
        Assert.True(world.FinanceConstruction(lot, world.Lots.FloorTiles(lot), world.Tick));
        Assert.True(loaded.FinanceConstruction(lot, loaded.Lots.FloorTiles(lot), loaded.Tick));
        Assert.Equal(world.HashState(), loaded.HashState());
    }

    [Fact]
    public void A_saved_purchase_keeps_its_owner_and_reconstruction_does_not_buy_land_again()
    {
        var (world, lots) = City();
        Household(world, lots[0], 100_000);
        Household(world, lots[0], 100_000);
        int lot = world.Lots.Rows.Resolve(lots[1]);
        long charge = world.ConstructionCharge(world.Lots.FloorTiles(lot)).Raw;
        Assert.True(world.LandPrice(lot).Raw > 0);
        Assert.True(world.FinanceConstruction(lot, world.Lots.FloorTiles(lot), world.Tick));
        var originalBuilding = world.CreateBuilding(lots[1], 1, world.Tick, Key);
        var owner = world.Lots.Landowner[lot];
        long treasury = world.TreasuryBalance()!.Value.Raw;
        long savings = Balance(world, owner);
        var file = new MemorySave();
        SaveFile.Write(world, 17, file);
        World loaded = SaveFile.Read(file, world.Rules, out _);
        Assert.Equal(owner, loaded.Lots.Landowner[lot]);
        Assert.Equal(world.HashState(), loaded.HashState());
        world.DestroyBuilding(originalBuilding, world.Tick);
        loaded.DestroyBuilding(originalBuilding, loaded.Tick);
        long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
        foreach (World city in new[] { world, loaded })
        {
            Assert.True(city.FinanceConstruction(lot, city.Lots.FloorTiles(lot), city.Tick));
            city.CreateBuilding(lots[1], 1, city.Tick, Key);
            Assert.Equal(owner, city.Lots.Landowner[lot]);
            Assert.Equal(treasury, city.TreasuryBalance()!.Value.Raw);
            Assert.Equal(savings - charge, Balance(city, owner));
            Assert.Equal(issued - charge, city.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);
            Conserved(city);
        }
        Assert.Equal(world.HashState(), loaded.HashState());
    }

    [Fact]
    public void Input_log_replay_agrees_with_land_sales_and_reaches_a_buyer_draw()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "declining.toml"));
        text = text.Replace("[lots]", "[lots]\ndemolition_price_per_tile = 3\nconstruction_charge_per_floor_tile = 2\nbuyer_reserve_per_member = 10", StringComparison.Ordinal)
            + "\n[households]\ncar_ownership_percent = 0\nopening_balance_min = 100000\nopening_balance_max = 100000\n";
        var parsed = RulesetLoader.Parse(text, "land-sale-replay.toml");
        Assert.True(parsed.Ok, parsed.Describe());
        const int ticks = 8192, cadence = 256;
        var builder = new InputLogBuilder(62002, new WorldConfiguration(100), ContentHash.Of(System.Text.Encoding.UTF8.GetBytes(text)));
        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        var encoded = new StringWriter();
        InputLogCodec.Write(encoded, builder.Build());
        InputLog log = InputLogCodec.Read(new StringReader(encoded.ToString()));
        ulong[] first = Replay.Run(log, new Ticks(ticks), cadence, parsed.Ruleset!);
        Simulation simulation = Replay.Start(log, parsed.Ruleset!);
        var second = new List<ulong>();
        Replay.Trace(simulation, log, new Ticks(ticks), cadence, second);
        Assert.Equal(first, second);
        int owned = 0;
        for (int row = 0; row < simulation.World.Lots.Rows.SlotCount; row++)
        {
            if (simulation.World.Lots.Rows.IsLive(row) && !simulation.World.Lots.IsCityHeld(row)) { owned++; }
        }
        Assert.True(owned > 0, "The replay must purchase land, not merely repeat a city with sales enabled.");
        Assert.True(simulation.World.TreasuryBalance()!.Value.Raw > 0);
        Conserved(simulation.World);
    }

    [Fact]
    public void Paid_construction_matches_across_worker_counts_and_midrun_save_reload()
    {
        var (world, lots) = City();
        Household(world, lots[0], 100_000);
        Household(world, lots[0], 100_000);
        world.Unplace(Household(world, lots[0], 0));
        var file = new MemorySave();
        SaveFile.Write(world, 17, file);
        World parallelWorld = SaveFile.Read(file, world.Rules, out _);
        var serial = new Simulation(world, world.Key) { RouteWorkerCount = 1, VerifyDecideWritesNothing = true };
        var parallel = new Simulation(parallelWorld, parallelWorld.Key) { RouteWorkerCount = 8, VerifyDecideWritesNothing = true };
        for (int tick = 0; tick < 32; tick++)
        {
            serial.Step(TickInput.Empty);
            parallel.Step(TickInput.Empty);
            Assert.Equal(world.HashState(), parallel.World.HashState());
            if (tick == 15)
            {
                var saved = new MemorySave();
                SaveFile.Write(parallel.World, 17, saved);
                World reloaded = SaveFile.Read(saved, world.Rules, out _);
                parallel = new Simulation(reloaded, reloaded.Key) { RouteWorkerCount = 8, VerifyDecideWritesNothing = true };
            }
        }
        Assert.Equal(5, world.Buildings.Rows.LiveCount);
        for (int i = 1; i <= 3; i++)
        { Assert.False(world.Lots.IsCityHeld(world.Lots.Rows.Resolve(lots[i]))); }
        Conserved(world);
        Conserved(parallel.World);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Assembly_refuses_mixed_ownership_before_retiring_or_charging(bool twoOwners)
    {
        var (world, lots) = City();
        var a = Household(world, lots[0], 100_000);
        var b = Household(world, lots[0], 100_000);
        world.Lots.Landowner[world.Lots.Rows.Resolve(lots[1])] = a;
        if (twoOwners) { world.Lots.Landowner[world.Lots.Rows.Resolve(lots[2])] = b; }
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        ulong before = world.HashState();
        Assert.Equal(LocalLayoutRefusal.MixedOwnership, LocalLayoutCommit.Apply(world, proposal, Key, out _, zoned: true).Refusal);
        Assert.Equal(before, world.HashState());
    }

    [Fact]
    public void Assembly_keeps_its_common_owner_and_charges_the_realized_floor()
    {
        var (world, lots) = City();
        var owner = Household(world, lots[0], 100_000);
        foreach (int i in new[] { 1, 2 }) { world.Lots.Landowner[world.Lots.Rows.Resolve(lots[i])] = owner; }
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        Assert.True(LocalLayoutCommit.Apply(world, proposal, Key, out var building, zoned: true).Accepted);
        int row = world.Lots.Rows.Resolve(world.Buildings.Lot[world.Buildings.Rows.Resolve(building)]);
        Assert.Equal(owner, world.Lots.Landowner[row]);
        Assert.Equal(100_000 - proposal.FloorTiles * 2, Balance(world, owner));
        Conserved(world);
    }

    [Fact]
    public void Assembly_with_no_capital_refuses_before_retiring_sources()
    {
        var (world, lots) = City();
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        ulong before = world.HashState();
        Assert.Equal(LocalLayoutRefusal.NoCapital, LocalLayoutCommit.Apply(world, proposal, Key, out _, zoned: true).Refusal);
        Assert.Equal(before, world.HashState());
    }

    [Fact]
    public void Local_housing_finances_the_selected_footprint_and_evidence_uses_that_same_plan()
    {
        const string housing = """
            [housing_construction]
            preference_persistence_ticks = 1024
            preference_freshness_ticks = 2048
            preference_margin_percent = 25
            max_seekers = 16
            max_building_slots = 4096
            max_lot_slots = 4096
            max_sources = 3
            max_candidates = 32
            surplus_percent = 0
            max_surplus = 0
            alignment_bonus = 2
            same_form_bonus = 1
            [[housing_form]]
            name = "terrace"
            pattern = 2
            min_frontage_tiles = 12
            max_frontage_tiles = 12
            min_depth_tiles = 16
            max_depth_tiles = 16
            storeys = 2
            setback_tiles = 1
            weight = 1
            """;
        var (world, lots) = City(extra: housing);
        var buyer = Household(world, lots[0], 0);
        for (int i = 0; i < 2; i++) { world.Unplace(Household(world, lots[0], 0)); }
        foreach (int i in new[] { 0, 4 })
        {
            int building = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[i]));
            while (world.HasRoomForHousehold(building)) { world.CreateHousehold(world.Buildings.Rows.At(building), 0); }
        }
        var proposal = HousingConstruction.Select(world, world.Key, lots[1], 1);
        Assert.NotNull(proposal);
        Assert.True(Core.Evidence.Evidence.OfLot(world, lots[1]).Reason.HasFlag(VacancyReason.NoCapital));
        foreach (var evidence in Core.Evidence.Evidence.OfLots(world))
        { Assert.Equal(Core.Evidence.Evidence.OfLot(world, evidence.Lot), evidence); }
        var footprint = proposal.Building.Footprint;
        var door = proposal.Sources[0];
        Money land = world.LandPrice(new Tiles(door.East), new Tiles(door.North), (long)footprint.Width * footprint.Height);
        Money charge = world.ConstructionCharge(proposal.FloorTiles);
        world.Endow(buyer, land + charge + new Money(10));
        Assert.False(world.CanFinanceConstruction(world.Lots.Rows.Resolve(lots[1]), world.Lots.FloorTiles(world.Lots.Rows.Resolve(lots[1]))));
        Assert.False(Core.Evidence.Evidence.OfLot(world, lots[1]).Reason.HasFlag(VacancyReason.NoCapital));
        int buildings = world.Buildings.Rows.LiveCount;
        var zoning = new ZoneRuleEngine(world, world.Key);
        for (ulong tick = 0; tick < 32; tick++) { zoning.Sweep(new Ticks(tick)); }
        Assert.Equal(buildings + 1, world.Buildings.Rows.LiveCount);
        Assert.Equal(10, Balance(world, buyer));
        Assert.Equal(charge.Raw, (land + charge + new Money(10)).Raw - world.TreasuryBalance()!.Value.Raw - Balance(world, buyer));
        int owned = 0;
        for (int row = 0; row < world.Lots.Rows.SlotCount; row++)
        { if (world.Lots.Rows.IsLive(row) && world.Lots.Landowner[row] == buyer) { owned++; } }
        Assert.Equal(1, owned);
        Conserved(world);
    }

    [Fact]
    public void A_sweep_updates_buyers_after_payments_and_refreshes_after_later_income()
    {
        var (world, lots) = City();
        int first = world.Lots.Rows.Resolve(lots[1]), second = world.Lots.Rows.Resolve(lots[2]);
        long charge = world.ConstructionCharge(world.Lots.FloorTiles(first)).Raw;
        long land = world.LandPrice(first).Raw;
        var buyer = Household(world, lots[0], land + charge + 10);
        world.BeginLandSaleSweep();
        Assert.True(world.CanFinanceConstruction(first, world.Lots.FloorTiles(first)));
        Assert.True(world.FinanceConstruction(first, world.Lots.FloorTiles(first), world.Tick));
        Assert.False(world.CanFinanceConstruction(second, world.Lots.FloorTiles(second)));
        Assert.False(world.FinanceConstruction(second, world.Lots.FloorTiles(second), world.Tick));
        world.EndLandSaleSweep();
        Assert.Equal(10, Balance(world, buyer));
        world.Endow(buyer, new Money(100_000));
        world.BeginLandSaleSweep();
        Assert.True(world.CanFinanceConstruction(second, world.Lots.FloorTiles(second)));
        Assert.True(world.FinanceConstruction(second, world.Lots.FloorTiles(second), world.Tick));
        world.EndLandSaleSweep();
        Conserved(world);
    }

    [Fact]
    public void Sale_proceeds_make_the_seller_eligible_for_a_cheaper_site_in_the_same_sweep()
    {
        var (world, lots) = City();
        int first = world.Lots.Rows.Resolve(lots[1]), second = world.Lots.Rows.Resolve(lots[2]);
        long charge = world.ConstructionCharge(world.Lots.FloorTiles(first)).Raw;
        long land = world.LandPrice(first).Raw;
        var seller = Household(world, lots[0], charge + 9);
        var buyer = Household(world, lots[0], land + charge + 10);
        world.Lots.Landowner[first] = seller;
        world.Lots.FootprintWide[second] = new Tiles(1);
        world.Lots.FootprintDeep[second] = new Tiles(1);
        world.BeginLandSaleSweep();
        Assert.True(world.FinanceConstruction(first, world.Lots.FloorTiles(first), world.Tick));
        Assert.True(world.FinanceConstruction(second, world.Lots.FloorTiles(second), world.Tick));
        world.EndLandSaleSweep();
        Assert.Equal(buyer, world.Lots.Landowner[first]);
        Assert.Equal(seller, world.Lots.Landowner[second]);
        Conserved(world);
    }

    [Theory]
    [InlineData("construction_charge_per_floor_tile = -1")]
    [InlineData("buyer_reserve_per_member = -1")]
    [InlineData("construction_charge_per_floor_tile = 2147483648")]
    [InlineData("buyer_reserve_per_member = 2147483648")]
    [InlineData("construction_charge_per_floor_tile = \"two\"")]
    [InlineData("buyer_reserve_per_member = true")]
    public void Loader_refuses_invalid_finance_values(string key)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        var result = RulesetLoader.Parse(text.Replace("[lots]", "[lots]\n" + key, StringComparison.Ordinal), "invalid-capital.toml");
        Assert.False(result.Ok);
        Assert.Contains(key[..key.IndexOf(' ')], result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Loader_requires_a_money_resource_even_when_the_new_key_is_zero()
    {
        string text = """
            [[resource]]
            name = "goods"
            family = "good"
            [lots]
            lots_per_segment = 5
            setback_tiles = 0
            construction_charge_per_floor_tile = 0
            """;
        var result = RulesetLoader.Parse(text, "no-money.toml");
        Assert.False(result.Ok);
        Assert.Contains("Land sales require a money Resource", result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Buyer_search_on_a_large_generated_city()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        text = text.Replace("revisit_ticks = 2048", "revisit_ticks = 2048\nbuild_threshold_days = 2\ncooldown_days = 2", StringComparison.Ordinal);
        var parsed = RulesetLoader.Parse(text.Replace("[lots]", "[lots]\nconstruction_charge_per_floor_tile = 1", StringComparison.Ordinal), "buyer-instrument.toml");
        Assert.True(parsed.Ok, parsed.Describe());
        var baselineRules = RulesetLoader.Parse(text, "buyer-baseline.toml");
        Assert.True(baselineRules.Ok, baselineRules.Describe());
        var baseline = new World(100_000, baselineRules.Ruleset!, Key);
        SyntheticCity.PopulateInto(baseline, Key, Ticks.Zero);
        var world = new World(100_000, parsed.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        for (int row = 0; row < baseline.Households.Rows.SlotCount; row++)
        { if (baseline.Households.Rows.IsLive(row)) { baseline.Endow(baseline.Households.Rows.At(row), new Money(10_000)); } }
        Assert.Equal(baseline.Citizens.Rows.LiveCount, world.Citizens.Rows.LiveCount);
        Assert.Equal(baseline.Lots.Rows.LiveCount, world.Lots.Rows.LiveCount);
        for (int row = 0; row < world.Households.Rows.SlotCount; row++)
        { if (world.Households.Rows.IsLive(row)) { world.Endow(world.Households.Rows.At(row), new Money(10_000)); } }
        int lot = 0;
        while (!world.Lots.Rows.IsLive(lot)) { lot++; }
        var zoning = new ZoneRuleEngine(world, Key);
        var baselineZoning = new ZoneRuleEngine(baseline, Key);
        for (int i = 0; i < 32; i++)
        {
            baselineZoning.Sweep(new Ticks((ulong)i * 32));
            zoning.Sweep(new Ticks((ulong)i * 32));
        }
        Assert.Empty(CapitalIndex(world));
        long worstTrigger = 0, worstBaseline = 0, totalEnabled = 0, totalBaseline = 0;
        var triggerWatch = new Stopwatch();
        for (int i = 32; i < 64; i++)
        {
            long enabledTicks = 0, baselineTicks = 0;
            for (int turn = 0; turn < 2; turn++)
            {
                bool enabled = ((i + turn) & 1) == 0;
                triggerWatch.Restart();
                (enabled ? zoning : baselineZoning).Sweep(new Ticks((ulong)i * 32));
                triggerWatch.Stop();
                if (enabled) { enabledTicks = triggerWatch.ElapsedTicks; }
                else { baselineTicks = triggerWatch.ElapsedTicks; }
            }
            if (enabledTicks > worstTrigger) { worstTrigger = enabledTicks; }
            if (baselineTicks > worstBaseline) { worstBaseline = baselineTicks; }
            totalEnabled += enabledTicks;
            totalBaseline += baselineTicks;
            output.WriteLine($"Paired trigger {i - 31}: enabled={TimeSpan.FromSeconds((double)enabledTicks / Stopwatch.Frequency).TotalMilliseconds:F3} ms; absent={TimeSpan.FromSeconds((double)baselineTicks / Stopwatch.Frequency).TotalMilliseconds:F3} ms.");
        }
        Assert.Empty(CapitalIndex(world));
        Assert.Empty(CapitalIndex(baseline));
        for (int i = 0; i < 32; i++) { world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true); }
        var gate = Stopwatch.StartNew();
        world.BeginLandSaleSweep();
        for (int i = 0; i < 1000; i++)
        { Assert.True(world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: false) != Rows.NoSlot); }
        world.EndLandSaleSweep();
        gate.Stop();
        world.BeginLandSaleSweep();
        var singleGate = Stopwatch.StartNew();
        Assert.True(world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: false) != Rows.NoSlot);
        singleGate.Stop();
        var singleDraw = Stopwatch.StartNew();
        int triggerBuyer = world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true);
        world.PayConstruction(triggerBuyer, Rows.NoSlot, new Money(100), new Money(100), world.Tick);
        singleDraw.Stop();
        world.EndLandSaleSweep();
        const int attempts = 1000;
        world.BeginLandSaleSweep();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < attempts; i++) { world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true); }
        watch.Stop();
        world.EndLandSaleSweep();
        world.BeginLandSaleSweep();
        var payments = Stopwatch.StartNew();
        for (int i = 0; i < attempts; i++)
        {
            int buyer = world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true);
            world.PayConstruction(buyer, Rows.NoSlot, new Money(100), new Money(100), world.Tick);
        }
        payments.Stop();
        world.EndLandSaleSweep();
        Conserved(world);
        for (int row = 0; row < world.Households.Rows.SlotCount; row++)
        {
            if (!world.Households.Rows.IsLive(row)) { continue; }
            var balance = world.Households.Balance[row];
            long amount = world.Bins.LevelAt(world.Bins.Rows.Resolve(balance));
            world.Withdraw(balance, amount, world.Tick);
            world.PayOutside(new Money(amount));
        }
        var evidenceWatch = Stopwatch.StartNew();
        var lots = Core.Evidence.Evidence.OfLots(world);
        evidenceWatch.Stop();
        Assert.Contains(lots, item => item.Reason.HasFlag(VacancyReason.NoCapital));
        Conserved(world);
        output.WriteLine($"Poor-city batch evidence for {lots.Length} Lots={evidenceWatch.Elapsed.TotalMilliseconds:F3} ms; one Household capital pass.");
        output.WriteLine($"Interleaved no-purchase Zone Rule trigger sweeps, alternating first runner, 32 pairs: enabled worst={TimeSpan.FromSeconds((double)worstTrigger / Stopwatch.Frequency).TotalMilliseconds:F3} ms; keys-absent worst={TimeSpan.FromSeconds((double)worstBaseline / Stopwatch.Frequency).TotalMilliseconds:F3} ms; enabled mean={TimeSpan.FromSeconds((double)totalEnabled / Stopwatch.Frequency / 32).TotalMilliseconds:F3} ms; keys-absent mean={TimeSpan.FromSeconds((double)totalBaseline / Stopwatch.Frequency / 32).TotalMilliseconds:F3} ms; mean paired difference={TimeSpan.FromSeconds((double)(totalEnabled - totalBaseline) / Stopwatch.Frequency / 32).TotalMilliseconds:F3} ms. Neither built a sorted index.");
        output.WriteLine($"One trigger finance preflight={singleGate.Elapsed.TotalMilliseconds:F3} ms; first draw plus index and payment={singleDraw.Elapsed.TotalMilliseconds:F3} ms; combined single-purchase trigger finance={singleGate.Elapsed.TotalMilliseconds + singleDraw.Elapsed.TotalMilliseconds:F3} ms; one unsorted pass plus 1,000 preflights={gate.Elapsed.TotalMilliseconds:F3} ms.");
        output.WriteLine($"100,000-target generated minimal city; seed 62002; Citizens={world.Citizens.Rows.LiveCount}; Households={world.Households.Rows.LiveCount}; Lots={world.Lots.Rows.LiveCount}; threads=1; attempts={attempts}; index plus search={watch.Elapsed.TotalMilliseconds:F3} ms; index plus paid purchases={payments.Elapsed.TotalMilliseconds:F3} ms; search per attempt={watch.Elapsed.TotalMilliseconds / attempts:F6} ms; paid per attempt={payments.Elapsed.TotalMilliseconds / attempts:F6} ms; concurrent host load is uncontrolled.");
    }
}
