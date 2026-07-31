using System.Globalization;
using System.Text;
using BenchmarkDotNet.Running;
using S4.Kernels.Baseline;
using S4.Kernels.Harness;
using S4.Kernels.Kernels;

// S4, the kernel benchmark. plans/0004-s4-kernel-benchmark.md.
//
//   baseline [--seconds N] [--label TAG] [--out PATH]   task 1: the machine and the denominator
//   scaling  [--seconds N] [--label TAG] [--out PATH]   task 1: aggregate bandwidth vs thread count
//   k0       [--citizens N] [--label TAG] [--out PATH]  task 3: the world's actual footprint
//   bench    [BDN arguments]                            tasks 4-8: K1-K5 under BenchmarkDotNet
//
// Written so far: K1 (linear scan, checked and unchecked), K2 (random gather by generational handle),
// K5 (wheel bucket drain). K3 and K4 are owed and are the cheap ones.
//
//   dotnet run -c Release --project spikes/S4.Kernels -- bench --filter '*K1*'
//
// K6 will be none of these; it is a ten-minute sustained loop with a histogram and it gets its own
// command when task 9 arrives.

var command = args.Length > 0 ? args[0] : "help";
var rest = args.Skip(1).ToArray();

switch (command)
{
    case "baseline":
        return Baseline(rest);

    case "scaling":
        return Scaling(rest);

    case "k0":
        return K0(rest);

    case "bench":
        BenchmarkSwitcher.FromAssembly(typeof(K1LinearScan).Assembly).Run(rest, new S4Config());
        return 0;

    default:
        Console.Error.WriteLine("usage: S4.Kernels <baseline|scaling|k0|bench> [options]");
        Console.Error.WriteLine("  baseline [--seconds N] [--label TAG] [--out PATH]");
        Console.Error.WriteLine("  scaling  [--seconds N] [--label TAG] [--out PATH]   (must not run under taskset)");
        Console.Error.WriteLine("  k0       [--citizens N] [--label TAG] [--out PATH]");
        Console.Error.WriteLine("  bench    [--filter '*'] and any other BenchmarkDotNet argument");
        return command == "help" ? 0 : 2;
}

static int Baseline(string[] args)
{
    if (Options.Parse(args, 10) is not { } o)
    {
        return 2;
    }

    Console.Error.WriteLine($"S4 baseline [{o.Label}]: sustained window {o.Seconds}s. Measuring...");
    var bandwidth = MemoryBandwidth.Run(o.Seconds);

    var sb = Header("baseline", o.Label, o.Seconds);
    sb.Append(MachineDescription.ToMarkdown());
    sb.AppendLine();
    sb.Append(bandwidth.ToMarkdown());

    return Emit(sb.ToString(), o.OutPath);
}

static int Scaling(string[] args)
{
    if (Options.Parse(args, 3) is not { } o)
    {
        return 2;
    }

    Console.Error.WriteLine($"S4 scaling [{o.Label}]: {o.Seconds}s per point, copy and read. Measuring...");
    var scaling = BandwidthScaling.Run(o.Seconds);

    var sb = Header("scaling curve", o.Label, o.Seconds);
    sb.Append(scaling.ToMarkdown());

    return Emit(sb.ToString(), o.OutPath);
}

static int K0(string[] args)
{
    var citizens = 1_000_000L;
    string? label = null;
    string? outPath = null;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--citizens" when i + 1 < args.Length:
                citizens = long.Parse(args[++i], CultureInfo.InvariantCulture);
                break;
            case "--label" when i + 1 < args.Length:
                label = args[++i];
                break;
            case "--out" when i + 1 < args.Length:
                outPath = args[++i];
                break;
            default:
                Console.Error.WriteLine($"unrecognised option: {args[i]}");
                return 2;
        }
    }

    label ??= MachineDescription.ConfigurationTag();
    Console.Error.WriteLine($"K0 [{label}]: allocating the world at {citizens:N0} Citizens...");

    // The Cap is a fixed world constant with no value, so K0 reports a curve rather than a point.
    var report = K0WorldFootprint.Run(citizens, [1_000, 2_000, 5_000, 10_000, 30_000]);

    var sb = new StringBuilder();
    var recorded = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    sb.AppendLine(CultureInfo.InvariantCulture, $"## S4 K0 — the world's footprint — `{label}`");
    sb.AppendLine();
    sb.AppendLine(CultureInfo.InvariantCulture, $"Recorded {recorded} UTC. Schema and row counts from S4 task 2.");
    sb.AppendLine();
    sb.Append(report.ToMarkdown());
    sb.AppendLine();
    sb.AppendLine("### The schema this was allocated against");
    sb.AppendLine();
    sb.Append(WorldSchema.ToMarkdown());

    return Emit(sb.ToString(), outPath);
}

static StringBuilder Header(string what, string label, int seconds)
{
    var sb = new StringBuilder();
    var recorded = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    sb.AppendLine(CultureInfo.InvariantCulture, $"## S4 {what} — `{label}`");
    sb.AppendLine();
    sb.AppendLine(CultureInfo.InvariantCulture,
        $"Recorded {recorded} UTC. Window {seconds}s. GB means 1e9 bytes. Copy rate is bytes delivered; traffic counts the read as well.");
    sb.AppendLine();
    return sb;
}

static int Emit(string markdown, string? outPath)
{
    Console.Out.Write(markdown);

    if (outPath is not null)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outPath, markdown);
        Console.Error.WriteLine($"written to {outPath}");
    }

    return 0;
}

internal sealed record Options(int Seconds, string Label, string? OutPath)
{
    public static Options? Parse(string[] args, int defaultSeconds)
    {
        var seconds = defaultSeconds;
        string? label = null;
        string? outPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seconds" when i + 1 < args.Length:
                    seconds = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                case "--label" when i + 1 < args.Length:
                    label = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outPath = args[++i];
                    break;
                default:
                    Console.Error.WriteLine($"unrecognised option: {args[i]}");
                    return null;
            }
        }

        return new Options(seconds, label ?? MachineDescription.ConfigurationTag(), outPath);
    }
}
