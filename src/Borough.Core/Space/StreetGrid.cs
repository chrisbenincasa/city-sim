using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
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
/// What a drag across the lattice asks for — <b>and every value but one is something the Street
/// tool cannot do in a single edit.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A Street edit is one Segment</b> (<c>adr/0077</c>), and the lattice runs on two axes, so the
/// only drag a click can honour is the one that begins and ends on the same edge. ⚠ <b>The other
/// two are not defects and are not refusals either</b>: they are the shape of the answer to
/// <em>can I drag a road from here to there</em>, which until this enum existed the tool answered
/// by laying one Segment somewhere near the start and saying nothing.
/// </para>
/// <para>
/// ⚠ <b><see cref="TwoAxes"/> is the diagonal AND the dog-leg, deliberately.</b> They differ in
/// shape and not in what the player is owed — both need east and north, so both are a sequence of
/// clicks rather than one — and a front end that separated them would be offering a distinction the
/// tool does not have.
/// </para>
/// </remarks>
public enum StreetDrag : byte
{
    /// <summary>
    /// The world states no <c>[roads] block_tiles</c>, so there is no lattice to drag across.
    /// <b><c>Refusal.ConnectWorldHasNoLattice</c> is the sentence for it</b>, and it is the core's
    /// rather than the aim's.
    /// </summary>
    NoLattice = 0,

    /// <summary>Both ends name one edge. <b>An ordinary click, and the only drag a click honours.</b></summary>
    OneEdge = 1,

    /// <summary>
    /// The two edges lie on one straight line. <b>A run of Streets, and a run is many edits.</b>
    /// </summary>
    OneLine = 2,

    /// <summary>
    /// Getting from one to the other needs both axes — <b>a diagonal, or a dog-leg.</b> There is no
    /// diagonal Street to lay, and the diagonals already standing in a generated city are
    /// <see cref="RoadKind.FootPath"/>s the world was made with.
    /// </summary>
    TwoAxes = 3,
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

    /// <summary>
    /// <b>Where the lines stand.</b> <see cref="BlockLattice.None"/> on a world with no roads.
    /// </summary>
    public BlockLattice Lattice { get; private set; } = BlockLattice.None;

