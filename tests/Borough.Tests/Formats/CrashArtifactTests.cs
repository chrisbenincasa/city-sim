using Borough.Core;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The crash artifact: <c>05 §8</c>'s reproduction of a panic, rather than a dump of one.
/// </summary>
/// <remarks>
/// <b>The property under test is that the file reproduces the crash</b>, so most of these assert
/// against a replay rather than against the bytes. A format that round-trips but replays to a
/// different city would pass every field-by-field check and be worthless — and the failure would
/// surface as a bug that "could not be reproduced", which is the diagnostic dead end this whole
/// slice exists to abolish.
/// </remarks>
public sealed class CrashArtifactTests
{
    private const ulong Seed = 0x0B07_0000_0000_0EA1UL;
    private const ulong Ruleset = 0xABCD_EF01_2345_6789UL;

    /// <summary>
    /// The Tick an artifact names is the Tick that failed, not the one after it.
    /// </summary>
    /// <remarks>
    /// <b>This is the assumption the whole artefact rests on and it is one line of
    /// <c>Simulation.Step</c>:</b> the Tick counter advances after the phases rather than before, so
    /// a phase that throws leaves the Tick naming the failure. An artifact off by one would send its
    /// reader to a Tick where nothing is wrong yet, and the mistake would look like the bug moving.
    /// </remarks>
    [Fact]
    public void The_tick_a_panic_leaves_behind_is_the_tick_that_failed()
    {
        // Connect is encoded by the format and refused by the simulation until slice 7, which makes
        // it the one verb that panics on demand without breaking anything to arrange it.
        InputLog log = Builder()
            .Append(new Ticks(5), new Command(CommandKind.Connect, new Tiles(3), new Tiles(4), 0))
            .Build();

        Simulation simulation = Replay.Start(log, Core.Rules.Ruleset.Empty);

        Assert.Throws<InvalidOperationException>(
            () => Replay.Trace(simulation, log, new Ticks(100), 10, []));

        Assert.Equal(5UL, simulation.Tick.Raw);
    }

    /// <summary>
    /// The claim the artefact makes: replaying it rebuilds the run that crashed, Tick for Tick.
    /// </summary>
    [Fact]
    public void The_log_an_artifact_carries_replays_to_the_run_that_crashed()
    {
        InputLog log = Zoned();

        ulong[] original = Replay.Run(log, new Ticks(200), hashEvery: 20);

        CrashArtifact artifact = Round(CrashArtifact.Of(
            log, new Ticks(37), Ruleset, new InvalidOperationException("something gave way.")));

        ulong[] reproduced = Replay.Run(artifact.Log, new Ticks(200), hashEvery: 20);

        Assert.Equal(original, reproduced);
    }

    [Fact]
    public void An_artifact_round_trips_its_header()
    {
        CrashArtifact artifact = Round(CrashArtifact.Of(
            Zoned(), new Ticks(5_000), Ruleset, new InvalidOperationException("gave way.")));

        Assert.Equal(5_000UL, artifact.Panic.Raw);
        Assert.Equal(0UL, artifact.From.Raw);
        Assert.Equal(Ruleset, artifact.RulesetHash);
        Assert.Contains("InvalidOperationException", artifact.Note, StringComparison.Ordinal);
        Assert.Contains("gave way.", artifact.Note, StringComparison.Ordinal);
    }

    /// <summary>
    /// An invariant violation's ids survive, because they are what a tool can act on.
    /// </summary>
    [Fact]
    public void A_violation_keeps_its_invariant_and_its_rows()
    {
        var broken = new Violation(Invariant.HouseholdIsAnOccupantOfItsHome, new Ticks(42), 17, 3);

        CrashArtifact artifact = Round(CrashArtifact.Of(
            Zoned(), new Ticks(42), Ruleset, new InvariantViolationException(broken)));

        Assert.Equal(Invariant.HouseholdIsAnOccupantOfItsHome, artifact.Violation.Invariant);
        Assert.Equal(17, artifact.Violation.Slot);
        Assert.Equal(3, artifact.Violation.Other);
        Assert.True(artifact.Violation.Broken);
    }

    [Fact]
    public void A_panic_that_is_not_an_invariant_violation_records_none()
    {
        CrashArtifact artifact = Round(CrashArtifact.Of(
            Zoned(), new Ticks(9), Ruleset, new InvalidOperationException("gave way.")));

        Assert.False(artifact.Violation.Broken);
        Assert.Equal(Invariant.None, artifact.Violation.Invariant);
    }

