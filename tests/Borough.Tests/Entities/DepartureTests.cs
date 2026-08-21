using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 11 task 7: the unhoused Departure, and the first sink the Unplaced Pool has ever had.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is that a Pool with a door into it does not grow for ever</b>
/// (<see href="../../../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md">adr/0130</see>).
/// Until the gate opened, <c>adr/0006</c> was satisfied for the Pool by an <em>absence</em> — nothing
/// created a Household after world creation, so the Pool was a subset of a fixed population and could
/// not grow with elapsed time whatever it did. The gate removed that reason, and these tests are what
/// replaces it.
/// </para>
/// <para>
/// ⚠ <b>Only the unhoused channel exists.</b> <c>CONTEXT.md</c> gives Departure three, and they are
/// not three sizes of the same thing: the <b>housed</b> one is a comparison the Household re-runs
/// (<c>adr/0102</c>) and ships at 16 with the choice model; the <b>destitute</b> one needs
/// Unemployment and a floor. <see cref="Invariant.OnlyAnUnhousedHouseholdGivesUp"/> is what keeps the
/// other two from arriving through this door by accident.
/// </para>
/// <para>
/// <b>Placement is driven directly rather than through <c>Simulation.Step</c></b>, which is task 6's
/// lesson: <c>bordered.toml</c> paves the lattice to the map's boundary, so a Day of Ticks over its
/// half-million Segments costs a minute and measures the simulation rather than this mechanism.
/// </para>
/// </remarks>
public sealed class DepartureTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    /// <summary><c>bordered.toml</c> with its give-up duration rewritten to <paramref name="days"/>.</summary>
    /// <remarks>
    /// <b>Edited as text so the Ruleset under test is one the loader accepts.</b> The shipped value is
    /// 120 Days, which is four months and is <c>00-vision.md</c>'s own figure; a test that waited it
    /// out would be measuring <c>Ticks.PerDay</c>.
    /// </remarks>
    private static Ruleset Bordered(int days)
    {
        string text =
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "bordered.toml"));

        text = text.Replace(
            "gives_up_after_days = 120",
            $"gives_up_after_days = {days}",
            StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(text, $"bordered-{days}d.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the edited Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>A generated city with a door in it, everybody housed and the Pool empty.</summary>
    private static World City(int days, int citizens = 1_000)
    {
        var world = new World(citizens, Bordered(days));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>
    /// A city whose homes have been demolished: everybody in the Pool, and nowhere to put them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The obvious fixture does not exist, and finding that out is worth recording.</b> The
    /// first draft asserted that a generated city leaves people waiting — that a non-empty Pool
    /// <em>is</em> the statement that no dwelling has room. <c>SyntheticCity</c> houses everybody, so
    /// the Pool is empty at world creation and eleven tests failed on their fixture rather than on
    /// their assertion. ***A world with a housing shortage in it has to be built; no shipped world is
    /// one.***
    /// </para>
    /// <para>
    /// <b>Demolition is what builds it, and it is one call rather than a rig.</b>
    /// <c>World.DestroyBuilding</c> evicts its Occupants into the Pool with their balances intact
    /// (<c>adr/0054</c>), so flattening every dwelling puts the whole population into the Pool and
    /// leaves the Lots standing with nothing on them. Every candidate draw then lands on a vacant Lot
    /// and <c>TryHouse</c> cannot succeed however long the test runs — <b>by construction rather than
    /// by luck</b>, which is what a fixture for a bound has to be.
    /// </para>
    /// <para>
    /// <b>The gates are left standing</b>, because they are what an arrival needs and they house
    /// nobody anyway — <c>bordered.toml</c>'s gate kind declares no <c>occupants</c>.
    /// </para>
    /// </remarks>
    private static World Full(int days, int citizens = 1_000)
    {
        World world = City(days, citizens);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                world.DestroyBuilding(world.Buildings.Rows.At(slot), Ticks.Zero);
            }
        }

        Assert.True(
            world.UnplacedPool.Count > 0,
            "the fixture needs everybody looking for a home, and this one housed them.");

        return world;
    }

    private static PlacementEngine Placement(World world) =>
        new(world, Key, new TripEngine(world));

    /// <summary>Every standing Outside Connection, by Building slot.</summary>
    private static int Gate(World world)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                return slot;
            }
        }

        throw new InvalidOperationException("bordered.toml raised no gate.");
    }

    /// <summary>Runs placement passes over <paramref name="ticks"/>, starting at <paramref name="from"/>.</summary>
    private static PlacementActivity Run(World world, ulong from, ulong ticks)
    {
        PlacementEngine placement = Placement(world);

        for (ulong tick = from; tick < from + ticks; tick++)
        {
            placement.Place(new Ticks(tick));
        }

        return placement.Drain();
    }

    // ---- the bound -----------------------------------------------------------------------------

    /// <summary>
    /// A Household that has been looking longer than it will look gives up and leaves.
    /// </summary>
    /// <remarks>
    /// 🔴 ⚠ <b>The window is eight revisit periods and not one, and the reason is a property of
    /// placement rather than of the bound.</b> The first draft ran one period and failed:
    /// <c>PlacementEngine.DrawPool</c> draws <b>with replacement</b>, so a revisit period is the rate
    /// at which a member is looked at and not a guarantee that every member has been — about
    /// <c>1/e</c> of the Pool goes unlooked-at in any given period, and this arrival was in it.
    /// ***A revisit period says how often somebody is looked at on average, and never that everybody
    /// has been.*** The run is seeded, so eight periods is a deterministic outcome rather than a
    /// probability the test is gambling on.
    /// </remarks>
    [Fact]
    public void A_household_past_its_duration_gives_up_and_leaves()
    {
        World world = Full(days: 1);

        Assert.True(
            world.TryArrive(world.Buildings.Rows.At(Gate(world)), 0, 2, Ticks.Zero, out var arrival));

        // From one Day in, which is past the one-Day bound, so every occasion from here is past it.
        Run(world, from: Ticks.PerDay, ticks: 8192);

        Assert.False(
            world.Households.Rows.TryResolve(arrival, out _),
            "the Household was still here well after its duration expired.");
    }

    /// <summary>
    /// A Household still inside its duration stays, which is what makes the other test mean anything.
    /// </summary>
    /// <remarks>
    /// <b>A sink that fires unconditionally passes the first test and empties the city.</b> This is
    /// the half that says the bound is a bound rather than a delay.
    /// </remarks>
    [Fact]
    public void A_household_inside_its_duration_keeps_looking()
    {
        World world = Full(days: 8);

        Assert.True(
            world.TryArrive(world.Buildings.Rows.At(Gate(world)), 0, 2, Ticks.Zero, out var arrival));

        Run(world, from: 0, ticks: 2048);

        Assert.True(
            world.Households.Rows.TryResolve(arrival, out int slot),
            "the Household gave up a Day into an eight-Day search.");

        Assert.True(world.Households.IsUnplaced(slot));
    }

    /// <summary>
    /// The Pool drains rather than growing, which is <c>adr/0006</c> discharged by a mechanism.
    /// </summary>
    /// <remarks>
    /// <b>This is the milestone's Definition of done for the Pool.</b> The check is not that the Pool
    /// is empty — a city with a housing shortage has people waiting in it, and should — but that the
    /// number goes <em>down</em> when nothing is being housed. Before task 7 it could only go up.
    /// </remarks>
    [Fact]
    public void The_pool_drains_when_nothing_can_be_housed()
    {
        World world = Full(days: 1);
        int before = world.UnplacedPool.Count;

        Run(world, from: Ticks.PerDay, ticks: Ticks.PerDay * 2);

        Assert.True(
            world.UnplacedPool.Count < before,
            $"the Pool held {before} and still holds {world.UnplacedPool.Count} after two Days "
            + "in which nothing could be housed.");
    }

    /// <summary>A Departure is reported as a flow of its own, beside considered and placed.</summary>
    /// <remarks>
    /// <b><c>CONTEXT.md</c> → Departure: *"Departure rate is a distinct demand signal from Pool
    /// size"*</b> — a stock and a flow answer different questions, and a city can have a large Pool
    /// and be healthy or a small one and be in crisis.
    /// </remarks>
    [Fact]
    public void Departures_are_counted_as_their_own_flow()
    {
        World world = Full(days: 1);

        PlacementActivity activity = Run(world, from: Ticks.PerDay, ticks: Ticks.PerDay);

        Assert.True(activity.Departed.Sum > 0, "no Departure was counted.");
        Assert.Equal(0, activity.Placed.Sum);
    }

    // ---- the money -----------------------------------------------------------------------------

    /// <summary>
    /// The money leaves with them, and the supply goes down by exactly what left.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the answer to the question <c>World.DestroyHousehold</c> filed against its first
    /// production caller.</b> That method's remark said the balance was destroyed, that the omission
    /// was deliberate, and that the first production caller would have to decide. <c>World.Depart</c>
    /// is that caller, and the answer is the only one with a recipient — there is no escheat, no
    /// estate and no treasury claim, and inventing one to keep a total tidy would be a Policy decided
    /// by an invariant.
    /// </remarks>
    [Fact]
    public void A_departing_household_takes_its_money_with_it()
    {
        World world = Full(days: 1);

        Assert.True(
            world.TryArrive(world.Buildings.Rows.At(Gate(world)), 0, 2, Ticks.Zero, out var arrival));

        long carried = world.BalanceOf(arrival).Raw;
        long before = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        Assert.True(carried > 0, "the arrival carried nothing, so this test would pass vacuously.");

        world.Depart(arrival);

        Assert.Equal(before - carried, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);
    }

    /// <summary>Conservation holds across a Departure, with no flow term.</summary>
    /// <remarks>
    /// <b>The equality stays exact because both sides move in one call</b>, which is task 5's finding
    /// (F20) arriving from the other direction: <c>Issued</c> is declared net of anything that has
    /// left it, so money walking out of the city is a decrement rather than a term.
    /// </remarks>
    [Fact]
    public void Conservation_holds_across_a_departure()
    {
        World world = Full(days: 1);

        for (int i = 0; i < 5; i++)
        {
            Assert.True(
                world.TryArrive(world.Buildings.Rows.At(Gate(world)), 0, 2, Ticks.Zero, out _));
        }

        Run(world, from: Ticks.PerDay, ticks: Ticks.PerDay);

        world.Invariants.Collect = true;
        WorldInvariants.MoneyIsConserved(world, world.Invariants);

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.MoneyIsConserved);

        Assert.Equal(
            world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw, MoneyLedger.Of(world).Total);
    }

    // ---- the channel ---------------------------------------------------------------------------

    /// <summary>
    /// A housed Household is refused at this door, because that channel is a comparison.
    /// </summary>
    [Fact]
    public void A_housed_household_does_not_give_up()
    {
        World world = City(days: 1);

        int housed = -1;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot) && !world.Households.IsUnplaced(slot))
            {
                housed = slot;
                break;
            }
        }

        Assert.True(housed >= 0, "the fixture housed nobody, so there is nothing to refuse.");

        Handle<Household> resident = world.Households.Rows.At(housed);

        world.Invariants.Collect = true;
        world.Depart(resident);

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.OnlyAnUnhousedHouseholdGivesUp);

        Assert.True(
            world.Households.Rows.TryResolve(resident, out _),
            "a refused Departure destroyed the Household anyway.");
    }

    /// <summary>
    /// A Household that never came through a gate gives up too, which is most of the Pool.
    /// </summary>
    /// <remarks>
    /// 🔴 ⚠ <b>The sink is not only for arrivals, and reading it as one would leave a hole the same
    /// shape as the one it closes.</b> Three of the Pool's four entry routes have no gate — a
    /// Household the city generated itself, one evicted by a demolition, one that decided to move —
    /// and demolitions happen throughout a run. ***A sink that only drains what came in through the
    /// door leaves everything that was already inside.*** The channel's own wording is *entered the
    /// Pool, failed repeatedly, gave up*, and it says nothing about where they entered from.
    /// </remarks>
    [Fact]
    public void An_evicted_household_gives_up_too()
    {
        World world = Full(days: 1);

        // Everybody in this Pool was evicted by the demolition that built the fixture, so every
        // membership carries a default gate. That is the case under test rather than a setup step.
        Handle<Household> evicted = world.UnplacedPool.At(0);

        Assert.True(
            world.UnplacedPool.GateAt(0).Equals(default(Handle<Building>)),
            "an eviction is meant to have no gate, and this fixture gave it one.");

        Run(world, from: Ticks.PerDay, ticks: Ticks.PerDay);

        Assert.False(
            world.Households.Rows.TryResolve(evicted, out _),
            "a Household that entered the Pool without a gate never left it.");
    }

    // ---- the Evidence count --------------------------------------------------------------------

    /// <summary>
    /// The count records dwellings looked at, and a Pool member that is shown nothing records zero.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Zero in a city with no vacancies is the assertion.</b> <c>adr/0130</c>'s guiding concept
    /// is that <em>a bound that cannot trip in its own headline case is not a bound</em> — which is
    /// why the count is Evidence and the duration is what bounds. This test is the other half: the
    /// count is <em>allowed</em> to read zero, because that is the honest description of a Household
    /// nobody offered a home to.
    /// </remarks>
    [Fact]
    public void A_household_shown_nothing_considered_nothing()
    {
        World world = Full(days: 8);

        Assert.True(
            world.TryArrive(world.Buildings.Rows.At(Gate(world)), 0, 2, Ticks.Zero, out var arrival));

        Run(world, from: 0, ticks: Ticks.PerDay);

        int position = world.Households.PoolPosition(world.Households.Rows.Resolve(arrival));

        Assert.Equal(0, world.UnplacedPool.Considered[position]);
    }

    // ---- the spell -----------------------------------------------------------------------------

    /// <summary>
    /// The clock is per spell in the Pool, so a Household that is re-housed and evicted starts again.
    /// </summary>
    /// <remarks>
    /// <b>It follows from the column being on the membership rather than on the Household</b>, which
    /// is <see cref="UnplacedTable.Gate"/>'s argument reused: a family looking again is not a family
    /// still looking, and a lifetime column would make the second eviction inherit the first one's
    /// waiting.
    /// </remarks>
    [Fact]
    public void A_second_spell_starts_its_own_clock()
    {
        World world = City(days: 8);

        int housed = -1;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot) && !world.Households.IsUnplaced(slot))
            {
                housed = slot;
                break;
            }
        }

        Assert.True(housed >= 0);

        // The Tick loop's setter is internal, and Unplace reads World.Tick rather than taking
        // one -- so the clock is moved where the clock lives.
        world.Clock.Tick[0] = new Ticks(Ticks.PerDay * 40);
        world.Unplace(world.Households.Rows.At(housed));

        int position = world.Households.PoolPosition(housed);

        Assert.Equal(Ticks.PerDay * 40, world.UnplacedPool.Since[position]);
    }

    /// <summary>
    /// A member swapped into another's position brings its own clock and count with it.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The regression guard for the shape that has now bitten this table twice.</b>
    /// <see cref="UnplacedTable.Leave"/> keeps the Pool dense by moving the last member into the
    /// vacated slot, so every column has to move together — and a column left behind gives the moved
    /// Household somebody else's history. The gate case is the loud one; <see cref="UnplacedTable.Since"/>
    /// is the dangerous one, because inheriting an older spell's clock makes a Household give up for
    /// somebody else's waiting and nothing about the result looks wrong.
    /// </remarks>
    [Fact]
    public void A_swapped_member_keeps_its_own_clock_and_count()
    {
        World world = City(days: 8);

        Assert.True(
            world.TryArrive(world.Buildings.Rows.At(Gate(world)), 0, 1, Ticks.Zero, out var first));
        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(Gate(world)), 0, 1, new Ticks(777), out var last));

        int lastPosition = world.Households.PoolPosition(world.Households.Rows.Resolve(last));

        Assert.Equal(world.UnplacedPool.Count - 1, lastPosition);

        world.UnplacedPool.Considered[lastPosition] = 9;

        // Remove somebody ahead of it, which is what pulls the last member down into the hole.
        world.UnplacedPool.Leave(world.Households, 0);

        int moved = world.Households.PoolPosition(world.Households.Rows.Resolve(last));

        Assert.Equal(777, world.UnplacedPool.Since[moved]);
        Assert.Equal(9, world.UnplacedPool.Considered[moved]);
        Assert.True(world.Households.Rows.TryResolve(first, out _));
    }
}
