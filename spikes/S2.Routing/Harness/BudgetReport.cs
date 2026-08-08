using System.Diagnostics;
using System.Globalization;
using System.Text;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Harness;

/// <summary>
/// R6.3 — routing's Tick bill with <b>both</b> consumers in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every budget figure in this corpus counts Trip starts.</b> R3's tripwire — <i>routing fits
/// while fewer than 85 Trips start per Tick</i> — counts them, and <c>plans/0013</c>'s routing row
/// counts them. But <c>adr/0046</c> introduced <b>Sight</b>, and S2 R8 measured what it does:
/// <b>1,269.51 diversions per Tick at 40,000 Travellers.</b> A diversion is a mid-journey
/// re-decision, and under a stored route it costs a fresh search from the current node.
/// </para>
/// <para>
/// <b>Then <c>adr/0047</c> deleted the next-hop table</b>, which was the only path source that served
/// a diversion cheaply — R8.6 priced it at 391 ns against a flat search's 485,507 ns.
/// </para>
/// <para>
/// <b>Nobody has multiplied those two facts together.</b> This section does. It measures its own cost
/// basis in-process rather than quoting absolutes across captures, and it publishes a feasible region
/// inverted R3's way rather than a verdict.
/// </para>
/// </remarks>
internal static class BudgetReport
{
    /// <summary><c>CLAUDE.md</c>: 15.6 ms at 4× speed. Itself unratified — see <c>plans/0013</c>.</summary>
    private const long BudgetNanoseconds = 15_600_000;

    private const int TicksPerDay = 8_192;

    /// <summary>
    /// R8.3's measured rate, as a per-Traveller intensity: 1,269.51 diversions per Tick at 40,000
    /// Travellers. Carried as an intensity so it can be applied to a different fleet size.
    /// </summary>
    private const int R8DiversionsPerTickHundredths = 126_951;
    private const int R8Travellers = 40_000;

    /// <summary>
    /// R8.6's own cost for serving one diversion by flat A* over live costs, on sites a live fleet
    /// actually diverted at. Carried as the <b>pessimistic end</b> of the basis rather than quoted as
    /// this section's own figure, because it comes from a different capture and R1.3's rule stands.
    /// </summary>
    private const long R8FlatSearchNs = 485_507;

    /// <summary>
    /// The in-flight band the corpus has, and <b>every rung of it is already the target city</b>:
    /// S0a derives 37,000 / 56,000 / 111,000 Trips in flight for a 1,000,000-population world by
    /// sweeping the mean Trip duration, because that duration is the thing nobody has measured.
    /// <c>spike-results</c> records the band as owed a re-derivation — it <i>"conflates duration
    /// sensitivity with peaking"</i> — so it is swept, never taken as a point.
    /// </summary>
    /// <remarks>
    /// 40,000 is carried alongside them because it is R8.3's own rung, which is what makes the
    /// diversion intensity quotable at all. It sits just above the band's floor. <b>Nothing here is a
    /// small city being extrapolated from</b>, and an earlier draft of this section said it was.
    /// </remarks>
    private static readonly int[] InFlight = [37_000, 40_000, 56_000, 111_000];

    /// <summary>How long a Citizen keeps a Habit Route before re-forming it, in Days.</summary>
    private static readonly int[] HabitLifetimeDays = [1, 7, 30];

    public static string Run()
    {
        var report = new StringBuilder();
        var graph = GraphGenerator.Build(GraphParameters.Working);
        var distribution = new OdDistribution(graph, new OdSampler(graph));

        report.AppendLine("## S2 R6.3 — routing's Tick bill, with both consumers in it");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(
            "**Every budget figure in this corpus counts Trip starts.** R3's tripwire counts them and "
            + "`plans/0013`'s routing row counts them. But `adr/0046` introduced **Sight**, R8 measured "
            + "**1,269.51 diversions per Tick at 40,000 Travellers**, and `adr/0047` then deleted the "
            + "next-hop table — the one path source that served a diversion cheaply. **Nobody has "
            + "multiplied those together.** This section does.");
        report.AppendLine();

        var basis = MeasureBasis(graph, distribution);
        AppendBasis(report, basis);
        AppendBill(report, basis);
        AppendInversion(report, basis);

        return report.ToString();
    }

