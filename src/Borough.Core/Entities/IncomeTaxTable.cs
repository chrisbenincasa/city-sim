// SPDX-License-Identifier: MIT
namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// The tax schedules in force — Citizen income and Business profit both — one row per Day that any
/// unpaid wage could still be attributed to.
/// </summary>
/// <remarks>
/// <para>
/// <b>A rate change applies prospectively and a late payment is still taxed at the Day it was
/// earned</b> — <c>plans/0072</c> D6. That needs the schedule history to outlive the change, and
/// the history is bounded rather than unbounded because <see cref="Rules.WageEngine"/> already
/// forfeits any entitlement older than one pay period. Nothing can ever be paid for a Day further
/// back than that, so <see cref="Retained"/> Days of history is exact and not a truncation.
/// </para>
/// <para>
/// ⚠ <b><see cref="Retained"/> is a <c>const</c> on purpose</b>, on
/// <see cref="RulesetTrailTable.Retained"/>'s grounds: the row count is fixed for the life of the
/// world, so a hot reload cannot be allowed to demand a deeper history than the table was built
/// with. The loader refuses a Ruleset that states <c>[income_tax]</c> and declares a
/// <c>[[business]]</c> whose <c>pay_period_days</c> exceeds <see cref="Retained"/> —
/// <c>RulesetLoader.RefuseUnassessablePayPeriods</c>, and <c>adr/0048</c>'s enumeration carries the
/// reason. ⚠ <b>It is gated on the table being stated and not on its rates levying anything</b>,
/// because <see cref="Govern"/> writes into this same fixed ring.
/// </para>
/// <para>
/// <b>An empty table is not an untaxed world.</b> Every row unstamped means the player has never
/// moved a rate, so every Day falls through to what the Ruleset authored.
/// </para>
/// </remarks>
[Table]
public sealed class IncomeTaxTable
{
    /// <summary>How many Days of schedule history the world keeps.</summary>
    public const int Retained = 32;

    private readonly Rows<IncomeTaxRate> _rows;

