using Borough.Core.Entities;
using Borough.Core.Rules;

namespace Borough.Tests.Entities;

/// <summary>
/// The ring holds two tax schedules per Day, and this is the file that keeps them from
/// overwriting each other.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The defect these exist for is silent and its symptom is a tax switching itself off.</b>
/// A row is one Day's tax position and carries both schedules, so a single shared stamp would
/// mean that a player adjusting an earnings rate in a world whose Ruleset authored
/// <c>[business_tax]</c> set every profit band to zero on the way past — no refusal, no
/// diagnostic, just a column of nothing from the next Day on. The two halves stamp independently
/// for that reason and these tests hold them apart.
/// </para>
/// </remarks>
public sealed class TaxScheduleRingTests
{
    private static readonly IncomeTaxSchedule Earnings = new(50, 200, 10, 20);

    private static readonly BusinessTaxSchedule Profit = new(400, 15, 30);

    [Fact]
    public void An_ungoverned_ring_falls_through_to_what_the_ruleset_authored()
    {
        var ring = new IncomeTaxTable();

        Assert.Equal(Earnings, ring.ScheduleFor(9, Earnings));
        Assert.Equal(Profit, ring.ProfitScheduleFor(9, Profit));
    }

    [Fact]
    public void Governing_the_earnings_schedule_leaves_the_authored_profit_bands_reachable()
    {
        var ring = new IncomeTaxTable();

        ring.Govern(9, new IncomeTaxSchedule(60, 300, 25, 25));

        Assert.Equal(25, ring.ScheduleFor(9, Earnings).MiddleRatePercent);
        Assert.Equal(Profit, ring.ProfitScheduleFor(9, Profit));
    }

    [Fact]
    public void Governing_the_profit_schedule_leaves_the_authored_earnings_bands_reachable()
    {
        var ring = new IncomeTaxTable();

        ring.GovernProfit(9, new BusinessTaxSchedule(900, 5, 40));

        Assert.Equal(900, ring.ProfitScheduleFor(9, Profit).ThresholdPerDay);
        Assert.Equal(Earnings, ring.ScheduleFor(9, Earnings));
    }

    [Fact]
    public void Both_governed_on_one_day_share_one_entry_and_neither_loses_the_other()
    {
        var ring = new IncomeTaxTable();

        ring.Govern(9, new IncomeTaxSchedule(60, 300, 25, 25));
        ring.GovernProfit(9, new BusinessTaxSchedule(900, 5, 40));

        Assert.Equal(25, ring.ScheduleFor(9, Earnings).MiddleRatePercent);
        Assert.Equal(900, ring.ProfitScheduleFor(9, Profit).ThresholdPerDay);
    }

    /// <summary>
    /// A recycled slot carries nothing of the Day it used to hold.
    /// </summary>
    /// <remarks>
    /// The ring is indexed by the Day itself, so Day <c>d</c> and Day <c>d + Retained</c> are the
    /// same slot. ⚠ <b>Without clearing the other half's stamp, a profit schedule set a full ring
    /// ago would read as this Day's</b> — the arithmetic that finds the row would agree, because
    /// the row's effective Day has been rewritten underneath it.
    /// </remarks>
    [Fact]
    public void A_slot_recycled_a_full_ring_later_carries_nothing_of_the_day_it_held()
    {
        var ring = new IncomeTaxTable();

        ring.GovernProfit(9, new BusinessTaxSchedule(900, 5, 40));
        ring.Govern(9 + IncomeTaxTable.Retained, new IncomeTaxSchedule(60, 300, 25, 25));

        Assert.Equal(
            Profit, ring.ProfitScheduleFor(9 + IncomeTaxTable.Retained, Profit));
    }

    [Fact]
    public void A_schedule_holds_from_the_day_it_was_set_until_the_next_change()
    {
        var ring = new IncomeTaxTable();

        ring.GovernProfit(4, new BusinessTaxSchedule(900, 5, 40));

        Assert.Equal(Profit, ring.ProfitScheduleFor(3, Profit));
        Assert.Equal(900, ring.ProfitScheduleFor(4, Profit).ThresholdPerDay);
        Assert.Equal(900, ring.ProfitScheduleFor(7, Profit).ThresholdPerDay);
    }

    [Fact]
    public void A_ring_holding_only_zero_rates_levies_nothing_on_either_schedule()
    {
        var ring = new IncomeTaxTable();

        ring.Govern(2, new IncomeTaxSchedule(50, 200, 0, 0));
        ring.GovernProfit(2, new BusinessTaxSchedule(400, 0, 0));

        Assert.False(ring.Levies(IncomeTaxSchedule.None));
        Assert.False(ring.Taxes(BusinessTaxSchedule.None));
    }
}
