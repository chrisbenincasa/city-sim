using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace S4.Kernels.Baseline;

/// <summary>
/// Aggregate memory bandwidth against thread count.
///
/// The single-threaded denominator says one core takes 78% of this machine's DIMM ceiling on a copy
/// and 45% on a read. Those two figures imply very different answers to the question `05 §6` asks —
/// "parallelise work that is compute-dense and read-only; do not parallelise work that is
/// memory-bound and pointer-chasing" — and that rule is currently an assertion with no number
/// attached. This curve attaches one.
///
/// The number that matters is not the aggregate, which will rise. It is the **per-core share**,
/// which will fall, because that is what a Tick phase spread across threads actually gets. K2's
/// Event Wheel wake pattern is the kernel this decides.
///
/// Each thread owns its buffers. Sharing them would measure cache coherence rather than bandwidth,
/// and coherence traffic on a shared L3 is a different subject with a different answer.
/// </summary>
internal static partial class BandwidthScaling
{
    /// <summary>
    /// Per thread, per buffer. Small enough that twelve threads hold 1.5 GiB rather than something
    /// that has to be swapped, large enough that one thread's pair is 128 MiB and cannot sit in a
    /// 12 MiB L3 even when it has the whole cache to itself.
    /// </summary>
    private const nuint BytesPerThread = 64 * 1024 * 1024;

    public static Report Run(int secondsPerPoint)
    {
        var order = Topology.PlacementOrder(out var cores);
        var points = new List<Point>();

        // One thread per physical core, siblings left idle, up to the core count.
        for (var threads = 1; threads <= cores; threads++)
        {
            points.Add(Measure(threads, order[..threads], secondsPerPoint, cores));
        }

        // Past the core count the only place left to put a thread is an SMT sibling, so these
        // points answer a different question: whether a second thread on a core already streaming
        // finds any bandwidth the first one left on the table. A thread pool sized to 8 on this
        // machine is in exactly this regime, which is why it is measured rather than assumed.
        for (var threads = cores + 2; threads <= order.Length; threads += 2)
        {
            points.Add(Measure(threads, order[..threads], secondsPerPoint, cores));
        }

        return new Report(points);
    }

    private static string Placement(int threads, int cores)
    {
        if (!Topology.CanPin)
        {
            return threads <= cores
                ? $"{threads} threads, OS-placed, {cores} performance cores available"
                : $"{threads} threads, OS-placed, past the {cores} performance cores";
        }

        return threads <= cores
            ? $"{threads} of {cores} physical cores, SMT siblings idle"
            : $"{cores} physical cores + {threads - cores} SMT siblings";
    }

    private static Point Measure(int threads, int[] cpus, int seconds, int cores)
    {
        var copy = RunKernel(threads, cpus, seconds, copying: true);
        var read = RunKernel(threads, cpus, seconds, copying: false);
        return new Point(threads, Placement(threads, cores), copy, read);
    }

