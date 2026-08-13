using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Headless;
using Borough.Tests.Golden;

namespace Borough.Tests.Headless;

/// <summary>
/// That every Census family reaches the report, which until 5b-bis task 6 two of them did not.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>A Census family with no reader is a family nobody can see, and this file exists because one
/// shipped that way.</b> Milestone 5b built <c>TripCounter</c>, wired all four Fate flows through
/// <c>Census.Observe</c>, tested them — and never added them to <c>--census</c>. For a whole milestone
/// the only way to read a Trip Fate was to write a test, which is a consumer no operator has. That is
/// the shape of the <c>adr/0064</c> finding on a different axis: not a fact with two copies that
/// drifted, but a mechanism whose only reader was the suite.
/// </para>
/// <para>
/// <b>Asserted as <i>the family appears</i> rather than as a number.</b> What the counters say is a
/// property of the fixture and belongs to the tests that own each family; what this file holds is
/// that a family which exists is printed, which no test of a counter can notice.
/// </para>
/// </remarks>
public sealed class CensusReportTests
{
    /// <summary>Long enough for the assignment pass and a few departure phases.</summary>
    private const int TickCount = 256;

    /// <summary>
    /// <b>Every family the Census carries has a block in the report.</b>
    /// </summary>
    /// <remarks>
    /// The families are named by their row label rather than by a type, because the label is what an
    /// operator greps for and a type name is not in the output at all. A family renamed here without
    /// being renamed in the report is a failing test rather than a silent disappearance.
    /// </remarks>
    [Theory]
    [InlineData("rules")]
    [InlineData("zones")]
    [InlineData("placement")]
    [InlineData("jobs")]
    [InlineData("trips")]
    [InlineData("trip cost")]
    public void Every_census_family_reaches_the_report(string family) =>
        Assert.Contains($"\n{family}", Report(), StringComparison.Ordinal);

    /// <summary>
    /// <b>All four Trip Fates are printed, including the two a walking city cannot produce.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0076</c> closes the Fate set at four, so a row each is the shape that needs no edit when
    /// the missing conditions arrive — and a zero beside a nonzero is informative in a way a missing
    /// row is not. Printing only the Fates a run happened to reach is the report deciding what the
    /// reader is allowed to conclude.
    /// </remarks>
    [Theory]
    [InlineData("completed")]
    [InlineData("no route")]
    [InlineData("over budget")]
    [InlineData("stranded")]
    public void Every_trip_fate_has_a_row(string fate) =>
        Assert.Contains(fate, Report(), StringComparison.Ordinal);

    /// <summary>
    /// <b>All seven cost bands are printed, empty ones included.</b>
    /// </summary>
    /// <remarks>
    /// A histogram with its empty bands omitted is not a histogram — the gap between two occupied
    /// bands is the shape, and a reader who cannot see the gap cannot see the distribution. This is
    /// the same argument as the Fates above and it is stronger here, because a missing band would
    /// silently change what the rows either side of it mean.
    /// </remarks>
    [Theory]
    [InlineData("under 1 min")]
    [InlineData("1-2 min")]
    [InlineData("2-4 min")]
    [InlineData("4-8 min")]
    [InlineData("8-16 min")]
    [InlineData("16-32 min")]
    [InlineData("32 min or none")]
    public void Every_cost_band_has_a_row(string band) =>
        Assert.Contains(band, Report(), StringComparison.Ordinal);

    /// <summary>The report of a short run of the shipped Ruleset.</summary>
    private static string Report()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.Rules());
        var census = new Census(simulation.World);

        Replay.Trace(simulation, log, new Ticks(TickCount), 64, [], census);

        var writer = new StringWriter();

        CensusReport.Print(writer, simulation.World, census, TickCount);

        return writer.ToString();
    }
}
