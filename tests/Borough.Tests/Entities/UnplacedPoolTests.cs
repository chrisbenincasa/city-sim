using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Entities;

/// <summary>
/// Slice 10 task 6: the minimal Unplaced Pool, and the two verbs that cross its boundary.
/// </summary>
/// <remarks>
/// <b>Most of what is asserted here is density and the reverse index</b>, which are the two
/// properties that fail silently. A Pool with a hole in it still has the right members and the right
/// count; what it loses is that a draw over the count names a live row, so the city simply builds
/// less than its Ruleset says. Nothing anywhere reports that.
/// </remarks>
public sealed class UnplacedPoolTests
{
    /// <summary>
    /// One kind, with room for two Households where these fixtures put one.
    /// </summary>
    /// <remarks>
    /// <b>A Ruleset where this used to be <see cref="Ruleset.Empty"/>, and the change is
    /// <c>adr/0068</c> made visible.</b> Occupancy is a property of the Ruleset in force, so a world
    /// running no Ruleset declares no kind, every Building in it is derelict, and nobody can move
    /// into one — <see cref="World.Place"/> now refuses, where before it took anybody anywhere.
    /// Declaring the kind is the only way to ask for a Building that houses somebody, which is the
    /// same shape <c>adr/0064</c> imposed on <c>BinTests</c>: a fixture that can no longer be written
    /// is the design claim enforcing itself.
    /// <para>
    /// <b>Two rather than one, because these tests re-house into an occupied Building on purpose.</b>
    /// Every fixture here builds one Household per Building and then moves one somewhere that already
    /// has a tenant, which is what makes the reverse index and the density claims worth asserting —
    /// so the spare place is the fixture stating that it is testing the Pool and not the ceiling.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b>It names money because <c>An_evicted_household_keeps_its_balance</c> endows somebody</b>, and
    /// since <c>adr/0114</c> a balance is a Bin — so a Household in a world whose Ruleset declares no
    /// money has nowhere to hold any and <c>World.Endow</c> refuses.
    /// </remarks>
    private static Ruleset Housing(int occupants) => new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0) { Houses = occupants > 0 , Premises = occupants > 0 }],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: [])
    {
        // ONE TILE PER TENANCY, so a Lot's width is its Building's ceiling (plans/0053). The count a
        // kind used to declare is derived from the ground now, and holding the rate at one is what
        // lets `occupants` below still read as the number of families it means.
        Capacity = new CapacityRuleset(1, 0, 0),
    };

    private static World Built(int households = 8, int occupants = 2)
    {
        var world = new World(1_000, Housing(occupants));

        for (int i = 0; i < households; i++)
        {
            Handle<Lot> lot = world.Lots.Create(
                new Tiles(i), new Tiles(0), zone: 1, wide: new Tiles(occupants), deep: new Tiles(1));
            Handle<Building> building = world.CreateBuilding(lot, kind: 1, Ticks.Zero, default);

            world.CreateHousehold(building, lifeStage: 0);
        }

        return world;
    }

    private static Handle<Household> Household(World world, int index) =>
        world.Households.Rows.At(index);

    private static Handle<Household>[] Members(World world) =>
        [.. Enumerable.Range(0, world.UnplacedPool.Count).Select(world.UnplacedPool.At)];

    /// <summary>Whether a walk visits <paramref name="node"/>.</summary>
    private static bool Lists(IndexListWalk walk, int node)
    {
        foreach (int element in walk)
        {
            if (element == node)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Every live Pool slot is below the count, after any amount of churn.</summary>
    private static void AssertDense(World world)
    {
        UnplacedTable pool = world.UnplacedPool;

        for (int slot = 0; slot < pool.Rows.SlotCount; slot++)
        {
            Assert.Equal(slot < pool.Count, pool.Rows.IsLive(slot));
        }
    }

    /// <summary>Every member's reverse index points back at the position holding it.</summary>
    private static void AssertAgrees(World world)
    {
        UnplacedTable pool = world.UnplacedPool;

        for (int position = 0; position < pool.Count; position++)
        {
            int slot = world.Households.Rows.Resolve(pool.At(position));

            Assert.True(world.Households.IsUnplaced(slot));
            Assert.Equal(position, world.Households.PoolPosition(slot));
        }
    }

    // ---- the boundary ---------------------------------------------------------------------------

    [Fact]
    public void A_new_world_has_an_empty_pool()
    {
        Assert.Equal(0, Built().UnplacedPool.Count);
    }

    [Fact]
    public void Unplacing_moves_a_household_out_of_its_building()
    {
        World world = Built();
        Handle<Household> household = Household(world, 0);
        int slot = world.Households.Rows.Resolve(household);
        int building = world.Buildings.Rows.Resolve(world.Households.Dwelling[slot]);

        world.Unplace(household);

        Assert.Equal(1, world.UnplacedPool.Count);
        Assert.True(world.Households.IsUnplaced(slot));
        Assert.False(world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out _));
        Assert.False(Lists(world.Occupants.Walk(building), slot));
    }

    /// <summary>
    /// <c>adr/0054</c>'s cheapest claim: a Household keeps what it owns when the city stops housing it.
    /// </summary>
    /// <remarks>
    /// <b>Worth a test precisely because it required no code.</b> Eviction does not write to the
    /// balance handle, so the property holds by omission — and a property that holds by omission is one
    /// a later edit can remove without noticing. <c>adr/0024</c> makes it a leak in
    /// conserved Money, which the end-of-run walk would report far from the cause.
    /// </remarks>
    [Fact]
    public void An_evicted_household_keeps_its_balance()
    {
        World world = Built();
        Handle<Household> household = Household(world, 0);

        world.Endow(household, new Money(6_912));

        world.Unplace(household);

        Assert.Equal(new Money(6_912), world.BalanceOf(household));
    }

    [Fact]
    public void Placing_houses_the_household_and_lists_it_as_an_occupant()
    {
        World world = Built();
        Handle<Household> evicted = Household(world, 0);
        int slot = world.Households.Rows.Resolve(evicted);

        world.Unplace(evicted);

        Handle<Building> elsewhere = world.Households.Dwelling[world.Households.Rows.Resolve(
            Household(world, 3))];

        world.Place(evicted, elsewhere);

        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.False(world.Households.IsUnplaced(slot));
        Assert.Equal(elsewhere, world.Households.Dwelling[slot]);
        Assert.True(Lists(world.Occupants.Walk(world.Buildings.Rows.Resolve(elsewhere)), slot));
    }

    /// <summary>
    /// Unplacing a Household that is already in the Pool is refused at the write site.
    /// </summary>
    /// <remarks>
    /// A second membership row would make the draw favour that Household in proportion to how many
    /// times it happened, which is indistinguishable from luck in every readout the game has.
    /// </remarks>
    [Fact]
    public void A_household_cannot_join_the_pool_twice()
    {
        World world = Built();
        Handle<Household> household = Household(world, 0);

        world.Unplace(household);

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.Unplace(household));

        Assert.Equal(Invariant.OnlyAHousedHouseholdIsUnplaced, failure.Violation.Invariant);
    }

    /// <summary>Housing a Household that is not in the Pool is refused at the write site.</summary>
    /// <remarks>
    /// The mirror of the check above, and it closes the other direction of the same boundary: this
    /// would otherwise move somebody out of a dwelling they still occupy and take an unrelated
    /// membership row with them.
    /// </remarks>
    [Fact]
    public void A_household_that_is_not_in_the_pool_cannot_be_placed()
    {
        World world = Built();
        Handle<Building> home = world.Households.Dwelling[world.Households.Rows.Resolve(
            Household(world, 7))];

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.Place(Household(world, 0), home));

        Assert.Equal(Invariant.OnlyAPooledHouseholdIsPlaced, failure.Violation.Invariant);
    }

    /// <summary>
    /// A Pool row freed anywhere but the end is caught on the next eviction, not at the end of the run.
    /// </summary>
    /// <remarks>
    /// <b>This writes the violation that the allocator changing would produce.</b> Density is inherited
    /// from <c>Rows</c>'s free list being LIFO, which nothing in <c>Rows</c> promises — so the failure
    /// mode being guarded is somebody else's edit, not this table's. Punching the hole by hand is the
    /// only way to reach it, and what it proves is that the write-site check fires on the very next
    /// <c>Join</c> rather than leaving the city quietly under-building for a whole run.
    /// </remarks>
    [Fact]
    public void A_hole_in_the_pool_is_caught_on_the_next_eviction()
    {
        World world = Built();

        for (int i = 0; i < 3; i++)
        {
            world.Unplace(Household(world, i));
        }

        // Straight at the table, behind Leave's back: the free list now offers a slot in the middle.
        world.UnplacedPool.Rows.Free(world.UnplacedPool.Rows.At(1));

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.Unplace(Household(world, 4)));

        Assert.Equal(Invariant.ThePoolAppendsInOrder, failure.Violation.Invariant);
    }

    // ---- density --------------------------------------------------------------------------------

    /// <summary>
    /// Leaving from the middle keeps the live rows contiguous, which is what the draw depends on.
    /// </summary>
    [Fact]
    public void Leaving_from_the_middle_keeps_the_pool_dense()
    {
        World world = Built();

        for (int i = 0; i < 6; i++)
        {
            world.Unplace(Household(world, i));
        }

        Handle<Building> home = world.Households.Dwelling[world.Households.Rows.Resolve(
            Household(world, 7))];

        Assert.Equal(Household(world, 2), world.UnplacedPool.At(2));
        world.Place(world.UnplacedPool.At(2), home);

        Assert.Equal(5, world.UnplacedPool.Count);

        AssertDense(world);
        AssertAgrees(world);
    }

    /// <summary>The degenerate case: the member leaving is also the one that would be moved.</summary>
    [Fact]
    public void Leaving_from_the_last_position_is_not_a_self_move()
    {
        World world = Built();

        world.Unplace(Household(world, 0));
        world.Unplace(Household(world, 1));

        Handle<Building> home = world.Households.Dwelling[world.Households.Rows.Resolve(
            Household(world, 7))];

        Assert.Equal(Household(world, 1), world.UnplacedPool.At(1));
        world.Place(world.UnplacedPool.At(1), home);

        Assert.Equal(1, world.UnplacedPool.Count);
        Assert.False(world.Households.IsUnplaced(world.Households.Rows.Resolve(Household(world, 1))));

        AssertDense(world);
        AssertAgrees(world);
    }

    /// <summary>
    /// Sustained churn never grows the table past its high-water mark, and never holes it.
    /// </summary>
    /// <remarks>
    /// <b>The slot count is the assertion that matters</b>, and it is task 10's in miniature: a table
    /// that grew with the number of cycles rather than with the size of the Pool would be
    /// <c>adr/0006</c> in the one structure built to churn.
    /// </remarks>
    [Fact]
    public void Churn_reuses_slots_rather_than_growing_the_table()
    {
        // Room for nine, because this test re-houses the whole Pool into one Building every round.
        // What it is watching is the Pool's slot recycling, so the ceiling is fixture scaffolding
        // rather than the subject -- but it has to be stated now, which is adr/0068 working.
        World world = Built(64, occupants: 9);
        Handle<Building> home = world.Households.Dwelling[world.Households.Rows.Resolve(
            Household(world, 63))];

        for (int round = 0; round < 40; round++)
        {
            for (int i = 0; i < 8; i++)
            {
                world.Unplace(Household(world, i));
            }

            AssertDense(world);

            while (world.UnplacedPool.Count > 0)
            {
                world.Place(world.UnplacedPool.At(world.UnplacedPool.Count / 2), home);
                AssertDense(world);
            }
        }

        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.Equal(8, world.UnplacedPool.Rows.SlotCount);
    }

    // ---- save and reload ------------------------------------------------------------------------

    /// <summary>
    /// The reverse index is rebuilt from the Pool, positions included.
    /// </summary>
    /// <remarks>
    /// <b>This is the reason the Pool is a saved table rather than a derived list.</b> The membership
    /// and its order come back verbatim, so the same save rehouses the same Household — a derived
    /// list rebuilt in slot order would not, and the divergence would surface as a save/reload hash
    /// mismatch a long way from its cause.
    /// </remarks>
    [Fact]
    public void The_reverse_index_survives_a_rebuild()
    {
        World world = Built();

        for (int i = 0; i < 5; i++)
        {
            world.Unplace(Household(world, i));
        }

        Handle<Building> home = world.Households.Dwelling[world.Households.Rows.Resolve(
            Household(world, 7))];

        world.Place(world.UnplacedPool.At(1), home);

        Handle<Household>[] before = Members(world);

        world.RebuildDerived();

        Assert.Equal<Handle<Household>>(before, Members(world));

        AssertAgrees(world);
    }
}
