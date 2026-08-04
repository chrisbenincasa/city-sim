using System.Numerics;

namespace Borough.Core.Arithmetic;

/// <summary>
/// Tabulated <c>exp</c> and <c>log</c> in Q16.16. adr/0003 bans <c>Math.*</c> from the core and
/// 02 section 5.4's choice model is a softmax over <c>exp</c>, so without this the choice model could
/// not legally be implemented at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>The resolution is 256 entries per table with linear interpolation, and adr/0038 is why.</b>
/// The stopping rule is that the table must not be the limiting factor — Q16.16 is. One ULP is
/// 1/65536 ≈ 1.526e-5; at 256 entries the worst interpolation error is 1.83e-6 for
/// <see cref="Exp2"/> and 2.74e-6 for <see cref="Log2"/>, clearing that by 8.3x and 5.6x. At 128
/// entries <see cref="Log2"/> clears it by only 1.4x, which is too close to call it settled.
/// </para>
/// <para>
/// <b>The tables are committed data, not computed at startup.</b> Generating them would need
/// floating point, which is banned here; <c>TranscendentalTableTests</c> regenerates both in double
/// precision and asserts every entry matches, so the literals are verified rather than trusted.
/// Rounding is to nearest, half away from zero, which is what the error analysis above assumes.
/// </para>
/// <para>
/// <b>Range reduction is base 2 so the integer part is an exact shift.</b> Only the fractional part
/// is ever interpolated, so no error accumulates with magnitude — <c>exp(-10)</c> is as accurate
/// relative to its own value as <c>exp(-1)</c>.
/// </para>
/// <para>
/// <b>These figures are hash-bearing world-creation constants.</b> Changing the table, the entry
/// count or the rounding changes every choice the model makes, which is a design change under
/// 05 section 4 and a re-baseline — not a free optimisation.
/// </para>
/// </remarks>
public static class Transcendental
{
    /// <summary>Entries per table, excluding the duplicated endpoint. adr/0038.</summary>
    public const int TableEntries = 256;

    /// <summary>ln 2 in Q16.16.</summary>
    public const int Ln2 = 45426;

    /// <summary>log₂ e in Q16.16.</summary>
    public const int Log2E = 94548;

    /// <summary>
    /// The most negative argument to <see cref="Exp"/> that does not underflow to zero.
    /// </summary>
    /// <remarks>
    /// ln(1/65536) ≈ -11.09. Below this, Q16.16 cannot represent the result and the choice model
    /// assigns probability exactly zero rather than merely small — a hard horizon, not a taper.
    /// adr/0038 records what that means for <c>μ</c>.
    /// </remarks>
    public const int ExpUnderflowsBelow = -726817;

    /// <summary>2^(i/256) in Q16.16, for i in [0, 256].</summary>
    private static readonly int[] Exp2Table =
    [
         65536,  65714,  65892,  66071,  66250,  66429,  66609,  66790,
         66971,  67153,  67335,  67517,  67700,  67884,  68068,  68252,
         68438,  68623,  68809,  68996,  69183,  69370,  69558,  69747,
         69936,  70126,  70316,  70507,  70698,  70889,  71082,  71274,
         71468,  71661,  71856,  72050,  72246,  72442,  72638,  72835,
         73032,  73230,  73429,  73628,  73828,  74028,  74229,  74430,
         74632,  74834,  75037,  75240,  75444,  75649,  75854,  76060,
         76266,  76473,  76680,  76888,  77096,  77305,  77515,  77725,
         77936,  78147,  78359,  78572,  78785,  78998,  79212,  79427,
         79642,  79858,  80075,  80292,  80510,  80728,  80947,  81166,
         81386,  81607,  81828,  82050,  82273,  82496,  82719,  82944,
         83169,  83394,  83620,  83847,  84074,  84302,  84531,  84760,
         84990,  85220,  85451,  85683,  85915,  86148,  86382,  86616,
         86851,  87086,  87322,  87559,  87796,  88034,  88273,  88513,
         88752,  88993,  89234,  89476,  89719,  89962,  90206,  90451,
         90696,  90942,  91188,  91436,  91684,  91932,  92181,  92431,
         92682,  92933,  93185,  93438,  93691,  93945,  94200,  94455,
         94711,  94968,  95226,  95484,  95743,  96002,  96263,  96524,
         96785,  97048,  97311,  97575,  97839,  98104,  98370,  98637,
         98905,  99173,  99442,  99711,  99982, 100253, 100524, 100797,
        101070, 101344, 101619, 101895, 102171, 102448, 102726, 103004,
        103283, 103564, 103844, 104126, 104408, 104691, 104975, 105260,
        105545, 105831, 106118, 106406, 106694, 106984, 107274, 107565,
        107856, 108149, 108442, 108736, 109031, 109326, 109623, 109920,
        110218, 110517, 110816, 111117, 111418, 111720, 112023, 112327,
        112631, 112937, 113243, 113550, 113858, 114167, 114476, 114787,
        115098, 115410, 115723, 116036, 116351, 116667, 116983, 117300,
        117618, 117937, 118257, 118577, 118899, 119221, 119544, 119869,
        120194, 120519, 120846, 121174, 121502, 121832, 122162, 122493,
        122825, 123158, 123492, 123827, 124163, 124500, 124837, 125176,
        125515, 125855, 126197, 126539, 126882, 127226, 127571, 127917,
        128263, 128611, 128960, 129310, 129660, 130012, 130364, 130718,
        131072,
    ];

