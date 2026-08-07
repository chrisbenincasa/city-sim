using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// The two properties that make a Map Layer a convolution rather than a relaxation.
/// </summary>
/// <remarks>
/// <para>
/// <b>These are the deliverable as much as the code is.</b> <c>adr/0034 §3</c> claims that a bounded
/// kernel makes many sources <em>superpose exactly</em> and makes incremental re-diffusion
/// <em>exact rather than approximate</em>. Both are properties of the arithmetic, both are cheap to
/// assert, and neither is visible in a field somebody looks at — a smeared or approximately-superposed
/// field looks like a pollution map. Under relaxation-to-steady-state neither holds, one changed
/// source perturbs the whole field, and saves diverge for reasons nobody could find.
/// </para>
/// <para>
/// <b>Every assertion here is bit-for-bit.</b> <em>Close</em> is the failure mode, not a weaker pass:
/// if superposition were merely approximate, the design would have silently become a relaxation.
/// </para>
/// </remarks>
public class LayerDiffusionTests
{
    /// <summary>Twenty, because <c>02 §2.4</c> makes the claim about twenty factories.</summary>
    private const int Sources = 20;

    private static SeparableKernel Kernel => MapLayers.PollutionKernel;

    [Fact]
    public void The_kernel_is_stated_in_metres_before_it_is_stated_in_Cells()
    {
        // 02 §2.5 question 2: what is its actionable range in metres, and can you defend the figure
        // from reality? The Cell count is the derived quantity, and it is derived by rounding up so
        // that a defended range is never silently shortened.
        Assert.Equal(1_024, LayerKernels.IndustrialPollutionMetres);
        Assert.Equal(8, Kernel.Radius.Raw);
        Assert.Equal(1_024, CellGrid.ToMetres(Kernel.Radius));
    }

    [Fact]
    public void The_tent_weights_and_gain_are_what_they_are_documented_to_be()
    {
        SeparableKernel kernel = SeparableKernel.Tent(new Cells(8));

        Assert.Equal(
            [1, 2, 3, 4, 5, 6, 7, 8, 9, 8, 7, 6, 5, 4, 3, 2, 1],
            kernel.Weights.ToArray());

        Assert.Equal(81, kernel.Gain);
        Assert.Equal(81 * 81, kernel.Scale);
    }

    /// <summary>
    /// <b>Superposition is exact.</b> Twenty sources diffused together equal the sum of twenty
    /// diffused separately, bit for bit.
    /// </summary>
    /// <remarks>
    /// <b>This is the property that makes incremental re-diffusion legal, so it is asserted rather
    /// than trusted.</b> It is also what forced the single rounding in the scheme out to the point of
    /// use: integer division is not linear, so a <c>RoundDiv</c> inside either pass would fail this
    /// test — see <see cref="SeparableKernel.Normalise"/>, and
    /// <see cref="Rounding_inside_a_pass_would_break_superposition_which_is_why_it_is_not_there"/>,
    /// which is that claim watched failing rather than asserted in a comment.
    /// </remarks>
    [Fact]
    public void Twenty_sources_together_equal_twenty_sources_summed()
    {
        (int East, int North, int Amount)[] emissions = Emissions(Sources);

        MapLayers together = new(LayerRuleset.Default);
        foreach ((int east, int north, int amount) in emissions)
        {
            together.EmitPollution(new Cells(east), new Cells(north), amount);
        }

        together.RediffusePollution();

        int[] summed = new int[CellGrid.WorldCellCount];

        foreach ((int east, int north, int amount) in emissions)
        {
            MapLayers alone = new(LayerRuleset.Default);
            alone.EmitPollution(new Cells(east), new Cells(north), amount);
            alone.RediffusePollution();

            for (int index = 0; index < summed.Length; index++)
            {
                summed[index] += RawAt(alone, index);
            }
        }

        for (int index = 0; index < summed.Length; index++)
        {
            Assert.Equal(summed[index], RawAt(together, index));
        }
    }

