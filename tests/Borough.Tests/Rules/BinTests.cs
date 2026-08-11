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
/// <para>
/// <b>The drain is the whole of the mechanism and most of what can be got wrong.</b> <c>02 §4.1</c>
/// says omitting the requirement <em>"silently deletes the whole mechanism"</em> — every waiter wakes,
/// and small waiters beat large ones on quantity — so the tests below are mostly about who is
/// <em>not</em> woken.
/// </para>
/// <para>
/// <b>Not on identity, which this comment used to claim.</b> The old wording had the sorted settle
/// order picking a permanent winner; there is no sorted settle order, and <c>02 §1.1</c> says so —
/// contention is a counter-based shuffle keyed on the Tick. Corrected under <c>adr/0063</c>, which
/// found the same error in three places.
/// </para>
/// <para>
/// <b>A waiter's requirement is a property of its Rule, not of its subscription</b> (<c>adr/0063</c>),
/// so these fixtures choose <em>which Rule</em> a sleeper runs rather than passing a number to
/// <c>Subscribe</c>. See <see cref="Needing"/> and <see cref="Filling"/>.
/// </para>
/// </remarks>
public sealed class BinTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);

    private static readonly RuleId Bake = new(1);

    /// <summary>The largest requirement any test below asks for.</summary>
    private const int Widest = 95;

    private const byte Kind = 1;

    /// <summary>A Rule that needs <paramref name="level"/> flour on its own Bin to run at all.</summary>
    private static RuleId Needing(int level) => new((ushort)(1 + level));

    /// <summary>A Rule that needs <paramref name="headroom"/> room in its own bread Bin to run.</summary>
    private static RuleId Filling(int headroom) => new((ushort)(1 + Widest + headroom));

    /// <summary>
    /// A Ruleset declaring kind 1, one term-free Rule, and a Rule per requirement the tests use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It declares no Bins, because these tests create theirs by hand, and it exists for one
    /// reason: <see cref="Invariant.DerelictBuildingRunsNoRules"/>.</b> A Building of an undeclared
    /// kind running a Rule Instance is dereliction with the Rules left armed, which is slice 8's
    /// migration defect — and a fixture that ran on <see cref="Ruleset.Empty"/> was that shape by
    /// accident, so it would have reported the defect for ever without one existing.
    /// </para>
    /// <para>
    /// <b>The band carries the requirement and the term stays at one</b>, so <c>floor × |net|</c> is
    /// the number the test names. Spelling it the other way round — one application of <c>n</c> — would
    /// derive the same product, and this way a single pair of Term arrays serves every Rule.
    /// </para>
    /// <para>
    /// <b>A Rule with no terms is kept, and it is not decoration.</b> Most of this suite is about the
    /// Wheel, dereliction and queue hygiene rather than about quantities, and those fixtures should not
    /// have to own a Bin to sleep on one.
    /// </para>
    /// </remarks>
    private static Ruleset Declaring()
    {
        var rules = new RuleDefinition[1 + (2 * Widest)];
        var kindRules = new RuleId[rules.Length];

        rules[0] = Definition(ApplyCount.Band(1, 1), inputs: 0, outputs: 0);

        for (int amount = 1; amount <= Widest; amount++)
        {
            ApplyCount band = ApplyCount.Band(amount, amount);

            rules[Needing(amount).Raw - 1] = Definition(band, inputs: 1, outputs: 0);
            rules[Filling(amount).Raw - 1] = Definition(band, inputs: 0, outputs: 1);
        }

        for (int i = 0; i < kindRules.Length; i++)
        {
            kindRules[i] = new RuleId((ushort)(i + 1));
        }

        return new Ruleset(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules: rules,
            kinds: [new KindDefinition(0, 0, 0, rules.Length)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 1)],
            outputs: [new Term(new BinRef(Scope.Local, Bread), 1)],
            emissions: [],
            bins: [],
            kindRules: kindRules,
            zoneRules: []);
    }

    private static RuleDefinition Definition(ApplyCount band, int inputs, int outputs) =>
        new(Kind, 8, band, RuleId.None, false, default, ConditionId.None, 0, inputs, 0, outputs, 0, 0);

    /// <summary>A world with one Building on one Lot, and nothing else of interest.</summary>
    private static (World World, Handle<Building> Building) Built()
    {
        var world = new World(1_000, Declaring());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);

        return (world, world.Buildings.Create(world.Lots, lot, Kind));
    }

    /// <summary>
    /// The Tick a <see cref="Sleeper"/> was drained on, and so the earliest Tick anything may wake it.
    /// </summary>
    /// <remarks>
    /// <b>A fixture cannot pop a row for Tick 1 and then deposit on Tick 0.</b> An armed row is
    /// distinguished from one in flight by <c>NextTick > now</c>
    /// (<see cref="EventWheel.IsArmed"/>), which is a statement about a clock that only moves
    /// forward — so a test running time backwards makes a drained row read as still armed, and
    /// <see cref="Invariant.RuleInstanceIsArmedOrWaiting"/> reports it. The Simulation cannot do this;
    /// three fixtures here could, and did.
    /// </remarks>
    private static readonly Ticks Woken = new(1);

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
    private static Handle<RuleInstance> Sleeper(World world, Handle<Building> building, RuleId rule)
    {
        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, rule, Ticks.Zero, delay: 1);

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

        Handle<RuleInstance> needsSix = Sleeper(world, building, Needing(6));
        Handle<RuleInstance> needsTen = Sleeper(world, building, Needing(10));

        world.Subscribe(needsSix, flour, Blocking.Level);
        world.Subscribe(needsTen, flour, Blocking.Level);

        world.Deposit(flour, 6, Woken);

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

        Handle<RuleInstance> big = Sleeper(world, building, Needing(10));
        Handle<RuleInstance> small = Sleeper(world, building, Needing(2));

        world.Subscribe(big, flour, Blocking.Level);
        world.Subscribe(small, flour, Blocking.Level);

        world.Deposit(flour, 6, Woken);

        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, big)));
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, small)));
    }

    /// <summary>The budget is spent down, so six covers two waiters needing three each and no more.</summary>
    [Fact]
    public void The_arriving_quantity_is_spent_down_across_the_waiters_it_covers()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Handle<RuleInstance> first = Sleeper(world, building, Needing(3));
        Handle<RuleInstance> second = Sleeper(world, building, Needing(3));
        Handle<RuleInstance> third = Sleeper(world, building, Needing(3));

        world.Subscribe(first, flour, Blocking.Level);
        world.Subscribe(second, flour, Blocking.Level);
        world.Subscribe(third, flour, Blocking.Level);

        world.Deposit(flour, 6, Woken);

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

        Handle<RuleInstance> blocked = Sleeper(world, building, Filling(4));
        world.Subscribe(blocked, bread, Blocking.Headroom);

        // Nothing a deposit could do helps, and at capacity there is not even room to try.
        world.Deposit(bread, 0, Woken);
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, blocked)));

        world.Withdraw(bread, 4, Woken);
        Assert.False(world.RuleInstances.IsWaiting(SlotOf(world, blocked)));
    }

    /// <summary>A waking Rule lands on the Wheel for the next Tick, because Phase 2 has already run.</summary>
    [Fact]
    public void A_woken_rule_is_armed_for_the_next_tick()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> waiter = Sleeper(world, building, Needing(5));

        world.Subscribe(waiter, flour, Blocking.Level);
        world.Deposit(flour, 5, new Ticks(40));

        int slot = SlotOf(world, waiter);

        Assert.Equal(new Ticks(41), world.RuleInstances.NextTick[slot]);
        Assert.True(world.RuleInstances.WaitingOn[slot].IsNone);
        Assert.Equal(slot, world.Wheel.PopDue(new Ticks(41)));
    }

    /// <summary>Arrival order, which is what makes the round-robin drain fair.</summary>
    [Fact]
    public void The_wait_list_drains_in_arrival_order()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));

        Handle<RuleInstance> first = Sleeper(world, building, Needing(4));
        Handle<RuleInstance> second = Sleeper(world, building, Needing(1));

        // The later subscriber asks for less, so a queue ordered by anything but arrival would
        // reach it first.
        world.Subscribe(first, flour, Blocking.Level);
        world.Subscribe(second, flour, Blocking.Level);

        world.Deposit(flour, 1, Woken);

        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, first)));
        Assert.True(world.RuleInstances.IsWaiting(SlotOf(world, second)));
    }

    /// <summary>Subscribing a Rule that is already asleep would be a Rule that polls and subscribes.</summary>
    [Fact]
    public void A_rule_cannot_subscribe_twice()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> waiter = Sleeper(world, building, Needing(5));

        world.Subscribe(waiter, flour, Blocking.Level);

        Violation violation = Assert.Throws<InvariantViolationException>(
            () => world.Subscribe(waiter, flour, Blocking.Level)).Violation;

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
        Handle<Building> neighbour = world.Buildings.Create(world.Lots, lot, kind: 1);

        Handle<Bin> flour = world.CreateBin(doomed, Flour, capacity: BinCapacity.Of(100));
        Handle<RuleInstance> own = Sleeper(world, doomed, Needing(50));
        Handle<RuleInstance> foreign = Sleeper(world, neighbour, Bake);

        world.Subscribe(own, flour, Blocking.Level);
        world.Subscribe(foreign, flour, Blocking.Level);

        int foreignSlot = SlotOf(world, foreign);

        world.DestroyBuilding(doomed, new Ticks(7));

        Assert.False(world.Bins.Rows.IsValid(flour));
        Assert.False(world.RuleInstances.Rows.IsValid(own));

        Assert.True(world.RuleInstances.Rows.IsValid(foreign));
        Assert.False(world.RuleInstances.IsWaiting(foreignSlot));
        Assert.Equal(new Ticks(8), world.RuleInstances.NextTick[foreignSlot]);

        world.Invariants.RunEndOfRun(world);
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

        Handle<RuleInstance> first = Sleeper(world, building, Needing(90));
        Handle<RuleInstance> second = Sleeper(world, building, Needing(95));

        world.Subscribe(first, flour, Blocking.Level);
        world.Subscribe(second, flour, Blocking.Level);

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
        Handle<RuleInstance> orphan = Sleeper(world, building, Needing(5));

        world.Subscribe(orphan, flour, Blocking.Level);
        world.LevelWaiters.PopFront(world.Bins.Rows.Resolve(flour));

        Assert.Equal(
            Invariant.RuleInstanceIsArmedOrWaiting,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world)).Violation.Invariant);
    }

    /// <summary>
    /// Arming a row that is already armed is refused where it happens, not at the end of the run.
    /// </summary>
    /// <remarks>
    /// <b>The symmetric half of <see cref="A_rule_cannot_subscribe_twice"/>, and it was missing.</b>
    /// A second arming appends the row to a second bucket, so it fires twice and one of the two entries
    /// survives every later unlink — detectable at the end of a run by a count of two, by which point
    /// the call site that did it is gone.
    /// </remarks>
    [Fact]
    public void The_wheel_refuses_arming_a_row_that_is_already_armed()
    {
        (World world, Handle<Building> building) = Built();

        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Bake, Ticks.Zero, delay: 5);

        Assert.Equal(
            Invariant.RuleInstanceIsArmedOrWaiting,
            Assert.Throws<InvariantViolationException>(
                () => world.Wheel.Arm(SlotOf(world, instance), Ticks.Zero, delay: 7))
                .Violation.Invariant);
    }

    /// <summary>
    /// The row Phase 1 popped may be armed again on the same Tick, which is the ordinary re-arm.
    /// </summary>
    /// <remarks>
    /// <b>The control for the refusal above, and the reason it is a strict comparison.</b> A popped row
    /// still reads <see cref="Blocking.Nothing"/> and still carries the <c>NextTick</c> it fired on, so
    /// a refusal written as <em>armed or in flight</em> would refuse every success in the engine's
    /// hottest path.
    /// </remarks>
    [Fact]
    public void A_row_the_wheel_has_popped_may_be_armed_again_on_the_same_tick()
    {
        (World world, Handle<Building> building) = Built();

        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Bake, Ticks.Zero, delay: 1);

        int slot = SlotOf(world, instance);

        Assert.Equal(slot, world.Wheel.PopDue(Woken));

        world.Wheel.Arm(slot, Woken, delay: 32);

        Assert.Equal(slot, world.Wheel.Armed.PeekFront(EventWheel.BucketOf(new Ticks(33))));
    }

    /// <summary>
    /// A row whose <c>NextTick</c> is a whole period stale is caught, and the bucket cannot catch it.
    /// </summary>
    /// <remarks>
    /// <b>The error a modulus makes, and the reason this check is written in absolute Ticks.</b>
    /// <c>BucketOf</c> is <c>NextTick % WHEEL_SIZE</c>, so adding a period leaves the row in the same
    /// bucket and every test written modulo the period still passes — while the row is now scheduled
    /// for a Tick the drain reached long ago. Corrupted directly, per this suite's rule: the two
    /// mechanisms that could produce it are a reload refit, which re-arms on the
    /// <c>[1, rate]</c> stagger and therefore cannot, and a load from a save, which does not exist.
    /// </remarks>
    [Fact]
    public void An_armed_row_a_whole_period_stale_is_caught_at_the_end_of_the_run()
    {
        (World world, Handle<Building> building) = Built();

        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Bake, Ticks.Zero, delay: 5);

        int slot = SlotOf(world, instance);
        Ticks stale = world.RuleInstances.NextTick[slot] + new Ticks(EventWheel.Size);

        // Same bucket, so the bucket test below it still holds. That is the whole point.
        Assert.Equal(
            EventWheel.BucketOf(world.RuleInstances.NextTick[slot]), EventWheel.BucketOf(stale));

        world.RuleInstances.NextTick[slot] = stale;

        Assert.Equal(
            Invariant.AnArmedRowIsDueWithinOnePeriod,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world)).Violation.Invariant);
    }

    /// <summary>
    /// Unlinking a row that says it is armed and is not in its bucket reports rather than passing.
    /// </summary>
    /// <remarks>
    /// <b><see cref="IndexList.Remove"/> returns whether the node was there, and discarding that was
    /// the defect.</b> A row in flight between Phase 1 and Phase 3 reads
    /// <see cref="Blocking.Nothing"/> while sitting in no bucket, so a caller unlinking one would
    /// silently remove nothing and free a row the engine is still holding. Both real callers are safe
    /// by the phase order — demolition is phase 6, a reload refit is phase 0 — and nothing stated that
    /// until this check existed.
    /// </remarks>
    [Fact]
    public void Unlinking_a_row_that_is_not_in_the_bucket_it_names_is_reported()
    {
        (World world, Handle<Building> building) = Built();

        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Bake, Ticks.Zero, delay: 5);

        // Moving the row's due Tick to another bucket without moving the row is what an in-flight
        // unlink looks like from inside Unlink: Blocked says armed, and the named bucket is empty.
        world.RuleInstances.NextTick[SlotOf(world, instance)] = new Ticks(6);

        Assert.Equal(
            Invariant.RuleInstanceIsArmedOrWaiting,
            Assert.Throws<InvariantViolationException>(
                () => world.DestroyBuilding(building, new Ticks(7))).Violation.Invariant);
    }

    /// <summary>A row queued on one Bin while naming another sleeps through the write it needs.</summary>
    [Fact]
    public void A_waiter_queued_on_a_bin_it_does_not_name_is_caught_at_the_end_of_the_run()
    {
        (World world, Handle<Building> building) = Built();

        Handle<Bin> flour = world.CreateBin(building, Flour, capacity: BinCapacity.Of(100));
        Handle<Bin> bread = world.CreateBin(building, Bread, capacity: BinCapacity.Of(20));
        Handle<RuleInstance> waiter = Sleeper(world, building, Needing(5));

        world.Subscribe(waiter, flour, Blocking.Level);
        world.RuleInstances.WaitingOn[SlotOf(world, waiter)] = bread;

        Assert.Equal(
            Invariant.WaiterIsQueuedOnTheBinItNames,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world)).Violation.Invariant);
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
