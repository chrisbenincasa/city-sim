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
/// The circular flow, printed: where the city's money is, and what moved it there.
/// </summary>
/// <remarks>
/// <para>
/// <b>A balance sheet is a level and a flow at once</b>, and the two halves of this dump are those
/// two things. The stocks answer <em>where is the money</em> at an instant; the circuit answers
/// <em>what moved</em> over an interval, in both directions, unnetted. Neither half is recoverable
/// from the other: a treasury that has not moved between two readings is a city that taxed nothing
/// and a city that taxed exactly what it paid out, and those are different cities.
/// </para>
/// <para>
/// ⚠ <b>The two money aggregates are reported separately because <c>01 §5.1</c> requires it</b> —
/// its trade-deficit row says of the second that it is <em>"a different bill — the money supply, not
/// the treasury"</em>. Insolvency is the treasury emptying; a trade deficit is the supply
/// contracting; one number cannot be both, and a picture showing one hides the one the endgame turns
/// on. Milestone 10 is a closed system, so the supply is flat for the whole of it and the flatness is
/// the evidence rather than a shortfall — <c>adr/0024</c> makes the Outside Connection money's only
/// source and sink and that is milestone 11. <b>A supply row that moves before then is a leak.</b>
/// </para>
/// <para>
/// <b>It reads the Census rather than the world</b>, which is what makes it a picture of a run rather
/// than of an instant. The money families landed in the Census for that reason — <c>plans/0033</c>
/// records the hole as <em>"no <c>Metric</c> member is a money one, so <c>01 §6</c>'s money supply
/// trajectory indicator is produced by nothing"</em> — and a dump that walked the Bins itself would
/// have left it exactly as open.
/// </para>
/// <para>
/// <b>The reading cadence is derived from the Ruleset, not chosen.</b> A row is one sweep round of
/// the shortest-interval Policy, so every row covers whole sweeps and the flow columns never split
/// one. A cadence chosen here would be a number with no ratifier under <c>adr/0052</c>; a cadence
/// read off the content is the content's number.
/// </para>
/// </remarks>
internal static class MoneyDump
{
    /// <summary>How many circuit rows to print before dropping to the tail.</summary>
    private const int Rows = 16;

    /// <summary>
    /// Runs a session on the given Ruleset and prints its balance sheet and its circuit.
    /// </summary>
    /// <param name="options">The parsed command line.</param>
    /// <param name="output">Where the picture goes.</param>
    /// <returns>0, or a non-zero code when the Ruleset cannot demonstrate a circular flow.</returns>
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

        uint cadence = Cadence(rules);

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        // Capacity for every reading the run will take, so no series is ever a tail of itself. The
        // whole point of the circuit table is that its first row is the founding.
        int readings = (int)((options.Ticks / cadence) + 2);
        Census census = new(world, readings);

