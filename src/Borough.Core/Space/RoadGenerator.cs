using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Lays a world's roads — <b>grid-snapped Streets falling out of the Tile grid, freeform Arterials
/// with authored Junction pieces, one graph with mode masks rather than two networks</b>
/// (<c>adr/0014</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>A generator rather than a player, and that is what makes 5a shippable without a
/// <c>build_road</c> command.</b> <c>CommandKind.Connect</c> is declared and throws;
/// <c>01 §2</c> counts road editing among the player's five verbs and the corpus calls it the
/// player's core verb, and <b>nowhere specifies its command surface</b>. Giving the graph a producer
/// that is not the player lets this slice retire its own risk — <i>geometry leaking into the
/// simulation</i> — without also settling a command shape nobody has designed. That is 5a-bis, along
/// with the Lot subdivider.
/// </para>
/// <para>
/// <b>The Arterials genuinely sever, and that is this generator's one substantive claim.</b> An
/// Arterial occupies the ground its polyline crosses, so every Street Segment it crosses is either
/// deleted or kept as a designated crossing. This is not realism for its own sake: a generator whose
/// Arterials politely overlay the grid produces a graph with no detours in it, and it is the only way
/// <c>CONTEXT.md</c> → Severance can be observed at all. A kept crossing loses
/// <see cref="TravelMode.Car"/> and keeps <see cref="TravelMode.Foot"/> — the road is gone, the
/// footbridge is not — and that asymmetry is the whole of Severance.
/// </para>
/// <para>
/// <b>Three things it deliberately does not model, named so nobody assumes they were forgotten.</b>
/// There are no one-way streets: every Segment's two masks are written equal, though the columns are
/// separate and would carry a difference (<c>adr/0072</c>). Arterial-to-Arterial crossings get no
/// Junction piece, so two Arterials pass over one another; adding them is a parameter rather than a
/// rewrite. And <see cref="RoadSegmentTable.Fidelity"/> is zero everywhere, because nothing moves yet.
/// </para>
/// <para>
/// <b>Every draw takes a distinct counter under its own tag rather than slicing one hash.</b> S2's
/// first capture sliced, the reduction consumed the bits the slice had zeroed, every Arterial drew the
/// same heading and left the map within a step — and nothing in the report said so, because a graph
/// with no Arterials in it is still a graph. See <see cref="PurposeTag.RoadArterialCurvature"/>.
/// </para>
/// </remarks>
public static class RoadGenerator
{
    /// <summary>Tiles per polyline step. One, so no Arterial can skip a grid line it crosses.</summary>
    private const int StepTiles = 1;

    /// <summary>Steps between curvature perturbations. A gentle bend over hundreds of Tiles.</summary>
    private const int CurvatureEverySteps = 48;

    /// <summary>
    /// Lays a network reaching <paramref name="extentTiles"/> Tiles from the origin corner into
    /// <paramref name="graph"/>, then rebuilds everything derived.
    /// </summary>
    /// <remarks>
    /// <b>The extent is a required argument with no default, and that is the point of it.</b> This
    /// method laid <see cref="CellGrid.WorldTiles"/> unconditionally until 2026-08-13, which made
    /// <c>adr/0021</c>'s <i>memory and save size scale with developed area, not with map area</i>
    /// false in exactly one place. A default would restore that for every call site that did not stop
    /// to think, which is how <c>[placement]</c>'s <c>revisit_ticks</c> shipped at a value copied from
    /// somewhere it meant something else and left 45% of the housing stock empty.
    /// <para>
    /// <b>A caller that genuinely wants the whole map says so.</b> A severance demonstration does,
    /// because it is a demonstration rather than a city; <see cref="Entities.SyntheticCity"/> does
    /// not, because it knows how many Buildings it is about to raise and the lattice that houses them
    /// follows from that.
    /// </para>
    /// </remarks>
    /// <param name="graph">The graph to fill. Expected empty; this is a world-creation pass.</param>
    /// <param name="key">The world seed, as <see cref="Randomness.Draw"/>'s first coordinate.</param>
    /// <param name="extentTiles">
    /// How far the lattice reaches from the origin corner, in Tiles. Clamped to the map.
    /// </param>
    /// <exception cref="InvalidOperationException">The graph already has Segments.</exception>
    public static void LayInto(
        RoadGraph graph, WorldKey key, int extentTiles, MapLayers? layers = null) =>
        LayInto(graph, key, OneAtTheOrigin, extentTiles, layers);

