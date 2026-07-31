using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace S4.Kernels.Baseline;

/// <summary>
/// The denominator. plans/0004 task 1: "Measure sustained single-threaded memcpy bandwidth and
/// record it. This is the denominator for K1, K3 and parts of K2." Every later kernel reports
/// against this number and a hand-computed ideal, never against a Tick budget.
///
/// Two measurements, and the distinction matters:
///
///   The sweep is a burst figure at each size, and its job is to show where the cache levels are.
///   Reporting a spike's L1-resident copy rate as "memory bandwidth" is the classic way to make a
///   later kernel look bad against a denominator it never had access to.
///
///   The sustained figure is the headline, taken at a working-set size several times L3 over a
///   window long enough for the clock to settle. It is the one K1, K3 and K2 divide by.
///
/// Bandwidth is reported two ways because both conventions are in circulation. Copy rate is bytes
/// delivered to the destination per second. Traffic is twice that — every byte copied is a byte
/// read and a byte written, and it is the figure to compare against a DIMM's theoretical peak.
/// GB means 1e9 bytes throughout, not 2^30.
/// </summary>
internal static class MemoryBandwidth
{
    private static readonly (string Label, nuint Bytes)[] SweepSizes =
    [
        ("16 KiB", 16 * 1024),          // inside L1d (32 KiB), both buffers
        ("64 KiB", 64 * 1024),          // L2
        ("512 KiB", 512 * 1024),        // L2 overflow, L3 resident
        ("4 MiB", 4 * 1024 * 1024),     // L3 (12 MiB), both buffers
        ("32 MiB", 32 * 1024 * 1024),   // past L3
        ("256 MiB", 256 * 1024 * 1024), // DRAM, no chance of residency
    ];

    private const nuint SustainedBytes = 256 * 1024 * 1024;

    public static Report Run(int sustainedSeconds)
    {
        var sweep = new List<SizeResult>();
        foreach (var (label, bytes) in SweepSizes)
        {
            sweep.Add(Burst(label, bytes));
        }

        return new Report(
            sweep,
            Sustained(SustainedBytes, sustainedSeconds),
            SustainedRead(SustainedBytes, Math.Max(2, sustainedSeconds / 3)));
    }

    /// <summary>Best-of-many copies at one size. Answers "how fast can it go", not "how fast does it stay".</summary>
    private static unsafe SizeResult Burst(string label, nuint bytes)
    {
        var src = Buffers.Allocate(bytes);
        var dst = Buffers.Allocate(bytes);
        try
        {
            // Chosen so that one sample is long enough for the timer's resolution not to be part
            // of the answer; at 16 KiB a single copy is a few hundred nanoseconds.
            var inner = (int)Math.Max(1, (nuint)(8 * 1024 * 1024) / bytes);
            for (var i = 0; i < inner * 4; i++)
            {
                Copy(dst, src, bytes);
            }

            var samples = new double[64];
            for (var s = 0; s < samples.Length; s++)
            {
                var start = Stopwatch.GetTimestamp();
                for (var i = 0; i < inner; i++)
                {
                    Copy(dst, src, bytes);
                }

                samples[s] = Seconds(Stopwatch.GetTimestamp() - start) / inner;
            }

            Array.Sort(samples);
            return new SizeResult(label, bytes, Rate(bytes, samples[0]), Rate(bytes, Median(samples)));
        }
        finally
        {
            Buffers.Free(src);
            Buffers.Free(dst);
        }
    }

    /// <summary>The headline. Copies for a fixed wall-clock window and reports the whole distribution.</summary>
    private static unsafe SustainedResult Sustained(nuint bytes, int seconds)
    {
        var src = Buffers.Allocate(bytes);
        var dst = Buffers.Allocate(bytes);
        try
        {
            for (var i = 0; i < 4; i++)
            {
                Copy(dst, src, bytes);
            }

            var clockAtStart = MachineDescription.CurrentMaxClockMhz();
            var durations = new List<double>(4096);
            var deadline = Stopwatch.GetTimestamp() + (long)(seconds * (double)Stopwatch.Frequency);
            var clockSum = 0L;
            var clockSamples = 0;
            var nextClockSample = Stopwatch.GetTimestamp();

            while (Stopwatch.GetTimestamp() < deadline)
            {
                var start = Stopwatch.GetTimestamp();
                Copy(dst, src, bytes);
                var end = Stopwatch.GetTimestamp();
                durations.Add(Seconds(end - start));

                if (end >= nextClockSample)
                {
                    clockSum += MachineDescription.CurrentMaxClockMhz();
                    clockSamples++;
                    nextClockSample = end + (Stopwatch.Frequency / 4);
                }
            }

            var sorted = durations.ToArray();
            Array.Sort(sorted);

            return new SustainedResult(
                bytes,
                durations.Count,
                Rate(bytes, sorted[0]),
                Rate(bytes, Percentile(sorted, 0.50)),
                Rate(bytes, Percentile(sorted, 0.95)),
                Rate(bytes, sorted[^1]),
                clockAtStart,
                clockSamples > 0 ? (int)(clockSum / clockSamples) : 0,
                MachineDescription.CurrentMaxClockMhz());
        }
        finally
        {
            Buffers.Free(src);
            Buffers.Free(dst);
        }
    }

