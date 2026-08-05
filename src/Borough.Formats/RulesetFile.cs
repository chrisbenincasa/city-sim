using Borough.Core.Determinism;

namespace Borough.Formats;

/// <summary>
/// Reads a Ruleset from disk and computes the content hash that names it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the hash, for now.</b> Slice 8 is where a Ruleset is parsed, interpreted and hot-reloaded
/// (<c>adr/0015</c>). What slice 5 needs is narrower and cannot wait for it: <c>05 §7</c> makes a
/// replay against a different Ruleset a <em>different simulation</em>, so the runner must be able to
/// tell whether the Rules on disk are the ones the log was recorded against, before it produces
/// numbers that mean nothing.
/// </para>
/// <para>
/// <b>Where the line falls: this project decides what counts as the same content, and the core
/// computes the hash of it.</b> The core reads no file (<c>02 §1</c>). The shells must not each
/// decide for themselves, or a log written by one replays in the other against a Ruleset both agree
/// is identical and neither hashed the same way.
/// </para>
/// </remarks>
public static class RulesetFile
{
    /// <summary>
    /// The content hash of the Ruleset at <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Line endings are normalised before hashing, and nothing else is.</b> A Ruleset is text a
    /// designer edits, checked into a repository that will be cloned on Windows and on Linux; without
    /// this, the same file would carry two content hashes depending on the machine, and
    /// <c>--strict</c> would refuse to replay a log against the very Ruleset it was recorded against.
    /// That failure is invisible in a diff and would be blamed on the log.
    /// </para>
    /// <para>
    /// <b>Whitespace, key order and comments are content.</b> Normalising further would mean parsing,
    /// and parsing is slice 8's. It also means a Ruleset that has been reformatted reads as changed,
    /// which is honest: nothing here has read the Rules, so nothing here can claim the reformatting
    /// was semantically empty.
    /// </para>
    /// </remarks>
    public static ulong HashOf(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return HashOfContent(File.ReadAllBytes(path));
    }

    /// <inheritdoc cref="HashOf"/>
    public static ulong HashOfContent(ReadOnlySpan<byte> content)
    {
        const byte CarriageReturn = 0x0D;
        const byte LineFeed = 0x0A;

        Span<byte> normalised = new byte[content.Length];
        int length = 0;

        for (int i = 0; i < content.Length; i++)
        {
            bool stripped = content[i] == CarriageReturn
                && i + 1 < content.Length
                && content[i + 1] == LineFeed;

            if (!stripped)
            {
                normalised[length++] = content[i];
            }
        }

        return ContentHash.Of(normalised[..length]);
    }
}
