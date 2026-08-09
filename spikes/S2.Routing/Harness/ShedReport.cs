using System.Diagnostics;
using System.Globalization;
using System.Text;
using S2.Routing.Cluster;
using S2.Routing.Graph;
using S2.Routing.Storm;

namespace S2.Routing.Harness;

/// <summary>
/// R5.6 — the Parking Shed, the second Epoch consumer, and the one the ladder is decided by.
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0010</c>: <i>"It ranks the ladder differently from routes, which is why it must be
/// measured beside them. A ladder chosen on routes alone would be chosen on the cheaper of the two
/// consumers."</i> <b>R6.2 chose 4-way LRU on routes alone.</b>
/// </para>
/// <para>
/// <b>The tripwire, written before the numbers.</b> <c>plans/0010</c> predicts that under a global
/// Epoch <i>"one road edit invalidates all ~150,000 sheds at once"</i>, and that the rebuild is paid
/// <b>on arrival</b> — the moment a Trip is trying to finish — making it a stampede triggered by the
/// player's most common action. If that reproduces, the global rung is out on this consumer whatever
/// it did on routes.
/// </para>
/// <para>
/// <b>It also settles a conditional the board has been carrying.</b> R3 picked an 8-Chunk cluster
/// over 16 and the board records the choice as <i>"conditional on R5.6, which may rank a Parking Shed
/// differently, so the sweep is not deleted."</i> Both sizes are swept here.
/// </para>
/// </remarks>
internal static class ShedReport
{
    /// <summary>
    /// <c>CONTEXT.md</c>'s own working figure: <i>"five Buildings share a Segment at the working
    /// figures"</i>. Not this harness's invention, which matters because the section scales with it.
    /// </summary>
    private const int BuildingsPerSegment = 5;

    /// <summary>
    /// Acceptable walking distance, in Tiles at ~4 m. <b>The corpus states no number</b> — it says
    /// <i>acceptable</i> and leaves it — so it is swept, and every row carries its radius.
    /// </summary>
    private static readonly int[] RadiusTiles = [50, 100, 200];

    /// <summary>The rung every table below is anchored on, and it is the middle one.</summary>
    private const int AnchorRadiusTiles = 100;

    private static readonly GestureShape[] GestureShapes =
        [GestureShape.Drag, GestureShape.Scattered, GestureShape.Arterial];

    /// <summary>Segments per gesture. R5's own sizes, so the two sections compare.</summary>
    private static readonly int[] GestureSizes = [1, 16, 256];

    /// <summary>R3's two surviving cluster sizes, in Chunks per side.</summary>
    private static readonly int[] ClusterRungs = [8, 16];

    private const int GesturesPerRung = 24;

    private const long TickBudgetNanoseconds = 15_600_000;

    private sealed record ShedShape(
        int RadiusTiles,
        int Sheds,
        long BuildNs,
        int MeanBins,
        int MeanBall,
        int MeanPaths,
        int Empty,
        long BallEntries,
        long PathEntries);

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var segmentArcs = SegmentArcs.Of(graph);
        var storm = new EditStorm(graph, segmentArcs);

        report.AppendLine("## S2 R5.6 — the Parking Shed, and the rung it disagrees with");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(
            "**The second Epoch consumer, and `plans/0010` calls it the one the ladder is most likely "
            + "to be decided by.** It scales with **Buildings** rather than with routes, and a shed is "
            + "a *neighbourhood* rather than a *path* — so what *\"my Segments\"* even means is a "
            + "choice, which is why there are **four rungs here where routes had three**. **R6.2 "
            + "recommended 4-way LRU on routes alone**, which is the exact thing `plans/0010` warned "
            + "against.");
        report.AppendLine();

        List<ShedShape> shapes = AppendShape(report, graph);
        ShedShape anchor = shapes.First(s => s.RadiusTiles == AnchorRadiusTiles);
        AppendStorm(report, graph, storm, anchor);