    /// <summary>
    /// Lays one lattice per <paramref name="lattices"/> entry, each reaching
    /// <paramref name="extentTiles"/> Tiles from its own origin, joins consecutive ones with a Street
    /// corridor, then rebuilds everything derived.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One extent for every lattice, because the shares are equal</b> — a Lattice paves what its
    /// share of the world's Lots needs and every Lattice gets the same share
    /// (<see cref="Entities.SyntheticCity"/>). A per-lattice extent would be a second authored number
    /// per table with nothing to ratify it; equal shares make it one derivation for all of them.
    /// </para>
    /// <para>
    /// 🔴 <b>The corridor is what makes this a world the District derivation can be tested on, and it
    /// is the whole reason the lattices are joined rather than left apart.</b>
    /// <c>adr/0134</c> clips the watershed to a road component and explicitly <em>rejected</em>
    /// splitting on components alone — <i>"a connected city is one District for ever, which is
    /// <c>adr/0013</c>'s explicitly rejected pool everything, city-wide wearing a derivation"</i>. Two
    /// lattices in two components would let component labelling pass for a watershed, which is the
    /// rejected mechanism wearing the chosen one's name. <b>Joined, the only thing that can find the
    /// boundary is the density field</b>, and that is the thing under test.
    /// </para>
    /// <para>
    /// <b>Street rather than Arterial, and <see cref="TravelMode.Any"/> both ways.</b> An Arterial
    /// link would carry cars and not feet (<c>ConnectToGrid</c>'s remark says why the ramps do that),
    /// so the world would be one component for driving and two for walking — and which of those the
    /// clip reads would become a fork landing on whoever writes the watershed. One component in every
    /// mode is one fewer question.
    /// </para>
    /// <para>
    /// <b>It carries no Lots and that is not an accident of the geometry.</b>
    /// <c>SyntheticCity.Subdivide</c> walks each Lattice's own block box and never the ground between
    /// them, so the corridor is road with nothing on it — which is what keeps the density field's
    /// saddle at zero and the two centres unambiguous.
    /// </para>
    /// </remarks>
    /// <param name="graph">The graph to fill. Expected empty; this is a world-creation pass.</param>
    /// <param name="key">The world seed, as <see cref="Randomness.Draw"/>'s first coordinate.</param>
    /// <param name="lattices">Where the lattices stand, in declaration order. At least one.</param>
    /// <param name="extentTiles">How far each lattice reaches from its origin, in Tiles.</param>
    /// <exception cref="InvalidOperationException">
    /// The graph already has Segments, or two lattices overlap.
    /// </exception>
    public static void LayInto(
        RoadGraph graph,
        WorldKey key,
        LatticeDefinition[] lattices,
        int extentTiles,
        MapLayers? layers = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(lattices);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(extentTiles);

        if (graph.Segments.Rows.LiveCount != 0)
        {
            throw new InvalidOperationException(
                "this world already has roads. The generator is a world-creation pass and lays a "
                + "whole network at once; editing a standing graph is CommandKind.Connect, which is "
                + "5a-bis and has no command surface yet.");
        }

        RoadRuleset roads = graph.Ruleset;

        if (!roads.Runs)
        {
            return;
        }

        LatticeDefinition[] standing = lattices.Length == 0 ? OneAtTheOrigin : lattices;

        var layouts = new Layout[standing.Length];

        for (int lattice = 0; lattice < standing.Length; lattice++)
        {
            LatticeDefinition origin = standing[lattice];

            // Clamped to what is left of the map east and north of this origin, which is the
            // single-lattice clamp with an origin in it. A ternary rather than Math.Min: BOR0202
            // refuses Math.* everywhere, including here.
            int room = CellGrid.WorldTiles - (origin.OriginEastTiles > origin.OriginNorthTiles
                ? origin.OriginEastTiles
                : origin.OriginNorthTiles);

            int extent = extentTiles < room ? extentTiles : room;

            layouts[lattice] = new Layout(graph, roads, key, extent, origin, lattice, layers);
        }

        RefuseOverlap(layouts);

        foreach (Layout layout in layouts)
        {
            layout.Run();
        }

        for (int lattice = 1; lattice < layouts.Length; lattice++)
        {
            Link(graph, roads, layouts[lattice - 1], layouts[lattice]);
        }

        graph.RebuildDerived();
    }

    /// <summary>The world every Ruleset described before <c>[[lattice]]</c> existed.</summary>
    private static readonly LatticeDefinition[] OneAtTheOrigin = [new LatticeDefinition(0, 0)];

    /// <summary>
    /// Refuses two lattices standing on the same ground.
    /// </summary>
    /// <remarks>
    /// <b>Thrown rather than refused at load, because the extent is not knowable there.</b> A
    /// Lattice's extent is derived from the population the world was allocated for, which the loader
    /// does not have — so whether two authored origins overlap is a property of the Ruleset
    /// <em>and</em> the world, exactly like <c>RulesetLoader.Reload</c>'s frozen constants. What
    /// overlap would produce is two Nodes on one Tile and a lattice laid twice, which is silent.
    /// </remarks>
    private static void RefuseOverlap(Layout[] layouts)
    {
        for (int first = 0; first < layouts.Length; first++)
        {
            for (int second = first + 1; second < layouts.Length; second++)
            {
                if (layouts[first].Overlaps(layouts[second]))
                {
                    throw new InvalidOperationException(
                        $"lattice {first} and lattice {second} stand on the same ground. Each one "
                        + "paves what its share of the population needs, so two origins far enough "
                        + "apart for a small city can overlap in a large one -- the gap a "
                        + "[[lattice]] table authors is a distance and the extent it has to clear "
                        + "is not authored at all. Move the origins apart.");
                }
            }
        }
    }

