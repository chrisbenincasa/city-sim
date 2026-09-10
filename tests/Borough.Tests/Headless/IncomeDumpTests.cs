using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// <c>plans/0072</c> — <c>--income</c>, the budget a player reads income and expenditure off.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is
/// <see cref="Withholding_is_its_own_income_column_and_is_not_folded_into_the_policy_one"/></b>,
/// because it holds the defect the mode exists to close. A tax withheld at a payday reaches the
/// treasury without any Policy moving it, so before <c>MoneyFlowCounter.Withheld</c> the balance rose
/// against a reported income of zero — ***and a budget that under-reports income is wrong in exactly
/// the direction a budget exists to be right about***. The shipped file makes the assertion sharp: it
/// declares one <c>[[policy]]</c> and it moves money only OUT of the treasury, so the policy
/// <em>income</em> column is zero for the whole run and any income at all must have come through
/// the payday or through a Bin Rule.
/// </para>
/// <para>
/// 🔴 <b>Its pair is
/// <see cref="The_columns_sum_to_what_the_run_says_and_the_balance_is_their_total"/></b>, which is
/// the assertion the second defect cost. A Bin Rule whose output names <c>scope = "global"</c> paid
/// the treasury and appeared in no flow, so the balance could only be asserted as an inequality —
/// ***and an inequality is what a budget says when it cannot explain itself***. With
/// <c>MoneyFlowCounter.RuleToTreasury</c> counting that path, the balance is the flows exactly, and
/// the equality is back.
/// </para>
/// <para>
/// ⚠ <b>This mode refuses where <c>--evidence</c> prints, on <c>MoneyDumpTests</c>' reasoning.</b>
/// A file that levies no income tax withholds nothing, and a zero column under a heading that says
/// <em>income</em> reads as a payday that stopped working rather than as a city that taxes nobody.
/// The absence cannot be made legible, so the input is refused instead.
/// </para>
/// </remarks>
public sealed class IncomeDumpTests
{
    private const string Population = "1000";

    /// <summary>
    /// Six Days, which is what it takes for anything to be withheld at all.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Not a cadence and not a choice about the picture</b> — it is how long the shipped world
    /// takes to employ somebody and then find an employer able to pay them. <c>taxing.toml</c> pays a
    /// seven-Day period, so the earliest payday that can move money is Day 1 and the earliest that
    /// actually does is later still. A shorter run would assert against a table of true zeroes.
    /// </remarks>
    private const string Ticks = "12288";

    /// <summary>The bands are printed above the table, so the column below can be checked by hand.</summary>
    /// <remarks>
    /// <b>A withheld figure is uninterpretable on its own.</b> The only way anybody finds out that
    /// the rates are reaching something other than what they think is by dividing the column by what
    /// the schedule says a Day is worth, and that needs the schedule on the same page.
    /// </remarks>
    [Fact]
    public void It_prints_the_schedule_the_run_was_taxed_under()
    {
        string report = Dump("taxing.toml");

        Assert.Contains("What the Ruleset levies", report, Ordinal);
        Assert.Contains("allowance          512 a Day", report, Ordinal);
        Assert.Contains("to 1,536 a Day, at 20%", report, Ordinal);
        Assert.Contains("above 1,536 a Day, at 40%", report, Ordinal);
        Assert.Contains("posts 2,048 a Day and pays every 7 Days.", report, Ordinal);
    }

    /// <summary>
    /// 🔴 The withholding is an income column of its own, and the Policy column stays at zero.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are asserted and neither alone would do.</b> A report that had folded the
    /// withholding into the Policy column would pass an assertion that income moved; a report over a
    /// world where nothing was ever paid would pass an assertion that the Policy column is zero.
    /// Together they say the treasury took money in through a mechanism no <c>[[policy]]</c> could
    /// have supplied — which is the mechanism this mode was built to make visible.
    /// </remarks>
    [Fact]
    public void Withholding_is_its_own_income_column_and_is_not_folded_into_the_policy_one()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.True(
            gross.Withheld > 0, "no payday ever withheld anything, so the column proves nothing.");
        Assert.Equal(0, gross.Policy);

