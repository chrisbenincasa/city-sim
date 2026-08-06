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
/// R4 — distance-vector, and whether the one structure that fits the Tick budget can be kept current.
/// </summary>
internal static class VectorReport
{
    /// <summary>R1's working anchor: 11 a side, 121 Districts, 2.21 km².</summary>
    private const int AnchorPerSide = 11;

    private const int RoundCap = 4096;
    private const int EditSamples = 8;
    private const int DetourSamples = 200;
    private const int DrawSamples = 2_000;
    private const int CostSamples = 150;

    /// <summary>Districts a side. 16, 121, 256, 400 and 1,024 Districts.</summary>
    private static readonly int[] DistrictRungs = [4, 11, 16, 20, 32];

    /// <summary>Arcs whose cost moved, per mille. The congestion-drift sweep.</summary>
    private static readonly int[] DriftPerMille = [1, 10, 100, 1_000];

    /// <summary>K0's whole-world footprint, the tripwire's threshold.</summary>
    private const long WorldBytes = 180_648_346;

    public static string Run()
    {
        var report = new StringBuilder();

        report.AppendLine("## S2 R4 — distance-vector, and the table that has to stay current");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine(
            "**R4 is not a beauty contest between two routers, and reading it as one would miss "
            + "what R3 handed it.** R3 found that no cluster size fits a per-Trip search into the "
            + "Tick budget — the best rung breaks even at 85 Trip starts — and named R2's next-hop "
            + "table as *\"the rung this arithmetic does not touch… a structural advantage over both "
            + "hierarchies rather than a faster constant, and it is R4's to press.\"* That table asks "
            + "for **no search at all** when a Trip starts, and 32 ns per Segment crossing "
            + "thereafter. At the derived 56,000 Travellers in flight and R2's measured 0.79 "
            + "crossings each per Tick, that is **1.24 ms of a 15.6 ms Tick**. It fits.");
        report.AppendLine();
        report.AppendLine(
            "**So the question R4 actually answers is what that table costs to maintain.** It is "
            + "built by 121 backward Dijkstras and R2 measured the build at 474.47 ms — thirty "
            + "Ticks — while the corpus's own framing of the routing problem is that *in a city "
            + "builder link deletion is the core verb*. Distance-vector is the answer "
            + "`plans/0010` names. It is not the only one, and measuring only the named candidate "
            + "is how a spike produces a verdict it has not earned, so four maintenance schemes "
            + "are measured against the same edits and a fifth against staleness.");
        report.AppendLine();

        var graph = GraphGenerator.Build(GraphParameters.Working);
        int[] freeFlow = (int[])graph.ArcCarTicks.Clone();
        var reverse = ReverseArcs.Of(graph);
        var sampler = new OdSampler(graph);
        var distribution = new OdDistribution(graph, sampler);
        var anchor = Districts.Partition(graph, AnchorPerSide);

        Mark("AppendDistribution");
        AppendDistribution(report, graph, distribution);
        Mark("AppendFootprint");
        AppendFootprint(report, graph);
        Mark("AppendColdStart");
        AppendColdStart(report, graph, reverse, anchor, freeFlow);
        Mark("AppendEdit");
        AppendEdit(report, graph, reverse, anchor, freeFlow);
        Mark("AppendSeverance");
        AppendSeverance(report, graph, reverse, anchor, freeFlow);
        Mark("AppendDrift");
        AppendDrift(report, graph, reverse, anchor, freeFlow);
        Mark("AppendRolling");
        AppendRolling(report, graph, reverse, anchor, freeFlow);
        Mark("AppendDetour");
        AppendDetour(report, graph, reverse, anchor, distribution, freeFlow);
        AppendVerdict(report);

        return report.ToString();
    }

    /// <summary>Progress to stderr. The report is built in memory and written once, so a long
    /// section is otherwise indistinguishable from a hang.</summary>
    private static void Mark(string section) =>
        Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {section}");

    // ---------------------------------------------------------------- R4.1

