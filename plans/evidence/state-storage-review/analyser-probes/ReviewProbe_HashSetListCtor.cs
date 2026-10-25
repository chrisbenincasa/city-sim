// Walks a HashSet in hash order through a constructor taking IEnumerable<T>.
using System.Collections.Generic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_HashSetListCtor
{
    internal static int First(HashSet<int> set) => new List<int>(set)[0];
}
