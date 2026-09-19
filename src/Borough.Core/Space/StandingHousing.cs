using Borough.Core.Entities;

namespace Borough.Core.Space;

/// <summary>
/// Live declared housing Buildings, in monotonic Building-id order, independent of future paint.
/// Full and abandoned homes remain candidates; the shared placement filters decide availability.
/// </summary>
public sealed class StandingHousing
{
    private Entry[] _entries = [];
    private int _count;
    private bool _stale = true;

    public void Invalidate() => _stale = true;

    public int Count(World world) { Ensure(world); return _count; }

    /// <summary>Returns the candidate's Lot slot for the common placement assessment.</summary>
    public int Nth(World world, int ordinal)
    {
        Ensure(world);
        if ((uint)ordinal >= (uint)_count) { throw new ArgumentOutOfRangeException(nameof(ordinal)); }
        return _entries[ordinal].Lot;
    }

    public void Rebuild(World world)
    {
        int bound = world.Buildings.Rows.LiveCount;
        if (_entries.Length < bound) { _entries = new Entry[bound]; }
        _count = 0;
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)) { continue; }
            byte kind = world.Buildings.Kind[slot];
            if (!world.Rules.Declares(kind) || !world.Rules.Kind(kind).Houses
                || !world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot)) { continue; }
            _entries[_count++] = new(world.Buildings.Rows.IdAt(slot), lot);
        }
        _entries.AsSpan(0, _count).Sort(static (a, b) => a.Id.CompareTo(b.Id));
        _stale = false;
    }

    private void Ensure(World world) { if (_stale) { Rebuild(world); } }
    private readonly record struct Entry(ulong Id, int Lot);
}