        // ⚠ Not zero, and asserting that it were would be asserting the demonstration is broken.
        // The file declares a Policy that spends OUT of the treasury, because row 33 needs income
        // read against expenditure and a column of zeros is not expenditure. What keeps this test
        // sharp is that no Policy pays money IN, so the income above still cannot have come from
        // one.
        Assert.True(
            gross.Spent > 0, "nothing was ever spent, so there is no expenditure to read against.");
    }

    /// <summary>
    /// 🔴 The Bin Rule's payment is an income column of its own, and it is the larger one.
    /// </summary>
    /// <remarks>
    /// <b>This is the defect row 33 closed.</b> <c>taxing.toml</c>'s <c>rates</c> Rule moves 16,384
    /// out of a shopfront and into the treasury on every firing, and until
    /// <c>MoneyFlowCounter.RuleToTreasury</c> that arrival appeared in no flow the instrument could
    /// see: over 24,576 Ticks it was <b>1,441,792</b> of a closing <b>1,621,850</b>, so <b>89%</b> of
    /// the treasury's income was unattributed and the footnote under this table said so. ⚠ <b>The
    /// magnitude comparison is the half that matters</b> — a report folding the rates bill into the
    /// policy column would still pass an assertion that income moved, and it would have to break the
    /// zero this asserts beside it.
    /// </remarks>
    [Fact]
    public void A_bin_rule_paying_the_treasury_is_its_own_income_column()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.True(gross.Rule > 0, "the rates Rule never fired, so the column proves nothing.");
        Assert.Equal(0, gross.Policy);
        Assert.Equal(0, gross.Drawn);
        Assert.True(
            gross.Rule > gross.Withheld,
            $"the rates Rule paid {gross.Rule} against {gross.Withheld} withheld; on this file the "
            + "Rule is the larger income, and a column that has become the smaller one is reading "
            + "something other than the firing.");
    }

    /// <summary>The three flows are printed apart, and the report says why.</summary>
    /// <remarks>
    /// <c>MoneyFlowCounter.FromTreasury</c>'s own remark is the argument, and this holds the report
    /// to it: a net cannot say whether a city taxed nothing and paid nothing or taxed heavily and
    /// paid it all back.
    /// </remarks>
    [Fact]
    public void It_states_income_against_expenditure_and_never_a_net()
    {
        string report = Dump("taxing.toml");

        Assert.Contains("withheld", report, Ordinal);
        Assert.Contains("policy in", report, Ordinal);
        Assert.Contains("rule in", report, Ordinal);
        Assert.Contains("never netted", report, Ordinal);
        Assert.DoesNotContain("a net of", report, Ordinal);
    }

    /// <summary>
    /// Every column sums to its closing sentence, and the treasury holds exactly what they say.
    /// </summary>
    /// <remarks>
    /// <b>The second half is an EQUALITY, and it was an inequality for one reason only.</b> A Bin
    /// Rule whose output named <c>scope = "global"</c> reached the treasury and no Census flow
    /// counted it, so the balance stood <em>above</em> the income these columns could name and the
    /// most the table could honestly claim was a direction. <c>MoneyFlowCounter.RuleToTreasury</c>
    /// closed that path, so the claim is the one a budget always owed its reader: ***the balance is
    /// the flows***. The treasury opens empty (<c>adr/0116</c>), so the last row's level is the whole
    /// run's income less its whole expenditure.
    /// </remarks>
    [Fact]
    public void The_columns_sum_to_what_the_run_says_and_the_balance_is_their_total()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.Equal(gross.Withheld, Column(report, 1));
        Assert.Equal(gross.Policy, Column(report, 2));
        Assert.Equal(gross.Rule, Column(report, 3));
        Assert.Equal(gross.Spent, Column(report, 4));
        Assert.Equal(gross.Drawn, Column(report, 5));

        long income = gross.Withheld + gross.Policy + gross.Rule;
        long expenditure = gross.Spent + gross.Drawn;

        Assert.Equal(income - expenditure, Rows(report)[^1].Treasury);
    }

    /// <summary>
    /// A Ruleset that levies nothing is refused, and the complaint names the file that works.
    /// </summary>
    /// <remarks>
    /// <c>minimal.toml</c> states no <c>[income_tax]</c>, so the income half would be a column of
    /// zeroes — and a zero flow reads as a broken payday rather than as a file that taxes nobody.
    /// </remarks>
    [Fact]
    public void It_refuses_a_ruleset_that_levies_nothing()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"));

        Assert.Equal(3, code);
        Assert.Contains("[income_tax]", report, Ordinal);
        Assert.Contains("rulesets/taxing.toml", report, Ordinal);
    }

    /// <summary>The mode needs a Ruleset, because a budget is content twice over.</summary>
    [Fact]
    public void It_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--income", "--ticks", Ticks], out Options? _, out string? complaint));

        Assert.Contains("--income needs --ruleset", complaint!, Ordinal);
    }

    /// <summary>A recorded session and a dump that populates its own world disagree.</summary>
    [Fact]
    public void It_refuses_a_log()
    {
        Assert.False(
            Options.TryParse(
                ["--income", "--ruleset", Ruleset("taxing.toml"), "--log", "session.borough"],
                out Options? _,
                out string? complaint));

        Assert.Contains("--income and --log disagree", complaint!, Ordinal);
    }

    /// <summary>Each picture builds its own world, so two of them are refused.</summary>
    [Fact]
    public void It_refuses_a_second_picture()
    {
        Assert.False(
            Options.TryParse(
                ["--income", "--money", "--ruleset", Ruleset("taxing.toml")],
                out Options? _,
                out string? complaint));

        Assert.Contains("Ask for one", complaint!, Ordinal);
    }

    /// <summary>
    /// A census rides a run and this is a picture, so the flag is refused rather than ignored.
    /// </summary>
    [Fact]
    public void It_refuses_a_census()
    {
        Assert.False(
            Options.TryParse(
                ["--income", "--ruleset", Ruleset("taxing.toml"), "--census"],
                out Options? _,
                out string? complaint));

        Assert.Contains("picture", complaint!, Ordinal);
    }

    /// <summary>
    /// The mode is listed in <c>--help</c>, which <c>CLAUDE.md</c> makes the one place they are.
    /// </summary>
    /// <remarks>
    /// <b>A mode reachable and undocumented is a mode nobody runs.</b> Every other picture asserts
    /// this the same way, and the second string is the sentence that distinguishes this budget from
    /// <c>--money</c>'s circuit.
    /// </remarks>
    [Fact]
    public void The_mode_is_named_in_the_usage()
    {
        Assert.Contains("--income", Options.Usage, Ordinal);
        Assert.Contains("Income is THREE columns", Options.Usage, Ordinal);
    }

    private const StringComparison Ordinal = StringComparison.Ordinal;

    /// <summary>One parsed budget row.</summary>
    private readonly record struct BudgetRow(
        long Tick,
        long Withheld,
        long Policy,
        long Rule,
        long Spent,
        long Drawn,
        long Treasury,
        long Households);

    /// <summary>Every row of the budget table, in the order it was printed.</summary>
    private static BudgetRow[] Rows(string report)
    {
        List<BudgetRow> rows = [];
        bool started = false;

        foreach (string line in report.Split('\n'))
        {
            if (line.StartsWith("tick ", Ordinal))
            {
                started = true;
                continue;
            }

            if (!started || line.Length == 0 || line[0] is '-' or ' ')
            {
                continue;
            }

            string[] cells = line.Split(
                "  ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (cells.Length != 8)
            {
                break;
            }

            rows.Add(new BudgetRow(
                Number(cells[0]), Number(cells[1]), Number(cells[2]), Number(cells[3]),
                Number(cells[4]), Number(cells[5]), Number(cells[6]), Number(cells[7])));
        }

        Assert.NotEmpty(rows);

        return [.. rows];
    }

    /// <summary>One column of the budget table, summed.</summary>
    private static long Column(string report, int index)
    {
        long total = 0;

        foreach (BudgetRow row in Rows(report))
        {
            total += index switch
            {
                1 => row.Withheld,
                2 => row.Policy,
                3 => row.Rule,
                4 => row.Spent,
                5 => row.Drawn,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "not a flow column."),
            };
        }

        return total;
    }

    /// <summary>The five gross figures from the budget's closing sentences.</summary>
    private static Gross OverTheRun(string report)
    {
        // "  Into the treasury: N withheld from wages, M by a Policy, R by a Bin Rule."
        string[] into = Line(report, "  Into the treasury:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // "  Out of it: S by a Policy, D by a Bin Rule."
        string[] outOf = Line(report, "  Out of it:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new Gross(
            Number(into[3]), Number(into[7]), Number(into[11]),
            Number(outOf[3]), Number(outOf[7]));
    }

    /// <summary>The budget's five closing figures, named so a caller cannot transpose two.</summary>
    private readonly record struct Gross(
        long Withheld, long Policy, long Rule, long Spent, long Drawn);

    private static long Number(string cell) =>
        long.Parse(cell.Replace(",", string.Empty, Ordinal), CultureInfo.InvariantCulture);

    private static string Line(string report, string starting) =>
        report.Split('\n').FirstOrDefault(line => line.StartsWith(starting, Ordinal))
        ?? throw new InvalidOperationException(
            $"the report has no line starting '{starting}'. Its shape has moved.");

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static string Dump(string ruleset)
    {
        (int code, string report) = Run(Ruleset(ruleset));

        Assert.Equal(0, code);

        return report;
    }

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--income", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = IncomeDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
