using System.Diagnostics;
using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Matrix;
using S2.Routing.Routing;
using S2.Routing.Traffic;

namespace S2.Routing.Harness;

/// <summary>
/// S2 R2 — the path source, the crossover, and the attribution lag.
/// </summary>
/// <remarks>
/// <para>
/// <b>R2 is the task the prescribed order exists to reach, and it arrives smaller than it was
/// planned.</b> <c>plans/0010</c> framed it as two axes; <c>adr/0041</c> has since settled one of them
/// — volume is attributed by the Traveller, not the District pair — on correctness grounds, and
/// explicitly left R2 the price rather than the choice. What remains open is the <b>path source</b>,
/// and R1 made it the last thing standing between the spike and its verdict on DSDV.
/// </para>
/// <para>
/// <b>It also arrives with a rung the plan does not have.</b> <c>adr/0041</c> requires a vehicular
/// Traveller to increment the Segment it enters, every Tick — so what a Traveller needs is a
/// <i>next Segment</i>, and a path is only one way to supply it. A next-hop table supplies it and
/// stores no path at all, which is what distance-vector routing <b>is</b>. The plan's condition for
/// retiring R4 — <i>"if Statistical Trips need no concrete path"</i> — was written before that ADR and
/// reads differently after it. See <see cref="NextHopTable"/>.
/// </para>
/// </remarks>
internal static class TrafficReport
{
    /// <summary>
    /// District rungs. Shorter than R1's sweep, and the reason is a hard limit rather than a choice:
    /// the shared route store is <c>n²</c> variable-length sequences, which R1 priced at 4.06 GiB at
    /// 4,096 Districts. Everything past the last rung here is reported from arithmetic and labelled.
    /// </summary>
    private static readonly int[] DistrictRungs = [4, 8, 10, 11, 16, 20];

    /// <summary>The rung every non-sweep section runs at: <c>CONTEXT.md</c> → District's anchor.</summary>
    private const int AnchorPerSide = 11;

    /// <summary>Congestion cycles, in Ticks. The axis R2a's crossover lives on.</summary>
    private static readonly int[] CycleRungs = [1, 10, 25, 50, 100, 200];

    /// <summary>
    /// Vehicles in flight. S4 task 2's derived band, plus the top of the peaking correction
    /// <c>plans/0010</c> owes <c>spike-results</c> — 56,000 × a 2–3× peaking factor is 110k–170k.
    /// </summary>
    private static readonly int[] InFlightRungs = [37_000, 56_000, 111_000, 170_000];

    /// <summary>
    /// Stress thresholds, Q16.16 <c>volume / capacity</c>. <b>Swept, because the corpus states none</b>
    /// — <c>CONTEXT.md</c> → Stress gives the mechanism and no number, exactly as it gives the
    /// Microscopic Cap none.
    /// </summary>
    private static readonly int[] ThresholdRungs = [52_429, 65_536, 78_643];

    /// <summary>Searched routes in the pool. See <c>RouteStore.ForSearchedPool</c> for why a pool.</summary>
    private const int SearchedPool = 3_000;

    /// <summary>Legs sampled for the detour columns. Reported per rung, per the board's owed finding.</summary>
    private const int DetourSamples = 300;

    /// <summary>Searches timed for the searched rung's per-Leg cost.</summary>
    private const int TimedSearches = 200;

    /// <summary>The fleet R2.2 and R2b run at. Stated rather than implied — it is not 1M.</summary>
    private const int WorkingFleet = 40_000;

    private const int WarmTicks = 40;
    private const int RateTicks = 60;
    private const int SurgeWindowTicks = 240;

    /// <summary>Share of the fleet the surge redirects, in hundredths.</summary>
    private const int SurgeShareHundredths = 40;

    public static string Run()
    {
        var report = new StringBuilder();

        report.AppendLine("## S2 R2 — the path source, the crossover, and the attribution lag");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(
            "**The attribution axis is not open and nothing here reopens it.** "
            + "`adr/0041` settled it on three correctness grounds and recorded which way that cut "
            + "against convenience — aggregate is the cheaper scheme and the one `03 §3.3` already "
            + "wrote down. R2a prices what rejecting it cost. **The path source is open**, and R1 left "
            + "it carrying the whole of what remains of the DSDV question.");
        report.AppendLine();
        report.AppendLine(
            "Every figure is at the **morning peak**, on the same synthetic monocentric field R1 "
            + "used, at `v/c = 1.2` and imbalance 0.50. Travellers advance by each arc's own "
            + "free-flow traversal time and **nothing feeds back** — a jam does not slow the "
            + "Travellers in it. That omission is deliberate: a feedback loop would put an unargued "
            + "shape inside the lag figures R2b exists to measure.");
        report.AppendLine();

        var graph = GraphGenerator.Build(GraphParameters.Working);
        int[] arcTicks = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);
        var reverse = ReverseArcs.Of(graph);
        var anchor = Districts.Partition(graph, AnchorPerSide);
        var sampler = new OdSampler(graph);

        var pool = RouteStore.ForSearchedPool(
            graph, sampler, arcTicks, SearchedPool, CounterHash.Seed, out int poolFound);

        AppendLadder(report, graph, reverse, anchor, arcTicks, sampler, pool, poolFound);

        // Built once and threaded through the remaining sections. Rebuilding per section would put
        // three unrelated build costs inside sections that are not measuring builds.
        var shared = RouteStore.ForDistrictPairs(graph, anchor, arcTicks, new OneToAll(graph));
        var nextHop = NextHopTable.Build(graph, reverse, anchor, arcTicks);

        AppendCrossingRate(report, graph, anchor, arcTicks, pool, shared, nextHop);
        AppendCrossover(report, graph, anchor, arcTicks, shared);
        AppendLag(report, graph, anchor, arcTicks, pool, shared, nextHop);
        AppendVerdict(report);

