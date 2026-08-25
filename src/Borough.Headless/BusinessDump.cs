namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

/// <summary>
/// The economic actor, printed: how many there are, where they got them, what they hold, who works
/// in them, and what read one.
/// </summary>
/// <remarks>
/// <para>
/// <b>Milestone 27's "something to look at", and the panels are the milestone's own risk taken apart.</b>
/// <c>plans/0041</c> names the risk as ***the economic actor exists in the build*** — the city
/// <em>creates</em> one, <em>funds</em> one, <em>employs</em> through one, and a Rule can <em>read</em>
/// its balance. Four claims, four panels, in that order. A dump that printed one number would be a
/// picture of whichever quarter of the mechanism happened to be working.
/// </para>
/// <para>
/// ⚠ <b>The question milestone 25 got wrong is asked here first, and the answer is different.</b>
/// <c>plans/0040</c> <b>F43</b>: that milestone went to show a tenancy ending and *the thing to look at
/// did not exist in any shipped world*, because every test of the mechanism built its Ruleset by hand.
/// ***So: which shipped world contains a Business the city created?*** <b>All sixteen</b>, since
/// <c>adr/0148</c> made construction instantiate a kind's declared trade — and only
/// <c>rulesets/levied.toml</c> contains all four quarters at once, which is why this dump names it in
/// every refusal.
/// </para>
/// <para>
/// 🔴 <b>Two of the five flows are DERIVED rather than counted, and the dump says so in the panel
/// rather than in this comment.</b> Nothing counts a Business instantiated by <c>World.Fit</c> or razed
/// by <c>World.DestroyBuilding</c> — they are not placement events and no engine owns them — so the
/// panel prints the Zone Rule's <b>created</b> and <b>demolished</b> beside them. ***On every shipped
/// file those two are equal to the instantiation and razing counts, and the equality breaks on the
/// first kind that declares no trade***, which no shipped file does today.
/// </para>
/// <para>
/// ⚠ <b>What this dump must not be read as is an economy.</b> A Business at milestone 27 has no
/// revenue — <c>Scope.Pool</c> throws — so every balance here is founding capital and what a levy takes
/// out of it. **The money panel is a picture of a stock that only shrinks.**
/// </para>
/// </remarks>
internal static class BusinessDump
{
    /// <summary>How many rows of the stock series to print.</summary>
    private const int Rows = 12;

    /// <summary>Runs a session and prints what the city did with its Businesses.</summary>
    /// <param name="options">The parsed command line.</param>
    /// <param name="output">Where the picture goes.</param>
    /// <returns>0, or a non-zero code when the Ruleset cannot demonstrate a Business.</returns>
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
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        int readings = (int)((options.Ticks / cadence) + 2);
        Census census = new(world, readings);

        // 🔴 THE PLACEMENT ACTIVITY IS DRAINED HERE AND HANDED TO THE CENSUS, rather than the Census
        // being handed the Simulation -- and the first version of this dump did the latter and
        // printed ZERO foundings in a world visibly full of founded shops. Census.Observe(simulation)
        // calls Drain on every engine, so a run that observes on a cadence and then drains at the end
        // reads only the tail since the last observation.
        //
        // ⚠ IT MATTERS HERE AND NOWHERE ELSE BECAUSE PlacementCounter HAS NO Founded OR Premised
        // MEMBER. Every other flow this dump prints survives into a census series and can be read
        // back out of one; these two exist only in the activity, so somebody has to keep the running
        // total by hand. Filed in plans/0041 as the gap it is.
        long founded = 0;
        long premised = 0;
        long retired = 0;

