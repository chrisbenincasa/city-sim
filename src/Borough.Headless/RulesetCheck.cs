using System.Globalization;

namespace Borough.Headless;

/// <summary>
/// Whether a session may run against the Ruleset it was handed, and what to say if not.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>05 §7</c> maps two policies onto the two shells:</b> <c>Borough.Godot</c> is play mode and
/// lenient, because refusing a cross-Ruleset load would mean every patch bricks every save, which is
/// how a city-builder loses its players. <c>Borough.Headless</c> is replay mode and strict, because
/// it exists to produce comparable numbers. This type is the strict half, and it lives in the shell
/// that holds the policy rather than in the format that holds the hash.
/// </para>
/// <para>
/// <b>The polarity is the opposite of the flag <c>plans/0008</c> sketched.</b> That listed
/// <c>--strict</c> as an opt-in, which implies a lenient default — and <c>05 §7</c> denies there is
/// one. So refusal is the default and <c>--force-ruleset</c> is the escape. The escape exists
/// because there is a real question on the other side of it, <em>how far does this Ruleset change
/// move the city</em>, and no reason to make somebody re-record a log to ask it. What it must not do
/// is produce numbers that look comparable, which is what the mark is for.
/// </para>
/// </remarks>
internal readonly record struct RulesetCheck
{
    private RulesetCheck(bool allowed, bool hashBroken, string? refusal)
    {
        Allowed = allowed;
        HashBroken = hashBroken;
        Refusal = refusal;
    }

    /// <summary>Whether the run may proceed.</summary>
    public bool Allowed { get; }

    /// <summary>
    /// Whether the trace must be stamped, because it is not comparable to anything.
    /// </summary>
    /// <remarks>
    /// <c>05 §7</c> marks a save loaded across an unaccounted mismatch permanently hash-broken, and
    /// the mark propagates to everything descended from it. A trace earns the same treatment for the
    /// same reason: without it, a divergence report eventually arrives for numbers that were never
    /// comparable, and it costs days.
    /// </remarks>
    public bool HashBroken { get; }

    /// <summary>What to tell the operator. Null when the run is allowed and unmarked.</summary>
    public string? Refusal { get; }

    /// <summary>
    /// Checks the Ruleset a session names against the one supplied on the command line.
    /// </summary>
    /// <param name="recorded">The content hash the session was recorded against.</param>
    /// <param name="supplied">The content hash of the Ruleset given, or <c>None</c> if none was.</param>
    /// <param name="path">The path given, or null. A null path is the unverifiable case, not a match.</param>
    /// <param name="force">Whether the operator asked to run anyway.</param>
    public static RulesetCheck Against(ulong recorded, ulong supplied, string? path, bool force)
    {
        if (supplied == recorded)
        {
            return new RulesetCheck(allowed: true, hashBroken: false, refusal: null);
        }

        // The unverifiable case, and the one worth getting right. A log naming a Ruleset nobody
        // supplied is not a mismatch — it is a match nothing can confirm, which is the same thing
        // from the far side: the run either was or was not against the right Rules and the runner
        // cannot say which. 05 §7's word is *unaccounted*, and this is the shape of it that arrives
        // first. Nothing triggers it before slice 8, when a Ruleset first has content.
        string why = path is null
            ? Text($"""
                   this session names Ruleset 0x{recorded:X16} and no --ruleset was given,
                   so the run cannot be shown to be against the Rules it was recorded against.
                   """)
            : Text($"""
                   this session names Ruleset 0x{recorded:X16} and {path}
                   hashes to 0x{supplied:X16}. A different Ruleset is a different simulation and
                   the trace would diverge -- which is arithmetic, not a bug.
                   """);

        return force
            ? new RulesetCheck(allowed: true, hashBroken: true, refusal: null)
            : new RulesetCheck(
                allowed: false,
                hashBroken: true,
                refusal: why
                    + "\n05 section 7. Refusing rather than producing a trace nobody can trust."
                    + "\nPass --force-ruleset to run anyway; the trace is stamped hash-broken.");
    }

    private static string Text(FormattableString message) =>
        message.ToString(CultureInfo.InvariantCulture);
}
