namespace Borough.Core.Determinism;

/// <summary>
/// The content hash that identifies a Ruleset.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>05 §7</c>: the Ruleset is identified by a content hash, and the Input Log references it.</b>
/// That reference is what lets a replay refuse to run against the wrong Rules instead of diverging
/// silently — which <c>05 §7</c> names as otherwise the most confusing possible failure, a replay
/// that reproduces nothing because the data files moved underneath it.
/// </para>
/// <para>
/// <b>It lives in the core because it is arithmetic, and the core reads no file.</b> The caller hands
/// over bytes it obtained from somewhere; how they were obtained, and what counts as the same content
/// when the bytes differ, is the format layer's question and is answered in
/// <c>Borough.Formats</c>. The reason to draw the line there rather than let each shell hash a file
/// its own way is <c>adr/0039</c>'s: a log written by the game must replay in the headless runner, so
/// the two must agree, so there must be one implementation for them to agree with.
/// </para>
/// <para>
/// <b>The fold is the State Hash's, deliberately.</b> Reaching for a cryptographic digest would be
/// the reflex here, and it would be wrong twice over: this hash defends against a Ruleset that
/// changed by accident, not against one changed by an adversary, and a second hash function in a
/// project whose entire correctness argument rests on one normative fold (<c>adr/0003</c>) is a
/// second thing that can drift.
/// </para>
/// </remarks>
public static class ContentHash
{
    /// <summary>
    /// The empty content's hash, which is also what a session with no Ruleset carries.
    /// </summary>
    /// <remarks>
    /// <b>Zero, so that "no Ruleset" and "a Ruleset that hashes to nothing" are the same value.</b>
    /// Before slice 8 there is no Ruleset to load and every log in the repository carries zero here;
    /// picking a non-zero empty-hash would mean every one of them changes the day content arrives,
    /// for no gain.
    /// </remarks>
    public const ulong None = 0UL;

    /// <summary>Folds a block of content into the hash that names it.</summary>
    /// <param name="content">The bytes, exactly as the format layer decided they should be compared.</param>
    public static ulong Of(ReadOnlySpan<byte> content)
    {
        if (content.IsEmpty)
        {
            return None;
        }

        // Eight bytes at a time, packed by shifting the accumulator rather than the byte. Both the
        // constant shift count and the byte order are then fixed by the code rather than by the
        // machine — this value is compared across machines, so a BitConverter read would have made
        // the answer depend on the endianness of whoever ran it.
        ulong hash = Randomness.Mix((ulong)content.Length);
        ulong word = 0;
        int filled = 0;

        foreach (byte b in content)
        {
            word = (word << 8) | b;
            filled++;

            if (filled == 8)
            {
                hash = Randomness.Mix(hash ^ word);
                word = 0;
                filled = 0;
            }
        }

        // The tail is folded with its length, so that trailing zero bytes cannot be added or removed
        // without moving the hash.
        return filled == 0 ? hash : Randomness.Mix(hash ^ word ^ (ulong)filled);
    }
}
