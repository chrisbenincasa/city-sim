using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Session N1 task 1: <c>adr/0033</c>'s second required mitigation, and the trickle it exists to catch.
/// </summary>
/// <remarks>
/// <para>
/// <b>These tests are the fixture two decisions share.</b> <c>adr/0033</c> names two mitigations for
/// subscription's silent failure mode and calls them <em>both required</em>; the first is in place
/// (a Bin has no assignable level) and the second — <em>no Rule is asleep with all inputs
/// satisfiable</em> — was specified in three documents and built in none until now. What made it
/// unbuilt for that long is that <b>nothing in the shipped content can violate it</b>: producing a
/// violation needs a Bin filled in instalments smaller than a waiter's requirement, and under
/// <c>local</c> scope no two Rules share a Bin they do not both own.
/// </para>
/// <para>
/// <b>The trickle below is written by hand, and it is <c>adr/0063</c>'s acceptance test.</b> It was
/// committed first as a test of the <em>defect</em> — asserting the waiter stayed asleep and the
/// invariant fired — and inverted by the commit that made the drain's budget the Bin's level and the
/// requirement derived. Both readings are kept in the remarks below, because what changed is the
/// answer and not the situation: the level still reads 3 at the moment of the assertion, and that is
/// the fact the whole argument rests on.
/// </para>
/// <para>
/// <b>It needs no <c>pool</c>, which is why it is the acceptance test and the invariant is not.</b>
/// Three <c>Deposit(1)</c> calls against a waiter requiring 3 reproduce the trickle on <c>local</c>
/// scope alone.
/// </para>
/// </remarks>
public sealed class BinWaitListTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);

    private const int Kind = 1;
    private const int Rate = 8;

    /// <summary>
    /// A consumer that needs three flour to run at all: three applications of one flour each.
    /// </summary>
    /// <remarks>
    /// <b>Three <em>applications</em> of one rather than one application of three, because that is the
    /// shape the shipped Ruleset has.</b> <c>rulesets/minimal.toml</c>'s <c>consume</c> is
    /// <c>apply = { derived = "occupancy" }</c> against three Households, so its floor is three and its
    /// term is one — and a derived band is a band of one, floor and ceiling together, so it cannot be
    /// served short. Spelling it as a fixed band of three keeps the arithmetic identical without
    /// needing a Readout, and <c>floor × |net| = 3</c> either way.
    /// </remarks>
    private static Ruleset Consuming() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(
                Kind, Rate, ApplyCount.Band(3, 3), RuleId.None, false, default, ConditionId.None,
                0, 1, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 1)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(12))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>
    /// A producer that needs three units of space to run at all, and no inputs.
    /// </summary>
    /// <remarks>
    /// The mirror of <see cref="Consuming"/> on the other wait list. It is not a hypothetical shape:
    /// <c>minimal.toml</c>'s <c>restock</c> is exactly this with <c>min = 1</c> and <c>amount = 1</c>,
    /// and <b>that is the only reason the shipped Ruleset never trips this</b> — a producer whose
    /// deficit is the smallest quantity the engine can express is covered by any withdrawal at all.
    /// Raise either number and the defect is on the space list too.
    /// </remarks>
    private static Ruleset Producing() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(
                Kind, Rate, ApplyCount.Band(3, 3), RuleId.None, false, default, ConditionId.None,
                0, 0, 0, 1, 0, 0),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [],
        outputs: [new Term(new BinRef(Scope.Local, Bread), 1)],
        emissions: [],
        bins: [new BinDeclaration(Bread, BinCapacity.Of(12))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    /// <summary>A world with one Building, its declared Bins, and its Rule armed for Tick 1.</summary>
    private static (World World, Simulation Simulation, Handle<Building> Building) Built(
        Ruleset ruleset)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(1));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Kind);

        foreach (BinDeclaration bin in ruleset.BinsOf(Kind))
        {
            world.CreateBin(building, bin.Resource);
        }

        foreach (RuleId rule in ruleset.RulesOf(Kind))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay: 1);
        }

        return (world, simulation, building);
    }

    private static int BinOf(World world, Handle<Building> building, ResourceId resource) =>
        world.FindBin(world.Buildings.Rows.Resolve(building), resource);

    /// <summary>Runs to the end of Tick 1, which is the Tick the helper arms for.</summary>
    private static void StepToTheFiring(Simulation simulation)
    {
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);
    }

    private static bool IsWaiting(World world, Handle<Building> building)
    {
        int instance = world.BuildingRules.PeekFront(world.Buildings.Rows.Resolve(building));

        return world.RuleInstances.IsWaiting(instance);
    }

    // ---- the invariant is silent where the city is merely poor -------------------------------------

    /// <summary>
    /// A waiter starved of everything violates nothing, which is most of why this check is affordable
    /// to state at all.
    /// </summary>
    /// <remarks>
    /// <b>This is the state <c>rulesets/minimal.toml</c> is permanently in.</b> Its <c>upkeep</c> Rule
    /// draws on a Resource the file deliberately never produces, so its Bin is written by nothing and
    /// its input is <em>genuinely</em> unsatisfiable for ever. A check that fired here would fire on
    /// every long-run test in the suite, and the honest reading of a Rule asleep on an empty Bin is
    /// that the city is short rather than that the engine is broken.
    /// </remarks>
    [Fact]
    public void A_waiter_starved_of_everything_violates_nothing()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Consuming());

        StepToTheFiring(simulation);

        Assert.True(IsWaiting(world, building));

        simulation.CheckEndOfRun();
    }

    /// <summary>Supply that covers the requirement in one write wakes the waiter, so nothing is asleep.</summary>
    [Fact]
    public void Supply_arriving_in_one_write_wakes_the_waiter()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Consuming());

        StepToTheFiring(simulation);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building, Flour)), 3, simulation.Tick);

        Assert.False(IsWaiting(world, building));

        simulation.CheckEndOfRun();
    }

    // ---- the trickle, which is the defect ---------------------------------------------------------

    /// <summary>
    /// Three arrivals of one wake a waiter that needs three, because the budget is what the Bin holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Goods are already in the Bin, and that was true before the fix too.</b>
    /// <c>World.Deposit</c> raises the level and only then drains, so nothing was ever being withheld:
    /// the level read 3, the waiter needed 3, and it slept anyway, because the drain's budget was
    /// <em>the arriving delta</em>. A requirement coarser than the granularity of supply was
    /// unreachable however much accumulated.
    /// </para>
    /// <para>
    /// <b>The third arrival is the one that wakes it, which is the assertion that matters.</b> Under
    /// <c>adr/0063</c> the budget is the level, so the drain covers a requirement of 3 the moment the
    /// Bin holds 3 — and <see cref="Invariant.WaiterIsBlockedByTheBinItNames"/>, which reported this
    /// state as a violation before the fix, now has nothing to report.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_trickle_wakes_a_waiter_once_the_bin_holds_what_it_needs()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Consuming());

        StepToTheFiring(simulation);

        int flour = BinOf(world, building, Flour);
        Handle<Bin> handle = world.Bins.Rows.At(flour);

        world.Deposit(handle, 1, simulation.Tick);
        world.Deposit(handle, 1, simulation.Tick);

        // Two of the three, so the waiter is short and the old and new predicates agree here.
        Assert.True(IsWaiting(world, building));

        world.Deposit(handle, 1, simulation.Tick);

        // The fact the argument rests on, and the one line here adr/0063 did not move.
        Assert.Equal(3, world.Bins.LevelAt(flour));

        Assert.False(IsWaiting(world, building));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// The space mirror, which is the half the shipped Ruleset actually runs on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Withdrawals of one against a producer needing three units of space.</b> The Bin ends with
    /// room for three and the producer wakes on the third withdrawal, exactly as the level case wakes
    /// on the third deposit — <c>a Withdraw drains the space list exactly as a Deposit drains the
    /// level list</c> (<c>02 §4.2</c>), and the budget on this side is <c>SpaceAt</c>.
    /// </para>
    /// <para>
    /// <b>This is the half that mattered for <c>minimal.toml</c>'s credibility.</b> Its steady state
    /// survived only because <c>restock</c>'s deficit is one, the smallest quantity expressible, so any
    /// withdrawal covered it — and <c>rulesets/minimal-tuned.toml</c>, which the golden session reloads
    /// into at Tick 128, raises that output amount to 2 and broke it. The committed trace held a
    /// <c>restock</c> asleep on space 3 until this fix landed.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_drip_of_withdrawals_wakes_a_producer_once_the_bin_has_the_space()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Producing());

        int bread = BinOf(world, building, Bread);
        Handle<Bin> handle = world.Bins.Rows.At(bread);

        world.Deposit(handle, 12, Ticks.Zero);

        StepToTheFiring(simulation);

        Assert.True(IsWaiting(world, building));

        world.Withdraw(handle, 1, simulation.Tick);
        world.Withdraw(handle, 1, simulation.Tick);

        Assert.True(IsWaiting(world, building));

        world.Withdraw(handle, 1, simulation.Tick);

        Assert.Equal(3, world.Bins.SpaceAt(bread));

        Assert.False(IsWaiting(world, building));

        simulation.CheckEndOfRun();
    }

    // ---- the derivation reads the Ruleset in force, not the one the waiter failed under -----------

    /// <summary>
    /// The check derives what a waiter needs rather than trusting what it recorded, which is the other
    /// half of <c>adr/0063</c>.
    /// </summary>
    /// <remarks>
    /// <b>A stored shortfall is a deficit at an instant, and nothing re-derives it.</b> Here the
    /// requirement is met exactly and the recorded number is irrelevant to the answer:
    /// <see cref="RuleEngine.BinStillBlocks"/> reads <c>floor × |net|</c> off the Ruleset in force and
    /// compares it against the Bin now. That is what makes the same check catch a tuning reload that
    /// halved a Rule's input and never woke the Building starving for the old amount — <c>plans/0015</c>
    /// finding 3, which needs no separate mechanism.
    /// </remarks>
    [Fact]
    public void The_requirement_is_derived_from_the_ruleset_rather_than_from_the_subscription()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Consuming());

        StepToTheFiring(simulation);

        int instance = world.BuildingRules.PeekFront(world.Buildings.Rows.Resolve(building));
        int flour = BinOf(world, building, Flour);

        Assert.True(RuleEngine.BinStillBlocks(world, instance, flour, Blocking.Supply));

        world.Bins.Move(flour, 3);

        Assert.False(RuleEngine.BinStillBlocks(world, instance, flour, Blocking.Supply));
    }

    /// <summary>A Bin the Rule only fills cannot be short of level for it, at any apply count.</summary>
    [Fact]
    public void A_bin_a_rule_only_fills_never_blocks_it_on_level()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Producing());

        StepToTheFiring(simulation);

        int instance = world.BuildingRules.PeekFront(world.Buildings.Rows.Resolve(building));
        int bread = BinOf(world, building, Bread);

        Assert.False(RuleEngine.BinStillBlocks(world, instance, bread, Blocking.Supply));
    }
}
