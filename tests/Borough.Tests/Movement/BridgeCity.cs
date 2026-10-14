using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Movement;

/// <summary>
/// Row 30's diagnostic fixture: two 2×2-block districts joined by one bridge Street, with the gate
/// on the near side.
/// </summary>
/// <remarks>
/// Built on <c>congested.toml</c>, where every dwelling is also a shop's premises, so jobs stand on
/// both sides. Removing the bridge fails every commute across it and cuts the far side off from the
/// Outside Connection. See <c>plans/decline-recovery.md</c>.
/// </remarks>
internal static class BridgeCity
{
    public const int BlockTiles = 32;
    public const int DistrictBlocks = 2;
    public const int BridgeColumn = DistrictBlocks;
    public const int FarColumn = DistrictBlocks + 1;
    public const int Population = 1_000;

    private const ulong Seed = 0xB121_D6E0_0000_0030UL;

    private static readonly WorldKey Key = WorldKey.FromSeed(Seed);

    public static readonly Tiles BridgeEast = new((BridgeColumn * BlockTiles) + (BlockTiles / 2));

    /// <summary>23:00, when no commuter is on the road.</summary>
    public static long LateEvening(int day) => (day * Ticks.PerDay) + Ticks.AtClock(23);

    /// <summary>11:00, when every commuter is at work.</summary>
    public static long LateMorning(int day) => (day * Ticks.PerDay) + Ticks.AtClock(11);

    public static Simulation Start(Ruleset rules)
    {
        InputLogBuilder builder = new(Seed, new WorldConfiguration(Population), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), rules);

        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(new TickInput(Streets(), rulesetHash: 0));
        simulation.Step(new TickInput(Zoning(), rulesetHash: 0));

        SyntheticCity.PeopleInto(simulation.World, Key, Ticks.Zero);

        return simulation;
    }

    /// <summary>
    /// <c>congested.toml</c> with a gate kind, optionally dismissing after
    /// <paramref name="dismissAfterDays"/> Days of failed commutes.
    /// </summary>
    public static Ruleset BridgeRules(int? dismissAfterDays = null, int? cutOffDays = null) =>
        Load(BridgeToml(dismissAfterDays, cutOffDays));

    public static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "bridge.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>
    /// The text of <see cref="BridgeRules"/>, optionally abandoning a dwelling cut off for
    /// <paramref name="cutOffDays"/> Days.
    /// </summary>
    public static string BridgeToml(int? dismissAfterDays = null, int? cutOffDays = null)
    {
        string toml = Replaced(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "congested.toml")),
                "\n[layers]\n",
                "gives_up_after_days = 120\n\n[layers]\n")
            + "\n[[building]]\nname = \"port\"\narrivals_per_day = 1\n";

        if (dismissAfterDays is { } days)
        {
            toml = Replaced(toml, "\n[jobs]\n", $"\n[jobs]\ndismissed_after_failed_commute_days = {days}\n");
        }

        if (cutOffDays is { } cutOff)
        {
            toml = toml.Replace(
                "[[building]]\nname = \"dwelling\"\n",
                $"[[building]]\nname = \"dwelling\"\nabandoned_after_cut_off_days = {cutOff}\ncollapses_after_days = 30\n",
                StringComparison.Ordinal);
        }

        return toml;
    }

    private static string Replaced(string text, string old, string replacement)
    {
        Assert.Contains(old, text, StringComparison.Ordinal);
        return text.Replace(old, replacement, StringComparison.Ordinal);
    }

    public static (long Completed, long NoRoute, long OverBudget) StepTo(Simulation simulation, long tick)
    {
        long completed = 0, noRoute = 0, overBudget = 0;

        while ((long)simulation.World.Tick.Raw < tick)
        {
            simulation.Step(new TickInput(default, rulesetHash: 0));
            simulation.Employment.Drain();

            TripActivity trips = simulation.Trips.Drain();
            completed += trips.Completed.Sum;
            noRoute += trips.NoRouteFound.Sum;
            overBudget += trips.ExceededCommuteBudget.Sum;
        }

        return (completed, noRoute, overBudget);
    }

    public static void RemoveBridge(Simulation simulation) =>
        Issue(simulation, Command.Demolish(BridgeEast, new Tiles(0), DemolishTarget.Street));

    public static void RebuildBridge(Simulation simulation) =>
        Issue(simulation, Lay(BridgeColumn, 0, StreetAxis.East));

    /// <summary>Employed Citizens whose home and workplace stand on opposite sides of the bridge.</summary>
    public static List<Handle<Citizen>> Crossers(World world)
    {
        List<Handle<Citizen>> crossers = [];

        for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
        {
            if (world.Citizens.Rows.IsLive(citizen) && Crosses(world, citizen))
            {
                crossers.Add(world.Citizens.Rows.At(citizen));
            }
        }

        return crossers;
    }

    public static bool Crosses(World world, int citizen) =>
        world.Businesses.Rows.TryResolve(world.Citizens.Workplace[citizen], out int business)
        && IsFar(world, HomeOf(world, citizen)) != IsFar(world, PremisesOf(world, business));

    public static int Gate(World world)
    {
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.Buildings.Rows.IsLive(building) && world.IsOutsideConnection(world.Buildings.Kind[building]))
            {
                return building;
            }
        }

        Assert.Fail("no gate was raised, so nothing defines what the far side is cut off from.");

        return Rows.NoSlot;
    }

    public static bool IsFar(World world, int building) =>
        world.Lots.East[world.Lots.Rows.Resolve(world.Buildings.Lot[building])].Raw >= FarColumn * BlockTiles;

    public static int HomeOf(World world, int citizen) =>
        world.Buildings.Rows.Resolve(
            world.Households.Dwelling[world.Households.Rows.Resolve(world.Citizens.HouseholdOf[citizen])]);

    public static int PremisesOf(World world, int business) =>
        world.Buildings.Rows.Resolve(world.Businesses.Building[business]);

    private static void Issue(Simulation simulation, Command command)
    {
        int refused = simulation.CommandsRefused;
        int segments = simulation.World.Roads.Segments.Rows.LiveCount;

        simulation.Step(new TickInput([command], rulesetHash: 0));

        Assert.Equal(refused, simulation.CommandsRefused);
        Assert.NotEqual(segments, simulation.World.Roads.Segments.Rows.LiveCount);
    }

    private static Command[] Streets()
    {
        List<Command> commands = [];

        Grid(commands, 0);
        Grid(commands, FarColumn);
        commands.Add(Lay(BridgeColumn, 0, StreetAxis.East));

        return [.. commands];
    }

    private static void Grid(List<Command> into, int at)
    {
        for (int row = 0; row <= DistrictBlocks; row++)
        {
            for (int column = at; column < at + DistrictBlocks; column++)
            {
                into.Add(Lay(column, row, StreetAxis.East));
            }
        }

        for (int column = at; column <= at + DistrictBlocks; column++)
        {
            for (int row = 0; row < DistrictBlocks; row++)
            {
                into.Add(Lay(column, row, StreetAxis.North));
            }
        }
    }

    private static Command[] Zoning()
    {
        List<Command> commands = [];

        foreach (int at in (int[])[0, FarColumn])
        {
            for (int column = at; column < at + DistrictBlocks; column++)
            {
                for (int row = 0; row < DistrictBlocks; row++)
                {
                    commands.Add(new Command(CommandKind.Zone, Middle(column), Middle(row), zone: 1));
                }
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
}
