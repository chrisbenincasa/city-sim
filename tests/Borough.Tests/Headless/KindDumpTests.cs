using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// <c>--kinds</c> — the standing city counted by Building kind.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is
/// <see cref="A_zone_rule_admitting_a_bit_no_lot_carries_is_named_as_a_dead_rule"/></b>, because that
/// is the one state no other instrument in this repository can report. A Zone Rule naming an
/// unpainted bit loads clean, sweeps for ever and raises nothing, and every counter in
/// <c>--census</c> reads exactly as it would for a rule that merely found no vacant Lot. ⚠ <b>The
/// fixture is written rather than shipped</b>, on <c>BusinessDumpTests</c>' precedent: no shipped
/// Ruleset has a dead rule in it, and adding one to <c>rulesets/</c> would put a defect in the
/// demonstration set to keep a test company.
/// </para>
/// <para>
/// <b><see cref="The_used_share_counts_both_kinds_of_tenant"/> asserts an arithmetic identity rather
/// than a value</b>, and it exists because the first cut of that column got it wrong: it divided
/// Households alone by a ceiling that counts Households <em>and</em> Businesses
/// (<c>adr/0147</c>), so a row of 24 shops each holding a trade printed <c>used 0%</c>. ***A share
/// whose halves are denominated differently is <c>plans/0012</c> Cause 5 inside one expression***,
/// and the identity catches it where a spot value would not.
/// </para>
/// <para>
/// ⚠ <b>No test here asserts a MIX.</b> Which kinds win a contested zone is a property of the
/// sampling and of what declines, and pinning it would make this file a golden baseline for a
/// quantity nothing has ratified. What is asserted is that the table can be read, that its columns
/// agree with each other, and that the three reasons a kind stands nowhere are told apart.
/// </para>
/// </remarks>
public sealed class KindDumpTests
{
    private const StringComparison Ordinal = StringComparison.Ordinal;

    /// <summary>Long enough for the sweep to raise a second kind, short enough to stay cheap.</summary>
    private const string Ticks = "8192";

    private const string Population = "2000";

    /// <summary>
    /// The rows of the table sum to the Buildings the header counted.
    /// </summary>
    /// <remarks>
    /// <b>The identity the whole dump rests on.</b> <c>KindDump.Standing</c> drops a Building whose
    /// kind is outside the Ruleset in force — a reload can retire a kind under a Building still
    /// standing — and a silent drop is how a per-kind table stops adding up to the city. This is what
    /// notices.
    /// </remarks>
    [Fact]
    public void The_rows_sum_to_the_buildings_the_header_counted()
    {
        string report = Dump("banded.toml");

        int header = Header(report, "Buildings");
        int summed = Rows(report, Before).Sum(row => row.Standing);

        Assert.Equal(header, summed);
    }

    /// <summary>
    /// The populator raises one kind, and every other kind in the file arrives later or not at all.
    /// </summary>
    /// <remarks>
    /// <c>SyntheticCity.DwellingKind</c> is a hardcoded <c>1</c> whose own remark says *"the kind this
    /// populator raises, and the only one it knows"*. ***Every measurement ever taken through this
    /// runner was taken on a city of one kind***, which is the fact this dump exists to make
    /// impossible to miss, and it is asserted here so that a generator that learns a second kind
    /// fails this test rather than quietly changing what every other reading means.
    /// </remarks>
    [Fact]
    public void The_populators_city_holds_exactly_one_kind()
    {
        List<Row> before = Rows(Dump("banded.toml"), Before);

        Assert.True(before.Count > 1, "banded.toml declares a second kind; the fixture is wrong.");
        Assert.Single(before, row => row.Standing > 0);
    }

