using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 5: <c>fertility = base − base·Sealing/1024 − w_p·pollution</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0156</c>. <b>The hole at <c>MapLayers.Fertility</c> closes here</b>, and what it closes into
/// is a composition at the point of use — there is no fertility column, no fertility Layer and nothing
/// baked (<c>adr/0155</c>, <c>02 §2.4</c>).
/// </para>
/// <para>
/// ⚠ <b>Two properties are asserted that a reader would expect to be bugs, and both are decisions.</b>
/// Fertility <b>goes negative and does not clamp</b>, because Sealing decays and the ordering between
/// exhausted Cells is what <c>adr/0022</c>'s recovery arc runs on. And it <b>saturates rather than
/// throwing</b>, on <c>LineSourceQueries.Saturate</c>'s rule that a read-only query must not throw on
/// a world somebody is allowed to build.
/// </para>
/// <para>
/// 🔴 <b>Nothing consumes Fertility yet</b> — no milestone in <c>06</c> builds a farm — so every test
/// here reads the producer directly. <c>w_p</c> is unratified and these tests are written so that
/// <b>retuning it fails none of them</b>: the arithmetic tests state the weight they use rather than
/// reading the shipped one, and the shipped value has one test of its own that says what it is for.
/// </para>
/// </remarks>
public sealed class FertilityTests
{
    private const int Citizens = 1_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x5EA1U);

    /// <summary>The five Base Fertilities <c>rulesets/varied.toml</c> states, as Q16.16.</summary>
    private static readonly TerrainRuleset Priced = TerrainRuleset.From(
        Percent(100), Percent(20), Percent(100), Percent(50), Percent(60),
        ordinaryDecayTau: 96,
        rockDecayTau: 0,
        floodplainDecayTau: 48,
        marshDecayTau: 64,
        thinSoilDecayTau: 160);

    private static int Percent(int percent) => IntegerMath.RoundDiv(Fixed.FromInt(percent), 100);

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    /// <summary>A world with terrain laid, so a Cell has a type to key off.</summary>
    private static World Generated(string file = "varied.toml")
    {
        World world = new(Citizens, Load(file), Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>
    /// The first Cell of a given terrain type that the city has not touched.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The <em>untouched</em> half is not decoration.</b> The first Cell of a type is very often
    /// one the generated city stands on — <c>SyntheticCity</c> lays roads and Buildings before a test
    /// gets the world, and those seal ground. A test that read the first Cell of a type and expected
    /// the Ruleset's number back would fail for a reason having nothing to do with Fertility.
    /// </remarks>
    private static (Cells East, Cells North) UntouchedCellOf(World world, TerrainKind kind)
    {
        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                if (world.Layers.Terrain.At(x, y) == kind
                    && world.Layers.Sealing(x, y) == 0
                    && world.Layers.Pollution(x, y) == 0)
                {
                    return (x, y);
                }
            }
        }

        throw new InvalidOperationException($"no untouched Cell of {kind} on this map.");
    }

    // ---- untouched ground -------------------------------------------------------------------------

    /// <summary>
    /// Untouched ground farms at exactly its Base Fertility.
    /// </summary>
    /// <remarks>
    /// <b>The ceiling, and the property that makes the scale readable.</b> With
    /// <see cref="Fixed.One"/> fully fertile, a Cell nobody has built on or polluted returns the
    /// Ruleset's own number back — so <c>adr/0022</c>'s <em>"41%"</em> panel needs no conversion and
    /// no denominator anybody has to name.
    /// </remarks>
    [Theory]
    [InlineData(TerrainKind.Ordinary, 100)]
    [InlineData(TerrainKind.Rock, 20)]
    [InlineData(TerrainKind.Floodplain, 100)]
    [InlineData(TerrainKind.Marsh, 50)]
    [InlineData(TerrainKind.ThinSoil, 60)]
    public void Untouched_ground_farms_at_its_base_fertility(TerrainKind kind, int percent)
    {
        World world = Generated();
        (Cells east, Cells north) = UntouchedCellOf(world, kind);

        // A Cell with no row has neither Sealing nor pollution, which is what untouched means here.
        Assert.Equal(0, world.Layers.Sealing(east, north));
        Assert.Equal(0, world.Layers.Pollution(east, north));

        Assert.Equal(
            Percent(percent),
            world.Layers.Fertility(Priced, new FertilityWeights(0), east, north));
    }

    /// <summary>Terrain is what makes two untouched Cells differ, and nothing else can.</summary>
    /// <remarks>
    /// 🔴 <b>The whole point of <c>adr/0158</c>, checked at the consumer rather than at the column.</b>
    /// Before terrain varied, every Cell in a city farmed identically until the player touched it, and
    /// <c>adr/0022</c>'s <em>agriculture and housing repel each other</em> dynamic had nothing to push
    /// against on Tick 0.
    /// </remarks>
    [Fact]
    public void Two_untouched_cells_of_different_ground_farm_differently()
    {
        World world = Generated();
        (Cells rockEast, Cells rockNorth) = UntouchedCellOf(world, TerrainKind.Rock);
        (Cells goodEast, Cells goodNorth) = UntouchedCellOf(world, TerrainKind.Ordinary);

        FertilityWeights none = new(0);

        Assert.True(
            world.Layers.Fertility(Priced, none, rockEast, rockNorth)
            < world.Layers.Fertility(Priced, none, goodEast, goodNorth),
            "rock farms at least as well as ordinary ground, so the terrain term is doing nothing.");
    }

    // ---- the Sealing term -------------------------------------------------------------------------

    /// <summary>
    /// A fully sealed Cell farms at exactly zero, whatever its ground was.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the endpoint that <em>derives</em> <c>w_s</c>, so it is the one test in this file
    /// that would have to change for the coefficient to become a Ruleset key.</b> <c>CONTEXT.md</c> →
    /// Sealing makes a Cell at <see cref="CellGrid.TilesInCell"/> one whose every Tile is built on,
    /// and ground with no soil left exposed grows nothing. ***A coefficient with an endpoint is not a
    /// tuning knob.***
    /// </remarks>
    [Theory]
    [InlineData(TerrainKind.Ordinary)]
    [InlineData(TerrainKind.Rock)]
    [InlineData(TerrainKind.Marsh)]
    public void A_fully_sealed_cell_farms_at_zero(TerrainKind kind)
    {
        World world = Generated();
        (Cells east, Cells north) = UntouchedCellOf(world, kind);

        world.Layers.Seal(east, north, CellGrid.TilesInCell);

        Assert.Equal(0, world.Layers.Fertility(Priced, new FertilityWeights(0), east, north));
    }

    /// <summary>Half-sealed ordinary ground farms at half.</summary>
    /// <remarks>
    /// The term between the two endpoints, and it is linear because the endpoint pins the slope: the
    /// term is <c>base × Sealing / 1024</c> and there is no curve anybody chose.
    /// </remarks>
    [Fact]
    public void Half_sealed_ground_farms_at_half_its_ceiling()
    {
        World world = Generated();
        (Cells east, Cells north) = UntouchedCellOf(world, TerrainKind.Ordinary);

        world.Layers.Seal(east, north, CellGrid.TilesInCell / 2);

        Assert.Equal(
            Percent(100) / 2,
            world.Layers.Fertility(Priced, new FertilityWeights(0), east, north));
    }

    // ---- the pollution term -----------------------------------------------------------------------

    /// <summary>
    /// Pollution is a count and the weight is a ratio, so the product is already Q16.16.
    /// </summary>
    /// <remarks>
    /// <b><see cref="MapLayers.Desirability"/>'s rule, reused rather than reinvented</b>
    /// (<c>adr/0018</c>'s discipline inside the codebase). Lifting the count into Q16.16 first is
    /// arithmetically the same and overflows at a magnitude
    /// <c>Invariant.LayerMagnitudeIsBounded</c> calls legal.
    /// </remarks>
    [Theory]
    [InlineData(4, 12)]
    [InlineData(4, 0)]
    [InlineData(50, 1)]
    [InlineData(0, 1_000)]
    public void The_pollution_term_is_the_weight_times_the_count(int weightPercent, int pollution)
    {
        World world = Generated();
        (Cells east, Cells north) = UntouchedCellOf(world, TerrainKind.Ordinary);

        world.Layers.EmitPollution(east, north, pollution);
        world.Layers.Step(new Ticks(0), world.Roads, Priced);

        int weight = Percent(weightPercent);
        int measured = world.Layers.Pollution(east, north);

        Assert.Equal(
            Percent(100) - (weight * measured),
            world.Layers.Fertility(Priced, new FertilityWeights(weight), east, north));
    }

    // ---- the two decisions a reader would take for bugs ---------------------------------------------

    /// <summary>
    /// Fertility goes <b>negative</b> and does not clamp.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Deliberate, and the reason is not the panel.</b> Evidence reads the <em>terms</em>, so a
    /// clamp would not hurt the decomposition. What a clamp destroys is the <b>ordering between
    /// exhausted Cells</b> — Sealing decays, so <c>base − 1.4·base</c> and <c>base − 3·base</c> are
    /// two Cells at very different distances from farming again, and clamped they are one number.
    /// ***That ordering is what <c>adr/0022</c>'s cyclical land-use arc runs on.***
    /// </para>
    /// <para>
    /// A consumer that wants <em>is there a farm here</em> takes <c>≤ 0</c> and loses nothing.
    /// </para>
    /// </remarks>
    [Fact]
    public void Fertility_goes_negative_and_keeps_the_ordering_between_exhausted_cells()
    {
        World world = Generated();
        (Cells lightly, Cells lightlyNorth) = UntouchedCellOf(world, TerrainKind.Ordinary);
        (Cells heavily, Cells heavilyNorth) = UntouchedCellOf(world, TerrainKind.Floodplain);

        FertilityWeights weight = new(Percent(100));

        // Emitted and diffused BEFORE the ground is sealed, because Step also runs the decays and a
        // Cell sealed first would be read after one of them.
        world.Layers.EmitPollution(lightly, lightlyNorth, 1_000);
        world.Layers.EmitPollution(heavily, heavilyNorth, 40_000);
        world.Layers.Step(new Ticks(0), world.Roads, Priced);

        world.Layers.Seal(lightly, lightlyNorth, CellGrid.TilesInCell);
        world.Layers.Seal(heavily, heavilyNorth, CellGrid.TilesInCell);

        // The plume has to have REACHED these Cells for the test to be about the pollution term.
        Assert.True(world.Layers.Pollution(lightly, lightlyNorth) > 0);
        Assert.True(
            world.Layers.Pollution(heavily, heavilyNorth)
            > world.Layers.Pollution(lightly, lightlyNorth));

        int lighter = world.Layers.Fertility(Priced, weight, lightly, lightlyNorth);
        int heavier = world.Layers.Fertility(Priced, weight, heavily, heavilyNorth);

        Assert.True(lighter < 0, $"a dead Cell reads {lighter}, so something is clamping.");
        Assert.True(
            heavier < lighter,
            "two exhausted Cells at different depths read the same way round or equal, so the "
            + "recovery ordering adr/0022 runs on has been flattened.");
    }

    /// <summary>
    /// It <b>saturates</b> rather than throwing, on a world somebody is allowed to build.
    /// </summary>
    /// <remarks>
    /// <c>LineSourceQueries.Saturate</c>'s reasoning: a read-only query must not throw on a legal
    /// world, and what catches a world gone mad is <c>Invariant.LayerMagnitudeIsBounded</c> at end of
    /// run — a better instrument than an exception raised wherever somebody happened to read a Cell.
    /// </remarks>
    [Fact]
    public void An_absurd_weight_saturates_rather_than_throwing()
    {
        World world = Generated();
        (Cells east, Cells north) = UntouchedCellOf(world, TerrainKind.Ordinary);

        world.Layers.EmitPollution(east, north, 1_000_000);
        world.Layers.Step(new Ticks(0), world.Roads, Priced);

        int fertility = world.Layers.Fertility(
            Priced, new FertilityWeights(int.MaxValue), east, north);

        Assert.Equal(int.MinValue, fertility);
    }

    // ---- the Ruleset that prices nothing -------------------------------------------------------------

    /// <summary>
    /// A Ruleset that prices no ground refuses the lookup rather than answering zero.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This is not the saturation rule failing to apply; it is a different question.</b>
    /// Saturation is about a <em>value</em> on a world somebody built. This is about a
    /// <em>declaration</em> that is absent — it fires on every Cell alike and immediately, which is
    /// the shape a configuration error should have. A zero would be a number somebody reads, believes
    /// and tunes around.
    /// </remarks>
    [Fact]
    public void A_ruleset_that_prices_no_ground_refuses_the_lookup()
    {
        World world = Generated("minimal.toml");

        Assert.Throws<InvalidOperationException>(
            () => world.Layers.Fertility(
                TerrainRuleset.None, new FertilityWeights(0), new Cells(1), new Cells(1)));
    }

    // ---- what the shipped Ruleset says ---------------------------------------------------------------

    /// <summary>
    /// The shipped <c>w_p</c> costs a Cell under a strong plume about what <c>adr/0022</c>'s
    /// specimen says it should.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The one test that reads the shipped weight, and it exists so that retuning it fails
    /// exactly one thing.</b> Every other test here states its own weight. <c>adr/0022</c>'s Evidence
    /// specimen — <em>"41% — ground sealed 12%, pollution from Eastfield Industrial 47%"</em> — is the
    /// only sentence in the corpus that says what a plume should cost a farm, and 4% against a
    /// measured plume of about 12 kernel units puts it near that.
    /// </para>
    /// <para>
    /// ⚠ <b>Asserted as a BAND and not a figure.</b> The specimen is a mock-up, the plume magnitude is
    /// a measurement of a different Ruleset, and the weight is unratified — so a test on the exact
    /// number would be pinning three soft things together. What must not regress is the order of
    /// magnitude: a strong plume costs a meaningful part of a Cell's fertility and does not wipe it
    /// out on its own.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shipped_weight_makes_a_strong_plume_cost_a_meaningful_fraction()
    {
        Ruleset ruleset = Load("varied.toml");
        FertilityWeights shipped = ruleset.Layers.Fertility;

        // Stated rather than defaulted, and only here: it is the only shipped file whose Fertility
        // can be computed at all, so it is the only one where the weight is a number somebody reads.
        Assert.Contains(
            "fertility_pollution_percent",
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "varied.toml")),
            StringComparison.Ordinal);
        Assert.Equal(FertilityWeights.Default, shipped);

        // The magnitude DesirabilityWeights.Default records under a strong source.
        const int StrongPlume = 12;

        long cost = (long)shipped.Pollution * StrongPlume;

        Assert.True(
            cost > Fixed.One / 4 && cost < Fixed.One,
            $"a strong plume costs {cost} of a ceiling of {Fixed.One}, which is either negligible or "
            + "enough to sterilise unsealed ground on its own. adr/0022's specimen puts it near half.");
    }
}
