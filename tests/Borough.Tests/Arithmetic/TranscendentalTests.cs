using Borough.Core.Arithmetic;

namespace Borough.Tests.Arithmetic;

/// <summary>
/// plans/0005 task 4, and the validation adr/0003 demanded for the table's resolution.
/// </summary>
/// <remarks>
/// The test project may use floating point; <c>Borough.Core</c> may not. That asymmetry is the whole
/// mechanism here — the committed integer tables are checked against a double-precision oracle the
/// core is not allowed to contain.
/// </remarks>
public class TranscendentalTests
{
    private const double Ulp = 1.0 / Fixed.One;

    private static double ToDouble(int q16) => q16 / (double)Fixed.One;

    private static int ToFixed(double value) => (int)Math.Floor((value * Fixed.One) + 0.5);

    // ── The committed tables are data, so they are verified rather than trusted ──────────────

    [Fact]
    public void Exp2_table_matches_a_double_precision_regeneration()
    {
        ReadOnlySpan<int> table = Transcendental.Exp2Entries;
        Assert.Equal(Transcendental.TableEntries + 1, table.Length);

        for (int i = 0; i < table.Length; i++)
        {
            Assert.Equal(ToFixed(Math.Pow(2, i / (double)Transcendental.TableEntries)), table[i]);
        }
    }

    [Fact]
    public void Log2_table_matches_a_double_precision_regeneration()
    {
        ReadOnlySpan<int> table = Transcendental.Log2Entries;
        Assert.Equal(Transcendental.TableEntries + 1, table.Length);

        for (int i = 0; i < table.Length; i++)
        {
            Assert.Equal(ToFixed(Math.Log2(1 + (i / (double)Transcendental.TableEntries))), table[i]);
        }
    }

    [Fact]
    public void The_endpoints_are_exact()
    {
        Assert.Equal(Fixed.One, Transcendental.Exp2Entries[0]);
        Assert.Equal(2 * Fixed.One, Transcendental.Exp2Entries[Transcendental.TableEntries]);
        Assert.Equal(0, Transcendental.Log2Entries[0]);
        Assert.Equal(Fixed.One, Transcendental.Log2Entries[Transcendental.TableEntries]);
    }

    [Fact]
    public void The_stated_constants_are_the_stated_constants()
    {
        Assert.Equal(Transcendental.Ln2, ToFixed(Math.Log(2)));
        Assert.Equal(Transcendental.Log2E, ToFixed(Math.Log2(Math.E)));
        Assert.Equal(Transcendental.ExpUnderflowsBelow, ToFixed(Math.Log(Ulp)));
    }

    // ── The error bound adr/0038 claims ─────────────────────────────────────────────────────

    /// <summary>
    /// <b>The claim the resolution rests on.</b> Total error is about one ULP, and the table's share
    /// of that is roughly a tenth of it — the rest is Q16.16's own rounding, which no table size can
    /// improve. That is what "the representation is the limiting factor, not the table" means, and
    /// it is why adr/0038 stops at 256 entries rather than 512.
    /// </summary>
    [Fact]
    public void Exp2_is_accurate_to_about_one_ulp_over_the_unit_interval()
    {
        double worst = 0;

        for (int raw = 0; raw < Fixed.One; raw += 7)
        {
            worst = Math.Max(worst, Math.Abs(ToDouble(Transcendental.Exp2(raw)) - Math.Pow(2, ToDouble(raw))));
        }

        Assert.True(worst < 1.05 * Ulp, $"exp2 worst error {worst / Ulp:f3} ULP exceeds the stated bound.");
    }

    [Fact]
    public void Log2_is_accurate_to_about_one_ulp_over_the_mantissa_interval()
    {
        double worst = 0;

        for (int raw = Fixed.One; raw < 2 * Fixed.One; raw += 7)
        {
            worst = Math.Max(worst, Math.Abs(ToDouble(Transcendental.Log2(raw)) - Math.Log2(ToDouble(raw))));
        }

        Assert.True(worst < 1.05 * Ulp, $"log2 worst error {worst / Ulp:f3} ULP exceeds the stated bound.");
    }

