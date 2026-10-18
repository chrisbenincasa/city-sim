using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>
/// What one Day's rent moved: how much reached a Landowner or the treasury, how much went unpaid,
/// and how many tenancies the deadline ended.
/// </summary>
/// <remarks>
/// <b>Drained by its reader, on <see cref="PayrollReading"/>'s precedent</b> — one sweep's totals,
/// overwritten by the next, so nothing here is a collection that grows with elapsed time
/// (<c>adr/0006</c>).
/// </remarks>
/// <param name="Collected">
/// Rent discharged, which is money that reached a Landowner or the treasury plus the mutual debt a
/// ring of Landowners cancelled. ⚠ <b>It can exceed <paramref name="Charged"/></b>, because income
/// landing between two Day boundaries settles arrears where it lands and this sweep no longer asks
/// for what it already paid off (<see cref="RentAccounts"/>).
/// </param>
/// <param name="Charged">What the Day asked for, this Day's rent plus every standing debt.</param>
/// <param name="Cancelled">
/// The share of <paramref name="Collected"/> that moved no money, because a ring of Landowners owed
/// each other and <c>RentAccounts.Net</c> cancelled the claims against one another. <b>Reported
/// beside the total rather than hidden in it</b>, on <c>MoneyFlowCounter.Withheld</c>'s reason: the
/// two arrive by different mechanisms and a reader watching balances needs to know which. So
/// <paramref name="Collected"/> less this is the cash, and that never exceeds
/// <paramref name="Charged"/> by more than the arrears income settled first.
/// </param>
/// <param name="Tenancies">Tenancies charged something.</param>
/// <param name="Behind">Tenancies still owing rent when the sweep finished.</param>
/// <param name="Evicted">Tenancies the deadline ended.</param>
public readonly record struct RentReading(
    long Collected, long Charged, long Cancelled, int Tenancies, int Behind, int Evicted);

/// <summary>
/// <b>Charges every tenancy its daily rent, carries what it could not pay, and ends the tenancy at
/// the Ruleset's deadline</b> (<c>plans/money-circulation.md</c> L7's charge, L8, L16, L17).
/// </summary>
/// <remarks>
/// <para>
/// <b>The second edge in the money loop that runs between two members of the city</b>, after
/// <see cref="WageEngine"/>, and it travels the <em>land</em> relation rather than the employment
/// one: from the tenant, to the Household holding the Lot it stands on. A city-held Lot's rent goes
/// to the treasury, which is <c>L9</c>'s <em>the city collects a held Lot's rent</em>.
/// </para>
/// <para>
/// <b>A Sweep Rule in shape</b> (<c>02 §4</c>, <c>adr/0033</c>), for <see cref="WageEngine"/>'s
/// reasons: attached to the city rather than to a Building, fires on a cadence, acts where it runs.
/// </para>
/// <para>
/// 🔴 <b>This sweep charges the rent; it is not where a debt gets paid off.</b>
/// <see cref="RentAccounts"/> settles arrears at each income site the moment money lands, which is
/// what <c>L16</c> asks for, and it holds the argument for why. ***A Day boundary is the wrong place
/// to collect a debt***, because income arrives all Day and the tenant can spend it first.
/// </para>
/// <para>
/// <b>It still runs BEHIND the payroll, the Policies and the subsidies in phase 6</b>, so a tenant
/// paid on this Day has its wage in hand before the Day's rent is asked for rather than owing a Day
/// it was always going to cover. Nothing depends on that ordering any more — the income-site
/// settlement is what keeps a tenant paid every seven Days clear at every payday.
/// </para>
/// <para>
/// ⚠ <b>Nothing runs where the Ruleset states no <c>[rent]</c></b>
/// (<see cref="RentRuleset.Collects"/>), so a file written before this mechanism existed charges
/// nobody and both debt columns stay zero for the life of the city.
/// </para>
/// </remarks>
internal sealed class RentEngine(World world)
{
    private readonly World _world = world;

