using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Free-flow travel time between every ordered pair of routing partitions — <b>the lookup that
/// exists so a question about travel time stops costing a search</b> (<c>adr/0047</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>It holds times and nothing else, and that is what makes it affordable.</b> S2 R1 measured a
/// version storing routes beside the times at <b>4.06 GiB</b> for 4,096 zones against a 172.3 MiB
/// world — the binding constraint on how fine the partition could be. <c>adr/0047</c> removed it by
/// serving actual routes from the route cache, leaving this holding one <see cref="TravelTime"/> per
/// ordered pair: about <b>8.3 MB</b> for the ~1,444 partitions a million-Citizen city occupies.
/// ***The thing that capped matrix granularity is the thing the decision removes.***
/// </para>
/// <para>
/// <b>Asymmetric, and nothing may halve it by symmetry.</b> <c>CONTEXT.md</c> → Arc states the
/// reason and lists this as one of three things that follow: the volume-delay function is evaluated
/// on one Segment's own volume over capacity and Lanes are directional queues, so
/// <c>cost(A→B) ≠ cost(B→A)</c>. It is <c>n²</c> entries and not <c>n²/2</c>, permanently, and a
/// future optimisation that exploits symmetry is a change to the city rather than to the storage.
/// </para>
/// <para>
/// <b>An unreachable pair is <see cref="TravelTime.Impassable"/> — and ⚠ that is <em>not</em> a
/// certainty a consumer may act on, which 5c task 2 established after nearly building a reject on
/// it.</b> A one-to-all search settles every node its mode subgraph can reach, so two partitions in
/// different components leave an Impassable entry. But the search starts at an <em>access node</em>:
/// a partition holding two pieces that do not connect to each other anchors on one of them, and a
/// journey starting in the other may succeed where the entry says nothing can. ***A structure laid
/// over a graph cannot answer a question about the graph.*** The sound reachability test is
/// <see cref="Space.RoadNodeTable.FootComponent"/>, which milestone 5a has computed since it shipped
/// — see <see cref="WalkRouting"/>. What this entry is good for is <em>ordering</em> and for a
/// diagnosis a human reads, never for refusing something on its own authority.
/// </para>
/// <para>
/// <b>Every other entry is an estimate, and the error is measured rather than derived</b> — mean
/// ≈ 0, p90 about +4 minutes and a worst overstatement of <b>9.2 minutes</b> against a real walk at
/// <see cref="RoutingPartition.DesignEdge"/>, flat across city size because it is a partition-local
/// quantity. <see cref="EntryError"/> gives the geometric scale and is deliberately not a bound; the
/// two differ by half again, which is the whole reason the ratifier had to be a measurement.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, not a <c>[Table]</c>, and it never joins <c>World._tables</c></b>
/// — <see cref="RoutingPartition"/>'s argument and <c>plans/0020</c>'s reason, unchanged.
/// </para>
/// <para>
/// <b>Refreshed against a graph-wide version rather than a per-Segment Epoch, and the usual warning
/// does not apply here.</b> <c>CONTEXT.md</c> → Epoch says a single counter <em>is</em> a global
/// flush, because a route cannot tell whether an edit touched it. A matrix has no such question to
/// ask: every entry is a shortest path over the whole graph, so any edit anywhere can move any
/// entry, and a global counter is the *correct* granularity rather than the lazy one. What the
/// per-Segment Epoch buys is the route cache's, which is task 4.
/// </para>
/// <para>
/// <b>Freshened explicitly by its consumer rather than lazily inside a query.</b> A rebuild needs a
/// <see cref="WalkScratch"/>, which is one per caller and never shared across threads — so a lazy
/// rebuild triggered inside a parallel Tick phase would have to invent one. <see cref="EnsureFresh"/>
/// is called at the top of the pass that reads it, where the caller owns its scratch and the phase is
/// sequential.
/// </para>
/// </remarks>
public sealed class TravelTimeMatrix
{
    private TravelTime[] _entries = [];
    private int _order;
    private uint _builtAt;
    private TravelMode _builtFor = TravelMode.None;

    /// <summary>How many one-to-all searches the last rebuild ran. One per partition.</summary>
    public int Searches { get; private set; }

    /// <summary>How many nodes the last rebuild settled, across every search. The rebuild's price.</summary>
    public long Settled { get; private set; }

    /// <summary>Partitions on a side of the matrix. Zero before the first rebuild.</summary>
    public int Order => _order;

    /// <summary>The mode the entries were computed in.</summary>
    public TravelMode Mode => _builtFor;

    /// <summary>
    /// Rebuilds the matrix if the graph has moved since it was last built, or if the mode has
    /// changed.
    /// </summary>
    /// <remarks>
    /// <b>A version comparison rather than a dirty flag, because the graph is edited by things that
    /// do not know this exists.</b> <see cref="RoadGraph.LayStreet"/> and its bulldoze twin bump the
    /// version through <see cref="RoadGraph.RebuildDerived"/>; a flag would have to be set by every
    /// one of them, which is the coverage hole a version number cannot have.
    /// </remarks>
    /// <returns>Whether a rebuild ran.</returns>
    public bool EnsureFresh(RoadGraph graph, TravelMode mode, WalkScratch scratch)
    {
        ArgumentNullException.ThrowIfNull(graph);

        if (_builtFor == mode && _builtAt == graph.Version && _order == graph.Partition.Count)
        {
            return false;
        }

        Rebuild(graph, mode, scratch);

        return true;
    }

