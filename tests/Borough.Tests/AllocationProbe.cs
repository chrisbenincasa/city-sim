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
/// <c>alloc-probe-archive/alloc-probe.csv</c> in the test project. It <b>appends</b>, so several runs
/// accumulate in one file, separated by the process id.
/// </para>
/// <para>
/// 🔴 ⚠ <b>It used to land beside the test binaries, and that nearly destroyed the evidence for the
/// open question it serves.</b> <c>AppContext.BaseDirectory</c> is <c>bin/Release/net10.0/</c>, which
/// <c>dotnet clean</c>, a worktree prune and a fresh clone all discard <em>by design</em> — and on
/// 2026-08-21 a dead worktree was about to be removed carrying **48 readings from six full runs that
/// existed nowhere else**, which is the sample <c>plans/0002</c> §B quotes. ***An instrument whose
/// output lives in a build directory is an instrument whose evidence is deleted by routine
/// housekeeping***, and the readings are the slowest thing in this project to reproduce: six full
/// unfiltered runs is about four hours.
/// </para>
/// <para>
/// <b>So the file is durable and committable now.</b> The archive directory sits in the test project
/// rather than under <c>bin/</c>, nothing gitignores it, and the two datasets rescued that day are in
/// it under names saying what they are. ⚠ <b>Whether the readings should be *committed* is a separate
/// question and is deliberately not answered here</b> — it is what would make §B's figures checkable
/// rather than quotable, which is <c>plans/0012</c> **Cause 5**'s repair on a dataset instead of a
/// sentence.
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

        /// <summary>
        /// Methods the JIT compiled <b>on this thread</b> inside the window.
        /// </summary>
        /// <remarks>
        /// <b>The rival mechanism's counter, which this probe recorded nothing for until
        /// 2026-08-26.</b> <c>plans/0003</c> queue item 13 holds a <em>tiered-JIT rejit allocating on
        /// the measuring thread</em> against <c>plans/0002</c> §D's allocation context, and the probe
        /// recorded <see cref="GC.CollectionCount"/> for one of them and nothing at all for the other.
        /// ***An instrument that records only one of two rival mechanisms cannot separate them***, and
        /// separating them is the entire reason the class exists.
        /// </remarks>
        internal long JitMethods;

        /// <summary>IL bytes the JIT compiled on this thread inside the window.</summary>
        /// <remarks>
        /// <b>Carried beside the count because a count of one says nothing about size.</b> A tier-1
        /// promotion of a large method and a first-touch of a tiny one both read as <c>1</c>.
        /// </remarks>
        internal long JitIl;

        /// <summary>When the reading was taken — <see cref="DateTime.UtcNow"/> ticks.</summary>
        /// <remarks>
        /// <para>
        /// 🔴 <b>Owed since the probe was written and taken 2026-08-26.</b> The file carried
        /// <c>process,test,bytes,gen0,gen1,gen2,thread</c> and no clock, so of three samples found that
        /// day only the two somebody watched go red could be dated — the third sat at row 706 of 853
        /// and could have come from any run in four days. ***A reading that cannot be ordered in time
        /// cannot be read against a change to the tree***, which is what <c>plans/0002</c> §D's history
        /// is made of: every sample in it is carried by its date.
        /// </para>
        /// <para>
        /// ⚠ <b>Ticks rather than a formatted string, because <see cref="Record"/> may not allocate.</b>
        /// <see cref="DateTime.UtcNow"/> is a struct and costs nothing; rendering one costs a string,
        /// so the formatting waits until <see cref="Flush"/>. ***The instrument's own constraint
        /// decides the column's type.***
        /// </para>
        /// </remarks>
        internal long Taken;
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
    /// <remarks>
    /// ⚠ <b>Outside <c>bin/</c> on purpose</b> — see the class remark. It falls back to the binary
    /// directory only when the test project cannot be found from the assembly's location, which is a
    /// packaged or single-file layout rather than a normal run; ***losing the rows is worse than
    /// writing them somewhere odd.***
    /// </remarks>
    internal static string Path =>
        Environment.GetEnvironmentVariable("BOROUGH_ALLOC_PROBE")
            is string set && set.Length > 0
            ? set
            : System.IO.Path.Combine(Archive, "alloc-probe.csv");

    /// <summary>
    /// The durable directory the readings accumulate in, created if it is not there.
    /// </summary>
    private static string Archive
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && directory.Name != "Borough.Tests")
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                return AppContext.BaseDirectory;
            }

            string archive = System.IO.Path.Combine(directory.FullName, "alloc-probe-archive");

            Directory.CreateDirectory(archive);

            return archive;
        }
    }

    /// <summary>
    /// Records one reading. Allocates nothing, so it may be called with other windows open.
    /// </summary>
    /// <param name="test">The assertion's name.</param>
    /// <param name="bytes">Bytes the counter moved across the window.</param>
    /// <param name="gen0">Gen0 collections inside the window.</param>
    /// <param name="gen1">Gen1 collections inside the window.</param>
    /// <param name="gen2">Gen2 collections inside the window.</param>
    internal static void Record(
        string test, long bytes, int gen0, int gen1, int gen2, long jitMethods, long jitIl)
    {
        var sample = new Sample
        {
            Test = test,
            Bytes = bytes,
            Gen0 = gen0,
            Gen1 = gen1,
            Gen2 = gen2,
            Thread = Environment.CurrentManagedThreadId,
            JitMethods = jitMethods,
            JitIl = jitIl,
            Taken = DateTime.UtcNow.Ticks,
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
    /// Records one reading <b>and asserts it was zero</b>, which is the only way the eight sites
    /// should be written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The failure message names the file, and that is the whole reason this method exists.</b>
    /// Every non-zero row in <see cref="Path"/> is a test that went red in that process —
    /// <see cref="Record"/> runs before the assertion and writes a firing through immediately — so a
    /// red allocation assertion means <c>plans/0002</c> §B's open question has just been handed a
    /// sample. **One was lost that way**: a 5,208-byte firing sat in the file unread while §B went on
    /// saying the half it answers was untested, because the correct response to an intermittent is to
    /// re-run it and the re-run was green.
    /// </para>
    /// <para>
    /// ⚠ <b>The eight sites wrote the assertion eight times and in two spellings</b> —
    /// <c>Assert.Equal(before, after)</c> and <c>Assert.Equal(0, after - before)</c> — which is what
    /// made them uncountable by grep and left §B naming four of them for months. ***A property
    /// asserted in two spellings is a property nothing can enumerate.***
    /// </para>
    /// </remarks>
    internal static void Check(
        string test,
        long before,
        long after,
        int gen0,
        int gen1,
        int gen2,
        long jitMethods,
        long jitIl)
    {
        int firedGen0 = GC.CollectionCount(0) - gen0;
        int firedGen1 = GC.CollectionCount(1) - gen1;
        int firedGen2 = GC.CollectionCount(2) - gen2;
        long compiled = System.Runtime.JitInfo.GetCompiledMethodCount(currentThread: true) - jitMethods;
        long compiledIl = System.Runtime.JitInfo.GetCompiledILBytes(currentThread: true) - jitIl;

        Record(test, after - before, firedGen0, firedGen1, firedGen2, compiled, compiledIl);

        if (after == before)
        {
            return;
        }

        throw new Xunit.Sdk.XunitException(
            Explain(test, after - before, firedGen0, firedGen1, firedGen2, compiled, compiledIl));
    }

    /// <summary>
    /// What a firing says to whoever is reading the red suite.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Separate from <see cref="Check"/> so that the message can be tested without recording a
    /// sample.</b> The first draft of <c>AllocationAssertionTests</c> asserted on the message by
    /// calling <see cref="Check"/> with a fabricated delta, which appended a **synthetic firing to
    /// the evidence file on every run** — <c>plans/0002</c> §B counts the rows in that file, and six
    /// real samples in four years would have been buried under one fake per test run.
    /// ***A test for an instrument must not write to the instrument's output.***
    /// </remarks>
    internal static string Explain(
        string test, long delta, int gen0, int gen1, int gen2, long jitMethods, long jitIl)
    {
        return
            $"{test} moved the allocation counter by {delta} bytes, with "
            + $"{gen0} gen0, {gen1} gen1 and {gen2} gen2 collections inside the window, and "
            + $"{jitMethods} methods ({jitIl} IL bytes) compiled on this thread inside it.\n\n"
            + "⚠ THIS MAY BE THE INTERMITTENT AND NOT A REGRESSION. plans/0002 §D is open on exactly "
            + "this, and TWO RIVAL MECHANISMS are on the table with the evidence split between them:\n"
            + "  - plans/0002 §D: GC.GetAllocatedBytesForCurrentThread is served out of a per-thread "
            + "allocation context, and every recorded firing is under 8,192 bytes, which is that "
            + "context's size. The gen columns are this mechanism's counter.\n"
            + "  - plans/0003 queue item 13: a tiered-JIT rejit allocating on the measuring thread. "
            + "The jit columns are this mechanism's counter, and they exist only since 2026-08-26.\n\n"
            + "🔴 SO READ THE JIT COLUMNS FIRST -- they are the half nobody had. A firing with jit "
            + "movement inside the window supports item 13; one without it, on a site whose delta "
            + "repeats a previous reading exactly, supports neither and is the state the question was "
            + "left in.\n\n"
            + $"🔴 THE SAMPLE HAS ALREADY BEEN WRITTEN TO {Path} -- go and read it, and put the row "
            + "in plans/0002 §D, BEFORE re-running. A green re-run is the correct response to an "
            + "intermittent and it is also how the last sample was lost.";
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
            $"{new DateTime(sample.Taken, DateTimeKind.Utc):yyyy-MM-ddTHH:mm:ssZ},{Environment.ProcessId},{sample.Test},{sample.Bytes},{sample.Gen0},{sample.Gen1},{sample.Gen2},{sample.Thread},{sample.JitMethods},{sample.JitIl}\n");

    private static void Append(string rows)
    {
        lock (FileGate)
        {
            string path = Path;
            bool fresh = !File.Exists(path);

            File.AppendAllText(
                path,
                fresh
                    ? "taken,process,test,bytes,gen0,gen1,gen2,thread,jit_methods,jit_il\n" + rows
                    : rows);
        }
    }
}
