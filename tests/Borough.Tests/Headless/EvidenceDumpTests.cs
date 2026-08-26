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
    /// <para>
    /// The panel is worth a test because it is the only place a reader meets
    /// <c>BuildingEvidence.Pressure</c>, and because the two Rules it shows fail in the two different
    /// ways the engine distinguishes: <c>upkeep</c> is short of an <em>input</em> and accumulates
    /// pressure, <c>restock</c> is out of <em>space</em> and does not. ***A full Bin is what a
    /// well-supplied Building looks like***, so a panel showing both at once is the distinction on
    /// screen.
    /// </para>
    /// <para>
    /// ⚠ <b>THERE ARE NOW THREE <c>restock</c> ROWS AND THE PANEL CANNOT SAY WHOSE.</b>
    /// <c>adr/0141</c> made <c>sundries</c> the tenant's, so a dwelling holding three Households runs
    /// three <c>restock</c>s and three <c>consume</c>s — and <c>RuleEvidence</c> names no subject, which
    /// that ADR already recorded as *"a field, not a redesign"*. Until that field lands
    /// (<c>plans/0040</c> task 3) this test takes the first row it can find in each state rather than
    /// the first row with each name. ***The claim is unchanged and it is the panel that got harder to
    /// read***, which is the honest shape for an assertion over an instrument that is mid-repair.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_building_panel_shows_pressure_and_which_failure_starts_the_clock()
    {
        string report = Dump("diagnosed.toml");

        Assert.Contains("## One Building, assembled", report, Ordinal);
        Assert.Contains("Failure pressure", report, Ordinal);

        string starved = Line(report, "  upkeep");
        string full = Rows(report, "  restock").FirstOrDefault(row => row.Contains("space", Ordinal))
            ?? throw new InvalidOperationException(
                "no restock row is out of space, so the panel is no longer showing the distinction "
                + "this test exists for. Every restock row:\n"
                + string.Join('\n', Rows(report, "  restock")));

        Assert.Contains("supply", starved, Ordinal);
        Assert.Contains("disrepair", starved, Ordinal);

        // The claim is that a Rule stopped for SPACE carries no pressure -- RuleEngine.Stop clears
        // StarvedSince for every blocking reason but Supply -- and it was asserted as "the row ends
        // in 0" until milestone 26 task 5 put a `waiting on` column after the missed count.
        //
        // ⚠ Re-anchored on the two fields TOGETHER rather than moved along by one, because
        // EndsWith("0") would pass again on any row whose last column happened to end in a zero.
        // `full:` is also the word this panel uses for a Bin that is FULL: printing `larder: sundries`
        // against an output with no headroom read as the exact opposite of the truth, and this row is
        // the only one in the suite that would have caught it.
        Assert.Matches(@"\s0\s+full: sundries$", full.TrimEnd());
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

    /// <summary>
    /// <b>The journeys panel names the silent population</b> — everybody the commute has not reached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Milestone 6 task 7. <c>CitizenTable.LastTripFate</c> is a saved column that would otherwise
    /// have had no reader outside the suite, which is 5b-bis task 6's finding exactly: ***a Census
    /// family with no reader is a family nobody can see***.
    /// </para>
    /// <para>
    /// <b>The assertion is an identity rather than a threshold, and that is what makes it a test.</b>
    /// Nobody commutes without a job, and the commute is the only Trip generator there is
    /// (<c>adr/0081</c>; <c>TripPurpose.Commanded</c> is a test affordance under <c>adr/0080</c>), so
    /// <em>never travelled</em> must be exactly the unemployed — 2,208 of 4,000 on this fixture, which
    /// is the 1,792 employed subtracted from the population. A count that merely looked plausible
    /// would survive a panel counting the wrong thing; this one does not.
    /// </para>
    /// <para>
    /// ⚠ <b>Three of the four Fates read zero and that is the world rather than the panel.</b> Nothing
    /// this runner can generate refuses a commute at the shipped fifty-minute ceiling, and
    /// <c>TripFate.Stranded</c> has no producer anywhere in the build. The zeros are asserted so that
    /// the day one of them moves, somebody has to read this note — 5b-bis task 4's precedent.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_journeys_panel_counts_everybody_the_commute_never_reached()
    {
        string report = Dump("diagnosed.toml");

        Assert.Contains("## Journeys, by how each Citizen's last one ended", report, Ordinal);

        int completed = Count(report, "  completed");
        int never = Count(report, "  never travelled");

        Assert.True(completed > 0, "nobody in this city ever finished a journey.");
        Assert.Equal(int.Parse(Population, CultureInfo.InvariantCulture), completed + never);

        Assert.Equal(0, Count(report, "  no route found"));
        Assert.Equal(0, Count(report, "  beyond commute budget"));
        Assert.Equal(0, Count(report, "  stranded"));
    }

    /// <summary>
    /// <b>The finances panel separates destitution from a world with no money, and says which.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Milestone 10 task 8. <c>CitizenEvidence</c> shipped declining <c>02 §9</c>'s finances clause
    /// because <em>"a Household with no money and a Household in a world with no money read the
    /// same"</em>, and this is the pair being told apart in the instrument that shows them.
    /// </para>
    /// <para>
    /// ⚠ <b>Neither file here is the absent case, and that is the finding.</b> Milestone 10 task 2 put
    /// a money Resource in all seven shipped Rulesets, so every Household in every shipped world holds
    /// a balance — <c>diagnosed.toml</c>'s are all empty because only <c>taxed.toml</c> states an
    /// opening balance. So what the two panels separate is <b>destitution</b> from a <b>distribution</b>,
    /// and the third reading is exercised in <c>HouseholdFinancesTests</c> against a Ruleset naming no
    /// money at all. ***A branch no shipped content reaches is still a branch content can reach.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_finances_panel_separates_destitution_from_a_world_with_no_money()
    {
        string destitute = Dump("diagnosed.toml");
        string endowed = Dump("taxed.toml");

        Assert.Contains("## Household finances", destitute, Ordinal);
        Assert.Contains("DESTITUTION rather than a", destitute, Ordinal);
        Assert.Equal(Count(destitute, "  citizens with a balance"), Count(destitute, "  holding exactly nothing"));

        // The same panel over a file that endows: balances present, and not all of them zero.
        Assert.DoesNotContain("DESTITUTION", endowed, Ordinal);
        Assert.Equal(0, Count(endowed, "  citizens reporting absent"));
        Assert.True(Count(endowed, "  highest balance") > 0, "taxed.toml endowed nobody.");
    }

    /// <summary>The trailing number on a panel line.</summary>
    private static int Count(string report, string label) => int.Parse(
        Line(report, label).Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1],
        CultureInfo.InvariantCulture);

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

    /// <summary>
    /// Every line starting <paramref name="starting"/>, because one Rule name can now name several
    /// rows — one per tenant running it (<c>adr/0141</c>).
    /// </summary>
    private static string[] Rows(string report, string starting) =>
        [.. report.Split('\n').Where(line => line.StartsWith(starting, Ordinal))];

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
