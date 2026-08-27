using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// <c>--day</c>: one Citizen, one Day, every Tick. <c>plans/0045</c>'s queue item 3.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every other mode in this runner aggregates.</b> This one follows a person, which is the first
/// pillar — <em>a city made of people you can actually meet</em> — and the one thing nothing in the
/// tree had ever printed.
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts a count of events, and the omission is deliberate.</b> How much happens
/// in one Citizen's Day is a property of the Shift draw, the geometry and the Ruleset, so a number
/// would be a baseline sitting in an assertion-tier test and would move on every retune
/// (<c>plans/0032</c>). What is asserted is that the trace is <em>about somebody who could have
/// travelled</em> and that the footer is present, because a sparse Day is the honest output and must
/// not be mistaken for a broken one.
/// </para>
/// </remarks>
public sealed class DayDumpTests
{
    private const string Population = "2000";

    /// <summary>Long enough for the job cadence to have placed somebody.</summary>
    /// <remarks>
    /// ⚠ <b>Measured rather than picked.</b> Employment is assigned every 32 Ticks over a sample
    /// (<c>adr/0081</c>), so a trace started early follows somebody with no job and no journey — which
    /// is the failure this constant exists to avoid and is not a property of the dump.
    /// </remarks>
    private const string Settle = "4096";

    /// <summary>
    /// <b>The subject can travel</b> — they have a home, an employer, and premises to travel to.
    /// </summary>
    /// <remarks>
    /// The first run of this dump followed somebody whose employer had no premises, so the trace was
    /// empty for a reason that was not about them. <c>CommuteEngine.Travel</c> refuses on exactly that
    /// hop, and <c>adr/0146</c> makes an unpremised employer a real state rather than a corner — a
    /// founder is their own Business's first worker before placement tenants it.
    /// </remarks>
    [Fact]
    public void The_subject_has_a_home_an_employer_and_premises()
    {
        string report = Dump("minimal.toml");

        Assert.Contains("lives in     Building ", report, StringComparison.Ordinal);
        Assert.Contains("works at     Business ", report, StringComparison.Ordinal);
        Assert.DoesNotContain("Building nowhere", report, StringComparison.Ordinal);
        Assert.DoesNotContain("Business nowhere", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The trace carries a denominator</b>, so a quiet Day can be read against the city.
    /// </summary>
    /// <remarks>
    /// Added because the first run could not distinguish <em>this person's Shift</em> from <em>a city
    /// in which nobody commutes</em>. ***A single-subject instrument needs a denominator.*** Both
    /// readings are asserted, because one taken only at the start cannot show a population that moved.
    /// </remarks>
    [Fact]
    public void The_city_is_counted_before_and_after()
    {
        string report = Dump("minimal.toml");

        Assert.Equal(
            2,
            report.Split("meanwhile", StringSplitOptions.None).Length - 1);
        Assert.Contains("and the city, one Day on:", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The footer names what is absent and why.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0070</c> — an unbuilt mechanism is not a design constraint — arriving on an instrument.
    /// A sparse timeline with no footer reads as a defect in the dump; the same timeline with the
    /// footer reads as the finding it is. ⚠ <b>This is the assertion that would fail on the day Needs
    /// or ageing land</b>, which is the point: the footer is a claim about the build and has to be
    /// re-read when the build changes.
    /// </remarks>
    [Fact]
    public void The_footer_names_the_mechanisms_that_do_not_exist()
    {
        string report = Dump("minimal.toml");

        Assert.Contains("did NOT do", report, StringComparison.Ordinal);
        Assert.Contains("adr/0070", report, StringComparison.Ordinal);
        Assert.Contains("CitizenTable.Age", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Ruleset with no <c>[jobs]</c> is refused rather than traced.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The commute is the only Trip generator in the build, so a file stating no <c>[jobs]</c> table
    /// produces a Day in which nothing can happen for a reason that is about the content and not about
    /// the city. Every other picture in this runner refuses an input it cannot demonstrate, and this
    /// follows them.
    /// </para>
    /// <para>
    /// ⚠ <b>No shipped Ruleset reaches this branch</b> — all twenty state <c>[jobs]</c> — so the test
    /// authors a file that does. ***A branch no shipped content reaches is still a branch content can
    /// reach, and the test is what says which.***
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_jobs_is_refused_and_says_which_file_to_use()
    {
        string path = WithoutJobs();

        try
        {
            (int code, string report) = Run(path);

            Assert.NotEqual(0, code);
            Assert.Contains("[jobs]", report, StringComparison.Ordinal);
            Assert.Contains("provisioned.toml", report, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary><c>minimal.toml</c> with its <c>[jobs]</c> table taken out, written to a temp file.</summary>
    private static string WithoutJobs()
    {
        string[] lines = File.ReadAllLines(Ruleset("minimal.toml"));
        var kept = new List<string>(lines.Length);
        bool dropping = false;

        foreach (string line in lines)
        {
            if (line.StartsWith('['))
            {
                dropping = line.StartsWith("[jobs]", StringComparison.Ordinal);
            }

            if (!dropping)
            {
                kept.Add(line);
            }
        }

        string path = Path.Combine(Path.GetTempPath(), $"no-jobs-{Guid.NewGuid():N}.toml");

        File.WriteAllLines(path, kept);

        return path;
    }

    /// <summary>Two runs of one seed follow the same person, which is what makes a diff readable.</summary>
    [Fact]
    public void The_same_seed_follows_the_same_citizen()
    {
        Assert.Equal(Dump("minimal.toml"), Dump("minimal.toml"));
    }

    private static string Ruleset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", name);

    private static string Dump(string ruleset)
    {
        (int code, string report) = Run(Ruleset(ruleset));

        Assert.Equal(0, code);

        return report;
    }

    private static (int Code, string Report) Run(
        string ruleset, string citizens = Population, string ticks = Settle)
    {
        Assert.True(
            Options.TryParse(
                ["--day", "--ruleset", ruleset, "--citizens", citizens, "--ticks", ticks],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter(CultureInfo.InvariantCulture);
        int code = DayDump.Run(options!, writer);

        return (code, writer.ToString());
    }
}
