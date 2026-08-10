using System.Globalization;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>What the runner was asked to do.</summary>
internal enum Mode
{
    /// <summary>Build a synthetic city and print what is in it. Slice 4's artefact.</summary>
    Report,

    /// <summary>Run a session and print its State Hash trace. Slice 5's.</summary>
    Run,

    /// <summary>
    /// Print a Map Layer's Cell grid before and after a source change. Slice 6's.
    /// </summary>
    /// <remarks>
    /// A third mode rather than a flag on the other two, for the reason the first two are separate:
    /// it needs a world with sources in it, and no session can put them there until Rules exist. A
    /// Layer dump taken at the end of a replay would print an empty grid.
    /// </remarks>
    Layer,

    /// <summary>
    /// Print the Lot grid by permission and occupancy, before and after a run. Slice 10's.
    /// </summary>
    /// <remarks>
    /// A fourth mode for <see cref="Layer"/>'s reason turned around. That one builds its own sources
    /// because no session can place any; this one runs a real session because a sweep is a thing that
    /// happens <em>over time</em>, and it refuses without a Ruleset because Zone Rules are content
    /// and there is nothing to invent in their place.
    /// </remarks>
    Zones,
}

/// <summary>
/// The command line, parsed.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two modes, because the runner does two things and one of them cannot see the other's city.</b>
/// The table report needs a populated world, and before slice 7 the only verb a session can apply is
/// Zone — so a report printed at the end of a replay would show four rows and three empty tables.
/// They are kept as separate modes rather than merged into one that degrades.
/// </para>
/// <para>
/// <b>Hand-rolled rather than a parsing library.</b> <c>adr/0018</c> prefers off-the-shelf
/// infrastructure and requires a written exception naming the property no library provides — this is
/// not that. It is below the threshold the ADR is aimed at: nine flags, no subcommands, no
/// completion, and no dependency worth carrying into the one project whose job is to prove it can
/// build with nothing installed. If the surface grows subcommands, take the library.
/// </para>
/// </remarks>
internal sealed class Options
{
    /// <summary>Slice 4's report population, kept as the no-argument behaviour.</summary>
    private const int DefaultPopulation = 10_000;

    private Options()
    {
    }

    public Mode Mode { get; private init; }

    /// <summary>A recorded session to replay, or null to run a fresh one.</summary>
    public string? LogPath { get; private init; }

    /// <summary>The Ruleset the session must have been recorded against, or null to skip the check.</summary>
    public string? RulesetPath { get; private init; }

    /// <summary>Where the trace goes. Null is standard output.</summary>
    public string? OutPath { get; private init; }

    /// <summary>The seed for a fresh session.</summary>
    public ulong Seed { get; private init; }

    /// <summary>Citizen sizing, for a fresh session or for the report.</summary>
    public int Citizens { get; private init; } = DefaultPopulation;

    /// <summary>How many Ticks to run.</summary>
    public ulong Ticks { get; private init; } = 1_024;

    /// <summary>The trace's sampling cadence.</summary>
    public int HashEvery { get; private init; } = 64;

    /// <summary>
    /// Run despite a Ruleset the session was not recorded against.
    /// </summary>
    /// <remarks>
    /// <b>The escape hatch is opt-in and the refusal is the default, which is the opposite polarity
    /// from the flag <c>plans/0008</c> sketched.</b> <c>05 §7</c> is explicit that
    /// <c>Borough.Headless</c> <em>is</em> replay mode and strict — so a <c>--strict</c> opt-in would
    /// have implied a lenient default the corpus denies. There is still a real use for running the
    /// mismatch deliberately, which is asking how far a Ruleset change moves the city; what that must
    /// not do is produce numbers that look comparable. So the trace it writes is stamped
    /// <c>hash-broken</c>, in the spirit of <c>05 §7</c>'s save mark.
    /// </remarks>
    public bool ForceRuleset { get; private init; }

    /// <summary>
    /// Sample every collection's size on the trace cadence and print the series at the end.
    /// </summary>
    /// <remarks>
    /// <b>Opt-in, because it is the one thing here that costs something the run did not ask for.</b>
    /// The readings are cheap but the ring is not free, and a run whose question is <em>did the hash
    /// change</em> has no use for it. It is also the flag that will grow an assertion when slice 7
    /// gives the world churn to reach a steady state with; today it reports and judges nothing.
    /// </remarks>
    public bool Census { get; private init; }

