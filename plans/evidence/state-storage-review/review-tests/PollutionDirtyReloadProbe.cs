using System.Text.RegularExpressions;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Review;

// Review probe B: MapLayers._pollutionDirty is cross-Tick state that is neither saved, hashed nor
// rebuilt. With pollution_decay_ticks = 0 nothing re-marks a source, so an emission between the
// last pollution cadence and a save is never diffused in the reloaded world.
public sealed class PollutionDirtyReloadProbe(ITestOutputHelper output)
{
    [Theory]
    [InlineData("2048", 700)]
    [InlineData("0", 700)]
    public void Fouled_world_reloads_and_runs_on_identically(string decay, int n)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "fouled.toml");
        string text = Regex.Replace(
            File.ReadAllText(path), @"(?m)^pollution_decay_ticks\s*=\s*\d+", $"pollution_decay_ticks = {decay}");
        RulesetLoadResult loaded = RulesetLoader.Parse(text, path);
        Ruleset rules = loaded.Ruleset ?? throw new InvalidOperationException(loaded.Describe());
        Assert.Equal(decay == "0" ? 0 : 1, Math.Sign(rules.Layers.Rates.PollutionTau));

        (World control, Simulation controlSim) = Stepped(rules, n);
        (World subject, Simulation subjectSim) = Stepped(rules, n);

        var file = new Borough.Tests.Persistence.MemorySave();
        subjectSim.SaveAtEndOfTick(file);
        subjectSim.Step(default);
        controlSim.Step(default);

        output.WriteLine($"dirty at save: {subject.Layers.PollutionIsDirty}, tick {subject.Tick.Raw}");

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key);

        Assert.Equal(control.HashState(), reloaded.HashState());
        output.WriteLine($"dirty after load: {reloaded.Layers.PollutionIsDirty}");

        for (int tick = 0; tick < 256; tick++)
        {
            controlSim.Step(default);
            resumed.Step(default);
            Assert.True(
                control.HashState() == reloaded.HashState(),
                $"decay={decay}: diverged at tick {control.Tick.Raw} (saved at {n + 1})");
        }
    }

    private static (World, Simulation) Stepped(Ruleset rules, int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
        return (world, simulation);
    }
}
