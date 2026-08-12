using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 5b-bis task 4: the <c>[jobs]</c> table and every refusal it states, of which one is a
/// refusal about a <em>different</em> table.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>RoadRulesetLoadTests</c>' discipline and for <c>adr/0064</c>'s reason: the one guard in this
/// loader that shipped without a test was later re-derived as <em>absent</em> from nothing but the
/// shape of its absence in the suite.
/// </para>
/// <para>
/// <b>The cross-table refusal is the one worth reading.</b> <c>[jobs]</c> in a Ruleset with no
/// <c>[trips] commute_budget_minutes</c> is rejected, because the assignment pass has no search
/// radius of its own — the box it draws candidates from is what a walk within the Budget covers. That
/// is a stronger form of <c>ReadLots(roads)</c>'s precedent: there the second table supplies a
/// ceiling, here it supplies the first table's entire geometry.
/// </para>
/// </remarks>
public sealed class JobRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule or a road to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    /// <summary>A <c>[trips]</c> table with a Commute Budget, which <c>[jobs]</c> requires.</summary>
    private const string Trips = """
        [trips]
        crossing_seconds = 30
        commute_budget_minutes = 20
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

    /// <summary><see cref="Nothing"/> and <see cref="Trips"/> with a <c>[jobs]</c> body appended.</summary>
    private static string With(string body) => $"{Nothing}\n\n{Trips}\n\n[jobs]\n{body}";

    /// <summary>A well-formed <c>[jobs]</c> body.</summary>
    private const string Whole = """
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3
        """;

    // ---- the absent table -----------------------------------------------------------------------

    /// <summary>
    /// <b>The absence means nobody is ever assigned work, and it is loud rather than quiet.</b>
    /// </summary>
    /// <remarks>
    /// <c>[placement]</c>'s polarity for <c>[placement]</c>'s reason. A default would put three
    /// hash-bearing numbers in the binary that nobody authored (<c>adr/0052</c>), and the failure it
    /// hides is a city employing people at a cadence its designer never wrote. A city that employs
    /// nobody says so in the Census, in four counters that all read zero.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_jobs_table_is_a_complete_ruleset_that_employs_nobody()
    {
        Ruleset ruleset = Accepted($"{Nothing}\n\n{Trips}");

        Assert.Equal(JobRuleset.None, ruleset.Jobs);
        Assert.False(ruleset.Jobs.Runs);
    }

    // ---- the well-formed table ------------------------------------------------------------------

    [Fact]
    public void A_jobs_table_reaches_the_ruleset_whole()
    {
        JobRuleset jobs = Accepted(With(Whole)).Jobs;

        Assert.True(jobs.Runs);
        Assert.Equal(32u, jobs.Interval);
        Assert.Equal(1_024, jobs.RevisitTicks);
        Assert.Equal(3, jobs.Candidates);
    }

    /// <summary>
    /// <b>The sample is derived from the duration and rounds up</b> (<c>adr/0059</c>, a third time).
    /// </summary>
    /// <remarks>
    /// The ceiling is load-bearing rather than tidy, on <see cref="PlacementRuleset.SampleFor"/>'s
    /// argument: flooring returns <b>zero</b> for any population below
    /// <c>revisit_ticks ÷ interval</c>, which is every fixture in this suite and every city a player
    /// opens on, so the pass would appear not to exist on small worlds.
    /// </remarks>
    [Theory]
    [InlineData(10_000, 313)]
    [InlineData(1_000, 32)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    public void The_sample_is_derived_from_the_revisit_period(int citizens, int expected)
    {
        JobRuleset jobs = Accepted(With(Whole)).Jobs;

        Assert.Equal(expected, jobs.SampleFor(citizens));
    }

    // ---- the cross-table refusal ----------------------------------------------------------------

    /// <summary>
    /// <b>A <c>[jobs]</c> table with no Commute Budget above it is refused, and the refusal says why
    /// rather than naming a missing key.</b>
    /// </summary>
    /// <remarks>
    /// The pass draws candidates from a box around home whose size is <em>what a walk within the
    /// Budget covers</em>. With no Budget the box is unbounded, and an unbounded draw is S2 R4's
    /// uniform origin-destination distribution — which R4 measured to be a different city rather than
    /// a noisier one. So the alternative to refusing is inventing a radius, which is exactly the
    /// hash-bearing number with no ratifier <c>adr/0052</c> forbids.
    /// </remarks>
    [Fact]
    public void A_jobs_table_without_a_commute_budget_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [trips]
            crossing_seconds = 30

            [jobs]
            {Whole}
            """);

        Assert.Contains("commute_budget_minutes", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("search radius", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary><b>No <c>[trips]</c> table at all is refused the same way.</b></summary>
    /// <remarks>
    /// The same absence reached from further out, and it is asserted separately because the two are
    /// different files to the person writing one: a Ruleset with no Trip model and a Ruleset with a
    /// Trip model and no ceiling both fail here, and a guard written against only the second would
    /// pass the first through to a null Budget.
    /// </remarks>
    [Fact]
    public void A_jobs_table_with_no_trips_table_at_all_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[jobs]\n{Whole}");

        Assert.Contains("commute_budget_minutes", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the ordinary refusals ------------------------------------------------------------------

    [Fact]
    public void A_jobs_table_with_no_interval_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            revisit_ticks = 1024
            candidates    = 3
            """));

        Assert.Contains("interval", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_jobs_table_with_no_revisit_period_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            interval   = 32
            candidates = 3
            """));

        Assert.Contains("revisit_ticks", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The revisit period is required rather than defaulting to a Day, unlike a Zone Rule's.</b>
    /// </summary>
    /// <remarks>
    /// <c>ReadZoneRevisit</c> may derive a Day because a Day is the scale a <c>rate</c> is denominated
    /// in and every one in a shipped Ruleset is 8–32 Ticks. Nothing here is denominated in anything:
    /// how often somebody out of work looks for some is a feel decision with no derivation behind it,
    /// so it is authored or the table is absent.
    /// </remarks>
    [Fact]
    public void A_revisit_period_shorter_than_the_interval_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            interval      = 64
            revisit_ticks = 32
            candidates    = 3
            """));

        Assert.Contains("shorter than the interval", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_candidate_count_of_zero_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            interval      = 32
            revisit_ticks = 1024
            candidates    = 0
            """));

        Assert.Contains("out of range", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("never finds work", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A second <c>[jobs]</c> is refused</b>, on every other singular table's reasoning: there is
    /// one assignment pass, so two cadences for it is ambiguous rather than additive.
    /// </summary>
    /// <remarks>
    /// Written as an array of tables, exactly as the <c>[roads]</c> and <c>[trips]</c> cases are,
    /// because a repeated <c>[jobs]</c> header is TOML's own error and never reaches this loader's
    /// guard. The guard exists for the form the parser accepts.
    /// </remarks>
    [Fact]
    public void A_second_jobs_table_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            {Trips}

            [[jobs]]
            {Whole}

            [[jobs]]
            {Whole}
            """);

        Assert.Contains("a second [jobs]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The section catalogue names <c>[jobs]</c></b>, which is what a reader of a refusal consults
    /// to find out what a Ruleset may contain.
    /// </summary>
    [Fact]
    public void The_unknown_section_refusal_lists_jobs_among_the_sections()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[labour]\ninterval = 32");

        Assert.Contains("[jobs]", refusal.Reason, StringComparison.Ordinal);
    }
}
