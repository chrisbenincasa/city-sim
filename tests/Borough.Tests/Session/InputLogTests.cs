using Borough.Core.Input;
using Borough.Core.Quantities;

namespace Borough.Tests.Session;

/// <summary>
/// The Input Log: <c>(world seed, configuration, Ruleset content hash, commands per Tick)</c>, and
/// nothing more.
/// </summary>
public sealed class InputLogTests
{
    [Fact]
    public void An_empty_log_has_no_commands_at_any_tick()
    {
        InputLog log = new InputLogBuilder(1, new WorldConfiguration(16), 0).Build();

        Assert.Equal(0, log.Count);
        Assert.Equal(0UL, log.Horizon.Raw);
        Assert.True(log.At(new Ticks(0)).IsEmpty);
        Assert.True(log.At(new Ticks(9_999)).IsEmpty);
    }

    [Fact]
    public void A_command_is_returned_for_its_own_tick_and_no_other()
    {
        InputLog log = Builder()
            .Append(new Ticks(7), Paint(east: 3))
            .Build();

        Assert.True(log.At(new Ticks(6)).IsEmpty);
        Assert.Equal(1, log.At(new Ticks(7)).Length);
        Assert.True(log.At(new Ticks(8)).IsEmpty);
    }

    /// <summary>
    /// Issue order within a Tick is content, not presentation: a player can issue two commands in one
    /// Tick and the second may depend on the first.
    /// </summary>
    [Fact]
    public void Commands_sharing_a_tick_keep_their_issue_order()
    {
        InputLog log = Builder()
            .Append(new Ticks(4), Paint(east: 1))
            .Append(new Ticks(4), Paint(east: 2))
            .Append(new Ticks(4), Paint(east: 3))
            .Build();

        ReadOnlySpan<Command> commands = log.At(new Ticks(4));

        Assert.Equal(3, commands.Length);
        Assert.Equal(new Tiles(1), commands[0].East);
        Assert.Equal(new Tiles(2), commands[1].East);
        Assert.Equal(new Tiles(3), commands[2].East);
    }

    /// <summary>
    /// Append-only is enforced rather than described. A log whose commands are out of order replays in
    /// an order the player never issued, and the divergence reads as a simulation bug.
    /// </summary>
    [Fact]
    public void Appending_at_an_earlier_tick_throws()
    {
        InputLogBuilder builder = Builder().Append(new Ticks(10), Paint(east: 1));

        Assert.Throws<InvalidOperationException>(() =>
            builder.Append(new Ticks(9), Paint(east: 2)));
    }

    [Fact]
    public void A_verbless_command_cannot_be_logged() =>
        Assert.Throws<ArgumentException>(() =>
            Builder().Append(new Ticks(0), default));

    [Fact]
    public void The_horizon_is_the_tick_after_the_last_command()
    {
        InputLog log = Builder()
            .Append(new Ticks(3), Paint(east: 1))
            .Append(new Ticks(88), Paint(east: 2))
            .Build();

        Assert.Equal(89UL, log.Horizon.Raw);
    }

    /// <summary>
    /// The lookup is a binary search over a sorted array rather than a map from Tick to a list — a
    /// hash map is banned outright (<c>adr/0003</c>) and a list per Tick would be a collection that
    /// grows with elapsed time (<c>adr/0006</c>).
    /// </summary>
    [Fact]
    public void Lookup_finds_every_tick_in_a_sparse_log()
    {
        InputLogBuilder builder = Builder();

        for (int i = 0; i < 200; i++)
        {
            builder.Append(new Ticks((ulong)i * 37), Paint(east: i));
        }

        InputLog log = builder.Build();

        for (int i = 0; i < 200; i++)
        {
            ReadOnlySpan<Command> at = log.At(new Ticks((ulong)i * 37));

            Assert.Equal(1, at.Length);
            Assert.Equal(new Tiles(i), at[0].East);
            Assert.True(log.At(new Ticks(((ulong)i * 37) + 1)).IsEmpty);
        }
    }

    [Fact]
    public void The_header_survives_the_build()
    {
        InputLog log = new InputLogBuilder(
            seed: 0xDEAD_BEEF_CAFE_BABEUL,
            new WorldConfiguration(4_096),
            rulesetHash: 0x0123_4567_89AB_CDEFUL).Build();

        Assert.Equal(0xDEAD_BEEF_CAFE_BABEUL, log.Seed);
        Assert.Equal(4_096, log.Configuration.Citizens);
        Assert.Equal(0x0123_4567_89AB_CDEFUL, log.RulesetHash);
    }

    /// <summary>
    /// Slice 8 makes this vary within a run, as a transition carrying both hashes. Until then one
    /// Ruleset is in force throughout — the shape is what is being fixed now, not the behaviour.
    /// </summary>
    [Fact]
    public void One_ruleset_is_in_force_for_the_whole_run()
    {
        InputLog log = new InputLogBuilder(1, new WorldConfiguration(16), rulesetHash: 42).Build();

        Assert.Equal(42UL, log.RulesetHashAt(new Ticks(0)));
        Assert.Equal(42UL, log.RulesetHashAt(new Ticks(100_000)));
    }

    private static InputLogBuilder Builder() =>
        new(seed: 1, new WorldConfiguration(64), rulesetHash: 0);

    private static Command Paint(int east) =>
        new(CommandKind.Zone, new Tiles(east), new Tiles(0), zone: 1);
}
