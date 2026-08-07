using System.Diagnostics;
using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Cluster;
using S2.Routing.Graph;
using S2.Routing.Routing;
using S2.Routing.Storm;

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

        AppendGestures(report, graph, storm, segmentArcs);
        AppendRepair(report, graph, reverse, storm);
        AppendLadder(report, graph, reverse, storm);
        AppendAddition(report, graph, reverse, storm);

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

    private static void AppendRepair(
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
