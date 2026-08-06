using System.Diagnostics;
using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Matrix;
using S2.Routing.Routing;

namespace S2.Routing.Harness;

/// <summary>
/// S2 R1 — the travel-time matrix. The prescribed first measurement.
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0010</c>: <i>"Build the zone-to-zone travel-time matrix first, then measure what work is
/// left"</i> — an instruction appearing in four places in the corpus and inverting what a routing spike
/// would naturally do first, because <b>the headline HPA\*-versus-DSDV question may substantially
/// dissolve once the matrix exists.</b> R1 decides whether the matrix can carry the choice loop and
/// therefore whether R4 is worth running.
/// </para>
/// <para>
/// <b>It is a District count, not a zone count</b> — see <see cref="Districts"/> for why the plan's
/// word is the wrong one.
/// </para>
/// </remarks>
internal static class MatrixReport
{
    /// <summary>
    /// The District sweep. Brackets <c>CONTEXT.md</c> → District's 128-Cell anchor (11 a side) and
    /// reaches both ends of <c>plans/0001</c>'s 100–400, the plan's 2,000-District DRAM row, and past
    /// it — because the ceiling is the finding and a sweep that stops at the answer cannot show one.
    /// </summary>
    private static readonly int[] DistrictRungs = [4, 8, 10, 11, 16, 20, 32, 45, 64];

    /// <summary>The rung every non-sweep section runs at: the anchor, 121 Districts.</summary>
    private const int AnchorPerSide = 11;

    /// <summary>Imbalance rungs, Q16.16. The axis the asymmetry finding is a curve against.</summary>
    private static readonly int[] ImbalanceRungs =
    [
        0,        // 0.00 — a peak that loads both directions equally
        13_107,   // 0.20
        32_768,   // 0.50
        52_429,   // 0.80
        65_536,   // 1.00 — a peak entirely in the phase's direction
    ];

    /// <summary>Commute Budgets, whole Ticks. The corpus gives no value, so this is swept.</summary>
    private static readonly int[] BudgetRungs = [20, 30, 40, 50, 60, 80, 120];

    /// <summary>Reads per timed pattern. Large enough that the loop, not the clock, is measured.</summary>
    private const int ReadsPerPattern = 4_000_000;

    /// <summary>District pairs sampled for the matrix-error section, and true searches per pair.</summary>
    private const int ErrorPairs = 400;
    private const int SearchesPerPair = 6;

    public static string Run()
    {
        var report = new StringBuilder();

        report.AppendLine("## S2 R1 — the travel-time matrix");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(
            "The matrix is **District**-to-District. `plans/0010` says *zone* in six places and "
            + "`CONTEXT.md` → Zone is a permission set over land, while `CONTEXT.md` → District is "
            + "*\"the granularity of the travel-time matrix\"*. The plan is owed the correction.");
        report.AppendLine();
        report.AppendLine(
            "Every congested figure below rests on a **synthetic** monocentric peak — there are no "
            + "Travellers in S2 and R2 is where volume comes from a routed load. So no number here "
            + "says how asymmetric a real city is. Each says: *at directional imbalance `i`, this.*");
        report.AppendLine();

        var graph = GraphGenerator.Build(GraphParameters.Working);
        var directed = GraphGenerator.Build(GraphParameters.Working with
        {
            VolumeScope = VolumeScope.PerDirection,
        });

        AppendPartition(report, directed);
        AppendBuildAndSize(report, directed);
        AppendRead(report, directed);
        AppendAsymmetry(report, graph, directed);
        AppendSettlements(report, directed);
        AppendMatrixError(report, directed);
        AppendDirtyRegion(report, directed);
        AppendResolution(report, directed);

        return report.ToString();
    }

    // --- R1.1 -------------------------------------------------------------------------------------

    private static void AppendPartition(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.1 — the partition, and what a District rung actually is");
        report.AppendLine();
        report.AppendLine("| Per side | Districts | Cells each | Area | Empty | vs the corpus |");
        report.AppendLine("|---:|---:|---:|---:|---:|---|");

        foreach (int perSide in DistrictRungs)
        {
            var districts = Districts.Partition(graph, perSide);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {perSide} | {districts.Count:N0} | {districts.CellsEach(graph):N0} "
                + $"| {Hundredths(districts.AreaHundredthsSquareKilometres(graph))} km² "
                + $"| {districts.Empty} | {Standing(districts)} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**Cell-aligned, never Chunk-aligned** (`CONTEXT.md` → District). The anchor — 128 Cells, "
            + "2.10 km² — lands at 11 a side, which is inside `plans/0001`'s 100–400 figure. That "
            + "agreement is arithmetic, not corroboration: `plans/0001` predates the 1M target.");
        report.AppendLine();
    }

    private static string Standing(Districts districts) => districts.PerSide switch
    {
        AnchorPerSide => "**`CONTEXT.md` → District's 128-Cell anchor**",
        10 or 20 => "`plans/0001`'s 100–400 band",
        45 => "`plans/0010`'s 2,000-District row",
        _ => "—",
    };

