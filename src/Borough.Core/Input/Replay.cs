using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

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
    /// <summary>
    /// Builds the world a log describes, with a Ruleset in force.
    /// </summary>
    /// <param name="log">The session to reproduce.</param>
    /// <param name="rules">
    /// The Rules to run under, already validated (<c>adr/0048</c>). Pass <see cref="Ruleset.Empty"/>
    /// for a session with no Rules — <b>required rather than defaulted</b>, so that running without
    /// Rules is something a caller says rather than something it inherits.
    /// </param>
    /// <remarks>
    /// <b>The Ruleset is the one thing a replay needs that the log does not carry, and that is not
    /// the exception to this class's rule that it looks like.</b> The paragraph above says world state
    /// may never arrive from outside the log. A Ruleset is not world state — it is the interpreter the
    /// state is run through — and the log carries its <em>content hash</em> precisely so that supplying
    /// the wrong one is loud rather than silent. <c>Borough.Headless</c>'s <c>RulesetCheck</c> is where
    /// that refusal lives, because <c>05 §7</c> makes the policy the shell's and not the format's.
    /// <para>
    /// What this does mean is that a log is not self-sufficient: reproducing a session needs the log
    /// <em>and</em> the Ruleset it names. <c>02 §4.3</c> already says so for hot reload — <em>"a replay
    /// needs the Rules' content, not the news that they changed"</em> — and carrying content rather
    /// than a hash is slice 8's problem, not this one's.
    /// </para>
    /// </remarks>
    public static Simulation Start(InputLog log, Ruleset rules)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(rules);

        return Start(log, RulesetCatalogue.Fixed(log.RulesetHash, rules));
    }

    /// <inheritdoc cref="Start(InputLog, Ruleset)"/>
    /// <param name="log">The session to reproduce.</param>
    /// <param name="rulesets">
    /// Every Ruleset the session was played against, by content hash, opening one first.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>A session that reloaded twice was played against three Rulesets</b>, which is the sentence
    /// <c>InputLog.cs:131</c> wrote down before anything could act on it. The one-Ruleset overload
    /// still exists and is still right for the overwhelming majority of sessions; what it now does is
    /// build a catalogue of one, so a log carrying a transition it cannot resolve is <b>refused</b>
    /// rather than replayed against the wrong Rules.
    /// </para>
    /// <para>
    /// <b>That refusal is an improvement rather than a new obligation.</b> Before slice 8 the same
    /// log would have replayed silently under its opening Ruleset and diverged — arithmetic rather
    /// than a bug, and indistinguishable from one.
    /// </para>
    /// </remarks>
    public static Simulation Start(InputLog log, RulesetCatalogue rulesets)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(rulesets);

        if (rulesets.OpeningHash != log.RulesetHash)
        {
            // The catalogue's opening entry is position 0 and the world is built from it, so a caller
            // holding exactly the right Rulesets in the wrong order would build a city under Rules
            // this log never named -- and Tick 0 would then *establish* that hash rather than swap
            // away from it, because the first Tick opens rather than reloads. Both Rulesets load,
            // both run, and nothing says which one the numbers came from. Refused rather than sorted:
            // a shell knows which file the operator meant and this class does not.
            throw new ArgumentException(
                $"this session opens on Ruleset 0x{log.RulesetHash:X16} and the catalogue opens on "
                + $"0x{rulesets.OpeningHash:X16}. A catalogue's first entry is the one the world is "
                + "built with, so the log's opening Ruleset has to be it.",
                nameof(rulesets));
        }

        WorldKey key = WorldKey.FromSeed(log.Seed);
        var world = new World(log.Configuration.Citizens, rulesets.Opening, key);

        return new Simulation(world, key, rulesets);
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
    public static ulong[] Run(InputLog log, Ticks ticks, int hashEvery) =>
        Run(log, ticks, hashEvery, Ruleset.Empty);

    /// <inheritdoc cref="Run(InputLog, Ticks, int)"/>
    /// <param name="log">The session to reproduce.</param>
    /// <param name="ticks">How many Ticks to run. May exceed the log's <see cref="InputLog.Horizon"/>.</param>
    /// <param name="hashEvery">The sampling cadence, in Ticks. Must be positive.</param>
    /// <param name="rules">The Rules to run under.</param>
    /// <remarks>
    /// <b>The three-argument form defaults to <see cref="Ruleset.Empty"/> and
    /// <see cref="Start(InputLog, Ruleset)"/> deliberately does not, which is not an inconsistency.</b>
    /// <c>Start</c> is the door the runner goes through, where a silently ruleless world would be a
    /// session that reported success having simulated nothing — the shape this project has found
    /// several times. <c>Run</c> is a convenience whose callers are almost all asking whether two
    /// replays of one log agree, a question no Ruleset changes.
    /// </remarks>
    public static ulong[] Run(InputLog log, Ticks ticks, int hashEvery, Ruleset rules)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(rules);

        return Run(log, ticks, hashEvery, RulesetCatalogue.Fixed(log.RulesetHash, rules));
    }

    /// <inheritdoc cref="Run(InputLog, Ticks, int, Ruleset)"/>
    /// <param name="log">The session to reproduce.</param>
    /// <param name="ticks">How many Ticks to run.</param>
    /// <param name="hashEvery">The sampling cadence, in Ticks. Must be positive.</param>
    /// <param name="rulesets">
    /// Every Ruleset the session was played against, opening one first — because a session that
    /// reloaded twice was played against three.
    /// </param>
    public static ulong[] Run(InputLog log, Ticks ticks, int hashEvery, RulesetCatalogue rulesets)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(rulesets);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hashEvery);

        Simulation simulation = Start(log, rulesets);
        var trace = new List<ulong>();

        Trace(simulation, log, ticks, hashEvery, trace);

        return [.. trace];
    }

    /// <summary>
    /// Advances a running Simulation through a log, appending a State Hash on every cadence boundary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separated from <see cref="Run"/> so that the headless runner can own the Simulation it is
    /// stepping — the crash artifact needs to name the Tick a panic landed on, and it cannot do that
    /// from a method that owns the loop and returns only on success.
    /// </para>
    /// <para>
    /// <b>The Census shares the hash's cadence rather than carrying its own.</b> Both answer
    /// <em>what did this run look like at Tick N</em>, and a reading taken on a different schedule
    /// from the hash beside it would make the two uncomparable for no gain — the first question asked
    /// of a diverging trace is what the city was doing at the sample before it.
    /// </para>
    /// </remarks>
    /// <param name="census">
    /// Where to record collection sizes, or null to record none. Optional because the property it
    /// serves is about the length of a run, and most callers of this method are not one.
    /// </param>
    public static void Trace(
        Simulation simulation,
        InputLog log,
        Ticks ticks,
        int hashEvery,
        List<ulong> trace,
        Census? census = null)
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
                census?.Observe(simulation);
            }
        }
    }
}
