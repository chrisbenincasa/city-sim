using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Text;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K6 — the GC tail. plans/0004 task 9: "Hold the whole K0 heap live and run K1–K5 in a loop for **ten
/// minutes**; histogram the per-iteration time; report **p99.9**, not the median."
///
/// *Decides:* adr/0036's named revisit trigger, and it is "**the only kernel that can genuinely surprise
/// and the only one a median hides**".
///
/// **Not a BenchmarkDotNet job.** Task 1 is explicit that BDN's warmup-and-discard model is exactly
/// wrong for a kernel whose whole subject is the tail, so this gets its own command beside `k0`.
///
/// ## The arm that makes this kernel able to fail
///
/// There is a trap in the brief. adr/0036's trigger is a p99.9 beyond 15.6 ms "with the heap already
/// pure unmanaged structs" — but a world in native memory is invisible to the garbage collector, so a
/// loop that allocates nothing over a native heap produces no collections, no pauses, and a p99.9 that
/// is just the kernels' own variance. **It would pass without measuring anything.** A kernel that cannot
/// fail is not a test.
///
/// So K6 runs two arms:
///
///   `unmanaged` — the design as adr/0036 specifies it. The world is native, first-touched, and held
///   live for the whole run. This is the arm the trigger is written against.
///
///   `managed` — the counterfactual adr/0004 and adr/0036 rejected: one object per entity. 1.56M small
///   class instances, linked into a graph so that marking is a pointer chase rather than a walk of a
///   flat root array. This is the shape that makes a gen2 mark expensive, and it is the only way to
///   know whether the unmanaged arm passes *because of the discipline* or merely because nothing was
///   stressing the collector.
///
/// The pair is the result. One number from the first arm alone would say "no pauses were observed",
/// which is true and uninformative.
///
/// ## What one iteration is, stated plainly
///
/// The brief says "run K1–K5 in a loop", and K6 does — it instantiates the real kernel classes and
/// calls into them, rather than keeping a second copy of each shape that could drift. Two deviations,
/// both deliberate:
///
///   K3 is excluded. It is a 172 MiB copy at ~14 ms, which would dominate an iteration and drown the
///   very pauses this kernel exists to find. adr/0037 deleted the per-Tick copy, so a per-iteration
///   bulk copy would also be modelling something the design does not do.
///
///   K5 runs at a Tick's scale rather than its benchmark's. Its `OperationsPerInvoke` batch is a
///   million wakes; here it drains 2,000, which is the order a real Tick drains.
///
/// One iteration is therefore K1's full scan, K2's 2,000-handle gather, K4's 2,000 lookups and K5's
/// 2,000-wake drain — about 1.2 ms, dominated by K1. Ten minutes is roughly 500,000 iterations, which
/// puts ~500 samples above p99.9: enough for the figure to mean something.
///
/// **The iteration is not a Tick and is not offered as one.** It is a repeating unit of the design's
/// real memory shapes, sized so that its duration is comparable to the 15.6 ms budget the trigger names.
///
/// ## Why the pause figures are reported alongside the tail
///
/// A tail can be lengthened by a GC pause, by the operating system, or by the machine's own thermal
/// behaviour, and a histogram cannot tell them apart. <c>GC.GetTotalPauseDuration()</c> reports what the
/// collector actually spent, so K6 reports both: if the tail is long and the pause total is near zero,
/// the tail is not the collector's and adr/0036's trigger has not fired however bad the number looks.
/// </summary>
internal static unsafe class K6GcTail
{
    /// <summary>The Tick budget at 4x speed. adr/0036's trigger is a p99.9 beyond this.</summary>
    private const double TickBudgetMs = 15.6;

    private const int WakesPerIteration = 2_000;

    /// <summary>One churn object. Small enough to be a gen0 citizen, big enough not to be free.</summary>
    private const int ChurnObjectBytes = 224;

    /// <summary>
    /// How many recently-churned objects are held long enough to be promoted. A workload where nothing
    /// survives produces only cheap gen0 collections and never a gen2, which is the one that matters —
    /// a real application always promotes some fraction of what it allocates.
    /// </summary>
    private const int ChurnRingSize = 4096;

