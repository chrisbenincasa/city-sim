using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// 🔴 <b>The profit-tax collection runs before anything on the Day-boundary Tick can roll a
/// Business's books, and this is the only thing holding it there.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A Business's Day figures stand until the first recognition of the NEXT Day rolls them to
/// zero</b> (<c>BusinessTable.Roll</c>). On a Day-boundary Tick there are three sites that do
/// exactly that, and every one of them runs later in the Tick than phase 1:
/// </para>
/// <list type="bullet">
/// <item><description><b>Phase 3</b> — <c>RuleEngine.Fire</c> posts a Bin Rule's sale.</description></item>
/// <item><description><b>Phase 4</b> — <c>ShoppingEngine</c> posts an over-the-counter sale.</description></item>
/// <item><description><b>Phase 6</b> — <c>WageEngine.Sweep</c> accrues every employer's wage bill for
/// the Day now starting, <em>above</em> its own payday test, so it fires on every Day boundary
/// whether or not anybody is paid.</description></item>
/// </list>
/// <para>
/// ***Behind any one of them the sweep reads a rolled row, <c>ProfitOn</c> answers zero, and the
/// city is never taxed*** — with no other column of any readout changed and nothing throwing. A
/// comment saying <em>this must run first</em> is exactly the kind of thing that survives the edit
/// that breaks it, so the ordering is a test.
/// </para>
/// <para>
/// ⚠ <b>Two tests and neither is redundant.</b> The first states the consequence — a Day that WAS
/// rolled later in the same Tick was still taxed — and would fail for a sweep moved anywhere behind
/// phase 3. The second states the position, at the phase boundary, and catches a move that has not
/// yet broken anything on <em>this</em> world but has taken away the margin.
/// </para>
/// </remarks>
public sealed class BusinessTaxPhaseOrderTests
{
    private const int Citizens = 1_000;

    /// <summary>How long the city runs before a Day with taxable trade in it.</summary>
    /// <remarks>
    /// <b>Not a cadence</b> — it is how long <c>rulesets/taxing.toml</c> takes to raise shopfronts,
    /// staff them and have them trade a Day at a profit. A shorter run would assert over a boundary
    /// on which nothing was assessable and would pass for the wrong reason, which is what
    /// <see cref="A_days_profit_is_taxed_although_the_payroll_rolls_the_row_on_the_same_tick"/>
    /// asserts rather than assumes.
    /// </remarks>
    private const int WarmDays = 5;

    /// <summary>
    /// 🔴 A Day's profit is taxed even though the payroll rolls the very same rows later in the
    /// very same Tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The expected bill is computed from the world immediately BEFORE the Step</b>, summed over
    /// every live Business, so the assertion is an exact equality rather than a direction and needs
    /// no world with a single trade in it.
    /// </para>
    /// <para>
    /// 🔴 <b>And the last assertion is the ordering stated as an arithmetic.</b> It counts the taxed
    /// Businesses whose <c>TradingDay</c> was moved on to the new Day <em>during that same Step</em>
    /// — if the sweep were moved behind the payroll, those are precisely the rows that would read
    /// zero, and the bill above would collapse to nothing. ***A run in which that count is zero is a
    /// run in which this test is not about anything***, which is why it is asserted and not assumed.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_days_profit_is_taxed_although_the_payroll_rolls_the_row_on_the_same_tick()
    {
        (World world, Simulation sim) = City();

        Warm(world, sim);

        BusinessTaxSchedule schedule = world.Rules.BusinessTax;
        ushort today = BusinessAccounts.DayOf(world.Tick);
        var yesterday = (ushort)(today - 1);

        long expected = 0;
        int taxable = 0;
        List<int> assessed = [];

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            long bill = BusinessTax.DueOn(world.Businesses.ProfitOn(slot, yesterday), schedule);

            if (bill <= 0)
            {
                continue;
            }

            expected += bill;
            taxable++;
            assessed.Add(slot);
        }

        Assert.True(
            taxable > 0,
            $"no Business closed Day {yesterday} in profit, so the boundary under test assesses "
            + "nobody and the ordering claim is untested. Lengthen the warm-up.");

        sim.Step(default);

        ProfitTaxReading reading = sim.LastProfitTax;

        Assert.Equal(expected, reading.Due);
        Assert.Equal(taxable, reading.Taxable);
        Assert.Equal(expected, reading.Collected);

