using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 17 task 4: <c>Demolish</c> is <c>01 §2</c>'s sixth verb, and it clears abandoned stock.
/// </summary>
/// <remarks>
/// <para>
/// <b>The verb ships over the narrow half of its own scope, and the tests are shaped by which half.</b>
/// <c>adr/0091</c> makes clearing <em>occupied</em> ground a compulsory purchase paid at market value
/// off the land value Map Layer, and refuses to compose that price — so the wide half is designed,
/// named and unbuilt, blocked on the land value target. Abandoned stock needs no compensation term
/// because there is nobody left in it to compensate, and that is what could ship.
/// </para>
/// <para>
/// ⚠ <b>The refusal is asserted as hard as the success, and it is the more important of the two.</b>
/// A verb that quietly demolished an occupied Building would be a free bulldozer, and <c>adr/0091</c>'s
/// whole argument is that the price is what makes clearing a decision rather than a button. ***An
/// absence a later sitting may reason from has to read <em>refused</em> rather than <em>missing</em>***
/// (<c>adr/0070</c>), which here means the exception names the successor.
/// </para>
/// <para>
/// <b>This is not the sink and does not claim to be.</b> A shell falls on its own after
/// <c>[[building]] collapses_after_days</c> (<c>adr/0172</c>) — a player is not a sink, and the
/// measurement that settled it is a city that dies with every invariant green. What this verb buys is
/// the Lot back sooner.
/// </para>
/// </remarks>
public sealed class DemolishVerbTests
{
    private static readonly ResourceId Repairs = new(1);

    private const byte House = 1;
    private const ushort Housing = 1;

    /// <summary>How long the starving Rule waits between firings.</summary>
    private const uint Rate = 8;

