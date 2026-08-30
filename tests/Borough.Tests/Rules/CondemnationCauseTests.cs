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
/// Milestone 6 task 2: the condition behind a demolition is copied into the trail before the
/// demolition frees the row holding it.
/// </summary>
/// <remarks>
/// <para>
/// <b>What makes these tests worth writing is the lifetime, not the copy.</b>
/// <see cref="World.DestroyBuilding"/> frees the Rule Instances that hold
/// <see cref="RuleInstanceTable.Reported"/> and the Building row that holds the kind, so every
/// assertion here is made against a world in which <b>nothing that knew the answer still exists</b> —
/// the Rule Instance table is empty and the Lot is vacant. A test that read the condition off a live
/// Rule Instance would pass without the mechanism.
/// </para>
/// <para>
/// <b>The fixture starves two Rules at different rates on purpose.</b> The condemnation predicate is
/// an <em>or</em> — any Rule past its kind's threshold settles it — but a trail entry names <em>one</em>
/// condition, and <c>ZoneRuleEngine</c>'s own remark says which one the design means: the Building's
/// pressure is the <b>longest</b> of its Rules', measured in missed firings. Until this task nothing
/// read that maximum, so nothing computed it. <b>The discriminating test runs the same city twice with
/// the two Rules declared in either order</b> and demands the same answer, because a mechanism that
/// picked the first Rule it met would pass one of those runs.
/// </para>
/// <para>
/// <b>The Zone Rule's interval is long deliberately.</b> A survey every few Ticks condemns on the
/// Tick the <em>faster</em> Rule crosses its threshold, when the slower one has not crossed its own
/// and there is only one candidate to choose between. Looking once, late, is what puts two qualifying
/// Rules in front of the choice.
/// </para>
/// </remarks>
public sealed class CondemnationCauseTests
{
    private static readonly ResourceId Repairs = new(1);
    private static readonly ResourceId Parts = new(2);

    /// <summary>What the slow Rule's chain reports. Never the answer: it is the lesser pressure.</summary>
    private static readonly ConditionId Disrepair = new(1);

    /// <summary>What the fast Rule's chain reports, and what every entry below should carry.</summary>
    private static readonly ConditionId Unsupplied = new(2);

    private const byte House = 1;
    private const byte HousingBit = 0;
    private const ushort Housing = 1 << HousingBit;

    private const uint SlowRate = 16;
    private const uint FastRate = 4;

    /// <summary>
    /// The condemnation threshold in <b>Ticks</b> — four missed firings of a
    /// <see cref="SlowRate"/> Rule, which is what this constant meant when the key was a firing
    /// count.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is denominated on the SLOW rate deliberately.</b> Milestone 17 made the threshold one
    /// wall clock for every Rule on a Building, where it used to be scaled by each Rule's own
    /// <c>rate</c>. Taking the slower Rule's old budget keeps every Rule in this fixture reaching the
    /// verdict, which is what the cause assertions need; taking the fast one's would condemn on the
    /// slow Rule's first missed firing and the trail would name a different condition.
    /// </remarks>
    private const int Condemn = 4 * (int)SlowRate;

    /// <summary>
    /// How often the Zone Rule looks. Long enough that both Rules are past their thresholds by the
    /// first survey, which is the whole point of the fixture.
    /// </summary>
    private const uint Survey = 128;

