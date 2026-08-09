using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// The two Layers that are not convolutions, and the holes where the rest would be.
/// </summary>
/// <remarks>
/// <b>Only three fields are Map Layers</b> under <c>adr/0034</c>'s classification, and two of them
/// have no kernel: land value has <em>momentum</em>, and Sealing is a <em>count</em>. Testing them
/// beside the diffusion is what keeps the enum from acquiring a fourth member by reflex — see
/// <see cref="LineSourceQueries"/>, which is the file that says why noise is not one.
/// </remarks>
public class LayerFieldsTests
{
    private const int Population = 1_000;

    /// <summary>Land value closes the gap to its target and settles on it, from either side.</summary>
    /// <remarks>
    /// <b>From either side, because a lag that is faster downward than upward is a bias.</b> The gap
    /// is signed and a shift would floor it, so a Cell above its target would creep down for ever
    /// while one below stalled a unit short — the same directional defect as the smear the double
    /// buffer exists to remove, arriving through the rounding instead of through the ordering.
    /// </remarks>
    [Theory]
    [InlineData(0, 1_000)]
    [InlineData(1_000, 0)]
    [InlineData(-400, 400)]
    [InlineData(400, -400)]
    public void Land_value_converges_on_its_target_from_either_side(int start, int target)
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(5);
        Cells north = new(6);

        layers.SetLandValueTarget(east, north, start);
        for (int i = 0; i < 200; i++)
        {
            layers.DriftLandValue();
        }

        Assert.Equal(start, layers.LandValue(east, north));

        layers.SetLandValueTarget(east, north, target);
        for (int i = 0; i < 200; i++)
        {
            layers.DriftLandValue();
        }

