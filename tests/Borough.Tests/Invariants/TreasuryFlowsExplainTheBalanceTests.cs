using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Invariants;

/// <summary>
/// 🔴 The treasury's balance is its flows: over any interval, what it holds moved by exactly what
/// the Census says came in less what it says went out.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the test the defect was worth, and it is worth more than the fix.</b>
/// <c>rulesets/taxing.toml</c> over 24,576 Ticks closed with a treasury of <b>1,621,850</b> against a
/// reported income of <b>180,058</b>. The missing <b>1,441,792</b> was 88 firings of that file's
/// <c>rates</c> Rule, whose output names <c>scope = "global"</c> — a third path into the treasury
/// that <see cref="MoneyFlowCounter"/> did not carry, because every member of it was folded from the
/// Policy sweeps and the paydays. <b>89% of the city's income was unattributed</b>, and the only
/// thing that said so was a footnote under the budget admitting it.
/// </para>
/// <para>
/// 🔴 <b>And it earned its keep a second time, on the next income path to be built.</b> The
/// profit-tax collection — <c>BusinessTaxEngine</c>, <c>plans/0072</c> phase B — reached the
/// treasury out of the Businesses' tills, and this went red on the third reading with a residual of
/// <b>220,338</b> before <see cref="MoneyFlowCounter.ProfitTax"/> was wired into the Census.
/// ***A test that only ever catches the defect it was written for is a regression test; this one is
/// the invariant***, and the fourth term in <see cref="Income"/> is what it asked for.
/// </para>
/// <para>
/// <b>Held per interval and not only at the end</b>, on <c>MoneyLongRunTests</c>' reasoning: a
/// closing equality passes over a run in which the identity broke and was repaired by a later error
/// of the opposite sign. What the per-interval form adds is <em>when</em>, and a failure names the
/// reading rather than the run.
/// </para>
/// <para>
/// ⚠ <b>It is stated over the instrument and never over the engines.</b> The quantity under test is
/// what a player reads off a budget, so both sides are taken from the Census — the level from
/// <c>MoneyCounter.Treasury</c> and the flows from <see cref="MoneyFlowCounter"/>. A test that asked
/// <c>RuleEngine</c> what it had moved would agree with the accumulator by construction and would
/// pass over the whole class of defect this is about, which is a flow that never reaches the
/// reading.
/// </para>
/// <para>
/// ⚠ <b>One path into the treasury is deliberately outside this world's reach and is not yet
/// counted</b> — <c>World.Dissolve</c> passes a dissolving Household's estate to the treasury.
/// <c>taxing.toml</c> declares no <c>[[life_stage]]</c> table, so nothing dissolves and the identity
/// is exact here; ***on a file that declares both life stages and money it is an open hole***, and
/// <c>MoneyFlowCounter.RuleToTreasury</c> names it rather than leaving it to be rediscovered by
/// somebody else's arithmetic.
/// </para>
/// </remarks>
public sealed class TreasuryFlowsExplainTheBalanceTests
{
    /// <summary>
    /// Small, because the assertion is an identity rather than a magnitude.
    /// </summary>
    /// <remarks>
    /// An identity holds at any population, so the size is chosen to make the run affordable in the
    /// assertion tier and for no other reason. What the population <em>does</em> have to do is be
    /// large enough that somebody is employed and paid, which is what
    /// <see cref="The_flows_are_all_non_zero_so_the_identity_is_not_vacuous"/> asserts rather than
    /// assumes.
    /// </remarks>
    private const int Citizens = 1_000;

    /// <summary>Six Days, which is <c>IncomeDumpTests</c>' length and for its reason.</summary>
    /// <remarks>
    /// It is how long this world takes to employ somebody and then find an employer able to pay
    /// them. A shorter run holds the identity over a table of true zeroes.
    /// </remarks>
    private const ulong Length = 12_288;

    private const ulong Seed = 1;

    /// <summary>The cadence, which is a Day: a payday falls on a Day boundary and nowhere else.</summary>
    private const ulong Cadence = 2_048;

