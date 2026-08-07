using BenchmarkDotNet.Attributes;
using Borough.Core.Entities;
using Borough.Core.Quantities;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// What <c>02 §10</c>'s tiers cost on a world that has something in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The question this answers is the one task 6 could not.</b> A replayed session holds a handful
/// of Lots before slice 7, so the tiers were built and measured against an empty world — which
/// measures the dispatch and nothing else. The numbers that decide whether the staggered tier is
/// affordable are per-row, and per-row needs rows.
/// </para>
/// <para>
/// <b>The figure to compare against is 15.6 ms</b>, the Tick budget at 4× speed. The staggered
/// sweep runs once per Tick and covers <c>1/Slices</c> of the world, so its cost belongs in that
/// budget alongside every other consumer of a Tick — of which, today, there are none, which is
/// exactly why the share it takes cannot yet be argued about honestly.
/// </para>
/// <para>
/// <b>The end-of-run walk is measured against a different question.</b> It is paid once per run, so
/// the comparison is not against a Tick but against a person's patience — <c>adr/0033</c>'s
/// <em>unaffordable per Tick and trivial at the end of a headless run</em> is a claim with a number
/// behind it, and this is the number.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class InvariantCostBenchmarks
{
    private World _world = null!;

    /// <summary>
    /// Populations an order of magnitude apart, because the claim under test is about the slope.
    /// </summary>
    /// <remarks>
    /// 1M is the late-game target and is deliberately absent: it belongs to S0, which is gated, and
    /// running it here would make this the synthetic city it is not allowed to become.
    /// </remarks>
    [Params(1_000, 10_000, 100_000)]
    public int Population { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _world = new World(Population);
        SyntheticCity.PopulateInto(_world);
    }

    /// <summary>
    /// One Tick's worth of the staggered tier: one slice of the world, at the default cadence.
    /// </summary>
    [Benchmark(Description = "staggered, one slice")]
    public void StaggeredSlice() => _world.Invariants.RunStaggered(_world, new Ticks(7));

    /// <summary>
    /// A full sweep: every row checked once, which takes <c>Slices</c> Ticks in a real run.
    /// </summary>
    /// <remarks>
    /// The honest per-row figure. Dividing the slice cost by the slice count would assume the
    /// partition is even in <em>work</em> rather than in row count, and a list walk per row is not
    /// obviously even.
    /// </remarks>
    [Benchmark(Description = "staggered, full sweep")]
    public void StaggeredFullSweep()
    {
        for (ulong tick = 0; tick < (ulong)_world.Invariants.Slices; tick++)
        {
            _world.Invariants.RunStaggered(_world, new Ticks(tick));
        }
    }

    /// <summary>The whole-world walks, paid once per run.</summary>
    [Benchmark(Description = "end of run")]
    public void EndOfRun() => _world.Invariants.RunEndOfRun(_world, new Ticks(0));

    /// <summary>
    /// The State Hash, as the reference point.
    /// </summary>
    /// <remarks>
    /// <b>Included because it is the cost already being paid and accepted.</b> An invariant sweep
    /// that costs less than a hash the project already takes on a cadence is a sweep nobody needs to
    /// argue about; one that costs several times more is a conversation. A number with nothing beside
    /// it invites whatever comparison the reader brought with them.
    /// </remarks>
    [Benchmark(Description = "state hash, for scale")]
    public ulong StateHash() => _world.HashState();
}
