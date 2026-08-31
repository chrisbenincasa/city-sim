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
    /// 🔴 <b>Replacement Rate is printed, and BOTH SIDES of it are readings.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The threshold printed here was a flat <c>2.00</c> until 2026-08-31 and it was wrong by a
    /// factor of two.</b> <c>adr/0011</c> derives it as two children replacing two adults, which is
    /// airtight for a Household of two adults — and <c>World.SpawnChildren</c> gives every child its
    /// own Household, so a formed one holds exactly one adult. ***A city reported as declining at
    /// 1.45 against 2.00 grows 45% a generation.***
    /// </para>
    /// <para>
    /// ⚠ <b>Neither number is asserted to a value</b>: both are figures for a document to quote, and
    /// pinning either would make an instrument out of an assertion. What is asserted is that the
    /// threshold is <b>measured</b> — it must not read <c>2.00</c> again unless a pairing mechanism
    /// lands — and that births and spawns are counted APART, since a birth creates a Citizen and a
    /// spawn moves one, and a readout summing them would report a population growing twice as fast as
    /// it is.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_replacement_rate_is_printed_against_its_threshold()
    {
        string report = Dump();

        Assert.Contains("Does the city replace itself?", report, StringComparison.Ordinal);
        Assert.Matches(@"REPLACEMENT RATE\s+[\d.]+\s+against [\d.]+ for exact", report);
        Assert.Matches(@"births\s+[1-9]", report);
        Assert.Matches(@"children left home\s+[1-9]", report);
    }

    /// <summary>
    /// 🔴 <b>Every Household in this city holds exactly one adult, and the threshold follows it.</b>
    /// </summary>
    /// <remarks>
    /// <b>The census is what says so and not the arithmetic.</b> <c>working age</c> and the Household
    /// count come back exactly equal on any run past the founding generation, because
    /// <c>World.SpawnChildren</c> forms one Household per child and nothing pairs anybody. ⚠ <b>This
    /// test goes red the day a pairing mechanism lands</b>, and that failure is the design question
    /// queued in <c>plans/0045</c>'s <i>Owed when the freeze lifts</i> being answered — not a
    /// regression.
    /// </remarks>
    [Fact]
    public void A_formed_household_holds_one_adult_so_exact_replacement_is_one_child()
    {
        string report = Dump();

        double adults = Figure(report, @"adults per Household\s+([\d.]+)");
        double threshold = Figure(report, @"against ([\d.]+) for exact");

        Assert.Equal(1.00, adults, 2);
        Assert.Equal(adults, threshold, 2);
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
    /// 🔴 <b>The instrument's own arithmetic: the ratio is the product of the two factors it
    /// prints.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the one claim in the class that checks the READOUT rather than the city.</b>
    /// <c>posts per worker</c> is <c>posts per Citizen × Citizens per worker</c> by construction,
    /// and every one of the three is summed over a different denominator — so a decomposition that
    /// mixed a per-Day mean with a run total would print three plausible numbers that do not
    /// multiply. ⚠ <b>The tolerance is rounding and nothing else</b>: the panel prints two decimals,
    /// so the product of two rounded factors can miss the rounded ratio by about a part in a
    /// hundred, and anything wider is a summing bug rather than a display one.
    /// </remarks>
    [Fact]
    public void The_labour_panel_decomposes_its_own_ratio()
    {
        string report = Dump();

        Assert.Contains("Is there work for the people who can work?", report, StringComparison.Ordinal);

        double ratio = Figure(report, @"POSTS PER WORKER\s+([\d.]+)");
        double perCitizen = Figure(report, @"posts per Citizen\s+([\d.]+)");
        double perWorker = Figure(report, @"Citizens per worker\s+([\d.]+)");

        Assert.True(
            Math.Abs((perCitizen * perWorker) - ratio) < 0.02,
            $"{perCitizen} x {perWorker} = {perCitizen * perWorker}, printed as {ratio}");
    }

    /// <summary>
    /// ✅ <b>The finding <c>plans/0046</c> was reopened for, now repaired: the dwelling stock
    /// falls.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test asserted the OPPOSITE and its own remark said it would</b> — <em>"expected to go
    /// red the day something demolishes, and that failure is the repair landing rather than a
    /// regression"</em>. <c>plans/0045</c> row 13 is that day.
    /// <c>[[building]] abandoned_when_empty_after_days</c> gives the stock a sink, and it is
    /// <c>adr/0069</c>'s build predicate mirrored rather than decline: the developer builds while the
    /// Unplaced Pool is non-empty and gives up on a Building the Pool never came for.
    /// </para>
    /// <para>
    /// ⚠ <b>Asserted as a shape and never as a count.</b> How many Days of 400 see a fall is a figure
    /// for a document to quote, and pinning it would make an instrument out of an assertion. What is
    /// asserted is that the number is not zero, which is the whole content of the old claim inverted.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_dwelling_stock_now_falls()
    {
        string report = Dump();

        Assert.Matches(@"Days the stock fell\s+[1-9]", report);
        Assert.DoesNotMatch(@"Days the stock fell\s+0\b", report);
    }

    /// <summary>
    /// 🔴 <b>Why the sink does not restore <c>jobs = 8</c>: a shrinking city does not
    /// consolidate.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>PlacementEngine.TryHouse</c> takes the first Lot with room out of a draw of three and
    /// nothing biases it toward a fuller house</b> (<c>adr/0069</c>), so the Households left after a
    /// demographic trough are spread ONE PER DWELLING instead of filling houses and vacating them.
    /// ***Over half the housing capacity in the city is empty while under a tenth of its houses
    /// are*** — and a sink keyed on an empty house can only collect the tail of that distribution,
    /// which is why a sixteenfold sweep of the clock moves <c>posts per Citizen</c> by 0.4.
    /// </para>
    /// <para>
    /// ⚠ <b>It asserts the GAP and not either number.</b> Both move with the run; what does not move
    /// is that the share of empty slots is far larger than the share of empty houses, and that gap is
    /// the finding. ⚠ <b>Neither is a defect</b>: a family choosing between two houses has no reason
    /// to prefer the one with neighbours in it, and a placement that steered toward occupancy would
    /// be the optimiser <c>adr/0017</c> refuses.
    /// </para>
    /// </remarks>
    [Fact]
    public void Empty_housing_capacity_far_outruns_empty_houses()
    {
        string report = Dump();

        double slotsFree = Figure(report, @"housing slots empty\s+([\d.]+)%");
        double homesEmpty = Figure(report, @"homes housing nobody\s+([\d,]+)\s+of\s+([\d,]+)");
        double standing = Figure(report, @"homes housing nobody\s+[\d,]+\s+of\s+([\d,]+)");

        Assert.True(
            slotsFree > 3 * (100.0 * homesEmpty / standing),
            $"{slotsFree}% of housing slots stand empty against "
            + $"{100.0 * homesEmpty / standing:N1}% of houses. The two converging means the city has "
            + "started to consolidate, which nothing in the build does — read the panel.");
    }

    /// <summary>
    /// 🔴 <b><c>jobs = 8</c> no longer buys what it was derived to buy.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0023</c> recorded the property the flooring bought</b> — <em>"full employment is
    /// out of reach by construction and the shortage flow is never trivially zero, which was the
    /// point"</em>. ⚠ <b>On a world with demographics it is in reach on most Days</b>, and this test
    /// says so with a number rather than leaving the claim in prose. ***It asserts the property is
    /// BROKEN***, which is the honest thing to pin while the cause lives in a stock that has no
    /// sink: re-deriving <c>jobs</c> to make this pass would be <c>adr/0073</c>'s local workaround
    /// for a cause somewhere else.
    /// </remarks>
    [Fact]
    public void Full_employment_is_no_longer_out_of_reach()
    {
        string report = Dump();

        Assert.True(
            Figure(report, @"full employment\s+([\d,]+)") > 0,
            "posts never reached workers on any Day — `jobs = 8`'s derived property has come back, "
            + "so read the panel and re-derive the number rather than deleting this test.");
    }

    private static double Figure(string report, string pattern)
    {
        Match match = Regex.Match(report, pattern);

        Assert.True(match.Success, $"the panel printed no line matching {pattern}");

        return double.Parse(
            match.Groups[1].Value.Replace(",", "", StringComparison.Ordinal),
            System.Globalization.CultureInfo.InvariantCulture);
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
