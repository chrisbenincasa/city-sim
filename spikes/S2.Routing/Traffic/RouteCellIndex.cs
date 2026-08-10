using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Traffic;

/// <summary>
/// The reverse index a wake needs: <b>which stored routes touch this Cell</b>, as a Cell-keyed
/// intrusive index list.
/// </summary>
/// <remarks>
/// <para>
/// <b>Built to the project's own shape rather than to the shape a spike finds convenient.</b>
/// <c>CLAUDE.md</c>: <i>"Every variable-length collection in <c>Borough.Core</c> is an intrusive index
/// list — a head index on the owner, a <c>next</c> index on the element, both in flat arrays. Never a
/// per-entity collection object."</i> A spike that measured a <c>Dictionary&lt;Cell, List&lt;int&gt;&gt;</c>
/// would be measuring a structure the project could not adopt, which is the wrong thing to measure.
/// </para>
/// <para>
/// <b>The one place it departs from the plain form, and why it must.</b> A route passes through many
/// Cells, so a route cannot be an element of the Cell's list — one <c>next</c> per route can only ever
/// belong to one chain. The elements are therefore <b>(route, Cell) memberships</b>, and each membership
/// carries <i>two</i> <c>next</c> indices: one threading its Cell's chain, one threading its route's.
/// The Cell chain answers the wake; the route chain answers the eviction, which would otherwise have to
/// scan the whole index to retire one route. Both heads are flat arrays — <see cref="_cellHead"/> on the
/// Cell and <see cref="_routeHead"/> on the route — and nothing here allocates per entity.
/// </para>
/// <para>
/// <b><c>adr/0006</c>: a collection with a sink.</b> Retired memberships go on a free list and are
/// reissued, so the entry arrays are a running maximum bounded by the largest population the index has
/// ever held rather than by elapsed time. <see cref="Entries"/> and <see cref="HighWater"/> are printed
/// apart for exactly that reason.
/// </para>
/// <para>
/// <b>Unlinking from a Cell chain walks it, and that is the measurement rather than a defect.</b> A
/// singly-linked chain cannot remove an element without finding its predecessor. The alternative — a
/// <c>previous</c> index — is a third flat array, so the choice is bytes against eviction cost and R6.4
/// reports the steps walked so the trade has a number on both sides instead of an argument on one.
/// </para>
/// </remarks>
internal sealed class RouteCellIndex
{
    /// <summary>The empty index, for both heads and the free list.</summary>
    public const int None = -1;

    private readonly RoadGraph _graph;
    private readonly int _cellsPerSide;

    private readonly int[] _cellHead;
    private readonly int[] _routeHead;

    /// <summary>Per-Cell stamp, so one route entering a Cell twice is one membership.</summary>
    private readonly int[] _cellStamp;

    /// <summary>Per-route stamp, so a wake reports each route once however many Cells reach it.</summary>
    private readonly int[] _routeStamp;

    private int[] _entryCell;
    private int[] _entryRoute;
    private int[] _nextInCell;
    private int[] _nextInRoute;

    /// <summary>
    /// The <c>previous</c> index the plain intrusive form does not have. Maintained always and
    /// <b>read only by <see cref="RemoveDoubly"/></b>, so R6.4 can price both sides of the trade in
    /// one run rather than argue one of them: <see cref="Remove"/> walks the Cell chain to find a
    /// predecessor and this array is what removes the walk, at the cost of a fifth flat array.
    /// </summary>
    private int[] _previousInCell;

    private int _highWater;
    private int _live;
    private int _freeHead = None;
    private int _insertGeneration;
    private int _wakeGeneration;

    public RouteCellIndex(RoadGraph graph, int routes, int capacityHint)
    {
        _graph = graph;
        _cellsPerSide = IntegerMath.CeilDiv(graph.Parameters.MapTiles, Units.CellTiles);

        int cells = _cellsPerSide * _cellsPerSide;
        _cellHead = new int[cells];
        _cellStamp = new int[cells];
        _routeHead = new int[routes];
        _routeStamp = new int[routes];

        Array.Fill(_cellHead, None);
        Array.Fill(_routeHead, None);

        int capacity = capacityHint < 1024 ? 1024 : capacityHint;
        _entryCell = new int[capacity];
        _entryRoute = new int[capacity];
        _nextInCell = new int[capacity];
        _nextInRoute = new int[capacity];
        _previousInCell = new int[capacity];
    }

    /// <summary>Cells on a side of the map.</summary>
    public int CellsPerSide => _cellsPerSide;

    /// <summary>Live (route, Cell) memberships.</summary>
    public int Entries => _live;

