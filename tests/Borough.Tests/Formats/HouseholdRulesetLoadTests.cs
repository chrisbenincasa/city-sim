using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 5c task 5: the <c>[households]</c> table and every refusal it states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>JobRulesetLoadTests</c>' discipline and for <c>adr/0064</c>'s reason: the one guard in this
/// loader that shipped without a test was later re-derived as <em>absent</em> from nothing but the
/// shape of its absence in the suite.
/// </para>
/// <para>
/// <b>The refusal this table does <em>not</em> state is the one worth reading.</b> <c>[jobs]</c> is
/// refused without a <c>[trips] commute_budget_minutes</c> because the assignment pass would have no
/// bound at all. <c>[households]</c> is accepted without a <c>[trips]</c>: a car in a city with no
/// Trip model is inert, not unbounded, and adding the refusal to look symmetrical would be inventing
/// a dependency. There is a test that pins that asymmetry, so nobody restores the symmetry later
/// under the impression it was an oversight.
/// </para>
/// </remarks>
public sealed class HouseholdRulesetLoadTests
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

    private static string With(string body) => $"{Nothing}\n\n[households]\n{body}";

    // ---- the absent table -----------------------------------------------------------------------

    /// <summary>
    /// <b>The absence means nobody keeps a car, and the file has said so by omitting the table.</b>
    /// </summary>
    /// <remarks>
    /// <c>[jobs]</c>' polarity for <c>[jobs]</c>' reason, and it is what keeps zero from being a
    /// placeholder. Zero sits inside the range of legitimate answers — a city before the motor car is
    /// a real city — so a defaulted <c>car_ownership_percent = 0</c> would be indistinguishable from a
    /// decision, which is session F's rule about placeholders. Reached through the absence of the
    /// table instead, where there is nothing to mistake for a chosen number.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_households_table_is_a_complete_ruleset_where_nobody_drives()
    {
        Ruleset rules = Accepted(Nothing);

        Assert.Equal(HouseholdRuleset.None, rules.Households);
        Assert.False(rules.Households.Runs);
        Assert.Equal(0, rules.Households.CarOwnershipPercent);
    }

    /// <summary>
    /// <b>A stated table must state its rate.</b>
    /// </summary>
    [Fact]
    public void A_households_table_with_no_rate_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[households]\n");

        Assert.Contains("car_ownership_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>A <c>[households]</c> table is accepted in a Ruleset with no <c>[trips]</c>, and that is
    /// the decision rather than a gap.</b>
    /// </summary>
    /// <remarks>
    /// The symmetry with <c>[jobs]</c> is tempting and wrong. <c>[jobs]</c> needs the Commute Budget
    /// because the search box is <em>derived</em> from it, so without one the pass draws from the
    /// whole city — S2 R4's uniform origin-destination draw, which R4 measured is a different city.
    /// Nothing derives anything from a car. With no Trip model, nobody asks what mode anybody travels
    /// in and the rate does nothing at all. <b>This test exists so the asymmetry cannot be repaired by
    /// somebody who reads it as an oversight</b>, which is what a refusal with no test invites.
    /// </remarks>
    [Fact]
    public void A_households_table_needs_no_trips_table_above_it()
    {
        Ruleset rules = Accepted(With("car_ownership_percent = 60"));

        Assert.Equal(60, rules.Households.CarOwnershipPercent);
        Assert.True(rules.Households.Runs);
        Assert.False(rules.Trips.Runs);
    }

    // ---- the range ------------------------------------------------------------------------------

    /// <summary>
    /// <b>Both ends of the range load, and they are the two cities the key is capable of.</b>
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Both_ends_of_the_range_are_legitimate_cities(int percent)
    {
        Ruleset rules = Accepted(With($"car_ownership_percent = {percent}"));

        Assert.Equal(percent, rules.Households.CarOwnershipPercent);
        Assert.Equal(percent > 0, rules.Households.Runs);
    }

    /// <summary>
    /// <b>A share outside 0–100 is not a bigger city, it is a quantity that is not a share.</b>
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(1_000)]
    public void A_rate_outside_the_range_is_refused(int percent)
    {
        RulesetRefusal refusal = Refused(With($"car_ownership_percent = {percent}"));

        Assert.Contains("car_ownership_percent", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("0..100", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the table itself -----------------------------------------------------------------------

    /// <summary>
    /// <b>Two tables of numbers for one population is ambiguous rather than additive.</b>
    /// </summary>
    [Fact]
    public void A_second_households_table_is_refused()
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n[households]\ncar_ownership_percent = 10\n\n"
            + "[households]\ncar_ownership_percent = 90\n");

        Assert.Contains("[households]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The section list a bad section name is measured against names this one.</b>
    /// </summary>
    /// <remarks>
    /// A list of valid sections is a fact stored in prose inside an error message, so it drifts the
    /// moment a section is added and nothing fails. That is <c>plans/0012</c> <b>Cause 1</b> at its
    /// smallest, and it is only visible to somebody who has already typed the wrong thing.
    /// </remarks>
    [Fact]
    public void The_section_list_in_a_refusal_names_households()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[hoseholds]\ncar_ownership_percent = 10\n");

        Assert.Contains("[households]", refusal.Reason, StringComparison.Ordinal);
    }
}