    private sealed record Basis(
        long WholeJourneyNs,
        long WholeJourneyLastNs,
        long RemainderNs,
        long CacheHitNs,
        int Samples,
        int MeanArcs,
        int MeanRemainderArcs);

    /// <summary>
    /// Prices the three events in one process. <b>The whole-journey search is taken twice</b> — first
    /// and last — because R3 found a denominator measured first carries a systematic error, and R4,
    /// R5 and R6 were all instructed to do the same.
    /// </summary>
    private static Basis MeasureBasis(RoadGraph graph, OdDistribution distribution)
    {
        var arcCost = new int[graph.Arcs];
        Array.Copy(graph.ArcCarTicks, arcCost, graph.Arcs);

        var search = new PointToPoint(graph, arcCost);
        var scratch = new List<int>();
        OdPair[] pool = distribution.Draw(
            CounterHash.Seed, 512, Modes.Car, KeyReport.OdRungs[0], out _, out _);

        // Warm. R5's rule: the first timed thing in a process pays the whole run's tiered-JIT bill.
        for (int warm = 0; warm < 256; warm++)
        {
            Probe(graph, search, scratch, pool[warm % pool.Length]);
        }

        long firstStart = Stopwatch.GetTimestamp();
        long arcs = 0;
        int counted = 0;

        foreach (OdPair pair in pool)
        {
            int found = Probe(graph, search, scratch, pair);

            if (found > 0)
            {
                arcs += found;
                counted++;
            }
        }

        long firstElapsed = Elapsed(firstStart);

        // The diversion: a search from the middle of a journey to its destination. R8.6 captured its
        // sites from a live fleet; this takes the midpoint node of the whole-journey path, which is
        // the same shape and is stated rather than passed off as the same measurement.
        var midpoints = new List<(AccessPoint From, AccessPoint To)>();

        foreach (OdPair pair in pool)
        {
            search.Bootstrap(pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);

            if (!search.Expand().Found)
            {
                continue;
            }

            scratch.Clear();
            search.PathArcs(scratch);

            if (scratch.Count < 4)
            {
                continue;
            }

            int arc = scratch[scratch.Count / 2];
            int node = graph.ArcTarget[arc];
            midpoints.Add((AtNode(graph, node), pair.Destination));
        }

        long remainderStart = Stopwatch.GetTimestamp();
        long remainderArcs = 0;
        int remainderCounted = 0;

        foreach ((AccessPoint from, AccessPoint to) in midpoints)
        {
            search.Bootstrap(from, to, Modes.Car, HeuristicKind.Chebyshev);

            if (!search.Expand().Found)
            {
                continue;
            }

            scratch.Clear();
            search.PathArcs(scratch);
            remainderArcs += scratch.Count;
            remainderCounted++;
        }

        long remainderElapsed = Elapsed(remainderStart);

        // A cache hit: the lookup and its validation, which is what a served route actually costs.
        long hitElapsed = MeasureCacheHit(graph, pool);

        // The same whole-journey loop again, last rather than first.
        long lastStart = Stopwatch.GetTimestamp();

        foreach (OdPair pair in pool)
        {
            Probe(graph, search, scratch, pair);
        }

        long lastElapsed = Elapsed(lastStart);

        return new Basis(
            firstElapsed / pool.Length,
            lastElapsed / pool.Length,
            remainderCounted == 0 ? 0 : remainderElapsed / remainderCounted,
            hitElapsed,
            counted,
            counted == 0 ? 0 : (int)(arcs / counted),
            remainderCounted == 0 ? 0 : (int)(remainderArcs / remainderCounted));
    }

    private static int Probe(
        RoadGraph graph, PointToPoint search, List<int> scratch, OdPair pair)
    {
        search.Bootstrap(pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);

        if (!search.Expand().Found)
        {
            return 0;
        }

        scratch.Clear();
        search.PathArcs(scratch);
        return scratch.Count;
    }