        // 🔴 THE ORDERING, AS AN ARITHMETIC. Every one of these rows was rolled on to the new Day
        // by something later in the same Tick -- the payroll's wage accrual, a Bin Rule's sale, or
        // a Household's shopping. A sweep placed behind any of them would have read a rolled row
        // and assessed nothing at all.
        int rolled = 0;

        foreach (int slot in assessed)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Businesses.TradingDay[slot] == today)
            {
                rolled++;
            }
        }

        Assert.True(
            rolled > 0,
            $"none of the {taxable} assessed Businesses had its books rolled on to Day {today} "
            + "during that Tick, so nothing in this world would notice the sweep being moved behind "
            + "the payroll and this test has stopped being about the ordering.");
    }

    /// <summary>
    /// 🔴 The collection is already complete at the end of phase 1, before any phase that recognises
    /// revenue has run.
    /// </summary>
    /// <remarks>
    /// <b>The position rather than the consequence, and it is the half that keeps the margin.</b>
    /// The test above fails once the sweep is behind a recogniser that actually fired on that
    /// boundary in that world; this one fails the moment it leaves phase 1 at all — including a move
    /// to phase 2, which would be a read-only phase writing to the city (<c>adr/0037</c>), and a move
    /// to the head of phase 6, which happens to be ahead of the payroll <em>today</em> and is one
    /// line's edit away from not being.
    /// </remarks>
    [Fact]
    public void The_collection_is_complete_by_the_end_of_phase_one()
    {
        (World world, Simulation sim) = City();

        Warm(world, sim);

        var seen = new Dictionary<TickPhase, long>();

        sim.PhaseCompleted = phase => seen[phase] = sim.LastProfitTax.Collected;

        sim.Step(default);

        sim.PhaseCompleted = null;

        Assert.True(
            sim.LastProfitTax.Collected > 0,
            "nothing was collected on this boundary, so the phase it was collected in says nothing.");

        Assert.True(
            seen.TryGetValue(TickPhase.Wake, out long atWake),
            "phase 1 never reported completing, so the fixture is not watching what it thinks.");

        Assert.True(
            atWake == sim.LastProfitTax.Collected,
            $"phase 1 ended with {atWake} collected against the Tick's {sim.LastProfitTax.Collected}. "
            + "The collection must be COMPLETE before phase 2, because phase 3's Rule engine, phase "
            + "4's shopping and phase 6's payroll all roll a Business's Day figures on to the new "
            + "Day -- and a sweep behind any of them reads zero and never taxes the city.");

        // Nothing is added to it afterwards either, which is the other half of "complete".
        Assert.Equal(atWake, seen[TickPhase.Commit]);
    }

    // ---- the fixture -----------------------------------------------------------------------------

    /// <summary>
    /// Steps to the first Day boundary past <see cref="WarmDays"/> on which somebody is taxable.
    /// </summary>
    /// <remarks>
    /// <b>It leaves <c>world.Tick</c> ON the boundary, which is the Tick about to run.</b> So the
    /// caller reads yesterday's books, steps once, and that single Step is the boundary Tick under
    /// test.
    /// </remarks>
    private static void Warm(World world, Simulation sim)
    {
        for (int tick = 0; tick < WarmDays * Ticks.PerDay; tick++)
        {
            sim.Step(default);
        }

        Assert.Equal(0UL, world.Tick.Raw % (ulong)Ticks.PerDay);
    }

    /// <summary>
    /// <c>rulesets/taxing.toml</c>, which is the one shipped world that states
    /// <c>[business_tax]</c>.
    /// </summary>
    /// <remarks>
    /// <b>The shipped file rather than one authored inline</b>, on
    /// <c>TreasuryFlowsExplainTheBalanceTests</c>' reasoning: what is under test is that the ordering
    /// holds in the <em>city</em>, against every recogniser it actually contains, and a fixture
    /// written here would only ever contain the ones whoever wrote it remembered.
    /// </remarks>
    private static (World World, Simulation Sim) City()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "taxing.toml"));

        Assert.True(loaded.Ok, loaded.Describe());
        Assert.True(
            loaded.Ruleset!.BusinessTax.Levies,
            "rulesets/taxing.toml no longer levies a profit tax, so this file tests nothing.");

        var key = WorldKey.FromSeed(1);
        var world = new World(Citizens, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, sim);
    }
}
