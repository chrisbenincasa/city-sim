using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace S4.Kernels.Baseline;

/// <summary>
/// The machine, recorded before any kernel runs. plans/0004 task 1: "Record the machine: CPU model,
/// core count, cache sizes at each level, RAM configuration and channel count, OS, kernel, SDK
/// version" and "Disable or record turbo and frequency-governor state. A benchmark whose variance
/// is the governor's is a benchmark of the governor."
///
/// Linux and sysfs only. This is a spike on one machine; portability would be cost with no buyer.
/// </summary>
internal static class MachineDescription
{
    public static string ToMarkdown()
    {
        var sb = new StringBuilder();

        sb.AppendLine("### Machine");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");

        Row(sb, "CPU", CpuModel());
        Row(sb, "Topology", Topology());
        foreach (var (label, value) in Caches())
        {
            Row(sb, label, value);
        }

        // Recorded explicitly because it is not 64 everywhere: Apple Silicon uses 128-byte lines,
        // which changes how many table rows share a line and therefore what a row schema should
        // pack to. A figure this easy to assume is a figure worth measuring.
        Row(sb, "Cache line", $"{CacheLineBytes()} B");

        Row(sb, "RAM total", RamTotal());
        var dimms = Dimms();
        Row(sb, "RAM configuration", DescribeDimms(dimms));
        Row(sb, "Theoretical DRAM peak", TheoreticalPeak(dimms));
        Row(sb, "OS", OsName());
        Row(sb, "Kernel", ReadFirstLine("/proc/sys/kernel/osrelease") ?? RuntimeInformation.OSDescription);
        Row(sb, "SDK / runtime", $"{RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture}");
        Row(sb, "GC mode", GcMode());
        sb.AppendLine();

        sb.AppendLine("### Frequency and scheduler state");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");

        if (OperatingSystem.IsMacOS())
        {
            // Nothing to disable and nothing to record: macOS exposes no governor, no turbo switch
            // and no per-core clock. plans/0004 asks for turbo and governor state to be disabled or
            // recorded; on this platform the honest recording is that neither is available.
            Row(sb, "Frequency control", "not exposed by macOS — no governor, no turbo switch, no per-core clock");
            Row(sb, "Core placement", "not controllable — threads cannot be pinned on Apple Silicon");
            Row(sb, "Process CPU affinity", "not applicable");
            return sb.ToString();
        }

        Row(sb, "Scaling driver", Sysfs("cpu0/cpufreq/scaling_driver"));
        Row(sb, "Governor", Sysfs("cpu0/cpufreq/scaling_governor"));
        Row(sb, "Turbo", TurboState());
        Row(sb, "Energy/performance preference", Sysfs("cpu0/cpufreq/energy_performance_preference"));
        Row(sb, "Frequency range", FrequencyRange());
        Row(sb, "SMT", Sysfs("smt/control"));
        Row(sb, "Transparent huge pages", ReadFirstLine("/sys/kernel/mm/transparent_hugepage/enabled") ?? "unknown");
        Row(sb, "Process CPU affinity", CpusAllowed());

        return sb.ToString();
    }

    private static void Row(StringBuilder sb, string field, string value) =>
        sb.Append("| ").Append(field).Append(" | ").Append(value).AppendLine(" |");

