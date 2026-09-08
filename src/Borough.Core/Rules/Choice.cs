using Borough.Core.Arithmetic;

namespace Borough.Core.Rules;

/// <summary>
/// 02 section 5.4's multinomial logit: scored candidates in, one choice out.
/// </summary>
/// <remarks>
/// <para>
/// <b>The interface is the decision and the algorithm is recorded rather than frozen.</b>
/// 02 section 5.4 names Gumbel-max as the same distribution with no <c>exp</c> on the hot path, so a
/// swap here is hash-breaking and distributionally neutral. Everything a caller may rely on is
/// <see cref="Draw"/>'s signature.
/// </para>
/// <para>
/// <b>This is <c>TranscendentalTests.TabulatedSoftmax</c> moved rather than a second derivation.</b>
/// That harness was written against a double-precision oracle before any caller existed and its
/// worst selection-probability divergence is below 0.001; adr/0038 owns the error budget and the
/// argument for 256 entries.
/// </para>
/// <para>
/// ⚠ <b>A candidate more than <c>11.09 / μ</c> utility units below the best is impossible rather
/// than unlikely.</b> <see cref="Transcendental.ExpUnderflowsBelow"/> is a hard horizon and not a
/// taper, so raising μ to make a city more decisive also deletes the tail of every candidate list.
/// adr/0038 states the consequence in full.
/// </para>
/// </remarks>
internal static class Choice
{
    /// <summary>
    /// Picks one of <paramref name="utilities"/> in proportion to <c>exp(μ·V)</c>.
    /// </summary>
    /// <param name="utilities">Each candidate's utility in Q16.16 units.</param>
    /// <param name="mu">
    /// The scale parameter in Q16.16. Above zero; a caller with no μ has no choice model and must
    /// take the argmax it already has.
    /// </param>
    /// <param name="value">One <see cref="Determinism.Randomness.Draw"/>, consumed whole.</param>
    /// <returns>An index into <paramref name="utilities"/>.</returns>
    /// <remarks>
    /// <para>
    /// <b>The maximum is subtracted first</b>, which is what keeps every argument to
    /// <see cref="Transcendental.Exp"/> non-positive. adr/0038's accuracy guarantee is absolute for
    /// non-positive arguments and relative for positive ones, and normalisation needs the absolute
    /// one. Only differences matter in a logit, so the subtraction is free.
    /// </para>
    /// <para>
    /// ⚠ <b>The sum can never be zero.</b> The best candidate's own term is <c>exp(0)</c>, which is
    /// <see cref="Fixed.One"/> exactly, so the modulo below always has a divisor and one candidate
    /// always clears the threshold.
    /// </para>
    /// </remarks>
    public static int Draw(ReadOnlySpan<int> utilities, int mu, ulong value)
    {
        if (utilities.Length == 1)
        {
            return 0;
        }

        int best = utilities[0];

        for (int i = 1; i < utilities.Length; i++)
        {
            if (utilities[i] > best)
            {
                best = utilities[i];
            }
        }

        long sum = 0;

        for (int i = 0; i < utilities.Length; i++)
        {
            sum += Weight(utilities[i], best, mu);
        }

        long threshold = (long)(value % (ulong)sum);
        long running = 0;

        for (int i = 0; i < utilities.Length - 1; i++)
        {
            running += Weight(utilities[i], best, mu);

            if (threshold < running)
            {
                return i;
            }
        }

        return utilities.Length - 1;
    }

    /// <summary>
    /// <c>exp(μ·(V − V_max))</c>, recomputed rather than kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Recomputing costs two table lookups and keeps the caller allocation-free</b> for a
    /// candidate count the Ruleset sets. A scratch buffer sized by <c>[placement] candidates</c>
    /// would be state on a hot path, and the alternative is a fixed cap the Ruleset could not state.
    /// </para>
    /// <para>
    /// ⚠ <b>The scaling is done in 64 bits and clamped rather than left to
    /// <see cref="Fixed.Mul"/>.</b> The difference is unbounded below — a Ruleset stating a small
    /// domain-unit scale makes every utility large — and <c>Mul</c> would throw on the narrowing
    /// where the answer is already known to be zero. <b>A candidate past the horizon is not an
    /// arithmetic failure; it is a candidate nobody would take.</b>
    /// </para>
    /// </remarks>
    private static long Weight(int utility, int best, int mu)
    {
        long scaled = IntegerMath.ShiftRight((long)mu * (utility - best), Fixed.FractionalBits);

        return scaled <= Transcendental.ExpUnderflowsBelow ? 0 : Transcendental.Exp((int)scaled);
    }
}
