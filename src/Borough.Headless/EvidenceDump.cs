namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

/// <summary>
/// <c>--evidence</c>: what the city can say about why something happened to it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Milestone 6 task 5, and it is the milestone's claim rendered in text rather than a picture.</b>
/// The three panels are the three things the milestone built: the <b>trail</b>, which keeps what the
/// world frees; the <b>aggregate</b>, which is what the trail turns into when it runs out of room; and
/// the <b>assembler</b>, which composes a live answer out of state nobody stored.
/// </para>
/// <para>
/// <b>It steps the world, and it is the third mode that does.</b> A trail is a record of things that
/// have happened, so a world at Tick 0 has an empty one — there is no <em>before</em> panel here for
/// the reason <c>--traffic</c> has none: the before is blank on every input.
/// </para>
/// <para>
/// ⚠ <b>It does <em>not</em> refuse a Ruleset that authors no <c>on_fail</c> chain, and that is a
/// departure from <c>--traffic</c>'s polarity rather than an oversight.</b> That mode refuses because
/// its two panels would be <em>identical</em> and an uninterpretable picture reads as a broken
/// instrument. Here the trail comes out fully populated and exactly one column is dashes, under a
/// heading that says which file fills it — a **legible absence**, which is the thing this milestone
/// exists to produce. Refusing would hide the corpus's own coverage hole behind a tidy error message.
/// ***An instrument that refuses to show a gap is an instrument that cannot report one.***
/// </para>
/// </remarks>
internal static class EvidenceDump
{
    /// <summary>How many retained entries to print. The trail holds 256; a terminal does not.</summary>
    private const int Shown = 12;

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
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

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        output.WriteLine("# Borough Evidence dump");
        output.WriteLine(
            $"# {options.Citizens} Citizens, {options.Ticks} Ticks, {world.Lots.Rows.LiveCount} Lots, "
            + $"{world.Buildings.Rows.LiveCount} Buildings standing.");
        output.WriteLine();

        Trail(output, world, names);
        output.WriteLine();
        WorstBuilding(output, world, rules, names);
        output.WriteLine();
        Vacancy(output, world);
        output.WriteLine();
        Finances(output, world);
        output.WriteLine();
        Journeys(output, world);

