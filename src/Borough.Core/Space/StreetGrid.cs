using Borough.Core.Arithmetic;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which way a lattice edge runs from the intersection that names it.
/// </summary>
/// <remarks>
/// <b>Two values rather than four, because an edge is named by its lower endpoint</b> — the
/// convention <c>adr/0077</c> puts on <c>CommandKind.Connect</c> and <see cref="StreetGrid"/> uses
/// internally. Four directions would name every edge twice and make <em>lay the Street north of here</em>
/// and <em>lay the Street south of there</em> two commands with one effect, which is a log that
/// records the player's keystroke rather than the player's edit.
/// </remarks>
public enum StreetAxis : byte
{
    /// <summary>Eastward, to the next intersection along the east axis.</summary>
    East = 0,

    /// <summary>Northward, to the next intersection along the north axis.</summary>
    North = 1,
}

/// <summary>
/// Which Street Segment lies on each edge of the block lattice — <b>the index that lets the
/// subdivider name a block's four faces without searching for them</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Derived, rebuilt, and invisible to the hash</b>, like <see cref="RoadArcs"/> and
/// <see cref="RoadConnectivity"/> beside it. It is a function of the Segments and their endpoints, so
/// under <c>adr/0040</c> it is free to change forever — and like those two it is a plain class the
/// <see cref="RoadGraph"/> owns rather than a registered <c>[Table]</c>, because a wholly-derived
/// table cannot join <c>World._tables</c>: <c>Rows.Fold</c> folds the allocator's scalars before
/// consulting any column's disposition, so it would hash its own rebuild count.
/// </para>
/// <para>
/// <b>Only <see cref="RoadKind.Street"/> is indexed, and that is <c>adr/0014</c>'s asymmetry rather
/// than a filter for convenience.</b> <c>CONTEXT.md</c> → Frontage: <i>"<b>Only Streets grant
/// frontage.</b> Arterials carry none and have no Access Point, so nothing zones onto one"</i>. A
/// severed Street kept as a pedestrian crossing has become a <see cref="RoadKind.FootPath"/>, so it
/// drops out here by kind — which is correct and worth noticing: <b>a footbridge is not frontage</b>,
/// and a block that keeps only its crossings genuinely has nothing to build against.
/// </para>
/// <para>
/// <b>An edge is addressed by its lower endpoint and a direction</b>, which is the same convention
/// <c>CommandKind.Connect</c> uses on the way in (<c>adr/0077</c>) — one origin plus an orientation
/// names an adjacent pair uniquely, because the grid spacing is Ruleset data. Keeping the command and
/// the index on one convention means the player's verb and the simulation's structure do not differ,
/// and there is no seam for a later consumer to know about.
/// </para>
/// </remarks>
public sealed class StreetGrid
{
    private int[] _horizontal = [];
    private int[] _vertical = [];
    private int[] _nodes = [];
    private int[] _offLattice = [];
    private int[] _offLatticeHead = [];
    private int[] _offLatticeNext = [];

    /// <summary>Intersections along one edge of the map. Zero where the world has no roads.</summary>
    public int Span { get; private set; }

    /// <summary>Tiles between adjacent intersections, as the <c>[roads]</c> table states it.</summary>
    public int BlockTiles { get; private set; }

    /// <summary>Blocks along one edge of the map — one fewer than the intersections.</summary>
    public int Blocks => Span > 0 ? Span - 1 : 0;

