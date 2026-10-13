using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Q16 = Borough.Core.Arithmetic.Fixed;

namespace Borough.Shell;

/// <summary>
/// Undo for the last construction gestures: Street gestures and zone strokes, reversed with the
/// opposite Commands through the ordinary path.
/// </summary>
/// <remarks>
/// <para>
/// A Command a gesture queues applies at the top of the next Tick, so what a gesture did is not
/// known when it is made. Recording is in two halves: the gesture notes what stands near it
/// before, and <see cref="SettleGestures"/> reads what changed once the step has run.
/// </para>
/// <para>
/// An undo is offered only when the reversal is exact, and the refusals are the shell's own
/// sentences. A refused entry is kept, so the next gesture stacks on top of it.
/// </para>
/// </remarks>
public partial class Main
{
    /// <summary>How many gestures the shell remembers. Provisional, plans/construction-tools.</summary>
    private const int UndoDepth = 16;

    /// <summary>A Segment a gesture laid, by monotonic id and the length it was laid at.</summary>
    private readonly record struct LaidStreet(ulong Id, int LengthTiles);

    /// <summary>Ground a zone stroke painted, and the one permission it held before.</summary>
    /// <param name="Before">The uses every Tile of the ground held; 0 is unzoned.</param>
    /// <param name="Mixed">Whether the ground held more than one permission, which no repaint restores.</param>
    private readonly record struct PaintedGround(
        CommandKind Kind, Tiles East, Tiles North, LandRectangle Bounds, ushort Before, bool Mixed);

    /// <summary>One gesture, and everything reversing it exactly depends on.</summary>
    private sealed class Gesture
    {
        /// <summary>A Street gesture; a zone stroke otherwise.</summary>
        public bool Laying { get; init; }

        public List<LaidStreet> Streets { get; } = [];

        public List<PaintedGround> Grounds { get; } = [];

        /// <summary>The Lots the gesture created, by monotonic id.</summary>
        public List<ulong> Lots { get; } = [];

        /// <summary>Whether the gesture cut a Segment that already stood.</summary>
        public bool Split { get; set; }

        /// <summary>Whether laying cleared Buildings.</summary>
        public bool Cleared { get; init; }

        /// <summary>The highest Building id standing when the gesture was made.</summary>
        public ulong BuiltBefore { get; init; }
    }

    /// <summary>A gesture whose Commands are queued, waiting for the Tick that applies them.</summary>
    private sealed class Settling
    {
        public required Gesture Entry { get; init; }

        public required object World { get; init; }

        /// <summary>The Street Commands the gesture sent.</summary>
        public Command[] Streets { get; init; } = [];

        /// <summary>The Segments standing in the gesture's reach, as they stood before it.</summary>
        public List<(int Slot, ulong Id, int LengthTiles, Handle<RoadNode> End)> Near { get; } = [];

        /// <summary>The Tile box the gesture reaches, for finding what it laid.</summary>
        public (int West, int South, int Wide, int Deep) Box { get; init; }

        /// <summary>The highest Lot id standing when the gesture was made.</summary>
        public required ulong LotsBefore { get; init; }
    }

    private readonly List<Gesture> _gestures = [];
    private readonly List<Settling> _settling = [];
    private Settling? _painting;
    private object? _undoWorld;
    private string _undoSaid = string.Empty;

    /// <summary>The <c>ui undo</c> word, which Ctrl+Z sends too.</summary>
    private bool UndoAction(string[] words)
    {
        if (words is not ["undo"]) { return false; }
        Undo();

        return true;
    }

    // ---- recording --------------------------------------------------------------------------------

    /// <summary>
    /// Notes the Street gesture whose Commands were queued above <paramref name="queued"/>, which is
    /// nothing when the click only placed a point.
    /// </summary>
    private void NoteStreetGesture(int queued)
    {
        var streets = new List<Command>();
        for (int at = queued; at < _queued.Count; at++)
        {
            if (_queued[at].Kind == CommandKind.Street) { streets.Add(_queued[at]); }
        }

        if (streets.Count == 0) { return; }

        Forget();
        Command[] sent = [.. streets];
        var settling = new Settling
        {
            Entry = new Gesture
            {
                Laying = true,
                Cleared = Previewed(sent).Buildings.Count > 0,
                BuiltBefore = Highest(_world.Buildings.Rows),
            },
            World = _world,
            Streets = sent,
            Box = StreetBox(sent),
            LotsBefore = Highest(_world.Lots.Rows),
        };

        RoadSegmentTable segments = _world.Roads.Segments;
        foreach (int slot in SegmentsIn(settling.Box))
        {
            settling.Near.Add((slot, segments.Rows.IdAt(slot), segments.LengthTiles[slot].Raw, segments.NodeB[slot]));
        }

        _settling.Add(settling);
    }

