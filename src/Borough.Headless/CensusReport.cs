using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Headless;

/// <summary>
/// Prints what every collection did over the length of a run.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>05 §1</c>'s "dumps aggregate series", and it goes to standard output rather than
/// into the trace.</b> The hash trace is a committed artefact diffed against another run, and a
/// census appended to it would change the file on every run whose sizes moved — which is every
/// interesting run, and would make the golden baseline unreviewable. They are two reports because
/// they are read by two people: one asks <em>did this change</em>, the other <em>what is it doing</em>.
/// </para>
/// <para>
/// <b>First, last, low and high rather than the whole series.</b> The claim anybody reads a census
/// for is about a trend, and a hundred readings printed in full is a thing nobody reads. The four
/// numbers are enough to see the two shapes that matter — <em>last above first</em> is growth, and
/// <em>high above last</em> is a peak that came back down, which is a collection with a working sink.
/// </para>
/// <para>
/// <b>Nothing here decides whether a series is acceptable.</b> That is an assertion, it needs a
/// definition of steady state, and the world has no churn to reach one with before slice 7. The
/// report states what happened and leaves the judgement with the reader.
/// </para>
/// </remarks>
internal static class CensusReport
{
    /// <summary>The counters, in the order <c>CensusCounter</c> declares them.</summary>
    /// <remarks>The shell owns every string a human reads (<c>adr/0002</c>), including these.</remarks>
    private static readonly (CensusCounter Counter, string Name)[] Counters =
    [
        (CensusCounter.Live, "live"),
        (CensusCounter.Slots, "slots"),
        (CensusCounter.Capacity, "capacity"),
    ];

    /// <summary>
    /// The Rule engine's counters, each read twice.
    /// </summary>
    /// <remarks>
    /// <b>These rows mean something different from the ones above them, and the labels have to carry
    /// that.</b> A table row is a size at the moment of the reading; a Rule row is a total <em>between</em>
    /// readings, so <c>first</c> and <c>last</c> are two intervals rather than two instants and
    /// <c>high</c> is the busiest interval rather than the largest the collection ever got. The
    /// <c>peak</c> rows are the finer question the sums cannot answer — the worst single Tick inside
    /// an interval, which is the figure a per-Tick budget is held against.
    /// </remarks>
    private static readonly (RuleCounter Counter, Aggregate Aggregate, string Name)[] RuleCounters =
    [
        (RuleCounter.Due, Aggregate.Sum, "due"),
        (RuleCounter.Due, Aggregate.Peak, "due peak"),
        (RuleCounter.Evaluations, Aggregate.Sum, "evaluations"),
        (RuleCounter.Evaluations, Aggregate.Peak, "evaluations peak"),
        (RuleCounter.ChainRungs, Aggregate.Sum, "chain rungs"),
        (RuleCounter.ChainRungs, Aggregate.Peak, "chain rungs peak"),
    ];

    /// <summary>
    /// The Sweep family's counters, on the same terms as the block above.
    /// </summary>
    /// <remarks>
    /// <b>Its own block for <c>adr/0033</c>'s reason.</b> A Zone Rule's trigger and a Bin Rule's due
    /// row are not the same kind of event — the two families differ in when their effect becomes
    /// visible within a Tick — so printing them in one block would be the layout claiming a
    /// commensurability the design denies. <c>evaluated</c> is <c>vacant + occupied</c> and is the
    /// quantity <c>02 §5.7</c>'s claim is about, which is why both halves are printed rather than
    /// their sum: their ratio is how full the city is, and no sum recovers it.
    /// </remarks>
    private static readonly (ZoneCounter Counter, Aggregate Aggregate, string Name)[] ZoneCounters =
    [
        (ZoneCounter.Triggers, Aggregate.Sum, "triggers"),
        (ZoneCounter.Vacant, Aggregate.Sum, "vacant"),
        (ZoneCounter.Occupied, Aggregate.Sum, "occupied"),
        (ZoneCounter.Created, Aggregate.Sum, "created"),
        (ZoneCounter.Created, Aggregate.Peak, "created peak"),
        (ZoneCounter.Demolished, Aggregate.Sum, "demolished"),
        (ZoneCounter.Demolished, Aggregate.Peak, "demolished peak"),
    ];

    /// <summary>
    /// The placement pass's counters (<c>adr/0069</c>), on the same terms as the blocks above.
    /// </summary>
    /// <remarks>
    /// <b>Both are printed because the gap between them is the reading.</b> <c>considered</c> without
    /// <c>placed</c> is a queue being looked at and not housed, which is a city out of dwellings;
    /// <c>placed</c> alone cannot tell that from a pass that has stopped running.
    /// </remarks>
    private static readonly (PlacementCounter Counter, Aggregate Aggregate, string Name)[]
        PlacementCounters =
    [
        (PlacementCounter.Considered, Aggregate.Sum, "considered"),
        (PlacementCounter.Placed, Aggregate.Sum, "placed"),
        (PlacementCounter.Placed, Aggregate.Peak, "placed peak"),
    ];

