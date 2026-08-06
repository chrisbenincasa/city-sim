using Borough.Core.Arithmetic;

namespace S2.Routing.Graph;

/// <summary>
/// Generates the synthetic Road Graph R0 measures against. <c>plans/0010</c> R0: <i>"Generate a
/// 4096² graph: grid-snapped Streets falling out of the Tile grid, freeform Arterials with authored
/// Junction pieces, one graph with mode masks rather than two networks."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>Mode masks are not decoration here.</b> The plan says why: <i>"a multi-Leg Trip is routed by a
/// single mode-aware search, and a router measured without them is measuring the wrong search."</i>
/// So the pedestrian network is a subgraph of this one rather than a second structure, exactly as
/// <c>03 §3.7</c> requires.
/// </para>
/// <para>
/// <b>The Arterials genuinely sever, and that is the generator's one substantive claim.</b> An
/// Arterial physically occupies the ground its polyline crosses, so every Street Segment it crosses
/// is either deleted or kept as a designated crossing. This is not realism for its own sake: a
/// generator whose Arterials politely overlay the grid produces a graph with no detours in it, and
/// a router measured on that graph has never been asked the question the city actually asks. It is
/// also the only way <c>CONTEXT.md</c> → Severance can be observed at all, and the plan is explicit
/// that <i>"a spike that never routes a walk cannot see the design's flagship emergent
/// behaviour."</i>
/// </para>
/// <para>
/// <b>Three things it deliberately does not model, named so nobody assumes they were forgotten.</b>
/// There are no one-way streets — every Segment's two arcs carry the same mask, though the
/// structure stores them separately and would carry a difference. Arterial-to-Arterial crossings get
/// no Junction piece, so two Arterials meeting simply pass over one another; adding them is a
/// parameter, not a rewrite. And Fidelity is zero everywhere, because S2 never simulates a vehicle.
/// </para>
/// </remarks>
internal static class GraphGenerator
{
    /// <summary>Tiles per polyline step. One, so no Arterial can skip a grid line it crosses.</summary>
    private const int StepTiles = 1;

    /// <summary>Steps between curvature perturbations. A gentle bend over hundreds of Tiles.</summary>
    private const int CurvatureEverySteps = 48;

    /// <summary>Street capacity: 2 lanes at 1,800 veh/h/lane.</summary>
    private const int StreetCapacity = 3_600 * Units.PerVehiclePerHour;

    /// <summary>Arterial capacity: 6 lanes at 2,000 veh/h/lane.</summary>
    private const int ArterialCapacity = 12_000 * Units.PerVehiclePerHour;

    /// <summary>A foot path carries no vehicles. Nominal, so <c>volume / capacity</c> is defined.</summary>
    private const int FootPathCapacity = 1_000 * Units.PerVehiclePerHour;

    public static RoadGraph Build(GraphParameters p)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(p.BlockTiles);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(p.MapTiles);

