using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// Milestone 10 task 7 — <c>--money</c>, the tenth runner mode and the milestone's picture.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is <see cref="The_two_aggregates_are_two_rows_and_only_one_of_them_moves"/></b>,
/// because it holds <c>01 §5.1</c>'s requirement in the instrument that has to satisfy it: the money
/// supply and the treasury are different bills, so a picture that showed one would hide the one the
/// endgame turns on. Everything else here is a refusal or a shape.
/// </para>
/// <para>
/// ⚠ <b>This mode refuses where <c>--evidence</c> prints, and the difference is worth stating.</b>
/// Milestone 6 chose a legible absence — an empty trail under a heading naming the file that fills
/// it. That works because an empty trail is <em>visibly</em> empty. A balance sheet over a city with
/// no money is not: every row is zero, <c>supply == held</c> is <b>true</b>, and the report says money
/// is conserved. ***A conservation identity that holds vacuously reads exactly like one that holds***,
/// so the absence here cannot be made legible and the input is refused instead.
/// </para>
/// </remarks>
public sealed class MoneyDumpTests
{
    private const string Population = "2000";

    /// <summary>
    /// Four Days, which is what it takes for the circuit to be a table rather than a row.
    /// </summary>
    /// <remarks>
    /// The shipped circuit sweeps once a Day, and the first reading is the founding — so at one Day
    /// the table has two rows and the *trend* the picture exists to show is a single difference.
    /// </remarks>
    private const string Ticks = "8192";

    /// <summary>
    /// <c>01 §5.1</c>'s two bills, as two rows, one of which is flat.
    /// </summary>
    /// <remarks>
    /// <b>Both halves are asserted and neither alone would do.</b> A picture whose supply row was a
    /// second copy of the treasury's would pass an assertion that the treasury moved; a picture of a
    /// city where nothing happened would pass an assertion that the supply is flat. Together they say
    /// these are two different numbers about a city in which money moved.
    /// </remarks>
    [Fact]
    public void The_two_aggregates_are_two_rows_and_only_one_of_them_moves()
    {
        string report = Dump("taxed.toml");

        (long supplyFirst, long supplyLast) = Ends(report, "supply");
        (long treasuryFirst, long treasuryLast) = Ends(report, "  treasury");

        Assert.True(supplyFirst > 0, "the populator endowed nobody, so the picture shows nothing.");
        Assert.Equal(supplyFirst, supplyLast);

        Assert.Equal(0, treasuryFirst);
        Assert.True(treasuryLast > 0, "the treasury never received anything.");
    }

    /// <summary>The identity is printed, and it is printed as the two sides it was reached from.</summary>
    /// <remarks>
    /// <c>Invariant.MoneyIsConserved</c> reports a difference; this prints the two numbers the
    /// difference came from, which is what a failing assertion cannot show.
    /// </remarks>
    [Fact]
    public void It_prints_the_conservation_identity_rather_than_asserting_it()
    {
        string report = Dump("taxed.toml");

        Assert.Contains("supply == held:", report, Ordinal);
        Assert.Contains("Conserved.", report, Ordinal);
        Assert.DoesNotContain("supply != held", report, Ordinal);
    }

    /// <summary>
    /// The circuit's gross figures are far larger than its net, in both directions.
    /// </summary>
    /// <remarks>
    /// <b>This is the picture's reason for existing in one assertion.</b> The shipped circuit moves
    /// tens of thousands each way per sweep and leaves the treasury holding tens; a netted column
    /// would print a city that barely taxes, and the two gross columns print a city that taxes heavily
    /// and pays it nearly all back. The report has to be able to tell them apart.
    /// </remarks>
    [Fact]
    public void The_circuit_prints_both_directions_and_the_gross_dwarfs_the_net()
    {
        string report = Dump("taxed.toml");

        Assert.Contains("What moved", report, Ordinal);

        (long collected, long paid) = OverTheRun(report);
        (_, long treasury) = Ends(report, "  treasury");

        Assert.True(collected > 0, "nothing was ever collected.");
        Assert.True(paid > 0, "nothing was ever paid out.");
        Assert.Equal(treasury, collected - paid);
        Assert.True(
            collected > treasury * 10,
            $"the circuit moved {collected} to leave {treasury}; a net would have said as much.");
    }

