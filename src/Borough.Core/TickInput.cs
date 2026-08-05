using Borough.Core.Input;

namespace Borough.Core;

/// <summary>
/// Everything entering the simulation on one Tick: the player's commands, and the Ruleset they were
/// issued against.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the whole of <c>inputs</c> in <c>step(inputs)</c>.</b> No wall-clock, no camera, no
/// renderer, no filesystem — <c>02 §1</c> lists those as things the simulation has no concept of, and
/// the way to keep that true is for there to be no parameter through which they could arrive.
/// </para>
/// <para>
/// <b>A <c>ref struct</c>, so the commands are a window rather than a copy.</b> It also means this
/// type cannot become a field of anything, which is the property that keeps a Tick's inputs from
/// being quietly retained past the Tick that received them. <c>Borough.Analysers</c> skips
/// <c>ref struct</c>s under <c>BOR0701</c> for the same structural reason.
/// </para>
/// </remarks>
public readonly ref struct TickInput
{
    /// <param name="commands">The commands issued on this Tick, in the order the player issued them.</param>
    /// <param name="rulesetHash">The content hash of the Ruleset in force.</param>
    public TickInput(ReadOnlySpan<Command> commands, ulong rulesetHash)
    {
        Commands = commands;
        RulesetHash = rulesetHash;
    }

    /// <summary>An empty Tick — the overwhelming majority of them.</summary>
    /// <remarks>
    /// A player issues a handful of commands a minute against 16 Ticks a second, so almost every Tick
    /// carries nothing. That ratio is why a ten-hour session is kilobytes and why a bug report can
    /// carry its own reproduction.
    /// </remarks>
    public static TickInput Empty => default;

    /// <summary>The commands issued on this Tick, in issue order.</summary>
    public ReadOnlySpan<Command> Commands { get; }

    /// <summary>
    /// The content hash of the Ruleset in force on this Tick.
    /// </summary>
    /// <remarks>
    /// <b>Carried per Tick rather than once per session, and that is a format decision made early on
    /// purpose.</b> Hot reload (slice 8) makes the Ruleset change mid-run, and a replay needs the
    /// Rules' <em>content</em> rather than the news that they changed — so a reload is a transition
    /// carrying both hashes, not an event. Slice 5 never varies this within a run; the field exists
    /// now so that the log format does not change when it starts to.
    /// </remarks>
    public ulong RulesetHash { get; }
}