        var b = new Builder(p);
        b.LayStreets();
        b.LayArterials();
        b.ApplySeverance();
        b.LayFootPaths();
        return b.Finish();
    }

    /// <summary>
    /// The mutable scaffolding. A class rather than a struct because it is generation-time only —
    /// nothing here survives into <see cref="RoadGraph"/>, which is arrays.
    /// </summary>
    private sealed class Builder(GraphParameters p)
    {
        /// <summary>Node columns and rows in the Street grid.</summary>
        private readonly int _grid = IntegerMath.FloorDiv(p.MapTiles, p.BlockTiles) + 1;

        private readonly List<int> _nodeX = [];
        private readonly List<int> _nodeY = [];

        private readonly List<int> _segA = [];
        private readonly List<int> _segB = [];
        private readonly List<int> _segLength = [];
        private readonly List<int> _segCapacity = [];
        private readonly List<int> _segFreeFlow = [];
        private readonly List<byte> _segModes = [];

        /// <summary>
        /// Which Street Segments an Arterial runs over. Indexed by Street Segment id, which is why
        /// the Streets are laid first and in a known order.
        /// </summary>
        private bool[] _severed = [];

        /// <summary>Street Segments the generator has kept as a pedestrian crossing.</summary>
        private readonly List<int> _crossings = [];

        private int _streetSegments;

        private int _footPaths;

        private int _arterialRuns;

        private long _arterialRunTiles;

        private int _arterialRamps;

        // --- Streets ---------------------------------------------------------------------------

        /// <summary>
        /// The grid-snapped Streets, <i>"falling out of the Tile grid directly"</i>
        /// (<c>CONTEXT.md</c> → Street). Laid in a fixed order — every horizontal edge, then every
        /// vertical one — because severance detection addresses them by index.
        /// </summary>
        public void LayStreets()
        {
            for (int y = 0; y < _grid; y++)
            {
                for (int x = 0; x < _grid; x++)
                {
                    _nodeX.Add(x * p.BlockTiles);
                    _nodeY.Add(y * p.BlockTiles);
                }
            }

            for (int y = 0; y < _grid; y++)
            {
                for (int x = 0; x < _grid - 1; x++)
                {
                    AddSegment(GridNode(x, y), GridNode(x + 1, y), p.BlockTiles,
                        StreetCapacity, Units.StreetFreeFlow, Modes.Foot | Modes.Car);
                }
            }

            for (int x = 0; x < _grid; x++)
            {
                for (int y = 0; y < _grid - 1; y++)
                {
                    AddSegment(GridNode(x, y), GridNode(x, y + 1), p.BlockTiles,
                        StreetCapacity, Units.StreetFreeFlow, Modes.Foot | Modes.Car);
                }
            }

            _streetSegments = _segA.Count;
            _severed = new bool[_streetSegments];
        }

        private int GridNode(int x, int y) => (y * _grid) + x;

        private int HorizontalSegment(int x, int y) => (y * (_grid - 1)) + x;

        private int VerticalSegment(int x, int y) => (_grid * (_grid - 1)) + (x * (_grid - 1)) + y;

        // --- Arterials -------------------------------------------------------------------------

        /// <summary>
        /// The freeform Arterials. Each is walked as a polyline at one-Tile steps with a gentle
        /// hashed curvature, and Junction pieces are placed at a fixed interval of <b>arc length</b>
        /// along it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The Segment's length is the arc length, not the distance between its Junctions</b>, and
        /// that gap is deliberate. <c>CONTEXT.md</c> → Segment puts an Arterial's nodes at the
        /// authored Junction pieces, and the road between two of them curves. A distance heuristic
        /// therefore underestimates on these Segments, which is the safe direction; what is
        /// <i>not</i> safe is the other effect this same loop produces — an Arterial at an arbitrary
        /// angle offers a diagonal shortcut across a grid where Manhattan says there is none, and
        /// that is the case that makes the tight heuristics inadmissible. Both live here, and R0's
        /// verdict is the Arterial density at which the second one starts to bite.
        /// </para>
        /// </remarks>
        public void LayArterials()
        {
            for (int a = 0; a < p.ArterialCount; a++)
            {
                WalkArterial((ulong)a);
            }
        }

        private void WalkArterial(ulong arterial)
        {
            (int px, int py, int hx, int hy) = Entry(arterial);

            int previousJunction = -1;
            int stepsSinceJunction = 0;
            int step = 0;

            (int stepX, int stepY) = UnitStep(hx, hy);

            int tileX = Fixed.ToIntFloor(px);
            int tileY = Fixed.ToIntFloor(py);

            while (InBounds(tileX, tileY))
            {
                // A Junction piece every so many Tiles of arc length. The first one anchors the
                // Arterial's first Segment; before it there is road with no graph edge, which is
                // correct — an Arterial with no Junction on it is unreachable, and that is what a
                // limited-access road is.
                if (stepsSinceJunction >= p.ArterialJunctionSpacingTiles || previousJunction < 0)
                {
                    int junction = AddNode(tileX, tileY);
                    ConnectToGrid(junction, tileX, tileY);

                    if (previousJunction >= 0)
                    {
                        AddSegment(previousJunction, junction, stepsSinceJunction * StepTiles,
                            ArterialCapacity, Units.ArterialFreeFlow, Modes.Car);
                        _arterialRuns++;
                        _arterialRunTiles += stepsSinceJunction * StepTiles;
                    }

                    previousJunction = junction;
                    stepsSinceJunction = 0;
                }

                if ((step % CurvatureEverySteps) == 0 && step > 0)
                {
                    hx += CounterHash.Below(CounterHash.Of(
                        p.Seed, arterial, (ulong)step * 2, CounterHash.Purpose.ArterialCurvature), 121) - 60;
                    hy += CounterHash.Below(CounterHash.Of(
                        p.Seed, arterial, ((ulong)step * 2) + 1, CounterHash.Purpose.ArterialCurvature), 121) - 60;
                    (stepX, stepY) = UnitStep(hx, hy);
                }

                px += stepX;
                py += stepY;
                step++;
                stepsSinceJunction++;

                int nextX = Fixed.ToIntFloor(px);
                int nextY = Fixed.ToIntFloor(py);
                MarkCrossings(tileX, tileY, nextX, nextY);
                tileX = nextX;
                tileY = nextY;
            }
        }

        /// <summary>
        /// Where an Arterial enters the map and which way it is pointing. It starts on an edge and
        /// heads inward, so every Arterial is a genuine crossing of the city rather than a stub.
        /// </summary>
        private (int PositionX, int PositionY, int HeadingX, int HeadingY) Entry(ulong arterial)
        {
            // Four independent draws, four distinct counters. Never one hash sliced into pieces —
            // see CounterHash.Below for what that cost the first capture.
            int edge = CounterHash.Below(
                CounterHash.Of(p.Seed, arterial, 0, CounterHash.Purpose.ArterialOrigin), 4);
            int along = CounterHash.Below(
                CounterHash.Of(p.Seed, arterial, 1, CounterHash.Purpose.ArterialOrigin), p.MapTiles);

            // Inward component is strictly positive, cross component is free — which is what makes
            // the angle arbitrary rather than one of eight.
            int inward = 300 + CounterHash.Below(
                CounterHash.Of(p.Seed, arterial, 0, CounterHash.Purpose.ArterialHeading), 701);
            int across = CounterHash.Below(
                CounterHash.Of(p.Seed, arterial, 1, CounterHash.Purpose.ArterialHeading), 2001) - 1000;

            return edge switch
            {
                0 => (Fixed.FromInt(0), Fixed.FromInt(along), inward, across),
                1 => (Fixed.FromInt(p.MapTiles - 1), Fixed.FromInt(along), -inward, across),
                2 => (Fixed.FromInt(along), Fixed.FromInt(0), across, inward),
                _ => (Fixed.FromInt(along), Fixed.FromInt(p.MapTiles - 1), across, -inward),
            };
        }

        /// <summary>
        /// A Q16.16 step vector of length one Tile in the given direction. No trigonometry and no
        /// floating point — a direction is an integer vector and the step is that vector scaled by
        /// its own magnitude.
        /// </summary>
        private static (int X, int Y) UnitStep(int headingX, int headingY)
        {
            long magnitude = IntegerGeometry.SqrtFloor(
                ((long)headingX * headingX) + ((long)headingY * headingY));

            if (magnitude == 0)
            {
                return (Fixed.One, 0);
            }

            return (
                (int)IntegerMath.FloorDiv((long)headingX * Fixed.One * StepTiles, magnitude),
                (int)IntegerMath.FloorDiv((long)headingY * Fixed.One * StepTiles, magnitude));
        }

        private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < p.MapTiles && y < p.MapTiles;

        /// <summary>
        /// Records every Street Segment the Arterial ran over between two consecutive polyline
        /// positions. The step is one Tile, so at most one grid line of each orientation can be
        /// crossed and neither can be skipped.
        /// </summary>
        private void MarkCrossings(int fromX, int fromY, int toX, int toY)
        {
            int fromColumn = IntegerMath.FloorDiv(fromX, p.BlockTiles);
            int toColumn = IntegerMath.FloorDiv(toX, p.BlockTiles);
            int fromRow = IntegerMath.FloorDiv(fromY, p.BlockTiles);
            int toRow = IntegerMath.FloorDiv(toY, p.BlockTiles);

            if (fromColumn != toColumn)
            {
                int column = fromColumn > toColumn ? fromColumn : toColumn;
                int row = fromRow;
                if (column >= 0 && column < _grid && row >= 0 && row < _grid - 1)
                {
                    _severed[VerticalSegment(column, row)] = true;
                }
            }

            if (fromRow != toRow)
            {
                int row = fromRow > toRow ? fromRow : toRow;
                int column = fromColumn;
                if (row >= 0 && row < _grid && column >= 0 && column < _grid - 1)
                {
                    _severed[HorizontalSegment(column, row)] = true;
                }
            }
        }

        /// <summary>
        /// The authored Junction piece's connection to the Street network — a ramp to the nearest
        /// grid intersection.
        /// </summary>
        /// <remarks>
        /// <b>The ramp carries cars and not pedestrians</b>, which is a modelling choice worth
        /// stating rather than a detail. A pedestrian's ability to get from one side of an Arterial
        /// to the other is then a property of the crossings alone, which is exactly the quantity
        /// <see cref="GraphParameters.FootCrossingEverySevered"/> exists to sweep. Letting the ramp
        /// carry feet would put a pedestrian route at every Junction and quietly make Severance
        /// unmeasurable.
        /// </remarks>
        private void ConnectToGrid(int junction, int tileX, int tileY)
        {
            int column = Clamp(IntegerMath.RoundDiv(tileX, p.BlockTiles), 0, _grid - 1);
            int row = Clamp(IntegerMath.RoundDiv(tileY, p.BlockTiles), 0, _grid - 1);
            int node = GridNode(column, row);

            int length = IntegerGeometry.Distance(tileX, tileY, _nodeX[node], _nodeY[node]);
            if (length <= 0)
            {
                length = StepTiles;
            }

            AddSegment(junction, node, length, ArterialCapacity, Units.ArterialFreeFlow, Modes.Car);
            _arterialRamps++;
        }

        private static int Clamp(int value, int low, int high) =>
            value < low ? low : value > high ? high : value;

        // --- Severance -------------------------------------------------------------------------

        /// <summary>
        /// Deletes the Street Segments the Arterials run over, keeping every
        /// <see cref="GraphParameters.FootCrossingEverySevered"/>th one as a pedestrian crossing.
        /// </summary>
        /// <remarks>
        /// A kept crossing loses <see cref="Modes.Car"/> and keeps <see cref="Modes.Foot"/>: the
        /// road is gone, the footbridge is not. That asymmetry is the whole of Severance —
        /// <i>"a city can be perfectly well connected for cars and broken for people"</i> is the
        /// claim, and this is the line that lets the measurement disagree with it.
        /// </remarks>
        public void ApplySeverance()
        {
            int severedSoFar = 0;

            for (int segment = 0; segment < _streetSegments; segment++)
            {
                if (!_severed[segment])
                {
                    continue;
                }

                severedSoFar++;

                bool crossing = p.FootCrossingEverySevered > 0
                    && (severedSoFar % p.FootCrossingEverySevered) == 0;

                if (crossing)
                {
                    _segModes[segment] = (byte)Modes.Foot;
                    _segFreeFlow[segment] = Units.WalkFreeFlow;
                    _segCapacity[segment] = FootPathCapacity;
                    _crossings.Add(segment);
                    _severed[segment] = false;
                }
            }
        }

        // --- Foot-only Segments ------------------------------------------------------------------

        /// <summary>
        /// The block cut-throughs. <c>CONTEXT.md</c> → Segment: the foot-only Segments are
        /// <i>"few and they are the edges Severance turns on, so nothing may size the graph by
        /// omitting them."</i>
        /// </summary>
        public void LayFootPaths()
        {
            if (p.FootPathsPerThousandBlocks <= 0)
            {
                return;
            }

            int diagonal = IntegerGeometry.Distance(0, 0, p.BlockTiles, p.BlockTiles);

            for (int y = 0; y < _grid - 1; y++)
            {
                for (int x = 0; x < _grid - 1; x++)
                {
                    ulong draw = CounterHash.Of(
                        p.Seed, (ulong)((y * _grid) + x), 0, CounterHash.Purpose.FootPathPlacement);

                    if (CounterHash.Below(draw, 1000) >= p.FootPathsPerThousandBlocks)
                    {
                        continue;
                    }

                    AddSegment(GridNode(x, y), GridNode(x + 1, y + 1), diagonal,
                        FootPathCapacity, Units.WalkFreeFlow, Modes.Foot);
                    _footPaths++;
                }
            }
        }

        // --- Assembly ----------------------------------------------------------------------------

        private int AddNode(int x, int y)
        {
            _nodeX.Add(x);
            _nodeY.Add(y);
            return _nodeX.Count - 1;
        }

        private void AddSegment(int a, int b, int length, int capacity, int freeFlow, Modes modes)
        {
            _segA.Add(a);
            _segB.Add(b);
            _segLength.Add(length);
            _segCapacity.Add(capacity);
            _segFreeFlow.Add(freeFlow);
            _segModes.Add((byte)modes);
        }

        /// <summary>
        /// Drops the severed Segments, renumbers, and builds the directed CSR.
        /// </summary>
        public RoadGraph Finish()
        {
            int total = _segA.Count;
            var keep = new List<int>(total);
            for (int i = 0; i < total; i++)
            {
                if (i >= _streetSegments || !_severed[i])
                {
                    keep.Add(i);
                }
            }

            int segments = keep.Count;
            var segA = new int[segments];
            var segB = new int[segments];
            var segLength = new int[segments];
            var segCapacity = new int[segments];
            var segFreeFlow = new int[segments];
            var segModes = new byte[segments];

            for (int i = 0; i < segments; i++)
            {
                int source = keep[i];
                segA[i] = _segA[source];
                segB[i] = _segB[source];
                segLength[i] = _segLength[source];
                segCapacity[i] = _segCapacity[source];
                segFreeFlow[i] = _segFreeFlow[source];
                segModes[i] = _segModes[source];
            }

            int nodes = _nodeX.Count;
            var degree = new int[nodes + 1];
            for (int i = 0; i < segments; i++)
            {
                degree[segA[i]]++;
                degree[segB[i]]++;
            }

            var arcStart = new int[nodes + 1];
            int running = 0;
            for (int node = 0; node < nodes; node++)
            {
                arcStart[node] = running;
                running += degree[node];
            }

            arcStart[nodes] = running;

            var cursor = new int[nodes];
            Array.Copy(arcStart, cursor, nodes);

            var arcTarget = new int[running];
            var arcSegment = new int[running];
            var arcModes = new byte[running];
            var arcCar = new int[running];
            var arcFoot = new int[running];

            for (int i = 0; i < segments; i++)
            {
                Emit(segA[i], segB[i], i);
                Emit(segB[i], segA[i], i);
            }

            void Emit(int from, int to, int segment)
            {
                int arc = cursor[from]++;
                arcTarget[arc] = to;
                arcSegment[arc] = segment;
                arcModes[arc] = segModes[segment];
                arcCar[arc] = RoadGraph.TraversalTicks(
                    segLength[segment], segFreeFlow[segment], Modes.Car, arcModes[arc]);
                arcFoot[arc] = RoadGraph.TraversalTicks(
                    segLength[segment], segFreeFlow[segment], Modes.Foot, arcModes[arc]);
            }

            int volumeSlots = p.VolumeScope == VolumeScope.PerSegment ? segments : segments * 2;

            var graph = new RoadGraph
            {
                Parameters = p,
                NodeX = [.. _nodeX],
                NodeY = [.. _nodeY],
                SegmentNodeA = segA,
                SegmentNodeB = segB,
                SegmentLengthTiles = segLength,
                SegmentCapacity = segCapacity,
                SegmentFreeFlow = segFreeFlow,
                SegmentModes = segModes,
                SegmentFidelity = new byte[segments],
                SeveredStreets = SeveredCount(),
                FootCrossings = _crossings.Count,
                FootPaths = _footPaths,
                ArterialRuns = _arterialRuns,
                ArterialRunTiles = _arterialRunTiles,
                ArterialRamps = _arterialRamps,
                Volume = new int[volumeSlots],
                ArcStart = arcStart,
                ArcTarget = arcTarget,
                ArcSegment = arcSegment,
                ArcModes = arcModes,
                ArcCarTicks = arcCar,
                ArcFootTicks = arcFoot,
            };

            graph.AssertWellFormed();
            return graph;
        }

        /// <summary>How many Street Segments the Arterials severed outright, for the report.</summary>
        private int SeveredCount()
        {
            int count = 0;
            for (int i = 0; i < _severed.Length; i++)
            {
                if (_severed[i])
                {
                    count++;
                }
            }

            return count;
        }
    }
}
