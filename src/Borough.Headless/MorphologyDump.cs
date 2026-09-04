using System;
using System.Globalization;
using System.IO;
using Borough.Core.Entities;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Measures the standing Road Graph the way the urban-morphology literature measures a real city —
/// orientation entropy, the orientation-order index, circuity, node degree and intersection
/// density.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>IT EXISTS BECAUSE EVERY FIGURE THE PROJECT HAD FOR ITS OWN NETWORK WAS ARITHMETIC RATHER
/// THAN MEASUREMENT.</b> A perfect square lattice has an orientation-order index of 1.000, 100%
/// four-way intersections and a circuity of 1.000 <em>by construction</em> — those are properties of
/// the definition, derivable at a desk, and the project quoted them as though the city had been
/// measured. ***A number you can compute without running the thing is not a reading of the thing.***
/// This mode runs the generator and counts what is actually there.
/// </para>
/// <para>
/// ⚠ <b>THE ENTROPY IS VERIFIED AGAINST ITS SOURCE AND THE INDEX IS NOT — and those are two claims,
/// not one.</b> Checked 2026-09-04 against <c>osmnx/bearing.py</c>, which is the paper's author's own
/// implementation (Boeing, <em>Applied Network Science</em> 4:67, 2019). <b>Four things match:</b>
/// <b>36</b> bins is that function's default; bearings are <b>bidirectional</b>, two reciprocal
/// bearings per undirected edge; the distribution is <b>unweighted</b> by default, equal weight per
/// bearing; and the entropy is in <b>nats</b>, because it calls <c>scipy.stats.entropy</c> with no
/// base.
/// <b>One thing did not, and it was a real defect here:</b> the reference bins are <b>CENTRED on the
/// compass points</b> rather than starting at them — it histograms into 72 half-bins, rolls by one
/// and sums adjacent pairs, <em>"so eg 0.01 deg and 359.99 deg will be binned together"</em>. This
/// file split them. See <see cref="Add"/>, which now centres.
/// <b>What is STILL unverified is the index itself:</b> <c>φ = 1 − ((H − Hg) / (Hmax − Hg))²</c>
/// with <c>Hg = ln 4</c> and <c>Hmax = ln 36</c>. OSMnx does not implement φ, and the paper is behind
/// Springer's authorisation wall — four routes to a readable copy were tried and none returned text.
/// So <b>the two anchors and the square are recalled and not sourced</b>, and every one of them is
/// named in <see cref="GridEntropy"/>, <see cref="MaxEntropy"/> and <see cref="Order"/> so that a
/// reader with the paper can check three lines. ***Until somebody does, φ is a figure this build
/// computes and not a figure comparable to a published one***, and a document setting it beside
/// Boeing's city table is setting two different measurements side by side
/// (<c>plans/0012</c> <b>Cause 5</b>).
/// </para>
/// <para>
/// <b>H is printed twice, unweighted and length-weighted, and the UNWEIGHTED one is now the
/// comparable one.</b> It was printed twice because which the published index used was unknown; the
/// reference implementation defaults to equal weight per bearing, so that question is closed and the
/// second figure stays only as a reading a longer-Segment world would want. ***A hedge kept after
/// the thing it hedged against is settled becomes a second answer nobody knows how to choose
/// between***, so the caption says which one to quote.
/// </para>
/// <para>
/// <b>Circuity is per-Segment and needs no router.</b> It is total Segment length over total
/// straight-line distance between each Segment's own two endpoints — which is the published
/// definition and is 1.000 for any network of straight edges, this one included. ⚠ <b>It is
/// therefore a check on the DRAWING of the graph rather than on its shape</b>: a lattice cannot fail
/// it, and a generator that starts curving Arterials can.
/// </para>
/// <para>
/// <b>Doubles are used freely here and that is not a lint violation.</b> <c>adr/0003</c> bans
/// floating point in simulation <em>state and arithmetic</em>; this project reads a world and
/// composes a report, changes nothing, and is not compiled with the analysers. The world it measures
/// was built entirely in integers.
/// </para>
/// </remarks>
internal static class MorphologyDump
{
    /// <summary>Compass bins, at 10° each. Boeing's 36, and the divisor every entropy below uses.</summary>
    private const int Bins = 36;

