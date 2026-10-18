namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// Where a rent debt is paid off and who it is paid to — the two questions
/// <see cref="RentEngine"/>'s Day sweep and every income site share
/// (<c>plans/money-circulation.md</c> L16).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>A debt is settled when money arrives, not at the next Day boundary.</b> L16 says unpaid
/// rent is owed and paid off as income lands, and income lands all Day: a Business sells over the
/// counter mid-Day and a Household can be levied by a Policy before the rent sweep runs. ***A debt
/// collected only at the boundary is a debt the tenant can spend its way out of***, and it also
/// leaves the arrears clock standing over money already handed back — the tenant settles everything
/// it owed and is evicted on the old deadline anyway.
/// </para>
/// <para>
/// 🔴 <b>Rent received is income too, so the whole chain settles and not just the first link.</b> A
/// Landowner that owes rent pays it out of what its tenant just handed over. In a chain A → B → C
/// that has to reach C: stopping at B leaves C holding A's money with its own debt standing, free to
/// spend it or be levied before the next Day, which is the very thing this class exists to prevent.
/// </para>
/// <para>
/// <b>An instance rather than a static class, because the walk needs a mark.</b> Following a chain
/// means revisiting Households, and a cycle — two Landowners renting from each other — would
/// otherwise run until the debt ran out. One stamp per slot bounds it; see <see cref="Walk"/>.
/// </para>
/// <para>
/// 🔴 <b>A ring of Landowners is finished by CANCELLING the debt they owe each other, not by
/// circulating cash round it.</b> Where each of a ring's members owes the next, carrying one coin
/// round the ring repeatedly discharges one unit of every member's debt per circuit — so a finish
/// costs debt-over-cash circuits, which is work proportional to the debt rather than to the city.
/// ***Cancelling the smallest debt in the ring from every member reaches the same end state in one
/// step***, and <see cref="Net"/> holds the argument and the one condition it turns on.
/// </para>
/// <para>
/// ⚠ <b>Nothing here runs where the Ruleset states no <c>[rent]</c></b>
/// (<see cref="RentRuleset.Collects"/>). Every entry point tests it first, so a file written before
/// this mechanism existed pays one bool read per income event and no more.
/// </para>
/// </remarks>
internal sealed class RentAccounts(World world)
{
    private readonly World _world = world;

    /// <summary>Which walk last visited each Household, as a <see cref="_walk"/> mark.</summary>
    /// <remarks>
    /// <b>Marked per WALK and not per Tick</b>, which is the difference that keeps two payments in
    /// one Tick independent: each income event runs its own walk under a fresh mark, so the second
    /// settles the same chain again rather than being swallowed as already visited. Grown on
    /// <c>SubsidyEngine</c>'s idiom; never saved and never hashed.
    /// </remarks>
    private long[] _visited = new long[world.Households.Rows.Capacity];

    /// <summary>Where in <see cref="_order"/> this walk settled each Household.</summary>
    /// <remarks>
    /// Read only where <see cref="_visited"/> carries the current mark, which is what makes a stale
    /// entry unreachable. ***It is how a ring is identified***: the hop that lands on a marked slot
    /// names the ring's first member, and the ring is <see cref="_order"/> from there on.
    /// </remarks>
    private int[] _step = new int[world.Households.Rows.Capacity];

    /// <summary>The Households this walk settled, in the order the payment reached them.</summary>
    private int[] _order = new int[world.Households.Rows.Capacity];

    /// <summary>How much of <see cref="_order"/> the current walk has filled.</summary>
    private int _length;

    /// <summary>The walk counter, whose current value marks <see cref="_visited"/>.</summary>
    /// <remarks>
    /// <b>Monotonic, so a mark is always greater than every mark already written</b> and no slot can
    /// read as visited before this walk touches it. A reloaded world restarts the counter at zero
    /// against an all-zero <see cref="_visited"/>, which is the same relation — so this carries no
    /// behaviour across a save, and replay and reload equivalence do not depend on its value.
    /// </remarks>
    private long _walk;

    /// <summary>Rent discharged since the last <see cref="Drain"/>, cash and cancellation together.</summary>
    private long _collected;

    /// <summary>The part of <see cref="_collected"/> that moved no cash, from <see cref="Net"/>.</summary>
    private long _cancelled;

