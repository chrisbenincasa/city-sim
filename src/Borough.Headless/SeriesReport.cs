using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;

namespace Borough.Headless;

/// <summary>
/// Prints the Census ring as a time series: one block per family, one row per reading.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shape of every quantity this simulation keeps has been recorded since slice 5 and printed
/// by nothing.</b> <see cref="Census"/> holds a ring of readings stamped with their Tick and
/// <c>series(metric, window)</c> hands them back; <see cref="CensusReport"/> then collapses each
/// series into <em>first, last, low, high</em> — four numbers, which is the right summary for
/// <em>did this trend</em> and is silent on <em>when</em>. This report is the other reading, and the
/// two are kept apart for the reason the census and the hash trace are: one asks whether something
/// moved, the other asks what it did.
/// </para>
/// <para>
/// ⚠ <b>It is a diagnostic and not a summary, and the size is the tell.</b> Nine blocks at one row a
/// reading is hundreds of lines, which is why it is behind its own flag rather than folded into
/// <c>--census</c>. Reach for it when a run's aggregate is surprising and the question is which part
/// of the run produced it.
/// </para>
/// <para>
/// <b>A column that never moves is dropped to a footnote, with its value.</b> A time series exists to
/// show a shape and a constant has none, so ninety-six repetitions of one number is the report
/// spending the reader's attention on the thing it is least about. Nothing is lost — the footnote
/// names every column withheld and what it held — and the alternative was a tables block seventeen
/// columns wide of which two typically move.
/// </para>
/// <para>
/// ⚠ <b>A level and a flow are read differently here and the blocks say which they are.</b> A table
/// column is sampled <em>at</em> the reading, so a spike that rises and falls between two readings is
/// invisible in it; a counter column is accumulated <em>between</em> readings, so the same spike is in
/// the sum and named exactly by the <c>peak</c> column beside it. Reading a table column as though it
/// were a flow is the mistake this distinction exists to prevent.
/// </para>
/// </remarks>
internal static class SeriesReport
{
    /// <summary>The gap between columns, so a header and its rows cannot disagree about it.</summary>
    private const string Gap = "  ";

    public static void Print(TextWriter writer, World world, Census census, ulong ticks)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(census);

        var window = new Ticks(ticks);

        writer.WriteLine();
        writer.WriteLine(
            $"series — every counter against the Tick it was read on, over {Count(ticks)} Ticks, "
            + $"{Count((long)census.Count)} readings");

        // One block per counter rather than one for the whole family, because seventeen tables times
        // three counters is a row nobody can read across. The split is by counter and not by table so
        // that the columns of a block are commensurable: `live` across every table is a set of sizes,
        // where one table's three counters are a size, a high-water mark and an allocation.
        foreach ((CensusCounter counter, string label) in CensusFamilies.Counters)
        {
            var columns = new (string Label, Metric Metric)[census.Tables];

            for (int table = 0; table < census.Tables; table++)
            {
                columns[table] = (world.Tables[table].Name, Metric.Of(table, counter));
            }

            Block(writer, census, window, $"tables — {label} (a level, read at the sample)", columns);
        }

        Block(
            writer,
            census,
            window,
            "rules — a flow, accumulated between samples",
            [.. CensusFamilies.RuleCounters.Select(
                row => (row.Name, Metric.Of(row.Counter, row.Aggregate)))]);

        Block(
            writer,
            census,
            window,
            "zones — a flow, accumulated between samples",
            [.. CensusFamilies.ZoneCounters.Select(
                row => (row.Name, Metric.Of(row.Counter, row.Aggregate)))]);

        Block(
            writer,
            census,
            window,
            "placement — a flow, accumulated between samples",
            [.. CensusFamilies.PlacementCounters.Select(
                row => (row.Name, Metric.Of(row.Counter, row.Aggregate)))]);

        Block(
            writer,
            census,
            window,
            "jobs — a flow, accumulated between samples",
            [.. CensusFamilies.JobCounters.Select(
                row => (row.Name, Metric.Of(row.Counter, row.Aggregate)))]);

        Block(
            writer,
            census,
            window,
            "policies — a flow, accumulated between samples",
            [.. CensusFamilies.PolicyCounters.Select(
                row => (row.Name, Metric.Of(row.Counter, row.Aggregate)))]);

        Block(
            writer,
            census,
            window,
            "trips — a flow, accumulated between samples",
            [.. CensusFamilies.TripCounters.Select(
                row => (row.Name, Metric.Of(row.Counter, row.Aggregate)))]);

        Block(
            writer,
            census,
            window,
            "trip cost — a flow, accumulated between samples",
            [.. CensusFamilies.TripCosts.Select(
                row => (row.Name, Metric.Of(row.Bucket, Aggregate.Sum)))]);

