using System.Globalization;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Quantities;

namespace Borough.Formats;

/// <summary>
/// A panic, written as the means of reproducing it: where to start, what to replay, and where it
/// went wrong.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>05 §8</c>, and it is not a dump.</b> The failure path is not <em>log a stack trace
/// and die</em> — it is catch at the Tick boundary and emit the last checkpoint plus the Input Log
/// since it, with the Ruleset content hash and the Tick the panic landed on. What that produces is a
/// <em>reproduction</em>: you replay to the Tick before and single-step into the failure under a
/// debugger, as many times as you like. A dump could only ever show you the corpse.
/// </para>
/// <para>
/// <b>It costs no new machinery, which is <c>adr/0037</c>'s doing.</b> The old justification for
/// crash forensics was the Past/Future double buffer — <em>a Tick that panics while computing the
/// Future leaves the Past intact</em> — and deleting that buffer made the guarantee stronger rather
/// than weaker, because determinism plus the Input Log reproduce the failure instead of merely
/// preserving its aftermath.
/// </para>
/// <para>
/// <b>The file is the header and then a log, verbatim.</b> Everything after the separator is exactly
/// what <see cref="InputLogCodec"/> writes, so cutting the file there yields a replayable
/// <c>.borough</c> and no tooling is needed to get one. That matters here more than anywhere else in
/// the project: this artefact is written at the moment tooling is least trustworthy, and read by
/// somebody who did not produce it.
/// </para>
/// <para>
/// <b><see cref="From"/> is the checkpoint-shaped field, and it is still always zero.</b> The
/// reproduction starts at world creation and the artifact is the seed plus the whole log —
/// equivalent, and smaller. ⚠ <b>This paragraph said checkpoints arrive in <em>milestone 10</em>,
/// which the renumber made 8 — and milestone 8 shipped, so the number is not the repair.</b> A save
/// exists now (<c>Borough.Core.Persistence.SaveFile</c>) and would serve as a checkpoint; nothing has
/// wired it to this artifact, and no milestone owns doing so. Under <c>adr/0070</c> that is
/// <b>unbuilt</b> rather than scheduled. Writing the field now still means somebody fills it in rather
/// than replacing a mechanism; and a reader that meets a non-zero
/// <see cref="From"/> it cannot honour <b>refuses</b>, because replaying from Tick zero instead would
/// reproduce a different city while claiming to reproduce this one.
/// </para>
/// </remarks>
public sealed class CrashArtifact
{
    /// <summary>
    /// The extension. Distinct from <see cref="InputLogCodec.Extension"/> because the two are not
    /// interchangeable: every crash artifact contains a log, and no log contains a crash.
    /// </summary>
    public const string Extension = ".borough-crash";

    private const string Magic = "borough-crash";

    /// <summary>The format version, bumped whenever a header field is added or changes meaning.</summary>
    /// <remarks>
    /// Independent of <see cref="InputLogCodec"/>'s: the log embedded below carries its own, and the
    /// two evolve for different reasons.
    /// </remarks>
    private const int Version = 1;

    private const string Separator = "--";

    /// <summary>When there is nothing to say, so the field is never absent and never empty.</summary>
    private const string NoNote = "none";

    private CrashArtifact(
        InputLog log, Ticks panic, Ticks from, ulong rulesetHash, Violation violation, string note)
    {
        Log = log;
        Panic = panic;
        From = from;
        RulesetHash = rulesetHash;
        Violation = violation;
        Note = note;
    }

    /// <summary>The session to replay, which for Phase 1 is the whole of it.</summary>
    public InputLog Log { get; }

    /// <summary>The Tick the panic landed on. Replay to the Tick before this one.</summary>
    public Ticks Panic { get; }

    /// <summary>The Tick the reproduction starts at. Always zero — nothing writes a checkpoint here
    /// yet.</summary>
    /// <inheritdoc cref="CrashArtifact" path="/remarks/para[5]"/>
    public Ticks From { get; }

    /// <summary>
    /// The Ruleset actually in force, which is not always the one the log names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A run forced across a mismatch is running Rules the log does not describe, and a reproduction
    /// attempted against the log's Ruleset would diverge for that reason rather than for the reason
    /// it crashed. Recording what was in force is what keeps the artefact honest about which.
    /// </para>
    /// <para>
    /// <b>Since slice 8 there is a second way for these to differ, and it is the ordinary one.</b>
    /// <c>InputLog.RulesetHash</c> is the Ruleset a session <em>opened</em> with; a session that hot
    /// reloaded crashed under a later one. So this is the hash at the <b>panic Tick</b>, which the
    /// embedded log can also be asked for — and the two agreeing is the check that the artefact and
    /// its log describe one run.
    /// </para>
    /// </remarks>
    public ulong RulesetHash { get; }

