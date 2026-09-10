using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
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
        long treasury = Read(census, Metric.Of(MoneyCounter.Treasury))[^1];

        Assert.True(withheld > 0, "no payday withheld anything, so the identity holds over nothing.");
        Assert.True(rule > 0, "the rates Rule never fired, so the path under test never ran.");

        // taxing.toml declares no [[policy]], which is what makes the other two columns legible.
        Assert.Equal(0, policy);

        Assert.True(
            withheld + policy != treasury,
            $"the payday and the Policy sweeps alone account for the whole treasury of {treasury}, "
            + "so this world no longer exercises the unattributed path and the test has stopped "
            + "being about anything.");

        Assert.True(
            rule > withheld,
            $"the rates Rule paid {rule} against {withheld} withheld. On this file the Rule is the "
            + "larger income; a column that has become the smaller one is reading something other "
            + "than the firing.");
    }

    // ---- the fixture ---------------------------------------------------------------------------

    /// <summary>Income at each reading: the three paths in, summed per reading.</summary>
    private static long[] Income(Census census) =>
        Add(
            Read(census, Metric.Of(MoneyFlowCounter.Withheld, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.ToTreasury, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.RuleToTreasury, Aggregate.Sum)));

    /// <summary>Expenditure at each reading: the two paths out, summed per reading.</summary>
    private static long[] Expenditure(Census census) =>
        Add(
            Read(census, Metric.Of(MoneyFlowCounter.FromTreasury, Aggregate.Sum)),
            Read(census, Metric.Of(MoneyFlowCounter.RuleFromTreasury, Aggregate.Sum)));

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
}
