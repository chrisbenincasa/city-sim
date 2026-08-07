using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Cluster;

/// <summary>
/// HPA\*'s abstract graph over the cluster grid: portals, the arcs that cross between clusters, and
/// the confined optimal costs between the portals of one cluster.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim under test is <c>adr/0014</c>'s</b> — that the Road Graph <i>"arrives
/// pre-partitioned, because the Chunk grid is already the pathfinding cluster, which is most of what
/// HPA\* wants handed to it"</i>. <c>adr/0040</c> already corrected the identity half of that claim.
/// What survives is that a regular tiling exists for free, and this class is where the rest of the
/// preprocessing — the part that is <i>not</i> free — gets priced.
/// </para>
/// <para>
/// <b>How many crossings become transitions is a parameter, and R3 sweeps it.</b> Botea's HPA\*
/// groups contiguous boundary tiles into entrances and keeps one or two transitions per entrance,
/// because its input is a tile grid whose boundary is a solid run of hundreds of walkable cells. A
/// road network's boundary is already sparse — a cluster edge is crossed only where a Street or an
/// Arterial actually crosses it — so the grouping step has nothing to group, and keeping every
/// crossing makes the abstraction <b>complete</b>: it can then answer every query at the flat
/// optimum. Completeness is not free, and the sweep is what prices it. Keeping fewer transitions is
/// the same lever Botea pulls, and it buys a smaller, sparser abstract graph at the cost of a
/// detour.
/// </para>
/// <para>
/// <b>The abstract graph is <c>(derived AND rebuilt)</c></b> (<c>adr/0040</c>), which is what makes
/// cluster size free to change forever and is the whole reason R3 may decide it outright.
/// </para>
/// </remarks>
internal sealed class AbstractGraph
{
    public const int Unreachable = ClusterSearch.Unreachable;

    private readonly RoadGraph _graph;
    private readonly Clusters _clusters;
    private readonly int[] _arcCost;
    private readonly ClusterSearch _search;
    private readonly List<int> _repairArena = [];
    private readonly bool[] _kept;
    private readonly bool _reduceIntra;
    private readonly bool _storePaths;

    private AbstractGraph(
        RoadGraph graph,
        Clusters clusters,
        int[] arcCost,
        ClusterSearch search,
        int[] portalOfNode,
        int[] portalNode,
        int[] portalStart,
        int[] edgeStart,
        int[] edgeTarget,
        int[] edgeCost,
        int[] edgeArc,
        int[] edgePathStart,
        int[] edgePathLength,
        int[][] pathArena,
        bool[] kept,
        bool reduceIntra,
        bool storePaths)
    {
        _kept = kept;
        _reduceIntra = reduceIntra;
        _storePaths = storePaths;
        _graph = graph;
        _clusters = clusters;
        _arcCost = arcCost;
        _search = search;
        PortalOfNode = portalOfNode;
        PortalNode = portalNode;
        PortalStart = portalStart;
        EdgeStart = edgeStart;
        EdgeTarget = edgeTarget;
        EdgeCost = edgeCost;
        EdgeArc = edgeArc;
        EdgePathStart = edgePathStart;
        EdgePathLength = edgePathLength;
        PathArena = pathArena;
    }

    /// <summary>Node index → portal id, or <c>-1</c> if the node is interior to its cluster.</summary>
    public int[] PortalOfNode { get; }

    /// <summary>Portal id → node index. Ids are grouped by cluster and ascending within it.</summary>
    public int[] PortalNode { get; }

    /// <summary>CSR offsets: cluster → its range of portal ids.</summary>
    public int[] PortalStart { get; }

    /// <summary>CSR offsets: portal id → its range of abstract edges.</summary>
    public int[] EdgeStart { get; }

    /// <summary>Abstract edge → the portal it leads to.</summary>
    public int[] EdgeTarget { get; private set; }

    /// <summary>Abstract edge → its cost, Q16.16 Ticks.</summary>
    public int[] EdgeCost { get; private set; }

