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

    private RoadRuleset _ruleset;
    private int[] _cursor = [];

    /// <param name="ruleset">The <c>[roads]</c> table in force. <see cref="RoadRuleset.None"/> is a world with no roads.</param>
    public RoadGraph(RoadRuleset ruleset)
    {
        _ruleset = ruleset;

        int nodes = ExpectedNodes(ruleset);
        _nodes = new RoadNodeTable(nodes);
        _segments = new RoadSegmentTable(nodes * 2, _nodes);
    }

    /// <summary>The nodes. Registered with <c>World</c> and folded.</summary>
    public RoadNodeTable Nodes => _nodes;

    /// <summary>The Segments. Registered with <c>World</c> and folded.</summary>
    public RoadSegmentTable Segments => _segments;

    /// <summary>The directed adjacency. Derived, rebuilt, and invisible to the hash.</summary>
    public RoadArcs Arcs => _arcs;

    /// <summary>The per-mode component labelling. Derived, rebuilt, and invisible to the hash.</summary>
    public RoadConnectivity Connectivity => _connectivity;

    /// <summary>The <c>[roads]</c> table this graph's derived columns currently reflect.</summary>
    public RoadRuleset Ruleset => _ruleset;

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
        RebuildDerived();
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
