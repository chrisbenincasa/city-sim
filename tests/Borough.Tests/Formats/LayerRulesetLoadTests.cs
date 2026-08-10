using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Slice 8 task 3: <c>[layers]</c>, the derivation inside it, and the world-creation refusal.
/// </summary>
/// <remarks>
/// <para>
/// <b>The refusals here share a symptom with slice 10's three, and it is the symptom that makes them
/// worth having</b>: each one produces a Ruleset that loads clean, runs for ever and quietly does
/// nothing — a Layer whose offset can never come round, a plume whose decay rounds to <em>never</em>.
/// Nothing in a running city distinguishes those from a designer's intent, which is why they are
/// caught at the one moment a human is looking at the file.
/// </para>
/// <para>
/// <b>The world-creation refusal is the first check in the project that is a property of a file
/// against a <em>world</em></b> rather than of the file alone (<c>plans/0015</c> item 4). It is tested
/// on both surfaces: the loader's, where it can say a file and a line, and the core's, which is the
/// backstop for a caller that never asked the loader.
/// </para>
/// </remarks>
public sealed class LayerRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    [Fact]
    public void A_Ruleset_with_no_layers_table_is_a_complete_Ruleset()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.Equal(LayerSchedule.Default.IndustrialPollution, ruleset.Layers.Schedule.IndustrialPollution);
        Assert.Equal(LayerSchedule.Default.LandValue, ruleset.Layers.Schedule.LandValue);
        Assert.Equal(LayerRates.Default, ruleset.Layers.Rates);
        Assert.Equal(LayerConstants.Default, ruleset.Layers.Constants);
    }

    [Fact]
    public void The_stated_cadence_reaches_the_world_it_builds()
    {
        Ruleset ruleset = Accepted($"""
            {Nothing}

            [layers]
            pollution_period = 32
            pollution_offset = 7
            land_value_period = 128
            land_value_offset = 3
            """);

        var world = new World(1_000, ruleset);

        Assert.Equal(new LayerCadence(32, 7), world.Layers.Schedule.IndustrialPollution);
        Assert.Equal(new LayerCadence(128, 3), world.Layers.Schedule.LandValue);
    }

    /// <summary>
    /// <c>plans/0015</c> decision owed 2, and the reason it was a decision rather than plumbing.
    /// </summary>
    /// <remarks>
    /// The file states a <b>duration</b>; the tau it becomes is a count of scheduled updates. Doubling
    /// the period must halve the tau on its own — a file listing <c>128</c> as a literal would keep
    /// saying <em>one Day</em> while meaning two.
    /// </remarks>
    [Theory]
    [InlineData(64, 128)]
    [InlineData(128, 64)]
    [InlineData(32, 256)]
    public void The_pollution_tau_is_derived_from_the_duration_and_the_cadence(int period, int tau)
    {
        Ruleset ruleset = Accepted($"""
            {Nothing}

            [layers]
            pollution_period = {period}
            pollution_decay_ticks = 8192
            """);

        Assert.Equal(tau, ruleset.Layers.Rates.PollutionTau);
    }

    [Fact]
    public void The_default_rates_are_the_derivation_run_on_the_default_cadence()
    {
        // 8192 / 64 = 128, which is what LayerRates.Default used to state as a literal. If the
        // default cadence ever moves, this is the assertion that notices the tau moved with it.
        Assert.Equal(128, LayerRates.Default.PollutionTau);
    }

    [Fact]
    public void An_offset_its_Layer_can_never_reach_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [layers]
            pollution_period = 64
            pollution_offset = 64
            """);

        Assert.Contains("never be recomputed", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(7, refusal.Line);
    }

    [Fact]
    public void A_negative_offset_is_refused_before_it_can_be_compared()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [layers]
            land_value_offset = -1
            """);

        Assert.Contains("land_value_offset = -1 is out of range", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("pollution_period = 0", "expressible as a period")]
    [InlineData("land_value_period = 0", "expressible as a period")]
    [InlineData("land_value_tau = 0", "A time constant divides")]
    [InlineData("kernel_metres = 0", "no reach")]
    [InlineData("sealing_decay_tau = -1", "scheduled updates")]
    [InlineData("pollution_decay_ticks = -1", "duration in Ticks")]
    public void A_Layer_number_outside_its_range_is_refused_by_name(string line, string because)
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [layers]
            {line}
            """);

        Assert.Contains(because, refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(6, refusal.Line);
    }

    /// <summary>
    /// The one refusal that is a property of two numbers together rather than of either.
    /// </summary>
    /// <remarks>
    /// A decay of 30 Ticks at a period of 64 rounds to zero scheduled updates, and zero is the
    /// sentinel for <em>never</em> — so the file reads as "fades within a minute" and behaves as
    /// "never fades". Accepting it would put the derivation's own failure mode back.
    /// </remarks>
    [Fact]
    public void A_decay_shorter_than_the_cadence_it_runs_at_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [layers]
            pollution_period = 64
            pollution_decay_ticks = 30
            """);

        Assert.Contains("rounds to zero scheduled updates", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("zero means never", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_decay_that_rounds_to_one_update_is_not_refused()
    {
        // RoundDiv, not truncation: 33 / 64 rounds to 1. The boundary is where the designer's
        // number stops meaning anything, not where the division stops being exact.
        Ruleset ruleset = Accepted($"""
            {Nothing}

            [layers]
            pollution_period = 64
            pollution_decay_ticks = 33
            """);

        Assert.Equal(1, ruleset.Layers.Rates.PollutionTau);
    }

    [Fact]
    public void A_second_layers_table_is_refused_rather_than_merged()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[layers]]
            pollution_period = 32

            [[layers]]
            pollution_period = 64
            """);

        Assert.Contains("a second [layers]", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_section_names_layers_among_the_ones_that_exist()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [layer]
            pollution_period = 32
            """);

        Assert.Contains("[layers]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the world-creation refusal -----------------------------------------------------------

    private static RulesetLoadResult Reload(string toml, int frozenMetres) =>
        RulesetLoader.Reload(toml, "test.toml", new LayerConstants(frozenMetres));

    [Fact]
    public void A_reload_that_leaves_the_kernel_alone_is_accepted()
    {
        RulesetLoadResult result = Reload($"""
            {Nothing}

            [layers]
            kernel_metres = 1024
            pollution_period = 32
            """, frozenMetres: 1_024);

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(32, result.Ruleset!.Layers.Schedule.IndustrialPollution.Period);
    }

    [Fact]
    public void A_reload_that_omits_the_table_entirely_still_states_the_default_kernel()
    {
        // The absent key is not "no opinion" -- a file with no [layers] is a file asking for the
        // documented defaults, and if the world was created with something else that is a change.
        RulesetLoadResult result = Reload(Nothing, frozenMetres: 2_048);

        Assert.False(result.Ok, "a silently different kernel was accepted.");
        Assert.Contains("world-creation constant", result.Refusals[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reload_that_changes_the_kernel_is_refused_and_says_why()
    {
        RulesetLoadResult result = Reload($"""
            {Nothing}

            [layers]
            kernel_metres = 2048
            """, frozenMetres: 1_024);

        Assert.False(result.Ok, "a changed kernel radius was accepted.");

        RulesetRefusal refusal = result.Refusals[0];

        Assert.Contains("adr/0015", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("stays live", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(6, refusal.Line);
    }

    /// <summary>
    /// <b>The comparison is on Cells, not on metres, and this is the test that pins it.</b>
    /// </summary>
    /// <remarks>
    /// 1,000 m and 1,024 m are both 8 Cells, so the kernel they build is the same array and no stored
    /// Cell is reinterpreted. <c>adr/0015</c>'s membership test asks whether existing state was
    /// recorded in units of the constant; here it was not, because the constant the state is in is the
    /// Cell count. Refusing this would be refusing a reload that changes nothing.
    /// </remarks>
    [Fact]
    public void A_reload_that_changes_the_metres_but_not_the_Cells_is_accepted()
    {
        RulesetLoadResult result = Reload($"""
            {Nothing}

            [layers]
            kernel_metres = 1000
            """, frozenMetres: 1_024);

        Assert.True(result.Ok, result.Describe());
    }

    [Fact]
    public void The_ordinary_entry_point_runs_no_world_creation_check()
    {
        // A world that does not exist yet has no frozen constants to disagree with, which is the
        // whole reason there are two entry points rather than one with a nullable argument the
        // callers keep passing null to.
        Ruleset ruleset = Accepted($"""
            {Nothing}

            [layers]
            kernel_metres = 4096
            """);

        Assert.Equal(4_096, ruleset.Layers.Constants.IndustrialPollutionMetres);
    }

    /// <summary>
    /// The core's backstop, for a caller that never asked the loader.
    /// </summary>
    /// <remarks>
    /// <c>adr/0002</c> keeps the sentence a designer reads in <c>Formats</c>, so the loader is where
    /// the refusal is <em>explained</em>. It is not where it is <em>guaranteed</em>: a shell that
    /// builds a Ruleset in code reaches the swap without passing the loader at all, and the failure
    /// this prevents is a field that is silently at the wrong scale rather than a crash.
    /// </remarks>
    [Fact]
    public void The_core_refuses_a_swapped_kernel_even_with_no_loader_in_the_way()
    {
        var world = new World(1_000, Ruleset.Empty);

        var exception = Assert.Throws<InvalidOperationException>(
            () => world.Adopt(
                Ruleset.Empty.WithLayers(
                    new LayerRuleset(
                        LayerSchedule.Default,
                        LayerRates.Default,
                        new LayerConstants(IndustrialPollutionMetres: 4_096))),
                0x1111_1111_1111_1111UL,
                Ticks.Zero,
                WorldKey.FromSeed(0x8000_0001UL)));

        Assert.Contains("adr/0015", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_core_accepts_a_swapped_cadence()
    {
        var world = new World(1_000, Ruleset.Empty);

        world.Adopt(
            Ruleset.Empty.WithLayers(new LayerRuleset(
                new LayerSchedule(new LayerCadence(16, 1), new LayerCadence(64, 5)),
                LayerRates.Default)),
            0x1111_1111_1111_1111UL,
            Ticks.Zero,
            WorldKey.FromSeed(0x8000_0001UL));

        Assert.Equal(new LayerCadence(16, 1), world.Layers.Schedule.IndustrialPollution);
    }
}
