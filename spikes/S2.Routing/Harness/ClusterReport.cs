using System.Diagnostics;
using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Cluster;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Harness;

/// <summary>
/// S2 R3 — HPA\*, and the cluster size it owns.
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0010</c> R3 decides <b>cluster size, outright</b>: it is <c>(derived AND rebuilt)</c>,
/// never written to a save, and therefore free to change forever, which is the whole of
/// <c>adr/0040</c>. Chunk size is informed and not decided.
/// </para>
/// <para>
/// <b>Wall-clock, never expansions saved.</b> R0's amendment to the plan, and it is not a stylistic
/// preference: <c>EuclideanFloor</c> expands 11% fewer nodes than <c>Chebyshev</c> and takes 1.8× as
/// long, and against plain Dijkstra it cuts expansions by 55% while being no faster at all. Nodes
/// expanded is the currency HPA\* results are conventionally quoted in, and on this graph the
/// currency does not convert. Expansions are printed here as a <i>work</i> column beside the clock,
/// never instead of it.
/// </para>
/// </remarks>
internal static class ClusterReport
{
    /// <summary>
    /// Chunks per cluster side. <b>Whole numbers, per <c>adr/0040</c></b>, and every rung tiles the
    /// 4096-Tile map exactly. The bottom rung is <c>adr/0014</c>'s literal claim — the cluster
    /// <i>is</i> the Chunk — and the sweep runs well past the answer, because a sweep that stops
    /// where the optimum is cannot show that it is one.
    /// </summary>
    private static readonly int[] ChunkRungs = [1, 2, 4, 8, 16, 32, 64];

    private const int TimingQueries = 1_000;
    private const int WarmupQueries = 200;
    private const int AuditSamples = 200;
    private const int EditSamples = 32;

    /// <summary>
    /// Transitions kept per cluster-pair boundary, <c>0</c> being all of them. Botea's lever, swept
    /// at the cluster rung R3.3 makes the best of.
    /// </summary>
    private static readonly (int Transitions, bool Reduced)[] TransitionRungs =
    [
        (1, false),
        (2, false),
        (4, false),
        (8, false),
        (0, false),
        (0, true),
    ];

    /// <summary>The cluster rung the transition sweep runs at. Chosen from R3.3, not in advance.</summary>
    private const int TransitionCluster = 16;
    private const int BypassQueries = 20_000;

    /// <summary>
    /// Trips starting per Tick, and the range around it. R2's figure, derived from ~56,000 Trips in
    /// flight — provisional, because the mean Trip duration it rests on is.
    /// </summary>
    private const int Arrivals = 550;
    private const int ArrivalsLow = 530;
    private const int ArrivalsHigh = 574;

    /// <summary>`CLAUDE.md`'s constant table: 15.6 ms at 4× speed.</summary>
    private const long TickBudgetNanoseconds = 15_600_000;

    /// <summary>A walk Leg is local. R0's radius, kept so the two tasks' walk figures compose.</summary>
    private const int WalkRadiusTiles = 400;

    /// <summary>O-D distance buckets, in Tiles. R0's, unchanged, for the same reason.</summary>
    private static readonly int[] BucketCeilings = [32, 64, 128, 256, 512, 1_024, 2_048, int.MaxValue];

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var reverse = ReverseArcs.Of(graph);

