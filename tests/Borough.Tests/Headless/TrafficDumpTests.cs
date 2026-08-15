using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// That <c>--traffic</c> compares two different cities, and refuses rather than pretending when it
/// cannot.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test in this file is the control one</b>, and it is here because of a mistake
/// made twice on 2026-08-14 while measuring this very mechanism: a <c>String.Replace</c> whose needle
/// did not match produced a clean-looking sweep of identical rows that was very nearly written down
/// as a result. This picture's control is built the same way — the <c>[traffic]</c> table is stripped
/// out of the file's text and the remainder re-parsed — and here the failure is worse than a wrong
/// number, because two identical runs report <b>×1.0000</b>, which is exactly what an inert
/// volume-delay function reports on a generated city. ***A control that silently equals its treatment
/// is indistinguishable from a null result.***
/// </para>
/// <para>
/// <b>The refusals are tested because they are the only thing an operator will meet.</b> No shipped
/// Ruleset states <c>[traffic]</c> or <c>[households]</c> except <c>congested.toml</c>, which was
/// written for this mode, so both refusal branches are on the path of anybody who reaches for
/// <c>--traffic</c> with a file in hand.
/// </para>
/// </remarks>
public sealed class TrafficDumpTests
{
    /// <summary>Enough Citizens to put Vehicles on Segments, and few enough to be a fast test.</summary>
    private const string Population = "4000";

    /// <summary>Half a Day, which is what it now takes to reach the morning peak.</summary>
    /// <remarks>
    /// ⚠ <b>It was 256, and <c>adr/0101</c> made that an empty picture rather than a small one.</b>
    /// Departures used to be spread over a window that began at Tick 0, so any prefix of a run caught
    /// a slice of it. They are now anchored on each Workplace's Shift start, and the shipped band is
    /// 6–10 in-world hours — Ticks <b>512 to 853</b> of a 2,048-Tick Day — so a run of 256 Ticks stops
    /// before anybody has left the house and every Segment reads zero. ***A run length that was a
    /// sample of a window becomes a question about what time it is when the departure acquires an
    /// hour***, and the two are not the same parameter wearing one name.
    /// </remarks>
    private const string Ticks = "1024";

    /// <summary>
    /// <b>The control is genuinely a different Ruleset, and the report proves it by disagreeing with
    /// itself.</b>
    /// </summary>
    /// <remarks>
    /// The one assertion that cannot be satisfied by a broken strip. <c>congested.toml</c> is the rung
    /// where the free-flow and loaded runs are <em>measured</em> to differ — its own header carries the
    /// capacity sweep that chose it — so an equal pair of totals here means the two worlds ran on the
    /// same Ruleset, whatever the rest of the report says.
    /// </remarks>
    [Fact]
    public void The_free_flow_control_is_not_the_same_run_as_the_loaded_one()
    {
        string report = Dump("congested.toml");

        long free = Total(report, "free-flow");
        long loaded = Total(report, "loaded");

        Assert.True(
            loaded > free,
            $"the two runs came out {free} and {loaded}: the [traffic] strip produced the same "
            + "Ruleset twice, so the control is the treatment and x1.0000 means nothing");
    }

    /// <summary>
    /// <b>Congestion never makes a city cheaper to cross, and the sign is worth pinning.</b>
    /// </summary>
    /// <remarks>
    /// A volume-delay function wired backwards moves the State Hash, produces a plausible Census and
    /// draws a picture that looks like this one. The direction is the cheapest thing that separates
    /// them, and it is the reason the report prints free-flow beside loaded rather than loaded alone.
    /// </remarks>
    [Fact]
    public void A_loaded_city_is_never_quicker_to_cross_than_a_free_flowing_one()
    {
        string report = Dump("congested.toml");

        Assert.True(Total(report, "loaded") >= Total(report, "free-flow"));
    }

