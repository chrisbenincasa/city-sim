using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Persistence;
using Xunit.Abstractions;

namespace Borough.Tests.Entities;

public sealed class JobsAttractTests(ITestOutputHelper output)
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    public static Ruleset Rules(string? text = null)
    {
        text ??= File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        RulesetLoadResult result = RulesetLoader.Parse(text, "jobs-attract.toml");
        return result.Ruleset ?? throw new InvalidOperationException(result.Describe());
    }

    public static World City(int population = 1_000, Ruleset? rules = null)
    {
        var world = new World(population, rules ?? Rules(), Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        return world;
    }

    [Fact]
    public void Whole_count_log_is_monotone_and_has_diminishing_returns()
    {
        Assert.Equal(0, Transcendental.Log1PCount(0));
        Assert.InRange(Transcendental.Log1PCount(1), Transcendental.Ln2 - 2, Transcendental.Ln2 + 2);
        Assert.True(Transcendental.Log1PCount(2) - Transcendental.Log1PCount(1)
            < Transcendental.Log1PCount(1));
        Assert.True(Transcendental.Log1PCount(4_000_000_000) > Transcendental.Log1PCount(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => Transcendental.Log1PCount(-1));
        Assert.Equal(0, HousingUtility.JobsWorth(Rules().Placement, 0));
    }

    [Theory]
    [InlineData("jobs_rent = -1")]
    [InlineData("jobs_rent = 2147483648")]
    [InlineData("jobs_rent = 1.5")]
    public void Invalid_jobs_weights_are_refused(string authored)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        Assert.Null(RulesetLoader.Parse(text.Replace("jobs_rent = 240", authored, StringComparison.Ordinal), "test").Ruleset);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(2147483648L)]
    public void Invalid_Outside_job_counts_are_refused(long count)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        Assert.Null(RulesetLoader.Parse(text.Replace("reachable_jobs = 8", $"reachable_jobs = {count}", StringComparison.Ordinal), "test").Ruleset);
    }

    [Fact]
    public void An_authored_jobs_weight_requires_the_choice_model()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        text = System.Text.RegularExpressions.Regex.Replace(text,
            @"(?m)^(mu_percent|centrality_tiles_per_unit|rent_per_unit|moving_costs_rent)\s*=.*\n", "");
        RulesetLoadResult result = RulesetLoader.Parse(text, "no-choice.toml");
        Assert.Null(result.Ruleset);
        Assert.Contains("jobs_rent", result.Describe(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("commute_budget_minutes = 50\n")]
    [InlineData("[trips]\ncrossing_seconds = 30\ncommute_fast_minutes = 20\ncommute_moderate_minutes = 40\ncommute_budget_minutes = 50\n")]
    public void An_authored_jobs_weight_requires_a_commute_budget(string removed)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"))
            .ReplaceLineEndings("\n");
        Assert.Contains(removed, text, StringComparison.Ordinal);
        RulesetLoadResult result = RulesetLoader.Parse(text.Replace(removed, "", StringComparison.Ordinal), "no-trips.toml");
        Assert.Null(result.Ruleset);
        Assert.Contains("jobs_rent", result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Turning_the_jobs_weight_off_clears_reachable_counts()
    {
        World world = City();
        world.JobAccess.Rebuild(world);
        int lot = PositiveLot(world);

        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        world.Adopt(Rules(text.Replace("jobs_rent = 240", "jobs_rent = 0", StringComparison.Ordinal)), 9, world.Tick, Key);
        world.JobAccess.Refresh(world);

        Assert.Equal(0, world.JobAccess.Count(lot, 3));
    }

    [Fact]
    public void Zero_jobs_weight_is_valid_without_a_choice_model()
    {
        const string text = """
            [[resource]]
            name = "money"
            family = "money"
            [[building]]
            name = "home"
            houses = true
            [placement]
            interval = 32
            revisit_ticks = 1024
            candidates = 3
            gives_up_after_days = 2
            jobs_rent = 0
            """;
        RulesetLoadResult result = RulesetLoader.Parse(text, "legacy-zero-jobs.toml");
        Assert.True(result.Ok, result.Describe());
        Assert.Equal(0, result.Ruleset!.Placement.JobsRent);
    }

    [Fact]
    public void Missing_keys_preserve_existing_rulesets()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml"));
        Ruleset rules = Rules(text);
        Assert.Equal(0, rules.Placement.JobsRent);
        Assert.All(rules.Hinterlands, row => Assert.Equal(0, row.ReachableJobs));
    }

    [Fact]
    public void Vacancies_refresh_when_employers_close_and_cache_hits_do_no_walks()
    {
        World world = City();
        world.JobAccess.Refresh(world);
        int lot = PositiveLot(world);
        long before = world.JobAccess.Count(lot, 3);
        Assert.True(before > 0);
        world.JobAccess.Refresh(world);
        Assert.Equal(0, world.JobAccess.Searches);
        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business)) { continue; }
            world.DestroyBusiness(world.Businesses.Rows.At(business));
        }
        world.JobAccess.Refresh(world);
        Assert.Equal(0, world.JobAccess.Count(lot, 3));
    }

    [Fact]
    public void Filling_one_post_invalidates_the_snapshot()
    {
        World world = City();
        world.JobAccess.Refresh(world);
        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business) || !world.HasJob(business)) { continue; }
            int building = world.Buildings.Rows.Resolve(world.Businesses.Building[business]);
            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
            long before = world.JobAccess.Count(lot, 3);
            Assert.True(before > 0);
            var citizen = world.CreateCitizen(world.Households.Rows.At(0));
            world.Employ(citizen, world.Businesses.Rows.At(business), Ticks.Zero);
            world.JobAccess.Refresh(world);
            Assert.Equal(before - 1, world.JobAccess.Count(lot, 3));
            return;
        }
        Assert.Fail("fixture has no hiring Business");
    }

    [Fact]
    public void More_reachable_jobs_draw_more_actual_arrivals()
    {
        World jobs = City();
        World none = City();
        for (int business = 0; business < none.Businesses.Rows.SlotCount; business++)
        {
            if (none.Businesses.Rows.IsLive(business))
            { none.DestroyBusiness(none.Businesses.Rows.At(business)); }
        }
        var withJobs = new Simulation(jobs, Key) { VerifyDecideWritesNothing = false };
        var without = new Simulation(none, Key) { VerifyDecideWritesNothing = false };
        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            withJobs.Step(default);
            without.Step(default);
        }
        long with = Admitted(jobs);
        long absent = Admitted(none);
        output.WriteLine($"Day arrivals with jobs {with}, without {absent}; walks {jobs.JobAccess.TotalSearches}, settled {jobs.JobAccess.TotalSettled}.");
        Assert.Equal(256, with);
        Assert.Equal(238, absent);
        Assert.True(with > absent, $"jobs {with}, none {absent}");
    }

    [Fact]
    public void Input_Log_replay_with_jobs_preserves_every_tick_across_worker_counts()
    {
        Ruleset rules = Rules();
        var builder = new InputLogBuilder(7, new WorldConfiguration(1_000), 7);
        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        InputLog log = builder.Build();
        Simulation serial = Replay.Start(log, rules);
        Simulation parallel = Replay.Start(log, rules);
        serial.VerifyDecideWritesNothing = false;
        parallel.VerifyDecideWritesNothing = false;
        parallel.RouteWorkerCount = 8;
        for (ulong tick = 0; tick < 128; tick++)
        {
            var input = new TickInput(log.At(new Ticks(tick)), log.RulesetHashAt(new Ticks(tick)));
            serial.Step(input);
            parallel.Step(input);
            Assert.Equal(serial.World.HashState(), parallel.World.HashState());
        }
        Assert.True(serial.World.JobAccess.TotalSearches > 0);
        serial.CheckEndOfRun();
        parallel.CheckEndOfRun();
    }

    [Fact]
    public void Save_reload_reconstructs_jobs_and_continues_identically()
    {
        Ruleset rules = Rules();
        World world = City(rules: rules);
        var live = new Simulation(world, Key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 1 };
        for (int tick = 0; tick < 33; tick++) { live.Step(default); }
        world.JobAccess.Refresh(world);
        int lot = PositiveLot(world);
        var file = new MemorySave();
        SaveFile.Write(world, 7, file);
        World reloaded = SaveFile.Read(file, rules, out _);
        Assert.Equal(world.JobAccess.Count(lot, 3), reloaded.JobAccess.Count(lot, 3));
        var loaded = new Simulation(reloaded, Key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 8 };
        for (int tick = 0; tick < 64; tick++)
        {
            live.Step(default);
            loaded.Step(default);
            Assert.Equal(world.HashState(), reloaded.HashState());
        }
    }

    [Fact]
    public void Credentials_and_hot_reload_change_qualified_counts()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        World world = City(rules: Rules(text.Replace("name = \"shop\"", "name = \"shop\"\nrequires_tier = 2", StringComparison.Ordinal)));
        world.JobAccess.Refresh(world);
        int lot = PositiveLot(world);
        Assert.Equal(0, world.JobAccess.Count(lot, 1));
        Assert.Equal(world.JobAccess.Count(lot, 2), world.JobAccess.Count(lot, 3));
        world.Adopt(Rules(), 8, world.Tick, Key);
        world.JobAccess.Refresh(world);
        Assert.True(world.JobAccess.Count(lot, 1) > 0);
    }

    [Fact]
    public void A_stronger_Outside_draws_fewer_prospects_and_extra_adults_do_not_multiply_jobs()
    {
        World world = City();
        var simulation = new Simulation(world, Key);
        var gate = world.Buildings.Rows.At(0);
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.Buildings.Rows.IsLive(building) && world.IsOutsideConnection(world.Buildings.Kind[building]))
            { gate = world.Buildings.Rows.At(building); break; }
        }
        var composition = HinterlandComposition.Of(world.Rules.HinterlandPopulations[0]);
        int before = 0;
        for (ulong identity = 1; identity <= 500; identity++)
        {
            var prospect = new ArrivalProspect(default, MapEdge.West, composition, new Money(20_000), identity);
            ProspectOutcome single = simulation.Placement.Compare(prospect, gate, Ticks.Zero);
            Assert.Equal(single, simulation.Placement.Compare(prospect with
            { Composition = composition with { AdultsTier1 = 2 } }, gate, Ticks.Zero));
            if (single == ProspectOutcome.Willing) { before++; }
        }
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"));
        world.Adopt(Rules(text.Replace("reachable_jobs = 8", "reachable_jobs = 80000", StringComparison.Ordinal)), 9, world.Tick, Key);
        int after = 0;
        for (ulong identity = 1; identity <= 500; identity++)
        {
            var prospect = new ArrivalProspect(default, MapEdge.West, composition, new Money(20_000), identity);
            if (simulation.Placement.Compare(prospect, gate, Ticks.Zero) == ProspectOutcome.Willing) { after++; }
        }
        Assert.True(before > after, $"Outside jobs increase, willingness {before} -> {after}");
    }

    [Fact]
    public void Each_prospect_weighs_the_Outside_jobs_of_its_own_edge()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "jobs-attract.toml"))
            .ReplaceLineEndings("\n");
        const string west = "edge = \"west\"\n";
        int at = text.IndexOf(west, StringComparison.Ordinal);
        int jobs = text.IndexOf("reachable_jobs = 8\n", at, StringComparison.Ordinal);
        Assert.True(at >= 0 && jobs > at && text.IndexOf("[[hinterland]]", at, StringComparison.Ordinal) > jobs);
        text = text[..jobs] + "reachable_jobs = 80000\n" + text[(jobs + "reachable_jobs = 8\n".Length)..];

        World world = City(rules: Rules(text));
        var simulation = new Simulation(world, Key);
        var gate = world.Buildings.Rows.At(0);
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.Buildings.Rows.IsLive(building) && world.IsOutsideConnection(world.Buildings.Kind[building]))
            { gate = world.Buildings.Rows.At(building); break; }
        }
        var composition = HinterlandComposition.Of(world.Rules.HinterlandPopulations[0]);
        int fromWest = 0, fromEast = 0;
        for (ulong identity = 1; identity <= 500; identity++)
        {
            var prospect = new ArrivalProspect(default, MapEdge.West, composition, new Money(20_000), identity);
            if (simulation.Placement.Compare(prospect, gate, Ticks.Zero) == ProspectOutcome.Willing) { fromWest++; }
            if (simulation.Placement.Compare(prospect with { Edge = MapEdge.East }, gate, Ticks.Zero) == ProspectOutcome.Willing) { fromEast++; }
        }
        Assert.True(fromEast > fromWest, $"willing from the east {fromEast}, from the strong west {fromWest}");
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void First_Day_State_Hash_trace()
    {
        World world = City();
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 1 };
        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);
            if (world.Tick.Raw % 32 == 0)
            { output.WriteLine($"Hash {world.Tick.Raw} {world.HashState():X16}"); }
        }
    }

    [Theory]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Accessibility_work_per_Day(int population)
    {
        World world = City(population);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 1 };
        for (int tick = 0; tick < Ticks.PerDay; tick++) { simulation.Step(default); }
        output.WriteLine($"population {population}; Lots {world.Lots.Rows.LiveCount}; Businesses {world.Businesses.Rows.LiveCount}; admissions {Admitted(world)}; Day walks {world.JobAccess.TotalSearches}; settled {world.JobAccess.TotalSettled}");
    }

    public static int PositiveLot(World world)
    {
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        { if (world.JobAccess.Count(lot, 3) > 0) { return lot; } }
        throw new InvalidOperationException("no Lot has qualified reachable vacancies");
    }

    private static long Admitted(World world)
    {
        long count = 0;
        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        { count += world.Hinterlands.AdmittedHouseholds[edge]; }
        return count;
    }
}
