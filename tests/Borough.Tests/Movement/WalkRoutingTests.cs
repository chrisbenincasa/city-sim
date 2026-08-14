using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Movement;

/// <summary>
/// The walk Leg, resolved: <c>distance / speed</c> over the foot subgraph, against a graph built by
/// hand so the answer is known in advance.
/// </summary>
/// <remarks>
/// <b>Every expectation here is stated as an arithmetic expression rather than as a literal</b>, and
/// the reason is that a literal would be the implementation's own output pasted into the suite. What
/// these assert is the <em>routing</em> — which Segments the walk uses and how the partial ones are
/// charged — with <see cref="TravelTime.Over"/> taken as the agreed spelling of <i>distance over
/// speed</i>, which is what <c>03 §3.7</c> defines a walk Leg to cost.
/// </remarks>
public sealed class WalkRoutingTests
{
    /// <summary>The fixture Chain's Segment length, and the spacing of its nodes.</summary>
    private static readonly Tiles Span = new(32);

    /// <summary>Walking pace, which binds on every Street in the fixture because 5 km/h &lt; 50.</summary>
    private static Speed Walk => RoadFixtures.Roads().WalkSpeed;

    /// <summary>A crossing cost that is unmistakably not zero, so a test can see it applied or not.</summary>
    /// <remarks>
    /// <b>A test value and emphatically not a chosen one.</b> The crossing cost is <c>[trips]</c>
    /// Ruleset data, hash-bearing, and <c>plans/0002</c> §D2 carries it unset with a named ratifier —
    /// 5b's walk-Leg cost distribution. It is one Tick here because one Tick is easy to see in an
    /// assertion, which is the only property the suite needs from it.
    /// </remarks>
    private static TravelTime Crossing => TravelTime.FromTicks(1);

    [Fact]
    public void SameSegmentSameSideIsTheOffsetDifference()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        const int segment = 0;

        Address from = Address.On(segment, new Tiles(4), StreetSide.Left);
        Address to = Address.On(segment, new Tiles(20), StreetSide.Left);

        TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch());

        Assert.Equal(TravelTime.Over(new Tiles(16), Walk), cost);
    }

    [Fact]
    public void SameSegmentIsSymmetric()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        const int segment = 0;

        Address low = Address.On(segment, new Tiles(4), StreetSide.Left);
        Address high = Address.On(segment, new Tiles(20), StreetSide.Left);

        WalkScratch scratch = new();

        Assert.Equal(
            WalkRouting.Cost(graph, TravelMode.Foot, low, high, Crossing, scratch),
            WalkRouting.Cost(graph, TravelMode.Foot, high, low, Crossing, scratch));
    }

    /// <summary>
    /// The across-the-street case: the one condition under which a crossing cost applies.
    /// </summary>
    [Fact]
    public void OppositeSidesOfOneSegmentPayTheCrossing()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        const int segment = 0;

        Address left = Address.On(segment, new Tiles(4), StreetSide.Left);
        Address right = Address.On(segment, new Tiles(20), StreetSide.Right);

        TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, left, right, Crossing, new WalkScratch());

        Assert.Equal(TravelTime.Over(new Tiles(16), Walk) + Crossing, cost);
    }

    /// <summary>
    /// Directly opposite is the crossing and nothing else — the case the term was argued for.
    /// </summary>
    [Fact]
    public void DirectlyOppositeCostsTheCrossingAlone()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        const int segment = 0;

        Address left = Address.On(segment, new Tiles(12), StreetSide.Left);
        Address right = Address.On(segment, new Tiles(12), StreetSide.Right);

        Assert.Equal(Crossing, WalkRouting.Cost(graph, TravelMode.Foot, left, right, Crossing, new WalkScratch()));
    }

    /// <summary>
    /// ⚠ <b>The crossing is silent once the walk leaves the Segment</b>, which is <c>adr/0074</c>'s
    /// scoping clause and the thing most likely to be "fixed" by a later reader.
    /// </summary>
    /// <remarks>
    /// <i>The same side</i> stops meaning anything once a route turns a corner: side is defined
    /// relative to one Segment's own A→B direction, so two Addresses on different Segments have no
    /// shared frame in which <i>opposite</i> is a fact. Charging a crossing there would be inventing
    /// precision the model does not have — the term would fire on a pair the player would not call
    /// across the street from one another.
    /// </remarks>
    [Fact]
    public void OppositeSidesOfDifferentSegmentsPayNoCrossing()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        Address from = Address.On(0, new Tiles(16), StreetSide.Left);
        Address to = Address.On(2, new Tiles(16), StreetSide.Right);

        WalkScratch scratch = new();

        TravelTime charged = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, scratch);
        TravelTime free = WalkRouting.Cost(graph, TravelMode.Foot, from, to, TravelTime.Zero, scratch);

        Assert.Equal(free, charged);
    }

    /// <summary>
    /// A walk over three Segments costs the three Arcs, and the endpoints are the cheap ones.
    /// </summary>
    [Fact]
    public void AcrossSegmentsSumsTheArcs()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        // Node 0 exactly, and node 3 exactly: every partial term is zero, so what is left is the
        // three whole Segments between them.
        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(2, Span, StreetSide.Left);

        TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch());

        Assert.Equal(TravelTime.Over(Span, Walk) * 3, cost);
    }

    /// <summary>
    /// ⚠ <b>A path's cost is the sum of per-Arc floors and not the floor of the total</b>, and the
    /// two genuinely differ.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0071</c> states the property this rests on — <i>rounding is floor, so a cost
    /// underestimates by at most 1/65,536 Tick</i>, per division — so an <c>n</c>-Arc path can sit up
    /// to <c>n</c> raw units below a single division over the same total distance. <b>This is
    /// recorded as a test rather than as a remark because it is the kind of difference a later reader
    /// will meet as an off-by-one and be tempted to fix in the wrong direction</b>: the per-Arc sum is
    /// the correct answer, because it is what a Traveller actually pays, Arc by Arc.
    /// </para>
    /// <para>
    /// ⚠ <b>The Segment length here is 31 Tiles and not the fixture's 32, and that is the whole
    /// point of the test.</b> At 32 Tiles and 5 km/h the division comes out with a fractional part of
    /// 0.306, so three of them sum to 0.918 and the floors happen to agree — <b>this test was first
    /// written against the standard fixture and passed without ever demonstrating anything</b>. A
    /// bound that is never approached is not a bound anybody has checked, which is slice 10 task 11's
    /// warning arriving in a unit test instead of a golden baseline.
    /// </para>
    /// </remarks>
    [Fact]
    public void PerArcRoundingSitsStrictlyBelowASingleDivision()
    {
        RoadGraph graph = Chain(nodes: 4, span: new Tiles(31));

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(2, new Tiles(31), StreetSide.Left);

        TravelTime walked = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch());
        TravelTime straight = TravelTime.Over(new Tiles(93), Walk);

        Assert.Equal(TravelTime.Over(new Tiles(31), Walk) * 3, walked);
        Assert.True(walked < straight, "the per-Arc sum should be the cheaper of the two.");
        Assert.True(straight.Raw - walked.Raw <= 3, "and short of it by at most one unit per Arc.");
    }

    /// <summary>
    /// <paramref name="nodes"/> nodes in a line, <paramref name="span"/> Tiles apart.
    /// </summary>
    /// <remarks>
    /// <see cref="RoadFixtures.Chain"/> with the span opened up. It is local rather than pushed into
    /// the shared fixtures because exactly one test needs a length other than 32, and that test needs
    /// it for a reason specific to itself.
    /// </remarks>
    private static RoadGraph Chain(int nodes, Tiles span)
    {
        RoadGraph graph = new(RoadFixtures.Roads());
        Handle<RoadNode> previous = graph.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int i = 1; i < nodes; i++)
        {
            Handle<RoadNode> next = graph.Nodes.Create(span * i, Tiles.Zero);

            graph.Segments.Create(
                previous, next, span, RoadKind.Street, TravelMode.Any, TravelMode.Any);

            previous = next;
        }

        graph.RebuildDerived();

        return graph;
    }

    /// <summary>The walk leaves its Segment by whichever end is nearer the destination.</summary>
    [Fact]
    public void LeavesBySpecifiedEndpointWhicheverIsCheaper()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        // Four Tiles from node 1, which is the end facing the destination — so the partial term is
        // the short one, not the 28 Tiles back to node 0.
        Address from = Address.On(0, new Tiles(28), StreetSide.Left);
        Address to = Address.On(1, Span, StreetSide.Left);

        TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch());

        Assert.Equal(TravelTime.Over(new Tiles(4), Walk) + TravelTime.Over(Span, Walk), cost);
    }

    /// <summary>
    /// <b>Severance.</b> Two pieces of city with no foot route between them report no route found.
    /// </summary>
    [Fact]
    public void SeveredCityHasNoWalkRoute()
    {
        RoadGraph graph = RoadFixtures.TwoIslands(4);

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(3, Tiles.Zero, StreetSide.Left);

        TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch());

        Assert.Equal(TravelTime.Impassable, cost);
    }

    /// <summary>
    /// The unsevered variant, kept in the suite watching itself pass.
    /// </summary>
    /// <remarks>
    /// Slice 6's precedent and slice 10 task 11's warning in one: a severance test that only ever
    /// asserts <em>unreachable</em> passes just as well against a resolver that has stopped finding
    /// anything at all.
    /// </remarks>
    [Fact]
    public void UnseveredCityHasOne()
    {
        RoadGraph graph = RoadFixtures.Chain(8);

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(6, Span, StreetSide.Left);

        TravelTime cost = WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch());

        Assert.NotEqual(TravelTime.Impassable, cost);
        Assert.Equal(TravelTime.Over(Span, Walk) * 7, cost);
    }

    /// <summary>
    /// A road cars may use and pedestrians may not is not a walk route — <b>the mask never granted
    /// one</b>.
    /// </summary>
    [Fact]
    public void CarOnlyRoadIsNotWalkable()
    {
        RoadGraph graph = RoadFixtures.Chain(4, TravelMode.Car, TravelMode.Car);

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(2, Span, StreetSide.Left);

        Assert.Equal(
            TravelTime.Impassable, WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch()));
    }

    /// <summary>
    /// A bulldozed Segment is impassable rather than a throw — <b>the caller's Trip is stranded</b>.
    /// </summary>
    /// <remarks>
    /// The resolver is called from Phase 4 on rows that may have been invalidated by a Phase 3 edit,
    /// so a stale Address has to be an answer rather than an exception. <c>TripFate.Stranded</c> is
    /// the outcome the caller records, and it exists for exactly this.
    /// </remarks>
    [Fact]
    public void BulldozedSegmentIsImpassableRatherThanAThrow()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(2, Span, StreetSide.Left);

        graph.Segments.Rows.Free(graph.Segments.Rows.At(0));

        Assert.Equal(
            TravelTime.Impassable, WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, new WalkScratch()));
    }

    /// <summary>An unset Address routes nowhere, and says so without resolving anything.</summary>
    [Fact]
    public void UnsetAddressIsImpassable()
    {
        RoadGraph graph = RoadFixtures.Chain(4);
        Address real = Address.On(0, Tiles.Zero, StreetSide.Left);

        WalkScratch scratch = new();

        Assert.Equal(TravelTime.Impassable, WalkRouting.Cost(graph, TravelMode.Foot, Address.None, real, Crossing, scratch));
        Assert.Equal(TravelTime.Impassable, WalkRouting.Cost(graph, TravelMode.Foot, real, Address.None, Crossing, scratch));
    }

    /// <summary>
    /// The same query on a reused scratch gives the same answer — <b>the stamp really does clear</b>.
    /// </summary>
    /// <remarks>
    /// The one failure a generation stamp can have is a stale entry read as current, which would show
    /// up as a second query inheriting the first's distances. Running two different queries and then
    /// repeating the first is what catches it; running one query twice would not.
    /// </remarks>
    [Fact]
    public void AReusedScratchDoesNotLeakBetweenQueries()
    {
        RoadGraph graph = RoadFixtures.Chain(8);
        WalkScratch scratch = new();

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address near = Address.On(1, Span, StreetSide.Left);
        Address far = Address.On(6, Span, StreetSide.Left);

        TravelTime first = WalkRouting.Cost(graph, TravelMode.Foot, from, near, Crossing, scratch);
        WalkRouting.Cost(graph, TravelMode.Foot, from, far, Crossing, scratch);
        TravelTime again = WalkRouting.Cost(graph, TravelMode.Foot, from, near, Crossing, scratch);

        Assert.Equal(first, again);
    }

    /// <summary>
    /// A near query settles a handful of nodes rather than the whole graph.
    /// </summary>
    /// <remarks>
    /// <b>The stopping rule, asserted rather than assumed.</b> Without it a walk between two
    /// neighbouring Buildings settles every node in the city and the answer is still correct — the
    /// most expensive kind of defect, because nothing fails. The bound is loose on purpose: what is
    /// being checked is that the search is proportional to the distance walked, not a particular
    /// node count.
    /// </remarks>
    [Fact]
    public void ANearQueryDoesNotSettleTheWholeGraph()
    {
        RoadGraph graph = RoadFixtures.Chain(64);
        WalkScratch scratch = new();

        Address from = Address.On(0, Tiles.Zero, StreetSide.Left);
        Address to = Address.On(1, Span, StreetSide.Left);

        WalkRouting.Cost(graph, TravelMode.Foot, from, to, Crossing, scratch);

        Assert.True(scratch.Relaxed < 8, $"settled {scratch.Relaxed} nodes for a two-Segment walk.");
    }
}
