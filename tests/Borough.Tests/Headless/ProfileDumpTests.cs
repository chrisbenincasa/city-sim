using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Formats;
using Borough.Headless;
using Borough.Core.Movement;
using Borough.Core.Space;
using Borough.Tests.Space;

namespace Borough.Tests.Headless;

public sealed class ProfileDumpTests
{
    [Fact]
    public void Cross_tick_reuse_is_bounded_versioned_and_counts_saved_search_work()
    {
        var graph = RoadFixtures.Chain(4);
        var other = RoadFixtures.Chain(4);
        var probe = new RouteReuseWindow(2);
        var from = Address.On(0, new Tiles(4), StreetSide.Left);
        var to = Address.On(2, new Tiles(20), StreetSide.Left);
        probe.Observe(graph, TravelMode.Foot, from, to); probe.Finish(7);
        probe.Observe(graph, TravelMode.Foot, from, to); probe.Finish(7);
        probe.Observe(graph, TravelMode.Car, from, to); probe.Finish(5);
        probe.Observe(other, TravelMode.Foot, from, to); probe.Finish(9);
        probe.Observe(graph, TravelMode.Foot, from, to); probe.Finish(7);
        Assert.Equal(1, probe.Hits);
        Assert.Equal(7, probe.HitSettled);
        Assert.Equal(35, probe.Settled);
        Assert.Equal(2, probe.Evictions);
        graph.RebuildDerived();
        probe.Observe(graph, TravelMode.Foot, from, to); probe.Finish(7);
        Assert.Equal(1, probe.Hits);
        Assert.False(Options.TryParse(["--profile", "--ruleset", "x", "--profile-reuse", "--route-workers", "8"], out _, out _));
        Assert.False(Options.TryParse(["--profile-services"], out _, out _));
    }

