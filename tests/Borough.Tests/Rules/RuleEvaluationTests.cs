using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 7 task 5: Rule evaluation, the four scopes, and atomicity.
/// </summary>
/// <remarks>
/// <b>Most of these tests are about what did <em>not</em> happen.</b> <c>02 §4.1</c> makes atomicity
/// the core semantic — <em>if any input is insufficient or any output would exceed capacity, nothing
/// happens</em> — and a half-applied Rule is not a visible failure. It is Goods that became nothing,
/// in a Bin nobody was watching, with no single cause to name.
/// </remarks>
public sealed class RuleEvaluationTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);

    /// <summary>
    /// The money Resource, for the treasury cases. <b>Id 1, the same slot <see cref="Flour"/> uses</b>
    /// — a Ruleset's Resource ids are positional, so which family id 1 belongs to is that Ruleset's
    /// own declaration and the two never appear in one.
    /// </summary>
    private static readonly ResourceId Coin = new(1);

    private const byte Bakery = 1;
    private const uint Rate = 8;

    /// <summary>One Rule definition, spelled positionally once so the tests below read as data.</summary>
    private static RuleDefinition Rule(
        int inputFirst, int inputCount, int outputFirst, int outputCount,
        int emissionFirst = 0, int emissionCount = 0, int min = 1, int max = 1) =>
        new(Bakery, Rate, ApplyCount.Band(min, max), RuleId.None, false, default, ConditionId.None,
            inputFirst, inputCount, outputFirst, outputCount, emissionFirst, emissionCount);

    /// <summary><c>02 §4.3</c>'s bakery: six flour in, four bread out, every eight Ticks.</summary>
    private static Ruleset Baking() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules: [Rule(0, 1, 0, 1)],
        kinds: [new KindDefinition(0, 2, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [new Term(new BinRef(Scope.Local, Bread), 4)],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60)), new BinDeclaration(Bread, BinCapacity.Of(20))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>
    /// A world with one bakery, its declared Bins, and its Rules armed for Tick 1.
    /// </summary>
    /// <remarks>
    /// <b>The Bins and the Rule Instances are created from the Ruleset's own declarations</b>, which is
    /// what task 10 will do at construction. Writing the loop here rather than hard-coding two
    /// <c>CreateBin</c> calls keeps the tests honest about where a Building's Bin set comes from.
    /// </remarks>
    private static (World World, Simulation Simulation, Handle<Building> Building) Built(
        Ruleset ruleset, ulong seed = 1)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(seed));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Bakery);

        foreach (BinDeclaration bin in ruleset.BinsOf(Bakery))
        {
            world.CreateBin(building, bin.Resource);
        }

        foreach (RuleId rule in ruleset.RulesOf(Bakery))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay: 1);
        }

        return (world, simulation, building);
    }

    private static int BinOf(World world, Handle<Building> building, ResourceId resource) =>
        world.FindBin(world.Buildings.Rows.Resolve(building), resource);

    private static long Level(World world, Handle<Building> building, ResourceId resource) =>
        world.Bins.LevelAt(BinOf(world, building, resource));

    /// <summary>Runs to the end of Tick 1, which is the Tick the helper arms for.</summary>
    private static void StepToTheFiring(Simulation simulation)
    {
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);
    }

    // ---- the Rule fires ---------------------------------------------------------------------------

    [Fact]
    public void A_rule_with_its_inputs_fires_and_the_goods_move()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(54, Level(world, building, Flour));
        Assert.Equal(4, Level(world, building, Bread));
    }

    /// <summary>A Rule that fires re-arms on the Wheel at <c>+rate</c>, and on nothing else.</summary>
    [Fact]
    public void A_rule_that_fires_re_arms_at_its_rate()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        int instance = 0;

        Assert.False(world.RuleInstances.IsWaiting(instance));
        Assert.Equal(new Ticks(1 + Rate), world.RuleInstances.NextTick[instance]);
        Assert.Equal(instance, world.Wheel.Armed.PeekFront(EventWheel.BucketOf(new Ticks(1 + Rate))));
    }

    /// <summary>Fires, sleeps out its rate, fires again — which is what makes a rate a rate.</summary>
    [Fact]
    public void A_rule_fires_once_per_rate_and_not_once_per_tick()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        for (int tick = 0; tick < 1 + (3 * (int)Rate); tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        // Ticks 1, 9 and 17 fired; 18 has not been reached.
        Assert.Equal(60 - (3 * 6), Level(world, building, Flour));
    }

    // ---- the Rule fails ---------------------------------------------------------------------------

    [Fact]
    public void A_rule_short_of_its_input_moves_nothing_and_subscribes()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 3, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(3, Level(world, building, Flour));
        Assert.Equal(0, Level(world, building, Bread));

        int instance = 0;

        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[instance]);

        // What it waits for is derived rather than recorded (adr/0063), and it is the whole
        // requirement rather than the deficit: six flour to fire, against the three in the Bin.
        Assert.Equal(
            6,
            RuleEngine.Requirement(
                world, instance, BinOf(world, building, Flour), Blocking.Supply));
        Assert.Equal(
            BinOf(world, building, Flour),
            world.Bins.Rows.Resolve(world.RuleInstances.WaitingOn[instance]));
    }

    /// <summary>
    /// <b>Atomicity, stated as the thing that would otherwise go unnoticed.</b> The first input is
    /// abundant and the second is short, so a Rule applied term by term would have spent the first.
    /// </summary>
    [Fact]
    public void A_rule_short_of_its_second_input_does_not_spend_its_first()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules: [Rule(0, 2, 0, 0)],
            kinds: [new KindDefinition(0, 2, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6), new Term(new BinRef(Scope.Local, Bread), 6)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60)), new BinDeclaration(Bread, BinCapacity.Of(20))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World world, Simulation simulation, Handle<Building> building) = Built(ruleset);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);
        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Bread)), 2, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60, Level(world, building, Flour));
        Assert.Equal(2, Level(world, building, Bread));
        Assert.Equal(
            BinOf(world, building, Bread),
            world.Bins.Rows.Resolve(world.RuleInstances.WaitingOn[0]));

        Assert.Equal(
            6,
            RuleEngine.Requirement(
                world, 0, BinOf(world, building, Bread), Blocking.Supply));
    }

    /// <summary>
    /// A full output Bin fails the Rule too, on <see cref="Blocking.Space"/> — <c>adr/0045</c>'s
    /// generalisation of <em>blocking</em> over both failure modes.
    /// </summary>
    [Fact]
    public void A_full_output_bin_blocks_the_rule_on_space()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);
        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Bread)), 18, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60, Level(world, building, Flour));
        Assert.Equal(18, Level(world, building, Bread));

        Assert.Equal(Blocking.Space, world.RuleInstances.Blocked[0]);

        Assert.Equal(
            4,
            RuleEngine.Requirement(
                world, 0, BinOf(world, building, Bread), Blocking.Space));

        Assert.Equal(
            BinOf(world, building, Bread),
            world.Bins.Rows.Resolve(world.RuleInstances.WaitingOn[0]));
    }

    /// <summary>
    /// <b>The case term-by-term checking deadlocks on.</b> A Rule drawing six from a Bin it also
    /// returns four to is checked as a net two out — where checking the output alone against a full
    /// Bin's space would refuse it, and the Rule would then wait on a Bin nothing else ever drains.
    /// </summary>
    [Fact]
    public void A_rule_naming_one_bin_on_both_sides_is_checked_net()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good],
            rules: [Rule(0, 1, 0, 1)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
            outputs: [new Term(new BinRef(Scope.Local, Flour), 4)],
            emissions: [],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World world, Simulation simulation, Handle<Building> building) = Built(ruleset);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(58, Level(world, building, Flour));
        Assert.False(world.RuleInstances.IsWaiting(0));
    }

    // ---- the apply count ----------------------------------------------------------------------------

    /// <summary><c>02 §4.3</c>'s bakery again, with a band rather than a fixed count.</summary>
    private static Ruleset BakingWithin(int min, int max) => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules: [Rule(0, 1, 0, 1, min: min, max: max)],
        kinds: [new KindDefinition(0, 2, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [new Term(new BinRef(Scope.Local, Bread), 4)],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60)), new BinDeclaration(Bread, BinCapacity.Of(20))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>
    /// <b>Greedy: the bakery bakes the flour it has</b>, up to its own ceiling. Sixty flour would
    /// support ten bakings and the Rule is allowed four, so four is what happens.
    /// </summary>
    [Fact]
    public void A_greedy_rule_applies_as_many_times_as_the_band_and_the_bins_allow()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(BakingWithin(1, 4));

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60 - (4 * 6), Level(world, building, Flour));
        Assert.Equal(4 * 4, Level(world, building, Bread));
        Assert.False(world.RuleInstances.IsWaiting(0));
    }

    /// <summary>
    /// <b>The bound is whichever Bin runs out first, and an output counts.</b> Ten bakings of flour
    /// exist; five bread fit. The Rule fires at five and does <em>not</em> fail on space, which is
    /// the distinction a fixed Rule cannot express.
    /// </summary>
    [Fact]
    public void A_greedy_rule_stops_at_the_space_of_its_output_rather_than_failing_on_it()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(BakingWithin(1, 10));

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60 - (5 * 6), Level(world, building, Flour));
        Assert.Equal(20, Level(world, building, Bread));
        Assert.False(world.RuleInstances.IsWaiting(0));
    }

    /// <summary>
    /// <b><c>adr/0035</c>'s Upkeep, as a test.</b> A fixed Rule owes a quantum and must never draw more
    /// because the treasury happens to be full — so ten bakings' worth of flour still buys two bakings.
    /// </summary>
    [Fact]
    public void A_fixed_rule_draws_its_quantum_and_no_more_however_full_the_bin()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(BakingWithin(2, 2));

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60 - (2 * 6), Level(world, building, Flour));
        Assert.Equal(2 * 4, Level(world, building, Bread));
    }

    /// <summary>
    /// <b>Below <c>min</c> is a failure, and what it waits for is the floor's requirement rather than
    /// the ceiling's.</b> A Rule that waited for its <em>max</em> would sleep through a delivery it
    /// could have used.
    /// </summary>
    [Fact]
    public void A_greedy_rule_below_its_floor_fails_and_subscribes_on_the_floor()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(BakingWithin(2, 4));

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 9, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(9, Level(world, building, Flour));
        Assert.Equal(0, Level(world, building, Bread));

        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[0]);

        Assert.Equal(
            2 * 6,
            RuleEngine.Requirement(
                world, 0, BinOf(world, building, Flour), Blocking.Supply));
    }

    /// <summary>
    /// <b>The raise reads the net delta, exactly as the check does.</b> Six drawn and four returned to
    /// one Bin is two out per application, so sixty flour affords thirty applications and not ten. A
    /// raise computed on the gross draw would leave forty flour in a Bin the Rule was entitled to empty.
    /// </summary>
    [Fact]
    public void A_greedy_rule_is_raised_on_its_net_delta_and_not_on_its_gross_draw()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good],
            rules: [Rule(0, 1, 0, 1, min: 1, max: 40)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
            outputs: [new Term(new BinRef(Scope.Local, Flour), 4)],
            emissions: [],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World world, Simulation simulation, Handle<Building> building) = Built(ruleset);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(0, Level(world, building, Flour));
        Assert.False(world.RuleInstances.IsWaiting(0));
    }

    /// <summary>A Map emission is per application, so a Rule applying four times emits four times.</summary>
    [Fact]
    public void A_greedy_rule_emits_once_for_every_application()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good],
            rules: [Rule(0, 1, 0, 0, emissionFirst: 0, emissionCount: 1, min: 1, max: 4)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
            outputs: [],
            emissions: [new MapEmission(Layer.IndustrialPollution, 40)],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World world, Simulation simulation, Handle<Building> building) = Built(ruleset);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(4 * 40, world.Layers.PollutionSource(Cells.Zero, Cells.Zero));
    }

    // ---- the apply count, across the Decide/Settle boundary ------------------------------------------

    /// <summary>Two greedy Rules on one Building, and six bakings' worth of flour between them.</summary>
    private static Ruleset ContestedGreedy() => new(
        resources: [ResourceFamily.Good],
        rules: [Rule(0, 1, 0, 0, min: 1, max: 4), Rule(1, 1, 0, 0, min: 1, max: 4)],
        kinds: [new KindDefinition(0, 1, 0, 2)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6), new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
        kindRules: [new RuleId(1), new RuleId(2)],
        zoneRules: []);

    /// <summary>
    /// <b>The Phase 3 re-check serves a greedy Rule short rather than failing it.</b> Both Rules decide
    /// on four; the winner takes four and leaves two bakings' worth, and the loser fires at two. Before
    /// the count could move, the loser would have fired at one and left six flour on the table with
    /// nobody able to reach it until the next rate.
    /// </summary>
    [Fact]
    public void The_settle_re_check_serves_a_greedy_rule_short_rather_than_failing_it()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(ContestedGreedy());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 36, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(0, Level(world, building, Flour));
        Assert.False(world.RuleInstances.IsWaiting(0));
        Assert.False(world.RuleInstances.IsWaiting(1));
    }

    /// <summary>
    /// One Rule making twenty-four flour from nothing, and one greedy Rule eating six at a time.
    /// </summary>
    private static Ruleset ProducerAndGreedyConsumer() => new(
        resources: [ResourceFamily.Good],
        rules: [Rule(0, 1, 0, 0, min: 1, max: 4), Rule(0, 0, 0, 1, min: 1, max: 1)],
        kinds: [new KindDefinition(0, 1, 0, 2)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [new Term(new BinRef(Scope.Local, Flour), 24)],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
        kindRules: [new RuleId(1), new RuleId(2)],
        zoneRules: []);

    /// <summary>
    /// <b>…and it may never serve one <em>more</em>, which is what makes Phase 2 the deciding phase.</b>
    /// Twelve flour exist when the consumer decides, so it decides on two. The producer then deposits
    /// twenty-four. Whichever settles first, the consumer eats twelve and the Bin ends on twenty-four —
    /// where a re-check free to re-derive its own count would eat twenty-four in the orders where the
    /// producer went first, and the same city would consume different amounts depending on a shuffle.
    /// </summary>
    /// <remarks>
    /// Across thirty-two seeds because the settle order is drawn per (instance, Tick) and both orders
    /// have to be reached for this to be worth asserting.
    /// </remarks>
    [Fact]
    public void The_settle_re_check_never_serves_a_greedy_rule_more_than_it_decided_on()
    {
        for (ulong seed = 1; seed <= 32; seed++)
        {
            (World world, Simulation simulation, Handle<Building> building) =
                Built(ProducerAndGreedyConsumer(), seed);

            world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 12, Ticks.Zero);

            StepToTheFiring(simulation);

            Assert.Equal(12 + 24 - (2 * 6), Level(world, building, Flour));
        }
    }

    // ---- the subscription, end to end -------------------------------------------------------------

    /// <summary>
    /// The whole mechanism in one test: fail, sleep, and be woken by the mutator that writes the Bin.
    /// </summary>
    [Fact]
    public void A_deposit_wakes_the_rule_it_covers_and_it_fires_the_next_tick()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        StepToTheFiring(simulation);

        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[0]);

        // Nothing on the Wheel: a starved Building costs nothing until supply arrives.
        for (int tick = 0; tick < 32; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[0]);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 6, simulation.Tick);

        Assert.False(world.RuleInstances.IsWaiting(0));

        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);

        Assert.Equal(0, Level(world, building, Flour));
        Assert.Equal(4, Level(world, building, Bread));
    }

    // ---- the settle order -------------------------------------------------------------------------

    /// <summary>Two Rules on one Building, six flour, and only one of them can have it.</summary>
    private static Ruleset Contested() => new(
        resources: [ResourceFamily.Good],
        rules: [Rule(0, 1, 0, 0), Rule(1, 1, 0, 0)],
        kinds: [new KindDefinition(0, 1, 0, 2)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6), new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
        kindRules: [new RuleId(1), new RuleId(2)],
        zoneRules: []);

    /// <summary>
    /// <b><c>02 §8</c> rule 5, measured.</b> Ordering a contested draw by entity id is <em>biased</em>:
    /// the lower slot would win every time, for the life of the city, and nothing observing the running
    /// city could see why. Here the same two Rule Instances contest the same six flour in thirty-two
    /// worlds identical but for the Tick, and both win some of them.
    /// </summary>
    [Fact]
    public void The_settle_order_is_a_shuffle_and_not_the_slot_order()
    {
        var wins = new int[2];

        for (int offset = 0; offset < 32; offset++)
        {
            (World world, Simulation simulation, Handle<Building> building) = Contest(offset);

            Assert.Equal(0, Level(world, building, Flour));

            int loser = world.RuleInstances.IsWaiting(0) ? 0 : 1;

            Assert.True(world.RuleInstances.IsWaiting(loser));
            Assert.False(world.RuleInstances.IsWaiting(1 - loser));

            wins[1 - loser]++;
        }

        Assert.True(wins[0] > 0 && wins[1] > 0,
            $"one Rule Instance won every contested draw ({wins[0]} to {wins[1]}). "
            + "02 §8 rule 5: a contested outcome is shuffled, never ordered by id.");
    }

    /// <summary>The same contest twice is the same contest — a shuffle, not a coin.</summary>
    [Fact]
    public void The_shuffle_is_deterministic()
    {
        for (int offset = 0; offset < 8; offset++)
        {
            (World first, _, _) = Contest(offset);
            (World again, _, _) = Contest(offset);

            Assert.Equal(first.HashState(), again.HashState());
        }
    }

    /// <summary>Runs one contested Tick at <paramref name="offset"/>, with six flour on the table.</summary>
    private static (World World, Simulation Simulation, Handle<Building> Building) Contest(int offset)
    {
        var world = new World(1_000, Contested());
        var simulation = new Simulation(world, WorldKey.FromSeed(1));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Bakery);

        world.CreateBin(building, Flour);

        for (int tick = 0; tick < offset; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        foreach (RuleId rule in world.Rules.RulesOf(Bakery))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay: 1);
        }

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 6, simulation.Tick);

        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);

        return (world, simulation, building);
    }

    // ---- Phase 2 writes nothing --------------------------------------------------------------------

    /// <summary>
    /// <c>adr/0037</c>'s load-bearing property, checked against a Tick that actually does something.
    /// </summary>
    /// <remarks>
    /// <b>The second assertion is the one that stops this passing vacuously.</b> A guard over a Tick
    /// where no Rule fired proves nothing at all; this one proves the world moved <em>and</em> that
    /// none of the movement happened in Phase 2.
    /// </remarks>
    [Fact]
    public void Deciding_writes_nothing_even_on_a_tick_where_rules_fire()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        Assert.True(simulation.VerifyDecideWritesNothing);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        ulong before = world.HashState();

        StepToTheFiring(simulation);

        Assert.NotEqual(before, world.HashState());
    }

    // ---- the named holes ----------------------------------------------------------------------------

    private static Ruleset Scoped(Scope scope) => new(
        resources: [ResourceFamily.Good],
        rules: [Rule(0, 1, 0, 0)],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [new Term(new BinRef(scope, Flour), 6)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>
    /// <c>pool</c> throws rather than resolving to an empty Bin, which is slice 6's pattern: a
    /// placeholder returning zero is a value somebody reads and tunes around.
    /// </summary>
    /// <remarks>
    /// <b>⚠ <c>global</c> was the second row of this theory and left it in milestone 10 task 1</b>,
    /// which is the whole point of that task — <c>adr/0114</c> supplied the entity decision the throw
    /// was waiting on. It is not deleted: it is replaced by the positive assertions below, because a
    /// closed hole whose negative test simply vanishes leaves nothing saying what it closed to.
    /// <c>pool</c> stays a hole until milestone 12, and it is a <b>market</b> rather than a Bin lookup.
    /// </remarks>
    [Theory]
    [InlineData(Scope.Pool)]
    public void A_scope_this_build_does_not_have_is_a_named_hole(Scope scope)
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Scoped(scope));

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        Assert.Throws<NotSupportedException>(() => StepToTheFiring(simulation));
    }

    // ---- the treasury -------------------------------------------------------------------------------

    /// <summary><c>02 §4.3</c>'s transfer: local money out, global money in, in one atomic Rule.</summary>
    private static Ruleset Taxing(int amount = 6) => new(
        resources: [ResourceFamily.Money],
        rules: [Rule(0, 1, 0, 1)],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Coin), amount)],
        outputs: [new Term(new BinRef(Scope.Global, Coin), amount)],
        emissions: [],
        bins: [new BinDeclaration(Coin, BinCapacity.Unbounded)],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>The treasury holds one Bin per conserved Resource, empty, from world creation.</summary>
    /// <remarks>
    /// <b>Empty is asserted rather than assumed</b> (<c>adr/0116</c>): the treasury opens at zero and
    /// nothing authors a value, and it is an empty treasury that makes <c>02 §4.2</c>'s <em>pays whom
    /// it reaches and reports where it stopped</em> branch reachable at all.
    /// </remarks>
    [Fact]
    public void The_treasury_opens_with_one_empty_unbounded_bin_per_conserved_resource()
    {
        var world = new World(1_000, Taxing());

        int treasury = world.FindTreasuryBin(Coin);

        Assert.NotEqual(Rows.NoSlot, treasury);
        Assert.Equal(0, world.Bins.LevelAt(treasury));
        Assert.Equal(long.MaxValue, world.Bins.Capacity[treasury]);
        Assert.Equal(BinOwnerKind.Treasury, world.Bins.OwnerKind[treasury]);
    }

    /// <summary>
    /// <b>The task's own acceptance</b>: <c>global</c> resolves, a transfer executes, and money is
    /// conserved across it — the sum over both Bins is what it was before.
    /// </summary>
    [Fact]
    public void A_transfer_moves_money_to_the_treasury_and_conserves_it()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Taxing());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Coin)), 60, Ticks.Zero);

        int treasury = world.FindTreasuryBin(Coin);
        long before = Level(world, building, Coin) + world.Bins.LevelAt(treasury);

        StepToTheFiring(simulation);

        Assert.Equal(54, Level(world, building, Coin));
        Assert.Equal(6, world.Bins.LevelAt(treasury));
        Assert.Equal(before, Level(world, building, Coin) + world.Bins.LevelAt(treasury));
    }

    /// <summary>
    /// A Building with no money leaves the transfer waiting on its <b>own</b> Bin, not the treasury's.
    /// </summary>
    /// <remarks>
    /// <b>This is <c>adr/0114</c>'s reason for the whole decision, asserted.</b> A balance held in a
    /// column has no blame target and no wait list; here the Rule that could not pay names the Bin
    /// that stopped it, which is what makes <c>adr/0050</c>'s bankruptcy-versus-starvation diagnosis a
    /// mechanism rather than a paragraph.
    /// </remarks>
    [Fact]
    public void A_transfer_a_building_cannot_afford_waits_on_its_own_bin()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Taxing());

        StepToTheFiring(simulation);

        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[0]);
        Assert.Equal(0, world.Bins.LevelAt(world.FindTreasuryBin(Coin)));
        Assert.Equal(
            BinOf(world, building, Coin),
            world.Bins.Rows.Resolve(world.RuleInstances.WaitingOn[0]));
    }

    /// <summary>
    /// <c>global</c> naming a Resource that is not conserved says so, rather than resolving to nothing.
    /// </summary>
    /// <remarks>
    /// <b>The named hole moved rather than closing entirely.</b> <c>02 §4.3</c> is narrow about what
    /// <c>global</c> is for — the far end of an explicit money transfer — so a city-wide larder of
    /// Flour is a different mechanism that nothing has designed, and the refusal says which.
    /// </remarks>
    [Fact]
    public void A_global_term_on_a_good_says_the_treasury_holds_no_bin_for_it()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Scoped(Scope.Global));

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        Assert.Throws<InvalidOperationException>(() => StepToTheFiring(simulation));
    }

    /// <summary>
    /// The treasury's Bins survive a rebuild of every derived structure, which is what a load does.
    /// </summary>
    /// <remarks>
    /// <b>The walk branches on the owner KIND and not on whether the owner handle resolves.</b> A
    /// treasury Bin's handle is unset by design, so a <c>TryResolve</c> alone would drop it exactly as
    /// it drops a Bin whose Building is gone — and the treasury would quietly unlink itself on every
    /// load, with the money still in a row nothing could reach.
    /// </remarks>
    [Fact]
    public void The_treasury_keeps_its_bins_across_a_rebuild_of_derived_state()
    {
        (World world, Simulation simulation, _) = Built(Taxing());

        StepToTheFiring(simulation);

        ulong before = world.HashState();
        int treasury = world.FindTreasuryBin(Coin);

        world.RebuildDerived();

        Assert.Equal(treasury, world.FindTreasuryBin(Coin));
        Assert.Equal(before, world.HashState());
    }

    /// <summary>
    /// <b>The corpus's own worked example rescues its bakery from the District Pool</b>, so the hole is
    /// at evaluation and not at load. A loader that refused <c>02 §4.3</c> would not be a loader.
    /// </summary>
    [Fact]
    public void A_pool_ruleset_loads_and_only_fails_when_a_rule_reaches_for_it()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Scoped(Scope.Pool));

        Assert.Equal(1, world.Rules.RuleCount);

        // Nothing is due until Tick 1, so the world runs perfectly well right up to the reach.
        simulation.Step(TickInput.Empty);

        Assert.Equal(0, Level(world, building, Flour));
        Assert.Throws<NotSupportedException>(() => simulation.Step(TickInput.Empty));
    }

    // ---- the derived apply count ---------------------------------------------------------------------

    /// <summary>A Rule whose count is read off the Building rather than bounded by a band.</summary>
    private static Ruleset PerOccupant(int percent = 100) => new(
        resources: [ResourceFamily.Good],
        rules: [new RuleDefinition(
            Bakery, Rate, ApplyCount.From(new ReadoutId((ushort)Readout.Occupancy), percent),
            RuleId.None, false, default, ConditionId.None, 0, 1, 0, 0, 0, 0)],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    private static void House(World world, Handle<Building> building, int households)
    {
        for (int i = 0; i < households; i++)
        {
            world.CreateHousehold(building, lifeStage: 1);
        }
    }

    /// <summary><c>02 §4.1</c>'s proportionality without an expression language.</summary>
    [Fact]
    public void A_derived_apply_count_applies_once_per_unit_of_its_readout()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(PerOccupant());

        House(world, building, households: 4);
        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60 - (4 * 6), Level(world, building, Flour));
    }

    /// <summary>
    /// <c>02 §4.1</c>'s worked spelling — <em>"one unit applied <c>readout × 15 / 100</c> times"</em>.
    /// </summary>
    [Fact]
    public void A_derived_apply_count_scales_by_its_percentage_and_floors()
    {
        // 5 occupants at 50% is 2.5 applications, and a fraction of an application is not one.
        (World world, Simulation simulation, Handle<Building> building) = Built(PerOccupant(percent: 50));

        House(world, building, households: 5);
        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(60 - (2 * 6), Level(world, building, Flour));
    }

    /// <summary>
    /// <b>A derived zero is a success, and this is the test that stops it being a wait on nothing.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §4.1</c>: a Readout is not subscribable, so there is no Bin that could ever wake a
    /// zero-count Rule. It must re-arm on its rate like any success. Reading success as
    /// <c>Applications &gt; 0</c> would instead put it on a wait list against <c>Rows.NoSlot</c> — a
    /// Rule asleep on a Bin that does not exist.
    /// </remarks>
    [Fact]
    public void A_derived_count_of_zero_is_a_success_that_re_arms_and_waits_on_nothing()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(PerOccupant());

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        // No Households, so occupancy is zero.
        StepToTheFiring(simulation);

        Assert.Equal(60, Level(world, building, Flour));
        Assert.False(world.RuleInstances.IsWaiting(0));
        Assert.Equal(new Ticks(1 + Rate), world.RuleInstances.NextTick[0]);
    }

    /// <summary>
    /// A derived count is a band of one, so a Rule that cannot afford it **fails** rather than being
    /// served short — the fixed case's semantics, arrived at from the other side.
    /// </summary>
    [Fact]
    public void A_derived_rule_its_bins_cannot_afford_fails_rather_than_applying_fewer_times()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(PerOccupant());

        House(world, building, households: 4);
        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 12, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(12, Level(world, building, Flour));
        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[0]);

        Assert.Equal(
            4 * 6,
            RuleEngine.Requirement(
                world, 0, BinOf(world, building, Flour), Blocking.Supply));
    }

    /// <summary>An id the simulation does not declare throws rather than counting zero.</summary>
    [Fact]
    public void An_undeclared_readout_id_is_a_named_hole()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good],
            rules: [new RuleDefinition(
                Bakery, Rate, ApplyCount.From(new ReadoutId(9999)), RuleId.None, false, default,
                ConditionId.None, 0, 1, 0, 0, 0, 0)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (_, Simulation simulation, _) = Built(ruleset);

        Assert.Throws<InvalidOperationException>(() => StepToTheFiring(simulation));
    }

    /// <summary>A local term naming a Resource the Building kind declares no Bin for.</summary>
    [Fact]
    public void A_local_term_with_no_bin_to_address_throws_rather_than_failing_quietly()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules: [Rule(0, 1, 0, 0)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Bread), 6)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (_, Simulation simulation, _) = Built(ruleset);

        Assert.Throws<InvalidOperationException>(() => StepToTheFiring(simulation));
    }

    // ---- map emissions -------------------------------------------------------------------------------

    /// <summary>
    /// <c>map</c> is write-only, so it is outside the subscription question entirely: a Layer cell has
    /// no capacity to exceed, a map output cannot fail, and no Rule ever waits on one.
    /// </summary>
    [Fact]
    public void A_map_emission_reaches_the_cell_under_the_building()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good],
            rules: [Rule(0, 1, 0, 0, emissionFirst: 0, emissionCount: 1)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
            outputs: [],
            emissions: [new MapEmission(Layer.IndustrialPollution, 40)],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World world, Simulation simulation, Handle<Building> building) = Built(ruleset);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.Equal(40, world.Layers.PollutionSource(Cells.Zero, Cells.Zero));
    }

    /// <summary>Land value is chased towards a target and Sealing is a footprint; neither is emitted.</summary>
    [Fact]
    public void A_map_emission_to_a_layer_that_is_not_a_source_is_a_named_hole()
    {
        var ruleset = new Ruleset(
            resources: [ResourceFamily.Good],
            rules: [Rule(0, 1, 0, 0, emissionFirst: 0, emissionCount: 1)],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
            outputs: [],
            emissions: [new MapEmission(Layer.LandValue, 40)],
            bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World world, Simulation simulation, Handle<Building> building) = Built(ruleset);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 60, Ticks.Zero);

        Assert.Throws<NotSupportedException>(() => StepToTheFiring(simulation));
    }

    // ---- the run-level claims -------------------------------------------------------------------------

    /// <summary>
    /// The acceptance criterion, at a length a unit suite can carry: a session in which Rules fire,
    /// run twice, produces one hash trace.
    /// </summary>
    [Fact]
    public void Two_runs_of_a_session_in_which_rules_fire_agree_at_every_hash()
    {
        ulong[] first = Trace();
        ulong[] second = Trace();

        Assert.Equal(first, second);

        static ulong[] Trace()
        {
            (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

            var hashes = new ulong[64];

            for (int tick = 0; tick < hashes.Length; tick++)
            {
                if (tick % 4 == 0)
                {
                    world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 6, simulation.Tick);
                }

                if (tick % 3 == 0)
                {
                    int bread = BinOf(world, building, Bread);

                    if (world.Bins.LevelAt(bread) > 0)
                    {
                        world.Withdraw(world.Bins.Rows.At(bread), 1, simulation.Tick);
                    }
                }

                simulation.Step(TickInput.Empty);
                hashes[tick] = world.HashState();
            }

            simulation.CheckEndOfRun();

            return hashes;
        }
    }

    /// <summary>
    /// <c>adr/0006</c> through the Rule engine: a wait list that only grows is the collection with no
    /// sink, arriving by a route nobody would think to look down.
    /// </summary>
    [Fact]
    public void A_long_starved_run_does_not_grow_a_wait_list()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Baking());

        for (int tick = 0; tick < 4_096; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        simulation.CheckEndOfRun();

        Assert.Equal(1, world.RuleInstances.Rows.LiveCount);
        Assert.Equal(Blocking.Supply, world.RuleInstances.Blocked[0]);

        // Subscribed once and once only. A Rule that re-subscribed each time it was polled would
        // queue 4,096 times over this run and the count is what would say so.
        int waiting = 0;

        foreach (int _ in world.SupplyWaiters.Walk(BinOf(world, building, Flour)))
        {
            waiting++;
        }

        Assert.Equal(1, waiting);
    }
}