    private static void AppendDistribution(
        StringBuilder report, RoadGraph graph, OdDistribution distribution)
    {
        report.AppendLine(
            "### R4.1 — the origin-destination distribution, which S2 has been guessing since R0");
        report.AppendLine();
        report.AppendLine(
            "R0 drew origin-destination pairs uniformly and said so, flagging the draw as a "
            + "placeholder to be replaced by the distribution R1 derived. **R1 derived none, and R2 "
            + "and R3 inherited the placeholder unchanged.** R3 had to publish every speedup it "
            + "measured as an upper bound because of it. The debt is now four tasks old and R6 "
            + "cannot be run against it at all — a cache hit rate measured on a uniform draw is "
            + "close to meaningless, because what makes a route cache work is that real Trips "
            + "repeat.");
        report.AppendLine();
        report.AppendLine(
            "**This does not close the hole; it makes the hole an axis.** Nobody can produce the "
            + "real distribution until Trips exist, and inventing one and calling it *the* "
            + "distribution would bake a guess into every downstream figure while making it look "
            + "like a measurement — which is exactly why R0 drew uniformly, and it was right to. "
            + "What is available is this plan's own precedent, used for the Microscopic Cap, "
            + "District count and the peaking factor alike: **report a curve, do not choose a "
            + "number.**");
        report.AppendLine();
        report.AppendLine(
            "**Uniform is a rung of the same mechanism rather than a separate code path**, so a "
            + "difference between two rows is the shape and cannot be the machinery. This spike has "
            + "twice caught an instrument that could not move and once caught two rungs that were "
            + "secretly the same rung; a family whose null case is a member of the family is the "
            + "cheap defence against both.");
        report.AppendLine();

        var search = new PointToPoint(graph);

        report.AppendLine(
            "| Shape | Mean | Median | p90 | Mean route | Draws/pair | Exhausted | Sample |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");

        foreach (OdRung rung in OdDistribution.Rungs)
        {
            OdPair[] pairs = distribution.Draw(
                CounterHash.Seed, DrawSamples, Modes.Car, rung, out long attempts, out int exhausted);

            var distances = new int[pairs.Length];
            for (int i = 0; i < pairs.Length; i++)
            {
                distances[i] = pairs[i].StraightLineTiles;
            }

            Array.Sort(distances);

            long sum = 0;
            foreach (int d in distances)
            {
                sum += d;
            }

            // A route cost as well as a straight line, because the decay is applied to distance and
            // the thing every other figure in S2 is denominated in is time.
            long costSum = 0;
            int found = 0;
            for (int i = 0; i < CostSamples && i < pairs.Length; i++)
            {
                search.Bootstrap(pairs[i].Origin, pairs[i].Destination, Modes.Car, HeuristicKind.Chebyshev);
                var outcome = search.Expand();
                if (outcome.Found)
                {
                    costSum += outcome.CostTicks;
                    found++;
                }
            }

            int mean = (int)(sum / distances.Length);

            report.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {rung.Name} | {Kilometres(mean)} | {Kilometres(distances[distances.Length / 2])} "
                + $"| {Kilometres(distances[(distances.Length * 9) / 10])} "
                + $"| {(found == 0 ? "—" : Ticks((int)(costSum / found)))} "
                + $"| {Hundredths((int)((attempts * 100) / DrawSamples))} | {exhausted} "
                + $"| {DrawSamples} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "Distances are straight-line between Segment midpoints, converted at ~4 m per Tile. "
            + "*Mean route* is the flat A\\* cost in Ticks over the first "
            + CostSamples.ToString(CultureInfo.InvariantCulture)
            + " pairs of each rung — the same denominator R0 established, re-measured in this "
            + "process. *Draws/pair* is the rejection sampler's mean attempt count and *exhausted* "
            + "is how many draws hit the 65,536-attempt cap; the second column is reported because "
            + "the five instruments in this spike that earned their place did so on the day they "
            + "read something other than zero.");
        report.AppendLine();
    }

    // ---------------------------------------------------------------- R4.2

    private static void AppendFootprint(StringBuilder report, RoadGraph graph)
    {
        report.AppendLine("### R4.2 — the memory axis, and the tripwire that fires on it");
        report.AppendLine();
        report.AppendLine(
            "`plans/0010` calls a table per node per destination *\"the frightening axis and the one "
            + "most likely to fail `adr/0006`\"*, and writes the wire before the numbers: "
            + "**DSDV's routing tables exceeding the whole world's footprint puts distance-vector "
            + "out on memory alone.** The whole world is K0's "
            + Bytes(WorldBytes) + ".");
        report.AppendLine();
        report.AppendLine(
            "| Destinations | Granularity | Entries | Unsequenced | **DSDV** | Against the world |");
        report.AppendLine("|---:|---|---:|---:|---:|---:|");

        foreach (int perSide in DistrictRungs)
        {
            int count = perSide * perSide;
            AppendFootprintRow(report, graph, count, $"District, {perSide} a side");
        }

        AppendFootprintRow(report, graph, graph.Nodes, "**node**");

        report.AppendLine();
        report.AppendLine(
            "An entry is a cost, a next arc and — for DSDV — a sequence number, at 4 B each. "
            + "**The sequence numbers are a third of the table**, so the protocol `references.md` "
            + "insists on costs 50% more than the bare next-hop table R2 measured at 7.70 MiB.");
        report.AppendLine();
        report.AppendLine(
            "**The wire fires, and it fires on the granularity rather than on the protocol.** At "
            + "node granularity — the destination set an actual routing table would carry, and the "
            + "one Citybound's does — the table is three orders of magnitude past the whole world. "
            + "Sequence numbers do not cause that and removing them does not fix it. **So "
            + "distance-vector in this design can only ever address Districts**, which is not a "
            + "footnote about memory: it is what imports R2's 18.52% mean detour and the "
            + "representative funnel, because a destination the table can afford to name is a "
            + "District and not a place. The correctness cost is caused by the memory constraint, "
            + "and the two have been discussed separately everywhere in the corpus.");
        report.AppendLine();
    }

