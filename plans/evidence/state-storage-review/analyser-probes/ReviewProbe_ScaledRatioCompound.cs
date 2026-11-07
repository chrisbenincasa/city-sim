// The BOR0207 idiom through a compound assignment.
using Borough.Core.Arithmetic;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_ScaledRatioCompound
{
    internal static int Run(int part, int whole) { part *= 10_000; return IntegerMath.FloorDiv(part, whole); }
}
