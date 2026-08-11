using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// The Rulesets and hand-built graphs the Road Graph tests run against.
/// </summary>
/// <remarks>
/// <b>Two kinds of fixture, and they answer different questions.</b> <see cref="Roads"/> and its
/// variants drive the <em>generator</em>, so a test written against them asks whether the mechanism
/// produces the city the Ruleset describes — which is the only way to see Severance, because nothing
/// smaller than a laid-out network can sever anything. <see cref="Chain"/> and
/// <see cref="TwoIslands"/> are built by hand a node at a time, so a test written against them knows
/// the answer in advance and can assert an exact one. A connectivity test that used only the
/// generator would be comparing a number against itself.
/// </remarks>
internal static class RoadFixtures
{
    /// <summary>Street capacity as the shipped Ruleset states it, in Vehicles per hour.</summary>
    internal const int StreetCapacityPerHour = 3_600;

    /// <summary>The hours a Day is converted through. The loader's factor, restated so a test can check it.</summary>
    internal const int HoursPerDay = 24;

    /// <summary>
    /// The shipped Ruleset's <c>[roads]</c> values, as a struct, with the block widened.
    /// </summary>
    /// <remarks>
    /// <b>512 Tiles a block rather than 32, and the reason is the suite's wall clock.</b> At 32 the
    /// generator lays 16,641 nodes and 32,870 Segments — the real city, and the right thing for the
    /// long run and the acceptance dump to use. Every other test here asks a structural question that
    /// a 9×9 grid answers exactly as well, and running the full lay-out for each of them would put
    /// minutes into a suite that has to stay runnable.
    /// </remarks>
    internal static RoadRuleset Roads(
        int blockTiles = 512,
        int arterials = 2,
        int junctionTiles = 512,
        int crossingEvery = 4,
        int footPaths = 40) =>
        new(
            BlockTiles: blockTiles,
            ArterialCount: arterials,
            ArterialJunctionTiles: junctionTiles,
            FootCrossingEvery: crossingEvery,
            FootPathsPerThousandBlocks: footPaths,
            StreetSpeed: Speed.FromKilometresPerHour(50),
            ArterialSpeed: Speed.FromKilometresPerHour(90),
            WalkSpeed: Speed.FromKilometresPerHour(5),
            StreetCapacityPerDay: StreetCapacityPerHour * HoursPerDay,
            ArterialCapacityPerDay: 12_000 * HoursPerDay,
            FootPathCapacityPerDay: 1_000 * HoursPerDay);

    /// <summary>
    /// A Ruleset whose grid is fine enough for an Arterial to actually cut it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>256 Tiles a block rather than <see cref="Roads"/>' 512, and the reason is a finding rather
    /// than a wall-clock convenience.</b> The severance tests were first written against the 512-Tile
    /// fixture and reported no Severance at any crossing density — correctly. <b>Severance is a
    /// property of the grid's fineness relative to the barrier, not of the barrier.</b> An Arterial
    /// destroys the Streets it runs over, so on a fine grid it destroys a long contiguous run of them
    /// and behaves like a wall; on a 9×9 lattice of 512-Tile blocks it destroys a handful of enormous
    /// Streets and every pedestrian simply walks round the end of it.
    /// </para>
    /// <para>
    /// <b>Eight Arterials rather than <see cref="Roads"/>' two, for the same reason on the other
    /// axis.</b> Two lines across a 15×15 lattice cut nothing either — this fixture was rewritten
    /// twice, once for each half. What severs a city is <b>barrier length per unit of grid</b>, and
    /// both the block size and the Arterial count move it, which is why neither alone is the dial.
    /// </para>
    /// <para>
    /// That is worth stating because it is the first thing a designer tuning
    /// <c>foot_crossing_every</c> will meet: <b>the dial does nothing at all until there is enough
    /// Arterial to cut with</b>, and then it does a great deal. At 256 Tiles and eight Arterials the
    /// largest walkable piece goes from 72 nodes to 289 of 353 when the crossings come back; at 512
    /// Tiles, or at two Arterials, it does not move at all.
    /// </para>
    /// </remarks>
    internal static RoadRuleset Severing(int crossingEvery) =>
        Roads(blockTiles: 256, arterials: 8, junctionTiles: 512, crossingEvery, footPaths: 0);

    /// <summary>A Ruleset carrying nothing but the <c>[roads]</c> table.</summary>
    internal static Ruleset With(RoadRuleset roads) =>
        new Ruleset([], [], [], [], [], [], [], [], []) { Roads = roads };

    /// <summary>
    /// <paramref name="nodes"/> nodes in a line, each joined to the next by an ordinary Street.
    /// </summary>
    /// <remarks>
    /// One component, by construction, and the smallest graph with a Segment in it. What it is for is
    /// the arithmetic a generated graph cannot state exactly: two Arcs per Segment, a node's slice
    /// beginning where the previous one ended, and an end node of degree one.
    /// </remarks>
    internal static RoadGraph Chain(int nodes, TravelMode forward = TravelMode.Any,
        TravelMode backward = TravelMode.Any)
    {
        RoadGraph graph = new(Roads());
        Handle<RoadNode> previous = graph.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int i = 1; i < nodes; i++)
        {
            Handle<RoadNode> next = graph.Nodes.Create(new Tiles(i * 32), Tiles.Zero);

            graph.Segments.Create(previous, next, new Tiles(32), RoadKind.Street, forward, backward);
            previous = next;
        }

        graph.RebuildDerived();

        return graph;
    }

    /// <summary>
    /// Two chains of <paramref name="each"/> nodes with nothing between them.
    /// </summary>
    /// <remarks>
    /// <b>The disconnected fixture the definition of done asks for by name.</b> The answer is known —
    /// two components, each holding <paramref name="each"/> nodes — so a labelling that merged them,
    /// split them further, or lost a node is caught against a number rather than against itself.
    /// </remarks>
    internal static RoadGraph TwoIslands(int each)
    {
        RoadGraph graph = new(Roads());

        for (int island = 0; island < 2; island++)
        {
            Handle<RoadNode> previous = graph.Nodes.Create(new Tiles(island * 4_096), Tiles.Zero);

            for (int i = 1; i < each; i++)
            {
                Handle<RoadNode> next =
                    graph.Nodes.Create(new Tiles((island * 4_096) + (i * 32)), Tiles.Zero);

                graph.Segments.Create(
                    previous, next, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);

                previous = next;
            }
        }

        graph.RebuildDerived();

        return graph;
    }
}
