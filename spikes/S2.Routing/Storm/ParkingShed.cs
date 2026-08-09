using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Routing;

namespace S2.Routing.Storm;

/// <summary>
/// Which version witness a cached Parking Shed is revalidated against.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four rungs where routes had three, and the extra one is the steelman.</b>
/// <see cref="EpochRung"/>'s per-Segment rung asks <i>did any Segment of my route change</i>, and a
/// route's Segments are an obvious, small, ordered set. A shed has no path — <c>CONTEXT.md</c> calls
/// it <i>"the set of parking Bins within acceptable walking distance of a destination's pedestrian
/// Access Point"</i> — so <b>what "my Segments" means is a choice, and the two defensible answers
/// differ by an order of magnitude.</b> Measuring only the conservative one would have condemned the
/// rung on a definition rather than on a number.
/// </para>
/// </remarks>
internal enum ShedRung
{
    /// <summary>One counter on the whole Road Graph. Any edit anywhere invalidates every shed.</summary>
    Global,

    /// <summary>A version per cluster. Stale if any cluster the walk ball reached moved.</summary>
    PerCluster,

    /// <summary>
    /// A version per Segment, witnessed by <b>every Segment the walk ball explored</b>. Conservative
    /// and unarguably sound: a Segment inside the ball can lie on the walk to a Bin, so deleting it
    /// can change which Bins are in the shed and in what order.
    /// </summary>
    PerSegmentBall,

    /// <summary>
    /// A version per Segment, witnessed only by the Segments on the <b>walk paths to the Bins the
    /// shed actually kept</b>. The steelman: far smaller, and still sound <i>under deletion</i> for
    /// the same reason <see cref="EditStorm"/> gives for routes — removing road elsewhere can make a
    /// walk longer or impossible but never shorter, so a shed whose own walk paths are untouched is
    /// the shed the rebuild would return. <b>Unsound under addition</b>, identically to routes.
    /// </summary>
    PerSegmentPaths,
}

/// <summary>
/// The Parking Sheds of a whole city, and the reverse indices an edit storm needs to invalidate them.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the Epoch consumer that scales with Buildings rather than with routes</b>, and
/// <c>plans/0010</c> calls it the one the ladder is most likely to be decided by. <c>05 §3</c>:
/// <i>"cached Parking Shed membership per Building — (derived AND rebuilt) … invalidated by the Road
/// Graph Epoch."</i>
/// </para>
/// <para>
/// <b>Two modelling choices, both stated rather than buried.</b> A Building sits on a car-admitting
/// Segment at an evenly spaced offset — <b>five per Segment</b>, which is <c>CONTEXT.md</c>'s own
/// working figure and not this harness's invention. And <b>every Building is a Bin site</b>:
/// <c>adr/0009</c> holds Bins on Buildings <i>and</i> on Road Segments, and collapsing the two keeps
/// the Bin population proportional to the Building population without inventing a second density
/// nobody has stated. What the measurement needs is how many Bin sites fall inside a walk ball, and
/// that is governed by Building density either way.
/// </para>
/// <para>
/// <b>Nothing here draws a random number.</b> Buildings are evenly spaced and every Building is a Bin,
/// so no <c>purpose_tag</c> is consumed and no draw can correlate with the storm's.
/// </para>
/// </remarks>
internal sealed class ParkingSheds
{
    private readonly RoadGraph _graph;

    private ParkingSheds(RoadGraph graph, int buildingsPerSegment, int radiusTicks)
    {
        _graph = graph;
        BuildingsPerSegment = buildingsPerSegment;
        RadiusTicks = radiusTicks;
    }

    public int BuildingsPerSegment { get; }

    /// <summary>The walk-time bound on <i>acceptable walking distance</i>, Q16.16 Ticks.</summary>
    public int RadiusTicks { get; }

    public int Count { get; private set; }

    /// <summary>The Segment each Building fronts, and its pedestrian Access Point offset.</summary>
    public int[] BuildingSegment { get; private set; } = [];

    public int[] BuildingOffset { get; private set; } = [];

    /// <summary>Bins the shed kept, ball Segments explored, and path Segments, per Building.</summary>
    public int[] Bins { get; private set; } = [];

    public int[] BallSegments { get; private set; } = [];

