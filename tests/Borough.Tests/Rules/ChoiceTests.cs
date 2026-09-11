using Borough.Core.Arithmetic;
using Borough.Core.Rules;

namespace Borough.Tests.Rules;

/// <summary>
/// 02 section 5.4's softmax, asserted as a distribution rather than as arithmetic.
/// </summary>
/// <remarks>
/// <b>The arithmetic is already covered and this is the half that was not.</b>
/// <c>TranscendentalTests</c> compares the tabulated softmax against a double-precision oracle and
/// owns the error budget; what nothing asserted is that <see cref="Choice.Draw"/> turns those
/// weights into selections at the right rate, that a Household given equal candidates spreads across
/// them, and that adr/0038's horizon is a wall rather than a taper.
/// </remarks>
public sealed class ChoiceTests
{
    private const int Mu = Fixed.One;

    private static int Units(int whole) => whole * Fixed.One;

    private static int[] Tally(ReadOnlySpan<int> utilities, int mu, int draws)
    {
        int[] counts = new int[utilities.Length];

        for (int draw = 0; draw < draws; draw++)
        {
            counts[Choice.Draw(utilities, mu, (ulong)draw * 2_654_435_761UL)]++;
        }

        return counts;
    }

    [Fact]
    public void One_candidate_is_taken_without_a_draw()
    {
        Assert.Equal(0, Choice.Draw([Units(-9)], Mu, 0));
    }

    /// <summary>
    /// Identical options split evenly, which is the property an argmax cannot have.
    /// </summary>
    /// <remarks>
    /// <b>The old accept resolved a tie by draw order</b> — <c>score &lt; bestScore</c> keeps the
    /// first — so every Household shown four equal dwellings took the one it happened to draw first.
    /// That is a hash-bearing tie-break nobody argued, and it is what the distribution replaces.
    /// </remarks>
    [Fact]
    public void Equal_candidates_are_taken_at_equal_rates()
    {
        int[] counts = Tally([Units(2), Units(2), Units(2), Units(2)], Mu, 4_000);

        foreach (int count in counts)
        {
            Assert.InRange(count, 900, 1_100);
        }
    }

    /// <summary>
    /// The rates follow <c>exp(μ·ΔV)</c>, which is the whole content of the model.
    /// </summary>
    /// <remarks>
    /// One utility unit at μ = 1 is a factor of <c>e</c>, so 0 and 1 split about 27:73. Anything
    /// that merely favours the better option would pass a weaker assertion than this one.
    /// </remarks>
    [Fact]
    public void A_one_unit_advantage_is_a_factor_of_e()
    {
        int[] counts = Tally([0, Units(1)], Mu, 10_000);

        Assert.InRange(counts[0], 2_500, 2_950);
        Assert.InRange(counts[1], 7_050, 7_500);
    }

    /// <summary>
    /// Raising μ concentrates the choice, which is the knob 02 section 5.4 says to reach for.
    /// </summary>
    [Fact]
    public void A_larger_mu_makes_the_city_more_decisive()
    {
        ReadOnlySpan<int> utilities = [0, Units(1)];

        int loose = Tally(utilities, Fixed.One / 4, 10_000)[1];
        int tight = Tally(utilities, Fixed.One * 4, 10_000)[1];

        Assert.True(
            tight > loose,
            $"mu = 4 put {tight} of 10,000 on the better option and mu = 0.25 put {loose}.");

        Assert.InRange(tight, 9_800, 10_000);
    }

    /// <summary>
    /// adr/0038's horizon: past it a candidate is impossible rather than unlikely.
    /// </summary>
    /// <remarks>
    /// <b>This is the consequence adr/0038 asked to be argued with rather than the resolution.</b>
    /// <c>exp</c> underflows below −11.09, so an option twelve units down has weight exactly zero and
    /// cannot be drawn — and doubling μ halves the distance at which that happens, which is why the
    /// second half of this test exists.
    /// </remarks>
    [Fact]
    public void A_candidate_past_the_horizon_is_never_taken()
    {
        Assert.Equal(0, Tally([Units(-12), 0], Mu, 5_000)[0]);
        Assert.Equal(0, Tally([Units(-6), 0], Fixed.One * 2, 5_000)[0]);
        Assert.True(Tally([Units(-6), 0], Mu, 5_000)[0] > 0);
    }

    /// <summary>
    /// A utility clamped to the representation's floor still loses to everything above it.
    /// </summary>
    /// <remarks>
    /// <c>PlacementEngine.Utility</c> clamps rather than throwing, and this is what that clamp is
    /// allowed to cost: nothing, because the clamped value is already past the horizon.
    /// </remarks>
    [Fact]
    public void The_representations_floor_is_past_the_horizon()
    {
        Assert.Equal(1, Choice.Draw([Fixed.MinValue, 0], Mu, 0));
    }

    /// <summary>
    /// A candidate at the bottom of the clamp stays at the bottom instead of wrapping to the top.
    /// </summary>
    /// <remarks>
    /// <b>Both operands span the whole of <c>int</c> because the scorer clamps rather than
    /// checks.</b> <c>PlacementEngine.Utility</c> saturates a sum it cannot hold, so a Ruleset with
    /// a small domain-unit scale produces exactly this pair — and a narrow subtraction turns the
    /// worst candidate in the list into the heaviest one, silently, with the draw landing on the
    /// home nobody would take.
    /// </remarks>
    [Fact]
    public void The_widest_possible_gap_stays_negative()
    {
        long scaled = Transcendental.ScaleForExp(Fixed.One, int.MinValue, int.MaxValue);

        Assert.True(scaled < 0, $"a gap of the whole int range scaled to {scaled}.");
        Assert.True(Transcendental.UnderflowsFor(Fixed.One, int.MinValue, int.MaxValue));
    }

    /// <summary>The horizon is exactly where adr/0038 says it is, from both sides.</summary>
    [Theory]
    [InlineData(Transcendental.ExpUnderflowsBelow, true)]
    [InlineData(Transcendental.ExpUnderflowsBelow + 1, false)]
    public void The_horizon_is_the_stated_value(int gap, bool underflows)
    {
        Assert.Equal(underflows, Transcendental.UnderflowsFor(Fixed.One, gap, 0));
        Assert.Equal(underflows, Transcendental.Exp(gap) == 0);
    }

    /// <summary>
    /// A scale parameter above one moves the horizon in proportion, which is what makes the
    /// stickiness ceiling depend on two keys at once.
    /// </summary>
    [Fact]
    public void A_sharper_scale_brings_the_horizon_nearer()
    {
        int gap = Transcendental.ExpUnderflowsBelow + 1;

        Assert.False(Transcendental.UnderflowsFor(Fixed.One, gap, 0));
        Assert.True(Transcendental.UnderflowsFor(2 * Fixed.One, gap, 0));
    }
}