        return 0;
    }

    /// <summary>
    /// <c>02 §9</c>'s household finances, and the clause <c>CitizenEvidence</c> shipped declining.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The panel's first job is to say whether the question applies at all.</b> A Ruleset naming
    /// no <c>family = "money"</c> Resource opens no money Bin on anybody, so every reading is absent
    /// — and printing a table of zeroes there would be the exact failure the clause was omitted to
    /// avoid, which is a Household with nothing reading the same as a world with no currency.
    /// </para>
    /// <para>
    /// <b>Where it does apply, the interesting number is the count holding nothing rather than the
    /// mean.</b> <c>adr/0024</c> makes destitution a real state the game takes no position on, and a
    /// mean over a conserved pool is fixed by the endowment and the population — it moves only when
    /// somebody arrives or leaves, so it is the one figure a transfer circuit cannot change.
    /// </para>
    /// </remarks>
    private static void Finances(TextWriter output, World world)
    {
        long total = 0;
        long low = long.MaxValue;
        long high = long.MinValue;
        int counted = 0;
        int destitute = 0;
        int absent = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            CitizenEvidence evidence = Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));

            if (evidence.HouseholdBalance is not Money balance)
            {
                absent++;
                continue;
            }

            counted++;
            total += balance.Raw;
            low = balance.Raw < low ? balance.Raw : low;
            high = balance.Raw > high ? balance.Raw : high;

            if (balance.Raw == 0)
            {
                destitute++;
            }
        }

        output.WriteLine("## Household finances, as each Citizen's row reports it");
        output.WriteLine();

        if (counted == 0)
        {
            output.WriteLine(
                "  This Ruleset names no money, so no Household holds a money Bin and every Citizen's");
            output.WriteLine(
                "  row reports finances ABSENT rather than zero. That distinction is the whole of what");
            output.WriteLine(
                "  milestone 10 task 8 built: a zero here would say every Household is destitute, and");
            output.WriteLine(
                "  what is true is that the question does not apply. rulesets/taxed.toml is the file");
            output.WriteLine(
                $"  that makes it apply. ({absent} Citizens, all absent.)");

            return;
        }

        output.WriteLine(
            "  ⚠ These are HOUSEHOLD balances read through Citizens, so a Household of three is");
        output.WriteLine(
            "  counted three times. The panel is 02 section 9's Citizen row rather than a census of");
        output.WriteLine("  Households, and the sum below is not the city's money.");
        output.WriteLine();

        output.WriteLine($"  citizens with a balance   {counted,10}");
        output.WriteLine($"  citizens reporting absent {absent,10}");
        output.WriteLine($"  holding exactly nothing   {destitute,10}");
        output.WriteLine();
        output.WriteLine($"  lowest balance            {low,10}");
        output.WriteLine($"  highest balance           {high,10}");
        output.WriteLine($"  mean                      {total / counted,10}");

        if (destitute == counted)
        {
            // The reading milestone 10 task 8 exists to separate from ABSENT, and the one every
            // shipped Ruleset but taxed.toml produces: task 2 put a money Resource in all seven, so
            // every Household holds a money Bin, and only taxed.toml states an opening balance.
            output.WriteLine();
            output.WriteLine(
                "  ⚠ EVERY Household holds exactly nothing, and this is DESTITUTION rather than a");
            output.WriteLine(
                "  world without money. The distinction is the point: this Ruleset names a money");
            output.WriteLine(
                "  Resource, so every Household has a balance and every balance is empty. Money");
            output.WriteLine(
                "  enters a world one way — [households] opening_balance_min/max, read by the");
            output.WriteLine(
                "  populator through World.Endow — and only rulesets/taxed.toml states it. adr/0024");
            output.WriteLine(
                "  makes the Outside Connection money's only other door, and that is milestone 11.");
        }

        if (absent > 0)
        {
            output.WriteLine();
            output.WriteLine(
                "  ⚠ Some rows report absent and some do not, in one world. A Household holds a money");
            output.WriteLine(
                "  Bin from the moment it is fitted, so this is a Citizen whose Household does not");
            output.WriteLine(
                "  resolve — CitizenIsInExactlyOneHousehold is the invariant that says so.");
        }
    }

    /// <summary>
    /// How the city's journeys ended, per Citizen, from the answer the assembler gives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Milestone 6 task 7, and it exists because of a finding this corpus has already made
    /// twice.</b> 5b-bis task 6: ***a Census family with no reader is a family nobody can see*** — 5b
    /// built <c>TripCounter</c>, wired it through the Census, tested it, and printed it nowhere, so for
    /// a whole milestone its only reader was the suite. <c>CitizenTable.LastTripFate</c> would have
    /// been the same shape: a saved column, tested, assembled, and invisible.
    /// </para>
    /// <para>
    /// <b>A distribution rather than one Citizen</b>, because a single Citizen's Fate is a value a test
    /// already asserts and tells a reader nothing about the city. What this shows that nothing else can
    /// is the <b>silent population</b> — how many people have never finished a journey at all — which
    /// is a count of everybody the commute has never reached, and is the number this column exists to
    /// make nameable.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads through <c>Evidence.OfCitizen</c> rather than off the column</b>, so the panel and
    /// the panel a host would build take the same path. Reading the column directly would print a
    /// number the assembler could disagree with.
    /// </para>
    /// </remarks>
    private static void Journeys(TextWriter output, World world)
    {
        int[] counts = new int[5];
        int never = 0;
        int inFlight = 0;
        ushort newest = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            CitizenEvidence evidence = Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));

            if (evidence.Trip is not null)
            {
                inFlight++;
            }

            if (evidence.LastTrip is not PastTripEvidence last)
            {
                never++;
                continue;
            }

            counts[(int)last.Fate]++;

            if (last.EndedDay > newest)
            {
                newest = last.EndedDay;
            }
        }

        output.WriteLine("## Journeys, by how each Citizen's last one ended");
        output.WriteLine();
        output.WriteLine(
            "  A Trip row is freed on the line after its Fate is asserted, so this is the Citizen's");
        output.WriteLine(
            "  copy and not the Trip's. `never travelled` is everybody the commute has not reached.");
        output.WriteLine();

        output.WriteLine($"  completed               {counts[(int)TripFate.Completed],8}");
        output.WriteLine($"  no route found          {counts[(int)TripFate.NoRouteFound],8}");
        output.WriteLine($"  beyond commute budget   {counts[(int)TripFate.ExceededCommuteBudget],8}");
        output.WriteLine($"  stranded                {counts[(int)TripFate.Stranded],8}");
        output.WriteLine($"  never travelled         {never,8}");
        output.WriteLine();
        output.WriteLine($"  in flight right now     {inFlight,8}");
        output.WriteLine(
            $"  newest ended on Day     {newest,8}  (of {world.Tick.Raw / Ticks.PerDay} elapsed)");
    }

    /// <summary>
    /// Refuses a Ruleset that condemns nothing, because the trail would be empty for a reason that
    /// is not about Evidence.
    /// </summary>
    /// <remarks>
    /// <b><c>--zones</c>' polarity.</b> Decline is content: a file with no <c>[[zone_rule]]</c> never
    /// looks at a Lot, and a file whose kinds set no <c>condemn_after</c> never condemns one — in both
    /// cases an empty trail would read as a broken accumulator rather than as a Ruleset that declines
    /// nothing. The <em>condition</em> being absent is a different case and is printed rather than
    /// refused; see this class's own remark.
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        if (rules.ZoneRules.Length == 0)
        {
            output.WriteLine(
                "This Ruleset declares no [[zone_rule]], so nothing ever looks at a Lot and nothing "
                + "is ever condemned. The trail would be empty, and an empty trail reads as a broken "
                + "accumulator rather than as a file that declines nothing. Decline is content.");
            output.WriteLine();
            output.WriteLine("  --evidence --ruleset rulesets/diagnosed.toml --ticks 2048");

            return 3;
        }

        for (byte kind = 1; kind <= byte.MaxValue; kind++)
        {
            // ⚠ THE PREMISES' THRESHOLD ONLY, and tenancy_ends_after_days deliberately does not
            // count here. The condemnation trail is a LOT's, and a tenancy that ends leaves the Lot,
            // the kind and the Building exactly as they were -- so evicted.toml, which ends tenancies
            // constantly, still produces an empty trail and is still the wrong file for --evidence.
            if (rules.Declares(kind) && rules.Kind(kind).CondemnAfterTicks > 0)
            {
                return null;
            }

            if (kind == byte.MaxValue)
            {
                break;
            }
        }

        output.WriteLine(
            "No [[building]] in this Ruleset sets condemn_after_days, so no Building can ever be "
            + "condemned however badly it is supplied. Decline is a duration and absent "
            + "means never, which is what every Ruleset written before decline existed already "
            + "meant — so this is a file that declines nothing rather than an accumulator that has "
            + "lost something.");
        output.WriteLine();
        output.WriteLine("  --evidence --ruleset rulesets/diagnosed.toml --ticks 2048");

        return 3;
    }

    /// <summary>
    /// The trail: what it kept, what it folded away, and what the entries say.
    /// </summary>
    /// <remarks>
    /// <b>The aggregate is printed as a row of the same table rather than as a footnote</b>, because
    /// that is what it is — <c>CondemnationTrailTable</c> makes slot 0 an entry and not a special
    /// case. Printing it in line is what makes ***attribution decays to magnitude*** visible: the
    /// count survives in the same column as everybody else's and the three identity columns are gone.
    /// </remarks>
    private static void Trail(TextWriter output, World world, RulesetNames names)
    {
        CondemnationTrailTable trail = world.CondemnationTrail;
        long total = trail.CondemnationsRecorded();
        long aggregated = trail.Condemnations[CondemnationTrailTable.AggregateSlot];

        output.WriteLine("## The condemnation trail — what the world freed and this kept");
        output.WriteLine(
            $"{total} Buildings condemned. {trail.Count} of {CondemnationTrailTable.Retained} are "
            + $"retained in full; {aggregated} have been folded into the aggregate, which keeps the "
            + "count and drops the identity.");

        int named = 0;

        for (int i = 0; i < trail.Count; i++)
        {
            if (!trail.Condition[trail.EntrySlot(i)].IsNone)
            {
                named++;
            }
        }

        output.WriteLine();
        output.WriteLine($"{named} of {trail.Count} retained entries name the condition that "
            + "condemned them.");

        if (named == 0)
        {
            // Printed rather than refused, and the wording is the finding. Measured 2026-08-17: the
            // condition column is None for every entry of every shipped file at 512 through 8192
            // Ticks, because no shipped Ruleset authors an on_fail chain. 02 §9 calls this question
            // "the hardest and the most valuable", and the answer the build can give is exactly as
            // good as the file it is given.
            output.WriteLine(
                "  ⚠ NONE OF THEM DO, and that is the Ruleset rather than the trail. A condition "
                + "reaches RuleInstance.reported only where an author wrote an on_fail chain ending "
                + "in a reporting terminal, and this file writes none — so the trail records when, "
                + "where and what kind, and never why. rulesets/diagnosed.toml is minimal.toml with "
                + "one such terminal and nothing else changed.");
        }

        output.WriteLine();
        output.WriteLine("       tick        lot  kind            condition       count");
        output.WriteLine("  ---------  ---------  --------------  --------------  -----");

        Row(
            output,
            "aggregate",
            Dash,
            Name(names.Kind(trail.Kind[CondemnationTrailTable.AggregateSlot])),
            Name(names.Condition(trail.Condition[CondemnationTrailTable.AggregateSlot])),
            aggregated);

        for (int i = trail.Count - 1; i >= 0 && i > trail.Count - 1 - Shown; i--)
        {
            int slot = trail.EntrySlot(i);

            // The Lot's SLOT rather than its handle, because a Handle's Index and Generation are
            // internal to Borough.Core -- deliberately, so that nothing outside can sort by one or
            // treat it as a name. A slot is what a reader can look up, and a dash is the honest
            // answer for a handle that no longer resolves. No dump before this one has ever had to
            // identify an individual entity at all.
            string lot = world.Lots.Rows.TryResolve(trail.Lot[slot], out int lotSlot)
                ? lotSlot.ToString(CultureInfo.InvariantCulture)
                : Dash;

            Row(
                output,
                trail.Tick[slot].Raw.ToString(CultureInfo.InvariantCulture),
                lot,
                Name(names.Kind(trail.Kind[slot])),
                Name(names.Condition(trail.Condition[slot])),
                trail.Condemnations[slot]);
        }

        if (trail.Count > Shown)
        {
            output.WriteLine($"  … and {trail.Count - Shown} more retained entries.");
        }
    }

    /// <summary>
    /// One Building's answer in full, assembled rather than stored.
    /// </summary>
    /// <remarks>
    /// <b>The one under the most pressure, chosen by scanning</b>, because a Building picked by slot
    /// would usually be a healthy one and a healthy Building's answer is a table of noughts. The scan
    /// is deterministic — first maximum wins — so the same world always shows the same Building.
    /// </remarks>
    private static void WorstBuilding(
        TextWriter output, World world, Ruleset rules, RulesetNames names)
    {
        int worst = Rows.NoSlot;
        long pressure = -1;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            long found = Evidence.OfBuilding(world, world.Buildings.Rows.At(slot)).Pressure;

            if (found > pressure)
            {
                pressure = found;
                worst = slot;
            }
        }

        output.WriteLine("## One Building, assembled — the worst-off one standing");

        if (worst == Rows.NoSlot)
        {
            output.WriteLine("Nothing is standing, so there is nothing to assemble.");
            return;
        }

        BuildingEvidence evidence = Evidence.OfBuilding(world, world.Buildings.Rows.At(worst));
        string kind = Name(names.Kind(evidence.Kind));
        string lot = world.Lots.Rows.TryResolve(evidence.Lot, out int lotSlot)
            ? lotSlot.ToString(CultureInfo.InvariantCulture)
            : Dash;

        output.WriteLine(
            $"Building slot {worst}, a {kind} on Lot slot {lot}. Its kind declares "
            + $"{evidence.DeclaredOccupancy} occupants and {evidence.DeclaredJobs} jobs; it holds "
            + $"{evidence.Occupants.Length} Households and {evidence.Workers.Length} workers.");

        if (!evidence.IsDeclared)
        {
            output.WriteLine(
                "  ⚠ Its kind is not declared by the Ruleset in force, so it is DERELICT (adr/0068): "
                + "it keeps what it has and takes nothing new, and the two declared counts above "
                + "mean nothing.");
        }

        output.WriteLine();
        // The occupant list resolved once and shared by both tables, so that `tenant 2` means
        // the same family in the Bins and in the Rules.
        Handle<Household>[] tenants = evidence.Occupants.ToArray();

        output.WriteLine("  bin             holder     level  capacity");
        output.WriteLine("  --------------  ---------  -----  --------");

        foreach (BinEvidence bin in evidence.Bins.ToArray())
        {
            string holder = bin.Tenant.IsNone
                ? "premises"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"tenant {Array.IndexOf(tenants, bin.Tenant) + 1}");

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {Name(names.Resource(bin.Resource)),-14}  {holder,-9}  {bin.Level,5}  "
                + $"{bin.Capacity,8}"));
        }

        output.WriteLine(
            "⚠ The capacity column is the PREMISES' either way (adr/0141): the Building's kind "
            + "declares every ceiling on the Lot and the tenant holds the level in the ones that "
            + "would empty if it left. A tenant's balance is not here — it is money, it is "
            + "unbounded, and the Household finances panel is where a reader meets it.");

        output.WriteLine();
        // `tenant N` is A POSITION IN THIS BUILDING'S OCCUPANT LIST rather than a slot, and that is
        // 05 §1's boundary rather than a presentation choice: Handle<T>.Index is internal precisely
        // so that identity does not escape the core, and `tenant 2` is what a reader of one
        // Building's panel actually wants -- a slot number is a fact about the allocator.
        output.WriteLine("  rule            whose      rate  last ran  state    reports         missed");
        output.WriteLine("  --------------  ---------  ----  --------  -------  --------------  ------");

        foreach (RuleEvidence rule in evidence.Rules.ToArray())
        {
            // adr/0141: the subject, which this panel could not say until milestone 25 task 4. A
            // dwelling holding three Households prints three `restock` rows, and `premises` against
            // `household 12` is the difference between a Rule that can condemn the Building and one
            // that can only end a tenancy.
            string whose = rule.Tenant.IsNone
                ? "premises"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"tenant {Array.IndexOf(tenants, rule.Tenant) + 1}");

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {Name(names.Rule(rule.Rule)),-14}  {whose,-9}  {rule.Rate,4}  "
                + $"{rule.LastRan.Raw,8}  "
                + $"{(rule.Succeeded ? "ok" : rule.Blocked.ToString().ToLowerInvariant()),-7}  "
                + $"{Name(names.Condition(rule.Reported)),-14}  {rule.MissedFirings,6}"));
        }

        // TWO THRESHOLDS SINCE MILESTONE 17, and printing one against both readings was wrong in a
        // way nobody could see: the tenant line said "against the same" and meant it.
        KindDefinition declared = evidence.IsDeclared ? rules.Kind(evidence.Kind) : default;

        int condemns = declared.CondemnAfterTicks;
        int endsTenancy = declared.TenancyEndsAfterTicks;

        output.WriteLine();
        output.WriteLine(
            $"Failure pressure {evidence.Pressure} missed firings — the LONGEST of its Rules' and "
            + $"not their sum (adr/0053) — against a condemn_after_days of {condemns}. Nothing "
            + "stores that maximum; it is recomputed here, which is what an assembler is for. ⚠ The "
            + "pressure is in FIRINGS and the threshold is in DAYS: milestone 17 moved the threshold "
            + "to a duration and left the ranking in firings, because they answer different "
            + "questions (ZoneRuleEngine.Worst says which).");

        output.WriteLine(
            $"Its worst TENANT is at {evidence.TenantPressure} missed firings against a "
            + $"tenancy_ends_after_days of {endsTenancy}, and that is a different verdict "
            + "(adr/0141): the premises' pressure "
            + "condemns the BUILDING, a tenant's ends the TENANCY and leaves the premises standing. "
            + "⚠ This line is a maximum over the whole Building and does not say WHICH tenant — the "
            + "`whose` column above does, per Rule.");

        output.WriteLine(
            "⚠ `last ran` is DERIVED and not a column. A Rule Instance is armed on the Event Wheel "
            + "or asleep on a Bin's wait list and never both, so an armed one last fired at its due "
            + "Tick minus its rate and a sleeping one at its due Tick. The one case this cannot tell "
            + "apart is a Rule that has never run, because a Building has no creation Tick.");
    }

    /// <summary>
    /// Why the vacant Lots are vacant, counted.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Every flag here reads zero on every world this runner can build, and the panel is kept
    /// because that is worth printing.</b> Measured 2026-08-17: <b>0 of 150</b> vacant Lots on the
    /// golden fixture lack frontage, and <b>every</b> vacant Lot is admitted by a Zone Rule — because
    /// <c>RoadGenerator</c> lays the lattice the subdivider carves the Lots out of, and
    /// <c>SyntheticCity</c> paints bit 0 on every Lot while the shipped <c>[[zone_rule]]</c> admits
    /// bit 0. The flags need a city somebody <b>zoned</b> and <b>roaded</b>, and no runner mode issues
    /// any Command but <c>Populate</c>. So the instrument is exercised, the world cannot feed it, and
    /// the panel says which of those two it is. ***A row of noughts under a heading that explains them
    /// is a measurement; the same row with no heading is a defect.***
    /// </remarks>
    private static void Vacancy(TextWriter output, World world)
    {
        int vacant = 0;
        int noFrontage = 0;
        int nobodySeeking = 0;
        int notZoned = 0;
        int unexplained = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.IsVacant(slot))
            {
                continue;
            }

            vacant++;

            VacancyReason reason = Evidence.OfLot(world, world.Lots.Rows.At(slot)).Reason;

            if (reason.HasFlag(VacancyReason.NoFrontage))
            {
                noFrontage++;
            }

            if (reason.HasFlag(VacancyReason.NobodySeeking))
            {
                nobodySeeking++;
            }

            if (reason.HasFlag(VacancyReason.NotZoned))
            {
                notZoned++;
            }

            if (reason == VacancyReason.None)
            {
                unexplained++;
            }
        }

        output.WriteLine($"## Why {vacant} Lots are vacant — 02 §9's hardest question");
        output.WriteLine($"  no frontage           {noFrontage,6}");
        output.WriteLine($"  nobody seeking        {nobodySeeking,6}");
        output.WriteLine($"  not zoned             {notZoned,6}");
        output.WriteLine($"  the build cannot say  {unexplained,6}");
        output.WriteLine();
        output.WriteLine(
            "⚠ THE FIRST THREE ARE ZERO ON EVERY WORLD THIS RUNNER CAN BUILD, and that is the world "
            + "rather than the instrument. RoadGenerator lays the lattice the Lots are carved out of, "
            + "so a generated Lot always has frontage; SyntheticCity paints bit 0 on every Lot and "
            + "the shipped [[zone_rule]] admits bit 0, so every Lot is zoned. Those two flags need a "
            + "city somebody ZONED and ROADED, and no runner mode issues any Command but Populate — "
            + "CommandKind.Zone and Connect have no production call site anywhere in the project.");
        output.WriteLine(
            "  `the build cannot say` is an honest answer and not a hole: a Zone Rule SAMPLES rather "
            + "than sweeps (adr/0059), so `not yet looked at` is the ordinary state of most vacant "
            + "Lots. 02 §9 names two more reasons — conditions below tolerance, and no capital — and "
            + "neither has a mechanism anywhere in the build. They arrive with milestone 17.");
    }

    private static void Row(
        TextWriter output, string tick, string lot, string kind, string condition, long count) =>
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  {tick,9}  {lot,9}  {kind,-14}  {condition,-14}  {count,5}"));

    /// <summary>What a column holds when the fact it names does not exist.</summary>
    private const string Dash = "—";

    /// <summary>
    /// A name, or a dash.
    /// </summary>
    /// <remarks>
    /// <b>The fallback is spelled here rather than in <see cref="RulesetNames"/></b>, and deliberately:
    /// what to show for an id nobody named is a presentation decision, and <c>Borough.Formats</c> is
    /// not the presentation layer. A dash rather than the number, because the number is an index into
    /// a file that did not mention it and would read as information.
    /// </remarks>
    private static string Name(string? found) => found ?? Dash;
}
