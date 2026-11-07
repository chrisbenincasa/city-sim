// A HashSet held as IEnumerable<T> (or passed to a helper that takes one) and walked there.
using System.Collections.Generic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_HashSetUpcastForeach
{
    internal static int Run(HashSet<int> set) => Sum(set);
    private static int Sum(IEnumerable<int> items) { int t = 0; foreach (int i in items) { t = t * 31 + i; } return t; }
}
