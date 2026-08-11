using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The Input Log's on-disk codec.
/// </summary>
/// <remarks>
/// <b>The property under test is agreement, not correctness.</b> A log written by
/// <c>Borough.Godot</c> must replay in <c>Borough.Headless</c> (<c>adr/0039</c>), and the failure
/// that matters is not a parse error — it is a log that parses cleanly and replays to a
/// <em>different</em> city. So the round trip is asserted through <see cref="Replay"/> as well as
/// through the fields: two logs with equal contents that produce different traces would be a defect
/// no field-by-field comparison could see.
/// </remarks>
public sealed class InputLogCodecTests
{
    [Fact]
    public void A_log_survives_a_round_trip_field_for_field()
    {
        InputLog original = Log();
        InputLog restored = InputLogCodec.FromText(InputLogCodec.ToText(original));

        Assert.Equal(original.Seed, restored.Seed);
        Assert.Equal(original.Configuration.Citizens, restored.Configuration.Citizens);
        Assert.Equal(original.RulesetHash, restored.RulesetHash);
        Assert.Equal(original.Count, restored.Count);

        for (int i = 0; i < original.Count; i++)
        {
            (Ticks tick, Command command) = original.Entry(i);
            (Ticks restoredTick, Command restoredCommand) = restored.Entry(i);

            Assert.Equal(tick, restoredTick);
            Assert.Equal(command.Kind, restoredCommand.Kind);
            Assert.Equal(command.East, restoredCommand.East);
            Assert.Equal(command.North, restoredCommand.North);
            Assert.Equal(command.Zone, restoredCommand.Zone);
        }
    }

    /// <summary>
    /// The round trip that matters: the restored log builds the same city.
    /// </summary>
    [Fact]
    public void A_log_survives_a_round_trip_hash_for_hash()
    {
        InputLog original = Log();
        InputLog restored = InputLogCodec.FromText(InputLogCodec.ToText(original));

        Assert.Equal(
            Replay.Run(original, new Ticks(64), hashEvery: 1),
            Replay.Run(restored, new Ticks(64), hashEvery: 1));
    }

    /// <summary>
    /// Writing is idempotent, which is what makes a committed log reviewable: a re-emitted log that
    /// differed in whitespace would produce a diff nobody could read past.
    /// </summary>
    [Fact]
    public void Writing_a_parsed_log_reproduces_the_text_it_came_from()
    {
        string text = InputLogCodec.ToText(Log());

        Assert.Equal(text, InputLogCodec.ToText(InputLogCodec.FromText(text)));
    }

    /// <summary>
    /// All four verbs survive the round trip, though Service and Govern are still unapplied — so the
    /// format does not have to change, or its version be bumped, when they arrive.
    /// </summary>
    /// <remarks>
    /// <b>Connect arrived in 5a-bis and the version did not move, which is the claim this line was
    /// written to make in advance</b> (<c>adr/0077</c>). It cost nothing because the verb was
    /// designed to fit the twelve bytes a <c>Command</c> already had rather than the other way round.
    /// </remarks>
    [Theory]
    [InlineData(CommandKind.Zone)]
    [InlineData(CommandKind.Connect)]
    [InlineData(CommandKind.Service)]
    [InlineData(CommandKind.Govern)]
    public void Every_declared_verb_survives_the_round_trip(CommandKind kind)
    {
        InputLogBuilder builder = new(1, new WorldConfiguration(8), rulesetHash: 0);
        builder.Append(new Ticks(3), new Command(kind, new Tiles(5), new Tiles(6), zone: 9));

        InputLog restored = InputLogCodec.FromText(InputLogCodec.ToText(builder.Build()));

        Assert.Equal(kind, restored.Entry(0).Command.Kind);
    }

    /// <summary>
    /// <b>A <c>connect</c> line survives with its payload intact, not merely with its verb.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>Every_declared_verb_survives_the_round_trip</c> asserts the <em>Kind</em> and nothing
    /// else</b>, which was the whole truth while Zone's payload was a permission set the codec
    /// carried as one number. Connect packs three fields into that same word (<c>adr/0077</c>), and
    /// a codec that dropped the high byte would round-trip every verb, keep every hash it had, and
    /// silently turn every road edit into a lay of a Street on the east axis — the only combination
    /// whose encoding is zero.
    /// </para>
    /// <para>
    /// <b>So the case that matters is the one furthest from zero in every field</b>: a bulldoze, on
    /// the north axis, of a kind that is not Street. The last of those is a road edit the simulation
    /// <em>refuses</em> — the player lays Streets only — and it is here on purpose: a refusal the
    /// log cannot express is a refusal that cannot be replayed.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(StreetAxis.East, ConnectAction.Lay, RoadKind.Street)]
    [InlineData(StreetAxis.North, ConnectAction.Bulldoze, RoadKind.Street)]
    [InlineData(StreetAxis.North, ConnectAction.Bulldoze, RoadKind.Arterial)]
    [InlineData(StreetAxis.East, ConnectAction.Bulldoze, RoadKind.FootPath)]
    public void A_connect_line_survives_with_its_payload(
        StreetAxis axis, ConnectAction action, RoadKind kind)
    {
        ConnectPayload payload = new(axis, action, kind);

        InputLogBuilder builder = new(1, new WorldConfiguration(8), rulesetHash: 0);
        builder.Append(
            new Ticks(7), new Command(CommandKind.Connect, new Tiles(64), new Tiles(96), payload.Encode()));

        InputLog restored = InputLogCodec.FromText(InputLogCodec.ToText(builder.Build()));
        Command command = restored.Entry(0).Command;

        Assert.Equal(CommandKind.Connect, command.Kind);
        Assert.Equal(payload, ConnectPayload.Decode(command.Zone));
    }

