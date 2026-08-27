using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0046</c> stage 0: the second tier of the Event Wheel, a bucket per Day above the Ticks.
/// </summary>
/// <remarks>
/// <para>
/// <b>The wheel and its consumer were each cited as the other's reason not to exist.</b>
/// <see cref="EventWheel.Arm"/>'s refusal said a long sleep <i>has no consumer until Life Stages
/// arrive</i>; Life Stages were deferred because the coarse wheel did not exist. Each half was true
/// where it was written, and nothing in the corpus was shaped to notice the cycle. <c>plans/0046</c>
/// broke it by building the wheel first.
/// </para>
/// <para>
/// <b>What is actually new is one method.</b> <see cref="EventWheel.Cascade"/> drains one Day's
/// bucket onto the fine wheel at midnight. The armings, the drain, the link column and the
/// <c>{armed, waiting}</c> partition are all unchanged — a coarse row is armed in exactly the sense
/// <see cref="EventWheel.IsArmed"/> already meant, because <c>NextTick</c> was always an absolute
/// Tick and never an offset.
/// </para>
/// </remarks>
public sealed class CoarseWheelTests
{
    private const byte Kind = 1;

    private static readonly RuleId Sleep = new(1);

    /// <summary>One kind, one term-free Rule, and nothing else.</summary>
    private static Ruleset Declaring()
    {
        RuleDefinition[] rules =
        [
            new(Kind, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                ConditionId.None, 0, 0, 0, 0, 0, 0),
        ];

        return new Ruleset(
            resources: [],
            rules: rules,
            kinds: [new KindDefinition(0, 0, 0, 1)],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [Sleep],
            zoneRules: []);
    }

    private static (World World, Handle<Building> Building) Built()
    {
        var world = new World(1_000, Declaring());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);

