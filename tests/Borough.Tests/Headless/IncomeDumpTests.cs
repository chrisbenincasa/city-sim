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
/// the direction a budget exists to be right about***.
/// <br/>
/// ⚠ <b>The shipped file used to make that assertion sharp by declaring no inbound
/// <c>[[policy]]</c> at all, and change 7 of <c>taxing.toml</c> ended that.</b> The file now states
/// a <c>tool = "charge"</c>, which <c>PolicyEngine</c> folds into the same accumulator as a plain
/// transfer, so the policy income column is non-zero and the test asserts the two columns DIFFER
/// instead. ***The demonstration is worth more than the sharpness***: a catalogue no shipped world
/// exercises is a catalogue nobody can read a budget off.
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

        // CHANGE 6 of taxing.toml, and the same argument one taxpayer along: a profit-tax column is
        // uninterpretable without the bands it came from.
        Assert.Contains("lower band         to 65,536 profit a Day, at 10%", report, Ordinal);
        Assert.Contains("upper band         above 65,536 a Day, at 20%", report, Ordinal);
    }

    /// <summary>
    /// A file with an income tax and no profit tax says so rather than printing a zero column.
    /// </summary>
    /// <remarks>
    /// <b>The polarity <c>--income</c> refuses for the OTHER schedule, and the difference is which
    /// column the mode exists to show.</b> A file with no <c>[income_tax]</c> is refused outright,
    /// because the withheld column is the subject; the profit tax is the fourth column, so an
    /// absence is printed rather than fatal — and ***a stated absence and a schedule that takes
    /// nothing are different cities***, which a column of zeroes could not tell apart.
    /// </remarks>
    [Fact]
    public void A_file_with_no_profit_tax_says_so_instead_of_printing_zeroes()
    {
        string report = Dump("shopping-taxed.toml", Untaxed());

        Assert.Contains("states no [business_tax]", report, Ordinal);
        Assert.Equal(0, Column(report, 2));
    }

    /// <summary>
    /// 🔴 The withholding is an income column of its own, and the Policy column stays at zero.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are asserted and neither alone would do.</b> A report that had folded the
    /// withholding into the Policy column would pass an assertion that income moved; a report over a
    /// world where nothing was ever paid would pass an assertion that the two are unequal. Together
    /// they say the treasury took money in through a mechanism no <c>[[policy]]</c> supplied — which
    /// is the mechanism this mode was built to make visible.
    /// <para>
    /// 🔴 <b>This assertion USED to be <c>policy income is zero</c>, and change 7 of
    /// <c>taxing.toml</c> took that away.</b> The file now declares a <c>tool = "charge"</c>, which
    /// <c>PolicyEngine</c> folds into the same accumulator as a plain transfer — so the sharp form
    /// is no longer available and the honest one is that the two columns differ. ***A test that had
    /// been left asserting the zero would have been asserting that the catalogue was not
    /// demonstrated.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Withholding_is_its_own_income_column_and_is_not_folded_into_the_policy_one()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.True(
            gross.Withheld > 0, "no payday ever withheld anything, so the column proves nothing.");
        Assert.True(
            gross.Policy > 0,
            "no Policy paid anything into the treasury. CHANGE 7 of taxing.toml is a charge on a "
            + "grocer's till, and a zero here is the charge never firing.");
        Assert.True(
            gross.Policy != gross.Withheld,
            $"the policy column and the withheld column both read {gross.Policy}, which is what a "
            + "report folding one into the other would print.");

        // ⚠ Not zero, and asserting that it were would be asserting the demonstration is broken.
        // The file declares a Policy that spends OUT of the treasury, because row 33 needs income
        // read against expenditure and a column of zeros is not expenditure.
        Assert.True(
            gross.Spent > 0, "nothing was ever spent, so there is no expenditure to read against.");
    }

    /// <summary>
    /// 🔴 All three tools of the targeted catalogue moved something, and the subsidy's pot bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A demonstration where a tool never fires is a demonstration of nothing.</b> Each of the
    /// three is individually easy to ship switched off — a charge whose <c>trade</c> matches no
    /// Business, a relief on a city whose shops never turn a profit, a subsidy whose ceiling is
    /// never reached — and every one of those loads clean and prints a column of zeroes.
    /// </para>
    /// <para>
    /// ⚠ <b>The rationing assertion is the one that is not implied by the others.</b> A subsidy that
    /// pays every claim in full exercises <c>Apportionment</c>'s trivial case only, so <c>Cut</c>
    /// being non-zero is what says the largest-remainder arithmetic ran on this world at all —
    /// <c>plans/0072</c> D12's whole subject.
    /// </para>
    /// </remarks>
    [Fact]
    public void All_three_tools_of_the_catalogue_moved_something_on_the_shipped_file()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.True(gross.Policy > 0, "the charge collected nothing.");
        Assert.True(gross.Relieved > 0, "the relief forwent nothing, so no bill was ever reduced.");
        Assert.True(gross.Granted > 0, "the subsidy paid nothing out of the treasury.");
        Assert.True(
            gross.Claimed > gross.Granted,
            $"the subsidies claimed {gross.Claimed} and were paid {gross.Granted}, so the ceiling "
            + "never bound and Apportionment's rationing branch is unexercised by any shipped "
            + "world. CHANGE 9 of taxing.toml states a ceiling chosen to bind.");
        Assert.True(gross.Cut > 0, "no claimant was ever cut, so nothing was rationed.");
    }

    /// <summary>
    /// 🔴 Relief is printed and is deliberately outside the arithmetic, because no Money moved.
    /// </summary>
    /// <remarks>
    /// <b>The mistake this forbids is a one-line one and it breaks the budget by exactly the amount
    /// forgone.</b> A relief feels like spending — the city gave something up — but it debited no
    /// Bin and credited none, so adding it to expenditure would make the treasury column stop being
    /// the flows beside it. ***The identity is the test***: it is asserted in
    /// <see cref="The_columns_sum_to_what_the_run_says_and_the_balance_is_their_total"/> against a
    /// run in which relief is large, so a report that folded it in could not pass both.
    /// </remarks>
    [Fact]
    public void Relief_is_printed_as_revenue_forgone_and_is_not_in_the_identity()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.Contains("Revenue forgone", report, Ordinal);
        Assert.Contains("Revenue forgone is not expenditure.", report, Ordinal);
        Assert.True(gross.Relieved > 0, "nothing was relieved, so the caveat covers nothing.");

        long expenditure = gross.Spent + gross.Drawn + gross.Granted;
        long income = gross.Withheld + gross.Profit + gross.Policy + gross.Rule;

        Assert.Equal(income - expenditure, Rows(report)[^1].Treasury);
        Assert.NotEqual(income - expenditure - gross.Relieved, Rows(report)[^1].Treasury);
    }

    /// <summary>
    /// 🔴 The profit tax is an income column of its own, and on this file it is the largest one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two taxes are the pair most easily folded and the pair that must not be.</b> They are
    /// levied under different tables on different payers — a rate in <c>[income_tax]</c> against a
    /// wage on its way to a purse, a rate in <c>[business_tax]</c> against a till that was already
    /// holding it — and <c>plans/0072</c>'s whole question is which of the two a city lives on. A
    /// report that added them could not answer it.
    /// </para>
    /// <para>
    /// ⚠ <b>The magnitude comparison is the half that matters.</b> On <c>taxing.toml</c> the profit
    /// tax is more than twenty times the withholding, because the file's shops take their stock from
    /// an input-less Rule and so post a profit that is very nearly their whole revenue. A column that
    /// has become the smaller one is reading something other than the collection.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_profit_tax_is_its_own_income_column_and_is_not_folded_into_the_withheld_one()
    {
        string report = Dump("taxing.toml");

        Gross gross = OverTheRun(report);

        Assert.True(gross.Profit > 0, "no Business ever paid a profit tax, so the column is empty.");
        Assert.True(gross.Withheld > 0, "no payday withheld anything, so there is nothing to hold it apart from.");

        Assert.True(
            gross.Profit > gross.Withheld,
            $"the profit tax collected {gross.Profit} against {gross.Withheld} withheld; on this "
            + "file the profit tax is the larger of the two taxes, and a column that has become the "
            + "smaller one is reading something other than the collection.");
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
        Assert.True(
            gross.Rule != gross.Policy,
            $"the rule column and the policy column both read {gross.Rule}, which is what folding "
            + "one into the other would print.");
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
        Assert.Contains("profit tax", report, Ordinal);
        Assert.Contains("policy in", report, Ordinal);
        Assert.Contains("rule in", report, Ordinal);
        Assert.Contains("subsidy out", report, Ordinal);
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
        Assert.Equal(gross.Profit, Column(report, 2));
        Assert.Equal(gross.Policy, Column(report, 3));
        Assert.Equal(gross.Rule, Column(report, 4));
        Assert.Equal(gross.Spent, Column(report, 5));
        Assert.Equal(gross.Drawn, Column(report, 6));
        Assert.Equal(gross.Granted, Column(report, 7));

        long income = gross.Withheld + gross.Profit + gross.Policy + gross.Rule;
        long expenditure = gross.Spent + gross.Drawn + gross.Granted;

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
        Assert.Contains("Income is FOUR columns", Options.Usage, Ordinal);
    }

    private const StringComparison Ordinal = StringComparison.Ordinal;

    /// <summary>One parsed budget row.</summary>
    private readonly record struct BudgetRow(
        long Tick,
        long Withheld,
        long Profit,
        long Policy,
        long Rule,
        long Spent,
        long Drawn,
        long Granted,
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

            if (cells.Length != 10)
            {
                break;
            }

            rows.Add(new BudgetRow(
                Number(cells[0]), Number(cells[1]), Number(cells[2]), Number(cells[3]),
                Number(cells[4]), Number(cells[5]), Number(cells[6]), Number(cells[7]),
                Number(cells[8]), Number(cells[9])));
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
                2 => row.Profit,
                3 => row.Policy,
                4 => row.Rule,
                5 => row.Spent,
                6 => row.Drawn,
                7 => row.Granted,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, "not a flow column."),
            };
        }

        return total;
    }

    /// <summary>The six gross figures from the budget's closing sentences.</summary>
    private static Gross OverTheRun(string report)
    {
        // "  Into the treasury: N withheld from wages, P in profit tax, M by a Policy, R by a Bin
        //  Rule."
        string[] into = Line(report, "  Into the treasury:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // "  Out of it: S by a Policy, D by a Bin Rule, G in subsidy."
        string[] outOf = Line(report, "  Out of it:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // "  Claimed in subsidy: C, of which N claimant-Days were cut by the ceiling, on D Days."
        string[] claim = Line(report, "  Claimed in subsidy:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // "  Revenue forgone: R in profit-tax relief."
        string[] forgone = Line(report, "  Revenue forgone:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new Gross(
            Number(into[3]), Number(into[7]), Number(into[11]), Number(into[15]),
            Number(outOf[3]), Number(outOf[7]), Number(outOf[12]),
            Number(claim[3].TrimEnd(',')), Number(claim[6]), Number(forgone[2]));
    }

    /// <summary>
    /// The budget's closing figures, named so a caller cannot transpose two.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The last three are NOT flows and are carried here so a test can assert they stay out of
    /// the identity.</b> <c>Claimed</c> is what the subsidies were asked for, <c>Cut</c> how many
    /// claimant-Days the ceiling trimmed, and <c>Relieved</c> profit tax the reliefs forwent — and
    /// not one unit of any of them crossed the treasury's edge.
    /// </remarks>
    private readonly record struct Gross(
        long Withheld,
        long Profit,
        long Policy,
        long Rule,
        long Spent,
        long Drawn,
        long Granted,
        long Claimed,
        long Cut,
        long Relieved);

    private static long Number(string cell) =>
        long.Parse(cell.Replace(",", string.Empty, Ordinal), CultureInfo.InvariantCulture);

    private static string Line(string report, string starting) =>
        report.Split('\n').FirstOrDefault(line => line.StartsWith(starting, Ordinal))
        ?? throw new InvalidOperationException(
            $"the report has no line starting '{starting}'. Its shape has moved.");

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static string Dump(string ruleset, string? path = null)
    {
        (int code, string report) = Run(path ?? Ruleset(ruleset));

        Assert.Equal(0, code);

        return report;
    }

    /// <summary>
    /// <c>rulesets/taxing.toml</c> with its <c>[business_tax]</c> table cut, written to a temporary
    /// file.
    /// </summary>
    /// <remarks>
    /// <b>The shipped file minus one table rather than a second fixture</b>, so the two runs differ
    /// in nothing else — which is what makes
    /// <see cref="A_file_with_no_profit_tax_says_so_instead_of_printing_zeroes"/> a statement about
    /// the table. ⚠ <b>The LAST occurrence</b>: the header names the table several times before
    /// declaring it.
    /// </remarks>
    private static string Untaxed()
    {
        string text = File.ReadAllText(Ruleset("taxing.toml"));
        int at = text.LastIndexOf("[business_tax]", Ordinal);

        Assert.True(at > 0, "rulesets/taxing.toml no longer declares [business_tax].");

        string path = Path.Combine(Path.GetTempPath(), $"borough-untaxed-{Guid.NewGuid():N}.toml");

        File.WriteAllText(path, text[..at]);

        return path;
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