    /// <summary>
    /// The job assignment pass's counters, all four (<c>adr/0081</c>).
    /// </summary>
    /// <remarks>
    /// <b>All four rather than a selection, because each pair of them is a different subtraction and
    /// every one of those subtractions is a reading.</b> <c>considered − seeking</c> is what sampling
    /// the whole population costs against keeping a list of the unemployed; <c>seeking − employed</c>
    /// is the job shortage; and <c>beyond</c> is the only line in this report that describes the
    /// <em>network</em> — vacancies that existed, were found, and could not be walked to inside the
    /// ceiling. Printing three of the four would leave one of those unavailable.
    /// </remarks>
    /// <remarks>
    /// <b>The three rungs are printed beside <c>employed</c>, which they sum to</b> (<c>adr/0095</c>).
    /// They are the whole readable output of the grading: two of the three edges refuse nothing, so
    /// if these lines are not printed the only trace a graded Budget leaves in a run is the one
    /// number a binary Budget already left. <b>That is 5b-bis task 6's finding taken as an
    /// instruction</b> — <c>TripCounter</c> was built, wired, tested and printed nowhere for a whole
    /// milestone, so for a milestone its only reader was the suite.
    /// </remarks>
    private static readonly (JobCounter Counter, Aggregate Aggregate, string Name)[] JobCounters =
    [
        (JobCounter.Considered, Aggregate.Sum, "considered"),
        (JobCounter.Seeking, Aggregate.Sum, "seeking"),
        (JobCounter.Employed, Aggregate.Sum, "employed"),
        (JobCounter.Employed, Aggregate.Peak, "employed peak"),
        (JobCounter.Fast, Aggregate.Sum, "fast"),
        (JobCounter.Moderate, Aggregate.Sum, "moderate"),
        (JobCounter.Unsavoury, Aggregate.Sum, "unsavoury"),
        (JobCounter.Beyond, Aggregate.Sum, "beyond ceiling"),
    ];

    /// <summary>
    /// The four Trip Fates, and <b>they were built in 5b and printed by nothing until 5b-bis task
    /// 6.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>A Census family with no reader is a family nobody can see.</b> Milestone 5b added
    /// <c>TripCounter</c>, wired all four flows through <c>Census.Observe</c> and tested them — and
    /// left this report untouched, so the only way to read a Trip Fate was to write a test. That is
    /// the shape of <c>adr/0064</c>'s finding on a different axis: not a fact with two copies that
    /// drifted, but a mechanism whose <em>only</em> consumer was the suite, which is a consumer no
    /// operator has.
    /// </para>
    /// <para>
    /// <b>All four, including the two a walking-only city cannot produce.</b> <c>adr/0076</c> closes
    /// the Fate set at four, so a row each is the shape that needs no edit when the missing conditions
    /// arrive — and a zero beside a nonzero is informative in a way a missing row is not.
    /// </para>
    /// </remarks>
    private static readonly (TripCounter Counter, Aggregate Aggregate, string Name)[] TripCounters =
    [
        (TripCounter.Completed, Aggregate.Sum, "completed"),
        (TripCounter.Completed, Aggregate.Peak, "completed peak"),
        (TripCounter.NoRouteFound, Aggregate.Sum, "no route"),
        (TripCounter.ExceededCommuteBudget, Aggregate.Sum, "over budget"),
        (TripCounter.Stranded, Aggregate.Sum, "stranded"),
        (TripCounter.WalkLegs, Aggregate.Sum, "walk legs"),
        (TripCounter.DriveLegs, Aggregate.Sum, "drive legs"),
    ];

    /// <summary>
    /// The Trip cost histogram, one row per band of clock minutes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sums only, and the omission is the point.</b> A bucket's peak is <em>the busiest single Tick
    /// in that band</em>, which is a statement about the departure window rather than about how far
    /// anybody walks — and the departure window is already stated by the Ruleset. Printing fourteen
    /// rows to say what <c>commute_peak_factor</c> says would be the report competing with the file.
    /// </para>
    /// <para>
    /// ⚠ <b>This is not the distribution the Commute Budget is a percentile of.</b> A commute exists
    /// only because the assignment pass already accepted the job at the other end of it, inside the
    /// Budget — so the ceiling is <em>upstream</em> and this distribution is censored by the number it
    /// would be used to ratify. The uncensored one is <c>--trips</c>, which walks every Building pair
    /// and had to be taken before a Budget existed.
    /// </para>
    /// </remarks>
    private static readonly (TripCostBucket Bucket, string Name)[] TripCosts =
    [
        (TripCostBucket.UnderOneMinute, "under 1 min"),
        (TripCostBucket.UnderTwoMinutes, "1-2 min"),
        (TripCostBucket.UnderFourMinutes, "2-4 min"),
        (TripCostBucket.UnderEightMinutes, "4-8 min"),
        (TripCostBucket.UnderSixteenMinutes, "8-16 min"),
        (TripCostBucket.UnderThirtyTwoMinutes, "16-32 min"),
        (TripCostBucket.ThirtyTwoMinutesOrMore, "32 min or none"),
    ];

