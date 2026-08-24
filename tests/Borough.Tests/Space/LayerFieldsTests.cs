using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
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

    /// <summary>
    /// <b>Land value leaves zero, and this test is the one that used to assert it could not.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// What stood here was <c>Land_value_is_zero_everywhere_until_something_computes_desirability</c>,
    /// a named hole whose own remark said it existed <em>so that the day something does compute a
    /// target, it fails and somebody deletes it deliberately</em>. Milestone 9 task 4 is that day and
    /// this is that deletion: the assertion is inverted rather than removed, so the hole leaves a test
    /// behind instead of a gap. ⚠ <b>A hole that closes silently is a hole nobody can tell closed.</b>
    /// </para>
    /// <para>
    /// It runs a whole <see cref="Simulation"/> rather than calling the producer, because the claim is
    /// about the wiring: phase 5 has to reach <c>SetLandValueTargets</c> on the land value cadence,
    /// carrying the world's own Road Graph, and nothing below the phase can show that.
    /// </para>
    /// </remarks>
    [Fact]
    public void Land_value_leaves_zero_once_something_computes_desirability()
    {
        World world = new(Population);
        Simulation simulation = new(world, WorldKey.FromSeed(7));
        Cells east = new(10);
        Cells north = new(10);

        world.Layers.EmitPollution(east, north, 500);

        for (int tick = 0; tick < 600; tick++)
        {
            simulation.Step(default);
        }

        int fouled = world.Layers.LandValue(east, north);

        Assert.True(fouled < 0, $"the polluted Cell is worth less than nothing, not {fouled}");

        // And a Cell far from the source is still zero -- the producer wrote a target there too, and
        // the target was zero, which is a true statement about clean empty ground rather than a hole.
        Assert.Equal(0, world.Layers.LandValue(new Cells(300), new Cells(300)));
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

    /// <summary>
    /// A Ruleset stating no <c>[[terrain]]</c> heals nowhere, which is every world before milestone 24.
    /// </summary>
    [Fact]
    public void Sealing_does_not_decay_where_the_Ruleset_states_no_terrain()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, 500);
        layers.DecaySealing(TerrainRuleset.None);

        Assert.False(TerrainRuleset.None.Stated);
        Assert.Equal(500, layers.Sealing(east, north));
    }

    /// <summary>And it decays at the rate its own terrain type states. <c>02 §2.4</c>.</summary>
    [Fact]
    public void Sealing_decays_at_its_terrain_types_rate()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, 1_000);
        layers.DecaySealing(Tau(ordinary: 4));

        Assert.Equal(TerrainKind.Ordinary, layers.Terrain.At(east, north));
        Assert.Equal(750, layers.Sealing(east, north));
    }

    /// <summary>A type stating zero never recovers, which is what <c>rock</c> is for.</summary>
    [Fact]
    public void Sealing_does_not_decay_where_its_terrain_type_states_zero()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, 500);
        layers.DecaySealing(Tau(ordinary: 0));

        Assert.Equal(500, layers.Sealing(east, north));
    }

    /// <summary>
    /// 🔴 The regression: exponential decay in integers <b>stalls</b>, and the floor is what stops it.
    /// </summary>
    /// <remarks>
    /// <c>value -= RoundDiv(value, tau)</c> rounds its decrement to zero once the value falls under
    /// <c>tau ÷ 2</c>, so ground would settle at a permanent residue and never reach bare. Measured
    /// before the fix: tau 8 stalled at 3. <c>CONTEXT.md</c> → Sealing states an endpoint —
    /// <em>"floodplain may recover over hundreds of Days"</em> — and a curve that never arrives cannot
    /// deliver one.
    /// </remarks>
    [Fact]
    public void Sealing_reaches_bare_ground_rather_than_stalling_above_it()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);
        TerrainRuleset terrain = Tau(ordinary: 8);

        layers.Seal(east, north, CellGrid.TilesInCell);

        for (int update = 0; update < 1_000; update++)
        {
            layers.DecaySealing(terrain);
        }

        Assert.Equal(0, layers.Sealing(east, north));
    }

    /// <summary>
    /// 🔴 And a tau above twice the Cell moved <b>nothing at all</b>, which is the same defect at the
    /// other end.
    /// </summary>
    /// <remarks>
    /// <c>RoundDiv(1024, 2400)</c> is zero, so a fully-sealed Cell never took a first step. The floor
    /// makes the whole band <c>1..2 × TilesInCell</c> — which is what the loader admits — do something.
    /// </remarks>
    [Fact]
    public void Sealing_moves_even_where_the_tau_exceeds_the_Cell()
    {
        MapLayers layers = new(LayerRuleset.Default);
        Cells east = new(3);
        Cells north = new(4);

        layers.Seal(east, north, CellGrid.TilesInCell);
        layers.DecaySealing(Tau(ordinary: CellGrid.TilesInCell * 2));

        Assert.Equal(CellGrid.TilesInCell - 1, layers.Sealing(east, north));
    }

    /// <summary>Sealing's own cadence, which it did not have before milestone 24 task 4.</summary>
    [Fact]
    public void Sealing_decays_on_its_cadence_and_not_on_another_Layers()
    {
        var world = new World(1_000, Ruleset.Empty);
        Cells east = new(3);
        Cells north = new(4);
        LayerCadence cadence = world.Layers.Schedule.Sealing;

        world.Layers.Seal(east, north, 1_000);
        world.Layers.Step(new Ticks((ulong)cadence.Offset + 1), world.Roads, Tau(ordinary: 4));

        Assert.Equal(1_000, world.Layers.Sealing(east, north));

        world.Layers.Step(new Ticks((ulong)cadence.Offset), world.Roads, Tau(ordinary: 4));

        Assert.Equal(750, world.Layers.Sealing(east, north));
    }

    /// <summary>A <c>[[terrain]]</c> table whose only interesting key is <c>ordinary</c>'s tau.</summary>
    private static TerrainRuleset Tau(int ordinary) => TerrainRuleset.From(
        Fixed.One, Fixed.One, Fixed.One, Fixed.One, Fixed.One,
        ordinaryDecayTau: ordinary,
        rockDecayTau: 0,
        floodplainDecayTau: 0,
        marshDecayTau: 0,
        thinSoilDecayTau: 0);

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
        new LayerRates(LandValueTau: 8, PollutionTau: pollutionTau, WoodlandRegrowthDays: 0)));

    /// <summary>
    /// The named holes fail loudly rather than returning zero.
    /// </summary>
    /// <remarks>
    /// <c>plans/0009</c> task 7: <b>leave named holes rather than placeholders.</b> A placeholder
    /// returning zero is a value that will be read, believed, and tuned around; a hole that fails
    /// loudly is a hole.
    /// <para>
    /// ⚠ <b>This test used to name five holes and now names one, and the four that left did so for
    /// four different reasons.</b> Noise and near-road pollution were built by milestone 9 task 1;
    /// <see cref="MapLayers.Desirability"/> composes as of task 2 — <b>partially</b>, and the shortfall
    /// is policed by <c>DesirabilityShortfallTests</c> rather than by a hole. <b>Fertility composes as
    /// of milestone 24 task 5</b> (<c>adr/0155</c>), which is the hole this test was written around
    /// closing. What is left is amenity, which needs a <b>kind</b> on a Business at milestone 15 —
    /// <em>not</em> the Road Graph, which shipped in 5a and which this test's own remark named as the
    /// blocker for three fields it was not the blocker for.
    /// <para>
    /// ⚠ <b>A partial composition is exactly what this discipline does NOT cover</b>, and that is worth
    /// saying in the file that owns it. A hole that throws is safe because nothing can read it;
    /// desirability now returns plausible numbers with its only positive term missing, and no assertion
    /// here can tell that apart from a finished field.
    /// </para>
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_composite_and_every_line_source_refuses_rather_than_answering()
    {
        Assert.Throws<NotSupportedException>(() => LineSourceQueries.Amenity(new Tiles(4), new Tiles(4)));
    }

    /// <summary>
    /// Noise is not in <see cref="Layer"/>, and this is the test that notices somebody adding it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §2.5</c>'s procedure exists because <em>"add a Map Layer" was the reflex answer four
    /// times running and was the right answer once</em>. This slice is where the reflex would fire.
    /// </para>
    /// <para>
    /// ⚠ <b>It was <em>three</em> until milestone 24 task 8b and <see cref="Layer.Woodland"/> is the
    /// fourth, which is this test doing its job rather than failing at it.</b> The two additions this
    /// milestone made — Sealing and Woodland — are both <b>counts per Cell with no kernel and no
    /// range</b>, and what puts them on this enum is neither a field nor a query: it is that each
    /// happens <em>on a clock</em>, so each needs a cadence and the cadences must be staggered apart.
    /// ***This enum is the stagger's membership list, and a Layer that is not a field is still a Layer
    /// for that purpose.***
    /// </para>
    /// </remarks>
    [Fact]
    public void There_are_exactly_four_Map_Layers()
    {
        Assert.Equal(
            [Layer.IndustrialPollution, Layer.LandValue, Layer.Sealing, Layer.Woodland],
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

        int ceiling = world.Layers.PollutionKernel.SourceCeiling;
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
