using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Slice 5a's artefact: the Road Graph, and what it is connected to itself by.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>06</c>'s rule that a milestone must have something to <em>look at</em>, and the thing worth
/// looking at here is not the picture.</b> A Zone dump shows a city thinning out and a Layer dump
/// shows a plume; a road network laid at world creation and never edited would print the same grid
/// twice however long a session ran. So this prints a graph and its <b>component counts per mode</b>,
/// and the number that carries the milestone is the second one: when the foot components outnumber
/// the car components, an Arterial has cut a neighbourhood off, and that is
/// <c>CONTEXT.md</c> → Severance visible for the first time in this project.
/// </para>
/// <para>
/// <b>It refuses without a Ruleset rather than degrading</b>, which is <c>--zones</c>' precedent
/// exactly: a road network is content, and an empty graph would read as a broken mechanism rather
/// than as a file that declares no <c>[roads]</c>.
/// </para>
/// </remarks>
internal static class RoadDump
{
    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
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

        // The generator runs inside the populator, because a road network is world creation and
        // belongs in the Input Log for the reason the population does (S0a). Nothing is stepped: the
        // graph is laid once and nothing yet edits it.
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        RoadGraph graph = world.Roads;

        if (!graph.Exists)
        {
            output.WriteLine(
                "This Ruleset declares no [roads], so this world has no Road Graph. That is a "
                + "legitimate Ruleset and an empty picture, which is why the runner asks for a "
                + "Ruleset rather than inventing a network.");

            return 3;
        }

        RoadRuleset roads = graph.Ruleset;

        output.WriteLine("# Borough Road Graph dump");
        output.WriteLine(
            $"# {graph.Nodes.Rows.LiveCount} nodes, {graph.Segments.Rows.LiveCount} Segments, "
            + $"{graph.Arcs.Count} Arcs, {graph.RoadLengthTiles()} Tiles of road.");
        output.WriteLine(
            $"# block {roads.BlockTiles} Tiles, {roads.ArterialCount} Arterials at "
            + $"{roads.ArterialJunctionTiles} Tiles a Junction, a foot crossing every "
            + $"{roads.FootCrossingEvery} severed Street, {roads.FootPathsPerThousandBlocks} "
            + "cut-throughs per thousand blocks.");

        output.WriteLine();
        output.WriteLine("## Segments, by kind");
        Kinds(output, graph);

        output.WriteLine();
        output.WriteLine("## What each mode may use");
        output.WriteLine(
            $"Car:  {graph.SegmentsAdmitting(TravelMode.Car)} Segments, "
            + $"{Arcs(graph, TravelMode.Car)} Arcs.");
        output.WriteLine(
            $"Foot: {graph.SegmentsAdmitting(TravelMode.Foot)} Segments, "
            + $"{Arcs(graph, TravelMode.Foot)} Arcs.");

        output.WriteLine();
        output.WriteLine("## Connectivity — the number this milestone is for");
        int live = graph.Nodes.Rows.LiveCount;

        output.WriteLine(
            $"Car subgraph:  {graph.Connectivity.CarComponents,5} component(s), largest holds "
            + $"{graph.Connectivity.LargestCar} of {live} nodes.");
        output.WriteLine(
            $"Foot subgraph: {graph.Connectivity.FootComponents,5} component(s), largest holds "
            + $"{graph.Connectivity.LargestFoot} of {live} nodes.");
        output.WriteLine(
            "The largest is printed beside the count because the count alone cannot be read: eight "
            + "components is a city in eight pieces or a city in one piece with seven stranded "
            + "corners, and those are opposite diagnoses.");
        output.WriteLine();
        output.WriteLine(
            $"Walkable nodes (a foot Segment touches them): {graph.Connectivity.WalkableNodes} of "
            + $"{live}. The rest are Arterial junctions, which is why the foot component count above "
            + "is roughly the number of Arterial junctions and moves with the Arterial count rather "
            + "than with Severance.");
        output.WriteLine(
            $"Cut off from the largest pedestrian piece: {graph.Connectivity.StrandedOnFoot} "
            + "walkable node(s). THIS is Severance.");
        output.WriteLine();
        output.WriteLine(Severance(graph));

        if (options.Csv)
        {
            output.WriteLine();
            output.WriteLine("## Segments");
            Csv(output, graph);
        }

