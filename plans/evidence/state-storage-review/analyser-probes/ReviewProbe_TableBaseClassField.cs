// A bare array inherited from a base class; BOR0901 reads declared members only.
using Borough.Core.Tables;
namespace Borough.Core.ReviewProbes;
internal readonly struct ReviewProbe_BaseClassRow;
internal abstract class ReviewProbe_TableBase { protected readonly int[] Extra = new int[16]; }
[Table]
internal sealed class ReviewProbe_TableBaseClassField : ReviewProbe_TableBase
{
    private readonly Rows<ReviewProbe_BaseClassRow> _rows = new("probe", 16);
}
