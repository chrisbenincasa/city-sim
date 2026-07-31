using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K4 — many lookups into small sorted arrays. plans/0004 task 7: "≤ 9 entries per array, matching the
/// ResourceMap (05 §3, nine Resources under adr/0031)."
///
/// *Decides:* whether the no-hash-maps rule costs anything. "This is **cache behaviour, not
/// algorithmic** — at nine entries the complexity class is irrelevant and only the layout matters."
///
/// The plan's framing drives the variant list. Binary search over nine entries is not measured, because
/// it is the algorithmic answer to a question the plan says is not algorithmic; what is measured is
/// three *layouts* and the banned alternative.
///
///   `EntryInterleaved` is WorldSchema's current shape: nine {resource, amount, capacity} entries packed
///   end to end, so the row is 81 bytes with no padding and the nine keys are spread across it at a
///   stride of nine. Scanning for a key touches the whole row.
///
///   `KeysThenValues` is the same 81 bytes and the same information, with the nine keys moved to the
///   front. The scan touches nine contiguous bytes and the hit costs one indexed load. Identical memory,
///   identical row stride — only the order within the row differs, which is exactly the kind of change
///   `05 §3` is free to make.
///
///   `KeysThenValuesVector` is that layout with the nine keys compared in a single 128-bit operation
///   instead of a loop. Nine keys fit in one vector with room to spare, so the compare is one
///   instruction and the branch disappears.
///
///   `DictionaryLookup` is the shape the corpus bans. It is here because the plan asks what the rule
///   costs, and a rule whose price has never been measured is a rule nobody can weigh. Its presence is
///   not a proposal — `Dictionary` is barred from simulation code for determinism as much as for speed,
///   and enumeration order is not something a benchmark can price.
///
/// **The ResourceMap is a subset, not a full nine.** A bakery holds Flour and Bread, not all nine
/// Resources, so each map here carries between one and nine sorted keys drawn from the nine. Filling
/// every map would make the lookup degenerate into an array index and measure nothing.
///
/// **The pool defeats residency the same way K2's does.** 200,000 maps at 81 bytes is 16.2 MB against a
/// 12 MB L3, but 2,000 lookups touch only ~160 KB of it — so the lookups are drawn from a permutation of
/// every map in windows of 2,000, and the whole table is swept before any map repeats.
///
/// **The hand-computed ideal.** A lookup is one map, and a map is 81 bytes spanning two cache lines
/// (81 > 64, and the row stride is not a divisor of the line size, so it always straddles). 2,000
/// lookups is therefore ~4,000 lines = 256 KiB against the machine's read rate. The split layouts should
/// beat that, because 200,000 nine-byte key blocks are only 1.8 MB when read alone — but they are not
/// stored alone, so whether they do is the measurement.
/// </summary>
public unsafe class K4SortedLookup
{
    /// <summary>adr/0031's nine Resources.</summary>
    private const int Entries = 9;

    /// <summary>{resource: u8, amount: i32, capacity: i32} x 9, packed. WorldSchema's `bins[9]`.</summary>
    private const int RowBytes = Entries * (1 + 4 + 4);

    private const int LookupsPerBatch = 2_000;

    private int _maps;
    private int _windows;
    private int _window;

    private byte* _interleaved;
    private byte* _split;
    private byte* _counts;

    private ulong[] _pool = [];
    private Dictionary<long, long> _dictionary = [];

    [GlobalSetup]
    public void Setup()
    {
        _maps = (int)MapCount(1_000_000);
        _windows = _maps / LookupsPerBatch;

        _interleaved = Streams.Allocate((nuint)_maps * RowBytes);
        _split = Streams.Allocate((nuint)_maps * RowBytes);
        _counts = Streams.Allocate((nuint)_maps);

        _dictionary = new Dictionary<long, long>(_maps * 5);

        // Hoisted: a stackalloc inside the loop accumulates a frame per iteration and there are
        // 200,000 of them.
        Span<byte> keys = stackalloc byte[Entries];

        for (var m = 0; m < _maps; m++)
        {
            var count = 1 + (int)(CounterHash.Of((ulong)m, 0, CounterHash.Purpose.K4MapShape) % Entries);
            _counts[m] = (byte)count;

            // A sorted subset of the nine Resources. Selection is by hashed rank rather than by
            // rejection sampling, so the loop is bounded and the same on both machines.
            var chosen = 0;
            for (byte resource = 0; resource < Entries && chosen < count; resource++)
            {
                var remaining = Entries - resource;
                var needed = count - chosen;
                if (CounterHash.Of((ulong)m, resource + 1u, CounterHash.Purpose.K4MapShape) % (ulong)remaining < (ulong)needed)
                {
                    keys[chosen++] = resource;
                }
            }

            var interleaved = _interleaved + ((nuint)m * RowBytes);
            var split = _split + ((nuint)m * RowBytes);

            for (var i = 0; i < chosen; i++)
            {
                var amount = (int)(CounterHash.Of((ulong)m, (ulong)i, CounterHash.Purpose.K4BinAmount) & 0xFFFF);
                var capacity = amount + 1024;

                interleaved[i * 9] = keys[i];
                Unsafe.WriteUnaligned(interleaved + (i * 9) + 1, amount);
                Unsafe.WriteUnaligned(interleaved + (i * 9) + 5, capacity);

                split[i] = keys[i];
                Unsafe.WriteUnaligned(split + Entries + (i * 4), amount);
                Unsafe.WriteUnaligned(split + Entries + (Entries * 4) + (i * 4), capacity);

                _dictionary[((long)m << 4) | keys[i]] = ((long)capacity << 32) | (uint)amount;
            }

            // Keys past the count are never read by the scalar variants and are masked off by the
            // vector one, but leaving them at Streams.Allocate's 0x5A would mean a stray 0x5A could
            // never be mistaken for a valid Resource. Set them past the valid range explicitly.
            for (var i = chosen; i < Entries; i++)
            {
                interleaved[i * 9] = 0xFF;
                split[i] = 0xFF;
            }
        }

        // A permutation of every map, in windows of LookupsPerBatch, carrying the Resource to look for
        // in the low bits. Fisher-Yates on the counter hash, never System.Random.
        var permutation = new int[_maps];
        for (var i = 0; i < _maps; i++)
        {
            permutation[i] = i;
        }

        for (var i = _maps - 1; i > 0; i--)
        {
            var j = (int)(CounterHash.Of((ulong)i, 0, CounterHash.Purpose.K4Permutation) % (ulong)(i + 1));
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }

        _pool = new ulong[_maps];
        for (var i = 0; i < _maps; i++)
        {
            var resource = CounterHash.Of((ulong)i, 1, CounterHash.Purpose.K4Permutation) % Entries;
            _pool[i] = ((ulong)(uint)permutation[i] << 32) | resource;
        }

        _window = 0;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Streams.Free(_interleaved);
        Streams.Free(_split);
        Streams.Free(_counts);
        _interleaved = null;
        _split = null;
        _counts = null;
        _dictionary.Clear();
    }