        census.Observe(simulation);

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            if (simulation.Tick.Raw % cadence == 0)
            {
                census.Observe(simulation);
            }
        }

        var window = new Ticks(options.Ticks);

        output.WriteLine("# Borough money dump — the circular flow");
        string sizing = F($"# {options.Citizens:N0} Citizens, {options.Ticks:N0} Ticks");
        string taken = F($"a reading every {cadence:N0} — {census.Count:N0} readings.");

        output.WriteLine($"{sizing}, {taken}");
        output.WriteLine();

        Stocks(output, census, window, names, rules);
        output.WriteLine();
        Circuit(output, census, window, cadence);

        return 0;
    }

    /// <summary>
    /// The level half: where the money is, at the founding and now, with the identity below it.
    /// </summary>
    /// <remarks>
    /// <b><c>supply</c> and <c>held</c> are printed as a pair and then compared</b>, which is
    /// <c>Invariant.MoneyIsConserved</c> made visible. The invariant runs at end of run and reports a
    /// difference; this prints the two sides it reached the difference from, which is the half a
    /// failing assertion cannot show you.
    /// </remarks>
    private static void Stocks(
        TextWriter output, Census census, Ticks window, RulesetNames names, Ruleset rules)
    {
        output.WriteLine("Where the money is");
        output.WriteLine();

        string header = Row("", "at the founding", "now", "low", "high");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        long supply = 0;
        long held = 0;

        foreach ((MoneyCounter counter, string label) in CensusFamilies.MoneyCounters)
        {
            Series series = census.Series(Metric.Of(counter), window);
            ReadOnlySpan<CensusSample> samples = series.Samples.Span;

            if (samples.IsEmpty)
            {
                output.WriteLine(Row(label, "—", "—", "—", "—"));
                continue;
            }

            long low = samples[0].Value;
            long high = samples[0].Value;

            foreach (CensusSample sample in samples)
            {
                low = sample.Value < low ? sample.Value : low;
                high = sample.Value > high ? sample.Value : high;
            }

            if (counter == MoneyCounter.Supply)
            {
                supply = samples[^1].Value;
            }

            if (counter == MoneyCounter.Held)
            {
                held = samples[^1].Value;
            }

            // The three that decompose `held` are indented under it, because the report saying so is
            // cheaper than a reader working out which rows sum.
            string name = counter is MoneyCounter.Supply or MoneyCounter.Held ? label : "  " + label;

            output.WriteLine(Row(
                name, Count(samples[0].Value), Count(samples[^1].Value), Count(low), Count(high)));
        }

        string sides = F($"{supply:N0} issued, {held:N0} in the city");
        string gap = F($"a difference of {held - supply:N0}");

        output.WriteLine();
        output.WriteLine(
            supply == held
                ? $"  supply == held: {sides}. Conserved."
                : $"  ⚠ supply != held: {sides}, {gap}. adr/0031's invariant is violated.");

        output.WriteLine(
            "  The two are reported separately because 01 §5.1 does: insolvency is the treasury");
        output.WriteLine(
            "  emptying, a trade deficit is the supply contracting, and the supply is flat for the");
        output.WriteLine(
            "  whole of milestone 10 because the Outside Connection is milestone 11's (adr/0024).");

        string? money = MoneyName(rules, names);

        if (money is not null)
        {
            output.WriteLine();
            output.WriteLine(F($"  The conserved Resource is \"{money}\"."));
        }
    }

    /// <summary>
    /// The flow half: what moved through the treasury, per sweep round, in both directions.
    /// </summary>
    /// <remarks>
    /// <b>Unnetted, and the two columns are the whole reason this table exists.</b> The stock table
    /// above already shows the treasury's level at every reading, so a net column would be its first
    /// difference and would carry nothing new. What it cannot show is the gross either way, which is
    /// what separates a city that taxes and rebates from a city that does neither.
    /// </remarks>
    private static void Circuit(TextWriter output, Census census, Ticks window, uint cadence)
    {
        Series treasury = census.Series(Metric.Of(MoneyCounter.Treasury), window);
        Series households = census.Series(Metric.Of(MoneyCounter.Households), window);
        Series toTreasury = census.Series(Metric.Of(MoneyFlowCounter.ToTreasury, Aggregate.Sum), window);
        Series fromTreasury =
            census.Series(Metric.Of(MoneyFlowCounter.FromTreasury, Aggregate.Sum), window);

        ReadOnlySpan<CensusSample> levels = treasury.Samples.Span;
        ReadOnlySpan<CensusSample> homes = households.Samples.Span;
        ReadOnlySpan<CensusSample> collected = toTreasury.Samples.Span;
        ReadOnlySpan<CensusSample> paid = fromTreasury.Samples.Span;

        output.WriteLine(F($"What moved — one row per {cadence:N0} Ticks, the shortest sweep interval"));
        output.WriteLine();

        string header = Row("tick", "to treasury", "from treasury", "treasury", "households");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        if (levels.IsEmpty)
        {
            output.WriteLine("  nothing was read.");
            return;
        }

        // The head and the tail, because a long run's middle is the part a reader skips and a table
        // that elides it says so where a truncated one does not.
        int shown = levels.Length <= Rows ? levels.Length : Rows / 2;

        for (int i = 0; i < shown; i++)
        {
            WriteCircuitRow(output, levels, homes, collected, paid, i);
        }

        if (levels.Length > Rows)
        {
            output.WriteLine(F($"  … {levels.Length - Rows:N0} readings not shown …"));

            for (int i = levels.Length - (Rows / 2); i < levels.Length; i++)
            {
                WriteCircuitRow(output, levels, homes, collected, paid, i);
            }
        }

        long movedIn = Total(collected);
        long movedOut = Total(paid);

        string gross = F($"{movedIn:N0} moved to the treasury and {movedOut:N0} moved out of it");
        string net = F($"a net of {movedIn - movedOut:N0}");

        output.WriteLine();
        output.WriteLine($"  Over the run: {gross} — {net}.");
        output.WriteLine(
            "  Both directions are printed because a net cannot separate a city that taxed nothing");
        output.WriteLine("  from one that taxed heavily and paid it all back.");
    }

    private static void WriteCircuitRow(
        TextWriter output,
        ReadOnlySpan<CensusSample> levels,
        ReadOnlySpan<CensusSample> homes,
        ReadOnlySpan<CensusSample> collected,
        ReadOnlySpan<CensusSample> paid,
        int i)
    {
        output.WriteLine(Row(
            F($"{levels[i].Tick.Raw:N0}"),
            i < collected.Length ? Count(collected[i].Value) : "—",
            i < paid.Length ? Count(paid[i].Value) : "—",
            Count(levels[i].Value),
            i < homes.Length ? Count(homes[i].Value) : "—"));
    }

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
    /// The shortest Policy interval in the Ruleset: one sweep round, and the dump's reading cadence.
    /// </summary>
    private static uint Cadence(Ruleset rules)
    {
        uint shortest = 0;

        foreach (PolicyDefinition policy in rules.Policies)
        {
            if (shortest == 0 || policy.Interval < shortest)
            {
                shortest = policy.Interval;
            }
        }

        return shortest;
    }

    /// <summary>The name of the first conserved Resource, or null when the names were not kept.</summary>
    private static string? MoneyName(Ruleset rules, RulesetNames names)
    {
        foreach (PolicyDefinition policy in rules.Policies)
        {
            if (rules.IsConserved(policy.Resource))
            {
                return names.Resource(policy.Resource);
            }
        }

        return null;
    }

    /// <summary>
    /// Refuses a Ruleset that cannot show a circular flow, and says which one can.
    /// </summary>
    /// <remarks>
    /// <b><c>EvidenceDump</c>'s polarity exactly.</b> A city on <c>minimal.toml</c> has no money in it
    /// at all — every Household holds zero, because the only issuance is <c>[households]
    /// opening_balance_min/max</c> — so the picture would be six rows of zero under a heading that
    /// says money is conserved. ***A conservation identity that holds vacuously reads exactly like one
    /// that holds***, which is the one failure a balance sheet must not be able to print.
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        if (rules.Policies.Length == 0)
        {
            output.WriteLine(
                "This Ruleset declares no [[policy]], so nothing ever moves money and the circuit "
                + "would be a table of zeroes. A flow of nothing reads as a broken sweep rather than "
                + "as a file that authors no circuit. A Policy is content.");
            output.WriteLine();
            output.WriteLine("  --money --ruleset rulesets/taxed.toml --ticks 8192");

            return 3;
        }

        foreach (PolicyDefinition policy in rules.Policies)
        {
            if (rules.IsConserved(policy.Resource))
            {
                return null;
            }
        }

        output.WriteLine(
            "This Ruleset's Policies move no conserved Resource, so there is no money in the "
            + "picture and every row of the balance sheet would be zero. A conservation identity "
            + "that holds vacuously reads exactly like one that holds, which is the one thing a "
            + "balance sheet must not be able to print.");
        output.WriteLine();
        output.WriteLine("  --money --ruleset rulesets/taxed.toml --ticks 8192");

        return 3;
    }

    private static string Row(string label, string a, string b, string c, string d) =>
        F($"{label,-18}  {a,17}  {b,15}  {c,13}  {d,13}");

    private static string Count(long value) => F($"{value:N0}");

    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