    private static string CpuModel()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Mac.Text("machdep.cpu.brand_string");
        }

        foreach (var line in ReadLines("/proc/cpuinfo"))
        {
            if (line.StartsWith("model name", StringComparison.Ordinal))
            {
                return line[(line.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim();
            }
        }

        return "unknown";
    }

    private static string Topology()
    {
        if (OperatingSystem.IsMacOS())
        {
            // Heterogeneous, and the split matters more here than the total: a thread on an
            // efficiency core gets a fraction of the bandwidth one on a performance core does, and
            // which it lands on is the scheduler's decision, not ours.
            var performance = Mac.Number("hw.perflevel0.physicalcpu");
            var efficiency = Mac.Number("hw.perflevel1.physicalcpu");
            return efficiency > 0
                ? $"{performance} performance cores + {efficiency} efficiency cores, {Mac.Number("hw.logicalcpu")} logical"
                : $"{Mac.Number("hw.physicalcpu")} cores, {Mac.Number("hw.logicalcpu")} logical";
        }

        // Not Environment.ProcessorCount: under taskset that reports the affinity mask, and the
        // machine description must describe the machine. The affinity mask has its own row.
        var threads = OnlineCpus().Length;
        var cores = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cpu in OnlineCpus())
        {
            var id = Sysfs($"cpu{cpu}/topology/core_id");
            var pkg = Sysfs($"cpu{cpu}/topology/physical_package_id");
            if (id != Unknown)
            {
                cores.Add($"{pkg}/{id}");
            }
        }

        var siblings = Sysfs("cpu0/topology/thread_siblings_list");
        return cores.Count > 0
            ? $"{cores.Count} physical cores, {threads} hardware threads (cpu0 siblings: {siblings})"
            : $"{threads} hardware threads";
    }

    /// <summary>Cache sizes at each level, read per index rather than assumed.</summary>
    private static List<(string Label, string Value)> Caches()
    {
        var rows = new List<(string, string)>();

        if (OperatingSystem.IsMacOS())
        {
            foreach (var (level, label) in new[] { (0, "performance"), (1, "efficiency") })
            {
                var l1 = Mac.Number($"hw.perflevel{level}.l1dcachesize");
                if (l1 == 0)
                {
                    continue;
                }

                rows.Add(($"L1d ({label})", $"{l1 / 1024} KiB per core"));
                rows.Add(($"L2 ({label})", $"{Mac.Number($"hw.perflevel{level}.l2cachesize") / 1024 / 1024} MiB, shared by the cluster"));
            }

            return rows;
        }

        for (var index = 0; index < 8; index++)
        {
            var dir = $"cpu0/cache/index{index}";
            var level = Sysfs($"{dir}/level");
            if (level == Unknown)
            {
                break;
            }

            var type = Sysfs($"{dir}/type");
            var size = Sysfs($"{dir}/size");
            var line = Sysfs($"{dir}/coherency_line_size");
            var ways = Sysfs($"{dir}/ways_of_associativity");
            var shared = Sysfs($"{dir}/shared_cpu_list");

            var name = type switch
            {
                "Data" => $"L{level}d",
                "Instruction" => $"L{level}i",
                _ => $"L{level}",
            };
            rows.Add((name, $"{size}, {ways}-way, {line} B lines, shared by cpus {shared}"));
        }

        return rows;
    }

    /// <summary>64 on x86-64; 128 on Apple Silicon, which is the point of asking.</summary>
    private static int CacheLineBytes()
    {
        if (OperatingSystem.IsMacOS())
        {
            var line = Mac.Number("hw.cachelinesize");
            return line > 0 ? (int)line : 0;
        }

        var size = Sysfs("cpu0/cache/index0/coherency_line_size");
        return int.TryParse(size, CultureInfo.InvariantCulture, out var bytes) ? bytes : 0;
    }

    private static string RamTotal()
    {
        if (OperatingSystem.IsMacOS())
        {
            return $"{Mac.Number("hw.memsize") / 1024.0 / 1024.0 / 1024.0:F1} GiB unified";
        }

        foreach (var line in ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
            {
                var kb = long.Parse(line.Split(':')[1].Replace("kB", "", StringComparison.Ordinal).Trim(), CultureInfo.InvariantCulture);
                return $"{kb / 1024.0 / 1024.0:F1} GiB";
            }
        }

        return Unknown;
    }

    private sealed record Dimm(string Locator, string Size, string RatedSpeed, string ConfiguredSpeed, string PartNumber);

    /// <summary>
    /// DIMM population and channel count, from a `sudo dmidecode -t memory` capture named by
    /// S4_DMIDECODE_FILE. dmidecode needs root; when the capture is absent the report says so
    /// rather than inventing a number, because the channel count and the *configured* transfer
    /// rate are what turn the measured copy rate into a fraction of a theoretical ceiling.
    /// </summary>
    private static List<Dimm> Dimms()
    {
        var path = Environment.GetEnvironmentVariable("S4_DMIDECODE_FILE");
        if (path is null || !File.Exists(path))
        {
            return [];
        }

        var dimms = new List<Dimm>();
        string? size = null, rated = null, locator = null, part = null;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("Size:", StringComparison.Ordinal))
            {
                size = line[5..].Trim();
            }
            else if (line.StartsWith("Locator:", StringComparison.Ordinal))
            {
                locator = line[8..].Trim();
            }
            else if (line.StartsWith("Part Number:", StringComparison.Ordinal))
            {
                part = line[12..].Trim();
            }
            else if (line.StartsWith("Speed:", StringComparison.Ordinal))
            {
                rated = line[6..].Trim();
            }
            else if (line.StartsWith("Configured Memory Speed:", StringComparison.Ordinal))
            {
                if (size is not null && !size.StartsWith("No Module", StringComparison.Ordinal))
                {
                    dimms.Add(new Dimm(locator ?? Unknown, size, rated ?? Unknown, line[24..].Trim(), part ?? Unknown));
                }

                size = rated = locator = part = null;
            }
        }

        return dimms;
    }

    private static string DescribeDimms(List<Dimm> dimms) => dimms.Count > 0
        ? string.Join("; ", dimms.Select(d => $"{d.Locator} {d.Size} {d.PartNumber} running at {d.ConfiguredSpeed}"))
        : OperatingSystem.IsMacOS()
            ? "unified memory on package — no DIMMs, no channel count to derive a ceiling from"
            : "unknown — needs `sudo dmidecode -t memory`, see tools/baseline-sweep.sh";

    /// <summary>
    /// channels x transfers/s x 8 bytes. The ceiling the sustained traffic figure is a fraction of.
    /// Channel count is taken from the distinct channels the populated DIMMs sit on, because a
    /// dual-channel board with both modules on one channel halves this and looks identical
    /// everywhere else.
    /// </summary>
    private static string TheoreticalPeak(List<Dimm> dimms)
    {
        var (channels, mts) = ChannelsAndRate(dimms);
        if (channels > 0)
        {
            return $"{Peak(channels, mts):F1} GB/s ({channels} channels x {mts} MT/s x 8 B)";
        }

        // Deliberately not filled in from the vendor's published figure. A number the machine did
        // not tell us, sitting in a column of numbers it did, is how a marketing claim becomes a
        // measurement. Where a published ceiling is relevant it belongs in the prose, named as
        // published.
        return OperatingSystem.IsMacOS() ? "not derivable — see the report" : Unknown;
    }

    /// <summary>The same ceiling as a number, for the scaling curve to report percentages against.</summary>
    public static double TheoreticalPeakGbPerSec()
    {
        var (channels, mts) = ChannelsAndRate(Dimms());
        return channels == 0 ? 0 : Peak(channels, mts);
    }

    private static double Peak(int channels, int mts) => channels * (long)mts * 1_000_000L * 8L / 1e9;

    private static (int Channels, int Mts) ChannelsAndRate(List<Dimm> dimms)
    {
        if (dimms.Count == 0)
        {
            return (0, 0);
        }

        var channels = dimms
            .Select(d => d.Locator)
            .Select(l =>
            {
                var at = l.IndexOf("Channel", StringComparison.Ordinal);
                return at >= 0 && at + 7 < l.Length ? l.Substring(at + 7, 1) : l;
            })
            .Distinct(StringComparer.Ordinal)
            .Count();

        var digits = new string(dimms[0].ConfiguredSpeed.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, CultureInfo.InvariantCulture, out var mts) ? (channels, mts) : (0, 0);
    }

    private static string OsName()
    {
        foreach (var line in ReadLines("/etc/os-release"))
        {
            if (line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
            {
                return line[12..].Trim('"');
            }
        }

        return RuntimeInformation.OSDescription;
    }

    private static string GcMode()
    {
        var concurrent = AppContext.TryGetSwitch("System.GC.Concurrent", out var c) ? c : true;
        return $"server={GCSettings.IsServerGC}, concurrent={concurrent}, latency={GCSettings.LatencyMode}";
    }

    private static string TurboState()
    {
        var noTurbo = ReadFirstLine("/sys/devices/system/cpu/intel_pstate/no_turbo");
        if (noTurbo is not null)
        {
            return noTurbo == "1" ? "disabled (intel_pstate/no_turbo=1)" : "enabled (intel_pstate/no_turbo=0)";
        }

        var boost = ReadFirstLine("/sys/devices/system/cpu/cpufreq/boost");
        return boost is null ? Unknown : boost == "0" ? "disabled (cpufreq/boost=0)" : "enabled (cpufreq/boost=1)";
    }

    private static string FrequencyRange()
    {
        var min = Sysfs("cpu0/cpufreq/scaling_min_freq");
        var max = Sysfs("cpu0/cpufreq/scaling_max_freq");
        return min == Unknown || max == Unknown
            ? Unknown
            : $"{int.Parse(min, CultureInfo.InvariantCulture) / 1000} - {int.Parse(max, CultureInfo.InvariantCulture) / 1000} MHz";
    }

    private static string CpusAllowed()
    {
        foreach (var line in ReadLines("/proc/self/status"))
        {
            if (line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal))
            {
                return line[18..].Trim();
            }
        }

        return Unknown;
    }

    /// <summary>Highest current clock across the CPUs this process may actually run on, in MHz.</summary>
    public static int CurrentMaxClockMhz()
    {
        var best = 0;
        foreach (var cpu in AllowedCpus())
        {
            var khz = Sysfs($"cpu{cpu}/cpufreq/scaling_cur_freq");
            if (khz != Unknown && int.TryParse(khz, CultureInfo.InvariantCulture, out var value))
            {
                best = Math.Max(best, value / 1000);
            }
        }

        return best;
    }

    private static int[]? _online;
    private static int[]? _allowed;

    /// <summary>Every online CPU on the machine, ignoring this process's affinity mask.</summary>
    private static int[] OnlineCpus() =>
        _online ??= ParseCpuList(ReadFirstLine("/sys/devices/system/cpu/online") ?? "0");

    /// <summary>The CPUs this process is pinned to — under taskset, usually one.</summary>
    private static int[] AllowedCpus()
    {
        if (_allowed is not null)
        {
            return _allowed;
        }

        var list = CpusAllowed();
        return _allowed = list == Unknown ? OnlineCpus() : ParseCpuList(list);
    }

    /// <summary>Parses the kernel's "0-3,8,10-11" CPU list form.</summary>
    private static int[] ParseCpuList(string list)
    {
        var cpus = new List<int>();
        foreach (var part in list.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var range = part.Split('-');
            var lo = int.Parse(range[0], CultureInfo.InvariantCulture);
            var hi = range.Length > 1 ? int.Parse(range[1], CultureInfo.InvariantCulture) : lo;
            for (var cpu = lo; cpu <= hi; cpu++)
            {
                cpus.Add(cpu);
            }
        }

        return [.. cpus];
    }

    public static string ConfigurationTag()
    {
        if (OperatingSystem.IsMacOS())
        {
            return CpuModel().Replace(' ', '-').ToLowerInvariant();
        }

        var governor = Sysfs("cpu0/cpufreq/scaling_governor");
        var turbo = ReadFirstLine("/sys/devices/system/cpu/intel_pstate/no_turbo") == "1" ? "noturbo" : "turbo";
        return $"{governor}-{turbo}";
    }

    /// <summary>
    /// macOS has no sysfs. Everything the Linux path reads out of /sys and /proc comes from sysctl
    /// here, shelled out rather than P/Invoked because this runs a dozen times at startup and never
    /// inside anything timed.
    /// </summary>
    internal static class Mac
    {
        private static readonly Dictionary<string, string> Answers = new(StringComparer.Ordinal);

        public static string Text(string key)
        {
            if (Answers.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var value = Unknown;
            try
            {
                using var process = Process.Start(new ProcessStartInfo("/usr/sbin/sysctl", $"-n {key}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });

                if (process is not null)
                {
                    value = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if (process.ExitCode != 0 || value.Length == 0)
                    {
                        value = Unknown;
                    }
                }
            }
            catch (SystemException)
            {
                value = Unknown;
            }

            Answers[key] = value;
            return value;
        }

        public static long Number(string key) =>
            long.TryParse(Text(key), CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private const string Unknown = "unknown";

    private static string Sysfs(string relative) =>
        ReadFirstLine($"/sys/devices/system/cpu/{relative}") ?? Unknown;

    private static string? ReadFirstLine(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadLines(path).FirstOrDefault()?.Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static IEnumerable<string> ReadLines(string path) =>
        File.Exists(path) ? File.ReadLines(path) : [];
}
