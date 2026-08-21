namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

/// <summary>
/// The door, printed: who came in, who is waiting, who gave up, and what the money did.
/// </summary>
/// <remarks>
/// <para>
/// <b>Milestone 11's "something to look at", and it is four quantities rather than one because the
/// milestone's mechanism is a pipe with two ends.</b> Arrivals are a flow in, the Unplaced Pool is
/// the stock between, Departures are a flow out, and the money supply is what all three move. ⚠ ***A
/// picture of any one of them is a picture of a symptom*** — `CONTEXT` → Departure is explicit that a
/// large Pool can be a healthy city and a small one a city in crisis, and that only the flow tells
/// them apart.
/// </para>
/// <para>
/// 🔴 <b>It is the first dump that issues Commands, and that is forced rather than chosen.</b>
/// Nothing in the simulation decides to arrive: <c>adr/0128</c> puts the comparison at milestone 16,
/// so until then every arrival comes through <see cref="CommandKind.Arrive"/> and somebody outside
/// has to ask. Every other dump steps an empty Tick and watches the city act on its own; this one
/// cannot, because there is nothing here that acts on its own yet.
/// </para>
/// <para>
/// 🔴 ⚠ <b>So the rate had to come from somewhere, and it is the Ruleset's rather than this file's.</b>
/// The dump asks each gate for <em>more than it can take</em>, every Day, and what is admitted is
/// <c>[[building]] arrivals_per_day</c> clipped by the gate itself. ***A demonstration that chose its
/// own rate would be showing the demonstration.*** The alternative — a cadence written here — would
/// have put a number with no ratifier into the shell (<c>adr/0052</c>) and made the picture a
/// picture of that number. The <b>asked</b> column is printed beside the <b>admitted</b> one so the
/// clipping is visible rather than implied.
/// </para>
/// <para>
/// ⚠ <b>What this dump must not be read as is an immigration rate.</b> There is no such thing in this
/// build and <c>adr/0023</c>'s first line refuses one. <c>arrivals_per_day</c> is a <em>ceiling on a
/// Day's admissions through one door</em> — what a gate will take, not what a Hinterland will send.
/// </para>
/// <para>
/// <b>The Households-per-command and Citizens-per-Household are the instrument's and are stated as
/// such.</b> Nothing in the build models Life Stage → composition (milestone 11 task 6 asked and the
/// answer was that the Command carries it), so a figure here would be a model nobody wrote. They are
/// printed in the header so no reader has to guess which numbers are the city's.
/// </para>
/// </remarks>
internal static class ArrivalDump
{
    /// <summary>Citizens per arriving Household. <b>The instrument's number, not the city's.</b></summary>
    /// <remarks>
    /// <b>Two, because a Household of one cannot show that a move-in is one Trip per Citizen and a
    /// Household of four takes four times as long to say so.</b> It stands in for a composition model
    /// that does not exist — milestone 11 task 6 established that the Command carries the count
    /// precisely because nothing in the build derives one — and ***an instrument states what it is
    /// standing in for, and does not model it.***
    /// </remarks>
    private const byte CitizensPerHousehold = 2;

    /// <summary>How many rows of the Pool series to print.</summary>
    private const int Rows = 16;

    /// <summary>How many waiting Households to name in the Evidence panel.</summary>
    private const int Named = 8;

    /// <summary>
    /// Runs a session on the given Ruleset, driving its gates, and prints what came of it.
    /// </summary>
    /// <param name="options">The parsed command line.</param>
    /// <param name="output">Where the picture goes.</param>
    /// <returns>0, or a non-zero code when the Ruleset cannot demonstrate an arrival.</returns>
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

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        Gate[] gates = Gates(world);

        if (gates.Length == 0)
        {
            output.WriteLine(
                "the Ruleset declares an Outside Connection kind and this world raised none, so "
                + "there is no door to drive. That is a generator question rather than a Ruleset "
                + "one -- SyntheticCity.RaiseGates places them, and it needs a lattice that reaches "
                + "the map's boundary.");
            return 3;
        }

        var series = new List<Reading>();
        long issuedAtStart = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        Run(simulation, world, gates, options.Ticks, series);

        Header(output, options, rules, names, gates);
        Doors(output, world, gates, names);
        Pool(output, series);
        Waiting(output, world);
        Money(output, world, issuedAtStart, series);