    /// <summary>
    /// 🔴 Every reading's change in the balance equals that reading's income less its expenditure.
    /// </summary>
    /// <remarks>
    /// <b>The whole of the claim, and it is an exact equality rather than a band.</b> Money is
    /// conserved (<c>adr/0024</c>) and the treasury opens empty (<c>adr/0116</c>), so there is no
    /// rounding term and no opening stock to allow for: a residual of any size at all is a path into
    /// or out of the treasury that no flow counter can see, which is precisely the defect.
    /// </remarks>
    [Fact]
    public void Every_interval_of_the_balance_is_explained_by_the_flows_beside_it()
    {
        Census census = Run();

        long[] treasury = Read(census, Metric.Of(MoneyCounter.Treasury));
        long[] income = Income(census);
        long[] expenditure = Expenditure(census);

        Assert.True(treasury.Length > 1, "one reading is no interval, so nothing is under test.");
        Assert.Equal(0, treasury[0]);

        for (int i = 1; i < treasury.Length; i++)
        {
            long moved = treasury[i] - treasury[i - 1];
            long explained = income[i] - expenditure[i];

            Assert.True(
                moved == explained,
                $"reading {i}: the treasury moved by {moved} and the flows explain {explained}, a "
                + $"residual of {moved - explained}. A unit of the balance that no flow column names "
                + "is a path into the treasury the budget cannot attribute -- which is what "
                + "MoneyFlowCounter.RuleToTreasury was added to close.");
        }
    }

    /// <summary>
    /// The closing balance is the whole run's income less its whole expenditure.
    /// </summary>
    /// <remarks>
    /// <b>Not implied by the per-interval form, because the readings are a window.</b> The Census
    /// holds a bounded number of them and drops the oldest, so a series that had become a tail of
    /// itself would satisfy every interval it still held and say nothing about the run. This asserts
    /// against the treasury's own opening zero, which only the first reading supplies.
    /// </remarks>
    [Fact]
    public void The_closing_balance_is_the_runs_whole_income_less_its_whole_expenditure()
    {
        Census census = Run();

        long[] treasury = Read(census, Metric.Of(MoneyCounter.Treasury));

        Assert.Equal(Sum(Income(census)) - Sum(Expenditure(census)), treasury[^1]);
    }

    /// <summary>
    /// Every mechanism this file can exercise moved something, and the Bin Rule moved the most.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Without this the identity above is satisfiable by a city where nothing happened.</b> A
    /// treasury that never moved has a residual of zero on every interval, so the equality passes
    /// over exactly the world that proves nothing.
    /// </para>
    /// <para>
    /// 🔴 <b>And the last assertion is the defect stated as an arithmetic.</b> The counters the
    /// instrument carried before row 33 — the payday's and the Policy sweeps' — do <em>not</em>
    /// explain this world's balance, and the gap is the Bin Rule's. If somebody folds
    /// <c>RuleToTreasury</c> back into <c>ToTreasury</c> the identity survives and this fails, which
    /// is the point: ***the columns are apart because the levers are apart***, and a report that
    /// balances is not thereby a report a player can act on.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_flows_are_all_non_zero_so_the_identity_is_not_vacuous()
    {
        Census census = Run();

        long withheld = Sum(Read(census, Metric.Of(MoneyFlowCounter.Withheld, Aggregate.Sum)));
        long policy = Sum(Read(census, Metric.Of(MoneyFlowCounter.ToTreasury, Aggregate.Sum)));
        long rule = Sum(Read(census, Metric.Of(MoneyFlowCounter.RuleToTreasury, Aggregate.Sum)));
        long profit = Sum(Read(census, Metric.Of(MoneyFlowCounter.ProfitTax, Aggregate.Sum)));
        long subsidy = Sum(Read(census, Metric.Of(MoneyFlowCounter.Subsidy, Aggregate.Sum)));
        long treasury = Read(census, Metric.Of(MoneyCounter.Treasury))[^1];

        Assert.True(withheld > 0, "no payday withheld anything, so the identity holds over nothing.");
        Assert.True(rule > 0, "the rates Rule never fired, so the path under test never ran.");
        Assert.True(
            profit > 0,
            "no Business paid any profit tax, so the fourth income path is in the identity as a "
            + "column of zeroes. [business_tax] is CHANGE 6 of taxing.toml; a zero here is either a "
            + "sweep that never ran or a world in which nothing traded at a profit.");

        // 🔴 THIS USED TO ASSERT policy == 0, and change 7 of taxing.toml ended it. The file states a
        // tool = "charge" on the grocers' tills, and PolicyEngine folds a charge into the same
        // accumulator as a transfer -- so the column is the charge, and a zero here is the charge
        // never firing rather than a file with no inbound Policy.
        Assert.True(
            policy > 0,
            "no Policy paid into the treasury. CHANGE 7 of taxing.toml is a charge on a grocer's "
            + "balance; a zero is the charge never firing.");

        // ⚠ Not implied by anything above: SubsidyEngine pays out through a door PolicyEngine never
        // touches, so before MoneyFlowCounter.Subsidy this money left the treasury unattributed and
        // the identity above went red -- which is RuleToTreasury's defect on the expenditure side.
        Assert.True(
            subsidy > 0,
            "no subsidy was ever paid. CHANGE 9 of taxing.toml states one with a ceiling chosen to "
            + "bind; a zero here leaves the third expenditure term untested.");

        Assert.True(
            withheld + policy + profit != treasury,
            $"the two taxes and the Policy sweeps alone account for the whole treasury of {treasury}, "
            + "so this world no longer exercises the unattributed path and the test has stopped "
            + "being about anything.");

        Assert.True(
            rule > withheld,
            $"the rates Rule paid {rule} against {withheld} withheld. On this file the Rule is the "
            + "larger income; a column that has become the smaller one is reading something other "
            + "than the firing.");
    }

