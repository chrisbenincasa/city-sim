using System.Globalization;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// That <c>--morphology</c> measures the network it was given rather than restating the lattice's
/// definition.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>THE DEFECT THIS MODE EXISTS AGAINST IS ONE THIS PROJECT COMMITTED, IN PROSE, FOR MONTHS.</b>
/// A perfect square lattice has an orientation-order index of 1.000, 100% four-way intersections and
/// a circuity of 1.000 — all three derivable from the definition at a desk — and those figures were
/// quoted beside a published table of real cities as though the city had been measured.
/// ***The first run refuted all three***: φ came out 0.9974, four-way 64.29% and circuity 0.9998 on
/// <c>minimal.toml</c> at 4,000 Citizens. So the assertions below are that the mode reports a
/// MEASUREMENT — a value that moves with the world — and never the ideal.
/// </para>
/// <para>
/// ⚠ <b>No figure here is pinned, and that is the point rather than a weakness.</b> Every number
/// this mode prints moves with the generator's dice, the population and the Ruleset, so an equality
/// assertion would be a snapshot that teaches its reader to re-record it. What is asserted is
/// structural: the four-way share rises with population because the perimeter's degree-3 Nodes shrink
/// as a share of the whole, and a lattice-plus-Arterials world occupies more compass bins than a
/// lattice. Both would break if the mode started printing the definition again.
/// </para>
/// <para>
/// <b>It is an ASSERTION and not an instrument, and the distinction is the tier rule's own test —
/// <em>what would you do on the day it failed</em>.</b> The mode <em>produces</em> figures a document
/// may quote, which sounds like an instrument; what these tests check is that it produces figures at
/// all rather than constants, and the answer to one failing is <em>find out what broke in the
/// generator</em>. A test that pinned φ would be the instrument, and there is deliberately none.
/// </para>
/// </remarks>
public sealed class MorphologyDumpTests
{
    /// <summary>
    /// <b>The measured network is NOT the perfect lattice its definition describes.</b>
    /// </summary>
    /// <remarks>
    /// The three ideals are asserted away one at a time rather than together, so a failure names
    /// which of them came back. ⚠ <b>Every one of these was a figure the corpus had already
    /// published</b> — see the class remark — which is why they are tested as
    /// <em>not the ideal</em> rather than as <em>near it</em>.
    /// </remarks>
    [Fact]
    public void The_generated_network_measures_short_of_a_perfect_lattice()
    {
        string report = Dump("minimal.toml", "4000");

        Assert.True(Figure(report, "Orientation order φ") < 1.0, report);
        Assert.True(Figure(report, "Four-way share") < 100.0, report);
        Assert.True(Figure(report, "Occupied bins") > 4, report);
    }

    /// <summary>
    /// <b>The four-way share RISES with population, which is the perimeter thinning out.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the caveat that keeps the headline honest.</b> A small lattice is mostly edge —
    /// its boundary Nodes have degree 3 — so a four-way share well under 100% is a reading of the
    /// city's SIZE and not of its variety. Measured 2026-09-04: 64.29% at 4,000 Citizens, 76.04% at
    /// 16,000, 84.03% at 64,000. ***A figure that converges on the ideal as the world grows is
    /// evidence about the boundary and not about the shape***, and a reader given only the smallest
    /// of the three would conclude the opposite of what is true.
    /// </remarks>
    [Fact]
    public void The_four_way_share_rises_with_the_city()
    {
        double small = Figure(Dump("minimal.toml", "4000"), "Four-way share");
        double large = Figure(Dump("minimal.toml", "64000"), "Four-way share");

        Assert.True(
            large > small,
            $"four-way share was {small} at 4,000 Citizens and {large} at 64,000. It should rise: a "
            + "small lattice is mostly perimeter, and perimeter Nodes have degree 3. If this fell, "
            + "either the generator stopped laying a lattice or the degree is being read wrongly.");
    }

    /// <summary>
    /// <b>Arterials spread the bearings and barely move the index, and both halves matter.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE FINDING THAT CUTS AGAINST THE ARGUMENT FOR BUILDING THEM.</b>
    /// <c>rulesets/bordered.toml</c> is the one shipped file that sets <c>arterial_count = 16</c>,
    /// and its curving Arterials occupy <b>all 36</b> compass bins where the pure lattice occupies
    /// six. That is real orientation diversity. <b>And φ does not notice</b>: 0.9982 against the
    /// lattice's 0.9983, because half a million lattice Streets drown a handful of Arterials in the
    /// distribution.
    /// </para>
    /// <para>
    /// ⚠ <b>So this asserts the bin count and deliberately asserts NOTHING about φ.</b> Whether that
    /// insensitivity is a true property of the measure or a defect in an implementation whose formula
    /// is unverified against its source is exactly what nobody has checked — <c>MorphologyDump</c>'s
    /// own remarks say so. ***A test written on top of an unverified formula would freeze the
    /// uncertainty into the suite.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Arterials_occupy_more_of_the_compass_than_a_lattice()
    {
        double lattice = Figure(Dump("minimal.toml", "16000"), "Occupied bins");
        double arterial = Figure(Dump("bordered.toml", "16000"), "Occupied bins");

        Assert.True(
            arterial > lattice,
            $"the lattice occupied {lattice} compass bins and the world with Arterials occupied "
            + $"{arterial}. A curving Arterial has bearings a square lattice does not, so this is "
            + "either the Arterials no longer being laid or the bearing no longer being computed "
            + "from the Segment's own endpoints.");
    }

