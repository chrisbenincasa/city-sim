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

    /// <summary>
    /// A Ruleset with Zone Rules and <b>no placement pass</b>, which is what most of this file wants.
    /// </summary>
    /// <remarks>
    /// <b>Since <c>adr/0069</c> the two halves of the growth loop are separable, and separating them
    /// is what these tests are for.</b> Construction houses nobody, so with no <c>[placement]</c> the
    /// Pool is a demand signal that creation reads and never touches — which is exactly the fixture a
    /// test of the <em>create predicate</em> wants. The three tests about the loop closing ask for a
    /// placement pass by name.
    /// </remarks>
    private static Ruleset Zoned(params ZoneRuleDefinition[] zoneRules) =>
        Zoned(PlacementRuleset.None, zoneRules);

    private static Ruleset Zoned(
        PlacementRuleset placement, params ZoneRuleDefinition[] zoneRules) => new(
        resources: [],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0) { Tenanted = 1 > 0 }],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: zoneRules)
    {
        Placement = placement,
        Capacity = new CapacityRuleset(FloorPerOccupant, 0, 0),
    };

    /// <summary>One Tile of floor houses one Household, so a one-Tile Lot holds exactly one.</summary>
    /// <remarks>
    /// <b>The rate is the fixture's whole occupancy declaration</b> (<c>plans/0053</c>). A kind says
    /// <em>whether</em> it houses; how many derives from the ground, so the smallest world that
    /// houses anybody is the one whose rate is one Tile and whose Lots are one Tile square.
    /// </remarks>
    private const int FloorPerOccupant = 1;

    /// <summary>A placement pass that looks at everybody waiting, every trigger.</summary>
    /// <remarks>
    /// <c>revisit_ticks</c> equal to the interval is the fastest legal survey, and <c>candidates</c>
    /// as wide as the fixtures' Lot count means a seeker that fails found nothing rather than looked
    /// too narrowly. Both make the loop tests statements about the <em>loop</em>.
    /// </remarks>
    private static PlacementRuleset Placing(uint interval = 4, int candidates = 64) =>
        new(interval, (int)interval, candidates, 0);

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

    /// <summary>
    /// A Zone Rule surveying the whole city every trigger.
    /// </summary>
    /// <remarks>
    /// <b>The revisit period equals the interval, which is the fastest survey a Ruleset can legally
    /// author</b> (<c>adr/0059</c>: the loader refuses a shorter one). It derives a sample of one draw
    /// per Lot per trigger, which is what these fixtures want — the predicate is what is under test,
    /// and a sampler that had to find the one interesting Lot would be testing the sampler. Drawing is
    /// with replacement, so a trigger still misses about a third of the Lots; the runs are long enough
    /// for that not to matter, and the two tests that care about being missed say so.
    /// </remarks>
    private static ZoneRuleDefinition Rule(int revisit = 4, uint interval = 4) =>
        new(House, HousingBit, interval, revisit);

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
            Zoned(new ZoneRuleDefinition(House, 7, 4, 4)));

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
    /// <b>Construction houses nobody</b> (<c>adr/0069</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test asserted the opposite until <c>adr/0069</c>, and the sentence it asserted was the
    /// bug.</b> A Building used to take one Household out of the Pool as it was raised, which read as
    /// a conservation law — creation drains the signal that authorised it — and was really the whole
    /// of <c>02 §5.2</c> step 2 compressed into step 5. It housed one family per Building whatever the
    /// kind declared it held, so a demolition returning three Occupants was answered by a rebuild
    /// taking one, and a 100,000-Tick run settled five-sixths homeless.
    /// </para>
    /// <para>
    /// <b>Growth is still self-limiting and the limit now runs through the Pool rather than around
    /// it.</b> The create predicate reads the Pool and the placement pass drains it, so a Ruleset
    /// still cannot build past its demand — one placement cycle later instead of instantly, which is
    /// what <c>Growth_stops_when_everybody_is_housed</c> asserts.
    /// </para>
    /// </remarks>
    [Fact]
    public void Creating_houses_nobody()
    {
        (World world, Simulation simulation) = Built(Zoned(Rule()), seeking: 6);

        ZoneActivity activity = Run(simulation, 256);

        Assert.True(activity.Created.Sum > 0);
        Assert.Equal(6, world.UnplacedPool.Count);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot) || slot < 6)
            {
                continue;
            }

            Assert.Equal(0, world.Occupants.Length(slot));
        }
    }

    /// <summary>Growth stops when demand is met, and stays stopped.</summary>
    /// <remarks>
    /// <b>The self-limit, and it needs both halves of the loop to exist.</b> The placement pass drains
    /// the Pool into what has been built, the create predicate reads the Pool, and the two together
    /// are what stops a wide sample building a city for nobody.
    /// </remarks>
    [Fact]
    public void Growth_stops_when_everybody_is_housed()
    {
        (World world, Simulation simulation) = Built(Zoned(Placing(), Rule()), seeking: 3);

        Assert.True(Run(simulation, 512).Created.Sum > 0);
        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.Equal(0, Run(simulation, 512).Created.Sum);
    }

    /// <summary>
    /// Every Household the loop houses ends up living in the Building that admitted it.
    /// </summary>
    [Fact]
    public void Everybody_housed_by_growth_lives_where_they_were_placed()
    {
        (World world, Simulation simulation) = Built(Zoned(Placing(), Rule()), seeking: 5);

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
            Zoned(Rule(), Rule()), vacant: 4, seeking: 8);

        ZoneActivity activity = Run(simulation, 128);

        // Four vacant Lots and two Rules sampling widely enough to reach all of them on every
        // trigger. Exactly four Buildings appear, so the second Rule found what the first had taken.
        Assert.Equal(4, activity.Created.Sum);
        Assert.Equal(12, world.Buildings.Rows.LiveCount);

        // And the Pool is untouched, because there is no placement pass here and construction houses
        // nobody: the eight seekers authorised the four Buildings and none of them moved in.
        Assert.Equal(8, world.UnplacedPool.Count);

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

    // Who moves in is drawn rather than taken from the front of the queue -- which used to be a
    // Zone Rule's property and is the placement pass's since adr/0069. It lives in PlacementTests.
}
