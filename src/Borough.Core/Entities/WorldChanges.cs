namespace Borough.Core.Entities;

// Optional, disposable observation at World mutation boundaries; never saved or hashed.
public sealed class WorldChanges
{
    private int[] _buildings = new int[16];
    private bool[] _marked = new bool[16];
    private int _count;
    public bool Full { get; private set; } = true;
    public ReadOnlySpan<int> Buildings => _buildings.AsSpan(0, _count);

    public void Invalidate() => Full = true;

    internal void Building(int slot)
    {
        if (Full) return;
        if (slot >= _marked.Length) Array.Resize(ref _marked, checked(slot + _marked.Length));
        if (_marked[slot]) return;
        if (_count == _buildings.Length) Array.Resize(ref _buildings, checked(_count * 2));
        _marked[slot] = true;
        _buildings[_count++] = slot;
    }

    public void Clear()
    {
        for (int i = 0; i < _count; i++) _marked[_buildings[i]] = false;
        _count = 0;
        Full = false;
    }
}
