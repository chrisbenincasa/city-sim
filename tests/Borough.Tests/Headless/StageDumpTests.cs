using System.Text.RegularExpressions;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// <c>plans/0046</c> stages 1 to 3: <c>--stages</c>, the age structure Day by Day.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is
/// <see cref="The_replacement_rate_is_printed_against_its_threshold"/></b>, because it is the one
/// claim that says the SOURCE is running rather than merely wired. ⚠ <b>Its predecessor asserted the
/// city reached zero</b>, which was correct while stage 2 was a sink with nothing feeding it — the
/// second time these assertions have been rewritten rather than extended, and the cost of pinning a
/// half-built mechanism.
/// </para>
/// <para>
/// ⚠ <b>Every measurement is on <c>rulesets/aged.toml</c> and it has to be.</b> It is the only
/// shipped file declaring <c>[[life_stage]]</c>, and the mode refuses every other file rather than
/// printing a histogram of whatever <c>SyntheticCity</c> handed out at creation.
/// </para>
/// <para>
/// ⚠ <b>400 Days is chosen and not rounded.</b> A Household seeded into <c>young</c> lives at most
/// <c>31 + 63 + 63 + 55 = 212</c> Days, so past 240 every Household standing was born here — and 400
/// leaves room for a third generation, which is what the cohort question needs. It is what makes this
/// the most expensive dump test in the tier, which is why the report is built once and shared.
/// </para>
/// </remarks>
public sealed class StageDumpTests
{
    /// <summary>400 Days — past the longest life the table can draw, with room for three generations.</summary>
    private const string Ticks = "819200";

    private const string Population = "2000";

    /// <summary>Both panels are reached, which is what says the wiring runs at all.</summary>
    [Fact]
    public void The_picture_has_both_panels()
    {
        string report = Dump();

        Assert.Contains("Does the founding cohort blur?", report, StringComparison.Ordinal);
        Assert.Contains("How does the city empty?", report, StringComparison.Ordinal);
    }

    /// <summary>Every stage the Ruleset declares gets a column, named from the file.</summary>
    /// <remarks>
    /// <b>Named rather than numbered, which is the shell's job and not the core's.</b>
    /// <c>Borough.Core</c> returns ids; <c>RulesetNames</c> is what turns stage 3 into
    /// <c>mature_fa</c>. A dump printing numbers would be one where the resolution never happened.
    /// </remarks>
    [Fact]
    public void Every_declared_stage_is_named_in_the_header()
    {
        string report = Dump();

        foreach (string stage in (string[])["young", "family", "mature_f", "childles", "empty_ne"])
        {
            Assert.Contains(stage, report, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 🔴 <b>The city outlives the generation it was seeded with.</b>
    /// </summary>
    /// <remarks>
    /// <b>400 Days is past the longest life the table can draw</b>, so every Household standing at
    /// the end was born here rather than seeded. ⚠ <b>This is the THIRD claim this test has made
    /// about the same column</b> — flat at stage 1, zero at stage 2, standing at stage 3 — and each
    /// was correct for exactly as long as the mechanism was half-built. ***A test written against a
    /// half-built mechanism is a claim about the stage and not about the city.***
    /// </remarks>
    [Fact]
    public void The_city_outlives_its_founding_generation()
    {
        string report = Dump();

        // ⚠ THE OPPOSITE ASSERTION TO THE ONE THIS TEST SHIPPED WITH. At stage 2 it read
        // `the city emptied on Day`; stage 3 gave the sink a source, and 400 Days is past the
        // longest life the table can draw, so everybody standing at the end was born here.
        Assert.Contains("had not emptied when the run ended", report, StringComparison.Ordinal);
        Assert.DoesNotContain("the city emptied on Day", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔴 <b>Replacement Rate is printed, and it is a reading rather than a restatement.</b>
    /// </summary>
    /// <remarks>
    /// <b>The 2.00 is arithmetic and the rate beside it is the city</b> — <c>adr/0011</c>: two
    /// children replace two adults, and ***"that threshold falls out of conservation rather than
    /// being chosen"***. ⚠ <b>The rate itself is not asserted to a value</b>: it is a figure for a
    /// document to quote, and pinning it would make an instrument out of an assertion. What is
    /// asserted is that births and spawns are counted APART — a birth creates a Citizen and a spawn
    /// moves one, and a readout summing them would report a population growing twice as fast as it
    /// is.
    /// </remarks>
    [Fact]
    public void The_replacement_rate_is_printed_against_its_threshold()
    {
        string report = Dump();

        Assert.Contains("Does the city replace itself?", report, StringComparison.Ordinal);
        Assert.Contains("against 2.00 for exact", report, StringComparison.Ordinal);
        Assert.Matches(@"births\s+[1-9]", report);
        Assert.Matches(@"children left home\s+[1-9]", report);
    }

    /// <summary>The cohort blurs rather than moving in lockstep.</summary>
    /// <remarks>
    /// <b><c>adr/0011</c>'s <c>spread_days</c>, asserted as a shape rather than a ratio.</b> A city
    /// whose founding generation never blurs puts every Household through one transition on one Day,
    /// so the series would be a train of spikes and most Days would be empty. ⚠ <b>The ratio itself
    /// is deliberately not asserted</b> — it is a figure for a document to quote, and pinning it here
    /// would make an instrument out of an assertion.
    /// </remarks>
    [Fact]
    public void The_founding_cohort_does_not_move_in_lockstep()
    {
        string report = Dump();

        Assert.Contains("busiest ÷ mean", report, StringComparison.Ordinal);
        Assert.DoesNotContain("fewer than one transition a Day", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Ruleset with no stage table is refused rather than printing an initialiser's histogram.
    /// </summary>
    /// <remarks>
    /// <b><c>--arrivals</c>' polarity, and the failure it is written against is the same one:</b> a
    /// mechanism that is correct and unobservable in every world that exists, for want of Ruleset
    /// <em>content</em> rather than code. ⚠ Every shipped file but one takes this branch.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_stage_table_is_refused()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"), ticks: "64", citizens: "200");

        Assert.Equal(2, code);
        Assert.Contains("Demographics are content.", report, StringComparison.Ordinal);
    }

    /// <summary>Asking for two pictures at once is refused, as every other mode refuses it.</summary>
    [Fact]
    public void Two_pictures_at_once_are_refused()
    {
        Assert.False(
            Options.TryParse(
                ["--stages", "--land-value", "--ruleset", Ruleset("aged.toml")],
                out Options? _,
                out string? complaint));

        Assert.Contains("each picture builds its own world", complaint!, StringComparison.Ordinal);
    }

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    /// <summary>
    /// The one session every panel test reads, run once.
    /// </summary>
    /// <remarks>
    /// <b>Cached on <c>ArrivalDumpTests</c>' finding, which is that a fixture six tests agree about
    /// is one fixture.</b> This run is 491,520 Ticks; building it per test would put the class alone
    /// near <c>TierBudgetTests</c>' four-minute assertion ceiling, and what grows is the tier rather
    /// than any single test.
    /// </remarks>
    private static readonly Lazy<string> Report = new(() =>
    {
        (int code, string report) = Run(Ruleset("aged.toml"));

        Assert.Equal(0, code);

        return report;
    });

    private static string Dump() => Report.Value;

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--stages", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = StageDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
