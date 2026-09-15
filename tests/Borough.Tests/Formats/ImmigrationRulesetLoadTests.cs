using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// <c>[immigration]</c> and <c>[[hinterland.population]]</c>: the counted Outside, and every way a
/// file can state one that would be read by nothing.
/// </summary>
public sealed class ImmigrationRulesetLoadTests
{
    /// <summary>
    /// A world with four Hinterlands, a choice model and five stages — everything a counted
    /// Outside needs before it can state one.
    /// </summary>
    private static string World(string populations = "", string immigration = Durations) =>
        $$"""
        [[resource]]
        name = "coin"
        family = "money"

        [[building]]
        name = "house"
        houses = true

        [placement]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        gives_up_after_days = 2
        mu_percent = 100
        centrality_tiles_per_unit = 2048
        rent_per_unit = 120
        moving_costs_rent = 720

        [[hinterland]]
        edge = "west"
        rent = 900
        centrality_tiles = 6000
        emigrant_balance_min = 800
        emigrant_balance_max = 4000
        {{populations}}

        [[hinterland]]
        edge = "south"
        rent = 600
        centrality_tiles = 4000
        emigrant_balance_min = 1200
        emigrant_balance_max = 9000

        [[hinterland]]
        edge = "east"
        rent = 300
        centrality_tiles = 2000
        emigrant_balance_min = 2000
        emigrant_balance_max = 14000

        [[hinterland]]
        edge = "north"
        rent = 1500
        centrality_tiles = 9000
        emigrant_balance_min = 3000
        emigrant_balance_max = 20000

        [[life_stage]]
        name = "young"
        duration_days = 24
        spread_days = 8
        next = "family"
        childless = "childless"
        children_min = 0
        children_max = 3
        adult_age_min_days = 1
        adult_age_max_days = 160
        rent_weight_percent = 100

        [[life_stage]]
        name = "family"
        duration_days = 48
        spread_days = 16
        next = "childless"
        school_level = 1
        rent_weight_percent = 150

        [[life_stage]]
        name = "childless"
        duration_days = 40
        spread_days = 16
        rent_weight_percent = 100
        {{immigration}}
        """;

    private const string Durations = """

        [immigration]
        reconsider_days = 2
        recovery_days = 32
        queue_wait_days = 2
        queue_reconsider_days = 1
        """;

    /// <summary>600 single people behind the west edge, in the lowest third of its purse range.</summary>
    private const string Singles = """

        [[hinterland.population]]
        stage = "young"
        adults_by_tier = [1, 0, 0]
        children = 0
        money_band = 0
        households = 600
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

    [Theory]
    [InlineData("[2147483647, 1, 0]", 0, 1)]
    [InlineData("[2147483647, 2147483647, 2]", 0, 1)]
    [InlineData("[1, 0, 0]", int.MaxValue, 1)]
    [InlineData("[2147483647, 1, 0]", 0, 0)]
    [InlineData("[1, 0, 0]", int.MaxValue, 0)]
    [InlineData("[2147483647, 0, 0]", 0, 2)]
    public void Population_counts_are_validated_before_narrow_sums(
        string adults, int children, int households)
    {
        RulesetRefusal refusal = Refused(World(Singles + $$"""

            [[hinterland.population]]
            stage = "family"
            adults_by_tier = {{adults}}
            children = {{children}}
            money_band = 0
            households = {{households}}
            """));

        Assert.Contains("more people", refusal.Reason, StringComparison.Ordinal);
        Assert.True(refusal.Line > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void A_representable_member_total_is_accepted(int households)
    {
        Ruleset rules = Accepted(World(Singles + $$"""