    /// <summary>
    /// Charges every tenancy on <paramref name="tick"/>'s Day, and nothing on other Ticks.
    /// </summary>
    /// <remarks>
    /// <b>The Day boundary is a modulo and not a Wheel</b>, which is <see cref="WageEngine.Sweep"/>'s
    /// own line and its reason: a Wheel exists so many Day countdowns can share a structure, and one
    /// rent pass needs none of it.
    /// </remarks>
    public RentReading Sweep(Ticks tick)
    {
        if (tick.Raw % (ulong)Ticks.PerDay != 0UL || !_world.Rules.Rent.Collects)
        {
            return default;
        }

        // A world whose Ruleset names no money has nothing to pay with and nowhere for rent to
        // land, which is Pay's own answer one engine across. The loader refuses [rent] in such a
        // file; a hot reload that dropped the money Resource reaches here anyway.
        if (!_world.TryMoneyResource(out ResourceId money))
        {
            return default;
        }

        int treasury = _world.FindTreasuryBin(money);
        long today = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);
        int deadline = _world.Rules.Rent.EvictsAfterDays;

        var reading = new RentReading();

        for (int slot = 0; slot < _world.Households.Rows.SlotCount; slot++)
        {
            if (!_world.Households.Rows.IsLive(slot)
                || !_world.Buildings.Rows.IsValid(_world.Households.Dwelling[slot]))
            {
                continue;
            }

            reading = Charge(
                slot,
                _world.ResidentRent(slot),
                _world.Households.RentDebt,
                _world.Households.RentDebtSinceDay,
                _world.Households.Balance[slot],
                _world.Households.Dwelling[slot],
                treasury,
                today,
                tick,
                reading);
        }

        for (int slot = 0; slot < _world.Businesses.Rows.SlotCount; slot++)
        {
            if (!_world.Businesses.Rows.IsLive(slot)
                || !_world.Buildings.Rows.IsValid(_world.Businesses.Building[slot]))
            {
                continue;
            }

            reading = Charge(
                slot,
                _world.BusinessRent(slot),
                _world.Businesses.RentDebt,
                _world.Businesses.RentDebtSinceDay,
                _world.Businesses.Balance[slot],
                _world.Businesses.Building[slot],
                treasury,
                today,
                tick,
                reading);
        }

        reading = Deadline(today, deadline, tick, reading);

        // LAST, so the drain carries both what income settled since the previous Day and what this
        // sweep's own chain walks moved. RentAccounts is where a debt is paid off and a ring's
        // mutual claims cancel, and neither is visible in the figures above.
        (long collected, long cancelled) = _world.Arrears.Drain();

