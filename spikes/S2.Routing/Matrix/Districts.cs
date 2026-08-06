using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Matrix;

/// <summary>
/// A partition of the map into Districts, and the node that stands for each one in the matrix.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a District and not a Zone, and <c>plans/0010</c> is owed the correction.</b> That plan
/// says <i>"sweep zone count"</i> in six places, but <c>CONTEXT.md</c> → Zone is <i>"a permission set
/// over land"</i> — what a player may build there — and <c>CONTEXT.md</c> → District is what is
/// actually being swept: <i>"the granularity of the travel-time matrix"</i>. The banned-terms section
/// makes the same assignment from the other side, sending <i>region</i> to <i>District for a
/// Goods-pooling region</i>. Two different objects wearing one word in the document that sizes both is
/// the failure the corpus's vocabulary rule exists to prevent, so R1 spells it District and the
/// spelling reaches the report.
/// </para>
/// <para>
/// <b>Cell-aligned, never Chunk-aligned</b>, per <c>CONTEXT.md</c> → District: <i>"A contiguous set of
/// Cells, never of Chunks. The Cell is frozen and the Chunk is tunable… so boundaries made of Chunks
/// would let a profiler move what a District <b>is</b>."</i> The map is 128 Cells across, so a
/// partition at <c>perSide</c> gives Districts of 128/<c>perSide</c> Cells a side, rounded so the
/// boundaries stay on Cell edges and the ragged remainder is spread rather than dumped on the last
/// row.
/// </para>
/// <para>
/// <b>The sweep brackets the working anchor rather than ranging freely</b>, as the plan requires:
/// <c>CONTEXT.md</c> → District's anchor is <b>128 Cells, 2.10 km²</b>, which on a 128×128-Cell map is
/// <c>perSide ≈ 11</c> — and 121 Districts sits inside <c>plans/0001</c>'s only figure of 100–400.
/// That the anchor and the stale figure agree is worth one line and no more: <c>plans/0001</c>
/// predates the 1M target, so the agreement is a coincidence of arithmetic, not a corroboration.
/// </para>
/// </remarks>
internal sealed class Districts
{
    private readonly int[] _segmentStart;
    private readonly int[] _segmentsByDistrict;

    private Districts(
        int perSide,
        int[] ofNode,
        int[] representative,
        int emptyCount,
        int[] segmentStart,
        int[] segmentsByDistrict)
    {
        PerSide = perSide;
        OfNode = ofNode;
        Representative = representative;
        Empty = emptyCount;
        _segmentStart = segmentStart;
        _segmentsByDistrict = segmentsByDistrict;
    }

    /// <summary>Districts along one edge of the map. <see cref="Count"/> is its square.</summary>
    public int PerSide { get; }

    /// <summary>Districts in the partition, and the matrix's <c>n</c>.</summary>
    public int Count => PerSide * PerSide;

    /// <summary>Node index → District index.</summary>
    public int[] OfNode { get; }

    /// <summary>
    /// District index → the car-reachable node standing for it, or <c>-1</c> if it has none.
    /// </summary>
    /// <remarks>
    /// <b>One representative node, and that is an approximation R1 measures rather than assumes.</b>
    /// A District-to-District entry is one number standing for every Access Point pair inside those
    /// two Districts; the representative is the nearest car-reachable node to the District's centre.
    /// How wrong that is against a true <c>(Segment, offset) → (Segment, offset)</c> search is R1's
    /// correctness axis, and it is the figure that decides whether the abstraction holds at all —
    /// separately from, and prior to, how fast the read is.
    /// </remarks>
    public int[] Representative { get; }

    /// <summary>
    /// Districts with no car-reachable node. Reported rather than silently dropped: an empty District
    /// is a row of unreachables in the matrix and it changes every average taken over it.
    /// </summary>
    public int Empty { get; }

    /// <summary>Cells on one side of a District, at the coarse end of the ragged split.</summary>
    public int CellsPerSide =>
        IntegerMath.CeilDiv(IntegerMath.FloorDiv(Units.MapTiles, Units.CellTiles), PerSide);

    public static Districts Partition(RoadGraph graph, int perSide)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(perSide);

        int cellsAcross = IntegerMath.FloorDiv(graph.Parameters.MapTiles, Units.CellTiles);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(perSide, cellsAcross);

        var ofNode = new int[graph.Nodes];
        var representative = new int[perSide * perSide];
        var bestDistance = new int[perSide * perSide];

        Array.Fill(representative, -1);
        Array.Fill(bestDistance, int.MaxValue);