    /// <summary>
    /// Abstract edge → the concrete arc it <i>is</i>, for an inter-cluster edge; <c>-1</c> for an
    /// intra-cluster edge, which stands for a path rather than an arc.
    /// </summary>
    public int[] EdgeArc { get; private set; }

    /// <summary>Where an intra-cluster edge's arcs begin in its own cluster's arena.</summary>
    public int[] EdgePathStart { get; private set; }

    /// <summary>How many arcs an intra-cluster edge's stored path has, or <c>0</c> if none is stored.</summary>
    public int[] EdgePathLength { get; private set; }

    /// <summary>
    /// Per cluster, the arcs of its intra-cluster edges laid end to end, or <c>null</c> if this
    /// abstract graph stores costs only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An intra-cluster edge stores a cost, and a cost is not a route.</b> Recovering the arcs
    /// without this means re-running the confined search that found them, once per edge on the
    /// abstract path — which R3 measured as **2.5× more expensive at 16 Chunks than at 8**, and
    /// which was the entire reason the smaller cluster won the only column with a customer.
    /// </para>
    /// <para>
    /// <b>It is not an <c>adr/0006</c> hazard, and the distinction is the one <c>plans/0010</c> R6
    /// draws.</b> R6's dangerous cache is keyed by origin-destination pair and grows with play. This
    /// is bounded by the partition — one entry per abstract edge, no eviction policy to write — and
    /// it is <c>(derived AND rebuilt)</c> alongside the costs it sits next to.
    /// </para>
    /// <para>
    /// <b>Per cluster rather than one flat array</b>, because a repair rewrites one cluster's paths
    /// and the new ones need not be the same length as the old.
    /// </para>
    /// </remarks>
    public int[][] PathArena { get; }

    /// <summary>Whether this abstract graph can hand back arcs without searching for them.</summary>
    public bool HasPaths => PathArena.Length > 0 && PathArena[0] is not null;

    public int Portals => PortalNode.Length;

    public int Edges => EdgeTarget.Length;

    /// <summary>Nodes settled while building or repairing. The preprocessing work figure.</summary>
    public long NodesSettled { get; private set; }

    /// <summary>
    /// What the abstract graph costs in memory. <b>The reverse index is not counted here</b> — it is
    /// a property of the Road Graph and is constant across the sweep, so folding it in would put a
    /// constant into a column whose whole purpose is to vary with cluster size.
    /// </summary>
    public long ResidentBytes
    {
        get
        {
            long words = (long)PortalOfNode.Length + PortalNode.Length + PortalStart.Length
                + EdgeStart.Length + EdgeTarget.Length + EdgeCost.Length + EdgeArc.Length;

            if (HasPaths)
            {
                words += EdgePathStart.Length + EdgePathLength.Length;
                foreach (int[] cluster in PathArena)
                {
                    words += cluster?.Length ?? 0;
                }
            }

            return words * sizeof(int);
        }
    }

    /// <summary>The path store alone, so R3 can price it apart from the graph it rides on.</summary>
    public long PathBytes
    {
        get
        {
            if (!HasPaths)
            {
                return 0;
            }

            long words = (long)EdgePathStart.Length + EdgePathLength.Length;
            foreach (int[] cluster in PathArena)
            {
                words += cluster?.Length ?? 0;
            }

            return words * sizeof(int);
        }
    }

    public ReadOnlySpan<int> PortalsIn(int cluster) =>
        PortalNode.AsSpan(PortalStart[cluster], PortalStart[cluster + 1] - PortalStart[cluster]);

    public int FirstPortalOf(int cluster) => PortalStart[cluster];

    public int PortalCountOf(int cluster) => PortalStart[cluster + 1] - PortalStart[cluster];