    public static void Print(TextWriter writer, World world, Census census, ulong ticks)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(census);

        writer.WriteLine();
        Write(
            writer,
            $"census — collection sizes and Rule activity over {ticks:N0} Ticks, {census.Count:N0} readings");
        writer.WriteLine();

        // Built through the same formatter as the rows, because a header aligned by hand is a header
        // that stops being aligned the first time a column width moves.
        string header = Row("table", "counter", "first", "last", "low", "high");

        writer.WriteLine(header);
        writer.WriteLine(new string('-', header.Length));

        // The window is the whole run. A window is how a panel asks for the recent past; a runner
        // that has just finished a run is asking about all of it.
        var window = new Ticks(ticks);
        bool truncated = false;

        for (int table = 0; table < census.Tables; table++)
        {
            string name = world.Tables[table].Name;

            foreach ((CensusCounter counter, string label) in Counters)
            {
                Series series = census.Series(Metric.Of(table, counter), window);
                truncated |= !series.Complete;

                WriteRow(writer, name, label, series);
            }
        }

        // The second family, and it is printed as its own block rather than interleaved because the
        // numbers are not commensurable: a table row is a level and a Rule row is a flow over the
        // interval between two readings.
        foreach ((RuleCounter counter, Aggregate aggregate, string label) in RuleCounters)
        {
            Series series = census.Series(Metric.Of(counter, aggregate), window);
            truncated |= !series.Complete;

            WriteRow(writer, "rules", label, series);
        }

        foreach ((ZoneCounter counter, Aggregate aggregate, string label) in ZoneCounters)
        {
            Series series = census.Series(Metric.Of(counter, aggregate), window);
            truncated |= !series.Complete;

            WriteRow(writer, "zones", label, series);
        }

        foreach ((PlacementCounter counter, Aggregate aggregate, string label) in PlacementCounters)
        {
            Series series = census.Series(Metric.Of(counter, aggregate), window);
            truncated |= !series.Complete;

            WriteRow(writer, "placement", label, series);
        }

        foreach ((JobCounter counter, Aggregate aggregate, string label) in JobCounters)
        {
            Series series = census.Series(Metric.Of(counter, aggregate), window);
            truncated |= !series.Complete;

            WriteRow(writer, "jobs", label, series);
        }

        foreach ((TripCounter counter, Aggregate aggregate, string label) in TripCounters)
        {
            Series series = census.Series(Metric.Of(counter, aggregate), window);
            truncated |= !series.Complete;

            WriteRow(writer, "trips", label, series);
        }

        foreach ((TripCostBucket bucket, string label) in TripCosts)
        {
            Series series = census.Series(Metric.Of(bucket, Aggregate.Sum), window);
            truncated |= !series.Complete;

            WriteRow(writer, "trip cost", label, series);
        }

        if (truncated)
        {
            string held = F($"{census.Capacity:N0}");
            string took = F($"{census.Taken:N0}");

            writer.WriteLine();
            writer.WriteLine(
                $"Readings before this window were discarded: the census holds {held} and this run "
                + $"took {took}. The figures above describe the tail of the run rather than all of "
                + "it — raise --hash-every to cover it in one ring.");
        }
    }

    private static void WriteRow(TextWriter writer, string table, string counter, Series series)
    {
        ReadOnlySpan<CensusSample> samples = series.Samples.Span;

        if (samples.IsEmpty)
        {
            writer.WriteLine(Row(table, counter, "—", "—", "—", "—"));
            return;
        }

        long low = samples[0].Value;
        long high = samples[0].Value;

        foreach (CensusSample sample in samples)
        {
            if (sample.Value < low)
            {
                low = sample.Value;
            }

            if (sample.Value > high)
            {
                high = sample.Value;
            }
        }

        writer.WriteLine(Row(
            table,
            counter,
            Count(samples[0].Value),
            Count(samples[^1].Value),
            Count(low),
            Count(high)));
    }

    /// <summary>The one place a column width is stated, so the header and the rows cannot disagree.</summary>
    private static string Row(
        string table, string counter, string first, string last, string low, string high)
    {
        string head = F($"{table,-14}  {counter,-16}  {first,11}");
        string tail = F($"{last,11}  {low,11}  {high,11}");

        return $"{head}  {tail}";
    }

    private static string Count(long value) => F($"{value:N0}");

    /// <summary>Formats with the invariant culture, which <c>adr/0003</c> requires of every number here.</summary>
    private static void Write(TextWriter writer, FormattableString line) => writer.WriteLine(F(line));

    /// <inheritdoc cref="Write"/>
    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
