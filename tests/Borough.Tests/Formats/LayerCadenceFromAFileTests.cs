using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Formats;

/// <summary>
/// Slice 8 task 8: the Map Layer cadence and rates come from a Ruleset file, which is this slice's
/// externally-stated definition of done.
/// </summary>
/// <remarks>
/// <para>
/// <b>Task 3 built the loader half and this is the half that was always the point.</b> Reading
/// <c>[layers]</c> is worth nothing while every shipped Ruleset accepts the defaults, because a
/// designer cannot tune a number they cannot find — session A retired <c>06</c>'s <em>must not slip
/// behind 3c</em> claim and put this checkable obligation in its place.
/// </para>
/// <para>
/// <b>The claim under it is <c>adr/0044</c>'s, and it has to be re-run through the file.</b> The ADR
/// measured that two worlds differing only in the diffusion period produce different hash traces —
/// which is what makes the cadence the designer's number rather than the profiler's — and it measured
/// that through a <em>constructor argument</em>. A number that reaches the world through a parser can
/// be lost anywhere along the way, and the loss would look like a Ruleset key nobody had noticed was
/// inert.
/// </para>
/// <para>
/// <b>It needs an emitting fixture, and finding that out is worth recording.</b>
/// <c>rulesets/minimal.toml</c> emits no pollution, so its field is zero everywhere; diffusing zero at
/// any period gives zero, and the golden session <em>cannot see its own cadence at all</em>. The
/// committed baselines are therefore no evidence for this claim and never could have been.
/// </para>
/// </remarks>
public sealed class LayerCadenceFromAFileTests
{
    /// <summary>
    /// One industrial kind whose Rule emits into the pollution Layer every 8 Ticks, for ever.
    /// </summary>
    /// <remarks>
    /// <c>{0}</c> is the pollution period. Nothing else in the file differs between the two runs, which
    /// is what makes the comparison below a statement about the cadence rather than about the city.
    /// </remarks>
    private const string Emitting = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "works"
        bins = [
            { resource = "sundries", capacity = 64 },
        ]

        [[rule]]
        name    = "smoke"
        kind    = "works"
        rate    = 8
        apply   = { min = 1, max = 1 }
        inputs  = []
        outputs = [
            { scope = "local", resource = "sundries", amount = 1 },
            { scope = "map",   layer    = "pollution", amount = 40 },
        ]

        [layers]
        pollution_period = {0}
        """;

    private const byte Works = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0001UL);

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>A works, running long enough for the plume to have been diffused several times.</summary>
    private static ulong Run(int pollutionPeriod, int ticks)
    {
        Ruleset rules = Load(
            Emitting.Replace("{0}", pollutionPeriod.ToString(), StringComparison.Ordinal));

        var world = new World(1_000, rules);
        var simulation = new Simulation(world, Key, RulesetCatalogue.None);

        Handle<Lot> lot = world.Lots.Create(new Tiles(64), new Tiles(64), zone: 1);
        world.CreateBuilding(lot, Works, Ticks.Zero, Key);

        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(new TickInput(default, 0x1111_1111_1111_1111UL));
        }

        return world.HashState();
    }

    /// <summary>
    /// <c>adr/0044</c>, re-run through the file: a stated cadence is hash-bearing.
    /// </summary>
    /// <remarks>
    /// <b>This is the acceptance clause, and it is stated over the State Hash rather than over the
    /// field.</b> A test that read the Layer back would pass on a cadence that reached the diffusion
    /// and not the save; a test on the hash covers the whole path, which is what makes a hot-reloadable
    /// number a design change rather than an optimisation (<c>05 §4</c>).
    /// </remarks>
    [Fact]
    public void A_cadence_stated_in_a_file_moves_the_hash_trace()
    {
        const int Ticks = 300;

        Assert.NotEqual(Run(pollutionPeriod: 64, Ticks), Run(pollutionPeriod: 32, Ticks));
    }

    /// <summary>
    /// The same city at the same cadence is the same city, which is what makes the line above evidence.
    /// </summary>
    [Fact]
    public void The_same_stated_cadence_reproduces_the_same_hash()
    {
        const int Ticks = 300;

        Assert.Equal(Run(pollutionPeriod: 64, Ticks), Run(pollutionPeriod: 64, Ticks));
    }

    // ---- the shipped Ruleset ---------------------------------------------------------------------

    /// <summary>
    /// <c>rulesets/minimal.toml</c> states the Layer numbers rather than accepting them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The values are <c>02 §2.4</c>'s and stating one is not choosing it</b>, so this asserts they
    /// are still the documented ones. What it cannot assert is the difference between *stated* and
    /// *defaulted* — the two are the same Ruleset by construction, which is the next line's job.
    /// </para>
    /// <para>
    /// <b>They are unratified, and the assertion is deliberately against the constants rather than
    /// against <c>LayerRuleset.Default</c> alone.</b> A future edit that moved the default and the file
    /// together would otherwise pass here while changing every city.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shipped_ruleset_states_the_documented_layer_numbers()
    {
        Ruleset rules = GoldenFixtures.Rules();

        Assert.Equal(new LayerCadence(64, 0), rules.Layers.Schedule.IndustrialPollution);
        Assert.Equal(new LayerCadence(256, 16), rules.Layers.Schedule.LandValue);
        Assert.Equal(1_024, rules.Layers.Constants.IndustrialPollutionMetres);
        Assert.Equal(8, rules.Layers.Rates.LandValueTau);
        Assert.Equal(0, rules.Layers.Rates.SealingDecayTau);

        // One Day, counted in scheduled updates: 8192 Ticks over a period of 64.
        Assert.Equal(128, rules.Layers.Rates.PollutionTau);
    }

    /// <summary>
    /// The shipped Ruleset states exactly what it would have defaulted to, which is why adding
    /// <c>[layers]</c> moved no State Hash.
    /// </summary>
    /// <remarks>
    /// <b>The baselines still had to be re-recorded, and the reason is worth keeping apart from the
    /// numbers.</b> Editing the file changes its <em>content hash</em>, which the golden session
    /// records and the runner refuses a mismatch on — so a re-baseline followed from the edit rather
    /// than from anything the city did differently. If this line ever fails, the file has started
    /// stating a different city and the trace has to move with it.
    /// </remarks>
    [Fact]
    public void The_shipped_layer_numbers_are_the_defaults_written_down()
    {
        Assert.Equal(LayerRuleset.Default, GoldenFixtures.Rules().Layers);
    }
}
