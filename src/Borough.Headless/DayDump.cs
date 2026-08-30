namespace Borough.Headless;

using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;

/// <summary>
/// One Citizen, one Day, every Tick. <c>plans/0045</c>'s queue item 3.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every other dump in this runner aggregates.</b> A census counts collections, a commute dump
/// bins journeys by block, a market dump prints a price. All of them answer <em>what is the city
/// doing</em>, and none of them answers <em>what is it like to be in it</em> — which is the first
/// pillar, <c>a city made of people you can actually meet</c>, and the one thing nothing in the tree
/// had ever printed.
/// </para>
/// <para>
/// <b>It costs almost nothing because <c>Evidence.OfCitizen</c> already existed.</b> Milestone 6
/// built <c>02 §9</c>'s Citizen answer and only ever called it in a loop over everybody. This calls
/// it on one person every Tick and prints the transitions.
/// </para>
/// <para>
/// 🔴 ⚠ <b>WHAT IT PRINTS IS MEANT TO BE THIN, AND THE THINNESS IS THE FINDING.</b> There are no
/// Needs (<c>adr/0103</c> closes the set at four and nothing builds it), no shopping occasion, no
/// ageing — <c>CitizenTable.Age</c> is saved, hashed and written by nothing — and one Trip generator.
/// So a Day is two journeys and a great deal of standing still. ***Do not read a sparse timeline as
/// a broken dump***; read the footer, which names what is absent and why.
/// </para>
/// </remarks>
internal static class DayDump
{
    /// <summary>The subject's state at one Tick, reduced to what a transition would change.</summary>
    private readonly record struct Reading(
        CitizenActivity Activity, bool Travelling, TripFate LastFate, long Balance,
        int Sustenance);

    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        if (!rules.Jobs.Runs)
        {
            output.WriteLine(
                "# This Ruleset states no [jobs] table, so nobody is employed and the only Trip "
                + "generator in the build never fires. A Day here is genuinely empty. Try "
                + "rulesets/provisioned.toml.");
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        // Settle first. Employment is assigned on a cadence and takes time (adr/0081), so a trace
        // started at Tick 0 would follow somebody who has no job yet and no journey to make.
        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        int subject = Subject(world);

        if (subject < 0)
        {
            output.WriteLine(
                $"# Nobody was employed and housed after {options.Ticks} Ticks, so there is no Day "
                + "to follow. Run more Ticks, or raise --citizens.");
            return 1;
        }

        Preamble(output, options, world, subject);
        Timeline(output, world, simulation, subject);

        return 0;
    }