    /// <summary>
    /// A message with a newline in it must not end the field early and take the parse with it.
    /// </summary>
    [Fact]
    public void A_note_spanning_lines_is_collapsed_to_one()
    {
        CrashArtifact artifact = Round(CrashArtifact.Of(
            Zoned(), new Ticks(1), Ruleset, new InvalidOperationException("first\nsecond\n\tthird")));

        Assert.DoesNotContain('\n', artifact.Note);
        Assert.Contains("first second third", artifact.Note, StringComparison.Ordinal);
    }

    /// <summary>
    /// Cutting the file at its separator yields a log this project already reads.
    /// </summary>
    /// <remarks>
    /// The artefact is written when tooling is least trustworthy and read by somebody who did not
    /// produce it. Needing a tool to extract the replayable part would be the wrong dependency at
    /// exactly the wrong moment.
    /// </remarks>
    [Fact]
    public void The_tail_of_the_file_is_a_log_on_its_own()
    {
        InputLog log = Zoned();

        string text = CrashArtifact.ToText(
            CrashArtifact.Of(log, new Ticks(3), Ruleset, new InvalidOperationException("gave way.")));

        int cut = text.IndexOf("--\n", StringComparison.Ordinal);
        InputLog tail = InputLogCodec.FromText(text[(cut + 3)..]);

        Assert.Equal(log.Seed, tail.Seed);
        Assert.Equal(log.Count, tail.Count);
        Assert.Equal(InputLogCodec.ToText(log), InputLogCodec.ToText(tail));
    }

    /// <summary>
    /// The runner takes an artifact where it takes a log, so the file has to say which it is.
    /// </summary>
    [Fact]
    public void An_artifact_and_a_bare_log_are_told_apart_by_the_file()
    {
        InputLog log = Zoned();

        string artifact = CrashArtifact.ToText(
            CrashArtifact.Of(log, new Ticks(3), Ruleset, new InvalidOperationException("gave way.")));

        Assert.True(CrashArtifact.IsCrashArtifact(artifact));
        Assert.False(CrashArtifact.IsCrashArtifact(InputLogCodec.ToText(log)));
    }

    /// <summary>
    /// An artifact that starts from a checkpoint is refused rather than replayed from zero.
    /// </summary>
    /// <remarks>
    /// Checkpoints arrive in milestone 10 and the field is written now so that milestone fills one in
    /// rather than replacing a mechanism. Until then, honouring a non-zero <c>from</c> by ignoring it
    /// would rebuild a different city and blame the difference on the crash.
    /// </remarks>
    [Fact]
    public void An_artifact_reproducing_from_a_checkpoint_is_refused()
    {
        string text = CrashArtifact
            .ToText(CrashArtifact.Of(
                Zoned(), new Ticks(5_000), Ruleset, new InvalidOperationException("gave way.")))
            .Replace("from 0", "from 4096", StringComparison.Ordinal);

        FormatException complaint = Assert.Throws<FormatException>(
            () => CrashArtifact.FromText(text));

        Assert.Contains("checkpoint", complaint.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("borough-log 1\n", "not a borough-crash file")]
    [InlineData("borough-crash 9\n", "format version 9")]
    public void A_file_this_build_cannot_read_is_refused(string first, string expected)
    {
        FormatException complaint = Assert.Throws<FormatException>(
            () => CrashArtifact.FromText(first + "tick 1\nfrom 0\n"));

        Assert.Contains(expected, complaint.Message, StringComparison.Ordinal);
    }

    /// <summary>Every refusal names the line it happened on: this file is read when tooling is thin.</summary>
    [Fact]
    public void A_refusal_names_the_line_it_is_about()
    {
        FormatException complaint = Assert.Throws<FormatException>(
            () => CrashArtifact.FromText("borough-crash 1\ntick 5\nwhen 0\n"));

        Assert.Contains("line 3", complaint.Message, StringComparison.Ordinal);
        Assert.Contains("'from'", complaint.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A log naming no Ruleset, because nothing names one before slice 8 gives a Ruleset content.
    /// </summary>
    private static InputLogBuilder Builder() =>
        new(Seed, new WorldConfiguration(256), rulesetHash: 0);

    /// <summary>A session with something in it, so a reproduction has something to reproduce.</summary>
    private static InputLog Zoned() =>
        Builder()
            .Append(new Ticks(1), new Command(CommandKind.Zone, new Tiles(0), new Tiles(0), 1))
            .Append(new Ticks(9), new Command(CommandKind.Zone, new Tiles(4), new Tiles(2), 3))
            .Append(new Ticks(9), new Command(CommandKind.Zone, new Tiles(-7), new Tiles(5), 2))
            .Build();

    /// <summary>Through the format and back, which is the only way these assertions are worth making.</summary>
    private static CrashArtifact Round(CrashArtifact artifact) =>
        CrashArtifact.FromText(CrashArtifact.ToText(artifact));
}