    /// <summary>
    /// Live Segments this index does <b>not</b> hold. <b>The complement, and it is recorded rather
    /// than derivable.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Segment earns a lattice place by geometry, so everything else — every
    /// <see cref="RoadKind.Arterial"/>, every Street whose endpoints miss the lattice, every Street on
    /// the lattice but not one step long — falls out of <see cref="Rebuild"/> through a
    /// <c>continue</c> and was, until this list existed, <b>invisible to any caller that asked the
    /// index what roads are near a Tile</b>.
    /// </para>
    /// <para>
    /// <b>It exists for <see cref="LineSourceQueries"/>, and the alternative was a silent gap.</b>
    /// <c>02 §2.4</c> names noise's sources as <em>frontage Street volume + Arterials within ~300 m</em>,
    /// so a lattice-only query omits the loudest ones and returns a quiet answer with nothing to say it
    /// is incomplete.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>This list is the MEMBERSHIP and no longer the traversal order. Walk it through
    /// <see cref="OffLatticeHead"/>, never end to end.</b> It used to say the linear scan was on
    /// purpose, resting that on <c>adr/0014</c>'s grid-plus-sparse-Arterials layout making the set
    /// small — <em>using the model's own premise as the implementation strategy</em>. ⚠ <b>The premise
    /// was true of Arterials and a foot path falsified it silently</b>: a foot path is off-lattice too,
    /// and <c>[roads] foot_paths_per_thousand_blocks</c> is a rate <em>per block</em>, so the set grew
    /// with the map rather than staying sparse. On <c>rulesets/bordered.toml</c> it is <b>12,581</b>
    /// Segments of which about <b>10,500</b> are foot paths, and one whole-map land value pass walked
    /// it 2 million times — <b>26.4 billion visits, 88 seconds</b>
    /// (<c>plans/0013</c>, and <c>tests/.../SealingCostTests</c> is the instrument).
    /// ***A premise cited as an implementation strategy has to be re-checked whenever anything joins
    /// the set it describes***, and nothing re-checked it.
    /// </para>
    /// <para>
    /// ⚠ <b>Rebuilt only by <see cref="Rebuild"/></b>, in the pass that fills the lattice, so it cannot
    /// disagree with the lattice about which Segments are on it. A second pass could.
    /// </para>
    /// </remarks>
    public int OffLatticeCount { get; private set; }

    /// <summary>The slot of the <paramref name="index"/>th Segment this index does not hold.</summary>
    public int OffLatticeAt(int index) =>
        index < 0 || index >= OffLatticeCount ? Rows.NoSlot : _offLattice[index];

    /// <summary>
    /// The furthest, in blocks, any off-lattice Segment reaches from the block its first endpoint
    /// stands in. <b>The amount a spatial query must widen its window by.</b>
    /// </summary>
    /// <remarks>
    /// <b>Each off-lattice Segment sits in exactly one bucket — the block of its first endpoint — so
    /// a walker visits it exactly once and pass two cannot double-count it.</b> The price of that is
    /// this: a Segment can reach out of its bucket, so a query widens its window by the worst case
    /// rather than filtering per Segment. An Arterial between Junction pieces is what sets it.
    /// </remarks>
    public int OffLatticeReachBlocks { get; private set; }

    /// <summary>
    /// The first off-lattice Segment bucketed at block <c>(column, row)</c>, or
    /// <see cref="Rows.NoSlot"/>.
    /// </summary>
    public int OffLatticeHead(int column, int row) =>
        column < 0 || row < 0 || column >= Span || row >= Span
            ? Rows.NoSlot
            : _offLatticeHead[(row * Span) + column];

    /// <summary>The next off-lattice Segment in the same bucket, or <see cref="Rows.NoSlot"/>.</summary>
    public int OffLatticeNext(int slot) =>
        slot < 0 || slot >= _offLatticeNext.Length ? Rows.NoSlot : _offLatticeNext[slot];

    /// <summary>
    /// The Segment running east from intersection <c>(column, row)</c>, or
    /// <see cref="Rows.NoSlot"/> if no Street lies there.
    /// </summary>
    public int Horizontal(int column, int row) =>
        column < 0 || row < 0 || column >= Blocks || row >= Span
            ? Rows.NoSlot
            : _horizontal[(row * Blocks) + column];

    /// <summary>
    /// The Segment running north from intersection <c>(column, row)</c>, or
    /// <see cref="Rows.NoSlot"/> if no Street lies there.
    /// </summary>
    public int Vertical(int column, int row) =>
        column < 0 || row < 0 || column >= Span || row >= Blocks
            ? Rows.NoSlot
            : _vertical[(column * Blocks) + row];

    /// <summary>
    /// The Segment on the lattice edge leaving <c>(column, row)</c> along <paramref name="axis"/>, or
    /// <see cref="Rows.NoSlot"/> if no Street lies there.
    /// </summary>
    public int SegmentOn(int column, int row, StreetAxis axis) =>
        axis == StreetAxis.East ? Horizontal(column, row) : Vertical(column, row);

