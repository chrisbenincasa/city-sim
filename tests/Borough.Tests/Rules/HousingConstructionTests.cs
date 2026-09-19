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
using Borough.Tests.Space;

namespace Borough.Tests.Rules;

public sealed class HousingConstructionTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static readonly WorldKey Key = WorldKey.FromSeed(62002);
    private const string Settings = """
        [housing_construction]
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
        setback_tiles = 0
        weight = 4
        [[housing_form]]
        name = "courtyard"
        pattern = 3
        min_frontage_tiles = 24
        max_frontage_tiles = 24
        min_depth_tiles = 16
        max_depth_tiles = 16
        storeys = 2
        setback_tiles = 0
        weight = 4
        """;

    [Fact]
    public void Four_seekers_support_a_courtyard_or_two_terraces_but_only_first_commits()
    {
        var (world, lots) = City(4);
        var terrace = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        var second = LocalLayoutTests.Evaluate(world, [lots[2]], BlockPattern.BackToBack);
        var courtyard = LocalLayoutTests.Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        ulong before = All(world);
        Assert.Equal(4, HousingNeedAssessment.Evaluate(world, Key, terrace).Uncovered);
        Assert.Equal(4, HousingNeedAssessment.Evaluate(world, Key, terrace, second).Served);
        Assert.Equal(4, HousingNeedAssessment.Evaluate(world, Key, courtyard).Served);
        var selected = HousingConstruction.Select(world, Key, lots[1], 1);
        Assert.NotNull(selected);
        Assert.Equal(before, All(world));
        Assert.True(new Simulation(world, Key).CommitHousingLayout(terrace, out _).Accepted);
        Assert.Equal(4, world.UnplacedPool.Count);
        Assert.True(world.Lots.IsVacant(world.Lots.Rows.Resolve(lots[2])));
        HousingNeed residual = HousingNeedAssessment.Evaluate(world, Key, second);
        Assert.True(residual.Accepted);
        Assert.Equal(2, residual.Uncovered);
    }

    [Fact]
    public void Missed_vacancy_and_new_capacity_suppress_construction_across_rules_and_ticks()
    {
        var (world, lots) = City(4, fillNeighbours: false);
        var plan = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        ulong before = All(world);
        Assert.Equal(HousingNeedRefusal.NoNeed, HousingNeedAssessment.Evaluate(world, Key, plan).Refusal);
        Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
        Assert.Equal(before, All(world));
        var (shortage, sites) = City(4, zoneRules: true);
        var zoning = new ZoneRuleEngine(shortage, Key);
        zoning.Sweep(Ticks.Zero);
        Assert.Equal(4, Vacancy(shortage));
        int built = shortage.Buildings.Rows.LiveCount;
        for (int tick = 1; tick <= 4; tick++) { zoning.Sweep(new Ticks((ulong)tick)); }
        Assert.Equal(built, shortage.Buildings.Rows.LiveCount);
        Assert.Equal(4, shortage.UnplacedPool.Count);
        Assert.InRange(built, 3, 4);
    }

    [Fact]
    public void Commit_rechecks_demand_and_repaint_without_mutation()
    {
        var (world, lots) = City(2);
        var plan = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(HousingNeedAssessment.Evaluate(world, Key, plan).Accepted);
        world.CreateBuilding(lots[2], 1, world.Tick, Key);
        ulong before = All(world);
        Assert.Equal(LocalLayoutRefusal.HousingNeed, new Simulation(world, Key).CommitHousingLayout(plan, out var built).Refusal);
        Assert.True(built.IsNone);
        Assert.Equal(before, All(world));
        world.PaintFormPermissions(plan.Site, true, 1 << (int)BlockPattern.Courtyard);
        before = All(world);
        Assert.Equal(LocalLayoutRefusal.Permission, new Simulation(world, Key).CommitHousingLayout(plan, out _).Refusal);
        Assert.Equal(before, All(world));
    }

    [Theory]
    [InlineData("max_building_slots = 4096", "max_building_slots = 1")]
    [InlineData("max_lot_slots = 4096", "max_lot_slots = 4")]
    public void Incomplete_coverage_refuses_before_mutation(string key, string replacement)
    {
        var (world, lots) = City(4, settings: Settings.Replace(key, replacement, StringComparison.Ordinal));
        var plan = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        ulong before = All(world);
        long start = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(HousingNeedRefusal.CoverageLimit, HousingNeedAssessment.Evaluate(world, Key, plan).Refusal);
        Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.True(bytes < 1024, $"Refusal allocated {bytes} bytes.");
        Assert.Equal(before, All(world));
    }

    [Fact]
    public void Save_load_has_no_queued_remainder_and_recomputes_current_evidence()
    {
        var (world, lots) = City(4);
        var plan = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(new Simulation(world, Key).CommitHousingLayout(plan, out var built).Accepted);
        var file = new MemorySave();
        SaveFile.Write(world, 62002, file);
        World loaded = SaveFile.Read(file, world.Rules, out _);
        Assert.Equal(world.HashState(), loaded.HashState());
        foreach (World city in new[] { world, loaded })
        {
            var remaining = LocalLayoutTests.Evaluate(city, [lots[2]], BlockPattern.BackToBack);
            Assert.Equal(2, HousingNeedAssessment.Evaluate(city, Key, remaining).Uncovered);
            city.PaintFormPermissions(remaining.Site, true, 1 << (int)BlockPattern.Courtyard);
            Assert.Null(HousingConstruction.Select(city, Key, lots[2], 1));
            Assert.True(city.Lots.IsVacant(city.Lots.Rows.Resolve(lots[2])));
        }
        Assert.Equal(world.HashState(), loaded.HashState());
        var a = new Simulation(world, Key) { RouteWorkerCount = 1, VerifyDecideWritesNothing = true };
        var b = new Simulation(loaded, Key) { RouteWorkerCount = 2, VerifyDecideWritesNothing = true };
        for (int tick = 0; tick < 32; tick++) { a.Step(default); b.Step(default); Assert.Equal(world.HashState(), loaded.HashState()); }
        a.CheckEndOfRun(); b.CheckEndOfRun();
    }

    [Fact]
    public void Surplus_is_bounded_and_zero_need_never_builds()
    {
        var (world, lots) = City(1);
        var plan = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.Equal(HousingNeedRefusal.ExcessCapacity, HousingNeedAssessment.Evaluate(world, Key, plan).Refusal);
        var (allowed, sites) = City(1, settings: Settings.Replace("surplus_percent = 0", "surplus_percent = 100", StringComparison.Ordinal)
            .Replace("max_surplus = 0", "max_surplus = 1", StringComparison.Ordinal));
        var small = LocalLayoutTests.Evaluate(allowed, [sites[1]], BlockPattern.BackToBack);
        Assert.True(new Simulation(allowed, Key).CommitHousingLayout(small, out _).Accepted);
        Assert.Null(HousingConstruction.Select(allowed, Key, sites[2], 1));
    }

    [Fact]
    public void Form_capacity_must_be_earnable_within_the_assessment_limit()
    {
        string text = File.ReadAllText(Path.Combine(FindRoot(), "rulesets", "urban-housing.toml"));
        var result = RulesetLoader.Parse(text.Replace("max_seekers = 16", "max_seekers = 2", StringComparison.Ordinal), "too-small.toml");
        Assert.False(result.Ok);
        Assert.Contains("maximum supported capacity", result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_flexible_seeker_cannot_consume_the_only_affordable_vacancy_as_evidence()
    {
        var (world, lots) = City(2, fillNeighbours: false, settings: Settings + MoneyKinds);
        int west = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[0]));
        int east = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[4]));
        world.Buildings.Kind[east] = 2;
        world.CreateHousehold(world.Buildings.Rows.At(west), 0);
        world.CreateHousehold(world.Buildings.Rows.At(east), 0);
        int first = (int)(Randomness.Draw(Key, 0, world.Tick, PurposeTag.HousingConstructionSeekers) % 2);
        world.Endow(world.UnplacedPool.At(first), new Money(100));
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.Equal(2, Vacancy(world));
        Assert.Equal(HousingNeedRefusal.NoNeed, HousingNeedAssessment.Evaluate(world, Key, proposal).Refusal);
    }

    [Fact]
    public void Unaffordable_prospective_homes_do_not_turn_seekers_into_demand()
    {
        var (world, lots) = City(2, settings: Settings + MoneyKinds);
        var shape = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(LocalLayout.Evaluate(world, [lots[1]], shape.Building with { Kind = 2 }, out var costly).Accepted);
        Assert.Equal(HousingNeedRefusal.NoNeed, HousingNeedAssessment.Evaluate(world, Key, costly!).Refusal);
        for (int i = 0; i < 2; i++) { world.Endow(world.UnplacedPool.At(i), new Money(100)); }
        Assert.True(HousingNeedAssessment.Evaluate(world, Key, costly!).Accepted);
    }

    [Theory]
    [InlineData(25, false)]
    [InlineData(50, false)]
    [InlineData(75, true)]
    public void Actual_households_compare_the_prospective_site_with_their_saved_Outside(int outsideRent, bool supported)
    {
        string choice = """
            [placement]
            interval = 32
            revisit_ticks = 1024
            candidates = 3
            gives_up_after_days = 2
            mu_percent = 100
            centrality_tiles_per_unit = 2048
            rent_per_unit = 120
            moving_costs_rent = 720
            [[hinterland]]
            edge = "west"
            rent = OUTSIDE
            centrality_tiles = 0
            emigrant_balance_min = 100
            emigrant_balance_max = 100
            """;
        var (world, lots) = City(2, settings: Settings + MoneyKinds + "\n" + choice.Replace("OUTSIDE", outsideRent.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        for (int i = 0; i < 2; i++)
        {
            var household = world.UnplacedPool.At(i);
            int row = world.Households.Rows.Resolve(household);
            world.Endow(household, new Money(100));
            world.Households.Arrived[row] = 1;
            world.Households.ArrivalEdge[row] = (byte)MapEdge.West;
        }
        var shape = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(LocalLayout.Evaluate(world, [lots[1]], shape.Building with { Kind = 2 }, out var costly).Accepted);
        Assert.Equal(supported, HousingNeedAssessment.Evaluate(world, Key, costly!).Accepted);
    }

    [Fact]
    public void One_seeker_is_never_extrapolated_and_arrangement_draws_are_reproducible()
    {
        var (world, lots) = City(40);
        var shape = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.Equal(16, HousingNeedAssessment.Evaluate(world, Key, shape).Assessed);
        var (small, sites) = City(4);
        var forms = new HashSet<BlockPattern>();
        for (ulong seed = 0; seed < 32; seed++)
        {
            var key = WorldKey.FromSeed(seed);
            var selected = HousingConstruction.Select(small, key, sites[1], 1);
            var again = HousingConstruction.Select(small, key, sites[1], 1);
            Assert.NotNull(selected); Assert.NotNull(again);
            Assert.Equal(selected.Building, again.Building);
            forms.Add(selected.Building.Form);
        }
        Assert.Contains(BlockPattern.BackToBack, forms);
        Assert.Contains(BlockPattern.Courtyard, forms);
    }

    [Fact]
    public void A_fitted_business_consumes_shared_capacity_in_both_evidence_and_commit()
    {
        var (world, lots) = LocalLayoutTests.Fixture(mixed: true, extra: Settings);
        int west = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[0]));
        for (int i = 0; i < 2; i++) { world.Unplace(world.CreateHousehold(world.Buildings.Rows.At(west), 0)); }
        foreach (int index in new[] { 0, 4 })
        {
            int row = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[index]));
            while (world.HasRoomForHousehold(row)) { world.CreateHousehold(world.Buildings.Rows.At(row), 0); }
        }
        var first = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.Equal(1, first.HousingCapacity);
        Assert.True(new Simulation(world, Key).CommitHousingLayout(first, out _).Accepted);
        var next = LocalLayoutTests.Evaluate(world, [lots[2]], BlockPattern.BackToBack);
        Assert.Equal(1, HousingNeedAssessment.Evaluate(world, Key, next).Uncovered);
        Assert.Equal(1, Vacancy(world));
    }

    [Fact]
    public void Save_load_continues_automatic_construction_with_thread_equivalence()
    {
        var (world, lots) = City(4, zoneRules: true);
        var first = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(new Simulation(world, Key).CommitHousingLayout(first, out _).Accepted);
        var file = new MemorySave(); SaveFile.Write(world, 62002, file);
        var loaded = SaveFile.Read(file, world.Rules, out _);
        var a = new Simulation(world, Key) { RouteWorkerCount = 1, VerifyDecideWritesNothing = true };
        var b = new Simulation(loaded, Key) { RouteWorkerCount = 2, VerifyDecideWritesNothing = true };
        for (int tick = 0; tick < 32; tick++) { a.Step(default); b.Step(default); Assert.Equal(world.HashState(), loaded.HashState()); }
        Assert.Equal(4, Vacancy(world));
        Assert.Equal(4, world.Buildings.Rows.LiveCount);
        a.CheckEndOfRun(); b.CheckEndOfRun();
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Report_allocation_for_bounded_walkthrough_selection()
    {
        var (world, lots) = City(4);
        HousingConstruction.Select(world, Key, lots[1], 1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var selected = HousingConstruction.Select(world, Key, lots[1], 1);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.NotNull(selected);
        output.WriteLine($"Release walkthrough: 5 Lots, 2 full Buildings, 4 seekers, source limit 3, candidate attempt limit 32. Selection allocated {bytes} managed bytes.");
    }

    private const string MoneyKinds = """

        [[resource]]
        name = "money"
        family = "money"
        [[building]]
        name = "expensive"
        houses = true
        rent = 50
        """;

    private static (World World, Handle<Lot>[] Lots) City(int seekers, bool fillNeighbours = true, bool zoneRules = false, string? settings = null)
    {
        string extra = settings ?? Settings;
        if (zoneRules) { extra += "\n[[zone_rule]]\nname = \"housing\"\nkind = \"dwelling\"\nzone = 0\ninterval = 1\nrevisit_ticks = 1\n[[zone_rule]]\nname = \"more-housing\"\nkind = \"dwelling\"\nzone = 0\ninterval = 1\nrevisit_ticks = 1\n"; }
        var (world, lots) = LocalLayoutTests.Fixture(extra: extra);
        var shelter = world.Buildings.Rows.At(world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[0])));
        for (int i = 0; i < seekers; i++) { world.Unplace(world.CreateHousehold(shelter, 0)); }
        if (fillNeighbours)
        {
            foreach (int index in new[] { 0, 4 })
            {
                int building = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[index]));
                while (world.HasRoomForHousehold(building)) { world.CreateHousehold(world.Buildings.Rows.At(building), 0); }
            }
        }
        return (world, lots);
    }

    private static int Vacancy(World world)
    {
        int total = 0;
        for (int row = 0; row < world.Buildings.Rows.SlotCount; row++)
        { if (world.Buildings.Rows.IsLive(row) && world.HasRoomForHousehold(row)) { world.TryDeclaredOccupancy(1, row, out int capacity); total += capacity - world.Tenants(row); } }
        return total;
    }
    private static ulong All(World world) { ulong hash = world.HashState(); foreach (Rows rows in world.Tables) { rows.FoldAll(ref hash); } return hash; }
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Borough.slnx"))) { directory = directory.Parent; }
        return directory!.FullName;
    }
}