    /// <param name="transitionsPerBoundary">
    /// How many crossings of each cluster-pair boundary become transitions, or <c>0</c> for all of
    /// them — which makes the abstraction complete.
    /// </param>
    public static AbstractGraph Build(
        RoadGraph graph,
        Clusters clusters,
        ReverseArcs reverse,
        int[] arcCost,
        int transitionsPerBoundary = 0,
        bool reduceIntra = false,
        bool storePaths = false)
    {
        bool[] kept = KeptCrossings(graph, clusters, reverse, arcCost, transitionsPerBoundary);

        var portalOfNode = new int[graph.Nodes];
        Array.Fill(portalOfNode, -1);

        // A node is a portal if any passable arc incident to it crosses a cluster boundary, in
        // either direction. Taking only outgoing arcs would miss the far end of a one-way crossing,
        // and the edge into it would then have no node to land on.
        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            if (arcCost[arc] == RoadGraph.Impassable || !kept[graph.ArcSegment[arc]])
            {
                continue;
            }

            int from = reverse.Source[arc];
            int to = graph.ArcTarget[arc];

            if (clusters.OfNode[from] != clusters.OfNode[to])
            {
                portalOfNode[from] = 0;
                portalOfNode[to] = 0;
            }
        }

        // Ids grouped by cluster and ascending within it, so a partition is the same abstract graph
        // forever and two rungs differ only where the partition differs.
        var portalStart = new int[clusters.Count + 1];
        int count = 0;

        for (int cluster = 0; cluster < clusters.Count; cluster++)
        {
            portalStart[cluster] = count;
            foreach (int node in clusters.NodesIn(cluster))
            {
                if (portalOfNode[node] == 0)
                {
                    portalOfNode[node] = count++;
                }
            }
        }

        portalStart[clusters.Count] = count;

        var portalNode = new int[count];
        for (int node = 0; node < graph.Nodes; node++)
        {
            if (portalOfNode[node] >= 0)
            {
                portalNode[portalOfNode[node]] = node;
            }
        }

        var search = new ClusterSearch(graph, clusters, reverse, arcCost);
        var edgeStart = new int[count + 1];
        var target = new List<int>();
        var cost = new List<int>();
        var edgeArc = new List<int>();
        var pathStart = new List<int>();
        var pathLength = new List<int>();
        var arena = new List<int>();
        var arenas = new int[clusters.Count][];
        long settled = 0;
        var confined = new int[0];

        for (int cluster = 0; cluster < clusters.Count; cluster++)
        {
            int first = portalStart[cluster];
            int n = portalStart[cluster + 1] - first;

            if (confined.Length < n * n)
            {
                confined = new int[n * n];
            }

            // Every portal's confined search first, then the edges — because the reduction below
            // needs the cluster's whole distance table and cannot be decided one row at a time.
            for (int i = 0; i < n; i++)
            {
                search.Begin(cluster, -1, backward: false);
                search.Seed(portalNode[first + i], 0);
                search.Run();
                settled += search.Expanded;

                for (int j = 0; j < n; j++)
                {
                    confined[(i * n) + j] = search.CostOf(portalNode[first + j]);
                }
            }

            // Second pass, and the searches are re-run rather than kept. An intra-edge's *path* is
            // only recoverable from the search that found it, and which edges survive the reduction
            // is only knowable once the whole table exists — so either the predecessor arrays of
            // every portal are held at once, or the searches run twice. Twice is cheaper than
            // holding n predecessor arrays over a cluster, and it is honest about what the path
            // store costs to build.
            for (int i = 0; i < n; i++)
            {
                int portal = first + i;
                edgeStart[portal] = target.Count;
                int node = portalNode[portal];

                // Inter-cluster: the crossing arc itself, at its own cost. Its path is the arc, so
                // it needs no entry in the arena.
                for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
                {
                    int to = graph.ArcTarget[arc];
                    if (arcCost[arc] == RoadGraph.Impassable || clusters.OfNode[to] == cluster
                        || !kept[graph.ArcSegment[arc]] || portalOfNode[to] < 0)
                    {
                        continue;
                    }

                    target.Add(portalOfNode[to]);
                    cost.Add(arcCost[arc]);
                    edgeArc.Add(arc);
                    pathStart.Add(0);
                    pathLength.Add(0);
                }

                bool searched = false;

                // Intra-cluster: the confined optimal cost to every other portal of this cluster,
                // and — if a path store was asked for — the arcs that realise it.
                for (int j = 0; j < n; j++)
                {
                    int reachable = confined[(i * n) + j];
                    if (i == j || reachable >= Unreachable)
                    {
                        continue;
                    }

                    if (reduceIntra && Redundant(confined, n, i, j, reachable))
                    {
                        continue;
                    }

                    target.Add(first + j);
                    cost.Add(reachable);
                    edgeArc.Add(-1);

                    if (!storePaths)
                    {
                        pathStart.Add(0);
                        pathLength.Add(0);
                        continue;
                    }

                    if (!searched)
                    {
                        search.Begin(cluster, -1, backward: false);
                        search.Seed(node, 0);
                        search.Run();
                        settled += search.Expanded;
                        searched = true;
                    }

                    pathStart.Add(arena.Count);
                    pathLength.Add(Trace(search, portalNode[first + j], arena));
                }
            }

            if (storePaths)
            {
                arenas[cluster] = [.. arena];
                arena.Clear();
            }
        }

