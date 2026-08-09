using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
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

    /// <summary>
    /// The exit code for a run that started and then panicked.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="Refused"/> because the two want opposite responses: a refusal means
    /// the invocation was wrong and nothing ran, and a panic means the simulation is wrong and there
    /// is now a file about it.
    /// </remarks>
    private const int Panicked = 4;

    public static int Run(Options options)
    {
        ulong supplied = options.RulesetPath is null
            ? ContentHash.None
            : RulesetFile.HashOf(options.RulesetPath);

        InputLog log = Load(options, supplied);

        RulesetCheck check = RulesetCheck.Against(
            log.RulesetHash, supplied, options.RulesetPath, options.ForceRuleset);

        if (!check.Allowed)
        {
            Console.Error.WriteLine(check.Refusal);
            return Refused;
        }

        // Parsed after the hash check and refused independently of it. The order is the operator's
        // rather than the machine's: given both a wrong Ruleset and a malformed one, "you supplied a
        // different Ruleset" is the more actionable sentence, and it is the one that explains why the
        // other refusals look unfamiliar. The parse refusal is unconditional, though —
        // --force-ruleset waives a mismatch and cannot waive Rules nobody can read.
        if (!TryRules(options.RulesetPath, out Ruleset rules))
        {
            return Refused;
        }

        Simulation simulation = Replay.Start(log, rules);
        simulation.VerifyDecideWritesNothing = options.DecideGuard;

        var hashes = new List<ulong>();
        Census? census = options.Census ? new Census(simulation.World) : null;

        try
        {
            Replay.Trace(simulation, log, new Ticks(options.Ticks), options.HashEvery, hashes, census);

            // 02 §10's end-of-run tier, on every run rather than behind a flag. It is O(world) once,
            // so it costs nothing against a run of any length, and a check that is off by default is
            // a check that is off. The trace is written first so a violation does not cost the
            // numbers.
            Write(options, log, hashes, check.HashBroken);

            if (census is not null)
            {
                CensusReport.Print(Console.Out, simulation.World, census, options.Ticks);
            }

            simulation.CheckEndOfRun();
        }
        catch (Exception fault) when (fault is not OutOfMemoryException)
        {
            // 05 §8: catch at the Tick boundary rather than unwinding the process. Broad on purpose —
            // this is the handler whose whole job is to turn any panic into a reproduction, and one
            // that only recognised the failures already thought of would be missing exactly the ones
            // worth catching. Out of memory is the exception: writing a file needs memory, and
            // failing there would bury the original.
            return Panic(options, log, simulation.Tick, check.InForce, fault);
        }

        return 0;
    }

    /// <summary>
    /// Writes the reproduction and says how to run it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>Simulation.Tick</c> is the Tick that panicked, and that is not an accident.</b>
    /// <c>Step</c> advances the counter after its phases rather than before, so a phase that throws
    /// leaves the Tick naming the failure instead of the one after it. An artifact off by one Tick
    /// would send its reader to a Tick where nothing is yet wrong.
    /// </para>
    /// <para>
    /// <b>The trace is deliberately not written.</b> A run that panicked has a partial one, and a
    /// partial trace is indistinguishable from a complete one once it is a file — it would be diffed
    /// against a full run and the missing tail read as a divergence. Nothing is lost by omitting it:
    /// replaying the artifact regenerates it up to the panic, which is the point of the artifact.
    /// </para>
    /// </remarks>
    private static int Panic(
        Options options, InputLog log, Ticks tick, ulong rulesetHash, Exception fault)
    {
        string path = options.CrashPath
            ?? string.Create(CultureInfo.InvariantCulture, $"crash-{tick.Raw}{CrashArtifact.Extension}");

        CrashArtifact artifact = CrashArtifact.Of(log, tick, rulesetHash, fault);

        using (var writer = new StreamWriter(path))
        {
            CrashArtifact.Write(writer, artifact);
        }

        // The stack belongs on the console of the run that crashed. What the file carries instead is
        // the means of producing a live one under a debugger, as many times as it takes.
        Console.Error.WriteLine(fault);
        Console.Error.WriteLine();
        // Two commands rather than one, because they are the two different things somebody wants and
        // the difference between them is one Tick. 05 §8's whole claim is the first: you arrive at
        // the Tick *before* the failure with the world intact and step into it under a debugger.
        const string Runner = "dotnet run --project src/Borough.Headless --";

        Console.Error.WriteLine(F($"Tick {tick.Raw} panicked. Wrote {path}"));
        Console.Error.WriteLine();
        Console.Error.WriteLine(F(
            $"  stop just before it:  {Runner} --log {path} --ticks {tick.Raw}"));
        Console.Error.WriteLine(F(
            $"  panic again:          {Runner} --log {path} --ticks {tick.Raw + 1}"));

        return Panicked;
    }

    /// <summary>Formats with the invariant culture, which adr/0003 requires of every number here.</summary>
    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The session to run: a recorded one, or a fresh one that never had a player.
    /// </summary>
    /// <remarks>
    /// <b>A fresh run is an empty log rather than a second code path.</b> <c>Replay.Start</c> builds
    /// the world a log describes, and <em>every difference between two cities is a difference in
    /// their logs</em> — a runner that could start a world some other way would be a way for state to
    /// arrive that the log does not account for, which is state no divergence can be attributed to.
    /// </remarks>
    /// <param name="supplied">
    /// The content hash of the Ruleset given on the command line, or <c>ContentHash.None</c>.
    /// </param>
    /// <remarks>
    /// <b>A fresh session is recorded against the Ruleset it was handed, and getting this wrong makes
    /// the flag unusable.</b> The builder previously stamped <c>ContentHash.None</c> unconditionally,
    /// which was right while nothing could be supplied — the moment <c>--ruleset</c> loads content, a
    /// fresh run would name no Ruleset, be handed one, and <see cref="RulesetCheck"/> would correctly
    /// refuse the session against its own Rules. A new session is not a mismatch; it is the recording.
    /// </remarks>
    /// <summary>
    /// Parses the Ruleset the run was given, or explains to the operator why it could not.
    /// </summary>
    /// <param name="path">The path from <c>--ruleset</c>, or null if none was given.</param>
    /// <param name="rules">The Rules to run under. <see cref="Ruleset.Empty"/> when no path was given.</param>
    /// <returns>Whether the run may proceed.</returns>
    /// <remarks>
    /// <para>
    /// <b>No path means no Rules, and there is deliberately no default Ruleset.</b> A run given none
    /// is genuinely running against nothing, which is what every figure this project has recorded so
    /// far was taken against. A default would silently change what every existing invocation
    /// measures, S0a's included, and the first symptom would be numbers that no longer compare.
    /// </para>
    /// <para>
    /// <b>Every refusal reaches the operator, not the first.</b> <c>adr/0048</c>'s whole argument for
    /// validating at the parse is that a designer gets a file, a line and a rule name; printing one
    /// refusal and stopping would turn a single pass over a broken file into as many runs as it has
    /// mistakes.
    /// </para>
    /// </remarks>
    internal static bool TryRules(string? path, out Ruleset rules)
    {
        if (path is null)
        {
            rules = Ruleset.Empty;
            return true;
        }

        RulesetLoadResult result = RulesetLoader.Load(path);

        if (result.Ruleset is null)
        {
            Console.Error.WriteLine(result.Describe());
            Console.Error.WriteLine(
                $"{result.Refusals.Count} refusal(s). The Ruleset was not loaded and nothing ran.");

            rules = Ruleset.Empty;
            return false;
        }

        rules = result.Ruleset;
        return true;
    }

    internal static InputLog Load(Options options, ulong supplied)
    {
        if (options.LogPath is null)
        {
            // A fresh session is populated, and it was not before. A run whose world had capacity for
            // a million Citizens and no rows in it reported a State Hash that never moved and a census
            // of zeroes, which read as a stable city and was an empty one — so every Tick figure this
            // project holds, slice 6's 100,000-Tick acceptance run included, was taken over nothing.
            // The command goes in the log rather than into the world, so the trace stays reproducible
            // from the file alone.
            InputLogBuilder builder = new(
                options.Seed, new WorldConfiguration(options.Citizens), supplied);

            builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

            return builder.Build();
        }

        string text = File.ReadAllText(options.LogPath);

        // A crash artifact is handed back here, in the place a log goes, because replaying it is the
        // only thing anybody wants to do with one. The file says which it is; requiring a flag would
        // mean the person reproducing a crash has to already know something the file could tell them.
        return CrashArtifact.IsCrashArtifact(text)
            ? CrashArtifact.FromText(text).Log
            : InputLogCodec.FromText(text);
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

    /// <summary>
    /// <c>--layer</c>: print a Map Layer's Cell grid before and after a source change.
    /// </summary>
    /// <remarks>
    /// <b>The first artefact from this runner that is looked at rather than diffed</b>
    /// (<c>plans/0009</c> acceptance). Every other output here is a number whose job is to be compared
    /// against another number; a field's defects are shaped, so this one is judged by eye.
    /// </remarks>
    internal static int DumpLayer(Options options)
    {
        if (options.OutPath is null)
        {
            LayerDump.Run(Console.Out, options.Layer, options.Csv);
        }
        else
        {
            using var writer = new StreamWriter(options.OutPath);
            LayerDump.Run(writer, options.Layer, options.Csv);
        }

        return 0;
    }
}
