using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Input;

/// <summary>
/// Slice 8 task 2: a reload is a transition in the Input Log, and <c>RulesetHashAt</c> stops
/// discarding its argument.
/// </summary>
/// <remarks>
/// <para>
/// <b>The signature was written in slice 5 for exactly this day.</b> <c>RulesetHashAt(Ticks)</c> has
/// answered <em>the one Ruleset</em> and thrown its argument away since it was written, and
/// <c>Replay.Trace</c> has called it every Tick throughout. Filling it in means a log drives a reload
/// with **no call site changed**, which is what a stub with the right shape buys and why it was worth
/// writing before it could be implemented.
/// </para>
/// <para>
/// <b>The <c>from</c> hash is redundant to replay and is written anyway.</b> The <c>to</c> hashes
/// alone reproduce a session. Carrying <c>from</c> means a log that has been hand-edited, truncated
/// or spliced is caught at parse time, by name and with a line number, rather than as a State Hash
/// divergence with no cause — which is the diagnostic dead end the whole format exists to abolish.
/// </para>
/// <para>
/// <b>The format version did not move, and that is the finding rather than an omission.</b> The
/// codec's stated rule was <em>bump whenever a field is added to the header</em>, and these are
/// header lines. The rule was a proxy for the property that matters — <em>bump when an old reader
/// would <b>misread</b> a new log</em> — and the proxy has now been wrong twice in the same
/// direction. Neither direction misreads here, so a bump would have cost every log ever written,
/// including the committed golden baseline, to answer a question no reader was going to get wrong.
/// </para>
/// </remarks>
public sealed class RulesetTransitionLogTests
{
    private const ulong Seed = 0x0B07_0000_0000_0EA1UL;
    private const ulong HashA = 0x1111_1111_1111_1111UL;
    private const ulong HashB = 0x2222_2222_2222_2222UL;
    private const ulong HashC = 0x3333_3333_3333_3333UL;

    private static InputLogBuilder Opening() =>
        new(Seed, new WorldConfiguration(1_000), HashA);

    // ---- What RulesetHashAt now answers ----

    /// <summary>A log with no reloads answers its opening hash for every Tick, as it always did.</summary>
    [Fact]
    public void A_log_with_no_reloads_answers_the_opening_hash()
    {
        InputLog log = Opening().Build();

        Assert.Equal(0, log.TransitionCount);
        Assert.Equal(HashA, log.RulesetHashAt(new Ticks(0)));
        Assert.Equal(HashA, log.RulesetHashAt(new Ticks(1_000_000)));
    }

    /// <summary>
    /// The Ruleset in force changes <b>on</b> the transition's Tick, not after it.
    /// </summary>
    /// <remarks>
    /// The boundary is the whole content of the answer, and off-by-one here would be invisible: the
    /// city would reload one Tick late and every hash after it would differ, with nothing naming the
    /// cause. It is also the log's half of <c>Simulation</c>'s swap-then-commands ordering — the two
    /// have to agree that a Tick has one Ruleset and that it is this one.
    /// </remarks>
    [Fact]
    public void The_new_ruleset_is_in_force_on_the_transitions_own_tick()
    {
        InputLog log = Opening().Reload(new Ticks(50), HashB).Build();

        Assert.Equal(HashA, log.RulesetHashAt(new Ticks(49)));
        Assert.Equal(HashB, log.RulesetHashAt(new Ticks(50)));
        Assert.Equal(HashB, log.RulesetHashAt(new Ticks(51)));
    }

    /// <summary>Several reloads answer from the last one at or before the Tick asked about.</summary>
    [Fact]
    public void Several_reloads_answer_from_the_last_one_that_has_happened()
    {
        InputLog log = Opening()
            .Reload(new Ticks(50), HashB)
            .Reload(new Ticks(90), HashC)
            .Reload(new Ticks(120), HashA)
            .Build();

        Assert.Equal(HashA, log.RulesetHashAt(new Ticks(0)));
        Assert.Equal(HashB, log.RulesetHashAt(new Ticks(50)));
        Assert.Equal(HashB, log.RulesetHashAt(new Ticks(89)));
        Assert.Equal(HashC, log.RulesetHashAt(new Ticks(90)));
        Assert.Equal(HashA, log.RulesetHashAt(new Ticks(500)));
    }

