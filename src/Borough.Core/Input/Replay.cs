using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;

namespace Borough.Core.Input;

/// <summary>
/// Runs an <see cref="InputLog"/> and returns the State Hash trace it produces.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is CI lint 5</b> (<c>05 §4</c>): two runs of one Input Log produce identical State Hash
/// sequences. Everything later in the project is a consumer of it — bisection, save/reload
/// equivalence, the golden-hash regression, and crash forensics, which since <c>adr/0037</c> deleted
/// the double buffer is <em>entirely</em> replay plus the log.
/// </para>
/// <para>
/// <b>The property that makes it work was built in slice 2, not here.</b> Randomness is
/// <c>draw(world_key, entity, tick, purpose)</c> — counter-based, never a stream — so a draw's result
/// does not depend on how many draws preceded it or on what order they ran in. That is also what will
/// let Phase 2 be spread across threads later with no coordination and bit-identical output.
/// </para>
/// <para>
/// <b>A replay starts from an empty world, and every difference between two cities is a difference in
/// their logs.</b> There is no separate initial-state file and there should not be one: the moment
/// world state can arrive from somewhere the log does not describe, the log stops being a complete
/// account of a session and a divergence stops being attributable.
/// </para>
/// </remarks>
public static class Replay
{
    /// <summary>Builds the world a log describes, at Tick zero, before any command has been applied.</summary>
    public static Simulation Start(InputLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        var world = new World(log.Configuration.Citizens);
        return new Simulation(world, WorldKey.FromSeed(log.Seed));
    }

    /// <summary>
    /// Runs a log for <paramref name="ticks"/> Ticks, sampling the State Hash every
    /// <paramref name="hashEvery"/> Ticks.
    /// </summary>
    /// <param name="log">The session to reproduce.</param>
    /// <param name="ticks">How many Ticks to run. May exceed the log's <see cref="InputLog.Horizon"/>.</param>
    /// <param name="hashEvery">The sampling cadence, in Ticks. Must be positive.</param>
    /// <remarks>
    /// <para>
    /// <b>Running past the last command is normal rather than exceptional.</b> A city keeps running
    /// after the player stops acting, and the long-run test is almost entirely that.
    /// </para>
    /// <para>
    /// <b>The cadence is the caller's</b>, which is why Phase 7 emits nothing on its own.
    /// <c>02 §1.1</c> says the Commit phase emits the State Hash <em>if due</em>, and <em>due</em> is
    /// a property of the run rather than of the simulation — a headless bisection wants every Tick and
    /// a balance run wants every thousandth.
    /// </para>
    /// </remarks>
    public static ulong[] Run(InputLog log, Ticks ticks, int hashEvery)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hashEvery);

        Simulation simulation = Start(log);
        var trace = new List<ulong>();

        Trace(simulation, log, ticks, hashEvery, trace);

        return [.. trace];
    }

    /// <summary>
    /// Advances a running Simulation through a log, appending a State Hash on every cadence boundary.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="Run"/> so that the headless runner can own the Simulation it is
    /// stepping — the crash artifact needs to name the Tick a panic landed on, and it cannot do that
    /// from a method that owns the loop and returns only on success.
    /// </remarks>
    public static void Trace(
        Simulation simulation,
        InputLog log,
        Ticks ticks,
        int hashEvery,
        List<ulong> trace)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hashEvery);

        ulong cadence = (ulong)hashEvery;

        for (ulong step = 0; step < ticks.Raw; step++)
        {
            Ticks tick = simulation.Tick;
            var input = new TickInput(log.At(tick), log.RulesetHashAt(tick));

            simulation.Step(input);

            if (simulation.Tick.Raw % cadence == 0)
            {
                trace.Add(simulation.World.HashState());
            }
        }
    }
}
