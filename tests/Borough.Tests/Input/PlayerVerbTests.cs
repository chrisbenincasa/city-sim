using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Tests.Space;

namespace Borough.Tests.Input;

/// <summary>
/// <c>plans/0045</c> queue item 15b: <b>the contract the shell's three ground verbs rest on.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b><c>src/Borough.Godot</c> IS NOT IN <c>Borough.slnx</c> AND CANNOT BE TESTED, so what the
/// shell decides has to be pinned from the side the suite can reach.</b> A click makes three
/// translations — a brush word, a lattice snap and an address — and each one is the shell guessing
/// at something the core already decides. ***These tests assert the core's half***, so a shell built
/// on a misreading of it goes red here rather than being wrong on screen where nothing watches.
/// </para>
/// <para>
/// ⚠ <b><c>Demolish</c> is deliberately absent</b> — <see cref="Rules.DemolishVerbTests"/> owns it,
/// including <c>Demolishing_empty_ground_is_refused</c>, which is the assertion the shell's
/// Lot-addressing exists to satisfy. ***Re-testing it here would be a second copy of one rule***,
/// which is <c>plans/0012</c> <b>Cause 1</b>.
/// </para>
/// </remarks>
public sealed class PlayerVerbTests
{
    private const byte House = 1;

    /// <summary>The one thing the shell reads to build its brush: a Zone Rule's permission word.</summary>
    private const byte Zone = 3;