    /// <summary>
    /// Every thread allocates its own buffers on its own core, warms them, waits at the gate, then
    /// streams until a shared deadline. Aggregate is total bytes over the wall window; the per-core
    /// share is that divided by the thread count.
    /// </summary>
    private static unsafe KernelResult RunKernel(int threads, int[] cpus, int seconds, bool copying)
    {
        using var ready = new CountdownEvent(threads);
        using var go = new ManualResetEventSlim(false);
        var bytesMoved = new long[threads];
        var deadline = 0L;
        var workers = new Thread[threads];

        for (var t = 0; t < threads; t++)
        {
            var index = t;
            var cpu = cpus[t];
            workers[t] = new Thread(() =>
            {
                Topology.PinCurrentThread(cpu);

                var a = Streams.Allocate(BytesPerThread);
                var b = copying ? Streams.Allocate(BytesPerThread) : null;
                try
                {
                    if (copying)
                    {
                        Streams.Copy(b!, a, BytesPerThread);
                    }
                    else
                    {
                        Streams.Consume(Streams.Sum(a, BytesPerThread));
                    }

                    ready.Signal();
                    go.Wait();

                    var moved = 0L;
                    var sink = 0L;
                    var until = Volatile.Read(ref deadline);
                    while (Stopwatch.GetTimestamp() < until)
                    {
                        if (copying)
                        {
                            Streams.Copy(b!, a, BytesPerThread);
                        }
                        else
                        {
                            sink += Streams.Sum(a, BytesPerThread);
                        }

                        moved += (long)BytesPerThread;
                    }

                    Streams.Consume(sink);
                    bytesMoved[index] = moved;
                }
                finally
                {
                    Streams.Free(a);
                    if (b is not null)
                    {
                        Streams.Free(b);
                    }
                }
            })
            { IsBackground = false, Name = $"s4-stream-{t}" };

            workers[t].Start();
        }

        ready.Wait();
        Volatile.Write(ref deadline, Stopwatch.GetTimestamp() + (long)(seconds * (double)Stopwatch.Frequency));
        var started = Stopwatch.GetTimestamp();
        go.Set();

        foreach (var worker in workers)
        {
            worker.Join();
        }

        var elapsed = (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency;
        var total = bytesMoved.Sum();
        return new KernelResult(total / elapsed / 1e9, threads);
    }

    /// <summary>Which CPUs exist, which are siblings of which, and how to pin a thread to one.</summary>
    private static partial class Topology
    {
        /// <summary>
        /// Every CPU, ordered so that taking the first N fills distinct physical cores before it
        /// doubles up on any core's SMT siblings. Taking them in kernel order instead would put
        /// threads 1 and 2 on the same core's line-fill buffers on some topologies and on different
        /// cores on others, which is how a scaling curve ends up measuring the enumeration order.
        /// <paramref name="cores"/> receives the physical core count — the point past which the
        /// curve stops being about cores at all.
        /// </summary>
        /// <summary>
        /// False on macOS, where threads cannot be pinned at all. Every conclusion drawn from a
        /// curve measured without pinning is weaker than one drawn with it, and the report has to
        /// say so rather than presenting the two as the same measurement.
        /// </summary>
        public static bool CanPin => !OperatingSystem.IsMacOS();

        private static int _qosRefusals;

        /// <summary>
        /// How many threads were refused even the quality-of-service hint. Nonzero means the curve
        /// was measured with no influence over placement whatsoever, which the report must say.
        /// </summary>
        public static int QosRefusals => Volatile.Read(ref _qosRefusals);

        public static int[] PlacementOrder(out int cores)
        {
            if (OperatingSystem.IsMacOS())
            {
                // No pinning, so the returned ids are never used to place anything — only their
                // count matters. The core count reported is the *performance* core count, because
                // that is the point past which an added thread can only land somewhere worse.
                cores = (int)MachineDescription.Mac.Number("hw.perflevel0.physicalcpu");
                var logical = (int)MachineDescription.Mac.Number("hw.logicalcpu");
                cores = cores > 0 ? cores : Environment.ProcessorCount;
                logical = logical > 0 ? logical : Environment.ProcessorCount;
                return [.. Enumerable.Range(0, logical)];
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var first = new List<int>();
            var rest = new List<int>();
            foreach (var cpu in AllCpus())
            {
                var siblings = Read($"/sys/devices/system/cpu/cpu{cpu}/topology/thread_siblings_list")
                    ?? cpu.ToString(CultureInfo.InvariantCulture);
                (seen.Add(siblings) ? first : rest).Add(cpu);
            }

            cores = first.Count;
            return [.. first, .. rest];
        }

        public static int[] AllCpus()
        {
            var cpus = new List<int>();
            foreach (var part in (Read("/sys/devices/system/cpu/online") ?? "0").Split(',', StringSplitOptions.RemoveEmptyEntries))
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

        public static void PinCurrentThread(int cpu)
        {
            if (OperatingSystem.IsMacOS())
            {
                // The nearest available thing to a pin: ask for the quality-of-service class that
                // makes the scheduler prefer a performance core. It is a request, not a placement,
                // and threads past the performance core count will land on efficiency cores
                // whatever it says.
                if (pthread_set_qos_class_self_np(QosUserInteractive, 0) != 0)
                {
                    Interlocked.Increment(ref _qosRefusals);
                }

                return;
            }

            var mask = 1UL << cpu;
            if (sched_setaffinity(0, sizeof(ulong), ref mask) != 0)
            {
                throw new InvalidOperationException(
                    $"could not pin a thread to cpu {cpu} (errno {Marshal.GetLastPInvokeError()}). " +
                    "The scaling curve places threads itself and must not be run under taskset.");
            }
        }

        private static string? Read(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadLines(path).FirstOrDefault()?.Trim() : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>pid 0 means the calling thread, which is what a per-thread pin needs.</summary>
        [LibraryImport("libc", SetLastError = true)]
        private static partial int sched_setaffinity(int pid, nuint cpusetsize, ref ulong mask);

        /// <summary>QOS_CLASS_USER_INTERACTIVE, the class that prefers a performance core.</summary>
        private const uint QosUserInteractive = 0x21;

        [LibraryImport("libSystem.dylib")]
        private static partial int pthread_set_qos_class_self_np(uint qosClass, int relativePriority);
    }

    internal sealed record KernelResult(double AggregateGbPerSec, int Threads)
    {
        public double PerCoreGbPerSec => AggregateGbPerSec / Threads;
    }

    internal sealed record Point(int Threads, string Placement, KernelResult Copy, KernelResult Read);

    internal sealed record Report(IReadOnlyList<Point> Points)
    {
        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;
            var peak = MachineDescription.TheoreticalPeakGbPerSec();
            var one = Points[0];

            sb.AppendLine("### Aggregate bandwidth against thread count");
            sb.AppendLine();
            sb.Append(c, $"{BytesPerThread / (1024 * 1024)} MiB per buffer per thread, private to that thread. ");
            sb.Append("Copy traffic counts the read as well as the write; read traffic is the figure itself. ");
            sb.AppendLine("Per-core is the share one thread gets, which is what a Tick phase spread across threads actually receives.");

            if (!Topology.CanPin)
            {
                sb.AppendLine();
                sb.Append(
                    "**Threads are not pinned on this platform.** Placement is the scheduler's, cores are " +
                    "heterogeneous, and a thread that lands on an efficiency core is not measuring the same " +
                    "machine as one that lands on a performance core. Read the shape of this curve, not its points.");
                sb.AppendLine(Topology.QosRefusals == 0
                    ? " Every thread did at least get the user-interactive quality-of-service class, which asks for a performance core."
                    : $" {Topology.QosRefusals} threads were refused even the user-interactive quality-of-service hint.");
            }
            sb.AppendLine();
            sb.AppendLine("| Threads | Placement | Copy aggregate | Copy traffic | Copy per-core | vs 1 thread | Read aggregate | Read per-core | vs 1 thread |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var p in Points)
            {
                sb.Append(c, $"| {p.Threads} | {p.Placement} | {p.Copy.AggregateGbPerSec:F1} GB/s | ");
                sb.Append(c, $"{p.Copy.AggregateGbPerSec * 2:F1} GB/s | {p.Copy.PerCoreGbPerSec:F1} GB/s | ");
                sb.Append(c, $"{p.Copy.AggregateGbPerSec / one.Copy.AggregateGbPerSec:F2}x | ");
                sb.Append(c, $"{p.Read.AggregateGbPerSec:F1} GB/s | {p.Read.PerCoreGbPerSec:F1} GB/s | ");
                sb.AppendLine(c, $"{p.Read.AggregateGbPerSec / one.Read.AggregateGbPerSec:F2}x |");
            }

            if (peak > 0)
            {
                var best = Points.Max(p => p.Copy.AggregateGbPerSec * 2);
                var bestRead = Points.Max(p => p.Read.AggregateGbPerSec);
                sb.AppendLine();
                sb.Append(c, $"Theoretical DRAM peak is {peak:F1} GB/s. Best copy traffic reached {best:F1} GB/s ");
                sb.Append(c, $"({best / peak * 100:F0}% of peak); best read reached {bestRead:F1} GB/s ");
                sb.AppendLine(c, $"({bestRead / peak * 100:F0}% of peak).");
            }

            return sb.ToString();
        }
    }
}
