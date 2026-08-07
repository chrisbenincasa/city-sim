using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Traffic;

namespace S2.Routing.Loop;

/// <summary>
/// R8.1 — the distance from an arrival to the next node at which the driver has a <b>real choice</b>.
/// No traffic, no fleet, no cost basis: this is a property of the Road Graph alone.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because <c>adr/0046</c> names one routing parameter whose lower bound is derivable
/// rather than tuned</b>, and says it should therefore be derived: <i>"A Traveller decides at a node,
/// so seeing one Segment ahead is actionable in principle… What breaks is that not every node is a
/// decision."</i> A Traveller looking <c>N</c> Segments ahead from a node whose next choice is
/// <c>N + 2</c> Segments away receives a signal it is structurally unable to act on.
/// </para>
/// <para>
/// <b>The quantity is defined on arrivals, not on nodes, and that is forced by the definition rather
/// than chosen.</b> <c>plans/0010</c> says <i>"out-degree ≥ 2 counting only arcs that are not the
/// reverse of the one used to arrive"</i> — so whether a node is a choice depends on the arc the
/// driver arrived by, and a node has no answer independent of one. A four-way crossroads reached
/// along a Street is a choice; the same node reached along its only other car-passable arc, in a
/// graph where the rest are foot-only, is not. The state space is therefore the car-passable arc set,
/// one state per <c>(node, arc arrived by)</c>, and <see cref="NodeSegments"/> projects it back onto
/// nodes by taking the <b>worst</b> arrival, because a floor derived from the best arrival is not a
/// floor.
/// </para>
/// <para>
/// <b>This is the graph's answer and not the driver's, and the two are different numbers.</b> The
/// unweighted distribution over arrivals weights a cul-de-sac nobody uses exactly as heavily as the
/// arterial ramp the whole city crosses. What a driver experiences is the distribution weighted by
/// where drivers actually are, which needs traffic — so R8.3 reports the crossing-weighted version
/// and this reports the structural one. Naming both is this spike's standing lesson about
/// denominators arriving for the fourth time; publishing either alone is the mistake.
/// </para>
/// </remarks>
internal sealed class Horizon
{
    private Horizon(
        bool[] carArc,
        int[] reverseArc,
        int[] segments,
        int[] ticks,
        int[] nodeSegments,
        int states,
        int unreachable,
        int deadEnds)
    {
        CarArc = carArc;
        ReverseArc = reverseArc;
        Segments = segments;
        Ticks = ticks;
        NodeSegments = nodeSegments;
        States = states;
        Unreachable = unreachable;
        DeadEnds = deadEnds;
    }

    /// <summary>Whether an arc admits cars at all. Non-car arcs are not arrival states.</summary>
    public bool[] CarArc { get; }

    /// <summary>Arc index → the arc back down the same Segment, or <c>-1</c> where the Segment is one-way.</summary>
    public int[] ReverseArc { get; }

    /// <summary>Per arrival state, Segments to the next real choice. <c>int.MaxValue</c> where there is none.</summary>
    public int[] Segments { get; }

    /// <summary>Per arrival state, free-flow car Ticks to the next real choice, Q16.16.</summary>
    public int[] Ticks { get; }

    /// <summary>Per node, the <b>worst</b> arrival's Segment distance. <c>int.MaxValue</c> where no car arc arrives.</summary>
    public int[] NodeSegments { get; }

    /// <summary>Car-passable arcs, which is the number of arrival states.</summary>
    public int States { get; }

    /// <summary>States from which no real choice is reachable at all. Expected small, reported anyway.</summary>
    public int Unreachable { get; }

    /// <summary>States whose node offers no onward car arc but the one arrived by. A forced U-turn.</summary>
    public int DeadEnds { get; }

    /// <summary>
    /// Measures the graph. One multi-source relaxation per metric, backwards from every state that
    /// is already a choice.
    /// </summary>
    public static Horizon Of(RoadGraph graph, ReverseArcs reverse)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(reverse);

