using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Parking;

/// <summary>
/// The Parking Shed query — <b>the Car Parks within walking distance of an Address, nearest
/// first</b> (<c>CONTEXT.md</c> → Parking Shed, <c>adr/0009</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>A ball on the Road Graph rather than a box on the Cell grid</b>, which is the distinction
/// <see cref="Space.BuildingResidency"/> is careful to say it does <em>not</em> make. A shed is about
/// reaching, so a Car Park across an unbridged motorway is not in it however near it is in metres —
/// which is what makes <c>03 §3.7</c>'s Severance visible in parking as well as in employment.
/// </para>
/// <para>
/// <b>Bounded in <see cref="Tiles"/> and not in <c>TravelTime</c>, and that follows task 2's unit
/// rather than restating it.</b> <c>[parking] radius_metres</c> is a distance, so membership is
/// geometry: the same set of Car Parks however fast anybody walks. The two would coincide at a uniform
/// walking speed but for the <b>crossing cost</b>, which is a duration with no distance — so a
/// time-bounded ball would price a Car Park across the street at about 42 m of the radius and would
/// move the whole shed whenever <c>crossing_seconds</c> was retuned. <b>Membership is geometric and
/// the walk's price is temporal</b>; the Leg made from the answer is where the crossing is paid.
/// </para>
/// <para>
/// <b>It reports what it kept <em>and</em> what it found, and the second is not instrumentation.</b>
/// The caller supplies the span it wants filled, so one piece of code answers <em>the nearest
/// eight</em> and <em>everything in range</em> — which is what lets the storage question be settled by
/// measurement rather than by argument (<c>adr/0043</c>). ⚠ <b>The spike's <c>KeptBins = 8</c> is a
/// property of that harness and not of a shed</b>, and <c>adr/0083</c>'s witness figures were taken
/// under it; nothing here inherits the number.
/// </para>
/// </remarks>
public static class ParkingShed
{
    /// <summary>
    /// Fills <paramref name="into"/> with the nearest Car Parks to <paramref name="door"/>, nearest
    /// first, and reports how many were within <paramref name="radius"/> altogether.
    /// </summary>
    /// <param name="graph">The Road Graph the ball walks.</param>
    /// <param name="carParks">The supply.</param>
    /// <param name="residency">Which Car Parks sit on which Segment.</param>
    /// <param name="door">The Building's pedestrian Access Point.</param>
    /// <param name="radius">The shed's reach, from <c>[parking] radius_metres</c>.</param>
    /// <param name="scratch">The caller's own scratch. Never shared across threads.</param>
    /// <param name="into">Where the answer goes. Its length is how many the caller wants kept.</param>
    /// <param name="found">
    /// How many Car Parks in range the query looked at. ⚠ <b>This is a shed's size only when
    /// <paramref name="into"/> is too long to fill</b> — see <see cref="Expand"/>, which stops the ball
    /// once nothing further out could be kept, and therefore stops counting too.
    /// </param>
    /// <returns>How many slots of <paramref name="into"/> were written.</returns>
    /// <remarks>
    /// <b>Ties break on the Car Park's slot, and that is not a nicety.</b> Two Car Parks at equal
    /// distance would otherwise be ordered by whichever the ball met first, and the ball's order is a
    /// function of the adjacency's — so an accumulated shed and a rebuilt one would disagree with
    /// nothing to report it. A generated city is a grid of identical Streets with a Car Park on every
    /// Building, so most comparisons here <em>are</em> ties.
    /// <para>
    /// <b>The door's own Street is walked before the ball, and that is now load-bearing twice over.</b>
    /// It was written for correctness — a neighbour four doors down is an offset difference away, and
    /// routing through an endpoint would price it as the length of the block. It also fills the kept set
    /// before the ball starts, at the shortest distances in the whole shed, which is what gives
    /// <see cref="Expand"/>'s exit a tight bound from its first pop rather than a loose one halfway
    /// through.
    /// </para>
    /// </remarks>
    public static int Nearest(
        RoadGraph graph,
        CarParkTable carParks,
        CarParkResidency residency,
        Address door,
        Tiles radius,
        ShedScratch scratch,
        Span<int> into,
        out int found)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(carParks);
        ArgumentNullException.ThrowIfNull(residency);
        ArgumentNullException.ThrowIfNull(scratch);