    /// <summary>
    /// A kind with two Rules drawing on two Bins nothing fills, each falling back to a chain that
    /// reports and stops.
    /// </summary>
    /// <remarks>
    /// <b>Both Bins are filled by nothing, which is the only way a Rule starves for ever</b>
    /// (<c>adr/0045</c>): a failed Rule subscribes to the Bin that stopped it and sleeps, so a Bin no
    /// Rule ever writes is a Rule that never wakes. The terminals are in <c>rules</c> and not in
    /// <c>kindRules</c> — a terminal is reached by walking a failed chain, never armed on its own rate.
    /// </remarks>
    /// <param name="fastFirst">
    /// Whether the fast Rule is declared before the slow one. The Building's Rule list is built from
    /// this order, and the mechanism under test must not depend on it.
    /// </param>
    private static Ruleset Failing(bool fastFirst) => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(House, SlowRate, ApplyCount.Band(1, 1), new RuleId(3),
                false, default, ConditionId.None, 0, 1, 0, 0, 0, 0),
            new RuleDefinition(House, FastRate, ApplyCount.Band(1, 1), new RuleId(4),
                false, default, ConditionId.None, 1, 1, 0, 0, 0, 0),
            new RuleDefinition(House, SlowRate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, Disrepair, 0, 0, 0, 0, 0, 0),
            new RuleDefinition(House, FastRate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, Unsupplied, 0, 0, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 2, 0, 2) { CondemnAfterTicks = Condemn, Occupants = 1 }],
        inputs:
        [
            new Term(new BinRef(Scope.Local, Repairs), 1),
            new Term(new BinRef(Scope.Local, Parts), 1),
        ],
        outputs: [],
        emissions: [],
        bins:
        [
            new BinDeclaration(Repairs, BinCapacity.Of(4)),
            new BinDeclaration(Parts, BinCapacity.Of(4)),
        ],
        kindRules: fastFirst ? [new RuleId(2), new RuleId(1)] : [new RuleId(1), new RuleId(2)],
        zoneRules: [Watching()]);

    /// <summary>A kind with one starving Rule and no <c>on_fail</c> chain, so it reports nothing.</summary>
    private static Ruleset Silent() => new(
        resources: [ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(House, FastRate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 1, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfterTicks = Condemn, Occupants = 1 }],
        inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
        kindRules: [new RuleId(1)],
        zoneRules: [Watching()]);

    /// <summary>
    /// A Zone Rule whose permission bit no Lot carries, so it condemns and never builds.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0055</c> used as a fixture</b>: a permission set scopes what a Zone Rule
    /// <em>builds</em> and never which Lots it looks at. With one Lot in the world the derived sample
    /// is one draw over one Lot, so the survey is exhaustive rather than probable and the run below
    /// has a single condemnation in it that nothing rebuilds over.
    /// </remarks>
    private static ZoneRuleDefinition Watching() =>
        new(House, HousingBit + 1, Survey, (int)Survey);

    /// <summary>One Lot, one Building, one Household, and a world about to lose all three.</summary>
    private static (World World, Simulation Simulation, Handle<Lot> Lot) Built(Ruleset ruleset)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0006UL))
        {
            VerifyDecideWritesNothing = true,
        };

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), Housing);
        Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

        world.CreateHousehold(building, lifeStage: 0);

        return (world, simulation, lot);
    }

    /// <summary>
    /// Steps until the Building is abandoned and returns the Tick it went on, which is the Tick the
    /// trail should carry.
    /// </summary>
    /// <remarks>
    /// The Tick is read <em>before</em> the step rather than after it, because the clock has moved on
    /// by the time the caller can see the result. Asserting the trail against a number this method
    /// computed from the same clock is the only version that could catch an off-by-one.
    /// <para>
    /// ⚠ <b>This waited on <c>Buildings.Rows.LiveCount == 0</c> until milestone 17 task 1</b>, which
    /// was the same question while condemnation freed the row. It is not any more: the shell stays
    /// standing on its Lot (<c>adr/0091</c>), so the table never empties and the old spelling would
    /// wait out its budget and report that the fixture had never condemned anything. ***The count it
    /// wants is Buildings still in use, and that is not a row count.***
    /// </para>
    /// </remarks>
    private static Ticks Fell(World world, Simulation simulation)
    {
        for (int i = 0; i < 4 * Survey; i++)
        {
            Ticks on = simulation.Tick;

            simulation.Step(TickInput.Empty);

            if (Standing(world) == 0)
            {
                return on;
            }
        }

        throw new InvalidOperationException(
            "the fixture never condemned its Building, so nothing here is testing the trail.");
    }

    /// <summary>
    /// Buildings that are live <em>and still in use</em> — the count that meant
    /// <c>Rows.LiveCount</c> before abandonment left the shell standing.
    /// </summary>
    private static int Standing(World world)
    {
        int standing = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Buildings.IsAbandoned(slot))
            {
                standing++;
            }
        }

        return standing;
    }

    /// <summary>
    /// A condemnation leaves one entry naming where it stood, what kind it was and when it went.
    /// </summary>
    [Fact]
    public void A_condemnation_is_recorded_with_its_lot_its_kind_and_its_tick()
    {
        (World world, Simulation simulation, Handle<Lot> lot) = Built(Failing(fastFirst: false));

        Ticks fell = Fell(world, simulation);
        CondemnationTrailTable trail = world.CondemnationTrail;

        Assert.Equal(1, trail.Count);
        Assert.Equal(1, trail.CondemnationsRecorded());

        int entry = trail.EntrySlot(0);

        Assert.Equal(lot, trail.Lot[entry]);
        Assert.Equal(House, trail.Kind[entry]);
        Assert.Equal(fell, trail.Tick[entry]);
        Assert.Equal(1, trail.Condemnations[entry]);
    }

    /// <summary>
    /// The condition is still readable after everything that held it has been freed, which is the
    /// claim the table exists for.
    /// </summary>
    [Fact]
    public void The_condition_outlives_the_rule_instance_that_reported_it()
    {
        (World world, Simulation simulation, Handle<Lot> lot) = Built(Failing(fastFirst: false));

        Fell(world, simulation);

        // Nothing that knew the answer is left: the Rule Instances are freed, so the condition the
        // trail reports cannot be re-derived from the city. This is the state 02 §9's question is
        // asked in.
        //
        // ⚠ THE SHELL IS STILL THERE, and this block asserted the opposite until milestone 17 task 1
        // -- "the Building row is freed, and the Lot is vacant". Abandonment leaves both standing
        // (adr/0091), so what is gone is the Rules and not the premises. ***The trail's job is
        // unchanged and is arguably plainer now***: the Building outlives the Rule Instance that
        // reported the condition, which is exactly the outliving this test is named for.
        Assert.Equal(0, world.RuleInstances.Rows.LiveCount);
        Assert.Equal(0, Standing(world));

        int abandoned = world.Lots.BuildingOn(world.Lots.Rows.Resolve(lot));

        Assert.NotEqual(Rows.NoSlot, abandoned);
        Assert.True(world.Buildings.IsAbandoned(abandoned));

        Assert.Equal(
            Unsupplied,
            world.CondemnationTrail.Condition[world.CondemnationTrail.EntrySlot(0)]);
    }

    /// <summary>
    /// The condition named is the worst-starved Rule's, whichever order the two are declared in.
    /// </summary>
    /// <remarks>
    /// <b>Both rows matter and neither is redundant.</b> A mechanism that took the first Rule it met
    /// past its threshold passes exactly one of them, and which one it passes is a property of how an
    /// intrusive list happens to link its head — so a single row would be a test that agreed with the
    /// defect half the time.
    /// </remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_worst_starved_rule_is_the_one_named(bool fastFirst)
    {
        (World world, Simulation simulation, Handle<Lot> _) = Built(Failing(fastFirst));

        Fell(world, simulation);
        CondemnationTrailTable trail = world.CondemnationTrail;

        // The fast Rule has missed roughly four times as many firings as the slow one by the first
        // survey, and both are past the threshold, so the choice is between two qualifying causes.
        Assert.Equal(Unsupplied, trail.Condition[trail.EntrySlot(0)]);
    }

    /// <summary>
    /// A Building whose author wrote no <c>on_fail</c> chain is recorded anyway, with no condition.
    /// </summary>
    /// <remarks>
    /// <b>This is the case the trail refuses to filter</b>, and it is the shipped Rulesets' case:
    /// <c>minimal.toml</c>'s <c>upkeep</c> has no chain and says so in its own comment. A Building
    /// that vanished with no entry at all is the worse answer, because the player is then told
    /// nothing rather than told that nobody wrote a reason down.
    /// </remarks>
    [Fact]
    public void A_condemnation_with_no_chain_behind_it_is_still_recorded()
    {
        (World world, Simulation simulation, Handle<Lot> lot) = Built(Silent());

        Ticks fell = Fell(world, simulation);
        CondemnationTrailTable trail = world.CondemnationTrail;

        Assert.Equal(1, trail.Count);
        Assert.Equal(ConditionId.None, trail.Condition[trail.EntrySlot(0)]);
        Assert.Equal(lot, trail.Lot[trail.EntrySlot(0)]);
        Assert.Equal(fell, trail.Tick[trail.EntrySlot(0)]);
    }

    /// <summary>A city that is not declining writes nothing, so the trail is not a Tick counter.</summary>
    [Fact]
    public void A_building_that_is_never_condemned_leaves_no_entry()
    {
        var world = new World(1_000, Immortal());
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0006UL))
        {
            VerifyDecideWritesNothing = true,
        };

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), Housing);

        world.CreateHousehold(world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key), 0);

        for (int i = 0; i < 4 * Survey; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(1, world.Buildings.Rows.LiveCount);
        Assert.Equal(0, world.CondemnationTrail.Count);
        Assert.Equal(0, world.CondemnationTrail.CondemnationsRecorded());
    }

    /// <summary>The same starving kind, with no threshold, so it declines for ever and never falls.</summary>
    private static Ruleset Immortal() => new(
        resources: [ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(House, FastRate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 1, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1) { Occupants = 1 }],
        inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
        kindRules: [new RuleId(1)],
        zoneRules: [Watching()]);
}
