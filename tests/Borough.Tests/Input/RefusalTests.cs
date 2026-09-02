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
/// <c>plans/0045</c> queue item 15e: <b>the refusal and the guard are one predicate, and this is
/// what holds them together.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every case below asserts the same two things about one command</b> — that
/// <see cref="Simulation.Refuses"/> names the reason, and that <see cref="Simulation.Step"/> throws
/// on it. ***A front end that asks the first and is told the truth may decline to send***, which is
/// the whole of what 15e buys: an exception out of Phase 0 aborts a Tick half way and leaves a world
/// no invariant covers, so a click that would throw must never be queued.
/// </para>
/// <para>
/// 🔴 <b>The theory is driven by <see cref="Refusal"/> itself, so a member with no case here goes
/// red.</b> That is deliberate and is the only defence against the failure this row exists to
/// repair: the shell guarded three refusals out of thirteen by restating the rule in its own words,
/// and nothing anywhere could see the other ten. ***A registry that does not enumerate itself is a
/// list somebody forgets to add to.***
/// </para>
/// </remarks>
public sealed class RefusalTests
{
    private const int Citizens = 400;
    private const int Seed = 20_260_831;

    private const byte Dwelling = 1;
    private const byte School = 2;

    /// <summary>Every refusal the simulation can give, one case each.</summary>
    public static TheoryData<Refusal> Every()
    {
        var data = new TheoryData<Refusal>();

        foreach (Refusal refusal in Enum.GetValues<Refusal>())
        {
            if (refusal != Refusal.None)
            {
                data.Add(refusal);
            }
        }

        return data;
    }

    /// <summary>
    /// 🔴 <b>The query answers what the applier does, for every reason the applier has.</b>
    /// </summary>
    [Theory]
    [MemberData(nameof(Every))]
    public void The_query_and_the_applier_agree(Refusal expected)
    {
        (Simulation simulation, Command command) = Case(expected);

        Assert.Equal(expected, simulation.Refuses(command));

        // The other half, and the half that makes the first one worth anything: a query returning a
        // reason for a command that would have applied is not a guard, it is a shell refusing clicks
        // the city would have accepted.
        Assert.Throws<InvalidOperationException>(() => simulation.Step(new TickInput([command], 0)));
    }

    /// <summary>A command the city accepts is refused by nothing, and applies.</summary>
    /// <remarks>
    /// ⚠ <b>Without this every assertion above passes against a query that answers a reason to
    /// everything.</b> <see cref="Refusal.None"/> has no case in <see cref="Case"/> for that reason:
    /// it is not a refusal to construct, it is the absence of one, and it is asserted here against
    /// commands that really do land.
    /// </remarks>
    [Fact]
    public void A_command_the_city_accepts_is_refused_by_nothing()
    {
        (World world, Simulation simulation) = City(Schooled);
        int lot = FirstVacantLot(world);

        Command service = Command.Service(world.Lots.East[lot], world.Lots.North[lot], School);

        Assert.Equal(Refusal.None, simulation.Refuses(service));

        int before = world.Buildings.Rows.LiveCount;

        simulation.Step(new TickInput([service], 0));

        Assert.Equal(before + 1, world.Buildings.Rows.LiveCount);
    }

    /// <summary>
    /// ⚠ <b><c>Zone</c> is refused by nothing, and that is <c>02 §2.2</c> rather than an oversight.</b>
    /// </summary>
    /// <remarks>
    /// A block with no Street on any face yields no Lots, and a block already subdivided yields none
    /// either. ***Both are outcomes and neither is a refusal*** — a front end that greyed the click
    /// out would be hiding the mechanism by which a bad street layout punishes the player.
    /// </remarks>
    [Fact]
    public void Zoning_empty_ground_is_an_outcome_and_not_a_refusal()
    {
        (World _, Simulation simulation) = City(Schooled);

        Assert.Equal(
            Refusal.None,
            simulation.Refuses(new Command(CommandKind.Zone, new Tiles(9_000), new Tiles(9_000), 1)));
    }

    /// <summary>Asking costs the world nothing, which is what lets a hover ask every frame.</summary>
    /// <remarks>
    /// <b>Stated as a State Hash equality</b> rather than as a claim about the code: the query walks
    /// the Lot table for three of the verbs, and a walk that wrote anything would be a Phase-0 side
    /// effect outside a Tick — the one thing <c>Simulation</c>'s door argument forbids.
    /// </remarks>
    [Fact]
    public void Asking_writes_nothing()
    {
        (World world, Simulation simulation) = City(Schooled);

        ulong before = world.HashState();

        foreach (Refusal refusal in Enum.GetValues<Refusal>())
        {
            simulation.Refuses(Case(refusal, simulation, world));
        }

        Assert.Equal(before, world.HashState());
    }

