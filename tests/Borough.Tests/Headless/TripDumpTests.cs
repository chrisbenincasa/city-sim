using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// That <c>--trips</c> reports the city it was given, and in particular that it does not report a
/// severed one as intact.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the failure the instrument it stands beside actually shipped with.</b> <c>--roads</c>
/// announced Severance over a city that has none for the whole of milestone 5a, because its verdict
/// compared component <em>counts</em> and almost every foot component in the shipped world is an
/// Arterial junction nobody can walk to. A verdict is a sentence a human reads and acts on, so a
/// wrong one is worse than no verdict — and <c>NO VERDICT</c> is one of this project's guiding
/// concepts precisely about that. So the verdict is tested in <b>both directions</b> against two
/// Rulesets whose severance is independently known.
/// </para>
/// <para>
/// <b>Run at a small population, and the numbers are not the assertion.</b> These are structural
/// claims — <i>a severed city reports unreachable pairs</i>, <i>an intact one reports none</i>,
/// <i>the two instruments agree</i> — and pinning a percentile here would make the suite fail
/// whenever the generator's dice moved, which is a test that teaches its reader to weaken it.
/// </para>
/// </remarks>
public sealed class TripDumpTests
{
    /// <summary>Enough Citizens for a few dozen Buildings, and few enough to be a fast test.</summary>
    private const string Population = "400";

    /// <summary>
    /// The populations <c>severance.toml</c> is read at, which are <b>not</b>
    /// <see cref="Population"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Severance on that file is a BAND, and 400 fell out of the bottom of it at
    /// <c>plans/0053</c>.</b> Its block is 256 Tiles rather than 32, so its parcels are eight times
    /// across what the shipped lattice's are — and once occupancy divides floor area, one Building
    /// on one of those parcels holds dozens of Households. At 400 Citizens the whole city was
    /// <b>two Buildings</b>, and two Buildings are one pair: ***a city with nothing in it cannot be
    /// severed***, and the instrument correctly reported no unreachable pair.
    /// </para>
    /// <para>
    /// 🔴 <b>AND AT <c>plans/0055</c> THE BAND STOPPED BEING A BAND.</b> Measured 2026-09-02 it read
    /// as a clean window — 1,000 → 0 unreachable pairs, 2,000 → 16 of 28, 4,000 → 73 of 120, 8,000 →
    /// 175 of 300, 16,000 → 0 — and 4,000 was chosen as its middle so that a test here would not be
    /// one small change away from an edge. Re-swept 2026-09-03 under the centre-out subdivider, the
    /// same file reads: <b>1,000 → 0, 2,000 → 16, 3,000 → 20, 4,000 → 0, 6,000 → 111, 8,000 → 153,
    /// 12,000 → 0, 16,000 → 951.</b> ***There is no middle to stand in.*** Whether a city of this
    /// shape severs is a property of exactly which blocks the walk reached before it had housed
    /// everybody, and it flips between adjacent sizes.
    /// </para>
    /// <para>
    /// ⚠ <b>So the test reads a LADDER and asserts the file can sever, rather than reading one
    /// population and asserting it does.</b> A single reading here was never testing the Ruleset; it
    /// was testing that one number still landed inside a band nobody was watching. <b>Every rung is
    /// dumped and every rung is checked for a disagreeing verdict</b>, so the cost is eight dumps and
    /// what it buys is a fact that survives the next change to how the city is carved.
    /// </para>
    /// </remarks>
    private static readonly string[] SeveredPopulations =
        ["1000", "2000", "3000", "4000", "6000", "8000", "12000", "16000"];

