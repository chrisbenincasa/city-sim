using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Core.Movement;

// Fixed scratch window, never saved or hashed. Workers read the stable Road Graph;
// only the coordinator mutates world state.
internal sealed class RouteBatch
{
    internal const int TripLimit = 128;
    private readonly RoadGraph _graph;
    private readonly PreparedRoute[] _routes = new PreparedRoute[TripLimit * 3];
    private readonly WalkScratch[] _scratch;
    private readonly ParallelOptions _options;
    private readonly Action<int> _execute;
    private int _length;

    internal RouteBatch(RoadGraph graph, int workers)
    {
        _graph = graph;
        _options = new ParallelOptions { MaxDegreeOfParallelism = workers };
        _scratch = new WalkScratch[workers];
        for (int i = 0; i < workers; i++) { _scratch[i] = new WalkScratch(); }
        for (int i = 0; i < _routes.Length; i++) { _routes[i] = new PreparedRoute(); }
        _execute = Execute;
    }

    internal int Prepared { get; private set; }
    internal int Used { get; private set; }
    internal int Fallback { get; private set; }
    internal Action<int>? WorkerStarting { get; set; }
    internal Action<int>? WorkerCompleted { get; set; }

    internal void ResetReading() => Prepared = Used = Fallback = 0;

    internal void Clear()
    {
        for (int i = 0; i < _length; i++) { _routes[i].Active = false; }
        _length = 0;
    }

    internal void Add(int trip, int leg, TravelMode mode, Address from, Address to, TravelTime crossing)
    {
        int index = trip * 3 + leg;
        _routes[index].Set(_graph.Version, mode, from, to, crossing);
        if (index >= _length) { _length = index + 1; }
    }

    internal void Run()
    {
        if (_length == 0) { return; }
        Parallel.For(0, _scratch.Length, _options, _execute);
        for (int i = 0; i < _length; i++)
        { if (_routes[i].Active) { Prepared++; } }
    }

    private void Execute(int worker)
    {
        WorkerStarting?.Invoke(worker);
        WalkScratch scratch = _scratch[worker];
        // Assign whole itineraries: striding individual Legs by 3 or 6 workers would put all
        // expensive car searches on one subset and the short flanking walks on another.
        for (int first = worker * 3; first < _length; first += _scratch.Length * 3)
        {
            for (int i = first; i < first + 3 && i < _length; i++)
            { if (_routes[i].Active) { _routes[i].Search(_graph, scratch); } }
        }
        WorkerCompleted?.Invoke(worker);
    }

    internal PreparedRoute? Take(int trip, int leg, TravelMode mode, Address from, Address to, TravelTime crossing)
    {
        var route = _routes[trip * 3 + leg];
        if (route.Matches(_graph.Version, mode, from, to, crossing))
        { Used++; return route; }
        Fallback++;
        return null;
    }

    internal sealed class PreparedRoute
    {
        private uint _version;
        private TravelMode _mode;
        private Address _from, _to;
        private TravelTime _crossing;
        private int[] _arcs = [];
        private int _length;
        private bool _ready;
        internal bool Active { get; set; }
        internal TravelTime Cost { get; private set; }
        internal ReadOnlySpan<int> Arcs => _arcs.AsSpan(0, _length);

        internal void Set(uint version, TravelMode mode, Address from, Address to, TravelTime crossing)
        {
            _version = version; _mode = mode; _from = from; _to = to; _crossing = crossing;
            Active = true; _ready = false;
        }

        internal void Search(RoadGraph graph, WalkScratch scratch)
        {
            Cost = WalkRouting.Cost(graph, _mode, _from, _to, _crossing, scratch, recordPath: true);
            int length = scratch.Arrived == WalkScratch.NoNode ? 0 : scratch.ArcsTo(scratch.Arrived, []);
            _length = length > 0 ? length : 0;
            if (_arcs.Length < _length) { _arcs = new int[_length]; }
            if (_length > 0) { scratch.ArcsTo(scratch.Arrived, _arcs); }
            _ready = true;
        }

        internal bool Matches(uint version, TravelMode mode, Address from, Address to, TravelTime crossing) =>
            Active && _ready && _version == version && _mode == mode && _from == from && _to == to
            && _crossing == crossing;
    }
}
