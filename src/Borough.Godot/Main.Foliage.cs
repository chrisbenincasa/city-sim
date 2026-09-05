using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private readonly List<Rect2> _foliageBuildings = [];
    private readonly Dictionary<Vector2I, List<Rect2>> _foliageBlocks = [];
    private bool _foliageChanged;

    private void FoliageFootprint(Transform3D transform, int index)
    {
        Vector3 half = (transform.Basis.X.Abs() + transform.Basis.Z.Abs()) * 0.5f;
        var rect = new Rect2(transform.Origin.X - half.X, -transform.Origin.Z - half.Z,
            half.X * 2f, half.Z * 2f);
        if (index == _foliageBuildings.Count)
        {
            _foliageBuildings.Add(rect);
            _foliageChanged = true;
        }
        else if (_foliageBuildings[index] != rect)
        {
            _foliageBuildings[index] = rect;
            _foliageChanged = true;
        }
    }

    private void RefreshFoliage(int count)
    {
        if (_foliageBuildings.Count > count)
        {
            _foliageBuildings.RemoveRange(count, _foliageBuildings.Count - count);
            _foliageChanged = true;
        }
        if (!_foliageChanged) return;
        _foliageChanged = false;
        Scatter();
    }

    private static Vector2I FoliageBlock(Vector2 point) =>
        new(Mathf.FloorToInt(point.X / CellMetres), Mathf.FloorToInt(point.Y / CellMetres));

    private void IndexFoliageBuildings()
    {
        _foliageBlocks.Clear();
        foreach (Rect2 footprint in _foliageBuildings)
        {
            // PROVISIONAL clearance includes the largest crown, not just its trunk.
            Rect2 bounds = footprint.Grow(8f);
            Vector2I first = FoliageBlock(bounds.Position);
            Vector2I last = FoliageBlock(bounds.End);
            for (int y = first.Y; y <= last.Y; y++)
            for (int x = first.X; x <= last.X; x++)
            {
                var key = new Vector2I(x, y);
                if (!_foliageBlocks.TryGetValue(key, out var found))
                    _foliageBlocks.Add(key, found = []);
                found.Add(bounds);
            }
        }
    }

    private bool BuildingExcludesFoliage(Vector2 point)
    {
        if (!_foliageBlocks.TryGetValue(FoliageBlock(point), out var nearby)) return false;
        foreach (Rect2 bounds in nearby)
            if (bounds.HasPoint(point)) return true;
        return false;
    }
}
