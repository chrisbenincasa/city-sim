// The BOR0207 idiom split across a local: the 32-bit multiply wraps before FloorDiv sees it.
using Borough.Core.Arithmetic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_ScaledRatioViaLocal
{
    internal static int Run(int part, int whole) { int scaled = part * 10_000; return IntegerMath.FloorDiv(scaled, whole); }
}
