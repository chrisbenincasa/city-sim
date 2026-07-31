using BenchmarkDotNet.Attributes;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K1 — linear scan and update, <c>checked</c> and <c>unchecked</c>. plans/0004 task 4: "Scan-and-update
/// over three struct-of-arrays columns, 1M rows, in two variants."
///
/// *Decides:* the throughput ceiling, whether bounds checks elide, and **the cost of `checked`** — the
/// last unmeasured claim in adr/0003's overflow policy. The ratio between the two variants is the whole
/// reason the second one exists, so the baseline is set on the unchecked span and BenchmarkDotNet
/// reports the rest against it.
///
/// **The arithmetic is adr/0003's fixed-point multiply, not a stand-in.** <c>(int)(((long)a * b) >> 16)</c>
/// accumulated into an i64 is what a Q16.16 multiply-accumulate compiles to, and the two places it can
/// overflow — the narrowing cast and the accumulate — are exactly the two the overflow policy is about.
/// Under <c>checked</c> those become <c>conv.ovf.i4</c> and <c>add.ovf</c>; under <c>unchecked</c> they
/// are free. Measuring anything else would measure the wrong instruction.
///
/// **Four variants, two questions.** Span against pointer answers whether the bounds check elides;
/// checked against unchecked answers what the overflow policy costs. The span loop is bounded by the
/// accumulator's length, so the JIT can prove that index safe and cannot prove the other two — which is
/// the realistic shape for a struct-of-arrays scan, not a handicap.
///
/// **The hand-computed ideal.** Three columns at 1,000,000 rows: read 4 MB of rate, read 4 MB of weight,
/// read-modify-write 8 MB of accumulator. Traffic is 16 MB read plus 8 MB written = 24 MB per invocation.
/// Against a machine's measured sustained `memcpy` traffic figure T (GB/s, and GB is 1e9), the ideal is
/// 24e6 / T seconds. Divide the achieved mean by that; the tripwire is ~3-4x.
///
/// **The inputs cannot overflow, deliberately.** Rate and weight are Q16.16 in [0, 1), so the product is
/// under 2^32, the shift lands under 2^16, and the accumulator grows by at most 65,536 per row. A million
/// rows is 6.5e10 per invocation and an i64 holds 9.2e18, so a hundred million invocations still cannot
/// throw. What is under test is the price of the check, never the price of the exception.
/// </summary>
public unsafe class K1LinearScan
{
    /// <summary>The Citizens table's row count at the target population. WorldSchema, task 2.</summary>
    private const int Rows = 1_000_000;

    private long[] _accumulator = [];
    private int[] _rate = [];
    private int[] _weight = [];

    private long* _accumulatorNative;
    private int* _rateNative;
    private int* _weightNative;

    [GlobalSetup]
    public void Setup()
    {
        _accumulator = new long[Rows];
        _rate = new int[Rows];
        _weight = new int[Rows];

        _accumulatorNative = (long*)Streams.Allocate((nuint)Rows * sizeof(long));
        _rateNative = (int*)Streams.Allocate((nuint)Rows * sizeof(int));
        _weightNative = (int*)Streams.Allocate((nuint)Rows * sizeof(int));

        for (var i = 0; i < Rows; i++)
        {
            var rate = (int)(CounterHash.Of((ulong)i, 0, CounterHash.Purpose.K1Rate) & 0xFFFF);
            var weight = (int)(CounterHash.Of((ulong)i, 0, CounterHash.Purpose.K1Weight) & 0xFFFF);

            _rate[i] = rate;
            _weight[i] = weight;
            _accumulator[i] = 0;

            _rateNative[i] = rate;
            _weightNative[i] = weight;
            _accumulatorNative[i] = 0;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        Streams.Free((byte*)_accumulatorNative);
        Streams.Free((byte*)_rateNative);
        Streams.Free((byte*)_weightNative);
        _accumulatorNative = null;
        _rateNative = null;
        _weightNative = null;
    }

    /// <summary>The idiomatic shape, and the baseline every other variant is reported against.</summary>
    [Benchmark(Baseline = true)]
    public long SpanUnchecked()
    {
        var accumulator = _accumulator.AsSpan();
        var rate = _rate.AsSpan();
        var weight = _weight.AsSpan();

        for (var i = 0; i < accumulator.Length; i++)
        {
            accumulator[i] += (int)(((long)rate[i] * weight[i]) >> 16);
        }

        return accumulator[^1];
    }

    /// <summary>adr/0003's overflow policy, priced. The delta against <see cref="SpanUnchecked"/> is the claim.</summary>
    [Benchmark]
    public long SpanChecked()
    {
        var accumulator = _accumulator.AsSpan();
        var rate = _rate.AsSpan();
        var weight = _weight.AsSpan();

        for (var i = 0; i < accumulator.Length; i++)
        {
            checked
            {
                accumulator[i] += (int)(((long)rate[i] * weight[i]) >> 16);
            }
        }

        return accumulator[^1];
    }

    /// <summary>No bounds check exists to elide. The delta against the span is what the check costs when the JIT cannot prove it away.</summary>
    [Benchmark]
    public long PointerUnchecked()
    {
        var accumulator = _accumulatorNative;
        var rate = _rateNative;
        var weight = _weightNative;

        for (var i = 0; i < Rows; i++)
        {
            accumulator[i] += (int)(((long)rate[i] * weight[i]) >> 16);
        }

        return accumulator[Rows - 1];
    }

    /// <summary>
    /// The overflow policy again, with the bounds check taken out of the comparison — and a trap.
    /// <c>checked</c> is a *block* in C#, so it covers the address arithmetic as well as the value
    /// arithmetic: <c>accumulator[i]</c> on a raw pointer is <c>i * 8</c>, and that multiply gets its own
    /// overflow branch even though no overflow policy has ever been about a byte offset. Compare against
    /// <see cref="PointerCheckedWalked"/>, which computes the same thing without indexing.
    /// </summary>
    [Benchmark]
    public long PointerChecked()
    {
        var accumulator = _accumulatorNative;
        var rate = _rateNative;
        var weight = _weightNative;

        for (var i = 0; i < Rows; i++)
        {
            checked
            {
                accumulator[i] += (int)(((long)rate[i] * weight[i]) >> 16);
            }
        }

        return accumulator[Rows - 1];
    }

    /// <summary>
    /// The same checked arithmetic over the same pointers, walked rather than indexed. Incrementing a
    /// pointer is an add and not a multiply, so nothing here can overflow and the JIT emits no check for
    /// it. Whatever separates this from <see cref="PointerChecked"/> is the price of the block scope
    /// rather than the price of the overflow policy, and the two should not be recorded as one number.
    /// </summary>
    [Benchmark]
    public long PointerCheckedWalked()
    {
        var accumulator = _accumulatorNative;
        var rate = _rateNative;
        var weight = _weightNative;
        var end = accumulator + Rows;

        while (accumulator < end)
        {
            checked
            {
                *accumulator += (int)(((long)*rate * *weight) >> 16);
            }

            accumulator++;
            rate++;
            weight++;
        }

        return _accumulatorNative[Rows - 1];
    }
}
