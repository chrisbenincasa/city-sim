using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Rent paid to a Landowner who owes rent itself — the chain a payment travels, and how far down it
/// is settled (<c>plans/money-circulation.md</c> L16).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Rent received is income, so it pays the receiver's own arrears before anything else can
/// spend it.</b> A Landowner holding its tenant's money against a debt it still owes is the same
/// defect as a tenant holding its wage against one, one link along — so the chain is settled where
/// the payment lands and not at the next Day boundary.
/// </para>
/// <para>
/// <b>One pass is complete for an acyclic chain</b>, because rent flows one way and the money only
/// moves downstream. <b>A cycle is the known limit</b>, and
/// <see cref="A_cycle_of_landowners_terminates_and_conserves_its_money"/> pins what it does rather
/// than claiming it is finished.
/// </para>
/// <para>
/// <b>Reachable only from tests today.</b> <c>LotTable.Landowner</c> is written by nothing in the
/// simulation on this branch, so every world here is hand-built — purchase arrives with slice 2, and
/// that is the slice these assertions are addressed to.
/// </para>
/// </remarks>
public sealed class RentChainTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x2026_10_19UL);

    private const int Rate = 10;
    private const int Share = 4;
    private const int Tenancies = 2;
    private const int Daily = Share * Rate;
    private const int Deadline = 3;

    private static readonly ResourceId Cash = new(1);

    private static Ruleset Rules() => new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)
        {
            Houses = true,
            RentPerFloorTile = new Money(Rate),
        }],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: [])
    {
        Placement = new PlacementRuleset(0, 0, 0, 20, 0),
        Capacity = new CapacityRuleset(Share, 0, 0),
        Rent = new RentRuleset(Deadline),
    };

    /// <summary>A Household on its own Lot and Building, <paramref name="east"/> Tiles along.</summary>
    private static (Handle<Lot> Lot, Handle<Household> Household) Resident(World world, int east)
    {
        Handle<Lot> lot = world.Lots.Create(
            new Tiles(east), Tiles.Zero, zone: 0,
            wide: new Tiles(Share * Tenancies), deep: new Tiles(1));

        return (lot, world.CreateHousehold(
            world.CreateBuilding(lot, 1, Ticks.Zero, Key), lifeStage: 0));
    }

    private static long Purse(World world, int household) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Households.Balance[household]));

    private static long Treasury(World world) =>
        world.Bins.LevelAt(world.FindTreasuryBin(Cash));

    /// <summary>
    /// 🔴 <b>A payment through a chain of three settles every Landowner it passes, in one Tick.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// C lives on city-held ground, B rents from C, A rents from B, and only A has money. A's rent
    /// is therefore the only money in the city, and it has to reach the treasury: it pays B's debt to
    /// C, and C's debt to the city, on the Tick it was charged.
    /// </para>
    /// <para>
    /// ***Stopping anywhere short leaves a Landowner holding money it owes.*** C ending the Tick with
    /// 40 in hand and 40 owed would let a levy or a purchase take it before the next Day, which is
    /// the whole property L16 asks for.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_payment_through_a_chain_of_three_settles_every_landowner()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        // Built in chain order, so the sweep reaches C and B before the one Household that can pay.
        (Handle<Lot> ground, Handle<Household> c) = Resident(world, 0);
        (Handle<Lot> middle, Handle<Household> b) = Resident(world, 128);
        (Handle<Lot> top, Handle<Household> a) = Resident(world, 256);

        world.Lots.Landowner[world.Lots.Rows.Resolve(middle)] = c;
        world.Lots.Landowner[world.Lots.Rows.Resolve(top)] = b;

        world.Endow(a, new Money(Daily));

        int cSlot = world.Households.Rows.Resolve(c);
        int bSlot = world.Households.Rows.Resolve(b);
        int aSlot = world.Households.Rows.Resolve(a);

        Assert.True(world.Lots.IsCityHeld(world.Lots.Rows.Resolve(ground)));
        Assert.Equal(Daily, world.ResidentRent(aSlot).Raw);
        Assert.Equal(Daily, world.ResidentRent(bSlot).Raw);
        Assert.Equal(Daily, world.ResidentRent(cSlot).Raw);

        // One Tick, which is Day 0's boundary and the only charge in this test.
        simulation.Step(TickInput.Empty);

        // Nobody owes anything and nobody is holding anything: the one Day's rent travelled the
        // whole chain and left the city.
        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[cSlot].Raw);

        Assert.Equal(0, world.Households.RentDebtSinceDay[aSlot]);
        Assert.Equal(0, world.Households.RentDebtSinceDay[bSlot]);
        Assert.Equal(0, world.Households.RentDebtSinceDay[cSlot]);

        Assert.Equal(0, Purse(world, aSlot));
        Assert.Equal(0, Purse(world, bSlot));
        Assert.Equal(0, Purse(world, cSlot));

        Assert.Equal(Daily, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A chain of four settles on the same Tick, so the walk is not a special case of three.
    /// </summary>
    [Fact]
    public void A_payment_through_a_chain_of_four_settles_every_landowner()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (Handle<Lot> ground, Handle<Household> d) = Resident(world, 0);
        (Handle<Lot> third, Handle<Household> c) = Resident(world, 128);
        (Handle<Lot> second, Handle<Household> b) = Resident(world, 256);
        (Handle<Lot> top, Handle<Household> a) = Resident(world, 384);

        world.Lots.Landowner[world.Lots.Rows.Resolve(third)] = d;
        world.Lots.Landowner[world.Lots.Rows.Resolve(second)] = c;
        world.Lots.Landowner[world.Lots.Rows.Resolve(top)] = b;

        world.Endow(a, new Money(Daily));

        int[] slots =
        [
            world.Households.Rows.Resolve(a),
            world.Households.Rows.Resolve(b),
            world.Households.Rows.Resolve(c),
            world.Households.Rows.Resolve(d),
        ];

        Assert.True(world.Lots.IsCityHeld(world.Lots.Rows.Resolve(ground)));

        simulation.Step(TickInput.Empty);

        foreach (int slot in slots)
        {
            Assert.Equal(0, world.Households.RentDebt[slot].Raw);
            Assert.Equal(0, Purse(world, slot));
        }

        Assert.Equal(Daily, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// ⚠ <b>A cycle of Landowners terminates, conserves its money, and is left PARTLY settled.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A and B each hold the Lot the other lives on, so each owes the other rent and the only cash in
    /// the city goes round in a ring. ***One pass cannot finish this.*** Each settlement moves
    /// <c>min(debt, held)</c>, so where the mutual debt exceeds the cash the ring has to be walked
    /// again — and again — with each circuit moving only as much as the cash. Circulating to a
    /// finish costs debt-over-cash passes, which is work proportional to the debt and not to the
    /// city.
    /// </para>
    /// <para>
    /// <b>Finishing it needs the two debts NETTED rather than circulated</b>, and netting is a
    /// decision about what a rent debt is that no plan has taken. This test records what the walk
    /// does now, so the slice that makes a cycle reachable — purchase, in slice 2 — finds the limit
    /// written down. ***What must hold regardless is recorded here too***: the walk terminates, and
    /// money is conserved to the unit.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_cycle_of_landowners_terminates_and_conserves_its_money()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (Handle<Lot> first, Handle<Household> a) = Resident(world, 0);
        (Handle<Lot> second, Handle<Household> b) = Resident(world, 128);

        // Each holds the ground the other lives on.
        world.Lots.Landowner[world.Lots.Rows.Resolve(first)] = b;
        world.Lots.Landowner[world.Lots.Rows.Resolve(second)] = a;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);

        // The reviewer's figures: each owes 100 and only A holds anything, 80 of it.
        world.Endow(a, new Money(80));
        world.Households.RentDebt[aSlot] = new Money(100);
        world.Households.RentDebt[bSlot] = new Money(100);
        world.Households.RentDebtSinceDay[aSlot] = 0;
        world.Households.RentDebtSinceDay[bSlot] = 0;

        long cash = Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world);

        // One settlement, driven directly so no Day charge is mixed into the figures.
        world.Arrears.SettleHousehold(aSlot, world.Tick);

        // It terminated, which is the first thing a ring has to do.
        // A paid 80, B paid 80 back, then A paid its last 20 -- so A is clear and B is not.
        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(20, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(60, Purse(world, aSlot));
        Assert.Equal(20, Purse(world, bSlot));

        // 🔴 THE LIMIT, stated as an assertion rather than left in a comment: B ends the walk
        // holding exactly what it still owes, and nothing here pays it.
        Assert.Equal(world.Households.RentDebt[bSlot].Raw, Purse(world, bSlot));

        // Money is conserved to the unit, which is what makes the residue a bookkeeping limit
        // rather than a leak (adr/0003 -- a debt is not money).
        Assert.Equal(cash, Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world));

        // The residue clears on the next settlement, which is the next income or the next Day
        // sweep. Two passes finish THIS ring; a ring whose debt dwarfs its cash needs more.
        world.Arrears.SettleHousehold(bSlot, world.Tick);

        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(80, Purse(world, aSlot));
        Assert.Equal(0, Purse(world, bSlot));
        Assert.Equal(cash, Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>The Day sweep charges each tenancy once, even where the chain comes back round to it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ***This is the path that was wrong, and it was wrong by ordering alone.*** The sweep followed
    /// the recipient chain before publishing the payer's own debt, so a ring that came back round
    /// read an obligation the payment had already met — and the sweep then wrote its own figure over
    /// what that nested settlement had paid. One tenant was charged 220 against an obligation of 140.
    /// </para>
    /// <para>
    /// A and B each owe the other, and each is charged the same fresh rent, so their obligations for
    /// the Day are equal. <b>Equal obligations met in full must leave the cash where it started</b> —
    /// every unit either of them pays comes back from the other.
    /// </para>
    /// <para>
    /// <b>Driven through <see cref="Simulation.Step"/></b> and not through
    /// <c>World.Arrears.SettleHousehold</c>, because the defect lived in the sweep rather than in the
    /// settlement.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_daily_sweep_through_a_cycle_charges_each_tenancy_once()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (Handle<Lot> first, Handle<Household> a) = Resident(world, 0);
        (Handle<Lot> second, Handle<Household> b) = Resident(world, 128);

        world.Lots.Landowner[world.Lots.Rows.Resolve(first)] = b;
        world.Lots.Landowner[world.Lots.Rows.Resolve(second)] = a;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);

        // Each owes 100 already and will be charged a Day's 40 on the Tick below, so each owes 140.
        world.Endow(a, new Money(80));
        world.Households.RentDebt[aSlot] = new Money(100);
        world.Households.RentDebt[bSlot] = new Money(100);

        Assert.Equal(Daily, world.ResidentRent(aSlot).Raw);
        Assert.Equal(Daily, world.ResidentRent(bSlot).Raw);

        long cash = Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world);

        simulation.Step(TickInput.Empty);

        // Both obligations met, and the 80 is back with the Household that started with it. A sweep
        // that charged against a stale debt would have moved it to B instead.
        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(80, Purse(world, aSlot));
        Assert.Equal(0, Purse(world, bSlot));

        // Neither is evicted, which is the eviction half of the same ordering defect: the decision
        // now reads what is owed AFTER the chain has run, not before it.
        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[aSlot]));
        Assert.True(world.Buildings.Rows.IsValid(world.Households.Dwelling[bSlot]));
        Assert.Equal(0, world.UnplacedPool.Count);

        // Nothing left the ring, because neither Lot is the city's.
        Assert.Equal(0, Treasury(world));
        Assert.Equal(cash, Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Two payments in one Tick settle twice, so the walk's mark bounds a chain without swallowing
    /// independent income.
    /// </summary>
    /// <remarks>
    /// A Household can be paid more than once on a Tick — two wages, or a wage and a rebate — and
    /// each payment has to reach the rent it owes. ***The mark is per walk and not per Tick*** for
    /// exactly this reason, and this is the test that would fail if it were per Tick.
    /// </remarks>
    [Fact]
    public void Two_payments_in_one_tick_each_settle_the_whole_chain()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (Handle<Lot> ground, Handle<Household> b) = Resident(world, 0);
        (Handle<Lot> top, Handle<Household> a) = Resident(world, 128);

        world.Lots.Landowner[world.Lots.Rows.Resolve(top)] = b;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);

        // A owes two Days, B owes two Days, and A is paid in two instalments on one Tick.
        world.Households.RentDebt[aSlot] = new Money(2 * Daily);
        world.Households.RentDebt[bSlot] = new Money(2 * Daily);

        Ticks tick = world.Tick;

        world.Endow(a, new Money(Daily));
        world.Arrears.SettleHousehold(aSlot, tick);

        // The first instalment travelled the chain: A paid B, and B paid the city.
        Assert.Equal(Daily, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(Daily, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(Daily, Treasury(world));

        world.Endow(a, new Money(Daily));
        world.Arrears.SettleHousehold(aSlot, tick);

        // 🔴 THE SECOND INSTALMENT IS NOT SWALLOWED, on the same Tick as the first.
        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(0, Purse(world, aSlot));
        Assert.Equal(0, Purse(world, bSlot));
        Assert.Equal(2 * Daily, Treasury(world));

        simulation.CheckEndOfRun();
    }
}
