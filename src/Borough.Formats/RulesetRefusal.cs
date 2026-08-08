using System.Globalization;
using Borough.Core.Rules;

namespace Borough.Formats;

/// <summary>
/// One reason a Ruleset was refused: a file, a line, and — where there is one — a rule name.
/// </summary>
/// <remarks>
/// <b>The three fields are <c>adr/0015</c>'s promise, not a convention.</b> That ADR makes
/// error-message quality part of the deliverable rather than polish, and names exactly this triple.
/// It is also why the parser is Tomlyn (<c>adr/0048</c>): it was chosen on source spans, and a span
/// is what turns <em>this Ruleset has a cycle</em> into something a designer can act on.
/// </remarks>
/// <param name="File">The file the refusal is in.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Rule">The rule, kind or resource being read, or null where none was in scope.</param>
/// <param name="Reason">What is wrong, in a sentence.</param>
public sealed record RulesetRefusal(string File, int Line, string? Rule, string Reason)
{
    /// <inheritdoc/>
    public override string ToString() =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{File}:{Line}: {(Rule is null ? string.Empty : $"rule '{Rule}': ")}{Reason}");
}

/// <summary>
/// What a load produced: a Ruleset, or the reasons there is not one. Never both, never half of one.
/// </summary>
/// <remarks>
/// <para>
/// <b>This shape is what makes <em>the previous Ruleset stays live</em> structural</b> rather than a
/// rule somebody has to remember. <c>adr/0015</c> requires a refused reload to leave the running
/// simulation on the Rules it already had; a loader that mutated as it parsed would have applied
/// half a Ruleset before reaching the cycle at the bottom of the file. There is nothing to roll back
/// here because nothing was ever applied — see <see cref="RulesetInForce"/> for the swap.
/// </para>
/// <para>
/// <b>Every refusal is collected, not the first one.</b> A designer fixing a Ruleset one refusal per
/// run is a designer for whom <c>adr/0015</c>'s <em>seconds, not a rebuild</em> has quietly stopped
/// being true.
/// </para>
/// </remarks>
public sealed class RulesetLoadResult
{
    private static readonly RulesetRefusal[] NoRefusals = [];

    private RulesetLoadResult(Ruleset? ruleset, IReadOnlyList<RulesetRefusal> refusals)
    {
        Ruleset = ruleset;
        Refusals = refusals;
    }

    /// <summary>The Ruleset, or null when it was refused.</summary>
    public Ruleset? Ruleset { get; }

    /// <summary>Every reason it was refused. Empty on success.</summary>
    public IReadOnlyList<RulesetRefusal> Refusals { get; }

    /// <summary>Whether there is a Ruleset to run.</summary>
    public bool Ok => Ruleset is not null;

    /// <summary>A Ruleset that passed every refusal.</summary>
    public static RulesetLoadResult Accepted(Ruleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(ruleset);

        return new RulesetLoadResult(ruleset, NoRefusals);
    }

    /// <summary>A Ruleset that did not, and why.</summary>
    public static RulesetLoadResult Refused(IReadOnlyList<RulesetRefusal> refusals)
    {
        ArgumentNullException.ThrowIfNull(refusals);

        return refusals.Count == 0
            ? throw new ArgumentException("a refusal must say why.", nameof(refusals))
            : new RulesetLoadResult(null, refusals);
    }

    /// <summary>Every refusal, one per line, for whoever is showing them.</summary>
    public string Describe() => string.Join(Environment.NewLine, Refusals);
}

/// <summary>
/// The Ruleset a simulation is running, and the only door a replacement comes through.
/// </summary>
/// <remarks>
/// <b>It exists so that <em>the previous Ruleset stays live</em> has somewhere to be true.</b>
/// <see cref="TryReplace"/> swaps only on a clean load and returns the refusals otherwise, so the
/// failure mode <c>adr/0015</c> cares about — a designer saves a broken file and the city stops —
/// is not reachable by forgetting a branch. <b>Swapping at a phase boundary, dropping wait lists and
/// logging the transition are slice 8's</b>; this is the holder those need, and nothing more.
/// </remarks>
public sealed class RulesetInForce
{
    /// <summary>The Rules the simulation is actually running. Never null.</summary>
    public Ruleset Current { get; private set; } = Borough.Core.Rules.Ruleset.Empty;

    /// <summary>How many times a load has been refused since this world was created.</summary>
    /// <remarks>
    /// <b>A count rather than a flag, because the interesting case is the second one.</b> A designer
    /// who saves a broken file once has made a typo; one whose count climbs while the city keeps
    /// running is fighting the file format, which is the signal <c>adr/0048</c>'s revisit trigger
    /// about the quote marks asks for.
    /// </remarks>
    public int Refusals { get; private set; }

    /// <summary>
    /// Replaces the Rules in force, or keeps the ones there are and says why it did not.
    /// </summary>
    /// <returns>Empty on success; every refusal otherwise, with <see cref="Current"/> untouched.</returns>
    public IReadOnlyList<RulesetRefusal> TryReplace(RulesetLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Ruleset is null)
        {
            Refusals++;
            return result.Refusals;
        }

        Current = result.Ruleset;
        return result.Refusals;
    }
}