    /// <summary>Notes one ground a zone stroke painted, with the permission it held before.</summary>
    /// <remarks>Called before the paint applies, so the reading is the ground as it stood.</remarks>
    private void NoteZonePainted(CommandKind kind, Tiles east, Tiles north)
    {
        if (!PaintedGroundAt(kind, east, north, out LandRectangle bounds, out ushort before, out bool mixed))
        {
            return;
        }

        Forget();
        _painting ??= new Settling
        {
            Entry = new Gesture { BuiltBefore = Highest(_world.Buildings.Rows) },
            World = _world,
            LotsBefore = Highest(_world.Lots.Rows),
        };
        _painting.Entry.Grounds.Add(new PaintedGround(kind, east, north, bounds, before, mixed));
    }

    /// <summary>Closes the zone stroke being noted. One stroke is one entry however many grounds it painted.</summary>
    private void PushZoneStroke()
    {
        if (_painting is null) { return; }

        _settling.Add(_painting);
        _painting = null;
    }

    /// <summary>
    /// Attributes what the applied Commands laid, cut and created to the gestures that sent them,
    /// and pushes one entry for each.
    /// </summary>
    /// <remarks>
    /// Several gestures can share a Tick, so a laid Segment is attributed by lying along the arc one
    /// of them named. A created Lot nothing claims goes to every gesture of the Tick, which refuses
    /// more undos rather than running one over ground something was built on.
    /// </remarks>
    private void SettleGestures()
    {
        if (_settling.Count == 0) { return; }

        List<Settling> settling = [.. _settling];
        _settling.Clear();
        settling.RemoveAll(pending => !ReferenceEquals(pending.World, _world));
        if (settling.Count == 0) { return; }

        RoadSegmentTable segments = _world.Roads.Segments;
        foreach (Settling pending in settling)
        {
            foreach ((int slot, ulong id, int length, Handle<RoadNode> end) in pending.Near)
            {
                if (!segments.Rows.IsLive(slot) || segments.Rows.IdAt(slot) != id
                    || segments.LengthTiles[slot].Raw != length || segments.NodeB[slot] != end)
                {
                    pending.Entry.Split = true;
                }
            }

            foreach (int slot in SegmentsIn(pending.Box))
            {
                ulong id = segments.Rows.IdAt(slot);
                if (Stood(pending, id) || !OnGesture(pending.Streets, slot)) { continue; }

                pending.Entry.Streets.Add(new LaidStreet(id, segments.LengthTiles[slot].Raw));
            }
        }

        SettleLots(settling);
        foreach (Settling pending in settling)
        {
            Push(pending.Entry);
        }
    }

    /// <summary>Gives each gesture the Lots it created.</summary>
    private void SettleLots(List<Settling> settling)
    {
        LotTable lots = _world.Lots;
        for (int lot = 0; lot < lots.Rows.SlotCount; lot++)
        {
            if (!lots.Rows.IsLive(lot)) { continue; }

            ulong id = lots.Rows.IdAt(lot);
            bool claimed = false;
            foreach (Settling pending in settling)
            {
                if (id <= pending.LotsBefore || !Claims(pending, lot)) { continue; }

                pending.Entry.Lots.Add(id);
                claimed = true;
            }

            if (claimed) { continue; }

            foreach (Settling pending in settling)
            {
                if (id > pending.LotsBefore) { pending.Entry.Lots.Add(id); }
            }
        }
    }

    /// <summary>Whether a Lot belongs to a gesture: fronting a Street it laid, or on ground it painted.</summary>
    private bool Claims(Settling pending, int lot)
    {
        if (pending.Entry.Laying)
        {
            int frontage = _world.Lots.FrontageOn(lot);

            return frontage != Rows.NoSlot
                && Laid(pending.Entry, _world.Roads.Segments.Rows.IdAt(frontage));
        }

        return Painted(pending.Entry, _world.Lots.East[lot], _world.Lots.North[lot]);
    }

    private void Push(Gesture entry)
    {
        if (entry.Laying ? entry.Streets.Count == 0 : entry.Grounds.Count == 0) { return; }

        _gestures.Add(entry);
        if (_gestures.Count > UndoDepth) { _gestures.RemoveAt(0); }

        _undoSaid = string.Empty;
    }

