using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// Milestone 26 task 8 — <c>--market</c>, the milestone's picture.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is
/// <see cref="Somebody_could_not_afford_it_and_that_is_the_panel_that_matters"/></b>, because
/// <c>plans/0044</c> task 8 named that panel as ***the one that would be dropped*** and said why:
/// a Pool with stock in it and a price beside it is a table, and only a Rule stopped on a money Bin
/// shows the market having a consequence for somebody. Everything else here is a refusal or a shape.
/// </para>
/// <para>
/// 🔴 <b><see cref="The_price_does_not_move_and_the_dump_says_so_in_the_table"/> ASSERTS A DEFECT AND
/// IS MEANT TO GO RED.</b> It pins the two halves of <c>plans/0044</c> <b>F50</b> — the Pool's own Bin
/// is empty in every row, and that is the level <c>MarketRuleset.Reprice</c> reads as cover — so on
/// the day somebody gives the reprice a measure of cover a market-not-a-store can supply, this fails
/// and names what changed. ⚠ <b>It does not assert that a flat price is correct</b>; it asserts that
/// the price is flat *and* that the input which makes it flat is zero, which is a diagnosis rather
/// than an expectation.
/// </para>
/// <para>
/// ⚠ <b>The third refusal is the one worth a test.</b> Two of them read off a Ruleset table a person
/// can see — <c>[districts]</c>, <c>[market]</c> — and the third is a property of a Bin declaration
/// three levels down. <c>rulesets/twinned.toml</c> states both tables, declares two
/// <c>[[business]]</c> trades, and sells nothing, so ***declaring a trade is not the same test as
/// having a seller***.
/// </para>
/// </remarks>
public sealed class MarketDumpTests
{
    private const string Population = "2000";

    /// <summary>
    /// Twelve Days, and the horizon is set by how late the seller arrives rather than by the price.
    /// </summary>
    /// <remarks>
    /// A market row waits on <c>[districts] revisit_ticks</c>, a seller waits on a Zone Rule's
    /// sample, and a Household short of money has to have spent its opening balance down. A shorter
    /// run prints an honest picture of a city where none of that has happened yet.
    /// </remarks>
    private const string Ticks = "24576";

    private const StringComparison Ordinal = StringComparison.Ordinal;

    /// <summary>
    /// The panel <c>plans/0044</c> task 8 said would be dropped, and the number in it.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are asserted and neither alone would do.</b> A dump whose money row was always
    /// zero would pass an assertion that the panel exists; a dump that counted every starving Rule as
    /// a money shortfall would pass an assertion that the number is positive. Together they say the
    /// classification discriminates and that the discriminated case actually occurs.
    /// </remarks>
    [Fact]
    public void Somebody_could_not_afford_it_and_that_is_the_panel_that_matters()
    {
        string report = Dump("provisioned.toml");

        long money = Instances(report, "money — it could not afford it");
        long larder = Instances(report, "a Bin of its own — the larder is empty");

        Assert.True(
            money > 0,
            "no Rule was stopped on a money Bin, so the picture shows a market with no consequence.");

        Assert.True(
            larder > 0,
            "every starving Rule was blamed on money, so the classification is not discriminating.");

        Assert.Contains("Named — the first", report, Ordinal);
    }

    /// <summary>
    /// 🔴 The price is pinned at the ceiling, and the column that pins it is printed beside it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>THIS TEST EXISTS TO FAIL ONE DAY.</b> It is <c>plans/0044</c> <b>F50</b> written as an
    /// assertion, and the two clauses are the finding: every price moved zero times, and the Pool's
    /// own Bin — which is what <c>MarketRuleset.Reprice</c> takes as <c>level</c> — holds nothing in
    /// every row. A fix makes the first clause false and this test names the second as the reason.
    /// </remarks>
    [Fact]
    public void The_price_does_not_move_and_the_dump_says_so_in_the_table()
    {
        string report = Dump("provisioned.toml");

        Assert.Contains("NOT ONE PRICE MOVED", report, Ordinal);

        foreach (string row in Rows(report, "What the price did"))
        {
            string[] cells = Cells(row);

            Assert.Equal(cells[2], cells[3]);
            Assert.Equal("0", cells[^1]);
        }

        foreach (string row in Rows(report, "Where the market is"))
        {
            Assert.Equal(0, Number(Cells(row)[^2]));
        }
    }

    /// <summary>
    /// The stock column sums the sellers, because a Pool is a market and not a store.
    /// </summary>
    /// <remarks>
    /// <b>It is asserted against the Pool's own level rather than against a constant.</b> A row with
    /// sellers holding goods and a Pool holding nothing is <c>adr/0139</c> visible in one line, and
    /// it is the shape a future *store* implementation would break.
    /// </remarks>
    [Fact]
    public void Stock_is_held_by_the_sellers_and_never_by_the_pool()
    {
        string report = Dump("provisioned.toml");

        long stocked = 0;

        foreach (string row in Rows(report, "Where the market is"))
        {
            string[] cells = Cells(row);
            long sellers = Number(cells[^4]);
            long stock = Number(cells[^3]);

            Assert.Equal(0, Number(cells[^2]));

            if (sellers == 0)
            {
                Assert.Equal(0, stock);
            }

            stocked += stock;
        }

        Assert.True(stocked > 0, "no seller anywhere held any stock, so the market has one side.");
    }