    /// <summary>
    /// The invariant that broke, or <see cref="Invariants.Violation.None"/> if the panic was not one.
    /// </summary>
    /// <remarks>
    /// Structured because these are ids a tool can act on — group two reports, or decide a known
    /// failure is already filed. <see cref="Note"/> is the same event for a human, and neither
    /// substitutes for replaying it.
    /// </remarks>
    public Violation Violation { get; }

    /// <summary>
    /// What was thrown, as one line, for whoever opens the file first.
    /// </summary>
    /// <remarks>
    /// <b>A diagnostic, never a Readout.</b> <c>adr/0002</c> gives the shell every string a human
    /// reads and the leak vector it names is a core method that <em>returns</em> a formatted string
    /// because a panel wanted one. This is the <c>StaleHandleException</c> precedent: text addressed
    /// to somebody holding a debugger, written by the project that already spells verbs in words.
    /// </remarks>
    public string Note { get; }

    /// <summary>Builds an artifact for a panic during a run.</summary>
    /// <param name="log">The session that was being replayed.</param>
    /// <param name="panic">The Tick the panic landed on.</param>
    /// <param name="rulesetHash">The Ruleset actually in force.</param>
    /// <param name="fault">What was thrown.</param>
    public static CrashArtifact Of(InputLog log, Ticks panic, ulong rulesetHash, Exception fault)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(fault);

        Violation violation = fault is InvariantViolationException broken
            ? broken.Violation
            : Violation.None;

