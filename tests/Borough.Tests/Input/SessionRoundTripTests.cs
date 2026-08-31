using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Input;

/// <summary>
/// <c>plans/0045</c> queue item 15d: <b>the session is a log</b>, and a city played by hand replays
/// to the same State Hash.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>This pins the SHELL's recording contract, in the one place a test host exists to pin it.</b>
/// <c>src/Borough.Godot</c> is not in <c>Borough.slnx</c> and cannot be tested, so what is asserted
/// here is the arithmetic the shell depends on: <em>a Command recorded at <c>world.Tick</c>, then
/// handed to <c>Step</c>, replays to the same city</em>. The shell's <c>Ordered()</c> does exactly
/// that and nothing else.
/// </para>
/// <para>
/// ⚠ <b><c>Populate</c> is the first entry rather than a call on the side.</b> Until this row the
/// shell called <c>SyntheticCity.PopulateInto</c> on the world directly — thousands of rows entering
/// by a door the log does not account for — so ***every hand-played session would have replayed
/// against an empty world and diverged at Tick 0***, with nothing in the file to explain it.
/// <c>Borough.Headless</c> has recorded the population as a Command since slice 6.
/// </para>
/// <para>
/// ⚠ <b>The off-by-one is asserted in BOTH directions</b>, because a round trip that only ever
/// checks agreement passes just as happily when the two sides are wrong together.
/// <see cref="A_command_recorded_one_tick_late_diverges"/> is what says this test can fail.
/// </para>
/// </remarks>
public sealed class SessionRoundTripTests
{
    private const ulong Seed = 0x5E5510_0F_C1_1CE5UL;
    private const int Citizens = 2_000;

    /// <summary>Where the hand-played city is stopped and compared.</summary>
    private const int Horizon = 400;

    /// <summary>
    /// <b><c>schooled.toml</c> and not <c>declining.toml</c>, and the reason is the negative
    /// control.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>MEASURED: on <c>declining.toml</c> a slip of 1, 2, 4, 8, 16, 31, 32, 33 and 64 Ticks
    /// was ABSORBED — every one of them replayed to the same State Hash.</b> <c>Connect</c> lays a
    /// Segment and <c>Zone</c> creates Lots, and ***neither table records when it happened***, so
    /// the city at Tick 400 is the same city whether the verb landed at 100 or at 164.
    /// <see cref="A_command_recorded_at_the_wrong_tick_diverges"/> therefore needs a verb whose
    /// effect carries a clock, and <c>Service</c> raising a Building is one:
    /// <c>BuildingTable.EmptySince</c> is <c>Saved</c> and is stamped when the Building goes up.
    /// ⚠ <b>Only <c>schooled.toml</c> declares a kind that <c>serves</c>.</b>
    /// </remarks>
    private const string File = "schooled.toml";

    private static Command Populate => new(CommandKind.Populate, default, default);

    private static (Ruleset Rules, ulong Hash) Shipped(string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
        string toml = System.IO.File.ReadAllText(path);
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, file);

        Assert.True(loaded.Ok, loaded.Describe());

