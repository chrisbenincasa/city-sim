using System.Globalization;
using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Formats;
using Borough.Headless;
using Borough.Tests.Golden;

namespace Borough.Tests.Headless;

/// <summary>
/// That the Census ring can be read as a shape, and that the collapse withholds nothing.
/// </summary>
/// <remarks>
/// <para>
/// <b>The ring has held the shape of every quantity since slice 5 and nothing printed it.</b>
/// <c>CensusReport</c> collapses each series to <em>first, last, low, high</em>, which answers
/// <em>did this trend</em> and is silent on <em>when</em> — and <em>when</em> is the question a run
/// whose aggregate is surprising has to be read with. That is <c>CensusReportTests</c>' finding one
/// level up: a family with no reader is a family nobody can see, and a <em>series</em> with no reader
/// is a shape nobody can see.
/// </para>
/// <para>
/// <b>What is asserted here is the report's contract and never its numbers.</b> What any counter says
/// is a property of the fixture and belongs to the tests that own that counter; what belongs here is
/// that a reading appears once per sample, that a column dropped for being flat is named with its
/// value, and that the Tick axis is the one the Census stamped rather than one recomputed from the
/// cadence.
/// </para>
/// </remarks>
public sealed class SeriesReportTests
{
    /// <summary>Long enough for the assignment pass, a few departure phases and four readings.</summary>
    /// <remarks>
    /// <b>256 until milestone 17, and it is derived rather than lengthened for comfort.</b> Both
    /// tests here need a column that MOVES, which means a Building has to fall down inside the run:
    /// <c>declining.toml</c> condemns on a 2-Day threshold and collapses a Day later, so nothing
    /// moves before 6,144 Ticks and this is the first round number past it. The old 256 worked only
    /// because <c>minimal.toml</c> condemned after 64 Ticks, which <c>adr/0164</c> removed.
    /// <para>
    /// ⚠ <b>The 32× costs nothing, because the guard was the price and not the Ticks.</b> See
    /// <c>Report</c>: with <c>VerifyDecideWritesNothing</c> left on, this ran past ten minutes; off,
    /// it is about a second.
    /// </para>
    /// </remarks>
    private const int TickCount = 8_192;

    /// <summary>The reading cadence, chosen so the row count below is a fact rather than a guess.</summary>
    private const int Cadence = 64;

    /// <summary>
    /// <b>Every family the Census carries has a block, on <c>CensusReportTests</c>' reasoning.</b>
    /// </summary>
    /// <remarks>
    /// Named by the block's own title rather than by a type, because the title is what an operator
    /// greps for and a type name is not in the output at all. A family added to the Census and not to
    /// this report is the failure the sibling file was written about, and it is one report wider now.
    /// </remarks>
    [Theory]
    [InlineData("tables — live")]
    [InlineData("tables — slots")]
    [InlineData("tables — capacity")]
    [InlineData("rules —")]
    [InlineData("zones —")]
    [InlineData("placement —")]
    [InlineData("jobs —")]
    [InlineData("trips —")]
    [InlineData("trip cost —")]
    public void Every_census_family_has_a_block(string family) =>
        Assert.Contains($"\n{family}", Report(), StringComparison.Ordinal);

    /// <summary>
    /// <b>One row per reading, and the Tick on it is the Tick the Census stamped.</b>
    /// </summary>
    /// <remarks>
    /// <b>The axis is read out of the samples rather than recomputed, and this is what holds that.</b>
    /// <c>CensusSample</c> carries its own Tick precisely so no reader has to assume the cadence never
    /// moved; a report that multiplied the row index by <c>--hash-every</c> would agree with this
    /// assertion on every run where nothing went wrong, which is every run that does not need the
    /// report.
    /// </remarks>
    [Fact]
    public void Each_reading_is_one_row_stamped_with_its_own_tick()
    {
        string report = Report();
        string block = Block(report, "jobs —");

        string[] rows =
        [
            .. block.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.Length > 0 && char.IsAsciiDigit(line.TrimStart()[0])),
        ];

        Assert.Equal(TickCount / Cadence, rows.Length);

