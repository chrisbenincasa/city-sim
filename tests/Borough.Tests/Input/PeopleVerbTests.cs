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
/// <c>plans/0045</c> row 21: <b>a world the player built can be lived in.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every world in this file is reached the way a player reaches one</b> — <c>Ground</c>, then
/// <c>Connect</c>, then <c>Zone</c>, then <c>People</c>, each through <see cref="Simulation.Step"/>
/// — because the gap the row names is not in any of the four verbs separately. <c>Ground</c> landed
/// on 2026-09-04 and made <c>adr/0090</c>'s world reachable for the first time; measured on
/// <c>rulesets/minimal.toml</c> the same day, 40 Street clicks and 16 zone clicks gave 40 Segments
/// and 128 Lots, and five in-world Days later the readout said <b>0 Buildings and 0 Citizens</b>.
/// ***The chain is Households → the Unplaced Pool → placement → Buildings, and an empty world has an
/// empty Pool, which is the Pool working rather than failing.***
/// </para>
/// <para>
/// ⚠ <b>Nothing here pins a count, and that is deliberate.</b> How many Buildings a zoning produces
/// is a property of <c>LotSubdivider</c>, of <c>[[building]] occupants</c> and of the ground the
/// generator drew, all of which move with a Ruleset edit and a seed. What the verb owes is
/// structural: a city that had none has some, the land it was given is untouched, and the two ways
/// of asking for it wrongly are refused rather than half-applied.
/// </para>
/// </remarks>
public sealed class PeopleVerbTests
{
    /// <summary>Small enough that the Lots a 3×3 block lattice carves hold everybody.</summary>
    private const int Citizens = 400;

    private const int Seed = 20_260_904;

    /// <summary>Blocks a side. The lattice is hand-laid, so this is what makes the Lots exist.</summary>
    private const int Blocks = 3;

    /// <summary>
    /// 🔴 <b>The row, in one test: Streets and Lots and nobody, then people.</b>
    /// </summary>
    [Fact]
    public void A_player_built_world_gets_a_population_through_the_verb()
    {
        (World world, Simulation simulation) = Laid();

        // The premise, asserted rather than assumed: the failing measurement was a world with Lots
        // in it and nobody, which is the only starting point this verb is about.
        Assert.True(world.Lots.Rows.LiveCount > 0, "the hand-laid zoning carved no Lots.");
        Assert.Equal(0, world.Buildings.Rows.LiveCount);
        Assert.Equal(0, world.Citizens.Rows.LiveCount);

        simulation.Step(new TickInput([People], 0));

        Assert.True(world.Buildings.Rows.LiveCount > 0, "the verb raised no Building.");
        Assert.True(world.Households.Rows.LiveCount > 0, "the verb formed no Household.");
        Assert.True(world.Citizens.Rows.LiveCount > 0, "the verb made nobody.");
    }

    /// <summary>
    /// ⚠ <b>It is <c>Populate</c>'s people half and lays no land</b>, which is the whole of what
    /// separates the two verbs.
    /// </summary>
    /// <remarks>
    /// <c>Populate</c> lays the lattice and carves the Lots itself and would throw on a world that
    /// already has Segments. This builds on what is standing, so the network the player drew has to
    /// come out the far side of it unchanged — ***a populator that re-plans the city is not the half
    /// that was split off.***
    /// </remarks>
    [Fact]
    public void The_verb_builds_on_the_players_network_and_does_not_relay_it()
    {
        (World world, Simulation simulation) = Laid();

        int segments = world.Roads.Segments.Rows.LiveCount;
        int lots = world.Lots.Rows.LiveCount;

        simulation.Step(new TickInput([People], 0));

        Assert.Equal(segments, world.Roads.Segments.Rows.LiveCount);
        Assert.Equal(lots, world.Lots.Rows.LiveCount);
    }

    /// <summary>
    /// <b>Twice is refused, and refused by the query before it is refused by the applier.</b>
    /// </summary>
    /// <remarks>
    /// Both halves matter and only together. An exception out of phase 0 aborts a Tick half way and
    /// leaves a world no invariant covers, so <c>Simulation.Refuses</c> answering first is what lets
    /// a front end decline the click — <c>plans/0045</c> row <b>15e</b>, and this verb is one a hand
    /// at the keyboard can genuinely press a second time.
    /// </remarks>
    [Fact]
    public void Populating_a_populated_world_is_refused()
    {
        (World world, Simulation simulation) = Laid();

        simulation.Step(new TickInput([People], 0));

        int citizens = world.Citizens.Rows.LiveCount;

        Assert.Equal(Refusal.PeopleWorldAlreadyHasAPopulation, simulation.Refuses(People));
        Assert.Throws<InvalidOperationException>(
            () => simulation.Step(new TickInput([People], 0)));

        Assert.Equal(citizens, world.Citizens.Rows.LiveCount);
    }

