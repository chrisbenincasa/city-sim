namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

/// <summary>
/// The city's budget, printed: what it took in, what it paid out, and what it is holding.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>MoneyDump</c> answers <em>where is the money</em>; this answers <em>what is the city
/// living on</em>.</b> The circular-flow dump is a balance sheet over the whole supply and its
/// treasury row is one of six; this is the treasury's own account, and its three income columns are
/// the reason it is a second picture rather than a column added to the first. A budget is a thing a
/// player reads before deciding what to change, and the thing they change is a rate or an amount —
/// so the report has to say which of the two brought the money in.
/// </para>
/// <para>
/// 🔴 <b>Income is printed in four columns and expenditure in three, and none of the seven is netted
/// against another.</b> <c>MoneyFlowCounter.FromTreasury</c>'s own remark carries the argument: a net
/// is the one figure that cannot say whether a city taxed nothing and paid nothing or taxed heavily
/// and paid it all back. The split within income is the same argument one level down — money an
/// employer <em>withheld</em> before a Household ever held it, money a <c>[[policy]]</c>
/// <em>moved</em> out of a purse that did, money a <c>[[rule]]</c> <em>paid in</em> off a premises,
/// and a <b>profit tax</b> taken off a trade's own till are one arrival through four levers, and a
/// city that swapped one for another would show a flat total under a changed file. ⚠ <b>The two
/// taxes are the pair most easily folded and the pair that must not be</b>: they are levied under
/// different tables, on different payers, and <c>plans/0072</c>'s whole question is which of the two
/// a city lives on.
/// </para>
/// <para>
/// 🔴 <b>The rule column is why the balance column can be read at all.</b> Before it, a Bin Rule
/// whose output named <c>scope = "global"</c> reached the treasury and appeared in no flow: on
/// <c>rulesets/taxing.toml</c> over 24,576 Ticks that was <b>1,441,792</b> of a closing
/// <b>1,621,850</b> — <b>89%</b> of the income unattributed, with a footnote here admitting it.
/// ***A budget whose balance cannot be explained by the flows beside it is a table, not a budget.***
/// The footnote is gone because the hole is; <c>MoneyFlowCounter.RuleToTreasury</c> holds the
/// argument for counting it apart rather than folding it into the policy column, and
/// <c>TreasuryFlowsExplainTheBalanceTests</c> is what stops the hole reopening.
/// </para>
/// <para>
/// <b>It reads the Census rather than the world</b>, on <c>MoneyDump</c>'s reasoning exactly: a flow
/// is a magnitude over an interval and the world holds only levels. ⚠ <b>The withheld column exists
/// because the Census now carries it</b> — <c>WageEngine</c> splits one till debit into a purse
/// credit and a treasury credit, and until <c>MoneyFlowCounter.Withheld</c> that second credit
/// appeared in no flow the instrument could see. A budget printed before it would have shown a
/// balance rising against an income of zero.
/// </para>
/// <para>
/// <b>The cadence is a Day and it is derived rather than chosen.</b> A payday falls on a Day
/// boundary and nowhere else (<c>WageEngine.Sweep</c>'s modulo), so a Day is the shortest interval
/// over which the withheld column can differ from zero — and a row covering less than one would
/// split no sweep but would print rows that structurally cannot move. <c>MoneyDump</c> derives its
/// cadence from the shortest Policy interval for the same reason and gets a different number,
/// because it is watching a different mechanism.
/// </para>
/// </remarks>
internal static class IncomeDump
{
    /// <summary>How many budget rows to print before dropping to the tail.</summary>
    private const int Rows = 16;

    /// <summary>
    /// Runs a session on the given Ruleset and prints its per-Day budget.
    /// </summary>
    /// <param name="options">The parsed command line.</param>
    /// <param name="output">Where the picture goes.</param>
    /// <returns>0, or a non-zero code when the Ruleset states no income tax.</returns>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames names))
        {
            return 2;
        }

        if (Refuse(rules, output) is int refusal)
        {
            return refusal;
        }

        uint cadence = (uint)Ticks.PerDay;

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        // Capacity for every reading the run will take, on MoneyDump's reason: the first row is the
        // founding, and a series that had become a tail of itself would lose it.
        int readings = (int)((options.Ticks / cadence) + 2);
        Census census = new(world, readings);

        census.Observe(simulation);