    /// <summary>Drops every entry when the world is replaced, since an id names a row in that world alone.</summary>
    private void Forget()
    {
        if (ReferenceEquals(_undoWorld, _world)) { return; }

        _undoWorld = _world;
        _gestures.Clear();
        _settling.Clear();
        _painting = null;
    }

    // ---- undoing ----------------------------------------------------------------------------------

    /// <summary>
    /// Reverses the last gesture with the opposite Commands, or says why the reversal would not be
    /// exact and keeps it.
    /// </summary>
    private void Undo()
    {
        Forget();
        _refused = string.Empty;
        _undoSaid = string.Empty;

        if (_gestures.Count == 0)
        {
            _refused = "there is no construction gesture to undo. A service or a gate is corrected "
                + "with demolish instead, because its price has already left the city.";

            return;
        }

        Gesture entry = _gestures[^1];

        if (Inexact(entry) is { } reason)
        {
            _refused = reason;

            return;
        }

        var opposite = new List<Command>();

        if (Opposites(entry, opposite) is { } refused)
        {
            _refused = refused;

            return;
        }

        foreach (Command command in opposite)
        {
            Send(command);
        }

        _gestures.RemoveAt(_gestures.Count - 1);
        _undoSaid = (entry.Laying
                ? $"removed {Count(entry.Streets.Count, "Street")}"
                : $"repainted {Count(entry.Grounds.Count, "patch")} of ground")
            + $"; {_gestures.Count:N0} gestures left to undo";
    }

    /// <summary>Why this gesture cannot be reversed exactly, or null when it can.</summary>
    private string? Inexact(Gesture entry)
    {
        if (entry.Split)
        {
            return "that gesture cut a Street that was already there in two, and removing a Street "
                + "does not join the halves back together.";
        }

        foreach (PaintedGround ground in entry.Grounds)
        {
            if (ground.Mixed)
            {
                return "that stroke painted over ground holding more than one permission, and one "
                    + "Zone command paints one permission over the whole of it.";
            }
        }

        if (entry.Cleared)
        {
            return "that gesture cleared Buildings, and no Command puts a Building back.";
        }

        if (RaisedSince(entry) is { } building)
        {
            return $"Building {building:N0} has been raised on the ground that gesture made, so "
                + "undoing it would take the ground out from under it.";
        }

        RoadSegmentTable segments = _world.Roads.Segments;

        foreach (LaidStreet street in entry.Streets)
        {
            int slot = SegmentOf(street.Id);

            if (slot == Rows.NoSlot)
            {
                return $"Street {street.Id:N0} is already gone, so this gesture is no longer the "
                    + "last thing done to the city.";
            }

            if (segments.LengthTiles[slot].Raw != street.LengthTiles)
            {
                return $"Street {street.Id:N0} has been cut since it was laid, and removing it "
                    + "would leave the half a later gesture made.";
            }
        }

        return null;
    }

    /// <summary>The first Building raised on the gesture's own ground since it was made.</summary>
    private ulong? RaisedSince(Gesture entry)
    {
        BuildingTable buildings = _world.Buildings;
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (!buildings.Rows.IsLive(slot) || buildings.Rows.IdAt(slot) <= entry.BuiltBefore
                || !lots.Rows.TryResolve(buildings.Lot[slot], out int lot))
            {
                continue;
            }

            if (entry.Lots.Contains(lots.Rows.IdAt(lot))
                || Painted(entry, lots.East[lot], lots.North[lot]))
            {
                return buildings.Rows.IdAt(slot);
            }
        }

