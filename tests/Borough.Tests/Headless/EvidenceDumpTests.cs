using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// Milestone 6 task 5: <c>--evidence</c>, the ninth runner mode.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is the pair</b> —
/// <see cref="The_two_rulesets_differ_in_the_why_column_and_in_nothing_else"/> — because it holds the
/// claim <c>rulesets/diagnosed.toml</c>'s own header makes, in the instrument that shows it: the same
/// city, condemned in the same places at the same Ticks, with one column filled in. Everything else
/// here is a refusal or a shape.
/// </para>
/// <para>
/// ⚠ <b>The mode does not refuse a Ruleset with no chain, and a test holds that open on purpose.</b>
/// Every other picture refuses an input it cannot demonstrate; this one prints the gap under a heading
/// naming the file that fills it. That is a decision rather than an omission, so
/// <see cref="A_ruleset_that_names_no_condition_is_printed_rather_than_refused"/> exists to fail on the
/// day somebody tidies it into a refusal.
/// </para>
/// </remarks>
public sealed class EvidenceDumpTests
{
    /// <summary>The golden fixture's population, which is what every measurement here was taken at.</summary>
    private const string Population = "4000";

    /// <summary>
    /// One Day, which is where the trail saturates.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Measured, and shorter does not work.</b> The trail holds 256 and the fixture condemns 187
    /// by Tick 1,024 — so at half a Day the aggregate is <b>empty</b>, and the panel that exists to
    /// show ***attribution decays to magnitude*** would show nothing decaying. At 2,048 it is 256
    /// retained and 76 folded away. ***A demonstration of a cap has to be run past the cap.***
    /// </remarks>
    private const string Ticks = "2048";

    /// <summary>
    /// <b>The same city, condemned in the same places, with one column filled in.</b>
    /// </summary>
    /// <remarks>
    /// <c>diagnosed.toml</c> is <c>minimal.toml</c> plus one <c>on_fail</c> key and one reporting
    /// terminal. A terminal is never evaluated — <c>RuleEngine.Descend</c> returns on
    /// <c>IsTerminal</c> before calling <c>Check</c> — so it rescues nothing and changes no behaviour,
    /// and the two runs must agree on every count. <b>Both halves are asserted</b>: an assertion that
    /// the conditions appear would pass over a file that also changed the city, and an assertion that
    /// the counts match would pass over a file that changed nothing at all.
    /// </remarks>
    [Fact]
    public void The_two_rulesets_differ_in_the_why_column_and_in_nothing_else()
    {
        string bare = Dump("minimal.toml");
        string diagnosed = Dump("diagnosed.toml");

        Assert.Contains("0 of 256 retained entries name the condition", bare, Ordinal);
        Assert.Contains("256 of 256 retained entries name the condition", diagnosed, Ordinal);

        Assert.DoesNotContain("disrepair", bare, Ordinal);
        Assert.Contains("disrepair", diagnosed, Ordinal);

        Assert.Equal(Headline(bare), Headline(diagnosed));

        // The Tick and Lot of the retained rows, in order. Same Lots condemned on the same Ticks is
        // a far stronger statement than the same totals: a file that changed the city would keep the
        // count and move the identities, and a total cannot see that.
        //
        // The count is asserted first, because two EMPTY lists are equal and that is exactly what a
        // change to the report's layout would produce here -- the strongest assertion in this file is
        // also the one that fails silently into vacuity.
        Assert.Equal(12, Rows(diagnosed).Length);
        Assert.Equal(Rows(bare), Rows(diagnosed));
    }

    /// <summary>
    /// <b>The aggregate keeps the count and loses the identity, and the panel shows both halves.</b>
    /// </summary>
    /// <remarks>
    /// The milestone's signature claim. The aggregate row carries a count above one and dashes in
    /// every identity column, in the same table as the entries that still have theirs — which is
    /// <c>CondemnationTrailTable</c> making slot 0 an entry rather than a special case, rendered.
    /// </remarks>
    [Fact]
    public void The_aggregate_keeps_the_count_and_drops_the_identity()
    {
        string report = Dump("diagnosed.toml");
        string row = Line(report, "  aggregate");

        Assert.Contains("—", row, Ordinal);
        Assert.DoesNotContain("dwelling", row, Ordinal);
        Assert.DoesNotContain("disrepair", row, Ordinal);

        int folded = int.Parse(
            row.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1], CultureInfo.InvariantCulture);