        report.AppendLine("## S2 R3 — HPA\\*, and the cluster size it owns");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: block {GraphParameters.Working.BlockTiles} Tiles, "
            + $"{GraphParameters.Working.ArterialCount} Arterials, {graph.Segments:N0} Segments, "
            + $"{graph.Nodes:N0} nodes, {graph.Arcs:N0} arcs. Free-flow car costs, `Chebyshev`, "
            + $"the query shape and the heuristic R0 published against."));
        report.AppendLine();
        report.AppendLine(
            "**Every figure below is wall-clock.** Expansions appear beside the clock as a work "
            + "column and never in place of it — R0 measured a case where the two disagree, and "
            + "*nodes expanded* is the currency HPA\\* results are conventionally quoted in.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The reverse index the goal insertion needs is {Bytes(reverse.ResidentBytes)} and is a "
            + $"property of the Road Graph rather than of the partition, so it is stated once here "
            + $"and kept out of every resident-size column below."));
        report.AppendLine();

        var queries = Draw(graph, Modes.Car, 0, TimingQueries);
        var warmQueries = Draw(graph, Modes.Car, 0, WarmupQueries);

        // The denominator is measured twice, once on either side of the sweep, and that is a
        // correction this task made to itself. The first pinned capture read 1,240,143 ns for the
        // flat search against 425,803 ns for the same code in the same configuration unpinned —
        // while every hierarchical rung was unmoved — because the flat loop was the first timed
        // thing in the process and the governor had not ramped. **Every ratio in R3.3 and R3.5
        // divides by this number**, so an artefact living in it decorates the whole task, which is
        // the shape R0's *"an argument for reporting a quantity you expect to be boring"* keeps
        // taking. Two passes make the drift visible rather than invisible.
        var flatFirst = MeasureFlat(graph, queries, warmQueries, out int[] flatCost);

        Warm(graph, reverse, warmQueries);

        // Both variants of every cluster rung, interleaved, because the reduction changes which
        // cluster size wins and R3 is the task that decides cluster size.
        var rungs = new List<Rung>();
        foreach (int chunks in ChunkRungs)
        {
            rungs.Add(Measure(graph, reverse, chunks, false, false, queries, warmQueries, flatCost));
            rungs.Add(Measure(graph, reverse, chunks, true, false, queries, warmQueries, flatCost));
            rungs.Add(Measure(graph, reverse, chunks, true, true, queries, warmQueries, flatCost));
        }

        var flat = MeasureFlat(graph, queries, warmQueries, out int[] flatCostAgain);

        int drifted = 0;
        for (int q = 0; q < flatCost.Length; q++)
        {
            if (flatCost[q] != flatCostAgain[q])
            {
                drifted++;
            }
        }

        AppendPartition(report, graph, rungs);
        AppendPreprocessing(report, rungs, flat);
        AppendQuery(report, rungs, flat, flatFirst, drifted);
        AppendBudget(report, rungs, flat);
        AppendOptimality(report, rungs);
        AppendTransitions(report, graph, reverse, queries, warmQueries, flatCost, flat);
        AppendInvalidation(report, rungs);
        AppendBypass(report, graph, reverse);

        return report.ToString();
    }

    // --- One rung's measurements ------------------------------------------------------------------

    private readonly record struct Rung(
        int Chunks,
        bool Reduced,
        bool Paths,
        long PathBytes,
        int TilesPerSide,
        int Clusters,
        int LargestCluster,
        int Portals,
        int Edges,
        long ResidentBytes,
        long BuildNanoseconds,
        long BuildSettled,
        long QueryNanoseconds,
        long RefineNanoseconds,
        long PortalsExpanded,
        long NodesExpanded,
        long EdgesRelaxed,
        long ArcsRelaxed,
        long RefinedArcs,
        int Found,
        int Optimal,
        int CheaperThanFlat,
        int MeanDetourHundredths,
        int WorstDetourHundredths,
        int Compared,
        long RepairNanoseconds,
        int RepairSpanHundredths,
        int Edits,
        int AuditFailures,
        int Audited);

    private static void Warm(RoadGraph graph, ReverseArcs reverse, (AccessPoint, AccessPoint)[] warm)
    {
        // The whole sweep is walked once before any of it is timed, and this is R1's finding rather
        // than a precaution. R1 needed four warm-up schemes before its cold-build column stopped
        // falling smoothly with District count — the shape a reader would most readily believe —
        // because the cost was per-process rather than per-rung: the small rungs never called the
        // kernel enough times to leave tier 0. R3 sweeps a partition too, and the board records it
        // as a finding R3 inherits.
        foreach (int chunks in ChunkRungs)
        {
            var clusters = Clusters.Partition(graph, chunks);
            var arcCost = (int[])graph.ArcCarTicks.Clone();
            var abstractGraph = AbstractGraph.Build(
                graph, clusters, reverse, arcCost,
                transitionsPerBoundary: 0, reduceIntra: true, storePaths: true);
            var search = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);
            var arcs = new List<int>();

            foreach ((AccessPoint origin, AccessPoint goal) in warm)
            {
                search.Run(origin, goal);
                arcs.Clear();
                search.Refine(arcs);
            }

            abstractGraph.Repair(0);
        }
    }

    private static Rung Measure(
        RoadGraph graph,
        ReverseArcs reverse,
        int chunks,
        bool reduced,
        bool paths,
        (AccessPoint Origin, AccessPoint Goal)[] queries,
        (AccessPoint Origin, AccessPoint Goal)[] warm,
        int[] flatCost)
    {
        var clusters = Clusters.Partition(graph, chunks);
        var arcCost = (int[])graph.ArcCarTicks.Clone();

        long buildStart = Stopwatch.GetTimestamp();
        var abstractGraph = AbstractGraph.Build(
            graph, clusters, reverse, arcCost,
            transitionsPerBoundary: 0, reduceIntra: reduced, storePaths: paths);
        long build = Nanoseconds(buildStart);

        var search = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);

        foreach ((AccessPoint origin, AccessPoint goal) in warm)
        {
            search.Run(origin, goal);
        }

        long portals = 0;
        long nodes = 0;
        long edges = 0;
        long relaxed = 0;
        int found = 0;
        int optimal = 0;
        int cheaper = 0;
        int compared = 0;
        long detourSum = 0;
        int worstDetour = 0;

        long queryStart = Stopwatch.GetTimestamp();
        foreach ((AccessPoint origin, AccessPoint goal) in queries)
        {
            var outcome = search.Run(origin, goal);
            portals += outcome.PortalsExpanded;
            nodes += outcome.NodesExpanded;
            edges += outcome.EdgesRelaxed;
            relaxed += outcome.ArcsRelaxed;
            found += outcome.Found ? 1 : 0;
        }

        long query = Nanoseconds(queryStart);

        // Correctness, out of the timed loop. The detour is the hierarchy's own error: HPA* returns
        // the best route that respects the partition, which is never cheaper than the flat optimum
        // and is sometimes dearer. R2 established the detour as this spike's correctness currency.
        for (int q = 0; q < queries.Length; q++)
        {
            var outcome = search.Run(queries[q].Origin, queries[q].Goal);
            if (!outcome.Found || flatCost[q] <= 0)
            {
                continue;
            }

            compared++;

            if (outcome.CostTicks < flatCost[q])
            {
                // Reported and expected to read zero. A hierarchy that returns a route cheaper than
                // the unconstrained optimum has not found a shortcut; it has a bug, and the column
                // it would otherwise hide in is the detour mean.
                cheaper++;
                continue;
            }

            if (outcome.CostTicks == flatCost[q])
            {
                optimal++;
                continue;
            }

            int over = (int)(((long)(outcome.CostTicks - flatCost[q]) * 10_000) / flatCost[q]);
            detourSum += over;
            worstDetour = over > worstDetour ? over : worstDetour;
        }

        // Refinement, timed apart: a cost alone is what the travel-time matrix already answers more
        // cheaply, so the figure that matters to adr/0041 is the one that comes back with arcs.
        //
        // **Over the same query set as the cost-only loop above, and that is a correction rather
        // than a nicety.** The first version of this harness refined a 200-query prefix and
        // subtracted the 1,000-query mean, which made refinement read *negative* at three rungs:
        // the prefix was cheaper than the whole, and the difference of two different samples was
        // being published as the cost of a step. Two loops over one set is the only form in which
        // the subtraction means anything.
        var arcs = new List<int>();
        long refinedArcs = 0;

        foreach ((AccessPoint origin, AccessPoint goal) in queries)
        {
            search.Run(origin, goal);
            arcs.Clear();
            search.Refine(arcs);
        }

        long refineStart = Stopwatch.GetTimestamp();
        foreach ((AccessPoint origin, AccessPoint goal) in queries)
        {
            search.Run(origin, goal);
            arcs.Clear();
            refinedArcs += search.Refine(arcs);
        }

        long refine = Nanoseconds(refineStart);

        int auditFailures = Audit(graph, reverse, search, queries, arcCost, out int audited);
        long repair = MeasureRepair(
            graph, abstractGraph, arcCost, reduced, out int spanHundredths, out int edits);

        return new Rung(
            Chunks: chunks,
            Reduced: reduced,
            Paths: paths,
            PathBytes: abstractGraph.PathBytes,
            TilesPerSide: clusters.TilesPerSide,
            Clusters: clusters.Count,
            LargestCluster: clusters.LargestCluster,
            Portals: abstractGraph.Portals,
            Edges: abstractGraph.Edges,
            ResidentBytes: abstractGraph.ResidentBytes,
            BuildNanoseconds: build,
            BuildSettled: abstractGraph.NodesSettled,
            QueryNanoseconds: query / queries.Length,
            RefineNanoseconds: (refine - query) / queries.Length,
            PortalsExpanded: portals / queries.Length,
            NodesExpanded: nodes / queries.Length,
            EdgesRelaxed: edges / queries.Length,
            ArcsRelaxed: relaxed / queries.Length,
            RefinedArcs: refinedArcs / queries.Length,
            Found: found,
            Optimal: optimal,
            CheaperThanFlat: cheaper,
            MeanDetourHundredths: compared == 0 ? 0 : (int)(detourSum / compared),
            WorstDetourHundredths: worstDetour,
            Compared: compared,
            RepairNanoseconds: repair,
            RepairSpanHundredths: spanHundredths,
            Edits: edits,
            AuditFailures: auditFailures,
            Audited: audited);
    }

    /// <summary>
    /// Walks a sample of refined routes and checks each one reconstitutes the cost that was reported
    /// for it.
    /// </summary>
    /// <remarks>
    /// <b>An invariant is worth printing on the run where it reads <i>yes</i>.</b> R2's next-hop rung
    /// published a peak <c>v/c</c> of 883× with every other column looking healthy, and the check
    /// that caught it had been specified in advance and simply not run. The analogue here is that a
    /// refinement and an abstract cost can disagree silently: the abstract search would keep
    /// returning a plausible number while the arcs handed to a Traveller went somewhere else. The
    /// check is that the arcs form a chain from the origin Segment to the goal Segment, and that the
    /// entry partial plus the arc costs plus the exit remainder equals the reported cost exactly —
    /// which Q16.16 addition makes an equality rather than a tolerance.
    /// </remarks>
    private static int Audit(
        RoadGraph graph,
        ReverseArcs reverse,
        HpaSearch search,
        (AccessPoint Origin, AccessPoint Goal)[] queries,
        int[] arcCost,
        out int audited)
    {
        var arcs = new List<int>();
        int failures = 0;
        audited = 0;

        int count = queries.Length < AuditSamples ? queries.Length : AuditSamples;

        for (int q = 0; q < count; q++)
        {
            (AccessPoint origin, AccessPoint goal) = queries[q];
            var outcome = search.Run(origin, goal);

            if (!outcome.Found || outcome.Bypass != Bypass.None)
            {
                continue;
            }

            arcs.Clear();
            if (search.Refine(arcs) == 0)
            {
                failures++;
                audited++;
                continue;
            }

            audited++;

            long total = 0;
            bool broken = false;

            for (int i = 0; i < arcs.Count; i++)
            {
                total += arcCost[arcs[i]];

                if (i > 0 && reverse.Source[arcs[i]] != graph.ArcTarget[arcs[i - 1]])
                {
                    broken = true;
                }
            }

            int entry = SegmentEntry.CostToEndpoint(
                graph, null, Modes.Car, origin, reverse.Source[arcs[0]]);
            int exit = SegmentEntry.CostFromEndpoint(
                graph, null, Modes.Car, graph.ArcTarget[arcs[^1]], goal);

            if (broken || entry >= SegmentEntry.Unreachable || exit >= SegmentEntry.Unreachable
                || total + entry + exit != outcome.CostTicks)
            {
                failures++;
            }
        }

        return failures;
    }

    private static long MeasureRepair(
        RoadGraph graph,
        AbstractGraph abstractGraph,
        int[] arcCost,
        bool reduced,
        out int spanHundredths,
        out int edits)
    {
        // The edit is a deletion, and deletion is the half of the core verb an in-place repair can
        // price. Drawing a road across a boundary creates a portal, which needs a slot the build
        // did not reserve — that is R5's, and AbstractGraph.Repair says so where it lives.
        var sampled = new List<int>();
        int stride = graph.Segments / EditSamples;

        for (int i = 0; i < EditSamples; i++)
        {
            int segment = (i * stride) % graph.Segments;
            if ((graph.SegmentModes[segment] & (byte)Modes.Car) != 0)
            {
                sampled.Add(segment);
            }
        }

        edits = sampled.Count;
        if (edits == 0)
        {
            spanHundredths = 0;
            return 0;
        }

        long span = 0;
        foreach (int segment in sampled)
        {
            span += abstractGraph.RepairSpan(segment);
        }

        spanHundredths = (int)((span * 100) / edits);

        // Warm, then timed. Each edit is applied, repaired, reverted and repaired again, so every
        // rung starts and ends on the same abstract graph and the timed figure is one repair.
        // **Which operation is sound depends on the rung, and that is the finding rather than a
        // harness detail.** A complete abstract graph keeps every intra-edge, so re-costing the
        // slots is exact. A reduced one removed edges whose redundancy is a property of the costs,
        // so an edit can make a removed edge necessary again and the cluster's edge set has to be
        // decided again — which is what RebuildFor does, and what this then measures. An earlier
        // draft of R3 derived that figure from the per-cluster build column instead of measuring it.
        foreach (int segment in sampled)
        {
            Damage(graph, arcCost, segment, true);
            Mend(abstractGraph, segment, reduced);
            Damage(graph, arcCost, segment, false);
            Mend(abstractGraph, segment, reduced);
        }

        long total = 0;

        foreach (int segment in sampled)
        {
            Damage(graph, arcCost, segment, true);

            long start = Stopwatch.GetTimestamp();
            Mend(abstractGraph, segment, reduced);
            total += Nanoseconds(start);

            Damage(graph, arcCost, segment, false);
            Mend(abstractGraph, segment, reduced);
        }

        return total / edits;
    }

    private static void Mend(AbstractGraph abstractGraph, int segment, bool reduced)
    {
        if (reduced)
        {
            abstractGraph.RebuildFor(segment);
            return;
        }

        abstractGraph.Repair(segment);
    }

    private static void Damage(RoadGraph graph, int[] arcCost, int segment, bool delete)
    {
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (graph.ArcSegment[arc] == segment)
            {
                arcCost[arc] = delete ? RoadGraph.Impassable : graph.ArcCarTicks[arc];
            }
        }
    }

    // --- The flat search this is all divided by ---------------------------------------------------

    private readonly record struct Flat(
        long Nanoseconds, long Expanded, long Relaxed, long Segments, int Found);

    private static Flat MeasureFlat(
        RoadGraph graph,
        (AccessPoint Origin, AccessPoint Goal)[] queries,
        (AccessPoint Origin, AccessPoint Goal)[] warm,
        out int[] cost)
    {
        // R0's denominator, re-measured in this process on this query set. Quoting R0's published
        // number instead would compare two processes' JIT states, and the ratio HPA* is judged by is
        // exactly the quantity that error would land in.
        var search = new PointToPoint(graph);

        foreach ((AccessPoint origin, AccessPoint goal) in warm)
        {
            search.Bootstrap(origin, goal, Modes.Car, HeuristicKind.Chebyshev);
            search.Expand();
        }

        cost = new int[queries.Length];
        long expanded = 0;
        long relaxed = 0;
        long segments = 0;
        int found = 0;

        long start = Stopwatch.GetTimestamp();
        for (int q = 0; q < queries.Length; q++)
        {
            search.Bootstrap(queries[q].Origin, queries[q].Goal, Modes.Car, HeuristicKind.Chebyshev);
            var outcome = search.Expand();
            cost[q] = outcome.Found ? outcome.CostTicks : 0;
            expanded += outcome.NodesExpanded;
            relaxed += outcome.ArcsRelaxed;
            segments += outcome.PathSegments;
            found += outcome.Found ? 1 : 0;
        }

        long elapsed = Nanoseconds(start);

        return new Flat(
            elapsed / queries.Length,
            expanded / queries.Length,
            relaxed / queries.Length,
            segments / queries.Length,
            found);
    }

    // --- Sections ---------------------------------------------------------------------------------

    private static void AppendPartition(StringBuilder report, RoadGraph graph, List<Rung> rungs)
    {
        report.AppendLine("### R3.1 — the partition, and what `adr/0014` was claiming");
        report.AppendLine();
        report.AppendLine(
            "| Chunks | Cluster | Clusters | Largest | Portals | Portals each | Abstract edges | Resident |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var rung in rungs)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Row(rung)} | {rung.TilesPerSide:N0} Tiles | {rung.Clusters:N0} "
                + $"| {rung.LargestCluster:N0} nodes | {rung.Portals:N0} "
                + $"| {(rung.Clusters == 0 ? 0 : rung.Portals / rung.Clusters):N0} "
                + $"| {rung.Edges:N0} | {Bytes(rung.ResidentBytes)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"*Largest* is the node count of the fullest cluster — the bound on one insertion "
            + $"search, and the reason the query column below turns back up. The graph has "
            + $"{graph.Nodes:N0} nodes, so the portal column is also the share of the network the "
            + $"abstract graph re-describes."));
        report.AppendLine();
        report.AppendLine(
            "**The bottom rung is `adr/0014`'s claim taken literally** — *\"the Chunk grid is "
            + "already the pathfinding cluster\"* — at Phase 1's provisional Chunk = Cell. "
            + "`05 §5` predicted the pathfinding role wants *larger, and loudly*, at 32×32.");
        report.AppendLine();
    }

    private static void AppendPreprocessing(StringBuilder report, List<Rung> rungs, Flat flat)
    {
        report.AppendLine("### R3.2 — preprocessing, in flat searches");
        report.AppendLine();
        report.AppendLine(
            "Priced in flat searches rather than in milliseconds alone, because that is the "
            + "question: preprocessing is only affordable if the queries it saves outnumber it. "
            + "`adr/0040` makes the abstract graph `(derived AND rebuilt)`, so this cost is paid on "
            + "every load and after every change to the cluster size — never amortised into a save.");
        report.AppendLine();
        report.AppendLine("| Chunks | Cold build | Nodes settled | Per cluster | In flat searches |");
        report.AppendLine("|---:|---:|---:|---:|---:|");

        foreach (var rung in rungs)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Row(rung)} | {Milliseconds(rung.BuildNanoseconds)} | {rung.BuildSettled:N0} "
                + $"| {(rung.Clusters == 0 ? 0 : rung.BuildNanoseconds / rung.Clusters):N0} ns "
                + $"| {(flat.Nanoseconds == 0 ? 0 : rung.BuildNanoseconds / flat.Nanoseconds):N0} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"One flat `Chebyshev` drive search in this process: **{flat.Nanoseconds:N0} ns**, "
            + $"{flat.Expanded:N0} nodes expanded, {flat.Segments:N0} path Segments."));
        report.AppendLine();
    }

    private static void AppendQuery(
        StringBuilder report, List<Rung> rungs, Flat flat, Flat first, int drifted)
    {
        report.AppendLine("### R3.3 — the query, which is the column R3 exists for");
        report.AppendLine();
        report.AppendLine(
            "*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. "
            + "They are timed apart because they have different customers: R1 showed the "
            + "travel-time matrix already answers the first more cheaply than any search can, and "
            + "`adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular "
            + "Traveller in flight.");
        report.AppendLine();
        report.AppendLine(
            "| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **flat** | **{flat.Nanoseconds:N0} ns** | 1.00× | — | — | {flat.Expanded:N0} nodes "
            + $"| {flat.Relaxed:N0} arcs | {flat.Segments:N0} |"));

        foreach (var rung in rungs)
        {
            long refined = rung.QueryNanoseconds + rung.RefineNanoseconds;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Row(rung)} | {rung.QueryNanoseconds:N0} ns "
                + $"| {Ratio(flat.Nanoseconds, rung.QueryNanoseconds)} "
                + $"| {refined:N0} ns | {Ratio(flat.Nanoseconds, refined)} "
                + $"| {rung.PortalsExpanded:N0} + {rung.NodesExpanded:N0} "
                + $"| {rung.EdgesRelaxed:N0} + {rung.ArcsRelaxed:N0} | {rung.RefinedArcs:N0} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges "
            + "relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two "
            + "insertions. **The two halves are what the clock column is made of, and they move in "
            + "opposite directions** — a larger cluster means fewer portals and more insertion.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{TimingQueries:N0} drive queries per rung, drawn once and shared by every rung and by "
            + $"the flat search, and **the refined column is a second pass over the same set** "
            + $"rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's "
            + $"entry-error table published a row built from nine searches beside rows built from "
            + $"two thousand, because its sampler shrank with the swept axis."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The denominator is measured twice, on either side of the sweep, and the ratios "
            + $"divide by the second.** First pass **{first.Nanoseconds:N0} ns**, second "
            + $"**{flat.Nanoseconds:N0} ns** — a spread of "
            + $"{Share(first.Nanoseconds - flat.Nanoseconds, flat.Nanoseconds)}. The first pinned "
            + $"capture of this task read 1,240,143 ns against 425,803 ns for the same code "
            + $"unpinned while every hierarchical rung stood still, because the flat loop was the "
            + $"first timed thing in the process and the clock had not ramped. Every ratio here "
            + $"divides by this number, so it is the one place an artefact would decorate the whole "
            + $"task. The second pass is quoted because the rungs are all measured after the warm "
            + $"sweep and share its process state; the first does not."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The two passes returned **{drifted}** differing route costs out of "
            + $"{TimingQueries:N0} — printed because it must read zero. The same query set over the "
            + $"same graph is the same search, and a non-zero here would mean the flat baseline "
            + $"every correctness column is judged against had moved underneath them."));
        report.AppendLine();
    }

    /// <summary>
    /// R2's tripwire, fired at R3's own result.
    /// </summary>
    /// <remarks>
    /// <b>R2 retired a whole rung with one line of arithmetic and R3 did not fire it at itself.</b>
    /// The searched rung went out because <i>"one Leg costs 716,800 ns against 530–574 arrivals per
    /// Tick, which is ~400 ms of searching per 15.6 ms of Tick budget"</i>. That test was written
    /// down before R3 ran, applies unchanged to any per-Trip search, and belongs in the harness
    /// rather than in a reader's head — which is the same argument the corpus keeps making about
    /// invariants nobody printed.
    /// </remarks>
    private static void AppendBudget(StringBuilder report, List<Rung> rungs, Flat flat)
    {
        report.AppendLine("### R3.4 — the Tick budget, which is the test R2 already wrote down");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — "
            + $"*one Leg against {ArrivalsLow}–{ArrivalsHigh} arrivals per Tick, ~400 ms of searching "
            + $"per {Milliseconds(TickBudgetNanoseconds)} of Tick budget* — and that test applies "
            + $"unchanged to **any** per-Trip search, including this one. A route must cost "
            + $"**{TickBudgetNanoseconds / Arrivals:N0} ns** to consume the whole budget on its own — "
            + $"or, put the way that depends on nothing derived, **routing fits only while fewer "
            + $"Trips start per Tick than the break-even column below.**"));
        report.AppendLine();
        report.AppendLine(
            "| Rung | Per route | **Break-even Trips/Tick** | At the working 550 | Fits |");
        report.AppendLine("|---|---:|---:|---:|---|");

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **flat** | {flat.Nanoseconds:N0} ns | **{TickBudgetNanoseconds / flat.Nanoseconds}** "
            + $"| {Milliseconds(flat.Nanoseconds * Arrivals)} "
            + $"| {Ratio(flat.Nanoseconds * Arrivals, TickBudgetNanoseconds)} over |"));

        foreach (var rung in rungs)
        {
            if (!rung.Paths)
            {
                continue;
            }

            long route = rung.QueryNanoseconds + rung.RefineNanoseconds;
            long load = route * Arrivals;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Row(rung)} | {route:N0} ns "
                + $"| **{TickBudgetNanoseconds / route}** | {Milliseconds(load)} "
                + $"| {Ratio(load, TickBudgetNanoseconds)} over |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The break-even column is the finding; the two columns right of it are a marker on "
            + $"it.** *Break-even Trips/Tick* is a measured per-route cost divided by a world "
            + $"constant and contains nothing derived — it stays true when the arrival rate is "
            + $"finally measured. **{Arrivals} is not measured and cannot be measured here**: it "
            + $"comes from ~56,000 Trips in flight, which rests on a mean Trip duration the corpus "
            + $"records as provisional, and S2 has no Travellers, no Trip generation and no Event "
            + $"Wheel to produce one. A tripwire whose denominator is a guess is a tripwire that can "
            + $"fire on the guess, so this one is stated in the form that does not depend on it."));
        report.AppendLine();
        report.AppendLine(
            "**No cluster size fits, and the shape of the curve says none can.** The load is U-shaped "
            + "in cluster size and both ends are pinned by the same thing: a small cluster makes the "
            + "abstract search approach the flat search, a large one makes the *insertion* approach "
            + "it. `adr/0040` admits only whole-Chunk clusters that tile the map, so the admissible "
            + "rungs are the divisors of 128 and the minimum sits at one of them with its two "
            + "neighbours worse. **This is a floor, not a rung that was missed.**");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Two exits, and neither is free.** A **cache** — `adr/0012` permits one keyed by "
            + $"origin-destination pair, and `plans/0010` R6 owns it — would have to reach roughly a "
            + $"**{HitRateNeeded(rungs):N0}% hit rate** to fit routing into half a Tick at the best "
            + $"rung. **That makes R6 load-bearing rather than an optimisation.** Or **threads**: "
            + $"invariant 4 is thread-count equivalence, so the best rung's load spread over eight "
            + $"cores fits — by spending the whole Tick budget of eight cores on routing, which is a "
            + $"mortgage rather than a solution."));
        report.AppendLine();
        report.AppendLine(
            "**R2's next-hop table is the rung this arithmetic does not touch**, because it does no "
            + "per-Trip search at all — 0 ns to start a Trip and 32 ns per crossing. That is a "
            + "structural advantage over both hierarchies rather than a faster constant, and it is "
            + "**R4's** to press.");
        report.AppendLine();
    }

    private static int HitRateNeeded(List<Rung> rungs)
    {
        long best = long.MaxValue;
        foreach (var rung in rungs)
        {
            long route = rung.QueryNanoseconds + rung.RefineNanoseconds;
            if (rung.Paths && route < best)
            {
                best = route;
            }
        }

        // Searches affordable in half a Tick, as a share of the arrivals that want one.
        long affordable = (TickBudgetNanoseconds / 2) / best;
        return (int)(((Arrivals - affordable) * 100) / Arrivals);
    }

    private static void AppendOptimality(StringBuilder report, List<Rung> rungs)
    {
        report.AppendLine("### R3.5 — the detour, because a different route is a different city");
        report.AppendLine();
        report.AppendLine(
            "HPA\\* returns the best route that **respects the partition**, which is never cheaper "
            + "than the flat optimum and is sometimes dearer. R2 established the detour as this "
            + "spike's correctness currency and measured 18.52% for a next-hop table against 36.01% "
            + "for a shared District route; those are the figures this column stands beside. "
            + "**The mean is over every query compared, optimal ones included at zero**, which is "
            + "what makes it the same quantity R2 published rather than a mean over survivors.");
        report.AppendLine();
        report.AppendLine(
            "| Chunks | Optimal | Mean detour | Worst detour | Cheaper than flat | Compared | Audited | Audit failures |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var rung in rungs)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Row(rung)} | {Percent(rung.Optimal, rung.Compared)} "
                + $"| {Hundredths(rung.MeanDetourHundredths)}% "
                + $"| {Hundredths(rung.WorstDetourHundredths)}% "
                + $"| {rung.CheaperThanFlat} | {rung.Compared:N0} | {rung.Audited:N0} "
                + $"| {rung.AuditFailures} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**Two columns here are printed to read zero.** *Cheaper than flat* would mean the "
            + "hierarchy had found a route the unconstrained search could not, which is not a "
            + "shortcut but a bug — and its natural hiding place is the detour mean. *Audit "
            + "failures* re-walks a sample of refined routes and requires the entry partial, the "
            + "arc costs and the exit remainder to sum **exactly** to the cost the query reported, "
            + "with the arcs forming an unbroken chain. R2's harness published a peak `v/c` of 883× "
            + "with every other column healthy, and the check that would have caught it had been "
            + "specified in advance and not run.");
        report.AppendLine();
    }

    /// <summary>
    /// The other lever HPA\* has, swept at the cluster size R3.3 makes the best of.
    /// </summary>
    /// <remarks>
    /// <b>This section exists because R3.4 read zero at every rung.</b> A correctness column that
    /// cannot move is not evidence that the thing it measures is absent — it may be evidence that
    /// the experiment removed the difference, which is the defect R2 caught in its own harness when
    /// two rungs reported byte-identical peaks. Sampling the transitions is the standard HPA\*
    /// lever and it is the one that makes the abstraction lossy, so a detour appearing here is what
    /// shows the zero above was a property of the design rather than of the instrument.
    /// </remarks>
    private static void AppendTransitions(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        (AccessPoint Origin, AccessPoint Goal)[] queries,
        (AccessPoint Origin, AccessPoint Goal)[] warm,
        int[] flatCost,
        Flat flat)
    {
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"### R3.6 — the sparser abstraction, at {TransitionCluster} Chunks"));
        report.AppendLine();
        report.AppendLine(
            "**Keeping every crossing is what makes the abstraction complete, and completeness is "
            + "what R3.5's zero is made of.** Botea's HPA\\* keeps one or two transitions per "
            + "entrance and accepts a detour for it. This is that lever, swept at the cluster size "
            + "R3.3 makes the best of — chosen from the measurement rather than in advance.");
        report.AppendLine();
        report.AppendLine(
            "| Transitions | Portals | Abstract edges | Edges each | Cold build | Query | vs flat | Optimal | Mean detour | Worst |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        var clusters = Clusters.Partition(graph, TransitionCluster);

        // Warm over the whole transition sweep before timing any of it, for R1's reason: the axis
        // being swept is exactly the axis a per-process warm-up defect would decorate.
        foreach ((int transitions, bool reduced) in TransitionRungs)
        {
            var warmArcCost = (int[])graph.ArcCarTicks.Clone();
            var warmGraph = AbstractGraph.Build(
                graph, clusters, reverse, warmArcCost, transitions, reduced);
            var warmSearch = new HpaSearch(graph, clusters, warmGraph, reverse, warmArcCost);

            foreach ((AccessPoint origin, AccessPoint goal) in warm)
            {
                warmSearch.Run(origin, goal);
            }
        }

        foreach ((int transitions, bool reduced) in TransitionRungs)
        {
            var arcCost = (int[])graph.ArcCarTicks.Clone();

            long buildStart = Stopwatch.GetTimestamp();
            var abstractGraph = AbstractGraph.Build(
                graph, clusters, reverse, arcCost, transitions, reduced);
            long build = Nanoseconds(buildStart);

            var search = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);

            foreach ((AccessPoint origin, AccessPoint goal) in warm)
            {
                search.Run(origin, goal);
            }

            long start = Stopwatch.GetTimestamp();
            foreach ((AccessPoint origin, AccessPoint goal) in queries)
            {
                search.Run(origin, goal);
            }

            long elapsed = Nanoseconds(start) / queries.Length;

            int optimal = 0;
            int compared = 0;
            int worst = 0;
            long detourSum = 0;

            for (int q = 0; q < queries.Length; q++)
            {
                var outcome = search.Run(queries[q].Origin, queries[q].Goal);
                if (!outcome.Found || flatCost[q] <= 0)
                {
                    continue;
                }

                compared++;

                if (outcome.CostTicks <= flatCost[q])
                {
                    optimal++;
                    continue;
                }

                int over = (int)(((long)(outcome.CostTicks - flatCost[q]) * 10_000) / flatCost[q]);
                detourSum += over;
                worst = over > worst ? over : worst;
            }

            int mean = compared == 0 ? 0 : (int)(detourSum / compared);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Label(transitions, reduced)} "
                + $"| {abstractGraph.Portals:N0} | {abstractGraph.Edges:N0} "
                + $"| {(abstractGraph.Portals == 0 ? 0 : abstractGraph.Edges / abstractGraph.Portals):N0} "
                + $"| {Milliseconds(build)} | {elapsed:N0} ns "
                + $"| {Ratio(flat.Nanoseconds, elapsed)} | {Percent(optimal, compared)} "
                + $"| {Hundredths(mean)}% | {Hundredths(worst)}% |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{queries.Length:N0} queries per rung, the same set R3.3 uses. *Edges each* is the "
            + $"abstract graph's mean degree, and the flat graph's is "
            + $"{(flat.Expanded == 0 ? 0 : flat.Relaxed / flat.Expanded):N0} — **the comparison this "
            + $"whole section exists to make.**"));
        report.AppendLine();
    }

    private static string Row(Rung rung) => string.Create(
        CultureInfo.InvariantCulture,
        $"{rung.Chunks}{(rung.Reduced ? ", reduced" : string.Empty)}{(rung.Paths ? " + paths" : string.Empty)}");

    private static string Label(int transitions, bool reduced) =>
        (transitions == 0 ? "all" : transitions.ToString(CultureInfo.InvariantCulture))
        + (reduced ? ", reduced" : string.Empty);

    private static void AppendInvalidation(StringBuilder report, List<Rung> rungs)
    {
        report.AppendLine("### R3.7 — invalidation, which is the half of the core verb R3 can price");
        report.AppendLine();
        report.AppendLine(
            "One Segment deleted. Only the clusters holding that Segment's endpoints can have "
            + "changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which "
            + "is this plan's own argument against distance-vector without sequence numbers, and it "
            + "cuts at a hierarchy too.");
        report.AppendLine();
        report.AppendLine(
            "| Rung | Operation | Cost | Clusters touched | Share of cold build | Edits in one build |");
        report.AppendLine("|---|---|---:|---:|---:|---:|");

        foreach (var rung in rungs)
        {

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Row(rung)} | {(rung.Reduced ? "rebuild cluster" : "re-cost")} "
                + $"| {rung.RepairNanoseconds:N0} ns "
                + $"| {Hundredths(rung.RepairSpanHundredths)} "
                + $"| {Share(rung.RepairNanoseconds, rung.BuildNanoseconds)} "
                + $"| {(rung.RepairNanoseconds == 0 ? 0 : rung.BuildNanoseconds / rung.RepairNanoseconds):N0} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**Two operations, and which one is sound is a property of the rung.** A complete "
            + "abstract graph keeps every intra-edge, so *re-costing* the slots is exact. A reduced "
            + "one removed edges whose redundancy is a property of the costs, so an edit can make a "
            + "removed edge necessary again — no amount of re-costing brings it back, and the "
            + "cluster's edge set must be **decided again**. That is the cost the recommended "
            + "configuration actually pays on an edit, and R3 measures it rather than deriving it "
            + "from the per-cluster build column, which is what an earlier draft did.");
        report.AppendLine();
        report.AppendLine(
            "**The rebuild column below 8 Chunks is mostly this harness and should not be read as a "
            + "property of the design.** A rebuilt cluster's edge list is spliced back into one "
            + "global CSR — kept global so the query path measured above is the one a real "
            + "implementation would run — and the splice copies every edge in the graph. At 16 "
            + "Chunks that is 11,768 edges and a couple of percent; at one Chunk it is 64,134 edges "
            + "plus a shift of 16,694 portal offsets, and it is most of the 469 µs. Per-cluster edge "
            + "lists would remove it and would cost the query an indirection per portal expanded.");
        report.AppendLine();
        report.AppendLine(
            "**Deletion only, and the limit is structural rather than an omission.** Both operations "
            + "work over the portals the build found, so either may cost an edge out of existence "
            + "but neither can create a portal that did not exist — which is what *drawing* a road "
            + "across a cluster boundary does. R5's edit storm is where the drawing half belongs.");
        report.AppendLine();
    }

    private static void AppendBypass(StringBuilder report, RoadGraph graph, ReverseArcs reverse)
    {
        report.AppendLine("### R3.8 — the bypass, and how local a walk Leg actually is");
        report.AppendLine();
        report.AppendLine(
            "`plans/0010` makes the same-Segment and adjacent-Segment bypass **mandatory rather "
            + "than an optimisation**: with five Buildings on a Segment, a share of walk Legs never "
            + "leave their own Segment or its neighbour, and routing those through the abstract "
            + "graph costs more than the answer.");
        report.AppendLine();
        report.AppendLine(
            "**The share per Tick is not measurable here, and reporting one would be a guess "
            + "wearing a measurement's clothes.** S2 has no Leg distribution — R0 said so in its "
            + "own sampler and bucketed everything instead, so that whatever distribution arrives "
            + "later is applied as *weights over buckets that already exist*. This table is that "
            + "bucketing. Read it as: *of Legs whose origin and destination are this far apart, "
            + "this share never enters the hierarchy.*");
        report.AppendLine();

        var clusters = Clusters.Partition(graph, 8);
        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var abstractGraph = AbstractGraph.Build(graph, clusters, reverse, arcCost);
        var search = new HpaSearch(graph, clusters, abstractGraph, reverse, arcCost);

        AppendBypassTable(report, graph, search, Modes.Foot, WalkRadiusTiles, "walk");
        AppendBypassTable(report, graph, search, Modes.Car, 0, "drive");

        report.AppendLine(
            "**The bypass is a property of the query, not of the cluster size**, so it is measured "
            + "at one rung and applies to every row of R3.1. What it costs to *decide* is two "
            + "Segment-id comparisons and, at most, four endpoint comparisons.");
        report.AppendLine();
    }

    private static void AppendBypassTable(
        StringBuilder report, RoadGraph graph, HpaSearch search, Modes mode, int radius, string name)
    {
        var sampler = new OdSampler(graph);
        var same = new int[BucketCeilings.Length];
        var adjacent = new int[BucketCeilings.Length];
        var total = new int[BucketCeilings.Length];
        ulong seed = graph.Parameters.Seed;

        for (int q = 0; q < BypassQueries; q++)
        {
            var origin = sampler.Origin(seed, (ulong)q, mode);
            var goal = sampler.Destination(seed, (ulong)q, mode, origin, radius);

            int bucket = Bucket(sampler.StraightLineTiles(origin.Segment, goal.Segment));
            total[bucket]++;

            switch (search.BypassFor(origin, goal, mode))
            {
                case Bypass.SameSegment:
                    same[bucket]++;
                    break;
                case Bypass.AdjacentSegment:
                    adjacent[bucket]++;
                    break;
                default:
                    break;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**{name}**, {BypassQueries:N0} drawn Legs."));
        report.AppendLine();
        report.AppendLine("| O-D distance | Legs | Same Segment | Adjacent | Bypassed |");
        report.AppendLine("|---|---:|---:|---:|---:|");

        for (int bucket = 0; bucket < BucketCeilings.Length; bucket++)
        {
            if (total[bucket] == 0)
            {
                continue;
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {BucketName(bucket)} | {total[bucket]:N0} "
                + $"| {Percent(same[bucket], total[bucket])} "
                + $"| {Percent(adjacent[bucket], total[bucket])} "
                + $"| {Percent(same[bucket] + adjacent[bucket], total[bucket])} |"));
        }

        report.AppendLine();
    }

    // --- Shared scaffolding -----------------------------------------------------------------------

    private static (AccessPoint Origin, AccessPoint Goal)[] Draw(
        RoadGraph graph, Modes mode, int radius, int count)
    {
        var sampler = new OdSampler(graph);
        var queries = new (AccessPoint, AccessPoint)[count];
        ulong seed = graph.Parameters.Seed;

        for (int q = 0; q < count; q++)
        {
            var origin = sampler.Origin(seed, (ulong)q, mode);
            queries[q] = (origin, sampler.Destination(seed, (ulong)q, mode, origin, radius));
        }

        return queries;
    }

    private static int Bucket(int tiles)
    {
        for (int i = 0; i < BucketCeilings.Length; i++)
        {
            if (tiles <= BucketCeilings[i])
            {
                return i;
            }
        }

        return BucketCeilings.Length - 1;
    }

    private static string BucketName(int bucket) => bucket switch
    {
        0 => "≤ 32 Tiles (one block)",
        1 => "≤ 64",
        2 => "≤ 128",
        3 => "≤ 256 (1 km)",
        4 => "≤ 512 (2 km)",
        5 => "≤ 1,024 (4 km)",
        6 => "≤ 2,048 (8 km)",
        _ => "> 2,048",
    };

    // Integer formatting throughout. BOR0201 stays an error in Harness/ as well — the one rule the
    // spike's prerequisites name by hand — and every figure here is a ratio of two integers anyway.

    private static long Nanoseconds(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1_000_000_000 / Stopwatch.Frequency;

    private static string Percent(int part, int whole) =>
        whole == 0 ? "—" : Hundredths((int)(((long)part * 10_000) / whole)) + "%";

    private static string Share(long part, long whole) =>
        whole == 0 ? "—" : Hundredths((int)((part * 10_000) / whole)) + "%";

    private static string Hundredths(int value) => string.Create(
        CultureInfo.InvariantCulture, $"{value / 100}.{IntegerMath.Abs(value % 100):D2}");

    private static string Ratio(long numerator, long denominator) =>
        denominator == 0 ? "—" : Hundredths((int)(numerator * 100 / denominator)) + "×";

    private static string Milliseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10_000)) + " ms";

    private static string Bytes(long bytes) =>
        bytes < 1024 ? bytes + " B"
        : bytes < 1024 * 1024 ? Hundredths((int)(bytes * 100 / 1024)) + " KiB"
        : bytes < 1024L * 1024 * 1024 ? Hundredths((int)(bytes * 100 / (1024 * 1024))) + " MiB"
        : Hundredths((int)(bytes * 100 / (1024L * 1024 * 1024))) + " GiB";
}
