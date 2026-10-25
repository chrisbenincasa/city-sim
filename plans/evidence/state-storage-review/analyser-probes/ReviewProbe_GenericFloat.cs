// Float arithmetic inside a generic instantiated at double.
using System.Numerics;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_GenericFloat
{
    private static T Half<T>(T x) where T : INumber<T> => x / (T.One + T.One);
    internal static int Run(int a) => System.Convert.ToInt32(Half(System.Convert.ToDouble(a)));
}