    /// <summary>
    /// The node standing at intersection <c>(column, row)</c>, or <see cref="Rows.NoSlot"/> if the
    /// lattice has no node there.
    /// </summary>
    /// <remarks>
    /// <b>An intersection with no node is an ordinary state and not a defect</b>: the generator lays
    /// the whole lattice at world creation, but a world whose <c>[roads]</c> declares a grid the
    /// generator never ran over has none, and <c>CommandKind.Connect</c> must be able to lay the first
    /// Street in an empty world. It is the endpoint that gets created on demand, never the Segment.
    /// </remarks>
    public int NodeAt(int column, int row) =>
        column < 0 || row < 0 || column >= Span || row >= Span
            ? Rows.NoSlot
            : _nodes[(row * Span) + column];

    /// <summary>
    /// Rebuilds the index from the live Streets.
    /// </summary>
    /// <remarks>
    /// <b>A Segment earns a place here by geometry rather than by provenance</b> — both endpoints on
    /// the lattice, one step apart, kind <see cref="RoadKind.Street"/> — so a Street laid by the
    /// player and one laid by the generator index identically. That is what makes re-subdivision after
    /// a road edit the same code path as subdivision at world creation, which is the property
    /// <c>plans/0022</c> asks for when it says neither half is testable alone.
    /// </remarks>
    public void Rebuild(RoadNodeTable nodes, RoadSegmentTable segments, int blockTiles)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(segments);

        BlockTiles = blockTiles;
        Span = blockTiles > 0 ? IntegerMath.FloorDiv(CellGrid.WorldTiles, blockTiles) + 1 : 0;

        int edges = Blocks * Span;
        int intersections = Span * Span;

        if (_horizontal.Length < edges)
        {
            _horizontal = new int[edges];
            _vertical = new int[edges];
        }

        if (_nodes.Length < intersections)
        {
            _nodes = new int[intersections];
        }

        if (_offLattice.Length < segments.Rows.SlotCount)
        {
            _offLattice = new int[segments.Rows.SlotCount];
            _offLatticeNext = new int[segments.Rows.SlotCount];
        }

        if (_offLatticeHead.Length < intersections)
        {
            _offLatticeHead = new int[intersections];
        }

        OffLatticeCount = 0;
        OffLatticeReachBlocks = 0;

        Array.Fill(_offLatticeHead, Rows.NoSlot, 0, intersections);
        Array.Fill(_offLatticeNext, Rows.NoSlot, 0, segments.Rows.SlotCount);

        // NoSlot rather than zero, because zero is a real Segment slot. The same plus-one reasoning
        // Address and LotTable.BuildingSlot give, spelled the other way round: here the array is
        // cleared explicitly, so the sentinel is free to be -1.
        Array.Fill(_horizontal, Rows.NoSlot, 0, edges);
        Array.Fill(_vertical, Rows.NoSlot, 0, edges);
        Array.Fill(_nodes, Rows.NoSlot, 0, intersections);

        // The nodes first, because the Segment pass reads lattice positions off them and a Segment
        // whose endpoints are not on the lattice is not a block face.
        for (int slot = 0; Span > 0 && slot < nodes.Rows.SlotCount; slot++)
        {
            if (nodes.Rows.IsLive(slot) && OnLattice(nodes, slot, out int column, out int row))
            {
                _nodes[(row * Span) + column] = slot;
            }
        }

        // Every live Segment is placed on the lattice or recorded as off it, in ONE pass. The
        // complement is what LineSourceQueries walks, and computing it here rather than in a second
        // pass is what stops the two disagreeing about which Segments are on the lattice.
        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            bool placed = false;

            if (Span > 0
                && (RoadKind)segments.Kind[slot] == RoadKind.Street
                && nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                && nodes.Rows.TryResolve(segments.NodeB[slot], out int b)
                && OnLattice(nodes, a, out int columnA, out int rowA)
                && OnLattice(nodes, b, out int columnB, out int rowB))
            {
                if (rowA == rowB && columnB == columnA + 1)
                {
                    _horizontal[(rowA * Blocks) + columnA] = slot;
                    placed = true;
                }
                else if (columnA == columnB && rowB == rowA + 1)
                {
                    _vertical[(columnA * Blocks) + rowA] = slot;
                    placed = true;
                }
            }

