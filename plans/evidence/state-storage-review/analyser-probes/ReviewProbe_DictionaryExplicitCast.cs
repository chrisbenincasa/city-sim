// An explicit cast before foreach; Unwrap peels implicit conversions only.
using System.Collections.Generic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_DictionaryExplicitCast
{
    internal static int Run(Dictionary<int, int> map)
    {
        int t = 0;
        foreach (KeyValuePair<int, int> kv in (IEnumerable<KeyValuePair<int, int>>)map) { t = t * 31 + kv.Key; }
        return t;
    }
}