    /// <summary>
    /// <b>No directional smear.</b> The result is invariant under transposing the source field.
    /// </summary>
    /// <remarks>
    /// <c>02 §2.4</c>: in-place diffusion is order-dependent, which is simultaneously a determinism
    /// hazard and a visible directional smear — <em>a bug that looks like an art decision</em>, and
    /// therefore one that would be found late or never.
    /// </remarks>
    [Fact]
    public void Transposing_the_sources_transposes_the_field()
    {
        (int East, int North, int Amount)[] emissions = Emissions(Sources);

        MapLayers upright = new(LayerRuleset.Default);
        MapLayers flipped = new(LayerRuleset.Default);

        foreach ((int east, int north, int amount) in emissions)
        {
            upright.EmitPollution(new Cells(east), new Cells(north), amount);
            flipped.EmitPollution(new Cells(north), new Cells(east), amount);
        }

        upright.RediffusePollution();
        flipped.RediffusePollution();

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Assert.Equal(Raw(upright, east, north), Raw(flipped, north, east));
            }
        }
    }

    /// <summary>
    /// The in-place variant, <b>watched failing</b> the test above.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>plans/0009</c> says to write it, watch it fail, and delete it. This keeps it, and the
    /// deviation is deliberate.</b> Deleting it means the transpose test's ability to fail was
    /// established once, by somebody who is no longer in the room, and every later reader has to take
    /// it on trust — which is the same objection <c>CLAUDE.md</c> makes to a diagnostic with no test
    /// that watches it fire, and the objection S2 R3 recorded from the other side: <em>a correctness
    /// column that cannot move is not evidence that the error is absent.</em> A zero should be paired
    /// with a case expected to be non-zero.
    /// </para>
    /// <para>
    /// The variant is a faithful in-place separable pass: it reads and writes one array, scanning west
    /// to east and then south to north, so a Cell's new value depends on whether its neighbour has
    /// been visited yet. That is exactly the implementation somebody writes when they think the
    /// second buffer is tidiness.
    /// </para>
    /// </remarks>
    [Fact]
    public void An_in_place_pass_fails_the_transpose_test()
    {
        int[] upright = Grid(Emissions(Sources));
        int[] flipped = Transpose(upright);

        DiffuseInPlace(upright);
        DiffuseInPlace(flipped);

        Assert.NotEqual(upright, Transpose(flipped));
    }

    /// <summary>
    /// A uniform source field reproduces itself once normalised, which is what fixes the gain.
    /// </summary>
    /// <remarks>
    /// <b>Asserted at the centre, because the edges lose mass and should.</b> A plume near the map
    /// edge blows off the map; zero-extension is what keeps the operator linear there, and clamping to
    /// hold mass in would pile pollution against the world boundary — an artefact of where the map
    /// stops rather than anything the city did.
    /// </remarks>
    [Fact]
    public void A_uniform_field_survives_diffusion_at_the_centre()
    {
        const int Level = 1_000;
        const int Margin = 32;

        MapLayers layers = new(LayerRuleset.Default);

        for (int north = Margin; north < CellGrid.WorldCells - Margin; north++)
        {
            for (int east = Margin; east < CellGrid.WorldCells - Margin; east++)
            {
                layers.EmitPollution(new Cells(east), new Cells(north), Level);
            }
        }

        layers.RediffusePollution();

        int middle = CellGrid.WorldCells / 2;
        Assert.Equal(Level, layers.Pollution(new Cells(middle), new Cells(middle)));
    }

    /// <summary>
    /// The rounding is stated, so the same source produces the same field on every machine.
    /// </summary>
    /// <remarks>
    /// Half away from zero, symmetric about zero. Symmetry is the part worth checking: a Layer value
    /// is signed — land value moves in both directions — and truncation toward zero would bias every
    /// negative reading by half a unit in the opposite direction from every positive one.
    /// </remarks>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(6_561, 1)]
    [InlineData(-6_561, -1)]
    [InlineData(3_281, 1)]
    [InlineData(3_280, 0)]
    [InlineData(-3_281, -1)]
    [InlineData(-3_280, 0)]
    public void Normalisation_rounds_half_away_from_zero(long accumulated, int expected)
    {
        Assert.Equal(expected, Kernel.Normalise(accumulated));
    }

    /// <summary>
    /// The claim that forced the rounding out of the passes, watched failing.
    /// </summary>
    /// <remarks>
    /// <b>Against <see cref="SeparableKernel.Gain"/>, not <see cref="SeparableKernel.Scale"/>, because
    /// that is the divisor a pass would use.</b> A pass normalising as it went would divide by 81 once
    /// per axis. Two sources of 41 in one Cell round to 1 each and to 1 together — so they diffuse to
    /// 2 apart and to 1 combined. That is the whole argument, in two numbers: an operator that rounds
    /// partway through is not linear, and superposition is the statement that it is.
    /// </remarks>
    [Fact]
    public void Rounding_inside_a_pass_would_break_superposition_which_is_why_it_is_not_there()
    {
        long apart = IntegerMath.RoundDiv(41, Kernel.Gain) + IntegerMath.RoundDiv(41, Kernel.Gain);
        long together = IntegerMath.RoundDiv(41 + 41, Kernel.Gain);

        Assert.Equal(2, apart);
        Assert.Equal(1, together);
        Assert.NotEqual(apart, together);
    }

    /// <summary>A repeatable spread of sources. Counter-based, never a stream.</summary>
    internal static (int East, int North, int Amount)[] Emissions(int count)
    {
        (int, int, int)[] emissions = new (int, int, int)[count];

        for (int i = 0; i < count; i++)
        {
            ulong draw = Randomness.Mix((ulong)i + 0x5EEDUL);

            emissions[i] = (
                (int)(draw % CellGrid.WorldCells),
                (int)((draw >> 16) % CellGrid.WorldCells),
                1 + (int)((draw >> 32) % 500));
        }

        return emissions;
    }

    /// <summary>The stored, pre-normalised value at a Cell. Zero where nothing is resident.</summary>
    internal static int Raw(MapLayers layers, int east, int north)
    {
        int slot = layers.Residency.Slot(new Cells(east), new Cells(north));

        return slot == CellResidency.NotResident ? 0 : layers.Cells.Pollution[slot];
    }

    private static int RawAt(MapLayers layers, int index) =>
        Raw(layers, index % CellGrid.WorldCells, index / CellGrid.WorldCells);

    private static int[] Grid((int East, int North, int Amount)[] emissions)
    {
        int[] grid = new int[CellGrid.WorldCellCount];

        foreach ((int east, int north, int amount) in emissions)
        {
            grid[CellGrid.Index(new Cells(east), new Cells(north))] += amount;
        }

        return grid;
    }

    private static int[] Transpose(int[] grid)
    {
        int[] flipped = new int[grid.Length];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                flipped[(east * CellGrid.WorldCells) + north] =
                    grid[(north * CellGrid.WorldCells) + east];
            }
        }

        return flipped;
    }

    /// <summary>
    /// The implementation this slice exists to rule out: one array, read and written in scan order.
    /// </summary>
    private static void DiffuseInPlace(int[] grid)
    {
        int radius = Kernel.Radius.Raw;

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                long accumulated = 0;

                for (int offset = -radius; offset <= radius; offset++)
                {
                    int neighbour = east + offset;

                    if (neighbour >= 0 && neighbour < CellGrid.WorldCells)
                    {
                        accumulated += Kernel.Weight(offset)
                            * (long)grid[(north * CellGrid.WorldCells) + neighbour];
                    }
                }

                grid[(north * CellGrid.WorldCells) + east] = Kernel.Normalise(accumulated * 81);
            }
        }

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                long accumulated = 0;

                for (int offset = -radius; offset <= radius; offset++)
                {
                    int neighbour = north + offset;

                    if (neighbour >= 0 && neighbour < CellGrid.WorldCells)
                    {
                        accumulated += Kernel.Weight(offset)
                            * (long)grid[(neighbour * CellGrid.WorldCells) + east];
                    }
                }

                grid[(north * CellGrid.WorldCells) + east] = Kernel.Normalise(accumulated * 81);
            }
        }
    }
}
