using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// The Road Graph's structure: the CSR adjacency, the derived columns, and the Epoch.
/// </summary>
/// <remarks>
/// <b>Structure rather than content, which is what makes these assertions exact.</b> How many
/// Arterials a seed happens to lay is a property of a hash and belongs to the acceptance run; that a
/// node's Arc slice begins where the previous node's ended is a property of the rebuild, and is either
/// true or a defect. <c>plans/0020</c>'s note applies to every one of these — the port was budgeted as
/// a rewrite with a very good reference, and the reference's <c>AssertWellFormed</c> is where the
/// structural half of this file comes from.
/// </remarks>
public sealed class RoadGraphTests
{
    [Fact]
    public void A_segment_produces_exactly_two_arcs()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 5);

        Assert.Equal(4, graph.Segments.Rows.LiveCount);
        Assert.Equal(8, graph.Arcs.Count);
    }

    /// <summary>
    /// The CSR slices tile the Arc array with no gap and no overlap.
    /// </summary>
    /// <remarks>
    /// <b>The property a single Arc cannot show.</b> A gap is an Arc nothing can reach and an overlap
    /// is one two nodes both claim; each looks perfectly well formed from inside, and only walking the
    /// nodes in slot order and checking that each run begins where the last ended can see either.
    /// </remarks>
    [Fact]
    public void The_arc_slices_tile_the_array()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 6);

        int expected = 0;

        for (int node = 0; node < graph.Nodes.Rows.SlotCount; node++)
        {
            Assert.Equal(expected, graph.Nodes.ArcStart[node]);
            expected += graph.Nodes.ArcCount[node];
        }

        Assert.Equal(graph.Arcs.Count, expected);
    }

    [Fact]
    public void An_end_of_a_chain_has_one_arc_and_the_middle_has_two()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 4);

        Assert.Equal(1, graph.Nodes.ArcCount[0]);
        Assert.Equal(2, graph.Nodes.ArcCount[1]);
        Assert.Equal(2, graph.Nodes.ArcCount[2]);
        Assert.Equal(1, graph.Nodes.ArcCount[3]);
    }

    [Fact]
    public void Every_arc_leads_somewhere_other_than_its_own_node()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 8);

        for (int node = 0; node < graph.Nodes.Rows.SlotCount; node++)
        {
            int end = graph.Nodes.ArcStart[node] + graph.Nodes.ArcCount[node];

            for (int arc = graph.Nodes.ArcStart[node]; arc < end; arc++)
            {
                Assert.NotEqual(node, graph.Arcs.Target[arc]);
            }
        }
    }

    /// <summary>
    /// A Segment's free-flow speed and capacity follow the Ruleset in force, never construction.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0064</c> and <c>adr/0068</c> applied to a road</b>, and the assertion is the one those
    /// ADRs were written for: retune the number, adopt, and the standing city moves. A value frozen at
    /// construction would pass every other test in this file and fail this one, which is why it is
    /// here rather than left to the loader's tests.
    /// </remarks>
    [Fact]
    public void Retuning_a_speed_moves_the_standing_city()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 3);

        Assert.Equal(Speed.FromKilometresPerHour(50), graph.Segments.FreeFlow[0]);

        graph.Adopt(RoadFixtures.Roads() with { StreetSpeed = Speed.FromKilometresPerHour(25) });

        Assert.Equal(Speed.FromKilometresPerHour(25), graph.Segments.FreeFlow[0]);
    }

    /// <summary>
    /// Halving a speed doubles the traversal cost, <b>to the last bit the format has and not past
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <b>This asserted exact equality until 2026-08-13 and it was exact by luck.</b> A cost is
    /// <c>FloorDiv(distance × One, speed)</c>, and a floored quotient does not in general double when
    /// its divisor halves — it does so only when the fraction happens to fall right. It did at
    /// <c>Ticks.PerDay = 8192</c>; at 2048 the two differ by <b>1</b>, which is <c>1/65,536</c> of a
    /// Tick, or <b>0.64 ms of in-world time</b>. <c>adr/0094</c> notes that one instrument gets coarser
    /// under the shorter Day; this is the same two bits showing up in the simulation's own arithmetic,
    /// and the tolerance is stated in ULPs so that a real regression still fails.
    /// </remarks>
    [Fact]
    public void Halving_a_speed_doubles_the_traversal_cost()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 3);

        TravelTime before = graph.Arcs.CarTime[0];

        graph.Adopt(RoadFixtures.Roads() with { StreetSpeed = Speed.FromKilometresPerHour(25) });

        int doubled = graph.Arcs.CarTime[0].Raw;
        int drift = doubled - (before.Raw * 2);

        Assert.True(
            drift is >= -1 and <= 1,
            $"halving the speed moved the cost from {before.Raw} to {doubled}, which is {drift} "
            + "Q16.16 units away from double. One unit is the format's floor; more than one is the "
            + "conversion having changed rather than the rounding having landed differently.");
    }

    /// <summary>
    /// A walk is held to walking pace on a Street, and a car is held to the Street.
    /// </summary>
    /// <remarks>
    /// <c>min(the mode's ceiling, the road's free-flow)</c>. One speed column and two rates, which is
    /// what stops a second speed column contradicting either <c>CONTEXT.md</c> → Segment or its
    /// <i>one graph with mode masks</i>.
    /// </remarks>
    [Fact]
    public void A_walk_is_slower_than_a_drive_on_the_same_segment()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 3);

        Assert.True(
            graph.Arcs.FootTime[0] > graph.Arcs.CarTime[0],
            "a pedestrian crossed a Street faster than a car did.");
    }

    [Fact]
    public void An_arc_a_mode_may_not_use_is_impassable_rather_than_expensive()
    {
        RoadGraph graph = RoadFixtures.Chain(
            nodes: 3, forward: TravelMode.Car, backward: TravelMode.Car);

        Assert.True(graph.Arcs.FootTime[0].IsImpassable);
        Assert.False(graph.Arcs.CarTime[0].IsImpassable);
    }

    /// <summary>
    /// A one-way Segment carries cars one way and pedestrians both — <c>adr/0072</c>'s whole point.
    /// </summary>
    /// <remarks>
    /// <b>Nothing generates this and that is exactly why it is tested.</b> The case the mask exists to
    /// serve is the case the spike never produced, which is how its own derivation came to run the
    /// wrong way for the length of S2 with no measurement able to notice. This asserts the structure
    /// can hold it before anything needs it to.
    /// </remarks>
    [Fact]
    public void A_one_way_street_carries_cars_one_way_and_pedestrians_both()
    {
        RoadGraph graph = new(RoadFixtures.Roads());

        Handle<RoadNode> a = graph.Nodes.Create(Tiles.Zero, Tiles.Zero);
        Handle<RoadNode> b = graph.Nodes.Create(new Tiles(32), Tiles.Zero);

        graph.Segments.Create(
            a, b, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Foot);

        graph.RebuildDerived();

        int outbound = graph.Nodes.ArcStart[0];
        int inbound = graph.Nodes.ArcStart[1];

        Assert.True(graph.Arcs.Admits(outbound, TravelMode.Car));
        Assert.False(graph.Arcs.Admits(inbound, TravelMode.Car));

        Assert.True(graph.Arcs.Admits(outbound, TravelMode.Foot));
        Assert.True(graph.Arcs.Admits(inbound, TravelMode.Foot));

        Assert.Equal((byte)TravelMode.Any, graph.Segments.Modes[0]);
    }

    [Fact]
    public void A_segments_mask_is_the_union_of_its_two_directions()
    {
        RoadGraph graph = RoadFixtures.Chain(
            nodes: 2, forward: TravelMode.Car, backward: TravelMode.Foot);

        Assert.Equal((byte)TravelMode.Any, graph.Segments.Modes[0]);
    }

    /// <summary>An Epoch opens at one, so zero stays available as <em>never computed</em>.</summary>
    [Fact]
    public void A_new_segments_epoch_is_one_rather_than_zero()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 2);

        Assert.Equal(1u, graph.Segments.Epoch[0]);
    }

    [Fact]
    public void Editing_a_segment_moves_only_its_own_epoch()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 3);

        graph.Segments.Edited(0);

        Assert.Equal(2u, graph.Segments.Epoch[0]);
        Assert.Equal(1u, graph.Segments.Epoch[1]);
    }

    /// <summary>
    /// <b>A road edit moves no standing Segment's Epoch.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The assertion <c>RoadGraph.LayStreet</c>'s own docstring names, written down at last.</b>
    /// S2 R5's finding is the reason it is a test rather than a remark: <b>a single-counter Epoch
    /// <em>is</em> a global flush</b>, and an edit path that bumped every Segment would be exactly
    /// that while wearing per-Segment storage — every route in the city invalidated by one player
    /// laying one Street, with nothing in any hash or count to say so.
    /// </para>
    /// <para>
    /// <b>Both directions, and the removal is the interesting one.</b> <c>adr/0012</c> permits an
    /// addition to leave standing routes <em>suboptimal</em>, so a lay touching nothing is the
    /// contract read literally. A bulldoze must be <i>never wrong about a removal</i> — and it is,
    /// by freeing the row: a route naming a freed Segment fails to resolve rather than comparing
    /// stale, which is a stronger guarantee than any Epoch could give.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_road_edit_moves_no_standing_segments_epoch()
    {
        var graph = new RoadGraph(RoadFixtures.Lattice(blockTiles: 512));

        RoadGenerator.LayInto(graph, WorldKey.FromSeed(1), CellGrid.WorldTiles);

        uint[] before = Epochs(graph);

        Assert.True(graph.BulldozeStreet(1, 1, StreetAxis.East), "the fixture has no Street to bulldoze");
        Assert.True(graph.LayStreet(1, 1, StreetAxis.East), "the bulldozed Street did not come back");

        uint[] after = Epochs(graph);

        for (int slot = 0; slot < before.Length; slot++)
        {
            if (before[slot] == 0 || !graph.Segments.Rows.IsLive(slot))
            {
                // Never existed, or was the row the edit freed -- and the slot the lay took back is
                // a NEW Segment, which opens at 1 like any other rather than inheriting a history.
                continue;
            }

            Assert.Equal(before[slot], after[slot]);
        }
    }

    /// <summary>Every Segment slot's Epoch, live or not.</summary>
    private static uint[] Epochs(RoadGraph graph)
    {
        uint[] epochs = new uint[graph.Segments.Rows.SlotCount];

        for (int slot = 0; slot < epochs.Length; slot++)
        {
            epochs[slot] = graph.Segments.Rows.IsLive(slot) ? graph.Segments.Epoch[slot] : 0;
        }

        return epochs;
    }

    /// <summary>
    /// The Epoch saturates rather than wrapping.
    /// </summary>
    /// <remarks>
    /// <b>The one failure the counter exists to prevent.</b> A <c>uint</c> that wrapped would make a
    /// route computed long ago compare <em>valid</em> against a Segment edited billions of times
    /// since — the counter reporting freshness it does not have. Saturating is wrong in the harmless
    /// direction: everything looks stale.
    /// </remarks>
    [Fact]
    public void An_epoch_at_its_ceiling_saturates_rather_than_wrapping()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 2);

        graph.Segments.Epoch[0] = uint.MaxValue;
        graph.Segments.Edited(0);

        Assert.Equal(uint.MaxValue, graph.Segments.Epoch[0]);
    }

    /// <summary>
    /// A rebuild is idempotent — running it twice produces the same adjacency.
    /// </summary>
    /// <remarks>
    /// <b>The property that makes <c>(derived AND rebuilt)</c> mean anything.</b> If a second rebuild
    /// disagreed with the first, then a saved world reloaded would not equal the world that saved it,
    /// and the derived declaration would be a lie the State Hash could not see — because derived
    /// columns do not fold.
    /// </remarks>
    [Fact]
    public void Rebuilding_twice_produces_the_same_adjacency()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 12);

        int[] target = graph.Arcs.Target.ToArray();
        int[] segment = graph.Arcs.Segment.ToArray();
        int[] start = graph.Nodes.ArcStart.Span.ToArray();

        graph.RebuildDerived();

        Assert.Equal(target, graph.Arcs.Target.ToArray());
        Assert.Equal(segment, graph.Arcs.Segment.ToArray());
        Assert.Equal(start, graph.Nodes.ArcStart.Span.ToArray());
    }

    /// <summary>
    /// A freed Segment leaves the adjacency, and the nodes it joined keep their other Arcs.
    /// </summary>
    /// <remarks>
    /// The generator does this on every severed Street, so it is the one edit path 5a actually
    /// exercises — <c>CommandKind.Connect</c> is 5a-bis.
    /// </remarks>
    [Fact]
    public void Freeing_a_segment_removes_both_of_its_arcs()
    {
        RoadGraph graph = RoadFixtures.Chain(nodes: 4);

        graph.Segments.Rows.Free(graph.Segments.Rows.At(1));
        graph.RebuildDerived();

        Assert.Equal(4, graph.Arcs.Count);
        Assert.Equal(1, graph.Nodes.ArcCount[1]);
        Assert.Equal(1, graph.Nodes.ArcCount[2]);
    }

    /// <summary>
    /// The graph folds into the State Hash, and a different <c>[roads]</c> table produces a different
    /// hash.
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0020</c>'s definition of done, stated as the two halves it actually has.</b> A
    /// <em>saved</em> difference must move the hash — a different block size lays different Segments
    /// — and a <em>derived</em> one must not, because a speed is read through the Ruleset rather than
    /// stored. The second half is the sharper claim and is the one a mistake would break: freezing
    /// free-flow into a saved column would make this test fail while every structural test passed.
    /// </remarks>
    [Fact]
    public void A_different_road_network_hashes_differently_and_a_retune_does_not()
    {
        ulong wide = HashOf(RoadFixtures.Roads(blockTiles: 1_024));
        ulong tight = HashOf(RoadFixtures.Roads(blockTiles: 512));

        Assert.NotEqual(wide, tight);

        ulong slower = HashOf(
            RoadFixtures.Roads(blockTiles: 512) with
            {
                StreetSpeed = Speed.FromKilometresPerHour(25),
            });

        Assert.Equal(tight, slower);
    }

    [Fact]
    public void A_ruleset_with_no_roads_lays_none()
    {
        World world = new(citizens: 100, RoadFixtures.With(RoadRuleset.None));

        SyntheticCity.PopulateInto(world, WorldKey.FromSeed(1), Ticks.Zero);

        Assert.False(world.Roads.Exists);
        Assert.Equal(0, world.Roads.Arcs.Count);
    }

    /// <summary>The generator is world creation, so it refuses to run twice.</summary>
    [Fact]
    public void Laying_roads_into_a_world_that_has_them_is_refused()
    {
        World world = Laid(RoadFixtures.Roads());

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => RoadGenerator.LayInto(world.Roads, WorldKey.FromSeed(1), CellGrid.WorldTiles));

        Assert.Contains("already has roads", failure.Message, StringComparison.Ordinal);
    }

    private static ulong HashOf(RoadRuleset roads) => Laid(roads).HashState();

    private static World Laid(RoadRuleset roads)
    {
        World world = new(citizens: 100, RoadFixtures.With(roads));

        SyntheticCity.PopulateInto(world, WorldKey.FromSeed(1), Ticks.Zero);

        return world;
    }
}
