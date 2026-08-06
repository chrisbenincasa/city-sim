using System.Globalization;
using System.Runtime.InteropServices;

namespace S2.Routing.Harness;

/// <summary>
/// What every S2 report stamps itself with. <c>plans/0010</c> R0: <i>"Record machine, SDK, governor
/// and the denominator's own timestamp."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>S4's recorded defect is the reason this exists as code rather than as a habit.</b> Its M4 Pro
/// capture claimed a sitting it did not share — baseline and kernels 42 hours 44 minutes apart, on a
/// machine with no governor control — and the claim went unchecked because nothing printed the
/// denominator's own timestamp beside the figure. <c>plans/0010</c> says flatly: <i>"S2's harness
/// prints it."</i> A stamp emitted by the same process that produced the numbers cannot be a claim
/// about a moment that did not happen.
/// </para>
/// <para>
/// The footprint curve is machine-independent by construction — it is a count of bytes, not a
/// duration — so this stamp buys nothing for R0's first half except the habit. It buys everything
/// for the second half, and a harness that acquires the habit only once it matters is a harness that
/// acquires it after the first capture is already wrong.
/// </para>
/// </remarks>
internal static class Capture
{
    public static string Stamp()
    {
        var lines = new List<string>
        {
            $"- **Captured** {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} UTC",
            $"- **Machine** {CpuModel()}, {Environment.ProcessorCount} logical processors",
            $"- **OS** {RuntimeInformation.OSDescription}, {RuntimeInformation.OSArchitecture}",
            $"- **Runtime** {RuntimeInformation.FrameworkDescription}",
            $"- **Governor** {Governor()}",
            $"- **Build** {Configuration()}",
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string CpuModel()
    {
        if (!OperatingSystem.IsLinux())
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
        }

        try
        {
            foreach (string line in File.ReadLines("/proc/cpuinfo"))
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
        }
        catch (IOException)
        {
            // A machine description that cannot be read is reported as unknown rather than guessed.
        }

        return "unknown";
    }

    /// <summary>
    /// The frequency governor, because a figure whose machine was in a different power state than
    /// its denominator is a ratio between two machines. Reported as unavailable rather than assumed
    /// on a platform that has no such control — which is itself the fact worth recording, and is
    /// exactly the gap S4's M4 Pro capture fell into.
    /// </summary>
    private static string Governor()
    {
        const string Path = "/sys/devices/system/cpu/cpu0/cpufreq/scaling_governor";

        try
        {
            return File.Exists(Path) ? File.ReadAllText(Path).Trim() : "unavailable on this platform";
        }
        catch (IOException)
        {
            return "unreadable";
        }
    }

    private static string Configuration()
    {
#if DEBUG
        return "**DEBUG — figures from this build are not comparable with anything**";
#else
        return "Release";
#endif
    }
}
