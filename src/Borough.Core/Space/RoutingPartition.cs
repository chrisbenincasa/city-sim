using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The <b>routing partition</b> — a square tiling of the map in Cells, over which the travel-time
/// matrix is keyed (<c>adr/0040</c>, <c>adr/0047</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is not the District, and keeping the two apart is the whole of <c>adr/0047</c>.</b> A
/// District is <em>the boundary within which Goods pool without physical transport</em>, sized by
/// what pools convincingly — a playtesting question with a working anchor of 128 Cells. This is a
/// routing structure sized by what a matrix entry can be wrong by. They were one object because
/// <c>adr/0014</c> asserted the identity rather than arguing it, and <em>a constant welded to two
/// decisions is governed by whichever of them is louder</em>. Redrawing a District now changes what
/// pools and never what a Traveller drives.
/// </para>
/// <para>
/// <b>This pays <c>adr/0040</c>'s owed correction, and the constructor is where it is paid.</b> That
/// ADR sized the cluster as a whole number of <em>Chunks</em> and never ran <c>05 §4</c>'s hash test
/// on the dependency that creates: the Chunk is declared <em>tuning, hash-preserving</em>, so a
/// routing structure whose size is <c>k</c> Chunks lets a profiler turning a hash-preserving knob
/// change the city. The size is therefore a multiple of the <b>frozen Cell</b>, and Chunk size is
/// constrained to <b>divide</b> it — the arrow that ADR drew, reversed. Both constraints are checked
/// here rather than commented, because an alignment rule that is not enforced fails silently and
/// <c>05 §5</c> says exactly that about every boundary the Cell/Chunk split unified away.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c>, and it clears <c>05 §3</c>'s bar by a wider margin than the
/// structures it sits beside.</b> The rule is that a derived structure earns the classification only
/// if its <em>order</em> is recoverable from saved state and not merely its membership.
/// <see cref="BuildingResidency"/> and <see cref="Movement.CommuteRoster"/> earn it by inserting in
/// ascending slot order; this earns it without needing to, because partitions are numbered in
/// <b>grid order</b> — row-major over the tiling — so the numbering is a function of <em>which
/// partitions hold a node</em> and not of the order any node was created in. A first-touch numbering
/// in slot order would also be reproducible, and it would tie the matrix's row order to the free
/// list. Grid order does not.
/// </para>
/// <para>
/// <b>Not a <c>[Table]</c> and it must never join <c>World._tables</c>.</b> <see cref="RoadArcs"/>
/// and <see cref="RoadConnectivity"/> are the precedent and <c>plans/0020</c> records the sharp
/// reason: <c>Rows.Fold</c> folds the allocator's four scalars before consulting any column's
/// disposition, so a wholly-derived table would hash its own rebuild count and two identical cities
/// would disagree. It is owned by <see cref="RoadGraph"/> and rebuilt by
/// <see cref="RoadGraph.RebuildDerived"/> beside the components.
/// </para>
/// <para>
/// <b>⚠ Nothing reads it yet, so building it moves no State Hash — and it is
/// <em>prospectively</em> hash-bearing, not hash-neutral.</b> The moment 5c task 2's matrix reaches
/// a decision — which job is reachable, which route is taken — this number changes the city. That is
/// <see cref="CellGrid.WorldTiles"/>'s trap seen coming rather than after the fact: <c>05 §4</c>'s
/// test asks whether a change moves <em>this</em> city, and a structure with no consumer moves no
/// city whatever it decides. Do not read this task's clean baselines as evidence the size is free.
/// </para>
/// </remarks>
public sealed class RoutingPartition
{
    /// <summary>A node in no partition — a freed slot, or a coordinate off the map.</summary>
    public const int None = -1;

    private readonly int _edgeCells;
    private readonly int _edgeShift;
    private readonly int _side;

    /// <summary>Dense id plus one, so a zeroed array reads as <em>empty</em> rather than as id 0.</summary>
    /// <remarks>
    /// The encoding <see cref="BuildingResidency"/> and <see cref="CellResidency"/> both use, for the
    /// reason both give: the empty state has to be the default value, or the structure needs an
    /// initialisation pass that somebody will eventually forget.
    /// </remarks>
    private readonly int[] _dense;

    private readonly int[] _east;
    private readonly int[] _north;

    /// <summary>
    /// The access node of each partition, one lane per mode — see <see cref="AccessNode"/>.
    /// </summary>
    /// <remarks>
    /// <b>Two lanes in one array rather than an array per mode</b>, because the mode count is fixed
    /// at two by <see cref="TravelMode"/> and a jagged structure would be a reference type inside a
    /// class the determinism rules already keep flat.
    /// </remarks>
    private readonly int[] _access;

