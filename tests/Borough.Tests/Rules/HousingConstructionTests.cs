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

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Preference_evidence_measures_elapsed_observed_ticks_not_search_count(int cadence)
    {
        var (world, lots) = MismatchCity();
        for (int tick = 0; tick <= 8; tick++)
        {
            AtTick(world, tick);
            if (tick % cadence == 0) { ObserveAll(world); }
            var plan = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
            ulong before = All(world);
            Assert.Equal(tick == 8, HousingNeedAssessment.Evaluate(world, Key, plan).Accepted);
            Assert.Equal(before, All(world));
        }
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(new Simulation(world, Key).CommitHousingLayout(proposal, out _).Accepted);
        Assert.Equal(6, Vacancy(world)); // Poor homes remain; the two new homes cover both seekers.
        Assert.Null(HousingConstruction.Select(world, Key, lots[2], 1));
        ObserveAll(world);
        Assert.Equal((byte)HousingSearchReason.Suitable, world.UnplacedPool.SearchReason[0]);
        Assert.Equal(0UL, world.UnplacedPool.MismatchSince[0]);
    }

    [Fact]
    public void Unsampled_waits_and_duplicate_observations_do_not_certify_persistence()
    {
        var (world, lots) = MismatchCity();
        AtTick(world, 40); // Time already spent in the Pool supplies no evidence.
        for (int i = 0; i < 10; i++) { ObserveAll(world); }
        Assert.Equal(40UL, world.UnplacedPool.MismatchSince[0]);
        AtTick(world, 48);
        Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
        ObserveAll(world); // The eight-Tick gap exceeds freshness and restarts the episode.
        Assert.Equal(48UL, world.UnplacedPool.MismatchSince[0]);
        AtTick(world, 52); ObserveAll(world);
        AtTick(world, 56); ObserveAll(world);
        Assert.NotNull(HousingConstruction.Select(world, Key, lots[1], 1));
        AtTick(world, 61);
        Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Changed_reason_clears_episode_and_capacity_shortage_does_not_wait(bool unaffordable)
    {
        var (world, lots) = MismatchCity();
        ObserveAll(world); AtTick(world, 4); ObserveAll(world);
        if (unaffordable)
        {
            for (int i = 0; i < world.UnplacedPool.Count; i++)
            { world.Withdraw(world.Households.Balance[world.Households.Rows.Resolve(world.UnplacedPool.At(i))], 100, world.Tick); }
        }
        else
        {
            foreach (int index in new[] { 0, 4 })
            {
                int row = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[index]));
                while (world.HasRoomForHousehold(row)) { world.CreateHousehold(world.Buildings.Rows.At(row), 0); }
            }
        }
        ObserveAll(world);
        Assert.Equal((byte)(unaffordable ? HousingSearchReason.Affordability : HousingSearchReason.Capacity), world.UnplacedPool.SearchReason[0]);
        Assert.Equal(0UL, world.UnplacedPool.MismatchObserved[0]);
        Assert.NotNull(HousingConstruction.Select(world, Key, lots[1], 1));
        if (unaffordable)
        {
            AtTick(world, 8);
            for (int i = 0; i < world.UnplacedPool.Count; i++) { world.Endow(world.UnplacedPool.At(i), new Money(100)); }
            ObserveAll(world);
            Assert.Equal(8UL, world.UnplacedPool.MismatchSince[0]);
            Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
        }
    }

    [Fact]
    public void Current_alternatives_and_proposed_rent_override_old_evidence_without_mutating_a_refusal()
    {
        var (world, lots) = MismatchCity();
        Mature(world);
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        Assert.True(LocalLayout.Evaluate(world, [lots[1]], proposal.Building with { Kind = 2 }, out var worse).Accepted);
        Assert.False(HousingNeedAssessment.Evaluate(world, Key, worse!).Accepted);
        int west = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[0]));
        world.Buildings.Kind[west] = 1; // Newly suitable capacity without another placement pass.
        ulong before = All(world);
        Assert.Equal(LocalLayoutRefusal.HousingNeed, new Simulation(world, Key).CommitHousingLayout(proposal, out _).Refusal);
        Assert.Equal(before, All(world));
        ObserveAll(world);
        Assert.Equal((byte)HousingSearchReason.Suitable, world.UnplacedPool.SearchReason[0]);
    }

    [Fact]
    public void Small_preference_gap_and_Outside_tie_never_accumulate_evidence()
    {
        foreach (int outside in new[] { 45, 50, 75 })
        {
            var (world, lots) = MismatchCity(outside: outside);
            Mature(world);
            Assert.Equal((byte)HousingSearchReason.Suitable, world.UnplacedPool.SearchReason[0]);
            Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
        }
    }

    [Fact]
    public void Pool_swap_reentry_and_ruleset_reload_preserve_or_clear_the_right_history()
    {
        var (world, lots) = MismatchCity();
        AtTick(world, 2); HousingSearchEvidence.Observe(world, Key, 1, world.Tick);
        AtTick(world, 4); HousingSearchEvidence.Observe(world, Key, 0, world.Tick);
        var leaver = world.UnplacedPool.At(0);
        var mover = world.UnplacedPool.At(1);
        var shelter = world.Buildings.Rows.At(world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[0])));
        world.Place(leaver, shelter);
        Assert.Equal(mover, world.UnplacedPool.At(0));
        Assert.Equal(2UL, world.UnplacedPool.MismatchSince[0]);
        world.Unplace(leaver);
        Assert.Equal((byte)HousingSearchReason.None, world.UnplacedPool.SearchReason[1]);
        Assert.Equal(0UL, world.UnplacedPool.MismatchSince[1]);
        world.Adopt(world.Rules, 1, world.Tick, Key);
        Assert.Equal((byte)HousingSearchReason.None, world.UnplacedPool.SearchReason[0]);
    }

    [Fact]
    public void Mid_episode_save_replay_and_route_threads_continue_real_search_and_construction()
    {
        var (world, lots) = MismatchCity(zoneRules: true);
        var simulation = new Simulation(world, Key) { RouteWorkerCount = 1, VerifyDecideWritesNothing = true };
        for (int tick = 0; tick < 5; tick++) { simulation.Step(default); }
        Assert.Equal(2, world.Buildings.Rows.LiveCount);
        Assert.Equal((byte)HousingSearchReason.Preference, world.UnplacedPool.SearchReason[0]);
        Assert.Equal(0UL, world.UnplacedPool.MismatchSince[0]);
        var file = new MemorySave(); SaveFile.Write(world, 62002, file);
        World loaded = SaveFile.Read(file, world.Rules, out _);
        Assert.Equal(world.HashState(), loaded.HashState());
        var resumed = new Simulation(loaded, Key) { RouteWorkerCount = 2, VerifyDecideWritesNothing = true };
        for (int tick = 5; tick < 32; tick++)
        {
            simulation.Step(default); resumed.Step(default);
            Assert.Equal(world.HashState(), loaded.HashState());
        }
        Assert.Equal(3, world.Buildings.Rows.LiveCount);
        simulation.CheckEndOfRun(); resumed.CheckEndOfRun();
    }

    [Theory]
    [InlineData("preference_persistence_ticks = 1024", "preference_persistence_ticks = 0")]
    [InlineData("preference_freshness_ticks = 2048", "preference_freshness_ticks = -1")]
    [InlineData("preference_margin_percent = 25", "preference_margin_percent = 10001")]
    [InlineData("preference_margin_percent = 25", "")]
    public void Invalid_or_missing_persistence_tuning_is_refused(string oldValue, string newValue)
    {
        string text = File.ReadAllText(Path.Combine(FindRoot(), "rulesets", "urban-housing.toml"));
        Assert.False(RulesetLoader.Parse(text.Replace(oldValue, newValue, StringComparison.Ordinal), "invalid.toml").Ok);
    }

    [Fact]
    public void Coverage_loss_clears_evidence_and_episode_clock_invariant_catches_corruption()
    {
        var (world, lots) = MismatchCity();
        ObserveAll(world);
        world.UnplacedPool.MismatchObserved[0] = 1;
        var failure = Assert.Throws<Borough.Core.Invariants.InvariantViolationException>(() => new Simulation(world, Key).CheckEndOfRun());
        Assert.Contains("HousingSearchEvidenceIsWellFormed", failure.Message, StringComparison.Ordinal);
        world.UnplacedPool.MismatchObserved[0] = 0;
        for (int i = world.Lots.Rows.SlotCount; i <= 4096; i++)
        { world.Lots.Create(new Tiles(1000 + i), new Tiles(1000), 0); }
        ObserveAll(world);
        Assert.Equal((byte)HousingSearchReason.CoverageLimit, world.UnplacedPool.SearchReason[0]);
        Assert.Equal(0UL, world.UnplacedPool.MismatchObserved[0]);
        Assert.Null(HousingConstruction.Select(world, Key, lots[1], 1));
    }

    private static void AtTick(World world, int tick)
    { while (world.Tick.Raw < (ulong)tick) { world.Advance(); } }
    private static void ObserveAll(World world)
    { for (int i = 0; i < world.UnplacedPool.Count; i++) { HousingSearchEvidence.Observe(world, Key, i, world.Tick); } }
    private static void Mature(World world)
    { ObserveAll(world); AtTick(world, 4); ObserveAll(world); AtTick(world, 8); ObserveAll(world); }

    private static (World World, Handle<Lot>[] Lots) MismatchCity(bool zoneRules = false, int outside = 25)
    {
        string choice = $"""

            [placement]
            interval = 2
            revisit_ticks = 2
            candidates = 3
            gives_up_after_days = 2
            mu_percent = 10000
            centrality_tiles_per_unit = 2048
            rent_per_unit = 120
            moving_costs_rent = 0
            [[hinterland]]
            edge = "west"
            rent = {outside}
            centrality_tiles = 0
            emigrant_balance_min = 100
            emigrant_balance_max = 100
            """;
        string settings = Settings.Replace("preference_persistence_ticks = 1024", "preference_persistence_ticks = 8", StringComparison.Ordinal)
            .Replace("preference_freshness_ticks = 2048", "preference_freshness_ticks = 4", StringComparison.Ordinal)
            .Replace("preference_margin_percent = 25", "preference_margin_percent = 10", StringComparison.Ordinal);
        var (world, lots) = City(2, fillNeighbours: false, zoneRules: zoneRules, settings: settings + MoneyKinds + choice);
        foreach (int index in new[] { 0, 4 })
        { world.Buildings.Kind[world.Lots.BuildingOn(world.Lots.Rows.Resolve(lots[index]))] = 2; }
        for (int i = 0; i < world.UnplacedPool.Count; i++)
        {
            var household = world.UnplacedPool.At(i);
            int row = world.Households.Rows.Resolve(household);
            world.Endow(household, new Money(100));
            world.Households.Arrived[row] = 1;
            world.Households.ArrivalEdge[row] = (byte)MapEdge.West;
        }
        return (world, lots);
    }

    private const string MoneyKinds = """

        [[resource]]
        name = "money"
        family = "money"
        [[building]]
        name = "expensive"
        houses = true
        rent = 50
        bins = [{ resource = "sundries", capacity = 48 }]
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
