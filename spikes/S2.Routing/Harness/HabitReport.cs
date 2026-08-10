using System.Diagnostics;
using System.Globalization;
using System.Text;
using S2.Routing.Graph;
using S2.Routing.Routing;
using S2.Routing.Storm;
using S2.Routing.Traffic;

namespace S2.Routing.Harness;

/// <summary>
/// R6.4 — what a per-Citizen Habit Route costs. The two unmeasured cells of session M's trilemma,
/// plus the reverse index without which the third sub-task's fan-out means nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Per-Citizen, not per-Traveller, and the rename is the reason the store is this size.</b>
/// <c>CONTEXT.md</c> → Traveller: <i>a Traveller is a view, not an owner.</i> A route reused across
/// many Trips is conserved across embodiments, so it lives on the Citizen — which sets the store to
/// the population rather than to the Microscopic Cap, and every figure below is that size times
/// something.
/// </para>
/// <para>
/// <b>The fixture is R5.4's and R5.5.4's exactly, so the numbers sit beside theirs.</b> Draw the
/// Arterial gesture — four Segments, ~512 m, the smallest addition worth drawing — <c>Apply</c> to
/// damage the graph, build the store against the damaged graph, then <c>Revert</c>, because
/// restoration is addition. Store is <c>RouteStore.ForSearchedPool</c>, swept across R4.1's O-D
/// family, because R4 established that the draw is a swept family and not a setting.
/// </para>
/// </remarks>
internal static class HabitReport
{
    /// <summary>
    /// Routes in the store, per rung. Two thousand rather than R5's 512: R6.4.3 divides the woken set
    /// by the resident population and a store of 512 quantises <c>P(stale)</c> at 0.20%, which is
    /// coarser than the differences between the <c>d</c> rungs it is there to separate.
    /// </summary>
    private const int StoreRoutes = 2_048;

    /// <summary>Diversions sampled per rung. The same sample serves every horizon — see below.</summary>
    private const int RejoinSamples = 512;

    /// <summary>Queries in the flat denominator, which is measured twice.</summary>
    private const int DenominatorQueries = 256;

    /// <summary>
    /// The Sight Horizons swept, in Segments. R8.1 derived the <b>floor</b> as 1 Segment off the graph
    /// and set no ceiling, so the ladder starts at the derived floor and doubles.
    /// </summary>
    private static readonly int[] Horizons = [1, 2, 3, 4, 8, 16];

    /// <summary>
    /// Wake radii, in Cells. A Cell is 32 Tiles ≈ 128 m, so this spans one Cell to 16 — ~2 km, which
    /// is already a quarter of the graph's mean uniform journey.
    /// </summary>
    private static readonly int[] Radii = [0, 1, 2, 4, 8, 16];

    /// <summary>The population the memory column is quoted at. <c>CLAUDE.md</c>'s late-game target.</summary>
    private const long TargetCitizens = 1_000_000;

    /// <summary>R8.3's measured diversions per Tick, at N = 1, uniform, 40,000 Travellers.</summary>
    private const long DiversionsPerTick = 1_269;

    /// <summary>The gesture's draw index. 7, which is R5.4's, so it is the same four Segments.</summary>
    private const ulong GestureIndex = 7;

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var segmentArcs = SegmentArcs.Of(graph);
        var storm = new EditStorm(graph, segmentArcs);
        var sampler = new OdSampler(graph);
        var distribution = new OdDistribution(graph, sampler);

        report.AppendLine("## S2 R6.4 — what a per-Citizen Habit Route costs");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();