    /// <summary>
    /// The best distance seen so far for each partition's access node, while a rebuild is running.
    /// </summary>
    /// <remarks>
    /// <b>A field rather than a local, because a rebuild runs on every road edit.</b>
    /// <see cref="RoadGraph.LayStreet"/> and <see cref="RoadGraph.BulldozeStreet"/> both call
    /// <see cref="RoadGraph.RebuildDerived"/>, and a road edit is the player's core verb rather than
    /// a rare event — so a local array here would allocate once per click for the lifetime of a
    /// session. <see cref="RoadConnectivity"/> keeps its union-find scratch for the same reason.
    /// </remarks>
    private readonly int[] _nearest;

    /// <param name="edge">
    /// The partition's edge, in Cells. A power of two, at least one Chunk, and no larger than the map.
    /// </param>
    public RoutingPartition(Cells edge)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(edge.Raw, ChunkGrid.CellsPerChunk, nameof(edge));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(edge.Raw, CellGrid.WorldCells, nameof(edge));

        if ((edge.Raw & (edge.Raw - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(edge),
                edge.Raw,
                "The routing partition's edge must be a power of two, so that every Cell-to-partition "
                + "conversion is a shift and no boundary can disagree with the Cell grid's.");
        }

        _edgeCells = edge.Raw;
        _edgeShift = Shift(edge.Raw);
        _side = IntegerMath.ShiftRight(CellGrid.WorldCells, _edgeShift);

        _dense = new int[_side * _side];
        _east = new int[_side * _side];
        _north = new int[_side * _side];
        _access = new int[_side * _side * Modes];
        _nearest = new int[_side * _side * Modes];
    }

    /// <summary>How many travel modes carry an access node. <see cref="TravelMode"/>'s two.</summary>
    private const int Modes = 2;

    /// <summary>
    /// The size this world's partition is built at. <b>4 Cells — 128 Tiles, 512 m.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠ Provisional and UNRATIFIED, with its ratifier named per <c>adr/0052</c>: 5c task 2's
    /// in-engine entry-error measurement.</b> It is chosen rather than derived, and the whole reason
    /// it may be chosen now is that this structure is <c>(derived AND rebuilt)</c> — moving it costs
    /// a recomputation and never a save migration, which is the asymmetry <c>adr/0040</c> was written
    /// on. Task 2 builds the matrix, at which point entry error is measurable against a real search,
    /// in a real world, on the draw the commute generator actually produces.
    /// </para>
    /// <para>
    /// <b>⚠ Do not read this off S2 R1's 24.70%–3.80% entry-error curve without re-denominating it
    /// three times, and the caveat <c>plans/0002</c> §D2 carries against that curve is itself
    /// wrong.</b> §D2 says the sweep was measured <em>"with the store in the denominator"</em>; it
    /// was not — <c>MatrixReport.MeasureError</c> compares a matrix entry against a real A\* search's
    /// cost and divides by that same per-query cost, while the route store is a separate size table
    /// that never touches it. What the curve <em>is</em> disqualified by is three things nobody wrote
    /// down: it is on a <b>uniform</b> origin-destination draw, which S2 R4 measured is a different
    /// city from a local one; its absolute Ticks are <b>pre-<c>adr/0094</c></b>, at 8192 Ticks a Day
    /// against today's 2048; and its costs are <b>car</b> times, while every Commute Budget rung in
    /// the build is a <b>foot</b> percentile. <c>plans/0012</c> <b>Cause 5</b>, landing on a Cause 5
    /// entry.
    /// </para>
    /// <para>
    /// <b>What 4 buys, stated so task 2 can refute it.</b> Re-denominated onto a comparable paved
    /// extent it is the rung R1 measured best — a 1M city paves 4,800 Tiles, giving 38×38 = 1,444
    /// partitions and an 8.3 MB matrix, against the ~4.2 MiB at 1,024² <c>plans/0026</c> prices. It
    /// is also the smallest rung that is not degenerate on the fixtures anybody runs: the golden
    /// world paves 320 Tiles, which is 3×3 partitions here and <b>2×2 at 8 Cells</b> — a matrix with
    /// four rows cannot be wrong in any way a test could see.
    /// </para>
    /// <para>
    /// <b>⚠ And the tension task 2 has to resolve is a mode tension, which no spike reading can
    /// settle because S2 was car-only.</b> A matrix entry's error is a fixed <em>distance</em> —
    /// bounded by half a partition's diagonal — and therefore a mode-dependent <em>time</em>: 362 m
    /// at this size is about 4.3 minutes on foot at 5 km/h, or 21% of <c>adr/0095</c>'s fast rung,
    /// against roughly 0.4 minutes by car at 50 km/h. <b>A partition that serves a car matrix is an
    /// order of magnitude too coarse for a foot one</b>, and halving the edge quadruples the matrix.
    /// The refuting readings are named in <c>plans/0002</c> §D2 in both directions.
    /// </para>
    /// </remarks>
    public static Cells DesignEdge => new(4);

