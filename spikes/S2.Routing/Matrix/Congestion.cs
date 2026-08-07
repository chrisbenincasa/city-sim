using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Matrix;

/// <summary>
/// The swept parameters of the synthetic congestion field. Nothing here is a chosen value.
/// </summary>
/// <param name="BaseVolumeCapacity">
/// Q16.16 <c>volume / capacity</c> at the map centre at full intensity and no imbalance. <b>The
/// intensity axis</b>, swept.
/// </param>
/// <param name="Imbalance">
/// Q16.16 in <c>[0, 1]</c>. How lopsided a peak is between the two directions of a Segment: zero is
/// a peak that loads both directions equally, one is a peak entirely in the phase's direction.
/// <b>The axis the asymmetry finding is a curve against</b>, and the axis S2 cannot measure —
/// see <see cref="ArcVolumeCapacity"/>.
/// </param>
/// <param name="Alpha">BPR's <c>α</c>, Q16.16. The standard is 0.15.</param>
/// <remarks>
/// <b>There is no <c>Beta</c> here, and its absence is deliberate.</b> An earlier draft carried one
/// and <see cref="Congestion.Delay"/> ignored it, being fixed at the standard 4 — a parameter that is
/// accepted and discarded is a claim the code does not honour, and this corpus has spent three
/// findings on values never checked against what they claimed to describe. <c>β</c> is stated as a
/// constant where it is used, with the argument for fixing it.
/// </remarks>
internal readonly record struct CongestionParameters(
    int BaseVolumeCapacity,
    int Imbalance,
    int Alpha)
{
    /// <summary>BPR's standard <c>α</c>, 0.15 in Q16.16.</summary>
    public const int StandardAlpha = 9_830;

    /// <summary>
    /// The working rung: a centre running at <c>v/c = 1.2</c> at the peak, half of it directional.
    /// A starting point on both axes and neither is a finding.
    /// </summary>
    public static CongestionParameters Working => new(
        BaseVolumeCapacity: 78_643,   // 1.20
        Imbalance: 32_768,            // 0.50
        Alpha: StandardAlpha);
}

/// <summary>
/// The synthetic peak congestion field, and the VDF that turns it into a travel time.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file exists because a free-flow matrix cannot answer the question R1 was sent to answer.</b>
/// <c>plans/0010</c> R1 requires <i>"the distribution of <c>|matrix[i][j] − matrix[j][i]|</c> at
/// peak"</i>, and says of it: <i>"This is what settles the <c>adr/0020</c> exposure."</i> On R0's graph
/// free-flow costs are symmetric by construction — a Segment has one length and one free-flow speed —
/// so a free-flow matrix has an asymmetry of exactly zero, and reporting that would have been the
/// vacuity this corpus keeps catching itself in: R0.5's severance count that read zero because the
/// instrument could not see, and the Census's trend assertion that was deliberately not written
/// because it could not fail. <b>An asymmetry can only exist where the two directions of one Segment
/// carry different volumes</b>, so the matrix's asymmetry is downstream of a decision R0 left open and
/// R1 is the first thing that needs settled.
/// </para>
/// <para>
/// <b>And that is the finding, before any number is captured.</b> Under
/// <see cref="VolumeScope.PerSegment"/> the two directions share one counter, so the VDF returns the
/// same delay both ways, the matrix is symmetric to the bit, and <c>adr/0020</c>'s union-find is
/// exactly right — not by evidence but by construction. Under <see cref="VolumeScope.PerDirection"/>
/// it is not. The volume-scope question and the <c>adr/0020</c> exposure are therefore <b>one
/// question</b>, and R0 priced the storage side of it at 5% of the graph. R1 reports both scopes
/// side by side for exactly that reason.
/// </para>
/// <para>
/// <b>What this field is, and what it is not.</b> It is a monocentric commute: volume rises toward the
/// map centre and leans in the phase's direction. It is <i>synthetic</i> — there are no Travellers in
/// S2 and R2 is where volume comes from a routed load. So no figure derived from it is a claim about
/// how asymmetric a real city is; every one of them is a claim of the form <i>"at directional
/// imbalance <c>i</c>, the matrix's asymmetry is <c>x</c>"</i>. The imbalance is the swept axis and
/// <b>S2 cannot measure where on it the design sits</b> — that needs a generator mix and a sun arc
/// with widths, which decision 5a says the corpus does not have.
/// </para>
/// </remarks>
internal static class Congestion
{
    /// <summary>
    /// The ceiling on <c>volume / capacity</c>. Not a modelling claim — a magnitude guard.
    /// </summary>
    /// <remarks>
    /// BPR at <c>β = 4</c> grows fast enough that an unbounded ratio overflows Q16.16 long before it
    /// says anything: <c>v/c = 8</c> is a 615× delay multiplier. The Microscopic tier is what covers
    /// saturation (<c>CONTEXT.md</c> → VDF: the function is <i>"exact when free-flowing and wrong when
    /// saturated"</i>), so a Statistical travel time past this point is already the wrong instrument
    /// and clamping it costs nothing that was not already lost.
    /// </remarks>
    public const int MaximumVolumeCapacity = 4 * Fixed.One;

