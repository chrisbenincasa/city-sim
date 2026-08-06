using System.Diagnostics;
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

    /// <summary>
    /// The machine's contention state at this instant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A load average printed at the top of a report describes the minute before the run, which is
    /// the wrong window, and printing it there would be this corpus's own recurring defect one level
    /// over</b> — a value never checked against what it claimed to describe. S4 recorded the desktop's
    /// background load as a defect *after the fact*, reasoning about what it could and could not have
    /// moved; the instrument that would have settled it is one that measures the run's own window.
    /// </para>
    /// <para>
    /// <b>So the load figures are a point sample and are labelled as one, and the load-bearing
    /// instrument is the pressure counter beside them.</b> Linux's PSI <c>total</c> fields are
    /// cumulative microseconds of stall, so <b>end minus start is stall that happened during this
    /// run</b> and nothing else. That is the number that answers *was the machine quiet while this was
    /// measured*, which is the question, rather than *was it quiet a minute beforehand*, which is not.
    /// </para>
    /// <para>
    /// <b>Why this matters more to R1 than to R0.</b> <c>routing-run.sh</c> pins to one physical core
    /// with the SMT sibling idle, so another process must be scheduled onto that core to steal cycles.
    /// That is real protection and it is **no protection against L3 eviction or DRAM bandwidth** —
    /// S4's own finding about the same machine. R1.3 walks a 64 MiB matrix deliberately and its two
    /// most interesting rungs straddle a 12 MB L3, so it is precisely the section another process's
    /// working set can move without ever touching the pinned core.
    /// </para>
    /// </remarks>
    public static MachineState Read()
    {
        bool available = LoadAverages(out int one, out int five, out int fifteen);

        return new MachineState(
            Utc: DateTime.UtcNow,
            LoadOneHundredths: one,
            LoadFiveHundredths: five,
            LoadFifteenHundredths: fifteen,
            LoadAvailable: available,
            CpuStallMicroseconds: PressureStall("cpu"),
            MemoryStallMicroseconds: PressureStall("memory"),
            IoStallMicroseconds: PressureStall("io"),
            Timestamp: Stopwatch.GetTimestamp());
    }

    /// <summary>
    /// The contention block, comparing the state before the run with the state after it.
    /// </summary>
    /// <remarks>
    /// Emitted once, at the end of the report, because that is the only place both readings exist.
    /// It reports the stall as a share of the run rather than raw, since microseconds of stall mean
    /// nothing without the duration they accumulated over.
    /// </remarks>
    public static string Contention(MachineState before, MachineState after)
    {
        // Stopwatch.GetElapsedTime rather than scaling the tick delta by hand, and the hand-rolled
        // version is why this comment exists. It read
        //
        //     (after.Timestamp - before.Timestamp) * 1_000_000_000 / Stopwatch.Frequency
        //
        // which overflows signed 64-bit before it divides: Stopwatch.Frequency is 1e9 on Linux, so a
        // 78-second run is 7.8e10 ticks and the product is 7.8e19 against a ceiling of 9.2e18. It
        // wrapped, and the block reported **4.21 s for a 78-second run and 15.82% CPU stall for
        // 0.85%** — a value never checked against what it claimed to describe, inside the instrument
        // added to close exactly that gap. TimeSpan.Ticks are 100 ns units and cannot overflow here.
        long microseconds = Stopwatch.GetElapsedTime(before.Timestamp, after.Timestamp).Ticks / 10;

        var lines = new List<string>
        {
            "## The machine's own state during this capture",
            string.Empty,
            "**The load averages are a point sample and the stall counters are not.** Linux's PSI"
            + " `total` fields are cumulative microseconds, so the figures below are stall that"
            + " happened **during this run** — which is the question, where a load average read at"
            + " the top of the report would have described the minute before it started.",
            string.Empty,
            string.Create(CultureInfo.InvariantCulture,
                $"- **Run duration** {microseconds / 1_000_000}.{(microseconds / 10_000) % 100:D2} s"
                + $" — from {Instant(before.Utc)} to {Instant(after.Utc)}, **which is what makes the"
                + $" duration checkable rather than asserted**"),
            $"- **Load average, at start** {Format(before)} (1 / 5 / 15 min)",
            $"- **Load average, at end** {Format(after)} (1 / 5 / 15 min)",
            Stall("CPU", before.CpuStallMicroseconds, after.CpuStallMicroseconds, microseconds),
            Stall("Memory", before.MemoryStallMicroseconds, after.MemoryStallMicroseconds, microseconds),
            Stall("IO", before.IoStallMicroseconds, after.IoStallMicroseconds, microseconds),
            string.Empty,
            "**A run whose memory stall is a rounding error is a run the pinning actually protected.**"
            + " Pinning to one physical core stops another process stealing cycles; it does nothing"
            + " about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same"
            + " machine and is the exposure R1.3's absolute nanoseconds live in. This block is what"
            + " lets a later reader check that rather than reason about it afterwards.",
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string Stall(string name, long before, long after, long runMicroseconds)
    {
        if (before < 0 || after < 0)
        {
            return $"- **{name} stall** unavailable — no PSI on this kernel";
        }

        long stall = after - before;
        if (runMicroseconds <= 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"- **{name} stall** {stall:N0} µs");
        }

        // Hundredths of a percent, because a quiet run reports zero at one decimal place and a
        // figure that cannot distinguish "quiet" from "not measured" is the defect this block exists
        // to close.
        long partsPerMillion = stall * 1_000_000 / runMicroseconds;

        return string.Create(CultureInfo.InvariantCulture,
            $"- **{name} stall** {stall:N0} µs over the run — "
            + $"{partsPerMillion / 10_000}.{(partsPerMillion / 100) % 100:D2}% of it");
    }

    private static string Instant(DateTime utc) =>
        utc.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

    private static string Format(MachineState state) =>
        !state.LoadAvailable
            ? "unavailable on this platform"
            : string.Create(CultureInfo.InvariantCulture,
                $"{state.LoadOneHundredths / 100}.{state.LoadOneHundredths % 100:D2} / "
                + $"{state.LoadFiveHundredths / 100}.{state.LoadFiveHundredths % 100:D2} / "
                + $"{state.LoadFifteenHundredths / 100}.{state.LoadFifteenHundredths % 100:D2}");

    /// <summary>
    /// The three load averages as integer hundredths. Parsed by hand because <c>BOR0201</c> stays an
    /// error in <c>Harness/</c> — a report that parses a machine statistic into a <c>double</c> is one
    /// edit away from a cost function that does.
    /// </summary>
    private static bool LoadAverages(out int one, out int five, out int fifteen)
    {
        one = five = fifteen = 0;

        try
        {
            if (!File.Exists("/proc/loadavg"))
            {
                return false;
            }

            string[] fields = File.ReadAllText("/proc/loadavg")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length < 3)
            {
                return false;
            }

            one = Hundredths(fields[0]);
            five = Hundredths(fields[1]);
            fifteen = Hundredths(fields[2]);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static int Hundredths(string value)
    {
        int point = value.IndexOf('.', StringComparison.Ordinal);
        if (point < 0)
        {
            return int.TryParse(value, CultureInfo.InvariantCulture, out int whole) ? whole * 100 : 0;
        }

        int units = int.TryParse(
            value.AsSpan(0, point), CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;

        var fraction = value.AsSpan(point + 1);
        int digits = fraction.Length < 2 ? fraction.Length : 2;
        int hundredths = int.TryParse(
            fraction[..digits], CultureInfo.InvariantCulture, out int part) ? part : 0;

        return (units * 100) + (digits == 1 ? hundredths * 10 : hundredths);
    }

    /// <summary>
    /// The cumulative <c>some total</c> stall in microseconds for a PSI resource, or <c>-1</c> where
    /// the kernel does not expose pressure information.
    /// </summary>
    /// <remarks>
    /// <c>some</c> rather than <c>full</c>: <c>some</c> counts time any task was stalled on the
    /// resource, which is what contention from another process looks like from inside this one.
    /// <c>full</c> counts time <i>every</i> task was stalled, which on a twelve-core desktop running
    /// one pinned benchmark would read zero through contention that mattered.
    /// </remarks>
    private static long PressureStall(string resource)
    {
        string path = $"/proc/pressure/{resource}";

        try
        {
            if (!File.Exists(path))
            {
                return -1;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                if (!line.StartsWith("some", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string field in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (field.StartsWith("total=", StringComparison.Ordinal)
                        && long.TryParse(
                            field.AsSpan("total=".Length), CultureInfo.InvariantCulture, out long total))
                    {
                        return total;
                    }
                }
            }

            return -1;
        }
        catch (IOException)
        {
            return -1;
        }
        catch (UnauthorizedAccessException)
        {
            return -1;
        }
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

/// <remarks>
/// <b>The three load averages are three fields rather than an array, and <c>BOR0701</c> is why.</b>
/// The first draft held an <c>int[]</c> and the analyser refused it — a reference in a struct — which
/// is the managed-state lint doing its job in a harness it was not written for. It is also the better
/// spelling: there are exactly three load averages, forever, so a collection was modelling a
/// variability that does not exist.
/// </remarks>
/// <param name="Utc">Wall clock at the reading. Printed so the duration can be checked against it.</param>
/// <param name="LoadOneHundredths">1-minute load average, integer hundredths.</param>
/// <param name="LoadFiveHundredths">5-minute load average, integer hundredths.</param>
/// <param name="LoadFifteenHundredths">15-minute load average, integer hundredths.</param>
/// <param name="LoadAvailable">Whether <c>/proc/loadavg</c> could be read at all.</param>
/// <param name="CpuStallMicroseconds">Cumulative PSI <c>cpu some total</c>, or <c>-1</c>.</param>
/// <param name="MemoryStallMicroseconds">Cumulative PSI <c>memory some total</c>, or <c>-1</c>.</param>
/// <param name="IoStallMicroseconds">Cumulative PSI <c>io some total</c>, or <c>-1</c>.</param>
/// <param name="Timestamp">A <see cref="Stopwatch"/> tick, so two readings bound a duration.</param>
internal readonly record struct MachineState(
    DateTime Utc,
    int LoadOneHundredths,
    int LoadFiveHundredths,
    int LoadFifteenHundredths,
    bool LoadAvailable,
    long CpuStallMicroseconds,
    long MemoryStallMicroseconds,
    long IoStallMicroseconds,
    long Timestamp);