    /// <summary>
    /// A block as a LENGTH, as the <c>[roads]</c> table states it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This is <see cref="BlockLattice.Nominal"/> and NOT the width of any particular block.</b>
    /// It survives as the <em>does this world have a lattice at all</em> test and as a distance;
    /// every site that meant <em>this block's ground</em> reads <see cref="Lattice"/> now.
    /// <c>plans/0045</c> row 25.
    /// </remarks>
    public int BlockTiles => Lattice.Nominal;

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
    /// The lattice edge nearest a Tile — <b>the aim, where <see cref="SegmentOn"/> is the lookup.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>NEAREST AND NOT FLOORED, AND THAT DIFFERENCE IS THE WHOLE OF WHY THE STREET TOOL COULD
    /// NOT BE AIMED.</b> <c>Simulation.ApplyConnect</c> floors the Tile a <c>CommandKind.Connect</c>
    /// names — <c>adr/0014</c>'s <em>Streets snap to the grid</em>, and it is the city's snap to make
    /// — so a Command naming <em>any</em> Tile inside a block edits that block's <b>south-west</b>
    /// corner. A click near a block's top-right laid a Street at its bottom-left, a block away, and
    /// the player raised it unprompted on the first session with a world worth building in
    /// (<c>plans/0045</c> row 22). ⚠ <b>Face midpoints work perfectly and the interior does not</b>,
    /// which is why forty driven clicks had never caught it.
    /// </para>
    /// <para>
    /// <b>Perpendicular distance to the four lines the block sits between</b>, which answers the axis
    /// as well as the edge: the two east–west faces are <see cref="StreetAxis.East"/> edges and the
    /// two north–south ones are <see cref="StreetAxis.North"/>. ***One click cannot say more than
    /// which edge it is nearest***, and that is exactly what it does say.
    /// </para>
    /// <para>
    /// ⚠ <b>Ties go south, then west, then north, then east.</b> A click at the dead centre of a
    /// block is equidistant from all four and there is no honest answer there — so it gets a stated
    /// one rather than a coin, and the same click always lays the same Street.
    /// </para>
    /// <para>
    /// ⚠ <b>Every edge it returns is one the graph will act on.</b> The block is brought inside
    /// <see cref="Blocks"/> first, so the four candidates all satisfy <c>RoadGraph.LayStreet</c>'s
    /// lattice bound — an edge outside it is not refused there, it returns <c>false</c> and applies
    /// nothing, and ***a verb that silently does nothing is worse than one that says no***.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The column, row and axis naming the edge — or <see cref="Rows.NoSlot"/> for both indices on a
    /// world whose <c>[roads]</c> states no lattice, which is the world
    /// <c>Refusal.ConnectWorldHasNoLattice</c> covers.
    /// </returns>
    public (int Column, int Row, StreetAxis Axis) NearestEdge(Tiles east, Tiles north)
    {
        if (BlockTiles <= 0 || Blocks <= 0)
        {
            return (Rows.NoSlot, Rows.NoSlot, StreetAxis.East);
        }

        int column = Lattice.LineAt(east.Raw);
        int row = Lattice.LineAt(north.Raw);

        column = column < 0 ? 0 : column >= Blocks ? Blocks - 1 : column;
        row = row < 0 ? 0 : row >= Blocks ? Blocks - 1 : row;

        // 🔴 THE TWO EXTENTS ARE THIS BLOCK'S OWN AND THEY NEED NOT BE EQUAL. They were one number
        // until plans/0045 row 25, which is what made the four comparisons below read as a
        // half-block test; they are a distance to each of four named edges and always were.
        int wide = Lattice.WidthOf(column);
        int deep = Lattice.WidthOf(row);

        int alongEast = east.Raw - Lattice.EdgeOf(column);
        int alongNorth = north.Raw - Lattice.EdgeOf(row);

        alongEast = alongEast < 0 ? 0 : alongEast > wide ? wide : alongEast;
        alongNorth = alongNorth < 0 ? 0 : alongNorth > deep ? deep : alongNorth;

        // South first, and every comparison below is strict -- so the first of an equal pair keeps
        // the answer and this order IS the tie-break the remark names.
        (int Column, int Row, StreetAxis Axis) edge = (column, row, StreetAxis.East);
        int nearest = alongNorth;

        if (alongEast < nearest)
        {
            nearest = alongEast;
            edge = (column, row, StreetAxis.North);
        }

        if (deep - alongNorth < nearest)
        {
            nearest = deep - alongNorth;
            edge = (column, row + 1, StreetAxis.East);
        }

        if (wide - alongEast < nearest)
        {
            edge = (column + 1, row, StreetAxis.North);
        }

        return edge;
    }

    /// <summary>
    /// The Tile intersection <c>(column, row)</c> stands on — <b>what a <c>CommandKind.Connect</c>
    /// must name to reach the edges leaving it.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Exact rather than merely inside the block, and that is what makes the city's floor a
    /// no-op.</b> <c>Simulation.ApplyConnect</c> takes <c>FloorDiv(tile, block_tiles)</c> of what the
    /// Command names, and <c>FloorDiv(column × block_tiles, block_tiles)</c> is <c>column</c> — so a
    /// front end that aims with <see cref="NearestEdge"/> and addresses with this hands the city a
    /// Tile it cannot move. ***The snap stays the city's and the aim stays the hand's***, which is
    /// the division <c>Simulation.Refuses</c> already draws between a rule and a pick.
    /// </remarks>
    public (Tiles East, Tiles North) IntersectionTile(int column, int row) =>
        (Lattice.TileOf(column), Lattice.TileOf(row));

