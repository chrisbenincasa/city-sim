using System.Globalization;
using System.Runtime.InteropServices;

namespace S5.Lanes.Harness;

/// <summary>
/// What every S5 report stamps itself with. The same instrument S2 carries, and for the same
/// recorded reason.
/// </summary>
/// <remarks>
/// <para>
/// <b>S4's defect is why this is code rather than a habit.</b> Its M4 Pro capture claimed a sitting
/// it did not share — baseline and kernels 42 hours apart — and nothing printed the denominator's
/// own timestamp beside the figure, so the claim went unchecked. A stamp emitted by the process
/// that produced the numbers cannot be a claim about a moment that did not happen.
/// </para>
/// <para>
/// <b>The governor line is load-bearing for S5 specifically.</b> The corpus already carries a
/// standing caveat that every S2 and S0a absolute is a <c>powersave</c> upper bound. S5's headline
/// is an absolute — Vehicles per Tick per core — and an absolute taken under <c>powersave</c> is a
/// floor on the machine's ability rather than a measurement of it. That has to be legible in the
/// artefact and not only in whoever ran it.
/// </para>
/// </remarks>
internal static class Capture
{
    public static string Stamp()
    {
        var lines = new List<string>
        {
            $"- **Captured** {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC",
            $"- **Machine** {CpuModel()}, {Environment.ProcessorCount} logical processors visible",
            $"- **OS** {RuntimeInformation.OSDescription}, {RuntimeInformation.OSArchitecture}",
            $"- **Runtime** {RuntimeInformation.FrameworkDescription}",
            $"- **Governor** {Governor()}",
            $"- **Turbo** {Turbo()}",
            $"- **Processors allowed** {AffinityList()}",
            $"- **Build** {Configuration()}",
            $"- **Stopwatch** {(System.Diagnostics.Stopwatch.IsHighResolution ? "high resolution" : "LOW RESOLUTION — distrust every figure below")}, {System.Diagnostics.Stopwatch.Frequency} Hz",
        };

        return string.Join(Environment.NewLine, lines);
    }

    public static string Governor()
    {
        const string path = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_governor";
        if (!File.Exists(path))
        {
            return "unknown (no cpufreq)";
        }

        string governor = File.ReadAllText(path).Trim();
        return governor == "powersave"
            ? "**powersave** — every absolute below is a lower bound on this machine's ability"
            : governor;
    }

    private static string Turbo()
    {
        const string path = "/sys/devices/system/cpu/intel_pstate/no_turbo";
        if (!File.Exists(path))
        {
            return "unknown";
        }

        return File.ReadAllText(path).Trim() == "1" ? "disabled" : "enabled";
    }

    private static string CpuModel()
    {
        const string path = "/proc/cpuinfo";
        if (!File.Exists(path))
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
        }

        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("model name", StringComparison.Ordinal))
            {
                int colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon >= 0)
                {
                    return line[(colon + 1)..].Trim();
                }
            }
        }

        return "unknown";
    }

    /// <summary>
    /// Which logical processors this process may run on, read from the kernel rather than computed
    /// from a mask — a mask would need a shift by a non-constant count, which BOR0204 refuses here
    /// for the same reason it refuses it in <c>Core</c>.
    /// </summary>
    private static string AffinityList()
    {
        const string path = "/proc/self/status";
        if (!File.Exists(path))
        {
            return "unknown";
        }

        foreach (string line in File.ReadLines(path))
        {
            if (line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal))
            {
                return line["Cpus_allowed_list:".Length..].Trim();
            }
        }

        return "unknown";
    }

    private static string Configuration()
    {
#if DEBUG
        return "**Debug — every figure below is worthless**";
#else
        return "Release";
#endif
    }

    /// <summary>
    /// Linux PSI stall counters, read before and after the run, so the contention block bounds the
    /// measurement rather than describing the minute before it.
    /// </summary>
    public static long CpuStallMicroseconds()
    {
        const string path = "/proc/pressure/cpu";
        if (!File.Exists(path))
        {
            return -1;
        }

        foreach (string line in File.ReadLines(path))
        {
            int marker = line.IndexOf("total=", StringComparison.Ordinal);
            if (marker >= 0 && long.TryParse(
                    line[(marker + 6)..].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out long total))
            {
                return total;
            }
        }

        return -1;
    }

    public static string Contention(long before, long after)
    {
        if (before < 0 || after < 0)
        {
            return "**Contention** — `/proc/pressure/cpu` unavailable; the run's own window is unmeasured.";
        }

        return $"**Contention** — {after - before} µs of CPU stall accumulated during this run "
            + "(Linux PSI `cpu total`, end minus start). A run with a quiet window reads near zero.";
    }
}
