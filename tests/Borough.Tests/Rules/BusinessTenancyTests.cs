using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 26 task 1: a Business runs Rules, holds Bins, and both live as long as its tenancy
/// (<c>adr/0166</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The third subject, and the reason a third was needed rather than a wider second.</b>
/// <c>adr/0141</c> gave a Rule Instance a Household subject and <c>BinTenancy</c> two values, which
/// answers <em>premises or tenant</em>. It cannot answer <em>which tenant</em> — and
/// <c>rulesets/minimal.toml</c>'s <c>dwelling</c> kind already holds both, since <c>adr/0148</c> gives
/// it an instantiated shop beside its families. ***A two-valued tenancy would have given every shop a
/// larder and a <c>consume</c>***, which is what
/// <see cref="A_shop_does_not_run_the_households_rules"/> exists to keep true.
/// </para>
/// <para>
/// ⚠ <b>No shipped Ruleset declares <c>owner = "business"</c>, so this fixture is hand-built</b>, for
/// <see cref="TenancyEndsTests"/>'s stated reason: *a mechanism no shipped content can exercise needs
/// a fixture that can, or it ships untested.* **The world that will exercise it is this milestone's
/// own Provider Ruleset**, which is task 3 and does not exist yet.
/// </para>
/// <para>
/// ⚠ <b><see cref="RuleDefinition.Tenancy"/> is set BY HAND here and the loader normally derives
/// it</b>, which is <see cref="TenancyEndsTests"/>'s filed caveat unchanged. The derivation itself is
/// <c>BinTenancyLoadTests</c>'s, including the money case that motivated the whole decision.
/// </para>
/// </remarks>
public sealed class BusinessTenancyTests
{
    private static readonly ResourceId Repairs = new(1);
    private static readonly ResourceId Larder = new(2);
    private static readonly ResourceId Stock = new(3);
    private static readonly ResourceId Money = new(4);

    private const byte Shopfront = 1;
    private const byte Trade = 1;
    private const ushort AnyZone = 1;

    private const uint Rate = 8;
    private const int Occupants = 3;

    /// <summary>
    /// A premises with three tenancies in it: the landlord's roof, a family's larder, and a trade's
    /// stock.
    /// </summary>
    /// <remarks>
    /// <b>All four Bins are declared by the KIND and that is <c>adr/0141</c> rather than an
    /// accident.</b> A ceiling is a function of <c>(building kind, Resource)</c>, so the premises
    /// declare every Bin on the Lot whoever ends up holding the level in it. ⚠ <b>The money
    /// declaration opens nothing</b> — <c>World.OpenBalance</c> does, unbounded — and it is here to
    /// exercise the skip that makes that true.
    /// </remarks>
    private static Ruleset Trading() =>
        new(
            resources:
            [
                ResourceFamily.Good, ResourceFamily.Good, ResourceFamily.Good, ResourceFamily.Money,
            ],
            rules:
            [
                // upkeep: the landlord's, producing into a Bin that fills and then blocks on space.
                new RuleDefinition(
                    Shopfront, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 0, 0, 1, 0, 0),

                // eat: a family's, drawing on a Bin nothing fills.
                new RuleDefinition(
                    Shopfront, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 0, 0, 0)
                {
                    Tenancy = BinTenancy.Occupant,
                },

                // sell: the trade's, drawing on the shop's own stock.
                new RuleDefinition(
                    Shopfront, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 1, 1, 0, 0, 0, 0)
                {
                    Tenancy = BinTenancy.Business,
                },
            ],
            kinds:
            [
                new KindDefinition(0, 4, 0, 3) { Occupants = Occupants, Business = Trade },
            ],
            inputs:
            [
                new Term(new BinRef(Scope.Local, Larder), 1),
                new Term(new BinRef(Scope.Local, Stock), 1),
            ],
            outputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            emissions: [],
            bins:
            [
                new BinDeclaration(Repairs, BinCapacity.Of(4)),
                new BinDeclaration(Larder, BinCapacity.Of(10), BinTenancy.Occupant),
                new BinDeclaration(Stock, BinCapacity.Of(96), BinTenancy.Business),
                new BinDeclaration(Money, BinCapacity.Unbounded, BinTenancy.Business),
            ],
            kindRules: [new RuleId(1), new RuleId(2), new RuleId(3)],
            zoneRules: []);

