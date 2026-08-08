using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 7 tasks 1 and 2: the Bin, the two wait lists, and the drain that decides who wakes.
/// </summary>
/// <remarks>
/// <b>The drain is the whole of the mechanism and most of what can be got wrong.</b> <c>02 §4.1</c>
/// says omitting the shortfall <em>"silently deletes the whole mechanism"</em> — every waiter wakes,
/// contends in Phase 3, and the sorted settle order picks a permanent winner — so the tests below are
/// mostly about who is <em>not</em> woken.
/// </remarks>
public sealed class BinTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);

    private static readonly RuleId Bake = new(1);

    /// <summary>A world with one Building on one Lot, and nothing else of interest.</summary>
    private static (World World, Handle<Building> Building) Built()
    {
        var world = new World(1_000);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);

        return (world, world.Buildings.Create(lot, kind: 1));
    }

    /// <summary>
    /// A Rule Instance on neither queue, which is what Phase 1 hands Phase 2.
    /// </summary>
    /// <remarks>
    /// <b>Armed and then drained, rather than fabricated asleep.</b> That is the only route a real one
    /// takes — the Wheel pops it, Phase 2 evaluates it, and it either re-arms or subscribes — and a
    /// helper that subscribed a still-armed row would leave it on two queues at once. Which is not a
    /// hypothetical: the first spelling of this helper did exactly that, and
    /// <see cref="Invariant.RuleInstanceIsArmedOrWaiting"/> is what noticed.
    /// </remarks>
    private static Handle<RuleInstance> Sleeper(World world, Handle<Building> building)
    {
        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Bake, Ticks.Zero, delay: 1);

        world.Wheel.PopDue(new Ticks(1));

        return instance;
    }

    private static int SlotOf(World world, Handle<RuleInstance> instance) =>
        world.RuleInstances.Rows.Resolve(instance);

    [Fact]
    public void A_bin_is_found_on_its_building_by_resource()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        world.CreateBin(building, Bread, capacity: BinCapacity.Of(20));

        int buildingSlot = world.Buildings.Rows.Resolve(building);

        Assert.Equal(world.Bins.Rows.Resolve(flour), world.FindBin(buildingSlot, Flour));
        Assert.Equal(Core.Tables.Rows.NoSlot, world.FindBin(buildingSlot, new ResourceId(9)));
    }

    /// <summary>Two Bins of one Resource would make <c>local</c> scope depend on allocation order.</summary>
    [Fact]
    public void A_building_refuses_a_second_bin_for_one_resource()
    {
        (World world, Handle<Building> building) = Built();

        world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Violation violation = Assert.Throws<InvariantViolationException>(
            () => world.CreateBin(building, Flour, capacity: BinCapacity.Of(50))).Violation;

        Assert.Equal(Invariant.BuildingHasOneBinPerResource, violation.Invariant);
    }

    [Fact]
    public void Depositing_raises_the_level_and_withdrawing_lowers_it()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        int slot = world.Bins.Rows.Resolve(flour);

        world.Deposit(flour, 40, Ticks.Zero);
        Assert.Equal(40, world.Bins.LevelAt(slot));
        Assert.Equal(60, world.Bins.HeadroomAt(slot));

        world.Withdraw(flour, 15, Ticks.Zero);
        Assert.Equal(25, world.Bins.LevelAt(slot));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_write_that_would_leave_the_range_is_refused(bool overCapacity)
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        world.Deposit(flour, 50, Ticks.Zero);

        Violation violation = Assert.Throws<InvariantViolationException>(
            () => (overCapacity
                ? () => world.Deposit(flour, 51, Ticks.Zero)
                : (Action)(() => world.Withdraw(flour, 51, Ticks.Zero)))()).Violation;

        Assert.Equal(Invariant.BinLevelIsWithinCapacity, violation.Invariant);
    }

    /// <summary><c>02 §4.1</c>, verbatim: six flour arriving wakes exactly the one bakery that needs six.</summary>
    [Fact]
    public void Six_flour_arriving_wakes_exactly_the_one_bakery_that_needs_six()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Handle<RuleInstance> needsSix = Sleeper(world, building);
        Handle<RuleInstance> needsTen = Sleeper(world, building);

        world.Subscribe(needsSix, flour, Blocking.Level, shortfall: 6);
        world.Subscribe(needsTen, flour, Blocking.Level, shortfall: 10);

        world.Deposit(flour, 6, Ticks.Zero);

        Assert.False(world.RuleInstances.IsWaiting(SlotOf(world, needsSix)));
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, needsTen)));
    }

    /// <summary>
    /// The drain stops at a waiter it cannot cover rather than reaching past it.
    /// </summary>
    /// <remarks>
    /// <b>The starvation test.</b> Skipping the head to satisfy a smaller waiter behind it is the
    /// cheap implementation and it makes the queue meaningless in exactly the case it exists for: the
    /// largest waiter never advances, for the life of the city.
    /// </remarks>
    [Fact]
    public void The_drain_stops_at_an_uncovered_waiter_rather_than_skipping_it()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Handle<RuleInstance> big = Sleeper(world, building);
        Handle<RuleInstance> small = Sleeper(world, building);

        world.Subscribe(big, flour, Blocking.Level, shortfall: 10);
        world.Subscribe(small, flour, Blocking.Level, shortfall: 2);

        world.Deposit(flour, 6, Ticks.Zero);

        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, big)));
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, small)));
    }

    /// <summary>The budget is spent down, so six covers two waiters needing three each and no more.</summary>
    [Fact]
    public void The_arriving_quantity_is_spent_down_across_the_waiters_it_covers()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Handle<RuleInstance> first = Sleeper(world, building);
        Handle<RuleInstance> second = Sleeper(world, building);
        Handle<RuleInstance> third = Sleeper(world, building);

        world.Subscribe(first, flour, Blocking.Level, shortfall: 3);
        world.Subscribe(second, flour, Blocking.Level, shortfall: 3);
        world.Subscribe(third, flour, Blocking.Level, shortfall: 3);

        world.Deposit(flour, 6, Ticks.Zero);

        Assert.False(world.RuleInstances.IsWaiting(SlotOf(world, first)));
        Assert.False(world.RuleInstances.IsWaiting(SlotOf(world, second)));
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, third)));
    }

    /// <summary>
    /// A deposit and a withdrawal wake opposite queues, and neither reaches the other's.
    /// </summary>
    /// <remarks>
    /// <c>adr/0045</c>'s <em>blocking</em> is two failure modes, and one queue holding both would
    /// deadlock: a deposit can never cover a waiter that needs headroom, and a drain that stops at the
    /// first waiter it cannot cover would stop at that one for ever.
    /// </remarks>
    [Fact]
    public void A_full_output_bin_is_rescued_by_a_withdrawal_and_never_by_a_deposit()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> bread = world.CreateBin(building, Bread, capacity: BinCapacity.Of(20));
        world.Deposit(bread, 20, Ticks.Zero);

        Handle<RuleInstance> blocked = Sleeper(world, building);
        world.Subscribe(blocked, bread, Blocking.Headroom, shortfall: 4);

        // Nothing a deposit could do helps, and at capacity there is not even room to try.
        world.Deposit(bread, 0, Ticks.Zero);
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, blocked)));

        world.Withdraw(bread, 4, Ticks.Zero);
        Assert.False(world.RuleInstances.IsWaiting(SlotOf(world, blocked)));
    }

    /// <summary>A waking Rule lands on the Wheel for the next Tick, because Phase 2 has already run.</summary>
    [Fact]
    public void A_woken_rule_is_armed_for_the_next_tick()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> waiter = Sleeper(world, building);

        world.Subscribe(waiter, flour, Blocking.Level, shortfall: 5);
        world.Deposit(flour, 5, new Ticks(40));

        int slot = SlotOf(world, waiter);

        Assert.Equal(new Ticks(41), world.RuleInstances.NextTick[slot]);
        Assert.Equal(0, world.RuleInstances.Shortfall[slot]);
        Assert.True(world.RuleInstances.WaitingOn[slot].IsNone);
        Assert.Equal(slot, world.Wheel.PopDue(new Ticks(41)));
    }

    /// <summary>Arrival order, which is what makes the round-robin drain fair.</summary>
    [Fact]
    public void The_wait_list_drains_in_arrival_order()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Handle<RuleInstance> first = Sleeper(world, building);
        Handle<RuleInstance> second = Sleeper(world, building);

        // The later subscriber asks for less, so a queue ordered by anything but arrival would
        // reach it first.
        world.Subscribe(first, flour, Blocking.Level, shortfall: 4);
        world.Subscribe(second, flour, Blocking.Level, shortfall: 1);

        world.Deposit(flour, 1, Ticks.Zero);

        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, first)));
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, second)));
    }

    /// <summary>Subscribing a Rule that is already asleep would be a Rule that polls and subscribes.</summary>
    [Fact]
    public void A_rule_cannot_subscribe_twice()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> waiter = Sleeper(world, building);

        world.Subscribe(waiter, flour, Blocking.Level, shortfall: 5);

        Violation violation = Assert.Throws<InvariantViolationException>(
            () => world.Subscribe(waiter, flour, Blocking.Level, shortfall: 5)).Violation;

        Assert.Equal(Invariant.RuleInstanceIsArmedOrWaiting, violation.Invariant);
    }

    /// <summary>
    /// Demolition frees the Bins and the Rules, and wakes whoever was asleep on those Bins.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0006</c>'s rule, and <c>05 §9</c>'s.</b> A waiter left asleep on a Bin that no longer
    /// exists is a row nothing will ever wake, because nothing will ever write that Bin again.
    /// </remarks>
    [Fact]
    public void Demolishing_a_building_frees_its_rows_and_wakes_its_bins_waiters()
    {
        (World world, Handle<Building> doomed) = Built();

        Handle<Lot> lot = world.Lots.Create(new Tiles(9), new Tiles(9), zone: 1);
        Handle<Building> neighbour = world.Buildings.Create(lot, kind: 1);

        Handle<Bin> flour = world.CreateBin(doomed, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> own = Sleeper(world, doomed);
        Handle<RuleInstance> foreign = Sleeper(world, neighbour);

        world.Subscribe(own, flour, Blocking.Level, shortfall: 50);
        world.Subscribe(foreign, flour, Blocking.Level, shortfall: 50);

        int foreignSlot = SlotOf(world, foreign);

        world.DestroyBuilding(doomed, new Ticks(7));

        Assert.False(world.Bins.Rows.IsValid(flour));
        Assert.False(world.RuleInstances.Rows.IsValid(own));

        Assert.True(world.RuleInstances.Rows.IsValid(foreign));
        Assert.False(world.RuleInstances.IsWaiting(foreignSlot));
        Assert.Equal(new Ticks(8), world.RuleInstances.NextTick[foreignSlot]);

        world.Invariants.RunEndOfRun(world, default);
    }

    /// <summary>
    /// The Bin and Rule lists rebuild from saved state; the wait lists are not touched, and are not
    /// rebuildable.
    /// </summary>
    /// <remarks>
    /// <b>The two declarations one indirection apart, checked against each other.</b> If the wait
    /// lists were derived, this test would pass while the queue silently re-ordered on every load.
    /// The hash is the oracle: it folds the wait lists and not the Bin list, so a rebuild that moved
    /// either would move it.
    /// </remarks>
    [Fact]
    public void Rebuilding_derived_state_restores_the_lists_and_leaves_the_hash_alone()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        world.CreateBin(building, Bread, capacity: BinCapacity.Of(20));
        world.Deposit(flour, 30, Ticks.Zero);

        Handle<RuleInstance> first = Sleeper(world, building);
        Handle<RuleInstance> second = Sleeper(world, building);

        world.Subscribe(first, flour, Blocking.Level, shortfall: 90);
        world.Subscribe(second, flour, Blocking.Level, shortfall: 95);

        int buildingSlot = world.Buildings.Rows.Resolve(building);
        int binSlot = world.Bins.Rows.Resolve(flour);

        ulong before = world.HashState();

        world.RebuildDerived();

        Assert.Equal(before, world.HashState());
        Assert.Equal(binSlot, world.FindBin(buildingSlot, Flour));
        Assert.Equal(SlotOf(world, first), world.LevelWaiters.PeekFront(binSlot));

        Assert.Equal(
            [SlotOf(world, first), SlotOf(world, second)],
            Walk(world.BuildingRules, buildingSlot));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData((uint)EventWheel.Size)]
    [InlineData((uint)EventWheel.Size + 1)]
    public void The_wheel_refuses_an_arming_that_would_land_in_the_bucket_being_drained(uint delay)
    {
        (World world, Handle<Building> building) = Built();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => world.CreateRuleInstance(building, Bake, Ticks.Zero, delay));
    }

    [Fact]
    public void An_armed_rule_comes_off_the_wheel_on_its_tick_and_only_then()
    {
        (World world, Handle<Building> building) = Built();

        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Bake, new Ticks(100), delay: 10);

        Assert.Equal(Core.Tables.Rows.NoSlot, world.Wheel.PopDue(new Ticks(109)));
        Assert.Equal(SlotOf(world, instance), world.Wheel.PopDue(new Ticks(110)));
        Assert.Equal(Core.Tables.Rows.NoSlot, world.Wheel.PopDue(new Ticks(110)));
    }

    /// <summary>
    /// A Rule Instance on no queue at all is found, which is the failure sharing one link makes easy.
    /// </summary>
    /// <remarks>
    /// Reached by corrupting the world directly, per this suite's rule: the API maintains the
    /// invariant, so a violation reachable through it would be a bug to fix rather than a check to
    /// test. Taking the row off the wait list without arming it is exactly what a Phase 2 that forgot
    /// to resubscribe would do.
    /// </remarks>
    [Fact]
    public void A_rule_instance_on_no_queue_at_all_is_caught_at_the_end_of_the_run()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> orphan = Sleeper(world, building);

        world.Subscribe(orphan, flour, Blocking.Level, shortfall: 5);
        world.LevelWaiters.PopFront(world.Bins.Rows.Resolve(flour));

        Assert.Equal(
            Invariant.RuleInstanceIsArmedOrWaiting,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world, default)).Violation.Invariant);
    }

    /// <summary>A row queued on one Bin while naming another sleeps through the write it needs.</summary>
    [Fact]
    public void A_waiter_queued_on_a_bin_it_does_not_name_is_caught_at_the_end_of_the_run()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<Bin> bread = world.CreateBin(building, Bread, capacity: BinCapacity.Of(20));
        Handle<RuleInstance> waiter = Sleeper(world, building);

        world.Subscribe(waiter, flour, Blocking.Level, shortfall: 5);
        world.RuleInstances.WaitingOn[SlotOf(world, waiter)] = bread;

        Assert.Equal(
            Invariant.WaiterIsQueuedOnTheBinItNames,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world, default)).Violation.Invariant);
    }

    private static int[] Walk(IndexList list, int owner)
    {
        var found = new List<int>();

        foreach (int element in list.Walk(owner))
        {
            found.Add(element);
        }

        return [.. found];
    }
}
