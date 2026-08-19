namespace Borough.Tests;

/// <summary>
/// Which question a test answers, declared on every test in this assembly.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0032</c>'s discriminator, built.</b> The axis is <b>assertion against instrument</b>
/// and duration is downstream of it. An <b>assertion</b> fails when the city changes, so it has to
/// run every time. An <b>instrument</b> produces a figure for a document to quote, and re-running it
/// on every invocation <em>re-derives a constant that did not move</em>.
/// </para>
/// <para>
/// ⚠ <b>The obvious axis is duration and it is the wrong one, because it describes the symptom.</b>
/// The clearest case is the thirty-four-minute row whose name is its own argument — <em>what the
/// arena is worth at a million Citizens</em>. It is not asking whether the city is correct; it is
/// pricing an allocator, and the price is a figure for a document. Sorting by minutes would put it
/// in the same bucket as a slow correctness test, which is the one grouping that helps nobody.
/// </para>
/// <para>
/// <b>Plain <c>[Trait]</c> rather than a custom attribute, and that is the decision rather than the
/// lazy option.</b> xUnit 2's custom trait attributes need an <c>ITraitDiscoverer</c> and a
/// <c>[TraitDiscoverer]</c> pointing at it by <em>string</em> type name — a citation no compiler
/// checks, in a project whose corpus rules exist because uncheckable citations rot. What the
/// plumbing would buy is a prettier call site; what it costs is a reference that breaks silently on
/// a rename. ***A mechanism whose only advantage is syntax is not worth an unchecked string.***
/// </para>
/// <para>
/// <b>Nothing here reaches the simulation.</b> A tier bounds how long a developer waits; it does not
/// enter the city, so no number in this file is hash-bearing and
/// <c>adr/0052</c> does not apply (<c>plans/0032</c> records that explicitly, because
/// <c>plans/0002</c> §D is where a chosen number reflexively goes).
/// </para>
/// </remarks>
public static class Tier
{
    /// <summary>
    /// The trait key. The fast run is <c>dotnet test --filter "tier!=instrument"</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Filter on <c>tier!=instrument</c> and never on <c>tier=assertion</c>.</b> The default is
    /// reached by <em>absence</em>, so the positive form selects only the handful of tests that
    /// bothered to say what they already were, and silently drops the ~1,600 that did not.
    /// ***With a default by absence, the negative filter is the only one that means what it says.***
    /// </remarks>
    public const string Key = "tier";

    /// <summary>
    /// Fails when the city changes, so it runs every time.
    /// </summary>
    /// <remarks>
    /// <b>The default, reached by absence.</b> A test with no <c>[Trait]</c> is an assertion and is
    /// held to <see cref="TierBudget"/> automatically, so the untagged test is the <em>most</em>
    /// checked one rather than the least. ⚠ <c>plans/0032</c> asks that no tier go undeclared, on the
    /// ground that a tier nothing checks is a per-test status stored in a second place
    /// (<c>plans/0012</c> <b>Cause 1</b>) — right about the risk and wrong about the remedy, because
    /// ***a default the guard applies is not a second copy of a status; a default the guard skips
    /// is.*** <see cref="Corpus.TierDeclarationTests"/> carries the reasoning.
    /// </remarks>
    public const string Assertion = "assertion";

    /// <summary>
    /// Produces a figure for a document to quote, and re-derives a constant when nothing moved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An instrument is not a worse test and must never be deleted for being slow.</b> The nine
    /// minutes buys a number the corpus quotes; this axis is about <em>when</em> that is paid.
    /// The project already has homes for what an instrument produces — <c>plans/0013</c> for what a
    /// Tick costs, <c>docs/spike-results.md</c> for recorded spike numbers — so the output has
    /// somewhere to live that is not a wait.
    /// </para>
    /// <para>
    /// ⚠ <b>The test for this tier is what the test would do on the day it failed.</b> If the answer
    /// is <em>read the new number and paste it into a document</em>, it is an instrument. If the
    /// answer is <em>find out what broke the city</em>, it is an assertion — however long it takes.
    /// A sweep over populations is the usual tell, because a parameter sweep is a shape that produces
    /// a curve rather than a verdict.
    /// </para>
    /// </remarks>
    public const string Instrument = "instrument";
}