            [[hinterland.population]]
            stage = "family"
            adults_by_tier = [2147483646, 0, 0]
            children = 1
            money_band = 0
            households = {{households}}
            """));

        Assert.Equal(int.MaxValue, rules.HinterlandPopulations[1].Members);
        Assert.Equal((long)households * int.MaxValue, rules.HinterlandPopulations[1].People);
    }

    /// <summary>
    /// A Ruleset stating no <c>[immigration]</c> is the build this row started from.
    /// </summary>
    [Fact]
    public void A_Ruleset_with_no_immigration_table_takes_its_arrivals_from_a_caller()
    {
        Ruleset ruleset = Accepted(World(immigration: string.Empty));

        Assert.False(ruleset.Immigration.Stated);
        Assert.False(ruleset.Immigration.Recovers);
        Assert.Equal(0, ruleset.Immigration.ReconsiderTicks);
        Assert.Empty(ruleset.HinterlandPopulations);
    }

    /// <summary>The durations reach the Ruleset in Days and are read in Ticks.</summary>
    [Fact]
    public void The_four_durations_are_authored_in_Days_and_read_in_Ticks()
    {
        ImmigrationRuleset immigration = Accepted(World(Singles)).Immigration;

        Assert.True(immigration.Stated);
        Assert.True(immigration.Recovers);
        Assert.Equal(2 * Core.Quantities.Ticks.PerDay, immigration.ReconsiderTicks);
        Assert.Equal(32 * Core.Quantities.Ticks.PerDay, immigration.RecoveryTicks);
        Assert.Equal(2 * Core.Quantities.Ticks.PerDay, immigration.QueueWaitTicks);
        Assert.Equal(Core.Quantities.Ticks.PerDay, immigration.QueueReconsiderTicks);
    }

    /// <summary>
    /// A population attaches to the <c>[[hinterland]]</c> above it and to no other.
    /// </summary>
    [Fact]
    public void A_population_belongs_to_the_edge_it_was_written_under()
    {
        Ruleset ruleset = Accepted(World(Singles));

        Assert.Single(ruleset.HinterlandPopulations);
        Assert.True(ruleset.TryHinterland(Core.Space.MapEdge.West, out HinterlandDefinition west));
        Assert.Equal(1, west.PopulationCount);

        Assert.True(ruleset.TryHinterland(Core.Space.MapEdge.North, out HinterlandDefinition north));
        Assert.Equal(0, north.PopulationCount);

        HinterlandPopulationDefinition group =
            ruleset.HinterlandPopulations[west.PopulationFirst];

        Assert.Equal(600, group.Households);
        Assert.Equal(1, group.Adults);
        Assert.Equal(1, group.Members);
        Assert.Equal(600, group.People);
        Assert.True(group.Authored);
    }

    /// <summary>
    /// A population stated before any <c>[[hinterland]]</c> has nothing to attach to.
    /// </summary>
    [Fact]
    public void A_population_with_no_hinterland_above_it_is_refused()
    {
        RulesetRefusal refusal = Refused($$"""
            [[resource]]
            name = "coin"
            family = "money"
            {{Singles}}
            """);

        Assert.Contains("before any [[hinterland]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Stock without the durations that reach it would be counted and never drawn on.
    /// </summary>
    [Fact]
    public void A_population_without_the_immigration_table_is_refused()
    {
        RulesetRefusal refusal = Refused(World(Singles, immigration: string.Empty));

        Assert.Contains("[immigration] is not", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A world that opts into autonomous arrivals with nobody to arrive.
    /// </summary>
    [Fact]
    public void An_immigration_table_with_nobody_behind_any_edge_is_refused()
    {
        RulesetRefusal refusal = Refused(World());

        Assert.Contains("holds a single Household", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The Skill Tiers are a closed set, so the array is exactly three long.
    /// </summary>
    [Theory]
    [InlineData("[1, 0]")]
    [InlineData("[1, 0, 0, 0]")]
    [InlineData("[]")]
    public void An_adults_by_tier_that_is_not_three_long_is_refused(string authored)
    {
        RulesetRefusal refusal = Refused(World($$"""

            [[hinterland.population]]
            stage = "young"
            adults_by_tier = {{authored}}
            children = 0
            money_band = 0
            households = 600
            """));

        Assert.Contains("the Skill Tiers are 3", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A composition with nobody in it would be recovered towards a target and transfer nobody.
    /// </summary>
    [Fact]
    public void A_composition_holding_nobody_is_refused()
    {
        RulesetRefusal refusal = Refused(World("""

            [[hinterland.population]]
            stage = "young"
            adults_by_tier = [0, 0, 0]
            children = 0
            money_band = 0
            households = 600
            """));

        Assert.Contains("holds nobody", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// An Outside that was never a city: children behind an edge with nobody who bore them.
    /// </summary>
    /// <remarks>
    /// Child-only return stock remains supported even though authored openings require an adult.
    /// </remarks>
    [Fact]
    public void An_authored_composition_of_children_with_no_adult_is_refused()
    {
        RulesetRefusal refusal = Refused(World("""

