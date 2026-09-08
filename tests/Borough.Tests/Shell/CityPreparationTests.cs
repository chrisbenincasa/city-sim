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
    [Theory]
    [InlineData(false, 0UL)]
    [InlineData(false, 128UL)]
    [InlineData(true, 128UL)]
    public void Preparation_matches_serial_boot_and_ageing(bool empty, ulong until)
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        var key = WorldKey.FromSeed(7);
        using var job = new CityPreparation(256, rules, key, empty, until, 8);
        var serial = new Simulation(new World(256, rules, key), key);
        serial.Step(new TickInput([new Command(empty ? CommandKind.Ground : CommandKind.Populate, default, default)], 0));
        while (serial.World.Tick.Raw < until) serial.Step(default);
        Assert.True(SpinWait.SpinUntil(() => job.IsCompleted, TimeSpan.FromSeconds(15)));
        var prepared = job.Complete();
        Assert.Equal(serial.World.Tick.Raw, job.Tick);
        Assert.Equal(serial.World.HashState(), prepared.World.HashState());
        Assert.Equal(8, prepared.RouteWorkerCount);
        for (int i = 0; i < 64; i++) { serial.Step(default); prepared.Step(default); }
        Assert.Equal(serial.World.HashState(), prepared.World.HashState());
    }

    [Fact]
    public void Cancellation_stops_ageing_at_a_tick_boundary_without_publishing_a_partial_city()
    {
        var rules = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;
        using var job = new CityPreparation(256, rules, WorldKey.FromSeed(1), false, ulong.MaxValue, 1);
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
        using var job = new CityPreparation(256, rules, WorldKey.FromSeed(1), false, 1, 0);
        Assert.True(SpinWait.SpinUntil(() => job.IsCompleted, TimeSpan.FromSeconds(15)));
        Assert.Throws<ArgumentOutOfRangeException>(() => job.Complete());
    }
}
