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
        default:
            Console.Error.WriteLine($"Unrecognised argument: {args[i]}");
            Console.Error.WriteLine("Usage: S2.Routing [--graph] [--denominator] [--out PATH]");
            return 2;
    }
}

if (!graph && !denominator)
{
    graph = true;
    denominator = true;
}

string report =
    (graph ? FootprintReport.Run() + Environment.NewLine : string.Empty)
    + (denominator ? DenominatorReport.Run() : string.Empty);

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
