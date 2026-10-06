using Q16 = Borough.Core.Arithmetic.Fixed;
using System;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

/// <summary>
/// The Street tool's freeform straight mode: two points, snapped in the shell, sent as one
/// <see cref="CommandKind.Street"/> after a preview the core computes.
/// </summary>
/// <remarks>
/// Snapping changes only the two ends the command carries. The Input Log records the snapped ends,
/// so a replay needs no snapping at all.
/// </remarks>
public partial class Main
{
    private (Tiles East, Tiles North)? _streetStart;
    private bool _streetSnaps = true;
    private (Tiles, Tiles, Tiles, Tiles, ulong, object?) _streetAsked;
    private StreetPreview _streetPreview;

    /// <summary>How far from the cursor a Node or a Segment captures a Street end, in Tiles.</summary>
    private const int StreetSnapTiles = 4;

    private enum SnapTarget : byte { None, Node, Segment }

    /// <summary>
    /// Where a Street end at <paramref name="at"/> lands: the nearest Node within reach, else the
    /// nearest point on a Segment rounded to its Tile, else the cursor's own Tile.
    /// </summary>
    /// <remarks>
    /// A Segment point closer to an end than <c>[roads] min_segment_length_tiles</c> snaps to that
    /// end's Node instead. Ties go to the lower slot, so the same city and cursor always snap the
    /// same way.
    /// </remarks>
    private (Tiles East, Tiles North, SnapTarget Target) Snap((Tiles East, Tiles North) at)
    {
        if (!_streetSnaps)
        {
            return (at.East, at.North, SnapTarget.None);
        }

        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;
        long reach = (long)StreetSnapTiles * StreetSnapTiles;
        int node = Rows.NoSlot;

        foreach (int segment in _world.Roads.Residency.Near(at.East, at.North, new Tiles(StreetSnapTiles)))
        {
            if (!segments.Rows.IsLive(segment)) continue;

            foreach (var end in (ReadOnlySpan<Handle<RoadNode>>)[segments.NodeA[segment], segments.NodeB[segment]])
            {
                if (!nodes.Rows.TryResolve(end, out int slot)) continue;
                long east = nodes.East[slot].Raw - (long)at.East.Raw, north = nodes.North[slot].Raw - (long)at.North.Raw;
                long distance = (east * east) + (north * north);
                if (distance < reach || (distance == reach && (node == Rows.NoSlot || slot < node)))
                {
                    (reach, node) = (distance, slot);
                }
            }
        }

        if (node != Rows.NoSlot)
        {
            return (nodes.East[node], nodes.North[node], SnapTarget.Node);
        }

        long cursorEast = (long)at.East.Raw * Q16.One, cursorNorth = (long)at.North.Raw * Q16.One;
        int nearest = Rows.NoSlot, closest = StreetSnapTiles * Q16.One;

        foreach (int segment in _world.Roads.Residency.Near(at.East, at.North, new Tiles(StreetSnapTiles)))
        {
            if (!segments.Rows.IsLive(segment)) continue;
            int distance = segments.Centerline[segment].DistanceTo(cursorEast, cursorNorth);
            if (distance < closest || (distance == closest && (nearest == Rows.NoSlot || segment < nearest)))
            {
                (closest, nearest) = (distance, segment);
            }
        }

        if (nearest == Rows.NoSlot)
        {
            return (at.East, at.North, SnapTarget.None);
        }

        StreetArc line = segments.Centerline[nearest];
        int along = line.OffsetAlong(cursorEast, cursorNorth);
        int shortest = _world.Rules.Roads.MinSegmentLengthTiles * Q16.One;

        // A split this close to an end would leave a piece the lay is refused for, so the end's
        // Node is the point the player meant.
        if (along < shortest || line.Length - along < shortest)
        {
            Handle<RoadNode> end = along < line.Length / 2 ? segments.NodeA[nearest] : segments.NodeB[nearest];
            if (nodes.Rows.TryResolve(end, out int slot))
            {
                return (nodes.East[slot], nodes.North[slot], SnapTarget.Node);
            }
        }

        var point = line.PointAt(along);

        return (new Tiles((int)Math.Round(point.East / (double)Q16.One)),
            new Tiles((int)Math.Round(point.North / (double)Q16.One)), SnapTarget.Segment);
    }

    private static Command StreetCommand((Tiles East, Tiles North) from, (Tiles East, Tiles North) to) =>
        Command.Street(from.East, from.North, to.East, to.North, SubTiles.Zero);

    /// <summary>The first point sets the start; the next lays the Street, and a refused lay keeps the start.</summary>
    private void StreetPoint((Tiles East, Tiles North) at)
    {
        var end = Snap(at);

        if (_streetStart is not { } start)
        {
            _streetStart = (end.East, end.North);

            return;
        }

        if (Send(StreetCommand(start, (end.East, end.North))))
        {
            _streetStart = null;
        }
    }

    /// <summary>What a straight Street command would do now, asked of the core once per pair of ends and Tick.</summary>
    private StreetPreview Previewed(Command street)
    {
        var asked = (street.East, street.North, street.EndEast, street.EndNorth, _world.Tick.Raw, (object?)_simulation);
        if (!_streetAsked.Equals(asked))
        {
            _streetAsked = asked;
            _streetPreview = _simulation.PreviewStreet(street);
        }

        return _streetPreview;
    }

