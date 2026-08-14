using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Instruments;

/// <summary>
/// The Trip cost histogram: the shape of what the city walks, and what it is not evidence of.
/// </summary>
/// <remarks>
/// <para>
/// <b>The seventh Census family and the first that is one quantity at seven resolutions.</b> Every
/// other family names distinct events that share an owner; these seven name one event at seven
/// bands, and the reason a mean would not do is that the Commute Budget is a <em>percentile</em> —
/// a city of short walks with a long tail and a city of uniform medium walks have the same mean and
/// want different Budgets.
/// </para>
/// <para>
/// ⚠ <b>Nothing in this file ratifies the Commute Budget, and that is asserted rather than assumed.</b>
/// A commute exists only because the assignment pass already accepted the job at the other end of it,
/// inside the Budget, so this distribution is censored by the number it would be used to ratify. The
/// uncensored one is <c>--trips</c>' census over every Building pair, taken before a Budget existed.
/// </para>
/// </remarks>
public sealed class TripCostCensusTests
{
    /// <summary>Long enough for departures and for the assignment pass to have run several times.</summary>
    private const int TickCount = 512;

    private const int HashEvery = 1_024;

    // ---- the ladder -------------------------------------------------------------------------------

    /// <summary>
    /// <b>Each band's own boundary belongs to the band above it.</b>
    /// </summary>
    /// <remarks>
    /// Half-open upwards, which is the convention every histogram wants and none states. Asserted at
    /// the edges rather than in the middles because a mis-stated comparison is an off-by-one at the
    /// boundary and is invisible anywhere else.
    /// </remarks>
    [Theory]
    [InlineData(0, TripCostBucket.UnderOneMinute)]
    [InlineData(59, TripCostBucket.UnderOneMinute)]
    [InlineData(60, TripCostBucket.UnderTwoMinutes)]
    [InlineData(120, TripCostBucket.UnderFourMinutes)]
    [InlineData(240, TripCostBucket.UnderEightMinutes)]
    [InlineData(480, TripCostBucket.UnderSixteenMinutes)]
    [InlineData(960, TripCostBucket.UnderThirtyTwoMinutes)]
    [InlineData(1_920, TripCostBucket.ThirtyTwoMinutesOrMore)]
    [InlineData(100_000, TripCostBucket.ThirtyTwoMinutesOrMore)]
    public void The_ladder_puts_each_boundary_in_the_band_above(int seconds, TripCostBucket bucket) =>
        Assert.Equal(bucket, TripEngine.BucketOf(TravelTime.FromSeconds(seconds)));

    /// <summary>
    /// <b>An impassable cost is the last band rather than a band of its own.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="TripCounter.NoRouteFound"/> already counts it exactly, and a second counter of one
    /// event is <c>plans/0012</c> <i>Cause 1</i>. What the last band claims is <em>a journey nobody
    /// would make</em>, and an impossible one qualifies — the alternative, leaving it out, would break
    /// the property that the bands sum to the Trips that were created.
    /// </remarks>
    [Fact]
    public void An_impassable_cost_is_the_last_band()
    {
        Assert.Equal(
            TripCostBucket.ThirtyTwoMinutesOrMore, TripEngine.BucketOf(TravelTime.Impassable));
    }

    // ---- the family -------------------------------------------------------------------------------

    /// <summary>
    /// <b>The city's commutes reach the Census as a distribution rather than as a count.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for the family. Asserted as <i>more than one band is occupied</i>, which is
    /// the whole claim a histogram makes over a counter: a single occupied band is a number, and a
    /// number is what <c>TripCounter</c> already had.
    /// </remarks>
    [Fact]
    public void The_commutes_of_a_run_occupy_more_than_one_band()
    {
        TripCostProfile costs = Run(GoldenFixtures.Rules()).Trips.Drain().Costs;

        int occupied = 0;
        long total = 0;

        foreach (TripCostBucket bucket in Buckets)
        {
            occupied += costs[bucket].Sum > 0 ? 1 : 0;
            total += costs[bucket].Sum;
        }

        Assert.True(total > 0, "the run made no Trips at all.");
        Assert.True(occupied > 1, $"every Trip in the run fell in one band of {total}.");
    }

