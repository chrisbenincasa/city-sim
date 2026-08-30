using System.Text.RegularExpressions;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// <c>plans/0046</c> stages 1 and 2: <c>--stages</c>, the age structure Day by Day.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is <see cref="The_city_empties_and_the_dump_says_so"/></b>, because it is
/// the one claim that says the sink is running rather than merely wired. A histogram that moved
/// between stages while the population stayed flat is stage 1, and stage 1 shipped a commit earlier —
/// so the assertion that separates them is the population reaching zero.
/// </para>
/// <para>
/// ⚠ <b>Every measurement is on <c>rulesets/aged.toml</c> and it has to be.</b> It is the only
/// shipped file declaring <c>[[life_stage]]</c>, and the mode refuses every other file rather than
/// printing a histogram of whatever <c>SyntheticCity</c> handed out at creation.
/// </para>
/// <para>
/// ⚠ <b>240 Days is chosen and not rounded.</b> A Household seeded into <c>young</c> lives at most
/// <c>31 + 63 + 63 + 55 = 212</c> Days, so 240 is the shortest run in which the emptying is a
/// measured fact rather than a trend. It is also what makes this the most expensive dump test in the
/// tier, which is why the report is built once and shared.
/// </para>
/// </remarks>
public sealed class StageDumpTests
{
    /// <summary>240 Days — past the longest life the shipped stage table can draw.</summary>
    private const string Ticks = "491520";

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
    /// 🔴 <b>The city empties, which is the whole of stage 2.</b>
    /// </summary>
    /// <remarks>
    /// <b>Both halves, because either alone is a different mechanism.</b> Dissolutions with a
    /// standing population would be a sink too slow to matter; an empty city with no dissolutions
    /// counted would mean the rows went somewhere this dump cannot see. ⚠ <b>The stage-1 version of
    /// this dump asserted the population was FLAT</b>, and that assertion was correct for exactly one
    /// commit.
    /// </remarks>
    [Fact]
    public void The_city_empties_and_the_dump_says_so()
    {
        string report = Dump();

        Assert.Contains("the city emptied on Day", report, StringComparison.Ordinal);
        Assert.DoesNotContain("had not emptied when the run ended", report, StringComparison.Ordinal);
        // A pattern rather than a column: the padding is a formatting choice and pinning it would
        // make this fail on a cosmetic edit. The Day it empties on is deliberately NOT asserted --
        // that is a figure for a document to quote, and an assertion that re-derives one is an
        // instrument wearing the wrong tier.
        Assert.Matches(@"standing at the end\s+0\b", report);
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
