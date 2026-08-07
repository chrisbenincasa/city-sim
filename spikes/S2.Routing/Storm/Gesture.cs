using S2.Routing.Graph;

namespace S2.Routing.Storm;

/// <summary>How the Segments in one edit gesture are arranged on the map.</summary>
internal enum GestureShape
{
    /// <summary>
    /// A contiguous run, followed along the road network from a starting Segment in a heading.
    /// <b>The case R3 deferred cluster size to R5 for</b> — the player drags a bulldozer.
    /// </summary>
    Drag,

    /// <summary>
    /// The same number of Segments drawn uniformly over the whole map.
    /// <b>A control, and it is not optional.</b> A contiguous drag touches few clusters and a
    /// scattered set touches many, so the per-cluster rung of the Epoch ladder is flattered by the
    /// gesture model itself. Reporting only the drag would publish a property of this generator as a
    /// property of the design. This is the corpus's own rule about an instrument that cannot move,
    /// applied before rather than after: a rung expected to be favourable is paired with one
    /// expected not to be.
    /// </summary>
    Scattered,

    /// <summary>
    /// A contiguous run drawn from <b>Arterial</b> Segments only — the fast-road case.
    /// <b>It exists because the addition measurement needs a plausible best case.</b> Every Street on
    /// this graph is the same speed, so restoring a deleted Street mostly restores connectivity and
    /// improves a route by a little. An Arterial is 90 km/h against a Street's 50, so a restored
    /// Arterial run is the shortcut a player actually draws when they draw something that matters —
    /// and it is the addition most able to invalidate a cached route it never touches.
    /// </summary>
    Arterial,
}

/// <summary>One edit gesture: the Segments a single player action deletes.</summary>
/// <param name="Shape">Drag or scattered.</param>
/// <param name="Segments">The Segments deleted, distinct, all admitting cars.</param>
/// <param name="Requested">
/// How many were asked for. <b>Reported beside <c>Segments.Length</c> rather than assumed equal</b>:
/// a drag runs out of unvisited road and stops short, and a sample that shrinks with the swept axis
/// is how this spike has three times manufactured a trend out of survivorship.
/// </param>
internal sealed record Gesture(GestureShape Shape, int[] Segments, int Requested)
{
    public string Name => Shape switch
    {
        GestureShape.Drag => "drag",
        GestureShape.Scattered => "scattered",
        _ => "arterial",
    };
}

/// <summary>
/// The edit storm: gestures drawn over the Road Graph, applied to a shadow cost array and reverted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deletion only, and the limit is inherited rather than chosen.</b> R3 records why: drawing a
/// road across a cluster boundary creates a portal, which needs a slot the abstract graph's build
/// did not reserve. That is an addition to the partition rather than a repair of it, and it is a
/// separate measurement. What is measured here is the half of the core verb every candidate
/// structure has an in-place answer for.
/// </para>
/// <para>
/// <b>Deletion-only is also what makes the per-Segment Epoch rung sound, and that is a finding
/// rather than a convenience.</b> A cached route is invalidated under that rung when one of its own
/// Segments changes version. Under deletion this is exact: removing road elsewhere can make a route
/// invalid or slower but never better, so a route whose own Segments are untouched is still the
/// route the search would return. <b>Under addition it is unsound</b> — a new road can shorten a
/// cached route without touching a single Segment the route uses, and the cache would serve a stale
/// answer forever with no version to notice it. The rung's exactness is therefore a property of the
/// verb being measured, and R5 must say so beside the column rather than let *exact* stand
/// unqualified.
/// </para>
/// </remarks>
internal sealed class EditStorm
{
    private readonly RoadGraph _graph;
    private readonly SegmentArcs _segmentArcs;
    private readonly int[] _carSegments;
    private readonly int[] _arterialSegments;
    private readonly bool[] _arterial;
    private readonly bool[] _taken;

    public EditStorm(RoadGraph graph, SegmentArcs segmentArcs)
    {
        _graph = graph;
        _segmentArcs = segmentArcs;
        _taken = new bool[graph.Segments];
        _arterial = new bool[graph.Segments];

        var car = new List<int>();
        var arterial = new List<int>();

        for (int segment = 0; segment < graph.Segments; segment++)
        {
            if ((graph.SegmentModes[segment] & (byte)Modes.Car) == 0)
            {
                continue;
            }

            car.Add(segment);

            if (graph.SegmentFreeFlow[segment] > Units.StreetFreeFlow)
            {
                arterial.Add(segment);
                _arterial[segment] = true;
            }
        }

        _carSegments = [.. car];
        _arterialSegments = [.. arterial];
    }

    /// <summary>Segments admitting cars — the pool every gesture draws from.</summary>
    public int CarSegments => _carSegments.Length;

