using BenchmarkDotNet.Attributes;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// What one walk search costs — <c>plans/0002</c> §B, and the row
/// <c>plans/0013-tick-budget.md</c> has never had.
/// </summary>
/// <remarks>
/// <para>
/// <b>The ledger prices routing and the Lane model and has no row for the pedestrian search at
/// all.</b> What stands in for one is a product of two guesses: <c>plans/0010</c> derives ~464 walk
/// Leg routes a Tick at 1M and <c>docs/spike-results.md</c> prices a walk search at 4–20 µs, giving
/// <b>1.9–9.3 ms against a 15.6 ms budget at 4×</b> — 12% to 60% — against a ledger that already sums
/// to ≥114% without it. This harness measures the <b>unit</b>. It cannot measure the multiplicand,
/// which needs a Trip generator, and <b>the row does not close</b>: half a measured row quoted as a
/// whole one is how the Bin Rule row came to be *right by cancellation*.
/// </para>
/// <para>
/// <b>Three cases rather than one, because <see cref="WalkRouting.Cost"/> has three regimes and a mean
/// over them is a number about nothing.</b> <see cref="Walk"/> runs the Dijkstra.
/// <see cref="AcrossTheStreet"/> never reaches it — a same-Segment walk is two offsets subtracted.
/// <see cref="Severed"/> never reaches it either, because union-find components over the foot subgraph
/// answer *unreachable* in constant time. Sampling pairs at random and averaging would blend all
/// three, and on a severed city would report the search getting **faster** as the city breaks.
/// </para>
/// <para>
/// <b><see cref="AcrossTheStreet"/> and <see cref="Severed"/> do not vary with
/// <see cref="Blocks"/>, and are re-timed at every rung on purpose.</b> That is
/// <see cref="ZoneRuleBenchmarks"/>' denominator discipline: a case whose true cost is flat, timed
/// once per rung, is this harness's own error bar. If those two columns drift across the sweep, the
/// <see cref="Walk"/> column is measuring the machine rather than the distance.
/// </para>
/// <para>
/// <b>The crossing cost is <see cref="TravelTime.Zero"/> and that is not a choice of its value.</b> It
/// is <c>[trips]</c> Ruleset data, hash-bearing, and 5b is forbidden from choosing it
/// (<c>adr/0052</c>, <c>plans/0021</c> → *what this slice must report and must not choose*). It is an
/// argument to <see cref="WalkRouting.Cost"/> rather than a table lookup, so passing zero costs
/// nothing and decides nothing: it is one addition on the same-Segment path and is not read at all on
/// the other two.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class WalkSearchBenchmarks
{
    /// <summary>
    /// Lattice edges between origin and destination — 32 Tiles each, so 128 m to 4.1 km.
    /// </summary>
    /// <remarks>
    /// <b>The axis is distance and not city size, because the search's work is settled nodes.</b> The
    /// Dijkstra stops when nothing on the frontier can beat the arrival it holds, so a graph ten times
    /// larger with the destination in the same place settles the same nodes. The range covers a real
    /// walk: 1 block is across a Cell, 16 blocks is 2 km, and 32 is past where anybody walks.
    /// </remarks>
    [Params(1, 2, 4, 8, 16, 32)]
    public int Blocks { get; set; }

    private RoadGraph _shipped = null!;
    private RoadGraph _severing = null!;
    private WalkScratch _scratch = null!;
    private Address _from;
    private Address _to;
    private Address _kerb;
    private Address _opposite;
    private Address _islandA;
    private Address _islandB;

    [GlobalSetup]
    public void Setup()
    {
        _shipped = WalkSearchFixture.Shipped();
        _severing = WalkSearchFixture.Severing();
        _scratch = new WalkScratch();

        (_from, _to) = WalkSearchFixture.Apart(_shipped, Blocks);
        (_kerb, _opposite) = WalkSearchFixture.AcrossTheStreet(_shipped);
        (_islandA, _islandB) = WalkSearchFixture.Severed(_severing);
    }

    /// <summary>One resolved walk Leg <see cref="Blocks"/> blocks long. <b>This is the §B number.</b></summary>
    [Benchmark(Description = "walk", Baseline = true)]
    public TravelTime Walk() =>
        WalkRouting.Cost(_shipped, TravelMode.Foot, _from, _to, TravelTime.Zero, _scratch);

    /// <summary>The closed-form case: same Segment, opposite sides. The floor.</summary>
    [Benchmark(Description = "across the street")]
    public TravelTime AcrossTheStreet() =>
        WalkRouting.Cost(_shipped, TravelMode.Foot, _kerb, _opposite, TravelTime.Zero, _scratch);

    /// <summary>
    /// The severed case, answered by a component comparison without settling a node.
    /// </summary>
    /// <remarks>
    /// <b>Worth its own column because the intuition is backwards.</b> *No route exists* reads like
    /// the worst case — an exhausted search over the whole component — and it is the case 5b exists to
    /// report, so a Trip model that priced it as expensive would have designed around a cost that is
    /// not there.
    /// </remarks>
    [Benchmark(Description = "severed")]
    public TravelTime Severed() =>
        WalkRouting.Cost(_severing, TravelMode.Foot, _islandA, _islandB, TravelTime.Zero, _scratch);
}
