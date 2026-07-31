using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace S4.Kernels.Baseline;

/// <summary>
/// The two streaming kernels the baseline measures, and the buffers they run over. Shared by the
/// single-threaded denominator and the scaling curve deliberately: a curve measured against a
/// different copy loop than the denominator it is divided by would be a comparison of two
/// implementations wearing the same name.
/// </summary>
internal static unsafe class Streams
{
    /// <summary>
    /// Cache-line alignment. 64 bytes on x86-64 and 128 on Apple Silicon — aligning to the wrong
    /// one would put every buffer at a half-line offset on one of the two machines, which is
    /// exactly the kind of difference that gets misread as a property of the hardware.
    /// </summary>
    private static readonly nuint Alignment = OperatingSystem.IsMacOS() ? 128u : 64u;

    /// <summary>
    /// Native, cache-line aligned, and first-touched. Aligned so the measurement is not a
    /// measurement of where the allocator happened to land relative to a cache line, and
    /// first-touched so the timed loop never takes a page fault. Call it on the thread that will
    /// use it.
    /// </summary>
    public static byte* Allocate(nuint bytes)
    {
        var p = (byte*)NativeMemory.AlignedAlloc(bytes, Alignment);
        for (nuint offset = 0; offset < bytes; offset += 64 * 1024)
        {
            var chunk = (int)Math.Min(64 * 1024, bytes - offset);
            new Span<byte>(p + offset, chunk).Fill(0x5A);
        }

        return p;
    }

    public static void Free(byte* p) => NativeMemory.AlignedFree(p);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Copy(byte* dst, byte* src, nuint bytes) =>
        Buffer.MemoryCopy(src, dst, bytes, bytes);

    /// <summary>
    /// Read-only streaming. Four independent accumulators so that the loop is limited by how many
    /// misses the core can have outstanding rather than by the latency of a single dependency chain
    /// — the same reason the real Tick's scans will be written this way.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long Sum(byte* buffer, nuint bytes)
    {
        var words = (long*)buffer;
        var count = (nuint)(bytes / sizeof(long));
        long a = 0, b = 0, c = 0, d = 0;
        for (nuint i = 0; i + 4 <= count; i += 4)
        {
            a += words[i];
            b += words[i + 1];
            c += words[i + 2];
            d += words[i + 3];
        }

        return a + b + c + d;
    }

    /// <summary>Keeps a sum from being optimised away without perturbing the loop that produced it.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Consume(long value)
    {
        if (value == long.MinValue)
        {
            Console.Write(' ');
        }
    }
}
