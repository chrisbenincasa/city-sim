using System;
using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Shell;

/// <summary>
/// Finds the plot and zone owners that a Tick's changes reach. A plot owner is a Lot slot and a
/// zone owner a permission rectangle slot.
/// </summary>
/// <remarks>
/// A plot reads its Lot and the Segments on its block's edges. A zone reads its rectangle and every
/// occupied Lot over it, so a Lot change reaches the zones under that Lot's occupied ground, before
/// and after.
/// </remarks>
public sealed class PlotTouches
{
    private LandRectangle[] _occupied = [];

    /// <summary>Records every Lot's occupied ground as the baseline for later changes.</summary>
    public void Reset(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _occupied = new LandRectangle[world.Lots.Rows.SlotCount];
        for (int slot = 0; slot < _occupied.Length; slot++) _occupied[slot] = Occupied(world, slot);
    }

    /// <summary>
    /// Adds the changed Lots, the Lots that may stand on <paramref name="ground"/>, and the
    /// changed rectangles, then the rectangles under each added Lot. Moves the baseline to now.
    /// </summary>
    /// <param name="ground">Tile boxes, west, south, east and north, where a road edit could change a block.</param>
    public void Find(
        World world,
        ReadOnlySpan<int> lots,
        ReadOnlySpan<int> rectangles,
        IReadOnlyList<(long West, long South, long East, long North)> ground,
        SortedSet<int> touchedLots,
        SortedSet<int> touchedZones)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(ground);
        ArgumentNullException.ThrowIfNull(touchedLots);
        ArgumentNullException.ThrowIfNull(touchedZones);
        foreach (int slot in lots) touchedLots.Add(slot);
        foreach (var box in ground)
            foreach (int slot in world.LotsInCells.Near(world.Lots, box.West, box.South, box.East, box.North))
                touchedLots.Add(slot);
        foreach (int slot in rectangles) touchedZones.Add(slot);

        if (_occupied.Length < world.Lots.Rows.SlotCount) Array.Resize(ref _occupied, world.Lots.Rows.SlotCount);
        foreach (int slot in touchedLots)
        {
            if (slot >= _occupied.Length) continue;
            world.LandPermissions.Overlapping(_occupied[slot], touchedZones);
            _occupied[slot] = Occupied(world, slot);
            world.LandPermissions.Overlapping(_occupied[slot], touchedZones);
        }
    }

    private static LandRectangle Occupied(World world, int slot)
    {
        LotTable lots = world.Lots;
        if (!lots.Rows.IsLive(slot) || lots.IsVacant(slot)) return default;
        OrientedRectangle parcel = lots.Parcel(slot);
        return parcel.IsValid ? parcel.Bounds : default;
    }
}