        return reading with
        {
            Collected = RentAccounts.Carry(reading.Collected, collected),
            Cancelled = cancelled,
        };
    }

    /// <summary>
    /// Ends the tenancies whose deadline has run out, once every tenancy in the sweep has been
    /// charged and paid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A SECOND PASS, and the separation is the correctness.</b> A Landowner's rent arrives
    /// when its tenant is charged, and a tenant can sit in any slot — so deciding one row's eviction
    /// while rows after it have not yet paid evicts a Landowner that the rest of the Day was about to
    /// clear. ***<see cref="World.Unplace"/> then writes the debt off***, so the money the tenant
    /// pays moments later can no longer reach the creditor it was owed to, and in a city of city-held
    /// Lots it never reaches the treasury either.
    /// </para>
    /// <para>
    /// <b>It reads the saved columns again rather than carrying a verdict forward</b>, for the same
    /// reason: whatever the charge pass computed for this row may have been overtaken by a payment,
    /// a chain walk or a ring cancellation that ran after it. <b><see cref="RentReading.Behind"/> is
    /// counted here too</b>, so the figure is what the city owes when the sweep finishes rather than
    /// a tally of rows that were behind at the moment they were charged.
    /// </para>
    /// <para>
    /// ⚠ <b>Households before Businesses, and each in slot order</b>, which is the charge pass's own
    /// order. Nothing here moves money, so the order cannot change what is collected; it fixes which
    /// tenancy a shared Building loses first and that has to be stable for replay.
    /// </para>
    /// </remarks>
    private RentReading Deadline(long today, int deadline, Ticks tick, RentReading reading)
    {
        for (int slot = 0; slot < _world.Households.Rows.SlotCount; slot++)
        {
            if (!_world.Households.Rows.IsLive(slot)
                || !_world.Buildings.Rows.IsValid(_world.Households.Dwelling[slot]))
            {
                continue;
            }

            if (!Overdue(
                _world.Households.RentDebt,
                _world.Households.RentDebtSinceDay,
                slot,
                today,
                deadline,
                ref reading))
            {
                continue;
            }

            // LAST for this row, on Bankrupt's rule: Unplace rewrites the dwelling handle and the
            // occupant list, and nothing below may read either.
            _world.Unplace(_world.Households.Rows.At(slot));
        }

        for (int slot = 0; slot < _world.Businesses.Rows.SlotCount; slot++)
        {
            if (!_world.Businesses.Rows.IsLive(slot)
                || !_world.Buildings.Rows.IsValid(_world.Businesses.Building[slot]))
            {
                continue;
            }

            if (!Overdue(
                _world.Businesses.RentDebt,
                _world.Businesses.RentDebtSinceDay,
                slot,
                today,
                deadline,
                ref reading))
            {
                continue;
            }

            _world.Unpremise(_world.Businesses.Rows.At(slot), tick);
        }

        return reading;
    }

    /// <summary>
    /// Whether one tenancy's arrears have outlived the deadline, counting it as behind or evicted
    /// either way.
    /// </summary>
    /// <remarks>
    /// <b>The clock is the authority on when the run of short Days began.</b> A fresh shortfall set
    /// it to today in the charge pass, and the loader floors the deadline at one Day, so a tenancy
    /// that only went short today cannot reach it today.
    /// </remarks>
    private static bool Overdue(
        Column<Money> debtColumn,
        Column<int> sinceColumn,
        int slot,
        long today,
        int deadline,
        ref RentReading reading)
    {
        if (debtColumn[slot].Raw <= 0)
        {
            return false;
        }

        bool evict = today - sinceColumn[slot] >= deadline;

        reading = reading with
        {
            Behind = reading.Behind + (evict ? 0 : 1),
            Evicted = reading.Evicted + (evict ? 1 : 0),
        };

        return evict;
    }

    /// <summary>
    /// Charges one tenancy this Day's rent and whatever it already owed, and settles the chain the
    /// payment travels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One body for both tenancy kinds</b>, because the two differ only in which table the
    /// columns come from. ***A Household and a Business in the same Building on the same floor pay
    /// the same rent***, which is <see cref="World.HouseholdFloorOf"/>'s own claim, so two copies of
    /// this arithmetic would be two places for that to stop being true.
    /// </para>
    /// <para>
    /// ⚠ <b>The standing debt is settled BEFORE this Day's rent, and as two steps rather than one
    /// total.</b> L16 says unpaid rent is owed and carried forward, so the tenant pays everything it
    /// holds with no reserve kept back — being behind on the rent is having this sweep empty your
    /// balance. ***Which of the two went short is what the arrears clock turns on***, so they cannot
    /// be added together: a tenant that cleared every Day it owed and then went short on today's
    /// charge starts a new run, and a tenant still carrying yesterday's debt does not.
    /// </para>
    /// <para>
    /// ⚠ <b>A tenancy with nowhere to pay is charged nothing and its clock does not move.</b> A
    /// Landowner holding no money Bin, or a city with no treasury Bin for the currency, means the
    /// rent cannot be received — so it is not owed either. ***Accruing a debt nobody can collect
    /// would evict the city's tenants for a missing Bin***, which is a defect in the world and not a
    /// tenant who cannot pay.
    /// </para>
    /// </remarks>
    private RentReading Charge(
        int slot,
        Money rent,
        Column<Money> debtColumn,
        Column<int> sinceColumn,
        Handle<Bin> balance,
        Handle<Building> premises,
        int treasury,
        long today,
        Ticks tick,
        RentReading reading)
    {
        long standing = debtColumn[slot].Raw;
        long fresh = rent.Raw;

        if (standing <= 0 && fresh <= 0)
        {
            return reading;
        }

        if (!_world.Arrears.TryPayee(premises, treasury, out int payee, out int landowner)
            || !_world.Bins.Rows.TryResolve(balance, out int purse))
        {
            return reading;
        }

        // A Household holding its own Lot would pay itself, which RentAccounts.Settle answers the
        // same way: the claim is nobody's, so write it off rather than move money onto one Bin.
        if (payee == purse)
        {
            RentAccounts.Forgive(debtColumn, sinceColumn, slot);
            return reading;
        }

        long held = _world.Bins.LevelAt(purse);

        // 🔴 TWO SUBTRACTIONS AND NEVER A SUM, which is what keeps this arithmetic inside long.
        // The loader accepts a rent rate up to long's range, so a standing debt plus one more Day's
        // charge can pass long.MaxValue -- and a wrapped total reads as nothing owed, which would
        // skip both the payment and the deadline for the rest of the city's life.
        long onStanding = long.Min(standing, held);
        long standingLeft = standing - onStanding;

        held -= onStanding;

        long onFresh = long.Min(fresh, held);
        long freshLeft = fresh - onFresh;

        long paid = onStanding + onFresh;

        if (paid > 0)
        {
            // Through World's doors rather than BinTable.Move, on Transfer's reason one engine
            // across: both writes drain their Bins' wait lists.
            _world.Withdraw(_world.Bins.Rows.At(purse), paid, tick);
            _world.Deposit(_world.Bins.Rows.At(payee), paid, tick);

            // L9: the city collects a held Lot's rent, and that is treasury income the budget owes a
            // line. TryPayee names no Landowner in exactly the city-held case.
            if (landowner == Rows.NoSlot)
            {
                _world.Arrears.CollectForTreasury(paid, tick);
            }
        }

        // 🔴 PUBLISHED BEFORE THE CHAIN IS FOLLOWED, and the order is the correctness.
        // Settling the recipient can come back round a ring to THIS tenancy, and a re-entry has to
        // read what it owes now rather than what it owed before the payment above. ***Writing these
        // after the walk would both feed the re-entry a stale debt and then overwrite what it
        // paid*** -- the tenant charged twice over for one obligation.
        debtColumn[slot] = new Money(RentAccounts.Carry(standingLeft, freshLeft));

        // A standing debt that survives leaves the clock alone, so the deadline measures the whole
        // run of short Days rather than the time since the last one. Clearing it ends that run,
        // and then only this Day's own charge decides: short, and a NEW run starts today; paid, and
        // the clock stops. ***Keeping the old Day through a full repayment would evict a tenant that
        // had settled every Day it was ever behind.***
        if (standingLeft == 0)
        {
            sinceColumn[slot] = freshLeft > 0 ? (int)today : 0;
        }

        if (paid > 0)
        {
            // Rent is income to whoever receives it, so the Landowner settles its own arrears out
            // of what just arrived rather than holding it until tomorrow -- and so does its
            // Landowner, down the whole chain. RentAccounts.Walk holds the bound and the one case it
            // leaves unfinished.
            _world.Arrears.SettleHousehold(landowner, tick);
        }

        // ⚠ NOTHING HERE DECIDES AN EVICTION, and nothing counts what is still owed. Deadline runs
        // once every tenancy has been charged and paid, because a Landowner in an earlier slot can
        // be cleared by a tenant in a later one.
        return reading with
        {
            // Saturating like Charged beside it, and for the same reason: the loader accepts a rent
            // rate up to long's range, so a city whose tenancies pay round a ring can hand the same
            // money over often enough for the Day's gross to pass the bound. A wrapped total reads
            // as a NEGATIVE rent collection.
            Collected = RentAccounts.Carry(reading.Collected, paid),
            Charged = RentAccounts.Carry(
                reading.Charged, RentAccounts.Carry(standing, fresh)),
            Tenancies = reading.Tenancies + 1,
        };
    }
}
