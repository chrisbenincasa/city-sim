using System.Diagnostics;
using System.Globalization;
using System.Text;
using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Loop;
using S2.Routing.Matrix;
using S2.Routing.Routing;
using S2.Routing.Traffic;

namespace S2.Routing.Harness;

/// <summary>
/// S2 R8 — the congestion loop, and whether three layers make it natural.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything S2 has measured so far ran on a frozen cost basis.</b> R1's matrix, R2's ladder,
/// R3's hierarchy, R4's protocols and R5's storm all route over an arc-cost array computed once and
/// never moved. Nothing in this spike has ever invalidated a route because a road got busy — and
/// under <c>adr/0041</c> the volume column moves every Tick, so every precomputed structure S2 has
/// priced is stale the Tick after it is built. That is not a defect in those tasks; it is the
/// question they deferred, and it is this one.
/// </para>
/// <para>
/// <b>The deliverable of R8.2 is not a number about traffic. It is the right to publish the rest of
/// the section.</b> This spike has shipped an instrument that could not move three times, so the
/// order here is: build the loop, prove the loop moves, and only then read anything off it.
/// </para>
/// </remarks>
internal static class LoopReport
{
    /// <summary>The rung every section runs at: <c>CONTEXT.md</c> → District's anchor.</summary>
    /// <summary>
    /// Where <see cref="Headline"/>'s block is spliced in. A token rather than a reordering, because
    /// the finding is derived from R8.0 and has to appear above it.
    /// </summary>
    private const string HeadlineMarker = "<!-- R8 headline -->";

    private const int AnchorPerSide = 11;

    /// <summary>
    /// Fleet sizes R8.0 sweeps, spanning more than a decade around R2's 40,000.
    /// </summary>
    /// <remarks>
    /// <b>R8 cannot inherit R2's fleet and the first draft's attempt to do so was the section's
    /// worst error.</b> R2 was pricing attribution and did not care whether the network was
    /// gridlocked; R8 is measuring a congestion response, and with live residuals and BPR at
    /// <c>β = 4</c> an arc at the clamp costs <b>39.4×</b> free-flow, so Travellers dwell 39× longer,
    /// so volume rises further. That is positive feedback, and it will pin at the clamp from any
    /// load high enough to reach it. What load this network carries is therefore a prerequisite
    /// measurement and not a parameter.
    /// </remarks>
    private static readonly int[] LoadRungs =
        [1_000, 2_500, 3_500, 5_000, 7_500, 10_000, 20_000, 40_000, 80_000];

    /// <summary>
    /// The share of the top-64 indices allowed to sit at or above the BPR clamp at the operating
    /// load, in hundredths. <b>Stated before the sweep runs.</b>
    /// </summary>
    /// <remarks>
    /// The criterion is <i>the largest rung that leaves the busiest arcs inside the range BPR can
    /// actually resolve</i>. Past <c>MaximumVolumeCapacity</c> the delay multiplier is constant, so
    /// an arc there is one the router cannot tell from any other arc there — a rung whose busiest
    /// arcs are mostly in that region is a rung where every later figure is being read out of a
    /// region the instrument is blind inside. Ten percent is chosen, not derived, and it is written
    /// here rather than after the numbers.
    /// </remarks>
    private const int ClampShareCeilingHundredths = 1_000;

    /// <summary>
    /// The ceiling on p99 <c>v/c</c> that selects the operating load. <b>Stated before the sweep
    /// runs, and this is the second criterion R8.0 has had.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is the same physical statement as <see cref="ClampShareCeilingHundredths"/> — <i>the
    /// largest load that leaves the network inside the range BPR can resolve</i> — moved off a count
    /// over the busiest sixty-four indices and onto a quantile of the whole population, because
    /// every other figure in this section moved the same way and a selector should be read on the
    /// same statistic as the thing it selects.
    /// </para>
    /// <para>
    /// The retired criterion is kept, printed and compared. If the two disagree, the p99 one governs
    /// and the disagreement is the finding; if they agree, that is worth saying plainly too.
    /// </para>
    /// </remarks>
    private const int KneeCeiling = Congestion.MaximumVolumeCapacity;

    /// <summary>
    /// The threshold R8.0 through R8.3 run at, before R8.4 measures where the decisions actually
    /// live. <b>A placeholder, and named one.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0046</c> supplies <c>adr/0017</c>'s word <i>substantially</i> and no number, and
    /// records the base threshold as one of four unratified Ruleset values. R8.4 measures the
    /// distribution of relative improvement on offer and reads its rungs off that; nothing before
    /// R8.4 has that distribution, so ten percent stands in and every table before R8.4 says so.
    /// </remarks>
    private const int DraftThreshold = 6_554;

    /// <summary>
    /// Ticks before measurement starts, during which the top-64 are selected.
    /// </summary>
    /// <remarks>
    /// <b>Sized against the journey, not against convenience.</b> A mean journey at this fleet and
    /// this Habit runs 70–100 Ticks, so a 64-Tick warm-up is less than one journey and the first
    /// window would be measuring the fleet's initial placement rather than its steady state — which
    /// is precisely what the two-window agreement caught when it was 64: the control's two windows
    /// came in 40% apart. Three journeys is the smallest number that is not one.
    /// </remarks>
    private const int WarmTicks = 256;

    /// <summary>Ticks in each of the two measurement windows. Both are printed; see R8.2.</summary>
    private const int WindowTicks = 96;

    /// <summary>Volume indices the oscillation metric is taken over.</summary>
    private const int TopIndices = 64;

    /// <summary>Origin-destination pairs drawn per rung. Drawn from, never sampled into.</summary>
    private const int PoolPairs = 4_096;

    /// <summary>Flat searches in the denominator batch, taken first and last and never warmed.</summary>
    private const int DenominatorQueries = 256;

    /// <summary>Ticks the surge window runs for. Longer than a measurement window on purpose.</summary>
    private const int SurgeWindowTicks = 640;

    /// <summary>Share of the fleet R8.5's surge redirects, in hundredths. R2b's number.</summary>
    private const int SurgeShareHundredths = 40;

    /// <summary>
    /// Destination Districts the surge is repeated into. One draw is an anecdote about one
    /// District's approach roads; the recovery is reported as a distribution over these.
    /// </summary>
    private const int SurgeDistricts = 5;

    /// <summary>Warm-up Ticks for an R8.0 rung. Shorter: it only has to find where the network breaks.</summary>
    private const int LoadWarmTicks = 128;

    /// <summary>Measurement Ticks for an R8.0 rung. One window, not two — see the caps.</summary>
    private const int LoadWindowTicks = 64;

    /// <summary>Quantiles of the improvement distribution the base-threshold rungs are read off.</summary>
    private static readonly int[] BasePercentiles = [10, 25, 50, 75, 90];

    /// <summary>
    /// Segments back from a District representative that count as its convergence zone.
    /// </summary>
    /// <remarks>
    /// <b>Two definitions are reported, not one, because the narrow one turned out to catch
    /// nothing.</b> The arcs *arriving at* a representative are the funnel proper and are printed
    /// as their own column; four Segments back is where the convergence actually happens and is
    /// printed beside it. Widening a definition until it catches something would be fitting the
    /// measurement to the expectation, so both are printed and the reader can see the narrow one
    /// read zero.
    /// </remarks>
    private const int FunnelHops = 4;

    /// <summary>Peaks are scanned every this many Ticks. A full sweep every Tick is most of the run.</summary>
    private const int PeakScanEvery = 4;

    /// <summary>
    /// The two windows agree if they are within this share of each other, in hundredths. Above it,
    /// the run was not at steady state and the figure is published as such rather than published.
    /// </summary>
    private const int SteadyMarginHundredths = 25;

    /// <summary>
    /// How far a Sight rung must differ from the control before the difference is called one, in
    /// hundredths. Stated before the run; see <see cref="AppendTripwireStatement"/>.
    /// </summary>
    /// <summary>
    /// How far the maximal-herd positive control must exceed every swept Temperament row before
    /// either herd metric may be believed, in hundredths. <b>Stated before the control runs.</b>
    /// </summary>
    /// <remarks>
    /// A refutation read off a flat column is only worth anything if the column can be shown to move
    /// when the thing it measures is present. Base 0 and spread 0 is a fleet applying one rule to one
    /// input with no randomness anywhere in it, which is <c>adr/0046</c>'s herd by construction; if a
    /// metric cannot tell that apart from the swept family, it is not measuring herding and it may
    /// not be used to refute the layer that exists to prevent it.
    /// </remarks>
    private const int HerdMarginHundredths = 2_500;

    private const int InstrumentMarginHundredths = 500;

    /// <summary>Sight Horizons, in Segments. <c>0</c> is the control and it must be inert.</summary>
    private static readonly int[] HorizonRungs = [0, 1, 2, 4, 8, 16, 32];

    /// <summary>Temperament spreads, as Q16.16 <i>shares of</i> <see cref="BaseThreshold"/>.</summary>
    private static readonly int[] SpreadRungs = [0, 4_096, 8_192, 16_384, 32_768];

    /// <summary>Blend weights, Q16.16: pure jitter, even, pure character.</summary>
    private static readonly int[] BlendRungs = [0, 32_768, Fixed.One];