    public int[] PathSegments { get; private set; } = [];

    public int[] ClustersTouched { get; private set; } = [];

    /// <summary>How many sheds were unreachable — no Bin at all inside the radius.</summary>
    public int Empty { get; private set; }

    public static ParkingSheds Place(RoadGraph graph, int buildingsPerSegment, int radiusTicks)
    {
        var sheds = new ParkingSheds(graph, buildingsPerSegment, radiusTicks);
        sheds.PlaceBuildings();
        return sheds;
    }

    /// <summary>
    /// Places <see cref="BuildingsPerSegment"/> Buildings on every Segment admitting <b>both</b> Car
    /// and Foot, at evenly
    /// spaced offsets. A Segment shorter than the spacing still gets its Buildings, stacked at the
    /// offsets its length permits — <c>CONTEXT.md</c>'s stacking case, and dropping them instead
    /// would make Building count a function of Segment length, which is a different city.
    /// <para>
    /// <b>Both modes, not just Car, and the first build had it wrong.</b> Arterials on this graph are
    /// <see cref="Modes.Car"/> only — a motorway has no pavement — so a Building placed on one has a
    /// pedestrian Access Point from which no foot arc leaves, and its shed is the five Bins on its own
    /// Segment for ever. <c>CONTEXT.md</c> gives a Building <i>one vehicle and one pedestrian Access
    /// Point</i>, so a Segment that cannot carry the second is not a Segment a Building fronts.
    /// Requiring both removes an artefact that was reading as a finding about Arterials.
    /// </para>
    /// </summary>
    private void PlaceBuildings()
    {
        var segments = new List<int>();

        for (int segment = 0; segment < _graph.Segments; segment++)
        {
            const byte Both = (byte)(Modes.Car | Modes.Foot);

            if ((_graph.SegmentModes[segment] & Both) == Both)
            {
                segments.Add(segment);
            }
        }

        Count = segments.Count * BuildingsPerSegment;
        BuildingSegment = new int[Count];
        BuildingOffset = new int[Count];

        int next = 0;

        foreach (int segment in segments)
        {
            int length = _graph.SegmentLengthTiles[segment];

            for (int i = 0; i < BuildingsPerSegment; i++)
            {
                BuildingSegment[next] = segment;
                BuildingOffset[next] =
                    IntegerMath.FloorDiv(length * (i + 1), BuildingsPerSegment + 1);
                next++;
            }
        }

        Bins = new int[Count];
        BallSegments = new int[Count];
        PathSegments = new int[Count];
        ClustersTouched = new int[Count];
    }

    public AccessPoint AccessPointOf(int building) =>
        new(BuildingSegment[building], BuildingOffset[building]);
}

/// <summary>
/// Builds one shed: a walk-bounded Dijkstra from a pedestrian Access Point, collecting the nearest
/// Bin sites and recording what the answer depended on.
/// </summary>
/// <remarks>
/// <b>The witness is the point of the whole section.</b> A route's witness is the arcs it drives, and
/// a route stores them anyway. A shed's answer depends on every Segment the walk could have crossed,
/// which the shed has no other reason to remember — so a per-Segment Epoch does not merely cost more
/// to check for a shed, it costs a <i>data structure the shed would not otherwise carry</i>. That is
/// the asymmetry <c>plans/0010</c> predicted in words and this measures.
/// </remarks>
internal sealed class ShedBuilder
{
    /// <summary>
    /// How many Bins a shed keeps. <c>CONTEXT.md</c>: arrival is <i>"a handful of lookups, never a
    /// search"</i>, and scarcity <i>widens</i> the shed rather than blocking the Trip — so the kept
    /// list has to be long enough to survive a full nearest Bin. Eight is a handful.
    /// </summary>
    public const int KeptBins = 8;

    /// <summary>
    /// <b>Deletion is carried as a Segment flag rather than through a cost array, and the reason is
    /// a defect this nearly shipped.</b> <see cref="EditStorm"/> deletes by writing
    /// <see cref="RoadGraph.Impassable"/> into a car-cost array — but
    /// <see cref="SegmentEntry.ArcCost"/> reads <c>ArcFootTicks</c> whenever the mode is
    /// <see cref="Modes.Foot"/> and <b>ignores the supplied array entirely</b>. A shed built against
    /// the storm's own array would have walked straight down bulldozed roads and reported a serene
    /// invalidation cost — which is <b>R5.5's pristine-seeding defect exactly</b>, in a second
    /// consumer, and it was found by reading the callee rather than by trusting the parameter name.
    /// </summary>
    private readonly bool[] _deleted;

