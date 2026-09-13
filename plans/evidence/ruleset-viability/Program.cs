using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

// Research instrument: fixture size and experiment schedule are not Ruleset controls.
string directory = args.Length > 0 ? args[0] : "plans/evidence/ruleset-viability";
if (args.Length > 1 && args[1] == "--validate")
{
    var candidate = RulesetLoader.Load(directory);
    if (!candidate.Ok) throw new InvalidOperationException(candidate.Describe());
    Console.WriteLine($"Accepted by the real Ruleset loader: {candidate.Ruleset!.KindCount} kinds.");
    return;
}
if (args.Length > 1 && args[1] == "--authoring")
{
    AuthoringProbe.Run(directory);
    return;
}
if (args.Length > 1 && args[1] == "--content")
{
    ContentProbe.Run(directory);
    return;
}
string text = File.ReadAllText(Path.Combine(directory, "connected.toml"));
string expensive = text.Replace("name = \"teaching\"\nwage_per_day    = 4096",
    "name = \"teaching\"\nwage_per_day    = 8192", StringComparison.Ordinal);
string matched = expensive.Replace("resource = \"money\", amount = 4096",
    "resource = \"money\", amount = 8192", StringComparison.Ordinal);
if (expensive == text || matched == expensive) throw new InvalidOperationException("Experiment edit did not match.");
Directory.CreateDirectory(Path.Combine(directory, "results"));
File.WriteAllText(Path.Combine(directory, "results", "wage-only.toml"), expensive);
File.WriteAllText(Path.Combine(directory, "results", "matched-grant.toml"), matched);

using (var diagnostic = File.CreateText(Path.Combine(directory, "results", "diagnostics.txt")))
{
    Probe("missing-pool-sink", text.Replace("gives_up_after_days = 60\n", "", StringComparison.Ordinal));
    Probe("unknown-key", text + "\n[authoring]\ninclude = \"common.toml\"\n");
    Probe("unpriced-good", text + "\n[[resource]]\nname = \"food\"\nfamily = \"good\"\n");
    Probe("unknown-trade", text.Replace("business = \"grocer\"", "business = \"typo\"", StringComparison.Ordinal));
    Probe("unfunded-wage-increase", expensive);
    void Probe(string name, string content)
    {
        var result = RulesetLoader.Parse(content, name + ".toml");
        diagnostic.WriteLine(result.Ok
            ? $"{name}: accepted=True"
            : $"{name}: accepted=False\n{result.Describe()}");
    }
}

if (args.Length > 1 && args[1] == "--reload")
{
    VerifyReload();
    return;
}

Run("baseline", text);
Run("wage-only", expensive);
Run("matched-grant", matched);

