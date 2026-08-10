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
/// Slice 8 task 1: a second Ruleset reaches a running World, through Phase 0, and the transition is
/// a property of the Tick rather than a command.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim being tested is <c>adr/0015</c>'s acceptance test</b>, which is one sentence —
/// *changing a production ratio and seeing the effect must take seconds* — and the reason it is a
/// test rather than a closing remark is that the ADR calls the risk project-survival: tuning that is
/// slow is tuning that stops happening, and a simulation nobody tunes is unbalanceable. Citybound's
/// 60–120 second warm rebuild is the recorded evidence.
/// </para>
/// <para>
/// <b>There is no new verb, and that is the finding this task's planning produced rather than a
/// convenience.</b> <c>Command</c> is 12 bytes and cannot carry a 64-bit hash;
/// <c>TickInput.RulesetHash</c> has been populated every Tick since slice 5 and read by nothing. So a
/// reload is a <em>transition</em> between Ticks, not an event inside one — which is also what
/// <c>InputLog.RulesetHashAt(tick)</c>'s signature had already promised by answering per Tick.
/// </para>
/// <para>
/// <b>Two refusals are load-bearing and are tested by name.</b> A transition this session cannot
/// resolve throws rather than running on, because <c>adr/0015</c>'s own revisit trigger names
/// <em>silently ignoring</em> as the failure mode. And a *structurally* different Ruleset is refused
/// until tasks 4–6 build the degradations, because half a migration applied during a balance session
/// is how a save gets quietly corrupted — which is the ADR's stated reason for the derelict flag
/// existing at all.
/// </para>
/// </remarks>
public sealed class RulesetReloadTests
{
    private const ulong HashA = 0x1111_1111_1111_1111UL;
    private const ulong HashB = 0x2222_2222_2222_2222UL;
    private const ulong HashC = 0x3333_3333_3333_3333UL;

    private const byte House = 1;
    private static readonly ResourceId Sundries = new(1);

