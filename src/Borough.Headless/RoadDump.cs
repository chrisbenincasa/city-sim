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
    private static string Severance(RoadGraph graph)
    {
        int car = graph.Connectivity.CarComponents;
        int foot = graph.Connectivity.FootComponents;

        if (foot > car)
        {
            return $"SEVERANCE: the pedestrian network is in {foot} pieces where the road network is "
                 + $"in {car}. Somewhere an Arterial has cut a neighbourhood off from the shops that "
                 + "served it, and nobody deleted a pedestrian route — the mode mask simply never "
                 + "granted one. Raise foot_crossing_every to see this get worse, or lower it to 1 "
                 + "to see it go away.";
        }

        return "No Severance: every node the car network reaches, the pedestrian network reaches "
             + "too. With Arterials on the map that means the crossings are dense enough to carry "
             + "the foot network across every one of them.";
    }

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
