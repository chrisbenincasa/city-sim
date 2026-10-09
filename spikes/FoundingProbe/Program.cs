using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

if (args.Length < 3)
{
    Console.Error.WriteLine("usage: FoundingProbe <ruleset.toml> <log.borough> <days> [save-day]");
    return 2;
}

string package = args[0];
string logPath = args[1];
int days = int.Parse(args[2]);
int saveDay = args.Length > 3 ? int.Parse(args[3]) : 0;

RulesetSourceResult loaded = RulesetSource.Load(package);
if (!loaded.Ok)
{
    Console.Error.WriteLine(loaded.Diagnostics[0]);
    return 2;
}

Ruleset rules = loaded.Ruleset!;
InputLog log = InputLogCodec.FromText(File.ReadAllText(logPath));

string[] trades = ["mill", "grocer", "teaching"];
byte[] tradeKinds = trades.Select(t => Kind(rules, t)).ToArray();

Simulation sim = Replay.Start(log, rules);
List<ulong> hashes = [];

Console.WriteLine(
    "day\ttreasury\ttills\thouseholds$\tsupply\tcitizens\thouseholds\tunplaced\temployed\tbusinesses\tsegments\t"
    + string.Join("\t", trades.Select(t => $"{t}#\t{t}$"))
    + "\thash\tincome_tax\tbusiness_tax\tpolicy_out\trule_out\tupkeep\tgrant\tplacement\tother_in\tother_out");

for (int day = 1; day <= days; day++)
{
    Replay.Trace(sim, log, new Ticks(Ticks.PerDay), hashEvery: Ticks.PerDay, hashes);
    Print(day, sim.World, hashes[^1], sim.DrainTreasuryFlows());
}

sim.World.Invariants.RunEndOfRun(sim.World);
Console.Error.WriteLine($"end-of-run invariants held at day {days}");

if (saveDay > 0 && !SaveReloadMatches())
{
    return 1;
}

return 0;

bool SaveReloadMatches()
{
    RulesetCapture capture = RulesetCapture.Read(package).Capture!;
    Ruleset resolved = RulesetSource.Resolve(capture).Ruleset!;
    Simulation first = Replay.Start(log, resolved);
    List<ulong> resumed = [];
    Replay.Trace(first, log, new Ticks((ulong)saveDay * Ticks.PerDay), hashEvery: Ticks.PerDay, resumed);

    string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");

    try
    {
        CitySave.Write(path, first.World, capture, log.Seed);
        SavedCity saved = CitySave.Read(path);
        var second = new Simulation(saved.World, saved.Header.Key);

        if (first.World.HashState() != second.World.HashState())
        {
            Console.Error.WriteLine("reload hash mismatch");
            return false;
        }

        Replay.Trace(
            second, log, new Ticks((ulong)(days - saveDay) * Ticks.PerDay), hashEvery: Ticks.PerDay, resumed);
        second.World.Invariants.RunEndOfRun(second.World);

        int firstBad = Enumerable.Range(0, days).FirstOrDefault(i => resumed[i] != hashes[i], -1);

        Console.Error.WriteLine(firstBad < 0
            ? $"save at day {saveDay}, reload, continue to day {days}: all {days} daily hashes match"
            : $"save and reload diverge at day {firstBad + 1}");

        return firstBad < 0;
    }
    finally
    {
        File.Delete(path);
    }
}

void Print(int day, World w, ulong hash, TreasuryFlows f)
{
    MoneyLedger m = MoneyLedger.Of(w);
    int employed = 0;

    for (int s = 0; s < w.Citizens.Rows.SlotCount; s++)
    {
        if (w.Citizens.Rows.IsLive(s) && w.Businesses.Rows.TryResolve(w.Citizens.Workplace[s], out _))
        {
            employed++;
        }
    }

    var cells = new StringBuilder();

    foreach (byte k in tradeKinds)
    {
        int count = 0;
        long held = 0;

        for (int s = 0; s < w.Businesses.Rows.SlotCount; s++)
        {
            if (!w.Businesses.Rows.IsLive(s) || w.Businesses.Kind[s] != k)
            {
                continue;
            }

            count++;

            if (w.Bins.Rows.TryResolve(w.Businesses.Balance[s], out int bin))
            {
                held += w.Bins.LevelAt(bin);
            }
        }

        cells.Append($"\t{count}\t{held}");
    }

    Console.WriteLine(
        $"{day}\t{m.Treasury}\t{m.Businesses}\t{m.Households}\t{w.MoneySupply.Issued[MoneySupplyTable.Slot].Raw}\t"
        + $"{w.Citizens.Rows.LiveCount}\t{w.Households.Rows.LiveCount}\t{w.UnplacedPool.Rows.LiveCount}\t{employed}\t"
        + $"{w.Businesses.Rows.LiveCount}\t{w.Roads.Segments.Rows.LiveCount}"
        + cells
        + $"\t{hash:X16}\t{f.Withheld}\t{f.ProfitTax}\t{f.PolicyOut}\t{f.RuleOut}\t{f.Upkeep}\t{f.Grant}\t{f.Placement}"
        + $"\t{f.PolicyIn + f.RuleIn}\t{f.Subsidy + f.Compensation}");
}

static byte Kind(Ruleset rules, string id)
{
    ulong key = ContentHash.Of(Encoding.UTF8.GetBytes(id));

    for (int k = 1; k <= rules.BusinessKindCount; k++)
    {
        if (rules.BusinessKindKey((byte)k) == key)
        {
            return (byte)k;
        }
    }

    throw new InvalidOperationException($"the Ruleset declares no trade '{id}'.");
}