        for (int node = 0; node < graph.Nodes; node++)
        {
            int column = Bucket(graph.NodeX[node], cellsAcross, perSide);
            int row = Bucket(graph.NodeY[node], cellsAcross, perSide);
            int district = (row * perSide) + column;
            ofNode[node] = district;

            if (!CarReachable(graph, node))
            {
                continue;
            }

            // Chebyshev to the District's centre. The metric is immaterial — it only ranks candidates
            // within one District — and it is the one R0 chose for the search, so no second metric
            // enters the spike.
            int centreX = CentreTile(column, cellsAcross, perSide);
            int centreY = CentreTile(row, cellsAcross, perSide);
            int dx = IntegerMath.Abs(graph.NodeX[node] - centreX);
            int dy = IntegerMath.Abs(graph.NodeY[node] - centreY);
            int distance = dx > dy ? dx : dy;

            // Ties break on the lower node index, so a partition is the same partition forever.
            if (distance < bestDistance[district])
            {
                bestDistance[district] = distance;
                representative[district] = node;
            }
        }

        int empty = 0;
        foreach (int node in representative)
        {
            if (node < 0)
            {
                empty++;
            }
        }

        BuildSegmentIndex(graph, ofNode, perSide * perSide, out var start, out var byDistrict);

        return new Districts(perSide, ofNode, representative, empty, start, byDistrict);
    }

    /// <summary>
    /// The car Segments lying in a District, as a span into a CSR index.
    /// </summary>
    /// <remarks>
    /// <b>This replaced a rejection sampler, and the replacement is a correction rather than a
    /// tidy-up.</b> R1.6 draws Access Points inside a named District; the first harness drew
    /// uniformly over the map and rejected misses, which at 1,024 Districts hits one draw in a
    /// thousand — so a bounded draw count silently reduced the sample to nine searches and the row
    /// was published beside rows built from two thousand. **A sample that shrinks with the swept axis
    /// makes a trend out of its own survivorship**, which is the defect R0.5 named in its own
    /// mean-cost column and is the third time this corpus has met it. An index costs one pass and
    /// cannot collapse.
    /// </remarks>
    public ReadOnlySpan<int> SegmentsIn(int district) =>
        _segmentsByDistrict.AsSpan(
            _segmentStart[district], _segmentStart[district + 1] - _segmentStart[district]);

    private static void BuildSegmentIndex(
        RoadGraph graph, int[] ofNode, int count, out int[] start, out int[] byDistrict)
    {
        // A Segment belongs to the District of its A endpoint. A Segment straddling a boundary is
        // therefore attributed to one side, which is the same convention the node partition uses and
        // is immaterial at every rung: a Segment is 32 Tiles and the finest District is 4 Cells.
        start = new int[count + 1];

        for (int segment = 0; segment < graph.Segments; segment++)
        {
            if ((graph.SegmentModes[segment] & (byte)Modes.Car) == 0)
            {
                continue;
            }

            start[ofNode[graph.SegmentNodeA[segment]] + 1]++;
        }

        for (int district = 0; district < count; district++)
        {
            start[district + 1] += start[district];
        }

        byDistrict = new int[start[count]];
        var cursor = (int[])start.Clone();

        for (int segment = 0; segment < graph.Segments; segment++)
        {
            if ((graph.SegmentModes[segment] & (byte)Modes.Car) == 0)
            {
                continue;
            }

            byDistrict[cursor[ofNode[graph.SegmentNodeA[segment]]]++] = segment;
        }
    }

    /// <summary>Land area of one District in hundredths of a km², for the anchor comparison.</summary>
    public int AreaHundredthsSquareKilometres(RoadGraph graph)
    {
        // A Tile is ~4 m — `05 §26`, "268 km² (4096² Tiles @ ~4 m)" — so 16 m² per Tile.
        long metres = (long)graph.Parameters.MapTiles * graph.Parameters.MapTiles * 16;
        return (int)IntegerMath.FloorDiv(metres * 100, 1_000_000L * Count);
    }

    /// <summary>Cells in one District, for the comparison against the 128-Cell anchor.</summary>
    public int CellsEach(RoadGraph graph)
    {
        int cells = IntegerMath.FloorDiv(graph.Parameters.MapTiles, Units.CellTiles);
        return IntegerMath.RoundDiv(cells * cells, Count);
    }

    private static int Bucket(int tile, int cellsAcross, int perSide)
    {
        int cell = IntegerMath.FloorDiv(tile, Units.CellTiles);
        int bucket = IntegerMath.FloorDiv(cell * perSide, cellsAcross);
        return bucket < 0 ? 0 : bucket >= perSide ? perSide - 1 : bucket;
    }

    private static int CentreTile(int bucket, int cellsAcross, int perSide)
    {
        int lowCell = IntegerMath.CeilDiv(bucket * cellsAcross, perSide);
        int highCell = IntegerMath.CeilDiv((bucket + 1) * cellsAcross, perSide);
        return IntegerMath.RoundDiv((lowCell + highCell) * Units.CellTiles, 2);
    }

    private static bool CarReachable(RoadGraph graph, int node)
    {
        for (int arc = graph.ArcStart[node]; arc < graph.ArcStart[node + 1]; arc++)
        {
            if (graph.ArcCarTicks[arc] != RoadGraph.Impassable)
            {
                return true;
            }
        }

        return false;
    }
}