    /// <summary>
    /// Recomputes every entry: one one-to-all search per partition, read off at every other
    /// partition's access node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One search fills a whole row, which is the only reason this is affordable at all.</b> A
    /// naive matrix is <c>n²</c> point-to-point searches; a one-to-all from each origin is <c>n</c>
    /// searches, because a settled Dijkstra already holds the cost to every node it reached and
    /// reading <c>n</c> of them out is <c>n</c> array accesses. That is the difference between
    /// <c>n²</c> Dijkstras and <c>n</c>.
    /// </para>
    /// <para>
    /// <b>In partition order, which is grid order, which is a function of occupancy alone</b>
    /// (<see cref="RoutingPartition"/>). So the fill order is recoverable from saved state and two
    /// identical cities produce byte-identical matrices — the property that lets this stay derived.
    /// </para>
    /// <para>
    /// <b>A partition with no access node in this mode has an all-Impassable row and column, apart
    /// from its diagonal.</b> That is a pedestrianised block asked for a car time, or the reverse. It
    /// is a real state rather than a defect, and it reads the same way a severed partition does —
    /// which is correct, because for that mode it is severed.
    /// </para>
    /// </remarks>
    public void Rebuild(RoadGraph graph, TravelMode mode, WalkScratch scratch)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(scratch);

        RoutingPartition partition = graph.Partition;
        int order = partition.Count;

        if (_entries.Length < order * order)
        {
            _entries = new TravelTime[order * order];
        }

        _order = order;
        _builtFor = mode;
        _builtAt = graph.Version;
        Searches = 0;
        Settled = 0;

        int nodeCount = graph.Nodes.Rows.SlotCount;

        for (int from = 0; from < order; from++)
        {
            int origin = partition.AccessNode(from, mode);

            if (origin == Rows.NoSlot)
            {
                for (int to = 0; to < order; to++)
                {
                    _entries[(from * order) + to] = TravelTime.Impassable;
                }

                // Zero to itself even with no anchor: a partition is always reachable from where you
                // already are, and a consumer comparing a home partition against itself must not be
                // told it cannot get there.
                _entries[(from * order) + from] = TravelTime.Zero;

                continue;
            }

            scratch.Begin(nodeCount);
            scratch.Seed(origin, TravelTime.Zero);
            scratch.SettleAll(graph, mode);

            Searches++;
            Settled += scratch.Relaxed;

            for (int to = 0; to < order; to++)
            {
                int target = partition.AccessNode(to, mode);

                _entries[(from * order) + to] =
                    target == Rows.NoSlot ? TravelTime.Impassable : scratch.CostTo(target);
            }
        }
    }

    /// <summary>
    /// The estimated free-flow time from one partition to another, or
    /// <see cref="TravelTime.Impassable"/>.
    /// </summary>
    /// <remarks>
    /// <b>An estimate in every case but two: the diagonal, and Impassable.</b> Everything else is
    /// measured access node to access node and is wrong by up to <see cref="EntryError"/> at each
    /// end. A caller that treats it as exact is choosing a threshold blurred by that amount in both
    /// directions.
    /// </remarks>
    public TravelTime From(int origin, int destination) =>
        (uint)origin < (uint)_order && (uint)destination < (uint)_order
            ? _entries[(origin * _order) + destination]
            : TravelTime.Impassable;

    /// <summary>
    /// The time to cross one partition at a given speed — <b>the geometric scale of an entry's
    /// error, and explicitly not a bound on it</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An entry runs access node to access node and a real journey runs Address to Address</b>, so
    /// the difference is two within-partition walks. This is the straight-line size of one of those:
    /// a whole partition crossed once, half at each end.
    /// </para>
    /// <para>
    /// ⚠ <b>It is NOT a bound and a reject must not be built on it, which was measured rather than
    /// argued.</b> A walk inside a partition is <em>road</em> distance, and nothing bounds road
    /// distance by the partition's size — a cul-de-sac, a spiral or a severed corner makes it
    /// arbitrarily long. At <see cref="RoutingPartition.DesignEdge"/> and walking pace this returns
    /// <b>6.1 minutes</b> while the measured worst overstatement against a real walk is <b>9.2</b>
    /// (<c>TravelTimeMatrixTests</c>, 5c task 2). A margin taken from here would have discarded
    /// reachable work. ***A structure laid over a graph cannot bound a quantity measured on the
    /// graph*** — which is <c>BuildingResidency</c>'s <i>a catchment is a time rather than a
    /// distance</i> one level up.
    /// </para>
    /// <para>
    /// <b>What it is good for is stating the scale in the mode's own units</b>, which is the thing
    /// the corpus keeps getting wrong: the same 512 m is about half a minute by car and about six on
    /// foot, so a partition sized against one mode is not thereby sized against the other. Callers
    /// wanting a safe reject margin take the measured tail and add a factor, and say so.
    /// </para>
    /// </remarks>
    /// <param name="partition">The tiling the entries were measured over.</param>
    /// <param name="speed">The mode's free-flow speed.</param>
    public static TravelTime EntryError(RoutingPartition partition, Speed speed)
    {
        ArgumentNullException.ThrowIfNull(partition);

        Tiles across = new(partition.Edge.Raw * CellGrid.TilesPerCell);

        return TravelTime.Over(across, speed);
    }
}
