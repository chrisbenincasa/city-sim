using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// The Road Graph — <b>nodes and Segments, uniform regardless of how a road was drawn</b>
/// (<c>CONTEXT.md</c> → Road Graph: <i>"The simulation never sees a spline"</i>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The owner of three structures, and not itself a <c>[Table]</c>.</b>
/// <see cref="MapLayers"/> is the precedent exactly: a plain class holding the tables <c>World</c>
/// registers plus the derived storage it rebuilds, so that <c>BOR0901</c>'s rule — a <c>[Table]</c>
/// holds declared columns and its own <c>Rows</c> and nothing else — is satisfied by structure rather
/// than by exemption.
/// </para>
/// <para>
/// <b>Nodes and Segments fold into the State Hash; the Arcs and the components do not.</b> An Arc is
/// a function of the Segments and a component label is a function of the Arcs, so under
/// <c>adr/0040</c> the whole abstract routing structure is <c>(derived AND rebuilt)</c> and free to
/// change forever. <see cref="RoadArcs"/> records the sharper reason it cannot be a registered table
/// even so.
/// </para>
/// <para>
/// <b>Nothing moves on this graph. That is 5b.</b> This slice delivers a graph that exists, hashes,
/// saves and can be asked what is connected to what — no Trips, no Legs, no routing, no travel-time
/// matrix and no Lanes. <see cref="RoadSegmentTable.Fidelity"/> and
/// <see cref="RoadSegmentTable.VolumeForward"/> are declared and constant for the same reason: where
/// the number lands is this slice's decision and what the number is, is not.
/// </para>
/// </remarks>
public sealed class RoadGraph
{
    private readonly RoadNodeTable _nodes;
    private readonly RoadSegmentTable _segments;
    private readonly RoadArcs _arcs = new();
    private readonly RoadConnectivity _connectivity = new();
    private readonly StreetGrid _streets = new();
    private readonly RoutingPartition _partition = new(RoutingPartition.DesignEdge);

    private RoadRuleset _ruleset;
    private readonly Determinism.WorldKey _key;
    private BlockLattice _lattice = BlockLattice.None;
    private int[] _cursor = [];

    /// <param name="ruleset">The <c>[roads]</c> table in force. <see cref="RoadRuleset.None"/> is a world with no roads.</param>
    public RoadGraph(RoadRuleset ruleset)
        : this(ruleset, default)
    {
    }

    /// <param name="ruleset">The <c>[roads]</c> table in force.</param>
    /// <param name="key">
    /// The world, which the lattice's spacing is drawn on when <c>[roads] block_spread_tiles</c>
    /// states one. <c>plans/0045</c> row 25.
    /// </param>
    public RoadGraph(RoadRuleset ruleset, Determinism.WorldKey key)
    {
        _key = key;
        _ruleset = ruleset;
        _lattice = Lay(ruleset, key);

        int nodes = ExpectedNodes(ruleset);
        _nodes = new RoadNodeTable(nodes);
        _segments = new RoadSegmentTable(nodes * 2, _nodes);

        // The derived structures are built once here, on an empty graph, so that a world which never
        // runs the generator still knows the lattice its Ruleset declares. Without this the Street
        // index carries block_tiles = 0 until something else happens to rebuild it, and
        // CommandKind.Connect on a fresh world -- which is exactly what a replayed session is, since
        // a log's roads arrive as commands rather than from a generator -- would refuse on the ground
        // that the world has no road network, when what it has is no *rebuild*.
        //
        // The general form is worth keeping in view: a derived structure that caches a Ruleset value
        // reads as absent rather than as stale before its first rebuild, and absent is the state
        // every guard is written against.
        RebuildDerived();
    }

    /// <summary>The nodes. Registered with <c>World</c> and folded.</summary>
    public RoadNodeTable Nodes => _nodes;

