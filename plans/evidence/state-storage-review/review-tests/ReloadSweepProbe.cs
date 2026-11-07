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

// Review probe: save at N, reload, run on M, compare with a control run, over every shipped Ruleset.
public sealed class ReloadSweepProbe(ITestOutputHelper output)
{
    public static TheoryData<string> Files()
    {
        var data = new TheoryData<string>();
        foreach (string path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml").Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileName(path);
            string? only = Environment.GetEnvironmentVariable("PROBE_FILES");
            if (only is null || only.Split(',').Contains(name)) { data.Add(name); }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void Reload_runs_on_identically(string file)
    {
        int n = int.Parse(Environment.GetEnvironmentVariable("PROBE_N") ?? "700");
        int m = int.Parse(Environment.GetEnvironmentVariable("PROBE_M") ?? "1500");
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
        RulesetLoadResult loaded = RulesetLoader.Load(path);
        if (loaded.Ruleset is null)
        {
            output.WriteLine($"{file}: refused");
            return;
        }

        Ruleset rules = loaded.Ruleset;
        (World control, Simulation controlSim) = Stepped(rules, n);
        (World subject, Simulation subjectSim) = Stepped(rules, n);

        var file2 = new Borough.Tests.Persistence.MemorySave();
        subjectSim.SaveAtEndOfTick(file2);
        subjectSim.Step(default);
        controlSim.Step(default);

        World reloaded = SaveFile.Read(file2, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key);

        Assert.Equal(control.HashState(), reloaded.HashState());

        for (int tick = 0; tick < m; tick++)
        {
            controlSim.Step(default);
            resumed.Step(default);
            if (control.HashState() != reloaded.HashState())
            {
                Assert.Fail($"{file}: diverged at tick {n + 1 + tick} (saved at {n})");
            }
        }

        output.WriteLine($"{file}: ok to {n + 1 + m}");
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
