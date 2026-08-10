using System.Globalization;
using System.Text;
using Borough.Core.Input;
using Borough.Core.Quantities;

namespace Borough.Formats;

/// <summary>
/// The Input Log's on-disk form: line-oriented text, in a <c>.borough</c> file.
/// </summary>
/// <remarks>
/// <para>
/// <b>Text rather than binary, decided in slice 5 and recorded in
/// <see href="../../plans/0008-tick-and-replay.md">plans/0008</see>.</b> The log is <em>attached</em>
/// to a bug report far more often than it is diffed, so <em>legible without tooling</em> beat
/// <em>diffable</em>; the crash artifact is emitted at the moment tooling is least trustworthy; and
/// binary's usual advantage is size, which a ten-hour session being kilobytes deletes. Binary's real
/// win — no locale exposure — is answered by <c>InvariantGlobalization</c> and by every parse here
/// naming the invariant culture explicitly.
/// </para>
/// <para>
/// <b>One implementation, in one project, because agreement is the format's entire purpose</b>
/// (<c>adr/0039</c>). A log written by <c>Borough.Godot</c> must replay in <c>Borough.Headless</c>.
/// Two codecs that drift produce a log which parses cleanly and replays to a <em>different</em> city
/// — a State Hash divergence with no cause, which is the diagnostic dead end this slice exists to
/// abolish.
/// </para>
/// <para>
/// <b>All four verbs are encoded, though only Zone is applied.</b> Connect, Service and Govern throw
/// on application until slice 7, but the log format has their slot today, so the artefact a bug
/// report is made of does not change shape when they arrive — and this format version does not have
/// to be bumped for their arrival.
/// </para>
/// </remarks>
public static class InputLogCodec
{
    /// <summary>The extension, from <c>adr/0039</c>. Not <c>.log</c> and not <c>.inputlog</c>.</summary>
    /// <remarks>
    /// Both of those are ignored by the repository's inherited .NET <c>.gitignore</c>, and the
    /// golden-hash baseline is a <em>committed</em> log. A template ignore rule could have silently
    /// prevented the project's most important regression artefact from ever being tracked.
    /// </remarks>
    public const string Extension = ".borough";

    /// <summary>The first token of the first line, so a file identifies itself before anything else.</summary>
    private const string Magic = "borough-log";

    /// <summary>
    /// The format version, which <c>adr/0039</c> makes this project's to bump.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bump it when an old reader would <em>misread</em> a new log</b> — a log outlives the build
    /// that wrote it, and a reader that guesses is a reader that reproduces the wrong city. That is
    /// the property, and it is narrower than the rule this comment used to state (<em>whenever a
    /// field is added to <see cref="Command"/>, to <see cref="WorldConfiguration"/> or to the
    /// header</em>), which was a proxy for it.
    /// </para>
    /// <para>
    /// <b>The proxy has now been wrong twice, in the same direction, and the second time was a header
    /// change.</b> A new verb did not bump it: an old reader meeting <c>populate</c> refuses by name,
    /// with a line number, which is strictly better than a version refusal. Slice 8's <c>reload</c>
    /// lines are a **header** addition and did not bump it either, for the same reason and with the
    /// same evidence: an old reader meeting one complains <em>expected '--' between the header and
    /// the commands</em>, and a new reader meeting an old log finds no reloads and runs one Ruleset
    /// throughout. Neither direction misreads, so a bump would buy nothing and cost every log ever
    /// written — including the committed golden baseline.
    /// </para>
    /// <para>
    /// <b>What would bump it is a change to a line that already exists</b>: a sixth field on a
    /// command, a second number on <c>citizens</c>, a different meaning for <c>seed</c>. Those an old
    /// reader parses happily and gets wrong, which is exactly the case a version exists for.
    /// </para>
    /// </remarks>
    private const int Version = 1;

    private const string Separator = "--";

    /// <summary>Writes a log in the form <see cref="Read(TextReader)"/> accepts.</summary>
    public static void Write(TextWriter writer, InputLog log)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(log);

