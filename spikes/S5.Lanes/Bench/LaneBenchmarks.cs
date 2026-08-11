using BenchmarkDotNet.Attributes;
using S5.Lanes.Lanes;

namespace S5.Lanes.Bench;

/// <summary>
/// The BenchmarkDotNet cross-check. Same kernels, same fixtures, a different instrument.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not the primary measurement and it is not decoration either.</b> S5's headline is a
/// derived ratio, which BenchmarkDotNet does not produce and the self-timed harness does. But a
/// self-timed loop is an instrument nobody has calibrated, and the corpus has three recorded
/// machine-state defects that a second instrument would have caught. So the same kernels run under
/// both, and a disagreement between them is a result rather than an inconvenience.
/// </para>
/// <para>
/// The rungs are the ones L1 and L2 quote, and no others: a benchmark class that swept everything
/// would take longer than the capture it is checking.
/// </para>
/// </remarks>
[MemoryDiagnoser(displayGenColumns: false)]
public class LaneBenchmarks
{
    private LaneNetwork network = null!;
    private LaneNetwork overlapped = null!;
    private PromotionFixture fixture = null!;

    /// <summary>Lanes at the rung L2 reads its self-consistent answer from.</summary>
    [Params(16_384)]
    public int Lanes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        network = LaneNetwork.Build(Lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        overlapped = LaneNetwork.Build(Lanes, Units.VehiclesPerLaneAtJam, 70, 2, 0x5EEDUL);
        fixture = PromotionFixture.Build(
            256, Units.LanesPerSegment, Units.LanesPerSegment * Units.VehiclesPerLaneAtJam, 0x5EEDUL);
    }

    [Benchmark(Baseline = true, Description = "L0 bare walk")]
    public long BareWalk() => Denominator.Touch(network);

    [Benchmark(Description = "L1/L2 queue pass")]
    public void QueuePass() => Idm.StepQueues(network);

    [Benchmark(Description = "L2 exchange by cursor + queue pass")]
    public void QueuePassWithOverlaps()
    {
        Overlaps.ExchangeByCursor(overlapped);
        Idm.StepQueuesWithOverlaps(overlapped);
    }

    [Benchmark(Description = "L3 promotion")]
    public void Promote() => Fidelity.Promote(fixture);
}
