using System.Globalization;

namespace Borough.Formats;

/// <summary>
/// A State Hash trace, as a file: a header of named numbers, then one line per sample.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is slice 5's "something to look at"</b> — the first artefact in the project that can catch
/// a bug nobody was looking for, and it earns that by being diffable against a trace recorded on
/// another day or another machine.
/// </para>
/// <para>
/// <b>One type, because the runner's output and the golden baseline are the same artefact.</b> The
/// runner's <c>--out</c> writes this; the golden-hash tests read it. Had they been two formats, a
/// re-baseline would have meant transcribing between them, which is the step at which a digit gets
/// dropped. It also means a deliberate re-baseline can be produced by running the runner, which is a
/// human act with a reviewable diff rather than a switch that rewrites its own oracle.
/// </para>
/// <para>
/// <b>Read as parsed structure, never as bytes.</b> Line endings differ between the machines this
/// repository is checked out on, and a baseline that reddened on a checkout setting is a baseline
/// people learn to ignore. Comments and blank lines are skipped, so a trace can be annotated where
/// somebody worked out what moved.
/// </para>
/// <para>
/// <b>A header with no samples is a legal trace</b>, and is how a single hash over a built world is
/// recorded. The grammar is the same one either way, which is the whole reason there is one reader.
/// </para>
/// </remarks>
public sealed class HashTrace
{
    private const string Separator = "--";

    private readonly List<KeyValuePair<string, ulong>> _header;
    private readonly List<Sample> _samples;

    private HashTrace(List<KeyValuePair<string, ulong>> header, List<Sample> samples)
    {
        _header = header;
        _samples = samples;
    }

    /// <summary>One State Hash, and the number of Ticks elapsed when it was taken.</summary>
    /// <param name="After">Ticks elapsed. The change that moved a sample entered in the window ending here.</param>
    /// <param name="Hash">The State Hash.</param>
    public readonly record struct Sample(ulong After, ulong Hash);

    /// <summary>The samples, in file order.</summary>
    public IReadOnlyList<Sample> Samples => _samples;

    /// <summary>The header, in file order.</summary>
    public IReadOnlyList<KeyValuePair<string, ulong>> Header => _header;

    /// <summary>Starts an empty trace, to be filled in and written.</summary>
    public static HashTrace Create() => new([], []);

    /// <summary>Adds a header line. Order is preserved, and a repeated key is an error.</summary>
    public HashTrace With(string key, ulong value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (Has(key))
        {
            throw new ArgumentException($"the header already carries '{key}'.", nameof(key));
        }

        _header.Add(new KeyValuePair<string, ulong>(key, value));
        return this;
    }

    /// <summary>Adds a sample.</summary>
    public HashTrace Add(ulong after, ulong hash)
    {
        _samples.Add(new Sample(after, hash));
        return this;
    }

    /// <summary>Whether the header carries <paramref name="key"/>.</summary>
    public bool Has(string key)
    {
        foreach (KeyValuePair<string, ulong> entry in _header)
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The header value under <paramref name="key"/>.</summary>
    /// <exception cref="KeyNotFoundException">There is no such line.</exception>
    public ulong Number(string key)
    {
        foreach (KeyValuePair<string, ulong> entry in _header)
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                return entry.Value;
            }
        }

        throw new KeyNotFoundException($"the trace header has no '{key}' line.");
    }

    /// <summary>The hashes alone, which is what a comparison between two runs is over.</summary>
    public ulong[] Hashes()
    {
        var hashes = new ulong[Samples.Count];

        for (int i = 0; i < hashes.Length; i++)
        {
            hashes[i] = Samples[i].Hash;
        }

        return hashes;
    }

    /// <summary>Parses a trace.</summary>
    /// <exception cref="FormatException">The text is not a trace.</exception>
    public static HashTrace Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var header = new List<KeyValuePair<string, ulong>>();
        var samples = new List<Sample>();
        bool body = false;
        int number = 0;

        while (reader.ReadLine() is { } raw)
        {
            number++;
            string line = raw.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line == Separator)
            {
                body = true;
                continue;
            }

            string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 2)
            {
                throw new FormatException($"line {number}: expected a pair, and found '{line}'.");
            }

            if (body)
            {
                samples.Add(new Sample(Number(fields[0], number), Number(fields[1], number)));
            }
            else
            {
                header.Add(new KeyValuePair<string, ulong>(fields[0], Number(fields[1], number)));
            }
        }

        return new HashTrace(header, samples);
    }

    /// <inheritdoc cref="Read(TextReader)"/>
    public static HashTrace FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        using var reader = new StringReader(text);
        return Read(reader);
    }

    /// <summary>
    /// Writes the trace, under <paramref name="preamble"/> lines rendered as comments.
    /// </summary>
    /// <remarks>
    /// The preamble is where a file says what it is and what regenerating it means. It is written
    /// rather than parsed: a comment that a reader had to understand would not be a comment.
    /// </remarks>
    public void Write(TextWriter writer, params string[] preamble)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(preamble);

        foreach (string comment in preamble)
        {
            writer.Write("# " + comment + "\n");
        }

        foreach (KeyValuePair<string, ulong> entry in _header)
        {
            writer.Write(Line($"{entry.Key} {Render(entry.Key, entry.Value)}"));
        }

        if (_samples.Count == 0)
        {
            return;
        }

        writer.Write(Separator + "\n");

        foreach (Sample sample in _samples)
        {
            writer.Write(Line($"{sample.After} 0x{sample.Hash:X16}"));
        }
    }

    /// <inheritdoc cref="Write"/>
    public string ToText(params string[] preamble)
    {
        var text = new StringWriter(CultureInfo.InvariantCulture);
        Write(text, preamble);
        return text.ToString();
    }

    /// <summary>
    /// A 64-bit identity is written in hex and a count in decimal, decided by the key's name.
    /// </summary>
    /// <remarks>
    /// <b>A rendering rule rather than a typed value, deliberately.</b> Both forms parse either way,
    /// so nothing depends on getting this right — it exists so that a human scanning the file sees
    /// <c>0xB9D2…</c> where an identity belongs and <c>1000</c> where a count does, and notices when
    /// a number is in the wrong column.
    /// </remarks>
    private static string Render(string key, ulong value) =>
        IsIdentity(key)
            ? string.Create(CultureInfo.InvariantCulture, $"0x{value:X16}")
            : string.Create(CultureInfo.InvariantCulture, $"{value}");

    private static bool IsIdentity(string key) =>
        key.EndsWith("seed", StringComparison.Ordinal)
        || key.EndsWith("hash", StringComparison.Ordinal)
        || key.EndsWith("ruleset", StringComparison.Ordinal);

    private static ulong Number(string field, int line)
    {
        bool hex = field.StartsWith("0x", StringComparison.Ordinal);

        return ulong.TryParse(
            hex ? field.AsSpan(2) : field,
            hex ? NumberStyles.HexNumber : NumberStyles.None,
            CultureInfo.InvariantCulture,
            out ulong value)
            ? value
            : throw new FormatException($"line {line}: '{field}' is not a number.");
    }

    private static string Line(FormattableString content) =>
        content.ToString(CultureInfo.InvariantCulture) + "\n";
}
