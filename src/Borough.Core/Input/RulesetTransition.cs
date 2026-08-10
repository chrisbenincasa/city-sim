using Borough.Core.Quantities;

namespace Borough.Core.Input;

/// <summary>
/// One hot reload, as it appears in an Input Log: the Tick it took effect on, and both hashes.
/// </summary>
/// <remarks>
/// <para>
/// <b>A transition rather than an event, and both hashes rather than one</b> (<c>02 §4.3</c>). The
/// <see cref="To"/> hash alone would be enough to replay the session, because the Ruleset in force
/// before a transition is whatever the previous one left. Carrying <see cref="From"/> as well makes
/// every line of the log self-describing, and — the reason it is actually here — lets a reader
/// <em>verify the chain</em>: a transition whose <c>from</c> disagrees with what preceded it is a log
/// that has been edited or truncated, and finding that at parse time is much better than discovering
/// it as a State Hash divergence with no cause.
/// </para>
/// <para>
/// <b>The hash travels; the content does not.</b> An Input Log is shared between people who have the
/// Rulesets in a repository. A crash artifact is attached to an issue by somebody who may not, which
/// is why <c>05 §7</c> makes that the artefact carrying content. The two are different jobs and the
/// design has said so since slice 5 — <c>InputLog.cs:136</c>, written before anything could act on it.
/// </para>
/// </remarks>
/// <param name="Tick">The Tick the new Ruleset is in force for, commands included.</param>
/// <param name="From">The content hash that was in force up to the Tick before.</param>
/// <param name="To">The content hash in force from this Tick on.</param>
public readonly record struct RulesetTransition(Ticks Tick, ulong From, ulong To);