    /// <summary>
    /// Read-only streaming rate, recorded as a secondary. K1 is a read-modify-write scan and K2 is
    /// a gather; neither writes as much as it reads, so a pure-copy denominator alone would make
    /// their ideals wrong in opposite directions.
    /// </summary>
    private static unsafe SustainedResult SustainedRead(nuint bytes, int seconds)
    {
        var buffer = Buffers.Allocate(bytes);
        try
        {
            Sum(buffer, bytes);

            var durations = new List<double>(1024);
            var deadline = Stopwatch.GetTimestamp() + (long)(seconds * (double)Stopwatch.Frequency);
            var sink = 0L;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                var start = Stopwatch.GetTimestamp();
                sink += Sum(buffer, bytes);
                durations.Add(Seconds(Stopwatch.GetTimestamp() - start));
            }

            Consume(sink);

            var sorted = durations.ToArray();
            Array.Sort(sorted);
            return new SustainedResult(
                bytes,
                durations.Count,
                Rate(bytes, sorted[0]),
                Rate(bytes, Percentile(sorted, 0.50)),
                Rate(bytes, Percentile(sorted, 0.95)),
                Rate(bytes, sorted[^1]),
                0,
                0,
                0);
        }
        finally
        {
            Buffers.Free(buffer);
        }
    }

    private static unsafe void Copy(byte* dst, byte* src, nuint bytes) => Streams.Copy(dst, src, bytes);

    private static unsafe long Sum(byte* buffer, nuint bytes) => Streams.Sum(buffer, bytes);

    private static void Consume(long value) => Streams.Consume(value);

    private static double Seconds(long ticks) => ticks / (double)Stopwatch.Frequency;

    /// <summary>Bytes per second delivered, in GB/s with GB = 1e9.</summary>
    private static double Rate(nuint bytes, double seconds) => bytes / seconds / 1e9;

    private static double Median(double[] sorted) => Percentile(sorted, 0.50);

    private static double Percentile(double[] sorted, double q)
    {
        var index = (int)Math.Clamp(Math.Round(q * (sorted.Length - 1)), 0, sorted.Length - 1);
        return sorted[index];
    }

    private static unsafe class Buffers
    {
        public static byte* Allocate(nuint bytes) => Streams.Allocate(bytes);

        public static void Free(byte* p) => Streams.Free(p);
    }

    internal sealed record SizeResult(string Label, nuint Bytes, double BestGbPerSec, double MedianGbPerSec);

    internal sealed record SustainedResult(
        nuint Bytes,
        int Copies,
        double BestGbPerSec,
        double MedianGbPerSec,
        double P95GbPerSec,
        double WorstGbPerSec,
        int ClockAtStartMhz,
        int ClockMeanMhz,
        int ClockAtEndMhz);

    internal sealed record Report(
        IReadOnlyList<SizeResult> Sweep,
        SustainedResult SustainedCopy,
        SustainedResult SustainedRead)
    {
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;

            sb.AppendLine("### Copy rate by working-set size (burst)");
            sb.AppendLine();
            sb.AppendLine("Two buffers of the stated size. Best-of-64 and median, single-threaded.");
            sb.AppendLine();
            sb.AppendLine("| Buffer | Best copy | Median copy | Median traffic |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var r in Sweep)
            {
                sb.Append(c, $"| {r.Label} | {r.BestGbPerSec:F1} GB/s | {r.MedianGbPerSec:F1} GB/s | ")
                  .Append(c, $"{r.MedianGbPerSec * 2:F1} GB/s |")
                  .AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("### Sustained single-threaded `memcpy` — the denominator");
            sb.AppendLine();
            var s = SustainedCopy;
            sb.AppendLine(c, $"{s.Bytes / (1024 * 1024)} MiB per copy, {s.Copies} copies.");
            sb.AppendLine();
            sb.AppendLine("| Statistic | Copy rate | Traffic |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine(c, $"| best | {s.BestGbPerSec:F1} GB/s | {s.BestGbPerSec * 2:F1} GB/s |");
            sb.AppendLine(c, $"| **median** | **{s.MedianGbPerSec:F1} GB/s** | **{s.MedianGbPerSec * 2:F1} GB/s** |");
            sb.AppendLine(c, $"| p95 (slow tail) | {s.P95GbPerSec:F1} GB/s | {s.P95GbPerSec * 2:F1} GB/s |");
            sb.AppendLine(c, $"| worst | {s.WorstGbPerSec:F1} GB/s | {s.WorstGbPerSec * 2:F1} GB/s |");
            sb.AppendLine();
            if (s.ClockMeanMhz > 0)
            {
                sb.Append(c, $"Clock during the window: {s.ClockAtStartMhz} MHz at start, ");
                sb.AppendLine(c, $"{s.ClockMeanMhz} MHz mean, {s.ClockAtEndMhz} MHz at end.");
            }
            else
            {
                sb.AppendLine("Clock during the window: not exposed by this OS, so drift cannot be ruled out.");
            }

            sb.AppendLine();
            sb.AppendLine("### Sustained single-threaded read (secondary)");
            sb.AppendLine();
            var r2 = SustainedRead;
            sb.AppendLine(c,
                $"{r2.Bytes / (1024 * 1024)} MiB scanned {r2.Copies} times: " +
                $"median **{r2.MedianGbPerSec:F1} GB/s**, best {r2.BestGbPerSec:F1} GB/s, " +
                $"p95 {r2.P95GbPerSec:F1} GB/s. Read-only, so copy rate and traffic are the same figure.");

            return sb.ToString();
        }
    }
}
