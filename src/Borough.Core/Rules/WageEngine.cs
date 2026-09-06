using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;

namespace Borough.Core.Rules;

/// <summary>
/// What one Day's payroll moved: how much, to how many, and how much went unpaid for want of funds.
/// </summary>
/// <remarks>
/// <b>Drained by its reader, on <c>PolicyEngine</c>'s precedent</b> — a magnitude accumulated between
/// two readings rather than a running total, so nothing here is a collection that grows with elapsed
/// time (<c>adr/0006</c>).
/// </remarks>
/// <param name="Paid">Money that reached a Household.</param>
/// <param name="Workers">Workers who received something.</param>
/// <param name="Shortfall">Money owed on a payday that the employer could not cover.</param>
/// <param name="Employers">Businesses whose payday came round this Day.</param>
/// <param name="Underpaying">Businesses that could not pay everybody in full.</param>
/// <param name="Bankrupted">Businesses wound up on this Day for coming up short too often.</param>
/// <param name="Tilless">
/// Businesses that declare a wage and hold no money Bin, so they can neither pay nor come up short.
/// ⚠ <b>It is a DEFECT ROW and not a census</b> — <c>plans/0065</c> <b>P1</b>. A trade's Bins come
/// from its premises, so whether it has a till depends on which building kind it ends up tenanting,
/// and nothing checks the pairing. ***A silent zero in every other row reads as health***, which is
/// why this one is reported rather than skipped.
/// </param>
public readonly record struct PayrollReading(
    long Paid, int Workers, long Shortfall, int Employers, int Underpaying,
    int Bankrupted, int Tilless);

/// <summary>
/// <b>Pays wages: the one edge in the money loop that ran in no direction until 2026-08-27.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A Household could be taxed and a Business could be levied, and nothing paid anybody</b> — so
/// money flowed one way, into Businesses and the treasury, for ever (<c>plans/0045</c>). This is the
/// return edge, and it travels the <em>employment</em> relation: from the Business a Citizen works
/// for, to the Household that Citizen belongs to.
/// </para>
/// <para>
/// 🔴 <b>It is not a <c>[[policy]]</c> and could not be one.</b> <see cref="PolicyEngine"/> moves
/// money between one member and the global treasury —
/// <c>source = From == Scope.Global ? treasury : balance</c> — so it has no way to name a
/// <em>second member</em> as the counterparty. Routing a wage through the treasury instead would
/// make it a tax-and-dividend: every worker would be paid the same by nobody in particular, and
/// ***the one thing a wage has to preserve is which employer paid it***.
/// </para>
/// <para>
/// <b>It is a Sweep Rule in shape</b> (<c>02 §4</c>, <c>adr/0033</c>): it is attached to the city
/// rather than to a Building, it fires on a cadence, and it acts where it runs. Moving it into a Bin
/// Rule family would be a change to the city and not an optimisation.
/// </para>
/// <para>
/// ⚠ <b>PROVISIONAL, and it is not what <c>adr/0026</c> describes.</b> That ADR has each Business
/// post a wage and adjust it by its own fill rate — a price that moves. This pays a flat declared
/// rate, because the posted wage is <em>unbuilt</em> and <c>adr/0070</c> says an unbuilt mechanism is
/// not a design constraint. ***When the posted wage ships, this becomes where it is paid rather than
/// what it is worth***, and the Ruleset keys go.
/// </para>
/// </remarks>
internal sealed class WageEngine(World world, WorldKey key)
{
    private readonly World _world = world;
    private readonly WorldKey _key = key;

    /// <summary>
    /// Runs every payday that falls on <paramref name="tick"/>'s Day, and nothing on other Ticks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Day boundary is a modulo and not a Wheel</b>, which is the argument the market reprice
    /// and the water graph both take one phase away: a Wheel exists so that many Day countdowns can
    /// share a structure, and one payroll pass needs none of it.
    /// </para>
    /// <para>
    /// ⚠ <b>Every Business is visited on every Day boundary and most do nothing</b>, because the
    /// payday test is per-Business — a trade's period staggers across its Businesses, so there is no
    /// Day on which the whole city is skippable. The walk is over live rows and costs one hash and
    /// two integer comparisons each.
    /// </para>
    /// </remarks>
    public PayrollReading Sweep(Ticks tick)
    {
        if (tick.Raw % (ulong)Ticks.PerDay != 0UL)
        {
            return default;
        }

        long today = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);

