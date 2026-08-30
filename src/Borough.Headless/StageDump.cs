namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

/// <summary>
/// The city's age structure, Day by Day: who is in which Life Stage, and whether the cohort blurs.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0046</c> stages 1 and 2's "something to look at", and the amnesty amended what that phrase
/// is allowed to mean.</b> <c>plans/0045</c> struck <em>"there is something to look at showing the
/// milestone doing its job"</em> because a column of hexadecimal was satisfying it, and replaced it
/// with ***done means you watched it happen and something surprised you***. A State Hash trace off
/// <c>aged.toml</c> would move, and would say nothing about whether the mechanism is behaving.
/// </para>
/// <para>
/// 🔴 <b>The specific question is whether the founding generation's echo ever damps</b>, and it is
/// the one <c>plans/0046</c> names as the first number that will move. Every Household in a synthetic
/// city is created on Tick 0, so without <c>spread_days</c> they would all leave every stage together
/// for ever — a demographic wave that reads as a mechanism and is an artefact of world creation.
/// <c>adr/0011</c>'s <c>W</c> exists to smear it. ***Whether four windows of 8–16 Days blur a cohort
/// across 160 Days of life is not something the plan could settle by reasoning***, so this dump is
/// how it gets settled.
/// </para>
/// <para>
/// 🔴 <b>The population column FALLS TO ZERO, and that is stage 2 rather than a defect.</b> A
/// Household reaching a terminal Life Stage dissolves, and nothing is born until stage 3 — so this is
/// a sink with no source and the city empties. ⚠ <b>This paragraph said the opposite for one commit</b>
/// — <em>"the column is expected to be FLAT… a reader who finds this column interesting has found a
/// bug"</em> — and it was correct for exactly as long as stage 1 was the whole mechanism. ***A
/// description written against a half-built mechanism reads as a claim about the city***, which is
/// <c>adr/0093</c>'s failure mode from the inside.
/// </para>
/// <para>
/// ⚠ <b>The two series are printed apart and must not be summed.</b> An advance moves a Household
/// along the chain; a dissolution removes it. A single "transitions" figure would have spikes that
/// could be either, and they answer different questions: whether the founding cohort blurs, and
/// whether the city dies in a wave.
/// </para>
/// </remarks>
internal static class StageDump
{
    /// <summary>How many Day rows the trajectory table prints before it starts thinning.</summary>
    /// <remarks>
    /// <b>A cap on the PICTURE and never on the run.</b> Every Day is sampled; what this bounds is
    /// how many rows a reader is shown, because a 500-Day run is 500 rows and the shape is what
    /// matters rather than the individual Day. Past it the table strides, and the stride is printed
    /// in the header so nobody reads a thinned table as a dense one.
    /// </remarks>
    private const int Rows = 40;

    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames names))
        {
            return 2;
        }

        if (!rules.DeclaresLifeStages)
        {
            output.WriteLine(
                "This Ruleset declares no [[life_stage]], so no Household ever advances one and "
                + "every row of this dump would be the stage SyntheticCity handed it at creation. "
                + "A histogram of an initialiser is not a reading. Demographics are content.");
            output.WriteLine();
            output.WriteLine("  --stages --ruleset rulesets/aged.toml --citizens 2000 --ticks 400000");

            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        int count = rules.LifeStageCount;
        List<int[]> days = [Histogram(world, count)];
        var advanced = new List<int>() { 0 };
        var dissolved = new List<int>() { 0 };
        var born = new List<int>() { 0 };
        var spawned = new List<int>() { 0 };
        var bore = new List<int>() { 0 };

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            // 🔴 GATED ON THE TICK THAT JUST RAN AND NOT ON simulation.Tick, AND THE FIRST SPELLING
            // OF THIS LOOP GOT IT WRONG. Step runs tick T and then advances, so simulation.Tick reads
            // T+1 by the time control comes back -- and `simulation.Tick % PerDay == 0` therefore
            // fires one Tick BEFORE each Day boundary rather than on it.
            //
            // ⚠ THAT MISTAKE IS INVISIBLE IN EVERY OTHER DUMP AND FATAL HERE, which is the thing
            // worth carrying away. A LEVEL -- a price, a stock, a Lot count -- is the same one Tick
            // either side of a boundary, so MarketDump samples exactly this way and is correct. A
            // FLOW is not: Simulation.LastLifeStages means *what the sweep did on the Tick just run*
            // and is `default` on the other 2,047, so reading it one Tick early returns zero for
            // ever. The histogram moved the whole time and the transition column stayed empty.
            //
            // ***A reading that says nothing happened, beside a picture that shows it happening, is
            // the shape to distrust*** -- the two came off the same run.
            if (tick % (ulong)Ticks.PerDay == 0UL)
            {
                days.Add(Histogram(world, count));
                advanced.Add(simulation.LastLifeStages.Advanced);
                dissolved.Add(simulation.LastLifeStages.Dissolved);
                born.Add(simulation.LastLifeStages.Born);
                spawned.Add(simulation.LastLifeStages.Spawned);
                bore.Add(simulation.LastLifeStages.Bore);
            }
        }

        Header(options, rules, names, days.Count - 1, output);
        output.WriteLine();
        Trajectory(days, advanced, dissolved, born, names, rules, output);
        output.WriteLine();
        Echo(advanced, dissolved, born, bore, spawned, days, output);

        return 0;
    }

    /// <summary>How many live Households sit in each stage right now.</summary>
    /// <remarks>
    /// <b>Index 0 is <em>no stage</em> and is printed rather than dropped.</b> A Household carrying
    /// zero in a world that declares stages is one nothing armed — which under stage 1 should be
    /// none, and a non-zero column here is the first thing to distrust.
    /// </remarks>
    private static int[] Histogram(World world, int count)
    {
        var tally = new int[count + 1];
        HouseholdTable households = world.Households;

        for (int slot = 0; slot < households.Rows.SlotCount; slot++)
        {
            if (!households.Rows.IsLive(slot))
            {
                continue;
            }

            byte stage = households.LifeStage[slot];

            tally[stage <= count ? stage : 0]++;
        }

        return tally;
    }

    private static void Header(
        Options options, Ruleset rules, RulesetNames names, int days, TextWriter output)
    {
        output.WriteLine("# Borough Life Stage dump — the city's age structure, Day by Day");
        string sizing = F($"# {options.Citizens:N0} Citizens, {options.Ticks:N0} Ticks");
        output.WriteLine(F($"{sizing}, {days:N0} Days, {rules.LifeStageCount} stages."));
        output.WriteLine("#");
        output.WriteLine(
            "# The population column is expected to be FLAT: stage 1 advances a stage and does "
            + "nothing else.");
        output.WriteLine(
            "# Dissolution is plans/0046 stage 2 and generation is stage 3, so a city that grew or "
            + "shrank here");
        output.WriteLine("# would be a defect rather than a demography.");
        output.WriteLine();
        output.WriteLine("The chain, as this Ruleset authors it");
        output.WriteLine();
        output.WriteLine("stage                 floor   spread    longest   exits to");
        output.WriteLine(new string('-', 68));

        for (byte stage = 1; stage <= rules.LifeStageCount; stage++)
        {
            LifeStageDefinition definition = rules.LifeStage(stage);
            string next = definition.NextStage == 0
                ? "— terminal"
                : Named(names, rules, definition.NextStage);

            int longest = definition.DurationDays + definition.SpreadDays - 1;
            string label = F($"{Named(names, rules, stage),-20}  {definition.DurationDays,5}");

            output.WriteLine(F(
                $"{label}   {definition.SpreadDays,6}   {longest,8}   {next}"));
        }
    }

    /// <summary>The histogram over time, one row per Day, thinned past <see cref="Rows"/>.</summary>
    private static void Trajectory(
        List<int[]> days,
        List<int> advanced,
        List<int> dissolved,
        List<int> born,
        RulesetNames names,
        Ruleset rules,
        TextWriter output)
    {
        int stride = ((days.Count + Rows - 1) / Rows) is int s && s > 0 ? s : 1;

        output.WriteLine(stride == 1
            ? "Where the city is, one row per Day"
            : F($"Where the city is, one row per {stride} Days — the run is longer than the table"));
        output.WriteLine();

        output.Write("  Day  ");

        for (byte stage = 1; stage <= rules.LifeStageCount; stage++)
        {
            output.Write(Short(Named(names, rules, stage)));
        }

        output.WriteLine("   none   moved   ended    born    live");
        output.WriteLine(new string('-', 38 + (9 * rules.LifeStageCount)));

        for (int day = 0; day < days.Count; day += stride)
        {
            int[] tally = days[day];
            int live = 0;

            output.Write(F($"{day,5}  "));

            for (int stage = 1; stage <= rules.LifeStageCount; stage++)
            {
                output.Write(F($"{tally[stage],8} "));
                live += tally[stage];
            }

            live += tally[0];

            output.WriteLine(F(
                $"{tally[0],6}  {advanced[day],6}  {dissolved[day],6}  {born[day],6}  {live,6}"));
        }
    }

    /// <summary>
    /// The cohort question, answered numerically rather than left to the eye.
    /// </summary>
    /// <remarks>
    /// <b>What is printed is the DISPERSION of the transition count</b>, because that is what the
    /// echo is. A perfectly synchronised founding generation puts the whole city through one
    /// transition on one Day and nothing on the others, so the Day-to-Day series is a train of
    /// spikes; a fully blurred one is flat. ⚠ <b>The busiest Day as a multiple of the mean is the
    /// number to watch</b> — 1× is blurred, and the founding spike is however many Households the
    /// city started with.
    /// </remarks>
    private static void Echo(
        List<int> advanced,
        List<int> dissolved,
        List<int> born,
        List<int> bore,
        List<int> spawned,
        List<int[]> days,
        TextWriter output)
    {
        int moves = 0;
        int busiest = 0;
        int busiestDay = 0;
        int quiet = 0;

        for (int day = 1; day < advanced.Count; day++)
        {
            moves += advanced[day];

            if (advanced[day] > busiest)
            {
                busiest = advanced[day];
                busiestDay = day;
            }

            if (advanced[day] == 0)
            {
                quiet++;
            }
        }

        int elapsed = advanced.Count - 1;

        output.WriteLine("Does the founding cohort blur?");
        output.WriteLine();
        output.WriteLine(F($"  advances              {moves,8:N0} over {elapsed:N0} Days"));
        output.WriteLine(F($"  busiest Day           {busiest,8:N0} on Day {busiestDay:N0}"));
        output.WriteLine(F($"  Days with none        {quiet,8:N0} of {elapsed:N0}"));

        if (elapsed > 0 && moves > 0)
        {
            long mean = moves / elapsed;

            output.WriteLine(F($"  mean a Day            {mean,8:N0}"));
            output.WriteLine(mean > 0
                ? F($"  busiest ÷ mean        {busiest / (double)mean,8:N1}×")
                : "  busiest ÷ mean            —  fewer than one transition a Day on average");
        }

        output.WriteLine();
        output.WriteLine(
            "  A city whose founding generation never blurs puts every Household through one");
        output.WriteLine(
            "  transition on one Day and none on the others, so the series is a train of spikes");
        output.WriteLine(
            "  and `busiest ÷ mean` is large. adr/0011's spread_days exists to smear that; whether");
        output.WriteLine(
            "  it is enough is what plans/0046 says to come here and find out.");

        Deaths(dissolved, days, output);
        Replacement(born, bore, spawned, days, output);
    }

    /// <summary>
    /// <c>plans/0046</c> stage 2: how the city empties, and whether it empties all at once.
    /// </summary>
    /// <remarks>
    /// <b>A SEPARATE series from the advances above, and it has to be.</b> An advance moves a
    /// Household along the chain and a dissolution removes it; summing them would produce a
    /// "transitions" figure whose spikes could be either, and the two answer different questions —
    /// the first asks whether the founding cohort blurs, the second asks whether the city dies in a
    /// wave. ⚠ <b>Every Day here is one on which the population strictly FELL</b>, because nothing is
    /// born until stage 3.
    /// </remarks>
    private static void Deaths(List<int> dissolved, List<int[]> days, TextWriter output)
    {
        int total = 0;
        int busiest = 0;
        int busiestDay = 0;
        int emptied = -1;

        for (int day = 1; day < dissolved.Count; day++)
        {
            total += dissolved[day];

            if (dissolved[day] > busiest)
            {
                busiest = dissolved[day];
                busiestDay = day;
            }

            if (emptied < 0 && Live(days[day]) == 0)
            {
                emptied = day;
            }
        }

        output.WriteLine();
        output.WriteLine("How does the city empty?");
        output.WriteLine();
        output.WriteLine(F($"  dissolutions          {total,8:N0}"));
        output.WriteLine(F($"  busiest Day           {busiest,8:N0} on Day {busiestDay:N0}"));
        output.WriteLine(F($"  standing at the end   {Live(days[^1]),8:N0}"));
        output.WriteLine(emptied >= 0
            ? F($"  the city emptied on Day {emptied:N0}")
            : "  the city had not emptied when the run ended");

        output.WriteLine();
        output.WriteLine(
            "  A Household reaching a terminal Life Stage dissolves, its estate goes to the");
        output.WriteLine(
            "  treasury and its members are retired. ⚠ THIS NO LONGER EMPTIES THE CITY: stage 3");
        output.WriteLine(
            "  gave the sink a source, so read this panel against `births` below rather than on");
        output.WriteLine(
            "  its own. A city that empties now is one whose fertility band sits under 2.0.");
    }

    /// <summary>
    /// <c>plans/0046</c> stage 3: the readout the milestone owes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Two children per Household is EXACT replacement and it is not a constant anybody
    /// chose</b> — <c>adr/0011</c>: <em>"children become adults and form new Households, so two
    /// children per Household is exact Citizen replacement — two replacing two. That threshold falls
    /// out of conservation rather than being chosen."</em> ***So the number below is a reading of the
    /// city and the 2.0 beside it is arithmetic.***
    /// </para>
    /// <para>
    /// ⚠ <b>The denominator counts every Household that made the decision, INCLUDING the ones that
    /// drew zero.</b> Dividing by the Households that had at least one child would report the
    /// fertility of the fertile, and the city the ADR's stagnation spiral describes — one where
    /// expensive housing produces zero-child draws — would read as perfectly healthy.
    /// </para>
    /// <para>
    /// ⚠ <b>Spawned is not a birth and is printed to say so.</b> A birth creates a Citizen; a spawn
    /// moves one out of its parents' Household into its own. <c>adr/0011</c> makes the conservation
    /// claim on the second: ***"Citizen count is conserved across the spawn transition"***.
    /// </para>
    /// </remarks>
    private static void Replacement(
        List<int> born, List<int> bore, List<int> spawned, List<int[]> days, TextWriter output)
    {
        int children = 0;
        int decisions = 0;
        int left = 0;

        for (int day = 1; day < born.Count; day++)
        {
            children += born[day];
            decisions += bore[day];
            left += spawned[day];
        }

        output.WriteLine();
        output.WriteLine("Does the city replace itself?");
        output.WriteLine();
        output.WriteLine(F($"  births                {children,8:N0}"));
        output.WriteLine(F($"  fertility decisions   {decisions,8:N0}"));
        output.WriteLine(F($"  children left home    {left,8:N0}"));
        output.WriteLine(F($"  standing at the end   {Live(days[^1]),8:N0} Households"));

        if (decisions > 0)
        {
            output.WriteLine();
            output.WriteLine(F(
                $"  REPLACEMENT RATE      {children / (double)decisions,8:N2}  against 2.00 for exact"));
        }

        output.WriteLine();
        output.WriteLine(
            "  Two children per Household replaces two adults with two, so 2.00 is exact");
        output.WriteLine(
            "  replacement and it falls out of conservation rather than being chosen (adr/0011).");
        output.WriteLine(
            "  🔴 THE DRAW IS UNCONDITIONED: adr/0011 conditions fertility on housing cost,");
        output.WriteLine(
            "  dwelling size and job security, and none of that machinery is built. So a rate");
        output.WriteLine(
            "  below 2.00 here is THE BAND THE RULESET AUTHORED, not a city that cannot afford");
        output.WriteLine(
            "  children. Read it as a check that the arithmetic works, never as a diagnosis.");
    }

    private static int Live(int[] tally)
    {
        int live = 0;

        for (int stage = 0; stage < tally.Length; stage++)
        {
            live += tally[stage];
        }

        return live;
    }

    private static string Named(RulesetNames names, Ruleset rules, byte stage) =>
        names.LifeStage(stage) ?? F($"stage {stage}");

    private static string Short(string name) =>
        F($"{(name.Length > 8 ? name[..8] : name),8} ");

    private static string F(FormattableString text) =>
        text.ToString(CultureInfo.InvariantCulture);
}
