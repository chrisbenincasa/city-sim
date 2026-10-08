using Q16 = Borough.Core.Arithmetic.Fixed;
using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

/// <summary>
/// The Street tool: points snapped in the shell, sent as one <see cref="CommandKind.Street"/> per
/// Street after a preview the core computes.
/// </summary>
/// <remarks>
/// <para>
/// Snapping changes only the two ends the command carries. The Input Log records the snapped ends,
/// so a replay needs no snapping at all.
/// </para>
/// <para>
/// Grid and Parallel lay several Streets from one gesture as ordinary commands on the same Tick.
/// The gesture is sent only when every Street previews clean and the treasury covers the total, so
/// phase 0 refuses one of them only when the batch's own Streets change the answer.
/// </para>
/// </remarks>
public partial class Main
{
    private (Tiles East, Tiles North)? _streetStart;
    private (Tiles East, Tiles North)? _streetBend;
    private (Tiles East, Tiles North)? _streetGridEdge;
    private (int East, int North)? _streetTangent;
    private StreetMode _streetMode;
    private bool _streetSnaps = true;
    private bool _streetLengthSnaps = true;
    private bool _streetAngleSnaps = true;
    private bool _streetStraight;
    private bool _streetParallel;
    private bool _streetParallelRight;
    private int _streetAngleDegrees = 15;
    private int _streetReachTiles = 4;
    private int _streetGridTiles;
    private int _streetParallelTiles;
    private bool _streetPreferences;
    private (Command[] Streets, ulong Tick, object? Simulation) _streetAsked = ([], 0, null);
    private StreetBatchPreview _streetPreview;

    private static readonly int[] StreetAngleSteps = [90, 45, 15, 5];

    private const string StreetPreferencesPath = "user://street.cfg";

    [Flags]
    private enum SnapTarget : byte { None = 0, Node = 1, Segment = 2, Angle = 4, Length = 8 }

    /// <summary>A snapped Street end, with the angle step and plot count a guide snap landed on.</summary>
    private readonly record struct StreetEnd(
        Tiles East, Tiles North, SnapTarget Target, int Degrees = 0, int Plots = 0, bool FromStreet = false);

    /// <summary>The Street tool's choices, in the order <c>hold street</c> numbers them.</summary>
    private enum StreetMode : byte { Straight, Curve, Continuous, Grid }

    private static readonly string[] StreetModeNames = ["Straight", "Simple curve", "Continuous", "Grid"];

    /// <summary>
    /// What a gesture would lay: the Streets in order, the sentence the shell refuses it with when
    /// the core is never asked, and the core's combined answer otherwise.
    /// </summary>
    private readonly record struct StreetBatchPreview(
        Refusal Refusal, int Refused, Money Price, IReadOnlyList<int> Buildings);

    /// <summary>Whether held Ctrl makes a curve or continuous Street lay straight.</summary>
    private bool Straightened => _streetStraight && _streetMode is StreetMode.Curve or StreetMode.Continuous;

    /// <summary>
    /// Where a Street end at <paramref name="at"/> lands: on an existing Street within reach, else
    /// on the angle and length guides from the start, else on the cursor's own Tile.
    /// </summary>
    /// <remarks>
    /// A grid's far corner always takes the guides, so its edge is a whole number of blocks. Its
    /// depth point does not snap; the block count rounds it.
    /// </remarks>
    private StreetEnd Snap((Tiles East, Tiles North) at)
    {
        if (_streetMode == StreetMode.Grid && _streetStart is { } corner)
        {
            return _streetGridEdge is null ? Guided(corner, at) : new StreetEnd(at.East, at.North, SnapTarget.None);
        }

        var onStreet = SnapToStreets(at);
        bool bending = _streetMode == StreetMode.Curve && _streetBend is null && !Straightened;

        return onStreet.Target != SnapTarget.None || !_streetSnaps || bending || _streetStart is not { } start
            ? new StreetEnd(onStreet.East, onStreet.North, onStreet.Target)
            : Guided(start, at);
    }

