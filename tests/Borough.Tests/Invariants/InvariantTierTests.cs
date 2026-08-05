using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
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
    private static World Built()
    {
        var world = new World(1_000);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(lot, kind: 1);
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
        world.Invariants.RunEndOfRun(world, default);
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
        world.Invariants.RunEndOfRun(world, default);
    }

    [Fact]
    public void A_replayed_session_violates_nothing()
    {
        Simulation simulation = Replay.Start(GoldenFixtures.Session());
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

    [Fact]
    public void A_household_whose_home_is_gone_is_caught()
    {
        World world = Built();
        world.Buildings.Rows.Free(world.Buildings.Rows.At(0));

        Assert.Equal(Invariant.HouseholdHomeExists, Caught(world));
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

        for (ulong tick = 0; tick < (ulong)world.Invariants.Slices; tick++)
        {
            world.Invariants.RunStaggered(world, new Ticks(tick));
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
    [Fact]
    public void Money_that_has_run_away_is_caught()
    {
        World world = Built();
        Handle<Household> second = world.CreateHousehold(world.Buildings.Rows.At(0), lifeStage: 1);

        world.Households.Money[0] = new Money(long.MaxValue);
        world.Households.Savings[world.Households.Rows.Resolve(second)] = new Money(long.MaxValue);

        Assert.Equal(Invariant.MoneyIsRepresentable, CaughtAtEnd(world));
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
        Simulation simulation = Replay.Start(log);
        simulation.World.Invariants.Slices = slices;

        var hashes = new List<ulong>();
        Replay.Trace(simulation, log, new Ticks(128), hashEvery: 8, hashes);

        return [.. hashes];
    }

    /// <summary>Runs a whole staggered sweep, so a violation anywhere in the world is reached.</summary>
    private static void Sweep(World world)
    {
        for (ulong tick = 0; tick < (ulong)world.Invariants.Slices; tick++)
        {
            world.Invariants.RunStaggered(world, new Ticks(tick));
        }
    }

    private static Invariant Caught(World world) =>
        Assert.Throws<InvariantViolationException>(() => Sweep(world)).Violation.Invariant;

    private static Invariant CaughtAtEnd(World world) =>
        Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world, default)).Violation.Invariant;
}
