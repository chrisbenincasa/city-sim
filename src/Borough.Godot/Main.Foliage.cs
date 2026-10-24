using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private readonly List<Rect2> _foliageBuildings = [];
    private readonly Dictionary<Vector2I, FoliageCell> _foliageBlocks = [];
    private long _foliageStamps;
    private bool _foliageChanged;

    /// <summary>The cleared footprints that reach one Cell, and a stamp that changes with them.</summary>
    private sealed class FoliageCell
    {
        public readonly List<Rect2> Bounds = [];
        public long Stamp;
    }

    private void FoliageFootprint(Transform3D transform, int index)
    {
        Vector3 half = (transform.Basis.X.Abs() + transform.Basis.Z.Abs()) * 0.5f;
        var rect = new Rect2(transform.Origin.X - half.X, -transform.Origin.Z - half.Z,
            half.X * 2f, half.Z * 2f);
        if (index == _foliageBuildings.Count)
        {
            _foliageBuildings.Add(rect);
            IndexFootprint(rect, add: true);
            _foliageChanged = true;
        }
        else if (_foliageBuildings[index] != rect)
        {
            IndexFootprint(_foliageBuildings[index], add: false);
            _foliageBuildings[index] = rect;
            IndexFootprint(rect, add: true);
            _foliageChanged = true;
        }
    }

    private void RefreshFoliage(int count)
    {
        if (_foliageBuildings.Count > count)
        {
            for (int at = count; at < _foliageBuildings.Count; at++) IndexFootprint(_foliageBuildings[at], add: false);
            _foliageBuildings.RemoveRange(count, _foliageBuildings.Count - count);
            _foliageChanged = true;
        }
        if (!_foliageChanged) return;
        _foliageChanged = false;
        Scatter();
    }

    private static Vector2I FoliageBlock(Vector2 point) =>
        new(Mathf.FloorToInt(point.X / CellMetres), Mathf.FloorToInt(point.Y / CellMetres));

    private void IndexFootprint(Rect2 footprint, bool add)
    {
        // PROVISIONAL clearance includes the largest crown, not just its trunk.
        Rect2 bounds = footprint.Grow(8f);
        Vector2I first = FoliageBlock(bounds.Position);
        Vector2I last = FoliageBlock(bounds.End);
        for (int y = first.Y; y <= last.Y; y++)
            for (int x = first.X; x <= last.X; x++)
            {
                var key = new Vector2I(x, y);
                if (!_foliageBlocks.TryGetValue(key, out FoliageCell? cell))
                    _foliageBlocks.Add(key, cell = new FoliageCell());
                if (add) cell.Bounds.Add(bounds);
                else cell.Bounds.Remove(bounds);
                cell.Stamp = ++_foliageStamps;
                if (cell.Bounds.Count == 0) _foliageBlocks.Remove(key);
            }
    }

    private bool BuildingExcludesFoliage(Vector2 point)
    {
        if (!_foliageBlocks.TryGetValue(FoliageBlock(point), out FoliageCell? nearby)) return false;
        foreach (Rect2 bounds in nearby.Bounds)
            if (bounds.HasPoint(point)) return true;
        return false;
    }
}