    /// <summary>
    /// <b><c>rulesets/severance.toml</c> is a city people cannot walk across, and the dump says so.</b>
    /// </summary>
    /// <remarks>
    /// The Ruleset exists because <c>rulesets/minimal.toml</c> <em>cannot</em> demonstrate Severance
    /// and that is measured, not assumed — 0.0% at every dial value on the shipped 32-Tile lattice.
    /// So this is the file that has something to report, and an instrument silent here would be
    /// reporting nothing anywhere.
    /// </remarks>
    [Fact]
    public void A_severed_city_reports_pairs_with_no_route()
    {
        List<string> read = [];
        int severed = 0;

        foreach (string citizens in SeveredPopulations)
        {
            string report = Dump("severance.toml", citizens);
            int unreachable = Unreachable(report);

            read.Add($"{citizens} → {unreachable}");

            if (unreachable > 0)
            {
                severed++;
            }

            // Checked at EVERY rung rather than at the one that severed. The two verdicts disagreeing
            // is a defect in the instrument, and an instrument is not excused from being right about
            // a city it has nothing to report on.
            Assert.DoesNotContain("THEY DISAGREE", report, StringComparison.Ordinal);
        }

        Assert.True(
            severed > 0,
            "a city known to sever reported every pair reachable at every size on the ladder: "
            + string.Join(", ", read)
            + ". severance.toml exists to be the one world with something to report, so this is "
            + "either the file no longer severing or the instrument no longer seeing it.");
    }