    /// <summary>
    /// <b>The bands sum to the Trips that were <em>created</em>, and the Fates to the Trips that
    /// <em>ended</em> — so the difference is exactly what is still walking.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test of <i>counted at creation rather than at completion</i>, and it is an equality rather
    /// than an inequality because the two families are complete: every Trip lands in exactly one band
    /// when it is made, and in exactly one Fate when it ends. A Trip refused for its length or with no
    /// front door is in both, having been created and resolved on the same Tick.
    /// </para>
    /// <para>
    /// <b>What it rules out is a histogram of completed Trips</b>, which would be censored twice —
    /// once upstream by the assignment pass's Budget and again by the Fate — and the second censoring
    /// is the one that would hide the shape of the refusal.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_bands_count_trips_made_and_the_fates_count_trips_ended()
    {
        Simulation simulation = Run(GoldenFixtures.Rules());
        TripActivity activity = simulation.Trips.Drain();

        long made = 0;

        foreach (TripCostBucket bucket in Buckets)
        {
            made += activity.Costs[bucket].Sum;
        }

        long ended = activity.Completed.Sum
            + activity.NoRouteFound.Sum
            + activity.ExceededCommuteBudget.Sum
            + activity.Stranded.Sum;

        Assert.True(made > 0, "the run made no Trips at all.");
        Assert.Equal(made, ended + simulation.World.Trips.Rows.LiveCount);
    }

    /// <summary>
    /// ⚠ <b>Tightening the Commute Budget empties this distribution rather than filling its tail, and
    /// that is the whole reason it cannot ratify the Budget.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The obvious experiment does not work, and finding out why is task 6's finding.</b> The
    /// intuitive test of a cost histogram is <i>lower the ceiling and watch Trips pile up above it</i>.
    /// They do not pile up: a commute exists only because the assignment pass <em>already accepted</em>
    /// the job at the other end of it, inside the Budget, so lowering the Budget removes the
    /// <em>acceptances</em>. At one minute the golden fixture makes twenty commutes and <b>every one of
    /// them is inside the shortest bands the ceiling allows</b> — the distribution collapses toward
    /// zero instead of growing a tail. ⚠ <b>Three minutes rather than one, because <c>adr/0095</c> put
    /// a floor under the ceiling</b>: three strictly increasing rungs of at least a minute each make
    /// three the tightest set anybody can author, so the assertion is now <i>nothing above the band
    /// the ceiling falls in</i> rather than <i>nothing above a minute</i>. The shape of the finding is
    /// unchanged and the resolution of the instrument is what moved.
    /// </para>
    /// <para>
    /// <b>So the ceiling is upstream, and a distribution censored by a number cannot be evidence about
    /// it.</b> The uncensored distribution is <c>--trips</c>' census over every Building pair, which
    /// task 3 insisted be taken before any Budget was set — and this test is the proof that the
    /// insistence was right, because there is now no way to take that reading again from inside a run.
    /// </para>
    /// </remarks>
    [Fact]
    public void Tightening_the_budget_empties_the_distribution_rather_than_filling_its_tail()
    {
        TripCostProfile shipped = Run(GoldenFixtures.Rules()).Trips.Drain().Costs;
        TripCostProfile tight = Run(WithCeiling(3)).Trips.Drain().Costs;

        Assert.True(
            Above(shipped, TripCostBucket.UnderFourMinutes) > 0,
            "the shipped ceiling produced no Trip over four minutes, so there is no tail to collapse.");
        Assert.True(
            tight[TripCostBucket.UnderOneMinute].Sum > 0, "a three-minute ceiling made no Trips.");
        Assert.Equal(0, Above(tight, TripCostBucket.UnderFourMinutes));
    }

    /// <summary>
    /// <b>The bands reach a <see cref="Census"/> reading and come back out under their own metric.</b>
    /// </summary>
    /// <remarks>
    /// The addressing test, and it is worth having because the family was appended to a layout whose
    /// bases are computed from the ones before it: a base off by one reads a neighbour's counter and
    /// returns a plausible number.
    /// </remarks>
    [Fact]
    public void A_band_reaches_the_census_under_its_own_metric()
    {
        Simulation simulation = Run(GoldenFixtures.Rules());
        TripActivity activity = simulation.Trips.Drain();

        var census = new Census(simulation.World);

        census.Observe(simulation.World, simulation.Tick, default, default, default, activity);

        foreach (TripCostBucket bucket in Buckets)
        {
            Series series = census.Series(Metric.Of(bucket, Aggregate.Sum), new Ticks(1));

            Assert.Equal(1, series.Count);
            Assert.Equal(activity.Costs[bucket].Sum, series.Samples.Span[0].Value);
        }
    }