        var gesture = storm.Draw(CounterHash.Seed, GestureIndex, GestureShape.Arterial, 4);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: {graph.Segments:N0} Segments, {graph.Nodes:N0} nodes, {graph.Arcs:N0} "
            + $"arcs, {storm.CarSegments:N0} admitting cars, {storm.ArterialSegments:N0} of them "
            + $"Arterial. The gesture is **{gesture.Segments.Length} of {gesture.Requested} "
            + $"requested** Arterial Segments — R5.4's rung, drawn at the same index, so it is the "
            + $"same road. Store is {StoreRoutes:N0} searched routes per O-D rung, built against the "
            + $"**damaged** graph and then compared against a full recompute on the restored one."));
        report.AppendLine();

        // Measured before anything else in this section and again after all of it. R3's finding, which
        // R4 and R5 both inherited and which the board addresses to R6 by name: a denominator measured
        // once has no error bar and a denominator measured first has a systematic one.
        var denominatorPool = distribution.Draw(
            CounterHash.Seed, DenominatorQueries, Modes.Car, new OdRung(OdShape.Uniform, 0),
            out _, out _);

        Mark("R6.4 flat denominator, first");
        long denominatorFirst = FlatDenominator(graph, denominatorPool);

        // The whole sweep walked once before any of it is timed. R1 needed four warm-up schemes before
        // its cold column stopped falling smoothly with the swept axis, because the cost was
        // per-process and the small rungs never left tier 0.
        Mark("R6.4 warm pass");
        Warm(graph, storm, distribution, gesture);

        var fixtures = new List<Fixture>();

        foreach (var rung in OdDistribution.Rungs)
        {
            Mark($"R6.4 fixture, O-D {rung.Name}");
            fixtures.Add(Build(graph, storm, distribution, gesture, rung));
        }

        AppendCompression(report, graph, fixtures);
        AppendRejoin(report, graph, fixtures, denominatorFirst);
        AppendWake(report, graph, gesture, fixtures);

        Mark("R6.4 flat denominator, last");
        long denominatorLast = FlatDenominator(graph, denominatorPool);

        AppendDenominator(report, denominatorFirst, denominatorLast);

        return report.ToString();
    }

    // --- the fixture ------------------------------------------------------------------------------

    /// <summary>
    /// One O-D rung's store, built on the damaged graph and recomputed on the restored one.
    /// </summary>
    /// <remarks>
    /// A class rather than a record struct, because <c>BOR0701</c> refuses a reference in a struct and
    /// is right to: this is harness scaffolding and not simulation state.
    /// </remarks>
    private sealed class Fixture
    {
        public required OdRung Rung { get; init; }

        public required OdPair[] Pool { get; init; }

        /// <summary>Routes as searched <b>before</b> the addition — what a Citizen would be holding.</summary>
        public required RouteStore Damaged { get; init; }

        /// <summary>The same pairs recomputed <b>after</b> it. R6.4.3's ground truth.</summary>
        public required RouteStore Restored { get; init; }

        public required int[] DamagedCost { get; init; }

        public required int[] RestoredCost { get; init; }

        public required int DamagedFound { get; init; }

        public required int RestoredFound { get; init; }

        /// <summary>Routes usable by every sub-task: found on both sides, so the pair is comparable.</summary>
        public required int[] Comparable { get; init; }
    }

    private static Fixture Build(
        RoadGraph graph,
        EditStorm storm,
        OdDistribution distribution,
        Gesture gesture,
        OdRung rung)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var pool = distribution.Draw(CounterHash.Seed, StoreRoutes, Modes.Car, rung, out _, out _);

        // The "before" world: the road does not exist yet.
        storm.Apply(gesture, arcCost);
        var damaged = RouteStore.ForSearchedPool(
            graph, pool, arcCost, out int damagedFound, out int[] damagedCost);

        // The addition.
        storm.Revert(gesture, arcCost);
        var restored = RouteStore.ForSearchedPool(
            graph, pool, arcCost, out int restoredFound, out int[] restoredCost);

        var comparable = new List<int>(pool.Length);
        for (int route = 0; route < pool.Length; route++)
        {
            if (damaged.Length(route) > 0 && restored.Length(route) > 0)
            {
                comparable.Add(route);
            }
        }

        return new Fixture
        {
            Rung = rung,
            Pool = pool,
            Damaged = damaged,
            Restored = restored,
            DamagedCost = damagedCost,
            RestoredCost = restoredCost,
            DamagedFound = damagedFound,
            RestoredFound = restoredFound,
            Comparable = [.. comparable],
        };
    }

    // --- R6.4.1 the branch-point compression ratio -------------------------------------------------

    private static void AppendCompression(StringBuilder report, RoadGraph graph, List<Fixture> fixtures)
    {
        report.AppendLine("### R6.4.1 — the branch-point compression ratio");
        report.AppendLine();
        report.AppendLine(
            "A stored route is mostly **forced**. R3 measured this network at degree ~3, so most "
            + "nodes on a route are degree 2 and mid-block, and the arc leaving them is the only one "
            + "there is once the way back is discounted. A route can therefore be stored as the "
            + "decisions taken at the nodes where a decision existed, and reconstructed by walking "
            + "forward and taking the only onward arc everywhere else. **`k` is that count.** The "
            + "branch test is R8.1's, reused rather than reinvented: *at least two onward car arcs "
            + "once the arrival Segment is discounted*, evaluated at the node **before** each arc, "
            + "because the last node of a route needs no decision.");
        report.AppendLine();
        report.AppendLine(
            "| O-D rung | Routes | Mean arcs `L` | Mean `k` | p50 `k` | p90 `k` | max `k` "
            + "| `L / k` | Bytes / Citizen | **At 1M** |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var fixture in fixtures)
        {
            var row = Compression(graph, fixture, fixture.Damaged);
            report.AppendLine(Row(fixture.Rung.Name, row));
        }

        report.AppendLine();
        report.AppendLine(
            "**The same measurement over the routes as they stand *after* the addition**, which is "
            + "the second reading the board's *two measurements that agree to the last digit are not "
            + "two measurements* asks for — and it is stated in advance that this is a **weak** "
            + "second reading rather than an independent one. Four restored Arterial Segments move "
            + "few routes, so agreement here is the expected result and disagreement would be the "
            + "surprise. What it does check is that `k` is a property of the graph rather than of the "
            + "damage: a `k` that moved materially when four Segments came back would mean the first "
            + "table was measuring the hole and not the network.");
        report.AppendLine();
        report.AppendLine(
            "| O-D rung | Routes | Mean arcs `L` | Mean `k` | p50 `k` | p90 `k` | max `k` "
            + "| `L / k` | Bytes / Citizen | **At 1M** |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var fixture in fixtures)
        {
            var row = Compression(graph, fixture, fixture.Restored);
            report.AppendLine(Row(fixture.Rung.Name, row));
        }

        report.AppendLine();
        report.AppendLine(
            "*Bytes per Citizen* is `k × 4`, one 32-bit arc id per decision, and **at 1M** is that "
            + "times the late-game population — the axis the per-Citizen row of session M's trilemma "
            + "was chosen against, and the one it is refuted on. The uncompressed comparator is the "
            + "trilemma's own **232.7 MiB**, which is `L × 4 × 1M` at `L = 61`.");
        report.AppendLine();
    }

    private sealed class CompressionRow
    {
        public int Routes { get; init; }

        public long MeanArcsHundredths { get; init; }

        public long MeanBranchHundredths { get; init; }

        public int Median { get; init; }

        public int NinetiethPercentile { get; init; }

        public int Max { get; init; }

        public long RatioHundredths { get; init; }

        public long BytesPerCitizen { get; init; }

        public long BytesAtTarget { get; init; }
    }

    private static string Row(string rung, CompressionRow row) => string.Create(
        CultureInfo.InvariantCulture,
        $"| {rung} | {row.Routes:N0} | {Hundredths(row.MeanArcsHundredths)} "
        + $"| **{Hundredths(row.MeanBranchHundredths)}** | {row.Median} "
        + $"| {row.NinetiethPercentile} | {row.Max} | {Hundredths(row.RatioHundredths)}× "
        + $"| {row.BytesPerCitizen} B | **{Mib(row.BytesAtTarget)}** |");

    private static CompressionRow Compression(RoadGraph graph, Fixture fixture, RouteStore store)
    {
        var branches = new List<int>(fixture.Comparable.Length);
        long arcs = 0;

        foreach (int route in fixture.Comparable)
        {
            int length = store.Length(route);
            arcs += length;
            branches.Add(BranchPoints(graph, store, route, fixture.Pool[route].Origin.Segment, null));
        }

        int[] sorted = [.. branches];
        Array.Sort(sorted);

        long total = 0;
        foreach (int k in sorted)
        {
            total += k;
        }

        int count = sorted.Length;
        long meanBranch = count == 0 ? 0 : (total * 100) / count;
        long meanArcs = count == 0 ? 0 : (arcs * 100) / count;
        long bytes = count == 0 ? 0 : ((total * 4) + (count / 2)) / count;

        return new CompressionRow
        {
            Routes = count,
            MeanArcsHundredths = meanArcs,
            MeanBranchHundredths = meanBranch,
            Median = count == 0 ? 0 : sorted[count / 2],
            NinetiethPercentile = count == 0 ? 0 : sorted[(count * 9) / 10],
            Max = count == 0 ? 0 : sorted[count - 1],
            RatioHundredths = total == 0 ? 0 : (arcs * 100) / total,
            BytesPerCitizen = bytes,
            BytesAtTarget = bytes * TargetCitizens,
        };
    }

    /// <summary>
    /// Counts the nodes along a route at which a decision existed, optionally recording the step index
    /// of each so R6.4.2 can divert at one.
    /// </summary>
    private static int BranchPoints(
        RoadGraph graph, RouteStore store, int route, int originSegment, List<int>? steps)
    {
        int length = store.Length(route);
        if (length == 0)
        {
            return 0;
        }

        int branches = 0;
        int arrivalSegment = originSegment;
        int node = Source(graph, store.ArcAt(route, 0));

        for (int step = 0; step < length; step++)
        {
            if (Choices(graph, node, arrivalSegment) >= 2)
            {
                branches++;
                steps?.Add(step);
            }

            int arc = store.ArcAt(route, step);
            arrivalSegment = graph.ArcSegment[arc];
            node = graph.ArcTarget[arc];
        }

        return branches;
    }

    /// <summary>Onward car arcs from a node once the way back down the arrival Segment is discounted.</summary>
    private static int Choices(RoadGraph graph, int node, int arrivalSegment)
    {
        int choices = 0;

        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            if ((graph.ArcModes[arc] & (byte)Modes.Car) == 0
                || graph.ArcSegment[arc] == arrivalSegment)
            {
                continue;
            }

            choices++;
        }

        return choices;
    }

    /// <summary>The node an arc leaves from, which the CSR stores only implicitly.</summary>
    private static int Source(RoadGraph graph, int arc)
    {
        int segment = graph.ArcSegment[arc];
        int a = graph.SegmentNodeA[segment];
        return graph.ArcTarget[arc] == a ? graph.SegmentNodeB[segment] : a;
    }

    // --- R6.4.2 the rejoin cost -------------------------------------------------------------------

    private static void AppendRejoin(
        StringBuilder report, RoadGraph graph, List<Fixture> fixtures, long denominator)
    {
        report.AppendLine("### R6.4.2 — the rejoin cost");
        report.AppendLine();
        report.AppendLine(
            "One arc off a stored path, search back to it, bounded by the Sight Horizon. This is what "
            + "a per-Citizen stored route has to do that a next-hop tree gets for free: the tree "
            + "answers *where next* from wherever the Traveller actually is, and a stored path only "
            + "answers it from on the path. **The diversion point is drawn among the route's branch "
            + "points**, not uniformly along it, because a Traveller cannot diverge where there is "
            + "nothing to diverge onto — R6.4.1 is what makes that distinction quantitative.");
        report.AppendLine();
        report.AppendLine(
            "**The same sample serves every horizon**, deliberately: a sample redrawn per rung shrinks "
            + "wherever the rung is harsher, and this spike has three times manufactured a trend out "
            + "of survivorship that way. So the *attempted* column is flat down each rung by "
            + "construction and only *rejoined* moves.");
        report.AppendLine();
        report.AppendLine(
            "**What is timed is a cost-ordered bounded search**, not a breadth-first walk: the "
            + "Traveller wants the cheapest way back, and the hop cap is on Segments because that is "
            + "the unit R8.1 derived the Horizon's floor in. Suffix marking is timed **apart** and "
            + "reported beside it, because *is this node on my route* is not free against a "
            + "branch-point-compressed store and pretending otherwise would price the winning "
            + "representation using the losing one's data structure.");
        report.AppendLine();
        report.AppendLine(
            "| O-D rung | Horizon | Attempted | Rejoined | Suffix mark | Search alone | **Total** "
            + "| p50 | p90 | max | × 1,269 diversions | of 15.6 ms |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        var excluded = new StringBuilder();
        var samples = new List<List<Diversion>>();
        var noBranchPer = new List<int>();
        var noAlternativePer = new List<int>();

        foreach (var fixture in fixtures)
        {
            samples.Add(DrawDiversions(graph, fixture, out int noBranch, out int noAlternative));
            noBranchPer.Add(noBranch);
            noAlternativePer.Add(noAlternative);
        }

        // The whole sweep walked once, untimed, before any of it is timed. R1's finding: an artefact
        // that varies with the swept axis is indistinguishable from a result, and the first build's
        // uniform rung — which is first in the ladder — read 1.05 µs at Horizon 1 where every other
        // rung read under 560 ns.
        for (int i = 0; i < fixtures.Count; i++)
        {
            foreach (int horizon in Horizons)
            {
                Rejoin(report, graph, fixtures[i], samples[i], horizon, 0, timed: false);
            }
        }

        for (int i = 0; i < fixtures.Count; i++)
        {
            Mark($"R6.4.2 rejoin, O-D {fixtures[i].Rung.Name}");
            long mark = MarkCost(graph, fixtures[i], samples[i]);

            foreach (int horizon in Horizons)
            {
                Rejoin(report, graph, fixtures[i], samples[i], horizon, mark, timed: true);
            }

            excluded.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"- `{fixtures[i].Rung.Name}`: **{samples[i].Count} of {RejoinSamples}** draws "
                + $"produced a diversion — {noBranchPer[i]} fell on a route with no branch point at "
                + $"all, {noAlternativePer[i]} on a branch point whose only other arcs were the way "
                + $"back or the way the route already goes."));
        }

        report.AppendLine();
        report.AppendLine("**Sample size per rung, which is the survivorship guard:**");
        report.AppendLine();
        report.Append(excluded);
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"*× 1,269 diversions* multiplies by R8.3's measured **{DiversionsPerTick:N0} diversions "
            + $"per Tick** at N = 1, uniform, 40,000 Travellers — which is a figure from a different "
            + $"fleet size than the 1M store above, and is printed because R6.4.2's own threshold is "
            + $"stated as a product against it. The flat denominator this section could have used "
            + $"instead reads **{Ns(denominator)}** a search, so a rejoin that costs less than a "
            + $"thousandth of one is the interesting case and a rejoin that costs a tenth of one is "
            + $"the row losing outright."));
        report.AppendLine();
    }

    /// <summary>One drawn diversion, resolved before anything is timed.</summary>
    private sealed class Diversion
    {
        public required int Route { get; init; }

        public required int SuffixFrom { get; init; }

        public required int Node { get; init; }

        public required int ArrivalSegment { get; init; }
    }

    /// <summary>
    /// Draws the diversion sample. <b>Drawn once per rung and shared by every horizon</b>, so the
    /// *attempted* column is flat down the ladder by construction and a rung cannot improve by losing
    /// the samples it would have failed.
    /// </summary>
    private static List<Diversion> DrawDiversions(
        RoadGraph graph, Fixture fixture, out int noBranch, out int noAlternative)
    {
        var drawn = new List<Diversion>(RejoinSamples);
        var steps = new List<int>();

        noBranch = 0;
        noAlternative = 0;

        if (fixture.Comparable.Length == 0)
        {
            return drawn;
        }

        for (int draw = 0; draw < RejoinSamples; draw++)
        {
            ulong roll = CounterHash.Of(
                CounterHash.Seed, (ulong)draw, 0, CounterHash.Purpose.HabitDiversionRoute);
            int route = fixture.Comparable[CounterHash.Below(roll, fixture.Comparable.Length)];

            steps.Clear();
            BranchPoints(graph, fixture.Damaged, route, fixture.Pool[route].Origin.Segment, steps);

            if (steps.Count == 0)
            {
                noBranch++;
                continue;
            }

            ulong pick = CounterHash.Of(
                CounterHash.Seed, (ulong)draw, 0, CounterHash.Purpose.HabitDiversionStep);
            int step = steps[CounterHash.Below(pick, steps.Count)];

            int arc = fixture.Damaged.ArcAt(route, step);
            int node = Source(graph, arc);
            int arrivalSegment = step == 0
                ? fixture.Pool[route].Origin.Segment
                : graph.ArcSegment[fixture.Damaged.ArcAt(route, step - 1)];

            int diversion = Alternative(graph, node, arrivalSegment, graph.ArcSegment[arc], pick);
            if (diversion < 0)
            {
                noAlternative++;
                continue;
            }

            drawn.Add(new Diversion
            {
                Route = route,
                SuffixFrom = step + 1,
                Node = graph.ArcTarget[diversion],
                ArrivalSegment = graph.ArcSegment[diversion],
            });
        }

        return drawn;
    }

    /// <summary>
    /// What one suffix mark costs. Measured <b>once per rung</b> and not per horizon, because the mark
    /// does not depend on the horizon — and the first build measured it per horizon and printed 498 ns,
    /// 2.39 µs and 259 ns for identical work, which is the process leaving tier 0 wearing a swept
    /// axis's clothes. It is batched over the whole sample because one mark is tens of nanoseconds and
    /// so is a <c>Stopwatch</c> read.
    /// </summary>
    private static long MarkCost(RoadGraph graph, Fixture fixture, List<Diversion> drawn)
    {
        if (drawn.Count == 0)
        {
            return 0;
        }

        var search = new BoundedRejoin(graph);
        const int Repeats = 64;

        long start = Stopwatch.GetTimestamp();
        for (int repeat = 0; repeat < Repeats; repeat++)
        {
            foreach (var diversion in drawn)
            {
                search.MarkSuffix(fixture.Damaged, diversion.Route, diversion.SuffixFrom);
            }
        }

        return Elapsed(start) / (drawn.Count * Repeats);
    }

    private static void Rejoin(
        StringBuilder report,
        RoadGraph graph,
        Fixture fixture,
        List<Diversion> drawn,
        int horizon,
        long mark,
        bool timed)
    {
        var search = new BoundedRejoin(graph);
        var samples = new List<long>(drawn.Count);

        int rejoined = 0;

        foreach (var diversion in drawn)
        {
            // The mark is outside the timed region, not subtracted from inside it. The first build
            // derived *search alone* as total minus mark and printed **−360 ns**, which is what a
            // derived column does when its two terms are measured under different warmth.
            search.MarkSuffix(fixture.Damaged, diversion.Route, diversion.SuffixFrom);

            long start = Stopwatch.GetTimestamp();
            bool found = search.Run(diversion.Node, diversion.ArrivalSegment, horizon);
            long elapsed = Elapsed(start);

            samples.Add(elapsed);
            if (found)
            {
                rejoined++;
            }
        }

        if (!timed)
        {
            return;
        }

        long[] sorted = [.. samples];
        Array.Sort(sorted);

        long total = 0;
        foreach (long ns in sorted)
        {
            total += ns;
        }

        long alone = sorted.Length == 0 ? 0 : total / sorted.Length;
        long combined = alone + mark;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {fixture.Rung.Name} | {horizon} | {drawn.Count:N0} "
            + $"| {rejoined:N0} ({Share(rejoined, drawn.Count)}) | {Ns(mark)} | {Ns(alone)} "
            + $"| **{Ns(combined)}** "
            + $"| {Ns(sorted.Length == 0 ? 0 : sorted[sorted.Length / 2])} "
            + $"| {Ns(sorted.Length == 0 ? 0 : sorted[(sorted.Length * 9) / 10])} "
            + $"| {Ns(sorted.Length == 0 ? 0 : sorted[^1])} "
            + $"| {Ms(combined * DiversionsPerTick)} | {Budget(combined * DiversionsPerTick)} |"));
    }

    /// <summary>
    /// An onward car arc from a node that is neither the route's own next arc nor the way back.
    /// </summary>
    private static int Alternative(
        RoadGraph graph, int node, int arrivalSegment, int takenSegment, ulong roll)
    {
        int candidates = 0;

        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            if ((graph.ArcModes[arc] & (byte)Modes.Car) == 0
                || graph.ArcSegment[arc] == arrivalSegment
                || graph.ArcSegment[arc] == takenSegment)
            {
                continue;
            }

            candidates++;
        }

        if (candidates == 0)
        {
            return -1;
        }

        int wanted = CounterHash.Below(roll, candidates);
        int seen = 0;

        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            if ((graph.ArcModes[arc] & (byte)Modes.Car) == 0
                || graph.ArcSegment[arc] == arrivalSegment
                || graph.ArcSegment[arc] == takenSegment)
            {
                continue;
            }

            if (seen++ == wanted)
            {
                return arc;
            }
        }

        return -1;
    }

    /// <summary>
    /// A cost-ordered search from one arc off a route back onto it, capped at a number of Segments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Generation stamping rather than clearing</b>, for <c>PointToPoint</c>'s reason: a per-query
    /// <c>Array.Clear</c> over 66,000 nodes would be most of a measurement whose whole point is that
    /// it is small.
    /// </para>
    /// <para>
    /// <b>What it does not claim.</b> Label-setting on cost with a hop cap can return a rejoin that a
    /// label-correcting search would have beaten within the same cap. This measures what a rejoin
    /// <i>costs</i>, not how good it is, and the distinction is stated rather than buried: a
    /// cost-optimal-within-cap variant is strictly more work, so every figure here is a **lower
    /// bound** on the rejoin's price and the threshold is scored against it in that direction.
    /// </para>
    /// </remarks>
    private sealed class BoundedRejoin
    {
        private readonly RoadGraph _graph;
        private readonly int[] _cost;
        private readonly int[] _hops;
        private readonly int[] _seen;
        private readonly int[] _onPath;
        private readonly int[] _heapKey;
        private readonly int[] _heapNode;

        private int _generation;
        private int _pathGeneration;
        private int _heapCount;

        public BoundedRejoin(RoadGraph graph)
        {
            _graph = graph;
            _cost = new int[graph.Nodes];
            _hops = new int[graph.Nodes];
            _seen = new int[graph.Nodes];
            _onPath = new int[graph.Nodes];
            _heapKey = new int[4096];
            _heapNode = new int[4096];
        }

        /// <summary>Marks the nodes of a route's suffix. Timed apart, because it is not free.</summary>
        public void MarkSuffix(RouteStore store, int route, int fromStep)
        {
            _pathGeneration++;

            int length = store.Length(route);
            for (int step = fromStep; step < length; step++)
            {
                _onPath[_graph.ArcTarget[store.ArcAt(route, step)]] = _pathGeneration;
            }
        }

        public bool Run(int fromNode, int arrivalSegment, int horizonSegments)
        {
            var graph = _graph;
            _generation++;
            _heapCount = 0;

            if (_onPath[fromNode] == _pathGeneration)
            {
                return true;
            }

            _cost[fromNode] = 0;
            _hops[fromNode] = 0;
            _seen[fromNode] = _generation;
            Push(0, fromNode);

            int enteredBy = arrivalSegment;

            while (_heapCount > 0)
            {
                (int key, int node) = Pop();

                if (key > _cost[node] || _hops[node] > horizonSegments)
                {
                    continue;
                }

                if (_onPath[node] == _pathGeneration)
                {
                    return true;
                }

                if (_hops[node] == horizonSegments)
                {
                    continue;
                }

                for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
                {
                    if ((graph.ArcModes[arc] & (byte)Modes.Car) == 0)
                    {
                        continue;
                    }

                    if (node == fromNode && graph.ArcSegment[arc] == enteredBy)
                    {
                        continue;
                    }

                    int step = graph.ArcCarTicks[arc];
                    if (step == RoadGraph.Impassable)
                    {
                        continue;
                    }

                    int target = graph.ArcTarget[arc];
                    int cost = _cost[node] + step;

                    if (_seen[target] == _generation && _cost[target] <= cost)
                    {
                        continue;
                    }

                    _seen[target] = _generation;
                    _cost[target] = cost;
                    _hops[target] = _hops[node] + 1;
                    Push(cost, target);
                }
            }

            return false;
        }

        private void Push(int key, int node)
        {
            if (_heapCount == _heapKey.Length)
            {
                return;
            }

            int i = _heapCount++;
            _heapKey[i] = key;
            _heapNode[i] = node;

            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_heapKey[parent] <= _heapKey[i])
                {
                    break;
                }

                Swap(parent, i);
                i = parent;
            }
        }

        private (int Key, int Node) Pop()
        {
            (int key, int node) = (_heapKey[0], _heapNode[0]);

            _heapCount--;
            _heapKey[0] = _heapKey[_heapCount];
            _heapNode[0] = _heapNode[_heapCount];

            int i = 0;
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= _heapCount)
                {
                    break;
                }

                int smallest = left;
                int right = left + 1;
                if (right < _heapCount && _heapKey[right] < _heapKey[left])
                {
                    smallest = right;
                }

                if (_heapKey[i] <= _heapKey[smallest])
                {
                    break;
                }

                Swap(i, smallest);
                i = smallest;
            }

            return (key, node);
        }

        private void Swap(int a, int b)
        {
            (_heapKey[a], _heapKey[b]) = (_heapKey[b], _heapKey[a]);
            (_heapNode[a], _heapNode[b]) = (_heapNode[b], _heapNode[a]);
        }
    }

    // --- R6.4.3 the addition wake ------------------------------------------------------------------

    private static void AppendWake(
        StringBuilder report, RoadGraph graph, Gesture gesture, List<Fixture> fixtures)
    {
        report.AppendLine("### R6.4.3 — the addition wake's fan-out, and what `d` should be");
        report.AppendLine();
        report.AppendLine(
            "`adr/0012`'s contract wakes routes within `d` of a newly added Segment. R1.7 measured "
            + "that a proximity test over a **matrix** missed 309 of 429 changed entries and missed "
            + "them silently; this runs the same method over a **route store**. `C` is ground truth — "
            + "the routes a full recompute on the restored graph returns cheaper than the stored one "
            + "— and `W(d)` is what the reverse index wakes.");
        report.AppendLine();
        report.AppendLine(
            "**A caveat stated in advance and running against us**: R1.7's entries are *pairs* and "
            + "these are *paths*. A path is a longer object with more chances to pass near an edit, so "
            + "a route store's fan-out should be **worse** than R1.7's at the same `d`, not better. "
            + "Nothing below is read as the earlier number improving.");
        report.AppendLine();
        report.AppendLine(
            "**The wake sets a bit; it does not recompute.** So `|W(d)|` is priced in marks, and the "
            + "refutation is `P(stale)` approaching 1 at every `d` that catches a useful share of `C` "
            + "— one road edit marking most of the city, every subsequent Trip start recomputing, and "
            + "the drain bounding nothing.");
        report.AppendLine();

        foreach (var fixture in fixtures)
        {
            AppendWakeRung(report, graph, gesture, fixture);
        }
    }

    private static void AppendWakeRung(
        StringBuilder report, RoadGraph graph, Gesture gesture, Fixture fixture)
    {
        var changed = new bool[fixture.Pool.Length];

        // C split by magnitude. `adr/0012`'s contract is stated over *changed*, and a strict
        // inequality on integer Tick costs makes a route improved by one Tick in five thousand a
        // member of C exactly as much as one improved by 6%. Whether that is the set worth waking is
        // a decision, not a measurement — so both are printed and the decision is left visible.
        var material = new bool[fixture.Pool.Length];
        int materialCount = 0;
        int changedCount = 0;
        int worse = 0;
        long improvementTotal = 0;
        int bestImprovement = 0;

        foreach (int route in fixture.Comparable)
        {
            int before = fixture.DamagedCost[route];
            int after = fixture.RestoredCost[route];

            if (after < before)
            {
                changed[route] = true;
                changedCount++;

                int gain = ((before - after) * 10_000) / before;
                improvementTotal += gain;

                if (gain >= 100)
                {
                    material[route] = true;
                    materialCount++;
                }

                if (gain > bestImprovement)
                {
                    bestImprovement = gain;
                }
            }
            else if (after > before)
            {
                // Addition is monotone-improving: restoring road cannot make a journey dearer. This
                // is a conservation law the board asks to be printed on the run where it reads yes,
                // and it is the check that says the two cost columns are the same quantity.
                worse++;
            }
        }

        var index = new RouteCellIndex(graph, fixture.Pool.Length, fixture.Damaged.Arcs);

        // Warmed by a full insert-and-evict cycle before either is timed, and the free list is what
        // makes the timed insert measure the same work as the warm one rather than a fresh allocation.
        foreach (int route in fixture.Comparable)
        {
            index.Insert(route, fixture.Damaged.Span(route));
        }

        foreach (int route in fixture.Comparable)
        {
            index.Remove(route);
        }

        long insertStart = Stopwatch.GetTimestamp();
        foreach (int route in fixture.Comparable)
        {
            index.Insert(route, fixture.Damaged.Span(route));
        }

        long insertTotal = Elapsed(insertStart);

        var woken = new int[fixture.Pool.Length];

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**O-D rung `{fixture.Rung.Name}`** — {fixture.Comparable.Length:N0} comparable routes "
            + $"of {fixture.Pool.Length:N0} drawn ({fixture.DamagedFound:N0} found on the damaged "
            + $"graph, {fixture.RestoredFound:N0} on the restored one). **`|C|` = "
            + $"{changedCount:N0}**, mean improvement "
            + $"{(changedCount == 0 ? "—" : Hundredths(improvementTotal / changedCount) + "%")}, "
            + $"best {(changedCount == 0 ? "—" : Hundredths(bestImprovement) + "%")}. Routes made "
            + $"**dearer** by the addition: **{worse}** — the conservation law, and it must read "
            + $"zero. Of those {changedCount:N0}, **{materialCount:N0} improve by more than 1%** — "
            + $"`C` is dominated by routes a recompute would change by a rounding error, and the "
            + $"`material` columns below are the same sweep against that subset."));
        report.AppendLine();
        report.AppendLine(
            "| `d`, Cells | `d`, m | `\\|W(d)\\|` | `\\|C \\ W(d)\\|` missed | `\\|W(d) \\ C\\|` "
            + "needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query "
            + "| Chain steps |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (int radius in Radii)
        {
            // Timed over repeats because one gesture's query is microseconds and the clock read would
            // otherwise be a visible share of it.
            const int Repeats = 64;
            int count = 0;
            long start = Stopwatch.GetTimestamp();
            for (int repeat = 0; repeat < Repeats; repeat++)
            {
                count = index.Wake(gesture.Segments, radius, woken);
            }

            long query = Elapsed(start) / Repeats;

            int hit = 0;
            int materialHit = 0;
            for (int i = 0; i < count; i++)
            {
                if (changed[woken[i]])
                {
                    hit++;
                }

                if (material[woken[i]])
                {
                    materialHit++;
                }
            }

            int missed = changedCount - hit;
            int needless = count - hit;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {radius} | {radius * Units.CellTiles * 4:N0} | **{count:N0}** | **{missed:N0}** "
                + $"| {needless:N0} | **{Share(count, fixture.Comparable.Length)}** "
                + $"| {Share(hit, changedCount)} "
                + $"| **{Share(materialHit, materialCount)}** ({materialHit} of {materialCount}) "
                + $"| {Ns(query)} | {index.LastWakeSteps:N0} |"));
        }

        // Both eviction forms, in one run, so the trade has a number on each side. The singly-linked
        // form is the plain intrusive list; the doubly-linked one pays a fifth flat array to delete
        // the predecessor walk.
        long singlyStart = Stopwatch.GetTimestamp();
        long singlySteps = 0;
        foreach (int route in fixture.Comparable)
        {
            index.Remove(route);
            singlySteps += index.LastRemoveSteps;
        }

        long singlyTotal = Elapsed(singlyStart);

        foreach (int route in fixture.Comparable)
        {
            index.Insert(route, fixture.Damaged.Span(route));
        }

        long doublyStart = Stopwatch.GetTimestamp();
        long doublySteps = 0;
        foreach (int route in fixture.Comparable)
        {
            index.RemoveDoubly(route);
            doublySteps += index.LastRemoveSteps;
        }

        long doublyTotal = Elapsed(doublyStart);
        int routes = fixture.Comparable.Length;
        long memberships = routes == 0 ? 0 : index.HighWater / routes;
        long routeBytes = memberships * 4 * sizeof(int);

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Reverse index: **{index.HighWater:N0} memberships** over "
            + $"{index.CellsPerSide * index.CellsPerSide:N0} Cells — **{memberships} a route** — "
            + $"**{Mib(index.SinglyLinkedBytes)}** singly linked against the store's "
            + $"{Mib(fixture.Damaged.ResidentBytes)} "
            + $"({Ratio(index.SinglyLinkedBytes, fixture.Damaged.ResidentBytes)}), "
            + $"{Mib(index.ResidentBytes)} with the `previous` array. Insert "
            + $"**{Ns(routes == 0 ? 0 : insertTotal / routes)}** a route. Evict "
            + $"**{Ns(routes == 0 ? 0 : singlyTotal / routes)}** over "
            + $"{(routes == 0 ? 0 : singlySteps / routes):N0} chain steps singly linked, against "
            + $"**{Ns(routes == 0 ? 0 : doublyTotal / routes)}** over "
            + $"{(routes == 0 ? 0 : doublySteps / routes):N0} doubly linked "
            + $"({Ratio(singlyTotal, doublyTotal)}). Live entries after the evictions: "
            + $"**{index.Entries}** against a high water of {index.HighWater:N0} — `adr/0006`'s "
            + $"sink, printed on the run where it reads yes."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The index's own cost per Citizen, which is the column session M's trilemma has no "
            + $"cell for**: {memberships} memberships × four ints is **{routeBytes} B a route**, so "
            + $"at 1M Citizens the index alone is **{Mib(routeBytes * TargetCitizens)}**, against the "
            + $"compressed route's own figure in R6.4.1. Every membership is a Cell the route "
            + $"enters, so this scales with **journey length** and not with `k` — and **nothing "
            + $"R6.4.1 compresses touches it.**"));
        report.AppendLine();
    }

    // --- the denominator, twice -------------------------------------------------------------------

    private static void AppendDenominator(StringBuilder report, long first, long last)
    {
        report.AppendLine("### The denominator, measured twice");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"R3's finding, addressed to R6 by name on the board: *a denominator measured once has no "
            + $"error bar, and a denominator measured first has a systematic one.* The same "
            + $"{DenominatorQueries} uniform flat searches, before anything else in this section and "
            + $"again after all of it."));
        report.AppendLine();
        report.AppendLine("| Reading | Mean per search |");
        report.AppendLine("|---|---:|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| first, cold | {Ns(first)} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| last, warm | {Ns(last)} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| spread | **{Ratio(first, last)}** |"));
        report.AppendLine();
    }

    private static long FlatDenominator(RoadGraph graph, OdPair[] pool)
    {
        var search = new PointToPoint(graph);
        long start = Stopwatch.GetTimestamp();

        for (int query = 0; query < pool.Length; query++)
        {
            search.Bootstrap(pool[query].Origin, pool[query].Destination, Modes.Car, HeuristicKind.Chebyshev);
            search.Expand();
        }

        return Elapsed(start) / pool.Length;
    }

    /// <summary>
    /// Walks every kind of work in this section once before any of it is timed.
    /// </summary>
    private static void Warm(
        RoadGraph graph, EditStorm storm, OdDistribution distribution, Gesture gesture)
    {
        var arcCost = (int[])graph.ArcCarTicks.Clone();
        var pool = distribution.Draw(
            CounterHash.Seed, 64, Modes.Car, new OdRung(OdShape.Uniform, 0), out _, out _);

        storm.Apply(gesture, arcCost);
        var store = RouteStore.ForSearchedPool(graph, pool, arcCost, out _, out _);
        storm.Revert(gesture, arcCost);

        var index = new RouteCellIndex(graph, pool.Length, store.Arcs);
        var search = new BoundedRejoin(graph);
        var woken = new int[pool.Length];
        var steps = new List<int>();

        for (int route = 0; route < pool.Length; route++)
        {
            if (store.Length(route) == 0)
            {
                continue;
            }

            index.Insert(route, store.Span(route));

            steps.Clear();
            BranchPoints(graph, store, route, pool[route].Origin.Segment, steps);
            search.MarkSuffix(store, route, 1);

            foreach (int horizon in Horizons)
            {
                search.Run(
                    graph.ArcTarget[store.ArcAt(route, 0)],
                    graph.ArcSegment[store.ArcAt(route, 0)],
                    horizon);
            }
        }

        foreach (int radius in Radii)
        {
            index.Wake(gesture.Segments, radius, woken);
        }

        for (int route = 0; route < pool.Length; route++)
        {
            index.Remove(route);
        }
    }

    // --- formatting -------------------------------------------------------------------------------

    private static void Mark(string section) =>
        Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {section}");

    /// <summary>
    /// Elapsed nanoseconds, overflow-safe <b>and</b> at the clock's own resolution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The board's owed finding: R4's helper computed <c>elapsed × 1,000,000,000</c> and passed
    /// <c>long.MaxValue</c> at about 9.2 seconds on a nanosecond clock, publishing a four-minute rung
    /// as <b>−8,267.51 ms</b>. <c>MatrixReport</c> still spells it that way; every other section was
    /// repaired to <c>TimeSpan.Ticks × 100</c>, which is overflow-safe.
    /// </para>
    /// <para>
    /// <b>That repair is not usable here, and noticing why is R6.4's own instrument finding.</b> A
    /// <c>TimeSpan</c> tick is 100 ns, so the repaired helper quantises every reading to a 100 ns
    /// grid — invisible against R5's millisecond storms and ruinous against a rejoin measured in
    /// single microseconds, where it is a 5–10% quantum and would flatten the p50/p90/max ladder
    /// this section reports. Splitting the timestamp delta into whole seconds and a remainder keeps
    /// the clock's full resolution while bounding the product: the remainder is below
    /// <c>Stopwatch.Frequency</c>, so <c>remainder × 1e9</c> cannot exceed ~1e18 against a ceiling
    /// of 9.2e18, at any duration whatever.
    /// </para>
    /// </remarks>
    private static long Elapsed(long start)
    {
        long delta = Stopwatch.GetTimestamp() - start;
        long frequency = Stopwatch.Frequency;
        long seconds = delta / frequency;

        return (seconds * 1_000_000_000L)
            + (((delta - (seconds * frequency)) * 1_000_000_000L) / frequency);
    }

    private static string Ns(long ns) => ns >= 1_000_000
        ? string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000_000}.{(ns / 10_000) % 100:D2} ms")
        : ns >= 1_000
            ? string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000}.{(ns / 10) % 100:D2} µs")
            : string.Create(CultureInfo.InvariantCulture, $"{ns} ns");

    private static string Ms(long ns) =>
        string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000_000}.{(ns / 1_000) % 1_000:D3} ms");

    private static string Budget(long ns) => string.Create(
        CultureInfo.InvariantCulture,
        $"{(ns * 100) / 15_600_000}.{((ns * 10_000) / 15_600_000) % 100:D2}%");

    private static string Mib(long bytes) => bytes >= 1024 * 1024
        ? string.Create(
            CultureInfo.InvariantCulture,
            $"{bytes / (1024 * 1024)}.{((bytes * 100) / (1024 * 1024)) % 100:D2} MiB")
        : string.Create(
            CultureInfo.InvariantCulture,
            $"{bytes / 1024}.{((bytes * 100) / 1024) % 100:D2} KiB");

    private static string Hundredths(long value) => string.Create(
        CultureInfo.InvariantCulture, $"{value / 100}.{value % 100:D2}");

    private static string Share(long part, long whole) => whole == 0
        ? "—"
        : string.Create(
            CultureInfo.InvariantCulture,
            $"{(part * 100) / whole}.{((part * 10_000) / whole) % 100:D2}%");

    private static string Ratio(long a, long b) => b == 0
        ? "—"
        : string.Create(CultureInfo.InvariantCulture, $"{a / b}.{((a * 100) / b) % 100:D2}×");
}
