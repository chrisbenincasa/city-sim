using S2.Routing.Harness;

// S2, the routing ceiling. plans/0010-s2-routing.md.
//
// Task R0's first half: the synthetic Road Graph, and the road density a 268 km² city implies.
// The denominator, the heuristic ladder and the admissibility verdict are the second half.
//
//   dotnet run -c Release --project spikes/S2.Routing
//   dotnet run -c Release --project spikes/S2.Routing -- --out docs/…/r0.md
//
// Release matters. Capture.Stamp() prints the configuration for exactly that reason.

string? output = null;
bool graph = false;
bool denominator = false;
bool matrix = false;
bool traffic = false;
bool cluster = false;
bool vector = false;
bool storm = false;
bool loop = false;
bool key = false;
bool eviction = false;
bool budget = false;
bool shed = false;

// R5.5 alone. It is the longest section in the spike and the last one open, so it gets a flag of
// its own — but it is also *part of* R5, so `--storm` runs it too and passing both runs it twice.
bool pathSource = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--out" when i + 1 < args.Length:
            output = args[++i];
            break;
        case "--graph":
            graph = true;
            break;
        case "--denominator":
            denominator = true;
            break;
        case "--matrix":
            matrix = true;
            break;
        case "--traffic":
            traffic = true;
            break;
        case "--cluster":
            cluster = true;
            break;
        case "--vector":
            vector = true;
            break;
        case "--storm":
            storm = true;
            break;
        case "--path-source":
            pathSource = true;
            break;
        case "--loop":
            loop = true;
            break;
        case "--key":
            key = true;
            break;
        case "--eviction":
            eviction = true;
            break;
        case "--budget":
            budget = true;
            break;
        case "--shed":
            shed = true;
            break;
        default:
            Console.Error.WriteLine($"Unrecognised argument: {args[i]}");
            Console.Error.WriteLine(
                "Usage: S2.Routing [--graph] [--denominator] [--matrix] [--traffic] [--cluster] "
                + "[--vector] [--storm] [--path-source] [--loop] [--key] [--eviction] "
                + "[--budget] [--shed] [--out PATH]");
            return 2;
    }
}

// `pathSource` is deliberately absent from the whole-run default: `--storm` already contains R5.5,
// and adding it here would print the section twice in every capture that names no section at all.
// `loop` IS in it, and the asymmetry with `pathSource` is the point: R5.5 is contained by --storm
// and R8 is contained by nothing, so leaving it out of the default would make a whole-run capture
// silently missing a section rather than printing one twice.
if (!graph && !denominator && !matrix && !traffic && !cluster && !vector && !storm && !pathSource
    && !loop && !key && !eviction && !budget && !shed)
{
    graph = true;
    denominator = true;
    matrix = true;
    traffic = true;
    cluster = true;
    vector = true;
    storm = true;
    loop = true;
    key = true;
    eviction = true;
    budget = true;
    shed = true;
}

// Read before any work and again after all of it, so the contention block at the foot of the report
// bounds the run rather than describing the minute before it. See Capture.Read.
var before = Capture.Read();

string report =
    (graph ? FootprintReport.Run() + Environment.NewLine : string.Empty)
    + (denominator ? DenominatorReport.Run() + Environment.NewLine : string.Empty)
    + (matrix ? MatrixReport.Run() + Environment.NewLine : string.Empty)
    + (traffic ? TrafficReport.Run() + Environment.NewLine : string.Empty)
    + (cluster ? ClusterReport.Run() + Environment.NewLine : string.Empty)
    + (vector ? VectorReport.Run() + Environment.NewLine : string.Empty)
    + (storm ? StormReport.Run() + Environment.NewLine : string.Empty)
    + (loop ? LoopReport.Run() + Environment.NewLine : string.Empty)
    + (key ? KeyReport.Run() + Environment.NewLine : string.Empty)
    + (eviction ? EvictionReport.Run() + Environment.NewLine : string.Empty)
    + (budget ? BudgetReport.Run() + Environment.NewLine : string.Empty)
    + (shed ? ShedReport.Run() + Environment.NewLine : string.Empty)
    + (pathSource ? StormReport.RunPathSource() : string.Empty);

report += Environment.NewLine + "---" + Environment.NewLine + Environment.NewLine
    + Capture.Contention(before, Capture.Read()) + Environment.NewLine;

if (output is null)
{
    Console.Write(report);
}
else
{
    File.WriteAllText(output, report);
    Console.Error.WriteLine($"Wrote {output}");
}

return 0;
