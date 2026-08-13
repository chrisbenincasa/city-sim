using Borough.Core.Determinism;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// The graphs and the Address pairs <see cref="WalkSearchBenchmarks"/> times, and the reason each one
/// is the pair it is.
/// </summary>
/// <remarks>
/// <para>
/// <b>The graph is the shipped one at full size, and that is the whole point of the fixture.</b>
/// <c>rulesets/minimal.toml</c>'s <c>block_tiles = 32</c> over a 4,096-Tile map is a 129×129 lattice —
/// <b>16,641 nodes and ~33,000 Street Segments</b> — which is not a scaled-down stand-in for the
/// million-Citizen city, it <em>is</em> that city's graph. Every other Road Graph fixture in the suite
/// widens the block to 512 to keep the suite's wall clock down, and a search timed on a 9×9 lattice
/// would be answering a different question. <c>plans/0013</c>'s standing lesson is the reason this
/// costs a minute of setup: <i>a unit cost is a hypothesis until a real world has produced one</i>,
/// and the 4–20 µs currently in the ledger came off S2's synthetic harness.
/// </para>
/// <para>
/// <b>Reachability is established by running the search, not by reasoning about the lattice.</b> The
/// generator deletes whichever Streets its Arterials happen to run over, from a polyline hashed off
/// the world key, so *is there a Street at (column, row)* is a property of one draw of the dice. A
/// fixture that assumed a pair was connected would degrade into timing
/// <see cref="TravelTime.Impassable"/> — which returns in constant time — and would publish that as
/// the cost of a walk search.
/// </para>
/// </remarks>
internal static class WalkSearchFixture
{
    /// <summary>The seed every fixture here lays its Arterials from.</summary>
    /// <remarks>
    /// <b>One draw, stated rather than hidden.</b> S2 R0.5's correction was that every Severance
    /// figure in the corpus was a single draw described as a property of the <c>[roads]</c> table.
    /// Search cost is far less seed-sensitive than severance — it is driven by settled nodes, and the
    /// lattice's node count is fixed — but the claim being made here is about a *distribution* of walk
    /// costs, so the seed is a named constant a re-run can vary.
    /// </remarks>
    internal const ulong Seed = 0xD0D0_CACA_0000_0001UL;

    /// <summary>
    /// The Road Graph <c>rulesets/minimal.toml</c> ships: a 32-Tile lattice with eight Arterials.
    /// </summary>
    /// <remarks>
    /// <b>This is the graph the reachable-walk timings must come from</b>, because the shipped city
    /// is the one <c>plans/0013</c> is pricing.
    /// <para>
    /// <b>⚠ It is not fully connected on foot at <see cref="Seed"/>, and the corpus says so if you
    /// read it exactly.</b> <c>plans/0020</c>'s amendment records zero stranded walkable nodes <b>on
    /// seven of eight seeds</b>, and this suite's own constant is the eighth: <b>7 of 16,641</b>
    /// walkable nodes sit in a pocket with no pedestrian route out — 0.04%, which reports as 0.0%.
    /// That is why <see cref="Apart"/> establishes reachability by running the search instead of
    /// trusting the lattice. See <c>WalkSearchBenchmarkFixtureTests</c> for the numbers.
    /// </para>
    /// </remarks>
    internal static RoadGraph Shipped() => Laid(RoadFixtures.Roads(
        blockTiles: 32, arterials: 8, junctionTiles: 512, crossingEvery: 4, footPaths: 40));

    /// <summary>
    /// <c>rulesets/severance.toml</c>'s <c>[roads]</c> — the rung at which the crossing dial bites.
    /// </summary>
    /// <remarks>
    /// <b>Here only to supply a genuinely unreachable pair.</b> <see cref="Shipped"/> severs nothing,
    /// so an <em>impassable</em> timing taken from it would be unobtainable rather than merely rare.
    /// A 256-Tile block is 1,024 m on a side and this graph is a demonstration rather than a city,
    /// which is why no reachable timing is taken from it.
    /// </remarks>
    internal static RoadGraph Severing() => Laid(RoadFixtures.Roads(
        blockTiles: 256, arterials: 16, junctionTiles: 512, crossingEvery: 16, footPaths: 40));

    /// <summary>The shipped <c>[roads]</c> laid from an arbitrary seed. For characterising a draw.</summary>
    internal static RoadGraph LaidAt(ulong seed) => Laid(
        RoadFixtures.Roads(
            blockTiles: 32, arterials: 8, junctionTiles: 512, crossingEvery: 4, footPaths: 40),
        seed);

    /// <summary>
    /// Two Addresses <paramref name="blocks"/> lattice edges apart along one row, both reachable.
    /// </summary>
    /// <remarks>
    /// <b>Separation is the axis because it is what the search's cost is a function of.</b>
    /// <see cref="WalkScratch.Search"/> is a Dijkstra that stops when nothing on the frontier can beat
    /// the best arrival found, so its work is the count of nodes it settles, and that grows with how
    /// far the destination is — not with how large the graph is. Timing one distance and calling the
    /// result *the cost of a walk search* would be quoting a point off a curve.
    /// </remarks>
    internal static (Address From, Address To) Apart(RoadGraph graph, int blocks)
    {
        ArgumentNullException.ThrowIfNull(graph);

        StreetGrid streets = graph.Streets;
        var scratch = new WalkScratch();

        for (int row = 0; row < streets.Span; row++)
        {
            for (int column = 0; column + blocks < streets.Blocks; column++)
            {
                int near = streets.Horizontal(column, row);
                int far = streets.Horizontal(column + blocks, row);

                if (!Usable(graph, near) || !Usable(graph, far))
                {
                    continue;
                }

                Address from = Midpoint(graph, near);
                Address to = Midpoint(graph, far);

                if (!WalkRouting.Cost(graph, from, to, TravelTime.Zero, scratch).IsImpassable)
                {
                    return (from, to);
                }
            }
        }

        throw new InvalidOperationException(
            $"no reachable pair {blocks} blocks apart on this graph. The fixture is not describing "
            + "the city it thinks it is, and a benchmark run against it would time nothing.");
    }