        // 🔴 THE TWO FIGURES THE CENSUS CANNOT CARRY, accumulated here beside it rather than
        // inside it. Relief moved no Money, so it is not a flow and has no member of
        // MoneyFlowCounter to ride; what a subsidy CLAIMED is not what it paid, and the shortfall
        // creates no debt (plans/0072 D12), so it crossed no edge either. Both are read off the
        // per-sweep readings, which stand for exactly one Tick -- so they are summed every Tick and
        // never sampled on the cadence. ⚠ Sampling them on a Day would be correct today and wrong
        // the moment anything runs at a cadence a Day does not divide.
        long relieved = 0;
        long claimed = 0;
        long cut = 0;
        int rationedDays = 0;

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            relieved += simulation.LastProfitTax.Relieved;

            SubsidyReading subsidies = simulation.LastSubsidies;

            claimed += subsidies.Claimed;
            cut += subsidies.Rationed;

            if (subsidies.Rationed > 0)
            {
                rationedDays++;
            }

            if (simulation.Tick.Raw % cadence == 0)
            {
                census.Observe(simulation);
            }
        }

        var catalogue = new Catalogue(relieved, claimed, cut, rationedDays);
        var window = new Ticks(options.Ticks);

        output.WriteLine("# Borough income dump — the city's budget");
        string sizing = F($"# {options.Citizens:N0} Citizens, {options.Ticks:N0} Ticks");
        string taken = F($"a reading every {cadence:N0} — {census.Count:N0} readings.");

        output.WriteLine($"{sizing}, {taken}");
        output.WriteLine();

        Schedule(output, rules, names);
        output.WriteLine();
        Budget(output, census, window, cadence, catalogue);

        return 0;
    }

    /// <summary>
    /// What the targeted catalogue did that no treasury flow can say.
    /// </summary>
    /// <remarks>
    /// <b>Three of these four numbers describe Money that never moved</b>, which is exactly why they
    /// are carried apart from the budget's columns rather than added to them. A relief reduces a
    /// bill, so its trace is a <em>smaller</em> profit-tax column; a rationed claim is not a debt,
    /// so its trace is nothing at all. ***Putting either in the identity would break it***, and
    /// leaving both out of the report would make a city that forwent half its profit tax
    /// indistinguishable from one whose shops traded at half the profit.
    /// </remarks>
    /// <param name="Relieved">Profit tax the reliefs forwent. Revenue forgone, never expenditure.</param>
    /// <param name="Claimed">What the subsidies were asked for, before any ceiling bound.</param>
    /// <param name="Cut">Claimant-Days paid less than they claimed.</param>
    /// <param name="RationedDays">Days on which the pot bound at all.</param>
    private readonly record struct Catalogue(
        long Relieved, long Claimed, long Cut, int RationedDays);

    /// <summary>
    /// The schedule the run was taxed under, printed above the table it produced.
    /// </summary>
    /// <remarks>
    /// <b>Printed because a withheld figure is uninterpretable without it.</b> A column of tax says
    /// nothing about whether the mechanism is working; the same column beside the bands it came from
    /// can be checked by hand against a wage, which is the only way anybody finds out that the rates
    /// are being applied to something other than what they think. ⚠ <b>It is the <em>authored</em>
    /// schedule and not necessarily the one in force</b> — the world keeps a history of Days and a
    /// player can move a rate, so a run in which anything issued a rate command would be taxed under
    /// a schedule this block does not show.
    /// </remarks>
    private static void Schedule(TextWriter output, Ruleset rules, RulesetNames names)
    {
        IncomeTaxSchedule schedule = rules.IncomeTax;

        output.WriteLine("What the Ruleset levies");
        output.WriteLine();
        output.WriteLine(F(
            $"  allowance          {schedule.AllowancePerDay:N0} a Day, taxed at nothing"));
        output.WriteLine(F(
            $"  middle band        to {schedule.UpperThresholdPerDay:N0} a Day, at {schedule.MiddleRatePercent:N0}%"));
        output.WriteLine(F(
            $"  upper band         above {schedule.UpperThresholdPerDay:N0} a Day, at {schedule.UpperRatePercent:N0}%"));
        output.WriteLine();
        output.WriteLine(
            "  Marginal, so a rate reaches only the earnings inside its own band and crossing a");
        output.WriteLine(
            "  threshold never reprices what came before it. The bands are read against ONE DAY's");
        output.WriteLine(
            "  earnings: a payment closing several Days is spread across them first, so a weekly");
        output.WriteLine(
            "  wage is taxed as seven Days and not as one enormous one.");

        Profits(output, rules);

        foreach ((byte kind, BusinessKindDefinition trade) in Trades(rules))
        {
            string trading = names.BusinessKind(kind)
                ?? kind.ToString(CultureInfo.InvariantCulture);

            output.WriteLine();
            output.WriteLine(F(
                $"  A \"{trading}\" posts {trade.WagePerDay:N0} a Day and pays every {trade.PayPeriodDays:N0} Days."));
        }
    }

    /// <summary>
    /// The profit-tax bands, printed under the income-tax ones for the same reason they are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two marginal bands and no allowance, and the absence is the decision</b> —
    /// <c>plans/0072</c> D8. An author wanting relief on modest profits writes a lower rate of zero
    /// and the threshold becomes the allowance, so a third key would be a second spelling of a city
    /// that is already writable. ⚠ <b>That is why this block prints a <em>lower band</em> where the
    /// income-tax block above prints an <em>allowance</em></b>: they are not the same shape, and a
    /// report that printed them as one would be teaching the wrong file format.
    /// </para>
    /// <para>
    /// ⚠ <b>A file stating no <c>[business_tax]</c> says so rather than printing three zeroes.</b>
    /// <c>--income</c> refuses a file with no <c>[income_tax]</c> because the column it exists to
    /// show would be empty; the profit tax is the fourth column and not the mode's subject, so an
    /// absence here is legible rather than fatal — and ***a stated absence and a schedule that takes
    /// nothing are different cities***, which three zeroes could not tell apart.
    /// </para>
    /// </remarks>
    private static void Profits(TextWriter output, Ruleset rules)
    {
        BusinessTaxSchedule schedule = rules.BusinessTax;

        output.WriteLine();

        if (!schedule.Levies)
        {
            output.WriteLine(
                "  This Ruleset states no [business_tax] that takes anything, so the profit tax");
            output.WriteLine(
                "  column below is zero throughout and no Business is ever assessed.");

            return;
        }

        output.WriteLine(F(
            $"  lower band         to {schedule.ThresholdPerDay:N0} profit a Day, at {schedule.LowerRatePercent:N0}%"));
        output.WriteLine(F(
            $"  upper band         above {schedule.ThresholdPerDay:N0} a Day, at {schedule.UpperRatePercent:N0}%"));
        output.WriteLine();
        output.WriteLine(
            "  Marginal too, and read against ONE DAY's PROFIT -- revenue recognised on the Day the");
        output.WriteLine(
            "  Goods were delivered, less the cost of that stock and less the whole of the Day's");
        output.WriteLine(
            "  gross wage bill, whether or not that Day was a payday. A loss is simply an untaxed");
        output.WriteLine(
            "  Day and there is no carry-forward, and a till that cannot cover its bill pays what");
        output.WriteLine(
            "  it has and the rest is forgiven -- so the column is what was COLLECTED and never");
        output.WriteLine(
            "  what was assessed. There is no allowance key: a lower rate of 0 is how one is spelt.");
    }

    /// <summary>Every declared trade that actually posts a wage, in declaration order.</summary>
    /// <remarks>
    /// <b>Both halves of the test, because either alone admits a trade that pays nobody.</b>
    /// <c>WageEngine.Sweep</c> skips a kind whose wage or whose period is zero, so a trade failing
    /// one of them contributes nothing to the withheld column and printing it beside the schedule
    /// would invite a reader to check the arithmetic against a wage that is never paid.
    /// </remarks>
    private static IEnumerable<(byte Kind, BusinessKindDefinition Trade)> Trades(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.BusinessKindCount; kind++)
        {
            BusinessKindDefinition trade = rules.BusinessKind((byte)kind);

            if (trade.WagePerDay > 0 && trade.PayPeriodDays > 0)
            {
                yield return ((byte)kind, trade);
            }
        }
    }

    /// <summary>
    /// The budget: income in four columns, expenditure in three, and the two levels they moved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eleven columns and no twelfth that adds any of them up.</b> The treasury column is already the
    /// running consequence of the five flows, so a net column would be its first difference and
    /// would carry nothing the table does not have — while costing the reader the one distinction
    /// the table exists to make. <b>The households column is the counterparty</b>: a budget that
    /// showed only the treasury could not tell a city taxing a growing population from one taxing a
    /// shrinking one harder.
    /// </para>
    /// <para>
    /// 🔴 <b>That the treasury column IS the running total is a property the table now has and did
    /// not.</b> The rule columns were the missing term — see
    /// <c>MoneyFlowCounter.RuleToTreasury</c> — and the identity is asserted rather than asserted
    /// <em>about</em>: <c>TreasuryFlowsExplainTheBalanceTests</c> holds a run on
    /// <c>rulesets/taxing.toml</c> to <em>change in balance == income − expenditure</em> over every
    /// interval of it.
    /// </para>
    /// </remarks>
    private static void Budget(
        TextWriter output, Census census, Ticks window, uint cadence, Catalogue catalogue)
    {
        Series treasury = census.Series(Metric.Of(MoneyCounter.Treasury), window);
        Series households = census.Series(Metric.Of(MoneyCounter.Households), window);
        Series withheld = census.Series(Metric.Of(MoneyFlowCounter.Withheld, Aggregate.Sum), window);
        Series profits = census.Series(Metric.Of(MoneyFlowCounter.ProfitTax, Aggregate.Sum), window);
        Series levied = census.Series(Metric.Of(MoneyFlowCounter.ToTreasury, Aggregate.Sum), window);
        Series spent =
            census.Series(Metric.Of(MoneyFlowCounter.FromTreasury, Aggregate.Sum), window);
        Series ruled =
            census.Series(Metric.Of(MoneyFlowCounter.RuleToTreasury, Aggregate.Sum), window);
        Series drawn =
            census.Series(Metric.Of(MoneyFlowCounter.RuleFromTreasury, Aggregate.Sum), window);
        Series granted =
            census.Series(Metric.Of(MoneyFlowCounter.Subsidy, Aggregate.Sum), window);
        Series placed =
            census.Series(Metric.Of(MoneyFlowCounter.Placement, Aggregate.Sum), window);

        ReadOnlySpan<CensusSample> levels = treasury.Samples.Span;
        var columns = new Columns(
            households.Samples.Span,
            withheld.Samples.Span,
            profits.Samples.Span,
            levied.Samples.Span,
            ruled.Samples.Span,
            spent.Samples.Span,
            drawn.Samples.Span,
            granted.Samples.Span,
            placed.Samples.Span);

        output.WriteLine(F($"Income and expenditure — one row per {cadence:N0} Ticks, which is a Day"));
        output.WriteLine();

        string header = Row(
            "tick", "withheld", "profit tax", "policy in", "rule in", "policy out", "rule out",
            "subsidy out", "placement out", "treasury", "households");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        if (levels.IsEmpty)
        {
            output.WriteLine("  nothing was read.");
            return;
        }

        // The head and the tail, on MoneyDump's reason: a long run's middle is the part a reader
        // skips, and a table that says it elided it is honest where a truncated one is not.
        int shown = levels.Length <= Rows ? levels.Length : Rows / 2;

        for (int i = 0; i < shown; i++)
        {
            WriteBudgetRow(output, levels, columns, i);
        }

        if (levels.Length > Rows)
        {
            output.WriteLine(F($"  … {levels.Length - Rows:N0} readings not shown …"));

            for (int i = levels.Length - (Rows / 2); i < levels.Length; i++)
            {
                WriteBudgetRow(output, levels, columns, i);
            }
        }

        long income = Total(columns.Withheld);
        long profit = Total(columns.Profits);
        long moved = Total(columns.Levied);
        long paidIn = Total(columns.Ruled);
        long out_ = Total(columns.Spent);
        long drawnOut = Total(columns.Drawn);
        long grantedOut = Total(columns.Granted);
        long placedOut = Total(columns.Placed);

        output.WriteLine();
        output.WriteLine(F(
            $"  Into the treasury: {income:N0} withheld from wages, {profit:N0} in profit tax, {moved:N0} by a Policy, {paidIn:N0} by a Bin Rule."));
        output.WriteLine(F(
            $"  Out of it: {out_:N0} by a Policy, {drawnOut:N0} by a Bin Rule, {grantedOut:N0} in subsidy, {placedOut:N0} on placements."));
        Catalogued(output, catalogue);
        output.WriteLine(
            "  The eight are printed apart and never netted. A net cannot say whether a city taxed");
        output.WriteLine(
            "  nothing and paid nothing or taxed heavily and paid it all back; and within the");
        output.WriteLine(
            "  income, a withholding, a profit tax, a levy and a rates bill are the same arrival");
        output.WriteLine(
            "  through four levers -- a rate in [income_tax], a rate in [business_tax], an amount in");
        output.WriteLine(
            "  a [[policy]], and an output term in a [[rule]]. The two taxes also fall on different");
        output.WriteLine(
            "  payers: one leaves a wage on its way to a purse, the other leaves a till that was");
        output.WriteLine(
            "  already holding it.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ `policy in` is every Policy that pays INTO the treasury, a plain transfer and a");
        output.WriteLine(
            "  tool = \"charge\" alike: PolicyEngine folds both into one accumulator, so on a file");
        output.WriteLine(
            "  whose only inbound Policy is a charge this column IS the charge, and on any other");
        output.WriteLine(
            "  file it is not. `subsidy out` is apart from `policy out` because the money never");
        output.WriteLine(
            "  passes through that sweep at all -- a subsidy is apportioned against a ceiling by");
        output.WriteLine(
            "  SubsidyEngine, and until it had a column of its own the treasury fell by money no");
        output.WriteLine(
            "  flow here could name.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ `placement out` is the one column that pays NOBODY. A service Building placed by");
        output.WriteLine(
            "  hand costs the treasury its kind's placement_cost, and that money buys imported");
        output.WriteLine(
            "  Materials -- so it leaves the money supply rather than arriving in another Bin. It is");
        output.WriteLine(
            "  expenditure because the balance fell by it, and a column because nothing else would");
        output.WriteLine(
            "  name it.");
        output.WriteLine();
        output.WriteLine(
            "  The treasury column is these eight columns' running total, and nothing else reaches");
        output.WriteLine(
            "  it: every unit of the balance is explained by the flows printed beside it.");
    }

    /// <summary>
    /// Every column of the budget but the tick and the treasury, which the caller already holds.
    /// </summary>
    /// <remarks>
    /// <b>A carrier so that a row writer takes three arguments rather than ten.</b> A <c>ref
    /// struct</c> because every member is a <see cref="ReadOnlySpan{T}"/> over the census's own
    /// storage — nothing is copied, and nothing outlives the reading it was taken from.
    /// </remarks>
    private readonly ref struct Columns(
        ReadOnlySpan<CensusSample> homes,
        ReadOnlySpan<CensusSample> withheld,
        ReadOnlySpan<CensusSample> profits,
        ReadOnlySpan<CensusSample> levied,
        ReadOnlySpan<CensusSample> ruled,
        ReadOnlySpan<CensusSample> spent,
        ReadOnlySpan<CensusSample> drawn,
        ReadOnlySpan<CensusSample> granted,
        ReadOnlySpan<CensusSample> placed)
    {
        public ReadOnlySpan<CensusSample> Homes { get; } = homes;

        public ReadOnlySpan<CensusSample> Withheld { get; } = withheld;

        /// <summary>What the Day-boundary collection took off the Businesses' tills.</summary>
        public ReadOnlySpan<CensusSample> Profits { get; } = profits;

        public ReadOnlySpan<CensusSample> Levied { get; } = levied;

        public ReadOnlySpan<CensusSample> Ruled { get; } = ruled;

        public ReadOnlySpan<CensusSample> Spent { get; } = spent;

        public ReadOnlySpan<CensusSample> Drawn { get; } = drawn;

        /// <summary>
        /// What the subsidy sweeps apportioned out of the treasury — the third expenditure path.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Its own column and not part of <see cref="Spent"/></b>, because the money does not
        /// pass through <c>PolicyEngine.Move</c> at all: a subsidy gathers every claim, takes the
        /// smaller of its ceiling and the treasury, and cuts everybody in proportion. Folding it in
        /// would keep the identity and lose the one lever a player actually turns, which is the
        /// <c>ceiling</c>.
        /// </remarks>
        public ReadOnlySpan<CensusSample> Granted { get; } = granted;

        /// <summary>
        /// What the city paid to place service Buildings by hand — the fourth expenditure path.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The one column here whose money reaches nobody.</b> A placement's price buys
        /// imported Materials, so it leaves the money supply rather than another Bin — and it is
        /// still expenditure, because the treasury's balance fell by it. Folding it into
        /// <see cref="Spent"/> would keep the identity and lose the lever, which is a
        /// <c>[[building]]</c> kind's price rather than a <c>[[policy]]</c>'s amount.
        /// </remarks>
        public ReadOnlySpan<CensusSample> Placed { get; } = placed;
    }

    /// <summary>
    /// What the catalogue did beside the budget: revenue forgone, and claims the pot could not meet.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Printed BELOW the identity and outside it, and the placement is the whole of what this
    /// block is for.</b> Relief is a tax the city chose not to collect: no Bin was debited, no Bin
    /// was credited, and the treasury is exactly where it would be if the reliefs had never been
    /// declared and the shops had simply earned less. ***Adding it to expenditure would break the
    /// balance by precisely the amount forgone*** — and it is an easy mistake to make, because a
    /// relief is the one tool in the catalogue whose effect a player feels as spending.
    /// </remarks>
    private static void Catalogued(TextWriter output, Catalogue catalogue)
    {
        output.WriteLine(F(
            $"  Claimed in subsidy: {catalogue.Claimed:N0}, of which {catalogue.Cut:N0} claimant-Days were cut by the ceiling, on {catalogue.RationedDays:N0} Days."));
        output.WriteLine(F(
            $"  Revenue forgone: {catalogue.Relieved:N0} in profit-tax relief."));
        output.WriteLine(
            "  ⚠ Neither of those two lines is in the arithmetic above, and neither may be. A");
        output.WriteLine(
            "  relief moves NO money -- it reduces a bill before the collection, so its only trace");
        output.WriteLine(
            "  is a smaller profit tax column -- and a claim the ceiling cut is not a debt, so the");
        output.WriteLine(
            "  city owes nothing for it. Revenue forgone is not expenditure.");
    }

    private static void WriteBudgetRow(
        TextWriter output, ReadOnlySpan<CensusSample> levels, in Columns columns, int i)
    {
        output.WriteLine(Row(
            F($"{levels[i].Tick.Raw:N0}"),
            Cell(columns.Withheld, i),
            Cell(columns.Profits, i),
            Cell(columns.Levied, i),
            Cell(columns.Ruled, i),
            Cell(columns.Spent, i),
            Cell(columns.Drawn, i),
            Cell(columns.Granted, i),
            Cell(columns.Placed, i),
            Count(levels[i].Value),
            Cell(columns.Homes, i)));
    }

    /// <summary>One cell, or an em dash where that column is shorter than the treasury's.</summary>
    private static string Cell(ReadOnlySpan<CensusSample> column, int i) =>
        i < column.Length ? Count(column[i].Value) : "—";

    private static long Total(ReadOnlySpan<CensusSample> samples)
    {
        long total = 0;

        foreach (CensusSample sample in samples)
        {
            total += sample.Value;
        }

        return total;
    }

    /// <summary>
    /// Refuses a Ruleset that levies no income tax, and says which one does.
    /// </summary>
    /// <remarks>
    /// <b><c>MoneyDump.Refuse</c>'s polarity, and it is the harder of the two refusals to skip.</b>
    /// A file stating no <c>[income_tax]</c> withholds nothing, so the withheld column would be a
    /// column of zeroes — and ***a zero flow reads as a broken mechanism rather than as a file that
    /// levies no tax***. That is the one reading a budget must not be able to print, because the
    /// mechanism it would be blamed on is exactly the one this picture exists to show working.
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        if (rules.IncomeTax.Levies)
        {
            return null;
        }

        output.WriteLine(
            "This Ruleset states no [income_tax] that takes anything, so nothing is ever withheld "
            + "and the income half of the budget would be a column of zeroes. A flow of nothing "
            + "reads as a broken payday rather than as a file that levies no tax, which is the one "
            + "reading a budget must not be able to print. An income tax is content.");
        output.WriteLine();
        output.WriteLine(
            "  --income --ruleset rulesets/taxing.toml --citizens 2000 --ticks 24576");

        return 3;
    }

    private static string Row(
        string label, string a, string b, string c, string d, string e, string f, string g,
        string h, string i, string j) =>
        F($"{label,-10}  {a,11}  {b,11}  {c,11}  {d,11}  {e,11}  {f,11}  {g,11}  {h,13}  {i,14}  {j,14}");

    private static string Count(long value) => F($"{value:N0}");

    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