        // Tick zero: nothing writes a checkpoint into an artifact, so every reproduction starts at
        // world creation. See the type's remarks for why the field is written at all, and for why the
        // number this comment used to name was not the repair.
        return new CrashArtifact(log, panic, new Ticks(0), rulesetHash, violation, Sanitise(fault));
    }

    /// <summary>Writes an artifact in the form <see cref="Read(TextReader)"/> accepts.</summary>
    public static void Write(TextWriter writer, CrashArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(artifact);

        writer.Write(Line($"{Magic} {Version}"));
        writer.Write(Line($"tick {artifact.Panic.Raw}"));
        writer.Write(Line($"from {artifact.From.Raw}"));
        writer.Write(Line($"ruleset 0x{artifact.RulesetHash:X16}"));
        writer.Write(Line(
            $"violation {artifact.Violation.Invariant} {artifact.Violation.Slot} "
            + $"{artifact.Violation.Other}"));
        writer.Write(Line($"note {artifact.Note}"));
        writer.Write(Line(Separator));

        // Verbatim, so that cutting the file here yields a log this project can already read.
        InputLogCodec.Write(writer, artifact.Log);
    }

    /// <summary>The whole file as a string.</summary>
    public static string ToText(CrashArtifact artifact)
    {
        var text = new StringWriter(CultureInfo.InvariantCulture);
        Write(text, artifact);
        return text.ToString();
    }

    /// <summary>
    /// Parses an artifact, refusing anything it does not fully understand.
    /// </summary>
    /// <exception cref="FormatException">The text is not an artifact this version can reproduce.</exception>
    public static CrashArtifact Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var lines = new Cursor(reader);

        ReadMagic(lines);

        ulong panic = Number(lines, Value(lines, "tick"));
        ulong from = Number(lines, Value(lines, "from"));
        ulong ruleset = Number(lines, Value(lines, "ruleset"));
        Violation violation = ReadViolation(lines, new Ticks(panic));
        string note = Value(lines, "note");

        if (from != 0)
        {
            throw lines.Complain(
                $"this artifact reproduces from Tick {from}, and reproducing from anywhere but world "
                + "creation needs a checkpoint, and nothing writes one into an artifact yet. "
                + "Replaying from zero instead would rebuild a different city and blame it on this "
                + "crash.");
        }

        if (lines.Next() is not Separator)
        {
            throw lines.Complain($"expected '{Separator}' between the header and the log.");
        }

        // The rest of the file is a log, and the log's own reader owns every complaint about it.
        InputLog log = InputLogCodec.Read(reader);

        return new CrashArtifact(log, new Ticks(panic), new Ticks(from), ruleset, violation, note);
    }

    /// <inheritdoc cref="Read(TextReader)"/>
    public static CrashArtifact FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        using var reader = new StringReader(text);
        return Read(reader);
    }

    /// <summary>Whether this text is a crash artifact rather than a bare Input Log.</summary>
    /// <remarks>
    /// <b>The magic line exists so that a file can answer this, and this is the question it was put
    /// there for.</b> An artifact is handed back to the runner in exactly the place a log goes —
    /// replaying it is the only thing anybody wants to do with one — so the runner has to tell the
    /// two apart without being told which it was given. Asking the file beats a flag the person
    /// reproducing a crash would have to know to pass.
    /// </remarks>
    public static bool IsCrashArtifact(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        using var reader = new StringReader(text);

        return new Cursor(reader).Next()?.StartsWith(Magic + ' ', StringComparison.Ordinal) ?? false;
    }

    private static void ReadMagic(Cursor lines)
    {
        string line = lines.Require();
        string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != 2 || fields[0] != Magic)
        {
            throw lines.Complain($"this is not a {Magic} file.");
        }

        int version = (int)Number(lines, fields[1]);

        if (version != Version)
        {
            throw lines.Complain(
                $"format version {version}, and this build reads version {Version}.");
        }
    }

    private static Violation ReadViolation(Cursor lines, Ticks panic)
    {
        string[] fields = Value(lines, "violation")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != 3)
        {
            throw lines.Complain("a violation is an invariant and two rows.");
        }

        if (!Enum.TryParse(fields[0], out Invariant invariant))
        {
            throw lines.Complain($"'{fields[0]}' is not an invariant this build knows.");
        }

        return new Violation(invariant, panic, Signed(lines, fields[1]), Wide(lines, fields[2]));
    }

    /// <summary>Reads a keyed line and returns everything after the key.</summary>
    /// <remarks>
    /// The remainder rather than the next field, because <c>note</c> is free text. Keeping one
    /// accessor for every header line is what stops the note's looseness spreading to the others.
    /// </remarks>
    private static string Value(Cursor lines, string key)
    {
        string line = lines.Require();

        if (!line.StartsWith(key + ' ', StringComparison.Ordinal))
        {
            throw lines.Complain($"expected the '{key}' line, and found '{line}'.");
        }

        return line[(key.Length + 1)..].Trim();
    }

    /// <summary>
    /// The exception as a single line: type, then message, with all whitespace collapsed.
    /// </summary>
    /// <remarks>
    /// Collapsed because the header is line-oriented and a message with a newline in it would end the
    /// field early and take the rest of the parse with it. The stack trace is deliberately absent —
    /// it belongs on the console of the run that crashed, and what makes this file worth having is
    /// that it produces a <em>live</em> stack on demand rather than a copy of a dead one.
    /// </remarks>
    private static string Sanitise(Exception fault)
    {
        string raw = $"{fault.GetType().FullName}: {fault.Message}";
        string[] words = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        return words.Length == 0 ? NoNote : string.Join(' ', words);
    }

    private static ulong Number(Cursor lines, string field)
    {
        bool hex = field.StartsWith("0x", StringComparison.Ordinal);

        return ulong.TryParse(
            hex ? field.AsSpan(2) : field,
            hex ? NumberStyles.HexNumber : NumberStyles.None,
            CultureInfo.InvariantCulture,
            out ulong value)
            ? value
            : throw lines.Complain($"'{field}' is not a number.");
    }

    /// <summary>A row index, which is <c>-1</c> when the violation did not name one.</summary>
    private static int Signed(Cursor lines, string field) =>
        int.TryParse(field, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw lines.Complain($"'{field}' is not a row.");

    /// <summary>A signed 64-bit detail: a row, or a quantity a Bin's level could reach.</summary>
    private static long Wide(Cursor lines, string field) =>
        long.TryParse(field, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value)
            ? value
            : throw lines.Complain($"'{field}' is not a row or a quantity.");

    private static string Line(FormattableString content) =>
        content.ToString(CultureInfo.InvariantCulture) + "\n";

    private static string Line(string content) => content + "\n";

    /// <summary>
    /// The reader's position, so that every complaint can name the line it is about.
    /// </summary>
    /// <remarks>
    /// Blank lines and <c>#</c> comments are skipped, matching <see cref="InputLogCodec"/>: this is a
    /// file people annotate while narrowing a bug down, and a format that refuses the annotation is a
    /// format they copy out of instead.
    /// </remarks>
    private sealed class Cursor(TextReader reader)
    {
        private int _number;

        public string? Next()
        {
            while (reader.ReadLine() is { } raw)
            {
                _number++;
                string line = raw.Trim();

                if (line.Length > 0 && !line.StartsWith('#'))
                {
                    return line;
                }
            }

            _number++;
            return null;
        }

        public string Require() => Next() ?? throw Complain("the file ends before it is complete.");

        public FormatException Complain(string what) =>
            new(string.Create(CultureInfo.InvariantCulture, $"line {_number}: {what}"));
    }
}