    /// <summary>The Street tool's <c>ui</c> words: move the driven cursor, cancel a start, switch snapping.</summary>
    private bool StreetAction(string[] words)
    {
        switch (words)
        {
            case ["street-cancel"]:
                _streetStart = null;
                _refused = string.Empty;
                return true;

            case ["street-snap", "on" or "off"]:
                _streetSnaps = words[1] == "on";
                return true;

            case ["street-point", var east, var north]
                when int.TryParse(east, out int e) && int.TryParse(north, out int n):
                _aimed = (new Tiles(e), new Tiles(n));
                return true;

            default:
                return false;
        }
    }

    /// <summary>The hover line for the Street tool: what the next click does and what it would cost.</summary>
    private string StreetSynopsis((Tiles East, Tiles North) at)
    {
        var end = Snap(at);
        string snapped = end.Target switch
        {
            SnapTarget.Node => " · snapped to a junction",
            SnapTarget.Segment => " · snapped to a Street",
            _ => string.Empty,
        };
        string snapping = _streetSnaps ? "N turns snapping off" : "snapping off, N turns it on";

        if (_streetStart is not { } start)
        {
            return $"Street · click the start ({end.East.Raw:N0}, {end.North.Raw:N0}){snapped} · "
                + $"Shift-click removes a grid Street · {snapping}";
        }

        Command street = StreetCommand(start, (end.East, end.North));
        StreetPreview preview = Previewed(street);
        string ends = $"Street ({start.East.Raw:N0}, {start.North.Raw:N0}) to ({end.East.Raw:N0}, {end.North.Raw:N0}){snapped}";

        if (preview.Refusal is not (Refusal.None or Refusal.StreetTreasuryCannotPay))
        {
            return $"{ends} · {Sentence(preview.Refusal, street)} · Escape cancels";
        }

        string clears = preview.Buildings.Count == 0
            ? "clears nothing"
            : $"clears {preview.Buildings.Count:N0} {(preview.Buildings.Count == 1 ? "Building" : "Buildings")} for {preview.Price.Raw:N0}";

        return preview.Refusal == Refusal.None
            ? $"{ends} · {clears} · click to lay · Escape cancels"
            : $"{ends} · {clears} · {Sentence(preview.Refusal, street)} · Escape cancels";
    }

    /// <summary>
    /// Draws the snapped end, and once a start is set, the line it would lay and the Buildings it would clear.
    /// </summary>
    private void StreetCursor((Tiles East, Tiles North) at)
    {
        var end = Snap(at);
        Vector3 point = TileGround(end.East, end.North);
        int count = 0;

        _cursor.Multimesh.SetInstanceTransform(count, Box(point - new Vector3(1.5f, 0f, 0f),
            point + new Vector3(1.5f, 0f, 0f), 3f, 0f, 0.4f));
        _cursor.Multimesh.SetInstanceColor(count++, (end.Target == SnapTarget.None
            ? new Color(.95f, .95f, .95f) : new Color(.35f, .75f, .95f)).SrgbToLinear());

        if (_streetStart is { } start && (start.East, start.North) != (end.East, end.North))
        {
            StreetPreview preview = Previewed(StreetCommand(start, (end.East, end.North)));
            Color line = preview.Refusal == Refusal.None ? new Color(.95f, .80f, .25f) : new Color(.90f, .30f, .25f);
            Transform3D laid = Box(TileGround(start.East, start.North), point, CarriagewayWidthMetres, 0f, 0.3f);
            laid.Origin += new Vector3(0f, 0.1f, 0f);
            _cursor.Multimesh.SetInstanceTransform(count, laid);
            _cursor.Multimesh.SetInstanceColor(count++, line.SrgbToLinear());

            foreach (int building in preview.Buildings)
            {
                if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[building], out int lot)) continue;
                _cursor.Multimesh.SetInstanceTransform(count, Plate(_world.Lots.Parcel(lot), 0f, 0.06f, 0.02f));
                _cursor.Multimesh.SetInstanceColor(count++, new Color(.85f, .20f, .20f).SrgbToLinear());
                _cursor.Multimesh.SetInstanceTransform(count, Plate(_world.Lots.Footprint(lot), 0.4f, 1f, 2f));
                _cursor.Multimesh.SetInstanceColor(count++, new Color(.85f, .20f, .20f).SrgbToLinear());
            }
        }

        _cursor.Multimesh.VisibleInstanceCount = count;
    }

    private static Vector3 TileGround(Tiles east, Tiles north) =>
        new(east.Raw * MetresPerTile, 0f, -north.Raw * MetresPerTile);

    /// <summary>A box over an oriented rectangle, grown by <paramref name="margin"/> metres each side.</summary>
    private static Transform3D Plate(OrientedRectangle rectangle, float margin, float up, float tall) =>
        new(RectangleBasis(rectangle) * Basis.FromScale(new Vector3(
                (rectangle.Wide * MetresPerTile) + (2f * margin), tall, (rectangle.Deep * MetresPerTile) + (2f * margin))),
            RectanglePoint(rectangle, rectangle.Wide * 0.5f, up, rectangle.Deep * 0.5f));
}
