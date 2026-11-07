// A [Table] column built with the internal constructor: passes BOR0901, never registered with Rows,
// so it is neither saved nor hashed nor grown.
using Borough.Core.Tables;
namespace Borough.Core.ReviewProbes;
internal readonly struct ReviewProbe_DirectColumnRow;
[Table]
internal sealed class ReviewProbe_TableDirectColumn
{
    private readonly Rows<ReviewProbe_DirectColumnRow> _rows;
    internal readonly Column<int> Hidden;
    internal ReviewProbe_TableDirectColumn()
    {
        _rows = new Rows<ReviewProbe_DirectColumnRow>("probe", 16);
        Hidden = new Column<int>(_rows, "hidden", Disposition.Saved, Touch.Wake, 16);
    }
}
