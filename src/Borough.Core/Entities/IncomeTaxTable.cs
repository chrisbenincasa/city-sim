// SPDX-License-Identifier: MIT
namespace Borough.Core.Entities;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// The Citizen income-tax schedules in force, one row per Day that any unpaid wage could still
/// be attributed to.
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

        EffectiveFrom[slot] = day;
        Allowance[slot] = schedule.AllowancePerDay;
        UpperThreshold[slot] = schedule.UpperThresholdPerDay;
        MiddleRate[slot] = schedule.MiddleRatePercent;
        UpperRate[slot] = schedule.UpperRatePercent;
        Stamped[slot] = 1;
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
