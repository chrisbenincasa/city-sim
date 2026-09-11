using Borough.Core.Rules;

namespace Borough.Tests.Rules;

/// <summary>
/// plans/0072 D8, D23 and D26. The point of these is not that percentages work — it is that a rate
/// only ever reaches the profit inside its own band, that a loss is an untaxed Day rather than a
/// negative bill, and that a trade is never worse off for having earned more.
/// </summary>
/// <remarks>
/// 🔴 <b>There is no instalment test here and there is one on the Citizen side, which is the
/// asymmetry worth naming.</b> A wage arrives in instalments, so <c>IncomeTax.WithholdingOn</c> has
/// to remember what a Day has already been taxed on. Profit is assessed <em>once</em> per Day
/// against a figure the Business accumulates, so there is nothing for a running total to protect —
/// <c>BusinessTax</c> has no <c>WithholdingOn</c> to test.
/// </remarks>
public class BusinessTaxTests
{
    /// <summary>
    /// The schedule most of these read against: a tenth of the first 200, a third of everything
    /// above it. Every figure asserted against it is hand-computed in the comment beside it.
    /// </summary>
    private static readonly BusinessTaxSchedule Banded = new(
        ThresholdPerDay: 200,
        LowerRatePercent: 10,
        UpperRatePercent: 30);

    // ---- a loss is an untaxed Day ----------------------------------------------------------------

    /// <summary>
    /// Profit at or below zero owes nothing, and the bill is never negative.
    /// </summary>
    /// <remarks>
    /// <b>D26, and the assertion is <c>0</c> rather than <c>&lt;= 0</c> on purpose.</b> A schedule
    /// that ran its arithmetic over a negative profit would produce a negative due, which is the
    /// treasury paying the trade — the carry-forward this design refuses, arriving by accident.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1_000_000)]
    public void A_loss_or_a_flat_day_owes_nothing(long profit) =>
        Assert.Equal(0, BusinessTax.DueOn(profit, Banded));

    // ---- the lower band --------------------------------------------------------------------------

    [Theory]
    // 9 * 10% floors to 0, which is the trade's way.
    [InlineData(1, 0)]
    [InlineData(9, 0)]
    // 10 * 10% = 1.
    [InlineData(10, 1)]
    // 100 * 10% = 10.
    [InlineData(100, 10)]
    // The boundary itself is entirely inside the lower band: 200 * 10% = 20.
    [InlineData(200, 20)]
    public void Profit_below_the_threshold_pays_the_lower_rate(long profit, long due) =>
        Assert.Equal(due, BusinessTax.DueOn(profit, Banded));

    // ---- the upper band --------------------------------------------------------------------------

    [Theory]
    // 20 on the first 200, then 1 * 30% floors to 0.
    [InlineData(201, 20)]
    // 20, then 100 * 30% = 30.
    [InlineData(300, 50)]
    // 20, then 800 * 30% = 240.
    [InlineData(1000, 260)]
    public void Only_the_excess_over_the_threshold_pays_the_upper_rate(long profit, long due) =>
        Assert.Equal(due, BusinessTax.DueOn(profit, Banded));

    /// <summary>
    /// Crossing the threshold does not reprice the profit below it.
    /// </summary>
    /// <remarks>
    /// <b>The whole content of the word <em>marginal</em>.</b> A schedule that applied the upper
    /// rate to the entire figure once the threshold was passed would charge 300 on a profit of 1,000
    /// rather than 260, and the difference is exactly the lower band re-rated.
    /// </remarks>
    [Fact]
    public void The_upper_rate_never_reaches_the_lower_band()
    {
        long flat = (1000 * Banded.UpperRatePercent) / 100;

        Assert.Equal(300, flat);
        Assert.Equal(260, BusinessTax.DueOn(1000, Banded));
    }

    // ---- the degenerate schedules ----------------------------------------------------------------

