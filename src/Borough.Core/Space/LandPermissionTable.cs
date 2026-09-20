using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>A geographic permission rectangle, independent of parcel lifetime.</summary>
public readonly struct PermissionRectangle;

/// <summary>Page-contained, disjoint rectangles. Only LandPermissions writes these rows.</summary>
[Table]
public sealed class LandPermissionTable
{
    public LandPermissionTable()
    {
        Rows = new Rows<PermissionRectangle>("land_permission", 8);
        X = Rows.Saved<int>("x", Touch.Cold);
        Y = Rows.Saved<int>("y", Touch.Cold);
        Width = Rows.Saved<int>("width", Touch.Cold);
        Height = Rows.Saved<int>("height", Touch.Cold);
        Permission = Rows.Saved<ulong>("permission", Touch.Cold);
        Next = Rows.Derived<int>("next", Touch.Cold);
        Rows.Seal();
    }

    public Rows<PermissionRectangle> Rows { get; }
    public Column<int> X { get; }
    public Column<int> Y { get; }
    public Column<int> Width { get; }
    public Column<int> Height { get; }
    public Column<ulong> Permission { get; }
    /// <summary>Next row in the Cell, encoded as slot plus one.</summary>
    public Column<int> Next { get; }
}