    /// <summary>
    /// Per-arc car traversal times under a phase's congestion, Q16.16 Ticks, or
    /// <see cref="RoadGraph.Impassable"/>.
    /// </summary>
    /// <remarks>
    /// Returned as a fresh array per phase rather than mutated in place, because the whole point of
    /// the per-phase rung is that five of them exist at once and the Day-average is a sixth.
    /// </remarks>
    public static int[] CarTicks(RoadGraph graph, CongestionParameters parameters, Phase phase)
    {
        // The demand field is deposited into a volume column and then read back out, rather than
        // being applied to the arc directly. That indirection is the whole experiment: under
        // PerSegment the two directions of a Segment land on ONE index and sum, so the VDF cannot
        // see which way the peak points; under PerDirection they land on two and it can. Computing
        // the ratio straight from the field would have made the scope a storage decision, which is
        // exactly the misreading R0's footprint table invites and R1 exists to correct.
        var volume = new int[graph.Volume.Length];
        Deposit(graph, parameters, phase, volume);

        var ticks = new int[graph.Arcs];

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            int free = graph.ArcCarTicks[arc];
            if (free == RoadGraph.Impassable)
            {
                ticks[arc] = RoadGraph.Impassable;
                continue;
            }

            ticks[arc] = Fixed.Mul(free, Delay(Ratio(graph, arc, volume), parameters.Alpha));
        }

