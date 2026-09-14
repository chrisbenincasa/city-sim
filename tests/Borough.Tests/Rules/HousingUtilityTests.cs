using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0045</c> row 31 task 3: the one kernel every housing comparison goes through.
/// </summary>
public sealed class HousingUtilityTests
{
    /// <summary><c>chosen.toml</c>'s scales: 2,048 Tiles and 120 a Day to the utility unit.</summary>
    private static PlacementRuleset Model() =>
        new(Interval: 32, RevisitTicks: 1024, Candidates: 3, GivesUpAfterDays: 120,
            ReconsiderTicks: 1024)
        {
            MuPercent = 100,
            CentralityTilesPerUnit = 2048,
            RentPerUnit = 120,
            MovingCostsRent = 720,
        };

    /// <summary>The arithmetic the engine had before a stage could weigh rent.</summary>
    private static long Legacy(long tiles, long taste, Money rent)
    {
        PlacementRuleset model = Model();

        return -IntegerMath.FloorDiv(tiles * taste, model.CentralityTilesPerUnit)
            - IntegerMath.FloorDiv(rent.Raw * Fixed.One, model.RentPerUnit);
    }

    /// <summary>A stage stating nothing scores what every world scored before this row.</summary>
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(6000, 0, 900)]
    [InlineData(6000, Fixed.One / 2, 900)]
    [InlineData(1200, -Fixed.One, 40)]
    [InlineData(0, Fixed.One, 4000)]
    public void The_neutral_weight_scores_exactly_what_the_engine_scored(
        int tiles, int taste, int rent)
    {
        Assert.Equal(
            Legacy(tiles, taste, new Money(rent)),
            HousingUtility.Worth(
                Model(), tiles, taste, new Money(rent), Ruleset.RentNeutralPercent));
    }

    /// <summary>
    /// A stage weighing rent at zero still has a centrality term, and nothing else.
    /// </summary>
    [Fact]
    public void A_stage_that_does_not_mind_rent_scores_only_distance()
    {
        PlacementRuleset model = Model();

        int free = HousingUtility.Worth(model, 6000, Fixed.One / 2, new Money(900), 0);
        int nothing = HousingUtility.Worth(model, 6000, Fixed.One / 2, Money.Zero, 0);

        Assert.Equal(nothing, free);
        Assert.Equal(-IntegerMath.FloorDiv(6000L * (Fixed.One / 2), 2048), free);
    }

    /// <summary>Weighing rent harder makes a dwelling worth less, in proportion.</summary>
    [Fact]
    public void Weighing_rent_harder_costs_a_dwelling_in_proportion()
    {
        PlacementRuleset model = Model();

        int neutral = HousingUtility.Worth(
            model, 0, 0, new Money(900), Ruleset.RentNeutralPercent);

        int minds = HousingUtility.Worth(model, 0, 0, new Money(900), 150);
        int relaxed = HousingUtility.Worth(model, 0, 0, new Money(900), 75);

        Assert.True(minds < neutral);
        Assert.True(relaxed > neutral);
        Assert.Equal(IntegerMath.FloorDiv(neutral * 150L, 100), minds);
        Assert.Equal(IntegerMath.FloorDiv(neutral * 75L, 100), relaxed);
    }

    /// <summary>
    /// The weight scales the term and never the money, so two stages at 100 agree with the world
    /// that has no stage table at all.
    /// </summary>
    [Fact]
    public void The_weight_is_applied_after_the_scale_and_not_before()
    {
        PlacementRuleset model = Model();

        for (int rent = 1; rent < 400; rent++)
        {
            long before = -IntegerMath.FloorDiv(
                IntegerMath.FloorDiv(rent * 150L, 100) * Fixed.One, model.RentPerUnit);

            int after = HousingUtility.Worth(model, 0, 0, new Money(rent), 150);

            if (after != before)
            {
                Assert.Equal(
                    -IntegerMath.FloorDiv(
                        IntegerMath.FloorDiv(rent * (long)Fixed.One, model.RentPerUnit) * 150L, 100),
                    after);

                return;
            }
        }

        Assert.Fail("the two orders never disagreed, so this test is asserting nothing");
    }

    /// <summary>A scale a Ruleset could state puts the far corner past Q16.16, and it saturates.</summary>
    [Fact]
    public void A_sum_past_what_the_representation_holds_saturates_rather_than_throwing()
    {
        PlacementRuleset tiny = Model() with { CentralityTilesPerUnit = 1, RentPerUnit = 1 };

        Assert.Equal(
            Fixed.MinValue, HousingUtility.Worth(tiny, 16384, Fixed.One, new Money(40_000), 200));

        Assert.Equal(Fixed.MaxValue, HousingUtility.Worth(tiny, 40_000, -Fixed.One, Money.Zero, 200));
    }

    /// <summary>Saturation is a bound and not a rounding: a sum inside the range is untouched.</summary>
    [Fact]
    public void A_sum_inside_the_representation_comes_back_unchanged()
    {
        Assert.Equal(0, HousingUtility.Saturate(0));
        Assert.Equal(Fixed.MaxValue, HousingUtility.Saturate(Fixed.MaxValue));
        Assert.Equal(Fixed.MinValue, HousingUtility.Saturate(Fixed.MinValue));
        Assert.Equal(-12345, HousingUtility.Saturate(-12345));
    }
}
