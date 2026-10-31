// Raw truncating division via int.DivRem (Math.DivRem is banned; this is not on System.Math).
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_IntDivRem
{
    internal static int Run(int a, int b) => int.DivRem(a, b).Quotient;
}