    /// <summary>Segments faster than a Street. The Arterial gesture's pool.</summary>
    public int ArterialSegments => _arterialSegments.Length;

    public Gesture Draw(ulong seed, ulong index, GestureShape shape, int count) => shape switch
    {
        GestureShape.Drag => Drag(seed, index, count, _carSegments, arterialOnly: false),
        GestureShape.Arterial => Drag(seed, index, count, _arterialSegments, arterialOnly: true),
        _ => Scattered(seed, index, count),
    };

    /// <summary>
    /// Deletes a gesture's Segments in <paramref name="arcCost"/>, returning the arcs changed.
    /// </summary>
    public int Apply(Gesture gesture, int[] arcCost)
    {
        int changed = 0;

        foreach (int segment in gesture.Segments)
        {
            foreach (int arc in _segmentArcs.For(segment))
            {
                arcCost[arc] = RoadGraph.Impassable;
                changed++;
            }
        }

        return changed;
    }

    /// <summary>Restores a gesture's Segments, so every rung starts from the same graph.</summary>
    public void Revert(Gesture gesture, int[] arcCost)
    {
        foreach (int segment in gesture.Segments)
        {
            foreach (int arc in _segmentArcs.For(segment))
            {
                arcCost[arc] = _graph.ArcCarTicks[arc];
            }
        }
    }

    private Gesture Scattered(ulong seed, ulong index, int count)
    {
        var taken = new List<int>(count);

        for (int attempt = 0; attempt < count * 8 && taken.Count < count; attempt++)
        {
            ulong roll = CounterHash.Of(
                seed, index, (ulong)attempt, CounterHash.Purpose.GestureOrigin);
            int segment = _carSegments[CounterHash.Below(roll, _carSegments.Length)];

            if (_taken[segment])
            {
                continue;
            }

            _taken[segment] = true;
            taken.Add(segment);
        }

        foreach (int segment in taken)
        {
            _taken[segment] = false;
        }

        return new Gesture(GestureShape.Scattered, [.. taken], count);
    }

    /// <summary>
    /// Follows the road network from a drawn Segment, preferring to continue straight.
    /// </summary>
    /// <remarks>
    /// <b>Straightest-continuation rather than a random walk</b>, because a random walk on a grid
    /// doubles back and produces a blob, and a blob is not the gesture. A player dragging along a
    /// street deletes a <i>run</i>; the whole reason the shape matters is that a run is long in one
    /// axis and therefore crosses cluster boundaries at a rate a blob of the same area does not.
    /// </remarks>
    private Gesture Drag(ulong seed, ulong index, int count, int[] pool, bool arterialOnly)
    {
        if (pool.Length == 0)
        {
            return new Gesture(
                arterialOnly ? GestureShape.Arterial : GestureShape.Drag, [], count);
        }

        ulong roll = CounterHash.Of(seed, index, 0, CounterHash.Purpose.GestureOrigin);
        int first = pool[CounterHash.Below(roll, pool.Length)];

        var taken = new List<int>(count) { first };
        _taken[first] = true;

        int node = _graph.SegmentNodeB[first];
        int headingX = _graph.NodeX[node] - _graph.NodeX[_graph.SegmentNodeA[first]];
        int headingY = _graph.NodeY[node] - _graph.NodeY[_graph.SegmentNodeA[first]];

        while (taken.Count < count)
        {
            int bestArc = -1;
            long bestAlignment = long.MinValue;

            for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
            {
                if ((_graph.ArcModes[arc] & (byte)Modes.Car) == 0 || _taken[_graph.ArcSegment[arc]])
                {
                    continue;
                }

                if (arterialOnly && !_arterial[_graph.ArcSegment[arc]])
                {
                    continue;
                }

                int target = _graph.ArcTarget[arc];
                long dx = _graph.NodeX[target] - _graph.NodeX[node];
                long dy = _graph.NodeY[target] - _graph.NodeY[node];
                long alignment = (dx * headingX) + (dy * headingY);

                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestArc = arc;
                }
            }

            if (bestArc < 0)
            {
                // The drag ran into ground it has already deleted. Stopping short is reported
                // rather than papered over with a fresh start elsewhere, which would silently turn
                // one gesture into several and flatter every locality column below.
                break;
            }

            int next = _graph.ArcTarget[bestArc];
            headingX = _graph.NodeX[next] - _graph.NodeX[node];
            headingY = _graph.NodeY[next] - _graph.NodeY[node];
            node = next;

            _taken[_graph.ArcSegment[bestArc]] = true;
            taken.Add(_graph.ArcSegment[bestArc]);
        }

        foreach (int segment in taken)
        {
            _taken[segment] = false;
        }

        return new Gesture(
            arterialOnly ? GestureShape.Arterial : GestureShape.Drag, [.. taken], count);
    }
}
