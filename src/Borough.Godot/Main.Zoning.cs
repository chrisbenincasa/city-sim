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

    /// <summary>
    /// Each zoned rectangle nothing has been built on yet, drawn whole, the block's unlotted middle
    /// included. The first built Lot on it clears the rectangle's wash.
    /// </summary>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)> ZonedBlocks()
    {
        var paint = _world.PermissionRectangles;
        LotTable lots = _world.Lots;
        var built = new System.Collections.Generic.List<OrientedRectangle>();
        for (int lot = 0; lot < lots.Rows.SlotCount; lot++)
        {
            if (!lots.Rows.IsLive(lot) || lots.IsVacant(lot)) continue;
            OrientedRectangle parcel = lots.Parcel(lot);
            if (parcel.IsValid) built.Add(parcel);
        }

        for (int slot = 0; slot < paint.Rows.SlotCount; slot++)
        {
            if (!paint.Rows.IsLive(slot)) continue;
            ushort uses = GroundPermissions.Unpack(paint.Permission[slot]).Uses;
            if (uses == 0) continue;
            var zoned = OrientedRectangle.FromBounds(new LandRectangle(
                paint.X[slot], paint.Y[slot], paint.Width[slot], paint.Height[slot]));
            if (built.Exists(zoned.Overlaps)) continue;
            yield return (paint.Rows.IdAt(slot), ParcelTransform(paint.X[slot], paint.Y[slot],
                paint.Width[slot], paint.Height[slot]), ZoneColour(uses).SrgbToLinear());
        }
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
        if (_verb != Verb.Zone || east < 0 || north < 0 || east >= CellGrid.WorldTiles || north >= CellGrid.WorldTiles)
        { _zoneStart = null; _refused = "Choose ground inside the map."; return true; }
        var at = (new Tiles(east), new Tiles(north));
        _aimed = at;
        if (words[0] == "zone-point") return true;
        if (words[0] == "zone-begin")
        { _zoneStart = at; _refused = string.Empty; _zoneFeedback = string.Empty; return true; }
        if (_zoneStart is null) { _refused = "Start a Zoning rectangle first."; return true; }
        if (!OnLattice(at))
        {
            _zoneStart = null;
            var paint = new Command(CommandKind.Zone, at.Item1, at.Item2, ZonePermission());
            if (Send(paint))
            {
                _refused = string.Empty;
                NoteZonePainted(paint);
                PushZoneStroke();
                _zoneFeedback = $"{(_world.TryZoneGround(at.Item1, at.Item2, out ZoneGround ground) && ground.Face >= 0 ? "Block" : "Street side")} queued: {ZoneName()}. Existing Buildings stay.";
            }
            return true;
        }
        if (_zoneParcels)
        {
            int painted = 0;
            foreach (var parcel in SelectedParcels(at))
            {
                var middle = (East: new Tiles(parcel.East.Raw + parcel.Wide.Raw / 2),
                    North: new Tiles(parcel.North.Raw + parcel.Deep.Raw / 2));
                var paint = new Command(CommandKind.ZoneParcel, middle.East, middle.North, ZonePermission());
                if (!Send(paint)) continue;
                NoteZonePainted(paint);
                painted++;
            }
            PushZoneStroke();
            _zoneStart = null;
            _zoneFeedback = painted == 0 ? "Choose a parcel beside a Street."
                : $"{painted} parcels queued: {ZoneName()}. Existing Buildings stay.";
            return true;
        }
        var sent = WholeBlockCommands(at, out int unchanged);
        _zoneStart = null;
        int count = 0;
        foreach (var tile in sent)
        {
            var paint = new Command(CommandKind.Zone, tile.East, tile.North, ZonePermission());
            if (!Send(paint)) continue;
            NoteZonePainted(paint);
            count++;
        }
        PushZoneStroke();
        _zoneFeedback = count == 0 ? $"{unchanged} blocks already have these permissions; nothing changed."
            : $"{count} blocks queued: {ZoneName()}; {unchanged} unchanged. Existing Buildings stay.";
        return true;
    }

    /// <summary>Whether a Tile lies in a lattice square with a lattice Street on some side. Other ground zones by face or Street side.</summary>
    private bool OnLattice((Tiles East, Tiles North) at)
    {
        StreetGrid streets = _world.Roads.Streets;
        if (streets.Blocks <= 0) return false;
        int column = streets.Lattice.LineAt(at.East.Raw), row = streets.Lattice.LineAt(at.North.Raw);
        if (column >= streets.Blocks || row >= streets.Blocks) return false;
        return streets.Horizontal(column, row) != Rows.NoSlot || streets.Horizontal(column, row + 1) != Rows.NoSlot
            || streets.Vertical(column, row) != Rows.NoSlot || streets.Vertical(column + 1, row) != Rows.NoSlot;
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
        if (!OnLattice(at)) return _world.TryZoneGround(at.East, at.North, out _) ? 1 : 0;
        if (_zoneParcels) return System.Linq.Enumerable.Count(SelectedParcels(at));
        return PreviewBlockCount(at);
    }

    private (int West, int South, int East, int North) PreviewBounds((Tiles East, Tiles North) at)
    {
        var bounds = ZoneBounds(at);
        int last = _world.Roads.Streets.Blocks - 1;
        return (Math.Max(0, bounds.West), Math.Max(0, bounds.South),
            Math.Min(last, bounds.East), Math.Min(last, bounds.North));
    }

    private int PreviewBlockCount((Tiles East, Tiles North) at)
    {
        var bounds = PreviewBounds(at);
        return Math.Max(0, bounds.East - bounds.West + 1) * Math.Max(0, bounds.North - bounds.South + 1);
    }

    /// <summary>The Tile of each <c>Zone</c> command a whole-block stroke ending here sends, in send order.</summary>
    private System.Collections.Generic.List<(Tiles East, Tiles North)> WholeBlockCommands(
        (Tiles East, Tiles North) at, out int unchanged)
    {
        var bounds = ZoneBounds(at);
        var streets = _world.Roads.Streets;
        var tiles = new System.Collections.Generic.List<(Tiles East, Tiles North)>();
        unchanged = 0;
        bool canSkip = _queued.Count == 0;
        ushort permission = ZonePermission();
        for (int row = bounds.South; row <= bounds.North; row++)
            for (int column = bounds.West; column <= bounds.East; column++)
            {
                var previous = _world.LandPermissions.Summary(_world.BlockGroundRectangle(column, row));
                if (canSkip && previous.CommonUses == permission && previous.AnyUses == permission)
                { unchanged++; continue; }
                tiles.Add(streets.IntersectionTile(column, row));
            }
        return tiles;
    }

    private static string LotsToCreate(int lots) => $"{lots} Lot{(lots == 1 ? "" : "s")} to create";

    private string WholeBlockPreviewText((Tiles East, Tiles North) at)
    {
        int blocks = PreviewBlockCount(at);
        return $"{LotsToCreate(ZonePlots(at).Count)} across {blocks} block{(blocks == 1 ? "" : "s")}";
    }

    private string FreeformPreviewText((Tiles East, Tiles North) at)
    {
        if (!_world.TryZoneGround(at.East, at.North, out ZoneGround ground)) return "no Street within reach to zone beside";
        return $"{LotsToCreate(ZonePlots(at).Count)} on this {(ground.Face >= 0 ? "block" : "Street side")}";
    }

    private readonly record struct ZonePlotsKey(object? World, object? Roads, uint Version, object? Rules,
        ulong LotIds, int Lots, ulong PaintIds, int Paint, ushort Zone, bool Queued, bool Blocks, int A, int B, int C, int D);

    private ZonePlotsKey _zonePlotsAt;
    private readonly System.Collections.Generic.List<Parcel> _zonePlots = [];
    private int _zoneCarves;

    /// <summary>The Lots the stroke ending here would create, from Core's read-only carve.</summary>
    /// <remarks>
    /// Carved again only when the hovered ground, the Streets, the permission, the Lots or the paint
    /// change. Lot and paint rows are created and freed, never edited. A change that creates a row
    /// moves its id counter and one that only frees rows lowers the live count.
    /// </remarks>
    private System.Collections.Generic.List<Parcel> ZonePlots((Tiles East, Tiles North) at)
    {
        ushort zone = ZonePermission();
        bool blocks = !_zoneParcels && OnLattice(at);
        (int A, int B, int C, int D) ground = (-1, Rows.NoSlot, 0, -1);
        if (blocks) ground = PreviewBounds(at);
        else if (_world.TryZoneGround(at.East, at.North, out ZoneGround g)) ground = (g.Face, g.Segment, (int)g.Side, 0);
        var key = new ZonePlotsKey(_world, _world.Roads, _world.Roads.Version, _world.Rules,
            _world.Lots.Rows.NextId, _world.Lots.Rows.LiveCount, _world.PermissionRectangles.Rows.NextId, _world.PermissionRectangles.Rows.LiveCount,
            zone, blocks && _queued.Count == 0, blocks, ground.A, ground.B, ground.C, ground.D);
        if (key == _zonePlotsAt) return _zonePlots;
        _zonePlotsAt = key;
        _zonePlots.Clear();
        if (zone == 0 || (!blocks && ground.D < 0)) return _zonePlots;
        _zoneCarves++;
        if (blocks) LotSubdivider.PreviewPaint(_world, [.. WholeBlockCommands(at, out _)], zone, _zonePlots);
        else LotSubdivider.PreviewPaint(_world, [at], zone, _zonePlots);
        return _zonePlots;
    }

    private void DrawZonePlots((Tiles East, Tiles North) at, int from)
    {
        var plots = ZonePlots(at);
        Color colour = ZoneColour(ZonePermission()).Lightened(.45f).SrgbToLinear();
        for (int i = 0; i < plots.Count; i++)
        {
            _cursor.Multimesh.SetInstanceTransform(from + i, Plate(plots[i].Geometry, -.35f, .04f, .01f));
            _cursor.Multimesh.SetInstanceColor(from + i, colour);
        }
        _cursor.Multimesh.VisibleInstanceCount = from + plots.Count;
    }

    private void ZonePreview((Tiles East, Tiles North) at)
    {
        if (!OnLattice(at))
        {
            FreeformZonePreview(at);
            return;
        }
        if (_zoneParcels)
        {
            int selectedParcelsCount = 0;
            foreach (var parcel in SelectedParcels(at))
            {
                _cursor.Multimesh.SetInstanceTransform(selectedParcelsCount, ParcelTransform(parcel.East.Raw,
                    parcel.North.Raw, parcel.Wide.Raw, parcel.Deep.Raw));
                _cursor.Multimesh.SetInstanceColor(selectedParcelsCount++, (_zoneErase ? new Color("e7a095")
                    : ZoneColour(ZonePermission()).Lightened(.22f)).SrgbToLinear());
            }
            _cursor.Multimesh.VisibleInstanceCount = selectedParcelsCount;
            return;
        }
        int selected = 0;
        foreach (var parcel in SelectedParcels(at, false))
        {
            _cursor.Multimesh.SetInstanceTransform(selected, ParcelTransform(parcel.East.Raw,
                parcel.North.Raw, parcel.Wide.Raw, parcel.Deep.Raw));
            _cursor.Multimesh.SetInstanceColor(selected++, (_zoneErase ? new Color("e7a095")
                : ZoneColour(ZonePermission()).Lightened(.22f)).SrgbToLinear());
        }
        DrawZonePlots(at, selected);
    }

    private (Tiles East, Tiles North, object? Roads, uint Version, object? Rules) _zoneGroundAt = (new Tiles(-1), default, null, 0, null);
    private readonly System.Collections.Generic.List<Transform3D> _zoneGroundRuns = [];

    /// <summary>Draws the face or Street side a click here would paint, as one box per row run of its Tiles.</summary>
    private void FreeformZonePreview((Tiles East, Tiles North) at)
    {
        var key = (at.East, at.North, (object?)_world.Roads, _world.Roads.Version, (object?)_world.Rules);
        if (key != _zoneGroundAt)
        {
            _zoneGroundAt = key;
            _zoneGroundRuns.Clear();
            if (_world.TryZoneGround(at.East, at.North, out ZoneGround ground))
            {
                LandRectangle box = ground.Bounds;
                for (int y = box.Y; y < box.Y + box.Height; y++)
                    for (int x = box.X; x < box.X + box.Width;)
                    {
                        if (!ground.Contains(x, y)) { x++; continue; }
                        int end = x + 1;
                        while (end < box.X + box.Width && ground.Contains(end, y)) end++;
                        _zoneGroundRuns.Add(ParcelTransform(x, y, end - x, 1));
                        x = end;
                    }
            }
        }
        Color colour = (_zoneErase ? new Color("e7a095") : ZoneColour(ZonePermission()).Lightened(.22f)).SrgbToLinear();
        for (int i = 0; i < _zoneGroundRuns.Count; i++)
        {
            Transform3D run = _zoneGroundRuns[i];
            run.Origin += new Vector3(0, .01f, 0);
            _cursor.Multimesh.SetInstanceTransform(i, run);
            _cursor.Multimesh.SetInstanceColor(i, colour);
        }
        DrawZonePlots(at, _zoneGroundRuns.Count);
    }

    // At the plot layer's height, which it replaces while zoning: car parks, stall lines and roads
    // sit above it. At 0.045 its top was coplanar with the road top and its slab with the stall lines.
    private static Transform3D ParcelTransform(int east, int north, int wide, int deep) =>
        new(Basis.FromScale(new Vector3(wide * MetresPerTile, .01f, deep * MetresPerTile)),
            new Vector3((east + wide * .5f) * MetresPerTile, .02f, -(north + deep * .5f) * MetresPerTile));

    private System.Collections.Generic.IEnumerable<Parcel> SelectedParcels(
        (Tiles East, Tiles North) at, bool clipToPointer = true)
    {
        if (!clipToPointer)
        {
            var blocks = PreviewBounds(at);
            for (int row = blocks.South; row <= blocks.North; row++)
                for (int column = blocks.West; column <= blocks.East; column++)
                {
                    foreach (Parcel parcel in StandingParcels(column, row)) yield return parcel;
                }
            yield break;
        }

        var start = _zoneStart ?? at;
        int west = Math.Min(start.East.Raw, at.East.Raw), east = Math.Max(start.East.Raw, at.East.Raw);
        int south = Math.Min(start.North.Raw, at.North.Raw), north = Math.Max(start.North.Raw, at.North.Raw);
        var bounds = ZoneBounds(at);
        var streets = _world.Roads.Streets;
        Parcel[] buffer = [];
        for (int row = Math.Max(0, bounds.South); row <= Math.Min(streets.Blocks - 1, bounds.North); row++)
            for (int column = Math.Max(0, bounds.West); column <= Math.Min(streets.Blocks - 1, bounds.East); column++)
            {
                int capacity = LotSubdivider.PreviewCapacity(_world, column, row);
                if (buffer.Length < capacity) buffer = new Parcel[capacity];
                int count = LotSubdivider.Preview(_world, column, row, buffer);
                for (int i = 0; i < count; i++)
                {
                    var p = buffer[i];
                    if (!clipToPointer || (p.East.Raw <= east && p.East.Raw + p.Wide.Raw > west
                        && p.North.Raw <= north && p.North.Raw + p.Deep.Raw > south)) yield return p;
                }
            }
    }

    private (object? World, object? Roads, uint Version, object? Rules, ulong LotIds, int Lots) _zoneBlocksAt;
    private readonly System.Collections.Generic.Dictionary<(int Column, int Row), Parcel[]> _zoneBlockLots = [];

    /// <summary>The parcels of the Lots standing on a block, which the plot layer stops drawing while zoning.</summary>
    private Parcel[] StandingParcels(int column, int row)
    {
        var key = ((object?)_world, (object?)_world.Roads, _world.Roads.Version,
            (object?)_world.Rules, _world.Lots.Rows.NextId, _world.Lots.Rows.LiveCount);
        if (key != _zoneBlocksAt)
        {
            _zoneBlocksAt = key;
            _zoneBlockLots.Clear();
        }
        if (_zoneBlockLots.TryGetValue((column, row), out var standing)) return standing;

        // Preview lists the block's standing Lots first, then its proposed parcels.
        int capacity = LotSubdivider.PreviewCapacity(_world, column, row);
        Parcel[] parcels = new Parcel[capacity];
        LotSubdivider.Preview(_world, column, row, parcels);
        int realised = capacity - _world.Rules.Lots.ParcelCeiling(
            BlockGround.At(_world.Roads.Lattice, column, row));
        standing = parcels[..realised];
        _zoneBlockLots.Add((column, row), standing);
        return standing;
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
