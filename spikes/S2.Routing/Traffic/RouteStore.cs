using Borough.Core.Arithmetic;
using S2.Routing.Graph;
using S2.Routing.Matrix;
using S2.Routing.Routing;

namespace S2.Routing.Traffic;

/// <summary>
/// A flat store of routes as <b>arc</b> sequences, CSR by route id.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two of R2's three path-source rungs are this structure with different contents</b>, which is
/// why they share one: the <i>shared</i> rung stores one route per ordered District pair, and the
/// <i>searched</i> rung stores one per Trip. Giving them a common shape is what makes the per-crossing
/// timings comparable — a difference between the two rungs is then a difference in how the store was
/// filled and how large it grew, never a difference in how it is read.
/// </para>
/// <para>
/// <b>Ordered pairs, never unordered.</b> <c>plans/0010</c>'s gate section: <i>"the route cache key is
/// an ordered node pair. Under R6's key a cache treating <c>{u,v}</c> as unordered returns the wrong
/// route for half its hits, silently."</i> The store therefore holds <c>n²</c> routes, not
/// <c>n(n−1)/2</c>, and nothing here may be halved by symmetry.
/// </para>
/// </remarks>
internal sealed class RouteStore
{
    private readonly int[] _start;
    private readonly int[] _arc;

    private RouteStore(int[] start, int[] arc)
    {
        _start = start;
        _arc = arc;
    }

    /// <summary>Routes held.</summary>
    public int Count => _start.Length - 1;

    /// <summary>Arcs held across every route.</summary>
    public int Arcs => _arc.Length;

    /// <summary>Arc storage plus the offset array. The figure <c>adr/0006</c> is checked against.</summary>
    public long ResidentBytes => ((long)_arc.Length + _start.Length) * sizeof(int);

    /// <summary>Mean arcs per non-empty route, which is also mean Segments per route.</summary>
    public int MeanLength
    {
        get
        {
            int nonEmpty = 0;
            for (int route = 0; route < Count; route++)
            {
                if (Length(route) > 0)
                {
                    nonEmpty++;
                }
            }

            return nonEmpty == 0 ? 0 : IntegerMath.FloorDiv(_arc.Length, nonEmpty);
        }
    }

    public int Length(int route) => _start[route + 1] - _start[route];

    public int ArcAt(int route, int step) => _arc[_start[route] + step];

    /// <summary>The whole store's arcs, for a smear that walks routes rather than reading them one by one.</summary>
    public ReadOnlySpan<int> Span(int route) => _arc.AsSpan(_start[route], Length(route));

    /// <summary>
    /// One route per ordered District pair, from <c>n</c> forward one-to-all searches — the store
    /// <c>03 §3.3</c> assumed and <c>adr/0041</c> declined to attribute volume along.
    /// </summary>
    /// <remarks>
    /// A forward search rooted at District <i>i</i>'s representative yields every route
    /// <c>i → j</c> at once, so the build is <c>n</c> searches and not <c>n²</c>. The same property
    /// R1 relied on for the matrix, and for the same reason.
    /// </remarks>
    public static RouteStore ForDistrictPairs(
        RoadGraph graph, Districts districts, int[] arcTicks, OneToAll search)
    {
        int routes = districts.Count * districts.Count;
        var start = new int[routes + 1];
        var arcs = new List<int>(routes * 32);
        var buffer = new int[graph.Segments];

        for (int from = 0; from < districts.Count; from++)
        {
            int source = districts.Representative[from];
            if (source >= 0)
            {
                search.Run(source, arcTicks);
            }

            for (int to = 0; to < districts.Count; to++)
            {
                int route = (from * districts.Count) + to;
                start[route] = arcs.Count;

                int target = districts.Representative[to];
                if (source < 0 || target < 0 || search.CostOf(target) >= OneToAll.Unreachable)
                {
                    continue;
                }

                int length = search.PathArcs(target, buffer);
                for (int step = 0; step < length; step++)
                {
                    arcs.Add(buffer[step]);
                }
            }
        }

        start[routes] = arcs.Count;
        return new RouteStore(start, [.. arcs]);
    }