    /// <summary>
    /// How many times the derived structures have been rebuilt. <b>The whole graph's version.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not an Epoch, and the distinction is the one <c>CONTEXT.md</c> → Epoch spends a paragraph
    /// on.</b> A Segment's Epoch is per-Segment so that a cached <em>route</em> can ask whether an
    /// edit touched <em>it</em>; a single counter would make every edit invalidate everything, which
    /// that entry calls a global flush. This counter is deliberately global, because its consumer is
    /// <see cref="Movement.TravelTimeMatrix"/> and every entry in a matrix is a shortest path over
    /// the whole graph — so any edit anywhere can move any entry and there is no finer question to
    /// ask. **A global counter is wrong for a route and right for a matrix**, and the two must not be
    /// collapsed because they happen to be counters.
    /// </para>
    /// <para>
    /// <b>Derived and unhashed.</b> It counts rebuilds, which is exactly the quantity
    /// <c>plans/0020</c> found makes a wholly-derived table unfoldable — two identical cities reached
    /// by different edit histories hold different values here, and that must never reach the State
    /// Hash.
    /// </para>
    /// </remarks>
    public uint Version { get; private set; }

    /// <summary>
    /// The routing partition — the tiling the travel-time matrix keys on (<c>adr/0040</c>).
    /// </summary>
    /// <remarks>
    /// <b>Derived, rebuilt, and read by nothing until 5c task 2.</b> It is owned here rather than by
    /// the matrix because it is a function of the graph and the map, both of which this class already
    /// holds, and because two consumers are foreseen — the matrix and the hierarchical router — so a
    /// tiling living inside the first of them would acquire that consumer's shape and be rewritten.
    /// </remarks>
    public RoutingPartition Partition => _partition;

    /// <summary>The Segments. Registered with <c>World</c> and folded.</summary>
    public RoadSegmentTable Segments => _segments;

    /// <summary>The directed adjacency. Derived, rebuilt, and invisible to the hash.</summary>
    public RoadArcs Arcs => _arcs;

    /// <summary>The per-mode component labelling. Derived, rebuilt, and invisible to the hash.</summary>
    public RoadConnectivity Connectivity => _connectivity;

    /// <summary>
    /// Which Street lies on each edge of the block lattice. Derived, rebuilt, and the subdivider's
    /// one entry point into the graph (<c>adr/0078</c>).
    /// </summary>
    public StreetGrid Streets => _streets;

    /// <summary>The <c>[roads]</c> table this graph's derived columns currently reflect.</summary>
    public RoadRuleset Ruleset => _ruleset;

    /// <summary>
    /// <b>Where this world's lattice lines stand</b> — the two questions <c>block_tiles</c> was
    /// answering by arithmetic (<see cref="BlockLattice"/>).
    /// </summary>
    /// <remarks>
    /// <b>Held by the graph rather than by <see cref="StreetGrid"/>, because the generator needs it
    /// before there is an index to read.</b> <c>RoadGenerator.LayInto</c> takes this graph and lays
    /// the Nodes the index will later find; if the lattice lived on the index the two would derive
    /// it separately and could disagree about where a line stands. Rebuilt in the constructor and in
    /// <see cref="Adopt"/>, which are the two places <c>[roads]</c> arrives.
    /// </remarks>
    public BlockLattice Lattice => _lattice;

    /// <summary>The lattice a <c>[roads]</c> table and a world describe.</summary>
    /// <remarks>
    /// ⚠ <b>Absent spread means UNIFORM, which is the shipped world.</b> The key is optional for the
    /// reason every <c>[roads]</c>-family key is: a default would be a hash-bearing world-creation
    /// number nobody chose. <c>plans/0045</c> row 25.
    /// </remarks>
    private static BlockLattice Lay(RoadRuleset ruleset, Determinism.WorldKey key) =>
        ruleset.BlockSpreadTiles > 0
            ? BlockLattice.Varied(key, ruleset.BlockTiles, ruleset.BlockSpreadTiles)
            : BlockLattice.Even(ruleset.BlockTiles);

    /// <summary>Whether this world has roads at all.</summary>
    public bool Exists => _segments.Rows.LiveCount > 0;

    /// <summary>
    /// Total road length in Tiles, summed over Segments rather than over Arcs.
    /// </summary>
    public long RoadLengthTiles()
    {
        long total = 0;

        for (int slot = 0; slot < _segments.Rows.SlotCount; slot++)
        {
            if (_segments.Rows.IsLive(slot))
            {
                total += _segments.LengthTiles[slot].Raw;
            }
        }

        return total;
    }