        return (loaded.Ruleset!, RulesetFile.HashOf(path));
    }

    /// <summary>
    /// Plays a city the way the shell does: a <c>Populate</c> Command, then verbs at Ticks, with
    /// every Command written to the log at <c>world.Tick</c> immediately before it is stepped.
    /// </summary>
    private static (ulong Hash, InputLog Log) Played(Ruleset rules, ulong rulesetHash, int slip)
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Citizens), rulesetHash);

        builder.Append(Ticks.Zero, Populate);
        simulation.Step(new TickInput([Populate], 0));

        int block = world.Roads.Streets.BlockTiles;

        foreach ((int at, Command command) in Verbs(world, block))
        {
            while (world.Tick.Raw < (ulong)at)
            {
                simulation.Step(default);
            }

            // THE LINE THE SHELL'S Ordered() IS. `slip` is zero for the honest recording and one
            // for the mistake below it.
            builder.Append(new Ticks(world.Tick.Raw + (ulong)slip), command);
            simulation.Step(new TickInput([command], 0));
        }

        while (world.Tick.Raw < Horizon)
        {
            simulation.Step(default);
        }

        return (world.HashState(), builder.Build());
    }

    /// <summary>
    /// Two verbs on virgin ground, well clear of the generated lattice.
    /// </summary>
    /// <remarks>
    /// <b>The Tiles are derived from <c>block_tiles</c> rather than typed</b>, so the fixture asks
    /// for a block corner the way <c>LotSubdivider</c> counts them and does not depend on the
    /// lattice's size staying what it is today.
    /// </remarks>
    private static (int At, Command Command)[] Verbs(World world, int block)
    {
        var east = new Tiles(block * 40);
        var north = new Tiles(block * 40);
        var payload = new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street);

        return
        [
            (100, new Command(CommandKind.Connect, east, north, payload.Encode())),
            (200, new Command(CommandKind.Zone, east, north, world.Rules.ZoneRules[0].Admits)),
            (300, Command.Service(
                world.Lots.East[Vacant(world)],
                world.Lots.North[Vacant(world)],
                Serving(world.Rules))),
        ];
    }

    /// <summary>The first vacant Lot, which is what <c>Service</c> needs and the shell hunts for.</summary>
    private static int Vacant(World world)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot))
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"{File} generated no vacant Lot to place a school on.");
    }

    /// <summary>The first kind declaring <c>serves</c>, which is what the shell's `s` key cycles.</summary>
    private static byte Serving(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind((byte)kind).Serves != Need.None)
            {
                return (byte)kind;
            }
        }

        throw new InvalidOperationException($"{File} declares no kind that serves.");
    }

    /// <summary>
    /// 🔴 <b>The round trip: a city played by hand, replayed from its file, same State Hash.</b>
    /// </summary>
    [Fact]
    public void A_hand_played_city_replays_from_its_log_to_the_same_state_hash()
    {
        (Ruleset rules, ulong hash) = Shipped(File);

        (ulong played, InputLog log) = Played(rules, hash, slip: 0);

        ulong[] replayed = Replay.Run(log, new Ticks(Horizon), Horizon, rules);

        Assert.Equal(played, replayed[^1]);
    }

    /// <summary>
    /// The log carries the verbs rather than only the population, which is what makes the
    /// comparison above mean anything.
    /// </summary>
    [Fact]
    public void The_log_holds_every_command_the_session_issued()
    {
        (Ruleset rules, ulong hash) = Shipped(File);

        (_, InputLog log) = Played(rules, hash, slip: 0);

        Assert.Equal(4, log.Count);
        Assert.Equal(CommandKind.Populate, log.At(Ticks.Zero)[0].Kind);
        Assert.Equal(CommandKind.Connect, log.At(new Ticks(100))[0].Kind);
        Assert.Equal(CommandKind.Zone, log.At(new Ticks(200))[0].Kind);
        Assert.Equal(CommandKind.Service, log.At(new Ticks(300))[0].Kind);
    }

    /// <summary>
    /// 🔴 <b>The assertion that says this test can fail.</b> A verb recorded one Tick after the
    /// Tick it applied at replays to a different city.
    /// </summary>
    /// <remarks>
    /// <b>It is the mistake the shell is one character away from</b> — <c>Ordered()</c> is
    /// evaluated as the argument to <c>Step</c>, so the Tick it reads is the one about to run.
    /// Recording <c>Tick + 1</c> compiles, writes a well-formed log, replays without complaint, and
    /// produces a different city. ***A divergence that appears at the first command and reads as a
    /// simulation bug.***
    /// </remarks>
    [Fact]
    public void A_command_recorded_at_the_wrong_tick_diverges()
    {
        (Ruleset rules, ulong hash) = Shipped(File);

        (ulong played, InputLog log) = Played(rules, hash, slip: 1);

        ulong[] replayed = Replay.Run(log, new Ticks(Horizon), Horizon, rules);

        Assert.NotEqual(played, replayed[^1]);
    }
}