    /// <summary>
    /// <b>The reading refuses to call the figure good or bad, which is the NO VERDICT rule.</b>
    /// </summary>
    /// <remarks>
    /// Whether a lattice-shaped city is a defect is a design question with two ADRs behind it
    /// (<c>adr/0090</c>, <c>adr/0077</c>). An instrument that answered it would be stating a
    /// conclusion its numbers cannot support — <c>--roads</c>' own shipped defect, where a verdict
    /// fired on a count that could not carry it.
    /// </remarks>
    [Fact]
    public void The_reading_states_what_was_measured_and_not_what_it_is_worth()
    {
        string report = Dump("minimal.toml", "4000");

        Assert.Contains("READING:", report, StringComparison.Ordinal);
        Assert.Contains("UNVERIFIED", report, StringComparison.Ordinal);
    }

    /// <summary>The fabric report separates assigned ground from Buildings actually standing.</summary>
    [Fact]
    public void Urban_fabric_reports_the_three_ground_surfaces_by_pattern()
    {
        string sparse = Dump("minimal.toml", "1000");
        string varied = Dump("platted.toml", "10000");

        Assert.Contains("## Urban fabric", sparse, StringComparison.Ordinal);
        Assert.Contains("Parcel/block", sparse, StringComparison.Ordinal);
        Assert.Contains("Potential/block", sparse, StringComparison.Ordinal);
        Assert.Contains("Standing/block", sparse, StringComparison.Ordinal);
        Assert.Contains("Detached", sparse, StringComparison.Ordinal);
        Assert.Contains("Courtyard", varied, StringComparison.Ordinal);
        Assert.Contains("Tower", varied, StringComparison.Ordinal);

        double[] detached = FabricRow(sparse, "Detached");
        double[] slab = FabricRow(varied, "Slab");
        double[] tower = FabricRow(varied, "Tower");

        Assert.True(detached[2] > detached[3] && detached[3] > detached[4], sparse);
        Assert.True(slab[4] > tower[4], varied);
    }

    /// <summary>The five numeric columns on one Urban-fabric pattern row.</summary>
    private static double[] FabricRow(string report, string pattern)
    {
        string line = Array.Find(
            report.Split('\n'), each => each.TrimStart().StartsWith(pattern, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"the dump printed no '{pattern}' fabric row:\n{report}");

        return line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1..]
            .Select(each => double.Parse(
                each.TrimEnd('%').Replace(",", string.Empty, StringComparison.Ordinal),
                CultureInfo.InvariantCulture))
            .ToArray();
    }

    /// <summary>
    /// The figure printed after <paramref name="label"/>, parsed rather than substring-matched.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>TripDumpTests.Unreachable</c>'s rule, applied on the day rather than after the
    /// failure</b>: a number is the thing being claimed, so a number is what to read. Group
    /// separators are stripped because the report prints them and the reader is asserting on
    /// magnitudes, not on formatting.
    /// </remarks>
    private static double Figure(string report, string label)
    {
        string line = Array.Find(
            report.Split('\n'), each => each.TrimStart().StartsWith(label, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"the dump printed no '{label}' line:\n{report}");

        string tail = line.TrimStart()[label.Length..].TrimStart();
        int end = 0;

        while (end < tail.Length && (char.IsDigit(tail[end]) || tail[end] is '.' or ','))
        {
            end++;
        }

        return double.Parse(
            tail[..end].Replace(",", string.Empty, StringComparison.Ordinal),
            CultureInfo.InvariantCulture);
    }

    /// <summary>One run of the mode, as a string.</summary>
    private static string Dump(string ruleset, string citizens)
    {
        Assert.True(
            Options.TryParse(
                ["--morphology", "--ruleset",
                 Path.Combine(AppContext.BaseDirectory, "Rulesets", ruleset),
                 "--citizens", citizens],
                out Options? options,
                out string? complaint),
            complaint);

        var writer = new StringWriter();

        Assert.Equal(0, MorphologyDump.Run(options!, writer));

        return writer.ToString();
    }
}