    /// <summary>
    /// A Zone Rule admitting a bit no Lot in the world carries is named as a dead rule.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The violation is written and watched to fire.</b> <c>minimal.toml</c>'s one Zone Rule
    /// moves from bit 0, which the generator paints on every block, to bit 5, which nothing paints —
    /// and <c>RulesetLoader</c> accepts it, because it can check a bit is inside
    /// <c>LotTable.ZoneBits</c> and cannot check that anything paints it. ⚠ <b>The Ruleset still
    /// LOADS</b>, which is the half worth stating: this is not a refusal arriving late, it is a
    /// silence this dump breaks.
    /// </remarks>
    [Fact]
    public void A_zone_rule_admitting_a_bit_no_lot_carries_is_named_as_a_dead_rule()
    {
        // Bit 5 rather than bit 2, so that the assertion cannot pass by accident on a world whose
        // generator has since learned to paint one more bit than it does today.
        string text = File.ReadAllText(Ruleset("minimal.toml"))
            .Replace("zone          = 0", "zone          = 5", Ordinal);

        string path = Path.Combine(Path.GetTempPath(), $"borough-deadzone-{Guid.NewGuid():N}.toml");

        try
        {
            File.WriteAllText(path, text);

            (int code, string report) = Run(path);

            Assert.True(code == 0, report);
            Assert.Contains("DEAD RULE", report, Ordinal);
            Assert.Contains("'housing' raises 'dwelling' on bit 5", report, Ordinal);

            // The populator builds kind 1 whatever the Zone Rules say, so the dwelling still STANDS.
            // That is exactly what hid this case in the first cut, and asserting it keeps the two
            // sections honest about asking different questions.
            Assert.DoesNotContain("dwelling: ", report, Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A service kind standing nowhere is reported as awaiting a command, not as unreachable content.
    /// </summary>
    /// <remarks>
    /// <b>The distinction is the whole reason the footer exists.</b> A school with no Zone Rule is
    /// <c>adr/0032</c> working — the city does not site its own schools — and a warehouse with no
    /// Zone Rule is content nothing can ever build. ***They are the same zero and opposite
    /// diagnoses.***
    /// </remarks>
    [Fact]
    public void A_service_kind_standing_nowhere_is_not_reported_as_unreachable_content()
    {
        string report = Dump("schooled.toml");

        Assert.Contains("school: no Zone Rule raises it, which is correct for a service", report, Ordinal);
        Assert.DoesNotContain("school: NO ZONE RULE RAISES IT", report, Ordinal);
    }

    /// <summary>
    /// <c>used</c> is holds plus trades over the ceiling, on every row that has a ceiling.
    /// </summary>
    /// <remarks>
    /// <b>An identity over the printed columns</b>, so it holds for every row of every world rather
    /// than for one figure somebody chose. <c>adr/0147</c>: one ceiling counts both kinds of tenant,
    /// so a numerator holding one of them is wrong however plausible the percentage looks.
    /// </remarks>
    [Fact]
    public void The_used_share_counts_both_kinds_of_tenant()
    {
        List<Row> rows = Rows(Dump("minimal.toml"), After);

        Assert.Contains(rows, row => row.Ceiling > 0);

        foreach (Row row in rows.Where(row => row.Ceiling > 0))
        {
            Assert.Equal(
                ((row.Holds + row.Trades) * 100) / row.Ceiling,
                int.Parse(row.Used.TrimEnd('%'), CultureInfo.InvariantCulture));
        }
    }

    /// <summary>A ceiling is never smaller than what is standing in it.</summary>
    /// <remarks>
    /// <b>The invariant a derived occupancy has to keep</b>, and the cheapest place to notice that a
    /// Building is holding more tenancies than its ground allows — which is what
    /// <c>adr/0068</c>'s eviction exists to prevent and what a retuned <c>[capacity]</c> rate would
    /// cause.
    /// </remarks>
    [Fact]
    public void Nothing_holds_more_tenancies_than_its_ground_allows()
    {
        foreach (Row row in Rows(Dump("minimal.toml"), After))
        {
            Assert.True(
                row.Holds + row.Trades <= row.Ceiling,
                $"{row.Kind} holds {row.Holds} + {row.Trades} against a ceiling of {row.Ceiling}.");
        }
    }

    /// <summary>It refuses without a Ruleset rather than printing an empty table.</summary>
    [Fact]
    public void It_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--kinds", "--ticks", "100"], out _, out string? complaint));

        Assert.Contains("A Building kind is content", complaint!, Ordinal);
    }

    /// <summary>It refuses to share a run with another picture.</summary>
    [Fact]
    public void It_refuses_to_share_a_run_with_another_picture()
    {
        Assert.False(
            Options.TryParse(
                ["--kinds", "--zones", "--ruleset", Ruleset("minimal.toml")],
                out _,
                out string? complaint));

        Assert.Contains("each picture builds its own world", complaint!, Ordinal);
    }

    /// <summary>The usage names it, and names what it is for.</summary>
    [Fact]
    public void The_usage_names_it()
    {
        Assert.Contains("--kinds", Options.Usage, Ordinal);
        Assert.Contains("BY BUILDING KIND", Options.Usage, Ordinal);
    }

    private const string Before = "## The standing city — the populator's, before any sweep";
    private const string After = "## The standing city — after";

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--kinds", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = KindDump.Run(options!, writer);

        return (code, writer.ToString());
    }

    private static string Dump(string ruleset)
    {
        (int code, string report) = Run(Ruleset(ruleset));

        Assert.True(code == 0, report);

        return report;
    }

    /// <summary>One figure out of the header line, by the word that follows it.</summary>
    private static int Header(string report, string noun)
    {
        string[] words = report
            .Split('\n')[1]
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < words.Length; i++)
        {
            if (words[i] == noun)
            {
                return int.Parse(words[i - 1], CultureInfo.InvariantCulture);
            }
        }

        Assert.Fail($"the header names no '{noun}': {report.Split('\n')[1]}");
        return 0;
    }

    /// <summary>The data rows of one standing-city table, between its rule and the note after it.</summary>
    private static List<Row> Rows(string report, string panel)
    {
        List<Row> rows = [];
        bool inside = false;

        foreach (string line in report.Split('\n'))
        {
            if (line.StartsWith(panel, Ordinal))
            {
                inside = true;
                continue;
            }

            if (!inside)
            {
                continue;
            }

            string[] cells = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // The column header, then nine cells a row, then the note — which starts with a
            // backtick and is the one line here that is prose.
            if (cells.Length != 9 || cells[0] == "id")
            {
                if (rows.Count > 0)
                {
                    break;
                }

                continue;
            }

            rows.Add(new Row(
                cells[1],
                int.Parse(cells[2], CultureInfo.InvariantCulture),
                int.Parse(cells[3], CultureInfo.InvariantCulture),
                int.Parse(cells[4], CultureInfo.InvariantCulture),
                int.Parse(cells[5], CultureInfo.InvariantCulture),
                int.Parse(cells[6], CultureInfo.InvariantCulture),
                int.Parse(cells[7], CultureInfo.InvariantCulture),
                cells[8]));
        }

        Assert.NotEmpty(rows);
        return rows;
    }

    private readonly record struct Row(
        string Kind,
        int Standing,
        int Shells,
        int Floor,
        int Ceiling,
        int Holds,
        int Trades,
        string Used);
}
