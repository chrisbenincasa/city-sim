// Math.Sqrt banned; double.Sqrt (generic-math static on System.Double) is the same call.
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_DoubleSqrt
{
    internal static int Root(int a) =>
        System.Convert.ToInt32(double.Sqrt(System.Convert.ToDouble(a)));
}