    /// <summary>
    /// The three named holders are indented under <c>held</c>, and the residue is empty.
    /// </summary>
    [Fact]
    public void The_balance_sheet_decomposes_and_the_residue_is_empty()
    {
        string report = Dump("taxed.toml");

        (_, long held) = Ends(report, "held");
        (_, long treasury) = Ends(report, "  treasury");
        (_, long households) = Ends(report, "  households");
        (_, long businesses) = Ends(report, "  businesses");
        (_, long elsewhere) = Ends(report, "  elsewhere");

        Assert.Equal(0, elsewhere);
        Assert.Equal(held, treasury + households + businesses);
    }

    /// <summary>
    /// A Ruleset with no <c>[[policy]]</c> is refused, and the complaint names the file that works.
    /// </summary>
    /// <remarks>
    /// <c>minimal.toml</c> has no circuit at all, so the flow half would be a column of zeroes. That
    /// reads as a broken sweep rather than as a file that authors no circuit — <c>--zones</c>'
    /// polarity, and the refusal every picture but <c>--evidence</c> makes.
    /// </remarks>
    [Fact]
    public void It_refuses_a_ruleset_with_no_circuit()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"));

        Assert.Equal(3, code);
        Assert.Contains("no [[policy]]", report, Ordinal);
        Assert.Contains("rulesets/taxed.toml", report, Ordinal);
    }

    /// <summary>The mode needs a Ruleset, because a circuit is content.</summary>
    [Fact]
    public void It_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--money", "--ticks", Ticks], out Options? _, out string? complaint));

        Assert.Contains("--money needs --ruleset", complaint!, Ordinal);
    }

    /// <summary>A recorded session and a dump that populates its own world disagree.</summary>
    [Fact]
    public void It_refuses_a_log()
    {
        Assert.False(
            Options.TryParse(
                ["--money", "--ruleset", Ruleset("taxed.toml"), "--log", "session.borough"],
                out Options? _,
                out string? complaint));

        Assert.Contains("--money and --log disagree", complaint!, Ordinal);
    }

    /// <summary>Each picture builds its own world, so two of them are refused.</summary>
    [Fact]
    public void It_refuses_a_second_picture()
    {
        Assert.False(
            Options.TryParse(
                ["--money", "--evidence", "--ruleset", Ruleset("taxed.toml")],
                out Options? _,
                out string? complaint));

        Assert.Contains("Ask for one", complaint!, Ordinal);
    }

    /// <summary>
    /// A census rides a run and this is a picture, so the flag is refused rather than ignored.
    /// </summary>
    /// <remarks>
    /// The hole slice 10 left and <c>--series</c> found. ⚠ <b>It bites harder here than anywhere</b>:
    /// this mode keeps a census of its own, so a <c>--census</c> accepted and ignored would be a flag
    /// silently doing nothing in the one picture whose whole content comes from the thing it names.
    /// </remarks>
    [Fact]
    public void It_refuses_a_census()
    {
        Assert.False(
            Options.TryParse(
                ["--money", "--ruleset", Ruleset("taxed.toml"), "--census"],
                out Options? _,
                out string? complaint));

        Assert.Contains("picture", complaint!, Ordinal);
    }

    private const StringComparison Ordinal = StringComparison.Ordinal;

    /// <summary>The first and last columns of one balance-sheet row.</summary>
    private static (long First, long Last) Ends(string report, string label)
    {
        string[] cells = Line(report, label)
            .Split("  ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return (Number(cells[1]), Number(cells[2]));
    }

    /// <summary>The two gross figures from the circuit's closing sentence.</summary>
    private static (long Collected, long Paid) OverTheRun(string report)
    {
        string[] words = Line(report, "  Over the run:")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // "Over the run: N moved to the treasury and M moved out of it — a net of K."
        return (Number(words[3]), Number(words[9]));
    }

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
                ["--money", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = MoneyDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
