using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// That <c>--commute</c> shows a city changing rather than a city, and refuses where it cannot.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim under test is that the picture has a <em>before</em>.</b> It is the second of the
/// five dumps to step the world — <c>--zones</c> is the first — and the reason is the same: a sweep
/// and an assignment pass are both things that happen over time, where a Road Graph is laid at world
/// creation and has no earlier state to compare against. A dump of the after alone would be a
/// photograph of a city, and this milestone's defect class is <em>the mechanism produced a city
/// nobody would build</em>, which needs the two frames.
/// </para>
/// <para>
/// <b>The numbers are not the assertion.</b> How many blocks come out balanced is a property of the
/// populator's dice and of the Ruleset's job count, and pinning one here would make the suite fail
/// whenever either moved. What is structural is that the before is all export and the after is not.
/// </para>
/// </remarks>
public sealed class CommuteDumpTests
{
    /// <summary>Small enough to be a fast test, large enough to fill several blocks.</summary>
    private const string Population = "600";

    /// <summary>Long enough for the assignment pass and a slice of the departure window.</summary>
    private const string TickCount = "2048";

    /// <summary>
    /// <b>Before, every block exports its whole population; after, they do not.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test, and it is a <em>difference</em> rather than a state. A dump that printed
    /// the same grid twice would pass any assertion about either grid on its own, and that is exactly
    /// the failure the two frames exist to catch — an assignment pass that ran and changed nothing
    /// produces a moving State Hash and a plausible Census.
    /// </remarks>
    [Fact]
    public void The_before_is_a_city_with_no_jobs_and_the_after_is_not()
    {
        string report = Dump("minimal.toml");

        int before = report.IndexOf("## Before", StringComparison.Ordinal);
        int after = report.IndexOf("## After", StringComparison.Ordinal);

        Assert.True(before >= 0 && after > before, "the dump printed fewer than two grids.");

        string first = report[before..after];
        string second = report[after..];

        // Asserted on the BALANCED count rather than on the importing one, because whether any block
        // ends up with more workers than residents is a property of the dice. What is structural is
        // that before the run nobody holds a job anywhere, so no block can be anything but an
        // exporter -- and after it, some are not.
        Assert.Contains("0 take them in, 0 are within a", first, StringComparison.Ordinal);
        Assert.DoesNotContain("0 are within a", second, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The header states the three numbers that decide what the picture shows.</b>
    /// </summary>
    /// <remarks>
    /// The Commute Budget sizes the search box <em>and</em> the acceptance test, the sample says how
    /// fast the pass works through the population, and the Shift band decides which Tick of the Day
    /// every journey falls on — so a reader who cannot see them cannot tell a city that has settled
    /// from a run that stopped early. <c>--trips</c>' header sets the precedent and its reason is the
    /// same: a report that does not say which value it ran at cannot be one of two runs compared.
    /// <para>
    /// ⚠ <b>It named <c>commute_peak_factor</c> until <c>adr/0101</c> retired that key</b>, and the
    /// replacement is not a rename: the peak was a number the file <em>authored</em> and the band is
    /// one it <em>bounds</em>, so what the header can honestly state changed with it.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_header_names_the_budget_the_sample_and_the_shift_band()
    {
        string report = Dump("minimal.toml");

        Assert.Contains(
            "fast to 20.0 min, moderate to 40.0, unsavoury to 50.0", report, StringComparison.Ordinal);
        Assert.Contains("Only the ceiling refuses", report, StringComparison.Ordinal);
        Assert.Contains("candidate(s) each", report, StringComparison.Ordinal);
        Assert.Contains("best rung it draws", report, StringComparison.Ordinal);
        Assert.Contains("A Shift runs 6-10 in-world hours", report, StringComparison.Ordinal);
        Assert.Contains("15 min of being early", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The cost distribution is printed under a warning that it is not evidence about the
    /// Budget.</b>
    /// </summary>
    /// <remarks>
    /// 5b-bis task 6's finding, carried into the one place a reader will meet the distribution and be
    /// tempted to read a percentile off it. A commute exists only because the assignment pass already
    /// accepted the job at the other end of it, inside the Budget — so the ceiling is upstream and
    /// this histogram is censored by the number it would be used to ratify. <c>NO VERDICT</c> is a
    /// guiding concept about exactly this: an instrument that lets a reader draw a conclusion its
    /// numbers cannot support is worse than one that prints nothing.
    /// </remarks>
    [Fact]
    public void The_distribution_says_it_is_not_the_budgets_ratifier()
    {
        string report = Dump("minimal.toml");

        Assert.Contains("NOT the one the Commute Budget is a percentile of", report, StringComparison.Ordinal);
        Assert.Contains("--trips", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>How many people work in the Building they live in is printed, because nothing else counts
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <c>[[building]] jobs</c> sits on the <c>dwelling</c> kind (task 4), so a Citizen can be
    /// employed where they sleep and the generator makes that a Trip of no length. It is a legitimate
    /// arrangement and it is invisible in every other reading — the Census counts the Trip, the hash
    /// folds it, and neither can say it went nowhere. Task 6 saw it and refused to change behaviour
    /// inside an instrumentation task; this is where it became visible instead.
    /// </remarks>
    [Fact]
    public void The_dump_counts_the_people_who_work_where_they_live()
    {
        Assert.Contains(
            "work in the Building they live in", Dump("minimal.toml"), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Ruleset with no <c>[jobs]</c> is named rather than printed as a city of the unemployed.</b>
    /// </summary>
    /// <remarks>
    /// <c>--zones</c>' refusal exactly. Employment is content: a grid in which every block exports its
    /// whole population is what this dump prints <em>before</em> the run, so printing it after as well
    /// would read as a broken assignment pass rather than as a file that grants no work. The two
    /// failures are indistinguishable in the picture, which is why the refusal is at the top.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_jobs_is_named_rather_than_printed_empty()
    {
        string shipped = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        int jobs = shipped.IndexOf("\n[jobs]", StringComparison.Ordinal);

        Assert.True(jobs > 0, "minimal.toml no longer declares [jobs]; this fixture is stale.");

        string path = Path.Combine(
            Path.GetTempPath(), $"borough-no-jobs-{Environment.ProcessId}.toml");

        try
        {
            File.WriteAllText(path, shipped[..jobs]);

            Assert.True(
                Options.TryParse(
                    ["--commute", "--ruleset", path, "--citizens", Population, "--ticks", TickCount],
                    out Options? options,
                    out string? complaint),
                complaint);

            var writer = new StringWriter();

            // 3 rather than 0: an absent mechanism is not a successful measurement of nothing.
            Assert.Equal(3, CommuteDump.Run(options!, writer));
            Assert.Contains("declares no [jobs]", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// <b>It refuses without a Ruleset at the option layer, before a world is built.</b>
    /// </summary>
    /// <remarks>
    /// Every picture's refusal. The complaint names <em>both</em> halves of what makes employment
    /// content — the cadence in <c>[jobs]</c> and the floor a post takes in
    /// <c>[capacity] floor_tiles_per_job</c> — because a reader who supplies one and not the other
    /// gets the refusal above instead, and the two messages have to lead to the same place.
    /// ⚠ <b>The second half was <c>[[building]] jobs</c> until <c>plans/0053</c> step 3</b>, and this
    /// assertion is what noticed the refusal still naming a retired key: ***a message that names a
    /// key nobody can write is a refusal that cannot be acted on.***
    /// </remarks>
    [Fact]
    public void It_refuses_without_a_ruleset()
    {
        Assert.False(
            Options.TryParse(["--commute"], out Options _, out string? complaint));

        Assert.Contains("--commute needs --ruleset", complaint, StringComparison.Ordinal);
        Assert.Contains("[capacity] floor_tiles_per_job", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Two pictures at once is a refusal, because each builds its own world.</b>
    /// </summary>
    [Theory]
    [InlineData("--zones")]
    [InlineData("--roads")]
    [InlineData("--trips")]
    public void Two_pictures_at_once_is_refused(string other)
    {
        Assert.False(
            Options.TryParse(
                ["--commute", other, "--ruleset", "r.toml"], out Options _, out string? complaint));

        Assert.Contains("Ask for one", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// A flag the usage text does not name is a flag nobody finds — <c>adr/0002</c>, the shell owns
    /// every string a human reads.
    /// </summary>
    [Fact]
    public void The_usage_text_names_the_commute_dump() =>
        Assert.Contains("--commute", Options.Usage, StringComparison.Ordinal);

    /// <summary>The CSV form carries the two counts the glyph collapses into one.</summary>
    /// <remarks>
    /// <c>--zones</c>' precedent: the picture answers <i>which way do people move</i> and the CSV
    /// answers <i>by how much</i>, and a glyph that carried the second would be a number pretending
    /// to be a picture.
    /// </remarks>
    [Fact]
    public void The_csv_form_carries_residents_and_workers_separately() =>
        Assert.Contains(
            "block_east,block_north,residents,workers", Dump("minimal.toml", csv: true),
            StringComparison.Ordinal);

    private static string Dump(string ruleset, bool csv = false)
    {
        string[] arguments = csv
            ? ["--commute", "--csv", "--ruleset", Path.Combine(AppContext.BaseDirectory, "Rulesets", ruleset),
               "--citizens", Population, "--ticks", TickCount]
            : ["--commute", "--ruleset", Path.Combine(AppContext.BaseDirectory, "Rulesets", ruleset),
               "--citizens", Population, "--ticks", TickCount];

        Assert.True(
            Options.TryParse(arguments, out Options? options, out string? complaint), complaint);

        var writer = new StringWriter();

        Assert.Equal(0, CommuteDump.Run(options!, writer));

        return writer.ToString();
    }
}
