using System.Globalization;

namespace Borough.Headless;

/// <summary>What the runner was asked to do.</summary>
internal enum Mode
{
    /// <summary>Build a synthetic city and print what is in it. Slice 4's artefact.</summary>
    Report,

    /// <summary>Run a session and print its State Hash trace. Slice 5's.</summary>
    Run,
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

        options = new Options
        {
            Mode = session ? Mode.Run : Mode.Report,
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
          --ruleset PATH        the Ruleset the session must have been recorded against
          --force-ruleset       run against a Ruleset the session does not name, and
                                stamp the trace hash-broken
          --out PATH            write the trace to a file instead of standard output
          --census              sample every collection's size on the trace cadence and
                                print first/last/low/high per collection at the end
          --crash PATH          where to write the crash artifact if the run panics.
                                One is always written; this only names where

        A replay whose Ruleset does not match refuses to run rather than diverging
        silently: a different Ruleset is a different simulation, and the divergence
        would be arithmetic rather than a bug. 05 section 7.
        """;

    private static bool TryNumber(string value, out ulong number) =>
        ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);

    private static bool TryCount(string value, out int count) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) && count > 0;
}
