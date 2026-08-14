using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 5b-bis task 3: <c>[trips]</c>, the two conversions inside it, and every refusal it
/// states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>RoadRulesetLoadTests</c>' discipline and for <c>adr/0064</c>'s reason: the one guard in this
/// loader that shipped without a test was later re-derived as <em>absent</em> from nothing but the
/// shape of its absence in the suite.
/// </para>
/// <para>
/// <b>This table is the first with a key that is optional inside a table that is present</b>, and
/// most of what is asserted here is that the three states are distinguishable: no table, a table
/// with no Budget, and a table with one. They mean three different cities and nothing but this file
/// says so.
/// </para>
/// </remarks>
public sealed class TripRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule or a road to exist.</summary>
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

    /// <summary><see cref="Nothing"/> with the given <c>[trips]</c> body appended.</summary>
    private static string With(string body) => $"{Nothing}\n\n[trips]\n{body}";

    // ---- the absent table -----------------------------------------------------------------------

    /// <summary>
    /// <b>The absence means the city does not travel, and it is stated by the type rather than
    /// inferred.</b>
    /// </summary>
    /// <remarks>
    /// <c>[roads]</c>' polarity rather than <c>[layers]</c>'. The assertion that matters is the
    /// second: <see cref="TripRuleset.None"/> is <em>not</em> <c>default</c>, because a zeroed
    /// crossing cost is a legitimate authored value — <c>adr/0074</c>'s rung 1 — so absence needs a
    /// value outside the range rather than the bottom of it.
    /// </remarks>
    [Fact]
    public void A_Ruleset_with_no_trips_table_is_a_complete_Ruleset_that_does_not_travel()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.Equal(TripRuleset.None, ruleset.Trips);
        Assert.False(ruleset.Trips.Runs);
        Assert.NotEqual(default, ruleset.Trips);
        Assert.NotEqual(TravelTime.Zero, ruleset.Trips.CrossingCost);
    }

    // ---- the well-formed table ------------------------------------------------------------------

    /// <summary>
    /// <b>A crossing authored in seconds reaches the Ruleset as Q16.16 Ticks.</b>
    /// </summary>
    /// <remarks>
    /// The exchange rate is <see cref="Speed.FromKilometresPerHour"/>'s own — a Day is 86,400 s over
    /// 8,192 Ticks — so 30 s is 30 ÷ 10.546875 Ticks. Asserted against the conversion rather than
    /// against a raw literal, because a literal here would be a second copy of the derivation and the
    /// two would drift.
    /// </remarks>
    [Fact]
    public void A_crossing_authored_in_seconds_arrives_as_a_travel_time()
    {
        TripRuleset trips = Accepted(With("crossing_seconds = 30")).Trips;

        Assert.True(trips.Runs);
        Assert.Equal(TravelTime.FromSeconds(30), trips.CrossingCost);
    }

    /// <summary>
    /// <b>A crossing cost of zero is a city, not an absence</b> — and it is the one this corpus had
    /// by omission until <c>adr/0074</c> named the term.
    /// </summary>
    /// <remarks>
    /// This is the assertion that forces the sentinel. If zero meant <em>unset</em> there would be no
    /// way to author rung 1, and — worse — an unauthored crossing would be indistinguishable from a
    /// deliberate one, which is session F's <i>a placeholder inside the range of legitimate answers
    /// cannot announce itself</i>.
    /// </remarks>
    [Fact]
    public void A_crossing_cost_of_zero_is_authorable_and_is_not_an_absent_table()
    {
        TripRuleset trips = Accepted(With("crossing_seconds = 0")).Trips;

        Assert.True(trips.Runs);
        Assert.Equal(TravelTime.Zero, trips.CrossingCost);
        Assert.NotEqual(TripRuleset.None, trips);
    }

    /// <summary><b>Three rungs authored in minutes arrive as three travel times.</b></summary>
    /// <remarks>
    /// <c>adr/0095</c>. The assertion that matters is the third: <c>commute_budget_minutes</c> is the
    /// <b>ceiling</b> rather than the acceptable commute, so the key whose name did not change is the
    /// one whose meaning did.
    /// </remarks>
    [Fact]
    public void A_budget_authored_in_minutes_arrives_as_a_travel_time()
    {
        TripRuleset trips = Accepted(With("""
            crossing_seconds = 30
            commute_fast_minutes = 15
            commute_moderate_minutes = 30
            commute_budget_minutes = 45
            """)).Trips;

        Assert.True(trips.HasCommuteBudget);
        Assert.Equal(TravelTime.FromMinutes(15), trips.Fast);
        Assert.Equal(TravelTime.FromMinutes(30), trips.Moderate);
        Assert.Equal(TravelTime.FromMinutes(45), trips.CommuteBudget);
    }

    /// <summary>
    /// <b>A cost lands on the rung its minutes put it on, and each edge is inclusive.</b>
    /// </summary>
    /// <remarks>
    /// The boundaries are asserted from both sides because <c>adr/0095</c>'s rungs are stated as
    /// <em>to 20, to 40, to 50</em> — an edge belongs to the band below it, so a commute of exactly
    /// twenty minutes is fast and a commute of exactly fifty is unsavoury rather than refused.
    /// </remarks>
    [Fact]
    public void A_cost_lands_on_the_rung_its_minutes_put_it_on()
    {
        TripRuleset trips = Accepted(With("""
            crossing_seconds = 30
            commute_fast_minutes = 20
            commute_moderate_minutes = 40
            commute_budget_minutes = 50
            """)).Trips;

        Assert.True(trips.TryRung(TravelTime.Zero, out CommuteRung zero));
        Assert.Equal(CommuteRung.Fast, zero);

        Assert.True(trips.TryRung(TravelTime.FromMinutes(20), out CommuteRung onEdge));
        Assert.Equal(CommuteRung.Fast, onEdge);

        Assert.True(trips.TryRung(TravelTime.FromMinutes(21), out CommuteRung past));
        Assert.Equal(CommuteRung.Moderate, past);

        Assert.True(trips.TryRung(TravelTime.FromMinutes(40), out CommuteRung moderateEdge));
        Assert.Equal(CommuteRung.Moderate, moderateEdge);

        Assert.True(trips.TryRung(TravelTime.FromMinutes(41), out CommuteRung unsavoury));
        Assert.Equal(CommuteRung.Unsavoury, unsavoury);

        Assert.True(trips.TryRung(TravelTime.FromMinutes(50), out CommuteRung ceiling));
        Assert.Equal(CommuteRung.Unsavoury, ceiling);

        Assert.False(trips.TryRung(TravelTime.FromMinutes(51), out _));
        Assert.False(trips.TryRung(TravelTime.Impassable, out _));
    }

    /// <summary>
    /// <b>A Ruleset stating some of the three rungs and not all of them is refused.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0095</c> makes the Budget one decision in three keys, so the optionality belongs to the
    /// <em>group</em>. <b>This guard exists because <c>adr/0064</c>'s lesson is that a loader guard
    /// with no test is invisible to the reader deciding it does not exist</b> — that ADR recorded a
    /// live defect on the strength of a refusal that had shipped, untested, a slice earlier.
    /// </remarks>
    [Theory]
    [InlineData("commute_fast_minutes = 20")]
    [InlineData("commute_moderate_minutes = 40")]
    [InlineData("commute_budget_minutes = 50")]
    [InlineData("commute_fast_minutes = 20\ncommute_moderate_minutes = 40")]
    [InlineData("commute_fast_minutes = 20\ncommute_budget_minutes = 50")]
    [InlineData("commute_moderate_minutes = 40\ncommute_budget_minutes = 50")]
    public void A_partial_set_of_rungs_is_refused(string partial)
    {
        RulesetRefusal refusal = Refused(With($"crossing_seconds = 30\n{partial}"));

        Assert.Contains("three rungs", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Rungs that are not strictly increasing are refused, including two that are equal.</b>
    /// </summary>
    /// <remarks>
    /// Equality is refused rather than tolerated because two equal rungs are a band no commute can
    /// fall in — and a Census counter pinned at zero because its band is empty reads exactly like a
    /// mechanism that has stopped, which is the ambiguity <c>[[building]] jobs</c> put full
    /// employment out of reach to avoid.
    /// </remarks>
    [Theory]
    [InlineData(40, 20, 50)]
    [InlineData(20, 50, 40)]
    [InlineData(20, 20, 50)]
    [InlineData(20, 40, 40)]
    [InlineData(50, 40, 20)]
    public void Rungs_that_do_not_strictly_increase_are_refused(int fast, int moderate, int ceiling)
    {
        RulesetRefusal refusal = Refused(With($"""
            crossing_seconds = 30
            commute_fast_minutes = {fast}
            commute_moderate_minutes = {moderate}
            commute_budget_minutes = {ceiling}
            """));

        Assert.Contains("strictly increasing", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the omitted Budget ---------------------------------------------------------------------

    /// <summary>
    /// <b>An omitted Budget is a city with no ceiling, and it is the state this milestone measures
    /// from.</b>
    /// </summary>
    /// <remarks>
    /// The Budget is a percentile of a Trip-cost distribution, so it cannot be authored before one
    /// exists (<c>plans/0002</c> §D, <c>adr/0052</c>). The key is therefore optional inside a table
    /// whose other key is required — the only such key in this loader — and the omission states a
    /// city rather than defaulting one: nothing is refused for its length, so the distribution the
    /// number will be read off is uncensored.
    /// </remarks>
    [Fact]
    public void A_trips_table_with_no_budget_is_a_city_with_no_ceiling()
    {
        TripRuleset trips = Accepted(With("crossing_seconds = 30")).Trips;

        Assert.True(trips.Runs);
        Assert.False(trips.HasCommuteBudget);
        Assert.True(trips.WithinBudget(TravelTime.FromMinutes(5_000)));
    }

    /// <summary>
    /// <b>An impassable cost is outside every Budget, including the Budget that does not exist.</b>
    /// </summary>
    /// <remarks>
    /// It is not <em>over budget</em> — it is no route, a different Fate with a different diagnosis,
    /// and the caller tests for it first. <c>false</c> here is the backstop: a caller that forgot must
    /// not send somebody down a route that does not exist.
    /// </remarks>
    [Fact]
    public void An_impassable_cost_is_within_no_budget_at_all()
    {
        TripRuleset unbounded = Accepted(With("crossing_seconds = 30")).Trips;

        Assert.False(unbounded.WithinBudget(TravelTime.Impassable));
    }

    // ---- the refusals ---------------------------------------------------------------------------

    /// <summary>
    /// <b>The crossing cost is required once the table exists</b>, because the absence of a crossing
    /// cost is what the absence of the table already says.
    /// </summary>
    [Fact]
    public void A_trips_table_with_no_crossing_cost_is_refused()
    {
        RulesetRefusal refusal = Refused(With(
            "commute_fast_minutes = 15\ncommute_moderate_minutes = 30\n"
            + "commute_budget_minutes = 45"));

        Assert.Contains("crossing_seconds", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_crossing_cost_is_refused()
    {
        RulesetRefusal refusal = Refused(With("crossing_seconds = -1"));

        Assert.Contains("out of range", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A crossing longer than an in-world hour is refused, and the refusal names the mechanism
    /// that belongs instead.</b>
    /// </summary>
    /// <remarks>
    /// Rung 2's whole approximation is that a Street may be crossed wherever you like at a constant
    /// cost. A road somebody waits an hour to cross is not that road, and this design already models
    /// the uncrossable road: it is an <b>Arterial</b>, whose Arcs carry no foot bit between Junction
    /// pieces. That is <c>adr/0074</c>'s own revisit trigger read forwards.
    /// </remarks>
    [Fact]
    public void A_crossing_longer_than_an_hour_is_refused_and_the_refusal_names_the_arterial()
    {
        RulesetRefusal refusal = Refused(With("crossing_seconds = 3601"));

        Assert.Contains("Arterial", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Budget of zero minutes is refused rather than read as <i>nobody travels</i></b>, and the
    /// refusal names what to do instead.
    /// </summary>
    /// <remarks>
    /// A ceiling of no minutes fails every Trip in the city, which nobody authored on purpose. The
    /// intent it would be reaching for — <i>length refuses nothing</i> — is what deleting the key
    /// already states, so the message says so rather than leaving the author to guess.
    /// </remarks>
    [Fact]
    public void A_budget_of_zero_is_refused_and_the_refusal_names_the_omission_instead()
    {
        RulesetRefusal refusal = Refused(With("""
            crossing_seconds = 30
            commute_budget_minutes = 0
            """));

        Assert.Contains("delete the key", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary><b>A Budget longer than a travel time can hold is refused.</b></summary>
    [Fact]
    public void A_budget_beyond_the_format_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            crossing_seconds = 30
            commute_budget_minutes = 5760
            """));

        Assert.Contains("out of range", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A second <c>[trips]</c> is refused</b>, on every other singular table's reasoning: a city
    /// has one Trip model, so two tables of numbers for it is ambiguous rather than additive.
    /// </summary>
    /// <remarks>
    /// Written as an array of tables, exactly as <c>A_second_roads_table_is_refused_rather_than_
    /// merged</c> is, because a repeated <c>[trips]</c> header is TOML's own error and never reaches
    /// this loader's guard — the file is refused by the parser with a message about a duplicate key.
    /// The guard exists for the form the parser accepts.
    /// </remarks>
    [Fact]
    public void A_second_trips_table_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[trips]]
            crossing_seconds = 30

            [[trips]]
            crossing_seconds = 60
            """);

        Assert.Contains("a second [trips]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The section catalogue names <c>[trips]</c></b>, which is what a reader of a refusal
    /// consults to find out what a Ruleset may contain.
    /// </summary>
    [Fact]
    public void The_unknown_section_refusal_lists_trips_among_the_sections()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[journeys]\ncrossing_seconds = 30");

        Assert.Contains("[trips]", refusal.Reason, StringComparison.Ordinal);
    }
}
