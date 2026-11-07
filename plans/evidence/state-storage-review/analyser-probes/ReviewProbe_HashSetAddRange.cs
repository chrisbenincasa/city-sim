// Walks a HashSet in hash order through an instance method taking IEnumerable<T>.
using System.Collections.Generic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_HashSetAddRange
{
    internal static int First(HashSet<int> set, List<int> into) { into.AddRange(set); return into[0]; }
}
