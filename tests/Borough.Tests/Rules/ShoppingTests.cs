using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

public sealed class ShoppingTests
{
    internal static Ruleset Rules()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        return loaded.Ruleset!;
    }

    internal static (World World, Simulation Sim) Start()
    {
        Ruleset rules = Rules();
        var key = WorldKey.FromSeed(0);
        var world = new World(400, rules, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        return (world, sim);
    }

    [Fact]
    public void Purchases_travel_home_and_every_unit_is_accounted_for()
    {
        var (world, sim) = Start();
        long bought = 0, delivered = 0, lost = 0;
        bool sawCargo = false;
        int outings = 0;
        for (int t = 0; t < 4096; t++)
        {
            sim.Step(default);
            var r = sim.Shopping.Last;
            bought += r.Bought; delivered += r.Delivered; lost += r.Lost; outings += r.Outings;
            long cargo = Cargo(world);
            sawCargo |= cargo > 0;
            Assert.Equal(bought, delivered + lost + cargo);
            var travelling = new HashSet<int>();
            for (int slot = 0; slot < world.Travellers.Rows.SlotCount; slot++)
            {
                if (world.Travellers.Rows.IsLive(slot))
                { Assert.True(travelling.Add(world.Citizens.Rows.Resolve(world.Travellers.Citizen[slot]))); }
            }
        }
        Assert.True(sawCargo);
        Assert.True(bought > 0 && delivered > 0 && outings > 0);
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_save_with_goods_in_transit_resumes_identically()
    {
        var (world, sim) = Start();
        int ticks = 0;
        while (Cargo(world) == 0 && ticks++ < 4096) { sim.Step(default); }
        Assert.True(Cargo(world) > 0);
        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        World restored = SaveFile.Read(file, world.Rules, out var header);
        var resumed = new Simulation(restored, header.Key) { VerifyDecideWritesNothing = true };
        sim.VerifyDecideWritesNothing = true;
        Assert.Equal(world.HashState(), restored.HashState());
        for (int t = 0; t < 128; t++)
        {
            sim.Step(default); resumed.Step(default);
            Assert.Equal(world.HashState(), restored.HashState());
        }
    }

    [Theory]
    [InlineData(0, 8, true)]
    [InlineData(5, 8, false)]
    [InlineData(6, 8, false)]
    [InlineData(0, 7, false)]
    [InlineData(0, 20, false)]
    public void Opening_hours_are_weekly_and_the_closing_boundary_is_exclusive(int day, int hour, bool expected)
    {
        var hours = new WeeklyHours(31, 8, 20);
        Assert.Equal(expected, hours.IsOpen(new Ticks((ulong)(day * Ticks.PerDay + Ticks.AtClock(hour)))));
    }

    [Fact]
    public void Actual_attendance_earns_wages_and_weekdays_have_a_common_weekend()
    {
        var (world, sim) = Start();
        for (int t = 0; t < 2048; t++) { sim.Step(default); }
        int worker = Enumerable.Range(0, world.Citizens.Rows.SlotCount).First(c => world.Citizens.Rows.IsLive(c)
            && world.Businesses.Rows.TryResolve(world.Citizens.Workplace[c], out int job)
            && world.Rules.BusinessKind(world.Businesses.Kind[job]).WorkDays == 31);
        int employer = world.Businesses.Rows.Resolve(world.Citizens.Workplace[worker]);
        var trade = world.Rules.BusinessKind(world.Businesses.Kind[employer]);
        int start = CommuteRoster.ShiftStartOf(world.Key, world.Businesses.Rows.IdAt(employer), trade);
        Ticks monday = new((ulong)(7 * Ticks.PerDay + start));
        Ticks saturday = new((ulong)(5 * Ticks.PerDay + start));
        Assert.True(WorkSchedule.OnDuty(world, worker, monday));
        Assert.False(WorkSchedule.OnDuty(world, worker, saturday));
        world.Citizens.EarnedWage[worker] = 0;
        world.Citizens.WageRemainder[worker] = 0;
        world.Citizens.Activity[worker] = (byte)CitizenActivity.AtHome;
        for (int t = 0; t < 128; t++) { WorkSchedule.Accrue(world, monday + new Ticks((ulong)t)); }
        Assert.Equal(0, world.Citizens.EarnedWage[worker]);
        world.Citizens.Activity[worker] = (byte)CitizenActivity.AtWork;
        for (int t = 0; t < 128; t++) { WorkSchedule.Accrue(world, monday + new Ticks((ulong)t)); }
        Assert.True(world.Citizens.EarnedWage[worker] > 0);
        long earned = world.Citizens.EarnedWage[worker];
        for (int t = 0; t < 128; t++) { WorkSchedule.Accrue(world, saturday + new Ticks((ulong)t)); }
        Assert.Equal(earned, world.Citizens.EarnedWage[worker]);
    }

    [Fact]
    public void A_partial_purchase_pays_the_sellers_price_and_does_not_fill_the_home_at_the_counter()
    {
        var (world, sim, row) = AtCounter();
        int hh = world.Households.Rows.Resolve(world.Shopping.Household[row]);
        int business = world.Businesses.Rows.Resolve(world.Shopping.Seller[row]);
        int stock = BinOf(world, world.Businesses.BinHead[business], world.Shopping.Good[row]);
        int home = BinOf(world, world.Households.BinHead[hh], world.Shopping.Good[row]);
        int purse = world.Bins.Rows.Resolve(world.Households.Balance[hh]);
        int till = world.Bins.Rows.Resolve(world.Businesses.Balance[business]);
        int market = world.Markets.MarketOf(world, stock);
        SetLevel(world, stock, 3);
        SetLevel(world, purse, 10000);
        long price = world.DistrictPools.Price[market].Raw;
        long beforeTill = world.Bins.LevelAt(till);
        long beforeHome = world.Bins.LevelAt(home);
        world.Shopping.Wanted[row] = 40;
        sim.Shopping.Step(world.Tick);
        Assert.Equal(3, world.Shopping.Cargo[row]);
        Assert.Equal(0, world.Bins.LevelAt(stock));
        Assert.Equal(10000 - 3 * price, world.Bins.LevelAt(purse));
        Assert.Equal(beforeTill + 3 * price, world.Bins.LevelAt(till));
        Assert.Equal(beforeHome, world.Bins.LevelAt(home));
        Assert.Equal(1, world.Shopping.Attempts[row]);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void An_empty_shelf_allows_only_one_extra_known_shop_and_only_under_severe_hunger(bool severe, int attempts)
    {
        var (world, sim, row) = AtCounter();
        int business = world.Businesses.Rows.Resolve(world.Shopping.Seller[row]);
        int next = Enumerable.Range(0, world.Businesses.Rows.SlotCount).First(b => b != business
            && world.Businesses.Rows.IsLive(b)
            && BinOf(world, world.Businesses.BinHead[b], world.Shopping.Good[row]) >= 0);
        int entry = world.KnownShops.Rows.Resolve(world.KnownShops.Rows.Allocate());
        world.KnownShops.Business[entry] = world.Businesses.Rows.At(next);
        world.KnownShops.Next[entry] = world.Shopping.ProviderHead[row];
        world.Shopping.ProviderHead[row] = entry + 1;
        int stock = BinOf(world, world.Businesses.BinHead[business], world.Shopping.Good[row]);
        SetLevel(world, stock, 0);
        world.Shopping.Severe[row] = severe ? (byte)1 : (byte)0;
        world.Shopping.Attempts[row] = 1;
        sim.Shopping.Step(world.Tick);
        Assert.Equal(attempts, world.Shopping.Attempts[row]);
        Assert.Equal(0, world.Shopping.Cargo[row]);
        if (severe)
        {
            Assert.Equal(world.Businesses.Rows.At(next), world.Shopping.Seller[row]);
            // Reach the second counter; a second empty shelf must send the shopper home.
            while ((CitizenActivity)world.Citizens.Activity[world.Citizens.Rows.Resolve(world.Shopping.Citizen[row])]
                == CitizenActivity.ShoppingTravelling)
            {
                world.Clock.Tick[0] += new Ticks(1);
                sim.Trips.Advance(world.Tick);
            }
            SetLevel(world, BinOf(world, world.Businesses.BinHead[next], world.Shopping.Good[row]), 0);
            world.Clock.Tick[0] += new Ticks(1);
            sim.Shopping.Step(world.Tick);
            Assert.Equal(2, world.Shopping.Attempts[row]);
            Assert.NotEqual(1, world.Shopping.Stage[row]);
        }
    }

    [Fact]
    public void A_shop_that_closes_during_the_journey_cannot_sell()
    {
        var (world, sim, row) = AtCounter();
        world.Clock.Tick[0] = new Ticks((ulong)(Ticks.PerDay * 3 + Ticks.AtClock(22)));
        world.Shopping.Severe[row] = 0;
        sim.Shopping.Step(world.Tick);
        Assert.Equal(0, world.Shopping.Cargo[row]);
        Assert.Equal((byte)ShoppingFailure.Closed, world.Shopping.Reason[row]);
    }

    private static (World World, Simulation Sim, int Row) AtCounter(bool differentCell = false)
    {
        var (world, sim) = Start();
        for (int t = 0; t < 8192; t++)
        {
            sim.Step(default);
            for (int row = 0; row < world.Shopping.Rows.SlotCount; row++)
            {
                if (!world.Shopping.Rows.IsLive(row) || world.Shopping.Stage[row] != 1
                    || !world.Citizens.Rows.TryResolve(world.Shopping.Citizen[row], out int citizen)
                    || (CitizenActivity)world.Citizens.Activity[citizen] != CitizenActivity.ShoppingStopped
                    || (TripFate)world.Citizens.LastTripFate[citizen] != TripFate.Completed) { continue; }
                if (differentCell)
                {
                    int hh = world.Households.Rows.Resolve(world.Shopping.Household[row]);
                    int home = world.Buildings.Rows.Resolve(world.Households.Dwelling[hh]);
                    int homeLot = world.Lots.Rows.Resolve(world.Buildings.Lot[home]);
                    int shop = world.Buildings.Rows.Resolve(world.Shopping.Destination[row]);
                    int shopLot = world.Lots.Rows.Resolve(world.Buildings.Lot[shop]);
                    if (CellGrid.ToCells(world.Lots.East[homeLot]) == CellGrid.ToCells(world.Lots.East[shopLot])
                        && CellGrid.ToCells(world.Lots.North[homeLot]) == CellGrid.ToCells(world.Lots.North[shopLot])) { continue; }
                }
                int shops = Enumerable.Range(0, world.Businesses.Rows.SlotCount).Count(b => world.Businesses.Rows.IsLive(b)
                    && BinOf(world, world.Businesses.BinHead[b], world.Shopping.Good[row]) >= 0);
                if (shops < 2) { continue; }
                for (int other = 0; other < world.Shopping.Rows.SlotCount; other++)
                { if (other != row && world.Shopping.Rows.IsLive(other)) { world.Shopping.NextAt[other] = new Ticks(ulong.MaxValue); } }
                world.Shopping.NextAt[row] = Ticks.Zero;
                return (world, sim, row);
            }
        }
        throw new InvalidOperationException("Fixture never reached a shop counter with an alternative shop.");
    }

    private static int BinOf(World world, Borough.Core.Tables.Handle<Bin> head, ResourceId good)
    {
        for (var at = head; world.Bins.Rows.TryResolve(at, out int bin); at = world.Bins.OwnerNext[bin])
        { if (world.Bins.Resource[bin] == good) { return bin; } }
        return -1;
    }

    private static void SetLevel(World world, int bin, long value)
    {
        long before = world.Bins.LevelAt(bin);
        if (before > value) { world.Withdraw(world.Bins.Rows.At(bin), before - value, world.Tick); }
        else if (before < value) { world.Deposit(world.Bins.Rows.At(bin), value - before, world.Tick); }
    }

    [Fact]
    public void Moving_house_does_not_erase_goods_already_bought()
    {
        var (world, sim) = Start();
        int t = 0;
        while (Cargo(world) == 0 && t++ < 4096) { sim.Step(default); }
        Assert.True(Cargo(world) > 0);
        int row = Enumerable.Range(0, world.Shopping.Rows.SlotCount).First(r => world.Shopping.Rows.IsLive(r) && world.Shopping.Cargo[r] > 0);
        long cargo = world.Shopping.Cargo[row];
        world.Unplace(world.Shopping.Household[row]);
        Assert.Equal(cargo, world.Shopping.Cargo[row]);
        long balance = Cargo(world);
        for (t = 0; t < 512; t++)
        {
            sim.Step(default);
            var r = sim.Shopping.Last;
            balance += r.Bought - r.Delivered - r.Lost;
            Assert.Equal(balance, Cargo(world));
        }
        sim.CheckEndOfRun();
    }

    [Fact]
    public void The_weekend_starts_at_midnight_rather_than_the_simulation_day_boundary()
    {
        Assert.Equal(4, WeeklyHours.DayOf(4L * Ticks.PerDay + Ticks.AtClock(23)));
        Assert.Equal(5, WeeklyHours.DayOf(4L * Ticks.PerDay + Ticks.AtClock(0)));
    }

    [Fact]
    public void A_district_boundary_does_not_block_a_reached_shop_or_replace_its_price()
    {
        var (world, sim, row) = AtCounter(differentCell: true);
        int hh = world.Households.Rows.Resolve(world.Shopping.Household[row]);
        int home = world.Buildings.Rows.Resolve(world.Households.Dwelling[hh]);
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[home]);
        Cells east = CellGrid.ToCells(world.Lots.East[lot]);
        Cells north = CellGrid.ToCells(world.Lots.North[lot]);
        var original = world.DistrictsInCells.Of(world.DistrictCells, east, north);
        int other = Enumerable.Range(0, world.Districts.Rows.SlotCount).First(d => world.Districts.Rows.IsLive(d)
            && world.Districts.Rows.At(d) != original);
        for (int cell = 0; cell < world.DistrictCells.Rows.SlotCount; cell++)
        {
            if (world.DistrictCells.Rows.IsLive(cell) && world.DistrictCells.East[cell] == east && world.DistrictCells.North[cell] == north)
            { world.DistrictCells.District[cell] = world.Districts.Rows.At(other); }
        }
        world.DistrictsInCells.Rebuild(world.DistrictCells);
        world.Markets.Invalidate();
        int business = world.Businesses.Rows.Resolve(world.Shopping.Seller[row]);
        int bin = BinOf(world, world.Businesses.BinHead[business], world.Shopping.Good[row]);
        int market = world.Markets.MarketOf(world, bin);
        Assert.NotEqual(world.Districts.Rows.At(other), world.DistrictPools.District[market]);
        world.DistrictPools.Price[market] = new Money(7);
        int buyerMarket = world.Markets.Row(world, other, world.Shopping.Good[row]);
        world.DistrictPools.Price[buyerMarket] = new Money(99);
        int purse = world.Bins.Rows.Resolve(world.Households.Balance[hh]);
        SetLevel(world, purse, 1000);
        SetLevel(world, bin, 5);
        world.Shopping.Wanted[row] = 5;
        sim.Shopping.Step(world.Tick);
        Assert.Equal(5, world.Shopping.Cargo[row]);
        Assert.Equal(965, world.Bins.LevelAt(purse));
    }

    [Fact]
    public void A_failed_return_waits_before_another_route_search()
    {
        var (world, sim, row) = AtCounter();
        int citizen = world.Citizens.Rows.Resolve(world.Shopping.Citizen[row]);
        world.Shopping.Stage[row] = 2;
        world.Citizens.LastTripFate[citizen] = (byte)TripFate.NoRouteFound;
        sim.Shopping.Step(world.Tick);
        Assert.Equal(3, world.Shopping.Stage[row]);
        Ticks retry = world.Shopping.NextAt[row];
        Assert.Equal(world.Tick.Raw + (ulong)world.Rules.Shopping.RetryTicks, retry.Raw);
        sim.Shopping.Step(world.Tick + new Ticks(1));
        Assert.Equal(retry, world.Shopping.NextAt[row]);
        Assert.Equal(3, world.Shopping.Stage[row]);
    }

    [Fact]
    public void Losing_the_return_origin_records_the_cargo_loss_and_releases_the_shopper()
    {
        var (world, sim, row) = AtCounter();
        int citizen = world.Citizens.Rows.Resolve(world.Shopping.Citizen[row]);
        world.Shopping.Stage[row] = 3;
        world.Shopping.Place[row] = default;
        world.Shopping.Cargo[row] = 7;
        sim.Shopping.Step(world.Tick);
        Assert.Equal(7, sim.Shopping.Last.Lost);
        Assert.Equal(0, world.Shopping.Cargo[row]);
        Assert.Equal(0, world.Shopping.Stage[row]);
        Assert.Equal((byte)TripFate.Stranded, world.Citizens.LastTripFate[citizen]);
    }

    private static long Cargo(World world)
    {
        long amount = 0;
        for (int row = 0; row < world.Shopping.Rows.SlotCount; row++)
        { if (world.Shopping.Rows.IsLive(row)) { amount += world.Shopping.Cargo[row]; } }
        return amount;
    }
}