            [[hinterland.population]]
            stage = "family"
            adults_by_tier = [0, 0, 0]
            children = 2
            money_band = 0
            households = 600
            """));

        Assert.Contains("no adult", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Children in a stage that states no school level would arrive with nothing to school them.
    /// </summary>
    [Fact]
    public void Children_in_a_stage_with_no_school_level_are_refused()
    {
        RulesetRefusal refusal = Refused(World("""

            [[hinterland.population]]
            stage = "young"
            adults_by_tier = [1, 1, 0]
            children = 2
            money_band = 0
            households = 600
            """));

        Assert.Contains("no school_level", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A band the purse range leaves empty holds stock nobody could ever be drawn from.
    /// </summary>
    [Fact]
    public void A_money_band_the_balance_range_leaves_empty_is_refused()
    {
        string narrow = World(Singles).Replace(
            "emigrant_balance_max = 4000", "emigrant_balance_max = 800", StringComparison.Ordinal)
            .Replace("money_band = 0", "money_band = 2", StringComparison.Ordinal);

        RulesetRefusal refusal = Refused(narrow);

        Assert.Contains("holds no amount", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A band outside the three the range is cut into.</summary>
    [Fact]
    public void A_money_band_past_the_last_one_is_refused()
    {
        RulesetRefusal refusal = Refused(
            World(Singles).Replace("money_band = 0", "money_band = 3", StringComparison.Ordinal));

        Assert.Contains("is out of range", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>One group written twice is not two groups.</summary>
    [Fact]
    public void The_same_composition_declared_twice_behind_one_edge_is_refused()
    {
        RulesetRefusal refusal = Refused(World(Singles + Singles));

        Assert.Contains("written twice", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Two edges may hold the same composition, because a composition is keyed by its edge too.
    /// </summary>
    [Fact]
    public void The_same_composition_behind_two_edges_is_two_groups()
    {
        string both = World(Singles).Replace(
            """
            emigrant_balance_max = 9000
            """,
            "emigrant_balance_max = 9000" + Singles,
            StringComparison.Ordinal);

        Assert.Equal(2, Accepted(both).HinterlandPopulations.Length);
    }

    /// <summary>A stage no file declares cannot be populated.</summary>
    [Fact]
    public void A_composition_naming_no_declared_stage_is_refused()
    {
        RulesetRefusal refusal = Refused(
            World(Singles).Replace("stage = \"young\"", "stage = \"retired\"",
                StringComparison.Ordinal));

        Assert.Contains("no [[life_stage]] declares", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A review at or beyond the wait it reviews never runs.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void A_queue_review_no_shorter_than_the_wait_is_refused(int review)
    {
        RulesetRefusal refusal = Refused(World(Singles).Replace(
            "queue_reconsider_days = 1", $"queue_reconsider_days = {review}",
            StringComparison.Ordinal));

        Assert.Contains("is not shorter than", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Zero recovery is a world and zero anything else is not.
    /// </summary>
    [Fact]
    public void Only_recovery_may_be_zero()
    {
        Ruleset frozen = Accepted(World(Singles).Replace(
            "recovery_days = 32", "recovery_days = 0", StringComparison.Ordinal));

        Assert.True(frozen.Immigration.Stated);
        Assert.False(frozen.Immigration.Recovers);

        Assert.Contains("at least 1", Refused(World(Singles).Replace(
                "reconsider_days = 2", "reconsider_days = 0", StringComparison.Ordinal)).Reason,
            StringComparison.Ordinal);
    }

    /// <summary>Two tables of durations for one circuit is ambiguous rather than additive.</summary>
    [Fact]
    public void A_second_immigration_table_is_refused()
    {
        // The array-of-table spelling, because TOML itself refuses a repeated [table] before the
        // loader sees it -- which is ParkingRulesetLoadTests' shape and the same reason.
        // BOTH in the array-of-table spelling, because TOML itself refuses a repeated [table] --
        // and refuses mixing the two forms -- before the loader ever sees the name. That is
        // ParkingRulesetLoadTests' shape and the same reason.
        RulesetRefusal refusal = Refused(World(Singles, immigration: """