        // Last, and the only block a reader checks for a SHAPE rather than for a size: a magnitude
        // that climbs for the whole run is adr/0006's one-way ratchet, and two endpoints cannot tell
        // that from one that settled early. This is the block that made *Sealing reaches a steady
        // state* answerable at all.
        Block(
            writer,
            census,
            window,
            "layers — a level, summed over the map and read at the sample",
            [.. CensusFamilies.LayerCounters.Select(row => (row.Name, Metric.Of(row.Counter)))]);
    }

    /// <summary>
    /// Prints one family: the readings down the page, the counters across it.
    /// </summary>
    /// <remarks>
    /// <b>The Tick column comes from the samples rather than from the cadence.</b>
    /// <see cref="CensusSample"/> carries the Tick it was taken on precisely so a reader need not
    /// assume the cadence held for the whole run, and a report that recomputed it from the row index
    /// would be making the assumption the type exists to remove.
    /// </remarks>
    private static void Block(
        TextWriter writer,
        Census census,
        Ticks window,
        string title,
        (string Label, Metric Metric)[] columns)
    {
        Series[] all = [.. columns.Select(column => census.Series(column.Metric, window))];

        writer.WriteLine();
        writer.WriteLine(title);

        if (all.Length == 0 || all[0].Count == 0)
        {
            writer.WriteLine("  (no readings)");
            return;
        }

        ReadOnlySpan<CensusSample> axis = all[0].Samples.Span;

        // Held apart from the moving ones before a width is computed, because a column that is about
        // to become a footnote must not set the width of the table it left.
        List<int> moving = [];
        List<string> constant = [];

        for (int column = 0; column < all.Length; column++)
        {
            ReadOnlySpan<CensusSample> samples = all[column].Samples.Span;

            if (Moves(samples))
            {
                moving.Add(column);
            }
            else
            {
                constant.Add($"{columns[column].Label} {Count(samples[0].Value)}");
            }
        }

        if (moving.Count == 0)
        {
            writer.WriteLine("  every column held constant for the whole run.");
            WriteConstants(writer, constant);
            return;
        }

        int tickWidth = Math.Max("tick".Length, Count(axis[^1].Tick.Raw).Length);
        var widths = new int[moving.Count];

        for (int k = 0; k < moving.Count; k++)
        {
            ReadOnlySpan<CensusSample> samples = all[moving[k]].Samples.Span;
            int width = columns[moving[k]].Label.Length;

            foreach (CensusSample sample in samples)
            {
                width = Math.Max(width, Count(sample.Value).Length);
            }

            widths[k] = width;
        }

        var header = new System.Text.StringBuilder();

        header.Append("tick".PadLeft(tickWidth));

        for (int k = 0; k < moving.Count; k++)
        {
            header.Append(Gap).Append(columns[moving[k]].Label.PadLeft(widths[k]));
        }

        writer.WriteLine(header.ToString());
        writer.WriteLine(new string('-', header.Length));

        for (int row = 0; row < axis.Length; row++)
        {
            var line = new System.Text.StringBuilder();

            line.Append(Count(axis[row].Tick.Raw).PadLeft(tickWidth));

            for (int k = 0; k < moving.Count; k++)
            {
                line.Append(Gap)
                    .Append(Count(all[moving[k]].Samples.Span[row].Value).PadLeft(widths[k]));
            }

            writer.WriteLine(line.ToString());
        }

        WriteConstants(writer, constant);

        if (!all[0].Complete)
        {
            writer.WriteLine(
                "  Readings before this window were discarded; the rows above are the tail of the "
                + "run. Raise --hash-every to cover it in one ring.");
        }
    }

    /// <summary>Whether a column has any shape at all, which is what earns it a place in the table.</summary>
    private static bool Moves(ReadOnlySpan<CensusSample> samples)
    {
        for (int i = 1; i < samples.Length; i++)
        {
            if (samples[i].Value != samples[0].Value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Names the columns that were withheld and what each held.
    /// </summary>
    /// <remarks>
    /// <b>Withheld rather than dropped, and the distinction is the whole licence for the collapse.</b>
    /// A reader who came looking for a specific counter has to be able to find out that it was read
    /// and did not move — otherwise the collapse is indistinguishable from the counter not existing,
    /// which is the failure <c>adr/0093</c> is about on the reporting side.
    /// </remarks>
    private static void WriteConstants(TextWriter writer, List<string> constant)
    {
        if (constant.Count == 0)
        {
            return;
        }

        writer.WriteLine($"  held constant: {string.Join(" · ", constant)}");
    }

    private static string Count(long value) => F($"{value:N0}");

    /// <inheritdoc cref="Count"/>
    private static string Count(ulong value) => F($"{value:N0}");

    /// <summary>Formats with the invariant culture, which <c>adr/0003</c> requires of every number here.</summary>
    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