    /// <summary>
    /// Where to write the crash artifact, or null for a name derived from the Tick that panicked.
    /// </summary>
    /// <remarks>
    /// <b>There is no flag to turn the artifact off, and a default path rather than none.</b> The
    /// mechanism exists so that a panic in an unattended run becomes a file somebody can replay
    /// (<c>05 §8</c>); a crash that produced nothing because nobody passed a flag is the mechanism
    /// failing at the only moment it is needed. The flag names the destination, never whether.
    /// </remarks>
    public string? CrashPath { get; private init; }

    /// <summary>
    /// Prove every Tick that Phase 2 wrote nothing (<c>adr/0037</c>). On unless turned off.
    /// </summary>
    /// <remarks>
    /// <b>The switch exists because the guard is <c>O(world)</c> and the runs that need it turned off
    /// are the ones this runner is for.</b> <c>Simulation.VerifyDecideWritesNothing</c> folds every
    /// column of every table twice per Tick, and its own documentation says to turn it off for the
    /// 100,000-Tick test — which was not possible from the command line until spike <c>S0</c> tried to
    /// run one at the 1M target and found the guard was the whole Tick. The polarity is deliberate:
    /// the correctness check is the default and the fast run is the thing you ask for by name.
    /// </remarks>
    public bool DecideGuard { get; private init; } = true;

    /// <summary>Which Map Layer to dump, in <see cref="Mode.Layer"/>.</summary>
    public Layer Layer { get; private init; }

    /// <summary>Dump the Layer as CSV rather than as an ASCII field.</summary>
    /// <remarks>
    /// <b>Both, because they answer different questions.</b> The ASCII form is for judging a
    /// <em>shape</em> — a directional smear or a hard kernel edge is obvious in it and invisible in a
    /// column of numbers — and the CSV form is for anybody comparing values, because the ramp is a
    /// nine-step display quantisation and nothing should read it back.
    /// </remarks>
    public bool Csv { get; private init; }

    /// <summary>
    /// Parses the command line, or explains why it could not.
    /// </summary>
    public static bool TryParse(string[] arguments, out Options options, out string? complaint)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        options = new Options();
        complaint = null;

        string? log = null;
        string? ruleset = null;
        string? output = null;
        string? crash = null;
        ulong seed = 0;
        int citizens = DefaultPopulation;
        ulong ticks = 1_024;
        int hashEvery = 64;
        bool force = false;
        bool census = false;
        bool session = false;
        bool citizensGiven = false;
        bool csv = false;
        bool decideGuard = true;
        bool zones = false;
        Layer? dump = null;

        for (int i = 0; i < arguments.Length; i++)
        {
            string flag = arguments[i];

            switch (flag)
            {
                case "--force-ruleset":
                    force = true;
                    continue;

                // A census is a property of a run, so asking for one is asking for a run — the same
                // reasoning that makes --ticks and --seed imply a session rather than the report.
                case "--census":
                    census = true;
                    session = true;
                    continue;

                case "--csv":
                    csv = true;
                    continue;

                // Deliberately not a session flag, unlike --census. A Zone dump *is* a run, and the
                // mode it selects already implies one; setting `session` too would make it collide
                // with --layer's refusal below for a reason the operator did not cause.
                case "--zones":
                    zones = true;
                    continue;

                // A run, for the same reason --census is: the guard is a property of stepping a world,
                // and the report never steps one.
                case "--no-decide-guard":
                    decideGuard = false;
                    session = true;
                    continue;

                case "--help" or "-h":
                    complaint = string.Empty;
                    return false;
            }

            if (i + 1 >= arguments.Length)
            {
                complaint = $"{flag} needs a value.";
                return false;
            }

            string value = arguments[++i];

            switch (flag)
            {
                case "--log":
                    log = value;
                    session = true;
                    break;

                case "--ruleset":
                    ruleset = value;
                    break;

                case "--out":
                    output = value;
                    break;

                case "--crash":
                    crash = value;
                    break;

                case "--seed":
                    if (!TryNumber(value, out seed))
                    {
                        complaint = $"--seed {value} is not a number.";
                        return false;
                    }

                    session = true;
                    break;

                case "--citizens":
                    if (!TryCount(value, out citizens))
                    {
                        complaint = $"--citizens {value} is not a positive count.";
                        return false;
                    }

                    citizensGiven = true;
                    break;

                case "--ticks":
                    if (!TryNumber(value, out ticks) || ticks == 0)
                    {
                        complaint = $"--ticks {value} is not a positive count.";
                        return false;
                    }

                    session = true;
                    break;

                case "--layer":
                    if (!LayerDump.TryParse(value, out Layer named))
                    {
                        complaint = $"--layer {value} is not a Map Layer. "
                                  + "The Layers are pollution, land-value and sealing. Noise is not "
                                  + "one: it is a line source and a point-of-use query (adr/0034).";
                        return false;
                    }

                    dump = named;
                    break;

                case "--hash-every":
                    if (!TryCount(value, out hashEvery))
                    {
                        complaint = $"--hash-every {value} is not a positive count.";
                        return false;
                    }

                    break;

                default:
                    complaint = $"{flag} is not an option this runner knows.";
                    return false;
            }
        }

