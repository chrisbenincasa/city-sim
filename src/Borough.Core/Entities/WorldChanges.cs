namespace Borough.Core.Entities;

// Optional, disposable observation at World mutation boundaries; never saved or hashed.
// The journals keep recording while Full is set, so a consumer with its own baseline can still
// read what changed since that baseline.
public sealed class WorldChanges
{
    private readonly Journal _buildings = new(), _lots = new(), _segments = new(), _rectangles = new();
    public bool Full { get; private set; } = true;
    public ReadOnlySpan<int> Buildings => _buildings.Slots;
    public ReadOnlySpan<int> Lots => _lots.Slots;
    public ReadOnlySpan<int> Segments => _segments.Slots;
    public ReadOnlySpan<int> PermissionRectangles => _rectangles.Slots;

    public void Invalidate() => Full = true;

    internal void Building(int slot) => _buildings.Mark(slot);

    internal void Lot(int slot) => _lots.Mark(slot);

    internal void Segment(int slot) => _segments.Mark(slot);

    internal void PermissionRectangle(int slot) => _rectangles.Mark(slot);

    public void Clear()
    {
        _buildings.Clear();
        _lots.Clear();
        _segments.Clear();
        _rectangles.Clear();
        Full = false;
    }

    private sealed class Journal
    {
        private int[] _slots = new int[16];
        private bool[] _marked = new bool[16];
        private int _count;

        public ReadOnlySpan<int> Slots => _slots.AsSpan(0, _count);

        public void Mark(int slot)
        {
            if (slot >= _marked.Length) Array.Resize(ref _marked, checked(slot + _marked.Length));
            if (_marked[slot]) return;
            if (_count == _slots.Length) Array.Resize(ref _slots, checked(_count * 2));
            _marked[slot] = true;
            _slots[_count++] = slot;
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++) _marked[_slots[i]] = false;
            _count = 0;
        }
    }
}
