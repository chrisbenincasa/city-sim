using Borough.Core.Movement;
using Borough.Core.Space;

namespace Borough.Headless;

// A bounded opportunity count, not a route cache. Reset at every measured Tick.
internal sealed class RouteReuseProbe
{
    internal const int Limit = 65536;
    private readonly Dictionary<Key, byte> _seen = new(Limit);
    internal long Searches { get; private set; }
    internal long Repeats { get; private set; }
    internal long Untracked { get; private set; }

    internal void Observe(RoadGraph graph, TravelMode mode, Address from, Address to)
    {
        Searches++;
        var key = new Key(graph, graph.Version, mode, from, to);
        if (_seen.ContainsKey(key)) { Repeats++; }
        else if (_seen.Count < Limit) { _seen.Add(key, 0); }
        else { Untracked++; }
    }

    internal void Reset()
    {
        _seen.Clear();
        Searches = Repeats = Untracked = 0;
    }

    internal object Reading() => new { Searches, Repeats, Untracked, Unique = _seen.Count, Limit };
    private readonly record struct Key(RoadGraph Graph, uint Version, TravelMode Mode, Address From, Address To);
}