    private readonly RoadGraph _graph;
    private readonly int[] _binsOnSegment;
    private readonly int[] _cost;
    private readonly int[] _stamp;
    private readonly int[] _cameFromArc;
    private readonly int[] _heapKey;
    private readonly int[] _heapNode;
    private readonly int[] _segmentStamp;
    private readonly int[] _pathStamp;

    private int _generation;
    private int _heapCount;

    public ShedBuilder(RoadGraph graph, ParkingSheds sheds)
    {
        _graph = graph;
        _deleted = new bool[graph.Segments];
        _binsOnSegment = new int[graph.Segments];

        for (int building = 0; building < sheds.Count; building++)
        {
            _binsOnSegment[sheds.BuildingSegment[building]]++;
        }

        _cost = new int[graph.Nodes];
        _stamp = new int[graph.Nodes];
        _cameFromArc = new int[graph.Nodes];
        _heapKey = new int[8_192];
        _heapNode = new int[8_192];
        _segmentStamp = new int[graph.Segments];
        _pathStamp = new int[graph.Segments];
    }

    /// <summary>The Segments the ball explored, valid until the next <see cref="Build"/>.</summary>
    public List<int> Ball { get; } = [];

    /// <summary>The Segments on walk paths to the Bins the shed kept.</summary>
    public List<int> Paths { get; } = [];

    public int BinsFound { get; private set; }

    /// <summary>Marks a gesture's Segments impassable on foot as well as by car.</summary>
    public void Delete(Gesture gesture)
    {
        foreach (int segment in gesture.Segments)
        {
            _deleted[segment] = true;
        }
    }

    public void Restore(Gesture gesture)
    {
        foreach (int segment in gesture.Segments)
        {
            _deleted[segment] = false;
        }
    }

    /// <summary>
    /// Runs the bounded walk from a pedestrian Access Point. Returns the Bins found;
    /// <see cref="Ball"/> and <see cref="Paths"/> hold the two Segment witnesses. <b>The cluster
    /// witness is derived from <see cref="Ball"/> by the caller</b>, so one build serves every
    /// cluster-size rung — which is what lets R5.6 answer the 8-versus-16 question the board files
    /// as conditional on it.
    /// </summary>
    public int Build(AccessPoint origin, int radiusTicks)
    {
        _generation++;
        _heapCount = 0;
        Ball.Clear();
        Paths.Clear();
        BinsFound = 0;

        int segment = origin.Segment;

        if (_deleted[segment])
        {
            return 0;
        }

        int length = _graph.SegmentLengthTiles[segment];
        int nodeA = _graph.SegmentNodeA[segment];
        int nodeB = _graph.SegmentNodeB[segment];

        TouchSegment(segment);
        BinsFound += _binsOnSegment[segment];

        // The Access Point's own Segment is a path witness as well as a ball witness. Deleting it
        // does not merely reorder the shed, it removes the Access Point the shed hangs off — and the
        // first build omitted it, so bulldozing the road a Building stands on left that Building's
        // shed valid. It read as a 0.00% row and 0.00% is the value this spike has learned to
        // distrust on sight.
        _pathStamp[segment] = _generation;
        Paths.Add(segment);

        Seed(
            nodeA,
            SegmentEntry.PartialCost(_graph, null, Modes.Foot, segment, nodeB, origin.OffsetTiles),
            -1);
        Seed(
            nodeB,
            SegmentEntry.PartialCost(
                _graph, null, Modes.Foot, segment, nodeA, length - origin.OffsetTiles),
            -1);

        // The node each kept Bin was reached at, so the path witness is a predecessor walk rather
        // than a second search. `covered` counts Bins rather than bin-bearing Segments: at five Bins
        // to a Segment the nearest handful is the Access Point's own Segment plus about one
        // neighbour, and capping Segments instead — which the first build did — made the path witness
        // five times the answer it is meant to describe.
        var keptAt = new List<int>(KeptBins);
        int covered = _binsOnSegment[segment];

        while (_heapCount > 0)
        {
            (int key, int node) = Pop();

            if (key > radiusTicks)
            {
                break;
            }

            if (key > _cost[node])
            {
                continue;
            }

            for (int arc = _graph.ArcStart[node]; arc < _graph.ArcStart[node + 1]; arc++)
            {
                if ((_graph.ArcModes[arc] & (byte)Modes.Foot) == 0)
                {
                    continue;
                }

                int arcSegment = _graph.ArcSegment[arc];

                if (_deleted[arcSegment])
                {
                    continue;
                }

                int step = SegmentEntry.ArcCost(_graph, null, Modes.Foot, arc);

                if (step == RoadGraph.Impassable)
                {
                    continue;
                }

                int reached = key + step;

                if (reached > radiusTicks)
                {
                    continue;
                }

                int target = _graph.ArcTarget[arc];

                if (TouchSegment(arcSegment) && _binsOnSegment[arcSegment] > 0)
                {
                    BinsFound += _binsOnSegment[arcSegment];

                    if (covered < KeptBins)
                    {
                        keptAt.Add(target);
                        covered += _binsOnSegment[arcSegment];
                    }
                }

                if (_stamp[target] != _generation || reached < _cost[target])
                {
                    Seed(target, reached, arc);
                }
            }
        }

        foreach (int node in keptAt)
        {
            WalkBack(node);
        }

        return BinsFound;
    }