    /// <summary>
    /// Joins two lattices with a corridor of Street Segments, east leg then north leg.
    /// </summary>
    /// <remarks>
    /// <b>An L rather than a straight line, so the two origins need not share an axis.</b> It runs
    /// between the two Nodes nearest each other — each lattice's own corner clamped towards the other
    /// — in whole blocks, which is why an origin off the block grid is refused at load. The corner
    /// Node is the only one the corridor invents that is not on either lattice.
    /// </remarks>
    private static void Link(RoadGraph graph, RoadRuleset roads, Layout from, Layout to)
    {
        BlockLattice lattice = graph.Lattice;

        (Handle<RoadNode> start, int east, int north) = from.NearestTo(to.OriginEast, to.OriginNorth);
        (Handle<RoadNode> end, int endEast, int endNorth) = to.NearestTo(east, north);

        Handle<RoadNode> current = start;

        // 🔴 A STEP TO THE NEXT LINE, and it used to be a step of one block. The corridor lands on
        // the same Nodes either way while the lines are evenly spaced; on a lattice whose lines are
        // not, a fixed step walks off the grid and invents a Node between two intersections.
        // plans/0045 row 25.
        int step = endEast > east ? 1 : -1;

        while (east != endEast)
        {
            int was = east;

            east = lattice.EdgeOf(lattice.LineAt(east) + step);

            Handle<RoadNode> next = east == endEast && north == endNorth
                ? end
                : graph.Nodes.Create(new Tiles(east), new Tiles(north));

            graph.Segments.Create(
                current,
                next,
                new Tiles(IntegerMath.Abs(east - was)),
                RoadKind.Street,
                TravelMode.Any,
                TravelMode.Any);

            current = next;
        }

        step = endNorth > north ? 1 : -1;

        while (north != endNorth)
        {
            int was = north;

            north = lattice.EdgeOf(lattice.LineAt(north) + step);

            Handle<RoadNode> next = north == endNorth
                ? end
                : graph.Nodes.Create(new Tiles(east), new Tiles(north));

            graph.Segments.Create(
                current,
                next,
                new Tiles(IntegerMath.Abs(north - was)),
                RoadKind.Street,
                TravelMode.Any,
                TravelMode.Any);

            current = next;
        }
    }