    /// <summary>The largest number of memberships ever live at once — <c>adr/0006</c>'s bound.</summary>
    public int HighWater => _highWater;

    /// <summary>Chain steps walked by the last <see cref="Remove"/>, which is what an eviction costs.</summary>
    public long LastRemoveSteps { get; private set; }

    /// <summary>Chain steps walked by the last <see cref="Wake"/>, and the Cells it read.</summary>
    public long LastWakeSteps { get; private set; }

    /// <summary>Cells visited by the last <see cref="Wake"/>, including empty ones.</summary>
    public long LastWakeCells { get; private set; }

    /// <summary>
    /// Every array this holds. Reported against <c>RouteStore.ResidentBytes</c>, because an index that
    /// costs more than the store it indexes is not an optimisation of anything.
    /// </summary>
    public long ResidentBytes => SinglyLinkedBytes + ((long)_previousInCell.Length * sizeof(int));

    /// <summary>The same index without the <c>previous</c> array — the plain intrusive form.</summary>
    public long SinglyLinkedBytes =>
        ((long)_cellHead.Length + _cellStamp.Length + _routeHead.Length + _routeStamp.Length
            + _entryCell.Length + _entryRoute.Length + _nextInCell.Length + _nextInRoute.Length)
        * sizeof(int);

    /// <summary>The Cell holding a Tile coordinate pair.</summary>
    public int CellOf(int tileX, int tileY)
    {
        int cx = Clamp(IntegerMath.FloorDiv(tileX, Units.CellTiles), _cellsPerSide - 1);
        int cy = Clamp(IntegerMath.FloorDiv(tileY, Units.CellTiles), _cellsPerSide - 1);
        return (cy * _cellsPerSide) + cx;
    }

    /// <summary>
    /// Threads one stored route into every Cell its arcs pass through.
    /// </summary>
    /// <remarks>
    /// <b>Sampled along the Segment rather than at its endpoints.</b> Most Segments here are one block —
    /// 32 Tiles, exactly a Cell — but an Arterial run between junctions is up to 512, and indexing only
    /// its endpoints would leave a route invisible in fifteen of the sixteen Cells it actually crosses.
    /// The step is half a Cell, which cannot skip a Cell on a straight Segment, and every Segment this
    /// generator draws is straight.
    /// </remarks>
    public void Insert(int route, ReadOnlySpan<int> arcs)
    {
        _insertGeneration++;

        foreach (int arc in arcs)
        {
            int segment = _graph.ArcSegment[arc];
            int a = _graph.SegmentNodeA[segment];
            int b = _graph.SegmentNodeB[segment];

            int ax = _graph.NodeX[a];
            int ay = _graph.NodeY[a];
            int dx = _graph.NodeX[b] - ax;
            int dy = _graph.NodeY[b] - ay;

            int span = IntegerMath.Abs(dx) > IntegerMath.Abs(dy)
                ? IntegerMath.Abs(dx)
                : IntegerMath.Abs(dy);
            int steps = IntegerMath.CeilDiv(span, Units.CellTiles >> 1);
            if (steps < 1)
            {
                steps = 1;
            }

            for (int step = 0; step <= steps; step++)
            {
                int x = ax + IntegerMath.RoundDiv(dx * step, steps);
                int y = ay + IntegerMath.RoundDiv(dy * step, steps);
                int cell = CellOf(x, y);

                if (_cellStamp[cell] == _insertGeneration)
                {
                    continue;
                }

                _cellStamp[cell] = _insertGeneration;
                Link(route, cell);
            }
        }
    }

    /// <summary>
    /// Retires a route by the plain intrusive form: each membership's predecessor is found by walking
    /// its Cell chain. The eviction half of the trade, and the half that is paid on the common path.
    /// </summary>
    public void Remove(int route)
    {
        long steps = 0;
        int entry = _routeHead[route];

        while (entry != None)
        {
            int next = _nextInRoute[entry];
            steps += UnlinkFromCell(entry);

            _nextInCell[entry] = _freeHead;
            _freeHead = entry;
            _live--;

            entry = next;
        }

        _routeHead[route] = None;
        LastRemoveSteps = steps;
    }

    /// <summary>The same eviction with a <c>previous</c> index, so it is <c>O(Cells on the route)</c>.</summary>
    public void RemoveDoubly(int route)
    {
        long steps = 0;
        int entry = _routeHead[route];

        while (entry != None)
        {
            int next = _nextInRoute[entry];
            int previous = _previousInCell[entry];
            int following = _nextInCell[entry];

            if (previous == None)
            {
                _cellHead[_entryCell[entry]] = following;
            }
            else
            {
                _nextInCell[previous] = following;
            }

            if (following != None)
            {
                _previousInCell[following] = previous;
            }

            steps++;

            _nextInCell[entry] = _freeHead;
            _freeHead = entry;
            _live--;

            entry = next;
        }

        _routeHead[route] = None;
        LastRemoveSteps = steps;
    }