        return 0;
    }

    // ---- the run -------------------------------------------------------------------------------

    /// <summary>Steps the world, knocking on every door once a Day and sampling once a Day.</summary>
    /// <remarks>
    /// <b>The knock is on the Day boundary because the meter is</b>: <c>World.TryArrive</c> resets a
    /// gate's quota when the Day number changes, so asking at any other point in the Day would show a
    /// ceiling half spent by the previous knock. ***A demonstration of a per-Day ceiling has to be
    /// denominated in the same Day the ceiling is.***
    /// </remarks>
    private static void Run(
        Simulation simulation,
        World world,
        Gate[] gates,
        ulong ticks,
        List<Reading> series)
    {
        Span<Command> knock = stackalloc Command[1];

        for (ulong tick = 0; tick < ticks; tick++)
        {
            if (tick % Ticks.PerDay == 0)
            {
                foreach (Gate gate in gates)
                {
                    knock[0] = gate.Knock();
                    simulation.Step(new TickInput(knock, simulation.RulesetInForce));
                }

                // Every knock above consumed a Tick, so the Day's remaining Ticks are stepped empty
                // below. Sampling here rather than after keeps a reading on the Day boundary.
                //
                // Drained rather than read through a Census, and the two are mutually exclusive
                // rather than merely different: Census.Observe drains the same engine, so a dump
                // doing both would read each flow at whichever of them ran second and get zero. The
                // Census is still where PlacementCounter.Departed belongs -- `--census` prints it --
                // and this mode wants per-DAY deltas rather than a windowed series.
                series.Add(Reading.Of(world, tick, simulation.Placement.Drain()));
            }

            simulation.Step(default);
        }
    }

    // ---- the panels ----------------------------------------------------------------------------

    private static void Header(
        TextWriter output, Options options, Ruleset rules, RulesetNames names, Gate[] gates)
    {
        output.WriteLine("ARRIVAL THROUGH THE GATE");
        output.WriteLine();
        output.WriteLine(F($"  ruleset        {options.RulesetPath}"));
        output.WriteLine(F($"  citizens       {options.Citizens} at world creation"));
        output.WriteLine(F($"  ticks          {options.Ticks} ({options.Ticks / Ticks.PerDay} Days)"));
        output.WriteLine(F($"  gates          {gates.Length}"));
        output.WriteLine(F(
            $"  gives up after {rules.Placement.GivesUpAfterDays} Days ({rules.Placement.OccasionsBeforeGivingUp} occasions at this cadence)"));
        output.WriteLine();
        output.WriteLine("  The runner asks each gate for more than it can take, once a Day, so what");
        output.WriteLine("  arrives is the FILE's arrivals_per_day and not a rate chosen here. Nothing");
        output.WriteLine("  in the simulation decides to arrive (adr/0128) -- there is no immigration");
        output.WriteLine("  rate in this build and arrivals_per_day is not one: it is a ceiling on a");
        output.WriteLine("  Day's admissions through one door.");
        output.WriteLine();
        output.WriteLine(F(
            $"  Each command carries {CitizensPerHousehold} Citizens per Household. THAT NUMBER IS THE INSTRUMENT'S:"));
        output.WriteLine("  nothing in the build models Life Stage to composition, so a figure derived");
        output.WriteLine("  here would be a model nobody wrote.");
        output.WriteLine();

        _ = names;
    }

    /// <summary>Per gate: what was asked, what the ceiling allowed, and which bound.</summary>
    private static void Doors(TextWriter output, World world, Gate[] gates, RulesetNames names)
    {
        output.WriteLine("THE DOORS");
        output.WriteLine();
        output.WriteLine("  gate              edge     ceiling/Day   asked/Day   admitted   refused");
        output.WriteLine("  ----------------  -------  -----------   ---------   --------   -------");

        foreach (Gate gate in gates)
        {
            int admitted = gate.Admitted(world);

            output.WriteLine(F(
                $"  {Name(names, world.Buildings.Kind[gate.Building]),-16}  {Edge(gate.Edge),-7}  {gate.Ceiling,11}   {gate.Ask,9}   {admitted,8}   {gate.Ask - admitted,7}"));
        }

        output.WriteLine();
        output.WriteLine("  `admitted` is the LAST Day's meter rather than the run's total -- the gate");
        output.WriteLine("  stores a Day's count and the Day it belongs to, not a lifetime tally, which");
        output.WriteLine("  is what makes it a ceiling rather than a quota. `refused` is what the door");
        output.WriteLine("  turned away, and a non-zero column here is the ceiling working.");
        output.WriteLine();
    }

    /// <summary>The stock and the two flows, over the run.</summary>
    private static void Pool(TextWriter output, List<Reading> series)
    {
        output.WriteLine("THE POOL, THE FLOWS");
        output.WriteLine();
        output.WriteLine("     day       pool   considered   placed   departed      supply");
        output.WriteLine("  ------   --------   ----------   ------   --------   ---------");

        int from = series.Count <= Rows ? 0 : series.Count - Rows;

        if (from > 0)
        {
            output.WriteLine(F($"  ... {from} earlier Days not shown"));
        }

        for (int i = from; i < series.Count; i++)
        {
            Reading reading = series[i];

            output.WriteLine(F(
                $"  {reading.Day,6}   {reading.Pool,8}   {reading.Considered,10}   {reading.Placed,6}   {reading.Departed,8}   {reading.Issued,9}"));
        }

        output.WriteLine();
        output.WriteLine("  A Pool that GROWS while `placed` is small is a city out of dwellings, and a");
        output.WriteLine("  Pool that grows while `considered` is small is a mechanism that has stopped.");
        output.WriteLine("  The two read identically on a `placed` column alone, which is why there are");
        output.WriteLine("  three flows here and not one.");
        output.WriteLine();
        output.WriteLine("  `departed` is the give-up channel and it is a FLOW, not a share of the Pool:");
        output.WriteLine("  Pool size is a stock of latent demand and departure rate measures how badly");
        output.WriteLine("  the city is failing to convert it. Only the flow tells a large healthy Pool");
        output.WriteLine("  from a small desperate one.");
        output.WriteLine();
    }

    /// <summary>What the Households still waiting have seen, and for how long.</summary>
    private static void Waiting(TextWriter output, World world)
    {
        output.WriteLine("WHO IS STILL WAITING");
        output.WriteLine();

        int pool = world.UnplacedPool.Count;

        if (pool == 0)
        {
            output.WriteLine("  Nobody. The Pool is empty, which in a world with a door in it means");
            output.WriteLine("  construction kept up with the gates for the whole run.");
            output.WriteLine();
            return;
        }

        long now = (long)world.Tick.Raw;
        long waited = 0;
        long considered = 0;
        int longest = 0;

        for (int position = 0; position < pool; position++)
        {
            long spell = now - world.UnplacedPool.Since[position];

            waited += spell;
            considered += world.UnplacedPool.Considered[position];

            if (spell > now - world.UnplacedPool.Since[longest])
            {
                longest = position;
            }
        }

        output.WriteLine(F($"  waiting            {pool} Households"));
        output.WriteLine(F($"  mean wait          {Days(waited / pool)}"));
        output.WriteLine(F($"  longest wait       {Days(now - world.UnplacedPool.Since[longest])}"));
        output.WriteLine(F($"  dwellings seen     {considered} between them"));
        output.WriteLine();
        output.WriteLine("  00-vision.md's Evidence line is TWO numbers -- \"Considered 20 dwellings over");
        output.WriteLine("  4 months\" -- and they answer different questions. The duration is what bounds");
        output.WriteLine("  and the count is what describes: a Household that saw plenty and took none is");
        output.WriteLine("  a different diagnosis from one nobody offered anything to, and a count of ZERO");
        output.WriteLine("  in a city with no vacancies is the honest reading rather than a broken one.");
        output.WriteLine();
        long bound = world.Rules.Placement.GivesUpAfterTicks;
        long overBound = 0;

        for (int position = 0; position < pool; position++)
        {
            if (now - world.UnplacedPool.Since[position] > bound)
            {
                overBound++;
            }
        }

        if (overBound > 0)
        {
            output.WriteLine(F(
                $"  ⚠ {overBound} of them have waited LONGER THAN THE BOUND, and that is the sampling"));
            output.WriteLine("  rather than a broken bound. PlacementEngine draws its sample WITH");
            output.WriteLine("  REPLACEMENT, so a revisit period is the rate at which a member is looked");
            output.WriteLine("  at and not a guarantee that every member has been -- about 1/e of the Pool");
            output.WriteLine("  goes unlooked-at in any given period, and the bound is tested when somebody");
            output.WriteLine("  is next looked at. So the wait is bounded in EXPECTATION and not absolutely.");
            output.WriteLine("  plans/0035 F24, and the Pool's SIZE is bounded regardless because the sample");
            output.WriteLine("  scales with the Pool.");
            output.WriteLine();
        }

        output.WriteLine("  the longest-waiting few:");
        output.WriteLine();
        output.WriteLine("     waited   considered   came through");
        output.WriteLine("     ------   ----------   ------------");

        // Sorted, because the heading says so. Reading the first N positions instead would print
        // whoever the Pool's churn happened to leave at the front -- a label claiming an ordering
        // the code does not do, which is the shape plans/0012 Cause 1 takes inside one file.
        int[] order = [.. Enumerable.Range(0, pool)];

        Array.Sort(order, (a, b) => world.UnplacedPool.Since[a].CompareTo(world.UnplacedPool.Since[b]));

        for (int i = 0; i < pool && i < Named; i++)
        {
            int position = order[i];

            string came = world.UnplacedPool.GateAt(position).Equals(default(Handle<Building>))
                ? "no gate"
                : "a gate";

            output.WriteLine(F(
                $"     {Days(now - world.UnplacedPool.Since[position]),6}   {world.UnplacedPool.Considered[position],10}   {came}"));
        }

        output.WriteLine();
        output.WriteLine("  `no gate` is the ordinary case and not a hole: three of the Pool's four entry");
        output.WriteLine("  routes come from inside the city -- a Household the city generated, one");
        output.WriteLine("  evicted by a demolition, one that decided to move -- and the give-up bound");
        output.WriteLine("  applies to all four. A sink that only drained what came in through the door");
        output.WriteLine("  would leave everything that was already inside.");
        output.WriteLine();
    }

    /// <summary>What crossed the gate in each direction, and whether it still adds up.</summary>
    private static void Money(TextWriter output, World world, long atStart, List<Reading> series)
    {
        output.WriteLine("THE MONEY");
        output.WriteLine();

        long issued = world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
        MoneyLedger ledger = MoneyLedger.Of(world);

        output.WriteLine(F($"  supply at the founding   {atStart,12}"));
        output.WriteLine(F($"  supply now               {issued,12}"));
        output.WriteLine(F($"  net across the gate      {issued - atStart,12}"));
        output.WriteLine();
        output.WriteLine(F($"  held by Households       {ledger.Households,12}"));
        output.WriteLine(F($"  held by Businesses       {ledger.Businesses,12}"));
        output.WriteLine(F($"  held by the treasury     {ledger.Treasury,12}"));
        output.WriteLine(F($"  held elsewhere           {ledger.Elsewhere,12}"));
        output.WriteLine(F($"  walked                   {ledger.Total,12}"));
        output.WriteLine();

        output.WriteLine(ledger.Total == issued
            ? "  CONSERVED: the walk and the anchor agree exactly."
            : F($"  🔴 LEAK: the walk is {ledger.Total - issued} away from the anchor."));

        output.WriteLine();
        output.WriteLine("  A world's money supply stopped being a constant at milestone 11 task 5, and");
        output.WriteLine("  this is the line that shows it. The equality is still EXACT and has no flow");
        output.WriteLine("  term, because MoneySupply.Issued is declared net of anything that has left");
        output.WriteLine("  and both sides move in one call: World.Endow on the way in, World.Depart on");
        output.WriteLine("  the way out. adr/0024 makes the Outside Connection money's only source and");
        output.WriteLine("  sink, so a Household walking out with its savings is the only disposal the");
        output.WriteLine("  corpus permits -- there is no escheat and no estate.");
        output.WriteLine();

        if (series.Count > 1 && series[^1].Issued == series[0].Issued)
        {
            output.WriteLine("  ⚠ The supply did not move over this run. That is not a leak, it is a");
            output.WriteLine("  Ruleset that declares no money Resource -- or a run in which no gate");
            output.WriteLine("  admitted anybody. Check THE DOORS above before reading anything into it.");
            output.WriteLine();
        }
    }

    // ---- refusals and helpers ------------------------------------------------------------------

    /// <summary>
    /// Refuses a Ruleset that cannot show an arrival, on <c>--land-value</c>'s polarity.
    /// </summary>
    /// <remarks>
    /// <b>A picture of a door needs a door.</b> Nine of the eleven shipped files declare no
    /// Outside Connection kind, so the panels would every one of them be blank — which is the failure
    /// <c>plans/0034</c> F17 records: milestone 9 shipped a producer that was correct and
    /// unobservable in every world that existed, for want of Ruleset <em>content</em> rather than
    /// code.
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        bool gated = false;

        // Kind ids run 1..KindCount, which the Ruleset states and this loop must not restate as 0.
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind((byte)kind).ArrivalsPerDay > 0)
            {
                gated = true;
                break;
            }
        }

        if (!gated)
        {
            output.WriteLine(
                "this Ruleset declares no kind with arrivals_per_day, so it has no Outside Connection "
                + "and nothing can arrive through it. Every panel would be blank. "
                + "rulesets/crowded.toml is the file this mode was written for, and "
                + "rulesets/bordered.toml is the same world at the designer's own numbers.");
            return 2;
        }

        if (!rules.Placement.Runs)
        {
            output.WriteLine(
                "this Ruleset states no [placement] table, so nobody is ever housed and nobody ever "
                + "gives up -- the Pool would grow monotonically and the departure column would be "
                + "zero for the whole run. That is a real Ruleset and it is not one this picture can "
                + "say anything about.");
            return 2;
        }

        return null;
    }

    /// <summary>Every standing Outside Connection, with the Tile a command must name.</summary>
    private static Gate[] Gates(World world)
    {
        var found = new List<Gate>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            if (!world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot))
            {
                continue;
            }

            int ceiling = world.Rules.Kind(world.Buildings.Kind[slot]).ArrivalsPerDay;

            found.Add(new Gate(
                slot, lot, world.Lots.East[lot], world.Lots.North[lot], world.EdgeOf(lot), ceiling));
        }

        return [.. found];
    }

    private static string Name(RulesetNames names, byte kind) =>
        names.Kind(kind) ?? F($"kind {kind}");

    private static string Edge(MapEdge edge) => edge switch
    {
        MapEdge.West => "west",
        MapEdge.East => "east",
        MapEdge.South => "south",
        MapEdge.North => "north",
        _ => "none",
    };

    /// <summary>A duration in Ticks, said in Days, because nobody thinks in Ticks.</summary>
    private static string Days(long ticks) => F($"{ticks / Ticks.PerDay}d");

    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>One gate, and what it takes to knock on it.</summary>
    private readonly record struct Gate(
        int Building, int Lot, Tiles East, Tiles North, MapEdge Edge, int Ceiling)
    {
        /// <summary>
        /// How many Households to ask for: more than the door can take, and never more than the word.
        /// </summary>
        /// <remarks>
        /// <b>Four over the ceiling rather than the full 255</b>, which is enough for the refused
        /// column to be non-zero and cheap enough that the overage is not most of the work. ⚠ A
        /// ceiling at or above <see cref="byte.MaxValue"/> cannot be saturated by one command at all,
        /// because <see cref="ArrivePayload.Households"/> is eight bits — the picture then shows a
        /// door wider than the instrument, and the refused column reads zero honestly.
        /// </remarks>
        public int Ask => Ceiling + 4 > byte.MaxValue ? byte.MaxValue : Ceiling + 4;

        /// <summary>The command that asks this gate for more than a Day's worth.</summary>
        public Command Knock() => new(
            CommandKind.Arrive,
            East,
            North,
            new ArrivePayload((byte)Ask, 0, CitizensPerHousehold).Encode());

        /// <summary>What this gate let through on the Day its meter currently names.</summary>
        public int Admitted(World world) => world.Buildings.ArrivalsToday[Building];
    }

    /// <summary>One Day's reading of the stock, the flows and the supply.</summary>
    private readonly record struct Reading(
        ulong Day, int Pool, long Considered, long Placed, long Departed, long Issued)
    {
        public static Reading Of(World world, ulong tick, PlacementActivity activity) => new(
            tick / Ticks.PerDay,
            world.UnplacedPool.Count,
            activity.Considered.Sum,
            activity.Placed.Sum,
            activity.Departed.Sum,
            world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw);
    }
}
