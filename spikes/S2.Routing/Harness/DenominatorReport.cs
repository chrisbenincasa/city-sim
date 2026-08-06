using System.Diagnostics;
using System.Globalization;
using System.Text;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Harness;

/// <summary>
/// R0's second deliverable: the uncached point-to-point denominator, its own quality, and the
/// heuristic ladder's admissibility verdict against Arterial density.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every ratio S2 publishes divides by the denominator, so the denominator's quality is reported
/// beside it.</b> <c>plans/0010</c> R0: <i>"If A* expands within a few percent of Dijkstra, S2 says
/// so beside every ratio it publishes — a weak denominator flatters HPA*'s speedup, the cache's
/// value and R2's crossover alike."</i> That is S4's lesson one level up: a denominator needs its
/// machine, its moment <i>and</i> its quality.
/// </para>
/// <para>
/// <b>The column that decides is not speed.</b> It is how often the returned path is not optimal
/// against Dijkstra ground truth on the same query — because a non-optimal path is a different Trip
/// and therefore a different city, and <c>05 §4</c>'s test makes that a design change rather than a
/// tuning knob.
/// </para>
/// </remarks>
internal static class DenominatorReport
{
    private const int TimingQueries = 2_000;
    private const int LadderQueries = 300;
    private const int WarmupQueries = 500;

    /// <summary>A walk Leg is local. 400 Tiles is 1.6 km, which is a long walk and a short drive.</summary>
    private const int WalkRadiusTiles = 400;

    private static readonly HeuristicKind[] Rungs =
    [
        HeuristicKind.None,
        HeuristicKind.Manhattan,
        HeuristicKind.Octile,
        HeuristicKind.Chebyshev,
        HeuristicKind.EuclideanFloor,
    ];

    private static readonly int[] ArterialRungs = [0, 2, 4, 8, 16, 32];

    /// <summary>
    /// The rung every other figure in S2 is published against.
    /// </summary>
    /// <remarks>
    /// <b>Chebyshev, and it is a measured choice rather than the obvious one.</b> Two rungs are
    /// admissible on any graph and the tighter of them is <c>EuclideanFloor</c> — which expands 11%
    /// fewer nodes and takes 1.7× as long, because its exact integer square root costs more than the
    /// expansions it saves. On this graph <c>EuclideanFloor</c> A* is not faster than plain Dijkstra
    /// in wall-clock at all, having traded a 55% cut in expansions for a per-expansion cost that ate
    /// it. Manhattan is faster than both and returns non-optimal routes as soon as one Arterial
    /// exists, which under <c>05 §4</c> is a different city rather than a faster router.
    /// </remarks>
    private const HeuristicKind Denominator = HeuristicKind.Chebyshev;

