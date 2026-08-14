using S5.Lanes.Harness;

// S5, the Lane kernel. plans/0019-s5-lane-kernel.md.
//
//   dotnet run -c Release --project spikes/S5.Lanes
//   dotnet run -c Release --project spikes/S5.Lanes -- --out results/s5.md
//   spikes/S5.Lanes/tools/lane-run.sh
//
// Release matters. Capture.Stamp() prints the configuration for exactly that reason.
//
// `bench` hands over to BenchmarkDotNet, which is the cross-check on the self-timed loops rather
// than the primary instrument — see S5.Lanes.csproj for why that way round.

if (args.Length > 0 && args[0] == "bench")
{
    BenchmarkDotNet.Running.BenchmarkSwitcher
        .FromAssembly(System.Reflection.Assembly.GetExecutingAssembly())
        .Run(args[1..]);
    return 0;
}

string? output = null;
bool denominator = false;
bool queue = false;
bool network = false;
bool promotion = false;
bool division = false;
bool threads = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--out" when i + 1 < args.Length:
            output = args[++i];
            break;
        case "--denominator":
            denominator = true;
            break;
        case "--queue":
            queue = true;
            break;
        case "--network":
            network = true;
            break;
        case "--promotion":
            promotion = true;
            break;
        case "--division":
            division = true;
            break;
        case "--threads":
            threads = true;
            break;
        default:
            Console.Error.WriteLine($"Unrecognised argument: {args[i]}");
            Console.Error.WriteLine(
                "Usage: S5.Lanes [--denominator] [--queue] [--network] [--promotion] [--division] [--threads] [--out PATH]");
            Console.Error.WriteLine("       S5.Lanes bench [BenchmarkDotNet arguments]");
            return 2;
    }
}

// L4 is a view over the other four and is never selected: a capture that ran one section and
// printed a product would be printing the product of one measurement and three zeros. It is
// emitted only when every section that feeds it ran, and it says so itself when they did not.
bool all = !denominator && !queue && !network && !promotion && !division && !threads;
if (all)
{
    denominator = true;
    queue = true;
    network = true;
    promotion = true;
    division = true;
    threads = true;
}

long stallBefore = Capture.CpuStallMicroseconds();

var report = new System.Text.StringBuilder();
report.AppendLine("# S5 — the Lane kernel");
report.AppendLine();
report.AppendLine(Capture.Stamp());
report.AppendLine();
report.AppendLine(
    "> `plans/0019-s5-lane-kernel.md`. **S5 does not set the Microscopic Cap.** It measures one "
    + "side of a ratio — Vehicles affordable in 15.6 ms on one core — whose other side is how many "
    + "Vehicles a real city stresses at once, and that is milestone 5b's.");
report.AppendLine();

if (denominator)
{
    report.Append(DenominatorReport.Run());
}

if (queue)
{
    report.Append(QueueReport.Run());
}

if (network)
{
    report.Append(NetworkReport.Run());
}

if (promotion)
{
    report.Append(PromotionReport.Run());
}

if (division)
{
    report.Append(DivisionReport.Run());
}

if (all)
{
    report.Append(ProductReport.Run());
}
else
{
    report.AppendLine("## L4 — the derived product");
    report.AppendLine();
    report.AppendLine(
        "**Not emitted.** L4 is a view over L0–L3 and this was a partial run. A product of one "
        + "measurement and three zeros is worse than no product.");
    report.AppendLine();
}

// L6 sits after L4 rather than inside it. The product is a one-core quantity by construction —
// every figure feeding it was taken on one core — and threading multiplies the result rather than
// contributing a term to it. Folding a speedup into L4 would make a supply-side multiple look like
// a property of the kernel's cost, which is the shape plans/0012 Cause 5 warns about.
if (threads)
{
    report.Append(ThreadReport.Run());
}

report.AppendLine("---");
report.AppendLine();
report.AppendLine(Capture.Contention(stallBefore, Capture.CpuStallMicroseconds()));
report.AppendLine();

if (output is null)
{
    Console.Write(report.ToString());
}
else
{
    File.WriteAllText(output, report.ToString());
    Console.Error.WriteLine($"Wrote {output}");
}

return 0;
