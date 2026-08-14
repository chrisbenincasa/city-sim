using System.Text;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>
/// L6 — the Lane kernel at 2 and 4 threads, and the equivalence check that has to pass first.
/// </summary>
/// <remarks>
/// <para>
/// <b>This section exists because every Microscopic Cap figure this corpus has ever quoted is one
/// core</b>, deliberately and with the reason recorded: S5 wrote that <em>"the kernels that will be
/// parallelised are decided by"</em> a later spike, and no later spike has run.
/// <c>plans/0002</c> §D2 files the gap as a <em>measurement owed</em> rather than a number to
/// choose, and <c>adr/0096</c> calls it the largest unclaimed multiple on the Cap's supply side.
/// </para>
/// <para>
/// <b>Two rungs, not a sweep.</b> §D2 states the question the rungs answer and it is a yes/no:
/// near-linear to 4 and the supply side is ~4× every published figure, flat at 2 and the one-core
/// numbers are final — which promotes the fallback tier below Microscopic from a probability to an
/// obligation. A sweep to 8, 12 and 16 would characterise <em>this machine's</em> memory system,
/// which is not the question and is the axis <c>spike-results</c> already warns is about a different
/// kind of kernel.
/// </para>
/// <para>
/// <b>The equivalence check runs before any timing, and a failure aborts the section.</b>
/// <c>05 §4</c> lint 4 — <c>run(log, threads=1).hash() == run(log, threads=8).hash()</c> — is one of
/// two invariants <c>CLAUDE.md</c> lists as needing machinery that does not exist. Nothing in this
/// project has ever produced evidence for it either way. A threading number taken over a kernel that
/// does not reproduce its own serial result would be a throughput figure for a different
/// simulation, so the order here is the point rather than a courtesy.
/// </para>
/// </remarks>
internal static class ThreadReport
{
    /// <summary>
    /// The rungs. 1 is the control and is the same code path, so the ratio is over one
    /// implementation; 2 and 4 are §D2's question.
    /// </summary>
    private static readonly int[] Rungs = [1, 2, 4];