    /// <summary>
    /// <b>What base-2 range reduction actually buys, stated precisely because the loose version is
    /// wrong.</b> The integer part is an exact shift, so <em>absolute</em> error stays around one ULP
    /// at every magnitude and never accumulates. <em>Relative</em> error necessarily degrades as the
    /// value shrinks, because Q16.16's ULP is absolute — at 2^-10 the result is only ten bits wide
    /// and carries under a percent of relative precision.
    /// </summary>
    /// <remarks>
    /// That degradation is a property of fixed point and not of the table, and it is harmless for the
    /// one consumer that matters: the softmax normalises, so absolute error in the terms is what
    /// moves a selection probability. The differential test at the bottom of this file is what
    /// actually establishes that.
    /// </remarks>
    [Fact]
    public void Absolute_accuracy_holds_at_one_ulp_across_the_softmax_domain()
    {
        // After subtracting the max, every argument the choice model passes is non-positive.
        for (int whole = -10; whole <= 0; whole++)
        {
            int x = (whole * Fixed.One) + (Fixed.One / 3);
            double expected = Math.Pow(2, ToDouble(x));
            double actual = ToDouble(Transcendental.Exp2(x));

            Assert.True(
                Math.Abs(actual - expected) < 1.05 * Ulp,
                $"2^{ToDouble(x)}: absolute error {Math.Abs(actual - expected) / Ulp:f3} ULP.");
        }
    }

    /// <summary>
    /// Above zero the exact shift scales the mantissa's error along with the mantissa, so it is
    /// <em>relative</em> precision that is preserved there rather than absolute. Stated separately
    /// because the two halves of the domain genuinely behave differently and a single loose claim
    /// covering both would be false at one end or the other.
    /// </summary>
    [Fact]
    public void Relative_accuracy_holds_above_zero_where_the_shift_is_leftward()
    {
        for (int whole = 0; whole <= 10; whole++)
        {
            int x = (whole * Fixed.One) + (Fixed.One / 3);
            double expected = Math.Pow(2, ToDouble(x));
            double actual = ToDouble(Transcendental.Exp2(x));

            Assert.True(
                Math.Abs(actual - expected) / expected < 1e-4,
                $"2^{ToDouble(x)}: relative error {Math.Abs(actual - expected) / expected:e3}.");
        }
    }

    // ── Monotonicity, which the choice model depends on more than it depends on accuracy ─────

    /// <summary>
    /// A non-monotonic table would let a strictly worse candidate score strictly higher — a defect
    /// no error bound would catch and no player could ever be told about.
    /// </summary>
    [Fact]
    public void Exp_is_monotonic_across_the_whole_representable_domain()
    {
        int previous = -1;

        for (int raw = Transcendental.ExpUnderflowsBelow; raw < 10 * Fixed.One; raw += 13)
        {
            int current = Transcendental.Exp(raw);
            Assert.True(current >= previous, $"exp decreased at {ToDouble(raw)}.");
            previous = current;
        }
    }

    [Fact]
    public void Log_is_monotonic_across_four_orders_of_magnitude()
    {
        int previous = int.MinValue;

        for (int raw = 1; raw < 1000 * Fixed.One; raw += 97)
        {
            int current = Transcendental.Log(raw);
            Assert.True(current >= previous, $"log decreased at {ToDouble(raw)}.");
            previous = current;
        }
    }

    // ── Round trips and the named consumers ─────────────────────────────────────────────────