    /// <summary>How many partitions hold at least one node. The matrix's order.</summary>
    /// <remarks>
    /// <b>Occupied partitions, never every partition on the map, and the difference is three orders
    /// of magnitude.</b> A 512-Cell map holds 16,384 partitions at <see cref="DesignEdge"/> and a 1M
    /// city occupies about 1,444 of them, so a matrix over the map would be 1.07 GB where a matrix
    /// over the city is 8.3 MB. That is <c>adr/0021</c>'s <em>scale with developed area, not map
    /// area</em> — the rule <c>RoadGenerator</c> broke and <c>plans/0003</c> queue item 6 repaired,
    /// arriving one structure further on.
    /// </remarks>
    public int Count { get; private set; }

    /// <summary>The partition's edge, in Cells.</summary>
    public Cells Edge => new(_edgeCells);

    /// <summary>Partitions across the map, on one axis.</summary>
    public int Side => _side;

    /// <summary>The east coordinate of a partition, in partitions.</summary>
    public int EastOf(int partition) => _east[partition];

    /// <summary>The north coordinate of a partition, in partitions.</summary>
    public int NorthOf(int partition) => _north[partition];

    /// <summary>
    /// The node a search into or out of this partition starts from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The live node nearest the partition's geometric centre by Chebyshev distance, ties to the
    /// lower slot</b> — the rule S2's harness used, kept because it is cheap, deterministic and has
    /// no free parameter. Chebyshev rather than Euclidean because it needs no square root and the
    /// tiling is square, so the two disagree only about which of several near-central nodes wins.
    /// </para>
    /// <para>
    /// <b>⚠ One per mode, and task 1 shipped it mode-agnostic before task 2 found that cannot
    /// work.</b> The node nearest a partition's centre may be an Arterial junction, whose Arcs carry
    /// <see cref="TravelMode.Car"/> and not <see cref="TravelMode.Foot"/> (<c>adr/0072</c>) — so a
    /// foot matrix row anchored on it would settle nothing and report the partition **severed from
    /// the entire city**, which is the one reading this whole structure exists to keep honest. The
    /// candidate set is therefore filtered to nodes with at least one Arc admitting the mode, and the
    /// nearest-centre rule then applies within it. **A partition with nodes but none of them
    /// traversable in a mode has no access node in that mode**, which is a real state — a
    /// pedestrianised block holds no car anchor — and reads as <see cref="Tables.Rows.NoSlot"/>
    /// rather than as slot 0.
    /// </para>
    /// </remarks>
    public int AccessNode(int partition, TravelMode mode) =>
        _access[Anchor(partition, mode)];

    /// <summary>The partition a Tile coordinate falls in, or <see cref="None"/> when it is off the map.</summary>
    public int At(Tiles east, Tiles north)
    {
        Cells cellEast = CellGrid.ToCells(east);
        Cells cellNorth = CellGrid.ToCells(north);

        if (!CellGrid.Contains(cellEast, cellNorth))
        {
            return None;
        }

        int index = Index(
            IntegerMath.ShiftRight(cellEast.Raw, _edgeShift),
            IntegerMath.ShiftRight(cellNorth.Raw, _edgeShift));

        return _dense[index] - 1;
    }