    /// <summary>A world with a Street lattice and one Zone Rule to paint for.</summary>
    /// <remarks>
    /// <b>512 Tiles a block</b>, which is <see cref="RoadFixtures.Roads"/>' own wall-clock choice and
    /// not a claim: what these tests ask is structural and a 9×9 lattice answers it exactly as a real
    /// one would.
    /// </remarks>
    private static (World World, Simulation Simulation) Laid()
    {
        Ruleset roads = RoadFixtures.With(RoadFixtures.Roads(arterials: 0));
        var ruleset = new Ruleset(
            resources: [],
            rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0) { Houses = 4 > 0 , Premises = 4 > 0 }],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: [new ZoneRuleDefinition(House, Zone, 4, 4)])
        {
            Roads = roads.Roads,
            Lots = roads.Lots,
        };

        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0_10_C0DE_5EEDUL))
        {
            VerifyDecideWritesNothing = false,
        };

        // ⚠ THE WORLD'S CONSTRUCTOR LAYS NO ROADS, and the first spelling of this fixture forgot it.
        // Both Connect tests then bulldozed edges that were not there, both no-ops hashed the same,
        // and the "two axes differ" assertion failed by AGREEING -- which is the answer a test that
        // asserts a difference gives when neither side did anything at all.
        SyntheticCity.PopulateInto(world, simulation.Key, Ticks.Zero);

        return (world, simulation);
    }

    private static void Order(Simulation simulation, Command command) =>
        simulation.Step(new TickInput([command], 0));

    // ---- the brush ------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The player's whole loop in one test: lay a Street, zone the block, get Lots.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two things are asserted and the second is the one that surprised.</b> First, a Lot's
    /// <c>Zone</c> is a <b>bitmask of which Zone Rules may build there</b> — so a brush painting the
    /// rule's <em>index</em>, or a plain 1, would make Lots that read as zoned and that no Rule ever
    /// builds on. ***That failure mode is a city which silently never grows.***
    /// </para>
    /// <para>
    /// 🔴 <b>Second: <c>Zone</c> SUBDIVIDES and does not repaint.</b> <c>LotSubdivider.Face</c>
    /// returns zero on a frontage <c>World.Frontage</c> has already claimed, so zoning a block that
    /// already has Lots creates nothing and changes nobody's permissions. ***The verb is a
    /// one-time act on virgin frontage, not a brush***, and the first spelling of this test zoned a
    /// block <c>SyntheticCity</c> had already carved and reported the verb broken. The order here —
    /// Street first, then zone — is the order it actually works in.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_street_then_a_zoning_is_what_makes_lots()
    {
        (World world, Simulation simulation) = Laid();
        ushort admits = world.Rules.ZoneRules[0].Admits;

        Assert.Equal((ushort)(1 << Zone), admits);
        Assert.Equal(0, Admitted(world, admits));

        // Well clear of the synthetic city, on lattice ground it never paved.
        var east = new Tiles(4_096);
        var north = new Tiles(4_096);

        Order(simulation, new Command(
            CommandKind.Connect,
            east,
            north,
            new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street).Encode()));

        Order(simulation, new Command(CommandKind.Zone, east, north, admits));

        Assert.True(
            Admitted(world, admits) > 0,
            "a Street was laid and the block zoned, and no Lot admitted the Zone Rule");
    }

    /// <summary>
    /// 🔴 <b>Zoning a block that already has Lots does nothing at all, silently.</b>
    /// </summary>
    /// <remarks>
    /// <b>The assertion the shell needs, and it is about an absence.</b> A frontage is claimed once
    /// (<c>World.Frontage</c>), so a second <c>Zone</c> over the same ground is not an error and not
    /// an edit — it is a no-op. ***A verb whose commonest misuse is invisible needs the panel to say
    /// so before the click***, which is why the hover reports whether the block under the cursor is
    /// already subdivided.
    /// </remarks>
    [Fact]
    public void Zoning_ground_that_is_already_subdivided_changes_nothing()
    {
        (World world, Simulation simulation) = Laid();
        ushort admits = world.Rules.ZoneRules[0].Admits;
        var east = new Tiles(4_096);
        var north = new Tiles(4_096);

        Order(simulation, new Command(
            CommandKind.Connect,
            east,
            north,
            new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street).Encode()));

        Order(simulation, new Command(CommandKind.Zone, east, north, admits));

        int first = Admitted(world, admits);

        // ⚠ AGAINST A CONTROL THAT STEPS THE SAME TICK, and the first spelling did not. Comparing a
        // hash before and after `Order` compares two DIFFERENT TICKS -- every clock, wheel and
        // cadence in the world has moved -- so it failed while saying nothing about the command.
        // ***An A/B on a command has to hold the Tick count equal on both arms.***
        (World control, Simulation quiet) = Laid();

        Order(quiet, new Command(
            CommandKind.Connect,
            east,
            north,
            new ConnectPayload(StreetAxis.East, ConnectAction.Lay, RoadKind.Street).Encode()));

        Order(quiet, new Command(CommandKind.Zone, east, north, admits));

        Order(simulation, new Command(CommandKind.Zone, east, north, admits));
        quiet.Step(TickInput.Empty);

        Assert.Equal(first, Admitted(world, admits));
        Assert.Equal(control.HashState(), world.HashState());
    }

    /// <summary>How many live Lots admit the given permission.</summary>
    private static int Admitted(World world, ushort admits)
    {
        int found = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && (world.Lots.Zone[slot] & admits) != 0)
            {
                found++;
            }
        }

        return found;
    }

    // ---- the snap -------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>Any Tile inside a block names the same intersection, which is what makes a click
    /// aimable.</b>
    /// </summary>
    /// <remarks>
    /// <c>ApplyConnect</c> snaps with <c>FloorDiv</c> to the intersection <em>at or below</em> the
    /// named Tile, so the player names a place rather than a node. ⚠ <b>A cursor lands on a Tile that
    /// is almost never an intersection</b> — a block is 512 Tiles here and 32 in the shipped
    /// Ruleset — so without this the verb would be unusable by hand and only reachable from a log.
    /// </remarks>
    [Fact]
    public void Every_tile_in_a_block_lays_the_same_street()
    {
        (World corner, Simulation atCorner) = Laid();
        (World inside, Simulation wellInside) = Laid();

        ushort east = new ConnectPayload(
            StreetAxis.East, ConnectAction.Bulldoze, RoadKind.Street).Encode();

        Order(atCorner, new Command(CommandKind.Connect, new Tiles(512), new Tiles(512), east));
        Order(wellInside, new Command(CommandKind.Connect, new Tiles(999), new Tiles(700), east));

        Assert.Equal(corner.HashState(), inside.HashState());
    }

    /// <summary>
    /// And the two axes are genuinely different edges, so choosing between them is a real choice.
    /// </summary>
    /// <remarks>
    /// <b>Both sides of the decision are asserted</b>, because a shell that always sent
    /// <see cref="StreetAxis.East"/> would pass the test above and be wrong half the time — the
    /// failure that a one-sided assertion cannot see.
    /// </remarks>
    [Fact]
    public void The_two_axes_leaving_an_intersection_are_different_streets()
    {
        (World eastward, Simulation cutEast) = Laid();
        (World northward, Simulation cutNorth) = Laid();

        Order(cutEast, new Command(
            CommandKind.Connect,
            new Tiles(1_024),
            new Tiles(1_024),
            new ConnectPayload(StreetAxis.East, ConnectAction.Bulldoze, RoadKind.Street).Encode()));

        Order(cutNorth, new Command(
            CommandKind.Connect,
            new Tiles(1_024),
            new Tiles(1_024),
            new ConnectPayload(StreetAxis.North, ConnectAction.Bulldoze, RoadKind.Street).Encode()));

        Assert.NotEqual(eastward.HashState(), northward.HashState());
    }
}