    /// <summary>One premises, one family in it, and the trade the kind comes with.</summary>
    private static (World World, Simulation Simulation, Handle<Building> Premises) Built()
    {
        var world = new World(1_000, Trading());
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0026UL))
        {
            VerifyDecideWritesNothing = true,
        };

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), AnyZone);
        Handle<Building> premises = world.CreateBuilding(
            lot, Shopfront, Ticks.Zero, simulation.Key);

        world.CreateHousehold(premises, lifeStage: 0);

        return (world, simulation, premises);
    }

    /// <summary>The one Business standing in a Building.</summary>
    private static Handle<Business> TraderIn(World world, Handle<Building> premises)
    {
        int buildingSlot = world.Buildings.Rows.Resolve(premises);

        foreach (int business in world.BuildingBusinesses.Walk(buildingSlot))
        {
            return world.Businesses.Rows.At(business);
        }

        Assert.Fail("no Business took premises in this Building.");

        return default;
    }

    private static List<int> InstancesOf(World world, Handle<Building> premises)
    {
        var found = new List<int>();

        foreach (int instance in world.BuildingRules.Walk(
            world.Buildings.Rows.Resolve(premises)))
        {
            found.Add(instance);
        }

        return found;
    }

    /// <summary>
    /// <c>adr/0139</c>'s seller finally has somewhere to keep stock, and a Rule to run over it.
    /// </summary>
    /// <remarks>
    /// <b><c>World.Unpremise</c>'s remark called this unbuilt</b> — *"a Business's stock Bins are
    /// unbuilt, and writing the sweep for them now would be a mechanism with no rows to walk."*
    /// ***There are rows to walk now***, and this is the assertion that says so.
    /// </remarks>
    [Fact]
    public void A_business_holds_its_own_stock_and_runs_its_own_rule()
    {
        (World world, _, Handle<Building> premises) = Built();
        Handle<Business> trader = TraderIn(world, premises);
        int slot = world.Businesses.Rows.Resolve(trader);

        // The stock, at the ceiling the PREMISES declared (adr/0141).
        Handle<Bin> stock = world.Businesses.BinHead[slot];
        var held = new List<ResourceId>();

        while (!stock.IsNone)
        {
            int binSlot = world.Bins.Rows.Resolve(stock);
            held.Add(world.Bins.Resource[binSlot]);
            stock = world.Bins.OwnerNext[binSlot];
        }

        Assert.Contains(Stock, held);

        // And exactly one money Bin, opened by OpenBalance rather than by the declaration -- which
        // is what makes the declaration a tenancy claim rather than an allocation.
        Assert.Single(held, resource => resource == Money);

        // The Rule, naming the Business as its subject and the Building as its place.
        List<int> mine = InstancesOf(world, premises)
            .FindAll(instance => world.RuleInstances.Business[instance] == trader);

        Assert.Single(mine);
        Assert.Equal(new RuleId(3), world.RuleInstances.Rule[mine[0]]);
        Assert.Equal(premises, world.RuleInstances.Building[mine[0]]);
        Assert.True(world.RuleInstances.Household[mine[0]].IsNone);
    }

    /// <summary>
    /// 🔴 <b>The regression the third <see cref="BinTenancy"/> exists for.</b>
    /// </summary>
    /// <remarks>
    /// One kind, two Occupants, and each runs only its own. ***Had <c>Occupant</c> simply been widened
    /// to mean "not the premises", the shop below would hold a larder and run <c>eat</c>*** — on every
    /// shipped world, silently, because <c>rulesets/minimal.toml</c>'s <c>dwelling</c> is exactly this
    /// shape.
    /// </remarks>
    [Fact]
    public void A_shop_does_not_run_the_households_rules()
    {
        (World world, _, Handle<Building> premises) = Built();
        Handle<Business> trader = TraderIn(world, premises);

        var byRule = new Dictionary<uint, (bool Trader, bool Family, bool Landlord)>();

        foreach (int instance in InstancesOf(world, premises))
        {
            byRule[world.RuleInstances.Rule[instance].Raw] = (
                !world.RuleInstances.Business[instance].IsNone,
                !world.RuleInstances.Household[instance].IsNone,
                world.RuleInstances.Business[instance].IsNone
                    && world.RuleInstances.Household[instance].IsNone);
        }

        Assert.Equal((false, false, true), byRule[1]);   // upkeep, the landlord's
        Assert.Equal((false, true, false), byRule[2]);   // eat, the family's
        Assert.Equal((true, false, false), byRule[3]);   // sell, the trade's

        // And the shop holds no larder, which is the Bin half of the same claim.
        Handle<Bin> at = world.Businesses.BinHead[world.Businesses.Rows.Resolve(trader)];

        while (!at.IsNone)
        {
            int binSlot = world.Bins.Rows.Resolve(at);

            Assert.NotEqual(Larder, world.Bins.Resource[binSlot]);

            at = world.Bins.OwnerNext[binSlot];
        }
    }

    /// <summary>
    /// A <c>local</c> term resolves to the trader's own Bin — not the premises', and not the
    /// family's.
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0041</c> G10's binary becoming a ternary</b>, asserted rather than argued.
    /// <c>World.FindLocalBin</c> tries the Household, then the Business, then the premises, and the
    /// order is not a precedence: the loader refuses a Rule addressing two owners, so at most one is
    /// ever set.
    /// </remarks>
    [Fact]
    public void A_local_term_resolves_to_the_traders_own_bin()
    {
        (World world, _, Handle<Building> premises) = Built();
        Handle<Business> trader = TraderIn(world, premises);

        int sell = InstancesOf(world, premises)
            .Find(instance => world.RuleInstances.Business[instance] == trader);

        int resolved = world.FindLocalBin(sell, Stock);

        Assert.NotEqual(Rows.NoSlot, resolved);
        Assert.Equal(BinOwnerKind.Business, world.Bins.OwnerKind[resolved]);
        Assert.Equal(96, world.Bins.Capacity[resolved]);
    }

    /// <summary>
    /// Losing the premises closes the stock and frees the Rules, and the <b>balance survives</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0144</c> and <c>adr/0142</c>, and it is what makes the widening safe.</b> Milestone
    /// 27 task 9 crashed with a <c>StaleHandleException</c> because a Rule Instance names premises
    /// unconditionally while <c>adr/0142</c> makes unpremised a legitimate steady state. ***The row
    /// cannot outlive the premises it names***, and this is the assertion that holds that true.
    /// </para>
    /// <para>
    /// ⚠ <b>What it costs is the shop's STOCK, destroyed on unpremising</b> — the same finding
    /// <c>World.FitOccupant</c> files for a tenant's larder, arriving at a seller whose inventory is
    /// the thing <c>adr/0139</c> put there.
    /// </para>
    /// </remarks>
    [Fact]
    public void Losing_the_premises_closes_the_stock_and_keeps_the_balance()
    {
        (World world, Simulation simulation, Handle<Building> premises) = Built();
        Handle<Business> trader = TraderIn(world, premises);
        int slot = world.Businesses.Rows.Resolve(trader);

        Handle<Bin> balance = world.Businesses.Balance[slot];

        Assert.False(balance.IsNone);

        world.Unpremise(trader, Ticks.Zero);

        // Every Rule Instance of this trader is gone, and the family's and the landlord's are not.
        Assert.DoesNotContain(
            InstancesOf(world, premises),
            instance => world.RuleInstances.Business[instance] == trader);

        Assert.Equal(2, InstancesOf(world, premises).Count);

        // The stock is closed and the balance is not.
        Assert.Equal(balance, world.Businesses.BinHead[slot]);
        Assert.True(world.Bins.OwnerNext[world.Bins.Rows.Resolve(balance)].IsNone);
        Assert.True(world.Bins.Rows.IsLive(world.Bins.Rows.Resolve(balance)));

        // And the world still steps, which is the StaleHandleException stated as an outcome rather
        // than as a hope.
        for (int i = 0; i < 64; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>
    /// Taking premises again reopens the stock and re-arms the Rules — the tenancy's other door.
    /// </summary>
    [Fact]
    public void Taking_premises_again_reopens_the_stock_and_rearms_the_rule()
    {
        (World world, _, Handle<Building> premises) = Built();
        Handle<Business> trader = TraderIn(world, premises);
        int slot = world.Businesses.Rows.Resolve(trader);

        world.Unpremise(trader, Ticks.Zero);
        world.Premise(trader, premises);

        Handle<Bin> at = world.Businesses.BinHead[slot];
        var held = new List<ResourceId>();

        while (!at.IsNone)
        {
            int binSlot = world.Bins.Rows.Resolve(at);
            held.Add(world.Bins.Resource[binSlot]);
            at = world.Bins.OwnerNext[binSlot];
        }

        Assert.Contains(Stock, held);
        Assert.Single(held, resource => resource == Money);

        Assert.Single(
            InstancesOf(world, premises),
            instance => world.RuleInstances.Business[instance] == trader);
    }
}