    /// <summary>Two equal rates are a flat tax, and the threshold stops mattering.</summary>
    [Theory]
    [InlineData(100, 25)]
    [InlineData(200, 50)]
    [InlineData(1000, 250)]
    public void Equal_rates_are_a_flat_tax_the_threshold_cannot_bend(long profit, long due) =>
        Assert.Equal(due, BusinessTax.DueOn(profit, new BusinessTaxSchedule(200, 25, 25)));

    /// <summary>
    /// A lower rate of zero is how this schedule spells a tax-free band.
    /// </summary>
    /// <remarks>
    /// <b>D8's own sentence, and the reason there is no allowance key</b> — *"There is no separate
    /// tax-free band, although setting the lower rate to zero can provide one."* The threshold then
    /// behaves exactly as <c>IncomeTaxSchedule.AllowancePerDay</c> does on the Citizen side.
    /// </remarks>
    [Theory]
    [InlineData(200, 0)]
    // 300 * 40% over a threshold of 200: 100 * 40% = 40.
    [InlineData(300, 40)]
    public void A_lower_rate_of_zero_is_a_tax_free_band(long profit, long due) =>
        Assert.Equal(due, BusinessTax.DueOn(profit, new BusinessTaxSchedule(200, 0, 40)));

    /// <summary>A threshold of zero puts every unit of profit in the upper band.</summary>
    [Fact]
    public void A_threshold_of_zero_taxes_everything_at_the_upper_rate() =>
        Assert.Equal(30, BusinessTax.DueOn(100, new BusinessTaxSchedule(0, 10, 30)));

    /// <summary>A schedule that takes nothing says so, and takes nothing.</summary>
    [Fact]
    public void The_absent_schedule_takes_nothing()
    {
        Assert.False(BusinessTaxSchedule.None.Levies);
        Assert.Equal(0, BusinessTax.DueOn(1_000_000, BusinessTaxSchedule.None));
    }

    /// <summary>Either rate above zero makes the schedule one that levies.</summary>
    [Theory]
    [InlineData(10, 0, true)]
    [InlineData(0, 30, true)]
    [InlineData(0, 0, false)]
    public void Levies_reports_whether_anything_can_ever_be_taken(int lower, int upper, bool levies) =>
        Assert.Equal(levies, new BusinessTaxSchedule(200, lower, upper).Levies);

    // ---- the property the loader's refusal exists to protect -------------------------------------

    /// <summary>
    /// Post-tax profit never falls as profit rises, across the threshold and through the flooring.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the property the loader's <c>upper_rate_percent</c> refusal exists to keep
    /// true</b>, and it is why that refusal is a design decision rather than a typo guard: the only
    /// way to break monotonicity with rates in <c>0..100</c> is an upper rate below the lower one.
    /// The sweep runs over the threshold in both directions so a schedule that repriced the lower
    /// band on crossing would show up as a step down rather than as a wrong total.
    /// </remarks>
    [Fact]
    public void Post_tax_profit_never_falls_as_profit_rises()
    {
        long previous = long.MinValue;

        for (long profit = -20; profit <= 600; profit++)
        {
            long kept = profit - BusinessTax.DueOn(profit, Banded);

            Assert.True(
                kept >= previous,
                $"post-tax profit fell at {profit}: {kept} after {previous}. A marginal schedule "
                    + "must never make a trade worse off for earning more.");

            previous = kept;
        }
    }

    /// <summary>
    /// The tax never exceeds the profit it is levied on, so a Day's takings cannot go negative.
    /// </summary>
    /// <remarks>
    /// <b>It follows from both rates being at most 100 and holds for every schedule the loader
    /// admits</b>, which is what lets <c>D25</c>'s short-till rule be about the till rather than
    /// about the schedule.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(199)]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(100_000)]
    public void The_bill_never_exceeds_the_profit(long profit)
    {
        Assert.True(BusinessTax.DueOn(profit, Banded) <= profit);
        Assert.True(BusinessTax.DueOn(profit, new BusinessTaxSchedule(200, 100, 100)) <= profit);
    }
}
