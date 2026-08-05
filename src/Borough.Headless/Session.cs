using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// Runs a session and writes its State Hash trace.
/// </summary>
/// <remarks>
/// <b>The cadence is the caller's, which is why the Commit phase emits nothing on its own.</b>
/// <c>02 §1.1</c> says Phase 7 emits the State Hash <em>if due</em>, and <em>due</em> is a property
/// of the run rather than of the simulation: a bisection wants every Tick and a balance run wants
/// every thousandth.
/// </remarks>
internal static class Session
{
    /// <summary>The exit code for a replay refused before it started.</summary>
    private const int Refused = 3;

    public static int Run(Options options)
    {
        InputLog log = Load(options);

        ulong supplied = options.RulesetPath is null
            ? ContentHash.None
            : RulesetFile.HashOf(options.RulesetPath);

        RulesetCheck check = RulesetCheck.Against(
            log.RulesetHash, supplied, options.RulesetPath, options.ForceRuleset);

        if (!check.Allowed)
        {
            Console.Error.WriteLine(check.Refusal);
            return Refused;
        }

        Simulation simulation = Replay.Start(log);
        var hashes = new List<ulong>();

        Replay.Trace(simulation, log, new Ticks(options.Ticks), options.HashEvery, hashes);

        // 02 §10's end-of-run tier, on every run rather than behind a flag. It is O(world) once, so
        // it costs nothing against a run of any length, and a check that is off by default is a
        // check that is off. The trace is written first so a violation does not cost the numbers.
        Write(options, log, hashes, check.HashBroken);
        simulation.CheckEndOfRun();

        return 0;
    }

    /// <summary>
    /// The session to run: a recorded one, or a fresh one that never had a player.
    /// </summary>
    /// <remarks>
    /// <b>A fresh run is an empty log rather than a second code path.</b> <c>Replay.Start</c> builds
    /// the world a log describes, and <em>every difference between two cities is a difference in
    /// their logs</em> — a runner that could start a world some other way would be a way for state to
    /// arrive that the log does not account for, which is state no divergence can be attributed to.
    /// </remarks>
    private static InputLog Load(Options options)
    {
        if (options.LogPath is null)
        {
            return new InputLogBuilder(
                options.Seed,
                new WorldConfiguration(options.Citizens),
                ContentHash.None).Build();
        }

        using var reader = new StreamReader(options.LogPath);
        return InputLogCodec.Read(reader);
    }

    /// <summary>
    /// Writes the trace, to a file or to standard output.
    /// </summary>
    /// <remarks>
    /// <b>The header records what would have to match for two traces to be comparable</b>, which is
    /// the question anybody diffing them is really asking. A trace whose seed differs from the one
    /// beside it is not a divergence, and should not cost anybody an afternoon.
    /// </remarks>
    private static void Write(Options options, InputLog log, List<ulong> hashes, bool hashBroken)
    {
        HashTrace trace = HashTrace.Create()
            .With("seed", log.Seed)
            .With("citizens", (ulong)log.Configuration.Citizens)
            .With("ruleset", log.RulesetHash)
            .With("commands", (ulong)log.Count)
            .With("ticks", options.Ticks)
            .With("hash-every", (ulong)options.HashEvery);

        if (hashBroken)
        {
            // 05 §7 marks a save loaded across an unaccounted mismatch permanently hash-broken. The
            // same reasoning applies to a trace, for the same reason: without the mark, a divergence
            // report eventually arrives for numbers that were never comparable to anything.
            trace.With("hash-broken", 1);
        }

        for (int i = 0; i < hashes.Count; i++)
        {
            trace.Add((ulong)(i + 1) * (ulong)options.HashEvery, hashes[i]);
        }

        string[] preamble =
        [
            "Borough State Hash trace. Diff this against a trace from another run;",
            "the first differing sample names the window the change entered in.",
        ];

        if (options.OutPath is null)
        {
            trace.Write(Console.Out, preamble);
        }
        else
        {
            using var writer = new StreamWriter(options.OutPath);
            trace.Write(writer, preamble);
        }
    }
}