    // ---- the cases ------------------------------------------------------------------------------

    /// <summary>A world and a command that produces exactly this refusal in it.</summary>
    private static (Simulation Simulation, Command Command) Case(Refusal refusal)
    {
        switch (refusal)
        {
            case Refusal.ConnectWorldHasNoLattice:
            case Refusal.TripWorldHasNoLattice:
            {
                (World world, Simulation simulation) = City(Pathless);

                return (simulation, Case(refusal, simulation, world));
            }

            case Refusal.TripRulesetStatesNoTrips:
            {
                (World world, Simulation simulation) = City(Untravelled);

                return (simulation, Case(refusal, simulation, world));
            }

            case Refusal.TripOriginHoldsNoCitizen:
            {
                (World world, Simulation simulation) = Uninhabited();

                return (simulation, Case(refusal, simulation, world));
            }

            case Refusal.GovernPolicyHasNoName:
            {
                (World world, Simulation simulation) = City(Schooled + Anonymous);

                return (simulation, Case(refusal, simulation, world));
            }

            case Refusal.GovernPolicyNotInThisWorld:
            {
                (World world, Simulation simulation) = City(Schooled + Levy("first"));

                // 🔴 THE ONLY WAY TO REACH THIS REFUSAL, and it is why the row exists: PolicyTable is
                // sized at world creation and Adopt never resizes it, so a reload that GROWS the
                // declared set leaves a Policy the Ruleset names and this world cannot hold.
                world.Adopt(
                    Parse(Schooled + Levy("first") + Levy("second")),
                    0,
                    Ticks.Zero,
                    WorldKey.FromSeed(Seed));

                return (simulation, Case(refusal, simulation, world));
            }

            default:
            {
                (World world, Simulation simulation) = City(Schooled);

                return (simulation, Case(refusal, simulation, world));
            }
        }
    }

    /// <summary>The command itself, once the world it is refused in stands.</summary>
    private static Command Case(Refusal refusal, Simulation simulation, World world) => refusal switch
    {
        Refusal.None => new Command(CommandKind.Zone, new Tiles(9_000), new Tiles(9_000), 1),

        Refusal.VerbNotApplied => new Command(CommandKind.None, default, default),

        Refusal.ConnectRoadKindIsNotStreet => new Command(
            CommandKind.Connect,
            new Tiles(4_096),
            new Tiles(4_096),
            new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Arterial).Encode()),