void Run(string name, string content)
{
    var loaded = RulesetLoader.Parse(content, name + ".toml");
    if (!loaded.Ok) throw new InvalidOperationException(loaded.Describe());
    var rules = loaded.Ruleset!;
    var key = WorldKey.FromSeed(0);
    var catalogue = RulesetCatalogue.Fixed(RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(content)), rules);
    var world = new World(1000, rules, key);
    var simulation = new Simulation(world, key, catalogue) { VerifyDecideWritesNothing = false };
    // Populate and Service use the same commands when replayed.
    simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], simulation.RulesetInForce));
    int schoolLot = Enumerable.Range(0, world.Lots.Rows.SlotCount)
        .First(i => world.Lots.Rows.IsLive(i) && world.Lots.IsVacant(i));
    byte schoolKind = (byte)rules.KindCount;
    var command = Command.Service(world.Lots.East[schoolLot], world.Lots.North[schoolLot], schoolKind);
    simulation.Step(new TickInput([command], simulation.RulesetInForce));
    Console.WriteLine($"{name}: school Tile {world.Lots.East[schoolLot].Raw},{world.Lots.North[schoolLot].Raw}; hash {RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(content)):X16}");
    using var csv = File.CreateText(Path.Combine(directory, "results", name + ".csv"));
    csv.WriteLine("day,households,businesses,treasury,income,grants,placement,residual,paid,shortfall,bankrupted,purchases,bought,delivered,school_workers,school_jobs,school_places,hungry");
    long opening = rules.Treasury.OpeningBalance.Raw;
    TreasuryFlows flows = default;
    long paid = 0, shortfall = 0, bankrupted = 0, purchases = 0, bought = 0, delivered = 0;
    for (int tick = 2; tick < 56 * Ticks.PerDay; tick++)
    {
        simulation.Step(default);
        var payroll = simulation.LastPayroll;
        paid += payroll.Paid; shortfall += payroll.Shortfall; bankrupted += payroll.Bankrupted;
        var shopping = simulation.Shopping.Last;
        purchases += shopping.Purchases; bought += shopping.Bought; delivered += shopping.Delivered;
        if (simulation.Tick.Raw % (ulong)Ticks.PerDay != 0) continue;
        flows = flows.Add(simulation.DrainTreasuryFlows());
        long treasury = world.TreasuryBalance()!.Value.Raw;
        int workers = 0, jobs = 0, places = 0, hungry = 0;
        for (int i = 0; i < world.Businesses.Rows.SlotCount; i++)
        {
            if (!world.Businesses.Rows.IsLive(i) || world.Businesses.Kind[i] != rules.BusinessKindCount) continue;
            foreach (int worker in world.Workers.Walk(i)) workers++;
            jobs += world.DeclaredJobs(i);
        }
        for (int i = 0; i < world.Buildings.Rows.SlotCount; i++)
            if (world.Buildings.Rows.IsLive(i) && world.Buildings.Kind[i] == schoolKind)
                places += world.DeclaredPlaces(i);
        for (int i = 0; i < world.Households.Rows.SlotCount; i++)
            if (world.Households.Rows.IsLive(i) && world.Households.Sustenance[i] < 0) hungry++;
        csv.WriteLine(FormattableString.Invariant($"{simulation.Tick.Raw / (ulong)Ticks.PerDay},{world.Households.Rows.LiveCount},{world.Businesses.Rows.LiveCount},{treasury},{flows.Income},{flows.PolicyOut},{flows.Placement},{treasury - opening - flows.Income + flows.Expenditure},{paid},{shortfall},{bankrupted},{purchases},{bought},{delivered},{workers},{jobs},{places},{hungry}"));
        if (simulation.Tick.Raw == 7UL * (ulong)Ticks.PerDay) VerifyPersistence();
    }
    simulation.CheckEndOfRun();
    Console.WriteLine($"{name}: 56 Days; invariants passed; final State Hash {world.HashState():X16}");

    void VerifyPersistence()
    {
        using var memory = new MemoryStream();
        var save = new SaveStream(memory);
        SaveFile.Write(world, simulation.RulesetInForce, save);
        memory.Position = 0;
        var restored = SaveFile.Read(save, rules, out var header);
        var resumed = new Simulation(restored, header.Key, catalogue) { VerifyDecideWritesNothing = false };
        var replayWorld = new World(1000, rules, key);
        var replay = new Simulation(replayWorld, key, catalogue) { VerifyDecideWritesNothing = false };
        replay.Step(new TickInput([new Command(CommandKind.Populate, default, default)], replay.RulesetInForce));
        replay.Step(new TickInput([command], replay.RulesetInForce));
        while (replay.Tick.Raw < simulation.Tick.Raw) replay.Step(default);
        if (world.HashState() != replayWorld.HashState() || world.HashState() != restored.HashState())
            throw new InvalidOperationException("Replay/save hash differs at Day 7.");
        // Compare a separate replay and save continuation without advancing the measured run.
        for (int i = 0; i < Ticks.PerDay; i++)
        {
            replay.Step(default); resumed.Step(default);
            if (replayWorld.HashState() != restored.HashState())
                throw new InvalidOperationException($"Save continuation differs at {replay.Tick.Raw}.");
        }
        replay.CheckEndOfRun(); resumed.CheckEndOfRun();
        Console.WriteLine($"{name}: command replay at Day 7 and save continuation through Day 8 passed ({memory.Length} bytes).");
    }
}

void VerifyReload()
{
    string[] contents = [text, expensive, matched];
    ulong[] hashes = contents.Select(t => RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(t))).ToArray();
    Ruleset[] rules = contents.Select(t => RulesetLoader.Parse(t, "reload.toml").Ruleset!).ToArray();
    var catalogue = RulesetCatalogue.Of(hashes, rules);
    var builder = new InputLogBuilder(0, new WorldConfiguration(1000), hashes[0]);
    builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
    builder.Append(new Ticks(1), Command.Service(new Tiles(67), new Tiles(64), 3));
    builder.Reload(new Ticks(2 * Ticks.PerDay), hashes[1]);
    builder.Reload(new Ticks(4 * Ticks.PerDay), hashes[2]);
    string encoded = InputLogCodec.ToText(builder.Build());
    File.WriteAllText(Path.Combine(directory, "results", "reload.borough"), encoded);
    InputLog log = InputLogCodec.FromText(encoded);
    var key = WorldKey.FromSeed(log.Seed);
    var first = new World(1000, rules[0], key);
    var second = new World(1000, rules[0], key);
    var a = new Simulation(first, key, catalogue) { VerifyDecideWritesNothing = false };
    var b = new Simulation(second, key, catalogue) { VerifyDecideWritesNothing = false };
    while (a.Tick.Raw < 8UL * (ulong)Ticks.PerDay)
    {
        var input = new TickInput(log.At(a.Tick), log.RulesetHashAt(a.Tick));
        a.Step(input); b.Step(input);
        if (a.Tick.Raw % 64 == 0 && first.HashState() != second.HashState())
            throw new InvalidOperationException($"Reload replay differs at {a.Tick.Raw}.");
    }
    a.CheckEndOfRun(); b.CheckEndOfRun();
    if (a.Reloads != 2 || a.RulesetInForce != hashes[2]
        || first.Rules.BusinessKind(2).WagePerDay != 8192
        || first.Policies.AmountOf(0, first.Rules.Policies[0]) != 8192)
        throw new InvalidOperationException("Wage/grant reload did not take effect.");
    Console.WriteLine($"Two encoded Input Log reloads applied; replay equal every 64 Ticks through Day 8; invariants passed; State Hash {first.HashState():X16}.");
}

internal sealed class SaveStream(Stream stream) : ISaveSink, ISaveSource
{
    public void Write(ReadOnlySpan<byte> bytes) => stream.Write(bytes);
    public void Read(Span<byte> into) => stream.ReadExactly(into);
}