    /// <summary>
    /// The wake: every route within <paramref name="radiusCells"/> of any Segment in the gesture,
    /// each reported once.
    /// </summary>
    /// <remarks>
    /// <b>A mark, not a recompute.</b> Session M's second decision, and it is what gives R6.4.3 a
    /// threshold at all: waking a route sets a bit that drains at Trip start on a budget line that
    /// already exists, so the fan-out is priced in marks rather than in searches.
    /// </remarks>
    public int Wake(ReadOnlySpan<int> gestureSegments, int radiusCells, int[] woken)
    {
        _wakeGeneration++;

        int count = 0;
        long steps = 0;
        long cells = 0;

        foreach (int segment in gestureSegments)
        {
            int a = _graph.SegmentNodeA[segment];
            int b = _graph.SegmentNodeB[segment];

            int ax = IntegerMath.FloorDiv(_graph.NodeX[a], Units.CellTiles);
            int ay = IntegerMath.FloorDiv(_graph.NodeY[a], Units.CellTiles);
            int bx = IntegerMath.FloorDiv(_graph.NodeX[b], Units.CellTiles);
            int by = IntegerMath.FloorDiv(_graph.NodeY[b], Units.CellTiles);

            int lowX = Clamp((ax < bx ? ax : bx) - radiusCells, _cellsPerSide - 1);
            int highX = Clamp((ax > bx ? ax : bx) + radiusCells, _cellsPerSide - 1);
            int lowY = Clamp((ay < by ? ay : by) - radiusCells, _cellsPerSide - 1);
            int highY = Clamp((ay > by ? ay : by) + radiusCells, _cellsPerSide - 1);

            for (int cy = lowY; cy <= highY; cy++)
            {
                int row = cy * _cellsPerSide;

                for (int cx = lowX; cx <= highX; cx++)
                {
                    cells++;

                    for (int entry = _cellHead[row + cx]; entry != None; entry = _nextInCell[entry])
                    {
                        steps++;
                        int route = _entryRoute[entry];

                        if (_routeStamp[route] == _wakeGeneration)
                        {
                            continue;
                        }

                        _routeStamp[route] = _wakeGeneration;
                        woken[count++] = route;
                    }
                }
            }
        }

        LastWakeSteps = steps;
        LastWakeCells = cells;
        return count;
    }

    private void Link(int route, int cell)
    {
        int entry = Take();

        _entryCell[entry] = cell;
        _entryRoute[entry] = route;

        _nextInCell[entry] = _cellHead[cell];
        _previousInCell[entry] = None;
        if (_cellHead[cell] != None)
        {
            _previousInCell[_cellHead[cell]] = entry;
        }

        _cellHead[cell] = entry;

        _nextInRoute[entry] = _routeHead[route];
        _routeHead[route] = entry;

        _live++;
    }

    private int UnlinkFromCell(int entry)
    {
        int cell = _entryCell[entry];
        int steps = 1;

        if (_cellHead[cell] == entry)
        {
            _cellHead[cell] = _nextInCell[entry];
            if (_cellHead[cell] != None)
            {
                _previousInCell[_cellHead[cell]] = None;
            }

            return steps;
        }

        int previous = _cellHead[cell];
        while (previous != None && _nextInCell[previous] != entry)
        {
            previous = _nextInCell[previous];
            steps++;
        }

        if (previous != None)
        {
            _nextInCell[previous] = _nextInCell[entry];
            if (_nextInCell[entry] != None)
            {
                _previousInCell[_nextInCell[entry]] = previous;
            }
        }

        return steps;
    }

    private int Take()
    {
        if (_freeHead != None)
        {
            int reused = _freeHead;
            _freeHead = _nextInCell[reused];
            return reused;
        }

        if (_highWater == _entryCell.Length)
        {
            int grown = _entryCell.Length * 2;
            Array.Resize(ref _entryCell, grown);
            Array.Resize(ref _entryRoute, grown);
            Array.Resize(ref _nextInCell, grown);
            Array.Resize(ref _nextInRoute, grown);
            Array.Resize(ref _previousInCell, grown);
        }

        return _highWater++;
    }

    private static int Clamp(int value, int high) =>
        value < 0 ? 0 : value > high ? high : value;
}