    /// <summary>
    /// The generation-time scaffolding. <b>A class rather than a struct because nothing here survives
    /// into the graph</b>, which is tables.
    /// </summary>
    private sealed class Layout(
        RoadGraph graph,
        RoadRuleset roads,
        WorldKey key,
        int extentTiles,
        LatticeDefinition origin,
        int index,
        MapLayers? layers)
    {
        /// <summary>
        /// Where the lines stand — <b>this world's, and not an arithmetic on
        /// <c>block_tiles</c></b> (<see cref="BlockLattice"/>).
        /// </summary>
        private readonly BlockLattice _lattice = graph.Lattice;

        /// <summary>The global line this lattice's west edge stands on.</summary>
        /// <remarks>
        /// <b>Exact, because <c>RulesetLoader</c> refuses an origin off the block grid.</b> ⚠ That
        /// refusal is stated as <em>a multiple of <c>block_tiles</c></em>, which is the same
        /// sentence as <em>on a line</em> only while the lines are evenly spaced —
        /// <c>plans/0045</c> row 25.
        /// </remarks>
        private readonly int _lineEast = graph.Lattice.LineAt(origin.OriginEastTiles);

        /// <summary>The global line this lattice's south edge stands on.</summary>
        private readonly int _lineNorth = graph.Lattice.LineAt(origin.OriginNorthTiles);

        /// <summary>Node columns and rows in the Street grid.</summary>
        private readonly int _grid = graph.Lattice.LinesIn(origin.OriginEastTiles, extentTiles);

        /// <summary>The Tile this lattice's west edge stands on.</summary>
        public int OriginEast => origin.OriginEastTiles;

        /// <summary>The Tile this lattice's south edge stands on.</summary>
        public int OriginNorth => origin.OriginNorthTiles;

        /// <summary>The Tile column <paramref name="column"/>'s line stands on.</summary>
        /// <remarks>
        /// <b>The global spacing, translated to this lattice's origin.</b> An origin on a line — the
        /// only kind the loader admits — makes this exactly <c>EdgeOf(_lineEast + column)</c>; the
        /// subtraction is what keeps an origin that is not one a translation rather than a snap, so
        /// no world's Nodes move because this method replaced the multiply.
        /// </remarks>
        public int EastOf(int column) =>
            OriginEast + _lattice.EdgeOf(_lineEast + column) - _lattice.EdgeOf(_lineEast);

        /// <summary>The Tile row <paramref name="row"/>'s line stands on.</summary>
        public int NorthOf(int row) =>
            OriginNorth + _lattice.EdgeOf(_lineNorth + row) - _lattice.EdgeOf(_lineNorth);

        /// <summary>How wide the block east of column <paramref name="column"/> is.</summary>
        public int WideAt(int column) => EastOf(column + 1) - EastOf(column);

        /// <summary>How deep the block north of row <paramref name="row"/> is.</summary>
        public int DeepAt(int row) => NorthOf(row + 1) - NorthOf(row);

        /// <summary>This lattice's column holding <paramref name="east"/>.</summary>
        public int ColumnAt(int east) =>
            _lattice.LineAt(east - OriginEast + _lattice.EdgeOf(_lineEast)) - _lineEast;

        /// <summary>This lattice's row holding <paramref name="north"/>.</summary>
        public int RowAt(int north) =>
            _lattice.LineAt(north - OriginNorth + _lattice.EdgeOf(_lineNorth)) - _lineNorth;

        /// <summary>Column <paramref name="column"/>'s line, measured from this lattice's origin.</summary>
        /// <remarks>
        /// <b>The Arterial pass works in lattice-local Tiles</b> — <c>InBounds</c> tests against the
        /// extent and <c>SealRun</c> adds the origin back — so it needs the local form of the same
        /// two questions rather than a second convention.
        /// </remarks>
        public int LocalEast(int column) => EastOf(column) - OriginEast;

        /// <summary>Row <paramref name="row"/>'s line, measured from this lattice's origin.</summary>
        public int LocalNorth(int row) => NorthOf(row) - OriginNorth;

        /// <summary>The column whose line is NEAREST a local Tile, rather than the one holding it.</summary>
        /// <remarks>
        /// <b>A ramp reaches the nearest intersection and not the one south-west of it</b>, which is
        /// why this rounds where <see cref="ColumnAt"/> floors. ⚠ <b>Comparing the two neighbours
        /// rather than dividing</b>, because <c>RoundDiv</c> is that comparison written out for a
        /// spacing that does not vary.
        /// </remarks>
        public int NearestColumn(int localEast)
        {
            int column = ColumnAt(OriginEast + localEast);

            return localEast - LocalEast(column) > LocalEast(column + 1) - localEast
                ? column + 1
                : column;
        }

        /// <summary>The row whose line is nearest a local Tile.</summary>
        public int NearestRow(int localNorth)
        {
            int row = RowAt(OriginNorth + localNorth);

            return localNorth - LocalNorth(row) > LocalNorth(row + 1) - localNorth
                ? row + 1
                : row;
        }


        /// <summary>How far the laid Nodes reach from the origin, which is not the authored extent.</summary>
        /// <remarks>
        /// <b>The grid is floored, so the far Node stops at or before the extent.</b> An overlap test
        /// against the authored extent would refuse worlds whose Nodes never touch.
        /// </remarks>
        private int Span
        {
            get
            {
                int east = EastOf(_grid - 1) - OriginEast;
                int north = NorthOf(_grid - 1) - OriginNorth;

                // ⚠ THE WIDER OF THE TWO, because Reach and Overlaps are one number applied to both
                // axes and this is a refusal test -- understating it lets two lattices contend for
                // ground. Equal on an evenly spaced lattice, which is why nothing moves here.
                return east > north ? east : north;
            }
        }

        /// <summary>
        /// How far this lattice's ground reaches — <b>the Nodes plus one block</b>.
        /// </summary>
        /// <remarks>
        /// ⚠ <b>The extra block is the Lots and not the roads, and leaving it out made the world go
        /// quietly lopsided rather than loudly wrong.</b> The block beyond a lattice's east edge has
        /// that edge's Segments as its west face, so it carries Lots
        /// (<c>SyntheticCity.Subdivide</c>). Two lattices whose Nodes clear each other by less than a
        /// block therefore contend for Lots without overlapping, and the first one to walk them takes
        /// what the second needed: measured at 344,000 Citizens on <c>twinned.toml</c>, the split went
        /// <b>20,545 / 20,736</b> where it is 20,641 / 20,640 by construction. ***A refusal drawn at
        /// the roads is drawn at the wrong thing, because it is the LAND that is contended.***
        /// </remarks>
        // ⚠ A block as a LENGTH and not a particular block's ground -- an overlap margin, which is
        // BlockLattice.Nominal's own distinction.
        private int Reach => Span + _lattice.Nominal;

        /// <summary>Whether this lattice stands on any of <paramref name="other"/>'s ground.</summary>
        public bool Overlaps(Layout other) =>
            OriginEast <= other.OriginEast + other.Reach
            && other.OriginEast <= OriginEast + Reach
            && OriginNorth <= other.OriginNorth + other.Reach
            && other.OriginNorth <= OriginNorth + Reach;

        /// <summary>
        /// This lattice's Node nearest a Tile, with the Tile it stands on — the corridor's endpoint.
        /// </summary>
        /// <remarks>
        /// <b>Clamped rather than searched.</b> The Nodes are a regular grid, so the nearest one is
        /// arithmetic; a search would be the same answer at the cost of a walk over every Node in the
        /// lattice, on a path that runs once per pair at world creation.
        /// </remarks>
        public (Handle<RoadNode> Node, int East, int North) NearestTo(int east, int north)
        {
            int column = Clamp(ColumnAt(east), 0, _grid - 1);
            int row = Clamp(RowAt(north), 0, _grid - 1);

            return (Intersection(column, row), EastOf(column), NorthOf(row));
        }

        /// <summary>
        /// Grid intersections, by <c>(row × grid) + column</c>. Held so severance can address a
        /// Street Segment by position rather than by searching for one.
        /// </summary>
        private Handle<RoadNode>[] _intersections = [];

        /// <summary>
        /// Every Street Segment in laying order, so that <see cref="MarkSevered"/> can name one by
        /// grid position. Nulled by severance, which is what makes deletion a decision not to keep.
        /// </summary>
        private Handle<RoadSegment>[] _streets = [];

        private bool[] _severed = [];

        public void Run()
        {
            LayStreets();
            LayArterials();
            ApplySeverance();
            LayFootPaths();
        }

        // --- Streets -------------------------------------------------------------------------

        /// <summary>
        /// The grid-snapped Streets — <c>CONTEXT.md</c> → Street: <i>"the Road Graph falls out of the
        /// Tile grid directly"</i>. Laid in a fixed order, every horizontal edge then every vertical
        /// one, because severance addresses them by index.
        /// </summary>
        private void LayStreets()
        {
            _intersections = new Handle<RoadNode>[_grid * _grid];

            for (int north = 0; north < _grid; north++)
            {
                for (int east = 0; east < _grid; east++)
                {
                    _intersections[(north * _grid) + east] = graph.Nodes.Create(
                        new Tiles(EastOf(east)), new Tiles(NorthOf(north)));
                }
            }

            _streets = new Handle<RoadSegment>[2 * _grid * (_grid - 1)];
            _severed = new bool[_streets.Length];

            // 🔴 A LENGTH PER SEGMENT AND NOT ONE FOR THE LATTICE. It was hoisted out of both loops
            // as `var length = new Tiles(roads.BlockTiles)`, which is the assumption row 25 removes:
            // a Street's length is the ground between two lines and those need not be equal.
            for (int north = 0; north < _grid; north++)
            {
                for (int east = 0; east < _grid - 1; east++)
                {
                    _streets[Horizontal(east, north)] = graph.Segments.Create(
                        Intersection(east, north),
                        Intersection(east + 1, north),
                        new Tiles(WideAt(east)),
                        RoadKind.Street,
                        TravelMode.Any,
                        TravelMode.Any);

                    SealRun(
                        EastOf(east), NorthOf(north),
                        EastOf(east + 1), NorthOf(north),
                        WideAt(east));
                }
            }

            for (int east = 0; east < _grid; east++)
            {
                for (int north = 0; north < _grid - 1; north++)
                {
                    _streets[Vertical(east, north)] = graph.Segments.Create(
                        Intersection(east, north),
                        Intersection(east, north + 1),
                        new Tiles(DeepAt(north)),
                        RoadKind.Street,
                        TravelMode.Any,
                        TravelMode.Any);

                    SealRun(
                        EastOf(east), NorthOf(north),
                        EastOf(east), NorthOf(north + 1),
                        DeepAt(north));
                }
            }
        }

        private Handle<RoadNode> Intersection(int east, int north) =>
            _intersections[(north * _grid) + east];

        private int Horizontal(int east, int north) => (north * (_grid - 1)) + east;

        private int Vertical(int east, int north) =>
            (_grid * (_grid - 1)) + (east * (_grid - 1)) + north;

        // --- Arterials -----------------------------------------------------------------------

        private void LayArterials()
        {
            for (int arterial = 0; arterial < roads.ArterialCount; arterial++)
            {
                WalkArterial((ulong)arterial);
            }
        }

        /// <summary>
        /// Walks one Arterial as a polyline at one-Tile steps with a gentle hashed curvature, placing
        /// a Junction piece at a fixed interval of <b>arc length</b> along it.
        /// </summary>
        /// <remarks>
        /// <b>The Segment's length is the arc length, not the distance between its Junctions</b>, and
        /// the gap is deliberate. A distance heuristic therefore underestimates on these Segments,
        /// which is the safe direction; what is <em>not</em> safe is the other effect this same loop
        /// produces — an Arterial at an arbitrary angle offers a diagonal shortcut across a grid where
        /// Manhattan says there is none, which is the case that makes a tight heuristic inadmissible.
        /// Both live here, and it is 5c's problem rather than this slice's.
        /// </remarks>
        private void WalkArterial(ulong arterial)
        {
            (int east, int north, int headingEast, int headingNorth) = Entry(arterial);

            Handle<RoadNode> previous = default;
            bool anchored = false;
            int sinceJunction = 0;
            int step = 0;

            (int stepEast, int stepNorth) = UnitStep(headingEast, headingNorth);

            int tileEast = Fixed.ToIntFloor(east);
            int tileNorth = Fixed.ToIntFloor(north);

            while (InBounds(tileEast, tileNorth))
            {
                SealTile(OriginEast + tileEast, OriginNorth + tileNorth);

                // A Junction piece every so many Tiles of arc length. The first anchors the
                // Arterial's first Segment; before it there is road with no graph edge, which is
                // correct — an Arterial with no Junction on it is unreachable, and that is what a
                // limited-access road is.
                if (!anchored || sinceJunction >= roads.ArterialJunctionTiles)
                {
                    Handle<RoadNode> junction = graph.Nodes.Create(
                        new Tiles(OriginEast + tileEast), new Tiles(OriginNorth + tileNorth));

                    ConnectToGrid(junction, tileEast, tileNorth);

                    if (anchored)
                    {
                        graph.Segments.Create(
                            previous,
                            junction,
                            new Tiles(sinceJunction * StepTiles),
                            RoadKind.Arterial,
                            TravelMode.Car,
                            TravelMode.Car);
                    }

                    previous = junction;
                    anchored = true;
                    sinceJunction = 0;
                }

                if (step > 0 && (step % CurvatureEverySteps) == 0)
                {
                    headingEast += Bend(arterial, (ulong)step * 2);
                    headingNorth += Bend(arterial, ((ulong)step * 2) + 1);
                    (stepEast, stepNorth) = UnitStep(headingEast, headingNorth);
                }

                east += stepEast;
                north += stepNorth;
                step++;
                sinceJunction++;

                int nextEast = Fixed.ToIntFloor(east);
                int nextNorth = Fixed.ToIntFloor(north);

                MarkSevered(tileEast, tileNorth, nextEast, nextNorth);

                tileEast = nextEast;
                tileNorth = nextNorth;
            }
        }

        /// <summary>A curvature perturbation in [-60, +60], drawn on its own counter.</summary>
        private int Bend(ulong arterial, ulong counter) =>
            (int)(Draw(arterial, counter, PurposeTag.RoadArterialCurvature) % 121ul) - 60;

        /// <summary>
        /// Where an Arterial enters the map and which way it is pointing. It starts on an edge and
        /// heads inward, so every Arterial genuinely crosses the city rather than stubbing off it.
        /// </summary>
        private (int East, int North, int HeadingEast, int HeadingNorth) Entry(ulong arterial)
        {
            int edge = (int)(Draw(arterial, 0, PurposeTag.RoadArterialOrigin) % 4ul);
            int along = (int)(Draw(arterial, 1, PurposeTag.RoadArterialOrigin)
                % (ulong)extentTiles);

            // The inward component is strictly positive and the cross component is free, which is
            // what makes the angle arbitrary rather than one of eight.
            int inward = 300 + (int)(Draw(arterial, 0, PurposeTag.RoadArterialHeading) % 701ul);
            int across = (int)(Draw(arterial, 1, PurposeTag.RoadArterialHeading) % 2001ul) - 1000;

            int far = extentTiles - 1;

            return edge switch
            {
                0 => (Fixed.FromInt(0), Fixed.FromInt(along), inward, across),
                1 => (Fixed.FromInt(far), Fixed.FromInt(along), -inward, across),
                2 => (Fixed.FromInt(along), Fixed.FromInt(0), across, inward),
                _ => (Fixed.FromInt(along), Fixed.FromInt(far), across, -inward),
            };
        }

        /// <summary>
        /// A Q16.16 step vector of length one Tile in the given direction. <b>No trigonometry and no
        /// floating point</b> — a direction is an integer vector, and the step is that vector scaled
        /// by its own magnitude.
        /// </summary>
        private static (int East, int North) UnitStep(int headingEast, int headingNorth)
        {
            long magnitude = IntegerMath.SqrtFloor(
                ((long)headingEast * headingEast) + ((long)headingNorth * headingNorth));

            if (magnitude == 0)
            {
                return (Fixed.One, 0);
            }

            return (
                (int)IntegerMath.FloorDiv((long)headingEast * Fixed.One * StepTiles, magnitude),
                (int)IntegerMath.FloorDiv((long)headingNorth * Fixed.One * StepTiles, magnitude));
        }

        private bool InBounds(int east, int north) =>
            east >= 0 && north >= 0 && east < extentTiles && north < extentTiles;

        /// <summary>
        /// Records every Street Segment the Arterial ran over between two consecutive polyline
        /// positions. The step is one Tile, so at most one grid line of each orientation can be
        /// crossed and neither can be skipped.
        /// </summary>
        private void MarkSevered(int fromEast, int fromNorth, int toEast, int toNorth)
        {
            int fromColumn = ColumnAt(OriginEast + fromEast);
            int toColumn = ColumnAt(OriginEast + toEast);
            int fromRow = RowAt(OriginNorth + fromNorth);
            int toRow = RowAt(OriginNorth + toNorth);

            if (fromColumn != toColumn)
            {
                int column = fromColumn > toColumn ? fromColumn : toColumn;

                if (column >= 0 && column < _grid && fromRow >= 0 && fromRow < _grid - 1)
                {
                    _severed[Vertical(column, fromRow)] = true;
                }
            }

            if (fromRow != toRow)
            {
                int row = fromRow > toRow ? fromRow : toRow;

                if (row >= 0 && row < _grid && fromColumn >= 0 && fromColumn < _grid - 1)
                {
                    _severed[Horizontal(fromColumn, row)] = true;
                }
            }
        }

        /// <summary>
        /// The authored Junction piece's connection to the Street network — a ramp to the nearest
        /// grid intersection.
        /// </summary>
        /// <remarks>
        /// <b>The ramp carries cars and not pedestrians</b>, which is a modelling choice worth stating
        /// rather than a detail. A pedestrian's ability to get from one side of an Arterial to the
        /// other is then a property of the crossings alone, which is exactly the quantity
        /// <c>[roads] foot_crossing_every</c> exists to move. Letting the ramp carry feet would put a
        /// pedestrian route at every Junction and quietly make Severance unmeasurable.
        /// </remarks>
        private void ConnectToGrid(Handle<RoadNode> junction, int tileEast, int tileNorth)
        {
            int column = Clamp(NearestColumn(tileEast), 0, _grid - 1);
            int row = Clamp(NearestRow(tileNorth), 0, _grid - 1);

            int length = Distance(tileEast, tileNorth, LocalEast(column), LocalNorth(row));

            graph.Segments.Create(
                junction,
                Intersection(column, row),
                new Tiles(length <= 0 ? StepTiles : length),
                RoadKind.Arterial,
                TravelMode.Car,
                TravelMode.Car);

            // A straight ramp between two Tile positions this method computed, so it seals
            // exactly despite not being axis-aligned.
            SealRun(
                OriginEast + tileEast,
                OriginNorth + tileNorth,
                EastOf(column), NorthOf(row),
                length <= 0 ? StepTiles : length);
        }

        private static int Clamp(int value, int low, int high) =>
            value < low ? low : value > high ? high : value;

        /// <summary>The Cell a road Tile Seals into, clamped to the map.</summary>
        /// <remarks>
        /// <b>The clamp is a fencepost and not a safety net.</b> A lattice paved to the boundary has
        /// its far Node at Tile <c>CellGrid.WorldTiles</c> — one past the last Tile, because N Tiles
        /// need N+1 grid lines — so the east and north edge Streets start on a coordinate that has no
        /// Cell. The road is real and the ground it covers is the last Cell's, which is what this
        /// returns. Without it a world with a door threw out of <c>LayStreets</c>.
        /// </remarks>
        private static Cells SealCell(int tiles) =>
            CellGrid.ToCellsClamped(new Tiles(tiles));

        /// <summary>
        /// Seals a straight run of road, attributing its Tiles to the Cells it actually crosses.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Called as the road is laid, because that is the only moment its geometry exists.</b>
        /// Nothing stores a Segment's covered Tiles and there is no Segment to Cell helper; both are
        /// deliberate, and neither matters here because the writer is holding the two endpoints when
        /// it calls this.
        /// </para>
        /// <para>
        /// <b>The attribution is per Cell crossed and NOT split between the endpoints' two
        /// Cells.</b> <see cref="MapLayers.Seal"/> writes one Cell and a Segment is never in one
        /// Cell -- a Street runs Cell boundary to Cell boundary at the shipped
        /// <c>block_tiles</c>, and <c>rulesets/severance.toml</c>'s 256 spans eight. Splitting
        /// between the endpoints would put half a Segment's Tiles into each end and <em>nothing</em>
        /// into the Cells the road runs through, which is a different quantity rather than an
        /// approximation of this one.
        /// </para>
        /// <para>
        /// The walk accumulates and flushes on a Cell change, so a run costs one
        /// <see cref="MapLayers.Seal"/> per Cell crossed rather than one per Tile.
        /// </para>
        /// </remarks>
        private void SealRun(
            int fromEastTiles, int fromNorthTiles, int toEastTiles, int toNorthTiles, int lengthTiles)
        {
            if (layers is null || lengthTiles <= 0)
            {
                return;
            }

            int deltaEast = toEastTiles - fromEastTiles;
            int deltaNorth = toNorthTiles - fromNorthTiles;

            Cells runEast = SealCell(fromEastTiles);
            Cells runNorth = SealCell(fromNorthTiles);
            int run = 0;

            for (int step = 0; step < lengthTiles; step++)
            {
                Cells cellEast = SealCell(
                    fromEastTiles + IntegerMath.FloorDiv(deltaEast * step, lengthTiles));
                Cells cellNorth = SealCell(
                    fromNorthTiles + IntegerMath.FloorDiv(deltaNorth * step, lengthTiles));

                if (cellEast != runEast || cellNorth != runNorth)
                {
                    layers.Seal(runEast, runNorth, run);

                    runEast = cellEast;
                    runNorth = cellNorth;
                    run = 0;
                }

                run++;
            }

            layers.Seal(runEast, runNorth, run);
        }

        /// <summary>Seals the single Tile an Arterial's polyline walk is standing on.</summary>
        /// <remarks>
        /// <b>The curved Arterial is the one Segment whose path cannot be recovered from its
        /// endpoints</b> -- its <c>LengthTiles</c> is arc length rather than the straight line, which
        /// <see cref="RoadSegmentTable"/> states and explains. It is also the one the generator walks
        /// a Tile at a time, so it is sealed here exactly and never reconstructed. <b>This also
        /// catches the run before the first Junction anchors a Segment</b>, which is pavement with no
        /// graph edge and which a per-Segment rule would miss.
        /// </remarks>
        private void SealTile(int eastTiles, int northTiles) =>
            layers?.Seal(SealCell(eastTiles), SealCell(northTiles), StepTiles);

        /// <summary>Straight-line Tile distance, floored — which underestimates, the safe direction.</summary>
        private static int Distance(int fromEast, int fromNorth, int toEast, int toNorth)
        {
            long east = (long)fromEast - toEast;
            long north = (long)fromNorth - toNorth;

            return IntegerMath.SqrtFloor((east * east) + (north * north));
        }

        // --- Severance -----------------------------------------------------------------------

        /// <summary>
        /// Frees the Street Segments the Arterials ran over, keeping every
        /// <c>[roads] foot_crossing_every</c>th one as a pedestrian crossing.
        /// </summary>
        /// <remarks>
        /// <b>A kept crossing becomes a <see cref="RoadKind.FootPath"/> and loses
        /// <see cref="TravelMode.Car"/>.</b> Changing its kind rather than only its mask is what makes
        /// its free-flow speed and capacity follow walking pace — those are derived from the kind
        /// (<c>adr/0064</c>), so a crossing that kept <see cref="RoadKind.Street"/> would be a
        /// footbridge cars drive at 50 km/h down.
        /// </remarks>
        private void ApplySeverance()
        {
            int severedSoFar = 0;

            for (int street = 0; street < _streets.Length; street++)
            {
                if (!_severed[street])
                {
                    continue;
                }

                severedSoFar++;

                int slot = graph.Segments.Rows.Resolve(_streets[street]);

                if (roads.FootCrossingEvery > 0
                    && severedSoFar % roads.FootCrossingEvery == 0)
                {
                    graph.Segments.Kind[slot] = (byte)RoadKind.FootPath;
                    graph.Segments.ModesForward[slot] = (byte)TravelMode.Foot;
                    graph.Segments.ModesBackward[slot] = (byte)TravelMode.Foot;
                    graph.Segments.Edited(slot);
                    continue;
                }

                graph.Segments.Rows.Free(_streets[street]);
            }
        }

        // --- Foot-only Segments ----------------------------------------------------------------

        /// <summary>
        /// The block cut-throughs. <c>CONTEXT.md</c> → Segment: the foot-only Segments are <i>"few,
        /// and they are the edges Severance turns on, so nothing may size the graph by omitting
        /// them."</i>
        /// </summary>
        private void LayFootPaths()
        {
            if (roads.FootPathsPerThousandBlocks <= 0)
            {
                return;
            }

            for (int north = 0; north < _grid - 1; north++)
            {
                for (int east = 0; east < _grid - 1; east++)
                {
                    // The lattice index rides in the high half, so two lattices of one size do not
                    // get the same cut-throughs in the same blocks. At index 0 it is the expression
                    // it always was, which is what keeps every existing world's State Hash still.
                    ulong block = (ulong)(((long)index << 32) + (north * _grid) + east);

                    if (Draw(block, 0, PurposeTag.RoadFootPath) % 1000ul
                        >= (ulong)roads.FootPathsPerThousandBlocks)
                    {
                        continue;
                    }

                    // ⚠ THIS BLOCK'S diagonal. It was hoisted above both loops as one length for
                    // the whole lattice, which is only right while every block is the same size.
                    var diagonal = new Tiles(Distance(0, 0, WideAt(east), DeepAt(north)));

                    graph.Segments.Create(
                        Intersection(east, north),
                        Intersection(east + 1, north + 1),
                        diagonal,
                        RoadKind.FootPath,
                        TravelMode.Foot,
                        TravelMode.Foot);

                    // Fully determined despite being a diagonal: it runs corner to corner
                    // across one block, and `diagonal` is that run's floored Euclidean length.
                    // It seals MORE Tiles than a Street of the same block because it is longer,
                    // which is the geometry and not a weighting.
                    SealRun(
                        EastOf(east), NorthOf(north),
                        EastOf(east + 1), NorthOf(north + 1),
                        diagonal.Raw);
                }
            }
        }

        // --- Draws ---------------------------------------------------------------------------

        /// <summary>
        /// One draw, on a counter distinct within its tag.
        /// </summary>
        /// <remarks>
        /// <b>Generation happens at Tick zero and the entity coordinate carries the counter</b>, so
        /// two draws of one Arterial differ in the coordinate <see cref="Randomness.Draw"/> mixes
        /// first rather than in the one it mixes last. Mixing the pair rather than adding them keeps
        /// <c>(arterial 1, counter 0)</c> and <c>(arterial 0, counter 1)</c> apart, which adding would
        /// not.
        /// </remarks>
        private ulong Draw(ulong entity, ulong counter, PurposeTag purpose) =>
            Randomness.Draw(
                key, Randomness.Mix(entity ^ (counter << 32)), Ticks.Zero, purpose);
    }
}