    private static long MeasureCacheHit(RoadGraph graph, OdPair[] pool)
    {
        var keys = new long[pool.Length];

        for (int i = 0; i < pool.Length; i++)
        {
            keys[i] = KeyReport.KeyOf(graph, pool[i], KeyReport.RouteKey.NearestNode);
        }

        var slotKey = new long[4_096];
        var occupied = new bool[4_096];

        foreach (long key in keys)
        {
            int slot = CounterHash.Below((ulong)key * 0x9E37_79B9_7F4A_7C15UL, 4_096);
            slotKey[slot] = key;
            occupied[slot] = true;
        }

        const int Repeats = 4_096;
        long sink = 0;
        long start = Stopwatch.GetTimestamp();

        for (int repeat = 0; repeat < Repeats; repeat++)
        {
            long key = keys[repeat % keys.Length];
            int slot = CounterHash.Below((ulong)key * 0x9E37_79B9_7F4A_7C15UL, 4_096);

            if (occupied[slot] && slotKey[slot] == key)
            {
                sink++;
            }
        }

        long elapsed = Elapsed(start);
        return sink == 0 ? elapsed / Repeats : elapsed / Repeats;
    }

    private static AccessPoint AtNode(RoadGraph graph, int node)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            int segment = graph.ArcSegment[arc];

            if (graph.SegmentNodeA[segment] == node)
            {
                return new AccessPoint(segment, 0);
            }

