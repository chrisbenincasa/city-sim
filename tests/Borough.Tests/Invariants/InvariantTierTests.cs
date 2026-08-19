using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Tests.Space;
using Borough.Core.Tables;
using Borough.Tests.Golden;

namespace Borough.Tests.Invariants;

/// <summary>
/// <c>02 §10</c>'s three tiers, and one test per invariant that writes the violation and watches it
/// fire.
/// </summary>
/// <remarks>
/// <para>
/// <b>An invariant nobody has seen fail is an invariant nobody knows works.</b> This is the same
/// discipline the analyser suite is held to — every diagnostic has a test that writes the violation
/// on purpose — and it matters more here, because an invariant that cannot fire is
/// indistinguishable from a world that is correct, right up until it is not.
/// </para>
/// <para>
/// <b>Every violation below is reached by corrupting the world directly rather than through the
/// public API.</b> That is not a shortcut: the API maintains these invariants, so a violation
/// reachable through it would be a bug to fix rather than a check to test. What is being tested is
/// that the check notices, and the fastest way to a broken world is to break one.
/// </para>
/// </remarks>
public sealed class InvariantTierTests
{
    // A world where the interesting rows exist: one Building on one Lot, one Household in it, two
    // Citizens in that Household.
    /// <summary><see cref="Built"/>, on a Ruleset that names money so a balance exists.</summary>
    private static World Moneyed()
    {
        var world = new World(1_000, TestRulesets.MoneyOnly);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);

        world.CreateHousehold(building, lifeStage: 1);

