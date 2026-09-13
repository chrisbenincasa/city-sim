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
/// <b>Four quantities rather than one, because the mechanism is a pipe with two ends.</b> Arrivals
/// are a flow in, the Unplaced Pool the stock between, Departures a flow out, and the money supply
/// is what all three move. ⚠ ***A picture of any one is a picture of a symptom*** — `CONTEXT` →
/// Departure: a large Pool can be a healthy city and a small one a city in crisis.
/// </para>
/// <para>
/// 🔴 <b>The Ruleset decides which of two pictures this is, and no flag does.</b> A file stating
/// <c>[immigration]</c> has a counted Outside that decides for itself, so the dump issues no
/// Commands at all and prints the circuit as well. A file without one gets <b>explicit
/// presentations</b>, where nobody in that world decides to come and the dump has to ask.
/// </para>
/// <para>
/// 🔴 ⚠ <b>The rate is never this file's.</b> Under explicit presentations the dump asks each gate
/// for more than it can take, so what is admitted is <c>[[building]] arrivals_per_day</c> clipped by
/// the gate and <b>asked</b> is printed beside <b>admitted</b> to show the clipping. Under a counted
/// Outside the rate is the Hinterland's own and the ceiling only clips it.
/// </para>
/// <para>
/// ⚠ <b><c>arrivals_per_day</c> is not an immigration rate</b>, which <c>adr/0023</c>'s first line
/// refuses. It is a ceiling on one door's admissions in a Day, whichever picture this is. The
/// Households-per-command and Citizens-per-Household are the instrument's and are printed in the
/// header, because a file without a counted Outside states no composition to use instead.
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

    /// <summary>How many of one edge's compositions to print.</summary>
    private const int GroupsShown = 32;

    /// <summary>How many of one edge's doors to print.</summary>
    private const int DoorsShown = 32;

    private static readonly MapEdge[] Edges =
        [MapEdge.West, MapEdge.East, MapEdge.South, MapEdge.North];

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

        // The Ruleset decides which of the two pictures this is, and no flag does. A file stating
        // [immigration] has its own reason for anybody to cross, so a runner knocking on its doors
        // would be adding a caller to a mechanism whose whole point is not having one.
        bool stock = rules.Immigration.Stated;

        Run(simulation, world, gates, options.Ticks, series, asking: !stock);

        Header(output, options, rules, names, gates, stock);

        if (stock)
        {
            Outside(output, world);
            Compositions(output, world, names);
            Connections(output, world, names);
            Circuit(output, world);
            Account(output, world);
        }
        else
        {
            Doors(output, world, gates, names);
        }

        Pool(output, series);
        Waiting(output, world);
        Money(output, world, issuedAtStart, series);

        return 0;
    }

    // ---- the run -------------------------------------------------------------------------------

    /// <summary>Steps the world, sampling once a Day and knocking once a Day where it asks at all.</summary>
    /// <remarks>
    /// <para>
    /// <b>The knock is on the Day boundary because the meter is</b>: <c>World.TryArrive</c> resets a
    /// gate's quota when the Day number changes, so asking at any other point in the Day would show a
    /// ceiling half spent by the previous knock. ***A demonstration of a per-Day ceiling has to be
    /// denominated in the same Day the ceiling is.***
    /// </para>
    /// <para>
    /// ⚠ <b><paramref name="asking"/> is false over a Ruleset stating <c>[immigration]</c></b>, where
    /// every Tick is stepped with an empty input. A single <c>Arrive</c> in that world would put a
    /// caller back inside the one mechanism that exists to have none.
    /// </para>
    /// </remarks>
    private static void Run(
        Simulation simulation,
        World world,
        Gate[] gates,
        ulong ticks,
        List<Reading> series,
        bool asking)
    {
        Span<Command> knock = stackalloc Command[1];

        for (ulong tick = 0; tick < ticks; tick++)
        {
            if (tick % Ticks.PerDay == 0)
            {
                foreach (Gate gate in gates)
                {
                    if (!asking)
                    {
                        break;
                    }

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
        TextWriter output,
        Options options,
        Ruleset rules,
        RulesetNames names,
        Gate[] gates,
        bool stock)
    {
        output.WriteLine("ARRIVAL THROUGH THE GATE");
        output.WriteLine();
        output.WriteLine(F($"  ruleset        {options.RulesetPath}"));
        output.WriteLine(F($"  citizens       {options.Citizens} at world creation"));
        output.WriteLine(F($"  ticks          {options.Ticks} ({options.Ticks / Ticks.PerDay} Days)"));
        output.WriteLine(F($"  gates          {gates.Length}"));
        output.WriteLine(F($"  mode           {(stock ? "a counted Outside" : "explicit presentations")}"));
        output.WriteLine(F(
            $"  gives up after {rules.Placement.GivesUpAfterDays} Days ({rules.Placement.OccasionsBeforeGivingUp} occasions at this cadence)"));
        output.WriteLine();

        if (stock)
        {
            output.WriteLine(F(
                $"  This file states [immigration], so THE RUNNER ISSUES NO COMMANDS AT ALL and every"));
            output.WriteLine("  Tick is stepped empty. Whoever arrives was generated by the Outside, weighed");
            output.WriteLine("  the city against where they already live, and crossed -- or waited, or");
            output.WriteLine("  stayed. arrivals_per_day is still only a ceiling on one door's Day, and it");
            output.WriteLine("  is not the rate: THE CIRCUIT below is where the rate comes from.");
            output.WriteLine();
            output.WriteLine(F(
                $"  reconsider every {rules.Immigration.ReconsiderDays} Days, recovery {rules.Immigration.RecoveryDays} Days"));
            output.WriteLine(F(
                $"  wait outside a full door for {rules.Immigration.QueueWaitDays} Days, reviewed every {rules.Immigration.QueueReconsiderDays} Days of it"));
            output.WriteLine();
            _ = names;

            return;
        }

        output.WriteLine("  This file states no [immigration], so arrivals here are EXPLICIT");
        output.WriteLine("  PRESENTATIONS: the runner asks each gate for more than it can take, once a");
        output.WriteLine("  Day, and what arrives is the FILE's arrivals_per_day rather than a rate");
        output.WriteLine("  chosen here. Nobody in this world decides to come -- the family at the door");
        output.WriteLine("  was invented by the caller, and arrivals_per_day is a ceiling on a Day's");
        output.WriteLine("  admissions through one door rather than an immigration rate.");
        output.WriteLine();
        output.WriteLine(F(
            $"  Each command carries {CitizensPerHousehold} Citizens per Household. THAT NUMBER IS THE INSTRUMENT'S:"));
        output.WriteLine("  a file with a counted Outside states its own compositions and needs none.");
        output.WriteLine();

        _ = names;
    }

    /// <summary>Who stands behind each edge, and who among them is waiting at a full door.</summary>
    private static void Outside(TextWriter output, World world)
    {
        output.WriteLine("THE OUTSIDE");
        output.WriteLine();
        output.WriteLine(F(
            $"  {"edge",-7}   {"doors",5}   {"stock",8}   {"reserved",8}   {"resting",8}   {"people",9}   {"waiting",8}   {"longest",8}"));
        output.WriteLine(F(
            $"  {Dash(7)}   {Dash(5)}   {Dash(8)}   {Dash(8)}   {Dash(8)}   {Dash(9)}   {Dash(8)}   {Dash(8)}"));

        foreach (MapEdge edge in Edges)
        {
            HinterlandReading reading = HinterlandReading.Of(world, edge);

            output.WriteLine(F(
                $"  {Edge(edge),-7}   {reading.Gates,5}   {reading.StockHouseholds,8}   {reading.ReservedHouseholds,8}   {reading.RestingHouseholds,8}   {reading.StockPeople,9}   {reading.QueueHouseholds,8}   {Days((long)reading.OldestWait),8}"));
        }

        output.WriteLine();
        output.WriteLine("  `waiting` is outside a FULL DOOR and it is not the Unplaced Pool. These");
        output.WriteLine("  Households are still part of the Outside's stock, still held as `reserved`");
        output.WriteLine("  against it, and no Citizen row exists for any of them. Somebody in the Pool");
        output.WriteLine("  has already been let in and is looking for a home inside the city.");
        output.WriteLine();
        output.WriteLine("  `stock` less `reserved` is who a door could still be offered today.");
        output.WriteLine("  `resting` is the count the Outside recovers towards: a stock below it grows");
        output.WriteLine("  back over the recovery period, and one above it is trimmed as turnover.");
        output.WriteLine();
        output.WriteLine("  An edge with no [[hinterland]] table prints zeros and that is not a fault.");
        output.WriteLine("  All four are shown so an absent Outside is visible rather than merely absent.");
        output.WriteLine();
    }

    /// <summary>Every composition standing behind an edge, and where each one came from.</summary>
    private static void Compositions(TextWriter output, World world, RulesetNames names)
    {
        output.WriteLine("WHO IS OUT THERE");
        output.WriteLine();
        output.WriteLine(F(
            $"  {"edge",-7}   {"stage",-10}   {"auth",4}   {"people",6}   {"target",6}   {"stock",6}   {"free",6}   {"admitted",8}   {"returned",8}"));
        output.WriteLine(F(
            $"  {Dash(7)}   {Dash(10)}   {Dash(4)}   {Dash(6)}   {Dash(6)}   {Dash(6)}   {Dash(6)}   {Dash(8)}   {Dash(8)}"));

        Span<HinterlandGroupReading> groups = stackalloc HinterlandGroupReading[GroupsShown];

        foreach (MapEdge edge in Edges)
        {
            int written = HinterlandGroupReading.Of(world, edge, groups);

            for (int group = 0; group < written; group++)
            {
                HinterlandGroupReading row = groups[group];

                output.WriteLine(F(
                    $"  {Edge(edge),-7}   {Stage(names, row.Composition.Stage),-10}   {(row.Authored ? "yes" : "no"),4}   {row.Composition.Members,6}   {row.Target,6}   {row.Stock,6}   {row.Free,6}   {row.Admitted,8}   {row.Returned,8}"));
            }
        }

        output.WriteLine();
        output.WriteLine("  `auth` is no on a group the Ruleset never declared. An emigration credits the");
        output.WriteLine("  Household it took back to the composition it matches, and creates that group");
        output.WriteLine("  when the file states none -- so a `no` row with a `target` of zero is the");
        output.WriteLine("  Outside holding people the city sent it, and it decays rather than recovers.");
        output.WriteLine();
        output.WriteLine("  A composition is a STORAGE KEY and not a group that decides anything. The");
        output.WriteLine("  Households in it are counted together because they are identical, and they");
        output.WriteLine("  split the moment one of them crosses.");
        output.WriteLine();
    }

    /// <summary>Per gate: the quota, what has gone through it today, and what is left.</summary>
    private static void Connections(TextWriter output, World world, RulesetNames names)
    {
        output.WriteLine("THE DOORS");
        output.WriteLine();
        output.WriteLine(F(
            $"  {"gate",-16}  {"edge",-7}  {"ceiling/Day",11}   {"admitted today",14}   {"remaining",9}"));
        output.WriteLine(F(
            $"  {Dash(16)}  {Dash(7)}  {Dash(11)}   {Dash(14)}   {Dash(9)}"));

        Span<HinterlandGateReading> doors = stackalloc HinterlandGateReading[DoorsShown];

        foreach (MapEdge edge in Edges)
        {
            int written = HinterlandGateReading.Of(world, edge, doors);

            for (int door = 0; door < written; door++)
            {
                HinterlandGateReading row = doors[door];

                output.WriteLine(F(
                    $"  {Name(names, row.Kind),-16}  {Edge(edge),-7}  {row.Ceiling,11}   {row.AdmittedToday,14}   {row.RemainingToday,9}"));
            }
        }

        output.WriteLine();
        output.WriteLine("  Nobody asked, so there is no `refused` column here: what came through is what");
        output.WriteLine("  the Outside sent, clipped by the ceiling. `remaining` at the ceiling means");
        output.WriteLine("  the door was never the constraint today -- read THE CIRCUIT for what was.");
        output.WriteLine();
        output.WriteLine("  A door has a QUOTA and it does not have a market. Two doors on one edge draw");
        output.WriteLine("  on one stock, so an edge's admissions are not divisible into each door's own.");
        output.WriteLine();
    }

    /// <summary>Every outcome an occasion can have, today and over the last complete Day.</summary>
    private static void Circuit(TextWriter output, World world)
    {
        output.WriteLine("THE CIRCUIT");
        output.WriteLine();
        output.WriteLine("  Who considered coming. The four outcomes are exclusive and they add up to");
        output.WriteLine("  `occasions`, so a column that does not sum is a defect rather than a finding.");
        output.WriteLine();
        output.WriteLine(F(
            $"  {"edge",-7}   {"when",-9}   {"occasions",9}   {"no door",9}   {"no sample",9}   {"stayed",9}   {"willing",9}"));
        output.WriteLine(F(
            $"  {Dash(7)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}"));

        foreach ((MapEdge edge, string when, HinterlandFlows flows) in Flows(world))
        {
            output.WriteLine(F(
                $"  {Edge(edge),-7}   {when,-9}   {flows.Occasions,9}   {flows.NoConnection,9}   {flows.NoSample,9}   {flows.StayedOutside,9}   {flows.Willing,9}"));
        }

        output.WriteLine();
        output.WriteLine("  `no sample` is nobody the engine could compare against -- no feasible dwelling");
        output.WriteLine("  to weigh the Outside's own rent and travel against -- and it is a different");
        output.WriteLine("  city from `stayed`, which compared and preferred where it already lives.");
        output.WriteLine();
        output.WriteLine("  What became of them. `reviewed` counts second thoughts by families ALREADY");
        output.WriteLine("  waiting and is deliberately outside the sum above: adding it to the fresh");
        output.WriteLine("  interest would report the same family twice.");
        output.WriteLine();
        output.WriteLine(F(
            $"  {"edge",-7}   {"when",-9}   {"admitted",9}   {"queued",9}   {"reviewed",9}   {"gave up",9}   {"went home",9}"));
        output.WriteLine(F(
            $"  {Dash(7)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}"));

        foreach ((MapEdge edge, string when, HinterlandFlows flows) in Flows(world))
        {
            output.WriteLine(F(
                $"  {Edge(edge),-7}   {when,-9}   {flows.Admitted,9}   {flows.Queued,9}   {flows.Reviewed,9}   {flows.Expired,9}   {flows.ChangedMind,9}"));
        }

        output.WriteLine();
        output.WriteLine("  `gave up` is a wait that ran out and `went home` is a review that changed its");
        output.WriteLine("  mind before it did. Both return the Household to the stock it came from.");
        output.WriteLine();
        output.WriteLine("  The Outside's own arithmetic, which is nobody's decision.");
        output.WriteLine();
        output.WriteLine(F(
            $"  {"edge",-7}   {"when",-9}   {"replenished",11}   {"turnover",11}   {"door lost",11}"));
        output.WriteLine(F(
            $"  {Dash(7)}   {Dash(9)}   {Dash(11)}   {Dash(11)}   {Dash(11)}"));

        foreach ((MapEdge edge, string when, HinterlandFlows flows) in Flows(world))
        {
            output.WriteLine(F(
                $"  {Edge(edge),-7}   {when,-9}   {flows.Replenished,11}   {flows.Turnover,11}   {flows.ConnectionLost,11}"));
        }

        output.WriteLine();
        output.WriteLine("  `door lost` cancels a wait because the edge has no gate left to wait at, and");
        output.WriteLine("  it is the demolition of a door showing up as a population figure.");
        output.WriteLine();
    }

    /// <summary>The population account, and whether the flows agree with the standing rows.</summary>
    private static void Account(TextWriter output, World world)
    {
        PopulationReading reading = PopulationReading.Of(world);

        output.WriteLine("THE POPULATION ACCOUNT");
        output.WriteLine();
        output.WriteLine(F($"  day                  {reading.Day,10}"));
        output.WriteLine(F(
            $"  people               {reading.People,10}   {reading.LivePeople} rows standing, residual {reading.Residual}"));
        output.WriteLine(F(
            $"  households           {reading.Households,10}   {reading.LiveHouseholds} rows standing, residual {reading.HouseholdResidual}"));
        output.WriteLine(F(
            $"  admitted, unhoused   {reading.PoolHouseholds,10}   {reading.PoolPeople} people, longest {Days((long)reading.PoolOldestWait)}"));
        output.WriteLine();
        output.WriteLine(F(
            $"  {"people",-10}   {"births",9}   {"admitted",9}   {"scenario",9}   {"departed",9}   {"died",9}   {"dissolved",9}"));
        output.WriteLine(F(
            $"  {Dash(10)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}"));

        People(output, "today", reading.Today);
        People(output, "yesterday", reading.Yesterday);

        output.WriteLine();
        output.WriteLine(F(
            $"  {"households",-10}   {"created",9}   {"formed",9}   {"admitted",9}   {"departed",9}   {"dissolved",9}   {"removed",9}"));
        output.WriteLine(F(
            $"  {Dash(10)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}   {Dash(9)}"));

        Households(output, "today", reading.Today);
        Households(output, "yesterday", reading.Yesterday);

        output.WriteLine();
        output.WriteLine("  A residual of zero says the classified flows and the standing rows agree about");
        output.WriteLine("  how many people are here. It says nothing whatever about whether the city is");
        output.WriteLine("  doing well, and a non-zero one is a door that wrote no entry -- a defect in");
        output.WriteLine("  the accounting rather than a finding about the city.");
        output.WriteLine();
        output.WriteLine("  Households and people are counted apart because neither derives from the");
        output.WriteLine("  other: a child leaving home makes a Household and adds nobody, an admission");
        output.WriteLine("  makes one Household and several people.");
        output.WriteLine();
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
            long admitted = world.PopulationLedger.Admissions[PopulationLedgerTable.Slot];

            output.WriteLine("  Nobody. An empty Pool does NOT by itself mean construction kept up with");
            output.WriteLine("  the doors. It reads exactly the same when nobody was willing to come and");
            output.WriteLine("  when no door could take them, so the figure that tells them apart is what");
            output.WriteLine("  actually came in:");
            output.WriteLine();
            output.WriteLine(F($"    people admitted over the run   {admitted}"));
            output.WriteLine();
            output.WriteLine(admitted > 0
                ? "  Non-zero, so every Household let in found a home before the run ended."
                : "  Zero, so there was nobody to house and the Pool says nothing about housing.");
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

    private static string Stage(RulesetNames names, byte stage) =>
        names.LifeStage(stage) ?? F($"stage {stage}");

    private static string Dash(int width) => new('-', width);

    private static void People(TextWriter output, string when, PopulationFlows flows) =>
        output.WriteLine(F(
            $"  {when,-10}   {flows.Births,9}   {flows.Admissions,9}   {flows.ScenarioAdditions,9}   {flows.Departures,9}   {flows.IllnessDeaths,9}   {flows.DissolutionPeople,9}"));

    private static void Households(TextWriter output, string when, PopulationFlows flows) =>
        output.WriteLine(F(
            $"  {when,-10}   {flows.HouseholdsCreated,9}   {flows.HouseholdsFormed,9}   {flows.HouseholdsAdmitted,9}   {flows.HouseholdsDeparted,9}   {flows.HouseholdsDissolved,9}   {flows.HouseholdsRemoved,9}"));

    /// <summary>Every edge twice, once for the Day in progress and once for the last complete one.</summary>
    private static IEnumerable<(MapEdge Edge, string When, HinterlandFlows Flows)> Flows(World world)
    {
        foreach (MapEdge edge in Edges)
        {
            HinterlandReading reading = HinterlandReading.Of(world, edge);

            yield return (edge, "today", reading.Today);
            yield return (edge, "yesterday", reading.Yesterday);
        }
    }

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
