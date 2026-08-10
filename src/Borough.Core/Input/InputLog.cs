using Borough.Core.Quantities;

namespace Borough.Core.Input;

/// <summary>
/// The record that fully determines a session: a world seed, a configuration, a Ruleset content hash,
/// and the player's commands per Tick.
/// </summary>
/// <remarks>
/// <para>
/// <b>Exactly that, and nothing more.</b> There is no camera input to record, because
/// <c>adr/0007</c> derives Fidelity from Stress rather than from the camera and <c>adr/0002</c>
/// removed the simulation from the camera in the other direction. An earlier design made the focus
/// point an input precisely so it could be replayed; making fidelity a consequence of the simulation
/// rather than of observation deleted the input altogether.
/// </para>
/// <para>
/// <b>A ten-hour session is kilobytes</b>, because a player issues a handful of commands a minute
/// against sixteen Ticks a second. That is not a pleasant surprise, it is a design check: if this
/// artefact were megabytes, something that is not a player command would have got into it. <b>A bug
/// report is an attachment.</b>
/// </para>
/// <para>
/// <b>Commands are held as two parallel arrays sorted by Tick, and looked up by binary search.</b>
/// The obvious structure is a map from Tick to a list, and it is banned (<c>adr/0003</c>): walking a
/// hash map is order-dependent across runs, and per-Tick lists would be a collection per Tick in a
/// project whose rule is that nothing grows with elapsed time. A sorted array is also simply the
/// better structure at these sizes — the same argument <c>ResourceMap</c> makes.
/// </para>
/// <para>
/// <b>There is no on-disk format here.</b> <c>02 §1</c> gives the core no filesystem, and the log's
/// text encoding spells verbs out in words — so the codec lives in the shell, which is where
/// <c>adr/0002</c> puts every string a human reads.
/// </para>
/// </remarks>
public sealed class InputLog
{
    /// <summary>
    /// The log format's version, written first and read first.
    /// </summary>
    /// <remarks>
    /// <b>Bump this whenever a field is added to a <see cref="Command"/>, to
    /// <see cref="WorldConfiguration"/>, or to the header.</b> A log is the artefact a bug report is
    /// made of, so it will outlive the build that wrote it; a reader that cannot tell which format it
    /// is holding will misread an old log rather than refuse it, and a misread log replays as a
    /// divergence with no cause.
    /// </remarks>
    public const int FormatVersion = 1;

    private readonly ulong[] _ticks;
    private readonly Command[] _commands;
    private readonly RulesetTransition[] _transitions;

    internal InputLog(
        ulong seed,
        WorldConfiguration configuration,
        ulong rulesetHash,
        ulong[] ticks,
        Command[] commands,
        RulesetTransition[] transitions)
    {
        Seed = seed;
        Configuration = configuration;
        RulesetHash = rulesetHash;
        _ticks = ticks;
        _commands = commands;
        _transitions = transitions;
    }

    /// <summary>The world seed, as authored. <see cref="Determinism.WorldKey"/> folds it.</summary>
    public ulong Seed { get; }

    /// <summary>The world-creation settings.</summary>
    public WorldConfiguration Configuration { get; }

    /// <summary>
    /// The content hash of the Ruleset this session <b>opened</b> with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The content, never the name or the path.</b> A replay run against a different Ruleset is a
    /// different simulation and will diverge — which is arithmetic rather than a bug — so the runner
    /// refuses it in <c>--strict</c> instead of reporting a divergence it caused itself.
    /// </para>
    /// <para>
    /// <b>The opening one specifically, since slice 8.</b> A session may reload, so this stopped being
    /// <em>the</em> Ruleset and became the first of several — which is why
    /// <see cref="RulesetHashAt(Ticks)"/> exists and why nothing outside a header should read this.
    /// </para>
    /// </remarks>
    public ulong RulesetHash { get; }

    /// <summary>How many times the Ruleset changed during this session.</summary>
    public int TransitionCount => _transitions.Length;

    /// <summary>The <paramref name="index"/>-th reload, in Tick order.</summary>
    public RulesetTransition Transition(int index) => _transitions[index];

    /// <summary>The number of commands in the log, across all Ticks.</summary>
    public int Count => _commands.Length;

    /// <summary>
    /// The Tick after the last one carrying a command. Zero for an empty log.
    /// </summary>
    /// <remarks>
    /// A replay may of course run past this: a city keeps running after the player stops issuing
    /// commands, and most of a long-run test is exactly that.
    /// </remarks>
    public Ticks Horizon => new(_ticks.Length == 0 ? 0 : _ticks[^1] + 1);

    /// <summary>The commands issued on one Tick, in issue order. Empty for almost every Tick.</summary>
    public ReadOnlySpan<Command> At(Ticks tick)
    {
        int first = LowerBound(tick.Raw);

        int last = first;
        while (last < _ticks.Length && _ticks[last] == tick.Raw)
        {
            last++;
        }

        return _commands.AsSpan(first, last - first);
    }

    /// <summary>The <paramref name="index"/>-th command, and the Tick it was issued on.</summary>
    /// <remarks>
    /// <b>Log order, which is issue order, and is not the same question <see cref="At"/> answers.</b>
    /// Replay walks Ticks and asks what happened on each; a codec walks the log and writes it down.
    /// The second cannot be built from the first, because a log that skips a thousand idle Ticks
    /// between two commands would have to be searched rather than read.
    /// </remarks>
    public (Ticks Tick, Command Command) Entry(int index) =>
        (new Ticks(_ticks[index]), _commands[index]);

    /// <summary>
    /// The Ruleset in force on one Tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The signature was written in slice 5 and stopped discarding its argument in slice 8</b>,
    /// which is the whole reason it was written early: a stub with the right shape meant that nothing
    /// had to learn a new question when reload arrived. <c>Replay.Trace</c> has been calling this
    /// every Tick since slice 5 and now drives reloads by doing so, with no call site changed.
    /// </para>
    /// <para>
    /// <b>The hash is what travels here; the content travels in the crash artifact.</b> An Input Log
    /// is shared between people who have the Rulesets in a repository, and an artifact is attached to
    /// an issue by somebody who may not.
    /// </para>
    /// <para>
    /// <b>A linear walk backwards rather than a binary search, and the asymmetry with
    /// <see cref="At(Ticks)"/> is deliberate.</b> Commands number in the thousands and are searched;
    /// transitions number in the handful, because each one is a designer saving a file. A binary
    /// search over three entries is slower and harder to read than the loop, and inventing one here
    /// would suggest a scale this list will never reach.
    /// </para>
    /// </remarks>
    public ulong RulesetHashAt(Ticks tick)
    {
        for (int i = _transitions.Length - 1; i >= 0; i--)
        {
            if (_transitions[i].Tick.Raw <= tick.Raw)
            {
                return _transitions[i].To;
            }
        }

        return RulesetHash;
    }

    /// <summary>The index of the first command at or after <paramref name="tick"/>.</summary>
    private int LowerBound(ulong tick)
    {
        int low = 0;
        int high = _ticks.Length;

        while (low < high)
        {
            // A constant shift rather than a divide: BOR0203 bans raw `/` outside the arithmetic
            // namespace, and a midpoint over non-negative indices is exactly the case where the
            // shift and the divide agree.
            int middle = low + ((high - low) >> 1);

            if (_ticks[middle] < tick)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