    public static Report Run(int minutes, bool managedHeap, int churnKilobytes)
    {
        // The K0 world, held live for the whole run. Native, first-touched, and never read by the
        // loop — its job is to be resident, which is the condition the brief states.
        var world = AllocateWorld(1_000_000, out var worldBytes);

        // The kernels themselves. Instantiated and set up once; their own allocations are part of the
        // live heap too, which is realistic — a real Tick's working set is not only the tables.
        var k1 = new K1LinearScan();
        var k2 = new K2RandomGather();
        var k4 = new K4SortedLookup();
        var k5 = new K5WheelDrain { MeanWakeIntervalTicks = 1024 };
        k1.Setup();
        k2.Setup();
        k4.Setup();
        k5.Setup();

        var managed = managedHeap ? BuildManagedGraph(1_560_000) : null;

        var churnRing = new object?[ChurnRingSize];
        var churnCursor = 0;

        // Sized for a pessimistic iteration rate and allocated before the loop, so that recording a
        // sample can never itself be the thing that triggers a collection.
        var samples = new long[Math.Max(1024, minutes * 60 * 4000)];
        var count = 0;

        // Settle the JIT and the page tables before the clock starts. Not a BDN-style warmup-and-
        // discard — the point is only that tier-1 compilation is not measured as a GC pause.
        for (var i = 0; i < 8; i++)
        {
            Iterate(k1, k2, k4, k5, churnRing, churnKilobytes, ref churnCursor);
        }

        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        var pauseBefore = GC.GetTotalPauseDuration();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);

        var deadline = Stopwatch.GetTimestamp() + (long)(minutes * 60.0 * Stopwatch.Frequency);
        var started = Stopwatch.GetTimestamp();

        while (Stopwatch.GetTimestamp() < deadline)
        {
            var start = Stopwatch.GetTimestamp();
            Iterate(k1, k2, k4, k5, churnRing, churnKilobytes, ref churnCursor);
            var elapsed = Stopwatch.GetTimestamp() - start;

            if (count < samples.Length)
            {
                samples[count++] = elapsed;
            }
        }

        var wall = (Stopwatch.GetTimestamp() - started) / (double)Stopwatch.Frequency;
        var pauses = GC.GetTotalPauseDuration() - pauseBefore;
        var allocated = GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore;

        var collections = new Collections(
            GC.CollectionCount(0) - gen0Before,
            GC.CollectionCount(1) - gen1Before,
            GC.CollectionCount(2) - gen2Before);

        var managedBytes = managed is null ? 0 : GC.GetTotalMemory(forceFullCollection: false);

        GC.KeepAlive(managed);
        GC.KeepAlive(churnRing);
        FreeWorld(world);
        k1.Cleanup();
        k2.Cleanup();
        k4.Cleanup();
        k5.Cleanup();

