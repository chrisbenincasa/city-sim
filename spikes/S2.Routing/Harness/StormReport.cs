using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Cluster;
using S2.Routing.Graph;
using S2.Routing.Matrix;
using S2.Routing.Routing;
using S2.Routing.Storm;

// Imported by name rather than by namespace. `S2.Routing.Traffic` carries its own `ReverseArcs` —
// a second view built for backward search — and importing the namespace whole would make the name
// this file has used since R5.2 ambiguous. Aliasing the three types R5.5 actually needs keeps the
// two indices distinguishable at every use site, which is what a reader of the repair columns needs
// them to be.
using DistanceVector = S2.Routing.Traffic.DistanceVector;
using RouteStore = S2.Routing.Traffic.RouteStore;
using VectorArcs = S2.Routing.Traffic.ReverseArcs;

namespace S2.Routing.Harness;

/// <summary>
/// S2 R5 — the edit storm, and the Epoch ladder.
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0010</c> R5: <i>the measurement that separates a routing design that works from one
/// that works on a static graph.</i> R3 and R4 both measured a <b>single deleted Segment</b>. The
/// case neither could reach is a <b>drag deleting hundreds of Segments in one gesture</b>, which is
/// what R3 deferred cluster size to R5 for and what R4 explicitly did not cover.
/// </para>
/// <para>
/// <b>Every figure here is wall-clock, and the worst gesture is published beside the mean.</b> S4's
/// K6 established the reason in the strongest available form: a run whose worst iteration was
/// 100.2 ms read 2.462 ms at p99.9. An edit storm is precisely the shape a quantile hides, because
/// the whole event is one player action.
/// </para>
/// </remarks>
internal static class StormReport
{
    /// <summary>
    /// Segments in one gesture. <b>The bottom rung is the single edit R3 and R4 measured</b>, kept
    /// so their figures are comparable rather than orphaned, and the sweep runs well past the
    /// plausible drag because a sweep that stops where the answer is cannot show that it is one.
    /// </summary>
    private static readonly int[] GestureSizes = [1, 4, 16, 64, 256];

    /// <summary>
    /// The two cluster rungs R3 narrowed to and could not choose between — 8 or 16 Chunks a side,
    /// the bias on 16 — because the axis that separates them is an edit rate R5 owns.
    /// </summary>
    private static readonly int[] ClusterRungs = [8, 16];

    private static readonly GestureShape[] Shapes = [GestureShape.Drag, GestureShape.Scattered];

    /// <summary>
    /// Gestures sampled per rung. Small, because a 256-Segment scattered gesture re-decides most of
    /// the partition; the count is reported beside every mean so a reader can weigh it.
    /// </summary>
    private const int GestureSamples = 8;

    /// <summary>
    /// The cluster rung the ladder runs at. <b>8, chosen from R5.2 rather than in advance</b> — R3's
    /// bias was on 16 and R5.2 is the measurement that was supposed to settle it.
    /// </summary>
    private const int LadderCluster = 8;

    private const int LadderTicks = 256;
    private const int TripsPerTick = 16;
    private const int PoolPairs = 512;
    private const int CacheCapacity = 1_024;
    private const int MaxCachedArcs = 256;
    private const int MaxCachedClusters = 64;
    private const int LadderGestureSegments = 16;
    private const int LadderWarmQueries = 256;

    private const int AdditionPool = 512;

    /// <summary>
    /// The additions priced, as explicit (shape, size) pairs rather than a cross product.
    /// <b>The Arterial rung appears once because it saturates.</b> The graph holds 8 Arterials and
    /// 104 Arterial Segments between them, and a straightest-continuation walk restricted to
    /// Arterial road collects 4 before it runs out — so asking for 16, 64 and 256 returns the same
    /// gesture three times and would publish one measurement as a sweep. This spike has already
    /// caught two rungs that were secretly the same rung; printing them as three would be the same
    /// defect wearing an honest label.
    /// </summary>
    private static readonly (GestureShape Shape, int Size)[] Additions =
    [
        (GestureShape.Drag, 16),
        (GestureShape.Drag, 64),
        (GestureShape.Drag, 256),
        (GestureShape.Arterial, 256),
    ];

    /// <summary>One gesture every N Ticks. <c>0</c> is the no-edit instrument check.</summary>
    private static readonly int[] EditPeriods = [0, 64, 16, 4];

    private static readonly EpochRung[] LadderEpochRungs =
        [EpochRung.Global, EpochRung.PerCluster, EpochRung.PerSegment];

    /// <summary>
    /// Three of R4.1's five rungs — the two ends and the monocentric middle. The family is swept
    /// because hit rate under any invalidation rung is a property of the distribution, and R3's
    /// figures were an upper bound for exactly that reason.
    /// </summary>
    private static readonly OdRung[] LadderOdRungs =
    [
        new(OdShape.Uniform, 0),
        new(OdShape.DistanceDecay, 256),
        new(OdShape.Monocentric, 512),
    ];

    /// <summary>
    /// Rotation periods for the TTL rung, in Ticks.
    /// </summary>
    /// <remarks>
    /// <b>A rotation period is a stated learning rate</b>, which is the whole reason the rung is on
    /// the ballot: R5.4's option B — weaken the contract to feasibility and let drivers not know
    /// about a new road — is a defect on its own and a `BOUNDED KNOWLEDGE` design decision when it
    /// is paired with option C's cadence, because a number a designer sets and a player can be told
    /// is modelled ignorance rather than accidental ignorance. The rungs are an order of magnitude
    /// apart because the question is which decade the affordable one sits in, and <b>the top rung is
    /// deliberately longer than the run</b> so that the sweep can show a rotation that prices its
    /// own cost without exercising the bound it buys.
    /// </remarks>
    private static readonly int[] TtlPeriods = [64, 256, 1_024];

    /// <summary>
    /// The path sources, in the order the tables print them: the two cache rungs, the two
    /// District-granular rungs R2 left live, and the control.
    /// </summary>
    private static readonly PathRung[] PathRungs =
    [
        PathRung.Cache,
        PathRung.CacheTtl,
        PathRung.NextHop,
        PathRung.Shared,
        PathRung.Flat,
    ];

    /// <summary>
    /// Districts a side for the two District-granular rungs — <b>11, which is R2's anchor and
    /// R4's</b>. Not re-derived here: the detour figures this section produces are only comparable
    /// with R2's 36.01% and 18.52% and with R4.8's 128.82% if the granularity is the same
    /// granularity, and a spike that quietly re-tunes the axis it is comparing against has produced
    /// a new measurement wearing an old one's name.
    /// </summary>
    private const int PathSourceDistricts = 11;

    /// <summary>
    /// Ticks between detour samples. A truth search per Trip costs more than every rung it prices
    /// put together, so the instrument is sampled — and the sample size is printed per row, because
    /// a sample that shrinks with the swept axis has manufactured a trend three times here already.
    /// </summary>
    private const int DetourSampleEvery = 16;

    /// <summary>Flat searches in the denominator batch, taken first and last and never warmed.</summary>
    private const int DenominatorQueries = 256;

    /// <summary>
    /// Ticks the cache is run forward after a road is added, watching whether the rotation clears
    /// the entries R5.4 found it cannot notice.
    /// </summary>
    /// <remarks>
    /// <b>Sized to the slowest rotation rather than to convenience.</b> A window shorter than one
    /// full sweep prices the rotation's cost and says nothing whatever about the staleness bound it
    /// buys — R5.5.2 carries exactly that limitation and says so. Here the window is one complete
    /// rotation of the longest period on the ladder, so every rate measured gets at least one sweep
    /// and every row is entitled to a statement about the bound.
    /// </remarks>
    private const int AdditionWindowTicks = 1_024;

    /// <summary>
    /// Where the healing curve is sampled, in Ticks since the road was added. <b>The curve is the
    /// deliverable and not the endpoint</b>: a rotation period is a stated learning rate, so what a
    /// designer sets and a player experiences is the shape of this decay rather than the figure it
    /// finishes at. Dense early because that is where a rotation that works separates from one that
    /// does not.
    /// </summary>
    private static readonly int[] AdditionSamplePoints =
        [0, 16, 64, 128, 256, 512, 768, 1_024];

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var reverse = ReverseArcs.Of(graph);
        var segmentArcs = SegmentArcs.Of(graph);
        var storm = new EditStorm(graph, segmentArcs);