    /// <summary>
    /// A kind whose one Rule draws on a Bin nothing fills, so every Building is condemned, and whose
    /// shell then stands for a full Day.
    /// </summary>
    /// <remarks>
    /// <b>A Day is the shortest a Ruleset may author</b> (<c>adr/0168</c>), and it is what gives these
    /// tests a window to click in: the whole point of a shell is that it has an extent, and a fixture
    /// that collapsed it on the sweep that found it would have nothing to demolish.
    /// </remarks>
    private static Ruleset Declining() =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 0, 0, 0),
            ],
            kinds:
            [
                new KindDefinition(0, 1, 0, 1)
                {
                    CondemnAfterTicks = 4 * (int)Rate, CollapsesAfterDays = 1, Occupants = 1,
                },
            ],
            inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],

            // A Zone Rule that condemns and never builds -- adr/0055's permission set scopes what a
            // Rule BUILDS and never which Lots it looks at. Without it the same Rule raises a new
            // Building on the Lot this verb just cleared, and every count below would say nothing.
            zoneRules: [new ZoneRuleDefinition(House, 1, 4, 4)]);

    /// <summary>Four houses in a row, one Household each, at Tiles <c>(0..3, 0)</c>.</summary>
    private static (World World, Simulation Simulation) Built(int houses = 4)
    {
        var world = new World(1_000, Declining());
        var simulation = new Simulation(world, WorldKey.FromSeed(0x0DE_C0DE_D0_11_5AEDUL))
        {
            // O(world) twice per Tick against a phase meant to be O(woken). These tests walk two
            // whole Days to watch a shell stand and fall, and with the guard on that was 16 s of a
            // 42 s assertion tier -- for a check whose own correctness is covered by the tests
            // written for it.
            VerifyDecideWritesNothing = false,
        };

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), Housing);
            Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

            world.CreateHousehold(building, lifeStage: 0);
        }

        return (world, simulation);
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>Issues one <c>demolish</c> at the named Tile on the next Tick.</summary>
    private static void Demolish(Simulation simulation, int east, int north)
    {
        Command[] commands =
        [
            new Command(CommandKind.Demolish, new Tiles(east), new Tiles(north)),
        ];

        simulation.Step(new TickInput(commands, 0));
    }

    /// <summary>How many Buildings are live and not standing empty.</summary>
    private static int Standing(World world)
    {
        int standing = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Buildings.IsAbandoned(slot))
            {
                standing++;
            }
        }

        return standing;
    }

    /// <summary>How many abandoned shells are still on their Lots.</summary>
    private static int Shells(World world)
    {
        int shells = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.IsAbandoned(slot))
            {
                shells++;
            }
        }

        return shells;
    }

    /// <summary>Runs until every Building in the fixture has been abandoned.</summary>
    /// <remarks>
    /// <b>Bounded by the collapse duration rather than by a round number.</b> A shell stands for one
    /// Day and then falls on its own, so a fixture that ran "long enough" could be looking at a city
    /// the clock had already cleared — and the test would pass for the wrong reason.
    /// </remarks>
    private static (World World, Simulation Simulation) Abandoned(int houses = 4)
    {
        (World world, Simulation simulation) = Built(houses);

        for (int i = 0; i < Ticks.PerDay && Shells(world) < houses; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(houses, Shells(world));
        Assert.Equal(0, Standing(world));

        return (world, simulation);
    }

    // ---- the verb -------------------------------------------------------------------------------

    /// <summary>A shell the player clears leaves its Lot, and its neighbours are untouched.</summary>
    [Fact]
    public void Demolishing_an_abandoned_building_clears_its_lot()
    {
        (World world, Simulation simulation) = Abandoned();

        Demolish(simulation, east: 2, north: 0);

        Assert.Equal(3, Shells(world));

        // The Lot is the point: a shell occupies one, and adr/0069 builds only on a vacant Lot, so a
        // demolition that freed the Building and left the Lot occupied would clear nothing that
        // matters. This is the assertion the verb exists to satisfy.
        Assert.True(VacantAt(world, east: 2));

        Assert.False(VacantAt(world, east: 1));
        Assert.False(VacantAt(world, east: 3));
    }

    /// <summary>Whether the Lot at this Tile carries no Building.</summary>
    private static bool VacantAt(World world, int east)
    {
        LotTable lots = world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (lots.Rows.IsLive(slot) && lots.East[slot].Raw == east && lots.North[slot].Raw == 0)
            {
                return lots.IsVacant(slot);
            }
        }

        return false;
    }

    /// <summary>
    /// 🔴 <b>An occupied Building is refused, and the refusal names its successor.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the assertion that keeps the verb from being a free bulldozer</b>, which is the one
    /// thing <c>adr/0091</c> argues at length it must not be: a verb with no cost is not governed by
    /// anything the city does. The message is asserted, not just the throw — <c>adr/0070</c> only
    /// counts an absence as evidence when it reads <em>refused</em>, and a bare exception reads as an
    /// oversight to whoever meets it.
    /// </remarks>
    [Fact]
    public void Demolishing_an_occupied_building_is_refused_by_name()
    {
        (_, Simulation simulation) = Built();

        var refusal = Assert.Throws<InvalidOperationException>(
            () => Demolish(simulation, east: 1, north: 0));

        Assert.Contains("COMPULSORY PURCHASE", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("land value", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>An occupied Building refused is an occupied Building still standing.</summary>
    /// <remarks>
    /// <b>Separate from the message test on purpose.</b> A refusal that threw <em>after</em> clearing
    /// the Lot would pass the assertion above and be the worse defect of the two, because the
    /// exception would read as though nothing had happened.
    /// </remarks>
    [Fact]
    public void A_refused_demolition_removes_nothing()
    {
        (World world, Simulation simulation) = Built();

        Assert.Throws<InvalidOperationException>(() => Demolish(simulation, east: 1, north: 0));

        Assert.Equal(4, Standing(world));
        Assert.False(VacantAt(world, east: 1));
    }

    /// <summary>A Tile with nothing on it is refused rather than resolved to a neighbour.</summary>
    /// <remarks>
    /// <b><c>ApplyTrip</c>'s rule, and it binds harder here.</b> <c>[lots] lots_per_segment</c> is
    /// five, so a block carries up to twenty Lots — a verb that answered <em>the Building in this
    /// block</em> would demolish somebody else's house on a mistyped coordinate, and a mistyped
    /// command must not be indistinguishable from the one somebody meant.
    /// </remarks>
    [Fact]
    public void Demolishing_empty_ground_is_refused()
    {
        (_, Simulation simulation) = Abandoned();

        var refusal = Assert.Throws<InvalidOperationException>(
            () => Demolish(simulation, east: 40, north: 0));

        Assert.Contains("no Building", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The shell falls on its own clock, and the verb only makes it sooner.
    /// </summary>
    /// <remarks>
    /// <b>The control for <c>adr/0172</c>, stated as a test rather than left in the ADR.</b> Both
    /// cities end with the Lot vacant; what differs is when. ⚠ <b>If this ever fails by the clock
    /// stopping, the verb has become the sink</b> — and the measurement in `adr/0172` is what that
    /// costs: a city that dies with `adr/0006` green from end to end.
    /// </remarks>
    [Fact]
    public void The_verb_is_a_shortcut_through_the_collapse_clock_and_not_a_replacement_for_it()
    {
        (World cleared, Simulation player) = Abandoned(houses: 1);

        Demolish(player, east: 0, north: 0);

        Assert.Equal(0, Shells(cleared));

        (World unattended, Simulation nobody) = Abandoned(houses: 1);

        Assert.Equal(1, Shells(unattended));

        Run(nobody, Ticks.PerDay + 8);

        Assert.Equal(0, Shells(unattended));
    }
}