    // --- R1.2 -------------------------------------------------------------------------------------

    private static void AppendBuildAndSize(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.2 — cold build, and resident size measured twice");
        report.AppendLine();
        report.AppendLine(
            "A cold build is **one forward one-to-all Dijkstra per District**, not one search per "
            + "pair: a forward search from District *i* fills the whole of row *i*. Nothing is halved "
            + "by symmetry — the matrix is asymmetric and the reverse row needs a backward search.");
        report.AppendLine();
        report.AppendLine(
            "**Resident size is reported twice** because `03 §3.3` needs both: the scalar matrix the "
            + "choice loop reads, and the cached District-pair *routes* volume would be distributed "
            + "along. They differ by far more than a constant factor — *n²* integers against *n²* "
            + "variable-length Segment sequences.");
        report.AppendLine();
        report.AppendLine(
            "| Districts | Searches | Cold build | Per search | Settled | Scalar matrix | Mean route | Route store |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

        int[] arcTicks = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);

        // The whole sweep is walked once untimed before any of it is timed, and getting here took
        // four attempts whose failures all pointed the same way.
        //
        // Warming inside the loop let each rung inherit the rungs before it, and rung 4 read 4.5 ms
        // per search against a settled 1.55. Hoisting the warm-up out at 64 searches made it worse —
        // the first four rungs read ~4.2 ms and the figure then halved twice. Raising it to 400 did
        // not fix it. Building each rung twice and timing the second did not fix it either, which is
        // what ruled out the explanation the first three attempts all assumed: **the cost is not
        // per-rung, it is per-process.** `OneToAll.Run` is called once per District, so the early
        // rungs call it too few times to leave tier 0 — and at tier 0 the heap's `Push` and `Pop` are
        // real calls rather than inlined, which is the whole of the 2.7×.
        //
        // **Every one of the four produced a smooth descending curve on exactly the axis being
        // swept**, which is the most convincing shape a measurement artefact can take: a per-search
        // cost that falls with District count is precisely what a reader would hope a sweep would
        // discover. Only a warm pass over the entire sweep removes it, and it removes the question
        // rather than answering it.
        foreach (int warmPerSide in DistrictRungs)
        {
            TravelTimeMatrix.Build(
                Districts.Partition(graph, warmPerSide), arcTicks, new OneToAll(graph));
        }

        foreach (int perSide in DistrictRungs)
        {
            var districts = Districts.Partition(graph, perSide);
            var search = new OneToAll(graph);

            long start = Stopwatch.GetTimestamp();
            var matrix = TravelTimeMatrix.Build(districts, arcTicks, search);
            long elapsed = Nanoseconds(start);

            long routeBytes = RouteStoreBytes(districts, arcTicks, search, out int meanRoute);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {districts.Count:N0} | {districts.Count:N0} | {Milliseconds(elapsed)} "
                + $"| {elapsed / districts.Count:N0} ns "
                + $"| {search.NodesSettled:N0} | {Bytes(matrix.ResidentBytes)} "
                + $"| {meanRoute:N0} Segments | {Bytes(routeBytes)} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "*Settled* is nodes settled by the last search of the rung — constant across rungs by "
            + "construction, because a one-to-all has no goal to prune toward. **That is why cold "
            + "build is linear in District count, and it is also why a departure from linearity is "
            + "readable as an artefact rather than as a finding.** The whole sweep is walked once "
            + "untimed before any of it is timed, because four weaker warm-ups each left a per-search "
            + "cost falling smoothly with District count — the shape a reader would most readily "
            + "believe, and the process leaving tier 0. `OneToAll.Run` is called once per District, "
            + "so the early rungs are the ones that call it too few times.");
        report.AppendLine();
        report.AppendLine(
            "*Route store* is `n² × mean route length × 4 B`, the Segment sequences at four bytes "
            + "each. **It is the figure that fails first**, and it fails on `adr/0006` grounds rather "
            + "than performance ones — see R1.7, where the same store turns out to be what a "
            + "dirty-region rebuild needs in order to be sound.");
        report.AppendLine();
    }

    private static long RouteStoreBytes(
        Districts districts, int[] arcTicks, OneToAll search, out int meanRoute)
    {
        // Sampled rather than exhaustive: the mean route length is a property of the graph and the
        // District spacing, and every row of a rung would give the same figure to within noise.
        int rows = districts.Count < 16 ? districts.Count : 16;
        long total = 0;
        long counted = 0;

        for (int from = 0; from < rows; from++)
        {
            int source = districts.Representative[from];
            if (source < 0)
            {
                continue;
            }

            search.Run(source, arcTicks);

            for (int to = 0; to < districts.Count; to++)
            {
                int target = districts.Representative[to];
                if (target < 0 || search.CostOf(target) >= OneToAll.Unreachable)
                {
                    continue;
                }

                total += search.PathSegments(target);
                counted++;
            }
        }

        meanRoute = counted == 0 ? 0 : (int)(total / counted);
        return (long)districts.Count * districts.Count * meanRoute * sizeof(int);
    }

