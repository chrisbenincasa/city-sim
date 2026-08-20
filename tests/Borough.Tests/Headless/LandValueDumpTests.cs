using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// Milestone 9 task 6: <c>--land-value</c>, and the first picture of a field chasing another field.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>No ordinal, deliberately.</b> <c>ParkingDumpTests</c> calls itself <em>the tenth runner
/// mode</em>, <c>Options</c> carries a struck sentence saying <em>a count in prose is a fact that
/// drifts, and the tenth mode is what made the drift legible</em>, and the board records two branches
/// each shipping a tenth. ***Count the enum.***
/// </para>
/// <para>
/// <b>The load-bearing test is <see cref="The_lag_lags"/></b>, because it is the one claim in the
/// picture a run could refute: <c>02 §2.4</c> says land value <em>moves slowly toward the current
/// desirability rather than tracking it</em>, and the gap panel exists to show that. A picture that
/// printed the target and the value and found them identical would be a picture of a mechanism that
/// is not there.
/// </para>
/// <para>
/// ⚠ <b>Every measurement here is on <c>rulesets/fouled.toml</c> and that is not a preference.</b> It
/// is the only shipped file whose Rules emit into a Map Layer, and the only thing in the build that
/// creates a Cell row is an emission — so on the other eight the Cell table is empty, land value is
/// zero everywhere, and every panel is blank. The other eight are the refusal below.
/// </para>
/// </remarks>
public sealed class LandValueDumpTests
{
    /// <summary>
    /// Ten Days and about eight hours, so the dump lands in the morning commute.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Not a round number, and the roundness is the trap.</b> A Day is 2,048 Ticks, so every
    /// round multiple of it lands at midnight — where a city with 100% car ownership has no Vehicle
    /// on any Segment and desirability's noise term is <b>zero in every Cell</b> (<c>adr/0127</c>).
    /// ***A dump taken on a round number of Days is a dump of a one-term composition***, and it would
    /// have read as a working two-term one.
    /// </remarks>
    private const string Ticks = "21163";

    private const string Population = "4000";

    /// <summary>All three panels are drawn, which is what says the wiring is reached at all.</summary>
    [Fact]
    public void The_target_the_lag_and_the_gap_are_all_drawn()
    {
        string report = Dump();

        Assert.Contains("## The TARGET", report, StringComparison.Ordinal);
        Assert.Contains("## The LAG", report, StringComparison.Ordinal);
        Assert.Contains("## The GAP", report, StringComparison.Ordinal);
        Assert.Contains("## What the field DID", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The lag lags: the gap is real, and it is small beside the field.</b>
    /// </summary>
    /// <remarks>
    /// Both halves matter and they fail differently. A gap of zero means land value is tracking its
    /// target instantly, so the stored column has no reason to exist and <c>02 §2.4</c>'s stated
    /// exception to <em>compose at the point of use</em> is unearned. A gap the size of the field
    /// means the lag is not converging at all, which is a different defect wearing the same panel.
    /// </remarks>
    [Fact]
    public void The_lag_lags()
    {
        string report = Dump();
        int target = Peak(report, "## The TARGET");
        int gap = Peak(report, "## The GAP");

        Assert.True(gap > 0, "the gap is zero everywhere, so land value is tracking rather than lagging");
        Assert.True(
            gap * 4 < target,
            $"the gap peaks at {gap} against a field peaking at {target}. Over a quarter of the field "
            + "and the lag is not converging, which is a different fault from the one this panel is for");
    }

    /// <summary>
    /// <b>The hour is in the header, and it is not decoration.</b>
    /// </summary>
    /// <remarks>
    /// The noise term reads a Segment's volume at the instant it is asked, so a reader who does not
    /// know the hour cannot tell a quiet neighbourhood from a quiet time of day. This asserts the
    /// header says which — and that this run is not at midnight, because a midnight run would be a
    /// picture of the pollution term alone.
    /// </remarks>
    [Fact]
    public void The_header_says_what_hour_it_was_taken_at()
    {
        string report = Dump();

        Assert.Contains("THE HOUR IS 08:00", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Ruleset that emits nothing is refused rather than drawn blank.</b>
    /// </summary>
    /// <remarks>
    /// <c>--parking</c>'s polarity, and the reason it is worth a refusal here is that the blank
    /// picture would be <em>correct</em>: land value really is zero everywhere on those files. ⚠ An
    /// operator reading a correct blank grid concludes the mechanism is broken, which is the more
    /// expensive mistake.
    /// </remarks>
    [Fact]
    public void A_ruleset_that_emits_nothing_is_refused()
    {
        (int code, string report) = Run(Ruleset("minimal.toml"));

        Assert.Equal(2, code);
        Assert.Contains("no Rule that emits into a Map Layer", report, StringComparison.Ordinal);
        Assert.Contains("fouled.toml", report, StringComparison.Ordinal);
    }

    /// <summary>The peak a panel reports, in raw Q16.16, read back out of its own header line.</summary>
    private static int Peak(string report, string heading)
    {
        int at = report.IndexOf(heading, StringComparison.Ordinal);

        Assert.True(at >= 0, $"{heading} is not in the report");

        const string marker = "(Q16.16 ";
        int from = report.IndexOf(marker, at, StringComparison.Ordinal) + marker.Length;
        int to = report.IndexOf(')', from);

        return int.Parse(report[from..to], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static string Dump()
    {
        (int code, string report) = Run(Ruleset("fouled.toml"));

        Assert.Equal(0, code);

        return report;
    }

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Ticks)
    {
        Assert.True(
            Options.TryParse(
                ["--land-value", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();
        int code = LandValueDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