    /// <summary>Rent cash that reached the TREASURY since the last <see cref="DrainTreasury"/>.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Treasury rent is treasury income and has to appear as its own line in the budget.</b>
    /// Every Lot in a generated city is the city's (<c>L14</c>), so on the only shipped rent fixture
    /// ***all of the rent is treasury income*** — and until this was counted the treasury rose by
    /// money no flow explained, which is the hole <c>TreasuryFlowsExplainTheBalanceTests</c> exists
    /// to keep shut.
    /// </para>
    /// <para>
    /// ⚠ <b>Cash to the treasury only.</b> Rent paid to a private Landowner moves between two
    /// members and is not the city's income, and a cancelled ring debt moves nothing at all — neither
    /// belongs here. <see cref="RentReading.Collected"/> is the figure that includes both, and it
    /// cannot stand in for this one.
    /// </para>
    /// </remarks>
    private MoneyFlow _treasuryFlow;

    /// <summary>This Tick's running total for <see cref="_treasuryFlow"/>.</summary>
    /// <remarks>
    /// <b>Folded per Tick and not per payment</b>, on <c>Simulation</c>'s placement-flow rule: a
    /// peak is what a Tick moved, not what one tenancy paid. ⚠ <b>Folded lazily, because this class
    /// has no Tick of its own</b> — it runs wherever income lands rather than as a sweep, so the
    /// fold happens when a payment arrives on a later Tick and again at the drain.
    /// </remarks>
    private long _tickToTreasury;

    /// <summary>The Tick <see cref="_tickToTreasury"/> belongs to.</summary>
    private Ticks _folded;

    /// <summary>
    /// Rent this class has discharged since the last call, which <see cref="RentEngine.Sweep"/>
    /// folds into the Day's reading.
    /// </summary>
    /// <remarks>
    /// <b>Drained rather than totalled</b>, on <c>BusinessTaxEngine.DrainCollected</c>'s terms: a
    /// running total would be a collection that grows with elapsed time (<c>adr/0006</c>). ⚠ <b>One
    /// caller only</b>, because a second reader would see each movement through whichever asked
    /// first and neither would know the other had taken it.
    /// </remarks>
    /// <returns>
    /// <c>Collected</c> is everything discharged; <c>Cancelled</c> is the share of it that cancelled
    /// against a ring rather than moving, so <c>Collected - Cancelled</c> is cash.
    /// </returns>
    internal (long Collected, long Cancelled) Drain()
    {
        (long Collected, long Cancelled) drained = (_collected, _cancelled);

        _collected = 0;
        _cancelled = 0;

        return drained;
    }

    /// <summary>
    /// Rent cash the treasury has taken since the last call, for the treasury-flow accounting.
    /// </summary>
    /// <remarks>
    /// <b>A second drain beside <see cref="Drain"/>, with a different caller and a different
    /// cadence</b>, which is why they are not one call. <see cref="Drain"/> feeds the Day's
    /// <see cref="RentReading"/> and a Day boundary takes it; this feeds
    /// <c>Simulation.DrainTreasuryFlows</c>, which a host reads on whatever interval it likes.
    /// Each has exactly one caller, on <c>BusinessTaxEngine.DrainCollected</c>'s rule.
    /// </remarks>
    internal MoneyFlow DrainTreasury()
    {
        Fold();

        MoneyFlow flow = _treasuryFlow;

        _treasuryFlow = default;

        return flow;
    }

    /// <summary>Records rent cash arriving in the treasury on <paramref name="tick"/>.</summary>
    private void Collect(long paid, Ticks tick)
    {
        if (tick != _folded)
        {
            Fold();
            _folded = tick;
        }

        _tickToTreasury += paid;
    }

    /// <summary>Folds the pending Tick into the flow. A Tick that collected nothing folds nothing.</summary>
    private void Fold()
    {
        if (_tickToTreasury > 0)
        {
            _treasuryFlow = _treasuryFlow.Fold(_tickToTreasury);
        }

        _tickToTreasury = 0;
    }

    /// <summary>
    /// Saturating addition of two nonnegative figures, so a debt or a total past <c>long</c>'s range
    /// stops at the bound instead of wrapping negative.
    /// </summary>
    /// <remarks>
    /// <b>Saturation costs nothing where it bites, because every consumer reads whether a figure is
    /// positive and not how large it is.</b> A tenant whose arrears saturate has paid nothing for a
    /// run of Days, and <see cref="RentEngine"/> ends that tenancy at the deadline whatever the
    /// column holds — so the bound caps the number without capping the consequence.
    /// </remarks>
    internal static long Carry(long left, long right) =>
        left > long.MaxValue - right ? long.MaxValue : left + right;