        long paid = 0;
        long shortfall = 0;
        int workers = 0;
        int employers = 0;
        int underpaying = 0;
        int bankrupted = 0;
        int tilless = 0;

        for (int slot = 0; slot < _world.Businesses.Rows.SlotCount; slot++)
        {
            if (!_world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            byte kind = _world.Businesses.Kind[slot];

            // ⚠ A live Business row whose kind names no declaration, which a hand-built Ruleset
            // reaches easily: UnpremisedPoolTests and BusinessTenancyTests build Businesses against
            // a Ruleset that declares no [[business]] at all, so every row carries kind 0 and
            // Ruleset.BusinessKind throws rather than answering. ***A trade that was never declared
            // pays no wage***, which is the same answer as a trade that declares no rate, so this is
            // a skip and not a refusal.
            if (kind == 0 || kind > _world.Rules.BusinessKindCount)
            {
                continue;
            }

            BusinessKindDefinition trade = _world.Rules.BusinessKind(kind);

            if (trade.WagePerDay <= 0 || trade.PayPeriodDays <= 0)
            {
                continue;
            }

            if (!IsPayday(slot, trade.PayPeriodDays, today))
            {
                continue;
            }

            employers++;

            // plans/0065 P1: a wage against no till. Counted HERE rather than inside Pay, because
            // Pay's early return cannot tell `this Business has no money Bin` from `this world names
            // no money at all` -- and only one of those is a defect.
            if (!_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out _))
            {
                tilless++;
            }

            (long moved, int reached, long owed) = Pay(slot, trade, today, tick);

            paid += moved;
            workers += reached;
            shortfall += owed;

            if (owed > 0)
            {
                underpaying++;

                // 🔴 THE CONSEQUENCE, and before this line there was none. A Business could take in
                // less than it paid out at every payday for the life of the world, and the only
                // trace was `underpaying` in a readout that survives one Tick. Measured on the build
                // before it: 7,165 premisings against ZERO give-ups over 131,072 Ticks -- nothing
                // drained a Business's money, so no shipped world could express decline and recovery
                // in one run, and every decline number in plans/0002 §D1 was unratifiable for it.
                //
                // ⚠ Saturating rather than wrapping. A trade stating no threshold never folds, so
                // this climbs unbounded without the clamp -- and a byte wrapping to 0 at 256 would
                // hand an insolvent Business a clean slate on a schedule, which is worse than
                // unbounded because it is invisible.
                byte standing = _world.Businesses.ShortPaydays[slot];

                if (standing < byte.MaxValue)
                {
                    _world.Businesses.ShortPaydays[slot] = (byte)(standing + 1);
                }

                if (GoesBankrupt(slot, trade))
                {
                    // ⚠ LAST, and nothing may read this slot afterwards. Rows.FreeSlot zeroes every
                    // column in place and pushes the slot on a free list -- it does NOT swap another
                    // row down into it -- so the sweep is safe to continue, and `underpaying` above
                    // was already counted for this payday. The bankruptcy IS this payday's outcome.
                    Bankrupt(slot);
                    bankrupted++;
                    continue;
                }
            }
            else
            {
                // ⚠ RECOVERY, and it is unconditional rather than guarded on the current value.
                // A guard would save a write on the common path and cost the branch its meaning:
                // the column's contract is `consecutive`, and the only way to keep that true is that
                // every payroll met in full ends a run. Writing zero over zero moves no hash.
                _world.Businesses.ShortPaydays[slot] = 0;
            }
        }

