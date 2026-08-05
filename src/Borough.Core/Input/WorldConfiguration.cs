namespace Borough.Core.Input;

/// <summary>
/// The world-creation settings a session was started with — the <em>configuration</em> of
/// <c>CONTEXT.md</c>'s <c>(world seed, configuration, inputs per Tick)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the half of world creation that is not the seed.</b> It is a struct rather than a
/// parameter list because it grows: <c>TICKS_PER_DAY</c>, <c>WHEEL_SIZE</c> and the Cell size are all
/// world-creation constants baked into the save and not hot-reloadable (<c>02 §1.2</c>), and each
/// joins this type when the mechanism that reads it exists. Adding a field here is a log format
/// change, which is why the format carries a version.
/// </para>
/// <para>
/// <b><see cref="Citizens"/> is a capacity and not a population</b>, and it is the one field that
/// provably cannot reach the State Hash — slice 4 has a test to that effect. It is in the log anyway,
/// because a replay that allocated different array sizes would exercise different growth paths, and
/// "cannot change the hash" is a claim worth being able to re-check rather than one to lean on.
/// </para>
/// </remarks>
public readonly struct WorldConfiguration
{
    /// <param name="citizens">Initial Citizen capacity. Every other table is sized from it.</param>
    public WorldConfiguration(int citizens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(citizens);
        Citizens = citizens;
    }

    /// <summary>Initial Citizen capacity. Every other table is sized from it.</summary>
    public int Citizens { get; }
}