    /// <summary>Each transition names both ends, and the chain is built rather than supplied.</summary>
    [Fact]
    public void A_transition_carries_both_hashes_and_the_chain_is_derived()
    {
        InputLog log = Opening()
            .Reload(new Ticks(50), HashB)
            .Reload(new Ticks(90), HashC)
            .Build();

        Assert.Equal(new RulesetTransition(new Ticks(50), HashA, HashB), log.Transition(0));
        Assert.Equal(new RulesetTransition(new Ticks(90), HashB, HashC), log.Transition(1));
    }

    // ---- What the builder refuses ----

    /// <summary>A log is append-only, and a reload before the previous one is not a recording.</summary>
    [Fact]
    public void A_reload_before_the_previous_one_is_refused()
    {
        InputLogBuilder builder = Opening().Reload(new Ticks(90), HashB);

        Assert.Throws<InvalidOperationException>(() => builder.Reload(new Ticks(50), HashC));
    }

    /// <summary>A Tick has exactly one Ruleset, so it carries at most one reload.</summary>
    [Fact]
    public void Two_reloads_on_one_tick_are_refused()
    {
        InputLogBuilder builder = Opening().Reload(new Ticks(90), HashB);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => builder.Reload(new Ticks(90), HashC));

        Assert.Contains("exactly one Ruleset", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A reload on Tick 0 could never have taken effect, so a log may not claim one happened.
    /// </summary>
    /// <remarks>
    /// The opening Ruleset is the header's, and <c>Simulation</c>'s first Tick <em>establishes</em>
    /// what is in force rather than swapping. A log is allowed to record only things that happened.
    /// </remarks>
    [Fact]
    public void A_reload_on_tick_zero_is_refused()
    {
        Assert.Throws<InvalidOperationException>(() => Opening().Reload(new Ticks(0), HashB));
    }

    /// <summary>
    /// Loading the Ruleset already in force is not a transition.
    /// </summary>
    /// <remarks>
    /// A designer saving the same file twice is a common thing to do and produces no change to the
    /// city. Recording it would make the reload count — which exists to say how much tuning a session
    /// contained — report keystrokes instead.
    /// </remarks>
    [Fact]
    public void A_reload_to_the_ruleset_already_in_force_is_refused()
    {
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => Opening().Reload(new Ticks(50), HashA));

        Assert.Contains("already in force", failure.Message, StringComparison.Ordinal);
    }

    // ---- The file ----

    /// <summary>A log with reloads survives a round trip through the file format.</summary>
    [Fact]
    public void Transitions_survive_a_round_trip()
    {
        InputLog written = Opening()
            .Append(new Ticks(1), new Command(CommandKind.Zone, new Tiles(2), new Tiles(3), 1))
            .Reload(new Ticks(50), HashB)
            .Reload(new Ticks(90), HashC)
            .Build();

        InputLog read = InputLogCodec.FromText(InputLogCodec.ToText(written));

        Assert.Equal(2, read.TransitionCount);
        Assert.Equal(written.Transition(0), read.Transition(0));
        Assert.Equal(written.Transition(1), read.Transition(1));
        Assert.Equal(HashC, read.RulesetHashAt(new Ticks(200)));
        Assert.Equal(1, read.Count);
    }

    /// <summary>
    /// <b>A log written before slice 8 still reads, which is why the format version did not move.</b>
    /// </summary>
    /// <remarks>
    /// Written out literally rather than round-tripped, because a round trip through today's writer
    /// would prove only that this build agrees with itself. What has to hold is that a file produced
    /// by a build that had never heard of reloads is still a file this one reproduces exactly.
    /// </remarks>
    [Fact]
    public void A_log_written_before_reloads_existed_still_reads()
    {
        InputLog read = InputLogCodec.FromText(
            "borough-log 1\n"
            + "seed 0x0B07000000000EA1\n"
            + "citizens 1000\n"
            + "ruleset 0x1111111111111111\n"
            + "--\n"
            + "0 populate 0 0 0\n"
            + "3 zone 7 8 1\n");

        Assert.Equal(0, read.TransitionCount);
        Assert.Equal(HashA, read.RulesetHashAt(new Ticks(9_999)));
        Assert.Equal(2, read.Count);
    }