        edgeStart[count] = target.Count;

        return new AbstractGraph(
            graph, clusters, arcCost, search,
            portalOfNode, portalNode, portalStart,
            edgeStart, [.. target], [.. cost], [.. edgeArc],
            [.. pathStart], [.. pathLength], arenas,
            kept, reduceIntra, storePaths)
        {
            NodesSettled = settled,
        };
    }

    /// <summary>
    /// Appends the arcs the last search reached <paramref name="node"/> by, in travel order, and
    /// returns how many. The arena is per cluster, so a start offset is local to it.
    /// </summary>
    private static int Trace(ClusterSearch search, int node, List<int> arena)
    {
        int before = arena.Count;
        int at = node;

        while (search.CameFromArc(at) >= 0)
        {
            int arc = search.CameFromArc(at);
            arena.Add(arc);
            at = search.Previous(arc);
        }

        arena.Reverse(before, arena.Count - before);
        return arena.Count - before;
    }

    /// <summary>
    /// Whether an intra-cluster edge is redundant — some other portal of the same cluster lies on a
    /// route between its ends that costs no more.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a lossless reduction and the optimality column is what proves it.</b> Every arc
    /// cost is strictly positive, so both hops of a replacement are strictly cheaper than the edge
    /// they replace; removals therefore cannot cascade into a route that no longer exists, and the
    /// abstract graph still answers every query at the flat optimum. R3.4's *Optimal* column is the
    /// check, and it is the reason that column is worth keeping after it has read 100% once.
    /// </para>
    /// <para>
    /// <b>What it costs is repairability, and that is the finding rather than the caveat.</b>
    /// Redundancy is a property of the <i>costs</i>, so an edit that lengthens one route can make a
    /// removed edge necessary again — and <see cref="Repair"/> can only re-cost the slots the build
    /// left it, never re-add one. A reduced abstract graph is therefore rebuilt rather than
    /// repaired, which is exactly the trade R5's edit storm is about.
    /// </para>
    /// </remarks>
    private static bool Redundant(int[] confined, int n, int i, int j, int direct)
    {
        for (int k = 0; k < n; k++)
        {
            if (k == i || k == j)
            {
                continue;
            }

            int left = confined[(i * n) + k];
            int right = confined[(k * n) + j];

            if (left < Unreachable && right < Unreachable && left + right <= direct)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Which crossing Segments become transitions.
    /// </summary>
    /// <remarks>
    /// <b>Grouped by the <i>unordered</i> cluster pair, and sorted rather than hashed.</b> Unordered
    /// because keeping the A→B direction of one Street and the B→A direction of another would make
    /// the abstraction asymmetric for a reason nothing in the city corresponds to. Sorted because
    /// <c>BOR0301</c> is loaded in this project and is right to be: a partition selected by walking
    /// a hash map's buckets would be a different partition on a different runtime, and every figure
    /// taken against it would move with it.
    /// </remarks>
    private static bool[] KeptCrossings(
        RoadGraph graph, Clusters clusters, ReverseArcs reverse, int[] arcCost, int perBoundary)
    {
        var kept = new bool[graph.Segments];

        if (perBoundary <= 0)
        {
            Array.Fill(kept, true);
            return kept;
        }

        var crossings = new List<(long Boundary, int Segment)>();
        var seen = new bool[graph.Segments];

        for (int arc = 0; arc < graph.Arcs; arc++)
        {
            int segment = graph.ArcSegment[arc];
            if (arcCost[arc] == RoadGraph.Impassable || seen[segment])
            {
                continue;
            }

            int from = clusters.OfNode[reverse.Source[arc]];
            int to = clusters.OfNode[graph.ArcTarget[arc]];

            if (from == to)
            {
                continue;
            }

            seen[segment] = true;
            long low = from < to ? from : to;
            long high = from < to ? to : from;
            crossings.Add(((low * clusters.Count) + high, segment));
        }

        crossings.Sort(static (a, b) =>
            a.Boundary != b.Boundary ? a.Boundary.CompareTo(b.Boundary) : a.Segment.CompareTo(b.Segment));

        int start = 0;
        while (start < crossings.Count)
        {
            int end = start;
            while (end < crossings.Count && crossings[end].Boundary == crossings[start].Boundary)
            {
                end++;
            }

            // Evenly spaced along the boundary rather than the first n, so a sampled boundary keeps
            // crossings at both ends of it. A boundary crossed fewer times than the budget keeps all
            // of them.
            int run = end - start;
            int take = run < perBoundary ? run : perBoundary;

            for (int i = 0; i < take; i++)
            {
                kept[crossings[start + IntegerMath.FloorDiv(i * run, take)].Segment] = true;
            }

            start = end;
        }

        return kept;
    }


    /// <summary>
    /// Recomputes one cluster's edges from scratch — costs, redundancy and paths — and splices the
    /// result back into the abstract graph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what an edit costs on a reduced abstract graph, and R3 measures it rather than
    /// deriving it.</b> <see cref="Repair"/> only re-costs the slots the build left it, which is
    /// sound while every intra-edge still exists — but reduction removes edges whose redundancy is a
    /// property of the <i>costs</i>, so an edit can make a removed edge necessary again and no
    /// amount of re-costing will bring it back. The cluster's edge set has to be decided again.
    /// </para>
    /// <para>
    /// <b>The claim this corrects is R3's own.</b> An earlier draft said a reduced abstract graph
    /// must be *rebuilt* — meaning all of it. Only the touched cluster's edge set must be, and the
    /// splice that puts it back is a copy of an array measured in tens of kilobytes.
    /// </para>
    /// </remarks>
    public void RebuildCluster(int cluster)
    {
        int first = PortalStart[cluster];
        int n = PortalStart[cluster + 1] - first;

        if (n == 0)
        {
            return;
        }

        var confined = new int[n * n];

        for (int i = 0; i < n; i++)
        {
            _search.Begin(cluster, -1, backward: false);
            _search.Seed(PortalNode[first + i], 0);
            _search.Run();
            NodesSettled += _search.Expanded;

            for (int j = 0; j < n; j++)
            {
                confined[(i * n) + j] = _search.CostOf(PortalNode[first + j]);
            }
        }

        var target = new List<int>();
        var cost = new List<int>();
        var arc = new List<int>();
        var pathStart = new List<int>();
        var pathLength = new List<int>();
        var arena = _repairArena;
        var starts = new int[n];
        arena.Clear();

        for (int i = 0; i < n; i++)
        {
            starts[i] = target.Count;
            int node = PortalNode[first + i];

            for (int a = _graph.ArcStart[node]; a < _graph.ArcStart[node + 1]; a++)
            {
                int to = _graph.ArcTarget[a];
                if (_arcCost[a] == RoadGraph.Impassable || _clusters.OfNode[to] == cluster
                    || !_kept[_graph.ArcSegment[a]] || PortalOfNode[to] < 0)
                {
                    continue;
                }

                target.Add(PortalOfNode[to]);
                cost.Add(_arcCost[a]);
                arc.Add(a);
                pathStart.Add(0);
                pathLength.Add(0);
            }

            bool searched = false;

            for (int j = 0; j < n; j++)
            {
                int reachable = confined[(i * n) + j];
                if (i == j || reachable >= Unreachable)
                {
                    continue;
                }

                if (_reduceIntra && Redundant(confined, n, i, j, reachable))
                {
                    continue;
                }

                target.Add(first + j);
                cost.Add(reachable);
                arc.Add(-1);

                if (!_storePaths)
                {
                    pathStart.Add(0);
                    pathLength.Add(0);
                    continue;
                }

                if (!searched)
                {
                    _search.Begin(cluster, -1, backward: false);
                    _search.Seed(node, 0);
                    _search.Run();
                    NodesSettled += _search.Expanded;
                    searched = true;
                }

                pathStart.Add(arena.Count);
                pathLength.Add(Trace(_search, PortalNode[first + j], arena));
            }
        }

        if (_storePaths)
        {
            PathArena[cluster] = [.. arena];
        }

        Splice(cluster, first, n, starts, target, cost, arc, pathStart, pathLength);
    }

    /// <summary>Puts a rebuilt cluster's edges back, shifting every portal after it.</summary>
    private void Splice(
        int cluster,
        int first,
        int n,
        int[] starts,
        List<int> target,
        List<int> cost,
        List<int> arc,
        List<int> pathStart,
        List<int> pathLength)
    {
        int from = EdgeStart[first];
        int to = EdgeStart[first + n];
        int delta = target.Count - (to - from);

        if (delta != 0)
        {
            EdgeTarget = Rewrite(EdgeTarget, from, to, target);
            EdgeCost = Rewrite(EdgeCost, from, to, cost);
            EdgeArc = Rewrite(EdgeArc, from, to, arc);
            EdgePathStart = Rewrite(EdgePathStart, from, to, pathStart);
            EdgePathLength = Rewrite(EdgePathLength, from, to, pathLength);

            for (int portal = first + n; portal < EdgeStart.Length; portal++)
            {
                EdgeStart[portal] += delta;
            }
        }
        else
        {
            for (int i = 0; i < target.Count; i++)
            {
                EdgeTarget[from + i] = target[i];
                EdgeCost[from + i] = cost[i];
                EdgeArc[from + i] = arc[i];
                EdgePathStart[from + i] = pathStart[i];
                EdgePathLength[from + i] = pathLength[i];
            }
        }

        for (int i = 0; i < n; i++)
        {
            EdgeStart[first + i] = from + starts[i];
        }
    }

    private static int[] Rewrite(int[] original, int from, int to, List<int> replacement)
    {
        var rebuilt = new int[original.Length - (to - from) + replacement.Count];
        Array.Copy(original, 0, rebuilt, 0, from);
        replacement.CopyTo(rebuilt, from);
        Array.Copy(original, to, rebuilt, from + replacement.Count, original.Length - to);
        return rebuilt;
    }

    /// <summary>Rebuilds every cluster an edited Segment touches. The measured edit cost.</summary>
    public void RebuildFor(int segment)
    {
        int clusterA = _clusters.OfNode[_graph.SegmentNodeA[segment]];
        int clusterB = _clusters.OfNode[_graph.SegmentNodeB[segment]];

        RebuildCluster(clusterA);
        if (clusterB != clusterA)
        {
            RebuildCluster(clusterB);
        }
    }

    /// <summary>
    /// Re-decides every cluster touched by a whole gesture, each one <b>once</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R5's addition, and the reason it is a method here rather than a loop in the harness.</b>
    /// R3 and R4 measured single edits, where <see cref="RebuildFor"/> is the whole story. A drag
    /// deletes hundreds of Segments and a contiguous drag deletes most of them inside the same few
    /// clusters, so calling <see cref="RebuildFor"/> per Segment re-decides one cluster's edge set
    /// dozens of times over and publishes the repetition as the cost of the gesture.
    /// </para>
    /// <para>
    /// The coalescing belongs with the structure rather than with the measurement because it is a
    /// statement about the invariant: a cluster's edge set is a function of its arcs, so the number
    /// of Segments deleted inside it does not change how many times it has to be decided. <b>The
    /// per-Segment loop is retained in R5 as a rung</b> — the naive spelling is what a first
    /// implementation would do, and the gap between the two is a finding rather than a detail to be
    /// silently optimised away.
    /// </para>
    /// </remarks>
    /// <returns>Clusters re-decided, which is the work the gesture actually implies.</returns>
    public int RebuildForAll(ReadOnlySpan<int> segments, bool[] touched, List<int> scratch)
    {
        scratch.Clear();

        foreach (int segment in segments)
        {
            Mark(_clusters.OfNode[_graph.SegmentNodeA[segment]], touched, scratch);
            Mark(_clusters.OfNode[_graph.SegmentNodeB[segment]], touched, scratch);
        }

        foreach (int cluster in scratch)
        {
            touched[cluster] = false;
            RebuildCluster(cluster);
        }

        return scratch.Count;

        static void Mark(int cluster, bool[] touched, List<int> scratch)
        {
            if (touched[cluster])
            {
                return;
            }

            touched[cluster] = true;
            scratch.Add(cluster);
        }
    }

    /// <summary>
    /// Repairs the abstract graph after one Segment's costs changed, in place.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Repair, not rebuild</b> — <c>plans/0010</c> R3 asks for <i>"invalidation cost on a single
    /// edit: the abstract graph's repair, not a rebuild"</i>. Only the clusters holding the edited
    /// Segment's endpoints can have changed, so only their portals' searches are re-run and only
    /// their edges are rewritten.
    /// </para>
    /// <para>
    /// <b>What this cannot do, stated rather than discovered later.</b> The edge slots are fixed at
    /// build, so a repair may re-cost an edge and may cost it out of existence, but it cannot create
    /// a portal that did not exist — which is what <i>drawing</i> a road across a cluster boundary
    /// does. R3 therefore prices the deletion half of the core verb exactly and the drawing half not
    /// at all; the drawing half is R5's, where the edit storm lives.
    /// </para>
    /// </remarks>
    public void Repair(int segment)
    {
        int clusterA = _clusters.OfNode[_graph.SegmentNodeA[segment]];
        int clusterB = _clusters.OfNode[_graph.SegmentNodeB[segment]];

        RepairCluster(clusterA);
        if (clusterB != clusterA)
        {
            RepairCluster(clusterB);
        }
    }

    /// <summary>Clusters a repair of this Segment would touch. The blast radius, for the report.</summary>
    public int RepairSpan(int segment)
    {
        int clusterA = _clusters.OfNode[_graph.SegmentNodeA[segment]];
        int clusterB = _clusters.OfNode[_graph.SegmentNodeB[segment]];
        return clusterA == clusterB ? 1 : 2;
    }

    private void RepairCluster(int cluster)
    {
        // The arena is rebuilt wholesale for this cluster rather than patched, because a repaired
        // path need not be the length of the one it replaces. That is why the arena is per cluster.
        List<int>? arena = HasPaths ? _repairArena : null;
        arena?.Clear();

        for (int portal = PortalStart[cluster]; portal < PortalStart[cluster + 1]; portal++)
        {
            int node = PortalNode[portal];

            _search.Begin(cluster, -1, backward: false);
            _search.Seed(node, 0);
            _search.Run();
            NodesSettled += _search.Expanded;

            for (int edge = EdgeStart[portal]; edge < EdgeStart[portal + 1]; edge++)
            {
                if (EdgeArc[edge] >= 0)
                {
                    int arc = _arcCost[EdgeArc[edge]];
                    EdgeCost[edge] = arc == RoadGraph.Impassable ? Unreachable : arc;
                    continue;
                }

                int reached = _search.CostOf(PortalNode[EdgeTarget[edge]]);
                EdgeCost[edge] = reached;

                if (arena is null)
                {
                    continue;
                }

                EdgePathStart[edge] = arena.Count;
                EdgePathLength[edge] = reached >= Unreachable
                    ? 0
                    : Trace(_search, PortalNode[EdgeTarget[edge]], arena);
            }
        }

        if (arena is not null)
        {
            PathArena[cluster] = [.. arena];
        }
    }
}