    /// <summary>
    /// <b>Every listed Segment costs at least free-flow, and the delay column says so.</b>
    /// </summary>
    /// <remarks>
    /// The per-Segment half of the assertion above. A total can come out right while individual rows
    /// are wrong in both directions, and the busiest-Segments table is the half a reader will quote.
    /// </remarks>
    [Fact]
    public void No_segment_is_quoted_below_its_own_free_flow_cost()
    {
        string report = Dump("congested.toml");

        var delays = report.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Contains(" T   x", StringComparison.Ordinal))
            .Select(line => line[(line.LastIndexOf('x') + 1)..])
            .Select(text => decimal.Parse(text, CultureInfo.InvariantCulture))
            .ToArray();

        Assert.NotEmpty(delays);
        Assert.All(delays, delay => Assert.True(delay >= 1m, $"a Segment was quoted at x{delay}"));
    }

    /// <summary>
    /// <b>A Ruleset with no <c>[traffic]</c> is refused, and the refusal says what to add.</b>
    /// </summary>
    /// <remarks>
    /// <c>--zones</c>' polarity: congestion is content, and a picture of quiet roads would read as a
    /// broken volume-delay function rather than as a file that declares none. The refusal prints the
    /// two tables because ⚠ <b>the state of the corpus is that most files have neither</b>, so
    /// <i>needs a [traffic] table</i> alone would leave the operator to find <c>[households]</c> on the
    /// second attempt.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_traffic_table_is_refused_rather_than_drawn_empty()
    {
        (int code, string report) = Attempt("minimal.toml");

        Assert.Equal(3, code);
        Assert.Contains("no [traffic] table", report, StringComparison.Ordinal);
        Assert.Contains("[households]", report, StringComparison.Ordinal);
        Assert.Contains("alpha_percent", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Ruleset with <c>[traffic]</c> and no <c>[households]</c> is refused too.</b>
    /// </summary>
    /// <remarks>
    /// The second refusal exists because the first does not cover it, and the resulting picture would
    /// be empty for a completely different reason: volume is <b>vehicular</b> by decision
    /// (<c>adr/0041</c>), so a city where no Household keeps a car puts nothing on any Segment however
    /// congested its Ruleset claims to be. ***An empty picture of a working mechanism is the failure
    /// mode a refusal exists to prevent.***
    /// </remarks>
    [Fact]
    public void A_ruleset_with_traffic_and_no_households_is_refused_too()
    {
        string path = Path.Combine(Path.GetTempPath(), $"borough-carless-{Guid.NewGuid():N}.toml");

        try
        {
            string text = File.ReadAllText(Ruleset("congested.toml"));
            string stripped = Without(text, "[households]");

            Assert.DoesNotContain("car_ownership_percent", stripped, StringComparison.Ordinal);
            Assert.Contains("alpha_percent", stripped, StringComparison.Ordinal);

            File.WriteAllText(path, stripped);

            (int code, string report) = Run(path);

            Assert.Equal(3, code);
            Assert.Contains("no [households]", report, StringComparison.Ordinal);
            Assert.Contains("vehicular", report, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// <b>The two panels are drawn over one lattice, so a reader may compare them cell by cell.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read off <c>--csv</c> rather than off the ASCII field, which is the only reading in which
    /// <em>same shape</em> is a structural claim rather than a claim about column alignment. Every row
    /// carries one block and <b>both</b> of its glyphs, so a panel sized from its own run would fail
    /// this by construction.
    /// </para>
    /// <para>
    /// ⚠ <b>It does not assert that any cell differs, and the reason is measured.</b> At this
    /// population the bill moves by ×1.0017 and <em>no block changes band</em>: the extra dwell is
    /// real and sub-Tick, so it changes what a crossing costs without changing how many Vehicles stand
    /// on a road at any Tick boundary. ***A per-Tick snapshot cannot see a sub-Tick delay*** — so an
    /// assertion that the panels differ would be an assertion about the population this test happens
    /// to run at, which is 5b-bis task 4's *a Budget chosen against the map is not thereby exercised
    /// by every world on it* arriving in a fifth place.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_two_panels_are_drawn_over_the_same_lattice()
    {
        string[] rows = [.. Csv("congested.toml").Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && char.IsAsciiDigit(line[0]))];

        Assert.NotEmpty(rows);

        foreach (string row in rows)
        {
            string[] fields = row.Split(',');

            Assert.Equal(4, fields.Length);
            Assert.True(IsBand(fields[2][0]), $"free-flow glyph '{fields[2]}' is not a band");
            Assert.True(IsBand(fields[3][0]), $"loaded glyph '{fields[3]}' is not a band");
        }
    }

    /// <summary>
    /// <b><c>--traffic</c> needs a Ruleset, and says which two tables it needs in it.</b>
    /// </summary>
    [Fact]
    public void The_mode_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--traffic"], out Options? _, out string? complaint));

        Assert.Contains("--traffic needs --ruleset", complaint!, StringComparison.Ordinal);
        Assert.Contains("[households]", complaint!, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>It is one picture among the others, and asking for two is refused by name.</b>
    /// </summary>
    /// <remarks>
    /// The complaint has to name <c>--traffic</c> rather than the other flag. Both are sessions, so
    /// the more general refusals below it in <c>Options</c> would fire first and describe the wrong
    /// mistake — <b>the most specific complaint wins</b> is the ordering rule <c>--commute</c>
    /// established, and this test is what holds the new row in its place.
    /// </remarks>
    [Theory]
    [InlineData("--commute")]
    [InlineData("--zones")]
    [InlineData("--roads")]
    [InlineData("--trips")]
    public void Two_pictures_at_once_are_refused_by_the_traffic_flag(string other)
    {
        Assert.False(
            Options.TryParse(
                ["--traffic", other, "--ruleset", Ruleset("congested.toml")],
                out Options? _,
                out string? complaint));

        Assert.Contains("--traffic asks for a sixth picture", complaint!, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The header states the three <c>[traffic]</c> numbers the run was priced with.</b>
    /// </summary>
    /// <remarks>
    /// <c>--commute</c>'s lesson: printing the Commute Budget beside the file's own <c>20</c> is what
    /// exposed a minute formatter that had been dropping the sub-Tick fraction in every duration it
    /// ever printed. A parameter the reader can check against the file is the cheapest instrument
    /// there is. ***A defect that only shows on a value you happen to know is a defect that hides in
    /// every value you do not.***
    /// </remarks>
    [Fact]
    public void The_header_states_the_function_it_priced_with()
    {
        string report = Dump("congested.toml");

        Assert.Contains("0.15", report, StringComparison.Ordinal);
        Assert.Contains("(v/c)^4", report, StringComparison.Ordinal);
        Assert.Contains("clamped at v/c 4.00", report, StringComparison.Ordinal);
        Assert.Contains("Little's Law", report, StringComparison.Ordinal);
    }

    private static bool IsBand(char glyph) => glyph is '.' or ':' or '#' or '@';

    /// <summary>A TOML section and its body, removed.</summary>
    private static string Without(string text, string section)
    {
        var kept = new List<string>();
        bool inside = false;

        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.TrimStart();

            if (trimmed.StartsWith('['))
            {
                inside = trimmed.StartsWith(section, StringComparison.Ordinal);
            }

            if (!inside)
            {
                kept.Add(line);
            }
        }

        return string.Join('\n', kept);
    }

    /// <summary>One of the two vehicle-Tick totals the summary prints.</summary>
    private static long Total(string report, string label)
    {
        string line = Array.Find(
            report.Split('\n'),
            each => each.TrimStart().StartsWith(label, StringComparison.Ordinal)
                && each.Contains("vehicle-Ticks", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"the dump printed no {label} total");

        string digits = line.Trim()[label.Length..].Split("vehicle-Ticks")[0].Trim();

        return long.Parse(digits, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
    }

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static string Dump(string ruleset)
    {
        (int code, string report) = Run(Ruleset(ruleset));

        Assert.Equal(0, code);

        return report;
    }

    private static string Csv(string ruleset)
    {
        Assert.True(
            Options.TryParse(
                ["--traffic", "--csv", "--ruleset", Ruleset(ruleset),
                 "--citizens", Population, "--ticks", Ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();

        Assert.Equal(0, TrafficDump.Run(options!, writer));

        return writer.ToString();
    }

    private static (int Code, string Report) Attempt(string ruleset) => Run(Ruleset(ruleset));

    private static (int Code, string Report) Run(string path)
    {
        Assert.True(
            Options.TryParse(
                ["--traffic", "--ruleset", path, "--citizens", Population, "--ticks", Ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = TrafficDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
