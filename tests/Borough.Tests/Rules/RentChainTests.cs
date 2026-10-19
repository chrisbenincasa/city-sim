using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Tests.Persistence;

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
/// moves downstream. <b>A ring is finished by cancelling the debt its members owe each other</b>,
/// which reaches the end state of circulating cash round it without bound — see
/// <see cref="A_ring_whose_debt_dwarfs_its_cash_is_cancelled_in_bounded_work"/> for the figure that
/// makes the two the same.
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

    private static Ruleset Rules() => Rules(Rate);

    private static Ruleset Rules(long rate) => new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)
        {
            Houses = true,
            RentPerFloorTile = new Money(rate),
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
    /// 🔴 <b>The property a settled ring has to have: nobody holds money against rent it owes.</b>
    /// </summary>
    /// <remarks>
    /// A tenant left holding cash it owes in rent can spend it, be levied or buy with it before the
    /// next Day — which is the whole reason a debt is settled where income lands rather than at the
    /// Day boundary. Each member either owes nothing or holds nothing.
    /// </remarks>
    private static void AssertNobodyHoldsPayableRent(World world, params int[] slots)
    {
        foreach (int slot in slots)
        {
            long debt = world.Households.RentDebt[slot].Raw;
            long held = Purse(world, slot);

            Assert.True(
                debt == 0 || held == 0,
                $"Household {slot} ends the settlement holding {held} against {debt} owed in rent.");
        }
    }

    /// <summary>Two Households each holding the ground the other lives on.</summary>
    private static (int A, int B) Ring(World world, long debt, long endowment)
    {
        (Handle<Lot> first, Handle<Household> a) = Resident(world, 0);
        (Handle<Lot> second, Handle<Household> b) = Resident(world, 128);

        world.Lots.Landowner[world.Lots.Rows.Resolve(first)] = b;
        world.Lots.Landowner[world.Lots.Rows.Resolve(second)] = a;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);

        if (endowment > 0)
        {
            world.Endow(a, new Money(endowment));
        }

        world.Households.RentDebt[aSlot] = new Money(debt);
        world.Households.RentDebt[bSlot] = new Money(debt);

        return (aSlot, bSlot);
    }

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

        // 🔴 THE READING AGREES WITH THE CITY. Behind used to be tallied where each row was charged,
        // so this chain ended with every debt at zero and reported two tenancies behind — the two
        // the payment had not reached yet at the moment they were asked.
        Assert.Equal(3, simulation.LastRent.Tenancies);
        Assert.Equal(0, simulation.LastRent.Behind);
        Assert.Equal(0, simulation.LastRent.Evicted);

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
    /// 🔴 <b>A ring of Landowners is settled whole, and nobody is left holding money it owes in
    /// rent.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A and B each hold the Lot the other lives on, so each owes the other and the only cash in the
    /// city goes round in a ring. The reviewer's figures: each owes 100, and only A holds anything —
    /// 80 of it. ***The 80 is enough to carry both debts*** because every unit either pays comes back
    /// from the other, so one settlement finishes the ring and the 80 ends where it started.
    /// </para>
    /// <para>
    /// <b>This ring needs no cancellation</b>, which is why it is worth asserting:
    /// <c>Cancelled</c> stays zero because the cash on hand reached round the ring on its own.
    /// <see cref="A_ring_whose_debt_dwarfs_its_cash_is_cancelled_in_bounded_work"/> is the case
    /// cancellation exists for.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ring_of_landowners_is_settled_whole_and_leaves_nobody_holding_payable_rent()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (int aSlot, int bSlot) = Ring(world, debt: 100, endowment: 80);

        long cash = Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world);

        // One settlement, driven directly so no Day charge is mixed into the figures.
        world.Arrears.SettleHousehold(aSlot, world.Tick);

        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(0, world.Households.RentDebtSinceDay[aSlot]);
        Assert.Equal(0, world.Households.RentDebtSinceDay[bSlot]);

        AssertNobodyHoldsPayableRent(world, aSlot, bSlot);

        // The 80 is back with the Household that started with it, because the two obligations were
        // equal and each unit paid came back from the other side.
        Assert.Equal(80, Purse(world, aSlot));
        Assert.Equal(0, Purse(world, bSlot));
        Assert.Equal(cash, Purse(world, aSlot) + Purse(world, bSlot) + Treasury(world));

        // 200 discharged for two debts of 100, all of it cash: this ring circulated rather than
        // cancelled.
        (long collected, long cancelled) = world.Arrears.Drain();

        Assert.Equal(200, collected);
        Assert.Equal(0, cancelled);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>A ring whose debt dwarfs its cash is finished by CANCELLING the mutual debt, in work
    /// set by the ring and not by the debt.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each of two Landowners owes the other <c>debt</c> and the ring holds a single unit of cash.
    /// ***Circulating that one unit would take <c>debt</c> circuits*** — a thousand for the small
    /// case here and a billion for the large one, on a rent rate the Ruleset puts no ceiling on. The
    /// cancellation reaches the same end state in one pass.
    /// </para>
    /// <para>
    /// 🔴 <b>The bound is asserted as a figure rather than as termination.</b> Every cash payment in
    /// this ring moves exactly one unit, so the cash discharged counts the payments: ***four of
    /// them, whether the debt is a thousand or a billion***. The rest of the obligation is
    /// cancelled, and the total discharged is the gross figure circulation would have recorded —
    /// twice the debt, because each side pays the whole of it.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(1_000L)]
    [InlineData(1_000_000_000L)]
    public void A_ring_whose_debt_dwarfs_its_cash_is_cancelled_in_bounded_work(long debt)
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (int aSlot, int bSlot) = Ring(world, debt, endowment: 1);

        world.Arrears.SettleHousehold(aSlot, world.Tick);

        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);

        AssertNobodyHoldsPayableRent(world, aSlot, bSlot);

        // The one unit is conserved and back where it started. Nothing was created to clear a debt
        // of a billion (adr/0003 -- a debt is not negative money).
        Assert.Equal(1, Purse(world, aSlot));
        Assert.Equal(0, Purse(world, bSlot));
        Assert.Equal(0, Treasury(world));

        (long collected, long cancelled) = world.Arrears.Drain();

        Assert.Equal(2 * debt, collected);
        Assert.Equal(4, collected - cancelled);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A ring of three cancels its SMALLEST debt and then moves what the one-sided remainder can.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pays B, B pays C, C pays A, owing 100, 50 and 100, and A holds 10. ***The cancellation is
    /// bounded by the smallest debt in the ring***, because the member owing only 50 stops passing
    /// the coin on once it is clear — so every member's debt falls by 50 and no further.
    /// </para>
    /// <para>
    /// <b>What is left is a path, and the cash on hand runs down it until it reaches the cleared
    /// member.</b> The 10 ends with B, which owes nothing: A and C still owe 40 and 50 and hold
    /// nothing, which is exactly where circulating the 10 by hand leaves them.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ring_of_three_cancels_the_smallest_debt_and_moves_the_remainder()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (Handle<Lot> ofA, Handle<Household> a) = Resident(world, 0);
        (Handle<Lot> ofB, Handle<Household> b) = Resident(world, 128);
        (Handle<Lot> ofC, Handle<Household> c) = Resident(world, 256);

        world.Lots.Landowner[world.Lots.Rows.Resolve(ofA)] = b;
        world.Lots.Landowner[world.Lots.Rows.Resolve(ofB)] = c;
        world.Lots.Landowner[world.Lots.Rows.Resolve(ofC)] = a;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);
        int cSlot = world.Households.Rows.Resolve(c);

        world.Endow(a, new Money(10));
        world.Households.RentDebt[aSlot] = new Money(100);
        world.Households.RentDebt[bSlot] = new Money(50);
        world.Households.RentDebt[cSlot] = new Money(100);

        world.Arrears.SettleHousehold(aSlot, world.Tick);

        Assert.Equal(40, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(50, world.Households.RentDebt[cSlot].Raw);

        AssertNobodyHoldsPayableRent(world, aSlot, bSlot, cSlot);

        Assert.Equal(0, Purse(world, aSlot));
        Assert.Equal(10, Purse(world, bSlot));
        Assert.Equal(0, Purse(world, cSlot));
        Assert.Equal(10, Purse(world, aSlot) + Purse(world, bSlot) + Purse(world, cSlot));

        // Gross: A paid 60, B 50 and C 50, of which 120 cancelled and 40 moved as cash.
        (long collected, long cancelled) = world.Arrears.Drain();

        Assert.Equal(160, collected);
        Assert.Equal(120, cancelled);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// ⚠ <b>A ring holding no money at all cancels nothing.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is the line between cancelling a debt and forgiving one.</b> The cancellation
    /// stands in for circuits of a coin, and a ring with no coin cannot make one — so circulation
    /// discharges nothing here and neither does the cancellation. ***Clearing these two debts would
    /// be writing off rent that was never paid***, which is not what the mutual claims come to.
    /// </para>
    /// <para>
    /// It never reaches the cancellation to be refused by it: the first settlement moves nothing, so
    /// no hop is taken and no ring is found. The behaviour is what this pins, not the route to it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ring_holding_no_money_cancels_nothing()
    {
        var world = new World(1_000, Rules(), Key);
        var simulation = new Simulation(world, Key);

        (int aSlot, int bSlot) = Ring(world, debt: 100, endowment: 0);

        world.Households.RentDebtSinceDay[aSlot] = 1;
        world.Households.RentDebtSinceDay[bSlot] = 1;

        world.Arrears.SettleHousehold(aSlot, world.Tick);
        world.Arrears.SettleHousehold(bSlot, world.Tick);

        Assert.Equal(100, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(100, world.Households.RentDebt[bSlot].Raw);

        // The arrears clock is untouched too, so the deadline still runs on both of them.
        Assert.Equal(1, world.Households.RentDebtSinceDay[aSlot]);
        Assert.Equal(1, world.Households.RentDebtSinceDay[bSlot]);

        (long collected, long cancelled) = world.Arrears.Drain();

        Assert.Equal(0, collected);
        Assert.Equal(0, cancelled);

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// ⚠ <b>The Day's gross rent SATURATES rather than wrapping negative, on a rate the loader
    /// accepts.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A ring hands the same money on at each hop, so the Day's gross is a multiple of the cash in
    /// it. Three tenancies charged four quintillion each pass one four-quintillion purse round the
    /// ring, and the twelve quintillion that reports is past <c>long</c>'s range — ***the reading
    /// used to come back as a large negative rent collection***, on a `rent_per_floor_tile` the
    /// loader refuses only for being negative.
    /// </para>
    /// <para>
    /// <b>The balances are right either way, which is what makes this a reading defect.</b> Money is
    /// conserved, every debt clears, and only the counter was wrong — so the assertion is on the
    /// counter and on the city together.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_rings_gross_rent_saturates_rather_than_wrapping_negative()
    {
        // 1e18 a Tile over the four Tiles a tenancy holds here, which is 4e18 a Day each. Three of
        // them is 1.2e19 and long stops at about 9.22e18.
        const long Steep = 1_000_000_000_000_000_000L;

        long daily = Share * Steep;

        var world = new World(1_000, Rules(Steep), Key);
        var simulation = new Simulation(world, Key);

        (Handle<Lot> ofA, Handle<Household> a) = Resident(world, 0);
        (Handle<Lot> ofB, Handle<Household> b) = Resident(world, 128);
        (Handle<Lot> ofC, Handle<Household> c) = Resident(world, 256);

        world.Lots.Landowner[world.Lots.Rows.Resolve(ofA)] = b;
        world.Lots.Landowner[world.Lots.Rows.Resolve(ofB)] = c;
        world.Lots.Landowner[world.Lots.Rows.Resolve(ofC)] = a;

        int aSlot = world.Households.Rows.Resolve(a);
        int bSlot = world.Households.Rows.Resolve(b);
        int cSlot = world.Households.Rows.Resolve(c);

        // One Day's rent for one of them is all the money in the city.
        world.Endow(a, new Money(daily));

        Assert.Equal(daily, world.ResidentRent(aSlot).Raw);

        simulation.Step(TickInput.Empty);

        // 🔴 THE DEFECT: positive and at the bound, not a negative figure.
        Assert.Equal(long.MaxValue, simulation.LastRent.Collected);
        Assert.Equal(long.MaxValue, simulation.LastRent.Charged);

        // The city itself was never wrong. Every tenancy paid, the one purse is back with A, and
        // nobody is behind or evicted.
        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[cSlot].Raw);
        Assert.Equal(daily, Purse(world, aSlot));
        Assert.Equal(0, simulation.LastRent.Evicted);
        Assert.Equal(0, simulation.LastRent.Behind);
        Assert.Equal(0, Treasury(world));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A ring settles the same way across a save and reload, and on either thread count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The cancellation reads and writes saved state only</b> — the two rent columns and the
    /// balance Bins — so a reloaded city reaches it with everything it needs. The walk's own route
    /// and marks are scratch, rebuilt per walk and never saved, and this is the test that would fail
    /// if the cancellation came to depend on them across a Tick.
    /// </para>
    /// <para>
    /// <b>Thread count cannot reach it</b>, because rent runs on the Tick's own thread and route
    /// workers only search commutes. The two Simulations run at 1 and 2 workers and the State Hash
    /// is compared every Tick, on <c>HousingConstructionTests</c>'s idiom.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_ring_settles_the_same_across_a_save_and_either_thread_count()
    {
        var world = new World(1_000, Rules(), Key);

        (int aSlot, int bSlot) = Ring(world, debt: 10 * Daily, endowment: 1);

        world.Households.RentDebtSinceDay[aSlot] = 1;
        world.Households.RentDebtSinceDay[bSlot] = 1;

        var file = new MemorySave();

        SaveFile.Write(world, 62004, file);

        World loaded = SaveFile.Read(file, world.Rules, out _);

        Assert.Equal(world.HashState(), loaded.HashState());

        var a = new Simulation(world, Key) { RouteWorkerCount = 1, VerifyDecideWritesNothing = true };
        var b = new Simulation(loaded, Key) { RouteWorkerCount = 2, VerifyDecideWritesNothing = true };

        long cancelled = 0;

        // Past the deadline, so the Day sweep charges and cancels through the whole run.
        for (int tick = 0; tick < (Deadline + 2) * Ticks.PerDay; tick++)
        {
            a.Step(TickInput.Empty);
            b.Step(TickInput.Empty);

            Assert.Equal(world.HashState(), loaded.HashState());
            Assert.Equal(a.LastRent.Cancelled, b.LastRent.Cancelled);

            cancelled += a.LastRent.Cancelled;
        }

        // The ring really was cancelled rather than merely agreeing about nothing.
        Assert.True(cancelled > 0, "no sweep cancelled any mutual debt, so this proved nothing.");

        Assert.Equal(0, world.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, loaded.Households.RentDebt[aSlot].Raw);
        Assert.Equal(0, world.Households.RentDebt[bSlot].Raw);

        a.CheckEndOfRun();
        b.CheckEndOfRun();
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
