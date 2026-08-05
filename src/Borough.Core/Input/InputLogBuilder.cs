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

    private ulong _lastTick;

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

    /// <summary>Freezes what has been appended into a log.</summary>
    public InputLog Build() =>
        new(Seed, Configuration, RulesetHash, [.. _ticks], [.. _commands]);
}