    /// <summary>
    /// <b>A world with no Lots is refused rather than populated into nowhere.</b>
    /// </summary>
    /// <remarks>
    /// ***A populator that makes no rows answers the sizing question with an empty world and reports
    /// success***, which is the one outcome nothing downstream can tell from a small city. It is the
    /// ordinary mistake on this path too: the Lots come from <c>Zone</c>, so a player who lays
    /// Streets and asks for people before zoning anything reaches it on the way to playing properly.
    /// </remarks>
    [Fact]
    public void Populating_a_world_with_no_Lots_is_refused()
    {
        (World world, Simulation simulation) = Grounded();

        Assert.Equal(0, world.Lots.Rows.LiveCount);
        Assert.Equal(Refusal.PeopleWorldHasNoLots, simulation.Refuses(People));
        Assert.Throws<InvalidOperationException>(
            () => simulation.Step(new TickInput([People], 0)));

        Assert.Equal(0, world.Citizens.Rows.LiveCount);
    }

    /// <summary>
    /// <b>The verb spells into the Input Log and back</b>, so a session that used it replays.
    /// </summary>
    /// <remarks>
    /// <c>InputLogCodecTests</c> asserts the round trip over the whole enum, which covers this verb
    /// the moment it is declared. What that cannot say is which <em>word</em> the format chose, and a
    /// log is read by hand when somebody is holding a crash artefact — so the spelling is pinned here.
    /// </remarks>
    [Fact]
    public void The_verb_is_written_into_a_log_as_people()
    {
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Citizens), rulesetHash: 0);

        builder.Append(Ticks.Zero, People);

        string text = InputLogCodec.ToText(builder.Build());

        Assert.Contains(" people ", text, StringComparison.Ordinal);
        Assert.Equal(
            CommandKind.People, InputLogCodec.FromText(text).Entry(0).Command.Kind);
    }

    // ---- the worlds -----------------------------------------------------------------------------

    private static readonly Command People = new(CommandKind.People, default, default);

    /// <summary>The ground and nothing on it — <c>adr/0090</c>'s world, as <c>--empty</c> gives it.</summary>
    private static (World World, Simulation Simulation) Grounded()
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, Minimal(), key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        simulation.Step(new TickInput([new Command(CommandKind.Ground, default, default)], 0));

        return (world, simulation);
    }

    /// <summary>That world with a hand-laid lattice on it and every block of it zoned.</summary>
    /// <remarks>
    /// ⚠ <b>The zoning is a Tick after the Streets and cannot share one.</b> <c>LotSubdivider</c>
    /// carves a block against the Street faces that are <em>standing</em>, so a Zone applied in the
    /// same <see cref="TickInput"/> as the Connect that gives it a face yields nothing.
    /// </remarks>
    private static (World World, Simulation Simulation) Laid()
    {
        (World world, Simulation simulation) = Grounded();

        simulation.Step(new TickInput(Streets(world), 0));
        simulation.Step(new TickInput(Zoning(world), 0));

        return (world, simulation);
    }

    /// <summary>Every face of a <see cref="Blocks"/>-square block grid at the map's origin corner.</summary>
    /// <remarks>
    /// <b>Every edge is named by its lower endpoint and an axis</b>, which is <c>StreetAxis</c>'s
    /// whole shape — there is no South and no West, so a grid is laid without naming an edge twice.
    /// </remarks>
    private static Command[] Streets(World world)
    {
        int block = world.Roads.Streets.BlockTiles;
        List<Command> commands = [];

        for (int row = 0; row <= Blocks; row++)
        {
            for (int column = 0; column < Blocks; column++)
            {
                commands.Add(Lay(block, column, row, StreetAxis.East));
            }
        }

        for (int column = 0; column <= Blocks; column++)
        {
            for (int row = 0; row < Blocks; row++)
            {
                commands.Add(Lay(block, column, row, StreetAxis.North));
            }
        }

        return [.. commands];
    }

    /// <summary>One <c>Zone</c> per block, addressed at a Tile in the middle of it.</summary>
    private static Command[] Zoning(World world)
    {
        int block = world.Roads.Streets.BlockTiles;
        List<Command> commands = [];

        for (int column = 0; column < Blocks; column++)
        {
            for (int row = 0; row < Blocks; row++)
            {
                commands.Add(new Command(
                    CommandKind.Zone,
                    new Tiles((column * block) + (block / 2)),
                    new Tiles((row * block) + (block / 2)),
                    zone: 1));
            }
        }

        return [.. commands];
    }

    private static Command Lay(int block, int column, int row, StreetAxis axis) => new(
        CommandKind.Connect,
        new Tiles(column * block),
        new Tiles(row * block),
        new ConnectPayload(axis, ConnectAction.Lay, RoadKind.Street).Encode());

    /// <summary>
    /// <c>rulesets/minimal.toml</c>, from the file, because that is the world the row was measured in.
    /// </summary>
    private static Ruleset Minimal()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml");
        RulesetLoadResult result = RulesetLoader.Parse(File.ReadAllText(path), "minimal.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
