using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 25 task 4: a failing tenant ends the <b>tenancy</b> and leaves the premises standing.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this closes is <c>adr/0141</c>'s own sentence</b>: <em>"one starving tenant condemns
/// the other's shop."</em> <c>ZoneRuleEngine.Condemn</c> took the longest failure pressure across the
/// Building's whole Rule list and demolished the premises — and once task 2 gave every tenant its own
/// Rules, that list was full of other people's.
/// </para>
/// <para>
/// ⚠ <b>No shipped Ruleset reaches this path and that is why these fixtures are hand-built.</b> Every
/// shipped dwelling's tenant Rules are `restock` and `consume`, and `restock` fills from nothing — so
/// a tenant on any of the twelve is fed for ever and the verdict is never reached. ***A mechanism no
/// shipped content can exercise needs a fixture that can, or it ships untested.***
/// </para>
/// <para>
/// <b>The pressure mechanism is deliberately not re-tested here.</b> <c>ZoneRuleDemolishTests</c> owns
/// the duration, the two blocking reasons and the threshold; what is under test here is only
/// <em>whose</em> pressure produces <em>which</em> outcome.
/// </para>
/// </remarks>
public sealed class TenancyEndsTests
{
    private static readonly ResourceId Repairs = new(1);
    private static readonly ResourceId Food = new(2);

    private const byte House = 1;
    private const byte HousingBit = 0;
    private const ushort Housing = 1 << HousingBit;

    private const uint Rate = 8;
    private const int Condemn = 4;
    private const int Occupants = 3;

    /// <summary>
    /// A dwelling whose <b>tenants</b> starve and whose <b>premises</b> cannot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The premises Rule fails on SPACE and never on level</b>, which is
    /// <c>ZoneRuleDemolishTests.Running_out_of_space_does_not</c> used as a fixture: it produces into
    /// a Bin of capacity 4 that nothing drains, so it fills, blocks, and accumulates no pressure ever.
    /// ***That is what makes a demolition in this fixture unambiguous evidence of the defect*** — the
    /// premises have no route to a verdict of their own.
    /// </para>
    /// <para>
    /// <b>The tenant Rule draws on a Bin nothing fills</b>, which is the only way a Rule starves for
    /// ever: under <c>adr/0045</c> a failed Rule subscribes rather than retrying, so a Bin no Rule
    /// writes is a Rule that never wakes.
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="RuleDefinition.Tenancy"/> is set BY HAND here, and the loader is what normally
    /// derives it</b> from the Rule's own <c>local</c> terms. A hand-built Ruleset can therefore state
    /// a tenancy its terms contradict, and nothing outside <c>RulesetLoader</c> checks the two agree —
    /// it surfaces as <c>RuleEngine</c> throwing on a Bin the subject does not hold, which is loud and
    /// in the wrong place. Filed rather than guarded.
    /// </para>
    /// </remarks>
    private static Ruleset Tenanted(ZoneRuleDefinition[] zones) =>
        new(
            resources: [ResourceFamily.Good, ResourceFamily.Good],
            rules:
            [
                // upkeep: the premises', producing into a Bin that fills and then blocks on space.
                new RuleDefinition(
                    House, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 0, 0, 1, 0, 0),

                // eat: the tenant's, drawing on a Bin nothing fills.
                new RuleDefinition(
                    House, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 1, 0, 0, 0)
                {
                    Tenancy = BinTenancy.Occupant,
                },
            ],
            kinds:
            [
                new KindDefinition(0, 2, 0, 2) { CondemnAfter = Condemn, Occupants = Occupants },
            ],
            inputs: [new Term(new BinRef(Scope.Local, Food), 1)],
            outputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            emissions: [],
            bins:
            [
                new BinDeclaration(Repairs, BinCapacity.Of(4)),
                new BinDeclaration(Food, BinCapacity.Of(10), BinTenancy.Occupant),
            ],
            kindRules: [new RuleId(1), new RuleId(2)],
            zoneRules: zones);