    /// <summary>
    /// Pays what <paramref name="slot"/> can of its standing rent debt, then settles every Landowner
    /// the payment passes through.
    /// </summary>
    internal void SettleHousehold(int slot, Ticks tick)
    {
        if (!_world.Rules.Rent.Collects || slot < 0 || !_world.Households.Rows.IsLive(slot))
        {
            return;
        }

        Walk(SettleOneHousehold(slot, tick), tick);
    }

    /// <summary>
    /// Pays what <paramref name="slot"/> can of its standing rent debt, then settles every Landowner
    /// the payment passes through.
    /// </summary>
    internal void SettleBusiness(int slot, Ticks tick)
    {
        if (!_world.Rules.Rent.Collects || slot < 0 || !_world.Businesses.Rows.IsLive(slot))
        {
            return;
        }

        int landowner = Settle(
            _world.Businesses.RentDebt,
            _world.Businesses.RentDebtSinceDay,
            _world.Businesses.Balance[slot],
            _world.Businesses.Building[slot],
            slot,
            tick);

        Walk(landowner, tick);
    }

    /// <summary>
    /// The Bin this tenancy's rent is paid into — its Lot's Landowner's balance, or the treasury
    /// while the city holds the Lot — and the Landowner's row behind it.
    /// </summary>
    /// <remarks>
    /// <b>Resolved rather than read off the column</b>, which is what <c>LotTable.LandownerOn</c> is
    /// for: a Landowner that died or departed leaves a severed handle and the Lot passed to the city
    /// with the estate, so its rent goes to the treasury from that Day on.
    /// </remarks>
    /// <param name="landowner">
    /// The Household the rent reaches, or <c>Rows.NoSlot</c> where that is the treasury. Returned
    /// alongside the Bin because a Bin cannot be resolved back to a Household —
    /// <c>BinTable.Owner</c> names a Building — and the chain walk needs the row.
    /// </param>
    internal bool TryPayee(
        Handle<Building> premises, int treasury, out int payee, out int landowner)
    {
        payee = Rows.NoSlot;
        landowner = Rows.NoSlot;

        if (!_world.Buildings.Rows.TryResolve(premises, out int buildingSlot)
            || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[buildingSlot], out int lotSlot))
        {
            return false;
        }

        int holder = _world.Lots.LandownerOn(lotSlot);

        if (holder == Rows.NoSlot)
        {
            payee = treasury;
            return treasury != Rows.NoSlot;
        }

        // An evicted Landowner keeps its Lots and their rent while it waits in the Unplaced Pool
        // (L17), so nothing here asks whether it is housed. Its balance is untouched by eviction --
        // adr/0054's "a Household keeps what it owns when the city stops housing it".
        if (!_world.Bins.Rows.TryResolve(_world.Households.Balance[holder], out payee))
        {
            return false;
        }

