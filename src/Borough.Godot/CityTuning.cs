using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Borough.Formats;

namespace Borough.Shell;

/// <summary>
/// The tuner's line rewrites, applied to a captured Ruleset and recaptured under a new identity.
/// </summary>
/// <remarks>
/// <para>
/// A dial names a table and a key, never a member, so every member is offered every edit. A member
/// that does not state the table comes back unchanged, which makes the result identical to
/// rewriting whichever member owns the table without having to know which one does.
/// </para>
/// <para>
/// 🔴 <b>BYTES IN, BYTES OUT.</b> Content identity folds over the captured bytes, so a byte order
/// mark and each line's own ending survive the round trip. Re-encoding without them would move the
/// identity of a Ruleset nobody tuned, and would write a member back to its author's directory
/// differing from their copy in more than the tuned value.
/// </para>
/// </remarks>
internal static class CityTuning
{
    /// <summary>The value a key carries, or <c>null</c> when no member states it.</summary>
    public static string? Stated(RulesetCapture capture, string table, string key)
    {
        ArgumentNullException.ThrowIfNull(capture);

        foreach (ReadOnlyMemory<byte> content in Held(capture))
        {
            if (Stated(Decode(content).Text, table, key) is { } value)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Recaptures <paramref name="capture"/> with every edit applied to every member.</summary>
    public static RulesetCaptureResult Turned(
        RulesetCapture capture, IReadOnlyList<(string Table, string Key, string To)> edits)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(edits);

        return RulesetCapture.FromEntries(
            capture.EntryName,
            Turned(capture.Entry, edits),
            capture.Members.Select(member =>
                new KeyValuePair<string, byte[]>(member.Path, Turned(member.Content, edits))));
    }

    /// <summary>
    /// Writes the capture beside <paramref name="beside"/> and returns the entry a runner loads.
    /// </summary>
    /// <remarks>
    /// A package becomes a directory rather than one file, because members are read relative to the
    /// manifest and a runner given a lone manifest would find none of them.
    /// </remarks>
    public static string WriteBeside(RulesetCapture capture, string beside)
    {
        ArgumentNullException.ThrowIfNull(capture);

        if (capture.Mode != RulesetSourceMode.Source)
        {
            string single = Path.ChangeExtension(beside, ".toml");

            File.WriteAllBytes(single, capture.Entry.ToArray());

            return single;
        }

        string folder = Path.ChangeExtension(beside, ".ruleset");

        Directory.CreateDirectory(folder);

        foreach (RulesetMember member in capture.Members)
        {
            string at = Path.Combine(folder, member.Path);

            Directory.CreateDirectory(Path.GetDirectoryName(at)!);
            File.WriteAllBytes(at, member.Content.ToArray());
        }

        string manifest = Path.Combine(folder, Path.GetFileName(capture.EntryName));

        File.WriteAllBytes(manifest, capture.Entry.ToArray());

        return manifest;
    }

    private static IEnumerable<ReadOnlyMemory<byte>> Held(RulesetCapture capture)
    {
        yield return capture.Entry;

        foreach (RulesetMember member in capture.Members)
        {
            yield return member.Content;
        }
    }

    private static string? Stated(string toml, string table, string key)
    {
        string? here = null;

        foreach (string line in toml.Split('\n'))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out string value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Whether a line assigns <paramref name="key"/>, and what it assigns.</summary>
    private static bool Names(string line, string key, out string value)
    {
        value = string.Empty;

        int equals = line.IndexOf('=');

        if (equals < 0 || line.StartsWith('#') || line[..equals].Trim() != key)
        {
            return false;
        }

        int start = equals + 1;

        while (start < line.Length && (line[start] == ' ' || line[start] == '\t'))
        {
            start++;
        }

        // Without the value's own end, a dial on a commented line reads back as "20 # the block"
        // and writing it again would duplicate the comment.
        value = line[start..ValueEnd(line, start)];

        return true;
    }

    private static byte[] Turned(
        ReadOnlyMemory<byte> content, IReadOnlyList<(string Table, string Key, string To)> edits)
    {
        (string text, bool mark) = Decode(content);

        foreach ((string table, string key, string to) in edits)
        {
            text = Turned(text, table, key, to);
        }

        return Encode(text, mark);
    }

    /// <summary>One key rewritten <b>inside its own table and nowhere else</b>.</summary>
    private static string Turned(string toml, string table, string key, string to)
    {
        string[] lines = toml.Split('\n');
        string? here = null;

        for (int at = 0; at < lines.Length; at++)
        {
            string trimmed = lines[at].Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here != table || !Names(trimmed, key, out _))
            {
                continue;
            }

            // Only the value is replaced. Everything around it -- the indentation these files are
            // column-aligned with, the spacing either side of the '=', a trailing comment and the
            // line ending -- is the author's, and comments are part of the content identity.
            string line = lines[at];
            int start = line.IndexOf('=') + 1;

            while (start < line.Length && (line[start] == ' ' || line[start] == '\t'))
            {
                start++;
            }

            lines[at] = string.Concat(line.AsSpan(0, start), to, line.AsSpan(ValueEnd(line, start)));
        }

        return string.Join('\n', lines);
    }

    /// <summary>One past the value beginning at <paramref name="start"/>.</summary>
    private static int ValueEnd(string line, int start)
    {
        if (start < line.Length && (line[start] == '"' || line[start] == '\''))
        {
            int close = line.IndexOf(line[start], start + 1);

            return close < 0 ? line.Length : close + 1;
        }

        int end = start;

        while (end < line.Length && line[end] != '#' && line[end] != '\r')
        {
            end++;
        }

        while (end > start && (line[end - 1] == ' ' || line[end - 1] == '\t'))
        {
            end--;
        }

        return end;
    }

    private static (string Text, bool Mark) Decode(ReadOnlyMemory<byte> bytes)
    {
        ReadOnlySpan<byte> span = bytes.Span;
        bool mark = span.StartsWith(Encoding.UTF8.Preamble);

        return (Encoding.UTF8.GetString(mark ? span[Encoding.UTF8.Preamble.Length..] : span), mark);
    }

    private static byte[] Encode(string text, bool mark)
    {
        byte[] body = Encoding.UTF8.GetBytes(text);

        if (!mark)
        {
            return body;
        }

        byte[] marked = new byte[Encoding.UTF8.Preamble.Length + body.Length];

        Encoding.UTF8.Preamble.CopyTo(marked);
        body.CopyTo(marked, Encoding.UTF8.Preamble.Length);

        return marked;
    }
}
