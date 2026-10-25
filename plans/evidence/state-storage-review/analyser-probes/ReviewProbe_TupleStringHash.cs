// A tuple's GetHashCode folds a randomised string hash (and HashCode's per-process seed).
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_TupleStringHash
{
    internal static int Run(string s, int n) => (s, n).GetHashCode();
}