        landowner = holder;
        return true;
    }

    /// <summary>
    /// Records rent cash the Day sweep paid into the treasury, which
    /// <see cref="RentEngine.Charge"/> collects on its own behalf.
    /// </summary>
    /// <remarks>
    /// <b>One accumulator for both payment sites</b>, because a host reading the budget wants the
    /// city's rent income and not an accident of which of the two moved it — the Day's charge and a
    /// mid-Day arrears repayment are the same income arriving by two routes.
    /// </remarks>
    internal void CollectForTreasury(long paid, Ticks tick) => Collect(paid, tick);

    /// <summary>The money Bin the city holds rent in, or <c>Rows.NoSlot</c> where it holds none.</summary>
    internal int TreasuryBin() =>
        _world.TryMoneyResource(out ResourceId money) ? _world.FindTreasuryBin(money) : Rows.NoSlot;

    /// <summary>
    /// Writes off a debt without moving money, which is what a tenancy ending does to it
    /// (<c>adr/0003</c> — a debt is not negative money, so there is nothing to move).
    /// </summary>
    internal static void Forgive(Column<Money> debtColumn, Column<int> sinceColumn, int slot)
    {
        debtColumn[slot] = Money.Zero;
        sinceColumn[slot] = 0;
    }

    /// <summary>
    /// Settles each Landowner along the chain a payment travels, starting at
    /// <paramref name="landowner"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>ONE PASS SETTLES ANY ACYCLIC CHAIN COMPLETELY, and that is a property of the chain
    /// rather than of this loop.</b> Rent flows one way — a tenant pays its Landowner — so in a
    /// chain A → B → C the money only ever moves downstream. Visiting each Landowner once, in the
    /// order the payment reaches it, settles every debt the payment is able to settle; a second pass
    /// over a chain has nothing left to move.
    /// </para>
    /// <para>
    /// <b>The mark is the bound.</b> A hop is taken only into a slot this walk has not visited, so
    /// the walk costs at most the number of Households — ***bounded by the city and not by the size
    /// of the debt***. <b>Iterative rather than recursive</b>, so depth is a loop counter and not
    /// stack.
    /// </para>
    /// <para>
    /// ⚠ <b>The entry tenancy is NOT marked, only the hops.</b> A Household paid twice in one Tick —
    /// two wages, or a wage and a rebate — settles twice, because each payment runs its own walk
    /// under its own mark.
    /// </para>
    /// <para>
    /// ⚠ <b>A hop onto a marked slot is a RING, and it hands off to <see cref="Net"/>.</b> The
    /// marked slot is the ring's first member and <see cref="_order"/> from there on is the ring, so
    /// the walk records the route it took rather than only where it has been.
    /// </para>
    /// </remarks>
    private void Walk(int landowner, Ticks tick)
    {
        Fit();

        long mark = ++_walk;

        _length = 0;

        while (landowner != Rows.NoSlot && _world.Households.Rows.IsLive(landowner))
        {
            if (_visited[landowner] == mark)
            {
                Net(_step[landowner], tick);
                return;
            }

            _visited[landowner] = mark;
            _step[landowner] = _length;
            _order[_length++] = landowner;

            landowner = SettleOneHousehold(landowner, tick);
        }
    }

    /// <summary>
    /// Finishes a ring of Landowners by cancelling the debt they owe each other, then moving what
    /// cash the one-sided remainder can.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>CANCELLING THE RING'S SMALLEST DEBT FROM EVERY MEMBER REACHES THE SAME END STATE AS
    /// CIRCULATING CASH ROUND IT WITHOUT BOUND.</b> One coin carried round a ring of
    /// <em>n</em> members discharges one unit of each member's debt per circuit and comes back to
    /// where it started, so <em>m</em> circuits discharge <em>m</em> from everybody and leave every
    /// balance exactly as it was — where <em>m</em> is the smallest debt in the ring, because the
    /// member owing that much stops passing the coin on once it is clear. ***Circulating to that
    /// point costs debt-over-cash circuits***, which is work set by the size of the debt and not by
    /// the city, and a rent rate has no ceiling. The cancellation is one pass.
    /// </para>
    /// <para>
    /// 🔴 <b>It takes ONE UNIT OF CASH somewhere in the ring, and that condition is the whole
    /// difference between this and forgiving a debt.</b> A ring holding nothing cannot move a coin
    /// at all, so circulation discharges nothing and neither does this — ***cancelling there would
    /// write off rent nobody could ever have paid***, which is not what the mutual claims net to.
    /// Every member owes more than nothing while the cancellation runs (<c>least</c> is positive), so
    /// whichever member holds the coin can pass it on and the circuits it stands in for are real.
    /// </para>
    /// <para>
    /// ⚠ <b>A ring that got here holds the coin by construction</b>, because a hop is taken only
    /// where a payment was made and a member pays nobody outside the ring — so <c>funded</c> states
    /// the condition rather than firing. It is kept because it is what makes the cancellation
    /// legitimate: were money ever to leave a ring mid-walk, cancelling without it would be money
    /// for nothing. <c>RentChainTests.A_ring_holding_no_money_cancels_nothing</c> pins the behaviour
    /// from the outside, where the first settlement moves nothing and no ring is ever found.
    /// </para>
    /// <para>
    /// <b>The ring is a path once the cancellation has run</b>, because its smallest debt is now
    /// zero and that member pays nobody. Two laps settle a path of <em>n</em> in any starting
    /// position: a lap from the member after the cleared one computes every payment in order, and
    /// starting anywhere else needs at most one lap more to reach that position. Each payment only
    /// ever gives the next member more to pay with, so the laps cannot undo each other and a third
    /// would move nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>Money is conserved because the cancellation moves none</b> (<c>adr/0003</c> — a debt is
    /// not negative money, so there is nothing to move). The figure still reaches
    /// <see cref="Drain"/>, so the rent the city records is what circulation would have moved.
    /// </para>
    /// </remarks>
    /// <param name="first">Where in <see cref="_order"/> the ring begins.</param>
    private void Net(int first, Ticks tick)
    {
        Column<Money> debts = _world.Households.RentDebt;
        Column<int> since = _world.Households.RentDebtSinceDay;

        long least = long.MaxValue;
        bool funded = false;

        for (int at = first; at < _length; at++)
        {
            int slot = _order[at];

            least = long.Min(least, debts[slot].Raw);

            funded = funded
                || (_world.Bins.Rows.TryResolve(_world.Households.Balance[slot], out int purse)
                    && _world.Bins.LevelAt(purse) > 0);
        }

        if (least > 0 && funded)
        {
            for (int at = first; at < _length; at++)
            {
                int slot = _order[at];
                long left = debts[slot].Raw - least;

                debts[slot] = new Money(left);

                if (left == 0)
                {
                    // The clock stops where the debt does, on Settle's reason: a cancelled debt is a
                    // debt met, and leaving the Day standing would evict a tenant that owes nothing.
                    since[slot] = 0;
                }

                _collected = Carry(_collected, least);
                _cancelled = Carry(_cancelled, least);
            }
        }

        int members = _length - first;

        for (int lap = 0; lap < 2 * members; lap++)
        {
            int slot = _order[first + (lap % members)];

            if (_world.Households.Rows.IsLive(slot))
            {
                SettleOneHousehold(slot, tick);
            }
        }
    }

    private int SettleOneHousehold(int slot, Ticks tick) => Settle(
        _world.Households.RentDebt,
        _world.Households.RentDebtSinceDay,
        _world.Households.Balance[slot],
        _world.Households.Dwelling[slot],
        slot,
        tick);

    /// <summary>
    /// Pays what one tenancy can of its standing debt, and returns the Landowner it paid.
    /// </summary>
    /// <remarks>
    /// <b>One body for both tenancy kinds</b>, on <see cref="RentEngine"/>'s own reason: the two
    /// differ only in which table the columns come from.
    /// </remarks>
    /// <returns>
    /// The Household that received the payment, or <c>Rows.NoSlot</c> where nothing moved or the
    /// money reached the treasury — either way, nowhere further to follow.
    /// </returns>
    private int Settle(
        Column<Money> debtColumn,
        Column<int> sinceColumn,
        Handle<Bin> balance,
        Handle<Building> premises,
        int slot,
        Ticks tick)
    {
        long debt = debtColumn[slot].Raw;

        if (debt <= 0 || !_world.Bins.Rows.TryResolve(balance, out int purse))
        {
            return Rows.NoSlot;
        }

        if (!TryPayee(premises, TreasuryBin(), out int payee, out int landowner))
        {
            return Rows.NoSlot;
        }

        // A Household that came to hold its own Lot would pay itself, so the claim is nobody's and
        // the money must not move -- an equal withdrawal and deposit on one Bin wakes its wait list
        // twice over nothing. Writing it off is the answer eviction already gives.
        if (payee == purse)
        {
            Forgive(debtColumn, sinceColumn, slot);
            return Rows.NoSlot;
        }

        long paid = long.Min(debt, _world.Bins.LevelAt(purse));

        if (paid <= 0)
        {
            return Rows.NoSlot;
        }

        // Through World's doors rather than BinTable.Move, on PolicyEngine.Move's reason: both
        // writes drain their Bins' wait lists.
        _world.Withdraw(_world.Bins.Rows.At(purse), paid, tick);
        _world.Deposit(_world.Bins.Rows.At(payee), paid, tick);

        _collected = Carry(_collected, paid);

        // The city is the landlord of every Lot it holds (L9), so this repayment is treasury income
        // and the budget has to show it. TryPayee names no Landowner in exactly that case.
        if (landowner == Rows.NoSlot)
        {
            Collect(paid, tick);
        }

        long left = debt - paid;

        debtColumn[slot] = new Money(left);

        if (left == 0)
        {
            // 🔴 THE CLOCK STOPS WHERE THE DEBT DOES. Leaving the Day standing would evict a tenant
            // that had paid back everything it owed, on a deadline measured from a run of short Days
            // that is over.
            sinceColumn[slot] = 0;
        }

        return landowner;
    }

    /// <summary>Makes room for every Household the world now holds.</summary>
    private void Fit()
    {
        int slots = _world.Households.Rows.Capacity;

        if (_visited.Length < slots)
        {
            // Dropped rather than copied, on SubsidyEngine.Fit's reason: a mark and a route belong
            // to a walk that has finished, so losing them costs nothing a later walk can observe.
            _visited = new long[slots];
            _step = new int[slots];
            _order = new int[slots];
        }
    }
}