        return ticks;
    }

    /// <summary>
    /// The <c>volume / capacity</c> the VDF actually sees for an arc, at the graph's volume scope.
    /// </summary>
    /// <remarks>
    /// <b>Capacity is halved under <see cref="VolumeScope.PerDirection"/> and whole under
    /// <see cref="VolumeScope.PerSegment"/></b>, because a Segment's capacity covers all of its
    /// Lanes and <c>CONTEXT.md</c> → Lane makes them directional queues, about half each way. This is
    /// what makes the two scopes agree exactly on a balanced Segment and diverge on a lopsided one —
    /// the divergence being the corpus's own sentence, that a Segment jammed inbound at the morning
    /// peak <i>"reads roughly half-loaded if the two directions are summed"</i>.
    /// </remarks>
    private static int Ratio(RoadGraph graph, int arc, int[] volume)
    {
        int capacity = graph.SegmentCapacity[graph.ArcSegment[arc]];
        if (graph.Parameters.VolumeScope == VolumeScope.PerDirection)
        {
            capacity = IntegerMath.ShiftRight(capacity, 1);
        }

        if (capacity <= 0)
        {
            return 0;
        }

        int ratio = Fixed.Div(volume[graph.VolumeIndex(arc)], capacity);
        return ratio > MaximumVolumeCapacity ? MaximumVolumeCapacity : ratio;
    }

    /// <summary>
    /// <c>volume / capacity</c> for one arc against a <b>caller-supplied live</b> volume column, with
    /// no ceiling applied. This is <b><c>Aggregate.Ratio</c>'s quantity</b>, unclamped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The denominator is <c>capacity × free-flow time</c> and not the bare capacity</b>, because
    /// a live volume column counts Travellers <i>present</i> while capacity is a flow. That makes
    /// this the same <c>v/c</c> R2b published a threshold crossing on, which is the point: two
    /// numbers that share a name must share a definition.
    /// </para>
    /// <para>
    /// <b>Additive, and it deliberately does not share an implementation with <see cref="Ratio"/>.</b>
    /// R1's asymmetry figures and R2's crossover are computed through that method against the
    /// synthetic demand field and are already recorded in <c>docs/spike-results.md</c>; folding R8's
    /// fleet into the same code path is how a published number moves without anyone deciding it
    /// should. The capacity arithmetic is repeated here rather than extracted for exactly that
    /// reason, and the duplication is the cheaper mistake.
    /// </para>
    /// <para>
    /// <b>The ceiling is off because R8's oscillation metric is taken over precisely the arcs most
    /// likely to be sitting on it.</b> A clamped reading on a saturated arc is constant by
    /// construction, so an amplitude computed over the busiest volume indices would read zero whether
    /// the network was still or thrashing — an instrument that cannot move, which this spike has
    /// already shipped three times. <see cref="LiveRatio"/> puts the ceiling back for the arcs that
    /// feed BPR, which is the one place it means something.
    /// </para>
    /// <para>
    /// Divided through <c>IntegerMath.FloorDiv</c> in <c>long</c> rather than through
    /// <c>Fixed.Div</c>. Same quotient, and it does not throw when a jam puts the ratio past
    /// Q16.16's ±32,768 — a harness that dies on the reading it exists to take reports nothing at all.
    /// </para>
    /// </remarks>
    public static int LiveRatioUnclamped(RoadGraph graph, int arc, int[] volume)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(volume);

        // Capacity is a FLOW and this volume column is a COUNT, so the two need reconciling before
        // they may be divided — `Aggregate.Ratio`'s argument, arrived at here for the same reason.
        // Vehicles that can be *present* is the flow times how long a vehicle stays, and that
        // product is the denominator. An earlier draft of this divided by the bare flow because the
        // private `Ratio` does, which is correct there — that method reads a demand field deposited
        // as a share of capacity, not a count of Travellers — and wrong here by the free-flow factor.
        // Two figures sharing the name `v/c` and differing by 13% is how a corpus acquires a
        // contradiction nobody can find later.
        int free = graph.ArcCarTicks[arc];
        if (free == RoadGraph.Impassable || free <= 0)
        {
            return 0;
        }

        int flow = graph.SegmentCapacity[graph.ArcSegment[arc]];
        if (graph.Parameters.VolumeScope == VolumeScope.PerDirection)
        {
            flow = IntegerMath.ShiftRight(flow, 1);
        }

        // Free-flow traversal time, never the congested one. A congested denominator grows as the
        // Segment jams — more vehicles fit because each stays longer — so a jam would partly hide
        // itself in the very reading built to detect it.
        int capacity = Fixed.Mul(flow, free);
        if (capacity <= 0)
        {
            return 0;
        }

        long ratio = IntegerMath.FloorDiv(
            IntegerMath.ShiftLeft((long)volume[graph.VolumeIndex(arc)], Fixed.FractionalBits),
            capacity);

        return ratio > int.MaxValue ? int.MaxValue : (int)ratio;
    }

    /// <summary>
    /// <see cref="LiveRatioUnclamped"/> under <see cref="MaximumVolumeCapacity"/>'s ceiling — the
    /// ratio <see cref="Delay"/> is entitled to read.
    /// </summary>
    public static int LiveRatio(RoadGraph graph, int arc, int[] volume)
    {
        int ratio = LiveRatioUnclamped(graph, arc, volume);
        return ratio > MaximumVolumeCapacity ? MaximumVolumeCapacity : ratio;
    }

    /// <summary>
    /// One arc's congested traversal time against a live volume column, Q16.16 Ticks, or
    /// <see cref="RoadGraph.Impassable"/>.
    /// </summary>
    /// <remarks>
    /// The body of <see cref="CarTicks"/>'s loop, reading a volume column somebody else is writing
    /// rather than one this class deposited. That is the whole of what R8 changes about the cost
    /// basis: <b>the array the router reads is the array the fleet is filling in</b>, which is the
    /// closed loop every earlier task in this spike deferred.
    /// </remarks>
    public static int LiveCarTicks(RoadGraph graph, int arc, int[] volume, int alpha)
    {
        ArgumentNullException.ThrowIfNull(graph);

        int free = graph.ArcCarTicks[arc];
        return free == RoadGraph.Impassable
            ? RoadGraph.Impassable
            : Fixed.Mul(free, Delay(LiveRatio(graph, arc, volume), alpha));
    }

    /// <summary>
    /// Deposits the demand field into a volume column, <b>accumulating</b> rather than assigning.
    /// </summary>
    /// <remarks>
    /// Accumulation is not a detail. Under <see cref="VolumeScope.PerSegment"/> two arcs share one
    /// index, and the choice between summing them and letting the later write win is the choice
    /// between modelling the scope and modelling the loop order.
    /// </remarks>
    private static void Deposit(
        RoadGraph graph, CongestionParameters parameters, Phase phase, int[] volume)
    {
        var profile = PhaseProfile.Of(phase);
        int effectiveImbalance = Fixed.Mul(parameters.Imbalance, profile.Direction);

        for (int node = 0; node < graph.Nodes; node++)
        {
            for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
            {
                if (graph.ArcCarTicks[arc] == RoadGraph.Impassable)
                {
                    continue;
                }

                int demand =
                    ArcVolumeCapacity(graph, node, arc, parameters, profile, effectiveImbalance);

                // Demand is a share of the DIRECTION's capacity — half the Segment's — whichever
                // scope the column is stored at. The field is a claim about traffic, not about
                // bookkeeping, so it must not change shape when the bookkeeping does.
                int directional =
                    IntegerMath.ShiftRight(graph.SegmentCapacity[graph.ArcSegment[arc]], 1);

                volume[graph.VolumeIndex(arc)] += Fixed.Mul(demand, directional);
            }
        }
    }

    /// <summary>
    /// The Day-average of the five phases, which is what a single-resolution matrix routes on.
    /// </summary>
    /// <remarks>
    /// <b>Averaged over the <i>cost</i>, not over the volume, and the difference is not cosmetic.</b>
    /// BPR is convex, so the delay at the mean volume is strictly less than the mean of the delays —
    /// averaging volumes first would make a Day-average matrix report a city with no rush hour in it
    /// at all rather than one whose rush hour has been smeared. Neither is right; this one is at
    /// least wrong in the direction that does not flatter the abstraction.
    /// <b>It also leaves the phase widths out of the answer</b>, because an unweighted mean of five
    /// phases is the only average available while decision 5a is open, and that is stated rather than
    /// hidden inside a weighting nobody chose.
    /// </remarks>
    public static int[] DayAverageCarTicks(RoadGraph graph, CongestionParameters parameters)
    {
        var phases = PhaseProfile.All;
        var total = new long[graph.Arcs];

        foreach (var phase in phases)
        {
            int[] ticks = CarTicks(graph, parameters, phase);
            for (int arc = 0; arc < graph.Arcs; arc++)
            {
                total[arc] = ticks[arc] == RoadGraph.Impassable ? long.MaxValue
                    : total[arc] == long.MaxValue ? long.MaxValue
                    : total[arc] + ticks[arc];
            }
        }

        var mean = new int[graph.Arcs];
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            mean[arc] = total[arc] == long.MaxValue
                ? RoadGraph.Impassable
                : (int)IntegerMath.FloorDiv(total[arc], phases.Length);
        }

        return mean;
    }

    /// <summary>
    /// BPR's delay multiplier — <c>1 + α(v/c)^β</c> at <c>β = 4</c>, so two squarings and no
    /// exponential.
    /// </summary>
    /// <remarks>
    /// <c>β</c> is fixed at 4 rather than taken from <see cref="CongestionParameters.Beta"/> because
    /// the substrate's <c>Transcendental</c> pair would introduce a tabulation error into a quantity
    /// R1 subtracts from itself to measure asymmetry — and an error that survives the subtraction
    /// would be indistinguishable from the finding. Two squarings are exact in the sense that
    /// matters: identical inputs give identical outputs, both directions, every time.
    /// </remarks>
    public static int Delay(int volumeCapacity, int alpha)
    {
        // β = 4, the BPR standard, spelled as two squarings rather than taken as a parameter.
        // The substrate's tabulated exp/log pair is what a general β would need, and it would put a
        // tabulation error inside a quantity R1 subtracts from itself to measure asymmetry — an
        // error surviving that subtraction would be indistinguishable from the finding. Two
        // squarings are exact in the sense that matters here: identical inputs give identical
        // outputs, both directions, every time.
        int squared = Fixed.Mul(volumeCapacity, volumeCapacity);
        int fourth = Fixed.Mul(squared, squared);
        return Fixed.One + Fixed.Mul(alpha, fourth);
    }

    /// <summary>
    /// One arc's <c>volume / capacity</c> under the field: monocentric in level, and leaning in the
    /// phase's direction by the swept imbalance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Inboundness is measured as what the arc does, not as where it points.</b> An arc is inbound
    /// to the degree that traversing it gets you closer to the centre — <c>d(source) − d(target)</c>
    /// over the arc's own length, which is <c>+1</c> for an arc aimed straight at the centre,
    /// <c>−1</c> for one aimed straight away, and near zero for a tangential one. A dot product would
    /// say the same thing and would need two normalisations to say it.
    /// </para>
    /// <para>
    /// <b>This is the one function whose output is not a measurement, and every asymmetry figure in
    /// R1 divides by an assumption made here.</b> Recorded plainly rather than buried: the shape of a
    /// real city's directional imbalance is not something S2 can see.
    /// </para>
    /// </remarks>
    private static int ArcVolumeCapacity(
        RoadGraph graph,
        int source,
        int arc,
        CongestionParameters parameters,
        PhaseProfile profile,
        int effectiveImbalance)
    {
        int target = graph.ArcTarget[arc];
        int centre = IntegerMath.ShiftRight(graph.Parameters.MapTiles, 1);
        int corner = IntegerGeometry.Distance(0, 0, centre, centre);

        int fromCentre = IntegerGeometry.Distance(graph.NodeX[source], graph.NodeY[source], centre, centre);
        int toCentre = IntegerGeometry.Distance(graph.NodeX[target], graph.NodeY[target], centre, centre);

        // Level: highest at the centre, tapering to zero at the corner.
        int midpointFromCentre = IntegerMath.RoundDiv(fromCentre + toCentre, 2);
        int centrality = Fixed.One - Fixed.Div(Fixed.FromInt(midpointFromCentre), Fixed.FromInt(corner));
        if (centrality < 0)
        {
            centrality = 0;
        }

        int length = graph.SegmentLengthTiles[graph.ArcSegment[arc]];
        int inboundness = length == 0
            ? 0
            : Fixed.Div(Fixed.FromInt(fromCentre - toCentre), Fixed.FromInt(length));

        if (inboundness > Fixed.One)
        {
            inboundness = Fixed.One;
        }
        else if (inboundness < -Fixed.One)
        {
            inboundness = -Fixed.One;
        }

        int level = Fixed.Mul(Fixed.Mul(parameters.BaseVolumeCapacity, centrality), profile.Intensity);
        int ratio = Fixed.Mul(level, Fixed.One + Fixed.Mul(effectiveImbalance, inboundness));

        if (ratio < 0)
        {
            ratio = 0;
        }
        else if (ratio > MaximumVolumeCapacity)
        {
            ratio = MaximumVolumeCapacity;
        }

        return ratio;
    }
}