        found = 0;

        RoadSegmentTable segments = graph.Segments;

        if (!door.Exists || !segments.Rows.IsLive(door.Segment))
        {
            return 0;
        }

        scratch.Begin(graph.Nodes.Rows.SlotCount, segments.Rows.SlotCount, into.Length);

        int doorSegment = door.Segment;

        // The Access Point's own Street, walked along rather than around. A neighbour four doors down
        // is |offset difference| away, and routing through an endpoint would price it as the length of
        // the block -- WalkRouting's same-Segment case, and the one a ball structurally cannot express
        // because a ball starts at nodes.
        if (residency.Any(doorSegment))
        {
            IndexListWalk own = residency.On(doorSegment, carParks);

            while (own.MoveNext())
            {
                Tiles reach = (carParks.WhereOffset[own.Current] - door.Offset).Magnitude;

                if (reach <= radius)
                {
                    found++;
                    scratch.Offer(reach, own.Current);
                }
            }
        }

        if (Endpoints(graph, doorSegment, out int nodeA, out int nodeB) && Admits(segments, doorSegment))
        {
            Tiles length = segments.LengthTiles[doorSegment];

            scratch.Seed(nodeA, door.Offset, radius);
            scratch.Seed(nodeB, length - door.Offset, radius);
        }

        Expand(graph, carParks, residency, scratch, radius, doorSegment, ref found);

        // What the ball did not finish. A Segment is priced during the ball as soon as BOTH its
        // endpoints are settled; the ones left here have an endpoint the ball never settled -- because
        // it lay outside the radius, or because the exit stopped short of it. Add() saturates on that
        // endpoint, so the Car Park is priced by the end that WAS reached, and that is not an
        // approximation: an unsettled endpoint's distance is at least the distance the ball stopped
        // at, which is past everything already kept, so the long way round could never have won.
        foreach (int segment in scratch.Touched)
        {
            if (segment == doorSegment || scratch.Done(segment) || !residency.Any(segment))
            {
                continue;
            }

            Take(graph, carParks, residency, scratch, radius, segment, ref found);
        }

