using System.Text;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
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
string[] goods = ["flour", "sundries"];
ResourceId[] goodIds = goods.Select(g => Good(rules, g)).ToArray();

Simulation sim = Replay.Start(log, rules);
List<ulong> hashes = [];

Console.WriteLine(
    "day\ttreasury\ttills\thouseholds$\tsupply\tcitizens\thouseholds\tunplaced\temployed\tbusinesses\tsegments\t"
    + string.Join("\t", trades.Select(t => $"{t}#\t{t}$\t{t}_arrears\t{t}_staff"))
    + string.Concat(goods.Select(g => $"\t{g}_min\t{g}_max\t{g}_stock\t{g}_rate"))
    + "\tstarving\tshopping\tpurses\thash\tincome_tax\tbusiness_tax\tpolicy_out\trule_out\tupkeep\tgrant\tplacement\tother_in\tother_out");

if (Environment.GetEnvironmentVariable("PROBE_NOON") is not null)
{
    Replay.Trace(sim, log, new Ticks(Ticks.PerDay / 2), hashEvery: Ticks.PerDay, []);
}

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
    var staff = new Dictionary<byte, int>();

    for (int s = 0; s < w.Citizens.Rows.SlotCount; s++)
    {
        if (w.Citizens.Rows.IsLive(s) && w.Businesses.Rows.TryResolve(w.Citizens.Workplace[s], out int at))
        {
            employed++;
            staff[w.Businesses.Kind[at]] = staff.GetValueOrDefault(w.Businesses.Kind[at]) + 1;
        }
    }

    var cells = new StringBuilder();

    foreach (byte k in tradeKinds)
    {
        int count = 0;
        int arrears = 0;
        long held = 0;

        for (int s = 0; s < w.Businesses.Rows.SlotCount; s++)
        {
            if (!w.Businesses.Rows.IsLive(s) || w.Businesses.Kind[s] != k)
            {
                continue;
            }

            count++;

            if (w.Businesses.DaysInArrears[s] > 0)
            {
                arrears++;
            }

            if (w.Bins.Rows.TryResolve(w.Businesses.Balance[s], out int bin))
            {
                held += w.Bins.LevelAt(bin);
            }
        }

        cells.Append($"\t{count}\t{held}\t{arrears}\t{staff.GetValueOrDefault(k)}");
    }

    foreach (ResourceId good in goodIds)
    {
        long min = long.MaxValue;
        long max = 0;
        long stock = 0;
        long rate = 0;

        for (int row = 0; row < w.DistrictPools.Rows.SlotCount; row++)
        {
            if (!w.DistrictPools.Rows.IsLive(row)
                || !w.Bins.Rows.TryResolve(w.DistrictPools.Bin[row], out int bin)
                || w.Bins.Resource[bin] != good)
            {
                continue;
            }

            rate += w.DistrictPools.Rate[row];
            long price = w.DistrictPools.Price[row].Raw;
            min = Math.Min(min, price);
            max = Math.Max(max, price);

            int sellers = w.Markets.SellerCount(w, row);

            for (int ordinal = 0; ordinal < sellers; ordinal++)
            {
                stock += w.Bins.LevelAt(w.Markets.Seller(w, row, ordinal).Bin);
            }
        }

        cells.Append($"\t{(min == long.MaxValue ? 0 : min)}\t{max}\t{stock}\t{rate}");
    }

    var starving = new SortedDictionary<int, int>();

    for (int r = 0; r < w.RuleInstances.Rows.SlotCount; r++)
    {
        if (w.RuleInstances.Rows.IsLive(r) && w.RuleInstances.IsStarving(r))
        {
            int id = w.RuleInstances.Rule[r].Raw;
            starving[id] = starving.GetValueOrDefault(id) + 1;
        }
    }

    cells.Append('\t').Append(string.Join(",", starving.Select(kv => $"{kv.Key}:{kv.Value}")));

    int unserved = 0;
    long failed = 0;
    var reasons = new SortedDictionary<string, int>();
    var stages = new SortedDictionary<int, int>();

    for (int r = 0; r < w.Shopping.Rows.SlotCount; r++)
    {
        if (!w.Shopping.Rows.IsLive(r))
        {
            continue;
        }

        stages[w.Shopping.Stage[r]] = stages.GetValueOrDefault(w.Shopping.Stage[r]) + 1;
        failed += w.Shopping.Failed[r];

        if (w.Shopping.UnservedSince[r].Raw != 0)
        {
            unserved++;
            string why = ((ShoppingFailure)w.Shopping.Reason[r]).ToString();
            reasons[why] = reasons.GetValueOrDefault(why) + 1;
        }
    }

    var balances = new List<long>();
    int nopurse = 0;

    for (int h = 0; h < w.Households.Rows.SlotCount; h++)
    {
        if (!w.Households.Rows.IsLive(h))
        {
            continue;
        }

        if (w.Bins.Rows.TryResolve(w.Households.Balance[h], out int purse))
        {
            balances.Add(w.Bins.LevelAt(purse));
        }
        else
        {
            nopurse++;
        }
    }

    int working = 0;

    for (int h = 0; h < w.Households.Rows.SlotCount; h++)
    {
        if (!w.Households.Rows.IsLive(h))
        {
            continue;
        }

        foreach (int member in w.Members.Walk(h))
        {
            if (w.Businesses.Rows.TryResolve(w.Citizens.Workplace[member], out _))
            {
                working++;
                break;
            }
        }
    }

    balances.Sort();
    string q(double f) => balances.Count == 0 ? "-" : balances[(int)(f * (balances.Count - 1))].ToString();
    string purses = $"working={working} nopurse={nopurse} zero={balances.Count(b => b == 0)} under100={balances.Count(b => b < 100)} p25={q(0.25)} p50={q(0.5)} p75={q(0.75)} p95={q(0.95)} max={q(1)}";

    cells.Append($"\tunserved={unserved} failed={failed} why=")
        .Append(string.Join(",", reasons.Select(kv => $"{kv.Key}:{kv.Value}")))
        .Append(" stage=")
        .Append(string.Join(",", stages.Select(kv => $"{kv.Key}:{kv.Value}")))
        .Append('\t').Append(purses);

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

static ResourceId Good(Ruleset rules, string id)
{
    ulong key = ContentHash.Of(Encoding.UTF8.GetBytes(id));

    for (int r = 1; r <= rules.ResourceCount; r++)
    {
        var resource = new ResourceId((byte)r);

        if (rules.ResourceKey(resource) == key)
        {
            return resource;
        }
    }

    throw new InvalidOperationException($"the Ruleset declares no Resource '{id}'.");
}
