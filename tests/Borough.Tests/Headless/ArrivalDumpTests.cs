using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// Milestone 11 task 8: <c>--arrivals</c>, the eleventh runner mode.
/// </summary>
/// <remarks>
/// <para>
/// <b>The load-bearing test is that the ceiling binds</b> —
/// <see cref="The_door_admits_its_declared_ceiling_and_refuses_the_rest"/> — because it is the one
/// claim in the picture that says the rate under observation is the <em>Ruleset's</em> rather than the
/// runner's. The mode asks each gate for more than it can take; if the refused column were ever zero
/// the picture would be showing a cadence chosen in the shell, which is exactly the thing it exists
/// not to do.
/// </para>
/// <para>
/// ⚠ <b>Every measurement here is on <c>rulesets/crowded.toml</c> and that is not a preference.</b> It
/// is the only shipped file in which arrivals outpace housing, and therefore the only one in which the
/// Pool grows, the give-up bound is reachable and a Departure happens at all. `bordered.toml` is the
/// same world at the designer's own numbers and its 120-Day bound needs 245,760 Ticks to show one
/// Departure.
/// </para>
/// <para>
/// ⚠ <b>The runs here are short and the world is expensive</b>, because a Ruleset declaring a gate
/// paves the lattice to the map's boundary — 61 Segments become <b>535,817</b>. Four Days is what it
/// takes for the give-up bound to fire at this file's numbers, and it is not a figure any document may
/// quote. ⚠ <b>The dump turns <c>VerifyDecideWritesNothing</c> off</b>, as every dump does; leaving it
/// on would fold the whole world twice a Tick and make these tests ~75× slower for nothing
/// (<c>plans/0035</c> <b>F26</b>).
/// </para>
/// </remarks>
public sealed class ArrivalDumpTests
{
    /// <summary>Four Days: two for the bound, and two for a Departure to be drawn.</summary>
    private const string Ticks = "8192";

    private const string Population = "1000";

    /// <summary>Every panel is reached, which is what says the wiring runs at all.</summary>
    [Fact]
    public void The_picture_has_all_four_panels()
    {
        string report = Dump();

        Assert.Contains("THE DOORS", report, StringComparison.Ordinal);
        Assert.Contains("THE POOL, THE FLOWS", report, StringComparison.Ordinal);
        Assert.Contains("WHO IS STILL WAITING", report, StringComparison.Ordinal);
        Assert.Contains("THE MONEY", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The door admits exactly its declared ceiling and turns the rest away.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is what makes the rate the file's rather than the runner's.</b> The mode asks for
    /// <c>ceiling + 4</c> Households every Day, so a refused column of zero would mean the ask never
    /// exceeded the door — and the picture would then be a picture of the ask. ***A demonstration that
    /// chose its own rate would be showing the demonstration.***
    /// </remarks>
    [Fact]
    public void The_door_admits_its_declared_ceiling_and_refuses_the_rest()
    {
        string report = Dump();

        // crowded.toml declares arrivals_per_day = 96 on four gates, and the mode asks for 100.
        Assert.Contains("          96         100         96         4", report, StringComparison.Ordinal);
    }

    /// <summary>All four edges have a door, and each one is named by the edge it stands on.</summary>
    [Fact]
    public void Every_edge_is_a_door()
    {
        string report = Dump();

        foreach (string edge in (string[])["west", "east", "south", "north"])
        {
            Assert.Contains(edge, report, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The Pool grows and somebody gives up, which is the whole reason this world exists.
    /// </summary>
    /// <remarks>
    /// <b>Both halves, because either alone is a different city.</b> A Pool that grows with no
    /// Departure is a milestone-10 world with a door bolted on; a Departure with no growth is a city
    /// that is coping. `CONTEXT` → Departure's rule is that only the flow tells a large healthy Pool
    /// from a small desperate one, and this asserts the picture carries both.
    /// </remarks>
    [Fact]
    public void Arrivals_outpace_housing_and_somebody_gives_up()
    {
        string report = Dump();

        Assert.DoesNotContain("Nobody. The Pool is empty", report, StringComparison.Ordinal);
        Assert.Contains("of them have waited LONGER THAN THE BOUND", report, StringComparison.Ordinal);
    }

    /// <summary>Money crosses the gate and the two sides still agree.</summary>
    /// <remarks>
    /// <b>The supply moving is half the assertion and the equality is the other half.</b> A world
    /// whose supply never moves is one where no gate admitted anybody, and a world whose supply moves
    /// without the walk agreeing is the leak <c>Invariant.MoneyIsConserved</c> exists for.
    /// </remarks>
    [Fact]
    public void Money_crosses_the_gate_and_still_adds_up()
    {
        string report = Dump();

        Assert.Contains("CONSERVED: the walk and the anchor agree exactly.", report, StringComparison.Ordinal);
        Assert.DoesNotContain("The supply did not move over this run", report, StringComparison.Ordinal);
        Assert.DoesNotContain("🔴 LEAK", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Ruleset with no gate is refused rather than printing four blank panels.
    /// </summary>
    /// <remarks>
    /// <b><c>--land-value</c>'s polarity, and the failure it is written against is
    /// <c>plans/0034</c> F17</b>: milestone 9 shipped a producer that was correct and unobservable in
    /// every world that existed, for want of Ruleset <em>content</em> rather than code. ⚠ Nine of the
    /// eleven shipped files take this branch.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_door_is_refused()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"), ticks: "64", citizens: "200");

        Assert.Equal(2, code);
        Assert.Contains("declares no kind with arrivals_per_day", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// The mode refuses to be pointed at a recorded session, because it drives one itself.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the only dump that issues Commands</b>, and it is the reason the refusal exists:
    /// a replayed log would be stepped and then driven on top of, so the run would be neither the
    /// recorded session nor a clean demonstration. Nothing in the simulation decides to arrive until
    /// milestone 16 (<c>adr/0128</c>), so a mode showing arrivals has to ask for them.
    /// </remarks>
    [Fact]
    public void The_mode_refuses_a_recorded_session()
    {
        Assert.False(
            Options.TryParse(
                ["--arrivals", "--ruleset", Ruleset("crowded.toml"), "--log", "somewhere.borough"],
                out Options? _,
                out string? complaint));

        Assert.Contains("issues its own arrive commands", complaint!, StringComparison.Ordinal);
    }

    /// <summary>Asking for two pictures at once is refused, as every other mode refuses it.</summary>
    [Fact]
    public void Two_pictures_at_once_are_refused()
    {
        Assert.False(
            Options.TryParse(
                ["--arrivals", "--land-value", "--ruleset", Ruleset("crowded.toml")],
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
    /// 🔴 <b>Cached because the world is expensive and the run is identical, and the first draft was
    /// not.</b> Six tests each stepping their own four Days cost <b>1m30s</b> and would have tripled
    /// the working-loop tier for one feature — a Ruleset declaring a gate paves the lattice to the
    /// map's boundary, so every Tick is half a million Segments however small the city is. The run is
    /// seeded and reads nothing outside itself, so one report answers all six questions.
    /// ***A fixture that six tests agree about is one fixture, and building it six times measures the
    /// builder.*** <c>TierBudgetTests</c> would not have caught this: no single test was near the
    /// four-minute ceiling, and what grew was the tier.
    /// </remarks>
    private static readonly Lazy<string> Report = new(() =>
    {
        (int code, string report) = Run(Ruleset("crowded.toml"));

        Assert.Equal(0, code);

        return report;
    });

    private static string Dump() => Report.Value;

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--arrivals", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = ArrivalDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
