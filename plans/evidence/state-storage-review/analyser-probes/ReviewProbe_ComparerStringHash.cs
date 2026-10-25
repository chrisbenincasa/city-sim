// Randomised string hash through the default comparer rather than string.GetHashCode().
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_ComparerStringHash
{
    internal static int Run(string s) => System.Collections.Generic.EqualityComparer<string>.Default.GetHashCode(s);
}