    [Fact]
    public void Combined_fixture_has_actual_school_care_and_shopping_activity()
    {
        string rules = Path.Combine(AppContext.BaseDirectory, "Rulesets", "profile-services.toml");
        Assert.True(Options.TryParse(["--profile", "--ruleset", rules, "--profile-services",
            "--citizens", "1000", "--warmup-ticks", "65536", "--ticks", "14336", "--no-decide-guard"], out var options, out _));
        using var output = new StringWriter();
        Assert.Equal(0, ProfileDump.Run(options, output));
        Assert.StartsWith("{\"type\":\"conditions\"", output.ToString(), StringComparison.Ordinal);
        var days = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => System.Text.Json.JsonDocument.Parse(line))
            .Where(row => row.RootElement.TryGetProperty("CivicEvents", out _)).ToArray();
        long Sum(string name) => days.Sum(row => row.RootElement.GetProperty("CivicEvents").GetProperty(name).GetInt64());
        Assert.True(Sum("SchoolAttended") > 0);
        Assert.True(Sum("Treated") > 0);
        Assert.True(days.Sum(row => row.RootElement.GetProperty("Purchases").GetInt64()) > 0);
        Assert.True(days.Sum(row => row.RootElement.GetProperty("Wages").GetInt64()) > 0);
        foreach (var day in days) { day.Dispose(); }
    }

    [Fact]
    public void Route_worker_limit_is_explicit_and_only_available_to_profiling()
    {
        Assert.True(Options.TryParse(["--profile", "--ruleset", "x", "--route-workers", "8"], out var options, out _));
        Assert.Equal(8, options.RouteWorkers);
        Assert.False(Options.TryParse(["--route-workers", "8"], out _, out _));
        Assert.False(Options.TryParse(["--profile", "--ruleset", "x", "--route-workers", "0"], out _, out _));
        Assert.False(Options.TryParse(["--profile", "--ruleset", "x", "--route-workers", "9"], out _, out _));
    }

    [Fact]
    public void Reuse_probe_counts_only_matching_searches_in_the_current_tick_and_graph_version()
    {
        var graph = RoadFixtures.Chain(4);
        var probe = new RouteReuseProbe();
        var work = new RouteWork();
        work.ObserveSearchWith(probe.Observe);
        var scratch = new WalkScratch { Work = work };
        var from = Address.On(0, new Tiles(4), StreetSide.Left);
        var to = Address.On(2, new Tiles(20), StreetSide.Left);
        void Query(Address a, Address b) =>
            WalkRouting.Cost(graph, TravelMode.Foot,
                a, b, TravelTime.Zero, scratch, recordPath: true);
        Query(from, to); Query(from, to);
        Assert.Equal(2, probe.Searches);
        Assert.Equal(1, probe.Repeats);
        Query(from, from);
        Assert.Equal(2, probe.Searches);
        Query(to, from);
        Assert.Equal(1, probe.Repeats);
        graph.RebuildDerived();
        Query(from, to);
        Assert.Equal(1, probe.Repeats);
        probe.Reset();
        Query(from, to);
        Assert.Equal(1, probe.Searches);
        Assert.Equal(0, probe.Repeats);
        Assert.Equal(0, probe.Untracked);
    }

    [Fact]
    public void Capacity_reporting_counts_replacement_payload_and_both_buffers()
    {
        var cells = new LayerCellTable(8);
        var probe = new TableGrowthProbe([cells.Rows]);
        Assert.Empty(probe.Read());
        for (int i = 0; i < 33; i++)
        { cells.Create(new Cells(i), new Cells(0)); }
        var growth = Assert.Single(probe.Read());
        Assert.Equal(8, growth.Before);
        Assert.Equal(64, growth.After);
        Assert.Equal(3, growth.Growths);
        Assert.Equal(33, growth.Live);
        long width = 0;
        foreach (var column in cells.Rows.Columns) { width += column.BytesPerRow; }
        Assert.Equal((16 + 32 + 64) * width * 2, growth.PayloadBytes);
        Assert.All(growth.Columns, column => Assert.Equal(2, column.Copies));
        Assert.Equal(growth.PayloadBytes, growth.Columns.Sum(column => column.PayloadBytes));
        Assert.Empty(probe.Read());
    }

    [Fact]
    public void Profiling_can_resume_a_checkpoint_at_an_absolute_capture_tick()
    {
        string rules = Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml");
        string save = Path.Combine(Path.GetTempPath(), $"borough-profile-{Guid.NewGuid():N}.save");
        try
        {
            var common = new[] { "--profile", "--ruleset", rules, "--citizens", "100", "--no-decide-guard" };
            Assert.True(Options.TryParse([.. common, "--warmup-ticks", "0", "--ticks", "5", "--profile-save", save], out var first, out _));
            using var firstLog = new StringWriter();
            Assert.Equal(0, ProfileDump.Run(first, firstLog));
            Assert.True(File.Exists(save));
            Assert.True(Options.TryParse([.. common, "--warmup-ticks", "7", "--ticks", "3", "--profile-load", save], out var resumed, out _));
            using var resumedLog = new StringWriter();
            Assert.Equal(0, ProfileDump.Run(resumed, resumedLog));
            Assert.Contains("\"loadedTick\":5", resumedLog.ToString(), StringComparison.Ordinal);
            Assert.Contains("\"StartTick\":7", resumedLog.ToString(), StringComparison.Ordinal);
            Assert.True(Options.TryParse([.. common, "--warmup-ticks", "7", "--ticks", "3"], out var control, out _));
            using var controlLog = new StringWriter();
            Assert.Equal(0, ProfileDump.Run(control, controlLog));
            Assert.Equal(controlLog.ToString().Trim().Split('\n')[^1], resumedLog.ToString().Trim().Split('\n')[^1]);
            Assert.True(Options.TryParse([.. common, "--warmup-ticks", "4", "--ticks", "1", "--profile-load", save], out var past, out _));
            Assert.Equal(2, ProfileDump.Run(past, new StringWriter()));
            Assert.True(Options.TryParse([.. common, "--seed", "1", "--profile-load", save], out var wrongSeed, out _));
            Assert.Equal(2, ProfileDump.Run(wrongSeed, new StringWriter()));
            Assert.False(Options.TryParse(["--profile-load", save], out _, out _));
            Assert.False(Options.TryParse([.. common, "--profile-load", save, "--profile-population", "50"], out _, out _));
        }
        finally { File.Delete(save); }
    }

    [Fact]
    public void Shopping_discovery_does_not_rescan_the_candidate_box_for_each_draw()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(1000, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        for (int t = 0; t < 7 * Ticks.PerDay; t++) { sim.Step(default); }
        var work = new Borough.Core.Rules.ShoppingWork();
        sim.Shopping.Work = work;
        for (int t = 0; t < Ticks.PerDay; t++) { sim.Step(default); }
        var selection = work.Selection;
        Assert.True(selection.CandidateCalls > 1000);
        long reads = selection.CountCells + selection.CandidateCells + selection.PrefixReads + selection.RebuiltCells;
        Assert.True(reads < selection.CandidateCalls * 32,
            $"{reads} index reads for {selection.CandidateCalls} candidate draws");
    }

    [Fact]
    public void Population_control_preserves_the_sized_network()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var control = new World(1000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(control, key, Ticks.Zero);
        var sparse = new World(1000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(sparse, key, Ticks.Zero, 100);
        Assert.Equal(100, sparse.Citizens.Rows.LiveCount);
        Assert.Equal(1000, control.Citizens.Rows.LiveCount);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(ProfileDump.Network(control)),
            System.Text.Json.JsonSerializer.Serialize(ProfileDump.Network(sparse)));
        Assert.False(Options.TryParse(["--profile-population", "100"], out _, out _));
        Assert.False(Options.TryParse(["--profile", "--ruleset", "x", "--citizens", "100", "--profile-population", "101"], out _, out _));
        Assert.True(Options.TryParse(["--profile", "--ruleset", "x", "--citizens", "1000", "--profile-population", "100"], out var options, out _));
        Assert.Equal(100, options.ProfilePopulation);
    }

    [Fact]
    public void Capture_waits_at_the_aged_boundary_and_requires_an_explicit_resume()
    {
        string rules = Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml");
        Assert.True(Options.TryParse(["--profile", "--ruleset", rules, "--citizens", "100",
            "--warmup-ticks", "2", "--ticks", "2", "--profile-wait", "--no-decide-guard"], out var options, out _));
        using var stopped = new StringWriter();
        using var eof = new StringReader("");
        Assert.Equal(2, ProfileDump.Run(options, stopped, eof));
        Assert.Contains("\"type\":\"ready\"", stopped.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"tick\":2", stopped.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\"MeanMs\"", stopped.ToString(), StringComparison.Ordinal);
        using var resumed = new StringWriter();
        using var go = new StringReader("go\n");
        Assert.Equal(0, ProfileDump.Run(options, resumed, go));
        Assert.Contains("\"StartTick\":2", resumed.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"tick\":4", resumed.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"invariants\":\"passed\"", resumed.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Profiling_does_not_change_the_city_and_the_aged_fixture_still_does_work()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        for (int t = 0; t < 7 * Ticks.PerDay; t++) { sim.Step(default); }
        sim.Trips.Drain(); sim.Rules.Drain();
        using var work = new StringWriter();
        var first = ProfileDump.Measure(sim, Ticks.PerDay, work, new RouteReuseWindows());
        var rows = work.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => System.Text.Json.JsonDocument.Parse(line)).ToArray();
        Assert.Equal(Ticks.PerDay, rows.Length);
        Assert.All(rows, row => Assert.Equal(8, row.RootElement.GetProperty("phaseMs").EnumerateObject().Count()));
        Assert.Equal(first.StartTick, rows[0].RootElement.GetProperty("tick").GetUInt64());
        Assert.True(rows.Sum(r => r.RootElement.GetProperty("shoppingWork").GetProperty("Selection").GetProperty("PrefixReads").GetInt64()) > 0);
        Assert.True(rows.Sum(r => r.RootElement.GetProperty("shoppingWork").GetProperty("Estimates").GetProperty("Searches").GetInt64()) > 0);
        Assert.True(rows.Sum(r => r.RootElement.GetProperty("shoppingRoutes").GetProperty("Requests").GetInt64()) > 0);
        foreach (var row in rows)
        {
            var root = row.RootElement;
            var discovery = root.GetProperty("shoppingWork");
            long Read(string name) => discovery.GetProperty(name).GetInt64();
            Assert.Equal(Read("BusinessesChecked"), Read("NoSaleBin") + Read("AlreadyKnown")
                + Read("DiscoveryRouteRejected") + Read("ProvidersAdded"));
            Assert.Equal(discovery.GetProperty("Selection").GetProperty("CandidateCalls").GetInt64(), Read("Draws"));
            Assert.InRange(Read("DiscoveryWithoutAddition"), 0, Read("DiscoveryCalls"));
            Assert.Equal(root.GetProperty("otherRoutes").GetProperty("Searches").GetInt64(),
                root.GetProperty("routeReuse").GetProperty("other").GetProperty("Searches").GetInt64());
            Assert.Equal(root.GetProperty("shoppingRoutes").GetProperty("Searches").GetInt64(),
                root.GetProperty("routeReuse").GetProperty("shopping").GetProperty("Searches").GetInt64());
            Assert.Equal(root.GetProperty("shoppingWork").GetProperty("Estimates").GetProperty("Searches").GetInt64(),
                root.GetProperty("routeReuse").GetProperty("estimates").GetProperty("Searches").GetInt64());
        }
        Assert.True(rows.Sum(r => r.RootElement.GetProperty("shoppingWork").GetProperty("NoSaleBin").GetInt64()) > 0);
        Assert.True(rows.Sum(r => r.RootElement.GetProperty("shoppingWork").GetProperty("ProvidersAdded").GetInt64()) > 0);
        foreach (var row in rows) { row.Dispose(); }
        Assert.Null(sim.PhaseCompleted);
        Assert.Null(sim.Shopping.Work);
        Assert.Null(sim.Trips.ShoppingRouteWork);
        var reading = ProfileDump.Measure(sim, 6 * Ticks.PerDay);
        Assert.True(reading.Employed > 0);
        Assert.True(reading.Purchases > 0);
        Assert.True(reading.DeliveredGoods > 0);
        Assert.True(reading.DriveLegs > 0);
        Assert.True(reading.WalkLegs > 0);
        Assert.True(reading.CongestedSegmentTicks > 0);
        Assert.True(reading.P99Ms <= reading.MaxMs);
        Assert.InRange(reading.MaxTick, reading.StartTick, reading.StartTick + (ulong)reading.Ticks - 1);
        sim.CheckEndOfRun();
        var control = new World(400, loaded.Ruleset!, key);
        var plain = new Simulation(control, key);
        SyntheticCity.PopulateInto(control, key, Ticks.Zero);
        for (int t = 0; t < 14 * Ticks.PerDay; t++) { plain.Step(default); }
        Assert.Equal(control.HashState(), world.HashState());
    }

    [Theory]
    [InlineData("--care")]
    [InlineData("--shopping")]
    [InlineData("--census")]
    [InlineData("--series")]
    public void Profiling_refuses_other_modes(string other)
    {
        Assert.False(Options.TryParse(["--profile", "--ruleset", "x.toml", other], out _, out _));
    }

    [Fact]
    public void Ageing_is_explicit_and_exclusive_to_profiling()
    {
        Assert.True(Options.TryParse(["--profile", "--ruleset", "x.toml", "--warmup-ticks", "42", "--ticks", "64"], out var options, out _));
        Assert.Equal(Mode.Profile, options.Mode);
        Assert.Equal(42UL, options.WarmupTicks);
        Assert.Equal(64UL, options.Ticks);
        Assert.True(options.DecideGuard);
        Assert.False(Options.TryParse(["--warmup-ticks", "42"], out _, out _));
        Assert.False(Options.TryParse(["--profile", "--ruleset", "x.toml", "--ticks", "0"], out _, out _));
        Assert.False(Options.TryParse(["--profile-wait"], out _, out _));
        Assert.False(Options.TryParse(["--profile-work"], out _, out _));
        Assert.True(Options.TryParse(["--profile", "--ruleset", "x.toml", "--profile-work"], out var counted, out _));
        Assert.True(counted.ProfileWork);
        Assert.True(Options.TryParse(["--profile", "--ruleset", "x.toml", "--profile-wait"], out var waiting, out _));
        Assert.True(waiting.ProfileWait);
    }
}