        return Build(minutes, managedHeap, churnKilobytes, samples.AsSpan(0, count), wall, worldBytes,
            managedBytes, collections, pauses, allocated, count == samples.Length);
    }

    /// <summary>
    /// One iteration. K1's scan dominates; K2, K4 and K5 contribute the random-access shapes, which is
    /// what makes the iteration a mix rather than a single kernel wearing K6's name. The churn is what
    /// makes any of it reach the collector.
    /// </summary>
    private static void Iterate(
        K1LinearScan k1,
        K2RandomGather k2,
        K4SortedLookup k4,
        K5WheelDrain k5,
        object?[] churnRing,
        int churnKilobytes,
        ref int churnCursor)
    {
        Streams.Consume(k1.SpanUnchecked());
        Streams.Consume(k2.SoaScattered());
        Streams.Consume(k4.EntryInterleaved());
        Streams.Consume(k5.DrainWakes(WakesPerIteration));
        Churn(churnRing, churnKilobytes, ref churnCursor);
    }

    /// <summary>
    /// Short-lived managed allocation, which is the only thing here that can cause a collection at all.
    ///
    /// **Without this the kernel cannot fail, and that is not a rhetorical point — it was measured.**
    /// A first version held 172 MiB of native world and 144 MiB of managed objects live for a minute
    /// and recorded *zero* collections and zero pause, because a garbage collector runs when something
    /// allocates and nothing did. Holding a heap live does not exercise the collector; allocating
    /// against it does.
    ///
    /// So K6 models what a real process does around `Borough.Core`: the shell, the UI and the per-frame
    /// snapshot all allocate, and a fraction of what they allocate survives long enough to be promoted.
    /// The rate is a parameter rather than a guess dressed as a constant, and the report states the
    /// resulting MB/s so a reader can judge whether it is the right rate and re-run if not.
    ///
    /// **What K6 actually measures is therefore the right thing:** at a fixed allocation rate, how much
    /// does the size of the *live* managed set change the pause? That is exactly adr/0036's claim — that
    /// GC pauses are manageable once the hot tables are unmanaged structs — and the two arms differ in
    /// nothing but that live set.
    /// </summary>
    private static void Churn(object?[] ring, int kilobytes, ref int cursor)
    {
        var objects = kilobytes * 1024 / ChurnObjectBytes;
        for (var i = 0; i < objects; i++)
        {
            var allocated = new byte[ChurnObjectBytes];

            // Touched so the allocation cannot be elided, and every sixteenth held so that something
            // survives gen0 and the run eventually reaches a gen2.
            allocated[0] = (byte)i;
            if ((i & 15) == 0)
            {
                ring[cursor] = allocated;
                cursor = cursor + 1 == ring.Length ? 0 : cursor + 1;
            }
        }
    }

    private static List<nint> AllocateWorld(long citizens, out long bytes)
    {
        var allocations = new List<nint>();
        long total = 0;

        foreach (var table in WorldSchema.All)
        {
            var rows = table.Rows(citizens);
            foreach (var column in table.Columns)
            {
                var columnBytes = (nuint)(rows * column.Bytes);
                if (columnBytes == 0)
                {
                    continue;
                }

                allocations.Add((nint)Streams.Allocate(columnBytes));
                total += (long)columnBytes;
            }
        }

        bytes = total;
        return allocations;
    }

    private static void FreeWorld(List<nint> allocations)
    {
        foreach (var p in allocations)
        {
            Streams.Free((byte*)p);
        }
    }

    /// <summary>
    /// The rejected shape: one object per entity, linked so that a mark has to chase pointers. adr/0004
    /// rejected an ECS and adr/0036 chose C# partly on the argument that the hot tables would be
    /// unmanaged structs; this is what that argument was avoiding, and pricing it is the only way to
    /// know what the argument bought.
    /// </summary>
    private static ManagedEntity[] BuildManagedGraph(int entities)
    {
        var graph = new ManagedEntity[entities];
        for (var i = 0; i < entities; i++)
        {
            graph[i] = new ManagedEntity { Generation = i, NextEventTick = i & 8191 };
        }

        for (var i = 0; i < entities; i++)
        {
            var link = (int)(CounterHash.Of((ulong)i, 0, CounterHash.Purpose.K6ManagedLink) % (ulong)entities);
            graph[i].Link = graph[link];
        }

        return graph;
    }

    private sealed class ManagedEntity
    {
        public ManagedEntity? Link;
        public long NextEventTick;
        public int Generation;
    }

    private static Report Build(
        int minutes,
        bool managedHeap,
        int churnKilobytes,
        Span<long> samples,
        double wallSeconds,
        long worldBytes,
        long managedBytes,
        Collections collections,
        TimeSpan pauses,
        long allocated,
        bool sampleBufferFull)
    {
        var sorted = samples.ToArray();
        Array.Sort(sorted);

        static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        return new Report(
            minutes,
            managedHeap,
            churnKilobytes,
            sorted.Length,
            wallSeconds,
            worldBytes,
            managedBytes,
            Ms(Percentile(sorted, 0.50)),
            Ms(Percentile(sorted, 0.90)),
            Ms(Percentile(sorted, 0.99)),
            Ms(Percentile(sorted, 0.999)),
            Ms(Percentile(sorted, 0.9999)),
            Ms(sorted.Length > 0 ? sorted[^1] : 0),
            collections,
            pauses,
            allocated,
            sampleBufferFull,
            GCSettings.IsServerGC,
            EffectiveConcurrentGc(),
            GCSettings.LatencyMode,
            CountOver(sorted, TickBudgetMs));
    }

    /// <summary>
    /// The **effective** background-GC setting, not the configured one.
    ///
    /// The first sweep read this from <c>AppContext.TryGetSwitch("System.GC.Concurrent")</c>, which
    /// reports what `runtimeconfig.json` asked for — and the csproj bakes `ConcurrentGarbageCollection`
    /// into that file, so it answered "on" in all eight runs whatever `DOTNET_gcConcurrent` was set to.
    /// Four configurations went in and two labels came out, which is precisely the failure the script's
    /// own comment says the printback exists to catch. It caught it; the printback was the thing at
    /// fault.
    ///
    /// <c>GC.GetConfigurationVariables()</c> — key <c>ConcurrentGC</c> — reports what the collector is actually running with, which
    /// is the only value worth putting in a report. <c>GCSettings.IsServerGC</c> was already a genuine
    /// runtime query, which is why the server dimension came through correctly and this one did not.
    /// </summary>
    private static bool? EffectiveConcurrentGc() =>
        GC.GetConfigurationVariables().TryGetValue("ConcurrentGC", out var value) && value is bool concurrent
            ? concurrent
            : null;

    private static long Percentile(long[] sorted, double q)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Clamp(Math.Round(q * (sorted.Length - 1)), 0, sorted.Length - 1);
        return sorted[index];
    }

    private static int CountOver(long[] sorted, double milliseconds)
    {
        var threshold = (long)(milliseconds / 1000.0 * Stopwatch.Frequency);
        var over = 0;
        for (var i = sorted.Length - 1; i >= 0 && sorted[i] > threshold; i--)
        {
            over++;
        }

        return over;
    }

    internal readonly record struct Collections(int Gen0, int Gen1, int Gen2);

    internal sealed record Report(
        int Minutes,
        bool ManagedHeap,
        int ChurnKilobytes,
        int Iterations,
        double WallSeconds,
        long WorldBytes,
        long ManagedBytes,
        double P50Ms,
        double P90Ms,
        double P99Ms,
        double P999Ms,
        double P9999Ms,
        double MaxMs,
        Collections Collections,
        TimeSpan Pauses,
        long AllocatedBytes,
        bool SampleBufferFull,
        bool ServerGc,
        bool? ConcurrentGc,
        GCLatencyMode LatencyMode,
        int IterationsOverBudget)
    {
        public string Arm => ManagedHeap ? "managed objects" : "unmanaged";

        public string GcTag =>
            $"server={(ServerGc ? "on" : "off")} concurrent={(ConcurrentGc is null ? "default" : ConcurrentGc.Value ? "on" : "off")}";

        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;

            sb.AppendLine(c, $"### K6 — {Arm}, {GcTag}");
            sb.AppendLine();
            var live = ManagedBytes > 0
                ? string.Create(c, $"{Mib(WorldBytes)} of native world plus {Mib(ManagedBytes)} of managed objects")
                : string.Create(c, $"{Mib(WorldBytes)} of native world");
            sb.AppendLine(c, $"{Iterations:N0} iterations over {WallSeconds:F0}s. Live: {live}.");
            sb.AppendLine();
            sb.AppendLine("| Statistic | Per-iteration |");
            sb.AppendLine("|---|---:|");
            sb.AppendLine(c, $"| p50 | {P50Ms:F3} ms |");
            sb.AppendLine(c, $"| p90 | {P90Ms:F3} ms |");
            sb.AppendLine(c, $"| p99 | {P99Ms:F3} ms |");
            sb.AppendLine(c, $"| **p99.9** | **{P999Ms:F3} ms** |");
            sb.AppendLine(c, $"| p99.99 | {P9999Ms:F3} ms |");
            sb.AppendLine(c, $"| max | {MaxMs:F3} ms |");
            sb.AppendLine();
            sb.AppendLine(c,
                $"Collections: {Collections.Gen0:N0} gen0, {Collections.Gen1:N0} gen1, {Collections.Gen2:N0} gen2. " +
                $"Total GC pause **{Pauses.TotalMilliseconds:F1} ms** across the run, {Pauses.TotalMilliseconds / WallSeconds / 10.0:F3}% of wall clock.");
            sb.AppendLine();
            sb.AppendLine(c,
                $"Churn {ChurnKilobytes} KiB per iteration — **{AllocatedBytes / 1024.0 / 1024.0 / WallSeconds:F0} MB/s** of short-lived " +
                $"managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no " +
                $"collection and no pause, whatever is held live; the rate is stated so it can be argued with.");
            sb.AppendLine();

            var verdict = P999Ms > TickBudgetMs
                ? $"**p99.9 of {P999Ms:F3} ms EXCEEDS the {TickBudgetMs} ms Tick budget.**"
                : $"p99.9 of {P999Ms:F3} ms is **{TickBudgetMs / P999Ms:F0}x inside** the {TickBudgetMs} ms Tick budget.";
            sb.AppendLine(c, $"{verdict} {IterationsOverBudget:N0} of {Iterations:N0} iterations exceeded it.");

            if (SampleBufferFull)
            {
                sb.AppendLine();
                sb.AppendLine("**The sample buffer filled and later iterations were not recorded.** The percentiles above");
                sb.AppendLine("describe the start of the run rather than all of it; raise the buffer and re-run.");
            }

            return sb.ToString();
        }

        private static string Mib(long bytes) =>
            string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024.0 / 1024.0:F1} MiB");
    }
}
