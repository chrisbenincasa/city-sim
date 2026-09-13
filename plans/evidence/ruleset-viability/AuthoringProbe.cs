using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Shell;

internal static class AuthoringProbe
{
    static ulong Hash(string text) => RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(text));
    public static void Run(string root)
    {
        string generated = Path.Combine(root, "generated");
        using var report = File.CreateText(Path.Combine(root, "persistence.txt"));
        void Log(string line) { report.WriteLine(line); report.Flush(); Console.WriteLine(line); }
        string[] cases = ["baseline", "shared", "variant", "add-good", "remove-override", "label", "retire", "change-id"];
        foreach (string name in cases.Concat(["scale20", "scale40", "scale80"]))
        {
            var candidate = RulesetLoader.Load(Path.Combine(generated, name + ".toml"));
            if (!candidate.Ok) throw new InvalidOperationException(candidate.Describe());
            Log($"{name}: real loader accepted; {candidate.Ruleset!.KindCount} kinds");
        }
        var excessive = RulesetLoader.Load(Path.Combine(generated, "scale83.toml"));
        if (excessive.Ok || !excessive.Describe().Contains("more than 254", StringComparison.Ordinal))
            throw new InvalidOperationException("Expected explicit runtime kind limit refusal");
        Log("scale83: real loader refuses 257 expanded kinds (limit 254). Authoring reuse does not remove runtime multiplication.");
        string original = File.ReadAllText(Path.Combine(generated, "baseline.toml"));
        var parsed = RulesetLoader.Parse(original, "baseline");
        var rules = parsed.Ruleset!;
        var key = WorldKey.FromSeed(0);
        var world = new World(1000, rules, key);
        var baseline = new Simulation(world, key, RulesetCatalogue.Fixed(Hash(original), rules)) { VerifyDecideWritesNothing = false };
        baseline.Step(new TickInput([new Command(CommandKind.Populate, default, default)], Hash(original)));
        // Fixture setup through World APIs: expose each maintenance path to an inhabited kind.
        int householdSlot = 0;
        foreach (string target in new[] { "home_00.reserve", "home_01.reserve", "home_10.standard" })
        {
            byte kind = Enumerable.Range(1, rules.KindCount).Select(i => (byte)i).Single(i => parsed.Names.Kind(i) == target);
            int lot = Enumerable.Range(0, world.Lots.Rows.SlotCount).First(i => world.Lots.Rows.IsLive(i) && world.Lots.IsVacant(i));
            var building = world.CreateBuilding(world.Lots.Rows.At(lot), kind, baseline.Tick, key);
            while (!world.Households.Rows.IsLive(householdSlot)) householdSlot++;
            var household = world.Households.Rows.At(householdSlot++);
            world.Unplace(household);
            world.Place(household, building);
        }
        for (byte kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind(kind).Business == 0) continue;
            int lot = Enumerable.Range(0, world.Lots.Rows.SlotCount).First(i => world.Lots.Rows.IsLive(i) && world.Lots.IsVacant(i));
            var building = world.CreateBuilding(world.Lots.Rows.At(lot), kind, baseline.Tick, key);
            // CreateBuilding instantiates and fits its declared Business.
        }
        while (baseline.Tick.Raw < 2048) baseline.Step(new TickInput([], Hash(original)));
        baseline.CheckEndOfRun();
        if (world.Households.Rows.LiveCount == 0) throw new InvalidOperationException("Fixture is not inhabited");
        int occupiedKinds = 0;
        for (byte kind = 1; kind <= rules.KindCount; kind++)
        {
            bool exists = false;
            for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
                if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == kind) exists = true;
            if (exists) occupiedKinds++;
        }
        Log($"Baseline Tick 2048: {world.Households.Rows.LiveCount} Households; {occupiedKinds} standing kinds; {world.RuleInstances.Rows.LiveCount} Rule Instances. Synthetic supply, not a balanced city.");
        string scratch = Path.Combine(Path.GetTempPath(), "borough-authoring-experiment");
        Directory.CreateDirectory(scratch);
        string save = Path.Combine(scratch, "baseline.city");
        CitySave.Write(save, world, original, 0);
        foreach (string name in cases.Skip(1))
        {
            string changed = File.ReadAllText(Path.Combine(generated, name + ".toml"));
            var pinned = CitySave.Read(save);
            if (pinned.Toml != original || pinned.World.HashState() != world.HashState()) throw new InvalidOperationException("Pinned content/state changed");
            var replacement = RulesetLoader.Parse(changed, name).Ruleset!;
            bool same = changed == original;
            var catalogue = same ? RulesetCatalogue.Fixed(Hash(original), pinned.World.Rules)
                : RulesetCatalogue.Of([Hash(original), Hash(changed)], [pinned.World.Rules, replacement]);
            var simulation = new Simulation(pinned.World, key, catalogue) { VerifyDecideWritesNothing = false };
            simulation.Step(new TickInput([], Hash(original)));
            // Probe a disposable copy; the baseline save and author's source remain untouched.
            simulation.Step(new TickInput([], Hash(changed)));
            if (!same && !ReferenceEquals(pinned.World.Rules, replacement)) throw new InvalidOperationException("Reload not applied");
            if ((name is "retire" or "change-id") && simulation.LastReload.BuildingsDerelicted == 0)
                throw new InvalidOperationException("Retirement did not exercise a standing kind");
            simulation.CheckEndOfRun();
            string upgradedPath = Path.Combine(scratch, "candidate.city");
            CitySave.Write(upgradedPath, pinned.World, changed, 0);
            var restored = CitySave.Read(upgradedPath);
            if (restored.World.HashState() != pinned.World.HashState()) throw new InvalidOperationException("Save hash differs");
            var continuation = new Simulation(restored.World, key, RulesetCatalogue.Fixed(Hash(changed), restored.World.Rules)) { VerifyDecideWritesNothing = false };
            for (int tick = 0; tick < 256; tick++)
            {
                simulation.Step(new TickInput([], Hash(changed)));
                continuation.Step(new TickInput([], Hash(changed)));
            }
            if (restored.World.HashState() != pinned.World.HashState()) throw new InvalidOperationException("Continuation differs");
            simulation.CheckEndOfRun(); continuation.CheckEndOfRun();
            Log($"{name}: reloads={simulation.Reloads}; {simulation.LastReload}; save and 256-Tick continuation passed");
        }
        var old = CitySave.Read(save);
        var oldRun = new Simulation(old.World, key, RulesetCatalogue.Fixed(Hash(original), old.World.Rules)) { VerifyDecideWritesNothing = false };
        for (int tick = 0; tick < 256; tick++) { baseline.Step(new TickInput([], Hash(original))); oldRun.Step(new TickInput([], Hash(original))); }
        if (world.HashState() != old.World.HashState()) throw new InvalidOperationException("Old continuation differs");
        Log("Original pinned save continuation passed. Candidate probes never overwrite the baseline save.");
    }
}