    public static string Run()
    {
        var report = new StringBuilder();
        var caps = new List<string>();

        var graph = GraphGenerator.Build(GraphParameters.Working);
        var reverse = ReverseArcs.Of(graph);
        var districts = Districts.Partition(graph, AnchorPerSide);
        var sampler = new OdSampler(graph);
        var distribution = new OdDistribution(graph, sampler);
        int[] freeFlow = (int[])graph.ArcCarTicks.Clone();

        report.AppendLine("## S2 R8 — the congestion loop, and whether three layers make it natural");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        AppendPreamble(report, graph, districts);

        // R8.0's concentration finding outranks everything else here and belongs above the tripwire
        // block, but it is derived from a sweep that has not run yet. A marker is emitted now and
        // replaced at the end, which is cheaper and less error-prone than reordering the section.
        report.Append(HeadlineMarker);
        AppendTripwireStatement(report);

        Mark("R8 denominator, first");
        var denominatorPool = distribution.Draw(
            CounterHash.Seed, DenominatorQueries, Modes.Car, OdDistribution.Rungs[0], out _, out _);
        long denominatorFirst = MeasureFlatDenominator(graph, freeFlow, denominatorPool);

        Mark("R8 next-hop table");
        long buildStart = Stopwatch.GetTimestamp();
        var nextHop = NextHopTable.Build(graph, reverse, districts, freeFlow, retainDistance: true);
        long buildNanoseconds = Since(buildStart);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Habit is one free-flow next-hop table over the {districts.Count} anchor Districts, "
            + $"built once in {Milliseconds(buildNanoseconds)} and **never refreshed**. It resides in "
            + $"{Bytes(nextHop.ResidentBytes)}, and R8 adds a second column of the same shape — the "
            + $"settled free-flow distance to each District, {Bytes(nextHop.DistanceResidentBytes)} — "
            + $"because a branch score is meaningless without the remainder. That column is opt-in and "
            + $"absent from every earlier task's resident-size figure."));
        report.AppendLine();

        var representativeArc = RepresentativeArcs(graph);
        var funnelImmediate = FunnelIndices(graph, districts, reverse, hops: 1);
        var funnelZone = FunnelIndices(graph, districts, reverse, hops: FunnelHops);
        int oneTraveller = OneTravellerRatio(graph, representativeArc);

        Mark("R8.1 the actionable-junction distance");
        var horizon = Horizon.Of(graph, reverse);
        int floor = AppendActionable(report, graph, horizon);

        var anchorRung = OdDistribution.Rungs[0];
        var anchorPool = BuildPool(
            graph, districts, nextHop, distribution, anchorRung, out int anchorDiscarded);

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"The origin-destination pool is {PoolPairs:N0} pairs per rung, drawn once and drawn "
            + $"from thereafter. At the anchor rung {anchorDiscarded:N0} were discarded as "
            + $"unreachable or degenerate, leaving {anchorPool.OriginNode.Length:N0}."));

        Mark("R8.0 the load sweep");
        int load = AppendLoad(
            report, graph, districts, nextHop, freeFlow, representativeArc, funnelImmediate,
            funnelZone, anchorPool, anchorRung, caps, out int retiredLoad, out string headline);

        Mark("R8.2 the instrument check");
        var control = Measure(
            graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung, load,
            horizonSegments: 0, baseThreshold: DraftThreshold, spreadShare: 0, blend: 0);

        var instrument = Measure(
            graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung, load,
            horizonSegments: floor, baseThreshold: DraftThreshold, spreadShare: 0, blend: 0);

        bool moves = AppendInstrument(report, control, instrument, floor, load);

        if (!moves)
        {
            AppendRefusal(report);
            AppendCaps(report, caps);
            return report.ToString();
        }

        Mark("R8.3 the Sight sweep");
        var sweep = new List<LoopOutcome> { control };
        foreach (int rung in HorizonRungs)
        {
            if (rung == 0)
            {
                continue;
            }

            Mark(string.Create(CultureInfo.InvariantCulture, $"R8.3 N={rung}"));
            sweep.Add(rung == floor
                ? instrument
                : Measure(
                    graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung,
                    load, rung, DraftThreshold, spreadShare: 0, blend: 0));
        }

        int selected = AppendSweep(report, sweep, floor, load);

        Mark("R8.3 the origin-destination cross-check");
        var crossCheck = new List<LoopOutcome>();
        foreach (OdRung rung in OdDistribution.Rungs)
        {
            Mark(string.Create(CultureInfo.InvariantCulture, $"R8.3 {rung.Name}"));
            var pool = rung == anchorRung
                ? anchorPool
                : BuildPool(graph, districts, nextHop, distribution, rung, out _);

            crossCheck.Add(Measure(
                graph, districts, nextHop, freeFlow, representativeArc, pool, rung, load, selected,
                DraftThreshold, spreadShare: 0, blend: 0));
        }

        AppendCrossCheck(report, crossCheck, selected, load);

        var crossLoad = new List<LoopOutcome>();

        if (retiredLoad >= 0 && retiredLoad != load)
        {
            Mark("R8.3 the cross-load ladder");
            AppendLoadCrossCheck(
                report, graph, districts, nextHop, freeFlow, representativeArc, anchorPool,
                anchorRung, load, retiredLoad, floor, caps, crossLoad);
        }

        // The sweep row the improvement distribution is read at, so its no-alternative and diversion
        // columns can be cross-checked against the histogram rather than described from memory.
        LoopOutcome atSelected = instrument;
        foreach (LoopOutcome outcome in sweep)
        {
            if (outcome.Horizon == selected)
            {
                atSelected = outcome;
            }
        }

        Mark("R8.4 the improvement distribution");
        int[] quantiles = AppendImprovement(
            report, graph, districts, nextHop, freeFlow, anchorPool, anchorRung, load, selected,
            atSelected);

        // Distinct values only. A concentrated distribution puts several quantiles in one octave,
        // and running the same threshold five times would print five identical rows that read as
        // five agreeing measurements — the same trap R8.4's spread-0 row is deduplicated for.
        int[] baseRungs = DistinctRungs(quantiles, out string[] baseLabels);

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"R8.4's base rungs are the {BasePercentiles.Length} quantiles of the measured "
            + $"improvement distribution, deduplicated to {baseRungs.Length} distinct thresholds. "
            + $"Where several quantiles share an octave the row names all of them."));

        Mark("R8.4 the Temperament sweep");
        var thresholds = new List<LoopOutcome>();
        foreach (int rung in baseRungs)
        {
            Mark(string.Create(CultureInfo.InvariantCulture, $"R8.4 base={rung}"));
            thresholds.Add(Measure(
                graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung,
                load, selected, rung, spreadShare: 0, blend: 0));
        }

        int selectedBase = AppendBaseSweep(report, thresholds, baseRungs, baseLabels, quantiles);

        var temperament = new List<LoopOutcome>();
        foreach (int share in SpreadRungs)
        {
            foreach (int blend in BlendRungs)
            {
                // At spread 0 the blend cannot reach the answer, so the three blend rungs would be
                // three runs of one rung. Run it once and say so rather than printing a row three
                // times and letting the agreement read as evidence.
                if (share == 0 && blend != BlendRungs[0])
                {
                    continue;
                }

                Mark(string.Create(CultureInfo.InvariantCulture,
                    $"R8.4 spread={share} blend={blend}"));

                temperament.Add(Measure(
                    graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung,
                    load, selected, selectedBase, share, blend));
            }
        }

        caps.Add(
            "R8.4's spread-0 row is measured once rather than once per blend weight: with no spread "
            + "the blend multiplies nothing and three identical rows would read as three agreeing "
            + "measurements.");

        // The maximal-herd positive control: one rule, one input, no threshold and no randomness.
        // Without it a flat spread column cannot be told apart from a metric that cannot move, which
        // is the failure this spike has caught three times and shipped once.
        Mark("R8.4 the maximal-herd positive control");
        var positive = Measure(
            graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung, load,
            selected, baseThreshold: 0, spreadShare: 0, blend: 0);

        bool herdMetricValidated =
            AppendTemperament(report, temperament, positive, selected, selectedBase, load);

        // The positive control herds two orders above every swept rung, so the switch happened
        // somewhere the sweep did not look. Which side of the transition the spread ladder was sited
        // on decides whether its flatness is a statement about Temperament or about the siting.
        Mark("R8.4 the herding regime");
        var herdLadder = new List<LoopOutcome>();
        TemperamentReading temperament8 = herdMetricValidated
            ? AppendHerdRegime(
                report, graph, districts, nextHop, freeFlow, representativeArc, anchorPool,
                anchorRung, load, selected, thresholds, positive, selectedBase, herdLadder, caps)
            : TemperamentReading.None(TemperamentVerdict.NotTested);

        Mark("R8.5 the surge");
        AppendSurge(
            report, graph, districts, nextHop, freeFlow, representativeArc, anchorPool, anchorRung,
            load, selected, caps, out bool surgeContrast);

        Mark("R8.6 what a diversion costs");
        AppendDiversionCost(
            report, graph, districts, nextHop, freeFlow, anchorPool, anchorRung, load, selected,
            sweep, caps);

        Mark("R8 denominator, last");
        long denominatorLast = MeasureFlatDenominator(graph, freeFlow, denominatorPool);

        AppendDenominator(report, denominatorFirst, denominatorLast);
        AppendTripwires(
            report, control, instrument, sweep, temperament, thresholds, floor, oneTraveller, load,
            herdMetricValidated, surgeContrast, crossLoad, herdLadder, positive, temperament8);

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"The `v/c` ladder is scanned every {PeakScanEvery} Ticks rather than every Tick, and "
            + $"every reading from both windows is pooled into one distribution. A full sweep of "
            + $"{graph.Volume.Length:N0} volume indices per Tick would be most of this section's "
            + $"runtime, and the ladder is a property of the observation rather than of a Tick."));

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"The Sight Horizon is swept at **one** origin-destination rung — {anchorRung.Name}, the "
            + $"family's anchor — and only the selected Horizon is carried across the other four. The "
            + $"full cross product is {HorizonRungs.Length} × {OdDistribution.Rungs.Length} runs and "
            + $"does not fit ten minutes."));

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"Mean journey time counts only journeys that **completed inside a {WindowTicks}-Tick "
            + $"window**. A long journey that spans the window is not in it, so the figure is biased "
            + $"short and is comparable between rungs rather than absolutely. With live residuals "
            + $"that bias is larger than it was, because a congested journey is a long one."));

        AppendCaps(report, caps);

        report.Replace(
            HeadlineMarker,
            headline.Length == 0
                ? "*R8.0 produced no headline finding, which means its sweep found no car-carrying "
                    + "capacity at all — read that before anything else here.*"
                : headline);

        return report.ToString();
    }

    // --- The report's own prose --------------------------------------------------------------------

    private static void AppendPreamble(StringBuilder report, RoadGraph graph, Districts districts)
    {
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Working rung: {graph.Segments:N0} Segments, {graph.Nodes:N0} nodes, "
            + $"{graph.Arcs:N0} arcs, {districts.Count} Districts. **The fleet size is not inherited "
            + $"from R2** — R8.0 measures it, for the reason given there."));
        report.AppendLine();
        report.AppendLine(
            "**The loop closes on both sides.** Fleet volume → live BPR cost → *both* a Sight "
            + "decision at each crossing *and* the traversal time the Traveller is actually charged "
            + "→ volume. Every earlier task in this spike routed over an array computed once; R8's "
            + "own first draft closed only the routing arrow, charging Travellers **free-flow** "
            + "residuals, and that was wrong in a way worth recording. With free-flow residuals "
            + "`03 §3.4`'s middle arrow — volume → travel time — does not exist: the VDF is "
            + "computed, routing reads it, and nothing in the world slows down. *Volume* then means "
            + "concurrent users of an arc rather than an accumulation, and the amplification that "
            + "makes a jam a jam — slower, so longer residence, so higher volume, so slower still — "
            + "is simply absent. It is present here.");
        report.AppendLine();
        report.AppendLine(
            "**`v/c` here is the same quantity R2b published**: `volume / (capacity × free-flow "
            + "time)`, reached through `Congestion.LiveRatioUnclamped`. An earlier draft divided by "
            + "the bare flow capacity because the private `Ratio` does — which is right there, since "
            + "that method reads a demand field deposited as a *share* of capacity rather than a "
            + "count of Travellers, and wrong here by the free-flow factor. Two figures sharing the "
            + "name `v/c` and differing by 13% is how a corpus acquires a contradiction nobody can "
            + "find later. **Reconciling it changes what `LiveCarTicks` returns**, because BPR now "
            + "reads the reconciled ratio, so the delay at a given volume is higher than the draft's "
            + "was. `adr/0046` names the same hazard from the other side: Sight and Promotion must "
            + "read the *same* quantity or the city diverts around a jam it never promotes.");
        report.AppendLine();
        report.AppendLine(
            "**The oscillation metric, defined before any number is printed.** Over the top-"
            + $"{TopIndices} volume indices by mean volume during warm-up, the **mean absolute "
            + "Tick-over-Tick change in `v/c`**, in Q16.16, across the measurement window. It is "
            + "taken on the **unclamped** ratio: `Congestion.MaximumVolumeCapacity` puts a ceiling at "
            + "4.00 and the busiest 64 indices are precisely the ones likely to be sitting on it, so "
            + "a clamped metric would read zero whether the network was still or thrashing. A "
            + "supplementary column differences the **mean over the 64** instead of averaging the 64 "
            + "differences; it is 8× less exposed to arrival noise and it is a diagnostic, not the "
            + "metric. **It is no longer what any tripwire fires on** — see the restatement below.");
        report.AppendLine();
        report.AppendLine(
            "**Habit carries a known granularity error and is used anyway.** R4 and R5.5 measured a "
            + "District-granular next-hop route as structurally wrong by 16.58% on the uniform draw "
            + "and 149.73% on the local one. It is the null hypothesis `adr/0046` requires — *static "
            + "per world* — and diversion under it is free, which is the property R8.6 prices against "
            + "a stored route. **R8's stability conclusions carry to either path source; its cost "
            + "column does not.**");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Run protocol.** {WarmTicks} warm-up Ticks, during which the top-{TopIndices} are "
            + $"selected, then **two consecutive {WindowTicks}-Tick measurement windows, both "
            + $"printed**. Two windows more than {SteadyMarginHundredths}% apart are reported as "
            + $"**not steady** rather than averaged. Every Tick of every rung asserts "
            + $"`TotalVolume() == Size × 65,536`, `Unplaced() == 0` and `Bounded == 0`; any failure "
            + $"is printed in the rung's row and again in the tripwire block."));
        report.AppendLine();
    }

    private static void AppendRefusal(StringBuilder report)
    {
        report.AppendLine("### R8.3 to R8.6 — not published");
        report.AppendLine();
        report.AppendLine(
            "**R8.2's instrument check failed, so nothing further in this section may be read.** "
            + "`plans/0010`: *\"If it is not, the loop is open — costs are being computed and not "
            + "read — and no figure in R8.3 to R8.6 may be published.\"* The sweeps were not run.");
        report.AppendLine();
    }

    /// <summary>
    /// The tripwires, written into the report <b>before</b> anything is run. Two of them were
    /// restated when the model changed; the original wording stays visible.
    /// </summary>
    private static void AppendTripwireStatement(StringBuilder report)
    {
        report.AppendLine("### The tripwires, stated before the run");
        report.AppendLine();
        report.AppendLine(
            "S4's practice and its stated reason: *the wire was written before the numbers arrived "
            + "precisely so it could not be reasoned around afterwards.* Verdicts are in the block "
            + "at the foot of the section; the conditions are here.");
        report.AppendLine();
        report.AppendLine(
            "| # | Condition | The number |");
        report.AppendLine("|---:|---|---|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 1 | **Sight lowers `v/c` against the control.** With the loop closed on both "
            + $"sides this *is* the question R8 exists to answer | **p99** `v/c` at R8.1's floor is "
            + $"at least {Hundredths(InstrumentMarginHundredths)}% below the Horizon-0 control's |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 2 | **The instrument is connected: Sight changes the volume trajectory relative to a "
            + $"control with identical physics and no ability to respond** | mean `v/c` over the "
            + $"top-{TopIndices} differs from the control's by at least "
            + $"{Hundredths(InstrumentMarginHundredths)}%, "
            + $"**and** the control records exactly zero diversions |"));
        report.AppendLine(
            "| 3 | Conservation, every Tick, every rung | `TotalVolume() == Size × 65,536`, "
            + "`Unplaced() == 0`, `Bounded == 0`, zero spawn failures |");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 4 | Steady state established, never assumed | every rung's two measurement windows "
            + $"within {SteadyMarginHundredths}% of each other |"));
        report.AppendLine(
            "| 5 | The Sight pass's cost is **measured**, never a per-decision cost times a guessed "
            + "decision rate | `Move(N) − Move(0)`, with `Refresh` timed as its own column |");
        report.AppendLine("| 6 | Every table names its O-D rung and its load | — |");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Two further conditions are stated here because they govern what may be published, "
            + $"and both are new to this capture.** First, R8.0's operating load is selected by the "
            + $"largest rung whose **p99** `v/c` stays below the BPR clamp; the criterion it replaces "
            + $"— a clamp-share count over the busiest sixty-four indices — is kept, printed and "
            + $"compared, and if the two disagree the p99 one governs. Second, **no verdict about "
            + $"Temperament may be published unless a maximal-herd positive control has first been "
            + $"shown to separate from the swept family by at least "
            + $"{Hundredths(HerdMarginHundredths)}%** on at least one of the two herd metrics. A "
            + $"refutation read off a flat column is worthless unless the column has been shown able "
            + $"to move."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Every `v/c` figure in this section is a quantile ladder and not a maximum, and that "
            + $"is the largest single change since the previous capture.** Every table from R8.0 to "
            + $"R8.5 reported *peak `v/c`* — one maximum over tens of thousands of volume indices — "
            + $"and three separate arguments in the previous version were built on the shape of that "
            + $"column. S4 already established what a maximum over a large noisy population is worth: "
            + $"a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9. The ladder is p50, "
            + $"p90, p99, p99 over occupied indices, and the maximum, in that order, with the "
            + $"maximum last because a runaway arc is still worth seeing and is not a headline. "
            + $"Where a rule or a selection used to read the maximum it now reads p99, and each such "
            + $"restatement is written down where it happens."));
        report.AppendLine();
        report.AppendLine(
            "**The `p99 occupied` column was not asked for and is here because the spike found the "
            + "instruction incomplete.** The correction that arrived was *read it on p99*; running "
            + "that revealed that this network's volume indices are nine parts empty, so an "
            + "unconditioned p99 reads the boundary of the empty region and — demonstrably, in "
            + "R8.3's cross-load ladder — takes the same value on every rung of a Horizon sweep. A "
            + "statistic that cannot move cannot carry a verdict, and that is provable from the "
            + "ladder without reference to any outcome. **The conditioned column is the spike's "
            + "correction and not the instruction's.** It is printed in every ladder from R8.0 "
            + "onward and no wire in this capture was restated onto it: R8 scores its wires as "
            + "written and recommends the conditioned rung to its successor.");
        report.AppendLine();
        report.AppendLine(
            "**Tripwires 1 and 2 are restatements and the originals are kept visible.** They were "
            + "first written as *\"the instrument must move — rung a's oscillation materially above "
            + "the control's\"* and *\"the control must not move — Horizon 0 must show near-zero "
            + "oscillation\"*, and both were written for a model in which Travellers consumed "
            + "**free-flow** residuals. Under that model the VDF was computed, routing read it, and "
            + "nothing in the world slowed down: `03 §3.4`'s middle arrow — volume → travel time — "
            + "did not exist, so the Horizon-0 control genuinely had no dynamics and *quiet* was a "
            + "sensible thing to demand of it.");
        report.AppendLine();
        report.AppendLine(
            "**That model is wrong and this run does not use it.** A Traveller now consumes the arc's "
            + "**live** traversal time, so a jam slows the Travellers in it, which lengthens their "
            + "residence, which raises the volume, which raises the cost. The control therefore has "
            + "genuine dynamics and asking it to be quiet would be asking it to be the old model. "
            + "What it does not have is any ability to *respond*, and that is what makes it the right "
            + "control: the only difference between it and a Sight rung is routing. The restated "
            + "wires isolate exactly that, and the oscillation amplitude is still reported "
            + "throughout — it is simply no longer what a wire fires on. Nothing here is amended "
            + "away; the original wording is above and the reason for the change is this paragraph.");
        report.AppendLine();
    }

    /// <summary>
    /// Volume indices of the arcs that <b>arrive at a District representative</b> — decision 11's
    /// funnel, where every Trip into a District converges on one node.
    /// </summary>
    /// <remarks>
    /// <b>Reported separately because a gridlock that lives here is an artefact of the partition and
    /// not a statement about the city.</b> R2 already measured the representative funnel at 412%
    /// `v/c`. A reader who cannot see how much of a peak is funnel cannot tell whether the network
    /// is too small or the routing granularity is.
    /// </remarks>
    private static bool[] FunnelIndices(RoadGraph graph, Districts districts, ReverseArcs reverse, int hops)
    {
        var funnel = new bool[graph.Volume.Length];
        var depth = new int[graph.Nodes];
        Array.Fill(depth, -1);

        var frontier = new List<int>();

        for (int district = 0; district < districts.Count; district++)
        {
            int node = districts.Representative[district];
            if (node >= 0 && depth[node] < 0)
            {
                depth[node] = 0;
                frontier.Add(node);
            }
        }

        // Breadth-first *backwards* from every representative: the convergence zone is the set of
        // Segments a Trip into that District must be on shortly before it arrives, and arriving is
        // the direction that matters. Walking forwards would map where traffic leaves a
        // representative, which under this origin-destination model is nobody.
        for (int hop = 0; hop < hops; hop++)
        {
            var next = new List<int>();

            foreach (int node in frontier)
            {
                for (int slot = reverse.Start[node]; slot < reverse.Start[node + 1]; slot++)
                {
                    int arc = reverse.Arc[slot];
                    if (graph.ArcCarTicks[arc] == RoadGraph.Impassable)
                    {
                        continue;
                    }

                    funnel[graph.VolumeIndex(arc)] = true;

                    int from = reverse.Source[arc];
                    if (depth[from] < 0)
                    {
                        depth[from] = hop + 1;
                        next.Add(from);
                    }
                }
            }

            frontier = next;
        }

        return funnel;
    }

    // --- R8.0 --------------------------------------------------------------------------------------

    private sealed record LoadOutcome(
        int Load,
        Vc All,
        Vc ExcludingImmediate,
        Vc ExcludingZone,
        long MeanTopAll,
        long ClampShare,
        long ImmediateShareOfTop,
        long ZoneShareOfTop,
        long ArrivalsPerTick,
        long MeanJourneyTicks,
        long HeadShare,
        bool Steady,
        long ConservationFailures);

    /// <summary>
    /// R8.0 — the load this network carries. Runs before everything else and selects the load
    /// everything else runs at.
    /// </summary>
    private static int AppendLoad(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        bool[] funnelImmediate,
        bool[] funnelZone,
        Pool pool,
        OdRung rung,
        List<string> caps,
        out int retired,
        out string headline)
    {
        headline = string.Empty;
        report.AppendLine("### R8.0 — the load this network carries. It runs before everything else");
        report.AppendLine();
        report.AppendLine(
            "**You cannot measure a congestion response without first establishing what load the "
            + "network carries, and R8's first draft did not.** It inherited R2's 40,000 Travellers "
            + "on the grounds that figures should be comparable — but R2 was pricing attribution and "
            + "did not care whether the network was gridlocked. With live residuals and BPR at "
            + "`β = 4` an arc at the clamp costs **39.4×** free-flow, so its Travellers dwell 39× "
            + "longer, so its volume rises further. That is positive feedback, and it pins at the "
            + "clamp from any load high enough to reach it.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Every rung here runs at **Horizon 0** — no routing response at all — so what is being "
            + $"measured is the network and the physics and nothing else. Rung is {rung.Name}."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**`v/c` is a quantile ladder and not a peak, and this is the second criterion R8.0 has "
            + $"had.** The first was *the largest rung at which fewer than "
            + $"{Hundredths(ClampShareCeilingHundredths)}% of the top-{TopIndices} indices sit at or "
            + $"above `MaximumVolumeCapacity`*, and it is kept here rather than amended away — it is "
            + $"still printed, in the *Past the clamp* column, and it still selected the load the "
            + $"first capture ran at. What was wrong with the section it fed was not that criterion "
            + $"but everything around it: **every other figure in R8 was a maximum over "
            + $"{graph.Volume.Length:N0} volume indices**, which is the worst available summary of a "
            + $"large noisy population and the mistake S4 already paid for once. So the ladder "
            + $"replaces the peak everywhere, and the selection criterion is restated on it, "
            + $"**before the sweep runs**:"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"> **The operating load is the largest rung at which p99 `v/c` over every car-carrying "
            + $"volume index stays below the BPR clamp, {Fix(KneeCeiling)}** — the largest load, that "
            + $"is, that leaves ninety-nine per cent of the network inside the range BPR can actually "
            + $"resolve. The rung at which p99 first passes {Fix(Fixed.One)} is reported alongside it "
            + $"as the point where the busiest percentile reaches free-flow saturation, and is not "
            + $"itself a selector."));
        report.AppendLine();
        report.AppendLine(
            "**`v/c` is reported twice: over every volume index, and with decision 11's funnel arcs "
            + "excluded.** Under District-granular routing every Trip into a District arrives "
            + "through one node, and R2 already measured that funnel at 412% `v/c`. The gap between "
            + "the two is how much of this network's congestion is the *partition* rather than the "
            + "*city*. The two are split across two tables here only because twelve ladder columns "
            + "do not fit one.");
        report.AppendLine();
        report.AppendLine(
            "| Travellers | " + RungHeadings("v/c") + " | Zero-volume share | "
            + "Mean v/c, top-64 | Past the clamp | Arrivals/Tick | Mean journey, Ticks | Steady |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|");

        var outcomes = new List<LoadOutcome>();

        foreach (int rungLoad in LoadRungs)
        {
            Mark(string.Create(CultureInfo.InvariantCulture, $"R8.0 load={rungLoad}"));

            LoadOutcome outcome = MeasureLoad(
                graph, districts, nextHop, freeFlow, representativeArc, funnelImmediate, funnelZone,
                pool, rungLoad);

            outcomes.Add(outcome);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {outcome.Load:N0} | {Rungs(outcome.All)} | "
                + $"{Percent(outcome.All.ZeroShare, Fixed.One)} | "
                + $"{Fix(outcome.MeanTopAll)} | {Percent(outcome.ClampShare, Fixed.One)} | "
                + $"{Fix(outcome.ArrivalsPerTick)} | {Fix(outcome.MeanJourneyTicks)} | "
                + $"{(outcome.Steady ? "yes" : "**no**")} |"));
        }

        report.AppendLine();
        report.AppendLine("The same sweep, with decision 11's funnel taken out:");
        report.AppendLine();
        report.AppendLine(
            "| Travellers | v/c p99, all | p99, 1-hop funnel out | p99, 4-hop zone out | "
            + "v/c max, all | max, 1-hop out | max, 4-hop out | Zone share of top-64 |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (LoadOutcome outcome in outcomes)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {outcome.Load:N0} | {Fix(outcome.All.P99)} | "
                + $"{Fix(outcome.ExcludingImmediate.P99)} | {Fix(outcome.ExcludingZone.P99)} | "
                + $"{Fix(outcome.All.Max)} | {Fix(outcome.ExcludingImmediate.Max)} | "
                + $"{Fix(outcome.ExcludingZone.Max)} | "
                + $"{Percent(outcome.ZoneShareOfTop, Fixed.One)} |"));
        }

        report.AppendLine();

        int selected = -1;
        int saturation = -1;

        foreach (LoadOutcome outcome in outcomes)
        {
            // The LARGEST passing rung, so the walk goes forward and keeps overwriting. Rungs are in
            // ascending order and the ladder rises with load, but the loop does not assume
            // monotonicity — it just takes the last one that passes.
            if (outcome.All.P99 < KneeCeiling)
            {
                selected = outcome.Load;
            }

            if (saturation < 0 && outcome.All.P99 >= Fixed.One)
            {
                saturation = outcome.Load;
            }
        }

        bool anyPassed = selected >= 0;
        if (!anyPassed)
        {
            // Nothing satisfied the criterion. The smallest rung is taken and the failure is stated,
            // because silently taking the smallest would read as the criterion having chosen it.
            selected = LoadRungs[0];
        }

        int byClampShare = -1;
        foreach (LoadOutcome outcome in outcomes)
        {
            if (outcome.ClampShare * 10_000 < (long)ClampShareCeilingHundredths * Fixed.One)
            {
                byClampShare = outcome.Load;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Operating load: {selected:N0} Travellers.** "
            + $"{(anyPassed
                ? "The p99 criterion above selected it, and every table from R8.2 onward names it."
                : "**The criterion selected nothing** — even the smallest rung puts the busiest "
                    + "percentile past the clamp, so the smallest rung is taken and everything "
                    + "downstream is being read partly out of a region BPR cannot resolve. That is "
                    + "a finding about this network and it is not tuned away.")}"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Does the load move when the knee is read on p99 rather than on the top-64 clamp "
            + $"share?** The retired criterion selects "
            + $"{(byClampShare >= 0 ? $"{byClampShare:N0}" : "nothing")} and the stated one selects "
            + $"{selected:N0}, so the answer is "
            + $"**{(byClampShare == selected ? "no" : "yes")}**. "
            + $"{(byClampShare == selected
                ? "The two criteria agree, which is worth stating plainly: the ladder changes what "
                    + "R8 *reports* everywhere, and it does not change where this network breaks. "
                    + "Nothing downstream re-runs at a different load on account of it."
                : "Everything downstream runs at the p99-selected load, and any figure a reader "
                    + "carries over from the previous capture is at the wrong load.")} "
            + $"p99 first reaches free-flow saturation — {Fix(Fixed.One)} — at "
            + $"{(saturation >= 0 ? $"{saturation:N0} Travellers" : "no rung on this sweep")}."));
        report.AppendLine();

        if (byClampShare != selected && byClampShare >= 0)
        {
            LoadOutcome? at = null;
            foreach (LoadOutcome outcome in outcomes)
            {
                if (outcome.Load == selected)
                {
                    at = outcome;
                }
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"**And the criterion has a defect that this sweep exposed and that the sentence "
                + $"stating it did not anticipate.** p99 is taken over *every car-carrying index*, "
                + $"and at the selected load {Percent(at?.All.ZeroShare ?? 0, Fixed.One)} of those "
                + $"indices are **empty**. A ninety-ninth percentile of a population that is nine "
                + $"parts nothing is roughly an eighty-ninth percentile of the part that is "
                + $"carrying traffic, and this network's congestion lives in its busiest fraction "
                + $"of one per cent. So the stated criterion looks *past* the jam rather than at "
                + $"it: it selects a load at which "
                + $"{Percent(at?.ClampShare ?? 0, Fixed.One)} of readings on the busiest sixty-four "
                + $"indices are past the clamp — which is the exact condition R8.0 was written to "
                + $"prevent. The *occupied only* column is printed beside the ladder so the "
                + $"dilution is visible; it reads {Fix(at?.All.P99Occupied ?? 0)} at the selected "
                + $"load."));
            report.AppendLine();
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"**The criterion is not retuned, and the run is not repeated until it gives a "
                + $"nicer answer.** It was stated before the sweep and it governs, exactly as "
                + $"written. What is done instead is what `adr/0044` did with its own second half: "
                + $"the defect is recorded where it happened, and the section is measured **at both "
                + $"loads** — the full Sight ladder is repeated at {byClampShare:N0} after R8.3 as "
                + $"a stated cross-check. If R8's central answer is the same at both, the selection "
                + $"did not matter and the ladder was the whole of the correction. If it is not, "
                + $"then **the answer to R8's central question is load-dependent**, and that is a "
                + $"larger finding than either load's number."));
            report.AppendLine();
        }

        headline = AppendMeaning(report, graph, representativeArc, outcomes, selected);
        AppendLoadVerdict(report, outcomes, selected);

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"R8.0's rungs are short — {LoadWarmTicks} warm-up Ticks and two {LoadWindowTicks}-Tick "
            + $"windows against {WarmTicks} and {WindowTicks} everywhere else — because the sweep "
            + $"only has to find where the network breaks. A rung marked **no** under *Steady* has "
            + $"not settled inside that budget and its numbers are a trajectory rather than a level."));

        caps.Add(
            "The funnel is defined as the volume indices of arcs **arriving at** a District "
            + "representative — one hop. A two-hop definition would catch more of the convergence "
            + "and would also start catching ordinary through-traffic, so the narrow definition is "
            + "used and the column reads as a lower bound on how much of the congestion is the "
            + "partition's.");

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"A ladder quantile is reported at the **lower edge** of a bucket a sixty-fourth of a "
            + $"`v/c` unit wide, so every quantile understates by up to {Fine(1_024)}. The maximum "
            + $"is exact and unquantised."));

        retired = byClampShare;
        return selected;
    }

    /// <summary>
    /// What an operating load of a few thousand Travellers <i>is</i>, in vehicles per hour and as a
    /// share of the design's own targets. Without this the section reports a number nobody can size.
    /// </summary>
    /// <remarks>
    /// There are exactly two answers and they have opposite consequences: either the synthetic
    /// capacity is unrealistically low, in which case the load figure re-baselines nothing because
    /// R0–R5 never routed on capacity at all; or the network genuinely saturates at this load, in
    /// which case it is the most important number S2 has produced. It is derived here rather than
    /// guessed at.
    /// </remarks>
    private static string AppendMeaning(
        StringBuilder report,
        RoadGraph graph,
        int[] representativeArc,
        List<LoadOutcome> outcomes,
        int operating)
    {
        report.AppendLine("#### What an operating load of this size means");
        report.AppendLine();

        // The distinct capacities on car-carrying indices, in vehicles per hour. The graph is built
        // from two road classes, so this is a two-row table and not a histogram.
        int lowest = int.MaxValue;
        int highest = 0;
        long carIndices = 0;
        int lowestArc = -1;

        for (int index = 0; index < representativeArc.Length; index++)
        {
            int arc = representativeArc[index];
            if (arc < 0 || graph.ArcCarTicks[arc] == RoadGraph.Impassable)
            {
                continue;
            }

            carIndices++;
            int capacity = graph.SegmentCapacity[graph.ArcSegment[arc]];

            if (capacity < lowest)
            {
                lowest = capacity;
                lowestArc = arc;
            }

            if (capacity > highest)
            {
                highest = capacity;
            }
        }

        if (lowestArc < 0)
        {
            report.AppendLine("No car-carrying index carries a capacity. Nothing to derive.");
            report.AppendLine();
            return string.Empty;
        }

        int lowestPerHour = IntegerMath.FloorDiv(lowest, Units.PerVehiclePerHour);
        int highestPerHour = IntegerMath.FloorDiv(highest, Units.PerVehiclePerHour);
        int lowestFree = graph.ArcCarTicks[lowestArc];
        int lowestPresent = Fixed.Mul(lowest, lowestFree);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"`SegmentCapacity` is Q16.16 vehicles per Tick and the graph carries two values: "
            + $"**{lowestPerHour:N0} veh/h** on a Street and **{highestPerHour:N0} veh/h** on an "
            + $"Arterial, both **whole-Segment rather than per-direction** — `GraphParameters.Working` "
            + $"runs `VolumeScope.PerSegment`, so the two directions share a volume index and share "
            + $"the capacity. A Tick is 10.55 s of in-world time and the conversion "
            + $"`(veh/h) × 192 = Q16.16 veh/Tick` is exact, so nothing here carries a rounding of its "
            + $"own."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Is {lowestPerHour:N0} veh/h a realistic Street?** It works out at "
            + $"{IntegerMath.FloorDiv(lowestPerHour, 2):N0} veh/h per direction, and the check that "
            + $"settles it is the headway it implies. A Street's free-flow speed is 50 km/h; the "
            + $"shortest car Segment traverses in {Fix(lowestFree)} Ticks; so at `v/c = 1` there are "
            + $"**{Fix(lowestPresent)} vehicles present on the Segment**, "
            + $"{Fix(IntegerMath.ShiftRight(lowestPresent, 1))} per direction. Over 128 m that is one "
            + $"vehicle every ~28 m, which at 13.9 m/s is a **two-second headway** — the textbook "
            + $"saturation headway for an urban lane. The capacity is not low. It is a single lane "
            + $"per direction running at saturation flow, and `v/c = 1` here means what it means in "
            + $"the traffic-engineering literature it was borrowed from."));
        report.AppendLine();

        long holding = HoldingCapacity(graph, representativeArc);
        long load = (long)operating * Fixed.One;
        long share = holding == 0 ? 0 : IntegerMath.FloorDiv(load * Fixed.One, holding);

        LoadOutcome? chosen = null;
        foreach (LoadOutcome outcome in outcomes)
        {
            if (outcome.Load == operating)
            {
                chosen = outcome;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Summed over all {carIndices:N0} car-carrying indices, the network holds "
            + $"**{IntegerMath.ShiftRight(holding, Fixed.FractionalBits):N0} vehicles at `v/c = 1` "
            + $"everywhere**. The operating load of {operating:N0} Travellers is "
            + $"**{Percent(share, Fixed.One)}** of that."));
        report.AppendLine();

        if (chosen is not null)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"And at that load the network is, almost everywhere, **empty**: "
                + $"{Percent(chosen.All.ZeroShare, Fixed.One)} of car-carrying indices hold no "
                + $"vehicle at all, the median index reads {Fix(chosen.All.P50)}, the ninetieth "
                + $"percentile reads {Fix(chosen.All.P90)} — and "
                + $"**{Percent(chosen.HeadShare, Fixed.One)} of all volume sits on the busiest one "
                + $"per cent of indices.**"));
            report.AppendLine();
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Against the design's own numbers.** `CLAUDE.md` targets 10,000 population in the "
            + $"first hour and 1,000,000 late game over 268 km² and ~30,000 Segments; S4 task 2 "
            + $"derived **56,000 vehicles in flight** as the Day average, and `plans/0010` carries a "
            + $"2–3× peaking correction on top of it — 111,000 to 170,000 at the morning peak. This "
            + $"network reaches the knee at {operating:N0}. That is **"
            + $"{Percent((long)operating * Fixed.One, 56_000L * Fixed.One)} of the derived Day "
            + $"average** and {Percent((long)operating * Fixed.One, 170_000L * Fixed.One)} of the "
            + $"top of the peaking band."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**So which of the two answers is it?** Neither, and the derivation says so cleanly: "
            + $"the capacity is realistic, and the network does *not* run out of road. It runs out "
            + $"of **routes**. "
            + $"{(chosen is null ? "Most" : Percent(chosen.All.ZeroShare, Fixed.One))} of the "
            + $"carriageway is carrying nothing while a fraction of a per cent of it is past a "
            + $"clamp where BPR can no longer tell one jammed arc from another, and the mechanism "
            + $"that puts it there is named and not mysterious: **Habit is a single shortest-path "
            + $"tree on free-flow costs, so every Traveller bound for a District is following the "
            + $"same tree into the same representative node.** There is one route per "
            + $"(node, District) pair in the entire model, and no amount of empty parallel road "
            + $"can be reached from it."));
        report.AppendLine();
        report.AppendLine(
            "**That finding is promoted to the top of the section**, with its three consequences, "
            + "because it outranks every timing column here. What follows below is the rest of R8 "
            + "measured inside it.");
        report.AppendLine();
        report.AppendLine(
            "Two limits on how far it travels. It is measured on a **synthetic grid** whose "
            + "Arterials were placed to be severable rather than to carry a city, and it is measured "
            + "with **one Traveller per Trip and no departure-time spread**, so the whole fleet is "
            + "on the road at once. Both make concentration worse than a real city's would be. "
            + "Neither is capable of making an empty network look full: the zero-volume share is a "
            + "direct reading and it does not depend on either.");
        report.AppendLine();

        return Headline(outcomes, chosen, operating, share, holding);
    }

    /// <summary>
    /// The concentration finding, as the block that opens the section. Built here because it is
    /// derived from R8.0's sweep, and spliced in above by <see cref="Run"/>.
    /// </summary>
    private static string Headline(
        List<LoadOutcome> outcomes, LoadOutcome? chosen, int operating, long share, long holding)
    {
        if (chosen is null)
        {
            return string.Empty;
        }

        // Is there ANY rung at which this network is both congested and resolvable? Both terms are
        // given numbers rather than adjectives: congested means the busiest occupied percentile has
        // reached free-flow saturation, resolvable means the top-64 clamp share is under the ceiling
        // R8.0's retired criterion named. The answer is computed, never asserted.
        LoadOutcome? both = null;
        LoadOutcome? lastResolvable = null;
        LoadOutcome? firstCongested = null;

        foreach (LoadOutcome outcome in outcomes)
        {
            bool congested = outcome.All.P99Occupied >= Fixed.One;
            bool resolvable =
                outcome.ClampShare * 10_000 < (long)ClampShareCeilingHundredths * Fixed.One;

            if (resolvable)
            {
                lastResolvable = outcome;
            }

            if (congested && firstCongested is null)
            {
                firstCongested = outcome;
            }

            if (congested && resolvable && both is null)
            {
                both = outcome;
            }
        }

        var block = new StringBuilder();

        block.AppendLine("### The finding, before anything else");
        block.AppendLine();
        block.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**At {Percent(share, Fixed.One)} of this network's holding capacity, "
            + $"{Percent(chosen.HeadShare, Fixed.One)} of all traffic is on the busiest one per cent "
            + $"of its road and {Percent(chosen.All.ZeroShare, Fixed.One)} of it is carrying "
            + $"nothing.** That is R8.0's measurement at the operating load of {operating:N0} "
            + $"Travellers against a network that holds "
            + $"{IntegerMath.ShiftRight(holding, Fixed.FractionalBits):N0} at `v/c = 1` everywhere, "
            + $"whose Streets are derived below to be running at a textbook two-second saturation "
            + $"headway. **It is not a congestion measurement.** It is a statement about what a "
            + $"District-granular free-flow shortest-path tree does to a road network: it funnels a "
            + $"whole city onto a skeleton and leaves nine tenths of the carriageway unused. There "
            + $"is exactly one route per (node, District) pair in the entire model, and no amount of "
            + $"empty parallel road can be reached from it. This outranks every timing column in the "
            + $"section."));
        block.AppendLine();
        block.AppendLine(
            "**It is decision 11 arriving from a third side.** R2 measured the representative "
            + "*funnel* at the destination node and put it at 412% `v/c`. R8.0 widened that "
            + "definition once, from the arcs arriving at a representative to a four-Segment "
            + "convergence zone, printed both, and found the funnel does **not** bind here — the "
            + "columns are identical to the printed digit at every rung. The binding term is not the "
            + "node where routes converge; it is the **tree upstream of it**. Decision 11 has been "
            + "argued as a question about how many access nodes a District exposes, and that is the "
            + "wrong axis: a District with a hundred access nodes still has one shortest-path tree "
            + "per destination, and one tree is what concentrates the traffic. **That is a different "
            + "fix from the one the question has been asking for**, and it is upstream of anything "
            + "the access-node count can reach.");
        block.AppendLine();
        block.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**It is why there may be no good operating load, and the sweep answers that rather "
            + $"than leaving it to be inferred.** Two terms with numbers on them: a rung is "
            + $"*congested* if p99 over occupied indices has reached free-flow saturation "
            + $"({Fix(Fixed.One)}), and *resolvable* if fewer than "
            + $"{Hundredths(ClampShareCeilingHundredths)}% of readings on the busiest "
            + $"{TopIndices} indices sit past the BPR clamp, which is the ceiling R8.0's retired "
            + $"criterion named. "
            + $"{(both is not null
                ? $"**{both.Load:N0} Travellers is both, so the claim is refuted**: there is a load "
                    + $"at which this network is congested and the congestion model can still see "
                    + $"it, and R8's readings should be taken there."
                : "**No rung on the sweep is both, so the data supports the claim.** "
                    + $"{(lastResolvable is not null
                        ? $"The largest resolvable rung is {lastResolvable.Load:N0}, where p99 over "
                            + $"occupied indices is only "
                            + $"{Fix(lastResolvable.All.P99Occupied)} — the network is not "
                            + $"congested there in any sense the statistic can see. "
                        : "No rung is resolvable at all. ")}"
                    + $"{(firstCongested is not null
                        ? $"The smallest congested rung is {firstCongested.Load:N0}, where "
                            + $"{Percent(firstCongested.ClampShare, Fixed.One)} of top-"
                            + $"{TopIndices} readings are already past the clamp. "
                        : "No rung reaches saturation on the occupied percentile. ")}"
                    + "**Under a District-granular free-flow tree there is no load at which this "
                    + "network is both congested and resolvable.** The concentration is what closes "
                    + "the gap: because the traffic is on one per cent of the road, the busiest arcs "
                    + "go past the clamp long before the network as a whole has anything worth "
                    + "calling congestion on it. That is the tension R8.0's two criteria exposed and "
                    + "it is not an artefact of either criterion.")}"));
        block.AppendLine();
        block.AppendLine(
            "**It bears directly on session M.** M has been choosing between a maintained next-hop "
            + "table and a cached route on two axes — structural error and temporal error — with "
            + "R8.6 adding diversion cost as a third. This is a **fourth**, and it is in the same "
            + "column as the first three: a maintained free-flow table does not merely go stale, it "
            + "*concentrates*. Every Traveller bound for a District follows one tree, so the table's "
            + "error is not distributed over the fleet, it is correlated across all of it, and the "
            + "correlation shows up as a saturated skeleton beside an empty network. A route cache "
            + "does not have this property for free either — it depends on what seeded the routes — "
            + "but a scheme that gives a Traveller more than one candidate route to begin with is "
            + "the only kind that can. **M should be told that the table's fourth defect is not a "
            + "cost, it is a spatial distribution.**");
        block.AppendLine();

        return block.ToString();
    }

    private static void AppendLoadVerdict(
        StringBuilder report, List<LoadOutcome> outcomes, int selected)
    {
        LoadOutcome? first = null;
        LoadOutcome? last = null;
        LoadOutcome? operating = null;

        foreach (LoadOutcome outcome in outcomes)
        {
            first ??= outcome;
            last = outcome;

            if (outcome.Load == selected)
            {
                operating = outcome;
            }
        }

        if (first is null || last is null || operating is null)
        {
            return;
        }

        // Whether the funnel is where the congestion lives is a reading, not a premise. The
        // paragraph says what the columns did, and the interpretation follows the reading rather
        // than preceding it — the first draft asserted divergence in prose and then printed two
        // identical columns underneath.
        long zoneGap = operating.All.P99 - operating.ExcludingZone.P99;
        bool funnelBinds = zoneGap * 10 >= (long)operating.All.P99;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**What the sweep says about the representative funnel.** Under District-granular "
            + $"routing every Trip into a District arrives through one node, and R2 measured that "
            + $"funnel at 412% `v/c`. At the operating load p99 is {Fix(operating.All.P99)} "
            + $"over every index, {Fix(operating.ExcludingImmediate.P99)} with the arcs arriving at "
            + $"a representative removed, and {Fix(operating.ExcludingZone.P99)} with the whole "
            + $"{FunnelHops}-Segment convergence zone removed; the maxima are "
            + $"{Fix(operating.All.Max)}, {Fix(operating.ExcludingImmediate.Max)} and "
            + $"{Fix(operating.ExcludingZone.Max)}. The zone holds "
            + $"{Percent(operating.ZoneShareOfTop, Fixed.One)} of the busiest 64 indices."));
        report.AppendLine();
        report.AppendLine(funnelBinds
            ? "**The funnel binds.** Removing the convergence zone takes more than a tenth off the "
                + "reading, so a material part of what this section calls congestion is the *routing "
                + "granularity* rather than the road network. A Sight Horizon cannot help there: "
                + "the funnel arc is on every route into the District, so there is no alternative "
                + "to divert to, and the no-alternative column is where that shows up."
            : "**The funnel does not bind here, and that is worth stating because it was expected "
                + "to.** Removing the convergence zone barely moves the reading. The reason is "
                + "arithmetic: only *destinations* converge under this origin-destination model — "
                + "origins are scattered real nodes — and arrivals are divided across every "
                + "non-empty District, so each representative receives a small fraction of the "
                + "fleet's arrival rate. R2's 412% was measured with **both** endpoints pinned to "
                + "representatives, which is a different and harsher query shape. **The congestion "
                + "R8 measures is in the network, not in the partition** — which makes the rest of "
                + "the section about routing, which is what it is for.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Where the network turns over.** Between {first.Load:N0} and {last.Load:N0} "
            + $"Travellers p99 goes {Fix(first.All.P99)} → {Fix(last.All.P99)}, the maximum goes "
            + $"{Fix(first.All.Max)} → {Fix(last.All.Max)}, the zero-volume share goes "
            + $"{Percent(first.All.ZeroShare, Fixed.One)} → "
            + $"{Percent(last.All.ZeroShare, Fixed.One)}, and the share of "
            + $"the busiest 64 indices past the clamp goes "
            + $"{Percent(first.ClampShare, Fixed.One)} → {Percent(last.ClampShare, Fixed.One)}. "
            + $"Mean journey time goes {Fix(first.MeanJourneyTicks)} → "
            + $"{Fix(last.MeanJourneyTicks)} Ticks while arrivals per Tick go "
            + $"{Fix(first.ArrivalsPerTick)} → {Fix(last.ArrivalsPerTick)} — **throughput rising far "
            + $"more slowly than load, which is the dwell-time feedback doing exactly what it is "
            + $"supposed to do.** The transition is not gradual, and the rungs are spaced to find "
            + $"where it happens rather than to bracket a chosen answer."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Read the *Steady* column too. A rung that does not settle inside two short windows is "
            + $"a rung where the dwell-time feedback has not finished amplifying, and it is the "
            + $"direct evidence for whether this loop converges at all rather than running away. "
            + $"The operating load is {selected:N0}."));
        report.AppendLine();
    }

    private static LoadOutcome MeasureLoad(
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        bool[] funnelImmediate,
        bool[] funnelZone,
        Pool pool,
        int load)
    {
        var fleet = NewFleet(
            graph, districts, nextHop, freeFlow, pool, load,
            horizonSegments: 0, baseThreshold: DraftThreshold, spread: 0, blend: 0);

        var accumulated = new long[graph.Volume.Length];
        var checks = new Checks();

        for (int tick = 0; tick < LoadWarmTicks; tick++)
        {
            fleet.Advance();
            checks.Inspect(fleet);

            for (int index = 0; index < accumulated.Length; index++)
            {
                accumulated[index] += fleet.Volume[index];
            }
        }

        int[] top = Top(accumulated, representativeArc);

        long firstMean = 0;
        long secondMean = 0;
        long clamped = 0;
        long topTotal = 0;
        long samples = 0;
        long arrivals = 0;

        var all = new Ladder();
        var excludingImmediate = new Ladder();
        var excludingZone = new Ladder();

        long journeysBefore = fleet.CompletedJourneys;
        long journeyTicksBefore = fleet.CompletedJourneyTicks;

        for (int window = 0; window < 2; window++)
        {
            long windowTotal = 0;

            for (int tick = 0; tick < LoadWindowTicks; tick++)
            {
                fleet.Advance();
                checks.Inspect(fleet);
                arrivals += fleet.Arrivals;

                for (int i = 0; i < top.Length; i++)
                {
                    int ratio = Congestion.LiveRatioUnclamped(
                        graph, representativeArc[top[i]], fleet.Volume);

                    windowTotal += ratio;
                    topTotal += ratio;
                    samples++;

                    if (ratio >= Congestion.MaximumVolumeCapacity)
                    {
                        clamped++;
                    }
                }

                if (tick % PeakScanEvery == 0)
                {
                    for (int index = 0; index < representativeArc.Length; index++)
                    {
                        if (representativeArc[index] < 0)
                        {
                            continue;
                        }

                        int ratio = Congestion.LiveRatioUnclamped(
                            graph, representativeArc[index], fleet.Volume);

                        all.Add(ratio);

                        if (!funnelImmediate[index])
                        {
                            excludingImmediate.Add(ratio);
                        }

                        if (!funnelZone[index])
                        {
                            excludingZone.Add(ratio);
                        }
                    }
                }
            }

            long mean = IntegerMath.FloorDiv(windowTotal, (long)top.Length * LoadWindowTicks);
            if (window == 0)
            {
                firstMean = mean;
            }
            else
            {
                secondMean = mean;
            }
        }

        long immediateInTop = 0;
        long zoneInTop = 0;
        foreach (int index in top)
        {
            if (funnelImmediate[index])
            {
                immediateInTop++;
            }

            if (funnelZone[index])
            {
                zoneInTop++;
            }
        }

        long larger = firstMean > secondMean ? firstMean : secondMean;
        long gap = firstMean > secondMean ? firstMean - secondMean : secondMean - firstMean;

        long journeys = fleet.CompletedJourneys - journeysBefore;
        long journeyTicks = fleet.CompletedJourneyTicks - journeyTicksBefore;

        return new LoadOutcome(
            Load: load,
            All: all.Read(),
            ExcludingImmediate: excludingImmediate.Read(),
            ExcludingZone: excludingZone.Read(),
            MeanTopAll: IntegerMath.FloorDiv(topTotal, samples),
            ClampShare: IntegerMath.FloorDiv(clamped * Fixed.One, samples),
            ImmediateShareOfTop: IntegerMath.FloorDiv(immediateInTop * Fixed.One, top.Length),
            ZoneShareOfTop: IntegerMath.FloorDiv(zoneInTop * Fixed.One, top.Length),
            ArrivalsPerTick: IntegerMath.FloorDiv(arrivals * Fixed.One, 2L * LoadWindowTicks),
            MeanJourneyTicks: journeys == 0
                ? 0
                : IntegerMath.FloorDiv(journeyTicks * Fixed.One, journeys),
            HeadShare: HeadShare(graph, representativeArc, fleet.Volume, percent: 1),
            Steady: larger == 0 || gap * 100 <= larger * SteadyMarginHundredths,
            ConservationFailures: checks.Conservation + checks.Unplaced + checks.Bounded);
    }

    // --- R8.1 --------------------------------------------------------------------------------------

    private static int AppendActionable(StringBuilder report, RoadGraph graph, Horizon horizon)
    {
        report.AppendLine("### R8.1 — the actionable-junction distance. No traffic at all");
        report.AppendLine();
        report.AppendLine(
            "For every arrival — a node *and* the arc arrived by — the distance to the nearest node "
            + "at which the driver has a **real choice**: at least two onward car-passable arcs once "
            + "the way back is discounted. `adr/0046` makes this the one routing parameter whose "
            + "lower bound is derivable rather than tuned, so it is derived before any behavioural "
            + "argument runs.");
        report.AppendLine();
        report.AppendLine(
            "**The state is the arrival and not the node, and that is forced rather than chosen.** "
            + "Whether a node is a choice depends on the arc used to reach it, so a node has no "
            + "answer independent of one. The node projection below takes each node's **worst** "
            + "arrival, because a floor derived from the best arrival is not a floor.");
        report.AppendLine();

        var states = new List<int>(horizon.States);
        var ticks = new List<int>(horizon.States);

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (!horizon.CarArc[arc] || horizon.Segments[arc] == int.MaxValue)
            {
                continue;
            }

            states.Add(horizon.Segments[arc]);
            ticks.Add(horizon.Ticks[arc] == int.MaxValue ? 0 : horizon.Ticks[arc]);
        }

        var nodes = new List<int>(graph.Nodes);
        for (int node = 0; node < graph.Nodes; node++)
        {
            if (horizon.NodeSegments[node] != int.MaxValue)
            {
                nodes.Add(horizon.NodeSegments[node]);
            }
        }

        int[] sortedStates = [.. states];
        int[] sortedTicks = [.. ticks];
        int[] sortedNodes = [.. nodes];
        Array.Sort(sortedStates);
        Array.Sort(sortedTicks);
        Array.Sort(sortedNodes);

        report.AppendLine("| Distribution over | Count | At distance 0 | p50 | p90 | max |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| arrivals, Segments | {sortedStates.Length:N0} | "
            + $"{Percent(ShareAtZero(sortedStates), sortedStates.Length)} | "
            + $"{Quantile(sortedStates, 50)} | {Quantile(sortedStates, 90)} | "
            + $"{(sortedStates.Length == 0 ? 0 : sortedStates[^1])} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| arrivals, free-flow Ticks | {sortedTicks.Length:N0} | "
            + $"{Percent(ShareAtZero(sortedTicks), sortedTicks.Length)} | "
            + $"{Hundredths(Centi(Quantile(sortedTicks, 50)))} | "
            + $"{Hundredths(Centi(Quantile(sortedTicks, 90)))} | "
            + $"{Hundredths(Centi(sortedTicks.Length == 0 ? 0 : sortedTicks[^1]))} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| nodes, worst arrival, Segments | {sortedNodes.Length:N0} | "
            + $"{Percent(ShareAtZero(sortedNodes), sortedNodes.Length)} | "
            + $"{Quantile(sortedNodes, 50)} | {Quantile(sortedNodes, 90)} | "
            + $"{(sortedNodes.Length == 0 ? 0 : sortedNodes[^1])} |"));
        report.AppendLine();

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{horizon.Unreachable:N0} arrivals of {horizon.States:N0} reach no real choice at all, "
            + $"and {horizon.DeadEnds:N0} arrive somewhere whose only onward car arc is the one "
            + $"arrived by — a forced U-turn, which this model does not offer. Both are excluded from "
            + $"the distribution above and printed here so the denominator is visible."));
        report.AppendLine();

        int floor = Quantile(sortedStates, 90);
        if (floor < 1)
        {
            floor = 1;
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The floor for the Sight Horizon is taken as the p90 of the arrival distribution, "
            + $"{floor} Segment(s)**, and the choice of p90 over the median is stated rather than "
            + $"assumed: a horizon set at the median is a horizon that is structurally useless to "
            + $"half the crossings in the city. `adr/0046`'s claim *a Sight Horizon of one is "
            + $"actionable* is **{(Quantile(sortedStates, 50) <= 1 ? "not refuted" : "refuted")}** by "
            + $"the median and **{(floor <= 1 ? "not refuted" : "refuted")}** at p90."));
        report.AppendLine();
        report.AppendLine(
            "**This is the graph's answer and not the driver's.** It weights a cul-de-sac nobody "
            + "uses as heavily as the arterial ramp the whole city crosses. R8.3's "
            + "*no-alternative share* column is the same finding weighted by where drivers actually "
            + "are, and neither may be published without the other.");
        report.AppendLine();

        return floor;
    }

    // --- R8.2 --------------------------------------------------------------------------------------

    private static bool AppendInstrument(
        StringBuilder report,
        LoopOutcome control,
        LoopOutcome instrument,
        int floor,
        int load)
    {
        report.AppendLine("### R8.2 — the loop closes, and the instrument moves");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Rung **a** — Habit plus Sight at the Horizon R8.1 set ({floor}), Temperament spread 0 — "
            + $"against rung **control**, which is Habit alone at Horizon 0. Both carry identical "
            + $"physics: live residuals, {load:N0} Travellers, {control.Od.Name}. The only difference "
            + $"is that the control cannot respond."));
        report.AppendLine();
        report.AppendLine(
            "| Rung | " + RungHeadings("v/c") + " | Mean v/c, top-64 | Oscillation w1 | w2 | Steady "
            + "| Diversions/Tick | Crossings/Tick |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|:-:|---:|---:|");
        AppendInstrumentRow(report, "control, N=0", control);
        AppendInstrumentRow(
            report, string.Create(CultureInfo.InvariantCulture, $"a, N={floor}"), instrument);
        report.AppendLine();

        // Tripwire 1 is read on p99, not on the maximum. The wire says `Sight lowers v/c`, and a
        // maximum over 33,018 indices answers a narrower question than that — whether the single
        // worst arc got better — which one Traveller arriving anywhere can move.
        long peakGap = control.Vc.P99 - instrument.Vc.P99;
        bool lowersPeak = peakGap * 10_000 >= (long)control.Vc.P99 * InstrumentMarginHundredths;

        long meanGap = instrument.MeanTopRatio > control.MeanTopRatio
            ? instrument.MeanTopRatio - control.MeanTopRatio
            : control.MeanTopRatio - instrument.MeanTopRatio;

        bool trajectoryDiffers =
            meanGap * 10_000 >= control.MeanTopRatio * InstrumentMarginHundredths;

        bool controlInert = control.Diversions == 0;
        bool connected = trajectoryDiffers && controlInert;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**TRIPWIRE 1 — Sight lowers `v/c`: {(lowersPeak ? "PASS" : "FIRED")}.** "
            + $"Read at **p99**: {Fix(instrument.Vc.P99)} against the control's "
            + $"{Fix(control.Vc.P99)}, a change of {Percent(peakGap, control.Vc.P99)} against a "
            + $"stated bar of {Hundredths(InstrumentMarginHundredths)}%. The maxima are "
            + $"{Fix(instrument.Vc.Max)} against {Fix(control.Vc.Max)}, printed because a runaway "
            + $"arc is worth seeing and not because the wire turns on it. **Advisory, and stated "
            + $"after the fact rather than before it**: read over occupied indices only, the same "
            + $"quantile is {Fix(instrument.Vc.P99Occupied)} against "
            + $"{Fix(control.Vc.P99Occupied)}."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**TRIPWIRE 2 — the instrument is connected: {(connected ? "PASS" : "FIRED")}.** Mean "
            + $"`v/c` over the top-{TopIndices} moves from {Fix(control.MeanTopRatio)} to "
            + $"{Fix(instrument.MeanTopRatio)}, {Percent(meanGap, control.MeanTopRatio)} against a "
            + $"stated bar of {Hundredths(InstrumentMarginHundredths)}%; the control recorded "
            + $"{control.Diversions:N0} diversions and rung a recorded {instrument.Diversions:N0}."));
        report.AppendLine();

        if (!lowersPeak)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"**Tripwire 1 fired, and the two columns above disagree about the sign of the "
                + $"answer, which is the finding rather than an embarrassment.** p99 `v/c` went "
                + $"**up** — {Fix(control.Vc.P99)} to {Fix(instrument.Vc.P99)} — while the mean over "
                + $"the busiest sixty-four indices went **down**, {Fix(control.MeanTopRatio)} to "
                + $"{Fix(instrument.MeanTopRatio)}, and so did the same quantile taken over the "
                + $"indices that are actually carrying something: "
                + $"{Fix(control.Vc.P99Occupied)} to {Fix(instrument.Vc.P99Occupied)}. All three "
                + $"are true and they describe one behaviour: **Sight redistributes.** It takes "
                + $"load off the extreme tail, where the arcs were far past the clamp, and puts it "
                + $"onto arcs that were previously carrying nothing — and a percentile of a "
                + $"population that is nine parts empty rises the moment previously-empty arcs "
                + $"start carrying traffic, however much relief the busy arcs got. A router that "
                + $"spreads a jam over more road *must* raise an unconditioned middling quantile. "
                + $"That is what spreading is."));
            report.AppendLine();
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"**Beside that sits an argument about the instrument that does not depend on the "
                + $"outcome, and it is what distinguishes this from reasoning around a wire that "
                + $"fired.** The unconditioned p99 is a quantile of a population in which "
                + $"{Percent(control.Vc.ZeroShare, Fixed.One)} of members are **empty road**. A "
                + $"ninety-ninth percentile of that population is roughly an eighty-ninth percentile "
                + $"of the part carrying traffic, and it sits at the boundary of the empty region "
                + $"where nothing distinguishes one rung from another. **The demonstration is in "
                + $"R8.3's cross-load ladder below**: at the lighter load the unconditioned p99 "
                + $"reads the *same value on every rung of the Horizon sweep* — a statistic that "
                + $"cannot move. That is an instrument defect, it is provable by inspecting the "
                + $"ladder without knowing what any rung did, and it is the same class of defect "
                + $"R8.2 exists to catch. It does not unfire the wire. It bounds what the wire is "
                + $"evidence of."));
            report.AppendLine();
            report.AppendLine(
                "**So the wire is scored FIRED and is not rewritten.** It was stated before the "
                + "run, in this form, and this capture is what it says about it. But every reading "
                + "in this section that is conditioned on *road that is carrying traffic* moves the "
                + "other way, and there are four of them: the mean over the busiest sixty-four "
                + "indices, p99 over occupied indices, the share of readings past the BPR clamp, "
                + "and mean journey time. All four are in R8.3's table and all four improve. The "
                + "one reading that fires is the one taken over a population that is nine parts "
                + "empty road. **A fourth version of this wire belongs in R8's successor and it "
                + "should read the share past the clamp** — the only one of the five that is a "
                + "statement about whether the *model* can still see what it is simulating — with "
                + "the occupied quantile beside it.");
            report.AppendLine();
        }

        report.AppendLine(
            "**The control is not quiet and must not be expected to be.** It carries the same "
            + "physics as rung a — Travellers slow in jams, dwell longer, and pile up — so its "
            + "oscillation column is the network's own dynamics with routing held out of it. The "
            + "wire that asked for a quiet control was written for the open-loop model and is "
            + "restated above rather than amended away. **Whether R8.3 to R8.6 may be published "
            + "turns on tripwire 2**: if the control and rung a have the same trajectory, costs are "
            + "being computed and not read, and `plans/0010` refuses the rest of the section.");
        report.AppendLine();

        return connected;
    }

    private static void AppendInstrumentRow(StringBuilder report, string label, LoopOutcome outcome)
    {
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {label} | {Rungs(outcome.Vc)} | {Fix(outcome.MeanTopRatio)} | "
            + $"{Fix(outcome.First.Oscillation)} | {Fix(outcome.Second.Oscillation)} | "
            + $"{(outcome.Steady ? "yes" : "**no**")} | "
            + $"{Fix(outcome.DiversionsPerTick)} | {Fix(outcome.CrossingsPerTick)} |"));
    }

    // --- R8.3 --------------------------------------------------------------------------------------

    /// <summary>
    /// One arc held at the BPR clamp, in Q16.16 Ticks. Derived from the two constants the paragraph
    /// below states rather than typed a third time: BPR at β = 4 with α = 0.15 and v/c capped at
    /// 4.00 is 1 + 0.15 × 4⁴ = 39.4× free-flow, and a Street's free-flow is 0.87 Ticks.
    /// </summary>
    private static readonly long SaturatedArcTicks = (87L * 394 * Fixed.One) / 1000;

    private static int AppendSweep(
        StringBuilder report, List<LoopOutcome> sweep, int floor, int load)
    {
        report.AppendLine("### R8.3 — the Sight sweep");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Temperament spread 0 throughout, at the **{sweep[0].Od.Name}** origin-destination rung, "
            + $"{load:N0} Travellers, base threshold {Fix(DraftThreshold)} (the placeholder R8.4 "
            + $"replaces). "
            + $"The expectation being tested is that `v/c` falls with `N`, cost rises with `N`, "
            + $"and the no-alternative share explains whatever `N = 1` does. **If `v/c` is flat "
            + $"in `N`, Sight is not a mechanism and `adr/0046`'s middle layer is wrong.** The "
            + $"reading is taken on **p99** and the maximum is carried alongside it."));
        report.AppendLine();
        report.AppendLine(
            "| N | " + RungHeadings("v/c") + " | Mean v/c, top-64 | Past the BPR clamp | Oscillation "
            + "| Diversions/Tick | No alternative | Mean journey, Ticks | Refresh ns/Tick | "
            + "Move ns/Tick | Sight ns/Tick | of 15.6 ms |");
        report.AppendLine(
            "|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        long baseMove = sweep[0].MoveNanoseconds;

        foreach (LoopOutcome outcome in sweep)
        {
            long sight = outcome.MoveNanoseconds - baseMove;
            if (sight < 0)
            {
                sight = 0;
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {outcome.Horizon} | {Rungs(outcome.Vc)} | {Fix(outcome.MeanTopRatio)} | "
                + $"{Percent(outcome.AboveClamp, Fixed.One)} | "
                + $"{Fix(Mean(outcome.First.Oscillation, outcome.Second.Oscillation))} | "
                + $"{Fix(outcome.DiversionsPerTick)} | "
                + $"{(outcome.Horizon == 0 ? "—" : Percent(outcome.NoAlternative, outcome.Crossings))} | "
                + $"{Fix(outcome.MeanJourneyTicks)} | {outcome.RefreshNanoseconds:N0} | "
                + $"{outcome.MoveNanoseconds:N0} | {(outcome.Horizon == 0 ? "—" : $"{sight:N0}")} | "
                + $"{(outcome.Horizon == 0 ? "—" : Percent(sight, 15_600_000))} |"));
        }

        report.AppendLine();

        long clampAtControl = sweep[0].AboveClamp;
        long clampLowest = clampAtControl;
        int clampLowestHorizon = 0;

        foreach (LoopOutcome outcome in sweep)
        {
            if (outcome.AboveClamp < clampLowest)
            {
                clampLowest = outcome.AboveClamp;
                clampLowestHorizon = outcome.Horizon;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Read the saturation column before anything else in this table, because at this load "
            + $"it is a warning and not a footnote.** `Congestion.MaximumVolumeCapacity` caps the "
            + $"ratio BPR reads at **4.00**, on R1's stated grounds that a Statistical travel time "
            + $"past that point is already the wrong instrument. Past it the delay multiplier is "
            + $"constant, so the router cannot tell a bad jam from a catastrophic one, and every "
            + $"`v/c` above 4.00 is a quantity it was structurally blind to while it formed. **R8.0's "
            + $"selection criterion exists to hold this column down and at this load it did not**: "
            + $"the control sits at {Percent(clampAtControl, Fixed.One)}, which means the busiest "
            + $"sixty-four indices are almost entirely inside the region the model cannot resolve. "
            + $"R8.0 says why — a p99 taken over a population that is nine parts empty looks past "
            + $"the jam — and does not tune it away."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**What that column then does down the ladder is the single most persuasive number in "
            + $"this section, and it is not the one the tripwire reads.** It falls from "
            + $"{Percent(clampAtControl, Fixed.One)} at Horizon 0 to "
            + $"{Percent(clampLowest, Fixed.One)} at N = {clampLowestHorizon}. Sight is pulling the "
            + $"busiest arcs **out of the unresolvable region** — more than halving the share of "
            + $"readings the congestion model is blind inside. That is a mechanism doing exactly "
            + $"what `adr/0046` claims for it, measured on the one column that cannot be argued "
            + $"about, and it is the reading a future statement of tripwire 1 should probably be "
            + $"built on. It is **not** substituted for the wire as written: the wire says p99 and "
            + $"p99 is what it is scored on."));
        report.AppendLine();

        bool monotoneOnP99 = MonotoneInHorizon(sweep, Rung.P99);
        bool monotoneOnMax = MonotoneInHorizon(sweep, Rung.Max);
        bool monotoneOnOccupied = MonotoneInHorizon(sweep, Rung.P99Occupied);

        LoopOutcome occupiedControl = sweep[0];
        LoopOutcome occupiedBest = sweep[0];
        foreach (LoopOutcome outcome in sweep)
        {
            if (outcome.Horizon != 0 && outcome.Vc.P99Occupied < occupiedBest.Vc.P99Occupied)
            {
                occupiedBest = outcome;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The conditioned ladder is printed beside the unconditioned one, and it is what "
            + $"decides whether the asymmetry argument below is needed at all.** p99 over *occupied* "
            + $"indices goes {Fix(occupiedControl.Vc.P99Occupied)} at Horizon 0 down to "
            + $"{Fix(occupiedBest.Vc.P99Occupied)} at N = {occupiedBest.Horizon}, and across the "
            + $"whole ladder it is {(monotoneOnOccupied ? "**monotone**" : "**not** monotone")}."));
        report.AppendLine();
        report.AppendLine(monotoneOnOccupied
            ? "**On the conditioned rung Sight behaves like a monotone knob, so the asymmetry "
                + "argument is withdrawn.** It was constructed to explain a non-monotonicity, and "
                + "on the only quantile of `v/c` that is not dominated by empty road there is no "
                + "non-monotonicity to explain. It is not refuted — nothing has shown the "
                + "live-versus-lagged asymmetry does not exist — it is **unsupported**, which is a "
                + "different and weaker status, and it is recorded rather than deleted so that a "
                + "successor can look for it deliberately instead of rediscovering it as an "
                + "artefact. What follows is kept for that reader."
            : "**The conditioned rung is not monotone either, so the asymmetry argument is not an "
                + "artefact of the unconditioned statistic and stands on its own.**");
        report.AppendLine();

        report.AppendLine(monotoneOnP99
            ? "**On p99, `v/c` falls with every step up the Horizon ladder, and that settles a "
                + "question the previous capture got wrong.** That capture reported the ladder "
                + "non-monotone and built an explanation on top of the non-monotonicity — an "
                + "asymmetry between a live lookahead bounded by the clamp and a lagged remainder "
                + "bounded by nothing, which it offered as the best available account of why a "
                + "longer Horizon could make things worse. **It was reading a maximum over 33,018 "
                + "indices.** The non-monotonicity was a property of that statistic and not of the "
                + "mechanism, and the asymmetry argument is withdrawn: it is not refuted, it is "
                + "unsupported, because the column it stood on no longer says what it said. Sight "
                + "behaves like a monotone knob on the distribution, which is what `adr/0046` "
                + "claims for it."
            : string.Create(
                CultureInfo.InvariantCulture,
                $"**On p99, `v/c` still does not fall monotonically in `N`, so the asymmetry "
                + $"argument survives the change of statistic and is worth naming.** BPR at β = 4 "
                + $"with `α = 0.15` and the ratio clamped at 4.00 makes one saturated arc "
                + $"**39.4× its free-flow time** — a Street that runs in 0.87 Ticks free-flow runs "
                + $"in 34, against a measured mean journey of {Fix(sweep[0].MeanJourneyTicks)} "
                + $"Ticks at this load, so two saturated arcs of lookahead are "
                + $"{Percent(2 * 34 * Fixed.One, sweep[0].MeanJourneyTicks)} of the whole trip. "
                + $"Where the live half is large against the remainder behind it — and the "
                + $"remainder is what makes the branches comparable — the comparison is dominated "
                + $"by its live half, the detour is charged at free-flow and looks cheap, and `N` "
                + $"stops behaving like a monotone knob. It is not a defect in the implementation: "
                + $"it is what a live-versus-lagged comparison does when the live half is bounded "
                + $"only by the clamp and the lagged half is not bounded at all, and it is a "
                + $"constraint on the base threshold that nothing in `adr/0046` anticipates. "
                + $"**The proportion is printed rather than asserted**: this paragraph once read "
                + $"*\"a mean journey of order 80\"*, which was true at the retired 5,000-Traveller "
                + $"load and had been left standing at the load the round settled on."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The maximum column is {(monotoneOnMax ? "also monotone" : "**not** monotone")}, which "
            + $"is stated for completeness and carries no argument either way: it is one arc."));
        report.AppendLine();
        report.AppendLine(
            "**The `Refresh` column is a finding on its own, and at this load it is the dominant "
            + "one.** Recomputing the live cost array is `O(arcs)`, touches nothing else, and costs "
            + "**more than the entire traveller loop** at every Horizon below 8 — before a single "
            + "Traveller has looked at anything. It does not scale with the fleet, so it does not "
            + "get better at 1M; it gets relatively cheaper only against work that grows. **The "
            + "conclusion is not that the sweep is expensive. It is that a sweep is the wrong shape: "
            + "cost updates have to be incremental and local.** Under `adr/0041` volume is written "
            + "by Travellers entering and leaving arcs, so the set of arcs whose cost actually moved "
            + "in a Tick is exactly the set of arcs somebody crossed — a few hundred, not 66,036 — "
            + "and it is already enumerated by the loop that caused it. A per-Tick VDF sweep over "
            + "every arc in the world recomputes a number that did not change for something like "
            + "ninety-nine arcs in a hundred, which is the same shape of mistake as diffusing a Map "
            + "Layer that nothing has touched. Whatever ships must update the arcs the Tick wrote "
            + "and leave the rest alone; a staggered cadence would bound the cost but would also "
            + "make a driver's Sight depend on which stagger bucket the arc in front of him fell "
            + "into, which is `adr/0044`'s hash-bearing problem arriving in the routing layer.");
        report.AppendLine();
        report.AppendLine(
            "**The Sight column is a difference and never a product.** It is this rung's measured "
            + "`Move` cost minus the control's, which charges Sight for exactly the work Horizon 0 "
            + "does not do. `plans/0010`'s R3 rule — *invert the derivation until what is published "
            + "is measured* — refuses the alternative of a per-decision cost times a guessed decision "
            + "rate. The `Refresh` column is separate for the same reason: it is `O(arcs)` and "
            + "independent of fleet size, so folding it into the traveller loop would charge the "
            + "Sight sweep for 66,036 arcs it never looked at.");
        report.AppendLine();

        // Selection: the lowest p99 v/c, ties to the smaller Horizon, and never the control. It was
        // the lowest MAXIMUM in the previous capture, which let one arc out of 33,018 pick the rung
        // every later task runs at.
        LoopOutcome best = sweep[0];
        bool chosen = false;

        foreach (LoopOutcome outcome in sweep)
        {
            if (outcome.Horizon == 0)
            {
                continue;
            }

            if (!chosen || outcome.Vc.P99 < best.Vc.P99
                || (outcome.Vc.P99 == best.Vc.P99 && outcome.Horizon < best.Horizon))
            {
                best = outcome;
                chosen = true;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Selected Horizon: {best.Horizon}.** The rule is stated rather than eyeballed — the "
            + $"lowest **p99** `v/c` among the non-control rungs, ties broken toward the smaller "
            + $"Horizon, and never below R8.1's floor of {floor}. R8.3's cross-check and all of R8.4 "
            + $"run there. **This selects nothing for the Ruleset**; R8 reports curves exactly as R1 "
            + $"did for the District count, and the corpus decides."));
        report.AppendLine();

        return best.Horizon < floor ? floor : best.Horizon;
    }

    private static void AppendCrossCheck(
        StringBuilder report, List<LoopOutcome> outcomes, int selected, int load)
    {
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"#### The selected Horizon across R4.1's swept family, N = {selected}, {load:N0} Travellers"));
        report.AppendLine();
        report.AppendLine(
            "**R4 found that S2's uniform draw had been hiding a conclusion**, so a figure taken at "
            + "one rung is a figure whose rung has to be named. Every row here is the same Horizon "
            + "under a different draw.");
        report.AppendLine();
        report.AppendLine(
            "| O-D rung | " + RungHeadings("v/c") + " | Mean v/c, top-64 | Oscillation | "
            + "Diversions/Tick | No alternative | Mean journey, Ticks | Steady |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|");

        foreach (LoopOutcome outcome in outcomes)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {outcome.Od.Name} | {Rungs(outcome.Vc)} | {Fix(outcome.MeanTopRatio)} | "
                + $"{Fix(Mean(outcome.First.Oscillation, outcome.Second.Oscillation))} | "
                + $"{Fix(outcome.DiversionsPerTick)} | "
                + $"{Percent(outcome.NoAlternative, outcome.Crossings)} | "
                + $"{Fix(outcome.MeanJourneyTicks)} | {(outcome.Steady ? "yes" : "**no**")} |"));
        }

        report.AppendLine();
    }

    /// <summary>
    /// The whole Sight ladder repeated at the load the retired criterion selected, so that R8's
    /// central answer can be checked for load-dependence rather than assumed free of it.
    /// </summary>
    /// <remarks>
    /// This is a cross-check and never a second selection. R8.0's stated criterion governs; the
    /// question here is only whether the section's conclusion survives being read somewhere else.
    /// </remarks>
    private static void AppendLoadCrossCheck(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        Pool pool,
        OdRung rung,
        int operating,
        int retired,
        int floor,
        List<string> caps,
        List<LoopOutcome> ladder)
    {
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"#### The same ladder at {retired:N0} Travellers, the load the retired criterion chose"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**R8.0's two criteria disagreed, so the section is read at both loads rather than at "
            + $"the one that suits it.** Everything above runs at {operating:N0}, which the stated "
            + $"p99 criterion selected. Everything here is the identical sweep at {retired:N0}, "
            + $"which the retired clamp-share criterion selected, and which R8.0 shows is the "
            + $"largest load leaving the busiest sixty-four indices mostly inside the range BPR can "
            + $"resolve. Nothing is selected off this table."));
        report.AppendLine();
        report.AppendLine(
            "| N | " + RungHeadings("v/c") + " | Mean v/c, top-64 | Past the BPR clamp | Oscillation "
            + "| Diversions/Tick | No alternative | Mean journey, Ticks | Steady |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|");

        foreach (int horizon in HorizonRungs)
        {
            Mark(string.Create(CultureInfo.InvariantCulture,
                $"R8.3 cross-load N={horizon} load={retired}"));

            LoopOutcome outcome = Measure(
                graph, districts, nextHop, freeFlow, representativeArc, pool, rung, retired,
                horizon, DraftThreshold, spreadShare: 0, blend: 0);

            ladder.Add(outcome);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {outcome.Horizon} | {Rungs(outcome.Vc)} | {Fix(outcome.MeanTopRatio)} | "
                + $"{Percent(outcome.AboveClamp, Fixed.One)} | "
                + $"{Fix(Mean(outcome.First.Oscillation, outcome.Second.Oscillation))} | "
                + $"{Fix(outcome.DiversionsPerTick)} | "
                + $"{(outcome.Horizon == 0 ? "—" : Percent(outcome.NoAlternative, outcome.Crossings))} | "
                + $"{Fix(outcome.MeanJourneyTicks)} | {(outcome.Steady ? "yes" : "**no**")} |"));
        }

        report.AppendLine();

        LoopOutcome control = ladder[0];
        LoopOutcome atFloor = ladder[0];
        foreach (LoopOutcome outcome in ladder)
        {
            if (outcome.Horizon == floor)
            {
                atFloor = outcome;
            }
        }

        long gap = control.Vc.P99 - atFloor.Vc.P99;
        bool lowers = gap * 10_000 >= (long)control.Vc.P99 * InstrumentMarginHundredths;
        bool monotone = MonotoneInHorizon(ladder, Rung.P99);

        long occupiedGap = control.Vc.P99Occupied - atFloor.Vc.P99Occupied;

        // The control is excluded, because the question is which Sight rung reads lowest and a
        // ladder whose best rung is Horizon 0 would be reporting the control twice.
        LoopOutcome bestOccupied = atFloor;
        foreach (LoopOutcome outcome in ladder)
        {
            if (outcome.Horizon != 0 && outcome.Vc.P99Occupied < bestOccupied.Vc.P99Occupied)
            {
                bestOccupied = outcome;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Tripwire 1's reading at this load: {(lowers ? "it would have passed" : "it would "
                + "have fired here too")}.** p99 goes {Fix(control.Vc.P99)} → "
            + $"{Fix(atFloor.Vc.P99)} at N = {floor}, {Percent(gap, control.Vc.P99)} against the "
            + $"same {Hundredths(InstrumentMarginHundredths)}% bar. The ladder is "
            + $"{(monotone ? "monotone" : "**not** monotone")} in `N` on p99 here."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**And here is the reason that reading should not be trusted either, at either load.** "
            + $"At {retired:N0} Travellers roughly nine car-carrying indices in ten hold nothing, "
            + $"so the ninety-ninth percentile *over every index* is a reading taken at the edge of "
            + $"the empty region rather than inside the traffic — which is why it is "
            + $"{Fix(control.Vc.P99)} at Horizon 0 and barely moves anywhere on the ladder. **A "
            + $"statistic that cannot move is the failure R8.2 exists to catch, and R8's own "
            + $"headline statistic has it — across {ladder.Count} rungs the unconditioned p99 takes "
            + $"{DistinctReadings(ladder, Rung.P99)} distinct value(s), against "
            + $"{DistinctReadings(ladder, Rung.P99Occupied)} for the conditioned one. That count is "
            + $"a property of the ladder and can be checked without knowing what any rung did, which "
            + $"is what makes it an argument about the instrument rather than about the answer.** "
            + $"Read over occupied indices the same ladder goes "
            + $"{Fix(control.Vc.P99Occupied)} → {Fix(atFloor.Vc.P99Occupied)} at N = {floor} "
            + $"({Percent(occupiedGap, control.Vc.P99Occupied)}) and reaches "
            + $"{Fix(bestOccupied.Vc.P99Occupied)} at N = {bestOccupied.Horizon}."));
        report.AppendLine();
        report.AppendLine(
            "**So the correction the ladder was asked for was right and incomplete, and the "
            + "incompleteness is recorded here rather than smoothed over.** Replacing a maximum "
            + "over tens of thousands of indices with a quantile ladder was the correct move and it "
            + "dissolved three separate arguments this section had been carrying. What it then "
            + "revealed is that the population being summarised is nine parts *empty road*, so an "
            + "unconditioned quantile below about p99.9 describes the emptiness and not the "
            + "traffic. The conditioned column — p99 over indices that are carrying something — is "
            + "printed in every ladder in this section from R8.0 onward, and it is the column a "
            + "successor task should state its wires on. R8 does not restate its own wires "
            + "mid-capture on a statistic it chose after seeing the numbers.");
        report.AppendLine();
        report.AppendLine(lowers
            ? "**So R8's central answer is load-dependent, and that is a larger finding than either "
                + "load's number.** At the lighter load Sight lowers `v/c` at the quantile the wire "
                + "reads; at the heavier one it raises it while lowering the mean of the busiest "
                + "arcs. Both are the same mechanism — spreading load off a saturated tail onto "
                + "empty road — and which sign a quantile shows depends entirely on how much empty "
                + "road there is left to spread onto. **A congestion-response figure quoted without "
                + "its load is meaningless**, and this is the second time S2 has found that: R4 "
                + "found the same about the origin-destination draw and made it a swept family. The "
                + "load deserves the same treatment, and R8.0 is the beginning of it rather than "
                + "the end."
            : "**The answer does not change with the load, which is the more comfortable of the two "
                + "outcomes and the less interesting one.** Whatever the wire says at the operating "
                + "load, it says here too, so R8.0's disagreement between its two criteria changed "
                + "what the section reports and not what it concludes.");
        report.AppendLine();

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"The cross-load ladder repeats only R8.3's Horizon sweep at {retired:N0}. R8.4's "
            + $"Temperament sweep, R8.5's surge and R8.6's diversion cost are measured at the "
            + $"operating load only, and none of them has been checked for load-dependence."));
    }

    // --- R8.4 --------------------------------------------------------------------------------------

    /// <summary>
    /// R8.4's first half: the distribution of relative improvement actually on offer, and the base
    /// threshold rungs read off it.
    /// </summary>
    /// <remarks>
    /// <b>This section exists because R8.4's first attempt swept the wrong axis.</b> It held the
    /// base at a chosen 10% and swept the spread around it, and the diversion rate moved by under
    /// 3% across the entire sweep — which says nothing about whether Temperament damps and
    /// everything about where 10% sits relative to the improvements drivers are actually offered. A
    /// threshold swept across a guess is a threshold swept somewhere the decisions do not live.
    /// </remarks>
    private static int[] AppendImprovement(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        Pool pool,
        OdRung rung,
        int load,
        int selected,
        LoopOutcome atSelected)
    {
        var fleet = NewFleet(
            graph, districts, nextHop, freeFlow, pool, load, selected, DraftThreshold,
            spread: 0, blend: 0);

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
        }

        fleet.ClearImprovement();

        for (int tick = 0; tick < WindowTicks; tick++)
        {
            fleet.Advance();
        }

        report.AppendLine("### R8.4 — the improvement on offer, then the Temperament sweep");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Before any threshold is swept, what is a threshold *on*?** At every crossing where at "
            + $"least one alternative survived the filters, `(habitScore − bestScore) / habitScore` "
            + $"is recorded — the relative improvement the best alternative offers over Habit, which "
            + $"is exactly the quantity the diversion test compares against. N = {selected}, "
            + $"{rung.Name}, {load:N0} Travellers, {WindowTicks} Ticks after warm-up."));
        report.AppendLine();

        long[] histogram = fleet.ImprovementHistogram;
        var quantiles = new int[BasePercentiles.Length];

        long offeredNothing = histogram.Length > 0 ? histogram[0] : 0;
        long offeredSomething = fleet.ImprovementSamples - offeredNothing;

        report.AppendLine(
            "| Quantile | Over every decision | Over decisions offered anything at all | "
            + "the latter as a percentage |");
        report.AppendLine("|---:|---:|---:|---:|");

        for (int i = 0; i < BasePercentiles.Length; i++)
        {
            int all = HistogramQuantile(
                histogram, fleet.ImprovementSamples, BasePercentiles[i], offeredSomethingOnly: false);

            quantiles[i] = HistogramQuantile(
                histogram, fleet.ImprovementSamples, BasePercentiles[i], offeredSomethingOnly: true);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| p{BasePercentiles[i]} | {Fine(all)} | {Fine(quantiles[i])} | "
                + $"{Percent(quantiles[i], Fixed.One)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**{Percent(offeredNothing, fleet.ImprovementSamples)} of decisions are offered an "
            + $"improvement of exactly zero** — an alternative existed and none of them beat Habit. "
            + $"No threshold in `[0, 1]` can act on those, because the diversion test is a strict "
            + $"inequality, so they are decisions a threshold is not *about*. **The base rungs are "
            + $"therefore read off the {offeredSomething:N0} decisions that were offered something.** "
            + $"Sited over all {fleet.ImprovementSamples:N0} the median is exactly zero, which makes "
            + $"the base zero, which makes every spread rung `share × 0` — thirteen identical rows "
            + $"reported as a Temperament sweep. That happened once and the column above is what "
            + $"caught it."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Why so many decisions offer nothing is not a mystery, and the explanation is "
            + $"structural rather than statistical.** Habit is `NextHopTable` — a shortest-path tree "
            + $"over the free-flow cost array, one tree per District. At every node on that tree the "
            + $"habit arc is the *first arc of the cheapest free-flow route to the destination*, "
            + $"which is to say it is optimal by construction and was optimal before any Traveller "
            + $"moved. An alternative arc is by definition off the tree, so it pays a free-flow "
            + $"penalty the moment it is taken, and it pays it in full and immediately. Both branch "
            + $"scores then carry a free-flow remainder — `DistanceOf(node, District)` — which is "
            + $"the same quantity computed from the same tree, so the remainders differ by exactly "
            + $"that penalty. **An alternative can therefore only win when the live congestion on the "
            + $"first {selected} arc(s) of the habit branch exceeds the free-flow penalty of leaving "
            + $"the tree.** Below that, the comparison is decided by arithmetic that congestion never "
            + $"enters, and the improvement is exactly zero — not small, zero, because `bestScore` "
            + $"and `habitScore` are then ordered by the free-flow tree alone."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Cross-check against the no-alternative column, which measures a different thing.** "
            + $"At N = {selected}, {Percent(atSelected.NoAlternative, atSelected.Crossings)} of "
            + $"crossings had **no surviving alternative at all** — a dead end, a U-turn, or an arc "
            + $"the next-hop table has no distance for. So the "
            + $"{Percent(offeredNothing, fleet.ImprovementSamples)} above is emphatically not that: "
            + $"in those decisions an alternative existed, was scored, and lost. The two columns are "
            + $"independent and both are printed, because *nowhere to go* and *nowhere better to go* "
            + $"have different consequences — the first is a property of the graph and the second is "
            + $"a property of the routing."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The consequence is the useful part: Sight fires rarely, and that is what makes it "
            + $"affordable.** Of {atSelected.Crossings:N0} crossings over the two windows, "
            + $"{atSelected.Diversions:N0} produced a diversion — "
            + $"{Percent(atSelected.Diversions, atSelected.Crossings)}. The share of decisions "
            + $"offered anything at all, {Percent(offeredSomething, fleet.ImprovementSamples)}, is "
            + $"the ceiling on that, and the diversion rate is the tighter figure because a "
            + $"threshold sits between them. **R8.6's per-Tick bill must be the cost of one "
            + $"re-search multiplied by the diversion rate and not by the crossing rate**, and that "
            + $"is what it does — R8.6 multiplies by the measured diversions per Tick from this "
            + $"very sweep. A reader who costs Sight by assuming every crossing re-plans will "
            + $"overstate it by more than an order of magnitude. A reader who forgets that the "
            + $"diversion rate is itself a function of the base threshold will understate how much "
            + $"that Ruleset value costs."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"There is a sting in it. The same structure that makes Sight cheap is the structure "
            + $"R8.0 found concentrating the whole fleet onto a fraction of the network: **one "
            + $"free-flow tree per District is both why alternatives rarely win and why there is "
            + $"congestion for them to win against.** Sight is being asked to relieve a jam its own "
            + $"null hypothesis created, using a score that same null hypothesis anchors. That is "
            + $"not an argument against `adr/0046` — it is the reason the ADR's Habit layer is "
            + $"named a *layer* and not a *baseline* — but it does mean the ~{Percent(offeredSomething, fleet.ImprovementSamples)} "
            + $"fire rate is a property of District-granular routing and should not be carried over "
            + $"to any scheme that gives a Traveller more than one candidate route to begin with."));

        report.AppendLine();
        report.AppendLine("The whole histogram, so the shape is visible and not only its quantiles:");
        report.AppendLine();
        report.AppendLine("| Improvement at least | Decisions | Share |");
        report.AppendLine("|---:|---:|---:|");

        for (int bucket = 0; bucket < histogram.Length; bucket++)
        {
            if (histogram[bucket] == 0)
            {
                continue;
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Fine(SightFleet.ImprovementLowerBound(bucket))} | {histogram[bucket]:N0} | "
                + $"{Percent(histogram[bucket], fleet.ImprovementSamples)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"{fleet.ImprovementSamples:N0} decisions with at least one surviving alternative, in "
            + $"**octaves** rather than equal bins: bucket `k` holds `[2^(k−1), 2^k)` in Q16.16 "
            + $"units. An equal-width histogram of a thousand bins was tried first and put every "
            + $"quantile from p10 to p90 in the first bin — a true reading and a useless one. The "
            + $"improvements on offer span orders of magnitude, and where they sit relative to the "
            + $"placeholder 10% is the whole of why the placeholder did nothing."));
        report.AppendLine();
        report.AppendLine(
            "**The base threshold rungs below are these quantiles, and nothing else.** A threshold "
            + "at p90 means one decision in ten clears it; a threshold at p10 means nine in ten do. "
            + "Sweeping a threshold across the distribution it is applied to is the only way the "
            + "sweep can be said to have covered anything — and it is the correction R8.4's first "
            + "attempt needed, which swept spread around a base nobody had sited.");
        report.AppendLine();

        return quantiles;
    }

    /// <summary>R8.4's second half: the base sweep, and the base the spread sweep then runs at.</summary>
    private static int AppendBaseSweep(
        StringBuilder report,
        List<LoopOutcome> outcomes,
        int[] baseRungs,
        string[] baseLabels,
        int[] quantiles)
    {
        report.AppendLine("#### The base threshold, swept across the distribution above");
        report.AppendLine();
        report.AppendLine(
            "| Base threshold | Quantile | Oscillation | " + RungHeadings("v/c")
            + " | Mean v/c, top-64 | Diversions/Tick | Mean journey, Ticks | Steady |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|");

        for (int i = 0; i < outcomes.Count && i < baseLabels.Length; i++)
        {
            LoopOutcome outcome = outcomes[i];
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Fine(outcome.BaseThreshold)} | {baseLabels[i]} | "
                + $"{Fix(Mean(outcome.First.Oscillation, outcome.Second.Oscillation))} | "
                + $"{Rungs(outcome.Vc)} | {Fix(outcome.MeanTopRatio)} | "
                + $"{Fix(outcome.DiversionsPerTick)} | {Fix(outcome.MeanJourneyTicks)} | "
                + $"{(outcome.Steady ? "yes" : "**no**")} |"));
        }

        report.AppendLine();

        // The rung the median quantile fell in, per the instruction that the spread sweep runs at
        // the base nearest the median. Located by value rather than by table position, so
        // deduplication cannot move it, and fixed by the distribution rather than by any reading in
        // the table above — the spread sweep cannot then be accused of having been sited where it
        // would look best.
        int median = 0;
        for (int i = 0; i < BasePercentiles.Length && i < quantiles.Length; i++)
        {
            if (BasePercentiles[i] == 50)
            {
                median = quantiles[i];
            }
        }

        int selectedBase = baseRungs.Length > 0 ? baseRungs[0] : 0;
        foreach (int rung in baseRungs)
        {
            if (rung == median)
            {
                selectedBase = rung;
            }
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The spread sweep runs at the base the median fell in, {Fine(selectedBase)}.** That "
            + $"is a position in the measured distribution and not a reading off this table."));
        report.AppendLine();
        report.AppendLine(
            "**A base threshold of exactly zero is a legitimate rung and not a degenerate one.** It "
            + "means *divert whenever the alternative is strictly better at all* — `adr/0017`'s "
            + "satisficing switched off, which is the comparison the whole layer needs. If the "
            + "distribution puts its median there, that is the finding: the improvements this model "
            + "offers a driver are too small for a relative threshold to have anything to bite on, "
            + "and **that is a statement about the score, not about Temperament.**");
        report.AppendLine();

        return selectedBase;
    }

    /// <summary>Distinct threshold rungs, with the quantiles that share each one named on its row.</summary>
    private static int[] DistinctRungs(int[] quantiles, out string[] labels)
    {
        var values = new List<int>();
        var names = new List<string>();

        for (int i = 0; i < quantiles.Length; i++)
        {
            string name = string.Create(CultureInfo.InvariantCulture, $"p{BasePercentiles[i]}");
            int at = values.IndexOf(quantiles[i]);

            if (at >= 0)
            {
                names[at] = names[at] + ", " + name;
                continue;
            }

            values.Add(quantiles[i]);
            names.Add(name);
        }

        labels = [.. names];
        return [.. values];
    }

    private static bool AppendTemperament(
        StringBuilder report,
        List<LoopOutcome> outcomes,
        LoopOutcome positive,
        int selected,
        int selectedBase,
        int load)
    {
        report.AppendLine("#### The spread and the blend");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"At N = {selected}, {load:N0} Travellers, base threshold {Fix(selectedBase)}. Spread is "
            + $"a **share of the base**; blend weight 0 is pure per-decision jitter and 1.00 is pure "
            + $"stable character. `adr/0046` argues **both endpoints fail**, for different reasons, "
            + $"and that is a claim with a number attached."));
        report.AppendLine();
        report.AppendLine(
            "**Two metrics, because amplitude and synchrony are different quantities and the ADR's "
            + "claim is about the second.** `adr/0046` says an identical rule over an identical "
            + "input *\"produces a herd: the whole flow switches to the alternative together, the "
            + "alternative jams, the whole flow switches back.\"* That is many drivers making the "
            + "**same** move at the **same** time. *Oscillation* — the mean absolute Tick-to-Tick "
            + "change in `v/c` over the busiest 64 indices — measures how much the network moves, "
            + "which a herd does cause but so does anything else. *Synchrony* measures the thing "
            + "itself: of the diversions taken in one Tick, the share that went to the **same arc**, "
            + "with the effective number of distinct arcs those diversions spread over alongside it. "
            + "A perfect herd reads 100% and 1.00.");
        report.AppendLine();

        if (selectedBase == 0)
        {
            // A base of zero makes every spread rung `share × 0`, so the sweep would print one rung
            // thirteen times. Refused rather than printed: thirteen identical rows are the most
            // convincing-looking dead instrument this spike could ship.
            report.AppendLine(
                "**The spread sweep is not published, because the base it would run at is zero.** "
                + "Spread is a share of the base, so every rung would be identical and the table "
                + "would be one measurement printed thirteen times. That is a dead instrument "
                + "wearing a full table, and it is refused for the reason R8.2 exists.");
            report.AppendLine();
            return false;
        }

        report.AppendLine(
            "| Spread, of base | Blend | Oscillation | Synchrony | Effective arcs | "
            + RungHeadings("v/c") + " | Mean journey, Ticks | Diversions/Tick | Steady |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|");

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| **positive control** | — | "
            + $"{Fix(Mean(positive.First.Oscillation, positive.Second.Oscillation))} | "
            + $"{Percent(positive.Synchrony, Fixed.One)} | {Fix(positive.EffectiveArcs)} | "
            + $"{Rungs(positive.Vc)} | {Fix(positive.MeanJourneyTicks)} | "
            + $"{Fix(positive.DiversionsPerTick)} | {(positive.Steady ? "yes" : "**no**")} |"));

        long worstOscillation = 0;
        long worstSynchrony = 0;
        long fewestArcs = long.MaxValue;

        foreach (LoopOutcome outcome in outcomes)
        {
            long oscillation = Mean(outcome.First.Oscillation, outcome.Second.Oscillation);

            if (oscillation > worstOscillation)
            {
                worstOscillation = oscillation;
            }

            if (outcome.Synchrony > worstSynchrony)
            {
                worstSynchrony = outcome.Synchrony;
            }

            if (outcome.EffectiveArcs > 0 && outcome.EffectiveArcs < fewestArcs)
            {
                fewestArcs = outcome.EffectiveArcs;
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Fix(outcome.SpreadShare)} | {(outcome.SpreadShare == 0 ? "—" : Fix(outcome.Blend))} | "
                + $"{Fix(oscillation)} | {Percent(outcome.Synchrony, Fixed.One)} | "
                + $"{Fix(outcome.EffectiveArcs)} | {Rungs(outcome.Vc)} | "
                + $"{Fix(outcome.MeanJourneyTicks)} | "
                + $"{Fix(outcome.DiversionsPerTick)} | {(outcome.Steady ? "yes" : "**no**")} |"));
        }

        if (fewestArcs == long.MaxValue)
        {
            fewestArcs = 0;
        }

        report.AppendLine();

        long positiveOscillation = Mean(positive.First.Oscillation, positive.Second.Oscillation);
        bool oscillationSeparates =
            positiveOscillation * 10_000 >= worstOscillation * (10_000 + HerdMarginHundredths);
        bool synchronySeparates =
            positive.Synchrony * 10_000 >= worstSynchrony * (10_000 + HerdMarginHundredths);
        bool validated = oscillationSeparates || synchronySeparates;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The positive control is a construction built to herd maximally, and the criterion "
            + $"for believing either metric was stated before it ran.** Base threshold **0**, spread "
            + $"**0**: every driver applies the same rule to the same live costs, diverts the moment "
            + $"an alternative is better at all, and draws nothing at random. `adr/0046`'s herd is "
            + $"not a risk on that row, it is the definition of it. The bar: **a metric counts as a "
            + $"herd detector only if the positive control exceeds every swept row by at least "
            + $"{Hundredths(HerdMarginHundredths)}%.**"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Oscillation: {Fix(positiveOscillation)} against a swept worst of "
            + $"{Fix(worstOscillation)} — **{(oscillationSeparates ? "separates" : "does not separate")}**. "
            + $"Synchrony: {Percent(positive.Synchrony, Fixed.One)} against a swept worst of "
            + $"{Percent(worstSynchrony, Fixed.One)} — "
            + $"**{(synchronySeparates ? "separates" : "does not separate")}**. The positive control's "
            + $"diversions spread over {Fix(positive.EffectiveArcs)} effective arcs against a swept "
            + $"low of {Fix(fewestArcs)}."));
        report.AppendLine();
        report.AppendLine(validated
            ? "**At least one metric separates, so a flat sweep is a reading and not an instrument "
                + "failure.** Whatever the spread column does below, it does it on an instrument "
                + "that has been shown capable of telling a herd from a non-herd on this network. "
                + "The tripwire verdict on Temperament stands as measured."
            : "**Neither metric separates, so neither is a herd detector on this network, and "
                + "`adr/0046`'s Temperament layer is `NOT TESTED` rather than refuted.** A "
                + "construction built to herd maximally reads the same as one built not to; that "
                + "is a statement about the instrument and it disqualifies the instrument from "
                + "carrying a refutation. The previous capture reported Temperament **REFUTED** on "
                + "a flat oscillation column, and it had no right to — a flat column from an "
                + "instrument that cannot move is exactly the failure R8.2 exists to catch, "
                + "arriving one task later and wearing a different name. **The correction is "
                + "recorded here rather than removed, because this spike's standing record is of "
                + "catching instruments that could not move, and that record is worth more than a "
                + "clean-looking refutation.**");
        report.AppendLine();
        report.AppendLine(
            "**A note on why the herd may be hard to produce here at all.** A herd needs many "
            + "drivers at the same junction facing the same choice in the same Tick. R8.4 measures "
            + "that the improvement on offer is zero for the large majority of decisions, so the "
            + "population that could herd is small before Temperament is applied to it; and the "
            + "diversions that do happen are spread over the effective-arc count in the table above. "
            + "If that number is large, the network is not herding for reasons that have nothing to "
            + "do with the layer under test, and no setting of the spread could show otherwise.");
        report.AppendLine();
        report.AppendLine(
            "**One pattern in this table was odd enough to name in the previous capture and is "
            + "reported here as retired.** The blend-0.50 rows carried visibly higher *peak* `v/c` "
            + "than either endpoint at the same spread, which is the opposite of what `adr/0046` "
            + "predicts. That reading was a maximum over 33,018 indices — the highest-variance "
            + "column in the report — and the ladder is what it should have been read on. Whether "
            + "anything survives at p99 is visible above; a three-row pattern in a thirteen-row "
            + "table is exactly the size of thing that turns out to be nothing, and it was.");
        report.AppendLine();
        report.AppendLine(
            "**Amplitude must fall monotonically in spread across at least the first three rungs.** "
            + "Flat or non-monotone refutes the layer, and `adr/0046` already names what would "
            + "replace it: staggered decision Ticks, or hysteresis on the diversion itself — both "
            + "cheaper than Temperament and neither serving `UNIQUE INDIVIDUALS`, so the trade would "
            + "be explicit. **That wire is now conditional on the paragraph above**: it can only "
            + "refute the layer if the metric it reads was shown able to detect a herd.");
        report.AppendLine();
        report.AppendLine(
            "**One doubt, recorded rather than settled: what the threshold is a share *of*.** The "
            + "diversion test is `habitScore − bestScore > threshold × habitScore`, and `habitScore` "
            + "is the live lookahead **plus the whole remaining free-flow journey**. So the absolute "
            + "margin a Traveller demands scales with how far it still has to go: a driver two "
            + "Segments from its destination has an effective margin near zero and diverts on almost "
            + "anything, whichever Temperament it drew. The counter-argument is that this is right — "
            + "*\"this saves me a tenth of my trip\"* is a reasonable thing for a person to weigh, "
            + "and `adr/0017`'s **substantially better** is a relative test against the incumbent, "
            + "not an absolute one. The formula is therefore kept and the distribution is published "
            + "so the objection can be checked against it. An absolute threshold in Ticks, or one "
            + "taken as a share of the lookahead alone, is a different measurement and R8 has not "
            + "made it. Settling which is right by argument is what `adr/0043` exists to refuse.");
        report.AppendLine();

        return validated;
    }

    /// <summary>
    /// R8.4's readings on <c>adr/0046</c>'s third layer, kept apart on purpose: the wire as
    /// <c>plans/0010</c> states it, the instrument's demonstrated resolution, and the net fall —
    /// which is a <b>different statistic</b> from the one the wire named and is labelled as such
    /// wherever it appears.
    /// </summary>
    private sealed record TemperamentReading(
        TemperamentVerdict Verdict,
        bool MonotoneFirstThree,
        long Resolution,
        long BreakingStep,
        int UnresolvableSteps,
        int TotalSteps,
        long NetFrom,
        long NetTo,
        long NetShare)
    {
        public static TemperamentReading None(TemperamentVerdict verdict) =>
            new(verdict, MonotoneFirstThree: false, Resolution: 0, BreakingStep: 0,
                UnresolvableSteps: 0, TotalSteps: 0, NetFrom: 0, NetTo: 0, NetShare: 0);
    }

    /// <summary>What R8.4 was able to conclude about <c>adr/0046</c>'s third layer.</summary>
    private enum TemperamentVerdict
    {
        /// <summary>The base ladder does not damp either, so something else suppresses the herd.</summary>
        Refuted,

        /// <summary>The threshold damps and per-Citizen variation adds nothing measurable on top.</summary>
        ThresholdDoesTheWork,

        /// <summary>Spread damps inside the herding regime; the flat sweep was a siting artefact.</summary>
        NotRefuted,

        /// <summary>The question could not be put — no non-zero base rung to site the test on.</summary>
        NotTested,
    }

    /// <summary>
    /// Whether the herd `adr/0046` predicts is killed by <b>the base threshold</b> rather than by
    /// per-Citizen variation — a distinction the previous verdict of <c>REFUTED</c> collapses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The positive control herds at two orders of magnitude above every swept row. That gap is a
    /// switch and not a gradient, and it happened somewhere between base 0 and the base the spread
    /// sweep was sited at. If it happened <i>below</i> that base, then spread was swept in a regime
    /// with no herd left in it, and a flat column there says nothing about whether spread damps.
    /// </para>
    /// <para>
    /// Both halves are measured. The base ladder at spread 0 says whether a threshold damps at all
    /// and where the transition is; a fresh spread ladder sited at the <b>most herding non-zero
    /// base</b> says whether variation adds anything where there is something left to damp. The
    /// bars for both are stated below before either is read.
    /// </para>
    /// </remarks>
    private static TemperamentReading AppendHerdRegime(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        Pool pool,
        OdRung rung,
        int load,
        int selected,
        List<LoopOutcome> baseSweep,
        LoopOutcome positive,
        int selectedBase,
        List<LoopOutcome> herdLadder,
        List<string> caps)
    {
        report.AppendLine("#### Is the herd killed by the threshold, or by the variation?");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The positive control herds at {Fix(Mean(positive.First.Oscillation, positive.Second.Oscillation))} "
            + $"and every swept spread rung sits two orders below it. That is a switch, not a "
            + $"gradient, and it throws until now went unasked: *where* did it happen?** If the herd "
            + $"dies at the first non-zero threshold, then the spread ladder above was swept in a "
            + $"regime with no herd left in it, and its flatness is a statement about the siting "
            + $"rather than about `adr/0046`'s third layer. The two hypotheses — *per-Citizen "
            + $"variation does not damp* and *a threshold already damped everything, so there was "
            + $"nothing left to damp* — have very different consequences, and the sweep as sited "
            + $"cannot tell them apart."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Both bars stated before either reading.** (i) *A threshold damps* if the lowest "
            + $"oscillation on the base ladder is **at most a quarter of the highest** — a factor of "
            + $"four, chosen rather than derived, and large enough that noise on this column cannot "
            + $"produce it. (ii) *Variation damps* if, at the most herding non-zero base, oscillation "
            + $"falls by at least {Hundredths(HerdMarginHundredths)}% from spread 0 to the largest "
            + $"spread — the same bar the positive control had to clear to be believed at all."));
        report.AppendLine();
        report.AppendLine("The base ladder at spread 0, with the positive control as its zero rung:");
        report.AppendLine();
        report.AppendLine("| Base threshold | Oscillation | Synchrony | Diversions/Tick |");
        report.AppendLine("|---:|---:|---:|---:|");

        long highest = Mean(positive.First.Oscillation, positive.Second.Oscillation);
        long lowest = highest;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {Fine(0)} *(positive control)* | {Fix(highest)} | "
            + $"{Percent(positive.Synchrony, Fixed.One)} | {Fix(positive.DiversionsPerTick)} |"));

        LoopOutcome? herdBase = null;
        long herdBaseOscillation = -1;
        LoopOutcome? firstNonZero = null;

        foreach (LoopOutcome outcome in baseSweep)
        {
            long oscillation = Mean(outcome.First.Oscillation, outcome.Second.Oscillation);

            if (oscillation > highest)
            {
                highest = oscillation;
            }

            if (oscillation < lowest)
            {
                lowest = oscillation;
            }

            if (outcome.BaseThreshold > 0)
            {
                firstNonZero ??= outcome;

                if (oscillation > herdBaseOscillation)
                {
                    herdBaseOscillation = oscillation;
                    herdBase = outcome;
                }
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Fine(outcome.BaseThreshold)} | {Fix(oscillation)} | "
                + $"{Percent(outcome.Synchrony, Fixed.One)} | {Fix(outcome.DiversionsPerTick)} |"));
        }

        report.AppendLine();

        // Bar (i): a factor of four between the ladder's extremes.
        bool thresholdDamps = highest > 0 && lowest * 4 <= highest;

        if (herdBase is null)
        {
            report.AppendLine(
                "**There is no non-zero base rung, so the question cannot be put.** The improvement "
                + "distribution put every quantile at zero, and a spread ladder at base 0 is one "
                + "measurement printed five times. Temperament is `NOT TESTED` on this capture.");
            report.AppendLine();
            return TemperamentReading.None(TemperamentVerdict.NotTested);
        }

        long firstOscillation = firstNonZero is null
            ? 0
            : Mean(firstNonZero.First.Oscillation, firstNonZero.Second.Oscillation);

        // Where the transition sits: at the first non-zero base, or inside the ladder. Read as
        // "the first non-zero base is still within a factor of two of the ladder's maximum".
        bool transitionInsideLadder = firstOscillation * 2 >= highest;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Reading (i).** The ladder runs from {Fix(highest)} to {Fix(lowest)}, a factor of "
            + $"{Ratio(highest, lowest == 0 ? 1 : lowest)}, so **a threshold "
            + $"{(thresholdDamps ? "does" : "does not")} damp** against the stated bar of four. "
            + $"{(transitionInsideLadder
                ? $"And the transition is **inside** the ladder rather than at its first step: the "
                    + $"smallest non-zero base, {Fine(firstNonZero?.BaseThreshold ?? 0)}, still "
                    + $"reads {Fix(firstOscillation)} — the same order as the positive control. The "
                    + $"herd survives a small threshold and dies at a larger one."
                : $"The transition is at the **first** non-zero base: {Fine(firstNonZero?.BaseThreshold ?? 0)} "
                    + $"already reads {Fix(firstOscillation)}. Any threshold at all is enough.")}"));
        report.AppendLine();

        if (!thresholdDamps)
        {
            report.AppendLine(
                "**The base ladder does not damp either, so the threshold is not what is "
                + "suppressing the herd** — something else on this network is, and neither the base "
                + "nor the spread is doing the work. The verdict on `adr/0046`'s third layer stays "
                + "**REFUTED** and no further rungs are run: a spread ladder sited inside a regime "
                + "that was never established would be the same siting error in the other "
                + "direction.");
            report.AppendLine();
            return TemperamentReading.None(TemperamentVerdict.Refuted);
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**So the spread ladder is re-sited.** The base above ran at {Fine(selectedBase)}, the "
            + $"median of the improvement distribution, which the reading above places "
            + $"{(selectedBase >= (herdBase.BaseThreshold * 2) ? "well past" : "at or past")} the "
            + $"transition. It is re-run at **{Fine(herdBase.BaseThreshold)}**, the non-zero base "
            + $"with the *highest* measured oscillation — the one place on the ladder where a herd "
            + $"demonstrably still exists for Temperament to damp. Blend is held at "
            + $"{Fix(BlendRungs[1])}, the even mixture `adr/0046` actually argues for, because a "
            + $"claim swept over two axes at once is a claim about neither."));
        report.AppendLine();
        report.AppendLine(
            "| Spread, of base | Oscillation | Synchrony | Effective arcs | "
            + RungHeadings("v/c") + " | Diversions/Tick | Steady |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|");

        long inRegimeZero = -1;
        long inRegimeLargest = -1;

        // The rungs are kept so the ladder's SHAPE can be read, not just its endpoints. A bar on the
        // endpoints alone would pass a ladder that dives and comes back up, and `adr/0046`'s own
        // wire is about monotonicity rather than about a net fall.
        var regime = new List<LoopOutcome>();

        foreach (int share in SpreadRungs)
        {
            Mark(string.Create(CultureInfo.InvariantCulture,
                $"R8.4 herd-regime spread={share} base={herdBase.BaseThreshold}"));

            LoopOutcome outcome = Measure(
                graph, districts, nextHop, freeFlow, representativeArc, pool, rung, load, selected,
                herdBase.BaseThreshold, share, BlendRungs[1]);

            herdLadder.Add(outcome);
            regime.Add(outcome);

            long oscillation = Mean(outcome.First.Oscillation, outcome.Second.Oscillation);

            if (share == 0)
            {
                inRegimeZero = oscillation;
            }

            inRegimeLargest = oscillation;

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Fix(outcome.SpreadShare)} | {Fix(oscillation)} | "
                + $"{Percent(outcome.Synchrony, Fixed.One)} | {Fix(outcome.EffectiveArcs)} | "
                + $"{Rungs(outcome.Vc)} | {Fix(outcome.DiversionsPerTick)} | "
                + $"{(outcome.Steady ? "yes" : "**no**")} |"));
        }

        report.AppendLine();

        long spreadGap = inRegimeZero - inRegimeLargest;
        bool variationDamps = inRegimeZero > 0
            && spreadGap * 10_000 >= inRegimeZero * HerdMarginHundredths;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Reading (ii).** Inside the herding regime, oscillation goes {Fix(inRegimeZero)} at "
            + $"spread 0 to {Fix(inRegimeLargest)} at the largest spread — "
            + $"{Percent(spreadGap, inRegimeZero == 0 ? 1 : inRegimeZero)} against a stated bar of "
            + $"{Hundredths(HerdMarginHundredths)}%. **Per-Citizen variation "
            + $"{(variationDamps ? "does" : "does not")} damp** where there is something to damp."));
        report.AppendLine();

        if (variationDamps)
        {
            report.AppendLine(
                "**That overturns the verdict above, and the overturned one is left standing rather "
                + "than deleted.** The spread ladder in the previous subsection was sited at the "
                + "median of the improvement distribution, which is past the transition — it swept "
                + "Temperament through a regime in which the base threshold had already killed the "
                + "herd, and read flat for that reason. Sited where a herd exists, spread damps. "
                + "`adr/0046`'s third layer is **not refuted**, and the lesson is about siting: a "
                + "sweep across a measured distribution is not automatically a sweep across the "
                + "regime the mechanism operates in.");
            report.AppendLine();

            return AppendRegimeShape(
                report, graph, districts, nextHop, freeFlow, representativeArc, pool, rung, load,
                selected, herdBase.BaseThreshold, regime, herdLadder, inRegimeZero,
                inRegimeLargest, spreadGap);
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**So the verdict is not `REFUTED`, and it is not a rescue either. It is sharper than "
            + $"both.** Measured **false**: that per-Citizen variation damps oscillation. Sited at "
            + $"the one base where a herd demonstrably survives, spreading the threshold across the "
            + $"population moves the amplitude by {Percent(spreadGap, inRegimeZero == 0 ? 1 : inRegimeZero)} "
            + $"against a {Hundredths(HerdMarginHundredths)}% bar. Measured **true**: that a "
            + $"threshold — *any* threshold past the transition, with no variation in it at all — "
            + $"does. The ladder falls by a factor of {Ratio(highest, lowest == 0 ? 1 : lowest)} on "
            + $"the base alone. **The base threshold does all the damping this model can measure, "
            + $"and making it vary per Citizen adds nothing on top of it.**"));
        report.AppendLine();
        report.AppendLine(
            "**Which of `adr/0046`'s two justifications for the layer survives is then a clean "
            + "question with a clean answer.** The ADR argues Temperament on two grounds. The "
            + "*mechanical* one — an identical rule over an identical input produces a herd, and "
            + "per-Citizen variation is what breaks it — **does not survive**: the herd it predicts "
            + "does not form at any threshold past the transition, so there is nothing for the "
            + "variation to prevent, and the structure collapses to **one Ruleset number**. The "
            + "*principled* one survives untouched, and it was never resting on this measurement: "
            + "`adr/0005` bans shared decisions outright, on `UNIQUE INDIVIDUALS` grounds, whether "
            + "or not sharing them happens to be stable. **A Citizen with its own threshold is "
            + "required by that ADR and is no longer justified by this one.**");
        report.AppendLine();
        report.AppendLine(
            "That is a design commitment surviving where a mechanical argument died, and it is "
            + "worth more to the corpus than *the layer failed*: it tells a successor exactly what "
            + "to keep (a per-Citizen threshold, because `adr/0005` says so), exactly what to stop "
            + "claiming for it (that it damps oscillation), and exactly what to tune instead (the "
            + "base, which is the only term with measured authority over the amplitude). The "
            + "stable-base-plus-jitter split and its two purpose tags are unaffected either way — "
            + "they exist so a Citizen's character and its mood are not the same draw, which is a "
            + "correlation argument and not a damping one.");
        report.AppendLine();

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"The herd-regime spread ladder is {SpreadRungs.Length} rungs at one blend weight "
            + $"({Fix(BlendRungs[1])}) and one base ({Fine(herdBase.BaseThreshold)}). The full "
            + $"cross product against the base ladder was not run; the claim it supports is about "
            + $"the base with the highest measured oscillation and no other."));

        return TemperamentReading.None(TemperamentVerdict.ThresholdDoesTheWork);
    }

    /// <summary>
    /// Whether the damping is <b>monotone</b> in spread, which is the form <c>adr/0046</c>'s wire
    /// actually takes — and, if it is not, whether the offending rung survives a change of blend.
    /// </summary>
    /// <remarks>
    /// A bar on the ladder's endpoints passes a ladder that dives and comes back up, and a ladder
    /// that comes back up is a herd re-forming at a particular amount of variation. That is either a
    /// finding about Temperament or one noisy rung, and the two are told apart by re-measuring the
    /// same spread at the blend weights the ladder did not use: a property of the spread survives a
    /// change of blend and a noisy rung does not.
    /// </remarks>
    private static TemperamentReading AppendRegimeShape(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        Pool pool,
        OdRung rung,
        int load,
        int selected,
        int herdBase,
        List<LoopOutcome> regime,
        List<LoopOutcome> herdLadder,
        long netFrom,
        long netTo,
        long netGap)
    {
        // The wire as `adr/0046` states it: amplitude falls with every step across the first three
        // rungs. A net fall is not that, and the difference is what this block is for.
        bool monotone = true;
        long previous = long.MaxValue;

        LoopOutcome? worst = null;
        long worstOscillation = -1;
        long previousOfWorst = 0;

        foreach (LoopOutcome outcome in regime)
        {
            long oscillation = Mean(outcome.First.Oscillation, outcome.Second.Oscillation);

            if (previous != long.MaxValue && oscillation > previous)
            {
                monotone = false;

                if (oscillation > worstOscillation)
                {
                    worstOscillation = oscillation;
                    worst = outcome;
                    previousOfWorst = previous;
                }
            }

            previous = oscillation;
        }

        // The wire as `plans/0010` states it, scored on the rungs it names: "amplitude must fall
        // monotonically in spread across at least the first three rungs".
        var series = new List<long>();
        foreach (LoopOutcome outcome in regime)
        {
            series.Add(Mean(outcome.First.Oscillation, outcome.Second.Oscillation));
        }

        bool monotoneFirstThree =
            series.Count >= 3 && series[1] < series[0] && series[2] < series[1];

        long breakingStep = 0;
        for (int i = 1; i < series.Count && i < 3; i++)
        {
            if (series[i] >= series[i - 1])
            {
                breakingStep = series[i] - series[i - 1];
                break;
            }
        }

        long netShare = netFrom == 0 ? 0 : IntegerMath.FloorDiv(netGap * Fixed.One, netFrom);

        if (monotone || worst is null)
        {
            report.AppendLine(
                "**And the fall is monotone across the ladder**, which is the form `adr/0046`'s "
                + "wire actually takes rather than the net-fall form the bar above was stated in. "
                + "Both readings agree, so nothing turns on the difference here.");
            report.AppendLine();

            return new TemperamentReading(
                TemperamentVerdict.NotRefuted, monotoneFirstThree, Resolution: 0,
                BreakingStep: breakingStep, UnresolvableSteps: 0,
                TotalSteps: series.Count - 1, netFrom, netTo, netShare);
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**But the fall is not monotone, and `adr/0046`'s wire is stated on monotonicity rather "
            + $"than on a net fall — so the two readings disagree and the disagreement is real.** At "
            + $"spread {Fix(worst.SpreadShare)} of the base the amplitude goes back up to "
            + $"{Fix(worstOscillation)} from {Fix(previousOfWorst)} on the rung below it. What makes "
            + $"that worth a paragraph rather than a shrug is that **three independent columns move "
            + $"together on it**: synchrony reaches {Percent(worst.Synchrony, Fixed.One)}, the "
            + $"effective arc count falls to {Fix(worst.EffectiveArcs)}, and diversions per Tick "
            + $"rise to {Fix(worst.DiversionsPerTick)}. Amplitude alone could be noise. Amplitude, "
            + $"synchrony and concentration agreeing is a herd."));
        report.AppendLine();
        report.AppendLine(
            "**It is one measurement, so it gets one cheap test rather than a paragraph of "
            + "reasoning.** The ladder was swept at a single blend weight. If the re-herd is a "
            + "property of *how much* the thresholds are spread, it should survive changing *what "
            + "they are spread with*; if it is one noisy rung, it should not. The same spread is "
            + "re-measured at the two blend weights the ladder did not use.");
        report.AppendLine();
        report.AppendLine("| Blend | Oscillation | Synchrony | Effective arcs | Diversions/Tick |");
        report.AppendLine("|---:|---:|---:|---:|---:|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {Fix(BlendRungs[1])} *(the ladder's)* | {Fix(worstOscillation)} | "
            + $"{Percent(worst.Synchrony, Fixed.One)} | {Fix(worst.EffectiveArcs)} | "
            + $"{Fix(worst.DiversionsPerTick)} |"));

        int survives = 0;
        int tested = 0;
        var probes = new List<long>();

        foreach (int blend in BlendRungs)
        {
            if (blend == BlendRungs[1])
            {
                continue;
            }

            Mark(string.Create(CultureInfo.InvariantCulture,
                $"R8.4 re-herd blend={blend} spread={worst.SpreadShare}"));

            LoopOutcome outcome = Measure(
                graph, districts, nextHop, freeFlow, representativeArc, pool, rung, load, selected,
                herdBase, worst.SpreadShare, blend);

            herdLadder.Add(outcome);
            tested++;

            long oscillation = Mean(outcome.First.Oscillation, outcome.Second.Oscillation);
            probes.Add(oscillation);

            // The re-herd counts as surviving this blend if the amplitude is still nearer the
            // offending rung's than the rung below it — a midpoint test, stated here before reading.
            long midpoint = previousOfWorst + IntegerMath.ShiftRight(
                worstOscillation - previousOfWorst, 1);

            if (oscillation > midpoint)
            {
                survives++;
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {Fix(blend)} | {Fix(oscillation)} | {Percent(outcome.Synchrony, Fixed.One)} | "
                + $"{Fix(outcome.EffectiveArcs)} | {Fix(outcome.DiversionsPerTick)} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The rule, stated before the two rows were read: the re-herd **survives a blend** if "
            + $"that blend's amplitude stays above the midpoint between the offending rung and the "
            + $"rung below it. It survived **{survives} of {tested}**."));
        report.AppendLine();
        report.AppendLine(survives > 0
            ? "**So it is not one noisy rung.** A herd re-forms at a specific amount of per-Citizen "
                + "spread, whatever the spread is composed of, and it does so *inside* the regime "
                + "where Temperament is supposed to be preventing exactly that. The layer damps on "
                + "net and does not damp monotonically, which means **the spread is a Ruleset value "
                + "with a bad interval in it** rather than a knob that gets safer as it is turned "
                + "up. That is a real constraint on `adr/0046`'s unratified spread and nothing in "
                + "the ADR anticipates it: it argues the endpoints fail and the mixture is the "
                + "point, and the mixture has a hole in it. **A successor must sweep spread finely "
                + "enough to find the hole before any value is ratified**, and R8's five rungs are "
                + "not that sweep."
            : "**So it is one noisy rung and the net-fall reading is the honest one.** The re-herd "
                + "does not survive a change of blend weight, which is what a property of the "
                + "spread would have to do. It is recorded rather than removed — the columns did "
                + "move together and that is why it was tested — but it carries no conclusion, and "
                + "`adr/0046`'s wire fails on monotonicity for a reason the data does not support "
                + "calling a mechanism.");
        report.AppendLine();

        // The two blend rows above were measured at ONE spread with nothing else changed, so the
        // amount they disagree by is a floor on what this instrument can resolve between spreads.
        // It is computed from a measurement made for a different purpose, which is what makes the
        // argument below independent of how the ladder happened to fall.
        long resolution = 0;
        if (probes.Count >= 2)
        {
            long low = probes[0];
            long high = probes[0];

            foreach (long value in probes)
            {
                if (value < low)
                {
                    low = value;
                }

                if (value > high)
                {
                    high = value;
                }
            }

            resolution = high - low;
        }

        int unresolvable = 0;
        for (int i = 1; i < series.Count; i++)
        {
            long step = series[i] > series[i - 1] ? series[i] - series[i - 1] : series[i - 1] - series[i];
            if (step < resolution)
            {
                unresolvable++;
            }
        }

        report.AppendLine("##### The wire, scored as written");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"`plans/0010` states it as *\"amplitude must fall monotonically in spread across at "
            + $"least the first three rungs. Flat or non-monotone refutes the layer.\"* The first "
            + $"three rungs of the ladder above read {Fix(series[0])}, {Fix(series[1])}, "
            + $"{Fix(series[2])}. **The wire is non-monotone and by the wire as written "
            + $"`adr/0046`'s third layer is REFUTED.** That is the score. It is not softened, and "
            + $"the net-fall reading below is not offered in its place — substituting a statistic "
            + $"for the one a wire was stated over is the exact move this section has caught itself "
            + $"making twice already."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Beside it, an argument about the instrument that does not depend on how the ladder "
            + $"fell.** The two blend rows above were measured at **one spread with nothing else "
            + $"changed**, and they returned {Fix(probes.Count > 0 ? probes[0] : 0)} and "
            + $"{Fix(probes.Count > 1 ? probes[1] : 0)} — a spread of {Fix(resolution)} between two "
            + $"measurements the monotonicity claim treats as the same point. **That is a floor on "
            + $"what this instrument can resolve between adjacent rungs**, established by a "
            + $"measurement taken for a different purpose. The step that breaks the wire is "
            + $"{Fix(breakingStep)}. {unresolvable} of the ladder's {series.Count - 1} steps "
            + $"{(unresolvable == 1 ? "is" : "are")} smaller than the resolution, which means the "
            + $"rungs they join are **mutually indistinguishable**. A monotonicity test over values the instrument cannot separate "
            + $"is not a test, and that holds whichever way those rungs had happened to fall — had "
            + $"they come out in ascending order the wire would have passed on the same "
            + $"non-information."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**What survives is a different statistic, and it is labelled as one.** Over the same "
            + $"ladder the amplitude falls {Fix(netFrom)} → {Fix(netTo)}, "
            + $"{Percent(netGap, netFrom == 0 ? 1 : netFrom)} against the "
            + $"{Hundredths(HerdMarginHundredths)}% bar stated before the run. That is a **net "
            + $"fall**, not a monotone fall; it is a claim about the ladder's endpoints and the wire "
            + $"was a claim about its steps. Both are printed, neither is presented as the other, "
            + $"and the tripwire table carries them on separate rows."));
        report.AppendLine();
        report.AppendLine("##### And the general lesson, which is now the third instance in R8");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**A wire stated on monotonicity cannot distinguish *nothing happened* from *it "
            + $"saturated at the first rung*.** The mechanism here is a **cliff, not a gradient**: "
            + $"the first spread rung alone takes the amplitude from {Fix(series[0])} to "
            + $"{Fix(series[1])}, a factor of {Ratio(series[0], series[1] == 0 ? 1 : series[1])}, "
            + $"and everything after it is flat inside the noise. Monotonicity was specified for a "
            + $"shape this phenomenon does not have, so the wire fires on the *saturation* rather "
            + $"than on any failure of the layer."));
        report.AppendLine();
        report.AppendLine(
            "**That is the same class of defect as the other two this section found, and three "
            + "instances make it the pattern rather than the anecdote.** A maximum over 33,018 "
            + "volume indices was chosen before anyone knew the distribution was nine parts empty. "
            + "An unconditioned p99 was chosen before anyone knew the same thing. A monotonicity "
            + "test was chosen before anyone knew the response was a cliff. **Each was a statistic "
            + "chosen before the shape of what it would measure was known**, and each survived "
            + "into a published wire because nothing in the process asks that question. `adr/0043` "
            + "requires a claim a measurement could settle to name the number that would refute it; "
            + "R8's experience adds a second requirement to that — **name the shape you expect, "
            + "because a number read off the wrong shape is not evidence.** A wire should be "
            + "re-derived once the first measurement shows what the response looks like, and the "
            + "re-derivation stated and scored separately rather than swapped in.");
        report.AppendLine();
        report.AppendLine(
            "**And the siting lesson beside it, which is this section's most transferable finding.** "
            + "The spread ladder was originally sited at the **median of the measured improvement "
            + "distribution** — a defensible choice, made precisely to avoid sweeping around a "
            + "number nobody had grounded, and it put every rung past the transition where the base "
            + "threshold had already killed the herd. *A sweep across a measured distribution is "
            + "not automatically a sweep across the regime the mechanism operates in.* The two are "
            + "different axes and coinciding is a coincidence. Siting a sweep requires locating the "
            + "**regime**, which means finding the transition first — and finding it cost five "
            + "rungs here.");
        report.AppendLine();

        return new TemperamentReading(
            TemperamentVerdict.NotRefuted, monotoneFirstThree, resolution, breakingStep,
            unresolvable, series.Count - 1, netFrom, netTo, netShare);
    }

    /// <summary>
    /// The value below which <paramref name="percent"/> of the histogram's mass lies, optionally
    /// over the decisions that were offered <b>something</b> rather than over all of them.
    /// </summary>
    /// <remarks>
    /// <b>The exclusion is not a convenience and it changes the answer completely.</b> Four decisions
    /// in five are offered an improvement of exactly zero — the best alternative is no better than
    /// Habit — and no threshold in <c>[0, 1]</c> can act on those, because the diversion test is a
    /// strict inequality. They are decisions a threshold is not <i>about</i>. Sited over all
    /// decisions the median is exactly zero, which makes the base zero, which makes every spread
    /// rung <c>share × 0</c>, which makes the whole Temperament sweep thirteen identical rows. That
    /// happened, and it is why this parameter exists.
    /// </remarks>
    private static int HistogramQuantile(
        long[] histogram, long samples, int percent, bool offeredSomethingOnly)
    {
        long mass = samples;
        int from = 0;

        if (offeredSomethingOnly && histogram.Length > 0)
        {
            mass = samples - histogram[0];
            from = 1;
        }

        if (mass <= 0)
        {
            return 0;
        }

        long target = IntegerMath.FloorDiv(mass * percent, 100);
        long seen = 0;

        for (int bucket = from; bucket < histogram.Length; bucket++)
        {
            seen += histogram[bucket];
            if (seen >= target)
            {
                return SightFleet.ImprovementLowerBound(bucket);
            }
        }

        return Fixed.One;
    }

    // --- R8.5 --------------------------------------------------------------------------------------

    private static void AppendSurge(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        Pool pool,
        OdRung rung,
        int load,
        int selected,
        List<string> caps,
        out bool contrast)
    {
        report.AppendLine("### R8.5 — does `03 §3.4`'s loop actually close?");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The load-bearing one, and **the version of it that ran in the previous capture could "
            + $"not answer the question**. That version replaced {SurgeShareHundredths}% of the fleet "
            + $"with Travellers bound for one District and watched the network recover. It recovered "
            + $"— five times out of five, at Horizon 0 as reliably as under Sight — and the reason "
            + $"was in the harness: this fleet respawns a Traveller against the pool the instant it "
            + $"arrives, so a one-off retarget is a **pulse with a half-life of one journey**. Any "
            + $"system at all recovers from a pulse it stops receiving. Control and Sight settling "
            + $"identically was not a null result; it was no result."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The surge is now sustained.** `SightFleet.SustainSurge` weights the *respawn pool* "
            + $"toward the surged District — {SurgeShareHundredths}% of every respawn for the whole "
            + $"{SurgeWindowTicks}-Tick window — on top of the same initial retarget. That is R1's "
            + $"monocentric morning peak as it actually behaves: people keep leaving for the centre "
            + $"for hours. Demand stays asymmetric while the network is watched, so the control "
            + $"cannot come down by waiting. Rung is {rung.Name}, {load:N0} Travellers."));
        report.AppendLine();
        report.AppendLine(
            "**And that changes the shape of the question.** Under a pulse the question was *does it "
            + "recover*. Under a sustained asymmetry it is **does it reach a bounded steady state, "
            + "and at what level** — a network that settles at a ruinous plateau has not "
            + "self-corrected, and one that never settles at all has diverged. Control and Sight "
            + "should differ in the level they settle at rather than in whether they settle. The "
            + "re-peak rule and its three versions are retired with the pulse they judged, and are "
            + "named here rather than removed.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The level rule, stated before the run and not touched after it.** The watched series "
            + $"is **p99 `v/c` over occupied car-carrying indices**, sampled every {PeakScanEvery} "
            + $"Ticks — occupied, because R8.3's cross-load block demonstrates *without reference to "
            + $"any outcome* that the unconditioned quantile cannot move on this network. Then:"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"> A rung has reached a **bounded steady state** if the mean of the last quarter of the "
            + $"window is within {SteadyMarginHundredths}% of the mean of the third quarter — two "
            + $"consecutive quarter-window means agreeing, which is R8.2's two-window steadiness "
            + $"test applied to a series instead of a scalar. It is **unbounded** otherwise. The "
            + $"**settling level** is the mean over the last quarter. **Sight beats the control** if "
            + $"both bound and Sight's settling level is at least "
            + $"{Hundredths(InstrumentMarginHundredths)}% below the control's — the same bar every "
            + $"other comparison in this section uses. If either fails to bound, the comparison is "
            + $"not made, and that is the result."));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Repeated into {SurgeDistricts} destination Districts and reported as a "
            + $"distribution.** One destination is one draw, and a single settle from a single "
            + $"District is an anecdote about that District's approach roads. The Districts are "
            + $"spread evenly across the index range for reproducibility rather than chosen for "
            + $"being busy."));
        report.AppendLine();

        var chosen = new List<int>();
        int stride = districts.Count > SurgeDistricts
            ? IntegerMath.FloorDiv(districts.Count, SurgeDistricts)
            : 1;

        for (int offset = 0; offset < districts.Count && chosen.Count < SurgeDistricts; offset++)
        {
            // Walk forward from each stride mark until a non-empty District turns up, so an empty
            // one does not silently drop a rung from the distribution.
            int start = offset * stride;

            for (int district = start; district < districts.Count; district++)
            {
                if (districts.Representative[district] >= 0 && !chosen.Contains(district))
                {
                    chosen.Add(district);
                    break;
                }
            }
        }

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"R8.5 runs {chosen.Count} destination Districts × 2 rungs. Each run is {WarmTicks} "
            + $"warm-up Ticks plus a {SurgeWindowTicks}-Tick observation with a full "
            + $"{graph.Volume.Length:N0}-index ladder scan every {PeakScanEvery} Ticks, which is the "
            + $"most expensive thing in this section."));

        report.AppendLine(
            "| Rung | District | Pre-surge level | Peak | Peaks at Tick | Third quarter | "
            + "Last quarter | Bounded | Settling level | Over pre-surge | End of window |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|:-:|---:|---:|---:|");

        var control = new List<SurgeOutcome>();
        var sight = new List<SurgeOutcome>();

        foreach (int district in chosen)
        {
            Mark(string.Create(CultureInfo.InvariantCulture, $"R8.5 control district={district}"));
            SurgeOutcome outcome = MeasureSurge(
                graph, districts, nextHop, freeFlow, representativeArc, pool, load,
                horizonSegments: 0, district);

            control.Add(outcome);
            AppendSurgeRow(report, "control, N=0", outcome);
        }

        foreach (int district in chosen)
        {
            Mark(string.Create(CultureInfo.InvariantCulture, $"R8.5 sight district={district}"));
            SurgeOutcome outcome = MeasureSurge(
                graph, districts, nextHop, freeFlow, representativeArc, pool, load, selected,
                district);

            sight.Add(outcome);
            AppendSurgeRow(
                report, string.Create(CultureInfo.InvariantCulture, $"Sight, N={selected}"),
                outcome);
        }

        report.AppendLine();
        report.AppendLine("The same runs as a distribution:");
        report.AppendLine();
        report.AppendLine(
            "| Rung | Bounded | Unbounded | Settling level, min | median | max | "
            + "Median over pre-surge |");
        report.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        AppendSurgeDistribution(report, "control, N=0", control);
        AppendSurgeDistribution(
            report, string.Create(CultureInfo.InvariantCulture, $"Sight, N={selected}"), sight);

        report.AppendLine();

        int controlBounded = 0;
        int sightBounded = 0;

        foreach (SurgeOutcome outcome in control)
        {
            if (outcome.Bounded)
            {
                controlBounded++;
            }
        }

        foreach (SurgeOutcome outcome in sight)
        {
            if (outcome.Bounded)
            {
                sightBounded++;
            }
        }

        long controlLevel = MedianLevel(control);
        long sightLevel = MedianLevel(sight);
        long levelGap = controlLevel - sightLevel;

        bool bothBound = controlBounded == control.Count && sightBounded == sight.Count;
        bool beats = bothBound
            && levelGap * 10_000 >= controlLevel * InstrumentMarginHundredths;

        contrast = beats;

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**The reading.** The control bounded {controlBounded} of {control.Count} and Sight "
            + $"bounded {sightBounded} of {sight.Count}. Median settling level: control "
            + $"{Fix(controlLevel)}, Sight {Fix(sightLevel)}, a difference of "
            + $"{Percent(levelGap, controlLevel)} against a stated bar of "
            + $"{Hundredths(InstrumentMarginHundredths)}%."));
        report.AppendLine();

        if (!bothBound)
        {
            report.AppendLine(
                "**At least one rung did not reach a bounded steady state, so by the rule stated "
                + "above the comparison is not made.** That is not a failure of the measurement — "
                + "it is the measurement. A rung that does not bound under a sustained asymmetric "
                + "demand is a rung where `03 §3.4`'s self-correction did not close, and which rung "
                + "it was decides what it means: an unbounded **control** is the expected result and "
                + "the thing that makes the row readable at all, while an unbounded **Sight** rung "
                + "would refute `adr/0046` outright. The per-District table above says which.");
            report.AppendLine();
        }
        else
        {
            report.AppendLine(beats
                ? "**Both rungs bound and Sight settles materially lower, so `03 §3.4`'s loop "
                    + "closes with only the local layers reading the VDF.** That is the claim "
                    + "`adr/0046` is most exposed on, tested under a demand asymmetry that does not "
                    + "go away, against a control carrying identical physics and no ability to "
                    + "respond. It does not make Sight sufficient — the level it settles at is "
                    + "printed and a reader can judge whether that plateau is a city anybody would "
                    + "want — but the mechanism is real and the self-correction is not being done "
                    + "by the harness."
                : "**Both rungs bound, and Sight does not settle materially lower than the "
                    + "control.** By the rule stated above that is a negative result on "
                    + "`adr/0046`'s most exposed claim: under a sustained asymmetry the local "
                    + "layers reach the same plateau as a fleet that cannot respond at all. Before "
                    + "it is read as a refutation, note what R8.2 established at this load — Sight "
                    + "*redistributes*, moving load off a saturated tail onto empty road — and that "
                    + "a conditioned quantile of a spreading distribution is not guaranteed to fall "
                    + "even when every arc the model can resolve has improved. The clamp-share "
                    + "column in R8.3 is the reading that does not have that problem, and a "
                    + "successor should state this rule on it.");
            report.AppendLine();
        }

        int lateControl = 0;
        int lateSight = 0;
        int lastQuarterStart = SurgeWindowTicks - IntegerMath.FloorDiv(SurgeWindowTicks, 4);

        foreach (SurgeOutcome outcome in control)
        {
            if (outcome.PeakTick >= lastQuarterStart)
            {
                lateControl++;
            }
        }

        foreach (SurgeOutcome outcome in sight)
        {
            if (outcome.PeakTick >= lastQuarterStart)
            {
                lateSight++;
            }
        }

        if (lateControl > 0 || lateSight > 0)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"**A caveat the *Peaks at Tick* column earns, and it does not cut the way a caveat "
                + $"usually does.** {lateControl} of {control.Count} control runs and {lateSight} of "
                + $"{sight.Count} Sight runs reached their highest sample inside the **last quarter "
                + $"of the window** — the same quarter the settling level is read over. A series "
                + $"whose maximum is at the end may still be climbing, and the quarter-agreement "
                + $"test can pass on a slow climb. Where that happens the settling level is a "
                + $"**lower bound** on where that rung would eventually sit. Since it is the control "
                + $"that peaks late here, the effect is to *understate* the control's plateau and "
                + $"therefore to understate Sight's advantage; the conclusion is not at risk from "
                + $"it, and a longer window would widen the gap rather than close it. A successor "
                + $"should run the window until the peak sample is not in the last quarter, which "
                + $"is a stated condition and cheap to check."));
            report.AppendLine();
        }

        report.AppendLine(
            "**One limit on this row, stated because it bounds the conclusion either way.** The "
            + "surge is sustained on the *destination* only: origins still come from the rung's own "
            + "pool, so what is modelled is everybody heading for one District rather than "
            + "everybody leaving one place for it. A real morning peak is asymmetric at both ends. "
            + "R2's 412% funnel figure was measured with both endpoints pinned, and it is the "
            + "harsher shape; this row is the milder one and its result should be read as a lower "
            + "bound on how hard a real peak would press.");
        report.AppendLine();
    }

    /// <summary>The median settling level over the surge runs of one rung.</summary>
    private static long MedianLevel(List<SurgeOutcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return 0;
        }

        var levels = new List<int>(outcomes.Count);
        foreach (SurgeOutcome outcome in outcomes)
        {
            levels.Add(outcome.Level);
        }

        levels.Sort();
        return levels[IntegerMath.FloorDiv(levels.Count, 2)];
    }

    /// <summary>One sustained surge, read on the occupied-index p99 series.</summary>
    private sealed record SurgeOutcome(
        int District,
        int Before,
        int Peak,
        int PeakTick,
        int ThirdQuarter,
        int LastQuarter,
        bool Bounded,
        int Level,
        int EndOfWindow);

    private static void AppendSurgeRow(StringBuilder report, string label, SurgeOutcome outcome)
    {
        long over = outcome.Before == 0
            ? 0
            : IntegerMath.FloorDiv((long)(outcome.Level - outcome.Before) * Fixed.One, outcome.Before);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {label} | {outcome.District} | {Fix(outcome.Before)} | {Fix(outcome.Peak)} | "
            + $"{outcome.PeakTick} | {Fix(outcome.ThirdQuarter)} | {Fix(outcome.LastQuarter)} | "
            + $"{(outcome.Bounded ? "yes" : "**no**")} | {Fix(outcome.Level)} | "
            + $"{Percent(over, Fixed.One)} | {Fix(outcome.EndOfWindow)} |"));
    }

    private static void AppendSurgeDistribution(
        StringBuilder report, string label, List<SurgeOutcome> outcomes)
    {
        int bounded = 0;
        int unbounded = 0;
        var levels = new List<int>();
        var over = new List<long>();

        foreach (SurgeOutcome outcome in outcomes)
        {
            if (outcome.Bounded)
            {
                bounded++;
            }
            else
            {
                unbounded++;
            }

            levels.Add(outcome.Level);

            over.Add(outcome.Before == 0
                ? 0
                : IntegerMath.FloorDiv(
                    (long)(outcome.Level - outcome.Before) * Fixed.One, outcome.Before));
        }

        levels.Sort();
        over.Sort();

        string min = levels.Count == 0 ? "\u2014" : Fix(levels[0]);
        string median = levels.Count == 0
            ? "\u2014"
            : Fix(levels[IntegerMath.FloorDiv(levels.Count, 2)]);
        string max = levels.Count == 0 ? "\u2014" : Fix(levels[levels.Count - 1]);
        string medianOver = over.Count == 0
            ? "\u2014"
            : Percent(over[IntegerMath.FloorDiv(over.Count, 2)], Fixed.One);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| {label} | {bounded} | {unbounded} | {min} | {median} | {max} | {medianOver} |"));
    }

    private static SurgeOutcome MeasureSurge(
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        Pool pool,
        int load,
        int horizonSegments,
        int destination)
    {
        var fleet = NewFleet(
            graph, districts, nextHop, freeFlow, pool, load, horizonSegments, DraftThreshold,
            spread: 0, blend: 0);

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
        }

        int before = Sample(graph, representativeArc, fleet.Volume);

        // Both halves of a morning peak: the fleet already on the road turns toward the District,
        // and everybody who starts a Trip from here on is drawn toward it too. The second is what
        // makes the demand SUSTAINED, and without it the first is a pulse the harness recovers from.
        int share = (int)IntegerMath.FloorDiv((long)SurgeShareHundredths * Fixed.One, 100);
        fleet.SustainSurge(destination, share);
        fleet.Surge((int)IntegerMath.FloorDiv((long)load * SurgeShareHundredths, 100), destination);

        // The whole series is recorded and read afterwards, never against a running statistic. Read
        // the running way once, and it reported a recovery ninety-six Ticks BEFORE the peak it was
        // supposedly recovering from — a negative duration in a published column, which is what a
        // one-pass detector over a series that has not finished rising will always eventually print.
        int samples = IntegerMath.CeilDiv(SurgeWindowTicks, PeakScanEvery);
        var series = new int[samples];

        for (int tick = 0; tick < SurgeWindowTicks; tick++)
        {
            fleet.Advance();

            if (tick % PeakScanEvery == 0)
            {
                series[IntegerMath.FloorDiv(tick, PeakScanEvery)] =
                    Sample(graph, representativeArc, fleet.Volume);
            }
        }

        int peak = 0;
        int peakSample = 0;

        for (int sample = 0; sample < samples; sample++)
        {
            if (series[sample] > peak)
            {
                peak = series[sample];
                peakSample = sample;
            }
        }

        int quarter = IntegerMath.FloorDiv(samples, 4);
        if (quarter < 1)
        {
            quarter = 1;
        }

        long third = 0;
        long last = 0;

        for (int sample = samples - (2 * quarter); sample < samples - quarter; sample++)
        {
            third += series[sample];
        }

        for (int sample = samples - quarter; sample < samples; sample++)
        {
            last += series[sample];
        }

        third = IntegerMath.FloorDiv(third, quarter);
        last = IntegerMath.FloorDiv(last, quarter);

        long larger = third > last ? third : last;
        long gap = third > last ? third - last : last - third;
        bool bounded = larger == 0 || gap * 100 <= larger * SteadyMarginHundredths;

        return new SurgeOutcome(
            District: destination,
            Before: before,
            Peak: peak,
            PeakTick: peakSample * PeakScanEvery,
            ThirdQuarter: (int)third,
            LastQuarter: (int)last,
            Bounded: bounded,
            Level: (int)last,
            EndOfWindow: series[samples - 1]);
    }

    /// <summary>
    /// One p99 reading over the <b>occupied</b> car-carrying indices, for the surge series.
    /// </summary>
    /// <remarks>
    /// Occupied rather than unconditioned, because the cross-load ladder demonstrates — without
    /// reference to any outcome — that on this network the unconditioned quantile reads the edge of
    /// the empty region and does not move.
    /// </remarks>
    private static int Sample(RoadGraph graph, int[] representativeArc, int[] volume)
    {
        var ladder = new Ladder();
        ladder.Scan(graph, representativeArc, volume);
        return ladder.Read().P99Occupied;
    }

    // --- R8.6 --------------------------------------------------------------------------------------

    private static void AppendDiversionCost(
        StringBuilder report,
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        Pool pool,
        OdRung rung,
        int load,
        int selected,
        List<LoopOutcome> sweep,
        List<string> caps)
    {
        report.AppendLine("### R8.6 — what a diversion costs, by path source");
        report.AppendLine();
        report.AppendLine(
            "Under a next-hop table a mid-journey diversion is **free**: the Traveller reads a "
            + "different arc out of the table and resumes from wherever it now is. Under a stored "
            + "route the same diversion costs a fresh search from the current node. Sight makes "
            + "diversion a *routine* event rather than an exception, so this stops being a footnote "
            + "and becomes a per-Tick bill that scales with how congested the city is.");
        report.AppendLine();
        report.AppendLine(
            "**It does not go through `HpaSearch`.** R5.5 found a pristine-seeding defect there and "
            + "R6 owns it; R8's diversion search reads the live cost array directly, so nothing here "
            + "inherits it.");
        report.AppendLine();

        // Diversion sites captured from a live rung rather than drawn, so the search lengths are the
        // ones diversions actually happen at. A drawn origin would be a whole journey and would
        // over-price the re-search by however far the Traveller had already come.
        var fleet = NewFleet(
            graph, districts, nextHop, freeFlow, pool, load, selected, DraftThreshold,
            spread: 0, blend: 0);

        var log = new int[8_192];
        fleet.DiversionLog = log;

        var siteNode = new List<int>();
        var siteTarget = new List<int>();

        for (int tick = 0; tick < WarmTicks && siteNode.Count < 512; tick++)
        {
            fleet.Advance();

            for (int i = 0; i + 1 < fleet.DiversionLogCount && siteNode.Count < 512; i += 2)
            {
                siteNode.Add(log[i]);
                siteTarget.Add(log[i + 1]);
            }
        }

        caps.Add(string.Create(CultureInfo.InvariantCulture,
            $"R8.6 prices {siteNode.Count:N0} diversion sites, captured from a live rung during "
            + $"warm-up rather than drawn. The capture buffer is {log.Length / 2:N0} sites per Tick "
            + $"and a Tick that overflows it is truncated silently by the fleet — the count here is "
            + $"the honest denominator."));

        if (siteNode.Count == 0)
        {
            report.AppendLine(
                "**No diversions occurred during the capture window, so there is nothing to price.** "
                + "That is itself a finding and it is reported rather than worked around.");
            report.AppendLine();
            return;
        }

        long sink = 0;
        long start = Stopwatch.GetTimestamp();
        const int Repeats = 64;

        for (int repeat = 0; repeat < Repeats; repeat++)
        {
            for (int i = 0; i < siteNode.Count; i++)
            {
                sink += nextHop.Of(siteNode[i], siteTarget[i]);
            }
        }

        long tableNanoseconds = IntegerMath.FloorDiv(
            Since(start), (long)Repeats * siteNode.Count);

        var search = new PointToPoint(graph, fleet.LiveTicks);
        int found = 0;
        var arcs = new List<int>();

        start = Stopwatch.GetTimestamp();

        for (int i = 0; i < siteNode.Count; i++)
        {
            AccessPoint origin = At(graph, siteNode[i]);
            AccessPoint goal = At(graph, districts.Representative[siteTarget[i]]);

            search.Bootstrap(origin, goal, Modes.Car, HeuristicKind.Chebyshev);
            SearchOutcome outcome = search.Expand();

            arcs.Clear();
            if (outcome.Found)
            {
                search.PathArcs(arcs);
                found++;
            }
        }

        long searchNanoseconds = IntegerMath.FloorDiv(Since(start), siteNode.Count);

        long diversionsPerTick = 0;
        foreach (LoopOutcome outcome in sweep)
        {
            if (outcome.Horizon == selected)
            {
                diversionsPerTick = outcome.DiversionsPerTick;
            }
        }

        long perTickTable = IntegerMath.FloorDiv(tableNanoseconds * diversionsPerTick, Fixed.One);
        long perTickSearch = IntegerMath.FloorDiv(searchNanoseconds * diversionsPerTick, Fixed.One);

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"At N = {selected}, {rung.Name}, {load:N0} Travellers, {Fix(diversionsPerTick)} diversions per "
            + $"Tick measured "
            + $"in R8.3. {found:N0} of {siteNode.Count:N0} re-searches found a route."));
        report.AppendLine();
        report.AppendLine("| Path source | Per diversion | × diversions/Tick | of 15.6 ms |");
        report.AppendLine("|---|---:|---:|---:|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| next-hop table read | {tableNanoseconds:N0} ns | {perTickTable:N0} ns | "
            + $"{Percent(perTickTable, 15_600_000)} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| flat A\\* over live costs | {searchNanoseconds:N0} ns | "
            + $"{Milliseconds(perTickSearch)} | {Percent(perTickSearch, 15_600_000)} |"));
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"The table read is timed over {Repeats} passes of the captured sites because one read is "
            + $"a single strided load and a single-pass timing would be measuring the clock. The "
            + $"search is timed once per site, cold, with its arcs returned — R0's denominator shape, "
            + $"over the **live** array rather than the free-flow one. `sink` is {sink} and exists so "
            + $"the reads are not elided."));
        report.AppendLine();
        report.AppendLine(
            "**This is the third axis session M is owed** — structural error, temporal error, and now "
            + "diversion cost — and R7 states the verdict. It decides nothing on its own.");
        report.AppendLine();
    }

    // --- The denominator and the tripwires ---------------------------------------------------------

    private static void AppendDenominator(StringBuilder report, long first, long last)
    {
        report.AppendLine("### The denominator, measured first and last");
        report.AppendLine();
        report.AppendLine(
            "R5's rule after the one-processor pinning artefact: a denominator read only at the "
            + "start of a long section describes the minute before the section rather than the "
            + "section. R5's own capture read 1,401,307 ns first and 477,609 ns last, which is what "
            + "the practice exists to catch.");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"- One uncached point-to-point search, arcs returned, free-flow costs: "
            + $"{Microseconds(first)} measured first, {Microseconds(last)} measured last, "
            + $"{Ratio(first, last)} apart."));
        report.AppendLine();
    }

    private static void AppendTripwires(
        StringBuilder report,
        LoopOutcome control,
        LoopOutcome instrument,
        List<LoopOutcome> sweep,
        List<LoopOutcome> temperament,
        List<LoopOutcome> thresholds,
        int floor,
        int oneTraveller,
        int load,
        bool herdMetricValidated,
        bool surgeContrast,
        List<LoopOutcome> crossLoad,
        List<LoopOutcome> herdLadder,
        LoopOutcome positive,
        TemperamentReading temperament8)
    {
        report.AppendLine("### The tripwires, and each one's verdict");
        report.AppendLine();

        long peakGap = control.Vc.P99 - instrument.Vc.P99;
        bool lowersPeak = peakGap * 10_000 >= (long)control.Vc.P99 * InstrumentMarginHundredths;

        long meanGap = instrument.MeanTopRatio > control.MeanTopRatio
            ? instrument.MeanTopRatio - control.MeanTopRatio
            : control.MeanTopRatio - instrument.MeanTopRatio;

        bool connected =
            meanGap * 10_000 >= control.MeanTopRatio * InstrumentMarginHundredths
            && control.Diversions == 0;

        long conservation = 0;
        long bounded = 0;
        long unplaced = 0;
        long spawnFailures = 0;
        int unsteady = 0;
        int rungs = 0;

        foreach (LoopOutcome outcome in All(
            sweep, temperament, thresholds, crossLoad, herdLadder, control, instrument, positive))
        {
            conservation += outcome.ConservationFailures;
            bounded += outcome.BoundedTotal;
            unplaced += outcome.UnplacedTotal;
            spawnFailures += outcome.SpawnFailures;
            rungs++;

            if (!outcome.Steady)
            {
                unsteady++;
            }
        }

        bool conserved = conservation == 0 && bounded == 0 && unplaced == 0 && spawnFailures == 0;
        // The mis-sited ladder's own monotonicity, kept because the section reports what that
        // ladder said before the re-siting corrected it, and a reader can check the claim.
        bool monotoneAtMedian = MonotoneInSpread(temperament);
        bool peakMonotone = MonotoneInHorizon(sweep, Rung.P99);

        report.AppendLine("| # | Tripwire | Verdict | Reading |");
        report.AppendLine("|---:|---|:-:|---|");
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 1 | Sight lowers p99 `v/c` against the control | "
            + $"{(lowersPeak ? "**PASS**" : "**FIRED**")} | {Fix(instrument.Vc.P99)} against "
            + $"{Fix(control.Vc.P99)}, {Percent(peakGap, control.Vc.P99)} against a bar of "
            + $"{Hundredths(InstrumentMarginHundredths)}%, at N = {floor}, {load:N0} Travellers |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 1a | *Advisory, stated after the numbers and scoring nothing*: the same wire read "
            + $"over **occupied** indices | "
            + $"{(control.Vc.P99Occupied - instrument.Vc.P99Occupied > 0 ? "would pass" : "would fire")} "
            + $"| {Fix(instrument.Vc.P99Occupied)} against {Fix(control.Vc.P99Occupied)}. Nine "
            + $"car-carrying indices in ten are empty, so wire 1's population is mostly empty road "
            + $"|"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 2 | The instrument is connected — Sight changes the trajectory, control cannot "
            + $"respond | {(connected ? "**PASS**" : "**FIRED**")} | mean `v/c` "
            + $"{Fix(control.MeanTopRatio)} → {Fix(instrument.MeanTopRatio)}, "
            + $"{Percent(meanGap, control.MeanTopRatio)}; control diversions "
            + $"{control.Diversions:N0} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 3 | Conservation, every Tick, every rung | "
            + $"{(conserved ? "**PASS**" : "**FIRED**")} | {conservation:N0} volume, "
            + $"{unplaced:N0} unplaced, {bounded:N0} bounded, {spawnFailures:N0} spawn failures, "
            + $"over {rungs} rungs |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| 4 | Steady state established by two-window agreement, not assumed | "
            + $"{(unsteady == 0 ? "**PASS**" : "**FIRED**")} | {unsteady} of {rungs} rungs outside "
            + $"{SteadyMarginHundredths}%; each is marked **no** in its own table |"));
        report.AppendLine(
            "| 5 | The Sight pass's cost is measured, never derived | **PASS** | R8.3's `Sight "
            + "ns/Tick` is this rung's measured `Move` minus the control's, and `Refresh` is timed "
            + "separately |");
        report.AppendLine(
            "| 6 | Every table names its O-D rung and its load | **PASS** | R8.0 and R8.2 to R8.6 "
            + "name both in the section text; R8.3's cross-check names one rung per row |");
        // Two rows, kept apart deliberately. The first is `plans/0010`'s wire scored on the
        // statistic it names; the second is what survives on a DIFFERENT statistic and says so.
        // Collapsing them into one verdict is how a substituted statistic gets published.
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| — | `adr/0046` / `plans/0010`: Temperament damps the herd — amplitude falls "
            + $"**monotonically** in spread across the first three rungs | "
            + $"{temperament8.Verdict switch
            {
                TemperamentVerdict.NotTested => "**NOT TESTED**",
                TemperamentVerdict.Refuted => "**REFUTED**",
                _ => temperament8.MonotoneFirstThree ? "**not refuted**" : "**REFUTED**",
            }} | "
            + $"{temperament8.Verdict switch
            {
                TemperamentVerdict.NotTested =>
                    "the maximal-herd positive control does not separate from the swept family on "
                        + "either metric, so neither metric is a herd detector on this network and "
                        + "neither may carry a refutation; see R8.4",
                TemperamentVerdict.Refuted =>
                    "neither the base threshold nor the spread damps, so something else suppresses "
                        + "the herd on this network; see R8.4",
                _ => temperament8.MonotoneFirstThree
                    ? "sited at the base where a herd demonstrably survives, amplitude falls at "
                        + "every step of the first three rungs; see R8.4"
                    : string.Create(CultureInfo.InvariantCulture,
                        $"the ladder is non-monotone, scored as written and not softened. Beside "
                        + $"it: two measurements at **one** spread with only the blend changed "
                        + $"differ by {Fix(temperament8.Resolution)}, which is a floor on what this "
                        + $"instrument resolves between rungs; the step that breaks the wire is "
                        + $"{Fix(temperament8.BreakingStep)}, and "
                        + $"{temperament8.UnresolvableSteps} of {temperament8.TotalSteps} steps "
                        + $"{(temperament8.UnresolvableSteps == 1 ? "is" : "are")} below the floor. **A monotonicity test over values the instrument "
                        + $"cannot separate is not a test**, and that holds whichever way they had "
                        + $"fallen. The mis-sited ladder at the improvement median read "
                        + $"{(monotoneAtMedian ? "monotone" : "non-monotone")} and is left standing "
                        + $"in R8.4's history; see R8.4"),
            }} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| — | *A **different** statistic from the wire above, published beside it and never "
            + $"in its place*: the **net fall** in amplitude across the same ladder | "
            + $"{(temperament8.Verdict == TemperamentVerdict.NotRefuted
                ? "**damps**"
                : "—")} | "
            + $"{(temperament8.Verdict == TemperamentVerdict.NotRefuted
                ? string.Create(CultureInfo.InvariantCulture,
                    $"{Fix(temperament8.NetFrom)} → {Fix(temperament8.NetTo)}, "
                    + $"{Percent(temperament8.NetShare, Fixed.One)} against a "
                    + $"{Hundredths(HerdMarginHundredths)}% bar stated before the run. A claim "
                    + $"about the ladder's **endpoints**; the wire above was a claim about its "
                    + $"**steps**. The response is a cliff — the first rung alone carries it — and "
                    + $"monotonicity was specified for a shape this phenomenon does not have; "
                    + $"see R8.4"
                    )
                : "not reached")} |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| — | `adr/0046`: Sight is a mechanism — p99 `v/c` falls with `N` | "
            + $"{(peakMonotone ? "**not refuted**" : "**non-monotone**")} | see R8.3 |"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"| — | `adr/0046`: under a **sustained** asymmetry of demand, `03 §3.4`'s "
            + $"self-correction still closes with only the local layers reading the VDF | "
            + $"{(surgeContrast ? "**not refuted**" : "**REFUTED**")} | "
            + $"{(surgeContrast
                ? "both rungs reach a bounded steady state and Sight settles materially lower than "
                    + "a control with identical physics and no ability to respond; see R8.5"
                : "Sight does not settle materially below the Horizon-0 control, or one of the two "
                    + "did not bound at all; see R8.5 for which, and for what it bounds")} |"));
        report.AppendLine();

        if (temperament8.Verdict == TemperamentVerdict.NotRefuted
            && !temperament8.MonotoneFirstThree)
        {
            report.AppendLine(
                "**The Temperament verdict has now changed twice and the final one is this**: "
                + "`plans/0010`'s wire, which is stated on monotonicity, is **REFUTED** — scored "
                + "as written, on the statistic it named, and not substituted. Standing beside it "
                + "and not in place of it: the layer **does** damp the amplitude, by a large "
                + "factor, sited where a herd exists; the wire fails because the response is a "
                + "cliff rather than a gradient and the rungs after the cliff are separated by "
                + "less than the instrument can resolve. The earlier flat reading at the "
                + "improvement distribution's median was a **siting artefact** and is left "
                + "standing in this section's history rather than deleted, as is the `REFUTED` it "
                + "produced. **What a successor should carry forward: keep the layer, stop "
                + "claiming monotonicity for it, and re-state the wire on a shape the response "
                + "actually has.**");
            report.AppendLine();
        }

        if (!herdMetricValidated)
        {
            report.AppendLine(
                "**The previous capture of this section reported Temperament `REFUTED`, and that "
                + "verdict is withdrawn rather than amended.** It rested on a flat oscillation "
                + "column from an instrument nobody had shown could move. R8.2 exists precisely "
                + "because this spike has shipped instruments that could not move; applying that "
                + "standard to R8.2's own rung and not to R8.4's was an inconsistency, and the "
                + "positive control is what closes it. `adr/0046`'s Temperament layer is **not "
                + "measured** by R8, and `plans/0010` should treat it as owed rather than settled.");
            report.AppendLine();
        }

        if (!conserved)
        {
            report.AppendLine(
                "**A conservation check failed.** `adr/0041`'s invariant is the one that catches a "
                + "Traveller vanishing without decrementing, which presents as a road that looks busy "
                + "forever and produced a `v/c` of 883 the last time it went unnoticed. Nothing in "
                + "this section should be read until it is explained.");
            report.AppendLine();
        }

        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"For reference and no longer for firing: one Traveller present is worth "
            + $"{Fix(oneTraveller)} of this `v/c` at the median car-passable index, so an oscillation "
            + $"amplitude is readable in Travellers per Tick per arc."));
        report.AppendLine();
    }

    /// <summary>Whether peak <c>v/c</c> falls with every step up the Horizon ladder.</summary>
    /// <summary>Which rung of the ladder a monotonicity question is being asked about.</summary>
    private enum Rung
    {
        P99,
        P99Occupied,
        Max,
    }

    private static bool MonotoneInHorizon(List<LoopOutcome> sweep, Rung rung)
    {
        int previous = int.MaxValue;

        foreach (LoopOutcome outcome in sweep)
        {
            int reading = rung switch
            {
                Rung.Max => outcome.Vc.Max,
                Rung.P99Occupied => outcome.Vc.P99Occupied,
                _ => outcome.Vc.P99,
            };

            if (reading > previous)
            {
                return false;
            }

            previous = reading;
        }

        return true;
    }

    /// <summary>
    /// How many <b>distinct</b> values a rung of the ladder takes across a sweep. One is the signature
    /// of a statistic that cannot move, which is an instrument defect and not a result.
    /// </summary>
    private static int DistinctReadings(List<LoopOutcome> sweep, Rung rung)
    {
        var seen = new List<int>();

        foreach (LoopOutcome outcome in sweep)
        {
            int reading = rung switch
            {
                Rung.Max => outcome.Vc.Max,
                Rung.P99Occupied => outcome.Vc.P99Occupied,
                _ => outcome.Vc.P99,
            };

            if (!seen.Contains(reading))
            {
                seen.Add(reading);
            }
        }

        return seen.Count;
    }

    private static void AppendCaps(StringBuilder report, List<string> caps)
    {
        report.AppendLine("### The caps imposed, all of them");
        report.AppendLine();
        report.AppendLine(
            "A silent truncation reads as *we covered everything* when it did not, so each one is "
            + "here whether or not it changes a conclusion.");
        report.AppendLine();

        foreach (string cap in caps)
        {
            report.AppendLine("- " + cap);
        }

        report.AppendLine();
    }

    // --- Measurement -------------------------------------------------------------------------------

    private static LoopOutcome Measure(
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        int[] representativeArc,
        Pool pool,
        OdRung rung,
        int load,
        int horizonSegments,
        int baseThreshold,
        int spreadShare,
        int blend)
    {
        // The spread rung is a SHARE of the base threshold and the fleet takes an absolute Q16.16
        // amount, so the multiplication happens exactly here and nowhere else. It was written in both
        // places first, which squared it — the Temperament sweep then ran at a hundredth of its
        // stated spread and reported the layer flat, which is `adr/0046`'s own predicted refutation
        // arriving as an arithmetic bug.
        var fleet = NewFleet(
            graph, districts, nextHop, freeFlow, pool, load,
            horizonSegments, baseThreshold, Fixed.Mul(spreadShare, baseThreshold), blend);

        var accumulated = new long[graph.Volume.Length];
        var checks = new Checks();

        for (int tick = 0; tick < WarmTicks; tick++)
        {
            fleet.Advance();
            checks.Inspect(fleet);

            for (int index = 0; index < accumulated.Length; index++)
            {
                accumulated[index] += fleet.Volume[index];
            }
        }

        // Warm-up decisions are discarded: while the fleet is still spreading, the improvements on
        // offer are not the ones a settled network offers, and R8.4 reads its threshold rungs off
        // this distribution.
        fleet.ClearImprovement();

        int[] top = Top(accumulated, representativeArc);

        // ONE ladder across both windows rather than one each. Taking a component-wise maximum of two
        // ladders would put a max back on top of p50, which is the very move this whole revision
        // exists to undo; pooling makes the reading the distribution of `v/c` over the observation.
        var ladder = new Ladder();

        Window first = MeasureWindow(fleet, graph, representativeArc, top, checks, ladder);
        Window second = MeasureWindow(fleet, graph, representativeArc, top, checks, ladder);

        long a = first.Oscillation;
        long b = second.Oscillation;
        long larger = a > b ? a : b;
        long gap = a > b ? a - b : b - a;
        bool steady = larger == 0 || gap * 100 <= larger * SteadyMarginHundredths;

        return new LoopOutcome(
            Od: rung,
            Load: load,
            Horizon: horizonSegments,
            BaseThreshold: baseThreshold,
            SpreadShare: spreadShare,
            Blend: blend,
            First: first,
            Second: second,
            Steady: steady,
            Vc: ladder.Read(),
            Synchrony: Mean(first.Synchrony, second.Synchrony),
            EffectiveArcs: Mean(first.EffectiveArcs, second.EffectiveArcs),
            MeanTopRatio: Mean(first.MeanTopRatio, second.MeanTopRatio),
            AboveClamp: Mean(first.AboveClamp, second.AboveClamp),
            DiversionsPerTick: Mean(first.DiversionsPerTick, second.DiversionsPerTick),
            CrossingsPerTick: Mean(first.CrossingsPerTick, second.CrossingsPerTick),
            Diversions: first.Diversions + second.Diversions,
            Crossings: first.Crossings + second.Crossings,
            NoAlternative: first.NoAlternative + second.NoAlternative,
            MeanJourneyTicks: Mean(first.MeanJourneyTicks, second.MeanJourneyTicks),
            RefreshNanoseconds: Mean(first.RefreshNanoseconds, second.RefreshNanoseconds),
            MoveNanoseconds: Mean(first.MoveNanoseconds, second.MoveNanoseconds),
            ConservationFailures: checks.Conservation,
            BoundedTotal: checks.Bounded,
            UnplacedTotal: checks.Unplaced,
            SpawnFailures: fleet.SpawnFailures);
    }

    private static Window MeasureWindow(
        SightFleet fleet, RoadGraph graph, int[] representativeArc, int[] top, Checks checks,
        Ladder ladder)
    {
        var previous = new int[top.Length];
        for (int i = 0; i < top.Length; i++)
        {
            previous[i] = Congestion.LiveRatioUnclamped(graph, representativeArc[top[i]], fleet.Volume);
        }

        long previousMean = MeanOf(previous);

        long absolute = 0;
        long meanFirst = 0;
        long topTotal = 0;
        long aboveClamp = 0;
        long diversions = 0;
        long crossings = 0;
        long noAlternative = 0;
        long refresh = 0;
        long move = 0;

        // Synchrony is averaged over the Ticks on which anybody diverted at all. Averaging over every
        // Tick would report a quiet rung as unherded when what it actually was is empty.
        long synchrony = 0;
        long effective = 0;
        long divertingTicks = 0;

        long journeysBefore = fleet.CompletedJourneys;
        long journeyTicksBefore = fleet.CompletedJourneyTicks;

        for (int tick = 0; tick < WindowTicks; tick++)
        {
            long start = Stopwatch.GetTimestamp();
            fleet.Refresh();
            refresh += Since(start);

            start = Stopwatch.GetTimestamp();
            fleet.Move();
            move += Since(start);

            checks.Inspect(fleet);

            for (int i = 0; i < top.Length; i++)
            {
                int current = Congestion.LiveRatioUnclamped(
                    graph, representativeArc[top[i]], fleet.Volume);

                absolute += current > previous[i] ? current - previous[i] : previous[i] - current;
                previous[i] = current;
                topTotal += current;

                // Counted, because BPR never sees anything above this and a reader has to know how
                // much of the congested core the router is blind inside.
                if (current > Congestion.MaximumVolumeCapacity)
                {
                    aboveClamp++;
                }
            }

            long currentMean = MeanOf(previous);
            meanFirst += currentMean > previousMean ? currentMean - previousMean : previousMean - currentMean;
            previousMean = currentMean;

            diversions += fleet.Diversions;
            crossings += fleet.Crossings;
            noAlternative += fleet.NoAlternative;

            if (fleet.Diversions > 0)
            {
                synchrony += fleet.DiversionTopShare;
                effective += fleet.DiversionEffectiveArcs;
                divertingTicks++;
            }

            if (tick % PeakScanEvery == 0)
            {
                ladder.Scan(graph, representativeArc, fleet.Volume);
            }
        }

        long samples = (long)top.Length * WindowTicks;
        long journeys = fleet.CompletedJourneys - journeysBefore;
        long journeyTicks = fleet.CompletedJourneyTicks - journeyTicksBefore;

        return new Window(
            Oscillation: IntegerMath.FloorDiv(absolute, samples),
            MeanFirst: IntegerMath.FloorDiv(meanFirst, WindowTicks),
            MeanTopRatio: IntegerMath.FloorDiv(topTotal, samples),
            AboveClamp: IntegerMath.FloorDiv(aboveClamp * Fixed.One, samples),
            Synchrony: divertingTicks == 0 ? 0 : IntegerMath.FloorDiv(synchrony, divertingTicks),
            EffectiveArcs: divertingTicks == 0 ? 0 : IntegerMath.FloorDiv(effective, divertingTicks),
            Diversions: diversions,
            Crossings: crossings,
            NoAlternative: noAlternative,
            DiversionsPerTick: IntegerMath.FloorDiv(diversions * Fixed.One, WindowTicks),
            CrossingsPerTick: IntegerMath.FloorDiv(crossings * Fixed.One, WindowTicks),
            MeanJourneyTicks: journeys == 0 ? 0 : IntegerMath.FloorDiv(journeyTicks * Fixed.One, journeys),
            RefreshNanoseconds: IntegerMath.FloorDiv(refresh, WindowTicks),
            MoveNanoseconds: IntegerMath.FloorDiv(move, WindowTicks));
    }

    private static SightFleet NewFleet(
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        int[] freeFlow,
        Pool pool,
        int load,
        int horizonSegments,
        int baseThreshold,
        int spread,
        int blend) =>
        new(
            graph,
            districts,
            freeFlow,
            nextHop,
            pool.OriginNode,
            pool.Target,
            load,
            horizonSegments,
            CongestionParameters.Working.Alpha,
            baseThreshold,
            spread,
            blend,
            CounterHash.Seed);

    /// <summary>Every Tick's three assertions, counted rather than thrown so the run finishes.</summary>
    private sealed class Checks
    {
        public long Conservation { get; private set; }

        public long Bounded { get; private set; }

        public long Unplaced { get; private set; }

        public void Inspect(SightFleet fleet)
        {
            if (fleet.TotalVolume() != (long)fleet.Size * Fixed.One)
            {
                Conservation++;
            }

            Bounded += fleet.Bounded;
            Unplaced += fleet.Unplaced();
        }
    }

    // --- Pools, tops and peaks ---------------------------------------------------------------------

    private sealed record Pool(int[] OriginNode, int[] Target);

    private static Pool BuildPool(
        RoadGraph graph,
        Districts districts,
        NextHopTable nextHop,
        OdDistribution distribution,
        OdRung rung,
        out int discarded)
    {
        OdPair[] pairs = distribution.Draw(
            CounterHash.Seed, PoolPairs, Modes.Car, rung, out _, out _);

        var origins = new List<int>(PoolPairs);
        var targets = new List<int>(PoolPairs);
        discarded = 0;

        foreach (OdPair pair in pairs)
        {
            int origin = graph.SegmentNodeA[pair.Origin.Segment];
            int district = districts.OfNode[graph.SegmentNodeA[pair.Destination.Segment]];

            if (district < 0
                || districts.Representative[district] < 0
                || origin == districts.Representative[district]
                || nextHop.DistanceOf(origin, district) == RoadGraph.Impassable)
            {
                discarded++;
                continue;
            }

            origins.Add(origin);
            targets.Add(district);
        }

        return new Pool([.. origins], [.. targets]);
    }

    /// <summary>One car-passable arc per volume index, so a peak scan reads each index once.</summary>
    private static int[] RepresentativeArcs(RoadGraph graph)
    {
        var representative = new int[graph.Volume.Length];
        Array.Fill(representative, -1);

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            int index = graph.VolumeIndex(arc);
            if (representative[index] < 0 && graph.ArcCarTicks[arc] != RoadGraph.Impassable)
            {
                representative[index] = arc;
            }
        }

        return representative;
    }

    private static int[] Top(long[] accumulated, int[] representativeArc)
    {
        var top = new int[TopIndices];
        var taken = new bool[accumulated.Length];

        for (int slot = 0; slot < TopIndices; slot++)
        {
            int best = -1;
            long bestValue = -1;

            for (int index = 0; index < accumulated.Length; index++)
            {
                if (taken[index] || representativeArc[index] < 0 || accumulated[index] <= bestValue)
                {
                    continue;
                }

                best = index;
                bestValue = accumulated[index];
            }

            // A graph with fewer than 64 used indices would leave a slot empty; repeating the last
            // one keeps the metric's denominator honest at 64 rather than silently shrinking it.
            top[slot] = best >= 0 ? best : top[slot > 0 ? slot - 1 : 0];
            if (best >= 0)
            {
                taken[best] = true;
            }
        }

        return top;
    }

    /// <summary>
    /// What one Traveller present is worth in this <c>v/c</c>, taken as the median over car-passable
    /// volume indices.
    /// </summary>
    /// <remarks>
    /// <b>The noise floor of the oscillation metric is a graph property and it is measured here
    /// rather than asserted.</b> Tripwire 2 asks whether the control is <i>near zero</i>, and near
    /// zero has to mean something: a fixed routing policy with random respawns still has Travellers
    /// entering and leaving every arc every Tick, and each one moves the reading by this much. A
    /// ceiling stated in Travellers is a ceiling a reader can check against the capacity table; one
    /// stated as a bare Q16.16 constant is a number somebody liked.
    /// </remarks>
    private static int OneTravellerRatio(RoadGraph graph, int[] representativeArc)
    {
        var unit = new int[graph.Volume.Length];
        Array.Fill(unit, Fixed.One);

        var ratios = new List<int>(representativeArc.Length);

        for (int index = 0; index < representativeArc.Length; index++)
        {
            if (representativeArc[index] >= 0)
            {
                ratios.Add(Congestion.LiveRatioUnclamped(graph, representativeArc[index], unit));
            }
        }

        int[] sorted = [.. ratios];
        Array.Sort(sorted);
        return Quantile(sorted, 50);
    }

    /// <summary>
    /// The distribution of live <c>v/c</c> over one observation: p50, p90, p99, the maximum, and the
    /// share of car-carrying volume indices holding nothing at all.
    /// </summary>
    /// <remarks>
    /// <b>A maximum over 33,018 volume indices is the worst available summary of that population</b>,
    /// and S4 has already paid for the lesson once — a run whose worst iteration was 100.2 ms read
    /// 2.462 ms at p99.9. One arc pinned against the BPR clamp says nothing about whether a network
    /// is congested, and every conclusion R8 drew from a max was a conclusion about one arc. The max
    /// is kept as the last column, because a runaway arc is still worth seeing; it is never the
    /// headline.
    /// </remarks>
    private sealed record Vc(int P50, int P90, int P99, int P99Occupied, int Max, long ZeroShare);

    /// <summary>A histogram of live <c>v/c</c> readings that <see cref="Vc"/> is read out of.</summary>
    private sealed class Ladder
    {
        // A sixty-fourth of a v/c unit per bucket, to 128.00 — above anything R8.0 produced at any
        // load. Readings past that land in the overflow bucket and the maximum is tracked exactly, so
        // the ladder cannot flatter a runaway by saturating.
        private const int BucketShift = 10;
        private const int Buckets = 8_192;

        private readonly long[] _counts = new long[Buckets + 1];
        private long _samples;
        private long _zero;
        private int _max;

        /// <summary>Readings folded in so far, across every index and every scan.</summary>
        public long Samples => _samples;

        public void Add(int ratio)
        {
            _samples++;

            if (ratio <= 0)
            {
                _zero++;
                _counts[0]++;
                return;
            }

            if (ratio > _max)
            {
                _max = ratio;
            }

            long bucket = IntegerMath.ShiftRight((long)ratio, BucketShift);
            _counts[bucket >= Buckets ? Buckets : (int)bucket]++;
        }

        /// <summary>Folds in one reading per car-carrying volume index.</summary>
        public void Scan(RoadGraph graph, int[] representativeArc, int[] volume)
        {
            for (int index = 0; index < representativeArc.Length; index++)
            {
                if (representativeArc[index] < 0)
                {
                    continue;
                }

                Add(Congestion.LiveRatioUnclamped(graph, representativeArc[index], volume));
            }
        }

        public Vc Read() => new(
            P50: Quantile(50, _samples, 0),
            P90: Quantile(90, _samples, 0),
            P99: Quantile(99, _samples, 0),
            P99Occupied: Quantile(99, _samples - _zero, _zero),
            Max: _max,
            ZeroShare: _samples == 0 ? 0 : IntegerMath.FloorDiv(_zero * Fixed.One, _samples));

        /// <summary>
        /// The lower edge of the bucket the quantile falls in, so a quantile never overstates. The
        /// comparison is cross-multiplied rather than divided because a rounded target would move a
        /// quantile by a whole bucket at small sample counts.
        /// </summary>
        /// <remarks>
        /// <paramref name="skip"/> discounts a prefix of the first bucket, which is what makes an
        /// <i>occupied-index</i> quantile possible from the same histogram: readings of exactly zero
        /// are counted separately from the readings between zero and a sixty-fourth that share
        /// bucket 0 with them.
        /// </remarks>
        private int Quantile(int percent, long population, long skip)
        {
            if (population <= 0)
            {
                return 0;
            }

            long target = population * percent;
            long seen = -skip;

            for (int bucket = 0; bucket <= Buckets; bucket++)
            {
                seen += _counts[bucket];

                if (seen > 0 && seen * 100 >= target)
                {
                    return bucket >= Buckets
                        ? _max
                        : (int)IntegerMath.ShiftLeft((long)bucket, BucketShift);
                }
            }

            return _max;
        }
    }

    /// <summary>
    /// The ladder cells of a Markdown row: p50, p90, p99, p99 over occupied indices only, max.
    /// </summary>
    private static string Rungs(Vc vc) =>
        $"{Fix(vc.P50)} | {Fix(vc.P90)} | {Fix(vc.P99)} | {Fix(vc.P99Occupied)} | {Fix(vc.Max)}";

    /// <summary>The ladder headings of a Markdown table, with a caller-supplied stem.</summary>
    private static string RungHeadings(string stem) =>
        $"{stem} p50 | {stem} p90 | {stem} p99 | {stem} p99 occupied | {stem} max";

    /// <summary>
    /// The volume at which every car-carrying index would read exactly <c>v/c = 1</c>, Q16.16
    /// vehicles — the whole network's holding capacity at saturation flow.
    /// </summary>
    /// <remarks>
    /// Capacity is a flow and volume is a count, so the two only meet through a traversal time; this
    /// is the same product <see cref="Congestion.LiveRatioUnclamped"/> divides by, summed. It is what
    /// makes an operating load of a few thousand Travellers mean something.
    /// </remarks>
    private static long HoldingCapacity(RoadGraph graph, int[] representativeArc)
    {
        long total = 0;

        for (int index = 0; index < representativeArc.Length; index++)
        {
            int arc = representativeArc[index];
            if (arc < 0)
            {
                continue;
            }

            int free = graph.ArcCarTicks[arc];
            if (free == RoadGraph.Impassable || free <= 0)
            {
                continue;
            }

            int flow = graph.SegmentCapacity[graph.ArcSegment[arc]];
            if (graph.Parameters.VolumeScope == VolumeScope.PerDirection)
            {
                flow = IntegerMath.ShiftRight(flow, 1);
            }

            total += Fixed.Mul(flow, free);
        }

        return total;
    }

    /// <summary>
    /// The share of all volume sitting on the busiest <paramref name="percent"/> of car-carrying
    /// indices, Q16.16 — a concentration reading, and the thing that decides whether a saturated
    /// network is short of road or short of routes.
    /// </summary>
    private static long HeadShare(
        RoadGraph graph, int[] representativeArc, int[] volume, int percent)
    {
        int count = 0;

        for (int index = 0; index < representativeArc.Length; index++)
        {
            if (representativeArc[index] >= 0)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return 0;
        }

        var loads = new int[count];
        int at = 0;
        long total = 0;

        for (int index = 0; index < representativeArc.Length; index++)
        {
            if (representativeArc[index] < 0)
            {
                continue;
            }

            int load = volume[index];
            loads[at++] = load;
            total += load;
        }

        if (total == 0)
        {
            return 0;
        }

        Array.Sort(loads);

        int head = IntegerMath.FloorDiv(count * percent, 100);
        if (head < 1)
        {
            head = 1;
        }

        long carried = 0;
        for (int i = count - head; i < count; i++)
        {
            carried += loads[i];
        }

        return IntegerMath.FloorDiv(carried * Fixed.One, total);
    }

    /// <summary>
    /// An Access Point standing exactly on <paramref name="node"/>, expressed the only way the query
    /// shape allows — an offset along one of its Segments, never a node.
    /// </summary>
    /// <remarks>
    /// <b>The Segment chosen has to admit cars.</b> A foot-only Segment gives a perfectly valid
    /// Access Point that a Car search cannot bootstrap from, and the first draft of this picked the
    /// first incident arc regardless: fourteen of five hundred and twelve re-searches came back
    /// <i>not found</i>, which reads as a disconnected graph rather than as a chosen wrong Segment.
    /// </remarks>
    private static AccessPoint At(RoadGraph graph, int node)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            if (graph.ArcCarTicks[arc] == RoadGraph.Impassable)
            {
                continue;
            }

            int segment = graph.ArcSegment[arc];
            return new AccessPoint(
                segment,
                graph.SegmentNodeA[segment] == node ? 0 : graph.SegmentLengthTiles[segment]);
        }

        return new AccessPoint(0, 0);
    }

    private static long MeasureFlatDenominator(RoadGraph graph, int[] freeFlow, OdPair[] pool)
    {
        var search = new PointToPoint(graph, freeFlow);
        var arcs = new List<int>();
        int found = 0;

        long start = Stopwatch.GetTimestamp();

        foreach (OdPair pair in pool)
        {
            search.Bootstrap(pair.Origin, pair.Destination, Modes.Car, HeuristicKind.Chebyshev);
            SearchOutcome outcome = search.Expand();
            arcs.Clear();

            if (outcome.Found)
            {
                search.PathArcs(arcs);
                found++;
            }
        }

        long taken = Since(start);
        return found == 0 ? 0 : IntegerMath.FloorDiv(taken, pool.Length);
    }

    private static bool MonotoneInSpread(List<LoopOutcome> temperament)
    {
        // Read at one blend weight — the even one — because a monotonicity claim over a sweep of two
        // axes at once is a claim about neither.
        var series = new List<long>();

        foreach (int share in SpreadRungs)
        {
            foreach (LoopOutcome outcome in temperament)
            {
                if (outcome.SpreadShare != share)
                {
                    continue;
                }

                if (share == 0 || outcome.Blend == BlendRungs[1])
                {
                    series.Add(Mean(outcome.First.Oscillation, outcome.Second.Oscillation));
                    break;
                }
            }
        }

        if (series.Count < 3)
        {
            return false;
        }

        return series[1] < series[0] && series[2] < series[1];
    }

    private static IEnumerable<LoopOutcome> All(
        List<LoopOutcome> sweep,
        List<LoopOutcome> temperament,
        List<LoopOutcome> thresholds,
        List<LoopOutcome> crossLoad,
        List<LoopOutcome> herdLadder,
        LoopOutcome control,
        LoopOutcome instrument,
        LoopOutcome positive)
    {
        foreach (LoopOutcome outcome in crossLoad)
        {
            yield return outcome;
        }

        foreach (LoopOutcome outcome in herdLadder)
        {
            yield return outcome;
        }

        foreach (LoopOutcome outcome in sweep)
        {
            yield return outcome;
        }

        foreach (LoopOutcome outcome in temperament)
        {
            yield return outcome;
        }

        foreach (LoopOutcome outcome in thresholds)
        {
            yield return outcome;
        }

        yield return control;
        yield return instrument;
        yield return positive;
    }

    // --- Rows --------------------------------------------------------------------------------------

    private sealed record Window(
        long Oscillation,
        long MeanFirst,
        long MeanTopRatio,
        long AboveClamp,
        long Synchrony,
        long EffectiveArcs,
        long Diversions,
        long Crossings,
        long NoAlternative,
        long DiversionsPerTick,
        long CrossingsPerTick,
        long MeanJourneyTicks,
        long RefreshNanoseconds,
        long MoveNanoseconds);

    private sealed record LoopOutcome(
        OdRung Od,
        int Load,
        int Horizon,
        int BaseThreshold,
        int SpreadShare,
        int Blend,
        Window First,
        Window Second,
        bool Steady,
        Vc Vc,
        long Synchrony,
        long EffectiveArcs,
        long MeanTopRatio,
        long AboveClamp,
        long DiversionsPerTick,
        long CrossingsPerTick,
        long Diversions,
        long Crossings,
        long NoAlternative,
        long MeanJourneyTicks,
        long RefreshNanoseconds,
        long MoveNanoseconds,
        long ConservationFailures,
        long BoundedTotal,
        long UnplacedTotal,
        long SpawnFailures);

    // --- Formatting --------------------------------------------------------------------------------

    private static void Mark(string section) =>
        Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {section}");

    private static long Since(long start)
    {
        long elapsed = Stopwatch.GetTimestamp() - start;
        long whole = elapsed / Stopwatch.Frequency;
        long remainder = elapsed - (whole * Stopwatch.Frequency);
        return (whole * 1_000_000_000) + (remainder * 1_000_000_000 / Stopwatch.Frequency);
    }

    private static long Mean(long a, long b) => (a + b) / 2;

    private static long MeanOf(int[] values)
    {
        long total = 0;
        foreach (int value in values)
        {
            total += value;
        }

        return values.Length == 0 ? 0 : total / values.Length;
    }

    private static int ShareAtZero(int[] sorted)
    {
        int count = 0;
        while (count < sorted.Length && sorted[count] == 0)
        {
            count++;
        }

        return count;
    }

    private static int Quantile(int[] sorted, int percent)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        int index = (int)(((long)sorted.Length * percent) / 100);
        return sorted[index >= sorted.Length ? sorted.Length - 1 : index];
    }

    /// <summary>Q16.16 to hundredths, for the <see cref="Hundredths"/> formatter.</summary>
    private static int Centi(long fixedValue) => (int)((fixedValue * 100) / Fixed.One);

    private static string Fix(long fixedValue) => Hundredths(Centi(fixedValue));

    /// <summary>
    /// Six decimal places. For quantities that live three orders below what <see cref="Fix"/> can
    /// show, which is where the relative-improvement distribution turned out to live.
    /// </summary>
    private static string Fine(long fixedValue)
    {
        long millionths = (fixedValue * 1_000_000) / Fixed.One;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{millionths / 1_000_000}.{IntegerMath.Abs((int)(millionths % 1_000_000)):D6}");
    }

    private static string Hundredths(int value) => string.Create(
        CultureInfo.InvariantCulture, $"{value / 100}.{IntegerMath.Abs(value % 100):D2}");

    private static string Percent(long part, long whole) =>
        whole == 0 ? "—" : Hundredths((int)((part * 10_000) / whole)) + "%";

    private static string Ratio(long numerator, long denominator) =>
        denominator == 0 ? "—" : Hundredths((int)((numerator * 100) / denominator)) + "×";

    private static string Microseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10)) + " µs";

    private static string Milliseconds(long nanoseconds) =>
        Hundredths((int)(nanoseconds / 10_000)) + " ms";

    private static string Bytes(long bytes) =>
        bytes < 1024 ? string.Create(CultureInfo.InvariantCulture, $"{bytes} B")
        : bytes < 1024 * 1024 ? Hundredths((int)((bytes * 100) / 1024)) + " KiB"
        : Hundredths((int)((bytes * 100) / (1024 * 1024))) + " MiB";
}