    /// <summary>
    /// What a drag from one Tile to another asks the lattice for — <b>the classification, so that a
    /// front end can say why a straight line between two points is not one Street.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE LATTICE HAS TWO AXES AND NOTHING SAID SO.</b> <see cref="StreetAxis"/> declares
    /// exactly <see cref="StreetAxis.East"/> and <see cref="StreetAxis.North"/>, so
    /// <em>how do I build a diagonal road</em> has the answer <b>you cannot</b> — a refusal, and
    /// <c>adr/0070</c>'s one classification that counts as evidence. ⚠ <b>And the generated world
    /// contains diagonals</b>: <c>[roads] foot_paths_per_thousand_blocks</c> lays a
    /// <see cref="RoadKind.FootPath"/> corner to corner across a block, which is why
    /// <c>--morphology</c> reports six occupied compass bins where a pure lattice has four.
    /// ***A player who has seen a diagonal on screen will reasonably ask for the tool that made
    /// it***, and no tool did. <c>plans/0045</c> row 23.
    /// </para>
    /// <para>
    /// ⚠ <b>It classifies and refuses nothing.</b> Both ends resolve through
    /// <see cref="NearestEdge"/>, so this is the same aim <c>Simulation.ApplyConnect</c> will act on
    /// rather than a second reading of the cursor — and the words a player reads are the shell's,
    /// as <c>Refusal</c>'s own remark requires.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The classification, and how many Streets one straight run would take — <b>which is stated
    /// only for <see cref="StreetDrag.OneLine"/></b>. There is no single run to count across
    /// <see cref="StreetDrag.TwoAxes"/>, because the route is a choice rather than a length, so it
    /// answers zero rather than inventing one.
    /// </returns>
    public (StreetDrag Drag, int Streets) Between(
        Tiles fromEast, Tiles fromNorth, Tiles toEast, Tiles toNorth)
    {
        if (BlockTiles <= 0 || Blocks <= 0)
        {
            return (StreetDrag.NoLattice, 0);
        }

        (int fromColumn, int fromRow, StreetAxis fromAxis) = NearestEdge(fromEast, fromNorth);
        (int toColumn, int toRow, StreetAxis toAxis) = NearestEdge(toEast, toNorth);

        if (fromAxis != toAxis)
        {
            return (StreetDrag.TwoAxes, 0);
        }

        if (fromColumn == toColumn && fromRow == toRow)
        {
            return (StreetDrag.OneEdge, 1);
        }

        // An east edge is named by the intersection it leaves, so two of them share a line when they
        // share a ROW; two north edges share one when they share a column. ⚠ Same axis and a
        // different perpendicular coordinate is not a line -- it is a dog-leg, and it needs both
        // axes to walk exactly as a diagonal does.
        return fromAxis == StreetAxis.East
            ? fromRow == toRow
                ? (StreetDrag.OneLine, IntegerMath.Abs(toColumn - fromColumn) + 1)
                : (StreetDrag.TwoAxes, 0)
            : fromColumn == toColumn
                ? (StreetDrag.OneLine, IntegerMath.Abs(toRow - fromRow) + 1)
                : (StreetDrag.TwoAxes, 0);
    }

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
    public void Rebuild(RoadNodeTable nodes, RoadSegmentTable segments, BlockLattice lattice)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(lattice);

        Lattice = lattice;
        Span = lattice.Lines;

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

                Bucket(nodes, segments, slot, lattice);
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
    private void Bucket(RoadNodeTable nodes, RoadSegmentTable segments, int slot, BlockLattice lattice)
    {
        if (lattice.Nominal <= 0
            || Span <= 0
            || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
            || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
        {
            return;
        }

        int columnA = lattice.LineAt(nodes.East[a].Raw);
        int rowA = lattice.LineAt(nodes.North[a].Raw);
        int columnB = lattice.LineAt(nodes.East[b].Raw);
        int rowB = lattice.LineAt(nodes.North[b].Raw);

        int spanEast = IntegerMath.Abs(columnB - columnA);
        int spanNorth = IntegerMath.Abs(rowB - rowA);

        // THE MIDPOINT AND NOT AN ENDPOINT, which halves the reach and therefore quarters the block
        // window every query walks. A Segment filed under one end reaches its whole length away; filed
        // under the middle it reaches half, and the window is squared, so this is worth about 3x on a
        // world with Arterials. It is why `reach` is a CEILING of the half-span: an odd span must round
        // up or the far end falls outside the window it is looked for in.
        // ⚠ The midpoint's TILE first and then its block, which is one step where it used to be a
        // divide by twice the block: FloorDiv(a + b, 2B) is only FloorDiv((a + b) / 2, B) because
        // the spacing is one number, and that is exactly the assumption row 25 is removing.
        int column = lattice.LineAt(IntegerMath.FloorDiv(nodes.East[a].Raw + nodes.East[b].Raw, 2));
        int row = lattice.LineAt(IntegerMath.FloorDiv(nodes.North[a].Raw + nodes.North[b].Raw, 2));

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

        column = Lattice.LineAt(east);
        row = Lattice.LineAt(north);

        // ⚠ EXACTLY on a line, which is what makes this a lattice membership test and not a
        // proximity one -- a node one Tile off a line is off the lattice however near it is.
        return east == Lattice.EdgeOf(column)
            && north == Lattice.EdgeOf(row)
            && column >= 0 && column < Span
            && row >= 0 && row < Span;
    }
}
