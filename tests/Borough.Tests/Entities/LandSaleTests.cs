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

    private static (World World, Handle<Lot>[] Lots) City(string keys = "construction_charge_per_floor_tile = 2\nbuyer_reserve_per_member = 10", string extra = "")
    {
        var (world, lots) = LocalLayoutTests.Fixture(extra: Zone + "\n" + Currency + "\n" + extra,
            edit: text => text.Replace("[lots]", "[lots]\ndemolition_price_per_tile = 3\n" + keys, StringComparison.Ordinal));
        foreach (var lot in lots)
        { world.PaintFormPermissions(world.LotGround(world.Lots.Rows.Resolve(lot)), false, 0); }
        return (world, lots);
    }

    private static Handle<Household> Household(World world, Handle<Lot> home, long savings, int members = 1)
    {
        var household = world.CreateHousehold(world.Buildings.Rows.At(world.Lots.BuildingOn(world.Lots.Rows.Resolve(home))), 0);
        for (int i = 0; i < members; i++) { world.CreateCitizen(household); }
        world.Endow(household, new Money(savings));
        return household;
    }

    private static long Balance(World world, Handle<Household> household) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Households.Balance[world.Households.Rows.Resolve(household)]));

    private static void Conserved(World world)
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
    public void Purchasing_then_save_reload_and_replay_choose_the_same_owner()
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
        Assert.True(world.FinanceConstruction(first, world.Lots.FloorTiles(first), world.Tick));
        Assert.False(world.FinanceConstruction(second, world.Lots.FloorTiles(second), world.Tick));
        world.EndLandSaleSweep();
        Assert.Equal(10, Balance(world, buyer));
        world.Endow(buyer, new Money(100_000));
        world.BeginLandSaleSweep();
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
        var parsed = RulesetLoader.Parse(text.Replace("[lots]", "[lots]\nconstruction_charge_per_floor_tile = 1", StringComparison.Ordinal), "buyer-instrument.toml");
        Assert.True(parsed.Ok, parsed.Describe());
        var world = new World(100_000, parsed.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        for (int row = 0; row < world.Households.Rows.SlotCount; row++)
        { if (world.Households.Rows.IsLive(row)) { world.Endow(world.Households.Rows.At(row), new Money(10_000)); } }
        int lot = 0;
        while (!world.Lots.Rows.IsLive(lot)) { lot++; }
        for (int i = 0; i < 32; i++) { world.ConstructionBuyer(lot, Rows.NoSlot, new Money(100), new Money(100), world.Tick, draw: true); }
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
        output.WriteLine($"100,000-target generated minimal city; seed 62002; Citizens={world.Citizens.Rows.LiveCount}; Households={world.Households.Rows.LiveCount}; Lots={world.Lots.Rows.LiveCount}; threads=1; attempts={attempts}; index plus search={watch.Elapsed.TotalMilliseconds:F3} ms; index plus paid purchases={payments.Elapsed.TotalMilliseconds:F3} ms; search per attempt={watch.Elapsed.TotalMilliseconds / attempts:F6} ms; paid per attempt={payments.Elapsed.TotalMilliseconds / attempts:F6} ms; concurrent host load is uncontrolled.");
    }
}