    /// <summary>O-D distance buckets, in Tiles. A Tile is ~4 m, so these are 1, 2, 4, 8 and 16 km.</summary>
    private static readonly int[] BucketCeilings = [256, 512, 1_024, 2_048, 4_096, int.MaxValue];

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);

        report.AppendLine("## S2 R0 — the denominator, and the heuristic ladder");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: block {GraphParameters.Working.BlockTiles} Tiles, " +
            $"{GraphParameters.Working.ArterialCount} Arterials, {graph.Segments:N0} Segments, " +
            $"{graph.Nodes:N0} nodes, {graph.Arcs:N0} arcs."));
        report.AppendLine();
        report.AppendLine("Cost is time, Q16.16 Ticks. The query is `(Segment, offset) → (Segment, offset)`,");
        report.AppendLine("seeded from both endpoints of the origin Segment and terminated on either endpoint of");
        report.AppendLine("the goal Segment plus the offset remainder — never node to node.");
        report.AppendLine();

        AppendDenominator(report, graph);
        AppendByDistance(report, graph);
        AppendQuality(report, graph);
        AppendLadder(report, graph);
        AppendAdmissibility(report);
        AppendSeverance(report, graph);

        return report.ToString();
    }

    // --- The denominator itself -------------------------------------------------------------------

    private static void AppendDenominator(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### The denominator — one uncached A\\* search");
        report.AppendLine();
        report.AppendLine("No hierarchy, no cache. Every rung timed, not just the admissible ones, because **the");
        report.AppendLine("expansion count is not the cost**: a tighter metric that has to be computed can lose to a");
        report.AppendLine("looser one that does not, and nothing in the plan's ladder would have shown it.");
        report.AppendLine();
        report.AppendLine("| Query | Heuristic | Mean expanded | Bootstrap | Search | Total | ns per expansion |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---:|");

        foreach ((string name, Modes mode, int radius) in Classes())
        {
            foreach (var kind in Rungs)
            {
                var measurement = Time(graph, mode, radius, kind);

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {name} | `{kind}` | {measurement.MeanExpanded:N0} " +
                    $"| {measurement.BootstrapNanoseconds:N0} ns | {measurement.SearchNanoseconds:N0} ns " +
                    $"| {measurement.TotalNanoseconds:N0} ns " +
                    $"| {(measurement.MeanExpanded == 0 ? 0 : measurement.SearchNanoseconds / measurement.MeanExpanded):N0} ns |"));
            }
        }

        report.AppendLine();
        report.AppendLine(
            "**Bootstrap is the query shape's fixed overhead** — seeding both origin endpoints and " +
            "resolving both goal remainders — measured by re-running the same query set with the " +
            "search loop omitted, rather than by a per-query stopwatch whose own cost would be a " +
            "visible share of it. The queries are drawn before the clock starts. It is reported " +
            "separately because a node-to-node denominator would not have paid it at all, and every " +
            "figure in this spike divides by this one.");
        report.AppendLine();
        report.AppendLine(
            "**The `ns per expansion` column is the one that surprised.** `EuclideanFloor` is the " +
            "tightest safe metric and expands the fewest nodes of the two safe rungs — and it is not " +
            "the fastest, because its exact integer square root is a sixteen-iteration loop run twice " +
            "for every node pushed. `Chebyshev` computes in three instructions and expands more. " +
            "Which one is actually cheaper is a measurement, and it is the reason this table times " +
            "every rung rather than reporting expansions and calling that cost.");
        report.AppendLine();
    }

    private static void AppendByDistance(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### The denominator by origin-destination distance");
        report.AppendLine();
        report.AppendLine("**R1 has not run, so R0 does not guess its distribution.** Queries are drawn uniformly and");
        report.AppendLine("reported per distance bucket, so R1's distribution applies afterwards as weights over");
        report.AppendLine("buckets that already exist — no re-run, and R1's result composes with R0's.");
        report.AppendLine();
        report.AppendLine("| Query | O-D distance | Count | Mean expanded | Mean Segments | Mean cost |");
        report.AppendLine("|---|---|---:|---:|---:|---:|");

        foreach ((string name, Modes mode, int radius) in Classes())
        {
            var buckets = Bucket(graph, mode, radius);

            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i].Count == 0)
                {
                    continue;
                }

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {name} | {BucketName(i)} | {buckets[i].Count:N0} " +
                    $"| {buckets[i].Expanded / buckets[i].Count:N0} " +
                    $"| {buckets[i].Segments / buckets[i].Count:N0} " +
                    $"| {Ticks(buckets[i].Cost / buckets[i].Count)} |"));
            }
        }

        report.AppendLine();
    }

    private static void AppendQuality(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### The denominator's own quality");
        report.AppendLine();
        report.AppendLine("Against `Chebyshev`, the rung R0 publishes. A weak denominator flatters every ratio");
        report.AppendLine("built on it — HPA\\*'s speedup, the cache's value, R2's crossover — so it is stated here.");
        report.AppendLine();
        report.AppendLine("| Query | A\\* expanded | Dijkstra expanded | A\\* as share | Expanded per path Segment |");
        report.AppendLine("|---|---:|---:|---:|---:|");

        foreach ((string name, Modes mode, int radius) in Classes())
        {
            var astar = Ladder(graph, mode, radius, Denominator);
            var dijkstra = Ladder(graph, mode, radius, HeuristicKind.None);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {name} | {astar.MeanExpanded:N0} | {dijkstra.MeanExpanded:N0} " +
                $"| {Percent(astar.MeanExpanded, dijkstra.MeanExpanded)} " +
                $"| {(astar.MeanSegments == 0 ? 0 : astar.MeanExpanded / astar.MeanSegments):N0} |"));
        }

        report.AppendLine();
    }

    // --- The ladder -------------------------------------------------------------------------------

    private static void AppendLadder(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### The heuristic ladder");
        report.AppendLine();
        report.AppendLine("Judged against Dijkstra ground truth on **the same query**, run through the same loop —");
        report.AppendLine("`HeuristicKind.None` is not a second implementation that could disagree for its own reasons.");
        report.AppendLine();
        report.AppendLine("**The non-optimal counts are a lower bound, and the reason is worth carrying forward.**");
        report.AppendLine("The heuristic converts Tiles to Ticks by multiplying by a floored reciprocal rather than");
        report.AppendLine("dividing, which is what removes four hardware divisions per node. The reciprocal's own");
        report.AppendLine("rounding leaves roughly two parts in ten thousand of slack, and that slack **partially");
        report.AppendLine("cancels an overestimating metric's error**. Measured: switching the exact division for the");
        report.AppendLine("reciprocal moved walking `Manhattan` from **35 of 300** to **4 of 300** while leaving");
        report.AppendLine("driving `Manhattan` at 13 — short walks are where the two errors are comparable in size.");
        report.AppendLine();
        report.AppendLine("So an implementation detail chosen for speed makes an unsafe heuristic *look* safer, and");
        report.AppendLine("it does so most exactly where the design cares most — `adr/0008`'s walk Legs. The verdict");
        report.AppendLine("below does not rest on the rate: `Manhattan` and `Octile` overestimate on this graph by");
        report.AppendLine("construction, and a rate that moves with an unrelated optimisation is not the evidence.");
        report.AppendLine();
        report.AppendLine("| Query | Heuristic | Admissible on | Mean expanded | vs Dijkstra | Non-optimal |");
        report.AppendLine("|---|---|---|---:|---:|---:|");

        foreach ((string name, Modes mode, int radius) in Classes())
        {
            var truth = Ladder(graph, mode, radius, HeuristicKind.None);

            foreach (var kind in Rungs)
            {
                var result = Ladder(graph, mode, radius, kind);

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {name} | `{kind}` | {AdmissibleOn(kind)} | {result.MeanExpanded:N0} " +
                    $"| {Percent(result.MeanExpanded, truth.MeanExpanded)} " +
                    $"| {result.NonOptimal:N0} of {result.Found:N0} |"));
            }
        }

        report.AppendLine();
    }

    private static void AppendAdmissibility(StringBuilder report)
    {
        report.AppendLine("### The verdict R0 owes — the Arterial density at which admissibility breaks");
        report.AppendLine();
        report.AppendLine("Non-optimal routes returned, out of routes found, against the number of freeform Arterials.");
        report.AppendLine("Driving only: an Arterial carries no pedestrian edges, so it cannot shortcut a walk.");
        report.AppendLine();
        report.Append("| Arterials | Severed | ");
        foreach (var kind in Rungs)
        {
            report.Append(CultureInfo.InvariantCulture, $"`{kind}` | ");
        }

        report.AppendLine();
        report.Append("|---:|---:|");
        foreach (var _ in Rungs)
        {
            report.Append("---:|");
        }

        report.AppendLine();

        foreach (int arterials in ArterialRungs)
        {
            var graph = GraphGenerator.Build(GraphParameters.Working with { ArterialCount = arterials });
            report.Append(CultureInfo.InvariantCulture, $"| {arterials} | {graph.SeveredStreets:N0} | ");

            foreach (var kind in Rungs)
            {
                var result = Ladder(graph, Modes.Car, radius: 0, kind);
                report.Append(CultureInfo.InvariantCulture,
                    $"{result.NonOptimal:N0} of {result.Found:N0} | ");
            }

            report.AppendLine();
        }

        report.AppendLine();
    }

    private static void AppendSeverance(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### Walking: severed, or merely far");
        report.AppendLine();
        report.AppendLine("`plans/0010`: whether the router can tell **severed** from **merely far** is two different");
        report.AppendLine("Trip Fates and two different player-facing diagnoses, and a search-radius bound chosen for");
        report.AppendLine("performance would collapse them into one. This search has **no radius bound**, so the");
        report.AppendLine("distinction is real rather than definitional: *no route found* is Severance, and a long");
        report.AppendLine("route is a long walk.");
        report.AppendLine();
        report.AppendLine("**The first capture reported zero unreachable walks, and zero is exactly the reading that");
        report.AppendLine("cannot be trusted on its own** — it is equally consistent with *this city is well connected*");
        report.AppendLine("and with *this instrument cannot see Severance*. So the crossing density is swept until the");
        report.AppendLine("count moves. A measurement that has never been observed to fire is not evidence.");
        report.AppendLine();
        report.AppendLine("| Arterials | Foot crossing every | Crossings | No route found | Mean cost when found |");
        report.AppendLine("|---:|---|---:|---:|---:|");

        foreach ((int arterials, int crossing) in SeveranceRungs())
        {
            var rung = GraphGenerator.Build(GraphParameters.Working with
            {
                ArterialCount = arterials,
                FootCrossingEverySevered = crossing,
            });

            var walks = Ladder(rung, Modes.Foot, radius: 0, Denominator);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {arterials} | {(crossing == 0 ? "never" : crossing == 1 ? "every severed Street" : $"{crossing}th severed Street")} " +
                $"| {rung.FootCrossings:N0} | {LadderQueries - walks.Found:N0} of {LadderQueries:N0} " +
                $"| {Ticks(walks.MeanCost)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The instrument fires, so its zeroes mean something.** At the working rung the foot " +
            $"network stays connected — {graph.SegmentsAdmitting(Modes.Foot):N0} of {graph.Segments:N0} " +
            $"Segments admit a pedestrian and the crossings leave no island — and that is now a " +
            $"finding rather than an absence of evidence, because the same measurement reaches " +
            $"230 of 300 unreachable once the crossings are removed at 32 Arterials."));
        report.AppendLine();
        report.AppendLine(
            "**Severance is a property of crossing density, not of Arterial count.** Eight Arterials " +
            "with no crossings at all sever nothing, because eight lines do not partition a plane " +
            "into pieces anyone wants to walk between; thirty-two with no crossings sever almost " +
            "everything. The parameter that decides is the one a player actually controls when they " +
            "choose whether to build a bridge, which is the right place for it to live.");
        report.AppendLine();
        report.AppendLine(
            "**One column reads backwards and it is not an error.** *Mean cost when found* falls at " +
            "32 Arterials with no crossings — 722 Ticks, below the 932 of the rung above it — because " +
            "by then only the nearby pairs are reachable at all. It is survivorship: the long walks " +
            "did not get slower, they stopped being in the sample. A mean conditioned on success " +
            "cannot be read beside a failure count without saying so.");
        report.AppendLine();
    }

    /// <summary>
    /// Arterial count crossed with crossing density. The second axis is the one that matters: an
    /// Arterial with a crossing on every severed Street is a road, and one with none is a wall.
    /// </summary>
    private static (int Arterials, int Crossing)[] SeveranceRungs() =>
    [
        (8, 1),
        (8, 4),
        (8, 16),
        (8, 0),
        (32, 4),
        (32, 16),
        (32, 0),
    ];

    // --- Measurement --------------------------------------------------------------------------------

    private static (string Name, Modes Mode, int Radius)[] Classes() =>
    [
        ("drive", Modes.Car, 0),
        ("walk", Modes.Foot, WalkRadiusTiles),
    ];

    private readonly record struct Timing(
        int Queries, long MeanExpanded, long MeanSegments,
        long TotalNanoseconds, long BootstrapNanoseconds, long SearchNanoseconds);

    /// <summary>
    /// Times a batch twice — once complete, once with the search loop omitted — and takes the
    /// difference. A per-query stopwatch would cost tens of nanoseconds against a bootstrap that may
    /// itself be tens of nanoseconds, which is measuring the instrument.
    /// </summary>
    private static Timing Time(RoadGraph graph, Modes mode, int radius, HeuristicKind kind)
    {
        var search = new PointToPoint(graph);

        // The queries are drawn BEFORE the clock starts, and that is a correction rather than a
        // tidy-up. In the first capture the sampler ran inside both timed loops, so its cost landed
        // in the bootstrap column — and for walks the sampler rejection-samples up to 256 times to
        // find a destination inside the radius, which read as a 3,956 ns "bootstrap" against a
        // drive's 619 ns. The search column was never affected, being a difference of two loops that
        // both paid it; the bootstrap column was almost entirely the sampler.
        var queries = Draw(graph, mode, radius, TimingQueries);
        var warmup = Draw(graph, mode, radius, WarmupQueries);

        foreach ((AccessPoint origin, AccessPoint goal) in warmup)
        {
            search.Bootstrap(origin, goal, mode, kind);
            search.Expand();
        }

        long expanded = 0;
        long segments = 0;

        long startAll = Stopwatch.GetTimestamp();
        foreach ((AccessPoint origin, AccessPoint goal) in queries)
        {
            search.Bootstrap(origin, goal, mode, kind);
            var outcome = search.Expand();
            expanded += outcome.NodesExpanded;
            segments += outcome.PathSegments;
        }

        long all = Nanoseconds(startAll);

        long startBootstrap = Stopwatch.GetTimestamp();
        foreach ((AccessPoint origin, AccessPoint goal) in queries)
        {
            search.Bootstrap(origin, goal, mode, kind);
        }

        long bootstrap = Nanoseconds(startBootstrap);

        return new Timing(
            queries.Length,
            expanded / queries.Length,
            segments / queries.Length,
            all / queries.Length,
            bootstrap / queries.Length,
            (all - bootstrap) / queries.Length);
    }

    /// <summary>Draws a fixed query set, so no measurement includes the cost of choosing what to measure.</summary>
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

    private static long Nanoseconds(long since) =>
        Stopwatch.GetElapsedTime(since, Stopwatch.GetTimestamp()).Ticks * 100;

    private readonly record struct LadderResult(
        long MeanExpanded, long MeanSegments, long MeanCost, int Found, int NonOptimal);

    /// <summary>
    /// Runs the ladder queries under one heuristic, and counts how many returned a cost worse than
    /// Dijkstra's on the same query. That count is the admissibility column.
    /// </summary>
    private static LadderResult Ladder(RoadGraph graph, Modes mode, int radius, HeuristicKind kind)
    {
        var search = new PointToPoint(graph);
        var truth = new PointToPoint(graph);
        var sampler = new OdSampler(graph);
        ulong seed = graph.Parameters.Seed;

        long expanded = 0;
        long segments = 0;
        long cost = 0;
        int found = 0;
        int nonOptimal = 0;

        for (ulong q = 0; q < LadderQueries; q++)
        {
            var origin = sampler.Origin(seed, q, mode);
            var goal = sampler.Destination(seed, q, mode, origin, radius);

            search.Bootstrap(origin, goal, mode, kind);
            var outcome = search.Expand();

            if (!outcome.Found)
            {
                continue;
            }

            found++;
            expanded += outcome.NodesExpanded;
            segments += outcome.PathSegments;
            cost += outcome.CostTicks;

            truth.Bootstrap(origin, goal, mode, HeuristicKind.None);
            var optimal = truth.Expand();

            if (optimal.Found && outcome.CostTicks > optimal.CostTicks)
            {
                nonOptimal++;
            }
        }

        return new LadderResult(
            found == 0 ? 0 : expanded / found,
            found == 0 ? 0 : segments / found,
            found == 0 ? 0 : cost / found,
            found,
            nonOptimal);
    }

    private record struct Bin(int Count, long Expanded, long Segments, long Cost);

    private static Bin[] Bucket(RoadGraph graph, Modes mode, int radius)
    {
        var search = new PointToPoint(graph);
        var sampler = new OdSampler(graph);
        var bins = new Bin[BucketCeilings.Length];
        ulong seed = graph.Parameters.Seed;

        for (ulong q = 0; q < TimingQueries; q++)
        {
            var origin = sampler.Origin(seed, q, mode);
            var goal = sampler.Destination(seed, q, mode, origin, radius);

            search.Bootstrap(origin, goal, mode, Denominator);
            var outcome = search.Expand();

            if (!outcome.Found)
            {
                continue;
            }

            int distance = sampler.StraightLineTiles(origin.Segment, goal.Segment);
            int bucket = 0;
            while (distance >= BucketCeilings[bucket])
            {
                bucket++;
            }

            bins[bucket] = new Bin(
                bins[bucket].Count + 1,
                bins[bucket].Expanded + outcome.NodesExpanded,
                bins[bucket].Segments + outcome.PathSegments,
                bins[bucket].Cost + outcome.CostTicks);
        }

        return bins;
    }

    // --- Formatting -------------------------------------------------------------------------------

    private static string BucketName(int index) => index switch
    {
        0 => "< 1 km",
        1 => "1–2 km",
        2 => "2–4 km",
        3 => "4–8 km",
        4 => "8–16 km",
        _ => "> 16 km",
    };

    private static string AdmissibleOn(HeuristicKind kind) => kind switch
    {
        HeuristicKind.None => "—, it *is* the ground truth",
        HeuristicKind.Manhattan => "4-connected only",
        HeuristicKind.Octile => "8-connected only",
        HeuristicKind.Chebyshev => "any graph",
        _ => "any graph",
    };

    /// <summary>Q16.16 Ticks as whole Ticks and hundredths. A Tick is ~10.5 in-world seconds.</summary>
    private static string Ticks(long fixedTicks) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{fixedTicks / 65536}.{fixedTicks % 65536 * 100 / 65536:D2} Ticks");

    private static string Percent(long part, long whole) =>
        whole == 0 ? "—" : string.Create(CultureInfo.InvariantCulture, $"{part * 100 / whole}%");
}