    /// <summary>Walks the predecessor chain from a kept Bin's node back to the Access Point.</summary>
    private void WalkBack(int node)
    {
        int guard = 0;

        while (_stamp[node] == _generation && guard++ < 8_192)
        {
            int arc = _cameFromArc[node];

            if (arc < 0)
            {
                return;
            }

            int segment = _graph.ArcSegment[arc];

            if (_pathStamp[segment] != _generation)
            {
                _pathStamp[segment] = _generation;
                Paths.Add(segment);
            }

            int nodeA = _graph.SegmentNodeA[segment];
            node = node == nodeA ? _graph.SegmentNodeB[segment] : nodeA;
        }
    }

    private bool TouchSegment(int segment)
    {
        if (_segmentStamp[segment] == _generation)
        {
            return false;
        }

        _segmentStamp[segment] = _generation;
        Ball.Add(segment);
        return true;
    }

    private void Seed(int node, int cost, int cameFromArc)
    {
        if (cost >= SegmentEntry.Unreachable)
        {
            return;
        }

        _stamp[node] = _generation;
        _cost[node] = cost;
        _cameFromArc[node] = cameFromArc;
        Push(cost, node);
    }

    private void Push(int key, int node)
    {
        if (_heapCount == _heapKey.Length)
        {
            return;
        }

        int i = _heapCount++;
        _heapKey[i] = key;
        _heapNode[i] = node;

        while (i > 0)
        {
            int parent = (i - 1) >> 1;

            if (_heapKey[parent] <= _heapKey[i])
            {
                break;
            }

            Swap(parent, i);
            i = parent;
        }
    }

    private (int Key, int Node) Pop()
    {
        int key = _heapKey[0];
        int node = _heapNode[0];
        _heapCount--;

        if (_heapCount > 0)
        {
            _heapKey[0] = _heapKey[_heapCount];
            _heapNode[0] = _heapNode[_heapCount];
            int i = 0;

            while (true)
            {
                int left = (2 * i) + 1;
                int right = left + 1;
                int smallest = i;

                if (left < _heapCount && _heapKey[left] < _heapKey[smallest])
                {
                    smallest = left;
                }

                if (right < _heapCount && _heapKey[right] < _heapKey[smallest])
                {
                    smallest = right;
                }

                if (smallest == i)
                {
                    break;
                }

                Swap(smallest, i);
                i = smallest;
            }
        }

        return (key, node);
    }

    private void Swap(int a, int b)
    {
        (_heapKey[a], _heapKey[b]) = (_heapKey[b], _heapKey[a]);
        (_heapNode[a], _heapNode[b]) = (_heapNode[b], _heapNode[a]);
    }
}
