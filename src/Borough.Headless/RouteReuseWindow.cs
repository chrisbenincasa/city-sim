using Borough.Core.Movement;
using Borough.Core.Space;

namespace Borough.Headless;

// Opportunity only: no results are cached and measured Step timings include this probe's overhead.
internal sealed class RouteReuseWindow(int capacity)
{
    private readonly Dictionary<Key, byte> _seen = new(capacity);
    private readonly Key[] _fifo = new Key[capacity];
    private int _next;
    private bool _hit;
    internal long Searches { get; private set; }
    internal long Hits { get; private set; }
    internal long Settled { get; private set; }
    internal long HitSettled { get; private set; }
    internal long Evictions { get; private set; }
    internal void Observe(RoadGraph graph, TravelMode mode, Address from, Address to)
    {
        var key = new Key(graph, graph.Version, mode, from, to);
        Searches++;
        _hit = _seen.ContainsKey(key);
        if (_hit) { Hits++; return; }
        if (_seen.Count == capacity) { _seen.Remove(_fifo[_next]); Evictions++; }
        _seen.Add(key, 0);
        _fifo[_next] = key;
        _next = (_next + 1) % capacity;
    }
    internal void Finish(int settled)
    {
        Settled += settled;
        if (_hit) { HitSettled += settled; }
    }
    internal object Reading() => new { Capacity = capacity, Entries = _seen.Count,
        Searches, Hits, Settled, HitSettled, Evictions };
    private readonly record struct Key(RoadGraph Graph, uint Version, TravelMode Mode, Address From, Address To);
}

internal sealed class RouteReuseWindows
{
    private readonly RouteReuseWindow[] _windows = [new(65536), new(1048576)];
    internal void Observe(RoadGraph graph, TravelMode mode, Address from, Address to)
    { foreach (var window in _windows) { window.Observe(graph, mode, from, to); } }
    internal void Finish(int settled)
    { foreach (var window in _windows) { window.Finish(settled); } }
    internal object Reading() => _windows.Select(w => w.Reading()).ToArray();
}
