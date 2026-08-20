using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Persistence;
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
        ArgumentNullException.ThrowIfNull(options);

        if (options.LoadPath is not null)
        {
            return Resume(options);
        }

        Supplied[] supplied = [.. options.RulesetPaths.Select(
            path => new Supplied(path, RulesetFile.HashOf(path)))];

        InputLog log = Load(options, supplied);

        RulesetCheck check = RulesetCheck.Against(log, supplied, options.ForceRuleset);

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
        if (!TryCatalogue(supplied, log.RulesetHash, out RulesetCatalogue rulesets))
        {
            return Refused;
        }

        Simulation simulation = Replay.Start(log, rulesets);
        simulation.VerifyDecideWritesNothing = options.DecideGuard;

        var hashes = new List<ulong>();
        Census? census = options.Census ? new Census(simulation.World) : null;

        try
        {
            if (options.SavePath is null)
            {
                Replay.Trace(
                    simulation, log, new Ticks(options.Ticks), options.HashEvery, hashes, census);
            }
            else
            {
                // Split so the save lands on the Tick the operator asked for rather than one past
                // it. `SaveAtEndOfTick` arms the NEXT Step, so the last Tick of the run is the one
                // that carries it -- and splitting the loop moves no sample, because Replay.Trace
                // decides what to sample from the simulation's own Tick and not from a loop counter.
                using var stream = File.Create(options.SavePath);

                Replay.Trace(
                    simulation, log, new Ticks(options.Ticks - 1), options.HashEvery, hashes, census);

                simulation.SaveAtEndOfTick(new SaveSink(stream));

                Replay.Trace(simulation, log, new Ticks(1), options.HashEvery, hashes, census);
            }

            // 02 §10's end-of-run tier, on every run rather than behind a flag. It is O(world) once,
            // so it costs nothing against a run of any length, and a check that is off by default is
            // a check that is off. The trace is written first so a violation does not cost the
            // numbers.
            Write(options, log, hashes, check.HashBroken);

            if (census is not null)
            {
                CensusReport.Print(Console.Out, simulation.World, census, options.Ticks);
            }

            // After the summary rather than instead of it. The four numbers are what most runs are
            // read for, and the series is what a surprising one is read with -- so the surprise and
            // the detail arrive in that order, and a reader who wanted neither has already stopped.
            if (census is not null && options.Series)
            {
                SeriesReport.Print(Console.Out, simulation.World, census, options.Ticks);
            }

            ReportReloads(simulation);

            // After the trace and the census, because it is the only part of a run that steps the
            // world further than the operator asked -- so it must not be able to change the numbers
            // above it, and a reader who wanted only those has already stopped.
            if (options.SavePath is not null)
            {
                RoundTrip(options, log, simulation, rulesets);
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
            // The Ruleset in force at the *panic*, not the one the run opened on. Once a session can
            // reload, those differ, and an artifact stamped with the opening Ruleset would send its
            // reader to a file that had not been in force for a thousand Ticks. Zero means the run
            // died before its first Tick established one, and then the opening hash is all there is.
            return Panic(
                options,
                log,
                simulation.Tick,
                simulation.RulesetInForce == ContentHash.None
                    ? check.InForce
                    : simulation.RulesetInForce,
                fault);
        }

        return 0;
    }

    /// <summary>
    /// Reloads the save the run just wrote and runs both cities on, side by side. <b>The picture.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A file on disk is not evidence that a save works, and this is what makes it one.</b>
    /// <c>05 §7</c>'s <em>replay from save</em> is a claim that a resumed city <em>is</em> the same
    /// city; the only thing that can show it is the resumed city being run and not agreeing by
    /// accident. So both worlds step the same further stretch against the same log, and the traces
    /// are printed together with the Tick each sample was taken at.
    /// </para>
    /// <para>
    /// <b>The further stretch is <c>--ticks</c> again rather than a number chosen here.</b> A fixed
    /// tail would be a hash-bearing quantity nobody ratified in the one place it is cheapest not to
    /// have one, and the operator already told the runner how long is interesting.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a demonstration and not the test.</b> <c>FactorioTests</c> is the assertion — seven
    /// cases, two Rulesets, compared at every Tick — and this compares on the trace cadence, over one
    /// invocation, so a divergence between samples would be reported at the sample after it. What it
    /// is for is a person watching a round trip happen, which no test can be.
    /// </para>
    /// </remarks>
    private static void RoundTrip(
        Options options, InputLog log, Simulation control, RulesetCatalogue rulesets)
    {
        World reloaded;
        SaveHeader header;

        using (var stream = File.OpenRead(options.SavePath!))
        {
            reloaded = SaveFile.Read(new SaveSource(stream), rulesets.Opening, out header);
        }

        var resumed = new Simulation(reloaded, header.Key);
        resumed.VerifyDecideWritesNothing = options.DecideGuard;

        var saved = new FileInfo(options.SavePath!);

        Console.Out.WriteLine();
        Console.Out.WriteLine(F(
            $"Saved {saved.Length:N0} bytes at Tick {control.Tick.Raw} to {options.SavePath}"));
        Console.Out.WriteLine(F(
            $"Reloaded at Tick {resumed.Tick.Raw}, hash {reloaded.HashState():X16} against {control.World.HashState():X16}"));
        Console.Out.WriteLine();

        List<ulong> unbroken = [];
        List<ulong> roundTripped = [];

        Replay.Trace(control, log, new Ticks(options.Ticks), options.HashEvery, unbroken);
        Replay.Trace(resumed, log, new Ticks(options.Ticks), options.HashEvery, roundTripped);

        Console.Out.WriteLine("        tick  unbroken          round-tripped");

        int disagreements = 0;

        for (int i = 0; i < unbroken.Count; i++)
        {
            ulong tick = control.Tick.Raw - (ulong)((unbroken.Count - 1 - i) * options.HashEvery);
            bool agrees = unbroken[i] == roundTripped[i];

            if (!agrees)
            {
                disagreements++;
            }

            Console.Out.WriteLine(F(
                $"  {tick,10}  {unbroken[i]:X16}  {roundTripped[i]:X16}  {(agrees ? "" : "DIVERGED")}"));
        }

        Console.Out.WriteLine();
        Console.Out.WriteLine(
            disagreements == 0
                ? F($"The round trip agrees, over {unbroken.Count} samples and {options.Ticks} Ticks.")
                : F($"⚠ {disagreements} of {unbroken.Count} samples DIVERGED."));
    }

    /// <summary>
    /// Resumes a save and runs on. <c>05 §7</c>'s <em>replay from save</em>, as a thing to type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A separate invocation rather than a phase of one, because the file outliving the process is
    /// the only property a save has that <c>WorldSnapshot</c> does not.</b> A round trip inside one
    /// run exercises the format; this exercises the format <em>and</em> the claim that a city can be
    /// put down and picked up.
    /// </para>
    /// <para>
    /// <b>It steps with no commands, and that is a property of a save rather than a limitation
    /// here.</b> A save records a world at a Tick; the session that produced it is a different
    /// artefact, which is why <c>--load</c> refuses <c>--log</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>A save resumed under a Ruleset it was not saved under is refused, and forcing it marks
    /// the trace <c>hash-broken</c>.</b> That is <c>RulesetCheck</c>'s polarity applied to a file
    /// rather than to a log — <c>05 §7</c> asks for exactly this mark, and the header carries the
    /// content hash that makes it checkable.
    /// </para>
    /// </remarks>
    private static int Resume(Options options)
    {
        Supplied[] supplied = [.. options.RulesetPaths.Select(
            path => new Supplied(path, RulesetFile.HashOf(path)))];

        if (!TryCatalogue(supplied, supplied[0].Hash, out RulesetCatalogue rulesets))
        {
            return Refused;
        }

        World world;
        SaveHeader header;

        using (var stream = File.OpenRead(options.LoadPath!))
        {
            world = SaveFile.Read(new SaveSource(stream), rulesets.Opening, out header);
        }

        bool hashBroken = header.RulesetInForce != rulesets.OpeningHash;

        if (hashBroken && !options.ForceRuleset)
        {
            Console.Error.WriteLine(F(
                $"{options.LoadPath} was saved under Ruleset {header.RulesetInForce:X16} and {options.RulesetPaths[0]} hashes to {rulesets.OpeningHash:X16}."));
            Console.Error.WriteLine(
                "A city resumed under different Rules is a different city; --force-ruleset runs it "
                + "anyway and marks the trace hash-broken.");

            return Refused;
        }

        var simulation = new Simulation(world, header.Key);
        simulation.VerifyDecideWritesNothing = options.DecideGuard;

        // An empty log, because a save carries no session. It exists so the trace has somewhere to
        // read the Ruleset in force from -- Replay.Trace asks the log at every Tick, and the answer
        // for a resumed world is "the one it was saved under" for the whole run.
        // The Citizen count is the loaded world's own and not `--citizens`, which a resumed run never
        // supplied. The trace header records what would have to match for two traces to be
        // comparable, so a default sitting in that field is the one kind of wrong value it must not
        // carry -- it would read as a city of 10,000 that hashes like a city of 4,000.
        InputLog log = new InputLogBuilder(
                options.Seed,
                new WorldConfiguration(world.Citizens.Rows.LiveCount),
                rulesets.OpeningHash)
            .Build();

        var hashes = new List<ulong>();
        Census? census = options.Census ? new Census(simulation.World) : null;

        ulong resumedAt = simulation.Tick.Raw;

        Console.Out.WriteLine(F(
            $"Resumed {options.LoadPath} at Tick {resumedAt}, hash {world.HashState():X16}"));

        Replay.Trace(simulation, log, new Ticks(options.Ticks), options.HashEvery, hashes, census);

        Write(options, log, hashes, hashBroken, resumedAt);

        if (census is not null)
        {
            CensusReport.Print(Console.Out, simulation.World, census, options.Ticks);
        }

        if (census is not null && options.Series)
        {
            SeriesReport.Print(Console.Out, simulation.World, census, options.Ticks);
        }

        simulation.CheckEndOfRun();

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
    /// What the run's reloads cost the city, if it reloaded at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>02 §4.3</c>'s logged warning, written by the shell because <c>Core</c> may not.</b> The
    /// simulation counts Buildings, Bins and Rule Instances; the sentence about them is the shell's,
    /// which is the whole of <c>adr/0002</c>'s string rule in one method.
    /// </para>
    /// <para>
    /// <b>Silent on a run that never reloaded, and that is deliberate.</b> A line saying <em>0
    /// reloads</em> on every run in the repository would train an operator to skip the block, which
    /// is the state this line needs not to be in on the day it says something.
    /// </para>
    /// </remarks>
    private static void ReportReloads(Simulation simulation)
    {
        if (simulation.Reloads == 0)
        {
            return;
        }

        RulesetTrailTable trail = simulation.World.RulesetTrail;
        RulesetDegradation total = trail.Total();
        int recorded = trail.TransitionsRecorded();

        Console.Out.WriteLine();
        Console.Out.WriteLine(F(
            $"{simulation.Reloads} reload(s), of which {recorded} cost the city something."));

        if (recorded == 0)
        {
            // The ordinary balancing case: numbers moved and no row did. Said explicitly, because
            // "nothing was destroyed" is the answer a designer wants and an absent line is not it.
            Console.Out.WriteLine("Nothing was derelicted, dropped or re-armed.");
            return;
        }

        Console.Out.WriteLine(F(
            $"  {total.BuildingsDerelicted} Building(s) derelicted, {total.BinsDropped} Bin(s) dropped, {total.RuleInstancesRearmed} Rule Instance(s) re-armed."));

        if (recorded > RulesetTrailTable.Retained)
        {
            Console.Out.WriteLine(F(
                $"  The last {RulesetTrailTable.Retained} transitions are named individually; {recorded - RulesetTrailTable.Retained} older one(s) are in the totals only."));
        }
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
    internal static bool TryRules(string? path, out Ruleset rules) =>
        TryRules(path, out rules, out _);

    /// <summary>
    /// <see cref="TryRules(string?, out Ruleset)"/>, and also what the file called the ids.
    /// </summary>
    /// <remarks>
    /// <b>Only a mode that prints a Ruleset-defined <em>name</em> needs this</b>, which until
    /// <see cref="Mode.Evidence"/> was none of them — the eight pictures before it print quantities,
    /// and a quantity needs no vocabulary. <see cref="RulesetNames.None"/> comes back for a run with
    /// no <c>--ruleset</c>, which is the honest answer: there was no file, so there are no names.
    /// </remarks>
    internal static bool TryRules(string? path, out Ruleset rules, out RulesetNames names)
    {
        names = RulesetNames.None;

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
        names = result.Names;
        return true;
    }

    /// <summary>
    /// Parses every supplied Ruleset into a catalogue the replay can resolve a transition against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The opening Ruleset is moved to the front rather than required there.</b>
    /// <see cref="RulesetCatalogue"/> takes its opening entry from position 0 and
    /// <see cref="Replay.Start(InputLog, RulesetCatalogue)"/> builds the world from it, so an operator
    /// who listed the right files in the wrong order would get a city running Rules the log never
    /// named — a divergence with no symptom, because both Rulesets load and both run. The runner knows
    /// which one is opening, because the log says so, and sorting is cheaper than a rule to remember.
    /// </para>
    /// <para>
    /// <b>Every refusal in every file reaches the operator</b>, for <see cref="TryRules"/>'s reason:
    /// stopping at the first would turn one pass over a broken set of files into as many runs as
    /// there are mistakes between them.
    /// </para>
    /// </remarks>
    /// <param name="supplied">The Rulesets named on the command line, with their content hashes.</param>
    /// <param name="opening">The content hash the log opens on.</param>
    /// <param name="rulesets">The catalogue, opening entry first.</param>
    /// <returns>Whether every file parsed.</returns>
    internal static bool TryCatalogue(
        IReadOnlyList<Supplied> supplied, ulong opening, out RulesetCatalogue rulesets)
    {
        ArgumentNullException.ThrowIfNull(supplied);

        rulesets = RulesetCatalogue.None;

        if (supplied.Count == 0)
        {
            // No Rules at all, which is what every figure recorded before slice 7 was taken against.
            // The log's own hash labels it so that Replay.Start's opening check has something to
            // agree with: a session recorded against nothing names nothing.
            rulesets = RulesetCatalogue.Fixed(opening, Ruleset.Empty);
            return true;
        }

        var hashes = new List<ulong>(supplied.Count);
        var rules = new List<Ruleset>(supplied.Count);
        bool ok = true;

        foreach (Supplied entry in supplied)
        {
            if (!TryRules(entry.Path, out Ruleset parsed))
            {
                ok = false;
                continue;
            }

            hashes.Add(entry.Hash);
            rules.Add(parsed);
        }

        if (!ok)
        {
            return false;
        }

        int first = hashes.IndexOf(opening);

        if (first > 0)
        {
            (hashes[0], hashes[first]) = (hashes[first], hashes[0]);
            (rules[0], rules[first]) = (rules[first], rules[0]);
        }
        else if (first < 0)
        {
            // --force-ruleset got the run this far with no supplied Ruleset matching the log's
            // opening hash. Relabelling the first one with the log's hash is exactly what force
            // means -- run these Rules under this log -- and the trace is already stamped
            // hash-broken, which is what stops the numbers being compared to anything.
            hashes[0] = opening;
        }

        rulesets = RulesetCatalogue.Of([.. hashes], [.. rules]);
        return true;
    }

    internal static InputLog Load(Options options, IReadOnlyList<Supplied> supplied)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(supplied);

        if (options.LogPath is null)
        {
            // A fresh session is populated, and it was not before. A run whose world had capacity for
            // a million Citizens and no rows in it reported a State Hash that never moved and a census
            // of zeroes, which read as a stable city and was an empty one — so every Tick figure this
            // project holds, slice 6's 100,000-Tick acceptance run included, was taken over nothing.
            // The command goes in the log rather than into the world, so the trace stays reproducible
            // from the file alone.
            InputLogBuilder builder = new(
                options.Seed,
                new WorldConfiguration(options.Citizens),
                supplied.Count == 0 ? ContentHash.None : supplied[0].Hash);

            builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

            // --reload-at, which is adr/0015's iteration loop and the only shape it could take. A log
            // records a transition by content hash, so a *recorded* session cannot survive the file
            // being edited -- and editing the file is what the loop is. So the transitions are built
            // here, against whatever the Rulesets hash to on this run, and the session stays fully
            // reproducible because the log the runner builds still carries them.
            for (int i = 0; i < options.ReloadTicks.Count; i++)
            {
                builder.Reload(new Ticks(options.ReloadTicks[i]), supplied[i + 1].Hash);
            }

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
    private static void Write(
        Options options, InputLog log, List<ulong> hashes, bool hashBroken, ulong from = 0)
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
            // ⚠ `from` is 0 for every run that starts at Tick 0, which was EVERY run this runner
            // could do until --load existed: a trace's Tick label was the sample index times the
            // cadence, and the assumption that those are the same number was nowhere written down.
            // A resumed run labelled its samples 128 and 256 while standing at Ticks 640 and 768 --
            // plausible numbers, wrong ones, and a trace's whole job is to be diffed against
            // another. A LABEL DERIVED FROM A LOOP COUNTER IS A CLAIM ABOUT WHERE THE LOOP STARTED.
            trace.Add(from + ((ulong)(i + 1) * (ulong)options.HashEvery), hashes[i]);
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

    /// <summary>Slice 10's artefact: the Lot grid before and after a run of sweeping.</summary>
    internal static int DumpZones(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return ZoneDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return ZoneDump.Run(options, writer);
    }

    /// <summary>Slice 5a's artefact: the Road Graph, and its components per mode.</summary>
    internal static int DumpRoads(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return RoadDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return RoadDump.Run(options, writer);
    }

    /// <summary>Milestone 5b's artefact: what a walk costs, and how far past the grid ideal.</summary>
    internal static int DumpTrips(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return TripDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return TripDump.Run(options, writer);
    }

    /// <summary>Milestone 5b-bis's artefact: where people work against where they live.</summary>
    internal static int DumpCommute(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return CommuteDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return CommuteDump.Run(options, writer);
    }

    /// <summary>Milestone 5c's artefact: where the traffic is, and what congestion does to it.</summary>
    internal static int DumpTraffic(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return TrafficDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return TrafficDump.Run(options, writer);
    }

    /// <summary>Runs the Evidence dump. Milestone 6 task 5.</summary>
    internal static int DumpEvidence(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return EvidenceDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return EvidenceDump.Run(options, writer);
    }

    /// <summary>Milestone 10's picture: the circular flow, printed.</summary>
    /// <param name="options">The parsed command line.</param>
    /// <returns>The process exit code.</returns>
    internal static int DumpMoney(Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.OutPath is null)
        {
            return MoneyDump.Run(options, Console.Out);
        }

        using var writer = new StreamWriter(options.OutPath);
        return MoneyDump.Run(options, writer);
    }
}
