using Borough.Core.Arithmetic;
using S2.Routing.Graph;

namespace S2.Routing.Routing;

/// <summary>
/// The shape of an origin-destination draw.
/// </summary>
internal enum OdShape
{
    /// <summary>Every Segment equally likely. What R0 through R3 drew, and the placeholder they flagged.</summary>
    Uniform,

    /// <summary>Destination weighted by <c>exp(-d / L)</c> on distance from the origin. The gravity model's deterrence function.</summary>
    DistanceDecay,

    /// <summary>Destination weighted by <c>exp(-d / L)</c> on distance from the map centre. The outbound morning commute into a job core.</summary>
    Monocentric,
}

/// <summary>One rung of the swept family: a shape, and the decay length it is swept on.</summary>
/// <param name="Shape">Which weight function.</param>
/// <param name="DecayLengthTiles">
/// <c>L</c>, in Tiles. Ignored by <see cref="OdShape.Uniform"/>. Under a radial <c>r·exp(-r/L)</c> the
/// mean draw is <c>2L</c>, which is the figure to compare a real commute length against.
/// </param>
internal readonly record struct OdRung(OdShape Shape, int DecayLengthTiles)
{
    public string Name => Shape switch
    {
        OdShape.Uniform => "uniform",
        OdShape.DistanceDecay => $"decay L={DecayLengthTiles}",
        OdShape.Monocentric => $"monocentric L={DecayLengthTiles}",
        _ => "unknown",
    };
}

/// <summary>An origin-destination pair, with the straight-line distance it was bucketed on.</summary>
internal readonly record struct OdPair(AccessPoint Origin, AccessPoint Destination, int StraightLineTiles);

/// <summary>
/// The origin-destination distribution family, and the debt it discharges.
/// </summary>
/// <remarks>
/// <para>
/// <b>S2 has drawn origin-destination pairs uniformly since R0, and R0 flagged that as a placeholder
/// to be replaced by the distribution R1 derived.</b> R1 derived none. R2 and R3 inherited the
/// placeholder unchanged, and R3 had to publish its speedups as an upper bound because of it: a
/// uniform draw over a 4,096-Tile map produces long routes, and long routes are where a hierarchy
/// wins widest. The same hole is worse downstream — a cache hit rate measured against a uniform draw
/// is close to meaningless, because the whole value of a cache is that real Trips repeat.
/// </para>
/// <para>
/// <b>This file does not close the hole. It makes the hole an axis.</b> Nobody can produce the real
/// distribution until Trips exist, and inventing one and calling it the distribution would bake a
/// guess into every figure downstream while making it look like a measurement — R0's stated reason
/// for drawing uniformly in the first place, and it was the right call then. What is available now is
/// the precedent this plan uses everywhere else, and which it calls good: <b>report a curve, do not
/// choose a number.</b> Three shapes spanning mean trip length, every figure reported against all of
/// them, and the reader locates the real city on the curve when somebody can say where it sits.
/// </para>
/// <para>
/// <b>Uniform is a rung of the same mechanism rather than a separate path</b>, and that is deliberate.
/// It is the degenerate weight function — accept the first candidate always — drawn from the same
/// pool, through the same rejection loop, with the same hash. So a difference between two rungs is
/// the shape and cannot be the machinery, and every prior S2 figure remains comparable against the
/// uniform rung rather than being orphaned by a new sampler. This spike has twice caught an
/// instrument that could not move, and once caught two rungs that were secretly the same rung; a
/// family whose null case is a member of the family is the cheap defence against both.
/// </para>
/// <para>
/// <b>The truncation is stated because it is the one approximation here.</b> Rejection sampling over
/// the whole pool is exact — there is no bounding box and no spatial index to bias the tail — but it
/// terminates only in expectation, so it is capped. At the tightest rung the acceptance rate is
/// ~0.6% and the cap is 65,536 attempts, which makes exhaustion vanishingly unlikely; the count is
/// reported anyway, because the last five instruments in this spike that earned their place did so on
/// the day they read something other than zero.
/// </para>
/// </remarks>
internal sealed class OdDistribution
{
    /// <summary>
    /// Attempts before a draw gives up and takes the nearest candidate it saw. Never expected to
    /// fire; reported so that "never expected" is a measurement rather than a belief.
    /// </summary>
    private const int AttemptLimit = 65_536;

    private readonly OdSampler _sampler;
    private readonly int _centreX;
    private readonly int _centreY;
    private readonly int[] _midX;
    private readonly int[] _midY;