    /// <summary>
    /// <b>A cost metric is not a Trip Fate metric, and asking the wrong family throws.</b>
    /// </summary>
    /// <remarks>
    /// <c>Metric</c>'s own rule: the accessors for the family a metric is not are errors rather than
    /// defaults, because a zero would be a valid counter of the other family and would read as its
    /// first member.
    /// </remarks>
    [Fact]
    public void A_cost_metric_does_not_answer_as_a_fate()
    {
        Metric metric = Metric.Of(TripCostBucket.UnderEightMinutes, Aggregate.Sum);

        Assert.Equal(MetricSource.TripCosts, metric.Source);
        Assert.Equal(TripCostBucket.UnderEightMinutes, metric.TripCostBucket);
        Assert.Throws<InvalidOperationException>(() => metric.TripCounter);
        Assert.Throws<InvalidOperationException>(() => metric.Table);
    }

    // ---- the fixture ------------------------------------------------------------------------------

    private static TripCostBucket[] Buckets =>
    [
        TripCostBucket.UnderOneMinute,
        TripCostBucket.UnderTwoMinutes,
        TripCostBucket.UnderFourMinutes,
        TripCostBucket.UnderEightMinutes,
        TripCostBucket.UnderSixteenMinutes,
        TripCostBucket.UnderThirtyTwoMinutes,
        TripCostBucket.ThirtyTwoMinutesOrMore,
    ];

    /// <summary>Trips created above the band <paramref name="ceiling"/> falls in.</summary>
    /// <remarks>
    /// The ladder is <c>TripCostBucket</c>'s and is deliberately <em>not</em> denominated in the
    /// Budget — <em>a ruler must not move with the thing it measures</em> — so a test asking about a
    /// ceiling has to find which band the ceiling lands in rather than assume one.
    /// </remarks>
    private static long Above(TripCostProfile costs, TripCostBucket ceiling)
    {
        long above = 0;

        foreach (TripCostBucket bucket in Buckets)
        {
            above += bucket <= ceiling ? 0 : costs[bucket].Sum;
        }

        return above;
    }

    /// <summary>Trips created over a run, read off the histogram.</summary>
    private static long Made(Ruleset rules)
    {
        TripCostProfile costs = Run(rules).Trips.Drain().Costs;
        long made = 0;

        foreach (TripCostBucket bucket in Buckets)
        {
            made += costs[bucket].Sum;
        }

        return made;
    }

    private static Simulation Run(Ruleset rules)
    {
        InputLog log = Log();
        Simulation simulation = Replay.Start(log, rules);

        Replay.Trace(simulation, log, new Ticks(TickCount), HashEvery, []);

        return simulation;
    }

    private static InputLog Log()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        return builder.Build();
    }

    /// <summary>The shipped Ruleset with its Commute Budget's ceiling replaced.</summary>
    /// <remarks>
    /// <b>All three rungs are substituted, not just the ceiling</b> (<c>adr/0095</c>). The shipped
    /// file states 20/40/50, so replacing only the last key with anything below 40 produces a set the
    /// loader refuses — the substitution would fail for exactly the tight ceilings these tests exist
    /// to try. The lower rungs go to 1 and 2 because these assertions are about the <em>ceiling</em>,
    /// which is the only edge that refuses anything, and a rung that grades nothing cannot affect
    /// them. <b>Three is therefore the tightest ceiling any test can ask for</b>, since the rungs must
    /// strictly increase from at least a minute.
    /// </remarks>
    private static Ruleset WithCeiling(int minutes)
    {
        Assert.True(minutes >= 3, "the tightest authorable ceiling is 3 minutes (adr/0095).");

        string toml = File.ReadAllText(GoldenFixtures.RulesetPath);
        (string Key, string Replacement)[] keys =
        [
            ("commute_fast_minutes = 20", "commute_fast_minutes = 1"),
            ("commute_moderate_minutes = 40", "commute_moderate_minutes = 2"),
            ("commute_budget_minutes = 50", $"commute_budget_minutes = {minutes}"),
        ];

        foreach ((string key, string replacement) in keys)
        {
            Assert.Contains(key, toml, StringComparison.Ordinal);
            toml = toml.Replace(key, replacement, StringComparison.Ordinal);
        }

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