    /// <summary>
    /// A pool of routes searched per Trip, over real <c>(Segment, offset)</c> Access Points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A pool rather than a search per spawn, and the reason is itself the rung's headline
    /// result.</b> The fleet respawns a Traveller whenever one arrives, which at the working figures
    /// is order 400 drive Legs per simulated Tick; at R0's denominator that is ~170 ms of searching
    /// per Tick against a 15.6 ms Tick budget, so a harness that searched on every spawn would spend
    /// its entire run demonstrating a conclusion available from one multiplication. <b>The searched
    /// rung is priced by timing one search and multiplying</b>, and the fleet draws from a pool so
    /// that the per-crossing and lag measurements still run over genuinely per-Trip routes.
    /// </para>
    /// <para>
    /// <b>What the pool does not flatter.</b> It reuses each searched route across many Travellers,
    /// which understates the searched rung's resident size — so R2.1 reports that size from the
    /// arithmetic (in-flight × mean route length) rather than from the pool's own footprint, and says
    /// which figure is which.
    /// </para>
    /// </remarks>
    public static RouteStore ForSearchedPool(
        RoadGraph graph,
        OdSampler sampler,
        int[] congestedCarTicks,
        int count,
        ulong seed,
        out int found)
    {
        var search = new PointToPoint(graph, congestedCarTicks);
        var start = new int[count + 1];
        var arcs = new List<int>(count * 48);

        found = 0;

        for (int query = 0; query < count; query++)
        {
            start[query] = arcs.Count;

            var origin = sampler.Origin(seed, (ulong)query, Modes.Car);
            var goal = sampler.Destination(seed, (ulong)query, Modes.Car, origin, radiusTiles: 0);

            search.Bootstrap(origin, goal, Modes.Car, HeuristicKind.Chebyshev);
            var outcome = search.Expand();
            if (!outcome.Found)
            {
                continue;
            }

            int length = search.PathArcs(arcs);
            if (length > 0)
            {
                found++;
            }
        }

        start[count] = arcs.Count;
        return new RouteStore(start, [.. arcs]);
    }

    /// <summary>
    /// The same pool, drawn from a named <b>O-D rung</b> rather than from the sampler's uniform draw,
    /// and reporting each route's whole-journey cost alongside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R4.1 is why this overload exists.</b> The draw is a swept family and not a setting, and
    /// <c>plans/0000</c> forbids quoting an S2 figure without naming its rung — so a store built by a
    /// sampler that only knows the uniform draw cannot be swept, and R6.4 sweeps everything it prints.
    /// The body is <see cref="ForSearchedPool(RoadGraph, OdSampler, int[], int, ulong, out int)"/>
    /// unchanged apart from where the pair comes from.
    /// </para>
    /// <para>
    /// <b>The cost column is not decoration.</b> R6.4.3's ground truth is *which stored routes a full
    /// recompute would change*, and a route's arcs cannot answer that on their own: two routes of
    /// equal cost may differ in arcs, which is R5.5.3's correction. Recording the whole-journey cost
    /// at build time — remainders included, exactly as the search returned it — is what lets the
    /// re-search after the addition be compared on cost rather than on arc identity.
    /// </para>
    /// </remarks>
    public static RouteStore ForSearchedPool(
        RoadGraph graph,
        OdPair[] pool,
        int[] congestedCarTicks,
        out int found,
        out int[] costTicks)
    {
        var search = new PointToPoint(graph, congestedCarTicks);
        var start = new int[pool.Length + 1];
        var arcs = new List<int>(pool.Length * 48);

        costTicks = new int[pool.Length];
        found = 0;

        for (int query = 0; query < pool.Length; query++)
        {
            start[query] = arcs.Count;
            costTicks[query] = OneToAll.Unreachable;

            search.Bootstrap(pool[query].Origin, pool[query].Destination, Modes.Car, HeuristicKind.Chebyshev);
            var outcome = search.Expand();
            if (!outcome.Found)
            {
                continue;
            }

            int length = search.PathArcs(arcs);
            if (length > 0)
            {
                costTicks[query] = outcome.CostTicks;
                found++;
            }
        }

        start[pool.Length] = arcs.Count;
        return new RouteStore(start, [.. arcs]);
    }
}