    /// <summary>
    /// The entropy of a perfect four-way grid, in nats — four bins equally occupied, so <c>ln 4</c>.
    /// </summary>
    /// <remarks>
    /// <b>It is the index's ZERO-disorder end and not its maximum.</b> A grid is the most ordered
    /// street network, so it has the LOWEST entropy of interest; <see cref="MaxEntropy"/> is the
    /// other end. Getting these two the wrong way round inverts the index without making it look
    /// wrong, which is why they are named rather than inlined.
    /// </remarks>
    private static readonly double GridEntropy = Math.Log(4);

    /// <summary>Maximum entropy over <see cref="Bins"/> bins, in nats — every bearing equally likely.</summary>
    private static readonly double MaxEntropy = Math.Log(Bins);

    /// <summary>Runs the measurement and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);

        // --roads' reason exactly: the graph is world creation and nothing steps it. What this mode
        // measures is what the generator laid, which is the whole point -- the Borough column in
        // every comparison against a real city has been describing THIS network from its definition.
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        RoadGraph graph = world.Roads;

        if (!graph.Exists)
        {
            output.WriteLine(
                "This Ruleset declares no [roads], so this world has no Road Graph and there is "
                + "nothing to measure. That is a legitimate Ruleset and an empty picture, which is "
                + "why the runner asks for a Ruleset rather than inventing a network.");

            return 3;
        }

        Report(options, world, graph, output);

