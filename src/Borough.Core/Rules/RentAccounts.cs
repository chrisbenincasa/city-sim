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

    /// <summary>The walk counter, whose current value marks <see cref="_visited"/>.</summary>
    /// <remarks>
    /// <b>Monotonic, so a mark is always greater than every mark already written</b> and no slot can
    /// read as visited before this walk touches it. A reloaded world restarts the counter at zero
    /// against an all-zero <see cref="_visited"/>, which is the same relation — so this carries no
    /// behaviour across a save, and replay and reload equivalence do not depend on its value.
    /// </remarks>
    private long _walk;

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
    /// ⚠ <b>A CYCLE of Landowners is left partly settled, and this is the known limit.</b> Where two
    /// Households each hold the Lot the other lives on, one pass moves what cash there is round the
    /// ring once and can leave a recipient holding money against a debt it still owes. Finishing it
    /// needs mutual debts NETTED rather than circulated — circulating converges in steps
    /// proportional to the debt divided by the cash, which is unbounded work — and netting is a
    /// decision about what a rent debt IS that no plan has taken. ***Unreachable today***:
    /// <c>LotTable.Landowner</c> is written only by tests and the Golden fixture, so a cycle needs
    /// the purchase slice 2 adds. <c>RentCycleTests</c> pins the current behaviour so the slice that
    /// makes it reachable finds it stated rather than surprising.
    /// </para>
    /// </remarks>
    private void Walk(int landowner, Ticks tick)
    {
        Fit();

        long mark = ++_walk;

        while (landowner != Rows.NoSlot
            && _world.Households.Rows.IsLive(landowner)
            && _visited[landowner] != mark)
        {
            _visited[landowner] = mark;
            landowner = SettleOneHousehold(landowner, tick);
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
            // Dropped rather than copied, on SubsidyEngine.Fit's reason: a mark belongs to a walk
            // that has finished, so losing it costs nothing a later walk can observe.
            _visited = new long[slots];
        }
    }
}