        return scratch.Write(into);
    }

    /// <summary>Prices every Car Park on one Segment against both its endpoints and offers them.</summary>
    /// <remarks>
    /// Called from two places on purpose — during the ball for a Segment whose second endpoint has just
    /// settled, and after it for one whose second endpoint never will. <b>The answer does not depend on
    /// which</b>, because <see cref="ShedScratch.Offer"/> orders on <c>(distance, slot)</c> and never on
    /// arrival. That total order was written for a different reason (a rebuilt shed had to equal an
    /// accumulated one) and it is what makes an early exit expressible at all.
    /// </remarks>
    private static void Take(
        RoadGraph graph,
        CarParkTable carParks,
        CarParkResidency residency,
        ShedScratch scratch,
        Tiles radius,
        int segment,
        ref int found)
    {
        if (!Endpoints(graph, segment, out int endA, out int endB))
        {
            return;
        }

        Tiles atA = scratch.DistanceAt(endA);
        Tiles atB = scratch.DistanceAt(endB);
        Tiles span = graph.Segments.LengthTiles[segment];

        IndexListWalk walk = residency.On(segment, carParks);

        while (walk.MoveNext())
        {
            Tiles offset = carParks.WhereOffset[walk.Current];
            Tiles viaA = Add(atA, offset);
            Tiles viaB = Add(atB, span - offset);
            Tiles reach = viaA <= viaB ? viaA : viaB;

            if (reach <= radius)
            {
                found++;
                scratch.Offer(reach, walk.Current);
            }
        }

        scratch.Finish(segment);
    }

    /// <summary>Saturating addition, so an unreached endpoint stays unreached.</summary>
    private static Tiles Add(Tiles reached, Tiles along) =>
        reached == ShedScratch.Unreached ? ShedScratch.Unreached : reached + along;

    /// <summary>The ball itself: nodes in ascending distance until the radius is passed.</summary>
    /// <summary>
    /// The ball itself: nodes in ascending distance until the radius is passed, or until nothing
    /// further out could displace what is already kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The exit is what makes this query cheap, and it is a bound rather than a heuristic.</b>
    /// Dijkstra settles in ascending distance, so once the node coming off the heap is further away
    /// than the last Car Park in a <em>full</em> kept set, every Car Park the ball has not yet met is
    /// further still and none of them can be kept. Measured before it existed, the query walked
    /// <b>182.3</b> Car Parks at 1,000,000 Citizens to keep <b>8</b>, and 88% of its microseconds were
    /// in reaching the 174 it discarded.
    /// </para>
    /// <para>
    /// ⚠ <b>It changes what <c>found</c> means, and callers that want a shed's true size must not cap.</b>
    /// <c>found</c> counts the Car Parks in range that the query <em>looked at</em>, and a query that
    /// stops early looks at fewer. A caller keeping nothing or keeping more than the radius holds never
    /// fills its kept set, never satisfies <see cref="ShedScratch.KeptFull"/>, and walks the whole ball
    /// exactly as this did before — which is how <c>ParkingShedSizeTests</c> still measures a
    /// distribution rather than its own cap.
    /// </para>
    /// </remarks>
    private static void Expand(
        RoadGraph graph,
        CarParkTable carParks,
        CarParkResidency residency,
        ShedScratch scratch,
        Tiles radius,
        int doorSegment,
        ref int found)
    {
        RoadNodeTable nodes = graph.Nodes;
        RoadArcs arcs = graph.Arcs;
        RoadSegmentTable segments = graph.Segments;

        while (scratch.TryTake(out int node, out Tiles reached))
        {
            if (scratch.KeptFull && scratch.KeptWorst < reached)
            {
                return;
            }

            scratch.Settle(node);

            int start = nodes.ArcStart[node];
            int end = start + nodes.ArcCount[node];

            for (int arc = start; arc < end; arc++)
            {
                if (!arcs.Admits(arc, TravelMode.Foot))
                {
                    continue;
                }

                int segment = arcs.Segment[arc];

                // Touched whether or not the far end is in range: a Car Park part way along a Segment
                // that leaves the radius is still inside it, and dropping the Segment here would lose
                // exactly the parking at the shed's edge.
                scratch.Touch(segment);

                scratch.Seed(arcs.Target[arc], reached + segments.LengthTiles[segment], radius);

                if (segment == doorSegment || scratch.Done(segment) || !residency.Any(segment))
                {
                    continue;
                }

                // Priced now only if both ends are final. Seeding the far end above does not settle it,
                // so this reads the same state it would have read before the seed.
                if (!Endpoints(graph, segment, out int endA, out int endB)
                    || scratch.DistanceAt(endA) == ShedScratch.Unreached
                    || scratch.DistanceAt(endB) == ShedScratch.Unreached)
                {
                    continue;
                }

                Take(graph, carParks, residency, scratch, radius, segment, ref found);
            }
        }
    }

    private static bool Endpoints(RoadGraph graph, int segment, out int nodeA, out int nodeB)
    {
        RoadSegmentTable segments = graph.Segments;

        nodeA = 0;
        nodeB = 0;

        return segments.Rows.IsLive(segment)
            && graph.Nodes.Rows.TryResolve(segments.NodeA[segment], out nodeA)
            && graph.Nodes.Rows.TryResolve(segments.NodeB[segment], out nodeB);
    }

    /// <summary>Whether a Segment admits pedestrians at all.</summary>
    private static bool Admits(RoadSegmentTable segments, int slot) =>
        (segments.Modes[slot] & (byte)TravelMode.Foot) != 0;
}
