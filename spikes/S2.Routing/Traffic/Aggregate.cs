using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Traffic;

/// <summary>
/// The attribution scheme <c>adr/0041</c> rejected, implemented so R2 can price what rejecting it
/// cost: <c>in_flight[origin_District][dest_District]</c> counts, redistributed along cached
/// District-pair routes once per congestion cycle.
/// </summary>
/// <remarks>
/// <para>
/// <b>R2 no longer chooses between the schemes and must not be read as reopening the choice.</b>
/// <c>adr/0041</c> settled it on three correctness grounds and recorded which way that cut against
/// convenience — aggregate is the cheaper option and the one already written down in <c>03 §3.3</c>.
/// What survives is <c>plans/0010</c> R2a's crossover <i>"as a measurement of what this costs, not of
/// whether to do it"</i>.
/// </para>
/// <para>
/// <b>The smear conserves vehicles, and the naive version does not.</b> Adding the whole pair count to
/// every Segment on the route would put one Traveller on fifty Segments at once and break the
/// invariant <c>adr/0041</c> names — <i>"summed Segment volume equals the number of in-flight
/// vehicular Travellers, every Tick."</i> A Traveller on a route of total time <c>T</c> spends
/// <c>t_s</c> of it on Segment <c>s</c>, so its expected occupancy there is <c>t_s / T</c> and those
/// shares sum to one. <b>This is the strongest form of the scheme available</b>, which matters: a
/// rejected alternative implemented weakly makes the price of rejecting it look smaller than it is.
/// </para>
/// <para>
/// <b>Time-weighted, not length-weighted, and the difference is the Arterial.</b> A route's Arterial
/// leg is long in Tiles and short in Ticks, so a length-weighted smear would park vehicles on exactly
/// the Segments they cross fastest — inverting the reading on the roads the design cares most about.
/// </para>
/// </remarks>
internal static class Aggregate
{
    /// <summary>
    /// Rebuilds <paramref name="volume"/> from the District-pair counts. One congestion cycle's work.
    /// </summary>
    /// <returns>Arc writes performed, which is the cost figure R2a's surface is built from.</returns>
    public static long Smear(
        RoadGraph graph, RouteStore routes, int[] inFlight, int[] arcTicks, int[] volume)
    {
        // Zeroed rather than accumulated: the scheme *re-derives* volume each cycle, and the clear is
        // part of its cost. At 33,018 Segments it is noise, and counting it anyway is what keeps the
        // comparison against direct attribution honest at the small-cycle end of the sweep, which is
        // precisely where the crossover lives.
        Array.Clear(volume);

        long writes = 0;

        for (int pair = 0; pair < inFlight.Length; pair++)
        {
            int count = inFlight[pair];
            if (count == 0 || pair >= routes.Count)
            {
                continue;
            }

            var route = routes.Span(pair);
            if (route.Length == 0)
            {
                continue;
            }

            long total = 0;
            for (int step = 0; step < route.Length; step++)
            {
                int ticks = arcTicks[route[step]];
                total += ticks == RoadGraph.Impassable ? 0 : ticks;
            }

            for (int step = 0; step < route.Length; step++)
            {
                int arc = route[step];
                int ticks = arcTicks[arc] == RoadGraph.Impassable ? 0 : arcTicks[arc];

                long share = total <= 0
                    ? IntegerMath.FloorDiv((long)count * Fixed.One, route.Length)
                    : IntegerMath.FloorDiv((long)count * Fixed.One * ticks, total);

                volume[graph.VolumeIndex(arc)] += (int)share;
                writes++;
            }
        }

        return writes;
    }

    /// <summary>
    /// The <c>volume / capacity</c> a scheme's volume column reports for an arc, Q16.16.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Capacity is a flow and volume is a count, so the two need reconciling before they may be
    /// divided.</b> <c>CONTEXT.md</c> → Segment carries capacity as vehicles per unit time;
    /// <c>adr/0041</c> makes volume <i>"a count of Travellers present"</i>. Vehicles that can be
    /// present is the flow times how long a vehicle stays — capacity × free-flow traversal time — and
    /// that product is what this divides by. Stating it matters because R2b's whole result is a
    /// threshold crossing, and a threshold on a ratio whose units were never reconciled is a threshold
    /// on nothing.
    /// </para>
    /// <para>
    /// <b>Free-flow traversal time, never the congested one.</b> A congested denominator grows as the
    /// Segment jams — more vehicles fit because each one stays longer — which is physically true and
    /// makes the ratio self-damping, so a jam would partly hide itself in the very reading built to
    /// detect it.
    /// </para>
    /// </remarks>
    public static int Ratio(RoadGraph graph, int arc, int[] volume)
    {
        int segment = graph.ArcSegment[arc];
        int free = graph.ArcCarTicks[arc];
        if (free == RoadGraph.Impassable || free <= 0)
        {
            return 0;
        }

        int flow = graph.SegmentCapacity[segment];
        if (graph.Parameters.VolumeScope == VolumeScope.PerDirection)
        {
            flow = IntegerMath.ShiftRight(flow, 1);
        }

        int capacity = Fixed.Mul(flow, free);
        return capacity <= 0 ? 0 : Fixed.Div(volume[graph.VolumeIndex(arc)], capacity);
    }
}
