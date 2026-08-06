using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Routing;

/// <summary>
/// Draws the origin-destination pairs the denominator is measured over.
/// </summary>
/// <remarks>
/// <para>
/// <b>R0 cannot have the distribution it is supposed to use, and pretending otherwise would be the
/// worse error.</b> <c>plans/0010</c> says the denominator is measured <i>"over the O-D distribution
/// R1 derives"</i>, and R1 has not run. Inventing a plausible commute-length distribution here would
/// bake a guess into every ratio the spike publishes, and it would be invisible afterwards because
/// the number would look like a measurement.
/// </para>
/// <para>
/// <b>So R0 samples uniformly and reports every figure bucketed by origin-destination distance.</b>
/// Whatever distribution R1 derives is then applied as <i>weights over buckets that already exist</i>,
/// with no re-run and no re-measurement. That is strictly better than guessing: it makes R1's result
/// compose with R0's instead of invalidating it, and it means the shape of the cost-against-distance
/// curve — which is the part that actually tells you whether the router scales — is available now.
/// </para>
/// <para>
/// <b>Walks and drives are drawn differently, because they are different queries.</b> The plan is
/// explicit that their cost profiles are nothing alike: <i>"walks are short searches over a dense
/// local subgraph at high count, drives are long searches over a sparse global one at lower count."</i>
/// A walk Leg is drawn within a radius; a drive is drawn across the map.
/// </para>
/// </remarks>
internal sealed class OdSampler
{
    /// <summary>Draws before a bounded walk gives up and takes what it has. Reported, never silent.</summary>
    private const int WalkDrawLimit = 256;

    private readonly RoadGraph _graph;
    private readonly int[] _midX;
    private readonly int[] _midY;
    private readonly int[] _carSegments;
    private readonly int[] _footSegments;

    public OdSampler(RoadGraph graph)
    {
        _graph = graph;
        _midX = new int[graph.Segments];
        _midY = new int[graph.Segments];

        var car = new List<int>();
        var foot = new List<int>();

        for (int segment = 0; segment < graph.Segments; segment++)
        {
            int a = graph.SegmentNodeA[segment];
            int b = graph.SegmentNodeB[segment];
            _midX[segment] = IntegerMath.RoundDiv(graph.NodeX[a] + graph.NodeX[b], 2);
            _midY[segment] = IntegerMath.RoundDiv(graph.NodeY[a] + graph.NodeY[b], 2);

            if ((graph.SegmentModes[segment] & (byte)Modes.Car) != 0)
            {
                car.Add(segment);
            }

            if ((graph.SegmentModes[segment] & (byte)Modes.Foot) != 0)
            {
                foot.Add(segment);
            }
        }

        _carSegments = [.. car];
        _footSegments = [.. foot];
    }

    /// <summary>Segments a mode may use at all. The denominator of "how local are walk Legs".</summary>
    public int Available(Modes mode) => Pool(mode).Length;

    public AccessPoint Origin(ulong seed, ulong query, Modes mode)
    {
        int[] pool = Pool(mode);
        int segment = pool[CounterHash.Below(
            CounterHash.Of(seed, query, (ulong)mode, CounterHash.Purpose.OriginSegment), pool.Length)];

        return new AccessPoint(segment, Offset(seed, query, mode, segment, CounterHash.Purpose.OriginOffset));
    }

    /// <summary>
    /// A destination. <paramref name="radiusTiles"/> of zero draws across the whole map; a positive
    /// radius rejection-samples for a destination within it, which is how a walk Leg is drawn.
    /// </summary>
    public AccessPoint Destination(ulong seed, ulong query, Modes mode, AccessPoint origin, int radiusTiles)
    {
        int[] pool = Pool(mode);
        int segment = -1;

        for (int attempt = 0; attempt < (radiusTiles > 0 ? WalkDrawLimit : 1); attempt++)
        {
            int candidate = pool[CounterHash.Below(
                CounterHash.Of(seed, query, ((ulong)attempt << 8) | (ulong)mode,
                    CounterHash.Purpose.DestinationSegment),
                pool.Length)];

            segment = candidate;

            if (radiusTiles <= 0 || StraightLineTiles(origin.Segment, candidate) <= radiusTiles)
            {
                break;
            }
        }

        return new AccessPoint(segment, Offset(seed, query, mode, segment, CounterHash.Purpose.DestinationOffset));
    }

    /// <summary>
    /// Straight-line Tiles between two Segments' midpoints. The bucketing axis, and an approximation
    /// on purpose — the exact Access Point positions differ by at most half a Segment, which is far
    /// inside a bucket.
    /// </summary>
    public int StraightLineTiles(int fromSegment, int toSegment) =>
        IntegerGeometry.Distance(_midX[fromSegment], _midY[fromSegment], _midX[toSegment], _midY[toSegment]);

    private int[] Pool(Modes mode) => mode == Modes.Foot ? _footSegments : _carSegments;

    private int Offset(ulong seed, ulong query, Modes mode, int segment, CounterHash.Purpose purpose)
    {
        int length = _graph.SegmentLengthTiles[segment];
        return CounterHash.Below(CounterHash.Of(seed, query, (ulong)mode, purpose), length + 1);
    }
}