    private static void AppendFootprintRow(
        StringBuilder report, RoadGraph graph, int destinations, string label)
    {
        long entries = (long)destinations * graph.Nodes;
        long sequenced = entries * 3 * sizeof(int);
        long unsequenced = entries * 2 * sizeof(int);

        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"| {destinations:N0} | {label} | {entries:N0} | {Bytes(unsequenced)} "
            + $"| {Bytes(sequenced)} | {Ratio(sequenced, WorldBytes)} |"));
    }

    // ---------------------------------------------------------------- R4.3

    private static void AppendColdStart(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, Districts districts, int[] arcTicks)
    {
        report.AppendLine("### R4.3 — cold start, which is not the protocol's claim but is owed a number");
        report.AppendLine();

        var byDijkstra = new DistanceVector(graph, reverse, districts.Count);
        var byExchange = new DistanceVector(graph, reverse, districts.Count);

        // The whole sweep walked once before any of it is timed. R1 needed four warm-up schemes
        // before its cold-build column stopped falling smoothly with District count, because the
        // cost was per-process rather than per-rung; the board files it as a finding every later
        // sweeping task inherits.
        for (int d = 0; d < districts.Count; d++)
        {
            byDijkstra.Seed(d, districts.Representative[d], arcTicks);
            byExchange.ConvergeCold(d, districts.Representative[d], arcTicks, RoundCap);
        }

        long seedStart = Stopwatch.GetTimestamp();
        for (int d = 0; d < districts.Count; d++)
        {
            byDijkstra.Seed(d, districts.Representative[d], arcTicks);
        }

        long seedNanoseconds = Since(seedStart);

        long exchangeRelaxations = 0;
        int worstRounds = 0;
        int unconverged = 0;

        long exchangeStart = Stopwatch.GetTimestamp();
        for (int d = 0; d < districts.Count; d++)
        {
            RepairCost cost = byExchange.ConvergeCold(
                d, districts.Representative[d], arcTicks, RoundCap);
            exchangeRelaxations += cost.Relaxations;
            worstRounds = cost.Rounds > worstRounds ? cost.Rounds : worstRounds;
            unconverged += cost.Converged ? 0 : 1;
        }

        long exchangeNanoseconds = Since(exchangeStart);

        int disagree = 0;
        var truth = new int[graph.Nodes];
        for (int d = 0; d < districts.Count; d++)
        {
            byDijkstra.CopyCosts(d, truth);
            byExchange.Audit(d, districts.Representative[d], truth, out int wrong, out _);
            disagree += wrong;
        }

        report.AppendLine("| Build | Whole table | Per column | Relaxations | Worst rounds | Entries wrong |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|");
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"| backward Dijkstra | {Milliseconds(seedNanoseconds)} "
            + $"| {Nanoseconds(seedNanoseconds / districts.Count)} | — | — | — |"));
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"| vector exchange | {Milliseconds(exchangeNanoseconds)} "
            + $"| {Nanoseconds(exchangeNanoseconds / districts.Count)} "
            + $"| {exchangeRelaxations:N0} | {worstRounds} | {disagree:N0} |"));
        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"At the {districts.Count}-District anchor, over {graph.Nodes:N0} nodes. "
            + $"{unconverged} column(s) hit the {RoundCap:N0}-round cap."));
        report.AppendLine();
        report.AppendLine(
            "**Both arrive at the same table, and the one that was expected to lose does not.** "
            + "Vector exchange is Bellman-Ford with an active set — the version anyone would "
            + "actually write, not the textbook one that sweeps every node every round — and on a "
            + "road network it beats a binary-heap Dijkstra, because a degree-3 graph with "
            + "well-behaved costs settles nearly in order anyway and the heap is pure overhead. "
            + "**An earlier draft of this paragraph asserted the opposite** and was written before "
            + "the column existed; it is recorded because a spike whose prose predicts its own "
            + "numbers is a spike that will eventually publish the prediction instead of the "
            + "number.");
        report.AppendLine();
        report.AppendLine(
            "**What it does not show is that distance-vector is cheap**, and the next three sections "
            + "are where that is decided. Cold start is not the protocol's claim — repair is — so "
            + "every scheme below starts from the identical Dijkstra-built table, copied rather "
            + "than re-derived.");
        report.AppendLine();
    }

    // ---------------------------------------------------------------- R4.4

    private static void AppendEdit(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, Districts districts, int[] freeFlow)
    {
        report.AppendLine("### R4.4 — one deleted Segment, which is the core verb");
        report.AppendLine();
        report.AppendLine(
            "A Segment is deleted, the whole table is brought back to correct by each scheme in "
            + "turn, and every scheme is audited against a table rebuilt from scratch on the edited "
            + "graph. **The audit is not a formality**: this spike has published a `v/c` of 883×, a "
            + "pair of byte-identical rungs and a denominator wrong by 193%, and in every case the "
            + "surrounding columns looked healthy.");
        report.AppendLine();

        var pristine = new DistanceVector(graph, reverse, districts.Count);
        for (int d = 0; d < districts.Count; d++)
        {
            pristine.Seed(d, districts.Representative[d], freeFlow);
        }

        var truth = new DistanceVector(graph, reverse, districts.Count);
        var working = new DistanceVector(graph, reverse, districts.Count);
        var arcTicks = new int[graph.Arcs];
        var changedArcs = new int[64];
        var changedNodes = new int[64];
        var truthCosts = new int[graph.Nodes];

        long[] elapsed = new long[4];
        long[] relaxations = new long[4];
        long[] rounds = new long[4];
        int[] wrongCost = new int[4];
        int[] noRoute = new int[4];
        int[] unconverged = new int[4];

        // The rebuild denominator is measured first and last, and both are published. R3 found the
        // same figure reading 1,401,307 ns measured first and 477,609 ns measured last in one
        // process, and every ratio it published divided by it.
        long rebuildFirst = 0;
        long rebuildLast = 0;

        for (int sample = 0; sample < EditSamples; sample++)
        {
            Array.Copy(freeFlow, arcTicks, freeFlow.Length);

            int segment = CounterHash.Below(
                CounterHash.Of(CounterHash.Seed, (ulong)sample, 0, CounterHash.Purpose.EditSegment),
                graph.Segments);

            int changed = Delete(graph, reverse, arcTicks, segment, changedArcs, changedNodes);
            if (changed == 0)
            {
                continue;
            }

            // Scheme 0 — rebuild every column. Also the ground truth every other scheme is audited
            // against, so it is computed once and used twice.
            long start = Stopwatch.GetTimestamp();
            for (int d = 0; d < districts.Count; d++)
            {
                truth.Seed(d, districts.Representative[d], arcTicks);
            }

            long rebuild = Since(start);
            elapsed[0] += rebuild;
            Mark($"  edit {sample} scheme 0 (rebuild): {rebuild / 1_000_000} ms");

            if (sample == 0)
            {
                rebuildFirst = rebuild;
            }

            rebuildLast = rebuild;

            for (int scheme = 1; scheme < 4; scheme++)
            {
                working.CopyFrom(pristine);

                long schemeStart = Stopwatch.GetTimestamp();
                for (int d = 0; d < districts.Count; d++)
                {
                    RepairCost cost = scheme switch
                    {
                        1 => working.Repair(
                            d, districts.Representative[d], arcTicks, changedNodes, changed,
                            VectorProtocol.Sequenced, RoundCap),
                        2 => working.Repair(
                            d, districts.Representative[d], arcTicks, changedNodes, changed,
                            VectorProtocol.Unsequenced, RoundCap),
                        _ => working.RepairSubtree(
                            d, districts.Representative[d], arcTicks, changedArcs, changed),
                    };

                    relaxations[scheme] += cost.Relaxations;
                    rounds[scheme] += cost.Rounds;
                    unconverged[scheme] += cost.Converged ? 0 : 1;
                }

                long schemeCost = Since(schemeStart);
                elapsed[scheme] += schemeCost;
                Mark($"  edit {sample} scheme {scheme}: {schemeCost / 1_000_000} ms");

                for (int d = 0; d < districts.Count; d++)
                {
                    truth.CopyCosts(d, truthCosts);
                    working.Audit(
                        d, districts.Representative[d], truthCosts, out int wrong, out int lost);
                    wrongCost[scheme] += wrong;
                    noRoute[scheme] += lost;
                }
            }
        }

        long entries = (long)EditSamples * districts.Count * graph.Nodes;

        report.AppendLine(
            "| Scheme | Per edit | Against rebuild | Relaxations | Rounds / settles | Wrong cost | Stranded |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|");

        string[] names =
        [
            "**rebuild** — every column",
            "DSDV, sequenced",
            "DSDV, unsequenced",
            "**dynamic repair** — affected subtree",
        ];

        for (int scheme = 0; scheme < 4; scheme++)
        {
            long per = elapsed[scheme] / EditSamples;
            report.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {names[scheme]} | {Milliseconds(per)} "
                + $"| {(scheme == 0 ? "—" : elapsed[scheme] <= elapsed[0] ? Ratio(elapsed[0], elapsed[scheme]) + " faster" : "**" + Ratio(elapsed[scheme], elapsed[0]) + " slower**")} "
                + $"| {(scheme == 0 ? "—" : (relaxations[scheme] / EditSamples).ToString("N0", CultureInfo.InvariantCulture))} "
                + $"| {(scheme == 0 ? "—" : (rounds[scheme] / EditSamples).ToString("N0", CultureInfo.InvariantCulture))} "
                + $"| {(scheme == 0 ? "—" : $"{wrongCost[scheme]:N0} ({Percent(wrongCost[scheme], entries)})")} "
                + $"| {(scheme == 0 ? "—" : noRoute[scheme].ToString("N0", CultureInfo.InvariantCulture))} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{EditSamples} deleted Segments, drawn uniformly, each repaired across all "
            + $"{districts.Count} columns — {entries:N0} entries audited per scheme. Columns that "
            + $"hit the {RoundCap:N0}-round cap: sequenced {unconverged[1]}, unsequenced "
            + $"{unconverged[2]}."));
        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"The rebuild denominator read {Nanoseconds(rebuildFirst)} on the first edit and "
            + $"{Nanoseconds(rebuildLast)} on the last, both published because R3 found the same "
            + $"quantity moving 193% between its first and last measurement in one process. "
            + $"**It also disagrees with R2's published build of 474.47 ms by rather more than the "
            + $"spread within this process**, and R4 does not resolve that: every ratio in this "
            + $"section is taken against R4's own in-process measurement, which is R3's rule, so no "
            + $"conclusion here moves either way. The discrepancy is owed to R7."));
        report.AppendLine();
    }

    // ---------------------------------------------------------------- R4.5

    private static void AppendSeverance(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, Districts districts, int[] freeFlow)
    {
        report.AppendLine("### R4.5 — a severed destination, and the failure sequence numbers exist for");
        report.AppendLine();
        report.AppendLine(
            "`references.md` is categorical — *\"if we adopt distance-vector routing, we take DSDV's "
            + "version, not Citybound's\"* — because Citybound's entries carry no sequence numbers "
            + "and link deletion count-to-infinities. **Under `adr/0043` that is a measurable claim "
            + "and it has never been measured**: it names a failure, a trigger and a mechanism, so "
            + "it has a refuting number, and R4 is the machine.");
        report.AppendLine();
        report.AppendLine(
            "Every arc into and out of one District's representative is deleted, which is a "
            + "bulldozed cul-de-sac and makes that destination unreachable from everywhere. Only "
            + "that District's column is repaired, because that is the column the severance breaks.");
        report.AppendLine();

        int district = districts.Count / 2;
        int target = districts.Representative[district];

        if (target < 0)
        {
            report.AppendLine("*Anchor District has no representative; section not run.*");
            report.AppendLine();
            return;
        }

        var arcTicks = (int[])freeFlow.Clone();
        var changedArcs = new List<int>();
        var changedNodes = new List<int>();

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (graph.ArcTarget[arc] != target && reverse.Source[arc] != target)
            {
                continue;
            }

            arcTicks[arc] = RoadGraph.Impassable;
            changedArcs.Add(arc);
            changedNodes.Add(reverse.Source[arc]);
        }

        int[] arcs = [.. changedArcs];
        int[] nodes = [.. changedNodes];

        var truth = new DistanceVector(graph, reverse, 1);
        truth.Seed(0, target, arcTicks);
        var truthCosts = new int[graph.Nodes];
        truth.CopyCosts(0, truthCosts);

        report.AppendLine("| Scheme | Rounds | Relaxations | Converged | Wrong cost | No route |");
        report.AppendLine("|---|---:|---:|---|---:|---:|");

        VectorProtocol[] protocols = [VectorProtocol.Sequenced, VectorProtocol.Unsequenced];

        foreach (VectorProtocol protocol in protocols)
        {
            var table = new DistanceVector(graph, reverse, 1);
            table.Seed(0, target, freeFlow);

            RepairCost cost = table.Repair(
                0, target, arcTicks, nodes, nodes.Length, protocol, RoundCap);

            table.Audit(0, target, truthCosts, out int wrong, out int lost);

            report.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| DSDV, {(protocol == VectorProtocol.Sequenced ? "sequenced" : "unsequenced")} "
                + $"| {cost.Rounds:N0} | {cost.Relaxations:N0} "
                + $"| {(cost.Converged ? "yes" : $"**no**, hit the {RoundCap:N0}-round cap")} "
                + $"| {wrong:N0} | {lost:N0} |"));
        }

        var subtree = new DistanceVector(graph, reverse, 1);
        subtree.Seed(0, target, freeFlow);
        RepairCost subtreeCost = subtree.RepairSubtree(0, target, arcTicks, arcs, arcs.Length);
        subtree.Audit(0, target, truthCosts, out int subtreeWrong, out int subtreeLost);

        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"| **dynamic repair** | {subtreeCost.Rounds:N0} settles | {subtreeCost.Relaxations:N0} "
            + $"| yes | {subtreeWrong:N0} | {subtreeLost:N0} |"));

        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{arcs.Length} arcs deleted, {graph.Nodes:N0} entries audited per scheme. "
            + $"*Wrong cost* counts entries whose cost differs from a table rebuilt on the severed "
            + $"graph; *no route* counts entries whose next hops do not reach the destination in "
            + $"`nodes` steps, which by the pigeonhole principle means a cycle. **The second column "
            + $"is the `adr/0006`-class defect `adr/0041` names in words** — a Traveller that never "
            + $"arrives, and *\"a road that looks busy forever.\"*"));
        report.AppendLine();
    }

    // ---------------------------------------------------------------- R4.6

    private static void AppendDrift(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, Districts districts, int[] freeFlow)
    {
        report.AppendLine("### R4.6 — congestion drift, which nothing in the corpus invalidates on");
        report.AppendLine();
        report.AppendLine(
            "**R3 left this as the largest unpriced exposure in the spike and it belongs here.** The "
            + "Epoch bumps on an *edit*; the VDF makes travel time a function of `volume / "
            + "capacity`; `adr/0041` moves volume **every Tick**. So every precomputed structure in "
            + "S2 is stale the Tick after it is built — HPA\\*'s intra-cluster edges, R2's next-hop "
            + "table and R1's matrix alike — and a flat search reads arc costs at query time and is "
            + "always current. That is a structural advantage of the denominator nobody has priced. "
            + "**A deleted road is the core verb; a changed travel time is every Tick.**");
        report.AppendLine();
        report.AppendLine(
            "The sweep is the fraction of arcs whose cost moved between one refresh and the next. "
            + "Nothing in the corpus says what that fraction is — it depends on the refresh cadence, "
            + "which is `plans/0010` decision 2 and still open — so it is swept and the **break-even "
            + "fraction** is what is published. That is the invertible form R3's rule requires: a "
            + "measured cost over a swept axis, containing no guess, still true when somebody "
            + "finally measures the cadence.");
        report.AppendLine();

        var pristine = new DistanceVector(graph, reverse, districts.Count);
        for (int d = 0; d < districts.Count; d++)
        {
            pristine.Seed(d, districts.Representative[d], freeFlow);
        }

        var working = new DistanceVector(graph, reverse, districts.Count);
        var truth = new DistanceVector(graph, reverse, districts.Count);
        var truthCosts = new int[graph.Nodes];
        var peak = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);

        report.AppendLine(
            "| Arcs moved | Rebuild | DSDV, sequenced | Dynamic repair | Repair vs rebuild | Wrong cost |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|");

        foreach (int perMille in DriftPerMille)
        {
            var arcTicks = (int[])freeFlow.Clone();
            var changedArcs = new List<int>();
            var changedNodes = new List<int>();

            for (int arc = 0; arc < graph.Arcs; arc++)
            {
                if (freeFlow[arc] == RoadGraph.Impassable)
                {
                    continue;
                }

                int roll = CounterHash.Below(
                    CounterHash.Of(CounterHash.Seed, (ulong)arc, (ulong)perMille,
                        CounterHash.Purpose.EditSegment),
                    1_000);

                if (roll >= perMille)
                {
                    continue;
                }

                arcTicks[arc] = peak[arc];
                changedArcs.Add(arc);
                changedNodes.Add(reverse.Source[arc]);
            }

            int[] arcs = [.. changedArcs];
            int[] nodes = [.. changedNodes];

            long rebuildStart = Stopwatch.GetTimestamp();
            for (int d = 0; d < districts.Count; d++)
            {
                truth.Seed(d, districts.Representative[d], arcTicks);
            }

            long rebuild = Since(rebuildStart);

            working.CopyFrom(pristine);
            long vectorStart = Stopwatch.GetTimestamp();
            for (int d = 0; d < districts.Count; d++)
            {
                working.Repair(
                    d, districts.Representative[d], arcTicks, nodes, nodes.Length,
                    VectorProtocol.Sequenced, RoundCap);
            }

            long vector = Since(vectorStart);

            working.CopyFrom(pristine);
            long subtreeStart = Stopwatch.GetTimestamp();
            for (int d = 0; d < districts.Count; d++)
            {
                working.RepairSubtree(d, districts.Representative[d], arcTicks, arcs, arcs.Length);
            }

            long subtree = Since(subtreeStart);

            int wrong = 0;
            for (int d = 0; d < districts.Count; d++)
            {
                truth.CopyCosts(d, truthCosts);
                working.Audit(d, districts.Representative[d], truthCosts, out int off, out _);
                wrong += off;
            }

            report.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {Hundredths(perMille * 10)}% ({arcs.Length:N0}) | {Milliseconds(rebuild)} "
                + $"| {Milliseconds(vector)} | {Milliseconds(subtree)} "
                + $"| {Ratio(rebuild, subtree)} | "
                + $"{Percent(wrong, (int)((long)districts.Count * graph.Nodes))} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"At the {districts.Count}-District anchor. Moved arcs take their morning-peak cost, so "
            + $"the change is the real congestion field rather than a synthetic perturbation. "
            + $"*Wrong cost* is the dynamic-repair rung audited against the rebuild."));
        report.AppendLine();
    }

    // ---------------------------------------------------------------- R4.7

    private static void AppendRolling(
        StringBuilder report, RoadGraph graph, ReverseArcs reverse, Districts districts, int[] freeFlow)
    {
        report.AppendLine("### R4.7 — the rolling refresh, which needs none of this machinery");
        report.AppendLine();
        report.AppendLine(
            "**The scheme a person would write if nobody had said the words *distance vector*.** "
            + "Rebuild *k* columns every Tick, forever, in rotation. It repairs edits and congestion "
            + "drift with one mechanism because it does not distinguish them; it needs no "
            + "invalidation, no Epoch and no sequence numbers; and its worst-case staleness is "
            + "bounded by construction at `destinations / k` Ticks. What it costs is a fixed slice "
            + "of every Tick whether or not anything changed.");
        report.AppendLine();

        var table = new DistanceVector(graph, reverse, districts.Count);
        for (int d = 0; d < districts.Count; d++)
        {
            table.Seed(d, districts.Representative[d], freeFlow);
        }

        long start = Stopwatch.GetTimestamp();
        for (int d = 0; d < districts.Count; d++)
        {
            table.Seed(d, districts.Representative[d], freeFlow);
        }

        long perColumn = Since(start) / districts.Count;

        report.AppendLine("| Columns per Tick | Cost per Tick | Share of 15.6 ms | Worst staleness |");
        report.AppendLine("|---:|---:|---:|---:|");

        int[] perTickRungs = [1, 2, 4, 8, districts.Count];

        foreach (int perTick in perTickRungs)
        {
            long cost = perColumn * perTick;
            int stale = (districts.Count + perTick - 1) / perTick;

            report.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {perTick} | {Nanoseconds(cost)} | {Percent((int)(cost / 1_000), 15_600)} "
                + $"| {stale} Ticks |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"One column costs {Nanoseconds(perColumn)} at the {districts.Count}-District anchor. "
            + $"**Staleness is in Ticks and a Tick is ~10.5 in-world seconds**, so a full rotation at "
            + $"one column per Tick is about 21 in-world minutes — well inside a congestion cycle and "
            + $"far outside a player's patience after deleting a road. The two consumers want "
            + $"different rates, which is the finding: **drift is satisfied by a slow rotation and an "
            + $"edit is not**, so a rolling refresh alone cannot serve the core verb no matter how it "
            + $"is tuned."));
        report.AppendLine();
    }

    // ---------------------------------------------------------------- R4.8

    private static void AppendDetour(
        StringBuilder report,
        RoadGraph graph,
        ReverseArcs reverse,
        Districts districts,
        OdDistribution distribution,
        int[] freeFlow)
    {
        report.AppendLine("### R4.8 — what the table's routes cost, on a distribution that is not uniform");
        report.AppendLine();
        report.AppendLine(
            "R2 measured a next-hop table's mean detour at **18.52%** against a shared District "
            + "route's 36.01% — on the uniform draw R4.1 has now replaced. A detour caused by "
            + "aiming at a District representative instead of at a destination is **a fixed error in "
            + "Ticks against a shrinking journey**, so it should worsen as trips get shorter, and "
            + "the uniform draw is the longest-trip rung there is. This is the recalibration.");
        report.AppendLine();
        report.AppendLine(
            "**Measured at the morning peak, on the same synthetic field R1 and R2 used**, because "
            + "R2's figure was and a detour compared against a free-flow one would be comparing two "
            + "graphs. The tail search starts at a Segment incident to the representative rather "
            + "than at the node itself, which can only make the followed route look cheaper, so "
            + "**every figure below is a lower bound** — R2's, measured at node granularity, was an "
            + "upper one, and the two bracket rather than contradict.");
        report.AppendLine();

        int[] peak = Congestion.CarTicks(graph, CongestionParameters.Working, Phase.MorningPeak);

        var table = new DistanceVector(graph, reverse, districts.Count);
        for (int d = 0; d < districts.Count; d++)
        {
            table.Seed(d, districts.Representative[d], peak);
        }

        var search = new PointToPoint(graph, peak);

        report.AppendLine("| Shape | Mean detour | p90 | Worst | Sample |");
        report.AppendLine("|---|---:|---:|---:|---:|");

        foreach (OdRung rung in OdDistribution.Rungs)
        {
            OdPair[] pairs = distribution.Draw(
                CounterHash.Seed, DetourSamples, Modes.Car, rung, out _, out _);

            var detours = new List<int>();

            foreach (OdPair pair in pairs)
            {
                search.Bootstrap(pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);
                var direct = search.Expand();
                if (!direct.Found || direct.CostTicks <= 0)
                {
                    continue;
                }

                int destinationDistrict = districts.OfNode[graph.SegmentNodeA[pair.Destination.Segment]];
                int representative = districts.Representative[destinationDistrict];
                if (representative < 0)
                {
                    continue;
                }

                // What the Traveller actually drives: to the representative by the table, then on
                // to where it was going. Coarse at the destination end only — the table is followed
                // from wherever the Traveller is, which is R2's stated reason the next-hop rung
                // costs half what a shared route does.
                int origin = graph.SegmentNodeA[pair.Origin.Segment];
                int viaTable = table.Cost(origin, destinationDistrict);
                if (viaTable >= DistanceVector.Unreachable)
                {
                    continue;
                }

                search.Bootstrap(
                    new AccessPoint(FirstSegmentAt(graph, representative), 0),
                    pair.Destination, Modes.Car, HeuristicKind.Chebyshev);

                var tail = search.Expand();
                if (!tail.Found)
                {
                    continue;
                }

                long followed = (long)viaTable + tail.CostTicks;
                detours.Add((int)(((followed - direct.CostTicks) * 10_000) / direct.CostTicks));
            }

            if (detours.Count == 0)
            {
                report.AppendLine(string.Create(
                    CultureInfo.InvariantCulture, $"| {rung.Name} | — | — | — | 0 |"));
                continue;
            }

            detours.Sort();
            long sum = 0;
            foreach (int detour in detours)
            {
                sum += detour;
            }

            report.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {rung.Name} | {Hundredths((int)(sum / detours.Count))}% "
                + $"| {Hundredths(detours[(detours.Count * 9) / 10])}% "
                + $"| {Hundredths(detours[^1])}% | {detours.Count} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "Sample size is reported per rung, per the board's owed finding — R1 published a row "
            + "built from nine searches beside rows built from 2,244, because its sample shrank with "
            + "the swept axis. Here a pair is dropped only if either search fails, so the column is "
            + "a survivorship check as well as a sample size.");
        report.AppendLine();
    }

    // ---------------------------------------------------------------- verdict

    private static void AppendVerdict(StringBuilder report)
    {
        report.AppendLine("### What R4 decided, and what it did not");
        report.AppendLine();
        report.AppendLine("**Decided.**");
        report.AppendLine();
        report.AppendLine(
            "- **Distance-vector is out, and on none of the three grounds anybody expected.** Not "
            + "memory: at District granularity the table is 23.12 MiB against a 172.27 MiB world, "
            + "and `plans/0010`'s wire does not fire. Not correctness: with sequence numbers it "
            + "converges to exactly the rebuilt table, 0 entries wrong, on both a deleted Segment "
            + "and a severance. **It is out because it costs more than the rebuild it exists to "
            + "avoid** — 472.53 ms against 217.36 ms for one deleted Segment, **2.17× slower** — and "
            + "145× more than the scheme this plan never named.");
        report.AppendLine();
        report.AppendLine(
            "  The reason is structural rather than a constant, which is why no tuning recovers it. "
            + "An odd-sequence unreachability claim outranks every finite route in circulation by "
            + "construction — that is exactly what stops count-to-infinity — so once the poison has "
            + "spread, **nothing any neighbour still believes can restore a route.** Only a newer "
            + "*even* sequence number, issued by the destination itself, outranks it. So one broken "
            + "link obliges the destination to re-flood its entire tree, because every node must at "
            + "minimum accept the new number. **The property that makes deletion safe is the same "
            + "property that makes deletion expensive**, and they cannot be separated.");
        report.AppendLine();
        report.AppendLine(
            "- **`references.md`'s claim about sequence numbers is confirmed by measurement**, and "
            + "under `adr/0043` it had never been more than an argument. On a severed destination "
            + "the unsequenced version does **215,894,753 relaxations against 133,258** — 1,620× the "
            + "work — fails to converge within 4,096 rounds, and leaves **16,684 of 16,697 entries "
            + "wrong**. The sequenced version converges in 121 rounds with nothing wrong. *If we "
            + "adopt distance-vector routing, we take DSDV's version, not Citybound's* is now a "
            + "finding rather than a reading.");
        report.AppendLine();
        report.AppendLine(
            "- **The scheme that wins was not on the ballot.** Invalidating the affected subtree and "
            + "re-deriving it from its own valid boundary repairs a deleted Segment in **3.35 ms "
            + "against a 217.36 ms rebuild — 64.81× — with 0 entries wrong**, and converges on a "
            + "severance too. It is not distance-vector, it needs no sequence numbers and no Epoch, "
            + "and it was measured only because pricing solely the candidate a plan names is how a "
            + "spike produces a verdict it has not earned.");
        report.AppendLine();
        report.AppendLine(
            "- **A rebuild is not the fallback anybody feared.** It is 217.36 ms for the whole "
            + "121-column table — 1.75 ms per column — which a rolling refresh spends at **11.19% "
            + "of one Tick** for a full rotation every 121 Ticks. That is affordable. What it "
            + "cannot do is answer an *edit* promptly, because a rotation is a cadence and an edit "
            + "is an event: **drift wants a slow rotation and the core verb does not**, so the two "
            + "consumers need different mechanisms and this is the section that shows why.");
        report.AppendLine();
        report.AppendLine("**Not decided, and owed.**");
        report.AppendLine();
        report.AppendLine(
            "- **The next-hop table's error is far worse than R2 measured, and R4 does not know what "
            + "to do about it.** R2's **18.52%** mean detour was taken on the uniform draw, which "
            + "R4.1 shows is the longest-trip distribution available — 8.53 km mean on a 16.4 km "
            + "map. Aiming a Traveller at a District representative is a roughly fixed error in "
            + "Ticks charged against a shrinking journey, so the detour rises to **36.04%** at a "
            + "4.1 km mean, **62.02%** at 2.6 km and **128.82%** at 1.5 km. **A Traveller driving "
            + "more than twice as far as it should is a different city under `05 §4`**, not a "
            + "tuning figure. This does not decide against the table — it says the table's "
            + "granularity is the open question, and R2's decision 11 (the representative funnel) "
            + "is the same question arriving from the other side. **The two should be answered "
            + "once.**");
        report.AppendLine();
        report.AppendLine(
            "- **The congestion-drift break-even sits between 1% and 10% of arcs moved**, and "
            + "nothing in the corpus says which side the design lands on, because the refresh "
            + "cadence is `plans/0010` decision 2 and still open. Below it, dynamic repair wins; "
            + "above it, a plain rebuild wins and every incremental scheme is doing extra work to "
            + "arrive at the same table. **The cadence therefore chooses the maintenance scheme**, "
            + "which nobody has said, and it is a hash-bearing decision that was filed as tuning.");
        report.AppendLine();
        report.AppendLine(
            "- **The O-D distribution is an axis now and still not a measurement.** R4.1 replaces a "
            + "silent guess with a swept family, which is what this plan does everywhere else, but "
            + "the family is invented. What would replace it is Trip generation, and that does not "
            + "exist. **Every figure in R4.8, and every speedup R3 published, is a point on a curve "
            + "whose location nobody can yet fix.**");
        report.AppendLine();
        report.AppendLine(
            "- **R4's rebuild denominator disagrees with R2's published build by 2.2×** — 217.36 ms "
            + "against 474.47 ms for the same 121 backward Dijkstras. Every ratio here is taken "
            + "in-process against R4's own measurement, per R3's rule, so no conclusion moves; but "
            + "two S2 tasks now publish different absolutes for the same operation and R7 owes the "
            + "reconciliation.");
        report.AppendLine();
        report.AppendLine("**Four defects in R4's own harness, all caught by instruments rather than by reading.**");
        report.AppendLine();
        report.AppendLine(
            "- **The sequenced protocol was missing DSDV's acceptance rule** — a node must *reject* "
            + "an advertisement older than what it already holds, not merely prefer newer ones. "
            + "Without it a poisoned node kept its odd sequence number while adopting a neighbour's "
            + "stale finite cost, then advertised that stale cost under the high sequence its own "
            + "poison had earned. **The first capture reported 232 seconds per edit and would have "
            + "published *distance-vector loses by three orders of magnitude*.** With the rule, the "
            + "same measurement is 472.53 ms. What flagged it was R2's own recorded lesson: the "
            + "sequenced and unsequenced rungs were reporting near-identical relaxation counts and "
            + "*identical* wrong-entry counts, and **two measurements that agree that closely are "
            + "not two measurements.**");
        report.AppendLine();
        report.AppendLine(
            "- **The poison phase was a silent no-op.** It seeded the flood with the nodes that "
            + "detected the break, which correctly reject every stale claim and therefore never "
            + "change and never notify anybody. In DSDV the detector *advertises*; the "
            + "advertisement is the event, not something a node discovers by looking around. The "
            + "phase converged in 2 rounds and 24 relaxations while leaving 16,680 of 16,697 "
            + "entries wrong — **and the report read \"converged: yes\", because a phase that does "
            + "nothing does it very quickly.**");
        report.AppendLine();
        report.AppendLine(
            "- **The audit counted the destination itself as stranded**, one phantom per column, "
            + "which read as a suspiciously round 121. A defect that produces a plausible number is "
            + "worse than one that produces an absurd one.");
        report.AppendLine();
        report.AppendLine(
            "- **The elapsed-time helper overflowed.** `elapsed × 1,000,000,000` passes "
            + "`long.MaxValue` at about 9.2 seconds on a nanosecond clock, and the first capture "
            + "published **−8,267.51 ms** for the rung that then took four minutes. Every earlier S2 "
            + "section times loops far below that threshold, so the same expression has been correct "
            + "everywhere else in this harness — **a helper is only as safe as the largest quantity "
            + "anybody has yet asked it to measure.**");
        report.AppendLine();
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    /// Makes a Segment's arcs impassable, collecting the arcs and the nodes that lose an option.
    /// </summary>
    /// <remarks>
    /// The seed set is the <b>source</b> node of each deleted arc. That is the node whose own
    /// re-derivation can discover the loss, because a node's entry is computed from its outgoing
    /// arcs — seeding the far end instead would ask a node to notice a change it cannot see.
    /// </remarks>
    private static int Delete(
        RoadGraph graph, ReverseArcs reverse, int[] arcTicks, int segment, int[] arcs, int[] nodes)
    {
        int count = 0;

        for (int arc = 0; arc < graph.Arcs && count < arcs.Length; arc++)
        {
            if (graph.ArcSegment[arc] != segment || arcTicks[arc] == RoadGraph.Impassable)
            {
                continue;
            }

            arcTicks[arc] = RoadGraph.Impassable;
            arcs[count] = arc;
            nodes[count] = reverse.Source[arc];
            count++;
        }

        return count;
    }

    private static int FirstSegmentAt(RoadGraph graph, int node)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            return graph.ArcSegment[arc];
        }

        return 0;
    }

    /// <summary>
    /// Elapsed nanoseconds since a <see cref="Stopwatch"/> timestamp.
    /// </summary>
    /// <remarks>
    /// <b>Split into whole seconds and a remainder because the obvious spelling overflows.</b>
    /// <c>elapsed × 1,000,000,000</c> passes <c>long.MaxValue</c> at about 9.2 seconds of elapsed
    /// time on a nanosecond-frequency clock, and R4 measures a rung that takes four minutes: the
    /// first capture published <b>−8,267.51 ms</b> for it. Every earlier S2 section timed loops far
    /// below the threshold, which is why the same expression has been correct everywhere else in
    /// this harness — a helper is only as safe as the largest quantity anybody has yet asked it to
    /// measure.
    /// </remarks>
    private static long Since(long start)
    {
        long elapsed = Stopwatch.GetTimestamp() - start;
        long whole = elapsed / Stopwatch.Frequency;
        long remainder = elapsed - (whole * Stopwatch.Frequency);
        return (whole * 1_000_000_000) + (remainder * 1_000_000_000 / Stopwatch.Frequency);
    }

    private static string Nanoseconds(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + " ns";

    private static string Milliseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10_000)) + " ms";

    private static string Ticks(int q16) => Hundredths((int)(((long)q16 * 100) >> 16)) + " Ticks";

    private static string Kilometres(int tiles) =>
        Hundredths(tiles * 4 / 10) + " km";

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