        return 0;
    }

    /// <summary>The reading, in a sentence, so the numbers above are not left to be interpreted.</summary>
    /// <summary>The reading, in a sentence, so the numbers above are not left to be interpreted.</summary>
    /// <remarks>
    /// <para>
    /// <b>⚠ This compared component <em>counts</em> until 2026-08-11, and it therefore announced
    /// Severance over the shipped Ruleset, which severs nothing.</b> The count includes every
    /// Arterial junction as its own foot component — 65 of the shipped world's 66 — so the banner
    /// fired on a number that rises with the Arterial count and never with Severance. The type's own
    /// caveat, printed two lines above, says the count alone cannot be read, and then it read it.
    /// </para>
    /// <para>
    /// <b>No threshold, deliberately.</b> A share below which severance "does not count" would be a
    /// hash-free number nobody ratified (<c>adr/0052</c>'s spirit on a diagnostic), and the honest
    /// alternative is to print the count inside the sentence and let a reader see that two nodes is
    /// two nodes.
    /// </para>
    /// </remarks>
    private static string Severance(RoadGraph graph)
    {
        int stranded = graph.Connectivity.StrandedOnFoot;
        int walkable = graph.Connectivity.WalkableNodes;

        if (stranded > 0)
        {
            return $"SEVERANCE: {stranded} of {walkable} walkable nodes are cut off from the largest "
                 + "pedestrian piece, and the car network reaches them. Nobody deleted a pedestrian "
                 + "route — the mode mask simply never granted one. " + Dial + Detour;
        }

        return $"No Severance: all {walkable} walkable nodes are in one pedestrian piece. " + Dial
             + Detour;
    }

    /// <summary>
    /// How to move the number, stated as the mechanism rather than as advice about one dial.
    /// </summary>
    /// <remarks>
    /// <b>The advice this replaces was <i>"raise foot_crossing_every to see this get worse"</i>, and
    /// at the shipped Ruleset that is false.</b> A sweep of 240 configurations found the shipped
    /// 32-Tile lattice unmoved by every value in <c>1..16</c>, because the dial states a ratio and
    /// what reconnects a city is an absolute count. Printing a lever that does nothing is worse than
    /// printing none: the reader concludes the mechanism is broken.
    /// </remarks>
    private const string Dial =
        "What reconnects a city is the ABSOLUTE NUMBER of crossings kept, not the ratio "
        + "foot_crossing_every states: an Arterial severs one Street per block it crosses, so a "
        + "finer lattice hands it more Streets to sever and the same ratio leaves more crossings "
        + "standing. At eight Arterials and a dial of 4 that is 306 crossings at block_tiles = 32 "
        + "and 16 at 512. If the dial seems inert, the lattice is too fine for the Arterials on it; "
        + "rulesets/severance.toml is a rung where it does graduated work. ";

    /// <summary>The half of Severance this instrument cannot see, printed either way.</summary>
    /// <remarks>
    /// <b>Stated on the no-Severance branch as well, which is the branch that would otherwise
    /// mislead.</b> <i>No Severance</i> over a metric that measures only disconnection reads as
    /// <i>nobody is cut off</i>, and the claim it supports is much narrower.
    /// </remarks>
    private const string Detour =
        "Note what this does and does not say: it measures DISCONNECTION, and the larger half of "
        + "Severance is DETOUR — a crossing four hundred metres away severs a neighbourhood in every "
        + "sense a player would recognise and is fully connected here. Measuring that needs a "
        + "shortest path over the foot subgraph, and nothing searches this graph yet (milestone 5b).";

    /// <summary>How many Arcs admit a mode. Not twice the Segment count once one-way roads exist.</summary>
    private static int Arcs(RoadGraph graph, TravelMode mode)
    {
        int count = 0;

        for (int arc = 0; arc < graph.Arcs.Count; arc++)
        {
            if (graph.Arcs.Admits(arc, mode))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// A count and a total length per kind, which is the shape of the network in three lines.
    /// </summary>
    /// <remarks>
    /// <b>A table rather than a picture, and that is the honest form for this object.</b> The Lot grid
    /// is a grid and prints as one; a Road Graph at one Street per Cell boundary is 129² nodes on a
    /// 4096-Tile map, so a glyph per node is a wall of identical characters and a glyph per Tile does
    /// not fit on a terminal. What a reader needs to judge the generator is how much of each kind
    /// there is and whether the Arterials severed anything, both of which are counts.
    /// </remarks>
    private static void Kinds(TextWriter output, RoadGraph graph)
    {
        Span<int> counts = stackalloc int[3];
        Span<long> lengths = stackalloc long[3];

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot))
            {
                continue;
            }

            int kind = graph.Segments.Kind[slot];

            counts[kind]++;
            lengths[kind] += graph.Segments.LengthTiles[slot].Raw;
        }

        Row(output, "Street  ", RoadKind.Street, counts, lengths, graph);
        Row(output, "Arterial", RoadKind.Arterial, counts, lengths, graph);
        Row(output, "FootPath", RoadKind.FootPath, counts, lengths, graph);
    }

    private static void Row(
        TextWriter output,
        string name,
        RoadKind kind,
        ReadOnlySpan<int> counts,
        ReadOnlySpan<long> lengths,
        RoadGraph graph)
    {
        Speed speed = graph.Ruleset.SpeedFor(kind);

        output.WriteLine(
            $"{name}  {counts[(int)kind],7} Segments, {lengths[(int)kind],9} Tiles, free-flow "
            + $"{speed.ToTilesPerTickFloor().Raw} Tiles/Tick, capacity "
            + $"{graph.Ruleset.CapacityFor(kind)} Vehicles/Day.");
    }

    /// <summary>One row per Segment, for anybody who wants the graph rather than its shape.</summary>
    private static void Csv(TextWriter output, RoadGraph graph)
    {
        output.WriteLine("segment,node_a,node_b,kind,length_tiles,modes_forward,modes_backward,epoch");

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot))
            {
                continue;
            }

            graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[slot], out int a);
            graph.Nodes.Rows.TryResolve(graph.Segments.NodeB[slot], out int b);

            output.WriteLine(
                $"{slot},{a},{b},{(RoadKind)graph.Segments.Kind[slot]},"
                + $"{graph.Segments.LengthTiles[slot].Raw},{graph.Segments.ModesForward[slot]},"
                + $"{graph.Segments.ModesBackward[slot]},{graph.Segments.Epoch[slot]}");
        }
    }
}