    /// <summary>Two Addresses on the same Segment and opposite sides — the closed-form case.</summary>
    /// <remarks>
    /// <b>The floor, and it never reaches the search at all.</b> <see cref="WalkRouting.Cost"/>
    /// answers a same-Segment walk by subtracting two offsets, which is also the only case the
    /// crossing cost applies to. It is timed so that the search's cost is read against something, and
    /// because a Trip model that turns out to be dominated by across-the-street walks would be priced
    /// by this row rather than by the others.
    /// </remarks>
    internal static (Address From, Address To) AcrossTheStreet(RoadGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        StreetGrid streets = graph.Streets;

        for (int row = 0; row < streets.Span; row++)
        {
            for (int column = 0; column < streets.Blocks; column++)
            {
                int segment = streets.Horizontal(column, row);

                if (Usable(graph, segment))
                {
                    Address from = Midpoint(graph, segment);

                    return (from, Opposite(from));
                }
            }
        }

        throw new InvalidOperationException("no walkable Street on this graph.");
    }

    /// <summary>
    /// Two Addresses on walkable Streets with no pedestrian route between them at all.
    /// </summary>
    /// <remarks>
    /// <b>This is the case that must be timed separately rather than blended into a mean.</b>
    /// <c>WalkRouting.Across</c> answers it by comparing union-find components over the foot
    /// subgraph, so it returns in <em>constant time</em> without settling a node. Sampling random
    /// pairs on a severed city and averaging would therefore report a search getting *faster* as the
    /// city breaks, which is the arithmetic of a mean over two regimes and not a fact about the
    /// algorithm.
    /// </remarks>
    internal static (Address From, Address To) Severed(RoadGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        RoadSegmentTable segments = graph.Segments;
        Address origin = Address.None;
        int originComponent = Rows.NoSlot;

        // Compared on the component label rather than by running a search, and the reason is the
        // suite's wall clock rather than taste: an unreachable pair is answered in constant time, but
        // every REACHABLE candidate rejected on the way to finding one costs a full Dijkstra, and
        // there are ~33,000 Segments to reject. Searching for the cheap case is the expensive way.
        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!Usable(graph, slot) || !FootComponentOf(graph, slot, out int component))
            {
                continue;
            }

            if (!origin.Exists)
            {
                origin = Midpoint(graph, slot);
                originComponent = component;
                continue;
            }

            if (component != originComponent)
            {
                return (origin, Midpoint(graph, slot));
            }
        }

        throw new InvalidOperationException(
            "every walkable Segment on this graph is in one foot component, so it cannot supply an "
            + "unreachable pair. Severing() is the fixture for this case; Shipped() happens to have "
            + "a seven-node pocket at this suite's seed and nothing at all at most others.");
    }

    /// <summary>The foot component both a Segment's endpoints sit in, or false if they disagree.</summary>
    /// <remarks>
    /// <b>Both endpoints, because one is not enough to place a Segment.</b> A walk may leave by
    /// either end (<c>WalkRouting.Across</c> seeds both), so a Segment whose ends sit in different
    /// components bridges them and belongs to neither for this purpose. Skipping it is what keeps the
    /// pair this fixture returns genuinely unreachable rather than merely awkward.
    /// </remarks>
    private static bool FootComponentOf(RoadGraph graph, int segment, out int component)
    {
        component = Rows.NoSlot;

        RoadSegmentTable segments = graph.Segments;

        if (!graph.Nodes.Rows.TryResolve(segments.NodeA[segment], out int a)
            || !graph.Nodes.Rows.TryResolve(segments.NodeB[segment], out int b))
        {
            return false;
        }

        component = graph.Nodes.FootComponent[a];

        return component == graph.Nodes.FootComponent[b];
    }

    /// <summary>A Segment's midpoint, on the left of its forward direction.</summary>
    private static Address Midpoint(RoadGraph graph, int segment) =>
        Address.On(segment, new Tiles(graph.Segments.LengthTiles[segment].Raw / 2), StreetSide.Left);

    /// <summary>The same place on the other side of the street.</summary>
    private static Address Opposite(Address address) =>
        Address.On(
            address.Segment,
            address.Offset,
            address.Side == StreetSide.Left ? StreetSide.Right : StreetSide.Left);

    /// <summary>A live Segment that admits pedestrians and is long enough to have a midpoint.</summary>
    private static bool Usable(RoadGraph graph, int segment) =>
        segment != Rows.NoSlot
        && graph.Segments.Rows.IsLive(segment)
        && (graph.Segments.Modes[segment] & (byte)TravelMode.Foot) != 0
        && graph.Segments.LengthTiles[segment].Raw > 1;

    private static RoadGraph Laid(RoadRuleset roads, ulong seed = Seed)
    {
        RoadGraph graph = new(roads);

        RoadGenerator.LayInto(graph, WorldKey.FromSeed(seed), CellGrid.WorldTiles);

        return graph;
    }
}
