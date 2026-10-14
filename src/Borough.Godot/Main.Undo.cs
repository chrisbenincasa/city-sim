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
/// A Command a gesture queues applies at the top of the Tick the batch holding it runs, so what a
/// gesture did is not known when it is made. Recording is in three steps: the gesture notes what
/// stands near it before, <see cref="SubmitGestures"/> marks the notes whose Commands went into a
/// batch, and <see cref="SettleGestures"/> reads what changed once that batch has stepped.
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

    /// <summary>The widest ground a stroke's shape is remembered over, in Tiles of its bounds.</summary>
    private const int MostGroundTiles = 1 << 18;

    /// <summary>A Segment a gesture laid, by monotonic id and the length it was laid at.</summary>
    private readonly record struct LaidStreet(ulong Id, int LengthTiles);

    /// <summary>Ground a zone stroke painted: the shape it covered and the permissions either side.</summary>
    /// <param name="Set">The Tiles of the ground, a bit each over <paramref name="Bounds"/>.</param>
    /// <param name="Before">The uses every Tile of the ground held; 0 is unzoned.</param>
    /// <param name="After">The uses the stroke painted, which the ground must still hold to undo.</param>
    /// <param name="Mixed">Whether the ground held more than one permission, which no repaint restores.</param>
    private readonly record struct PaintedGround(
        CommandKind Kind, Tiles East, Tiles North, LandRectangle Bounds, ulong[] Set,
        ushort Before, ushort After, bool Mixed);

    /// <summary>A paint queued in the batch being collected, in the order the Commands apply.</summary>
    private readonly record struct QueuedPaint(LandRectangle Bounds, ulong[] Set, ushort Uses);

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

        /// <summary>
        /// Whether the ground the stroke painted is remembered only by its bounds, either because
        /// it is wider than <see cref="MostGroundTiles"/> or because such a stroke of the same
        /// batch paints under it. The entry is kept and refused, so the order of the stack still
        /// matches the order of the gestures.
        /// </summary>
        public bool Unknown { get; set; }

        /// <summary>Whether laying cleared Buildings.</summary>
        public bool Cleared { get; init; }

        /// <summary>
        /// Why this act cannot be reversed at all, for a demolition or a placement; empty for a
        /// gesture the undo may reverse. The entry stands in the way so that undoing a gesture
        /// older than the act is impossible while the act stands.
        /// </summary>
        public string Barrier { get; init; } = string.Empty;

        /// <summary>The highest Building id standing when the gesture was made.</summary>
        public ulong BuiltBefore { get; init; }
    }

    /// <summary>A gesture whose Commands are queued, waiting for the Tick that applies them.</summary>
    private sealed class Settling
    {
        public required Gesture Entry { get; init; }

        public required object World { get; init; }

        /// <summary>Whether the gesture's Commands have gone into a batch; only then can it settle.</summary>
        public bool Submitted { get; set; }

        /// <summary>The Street Commands the gesture sent.</summary>
        public Command[] Streets { get; init; } = [];

        /// <summary>The Segments standing in the gesture's reach, as they stood before it.</summary>
        public List<(int Slot, ulong Id, int LengthTiles, Handle<RoadNode> End)> Near { get; } = [];

        /// <summary>The Tile box the gesture reaches, for finding what it laid.</summary>
        public (int West, int South, int Wide, int Deep) Box { get; init; }

        /// <summary>The highest Segment id standing when the gesture was made.</summary>
        public required ulong StreetsBefore { get; init; }

        /// <summary>The highest Lot id standing when the gesture was made.</summary>
        public required ulong LotsBefore { get; init; }
    }

    private readonly List<Gesture> _gestures = [];
    private readonly List<Settling> _settling = [];
    private readonly List<QueuedPaint> _queuedPaint = [];
    private readonly List<QueuedPaint> _overlapping = [];
    private bool _vaguePaint;
    private bool _undoing;
    private bool _undoWanted;
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

    /// <summary>
    /// Notes an act no Command reverses -- a demolition, a Street removal, a service or a gate --
    /// as an entry the undo refuses.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="Send"/>, which every Command goes through, so no path misses one. A
    /// run of sends of the same kind is one barrier, which is what a Shift-drag over several
    /// Streets is. The undo's own Commands are exempt, because reversing a gesture is not an act to
    /// stand in the way of the gesture beneath it.
    /// </remarks>
    private void NoteBarrier(Command command)
    {
        if (_undoing) { return; }

        string barrier = command.Kind switch
        {
            CommandKind.Demolish => "that act cleared what stood there, and no Command puts it back.",
            CommandKind.Service or CommandKind.Gate =>
                "that act placed a Building, and its price has already left the city, so demolish "
                    + "corrects it instead.",
            _ => string.Empty,
        };

        if (barrier.Length == 0) { return; }

        Forget();
        PushZoneStroke();

        if (_settling.Count > 0 && _settling[^1].Entry.Barrier == barrier
            && !_settling[^1].Submitted)
        {
            return;
        }

        _settling.Add(new Settling
        {
            Entry = new Gesture { Barrier = barrier },
            World = _world,
            StreetsBefore = Highest(_world.Roads.Segments.Rows),
            LotsBefore = Highest(_world.Lots.Rows),
        });
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
            StreetsBefore = Highest(_world.Roads.Segments.Rows),
        };

        RoadSegmentTable segments = _world.Roads.Segments;
        foreach (int slot in SegmentsIn(settling.Box))
        {
            settling.Near.Add((slot, segments.Rows.IdAt(slot), segments.LengthTiles[slot].Raw, segments.NodeB[slot]));
        }

        _settling.Add(settling);
    }

    /// <summary>Notes one ground a zone Command paints, with the permission it replaces.</summary>
    /// <remarks>
    /// The Command has been accepted, so the paint will land. Ground the shell cannot remember
    /// exactly still leaves an entry, marked unknown, because an entry the undo refuses is the only
    /// thing that keeps it from reaching past a stroke that stands.
    /// </remarks>
    private void NoteZonePainted(Command command)
    {
        Forget();
        _painting ??= new Settling
        {
            Entry = new Gesture { BuiltBefore = Highest(_world.Buildings.Rows) },
            World = _world,
            LotsBefore = Highest(_world.Lots.Rows),
            StreetsBefore = Highest(_world.Roads.Segments.Rows),
        };

        if (!GroundAt(command.Kind, command.East, command.North,
                out LandRectangle bounds, out ulong[] set))
        {
            _painting.Entry.Unknown = true;
            _vaguePaint = true;

            return;
        }

        Underneath(bounds, set, out ushort before, out bool mixed, out bool vague);
        _queuedPaint.Add(new QueuedPaint(bounds, set, command.Zone));
        _painting.Entry.Unknown |= vague || set.Length == 0;
        _painting.Entry.Grounds.Add(new PaintedGround(
            command.Kind, command.East, command.North, bounds, set, before, command.Zone, mixed));
    }

    /// <summary>Closes the zone stroke being noted. One stroke is one entry however many grounds it painted.</summary>
    private void PushZoneStroke()
    {
        if (_painting is null) { return; }

        _settling.Add(_painting);
        _painting = null;
    }

    /// <summary>
    /// Marks every noted gesture as sent, which is what lets it settle.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="Ordered"/> as the batch is collected, because that is the moment the
    /// gesture's Commands leave the shell. A frame that collects no batch -- a shot, an open menu,
    /// a frame whose next drive entry is due at the Tick already presented -- leaves the queue
    /// standing, and a note settled against a city its Commands had not reached yet would find
    /// nothing and drop the entry.
    /// </remarks>
    private void SubmitGestures()
    {
        PushZoneStroke();
        _queuedPaint.Clear();
        _vaguePaint = false;

        foreach (Settling pending in _settling)
        {
            pending.Submitted = true;
        }
    }

    /// <summary>
    /// Attributes what the applied Commands laid, cut and created to the submitted gestures that
    /// sent them, and pushes one entry for each.
    /// </summary>
    /// <remarks>
    /// Several gestures can share a Tick, so a laid Segment is attributed by lying along the arc one
    /// of them named. A created Lot nothing claims goes to every gesture of the Tick, which refuses
    /// more undos rather than running one over ground something was built on.
    /// </remarks>
    private void SettleGestures()
    {
        if (_settling.Count == 0) { return; }

        List<Settling> settling = [];

        foreach (Settling pending in _settling)
        {
            if (pending.Submitted && ReferenceEquals(pending.World, _world)) { settling.Add(pending); }
        }

        _settling.RemoveAll(pending => pending.Submitted || !ReferenceEquals(pending.World, _world));
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

        // A new Segment no gesture claimed is a lay nothing could be attributed to. The gesture
        // applied all the same, so it keeps an entry the undo refuses rather than one the undo
        // reaches past.
        foreach (Settling pending in settling)
        {
            if (!pending.Entry.Laying || pending.Entry.Streets.Count > 0) { continue; }

            pending.Entry.Unknown = Unclaimed(settling, pending.StreetsBefore);
        }

        SettleBatchSplits(settling);
        SettleLots(settling);
        foreach (Settling pending in settling)
        {
            Push(pending.Entry);
        }

        // Last, because the undo reads the stack these pushes just finished.
        if (_undoWanted && !Unsettled())
        {
            _undoWanted = false;
            Undo();
        }
    }

    /// <summary>
    /// Marks every pair of Street gestures of one batch whose Streets meet, both ways.
    /// </summary>
    /// <remarks>
    /// A gesture's Near snapshot is taken before any Command of its batch applies, so it cannot see
    /// what a batch-mate is about to lay and the split test alone would miss the cut. Two gestures
    /// that meet are therefore both refused: whichever is undone first, the Street it crossed would
    /// stay in halves, which is the refusal #157 asks for.
    /// </remarks>
    private void SettleBatchSplits(List<Settling> settling)
    {
        for (int one = 0; one < settling.Count; one++)
        {
            if (!settling[one].Entry.Laying) { continue; }

            for (int other = one + 1; other < settling.Count; other++)
            {
                if (!settling[other].Entry.Laying || !Meet(settling[one], settling[other]))
                {
                    continue;
                }

                settling[one].Entry.Split = true;
                settling[other].Entry.Split = true;
            }
        }
    }

    /// <summary>Whether a Street either gesture laid ends on the other gesture's arc.</summary>
    /// <remarks>A crossing or a tee puts a junction on both arcs, and a Segment ends at a junction,
    /// so the end points answer for the meeting.</remarks>
    private bool Meet(Settling one, Settling other)
    {
        RoadSegmentTable segments = _world.Roads.Segments;

        foreach (LaidStreet street in one.Entry.Streets)
        {
            int slot = SegmentOf(street.Id);

            if (slot != Rows.NoSlot && Ends(other.Streets, segments.Centerline[slot])) { return true; }
        }

        foreach (LaidStreet street in other.Entry.Streets)
        {
            int slot = SegmentOf(street.Id);

            if (slot != Rows.NoSlot && Ends(one.Streets, segments.Centerline[slot])) { return true; }
        }

        return false;
    }

    /// <summary>Whether either end of a Segment lies on one of the arcs a gesture named.</summary>
    private static bool Ends(Command[] streets, StreetArc line)
    {
        foreach (Command street in streets)
        {
            if (!StreetArc.TryCreate(street.East.Raw, street.North.Raw, street.EndEast.Raw,
                    street.EndNorth.Raw, street.Sagitta.Raw, out StreetArc arc))
            {
                continue;
            }

            if (Along(arc, line.A) || Along(arc, line.B)) { return true; }
        }

        return false;
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

    /// <summary>Whether a Segment laid in this batch belongs to no gesture of it.</summary>
    private bool Unclaimed(List<Settling> settling, ulong before)
    {
        Rows<RoadSegment> rows = _world.Roads.Segments.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!rows.IsLive(slot) || rows.IdAt(slot) <= before) { continue; }

            bool claimed = false;

            foreach (Settling pending in settling)
            {
                if (Laid(pending.Entry, rows.IdAt(slot))) { claimed = true; break; }
            }

            if (!claimed) { return true; }
        }

        return false;
    }

    private void Push(Gesture entry)
    {
        if (!entry.Unknown && entry.Barrier.Length == 0
            && (entry.Laying ? entry.Streets.Count == 0 : entry.Grounds.Count == 0))
        {
            return;
        }

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
        _queuedPaint.Clear();
        _vaguePaint = false;
        _undoWanted = false;
        _painting = null;
    }

    /// <summary>Whether a gesture is queued, held over a batch or waiting to settle.</summary>
    private bool Unsettled() => _settling.Count > 0 || _painting is not null;

    // ---- undoing ----------------------------------------------------------------------------------

    /// <summary>
    /// Reverses the last gesture with the opposite Commands, or says why the reversal would not be
    /// exact and keeps it.
    /// </summary>
    /// <remarks>
    /// A gesture whose Commands have not applied yet is not on the stack, and the newest entry
    /// there is then older than the player's last act. So an undo asked for while anything is
    /// unsettled waits for the settlement instead of reversing the gesture underneath it.
    /// </remarks>
    private void Undo()
    {
        Forget();
        _refused = string.Empty;
        _undoSaid = string.Empty;

        if (Unsettled())
        {
            _undoWanted = true;
            _undoSaid = "waiting for the last gesture to land";

            return;
        }

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

        bool sent = true;
        _undoing = true;

        foreach (Command command in opposite)
        {
            sent &= Send(command);
        }

        _undoing = false;

        // Kept, because a reversal that went out in part is not a reversal. Send has already said
        // why, and the entry stays so that the undo does not reach the gesture beneath it.
        if (!sent) { return; }

        // The repaint joins the batch, so a stroke noted after it reads the permission it restores
        // rather than the one standing in the city.
        foreach (PaintedGround ground in entry.Grounds)
        {
            _queuedPaint.Add(new QueuedPaint(ground.Bounds, ground.Set, ground.Before));
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
        if (entry.Barrier.Length > 0)
        {
            return entry.Barrier
                + " It is kept here so that undoing does not step over it to an earlier gesture.";
        }

        if (entry.Unknown)
        {
            return "that gesture covered more ground than the shell remembers Tile by Tile, so "
                + "reversing it would reach ground the gesture never touched. It is kept here so "
                + "that undoing does not step over it to an earlier gesture.";
        }

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

        // Asked of the city again and not trusted from the note: a Street laid or removed since
        // reshapes the faces either side of it, and the Tile the stroke painted through then names
        // different ground. Two patches of ground can share a bounding box, so the comparison is
        // Tile by Tile.
        foreach (PaintedGround ground in entry.Grounds)
        {
            if (!GroundAt(ground.Kind, ground.East, ground.North,
                    out LandRectangle bounds, out ulong[] set)
                || set.Length == 0 || bounds != ground.Bounds || !Same(set, ground.Set))
            {
                return "the ground that stroke painted is not the shape it was, so repainting it "
                    + "would cover ground the stroke never touched.";
            }

            Holding(bounds, set, out ushort uses, out bool since);

            if (since || uses != ground.After)
            {
                return "the ground that stroke painted no longer holds what the stroke put there, "
                    + "so repainting it would take back something else as well.";
            }
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

    /// <summary>The ground a zone Command paints: its bounds, and the Tiles of it as a bitmap.</summary>
    /// <remarks>
    /// A parcel paints its whole bounds, as <c>LotSubdivider.PaintParcelAt</c> does. Other ground
    /// is one face or one Street side, which the bounds only enclose -- and two different patches
    /// of ground can share a bounding box, so the Tiles and not the box are what an exact undo
    /// compares. Ground wider than <see cref="MostGroundTiles"/> returns its bounds and an empty
    /// bitmap, which reads as the whole box at an unknown shape rather than as a bitmap of
    /// unbounded size; the gesture keeps an entry and the undo refuses it.
    /// </remarks>
    private bool GroundAt(CommandKind kind, Tiles east, Tiles north,
        out LandRectangle bounds, out ulong[] set)
    {
        bounds = default;
        set = [];
        ZoneGround? shape = null;

        if (kind == CommandKind.ZoneParcel)
        {
            if (!LotSubdivider.ParcelAt(_world, east, north, out Parcel parcel)) { return false; }

            bounds = new LandRectangle(parcel.East.Raw, parcel.North.Raw, parcel.Wide.Raw, parcel.Deep.Raw);
        }
        else
        {
            if (!_world.TryZoneGround(east, north, out ZoneGround ground)) { return false; }

            bounds = ground.Bounds;
            shape = ground;
        }

        if (!bounds.IsValid) { return false; }

        if ((long)bounds.Width * bounds.Height > MostGroundTiles) { return true; }

        set = new ulong[((bounds.Width * bounds.Height) + 63) / 64];
        int bit = 0, held = 0;

        for (int up = bounds.Y; up < bounds.Y + bounds.Height; up++)
        {
            for (int along = bounds.X; along < bounds.X + bounds.Width; along++, bit++)
            {
                if (shape is { } ground && !ground.Contains(along, up)) { continue; }

                set[bit >> 6] |= 1UL << (bit & 63);
                held++;
            }
        }

        return held > 0;
    }

    /// <summary>The uses every Tile of a ground holds now, and whether they differ across it.</summary>
    private void Holding(LandRectangle bounds, ulong[] set, out ushort uses, out bool mixed)
    {
        uses = 0;
        mixed = false;
        bool seen = false;
        int bit = 0;

        for (int up = bounds.Y; up < bounds.Y + bounds.Height; up++)
        {
            for (int along = bounds.X; along < bounds.X + bounds.Width; along++, bit++)
            {
                if ((set[bit >> 6] & (1UL << (bit & 63))) == 0) { continue; }

                ushort here = _world.LandPermissions.At(along, up).Uses;

                if (!seen) { (uses, seen) = (here, true); }
                else if (here != uses) { mixed = true; return; }
            }
        }
    }

    /// <summary>
    /// The uses a ground will hold when the Command being noted lands, and whether they will differ
    /// across it.
    /// </summary>
    /// <remarks>
    /// Nothing in the batch has applied yet, so a Tile an earlier Command of the batch paints will
    /// hold that Command's permission and not the one standing in the city. A stroke over ground
    /// another stroke of the same moment covered exactly is therefore still undone exactly; one
    /// that covers part of another's leaves a mixture, which the mixed-paint refusal catches.
    /// </remarks>
    private void Underneath(LandRectangle bounds, ulong[] set,
        out ushort before, out bool mixed, out bool vague)
    {
        before = 0;
        mixed = false;
        vague = _vaguePaint || set.Length == 0;
        bool seen = false;
        int bit = 0;

        if (vague) { return; }

        Overlapping(bounds);

        for (int up = bounds.Y; up < bounds.Y + bounds.Height; up++)
        {
            for (int along = bounds.X; along < bounds.X + bounds.Width; along++, bit++)
            {
                if ((set[bit >> 6] & (1UL << (bit & 63))) == 0) { continue; }

                ushort? queued = _overlapping.Count == 0 ? null : Queued(along, up, ref vague);
                ushort here = queued ?? _world.LandPermissions.At(along, up).Uses;

                if (vague) { return; }

                if (!seen) { (before, seen) = (here, true); }
                else if (here != before) { mixed = true; return; }
            }
        }
    }

    /// <summary>
    /// Collects the queued paint of the batch whose bounds reach this ground, so the Tile walk
    /// consults a short list rather than every Command of the batch.
    /// </summary>
    /// <remarks>
    /// The walk would otherwise cost one test per Tile per earlier Command, and a whole-block
    /// stroke over hundreds of blocks is one batch. Those grounds are disjoint, so the list is
    /// usually empty and the walk reads the city alone.
    /// </remarks>
    private void Overlapping(LandRectangle bounds)
    {
        _overlapping.Clear();

        foreach (QueuedPaint paint in _queuedPaint)
        {
            LandRectangle box = paint.Bounds;

            if (box.X < bounds.X + bounds.Width && bounds.X < box.X + box.Width
                && box.Y < bounds.Y + bounds.Height && bounds.Y < box.Y + box.Height)
            {
                _overlapping.Add(paint);
            }
        }
    }

    /// <summary>The uses a Tile takes from the batch being collected, or null when none paints it.</summary>
    /// <remarks>The last paint over a Tile is the one it holds, and a paint whose shape is unknown
    /// leaves the Tile unknown.</remarks>
    private ushort? Queued(int east, int north, ref bool vague)
    {
        ushort? uses = null;

        foreach (QueuedPaint paint in _overlapping)
        {
            if (!Covers(paint.Bounds, paint.Set, east, north)) { continue; }

            uses = paint.Uses;
            vague = paint.Set.Length == 0;
        }

        return uses;
    }

    /// <summary>Whether a Tile lies on ground the gesture painted.</summary>
    /// <remarks>The shape the stroke covered and not whatever the Tile names now: a Street laid or
    /// removed since would answer for different ground.</remarks>
    private static bool Painted(Gesture entry, Tiles east, Tiles north)
    {
        foreach (PaintedGround ground in entry.Grounds)
        {
            if (Covers(ground.Bounds, ground.Set, east.Raw, north.Raw)) { return true; }
        }

        return false;
    }

    /// <summary>Whether a Tile is one of a ground's own, by its bit over the ground's bounds.</summary>
    /// <remarks>An empty bitmap is ground known only by its box, so the whole box counts.</remarks>
    private static bool Covers(LandRectangle bounds, ulong[] set, int east, int north)
    {
        if (east < bounds.X || east >= bounds.X + bounds.Width
            || north < bounds.Y || north >= bounds.Y + bounds.Height)
        {
            return false;
        }

        if (set.Length == 0) { return true; }

        int bit = ((north - bounds.Y) * bounds.Width) + (east - bounds.X);

        return (set[bit >> 6] & (1UL << (bit & 63))) != 0;
    }

    private static bool Same(ulong[] set, ulong[] other)
    {
        if (set.Length != other.Length) { return false; }

        for (int at = 0; at < set.Length; at++)
        {
            if (set[at] != other[at]) { return false; }
        }

        return true;
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