        return new PayrollReading(
            paid, workers, shortfall, employers, underpaying, bankrupted, tilless);
    }

    /// <summary>Whether <paramref name="slot"/>'s payday falls on <paramref name="today"/>.</summary>
    /// <remarks>
    /// <b>The offset is derived and stored nowhere</b> — see
    /// <see cref="PurposeTag.WagePayday"/>, which carries why it is keyed on the row's monotonic id
    /// and not on the Tick. ⚠ <b>A period of 1 makes this every Day whatever the offset is</b>, which
    /// is the arithmetic saying what it should: a trade paid daily has no payday to stagger.
    /// </remarks>
    private bool IsPayday(int slot, int period, long today)
    {
        ulong offset = Randomness.Draw(
            _key, _world.Businesses.Rows.IdAt(slot), new Ticks(0), PurposeTag.WagePayday)
            % (ulong)period;

        return (today + (long)offset) % period == 0;
    }

    /// <summary>
    /// Whether <paramref name="slot"/> has come up short often enough to be wound up.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Zero means never, and it is reached by omitting the key rather than by defaulting one.</b>
    /// <c>CLAUDE.md</c>'s idiom for exactly this shape — <em>absent means nobody ever gives up</em> —
    /// so a Ruleset that says nothing about insolvency keeps the behaviour the build had before this
    /// mechanism existed.
    /// </remarks>
    private bool GoesBankrupt(int slot, in BusinessKindDefinition trade) =>
        trade.GoesBankruptAfterShortPaydays > 0
        && _world.Businesses.ShortPaydays[slot] >= trade.GoesBankruptAfterShortPaydays;

    /// <summary>
    /// Winds up a Business that cannot pay its staff.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS NOT UNPREMISING, AND <c>plans/0065</c> EXISTS BECAUSE A FIRST DRAFT THOUGHT IT
    /// WAS.</b> Losing your premises and going bankrupt are two different failures that share an END
    /// rather than a route. A trade whose premises came down is <em>solvent with nowhere to trade
    /// from</em> and waits in the Unpremised Pool under <c>[placement] gives_up_after_days</c>;
    /// ***a bankrupt one is finished***. <see cref="World.UnfitBusiness"/> sends a trade's Rules and
    /// Bins away with its premises, so an unpremised trade holds no stock and runs no Rule — which is
    /// the question that separated them: <em>if a shop failed, how can it still do business?</em>
    /// </para>
    /// <para>
    /// <b>The staff are DISMISSED, and <see cref="World.DestroyBusiness"/> already spells that.</b>
    /// It pops every worker off the employer's list and leaves <c>CitizenTable.Workplace</c> — declared
    /// <c>Reference.Severable</c> for exactly this — answering ***my employer is gone*** rather than
    /// <em>I never had one</em>. ⚠ <b>It also frees every Bin the trade owned</b>, so the till goes
    /// with it, which is why the money supply is settled first.
    /// </para>
    /// <para>
    /// ⚠ <b>The <c>MoneySupply.Issued</c> decrement is the CALLER's, and this is
    /// <see cref="World.Depart(Handle{Business})"/>'s own line and its reason</b> —
    /// <c>DestroyBusiness</c> is the table operation with no economics in it. <b>Before the free, not
    /// after</b>: reading the level out of a freed Bin reads a zeroed row and the supply would drift
    /// by exactly the till's contents, which <c>Invariant.MoneyIsConserved</c> would then report at
    /// end of run with nothing pointing at the cause.
    /// </para>
    /// <para>
    /// ⚠ <b>The PREMISES ARE LEFT STANDING.</b> A bankruptcy empties a Building; it does not demolish
    /// one. <c>DestroyBusiness</c> takes the trade off <c>BuildingBusinesses</c> and stops there, so
    /// the vacancy is real and a solvent trade may take it — which is what makes a run able to go
    /// <em>balance → unbalance → balance</em> rather than merely to decline.
    /// </para>
    /// </remarks>
    private void Bankrupt(int slot)
    {
        if (_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out int till))
        {
            _world.MoneySupply.Issued[MoneySupplyTable.Slot] -=
                new Money(_world.Bins.LevelAt(till));
        }

        _world.DestroyBusiness(_world.Businesses.Rows.At(slot));
    }

    /// <summary>Pays one Business's workers, in worker-list order, until the money runs out.</summary>
    /// <remarks>
    /// <para>
    /// <b>Pro-rata from <see cref="CitizenTable.LastPaidDay"/> rather than a flat period's worth</b>,
    /// so a Citizen hired midway through a period is paid for the part they worked and an employer
    /// inherits none of another's arrears. ***A flat lump would make job churn a money supply.***
    /// </para>
    /// <para>
    /// ⚠ <b>The clock advances only for what was actually paid.</b> A worker paid nothing keeps their
    /// old <see cref="CitizenTable.LastPaidDay"/> and is owed the same Days again next payday, so a
    /// shortfall is a debt rather than a forgiveness — which is what makes an employer that cannot
    /// pay get further behind instead of quietly starting level.
    /// </para>
    /// <para>
    /// ⚠ <b>In worker-list order, which is by monotonic id</b> (<c>BusinessTable.WorkerHead</c>
    /// inserts ordered), so a short payroll pays the same people every time. That is a standing
    /// disadvantage to whoever sits at the tail and it is <em>deliberately</em> not shuffled: a wage
    /// is not a lottery, and the honest repair for it is an employer that can pay.
    /// </para>
    /// </remarks>
    private (long Paid, int Workers, long Owed) Pay(
        int slot, in BusinessKindDefinition trade, long today, Ticks tick)
    {
        if (!_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out int till))
        {
            // A world whose Ruleset names no money. Nothing holds a balance, so there is nothing to
            // pay out of and nothing to pay into -- Readouts' own answer, one table across.
            return (0, 0, 0);
        }

        long paid = 0;
        long owed = 0;
        int reached = 0;

        foreach (int worker in _world.Workers.Walk(slot))
        {
            long days = today - _world.Citizens.LastPaidDay[worker];

            if (days <= 0)
            {
                continue;
            }

            // 🔴 adr/0006's SINK, and without it this mechanism is the rule's own worked example.
            // Entitlement accrues from LastPaidDay and the clock advances only for what was actually
            // paid, so a worker at an employer that can never pay them is owed one more Day every
            // Day, for ever -- a magnitude trending upward at steady state, in a build whose
            // Definition of done forbids exactly that. Measured before this line existed: one
            // underpaying shop on provisioned.toml at a daily period ran a shortfall of 8,384 on Day
            // 14 and 499,712 on Day 56, climbing linearly with no ceiling.
            //
            // ⚠ The cap is ONE PERIOD and the remainder is FORFEIT rather than carried. A worker can
            // be at most one payday behind; Days older than that are wages nobody will ever receive,
            // which is what being unpaid means. ***Carrying them instead would make an insolvent
            // employer's debt a number that outlives the city.***
            if (days > trade.PayPeriodDays)
            {
                days = trade.PayPeriodDays;

                // Move the clock up to the start of the window being paid for, so the forfeited Days
                // cannot be claimed again on the next payday.
                _world.Citizens.LastPaidDay[worker] = (ushort)(today - trade.PayPeriodDays);
            }

            if (!_world.Households.Rows.TryResolve(
                    _world.Citizens.HouseholdOf[worker], out int household)
                || !_world.Bins.Rows.TryResolve(
                    _world.Households.Balance[household], out int purse))
            {
                continue;
            }

            long due = Borough.Core.Movement.WorkSchedule.Runs(_world)
                ? _world.Citizens.EarnedWage[worker] : days * trade.WagePerDay;
            long available = _world.Bins.LevelAt(till);

            if (available < due)
            {
                owed += due - available;
                due = available;
            }

            if (due <= 0)
            {
                continue;
            }

            // Through World's doors rather than BinTable.Move, so both writes drain their wait
            // lists -- PolicyEngine.Move's reason, and the same one applies: nothing subscribes to a
            // balance today, and going round them would make that permanent.
            _world.Withdraw(_world.Bins.Rows.At(till), due, tick);
            _world.Deposit(_world.Bins.Rows.At(purse), due, tick);

            paid += due;
            reached++;

            // Only as far as what was paid for. Integer division is exact when the employer paid in
            // full and truncates toward the last whole Day covered otherwise, so a part payment
            // leaves the remainder owed rather than rounding it away.
            if (Borough.Core.Movement.WorkSchedule.Runs(_world))
            {
                _world.Citizens.EarnedWage[worker] -= due;
                _world.Citizens.LastPaidDay[worker] = (ushort)today;
                continue;
            }
            long covered = IntegerMath.FloorDiv(due, trade.WagePerDay);
            long upTo = _world.Citizens.LastPaidDay[worker] + covered;

            _world.Citizens.LastPaidDay[worker] =
                upTo >= ushort.MaxValue ? ushort.MaxValue : (ushort)upTo;
        }

        return (paid, reached, owed);
    }
}