        return world;
    }

    private static World Built()
    {
        var world = new World(1_000);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);

        world.CreateCitizen(household, new Ticks(10));
        world.CreateCitizen(household, new Ticks(20));

        return world;
    }

    /// <summary>Nothing the ordinary API builds violates anything. Without this, nothing below means much.</summary>
    [Fact]
    public void A_world_built_through_the_api_violates_nothing()
    {
        World world = Built();

        Sweep(world);
        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// The golden world passes, which is what makes it usable as this suite's reference city.
    /// </summary>
    /// <remarks>
    /// Task 4 built that fixture coherent on purpose, naming this tier as the reason. An invariant
    /// suite whose reference world is already broken cannot be trusted to report anything.
    /// </remarks>
    [Fact]
    public void The_golden_world_violates_nothing()
    {
        World world = GoldenFixtures.Build();

        Sweep(world);
        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void A_replayed_session_violates_nothing()
    {
        Simulation simulation = Replay.Start(GoldenFixtures.Session(), GoldenFixtures.Catalogue());
        InputLog log = GoldenFixtures.Session();

        Replay.Trace(simulation, log, new Ticks(256), hashEvery: 8, []);
        simulation.CheckEndOfRun();
    }

    // ---- The per-Tick tier: at the write site ----

    /// <summary>
    /// <b>The write-site check earns its place on a realistic bug.</b> A row freed without being
    /// unlinked stays in its owner's list; the next allocation recycles that slot, and the recycled
    /// row is inserted into a list it is already in. That is a Citizen in two places, caught at the
    /// moment it happens rather than a sweep later.
    /// </summary>
    [Fact]
    public void Recycling_a_slot_that_was_freed_without_unlinking_is_caught_at_the_write_site()
    {
        World world = Built();
        Handle<Household> household = world.Households.Rows.At(0);

        // Free the Citizen the wrong way round — the row goes, the list entry stays.
        world.Citizens.Rows.Free(world.Citizens.Rows.At(0));

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.CreateCitizen(household, new Ticks(30)));

        Assert.Equal(Invariant.CitizenIsNotAlreadyInThisHousehold, failure.Violation.Invariant);
    }

    /// <inheritdoc cref="Recycling_a_slot_that_was_freed_without_unlinking_is_caught_at_the_write_site"/>
    [Fact]
    public void The_same_check_guards_a_buildings_occupants()
    {
        World world = Built();
        Handle<Building> building = world.Buildings.Rows.At(0);

        world.Households.Rows.Free(world.Households.Rows.At(0));

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.CreateHousehold(building, lifeStage: 2));

        Assert.Equal(Invariant.HouseholdIsNotAlreadyInThisBuilding, failure.Violation.Invariant);
    }

    // ---- The staggered tier ----

    /// <summary>
    /// A Household with no dwelling and no place in the Pool is still a violation.
    /// </summary>
    /// <remarks>
    /// <c>adr/0054</c> qualified this claim rather than deleting it, and this is the half that
    /// survived: <em>housed or looking</em>. A Household that is neither is a row nothing will ever
    /// touch again, and it is the only thing the unqualified check was ever catching.
    /// </remarks>
    [Fact]
    public void A_household_that_is_neither_housed_nor_in_the_pool_is_caught()
    {
        World world = Built();
        world.Buildings.Rows.Free(world.Buildings.Rows.At(0));

        Assert.Equal(Invariant.HouseholdIsHousedOrInThePool, Caught(world));
    }

    /// <summary>The other side of the exclusive-or, which a one-directional check would miss.</summary>
    /// <remarks>
    /// A Household both housed and in the Pool would be drawn for a second dwelling and would then
    /// occupy two Buildings — which the occupant lists would report as perfectly consistent, because
    /// each list is walked on its own.
    /// </remarks>
    [Fact]
    public void A_household_both_housed_and_in_the_pool_is_caught()
    {
        World world = Built();

        world.UnplacedPool.Join(world.Households, world.Households.Rows.At(0));

        Assert.Equal(Invariant.HouseholdIsHousedOrInThePool, Caught(world));
    }

    /// <summary>A Household in the Pool is legal, which is the whole point of the qualification.</summary>
    [Fact]
    public void A_household_in_the_pool_is_not_a_violation()
    {
        World world = Built();

        world.Unplace(world.Households.Rows.At(0));

        Sweep(world);
        world.Invariants.RunEndOfRun(world);
    }

    [Fact]
    public void A_household_its_home_does_not_list_is_caught()
    {
        World world = Built();
        world.Buildings.OccupantHead.Span.Clear();
        world.Buildings.OccupantTail.Span.Clear();

        Assert.Equal(Invariant.HouseholdIsAnOccupantOfItsHome, Caught(world));
    }

    [Fact]
    public void A_citizen_whose_household_is_gone_is_caught()
    {
        World world = Built();
        world.Households.Rows.Free(world.Households.Rows.At(0));

        Assert.Equal(Invariant.CitizenHouseholdExists, Caught(world));
    }

    [Fact]
    public void A_citizen_their_household_does_not_list_is_caught()
    {
        World world = Built();
        world.Households.MemberHead.Span.Clear();
        world.Households.MemberTail.Span.Clear();

        Assert.Equal(Invariant.CitizenIsAMemberOfItsHousehold, Caught(world));
    }

    /// <summary>
    /// The staggered tier covers every row across <see cref="InvariantRegistry.Slices"/> Ticks, and
    /// a violation in the last slice is found as surely as one in the first — just later. That
    /// lateness is the tier's whole price and it should be a known quantity.
    /// </summary>
    [Fact]
    public void Every_row_is_covered_within_one_full_sweep()
    {
        World world = Built();
        world.Households.MemberHead.Span.Clear();

        world.Invariants.Collect = true;

        // Advancing the world rather than handing the tier a Tick, which is the point of the Tick
        // living on the World: the slice is a function of the Tick, and there is now one Tick.
        for (int slice = 0; slice < world.Invariants.Slices; slice++)
        {
            world.Invariants.RunStaggered(world);
            world.Advance();
        }

        Assert.Contains(
            world.Invariants.Collected,
            v => v.Invariant == Invariant.CitizenIsAMemberOfItsHousehold);
    }

    /// <summary>
    /// Slicing is a partition: every row lands in exactly one slice, including when there are fewer
    /// rows than slices. A stride would have skipped rows and a naive divide would have double-counted.
    /// </summary>
    [Theory]
    [InlineData(3, 64)]
    [InlineData(64, 64)]
    [InlineData(1_000, 64)]
    [InlineData(7, 1)]
    [InlineData(0, 8)]
    public void The_slices_partition_the_table(int count, int slices)
    {
        var covered = new int[count];

        for (int slice = 0; slice < slices; slice++)
        {
            (int from, int to) = InvariantRegistry.Range(slice, slices, count);

            for (int row = from; row < to; row++)
            {
                covered[row]++;
            }
        }

        Assert.All(covered, times => Assert.Equal(1, times));
    }

    // ---- The end-of-run tier ----

    [Fact]
    public void A_handle_pointing_at_a_freed_row_is_caught()
    {
        World world = Built();
        Handle<Lot> spare = world.Lots.Create(new Tiles(9), new Tiles(9), zone: 2);
        world.Buildings.Lot[0] = spare;
        world.Lots.Rows.Free(spare);

        Assert.Equal(Invariant.CrossTableHandleResolves, CaughtAtEnd(world));
    }

    /// <summary>
    /// A Citizen in no list at all — the failure the write-site check cannot see, because nothing
    /// was written.
    /// </summary>
    [Fact]
    public void A_citizen_in_no_household_list_is_caught()
    {
        World world = Built();
        world.Members.Remove(0, 0);

        Assert.Equal(Invariant.CitizenIsInExactlyOneHousehold, CaughtAtEnd(world));
    }

    [Fact]
    public void A_household_in_no_building_list_is_caught()
    {
        World world = Built();
        world.Occupants.Remove(0, 0);

        Assert.Equal(Invariant.HouseholdIsInExactlyOneBuilding, CaughtAtEnd(world));
    }

    /// <summary>
    /// <c>adr/0006</c>'s failure arriving by the back door: a row nothing can reach and nothing will
    /// free, because the list still holds it.
    /// </summary>
    [Fact]
    public void A_freed_row_still_linked_into_a_list_is_caught()
    {
        World world = Built();
        world.Citizens.Rows.Free(world.Citizens.Rows.At(0));

        Assert.Equal(Invariant.NoFreedRowIsStillLinked, CaughtAtEnd(world));
    }

    /// <summary><c>adr/0003</c>'s overflow detector: an accumulator with no sink, at the far end.</summary>
    /// <remarks>
    /// <b>It builds its own world because <see cref="Built"/>'s Ruleset names no money</b>, and since
    /// <c>adr/0114</c> that means its Households have no balance to overflow. Leaving the fixture
    /// moneyless is deliberate: it is the world every other test in this class runs on, and the point
    /// of those is that the ordinary API violates nothing.
    /// </remarks>
    [Fact]
    public void Money_that_has_run_away_is_caught()
    {
        World world = Moneyed();
        Handle<Household> second = world.CreateHousehold(world.Buildings.Rows.At(0), lifeStage: 1);

        // Deposited rather than assigned, because a balance is a Bin since adr/0114 -- and Deposit is
        // a correct call, which is the point: what overflows is the SUM over two legitimate balances,
        // not any one of them. Two Households because one unbounded Bin cannot hold long.MaxValue
        // twice.
        world.Deposit(world.Households.Balance[0], long.MaxValue, world.Tick);
        world.Deposit(
            world.Households.Balance[world.Households.Rows.Resolve(second)],
            long.MaxValue,
            world.Tick);

        Assert.Equal(Invariant.MoneyIsRepresentable, CaughtAtEnd(world));
    }

    /// <summary>
    /// A Lot whose reverse index was never written — the shape a create path that forgets the second
    /// end of the relation leaves behind.
    /// </summary>
    /// <remarks>
    /// <b>This is the failure the whole-world tier exists for, and it is why the check walks both
    /// directions.</b> Walking Lots alone would see a vacant Lot and be content. Walking Buildings
    /// alone is what catches it: the Building names a Lot that does not name it back.
    /// </remarks>
    [Fact]
    public void A_building_its_lot_does_not_point_back_at_is_caught()
    {
        World world = Built();
        world.Lots.Vacate(0);

        Assert.Equal(Invariant.LotHoldsExactlyOneBuilding, CaughtAtEnd(world));
    }

    /// <summary>
    /// The other direction: an index left pointing at a row that has since been freed.
    /// </summary>
    /// <remarks>
    /// <b>Walking Buildings cannot see this one</b>, because there is no live Building left to walk
    /// from — which is exactly the demolition-that-did-not-vacate bug, and the reason one direction
    /// would have been a check that passed while the world was wrong.
    /// </remarks>
    [Fact]
    public void A_lot_still_holding_a_freed_building_is_caught()
    {
        // An unoccupied spare, so that no Household's Dwelling handle points at the freed row and
        // CrossTableHandleResolves does not catch it first. The point is to isolate this check.
        World world = Built();
        Handle<Lot> spare = world.Lots.Create(new Tiles(7), new Tiles(7), zone: 1);
        world.Buildings.Rows.Free(world.CreateBuilding(spare, kind: 1, default, WorldKey.FromSeed(1)));

        Assert.Equal(Invariant.LotHoldsExactlyOneBuilding, CaughtAtEnd(world));
    }

    /// <summary>
    /// <c>02 §2.2</c>'s *exactly one*, at the write site where the second one would go up.
    /// </summary>
    [Fact]
    public void Building_twice_on_one_lot_is_caught_at_the_write_site()
    {
        World world = Built();
        Handle<Lot> occupied = world.Buildings.Lot[0];

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.CreateBuilding(occupied, kind: 1, default, WorldKey.FromSeed(1)));

        Assert.Equal(Invariant.LotIsNotAlreadyBuiltOn, failure.Violation.Invariant);
    }

    /// <summary>
    /// The index is a cache, so rebuilding it from the saved relation must reproduce it exactly.
    /// </summary>
    /// <remarks>
    /// <b>The property that makes it safe to declare <c>Derived</c>.</b> If a rebuild disagreed with
    /// the incrementally maintained value, the column would be state wearing a cache's label — and
    /// because it is outside the State Hash, nothing else in the project would ever notice.
    /// </remarks>
    [Fact]
    public void Rebuilding_the_reverse_index_reproduces_it()
    {
        World world = Built();
        int before = world.Lots.BuildingSlot[0];

        world.RebuildDerived();

        Assert.Equal(before, world.Lots.BuildingSlot[0]);
        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Demolition returns the Lot to vacant, which is what lets it be built on again.</summary>
    /// <remarks>
    /// <b>An unoccupied Building, because demolishing an occupied one is still broken</b> —
    /// <c>DestroyBuilding</c> does not touch Occupants, so its Households would be left holding a
    /// handle to a freed row. That is slice 10 task 8's work and
    /// <see href="../../../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md">adr/0054</see>
    /// settles what it should do. This test is deliberately scoped to the Lot relation so that it
    /// keeps testing that once eviction lands.
    /// </remarks>
    [Fact]
    public void Demolishing_returns_the_lot_to_vacant()
    {
        World world = Built();
        Handle<Lot> lot = world.Lots.Create(new Tiles(7), new Tiles(7), zone: 1);
        Handle<Building> building = world.CreateBuilding(lot, kind: 1, default, WorldKey.FromSeed(1));
        int lotSlot = world.Lots.Rows.Resolve(lot);

        Assert.False(world.Lots.IsVacant(lotSlot));

        world.DestroyBuilding(building, default);

        Assert.True(world.Lots.IsVacant(lotSlot));

        // And the Lot is genuinely reusable rather than merely reading as empty — which is the
        // property slice 10's churn depends on and nothing has ever exercised.
        world.CreateBuilding(lot, kind: 1, default, WorldKey.FromSeed(1));
        world.Invariants.RunEndOfRun(world);
    }

    // ---- The switch, and the cost model ----

    /// <summary>
    /// Collecting finishes the run and reports, which is what a million-Tick balance run wants —
    /// <em>what is wrong with this city</em> rather than <em>where did this go wrong</em>.
    /// </summary>
    [Fact]
    public void Collecting_reports_instead_of_throwing()
    {
        World world = Built();
        world.Invariants.Collect = true;
        world.Buildings.Rows.Free(world.Buildings.Rows.At(0));

        Sweep(world);

        Assert.NotEmpty(world.Invariants.Collected);
        Assert.All(world.Invariants.Collected, v => Assert.True(v.Broken));
    }

    [Fact]
    public void Throwing_is_the_default()
    {
        Assert.False(new World(8).Invariants.Collect);
    }

    /// <summary>
    /// <b>The claim that makes <see cref="InvariantRegistry.Slices"/> a knob rather than a design
    /// decision.</b> Invariants only read, so no setting of it can move the State Hash — and by
    /// <c>05 §4</c>'s own test, a change that leaves the hash unchanged is an optimisation. Asserted
    /// rather than argued, because it is the argument that lets the number live outside the Ruleset.
    /// </summary>
    [Fact]
    public void The_stagger_period_is_not_a_hash_input()
    {
        Assert.Equal(Trace(slices: 1), Trace(slices: 997));
    }

    private static ulong[] Trace(int slices)
    {
        InputLog log = GoldenFixtures.Session();
        Simulation simulation = Replay.Start(log, Ruleset.Empty);
        simulation.World.Invariants.Slices = slices;

        var hashes = new List<ulong>();
        Replay.Trace(simulation, log, new Ticks(128), hashEvery: 8, hashes);

        return [.. hashes];
    }

    /// <summary>Runs a whole staggered sweep, so a violation anywhere in the world is reached.</summary>
    // ---- The Road Graph: invariants 31-34 ----

    /// <summary>
    /// A Segment whose endpoint has been freed is reported rather than quietly ignored.
    /// </summary>
    /// <remarks>
    /// <b>This is the failure the Staggered tier exists to catch here, and nothing else in the project
    /// would see it.</b> <c>RoadGraph.RebuildDerived</c> skips a Segment with a dangling endpoint —
    /// correctly, because a rebuild also runs on the load path and must not throw on a half-read world
    /// — so such a Segment folds into the State Hash while appearing in no adjacency. It is a road that
    /// exists for the save and does not exist for a router.
    /// </remarks>
    [Fact]
    public void A_segment_whose_node_is_gone_is_caught()
    {
        World world = WithRoads();
        world.Roads.Nodes.Rows.Free(world.Roads.Nodes.Rows.At(0));

        Assert.Equal(Invariant.RoadSegmentEndpointsExist, Caught(world));
    }

    /// <summary>A Segment joining a node to itself is reported.</summary>
    /// <remarks>
    /// A self-loop is not a road; it is a length of carriageway that leaves and arrives nowhere, and it
    /// would give a search an Arc it can relax through for ever at zero benefit.
    /// </remarks>
    [Fact]
    public void A_segment_that_loops_to_its_own_node_is_caught()
    {
        World world = WithRoads();
        world.Roads.Segments.NodeA[0] = world.Roads.Segments.NodeB[0];

        Assert.Equal(Invariant.RoadSegmentEndpointsExist, Caught(world));
    }

    /// <summary>A Segment of no length, or at no speed, is reported.</summary>
    /// <remarks>
    /// <b>Both halves are the same defect seen from two ends: a traversal whose cost is not a
    /// duration.</b> <c>TravelTime.Over</c> divides length by speed, so a zero speed is a
    /// <c>DivideByZeroException</c> in the middle of a Tick and a zero length is a free Arc — and a
    /// free Arc is worse, because it does not crash. It makes two places the same place for every
    /// router that will ever read this graph.
    /// </remarks>
    [Fact]
    public void A_segment_with_no_length_is_caught()
    {
        World world = WithRoads();
        world.Roads.Segments.LengthTiles[0] = Tiles.Zero;

        Assert.Equal(Invariant.RoadSegmentIsTraversable, Caught(world));
    }

    /// <summary>
    /// A Segment whose derived mask disagrees with the Arcs it was derived from is reported.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0072</c>'s decision, guarded.</b> The masks are saved per direction and the Segment's
    /// is their union, so the two can only disagree if something wrote the derived column directly or
    /// a rebuild did not run after an edit. Both are silent: the graph stays traversable, Severance
    /// stays plausible, and the answer to <i>may a pedestrian use this Segment</i> stops matching the
    /// answer to <i>may a pedestrian use either of its Arcs</i>.
    /// </remarks>
    [Fact]
    public void A_segment_whose_mask_disagrees_with_its_arcs_is_caught()
    {
        World world = WithRoads();
        world.Roads.Segments.Modes[0] = (byte)TravelMode.None;

        Assert.Equal(Invariant.SegmentModesAreTheUnionOfItsArcs, Caught(world));
    }

    /// <summary>
    /// An adjacency whose slices no longer tile the Arc array is reported at end of run.
    /// </summary>
    /// <remarks>
    /// <b>The property no single Arc can see.</b> Every Arc here is individually a perfectly good
    /// direction of a real Segment; what is wrong is the <em>partition</em> — one node's run now
    /// overlaps the next one's, so an Arc belongs to two nodes and another belongs to none. That is
    /// why this is one whole-world walk rather than a per-row check, and why it is the one Road Graph
    /// invariant in the end-of-run tier.
    /// </remarks>
    [Fact]
    public void An_adjacency_whose_slices_do_not_tile_the_arcs_is_caught()
    {
        World world = WithRoads();
        world.Roads.Nodes.ArcCount[0]++;

        Assert.Equal(Invariant.ArcsAreDirectionsOfTheirSegments, CaughtAtEnd(world));
    }

    /// <summary>An Arc naming a Segment that does not touch the node it hangs off is reported.</summary>
    [Fact]
    public void An_arc_that_is_not_a_direction_of_its_segment_is_caught()
    {
        World world = WithRoads();
        world.Roads.Nodes.ArcStart.Span.Clear();
        world.Roads.Nodes.ArcCount.Span.Clear();
        world.Roads.Nodes.ArcCount[0] = world.Roads.Arcs.Count;

        Assert.Equal(Invariant.ArcsAreDirectionsOfTheirSegments, CaughtAtEnd(world));
    }

    /// <summary>
    /// A hand-built graph large enough for the checks above to have something to walk.
    /// </summary>
    /// <remarks>
    /// Built through <see cref="RoadFixtures.Chain"/> and then damaged directly, which is this file's
    /// standing method: the ordinary API cannot produce any of these states, so every one of them is
    /// reached by writing to a column. A test that could reach them through the API would be reporting
    /// a defect in the API instead.
    /// </remarks>
    private static World WithRoads()
    {
        // A Ruleset with a [roads] table, and not Built()'s bare World. Free-flow is derived from the
        // Ruleset in force (adr/0064), so a world with no [roads] gives every Segment a speed of zero
        // and the first rebuild divides by it -- which is TravelTime.Over refusing to answer a question
        // with no answer, a Tick before the invariant would have said the same thing more politely.
        World world = new(1_000, RoadFixtures.With(RoadFixtures.Roads()));

        RoadGraph graph = world.Roads;
        Handle<RoadNode> previous = graph.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int i = 1; i < 6; i++)
        {
            Handle<RoadNode> next = graph.Nodes.Create(new Tiles(i * 32), Tiles.Zero);

            graph.Segments.Create(
                previous, next, new Tiles(32), RoadKind.Street, TravelMode.Any, TravelMode.Any);

            previous = next;
        }

        graph.RebuildDerived();

        return world;
    }

    private static void Sweep(World world)
    {
        // Advancing the world rather than handing the tier a Tick, which is the point of the Tick
        // living on the World: the slice is a function of the Tick, and there is now one Tick.
        for (int slice = 0; slice < world.Invariants.Slices; slice++)
        {
            world.Invariants.RunStaggered(world);
            world.Advance();
        }
    }

    /// <summary>
    /// <b>Volume on a road nobody is driving on is caught.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0041</c>'s failure mode, written by hand: <i>"a Traveller that vanishes without
    /// decrementing destroys the reading permanently … a road that looks busy forever."</i> Nothing in
    /// milestone 5b increments volume — walk Legs contribute nothing and there are no others — so this
    /// is the only way the check can be seen to work at all, and it is worth seeing before the first
    /// vehicular Leg is written rather than after.
    /// </remarks>
    [Fact]
    public void A_segment_carrying_volume_no_traveller_accounts_for_is_caught()
    {
        World world = WithRoads();
        world.Roads.Segments.VolumeForward[0]++;

        Assert.Equal(Invariant.SegmentVolumeIsConserved, CaughtAtEnd(world));
    }

    /// <summary>
    /// <b>A Trip released while still in flight is caught at the write site.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §10</c>'s <em>no Trip without a Fate</em>. <see cref="TripEngine.Release"/> is called
    /// directly because nothing else can reach it in this state — the sweep filters
    /// <see cref="TripFate.InFlight"/> out before freeing, so the guard is unreachable through
    /// <see cref="TripEngine.Advance"/> by construction.
    /// </para>
    /// <para>
    /// <b>That is the argument for the test, not against it.</b> The condition the guard protects holds
    /// today because there is exactly one release site; what it is written for is the second one, and a
    /// test that could only exercise the first would pass identically if the guard were deleted.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_trip_released_without_a_fate_is_caught()
    {
        World world = WithRoads();
        var engine = new TripEngine(world);

        Handle<Trip> trip = world.Trips.Create(
            world.Roads.Segments, TripPurpose.Commanded, Address.None, Address.None);

        Assert.Equal(
            Invariant.TripHasAFate,
            Assert.Throws<InvariantViolationException>(
                () => engine.Release(world.Trips.Rows.Resolve(trip))).Violation.Invariant);
    }

    private static Invariant Caught(World world) =>
        Assert.Throws<InvariantViolationException>(() => Sweep(world)).Violation.Invariant;

    private static Invariant CaughtAtEnd(World world) =>
        Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation.Invariant;
}