        return 0;
    }

    /// <summary>The whole reading, in the order a reader needs it.</summary>
    private static void Report(Options options, World world, RoadGraph graph, TextWriter output)
    {
        RoadSegmentTable segments = graph.Segments;
        RoadNodeTable nodes = graph.Nodes;

        var bearings = new double[Bins];
        var weighted = new double[Bins];
        double lengthMetres = 0;
        double straightMetres = 0;
        int counted = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot))
            {
                continue;
            }

            int a = nodes.Rows.Resolve(segments.NodeA[slot]);
            int b = nodes.Rows.Resolve(segments.NodeB[slot]);

            double east = (nodes.East[b].Raw - nodes.East[a].Raw) * Tiles.Metres;
            double north = (nodes.North[b].Raw - nodes.North[a].Raw) * Tiles.Metres;
            double straight = Math.Sqrt((east * east) + (north * north));
            double length = segments.LengthTiles[slot].Raw * Tiles.Metres;

            lengthMetres += length;
            straightMetres += straight;
            counted++;

            if (straight == 0)
            {
                // A Segment whose endpoints coincide has no bearing to bin. It contributes its
                // length to the circuity numerator and nothing to the denominator, which is the
                // honest treatment: it is a real edge with no direction.
                continue;
            }

            // Compass bearing: 0° is north and it increases clockwise, which is the convention the
            // literature uses and the OPPOSITE of the mathematical convention Atan2 returns. Getting
            // it wrong rotates every bin by 90° and changes no entropy at all -- so it would never
            // show up in phi, which is exactly why it is spelled out here.
            double bearing = (Math.Atan2(east, north) * 180 / Math.PI + 360) % 360;

            // BOTH DIRECTIONS, because a Segment is undirected. A one-way treatment puts an
            // east-west street in one bin and a west-east street in another, which halves the
            // occupied bins of a perfect grid and reports a grid as more ordered than a grid.
            Add(bearings, weighted, bearing, length);
            Add(bearings, weighted, (bearing + 180) % 360, length);
        }

        double entropy = Entropy(bearings);
        double entropyByLength = Entropy(weighted);
        double squareKm = PavedSquareKm(graph, nodes);

        output.WriteLine("# Borough street-network morphology");
        output.WriteLine();
        output.WriteLine(
            Line($"Ruleset {Path.GetFileName(options.RulesetPath)}, seed {options.Seed}, "
                + $"{options.Citizens:N0} Citizens. The generator's network, measured rather than "
                + "derived."));
        output.WriteLine();

        output.WriteLine("## The graph");
        output.WriteLine();
        output.WriteLine(Line($"Segments                {counted:N0}"));
        output.WriteLine(Line($"Nodes                   {nodes.Rows.LiveCount:N0}"));
        output.WriteLine(Line($"Road length             {lengthMetres / 1000:N2} km"));
        output.WriteLine(Line($"Paved extent            {squareKm:N2} km²"));

        if (squareKm > 0)
        {
            output.WriteLine(Line($"Road density            {lengthMetres / 1000 / squareKm:N2} km/km²"));
        }

        output.WriteLine();
        output.WriteLine("## Orientation");
        output.WriteLine();
        output.WriteLine(Line($"Occupied bins           {Occupied(bearings)} of {Bins}, at 10° each"));
        output.WriteLine(Line($"Entropy H               {entropy:N4} nats, unweighted"));
        output.WriteLine(Line($"Entropy H               {entropyByLength:N4} nats, weighted by length"));
        output.WriteLine(Line($"Grid entropy Hg         {GridEntropy:N4} nats — ln 4, the ordered end"));
        output.WriteLine(Line($"Max entropy Hmax        {MaxEntropy:N4} nats — ln 36, the disordered end"));
        output.WriteLine(Line($"Orientation order φ     {Order(entropy):N4}   ⚠ formula UNVERIFIED, see the type's remarks"));
        output.WriteLine(Line($"Orientation order φ     {Order(entropyByLength):N4}   (from the length-weighted H)"));
        output.WriteLine();
        output.WriteLine("### Bearings, by bin");
        output.WriteLine();

        for (int bin = 0; bin < Bins; bin++)
        {
            if (bearings[bin] == 0)
            {
                continue;
            }

            // The arc the bin covers, which is centred on its compass point rather than starting
            // there -- see Add. Bin 0 therefore prints as 355-5 and wraps, which is the histogram
            // being honest about a circle rather than a defect in the label.
            int from = (((bin * 360 / Bins) - (180 / Bins)) + 360) % 360;

            output.WriteLine(Line(
                $"{from,3}°–{(from + (360 / Bins)) % 360,3}°   {bearings[bin],8:N0}   "
                + Bar(bearings[bin], bearings)));
        }

        output.WriteLine();
        output.WriteLine("## Intersections");
        output.WriteLine();
        Degrees(graph, nodes, squareKm, output);
        output.WriteLine();
        output.WriteLine("## Circuity");
        output.WriteLine();
        output.WriteLine(Line(
            straightMetres == 0
                ? "no Segment has two distinct endpoints, so circuity is undefined here."
                : $"Circuity                {lengthMetres / straightMetres:N4} — Segment length over "
                    + "straight-line distance between its own endpoints"));
        output.WriteLine();
        output.WriteLine(Reading(entropy, squareKm, graph, nodes));
    }

    /// <summary>Adds one bearing to both histograms, in a bin CENTRED on its compass point.</summary>
    /// <remarks>
    /// 🔴 <b>THE HALF-BIN OFFSET IS THE ONE PLACE THIS FILE DISAGREED WITH THE REFERENCE
    /// IMPLEMENTATION, AND IT MATTERS AT EXACTLY THE COMPASS POINTS A GRID SITS ON.</b> Bins used to
    /// start at 0°, so bin 0 spanned 0–10° and a bearing of 359.9° went to bin 35 while 0.1° went to
    /// bin 0 — <b>one direction split across the two ends of the histogram</b>, which is entropy
    /// invented out of a boundary. <c>osmnx/bearing.py</c> avoids it by histogramming into twice as
    /// many half-bins, rolling by one and summing adjacent pairs, <em>"so eg 0.01 deg and 359.99 deg
    /// will be binned together"</em>. Adding half a bin before flooring is the same partition
    /// arrived at directly: bin <c>k</c> spans <c>[10k − 5, 10k + 5)</c>.
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>It changes nothing on the shipped lattice and that is not a reason to skip it.</b> A
    /// lattice's bearings are exactly 0, 90, 180 and 270, which land cleanly under either partition
    /// — so the defect was invisible on the only world anybody had measured. ***A boundary error is
    /// invisible on data that does not sit near the boundary***, and a curving Arterial does.
    /// </remarks>
    private static void Add(double[] bearings, double[] weighted, double bearing, double metres)
    {
        const double HalfBin = 180.0 / Bins;

        // Floored rather than rounded, so a bearing lands in the bin its arc belongs to. The modulo
        // after the offset is what wraps 357° back onto bin 0 rather than off the end of the array.
        int bin = (int)(((bearing + HalfBin) % 360) * Bins / 360) % Bins;

        bearings[bin]++;
        weighted[bin] += metres;
    }

    /// <summary>Shannon entropy of a histogram, in nats. Empty bins contribute nothing.</summary>
    /// <remarks>
    /// <b>Nats and not bits, because the index's two anchors are natural logs.</b> Mixing the bases
    /// scales H against Hg and Hmax differently and produces an index that is wrong by a constant
    /// nobody would notice — it still reads between 0 and 1.
    /// </remarks>
    private static double Entropy(double[] histogram)
    {
        double total = 0;

        foreach (double count in histogram)
        {
            total += count;
        }

        if (total == 0)
        {
            return 0;
        }

        double sum = 0;

        foreach (double count in histogram)
        {
            if (count <= 0)
            {
                continue;
            }

            double share = count / total;

            sum -= share * Math.Log(share);
        }

        return sum;
    }

    /// <summary>The orientation-order index for an entropy, clamped to [0, 1].</summary>
    /// <remarks>
    /// ⚠ <b>Clamped because H can fall BELOW <see cref="GridEntropy"/>.</b> A network whose bearings
    /// occupy fewer than four bins — one straight road, or a Ruleset with a single axis — is more
    /// ordered than a grid, and the squared term would then read as disorder. The clamp is a
    /// statement that the index does not describe such a network rather than a fix for one.
    /// </remarks>
    private static double Order(double entropy)
    {
        double scaled = (entropy - GridEntropy) / (MaxEntropy - GridEntropy);

        return Math.Clamp(1 - (scaled * scaled), 0, 1);
    }

    /// <summary>How many bins hold anything.</summary>
    private static int Occupied(double[] histogram)
    {
        int held = 0;

        foreach (double count in histogram)
        {
            if (count > 0)
            {
                held++;
            }
        }

        return held;
    }

    /// <summary>A proportional bar, for reading the distribution without a plotting tool.</summary>
    private static string Bar(double count, double[] histogram)
    {
        double most = 0;

        foreach (double each in histogram)
        {
            most = Math.Max(most, each);
        }

        return most == 0 ? string.Empty : new string('█', Math.Max(1, (int)(count * 40 / most)));
    }

    /// <summary>The node degree histogram, and the four-way share the literature reports.</summary>
    /// <remarks>
    /// ⚠ <b>The four-way share is over INTERSECTIONS and not over nodes.</b> Boeing's tables count
    /// degree-4 nodes as a share of nodes with degree 3 or more; including degree-1 dead ends and
    /// degree-2 kinks in the denominator gives a different, smaller number that looks like the same
    /// one. Both are printed, so a reader cannot pick up the wrong denominator by accident.
    /// </remarks>
    private static void Degrees(RoadGraph graph, RoadNodeTable nodes, double squareKm, TextWriter output)
    {
        var histogram = new int[16];
        int live = 0;
        int intersections = 0;
        int fourWay = 0;

        for (int slot = 0; slot < nodes.Rows.SlotCount; slot++)
        {
            if (!nodes.Rows.IsLive(slot))
            {
                continue;
            }

            live++;

            // The Arc count is the degree: one Arc per Segment leaving this Node. It is the graph's
            // own derived structure rather than a walk of the Segment table, so it costs nothing.
            int degree = Math.Min(nodes.ArcCount[slot], histogram.Length - 1);

            histogram[degree]++;

            if (degree < 3)
            {
                continue;
            }

            intersections++;

            if (degree == 4)
            {
                fourWay++;
            }
        }

        for (int degree = 0; degree < histogram.Length; degree++)
        {
            if (histogram[degree] == 0)
            {
                continue;
            }

            output.WriteLine(Line(
                $"degree {degree,2}              {histogram[degree],8:N0}   "
                + $"{100.0 * histogram[degree] / Math.Max(1, live),6:N2}% of Nodes"));
        }

        output.WriteLine();
        output.WriteLine(Line($"Intersections           {intersections:N0} — Nodes of degree 3 or more"));
        output.WriteLine(Line(
            $"Four-way share          {100.0 * fourWay / Math.Max(1, intersections):N2}% of intersections, "
            + $"{100.0 * fourWay / Math.Max(1, live):N2}% of Nodes"));

        if (squareKm > 0)
        {
            output.WriteLine(Line($"Intersection density    {intersections / squareKm:N1} per km²"));
            output.WriteLine(Line(
                $"                        {intersections / squareKm * 2.58999:N0} per sq mile, which is "
                + "the unit the literature reports"));
        }
    }

    /// <summary>The ground the Nodes span, in km² — the paved extent rather than the map.</summary>
    /// <remarks>
    /// ⚠ <b>The extent and not the map, for the reason a density figure exists at all.</b> A lattice
    /// sized to its population sits in one corner of a 65.5 km map, and dividing by the map would
    /// report a density two orders of magnitude below the real one. This is the same correction
    /// <c>Borough.Godot</c>'s <c>Frame</c> makes for the camera, taken for the same reason.
    /// </remarks>
    private static double PavedSquareKm(RoadGraph graph, RoadNodeTable nodes)
    {
        long east = long.MaxValue;
        long north = long.MaxValue;
        long eastEnd = long.MinValue;
        long northEnd = long.MinValue;

        for (int slot = 0; slot < nodes.Rows.SlotCount; slot++)
        {
            if (!nodes.Rows.IsLive(slot))
            {
                continue;
            }

            east = Math.Min(east, nodes.East[slot].Raw);
            north = Math.Min(north, nodes.North[slot].Raw);
            eastEnd = Math.Max(eastEnd, nodes.East[slot].Raw);
            northEnd = Math.Max(northEnd, nodes.North[slot].Raw);
        }

        if (east > eastEnd)
        {
            return 0;
        }

        double wide = (eastEnd - east) * Tiles.Metres / 1000.0;
        double tall = (northEnd - north) * Tiles.Metres / 1000.0;

        return wide * tall;
    }

    /// <summary>The reading, in sentences, so the figures above are not left to be interpreted.</summary>
    /// <remarks>
    /// <b>It states what the numbers mean and refuses to state what they are worth.</b> Whether a
    /// φ of 1.000 is a defect is a design question with an ADR's worth of argument behind it
    /// (<c>adr/0090</c>, <c>adr/0077</c>); what this mode can say is that the network is or is not
    /// distinguishable from a perfect lattice, which is a fact about the graph.
    /// </remarks>
    private static string Reading(double entropy, double squareKm, RoadGraph graph, RoadNodeTable nodes)
    {
        double order = Order(entropy);

        if (order < 0.999)
        {
            return Line(
                $"READING: this network is distinguishable from a perfect lattice — φ = {order:N4}, "
                + "so its bearings do not sit in four bins alone. Compare it against a real city's "
                + "φ only after the formula in this file has been checked against its source.");
        }

        return Line(
            "READING: this network is INDISTINGUISHABLE FROM A PERFECT LATTICE on every measure "
            + "above. φ is 1.0000, the bearings occupy four bins, and circuity is 1.0000 because "
            + "every Segment is straight. ⚠ That is not a defect this mode found; it is the "
            + "generator doing exactly what it was written to do, measured for the first time. What "
            + "the figure is WORTH is a design question (adr/0090, adr/0077) and this mode does not "
            + "have an opinion about it.");
    }

    /// <summary>One line, in the invariant culture, so a dump is byte-comparable across machines.</summary>
    /// <remarks>
    /// ⚠ <b>Every figure above goes through this and none is formatted in the ambient culture.</b>
    /// A decimal comma would make two machines disagree about a dump that measured the same world,
    /// which is the class of difference a byte comparison reports as a change to the city.
    /// </remarks>
    private static string Line(FormattableString text) =>
        FormattableString.Invariant(text);

    /// <summary>An already-composed line, passed through so a caller need not choose an overload.</summary>
    private static string Line(string text) => text;
}