    /// <summary>
    /// The lowest-slot Citizen who has both a job and somewhere to live.
    /// </summary>
    /// <remarks>
    /// <b>Lowest slot rather than a draw, because a dump must be reproducible</b> — two runs of one
    /// seed follow the same person, which is what makes a before-and-after readable. Both conditions
    /// are needed: <c>CommuteEngine.Travel</c> refuses a journey for want of either, so somebody with
    /// a job and no dwelling would produce a Day with nothing in it for a reason that is not about
    /// them.
    /// </remarks>
    private static int Subject(World world)
    {
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            CitizenEvidence who = Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));

            if (who.Home.IsNone
                || !world.Businesses.Rows.TryResolve(who.Workplace, out int employer))
            {
                continue;
            }

            // The employer's premises, and it is a THIRD condition rather than part of the second.
            // A Business is not a place -- adr/0141 -- and adr/0146 lets a founder hold one before
            // placement gives it any. CommuteEngine.Travel refuses the journey on exactly this hop,
            // so a subject picked without it is somebody who cannot travel for a reason that is not
            // about them, and the trace reads as a broken dump. Found by the first run of this file.
            if (world.Buildings.Rows.TryResolve(world.Businesses.Building[employer], out _))
            {
                return slot;
            }
        }

        return -1;
    }

    private static void Preamble(TextWriter output, Options options, World world, int subject)
    {
        CitizenEvidence who = Evidence.OfCitizen(world, world.Citizens.Rows.At(subject));

        output.WriteLine($"# A Day in the life of Citizen {subject}");
        output.WriteLine(
            $"# {options.RulesetPath}, {options.Citizens} Citizens, traced from Tick "
            + $"{options.Ticks} for one Day ({Ticks.PerDay} Ticks).");
        output.WriteLine("#");
        output.WriteLine($"#   lives in     Building {SlotOf(world, who)}");
        output.WriteLine($"#   works at     Business {BusinessSlot(world, who)}");
        output.WriteLine(
            $"#   commute      {Minutes(who.PlannedCommute.Raw)} min, as planned when the job was "
            + "taken");
        output.WriteLine($"#   household    {Balance(who)}");
        output.WriteLine($"#   sustenance   {Need(who)}");
        output.WriteLine(
            $"#   job searches refused for want of a road: {who.ReachFailures}");
        output.WriteLine("#");
        Meanwhile(output, world);
        output.WriteLine("#");
        output.WriteLine("  time    tick   what happened");
        output.WriteLine("  -----  ------  " + new string('-', 46));
    }

    /// <summary>
    /// What everybody else is doing at the instant the trace starts.
    /// </summary>
    /// <remarks>
    /// <b>Context, and it is the line that says whether a quiet trace is about this person or about
    /// the city.</b> The first run of this file followed somebody who made no journey in a whole Day
    /// and there was no way to tell, from the trace alone, whether that was one Citizen's Shift or a
    /// city in which nobody commutes at all. ***A single-subject instrument needs a denominator.***
    /// </remarks>
    private static void Meanwhile(TextWriter output, World world)
    {
        var counts = new int[4];
        int employed = 0;
        int live = 0;
        int hungry = 0;
        int pinned = 0;
        int deepest = 0;
        long total = 0;
        int floor = world.Rules.Needs.Floor;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            live++;
            counts[world.Citizens.Activity[slot] & 3]++;

            if (!world.Citizens.Workplace[slot].IsNone)
            {
                employed++;
            }
        }

        // ⚠ HOUSEHOLDS rather than Citizens, and it is the one count on this line with a different
        // denominator. A Need belongs to the Household, so walking the Citizens would count a
        // Household of three three times and report a hunger that is really an occupancy.
        int households = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            households++;

            int need = world.Households.Sustenance[slot];

            deepest = need < deepest ? need : deepest;
            total += need;
            hungry += need < 0 ? 1 : 0;
            pinned += need == floor ? 1 : 0;
        }

        output.WriteLine(
            $"#   meanwhile    {live:N0} live, {employed:N0} employed — "
            + $"{counts[(int)CitizenActivity.AtHome]:N0} at home, "
            + $"{counts[(int)CitizenActivity.TravellingToWork]:N0} walking to work, "
            + $"{counts[(int)CitizenActivity.AtWork]:N0} at work, "
            + $"{counts[(int)CitizenActivity.TravellingHome]:N0} walking home");

        if (!world.Rules.Needs.Runs)
        {
            return;
        }

        // The count at the floor is the number to read: a Need is bounded below, so its mean is
        // dragged by the bound rather than by the city.
        output.WriteLine(
            $"#   hunger       {hungry:N0} of {households:N0} Households in deficit, {pinned:N0} "
            + $"pinned at the floor ({floor}) — deepest {deepest}, mean "
            + $"{(households > 0 ? total / households : 0)}");
    }

    /// <summary>
    /// Steps one Day and prints a line every time the subject's reading changes.
    /// </summary>
    /// <remarks>
    /// <b>Transitions rather than samples.</b> 2,048 rows of <em>at home</em> is not a Day, it is a
    /// log; what a reader wants is the handful of Ticks on which anything was different. The count of
    /// suppressed rows is the footer's first number, because <em>how much of the Day was nothing</em>
    /// is the honest headline.
    /// </remarks>
    private static void Timeline(
        TextWriter output, World world, Simulation simulation, int subject)
    {
        Reading previous = Read(world, subject);
        var spent = new long[4];
        int events = 0;

        Line(output, 0, previous.Activity, "the Day begins", previous);

        for (int tick = 1; tick <= Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            if (!world.Citizens.Rows.IsLive(subject))
            {
                output.WriteLine($"  {Clock(tick),5}  {tick,6}  this Citizen no longer exists");
                break;
            }

            Reading now = Read(world, subject);
            spent[(int)previous.Activity]++;

            if (now != previous)
            {
                Line(output, tick, now.Activity, Describe(previous, now), now);
                events++;
            }

            previous = now;
        }

        output.WriteLine();
        output.WriteLine("# and the city, one Day on:");
        Meanwhile(output, world);

        Footer(output, spent, events);
    }

    private static Reading Read(World world, int subject)
    {
        CitizenEvidence who = Evidence.OfCitizen(world, world.Citizens.Rows.At(subject));

        return new Reading(
            (CitizenActivity)who.Activity,
            who.Trip is not null,
            who.LastTrip?.Fate ?? TripFate.InFlight,
            who.HouseholdBalance is { } money ? money.Raw : long.MinValue,
            who.Sustenance ?? 0);
    }

    private static string Describe(Reading was, Reading now)
    {
        if (was.Activity != now.Activity)
        {
            return now.Activity switch
            {
                CitizenActivity.TravellingToWork => "set off for work",
                CitizenActivity.AtWork => "arrived at work",
                CitizenActivity.TravellingHome => "set off home",
                CitizenActivity.AtHome when was.Activity == CitizenActivity.TravellingHome =>
                    "got home",
                CitizenActivity.AtHome => $"turned back — {Fate(now.LastFate)}",
                _ => "changed activity",
            };
        }

        if (was.Balance != now.Balance)
        {
            long moved = now.Balance - was.Balance;

            return moved < 0
                ? $"the household spent {-moved:N0}"
                : $"the household received {moved:N0}";
        }

        if (was.Sustenance != now.Sustenance)
        {
            // Only a world stating [needs] ever reaches this, and only while the Household is short:
            // 0 is the ideal and the accumulator clamps there, so a fed city produces no line at all
            // and a starving one stops producing them once it is pinned at the floor.
            return now.Sustenance < was.Sustenance
                ? $"went hungrier — sustenance {now.Sustenance}"
                : $"ate — sustenance {now.Sustenance}";
        }

        return was.Travelling != now.Travelling ? "the journey resolved" : "something changed";
    }

    private static void Footer(TextWriter output, long[] spent, int events)
    {
        long total = spent.Sum();

        output.WriteLine();
        output.WriteLine($"# {events} thing(s) happened in {total:N0} Ticks.");
        output.WriteLine(
            $"#   at home          {spent[(int)CitizenActivity.AtHome],6:N0} Ticks");
        output.WriteLine(
            $"#   walking to work  {spent[(int)CitizenActivity.TravellingToWork],6:N0} Ticks");
        output.WriteLine(
            $"#   at work          {spent[(int)CitizenActivity.AtWork],6:N0} Ticks");
        output.WriteLine(
            $"#   walking home     {spent[(int)CitizenActivity.TravellingHome],6:N0} Ticks");
        output.WriteLine("#");
        output.WriteLine(
            "# What this Citizen did NOT do, because the mechanism does not exist rather than "
            + "because");
        output.WriteLine(
            "# they chose not to (adr/0070 — an unbuilt mechanism is not a design constraint):");
        output.WriteLine("#");
        output.WriteLine("#   feel Education or Health      adr/0103 leaves their degradation UNDESIGNED");
        output.WriteLine("#   buy anything                  no shopping occasion; the commute is the only generator");
        output.WriteLine("#   visit or know anybody         no social mechanism of any kind");
        output.WriteLine("#   get one Day older             CitizenTable.Age is saved, hashed, and written by nothing");
        output.WriteLine("#   earn a wage                   milestone 15; money only ever leaves a Household");
        output.WriteLine("#   make a single decision        every branch above was taken by a cadence");
        output.WriteLine("#");
        output.WriteLine("# This is the whole of a life in the current build.");
    }

    private static void Line(
        TextWriter output, int tick, CitizenActivity activity, string what, Reading reading)
    {
        string money = reading.Balance == long.MinValue ? string.Empty : $"   [{reading.Balance:N0}]";

        output.WriteLine($"  {Clock(tick),5}  {tick,6}  {what}{money}");
        _ = activity;
    }

    /// <summary>Tick-of-Day as a wall clock. A Day is 2,048 Ticks and 1,440 minutes.</summary>
    private static string Clock(int tick)
    {
        int minutes = (int)((long)(tick % Ticks.PerDay) * Ticks.MinutesPerDay / Ticks.PerDay);

        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    private static int Minutes(ulong ticks) =>
        (int)((long)ticks * Ticks.MinutesPerDay / Ticks.PerDay);

    private static string Fate(TripFate fate) => fate switch
    {
        TripFate.NoRouteFound => "no route exists for their mode",
        TripFate.ExceededCommuteBudget => "the journey cost more than the Commute Budget",
        TripFate.Completed => "the journey completed",
        _ => "the journey did not finish",
    };

    /// <summary>The subject's Sustenance, or why the question does not apply.</summary>
    /// <remarks>
    /// ⚠ <b>Absent rather than zero where the Ruleset states no <c>[needs]</c></b>: 0 is the ideal,
    /// so printing it about a city nobody feeds would say the opposite of the truth.
    /// </remarks>
    private static string Need(CitizenEvidence who) =>
        who.Sustenance is { } need
            ? $"{need}  (0 is ideal, negative is deficit)"
            : "this Ruleset states no [needs], so the question does not apply";

    private static string Balance(CitizenEvidence who) =>
        who.HouseholdBalance is { } money
            ? $"{money.Raw:N0}"
            : "no money in this world";

    private static string SlotOf(World world, CitizenEvidence who) =>
        world.Buildings.Rows.TryResolve(who.Home, out int slot)
            ? slot.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "nowhere";

    private static string BusinessSlot(World world, CitizenEvidence who) =>
        world.Businesses.Rows.TryResolve(who.Workplace, out int slot)
            ? slot.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "nowhere";
}