    /// <summary>log₂(1 + i/256) in Q16.16, for i in [0, 256].</summary>
    private static readonly int[] Log2Table =
    [
             0,    369,    736,   1102,   1466,   1829,   2190,   2551,
          2909,   3267,   3623,   3978,   4331,   4683,   5034,   5384,
          5732,   6079,   6425,   6769,   7112,   7454,   7795,   8134,
          8473,   8810,   9146,   9480,   9814,  10146,  10477,  10807,
         11136,  11464,  11791,  12116,  12440,  12764,  13086,  13407,
         13727,  14046,  14363,  14680,  14996,  15310,  15624,  15937,
         16248,  16559,  16868,  17177,  17484,  17791,  18096,  18401,
         18704,  19007,  19308,  19609,  19909,  20207,  20505,  20802,
         21098,  21393,  21687,  21980,  22272,  22564,  22854,  23144,
         23433,  23720,  24007,  24293,  24579,  24863,  25146,  25429,
         25711,  25992,  26272,  26551,  26830,  27108,  27384,  27660,
         27936,  28210,  28484,  28757,  29029,  29300,  29571,  29840,
         30109,  30378,  30645,  30912,  31178,  31443,  31707,  31971,
         32234,  32496,  32758,  33019,  33279,  33538,  33797,  34055,
         34312,  34569,  34825,  35080,  35334,  35588,  35841,  36094,
         36346,  36597,  36847,  37097,  37346,  37595,  37842,  38090,
         38336,  38582,  38827,  39072,  39316,  39559,  39802,  40044,
         40286,  40527,  40767,  41006,  41246,  41484,  41722,  41959,
         42196,  42432,  42667,  42902,  43137,  43370,  43603,  43836,
         44068,  44300,  44530,  44761,  44990,  45220,  45448,  45676,
         45904,  46131,  46357,  46583,  46809,  47034,  47258,  47482,
         47705,  47928,  48150,  48372,  48593,  48813,  49034,  49253,
         49472,  49691,  49909,  50127,  50344,  50560,  50776,  50992,
         51207,  51422,  51636,  51850,  52063,  52276,  52488,  52700,
         52911,  53122,  53332,  53542,  53751,  53960,  54169,  54377,
         54584,  54791,  54998,  55204,  55410,  55615,  55820,  56025,
         56229,  56432,  56635,  56838,  57040,  57242,  57443,  57644,
         57845,  58045,  58245,  58444,  58643,  58841,  59039,  59237,
         59434,  59631,  59827,  60023,  60219,  60414,  60609,  60803,
         60997,  61190,  61384,  61576,  61769,  61961,  62152,  62343,
         62534,  62725,  62915,  63104,  63294,  63483,  63671,  63859,
         64047,  64234,  64421,  64608,  64794,  64980,  65166,  65351,
         65536,
    ];

    /// <summary>Exposes the tables for the verification test. Not for simulation use.</summary>
    internal static ReadOnlySpan<int> Exp2Entries => Exp2Table;

    /// <inheritdoc cref="Exp2Entries"/>
    internal static ReadOnlySpan<int> Log2Entries => Log2Table;

    /// <summary>2 raised to a Q16.16 power, in Q16.16. Underflows to zero rather than throwing.</summary>
    /// <exception cref="OverflowException">The result exceeds Q16.16's ±32,768.</exception>
    public static int Exp2(int x)
    {
        int whole = x >> Fixed.FractionalBits;
        int fraction = x & (Fixed.One - 1);

        int index = fraction >> 8;
        int weight = fraction & 0xFF;

        // Rounded, not truncated. Truncating here costs a further half ULP and biases every
        // result downward; rounding leaves the representation's own rounding as the only term.
        int lo = Exp2Table[index];
        int mantissa = lo + (((Exp2Table[index + 1] - lo) * weight) + 128 >> 8);

        if (whole >= 0)
        {
            // A shift past 14 always exceeds Q16.16; check before shifting rather than after,
            // because a shift count above 31 is masked and would produce a plausible wrong answer.
            if (whole > 14)
            {
                throw new OverflowException($"2^{x >> Fixed.FractionalBits} exceeds Q16.16.");
            }

            return checked(mantissa << whole);
        }

        int shift = -whole;
        return shift > 31 ? 0 : mantissa >> shift;
    }

    /// <summary>e raised to a Q16.16 power, in Q16.16. Underflows to zero below ln(1/65536).</summary>
    public static int Exp(int x) => x <= ExpUnderflowsBelow ? 0 : Exp2(Fixed.Mul(x, Log2E));

    /// <summary>Base-2 logarithm of a positive Q16.16 value, in Q16.16.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The argument is zero or negative.</exception>
    public static int Log2(int x)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x);

        // Normalise to m in [1, 2): x == m * 2^exponent.
        int highestBit = 31 - BitOperations.LeadingZeroCount((uint)x);
        int exponent = highestBit - Fixed.FractionalBits;

        int mantissa = exponent >= 0 ? x >> exponent : x << -exponent;
        int offset = mantissa - Fixed.One;

        int index = offset >> 8;
        int weight = offset & 0xFF;

        // offset == One only if x was an exact power of two above the table's last entry.
        if (index >= TableEntries)
        {
            return exponent << Fixed.FractionalBits;
        }

        int lo = Log2Table[index];
        int fraction = lo + (((Log2Table[index + 1] - lo) * weight) + 128 >> 8);

        return checked((exponent << Fixed.FractionalBits) + fraction);
    }

    /// <summary>Natural logarithm of a positive Q16.16 value, in Q16.16.</summary>
    public static int Log(int x) => Fixed.Mul(Log2(x), Ln2);

    /// <summary>
    /// <c>log(1 + x)</c> for a non-negative Q16.16 value — 02 section 5.4's diminishing-returns form
    /// for count-like utility terms, so that the 500th reachable job matters less than the 5th.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The argument is negative.</exception>
    public static int Log1P(int x)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        return Log(checked(x + Fixed.One));
    }
}