    public static string Run()
    {
        var text = new StringBuilder();

        text.AppendLine("## L6 — the kernel at 2 and 4 threads");
        text.AppendLine();
        text.AppendLine(
            "Every Vehicles-per-Tick figure S5 has published is **one core**, and `plans/0002` §D2 "
            + "files the multi-core reading as a *measurement owed* rather than as a number to "
            + "choose. This is that measurement. **Two rungs and not a sweep**: the question is "
            + "whether a compute-bound queue pass scales at all, and near-linear to 4 puts ~4× on "
            + "the Microscopic Cap's supply side while flat at 2 makes the one-core numbers final.");
        text.AppendLine();

        text.AppendLine("### Thread-count equivalence, checked first");
        text.AppendLine();

        int lanes = Findings.ThreadLanes > 0
            ? (int)Findings.ThreadLanes
            : QueueReport.TotalVehicles / Units.VehiclesPerLaneAtJam;

        string equivalence = Equivalence(lanes);
        text.AppendLine(equivalence);
        text.AppendLine();

        if (!Findings.ThreadsAreEquivalent)
        {
            text.AppendLine(
                "**No timings are reported.** A kernel that does not reproduce its serial result is "
                + "not the kernel the one-core figures were taken over, so a throughput number for "
                + "it would describe a different simulation.");
            text.AppendLine();
            return text.ToString();
        }

        text.AppendLine("### Scaling");
        text.AppendLine();
        text.AppendLine(
            $"{Format.Count((long)lanes * Units.VehiclesPerLaneAtJam)} Vehicles across "
            + $"{Format.Count(lanes)} Lanes at {Units.VehiclesPerLaneAtJam} each — the working set "
            + "L3 chose as self-consistent. Lanes are split into contiguous equal ranges, one per "
            + "thread.");
        text.AppendLine();
        text.AppendLine(
            "| Threads | ns/Vehicle | ns/Vehicle (median) | vs 1 thread | Vehicles in 15.6 ms |");
        text.AppendLine("|---:|---:|---:|---:|---:|");

        var perVehicle = new long[Rungs.Length];
        var fits = new long[Rungs.Length];

        for (int i = 0; i < Rungs.Length; i++)
        {
            int threads = Rungs[i];
            var network = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
            var sample = Timing.Measure(
                () => Idm.StepQueuesThreaded(network, threads), network.Vehicles, 250, 9);

            perVehicle[i] = sample.PicosecondsPerUnit;
            fits[i] = sample.UnitsPerTickBudget;

            text.AppendLine(
                $"| {threads} | {Format.Nanoseconds(sample.PicosecondsPerUnit)} "
                + $"| {Format.Nanoseconds(sample.MedianPicosecondsPerUnit)} "
                + $"| {Format.Ratio(perVehicle[0], sample.PicosecondsPerUnit)} "
                + $"| {Format.Count(sample.UnitsPerTickBudget)} |");
        }

        Findings.ThreadPicosecondsAtOne = perVehicle[0];
        Findings.ThreadPicosecondsAtTwo = perVehicle[1];
        Findings.ThreadPicosecondsAtFour = perVehicle[2];
        Findings.ThreadVehiclesAtFour = fits[^1];

        text.AppendLine();
        text.AppendLine(
            "**The *vs 1 thread* column is a speedup and is the whole deliverable** — the "
            + "per-Vehicle nanoseconds are wall-clock per Vehicle across the whole pass, so a "
            + "perfectly scaling kernel halves them at 2 and quarters them at 4.");
        text.AppendLine();

        text.AppendLine("### The refactor's own cost, stated rather than assumed");
        text.AppendLine();
        text.AppendLine(
            "`StepQueuesThreaded` at one thread runs a Lane-range form of the same body, reached "
            + "through one extra call boundary. `StepQueues` — the method every published S5 figure "
            + "was taken over — is left duplicating it rather than delegating to it, so that the "
            + "difference is measurable instead of absorbed. **A refactor of the instrument is a "
            + "change to the measurement until it has been shown not to be.**");
        text.AppendLine();

        var untouched = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var untouchedSample = Timing.Measure(
            () => Idm.StepQueues(untouched), untouched.Vehicles, 250, 9);

        Findings.ThreadPublishedForm = untouchedSample.PicosecondsPerUnit;

        text.AppendLine("| Form | ns/Vehicle | vs published form |");
        text.AppendLine("|---|---:|---:|");
        text.AppendLine(
            $"| `StepQueues` — as published | {Format.Nanoseconds(untouchedSample.PicosecondsPerUnit)} "
            + "| — |");
        text.AppendLine(
            $"| `StepQueuesThreaded(1)` — the control | {Format.Nanoseconds(perVehicle[0])} "
            + $"| {Format.Ratio(perVehicle[0], untouchedSample.PicosecondsPerUnit)} |");
        text.AppendLine();
        text.AppendLine(
            "A ratio near 1.00 means the speedup column above may be read against the published "
            + "one-core figures directly. Anything else has to be carried through, and this row is "
            + "how a reader knows which.");
        text.AppendLine();

        text.AppendLine("### What this number does not carry");
        text.AppendLine();
        text.AppendLine(
            "**Every Lane here holds the same number of Vehicles, and a city's do not.** An equal "
            + "split of Lanes is an equal split of work only on this fixture; under a real "
            + "Microscopic set — which is whatever the Stress trigger promoted, in whatever shape "
            + "congestion left it — a static contiguous partition is the *best* case and the "
            + "measured speedup is a ceiling. That is the S0b finding in its general form: **a unit "
            + "cost is a hypothesis until a real world has produced one**, and no world has produced "
            + "a Microscopic Lane set at all.");
        text.AppendLine();
        text.AppendLine(
            "**And it is one machine.** `spike-results`' bandwidth curves — 1.83× on six desktop "
            + "cores, 3.75× on twelve M4 Pro threads — are about a *streaming* kernel and must not "
            + "be borrowed for this one; S5 measured the Lane pass at 17–29× a bare walk, which is "
            + "the signature of compute rather than of memory. Whether that distinction survives at "
            + "4 threads is exactly what the table above reports, and it reports it here only.");
        text.AppendLine();

        return text.ToString();
    }

