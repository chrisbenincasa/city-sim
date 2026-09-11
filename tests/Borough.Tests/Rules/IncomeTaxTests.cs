using Borough.Core.Arithmetic;
using Borough.Core.Rules;

namespace Borough.Tests.Rules;

/// <summary>
/// plans/0072 D2, D5 and D7. The point of these is not that percentages work — it is that a rate
/// only ever reaches the earnings inside its own band, so crossing a threshold cannot reprice what
/// came before it, and that a Day paid in instalments is taxed exactly once.
/// </summary>
public class IncomeTaxTests
{
    /// <summary>
    /// The schedule most of these read against: nothing to 50, a fifth of the next 150, two fifths
    /// of everything above 200. Every figure asserted against it is hand-computed in the comment
    /// beside it.
    /// </summary>
    private static readonly IncomeTaxSchedule Banded = new(
        AllowancePerDay: 50,
        UpperThresholdPerDay: 200,
        MiddleRatePercent: 20,
        UpperRatePercent: 40);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(49)]
    // The boundary itself is inside the allowance, not the first taxed unit.
    [InlineData(50)]
    public void Gross_at_or_below_the_allowance_owes_nothing(long gross) =>
        Assert.Equal(0, IncomeTax.DueOn(gross, Banded));

    [Theory]
    // Only the excess over 50 is taxed: 1 * 20% floors to 0, which is the Citizen's way.
    [InlineData(51, 0)]
    [InlineData(54, 0)]
    // 5 * 20% = 1.
    [InlineData(55, 1)]
    // 50 * 20% = 10.
    [InlineData(100, 10)]
    // 100 * 20% = 20.
    [InlineData(150, 20)]
    // The whole middle band, 150 * 20% = 30, with nothing yet in the upper band.
    [InlineData(200, 30)]
    public void Gross_inside_the_middle_band_is_taxed_on_the_excess_over_the_allowance(
        long gross, long expected) =>
        Assert.Equal(expected, IncomeTax.DueOn(gross, Banded));

    [Theory]
    // 150 * 20% = 30, plus 1 * 40% floored to 0.
    [InlineData(201, 30)]
    // 30, plus 60 * 40% = 24.
    [InlineData(260, 54)]
    // 30, plus 100 * 40% = 40.
    [InlineData(300, 70)]
    // 30, plus 800 * 40% = 320.
    [InlineData(1000, 350)]
    public void Gross_above_the_upper_threshold_pays_the_middle_band_in_full_plus_the_upper_rate(
        long gross, long expected) =>
        Assert.Equal(expected, IncomeTax.DueOn(gross, Banded));

    /// <summary>
    /// D2's whole reason for marginal bands. A steep upper rate is the case that would expose a
    /// schedule taxing the whole gross at the band it lands in: at 200 such a schedule takes 30 and
    /// at 201 it would take 180, and the Citizen would be poorer for earning more.
    /// </summary>
    [Fact]
    public void Take_home_never_falls_as_gross_rises()
    {
        IncomeTaxSchedule steep = new(
            AllowancePerDay: 50,
            UpperThresholdPerDay: 200,
            MiddleRatePercent: 20,
            UpperRatePercent: 90);

        long previous = long.MinValue;

        for (long gross = 0; gross <= 600; gross++)
        {
            long takeHome = gross - IncomeTax.DueOn(gross, steep);

            Assert.True(
                takeHome >= previous,
                $"take-home fell from {previous} to {takeHome} at gross {gross}");

            previous = takeHome;
        }
    }

    /// <summary>
    /// The worked example in plans/0072 D5. Two payments against one earning Day: the first sits
    /// inside the allowance and the second is the one that crosses it.
    /// </summary>
    [Fact]
    public void A_day_paid_in_two_instalments_is_granted_the_allowance_once()
    {
        IncomeTaxSchedule schedule = new(
            AllowancePerDay: 50,
            UpperThresholdPerDay: 1_000,
            MiddleRatePercent: 20,
            UpperRatePercent: 20);

        Assert.Equal(0, IncomeTax.WithholdingOn(0, 40, schedule));

        // 100 for the Day, 50 of it above the allowance, 20% of that. Assessed fresh on 60 alone it
        // would have been 2, and the treasury would have handed out the allowance twice.
        Assert.Equal(10, IncomeTax.WithholdingOn(40, 60, schedule));
    }

    /// <summary>
    /// Because <c>WithholdingOn</c> is a difference of two totals rather than a fresh assessment,
    /// only the final total is ever floored, so the split cannot cost the treasury anything at all.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(40)]
    public void Splitting_a_day_into_instalments_withholds_the_same_total(int instalments)
    {
        const long GrossForDay = 313;

        long paid = 0;
        long withheld = 0;

        for (int i = 1; i <= instalments; i++)
        {
            // Walk the running total to the i-th of `instalments` equal shares, so the last
            // instalment carries whatever the shares did not divide evenly.
            long target = IntegerMath.FloorDiv(GrossForDay * i, instalments);
            withheld += IncomeTax.WithholdingOn(paid, target - paid, Banded);
            paid = target;
        }

        Assert.Equal(GrossForDay, paid);
        Assert.Equal(IncomeTax.DueOn(GrossForDay, Banded), withheld);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void A_payment_of_nothing_withholds_nothing(long addition) =>
        Assert.Equal(0, IncomeTax.WithholdingOn(100, addition, Banded));

    /// <summary>
    /// An employer cannot hand the treasury more than it is paying out, whatever the schedule and
    /// wherever in the Day the payment lands.
    /// </summary>
    [Fact]
    public void Withholding_is_never_negative_and_never_exceeds_the_payment()
    {
        IncomeTaxSchedule steep = new(
            AllowancePerDay: 50,
            UpperThresholdPerDay: 200,
            MiddleRatePercent: 20,
            UpperRatePercent: 100);

        for (long paid = 0; paid <= 400; paid += 7)
        {
            for (long addition = 1; addition <= 400; addition += 11)
            {
                long withheld = IncomeTax.WithholdingOn(paid, addition, steep);

                Assert.InRange(withheld, 0, addition);
            }
        }
    }

    /// <summary>
    /// D7 admits a schedule whose two rates are equal, and it must then be indistinguishable from a
    /// single bracket over the allowance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The middle band is 200 wide at 20% deliberately</b>, so the band closes on an exact 40
    /// and no unit is lost to the split. Bands whose width times rate is not a multiple of 100 floor
    /// twice rather than once and can come out a unit under the single bracket — that is the
    /// Citizen's way and not a defect, but it is not the property this test is about.
    /// </para>
    /// </remarks>
    [Fact]
    public void Equal_rates_behave_as_one_bracket_above_the_allowance()
    {
        IncomeTaxSchedule twoBands = new(
            AllowancePerDay: 50,
            UpperThresholdPerDay: 250,
            MiddleRatePercent: 20,
            UpperRatePercent: 20);

        IncomeTaxSchedule oneBand = new(
            AllowancePerDay: 50,
            UpperThresholdPerDay: 1_000_000,
            MiddleRatePercent: 20,
            UpperRatePercent: 20);

        for (long gross = 0; gross <= 800; gross++)
        {
            Assert.Equal(
                IncomeTax.DueOn(gross, oneBand),
                IncomeTax.DueOn(gross, twoBands));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1_000)]
    [InlineData(1_000_000)]
    public void A_world_with_no_income_tax_withholds_nothing(long gross)
    {
        Assert.Equal(0, IncomeTax.DueOn(gross, IncomeTaxSchedule.None));
        Assert.Equal(0, IncomeTax.WithholdingOn(0, gross, IncomeTaxSchedule.None));
        Assert.False(IncomeTaxSchedule.None.Levies);
    }
}
