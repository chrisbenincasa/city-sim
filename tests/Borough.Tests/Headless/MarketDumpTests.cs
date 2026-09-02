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
/// ✅ <b><see cref="The_price_moves_where_there_is_a_glut_and_holds_at_the_ceiling_where_there_is_not"/>
/// REPLACED A TEST THAT ASSERTED A DEFECT.</b> Until <c>adr/0171</c> no price had ever moved on any
/// world, because the cover was read off the Pool's own Bin and <c>adr/0139</c> had emptied it
/// (<c>plans/0044</c> <b>F50</b>). ⚠ <b>The defect and a correctly scarce market print the SAME
/// COLUMN</b> — a price flat at the import ceiling — so the replacement runs on two Rulesets and
/// asserts the difference between them, which is the only thing neither state can fake.
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
    /// <summary>
    /// Four thousand Citizens, and the figure is the SEPARATION between the two worlds rather than
    /// a city size.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>TWO THOUSAND UNTIL <c>plans/0053</c>, WHERE THE TWO WORLDS MET AND STOPPED DIFFERING.</b>
    /// <see cref="The_price_moves_where_there_is_a_glut_and_holds_at_the_ceiling_where_there_is_not"/>
    /// asserts a DIFFERENCE, so it needs a population at which one world is over a Day's cover and the
    /// other is under it. Occupancy started deriving from the ground and 2,000 became the crossing
    /// point: the glutted world read <b>652 of stock against a draw of 699</b> — under cover by 7% —
    /// and printed the scarce world's flat column.
    /// </para>
    /// <para>
    /// ⚠ <b>Swept rather than nudged, at the shipped <see cref="Ticks"/>.</b> Stock against draw in
    /// District 1, over-supplied then provisioned: <b>1,000</b> → 656/239 and 92/129 (both glutted
    /// somewhere, so the scarce half fails); <b>2,000</b> → 652/699 and 188/622 (both flat);
    /// <b>3,000</b> → 1,612/880 and 284/982; <b>4,000</b> → 3,024/1,725 and 284/1,830;
    /// <b>6,000</b> → 4,708/2,186 and 376/4,037; <b>8,000</b> → 5,068/3,010 and 568/4,795.
    /// ***The band is 3,000 upward and 4,000 sits inside it with the widest margin on both sides***
    /// — 1.75× cover in the glut, 0.16× in the scarcity, and eight price moves rather than four.
    /// </para>
    /// <para>
    /// ⚠ <b>Time separates these two worlds as well, and that is why the horizon is NOT the lever.</b>
    /// Run either at 98,304 Ticks and it gluts — the provisioned city reaches 100 → 21 — because
    /// stock accumulates while the draw decays. ***A contrast that only holds at one moment is not a
    /// contrast between the files***, so the fixture moves the population, which is a property of the
    /// city, and leaves the clock where <see cref="Ticks"/> argues it belongs.
    /// </para>
    /// </remarks>
    private const string Population = "4000";

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
    /// ✅ The price moves where the city has a glut and holds at the ceiling where it has not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two worlds, because the finding is the DIFFERENCE and neither half means anything alone.</b>
    /// <c>rulesets/oversupplied.toml</c> is <c>rulesets/provisioned.toml</c> with two tier-1 keys
    /// deleted (<c>adr/0170</c> condition 4), so it raises <b>32 sellers a District where the other
    /// raises 3</b> at <see cref="Population"/> — and ***the diff between the files is the whole
    /// demonstration***, for the price exactly as for the shop's life. ⚠ <b>Those two counts are a
    /// reading and not a property of the files</b>; they were 10 against 2 before occupancy derived
    /// from the ground, and they move with the population and the generator both.
    /// </para>
    /// <para>
    /// ⚠ <b>The scarce half asserts the CAUSE and not merely the flatness.</b> Every row holds no more
    /// than a Day's cover, which is why the ceiling is the honest price there. ***A row over a Day's
    /// cover sitting at its ceiling is the old defect returning***, and it is the one shape this has to
    /// go red on — a test that asserted flatness alone would have passed both before and after
    /// <c>adr/0171</c>.
    /// </para>
    /// <para>
    /// 🔴 <b>A ROW WITH NO DRAW IS SKIPPED AND COUNTED, added at <c>plans/0053</c>.</b> The cover
    /// assertion divides by a demand, and a District can hold sellers and no consumers — this one
    /// grew such a row the moment occupancy started dividing the ground, District 3 standing with
    /// 2 sellers, 188 sundries and a rate of nothing. ⚠ <b>On a zero draw the sentence
    /// <c>stock ≤ rate/Day</c> stops meaning "under a Day's cover" and starts meaning "the stock is
    /// zero"</b>, which is a different claim and a false one. ***A degenerate denominator is named,
    /// never quietly asserted against*** — and the count is asserted positive afterwards, because a
    /// skip that could take every row is a vacuous test wearing a green tick.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_price_moves_where_there_is_a_glut_and_holds_at_the_ceiling_where_there_is_not()
    {
        string scarce = Dump("provisioned.toml");
        int drawn = 0;

        foreach (string row in Rows(scarce, "Where the market is"))
        {
            string[] cells = Cells(row);

            // 🔴 A ROW WITH NO DRAW HAS NO DAY'S COVER, and the comparison below is not defined on it
            // (plans/0053). `stock <= rate/Day` reads "less than one Day of demand"; where the demand
            // is ZERO that sentence becomes "the stock is zero", which is a different claim and a
            // false one -- a District can hold sellers and no consumers, and provisioned.toml grew
            // exactly such a row when occupancy started dividing the ground: District 3 stands with
            // 2 sellers, 188 sundries and a draw of nothing. ***A degenerate denominator is skipped
            // and counted, never quietly asserted against.***
            if (Number(cells[^1]) <= 0)
            {
                continue;
            }

            drawn++;

            Assert.True(
                Number(cells[^3]) <= Number(cells[^1]),
                $"'{row}' holds more than a Day's cover and the price is still flat, which is the "
                + "cover being taken from the wrong Bin again (adr/0171).");
        }

        Assert.True(
            drawn > 0,
            "no row in this market has a draw at all, so the cover assertion above ran on nothing "
            + "and this half of the test is vacuous. The scarce world has stopped consuming.");

        foreach (string row in Rows(scarce, "What the price did"))
        {
            string[] cells = Cells(row);

            Assert.Equal(cells[2], cells[3]);
            Assert.Equal("0", cells[^1]);
        }

        Assert.Contains("the mechanism rather than a defect", scarce, Ordinal);

        string glut = Dump("oversupplied.toml");

        long moves = 0;
        long fell = 0;

        foreach (string row in Rows(glut, "What the price did"))
        {
            string[] cells = Cells(row);

            moves += Number(cells[^1]);

            if (Number(cells[3]) < Number(cells[2]))
            {
                fell++;
            }
        }

        Assert.True(
            moves > 0,
            "no price moved in the over-supplied city, so the tatonnement is not running at all.");

        Assert.True(
            fell > 0,
            "prices moved and none of them FELL under a glut, which is the wrong direction and is "
            + "worse than not moving.");
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
