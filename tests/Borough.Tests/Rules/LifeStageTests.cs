using Borough.Core.Invariants;
using Borough.Core.Tables;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0046</c> stages 1 and 2: a Household's Life Stage advances, and a terminal stage ends it.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The city empties, and that is the milestone rather than a defect.</b> <c>adr/0011</c> gives
/// the stage table two <em>decisions</em> — how many children, and whether to dissolve — and only the
/// second is here. So there is a sink and no source, and the assertions below say so out loud: run
/// long enough and the population reaches <b>zero</b>. ***A source without a sink is
/// <c>adr/0006</c>; a sink without a source merely empties*** — and an emptying city is bounded below
/// by zero, which is why dissolution ships first. Stage 3 is what stops the fall.
/// </para>
/// <para>
/// ⚠ <b>These tests were rewritten rather than extended when stage 2 landed.</b> The stage-1 versions
/// asserted a <em>flat</em> population, and that assertion was correct for exactly one commit. It is
/// recorded here because the shape recurs: a test written to pin a half-built mechanism has to be
/// read as a claim about the stage rather than about the city, or it becomes an argument against
/// finishing the work.
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

    /// <summary>A stage advances, the chain is walked end to end, and the terminal stages fill.</summary>
    /// <remarks>
    /// <b>160 Days is past the transient chain's floor of 120 and past its ceiling of 157</b>
    /// (<c>31 + 63 + 63</c>), so a city still holding a transient stage by then has a countdown that
    /// is not firing. ⚠ <b>The ceiling was written down as 156 first and the measured city held 8
    /// Households in <c>mature_family</c> at Day 150</b> — an off-by-one in a sum of three windows,
    /// caught by the instrument rather than by the arithmetic. ⚠ <b>It is deliberately SHORT of the
    /// terminal stages' own countdowns</b>, which is what leaves anything alive to count: the city is
    /// gone by Day 210 and this test would then be asserting against an empty table, which
    /// <see cref="Nothing_is_born_so_the_city_empties"/> is for.
    /// </remarks>
    [Fact]
    public void The_chain_is_walked_end_to_end_and_the_transient_stages_empty()
    {
        (World world, Simulation simulation) = City(2_000);

        int[] before = Histogram(world);

        Assert.All(Transient, stage => Assert.True(before[stage] > 0));

        Run(simulation, days: 160);

        int[] after = Histogram(world);

        Assert.Equal(0, after[1]);
        Assert.Equal(0, after[2]);
        Assert.Equal(0, after[3]);

        // childless is declared and UNREACHABLE -- nothing routes into it, because the only thing
        // that would is stage 3's fertility decision. Under stage 1 it held exactly what it was
        // seeded with; under stage 2 it DRAINS, because it is terminal and a terminal stage
        // dissolves. So the claim is that it never GREW, which is what a chain wired wrong would
        // have done.
        Assert.True(after[4] < before[4]);

        // Everything that had anywhere to go arrived at the terminal stage, less whatever has
        // already dissolved out of it. The inequality is the honest form: an equality here would be
        // asserting that nothing died, which is the stage-1 claim.
        Assert.True(after[5] > 0);
        Assert.True(after[5] <= before[1] + before[2] + before[3] + before[5]);
    }

    /// <summary>
    /// 🔴 <b>The city empties, and that is the milestone rather than a side condition.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>240 Days is past the longest life the stage table can draw.</b> A Household seeded into
    /// <c>young</c> lives at most <c>31 + 63 + 63 + 55 = 212</c> Days; every other seeding is
    /// shorter. So a city still holding anybody at 240 has a countdown that is not firing, and the
    /// assertion is an exact zero rather than a decline.
    /// </para>
    /// <para>
    /// ⚠ <b>The Citizens go with the Households and nothing here asks them to.</b>
    /// <see cref="World.DestroyHousehold"/> retires every member through
    /// <c>DestroyCitizen</c> — one implementation, which is <c>plans/0035</c> <b>F29</b>'s repair —
    /// so a Citizen table that did not empty alongside would mean members were being orphaned rather
    /// than retired.
    /// </para>
    /// </remarks>
    [Fact]
    public void Nothing_is_born_so_the_city_empties()
    {
        (World world, Simulation simulation) = City(2_000);

        Assert.True(world.Households.Rows.LiveCount > 0);
        Assert.True(world.Citizens.Rows.LiveCount > 0);

        Run(simulation, days: 240);

        Assert.Equal(0, world.Households.Rows.LiveCount);
        Assert.Equal(0, world.Citizens.Rows.LiveCount);

        world.Invariants.RunEndOfRun(world);
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

        Assert.False(simulation.LastLifeStages.Ran);
    }

    /// <summary>🔴 <b>The estate goes to the treasury, and the money supply does not move.</b></summary>
    /// <remarks>
    /// <para>
    /// <b>A hand-funded fixture rather than a shipped world, and that is the honest form.</b> Every
    /// Household on <c>aged.toml</c> holds exactly zero — it is <c>minimal.toml</c> underneath, and
    /// <c>CLAUDE.md</c> says so of that file — so a run of it exercises the transfer's <em>guard</em>
    /// and never its body. ⚠ <b>A world in which the estate is non-zero would be
    /// <c>taxed.toml</c> plus the stage table</b>, and it is not shipped yet: the demonstration is
    /// owed, and until it exists this test is the only thing that watches the money move.
    /// </para>
    /// <para>
    /// <b><see cref="World.Endow"/> is the door money enters by</b>, so the fixture uses it rather
    /// than depositing into the Bin directly. A direct deposit would raise the ledger without raising
    /// <c>MoneySupplyTable.Issued</c>, and this test would then be asserting against a world that was
    /// already failing <see cref="Invariant.MoneyIsConserved"/> before the estate went anywhere.
    /// </para>
    /// <para>
    /// ⚠ <b>The conservation check is the load-bearing half and it is nearly free.</b>
    /// <c>World.DestroyHousehold</c> frees every Bin the Household owned <em>including its
    /// balance</em>, and its own remark says the money in it is destroyed. So a dissolution that
    /// forgot the transfer would leave the ledger short and
    /// <see cref="Invariant.MoneyIsConserved"/> — an exact equality — would name the gap.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_estate_goes_to_the_treasury()
    {
        (World world, Simulation simulation) = City(2_000);

        Assert.True(world.TryMoneyResource(out ResourceId money));

        int treasury = world.FindTreasuryBin(money);

        Assert.NotEqual(Rows.NoSlot, treasury);

        var endowed = new Money(500);
        long estates = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                world.Endow(world.Households.Rows.At(slot), endowed);
                estates += endowed.Raw;
            }
        }

        long opening = world.Bins.LevelAt(treasury);
        long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        Assert.True(estates > 0);

        Run(simulation, days: 240);

        Assert.Equal(0, world.Households.Rows.LiveCount);

        // Every penny, and in the one place left that can hold it.
        Assert.Equal(opening + estates, world.Bins.LevelAt(treasury));

        // The supply is FLAT. A death is not an emigration: nothing left the city, so unlike
        // World.Depart there is no decrement to make and making one would be the leak.
        Assert.Equal(issued, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A stage the Ruleset no longer declares is dropped off the wheel, never dissolved.</summary>
    /// <remarks>
    /// <b>The two outcomes are counted apart because they read identically in a histogram.</b> A hot
    /// reload that shortens the stage table (<c>adr/0015</c>) strands every Household standing in a
    /// deleted row, and the wheel has to let go of them — but letting go must not kill them.
    /// ⚠ <b>The stage id is written by hand here</b>: no shipped Ruleset produces this, which is
    /// exactly why nothing else would catch it.
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>It asserts on the SURVIVORS and not on <c>LastLifeStages.Dropped</c>, deliberately.</b>
    /// That counter is a <em>flow</em> and it is written on the Day's first Tick only, so a
    /// <c>Run(days: 1)</c> ends 2,047 Ticks later with the reading back at <c>default</c> — the same
    /// trap that made <c>--stages</c> print a moving histogram beside a column of zeros. ***A level
    /// tolerates being sampled a Tick late and a flow does not.***
    /// </remarks>
    [Fact]
    public void A_stage_the_ruleset_no_longer_declares_is_dropped_and_not_dissolved()
    {
        (World world, Simulation simulation) = City(2_000);

        int stranded = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot) && world.Households.LifeStage[slot] == 1)
            {
                world.Households.LifeStage[slot] = (byte)(world.Rules.LifeStageCount + 1);
                stranded++;
            }
        }

        Assert.True(stranded > 0);

        // 240 Days is past the longest life the table can draw, so everything with a real stage has
        // dissolved and the survivors are exactly the stranded.
        Run(simulation, days: 240);

        Assert.Equal(stranded, world.Households.Rows.LiveCount);

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                Assert.Equal(world.Rules.LifeStageCount + 1, world.Households.LifeStage[slot]);
            }
        }

        world.Invariants.RunEndOfRun(world);
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
