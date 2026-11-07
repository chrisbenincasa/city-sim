// Raw truncating integer division through a generic-math operator (OperatorMethod is non-null).
using System.Numerics;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_GenericDivision
{
    private static T Div<T>(T a, T b) where T : IDivisionOperators<T, T, T> => a / b;
    internal static int Run(int a, int b) => Div(a, b);
}
