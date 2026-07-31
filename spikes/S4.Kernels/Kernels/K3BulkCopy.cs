using BenchmarkDotNet.Attributes;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K3 — bulk copy of the K0 footprint. plans/0004 task 6.
///
/// *Decides:* **ledger #29 directly.** "It must be isolated in its own kernel or the number will be
/// misattributed to the language, which is how the question got asked in the first place. adr/0037
/// deleted the per-Tick copy, but the async save still takes one real copy at save time and this is its
/// price."
///
/// **The source and the destination are one contiguous block each, and the columns are offsets into
/// them.** Both variants therefore move the same bytes across the same addresses; the only thing that
/// differs is how many <c>memcpy</c> calls it takes. That is the whole question — a save that copies
/// column by column makes one call per column, and if per-call overhead were material the save would
/// have to stage through a packed buffer instead. Allocating the columns separately would have made
/// the comparison a comparison of two allocation layouts as well, which is a different kernel.
///
/// **The hand-computed ideal is the footprint over the measured sustained copy rate**, and nothing else:
/// this is a <c>memcpy</c>, so the denominator is the denominator. Any ratio above 1.0 is per-call
/// overhead or page behaviour, and at ~172 MiB there should not be much of either.
/// </summary>
public unsafe class K3BulkCopy
{
    private const long Citizens = 1_000_000;

    private byte* _source;
    private byte* _destination;
    private nuint _totalBytes;
    private nuint[] _columnOffsets = [];
    private nuint[] _columnBytes = [];

    /// <summary>The footprint in bytes, so the report does not have to re-derive it from K0's markdown.</summary>
    public long FootprintBytes => (long)_totalBytes;

    [GlobalSetup]
    public void Setup()
    {
        var offsets = new List<nuint>();
        var sizes = new List<nuint>();

        nuint cursor = 0;
        foreach (var table in WorldSchema.All)
        {
            var rows = table.Rows(Citizens);
            foreach (var column in table.Columns)
            {
                var bytes = (nuint)(rows * column.Bytes);
                if (bytes == 0)
                {
                    continue;
                }

                offsets.Add(cursor);
                sizes.Add(bytes);
                cursor += bytes;
            }
        }

        _columnOffsets = [.. offsets];
        _columnBytes = [.. sizes];
        _totalBytes = cursor;

        // Streams.Allocate first-touches every page, so neither variant takes a fault inside the
        // timed loop and the first invocation is not systematically slower than the rest.
        _source = Streams.Allocate(_totalBytes);
        _destination = Streams.Allocate(_totalBytes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Streams.Free(_source);
        Streams.Free(_destination);
        _source = null;
        _destination = null;
    }

    /// <summary>What the copy costs when nothing stands between it and the memory system.</summary>
    [Benchmark(Baseline = true)]
    public void SingleBlock() => Buffer.MemoryCopy(_source, _destination, _totalBytes, _totalBytes);

    /// <summary>
    /// Equal chunks of a stated size over the same bytes, to find out *why* per-column differs from
    /// single-block rather than only that it does. A gap of that size cannot be per-call overhead —
    /// there are ~104 columns, so it would have to be tens of microseconds a call — which leaves a
    /// change of copy regime, and the regime a <c>memcpy</c> picks is chosen from the length it is
    /// handed. Sweeping the length is the direct test.
    /// </summary>
    [Benchmark]
    [Arguments(64 * 1024)]
    [Arguments(1024 * 1024)]
    [Arguments(8 * 1024 * 1024)]
    [Arguments(32 * 1024 * 1024)]
    public void Chunked(int chunkBytes)
    {
        var source = _source;
        var destination = _destination;
        var chunk = (nuint)chunkBytes;

        for (nuint offset = 0; offset < _totalBytes; offset += chunk)
        {
            var bytes = _totalBytes - offset < chunk ? _totalBytes - offset : chunk;
            Buffer.MemoryCopy(source + offset, destination + offset, bytes, bytes);
        }
    }

    /// <summary>The shape the async save actually has: one call per column, over exactly the same bytes.</summary>
    [Benchmark]
    public void PerColumn()
    {
        var source = _source;
        var destination = _destination;
        var offsets = _columnOffsets;
        var sizes = _columnBytes;

        for (var i = 0; i < offsets.Length; i++)
        {
            var bytes = sizes[i];
            Buffer.MemoryCopy(source + offsets[i], destination + offsets[i], bytes, bytes);
        }
    }
}
