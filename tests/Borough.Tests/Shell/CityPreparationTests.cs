using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Tests.Shell;

public sealed class CityPreparationTests
{
    private const ulong Hash = 0x5EED_0000_0000_0001;

    [Theory]
    [InlineData(false, 0UL)]
    [InlineData(false, 128UL)]
    [InlineData(true, 128UL)]
    public void Preparation_matches_serial_boot_and_ageing(bool empty, ulong until)
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        var key = WorldKey.FromSeed(7);
        using var job = new CityPreparation(256, rules, Hash, key, empty, until, 8);
        var serial = new Simulation(new World(256, rules, key), key);
        serial.Step(new TickInput([new Command(empty ? CommandKind.Ground : CommandKind.Populate, default, default)], Hash));
        while (serial.World.Tick.Raw < until) serial.Step(new TickInput([], Hash));
        Assert.True(SpinWait.SpinUntil(() => job.IsCompleted, TimeSpan.FromSeconds(15)));
        var prepared = job.Complete();
        Assert.Equal(serial.World.Tick.Raw, job.Tick);
        Assert.Equal(serial.World.HashState(), prepared.World.HashState());
        Assert.Equal(8, prepared.RouteWorkerCount);
        Assert.Equal(Hash, prepared.RulesetInForce);
        for (int i = 0; i < 64; i++) { serial.Step(new TickInput([], Hash)); prepared.Step(new TickInput([], Hash)); }
        Assert.Equal(serial.World.HashState(), prepared.World.HashState());
        Assert.Equal(Hash, prepared.RulesetInForce);
        Assert.Equal(0, prepared.Reloads);
    }

    [Fact]
    public void A_saved_city_is_prepared_and_stepped_under_its_saved_ruleset_hash()
    {
        RulesetCapture capture = RulesetCapture.Read(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Capture!;
        var key = WorldKey.FromSeed(7);
        var original = new Simulation(new World(64, RulesetSource.Resolve(capture).Ruleset!, key), key);
        original.Step(new TickInput([new Command(CommandKind.Populate, default, default)], capture.ContentHash));
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "saved.borough-city");
        try
        {
            CitySave.Write(path, original.World, capture, 7);
            using var job = new CityPreparation(path, 32, 1);
            Assert.True(SpinWait.SpinUntil(() => job.IsCompleted, TimeSpan.FromSeconds(15)));
            var prepared = job.Complete();
            Assert.Equal(capture.ContentHash, prepared.RulesetInForce);
            for (int i = 0; i < 4; i++) prepared.Step(new TickInput([], capture.ContentHash));
            Assert.Equal(capture.ContentHash, prepared.RulesetInForce);
            Assert.Equal(0, prepared.Reloads);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void Cancellation_stops_ageing_at_a_tick_boundary_without_publishing_a_partial_city()
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        using var job = new CityPreparation(256, rules, Hash, WorldKey.FromSeed(1), false, ulong.MaxValue, 1);
        Assert.True(SpinWait.SpinUntil(() => job.Tick >= 2, TimeSpan.FromSeconds(15)));
        Assert.Throws<InvalidOperationException>(() => job.Complete());
        job.Cancel();
        Assert.True(SpinWait.SpinUntil(() => job.IsCompleted, TimeSpan.FromSeconds(15)));
        Assert.ThrowsAny<OperationCanceledException>(() => job.Complete());
        ulong stopped = job.Tick;
        job.Cancel();
        Assert.Equal(stopped, job.Tick);
    }

    [Fact]
    public void Preparation_failures_are_reported_to_the_shell()
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        using var job = new CityPreparation(256, rules, Hash, WorldKey.FromSeed(1), false, 1, 0);
        Assert.True(SpinWait.SpinUntil(() => job.IsCompleted, TimeSpan.FromSeconds(15)));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.Complete());
    }
}
