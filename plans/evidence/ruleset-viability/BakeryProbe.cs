using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Rules;
using Borough.Formats;

internal static class BakeryProbe
{
    public static void Run(string root)
    {
        string generated = Path.Combine(root, "generated");
        using var report = File.CreateText(Path.Combine(root, "bakery-core-results.txt"));
        foreach (string name in new[] { "bakery-present-workers", "bakery-worker-slots" })
        {
            var refusal = RulesetLoader.Load(Path.Combine(generated, name + ".toml"));
            if (refusal.Ok) throw new InvalidOperationException("Capability changed; reassess the bakery adapter");
            report.WriteLine($"{name}: refused\n{refusal.Describe()}");
        }
        string text = File.ReadAllText(Path.Combine(generated, "bakery-core.toml"));
        var loaded = RulesetLoader.Parse(text, "bakery-core");
        if (!loaded.Ok) throw new InvalidOperationException(loaded.Describe());
        var rules = loaded.Ruleset!;
        ulong hash = RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(text));
        var key = WorldKey.FromSeed(0);
        var world = new World(1000, rules, key);
        var sim = new Simulation(world, key, RulesetCatalogue.Fixed(hash, rules)) { VerifyDecideWritesNothing = false };
        sim.Step(new TickInput([new Command(CommandKind.Populate, default, default)], hash));
        byte kind = Enumerable.Range(1, rules.KindCount).Select(i => (byte)i).Single(k => loaded.Names.Kind(k) == "producer.food");
        int lot = Enumerable.Range(0, world.Lots.Rows.SlotCount).First(i => world.Lots.Rows.IsLive(i) && world.Lots.IsVacant(i));
        var building = world.CreateBuilding(world.Lots.Rows.At(lot), kind, sim.Tick, key);
        int businessSlot = Enumerable.Range(0, world.Businesses.Rows.SlotCount).Single(i => world.Businesses.Rows.IsLive(i) && world.Businesses.Origin[i] == building);
        int FindBin(string name)
        {
            var bin = world.Businesses.BinHead[businessSlot];
            while (!bin.IsNone)
            {
                int slot = world.Bins.Rows.Resolve(bin);
                if (loaded.Names.Resource(world.Bins.Resource[slot]) == name) return slot;
                bin = world.Bins.OwnerNext[slot];
            }
            throw new InvalidOperationException("Missing " + name);
        }
        int input = FindBin("produce"), output = FindBin("food");
        world.Deposit(world.Bins.Rows.At(input), 32, sim.Tick);
        int maxWorkers = 0;
        for (int i = 0; i < 64; i++)
        {
            sim.Step(new TickInput([], hash));
            int workers = 0;
            foreach (int _ in world.Workers.Walk(businessSlot)) workers++;
            maxWorkers = int.Max(maxWorkers, workers);
        }
        long used = 32-world.Bins.LevelAt(input), produced = world.Bins.LevelAt(output);
        if (maxWorkers != 0 || produced <= 0 || produced != used*2 || world.DeclaredJobs(businessSlot)<=0)
            throw new InvalidOperationException("The unstaffed production hypothesis did not reproduce");
        sim.CheckEndOfRun();
        report.WriteLine($"64 Ticks: declared posts={world.DeclaredJobs(businessSlot)}, workers={maxWorkers}, Produce consumed={used}, Food produced={produced}; invariants passed.");
        report.WriteLine("Input was manually deposited; no paid-supply or balance claim. Current fixed-count production runs with no workers.");
        Console.WriteLine("Bakery capability and zero-worker production probes passed; see bakery-core-results.txt.");
    }
}
