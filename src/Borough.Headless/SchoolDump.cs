namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

/// <summary>
/// Who can get to a school, and what it costs the ones who cannot.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The question is whether a service Building's reach is anything like its radius.</b>
/// <c>adr/0032</c> threw out the distance field on one case, and that predicts a number: the share
/// of Households with a school inside the straight-line box that the Road Graph can deliver.
/// <b>At 100% the field was right and the mechanism is ceremony.</b> ⚠ It places the schools
/// <i>through the verb</i> — a dump raising a Building directly would demonstrate something no
/// player can do. <c>plans/0045</c> holds what it found.
/// </para>
/// </remarks>
internal static class SchoolDump
{
    /// <summary>How many Day rows the trajectory prints before it starts thinning.</summary>
    private const int Rows = 32;

    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames names))
        {
            return 2;
        }

        if (!rules.ServesAny(Need.Education))
        {
            output.WriteLine(
                "This Ruleset declares no [[building]] with serves = \"education\", so there is "
                + "nothing for a Household to attend and every row of this dump would report the "
                + "same absence. A service Building is content, and the verb that places one refuses "
                + "any kind that does not declare what it is attended for.");
            output.WriteLine();
            output.WriteLine("  --school --ruleset rulesets/schooled.toml --citizens 2000 --ticks 40960");

            return 2;
        }

        if (!rules.DeclaresLifeStages)
        {
            output.WriteLine(
                "This Ruleset declares no [[life_stage]], so Citizens.Age is written by nothing and "
                + "every Citizen carries zero. A child is Age == 0 in a world WITH demographics; "
                + "without them that same zero means the column was never written, and "
                + "ServiceEngine refuses to read a city of adults as a city of children. Nobody "
                + "would attend anything.");

            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        byte kind = ServiceKind(rules, Need.Education);
        int placed = Found(simulation, world, kind, options.Schools, output);

        List<Reading> series = [];

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            // 🔴 GATED ON THE SIMULATION'S OWN TICK AND NOT ON THE LOOP COUNTER, AND THE FIRST
            // SPELLING OF THIS LOOP GOT IT WRONG IN A WAY StageDump's REMARK DID NOT PREDICT.
            //
            // That dump warns against `simulation.Tick % PerDay == 0`, because Step runs Tick T and
            // then advances, so the property reads T+1 and fires one Tick early. The fix there was to
            // gate on the loop counter -- and the fix there is WRONG HERE, because Found() spends a
            // Tick per school before the loop starts. With four schools the loop's Tick 0 is the
            // world's Tick 4, so every sample landed four Ticks past a Day boundary and read three
            // counters that ServiceEngine had already reset. ***The table printed 109 families and
            // zero occasions for 146 Days.***
            //
            // ⚠ THE LESSON IS NOT "USE THE OTHER ONE". Both spellings are a loop assuming it knows
            // the world's Tick, and the only thing that does is the world. `Tick - 1` is the Tick
            // Step just ran; a flow belongs to that one.
            if ((simulation.Tick.Raw - 1UL) % (ulong)Ticks.PerDay == 0UL)
            {
                series.Add(Reading.Of(world, simulation, tick));
            }
        }

        Header(options, rules, names, placed, series.Count, output);
        output.WriteLine();
        Trajectory(series, output);
        output.WriteLine();
        Reach(series, world, placed, output);
        Supply(world, output);
        Admission(world, simulation, output);

        return 0;
    }

    // ---- placing the schools -------------------------------------------------------------------

    /// <summary>
    /// Issues one <c>service</c> per school, spread across the vacant Lots the generator left.
    /// </summary>
    /// <remarks>
    /// <b>Spread by STRIDING the vacant Lot list rather than by choosing coordinates</b>, because a
    /// dump that named Tiles would be authoring a site plan. ⚠ <b>It is not a good siting policy and
    /// is not meant to be</b>: siting schools well is the skill the verb exists to ask for, and a
    /// dump that did it well would be measuring its own cleverness. Before the run rather than during
    /// it, so every Day of the table asks the same question.
    /// </remarks>
    private static int Found(
        Simulation simulation, World world, byte kind, int wanted, TextWriter output)
    {
        if (wanted == 0)
        {
            return 0;
        }

        List<int> vacant = [];
        LotTable lots = world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (lots.Rows.IsLive(slot) && lots.IsVacant(slot))
            {
                vacant.Add(slot);
            }
        }

        if (vacant.Count == 0)
        {
            output.WriteLine(
                "# No vacant Lot: every one the subdivider carved already holds a Building, so there "
                + "is nowhere to place a school. Run with fewer Citizens.");

            return 0;
        }

        if (wanted > vacant.Count)
        {
            wanted = vacant.Count;
        }

        int stride = vacant.Count / wanted;
        Span<Command> one = stackalloc Command[1];
        int placed = 0;

        for (int i = 0; i < wanted; i++)
        {
            int lot = vacant[i * stride];

            one[0] = Command.Service(lots.East[lot], lots.North[lot], kind);
            simulation.Step(new TickInput(one, simulation.RulesetInForce));
            placed++;
        }

        return placed;
    }

    private static byte ServiceKind(Ruleset rules, Need need)
    {
        for (byte kind = 1; rules.Declares(kind); kind++)
        {
            if (rules.Kind(kind).Serves == need)
            {
                return kind;
            }
        }

        return 0;
    }

    // ---- the reading ---------------------------------------------------------------------------

    private readonly record struct Reading(
        ulong Tick, int Attended, int Unreached, int NoService, int Full, long Depth, int Schooled,
        int Families)
    {
        internal static Reading Of(World world, Simulation simulation, ulong tick)
        {
            long depth = 0;
            int schooled = 0;
            int families = 0;

            HouseholdTable households = world.Households;

            for (int slot = 0; slot < households.Rows.SlotCount; slot++)
            {
                if (!households.Rows.IsLive(slot))
                {
                    continue;
                }

                // A Household with no child has no occasion, so its zero is "nobody attends" rather
                // than "well served" -- averaging it in would dilute the depth with abstainers and
                // make a schooling crisis read as a mild one.
                if (!HasChild(world, slot))
                {
                    continue;
                }

                families++;
                depth += households.Education[slot];

                if (households.Education[slot] == 0)
                {
                    schooled++;
                }
            }

            ServiceEngine services = simulation.Services;

            return new Reading(
                tick, services.Attended, services.Unreached, services.NoService, services.Full,
                depth, schooled, families);
        }
    }

    private static bool HasChild(World world, int household)
    {
        foreach (int member in world.Members.Walk(household))
        {
            if (world.Citizens.Age[member] == 0)
            {
                return true;
            }
        }

        return false;
    }

    // ---- the panels ----------------------------------------------------------------------------

    private static void Header(
        Options options, Ruleset rules, RulesetNames names, int placed, int days, TextWriter output)
    {
        output.WriteLine("# Borough school dump — who can get to a school, and what it costs the rest");
        string sizing = F($"# {options.Citizens:N0} Citizens, {options.Ticks:N0} Ticks, {days:N0} Days");
        output.WriteLine(F($"{sizing}, {placed:N0} schools placed through the `service` verb."));
        output.WriteLine("#");
        output.WriteLine(
            "# adr/0032: a Service reaches people because somebody makes a JOURNEY. `unreached` is a");
        output.WriteLine(
            "# school inside the straight-line box that no route delivers within the Commute Budget —");
        output.WriteLine(
            "# Severance, and the one number a coverage Map Layer could never have produced.");
        output.WriteLine("#");
        string? kind = names.Kind(ServiceKind(rules, Need.Education));
        string rates = F(
            $"education degrade {rules.Needs.EducationDegrade:N0}/Day, recover {rules.Needs.EducationRecover:N0}, floor {rules.Needs.Floor:N0}");
        output.WriteLine(F($"# Ruleset: {options.RulesetPath}, service kind \"{kind}\", {rates}."));
    }

    private static void Trajectory(List<Reading> series, TextWriter output)
    {
        if (series.Count == 0)
        {
            return;
        }

        int stride = series.Count <= Rows ? 1 : series.Count / Rows;

        output.WriteLine(
            "  Day     attended  unreached  no school   full   families  at zero   mean depth"
            + (stride > 1 ? F($"   (every {stride:N0} Days)") : string.Empty));

        for (int i = 0; i < series.Count; i += stride)
        {
            Reading r = series[i];
            long mean = r.Families == 0 ? 0 : r.Depth / r.Families;

            string flows = F($"  {i,-6:N0}  {r.Attended,8:N0}  {r.Unreached,9:N0}  {r.NoService,9:N0}");
            output.WriteLine(F(
                $"{flows}  {r.Full,5:N0}  {r.Families,9:N0}  {r.Schooled,7:N0}  {mean,11:N0}"));
        }
    }

    /// <summary>The one number this dump exists for: what share of in-range schools were reached.</summary>
    private static void Reach(List<Reading> series, World world, int placed, TextWriter output)
    {
        long attended = 0;
        long unreached = 0;
        long none = 0;
        long full = 0;

        foreach (Reading r in series)
        {
            attended += r.Attended;
            unreached += r.Unreached;
            none += r.NoService;
            full += r.Full;
        }

        long inBox = attended + unreached + full;
        long occasions = inBox + none;

        output.WriteLine("  Reach");
        output.WriteLine(F($"    schools standing        {placed,10:N0}"));
        output.WriteLine(F($"    occasions               {occasions,10:N0}"));
        output.WriteLine(F($"    school in the box       {inBox,10:N0}"));
        output.WriteLine(F($"    ... and delivered       {attended,10:N0}"));
        output.WriteLine(F($"    ... and unreachable     {unreached,10:N0}"));
        output.WriteLine(F($"    ... and full            {full,10:N0}"));
        output.WriteLine(F($"    no school in the box    {none,10:N0}"));

        if (inBox > 0)
        {
            output.WriteLine(F(
                $"    deliverable share       {(100L * attended) / inBox,9:N0}%")
                + "   — what a distance field would have called 100%.");
        }

        output.WriteLine();
        output.WriteLine(F(
            $"    {world.Buildings.Rows.LiveCount:N0} Buildings stand, of which {placed:N0} are schools."));
    }

    /// <summary>
    /// What each school actually holds, which is the panel the ceiling cannot be chosen without.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A rate is only choosable against a floor area somebody has looked at.</b>
    /// <c>[capacity] floor_tiles_per_place</c> is the one rate in that table with no anchor outside
    /// this repository — there is no standing city to divide, because no kind ever declared a place
    /// count — so ***the substitute for a derivation is a reading***, and this is where it is taken.
    /// ⚠ <b>It prints in a world with no rate too</b>, and that is the point: the tally advances
    /// everywhere and the ceiling binds only where stated, so a designer can see what a school is
    /// asked for before deciding what it may give.
    /// </remarks>
    private static void Supply(World world, TextWriter output)
    {
        int rate = world.Rules.Capacity.FloorTilesPerPlace;

        output.WriteLine();
        output.WriteLine(rate > 0
            ? F($"  Each school — floor_tiles_per_place = {rate:N0}, so places derive from the ground")
            : "  Each school — no [capacity] floor_tiles_per_place, so none of them is ever full");
        output.WriteLine("    slot      floor    places   attended on the last Day");

        BuildingTable buildings = world.Buildings;
        int shown = 0;

        for (int slot = 0; slot < buildings.Rows.SlotCount && shown < Rows; slot++)
        {
            if (!buildings.Rows.IsLive(slot)
                || !world.Rules.Declares(buildings.Kind[slot])
                || !world.Rules.Kind(buildings.Kind[slot]).IsService)
            {
                continue;
            }

            shown++;

            string places = rate > 0
                ? F($"{world.DeclaredPlaces(slot),8:N0}")
                : "       —";

            output.WriteLine(F(
                $"    {slot,-8:N0}  {world.FloorTilesOf(slot),7:N0}  {places}  {buildings.AttendedToday[slot],9:N0}"));
        }
    }

    /// <summary>
    /// Whether the ordering that decides WHO gets the last place agrees with a per-school rule.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE ONE PANEL HERE THAT IS NOT A COUNT, AND IT IS HERE BECAUSE EVERY OTHER ONE IS.</b>
    /// The Day admission stopped being decided by Household slot order and started being decided by
    /// distance, ***this dump printed byte-for-byte the same output it had printed before*** — 103
    /// attended, 6 turned away, 95%, the four schools at 21/18/16/48. Every column above is a tally
    /// of how many, and the change was a change of which. ⚠ <b>That is the reported 100% arriving a
    /// second time</b>: a share-of-occasions number could not see that one school served the whole
    /// city, and a count of the turned-away cannot see that the wrong ones were turned away.
    /// </para>
    /// <para>
    /// <b>An inversion is a BLOCKING PAIR: a family turned away from a full door that lives nearer
    /// that school than somebody the school admitted.</b> Both would rather have each other, which
    /// is what a stable matching forbids — so this column reads zero exactly when
    /// <c>ServiceEngine.Match</c> did its job.
    /// </para>
    /// <para>
    /// 🔴 <b>IT WAS BUILT TO PRICE A GAP AND IT NOW GUARDS THE THING THAT CLOSED IT, WHICH IS THE
    /// SAME INSTRUMENT DOING BOTH JOBS.</b> Against the nearest-first ordering it read
    /// <b>0 / 55 / 4 / 0</b> at one, two, four and eight schools — zero at both ends and total in the
    /// middle, because a mechanism with no scarcity in it cannot be unfair. Against deferred
    /// acceptance it reads <b>0 / 0 / 0 / 0</b>. ***A number that justified building something is the
    /// cheapest possible test that it works***, and this one was already written. <c>plans/0054</c>
    /// <b>F6a</b> holds the readings.
    /// </para>
    /// <para>
    /// ⚠ <b>It prints the margin beside the count</b>, because a count says the matching was unstable
    /// and never says by how much. ⚠ <b>And it is a LOWER BOUND on the disagreement</b>: one blocking
    /// pair can stand for a chain of displacements. What it is exact about is the zero.
    /// </para>
    /// </remarks>
    private static void Admission(World world, Simulation simulation, TextWriter output)
    {
        ServiceAdmission.Reading reading = ServiceAdmission.Measure(world, simulation.Services);

        output.WriteLine();
        output.WriteLine("  Admission on the last Day — is the matching stable?");
        output.WriteLine(F($"    admitted                {reading.Admitted,10:N0}"));
        output.WriteLine(F($"    turned away at the door {reading.TurnedAway,10:N0}"));
        output.WriteLine(F($"    ... of which INVERTED   {reading.Inverted,10:N0}"));

        output.WriteLine(reading.Inverted == 0
            ? "    ... so no family and school would both rather have had each other."
            : F($"    worst margin        {TripDump.Minutes(reading.WorstMargin.Raw),10} min of walk"));
    }

    private static string F(FormattableString text) =>
        text.ToString(CultureInfo.InvariantCulture);
}
