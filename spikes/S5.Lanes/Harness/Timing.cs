using System.Diagnostics;
using LaneUnits = S5.Lanes.Lanes.Units;

namespace S5.Lanes.Harness;

/// <summary>
/// A self-timed loop, reported as a minimum and a median over repetitions.
/// </summary>
/// <remarks>
/// <para>
/// <b>The minimum is the estimator, and the median is the check on it.</b> A kernel's minimum over
/// repetitions is its cost with the machine's interference removed, which is the quantity a budget
/// wants; the median says how much interference there was. A pair that diverges is a capture taken
/// on a busy machine and should be read as such rather than quoted.
/// </para>
/// <para>
/// <b>Everything is in picoseconds internally</b> so that a per-Vehicle figure has two decimal
/// places without a <c>double</c> ever existing. BOR0201 is on in this project including here, and
/// the one number this spike must never compute in floating point is its own headline.
/// </para>
/// </remarks>
internal static class Timing
{
    internal readonly record struct Sample(long MinimumPicoseconds, long MedianPicoseconds, long WorkUnits)
    {
        /// <summary>Picoseconds per unit of work, taken from the minimum.</summary>
        public long PicosecondsPerUnit => WorkUnits == 0 ? 0 : MinimumPicoseconds / WorkUnits;

        /// <summary>Picoseconds per unit of work, taken from the median.</summary>
        public long MedianPicosecondsPerUnit => WorkUnits == 0 ? 0 : MedianPicoseconds / WorkUnits;

        /// <summary>How many units fit in one 15.6 ms Tick at 4× speed, from the minimum.</summary>
        public long UnitsPerTickBudget => PicosecondsPerUnit == 0
            ? 0
            : (LaneUnits.TickBudgetNanoseconds * 1000L) / PicosecondsPerUnit;
    }

    /// <summary>
    /// Times <paramref name="work"/>, warming for a fixed <em>duration</em> rather than a fixed
    /// number of repetitions.
    /// </summary>
    /// <remarks>
    /// <b>The duration is not a detail and the first capture caught it.</b> Under <c>powersave</c>
    /// the governor ramps the core in response to sustained load, so a rung whose repetition takes
    /// 0.9 ms and a rung whose repetition takes 10 ms arrive at the timed region at different
    /// frequencies if both are warmed a fixed number of times. That produced a denominator curve in
    /// which the 75 MiB working set read *faster* per Vehicle than the 4.6 MiB one, which is not a
    /// thing memory does. Warming by wall-clock time makes every rung enter its measurement in the
    /// same state.
    /// </remarks>
    public static Sample Measure(
        Action work, long unitsPerRepetition, int warmupMilliseconds, int repetitions)
    {
        var warm = Stopwatch.StartNew();
        do
        {
            work();
        }
        while (warm.ElapsedMilliseconds < warmupMilliseconds);

        var elapsed = new long[repetitions];
        var watch = new Stopwatch();

        for (int i = 0; i < repetitions; i++)
        {
            watch.Restart();
            work();
            watch.Stop();
            elapsed[i] = Picoseconds(watch.ElapsedTicks);
        }

        Array.Sort(elapsed);

        return new Sample(elapsed[0], elapsed[repetitions / 2], unitsPerRepetition);
    }

    /// <summary>
    /// As <see cref="Measure"/>, with a setup step run outside the timer before every repetition.
    /// </summary>
    /// <remarks>
    /// Demotion needs this and measuring it by subtraction would have been wrong rather than
    /// merely soft: demotion empties the queues *and* rewrites the Traveller list into queue order,
    /// so the promotion inside a repeated pair walks an already-sorted list and is cheaper than the
    /// promotion measured alone. Subtracting the one from the other would have credited demotion
    /// with promotion's own saving.
    /// </remarks>
    public static Sample MeasureWithSetup(
        Action setup, Action work, long unitsPerRepetition, int warmups, int repetitions)
    {
        for (int i = 0; i < warmups; i++)
        {
            setup();
            work();
        }

        var elapsed = new long[repetitions];
        var watch = new Stopwatch();

        for (int i = 0; i < repetitions; i++)
        {
            setup();
            watch.Restart();
            work();
            watch.Stop();
            elapsed[i] = Picoseconds(watch.ElapsedTicks);
        }

        Array.Sort(elapsed);

        return new Sample(elapsed[0], elapsed[repetitions / 2], unitsPerRepetition);
    }

    /// <summary>
    /// Stopwatch ticks to picoseconds. Widened to <c>Int128</c> because the obvious spelling
    /// overflows: a one-second interval on a 1 GHz timer is 10^9 ticks, and 10^9 × 10^12 is two
    /// orders of magnitude past <c>long</c>.
    /// </summary>
    private static long Picoseconds(long stopwatchTicks) =>
        (long)((Int128)stopwatchTicks * 1_000_000_000_000L / Stopwatch.Frequency);
}