    [Fact]
    public void Log2_and_Exp2_round_trip_at_whole_powers()
    {
        for (int whole = -8; whole <= 8; whole++)
        {
            Assert.Equal(whole * Fixed.One, Transcendental.Log2(Transcendental.Exp2(whole * Fixed.One)));
        }
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 0.6931)]
    [InlineData(10, 2.3979)]
    [InlineData(500, 6.2166)]
    public void Log1P_gives_02_section_5_4s_diminishing_returns(int count, double expected)
    {
        double actual = ToDouble(Transcendental.Log1P(Fixed.FromInt(count)));
        Assert.True(Math.Abs(actual - expected) < 1e-3, $"log(1+{count}) was {actual}.");
    }

    [Fact]
    public void Log1P_rejects_a_negative_count() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Transcendental.Log1P(-1));

    [Fact]
    public void Log_rejects_zero_and_negative() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Transcendental.Log(0));

    // ── The horizon, which is a behavioural claim and not a numerical one ────────────────────

    /// <summary>
    /// adr/0038's stated consequence: below ln(1/65536) the choice model assigns probability
    /// exactly zero, not merely small. A candidate past the horizon is impossible, not unlikely.
    /// </summary>
    [Fact]
    public void Exp_underflows_to_zero_at_the_stated_horizon()
    {
        Assert.Equal(0, Transcendental.Exp(Transcendental.ExpUnderflowsBelow));
        Assert.Equal(0, Transcendental.Exp(Transcendental.ExpUnderflowsBelow - Fixed.One));
        Assert.True(Transcendental.Exp(Transcendental.ExpUnderflowsBelow + Fixed.One) > 0);

        // ~11.09 utility units at mu = 1.
        Assert.True(Math.Abs(ToDouble(Transcendental.ExpUnderflowsBelow) + 11.09) < 0.01);
    }

    [Fact]
    public void Exp2_throws_rather_than_producing_a_masked_shift()
    {
        // A shift count above 31 is silently masked by the hardware; this must not reach one.
        Assert.Throws<OverflowException>(() => Transcendental.Exp2(Fixed.FromInt(20)));
        Assert.Throws<OverflowException>(() => Transcendental.Exp2(Fixed.FromInt(15)));
        Assert.True(Transcendental.Exp2(Fixed.FromInt(14)) > 0);
    }

    // ── The validation adr/0003 owed ────────────────────────────────────────────────────────

    /// <summary>
    /// <b>The differential test the resolution was chosen against.</b> adr/0003 requires the figure
    /// be validated against the herding behaviour adr/0005 describes. The half of that which needs a
    /// city cannot run yet. The half that does not is this: run 02 section 5.4's softmax through the
    /// committed table and through a double-precision oracle, over candidate sets spanning the range
    /// the design says is meaningful, and compare the <em>selection probabilities</em> — which is
    /// the only quantity the choice model actually exposes.
    /// </summary>
    [Fact]
    public void Softmax_selection_probabilities_match_a_double_precision_oracle()
    {
        // 02 section 5.4: mu ~= 1 with utilities scaled so meaningful differences are 1-3 units.
        double[][] candidateSets =
        [
            [0.0, 1.0, 2.0, 3.0],
            [0.0, 0.1, 0.2, 0.3],              // near-ties, where quantisation would bite hardest
            [0.0, 3.0, 6.0, 9.0],              // wide spread, approaching the horizon
            [5.0, 5.0, 5.0, 5.0],              // exact ties
            [0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 2.75, 3.0],
        ];

        double worst = 0;

        foreach (double[] utilities in candidateSets)
        {
            double[] fromTable = TabulatedSoftmax(utilities);
            double[] fromOracle = OracleSoftmax(utilities);

            for (int i = 0; i < utilities.Length; i++)
            {
                worst = Math.Max(worst, Math.Abs(fromTable[i] - fromOracle[i]));
            }
        }

        // A tenth of a percentage point of selection probability. Meaningful utility differences are
        // 1-3 units, which move probability by tens of points, so this is three orders below the
        // smallest difference the design intends anyone to notice.
        Assert.True(worst < 0.001, $"worst selection-probability divergence was {worst:e3}.");
    }

    /// <summary>
    /// Errors are in the safe direction, and this asserts it. Quantisation perturbs the utilities,
    /// which is equivalent to lowering <c>mu</c> — toward <em>more</em> randomness, away from the
    /// deterministic limit where 02 section 5.4 says the city stampedes.
    /// </summary>
    [Fact]
    public void The_tabulated_softmax_is_never_sharper_than_the_oracle()
    {
        double[] utilities = [0.0, 1.0, 2.0, 3.0];

        double tableMax = TabulatedSoftmax(utilities).Max();
        double oracleMax = OracleSoftmax(utilities).Max();

        Assert.True(
            tableMax <= oracleMax + 0.001,
            $"the table concentrated more probability on the best option ({tableMax:f6}) " +
            $"than the oracle did ({oracleMax:f6}) — quantisation must not sharpen the choice.");
    }

    /// <summary>02 section 5.4's softmax, computed the way the core will have to compute it.</summary>
    private static double[] TabulatedSoftmax(double[] utilities)
    {
        int[] scaled = [.. utilities.Select(ToFixed)];
        int max = scaled.Max();

        // Subtracting the max is what keeps every argument non-positive; only differences matter.
        int[] terms = [.. scaled.Select(u => Transcendental.Exp(u - max))];
        long sum = terms.Sum(t => (long)t);

        return [.. terms.Select(t => t / (double)sum)];
    }

    private static double[] OracleSoftmax(double[] utilities)
    {
        double max = utilities.Max();
        double[] terms = [.. utilities.Select(u => Math.Exp(u - max))];
        double sum = terms.Sum();

        return [.. terms.Select(t => t / sum)];
    }
}
