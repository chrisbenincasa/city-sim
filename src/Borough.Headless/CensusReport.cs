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

    public static void Print(TextWriter writer, World world, Census census, ulong ticks)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(census);

        writer.WriteLine();
        Write(writer, $"census — collection sizes over {ticks:N0} Ticks, {census.Count:N0} readings");
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
                Series series = census.Series(new Metric(table, counter), window);
                truncated |= !series.Complete;

                WriteRow(writer, name, label, series);
            }
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
        string head = F($"{table,-10}  {counter,-10}  {first,11}");
        string tail = F($"{last,11}  {low,11}  {high,11}");

        return $"{head}  {tail}";
    }

    private static string Count(long value) => F($"{value:N0}");

    /// <summary>Formats with the invariant culture, which <c>adr/0003</c> requires of every number here.</summary>
    private static void Write(TextWriter writer, FormattableString line) => writer.WriteLine(F(line));

    /// <inheritdoc cref="Write"/>
    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
