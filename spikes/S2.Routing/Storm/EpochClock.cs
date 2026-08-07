using S2.Routing.Cluster;
using S2.Routing.Graph;

namespace S2.Routing.Storm;

/// <summary>
/// Which revalidation test a cached route is checked against.
/// </summary>
/// <remarks>
/// <para>
/// <b>All three rungs are conservative, which is what makes this an optimisation rather than a
/// design change.</b> A revalidated route is recomputed deterministically over the same graph and
/// comes back identical, so the rungs differ only in how often they say <i>might be stale</i>. Over-
/// invalidation costs work and never correctness, and the State Hash is unchanged across all three —
/// which under <c>05 §4</c>'s own test is the definition of an optimisation, and optimisations are
/// settled by measurement rather than by argument.
/// </para>
/// <para>
/// <b>The corpus's phrase conflates two things and R5 has to separate them.</b>
/// <c>CONTEXT.md</c> → Epoch says cached routes <i>"revalidate lazily on next use, never a global
/// flush"</i>. <b><c>Lazy</c> describes when you pay, not what survives.</b> Under
/// <see cref="Global"/> nothing survives an edit — the flush is total, merely paid on next use
/// instead of at the edit — so a design that ships the global rung has a global flush by any name
/// that matters, and <c>plans/0010</c>'s tripwire fires on it.
/// </para>
/// </remarks>
internal enum EpochRung
{
    /// <summary>
    /// One counter on the whole Road Graph, as the corpus writes it today. O(1) to check, and the
    /// hit rate collapses on any edit anywhere.
    /// </summary>
    Global,

    /// <summary>
    /// A version per cluster, riding <c>adr/0040</c>'s partition. A route is stale if any cluster it
    /// crosses moved. O(clusters crossed), and over-invalidates a route that merely passes near an
    /// edit.
    /// </summary>
    PerCluster,

    /// <summary>
    /// A version per Segment. A route is stale if any of its own Segments moved. O(path length), and
    /// <b>exact under deletion only</b> — see <see cref="EditStorm"/>, which is where the asymmetry
    /// is argued.
    /// </summary>
    PerSegment,
}

/// <summary>
/// The three version counters an edit moves, kept side by side so one storm drives every rung.
/// </summary>
/// <remarks>
/// <b>Storage decides nothing here and it is worth saying so before the numbers.</b>
/// <c>plans/0010</c>: a version word per Segment is 33,018 × 4 B ≈ <b>129 KiB</b>, against a world
/// of 172.3 MiB. The comparison is hit rate against revalidation cost, which only the edit storm can
/// drive — nobody is choosing a rung to save 129 KiB.
/// </remarks>
internal sealed class EpochClock
{
    private readonly RoadGraph _graph;
    private readonly Clusters _clusters;

    public EpochClock(RoadGraph graph, Clusters clusters)
    {
        _graph = graph;
        _clusters = clusters;
        ClusterVersion = new int[clusters.Count];
        SegmentVersion = new int[graph.Segments];
    }

    /// <summary>The single counter the corpus describes today.</summary>
    public int Global { get; private set; }

    public int[] ClusterVersion { get; }

    public int[] SegmentVersion { get; }

    public long ResidentBytes =>
        ((long)ClusterVersion.Length + SegmentVersion.Length) * sizeof(int);

    /// <summary>Moves every counter one edit gesture implies.</summary>
    public void Bump(Gesture gesture)
    {
        Global++;

        foreach (int segment in gesture.Segments)
        {
            SegmentVersion[segment]++;
            ClusterVersion[_clusters.OfNode[_graph.SegmentNodeA[segment]]]++;
            ClusterVersion[_clusters.OfNode[_graph.SegmentNodeB[segment]]]++;
        }
    }
}