    /// <summary>
    /// <b>A broken chain is caught at parse time, by name and with a line number.</b>
    /// </summary>
    /// <remarks>
    /// This is what the redundant <c>from</c> hash buys, and the only reason it is written. A log
    /// spliced from two sessions parses perfectly without it and replays to a city neither session
    /// ever contained.
    /// </remarks>
    [Fact]
    public void A_reload_whose_from_hash_breaks_the_chain_is_refused()
    {
        FormatException failure = Assert.Throws<FormatException>(() => InputLogCodec.FromText(
            "borough-log 1\n"
            + "seed 0x0B07000000000EA1\n"
            + "citizens 1000\n"
            + "ruleset 0x1111111111111111\n"
            + "reload 50 0x2222222222222222 0x3333333333333333\n"
            + "--\n"));

        Assert.Contains("chain is broken", failure.Message, StringComparison.Ordinal);
        Assert.Contains("line 5", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>An unknown header line is refused by name rather than mistaken for the separator.</summary>
    [Fact]
    public void An_unknown_header_line_is_refused_by_name()
    {
        FormatException failure = Assert.Throws<FormatException>(() => InputLogCodec.FromText(
            "borough-log 1\n"
            + "seed 0x0B07000000000EA1\n"
            + "citizens 1000\n"
            + "ruleset 0x1111111111111111\n"
            + "weather 4 0x0 0x0\n"
            + "--\n"));

        Assert.Contains("'weather'", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>The builder's refusals apply to a parsed log too, with the line attached.</summary>
    [Fact]
    public void A_parsed_reload_on_tick_zero_is_refused_with_its_line()
    {
        FormatException failure = Assert.Throws<FormatException>(() => InputLogCodec.FromText(
            "borough-log 1\n"
            + "seed 0x0B07000000000EA1\n"
            + "citizens 1000\n"
            + "ruleset 0x1111111111111111\n"
            + "reload 0 0x1111111111111111 0x2222222222222222\n"
            + "--\n"));

        Assert.Contains("line 5", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Tick 0", failure.Message, StringComparison.Ordinal);
    }

    // ---- End to end ----

    /// <summary>
    /// <b>A log drives a reload with no call site changed, which is what the slice-5 stub bought.</b>
    /// </summary>
    /// <remarks>
    /// <c>Replay.Trace</c> has called <c>RulesetHashAt</c> every Tick since slice 5. Nothing in it
    /// moved for this test to pass; the method simply started answering the question it was asked.
    /// </remarks>
    [Fact]
    public void A_logged_reload_reaches_the_running_simulation()
    {
        Ruleset slow = Producing(rate: 64);
        Ruleset fast = Producing(rate: 4);

        InputLog log = new InputLogBuilder(Seed, new WorldConfiguration(1_000), HashA)
            .Reload(new Ticks(64), HashB)
            .Build();

        Simulation simulation = Replay.Start(
            log, RulesetCatalogue.Of([HashA, HashB], [slow, fast]));

        var trace = new List<ulong>();
        Replay.Trace(simulation, log, new Ticks(256), hashEvery: 64, trace);

        Assert.Equal(1, simulation.Reloads);
        Assert.Equal(HashB, simulation.RulesetInForce);
        Assert.Same(fast, simulation.World.Rules);
    }

    /// <summary>
    /// <b>A log carrying a transition this session cannot resolve is refused, not replayed.</b>
    /// </summary>
    /// <remarks>
    /// Before slice 8 the same log replayed silently under its opening Ruleset and diverged —
    /// arithmetic rather than a bug, and indistinguishable from one. The one-Ruleset
    /// <c>Replay.Start</c> overload now builds a catalogue of one, so this is the refusal rather than
    /// a new obligation on its callers.
    /// </remarks>
    [Fact]
    public void A_log_that_reloads_replayed_against_one_ruleset_is_refused()
    {
        Ruleset slow = Producing(rate: 64);

        InputLog log = new InputLogBuilder(Seed, new WorldConfiguration(1_000), HashA)
            .Reload(new Ticks(8), HashB)
            .Build();

        Simulation simulation = Replay.Start(log, slow);
        var trace = new List<ulong>();

        Assert.Throws<InvalidOperationException>(
            () => Replay.Trace(simulation, log, new Ticks(64), hashEvery: 64, trace));
    }

    private static Ruleset Producing(uint rate) =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    1, rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 0, 0, 1, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [],
            outputs: [new Term(new BinRef(Scope.Local, new ResourceId(1)), 1)],
            emissions: [],
            bins: [new BinDeclaration(new ResourceId(1), BinCapacity.Of(1_000_000))],
            kindRules: [new RuleId(1)],
            zoneRules: []);
}