    /// <summary>
    /// <b><c>rulesets/minimal.toml</c> is a city people can walk across, and the dump says that too.</b>
    /// </summary>
    /// <remarks>
    /// <b>The direction that catches the defect <c>--roads</c> had.</b> An instrument that announces
    /// Severance over everything passes the test above and is useless; only this one distinguishes a
    /// verdict from a banner.
    /// </remarks>
    [Fact]
    public void An_intact_city_reports_no_pair_without_a_route()
    {
        string report = Dump("minimal.toml");

        Assert.Equal(0, Unreachable(report));
        Assert.DoesNotContain("THEY DISAGREE", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A band where every pair was unreachable does not print as a band where walking was free.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Found by reading the output rather than by reasoning about it.</b> Percentiles of an empty
    /// list are zero, so the longest band of the severed city printed <c>0.0 min</c> and <c>0%</c>
    /// detour — which reads as <i>instant, and a perfect route</i> over a band in which nobody could
    /// get anywhere at all. *A placeholder whose value sits inside the range of legitimate answers
    /// cannot announce itself* (session F), and zero minutes is a legitimate walk.
    /// </para>
    /// <para>
    /// <b>Asserted as <i>no band ever prints a zero walk</i> rather than as <i>this band says NOT
    /// ONE</i>, and the difference is what makes it a test rather than a snapshot.</b> Whether any
    /// band comes out wholly unreachable depends on the population and on where the Arterials fell,
    /// so pinning the sentence would make this pass or fail on the dice. What is true at every
    /// population is that two distinct Buildings are never a zero-minute walk apart.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>Matched with a digit boundary, and the plain substring failed on 2026-08-12 — three
    /// paragraphs below the note warning about exactly this.</b> <see cref="Unreachable"/> already
    /// says <i>parsed rather than substring-matched, because the substring lies</i>, about a different
    /// assertion in this same file. This one then broke the moment the header's Commute Budget started
    /// printing as <c>20.0 min</c>, which contains <c>0.0 min</c>. <b>A rule written down beside the
    /// code it governs is not thereby applied to the code next to it</b> — which is the corpus's own
    /// <i>citing an ADR is not applying it</i> (<c>adr/0044</c>) at the scale of one file.
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>The two files are read at DIFFERENT populations and the second argument is why.</b>
    /// <see cref="Population"/> is *enough Citizens for a few dozen Buildings* on the shipped
    /// lattice and has never been that on <c>severance.toml</c>, whose block is 256 Tiles — the
    /// class remark above records 400 falling out of the bottom of that file's band at
    /// <c>plans/0053</c>, and this theory went on passing 400 to it anyway. <c>plans/0058</c> made
    /// the sparsest form three storeys, one Building on one of those enormous parcels then held the
    /// entire population, and the dump correctly reported ***a city with one Building has no pair to
    /// walk between***. ⚠ <b>The exit code is what failed, not the assertion</b>, which is the
    /// instrument refusing to report on a world that cannot carry the reading rather than reporting
    /// a clean zero from it.
    /// </remarks>
    [Theory]
    [InlineData("severance.toml", "2000")]
    [InlineData("minimal.toml", Population)]
    public void No_band_reports_a_walk_of_no_time_at_all(string ruleset, string citizens) =>
        Assert.DoesNotMatch(@"(?<!\d)0\.0 min", Dump(ruleset, citizens));

    /// <summary>
    /// The detour figure exists, which is the half <c>--roads</c> states it cannot measure.
    /// </summary>
    [Fact]
    public void The_dump_reports_detour_over_the_grid_ideal()
    {
        string report = Dump("minimal.toml");

        Assert.Contains("DETOUR, over every reachable pair", report, StringComparison.Ordinal);
        Assert.Contains("% of the grid ideal", report, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Ruleset with no <c>[roads]</c> is a legitimate file, and the dump names the absence.
    /// </summary>
    /// <remarks>
    /// The refusal at the option layer catches <em>no Ruleset at all</em>; this catches a Ruleset
    /// that declares no network, which the parser cannot see. Both exist so that an empty table is
    /// never what the operator gets.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_roads_is_named_rather_than_printed_empty()
    {
        string shipped = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        int roads = shipped.IndexOf("[roads]", StringComparison.Ordinal);

        Assert.True(roads > 0, "minimal.toml no longer declares [roads]; this fixture is stale.");

        string path = Path.Combine(Path.GetTempPath(), $"borough-no-roads-{Environment.ProcessId}.toml");

        try
        {
            File.WriteAllText(path, shipped[..roads] + Capacity(shipped));

            Assert.True(
                Options.TryParse(
                    ["--trips", "--ruleset", path, "--citizens", Population],
                    out Options? options,
                    out string? complaint),
                complaint);

            var writer = new StringWriter();

            // 3 rather than 0: an absent network is not a successful measurement of nothing.
            Assert.Equal(3, TripDump.Run(options!, writer));
            Assert.Contains("declares no [roads]", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// <c>minimal.toml</c>'s <c>[capacity]</c> table, sliced out of the shipped text.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A truncated Ruleset has to keep this</b> (<c>plans/0053</c>). Employment derives from
    /// floor area against <c>floor_tiles_per_job</c>, and <c>adr/0101</c>'s pairing is two-way — a
    /// kind that employs nobody must not state when its Shift begins — so a file cut above
    /// <c>[capacity]</c> keeps a <c>[[business]]</c> Shift band with nothing to employ and is
    /// <em>refused at load</em>. That refusal is the loader working, and it would reach this test as
    /// an exit code that looks like the absence under test. ⚠ <b>Sliced rather than retyped</b>: a
    /// second copy of the rates here would be a number free to disagree with the shipped one.
    /// </remarks>
    private static string Capacity(string shipped)
    {
        int start = shipped.IndexOf("\n[capacity]", StringComparison.Ordinal);

        Assert.True(start > 0, "minimal.toml no longer declares [capacity]; this fixture is stale.");

        int end = shipped.IndexOf("\n[parking]", start, StringComparison.Ordinal);

        Assert.True(end > start, "[capacity] is no longer followed by [parking]; fixture is stale.");

        // ⚠ The table headers are found at a LINE START, because both names also appear inside the
        // comments between them -- a match on the bare token cut the slice mid-line.
        return shipped[start..end] + "\n";
    }

    /// <summary>
    /// <b>A Ruleset with no <c>[trips]</c> is refused rather than walked at a crossing cost of
    /// zero.</b>
    /// </summary>
    /// <remarks>
    /// The same polarity as the <c>[roads]</c> refusal above and for a sharper reason: an absent road
    /// network produces an obviously empty picture, whereas an unauthored crossing cost produces a
    /// full and plausible one. Zero is <c>adr/0074</c>'s rung 1 and a legitimate authored value, so a
    /// zero standing in for <em>nobody chose</em> would be a number this instrument had chosen — which
    /// <c>adr/0052</c> forbids it to do, and which no reader of the output could detect.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_trips_is_refused_rather_than_walked_at_a_free_crossing()
    {
        string shipped = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        int trips = shipped.IndexOf("[trips]", StringComparison.Ordinal);

        Assert.True(trips > 0, "minimal.toml no longer declares [trips]; this fixture is stale.");

        string path = Path.Combine(Path.GetTempPath(), $"borough-no-trips-{Environment.ProcessId}.toml");

        try
        {
            File.WriteAllText(path, shipped[..trips]);

            Assert.True(
                Options.TryParse(
                    ["--trips", "--ruleset", path, "--citizens", Population],
                    out Options? options,
                    out string? complaint),
                complaint);

            var writer = new StringWriter();

            Assert.Equal(3, TripDump.Run(options!, writer));
            Assert.Contains("declares no [trips]", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// <b>The header names the crossing cost it ran at and the Commute Budget the city states.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is half of the crossing cost's named ratifier, and the half that makes the other half
    /// legible.</b> <c>plans/0002</c> §D asks for the walk-Leg distribution <i>with the term at zero
    /// and at a candidate value</i> — two runs, compared — and a report that does not say which value
    /// it ran at cannot be one of the two.
    /// </para>
    /// <para>
    /// <b>The Budget line flipped branch on 2026-08-12 and the discipline behind it did not.</b> This
    /// test asserted <i>no Commute Budget</i> until 5b-bis task 4 set one, which is exactly what task
    /// 3 said would happen: the Budget is a percentile of this distribution, so it could not be
    /// authored until the distribution existed, and authoring it does not make this census apply it.
    /// <b>Every pair is still walked</b> — the report says so — because the source of a percentile
    /// must stay uncensored by the number read off it. That sentence is what the assertion is really
    /// holding in place.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_header_names_the_crossing_cost_and_the_commute_budget()
    {
        string report = Dump("minimal.toml");

        Assert.Contains("crossing_seconds", report, StringComparison.Ordinal);
        Assert.Contains("UNRATIFIED", report, StringComparison.Ordinal);
        Assert.Contains("The Commute Budget is", report, StringComparison.Ordinal);
        Assert.Contains("does not apply it", report, StringComparison.Ordinal);
    }

    /// <summary>The pair count the verdict reports as having no route at all.</summary>
    /// <remarks>
    /// <b>Parsed rather than substring-matched, because the substring lies.</b> The first version of
    /// this asserted the report did NOT contain <c>"0 pair(s) had no pedestrian route"</c> — which is
    /// a substring of <c>"220 pair(s) had no pedestrian route"</c>, so a correctly severed city
    /// failed its own test. A number is the thing being claimed, so a number is what to read.
    /// </remarks>
    private static int Unreachable(string report)
    {
        const string Tail = " pair(s) had no pedestrian route at all";

        string line = Array.Find(
            report.Split('\n'), each => each.Contains(Tail, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("the dump printed no unreachable-pair verdict");

        return int.Parse(
            line[..line.IndexOf(Tail, StringComparison.Ordinal)],
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// ⚠ <b>The population is the caller's</b> — see <see cref="SeveredPopulations"/>, which records
    /// why <c>severance.toml</c> is read at eight sizes rather than at one.
    /// </summary>
    private static string Dump(string ruleset, string? citizens = null)
    {
        Assert.True(
            Options.TryParse(
                ["--trips", "--ruleset", Path.Combine(AppContext.BaseDirectory, "Rulesets", ruleset),
                 "--citizens", citizens ?? Population],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();

        Assert.Equal(0, TripDump.Run(options!, writer));

        return writer.ToString();
    }

}
