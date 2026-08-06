using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Cluster;

/// <summary>
/// The pathfinding cluster grid: a square of whole Chunks, and the partition HPA\* is built over.
/// </summary>
/// <remarks>
/// <para>
/// <b>A cluster, not the Chunk.</b>
/// <c>adr/0040</c> splits the two: the Chunk is in the save and is therefore permanent, while the
/// abstract graph is <c>(derived AND rebuilt)</c> and costs a recomputation to change <i>forever</i>.
/// The cluster is constrained to a whole number of Chunks and sized independently, which is why R3
/// decides cluster size outright and only informs the Chunk.
/// </para>
/// <para>
/// <b>The alignment constraint is load-bearing and is enforced here rather than assumed.</b>
/// <c>adr/0040</c>: <i>"A cluster that is not a whole number of Chunks reintroduces every boundary
/// disagreement <c>05 §5</c> unified away, and it would do so silently."</i> A rung whose cluster
/// does not tile the map exactly is rejected in <see cref="Partition"/>, so the sweep cannot quietly
/// include one.
/// </para>
/// <para>
/// <b>Distinct from <c>Districts</c>, which is R1's partition and a different object.</b> A District
/// is the granularity of the travel-time matrix and is Cell-aligned because a Chunk is tunable; a
/// cluster is Chunk-aligned by construction. They are swept separately and nothing here reads that
/// one.
/// </para>
/// </remarks>
internal sealed class Clusters
{
    private readonly int[] _start;
    private readonly int[] _nodes;

    private Clusters(int chunksPerSide, int perSide, int[] ofNode, int[] start, int[] nodes)
    {
        ChunksPerSide = chunksPerSide;
        PerSide = perSide;
        OfNode = ofNode;
        _start = start;
        _nodes = nodes;
    }

    /// <summary>Chunks along one side of a cluster. <b>The swept axis.</b></summary>
    public int ChunksPerSide { get; }

    /// <summary>Tiles along one side of a cluster.</summary>
    public int TilesPerSide => ChunksPerSide * Units.ChunkTiles;

    /// <summary>Clusters along one side of the map.</summary>
    public int PerSide { get; }

    /// <summary>Clusters in the partition.</summary>
    public int Count => PerSide * PerSide;

    /// <summary>Node index → cluster index.</summary>
    public int[] OfNode { get; }

    /// <summary>The nodes lying in a cluster, in node order, as a span into a CSR index.</summary>
    public ReadOnlySpan<int> NodesIn(int cluster) =>
        _nodes.AsSpan(_start[cluster], _start[cluster + 1] - _start[cluster]);

    /// <summary>Nodes in the largest cluster — the bound on one insertion search's work.</summary>
    public int LargestCluster
    {
        get
        {
            int largest = 0;
            for (int cluster = 0; cluster < Count; cluster++)
            {
                int size = _start[cluster + 1] - _start[cluster];
                if (size > largest)
                {
                    largest = size;
                }
            }

            return largest;
        }
    }

    public static Clusters Partition(RoadGraph graph, int chunksPerSide)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunksPerSide);

        int tilesPerSide = chunksPerSide * Units.ChunkTiles;
        int perSide = IntegerMath.FloorDiv(graph.Parameters.MapTiles, tilesPerSide);

        if (perSide < 1 || perSide * tilesPerSide != graph.Parameters.MapTiles)
        {
            // adr/0040's alignment constraint, which "belongs with the world-creation constants,
            // validated where TICKS_PER_DAY and WHEEL_SIZE are". A ragged rung would put a boundary
            // where no Chunk boundary is, and every figure taken at that rung would be a figure
            // about a partition the design forbids.
            throw new ArgumentOutOfRangeException(
                nameof(chunksPerSide),
                chunksPerSide,
                "A cluster must tile the map in whole Chunks (adr/0040).");
        }

        var ofNode = new int[graph.Nodes];
        var start = new int[(perSide * perSide) + 1];

        for (int node = 0; node < graph.Nodes; node++)
        {
            int column = Bucket(graph.NodeX[node], tilesPerSide, perSide);
            int row = Bucket(graph.NodeY[node], tilesPerSide, perSide);
            int cluster = (row * perSide) + column;
            ofNode[node] = cluster;
            start[cluster + 1]++;
        }

        for (int cluster = 0; cluster < perSide * perSide; cluster++)
        {
            start[cluster + 1] += start[cluster];
        }

        var nodes = new int[graph.Nodes];
        var cursor = (int[])start.Clone();

        for (int node = 0; node < graph.Nodes; node++)
        {
            nodes[cursor[ofNode[node]]++] = node;
        }

        return new Clusters(chunksPerSide, perSide, ofNode, start, nodes);
    }

    private static int Bucket(int tile, int tilesPerSide, int perSide)
    {
        int bucket = IntegerMath.FloorDiv(tile, tilesPerSide);
        return bucket < 0 ? 0 : bucket >= perSide ? perSide - 1 : bucket;
    }
}