    /// <summary>WorldSchema's shape today: nine {resource, amount, capacity} entries, keys at a stride of nine.</summary>
    [Benchmark(Baseline = true)]
    public long EntryInterleaved()
    {
        var start = NextWindow();
        var pool = _pool;
        long sink = 0;

        for (var k = 0; k < LookupsPerBatch; k++)
        {
            var packed = pool[start + k];
            var map = (int)(uint)(packed >> 32);
            var wanted = (byte)packed;

            var row = _interleaved + ((nuint)map * RowBytes);
            int count = _counts[map];

            for (var i = 0; i < count; i++)
            {
                if (row[i * 9] == wanted)
                {
                    sink += Unsafe.ReadUnaligned<int>(row + (i * 9) + 1);
                    break;
                }
            }
        }

        return sink;
    }

    /// <summary>The same 81 bytes with the nine keys moved to the front, so the scan reads one span.</summary>
    [Benchmark]
    public long KeysThenValues()
    {
        var start = NextWindow();
        var pool = _pool;
        long sink = 0;

        for (var k = 0; k < LookupsPerBatch; k++)
        {
            var packed = pool[start + k];
            var map = (int)(uint)(packed >> 32);
            var wanted = (byte)packed;

            var row = _split + ((nuint)map * RowBytes);
            int count = _counts[map];

            for (var i = 0; i < count; i++)
            {
                if (row[i] == wanted)
                {
                    sink += Unsafe.ReadUnaligned<int>(row + Entries + (i * 4));
                    break;
                }
            }
        }

        return sink;
    }

    /// <summary>
    /// The split layout with the scan replaced by one compare. Nine keys fit inside a 128-bit vector
    /// with seven bytes to spare, and the row is 81 bytes, so loading sixteen never reads past it.
    /// </summary>
    [Benchmark]
    public long KeysThenValuesVector()
    {
        var start = NextWindow();
        var pool = _pool;
        long sink = 0;

        for (var k = 0; k < LookupsPerBatch; k++)
        {
            var packed = pool[start + k];
            var map = (int)(uint)(packed >> 32);
            var wanted = (byte)packed;

            var row = _split + ((nuint)map * RowBytes);
            int count = _counts[map];

            var found = Vector128.Equals(Vector128.Load(row), Vector128.Create(wanted))
                .ExtractMostSignificantBits() & ((1u << count) - 1);

            if (found != 0)
            {
                sink += Unsafe.ReadUnaligned<int>(row + Entries + (BitOperations.TrailingZeroCount(found) * 4));
            }
        }

        return sink;
    }

    /// <summary>
    /// The alternative the corpus bans, priced. Not a proposal: `Dictionary` is barred from simulation
    /// code for enumeration-order determinism as much as for speed, and this measures only the speed.
    /// </summary>
    [Benchmark]
    public long DictionaryLookup()
    {
        var start = NextWindow();
        var pool = _pool;
        var dictionary = _dictionary;
        long sink = 0;

        for (var k = 0; k < LookupsPerBatch; k++)
        {
            var packed = pool[start + k];
            var map = (int)(uint)(packed >> 32);
            var wanted = (long)(byte)packed;

            if (dictionary.TryGetValue(((long)map << 4) | wanted, out var value))
            {
                sink += (int)(uint)value;
            }
        }

        return sink;
    }

    private int NextWindow()
    {
        var window = _window;
        _window = window + 1 == _windows ? 0 : window + 1;
        return window * LookupsPerBatch;
    }

    /// <summary>
    /// Every table carrying a `bins[9]` column has a ResourceMap. Derived from the schema so that a
    /// third table gaining Bins in slice 4 changes this figure rather than silently disagreeing with it.
    /// </summary>
    private static long MapCount(long citizens) =>
        WorldSchema.All
            .Where(table => table.Columns.Any(column => column.Name == "bins[9]"))
            .Sum(table => table.Rows(citizens));
}