    /// <summary>
    /// 🔴 The identity holds on a world that pays for a school, which is the eighth flow's own test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A placement is the first expenditure with no payee</b>, so it is also the first that could
    /// break this by arithmetic rather than by omission: the balance falls and no Bin anywhere rose.
    /// Before <see cref="MoneyFlowCounter.Placement"/> the residual would have been the whole price
    /// of every school the city bought — ***which is <c>plans/0072</c> F11's defect arriving for the
    /// fourth time and on the expenditure side for the second.***
    /// </para>
    /// <para>
    /// ⚠ <b>This world opens with money, unlike <see cref="Run"/>'s.</b> The opening balance is not a
    /// flow and appears in no column, so the identity is stated over the CHANGE in the balance per
    /// interval and the closing assertion adds the opening back — an opening stock is not income.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_identity_holds_on_a_world_that_places_a_school()
    {
        Census census = RunPlacing(out long spent);

        long[] treasury = Read(census, Metric.Of(MoneyCounter.Treasury));
        long[] income = Income(census);
        long[] expenditure = Expenditure(census);
        long[] placement = Read(census, Metric.Of(MoneyFlowCounter.Placement, Aggregate.Sum));

        Assert.True(treasury.Length > 1, "one reading is no interval, so nothing is under test.");
        Assert.Equal(Opening, treasury[0]);

        Assert.True(
            spent > 0,
            "no school was ever placed, so the eighth flow is in the identity as a column of "
            + "zeroes and this test is about nothing.");

        Assert.Equal(spent, Sum(placement));

        for (int i = 1; i < treasury.Length; i++)
        {
            long moved = treasury[i] - treasury[i - 1];
            long explained = income[i] - expenditure[i];

            Assert.True(
                moved == explained,
                $"reading {i}: the treasury moved by {moved} and the flows explain {explained}, a "
                + $"residual of {moved - explained}. A placement's price leaves the treasury and "
                + "reaches nobody, so MoneyFlowCounter.Placement is the only column that can name "
                + "it.");
        }

        Assert.Equal(Opening + Sum(Income(census)) - Sum(Expenditure(census)), treasury[^1]);
    }

    // ---- the fixture ---------------------------------------------------------------------------

    /// <summary>Income at each reading: the four paths in, summed per reading.</summary>
    /// <remarks>
    /// 🔴 <b>The fourth term was added on the day the path was, and this test found it rather than
    /// being updated alongside it.</b> The profit-tax collection reached the treasury before
    /// <see cref="MoneyFlowCounter.ProfitTax"/> existed and the identity broke by a residual of
    /// 220,338 on the third reading of a twelve-Day run — which is the whole of what this test is
    /// for. ***A new income path that does not appear here is a new hole***, and the failure names
    /// the reading rather than the run.
    /// </remarks>
    private static long[] Income(Census census) =>
        Add(
            Read(census, Metric.Of(MoneyFlowCounter.Withheld, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.ToTreasury, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.RuleToTreasury, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.ProfitTax, Aggregate.Sum)));

    /// <summary>Expenditure at each reading: the three paths out, summed per reading.</summary>
    /// <remarks>
    /// 🔴 <b>And it earned its keep a THIRD time, on the first expenditure path ever added.</b>
    /// <c>SubsidyEngine</c> apportions a pot and pays it itself, so not one unit of it passes
    /// through <c>PolicyEngine.Move</c> and none of it reached <c>FromTreasury</c>. The third term
    /// is what this asked for — ***a new path out of the treasury that does not appear here is a
    /// new hole***, in the direction a budget over-reports rather than under-reports.
    /// ⚠ <b>Profit-tax RELIEF has no term here and must never acquire one</b>: it reduces a bill
    /// before the collection, so it moves no Money and the treasury is exactly where it would be if
    /// the reliefs had never been declared.
    /// 🔴 <b>The fourth term is the first that pays NOBODY.</b> A placement's price leaves the money
    /// supply rather than another Bin (<c>adr/0035</c> §2), and it is expenditure all the same
    /// because the balance fell by it — ***what makes something a term here is that it crossed the
    /// treasury's edge, not that somebody caught it.***
    /// </remarks>
    private static long[] Expenditure(Census census) =>
        Add(
            Read(census, Metric.Of(MoneyFlowCounter.FromTreasury, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.RuleFromTreasury, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.Subsidy, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.Placement, Aggregate.Sum)));