        for (int row = 0; row < rows.Length; row++)
        {
            string tick = rows[row].TrimStart().Split(' ')[0];
            string expected = ((row + 1) * Cadence).ToString("N0", CultureInfo.InvariantCulture);

            Assert.Equal(expected, tick);
        }
    }

    /// <summary>
    /// ⚠ <b>A column dropped for being flat is named in the footnote, with the value it held.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the whole licence for the collapse and it is the assertion worth having.</b> Ninety-six
    /// repetitions of one number is a report spending the reader on the thing it is least about — but a
    /// counter that is silently absent is indistinguishable from a counter that does not exist, which is
    /// <c>adr/0093</c>'s failure on the reporting side. Withheld and named is a different claim from
    /// dropped, and only the first one is honest. <c>citizen</c> is the case: <c>CommandKind.Populate</c>
    /// fixes the population and nothing in this fixture moves it, so it is flat by construction.
    /// </remarks>
    [Fact]
    public void A_flat_column_is_withheld_by_name_and_by_value()
    {
        string block = Block(Report(), "tables — live");

        Assert.Contains("held constant:", block, StringComparison.Ordinal);
        Assert.Contains(
            $"citizen {GoldenFixtures.Population:N0}", block, StringComparison.Ordinal);

        // And it is not in the table it was withheld from, or the footnote would be a second copy
        // rather than a substitute.
        string header = block.Split('\n').First(
    line => line.TrimStart().StartsWith("tick", StringComparison.Ordinal));

        Assert.DoesNotContain("citizen", header, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A moving column is in the table rather than in the footnote.</b>
    /// </summary>
    /// <remarks>
    /// The negative of the test above, and it is needed because a collapse that withheld
    /// <em>everything</em> would satisfy that one perfectly. <c>building</c> moves on this fixture
    /// because <c>rulesets/minimal.toml</c> condemns every dwelling 64 Ticks after it is raised, which
    /// its own header says it does on purpose.
    /// </remarks>
    [Fact]
    public void A_moving_column_is_a_column()
    {
        string block = Block(Report(), "tables — live");
        string header = block.Split('\n').First(
    line => line.TrimStart().StartsWith("tick", StringComparison.Ordinal));

        Assert.Contains("building", header, StringComparison.Ordinal);
        Assert.DoesNotContain("held constant: building", block, StringComparison.Ordinal);
    }

    /// <summary>The block beginning with the given title, up to the next blank line's block.</summary>
    private static string Block(string report, string title)
    {
        int start = report.IndexOf($"\n{title}", StringComparison.Ordinal);

        Assert.True(start >= 0, $"the report has no {title} block.");

        int end = report.IndexOf("\n\n", start + 1, StringComparison.Ordinal);

        return end < 0 ? report[start..] : report[start..end];
    }

    /// <summary>The series of a short run of the shipped Ruleset. <c>CensusReportTests</c>' fixture.</summary>
    /// <remarks>
    /// 🔴 <b><c>declining.toml</c> rather than <c>minimal.toml</c> since milestone 17, because both
    /// tests in this class need a column that MOVES.</b> A series report withholds a flat column by
    /// name; on a city where nothing is ever built or demolished every column is flat, so the report
    /// withheld all of them and both assertions failed looking for a column that was not there. That
    /// is a vacuous fixture rather than a broken report.
    /// <para>
    /// ⚠ <b>The content hash is COMPUTED here and a literal at <see cref="GoldenFixtures.RulesetHash"/>,
    /// and the difference is not style.</b> That one has to be a literal because a committed
    /// <c>session.borough</c> carries it, so editing the file is a re-baseline. This log is built in
    /// code and thrown away, so nothing outside this method records the number and pinning it would
    /// only mean a second thing to update.
    /// </para>
    /// </remarks>
    private static string Report()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            RulesetFile.HashOf(GoldenFixtures.DecliningRulesetPath));

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.DecliningRules());

        // ⚠ THE GUARD IS WHAT MADE THE LONGER RUN UNAFFORDABLE, not the Tick count. It folds the
        // whole world's State Hash twice a Tick against a phase meant to be O(woken), so 8,192 Ticks
        // ran past ten minutes with it on and takes seconds without -- the same ~75x CLAUDE.md
        // records for `--no-decide-guard`. Its own correctness has its own tests; this one is about
        // what the report prints.
        simulation.VerifyDecideWritesNothing = false;

        var census = new Census(simulation.World);

        Replay.Trace(simulation, log, new Ticks(TickCount), Cadence, [], census);

        var writer = new StringWriter();

        SeriesReport.Print(writer, simulation.World, census, TickCount);

        return writer.ToString();
    }
}