            if (graph.SegmentNodeB[segment] == node)
            {
                return new AccessPoint(segment, graph.SegmentLengthTiles[segment]);
            }
        }

        return new AccessPoint(0, 0);
    }

    private static void AppendBasis(StringBuilder report, Basis basis)
    {
        report.AppendLine("### The cost basis, measured in this process");
        report.AppendLine();
        report.AppendLine("| Event | Cost | Mean arcs |");
        report.AppendLine("|---|---:|---:|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| Whole-journey search — a **Habit formation** | {Ns(basis.WholeJourneyNs)} "
            + $"| {basis.MeanArcs:N0} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| Remainder search from mid-journey — a **diversion** | {Ns(basis.RemainderNs)} "
            + $"| {basis.MeanRemainderArcs:N0} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| Cache hit — lookup and compare | {Ns(basis.CacheHitNs)} | — |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| *the same whole-journey loop, measured last* | *{Ns(basis.WholeJourneyLastNs)}* | — |"));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The denominator is taken twice**, first and last, because R3 found that a denominator "
            + $"measured first carries a systematic error and instructed R4, R5 and R6 to do the same. "
            + $"The two readings are {Ns(basis.WholeJourneyNs)} and {Ns(basis.WholeJourneyLastNs)}; "
            + $"**every ratio below uses the last.**"));
        report.AppendLine();

        report.AppendLine(
            "**The diversion search is shorter than a whole journey and that is the point of pricing "
            + "it separately** — a Traveller diverting halfway has half a journey left. R8.6 captured "
            + "its sites from a live fleet; this takes the midpoint node of a drawn journey, which is "
            + "the same shape and is stated rather than passed off as the same measurement.");
        report.AppendLine();
    }

    private static void AppendBill(StringBuilder report, Basis basis)
    {
        report.AppendLine("### The bill");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Diversions are applied at R8.3's measured intensity — **1,269.51 per Tick at 40,000 "
            + $"Travellers**, carried as a per-Traveller rate. **The in-flight rungs are S0a's own "
            + $"band for a 1,000,000-population city** — 37,000 / 56,000 / 111,000, swept because the "
            + $"mean Trip duration behind them is unmeasured — plus R8.3's 40,000. **None of them is "
            + $"a small city.** Habit formations are "
            + $"`Travellers / (lifetime × {TicksPerDay:N0} Ticks)`. The budget is "
            + $"**{Ms(BudgetNanoseconds)}**, itself unratified — `plans/0013`."));
        report.AppendLine();

        report.AppendLine(
            "| In flight | Habit lifetime | Formations/Tick | Diversions/Tick | Formation bill "
            + "| Diversion bill | **Total, of budget** |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");

        foreach (int fleet in InFlight)
        {
            long diversions = ((long)fleet * R8DiversionsPerTickHundredths) / (R8Travellers * 100L);

            foreach (int days in HabitLifetimeDays)
            {
                long formationsPerThousand = ((long)fleet * 1_000) / ((long)days * TicksPerDay);
                long formationBill = (formationsPerThousand * basis.WholeJourneyLastNs) / 1_000;
                long diversionBill = diversions * basis.RemainderNs;
                long total = formationBill + diversionBill;

                report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"| {fleet:N0} | {days} d "
                    + $"| {formationsPerThousand / 1_000}.{formationsPerThousand % 1_000:D3} "
                    + $"| {diversions:N0} "
                    + $"| {Ms(formationBill)} | {Ms(diversionBill)} "
                    + $"| **{Percent(total)}** |"));
            }
        }

        report.AppendLine();

        long anchorDiversions =
            ((long)R8Travellers * R8DiversionsPerTickHundredths) / (R8Travellers * 100L);
        long anchorFormations = ((long)R8Travellers * 1_000) / (7L * TicksPerDay);
        long anchorFormationBill = (anchorFormations * basis.WholeJourneyLastNs) / 1_000;
        long anchorDiversionBill = anchorDiversions * basis.RemainderNs;
        long anchorShare =
            (anchorDiversionBill * 10_000) / (anchorDiversionBill + anchorFormationBill);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The two consumers are not the same order of magnitude and nothing in the corpus says "
            + $"so.** At R8's own rung — 40,000 Travellers, a 7-Day Habit — formations cost "
            + $"{Ms(anchorFormationBill)} and diversions cost {Ms(anchorDiversionBill)}, which is "
            + $"**{Ratio(anchorDiversionBill, anchorFormationBill)} the formation bill and "
            + $"{anchorShare / 100}.{anchorShare % 100:D2}% of routing's total**. Every budget figure "
            + $"the corpus publishes counts only the other one."));
        report.AppendLine();

        report.AppendLine(
            "**Habit is doing exactly what `adr/0046` claims.** A Citizen that computes a route once "
            + "and keeps it for a week costs well under one search per Tick across a whole fleet — "
            + "R3's *85 Trip starts* is not the binding constraint and never was, because a Trip start "
            + "under static Habit is a **lookup**. The constraint is the thing the same ADR introduced "
            + "in its next paragraph.");
        report.AppendLine();
    }

    private static void AppendInversion(StringBuilder report, Basis basis)
    {
        report.AppendLine("### The feasible region, inverted");
        report.AppendLine();
        report.AppendLine(
            "Published R3's way — a threshold on a quantity, not a multiple over a guess — because the "
            + "diversion rate is the number most likely to move and the least likely to be measured "
            + "soon.");
        report.AppendLine();

        long anchor = 1_269;
        long researchFits = BudgetNanoseconds / basis.RemainderNs;
        long pessimistFits = BudgetNanoseconds / R8FlatSearchNs;

        report.AppendLine("| Policy | Cost per diversion | Diversions/Tick that fit | R8's rate is |");
        report.AppendLine("|---|---:|---:|---|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **re-search** — what the corpus specifies today | {Ns(basis.RemainderNs)} "
            + $"| {researchFits:N0} | **over**, by {Ratio(anchor, researchFits)} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| *the same, on R8.6's own diversion cost* | *{Ns(R8FlatSearchNs)}* "
            + $"| *{pessimistFits:N0}* | *over, by {Ratio(anchor, pessimistFits)}* |"));
        report.AppendLine(
            "| cache-served | **no hit rate exists to price this** | — | *see below* |");
        report.AppendLine(
            "| **rejoin** the Habit Route, no search — *unproposed* | — | unbounded "
            + "| **free — no search at all** |");
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The two ends of the basis disagree by {Ratio(R8FlatSearchNs, basis.RemainderNs)} and "
            + $"the conclusion survives both.** This section's remainder search is the optimistic end — "
            + $"a midpoint site on a graph nobody is bulldozing. R8.6's {Ns(R8FlatSearchNs)} is the "
            + $"pessimistic end, taken on sites a live fleet actually diverted at, and it is close to a "
            + $"*whole-journey* cost rather than half of one, which is itself worth someone's attention. "
            + $"**Between {pessimistFits:N0} and {researchFits:N0} diversions per Tick fit. R8 measured "
            + $"{anchor:N0}.**"));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**`adr/0046` and `adr/0047` are not jointly affordable under the policy the corpus "
            + $"currently specifies.** Sight makes diversion routine; `adr/0047` removed the only thing "
            + $"that served one cheaply; and a re-search per diversion is over the whole Tick budget "
            + $"at R8's own measured rate, under the optimistic basis, **at every rung of the "
            + $"in-flight band S0a derived for the target city** — not at some extrapolated future "
            + $"size. The band's own midpoint is 56,000."));
        report.AppendLine();

        report.AppendLine("#### What the cache would have to do, since nobody may quote what it does");
        report.AppendLine();
        report.AppendLine(
            "**R6.2 says in terms that no document may cite a hit rate from it** — it reports *blame* "
            + "precisely because the absolute rate rests on Trip repetition that needs `06` milestone "
            + "5b. So this row is not priced. It is **inverted**: a cached diversion costs "
            + "`hit + miss-rate × search`, and the question is what miss rate makes it fit.");
        report.AppendLine();

        report.AppendLine("| In flight | Diversions/Tick | Required hit rate, optimistic basis "
            + "| Required hit rate, R8.6's basis |");
        report.AppendLine("|---:|---:|---:|---:|");

        foreach (int fleet in InFlight)
        {
            long diversions = ((long)fleet * R8DiversionsPerTickHundredths) / (R8Travellers * 100L);
            long allowance = BudgetNanoseconds / diversions;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {fleet:N0} | {diversions:N0} "
                + $"| {RequiredHit(allowance, basis.CacheHitNs, basis.RemainderNs)} "
                + $"| {RequiredHit(allowance, basis.CacheHitNs, R8FlatSearchNs)} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**And R6.1b is the reason those are not attainable.** A diversion is keyed on *(wherever I "
            + "now am → destination)*, and a mid-journey position is an arbitrary point along a route "
            + "rather than a Building. R6.1b established that a coarse key collapses **nothing** unless "
            + "trips coincide at *both* ends — and diversion origins coincide far less than trip "
            + "origins do, because they are wherever congestion happened to be. **The cache is being "
            + "asked for its best case on its worst input.**");
        report.AppendLine();

        report.AppendLine(
            "**Three levers, and two of them are Ruleset numbers that are already unset.** Raise the "
            + "**Temperament** threshold so fewer drivers act on what Sight shows them; shrink the "
            + "**Sight Horizon** toward its 1-Segment floor; or take the last row — **let a diversion "
            + "rejoin the Habit Route without re-searching**, which is what a driver with a map in "
            + "their head actually does, and which nothing in the corpus proposes. The third is free "
            + "by construction and is a design question rather than a tuning one.");
        report.AppendLine();

        report.AppendLine(
            "**What this section does not do is pick one.** `05 §4` says a different route is a "
            + "different city, and all three change which route a Traveller takes — so this is session "
            + "**M**'s to answer and not a benchmark's. What R6.3 supplies is that **it must be "
            + "answered**, which the corpus did not previously know, because the two facts sat in "
            + "different documents and were never multiplied.");
        report.AppendLine();
    }

    /// <summary>
    /// The hit rate at which <c>hit + (1-h) × search</c> comes in under <paramref name="allowance"/>.
    /// Reported in hundredths, and as <i>unattainable</i> where even a perfect cache is too slow.
    /// </summary>
    private static string RequiredHit(long allowance, long hitNs, long searchNs)
    {
        if (allowance <= hitNs)
        {
            return "**unattainable — a 100% hit does not fit**";
        }

        long missPermille = ((allowance - hitNs) * 1_000) / searchNs;

        if (missPermille >= 1_000)
        {
            return "none — a miss every time still fits";
        }

        long hitPermille = 1_000 - missPermille;
        return string.Create(
            CultureInfo.InvariantCulture, $"**{hitPermille / 10}.{hitPermille % 10}%**");
    }

    private static long Elapsed(long start) =>
        (long)(Stopwatch.GetElapsedTime(start, Stopwatch.GetTimestamp()).Ticks * 100L);

    private static string Ns(long ns) => ns >= 1_000_000
        ? string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000_000}.{(ns / 10_000) % 100:D2} ms")
        : ns >= 1_000
            ? string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000}.{(ns / 10) % 100:D2} µs")
            : string.Create(CultureInfo.InvariantCulture, $"{ns} ns");

    private static string Ms(long ns) =>
        string.Create(CultureInfo.InvariantCulture, $"{ns / 1_000_000}.{(ns / 1_000) % 1_000:D3} ms");

    private static string Percent(long ns) => string.Create(
        CultureInfo.InvariantCulture,
        $"{(ns * 100) / BudgetNanoseconds}.{((ns * 10_000) / BudgetNanoseconds) % 100:D2}%");

    private static string Ratio(long a, long b) => b == 0
        ? "—"
        : string.Create(CultureInfo.InvariantCulture, $"{a / b}.{((a * 100) / b) % 100:D2}×");
}