    /// <summary>
    /// A Zone Rule that condemns and never builds, so a live-Building count means something.
    /// </summary>
    /// <remarks>
    /// <c>adr/0055</c>: a permission set scopes what a Zone Rule <em>builds</em> and never which Lots
    /// it looks at, so a Rule that could not have raised this Building still notices it.
    /// </remarks>
    private static ZoneRuleDefinition Watching() => new(House, HousingBit + 1, 4, 4);

    /// <summary>
    /// <paramref name="houses"/> Buildings, each full to its declared occupancy.
    /// </summary>
    private static (World World, Simulation Simulation) Built(int houses = 2)
    {
        var world = new World(1_000, Tenanted([Watching()]));
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0025UL));

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), Housing);
            Handle<Building> building = world.CreateBuilding(
                lot, House, Ticks.Zero, simulation.Key);

            for (int occupant = 0; occupant < Occupants; occupant++)
            {
                world.CreateHousehold(building, lifeStage: 0);
            }
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

    /// <summary>
    /// Every tenant starving empties the Building and leaves it standing.
    /// </summary>
    [Fact]
    public void A_starving_tenant_loses_its_tenancy_and_the_premises_stand()
    {
        (World world, Simulation simulation) = Built();

        Run(simulation, (int)Rate * (Condemn + 4));

        Assert.Equal(2, world.Buildings.Rows.LiveCount);
        Assert.Equal(6, world.UnplacedPool.Count);

        foreach (int building in new[] { 0, 1 })
        {
            Assert.True(
                world.Occupants.IsEmpty(building),
                $"Building {building} still holds a tenant whose Rules are past the threshold.");
        }
    }

    /// <summary>
    /// 🔴 <b>The defect, stated as a test: one starving tenant must not condemn the other's shop.</b>
    /// </summary>
    /// <remarks>
    /// <b>The fed tenant is fed by a deposit rather than by a Rule</b>, because the fixture has no
    /// Rule that fills <c>food</c> — that is what makes the starving one starve for ever. Depositing
    /// enough for the whole run is the smallest way to make the two tenants differ.
    /// </remarks>
    [Fact]
    public void A_fed_tenant_keeps_its_tenancy_while_its_neighbour_loses_one()
    {
        (World world, Simulation simulation) = Built(houses: 1);

        // The first occupant in the Building's list, fed for the length of the run.
        int fed = world.Occupants.PeekFront(0);

        Handle<Bin> larder = world.Households.BinHead[fed];

        Assert.False(larder.IsNone, "the tenant holds no Bin, so the fixture is not tenanted.");

        world.Deposit(larder, 10, Ticks.Zero);

        Run(simulation, (int)Rate * (Condemn + 4));

        Assert.Equal(1, world.Buildings.Rows.LiveCount);
        Assert.True(
            world.Households.Rows.IsLive(fed) && !world.Households.Dwelling[fed].IsNone,
            "the fed tenant lost its tenancy, so the verdict is not being made per tenant.");
        Assert.Equal(2, world.UnplacedPool.Count);
    }

    /// <summary>
    /// The counter that makes the second outcome visible at all.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>Ended</c> and <c>Demolished</c> never overlap.</b> A demolition ends every tenancy in
    /// the Building through <c>World.DestroyBuilding</c> and is counted once, as one Building — so a
    /// run in which the premises cannot fail must report zero demolitions however many tenancies end.
    /// </remarks>
    [Fact]
    public void The_census_counts_an_ended_tenancy_and_not_a_demolition()
    {
        (_, Simulation simulation) = Built();

        Run(simulation, (int)Rate * (Condemn + 4));

        ZoneActivity activity = simulation.Zoning.Drain();

        Assert.Equal(6, activity.Ended.Sum);
        Assert.Equal(0, activity.Demolished.Sum);
    }
}
