using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Core.Movement;

public sealed partial class WalkScratch
{
    public bool UseHeuristic { get; set; } = true;
    private long[] _heapPriority = [];
    private RoadGraph? _boundGraph;
    private uint _boundVersion;
    private int _footUnit, _carUnit, _unit;
    private RoadNodeTable? _coordinates;
    private long _ax, _ay, _bx, _by, _maxDistance;
    private int _entryA, _entryB;
    private bool _directed;

    private void Direct(RoadGraph graph, TravelMode mode, int a, int b, TravelTime entryA, TravelTime entryB)
    {
        if (!UseHeuristic || (uint)a >= (uint)graph.Nodes.Rows.SlotCount
            || (uint)b >= (uint)graph.Nodes.Rows.SlotCount
            || !graph.Nodes.Rows.IsLive(a) || !graph.Nodes.Rows.IsLive(b)) { return; }
        if (!ReferenceEquals(_boundGraph, graph) || _boundVersion != graph.Version)
        {
            _footUnit = UnitBound(graph, TravelMode.Foot);
            _carUnit = UnitBound(graph, TravelMode.Car);
            _boundGraph = graph; _boundVersion = graph.Version;
        }
        _unit = mode == TravelMode.Foot ? _footUnit : _carUnit;
        if (_unit == 0) { return; }
        _coordinates = graph.Nodes;
        _ax = _coordinates.East[a].Raw; _ay = _coordinates.North[a].Raw;
        _bx = _coordinates.East[b].Raw; _by = _coordinates.North[b].Raw;
        _entryA = entryA.Raw; _entryB = entryB.Raw;
        _maxDistance = IntegerMath.FloorDiv(int.MaxValue, _unit);
        _directed = true;
        for (int i = 0; i < _heapCount; i++) { _heapPriority[i] = _heapCost[i].Raw + Heuristic(_heapNode[i]); }
        for (int i = (_heapCount >> 1) - 1; i >= 0; i--)
        { SiftDown(i, _heapPriority[i], _heapCost[i], _heapNode[i]); }
    }

    // The floor of cost / Manhattan displacement on EVERY traversable Arc is a consistent lower
    // bound, including diagonal roads, authored lengths and mode-specific speeds. Zero-cost Arcs
    // disable it so equal-cost predecessor cycles retain the original Dijkstra order.
    private static int UnitBound(RoadGraph graph, TravelMode mode)
    {
        int unit = int.MaxValue;
        var nodes = graph.Nodes; var arcs = graph.Arcs;
        for (int n = 0; n < nodes.Rows.SlotCount; n++)
        {
            if (!nodes.Rows.IsLive(n)) { continue; }
            int end = nodes.ArcStart[n] + nodes.ArcCount[n];
            for (int arc = nodes.ArcStart[n]; arc < end; arc++)
            {
                if (!arcs.Admits(arc, mode)) { continue; }
                TravelTime cost = arcs.TimeFor(arc, mode);
                if (cost.IsImpassable) { continue; }
                if (cost.Raw <= 0) { return 0; }
                int target = arcs.Target[arc];
                long distance = Abs((long)nodes.East[n].Raw - nodes.East[target].Raw)
                    + Abs((long)nodes.North[n].Raw - nodes.North[target].Raw);
                if (distance == 0) { continue; }
                int bound = (int)IntegerMath.FloorDiv(cost.Raw, distance);
                if (bound < unit) { unit = bound; }
            }
        }
        return unit == int.MaxValue ? 0 : unit;
    }

    private long Heuristic(int node)
    {
        if (!_directed) { return 0; }
        long x = _coordinates!.East[node].Raw, y = _coordinates.North[node].Raw;
        long a = Lower(Abs(x - _ax) + Abs(y - _ay), _entryA);
        long b = Lower(Abs(x - _bx) + Abs(y - _by), _entryB);
        return a < b ? a : b;
    }
    private long Lower(long distance, int entry)
    {
        if (distance > _maxDistance) { return int.MaxValue; }
        long value = distance * _unit + entry;
        return value > int.MaxValue ? int.MaxValue : value;
    }
    private static long Abs(long value) => value < 0 ? -value : value;
    private bool BetterArrival(int node, TravelTime candidate, TravelTime best) => candidate < best
        || (_directed && !candidate.IsImpassable && candidate == best && Arrived != NoNode
            && Precedes(_distance[node], node, _distance[Arrived], Arrived));
}