    /// <summary>The partition a node sits in, or <see cref="None"/> for a freed or off-map node.</summary>
    public int Of(RoadNodeTable nodes, int node)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        return nodes.Rows.IsLive(node) ? At(nodes.East[node], nodes.North[node]) : None;
    }

    /// <summary>
    /// Recomputes the tiling from the nodes standing on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three passes and no allocation, and the order of the three is the determinism argument.</b>
    /// The first marks which partitions hold a live node; the second numbers the marked ones in grid
    /// order, which is what makes the numbering independent of the node free list; the third walks
    /// the nodes once more in slot order to settle each access node, where a strict comparison makes
    /// the lowest slot win a tie. Collapsing the first two into a first-touch numbering would save a
    /// pass over 16,384 <c>int</c>s and would tie the matrix's row order to the order the player laid
    /// road in.
    /// </para>
    /// <para>
    /// <b>An unreached node still partitions.</b> A node no Segment touches is a real state — the
    /// generator lays a lattice and a bulldoze can strand a junction — and excluding it here would
    /// make the partition's membership depend on the Arcs, which are themselves derived from the
    /// Segments. Reachability is a question the matrix answers, not one the tiling should
    /// pre-empt; that is <see cref="BuildingResidency"/>'s two-stage rule on a second object.
    /// </para>
    /// </remarks>
    public void Rebuild(RoadNodeTable nodes, RoadArcs arcs)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(arcs);

        Array.Clear(_dense);
        Count = 0;

        int slots = nodes.Rows.SlotCount;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!nodes.Rows.IsLive(slot))
            {
                continue;
            }

            if (Grid(nodes, slot, out int east, out int north))
            {
                _dense[Index(east, north)] = 1;
            }
        }

        for (int north = 0; north < _side; north++)
        {
            for (int east = 0; east < _side; east++)
            {
                int index = Index(east, north);

                if (_dense[index] == 0)
                {
                    continue;
                }

                _dense[index] = ++Count;
                _east[Count - 1] = east;
                _north[Count - 1] = north;

                for (int lane = 0; lane < Modes; lane++)
                {
                    _access[(lane * _side * _side) + Count - 1] = Tables.Rows.NoSlot;
                    _nearest[(lane * _side * _side) + Count - 1] = int.MaxValue;
                }
            }
        }

        for (int slot = 0; slot < slots; slot++)
        {
            if (!nodes.Rows.IsLive(slot) || !Grid(nodes, slot, out int east, out int north))
            {
                continue;
            }

            int partition = _dense[Index(east, north)] - 1;
            int distance = ToCentre(nodes, slot, east, north);

            for (int lane = 0; lane < Modes; lane++)
            {
                int anchor = (lane * _side * _side) + partition;

                if (distance >= _nearest[anchor] || !Admits(nodes, arcs, slot, Mode(lane)))
                {
                    continue;
                }

                _nearest[anchor] = distance;
                _access[anchor] = slot;
            }
        }
    }

    /// <summary>Whether any Arc leaving a node admits a mode — the anchor's eligibility test.</summary>
    /// <remarks>
    /// <b>Leaving rather than touching, and a one-way street is why the difference is real.</b> An
    /// anchor is where a one-to-all search <em>starts</em>, so a node every Arc of which points
    /// inward is useless as one however well connected it looks. The adjacency is directed
    /// (<c>adr/0072</c>), so this is the correct half to test and it is the cheap one — a node's Arcs
    /// are a contiguous slice.
    /// </remarks>
    private static bool Admits(RoadNodeTable nodes, RoadArcs arcs, int node, TravelMode mode)
    {
        int start = nodes.ArcStart[node];
        int count = nodes.ArcCount[node];

        for (int i = start; i < start + count; i++)
        {
            if (arcs.Admits(i, mode))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The mode a lane of <see cref="_access"/> holds.</summary>
    private static TravelMode Mode(int lane) => lane == 0 ? TravelMode.Foot : TravelMode.Car;

    /// <summary>The lane of <see cref="_access"/> a partition's anchor sits in, for a mode.</summary>
    private int Anchor(int partition, TravelMode mode) =>
        (mode == TravelMode.Foot ? 0 : _side * _side) + partition;

    /// <summary>The Chebyshev distance in Tiles from a node to its partition's centre.</summary>
    private int ToCentre(RoadNodeTable nodes, int slot, int east, int north)
    {
        int edgeTiles = IntegerMath.ShiftLeft(_edgeCells, CellGrid.TilesPerCellShift);
        int half = IntegerMath.ShiftRight(edgeTiles, 1);

        int centreEast = (east * edgeTiles) + half;
        int centreNorth = (north * edgeTiles) + half;

        int deltaEast = IntegerMath.Abs(nodes.East[slot].Raw - centreEast);
        int deltaNorth = IntegerMath.Abs(nodes.North[slot].Raw - centreNorth);

        return deltaEast > deltaNorth ? deltaEast : deltaNorth;
    }

    /// <summary>The partition coordinates of a node, or false when it stands off the map.</summary>
    private bool Grid(RoadNodeTable nodes, int slot, out int east, out int north)
    {
        Cells cellEast = CellGrid.ToCells(nodes.East[slot]);
        Cells cellNorth = CellGrid.ToCells(nodes.North[slot]);

        if (!CellGrid.Contains(cellEast, cellNorth))
        {
            east = 0;
            north = 0;

            return false;
        }

        east = IntegerMath.ShiftRight(cellEast.Raw, _edgeShift);
        north = IntegerMath.ShiftRight(cellNorth.Raw, _edgeShift);

        return true;
    }

    /// <summary>The row-major index of a partition on the map. North-major, as the Cell grid is.</summary>
    private int Index(int east, int north) => (north * _side) + east;

    /// <summary>The power of two a value is. The caller has already established that it is one.</summary>
    private static int Shift(int value)
    {
        int shift = 0;

        while (IntegerMath.ShiftLeft(1, shift) != value)
        {
            shift++;
        }

        return shift;
    }
}
