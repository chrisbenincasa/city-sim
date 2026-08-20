namespace Borough.Tests;

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

/// <summary>
/// What an exact-equality allocation assertion actually saw, recorded either side of its measured
/// window.
/// </summary>
/// <remarks>
/// <para>
/// <b>The machine <c>plans/0002</c> §B names, and nothing more.</b> The question is whether
/// <c>GC.GetAllocatedBytesForCurrentThread</c>'s error is bounded by one gen0 allocation context, so
/// that an exact equality can survive a suite that runs in parallel. Four discrepancies are on record
/// — 5,672, 5,696, 6,768 and 7,896 bytes — and every one is under <b>8,192</b>, which is the size of
/// that context. ⚠ <b>Four samples is a pattern and not a bound</b>
/// (<c>adr/0043</c>), and this class exists to turn the one into the other or refute it.
/// </para>
/// <para>
/// ⚠ <b>It records every reading and not only the failures, and that is the point.</b> A reading of
/// <em>zero bytes with a collection inside the window</em> refutes <em>any collection perturbs the
/// counter</em> while leaving <em>a jump requires a collection</em> standing, and only the joint
/// distribution separates them. A run that records failures alone cannot tell those two apart, which
/// is the same shape of mistake as ***one green run cannot tell "the cause was removed" from "the
/// intermittent did not fire"*** — the protocol <c>TableAllocationTests</c> carries.
/// </para>
/// <para>
/// ⚠ <b>The probe must not become the thing it measures.</b> <see cref="Record"/> allocates nothing:
/// it stores an already-interned name into a preallocated array under an
/// <see cref="Interlocked"/> index, and <see cref="GC.CollectionCount"/> does not allocate either.
/// The buffered rows are written once, from the assembly hook in
/// <c>TierTimingFramework</c>, after every test has finished — because a file append is an
/// allocation on a shared thread pool, and doing one <em>while another test's window is open</em>
/// would make this class a cause of the effect it is here to observe.
/// </para>
/// <para>
/// ⚠ <b>A non-zero reading is written through immediately, and that exception is deliberate.</b> The
/// event being hunted is rare — milestone 10's gate saw it once in three full suites — so losing one
/// to a crash, a kill or a lost file costs another thirty-six minutes. Once the delta is non-zero the
/// perturbation no longer matters: this thread's window has already closed and already fired.
/// </para>
/// <para>
/// <b>Where the rows land</b>: <c>BOROUGH_ALLOC_PROBE</c> if it is set, otherwise
/// <c>alloc-probe.csv</c> beside the test binaries. It <b>appends</b>, so several runs accumulate in
/// one file, separated by the process id.
/// </para>
/// </remarks>
internal static class AllocationProbe
{
    /// <summary>One assertion's reading.</summary>
    internal struct Sample
    {
        /// <summary>The test that took it.</summary>
        internal string Test;

        /// <summary>Bytes the counter moved across the window. Zero is the assertion passing.</summary>
        internal long Bytes;

        /// <summary>Gen0 collections that happened inside the window.</summary>
        internal int Gen0;

        /// <summary>Gen1 collections that happened inside the window.</summary>
        internal int Gen1;

        /// <summary>Gen2 collections that happened inside the window.</summary>
        internal int Gen2;

        /// <summary>The thread the window ran on.</summary>
        internal int Thread;
    }

    /// <summary>
    /// Room for far more readings than the eight assertion sites can produce in one run, so the
    /// probe never has to decide what to drop.
    /// </summary>
    private const int Capacity = 256;

    private static readonly Sample[] Buffered = new Sample[Capacity];
    private static readonly Lock FileGate = new();
    private static int count = -1;

    /// <summary>The file the rows are appended to.</summary>
    internal static string Path =>
        Environment.GetEnvironmentVariable("BOROUGH_ALLOC_PROBE")
            is string set && set.Length > 0
            ? set
            : System.IO.Path.Combine(AppContext.BaseDirectory, "alloc-probe.csv");

    /// <summary>
    /// Records one reading. Allocates nothing, so it may be called with other windows open.
    /// </summary>
    /// <param name="test">The assertion's name.</param>
    /// <param name="bytes">Bytes the counter moved across the window.</param>
    /// <param name="gen0">Gen0 collections inside the window.</param>
    /// <param name="gen1">Gen1 collections inside the window.</param>
    /// <param name="gen2">Gen2 collections inside the window.</param>
    internal static void Record(string test, long bytes, int gen0, int gen1, int gen2)
    {
        var sample = new Sample
        {
            Test = test,
            Bytes = bytes,
            Gen0 = gen0,
            Gen1 = gen1,
            Gen2 = gen2,
            Thread = Environment.CurrentManagedThreadId,
        };

        int slot = Interlocked.Increment(ref count);

        if (slot < Capacity)
        {
            Buffered[slot] = sample;
        }

        // A firing is what the whole run is for; do not risk carrying it only in memory.
        if (bytes != 0)
        {
            Append(Render(sample));
        }
    }

    /// <summary>
    /// Writes every buffered reading out. Called once, after the last test in the assembly has
    /// finished.
    /// </summary>
    internal static void Flush()
    {
        int taken = Math.Min(Volatile.Read(ref count) + 1, Capacity);

        if (taken <= 0)
        {
            return;
        }

        var text = new StringBuilder();

        for (int i = 0; i < taken; i++)
        {
            // The non-zero ones are already on disk, written through when they fired.
            if (Buffered[i].Bytes == 0)
            {
                text.Append(Render(Buffered[i]));
            }
        }

        if (text.Length > 0)
        {
            Append(text.ToString());
        }
    }

    private static string Render(Sample sample) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Environment.ProcessId},{sample.Test},{sample.Bytes},{sample.Gen0},{sample.Gen1},{sample.Gen2},{sample.Thread}\n");

    private static void Append(string rows)
    {
        lock (FileGate)
        {
            string path = Path;
            bool fresh = !File.Exists(path);

            File.AppendAllText(
                path,
                fresh ? "process,test,bytes,gen0,gen1,gen2,thread\n" + rows : rows);
        }
    }
}
