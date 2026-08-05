namespace Borough.Core.Determinism;

/// <summary>
/// The fourth coordinate of <see cref="Randomness.Draw"/>: what a draw is <em>for</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every distinct use gets a distinct tag.</b> Reusing one across two decisions makes those two
/// decisions the same coin flip — a Citizen who chooses a job badly also chooses a shop badly, always,
/// in a way no playtest reads as a bug. adr/0003 and 05 §4 both name this: the correlation is
/// <em>invisible at runtime</em>, so nothing observing a running city can find it.
/// </para>
/// <para>
/// <b>Never a string.</b> A string tag needs string hashing, which 02 §8 rule 2 bans outright, and a
/// mistyped one collides silently instead of failing to compile.
/// </para>
/// <para>
/// <b>Values are explicit and are never renumbered or reused.</b> A tag is an input to the hash, so
/// changing which integer a tag denotes changes every value drawn under it. That is a format change in
/// the sense adr/0003 gives the word — it invalidates stored Input Logs and State Hash baselines — and
/// it is invisible in a diff that only shows the enum. <b>Append; do not slot new tags in beside their
/// siblings.</b>
/// </para>
/// <para>
/// <b>Uniqueness is owed as a build-time check.</b> A duplicate value here is exactly the silent
/// correlation above, wearing valid syntax. <c>PurposeTagTests</c> is the stopgap; the real detector is
/// slice 3's analyser (plans/0006), because a test only runs when someone runs it.
/// </para>
/// <para>
/// This enum is deliberately near-empty. Tags are added when a mechanism that draws is built, not in
/// advance — a tag with no caller cannot be checked against the draw it is supposed to name.
/// </para>
/// </remarks>
public enum PurposeTag : ulong
{
    /// <summary>
    /// Reserved, and never a valid argument to <see cref="Randomness.Draw"/>. It exists so that a
    /// default-initialised <see cref="PurposeTag"/> is recognisable rather than being some real tag's
    /// value — a zeroed struct field must not silently mean "the first purpose anyone declared".
    /// </summary>
    None = 0,
}