    /// <summary>
    /// A kind with one Bin and one Rule that fills it from nothing, so the Rule always succeeds and
    /// its firing rate is the only thing deciding how much work a run does.
    /// </summary>
    /// <remarks>
    /// <b>A producer with no inputs, deliberately.</b> The number being tuned has to be observable
    /// without anything else being able to stop the Rule — a Rule that starved would confound a rate
    /// change with a supply change, which is precisely the ambiguity a designer tuning a production
    /// ratio is trying to resolve.
    /// </remarks>
    private static Ruleset Producing(uint rate, int capacity = 1_000_000) =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 0, 0, 1, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [],
            outputs: [new Term(new BinRef(Scope.Local, Sundries), 1)],
            emissions: [],
            bins: [new BinDeclaration(Sundries, BinCapacity.Of(capacity))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

    /// <summary>The same kind with a second one beside it — structurally different, so it is refused.</summary>
    private static Ruleset TwoKinds() =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, 8, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 0, 0, 1, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1), new KindDefinition(0, 0, 0, 0)],
            inputs: [],
            outputs: [new Term(new BinRef(Scope.Local, Sundries), 1)],
            emissions: [],
            bins: [new BinDeclaration(Sundries, BinCapacity.Of(1_000_000))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

    private static (World World, Simulation Simulation) Built(
        Ruleset opening, RulesetCatalogue catalogue, int houses = 4)
    {
        var world = new World(1_000, LayerRuleset.Default, opening);
        var simulation = new Simulation(world, WorldKey.FromSeed(0x8000_0001UL), catalogue);

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), zone: 1);
            world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);
        }

        return (world, simulation);
    }

    private static void Step(Simulation simulation, ulong hash, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(new TickInput(default, hash));
        }
    }

    /// <summary>
    /// The first Tick establishes what is in force; it does not swap.
    /// </summary>
    /// <remarks>
    /// A World is constructed with its opening Ruleset and the log's first Tick names it. Treating
    /// that as a transition would make every session that has ever run reload on Tick 0 — and with an
    /// empty catalogue, throw.
    /// </remarks>
    [Fact]
    public void The_first_tick_establishes_what_is_in_force_rather_than_reloading()
    {
        Ruleset opening = Producing(rate: 8);
        (_, Simulation simulation) = Built(opening, RulesetCatalogue.None);

        Step(simulation, HashA, ticks: 1);

        Assert.Equal(0, simulation.Reloads);
        Assert.Equal(HashA, simulation.RulesetInForce);
    }

    /// <summary>A session whose hash never moves never reloads, however long it runs.</summary>
    [Fact]
    public void A_session_with_no_transition_never_reloads()
    {
        Ruleset opening = Producing(rate: 8);
        (World world, Simulation simulation) = Built(opening, RulesetCatalogue.None);

        Step(simulation, HashA, ticks: 500);

        Assert.Equal(0, simulation.Reloads);
        Assert.Same(opening, world.Rules);
    }

    /// <summary>A Tick naming a different Ruleset puts it in force.</summary>
    [Fact]
    public void A_transition_puts_the_new_ruleset_in_force()
    {
        Ruleset opening = Producing(rate: 8);
        Ruleset faster = Producing(rate: 2);

        (World world, Simulation simulation) = Built(
            opening, RulesetCatalogue.Of([HashA, HashB], [opening, faster]));

        Step(simulation, HashA, ticks: 100);
        Assert.Same(opening, world.Rules);

        Step(simulation, HashB, ticks: 1);

        Assert.Same(faster, world.Rules);
        Assert.Equal(1, simulation.Reloads);
        Assert.Equal(HashB, simulation.RulesetInForce);
    }

    /// <summary>
    /// <b><c>adr/0015</c>'s acceptance test, as a measurement rather than a promise: changing a
    /// production ratio changes what the city does.</b>
    /// </summary>
    /// <remarks>
    /// <b>Firings rather than Bin levels, because a level is confounded by capacity.</b> The Rule
    /// produces one unit per firing into a Bin large enough never to fill, so the count of
    /// evaluations over a fixed window *is* the production rate. Quadrupling the rate must cut it to
    /// about a quarter — asserted as a band rather than exactly, because the arming stagger spreads
    /// the four Buildings over the new period and the window boundary falls where it falls.
    /// </remarks>
    [Fact]
    public void Changing_a_production_rate_changes_what_the_city_does()
    {
        Ruleset slow = Producing(rate: 32);
        Ruleset fast = Producing(rate: 8);

        (_, Simulation simulation) = Built(fast, RulesetCatalogue.Of([HashA, HashB], [fast, slow]));

        Step(simulation, HashA, ticks: 1_024);
        long busy = simulation.Rules.Drain().Evaluations.Sum;

        Step(simulation, HashB, ticks: 1_024);
        long quiet = simulation.Rules.Drain().Evaluations.Sum;

        Assert.True(busy > 0, "the fast Ruleset produced nothing, so there is nothing to compare.");

        // Four times the period, so about a quarter of the work. The band is wide because the point
        // is that the number reached the running city at all, not that it reached it to the firing.
        Assert.InRange(quiet, busy / 5, busy / 3);
    }

    /// <summary>
    /// The commands in the reloading Tick run under the <b>new</b> Rules, because a Tick has one
    /// Ruleset.
    /// </summary>
    /// <remarks>
    /// <b>Observable through Bin capacity, which is the one number a command's effect reads
    /// immediately.</b> <c>CreateBuilding</c> gives a Building its kind's Bins at the capacity the
    /// Ruleset declares, so a Building raised on the reloading Tick carries the new capacity if the
    /// swap ran first and the old one if it ran last. The alternative ordering is not absurd — apply
    /// the Tick's commands under the Rules they were issued against — but it would make a Tick's
    /// meaning depend on where in the Tick you asked, and <c>RulesetHashAt(tick)</c> answers per Tick.
    /// </remarks>
    [Fact]
    public void The_commands_in_the_reloading_tick_run_under_the_new_rules()
    {
        Ruleset small = Producing(rate: 8, capacity: 10);
        Ruleset large = Producing(rate: 8, capacity: 900);

        (World world, Simulation simulation) = Built(
            small, RulesetCatalogue.Of([HashA, HashB], [small, large]), houses: 0);

        Step(simulation, HashA, ticks: 1);

        // The Zone verb is the one this build has, so the Lot is painted in the reloading Tick and
        // the Building is raised straight after it, still inside the same Tick's Phase 0 aftermath.
        simulation.Step(new TickInput(
            [new Command(CommandKind.Zone, new Tiles(0), new Tiles(0), 1)], HashB));

        Handle<Lot> lot = world.Lots.Rows.At(0);
        Handle<Building> building = world.CreateBuilding(lot, House, simulation.Tick, simulation.Key);

        int slot = world.Bins.Rows.Resolve(
            world.Bins.Rows.At(world.BuildingBins.PeekFront(world.Buildings.Rows.Resolve(building))));

        Assert.Same(large, world.Rules);
        Assert.Equal(900, world.Bins.Capacity[slot]);
    }

    /// <summary>
    /// <b>A transition this session cannot resolve is refused, never ignored.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0015</c>'s revisit trigger names silent ignoring as its failure mode, and this is the
    /// shape it would arrive in: a log recording a reload, played by a runner that was handed one
    /// file, producing a run that diverges from the session it claims to reproduce while reporting
    /// success.
    /// </remarks>
    [Fact]
    public void A_transition_this_session_cannot_resolve_is_refused()
    {
        Ruleset opening = Producing(rate: 8);

        (_, Simulation simulation) = Built(
            opening, RulesetCatalogue.Fixed(HashA, opening));

        Step(simulation, HashA, ticks: 4);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => Step(simulation, HashC, ticks: 1));

        Assert.Contains("0x3333333333333333", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A session given no Rulesets at all meets a transition with the same refusal, rather than
    /// treating an empty catalogue as permission to ignore the hash.
    /// </summary>
    [Fact]
    public void A_session_given_no_rulesets_refuses_a_transition_rather_than_ignoring_it()
    {
        Ruleset opening = Producing(rate: 8);
        (_, Simulation simulation) = Built(opening, RulesetCatalogue.None);

        Step(simulation, HashA, ticks: 4);

        Assert.Throws<InvalidOperationException>(() => Step(simulation, HashB, ticks: 1));
    }

    /// <summary>
    /// <b>A structurally different Ruleset is refused until tasks 4–6 exist.</b>
    /// </summary>
    /// <remarks>
    /// The named hole, and it is named after the tasks that fill it. Applying half a migration during
    /// a balance session is how a save gets quietly corrupted — <c>adr/0015</c>'s own reason for the
    /// derelict flag — so a swap this build cannot perform correctly says so.
    /// </remarks>
    [Fact]
    public void A_structurally_different_ruleset_is_refused_until_the_degradations_exist()
    {
        Ruleset opening = Producing(rate: 8);
        Ruleset wider = TwoKinds();

        (World world, Simulation simulation) = Built(
            opening, RulesetCatalogue.Of([HashA, HashB], [opening, wider]));

        Step(simulation, HashA, ticks: 4);

        NotSupportedException failure = Assert.Throws<NotSupportedException>(
            () => Step(simulation, HashB, ticks: 1));

        Assert.Contains("KindCount", failure.Message, StringComparison.Ordinal);
        Assert.Contains("tasks 4-6", failure.Message, StringComparison.Ordinal);

        // And the previous Ruleset stays live, which is adr/0015's requirement and the property
        // RulesetInForce was built to make structural rather than remembered.
        Assert.Same(opening, world.Rules);
    }

    /// <summary>
    /// A catalogue whose opening entry is not the Ruleset the World holds is refused at construction.
    /// </summary>
    /// <remarks>
    /// It would make the first reload swap <em>away</em> from Rules the city had never been running —
    /// a divergence with no symptom, because both Rulesets load and both run.
    /// </remarks>
    [Fact]
    public void A_catalogue_that_does_not_open_with_the_worlds_ruleset_is_refused()
    {
        Ruleset opening = Producing(rate: 8);
        Ruleset other = Producing(rate: 2);

        var world = new World(1_000, LayerRuleset.Default, opening);

        Assert.Throws<ArgumentException>(() => new Simulation(
            world,
            WorldKey.FromSeed(0x8000_0001UL),
            RulesetCatalogue.Of([HashB, HashA], [other, opening])));
    }

    /// <summary>Two Rulesets under one content hash make a transition ambiguous.</summary>
    [Fact]
    public void A_catalogue_with_two_rulesets_under_one_hash_is_refused()
    {
        Assert.Throws<ArgumentException>(() => RulesetCatalogue.Of(
            [HashA, HashA], [Producing(rate: 8), Producing(rate: 2)]));
    }

    /// <summary>A catalogue is one hash per Ruleset, and the counts are checked rather than zipped.</summary>
    [Fact]
    public void A_catalogue_with_mismatched_lengths_is_refused()
    {
        Assert.Throws<ArgumentException>(() => RulesetCatalogue.Of(
            [HashA, HashB], [Producing(rate: 8)]));
    }
}
