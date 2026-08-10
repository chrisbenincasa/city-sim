using System.Globalization;
using Borough.Core.Determinism;
using Borough.Core.Input;

namespace Borough.Headless;

/// <summary>One Ruleset the operator named, and what it hashes to.</summary>
/// <remarks>
/// <b>The path travels with the hash because the refusal is written for a person.</b> A message
/// naming three hashes and no filenames is technically complete and practically useless — the
/// operator's next action is to open a file, and the runner is the only thing in the stack that
/// knows which file is which.
/// </remarks>
/// <param name="Path">The path as it was given on the command line.</param>
/// <param name="Hash">Its content hash.</param>
internal readonly record struct Supplied(string Path, ulong Hash);

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
    private RulesetCheck(bool allowed, bool hashBroken, string? refusal, ulong inForce)
    {
        Allowed = allowed;
        HashBroken = hashBroken;
        Refusal = refusal;
        InForce = inForce;
    }

    /// <summary>Whether the run may proceed.</summary>
    public bool Allowed { get; }

    /// <summary>
    /// The content hash of the Ruleset actually loaded, which is not always the one the log names.
    /// </summary>
    /// <remarks>
    /// <b>What the run opens on, which is not always what the log wanted.</b> On a clean check the two
    /// agree; on a forced one this is the first Ruleset supplied, and a run given none is genuinely
    /// running against no Rules. <b>It is the <em>opening</em> hash and not the one in force at the
    /// end</b> — once a session can reload, those differ, and the crash artifact takes the live one
    /// from <see cref="Borough.Core.Simulation.RulesetInForce"/> instead.
    /// </remarks>
    public ulong InForce { get; }

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
    /// Checks every Ruleset a session names against the ones supplied on the command line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A session names more than one Ruleset once it can reload</b>, so this checks the opening
    /// hash <em>and</em> every transition's destination. `InputLog.cs:131` wrote the problem down in
    /// slice 5: <em>"--ruleset PATH names one file and a session that reloaded twice was played
    /// against three."</em>
    /// </para>
    /// <para>
    /// <b><c>--force-ruleset</c> waives a mismatch and cannot waive an absence, which is the third
    /// instance of one distinction in this runner.</b> A malformed Ruleset is refused unconditionally
    /// because force cannot make Rules readable; an unaccounted <em>transition</em> is refused for the
    /// same reason turned one notch — there is no Ruleset to run those Ticks against, so there is
    /// nothing for force to authorise. What force means is <em>run these Rules under this log anyway</em>,
    /// and that sentence needs Rules.
    /// </para>
    /// </remarks>
    /// <param name="log">The session, which names its opening Ruleset and every transition.</param>
    /// <param name="supplied">The Rulesets given on the command line, in the operator's order.</param>
    /// <param name="force">Whether the operator asked to run anyway.</param>
    public static RulesetCheck Against(InputLog log, IReadOnlyList<Supplied> supplied, bool force)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(supplied);

        // An absence is checked before a mismatch, and the order is the operator's rather than the
        // machine's. Given both a wrong opening Ruleset and a reload nobody supplied, "you do not have
        // the Rules this session reloads into" is the sentence that survives fixing the other one.
        for (int i = 0; i < log.TransitionCount; i++)
        {
            RulesetTransition transition = log.Transition(i);

            if (Index(supplied, transition.To) < 0)
            {
                return new RulesetCheck(
                    allowed: false,
                    hashBroken: true,
                    inForce: Opening(supplied),
                    refusal: Text($"""
                           this session reloads at Tick {transition.Tick.Raw} to Ruleset
                           0x{transition.To:X16}, and no --ruleset supplied it. A session that
                           reloaded twice was played against three Rulesets and needs all three.
                           """)
                        + "\n05 section 7. --ruleset repeats, and order does not matter."
                        + "\n--force-ruleset cannot waive this: it waives a mismatch, and Rules"
                        + " nobody has are not a mismatch.");
            }
        }

        // A session that names no Ruleset run with no Ruleset is a match rather than a hole: it is
        // genuinely running against nothing, which is what every figure recorded before slice 7 was
        // taken against. Nothing to verify, so nothing to mark.
        if (supplied.Count == 0 && log.RulesetHash == ContentHash.None)
        {
            return new RulesetCheck(
                allowed: true, hashBroken: false, refusal: null, inForce: ContentHash.None);
        }

        if (Index(supplied, log.RulesetHash) >= 0)
        {
            return new RulesetCheck(
                allowed: true, hashBroken: false, refusal: null, inForce: log.RulesetHash);
        }

        // The unverifiable case, and the one worth getting right. A log naming a Ruleset nobody
        // supplied is not a mismatch — it is a match nothing can confirm, which is the same thing
        // from the far side: the run either was or was not against the right Rules and the runner
        // cannot say which. 05 §7's word is *unaccounted*, and this is the shape of it that arrives
        // first.
        string why = supplied.Count == 0
            ? Text($"""
                   this session names Ruleset 0x{log.RulesetHash:X16} and no --ruleset was given,
                   so the run cannot be shown to be against the Rules it was recorded against.
                   """)
            : Text($"""
                   this session opens on Ruleset 0x{log.RulesetHash:X16} and the {supplied.Count}
                   Ruleset(s) given are {Names(supplied)}. A different Ruleset is a different
                   simulation and the trace would diverge -- which is arithmetic, not a bug.
                   """);

        return force
            ? new RulesetCheck(
                allowed: true, hashBroken: true, refusal: null, inForce: Opening(supplied))
            : new RulesetCheck(
                allowed: false,
                hashBroken: true,
                inForce: Opening(supplied),
                refusal: why
                    + "\n05 section 7. Refusing rather than producing a trace nobody can trust."
                    + "\nPass --force-ruleset to run anyway; the trace is stamped hash-broken.");
    }

    /// <summary>The position of a content hash in what was supplied, or -1.</summary>
    private static int Index(IReadOnlyList<Supplied> supplied, ulong hash)
    {
        for (int i = 0; i < supplied.Count; i++)
        {
            if (supplied[i].Hash == hash)
            {
                return i;
            }
        }

        return -1;
    }

    private static ulong Opening(IReadOnlyList<Supplied> supplied) =>
        supplied.Count == 0 ? ContentHash.None : supplied[0].Hash;

    private static string Names(IReadOnlyList<Supplied> supplied) =>
        string.Join(", ", supplied.Select(entry => Text($"{entry.Path} (0x{entry.Hash:X16})")));

    private static string Text(FormattableString message) =>
        message.ToString(CultureInfo.InvariantCulture);
}