        Assert.True(
            folded > 1,
            $"the aggregate holds {folded} condemnations, so the trail has not overflowed and the "
            + "panel that exists to show attribution decaying to magnitude shows nothing decaying. "
            + $"Run longer than {Ticks} Ticks or condemn faster.");
    }

    /// <summary>
    /// <b>The assembled Building panel recomputes a maximum nothing stores.</b>
    /// </summary>
    /// <remarks>
    /// The panel is worth a test because it is the only place a reader meets
    /// <c>BuildingEvidence.Pressure</c>, and because the two Rules it shows fail in the two different
    /// ways the engine distinguishes: <c>upkeep</c> is short of an <em>input</em> and accumulates
    /// pressure, <c>restock</c> is out of <em>space</em> and does not. ***A full Bin is what a
    /// well-supplied Building looks like***, so a panel showing both at once is the distinction on
    /// screen.
    /// </remarks>
    [Fact]
    public void The_building_panel_shows_pressure_and_which_failure_starts_the_clock()
    {
        string report = Dump("diagnosed.toml");

        Assert.Contains("## One Building, assembled", report, Ordinal);
        Assert.Contains("Failure pressure", report, Ordinal);

        string starved = Line(report, "  upkeep");
        string full = Line(report, "  restock");

        Assert.Contains("supply", starved, Ordinal);
        Assert.Contains("disrepair", starved, Ordinal);

        Assert.Contains("space", full, Ordinal);
        Assert.EndsWith("0", full.TrimEnd(), Ordinal);
    }

    /// <summary>
    /// <b>A Ruleset that names no condition is printed rather than refused.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The one place this mode departs from every picture before it</b>, and the test exists so
    /// that the departure cannot be tidied away by somebody applying <c>--traffic</c>'s polarity
    /// uniformly. That mode refuses because its two panels would be <em>identical</em>; here the trail
    /// is fully populated and one column is dashes under a heading saying which file fills it.
    /// ***An instrument that refuses to show a gap is an instrument that cannot report one.***
    /// </remarks>
    [Fact]
    public void A_ruleset_that_names_no_condition_is_printed_rather_than_refused()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"));

        Assert.Equal(0, code);
        Assert.Contains("NONE OF THEM DO", report, Ordinal);
        Assert.Contains("diagnosed.toml", report, Ordinal);
    }

    /// <summary>
    /// <b>A Ruleset that condemns nothing is refused, because an empty trail is uninterpretable.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>--zones</c>' polarity, and the distinction from the test above is the whole of the mode's
    /// refusal policy: a trail with <b>no entries</b> says nothing about anything, where a trail whose
    /// entries lack one field says exactly what is missing.
    /// </para>
    /// <para>
    /// <b>The fixture is minimal.toml with one line deleted</b>, because no shipped Ruleset declines
    /// nothing — all four set <c>condemn_after</c>. Deleting the key rather than writing a file from
    /// scratch keeps the input a city that populates, so what is under test is the refusal and not
    /// whether a hand-written stub can generate a world.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ruleset_that_condemns_nothing_is_refused()
    {
        (int code, string report) = Run(Without("condemn_after = 4"), citizens: "1000", ticks: "64");

        Assert.Equal(3, code);
        Assert.Contains("condemn_after", report, Ordinal);
        Assert.Contains("diagnosed.toml", report, Ordinal);
    }

    /// <summary>minimal.toml with one line removed, written where the runner can load it.</summary>
    private static string Without(string line)
    {
        string text = File.ReadAllText(Ruleset("minimal.toml"));

        Assert.Contains(line, text, Ordinal);

        string path = Path.Combine(Path.GetTempPath(), $"borough-evidence-{line.GetHashCode(Ordinal)}.toml");

        File.WriteAllText(path, text.Replace(line, string.Empty, Ordinal));

        return path;
    }

    /// <summary>Without a Ruleset there is no decline, so the mode refuses at the command line.</summary>
    [Fact]
    public void It_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--evidence"], out Options? _, out string? complaint));

        Assert.Contains("--evidence needs --ruleset", complaint!, Ordinal);
    }

    /// <summary>
    /// A census rides a run and this is a picture, so the two flags are refused rather than ignored.
    /// </summary>
    /// <remarks>
    /// The hole slice 10 left under <c>--zones</c> and <c>--series</c> found: a flag accepted and then
    /// ignored is worse than one refused, because an absent census reads as a census with nothing in
    /// it. Asserted here so the ninth mode does not reopen it.
    /// </remarks>
    [Fact]
    public void It_refuses_a_census()
    {
        Assert.False(
            Options.TryParse(
                ["--evidence", "--ruleset", Ruleset("diagnosed.toml"), "--census"],
                out Options? _,
                out string? complaint));

        Assert.Contains("picture", complaint!, Ordinal);
    }

    private const StringComparison Ordinal = StringComparison.Ordinal;

    /// <summary>The trail's summary line, which is every count the two runs must agree on.</summary>
    private static string Headline(string report) =>
        report.Split('\n').FirstOrDefault(line => line.Contains("Buildings condemned.", Ordinal))
        ?? throw new InvalidOperationException("the report has no trail summary line.");

    /// <summary>The Tick and Lot of every retained row printed, in order.</summary>
    private static string[] Rows(string report) =>
    [
        .. report.Split('\n')
            .Where(line => line.StartsWith("       ", Ordinal) && line.Contains("dwelling", Ordinal))
            .Select(line => string.Join(
                ' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2))),
    ];

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
                ["--evidence", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = EvidenceDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