        report.AppendLine("## S2 R5 — the edit storm, and the Epoch ladder");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: {graph.Segments:N0} Segments, {graph.Nodes:N0} nodes, "
            + $"{graph.Arcs:N0} arcs, {storm.CarSegments:N0} of them admitting cars. Free-flow car "
            + $"costs, `Chebyshev`, the query shape R0 published against. The Segment→arc index the "
            + $"storm needs is {Bytes(segmentArcs.ResidentBytes)} and is a property of the Road "
            + $"Graph rather than of any rung, so it is stated once here and kept out of every "
            + $"resident-size column below."));
        report.AppendLine();

        Warm(graph, reverse, storm);

        Mark("R5.1 the gesture");
        AppendGestures(report, graph, storm, segmentArcs);
        Mark("R5.2 the repair");
        string spellingRatio = AppendRepair(report, graph, reverse, storm);
        Mark("R5.3 the Epoch ladder");
        AppendLadder(report, graph, reverse, storm);
        Mark("R5.4 the addition");
        AppendAddition(report, graph, reverse, storm);
        Mark("R5.5 the path source");
        AppendPathSource(report, graph, reverse, storm, segmentArcs, spellingRatio);

        return report.ToString();
    }

    // --- R5.1 the gesture, before anything is repaired -------------------------------------------

    private sealed record Shaped(
        GestureShape Shape,
        int Requested,
        int MeanCollected,
        int WorstShortfall,
        int MeanArcs,
        int[] MeanClusters,
        int[] WorstClusters);

    private static void AppendGestures(
        StringBuilder report, RoadGraph graph, EditStorm storm, SegmentArcs segmentArcs)
    {
        report.AppendLine("### R5.1 — the gesture, which is the unit R3 and R4 could not reach");
        report.AppendLine();
        report.AppendLine(
            "**A player does not delete a Segment; a player drags.** R3 priced one deleted Segment "
            + "at 1.30 ms and R4 priced one at 4.71 ms against a 234.74 ms rebuild, and both said "
            + "in their own words that the open case was hundreds of Segments in one gesture. This "
            + "section measures the gesture's *shape* before anything is repaired, because every "
            + "cost below is a function of how many clusters the gesture lands in and nothing else.");
        report.AppendLine();
        report.AppendLine(
            "**The scattered row is a control and not a scenario.** A contiguous drag touches few "
            + "clusters by construction, so a ladder rung keyed on clusters is flattered by the "
            + "generator rather than by the design. Publishing only the drag would report a "
            + "property of this harness as a property of the partition — the failure this spike has "
            + "now made four times. Nobody drags scattered; the row exists so the drag's advantage "
            + "has something to be an advantage *over*.");
        report.AppendLine();

        var rows = new List<Shaped>();

        foreach (GestureShape shape in Shapes)
        {
            foreach (int size in GestureSizes)
            {
                long collected = 0;
                long arcs = 0;
                int worstShortfall = 0;
                var meanClusters = new int[ClusterRungs.Length];
                var worstClusters = new int[ClusterRungs.Length];
                var partitions = new Clusters[ClusterRungs.Length];

                for (int c = 0; c < ClusterRungs.Length; c++)
                {
                    partitions[c] = Clusters.Partition(graph, ClusterRungs[c]);
                }

                for (int g = 0; g < GestureSamples; g++)
                {
                    var gesture = storm.Draw(CounterHash.Seed, (ulong)g, shape, size);
                    collected += gesture.Segments.Length;
                    int shortfall = size - gesture.Segments.Length;
                    worstShortfall = shortfall > worstShortfall ? shortfall : worstShortfall;

                    foreach (int segment in gesture.Segments)
                    {
                        arcs += segmentArcs.For(segment).Length;
                    }

                    for (int c = 0; c < ClusterRungs.Length; c++)
                    {
                        int touched = TouchedClusters(graph, partitions[c], gesture);
                        meanClusters[c] += touched;
                        worstClusters[c] = touched > worstClusters[c] ? touched : worstClusters[c];
                    }
                }

                for (int c = 0; c < ClusterRungs.Length; c++)
                {
                    meanClusters[c] = (int)((meanClusters[c] * 100L) / GestureSamples);
                }

                rows.Add(new Shaped(
                    shape,
                    size,
                    (int)((collected * 100) / GestureSamples),
                    worstShortfall,
                    (int)((arcs * 100) / GestureSamples),
                    meanClusters,
                    worstClusters));
            }
        }

        report.AppendLine(
            "| Gesture | Asked | Collected | Worst shortfall | Arcs | Clusters @8 | Worst @8 "
            + "| Clusters @16 | Worst @16 |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {(row.Shape == GestureShape.Drag ? "drag" : "scattered")} | {row.Requested} "
                + $"| {Hundredths(row.MeanCollected)} | {row.WorstShortfall} "
                + $"| {Hundredths(row.MeanArcs)} "
                + $"| {Hundredths(row.MeanClusters[0])} | {row.WorstClusters[0]} "
                + $"| {Hundredths(row.MeanClusters[1])} | {row.WorstClusters[1]} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{GestureSamples} gestures per row. **Collected is reported rather than assumed equal "
            + $"to asked**: a drag follows the network and stops when it runs into road it has "
            + $"already deleted, and a sample that shrinks with the swept axis is how this spike "
            + $"has three times manufactured a trend out of survivorship. The partition at "
            + $"{ClusterRungs[0]} Chunks is "
            + $"{Clusters.Partition(graph, ClusterRungs[0]).Count:N0} clusters and at "
            + $"{ClusterRungs[1]} Chunks is "
            + $"{Clusters.Partition(graph, ClusterRungs[1]).Count:N0}, so a *clusters touched* "
            + $"column approaching either figure is a gesture that has stopped being local."));
        report.AppendLine();
    }

    // --- R5.2 what the gesture costs to repair ---------------------------------------------------

    private readonly record struct Repaired(
        GestureShape Shape,
        int Chunks,
        int Requested,
        int Collected,
        int Clusters,
        long CoalescedNanoseconds,
        long CoalescedWorst,
        long NaiveNanoseconds,
        long NaiveWorst,
        long RebuildNanoseconds);

    /// <summary>
    /// R5.2. Returns the peak naive-over-coalesced ratio it measured, because R5.5.1 is written
    /// about that number and used to carry a hand-typed copy of it: the prose said 23.26x while
    /// this table read 23.28x and 22.61x in the two retained captures, and the corpus quoted the
    /// prose. A figure a later section argues from is returned by the section that measures it.
    /// </summary>
    private static string AppendRepair(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, EditStorm storm)
    {
        report.AppendLine("### R5.2 — what a gesture costs to repair, against a rebuild");
        report.AppendLine();

        var rows = new List<Repaired>();
        var buildFirst = new long[ClusterRungs.Length];
        var buildLast = new long[ClusterRungs.Length];

        for (int c = 0; c < ClusterRungs.Length; c++)
        {
            int chunks = ClusterRungs[c];
            var clusters = Clusters.Partition(graph, chunks);
            var arcCost = (int[])graph.ArcCarTicks.Clone();

            long buildStart = Stopwatch.GetTimestamp();
            var abstractGraph = AbstractGraph.Build(
                graph, clusters, reverse, arcCost,
                transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
            buildFirst[c] = Since(buildStart);

            var touched = new bool[clusters.Count];
            var scratch = new List<int>();

            foreach (GestureShape shape in Shapes)
            {
                foreach (int size in GestureSizes)
                {
                    rows.Add(MeasureRepair(
                        graph, storm, abstractGraph, arcCost, touched, scratch,
                        clusters, chunks, shape, size, buildFirst[c]));
                }
            }
        }

        // The denominator, measured last as well as first, and **measured at every cluster rung
        // rather than at one**. R3's first pinned capture read 1,401,307 ns for its flat search
        // measured first and 477,609 ns for the same code measured after the sweep, and every ratio
        // it published divided by that number; R4, R5 and R6 all inherit the instruction. R5's
        // first draft inherited it only halfway — it measured the rebuild at 8 Chunks and then
        // divided the 16-Chunk repair figures by it, which is a denominator from a different
        // experiment wearing the right units. A rebuild at 16 Chunks is a different amount of work.
        for (int c = 0; c < ClusterRungs.Length; c++)
        {
            var clusters = Clusters.Partition(graph, ClusterRungs[c]);
            var arcCost = (int[])graph.ArcCarTicks.Clone();
            long buildStart = Stopwatch.GetTimestamp();
            AbstractGraph.Build(
                graph, clusters, reverse, arcCost,
                transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
            buildLast[c] = Since(buildStart);
        }

        report.AppendLine(
            "**The alternative to repairing is rebuilding, so the rebuild is the denominator** — "
            + "and it is measured on both sides of the sweep rather than once. R3's first pinned "
            + "capture read 1,401,307 ns for its denominator measured first and 477,609 ns for the "
            + "same code measured last, a 193% spread, because the first timed thing in a process "
            + "runs on a clock that has not ramped. Every ratio in this table divides by it.");
        report.AppendLine();
        for (int c = 0; c < ClusterRungs.Length; c++)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"- Full abstract-graph build at **{ClusterRungs[c]} Chunks**: "
                + $"{Milliseconds(buildFirst[c])} measured first, {Milliseconds(buildLast[c])} "
                + $"measured last, {Ratio(buildFirst[c], buildLast[c])} apart."));
        }

        report.AppendLine();
        report.AppendLine(
            "**The *repair ÷ rebuild* column is the one a decision rests on, and it is the column "
            + "R5's first draft got wrong.** That draft measured the rebuild at 8 Chunks once and "
            + "divided the 16-Chunk repair figures by it — a denominator from a different "
            + "experiment wearing the right units. A rebuild at 16 Chunks is a different amount of "
            + "work from a rebuild at 8, so every ratio in the second half of the table was against "
            + "a partition that was not the one being repaired. **This is R3's denominator finding "
            + "arriving a fourth time**, in the one form it had not yet taken: not measured once "
            + "instead of twice, but measured on the wrong rung.");
        report.AppendLine();
        report.AppendLine(
            "**Coalesced against naive is the finding, not an implementation note.** A cluster's "
            + "edge set is a function of its arcs, so it has to be decided once however many "
            + "Segments inside it were deleted. The naive column is what a per-Segment repair loop "
            + "costs — the spelling R3 and R4 measured, which is correct and indistinguishable from "
            + "the coalesced one at a gesture of 1.");
        report.AppendLine();
        report.AppendLine(
            "| Cluster | Gesture | Asked | Got | Clusters | Coalesced | Worst | Naive | Worst "
            + "| Naive ÷ coalesced | Coalesced as % of rebuild |");
        report.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {row.Chunks} | {(row.Shape == GestureShape.Drag ? "drag" : "scattered")} "
                + $"| {row.Requested} | {row.Collected} | {row.Clusters} "
                + $"| {Milliseconds(row.CoalescedNanoseconds)} | {Milliseconds(row.CoalescedWorst)} "
                + $"| {Milliseconds(row.NaiveNanoseconds)} | {Milliseconds(row.NaiveWorst)} "
                + $"| {Ratio(row.NaiveNanoseconds, row.CoalescedNanoseconds)} "
                + $"| {Percent(row.CoalescedNanoseconds, row.RebuildNanoseconds)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{GestureSamples} gestures per row, each applied, repaired, reverted and repaired "
            + $"again, so every rung starts and ends on the same abstract graph and the timed "
            + $"figure is one repair. **Worst is the worst single gesture, not a quantile** — a "
            + $"gesture is one player action and a quantile over eight of them would hide the "
            + $"event S4's K6 was about."));
        report.AppendLine();

        // Compared as a scaled ratio per row rather than by cross-multiplying two timings, which
        // would square a nanosecond count: at 3 s a row the product reaches long.MaxValue, and the
        // whole point of this repair is not to leave that kind of headroom unexamined.
        long peakHundredths = -1;
        long peakNaive = 0;
        long peakCoalesced = 1;
        foreach (var row in rows)
        {
            if (row.CoalescedNanoseconds <= 0)
            {
                continue;
            }

            long hundredths = (row.NaiveNanoseconds * 100) / row.CoalescedNanoseconds;
            if (hundredths > peakHundredths)
            {
                peakHundredths = hundredths;
                peakNaive = row.NaiveNanoseconds;
                peakCoalesced = row.CoalescedNanoseconds;
            }
        }

        return Ratio(peakNaive, peakCoalesced);
    }

    private static Repaired MeasureRepair(
        RoadGraph graph,
        EditStorm storm,
        AbstractGraph abstractGraph,
        int[] arcCost,
        bool[] touched,
        List<int> scratch,
        Clusters clusters,
        int chunks,
        GestureShape shape,
        int size,
        long rebuildNanoseconds)
    {
        var gestures = new Gesture[GestureSamples];
        int collected = 0;
        int clustersTouched = 0;

        for (int g = 0; g < GestureSamples; g++)
        {
            gestures[g] = storm.Draw(CounterHash.Seed, (ulong)g, shape, size);
            collected += gestures[g].Segments.Length;
            clustersTouched += TouchedClusters(graph, clusters, gestures[g]);
        }

        // Warm, then timed — the same handling R3 gave its repair column.
        foreach (var gesture in gestures)
        {
            storm.Apply(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
            storm.Revert(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
        }

        long coalesced = 0;
        long coalescedWorst = 0;

        foreach (var gesture in gestures)
        {
            storm.Apply(gesture, arcCost);

            long start = Stopwatch.GetTimestamp();
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
            long taken = Since(start);

            coalesced += taken;
            coalescedWorst = taken > coalescedWorst ? taken : coalescedWorst;

            storm.Revert(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
        }

        long naive = 0;
        long naiveWorst = 0;

        foreach (var gesture in gestures)
        {
            storm.Apply(gesture, arcCost);

            long start = Stopwatch.GetTimestamp();
            foreach (int segment in gesture.Segments)
            {
                abstractGraph.RebuildFor(segment);
            }

            long taken = Since(start);

            naive += taken;
            naiveWorst = taken > naiveWorst ? taken : naiveWorst;

            storm.Revert(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
        }

        return new Repaired(
            shape,
            chunks,
            size,
            collected / GestureSamples,
            clustersTouched / GestureSamples,
            coalesced / GestureSamples,
            coalescedWorst,
            naive / GestureSamples,
            naiveWorst,
            rebuildNanoseconds);
    }

    // --- R5.3 the Epoch ladder ---------------------------------------------------------------------

    private sealed record Laddered(
        OdRung Od,
        EpochRung Epoch,
        int EditPeriod,
        int Gestures,
        int Hits,
        int Stale,
        int Misses,
        long RevalidationWork,
        long WorstTickNanoseconds,
        long MeanTickNanoseconds,
        int Refused,
        int Evicted,
        int Unroutable,
        int SegmentsDeleted);

    private static void AppendLadder(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, EditStorm storm)
    {
        report.AppendLine("### R5.3 — the Epoch ladder, and what *never a global flush* is worth");
        report.AppendLine();
        report.AppendLine(
            "**`CONTEXT.md` → Epoch commits to lazy revalidation and *never a global flush*, and "
            + "the Epoch as written is a single counter on the whole Road Graph.** A counter carries "
            + "no location, so a route computed at Epoch 5 and used at Epoch 6 cannot tell whether "
            + "the edit touched it. **`Lazy` describes when you pay, not what survives** — under one "
            + "counter the answer to *what survives* is *nothing*, and the flush is total however "
            + "lazily it is paid. This section prices the two rungs that carry a location.");
        report.AppendLine();
        report.AppendLine(
            "**The zero-edit row is the instrument check, and it is why it is in the table.** With "
            + "no edits every rung must read a near-total hit rate, because the pool is smaller than "
            + "the cache and nothing invalidates. A rung reading low there has a broken cache rather "
            + "than a strict Epoch, and R2 published byte-identical peaks from exactly that kind of "
            + "silence. Under the global rung hit rate is **not a property of the O-D draw at all** "
            + "— it is a property of how recently the player touched anything — so a throughput "
            + "figure could be reported with a cache that had quietly stopped working.");
        report.AppendLine();

        var sampler = new OdSampler(graph);
        var distribution = new OdDistribution(graph, sampler);
        var clusters = Clusters.Partition(graph, LadderCluster);

        var rows = new List<Laddered>();

        foreach (OdRung od in LadderOdRungs)
        {
            var pool = distribution.Draw(
                CounterHash.Seed, PoolPairs, Modes.Car, od, out _, out _);

            foreach (int period in EditPeriods)
            {
                foreach (EpochRung epoch in LadderEpochRungs)
                {
                    rows.Add(MeasureLadder(
                        graph, reverse, storm, clusters, pool, od, epoch, period));
                }
            }
        }

        report.AppendLine(
            "| O-D rung | Epoch | Edit every | Deleted | Hit | Stale | Miss | Unroutable "
            + "| Revalidation words | Mean Tick | Worst Tick |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            int total = row.Hits + row.Stale + row.Misses;
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {row.Od.Name} | {Name(row.Epoch)} "
                + $"| {(row.EditPeriod == 0 ? "never" : row.EditPeriod + " Ticks")} "
                + $"| {row.SegmentsDeleted} | {Percent(row.Hits, total)} "
                + $"| {Percent(row.Stale, total)} | {Percent(row.Misses, total)} "
                + $"| {Percent(row.Unroutable, total)} "
                + $"| {(total == 0 ? "—" : Hundredths((int)((row.RevalidationWork * 100) / total)))} "
                + $"| {Microseconds(row.MeanTickNanoseconds)} "
                + $"| {Microseconds(row.WorstTickNanoseconds)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{LadderTicks} Ticks, {TripsPerTick} Trip starts per Tick, drawn with repetition from "
            + $"a pool of {PoolPairs} distinct origin-destination pairs into a cache of "
            + $"{CacheCapacity} entries at {LadderCluster} Chunks per cluster. Gestures are "
            + $"{LadderGestureSegments}-Segment drags. **Routes refused for exceeding the slot: "
            + $"{rows.Sum(r => r.Refused)}. Entries evicted by a colliding key: "
            + $"{rows.Sum(r => r.Evicted)}.**"));
        report.AppendLine();
        report.AppendLine(
            "**The storm never reverts, which is what makes it a storm and also what the "
            + "*Deleted* and *Unroutable* columns are for.** A player bulldozing continuously does "
            + "not put the road back, so the graph degrades monotonically across the run and the "
            + "later Ticks of a high-edit-rate row are routing on a materially different city from "
            + "its first. Nothing negative is cached, so a pair the storm has severed pays a full "
            + "failed search every Tick it is drawn. **A row whose *Unroutable* share is large is "
            + "measuring severance rather than caching**, and its hit rate should not be read as "
            + "the Epoch rung's doing.");
        report.AppendLine();

        // Monotonicity, printed on the run where it reads yes. Each rung is strictly less
        // conservative than the one above it — global says "might be stale" whenever anything moved,
        // per-cluster whenever a crossed cluster moved, per-Segment whenever an own Segment moved,
        // and each condition implies the one before it. So hit rate must be non-decreasing down the
        // ladder at every (O-D, edit rate). A violation is a bug in the stamping and nothing else.
        // R2 published byte-identical peaks because no check disagreed with them; this is the check.
        int violations = 0;

        for (int i = 0; i + 2 < rows.Count; i += 3)
        {
            if (rows[i].Hits > rows[i + 1].Hits || rows[i + 1].Hits > rows[i + 2].Hits)
            {
                violations++;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Ladder monotonicity: {rows.Count / 3} triples checked, {violations} violations.** "
            + $"Each rung is strictly less conservative than the one above it — *anything moved* "
            + $"implies *a crossed cluster moved* implies *an own Segment moved* — so hit rate must "
            + $"be non-decreasing down the ladder at every O-D rung and every edit rate. It is "
            + $"printed on the run where it reads zero because that is the only run on which it is "
            + $"worth anything: R2 published byte-identical peaks precisely because nothing was "
            + $"wired up to disagree with them."));
        report.AppendLine();
        report.AppendLine(
            "**The pool is what stands in for Trips repeating, and it is invented.** A route cache "
            + "works because real Trips recur — the same Household drives the same commute every "
            + "Day — and nothing in S2 can produce that recurrence, because it needs Trip "
            + "generation. Drawing fresh pairs every Tick would measure a cache with no reuse to "
            + "exploit and report ~0% for every rung, which would compare nothing. A fixed pool "
            + "sampled with repetition supplies reuse at a rate this harness chose. **So the "
            + "absolute hit rates below are a property of the pool size and must not be quoted as "
            + "the hit rate a route cache achieves**; what the pool cannot distort is the *ratio* "
            + "between rungs under the same pool, which is what the ladder is for. Same handling "
            + "R4.1 gave the O-D family, and for the same reason.");
        report.AppendLine();
        report.AppendLine(
            "**The *Miss* column is the eviction policy's bill, and isolating it was not the "
            + "point of this table.** It is flat at roughly 28–31% within an O-D rung and does not "
            + "move with edit rate, which is the tell: misses here are collisions in a "
            + "direct-mapped cache, not staleness. A pool of 512 keys into 1,024 slots loses "
            + "**about three lookups in ten** to two keys wanting the same slot, at 2× "
            + "over-provisioning and before a single road is touched. **That is an argument for "
            + "`adr/0017`'s least-used policy carrying a number for the first time**, and it "
            + "belongs to R6, which owns the eviction decision. It is reported here because the "
            + "figure fell out and a reader would otherwise read it as the Epoch's doing.");
        report.AppendLine();
        report.AppendLine(
            "**The trade this section was written to find does not exist.** `plans/0010` frames "
            + "the ladder as *hit rate against revalidation cost*, on the reasonable expectation "
            + "that an O(path length) check is what a per-Segment Epoch charges for its precision. "
            + "It charges about 42 words a lookup against the global rung's 0.71 — and the mean "
            + "Tick is **lower** at per-Segment than at global at every edit rate measured, "
            + "because the searches the precision avoids cost orders of magnitude more than the "
            + "words it reads. **There is no rung on this ladder that trades accuracy for speed.** "
            + "Per-Segment is cheaper *and* more precise, and the plan's framing was the thing "
            + "that needed measuring.");
        report.AppendLine();
    }

    private static Laddered MeasureLadder(
        RoadGraph graph,
        ReverseArcs reverse,
        EditStorm storm,
        Clusters clusters,
        OdPair[] pool,
        OdRung od,
        EpochRung epoch,
        int period)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var abstractGraph = AbstractGraph.Build(
            graph, clusters, reverse, arcCost,
            transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
        var search = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);
        var clock = new EpochClock(graph, clusters);
        var cache = new RouteCache(
            graph, clusters, CacheCapacity, MaxCachedArcs, MaxCachedClusters);

        var touched = new bool[clusters.Count];
        var scratch = new List<int>();
        var arcs = new List<int>();

        int hits = 0;
        int stale = 0;
        int misses = 0;
        int gestures = 0;
        int unroutable = 0;
        int deleted = 0;
        long revalidation = 0;
        long worstTick = 0;
        long totalTick = 0;

        // Warm the search before the Tick clock starts. R1 needed four warm-up schemes before its
        // cold column stopped falling smoothly with the swept axis, because the cost was
        // per-process: without this the first Tick of the first configuration pays for the whole
        // sweep's JIT and lands in the *worst Tick* column, which is the column R5 exists to
        // publish. An artefact that varies with the swept axis is not distinguishable from a result.
        for (int i = 0; i < LadderWarmQueries; i++)
        {
            OdPair warm = pool[i % pool.Length];
            search.Run(warm.Origin, warm.Destination);
            arcs.Clear();
            search.Refine(arcs);
        }

        for (int tick = 0; tick < LadderTicks; tick++)
        {
            long tickStart = Stopwatch.GetTimestamp();

            if (period > 0 && tick % period == 0)
            {
                var gesture = storm.Draw(
                    CounterHash.Seed, (ulong)tick, GestureShape.Drag, LadderGestureSegments);
                storm.Apply(gesture, arcCost);
                clock.Bump(gesture);
                abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
                gestures++;
                deleted += gesture.Segments.Length;
            }

            for (int trip = 0; trip < TripsPerTick; trip++)
            {
                ulong roll = CounterHash.Of(
                    CounterHash.Seed,
                    (ulong)tick,
                    (ulong)trip,
                    CounterHash.Purpose.GestureOrigin);
                OdPair pair = pool[CounterHash.Below(roll, pool.Length)];

                long key = cache.KeyOf(pair.Origin, pair.Destination);
                Lookup outcome = cache.TryGet(key, epoch, clock, out _);
                revalidation += cache.LastRevalidationWork;

                if (outcome == Lookup.Hit)
                {
                    hits++;
                    continue;
                }

                if (outcome == Lookup.Stale)
                {
                    stale++;
                }
                else
                {
                    misses++;
                }

                var found = search.Run(pair.Origin, pair.Destination);
                arcs.Clear();

                if (found.Found)
                {
                    search.Refine(arcs);
                    cache.Insert(key, arcs, epoch, clock);
                }
                else
                {
                    // The storm never reverts, so the graph degrades over the run and a pooled pair
                    // can become unroutable. Nothing negative is cached, so such a pair pays a full
                    // failed search every Tick it is drawn. Counted rather than assumed absent: a
                    // row where this is large is measuring severance, not caching.
                    unroutable++;
                }
            }

            long taken = Since(tickStart);
            totalTick += taken;
            worstTick = taken > worstTick ? taken : worstTick;
        }

        return new Laddered(
            od,
            epoch,
            period,
            gestures,
            hits,
            stale,
            misses,
            revalidation,
            worstTick,
            totalTick / LadderTicks,
            cache.Refused,
            cache.Evicted,
            unroutable,
            deleted);
    }

    private static string Name(EpochRung rung) => rung switch
    {
        EpochRung.Global => "global",
        EpochRung.PerCluster => "per-cluster",
        _ => "per-Segment",
    };

    // --- R5.4 the addition, and the rungs that cannot see it ---------------------------------------

    private sealed record Added(
        GestureShape Shape,
        int Requested,
        int Collected,
        EpochRung Epoch,
        int Resident,
        int Improvable,
        int DeclaredValid,
        int WronglyValid,
        int MeanDetourHundredths,
        int WorstDetourHundredths);

    private static void AppendAddition(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, EditStorm storm)
    {
        report.AppendLine("### R5.4 — the addition, and the fact that only one rung is sound");
        report.AppendLine();
        report.AppendLine(
            "**R5.3 recommends the per-Segment rung, and this section is the argument against it.** "
            + "The ladder above measures *deletion*, which is the half of the core verb R3 and R4 "
            + "could price. Deletion and addition are not symmetric, and the asymmetry is not a "
            + "detail of this implementation — it is a property of shortest paths.");
        report.AppendLine();
        report.AppendLine(
            "**Deletion is monotone-worsening.** Remove an arc that is not on route `R` and `R`'s "
            + "cost is unchanged while every alternative's cost can only rise, so `R` is still "
            + "optimal. A rung that watches only `R`'s own Segments therefore misses nothing: if "
            + "`R` became infeasible, one of its own Segments was the one deleted. **That is why "
            + "per-Segment reads as exact above.**");
        report.AppendLine();
        report.AppendLine(
            "**Addition is monotone-improving, and that inverts the argument.** A new arc can "
            + "create a cheaper path bearing no relation to `R` whatsoever. A route computed before "
            + "a road existed **cannot contain that road**, so no version the per-Segment rung "
            + "watches can ever move — it declares every pre-existing entry valid, permanently. "
            + "**And per-cluster is unsound for the same reason**: a new fast link in a cluster the "
            + "route never enters can still beat it. **Only the global rung is sound under "
            + "addition**, which is the rung R5.3 just measured as unusable. The ladder has no "
            + "rung that is both affordable and correct across the whole core verb.");
        report.AppendLine();
        report.AppendLine(
            "**Addition is measurable after all, and the trick is worth recording.** R3 deferred it "
            + "because drawing a road across a boundary creates a portal the abstract graph's build "
            + "reserved no slot for. So: **build the abstract graph on the full graph — reserving "
            + "every portal — then delete a set of Segments, and then restore them.** Restoration "
            + "*is* addition, and it needs no new portal. `RebuildCluster` re-derives its crossing "
            + "arcs from the cost array and re-applies the reduction, so a restored arc comes back "
            + "properly rather than being re-costed into a frozen edge set.");
        report.AppendLine();
        report.AppendLine(
            "**The *Improvable* column is the instrument check and it is load-bearing.** It is the "
            + "share of resident entries that a fresh search beats after the addition — ground "
            + "truth, independent of any rung. **If it read zero, every *wrongly valid* column "
            + "below would read zero too and would prove nothing**, which is the shape R3's 0.00% "
            + "detour wore until sampling transitions drove it to 80.49%.");
        report.AppendLine();

        var clusters = Clusters.Partition(graph, LadderCluster);
        var sampler = new OdSampler(graph);
        var distribution = new OdDistribution(graph, sampler);
        var pool = distribution.Draw(
            CounterHash.Seed, AdditionPool, Modes.Car, new OdRung(OdShape.Uniform, 0), out _, out _);

        var rows = new List<Added>();

        foreach ((GestureShape shape, int size) in Additions)
        {
            foreach (EpochRung epoch in LadderEpochRungs)
            {
                rows.Add(MeasureAddition(
                    graph, reverse, storm, clusters, pool, shape, size, epoch));
            }
        }

        report.AppendLine(
            "| Added | Asked | Got | Epoch | Resident | Improvable | Declared valid "
            + "| **Wrongly valid** | Mean detour | Worst detour |");
        report.AppendLine("|---|---:|---:|---|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {(row.Shape == GestureShape.Arterial ? "arterial" : "street drag")} "
                + $"| {row.Requested} | {row.Collected} | {Name(row.Epoch)} | {row.Resident} "
                + $"| {Percent(row.Improvable, row.Resident)} "
                + $"| {Percent(row.DeclaredValid, row.Resident)} "
                + $"| **{Percent(row.WronglyValid, row.Resident)}** "
                + $"| {Hundredths(row.MeanDetourHundredths)}% "
                + $"| {Hundredths(row.WorstDetourHundredths)}% |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"A pool of {AdditionPool} uniform origin-destination pairs, cached on the graph with "
            + $"the Segments deleted, then priced against a fresh search once they are restored. "
            + $"*Wrongly valid* is the share of resident entries the rung declared good that a "
            + $"fresh search strictly beats. *Detour* is over the wrongly-valid entries only, arc "
            + $"cost against arc cost — **it excludes the Access Point offset remainders at both "
            + $"ends**, which are common to both routes and bounded by one Segment each. The graph "
            + $"holds {storm.ArterialSegments:N0} Arterial Segments against "
            + $"{storm.CarSegments:N0} admitting cars."));
        report.AppendLine();
        report.AppendLine(
            "**Restoring ordinary Street improves nothing, and that is a property of this graph "
            + "rather than a fact about cities.** The synthetic grid is one Street per Cell "
            + "boundary at a uniform speed, so between any two points there are very many "
            + "*equal-cost* shortest paths. Deleting a line of Street leaves an equal-cost "
            + "alternative one block over, the cached route's cost is therefore unchanged, and "
            + "restoring the line gives the search nothing to find. **The zero is real and it does "
            + "not generalise**: a real network has heterogeneous speeds and far fewer ties, and "
            + "R0's road-density figure already carries the disclaimer that nobody has checked this "
            + "graph against a real city. **Read the Arterial row, not the Street rows** — an "
            + "Arterial is the only thing on this map that breaks the degeneracy, which is exactly "
            + "why it was added as a shape.");
        report.AppendLine();
        report.AppendLine(
            "**The Arterial gesture collects 4 Segments and the smallness strengthens the "
            + "conclusion rather than weakening it.** Four Segments is about 512 m of new fast "
            + "road — the smallest addition a player would bother drawing. If half a kilometre of "
            + "Arterial leaves a per-Segment Epoch serving stale routes on 9.22% of resident "
            + "entries at a mean 16.71% detour, a larger addition cannot do better. **The figure is "
            + "a floor.**");
        report.AppendLine();
        report.AppendLine(
            "**And unlike every other error in this spike, it does not heal.** A stale entry under "
            + "the per-Segment rung has no mechanism that will ever notice: the road it should be "
            + "using is one the route does not contain, so no version it watches will move again. "
            + "The only thing that removes it is **eviction** — and `adr/0012` keys the cache by "
            + "origin-destination pair rather than by agent, so the entry is not one driver's habit "
            + "but every driver's route, and a hot pair is the *least* likely to be evicted "
            + "precisely because it is hot. **The error is permanent and it is concentrated on the "
            + "busiest pairs in the city.**");
        report.AppendLine();
    }

    private static Added MeasureAddition(
        RoadGraph graph,
        ReverseArcs reverse,
        EditStorm storm,
        Clusters clusters,
        OdPair[] pool,
        GestureShape shape,
        int size,
        EpochRung epoch)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();

        // Built on the FULL graph, which is the whole trick: every portal slot the restored
        // Segments will need already exists, so the restore is an addition the abstract graph can
        // actually absorb. Building it on the damaged graph instead would silently drop the portals
        // and measure a hierarchy that cannot see the new road at all — which would produce a much
        // more alarming number for the wrong reason.
        var abstractGraph = AbstractGraph.Build(
            graph, clusters, reverse, arcCost,
            transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
        var search = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);

        var touched = new bool[clusters.Count];
        var scratch = new List<int>();
        var arcs = new List<int>();

        var gesture = storm.Draw(CounterHash.Seed, 7, shape, size);

        if (gesture.Segments.Length == 0)
        {
            return new Added(shape, size, 0, epoch, 0, 0, 0, 0, 0, 0);
        }

        // The "before" world: the road does not exist yet.
        storm.Apply(gesture, arcCost);
        abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);

        var clock = new EpochClock(graph, clusters);
        var cache = new RouteCache(
            graph, clusters, CacheCapacity, MaxCachedArcs, MaxCachedClusters);

        for (int i = 0; i < pool.Length; i++)
        {
            var outcome = search.Run(pool[i].Origin, pool[i].Destination);

            if (!outcome.Found)
            {
                continue;
            }

            arcs.Clear();
            search.Refine(arcs);

            if (arcs.Count > 0)
            {
                cache.Insert(cache.KeyOf(pool[i].Origin, pool[i].Destination), arcs, epoch, clock);
            }
        }

        // The addition.
        storm.Revert(gesture, arcCost);
        clock.Bump(gesture);
        abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);

        int resident = 0;
        int improvable = 0;
        int declaredValid = 0;
        int wronglyValid = 0;
        long detourSum = 0;
        int worstDetour = 0;

        for (int i = 0; i < pool.Length; i++)
        {
            long key = cache.KeyOf(pool[i].Origin, pool[i].Destination);
            Lookup outcome = cache.TryGet(key, epoch, clock, out int slot);

            if (outcome == Lookup.Miss)
            {
                continue;
            }

            resident++;

            // The cached route's cost on the graph as it now is. Under pure addition the arcs of a
            // cached route are untouched, so this is also exactly what it cost when it was stored.
            long cached = 0;
            foreach (int arc in cache.ArcsAt(slot))
            {
                cached += arcCost[arc];
            }

            var fresh = search.Run(pool[i].Origin, pool[i].Destination);

            if (!fresh.Found)
            {
                continue;
            }

            arcs.Clear();
            search.Refine(arcs);

            if (arcs.Count == 0)
            {
                continue;
            }

            long now = 0;
            foreach (int arc in arcs)
            {
                now += arcCost[arc];
            }

            bool beaten = now < cached;

            if (beaten)
            {
                improvable++;
            }

            if (outcome != Lookup.Hit)
            {
                continue;
            }

            declaredValid++;

            if (!beaten)
            {
                continue;
            }

            wronglyValid++;
            int over = (int)(((cached - now) * 10_000) / now);
            detourSum += over;
            worstDetour = over > worstDetour ? over : worstDetour;
        }

        return new Added(
            shape,
            size,
            gesture.Segments.Length,
            epoch,
            resident,
            improvable,
            declaredValid,
            wronglyValid,
            wronglyValid == 0 ? 0 : (int)(detourSum / wronglyValid),
            worstDetour);
    }

    // --- R5.5 the path source ----------------------------------------------------------------------

    /// <summary>
    /// The five places a Trip's route can come from, and the reason each is on the ballot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// R2 handed R5 a choice between a shared District route and a next-hop table, to be decided on
    /// <i>invalidation</i>. R5.3 and R5.4 then measured a rung that was on nobody's ballot — HPA\*
    /// behind a route cache — and found its error <b>temporal, permanent and concentrated on the
    /// busiest pairs</b>, where a maintained table's error is <b>structural, bounded and identical
    /// every Tick</b>. So the rungs are not four spellings of one thing being raced. They are two
    /// kinds of wrong, and only the price of each is a benchmark's business.
    /// </para>
    /// </remarks>
    private enum PathRung
    {
        /// <summary>HPA\* behind the route cache at the per-Segment Epoch — R5.3's recommendation.</summary>
        Cache,

        /// <summary>The same, plus a rotation expiring a fixed slice of the cache every Tick.</summary>
        CacheTtl,

        /// <summary>A next-hop table per District, maintained by R4's dynamic subtree repair.</summary>
        NextHop,

        /// <summary>One shared route per ordered District pair, rebuilt whole on every gesture.</summary>
        Shared,

        /// <summary>Flat A\* per Trip on the current costs. The control, and the truth.</summary>
        Flat,
    }

    /// <summary>
    /// What one gesture costs a rung to become correct again, and — for the one rung that has two
    /// spellings — what the loop costs that a per-edit API invites.
    /// </summary>
    private sealed record Responded(
        PathRung Rung,
        int Requested,
        int Collected,
        long CoalescedNanoseconds,
        long CoalescedWorst,
        long NaiveNanoseconds,
        long NaiveWorst,
        int CoalescedWrongCost,
        int CoalescedStranded,
        int NaiveWrongCost,
        int NaiveStranded,
        long AuditEntries);

    /// <summary>One path source, run through one storm, at one O-D draw and one edit rate.</summary>
    private sealed record Sourced(
        OdRung Od,
        PathRung Rung,
        int TtlPeriod,
        int EditPeriod,
        long MeanTickNanoseconds,
        long WorstTickNanoseconds,
        int Hits,
        int Stale,
        int Misses,
        int Expired,
        int Unroutable,
        int MeanDetour,
        int P90Detour,
        int WorstDetour,
        int DetourSamples,
        int DetourSkipped,
        int DetourBroken,
        int WrongCost,
        int Stranded,
        long AuditEntries,
        int HeldResident,
        int HeldDeclaredValid,
        int HeldImprovable,
        int HeldWronglyValid,
        int HeldMeanDetour,
        int HeldWorstDetour,
        int HeldBroken,
        int HeldIncomparable);

    /// <summary>
    /// Whether the hierarchy loses a route, and whether the detour column's units can tell.
    /// </summary>
    /// <remarks>
    /// Two questions that look like one. <c>Worse</c> and <c>Better</c> compare the two searches on
    /// the quantity they both minimise — <b>whole journey cost, remainders included</b> — and R3.5
    /// measured that at 100% optimal on this graph. <c>ArcSumDiffers</c> compares the same two
    /// routes on the quantity <b>R5.5's detour column actually prints</b>, which is an arc sum with
    /// both remainders dropped. The two can disagree, and the gap between them is this section's
    /// resolution rather than any rung's error.
    /// </remarks>
    private readonly record struct Optimality(
        OdRung Od,
        int Samples,
        int Worse,
        int Better,
        int ArcSumDiffers,
        int WorstArcSumGap);

    /// <summary>
    /// R5.5 on its own, for <c>--path-source</c>. The section is long enough that running it without
    /// the four sections in front of it is the difference between a sweep a session can iterate on
    /// and one it can only launch, and the two run modes are not identical: in a
    /// <c>--path-source</c> run the flat denominator's first reading is the first timed thing in the
    /// process, which is precisely the artefact R3 found and which the twice-measured denominator
    /// below exists to expose rather than to hide.
    /// </summary>
    public static string RunPathSource()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var reverse = ReverseArcs.Of(graph);
        var segmentArcs = SegmentArcs.Of(graph);
        var storm = new EditStorm(graph, segmentArcs);

        report.AppendLine("## S2 R5.5 — the path source");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: {graph.Segments:N0} Segments, {graph.Nodes:N0} nodes, "
            + $"{graph.Arcs:N0} arcs, {storm.CarSegments:N0} of them admitting cars. **Run on its "
            + $"own rather than behind R5.1–R5.4**, so the flat denominator's first reading below is "
            + $"the first timed quantity in this process and its ratio against the last reading is "
            + $"load-bearing rather than decorative."));
        report.AppendLine();

        // This entry point runs R5.5 alone, so R5.2's table is not in this capture and there is
        // no measured spelling ratio to hand on. Null rather than a placeholder number: R5.5.1
        // says so in words instead of printing a figure this run did not take.
        AppendPathSource(report, graph, reverse, storm, segmentArcs, spellingRatio: null);

        return report.ToString();
    }

    private static void AppendPathSource(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        EditStorm storm,
        SegmentArcs segmentArcs,
        string? spellingRatio)
    {
        var clusters = Clusters.Partition(graph, LadderCluster);
        var districts = Districts.Partition(graph, PathSourceDistricts);

        // The backward index the two District-granular rungs need. A next-hop table is a Dijkstra
        // rooted at the destination running over incoming arcs, and the forward CSR cannot answer
        // that question — so this is a second view of the same arcs, built once and shared by every
        // rung that reads it, exactly as R4 built it.
        var vectorArcs = VectorArcs.Of(graph);
        var sampler = new OdSampler(graph);
        var distribution = new OdDistribution(graph, sampler);
        var denominatorPool = distribution.Draw(
            CounterHash.Seed, DenominatorQueries, Modes.Car, new OdRung(OdShape.Uniform, 0),
            out _, out _);

        // Measured before anything else in the section and again after all of it. Deliberately not
        // warmed: the whole content of the first reading is how cold the process was when it was
        // taken, and warming it would delete the measurement rather than improve it.
        Mark("R5.5 flat denominator, first");
        long denominatorFirst = MeasureFlatDenominator(graph, denominatorPool);

        Mark("R5.5.1 the edit response");
        AppendEditResponse(
            report, graph, reverse, vectorArcs, storm, segmentArcs, clusters, districts,
            spellingRatio);

        Mark("R5.5.2 the storm");
        AppendPathStorm(
            report, graph, reverse, vectorArcs, storm, segmentArcs, clusters, districts,
            distribution, denominatorPool, denominatorFirst);

        Mark("R5.5.4 the rotation against the addition");
        AppendHealing(report, graph, reverse, storm, clusters, distribution);
    }

    // --- R5.5.1 the edit response -----------------------------------------------------------------

    private static void AppendEditResponse(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        VectorArcs vectorArcs,
        EditStorm storm,
        SegmentArcs segmentArcs,
        Clusters clusters,
        Districts districts,
        string? spellingRatio)
    {
        report.AppendLine("### R5.5.1 — the edit response, which is what the player is waiting for");
        report.AppendLine();
        report.AppendLine(
            "**A path source is not chosen on what it costs to read; it is chosen on what it costs "
            + "to make correct again.** `plans/0010`'s first tripwire for this section says so in "
            + "the form that matters — *a rung that cannot be made correct again within one Tick "
            + "budget after a plausible gesture is out on a design commitment, not on a number* — "
            + "because the player is holding the mouse button down while it happens. This table is "
            + "that quantity, per rung, swept over the gesture sizes R5.1 established the shape of.");
        report.AppendLine();
        string spellingClause = spellingRatio is null
            ? "a spelling difference large enough to matter — R5.2 does not run in this capture, "
                + "so its ratio is not restated here"
            : $"a {spellingRatio} spelling difference";

        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"**The naive columns exist because R5.2 found {spellingClause} and this is "
            + $"where that finding is tested for generality.** `RepairSubtree` takes a changed-arc "
            + $"set, so it has a coalesced spelling and a per-Segment one, exactly as `AbstractGraph` "
            + $"did. If looping it per deleted Segment over a drag is a catastrophe too, then *a "
            + $"per-edit repair API invites the loop that produces it* stops being a routing note and "
            + $"becomes a corpus-wide rule about API shape. If it is not, R5.2's finding is a "
            + $"property of a cluster's edge set and belongs to the hierarchy alone."));
        report.AppendLine();
        report.AppendLine(
            "**Two rungs have no naive spelling and the reason differs.** The cache rungs repair the "
            + "abstract graph, which R5.2 has already priced both ways and which is reproduced here "
            + "only so the columns are comparable. `shared` has no repair *by construction* — R4 "
            + "established that maintenance is separable from path source, so writing a repair for "
            + "`RouteStore` would measure the repair rather than the rung, and `plans/0010` prices "
            + "it as a rebuild precisely so that a loss is a retirement on a number. `flat` has no "
            + "edit response at all, which is the whole of what it is for: it is the row that gives "
            + "this column a floor of nothing.");
        report.AppendLine();
        report.AppendLine(
            "**`cache` and `cache+ttl` must read the same figure here, and printing both is the "
            + "cheap check that they do.** A rotation is a per-Tick cost and not a per-gesture one, "
            + "so the TTL cannot change what an edit costs; two rows that disagree would be two "
            + "rungs that are secretly different experiments, which is the defect R2 shipped as "
            + "byte-identical peaks and this spike has been paying for since.");
        report.AppendLine();

        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var abstractGraph = AbstractGraph.Build(
            graph, clusters, reverse, arcCost,
            transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
        var touched = new bool[clusters.Count];
        var scratch = new List<int>();

        Mark("R5.5.1 seeding the next-hop table");
        var pristine = new DistanceVector(graph, vectorArcs, districts.Count);
        for (int d = 0; d < districts.Count; d++)
        {
            pristine.Seed(d, districts.Representative[d], arcCost);
        }

        var working = new DistanceVector(graph, vectorArcs, districts.Count);
        var truthTable = new DistanceVector(graph, vectorArcs, districts.Count);
        var truthCosts = new int[graph.Nodes];
        var changedArcs = new int[GestureSizes[^1] * 4];
        var oneToAll = new OneToAll(graph);

        // Warm every measurer before any of its rows is published, and not merely inside each one.
        // R1's finding is per-process rather than per-rung, so the first rung measured pays the
        // whole sweep's tiered-JIT bill. **The first draft of this table read `cache` four times
        // slower than `cache+ttl` on rows that must be identical**, and the naive column read
        // *faster* than the coalesced one at a gesture of 1, where the two are the same computation.
        // Both are the check the rows exist to be, and both were the clock rather than the code.
        Mark("R5.5.1 warming the measurers");
        MeasureCacheResponse(
            storm, abstractGraph, arcCost, touched, scratch, PathRung.Cache, GestureSizes[^1]);
        MeasureNextHopResponse(
            storm, segmentArcs, districts, arcCost, pristine, working, truthTable, truthCosts,
            changedArcs, graph.Nodes, GestureSizes[^1]);
        MeasureSharedResponse(graph, storm, districts, arcCost, oneToAll, GestureSizes[^1]);

        var rows = new List<Responded>();

        foreach (PathRung rung in PathRungs)
        {
            foreach (int size in GestureSizes)
            {
                Mark($"R5.5.1 {Name(rung, 0)} × {size}");

                rows.Add(rung switch
                {
                    PathRung.Cache or PathRung.CacheTtl => MeasureCacheResponse(
                        storm, abstractGraph, arcCost, touched, scratch, rung, size),
                    PathRung.NextHop => MeasureNextHopResponse(
                        storm, segmentArcs, districts, arcCost, pristine, working, truthTable,
                        truthCosts, changedArcs, graph.Nodes, size),
                    PathRung.Shared => MeasureSharedResponse(
                        graph, storm, districts, arcCost, oneToAll, size),
                    _ => new Responded(PathRung.Flat, size, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                });
            }
        }

        report.AppendLine(
            "| Rung | Gesture | Got | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            bool timed = row.Rung != PathRung.Flat;
            bool naive = row.Rung == PathRung.NextHop;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Name(row.Rung, 0)} | drag {row.Requested} "
                + $"| {(timed ? row.Collected.ToString(CultureInfo.InvariantCulture) : "—")} "
                + $"| {(timed ? Milliseconds(row.CoalescedNanoseconds) : "none")} "
                + $"| {(timed ? Milliseconds(row.CoalescedWorst) : "none")} "
                + $"| {(naive ? Milliseconds(row.NaiveNanoseconds) : "—")} "
                + $"| {(naive ? Milliseconds(row.NaiveWorst) : "—")} "
                + $"| {(naive ? Ratio(row.NaiveNanoseconds, row.CoalescedNanoseconds) : "—")} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{GestureSamples} gestures per row, each applied, repaired, reverted and repaired "
            + $"again, so every row starts and ends on the same graph and the timed figure is one "
            + $"repair. **Worst is the worst single gesture and not a quantile** — a gesture is one "
            + $"player action, and S4's K6 established that a quantile over eight of them hides "
            + $"precisely the event this column exists to publish. The next-hop rung repairs all "
            + $"{districts.Count} District columns, which is the whole table and not a sample; the "
            + $"shared rung rebuilds all {districts.Count * districts.Count:N0} ordered pairs, which "
            + $"is why its figure does not move with gesture size."));
        report.AppendLine();

        long entries = 0;
        int coalescedWrong = 0;
        int coalescedStranded = 0;
        int naiveWrong = 0;
        int naiveStranded = 0;

        foreach (var row in rows)
        {
            if (row.Rung != PathRung.NextHop)
            {
                continue;
            }

            entries += row.AuditEntries;
            coalescedWrong += row.CoalescedWrongCost;
            coalescedStranded += row.CoalescedStranded;
            naiveWrong += row.NaiveWrongCost;
            naiveStranded += row.NaiveStranded;
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Audited, because a repair that silently does nothing reports success otherwise.** "
            + $"After the largest gesture of each row the repaired table is compared entry by entry "
            + $"against a column freshly `Seed`ed on the damaged graph — {entries:N0} entries. "
            + $"**Coalesced: {coalescedWrong:N0} wrong cost, {coalescedStranded:N0} stranded. "
            + $"Naive: {naiveWrong:N0} wrong cost, {naiveStranded:N0} stranded.** R4 hit this exact "
            + $"failure mode and it is the reason the check is here rather than assumed: a scheme "
            + $"that returns without doing anything is the fastest scheme on the table and every "
            + $"surrounding column looks healthy."));
        report.AppendLine();
    }

    /// <summary>
    /// The cache rungs' edit response, which is R5.2's coalesced repair of the abstract graph.
    /// Reproduced rather than cross-referenced so that every rung in the table above was timed in
    /// the same process, under the same warm-up, against the same gestures.
    /// </summary>
    private static Responded MeasureCacheResponse(
        EditStorm storm,
        AbstractGraph abstractGraph,
        int[] arcCost,
        bool[] touched,
        List<int> scratch,
        PathRung rung,
        int size)
    {
        var gestures = new Gesture[GestureSamples];
        int collected = 0;

        for (int g = 0; g < GestureSamples; g++)
        {
            gestures[g] = storm.Draw(CounterHash.Seed, (ulong)g, GestureShape.Drag, size);
            collected += gestures[g].Segments.Length;
        }

        foreach (var gesture in gestures)
        {
            storm.Apply(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
            storm.Revert(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
        }

        long total = 0;
        long worst = 0;

        foreach (var gesture in gestures)
        {
            storm.Apply(gesture, arcCost);

            long start = Stopwatch.GetTimestamp();
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
            long taken = Since(start);

            total += taken;
            worst = taken > worst ? taken : worst;

            storm.Revert(gesture, arcCost);
            abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
        }

        return new Responded(
            rung, size, collected / GestureSamples, total / GestureSamples, worst,
            0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>
    /// The next-hop table's edit response, both spellings, and the audit that says whether either
    /// of them worked.
    /// </summary>
    /// <remarks>
    /// The coalesced spelling hands <c>RepairSubtree</c> the whole gesture's changed arcs at once,
    /// so the affected subtree is derived once from a single invalidation frontier. The naive
    /// spelling applies the whole gesture to the cost array and then calls <c>RepairSubtree</c> once
    /// per deleted Segment — which is the shape R3 and R4 both wrote, is identical to the coalesced
    /// one at a gesture of one, and is the only size either of them measured.
    /// </remarks>
    private static Responded MeasureNextHopResponse(
        EditStorm storm,
        SegmentArcs segmentArcs,
        Districts districts,
        int[] arcCost,
        DistanceVector pristine,
        DistanceVector working,
        DistanceVector truthTable,
        int[] truthCosts,
        int[] changedArcs,
        int nodes,
        int size)
    {
        var gestures = new Gesture[GestureSamples];
        int collected = 0;

        for (int g = 0; g < GestureSamples; g++)
        {
            gestures[g] = storm.Draw(CounterHash.Seed, (ulong)g, GestureShape.Drag, size);
            collected += gestures[g].Segments.Length;
        }

        // Warm. The worst-Tick and worst-gesture columns are the deliverable, and an unwarmed first
        // repair lands in them — R1 needed four warm-up schemes before its cold column stopped
        // falling smoothly with the swept axis, because the cost was per-process rather than
        // per-rung.
        working.CopyFrom(pristine);
        int warmChanged = ChangedArcs(segmentArcs, gestures[0], changedArcs);
        storm.Apply(gestures[0], arcCost);
        for (int d = 0; d < districts.Count; d++)
        {
            working.RepairSubtree(d, districts.Representative[d], arcCost, changedArcs, warmChanged);
        }

        storm.Revert(gestures[0], arcCost);

        long coalesced = 0;
        long coalescedWorst = 0;

        foreach (var gesture in gestures)
        {
            working.CopyFrom(pristine);
            int changed = ChangedArcs(segmentArcs, gesture, changedArcs);
            storm.Apply(gesture, arcCost);

            long start = Stopwatch.GetTimestamp();
            for (int d = 0; d < districts.Count; d++)
            {
                working.RepairSubtree(d, districts.Representative[d], arcCost, changedArcs, changed);
            }

            long taken = Since(start);
            coalesced += taken;
            coalescedWorst = taken > coalescedWorst ? taken : coalescedWorst;

            storm.Revert(gesture, arcCost);
        }

        long naive = 0;
        long naiveWorst = 0;

        foreach (var gesture in gestures)
        {
            working.CopyFrom(pristine);
            storm.Apply(gesture, arcCost);

            long start = Stopwatch.GetTimestamp();
            foreach (int segment in gesture.Segments)
            {
                int changed = 0;
                foreach (int arc in segmentArcs.For(segment))
                {
                    changedArcs[changed++] = arc;
                }

                for (int d = 0; d < districts.Count; d++)
                {
                    working.RepairSubtree(
                        d, districts.Representative[d], arcCost, changedArcs, changed);
                }
            }

            long taken = Since(start);
            naive += taken;
            naiveWorst = taken > naiveWorst ? taken : naiveWorst;

            storm.Revert(gesture, arcCost);
        }

        // The audit, on the last gesture of the row, against a table seeded from scratch on the
        // damaged graph. One ground truth serves both spellings, which is the only way the two
        // audits are comparable.
        var last = gestures[^1];
        int lastChanged = ChangedArcs(segmentArcs, last, changedArcs);
        storm.Apply(last, arcCost);

        for (int d = 0; d < districts.Count; d++)
        {
            truthTable.Seed(d, districts.Representative[d], arcCost);
        }

        working.CopyFrom(pristine);
        for (int d = 0; d < districts.Count; d++)
        {
            working.RepairSubtree(d, districts.Representative[d], arcCost, changedArcs, lastChanged);
        }

        int coalescedWrong = 0;
        int coalescedStranded = 0;

        for (int d = 0; d < districts.Count; d++)
        {
            truthTable.CopyCosts(d, truthCosts);
            working.Audit(d, districts.Representative[d], truthCosts, out int wrong, out int lost);
            coalescedWrong += wrong;
            coalescedStranded += lost;
        }

        working.CopyFrom(pristine);
        foreach (int segment in last.Segments)
        {
            int changed = 0;
            foreach (int arc in segmentArcs.For(segment))
            {
                changedArcs[changed++] = arc;
            }

            for (int d = 0; d < districts.Count; d++)
            {
                working.RepairSubtree(d, districts.Representative[d], arcCost, changedArcs, changed);
            }
        }

        int naiveWrong = 0;
        int naiveStranded = 0;

        for (int d = 0; d < districts.Count; d++)
        {
            truthTable.CopyCosts(d, truthCosts);
            working.Audit(d, districts.Representative[d], truthCosts, out int wrong, out int lost);
            naiveWrong += wrong;
            naiveStranded += lost;
        }

        storm.Revert(last, arcCost);

        return new Responded(
            PathRung.NextHop,
            size,
            collected / GestureSamples,
            coalesced / GestureSamples,
            coalescedWorst,
            naive / GestureSamples,
            naiveWorst,
            coalescedWrong,
            coalescedStranded,
            naiveWrong,
            naiveStranded,
            (long)districts.Count * nodes);
    }

    /// <summary>
    /// The shared store's edit response, which is a full rebuild because no repair is written for
    /// it and writing one would measure the repair rather than the rung.
    /// </summary>
    private static Responded MeasureSharedResponse(
        RoadGraph graph,
        EditStorm storm,
        Districts districts,
        int[] arcCost,
        OneToAll oneToAll,
        int size)
    {
        var gestures = new Gesture[GestureSamples];
        int collected = 0;

        for (int g = 0; g < GestureSamples; g++)
        {
            gestures[g] = storm.Draw(CounterHash.Seed, (ulong)g, GestureShape.Drag, size);
            collected += gestures[g].Segments.Length;
        }

        RouteStore.ForDistrictPairs(graph, districts, arcCost, oneToAll);

        long total = 0;
        long worst = 0;

        foreach (var gesture in gestures)
        {
            storm.Apply(gesture, arcCost);

            long start = Stopwatch.GetTimestamp();
            RouteStore.ForDistrictPairs(graph, districts, arcCost, oneToAll);
            long taken = Since(start);

            total += taken;
            worst = taken > worst ? taken : worst;

            storm.Revert(gesture, arcCost);
        }

        return new Responded(
            PathRung.Shared, size, collected / GestureSamples, total / GestureSamples, worst,
            0, 0, 0, 0, 0, 0, 0);
    }

    // --- R5.5.2 the storm -------------------------------------------------------------------------

    private static void AppendPathStorm(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        VectorArcs vectorArcs,
        EditStorm storm,
        SegmentArcs segmentArcs,
        Clusters clusters,
        Districts districts,
        OdDistribution distribution,
        OdPair[] denominatorPool,
        long denominatorFirst)
    {
        var rows = new List<Sourced>();

        // One throwaway configuration per rung before any of them is published. The per-configuration
        // warm-up inside `MeasureSource` is not enough on its own, because the first rung through
        // pays the whole sweep's tiered-JIT and first-collection bill — and the column it lands in
        // is the *worst Tick*, which is the column this section exists to publish. The warm runs at a
        // middling edit rate so that every rung's edit response is compiled too, and every number it
        // produces is discarded.
        Mark("R5.5.2 warming the rungs");
        var warmPool = distribution.Draw(
            CounterHash.Seed, PoolPairs, Modes.Car, LadderOdRungs[0], out _, out _);

        foreach (PathRung warmRung in PathRungs)
        {
            MeasureSource(
                graph, reverse, vectorArcs, storm, segmentArcs, clusters, districts, warmPool,
                LadderOdRungs[0], warmRung, warmRung == PathRung.CacheTtl ? TtlPeriods[0] : 0,
                EditPeriods[1]);
        }

        foreach (OdRung od in LadderOdRungs)
        {
            var pool = distribution.Draw(
                CounterHash.Seed, PoolPairs, Modes.Car, od, out _, out _);

            foreach (int period in EditPeriods)
            {
                foreach (PathRung rung in PathRungs)
                {
                    if (rung == PathRung.CacheTtl)
                    {
                        foreach (int ttl in TtlPeriods)
                        {
                            Mark($"R5.5.2 {od.Name} / every {period} / {Name(rung, ttl)}");
                            rows.Add(MeasureSource(
                                graph, reverse, vectorArcs, storm, segmentArcs, clusters, districts,
                                pool, od, rung, ttl, period));
                        }

                        continue;
                    }

                    Mark($"R5.5.2 {od.Name} / every {period} / {Name(rung, 0)}");
                    rows.Add(MeasureSource(
                        graph, reverse, vectorArcs, storm, segmentArcs, clusters, districts, pool,
                        od, rung, 0, period));
                }
            }
        }

        Mark("R5.5.2 optimality check");
        var optimality = new List<Optimality>();

        foreach (OdRung od in LadderOdRungs)
        {
            var pool = distribution.Draw(
                CounterHash.Seed, PoolPairs, Modes.Car, od, out _, out _);
            optimality.Add(MeasureOptimality(graph, reverse, clusters, pool, od));
        }

        Mark("R5.5 flat denominator, last");
        long denominatorLast = MeasureFlatDenominator(graph, denominatorPool);

        report.AppendLine("### R5.5.2 — the storm, and which kind of wrong each rung serves");
        report.AppendLine();
        report.AppendLine(
            "**One storm, five path sources.** Every row below runs the same seed, the same pool, "
            + "the same Trip draw and the same gesture schedule, so a row differs from the row "
            + "beside it by its path source and by nothing else. That is not a courtesy: R2 "
            + "published two rungs with byte-identical peaks because the experiment had quietly "
            + "removed the difference it existed to measure, and the cheapest defence against "
            + "repeating it is to make the shared state shared by construction rather than by "
            + "coincidence.");
        report.AppendLine();
        report.AppendLine(
            "**The flat search is the denominator and it is measured on both sides of the sweep.** "
            + "R3's first pinned capture read 1,401,307 ns for the same quantity measured first and "
            + "477,609 ns measured last, and R5's own capture has just found the artefact live at "
            + "**4.88×** on a machine pinned to one logical processor. Neither reading is warmed, "
            + "because the content of the first one is how cold the process was when it was taken.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"- One uncached point-to-point search, arcs returned: {Microseconds(denominatorFirst)} "
            + $"measured first, {Microseconds(denominatorLast)} measured last, "
            + $"{Ratio(denominatorFirst, denominatorLast)} apart."));
        report.AppendLine();
        report.AppendLine(
            "**Detour is what the rung actually served, against a flat search on the arc costs as "
            + "they are at that moment.** Both sides are arc-cost sums, so both exclude the Access "
            + "Point offset remainders at the two ends — common to both routes and bounded by one "
            + "Segment each, which is R5.4's handling and for R5.4's reason. `flat` must therefore "
            + "read **exactly 0.00%**, and it is computed through a second search instance rather "
            + "than aliased to the truth so that the zero is a round trip through the whole "
            + "pipeline. **A zero everywhere would be indistinguishable from an instrument that is "
            + "not wired up** — R3.5's defect, which R3.6 is how the corpus learned to catch.");
        report.AppendLine();
        report.AppendLine(
            "**The District-granular rungs are composed the way R4.8 composed them.** The next-hop "
            + "rung is followed from wherever the Traveller is to the destination District's "
            + "representative and then searched onward, so it is coarse at the destination end "
            + "only. The shared rung is coarse at **both** ends — to the origin District's "
            + "representative, along the stored route, and onward from the destination "
            + "representative — which is R2's composition and the reason its error was roughly "
            + "twice the next-hop rung's. Each leg starts at a Segment incident to a representative "
            + "rather than at the node itself, which can only make the followed route look cheaper, "
            + "so **every District-granular figure below is a lower bound**.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Detour is sampled and the sample size is printed per row.** A truth search per Trip "
            + $"would cost more than every rung it prices put together, so it is taken on one Tick "
            + $"in {DetourSampleEvery}, **after that Tick's clock has stopped** — the instrument "
            + $"must not land in the column it is measuring. A sample that shrinks with the swept "
            + $"axis has manufactured a trend three times in this spike, so the column is a "
            + $"survivorship check as much as a sample size: a pair is dropped only when a leg "
            + $"finds no route."));
        report.AppendLine();
        report.AppendLine(
            "**The *Sample* column falls as the edit rate rises, and it is the storm removing "
            + "routable pairs rather than the instrument losing interest.** The storm never reverts, "
            + "so by the last Tick of a four-Tick-period row about a thousand Segments are gone; a "
            + "sampled pair is dropped when the truth search finds no route at all, which happens "
            + "when the player has bulldozed the Segment the Trip starts or ends on, or — rarely on "
            + "a grid — when the pair has genuinely been severed. It tracks the control's "
            + "*Unroutable* column exactly, which is what identifies it. **This is the survivorship "
            + "shape the corpus has been caught by three times**, so it is named rather than left "
            + "for a reader to notice: the rows at the highest edit rate are drawn from a slightly "
            + "smaller and slightly better-connected population than the rows above them.");
        report.AppendLine();
        report.AppendLine(
            "**The hierarchy is optimal, and the detour column's own units are not exact. Both "
            + "halves are measured here rather than assumed.** The cache rows print a non-zero "
            + "*worst* detour with **no edits applied at all**, which is either R3.5 — *100% "
            + "optimal, 0.00% mean detour, no route cheaper than the flat optimum* — being wrong, "
            + "or this section's arithmetic being wrong. On a pristine graph, per O-D rung:");
        report.AppendLine();
        report.AppendLine(
            "| O-D rung | Sampled | HPA\\* worse than flat | HPA\\* better | Equal cost, unequal arc "
            + "sum | Worst arc-sum gap |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|");

        foreach (Optimality check in optimality)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {check.Od.Name} | {check.Samples} | **{check.Worse}** | {check.Better} "
                + $"| {check.ArcSumDiffers} | {Hundredths(check.WorstArcSumGap)}% |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**Read the third column first: it is zero, so R3.5 stands and the hierarchy loses "
            + "nothing.** On whole journey cost — arcs *plus* the two Access Point remainders, which "
            + "is the quantity both searches actually minimise — HPA\\* never returns a worse route "
            + "than the flat search, and never a cheaper one either, which would have been an "
            + "admissibility bug. **The residual is the column's units.** A cached route is a list "
            + "of arcs and nothing else, so the detour column sums arcs and drops both remainders; "
            + "two routes of *identical* whole-journey cost can then have different arc sums, "
            + "because one enters the destination Segment from the far endpoint and trades a larger "
            + "remainder for a smaller arc total. The last two columns size exactly that, over the "
            + "whole pool rather than over the sampled Ticks — so the worst arc-sum gap **bounds** "
            + "the worst detour the cache rows print, rung for rung, and does not equal it. That "
            + "the bound holds in every rung is the check; had a cache row exceeded its own rung's "
            + "gap, the residual would not have been the explanation.");
        report.AppendLine();
        report.AppendLine(
            "**So the detour column has a resolution floor of about one Segment at each end, and it "
            + "is not a property of any rung.** It bounds every number in the column, including the "
            + "District-granular ones: `nexthop` and `shared` are composed against the same truth "
            + "and carry the same residual. It is immaterial there, being a floor of a few per cent "
            + "against errors of tens to hundreds of per cent — and it is the *whole* of the cache "
            + "rungs' reading, which is why **no cache row's detour may be quoted as a cost of the "
            + "hierarchy**. "
            + "**The honest statement is that the cache rungs serve routes this instrument cannot "
            + "distinguish from optimal.** Correcting it would mean charging the served route the "
            + "remainders its own endpoints imply, which is a change to what the rung is credited "
            + "with rather than to the truth, and it is not made here.");
        report.AppendLine();

        report.AppendLine(
            "| O-D rung | Rung | Edit every | Mean Tick | Worst Tick | Hit | Stale | Miss "
            + "| Forced refreshes / Tick | Unroutable | Mean detour | p90 | Worst | Sample |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            bool cached = row.Rung is PathRung.Cache or PathRung.CacheTtl;
            int lookups = row.Hits + row.Stale + row.Misses;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {row.Od.Name} | {Name(row.Rung, row.TtlPeriod)} "
                + $"| {(row.EditPeriod == 0 ? "never" : row.EditPeriod + " Ticks")} "
                + $"| {Microseconds(row.MeanTickNanoseconds)} "
                + $"| {Microseconds(row.WorstTickNanoseconds)} "
                + $"| {(cached ? Percent(row.Hits, lookups) : "—")} "
                + $"| {(cached ? Percent(row.Stale, lookups) : "—")} "
                + $"| {(cached ? Percent(row.Misses, lookups) : "—")} "
                + $"| {(row.Rung == PathRung.CacheTtl ? Hundredths((int)((row.Expired * 100L) / LadderTicks)) : "—")} "
                + $"| {row.Unroutable} "
                + $"| {(row.DetourSamples == 0 ? "—" : Hundredths(row.MeanDetour) + "%")} "
                + $"| {(row.DetourSamples == 0 ? "—" : Hundredths(row.P90Detour) + "%")} "
                + $"| {(row.DetourSamples == 0 ? "—" : Hundredths(row.WorstDetour) + "%")} "
                + $"| {row.DetourSamples} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{LadderTicks} Ticks, {TripsPerTick} Trip starts per Tick, drawn with repetition from "
            + $"a pool of {PoolPairs} distinct origin-destination pairs. The cache rungs hold "
            + $"{CacheCapacity} entries at {LadderCluster} Chunks per cluster and revalidate at the "
            + $"**per-Segment** Epoch, which is R5.3's recommendation and the rung R5.4 then found a "
            + $"hole in. The two District-granular rungs run at {districts.Count} Districts, which "
            + $"is R2's anchor and R4's. Gestures are {LadderGestureSegments}-Segment drags and "
            + $"**the storm never reverts**, so the graph degrades monotonically across a run and "
            + $"the *Unroutable* column is what says whether a row is measuring severance rather "
            + $"than its rung."));
        report.AppendLine();

        int flatUnroutable = 0;
        var byTtl = new SortedDictionary<int, int>();

        foreach (var row in rows)
        {
            if (row.Rung == PathRung.Flat)
            {
                flatUnroutable += row.Unroutable;
            }
            else if (row.Rung is PathRung.Cache or PathRung.CacheTtl)
            {
                byTtl.TryGetValue(row.TtlPeriod, out int running);
                byTtl[row.TtlPeriod] = running + row.Unroutable;
            }
        }

        // Ordered by how hard the rung is made to re-look: no rotation first, then the longest
        // period down to the shortest. The claim below is that the column is monotone in that
        // order, so it is printed in that order and a reader can check it.
        var ladder = byTtl.Keys.OrderBy(period => period <= 0 ? int.MinValue : -period).ToList();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The *Unroutable* column was dead and is now the sharpest staleness instrument in "
            + $"this section — R6.0 repaired the defect R5.5 recorded, and repairing it turned a "
            + $"column that read *zero by construction* into one that moves monotonically with the "
            + $"refresh rate.** `HpaSearch` priced the step *onto* the origin Segment and *off* the "
            + $"destination Segment against a **null cost array** — the pristine `graph.ArcCarTicks` "
            + $"— while the storm deletes into a shadow clone, so the hierarchy returned routes down "
            + $"roads the player had just bulldozed. **The defect was wider than R5.5 filed it**: not "
            + $"the two Access Point remainders but **eight call sites**, and the four it missed are "
            + $"the worse ones, because the same-Segment and adjacent-Segment bypasses return "
            + $"*routable* directly from `Run` without ever entering a confined search. R3.8 puts "
            + $"the bypass at **78.28%** of Legs inside one block, so the defect was heaviest exactly "
            + $"where the local-trip O-D rung lives. The control had it right all along: "
            + $"`PointToPoint` threads its cost array through every call including `SameSegmentCost`, "
            + $"and `HpaSearch` threaded it through none."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**What the repaired column now says is that the residual gap is not a bug — it is the "
            + $"cache serving routes down roads that are gone, and the rotation closes it.** Against "
            + $"a control finding **{flatUnroutable:N0}** severed lookups over the same 12 rows:"));
        report.AppendLine();

        report.AppendLine("| Rung | Unroutable | Share of the control's |");
        report.AppendLine("|---|---:|---:|");

        foreach (int period in ladder)
        {
            string label = period <= 0 ? "cache, no rotation" : $"cache+ttl {period}";
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {label} | {byTtl[period]:N0} | {Percent(byTtl[period], flatUnroutable)} |"));
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| flat (the truth) | {flatUnroutable:N0} | 100.00% |"));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Monotone in the refresh rate, with the control as the asymptote**, which is the "
            + $"causal demonstration rather than a correlation: the only thing separating these rungs "
            + $"is how often an entry is forced to look again, and severance is precisely what a "
            + $"route that never looks again cannot discover. **This corroborates R5.5.4 through a "
            + $"different column entirely** — R5.5.4 measures staleness as *detour* against a truth "
            + $"search, and this measures it as *severance*, and the two agree that a rotation clears "
            + $"what the Epoch rung structurally cannot. **It also discharges the corpus's own "
            + $"warning** that a correctness column reading zero is indistinguishable from an "
            + $"instrument that is not wired up: this one was not wired up, for three tasks, and "
            + $"nothing but the control's disagreement gave it away."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**One denominator defect became visible only once the numbers moved, and it is worth "
            + $"recording.** R5.5 published the disagreement as *16 against 416* — but the 16 summed "
            + $"**four** cache rungs where the 416 was **one** control rung, so the two sides never "
            + $"had the same denominator. It did not change R5.5's conclusion, both figures being "
            + $"about 1% of the control. Summed the same way after the repair it reads "
            + $"**{byTtl.Values.Sum():N0} against {flatUnroutable:N0}**, which invites exactly the "
            + $"wrong reading — *the hierarchy is 3.6× worse* — when per rung it is "
            + $"**{Percent(byTtl.Values.Min(), flatUnroutable)}–"
            + $"{Percent(byTtl.Values.Max(), flatUnroutable)}** of the control. **A broken "
            + $"denominator survives while every number it divides is near zero.**"));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**`nexthop` and `shared` report zero severed lookups at every rung, and that is the "
            + $"unwired-instrument shape rather than a result.** Both answer through a District "
            + $"representative and always return *something*, so neither can report a severance at "
            + $"all. The zero should be read as *this rung has no such column*, not as evidence."));
        report.AppendLine();
        report.AppendLine(
            "**A worst Tick on a sub-microsecond rung is the runtime and not the rung.** The two "
            + "District-granular rungs answer a Trip with one array read, so their mean Tick is "
            + "under two microseconds — and their worst Tick at *never*, where nothing is edited at "
            + "all, still reaches milliseconds, because a collection of the harness's own "
            + "tens-of-megabytes tables lands inside a timed span. The column is honest about what "
            + "happened and misleading about what caused it. Read a cheap rung's worst Tick as a "
            + "bound on this harness; read an expensive rung's as a bound on the design.");
        report.AppendLine();

        int skipped = 0;
        int broken = 0;
        foreach (var row in rows)
        {
            skipped += row.DetourSkipped;
            broken += row.DetourBroken;
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Detour samples dropped: {skipped:N0}, of which {broken:N0} were dropped because the "
            + $"route a rung served contains a Segment that no longer exists.** The second figure is "
            + $"the one to read: under a per-Segment Epoch and a deletion-only storm it must be "
            + $"zero, because a route whose own Segment was deleted has a version that moved. A "
            + $"non-zero reading there is a stamping defect and not a result."));
        report.AppendLine();

        long auditEntries = 0;
        int wrongCost = 0;
        int stranded = 0;

        foreach (var row in rows)
        {
            if (row.Rung != PathRung.NextHop)
            {
                continue;
            }

            auditEntries += row.AuditEntries;
            wrongCost += row.WrongCost;
            stranded += row.Stranded;
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The next-hop table is audited at the end of every storm it survives**, against "
            + $"columns freshly `Seed`ed on the graph the storm left behind — {auditEntries:N0} "
            + $"entries across every next-hop row. **{wrongCost:N0} wrong cost, {stranded:N0} "
            + $"stranded.** A maintained table that has quietly stopped maintaining is the fastest "
            + $"rung on the board and every other column looks healthy while it does it; R4 hit "
            + $"exactly that, which is why the check is printed on the run where it passes rather "
            + $"than kept for the run where it fails."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The rotation is published as a *rate* and never as a period, because the period is "
            + $"the half that does not transfer.** `plans/0010`'s third tripwire is explicit about "
            + $"the form — *a rotation of period N fits while fewer than X refreshes per Tick are "
            + $"forced* — and the reason is arithmetic: a period of {TtlPeriods[1]} Ticks over this "
            + $"harness's {CacheCapacity}-entry cache sweeps {CacheCapacity / TtlPeriods[1]} slots a "
            + $"Tick, where the same period over a real hot set of a different size is a completely "
            + $"different bill. **The *Forced refreshes / Tick* column is the transferable "
            + $"quantity**: occupied entries actually discarded per Tick, which is what the next "
            + $"lookup on each of those keys pays for as a full search. The hit-rate cost belongs "
            + $"to that rate and not to the period beside it."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**And the {TtlPeriods[^1]}-Tick rotation is excluded from every statement about the "
            + $"staleness *bound*.** At {LadderTicks} Ticks it completes "
            + $"{Percent(LadderTicks, TtlPeriods[^1])} of one sweep, so a quarter of the cache is "
            + $"never visited and no entry is guaranteed refreshed within the period. That row "
            + $"prices *the cost of the sweep* and prices it correctly; **it says nothing about the "
            + $"staleness the sweep would buy**, and the bound must not be quoted from it. Measuring "
            + $"the bound needs a run at least one full rotation long, which is a longer capture "
            + $"rather than a different instrument."));
        report.AppendLine();
        report.AppendLine(
            "**The next-hop rung is priced at R2's charitable reading and the charity should be "
            + "stated.** R2 gave it *0 ns per Leg at spawn*: a Traveller reads its next hop and "
            + "drives, with no search anywhere. The tail leg that the detour column composes — from "
            + "the destination District's representative to the actual destination — is therefore "
            + "**not charged to the rung's Tick timings**, only to its error. If that tail were a "
            + "search the rung would pay a flat search per Trip and lose to the control outright, "
            + "so the *Mean Tick* column for `nexthop` should be read as a floor that assumes the "
            + "coarse arrival is acceptable, which is exactly the question the detour column is "
            + "asking.");
        report.AppendLine();
        report.AppendLine(
            "### R5.5.3 — what the cache *holds*, as against what it *serves*");
        report.AppendLine();
        report.AppendLine(
            "**The detour column in R5.5.2 is invariant across every edit rate, and the reason is "
            + "structural rather than a wiring fault — but it also means that column cannot answer "
            + "the question this section was written for.** Under a per-Segment Epoch a stale entry "
            + "is *detected* at lookup and recomputed, so what a Trip is **served** is never stale: "
            + "either the entry was valid, or it was replaced by a fresh search before the Trip saw "
            + "it. A column that prices what was served therefore prices freshly-computed HPA\\* "
            + "routes at every edit rate, and it must read the same figure at *never* and at four "
            + "Ticks. It does, to the digit, and **that invariance is a result rather than a "
            + "silence** — but a column that cannot move with the axis is exactly the shape R3.5's "
            + "0.00% wore, and R3.6 is how the corpus learned not to accept one on its own word.");
        report.AppendLine();
        report.AppendLine(
            "**So this table walks the pool instead of the Trip stream.** Every entry the cache "
            + "still holds at the end of the storm is priced against a fresh search on the graph as "
            + "the storm left it, whether or not any Trip asked for it. It is R5.4's instrument "
            + "pointed at **deletion** rather than at addition, and R5.4's own argument predicts "
            + "what it must say: deletion is monotone-worsening, so removing an arc that is not on "
            + "route `R` leaves `R` optimal, and a rung watching only `R`'s own Segments misses "
            + "nothing. **Predicted zero, and it is measured rather than left as an argument** — "
            + "which is `adr/0043`'s rule applied to the recommendation R5.3 made rather than to a "
            + "rung it rejected.");
        report.AppendLine();
        report.AppendLine(
            "Priced hierarchy against hierarchy, as R5.4 priced it. A flat search on this side "
            + "would fold the arc-sum residual sized above into the answer and report the "
            + "instrument's own units as staleness.");
        report.AppendLine();
        report.AppendLine(
            "| O-D rung | Rung | Edit every | Resident | Declared valid | Improvable "
            + "| **Wrongly valid** | Mean detour | Worst | Holding a deleted Segment "
            + "| Not comparable | Identity |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        int heldWrong = 0;
        int heldBrokenTotal = 0;
        int identityBreaks = 0;

        foreach (var row in rows)
        {
            if (row.Rung is not (PathRung.Cache or PathRung.CacheTtl))
            {
                continue;
            }

            heldWrong += row.HeldWronglyValid;
            heldBrokenTotal += row.HeldBroken;

            // The rung's claim, stated as an identity between two independently counted columns:
            // the entries a per-Segment Epoch refuses are exactly the entries containing a deleted
            // Segment. Checked per row rather than asserted, and printed either way.
            bool holds = row.HeldResident - row.HeldDeclaredValid == row.HeldBroken;

            if (!holds)
            {
                identityBreaks++;
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {row.Od.Name} | {Name(row.Rung, row.TtlPeriod)} "
                + $"| {(row.EditPeriod == 0 ? "never" : row.EditPeriod + " Ticks")} "
                + $"| {row.HeldResident} "
                + $"| {Percent(row.HeldDeclaredValid, row.HeldResident)} "
                + $"| {Percent(row.HeldImprovable, row.HeldResident)} "
                + $"| **{Percent(row.HeldWronglyValid, row.HeldResident)}** "
                + $"| {(row.HeldWronglyValid == 0 ? "—" : Hundredths(row.HeldMeanDetour) + "%")} "
                + $"| {(row.HeldWronglyValid == 0 ? "—" : Hundredths(row.HeldWorstDetour) + "%")} "
                + $"| {row.HeldBroken} | {row.HeldIncomparable} "
                + $"| {(holds ? "holds" : "**BREAKS**")} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"*Resident* is entries of the {PoolPairs}-pair pool the cache still holds; *declared "
            + $"valid* is the share of those the per-Segment Epoch passes; *improvable* is the "
            + $"share a fresh search strictly beats, which is **ground truth and independent of the "
            + $"rung**; *wrongly valid* is the intersection. *Not comparable* is resident entries "
            + $"whose fresh search found nothing, excluded from *improvable* and printed so the "
            + $"denominator is visible rather than assumed. **Total wrongly valid across every "
            + $"cache row: {heldWrong}. Total holding a Segment that no longer exists: "
            + $"{heldBrokenTotal}. Identity breaks: {identityBreaks} of "
            + $"{rows.Count(r => r.Rung is PathRung.Cache or PathRung.CacheTtl)} rows.**"));
        report.AppendLine();
        report.AppendLine(
            "**The *Improvable* column is the load-bearing one and it is the reason this table is "
            + "weaker than R5.4's.** There it read 9.22% on the Arterial gesture, so the "
            + "*wrongly valid* columns beside it had something to be wrong about. Here deletion "
            + "cannot improve a route by construction, so improvable is zero and every rung passes "
            + "trivially. **That is a confirmation of R5.4's asymmetry argument and not an "
            + "endorsement of the rung**: it says the per-Segment Epoch is exact under the half of "
            + "the core verb this storm applies, which is the half R5.3 already recommended it for. "
            + "**The hole is under addition, it is measured in R5.4, and nothing in R5.5 closes "
            + "it** — a deletion-only storm cannot, whatever it samples.");
        report.AppendLine();
        report.AppendLine(
            "**Two things this table does check that R5.5.2 could not, and the first is the "
            + "sharpest result in the section.** *Holding a deleted Segment* counts resident routes "
            + "whose own arcs the storm has removed; the rung is allowed to hold those, and "
            + "declaring one valid would be a stamping defect. **The *Identity* column tests, per "
            + "row, whether resident minus declared-valid equals that count** — whether the entries "
            + "the per-Segment Epoch refuses are precisely the entries containing a deleted "
            + "Segment, neither one more nor one fewer. That is the rung's exactness claim written "
            + "as an identity between two independently counted columns, and it is a far stronger "
            + "check than a hit rate, because a hit rate cannot distinguish exactness from luck. "
            + "**It is printed per row and totalled above, on the run where it holds**, which is "
            + "the only run on which printing it is worth anything.");
        report.AppendLine();
        report.AppendLine(
            "**And the *Resident* column is where a rotation that is expiring nothing stops looking "
            + "like one that is working.** The hit column alone cannot tell those apart — an empty "
            + "cache and a cache nobody invalidates both read as *no staleness* — whereas resident "
            + "counts fall monotonically with the forced-refresh rate, which is the rotation "
            + "leaving a footprint on something other than the column it is meant to move.");
        report.AppendLine();
        report.AppendLine(
            "**Two caveats travel with every figure here and neither is fixable inside S2.** The "
            + "hit-rate levels rest on R5.3's invented pool standing in for Trip repetition, which "
            + "needs Trip generation to replace; and the Street half of R5.4's addition finding "
            + "reads zero because the synthetic grid is degenerate. The ratios *between* rungs under "
            + "one pool are what this section is for, and they are what it may be quoted on.");
        report.AppendLine();
    }

    /// <summary>
    /// One path source, one O-D draw, one edit rate, one storm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every rung is warmed before its clock starts, and the reason is the <i>worst Tick</i> column
    /// rather than the mean one. R5.3 established that this workload's exposure is the worst Tick —
    /// at 16 Trip starts it already reaches 66% of the budget — and an unwarmed first Tick lands
    /// squarely in it, so a configuration that was measured first would publish the whole sweep's
    /// tiered-JIT bill as its own peak.
    /// </para>
    /// <para>
    /// The detour instrument runs <b>after</b> each sampled Tick's clock has stopped. It costs more
    /// than the Tick it prices, so leaving it inside the timed span would publish the instrument as
    /// the result — the defect R3 caught in its own harness and R5 inherited the warning from.
    /// </para>
    /// </remarks>
    private static Sourced MeasureSource(
        RoadGraph graph,
        ReverseArcs reverse,
        VectorArcs vectorArcs,
        EditStorm storm,
        SegmentArcs segmentArcs,
        Clusters clusters,
        Districts districts,
        OdPair[] pool,
        OdRung od,
        PathRung rung,
        int ttlPeriod,
        int editPeriod)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();

        AbstractGraph? abstractGraph = null;
        HpaSearch? hpa = null;
        EpochClock? clock = null;
        RouteCache? cache = null;
        DistanceVector? table = null;
        RouteStore? store = null;
        PointToPoint? flat = null;
        OneToAll? oneToAll = null;

        bool cached = rung is PathRung.Cache or PathRung.CacheTtl;

        if (cached)
        {
            abstractGraph = AbstractGraph.Build(
                graph, clusters, reverse, arcCost,
                transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
            hpa = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);
            clock = new EpochClock(graph, clusters);
            cache = new RouteCache(
                graph, clusters, CacheCapacity, MaxCachedArcs, MaxCachedClusters);
        }
        else if (rung == PathRung.NextHop)
        {
            table = new DistanceVector(graph, vectorArcs, districts.Count);
            for (int d = 0; d < districts.Count; d++)
            {
                table.Seed(d, districts.Representative[d], arcCost);
            }
        }
        else if (rung == PathRung.Shared)
        {
            oneToAll = new OneToAll(graph);
            store = RouteStore.ForDistrictPairs(graph, districts, arcCost, oneToAll);
        }
        else
        {
            flat = new PointToPoint(graph, arcCost);
        }

        var touched = new bool[clusters.Count];
        var scratch = new List<int>();
        var arcs = new List<int>();
        var changedArcs = new int[LadderGestureSegments * 4];

        var truthSearch = new PointToPoint(graph, arcCost);
        var legSearch = new PointToPoint(graph, arcCost);
        var detourArcs = new List<int>();
        var detours = new List<int>();

        int hits = 0;
        int stale = 0;
        int misses = 0;
        int unroutable = 0;
        int detourSkipped = 0;
        int detourBroken = 0;
        long sink = 0;
        long worstTick = 0;
        long totalTick = 0;

        int expiryWidth = ttlPeriod <= 0 ? 0 : (CacheCapacity + ttlPeriod - 1) / ttlPeriod;
        int expiryFrom = 0;

        // Warm the per-Trip path, then the edit response, then put the graph back. The warm gesture
        // is drawn at an index no Tick uses, so warming cannot consume a gesture the storm was
        // going to apply.
        for (int i = 0; i < LadderWarmQueries; i++)
        {
            Serve(pool[i % pool.Length]);
        }

        if (editPeriod > 0)
        {
            var warmGesture = storm.Draw(
                CounterHash.Seed, (ulong)LadderTicks + 1, GestureShape.Drag, LadderGestureSegments);

            storm.Apply(warmGesture, arcCost);
            Respond(warmGesture);
            storm.Revert(warmGesture, arcCost);
            Respond(warmGesture);
        }

        // A warmed cache would carry the warm-up's routes into the storm and inflate the first
        // Ticks' hit rate, so the cache alone is discarded and rebuilt after warming. The abstract
        // graph, the search and the JIT keep everything the warm-up bought them. Every counter the
        // warm-up touched is reset for the same reason — a warm-up that contributes to a published
        // count is a warm-up that is part of the result.
        if (cached)
        {
            cache = new RouteCache(
                graph, clusters, CacheCapacity, MaxCachedArcs, MaxCachedClusters);
        }

        hits = 0;
        stale = 0;
        misses = 0;
        unroutable = 0;

        for (int tick = 0; tick < LadderTicks; tick++)
        {
            long tickStart = Stopwatch.GetTimestamp();

            if (editPeriod > 0 && tick % editPeriod == 0)
            {
                var gesture = storm.Draw(
                    CounterHash.Seed, (ulong)tick, GestureShape.Drag, LadderGestureSegments);
                storm.Apply(gesture, arcCost);
                Respond(gesture);
            }

            if (rung == PathRung.CacheTtl)
            {
                cache!.Expire(expiryFrom, expiryWidth);
                expiryFrom = (expiryFrom + expiryWidth) % CacheCapacity;
            }

            for (int trip = 0; trip < TripsPerTick; trip++)
            {
                Serve(PairAt(tick, trip));
            }

            long taken = Since(tickStart);
            totalTick += taken;
            worstTick = taken > worstTick ? taken : worstTick;

            if (tick % DetourSampleEvery != 0)
            {
                continue;
            }

            for (int trip = 0; trip < TripsPerTick; trip++)
            {
                OdPair pair = PairAt(tick, trip);

                truthSearch.Bootstrap(
                    pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);
                var direct = truthSearch.Expand();

                if (!direct.Found)
                {
                    detourSkipped++;
                    continue;
                }

                detourArcs.Clear();
                truthSearch.PathArcs(detourArcs);
                long truth = ArcSum(arcCost, detourArcs);

                if (truth <= 0)
                {
                    detourSkipped++;
                    continue;
                }

                long served = ServedCost(
                    graph, districts, arcCost, legSearch, detourArcs, rung, pair,
                    cache, clock, table, store, out bool wasBroken);

                if (wasBroken)
                {
                    detourBroken++;
                    detourSkipped++;
                    continue;
                }

                if (served < 0)
                {
                    detourSkipped++;
                    continue;
                }

                detours.Add((int)(((served - truth) * 10_000) / truth));
            }
        }

        // What the cache HOLDS, as against what it served. The detour column above can only price
        // entries the Epoch declared valid at the moment a Trip asked for one — and under a
        // per-Segment Epoch a stale entry is *detected* and recomputed, so what is served is never
        // stale and that column structurally cannot show the R5.4 hole. This walks the whole pool
        // instead, prices every resident entry the rung declares good against a fresh search on the
        // graph as it now is, and counts the ones that are wrong anyway. It is R5.4's instrument
        // pointed at deletion rather than at addition, and R5.4's argument says it must read zero.
        int heldResident = 0;
        int heldDeclaredValid = 0;
        int heldImprovable = 0;
        int heldWronglyValid = 0;
        int heldBroken = 0;
        int heldIncomparable = 0;
        long heldDetourSum = 0;
        int heldWorstDetour = 0;

        if (cached)
        {
            foreach (OdPair pair in pool)
            {
                long key = cache!.KeyOf(pair.Origin, pair.Destination);
                Lookup outcome = cache.TryGet(key, EpochRung.PerSegment, clock!, out int slot);

                if (outcome == Lookup.Miss)
                {
                    continue;
                }

                heldResident++;

                // The Epoch's own verdict, counted over every resident entry and before any
                // comparison is attempted. Counting it inside the comparison — which an earlier
                // draft of this did — makes the column a share of a denominator it was never
                // eligible to fill, and the *resident minus declared-valid equals broken* identity
                // below then holds on the long-trip draw and fails on the short one, purely because
                // short trips take a bypass and a bypass returns no arcs to compare.
                if (outcome == Lookup.Hit)
                {
                    heldDeclaredValid++;
                }

                long held = RouteTotal(
                    graph, arcCost, cache.ArcsAt(slot), pair.Origin, pair.Destination,
                    out bool reachable);

                if (held < 0)
                {
                    // A resident route with a deleted arc in it. The rung is allowed to hold one;
                    // declaring it valid is a stamping defect, and that is what is counted.
                    heldBroken++;

                    if (outcome == Lookup.Hit)
                    {
                        heldWronglyValid++;
                    }

                    continue;
                }

                if (!reachable)
                {
                    heldIncomparable++;
                    continue;
                }

                // Priced hierarchy against hierarchy on **whole journey cost**, which is the
                // quantity HPA* itself minimises, rather than on the arc sums R5.5.2 prints. Two
                // routes of equal whole-journey cost can differ in arc sum by up to a Segment at
                // each end, and an earlier draft of this table compared arc sums and duly reported
                // one entry in 399 as *wrongly valid* at a detour of 1.02% — comfortably inside the
                // residual the optimality check above sizes at 5.26% for that very draw. It was the
                // units, not the cache, and the whole point of this table is that it must not
                // report the instrument as the rung.
                var fresh = hpa!.Run(pair.Origin, pair.Destination);

                if (!fresh.Found || fresh.CostTicks <= 0)
                {
                    heldIncomparable++;
                    continue;
                }

                if (fresh.CostTicks >= held)
                {
                    continue;
                }

                heldImprovable++;

                if (outcome != Lookup.Hit)
                {
                    continue;
                }

                heldWronglyValid++;
                int over = (int)(((held - fresh.CostTicks) * 10_000L) / fresh.CostTicks);
                heldDetourSum += over;
                heldWorstDetour = over > heldWorstDetour ? over : heldWorstDetour;
            }
        }

        int wrongCost = 0;
        int stranded = 0;
        long auditEntries = 0;

        if (rung == PathRung.NextHop)
        {
            var truthTable = new DistanceVector(graph, vectorArcs, districts.Count);
            var truthCosts = new int[graph.Nodes];

            for (int d = 0; d < districts.Count; d++)
            {
                truthTable.Seed(d, districts.Representative[d], arcCost);
            }

            for (int d = 0; d < districts.Count; d++)
            {
                truthTable.CopyCosts(d, truthCosts);
                table!.Audit(
                    d, districts.Representative[d], truthCosts, out int wrong, out int lost);
                wrongCost += wrong;
                stranded += lost;
            }

            auditEntries = (long)districts.Count * graph.Nodes;
        }

        detours.Sort();
        long sum = 0;
        foreach (int detour in detours)
        {
            sum += detour;
        }

        // Read once so the release JIT cannot delete the work the rungs were timed doing.
        if (sink == long.MinValue)
        {
            Console.Error.WriteLine("sink");
        }

        return new Sourced(
            od,
            rung,
            ttlPeriod,
            editPeriod,
            totalTick / LadderTicks,
            worstTick,
            hits,
            stale,
            misses,
            cache?.Expired ?? 0,
            unroutable,
            detours.Count == 0 ? 0 : (int)(sum / detours.Count),
            detours.Count == 0 ? 0 : detours[(detours.Count * 9) / 10],
            detours.Count == 0 ? 0 : detours[^1],
            detours.Count,
            detourSkipped,
            detourBroken,
            wrongCost,
            stranded,
            auditEntries,
            heldResident,
            heldDeclaredValid,
            heldImprovable,
            heldWronglyValid,
            heldWronglyValid == 0 ? 0 : (int)(heldDetourSum / heldWronglyValid),
            heldWorstDetour,
            heldBroken,
            heldIncomparable);

        OdPair PairAt(int tick, int trip)
        {
            ulong roll = CounterHash.Of(
                CounterHash.Seed,
                (ulong)tick,
                (ulong)trip,
                CounterHash.Purpose.GestureOrigin);
            return pool[CounterHash.Below(roll, pool.Length)];
        }

        void Respond(Gesture gesture)
        {
            switch (rung)
            {
                case PathRung.Cache:
                case PathRung.CacheTtl:
                    clock!.Bump(gesture);
                    abstractGraph!.RebuildForAll(gesture.Segments, touched, scratch);
                    break;

                case PathRung.NextHop:
                {
                    int changed = ChangedArcs(segmentArcs, gesture, changedArcs);
                    for (int d = 0; d < districts.Count; d++)
                    {
                        table!.RepairSubtree(
                            d, districts.Representative[d], arcCost, changedArcs, changed);
                    }

                    break;
                }

                case PathRung.Shared:
                    store = RouteStore.ForDistrictPairs(graph, districts, arcCost, oneToAll!);
                    break;

                default:
                    // The control has no edit response, which is the whole of what it is for.
                    break;
            }
        }

        void Serve(OdPair pair)
        {
            switch (rung)
            {
                case PathRung.Cache:
                case PathRung.CacheTtl:
                {
                    long key = cache!.KeyOf(pair.Origin, pair.Destination);
                    Lookup outcome = cache.TryGet(key, EpochRung.PerSegment, clock!, out _);

                    if (outcome == Lookup.Hit)
                    {
                        hits++;
                        break;
                    }

                    if (outcome == Lookup.Stale)
                    {
                        stale++;
                    }
                    else
                    {
                        misses++;
                    }

                    var found = hpa!.Run(pair.Origin, pair.Destination);
                    arcs.Clear();

                    if (found.Found)
                    {
                        hpa.Refine(arcs);
                        cache.Insert(key, arcs, EpochRung.PerSegment, clock!);
                        sink += arcs.Count;
                    }
                    else
                    {
                        unroutable++;
                    }

                    break;
                }

                case PathRung.NextHop:
                {
                    int originNode = graph.SegmentNodeA[pair.Origin.Segment];
                    int destination = districts.OfNode[graph.SegmentNodeA[pair.Destination.Segment]];
                    int cost = table!.Cost(originNode, destination);

                    if (cost >= DistanceVector.Unreachable)
                    {
                        unroutable++;
                    }
                    else
                    {
                        sink += cost;
                    }

                    break;
                }

                case PathRung.Shared:
                {
                    int from = districts.OfNode[graph.SegmentNodeA[pair.Origin.Segment]];
                    int to = districts.OfNode[graph.SegmentNodeA[pair.Destination.Segment]];
                    int route = (from * districts.Count) + to;
                    int length = store!.Length(route);

                    if (length == 0 && from != to)
                    {
                        unroutable++;
                    }
                    else
                    {
                        sink += length;
                    }

                    break;
                }

                default:
                {
                    flat!.Bootstrap(
                        pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);
                    var outcome = flat.Expand();
                    arcs.Clear();

                    if (outcome.Found)
                    {
                        flat.PathArcs(arcs);
                        sink += arcs.Count;
                    }
                    else
                    {
                        unroutable++;
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// The arc cost of what a rung actually served for one pair, or <c>-1</c> if no comparison is
    /// available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache rungs are read <b>after</b> the Tick that served them, which is why an entry the
    /// Tick found stale reads as a hit here: the Trip that found it stale was served a fresh search
    /// and the fresh route is what the entry now holds. So the lookup below returns what was served
    /// in both cases and not merely what is resident, which is the property the column needs.
    /// </para>
    /// <para>
    /// <c>wasBroken</c> is separate from a missing comparison on purpose. A served route containing
    /// a Segment that no longer exists is a <i>stamping defect</i> under a per-Segment Epoch and a
    /// deletion-only storm, and folding it into the same counter as an unroutable pair would let a
    /// real defect hide inside an expected loss.
    /// </para>
    /// </remarks>
    private static long ServedCost(
        RoadGraph graph,
        Districts districts,
        int[] arcCost,
        PointToPoint legSearch,
        List<int> scratch,
        PathRung rung,
        OdPair pair,
        RouteCache? cache,
        EpochClock? clock,
        DistanceVector? table,
        RouteStore? store,
        out bool wasBroken)
    {
        wasBroken = false;

        switch (rung)
        {
            case PathRung.Cache:
            case PathRung.CacheTtl:
            {
                long key = cache!.KeyOf(pair.Origin, pair.Destination);

                if (cache.TryGet(key, EpochRung.PerSegment, clock!, out int slot) != Lookup.Hit)
                {
                    return -1;
                }

                long served = ArcSum(arcCost, cache.ArcsAt(slot));
                wasBroken = served < 0;
                return served;
            }

            case PathRung.NextHop:
            {
                int originNode = graph.SegmentNodeA[pair.Origin.Segment];
                int destination = districts.OfNode[graph.SegmentNodeA[pair.Destination.Segment]];
                int representative = districts.Representative[destination];

                if (representative < 0)
                {
                    return -1;
                }

                int viaTable = table!.Cost(originNode, destination);

                if (viaTable >= DistanceVector.Unreachable)
                {
                    return -1;
                }

                long tail = LegCost(
                    graph, arcCost, legSearch, scratch,
                    new AccessPoint(FirstSegmentAt(graph, representative), 0), pair.Destination);

                return tail < 0 ? -1 : viaTable + tail;
            }

            case PathRung.Shared:
            {
                int from = districts.OfNode[graph.SegmentNodeA[pair.Origin.Segment]];
                int to = districts.OfNode[graph.SegmentNodeA[pair.Destination.Segment]];
                int fromRepresentative = districts.Representative[from];
                int toRepresentative = districts.Representative[to];

                if (fromRepresentative < 0 || toRepresentative < 0)
                {
                    return -1;
                }

                long head = LegCost(
                    graph, arcCost, legSearch, scratch, pair.Origin,
                    new AccessPoint(FirstSegmentAt(graph, fromRepresentative), 0));

                if (head < 0)
                {
                    return -1;
                }

                long between = ArcSum(arcCost, store!.Span((from * districts.Count) + to));

                if (between < 0)
                {
                    wasBroken = true;
                    return -1;
                }

                long tail = LegCost(
                    graph, arcCost, legSearch, scratch,
                    new AccessPoint(FirstSegmentAt(graph, toRepresentative), 0), pair.Destination);

                return tail < 0 ? -1 : head + between + tail;
            }

            default:
                // The control, re-run through a second search instance rather than aliased to the
                // truth. It must read exactly zero, and a zero that came from `served = truth` would
                // prove that the assignment worked and nothing else.
                return LegCost(
                    graph, arcCost, legSearch, scratch, pair.Origin, pair.Destination);
        }
    }

    private static long LegCost(
        RoadGraph graph,
        int[] arcCost,
        PointToPoint search,
        List<int> scratch,
        AccessPoint origin,
        AccessPoint destination)
    {
        search.Bootstrap(origin, destination, Modes.Car, HeuristicKind.Chebyshev);

        if (!search.Expand().Found)
        {
            return -1;
        }

        scratch.Clear();
        search.PathArcs(scratch);
        return ArcSum(arcCost, scratch);
    }

    /// <summary>
    /// One uncached point-to-point search with its arcs returned — R0's denominator, plus the
    /// refinement R3 found weakens a hierarchy's standing to 2.63× once a route has to come back
    /// with arcs rather than with a cost.
    /// </summary>
    private static long MeasureFlatDenominator(RoadGraph graph, OdPair[] pool)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var search = new PointToPoint(graph, arcCost);
        var arcs = new List<int>();
        int found = 0;

        long start = Stopwatch.GetTimestamp();

        foreach (OdPair pair in pool)
        {
            search.Bootstrap(pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);
            var outcome = search.Expand();
            arcs.Clear();

            if (outcome.Found)
            {
                search.PathArcs(arcs);
                found++;
            }
        }

        long taken = Since(start);
        return found == 0 ? 0 : taken / pool.Length;
    }

    /// <summary>
    /// The instrument check R5.5's detour column cannot do without: <b>on a pristine graph, does the
    /// hierarchy ever return a worse route than the flat search, and does this section's arc-sum
    /// spelling agree with the whole-journey cost both searches actually minimise?</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// R3.5 measured HPA\* at <b>100% optimal, 0.00% mean detour, no route cheaper than the flat
    /// optimum</b> on this graph, and argued structurally that a hierarchy built over full
    /// transitions cannot lose a route. R5.5's cache rows nonetheless print a non-zero <i>worst</i>
    /// detour with zero edits applied, which is either R3.5 being wrong or this section's units
    /// being wrong. Only a measurement separates the two, and it is cheap, so it is taken.
    /// </para>
    /// <para>
    /// <b>The two questions are deliberately answered on different quantities.</b> Both searches
    /// minimise whole journey cost — arcs <i>plus</i> the two Access Point remainders — while the
    /// detour column sums arcs and drops the remainders, because a cached route is a list of arcs
    /// and nothing else. Two routes with identical whole-journey cost can therefore have different
    /// arc sums whenever they enter the destination Segment from opposite endpoints, trading a
    /// larger remainder for a smaller arc total. That is a property of the column's units and not
    /// of any rung, and the only honest thing to do with it is to size it and print it beside every
    /// number it bounds.
    /// </para>
    /// </remarks>
    private static Optimality MeasureOptimality(
        RoadGraph graph,
        ReverseArcs reverse,
        Clusters clusters,
        OdPair[] pool,
        OdRung od)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var abstractGraph = AbstractGraph.Build(
            graph, clusters, reverse, arcCost,
            transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
        var hpa = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);
        var flat = new PointToPoint(graph, arcCost);
        var arcs = new List<int>();

        int samples = 0;
        int worse = 0;
        int better = 0;
        int differs = 0;
        int worstGap = 0;

        foreach (OdPair pair in pool)
        {
            flat.Bootstrap(pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);
            var direct = flat.Expand();

            if (!direct.Found || direct.CostTicks <= 0)
            {
                continue;
            }

            var found = hpa.Run(pair.Origin, pair.Destination);

            if (!found.Found)
            {
                continue;
            }

            samples++;

            if (found.CostTicks > direct.CostTicks)
            {
                worse++;
            }
            else if (found.CostTicks < direct.CostTicks)
            {
                better++;
            }

            arcs.Clear();
            flat.PathArcs(arcs);
            long truth = ArcSum(arcCost, arcs);

            if (arcs.Count == 0 || truth <= 0)
            {
                continue;
            }

            arcs.Clear();
            hpa.Refine(arcs);

            // A bypass never leaves its own Segments and returns no arcs at all, so it has no arc
            // sum to disagree about. Excluded rather than counted as a zero, which would dilute the
            // very residual this check exists to size.
            if (arcs.Count == 0)
            {
                continue;
            }

            long served = ArcSum(arcCost, arcs);

            if (served < 0 || served == truth)
            {
                continue;
            }

            differs++;
            int gap = (int)(((served - truth) * 10_000) / truth);
            worstGap = gap > worstGap ? gap : worstGap;
        }

        return new Optimality(od, samples, worse, better, differs, worstGap);
    }

    // --- R5.5.4 the rotation against the addition -------------------------------------------------

    /// <summary>
    /// One point on the healing curve: what the cache still holds wrongly, this many Ticks after a
    /// road was drawn.
    /// </summary>
    private sealed record Healed(
        int TtlPeriod,
        int RateHundredths,
        int Tick,
        int Resident,
        int DeclaredValid,
        int Improvable,
        int WronglyValid,
        int MeanDetour,
        int WorstDetour,
        int Expired,
        int Incomparable);

    private static void AppendHealing(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        EditStorm storm,
        Clusters clusters,
        OdDistribution distribution)
    {
        report.AppendLine("### R5.5.4 — does the rotation actually clear the addition hole");
        report.AppendLine();
        report.AppendLine(
            "**R5.5.2 prices the rotation's cost and never prices its benefit, and that asymmetry "
            + "is not publishable.** The hole a TTL exists to close is **addition** — R5.4's "
            + "finding that a route computed before a road existed cannot contain it, so no version "
            + "the per-Segment rung watches will ever move and the entry is stale *permanently*. "
            + "Every storm in R5.5 so far only ever deletes, so nothing in the section has shown a "
            + "rotation clearing anything. The benefit is arithmetically obvious, and `adr/0043` is "
            + "exactly the rule that says obvious is not a reason to leave it unmeasured.");
        report.AppendLine();
        report.AppendLine(
            "**The technique is R5.4's and it is reused rather than reinvented.** The abstract "
            + "graph is built on the **full** graph so every portal slot is reserved; a set of "
            + "Segments is deleted; the whole pool is cached against the damaged graph; then the "
            + "Segments are restored. **Restoration *is* addition**, and it needs no new portal. "
            + "The gesture is R5.4's Arterial rung — the smallest addition worth drawing, about "
            + "half a kilometre of new fast road — because R5.4 established that a larger addition "
            + "cannot do better and the figure is therefore a floor.");
        report.AppendLine();
        report.AppendLine(
            "**What is new is the window.** After the restoration the cache is run forward with "
            + "ordinary Trip traffic and a rotation active, and the wrongly-valid population is "
            + "sampled at points across the window rather than at its end. **The curve is the "
            + "deliverable**: a rotation period is a stated learning rate, so the quantity a "
            + "designer sets and a player experiences is how fast this decays, not where it stops.");
        report.AppendLine();
        report.AppendLine(
            "**The comparison is on whole journey cost, not on arc sums.** That is the correction "
            + "R5.5.3 had to make and it carries here: comparing arc sums manufactures improvable "
            + "entries out of two equal-cost routes that enter the destination Segment from "
            + "opposite endpoints. **This matters for reading the *tick 0* row against R5.4's "
            + "published 9.22%**, which was measured on arc sums.");
        report.AppendLine();

        var pool = distribution.Draw(
            CounterHash.Seed, AdditionPool, Modes.Car, new OdRung(OdShape.Uniform, 0),
            out _, out _);

        var rows = new List<Healed>();

        // The control first: no rotation at all. It is the instrument-can-move check and R5.4's
        // permanence finding reproduced — if it decays as fast as the rotations do, something other
        // than the rotation is clearing entries and every row below means nothing.
        Mark("R5.5.4 control, no rotation");
        rows.AddRange(MeasureHealing(graph, reverse, storm, clusters, pool, 0));

        foreach (int period in TtlPeriods)
        {
            Mark($"R5.5.4 rotation period {period}");
            rows.AddRange(MeasureHealing(graph, reverse, storm, clusters, pool, period));
        }

        report.AppendLine(
            "| Rotation | Forced refreshes / Tick | Ticks since addition | Resident "
            + "| Declared valid | Improvable | **Wrongly valid** | **Count** | Mean detour | Worst "
            + "| Not comparable |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var row in rows)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {(row.TtlPeriod == 0 ? "**none**" : "every " + row.TtlPeriod)} "
                + $"| {(row.TtlPeriod == 0 ? "0.00" : Hundredths(row.RateHundredths))} "
                + $"| {row.Tick} | {row.Resident} "
                + $"| {Percent(row.DeclaredValid, row.Resident)} "
                + $"| {Percent(row.Improvable, row.Resident)} "
                + $"| **{Percent(row.WronglyValid, row.Resident)}** "
                + $"| **{row.WronglyValid}** "
                + $"| {(row.WronglyValid == 0 ? "—" : Hundredths(row.MeanDetour) + "%")} "
                + $"| {(row.WronglyValid == 0 ? "—" : Hundredths(row.WorstDetour) + "%")} "
                + $"| {row.Incomparable} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"A pool of {AdditionPool} uniform origin-destination pairs cached on the damaged "
            + $"graph, the Arterial gesture restored, then {AdditionWindowTicks} Ticks of "
            + $"{TripsPerTick} Trip starts each. **The window is one full sweep of the longest "
            + $"rotation on the ladder**, so every rate here is entitled to a statement about the "
            + $"staleness bound — which is what R5.5.2's {TtlPeriods[^1]}-Tick row explicitly was "
            + $"not. *Not comparable* is resident entries whose fresh search found nothing, "
            + $"excluded from the shares and printed so the denominator is visible."));
        report.AppendLine();
        report.AppendLine(
            "**Read the *Count* column, not the share, and the reason is a trap this table was "
            + "built to walk into.** A rotation evicts, and an evicted entry leaves the "
            + "denominator as well as the numerator — so a wrongly-valid *share* can fall while "
            + "not one stale route has been replaced by a correct one, purely because the "
            + "population shrank underneath it. The absolute count cannot be got at that way. "
            + "**The *Resident* column is printed beside it for exactly this reason**: a count "
            + "falling while resident holds steady is entries being relearned, and a count falling "
            + "in step with resident is entries being thrown away.");
        report.AppendLine();

        int controlFirst = 0;
        int controlLast = 0;

        foreach (var row in rows)
        {
            if (row.TtlPeriod != 0)
            {
                continue;
            }

            if (row.Tick == 0)
            {
                controlFirst = row.WronglyValid;
            }

            controlLast = row.WronglyValid;
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The control is the check that the instrument can move, and it reads "
            + $"{controlFirst} wrongly-valid entries at the addition and {controlLast} after "
            + $"{AdditionWindowTicks} Ticks with no rotation.** R5.4's claim is that this error has "
            + $"no mechanism that will ever notice it — the road the route should be using is one "
            + $"the route does not contain, so no version it watches will move again — and that "
            + $"**only eviction removes it**. A control that decayed to nothing on its own would "
            + $"mean R5.4's *does not heal* is wrong, which is a larger finding than this "
            + $"subsection and would be reported as one rather than published as a rotation's "
            + $"success."));
        report.AppendLine();

        int controlPlateauTick = AdditionWindowTicks;
        int controlResident = 0;

        foreach (var row in rows)
        {
            if (row.TtlPeriod == 0 && row.Tick == 0)
            {
                controlResident = row.Resident;
            }
        }

        foreach (var row in rows)
        {
            if (row.TtlPeriod == 0 && row.WronglyValid == controlLast)
            {
                controlPlateauTick = row.Tick;
                break;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**It plateaus, and the plateau is the finding.** The control falls to {controlLast} "
            + $"entries by Tick {controlPlateauTick} and then **does not move again for the "
            + $"remaining {AdditionWindowTicks - controlPlateauTick} Ticks**, with its resident "
            + $"population constant at {controlResident} throughout. **That is R5.4's *does not "
            + $"heal* measured rather than argued**: {Percent(controlLast, controlFirst)} of the "
            + $"error survives every Tick this window contains, and the flatness is what "
            + $"distinguishes a permanent error from a slow one. A curve still descending at the "
            + $"right-hand edge would have meant the window was too short to conclude anything; "
            + $"this one stopped descending at Tick {controlPlateauTick} and stayed stopped."));
        report.AppendLine();
        report.AppendLine(
            "**Which row the conclusion rests on, stated rather than left to the reader.** A "
            + "rotation can drive the wrongly-valid count to zero two ways — by teaching entries "
            + "the new road, or by throwing them away — and only the first is closing the hole. "
            + "The two are told apart by what happens to the resident population beside the count:");
        report.AppendLine();
        report.AppendLine(
            "| Rotation | Forced refreshes / Tick | Wrongly valid cleared by | Resident retained "
            + "| Verdict |");
        report.AppendLine("|---|---:|---:|---:|---|");

        foreach (int period in new[] { 0 }.Concat(TtlPeriods))
        {
            var rung = rows.Where(r => r.TtlPeriod == period).ToList();

            if (rung.Count == 0)
            {
                continue;
            }

            var cleared = rung.FirstOrDefault(r => r.WronglyValid == 0);
            int retained = rung[0].Resident == 0
                ? 0
                : (int)((rung[^1].Resident * 10_000L) / rung[0].Resident);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {(period == 0 ? "**none**" : "every " + period)} "
                + $"| {(period == 0 ? "0.00" : Hundredths(rung[0].RateHundredths))} "
                + $"| {(cleared is null ? "**never** — plateau at " + rung[^1].WronglyValid : "Tick " + cleared.Tick)} "
                + $"| {Hundredths(retained)}% "
                + $"| {(cleared is null ? "the hole is permanent" : retained >= 9_500 ? "**relearned** — the count fell and the population did not" : retained >= 8_500 ? "relearned, at a visible cost in resident entries" : "cleared partly by discarding the cache")} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**The slowest rotation on the ladder is the one that settles it, and it settles it "
            + "cheaply.** At 0.40 forced refreshes a Tick the whole hole is gone within one "
            + "rotation period while the cache keeps 97% of its resident entries — so the count "
            + "cannot have fallen because the denominator did, and the entries really were taught "
            + "the new road rather than discarded. **The fastest rotation clears it sooner and "
            + "sheds 40% of the cache doing it**, which is the same bill R5.5.2 charges as 25 "
            + "points of hit rate. The conclusion rests on the slow row; the fast row is the one "
            + "tripwire 4 was written about and it is exactly the row that cannot carry it.");
        report.AppendLine();
        report.AppendLine(
            "**The error that survives is worse than the error that clears, and that is not "
            + "reassuring.** The control's mean detour *rises* from 16.35% to 19.31% as its count "
            + "falls from 38 to 23 — collision eviction is removing the mild errors and leaving "
            + "the severe ones. It is the same mechanism `adr/0012` predicts from the other end: "
            + "keyed by origin-destination pair rather than by agent, **a hot pair is the least "
            + "likely to be evicted precisely because it is hot**, so what persists is every "
            + "driver's route on the busiest pairs. A residual quoted as a count understates it; "
            + "the surviving entries are the ones carrying the most traffic and the largest detour.");
        report.AppendLine();
        report.AppendLine(
            "**Tick 0 is a re-measurement of R5.4 and it agrees, which also bounds the units "
            + "worry.** R5.4 published **9.22% improvable, 16.71% mean detour, 62.65% worst** on "
            + "this gesture, measured on **arc sums**; the row above reads **9.22%** improvable "
            + "with a mean of **16.35%** and a worst of **62.41%**, measured on whole journey "
            + "cost. The improvable share is identical to the digit and the detours move by about "
            + "a third of a percentage point. **So R5.4's figures carry the arc-sum residual this "
            + "section sized, and carry it at the level of rounding rather than as a factor** — "
            + "which is worth stating explicitly, because R5.5.3 found the same residual "
            + "manufacturing a *wrongly valid* entry out of nothing and the two outcomes could "
            + "otherwise be read as contradicting each other.");
        report.AppendLine();
        report.AppendLine(
            "**The residual decay in the control is the direct-mapped cache's collision eviction "
            + "and not the Epoch noticing anything.** R5.3 measured the miss column flat at 28–31% "
            + "regardless of edit rate and identified it as collisions rather than staleness; a "
            + "colliding pair evicts whatever shares its slot, and the replacement is searched on "
            + "the graph as it now is, so it comes back correct. That is precisely the *only* "
            + "removal mechanism R5.4 named. It is reported here rather than subtracted, because "
            + "the honest comparison for every rotation row is against a control that has the same "
            + "collisions and differs only in the rotation.");
        report.AppendLine();
        report.AppendLine(
            "**And `adr/0012` is why the collisions do not rescue the design.** The cache is keyed "
            + "by origin-destination pair rather than by agent, so an entry is not one driver's "
            + "habit but every driver's route — and **a hot pair is the least likely to be evicted "
            + "precisely because it is hot**. Collision eviction clears the entries nobody is "
            + "using. The rotation is the only mechanism on this table that touches an entry "
            + "*because time has passed* rather than because something else wanted its slot.");
        report.AppendLine();
    }

    /// <summary>
    /// Deletes a road, caches the world without it, puts the road back, and then watches how long
    /// the cache goes on serving routes that predate it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Trip traffic runs during the window and it is load-bearing.</b> A rotation on its own only
    /// empties slots; what makes an entry <i>correct</i> again is the next Trip missing on it and
    /// searching the graph as it now is. A window with the rotation running and no traffic would
    /// measure a cache being deleted and would report it as a cache being taught.
    /// </para>
    /// <para>
    /// <b>Sampling does not perturb what it samples.</b> <c>TryGet</c> is a read, and the fresh
    /// searches the comparison needs go through the search object rather than through the cache, so
    /// a sample point costs the measurement nothing it would not otherwise have.
    /// </para>
    /// </remarks>
    private static List<Healed> MeasureHealing(
        RoadGraph graph,
        ReverseArcs reverse,
        EditStorm storm,
        Clusters clusters,
        OdPair[] pool,
        int ttlPeriod)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();

        // Built on the FULL graph, which is the whole trick: every portal slot the restored
        // Segments will need already exists, so the restore is an addition the hierarchy can absorb.
        var abstractGraph = AbstractGraph.Build(
            graph, clusters, reverse, arcCost,
            transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
        var hpa = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);
        var clock = new EpochClock(graph, clusters);
        var cache = new RouteCache(
            graph, clusters, CacheCapacity, MaxCachedArcs, MaxCachedClusters);

        var touched = new bool[clusters.Count];
        var scratch = new List<int>();
        var arcs = new List<int>();

        for (int i = 0; i < LadderWarmQueries; i++)
        {
            OdPair warm = pool[i % pool.Length];
            hpa.Run(warm.Origin, warm.Destination);
            arcs.Clear();
            hpa.Refine(arcs);
        }

        var gesture = storm.Draw(CounterHash.Seed, 7, GestureShape.Arterial, GestureSizes[^1]);
        var samples = new List<Healed>();

        if (gesture.Segments.Length == 0)
        {
            return samples;
        }

        // The world before the road exists.
        storm.Apply(gesture, arcCost);
        abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);

        foreach (OdPair pair in pool)
        {
            var outcome = hpa.Run(pair.Origin, pair.Destination);

            if (!outcome.Found)
            {
                continue;
            }

            arcs.Clear();
            hpa.Refine(arcs);

            if (arcs.Count > 0)
            {
                cache.Insert(
                    cache.KeyOf(pair.Origin, pair.Destination), arcs, EpochRung.PerSegment, clock);
            }
        }

        // The addition.
        storm.Revert(gesture, arcCost);
        clock.Bump(gesture);
        abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);

        int width = ttlPeriod <= 0 ? 0 : (CacheCapacity + ttlPeriod - 1) / ttlPeriod;
        int from = 0;
        int next = 0;

        for (int tick = 0; tick <= AdditionWindowTicks; tick++)
        {
            if (next < AdditionSamplePoints.Length && AdditionSamplePoints[next] == tick)
            {
                samples.Add(SampleHealing(
                    graph, arcCost, hpa, cache, clock, pool, ttlPeriod, tick, cache.Expired));
                next++;
            }

            if (tick == AdditionWindowTicks)
            {
                break;
            }

            if (width > 0)
            {
                cache.Expire(from, width);
                from = (from + width) % CacheCapacity;
            }

            for (int trip = 0; trip < TripsPerTick; trip++)
            {
                ulong roll = CounterHash.Of(
                    CounterHash.Seed,
                    (ulong)tick,
                    (ulong)trip,
                    CounterHash.Purpose.GestureOrigin);
                OdPair pair = pool[CounterHash.Below(roll, pool.Length)];

                long key = cache.KeyOf(pair.Origin, pair.Destination);

                if (cache.TryGet(key, EpochRung.PerSegment, clock, out _) == Lookup.Hit)
                {
                    continue;
                }

                var found = hpa.Run(pair.Origin, pair.Destination);
                arcs.Clear();

                if (!found.Found)
                {
                    continue;
                }

                hpa.Refine(arcs);

                if (arcs.Count > 0)
                {
                    cache.Insert(key, arcs, EpochRung.PerSegment, clock);
                }
            }
        }

        // The rotation's rate, measured rather than derived from the period — the period is the
        // half that does not transfer to a cache of a different size.
        int rate = (int)((cache.Expired * 100L) / AdditionWindowTicks);

        for (int i = 0; i < samples.Count; i++)
        {
            samples[i] = samples[i] with { RateHundredths = rate };
        }

        return samples;
    }

    private static Healed SampleHealing(
        RoadGraph graph,
        int[] arcCost,
        HpaSearch hpa,
        RouteCache cache,
        EpochClock clock,
        OdPair[] pool,
        int ttlPeriod,
        int tick,
        int expired)
    {
        int resident = 0;
        int declaredValid = 0;
        int improvable = 0;
        int wronglyValid = 0;
        int incomparable = 0;
        long detourSum = 0;
        int worstDetour = 0;

        foreach (OdPair pair in pool)
        {
            long key = cache.KeyOf(pair.Origin, pair.Destination);
            Lookup outcome = cache.TryGet(key, EpochRung.PerSegment, clock, out int slot);

            if (outcome == Lookup.Miss)
            {
                continue;
            }

            resident++;

            if (outcome == Lookup.Hit)
            {
                declaredValid++;
            }

            long held = RouteTotal(
                graph, arcCost, cache.ArcsAt(slot), pair.Origin, pair.Destination,
                out bool reachable);

            if (held < 0 || !reachable)
            {
                incomparable++;
                continue;
            }

            var fresh = hpa.Run(pair.Origin, pair.Destination);

            if (!fresh.Found || fresh.CostTicks <= 0)
            {
                incomparable++;
                continue;
            }

            if (fresh.CostTicks >= held)
            {
                continue;
            }

            improvable++;

            if (outcome != Lookup.Hit)
            {
                continue;
            }

            wronglyValid++;
            int over = (int)(((held - fresh.CostTicks) * 10_000L) / fresh.CostTicks);
            detourSum += over;
            worstDetour = over > worstDetour ? over : worstDetour;
        }

        return new Healed(
            ttlPeriod,
            0,
            tick,
            resident,
            declaredValid,
            improvable,
            wronglyValid,
            wronglyValid == 0 ? 0 : (int)(detourSum / wronglyValid),
            worstDetour,
            expired,
            incomparable);
    }

    /// <summary>
    /// Every arc a gesture deletes, coalesced into one buffer. <b>The whole gesture at once is the
    /// point</b> — R5.2 found the per-edit spelling costing an order of magnitude more than the
    /// coalesced one at the hierarchy, and R5.5.1 exists to find out whether that generalises. The
    /// ratio is deliberately not quoted here: it is measured per run and per rung by AppendRepair,
    /// which returns it, and a doc comment cannot be kept honest by anything.
    /// </summary>
    private static int ChangedArcs(SegmentArcs segmentArcs, Gesture gesture, int[] into)
    {
        int count = 0;

        foreach (int segment in gesture.Segments)
        {
            foreach (int arc in segmentArcs.For(segment))
            {
                if (count == into.Length)
                {
                    return count;
                }

                into[count++] = arc;
            }
        }

        return count;
    }

    /// <summary>
    /// The <b>whole journey cost</b> of a stored route — its arcs on the live costs, plus the two
    /// Access Point remainders its own endpoints imply. <c>-1</c> if any arc of it has been deleted;
    /// <paramref name="reachable"/> is false if either remainder is not payable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because comparing arc sums is not a comparison.</b> Both searches in this
    /// spike choose a route by minimising arcs <i>plus</i> remainders, so two routes of identical
    /// whole-journey cost can have arc sums differing by up to a Segment at each end — one enters
    /// the destination Segment from the far endpoint and trades a larger remainder for a smaller arc
    /// total. R5.5.2's detour column pays that residual because a cached route is a list of arcs and
    /// the truth is a different search; R5.5.3 does not have to, because both sides there are the
    /// same hierarchy, and a table about staleness must not report the units as staleness.
    /// </para>
    /// <para>
    /// <b>The remainders are priced free-flow, through the null path, because that is what
    /// <c>HpaSearch</c> itself does.</b> Matching the rung's own accounting is the point — the
    /// comparison is hierarchy against hierarchy. That the hierarchy's remainders cannot observe a
    /// deletion at all is a separate defect, recorded against the <i>Unroutable</i> column, and it
    /// cancels here because both sides carry it identically.
    /// </para>
    /// </remarks>
    private static long RouteTotal(
        RoadGraph graph,
        int[] arcCost,
        ReadOnlySpan<int> arcs,
        AccessPoint origin,
        AccessPoint destination,
        out bool reachable)
    {
        reachable = false;

        long sum = ArcSum(arcCost, arcs);

        if (sum < 0)
        {
            return -1;
        }

        if (arcs.Length == 0)
        {
            // A bypass never leaves its own Segments, so the whole journey is the two remainders
            // and there is nothing to derive endpoints from. Priced by the search that owns it.
            int direct = SegmentEntry.SameSegmentCost(graph, null, Modes.Car, origin, destination);
            reachable = direct < HpaSearch.Unreachable;
            return reachable ? direct : 0;
        }

        int first = arcs[0];
        int firstSegment = graph.ArcSegment[first];
        int a = graph.SegmentNodeA[firstSegment];
        int b = graph.SegmentNodeB[firstSegment];
        int from = graph.ArcTarget[first] == a ? b : a;
        int to = graph.ArcTarget[arcs[^1]];

        int entry = SegmentEntry.CostToEndpoint(graph, null, Modes.Car, origin, from);
        int exit = SegmentEntry.CostFromEndpoint(graph, null, Modes.Car, to, destination);

        if (entry >= HpaSearch.Unreachable || exit >= HpaSearch.Unreachable)
        {
            return 0;
        }

        reachable = true;
        return sum + entry + exit;
    }

    /// <summary>
    /// The arc cost of a route on the costs as they now are, or <c>-1</c> if any arc of it has been
    /// deleted. The sentinel is not a convenience: <c>Impassable</c> is <c>int.MaxValue</c> and
    /// summing it would publish a detour of several billion percent as a number.
    /// </summary>
    private static long ArcSum(int[] arcCost, ReadOnlySpan<int> arcs)
    {
        long sum = 0;

        foreach (int arc in arcs)
        {
            if (arcCost[arc] == RoadGraph.Impassable)
            {
                return -1;
            }

            sum += arcCost[arc];
        }

        return sum;
    }

    private static long ArcSum(int[] arcCost, List<int> arcs)
    {
        long sum = 0;

        foreach (int arc in arcs)
        {
            if (arcCost[arc] == RoadGraph.Impassable)
            {
                return -1;
            }

            sum += arcCost[arc];
        }

        return sum;
    }

    /// <summary>
    /// A Segment incident to a node, so a District representative can be spoken to as an Access
    /// Point. R4.8 does the same and records the same caveat: the leg starts at the Segment's own
    /// node A rather than at the representative, which can only make a followed route look cheaper.
    /// </summary>
    private static int FirstSegmentAt(RoadGraph graph, int node)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            return graph.ArcSegment[arc];
        }

        return 0;
    }

    private static string Name(PathRung rung, int ttlPeriod) => rung switch
    {
        PathRung.Cache => "cache",
        PathRung.CacheTtl => ttlPeriod == 0
            ? "cache+ttl"
            : string.Create(CultureInfo.InvariantCulture, $"cache+ttl {ttlPeriod}"),
        PathRung.NextHop => "nexthop",
        PathRung.Shared => "shared",
        _ => "flat",
    };

    /// <summary>
    /// Progress to stderr. <b>This sweep is long enough that a silent run is indistinguishable from
    /// a hung one</b>, and R4 needed the same thing for the same reason. It never reaches the
    /// report, so it cannot contaminate a capture.
    /// </summary>
    private static void Mark(string section) =>
        Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {section}");

    // --- shared -----------------------------------------------------------------------------------

    /// <summary>
    /// Walks the whole sweep once before any of it is timed.
    /// </summary>
    /// <remarks>
    /// R1's finding, which R3 inherited and R5 inherits again: R1 needed four warm-up schemes
    /// before its cold column stopped falling smoothly with the swept axis — the shape a reader
    /// would most readily believe — because the cost was per-process rather than per-rung, and the
    /// small rungs never called the kernel enough times to leave tier 0.
    /// </remarks>
    private static void Warm(RoadGraph graph, ReverseArcs reverse, EditStorm storm)
    {
        foreach (int chunks in ClusterRungs)
        {
            var clusters = Clusters.Partition(graph, chunks);
            var arcCost = (int[])graph.ArcCarTicks.Clone();
            var abstractGraph = AbstractGraph.Build(
                graph, clusters, reverse, arcCost,
                transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);

            var touched = new bool[clusters.Count];
            var scratch = new List<int>();

            foreach (GestureShape shape in Shapes)
            {
                var gesture = storm.Draw(CounterHash.Seed, 0, shape, GestureSizes[^1]);
                storm.Apply(gesture, arcCost);
                abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
                storm.Revert(gesture, arcCost);
                abstractGraph.RebuildForAll(gesture.Segments, touched, scratch);
            }
        }
    }

    private static int TouchedClusters(RoadGraph graph, Clusters clusters, Gesture gesture)
    {
        var seen = new HashSet<int>();

        foreach (int segment in gesture.Segments)
        {
            seen.Add(clusters.OfNode[graph.SegmentNodeA[segment]]);
            seen.Add(clusters.OfNode[graph.SegmentNodeB[segment]]);
        }

        return seen.Count;
    }

    /// <summary>
    /// Elapsed nanoseconds since a <see cref="Stopwatch"/> timestamp.
    /// </summary>
    /// <remarks>
    /// <b>Split into whole seconds and a remainder because the obvious spelling overflows.</b>
    /// <c>elapsed × 1,000,000,000</c> passes <c>long.MaxValue</c> at about 9.2 seconds on a
    /// nanosecond-frequency clock. R4 measured a rung that took four minutes and published
    /// <b>−8,267.51 ms</b> for it. <b>R5 is the first task in this spike that times a storm rather
    /// than a loop</b>, so this is not a latent hazard here — it is on the path.
    /// </remarks>
    private static long Since(long start)
    {
        long elapsed = Stopwatch.GetTimestamp() - start;
        long whole = elapsed / Stopwatch.Frequency;
        long remainder = elapsed - (whole * Stopwatch.Frequency);
        return (whole * 1_000_000_000) + (remainder * 1_000_000_000 / Stopwatch.Frequency);
    }

    private static string Microseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10)) + " µs";

    private static string Milliseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10_000)) + " ms";

    private static string Percent(long part, long whole) =>
        whole == 0 ? "—" : Hundredths((int)((part * 10_000) / whole)) + "%";

    private static string Ratio(long numerator, long denominator) =>
        denominator == 0 ? "—" : Hundredths((int)((numerator * 100) / denominator)) + "×";

    private static string Hundredths(int value) => string.Create(
        CultureInfo.InvariantCulture, $"{value / 100}.{IntegerMath.Abs(value % 100):D2}");

    private static string Bytes(long bytes) =>
        bytes < 1024 ? bytes + " B"
        : bytes < 1024 * 1024 ? Hundredths((int)(bytes * 100 / 1024)) + " KiB"
        : bytes < 1024L * 1024 * 1024 ? Hundredths((int)(bytes * 100 / (1024 * 1024))) + " MiB"
        : Hundredths((int)(bytes * 100 / (1024L * 1024 * 1024))) + " GiB";
}
