using Borough.Core.Entities;
using Borough.Core.Space;
using Xunit.Abstractions;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// Why <see cref="MapLayerBenchmarks"/> sweeps the dirty rectangle and not the emitter count.
/// </summary>
/// <remarks>
/// <b>The first draft swept emitters and produced a non-monotonic column</b> — the whole-map recompute
/// read 982 µs at 100 emitters, 1,024 µs at 1,000 and 662 µs at 10,000 — which is the shape S2 names
/// twice over: <em>an artefact that varies with the swept axis is not distinguishable from a result</em>.
/// The cause is here, and it is not a defect in the Layer: the pollution kernel has a radius of 8
/// Cells, so an interior emitter makes **17×17 = 289 Cells** resident, and a 128×128 map saturates
/// after a couple of hundred scattered sources. Residency is the axis the timings actually move on,
/// and the emitter count stops being able to move it almost immediately.
/// <para>
/// <b>The ladder's first rung reads 81 rather than 289, and that is the map edge rather than a
/// mistake.</b> Emitter zero sits at Cell (0,0), so its halo clamps to a quarter of itself. Recorded
/// because a reader checking 289 against the printed 81 would otherwise conclude the radius is 4.
/// </para>
/// </remarks>
public sealed class MapLayerFixtureTests
{
    private readonly ITestOutputHelper _output;

    public MapLayerFixtureTests(ITestOutputHelper output) => _output = output;

    private static int ResidentAfter(int emitters)
    {
        var world = new World(1_000, LayerRuleset.Default);

        MapLayerBenchmarks.Scatter(world, emitters);

        return world.Layers.Cells.Rows.LiveCount;
    }

    /// <summary>
    /// A handful of scattered emitters makes the whole Layer resident, so residency is not a lever the
    /// city has.
    /// </summary>
    /// <remarks>
    /// <b>This is the row's good news and it belongs in the ledger.</b> A cost whose driver saturates
    /// at a few dozen sources has no guessed multiplicand: it does not matter how many factories a
    /// million-Citizen city has, because any plausible number is past the knee.
    /// </remarks>
    [Fact]
    public void Residency_saturates_far_below_any_plausible_emitter_count()
    {
        foreach (int emitters in (int[])[1, 16, 64, 128, 256, 1_024])
        {
            int resident = ResidentAfter(emitters);
            int percent = resident * 100 / CellGrid.WorldCellCount;

            _output.WriteLine($"{emitters,5} emitters → {resident,6} of {CellGrid.WorldCellCount} Cells ({percent}%)");
        }

        // 256 is the claim the benchmark's fixture rests on, and it is the number that was measured
        // rather than the one first assumed: 16 emitters leaves only 26% resident, so an earlier
        // draft of this test asserted saturation two rungs before it happens.
        Assert.Equal(CellGrid.WorldCellCount, ResidentAfter(256));

        // A 1M-Citizen city holds 120,001 Buildings (S0a). The knee is three orders of magnitude
        // below that, so no plausible industrial share can put a city on the sloped part of the curve.
        Assert.True(256 * 1_000 < 120_001 * 10);
    }
}
