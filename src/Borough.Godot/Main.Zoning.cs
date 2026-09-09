using System;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private (Tiles East, Tiles North)? _zoneStart;
    private bool _zoneErase, _zoneMouseDown;
    private bool _zoneParcels = true;
    private InstanceLayer _zones = null!;

    private static Color ZoneColour(ushort permission) => (permission & (LotTable.Housing | LotTable.Trade)) switch
    {
        LotTable.Housing => new Color("62bf70"),
        LotTable.Trade => new Color("579ee0"),
        LotTable.Housing | LotTable.Trade => new Color("ab83dc"),
        _ => new Color("d5b76b"),
    };

    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)> ZonedBlocks()
    {
        var lots = _world.Lots;
        var occupied = new System.Collections.Generic.HashSet<(int, int)>();
        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.HasFrontage(slot)) continue;
            if (Frontage.BlockOf(_world.Roads.Streets, lots.East[slot], lots.North[slot],
                (StreetSide)lots.Side[slot], out int c, out int r)) occupied.Add((c, r));
            if (lots.Zone[slot] == 0 || lots.ParcelWide[slot].Raw <= 0 || lots.ParcelDeep[slot].Raw <= 0) continue;
            yield return (lots.Rows.IdAt(slot), ParcelTransform(lots.ParcelEast[slot].Raw,
                lots.ParcelNorth[slot].Raw, lots.ParcelWide[slot].Raw, lots.ParcelDeep[slot].Raw),
                ZoneColour(lots.Zone[slot]).SrgbToLinear());
        }
        var blocks = _world.Blocks;
        for (int slot = 0; slot < blocks.Rows.SlotCount; slot++)
            if (blocks.Rows.IsLive(slot) && blocks.Zone[slot] != 0
                && !occupied.Contains((blocks.LatticeColumn[slot], blocks.LatticeRow[slot]))
                && ZoneInterior(blocks.LatticeColumn[slot], blocks.LatticeRow[slot]) is { } interior)
                yield return (blocks.Rows.IdAt(slot), interior, ZoneColour(blocks.Zone[slot]).SrgbToLinear());
    }
    private string _zoneFeedback = string.Empty;

    private ushort ZonePermission() => _zoneErase || _world.Rules.ZoneRules.Length == 0
        ? (ushort)0 : _world.Rules.ZoneRules[_zoneChoice].Admits;
    private string ZoneName() => _zoneErase ? "Erase zoning"
        : _names.ZoneRule(_zoneChoice) ?? "Zoning";

    private bool ZoningAction(string[] words)
    {
        if (words[0] == "zone-size")
        {
            if (words.Length != 2 || words[1] is not ("parcels" or "blocks"))
            { _refused = "Choose parcels or blocks."; return true; }
            _zoneParcels = words[1] == "parcels";
            _zoneStart = null;
            _zoneFeedback = string.Empty;
            return true;
        }
        if (words[0] == "zone-cancel")
        {
            _zoneStart = null;
            _zoneFeedback = "Zoning stroke cancelled.";
            _refused = string.Empty;
            return true;
        }
        if (words[0] is not ("zone-begin" or "zone-end" or "zone-point")) return false;
        if (words.Length != 3 || !int.TryParse(words[1], out int east) || !int.TryParse(words[2], out int north))
        { _zoneStart = null; _refused = "Choose ground inside the map."; return true; }
        if (!_zoneErase && _world.Rules.ZoneRules.Length == 0)
        { _zoneStart = null; _refused = "No development permissions are available in this city."; return true; }
        StreetGrid streets = _world.Roads.Streets;
        if (_verb != Verb.Zone || streets.Blocks <= 0 || east < 0 || north < 0
            || east >= CellGrid.WorldTiles || north >= CellGrid.WorldTiles
            || streets.Lattice.LineAt(east) >= streets.Blocks || streets.Lattice.LineAt(north) >= streets.Blocks)
        { _zoneStart = null; _refused = "This ground has no editable blocks."; return true; }
        var at = (new Tiles(east), new Tiles(north));
        _aimed = at;
        if (words[0] == "zone-point") return true;
        if (words[0] == "zone-begin")
        { _zoneStart = at; _refused = string.Empty; _zoneFeedback = string.Empty; return true; }
        if (_zoneStart is null) { _refused = "Start a Zoning rectangle first."; return true; }
        if (_zoneParcels)
        {
            int painted = 0;
            foreach (var parcel in SelectedParcels(at))
                if (Send(new Command(CommandKind.ZoneParcel,
                    new Tiles(parcel.East.Raw + parcel.Wide.Raw / 2),
                    new Tiles(parcel.North.Raw + parcel.Deep.Raw / 2), ZonePermission()))) painted++;
            _zoneStart = null;
            _zoneFeedback = painted == 0 ? "Choose a parcel beside a Street."
                : $"{painted} parcels queued: {ZoneName()}. Existing Buildings stay.";
            return true;
        }
        var bounds = ZoneBounds(at);
        _zoneStart = null;
        int count = 0, unchanged = 0;
        bool canSkip = _queued.Count == 0;
        ushort permission = ZonePermission();
        int width = bounds.East - bounds.West + 1;
        var different = new bool[width * (bounds.North - bounds.South + 1)];
        for (int slot = 0; slot < _world.Lots.Rows.SlotCount; slot++)
        {
            if (!_world.Lots.Rows.IsLive(slot) || _world.Lots.Zone[slot] == permission
                || !Frontage.BlockOf(streets, _world.Lots.East[slot], _world.Lots.North[slot],
                    (StreetSide)_world.Lots.Side[slot], out int c, out int r)
                || c < bounds.West || c > bounds.East || r < bounds.South || r > bounds.North) continue;
            different[(r - bounds.South) * width + c - bounds.West] = true;
        }
        for (int row = bounds.South; row <= bounds.North; row++)
            for (int column = bounds.West; column <= bounds.East; column++)
            {
                int block = _world.BlockIndex.Contains(column, row) ? _world.BlockIndex.Slot(column, row) : Rows.NoSlot;
                ushort previous = block == Rows.NoSlot ? (ushort)0 : _world.Blocks.Zone[block];
                if (canSkip && previous == permission && !different[(row - bounds.South) * width + column - bounds.West])
                { unchanged++; continue; }
                var tile = streets.IntersectionTile(column, row);
                if (Send(new Command(CommandKind.Zone, tile.East, tile.North, ZonePermission()))) count++;
            }
        _zoneFeedback = count == 0 ? $"{unchanged} blocks already have these permissions; nothing changed."
            : $"{count} blocks queued: {ZoneName()}; {unchanged} unchanged. Existing Buildings stay.";
        return true;
    }

    private (int West, int South, int East, int North) ZoneBounds((Tiles East, Tiles North) at)
    {
        var lattice = _world.Roads.Streets.Lattice;
        var start = _zoneStart ?? at;
        int a = lattice.LineAt(start.East.Raw), b = lattice.LineAt(start.North.Raw);
        int c = lattice.LineAt(at.East.Raw), d = lattice.LineAt(at.North.Raw);
        return (Math.Min(a, c), Math.Min(b, d), Math.Max(a, c), Math.Max(b, d));
    }

    private int ZoneSelectionCount()
    {
        if (_zoneStart is null || Aim() is not { } at) return 0;
        if (_zoneParcels) return System.Linq.Enumerable.Count(SelectedParcels(at));
        var bounds = ZoneBounds(at);
        return (bounds.East - bounds.West + 1) * (bounds.North - bounds.South + 1);
    }

    private void ZonePreview((Tiles East, Tiles North) at)
    {
        if (_zoneParcels)
        {
            int selected = 0;
            foreach (var parcel in SelectedParcels(at))
            {
                _cursor.Multimesh.SetInstanceTransform(selected, ParcelTransform(parcel.East.Raw,
                    parcel.North.Raw, parcel.Wide.Raw, parcel.Deep.Raw));
                _cursor.Multimesh.SetInstanceColor(selected++, (_zoneErase ? new Color("e7a095")
                    : ZoneColour(ZonePermission()).Lightened(.22f)).SrgbToLinear());
            }
            _cursor.Multimesh.VisibleInstanceCount = selected;
            return;
        }
        var bounds = ZoneBounds(at);
        var streets = _world.Roads.Streets;
        int count = 0;
        for (int row = Math.Max(0, bounds.South); row <= Math.Min(streets.Blocks - 1, bounds.North); row++)
            for (int column = Math.Max(0, bounds.West); column <= Math.Min(streets.Blocks - 1, bounds.East); column++)
            {
                if (ZoneInterior(column, row) is not { } interior) continue;
                interior.Origin += new Vector3(0, .01f, 0);
                _cursor.Multimesh.SetInstanceTransform(count, interior);
                _cursor.Multimesh.SetInstanceColor(count++, (_zoneErase ? new Color("e7a095")
                    : ZoneColour(ZonePermission()).Lightened(.22f)).SrgbToLinear());
            }
        _cursor.Multimesh.VisibleInstanceCount = count;
    }

    private static Transform3D ParcelTransform(int east, int north, int wide, int deep) =>
        new(Basis.FromScale(new Vector3(wide * MetresPerTile, .01f, deep * MetresPerTile)),
            new Vector3((east + wide * .5f) * MetresPerTile, .045f, -(north + deep * .5f) * MetresPerTile));

    private System.Collections.Generic.IEnumerable<Parcel> SelectedParcels((Tiles East, Tiles North) at)
    {
        var start = _zoneStart ?? at;
        int west = Math.Min(start.East.Raw, at.East.Raw), east = Math.Max(start.East.Raw, at.East.Raw);
        int south = Math.Min(start.North.Raw, at.North.Raw), north = Math.Max(start.North.Raw, at.North.Raw);
        var bounds = ZoneBounds(at);
        var streets = _world.Roads.Streets;
        var buffer = new Parcel[_world.Rules.Lots.ParcelCeiling(BlockGround.Square(streets.Lattice.Widest))];
        for (int row = Math.Max(0, bounds.South); row <= Math.Min(streets.Blocks - 1, bounds.North); row++)
            for (int column = Math.Max(0, bounds.West); column <= Math.Min(streets.Blocks - 1, bounds.East); column++)
            {
                int count = LotSubdivider.Preview(_world, column, row, buffer);
                for (int i = 0; i < count; i++)
                {
                    var p = buffer[i];
                    if (p.East.Raw <= east && p.East.Raw + p.Wide.Raw > west
                        && p.North.Raw <= north && p.North.Raw + p.Deep.Raw > south) yield return p;
                }
            }
    }

    private Transform3D? ZoneInterior(int column, int row)
    {
        var streets = _world.Roads.Streets;
        var lattice = streets.Lattice;
        float west = lattice.EdgeOf(column) * MetresPerTile + ZoneStreetInset(streets.Vertical(column, row));
        float east = lattice.EdgeOf(column + 1) * MetresPerTile - ZoneStreetInset(streets.Vertical(column + 1, row));
        float south = lattice.EdgeOf(row) * MetresPerTile + ZoneStreetInset(streets.Horizontal(column, row));
        float north = lattice.EdgeOf(row + 1) * MetresPerTile - ZoneStreetInset(streets.Horizontal(column, row + 1));
        if (east <= west || north <= south) return null;
        return new Transform3D(Basis.FromScale(new Vector3(east - west, .01f, north - south)),
            new Vector3((west + east) / 2, .02f, -(south + north) / 2));
    }

    private float ZoneStreetInset(int segment)
    {
        if (segment == Rows.NoSlot) return 0;
        return (RoadKind)_world.Roads.Segments.Kind[segment] switch
        {
            RoadKind.Arterial => ArterialWidthMetres * .5f + FootwayWidthMetres,
            RoadKind.FootPath => FootPathWidthMetres * .5f,
            _ => CarriagewayWidthMetres * .5f + FootwayWidthMetres,
        };
    }
}
