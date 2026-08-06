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
        default:
            Console.Error.WriteLine($"Unrecognised argument: {args[i]}");
            Console.Error.WriteLine(
                "Usage: S2.Routing [--graph] [--denominator] [--matrix] [--traffic] [--cluster] "
                + "[--out PATH]");
            return 2;
    }
}

if (!graph && !denominator && !matrix && !traffic && !cluster)
{
    graph = true;
    denominator = true;
    matrix = true;
    traffic = true;
    cluster = true;
}

// Read before any work and again after all of it, so the contention block at the foot of the report
// bounds the run rather than describing the minute before it. See Capture.Read.
var before = Capture.Read();

string report =
    (graph ? FootprintReport.Run() + Environment.NewLine : string.Empty)
    + (denominator ? DenominatorReport.Run() + Environment.NewLine : string.Empty)
    + (matrix ? MatrixReport.Run() + Environment.NewLine : string.Empty)
    + (traffic ? TrafficReport.Run() + Environment.NewLine : string.Empty)
    + (cluster ? ClusterReport.Run() : string.Empty);

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