        return null;
    }

    /// <summary>
    /// The Commands that reverse a gesture, or the sentence it is refused with.
    /// </summary>
    /// <remarks>
    /// A Segment is removed by the Tile that picks it and no other, so the removal names the Segment
    /// the gesture laid rather than whatever lies nearest a remembered point. Removing a Street only
    /// takes candidates away, so every Tile keeps picking its own Segment as the others go.
    /// </remarks>
    private string? Opposites(Gesture entry, List<Command> into)
    {
        foreach (LaidStreet street in entry.Streets)
        {
            if (!TileOn(SegmentOf(street.Id), out (Tiles East, Tiles North) tile))
            {
                return $"no Tile names Street {street.Id:N0} on its own, so removing it would have "
                    + "to name a different Street.";
            }

            into.Add(Command.Demolish(tile.East, tile.North, DemolishTarget.Street));
        }

        foreach (PaintedGround ground in entry.Grounds)
        {
            into.Add(new Command(ground.Kind, ground.East, ground.North, ground.Before));
        }

        foreach (Command command in into)
        {
            Refusal refusal = _simulation.Refuses(command);

            if (refusal != Refusal.None)
            {
                return Sentence(refusal, command);
            }
        }

        return null;
    }

    /// <summary>
    /// A Tile that <see cref="Borough.Core.Entities.World.StreetAt"/> resolves to this Segment and
    /// to no other, searched from the middle outwards.
    /// </summary>
    /// <remarks>The middle is the point furthest from the junctions at either end, where a side
    /// Street is often the nearer centerline.</remarks>
    private bool TileOn(int segment, out (Tiles East, Tiles North) tile)
    {
        tile = default;
        if (segment == Rows.NoSlot) { return false; }

        StreetArc line = _world.Roads.Segments.Centerline[segment];
        int middle = line.Length / 2;
        int steps = (2 * (line.Length / Q16.One)) + 2;

        for (int step = 0; step <= steps; step++)
        {
            int offset = middle + ((step % 2 == 0 ? step / 2 : -((step + 1) / 2)) * Q16.One);
            if (offset < 0 || offset > line.Length) { continue; }

            var (east, north) = line.PointAt(offset);
            int tileEast = (int)(east / Q16.One), tileNorth = (int)(north / Q16.One);
            if (tileEast < 0 || tileNorth < 0
                || tileEast >= CellGrid.WorldTiles || tileNorth >= CellGrid.WorldTiles)
            {
                continue;
            }

            if (_world.StreetAt(new Tiles(tileEast), new Tiles(tileNorth)) == segment)
            {
                tile = (new Tiles(tileEast), new Tiles(tileNorth));

                return true;
            }
        }

        return false;
    }

    // ---- reading the city -------------------------------------------------------------------------

    /// <summary>
    /// The ground a zone Command paints, the single permission it holds and whether it holds more
    /// than one.
    /// </summary>
    /// <remarks>
    /// A parcel paints its whole bounds, as <c>LotSubdivider.PaintParcelAt</c> does. Other ground
    /// paints one face or one Street side, so the walk tests each Tile of the bounds for membership.
    /// </remarks>
    private bool PaintedGroundAt(CommandKind kind, Tiles east, Tiles north,
        out LandRectangle bounds, out ushort before, out bool mixed)
    {
        bounds = default;
        before = 0;
        mixed = false;

        if (kind == CommandKind.ZoneParcel)
        {
            if (!LotSubdivider.ParcelAt(_world, east, north, out Parcel parcel)) { return false; }

            bounds = new LandRectangle(parcel.East.Raw, parcel.North.Raw, parcel.Wide.Raw, parcel.Deep.Raw);

            return Holds(bounds, null, out before, out mixed);
        }

        if (!_world.TryZoneGround(east, north, out ZoneGround ground)) { return false; }

        bounds = ground.Bounds;

        return Holds(bounds, ground, out before, out mixed);
    }

    /// <summary>The uses the first Tile of a region holds, and whether every other Tile matches it.</summary>
    private bool Holds(LandRectangle bounds, ZoneGround? ground, out ushort uses, out bool mixed)
    {
        uses = 0;
        mixed = false;
        if (!bounds.IsValid) { return false; }

        bool seen = false;

        for (int north = bounds.Y; north < bounds.Y + bounds.Height; north++)
        {
            for (int east = bounds.X; east < bounds.X + bounds.Width; east++)
            {
                if (ground is { } set && !set.Contains(east, north)) { continue; }

                ushort here = _world.LandPermissions.At(east, north).Uses;

                if (!seen)
                {
                    (uses, seen) = (here, true);
                }
                else if (here != uses)
                {
                    mixed = true;

                    return true;
                }
            }
        }

        return seen;
    }

    /// <summary>Whether a Tile lies on ground the gesture painted.</summary>
    /// <remarks>
    /// The bounds reject cheaply and are exact for a parcel. Other ground is asked of the city
    /// again, because a face or a Street side is only a shape while the Streets stand still.
    /// </remarks>
    private bool Painted(Gesture entry, Tiles east, Tiles north)
    {
        foreach (PaintedGround ground in entry.Grounds)
        {
            LandRectangle box = ground.Bounds;

            if (east.Raw < box.X || east.Raw >= box.X + box.Width
                || north.Raw < box.Y || north.Raw >= box.Y + box.Height)
            {
                continue;
            }

            if (ground.Kind == CommandKind.ZoneParcel
                || !_world.TryZoneGround(ground.East, ground.North, out ZoneGround set)
                || set.Contains(east.Raw, north.Raw))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The Tile box a gesture's Streets reach, padded for the bulge of a curve.</summary>
    private static (int West, int South, int Wide, int Deep) StreetBox(Command[] streets)
    {
        int west = int.MaxValue, south = int.MaxValue, east = int.MinValue, north = int.MinValue;
        int pad = 2;

        foreach (Command street in streets)
        {
            west = Lower(west, Lower(street.East.Raw, street.EndEast.Raw));
            south = Lower(south, Lower(street.North.Raw, street.EndNorth.Raw));
            east = Upper(east, Upper(street.East.Raw, street.EndEast.Raw));
            north = Upper(north, Upper(street.North.Raw, street.EndNorth.Raw));
            int sagitta = street.Sagitta.Raw < 0 ? -street.Sagitta.Raw : street.Sagitta.Raw;
            pad = Upper(pad, (sagitta / Q16.One) + 2);
        }

        west = Lower(Upper(0, west - pad), CellGrid.WorldTiles - 1);
        south = Lower(Upper(0, south - pad), CellGrid.WorldTiles - 1);
        east = Lower(east + pad, CellGrid.WorldTiles - 1);
        north = Lower(north + pad, CellGrid.WorldTiles - 1);

        return (west, south, Upper(1, east - west + 1), Upper(1, north - south + 1));
    }

    /// <summary>The live Segments whose Cells meet a Tile box.</summary>
    private List<int> SegmentsIn((int West, int South, int Wide, int Deep) box)
    {
        var found = new List<int>();

        foreach (int slot in _world.Roads.Residency.In(
            new Tiles(box.West), new Tiles(box.South), new Tiles(box.Wide), new Tiles(box.Deep)))
        {
            if (_world.Roads.Segments.Rows.IsLive(slot)) { found.Add(slot); }
        }

        found.Sort();

        return found;
    }

    /// <summary>Whether a Segment lies along one of the arcs a gesture's Commands named.</summary>
    private bool OnGesture(Command[] streets, int segment)
    {
        StreetArc line = _world.Roads.Segments.Centerline[segment];

        foreach (Command street in streets)
        {
            if (!StreetArc.TryCreate(street.East.Raw, street.North.Raw, street.EndEast.Raw,
                    street.EndNorth.Raw, street.Sagitta.Raw, out StreetArc arc))
            {
                continue;
            }

            if (Along(arc, line.A) && Along(arc, line.B) && Along(arc, line.PointAt(line.Length / 2)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a Q16.16 point lies within a Tile of a finite arc.</summary>
    /// <remarks>Far points are rejected on their coordinates, because the distance itself overflows
    /// Q16.16 past 16,384 Tiles.</remarks>
    private static bool Along(StreetArc arc, (long East, long North) point)
    {
        const long Reach = 1L << 30;
        long away = point.East - arc.A.East, up = point.North - arc.A.North;

        return away <= Reach && away >= -Reach && up <= Reach && up >= -Reach
            && arc.DistanceTo(point.East, point.North) <= Q16.One;
    }

    private static bool Stood(Settling pending, ulong id)
    {
        foreach (var near in pending.Near)
        {
            if (near.Id == id) { return true; }
        }

        return false;
    }

    private static bool Laid(Gesture entry, ulong id)
    {
        foreach (LaidStreet street in entry.Streets)
        {
            if (street.Id == id) { return true; }
        }

        return false;
    }

    /// <summary>The live slot holding a monotonic Segment id, or <see cref="Rows.NoSlot"/>.</summary>
    private int SegmentOf(ulong id)
    {
        Rows<RoadSegment> rows = _world.Roads.Segments.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot) && rows.IdAt(slot) == id) { return slot; }
        }

        return Rows.NoSlot;
    }

    /// <summary>The highest monotonic id of a live row. Anything created later carries a greater one.</summary>
    private static ulong Highest(Rows rows)
    {
        ulong highest = 0;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (rows.IsLive(slot) && rows.IdAt(slot) > highest) { highest = rows.IdAt(slot); }
        }

        return highest;
    }

    private static int Lower(int a, int b) => a < b ? a : b;

    private static int Upper(int a, int b) => a > b ? a : b;
}
