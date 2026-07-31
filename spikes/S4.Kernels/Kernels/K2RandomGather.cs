using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K2 — random gather by generational handle. plans/0004 task 5: "~2,000 handles into 1M rows, three
/// columns each."
///
/// *Decides:* **the Event Wheel wake pattern**, and it is the most decision-relevant kernel in the suite.
/// If K2 is close to its ideal the Wheel's sparse-wake premise holds at scale; if it is not, everything
/// sized against the Wheel is mis-sized. It is also the kernel 05 §6's Factorio rule is about —
/// *parallelise work that is compute-dense and read-only; do not parallelise work that is memory-bound
/// and pointer-chasing.*
///
/// **The generational check is the kernel, not a preamble.** A handle is {index, generation} and a wake
/// dereferences it by loading <c>generation[index]</c> and comparing before it touches anything else.
/// Every handle here is live and every check passes — the Wheel's list only holds live entities — so what
/// is measured is the cost of the validated dereference, never the cost of the stale branch.
///
/// **Five variants, two questions.**
///
///   Scattered against sorted asks whether ordering a bucket's wake list by row index is worth doing.
///   The Wheel hands back whatever order the intrusive list happens to be in; if sorted is materially
///   faster, keeping the list ordered is a design decision the corpus has not made.
///
///   Struct-of-arrays against array-of-structs asks what SoA charges on a random gather. On a linear scan
///   SoA wins and that is why 05 §3 chose it, but a wake touches three columns of one row — three cache
///   lines under SoA and one under AoS. If the penalty is near 3x, interleaving the *wake tier* into one
///   packed row while leaving the per-Tick tier columnar is a real option, and WorldSchema already
///   separates the tiers.
///
///   Sequential is the control and the empirical floor: the same 2,000 rows, the same three columns, with
///   the prefetcher able to do its job. Nothing in the design gathers this way.
///
/// **The pool exists to defeat cache residency, and it is the subtle part.** 2,000 handles touch at most
/// 384 KiB of column data, which sits inside L3 — so a benchmark that gathers the *same* 2,000 handles
/// on every invocation would measure L3 latency and call it DRAM. The pool is a full permutation of all
/// 1,000,000 rows in 500 windows of 2,000; each invocation takes the next window, so the columns are
/// swept in their entirety before any row repeats. The pool itself is read sequentially, 16 KB per
/// invocation, and is the one part of this kernel the prefetcher is welcome to.
///
/// **The hand-computed ideal.** Under SoA, three columns are three distinct lines per handle: 2,000 x 3 =
/// 6,000 lines = 384 KiB moved per invocation. That is the bandwidth floor and it is the wrong floor —
/// this kernel is latency-bound, not bandwidth-bound. The latency ideal is 6,000 misses at the machine's
/// DRAM latency L, divided by the memory-level parallelism M the core can sustain: 6000 x L / M. On a
/// desktop of this class L is ~80 ns and M is ~10, giving ~48 us. Under AoS it is 2,000 lines and ~16 us.
/// Report the achieved figure against both, and against the sequential control, which is the only one of
/// the three that was measured rather than asserted.
/// </summary>
public unsafe class K2RandomGather
{
    private const int Rows = 1_000_000;
    private const int HandlesPerGather = 2_000;
    private const int Windows = Rows / HandlesPerGather;

    /// <summary>
    /// Index in the high half so that sorting the packed handle sorts by row. A handle is
    /// {index: u32, generation: u32} and which half is which is arbitrary; this half is the one that
    /// makes <c>Array.Sort</c> mean what the sorted variant needs it to mean.
    /// </summary>
    private static ulong Pack(int index, int generation) => ((ulong)(uint)index << 32) | (uint)generation;

    private int* _generation;
    private ulong* _household;
    private long* _nextEventTick;
    private WakeRow* _rows;

    private ulong[] _scattered = [];
    private ulong[] _sorted = [];
    private ulong[] _sequential = [];

    private int _window;