        if (log is not null && citizensGiven)
        {
            complaint = "--citizens and --log disagree: a log carries its own configuration, "
                      + "and a replay that took its world size from the command line would be "
                      + "reproducing a different session.";
            return false;
        }

        if (dump is not null && session)
        {
            complaint = "--layer and the session flags disagree: a Layer dump builds its own world "
                      + "with sources in it, because no session can place a source until Rules exist.";
            return false;
        }

        if (zones && dump is not null)
        {
            complaint = "--zones and --layer are two pictures of two different things, and each "
                      + "builds its own world. Ask for one.";
            return false;
        }

        // A sweep is a Ruleset's behaviour, so a Zone dump with no Rules would print the same grid
        // twice and read as a broken mechanism rather than an absent one. Refuse instead of degrade.
        if (zones && ruleset is null)
        {
            complaint = "--zones needs --ruleset PATH. A Zone Rule is content, not a default: "
                      + "without one there is no sweep to show, and an unchanging grid would look "
                      + "like a defect rather than like a Ruleset that declares no [[zone_rule]].";
            return false;
        }

        if (zones && log is not null)
        {
            complaint = "--zones and --log disagree: a Zone dump runs its own populated session, and "
                      + "a replay's commands would be applied to a world it did not record.";
            return false;
        }

        options = new Options
        {
            Mode = zones ? Mode.Zones
                 : dump is not null ? Mode.Layer
                 : session ? Mode.Run
                 : Mode.Report,
            Layer = dump ?? default,
            Csv = csv,
            LogPath = log,
            RulesetPath = ruleset,
            OutPath = output,
            Seed = seed,
            Citizens = citizens,
            Ticks = ticks,
            HashEvery = hashEvery,
            ForceRuleset = force,
            Census = census,
            CrashPath = crash,
            DecideGuard = decideGuard,
        };

        return true;
    }

    /// <summary>The usage text. Every string a human reads is the shell's (<c>adr/0002</c>).</summary>
    public static string Usage =>
        """
        Borough.Headless -- run a session and print its State Hash trace.

          (no options)          the table report, at 10,000 Citizens

          --log PATH            replay a session recorded in a .borough file
          --seed N              run a fresh session with this seed and no commands
          --citizens N          Citizen sizing, for a fresh session or the report
          --ticks N             how many Ticks to run
          --hash-every N        trace sampling cadence, in Ticks
          --ruleset PATH        the Rules to run under. Loaded and put in force, and
                                the session must name it. Without one, nothing has
                                any Rules and the run simulates an inert city
          --force-ruleset       run against a Ruleset the session does not name, and
                                stamp the trace hash-broken
          --out PATH            write the trace to a file instead of standard output
          --census              sample every collection's size on the trace cadence and
                                print first/last/low/high per collection at the end
          --crash PATH          where to write the crash artifact if the run panics.
                                One is always written; this only names where
          --no-decide-guard     stop proving every Tick that Phase 2 wrote nothing.
                                The proof is O(world) per Tick; turn it off for a
                                long run at scale and leave it on everywhere else
          --layer NAME          dump a Map Layer's Cell grid before and after a source
                                change, with the halo that was recomputed. NAME is
                                pollution, land-value or sealing
          --zones               dump the Lot grid by permission and occupancy, before and
                                after --ticks Ticks of sweeping, with what the sweep did.
                                Needs --ruleset, because a sweep is a Ruleset's behaviour
          --csv                 dump the Layer or the Lot grid as CSV rather than as an
                                ASCII field

        A replay whose Ruleset does not match refuses to run rather than diverging
        silently: a different Ruleset is a different simulation, and the divergence
        would be arithmetic rather than a bug. 05 section 7.
        """;

    private static bool TryNumber(string value, out ulong number) =>
        ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);

    private static bool TryCount(string value, out int count) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) && count > 0;
}
