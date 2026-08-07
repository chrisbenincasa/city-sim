using Borough.Core.Determinism;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// Incremental re-diffusion is <b>exact, not approximate</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The test is bit-identity against a full recompute. Not close; identical</b> (<c>plans/0009</c>
/// task 5). If it were merely close, the design would have silently become a relaxation and the
/// save-divergence failure would be back — two cities built by the same commands would disagree
/// because one of them had been saved partway through, and nothing in the project would say why.
/// </para>
/// <para>
/// <b>What makes it exact is bounded support, and nothing else.</b> An output Cell reads no source
/// further than one kernel radius away, so recomputing every Cell within that radius of a changed
/// source recomputes everything that could have moved — and reads exactly the sources a full pass
/// would have read there. That is a property of the kernel, so the halo radius and the kernel radius
/// are the same number by construction rather than by a coincidence somebody has to maintain.
/// </para>
/// </remarks>
public class IncrementalDiffusionTests
{
    private const int Rounds = 12;
    private const int PerRound = 5;

    /// <summary>
    /// A randomised sequence of source changes, checked after every round.
    /// </summary>
    /// <remarks>
    /// <b>Checked every round rather than at the end, because the failure this looks for compounds.</b>
    /// A halo one Cell short leaves a thin stale ring after the first round; by the twelfth those rings
    /// overlap and the field is wrong in a pattern that looks like plausible terrain. Comparing only
    /// the final state would find it, but comparing each round names the round it entered — which is
    /// the difference between a failing test and a diagnosable one.
    /// </remarks>
    [Fact]
    public void Incremental_re_diffusion_is_bit_identical_to_a_full_recompute()
    {
        MapLayers incremental = new(LayerRuleset.Default);
        MapLayers full = new(LayerRuleset.Default);

        for (int round = 0; round < Rounds; round++)
        {
            foreach ((int east, int north, int amount) in Round(round))
            {
                incremental.EmitPollution(new Cells(east), new Cells(north), amount);
                full.EmitPollution(new Cells(east), new Cells(north), amount);
            }

            incremental.DiffusePollution();
            full.RediffusePollution();

            AssertIdentical(incremental, full, round);
        }
    }

    /// <summary>
    /// A round that changes nothing recomputes nothing, and the field is unmoved.
    /// </summary>
    /// <remarks>
    /// The cheap half of the claim, and the one that makes the cadence affordable: 63 Ticks in 64
    /// carry no convolution at all, and of the Ticks that do, one with no changed source does no work.
    /// </remarks>
    [Fact]
    public void A_diffusion_with_no_changed_source_is_a_no_op()
    {
        MapLayers layers = new(LayerRuleset.Default);

        foreach ((int east, int north, int amount) in Round(0))
        {
            layers.EmitPollution(new Cells(east), new Cells(north), amount);
        }

        layers.DiffusePollution();
        Assert.False(layers.PollutionIsDirty);

        int[] before = Snapshot(layers);
        layers.DiffusePollution();

        Assert.Equal(before, Snapshot(layers));
    }

    /// <summary>
    /// A source at the very edge of the map diffuses without reaching past it.
    /// </summary>
    /// <remarks>
    /// The halo of a Cell at the origin dilates to a rectangle three-quarters of which is off the map.
    /// Clamping that to nothing, or to the wrong rectangle, is the off-by-one <see cref="CellRect"/>'s
    /// half-open form exists to remove — and it is invisible in the middle of the map, which is where
    /// every other test in this file puts its sources.
    /// </remarks>
    [Fact]
    public void A_source_in_the_corner_diffuses_the_same_way_both_paths_compute_it()
    {
        MapLayers incremental = new(LayerRuleset.Default);
        MapLayers full = new(LayerRuleset.Default);

        foreach (MapLayers layers in (MapLayers[])[incremental, full])
        {
            layers.EmitPollution(Cells.Zero, Cells.Zero, 5_000);
            layers.EmitPollution(
                new Cells(CellGrid.WorldCells - 1), new Cells(CellGrid.WorldCells - 1), 5_000);
        }

        incremental.DiffusePollution();
        full.RediffusePollution();

        AssertIdentical(incremental, full, round: 0);
        Assert.True(incremental.Pollution(Cells.Zero, Cells.Zero) > 0);
    }

    private static void AssertIdentical(MapLayers incremental, MapLayers full, int round)
    {
        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                int expected = LayerDiffusionTests.Raw(full, east, north);
                int observed = LayerDiffusionTests.Raw(incremental, east, north);

                if (expected != observed)
                {
                    Assert.Fail(
                        $"round {round}, Cell ({east}, {north}): incremental {observed}, "
                        + $"full recompute {expected}. The halo missed a Cell the sources reach.");
                }
            }
        }
    }

    private static int[] Snapshot(MapLayers layers)
    {
        int[] field = new int[CellGrid.WorldCellCount];

        for (int index = 0; index < field.Length; index++)
        {
            field[index] = LayerDiffusionTests.Raw(
                layers, index % CellGrid.WorldCells, index / CellGrid.WorldCells);
        }

        return field;
    }

    private static (int East, int North, int Amount)[] Round(int round)
    {
        (int, int, int)[] emissions = new (int, int, int)[PerRound];

        for (int i = 0; i < PerRound; i++)
        {
            ulong draw = Randomness.Mix((((ulong)round + 1) << 20) + (ulong)i + 0xC17ECAFEUL);

            emissions[i] = (
                (int)(draw % CellGrid.WorldCells),
                (int)((draw >> 16) % CellGrid.WorldCells),
                1 + (int)((draw >> 32) % 400));
        }

        return emissions;
    }
}
