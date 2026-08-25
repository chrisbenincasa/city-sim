namespace Borough.Tests.Entities;

using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit;

/// <summary>
/// The populator has two doors, and a player-shaped network can get a population through the second.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0003</c> hash-moving queue item 9.</b> <see cref="RoadGenerator.LayInto"/> throws on a
/// world that already has Segments and <see cref="SyntheticCity.PopulateInto"/> called it
/// unconditionally, before it built a row — so a city laid by <c>CommandKind.Connect</c> could not be
/// populated in either order, and under <c>adr/0090</c> a Connect-laid city is every city a player will
/// ever have. <see cref="SyntheticCity.PeopleInto"/> is the people half on its own.
/// </para>
/// <para>
/// <b>The refusal is asserted as well as the repair, and that is the point of this file rather than an
/// extra.</b> The obvious way to close the gap is to soften <see cref="RoadGenerator.LayInto"/>, and
/// that would have been wrong: the generator is a world-creation pass, editing a standing graph is
/// <c>Connect</c>, and a generator that quietly no-ops on a non-empty graph would let a second
/// <c>Populate</c> double a lattice with nothing to say so. <b>The refusal was never the defect</b>, so
/// a test holds it in place.
/// </para>
/// </remarks>
public sealed class PopulatorDoorTests
{
    private const int BlockTiles = 32;

    private const int Blocks = 4;

    private const int Population = 400;

    private const ulong Seed = 0xD00D_0000_0001UL;

    private static readonly WorldKey Key = WorldKey.FromSeed(Seed);

    /// <summary>
    /// A city whose every Street was laid by the player can be given a population.
    /// </summary>
    /// <remarks>
    /// <b>This is the gap, stated as the thing that used to be impossible.</b> Before item 9 the only
    /// route to this world was copying the populator's three loops into a fixture, which
    /// <c>ConnectedCityCongestionTests</c> did and no longer does.
    /// </remarks>
    [Fact]
    public void A_connect_laid_city_can_be_populated()
    {
        Simulation simulation = Connected();

        Assert.True(simulation.World.Lots.Rows.LiveCount > 0, "the zoning carved no Lots.");

        SyntheticCity.PeopleInto(simulation.World, Key, Ticks.Zero);

        Assert.Equal(Population, simulation.World.Citizens.Rows.LiveCount);
        Assert.True(simulation.World.Buildings.Rows.LiveCount > 0, "nobody was housed.");

        // Every Building sits on a Lot the player's own Streets fronted, which is the whole difference
        // between this city and a generated one. A Building with no frontage is adr/0079's hole.
        Assert.True(
            simulation.World.Roads.Segments.Rows.LiveCount > 0,
            "the Streets vanished, so the city under test is not the one that was laid.");
    }

    /// <summary>
    /// The generator's refusal survives the repair, and a Connect-laid world still cannot be
    /// <em>generated</em> into.
    /// </summary>
    /// <remarks>
    /// <b>The negative half, and it is the one a future reader is most likely to delete.</b> Item 9
    /// says in as many words: do not delete <c>LayInto</c>'s refusal to get there.
    /// </remarks>
    [Fact]
    public void The_generator_still_refuses_a_world_that_has_streets()
    {
        Simulation simulation = Connected();

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => SyntheticCity.PopulateInto(simulation.World, Key, Ticks.Zero));

