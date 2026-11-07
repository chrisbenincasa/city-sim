// Non-constant shift through a generic-math operator; the count is masked as with <<.
using System.Numerics;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_GenericShift
{
    private static T Shl<T>(T a, int n) where T : IShiftOperators<T, int, T> => a << n;
    internal static int Run(int a, int n) => Shl(a, n);
}