    [GlobalSetup]
    public void Setup()
    {
        _generation = (int*)Streams.Allocate((nuint)Rows * sizeof(int));
        _household = (ulong*)Streams.Allocate((nuint)Rows * sizeof(ulong));
        _nextEventTick = (long*)Streams.Allocate((nuint)Rows * sizeof(long));
        _rows = (WakeRow*)Streams.Allocate((nuint)Rows * (nuint)sizeof(WakeRow));

        for (var i = 0; i < Rows; i++)
        {
            var generation = (int)(CounterHash.Of((ulong)i, 0, CounterHash.Purpose.K2Generation) & 0x7FFF);
            var household = CounterHash.Of((ulong)i, 1, CounterHash.Purpose.K2Generation);
            var nextEventTick = (long)(household & 0x1FFF);

            _generation[i] = generation;
            _household[i] = household;
            _nextEventTick[i] = nextEventTick;
            _rows[i] = new WakeRow
            {
                NextEventTick = nextEventTick,
                Household = household,
                Generation = generation,
            };
        }

        // A full permutation of every row, shuffled once. Fisher-Yates driven by the counter hash
        // rather than System.Random, so the same windows land on both machines.
        var permutation = new int[Rows];
        for (var i = 0; i < Rows; i++)
        {
            permutation[i] = i;
        }

        for (var i = Rows - 1; i > 0; i--)
        {
            var j = (int)(CounterHash.Of((ulong)i, 0, CounterHash.Purpose.K2Permutation) % (ulong)(i + 1));
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }

        _scattered = new ulong[Rows];
        _sequential = new ulong[Rows];
        for (var i = 0; i < Rows; i++)
        {
            _scattered[i] = Pack(permutation[i], _generation[permutation[i]]);
            _sequential[i] = Pack(i, _generation[i]);
        }

        // The same rows in the same windows, ordered within the window. The only difference from
        // scattered is the order, which is what makes the comparison mean anything.
        _sorted = (ulong[])_scattered.Clone();
        for (var w = 0; w < Windows; w++)
        {
            Array.Sort(_sorted, w * HandlesPerGather, HandlesPerGather);
        }

        _window = 0;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Streams.Free((byte*)_generation);
        Streams.Free((byte*)_household);
        Streams.Free((byte*)_nextEventTick);
        Streams.Free((byte*)_rows);
        _generation = null;
        _household = null;
        _nextEventTick = null;
        _rows = null;
    }

    [Benchmark(Baseline = true)]
    public long SoaScattered() => GatherSoa(_scattered);

    [Benchmark]
    public long SoaSorted() => GatherSoa(_sorted);

    [Benchmark]
    public long SoaSequential() => GatherSoa(_sequential);

    [Benchmark]
    public long AosScattered() => GatherAos(_scattered);

    [Benchmark]
    public long AosSorted() => GatherAos(_sorted);

    /// <summary>
    /// Three columns, three lines. The handle pool is a managed array read sequentially and its bounds
    /// check is not what this kernel is about; the columns are native so that what is measured is the
    /// miss and nothing beside it.
    /// </summary>
    private long GatherSoa(ulong[] pool)
    {
        var start = NextWindow();
        var generation = _generation;
        var household = _household;
        var nextEventTick = _nextEventTick;

        long sink = 0;
        for (var k = 0; k < HandlesPerGather; k++)
        {
            var handle = pool[start + k];
            var index = (int)(uint)(handle >> 32);
            if (generation[index] != (int)(uint)handle)
            {
                continue;
            }

            sink += nextEventTick[index] + (long)household[index];
        }

        return sink;
    }

    /// <summary>The same three fields interleaved into one 32-byte row, so a wake is one line instead of three.</summary>
    private long GatherAos(ulong[] pool)
    {
        var start = NextWindow();
        var rows = _rows;

        long sink = 0;
        for (var k = 0; k < HandlesPerGather; k++)
        {
            var handle = pool[start + k];
            var index = (int)(uint)(handle >> 32);
            ref var row = ref rows[index];
            if (row.Generation != (int)(uint)handle)
            {
                continue;
            }

            sink += row.NextEventTick + (long)row.Household;
        }

        return sink;
    }

    private int NextWindow()
    {
        var window = _window;
        _window = window + 1 == Windows ? 0 : window + 1;
        return window * HandlesPerGather;
    }

    /// <summary>
    /// Padded to 32 bytes rather than left at its natural 24. 32 divides the cache line and 24 does not,
    /// so an unpadded row would straddle a line three times in eight and the AoS variant would be
    /// measuring the padding decision instead of the layout decision. The 33% the padding wastes is the
    /// price AoS charges for the one-line guarantee, and it belongs in the comparison.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct WakeRow
    {
        public long NextEventTick;
        public ulong Household;
        public int Generation;
    }
}
