// SPDX-License-Identifier: MIT
namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Quantities;

/// <summary>
/// What one Day's profit-tax collection took: from how many, how much, and how much it had to
/// forgive.
/// </summary>
/// <remarks>
/// <b>Drained by its reader, on <see cref="PayrollReading"/>'s precedent</b> — a magnitude
/// accumulated between two readings rather than a running total, so nothing here is a collection
/// that grows with elapsed time (<c>adr/0006</c>).
/// </remarks>
/// <param name="Due">What the schedule assessed on yesterday's profits, before any till was opened.</param>
/// <param name="Collected">What actually reached the treasury. Never more than <paramref name="Due"/>.</param>
/// <param name="Taxable">Businesses that closed yesterday in profit.</param>
/// <param name="Paying">Businesses that handed over something.</param>
/// <param name="Underpaying">
/// Businesses whose till could not cover the whole bill. ⚠ <b>The remainder is FORGIVEN and not
/// carried</b> (<c>plans/0072</c> D25), so this is the only trace a short collection leaves.
/// </param>
/// <param name="Tilless">
/// Businesses assessed a bill that hold no money Bin at all, so they can neither pay nor come up
/// short. ⚠ <b>A DEFECT ROW and not a census</b>, on <c>PayrollReading.Tilless</c>'s reasoning
/// exactly: a trade's Bins come from its premises, and a trade that traded at a profit out of a
/// building kind declaring no business-owned money Bin is a Ruleset defect that would otherwise read
/// as a city nobody taxed.
/// </param>
public readonly record struct ProfitTaxReading(
    long Due, long Collected, int Taxable, int Paying, int Underpaying, int Tilless);

/// <summary>
/// <b>Collects the tax on a Business's profit, once a Day, out of its till and into the treasury.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The second half of <c>plans/0072</c>'s city income, and the mirror of <see cref="WageEngine"/>'s
/// withholding.</b> A Citizen is taxed where their wage is paid, because a wage is money moving; a
/// Business is taxed here, because profit is not money moving at all — it is a figure
/// <see cref="BusinessAccounts"/> accumulates over a Day, and the only moment it is a settled
/// quantity is after the Day it belongs to has ended.
/// </para>
/// <para>
/// 🔴 <b>IT RUNS IN PHASE 1 AND THAT IS THE WHOLE OF THE DESIGN, not a convenience.</b> A Business's
/// Day figures stand until the first recognition of the <em>next</em> Day rolls them to zero
/// (<c>BusinessTable.Roll</c>), so this sweep is racing every site that recognises revenue or
/// expense. On a Day-boundary Tick there are three of them and <b>all three run later in the same
/// Tick</b>:
/// </para>
/// <list type="bullet">
/// <item><description><b>Phase 3</b> — <c>RuleEngine.Fire</c> posts a Bin Rule's sale through
/// <c>BusinessAccounts.Deliver</c>.</description></item>
/// <item><description><b>Phase 4</b> — <c>ShoppingEngine</c> posts an over-the-counter sale through
/// the same door.</description></item>
/// <item><description><b>Phase 6</b> — <c>WageEngine.Sweep</c> accrues every employer's wage bill
/// for the Day now starting, <em>above</em> its own payday test, so it fires on every Day boundary
/// whether or not anybody is paid.</description></item>
/// </list>
/// <para>
/// ***Behind any one of them this sweep reads a rolled row, <c>ProfitOn</c> answers zero, and the
/// city is never taxed*** — silently, with every other column of every readout unchanged. Phase 2 is
/// read-only (<c>adr/0037</c>) and Phase 0 is the player's, which leaves Phase 1, and the sweep goes
/// at the head of it: <em>yesterday's books are closed before this Tick does anything at all</em>.
/// <b><c>BusinessTaxPhaseOrderTests</c> is what holds it there</b>, because a comment saying <em>must
/// run first</em> is exactly the kind of thing that survives the edit that breaks it.
/// </para>
/// <para>
/// ⚠ <b>Waking a waiter here is safe by the phase order rather than by anything this does.</b>
/// <c>World.Withdraw</c> and <c>World.Deposit</c> drain their Bins' wait lists, and
/// <c>World.Wake</c> arms the freed Rule Instance on the Wheel. This runs <em>before</em>
/// <c>RuleEngine.CollectDue</c> has taken any row out, which is the same window
/// <c>World.Unlink</c> names for a Ruleset reload in phase 0 — so no row the engine is holding can
/// be touched.
/// </para>
/// <para>
/// <b>It is a Sweep Rule in shape</b> (<c>02 §4</c>, <c>adr/0033</c>) for <see cref="WageEngine"/>'s
/// reasons, and it could not be a <c>[[policy]]</c> for a further one: a Policy transfers a stated
/// <em>amount</em> against a Readout, and a tax bill is a marginal schedule read against a quantity
/// no Readout names.
/// </para>
/// </remarks>
internal sealed class BusinessTaxEngine(World world)
{
    private readonly World _world = world;