            if (!placed)
            {
                _offLattice[OffLatticeCount++] = slot;

                Bucket(nodes, segments, slot, blockTiles);
            }
        }
    }

    /// <summary>
    /// Files one off-lattice Segment under the block its first endpoint stands in, and widens
    /// <see cref="OffLatticeReachBlocks"/> to cover how far it runs from there.
    /// </summary>
    /// <remarks>
    /// <b>This is what stopped the noise query being a scan of every off-lattice Segment in the
    /// world.</b> <see cref="OffLatticeCount"/>'s remark calls the scan deliberate and rests it on
    /// <c>adr/0014</c>'s <em>grid plus sparse Arterials</em> — which held while the off-lattice set
    /// WAS the Arterials. ⚠ <b>A foot path is off-lattice too, and
    /// <c>[roads] foot_paths_per_thousand_blocks</c> is a rate per block</b>, so the set grew with
    /// the map: on <c>rulesets/bordered.toml</c> it is 12,581 Segments of which about 10,500 are foot
    /// paths, and one whole-map land value pass walked it 2 million times.
    /// </remarks>
    private void Bucket(RoadNodeTable nodes, RoadSegmentTable segments, int slot, int blockTiles)
    {
        if (blockTiles <= 0
            || Span <= 0
            || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
            || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
        {
            return;
        }

        int columnA = IntegerMath.FloorDiv(nodes.East[a].Raw, blockTiles);
        int rowA = IntegerMath.FloorDiv(nodes.North[a].Raw, blockTiles);
        int columnB = IntegerMath.FloorDiv(nodes.East[b].Raw, blockTiles);
        int rowB = IntegerMath.FloorDiv(nodes.North[b].Raw, blockTiles);

        int spanEast = IntegerMath.Abs(columnB - columnA);
        int spanNorth = IntegerMath.Abs(rowB - rowA);

        // THE MIDPOINT AND NOT AN ENDPOINT, which halves the reach and therefore quarters the block
        // window every query walks. A Segment filed under one end reaches its whole length away; filed
        // under the middle it reaches half, and the window is squared, so this is worth about 3x on a
        // world with Arterials. It is why `reach` is a CEILING of the half-span: an odd span must round
        // up or the far end falls outside the window it is looked for in.
        int column = IntegerMath.FloorDiv(
            nodes.East[a].Raw + nodes.East[b].Raw, blockTiles * 2);
        int row = IntegerMath.FloorDiv(
            nodes.North[a].Raw + nodes.North[b].Raw, blockTiles * 2);

        int reach = IntegerMath.CeilDiv(spanEast > spanNorth ? spanEast : spanNorth, 2) + 1;

        if (reach > OffLatticeReachBlocks)
        {
            OffLatticeReachBlocks = reach;
        }

        if (column < 0 || row < 0 || column >= Span || row >= Span)
        {
            return;
        }

        int bucket = (row * Span) + column;

        _offLatticeNext[slot] = _offLatticeHead[bucket];
        _offLatticeHead[bucket] = slot;
    }

    /// <summary>
    /// Whether a node sits exactly on a lattice intersection, and where.
    /// </summary>
    /// <remarks>
    /// <b>An Arterial's Junction node is at an arbitrary Tile and fails this test</b>, which is what
    /// keeps `adr/0014`'s asymmetry honest without a second column recording how a node was made. The
    /// exact-multiple check is doing the work: a Junction that happened to land on a multiple is still
    /// refused, because the Segments incident to it are <see cref="RoadKind.Arterial"/> and never
    /// reach this method.
    /// </remarks>
    private bool OnLattice(RoadNodeTable nodes, int slot, out int column, out int row)
    {
        int east = nodes.East[slot].Raw;
        int north = nodes.North[slot].Raw;

        column = IntegerMath.FloorDiv(east, BlockTiles);
        row = IntegerMath.FloorDiv(north, BlockTiles);

        return east == column * BlockTiles
            && north == row * BlockTiles
            && column >= 0 && column < Span
            && row >= 0 && row < Span;
    }
}