        Assert.Equal(target, layers.LandValue(east, north));
    }

    /// <summary>
    /// It is <em>momentum</em>: one update moves part of the way, never all of it.
    /// </summary>
    /// <remarks>
    /// <c>02 §2.4</c>: land value moves slowly toward the current desirability rather than tracking
    /// it, which is both realistic and a stabiliser against oscillation. A Layer that jumped to its
    /// target would have no reason to be stored at all — it would be composable at the point of use,
    /// like every other composite, and <c>02 §2.4</c>'s composition rule would apply to it.
    /// </remarks>
    [Fact]
    public void One_update_moves_part_of_the_way()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(5);
        Cells north = new(6);

        layers.SetLandValueTarget(east, north, 800);
        layers.DriftLandValue();

        int after = layers.LandValue(east, north);

        Assert.True(after > 0, "land value did not move toward its target.");
        Assert.True(after < 800, "land value jumped to its target; it has no momentum.");
        Assert.Equal(100, after);
    }

    /// <summary>Nothing computes desirability, so land value has nowhere to go.</summary>
    /// <remarks>
    /// <b>Zero here is the input being absent, not a placeholder in the mechanism.</b> The mechanism
    /// is exercised by the test above; what is missing is the target, and the place that says so
    /// loudly is <see cref="MapLayers.Desirability"/>. This test exists so that the day something does
    /// compute a target, it fails and somebody deletes it deliberately.
    /// </remarks>
    [Fact]
    public void Land_value_is_zero_everywhere_until_something_computes_desirability()
    {
        World world = new(Population);
        Simulation simulation = new(world, WorldKey.FromSeed(7));

        world.Layers.EmitPollution(new Cells(10), new Cells(10), 500);

        for (int tick = 0; tick < 600; tick++)
        {
            simulation.Step(default);
        }

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Assert.Equal(0, world.Layers.LandValue(new Cells(east), new Cells(north)));
            }
        }
    }

    [Fact]
    public void Sealing_accumulates_and_clamps_at_the_Cell_s_own_Tile_count()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, 1);
        Assert.Equal(1, layers.Sealing(east, north));

        layers.Seal(east, north, 100);
        Assert.Equal(101, layers.Sealing(east, north));

        // One house seals 1/1024 of its Cell (CONTEXT.md -> Sealing), so 1024 is the whole Cell and
        // nothing can seal more of it than there is.
        layers.Seal(east, north, CellGrid.TilesInCell);
        Assert.Equal(CellGrid.TilesInCell, layers.Sealing(east, north));
        Assert.Equal(1_024, CellGrid.TilesInCell);
    }

    /// <summary>Sealing does not decay in Phase 1, because its rate has no terrain type to key on.</summary>
    [Fact]
    public void Sealing_does_not_decay_at_the_default_rate()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, 500);
        layers.DecaySealing();

        Assert.Equal(0, LayerRates.Default.SealingDecayTau);
        Assert.Equal(500, layers.Sealing(east, north));
    }

    /// <summary>And it does decay once a rate exists, which is what the operator is for.</summary>
    [Fact]
    public void Sealing_decays_once_a_rate_is_supplied()
    {
        MapLayers layers = new(new LayerRuleset(
            LayerSchedule.Default,
            new LayerRates(LandValueTau: 8, SealingDecayTau: 4, PollutionTau: 128)));

        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, 1_000);
        layers.DecaySealing();

        Assert.Equal(750, layers.Sealing(east, north));
    }

    /// <summary>
    /// <c>adr/0051</c>: a Cell's pollution source is a stock the environment absorbs.
    /// </summary>
    [Fact]
    public void A_pollution_source_is_absorbed()
    {
        MapLayers layers = Layers(pollutionTau: 4);

        layers.EmitPollution(new Cells(3), new Cells(4), 1_000);
        layers.DecayPollution();

        Assert.Equal(750, layers.PollutionSource(new Cells(3), new Cells(4)));
    }

    /// <summary>
    /// A source reaches <b>exactly zero</b>, which is the tail rule <c>adr/0051</c> left to the
    /// implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Plain <c>value − value/tau</c> stalls, and the residue it leaves is <c>adr/0006</c> in
    /// miniature.</b> Integer division floors, so a source below tau loses nothing and a demolished
    /// factory leaves a permanent stain on the map. The ADR names the problem, declines to solve it,
    /// and says the answer belongs with the implementation — this is it: absorb
    /// <c>max(1, round(value/tau))</c>, which is <see cref="MapLayers"/>'s existing <c>Step</c> helper
    /// and therefore the same arithmetic land value already drifts by. <b>The tail rule was solved in
    /// this file before the question was asked</b>; it needed applying rather than inventing.
    /// </para>
    /// <para>
    /// <b>The floor has a consequence worth stating, because it is the honest cost of the fix.</b> It
    /// quantises the equilibrium: a Cell emitting less than one unit per cadence is absorbed at one
    /// unit per cadence, so its level is pinned around tau rather than being proportional to its rate.
    /// That bounds it — <c>adr/0006</c> is satisfied — but below one unit per cadence the field
    /// reports a floor instead of a rate, and a designer who wants resolution there wants a larger tau
    /// rather than a different rule.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_source_is_absorbed_to_exactly_zero()
    {
        MapLayers layers = Layers(pollutionTau: 4);
        Cells east = new(3);
        Cells north = new(4);

        layers.EmitPollution(east, north, 10);

        for (int i = 0; i < 64; i++)
        {
            layers.DecayPollution();
        }

        Assert.Equal(0, layers.PollutionSource(east, north));
    }

    /// <summary>Zero tau is the pre-<c>adr/0051</c> accumulator, kept reachable and watched to fail.</summary>
    /// <remarks>
    /// The counterfactual, written down rather than described. <b>Without a sink the source is exactly
    /// what it was emitted as, for ever</b> — which is what <c>plans/0011</c> finding 37 found in the
    /// shipped code and what slice 6's long-run test had been written around.
    /// </remarks>
    [Fact]
    public void Without_a_rate_a_pollution_source_only_accumulates()
    {
        MapLayers layers = Layers(pollutionTau: 0);
        Cells east = new(3);
        Cells north = new(4);

        for (int i = 0; i < 16; i++)
        {
            layers.EmitPollution(east, north, 100);
            layers.DecayPollution();
        }

        Assert.Equal(1_600, layers.PollutionSource(east, north));
    }

    /// <summary>
    /// A steady emitter settles at a level proportional to its <b>rate</b>, which is the ADR's headline.
    /// </summary>
    /// <remarks>
    /// <b>This is the claim the accumulator got wrong, stated as a measurement.</b> Under <c>+=</c> the
    /// level counts firings, so it reports <em>how long this has stood</em>; under absorption it settles
    /// where what is added equals what is taken, so it reports <em>how hard this emits</em> — which is
    /// what <c>02 §2.4</c> says a source field holds. Doubling the emission doubles the settled level,
    /// and neither depends on how long the run has been going.
    /// </remarks>
    [Fact]
    public void A_steady_emitter_settles_in_proportion_to_its_rate()
    {
        int single = Settled(emission: 40, cycles: 512);
        int doubled = Settled(emission: 80, cycles: 512);

        // Within 1%, and not exact, because absorption rounds: adr/0003 requires integer arithmetic
        // with stated rounding, so the equilibrium is the fixed point of a rounded map rather than of
        // a real-valued one. Asserting exact proportionality would be asserting that RoundDiv does not
        // round. The residue is 4 parts in 556 and does not grow with the run.
        Assert.True(
            Math.Abs((2 * single) - doubled) * 100 < doubled,
            $"doubling the emission moved the settled level from {single} to {doubled}, which is not "
            + "proportional. adr/0051: the level a steady emitter settles at is proportional to its "
            + "rate, because that is the quantity 02 §2.4 says a source field holds.");

        // The half the accumulator got wrong, and the one that needs no tolerance. Under `+=` the
        // level counts firings, so it answers "how long has this stood"; under absorption it answers
        // "how hard does this emit" and running four times as long changes nothing at all.
        Assert.Equal(single, Settled(emission: 40, cycles: 2_048));

        static int Settled(int emission, int cycles)
        {
            MapLayers layers = Layers(pollutionTau: 8);
            Cells east = new(3);
            Cells north = new(4);

            for (int i = 0; i < cycles; i++)
            {
                layers.EmitPollution(east, north, emission);
                layers.DecayPollution();
            }

            return layers.PollutionSource(east, north);
        }
    }

    private static MapLayers Layers(int pollutionTau) => new(new LayerRuleset(
        LayerSchedule.Default,
        new LayerRates(LandValueTau: 8, SealingDecayTau: 0, PollutionTau: pollutionTau)));

    /// <summary>
    /// The named holes fail loudly rather than returning zero.
    /// </summary>
    /// <remarks>
    /// <c>plans/0009</c> task 7: <b>leave named holes rather than placeholders.</b> A placeholder
    /// returning zero is a value that will be read, believed, and tuned around; a hole that fails
    /// loudly is a hole. Each of these composes or queries something that does not exist — terrain
    /// suitability needs the world generator, and noise, near-road pollution and amenity all need the
    /// Road Graph.
    /// </remarks>
    [Fact]
    public void Every_composite_and_every_line_source_refuses_rather_than_answering()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(1);
        Cells north = new(1);

        Assert.Throws<NotSupportedException>(() => layers.Fertility(east, north));
        Assert.Throws<NotSupportedException>(() => layers.Desirability(east, north));

        Assert.Throws<NotSupportedException>(() => LineSourceQueries.Noise(new Tiles(4), new Tiles(4)));
        Assert.Throws<NotSupportedException>(
            () => LineSourceQueries.NearRoadPollution(new Tiles(4), new Tiles(4)));
        Assert.Throws<NotSupportedException>(() => LineSourceQueries.Amenity(new Tiles(4), new Tiles(4)));
    }

    /// <summary>
    /// Noise is not in <see cref="Layer"/>, and this is the test that notices somebody adding it.
    /// </summary>
    /// <remarks>
    /// <c>02 §2.5</c>'s procedure exists because <em>"add a Map Layer" was the reflex answer four
    /// times running and was the right answer once</em>. This slice is where the reflex would fire.
    /// </remarks>
    [Fact]
    public void There_are_exactly_three_Map_Layers()
    {
        Assert.Equal(
            [Layer.IndustrialPollution, Layer.LandValue, Layer.Sealing],
            Enum.GetValues<Layer>());
    }

    /// <summary>
    /// <c>adr/0003</c>'s magnitude bound, watched firing.
    /// </summary>
    /// <remarks>
    /// <b>The ceiling is the kernel's, not the integer's</b> — a two-pass tent multiplies by 6,561, so
    /// a source above roughly 327,000 cannot be represented diffused. Catching it here rather than
    /// letting the convolution overflow reports the failure at the end of the run that caused it,
    /// instead of at whichever diffusion cadence next happened to touch the plume.
    /// </remarks>
    [Fact]
    public void A_runaway_source_is_reported_by_the_end_of_run_tier()
    {
        World world = new(Population);
        world.Invariants.Collect = true;

        int ceiling = MapLayers.PollutionKernel.SourceCeiling;
        world.Layers.EmitPollution(new Cells(20), new Cells(20), ceiling);

        new Simulation(world, WorldKey.FromSeed(1)).CheckEndOfRun();
        Assert.Empty(world.Invariants.Collected);

        world.Layers.EmitPollution(new Cells(20), new Cells(20), 1);

        new Simulation(world, WorldKey.FromSeed(1)).CheckEndOfRun();
        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.LayerMagnitudeIsBounded);
    }

    /// <summary>Sealing's structural bound, checked rather than trusted.</summary>
    /// <remarks>
    /// The clamp lives at the one write site. This walks the column, because <b>a bound maintained at
    /// one write site is a bound that stops holding the day somebody adds a second</b> — so the check
    /// is written against the storage rather than against the setter.
    /// </remarks>
    [Fact]
    public void An_over_sealed_Cell_is_reported_by_the_end_of_run_tier()
    {
        World world = new(Population);
        world.Invariants.Collect = true;

        world.Layers.Seal(new Cells(2), new Cells(2), 10);
        int slot = world.Layers.Residency.Slot(new Cells(2), new Cells(2));

        // Written past the clamp deliberately: the check must not depend on the setter it is checking.
        world.Layers.Cells.Sealing[slot] = CellGrid.TilesInCell + 1;

        new Simulation(world, WorldKey.FromSeed(1)).CheckEndOfRun();

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.SealingIsWithinTheCell);
    }
}
