using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 27 task 8: a Household founds a Business (<c>adr/0145</c>).
/// </summary>
public sealed class FoundingTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out = output;

    private static Ruleset Load(string file)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    private static (World World, Simulation Simulation) City(string file, ulong seed, int citizens)
    {
        var key = WorldKey.FromSeed(seed);
        var world = new World(citizens, Load(file));

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, new Simulation(world, key));
    }

    /// <summary>
    /// The shipped founding Ruleset creates Businesses, and no other shipped file does.
    /// </summary>
    [Fact]
    public void The_shipped_founding_ruleset_creates_businesses_and_taxed_creates_none()
    {
        (World founded, Simulation running) = City("founded.toml", 0xF0DDU, 2_000);

        for (int tick = 0; tick < 4_096; tick++)
        {
            running.Step(TickInput.Empty);
        }

        PlacementActivity activity = running.Placement.Drain();

        _out.WriteLine($"founded.toml: founded={activity.Founded.Sum} premised={activity.Premised.Sum} retired={activity.Retired.Sum} live={founded.Businesses.Rows.LiveCount} pool={founded.UnpremisedPool.Count}");

        Assert.True(
            activity.Founded.Sum > 0,
            "founded.toml founded no Business in 4,096 Ticks. It is the only shipped file with a "
            + "[founding] table, and if nothing is founded either the means test refuses every "
            + "Household -- check [households] opening_balance against founding_band -- or the pass "
            + "is not running.");

        // The control. taxed.toml is founded.toml minus the three added tables, so if IT founds
        // something the founding is coming from somewhere other than [founding].
        //
        // ⚠ THE CONTROL IS THE COUNTER AND NOT THE ROW COUNT, and it stopped being the row count on
        // the day adr/0148 landed: every shipped file's dwelling kind now comes with a trade, so
        // taxed.toml holds a Business per standing Building and always will. What it must not do is
        // FOUND one -- the row count answers "are there shops", and the question here is "did the
        // [founding] pass run in a file that does not state it".
        (World control, Simulation controlRunning) = City("taxed.toml", 0xF0DDU, 2_000);

        for (int tick = 0; tick < 4_096; tick++)
        {
            controlRunning.Step(TickInput.Empty);
        }

        PlacementActivity controlActivity = controlRunning.Placement.Drain();

        _out.WriteLine(
            $"taxed.toml: founded={controlActivity.Founded.Sum} "
            + $"live={control.Businesses.Rows.LiveCount} (all of them came with their premises)");

        Assert.Equal(0, controlActivity.Founded.Sum);
    }

    /// <summary>
    /// The founding channel has a sink, and a run long enough to reach it drains the pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is <c>adr/0006</c>'s obligation for the channel milestone 27 task 8 added</b>, and
    /// it is the reason the loader refuses <c>[founding]</c> in a file with no
    /// <c>gives_up_after_days</c>. Nothing tenants a Business, so ***every shop founded here leaves
    /// eventually*** and the pool's size is what must stay bounded.
    /// </para>
    /// <para>
    /// <b>The bound is patched down rather than the run being made long.</b> The shipped file waits
    /// <b>30 Days</b> — 61,440 Ticks — so an honest test of the sink at the shipped value would be a
    /// two-minute assertion, which <c>TierBudgetTests</c> is right to dislike. Editing the text and
    /// parsing it is <c>ReachFailureTests</c>'s trick, and the thing under test is the mechanism
    /// rather than the number.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_founded_business_nothing_tenants_eventually_leaves()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "founded.toml"));

        Assert.Contains("gives_up_after_days = 30", toml, StringComparison.Ordinal);

        RulesetLoadResult loaded = RulesetLoader.Parse(
            toml.Replace("gives_up_after_days = 30", "gives_up_after_days = 1", StringComparison.Ordinal),
            "founded-impatient.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(0xF0DDU);
        var world = new World(2_000, loaded.Ruleset!);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var running = new Simulation(world, key);

        for (int tick = 0; tick < 8_192; tick++)
        {
            running.Step(TickInput.Empty);
        }

        PlacementActivity activity = running.Placement.Drain();

        _out.WriteLine(
            $"impatient: founded={activity.Founded.Sum} retired={activity.Retired.Sum} "
            + $"pool={world.UnpremisedPool.Count}");

        Assert.True(activity.Founded.Sum > 0, "nothing was founded, so the sink is untested.");

        Assert.True(
            activity.Retired.Sum > 0,
            $"{activity.Founded.Sum} Businesses were founded and NONE was retired over 8,192 Ticks "
            + "with a one-Day bound. Nothing tenants a Business, so the unpremised pool has an inflow "
            + "and no outflow -- which is the adr/0006 hole the gives_up_after_days refusal exists to "
            + "prevent, reached anyway.");

        // The pool is BOUNDED rather than empty, which is the claim adr/0006 actually makes: the
        // drain rate is proportional to the stock, so the size settles rather than the pool emptying.
        Assert.True(
            world.UnpremisedPool.Count < activity.Founded.Sum,
            $"the pool holds {world.UnpremisedPool.Count} of {activity.Founded.Sum} ever founded, so "
            + "nothing is actually leaving it.");
    }

    /// <summary>
    /// Every founded Business has its founder on its worker list, and the founder had no job before.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0146</c>'s labour cost, which is the half of it milestone 27 can ship.</b> The
    /// founder is <em>occupied</em>, so the employment pass will not hire them and the city is one
    /// worker down. ***A cost with no wage attached is still a cost, because a Citizen is a scarce
    /// thing.***
    /// </para>
    /// <para>
    /// ⚠ <b>The income half is NOT tested here and must not be added.</b> <c>adr/0026</c> — wages
    /// posted locally, each Business adjusting by its own fill rate — is milestone <b>15</b>, and the
    /// founder forgoing a wage is that ADR running on a Business with an empty Bin. A 27-shaped proxy
    /// would be a second, worse answer somebody has to find and delete on the day 15 lands.
    /// </para>
    /// <para>
    /// <b>The founder is identified by the JOB and by nothing else</b>, which is why this test walks
    /// the worker list rather than reading a column: there is no <c>founder</c> column, and declaring
    /// one would make <c>BusinessTable</c> and <c>CitizenTable</c> mutually dependent at construction.
    /// ⚠ <b>So what is asserted is that every founded Business is STAFFED</b> — that the link exists —
    /// rather than that a particular Citizen is on it, because after the assignment pass has run a
    /// second worker is an ordinary outcome and the founder is no longer distinguishable.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_founded_business_carries_its_founder_as_a_worker()
    {
        (World world, Simulation running) = City("founded.toml", 0xF0DDU, 2_000);

        for (int tick = 0; tick < 8_192; tick++)
        {
            running.Step(TickInput.Empty);
        }

        // The trades this file FOUNDS are `bakery` and `barber`; `shop` is what the dwelling kind
        // comes with (adr/0148), and those are instantiated by construction with nobody in them until
        // the assignment pass arrives. Kind 1 is `shop`, declared first.
        int founded = 0;
        int staffed = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot) || world.Businesses.Kind[slot] == 1)
            {
                continue;
            }

            founded++;

            if (world.Workers.Length(slot) > 0)
            {
                staffed++;
            }
        }

        _out.WriteLine($"founders: founded-trade businesses={founded} staffed={staffed}");

        Assert.True(founded > 0, "nothing was founded, so the labour cost is untested.");

        Assert.Equal(founded, staffed);
    }

    /// <summary>
    /// A founded Business finds premises, and the room it takes is room a Household cannot have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The middle of the mechanism</b> (<c>adr/0147</c>). Milestone 25 shipped the exit and task 8
    /// shipped the entrance; until this pass existed <c>founded.toml</c> founded Businesses that could
    /// only wait and leave, which its own header calls a leak by construction.
    /// </para>
    /// <para>
    /// ⚠ <b>The premised count does NOT equal the live-and-premised count, and that is the mechanism
    /// rather than a discrepancy.</b> <c>founded.toml</c> descends from <c>minimal.toml</c>, which
    /// condemns Buildings throughout a run — and <c>World.Destroy</c> unpremises every tenant of a
    /// Building it takes down (<c>adr/0144</c>). ***So a shop can take premises, lose them to
    /// condemnation, and return to the pool to look again***, which is why the flow exceeds the
    /// standing count.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_founded_business_finds_premises_and_takes_a_households_room()
    {
        (World world, Simulation running) = City("founded.toml", 0xF0DDU, 2_000);

        for (int tick = 0; tick < 4_096; tick++)
        {
            running.Step(TickInput.Empty);
        }

        PlacementActivity activity = running.Placement.Drain();
        int premisedNow = world.Businesses.Rows.LiveCount - world.UnpremisedPool.Count;

        _out.WriteLine(
            $"premises: founded={activity.Founded.Sum} premised={activity.Premised.Sum} "
            + $"standing={premisedNow} pooled={world.UnpremisedPool.Count}");

        Assert.True(
            activity.Premised.Sum > 0,
            "no Business took premises, so the pass did nothing and adr/0147 is untested.");

        // ⚠ THE STANDING COUNT IS NOT COMPARABLE TO THE FLOW, and it was until adr/0148. Most shops
        // standing in this world came with their premises and were never premised by this pass at
        // all, so `premised >= standing` is now false by construction rather than by a defect. What
        // the flow can still be held against is the POOL: every founded Business lands there, and a
        // pass that never emptied it would leave it holding everything ever founded.
        Assert.True(
            world.UnpremisedPool.Count < activity.Founded.Sum,
            $"the pool holds {world.UnpremisedPool.Count} of {activity.Founded.Sum} ever founded "
            + "while the pass recorded " + activity.Premised.Sum + " premise events, so what it is "
            + "premising is not coming out of the pool.");

        // Every premised Business occupies a slot inside its ceiling. This is the assertion that
        // would fail if HasRoom had gone on counting Households alone.
        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || !world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out int building))
            {
                continue;
            }

            Assert.True(
                world.TryDeclaredOccupancy(world.Buildings.Kind[building], out int allowed)
                && world.Tenants(building) <= allowed,
                $"Building {building} holds more tenants than its kind admits.");
        }
    }
}
