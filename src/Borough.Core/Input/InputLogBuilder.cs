using Borough.Core.Quantities;

namespace Borough.Core.Input;

/// <summary>
/// Accumulates commands into an <see cref="InputLog"/>, in Tick order.
/// </summary>
/// <remarks>
/// <para>
/// <b>Append-only, and enforced rather than described.</b> A log is written forwards as a session is
/// played, and the property that makes it a trustworthy artefact is that nothing already written can
/// be revised. Appending at a Tick earlier than the last one throws — a log whose commands are out of
/// order would replay in an order the player never issued, and the resulting divergence would look
/// like a simulation bug rather than a recording one.
/// </para>
/// <para>
/// <b>Several commands may share a Tick, and their relative order is preserved.</b> The player can
/// issue two commands in one Tick and the second may depend on the first, so issue order is content.
/// </para>
/// </remarks>
public sealed class InputLogBuilder
{
    private readonly List<ulong> _ticks = [];
    private readonly List<Command> _commands = [];
    private readonly List<RulesetTransition> _transitions = [];

    private ulong _lastTick;
    private ulong _lastReloadTick;

    /// <param name="seed">The world seed, as authored.</param>
    /// <param name="configuration">The world-creation settings.</param>
    /// <param name="rulesetHash">The content hash of the Ruleset in force.</param>
    public InputLogBuilder(ulong seed, WorldConfiguration configuration, ulong rulesetHash)
    {
        Seed = seed;
        Configuration = configuration;
        RulesetHash = rulesetHash;
    }

    /// <inheritdoc cref="InputLog.Seed"/>
    public ulong Seed { get; }

    /// <inheritdoc cref="InputLog.Configuration"/>
    public WorldConfiguration Configuration { get; }

    /// <inheritdoc cref="InputLog.RulesetHash"/>
    public ulong RulesetHash { get; }

    /// <summary>Records one command at one Tick. The Tick must not precede the previous one.</summary>
    public InputLogBuilder Append(Ticks tick, Command command)
    {
        if (tick.Raw < _lastTick)
        {
            throw new InvalidOperationException(
                $"Tick {tick.Raw} precedes {_lastTick}: an Input Log is append-only.");
        }

        if (command.Kind == CommandKind.None)
        {
            throw new ArgumentException("a command with no verb cannot be logged.", nameof(command));
        }

        _ticks.Add(tick.Raw);
        _commands.Add(command);
        _lastTick = tick.Raw;

        return this;
    }

    /// <summary>The Ruleset in force at the point this builder has reached.</summary>
    /// <remarks>
    /// <b>Derived rather than supplied, which is what makes an inconsistent chain unauthorable.</b>
    /// A <see cref="RulesetTransition"/> carries both hashes so that a reader can verify the chain;
    /// letting a caller supply the <c>from</c> would let it write a chain that does not chain, and a
    /// verification everybody passes by construction is worth more than one anybody can fail.
    /// </remarks>
    public ulong InForce => _transitions.Count == 0 ? RulesetHash : _transitions[^1].To;

    /// <summary>
    /// Records a hot reload: from the Tick given, the session runs under a different Ruleset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Append-only like <see cref="Append(Ticks, Command)"/>, and independently of it.</b> A
    /// transition and a command are two lists over one timeline, so a reload at Tick 50 does not stop
    /// a command being recorded at Tick 40 — that is ordinary, since the two are usually written by
    /// different parts of the shell. What is refused is a reload earlier than the previous reload,
    /// which would make <see cref="InputLog.RulesetHashAt(Ticks)"/>'s backwards walk answer from the
    /// wrong entry.
    /// </para>
    /// <para>
    /// <b>Tick 0 is refused, and it is a real refusal rather than tidiness.</b> The opening Ruleset is
    /// the header's, and <c>Simulation</c>'s first Tick <em>establishes</em> what is in force rather
    /// than swapping — so a transition on Tick 0 could never take effect. A log is allowed to record
    /// only things that happened.
    /// </para>
    /// </remarks>
    /// <param name="tick">The first Tick the new Ruleset is in force for. Must be after Tick 0.</param>
    /// <param name="to">The content hash of the Ruleset being loaded.</param>
    public InputLogBuilder Reload(Ticks tick, ulong to)
    {
        if (tick.Raw == 0)
        {
            throw new InvalidOperationException(
                "a reload on Tick 0 is the opening Ruleset, which is the log's header. The first "
                + "Tick establishes what is in force rather than swapping, so this could never have "
                + "taken effect.");
        }

        if (tick.Raw < _lastReloadTick)
        {
            throw new InvalidOperationException(
                $"Tick {tick.Raw} precedes the reload at {_lastReloadTick}: an Input Log is "
                + "append-only.");
        }

        if (tick.Raw == _lastReloadTick)
        {
            // Two Rulesets in force on one Tick is the thing Simulation's swap-then-commands ordering
            // exists to prevent, and it would make RulesetHashAt(tick) answer from whichever entry
            // the walk happened to reach.
            throw new InvalidOperationException(
                $"Tick {tick.Raw} already carries a reload. A Tick has exactly one Ruleset.");
        }

        ulong from = InForce;

        if (from == to)
        {
            throw new InvalidOperationException(
                $"the reload at Tick {tick.Raw} loads the Ruleset already in force "
                + $"(0x{to:X16}). A transition that changes nothing is not one, and recording it "
                + "would make the reload count say a designer tuned something when they saved the "
                + "same file twice.");
        }

        _transitions.Add(new RulesetTransition(tick, from, to));
        _lastReloadTick = tick.Raw;

        return this;
    }

    /// <summary>Freezes what has been appended into a log.</summary>
    public InputLog Build() =>
        new(Seed, Configuration, RulesetHash, [.. _ticks], [.. _commands], [.. _transitions]);
}
