// Copies a HashSet out in hash order.
using System.Collections.Generic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_HashSetCopyTo
{
    internal static int First(HashSet<int> set, int[] into) { set.CopyTo(into); return into[0]; }
}
