// Randomised string hash through the StringComparison overload.
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_StringHashOverload
{
    internal static int Run(string s) => s.GetHashCode(System.StringComparison.Ordinal);
}
