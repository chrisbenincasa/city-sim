using System.Text;
using System.Text.Json;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Entities;
using Borough.Formats;
using Borough.Shell;

internal static class ContentProbe
{
    static ulong Hash(string text) => RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(text));
    public static void Run(string root)
    {
        string generated = Path.Combine(root, "study-generated");
        using var report = File.CreateText(Path.Combine(root, "results", "content-probe.txt"));
        void Log(string message) { Console.WriteLine(message); report.WriteLine(message); report.Flush(); }
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(generated, "manifest.json")));
        foreach (var item in manifest.RootElement.EnumerateArray())
        {
            string name = item.GetProperty("name").GetString()!;
            var loaded = RulesetLoader.Load(Path.Combine(generated, name + ".toml"));
            Log($"{name}: accepted={loaded.Ok}" + (loaded.Ok ? $", kinds={loaded.Ruleset!.KindCount}" : "\n" + loaded.Describe()));
            if (!loaded.Ok && item.GetProperty("group").GetString() != "omission") throw new InvalidOperationException(loaded.Describe());
        }
        string original = File.ReadAllText(Path.Combine(root, "connected.toml"));
        var rules = RulesetLoader.Parse(original, "original").Ruleset!;
        var key = WorldKey.FromSeed(0);
        var world = new World(1000, rules, key);
        var baseline = new Simulation(world, key, RulesetCatalogue.Fixed(Hash(original), rules)) { VerifyDecideWritesNothing = false };
        baseline.Step(new TickInput([new Command(CommandKind.Populate, default, default)], Hash(original)));
        baseline.Step(new TickInput([Command.Service(new Tiles(67), new Tiles(64), 3)], Hash(original)));
        while (baseline.Tick.Raw < 2304) baseline.Step(new TickInput([], baseline.RulesetInForce));
        baseline.CheckEndOfRun();
        string scratch = Path.Combine(Path.GetTempPath(), "borough-content-viability");
        Directory.CreateDirectory(scratch);
        string save = Path.Combine(scratch, "original.city");
        CitySave.Write(save, world, original, 0);
        foreach (string path in Directory.GetFiles(generated, "evolve-*.toml").Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string changed = File.ReadAllText(path);
            var loaded = RulesetLoader.Parse(changed, name);
            if (!loaded.Ok) { Log($"{name}: load rejected: {loaded.Describe()}"); continue; }
            var pinned = CitySave.Read(save);
            if (pinned.Toml != original || pinned.World.HashState() != world.HashState()) throw new InvalidOperationException("Pinning failed");
            var catalogue = RulesetCatalogue.Of([Hash(original), Hash(changed)], [pinned.World.Rules, loaded.Ruleset!]);
            var simulation = new Simulation(pinned.World, key, catalogue) { VerifyDecideWritesNothing = false };
            // A resumed Simulation establishes its opening identity on its first Step.
            simulation.Step(new TickInput([], Hash(original)));
            ulong before = pinned.World.HashState();
            try
            {
                simulation.Step(new TickInput([], Hash(changed)));
                if (!ReferenceEquals(pinned.World.Rules, loaded.Ruleset)) throw new InvalidOperationException("New Rules were not adopted");
                if (name == "evolve-opening-balance") throw new InvalidOperationException("Expected founding-balance refusal");
                Log($"{name}: pinned original preserved; adopted={simulation.RulesetInForce == Hash(changed)}, reloads={simulation.Reloads}, degradation={simulation.LastReload}");
                simulation.CheckEndOfRun();
                string effective = simulation.RulesetInForce == Hash(changed) ? changed : original;
                string upgradedPath = Path.Combine(scratch, "upgraded.city");
                CitySave.Write(upgradedPath, pinned.World, effective, 0);
                var restored = CitySave.Read(upgradedPath);
                if (restored.World.HashState() != pinned.World.HashState()) throw new InvalidOperationException("Upgrade save hash failed");
                var continuation = new Simulation(restored.World, key, RulesetCatalogue.Fixed(Hash(effective), restored.World.Rules)) { VerifyDecideWritesNothing = false };
                for (int tick = 0; tick < 256; tick++) { simulation.Step(new TickInput([], simulation.RulesetInForce)); continuation.Step(new TickInput([], Hash(effective))); }
                if (restored.World.HashState() != pinned.World.HashState()) throw new InvalidOperationException("Upgrade continuation hash failed");
                simulation.CheckEndOfRun(); continuation.CheckEndOfRun();
                Log($"{name}: save and 256-Tick continuation passed");
            }
            catch (InvalidOperationException error) when (name == "evolve-opening-balance" && error.Message.StartsWith("this world was founded", StringComparison.Ordinal))
            {
                if (pinned.World.HashState() != before || simulation.RulesetInForce != Hash(original)) throw new InvalidOperationException("Refusal changed the city");
                Log($"{name}: refused without changing State Hash or content: {error.Message}");
            }
        }
        var old = CitySave.Read(save);
        var oldRun = new Simulation(old.World, key, RulesetCatalogue.Fixed(Hash(original), old.World.Rules)) { VerifyDecideWritesNothing = false };
        for (int tick = 0; tick < 256; tick++) { baseline.Step(new TickInput([], baseline.RulesetInForce)); oldRun.Step(new TickInput([], Hash(original))); }
        if (world.HashState() != old.World.HashState()) throw new InvalidOperationException("Original continuation failed");
        Log("Original packaged save: independent of all candidate files; 256-Tick old-content continuation passed.");
    }
}