    // --- R1.3 -------------------------------------------------------------------------------------

    private static void AppendRead(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.3 — the read, in both access patterns");
        report.AppendLine();
        report.AppendLine(
            "**The read is the measurement that matters most.** `02 §5.8` makes *never resolve a "
            + "route inside the choice loop* a rule — named as the one thing UrbanSim gets "
            + "architecturally right that this design must not violate. If the matrix read is not "
            + "cheap, that rule is unenforceable and the finding is larger than S2.");
        report.AppendLine();
        report.AppendLine(
            "`references.md §2` describes the choice loop **twice in one sentence** — *\"what is the "
            + "commute from this candidate dwelling to any job?\"*, which is one origin against many "
            + "destinations and therefore a sequential **row scan**, and *\"many-to-many, evaluated "
            + "tens of thousands of times per cycle\"*, which reads as **scattered**. Both are timed, "
            + "so which one the choice loop performs becomes a design question with a priced answer "
            + "rather than a detail settled by whoever writes the loop.");
        report.AppendLine();
        report.AppendLine("| Districts | Resident | Row scan | Scattered | Scattered ÷ row | vs K2 |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|");

        int[] arcTicks = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);

        foreach (int perSide in DistrictRungs)
        {
            var districts = Districts.Partition(graph, perSide);
            var matrix = TravelTimeMatrix.Build(districts, arcTicks, new OneToAll(graph));

            long row = TimeRowScan(matrix);
            long scattered = TimeScattered(matrix);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {districts.Count:N0} | {Bytes(matrix.ResidentBytes)} "
                + $"| {Picoseconds(row)} | {Picoseconds(scattered)} "
                + $"| {Ratio(scattered, row)} | {Ratio(scattered, K2ScatteredPicoseconds)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The tripwire, and it is a real threshold rather than a tautology.** `plans/0010` "
            + $"rewrote this wire during grilling for exactly the reason the numbers now show: *\"a "
            + $"lookup into an n×n array is O(1) by construction, so the original wire — 'not O(1) "
            + $"and cheap' — could not fire on any plausible implementation.\"* What binds is where "
            + $"the matrix lives. The wire that replaced it fires if a read costs more than S4's K2 "
            + $"random gather, **{K2ScatteredPicoseconds / 1000}.{(K2ScatteredPicoseconds / 100) % 10} ns "
            + $"per handle** on this machine — the `SoaScattered` figure, which is the closest thing "
            + $"in the corpus to a priced cache miss."));
        report.AppendLine();
        report.AppendLine(
            "The row scan is immune to the ceiling and the scattered read is not, which is the whole "
            + "of the finding: **District count has a ceiling set by L3, and the ceiling only exists "
            + "for one of the two access patterns.** A player drawing thousands of Districts would be "
            + "drawing a performance cliff, so District count is not a free UI decision — but it is "
            + "only a cliff if the choice loop scatters.");
        report.AppendLine();
    }

    /// <summary>S4's K2 `SoaScattered`, 13.66 ns per handle, in picoseconds.</summary>
    private const long K2ScatteredPicoseconds = 13_660;

    private static long TimeRowScan(TravelTimeMatrix matrix)
    {
        int n = matrix.Count;
        int[] entries = matrix.Entries;
        long checksum = 0;
        int reads = 0;

        // Warm.
        for (int i = 0; i < n; i++)
        {
            checksum += entries[i];
        }

        long start = Stopwatch.GetTimestamp();
        int row = 0;
        while (reads < ReadsPerPattern)
        {
            int offset = row * n;
            for (int j = 0; j < n; j++)
            {
                checksum += entries[offset + j];
            }

            reads += n;
            row++;
            if (row == n)
            {
                row = 0;
            }
        }

        long elapsed = Nanoseconds(start);
        Sink(checksum);
        return elapsed * 1000 / reads;
    }

    private static long TimeScattered(TravelTimeMatrix matrix)
    {
        int[] entries = matrix.Entries;
        long checksum = 0;

        // The indices are drawn BEFORE the clock starts — R0's second harness defect was a sampler
        // running inside a timed loop, and a hash per read here would be most of the measurement.
        var indices = new int[1 << 16];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = CounterHash.Below(
                CounterHash.Of(CounterHash.Seed, (ulong)i, (ulong)matrix.Count,
                    CounterHash.Purpose.MatrixReadIndex),
                entries.Length);
        }

        for (int i = 0; i < indices.Length; i++)
        {
            checksum += entries[indices[i]];
        }

        long start = Stopwatch.GetTimestamp();
        for (int read = 0; read < ReadsPerPattern; read++)
        {
            checksum += entries[indices[read & (indices.Length - 1)]];
        }

        long elapsed = Nanoseconds(start);
        Sink(checksum);
        return elapsed * 1000 / ReadsPerPattern;
    }

    // --- R1.4 -------------------------------------------------------------------------------------

    private static void AppendAsymmetry(StringBuilder report, RoadGraph perSegment, RoadGraph perDirection)
    {
        report.AppendLine("### R1.4 — asymmetry, and the decision that decides whether it exists");
        report.AppendLine();
        report.AppendLine(
            "**The volume-scope question and the `adr/0020` exposure are one question.** R0 was "
            + "forbidden from settling `volume / capacity` per Segment or per direction and priced it "
            + "at 5% of the graph, saying *\"what it buys is not visible until R2 has volume to "
            + "attribute\"*. That is not quite right, and R1 is where it shows: under **per Segment** "
            + "the two directions share one counter, so the VDF returns the same delay both ways and "
            + "**the matrix is symmetric to the bit** — which makes `adr/0020`'s union-find exactly "
            + "correct, not by evidence but by construction.");
        report.AppendLine();

        // Both scopes carry their own partition, rather than one being reused for whichever graph is
        // not the other. The two partitions are identical — the scope changes a column's width and
        // not a node's position — and saying so by construction is cheaper than a reference
        // comparison standing in for the claim.
        var scopes = new[]
        {
            ("per Segment", perSegment, Districts.Partition(perSegment, AnchorPerSide)),
            ("**per direction**", perDirection, Districts.Partition(perDirection, AnchorPerSide)),
        };

        report.AppendLine("| Scope | Imbalance | Pairs | Mean | Median | p90 | Max | Mean, relative |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");

        foreach (int imbalance in ImbalanceRungs)
        {
            foreach ((string name, RoadGraph graph, Districts partition) in scopes)
            {
                var parameters = CongestionParameters.Working with { Imbalance = imbalance };
                int[] arcTicks = Congestion.CarTicks(graph, parameters, Phase.MorningPeak);

                var matrix = TravelTimeMatrix.Build(partition, arcTicks, new OneToAll(graph));
                var asymmetry = matrix.MeasureAsymmetry();

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {name} | {Hundredths(Percent(imbalance))}% | {asymmetry.Pairs:N0} "
                    + $"| {Ticks(asymmetry.MeanTicks)} | {Ticks(asymmetry.MedianTicks)} "
                    + $"| {Ticks(asymmetry.NinetiethTicks)} | {Ticks(asymmetry.MaximumTicks)} "
                    + $"| {Hundredths(Percent(asymmetry.MeanRelative))}% |"));
            }
        }

        report.AppendLine();
        report.AppendLine(
            "Every per-Segment row reading exactly zero is **the instrument working, not failing**: "
            + "it is the same measurement R0.5 had to sweep crossing density to obtain, arrived at "
            + "from the other side. A zero that can be made non-zero by moving one parameter is "
            + "evidence; a zero that has never been observed to move is not.");
        report.AppendLine();
    }

    // --- R1.5 -------------------------------------------------------------------------------------

    private static void AppendSettlements(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.5 — `adr/0020`: union-find against the definition it claims to compute");
        report.AppendLine();
        report.AppendLine(
            "`adr/0020` computes a Settlement as *\"a **connected component** of the District graph… "
            + "**a union-find** over data already being maintained, at effectively no cost\"*. "
            + "`CONTEXT.md` → Settlement defines one as *\"a maximal set of Districts **mutually** "
            + "reachable within the Commute Budget.\"* **Union-find computes weak connectivity; "
            + "\"mutually reachable\" is strong connectivity**, and the two coincide only on a "
            + "symmetric matrix.");
        report.AppendLine();
        report.AppendLine(
            "So the test is not the asymmetry distribution — that is a number about travel times. "
            + "The test is whether the two algorithms **disagree about the city**, because a "
            + "Settlement is an object the game is made of. The Commute Budget has no value anywhere "
            + "in the corpus, so it is swept: at a Budget longer than any commute everything is one "
            + "Settlement under both readings, and at a Budget shorter than any it is all singletons. "
            + "**The divergence lives in the middle, which is where the game is played.**");
        report.AppendLine();
        report.AppendLine(
            "| Budget | Union-find | Tarjan SCC | Largest weak | Largest strong | One-way pairs |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|");

        var districts = Districts.Partition(graph, AnchorPerSide);
        int[] arcTicks = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);
        var matrix = TravelTimeMatrix.Build(districts, arcTicks, new OneToAll(graph));

        foreach (int budget in BudgetRungs)
        {
            var settlements = Connectivity.Measure(matrix, Fixed.FromInt(budget));

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {budget} Ticks | {settlements.Weak:N0} | {settlements.Strong:N0} "
                + $"| {settlements.LargestWeak:N0} | {settlements.LargestStrong:N0} "
                + $"| {settlements.OneWayPairs:N0} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**One-way pairs are the exposure in one number** — District pairs within budget in "
            + "exactly one direction, which is precisely what union-find merges and Tarjan does not. "
            + "The headline case is the one `plans/0010` named before the numbers arrived: inbound "
            + "within budget at the morning peak, outbound not.");
        report.AppendLine();
        report.AppendLine(
            "**The one-way count rises and then falls, and the fall is the curve rather than an "
            + "error.** A pair can only be one-way while the Budget sits between its two directions' "
            + "costs, so a Budget below every commute produces none and a Budget above every commute "
            + "produces none either. **The exposure is a band, not a threshold** — which matters more "
            + "than the peak height, because it says the disagreement cannot be designed away by "
            + "choosing a generous Commute Budget. A Budget generous enough to close it is a Budget "
            + "that has stopped bounding anything, and `CONTEXT.md` → Commute Budget exists to make "
            + "geography matter.");
        report.AppendLine();
    }

    // --- R1.6 -------------------------------------------------------------------------------------

    private static void AppendMatrixError(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.6 — what the matrix entry is wrong by");
        report.AppendLine();
        report.AppendLine(
            "**Not in `plans/0010`, and it should have been.** The plan measures four things — build, "
            + "rebuild, size, read — and all four are about cost. None asks how *wrong* an entry is. "
            + "A District-to-District entry is one number standing for every Access Point pair inside "
            + "those two Districts, and **if its error exceeds what the Commute Budget resolves, "
            + "\"the matrix carries the choice loop\" is false however fast the read is.** That is a "
            + "prior question to every figure above.");
        report.AppendLine();
        report.AppendLine(
            "Measured against the query the game actually issues: R0's "
            + "`(Segment, offset) → (Segment, offset)` A\\* with `Chebyshev`, drawn inside the two "
            + "Districts, against the same congested arc costs the matrix was built on.");
        report.AppendLine();
        report.AppendLine("| Districts | Pairs | Searches | Mean error | p90 | Max | Mean, relative |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");

        int[] arcTicks = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);

        foreach (int perSide in new[] { 4, 8, AnchorPerSide, 20, 32 })
        {
            var districts = Districts.Partition(graph, perSide);
            var matrix = TravelTimeMatrix.Build(districts, arcTicks, new OneToAll(graph));
            var error = MeasureError(graph, districts, matrix, arcTicks, out int searches);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {districts.Count:N0} | {error.Pairs:N0} | {searches:N0} "
                + $"| {Ticks(error.MeanTicks)} | {Ticks(error.NinetiethTicks)} "
                + $"| {Ticks(error.MaximumTicks)} | {Hundredths(Percent(error.MeanRelative))}% |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**The error is a property of District extent, and it shrinks the way the resident size "
            + "grows.** That is the trade R1 exists to price, and it is the reason District count "
            + "cannot be chosen on cache behaviour alone: the rung that fits in L3 is the rung whose "
            + "entries stand for the most ground.");
        report.AppendLine();
    }

    private static Asymmetry MeasureError(
        RoadGraph graph, Districts districts, TravelTimeMatrix matrix, int[] arcTicks, out int searches)
    {
        var search = new PointToPoint(graph, arcTicks);

        var absolute = new List<int>();
        var relative = new List<int>();
        searches = 0;

        for (int pair = 0; pair < ErrorPairs; pair++)
        {
            int from = CounterHash.Below(
                CounterHash.Of(CounterHash.Seed, (ulong)pair, (ulong)districts.Count,
                    CounterHash.Purpose.MatrixSampleOrigin),
                districts.Count);
            int to = CounterHash.Below(
                CounterHash.Of(CounterHash.Seed, (ulong)pair, (ulong)districts.Count,
                    CounterHash.Purpose.MatrixSampleDestination),
                districts.Count);

            int entry = matrix[from, to];
            if (from == to || entry >= OneToAll.Unreachable)
            {
                continue;
            }

            for (int attempt = 0; attempt < SearchesPerPair; attempt++)
            {
                ulong query = ((ulong)pair << 8) | (ulong)attempt;

                var origin = InDistrict(graph, districts, from, query, isOrigin: true);
                var goal = InDistrict(graph, districts, to, query, isOrigin: false);

                if (origin.Segment < 0 || goal.Segment < 0)
                {
                    continue;
                }

                search.Bootstrap(origin, goal, Modes.Car, HeuristicKind.Chebyshev);
                var outcome = search.Expand();
                searches++;

                if (!outcome.Found)
                {
                    continue;
                }

                int difference = IntegerMath.Abs(outcome.CostTicks - entry);
                absolute.Add(difference);
                relative.Add(outcome.CostTicks == 0 ? 0 : Fixed.Div(difference, outcome.CostTicks));
            }
        }

        absolute.Sort();
        relative.Sort();

        return new Asymmetry(
            Pairs: IntegerMath.FloorDiv(absolute.Count, SearchesPerPair),
            MeanTicks: MeanOf(absolute),
            MedianTicks: QuantileOf(absolute, 50),
            NinetiethTicks: QuantileOf(absolute, 90),
            MaximumTicks: absolute.Count == 0 ? 0 : absolute[^1],
            MeanRelative: MeanOf(relative),
            NinetiethRelative: QuantileOf(relative, 90));
    }

    /// <summary>
    /// A car Access Point inside a District, by rejection against the node partition. Returns
    /// <c>Segment = -1</c> when a bounded number of draws fails, which is reported rather than
    /// retried forever.
    /// </summary>
    private static AccessPoint InDistrict(
        RoadGraph graph, Districts districts, int district, ulong query, bool isOrigin)
    {
        var pool = districts.SegmentsIn(district);
        if (pool.Length == 0)
        {
            return new AccessPoint(-1, 0);
        }

        var purpose = isOrigin
            ? CounterHash.Purpose.MatrixSampleOrigin
            : CounterHash.Purpose.MatrixSampleDestination;

        int segment = pool[CounterHash.Below(
            CounterHash.Of(CounterHash.Seed, query, (ulong)district, purpose), pool.Length)];

        int offset = CounterHash.Below(
            CounterHash.Of(CounterHash.Seed, query + 1, (ulong)segment, purpose),
            graph.SegmentLengthTiles[segment] + 1);

        return new AccessPoint(segment, offset);
    }

    // --- R1.7 -------------------------------------------------------------------------------------

    private static void AppendDirtyRegion(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.7 — incremental rebuild, and what a dirty region cannot see");
        report.AppendLine();
        report.AppendLine(
            "`02 §6` describes the matrix as rebuilt at a *slow cadence, dirty regions only*. **That "
            + "is a spatial invalidation mechanism, and routes invalidate against a scalar Epoch** — "
            + "`plans/0010` R5 flags that *\"two invalidation mechanisms are in the corpus and nothing "
            + "relates them, so the matrix and the cache can disagree about what the network currently "
            + "is\"*, and requires R1 to state whether the matrix carries an Epoch at all. "
            + "**It does not, and it must not be given one silently** — a version counter would imply "
            + "a relationship to the route cache that nobody has argued.");
        report.AppendLine();
        report.AppendLine(
            "The measurement below is what makes that more than bookkeeping. One District's roads are "
            + "bulldozed — `plans/0010`'s *\"in a city builder link deletion is the core verb\"* — and "
            + "the matrix is rebuilt three ways, each checked against a full rebuild's ground truth.");
        report.AppendLine();
        report.AppendLine(
            "**Two edit sites, because one of them is degenerate and a single row would not have "
            + "shown it.** A central District is on the shortest path between most pairs on the map; "
            + "a corner District is on almost none. The mechanism's worth is the difference between "
            + "them, and a report that bulldozed only the middle would have concluded that incremental "
            + "rebuild is worthless.");
        report.AppendLine();
        report.AppendLine("| Edit site | Rung | Rows rebuilt | Cost | Entries missed | Sound |");
        report.AppendLine("|---|---|---:|---:|---:|---|");

        var districts = Districts.Partition(graph, AnchorPerSide);
        var parameters = CongestionParameters.Working;
        int[] before = Congestion.CarTicks(graph, parameters, Phase.MorningPeak);

        var search = new OneToAll(graph);
        var baseline = TravelTimeMatrix.Build(districts, before, search);

        int centre = (IntegerMath.FloorDiv(AnchorPerSide, 2) * AnchorPerSide)
            + IntegerMath.FloorDiv(AnchorPerSide, 2);

        var sites = new[] { ("**Centre**", centre), ("**Corner**", 0) };
        var moved = new int[sites.Length];
        var severed = new int[sites.Length];

        for (int site = 0; site < sites.Length; site++)
        {
            (string name, int dirtyDistrict) = sites[site];
            int[] after = Edited(
                graph, before, districts, dirtyDistrict, out var dirtyNode, out severed[site]);

            long start = Stopwatch.GetTimestamp();
            var truth = TravelTimeMatrix.Build(districts, after, search);
            long fullCost = Nanoseconds(start);
            moved[site] = TravelTimeMatrix.Differences(baseline, truth);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {name} | Full rebuild | {districts.Count:N0} | {Milliseconds(fullCost)} "
                + $"| 0 | yes, by definition |"));

            // The dirty-region rung: rebuild the rows whose District touches the edit.
            var spatial = Rows(districts, dirtyNode);
            var spatialMatrix = baseline.Clone();
            start = Stopwatch.GetTimestamp();
            TravelTimeMatrix.Rebuild(spatialMatrix, districts, after, search, spatial);
            long spatialCost = Nanoseconds(start);
            int spatialWrong = TravelTimeMatrix.Differences(spatialMatrix, truth);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| | Dirty region — rows whose District touches it | {Count(spatial):N0} "
                + $"| {Milliseconds(spatialCost)} | {spatialWrong:N0} of {moved[site]:N0} "
                + $"| {(spatialWrong == 0 ? "yes, on this edit" : "**no**")} |"));

            // The sound rung: rebuild the rows whose ROUTES crossed the edit.
            var crossing = RowsWhoseRoutesCross(
                districts, before, search, dirtyNode, out int crossingEntries);
            var crossingMatrix = baseline.Clone();
            start = Stopwatch.GetTimestamp();
            TravelTimeMatrix.Rebuild(crossingMatrix, districts, after, search, crossing);
            long crossingCost = Nanoseconds(start);
            int crossingWrong = TravelTimeMatrix.Differences(crossingMatrix, truth);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| | Routes crossing it — needs the route store | {Count(crossing):N0} rows, "
                + $"{crossingEntries:N0} entries | {Milliseconds(crossingCost)} "
                + $"| {crossingWrong:N0} of {moved[site]:N0} "
                + $"| {(crossingWrong == 0 ? "yes" : "**no**")} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The centre edit severs **{severed[0]:N0} arcs** and moves **{moved[0]:N0}** of the "
            + $"matrix's {districts.Count * districts.Count:N0} entries; the corner edit severs "
            + $"**{severed[1]:N0}** and moves **{moved[1]:N0}**."));
        report.AppendLine();
        report.AppendLine(
            "**The dirty region is a spatial test on a non-spatial quantity, and that is the finding.** "
            + "A path from District *i* to District *j* can cross the edited ground without either "
            + "endpoint being near it, so rebuilding by *which Districts the region overlaps* misses "
            + "exactly the long routes the matrix exists to serve — and it misses them **silently**, "
            + "leaving entries that are stale rather than merely coarse. Making it sound requires "
            + "knowing **which routes crossed the region**, which means keeping the route store R1.2 "
            + "priced, and that store is the one that does not fit. **The matrix's cheap invalidation "
            + "and its cheap storage are the same trade, taken twice.**");
        report.AppendLine();
        report.AppendLine(
            "**And the sound rung rebuilds every row anyway, which is a finding about the matrix "
            + "rather than about the edit.** A one-to-all fills a whole row, so the build granularity "
            + "*is* the row — and every row holds the entry addressed *to* the edited District, whose "
            + "route necessarily ends inside it. So however few entries an edit invalidates, **at "
            + "least one lands in every row and the incremental path collapses into the full one.** "
            + "The entries column is the work genuinely needed; the rows column is the work the "
            + "structure forces. An incremental rebuild worth having would need a build kernel finer "
            + "than one-to-all — a point-to-point search per entry, which R0 priced at 435 µs "
            + "against this row's ~1.5 ms for a hundred and twenty-one of them.");
        report.AppendLine();
    }

    private static int[] Edited(
        RoadGraph graph, int[] arcTicks, Districts districts, int dirtyDistrict,
        out bool[] dirtyNode, out int touched)
    {
        var edited = (int[])arcTicks.Clone();
        dirtyNode = new bool[graph.Nodes];
        touched = 0;

        for (int node = 0; node < graph.Nodes; node++)
        {
            if (districts.OfNode[node] != dirtyDistrict)
            {
                continue;
            }

            dirtyNode[node] = true;

            for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
            {
                if (edited[arc] == RoadGraph.Impassable)
                {
                    continue;
                }

                // A bulldoze, which `plans/0010` names as the core verb: "in a city builder link
                // deletion is the core verb". A capacity change would move fewer entries and would
                // be the flattering edit to measure.
                edited[arc] = RoadGraph.Impassable;
                touched++;
            }
        }

        return edited;
    }

    private static bool[] Rows(Districts districts, bool[] dirtyNode)
    {
        var rows = new bool[districts.Count];

        for (int node = 0; node < dirtyNode.Length; node++)
        {
            if (dirtyNode[node])
            {
                rows[districts.OfNode[node]] = true;
            }
        }

        return rows;
    }

    /// <summary>
    /// Rows whose routes cross the edit, and — separately — how many <b>entries</b> do.
    /// </summary>
    /// <remarks>
    /// <b>The two counts are the section's real finding, and the loop must not stop early to get the
    /// first one.</b> A one-to-all fills a whole row, so the matrix's build granularity <i>is</i> the
    /// row: one stale entry forces every entry in its row to be recomputed. Counting entries as well
    /// says how much of that recomputation was actually needed, and the gap between the two is the
    /// price of the granularity rather than the price of the edit.
    /// </remarks>
    private static bool[] RowsWhoseRoutesCross(
        Districts districts, int[] arcTicks, OneToAll search, bool[] dirtyNode, out int entries)
    {
        var rows = new bool[districts.Count];
        entries = 0;

        for (int from = 0; from < districts.Count; from++)
        {
            int source = districts.Representative[from];
            if (source < 0)
            {
                continue;
            }

            search.Run(source, arcTicks);

            for (int to = 0; to < districts.Count; to++)
            {
                int target = districts.Representative[to];
                if (target >= 0 && search.PathTouches(target, dirtyNode))
                {
                    rows[from] = true;
                    entries++;
                }
            }
        }

        return rows;
    }

    // --- R1.8 -------------------------------------------------------------------------------------

    private static void AppendResolution(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R1.8 — time resolution: one matrix, or five");
        report.AppendLine();
        report.AppendLine(
            "**A second unstated axis, and `plans/0010` is right that it interacts with everything "
            + "else.** A single Day-average matrix cannot represent the peak every other figure in "
            + "this spike is measured at: morning inbound and evening outbound cancel, and the "
            + "asymmetry the directed graph exists to carry vanishes into the mean. A per-phase "
            + "matrix multiplies build cost and resident size by five and gives the choice loop a "
            + "travel time that matches the moment being asked about.");
        report.AppendLine();
        report.AppendLine("| Resolution | Build | Resident | Mean asymmetry | p90 | One-way pairs at 40 Ticks |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|");

        var districts = Districts.Partition(graph, AnchorPerSide);
        var parameters = CongestionParameters.Working;
        var search = new OneToAll(graph);

        long start = Stopwatch.GetTimestamp();
        int[] average = Congestion.DayAverageCarTicks(graph, parameters);
        var averaged = TravelTimeMatrix.Build(districts, average, search);
        long averageCost = Nanoseconds(start);

        var averageAsymmetry = averaged.MeasureAsymmetry();
        var averageSettlements = Connectivity.Measure(averaged, Fixed.FromInt(40));

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| Day average, one matrix | {Milliseconds(averageCost)} "
            + $"| {Bytes(averaged.ResidentBytes)} | {Ticks(averageAsymmetry.MeanTicks)} "
            + $"| {Ticks(averageAsymmetry.NinetiethTicks)} | {averageSettlements.OneWayPairs:N0} |"));

        long phaseCost = 0;
        long phaseBytes = 0;
        foreach (var phase in PhaseProfile.All)
        {
            start = Stopwatch.GetTimestamp();
            int[] ticks = Congestion.CarTicks(graph, parameters, phase);
            var matrix = TravelTimeMatrix.Build(districts, ticks, search);
            phaseCost += Nanoseconds(start);
            phaseBytes += matrix.ResidentBytes;

            var asymmetry = matrix.MeasureAsymmetry();
            var settlements = Connectivity.Measure(matrix, Fixed.FromInt(40));

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| — of which `{phase}` | | {Bytes(matrix.ResidentBytes)} "
                + $"| {Ticks(asymmetry.MeanTicks)} | {Ticks(asymmetry.NinetiethTicks)} "
                + $"| {settlements.OneWayPairs:N0} |"));
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **Per phase, five matrices** | {Milliseconds(phaseCost)} | {Bytes(phaseBytes)} | | | |"));

        report.AppendLine();
        report.AppendLine(
            "**The Day-average row is the one to read, and it should be read against the peak rows "
            + "rather than against the others.** It is the matrix a single-resolution design gives "
            + "the choice loop, and what it reports about the morning peak is what a Household would "
            + "be deciding on.");
        report.AppendLine();
        report.AppendLine(
            "The average is taken over the **cost**, not over the volume. BPR is convex, so the delay "
            + "at the mean volume is strictly less than the mean of the delays — averaging volumes "
            + "first would give a Day-average matrix describing a city with no rush hour in it at "
            + "all, rather than one whose rush hour has been smeared. It is also **unweighted**, "
            + "because the sun arc's phase widths are `plans/0010`'s open decision 5a and an "
            + "unweighted mean is the only average available while they are unsized.");
        report.AppendLine();
        report.AppendLine(
            "**And that is the refresh-cadence decision arriving from a direction nobody expected.** "
            + "`plans/0010` decision 2 files the matrix refresh cadence as *almost certainly "
            + "hash-bearing* — two cadences produce two cities, so it is a design change under "
            + "`05 §4` rather than a free knob. Time *resolution* is the same class of decision and "
            + "the corpus has not named it at all: a Day-average matrix and a per-phase one give the "
            + "choice loop different answers to the same question, so they are different cities, and "
            + "**the choice belongs beside cadence in whatever settles that.**");
        report.AppendLine();
    }

    // --- formatting ------------------------------------------------------------------------------

    private static int Count(bool[] flags)
    {
        int set = 0;
        foreach (bool flag in flags)
        {
            if (flag)
            {
                set++;
            }
        }

        return set;
    }

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

    private static int Percent(int fixedValue) => (int)((((long)fixedValue * 10_000) + 32_768) >> 16);

    private static string Hundredths(int value) => string.Create(
        CultureInfo.InvariantCulture, $"{value / 100}.{IntegerMath.Abs(value % 100):D2}");

    private static string Ticks(int fixedTicks) =>
        Hundredths((int)(((long)fixedTicks * 100) >> 16)) + " Ticks";

    private static string Milliseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10_000)) + " ms";

    private static string Picoseconds(long picoseconds) =>
        Hundredths((int)(picoseconds / 10)) + " ns";

    private static string Ratio(long numerator, long denominator) =>
        denominator == 0 ? "—" : Hundredths((int)(numerator * 100 / denominator)) + "×";

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