        writer.Write(Line($"{Magic} {Version}"));
        writer.Write(Line($"seed 0x{log.Seed:X16}"));
        writer.Write(Line($"citizens {log.Configuration.Citizens}"));
        writer.Write(Line($"ruleset 0x{log.RulesetHash:X16}"));

        for (int i = 0; i < log.TransitionCount; i++)
        {
            RulesetTransition transition = log.Transition(i);

            writer.Write(Line(
                $"reload {transition.Tick.Raw} 0x{transition.From:X16} 0x{transition.To:X16}"));
        }

        writer.Write(Line(Separator));

        for (int i = 0; i < log.Count; i++)
        {
            (Ticks tick, Command command) = log.Entry(i);

            writer.Write(Line(
                $"{tick.Raw} {Verb(command.Kind)} {command.East.Raw} {command.North.Raw} {command.Zone}"));
        }
    }

    /// <summary>The whole file as a string, for a caller holding one in memory rather than on disk.</summary>
    public static string ToText(InputLog log)
    {
        var text = new StringWriter(CultureInfo.InvariantCulture);
        Write(text, log);
        return text.ToString();
    }

    /// <summary>
    /// Parses a log, refusing anything it does not fully understand.
    /// </summary>
    /// <remarks>
    /// <b>Every refusal names the line it happened on.</b> This file is read when something has
    /// already gone wrong, often by somebody who did not write it, and <em>malformed input log</em>
    /// is not a diagnosis.
    /// </remarks>
    /// <exception cref="FormatException">The text is not a log this version can read.</exception>
    public static InputLog Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var lines = new Cursor(reader);

        ReadMagic(lines);

        ulong seed = ReadHeader(lines, "seed");
        ulong citizens = ReadHeader(lines, "citizens");
        ulong ruleset = ReadHeader(lines, "ruleset");

        if (citizens > int.MaxValue)
        {
            throw lines.Complain($"a configuration of {citizens} Citizens is not a number of rows.");
        }

        InputLogBuilder builder = new(seed, new WorldConfiguration((int)citizens), ruleset);

        // Zero or more reloads, then the separator. A log written before slice 8 has none and reads
        // exactly as it always did, which is why the format version did not move — see Version.
        string? header = lines.Next();

        while (header is not null && header != Separator)
        {
            ReadReload(lines, builder, header);
            header = lines.Next();
        }

        if (header is null)
        {
            throw lines.Complain($"expected '{Separator}' between the header and the commands.");
        }

        while (lines.Next() is { } line)
        {
            builder.Append(ReadTick(lines, line), ReadCommand(lines, line));
        }

        return builder.Build();
    }

    /// <inheritdoc cref="Read(TextReader)"/>
    public static InputLog FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        using var reader = new StringReader(text);
        return Read(reader);
    }

    private static void ReadMagic(Cursor lines)
    {
        string[] fields = Fields(lines, lines.Next(), expected: 2);

        if (fields[0] != Magic)
        {
            throw lines.Complain($"this is not a {Magic} file.");
        }

        int version = (int)Number(lines, fields[1]);

        if (version != Version)
        {
            throw lines.Complain(
                $"format version {version}, and this build reads version {Version}. "
                + "A log outlives the build that wrote it; the reader is what must be taught, "
                + "never the file that must be edited.");
        }
    }

    private static ulong ReadHeader(Cursor lines, string key)
    {
        string[] fields = Fields(lines, lines.Next(), expected: 2);

        if (fields[0] != key)
        {
            throw lines.Complain($"expected the '{key}' line, and found '{fields[0]}'.");
        }

        return Number(lines, fields[1]);
    }

    /// <summary>
    /// One <c>reload TICK 0xFROM 0xTO</c> line, with its <c>from</c> checked against the chain.
    /// </summary>
    /// <remarks>
    /// <b>The <c>from</c> hash is redundant to replay and checked anyway, which is the only reason it
    /// is written.</b> A log's <c>to</c> hashes alone reproduce the session; carrying <c>from</c> as
    /// well means a log that has been hand-edited, truncated or spliced is caught <em>here</em>, by
    /// name and with a line number, rather than as a State Hash divergence with no cause — which is
    /// the diagnostic dead end this whole format exists to abolish.
    /// </remarks>
    private static void ReadReload(Cursor lines, InputLogBuilder builder, string line)
    {
        string[] fields = Fields(lines, line, expected: 4);

        if (fields[0] != "reload")
        {
            throw lines.Complain(
                $"'{fields[0]}' is not a header line this format knows. After 'ruleset' come zero or "
                + $"more 'reload' lines, then '{Separator}'.");
        }

        var tick = new Ticks(Number(lines, fields[1]));
        ulong from = Number(lines, fields[2]);
        ulong to = Number(lines, fields[3]);

        if (from != builder.InForce)
        {
            throw lines.Complain(
                $"this reload says it leaves Ruleset 0x{from:X16}, and 0x{builder.InForce:X16} was "
                + "in force. The chain is broken, so this log has been edited or spliced and does "
                + "not describe one session.");
        }

        try
        {
            builder.Reload(tick, to);
        }
        catch (InvalidOperationException refusal)
        {
            // The builder's refusals are about a log being well-formed and are the same whether it
            // was authored or parsed. Re-thrown with the line, because that is what a reader needs
            // and what a caller of the builder does not have.
            throw lines.Complain(refusal.Message);
        }
    }

    private static Ticks ReadTick(Cursor lines, string? line) =>
        new(Number(lines, Fields(lines, line, expected: 5)[0]));

    private static Command ReadCommand(Cursor lines, string? line)
    {
        string[] fields = Fields(lines, line, expected: 5);

        CommandKind kind = fields[1] switch
        {
            "zone" => CommandKind.Zone,
            "connect" => CommandKind.Connect,
            "service" => CommandKind.Service,
            "govern" => CommandKind.Govern,
            "populate" => CommandKind.Populate,
            _ => throw lines.Complain($"'{fields[1]}' is not a verb this format knows."),
        };

        return new Command(
            kind,
            new Tiles(Signed(lines, fields[2])),
            new Tiles(Signed(lines, fields[3])),
            (ushort)Bounded(lines, fields[4], ushort.MaxValue));
    }

    private static string Verb(CommandKind kind) => kind switch
    {
        CommandKind.Zone => "zone",
        CommandKind.Connect => "connect",
        CommandKind.Service => "service",
        CommandKind.Govern => "govern",
        CommandKind.Populate => "populate",
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "a command with no verb cannot be written."),
    };

    private static string[] Fields(Cursor lines, string? line, int expected)
    {
        if (line is null)
        {
            throw lines.Complain("the file ends before it is complete.");
        }

        string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return fields.Length == expected
            ? fields
            : throw lines.Complain($"expected {expected} fields and found {fields.Length}.");
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

    private static ulong Bounded(Cursor lines, string field, ulong limit)
    {
        ulong value = Number(lines, field);

        return value <= limit
            ? value
            : throw lines.Complain($"{value} does not fit the field, whose largest value is {limit}.");
    }

    /// <summary>
    /// A coordinate, which may be negative — <c>Tiles</c> is signed and an origin is a choice.
    /// </summary>
    private static int Signed(Cursor lines, string field) =>
        int.TryParse(field, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw lines.Complain($"'{field}' is not a coordinate.");

    private static string Line(FormattableString content) =>
        content.ToString(CultureInfo.InvariantCulture) + "\n";

    private static string Line(string content) => content + "\n";

    /// <summary>
    /// The reader's position, so that every complaint can name the line it is about.
    /// </summary>
    /// <remarks>
    /// <b>Blank lines and <c>#</c> comments are skipped everywhere</b>, including inside the command
    /// body. A log is a file people annotate while narrowing down a bug — <em>this is the command
    /// that does it</em> — and a format that refuses the annotation is a format they copy out of
    /// instead.
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

        public FormatException Complain(string what) =>
            new(string.Create(CultureInfo.InvariantCulture, $"line {_number}: {what}"));
    }
}