    /// <summary>Builds the table and allocates its fixed ring.</summary>
    public IncomeTaxTable()
    {
        _rows = new Rows<IncomeTaxRate>("income_tax", Retained, Buffering.OneCopy);

        EffectiveFrom = _rows.Saved<long>("effective_from", Touch.Cold);
        Allowance = _rows.Saved<long>("allowance", Touch.Cold);
        UpperThreshold = _rows.Saved<long>("upper_threshold", Touch.Cold);
        MiddleRate = _rows.Saved<int>("middle_rate", Touch.Cold);
        UpperRate = _rows.Saved<int>("upper_rate", Touch.Cold);
        Stamped = _rows.Saved<byte>("stamped", Touch.Cold);
        ProfitStamped = _rows.Saved<byte>("profit_stamped", Touch.Cold);
        ProfitThreshold = _rows.Saved<long>("profit_threshold", Touch.Cold);
        ProfitLowerRate = _rows.Saved<int>("profit_lower_rate", Touch.Cold);
        ProfitUpperRate = _rows.Saved<int>("profit_upper_rate", Touch.Cold);

        _rows.Seal();

        for (int slot = 0; slot < Retained; slot++)
        {
            _rows.Allocate();
        }
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<IncomeTaxRate> Rows => _rows;

    /// <summary>The Day this row's schedule first applies to. Meaningless unless stamped.</summary>
    public Column<long> EffectiveFrom { get; }

    /// <summary>What a Citizen may earn in one Day before any tax.</summary>
    public Column<long> Allowance { get; }

    /// <summary>The Day's earnings at which the upper marginal band starts.</summary>
    public Column<long> UpperThreshold { get; }

    /// <summary>The marginal rate between the allowance and the threshold, as a percentage.</summary>
    public Column<int> MiddleRate { get; }

    /// <summary>The marginal rate above the threshold, as a percentage.</summary>
    public Column<int> UpperRate { get; }

    /// <summary>Whether this row holds a schedule at all.</summary>
    public Column<byte> Stamped { get; }

    /// <summary>Whether this row holds a Business profit schedule.</summary>
    /// <remarks>
    /// 🔴 <b>Separate from <see cref="Stamped"/>, and the separation is load-bearing.</b> A player
    /// who moves an earnings rate in a world whose Ruleset authored <c>[business_tax]</c> must not
    /// thereby set the profit bands to zero — which is what one shared stamp would do, silently
    /// switching the profit tax off on the Day somebody adjusted an unrelated number.
    /// </remarks>
    public Column<byte> ProfitStamped { get; }

    /// <summary>The Day's Business profit at which the upper marginal band starts.</summary>
    public Column<long> ProfitThreshold { get; }

    /// <summary>The marginal rate on Business profit below the threshold, as a percentage.</summary>
    public Column<int> ProfitLowerRate { get; }

    /// <summary>The marginal rate on Business profit above the threshold, as a percentage.</summary>
    public Column<int> ProfitUpperRate { get; }

    /// <summary>
    /// Whether any Business profit schedule this world could consult takes anything at all.
    /// </summary>
    public bool Taxes(in BusinessTaxSchedule authored)
    {
        if (authored.Levies)
        {
            return true;
        }

        for (int slot = 0; slot < Retained; slot++)
        {
            if (ProfitStamped[slot] != 0
                && (ProfitLowerRate[slot] > 0 || ProfitUpperRate[slot] > 0))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The Business profit schedule that governs a Day, falling through to what the Ruleset
    /// authored when the player has set nothing that reaches it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It shares this table's ring with the income tax and needs almost none of its depth.</b>
    /// Profit is assessed the Day after it is made (<c>plans/0072</c> D23) and nothing is ever
    /// assessed late, so two Days would do. It sits here anyway because ***the two schedules are
    /// governed by one verb and change on one Day boundary***, and splitting them would be two
    /// tables holding one effective Day between them.
    /// </remarks>
    public BusinessTaxSchedule ProfitScheduleFor(long day, in BusinessTaxSchedule authored)
    {
        long oldest = day - Retained + 1;

        for (long probe = day; probe >= oldest && probe >= 0; probe--)
        {
            int slot = (int)(probe % Retained);

            if (ProfitStamped[slot] != 0 && EffectiveFrom[slot] == probe)
            {
                return new BusinessTaxSchedule(
                    ProfitThreshold[slot], ProfitLowerRate[slot], ProfitUpperRate[slot]);
            }
        }

        return authored;
    }

    /// <summary>
    /// Whether any schedule this world could consult takes anything at all.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Asked so that a world with no income tax leaves the per-Citizen accumulator
    /// untouched</b>, rather than writing a Day and a zero into every worker on every payday. The
    /// scan is the fixed ring depth and it is asked once per employer per payday, never per worker.
    /// </remarks>
    public bool Levies(in IncomeTaxSchedule authored)
    {
        if (authored.Levies)
        {
            return true;
        }

        for (int slot = 0; slot < Retained; slot++)
        {
            if (Stamped[slot] != 0 && (MiddleRate[slot] > 0 || UpperRate[slot] > 0))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Records a schedule taking effect from the start of <paramref name="day"/>.</summary>
    /// <remarks>
    /// ⚠ <b>Governing twice for the same Day replaces rather than appends.</b> The ring is indexed
    /// by the Day itself, so a player who moves a rate repeatedly within one Day leaves one entry,
    /// and the history stays exactly as deep as the wage window it has to cover.
    /// </remarks>
    public void Govern(long day, in IncomeTaxSchedule schedule)
    {
        int slot = (int)(day % Retained);

        // ⚠ A row recycled onto a different Day carries no profit schedule into it. The two
        // halves stamp independently, so the other half's stamp has to be cleared or an entry from
        // 32 Days ago is read as this Day's.
        if (EffectiveFrom[slot] != day)
        {
            ProfitStamped[slot] = 0;
        }

        EffectiveFrom[slot] = day;
        Allowance[slot] = schedule.AllowancePerDay;
        UpperThreshold[slot] = schedule.UpperThresholdPerDay;
        MiddleRate[slot] = schedule.MiddleRatePercent;
        UpperRate[slot] = schedule.UpperRatePercent;
        Stamped[slot] = 1;
    }

    /// <summary>Records a Business profit schedule taking effect from the start of a Day.</summary>
    /// <remarks>
    /// <b><see cref="Govern"/>'s counterpart.</b> The two halves of a row stamp independently, so
    /// setting a profit band leaves an earnings schedule already stamped for this Day alone and
    /// clears one inherited from a Day the row has been recycled away from.
    /// </remarks>
    public void GovernProfit(long day, in BusinessTaxSchedule schedule)
    {
        int slot = (int)(day % Retained);

        if (EffectiveFrom[slot] != day)
        {
            Stamped[slot] = 0;
        }

        EffectiveFrom[slot] = day;
        ProfitStamped[slot] = 1;

        ProfitThreshold[slot] = schedule.ThresholdPerDay;
        ProfitLowerRate[slot] = schedule.LowerRatePercent;
        ProfitUpperRate[slot] = schedule.UpperRatePercent;
    }

    /// <summary>
    /// The schedule that governs earnings on <paramref name="day"/>, falling through to what the
    /// Ruleset authored when the player has set nothing that reaches it.
    /// </summary>
    /// <remarks>
    /// Walks backwards to the most recent change at or before the Day, which is what makes a
    /// schedule persist across the Days between changes without a row for each of them.
    /// </remarks>
    public IncomeTaxSchedule ScheduleFor(long day, in IncomeTaxSchedule authored)
    {
        long oldest = day - Retained + 1;

        for (long probe = day; probe >= oldest && probe >= 0; probe--)
        {
            int slot = (int)(probe % Retained);

            if (Stamped[slot] != 0 && EffectiveFrom[slot] == probe)
            {
                return new IncomeTaxSchedule(
                    Allowance[slot], UpperThreshold[slot], MiddleRate[slot], UpperRate[slot]);
            }
        }

        return authored;
    }
}