        return report.ToString();
    }

    /// <summary>R5.6a — what a shed is, before anything is invalidated.</summary>
    private static List<ShedShape> AppendShape(StringBuilder report, RoadGraph graph)
    {
        var shapes = new List<ShedShape>();

        report.AppendLine("### R5.6a — what a shed actually is");
        report.AppendLine();

        foreach (int tiles in RadiusTiles)
        {
            int radius = Units.TraversalTicks(tiles, Units.WalkFreeFlow);
            var sheds = ParkingSheds.Place(graph, BuildingsPerSegment, radius);
            var builder = new ShedBuilder(graph, sheds);

            for (int warm = 0; warm < 256 && warm < sheds.Count; warm++)
            {
                builder.Build(sheds.AccessPointOf(warm), radius);
            }

            long bins = 0;
            long ball = 0;
            long paths = 0;
            int empty = 0;

            long start = Stopwatch.GetTimestamp();

            for (int building = 0; building < sheds.Count; building++)
            {
                int found = builder.Build(sheds.AccessPointOf(building), radius);

                bins += found;
                ball += builder.Ball.Count;
                paths += builder.Paths.Count;

                if (found == 0)
                {
                    empty++;
                }
            }

            long elapsed = Elapsed(start);

            shapes.Add(new ShedShape(
                tiles,
                sheds.Count,
                elapsed / sheds.Count,
                (int)(bins / sheds.Count),
                (int)(ball / sheds.Count),
                (int)(paths / sheds.Count),
                empty,
                ball,
                paths));
        }

        report.AppendLine(
            "| Walk radius | Sheds | Build | Bins found | Ball Segments | Path Segments | Empty |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");

        foreach (ShedShape shape in shapes)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {shape.RadiusTiles * 4} m | {shape.Sheds:N0} | {Ns(shape.BuildNs)} "
                + $"| {shape.MeanBins:N0} | {shape.MeanBall:N0} | {shape.MeanPaths:N0} "
                + $"| {shape.Empty:N0} |"));
        }

        report.AppendLine();

        ShedShape anchor = shapes.First(s => s.RadiusTiles == AnchorRadiusTiles);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**A shed is not a path, and the two witness columns are the whole argument.** At 400 m a "
            + $"shed's walk ball explores **{anchor.MeanBall:N0} Segments** while the walks to the "
            + $"Bins it keeps touch **{anchor.MeanPaths:N0}**. A route's witness is the arcs it drives "
            + $"and it stores them anyway; **a shed's conservative witness is "
            + $"{Ratio(anchor.MeanBall, anchor.MeanPaths)} its own answer**, and it is a structure the "
            + $"shed has no other reason to carry."));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Rebuilding every shed in the city costs {Ms(anchor.BuildNs * anchor.Sheds)}** at "
            + $"{Ns(anchor.BuildNs)} each. That is the figure every row below is denominated in, and "
            + $"it is why the global rung is a question about the Tick budget rather than about cache "
            + $"hygiene."));
        report.AppendLine();

        return shapes;
    }

    /// <summary>R5.6b — the storm, and what each rung invalidates.</summary>
    private static void AppendStorm(
        StringBuilder report, RoadGraph graph, EditStorm storm, ShedShape anchor)
    {
        report.AppendLine("### R5.6b — the storm, and the stampede");
        report.AppendLine();

        int radius = Units.TraversalTicks(anchor.RadiusTiles, Units.WalkFreeFlow);
        var sheds = ParkingSheds.Place(graph, BuildingsPerSegment, radius);
        var builder = new ShedBuilder(graph, sheds);
        var partitions = ClusterRungs.Select(r => Clusters.Partition(graph, r)).ToArray();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"At **{anchor.RadiusTiles * 4} m** — {sheds.Count:N0} sheds, {Ns(anchor.BuildNs)} to "
            + $"rebuild one. Gestures are R5's own, so the two sections compare directly. Each row is "
            + $"the mean over {GesturesPerRung} gestures."));
        report.AppendLine();

        Index ball = BuildIndex(sheds, builder, radius, graph.Segments, useBall: true);
        Index paths = BuildIndex(sheds, builder, radius, graph.Segments, useBall: false);
        Index[] clusterIndex = partitions
            .Select(p => BuildClusterIndex(sheds, builder, radius, graph, p))
            .ToArray();

        var stamp = new int[sheds.Count];
        int generation = 0;

        report.AppendLine(
            "| Gesture | Asked | Got | Rung | Sheds invalidated | Share | Rebuild at the edit "
            + "| Of a Tick |");
        report.AppendLine("|---|---:|---:|---|---:|---:|---:|---:|");

        foreach (GestureShape shape in GestureShapes)
        {
            foreach (int size in GestureSizes)
            {
                var gestures = new List<Gesture>();

                for (int g = 0; g < GesturesPerRung; g++)
                {
                    Gesture gesture = storm.Draw(CounterHash.Seed, (ulong)g, shape, size);

                    if (gesture.Segments.Length > 0)
                    {
                        gestures.Add(gesture);
                    }
                }

                // Reported rather than assumed equal to `size`: an Arterial drag runs out of fast
                // road and stops short, which is what made the 16- and 256-Segment arterial rows
                // byte-identical in the first capture. Gesture carries Requested for exactly this.
                long got = gestures.Sum(g => (long)g.Segments.Length);
                int actual = gestures.Count == 0 ? 0 : (int)(got / gestures.Count);

                Emit(report, shape, size, actual, "global", (long)gestures.Count * sheds.Count,
                    gestures.Count, sheds.Count, anchor.BuildNs);

                for (int r = 0; r < ClusterRungs.Length; r++)
                {
                    long total = 0;

                    foreach (Gesture gesture in gestures)
                    {
                        total += DistinctClusters(
                            gesture, graph, partitions[r], clusterIndex[r], stamp, ++generation);
                    }

                    Emit(report, shape, size, actual, $"per-cluster ({ClusterRungs[r]})", total,
                        gestures.Count, sheds.Count, anchor.BuildNs);
                }

                long ballTotal = 0;
                long pathTotal = 0;

                foreach (Gesture gesture in gestures)
                {
                    ballTotal += Distinct(gesture, ball, stamp, ++generation);
                    pathTotal += Distinct(gesture, paths, stamp, ++generation);
                }

                Emit(report, shape, size, actual, "per-Segment (ball)", ballTotal, gestures.Count,
                    sheds.Count, anchor.BuildNs);
                Emit(report, shape, size, actual, "per-Segment (paths)", pathTotal, gestures.Count,
                    sheds.Count, anchor.BuildNs);
            }
        }

        report.AppendLine();
        AppendVerdict(report, sheds, anchor, ball, paths, clusterIndex);
    }

    private static void Emit(
        StringBuilder report,
        GestureShape shape,
        int size,
        int actual,
        string rung,
        long total,
        int gestures,
        int sheds,
        long buildNs)
    {
        if (gestures == 0)
        {
            return;
        }

        long mean = total / gestures;
        long rebuild = mean * buildNs;
        long sharePermille = sheds == 0 ? 0 : (mean * 1_000) / sheds;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {Name(shape)} | {size} | {actual} | {rung} | {mean:N0} "
            + $"| {sharePermille / 10}.{sharePermille % 10}% | {Ms(rebuild)} "
            + $"| **{Percent(rebuild)}** |"));
    }

    private static void AppendVerdict(
        StringBuilder report,
        ParkingSheds sheds,
        ShedShape anchor,
        Index ball,
        Index paths,
        Index[] clusterIndex)
    {
        report.AppendLine("### What it costs to be able to ask");
        report.AppendLine();
        report.AppendLine(
            "**A rung is not free merely because it invalidates less.** Checking *did any of my "
            + "Segments move* needs a reverse index the shed would not otherwise hold, and that is the "
            + "column routes never had to pay.");
        report.AppendLine();

        report.AppendLine("| Rung | Reverse index | Resident |");
        report.AppendLine("|---|---:|---:|");

        for (int r = 0; r < ClusterRungs.Length; r++)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| per-cluster ({ClusterRungs[r]}) | {clusterIndex[r].Shed.LongLength:N0} entries "
                + $"| {Mib(clusterIndex[r].Shed.LongLength * 4)} |"));
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| per-Segment (paths) | {paths.Shed.LongLength:N0} entries "
            + $"| {Mib(paths.Shed.LongLength * 4)} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| per-Segment (ball) | {ball.Shed.LongLength:N0} entries "
            + $"| {Mib(ball.Shed.LongLength * 4)} |"));
        report.AppendLine();

        long globalRebuild = (long)sheds.Count * anchor.BuildNs;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The global rung is out, and the tripwire fired as written.** One deleted Segment "
            + $"anywhere invalidates all {sheds.Count:N0} sheds, and rebuilding them costs "
            + $"**{Ms(globalRebuild)} — {Percent(globalRebuild)} of a Tick.** `plans/0010` predicted "
            + $"it in words before this harness existed. **The number is worse than the sentence**, "
            + $"because the rebuild is paid *on arrival* — the moment a Trip is trying to finish — so "
            + $"it is not one stall but a stampede spread across every arriving vehicle. **`05 §3`'s "
            + $"*invalidated by the Road Graph Epoch* is owed the correction `CONTEXT.md` → Epoch "
            + $"already took**: the phrase says when the rebuild is paid, not how much survives, and "
            + $"under one counter the answer is none of it."));
        report.AppendLine();
    }

    private sealed record Index(int[] Start, int[] Shed);

    /// <summary>
    /// A Segment → sheds reverse index, built in two passes so no per-shed witness is retained.
    /// <b>Two passes rather than one because the alternative is holding every witness list at once</b>,
    /// which at this Building count is the largest allocation in the spike.
    /// </summary>
    private static Index BuildIndex(
        ParkingSheds sheds, ShedBuilder builder, int radius, int segments, bool useBall)
    {
        var start = new int[segments + 1];

        for (int building = 0; building < sheds.Count; building++)
        {
            builder.Build(sheds.AccessPointOf(building), radius);

            foreach (int segment in useBall ? builder.Ball : builder.Paths)
            {
                start[segment + 1]++;
            }
        }

        for (int i = 1; i <= segments; i++)
        {
            start[i] += start[i - 1];
        }

        var shed = new int[start[segments]];
        var cursor = new int[segments];

        for (int building = 0; building < sheds.Count; building++)
        {
            builder.Build(sheds.AccessPointOf(building), radius);

            foreach (int segment in useBall ? builder.Ball : builder.Paths)
            {
                shed[start[segment] + cursor[segment]++] = building;
            }
        }

        return new Index(start, shed);
    }

    /// <summary>
    /// The same, keyed on cluster. The witness is derived from the ball rather than tracked by the
    /// builder, which is what lets one build serve every cluster size.
    /// </summary>
    private static Index BuildClusterIndex(
        ParkingSheds sheds, ShedBuilder builder, int radius, RoadGraph graph, Clusters clusters)
    {
        var start = new int[clusters.Count + 1];
        var seen = new int[clusters.Count];
        int generation = 0;

        for (int building = 0; building < sheds.Count; building++)
        {
            builder.Build(sheds.AccessPointOf(building), radius);
            generation++;

            foreach (int segment in builder.Ball)
            {
                int cluster = clusters.OfNode[graph.SegmentNodeA[segment]];

                if (seen[cluster] == generation)
                {
                    continue;
                }

                seen[cluster] = generation;
                start[cluster + 1]++;
            }
        }

        for (int i = 1; i <= clusters.Count; i++)
        {
            start[i] += start[i - 1];
        }

        var shed = new int[start[clusters.Count]];
        var cursor = new int[clusters.Count];
        generation = 0;

        for (int building = 0; building < sheds.Count; building++)
        {
            builder.Build(sheds.AccessPointOf(building), radius);
            generation++;

            foreach (int segment in builder.Ball)
            {
                int cluster = clusters.OfNode[graph.SegmentNodeA[segment]];

                if (seen[cluster] == generation)
                {
                    continue;
                }

                seen[cluster] = generation;
                shed[start[cluster] + cursor[cluster]++] = building;
            }
        }

        return new Index(start, shed);
    }

    private static long Distinct(Gesture gesture, Index index, int[] stamp, int generation)
    {
        long distinct = 0;

        foreach (int segment in gesture.Segments)
        {
            for (int i = index.Start[segment]; i < index.Start[segment + 1]; i++)
            {
                if (stamp[index.Shed[i]] == generation)
                {
                    continue;
                }

                stamp[index.Shed[i]] = generation;
                distinct++;
            }
        }

        return distinct;
    }

    private static long DistinctClusters(
        Gesture gesture,
        RoadGraph graph,
        Clusters clusters,
        Index index,
        int[] stamp,
        int generation)
    {
        long distinct = 0;

        foreach (int segment in gesture.Segments)
        {
            for (int end = 0; end < 2; end++)
            {
                int node = end == 0 ? graph.SegmentNodeA[segment] : graph.SegmentNodeB[segment];
                int cluster = clusters.OfNode[node];

                for (int i = index.Start[cluster]; i < index.Start[cluster + 1]; i++)
                {
                    if (stamp[index.Shed[i]] == generation)
                    {
                        continue;
                    }

                    stamp[index.Shed[i]] = generation;
                    distinct++;
                }
            }
        }

        return distinct;
    }

    private static string Name(GestureShape shape) => shape switch
    {
        GestureShape.Drag => "drag",
        GestureShape.Scattered => "scattered",
        _ => "arterial",
    };

    private static long Elapsed(long start) =>
        (long)(Stopwatch.GetElapsedTime(start, Stopwatch.GetTimestamp()).Ticks * 100L);

    private static string Ns(long ns) => ns >= 1_000_000
        ? string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000_000}.{(ns / 10_000) % 100:D2} ms")
        : ns >= 1_000
            ? string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000}.{(ns / 10) % 100:D2} µs")
            : string.Create(CultureInfo.InvariantCulture, $"{ns} ns");

    private static string Ms(long ns) =>
        string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000_000}.{(ns / 1_000) % 1_000:D3} ms");

    private static string Mib(long bytes) => string.Create(
        CultureInfo.InvariantCulture,
        $"{bytes / (1024 * 1024)}.{((bytes * 100) / (1024 * 1024)) % 100:D2} MiB");

    private static string Percent(long ns) => string.Create(
        CultureInfo.InvariantCulture,
        $"{(ns * 100) / TickBudgetNanoseconds}.{((ns * 10_000) / TickBudgetNanoseconds) % 100:D2}%");

    private static string Ratio(long a, long b) => b == 0
        ? "—"
        : string.Create(CultureInfo.InvariantCulture, $"{a / b}.{((a * 100) / b) % 100:D2}×");
}