        return (world, world.Buildings.Create(world.Lots, lot, Kind));
    }

    private static int SlotOf(World world, Handle<RuleInstance> instance) =>
        world.RuleInstances.Rows.Resolve(instance);

    /// <summary>The one row in a bucket, asserting there is exactly one.</summary>
    /// <remarks>
    /// <b>Hand-rolled because <see cref="IndexListWalk"/> is a <c>ref struct</c> and LINQ cannot
    /// touch it</b>, which is the point of it: an enumerator that cannot be boxed is one that cannot
    /// be captured into a lambda and walked after the prefix it runs over has moved.
    /// </remarks>
    private static int Only(IndexList list, int bucket)
    {
        int found = Rows.NoSlot;
        int seen = 0;

        foreach (int slot in list.Walk(bucket))
        {
            found = slot;
            seen++;
        }

        Assert.Equal(1, seen);

        return found;
    }

    /// <summary>How many rows a bucket holds.</summary>
    private static int Count(IndexList list, int bucket)
    {
        int seen = 0;

        foreach (int unused in list.Walk(bucket))
        {
            seen++;
        }

        return seen;
    }

    /// <summary>
    /// A sleep past a Day goes on the coarse wheel, and the fine wheel never sees it until its Day.
    /// </summary>
    /// <remarks>
    /// <b>The assertion that matters is the negative one.</b> A cascade that ran every Tick, or a
    /// tier chosen by the due Tick's bucket rather than by the delay, would both put the row on the
    /// fine wheel immediately — and the row would then fire on the right <em>phase</em> of the wrong
    /// <em>Day</em>, which is a defect no test of "does it eventually fire" can see.
    /// </remarks>
    [Fact]
    public void A_sleep_past_a_day_waits_on_the_coarse_wheel_until_its_own_day()
    {
        (World world, Handle<Building> building) = Built();

        const uint Delay = (3 * Ticks.PerDay) + 700;

        Handle<RuleInstance> instance =
            world.CreateRuleInstance(building, Sleep, Ticks.Zero, Delay);

        int slot = SlotOf(world, instance);

        Assert.Equal(slot, Only(world.Wheel.CoarseArmed, 3));
        Assert.Equal(0, Count(world.Wheel.Armed, 700));

        // Every midnight up to but not including its own leaves it where it is: the cascade drains
        // one bucket, and 3 is not 1 or 2.
        for (int day = 1; day <= 2; day++)
        {
            world.Wheel.Cascade(new Ticks((ulong)day * Ticks.PerDay));
            Assert.Equal(slot, Only(world.Wheel.CoarseArmed, 3));
        }

        world.Wheel.Cascade(new Ticks(3 * (ulong)Ticks.PerDay));

        Assert.Equal(0, Count(world.Wheel.CoarseArmed, 3));
        Assert.Equal(slot, Only(world.Wheel.Armed, 700));
        Assert.Equal(slot, world.Wheel.PopDue(new Ticks(Delay)));
    }

    /// <summary>
    /// 🔴 <b>The ordering claim: cascade before drain, or one arming in 2,048 loses a whole period.</b>
    /// </summary>
    /// <remarks>
    /// A sleep ending at exactly midnight cascades into fine bucket 0 on the same Tick that drains
    /// fine bucket 0. Cascading first fires it on the Tick it was armed for; cascading second leaves
    /// it sitting until the bucket comes round again. ***The requirement is invisible from either
    /// method alone***, which is why it is asserted here rather than left as a comment in
    /// <c>Simulation.Wake</c> — though it is stated there too.
    /// </remarks>
    [Fact]
    public void A_sleep_ending_at_midnight_fires_at_midnight()
    {
        (World world, Handle<Building> building) = Built();

        const uint Delay = 5 * Ticks.PerDay;
        var midnight = new Ticks(Delay);

        int slot = SlotOf(
            world, world.CreateRuleInstance(building, Sleep, Ticks.Zero, Delay));

        // The drain alone finds nothing: the row is still a Day-bucket away.
        Assert.Equal(Rows.NoSlot, world.Wheel.PopDue(midnight));

        world.Wheel.Cascade(midnight);

        Assert.Equal(slot, world.Wheel.PopDue(midnight));
    }

    /// <summary>The tier is chosen by the delay, and the boundary sits between 2,047 and 2,048.</summary>
    /// <remarks>
    /// <b>A delay of 2,047 made at phase 1 falls due tomorrow and still belongs on the fine wheel</b>,
    /// because the fine wheel's period is exactly a Day and <c>BucketOf</c> does not care which Day it
    /// is. Choosing the tier by the due Tick's distance instead would send this one to the coarse
    /// wheel, where it would wait a further Day for its cascade.
    /// </remarks>
    [Fact]
    public void The_tier_is_chosen_by_the_delay_and_not_by_the_day_the_row_falls_due_on()
    {
        (World world, Handle<Building> building) = Built();

        int fine = SlotOf(
            world, world.CreateRuleInstance(building, Sleep, new Ticks(1), delay: Ticks.PerDay - 1));

        Assert.Equal(fine, Only(world.Wheel.Armed, 0));
        Assert.Equal(fine, world.Wheel.PopDue(new Ticks(Ticks.PerDay)));
    }

    /// <summary>
    /// The refusal moved to the coarse ceiling and is still a wrap rather than a capacity.
    /// </summary>
    /// <remarks>
    /// <b>127 Days rather than 128, and the missing Day is the phase.</b> An arming made partway
    /// through a Day falls due up to one Day beyond <c>⌊delay / 2048⌋</c>, so a delay of a full
    /// <see cref="EventWheel.CoarseDays"/> worth of Ticks can land on the bucket it was armed from.
    /// </remarks>
    [Theory]
    [InlineData(0u)]
    [InlineData((uint)EventWheel.CoarseCeilingTicks)]
    [InlineData((uint)EventWheel.CoarseCeilingTicks + 1)]
    public void The_wheel_refuses_an_arming_it_has_no_tier_for(uint delay)
    {
        (World world, Handle<Building> building) = Built();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => world.CreateRuleInstance(building, Sleep, Ticks.Zero, delay));
    }

    /// <summary>The longest arming the wheel accepts is armed, cascaded and fired.</summary>
    /// <remarks>
    /// <b>The ceiling is exercised rather than trusted.</b> A bound stated as a constant and never run
    /// against is a bound nobody has checked the arithmetic of — and the arithmetic here is the one
    /// thing about this tier that is easy to get wrong by one.
    /// </remarks>
    [Fact]
    public void The_longest_accepted_arming_survives_its_whole_sleep()
    {
        (World world, Handle<Building> building) = Built();

        var delay = (uint)(EventWheel.CoarseCeilingTicks - 1);
        var due = new Ticks(delay);

        int slot = SlotOf(world, world.CreateRuleInstance(building, Sleep, Ticks.Zero, delay));

        // Up to the last midnight at or before the due Tick, and no further: past it the row is
        // overdue on the fine wheel and AnArmedRowIsDueWithinOnePeriod says so correctly. A real run
        // pops it on the Tick it is due; this fixture pops it once, below.
        for (ulong day = 1; day * Ticks.PerDay <= delay; day++)
        {
            var midnight = new Ticks(day * Ticks.PerDay);

            // The clock is moved by hand because the invariant reads World.Tick and this fixture
            // has no Simulation to advance it. That is exactly the state a real run is in at
            // midnight, and it is what makes the check below mean anything: AnArmedRowIsDueWithinOne
            // Period is relative to NOW, so a cascaded row is only inside its window while the clock
            // agrees the Day has started.
            world.Clock.Tick[0] = midnight;
            world.Wheel.Cascade(midnight);
            world.Invariants.RunEndOfRun(world);
        }

        Assert.Equal(slot, world.Wheel.PopDue(due));
    }

    /// <summary>
    /// A coarse row a whole coarse period stale is caught, and the fine wheel's claim cannot catch it.
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0036</c> decision 4 asked for two claims and this is why.</b> Adding 128 Days to a
    /// coarse row's <c>NextTick</c> leaves it in the same coarse bucket, so every test written modulo
    /// the period still passes while the row is now scheduled for a Day the cascade reached long ago.
    /// Corrupted directly, as <c>BinTests</c> does for the fine wheel's version of the same error.
    /// </remarks>
    [Fact]
    public void A_coarse_row_a_whole_period_stale_is_caught_at_the_end_of_the_run()
    {
        (World world, Handle<Building> building) = Built();

        int slot = SlotOf(
            world,
            world.CreateRuleInstance(building, Sleep, Ticks.Zero, delay: 4 * Ticks.PerDay));

        Ticks stale = world.RuleInstances.NextTick[slot]
            + new Ticks((ulong)EventWheel.CoarseDays * Ticks.PerDay);

        // Same coarse bucket, so the bucket half of the claim still holds. That is the whole point.
        Assert.Equal(EventWheel.CoarseBucketOf(stale), EventWheel.CoarseBucketOf(new Ticks(4 * (ulong)Ticks.PerDay)));

        world.RuleInstances.NextTick[slot] = stale;

        Assert.Equal(
            Invariant.ACoarseRowIsDueOnTheDayItsBucketNames,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world)).Violation.Invariant);
    }

    /// <summary>A coarse row is counted by the queue census rather than reported as on no queue.</summary>
    /// <remarks>
    /// <b>The failure this rules out is the one a new queue always has.</b>
    /// <see cref="Invariant.RuleInstanceIsArmedOrWaiting"/> counts every row it finds on a wait list
    /// or a bucket and reports any live row it saw a number of times other than once — so a tier the
    /// census does not walk turns every row on it into a violation.
    /// </remarks>
    [Fact]
    public void A_coarse_row_satisfies_the_armed_or_waiting_census()
    {
        (World world, Handle<Building> building) = Built();

        world.CreateRuleInstance(building, Sleep, Ticks.Zero, delay: 9 * Ticks.PerDay);

        world.Invariants.RunEndOfRun(world);
    }
}
