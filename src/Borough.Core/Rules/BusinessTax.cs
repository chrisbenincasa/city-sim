// SPDX-License-Identifier: MIT
namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;

/// <summary>
/// The two marginal bands a Business's profit for one Day is taxed against: a lower band running to
/// <see cref="ThresholdPerDay"/>, and an upper band above it.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>There is no tax-free band and that absence is the decision</b>, per <c>plans/0072</c> D8 —
/// which is the one place this schedule departs from <see cref="IncomeTaxSchedule"/>'s shape rather
/// than mirroring it. An author who wants relief on modest profits writes a
/// <see cref="LowerRatePercent"/> of zero, and the threshold becomes the allowance. ⚠ <b>An
/// allowance key would be a second spelling of a city that is already writable</b>, which is the
/// argument <c>[lots] storeys_per_rung</c> made for a floor of 1.
/// </para>
/// <para>
/// ⚠ <b>The threshold is profit for one Day and never a Business's balance</b> (<c>plans/0072</c>
/// D23). A Business that has a bad Day owes nothing for it, and a trade with lumpy sales pays more
/// over a span than a steady one with the same total — accepted rather than smoothed.
/// </para>
/// </remarks>
public readonly record struct BusinessTaxSchedule(
    long ThresholdPerDay,
    int LowerRatePercent,
    int UpperRatePercent)
{
    /// <summary>A world whose Ruleset states no <c>[business_tax]</c>. No profit is taxed.</summary>
    public static BusinessTaxSchedule None => new(0, 0, 0);

    /// <summary>Whether this schedule can ever take anything.</summary>
    public bool Levies => LowerRatePercent > 0 || UpperRatePercent > 0;
}

/// <summary>
/// What a Business owes on one Day's profit.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>There is deliberately no counterpart to <see cref="IncomeTax.WithholdingOn"/>, and the
/// asymmetry is a property of the two mechanisms rather than an omission.</b> A wage reaches a
/// Citizen in instalments, so the Citizen side has to remember what a Day has already been taxed on
/// or the tax-free allowance is granted again at every payday (<c>plans/0072</c> D5). Profit is not
/// paid to anybody — it is a figure the Business accumulates over the Day and is assessed against
/// <b>once</b>, at the Day's end (D23). ***There are no instalments, so there is nothing for a
/// running total to protect***, and a per-Business accumulator of what has already been assessed
/// would be a saved column recording a thing that never happens twice.
/// </para>
/// </remarks>
public static class BusinessTax
{
    /// <summary>
    /// The tax due on <paramref name="profitForDay"/> earned in a single Day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Profit may be negative or zero and a loss owes nothing</b> (<c>plans/0072</c> D26).
    /// There is no carry-forward, so a Business that loses money one Day and profits the next pays
    /// in full on the profitable Day — the consequence of the daily cadence rather than a separate
    /// decision, and the reason this returns early rather than computing a negative due.
    /// </para>
    /// <para>
    /// Each rate applies only to profit inside its own band, so crossing the threshold never
    /// reprices what came below it and post-tax profit never falls as profit rises — which holds for
    /// any schedule the loader admits, because the upper rate is at or above the lower one (D8).
    /// Every division floors, which rounds in the Business's favour.
    /// </para>
    /// </remarks>
    public static long DueOn(long profitForDay, in BusinessTaxSchedule schedule)
    {
        if (profitForDay <= 0)
        {
            return 0;
        }

        long threshold = schedule.ThresholdPerDay < 0 ? 0 : schedule.ThresholdPerDay;
        long lower = profitForDay < threshold ? profitForDay : threshold;
        long due = IntegerMath.FloorDiv(lower * schedule.LowerRatePercent, 100);

        if (profitForDay > threshold)
        {
            due += IntegerMath.FloorDiv(
                (profitForDay - threshold) * schedule.UpperRatePercent, 100);
        }

        return due;
    }
}
