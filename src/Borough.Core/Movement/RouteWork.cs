using Borough.Core.Space;

namespace Borough.Core.Movement;

// Optional, caller-owned diagnostics; never simulation state. Reset between measured Ticks.
public sealed class RouteWork
{
    private Action<RoadGraph, TravelMode, Address, Address>? _searchObserver;
    public void ObserveSearchWith(Action<RoadGraph, TravelMode, Address, Address>? observer) =>
        _searchObserver = observer;
    internal void StartingSearch(RoadGraph graph, TravelMode mode, Address from, Address to) =>
        _searchObserver?.Invoke(graph, mode, from, to);
    private Action<int>? _searchFinished;
    public void ObserveFinishedSearchWith(Action<int>? observer) => _searchFinished = observer;
    internal void FinishedSearch(int settled) => _searchFinished?.Invoke(settled);
    public long Requests { get; set; }
    public long Searches { get; set; }
    public long Settled { get; set; }
    public long Pops { get; set; }
    public long Pushes { get; set; }
    public void Reset() { Requests = Searches = Settled = Pops = Pushes = 0; }
}
