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

    internal InputLog(
        ulong seed,
        WorldConfiguration configuration,
        ulong rulesetHash,
        ulong[] ticks,
        Command[] commands)
    {
        Seed = seed;
        Configuration = configuration;
        RulesetHash = rulesetHash;
        _ticks = ticks;
        _commands = commands;
    }

    /// <summary>The world seed, as authored. <see cref="Determinism.WorldKey"/> folds it.</summary>
    public ulong Seed { get; }

    /// <summary>The world-creation settings.</summary>
    public WorldConfiguration Configuration { get; }

    /// <summary>
    /// The content hash of the Ruleset this session was played against.
    /// </summary>
    /// <remarks>
    /// <b>The content, never the name or the path.</b> A replay run against a different Ruleset is a
    /// different simulation and will diverge — which is arithmetic rather than a bug — so the runner
    /// refuses it in <c>--strict</c> instead of reporting a divergence it caused itself.
    /// </remarks>
    public ulong RulesetHash { get; }

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
    /// <b>A stub with the right shape, which is the point of writing it now.</b> Slice 8 makes the
    /// Ruleset hot-reloadable, at which point a reload becomes a transition in this log carrying
    /// <em>both</em> hashes. Until then one Ruleset is in force for the whole run and this returns it.
    /// </para>
    /// <para>
    /// <b>This comment used to claim that <em>"what does not change is the log format or any caller"</em>,
    /// and that is wrong</b> (<c>adr/0048</c>). The log gains reload transitions, so the format changes
    /// and so does every caller that supplies a Ruleset — <c>--ruleset PATH</c> names one file and a
    /// session that reloaded twice was played against three. What stays true is the narrower claim this
    /// method was written for: the <em>signature</em> is right, so nothing has to learn a new question.
    /// </para>
    /// <para>
    /// <b>The hash is what travels here; the content travels in the crash artifact.</b> An Input Log is
    /// shared between people who have the Rulesets in a repository, and an artifact is attached to an
    /// issue by somebody who may not.
    /// </para>
    /// </remarks>
    public ulong RulesetHashAt(Ticks tick)
    {
        _ = tick;
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