    /// <summary>
    /// Steps a serial network and a threaded one from identical seeds and compares every mutated
    /// array, for each rung above 1.
    /// </summary>
    /// <remarks>
    /// <b>The comparison is over whole arrays and not over a checksum</b>, because a checksum that
    /// agreed would leave open which Vehicle disagreed, and the first thing anybody would ask on a
    /// failure is which one. It runs several Ticks rather than one: a race on a Lane boundary that
    /// happened to be quiescent in the opening state would pass a single-Tick check, and the
    /// positions only diverge once the queues have moved.
    /// </remarks>
    private static string Equivalence(int lanes)
    {
        const int ticks = 8;
        var text = new StringBuilder();

        text.AppendLine(
            $"`05 §4` lint 4 is *"
            + "`run(log, threads=1).hash() == run(log, threads=8).hash()`"
            + $"*, and `CLAUDE.md` lists it as one of two invariants needing machinery that does "
            + $"not exist. **Nothing in this project has produced evidence for it either way.** "
            + $"Here it is checked directly: {ticks} Ticks over "
            + $"{Format.Count(lanes)} Lanes, comparing every `Position`, `Velocity` and `Head` "
            + "entry against a serial run of the same seed.");
        text.AppendLine();
        text.AppendLine("| Threads | Vehicle rows compared | Disagreeing |");
        text.AppendLine("|---:|---:|---:|");

        bool equivalent = true;

        var serial = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        for (int tick = 0; tick < ticks; tick++)
        {
            Idm.StepQueuesThreaded(serial, 1);
        }

        foreach (int threads in Rungs)
        {
            if (threads == 1)
            {
                continue;
            }

            var candidate = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
            for (int tick = 0; tick < ticks; tick++)
            {
                Idm.StepQueuesThreaded(candidate, threads);
            }

            long disagreeing = 0;
            for (int i = 0; i < serial.Vehicles; i++)
            {
                if (serial.Position[i] != candidate.Position[i]
                    || serial.Velocity[i] != candidate.Velocity[i])
                {
                    disagreeing++;
                }
            }

            for (int lane = 0; lane < lanes; lane++)
            {
                if (serial.Head[lane] != candidate.Head[lane])
                {
                    disagreeing++;
                }
            }

            equivalent &= disagreeing == 0;

            text.AppendLine(
                $"| {threads} | {Format.Count(serial.Vehicles)} | {Format.Count(disagreeing)} |");
        }

        Findings.ThreadsAreEquivalent = equivalent;
        Findings.ThreadLanes = lanes;

        text.AppendLine();
        text.AppendLine(
            equivalent
                ? "**Equivalent at every rung, and it holds by construction rather than by luck.** "
                  + "Every read and write in the pass is inside the Lane's own `BlockStart .. "
                  + "+Count` rows or is its own `Head`, and the ranges handed to threads are "
                  + "disjoint — so there is no interleaving that could produce a different answer. "
                  + "⚠ **That is a property of *this* kernel and not a discharge of lint 4**, which "
                  + "is about `step()` over the whole world: the Lane pass is the easy case, and "
                  + "the phases with cross-entity writes are where the invariant will actually be "
                  + "tested."
                : "⚠ **NOT equivalent.** The pass does not reproduce its serial result, so `05 §4` "
                  + "lint 4 fails on the one kernel that was expected to satisfy it structurally. "
                  + "Everything below is withheld: a throughput figure for a kernel computing a "
                  + "different answer is not a throughput figure for this simulation.");

        return text.ToString();
    }
}
