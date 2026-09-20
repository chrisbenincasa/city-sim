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

    /// <summary>A second service kind, and the only one in these worlds that costs anything.</summary>
    private const byte Academy = 3;

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

            case Refusal.PeopleWorldHasNoLots:
                {
                    // A world nothing has been laid on -- which is what CommandKind.Ground now gives a
                    // player, and where a hand that lays Streets and asks for people before zoning
                    // anything ends up.
                    (World world, Simulation simulation) = Unbuilt();

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.ArriveNoSuchFamilyOutside:
                {
                    // The one shipped world whose edges hold a counted stock, so the one world in
                    // which Arrive is answerable to who actually stands out there.
                    (World world, Simulation simulation) = AttractedWorld();

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.ServiceTreasuryCannotPay:
                {
                    // The treasury opens empty (adr/0116) because no [treasury] is declared, so any
                    // price at all is more than the city holds. ⚠ This is the one refusal that
                    // turns on a LEVEL, so the world has to have a number in it rather than a shape.
                    (World world, Simulation simulation) = City(Schooled + Priced);

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.GovernPolicyHasNoName:
                {
                    (World world, Simulation simulation) = City(Schooled + Anonymous);

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.FundPolicyPaysNobody:
            case Refusal.FundCeilingIsNegative:
                {
                    (World world, Simulation simulation) = City(Schooled + Levy("first"));

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

            case Refusal.GateNoVacantLotOnThatTile:
            case Refusal.GateLotIsNotOnAnEdge:
                {
                    // The one shipped world declaring a gate kind AND a market behind every edge,
                    // so the ordered checks past the kind are the ones being reached.
                    (World world, Simulation simulation) = AttractedWorld();

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.GateLotIsOnTwoEdges:
                {
                    (World world, Simulation simulation) = AttractedWorld();

                    // 🔴 THE GENERATED CITY LEAVES NO VACANT LOT IN A CORNER, which is why this one
                    // is built rather than found. Both coordinates at zero is the only shape
                    // MapEdges.Touching answers with a count of two.
                    world.Lots.Create(new Tiles(0), new Tiles(0), zone: 0);

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.GateLotHasNoFrontage:
                {
                    (World world, Simulation simulation) = AttractedWorld();

                    // LotTable.Create does not write the frontage columns -- the subdivider is what
                    // knows the Segment -- so a Lot made here stands on an edge and is reachable
                    // from no Street. The generator never produces one, having carved them all.
                    world.Lots.Create(new Tiles(0), new Tiles(2_048), zone: 0);

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.GateEdgeHasNoHinterland:
                {
                    // A door and no market behind any edge, which no shipped Ruleset states: the
                    // three that declare a gate kind all declare four Hinterlands beside it.
                    (World world, Simulation simulation) = City(Schooled + Ported);

                    world.Lots.Create(new Tiles(0), new Tiles(2_048), zone: 0);

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.GateRemoveGateIsOccupied:
                {
                    (World world, Simulation simulation) = AttractedWorld();

                    // A port houses nobody, so no generated world stands a tenant in one. The
                    // refusal turns on the occupant list rather than on the kind, and this is the
                    // only way to put anything on it.
                    world.CreateHousehold(
                        world.Buildings.Rows.At(FirstGateBuilding(world)), lifeStage: 0);

                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.ZoneRecordLimit:
                {
                    var world = new World(0, Parse(Schooled + "\n[land_permissions]\nmax_records = 8\n"));
                    var simulation = new Simulation(world, WorldKey.FromSeed(Seed));
                    world.Roads.LayStreet(0, 0, StreetAxis.East);
                    world.PaintPermissions(new LandRectangle(0, 0, 256, 32), new GroundPermissions(1, 0));
                    return (simulation, Case(refusal, simulation, world));
                }

            case Refusal.GateTreasuryCannotPay:
                {
                    // No shipped Ruleset prices a door, so the world states one. The treasury opens
                    // empty for ServiceTreasuryCannotPay's reason, and this refusal likewise turns
                    // on a LEVEL rather than on a shape.
                    (World world, Simulation simulation) = City(Schooled + PricedPort);

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
        Refusal.ZoneNoParcel => new Command(CommandKind.ZoneParcel, new Tiles(9000), new Tiles(9000), 1),
        Refusal.ZoneInvalidBounds => new Command(CommandKind.Zone, new Tiles(-1), Tiles.Zero, 1),
        Refusal.ZoneRecordLimit => new Command(CommandKind.Zone, new Tiles(320), Tiles.Zero, 1),
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

        // At a real gate, asking for a family of nine. Every composition attracted.toml declares
        // holds one, two or four people, so this names nobody the Outside could ever supply -- which
        // is the refusal, as against a composition that exists and is spent.
        Refusal.ArriveNoSuchFamilyOutside => Arrive(world, new ArrivePayload(1, 0, 9)),

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

        Refusal.ServiceTreasuryCannotPay => Command.Service(
            world.Lots.East[FirstVacantLot(world)],
            world.Lots.North[FirstVacantLot(world)],
            Academy),

        Refusal.TaxControlNotDeclared => new Command(
            CommandKind.Tax, new Tiles(10), default, zone: 9),

        Refusal.TaxRateOutOfRange => Command.Tax(TaxControl.MiddleRate, 101),

        Refusal.TaxAllowanceIsNegative => Command.Tax(TaxControl.Allowance, -1),

        // ⚠ THE DEFAULT WORLD AUTHORS NO [income_tax], so the pending schedule is
        // IncomeTaxSchedule.None and every number in it is zero. A middle rate of 10 therefore puts
        // it above an upper rate of 0, and a threshold of -1 puts the band below an allowance of 0.
        // Both are the pair being checked rather than the value, which is RefuseTax's whole shape.
        Refusal.TaxUpperRateBelowMiddleRate => Command.Tax(TaxControl.MiddleRate, 10),

        Refusal.TaxUpperThresholdBelowAllowance => Command.Tax(TaxControl.UpperThreshold, -1),

        // The profit schedule's pair, reached the same way. The default world authors no
        // [business_tax] either, so the pending profit bands are all zero: a lower rate of 10 sits
        // above an upper rate of 0. ⚠ The negative threshold is refused on its own VALUE rather
        // than on the pair -- profit ranges below zero and its band boundary does not, which is the
        // one place these two schedules do not mirror each other.
        Refusal.TaxProfitUpperRateBelowLowerRate => Command.Tax(TaxControl.ProfitLowerRate, 10),

        Refusal.TaxProfitThresholdIsNegative => Command.Tax(TaxControl.ProfitThreshold, -1),

        // ⚠ BOTH AGAINST A NAMED TRANSFER, which is what makes the pair separable. The world's one
        // Policy is a levy, so it pays nobody and a ceiling against it is refused on the Policy --
        // but a negative ceiling is refused on its own value first, before the tool is consulted.
        Refusal.FundPolicyPaysNobody => Command.Fund(policy: 0, ceiling: 100),

        Refusal.FundCeilingIsNegative => Command.Fund(policy: 0, ceiling: -1),

        // ⚠ ONE COMMAND FOR TWO REFUSALS, because the verb carries no payload at all: what
        // distinguishes them is the WORLD it is applied to, which is what Case's world switch above
        // selects. The default world is a generated city, so it holds people.
        Refusal.PeopleWorldAlreadyHasAPopulation or Refusal.PeopleWorldHasNoLots =>
            new Command(CommandKind.People, default, default),

        // ⚠ NOT Command.Gate, which takes a byte and could not express this. The payload word is
        // sixteen bits wide and the kind id is eight, so the only way to reach the refusal is to
        // build the command the way a hand-written log would.
        Refusal.GateKindIsWiderThanAKindId => new Command(
            CommandKind.Gate, new Tiles(9_000), new Tiles(9_000), zone: 300),

        Refusal.GateKindNotDeclared => Command.Gate(
            new Tiles(9_000), new Tiles(9_000), kind: 200),

        // A dwelling is declared and states no arrivals_per_day. The Tile is never reached: the
        // kind is answered first, which is what makes the ordering assertable.
        Refusal.GateKindIsNotAnOutsideConnection => Command.Gate(
            new Tiles(9_000), new Tiles(9_000), Dwelling),

        Refusal.GateNoVacantLotOnThatTile => Command.Gate(
            new Tiles(9_000), new Tiles(9_000), GateKind(world)),

        Refusal.GateLotIsNotOnAnEdge => Gated(
            world, VacantLot(world, (_, _, touching) => touching == 0), GateKind(world)),

        Refusal.GateLotIsOnTwoEdges => Gated(
            world, VacantLot(world, (_, _, touching) => touching == 2), GateKind(world)),

        // ⚠ The discard is TYPED because the lambda's own first parameter is named `_`, and an
        // untyped `out _` binds to that int rather than discarding a HinterlandDefinition.
        Refusal.GateEdgeHasNoHinterland => Gated(
            world,
            VacantLot(world, (_, edge, touching) =>
                touching == 1 && !world.Rules.TryHinterland(edge, out HinterlandDefinition _)),
            GateKind(world)),

        Refusal.GateLotHasNoFrontage => Gated(
            world,
            VacantLot(world, (slot, edge, touching) =>
                touching == 1
                && world.Rules.TryHinterland(edge, out HinterlandDefinition _)
                && !world.Lots.HasFrontage(slot)),
            GateKind(world)),

        Refusal.GateRemoveNoGateOnThatTile => Command.Gate(
            new Tiles(9_000), new Tiles(9_000), kind: 0),

        Refusal.GateRemoveGateIsOccupied => Gated(world, FirstGateLot(world), kind: 0),

        // Every check before the price passes here -- a declared gate kind on an edge Lot with
        // frontage and a market behind it -- so the price is what is left to refuse.
        Refusal.GateTreasuryCannotPay => Gated(
            world,
            VacantLot(world, (slot, edge, touching) =>
                touching == 1
                && world.Rules.TryHinterland(edge, out HinterlandDefinition _)
                && world.Lots.HasFrontage(slot)),
            GateKind(world)),

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

    /// <summary>An <c>Arrive</c> addressed at the Tile the world's first gate stands on.</summary>
    private static Command Arrive(World world, ArrivePayload payload)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || world.Lots.IsVacant(slot))
            {
                continue;
            }

            int building = world.Lots.BuildingOn(slot);

            if (building >= 0 && world.IsOutsideConnection(world.Buildings.Kind[building]))
            {
                return new Command(
                    CommandKind.Arrive,
                    world.Lots.East[slot],
                    world.Lots.North[slot],
                    payload.Encode());
            }
        }

        // A world with no gate in it: off the map, which is refused for the other Arrive reason.
        // Asking_writes_nothing builds every case against one world and only ever queries them, so a
        // command that cannot be addressed there still has to be constructible.
        return new Command(
            CommandKind.Arrive, new Tiles(9_000), new Tiles(9_000), payload.Encode());
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

    /// <summary>
    /// A gate command against a named Lot, or off the map where this world holds no such Lot.
    /// </summary>
    private static Command Gated(World world, int lot, byte kind) =>
        lot < 0
            ? Command.Gate(new Tiles(9_000), new Tiles(9_000), kind)
            : Command.Gate(world.Lots.East[lot], world.Lots.North[lot], kind);

    /// <summary>
    /// The first vacant Lot the map's geometry answers a given way about — the slot, the edge it
    /// stands on and how many edges it touches.
    /// </summary>
    private static int VacantLot(World world, Func<int, MapEdge, int, bool> matching)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.IsVacant(slot))
            {
                continue;
            }

            int touching =
                MapEdges.Touching(world.Lots.East[slot], world.Lots.North[slot], out MapEdge edge);

            if (matching(slot, edge, touching))
            {
                return slot;
            }
        }

        return -1;
    }

    /// <summary>The first declared kind stating an <c>arrivals_per_day</c>, or zero.</summary>
    private static byte GateKind(World world)
    {
        for (int kind = 1; kind <= world.Rules.KindCount; kind++)
        {
            if (world.IsOutsideConnection((byte)kind))
            {
                return (byte)kind;
            }
        }

        return 0;
    }

    /// <summary>The first Lot holding an Outside Connection, or <c>-1</c>.</summary>
    private static int FirstGateLot(World world)
    {
        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || world.Lots.IsVacant(slot))
            {
                continue;
            }

            int building = world.Lots.BuildingOn(slot);

            if (building >= 0 && world.IsOutsideConnection(world.Buildings.Kind[building]))
            {
                return slot;
            }
        }

        return -1;
    }

    private static int FirstGateBuilding(World world)
    {
        int lot = FirstGateLot(world);

        Assert.True(lot >= 0, "the generated city raised no gate to stand a tenant in.");

        return world.Lots.BuildingOn(lot);
    }

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

    /// <summary>
    /// The shipped world whose four edges hold a counted Outside — the only one where an
    /// <c>Arrive</c> can name a family that does not exist.
    /// </summary>
    private static (World World, Simulation Simulation) AttractedWorld()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, loaded.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    /// <summary>
    /// A world with the ground laid and nothing standing on it — <c>adr/0090</c>'s world.
    /// </summary>
    /// <remarks>
    /// <b>Not populated, and that is the whole of it.</b> <see cref="City"/> calls the populator, so
    /// every world it makes already holds Lots and people; this one is what <c>CommandKind.Ground</c>
    /// leaves behind, and it is the only world in which <c>People</c> can be refused for having
    /// nowhere to put anybody.
    /// </remarks>
    private static (World World, Simulation Simulation) Unbuilt()
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, Parse(Schooled), key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

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
        houses = true
        premises = true
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
        # Inert in a world with no door into the Unplaced Pool, and required the moment one has a
        # gate kind: the loader refuses a Pool that can fill and never empties.
        gives_up_after_days = 2

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

        [capacity]
        floor_tiles_per_occupant      = 6
        floor_tiles_per_job           = 1
        floor_tiles_per_parking_space = 6
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

    /// <summary>A service kind the city has to pay for, in a city that opens with nothing.</summary>
    private const string Priced = """

        [[building]]
        name = "academy"
        serves = "education"
        placement_cost = 1000
        """;

    /// <summary>A door, and no market behind any edge for it to open onto.</summary>
    private const string Ported = """

        [[building]]
        name = "port"
        arrivals_per_day = 96
        """;

    /// <summary>A door with a price on it, and a market behind every edge to open onto.</summary>
    /// <remarks>
    /// Give the gate a nonzero placement price to exercise treasury refusal.
    /// </remarks>
    private const string PricedPort = """

        [[building]]
        name = "port"
        arrivals_per_day = 96
        placement_cost = 1000

        [[hinterland]]
        edge = "west"
        emigrant_balance_min = 800
        emigrant_balance_max = 4000

        [[hinterland]]
        edge = "south"
        emigrant_balance_min = 1200
        emigrant_balance_max = 9000

        [[hinterland]]
        edge = "east"
        emigrant_balance_min = 2000
        emigrant_balance_max = 14000

        [[hinterland]]
        edge = "north"
        emigrant_balance_min = 3000
        emigrant_balance_max = 20000
        """;
}