    private static long[] Add(params long[][] columns)
    {
        long[] total = new long[columns[0].Length];

        foreach (long[] column in columns)
        {
            Assert.Equal(total.Length, column.Length);

            for (int i = 0; i < total.Length; i++)
            {
                total[i] += column[i];
            }
        }

        return total;
    }

    private static long Sum(long[] column)
    {
        long total = 0;

        foreach (long value in column)
        {
            total += value;
        }

        return total;
    }

    /// <summary>One metric's readings over the whole run, in order.</summary>
    private static long[] Read(Census census, Metric metric)
    {
        ReadOnlyMemory<CensusSample> samples =
            census.Series(metric, new Ticks(ulong.MaxValue / 2)).Samples;

        long[] values = new long[samples.Length];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = samples.Span[i].Value;
        }

        return values;
    }

    /// <summary>
    /// A run of <c>rulesets/taxing.toml</c>, observed every Day.
    /// </summary>
    /// <remarks>
    /// <b>The shipped file rather than one authored inline</b>, which is the opposite of
    /// <c>MoneyCensusTests</c>' choice and for a stated reason: that file is testing the instrument
    /// and this is testing that the instrument covers the <em>city</em>. The hole this closes was
    /// found by running a shipped Ruleset and doing the arithmetic by hand, and a fixture written
    /// here would only ever contain the paths whoever wrote it remembered.
    /// </remarks>
    private static Census Run()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "taxing.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, loaded.Ruleset!, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var simulation = new Simulation(world, key);

        // Room for every reading the run takes, so the series is never a tail of itself: the first
        // one is the founding, and the closing assertion is stated against its zero.
        var census = new Census(world, (int)((Length / Cadence) + 2));

        census.Observe(simulation);

        for (ulong tick = 0; tick < Length; tick++)
        {
            simulation.Step(default);

            if (simulation.Tick.Raw % Cadence == 0)
            {
                census.Observe(simulation);
            }
        }

        return census;
    }

    /// <summary>What the placing world founds its treasury with.</summary>
    private const long Opening = 65_536;

    /// <summary>What one school costs it.</summary>
    private const long Price = 4_096;

    private const byte School = 2;

    /// <summary>
    /// A world that opens with money and buys four schools, observed every Day.
    /// </summary>
    /// <remarks>
    /// <b>Authored here rather than shipped, which is the opposite of <see cref="Run"/>'s choice and
    /// for a stated reason.</b> No shipped Ruleset prices a placement — absence is free, and that is
    /// what keeps every shipped world placing what it always placed — so a fixture is the only world
    /// in which this flow can be non-zero at all.
    /// </remarks>
    private static Census RunPlacing(out long spent)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(Priced, "priced.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(200, loaded.Ruleset!, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };
        var census = new Census(world, 16);

        census.Observe(simulation);
        spent = 0;

        for (int day = 0; day < 8; day++)
        {
            for (ulong tick = 0; tick < Cadence; tick++)
            {
                // Two of the eight Days buy a school, so the identity is asserted over intervals
                // that spent and over intervals that did not -- a residual only ever appears in one
                // of the two, and a fixture that spent on every interval could not tell them apart.
                bool buying = tick == 512 && day is 1 or 3 or 5 or 7;

                simulation.Step(buying ? new TickInput([Buy(world)], 0) : default);

                if (buying)
                {
                    spent += Price;
                }
            }

            census.Observe(simulation);
        }

        return census;
    }

    /// <summary>A <c>Service</c> command for the first vacant Lot standing.</summary>
    private static Command Buy(World world)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot))
            {
                return Command.Service(world.Lots.East[slot], world.Lots.North[slot], School);
            }
        }

        Assert.Fail("the generated city left no vacant Lot to buy a school on.");
        return default;
    }

    /// <summary>A city that opens with money and a school kind it has to pay for.</summary>
    private const string Priced = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true
        bins = [ { resource = "sundries", capacity = 48 } ]

        [[building]]
        name = "school"
        serves = "education"
        placement_cost = 4096

        [[zone_rule]]
        name          = "housing"
        kind          = "dwelling"
        zone          = 0
        interval      = 32
        revisit_ticks = 2048

        [placement]
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3

        [roads]
        block_tiles = 32
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000

        [lots]
        lots_per_segment = 5
        setback_tiles = 2

        [capacity]
        floor_tiles_per_occupant      = 6
        floor_tiles_per_job           = 1
        floor_tiles_per_parking_space = 6

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50

        [needs]
        sustenance_degrade   = 1
        sustenance_recover   = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        education_degrade    = 2
        education_recover    = 2
        floor = -1000

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000

        [treasury]
        opening_balance = 65536
        """;
}