    /// <summary>
    /// The nearest Node within reach, else the nearest point on a Segment rounded to its Tile, else
    /// the cursor's own Tile.
    /// </summary>
    /// <remarks>
    /// A Segment point closer to an end than <c>[roads] min_segment_length_tiles</c> snaps to that
    /// end's Node instead. Ties go to the lower slot, so the same city and cursor always snap the
    /// same way.
    /// </remarks>
    private (Tiles East, Tiles North, SnapTarget Target) SnapToStreets((Tiles East, Tiles North) at)
    {
        if (!_streetSnaps)
        {
            return (at.East, at.North, SnapTarget.None);
        }

        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;
        long reach = (long)_streetReachTiles * _streetReachTiles;
        int node = Rows.NoSlot;

        foreach (int segment in _world.Roads.Residency.Near(at.East, at.North, new Tiles(_streetReachTiles)))
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
        int nearest = Rows.NoSlot, closest = _streetReachTiles * Q16.One;

        foreach (int segment in _world.Roads.Residency.Near(at.East, at.North, new Tiles(_streetReachTiles)))
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

    /// <summary>
    /// Turns the start-to-cursor direction to the nearest angle step and stretches it to a whole
    /// number of plots, each within reach of the cursor. The end rounds to a Tile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Angles count from every Street through the start, or from east where none passes. Every step
    /// divides 90°, so square to the Street is always a choice. A continuous Street snaps how far it
    /// turns instead, from the tangent it leaves along.
    /// </para>
    /// <para>
    /// Each snap is measured from the start alone, so a chain of snapped Streets cannot drift. A
    /// continuous Street's arc length is its chord times θ / sin θ for the chord-tangent angle θ, which
    /// is fixed along one direction, so its plots are counted along the arc. A simple curve's length
    /// depends on its bend point and takes no length snap.
    /// </para>
    /// </remarks>
    private StreetEnd Guided((Tiles East, Tiles North) start, (Tiles East, Tiles North) at)
    {
        double east = at.East.Raw - start.East.Raw, north = at.North.Raw - start.North.Raw;
        double distance = Math.Sqrt((east * east) + (north * north));
        if (distance < 1) return new StreetEnd(at.East, at.North, SnapTarget.None);

        double heading = Math.Atan2(north, east), turn = 0;
        SnapTarget target = SnapTarget.None;
        int degrees = 0, plots = 0;
        bool fromStreet = false;
        (int East, int North)? tangent = _streetMode == StreetMode.Continuous && !Straightened ? _streetTangent : null;
        double leaving = tangent is { } t ? Math.Atan2(t.North, t.East) : 0;

        if (_streetAngleSnaps)
        {
            double step = _streetAngleDegrees * Math.PI / 180, best = double.MaxValue, snapped = heading;
            int stepped = int.MaxValue;
            double[] streets = tangent is null ? StreetDirections(start) : [];
            fromStreet = streets.Length > 0;

            foreach (double reference in tangent is not null ? [leaving] : fromStreet ? streets : [0.0])
            {
                double from = Math.IEEERemainder(heading - reference, 2 * Math.PI);
                int count = (int)Math.Round((tangent is null ? from : 2 * from) / step);
                double candidate = reference + (tangent is null ? count * step : count * step / 2);
                double off = Math.Abs(Math.IEEERemainder(heading - candidate, 2 * Math.PI));
                int angle = Math.Abs(count * _streetAngleDegrees) % 360;
                if (tangent is null) angle = Math.Min(angle % 180, 180 - (angle % 180));
                if (off < best - 1e-9 || (off < best + 1e-9 && angle < stepped)) (best, snapped, stepped) = (off, candidate, angle);
            }

            if (distance * Math.Sin(best) <= _streetReachTiles)
            {
                (heading, target, degrees) = (snapped, SnapTarget.Angle, stepped);
                distance *= Math.Cos(best);
            }
        }

        if (tangent is not null) turn = Math.IEEERemainder(heading - leaving, 2 * Math.PI);
        double arc = Math.Abs(turn) < 1e-9 ? 1 : turn / Math.Sin(turn);
        bool grid = _streetMode == StreetMode.Grid;
        double plot = grid ? GridTiles() : PlotTiles();

        if ((grid || (_streetLengthSnaps && (_streetMode != StreetMode.Curve || Straightened))) && plot > 0)
        {
            plots = Math.Max(1, (int)Math.Round(distance * arc / plot));
            (distance, target) = (plots * plot / arc, target | SnapTarget.Length);
        }

        if (target == SnapTarget.None) return new StreetEnd(at.East, at.North, SnapTarget.None);

        return new StreetEnd(new Tiles(start.East.Raw + (int)Math.Round(distance * Math.Cos(heading))),
            new Tiles(start.North.Raw + (int)Math.Round(distance * Math.Sin(heading))), target, degrees, plots, fromStreet);
    }

    /// <summary>The directions of the Streets through <paramref name="start"/>, in radians from east.</summary>
    private double[] StreetDirections((Tiles East, Tiles North) start)
    {
        RoadSegmentTable segments = _world.Roads.Segments;
        long east = (long)start.East.Raw * Q16.One, north = (long)start.North.Raw * Q16.One;
        var directions = new System.Collections.Generic.List<double>();

        foreach (int segment in _world.Roads.Residency.Near(start.East, start.North, new Tiles(1)))
        {
            if (!segments.Rows.IsLive(segment) || (RoadKind)segments.Kind[segment] == RoadKind.FootPath) continue;
            StreetArc line = segments.Centerline[segment];
            if (line.DistanceTo(east, north) > Q16.One) continue;
            var (tangentEast, tangentNorth) = line.TangentAt(line.OffsetAlong(east, north));
            directions.Add(Math.Atan2(tangentNorth, tangentEast));
        }

        return [.. directions];
    }

    /// <summary>
    /// One plot's width in Tiles: <c>[lots] residential_frontage_tiles</c> where the Ruleset sets it,
    /// else 2 × <c>block_tiles</c> / <c>lots_per_segment</c>.
    /// </summary>
    private double PlotTiles() =>
        _world.Rules.Lots.Plots.Runs ? _world.Rules.Lots.Plots.FrontageTiles
        : _world.Rules.Lots.LotsPerSegment > 0 ? 2.0 * _world.Roads.Streets.BlockTiles / _world.Rules.Lots.LotsPerSegment
        : 0;

    /// <summary>
    /// The Street from the start to <paramref name="to"/>: straight, through the bend point, or
    /// tangent to the Street laid before it.
    /// </summary>
    private Command StreetCommand((Tiles East, Tiles North) from, (Tiles East, Tiles North) to)
    {
        int sagitta = (_streetMode, _streetBend, _streetTangent) switch
        {
            _ when Straightened => 0,
            (StreetMode.Curve, { } bend, _) => SagittaThrough(from, bend, to),
            (StreetMode.Continuous, _, { } tangent) => SagittaFrom(tangent, from, to),
            _ => 0,
        };

        return Command.Street(from.East, from.North, to.East, to.North, new SubTiles(sagitta));
    }

    /// <summary>
    /// A grid block's side in Tiles: the <c>grid_block_tiles</c> preference, else <c>[roads]
    /// block_tiles</c>, and never shorter than <c>[roads] min_segment_length_tiles</c>.
    /// </summary>
    private int GridTiles() =>
        Math.Max(_streetGridTiles > 0 ? _streetGridTiles : _world.Roads.Streets.BlockTiles,
            Math.Max(1, _world.Rules.Roads.MinSegmentLengthTiles));

    /// <summary>
    /// How far a parallel Street stands from the drawn one, in Tiles: the <c>parallel_offset_tiles</c>
    /// preference, else <c>[roads] block_tiles</c>.
    /// </summary>
    /// <remarks>
    /// The floor is two Street half-widths plus <c>[lots] min_plot_depth_tiles</c>, so a plot fits
    /// between the two Streets.
    /// </remarks>
    private int ParallelTiles() =>
        Math.Max(_streetParallelTiles > 0 ? _streetParallelTiles : _world.Roads.Streets.BlockTiles,
            (2 * _world.Rules.Lots.StreetHalfWidthTiles) + Math.Max(1, _world.Rules.Lots.MinPlotDepthTiles));

    /// <summary>
    /// What one gesture lays, ending at <paramref name="end"/> with the cursor at
    /// <paramref name="at"/>, or the sentence the shell refuses it with.
    /// </summary>
    private (Command[] Streets, string? Refused) StreetBatch(
        (Tiles East, Tiles North) start, (Tiles East, Tiles North) end, (Tiles East, Tiles North) at)
    {
        if (_streetMode == StreetMode.Grid)
        {
            return _streetGridEdge is { } edge ? GridStreets(start, edge, at) : ([StreetCommand(start, end)], null);
        }

        Command drawn = StreetCommand(start, end);
        if (!_streetParallel) return ([drawn], null);

        return ParallelTo(drawn) is { } copy
            ? ([drawn, copy], null)
            : ([drawn], $"a parallel Street {ParallelTiles():N0} Tiles inside this curve would pass its center.");
    }

    /// <summary>
    /// The Street <see cref="ParallelTiles"/> to the left of <paramref name="drawn"/>, or to its right
    /// when the side is flipped. Null when the copy would reach the curve's center.
    /// </summary>
    /// <remarks>
    /// A curve's copy is concentric, so its sagitta scales with its radius. Its ends round to Tiles,
    /// which keeps it concentric to within a Tile.
    /// </remarks>
    private Command? ParallelTo(Command drawn)
    {
        if (!StreetArc.TryCreate(drawn.East.Raw, drawn.North.Raw, drawn.EndEast.Raw, drawn.EndNorth.Raw,
                drawn.Sagitta.Raw, out StreetArc arc))
        {
            return null;
        }

        int distance = ParallelTiles() * Q16.One * (_streetParallelRight ? -1 : 1);
        StreetArc copy;
        try
        {
            copy = arc.Parallel(distance);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        catch (OverflowException)
        {
            return null;
        }

        long sagitta = arc.IsStraight ? 0 : drawn.Sagitta.Raw * copy.Radius / arc.Radius;

        return Command.Street(TileOf(copy.A.East), TileOf(copy.A.North), TileOf(copy.B.East), TileOf(copy.B.North),
            new SubTiles((int)Math.Clamp(sagitta, -int.MaxValue, int.MaxValue)));
    }

    private static Tiles TileOf(long q16) => new((int)Math.Round(q16 / (double)Q16.One));

    /// <summary>
    /// A grid of whole blocks: <paramref name="edge"/> fixes the first side, and the cursor's distance
    /// from that side sets the rows and which side they fall on. One Street runs the length of each
    /// grid line, and the core splits them where they cross.
    /// </summary>
    /// <remarks>
    /// Every corner is measured from the start and rounds to its own Tile, so an off-axis grid
    /// stays within half a Tile of true and cannot drift. A line that runs along an existing Street
    /// is left out, since the core refuses that lay as too shallow.
    /// </remarks>
    private (Command[] Streets, string? Refused) GridStreets(
        (Tiles East, Tiles North) start, (Tiles East, Tiles North) edge, (Tiles East, Tiles North) at)
    {
        if (GridShape(start, edge, at) is not { } shape) return ([], "a grid needs a first side.");

        var (alongEast, alongNorth, acrossEast, acrossNorth, columns, rows) = shape;

        double spacing = GridTiles();

        if (columns + rows + 2 > ConnectPayload.MaxSegments)
        {
            return ([], $"a grid lays at most {ConnectPayload.MaxSegments:N0} Streets, and this one is {columns + rows + 2:N0}.");
        }

        (Tiles, Tiles) Corner(int column, int row) => (
            new Tiles((int)Math.Round(start.East.Raw + (spacing * ((column * alongEast) + (row * acrossEast))))),
            new Tiles((int)Math.Round(start.North.Raw + (spacing * ((column * alongNorth) + (row * acrossNorth))))));

        var streets = new List<Command>(columns + rows + 2);
        for (int row = 0; row <= rows; row++)
        {
            var (from, to) = (Corner(0, row), Corner(columns, row));
            Command street = Command.Street(from.Item1, from.Item2, to.Item1, to.Item2, new SubTiles(0));
            if (!RunsAlongStreet(street)) streets.Add(street);
        }

        for (int column = 0; column <= columns; column++)
        {
            var (from, to) = (Corner(column, 0), Corner(column, rows));
            Command street = Command.Street(from.Item1, from.Item2, to.Item1, to.Item2, new SubTiles(0));
            if (!RunsAlongStreet(street)) streets.Add(street);
        }

        return streets.Count == 0 ? ([], "every line of this grid is already a Street.") : ([.. streets], null);
    }

    /// <summary>
    /// A grid's unit directions along its first side and across it, toward the cursor, with its
    /// block counts each way. Null when the first side has no length.
    /// </summary>
    private (double AlongEast, double AlongNorth, double AcrossEast, double AcrossNorth, int Columns, int Rows)? GridShape(
        (Tiles East, Tiles North) start, (Tiles East, Tiles North) edge, (Tiles East, Tiles North) at)
    {
        double spacing = GridTiles();
        double alongEast = edge.East.Raw - start.East.Raw, alongNorth = edge.North.Raw - start.North.Raw;
        double length = Math.Sqrt((alongEast * alongEast) + (alongNorth * alongNorth));
        if (length < 1) return null;

        (alongEast, alongNorth) = (alongEast / length, alongNorth / length);
        double depth = ((at.North.Raw - start.North.Raw) * alongEast) - ((at.East.Raw - start.East.Raw) * alongNorth);
        double side = depth < 0 ? -1 : 1;

        return (alongEast, alongNorth, -alongNorth * side, alongEast * side,
            Math.Max(1, (int)Math.Round(length / spacing)), Math.Max(1, (int)Math.Round(Math.Abs(depth) / spacing)));
    }

    /// <summary>Whether every quarter point of a straight Street lies within a Tile of an existing Street.</summary>
    private bool RunsAlongStreet(Command street)
    {
        RoadSegmentTable segments = _world.Roads.Segments;

        for (int quarter = 0; quarter <= 4; quarter++)
        {
            long east = (((long)street.East.Raw * (4 - quarter)) + ((long)street.EndEast.Raw * quarter)) * Q16.One / 4;
            long north = (((long)street.North.Raw * (4 - quarter)) + ((long)street.EndNorth.Raw * quarter)) * Q16.One / 4;
            bool near = false;

            foreach (int segment in _world.Roads.Residency.Near(new Tiles((int)(east / Q16.One)), new Tiles((int)(north / Q16.One)), new Tiles(2)))
            {
                if (segments.Rows.IsLive(segment) && segments.Centerline[segment].DistanceTo(east, north) <= Q16.One)
                {
                    near = true;
                    break;
                }
            }

            if (!near) return false;
        }

        return true;
    }

    /// <remarks>An arc too large for Q16.16 is far past a quarter turn, which the core refuses by name.</remarks>
    private static int SagittaThrough((Tiles East, Tiles North) a, (Tiles East, Tiles North) m, (Tiles East, Tiles North) b)
    {
        try
        {
            return StreetArc.SagittaThrough(a.East.Raw, a.North.Raw, m.East.Raw, m.North.Raw, b.East.Raw, b.North.Raw);
        }
        catch (OverflowException)
        {
            return int.MaxValue;
        }
    }

    /// <summary>The sagitta of the arc that leaves <paramref name="a"/> along a Q16.16 unit tangent and reaches <paramref name="b"/>.</summary>
    /// <remarks>
    /// The arc meets its chord at half its sweep θ on both ends, so the sagitta is half the chord
    /// times tan(θ/2), which is cross / (chord + dot) for the chord and the unit tangent.
    /// </remarks>
    private static int SagittaFrom((int East, int North) tangent, (Tiles East, Tiles North) a, (Tiles East, Tiles North) b)
    {
        double east = b.East.Raw - a.East.Raw, north = b.North.Raw - a.North.Raw;
        double tangentEast = tangent.East / (double)Q16.One, tangentNorth = tangent.North / (double)Q16.One;
        double chord = Math.Sqrt((east * east) + (north * north));
        double cross = (east * tangentNorth) - (north * tangentEast);
        double ahead = chord + (east * tangentEast) + (north * tangentNorth);
        double sagitta = ahead <= 0 ? double.MaxValue : chord * 0.5 * cross / ahead * Q16.One;

        return (int)Math.Clamp(Math.Round(sagitta), -int.MaxValue, int.MaxValue);
    }

    /// <summary>
    /// The first point sets the start, a simple curve's second sets the bend, a grid's second sets its
    /// first side, and the last lays the Streets. A refused lay keeps the points. A continuous Street
    /// starts the next one at its end.
    /// </summary>
    private void StreetPoint((Tiles East, Tiles North) at)
    {
        var end = Snap(at);

        if (_streetStart is not { } start)
        {
            _streetStart = (end.East, end.North);

            return;
        }

        if (_streetMode == StreetMode.Curve && _streetBend is null && !Straightened)
        {
            if ((at.East, at.North) != start) _streetBend = at;

            return;
        }

        if (_streetMode == StreetMode.Grid && _streetGridEdge is null)
        {
            if ((end.East, end.North) != start) _streetGridEdge = (end.East, end.North);

            return;
        }

        var (streets, refused) = StreetBatch(start, (end.East, end.North), at);
        if (!SendStreets(streets, refused)) return;

        Command street = streets[0];
        _streetBend = null;
        _streetGridEdge = null;
        if (_streetMode != StreetMode.Continuous
            || !StreetArc.TryCreate(start.East.Raw, start.North.Raw, end.East.Raw, end.North.Raw, street.Sagitta.Raw, out StreetArc laid))
        {
            _streetStart = null;
            _streetTangent = null;

            return;
        }

        _streetStart = (end.East, end.North);
        _streetTangent = laid.TangentAt(laid.Length);
    }

    private void StreetReset()
    {
        _streetStart = null;
        _streetBend = null;
        _streetGridEdge = null;
        _streetTangent = null;
    }

    /// <summary>
    /// What a gesture's Streets would do now, asked of the core once per gesture and Tick: the first
    /// refusal in order, else the treasury's answer to the combined price.
    /// </summary>
    /// <remarks>
    /// Each Street is asked against the city as it stands, so a refusal that only the batch's own
    /// earlier Streets would cause is left to phase 0. A Building two Streets clear is paid for once.
    /// </remarks>
    private StreetBatchPreview Previewed(Command[] streets)
    {
        if (_streetAsked.Tick == _world.Tick.Raw && ReferenceEquals(_streetAsked.Simulation, _simulation)
            && _streetAsked.Streets.SequenceEqual(streets))
        {
            return _streetPreview;
        }

        var buildings = new List<int>();
        Refusal refusal = Refusal.None;
        int refused = 0;
        long total = 0;

        for (int i = 0; i < streets.Length; i++)
        {
            StreetPreview preview = _simulation.PreviewStreet(streets[i]);
            if (refusal == Refusal.None && preview.Refusal is not (Refusal.None or Refusal.StreetTreasuryCannotPay))
            {
                (refusal, refused) = (preview.Refusal, i);
            }

            foreach (int building in preview.Buildings)
            {
                if (buildings.Contains(building)) continue;
                buildings.Add(building);
                total += _world.DemolitionPrice(building).Raw;
            }
        }

        if (refusal == Refusal.None && total > 0 && (_world.TreasuryBalance()?.Raw ?? 0) < total)
        {
            refusal = Refusal.StreetTreasuryCannotPay;
        }

        _streetAsked = (streets, _world.Tick.Raw, _simulation);
        _streetPreview = new StreetBatchPreview(refusal, refused, new Money(total), buildings);

        return _streetPreview;
    }

    /// <summary>Queues every Street of a gesture, or none of them and says why.</summary>
    private bool SendStreets(Command[] streets, string? refused)
    {
        if (refused is not null)
        {
            _refused = refused;

            return false;
        }

        StreetBatchPreview preview = Previewed(streets);
        if (preview.Refusal != Refusal.None)
        {
            _refused = Sentence(preview.Refusal, streets[preview.Refused]);

            return false;
        }

        _queued.AddRange(streets);

        return true;
    }

    /// <summary>
    /// The Street tool's <c>ui</c> words: move the driven cursor, cancel a start, switch snapping,
    /// hold Ctrl, and set Parallel and Grid. A Tile setting of 0 means <c>[roads] block_tiles</c>.
    /// </summary>
    private bool StreetAction(string[] words)
    {
        switch (words)
        {
            case ["street-cancel"]:
                StreetReset();
                _refused = string.Empty;
                return true;

            case ["street-snap", "on" or "off"]:
                _streetSnaps = words[1] == "on";
                return true;

            case ["street-length", "on" or "off"]:
                _streetLengthSnaps = words[1] == "on";
                SaveStreetPreferences();
                return true;

            case ["street-angle", "on" or "off"]:
                _streetAngleSnaps = words[1] == "on";
                SaveStreetPreferences();
                return true;

            case ["street-angle-step", var step] when int.TryParse(step, out int degrees) && StreetAngleSteps.Contains(degrees):
                _streetAngleDegrees = degrees;
                SaveStreetPreferences();
                return true;

            case ["street-reach", var reach] when int.TryParse(reach, out int tiles) && tiles is >= 1 and <= 16:
                _streetReachTiles = tiles;
                SaveStreetPreferences();
                return true;

            case ["street-straight", "on" or "off"]:
                _streetStraight = words[1] == "on";
                return true;

            case ["street-parallel", "on" or "off"]:
                _streetParallel = words[1] == "on";
                return true;

            case ["street-parallel-side", "left" or "right"]:
                _streetParallelRight = words[1] == "right";
                return true;

            case ["street-parallel-tiles", var offset] when int.TryParse(offset, out int tiles) && tiles is >= 0 and <= 1024:
                _streetParallelTiles = tiles;
                SaveStreetPreferences();
                return true;

            case ["street-grid-tiles", var side] when int.TryParse(side, out int tiles) && tiles is >= 0 and <= 1024:
                _streetGridTiles = tiles;
                SaveStreetPreferences();
                return true;

            case ["street-point", var east, var north]
                when int.TryParse(east, out int e) && int.TryParse(north, out int n):
                _aimed = (new Tiles(e), new Tiles(n));
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Reads the snap settings a person chose. A driven or recorded run keeps the defaults, so a
    /// script reproduces on any machine.
    /// </summary>
    private void LoadStreetPreferences(bool driven)
    {
        _streetPreferences = !driven;
        var preferences = new ConfigFile();
        if (driven || preferences.Load(StreetPreferencesPath) != Error.Ok) return;

        _streetLengthSnaps = (bool)preferences.GetValue("street", "length_snap", true);
        _streetAngleSnaps = (bool)preferences.GetValue("street", "angle_snap", true);
        int degrees = (int)preferences.GetValue("street", "angle_step_degrees", 15);
        _streetAngleDegrees = StreetAngleSteps.Contains(degrees) ? degrees : 15;
        _streetReachTiles = Math.Clamp((int)preferences.GetValue("street", "snap_reach_tiles", 4), 1, 16);
        _streetGridTiles = Math.Clamp((int)preferences.GetValue("street", "grid_block_tiles", 0), 0, 1024);
        _streetParallelTiles = Math.Clamp((int)preferences.GetValue("street", "parallel_offset_tiles", 0), 0, 1024);
    }

    private void SaveStreetPreferences()
    {
        if (!_streetPreferences) return;
        var preferences = new ConfigFile();
        preferences.SetValue("street", "length_snap", _streetLengthSnaps);
        preferences.SetValue("street", "angle_snap", _streetAngleSnaps);
        preferences.SetValue("street", "angle_step_degrees", _streetAngleDegrees);
        preferences.SetValue("street", "snap_reach_tiles", _streetReachTiles);
        preferences.SetValue("street", "grid_block_tiles", _streetGridTiles);
        preferences.SetValue("street", "parallel_offset_tiles", _streetParallelTiles);
        preferences.Save(StreetPreferencesPath);
    }

    /// <summary>The hover line for the Street tool: what the next click does and what it would cost.</summary>
    private string StreetSynopsis((Tiles East, Tiles North) at)
    {
        var end = Snap(at);
        bool grid = _streetMode == StreetMode.Grid;
        string unit = grid ? "block" : "plot";
        string snapped = end.Target switch
        {
            SnapTarget.Node => " · snapped to a junction",
            SnapTarget.Segment => " · snapped to a Street",
            _ => (end.Target.HasFlag(SnapTarget.Angle)
                    ? _streetMode == StreetMode.Continuous && _streetTangent is not null && !Straightened ? $" · turns {end.Degrees}°"
                    : end.FromStreet ? $" · {end.Degrees}° to a Street" : $" · {end.Degrees}° from east"
                    : string.Empty)
                + (end.Target.HasFlag(SnapTarget.Length) ? $" · {end.Plots:N0} {unit}{(end.Plots == 1 ? "" : "s")} long" : string.Empty),
        };
        string snapping = _streetSnaps ? "N turns snapping off" : "snapping off, N turns it on";
        string parallel = _streetParallel && !grid
            ? $" · parallel Street {ParallelTiles():N0} Tiles {(_streetParallelRight ? "right" : "left")} (I flips)"
            : string.Empty;
        string mode = StreetModeNames[(int)_streetMode] + (Straightened ? ", straight while Ctrl is held" : string.Empty);

        if (_streetStart is not { } start)
        {
            return $"Street, {mode} · click the start ({end.East.Raw:N0}, {end.North.Raw:N0}){snapped}{parallel} · "
                + $"X changes mode · U lays a parallel Street · Ctrl lays a curve straight · Shift-click removes a grid Street · {snapping}";
        }

        if (_streetMode == StreetMode.Curve && _streetBend is null && !Straightened)
        {
            return $"Street, {mode} from ({start.East.Raw:N0}, {start.North.Raw:N0}) · "
                + $"click the bend point ({at.East.Raw:N0}, {at.North.Raw:N0}) · hold Ctrl to lay it straight · Escape cancels";
        }

        if (grid && _streetGridEdge is null)
        {
            return $"Street, Grid from ({start.East.Raw:N0}, {start.North.Raw:N0}) · click the far corner of the first side "
                + $"({end.East.Raw:N0}, {end.North.Raw:N0}){snapped} · blocks are {GridTiles():N0} Tiles · Escape cancels";
        }

        var (streets, shellRefused) = StreetBatch(start, (end.East, end.North), at);
        string ends = grid && _streetGridEdge is { } edge && GridShape(start, edge, at) is { } shape
            ? $"Street, Grid from ({start.East.Raw:N0}, {start.North.Raw:N0}) · {shape.Columns:N0} × {shape.Rows:N0} blocks, "
                + $"{streets.Length:N0} Streets · drag sideways for more rows"
            : $"Street, {mode} ({start.East.Raw:N0}, {start.North.Raw:N0}) to ({end.East.Raw:N0}, {end.North.Raw:N0}){snapped}{parallel}";

        if (shellRefused is not null)
        {
            return $"{ends} · {shellRefused} · Escape cancels";
        }

        StreetBatchPreview preview = Previewed(streets);

        if (preview.Refusal is not (Refusal.None or Refusal.StreetTreasuryCannotPay))
        {
            return $"{ends} · {Sentence(preview.Refusal, streets[preview.Refused])} · Escape cancels";
        }

        string clears = preview.Buildings.Count == 0
            ? "clears nothing"
            : $"clears {preview.Buildings.Count:N0} {(preview.Buildings.Count == 1 ? "Building" : "Buildings")} for {preview.Price.Raw:N0}";

        return preview.Refusal == Refusal.None
            ? $"{ends} · {clears} · click to lay · Escape cancels"
            : $"{ends} · {clears} · {Sentence(preview.Refusal, streets[0])} · Escape cancels";
    }

    /// <summary>
    /// Draws the snapped end, and once a start is set, the Streets the gesture would lay and the
    /// Buildings they would clear. A refused Street draws red.
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

        bool bending = _streetMode == StreetMode.Curve && _streetBend is null && !Straightened;
        if (_streetStart is { } start && bending && (start.East, start.North) != (at.East, at.North))
        {
            Transform3D chord = Box(TileGround(start.East, start.North), TileGround(at.East, at.North), 0.5f, 0f, 0.3f);
            chord.Origin += new Vector3(0f, 0.1f, 0f);
            _cursor.Multimesh.SetInstanceTransform(count, chord);
            _cursor.Multimesh.SetInstanceColor(count++, new Color(.95f, .95f, .95f).SrgbToLinear());
        }
        else if (_streetStart is { } origin && !bending
            && ((origin.East, origin.North) != (end.East, end.North) || _streetGridEdge is not null))
        {
            var (streets, shellRefused) = StreetBatch(origin, (end.East, end.North), at);
            StreetBatchPreview preview = shellRefused is null && streets.Length > 0 ? Previewed(streets) : default;
            bool allRefused = shellRefused is not null || preview.Refusal == Refusal.StreetTreasuryCannotPay;
            Color laid = new Color(.95f, .80f, .25f).SrgbToLinear(), refused = new Color(.90f, .30f, .25f).SrgbToLinear();
            Vector3 lift = new(0f, 0.1f, 0f);

            for (int i = 0; i < streets.Length; i++)
            {
                Command street = streets[i];
                Color line = allRefused || (preview.Refusal != Refusal.None && i == preview.Refused) ? refused : laid;
                Vector3 from = TileGround(street.East, street.North), to = TileGround(street.EndEast, street.EndNorth);
                IEnumerable<Transform3D> chords = StreetArc.TryCreate(street.East.Raw, street.North.Raw,
                        street.EndEast.Raw, street.EndNorth.Raw, street.Sagitta.Raw, out StreetArc arc)
                    ? Chords(arc, from, to, CarriagewayWidthMetres, 0.3f)
                    : [Box(from, to, CarriagewayWidthMetres, 0f, 0.3f)];
                foreach (Transform3D chord in chords)
                {
                    _cursor.Multimesh.SetInstanceTransform(count, chord with { Origin = chord.Origin + lift });
                    _cursor.Multimesh.SetInstanceColor(count++, line);
                }
            }

            foreach (int building in preview.Buildings ?? [])
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
