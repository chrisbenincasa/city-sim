using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 10 task 6: <b>vacant AND permitted AND somebody in the Pool would take it</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each of the three terms gets a test that removes only that term</b>, because a predicate whose
/// terms are never varied independently is a predicate that may as well be one term. Two of the three
/// failure modes here — building where the player forbade it, and building for nobody — produce a
/// city that looks plausible and is wrong.
/// </para>
/// <para>
/// <b>Getting a Household into the Pool takes an eviction, and eviction is task 7's verb.</b> These
/// tests house a Household and then unplace it by hand, which is the honest way to test one half of a
/// cycle whose other half does not exist yet. It is also the finding: <em>neither task 6 nor task 7
/// can run in a real world alone</em> — creation needs a vacant Lot and a Household with no home, and
/// demolition is the only thing that produces either.
/// </para>
/// </remarks>
public sealed class ZoneRuleCreateTests
{
    private const byte House = 1;
    private const byte HousingBit = 0;

    /// <summary>The permission set a Lot needs to admit <see cref="House"/>.</summary>
    private const ushort Housing = 1 << HousingBit;

    private static Ruleset Zoned(params ZoneRuleDefinition[] zoneRules) => new(
        resources: [],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: zoneRules);

    /// <summary>
    /// A world with <paramref name="vacant"/> empty Lots and <paramref name="seeking"/> Households
    /// in the Pool.
    /// </summary>
    /// <remarks>
    /// The seeking Households come from Buildings that stay standing, so the only Lots a Zone Rule
    /// can build on are the deliberately empty ones — which is what makes <c>Created</c> countable
    /// against them.
    /// </remarks>
    private static (World World, Simulation Simulation) Built(
        Ruleset ruleset, int vacant = 32, int seeking = 4, ushort zone = Housing)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0001UL));

        for (int i = 0; i < seeking; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), zone);
            Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

            world.Unplace(world.CreateHousehold(building, lifeStage: 0));
        }

        for (int i = 0; i < vacant; i++)
        {
            world.Lots.Create(new Tiles(i), new Tiles(1), zone);
        }

        return (world, simulation);
    }

    private static ZoneActivity Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        return simulation.Zoning.Drain();
    }

    private static ZoneRuleDefinition Rule(int sample = 8, uint interval = 4) =>
        new(House, HousingBit, interval, sample);

    // ---- the three terms, removed one at a time --------------------------------------------------

    [Fact]
    public void A_vacant_permitted_lot_with_somebody_waiting_gets_a_building()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()));

        ZoneActivity activity = Run(simulation, 64);

        Assert.True(activity.Created.Sum > 0);
        Assert.Equal(4 + activity.Created.Sum, world.Buildings.Rows.LiveCount);
    }

    /// <summary>
    /// A Lot admitting nothing is never built on, however often it is sampled.
    /// </summary>
    /// <remarks>
    /// <b>And it is still sampled, which is <c>adr/0055</c>.</b> The permission bit scopes what may be
    /// built, never the population drawn from — so the wasted sample here is the model working. A Rule
    /// that filtered its population instead would make an empty permission set a preservation order.
    /// </remarks>
    [Fact]
    public void An_unpermitted_lot_is_sampled_and_never_built_on()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()), zone: 0);

        ZoneActivity activity = Run(simulation, 64);

        Assert.Equal(0, activity.Created.Sum);
        Assert.True(activity.Vacant.Sum > 0);
        Assert.Equal(4, world.Buildings.Rows.LiveCount);
    }

    /// <summary>
    /// A Rule whose bit no Lot carries builds nothing, even where another Rule's bit is set.
    /// </summary>
    [Fact]
    public void A_rule_whose_bit_is_unpainted_builds_nothing()
    {
        (World world, Simulation simulation) = Built(
            Zoned(new ZoneRuleDefinition(House, 7, 4, 8)));

        Assert.Equal(0, Run(simulation, 64).Created.Sum);
        Assert.Equal(4, world.Buildings.Rows.LiveCount);
    }

    /// <summary>
    /// With nobody in the Pool nothing is built, which is <c>CONTEXT</c> → Frontage's fourth reason.
    /// </summary>
    /// <remarks>
    /// <b>This is the term that stops the city building for nobody</b>, and it is the only demand
    /// signal in the design — there is no RCI meter and the Pool is not a scalar anything can set.
    /// </remarks>
    [Fact]
    public void An_empty_pool_builds_nothing()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()), seeking: 0);

        Assert.Equal(0, Run(simulation, 64).Created.Sum);
        Assert.Equal(0, world.Buildings.Rows.LiveCount);
    }

    // ---- what a create does ----------------------------------------------------------------------

    /// <summary>
    /// Every Building built houses exactly one Household out of the Pool.
    /// </summary>
    /// <remarks>
    /// <b>The conservation statement, and it is what makes growth self-limiting.</b> Creation drains
    /// the signal that authorised it, so a Ruleset cannot build past its demand however wide its
    /// sample — which is the property that would otherwise need a cap somebody chose.
    /// </remarks>
    [Fact]
    public void Creating_drains_the_pool_by_exactly_one_each_time()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()), seeking: 6);

        ZoneActivity activity = Run(simulation, 256);

        Assert.Equal(6, activity.Created.Sum);
        Assert.Equal(0, world.UnplacedPool.Count);

        // Six seed Buildings, whose Households were evicted into the Pool, plus the six built to
        // rehouse them. The Pool is what conserves the two numbers against each other.
        Assert.Equal(12, world.Buildings.Rows.LiveCount);
    }

    /// <summary>Growth stops when demand is met, and stays stopped.</summary>
    [Fact]
    public void Growth_stops_when_the_pool_empties()
    {
        (_, Simulation simulation) = Built(Zoned(Rule()), seeking: 3);

        Assert.Equal(3, Run(simulation, 512).Created.Sum);
        Assert.Equal(0, Run(simulation, 512).Created.Sum);
    }

    /// <summary>
    /// Every Household taken from the Pool ends up living in the Building that took it.
    /// </summary>
    [Fact]
    public void Everybody_housed_by_growth_lives_where_they_were_placed()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()), seeking: 5);

        Run(simulation, 256);

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.False(world.Households.IsUnplaced(slot));
            Assert.True(world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out _));
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A Building is built on the Lot that was sampled, and no Lot ever takes a second.
    /// </summary>
    /// <remarks>
    /// <c>02 §2.2</c>'s claim, reached through the growth path rather than by a test writing the
    /// relation by hand. The write-site invariant would throw before the end-of-run walk saw it.
    /// </remarks>
    [Fact]
    public void Growth_never_puts_two_buildings_on_one_lot()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()), vacant: 8, seeking: 8);

        Run(simulation, 512);

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && !world.Lots.IsVacant(slot))
            {
                Assert.True(world.Buildings.Rows.IsLive(world.Lots.BuildingOn(slot)));
            }
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// Two Zone Rules on one Tick do not both build on the Lot they both sampled.
    /// </summary>
    /// <remarks>
    /// Contention is settled by declaration order and nothing else — the first Rule to act takes the
    /// Lot, and the second finds it occupied. There is no bid, because <c>02 §5.5</c>'s contest needs
    /// prices this build does not have.
    /// </remarks>
    [Fact]
    public void Two_rules_contending_for_one_lot_settle_by_declaration_order()
    {
        (World world, Simulation simulation) = Built(
            Zoned(Rule(sample: 24), Rule(sample: 24)), vacant: 4, seeking: 8);

        ZoneActivity activity = Run(simulation, 128);

        // Four vacant Lots and two Rules sampling widely enough to reach all of them on every
        // trigger. Exactly four Buildings appear, so the second Rule found what the first had taken.
        Assert.Equal(4, activity.Created.Sum);
        Assert.Equal(12, world.Buildings.Rows.LiveCount);
        Assert.Equal(4, world.UnplacedPool.Count);

        world.Invariants.RunEndOfRun(world);
    }

    // ---- determinism ------------------------------------------------------------------------------

    /// <summary>Two runs of one world and one key grow identically.</summary>
    [Fact]
    public void Growth_is_reproducible()
    {
        (World first, Simulation firstRun) = Built(Zoned(Rule()), seeking: 6);
        (World second, Simulation secondRun) = Built(Zoned(Rule()), seeking: 6);

        Assert.Equal(Run(firstRun, 256), Run(secondRun, 256));
        Assert.Equal(first.HashState(), second.HashState());
    }

    /// <summary>
    /// Who moves in is drawn rather than taken from the front of the queue.
    /// </summary>
    /// <remarks>
    /// <b><c>02 §8</c> rule 5's reason, reaching a case its wording does not cover.</b> Nothing is
    /// contested here — any member would take the house — but a Pool that never fully drains is what
    /// a housing shortage <em>is</em>, and under any fixed order the same Households would stay
    /// unhoused for the life of the city with nothing to explain why. Housing strictly more than the
    /// first-arrived would-be tenants is what this asserts.
    /// </remarks>
    [Fact]
    public void Who_moves_in_is_drawn_rather_than_queued()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule(sample: 2)), vacant: 2, seeking: 8);
        Handle<Household> first = world.UnplacedPool.At(0);

        Run(simulation, 64);

        Assert.Equal(6, world.UnplacedPool.Count);

        // The two houses went to somebody, and a strict queue would have started with this one.
        bool housed = !world.Households.IsUnplaced(world.Households.Rows.Resolve(first));

        Assert.False(housed);
    }
}