        return report.ToString();
    }

    // --- R2.1 -------------------------------------------------------------------------------------

    private static void AppendLadder(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        Districts anchor,
        int[] arcTicks,
        OdSampler sampler,
        RouteStore pool,
        int poolFound)
    {
        report.AppendLine("### R2.1 — the path source ladder, and the rung the plan did not have");
        report.AppendLine();
        report.AppendLine(
            "Three ways a Traveller can be told which Segment to enter next. **The third is not in "
            + "`plans/0010`** — it follows from `adr/0041`, which makes a Traveller need a *next "
            + "Segment* every Tick rather than a *path*, and a next-hop table is exactly that and "
            + "stores no path at all. That is distance-vector's data structure, so measuring only "
            + "searched-against-shared would have answered a question the design had moved past.");
        report.AppendLine();

        long sharedStart = Stopwatch.GetTimestamp();
        var shared = RouteStore.ForDistrictPairs(graph, anchor, arcTicks, new OneToAll(graph));
        long sharedBuild = Nanoseconds(sharedStart);

        long hopStart = Stopwatch.GetTimestamp();
        var nextHop = NextHopTable.Build(graph, reverse, anchor, arcTicks);
        long hopBuild = Nanoseconds(hopStart);

        long searchNs = TimeSearches(graph, sampler, arcTicks);
        long sharedSpawn = TimeSharedSpawn(shared, anchor);

        long searchedResident =
            (long)InFlightRungs[1] * shared.MeanLength * sizeof(int);

        var detour = MeasureDetour(graph, anchor, arcTicks, sampler);

        report.AppendLine(
            "| Rung | Build | Resident | Per Leg at spawn | Per crossing | Detour, mean | p90 |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|");

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **searched** | — | {Bytes(searchedResident)} | {searchNs:N0} ns "
            + $"| {PerCrossing(graph, anchor, arcTicks, PathSource.Searched, pool, null):N0} ns "
            + $"| 0.00% | 0.00% |"));

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **shared** | {Milliseconds(sharedBuild)} | {Bytes(shared.ResidentBytes)} "
            + $"| {sharedSpawn:N0} ns "
            + $"| {PerCrossing(graph, anchor, arcTicks, PathSource.Shared, shared, null):N0} ns "
            + $"| {Hundredths(detour.SharedMean)}% | {Hundredths(detour.SharedP90)}% |"));

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **next-hop** | {Milliseconds(hopBuild)} | {Bytes(nextHop.ResidentBytes)} | 0 ns "
            + $"| {PerCrossing(graph, anchor, arcTicks, PathSource.NextHop, null, nextHop):N0} ns "
            + $"| {Hundredths(detour.HopMean)}% | {Hundredths(detour.HopP90)}% |"));

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"At the anchor, {anchor.Count:N0} Districts. *Searched* resident is "
            + $"`in-flight × mean route × 4 B` at the derived 56,000, **not** the pool's own "
            + $"footprint — the pool reuses each route across many Travellers and would understate "
            + $"it. The pool is {poolFound:N0} routes of {SearchedPool:N0} drawn; the rest found no "
            + $"route and are excluded rather than counted as short ones."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The detour columns are the finding, and `adr/0041` says they should not exist.** That "
            + $"ADR calls the path source *\"a performance axis with no correctness content\"*. It has "
            + $"correctness content: two of the three rungs aim a Traveller at a District "
            + $"**representative** rather than at where it is going. A shared route is coarse at both "
            + $"ends — the Traveller must reach the origin representative before the stored route "
            + $"means anything — while a next-hop table is followed from wherever the Traveller "
            + $"actually is, so it is **exact on the origin side and coarse only on the destination "
            + $"side**. {detour.Samples:N0} Legs sampled, drives drawn across the whole map."));
        report.AppendLine();
        report.AppendLine(
            "**And there is a second correctness cost that is structural rather than statistical.** "
            + "Under either coarse rung, *every* Trip bound for a District arrives through that "
            + "District's one representative node — a shared route ends there and a next-hop column "
            + "is a tree rooted there. So the arcs into a representative carry the whole of a "
            + "District's inbound traffic, and R2b measures the consequence: a monocentric surge "
            + "drives them to a `v/c` an order of magnitude past what the same surge produces under "
            + "searched routes. **The representative is not a summary of the District under these "
            + "rungs; it is a hole every Trip is threaded through**, and a fidelity model that "
            + "promotes on `volume / capacity` would promote there and nowhere else.");
        report.AppendLine();
        report.AppendLine(
            "Measured at **node granularity**, so both percentages are an **upper bound**: adding the "
            + "two Access Point remainders leaves each detour unchanged in Ticks and raises the "
            + "denominator, which lowers every percentage above. Stated because the alternative — "
            + "quoting them as exact — is the shape of error this spike has already published once.");
        report.AppendLine();

        AppendLadderSweep(report, graph, reverse, arcTicks);
    }

    private static void AppendLadderSweep(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, int[] arcTicks)
    {
        report.AppendLine("#### Resident size against District count");
        report.AppendLine();
        report.AppendLine(
            "**The two coarse rungs scale on different axes and cross.** A route store is "
            + "`n² × mean route`; a next-hop table is `nodes × n`. The store is quadratic in District "
            + "count and the table is linear in it, so the rung that is cheaper depends entirely on "
            + "where District count lands — which R1 left as an open trade rather than a settled "
            + "number.");
        report.AppendLine();
        report.AppendLine(
            "| Districts | Route store | Next-hop table | Mean route | vs the world's 172.3 MiB |");
        report.AppendLine("|---:|---:|---:|---:|---|");

        // The whole sweep is walked once before any of it is timed. R1's cold-build column needed
        // four warm-up schemes before it stopped falling smoothly with the swept axis, and the board
        // records the general form: an artefact that varies with the swept axis is not
        // distinguishable from a result. Nothing here is a timing column, but the same discipline
        // costs one pass and removes the question.
        foreach (int perSide in DistrictRungs)
        {
            var districts = Districts.Partition(graph, perSide);
            var store = RouteStore.ForDistrictPairs(graph, districts, arcTicks, new OneToAll(graph));
            var table = NextHopTable.Build(graph, reverse, districts, arcTicks);

            long world = 172L * 1024 * 1024;
            string standing =
                store.ResidentBytes > world && table.ResidentBytes > world ? "**both exceed it**"
                : store.ResidentBytes > world ? "the store exceeds it"
                : table.ResidentBytes > world ? "the table exceeds it"
                : "both inside it";

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {districts.Count:N0} | {Bytes(store.ResidentBytes)} "
                + $"| {Bytes(table.ResidentBytes)} | {store.MeanLength:N0} Segments | {standing} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "The sweep stops at the last rung either structure can actually be built at on this "
            + "machine. R1 already reports the store's arithmetic beyond it — 4.06 GiB at 4,096 "
            + "Districts — and the table's is `nodes × n × 4 B`, which at 4,096 is 261 MiB. "
            + "**Neither is a rung anybody should reach**, and that is the point of printing them.");
        report.AppendLine();
    }

    private readonly record struct Detour(
        int SharedMean, int SharedP90, int HopMean, int HopP90, int Samples);

    /// <summary>
    /// The travel time a Traveller actually experiences under each coarse rung, against the searched
    /// route it would have taken.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Composed from exact costs rather than from re-searches, which is what makes it cheap enough to
    /// sample properly: one forward one-to-all from each sampled origin, plus one from every District
    /// representative held for the whole section. Then, writing <c>c(a→b)</c> for the exact cost,
    /// </para>
    /// <para>
    /// <c>truth = c(origin→dest)</c>;
    /// <c>shared = c(origin→rep_from) + c(rep_from→rep_to) + c(rep_to→dest)</c>;
    /// <c>next-hop = c(origin→rep_to) + c(rep_to→dest)</c>.
    /// </para>
    /// <para>
    /// The next-hop line is not an approximation of the walk — following a shortest-path tree from the
    /// origin to the destination representative <i>is</i> <c>c(origin→rep_to)</c>, exactly. The
    /// difference between the two coarse rungs is therefore a single term, and it is the whole of the
    /// origin-side coarseness.
    /// </para>
    /// </remarks>
    private static Detour MeasureDetour(
        RoadGraph graph, Districts districts, int[] arcTicks, OdSampler sampler)
    {
        var search = new OneToAll(graph);
        var fromRepresentative = new int[districts.Count][];

        for (int district = 0; district < districts.Count; district++)
        {
            int representative = districts.Representative[district];
            if (representative < 0)
            {
                continue;
            }

            search.Run(representative, arcTicks);

            var row = new int[graph.Nodes];
            for (int node = 0; node < graph.Nodes; node++)
            {
                row[node] = search.CostOf(node);
            }

            fromRepresentative[district] = row;
        }

        var sharedError = new List<int>(DetourSamples);
        var hopError = new List<int>(DetourSamples);

        for (int query = 0; query < DetourSamples; query++)
        {
            var origin = sampler.Origin(CounterHash.Seed, (ulong)query, Modes.Car);
            var goal = sampler.Destination(
                CounterHash.Seed, (ulong)query, Modes.Car, origin, radiusTiles: 0);

            int originNode = graph.SegmentNodeA[origin.Segment];
            int destNode = graph.SegmentNodeA[goal.Segment];

            int from = districts.OfNode[originNode];
            int to = districts.OfNode[destNode];

            int[]? outward = fromRepresentative[from];
            int[]? inward = fromRepresentative[to];
            if (outward is null || inward is null)
            {
                continue;
            }

            int repFrom = districts.Representative[from];
            int repTo = districts.Representative[to];

            search.Run(originNode, arcTicks);
            int truth = search.CostOf(destNode);
            int toRepFrom = search.CostOf(repFrom);
            int toRepTo = search.CostOf(repTo);

            int tail = inward[destNode];
            int between = outward[repTo];

            if (truth <= 0 || truth >= OneToAll.Unreachable
                || tail >= OneToAll.Unreachable
                || between >= OneToAll.Unreachable
                || toRepFrom >= OneToAll.Unreachable
                || toRepTo >= OneToAll.Unreachable)
            {
                continue;
            }

            long shared = (long)toRepFrom + between + tail;
            long hop = (long)toRepTo + tail;

            sharedError.Add((int)((shared - truth) * 10_000 / truth));
            hopError.Add((int)((hop - truth) * 10_000 / truth));
        }

        sharedError.Sort();
        hopError.Sort();

        return new Detour(
            MeanOf(sharedError), QuantileOf(sharedError, 90),
            MeanOf(hopError), QuantileOf(hopError, 90),
            sharedError.Count);
    }

    private static long TimeSearches(RoadGraph graph, OdSampler sampler, int[] arcTicks)
    {
        var search = new PointToPoint(graph, arcTicks);

        // Warmed over the same query shape before timing, for the reason the board records against
        // R1: the first row a process times is the least trustworthy number in the table.
        for (int query = 0; query < 40; query++)
        {
            var warmOrigin = sampler.Origin(CounterHash.Seed, (ulong)query, Modes.Car);
            var warmGoal = sampler.Destination(
                CounterHash.Seed, (ulong)query, Modes.Car, warmOrigin, radiusTiles: 0);
            search.Bootstrap(warmOrigin, warmGoal, Modes.Car, HeuristicKind.Chebyshev);
            search.Expand();
        }

        long start = Stopwatch.GetTimestamp();
        long found = 0;

        for (int query = 0; query < TimedSearches; query++)
        {
            var origin = sampler.Origin(CounterHash.Seed, (ulong)(query + 5_000), Modes.Car);
            var goal = sampler.Destination(
                CounterHash.Seed, (ulong)(query + 5_000), Modes.Car, origin, radiusTiles: 0);

            search.Bootstrap(origin, goal, Modes.Car, HeuristicKind.Chebyshev);
            found += search.Expand().CostTicks;
        }

        long elapsed = Nanoseconds(start);
        Sink(found);
        return IntegerMath.FloorDiv(elapsed, TimedSearches);
    }

    private static long TimeSharedSpawn(RouteStore shared, Districts districts)
    {
        const int Spawns = 4_000_000;
        long sink = 0;

        long start = Stopwatch.GetTimestamp();
        for (int spawn = 0; spawn < Spawns; spawn++)
        {
            int pair = spawn % shared.Count;
            sink += shared.Length(pair);
        }

        long elapsed = Nanoseconds(start);
        Sink(sink);
        return IntegerMath.FloorDiv(elapsed * 1_000, Spawns);
    }

    /// <summary>
    /// Nanoseconds per Segment boundary crossing, over a warmed fleet — <b>path source read and
    /// direct attribution together</b>, because that is the pair a Traveller pays at a crossing.
    /// </summary>
    private static long PerCrossing(
        RoadGraph graph,
        Districts districts,
        int[] arcTicks,
        PathSource source,
        RouteStore? routes,
        NextHopTable? nextHop)
    {
        var fleet = new Fleet(
            graph, districts, arcTicks, source, routes, nextHop, WorkingFleet, CounterHash.Seed);

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
        }

        long start = Stopwatch.GetTimestamp();
        long crossings = 0;
        for (int tick = 0; tick < RateTicks; tick++)
        {
            fleet.Advance();
            crossings += fleet.Crossings;
        }

        long elapsed = Nanoseconds(start);
        return crossings == 0 ? 0 : IntegerMath.FloorDiv(elapsed, crossings);
    }

    // --- R2.2 -------------------------------------------------------------------------------------

    private static void AppendCrossingRate(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        int[] arcTicks,
        RouteStore pool,
        RouteStore shared,
        NextHopTable nextHop)
    {
        report.AppendLine("### R2.2 — the crossing rate, which `adr/0041` assumed and S2 can measure");
        report.AppendLine();
        report.AppendLine(
            "`adr/0041` prices direct attribution from *\"a vehicle crosses about one Segment per "
            + "Tick\"*, and names the rate as its own revisit trigger: *\"if the Segment turns out "
            + "much shorter than a block — S2 owns the road-density figure that decides it — the "
            + "crossing rate rises and this should be re-priced before it is re-argued.\"* R0 "
            + "measured the density. This is the rate.");
        report.AppendLine();
        report.AppendLine(
            "**Reported at free flow and at the peak, because the ADR's estimate is a free-flow one "
            + "and the simulation is not.** A Segment under BPR at `v/c = 1.2` takes about 1.3× its "
            + "free-flow time, so congestion *lowers* the crossing rate and lowers direct "
            + "attribution's cost with it. Quoting only the congested figure would credit the scheme "
            + "for a saving the jam paid for.");
        report.AppendLine();
        report.AppendLine(
            "| Path source | Arc costs | Crossings/vehicle/Tick | Arrivals/Tick | Mean route "
            + "| Volume conserved | Bounded |");
        report.AppendLine("|---|---|---:|---:|---:|---|---:|");

        foreach (var source in new[] { PathSource.Searched, PathSource.Shared, PathSource.NextHop })
        {
            var routes = source switch
            {
                PathSource.Searched => pool,
                _ => shared,
            };

            foreach (bool peak in new[] { false, true })
            {
                int[] costs = peak ? arcTicks : graph.ArcCarTicks;

                var fleet = new Fleet(
                    graph, districts, costs, source, routes,
                    source == PathSource.NextHop ? nextHop : null,
                    WorkingFleet, CounterHash.Seed);

                for (int tick = 0; tick < WarmTicks; tick++)
                {
                    fleet.Advance();
                }

                long crossings = 0;
                long arrivals = 0;
                long bounded = 0;
                for (int tick = 0; tick < RateTicks; tick++)
                {
                    fleet.Advance();
                    crossings += fleet.Crossings;
                    arrivals += fleet.Arrivals;
                    bounded += fleet.Bounded;
                }

                int rate = (int)IntegerMath.FloorDiv(
                    crossings * 100, (long)WorkingFleet * RateTicks);

                long expected = (long)(fleet.Size - fleet.Unplaced()) * Fixed.One;
                long actual = fleet.TotalVolume();
                string conserved = actual == expected
                    ? "yes"
                    : string.Create(CultureInfo.InvariantCulture, $"**NO — {actual - expected:+#;-#;0}**");

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {source} | {(peak ? "morning peak" : "free flow")} | {Hundredths(rate)} "
                    + $"| {IntegerMath.FloorDiv(arrivals, RateTicks):N0} "
                    + $"| {routes.MeanLength:N0} Segments | {conserved} | {bounded:N0} |"));
            }
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Fleet of {WorkingFleet:N0}, warmed {WarmTicks} Ticks and measured over {RateTicks}. "
            + $"**Scale the rate, not the fleet**: `adr/0041`'s ~80,000 increment/decrement pairs per "
            + $"Tick is the in-flight count times this column."));
        report.AppendLine();
        report.AppendLine(
            "**The *volume conserved* column is the one that earned its place.** `adr/0041` requires "
            + "*\"summed Segment volume equals the number of in-flight vehicular Travellers, every "
            + "Tick\"*, and names the failure it catches: *\"a Traveller that vanishes without "
            + "decrementing destroys the reading permanently, which is an `adr/0006`-class defect "
            + "that presents as a road that looks busy forever.\"* The next-hop rung was written "
            + "with exactly that defect — arrival was tested *after* entering the last arc — and the "
            + "first capture reported a peak `v/c` of **883×** without anything else in the report "
            + "looking wrong. **The invariant the ADR asked for is what found it**, on the first run "
            + "it was printed. *Bounded* is the advance loop's crossings-per-Tick guard, and a "
            + "non-zero figure means a zero-cost arc: a graph defect, not a result.");
        report.AppendLine();
    }

    // --- R2a --------------------------------------------------------------------------------------

    private static void AppendCrossover(
        StringBuilder report, RoadGraph graph, Districts anchor, int[] arcTicks, RouteStore shared)
    {
        report.AppendLine("### R2a — the crossover, priced rather than chosen");
        report.AppendLine();
        report.AppendLine(
            "The two schemes scale on **independent** axes — direct with vehicles in flight, "
            + "aggregate with `District count² × route length` — so there is a congestion-cycle "
            + "length at which they cost the same, and S2 can find it rather than assume it. "
            + "`adr/0041` has already chosen direct; this is what that choice costs.");
        report.AppendLine();

        report.AppendLine("#### Direct, against vehicles in flight");
        report.AppendLine();
        report.AppendLine("| In flight | Crossings/Tick | Attribution/Tick | Per crossing | Standing |");
        report.AppendLine("|---:|---:|---:|---:|---|");

        long directAtMean = 0;

        foreach (int inFlight in InFlightRungs)
        {
            (long perTick, long crossings) = TimeDirect(graph, anchor, arcTicks, shared, inFlight);

            if (inFlight == InFlightRungs[1])
            {
                directAtMean = perTick;
            }

            string standing = inFlight switch
            {
                37_000 => "band floor",
                56_000 => "**the derived Day-average**",
                111_000 => "band ceiling / 2× peak",
                _ => "3× peak",
            };

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {inFlight:N0} | {crossings:N0} | {perTick:N0} ns | "
                + $"{(crossings == 0 ? 0 : IntegerMath.FloorDiv(perTick * 1_000, crossings)):N0} ps "
                + $"| {standing} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "Timed over the **real** crossing distribution — the arcs an advancing fleet actually "
            + "entered and left, captured and replayed — rather than over drawn indices, because "
            + "whether the volume column sits in L2 is a property of how scattered those indices are "
            + "and drawing them would have measured the draw.");
        report.AppendLine();

        report.AppendLine("#### Aggregate, against District count");
        report.AppendLine();
        report.AppendLine("| Districts | Pairs in flight | Arc writes | Per cycle | Crossover cycle |");
        report.AppendLine("|---:|---:|---:|---:|---:|");

        foreach (int perSide in DistrictRungs)
        {
            var districts = Districts.Partition(graph, perSide);
            var store = RouteStore.ForDistrictPairs(graph, districts, arcTicks, new OneToAll(graph));

            var fleet = new Fleet(
                graph, districts, arcTicks, PathSource.Shared, store, null,
                InFlightRungs[1], CounterHash.Seed);

            for (int tick = 0; tick < WarmTicks; tick++)
            {
                fleet.Advance();
            }

            var volume = new int[graph.Volume.Length];
            long writes = Aggregate.Smear(graph, store, fleet.InFlight, arcTicks, volume);

            long start = Stopwatch.GetTimestamp();
            const int Cycles = 8;
            for (int cycle = 0; cycle < Cycles; cycle++)
            {
                Aggregate.Smear(graph, store, fleet.InFlight, arcTicks, volume);
            }

            long perCycle = IntegerMath.FloorDiv(Nanoseconds(start), Cycles);

            int occupied = 0;
            foreach (int count in fleet.InFlight)
            {
                if (count > 0)
                {
                    occupied++;
                }
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {districts.Count:N0} | {occupied:N0} | {writes:N0} | {Milliseconds(perCycle)} "
                + $"| {(directAtMean == 0 ? 0 : IntegerMath.FloorDiv(perCycle, directAtMean)):N0} "
                + $"Ticks |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"*Crossover cycle* is the cycle length at which one smear costs what direct attribution "
            + $"costs over the same span, at the derived {InFlightRungs[1]:N0} in flight. **Longer "
            + $"than this, aggregate is cheaper; shorter, direct is.** `adr/0041`'s own arithmetic put "
            + $"it near 10 Ticks from an assumed crossing rate; R2.2 measured the rate."));
        report.AppendLine();
        report.AppendLine(
            "The smear is the **conserving** form — a Traveller on a route of total time `T` "
            + "contributes `t_s / T` to each Segment, so the shares sum to one and `adr/0041`'s "
            + "invariant holds. Adding the whole pair count to every Segment would be cheaper per "
            + "write and would put one vehicle on fifty Segments at once. **A rejected alternative "
            + "implemented weakly makes the price of rejecting it look smaller than it is.**");
        report.AppendLine();

        AppendPeaking(report, directAtMean, graph, anchor, arcTicks, shared);
    }

    private static void AppendPeaking(
        StringBuilder report,
        long directAtMean,
        RoadGraph graph,
        Districts anchor,
        int[] arcTicks,
        RouteStore shared)
    {
        var fleet = new Fleet(
            graph, anchor, arcTicks, PathSource.Shared, shared, null,
            InFlightRungs[1], CounterHash.Seed);

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
        }

        var volume = new int[graph.Volume.Length];
        long start = Stopwatch.GetTimestamp();
        const int Cycles = 8;
        for (int cycle = 0; cycle < Cycles; cycle++)
        {
            Aggregate.Smear(graph, shared, fleet.InFlight, arcTicks, volume);
        }

        long perCycle = IntegerMath.FloorDiv(Nanoseconds(start), Cycles);

        report.AppendLine("#### Where the crossover inverts, across the peaking sweep");
        report.AppendLine();
        report.AppendLine(
            "`plans/0010`: *\"only one side of it moves — direct attribution scales with vehicles in "
            + "flight and is peak-sensitive; aggregate scales with `zone count² × route length` and "
            + "is not. **Report the peaking factor at which the crossover inverts.**\"* At the "
            + "anchor's District count:");
        report.AppendLine();
        report.AppendLine("| Congestion cycle | Aggregate/Tick | Peaking factor that inverts it |");
        report.AppendLine("|---:|---:|---:|");

        foreach (int cycle in CycleRungs)
        {
            long aggregatePerTick = IntegerMath.FloorDiv(perCycle, cycle);
            int factor = directAtMean == 0
                ? 0
                : (int)IntegerMath.FloorDiv(aggregatePerTick * 100, directAtMean);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {cycle} Ticks | {aggregatePerTick:N0} ns | {Hundredths(factor)}× |"));
        }

        report.AppendLine();
        report.AppendLine(
            "A factor **below 1.00×** means aggregate is already the cheaper scheme at the "
            + "Day-average load and no peak is needed to invert it. **Above 3.00× means the "
            + "inversion is out of reach**, because the corpus's own generator mix caps the peak "
            + "near 3× — 79% of Trips are commutes and school runs, and `02 §1.2`'s sun arc has "
            + "five phases. The peaking factor itself is still unsized: decision 5a.");
        report.AppendLine();
    }

    private static (long PerTick, long Crossings) TimeDirect(
        RoadGraph graph, Districts districts, int[] arcTicks, RouteStore routes, int inFlight)
    {
        var fleet = new Fleet(
            graph, districts, arcTicks, PathSource.Shared, routes, null, inFlight, CounterHash.Seed)
        {
            CrossingLog = new int[inFlight * 4],
        };

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
        }

        int[] log = fleet.CrossingLog!;
        int entries = fleet.CrossingLogCount;
        var volume = new int[graph.Volume.Length];

        // Replayed rather than measured in place, so the figure is attribution and not the advance
        // loop around it. The indices are the ones a real fleet just produced.
        const int Repeats = 32;
        long start = Stopwatch.GetTimestamp();

        for (int repeat = 0; repeat < Repeats; repeat++)
        {
            for (int entry = 0; entry < entries; entry += 2)
            {
                if (log[entry] >= 0)
                {
                    volume[graph.VolumeIndex(log[entry])] -= Fixed.One;
                }

                if (log[entry + 1] >= 0)
                {
                    volume[graph.VolumeIndex(log[entry + 1])] += Fixed.One;
                }
            }
        }

        long elapsed = IntegerMath.FloorDiv(Nanoseconds(start), Repeats);

        long checksum = 0;
        foreach (int value in volume)
        {
            checksum += value;
        }

        Sink(checksum);
        return (elapsed, IntegerMath.FloorDiv(entries, 2));
    }

    // --- R2b --------------------------------------------------------------------------------------

    private static void AppendLag(
        StringBuilder report,
        RoadGraph graph,
        Districts anchor,
        int[] arcTicks,
        RouteStore pool,
        RouteStore shared,
        NextHopTable nextHop)
    {
        report.AppendLine("### R2b — the lag, and the peak");
        report.AppendLine();
        report.AppendLine(
            "`03 §3.3` confesses the aggregate scheme's defect in its own text: *\"a jam propagates "
            + "backward at roughly 15 km/h — faster than any cycle worth running — so a cycle-driven "
            + "region always lags the jam during exactly the event it exists to capture.\"* That "
            + "admission is why `03 §3.3` had to invent **force-promotion on downstream blocking** as "
            + "compensation. This measures the lag it was compensating for.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The jam is a surge: {SurgeShareHundredths}% of a {WorkingFleet:N0} fleet redirected to "
            + $"the central District — the monocentric morning peak R1 modelled — **replacing** "
            + $"Travellers rather than adding them, so the surge changes where the fleet is going and "
            + $"not how large it is. Lag is Ticks between the watched Segment's **true** `v/c` "
            + $"crossing the threshold and the scheme's own reading crossing it."));
        report.AppendLine();

        int centre = ((AnchorPerSide / 2) * AnchorPerSide) + (AnchorPerSide / 2);

        report.AppendLine(
            "| Path source | Cycle | Direct lag | Aggregate lag | Watched peak, direct "
            + "| Watched peak, aggregate | Peak, direct | Peak, aggregate | Compression |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var source in new[] { PathSource.Searched, PathSource.Shared, PathSource.NextHop })
        {
            foreach (int cycle in CycleRungs)
            {
                var routes = source == PathSource.Searched ? pool : shared;
                var outcome = RunSurge(
                    graph, anchor, arcTicks, source, routes, nextHop, shared, cycle,
                    Fixed.One, centre);

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {source} | {cycle} | {outcome.DirectLag} "
                    + $"| {(outcome.AggregateLag < 0 ? "**never**" : outcome.AggregateLag.ToString(CultureInfo.InvariantCulture))} "
                    + $"| {Hundredths(Percent(outcome.WatchedDirect))}% "
                    + $"| {Hundredths(Percent(outcome.WatchedAggregate))}% "
                    + $"| {Hundredths(Percent(outcome.PeakDirect))}% "
                    + $"| {Hundredths(Percent(outcome.PeakAggregate))}% "
                    + $"| {Compression(outcome.PeakDirect, outcome.PeakAggregate)} |"));
            }
        }

        report.AppendLine();
        report.AppendLine(
            "**Direct lag is zero by construction and is printed anyway**, because a column that "
            + "cannot be anything else is the one worth checking: a non-zero entry would mean the "
            + "advance loop and the volume column had come apart.");
        report.AppendLine();
        report.AppendLine(
            "**A column of identical *never*s is the shape of a broken instrument, so the two watched "
            + "columns are printed to tell the two apart.** They give the highest `v/c` each scheme "
            + "ever reads on the *same* arc across the window: if aggregate reaches a large number "
            + "that merely arrived late, the lag is a cadence problem; if it never reaches one, the "
            + "smear has put the volume **somewhere else** and no cadence recovers it. **The columns say it is the "
            + "second**, and *never* appears at a one-Tick cycle — where there is no cadence left to "
            + "blame — which is the same conclusion arrived at from the other side.");
        report.AppendLine();
        report.AppendLine(
            "That is `adr/0041`'s first argument, measured: *\"a Traveller experiences congestion on "
            + "its own route and deposits congestion on the District pair's route, so the failure "
            + "feeds a **different** detector, watching different Segments.\"* The lag was never the "
            + "whole defect — it is the part that has a number, and the part that does not is worse. "
            + "It also means **force-promotion loses its remaining bundled justification here** and "
            + "must stand on `03 §3.3`'s second argument alone, which the board already records as "
            + "owed.");
        report.AppendLine();
        report.AppendLine(
            "**Compression is the column `plans/0010` actually asked for** — *\"report peak Segment "
            + "volume under each on the same O-D distribution. A scheme that understates the peak "
            + "promotes late, and `adr/0007` demotes on a *lower* threshold, so an understated peak "
            + "also demotes early.\"* It is aggregate's peak over direct's.");
        report.AppendLine();
        report.AppendLine(
            "**Read the `v/c` columns comparatively and never as absolute levels.** A Traveller here "
            + "passes through a Segment regardless of how loaded it is — there is no queue, because "
            + "`plans/0010` forbids this spike simulating traffic — so `v/c` is unbounded and a "
            + "monocentric surge drives it far past anything a real Segment reaches. **What is being "
            + "compared is two readings of one load**, and that comparison is unaffected.");
        report.AppendLine();

        AppendThresholdSweep(report, graph, anchor, arcTicks, shared, nextHop, centre);
    }

    private static void AppendThresholdSweep(
        StringBuilder report,
        RoadGraph graph,
        Districts anchor,
        int[] arcTicks,
        RouteStore shared,
        NextHopTable nextHop,
        int centre)
    {
        report.AppendLine("#### Against the threshold, which the corpus does not state");
        report.AppendLine();
        report.AppendLine(
            "`CONTEXT.md` → Stress gives the mechanism — *\"Microscopic above a high threshold and "
            + "back below a lower one\"* — and no numbers, exactly as it gives the Microscopic Cap "
            + "none. So the threshold is swept and not chosen, at a 50-Tick cycle and the shared "
            + "path source.");
        report.AppendLine();
        report.AppendLine("| Threshold | Direct lag | Aggregate lag | Segments over, direct | over, aggregate |");
        report.AppendLine("|---:|---:|---:|---:|---:|");

        foreach (int threshold in ThresholdRungs)
        {
            var outcome = RunSurge(
                graph, anchor, arcTicks, PathSource.Shared, shared, nextHop, shared, cycle: 50,
                threshold, centre);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Hundredths(Percent(threshold))}% | {outcome.DirectLag} "
                + $"| {(outcome.AggregateLag < 0 ? "**never**" : outcome.AggregateLag.ToString(CultureInfo.InvariantCulture))} "
                + $"| {outcome.OverDirect:N0} | {outcome.OverAggregate:N0} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "*Segments over* is how many the scheme places above the threshold at the end of the "
            + "window, and it is the column that decides the **Microscopic Cap**'s exposure: under "
            + "`adr/0007` those are the Segments competing for slots, and a scheme that names a "
            + "different set names a different city. S2 does not set the Cap — that needs a built "
            + "traffic model — but this is the first quantitative thing anyone has been able to say "
            + "about how many Segments would want one.");
        report.AppendLine();
    }

    private readonly record struct SurgeOutcome(
        int DirectLag, int AggregateLag, int PeakDirect, int PeakAggregate,
        int OverDirect, int OverAggregate, int WatchedDirect, int WatchedAggregate);

    private static SurgeOutcome RunSurge(
        RoadGraph graph,
        Districts districts,
        int[] arcTicks,
        PathSource source,
        RouteStore? routes,
        NextHopTable? nextHop,
        RouteStore smearRoutes,
        int cycle,
        int threshold,
        int destination)
    {
        var fleet = new Fleet(
            graph, districts, arcTicks, source, routes, nextHop, WorkingFleet, CounterHash.Seed);

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
        }

        // Only arcs below the threshold at the moment of the surge are eligible to be watched. The
        // plan asks for the Ticks between a Segment's true v/c *crossing* the threshold and the
        // scheme reporting it, and a Segment already over it has not crossed anything.
        var eligible = new bool[graph.Arcs];
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            eligible[arc] = Aggregate.Ratio(graph, arc, fleet.Volume) < threshold;
        }

        fleet.Surge(
            (int)IntegerMath.FloorDiv((long)WorkingFleet * SurgeShareHundredths, 100), destination);

        var aggregateVolume = new int[graph.Volume.Length];

        int watched = -1;
        int directTick = -1;
        int aggregateTick = -1;
        int peakDirect = 0;
        int peakAggregate = 0;
        int watchedDirect = 0;
        int watchedAggregate = 0;

        for (int tick = 0; tick < SurgeWindowTicks; tick++)
        {
            fleet.Advance();

            if (tick % cycle == 0)
            {
                Aggregate.Smear(graph, smearRoutes, fleet.InFlight, arcTicks, aggregateVolume);
            }

            if (watched < 0)
            {
                for (int arc = 0; arc < graph.Arcs; arc++)
                {
                    if (eligible[arc] && Aggregate.Ratio(graph, arc, fleet.Volume) >= threshold)
                    {
                        watched = arc;
                        directTick = tick;
                        break;
                    }
                }
            }

            if (watched >= 0)
            {
                // The MAXIMUM either scheme ever reads on the watched arc, not its reading at the end
                // of the window. A surge passes: by the last Tick the jam has drained and both
                // schemes read something unremarkable, which would have made the evidence columns
                // agree for a reason that has nothing to do with what they were printed to show.
                int direct = Aggregate.Ratio(graph, watched, fleet.Volume);
                int aggregate = Aggregate.Ratio(graph, watched, aggregateVolume);

                if (direct > watchedDirect)
                {
                    watchedDirect = direct;
                }

                if (aggregate > watchedAggregate)
                {
                    watchedAggregate = aggregate;
                }

                if (aggregateTick < 0 && aggregate >= threshold)
                {
                    aggregateTick = tick;
                }
            }

            // Peaks scanned on a coarse cadence: a full sweep every Tick would dominate the section's
            // runtime and the peak is a property of the window, not of a Tick.
            if (tick % 8 == 0)
            {
                for (int arc = 0; arc < graph.Arcs; arc++)
                {
                    int direct = Aggregate.Ratio(graph, arc, fleet.Volume);
                    int aggregate = Aggregate.Ratio(graph, arc, aggregateVolume);

                    if (direct > peakDirect)
                    {
                        peakDirect = direct;
                    }

                    if (aggregate > peakAggregate)
                    {
                        peakAggregate = aggregate;
                    }
                }
            }
        }

        int overDirect = 0;
        int overAggregate = 0;
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (Aggregate.Ratio(graph, arc, fleet.Volume) >= threshold)
            {
                overDirect++;
            }

            if (Aggregate.Ratio(graph, arc, aggregateVolume) >= threshold)
            {
                overAggregate++;
            }
        }

        return new SurgeOutcome(
            DirectLag: watched < 0 ? -1 : 0,
            AggregateLag: watched < 0 || aggregateTick < 0 ? -1 : aggregateTick - directTick,
            PeakDirect: peakDirect,
            PeakAggregate: peakAggregate,
            OverDirect: overDirect,
            OverAggregate: overAggregate,
            WatchedDirect: watchedDirect,
            WatchedAggregate: watchedAggregate);
    }

    // --- the verdict ------------------------------------------------------------------------------

    private static void AppendVerdict(StringBuilder report)
    {
        report.AppendLine("### What R2 decides, and what it hands on");
        report.AppendLine();
        report.AppendLine(
            "**R4's condition has moved and R7 must not apply it as written.** `plans/0010` retires "
            + "DSDV *\"if the matrix carries the choice loop and Statistical Trips need no concrete "
            + "path\"*. R1 settled the first clause — it does. The second was written before "
            + "`adr/0041`, which requires a vehicular Traveller to increment the Segment it **enters**, "
            + "every Tick. What that needs is a next Segment, not a path; a next-hop table supplies "
            + "one and stores no path at all. **So the second clause is false, and it is false for a "
            + "reason that favours distance-vector rather than merely failing to retire it.**");
        report.AppendLine();
        report.AppendLine(
            "That is an argument, not a measurement, and R2 does not settle R4 with it — R4's own "
            + "subject is **convergence after an edit**, which nothing here touches. What R2 changes "
            + "is that R4 is **live**, and R5's edit storm is where it is decided: a next-hop table's "
            + "attraction is that it needs no per-route invalidation, and its exposure is that "
            + "*in a city builder link deletion is the core verb*.");
        report.AppendLine();
        report.AppendLine(
            "**`adr/0041` is owed a correction, small and worth making.** It calls the path source "
            + "*\"a performance axis with no correctness content\"*. R2.1's detour columns are "
            + "correctness content: a Traveller handed a coarse route drives a different Trip, which "
            + "under `05 §4`'s test is a different city. The ADR's substantive claim survives intact "
            + "— experience and contribution stay the same list of Segments under every rung, because "
            + "a Traveller increments whatever it actually drives — so this amends a sentence and not "
            + "a decision.");
        report.AppendLine();
        report.AppendLine(
            "**The crossing rate is now measured and `adr/0041`'s own revisit trigger is the place to "
            + "record it.** Its cost arithmetic assumes one Segment per Tick; R2.2 reports what the "
            + "graph actually produces at R0's density. Every figure in R2a scales linearly on that "
            + "number.");
        report.AppendLine();
        report.AppendLine(
            "**What R2 does not settle.** The peaking factor is still unsized (decision 5a), so the "
            + "inversion table is a curve and not a verdict. District count remains R1's open trade, "
            + "and R2.1 adds a second structure to it — the next-hop table is linear in District "
            + "count where the route store is quadratic, so the two rungs rank differently at "
            + "different District counts and neither ranking is a reason to pick one. And nothing "
            + "here prices **invalidation**, which is R5's.");
        report.AppendLine();
    }

    // --- formatting -------------------------------------------------------------------------------

    private static int MeanOf(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        long total = 0;
        foreach (int value in values)
        {
            total += value;
        }

        return (int)(total / values.Count);
    }

    private static int QuantileOf(List<int> sorted, int percent) =>
        sorted.Count == 0 ? 0 : sorted[IntegerMath.FloorDiv((sorted.Count - 1) * percent, 100)];

    private static long Nanoseconds(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1_000_000_000 / Stopwatch.Frequency;

    /// <summary>Aggregate's peak as a multiple of direct's. Under 1.00x, the scheme understates.</summary>
    private static string Compression(int direct, int aggregate) =>
        direct == 0 ? "—" : Hundredths((int)IntegerMath.FloorDiv((long)aggregate * 100, direct)) + "x";

    private static int Percent(int fixedValue) => (int)((((long)fixedValue * 10_000) + 32_768) >> 16);

    private static string Hundredths(int value) => string.Create(
        CultureInfo.InvariantCulture, $"{value / 100}.{IntegerMath.Abs(value % 100):D2}");

    private static string Milliseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10_000)) + " ms";

    private static string Bytes(long bytes) =>
        bytes < 1024 ? bytes + " B"
        : bytes < 1024 * 1024 ? Hundredths((int)(bytes * 100 / 1024)) + " KiB"
        : bytes < 1024L * 1024 * 1024 ? Hundredths((int)(bytes * 100 / (1024 * 1024))) + " MiB"
        : Hundredths((int)(bytes * 100 / (1024L * 1024 * 1024))) + " GiB";

    private static void Sink(long checksum)
    {
        if (checksum == long.MinValue)
        {
            Console.Error.Write(' ');
        }
    }
}