    /// <summary>How many live Segments admit a mode at all, in either direction.</summary>
    public int SegmentsAdmitting(TravelMode mode)
    {
        int count = 0;

        for (int slot = 0; slot < _segments.Rows.SlotCount; slot++)
        {
            if (_segments.Rows.IsLive(slot) && (_segments.Modes[slot] & (byte)mode) != 0)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Adopts a new <c>[roads]</c> table and rebuilds everything derived from it.
    /// </summary>
    /// <remarks>
    /// <b>Every number in <c>[roads]</c> is ordinary tuning, so nothing here refuses.</b>
    /// <c>adr/0015</c>'s membership test is <em>what live state points at</em>: a Segment row holds
    /// its own length, its own kind and its own masks, and reads a speed and a capacity <em>through</em>
    /// the Ruleset rather than storing one — which is exactly what makes retuning them free.
    /// <c>block_tiles</c> and the Arterial counts shape a graph the generator has already laid, so
    /// changing them alters what the <em>next</em> generation would produce and leaves the standing
    /// city alone. That is a no-op rather than a refusal, and it is the honest one: the alternative is
    /// re-laying every road under a player who edited a speed limit.
    /// </remarks>
    public void Adopt(RoadRuleset ruleset)
    {
        _ruleset = ruleset;
        _lattice = Lay(ruleset, _key);
        RebuildDerived();
    }

    /// <summary>
    /// Lays one Street on the lattice edge leaving <c>(column, row)</c> — <c>CommandKind.Connect</c>'s
    /// whole effect (<c>adr/0077</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Idempotent, and that is a property of the verb rather than a convenience.</b> A log that
    /// replays must produce one Street from two identical <c>connect</c> lines, because the second is
    /// the player clicking a Tile that already has a road on it. Returning <c>false</c> rather than
    /// throwing is the honest reading: the edit was refused by the world already being in the
    /// requested state, which is not an error the session should die on.
    /// </para>
    /// <para>
    /// <b>The endpoints are created on demand and never freed.</b> A node is an intersection, and an
    /// intersection with no Streets on it costs one row and is what makes the next lay cheap. Freeing
    /// them would make <see cref="StreetGrid"/>'s node index churn for no gain, and would put a
    /// recycled node slot underneath a Segment that outlived it.
    /// </para>
    /// <para>
    /// <b>Nothing else's Epoch moves, and that is the assertion worth testing.</b> The new Segment
    /// opens at Epoch 1 like any other; no standing Segment is touched, because under
    /// <c>adr/0012</c> an addition cannot make an existing route <em>wrong</em> — only suboptimal,
    /// which is the *boundedly wrong about an addition* half of the contract. S2 R5's finding is the
    /// reason to check it rather than assume it: <b>a single-counter Epoch <em>is</em> a global
    /// flush</b>, and an edit path that bumped every Segment would be exactly that, wearing per-Segment
    /// storage.
    /// </para>
    /// </remarks>
    /// <returns>Whether the graph changed.</returns>
    public bool LayStreet(int column, int row, StreetAxis axis)
    {
        if (!OnLattice(column, row, axis) || _streets.SegmentOn(column, row, axis) != Rows.NoSlot)
        {
            return false;
        }

        (int toColumn, int toRow) = axis == StreetAxis.East ? (column + 1, row) : (column, row + 1);

        _segments.Create(
            NodeFor(column, row),
            NodeFor(toColumn, toRow),

            // ⚠ THIS BLOCK'S WIDTH and not a nominal one -- a Segment's length is a piece of ground.
            // BlockLattice.Nominal is the other kind and would be wrong here on any lattice whose
            // lines are not evenly spaced. plans/0045 row 25.
            new Tiles(
                axis == StreetAxis.East
                    ? _lattice.WidthOf(column)
                    : _lattice.WidthOf(row)),
            RoadKind.Street,
            TravelMode.Any,
            TravelMode.Any);

        RebuildDerived();

        return true;
    }

    /// <summary>
    /// Bulldozes the Street on the lattice edge leaving <c>(column, row)</c>.
    /// </summary>
    /// <remarks>
    /// <b>This is the half of <c>adr/0012</c>'s contract that must never be wrong</b> — <i>never wrong
    /// about a removal</i> — and it is the only thing in the project that has ever freed a Segment
    /// from a running world. The row is freed rather than masked to zero modes, because a Segment
    /// nothing may traverse is still a Segment an Address can name, and <c>adr/0079</c> needs the
    /// distinction: a Building whose Street was bulldozed must end with **no** Address rather than one
    /// pointing at an impassable ghost.
    /// </remarks>
    /// <returns>Whether the graph changed.</returns>
    public bool BulldozeStreet(int column, int row, StreetAxis axis)
    {
        int slot = OnLattice(column, row, axis)
            ? _streets.SegmentOn(column, row, axis)
            : Rows.NoSlot;

        if (slot == Rows.NoSlot)
        {
            return false;
        }

        _segments.Rows.Free(_segments.Rows.At(slot));
        RebuildDerived();

        return true;
    }

    /// <summary>Whether an edge named this way exists on the lattice at all.</summary>
    private bool OnLattice(int column, int row, StreetAxis axis)
    {
        if (!_ruleset.Runs || _streets.Span == 0 || column < 0 || row < 0)
        {
            return false;
        }

        return axis == StreetAxis.East
            ? column < _streets.Blocks && row < _streets.Span
            : column < _streets.Span && row < _streets.Blocks;
    }

    /// <summary>The node at a lattice intersection, created if the lattice has none there.</summary>
    private Handle<RoadNode> NodeFor(int column, int row)
    {
        int slot = _streets.NodeAt(column, row);

        return slot != Rows.NoSlot
            ? _nodes.Rows.At(slot)
            : _nodes.Create(_lattice.TileOf(column), _lattice.TileOf(row));
    }

    /// <summary>
    /// Rebuilds the Segment's derived attributes, the CSR adjacency and the component labels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Wholesale rather than incremental, and that is a decision this slice is allowed to make
    /// cheaply.</b> Nothing yet edits the graph after generation — <c>CommandKind.Connect</c> is
    /// 5a-bis — so an incremental rebuild would be an optimisation of a call that happens once, sized
    /// against a cost nobody has measured. It is called from <c>World.RebuildDerived</c> on load and
    /// from <see cref="Adopt"/> on reload, which is the same discipline every other derived structure
    /// in the project follows.
    /// </para>
    /// <para>
    /// <b>In slot order, which is the one order a rebuild has available</b> (<c>05 §3</c>). Each node
    /// is reserved a run of Arcs and the emit fills it, so a node's Arcs are contiguous and
    /// <see cref="RoadNodeTable.ArcStart"/> is a slice rather than a search.
    /// </para>
    /// </remarks>
    public void RebuildDerived()
    {
        DeriveSegmentAttributes();
        RebuildAdjacency();
        _connectivity.Rebuild(_nodes, _segments);
        _streets.Rebuild(_nodes, _segments, _lattice);

        // Last, and it must run after RebuildAdjacency because it reads the Arcs.
        //
        // ⚠ This comment said the opposite one commit ago -- "it reads only the nodes ... so it has
        // no ordering constraint against anything above it" -- and task 1 put the call last for that
        // (wrong) reason. The dependency arrived with the per-mode access node: an anchor has to be a
        // node some Arc leaves in that mode, and the Arcs are what says so. Corrected rather than
        // quietly overwritten, because a stated absence of a constraint is the thing a later reader
        // reorders against.
        _partition.Rebuild(_nodes, _arcs);

        // After everything, so a consumer that compares versions and then reads is never handed a
        // half-rebuilt structure under a fresh number.
        Version++;
    }

    private void DeriveSegmentAttributes()
    {
        for (int slot = 0; slot < _segments.Rows.SlotCount; slot++)
        {
            if (!_segments.Rows.IsLive(slot))
            {
                continue;
            }

            var kind = (RoadKind)_segments.Kind[slot];

            _segments.Modes[slot] =
                (byte)(_segments.ModesForward[slot] | _segments.ModesBackward[slot]);
            _segments.FreeFlow[slot] = _ruleset.SpeedFor(kind);
            _segments.CapacityPerDay[slot] = _ruleset.CapacityFor(kind);

            // adr/0007's named hole. Fidelity follows Stress, Stress needs volume, and volume is
            // written by Trips — 5b. Written rather than left alone so that a rebuild is idempotent
            // over a column somebody may later start writing.
            _segments.Fidelity[slot] = 0;
        }
    }

    private void RebuildAdjacency()
    {
        int nodeSlots = _nodes.Rows.SlotCount;

        if (_cursor.Length < nodeSlots)
        {
            _cursor = new int[nodeSlots];
        }

        Array.Clear(_cursor, 0, nodeSlots);

        for (int slot = 0; slot < nodeSlots; slot++)
        {
            _nodes.ArcStart[slot] = 0;
            _nodes.ArcCount[slot] = 0;
        }

        // Pass one: how many Arcs leave each node.
        for (int segment = 0; segment < _segments.Rows.SlotCount; segment++)
        {
            if (!Endpoints(segment, out int a, out int b))
            {
                continue;
            }

            _cursor[a]++;
            _cursor[b]++;
        }

        // Pass two: the prefix sum, which is where a node's slice begins.
        int running = 0;

        for (int slot = 0; slot < nodeSlots; slot++)
        {
            _nodes.ArcStart[slot] = running;
            running += _cursor[slot];
            _cursor[slot] = _nodes.ArcStart[slot];
        }

        _arcs.Reset(running);

        // Pass three: emit. Each node's cursor walks its own reserved run, so the array comes out
        // grouped by source node without a sort — and every reserved position is written exactly
        // once, because the runs partition the array and a cursor advances once per incident Segment.
        for (int segment = 0; segment < _segments.Rows.SlotCount; segment++)
        {
            if (!Endpoints(segment, out int a, out int b))
            {
                continue;
            }

            Emit(_cursor[a]++, segment, b, (TravelMode)_segments.ModesForward[segment]);
            Emit(_cursor[b]++, segment, a, (TravelMode)_segments.ModesBackward[segment]);
        }

        for (int slot = 0; slot < nodeSlots; slot++)
        {
            _nodes.ArcCount[slot] = _cursor[slot] - _nodes.ArcStart[slot];
        }
    }

    private void Emit(int arc, int segment, int target, TravelMode modes)
    {
        Tiles length = _segments.LengthTiles[segment];
        Speed road = _segments.FreeFlow[segment];

        _arcs.Set(
            arc,
            target,
            segment,
            modes,
            Traversal(length, road, modes, TravelMode.Car),
            Traversal(length, road, modes, TravelMode.Foot));
    }

    /// <summary>
    /// An Arc's traversal time for one mode, from the Segment's free-flow speed and the mode's own
    /// ceiling.
    /// </summary>
    /// <remarks>
    /// <b><c>min(the mode's ceiling, the road's free-flow)</c>, and the ceiling only exists for
    /// foot.</b> A Segment has one free-flow speed and two modes traverse it at different rates,
    /// which one column cannot express — and a second speed column would contradict either
    /// <c>CONTEXT.md</c> → Segment, which lists free-flow speed in the singular, or
    /// <c>CONTEXT.md</c> → Road Graph's <i>one graph with mode masks</i>. A pedestrian walks at
    /// walking pace on a boulevard and in a lane alike; a car is held to the road, so its ceiling is
    /// the road itself.
    /// </remarks>
    private TravelTime Traversal(Tiles length, Speed road, TravelMode arc, TravelMode mode)
    {
        if ((arc & mode) == 0)
        {
            return TravelTime.Impassable;
        }

        Speed at = mode == TravelMode.Foot ? Speed.SlowerOf(road, _ruleset.WalkSpeed) : road;

        return TravelTime.Over(length, at);
    }

    /// <summary>
    /// Resolves a Segment's two endpoints, refusing a dead row or a dangling handle.
    /// </summary>
    /// <remarks>
    /// <b>A dangling endpoint is skipped rather than reported here</b>, because this runs on the
    /// rebuild path and a rebuild that threw would take down a load rather than a write. The
    /// whole-world invariant tier reports it, which is where <c>02 §10</c> puts a check that walks
    /// every row.
    /// </remarks>
    private bool Endpoints(int segment, out int a, out int b)
    {
        a = 0;
        b = 0;

        return _segments.Rows.IsLive(segment)
            && _nodes.Rows.TryResolve(_segments.NodeA[segment], out a)
            && _nodes.Rows.TryResolve(_segments.NodeB[segment], out b);
    }

    /// <summary>
    /// How many nodes a Ruleset's grid implies, so the tables open at roughly the right size.
    /// </summary>
    /// <remarks>
    /// A sizing figure and nothing more — it never reaches the hash, and a graph that outgrows it
    /// simply grows. Zero <c>block_tiles</c> means a world with no roads, which opens at the
    /// allocator's own minimum rather than at zero.
    /// </remarks>
    private static int ExpectedNodes(RoadRuleset ruleset)
    {
        if (!ruleset.Runs)
        {
            return 1;
        }

        int across = Arithmetic.IntegerMath.FloorDiv(CellGrid.WorldTiles, ruleset.BlockTiles) + 1;

        return across * across;
    }
}