    /// <summary>
    /// Negative coordinates survive. <c>Tiles</c> is signed and where the origin sits is a choice
    /// nothing has made yet, so a codec that lost the sign would be a trap set for a later slice.
    /// </summary>
    [Fact]
    public void A_negative_coordinate_survives_the_round_trip()
    {
        InputLogBuilder builder = new(1, new WorldConfiguration(8), rulesetHash: 0);
        builder.Append(new Ticks(0), new Command(CommandKind.Zone, new Tiles(-40), new Tiles(-1)));

        InputLog restored = InputLogCodec.FromText(InputLogCodec.ToText(builder.Build()));

        Assert.Equal(new Tiles(-40), restored.Entry(0).Command.East);
        Assert.Equal(new Tiles(-1), restored.Entry(0).Command.North);
    }

    /// <summary>
    /// A log is annotatable. Narrowing down a bug means writing <em>this is the command that does
    /// it</em> beside a line, and a format that refuses the annotation is one people copy out of.
    /// </summary>
    [Fact]
    public void Comments_and_blank_lines_are_ignored_anywhere()
    {
        InputLog restored = InputLogCodec.FromText(
            """
            # a session that reproduces the thing
            borough-log 1

            seed 0x000000000000002A
            citizens 16
            ruleset 0x0000000000000000
            --
            0 zone 1 2 3
            # this is the command that does it
            4 zone 5 6 7
            """);

        Assert.Equal(0x2AUL, restored.Seed);
        Assert.Equal(2, restored.Count);
        Assert.Equal(new Ticks(4), restored.Entry(1).Tick);
    }

    /// <summary>
    /// A future format version is refused rather than guessed at. A log outlives the build that
    /// wrote it, and a reader that guesses reproduces the wrong city.
    /// </summary>
    [Fact]
    public void A_format_version_this_build_does_not_know_is_refused()
    {
        FormatException failure = Assert.Throws<FormatException>(
            () => InputLogCodec.FromText("borough-log 2\nseed 0x1\ncitizens 1\nruleset 0x0\n--\n"));

        Assert.Contains("version 2", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Something_that_is_not_a_log_is_refused()
    {
        Assert.Throws<FormatException>(() => InputLogCodec.FromText("[ruleset]\nname = \"vanilla\"\n"));
    }

    [Fact]
    public void An_unknown_verb_is_refused()
    {
        FormatException failure = Assert.Throws<FormatException>(
            () => InputLogCodec.FromText(Header + "0 demolish 1 2 3\n"));

        Assert.Contains("demolish", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A truncated log is refused, and this is the case worth having: the crash artifact is written
    /// at the moment the process is least healthy, so a half-written one is the ordinary accident
    /// rather than the exotic one.
    /// </summary>
    [Fact]
    public void A_truncated_log_is_refused()
    {
        Assert.Throws<FormatException>(() => InputLogCodec.FromText("borough-log 1\nseed 0x1\n"));
    }

    /// <summary>
    /// A hand-edited log with its Ticks out of order is refused by the same code that refuses it in
    /// memory — the reader builds through <see cref="InputLogBuilder"/> rather than around it, so
    /// the append-only rule has one implementation and not two.
    /// </summary>
    [Fact]
    public void A_log_whose_ticks_run_backwards_is_refused()
    {
        Assert.Throws<InvalidOperationException>(
            () => InputLogCodec.FromText(Header + "9 zone 1 1 1\n2 zone 1 1 1\n"));
    }

    /// <summary>Every complaint names the line it is about. Malformed input log is not a diagnosis.</summary>
    [Fact]
    public void A_complaint_names_the_line_it_is_about()
    {
        FormatException failure = Assert.Throws<FormatException>(
            () => InputLogCodec.FromText(Header + "0 zone 1 1 1\n1 zone 1 1\n"));

        // Five header lines, then the good command, then the short one.
        Assert.StartsWith("line 7:", failure.Message, StringComparison.Ordinal);
    }

    private const string Header = "borough-log 1\nseed 0x2A\ncitizens 16\nruleset 0x0\n--\n";

    private static InputLog Log()
    {
        InputLogBuilder builder = new(
            seed: 0x0B07_0000_0000_0001UL,
            new WorldConfiguration(64),
            rulesetHash: 0xDEAD_BEEF_0000_0001UL);

        builder.Append(new Ticks(0), new Command(CommandKind.Zone, new Tiles(0), new Tiles(0), zone: 1));
        builder.Append(new Ticks(1), new Command(CommandKind.Zone, new Tiles(1), new Tiles(0), zone: 2));
        builder.Append(new Ticks(1), new Command(CommandKind.Zone, new Tiles(2), new Tiles(0), zone: 3));
        builder.Append(new Ticks(9), new Command(CommandKind.Zone, new Tiles(7), new Tiles(3), zone: 1));

        return builder.Build();
    }
}
