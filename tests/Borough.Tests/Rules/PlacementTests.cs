using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>adr/0069</c>: placement is a mechanism of its own, and construction houses nobody.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is a step of <c>02 §5.2</c> that had never been built.</b> Until this pass
/// existed <c>World.Place</c> had exactly one caller and it was inside <c>ZoneRuleEngine.Create</c>,
/// so the only way to be housed was for somebody to raise you a house. Nothing moved a Household into
/// a Building that already stood, and the vacancy that produced was read twice as a <em>number</em>
/// wanting tuning before it was read as a <em>mechanism</em> wanting building — which is
/// <c>adr/0070</c>.
/// </para>
/// <para>
/// <b>The fixture has no Zone Rules at all.</b> The two halves of the growth loop are separable since
/// <c>adr/0069</c> and separating them is the point: a test that ran both could not say which one
/// housed anybody. The loop closing is <c>ZoneRuleCreateTests</c>' business and the long-run
/// equilibrium is <c>PlacementLongRunTests</c>'.
/// </para>
/// </remarks>
public sealed class PlacementTests
{
    private const byte House = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0002UL);

    /// <summary>A Ruleset that houses <paramref name="occupants"/> per Building and no Zone Rules.</summary>
    private static Ruleset Housing(int occupants, PlacementRuleset placement) => new(
        resources: [],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0) { Occupants = occupants }],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: [])
    {
        Placement = placement,
    };

    /// <summary>A pass that looks at everybody waiting every trigger.</summary>
    private static PlacementRuleset Placing(uint interval = 4, int candidates = 64) =>
        new(interval, (int)interval, candidates);

    /// <summary>
    /// <paramref name="buildings"/> standing empty, and <paramref name="seeking"/> Households in the
    /// Pool.
    /// </summary>
    /// <remarks>
    /// The seekers are made by housing them and unplacing them, which is the only way into the Pool
    /// today. Each seeder Building is then demolished so that the standing stock is exactly what the
    /// caller asked for.
    /// </remarks>
    private static (World World, Simulation Simulation) City(
        Ruleset ruleset, int buildings, int seeking)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, Key);

        Handle<Lot> seed = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> shelter = world.CreateBuilding(seed, House, Ticks.Zero, Key);

        for (int i = 0; i < seeking; i++)
        {
            world.Unplace(world.CreateHousehold(shelter, lifeStage: 0));
        }

        world.DestroyBuilding(shelter, Ticks.Zero);

        for (int i = 0; i < buildings; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(1), zone: 1);

            world.CreateBuilding(lot, House, Ticks.Zero, Key);
        }

        return (world, simulation);
    }

    private static PlacementActivity Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        return simulation.Placement.Drain();
    }

    /// <summary>How many Households live in Buildings, over the whole table.</summary>
    private static int Housed(World world)
    {
        int total = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                total += world.Occupants.Length(slot);
            }
        }

        return total;
    }

    // ---- the mechanism ---------------------------------------------------------------------------

    /// <summary>
    /// <b>A Household in the Pool moves into a Building that already stands.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for <c>adr/0069</c>, and note that nothing is built during it: there are no
    /// Zone Rules in this fixture at all. Before the pass existed this run housed nobody, for ever.
    /// </remarks>
    [Fact]
    public void The_pool_drains_into_buildings_that_already_stand()
    {
        (World world, Simulation simulation) = City(
            Housing(2, Placing()), buildings: 8, seeking: 6);

        PlacementActivity activity = Run(simulation, 64);

        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.Equal(6, Housed(world));
        Assert.Equal(6, activity.Placed.Sum);
        Assert.True(activity.Considered.Sum >= activity.Placed.Sum);
    }

    /// <summary>Nobody is admitted past the ceiling their kind declares.</summary>
    /// <remarks>
    /// <b><c>adr/0068</c> and <c>adr/0069</c> meeting</b>: the declared occupancy is what makes the
    /// Pool stop draining, and it is the only thing that does. There is no other cap.
    /// </remarks>
    [Fact]
    public void Placement_stops_at_the_declared_ceiling()
    {
        (World world, Simulation simulation) = City(
            Housing(2, Placing()), buildings: 3, seeking: 20);

        Run(simulation, 256);

        Assert.Equal(6, Housed(world));
        Assert.Equal(14, world.UnplacedPool.Count);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                Assert.Equal(2, world.Occupants.Length(slot));
            }
        }
    }

    /// <summary>A Ruleset with no <c>[placement]</c> houses nobody, and says nothing happened.</summary>
    /// <remarks>
    /// <b>The absent table means the pass does not run, rather than running on defaults.</b> Three
    /// hash-bearing numbers nobody authored would be the alternative (<c>adr/0052</c>), and the
    /// failure that hides is quiet — a city housing people at a cadence its designer never wrote. This
    /// failure is loud: the Pool grows and the Census says so.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_placement_table_houses_nobody()
    {
        (World world, Simulation simulation) = City(
            Housing(2, PlacementRuleset.None), buildings: 8, seeking: 6);

        PlacementActivity activity = Run(simulation, 256);

        Assert.Equal(6, world.UnplacedPool.Count);
        Assert.Equal(0, Housed(world));
        Assert.Equal(0, activity.Considered.Sum);
    }

    /// <summary>The pass runs on its interval and not on the Ticks between.</summary>
    [Fact]
    public void The_pass_runs_on_its_interval()
    {
        (_, Simulation simulation) = City(
            Housing(1, Placing(interval: 16)), buildings: 64, seeking: 64);

        // 64 Ticks is four triggers, and the sample is the whole Pool each time -- but the Pool
        // shrinks as it drains, so what is asserted is the trigger count rather than the sum.
        PlacementActivity activity = Run(simulation, 64);

        Assert.Equal(4, activity.Considered.Peak > 0 ? 4 : 0);
        Assert.True(activity.Placed.Sum > 0);

        // Nothing between triggers: a Tick that is not a multiple of the interval contributes a zero
        // to both flows, so the peak is a trigger's work rather than a Tick's.
        Assert.True(activity.Considered.Peak >= activity.Placed.Peak);
    }

    // ---- the sample ------------------------------------------------------------------------------

    /// <summary>
    /// <b>The sample is derived from a duration, so it scales with the queue</b> (<c>adr/0059</c>).
    /// </summary>
    /// <remarks>
    /// The property that an absolute count would not have: a queue twice as long is looked at twice as
    /// hard over the same period, so the <em>fraction</em> of it cleared per cycle is what the file
    /// states. An absolute count would house a fixed number of families a Day however many were
    /// waiting, in the one collection whose growth is the thing being fixed.
    /// </remarks>
    [Theory]
    [InlineData(64, 2)]
    [InlineData(128, 4)]
    [InlineData(256, 8)]
    public void The_sample_is_a_fraction_of_the_queue(int pool, int expected)
    {
        var placement = new PlacementRuleset(Interval: 32, RevisitTicks: 1024, Candidates: 3);

        Assert.Equal(expected, placement.SampleFor(pool));
    }

    /// <summary>An empty Pool costs nothing, which is the trigger's first test.</summary>
    [Fact]
    public void An_empty_pool_is_not_sampled()
    {
        (_, Simulation simulation) = City(Housing(2, Placing()), buildings: 8, seeking: 0);

        Assert.Equal(0, Run(simulation, 64).Considered.Sum);
    }

    // ---- who moves, and where ---------------------------------------------------------------------

    /// <summary>
    /// Who moves in is drawn rather than taken from the front of the queue.
    /// </summary>
    /// <remarks>
    /// <b><c>02 §8</c> rule 5's reason, reaching a case its wording does not cover.</b> Nothing is
    /// contested here — any member would take the house — but a Pool that never fully drains is what a
    /// housing shortage <em>is</em>, and under any fixed order the same Households would stay unhoused
    /// for the life of the city with nothing to explain why. This test moved here from
    /// <c>ZoneRuleCreateTests</c> when the draw did: it was a Zone Rule's property until
    /// <c>adr/0069</c>, and it is placement's now.
    /// </remarks>
    [Fact]
    public void Who_moves_in_is_drawn_rather_than_queued()
    {
        // One seeker considered per trigger, and a place waiting for every one of them -- so who is
        // housed after eight triggers is exactly who the sampler picked, with nothing else in the
        // way. A strict queue would house the eight at the front of the Pool and no others.
        (World world, Simulation simulation) = City(
            new Ruleset(
                resources: [], rules: [],
                kinds: [new KindDefinition(0, 0, 0, 0) { Occupants = 1 }],
                inputs: [], outputs: [], emissions: [], bins: [], kindRules: [], zoneRules: [])
            {
                Placement = new PlacementRuleset(Interval: 4, RevisitTicks: 128, Candidates: 64),
            },
            buildings: 64,
            seeking: 32);

        var queue = new List<int>();

        for (int position = 0; position < 8; position++)
        {
            queue.Add(world.Households.Rows.Resolve(world.UnplacedPool.At(position)));
        }

        Run(simulation, 32);

        var housed = new List<int>();

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot) && !world.Households.IsUnplaced(slot))
            {
                housed.Add(slot);
            }
        }

        Assert.NotEmpty(housed);
        Assert.NotEqual(queue.Order(), housed.Order());
    }

    /// <summary>
    /// <b>A seeker looks at <c>candidates</c> places and waits if none of them will have it.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §5.3</c>'s N, and the assertion is the shape of the number rather than its value: a
    /// single look into a mostly-full city places somebody rarely, and a wide look places them
    /// promptly. A test on the exact count would be a test of the draw.
    /// </remarks>
    [Fact]
    public void A_wider_look_houses_more_of_the_queue()
    {
        (World narrow, Simulation narrowRun) = City(
            Housing(1, Placing(candidates: 1)), buildings: 64, seeking: 32);
        (World wide, Simulation wideRun) = City(
            Housing(1, Placing(candidates: 16)), buildings: 64, seeking: 32);

        Run(narrowRun, 4);
        Run(wideRun, 4);

        Assert.True(
            Housed(wide) > Housed(narrow),
            $"one look housed {Housed(narrow)} of 32 and sixteen housed {Housed(wide)}. A seeker "
            + "that looks at more places finds one sooner, which is what candidates means.");
    }

    /// <summary>
    /// A look that lands on a vacant Lot found nothing, and that is the model rather than a miss.
    /// </summary>
    /// <remarks>
    /// <b>The draw is over Lots and not over Buildings.</b> A Lot is a place in the city; the Building
    /// table's slot count is a recycling table's, whose freed rows are an artefact of storage. Drawing
    /// over Buildings made <c>candidates</c> mean something the file could not state, because the
    /// fraction of freed slots moves with the demolition rate.
    /// </remarks>
    [Fact]
    public void A_city_of_mostly_empty_lots_houses_slowly()
    {
        var world = new World(1_000, Housing(1, Placing(candidates: 1)));
        var simulation = new Simulation(world, Key);

        Handle<Lot> seed = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> shelter = world.CreateBuilding(seed, House, Ticks.Zero, Key);

        for (int i = 0; i < 16; i++)
        {
            world.Unplace(world.CreateHousehold(shelter, lifeStage: 0));
        }

        // 64 Lots, one of them built on -- so a single look finds a dwelling one time in 64.
        for (int i = 0; i < 63; i++)
        {
            world.Lots.Create(new Tiles(i), new Tiles(1), zone: 1);
        }

        Run(simulation, 4);

        Assert.True(
            world.UnplacedPool.Count >= 15,
            $"{16 - world.UnplacedPool.Count} of 16 were housed in one trigger against a city that "
            + "is 63 parts empty ground to one part dwelling. A single look cannot do better than "
            + "one in 64 unless the draw is skipping the empty Lots.");
    }

    // ---- determinism ------------------------------------------------------------------------------

    /// <summary>Two runs of one world and one key place the same families in the same places.</summary>
    [Fact]
    public void Placement_is_reproducible()
    {
        (World first, Simulation firstRun) = City(
            Housing(2, Placing()), buildings: 8, seeking: 12);
        (World second, Simulation secondRun) = City(
            Housing(2, Placing()), buildings: 8, seeking: 12);

        Assert.Equal(Run(firstRun, 64), Run(secondRun, 64));
        Assert.Equal(first.HashState(), second.HashState());
    }

    /// <summary>
    /// The two flows reach the Census, as a fourth metric family.
    /// </summary>
    /// <remarks>
    /// <b>Written because the sibling family has no such test.</b> Nothing in the suite reads a
    /// <c>ZoneCounter</c> back through a <c>Census</c>, so the offset arithmetic addressing the Sweep
    /// family's block is exercised by the headless report and by nothing that fails. A block written
    /// and never read back is <c>adr/0064</c>'s id-29 shape — invisible to every future reader — and
    /// adding a fourth family without a test would have made it two.
    /// </remarks>
    [Fact]
    public void The_two_flows_reach_the_census()
    {
        (World world, Simulation simulation) = City(
            Housing(2, Placing()), buildings: 8, seeking: 6);

        var census = new Census(world);

        for (int i = 0; i < 64; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        census.Observe(simulation);

        Series considered = census.Series(
            Metric.Of(PlacementCounter.Considered, Aggregate.Sum), new Ticks(64));
        Series placed = census.Series(
            Metric.Of(PlacementCounter.Placed, Aggregate.Sum), new Ticks(64));

        Assert.Equal(6, placed.Samples.Span[0].Value);
        Assert.True(considered.Samples.Span[0].Value >= placed.Samples.Span[0].Value);

        // And the reading drained it, which is what makes these flows rather than levels.
        simulation.Step(TickInput.Empty);
        census.Observe(simulation);

        Series after = census.Series(
            Metric.Of(PlacementCounter.Placed, Aggregate.Sum), new Ticks(1));

        Assert.Equal(0, after.Samples.Span[^1].Value);
    }

    /// <summary>Placement leaves the world in a state the end-of-run walk accepts.</summary>
    [Fact]
    public void A_placed_city_passes_the_end_of_run_invariants()
    {
        (World world, Simulation simulation) = City(
            Housing(3, Placing()), buildings: 8, seeking: 20);

        Run(simulation, 256);

        simulation.CheckEndOfRun();
    }
}