    /// <summary>
    /// Profit tax collected since the last <see cref="DrainCollected"/>.
    /// </summary>
    /// <remarks>
    /// <b>Accumulated and drained, on <c>WageEngine._withheldFlow</c>'s precedent and for its
    /// reason</b> — a Census observes on an interval that several Day boundaries fall inside, and a
    /// flow has no value at an instant. ⚠ <b>Folded once per Day rather than once per Tick, and that
    /// is the same number</b>: this sweep returns on every Tick that is not a Day boundary, and
    /// folding a zero would move neither <see cref="MoneyFlow.Sum"/> nor <see cref="MoneyFlow.Peak"/>.
    /// It is not simulation state and folds into no State Hash.
    /// </remarks>
    private MoneyFlow _collectedFlow;

    /// <summary>Reads what the collections took since the last call, and resets the accumulator.</summary>
    /// <returns>The interval's collection: everything taken, and the most taken on one Tick.</returns>
    public MoneyFlow DrainCollected()
    {
        MoneyFlow flow = _collectedFlow;

        _collectedFlow = default;

        return flow;
    }

    /// <summary>
    /// Assesses every live Business on <em>yesterday's</em> profit and collects what it can, on a
    /// Day boundary and nothing on other Ticks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Yesterday and never today, and the off-by-one is the mechanism.</b> A Day boundary is the
    /// first Tick of a Day, so the Day that has just ended is <c>today - 1</c> and the figures on
    /// the row still belong to it. Assessing <c>today</c> would assess a Day that has not happened.
    /// </para>
    /// <para>
    /// 🔴 <b>A world stating no <c>[business_tax]</c> returns before it writes anything at all</b> —
    /// <see cref="WageEngine.Pay"/>'s <c>bool levies</c> guard, and the reason is the same one: an
    /// untaxed city must not have its State Hash moved by a tax it does not have. ⚠ <b>The guard is
    /// <see cref="BusinessTaxSchedule.Levies"/> and not <em>the table is present</em></b>, because a
    /// schedule stating two rates of zero takes nothing from anybody and a sweep that moved zero
    /// money would still have walked the Bins and drained their wait lists. ⚠ <b>And it is asked of
    /// the schedule the HISTORY resolves rather than of the authored one</b>, so a city whose
    /// Ruleset levies nothing and whose player has set a rate is taxed under the rate they set.
    /// </para>
    /// <para>
    /// ⚠ <b>Tick 0 is Day 0 and there is no Day before it</b>, so the first boundary the city ever
    /// sees collects nothing. A <c>ushort</c> Day of zero would underflow to 65,535 and assess a Day
    /// no row has ever carried — which is a nothing rather than a defect, but only by luck.
    /// </para>
    /// </remarks>
    /// <param name="tick">The Tick about to run.</param>
    /// <returns>What this Day's collection took, or zeroes on any other Tick.</returns>
    public ProfitTaxReading Sweep(Ticks tick)
    {
        if (tick.Raw % (ulong)Ticks.PerDay != 0UL)
        {
            return default;
        }

        ushort today = BusinessAccounts.DayOf(tick);

        if (today == 0)
        {
            return default;
        }

        ushort yesterday = (ushort)(today - 1);

        // 🔴 THE SCHEDULE IN FORCE ON THE DAY BEING ASSESSED, and not the one in force today.
        // A Day's profit is a fact about that Day, so a rate a player moved overnight must not reach
        // back over trading that is already finished -- <c>WageEngine.Withhold</c>'s own line, which
        // consults the history per earning Day for the same reason. The authored schedule is the
        // fall-through, so a world whose player has governed nothing reads the Ruleset.
        BusinessTaxSchedule schedule =
            _world.IncomeTaxRates.ProfitScheduleFor(yesterday, _world.Rules.BusinessTax);

        if (!schedule.Levies)
        {
            return default;
        }

        long due = 0;
        long collected = 0;
        int taxable = 0;
        int paying = 0;
        int shortTill = 0;
        int tilless = 0;

        for (int slot = 0; slot < _world.Businesses.Rows.SlotCount; slot++)
        {
            if (!_world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            long profit = _world.Businesses.ProfitOn(slot, yesterday);

            if (profit <= 0)
            {
                // plans/0072 D26. A loss is an untaxed Day and there is no carry-forward, so there
                // is nothing to record and nothing to remember -- which is what makes this a
                // `continue` rather than a column.
                continue;
            }

            taxable++;

            long bill = BusinessTax.DueOn(profit, schedule);

            if (bill <= 0)
            {
                // A profit small enough that both bands floor to nothing. It traded at a profit, so
                // it is counted above; it owes nothing, so it is not short.
                continue;
            }

            due += bill;

            if (!_world.Bins.Rows.TryResolve(_world.Businesses.Balance[slot], out int till))
            {
                // A trade that made a profit out of premises declaring no business-owned money Bin.
                // Counted rather than skipped, for PayrollReading.Tilless' reason: a silent zero in
                // every other row reads as a city that owed nothing.
                tilless++;
                continue;
            }

            int treasury = _world.FindTreasuryBin(_world.Bins.Resource[till]);

            if (treasury == Tables.Rows.NoSlot)
            {
                // A world naming money but giving the treasury no Bin to hold this one in. There is
                // nowhere for a collection to land, so nothing is taken rather than money vanishing
                // -- WageEngine.Pay's own answer to the same shape.
                continue;
            }

            // 🔴 plans/0072 D25. The till pays what it has and the rest is forgiven: no arrears
            // column, no debt, and no collection may push a till negative. Carrying the shortfall
            // instead would be a magnitude trending upward at steady state on a Business nobody
            // could ever collect from, which adr/0006 forbids outright -- and D5 already settled the
            // identical question on the Citizen side, where unpaid wages generate no tax debt.
            long held = _world.Bins.LevelAt(till);
            long taken = bill < held ? bill : held;

            if (taken < bill)
            {
                shortTill++;
            }

            if (taken <= 0)
            {
                continue;
            }

            // Through World's doors rather than BinTable.Move, so both writes drain their wait
            // lists -- PolicyEngine.Move's reason and WageEngine.Pay's. ONE debit against ONE credit
            // of the same size: the money supply is untouched, because a tax moves money and does
            // not issue it.
            _world.Withdraw(_world.Bins.Rows.At(till), taken, tick);
            _world.Deposit(_world.Bins.Rows.At(treasury), taken, tick);

            collected += taken;
            paying++;
        }

        _collectedFlow = _collectedFlow.Fold(collected);

        return new ProfitTaxReading(due, collected, taxable, paying, shortTill, tilless);
    }
}
