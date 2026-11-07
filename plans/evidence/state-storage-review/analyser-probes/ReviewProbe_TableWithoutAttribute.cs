// A table that owns Rows and a bare array but carries no [Table]; BOR0901 is opt-in.
using Borough.Core.Tables;
namespace Borough.Core.ReviewProbes;
internal readonly struct ReviewProbe_NoAttributeRow;
internal sealed class ReviewProbe_TableWithoutAttribute
{
    private readonly Rows<ReviewProbe_NoAttributeRow> _rows = new("probe", 16);
    internal readonly int[] Extra = new int[16];
}
