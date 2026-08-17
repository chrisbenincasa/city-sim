namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
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

        return 0;
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
            if (rules.Declares(kind) && rules.Kind(kind).CondemnAfter > 0)
            {
                return null;
            }

            if (kind == byte.MaxValue)
            {
                break;
            }
        }

        output.WriteLine(
            "No [[building]] in this Ruleset sets condemn_after, so no Building can ever be "
            + "condemned however badly it is supplied. adr/0053 makes decline a duration and absent "
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
        int total = trail.CondemnationsRecorded();
        int aggregated = trail.Condemnations[CondemnationTrailTable.AggregateSlot];

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
        output.WriteLine("  bin             level  capacity");
        output.WriteLine("  --------------  -----  --------");

        foreach (BinEvidence bin in evidence.Bins.ToArray())
        {
            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {Name(names.Resource(bin.Resource)),-14}  {bin.Level,5}  {bin.Capacity,8}"));
        }

        output.WriteLine();
        output.WriteLine("  rule            rate  last ran  state    reports         missed");
        output.WriteLine("  --------------  ----  --------  -------  --------------  ------");

        foreach (RuleEvidence rule in evidence.Rules.ToArray())
        {
            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {Name(names.Rule(rule.Rule)),-14}  {rule.Rate,4}  {rule.LastRan.Raw,8}  "
                + $"{(rule.Succeeded ? "ok" : rule.Blocked.ToString().ToLowerInvariant()),-7}  "
                + $"{Name(names.Condition(rule.Reported)),-14}  {rule.MissedFirings,6}"));
        }

        int threshold = evidence.IsDeclared ? rules.Kind(evidence.Kind).CondemnAfter : 0;

        output.WriteLine();
        output.WriteLine(
            $"Failure pressure {evidence.Pressure} missed firings — the LONGEST of its Rules' and "
            + $"not their sum (adr/0053) — against a condemn_after of {threshold}. Nothing stores "
            + "that maximum; it is recomputed here, which is what an assembler is for.");

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
        TextWriter output, string tick, string lot, string kind, string condition, int count) =>
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
