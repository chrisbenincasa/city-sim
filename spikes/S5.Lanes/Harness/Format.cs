using System.Globalization;

namespace S5.Lanes.Harness;

/// <summary>
/// Number formatting with no floating point anywhere, because BOR0201 is on in this project and
/// because S5's whole subject is what happens when the floats are taken away.
/// </summary>
internal static class Format
{
    /// <summary>Picoseconds as nanoseconds with two decimal places.</summary>
    public static string Nanoseconds(long picoseconds)
    {
        long whole = picoseconds / 1000;
        long hundredths = (picoseconds % 1000) / 10;
        if (picoseconds < 0)
        {
            hundredths = -hundredths;
        }

        return string.Create(
            CultureInfo.InvariantCulture, $"{whole}.{hundredths:00}");
    }

    /// <summary>Picoseconds as milliseconds with three decimal places.</summary>
    public static string Milliseconds(long picoseconds)
    {
        long whole = picoseconds / 1_000_000_000L;
        long thousandths = (picoseconds % 1_000_000_000L) / 1_000_000L;
        return string.Create(CultureInfo.InvariantCulture, $"{whole}.{thousandths:000}");
    }

    /// <summary>A ratio <c>numerator / denominator</c> as a multiplier with two decimals.</summary>
    public static string Ratio(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            return "n/a";
        }

        long hundredths = (numerator * 100L) / denominator;
        return string.Create(
            CultureInfo.InvariantCulture, $"{hundredths / 100}.{Abs(hundredths % 100):00}×");
    }

    /// <summary>A ratio as a percentage with two decimals.</summary>
    public static string Percent(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            return "n/a";
        }

        long hundredths = (numerator * 10_000L) / denominator;
        return string.Create(
            CultureInfo.InvariantCulture, $"{hundredths / 100}.{Abs(hundredths % 100):00}%");
    }

    public static string Count(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    private static long Abs(long value) => value < 0 ? -value : value;
}