            [[immigration]]
            reconsider_days = 2
            recovery_days = 32
            queue_wait_days = 2
            queue_reconsider_days = 1

            [[immigration]]
            reconsider_days = 4
            recovery_days = 16
            queue_wait_days = 2
            queue_reconsider_days = 1
            """));

        Assert.Contains("a second [immigration]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A counted Outside with nothing to weigh the city against.
    /// </summary>
    [Fact]
    public void A_stock_world_without_the_choice_model_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            World(Singles).Replace("mu_percent = 100", string.Empty, StringComparison.Ordinal)
                .Replace("centrality_tiles_per_unit = 2048", string.Empty, StringComparison.Ordinal)
                .Replace("rent_per_unit = 120", string.Empty, StringComparison.Ordinal)
                .Replace("moving_costs_rent = 720", string.Empty, StringComparison.Ordinal),
            "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");
        Assert.Contains("no mu_percent", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Admitted Households that can never give up would fill the Pool for the length of the run.
    /// </summary>
    [Fact]
    public void A_stock_world_whose_Pool_never_gives_up_is_refused()
    {
        RulesetRefusal refusal = Refused(World(Singles).Replace(
            "gives_up_after_days = 2", string.Empty, StringComparison.Ordinal));

        Assert.Contains("no gives_up_after_days", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A departure over an edge with no Hinterland behind it could not be credited anywhere.
    /// </summary>
    [Fact]
    public void A_stock_world_stating_fewer_than_four_hinterlands_is_refused()
    {
        string three = World(Singles).Replace("""
            [[hinterland]]
            edge = "north"
            rent = 1500
            centrality_tiles = 9000
            emigrant_balance_min = 3000
            emigrant_balance_max = 20000
            """, string.Empty, StringComparison.Ordinal);

        Assert.Contains("[[hinterland]] tables", Refused(three).Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A stock world is refused without the rent weight its arrivals are chosen by.
    /// </summary>
    [Fact]
    public void A_stock_world_leaving_a_stage_without_a_rent_weight_is_refused()
    {
        RulesetRefusal refusal = Refused(World(Singles).Replace(
            "rent_weight_percent = 150", string.Empty, StringComparison.Ordinal));

        Assert.Contains("rent_weight_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A world with no counted Outside keeps the neutral weight and states nothing.
    /// </summary>
    [Fact]
    public void A_world_with_no_counted_Outside_carries_the_neutral_rent_weight()
    {
        Ruleset ruleset = Accepted(World(immigration: string.Empty).Replace(
            "rent_weight_percent = 100", string.Empty, StringComparison.Ordinal)
            .Replace("rent_weight_percent = 150", string.Empty, StringComparison.Ordinal));

        for (byte stage = 1; stage <= ruleset.LifeStageCount; stage++)
        {
            Assert.Equal(Ruleset.RentNeutralPercent, ruleset.LifeStage(stage).RentWeightPercent);
        }
    }

    /// <summary>The weight is a percent of the neutral weight and stops at twice it.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(201)]
    public void A_rent_weight_outside_its_range_is_refused(int weight)
    {
        RulesetRefusal refusal = Refused(World(Singles).Replace(
            "rent_weight_percent = 150", $"rent_weight_percent = {weight}",
            StringComparison.Ordinal));

        Assert.Contains("rent_weight_percent is", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shipped demonstration states every key this file tests, which is what puts them in the
    /// generated schema and the key reference at all.
    /// </summary>
    [Fact]
    public void The_shipped_demonstration_states_a_counted_Outside()
    {
        Ruleset ruleset = Accepted(
            File.ReadAllText(Path.Combine(
                AppContext.BaseDirectory, "Rulesets", "attracted.toml")));

        Assert.True(ruleset.Immigration.Stated);
        Assert.Equal(12, ruleset.HinterlandPopulations.Length);

        long people = 0;

        foreach (HinterlandPopulationDefinition group in ruleset.HinterlandPopulations)
        {
            people += group.People;
        }

        Assert.Equal(4 * ((600 * 1) + (200 * 4) + (100 * 1)), people);
    }
}