        var carArc = new bool[graph.Arcs];
        var reverseArc = new int[graph.Arcs];
        var carOut = new int[graph.Nodes];
        int states = 0;

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            carArc[arc] = graph.ArcCarTicks[arc] != RoadGraph.Impassable;
            if (carArc[arc])
            {
                states++;
                carOut[reverse.Source[arc]]++;
            }
        }

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            reverseArc[arc] = -1;
            int at = graph.ArcTarget[arc];

            for (int back = graph.ArcStart[at]; back < graph.ArcStart[at + 1]; back++)
            {
                // One Segment carries at most one arc in each direction, so sharing a Segment with
                // the arc arrived by *is* being its reverse. Comparing endpoints instead would also
                // catch a parallel Segment between the same two nodes, which is a different thing —
                // a genuine alternative that happens to end up in the same place.
                if (graph.ArcSegment[back] == graph.ArcSegment[arc])
                {
                    reverseArc[arc] = back;
                    break;
                }
            }
        }

        // A state is already a choice when the node it arrives at offers at least two onward car arcs
        // once the way back is discounted. adr/0046's degree-2 mid-block node fails this: one arc
        // out, and it is the one you came in on.
        var actionable = new bool[graph.Arcs];
        int deadEnds = 0;

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (!carArc[arc])
            {
                continue;
            }

            int at = graph.ArcTarget[arc];
            int back = reverseArc[arc];
            int onward = carOut[at] - (back >= 0 && carArc[back] ? 1 : 0);

            actionable[arc] = onward >= 2;
            if (onward <= 0)
            {
                deadEnds++;
            }
        }

        int[] segments = Relax(graph, reverse, carArc, actionable, weights: null);
        int[] ticks = Relax(graph, reverse, carArc, actionable, graph.ArcCarTicks);

        var nodeSegments = new int[graph.Nodes];
        Array.Fill(nodeSegments, -1);

        int unreachable = 0;

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (!carArc[arc])
            {
                continue;
            }

            if (segments[arc] == int.MaxValue)
            {
                unreachable++;
            }

            int at = graph.ArcTarget[arc];
            if (nodeSegments[at] < 0 || segments[arc] > nodeSegments[at])
            {
                nodeSegments[at] = segments[arc];
            }
        }

        for (int node = 0; node < graph.Nodes; node++)
        {
            if (nodeSegments[node] < 0)
            {
                nodeSegments[node] = int.MaxValue;
            }
        }

        return new Horizon(
            carArc, reverseArc, segments, ticks, nodeSegments, states, unreachable, deadEnds);
    }

    /// <summary>
    /// Multi-source shortest path over arrival states, relaxed <b>backwards</b>: every state that is
    /// already a choice starts at zero and the answer propagates against the direction of travel.
    /// </summary>
    /// <remarks>
    /// Backwards, because forwards would be one search per node and there are 16,697 of them. The
    /// transition is the same either way and it is the one place the U-turn rule enters: from a
    /// settled state <c>b</c> leaving node <c>u</c>, the predecessors are the states arriving at
    /// <c>u</c> along a <i>different</i> Segment.
    /// </remarks>
    private static int[] Relax(
        RoadGraph graph, ReverseArcs reverse, bool[] carArc, bool[] actionable, int[]? weights)
    {
        var distance = new int[graph.Arcs];
        Array.Fill(distance, int.MaxValue);

        var heap = new int[4096];
        var heapArc = new int[4096];
        int count = 0;

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (carArc[arc] && actionable[arc])
            {
                distance[arc] = 0;
                Push(ref heap, ref heapArc, ref count, 0, arc);
            }
        }

        var closed = new bool[graph.Arcs];

        while (count > 0)
        {
            (int key, int settled) = Pop(heap, heapArc, ref count);

            if (closed[settled])
            {
                continue;
            }

            closed[settled] = true;

            int step = weights is null ? 1 : weights[settled];
            if (step == RoadGraph.Impassable)
            {
                continue;
            }

            int at = reverse.Source[settled];
            int segment = graph.ArcSegment[settled];

            for (int slot = reverse.Start[at]; slot < reverse.Start[at + 1]; slot++)
            {
                int into = reverse.Arc[slot];

                if (!carArc[into] || closed[into] || graph.ArcSegment[into] == segment)
                {
                    continue;
                }

                int candidate = key + step;
                if (candidate >= distance[into])
                {
                    continue;
                }

                distance[into] = candidate;
                Push(ref heap, ref heapArc, ref count, candidate, into);
            }
        }

        return distance;
    }

    private static void Push(ref int[] heap, ref int[] heapArc, ref int count, int key, int arc)
    {
        if (count == heap.Length)
        {
            Array.Resize(ref heap, heap.Length * 2);
            Array.Resize(ref heapArc, heapArc.Length * 2);
        }

        int i = count++;
        heap[i] = key;
        heapArc[i] = arc;

        while (i > 0)
        {
            int parent = IntegerMath.ShiftRight(i - 1, 1);
            if (heap[parent] <= heap[i])
            {
                break;
            }

            (heap[parent], heap[i]) = (heap[i], heap[parent]);
            (heapArc[parent], heapArc[i]) = (heapArc[i], heapArc[parent]);
            i = parent;
        }
    }

    private static (int Key, int Arc) Pop(int[] heap, int[] heapArc, ref int count)
    {
        int key = heap[0];
        int arc = heapArc[0];

        count--;
        heap[0] = heap[count];
        heapArc[0] = heapArc[count];

        int i = 0;
        while (true)
        {
            int left = IntegerMath.ShiftLeft(i, 1) + 1;
            if (left >= count)
            {
                break;
            }

            int right = left + 1;
            int smaller = right < count && heap[right] < heap[left] ? right : left;

            if (heap[i] <= heap[smaller])
            {
                break;
            }

            (heap[i], heap[smaller]) = (heap[smaller], heap[i]);
            (heapArc[i], heapArc[smaller]) = (heapArc[smaller], heapArc[i]);
            i = smaller;
        }

        return (key, arc);
    }
}
