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

    /// <summary>Runs one Day and returns what the midnight sweep did.</summary>
    /// <remarks>
    /// ⚠ <b>The reading is taken on the FIRST Tick and never after the loop.</b>
    /// <see cref="LifeStageReading"/> is a <em>flow</em>, written only at midnight, so a caller that
    /// steps a whole Day and then reads <c>LastLifeStages</c> gets <c>default</c> — the trap that
    /// made <c>--stages</c> print a moving histogram beside a transition column of zeros, and that
    /// cost this class a test at stage 2. ***A level tolerates being sampled a Tick late; a flow does
    /// not.***
    /// </remarks>
    private static LifeStageReading RunDay(Simulation simulation)
    {
        simulation.Step(default);

        LifeStageReading midnight = simulation.LastLifeStages;

        for (int tick = 1; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        return midnight;
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

        // Past `young`'s ceiling of 31 Days and short of anything generated arriving back in it: the
        // founding cohort has left the head of the chain and nothing has refilled it yet.
        Run(simulation, days: 40);

        Assert.Equal(0, Histogram(world)[1]);

        // 🔴 AND THEN IT REFILLS, which is the whole of stage 3 in one assertion. Under stages 1 and
        // 2 this stayed at zero for the rest of the run, because nothing in the build could route a
        // Household back to the head of the chain. `mature_family` now sends its children out as new
        // `young` Households, so a second generation is standing in a stage the first one emptied.
        // ⚠ SAMPLED EVERY DAY RATHER THAN AT THE END, and the first draft was not -- it asserted
        // every stage occupied at Day 160 and `mature_family` was EMPTY there. That is not a defect:
        // the founding cohort does not blur, so it passes through the chain as a WAVE and leaves
        // each stage empty behind it until the next generation arrives. ***A snapshot of an
        // oscillating city catches whatever phase it is in***, which is a property of this world and
        // the reason the cohort question is worth asking at all.
        var everOccupied = new bool[world.Rules.LifeStageCount + 1];

        for (int day = 0; day < 120; day++)
        {
            Run(simulation, days: 1);

            int[] tally = Histogram(world);

            for (int stage = 1; stage <= world.Rules.LifeStageCount; stage++)
            {
                everOccupied[stage] |= tally[stage] > 0;
            }
        }

        // 🔴 `young` REFILLED, which is the whole of stage 3 in one assertion. Under stages 1 and 2
        // it stayed at zero for the rest of the run, because nothing in the build could route a
        // Household back to the head of the chain.
        Assert.True(everOccupied[1]);

        // And every other stage was reached -- the chain is walked end to end rather than piling up
        // anywhere. ⚠ `childless` among them, which nothing could reach before stage 3: it is where
        // a zero draw goes, and until the fertility decision existed no Household ever drew.
        for (int stage = 2; stage <= world.Rules.LifeStageCount; stage++)
        {
            Assert.True(everOccupied[stage], $"stage {stage} was never occupied.");
        }
    }

    /// <summary>
    /// 🔴 <b>The city outlives its founding generation, which is the whole of stage 3.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>240 Days is past the longest life the stage table can draw</b> — a Household seeded into
    /// <c>young</c> lives at most <c>31 + 63 + 63 + 55 = 212</c> Days — so ***every Household alive
    /// at 240 was born here***. ⚠ <b>This test asserted an EMPTY city one commit ago</b> and that was
    /// correct while stage 2 was a sink with no source. It is the second time these assertions have
    /// been rewritten rather than extended, which is what a test pinning a half-built mechanism
    /// costs.
    /// </para>
    /// <para>
    /// ⚠ <b>It does not assert the population GROWS</b>, and must not: <c>aged.toml</c>'s band is
    /// <c>0..3</c> with a mean of 1.5, which is below the 2.0 that replaces exactly, so the city
    /// declines by construction. ***That is a property of the authored band and not a finding about
    /// cities.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_city_outlives_its_founding_generation()
    {
        (World world, Simulation simulation) = City(2_000);

        Run(simulation, days: 240);

        Assert.True(world.Households.Rows.LiveCount > 0);
        Assert.True(world.Citizens.Rows.LiveCount > 0);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// 🔴 <b><c>adr/0011</c>'s own invariant: Citizens are conserved across the spawn.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The ADR states it as a property worth having</b> — ***"Citizen count is conserved across
    /// the spawn transition — children become the adults of the new Households — which makes the
    /// invariant testable rather than asserted."*** This is that test. A spawn that CREATED its
    /// adults would leave the children behind to be destroyed with their parents, and the city would
    /// read as healthy while quietly running two populations.
    /// </para>
    /// <para>
    /// <b>Measured across ONE Day and not over the run</b>, because a run mixes the spawn with
    /// births and dissolutions and the sum of three flows cannot fail informatively. The Day chosen
    /// is one on which the counter says a spawn happened.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_spawn_moves_citizens_and_never_creates_them()
    {
        (World world, Simulation simulation) = City(2_000);

        int checkedDays = 0;

        for (int day = 1; day <= 200 && checkedDays < 3; day++)
        {
            int before = world.Citizens.Rows.LiveCount;

            LifeStageReading reading = RunDay(simulation);

            if (reading.Spawned == 0 || reading.Born > 0 || reading.Dissolved > 0)
            {
                continue;
            }

            // A Day that spawned and did nothing else to the population. The Households grew by one
            // per child; the Citizens did not move at all.
            Assert.Equal(before, world.Citizens.Rows.LiveCount);
            checkedDays++;
        }

        Assert.True(checkedDays > 0, "no Day in 200 spawned without also bearing or dissolving.");
    }

    /// <summary>A child carries age zero and an adult carries a draw from the authored band.</summary>
    /// <remarks>
    /// 🔴 <b><c>Citizens.Age</c>'s writer, which is the amnesty queue item's literal ask</b> — the
    /// column has been declared, saved and hashed since the table was written and nothing had ever
    /// written it. ⚠ <b>Zero is the only marker of childhood there is</b>, so an adult drawing zero
    /// would make the whole population children under stage 4's gate; the band is refused below 1
    /// for exactly that reason.
    /// </remarks>
    [Fact]
    public void A_child_is_age_zero_and_an_adult_is_drawn_from_the_band()
    {
        (World world, Simulation simulation) = City(2_000);

        Assert.True(world.Rules.TryAdultAge(out int min, out int max));

        // The founding city is all adults: nothing has borne a child yet.
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot))
            {
                Assert.InRange(world.Citizens.Age[slot], min, max);
            }
        }

        Run(simulation, days: 60);

        int children = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            if (world.Citizens.Age[slot] == 0)
            {
                children++;
            }
            else
            {
                Assert.InRange(world.Citizens.Age[slot], min, max);
            }
        }

        // Past the Young stage's ceiling of 31 Days, so the fertility decision has fired.
        Assert.True(children > 0);
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

        // EVERY penny, and stage 3 does not change that even though the city is still standing.
        // 240 Days is past the longest life the table can draw, so every endowed Household has
        // dissolved -- and the Households alive at the end were FORMED by the spawn, which opens
        // them at zero. ***A generated Household inherits nothing***, which is what makes this an
        // equality rather than a band.
        Assert.Equal(opening + estates, world.Bins.LevelAt(treasury));

        // The supply is FLAT, and this is the load-bearing half. A death is not an emigration:
        // nothing left the city, so unlike World.Depart there is no decrement to make and making one
        // would be the leak. ⚠ A Household FORMED by the spawn opens at zero, so generation adds
        // nothing to the supply either -- a generated Household inherits nothing.
        Assert.Equal(issued, world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);

        // The exact equality lives here: Invariant.MoneyIsConserved walks every conserved Bin in the
        // world and compares the sum against Issued, so a penny lost in a dissolution or invented in
        // a formation is named with its size.
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

        Run(simulation, days: 240);

        // ⚠ NOT the whole live count: stage 3 means the rest of the city goes on generating around
        // them. The claim is that every stranded Household is STILL THERE -- none was dissolved for
        // carrying a stage id a reload deleted.
        int survivors = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Households.LifeStage[slot] == world.Rules.LifeStageCount + 1)
            {
                survivors++;
            }
        }

        Assert.Equal(stranded, survivors);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>🔴 <b>No child holds a job. <c>plans/0046</c> stage 4.</b></summary>
    /// <remarks>
    /// <para>
    /// <b>Measured after 60 Days</b>, which is past <c>young</c>'s ceiling of 31 so the fertility
    /// decision has fired and there are children to exclude. ⚠ <b>The population is checked to be
    /// mixed first</b>: a run with no children in it would pass this assertion by vacuity, which is
    /// the way a gate test quietly stops testing anything.
    /// </para>
    /// <para>
    /// <b>What it costs the city is the point of the stage.</b> Measured on <c>aged.toml</c> at
    /// 2,000 Citizens over 60 Days: <b>1,411 of 1,411 held a job before the gate and 1,200 of 1,411
    /// after</b> — 211 children, 15% of the population, left the labour force. ***That is a change
    /// to the labour supply and therefore to the city***, which is why <c>plans/0046</c> kept this
    /// stage apart from generation: landing both together would have made two changes to employment
    /// on one day and left neither attributable.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_child_holds_a_job()
    {
        (World world, Simulation simulation) = City(2_000);

        Run(simulation, days: 60);

        int children = 0;
        int adults = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            if (world.Citizens.Age[slot] == 0)
            {
                children++;

                Assert.False(
                    world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]),
                    $"Citizen {slot} is a child and holds a job.");
            }
            else
            {
                adults++;
            }
        }

        // Neither half may be empty, or the assertion above is about nobody.
        Assert.True(children > 0);
        Assert.True(adults > 0);
    }

    /// <summary>
    /// 🔴 <b>No child founds a Business — the gate's second caller, and the world that reaches it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>rulesets/raised.toml</c> exists for this test.</b> The gate has two callers and no
    /// shipped world could reach the second: <c>aged.toml</c> has demographics and no
    /// <c>[founding]</c>, <c>founded.toml</c> has <c>[founding]</c> and no demographics. ***So the
    /// founding half was correct and unobservable***, which is <c>plans/0034</c> <b>F17</b>'s shape —
    /// a mechanism shipped right and unreachable for want of Ruleset content rather than code.
    /// </para>
    /// <para>
    /// 🔴 <b>It asserts on WORKERS and not on founders, and that is not a weaker claim.</b>
    /// <c>adr/0146</c>: ***a founder becomes their Business's first worker***, which is the whole of
    /// the labour cost milestone 27 ships. So a child that founded anything would be holding a job at
    /// the thing it founded, and this catches it. Nothing stores a founder to assert on directly, and
    /// adding a column to make a test easier would be storing state the city does not use.
    /// </para>
    /// <para>
    /// ⚠ <b>The Business count is checked to be non-zero</b>, because a world where nobody founds
    /// would pass by vacuity — and this file is <c>founded.toml</c> underneath precisely so that
    /// Households hold the money to pass the means test.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_child_founds_a_business()
    {
        Ruleset rules = RulesetLoader.Load(
                Path.Combine(AppContext.BaseDirectory, "Rulesets", "raised.toml")).Ruleset
            ?? throw new InvalidOperationException("rulesets/raised.toml did not load.");

        var key = WorldKey.FromSeed(0);
        World world = new(2_000, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        Run(simulation, days: 60);

        int children = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot) || world.Citizens.Age[slot] != 0)
            {
                continue;
            }

            children++;

            Assert.False(
                world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]),
                $"Citizen {slot} is a child and works at a Business.");
        }

        Assert.True(children > 0, "no child existed, so the gate was never asked.");
        Assert.True(world.Businesses.Rows.LiveCount > 0, "nothing was founded, so nothing was gated.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// 🔴 <b>A world with no stage table employs everybody, and this is the guard that matters most.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>Citizens.Age</c> is zero in every Ruleset that declares no <c>[[life_stage]]</c></b>,
    /// which is every shipped file but two — nothing writes the column there. So a working-age gate
    /// reading the column alone would make ***twenty Rulesets into cities of children***, with nobody
    /// employed anywhere and no test in the suite obviously naming why.
    /// <c>World.IsOfWorkingAge</c> guards on <c>DeclaresLifeStages</c> for exactly this, and the
    /// column's zero means <em>child</em> in one world and <em>this world has no demographics</em> in
    /// all the others.
    /// </remarks>
    [Fact]
    public void A_world_with_no_stage_table_employs_everybody()
    {
        Ruleset rules = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;

        var key = WorldKey.FromSeed(0);
        World world = new(2_000, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Assert.False(rules.DeclaresLifeStages);

        Run(simulation, days: 20);

        int employed = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                employed++;
            }

            // Every Citizen here carries the zero that means "child" one world over.
            if (world.Citizens.Rows.IsLive(slot))
            {
                Assert.Equal(0, world.Citizens.Age[slot]);
            }
        }

        Assert.True(employed > 0, "nobody was employed, so the gate read the column without asking "
            + "the Ruleset whether the column means anything.");
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