    public OdDistribution(RoadGraph graph, OdSampler sampler)
    {
        _sampler = sampler;
        _centreX = graph.Parameters.MapTiles >> 1;
        _centreY = graph.Parameters.MapTiles >> 1;

        _midX = new int[graph.Segments];
        _midY = new int[graph.Segments];

        for (int segment = 0; segment < graph.Segments; segment++)
        {
            int a = graph.SegmentNodeA[segment];
            int b = graph.SegmentNodeB[segment];
            _midX[segment] = IntegerMath.RoundDiv(graph.NodeX[a] + graph.NodeX[b], 2);
            _midY[segment] = IntegerMath.RoundDiv(graph.NodeY[a] + graph.NodeY[b], 2);
        }
    }

    /// <summary>
    /// The rungs every R4 section sweeps. <b>Uniform first, so a reader comparing against R0-R3 does
    /// not have to hunt for the comparable row.</b>
    /// </summary>
    /// <remarks>
    /// A Tile is ~4 m, so the decay lengths are 1.02, 2.05 and 4.10 km and the mean draws are twice
    /// that. Against a map 16.4 km across and a Commute Budget the corpus has never sized, those
    /// bracket the plausible range from a city that is mostly local trips to one that is mostly
    /// cross-town. Nothing here claims to know which.
    /// </remarks>
    public static OdRung[] Rungs =>
    [
        new(OdShape.Uniform, 0),
        new(OdShape.DistanceDecay, 1024),
        new(OdShape.DistanceDecay, 512),
        new(OdShape.DistanceDecay, 256),
        new(OdShape.Monocentric, 512),
    ];

    /// <summary>
    /// Draws <paramref name="count"/> pairs under one rung, reporting the mean attempts spent and how
    /// many draws exhausted the cap.
    /// </summary>
    public OdPair[] Draw(
        ulong seed, int count, Modes mode, OdRung rung, out long attempts, out int exhausted)
    {
        var pairs = new OdPair[count];
        attempts = 0;
        exhausted = 0;

        for (int i = 0; i < count; i++)
        {
            AccessPoint origin = _sampler.Origin(seed, (ulong)i, mode);
            AccessPoint destination = DrawDestination(
                seed, (ulong)i, mode, rung, origin, out int spent, out bool gaveUp);

            attempts += spent;
            if (gaveUp)
            {
                exhausted++;
            }

            pairs[i] = new OdPair(
                origin, destination, _sampler.StraightLineTiles(origin.Segment, destination.Segment));
        }

        return pairs;
    }

    /// <summary>
    /// The weight a candidate destination carries, Q16.16 in <c>[0, 1]</c>. <c>Fixed.One</c> is
    /// "always accept".
    /// </summary>
    private int Weight(OdRung rung, AccessPoint origin, int candidate)
    {
        if (rung.Shape == OdShape.Uniform)
        {
            return Fixed.One;
        }

        int distance = rung.Shape == OdShape.Monocentric
            ? IntegerGeometry.Distance(_midX[candidate], _midY[candidate], _centreX, _centreY)
            : _sampler.StraightLineTiles(origin.Segment, candidate);

        // exp(-d/L). Transcendental.Exp underflows to zero well before d/L is large enough to
        // matter, which is the behaviour wanted: beyond a handful of decay lengths the candidate is
        // simply never accepted.
        int ratio = Fixed.Div(Fixed.FromInt(distance), Fixed.FromInt(rung.DecayLengthTiles));
        return Transcendental.Exp(-ratio);
    }

    private AccessPoint DrawDestination(
        ulong seed, ulong query, Modes mode, OdRung rung, AccessPoint origin,
        out int spent, out bool gaveUp)
    {
        int nearest = -1;
        int nearestDistance = int.MaxValue;

        for (int attempt = 0; attempt < AttemptLimit; attempt++)
        {
            AccessPoint candidate = _sampler.Destination(
                seed, query ^ ((ulong)attempt << 24), mode, origin, radiusTiles: 0);

            int weight = Weight(rung, origin, candidate.Segment);

            ulong roll = CounterHash.Of(
                seed, query, ((ulong)attempt << 8) | (ulong)mode, CounterHash.Purpose.OdAccept);

            if (CounterHash.Below(roll, Fixed.One) < weight)
            {
                spent = attempt + 1;
                gaveUp = false;
                return candidate;
            }

            int distance = _sampler.StraightLineTiles(origin.Segment, candidate.Segment);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = attempt;
            }
        }

        // Never expected. Taking the nearest candidate seen is the least-bad tie-break under a
        // decaying weight — taking the last would bias the very tail the rung exists to suppress.
        spent = AttemptLimit;
        gaveUp = true;
        return _sampler.Destination(
            seed, query ^ ((ulong)nearest << 24), mode, origin, radiusTiles: 0);
    }
}
