using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 25 task 5: a Business that loses its premises waits, and then leaves the city.
/// </summary>
/// <remarks>
/// <para>
/// <b><see href="../../../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md">adr/0142</see>
/// closing a hole <c>adr/0006</c> forbids.</b> Before this task <c>World.DestroyBuilding</c> unlisted
/// its Businesses and freed nothing, leaving live rows holding a severed premises handle and a balance
/// the money supply still counted as issued. ***It was unreachable only because nothing creates a
/// Business***, which is a property of the calendar rather than of the code.
/// </para>
/// <para>
/// 🔴 ⚠ <b>EVERY TEST HERE IS A FIXTURE AND THAT IS THE POINT</b> (<c>plans/0040</c> <b>F7</b>).
/// <c>World.CreateBusiness</c> has no <c>src/</c> caller and milestone <b>27 task 8</b> is the first
/// pass that would, so nothing in a generated city reaches any of this. ***A sink built for a
/// collection that cannot yet fill is what adr/0142's own rule requires***: the bound goes in on the
/// day the collection does.
/// </para>
/// <para>
/// ⚠ <b>The pool ships with ONE exit and it is the sink.</b> Nothing tenants a Business, so no test
/// here places one — and a test asserting that a pooled Business is never re-premised would be
/// asserting the schedule rather than the build.
/// </para>
/// </remarks>
public sealed class UnpremisedPoolTests
{
    private const byte House = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0005UL);

    /// <summary>
    /// A Ruleset that names money and gives up after <paramref name="days"/>.
    /// </summary>
    /// <remarks>
    /// <b>Hand-built rather than a shipped file, because the loader would refuse this combination.</b>
    /// <c>adr/0130</c> requires <c>gives_up_after_days</c> of any Ruleset declaring a gate kind and
    /// refuses it elsewhere, so no shipped file states a bound without also carrying a gate, a
    /// hinterland and a paved lattice. ⚠ <b>The refusal is the loader's and the engine is what is
    /// under test</b> — constructing the record directly is testing the mechanism rather than
    /// evading the rule, and <c>BinTenancyLoadTests</c> is where the rule itself is checked.
    /// </remarks>
    private static Ruleset Giving(int days) =>
        new(
            resources: [ResourceFamily.Money],
            rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0) { Tenanted = 1 > 0 }],
            inputs: [], outputs: [], emissions: [], bins: [], kindRules: [], zoneRules: [])
        {
            Placement = new PlacementRuleset(
                Interval: 4, RevisitTicks: 128, Candidates: 8, GivesUpAfterDays: days),
        };

    private static (World World, Handle<Business> Business, Handle<Building> Premises) Shop(
        int days, long money)
    {
        var world = new World(1_000, Giving(days));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> premises = world.CreateBuilding(lot, House, Ticks.Zero, Key);

        Handle<Business> business = world.CreateBusiness(premises);

        if (money > 0)
        {
            // Through the treasury rather than by writing the Bin, so the money supply agrees with
            // the balance and Invariant.MoneyIsConserved has something real to check.
            Handle<Household> owner = world.CreateHousehold(premises, lifeStage: 1);

            world.Endow(owner, new Money(money));
            world.Withdraw(
                world.Households.Balance[world.Households.Rows.Resolve(owner)], money, world.Tick);
            world.Deposit(
                world.Businesses.Balance[world.Businesses.Rows.Resolve(business)], money, world.Tick);
        }

        return (world, business, premises);
    }

    /// <summary>
    /// Demolition puts the Business in the pool, with its premises severed and its money intact.
    /// </summary>
    /// <remarks>
    /// <b>The entry route, and the one the old code got half right.</b> Unlisting was always correct;
    /// what was missing was somewhere for the row to go. ⚠ <b>The balance is asserted unchanged
    /// because <c>adr/0142</c> says a pooled tenant keeps what it owns and that needs no code</b> —
    /// <c>Unpremise</c> touches the premises handle and the membership and nothing else, so a
    /// regression here would be somebody adding a line rather than forgetting one.
    /// </remarks>
    [Fact]
    public void A_demolition_puts_a_business_in_the_pool_with_its_money()
    {
        (World world, Handle<Business> business, Handle<Building> premises) = Shop(days: 4, money: 600);

        world.DestroyBuilding(premises, Ticks.Zero);

        int slot = world.Businesses.Rows.Resolve(business);

        Assert.True(world.Businesses.Rows.IsLive(slot));
        Assert.True(world.Businesses.IsUnpremised(slot));
        Assert.Equal(1, world.UnpremisedPool.Count);
        Assert.Equal(business, world.UnpremisedPool.At(0));
        Assert.False(world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out _));
        Assert.Equal(new Money(600), world.BalanceOf(business));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Past the bound the Business emigrates, and its money leaves the world rather than vanishing.
    /// </summary>
    /// <remarks>
    /// <b>The sink, and the assertion that matters is the money supply rather than the row.</b>
    /// Freeing the row alone would be a leak <c>Invariant.MoneyIsConserved</c> exists to catch;
    /// ***the money is neither destroyed nor confiscated, it is exported***, through the same door
    /// <c>World.Endow</c> brings an arriving Household's balance in by.
    /// </remarks>
    [Fact]
    public void Past_the_bound_the_business_leaves_and_takes_its_money_out_of_the_city()
    {
        (World world, Handle<Business> business, Handle<Building> premises) = Shop(days: 1, money: 600);

        var simulation = new Simulation(world, Key);

        world.DestroyBuilding(premises, Ticks.Zero);

        int slot = world.Businesses.Rows.Resolve(business);
        Money issued = world.MoneySupply.Issued[MoneySupplyTable.Slot];

        // A day and a bit, so the bound is genuinely crossed rather than landed on.
        for (int i = 0; i < Ticks.PerDay + Ticks.PerDay / 4; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.False(world.Businesses.Rows.IsLive(slot));
        Assert.Equal(0, world.UnpremisedPool.Count);
        Assert.Equal(issued - new Money(600), world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Inside the bound it is still there, which is what makes the previous test about the bound.
    /// </summary>
    /// <remarks>
    /// <b>The control, and without it <see cref="Past_the_bound_the_business_leaves_and_takes_its_money_out_of_the_city"/>
    /// passes for a build that retires everything on sight.</b> ***A sink with no holding period is
    /// not a pool***, and the difference between the two tests is only the Ruleset's number.
    /// </remarks>
    [Fact]
    public void Inside_the_bound_the_business_is_still_waiting()
    {
        (World world, Handle<Business> business, Handle<Building> premises) =
            Shop(days: 64, money: 600);

        var simulation = new Simulation(world, Key);

        world.DestroyBuilding(premises, Ticks.Zero);

        int slot = world.Businesses.Rows.Resolve(business);

        for (int i = 0; i < Ticks.PerDay * 2; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.True(world.Businesses.Rows.IsLive(slot));
        Assert.Equal(1, world.UnpremisedPool.Count);
        Assert.Equal(new Money(600), world.BalanceOf(business));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A Ruleset with no bound retires nobody, and the pool is a holding pen for the rest of the run.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0130</c>'s <em>absent means nobody ever gives up</em>, reaching a second
    /// collection.</b> ⚠ <b>It is coherent here for that ADR's own reason</b>: with nothing creating a
    /// Business the pool has no inflow, and a pool with no inflow needs no sink — so this is
    /// <c>adr/0006</c> satisfied by the same absence rather than in spite of it.
    /// </remarks>
    [Fact]
    public void With_no_bound_nobody_is_retired()
    {
        (World world, Handle<Business> business, Handle<Building> premises) = Shop(days: 0, money: 0);

        var simulation = new Simulation(world, Key);

        world.DestroyBuilding(premises, Ticks.Zero);

        for (int i = 0; i < Ticks.PerDay * 4; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.True(world.Businesses.Rows.IsLive(world.Businesses.Rows.Resolve(business)));
        Assert.Equal(1, world.UnpremisedPool.Count);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Departing a Business that still has premises is refused and reported.
    /// </summary>
    /// <remarks>
    /// <b><c>Invariant.OnlyAnUnhousedHouseholdGivesUp</c>'s rule with a different subject.</b> A
    /// premised Business choosing to leave is the housed-departure channel — a comparison rather than
    /// a threshold (<c>adr/0102</c>), and <b>unbuilt</b> — so reaching this door with premises means
    /// somebody wired the wrong channel to it. ⚠ <b>It reports and leaves the row alone</b>, which is
    /// <c>02 §10</c>'s rule: the conservative outcome, and the run continues.
    /// </remarks>
    [Fact]
    public void A_premised_business_cannot_be_departed()
    {
        (World world, Handle<Business> business, _) = Shop(days: 4, money: 600);

        world.Invariants.Collect = true;
        world.Depart(business);

        Assert.True(world.Businesses.Rows.IsLive(world.Businesses.Rows.Resolve(business)));
        Assert.Equal(0, world.UnpremisedPool.Count);
        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.ABusinessIsPremisedOrItIsInThePool);
    }

    /// <summary>
    /// The pool's membership and its clock survive a rebuild, so a reload retires the same Business.
    /// </summary>
    /// <remarks>
    /// <b>The reason the pool is a saved TABLE and the reverse index is derived</b>
    /// (<c>UnplacedTable</c>'s own argument, one collection across): a member is chosen by
    /// <em>position</em>, so a pool rebuilt in slot order would retire a different Business from the
    /// same save. ⚠ <b>And <c>Since</c> must travel</b> — a clock that restarted at the load would
    /// give every waiting Business its patience back, for free, on every reload.
    /// </remarks>
    [Fact]
    public void The_pool_and_its_clock_survive_a_rebuild()
    {
        var world = new World(1_000, Giving(days: 8));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> premises = world.CreateBuilding(lot, House, Ticks.Zero, Key);

        Handle<Business> first = world.CreateBusiness(premises);
        Handle<Business> second = world.CreateBusiness(premises);

        world.DestroyBuilding(premises, new Ticks(96));

        int[] before =
        [
            world.Businesses.PoolPosition(world.Businesses.Rows.Resolve(first)),
            world.Businesses.PoolPosition(world.Businesses.Rows.Resolve(second)),
        ];

        Assert.Equal(96, world.UnpremisedPool.Since[0]);

        world.RebuildDerived();

        Assert.Equal<int[]>(
            before,
            [
                world.Businesses.PoolPosition(world.Businesses.Rows.Resolve(first)),
                world.Businesses.PoolPosition(world.Businesses.Rows.Resolve(second)),
            ]);

        Assert.Equal(96, world.UnpremisedPool.Since[0]);

        world.Invariants.RunEndOfRun(world);
    }
}
