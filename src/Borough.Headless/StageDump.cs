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
        List<Labour> labour = [Market(world)];
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
                labour.Add(Market(world));
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
        Echo(advanced, dissolved, born, bore, spawned, days, labour[^1].Workers, output);
        Work(labour, output);

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
            "# The population column MOVES, and every stage of plans/0046 changed which way. It was "
            + "flat while");
        output.WriteLine(
            "# stage 1 only advanced a stage, fell to zero while stage 2 was a sink with no source, "
            + "and now");
        output.WriteLine(
            "# rises and falls. ⚠ A DESCRIPTION WRITTEN AGAINST A HALF-BUILT MECHANISM READS AS A "
            + "CLAIM ABOUT");
        output.WriteLine(
            "# THE CITY — this line said FLAT for two commits after it stopped being true, printed "
            + "above a");
        output.WriteLine("# column that was visibly doing something else.");
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
        int adults,
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
            "  and `busiest ÷ mean` is large. adr/0011's spread_days exists to smear that.");
        output.WriteLine();
        output.WriteLine(
            "  ✅ ANSWERED 2026-08-31, AND THE ANSWER WAS NO UNTIL THE WINDOWS WERE WIDENED. aged.toml");
        output.WriteLine(
            "  authored four windows of 8-16 Days against a ~160-Day life and read 7.0x here, with 116");
        output.WriteLine(
            "  of 400 Days seeing no transition at all. Each window is now as wide as its own floor and");
        output.WriteLine(
            "  it reads 3.3x, with 19 Days of 400 quiet.");
        output.WriteLine();
        output.WriteLine(
            "  🔴 THE WINDOW COULD NOT BE WIDENED ALONE, AND THAT IS THE FINDING. A stage's wake is drawn");
        output.WriteLine(
            "  uniform on [floor, floor + width), so a MEAN life is floor + width/2 — and aged.toml's");
        output.WriteLine(
            "  mean was already 188 Days against adr/0094's ~190 ceiling for a readable Replacement");
        output.WriteLine(
            "  Rate. Widening the windows at fixed floors would have put it at 236 and broken that");
        output.WriteLine(
            "  argument. ***The floors were halved to pay for the widths***, and the mean came back to");
        output.WriteLine(
            "  the 160 the file's own header always claimed and never had.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ IT DAMPS THE ECHO AND DOES NOT REMOVE IT, AND 400 DAYS CANNOT SEE WHICH. Over 1,200");
        output.WriteLine(
            "  Days the widened city converges — the population swing falls 3.67x, 2.55x, 1.19x over");
        output.WriteLine(
            "  three 400-Day thirds — where the narrow one is still swinging 2.47x in its last third.");
        output.WriteLine(
            "  ***A 400-Day run holds two and a half generations and cannot tell a damping echo from a");
        output.WriteLine(
            "  standing one.***");

        Deaths(dissolved, days, output);
        Replacement(born, bore, spawned, days, adults, output);
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
            "  its own. A city that empties now is one whose fertility band sits under the threshold");
        output.WriteLine(
            "  the next panel MEASURES — which is one child per adult here, and not the 2.00 this line");
        output.WriteLine(
            "  used to name.");
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
        List<int> born,
        List<int> bore,
        List<int> spawned,
        List<int[]> days,
        int adults,
        TextWriter output)
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
        int households = Live(days[^1]);
        double perHousehold = households == 0 ? 0 : adults / (double)households;

        output.WriteLine(F($"  standing at the end   {households,8:N0} Households"));
        output.WriteLine(F(
            $"  adults per Household  {perHousehold,8:N2}   at the end of the run"));

        if (decisions > 0)
        {
            output.WriteLine();
            double rate = children / (double)decisions;

            output.WriteLine(F(
                $"  REPLACEMENT RATE      {rate,8:N2}   against {perHousehold:N2} for exact"));
        }

        output.WriteLine();
        output.WriteLine(
            "  🔴 THE THRESHOLD IS THE COLUMN ABOVE IT, AND IT READ A FLAT 2.00 UNTIL 2026-08-31.");
        output.WriteLine(
            "  adr/0011 derives exact replacement as TWO children per Household — \"two children");
        output.WriteLine(
            "  replacing two adults\" — and that is airtight for a Household of two adults. It is");
        output.WriteLine(
            "  wrong by a factor of two for this city, because World.SpawnChildren gives EVERY CHILD");
        output.WriteLine(
            "  ITS OWN HOUSEHOLD: nothing in the build pairs anybody, so a formed Household holds");
        output.WriteLine(
            "  exactly one adult and exact replacement is ONE CHILD PER ADULT.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ THE CENSUS IS WHAT SAYS SO RATHER THAN THE ARITHMETIC. `adults per Household` is");
        output.WriteLine(
            "  measured, not derived, and on any run past the founding generation it reads 1.00 —");
        output.WriteLine(
            "  `working age` and the Household count are EXACTLY EQUAL. Watch it move if a pairing");
        output.WriteLine(
            "  mechanism ever lands; the threshold follows it with no edit here.");
        output.WriteLine();
        output.WriteLine(
            "  🔴 SO A RATE OF ~1.45 IS A CITY THAT GROWS 45% A GENERATION, and it was reported for");
        output.WriteLine(
            "  weeks as one in decline. Over 1,200 Days on aged.toml the Households go 720 -> ~1,100");
        output.WriteLine(
            "  and settle, bounded by the housing the lattice paved and by [placement]");
        output.WriteLine(
            "  gives_up_after_days, not by fertility. ***A 400-Day run reads as decline because it");
        output.WriteLine(
            "  catches the first trough***, which is the founding generation of TWO-adult Households");
        output.WriteLine(
            "  being replaced by one-adult ones.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ WHETHER CHILDREN SHOULD PAIR IS DESIGN AND IT IS NOT SETTLED HERE. adr/0011 clearly");
        output.WriteLine(
            "  assumes they do; nothing says who pairs with whom, and this project has no marriage");
        output.WriteLine(
            "  market. It is queued in plans/0045's `Owed when the freeze lifts`.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ AND THE DRAW IS UNCONDITIONED EITHER WAY: adr/0011 conditions fertility on housing");
        output.WriteLine(
            "  cost, dwelling size and job security, and none of that machinery is built. So a rate");
        output.WriteLine(
            "  either side of the threshold is THE BAND THE RULESET AUTHORED, never a city that can");
        output.WriteLine(
            "  or cannot afford children.");
    }

    /// <summary>One Day's labour market: who can work, and how many posts stand for them.</summary>
    /// <remarks>
    /// <b>Four levels rather than a ratio, because a ratio cannot be re-divided.</b> Whoever reads
    /// this wants <em>posts per worker</em>, but they may also want the unemployment level, the
    /// child share, or the vacancy count — and every one of those is a different pair out of these
    /// four. Storing the quotient would keep one reading and discard the other three.
    /// </remarks>
    private readonly record struct Labour(
        int Workers, int Children, int Posts, int Filled, int Dwellings,
        int Housed, int Slots, int Empty);

    /// <summary>The labour market as it stands right now.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Posts are counted on PREMISED Businesses only, and the difference is not pedantry.</b>
    /// The assignment pass reaches an employer by walking the Buildings inside a box, so a Business
    /// sitting in the pool is unreachable however many posts its trade declares. Counting those
    /// would report a labour supply no Citizen can take a job from — the shape of
    /// <c>plans/0041</c> <b>G44</b>'s stranded shops, which outlived their premises and stayed in
    /// the pool for ever.
    /// </para>
    /// <para>
    /// ⚠ <b><c>Workers</c> is what <see cref="World.IsOfWorkingAge"/> says and not what the age
    /// column says</b>, which is the whole reason it can be summed at all. <c>Citizens.Age</c> is
    /// zero in every world declaring no <c>[[life_stage]]</c>, so a count reading the column
    /// directly would report twenty Rulesets as cities with no workers in them.
    /// </para>
    /// </remarks>
    private static Labour Market(World world)
    {
        int workers = 0;
        int children = 0;
        int posts = 0;
        int filled = 0;

        CitizenTable citizens = world.Citizens;

        for (int slot = 0; slot < citizens.Rows.SlotCount; slot++)
        {
            if (!citizens.Rows.IsLive(slot))
            {
                continue;
            }

            if (world.IsOfWorkingAge(slot))
            {
                workers++;
            }
            else
            {
                children++;
            }
        }

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || !world.Buildings.Rows.TryResolve(world.Businesses.Building[slot], out int _))
            {
                continue;
            }

            posts += world.DeclaredJobs(slot);
            filled += world.Workers.Length(slot);
        }

        // 🔴 THE SECOND FACTOR UNDER `posts per Citizen`, AND IT IS THE ONE THE SINK CANNOT REACH.
        // `abandoned_when_empty_after_days` collects a dwelling that houses NOBODY, and a shrinking
        // city does not produce many: placement takes the first Lot with room out of a draw of three
        // and nothing biases it toward a fuller house, so the Households that remain are SPREAD one
        // per dwelling rather than consolidated. Half the housing capacity in the city can be empty
        // while almost no house is. These two columns are what say so.
        int housed = 0;
        int slots = 0;
        int empty = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            byte kind = world.Buildings.Kind[slot];

            if (!world.Buildings.Rows.IsLive(slot)
                || world.Buildings.IsAbandoned(slot)
                || !world.Rules.Declares(kind))
            {
                continue;
            }

            int families = world.Occupants.Length(slot);

            housed += families;

            // ⚠ THE CAPACITY IS NOT `occupants` AND THE DIFFERENCE IS A WHOLE SLOT. adr/0147 made one
            // ceiling count tenants of EVERY kind and adr/0148 gives a dwelling a trade, so a kind
            // declaring 4 houses THREE families and rents the fourth slot to a shop. Printing the
            // declared number as the denominator would report a third of the housing capacity in the
            // city as empty when it is let.
            slots += Math.Max(
                0, world.Rules.Kind(kind).Occupants - world.BuildingBusinesses.Length(slot));

            if (families == 0)
            {
                empty++;
            }
        }

        return new Labour(
            workers, children, posts, filled, world.Buildings.Rows.LiveCount, housed, slots, empty);
    }

    /// <summary>
    /// 🔴 <c>plans/0046</c>'s loose end: whether <c>[[building]] jobs = 8</c> still buys what it was
    /// derived to buy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The number was derived as <c>1000/360 × 3 = 8.33</c>, floored</b> — the world generator's
    /// Citizens-per-Household ratio through the dwelling kind's <c>occupants = 3</c> — and
    /// <c>plans/0023</c> recorded what the flooring bought: <em>"full employment is out of reach by
    /// construction and the shortage flow is never trivially zero, which was the point"</em>. ⚠
    /// <b>Every term in that derivation counts CITIZENS, and it assumes every Citizen works.</b>
    /// That assumption is what <c>plans/0046</c> stage 4 retired.
    /// </para>
    /// <para>
    /// 🔴 <b>So the panel prints a BAND and not a census, because the ratio is not a constant of the
    /// city.</b> Posts follow the standing Building stock and workers follow the population, and on
    /// a world with demographics those two move on different clocks — the stock outlives the people
    /// who built it. ***A single reading of this ratio is a reading of the Day it was taken on***,
    /// which is why the stage-4 commit's <c>1,200 of 1,411</c> and a Day-400 census disagree by a
    /// factor of two without either being wrong.
    /// </para>
    /// </remarks>
    private static void Work(List<Labour> labour, TextWriter output)
    {
        double lowest = double.MaxValue;
        double highest = 0;
        long posts = 0;
        long workers = 0;
        long people = 0;
        int reachable = 0;
        int peakStock = 0;
        int stockFell = 0;

        for (int day = 1; day < labour.Count; day++)
        {
            Labour today = labour[day];

            if (today.Dwellings < labour[day - 1].Dwellings)
            {
                stockFell++;
            }

            peakStock = Math.Max(peakStock, today.Dwellings);

            if (today.Workers == 0)
            {
                continue;
            }

            double ratio = today.Posts / (double)today.Workers;

            lowest = Math.Min(lowest, ratio);
            highest = Math.Max(highest, ratio);
            posts += today.Posts;
            workers += today.Workers;
            people += today.Workers + today.Children;

            if (today.Posts >= today.Workers)
            {
                reachable++;
            }
        }

        Labour last = labour[^1];
        int population = last.Workers + last.Children;
        int share = population == 0 ? 0 : 100 * last.Children / population;

        output.WriteLine();
        output.WriteLine("Is there work for the people who can work?");
        output.WriteLine();
        output.WriteLine(F($"  working age           {last.Workers,8:N0}   at the end of the run"));
        output.WriteLine(F($"  children              {last.Children,8:N0}   {share}% of the population"));
        output.WriteLine(F($"  dwellings standing    {last.Dwellings,8:N0}   peak {peakStock:N0}"));
        output.WriteLine(F($"  posts standing        {last.Posts,8:N0}"));
        output.WriteLine(F($"  posts filled          {last.Filled,8:N0}"));
        output.WriteLine(F($"  unemployed            {last.Workers - last.Filled,8:N0}"));

        if (workers > 0)
        {
            int sampled = labour.Count - 1;

            output.WriteLine();
            output.WriteLine(F(
                $"  POSTS PER WORKER      {posts / (double)workers,8:N2}   mean over the run"));
            output.WriteLine(F($"                        {lowest,8:N2}   lowest Day"));
            output.WriteLine(F($"                        {highest,8:N2}   highest Day"));
            output.WriteLine(F(
                $"  full employment       {reachable,8:N0}   Days of {sampled} with posts >= workers"));
            output.WriteLine();
            output.WriteLine("  It is the PRODUCT of two ratios and the derivation fixed both:");
            output.WriteLine();
            output.WriteLine(F(
                $"  posts per Citizen     {posts / (double)people,8:N2}   derived 0.96"));
            output.WriteLine(F(
                $"  Citizens per worker   {people / (double)workers,8:N2}   derived 1.00"));
            output.WriteLine(F(
                $"  Days the stock fell   {stockFell,8:N0}   of {sampled}"));
            output.WriteLine();
            double perHome = last.Housed / (double)Math.Max(1, last.Dwellings);
            double free = last.Slots == 0 ? 0 : 100.0 * (last.Slots - last.Housed) / last.Slots;

            double perHomeSlots = last.Slots / (double)Math.Max(1, last.Dwellings);

            output.WriteLine(F(
                $"  Households per home   {perHome,8:N2}   of {perHomeSlots:N2} the trade leaves free"));
            output.WriteLine(F(
                $"  housing slots empty   {free,8:N0}%  {last.Slots - last.Housed:N0} of {last.Slots:N0}"));
            output.WriteLine(F(
                $"  homes housing nobody  {last.Empty,8:N0}   of {last.Dwellings:N0} standing"));
        }

        output.WriteLine();
        output.WriteLine(
            "  `jobs = 8` is the floor of 1000/360 x 3 — the generator's Citizens per Household");
        output.WriteLine(
            "  through the dwelling kind's occupants — and plans/0023 recorded what the flooring");
        output.WriteLine(
            "  bought: 0.96 posts per resident, so full employment is out of reach BY CONSTRUCTION");
        output.WriteLine(
            "  and the shortage is never trivially zero.");
        output.WriteLine();
        output.WriteLine(
            "  🔴 BOTH FACTORS HAVE MOVED AND THE BIGGER ONE IS NOT ABOUT CHILDREN. plans/0046 recorded");
        output.WriteLine(
            "  that `Citizens per worker` left 1.00 when stage 4 stopped counting children as labour.");
        output.WriteLine(
            "  It did not record the other, and the other is larger: `posts per Citizen` is");
        output.WriteLine(
            "  8 x DWELLINGS / Citizens, and this world used to have NO WAY AT ALL to lose a dwelling —");
        output.WriteLine(
            "  adr/0069 builds while the Unplaced Pool is non-empty and nothing here ever condemned, so");
        output.WriteLine(
            "  the stock only ever rose and `Days the stock fell` read zero over 400 Days.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ THAT WAS NEVER adr/0006 VIOLATED, WHICH IS WHY NO LONG RUN CAUGHT IT. The stock is");
        output.WriteLine(
            "  BOUNDED — by peak demand — so the collection converges and every collection check passes.");
        output.WriteLine(
            "  What is unbounded is the RATIO, because its denominator is free to fall and on a world");
        output.WriteLine(
            "  with dissolution it does. A monotone numerator over a falling denominator is invisible");
        output.WriteLine(
            "  to a test that watches the numerator.");
        output.WriteLine();
        output.WriteLine(
            "  ✅ THE SINK NOW EXISTS AND IT IS NOT DECLINE. `[[building]] abandoned_when_empty_after_days`");
        output.WriteLine(
            "  gives up on a dwelling nobody has lived in for the stated number of Days — adr/0069's build");
        output.WriteLine(
            "  predicate mirrored, and 02 §5.5's redevelopment floor, the case where nobody wants the land.");
        output.WriteLine(
            "  It ABANDONS rather than demolishing, so adr/0091 is untouched: the city stops maintaining an");
        output.WriteLine(
            "  empty house and never sends a bulldozer a player would have had to pay for.");
        output.WriteLine();
        output.WriteLine(
            "  🔴 AND IT DOES NOT RESTORE `jobs`, WHICH IS THE FINDING RATHER THAN A FAILED REPAIR. The two");
        output.WriteLine(
            "  columns above say why: a shrinking city DOES NOT CONSOLIDATE. Placement takes the first Lot");
        output.WriteLine(
            "  with room out of a draw of three (adr/0069) and nothing biases it toward a fuller house, so");
        output.WriteLine(
            "  the families left over are spread ONE PER DWELLING instead of filling houses and vacating");
        output.WriteLine(
            "  them. Read the three columns together: the share of housing SLOTS standing empty runs");
        output.WriteLine(
            "  several times the share of HOUSES standing empty, whatever the run — so a sink keyed on");
        output.WriteLine(
            "  an empty house can only ever collect the tail of that distribution, however short its");
        output.WriteLine(
            "  clock. ⚠ Both shares move with the Ruleset's stage windows and the gap does not: quote");
        output.WriteLine(
            "  the ratio and never either figure on its own.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ MEASURED, AND THE CLOCK IS NOT THE BINDING CONSTRAINT. Swept on aged.toml at 2,000");
        output.WriteLine(
            "  Citizens over 400 Days, at 5, 10, 20, 40 and 80 Days: `posts per Citizen` moves");
        output.WriteLine(
            "  1.61 -> 1.98 across a SIXTEENFOLD range, against 2.06 with no sink at all and a derived");
        output.WriteLine(
            "  0.96. ***Shortening the duration does not buy the property back***, so a number tuned");
        output.WriteLine(
            "  until this panel read 0.96 would be measuring the saturation of the wrong knob. ⚠ The");
        output.WriteLine(
            "  same sweep on the file's OLD narrow stage windows gave 1.60 -> 1.99, so the saturation");
        output.WriteLine(
            "  is a property of placement and not of the demography.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ SO THERE IS STILL NO VALUE OF `jobs` THAT RESTORES THE PROPERTY, AND THE REASON IS NOW");
        output.WriteLine(
            "  SHARPER. It was derived as 1000/360 x OCCUPANTS, which assumes every dwelling is FULL — true");
        output.WriteLine(
            "  only of a city under housing pressure. A demographic city is under it half the time. The");
        output.WriteLine(
            "  ratio is not a constant of this city, and a number re-derived against any one Day is a");
        output.WriteLine(
            "  reading of that Day (adr/0073).");
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
