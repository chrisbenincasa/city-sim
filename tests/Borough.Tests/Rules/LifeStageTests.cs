using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0046</c> stage 1: a Household's Life Stage advances, and nothing else happens.
/// </summary>
/// <remarks>
/// <para>
/// <b>The negative half is the milestone.</b> <c>adr/0011</c> gives the stage table two
/// <em>decisions</em> — how many children, and when to dissolve — and both are later stages. So the
/// assertions below are as much about what does <em>not</em> move: the population is flat, no Citizen
/// is created or destroyed, and no money changes hands. ***A source without a sink is
/// <c>adr/0006</c>; a sink without a source merely empties*** — which is why dissolution ships before
/// generation and why neither is here.
/// </para>
/// <para>
/// <b><c>rulesets/aged.toml</c> is the world</b>, and it is <c>minimal.toml</c> with five
/// <c>[[life_stage]]</c> tables and nothing else changed — so anything these tests observe that
/// <c>minimal.toml</c> does not is the stage table's.
/// </para>
/// </remarks>
public sealed class LifeStageTests
{
    /// <summary>The three stages aged.toml routes anything out of.</summary>
    private static readonly int[] Transient = [1, 2, 3];

    private static string Aged =>
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "aged.toml");

    private static (World World, Simulation Simulation) City(int citizens)
    {
        Ruleset rules = RulesetLoader.Load(Aged).Ruleset
            ?? throw new InvalidOperationException("rulesets/aged.toml did not load.");

        var key = WorldKey.FromSeed(0);
        World world = new(citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    private static int[] Histogram(World world)
    {
        var tally = new int[world.Rules.LifeStageCount + 1];

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                tally[world.Households.LifeStage[slot]]++;
            }
        }

        return tally;
    }

    private static void Run(Simulation simulation, int days)
    {
        for (int tick = 0; tick < days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }
    }

    /// <summary>The chain the file authors is the chain the loader read.</summary>
    /// <remarks>
    /// <b>The successor is authored and not taken from declaration order</b>, and this is where that
    /// is checked: <c>childless</c> is declared fourth and <c>mature_family</c> exits past it to
    /// <c>empty_nest</c>. An order-derived chain would route the third stage into the fourth and
    /// would be wrong in a way no refusal could catch.
    /// </remarks>
    [Fact]
    public void The_successor_is_authored_rather_than_taken_from_declaration_order()
    {
        (World world, _) = City(2_000);
        Ruleset rules = world.Rules;

        Assert.Equal(5, rules.LifeStageCount);

        Assert.Equal(2, rules.LifeStage(1).NextStage);
        Assert.Equal(3, rules.LifeStage(2).NextStage);

        // The one that matters: third exits to FIFTH, over the top of the fourth.
        Assert.Equal(5, rules.LifeStage(3).NextStage);

        Assert.Equal(0, rules.LifeStage(4).NextStage);
        Assert.Equal(0, rules.LifeStage(5).NextStage);
    }

    /// <summary>Every Household is on the wheel at creation, and none carries stage zero.</summary>
    /// <remarks>
    /// <b>Zero means <em>this world has no demographics</em> and not <em>stage zero</em></b>, which is
    /// what <c>SyntheticCity</c> was getting wrong: it cycled <c>i % 5</c>, so a fifth of every city
    /// carried a stage id no Ruleset resolves and would never have been armed.
    /// </remarks>
    [Fact]
    public void A_world_with_stages_arms_every_household_and_leaves_none_stageless()
    {
        (World world, _) = City(2_000);

        Assert.Equal(0, Histogram(world)[0]);

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            // Armed strictly ahead: a countdown due today would land in the bucket being drained.
            Assert.True(world.Households.NextStageDay[slot] >= 1);
            Assert.True(world.Households.NextStageDay[slot] < EventWheel.CoarseDays);
        }
    }

    /// <summary>A stage advances, and the whole chain is walked end to end.</summary>
    /// <remarks>
    /// <b>200 Days is past the chain's floor of 160 and past its ceiling</b>, so a city that has not
    /// emptied its transient stages by then has a countdown that is not firing.
    /// </remarks>
    [Fact]
    public void The_chain_is_walked_end_to_end_and_the_transient_stages_empty()
    {
        (World world, Simulation simulation) = City(2_000);

        int[] before = Histogram(world);

        Assert.All(Transient, stage => Assert.True(before[stage] > 0));

        Run(simulation, days: 200);

        int[] after = Histogram(world);

        Assert.Equal(0, after[1]);
        Assert.Equal(0, after[2]);
        Assert.Equal(0, after[3]);

        // childless is declared and UNREACHABLE under stage 1 -- nothing routes into it, because the
        // only thing that would is stage 3's fertility decision. So it holds exactly what it was
        // seeded with, and a test that found it growing would have found a chain wired wrong.
        Assert.Equal(before[4], after[4]);

        // Everything that had anywhere to go ended up in the terminal stage.
        Assert.Equal(before[1] + before[2] + before[3] + before[5], after[5]);
    }

    /// <summary>
    /// 🔴 <b>The population is flat, and that is the milestone rather than a side condition.</b>
    /// </summary>
    /// <remarks>
    /// Stage 1 advances a stage and does nothing else. A Household that dissolved here would be
    /// stage 2 arriving early, and one that spawned would be stage 3 — and either would be a sink or
    /// a source landing without the other, which is the ordering <c>plans/0046</c> calls the safety
    /// property.
    /// </remarks>
    [Fact]
    public void Nothing_is_born_and_nothing_dies()
    {
        (World world, Simulation simulation) = City(2_000);

        int households = world.Households.Rows.LiveCount;
        int citizens = world.Citizens.Rows.LiveCount;

        Run(simulation, days: 200);

        Assert.Equal(households, world.Households.Rows.LiveCount);
        Assert.Equal(citizens, world.Citizens.Rows.LiveCount);
    }

    /// <summary>Every transition is drawn inside its stage's own window.</summary>
    /// <remarks>
    /// <b><c>adr/0011</c>'s <c>[N, N+W)</c>, checked at the boundaries rather than on the mean.</b> A
    /// draw that ignored the floor would show up as a stage emptying early; one that ignored the
    /// window would put the whole cohort on one Day. The strict bound below is the second: a spread
    /// of 8 that produced 8 distinct Days is the window doing its job.
    /// </remarks>
    [Fact]
    public void A_countdown_is_drawn_inside_its_stages_window()
    {
        (World world, Simulation simulation) = City(2_000);

        var leftYoung = new HashSet<int>();
        int seeded = Histogram(world)[1];

        for (int day = 1; day <= 40; day++)
        {
            Run(simulation, days: 1);

            if (Histogram(world)[1] != seeded)
            {
                leftYoung.Add(day);
                seeded = Histogram(world)[1];
            }
        }

        LifeStageDefinition young = world.Rules.LifeStage(1);

        Assert.NotEmpty(leftYoung);
        Assert.True(leftYoung.Min() >= young.DurationDays);
        Assert.True(leftYoung.Max() <= young.DurationDays + young.SpreadDays);

        // The window is the load-bearing half: a cohort leaving on one Day is the lockstep world
        // spread_days exists to prevent.
        Assert.True(leftYoung.Count > 1);
    }

    /// <summary>A Ruleset with no stage table has no demographics and pays nothing for it.</summary>
    /// <remarks>
    /// <b><c>[[hinterland]]</c>'s precedent, and it is what keeps this milestone off thirteen
    /// standing baselines.</b> A mechanism arrives as a table a Ruleset may state, never as a default
    /// every world inherits.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_stage_table_advances_nothing()
    {
        Ruleset rules = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;

        var key = WorldKey.FromSeed(0);
        World world = new(2_000, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Assert.False(rules.DeclaresLifeStages);

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                Assert.Equal(0, world.Households.LifeStage[slot]);
            }
        }

        Run(simulation, days: 10);

        Assert.Equal(0, simulation.LastLifeStages.Advanced);
        Assert.Equal(0, simulation.LastLifeStages.Retired);
    }

    /// <summary>The invariants hold across a whole chain's worth of transitions.</summary>
    [Fact]
    public void The_world_is_consistent_after_a_full_chain()
    {
        (World world, Simulation simulation) = City(2_000);

        Run(simulation, days: 200);

        world.Invariants.RunEndOfRun(world);
    }
}