        Accumulate(simulation, census, ref founded, ref premised, ref retired);

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            if (simulation.Tick.Raw % cadence == 0)
            {
                Accumulate(simulation, census, ref founded, ref premised, ref retired);
            }
        }

        var window = new Ticks(options.Ticks);

        output.WriteLine("# Borough business dump — the economic actor, and what the city does with it");
        output.WriteLine(
            F($"# {options.Citizens:N0} Citizens, {options.Ticks:N0} Ticks, ")
            + F($"a reading every {cadence:N0} — {census.Count:N0} readings."));
        output.WriteLine();

        Stock(output, census, world, window);
        output.WriteLine();
        Flows(output, census, window, founded, premised, retired);
        output.WriteLine();
        Held(output, world, rules, names);
        output.WriteLine();
        Staff(output, world, census, window);
        output.WriteLine();
        Read(output, rules, census, window);

        return 0;
    }

    /// <summary>Panel one: how many Businesses there are, and where they are.</summary>
    /// <remarks>
    /// ⚠ <b>The slot high-water mark is printed beside the live count because it is the one that
    /// settles <c>adr/0006</c></b>, which <c>plans/0040</c> <b>F45</b> found the hard way: *"live counts
    /// oscillate and prove little on their own … a flat high-water mark under continuous churn is rows
    /// being recycled."* ***A live count can be flat while the allocator creeps.***
    /// </remarks>
    private static void Stock(TextWriter output, Census census, World world, Ticks window)
    {
        output.WriteLine("How many there are, and where");
        output.WriteLine();

        string header = Row("", "at the founding", "now", "low", "high");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        int business = TableIndex(world, "business");
        int unpremised = TableIndex(world, "unpremised");

        Print(output, census, window, "businesses", Metric.Of(business, CensusCounter.Live));
        Print(output, census, window, "  unpremised", Metric.Of(unpremised, CensusCounter.Live));
        Print(output, census, window, "business slots", Metric.Of(business, CensusCounter.Slots));
        Print(output, census, window, "unpremised slots", Metric.Of(unpremised, CensusCounter.Slots));

        output.WriteLine();
        output.WriteLine(
            "  ⚠ THE SLOT ROWS ARE THE ONES THAT SETTLE adr/0006 AND THE LIVE ROWS ARE NOT. A slot");
        output.WriteLine(
            "  count is a high-water mark, so a flat one under churn is rows being recycled; a live");
        output.WriteLine(
            "  count can sit still while the allocator creeps underneath it. plans/0040 F45 is the");
        output.WriteLine(
            "  reading that established this, on the other milestone's collection.");
        output.WriteLine();
        output.WriteLine(
            "  `unpremised` counts Businesses looking for premises, never Households looking for a");
        output.WriteLine(
            "  home -- that is the Unplaced Pool and it is a different table. The two are not");
        output.WriteLine(
            "  summable: adding them adds shops to families.");

        Trend(output, census, world, window);
    }

    /// <summary>The stock over time, because a bound is a trajectory and not a pair of endpoints.</summary>
    /// <remarks>
    /// 🔴 <b>The panel above cannot answer <c>adr/0006</c> and this one is why it exists.</b> A
    /// founding, a now, a low and a high are four readings of a curve, and ***a series that climbed
    /// steadily and one that settled after the first Day print the same four numbers*** whenever the
    /// settled value is the highest. The head and the tail are printed with the middle elided, which is
    /// <c>MoneyDump</c>'s shape: a long run's middle is the part a reader skips, and a table that says
    /// it elided beats one that silently truncates.
    /// </remarks>
    private static void Trend(TextWriter output, Census census, World world, Ticks window)
    {
        int business = TableIndex(world, "business");
        int unpremised = TableIndex(world, "unpremised");

        ReadOnlySpan<CensusSample> live =
            census.Series(Metric.Of(business, CensusCounter.Live), window).Samples.Span;
        ReadOnlySpan<CensusSample> slots =
            census.Series(Metric.Of(business, CensusCounter.Slots), window).Samples.Span;
        ReadOnlySpan<CensusSample> pool =
            census.Series(Metric.Of(unpremised, CensusCounter.Live), window).Samples.Span;

        output.WriteLine();
        output.WriteLine("The stock over time, because a bound is a trajectory");
        output.WriteLine();

        string header = F($"{"tick",-14}{"businesses",14}{"slots",14}{"unpremised",14}");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        if (live.IsEmpty)
        {
            output.WriteLine("  nothing was read.");
            return;
        }

        int shown = live.Length <= Rows ? live.Length : Rows / 2;

        for (int i = 0; i < shown; i++)
        {
            WriteTrendRow(output, live, slots, pool, i);
        }

        if (live.Length > Rows)
        {
            output.WriteLine(F($"  … {live.Length - Rows:N0} readings not shown …"));

            for (int i = live.Length - (Rows / 2); i < live.Length; i++)
            {
                WriteTrendRow(output, live, slots, pool, i);
            }
        }
    }

    private static void WriteTrendRow(
        TextWriter output,
        ReadOnlySpan<CensusSample> live,
        ReadOnlySpan<CensusSample> slots,
        ReadOnlySpan<CensusSample> pool,
        int i)
    {
        ulong at = live[i].Tick.Raw;
        long slot = i < slots.Length ? slots[i].Value : 0;
        long waiting = i < pool.Length ? pool[i].Value : 0;

        output.WriteLine(F($"{at,-14:N0}{live[i].Value,14:N0}{slot,14:N0}{waiting,14:N0}"));
    }

    /// <summary>Panel two: the sources and the sinks, and which of them nothing counts.</summary>
    private static void Flows(
        TextWriter output, Census census, Ticks window, long founded, long premised, long retired)
    {
        output.WriteLine("What moved, over the whole run");
        output.WriteLine();

        long created = Sum(census, Metric.Of(ZoneCounter.Created, Aggregate.Sum), window);
        long demolished = Sum(census, Metric.Of(ZoneCounter.Demolished, Aggregate.Sum), window);

        output.WriteLine(Flow("founded by a Household", founded));
        output.WriteLine(Flow("premised by placement", premised));
        output.WriteLine(Flow("gave up and emigrated", retired));
        output.WriteLine();
        output.WriteLine(Flow("Buildings created", created));
        output.WriteLine(Flow("Buildings demolished", demolished));

        output.WriteLine();
        output.WriteLine(
            "  🔴 THE TWO LARGEST FLOWS ARE NOT IN THIS TABLE AND THE BOTTOM TWO ROWS STAND IN FOR");
        output.WriteLine(
            "  THEM. adr/0148 makes construction INSTANTIATE a kind's declared trade and demolition");
        output.WriteLine(
            "  raze it, and neither event is a placement event, so no engine counts one. On every");
        output.WriteLine(
            "  shipped file the dwelling kind declares a trade, so `Buildings created` IS the");
        output.WriteLine(
            "  instantiation count and `Buildings demolished` IS the razing count.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ THAT EQUALITY IS DERIVED AND NOT MEASURED, and it breaks on the first shipped kind");
        output.WriteLine(
            "  that declares no trade. Read it as an upper bound rather than as a count.");
    }

    /// <summary>Panel three: what the Businesses hold.</summary>
    private static void Held(TextWriter output, World world, Ruleset rules, RulesetNames names)
    {
        output.WriteLine("What they hold");
        output.WriteLine();

        long total = 0;
        int holding = 0;
        int live = 0;
        long most = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            live++;

            long balance = world.BalanceOf(world.Businesses.Rows.At(slot)).Raw;

            total += balance;

            if (balance > 0)
            {
                holding++;
                most = balance > most ? balance : most;
            }
        }

        output.WriteLine(Flow("live Businesses", live));
        output.WriteLine(Flow("holding something", holding));
        output.WriteLine(Flow("holding nothing", live - holding));
        output.WriteLine(Flow("held between them", total));
        output.WriteLine(Flow("the largest balance", most));

        output.WriteLine();
        output.WriteLine(
            "  ⚠ A BUSINESS HOLDING NOTHING IS THE ORDINARY CASE AND NOT A FAILURE. adr/0148's");
        output.WriteLine(
            "  instantiated trade is capitalised by nobody and opens at zero; only adr/0145's");
        output.WriteLine(
            "  FOUNDED shop is given a band out of its founder's Household. The two kinds are");
        output.WriteLine(
            "  indistinguishable in the table and the balance is the only thing that separates");
        output.WriteLine(
            "  them, which is why this panel counts rather than lists.");

        if (!Declares(rules, ResourceFamily.Money))
        {
            output.WriteLine();
            output.WriteLine(
                "  ⚠ THIS RULESET DECLARES NO MONEY, so no Business owns a balance Bin at all and");
            output.WriteLine(
                "  every figure above is zero by construction rather than by poverty.");

            return;
        }

        _ = names;
    }

    /// <summary>Panel four: who works in them.</summary>
    /// <remarks>
    /// ⚠ <b>The interesting row is <em>Businesses with nobody in them</em>, and it is the one a job
    /// count cannot show.</b> <c>adr/0141</c> put <c>jobs</c> on the trade, so a world where every shop
    /// posts eight vacancies and fills none reads identically to one with no shops at all if only the
    /// employed are counted.
    /// </remarks>
    private static void Staff(TextWriter output, World world, Census census, Ticks window)
    {
        output.WriteLine("Who works in them");
        output.WriteLine();

        int staffed = 0;
        int empty = 0;
        int workers = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            int here = world.Workers.Length(slot);

            workers += here;

            if (here > 0)
            {
                staffed++;
            }
            else
            {
                empty++;
            }
        }

        output.WriteLine(Flow("Citizens employed by a Business", workers));
        output.WriteLine(Flow("Businesses with at least one", staffed));
        output.WriteLine(Flow("Businesses with nobody", empty));
        output.WriteLine(Flow(
            "job assignments over the run",
            Sum(census, Metric.Of(JobCounter.Employed, Aggregate.Sum), window)));

        output.WriteLine();
        output.WriteLine(
            "  ⚠ `Businesses with nobody` IS THE ROW TO READ. adr/0141 moved `jobs` onto the trade,");
        output.WriteLine(
            "  so a city whose shops all post vacancies and fill none prints the same employed count");
        output.WriteLine(
            "  as a city with no shops in it. An unpremised Business employs nobody BY DESIGN -- it");
        output.WriteLine(
            "  has no premises for a commute to end at -- so this row includes the pool.");
    }

    /// <summary>Panel five: what read a Business's balance.</summary>
    private static void Read(TextWriter output, Ruleset rules, Census census, Ticks window)
    {
        output.WriteLine("What read them");
        output.WriteLine();

        int sweeping = 0;

        foreach (PolicyDefinition policy in rules.Policies)
        {
            if (policy.Subject == PolicySubject.Business)
            {
                sweeping++;
            }
        }

        output.WriteLine(Flow("Policies sweeping Businesses", sweeping));

        if (sweeping == 0)
        {
            output.WriteLine();
            output.WriteLine(
                "  This Ruleset declares no Policy over `business`, so nothing in it ever reads a");
            output.WriteLine(
                "  Business. adr/0149 made `sweeps = \"business\"` legal and rulesets/levied.toml is");
            output.WriteLine(
                "  the shipped file that uses it -- the fourth quarter of this milestone's risk is");
            output.WriteLine(
                "  the one this world cannot show.");

            return;
        }

        output.WriteLine(Flow(
            "members swept", Sum(census, Metric.Of(PolicyCounter.Considered, Aggregate.Sum), window)));
        output.WriteLine(Flow(
            "transfers applied", Sum(census, Metric.Of(PolicyCounter.Applied, Aggregate.Sum), window)));

        output.WriteLine();
        output.WriteLine(
            "  🔴 THOSE TWO COUNTS ARE EVERY POLICY'S TOGETHER AND CANNOT BE ATTRIBUTED TO ONE.");
        output.WriteLine(
            "  PolicyCounter is one set for the whole engine, so a file with Household Policies in");
        output.WriteLine(
            "  it -- which every file declaring a Business levy has, since the levy needs money in");
        output.WriteLine(
            "  the city -- prints their members here too. plans/0041 G41.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ A LEVY DERIVED FROM `balance` OWES NOTHING ON ZERO, so `applied` is expected to sit");
        output.WriteLine(
            "  well below `members swept` and a gap is not a defect. See the panel above for how");
        output.WriteLine(
            "  many of this world's Businesses hold nothing at all.");
    }

    /// <summary>Observes one reading, keeping the placement flows the Census cannot carry.</summary>
    private static void Accumulate(
        Simulation simulation, Census census, ref long founded, ref long premised, ref long retired)
    {
        PlacementActivity placement = simulation.Placement.Drain();

        founded += placement.Founded.Sum;
        premised += placement.Premised.Sum;
        retired += placement.Retired.Sum;

        census.Observe(
            simulation.World,
            simulation.Tick,
            simulation.Rules.Drain(),
            simulation.Zoning.Drain(),
            placement,
            simulation.Trips.Drain(),
            simulation.Employment.Drain(),
            simulation.Policies.Drain());
    }

    // ---- the plumbing --------------------------------------------------------------------------

    /// <summary>Refuses a Ruleset in which no Business can exist.</summary>
    /// <remarks>
    /// ⚠ <b>The test is whether a Business can be CREATED, not whether a trade is declared</b>, and the
    /// two are different files: <c>rulesets/tenanted.toml</c> names two trades and instantiates neither,
    /// so it would print a table of zeroes and read as a broken dump rather than as a file that
    /// declares without building.
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        for (byte kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind(kind).Business != 0)
            {
                return null;
            }
        }

        if (rules.Founding.Runs)
        {
            return null;
        }

        output.WriteLine(
            "No kind in this Ruleset declares a trade and it states no [founding] channel, so "
            + "nothing in a world on it ever creates a Business and every panel below would be a "
            + "row of zeroes. A table of zeroes reads as a broken dump rather than as a file that "
            + "authors no economic actor.");
        output.WriteLine();
        output.WriteLine("  --business --ruleset rulesets/levied.toml --citizens 2000 --ticks 12288");

        return 3;
    }

    private static bool Declares(Ruleset rules, ResourceFamily family)
    {
        for (int id = 1; id <= rules.ResourceCount; id++)
        {
            if (rules.Family(new ResourceId((byte)id)) == family)
            {
                return true;
            }
        }

        return false;
    }

    private static int TableIndex(World world, string name)
    {
        ReadOnlySpan<Rows> tables = world.Tables;

        for (int i = 0; i < tables.Length; i++)
        {
            if (tables[i].Name == name)
            {
                return i;
            }
        }

        throw new InvalidOperationException(
            $"this world has no table called '{name}'. The dump names tables by string because the "
            + "Census does, and a rename is a compile-clean break -- which is what this throw exists "
            + "to turn back into a loud one.");
    }

    private static long Sum(Census census, Metric metric, Ticks window)
    {
        ReadOnlySpan<CensusSample> samples = census.Series(metric, window).Samples.Span;

        return samples.IsEmpty ? 0 : samples[^1].Value;
    }

    private static void Print(
        TextWriter output, Census census, Ticks window, string label, Metric metric)
    {
        ReadOnlySpan<CensusSample> samples = census.Series(metric, window).Samples.Span;

        if (samples.IsEmpty)
        {
            output.WriteLine(Row(label, "—", "—", "—", "—"));
            return;
        }

        long low = samples[0].Value;
        long high = samples[0].Value;

        foreach (CensusSample sample in samples)
        {
            low = sample.Value < low ? sample.Value : low;
            high = sample.Value > high ? sample.Value : high;
        }

        output.WriteLine(Row(
            label, Count(samples[0].Value), Count(samples[^1].Value), Count(low), Count(high)));
    }

    private static string Row(string label, string a, string b, string c, string d) =>
        $"{label,-28}{a,17}{b,17}{c,15}{d,15}";

    private static string Flow(string label, long value) => F($"  {label,-34}{value,14:N0}");

    private static string Count(long value) => F($"{value:N0}");

    private static string F(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
