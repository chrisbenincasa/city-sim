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
    /// Scattered emitters make the whole Layer resident well below any plausible count, so residency
    /// is not a lever the city has — <b>and the margin is an order of magnitude, not three.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This was the row's good news, and at 512 Cells it is merely good.</b> A cost whose driver
    /// saturates far below any plausible source count has no guessed multiplicand: it does not matter
    /// how many factories a million-Citizen city has, because any plausible number is past the knee.
    /// </para>
    /// <para>
    /// ⚠ <b>The knee moved 256 → 8,192 when the map went to 512 Cells on 2026-08-13</b>
    /// (<c>adr/0089</c>) — 32× for a 16× area, because a halo of fixed radius covers a smaller share
    /// of a larger map and overlaps less. Against S0a's <b>120,001 Buildings</b> in a 1M city the
    /// headroom falls from <b>469× to 14.6×</b>. The claim survives; <b>the reason it survives has
    /// changed from <em>obviously</em> to <em>by a factor of fifteen</em></b>, and fifteen is inside
    /// the range an industrial share could plausibly move. Routed to <c>plans/0013</c>'s Map Layer row
    /// rather than left here, per <c>adr/0073</c>.
    /// </para>
    /// <para>
    /// <b>Both the knee and the margin are asserted, because only the second is the claim.</b> An
    /// assertion on saturation alone would have gone green at 8,192 exactly as it did at 256, and the
    /// thing that changed — the distance to a plausible city — would have gone unrecorded.
    /// </para>
    /// </remarks>
    [Fact]
    public void Residency_saturates_far_below_any_plausible_emitter_count()
    {
        foreach (int emitters in (int[])[1, 16, 64, 256, 1_024, 2_048, 4_096, 8_192, 16_384])
        {
            int resident = ResidentAfter(emitters);
            int percent = resident * 100 / CellGrid.WorldCellCount;

            _output.WriteLine($"{emitters,5} emitters → {resident,6} of {CellGrid.WorldCellCount} Cells ({percent}%)");
        }

        // Measured rather than assumed, twice over: an early draft asserted saturation at 16, and the
        // 256 that replaced it was a reading on a 128-Cell map that survived the map's flip in silence
        // until this line failed.
        const int Knee = 8_192;

        Assert.Equal(CellGrid.WorldCellCount, ResidentAfter(Knee));

        // And the rung below it is not saturated, which is what makes the number above a knee rather
        // than just a value that happens to work.
        Assert.True(ResidentAfter(Knee / 2) < CellGrid.WorldCellCount);

        // A 1M-Citizen city holds 120,001 Buildings (S0a). The knee must stay well below that or the
        // row acquires a multiplicand — and "well below" is now 14.6x where it was 469x.
        const int BuildingsAtTarget = 120_001;

        Assert.True(
            Knee * 10 < BuildingsAtTarget,
            $"the residency knee is {Knee} emitters against {BuildingsAtTarget} Buildings in a 1M "
            + "city, which is under an order of magnitude. plans/0013's Map Layer row is written on "
            + "the ground that no plausible industrial share reaches the sloped part of this curve; "
            + "at this margin that ground is gone and the row needs a measured multiplicand.");
    }
}