        Assert.Contains("already has roads", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The people half refuses a world that already holds people, exactly as the whole verb does.
    /// </summary>
    /// <remarks>
    /// <b>A second entry point is a second way past a guard unless the guard is on both.</b> The
    /// refusal is one method now, so this asserts the sharing rather than a copy of the message.
    /// </remarks>
    [Fact]
    public void The_people_half_refuses_a_world_that_already_has_a_population()
    {
        Simulation simulation = Connected();

        SyntheticCity.PeopleInto(simulation.World, Key, Ticks.Zero);

        Assert.Throws<InvalidOperationException>(
            () => SyntheticCity.PeopleInto(simulation.World, Key, Ticks.Zero));
    }

    /// <summary>
    /// The people half refuses a world with no Lots rather than quietly building an empty city.
    /// </summary>
    /// <remarks>
    /// <b>⚠ Opening the door is what made this case reachable, and writing the test is what found
    /// it.</b> <see cref="SyntheticCity.PopulateInto"/> cannot reach zero Lots — <c>Subdivide</c>'s
    /// degenerate branch lays one per wanted Building when there is no lattice — but a caller who has
    /// laid Streets and not zoned them can, and the clamp then divided by zero in the Household loop.
    /// <b>Silence is the wrong answer for the reason <c>Subdivide</c> states about itself</b>: a
    /// populator that makes no rows answers the sizing question with an empty world and reports
    /// success. The land is the caller's to lay, so the caller is told it laid none.
    /// </remarks>
    [Fact]
    public void The_people_half_refuses_a_world_where_no_lots_stand()
    {
        Simulation simulation = Bare();

        Assert.Equal(0, simulation.World.Lots.Rows.LiveCount);

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(
            () => SyntheticCity.PeopleInto(simulation.World, Key, Ticks.Zero));

        Assert.Contains("no Lots", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, simulation.World.Buildings.Rows.LiveCount);
    }

    /// <summary>A world with a grid of player-laid Streets, zoned.</summary>
    private static Simulation Connected()
    {
        Simulation simulation = Bare();

        // A Tick apart, because a Zone carves against the faces that are standing.
        simulation.Step(new TickInput(Streets(), rulesetHash: 0));
        simulation.Step(new TickInput(Zoning(), rulesetHash: 0));

        return simulation;
    }

    /// <summary>A world with nothing in it, under a Ruleset that declares no generator lattice.</summary>
    private static Simulation Bare()
    {
        InputLogBuilder builder = new(
            Seed, new WorldConfiguration(Population), rulesetHash: 0);

        Simulation simulation = Replay.Start(builder.Build(), Rules());

        simulation.VerifyDecideWritesNothing = false;

        return simulation;
    }

    private static Command[] Streets()
    {
        List<Command> commands = [];

        for (int row = 0; row <= Blocks; row++)
        {
            for (int column = 0; column < Blocks; column++)
            {
                commands.Add(Lay(column, row, StreetAxis.East));
            }
        }

        for (int column = 0; column <= Blocks; column++)
        {
            for (int row = 0; row < Blocks; row++)
            {
                commands.Add(Lay(column, row, StreetAxis.North));
            }
        }

        return [.. commands];
    }

    private static Command[] Zoning()
    {
        List<Command> commands = [];

        for (int column = 0; column < Blocks; column++)
        {
            for (int row = 0; row < Blocks; row++)
            {
                commands.Add(new Command(CommandKind.Zone, Middle(column), Middle(row), zone: 1));
            }
        }

        return [.. commands];
    }

    private static Command Lay(int column, int row, StreetAxis axis) => new(
        CommandKind.Connect,
        new Tiles(column * BlockTiles),
        new Tiles(row * BlockTiles),
        new ConnectPayload(axis, ConnectAction.Lay, RoadKind.Street).Encode());

    private static Tiles Middle(int block) => new((block * BlockTiles) + (BlockTiles / 2));

    /// <summary>
    /// The smallest Ruleset that gives kind 1 an occupancy and a Lot subdivider, and declares no
    /// generator lattice.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It stated <c>kind = 1</c> until 2026-08-25 and that key was never read</b> — a kind's id
    /// is its <em>declaration order</em>, so this table is kind 1 by being first and the line only
    /// ever restated what position already decided. `plans/0041` G31's unknown-key check is what
    /// found it; before that a fixture could assert a number the loader never saw.
    /// </remarks>
    private static Ruleset Rules()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            """
            [[building]]
            name = "dwelling"
            occupants = 3

            [roads]
            block_tiles = 32
            arterial_count = 0
            arterial_junction_tiles = 512
            foot_crossing_every = 4
            foot_paths_per_thousand_blocks = 40
            street_speed_kph = 50
            arterial_speed_kph = 90
            walk_speed_kph = 5
            street_capacity_per_hour = 3600
            arterial_capacity_per_hour = 12000
            foot_path_capacity_per_hour = 1000

            [lots]
            lots_per_segment = 5
            """,
            "populator-door.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }
}