        Refusal.ConnectWorldHasNoLattice => new Command(
            CommandKind.Connect,
            new Tiles(4_096),
            new Tiles(4_096),
            new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street).Encode()),

        Refusal.TripRulesetStatesNoTrips or Refusal.TripWorldHasNoLattice
            or Refusal.TripBlockHoldsNobody => new Command(
                CommandKind.Trip,
                new Tiles(9_000),
                new Tiles(9_000),
                new TripPayload(1, 0).Encode()),

        Refusal.TripEndpointsAreOneBuilding => Trip(world, new TripPayload(0, 0)),

        Refusal.TripOriginHoldsNoCitizen => Trip(world, new TripPayload(2, 0)),

        Refusal.ArriveNoGateOnThatTile => new Command(
            CommandKind.Arrive,
            new Tiles(9_000),
            new Tiles(9_000),
            new ArrivePayload(1, 0, 1).Encode()),

        Refusal.GovernNoSuchPolicy => Command.Govern(policy: 7, amount: 25),

        Refusal.GovernPolicyNotInThisWorld => Command.Govern(policy: 1, amount: 25),

        Refusal.GovernPolicyHasNoName => Command.Govern(policy: 0, amount: 25),

        Refusal.DemolishNoBuildingOnThatTile => new Command(
            CommandKind.Demolish, new Tiles(9_000), new Tiles(9_000)),

        Refusal.DemolishBuildingIsOccupied => Standing(world),

        Refusal.ServiceKindNotDeclared => Command.Service(
            world.Lots.East[FirstVacantLot(world)],
            world.Lots.North[FirstVacantLot(world)],
            kind: 200),

        Refusal.ServiceKindServesNothing => Command.Service(
            world.Lots.East[FirstVacantLot(world)],
            world.Lots.North[FirstVacantLot(world)],
            Dwelling),

        Refusal.ServiceNoVacantLotOnThatTile => Command.Service(
            new Tiles(9_000), new Tiles(9_000), School),

        _ => throw new Xunit.Sdk.XunitException(
            $"Refusal.{refusal} has no case, so nothing anywhere asserts that the query and the "
            + "applier agree about it."),
    };

    /// <summary>A <c>Trip</c> leaving the block the first occupied Lot stands in.</summary>
    private static Command Trip(World world, TripPayload payload)
    {
        int lot = FirstOccupiedLot(world);

        return new Command(
            CommandKind.Trip, world.Lots.East[lot], world.Lots.North[lot], payload.Encode());
    }

    /// <summary>A <c>Demolish</c> addressed at a Building somebody is still in.</summary>
    private static Command Standing(World world)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || world.Lots.IsVacant(slot))
            {
                continue;
            }

            int building = world.Lots.BuildingOn(slot);

            if (building >= 0 && !world.Buildings.IsAbandoned(building))
            {
                return new Command(
                    CommandKind.Demolish, world.Lots.East[slot], world.Lots.North[slot]);
            }
        }

        Assert.Fail("the generated city stands nobody up, so nothing can be refused for being lived in.");
        return default;
    }

    private static int FirstVacantLot(World world) => FirstLot(world, vacant: true);

    private static int FirstOccupiedLot(World world) => FirstLot(world, vacant: false);

    private static int FirstLot(World world, bool vacant)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.IsVacant(slot) == vacant)
            {
                return slot;
            }
        }

        Assert.Fail($"the generated city left no {(vacant ? "vacant" : "occupied")} Lot.");
        return -1;
    }

    // ---- the worlds -----------------------------------------------------------------------------

    private static Ruleset Parse(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static (World World, Simulation Simulation) City(string toml)
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, Parse(toml), key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    /// <summary>
    /// Two Buildings in different blocks and nobody in either — the one world in which a
    /// <c>Trip</c>'s endpoints both resolve and its origin still holds no Citizen.
    /// </summary>
    /// <remarks>
    /// <b>Hand-built rather than generated</b>, because a synthetic city houses everybody it builds
    /// for: the population is what the generator sizes the stock against, so an empty standing
    /// Building is a state it never produces.
    /// </remarks>
    private static (World World, Simulation Simulation) Uninhabited()
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, Parse(Schooled), key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        int block = world.Roads.Streets.BlockTiles;

        world.CreateBuilding(world.Lots.Create(new Tiles(0), new Tiles(0), 1), Dwelling, Ticks.Zero, key);
        world.CreateBuilding(
            world.Lots.Create(new Tiles(2 * block), new Tiles(0), 1), Dwelling, Ticks.Zero, key);

        return (world, simulation);
    }

    private const string Levied = """
        name          = "levy"
        """;

    private static string Levy(string name) => $$"""

        [[policy]]
        name = "{{name}}"
        sweeps = "household"
        interval = 2048
        apply = { min = 1, max = 1 }
        transfer = { from = "local", to = "global", resource = "money", amount = 10 }
        """;

    /// <summary>A <c>[[policy]]</c> with no <c>name</c>, which is the one thing that makes it ungovernable.</summary>
    private const string Anonymous = """

        [[policy]]
        sweeps = "household"
        interval = 2048
        apply = { min = 1, max = 1 }
        transfer = { from = "local", to = "global", resource = "money", amount = 10 }
        """;

    private const string Base = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 4
        bins = [ { resource = "sundries", capacity = 48 } ]

        [[building]]
        name = "school"
        serves = "education"

        [[zone_rule]]
        name          = "housing"
        kind          = "dwelling"
        zone          = 0
        interval      = 32
        revisit_ticks = 2048

        [placement]
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3

        [needs]
        sustenance_degrade   = 1
        sustenance_recover   = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        education_degrade    = 2
        education_recover    = 2
        floor = -1000

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;

    private const string Streets = """

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
        setback_tiles = 2
        """;

    private const string Travelled = """

        [trips]
        crossing_seconds = 30
        commute_fast_minutes = 20
        commute_moderate_minutes = 40
        commute_budget_minutes = 50
        """;

    /// <summary>Roads, Trips and a service kind — the world most cases are refused in.</summary>
    private const string Schooled = Base + Streets + Travelled;

    /// <summary>A city that travels and has no lattice to travel on.</summary>
    private const string Pathless = Base + Travelled;

    /// <summary>A city with streets and no Trip model.</summary>
    private const string Untravelled = Base + Streets;
}
