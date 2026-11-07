// Float arithmetic with no float literal, local, member, lambda or conversion operation.
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_FloatViaConvert
{
    internal static int Scale(int a, int b) =>
        System.Convert.ToInt32(System.Convert.ToDouble(a) * System.Convert.ToDouble(b));
}