    /// <summary>A world with no centre has no market row, and the complaint says which table.</summary>
    [Fact]
    public void It_refuses_a_ruleset_with_no_districts()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"), ticks: "100");

        Assert.Equal(3, code);
        Assert.Contains("no [districts]", report, Ordinal);
        Assert.Contains("rulesets/provisioned.toml", report, Ordinal);
    }

    /// <summary>
    /// 🔴 A Ruleset that declares a trade and sells nothing is refused, and that is the refusal that
    /// would have been missed.
    /// </summary>
    /// <remarks>
    /// <c>rulesets/twinned.toml</c> states <c>[districts]</c>, states <c>[market]</c>, names two
    /// <c>[[business]]</c> trades and instantiates neither — so both of the checks a reader would
    /// think to write pass, and the market has one side.
    /// </remarks>
    [Fact]
    public void It_refuses_a_ruleset_in_which_nothing_sells()
    {
        (int code, string report) = Run(Ruleset("twinned.toml"), ticks: "100");

        Assert.Equal(3, code);
        Assert.Contains("BUSINESS-owned Bin", report, Ordinal);
        Assert.Contains("rulesets/twinned.toml", report, Ordinal);
    }

    /// <summary>Without a Ruleset there is no market, and the complaint names all three halves.</summary>
    [Fact]
    public void It_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--market", "--ticks", "100"], out _, out string? complaint));

        Assert.Contains("--market needs --ruleset", complaint!, Ordinal);
        Assert.Contains("rulesets/provisioned.toml", complaint!, Ordinal);
    }

    /// <summary>A recorded session would be replayed and then over-populated.</summary>
    [Fact]
    public void It_refuses_a_log()
    {
        Assert.False(
            Options.TryParse(
                ["--market", "--ruleset", Ruleset("provisioned.toml"), "--log", "a.borough"],
                out _,
                out string? complaint));

        Assert.Contains("--market and --log disagree", complaint!, Ordinal);
    }

    /// <summary>Each picture builds its own world, so two of them is two worlds.</summary>
    [Fact]
    public void It_refuses_a_second_picture()
    {
        Assert.False(
            Options.TryParse(
                ["--market", "--business", "--ruleset", Ruleset("provisioned.toml")],
                out _,
                out string? complaint));

        Assert.Contains("Ask for one", complaint!, Ordinal);
    }

    /// <summary>A census rides a run, and a picture is not one however much it steps.</summary>
    [Fact]
    public void It_refuses_a_census()
    {
        Assert.False(
            Options.TryParse(
                ["--market", "--census", "--ruleset", Ruleset("provisioned.toml")],
                out _,
                out string? complaint));

        Assert.Contains("--census and the picture modes disagree", complaint!, Ordinal);
    }

    /// <summary>The flag selects the mode and the help text describes it.</summary>
    [Fact]
    public void The_flag_selects_the_mode_and_is_documented()
    {
        Assert.True(
            Options.TryParse(
                ["--market", "--ruleset", Ruleset("provisioned.toml")],
                out Options? options,
                out string? complaint),
            complaint);

        Assert.Equal(Mode.Market, options!.Mode);

        Assert.Contains("--market", Options.Usage, Ordinal);
        Assert.Contains("WHO COULD NOT AFFORD IT", Options.Usage, Ordinal);
    }

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--market", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = MarketDump.Run(options!, writer);

        return (code, writer.ToString());
    }

    private static string Dump(string ruleset)
    {
        (int code, string report) = Run(Ruleset(ruleset));

        Assert.True(code == 0, report);

        return report;
    }

    /// <summary>The data rows of one panel — between its rule and the blank line after it.</summary>
    private static List<string> Rows(string report, string panel)
    {
        string[] lines = report.Split('\n');
        List<string> rows = [];
        bool inside = false;

        foreach (string line in lines)
        {
            string trimmed = line.TrimEnd('\r');

            if (!inside)
            {
                inside = trimmed.StartsWith(panel, Ordinal);
                continue;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith("  ", Ordinal))
            {
                if (rows.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (trimmed.StartsWith("district", Ordinal) || trimmed.StartsWith("---", Ordinal))
            {
                continue;
            }

            rows.Add(trimmed);
        }

        Assert.True(rows.Count > 0, $"the panel '{panel}' has no data rows. Its shape has moved.");

        return rows;
    }

    private static string[] Cells(string row) =>
        row.Split("  ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>The instance count off one row of the shortfall panel.</summary>
    private static long Instances(string report, string label)
    {
        string line = report.Split('\n').FirstOrDefault(at => at.StartsWith(label, Ordinal))
            ?? throw new InvalidOperationException(
                $"the report has no line starting '{label}'. Its shape has moved.");

        return Number(Cells(line)[1]);
    }

    private static long Number(string cell) =>
        long.Parse(cell.Replace(",", string.Empty, Ordinal), CultureInfo.InvariantCulture);
}
