using System;
using System.Globalization;
using System.IO;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

// ---- the readout and the hover -- what the shell SAYS about the city, and how large it says it 
//
// Aim() and NearestIn() are here rather than with the verbs because picking is a question the
// hover asks on every frame and a click asks once: the reading is the common case.
//
// The typography sits here for the same reason. Retype() also sizes the panels' shared Theme,
// so it straddles this seam and Main.Panels.cs -- five of its seven references are Labels this
// file writes, which is the whole argument for the file it is in.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    // ---- picking ---------------------------------------------------------------------------------

    /// <summary>
    /// Where the cursor meets the ground, in Tiles — <b>the shell's first screen-to-world query, and
    /// every player verb is blocked on it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A ray against the ground plane and nothing more.</b> Godot's physics picking would want
    /// collision shapes on 262,144 MultiMesh instances, which is a body per box for a question that
    /// is one division — and <c>05 §2</c>'s boundary says the shell reads the world rather than
    /// modelling it twice.
    /// </para>
    /// <para>
    /// ⚠ <b>The horizon is REFUSED rather than clamped.</b> A ray that barely descends does meet the
    /// plane, thousands of Tiles away and behind the visible city, so clamping would hand back a
    /// confident answer about somewhere nobody is looking. ***A pick that cannot fail is a pick
    /// nobody can trust***, which is why this returns null and the readout says so.
    /// </para>
    /// <para>
    /// ⚠ <b>It answers in Tiles and the city is what resolves them.</b> The Building boxes on screen
    /// are the renderer's own invention — a frontage and a depth composed here, from a Lot that has
    /// neither (<c>adr/0078</c>) — so picking against the drawn geometry would pick the fiction.
    /// <see cref="Pointing"/> takes the Tile to the Cell and asks
    /// <see cref="BuildingResidency.In"/>, which is the query the simulation already uses.
    /// </para>
    /// </remarks>
    /// <returns>The Tile under the cursor, or <c>null</c> for the sky and for off-map ground.</returns>
    private (Tiles East, Tiles North)? Aim(Vector2? screen = null)
    {
        // A driven run names a Tile rather than a pixel, so there is no ray to cast: plans/0048's
        // whole finding is that a wall-clock channel addresses moments and the city is addressed in
        // Ticks, and the same is true of a place -- a screen position is a property of the camera.
        if (_aimed is { } driven)
        {
            return driven;
        }

        Vector2 at = screen ?? GetViewport().GetMousePosition();
        Vector3 from = _camera.ProjectRayOrigin(at);
        Vector3 along = _camera.ProjectRayNormal(at);

        if (along.Y > -0.001f)
        {
            return null;
        }

        float toGround = -from.Y / along.Y;
        float east = (from.X + (along.X * toGround)) / MetresPerTile;
        float north = -(from.Z + (along.Z * toGround)) / MetresPerTile;

        if (east < 0f || north < 0f
            || east >= CellGrid.WorldTiles || north >= CellGrid.WorldTiles)
        {
            return null;
        }

        return (new Tiles((int)east), new Tiles((int)north));
    }

    /// <summary>
    /// Everything the city knows about the Tile under the cursor, <b>stacked most specific first.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every string here is the shell's</b> (<c>adr/0002</c>): the core hands back a kind id and
    /// this resolves it through <see cref="RulesetNames"/>. ***That is the leak vector
    /// <c>CLAUDE.md</c> actually names*** — not <c>using Godot;</c>, but a core method that returns a
    /// formatted string because a panel wanted one.
    /// </para>
    /// <para>
    /// 🔴 <b>A LINE IS OMITTED WHEN THE THING IT WOULD DESCRIBE HAS NO ROW, AND THAT IS THE WHOLE
    /// DESIGN OF THIS PANEL.</b> Nine of the shipped worlds have no Layer row anywhere, no District
    /// and no water, so a fixed template would print <c>pollution 0 · land value 0 · district 0</c>
    /// over every Tile of every one of them. ***Zero and absent are different answers, and a panel
    /// that renders them identically teaches a city has a quantity when it has no mechanism.***
    /// A Cell with no Layer row says nothing about pollution rather than saying none.
    /// </para>
    /// <para>
    /// ⚠ <b>Terrain is the one exception and it is stated rather than hidden.</b>
    /// <see cref="TerrainCellTable"/> is dense — one byte a Cell, on every world — so a file with no
    /// <c>[[terrain]]</c> reads <c>ordinary</c> everywhere truthfully. It is a uniform answer and not
    /// a missing one.
    /// </para>
    /// <para>
    /// ⚠ <b>Nearest within the Cell, and the Cell is the whole search.</b> A Lot is a point on a
    /// Segment rather than a plot of ground, so *containment* is not a question the city can answer —
    /// what it can answer is which Buildings are resident in a Cell, which is
    /// <see cref="BuildingResidency"/>'s own index. 🔴 <b>A Cell is 128 m and covers about four
    /// frontages</b>, so this is honest for a hover and too coarse for a verb that names one
    /// Building: the distance is printed so a person can see when the answer is a neighbour's.
    /// </para>
    /// </remarks>
    private string Pointing()
    {
        if (Aim() is not { } at)
        {
            return "— pointing off the map —";
        }

        Cells east = CellGrid.ToCells(at.East);
        Cells north = CellGrid.ToCells(at.North);
        var said = new System.Collections.Generic.List<string>
        {
            $"Tile ({at.East.Raw:N0}, {at.North.Raw:N0})    Cell ({east.Raw}, {north.Raw})",
        };

        Built(said, at, east, north);
        Underfoot(said, east, north);

        if (_verb == Verb.Zone)
        {
            Virgin(said, at);
        }

        if (_verb == Verb.Connect)
        {
            Aiming(said, at);
        }

        if (_verb == Verb.Service)
        {
            int lot = VacantNear(at);

            said.Add(lot == Rows.NoSlot
                ? "no vacant Lot in this Cell — a shell is not vacant, demolish first"
                : $"would raise on Lot {_world.Lots.Rows.IdAt(lot):N0} at "
                    + $"({_world.Lots.East[lot].Raw:N0}, {_world.Lots.North[lot].Raw:N0})");
        }

        return string.Join('\n', said);
    }

    /// <summary>
    /// The Building nearest a Tile within its own Cell, with the Lot it stands on and the square of
    /// the distance.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Shared by the hover and by <see cref="Act"/>, and that sharing is the point.</b> A verb
    /// must act on the thing the panel named — <c>Demolish</c>'s own refusal says
    /// <em>"a mistyped command must not be indistinguishable from the demolition somebody meant"</em>
    /// — so the two must not resolve a click twice and risk disagreeing.
    /// </remarks>
    private (int Building, int Lot, long Away) NearestIn(
        (Tiles East, Tiles North) at, Cells east, Cells north)
    {
        Span<int> found = stackalloc int[64];
        int count = _world.BuildingsInCells.In(
            CellRect.At(east, north), _world.Buildings, found);

        LotTable lots = _world.Lots;
        int nearest = Rows.NoSlot;
        int onLot = Rows.NoSlot;
        long best = long.MaxValue;

        for (int seen = 0; seen < count; seen++)
        {
            if (!lots.Rows.TryResolve(_world.Buildings.Lot[found[seen]], out int lot))
            {
                continue;
            }

            long de = lots.East[lot].Raw - at.East.Raw;
            long dn = lots.North[lot].Raw - at.North.Raw;
            long away = (de * de) + (dn * dn);

            if (away < best)
            {
                best = away;
                nearest = found[seen];
                onLot = lot;
            }
        }

        return (nearest, onLot, best);
    }

    /// <summary>The Building nearest the cursor inside its Cell, its tenants and its trades.</summary>
    private void Built(
        System.Collections.Generic.List<string> said,
        (Tiles East, Tiles North) at,
        Cells east,
        Cells north)
    {
        (int nearest, int onLot, long best) = NearestIn(at, east, north);
        LotTable lots = _world.Lots;

        if (nearest == Rows.NoSlot)
        {
            said.Add("open ground — no Building in this Cell");

            return;
        }

        byte kind = _world.Buildings.Kind[nearest];
        string named = _names.Kind(kind) ?? $"kind {kind}";
        int room = _world.DeclaredOccupancy(nearest);
        int held = _world.Occupants.Length(nearest);
        int trades = _world.BuildingBusinesses.Length(nearest);

        // ⚠ THE CEILING COUNTS TENANTS OF ANY KIND (adr/0147), so a shop occupies one of the
        // declared occupants and the households line must not be read against the whole number.
        said.Add(_world.Buildings.IsAbandoned(nearest)
            ? $"{named} — ABANDONED, a shell standing on its collapse clock"
            : $"{named} — {held} of {room} occupied, {Math.Sqrt(best):N0} Tiles off");

        for (int business = _world.BuildingBusinesses.PeekFront(nearest);
             business != Rows.NoSlot && trades > 0;
             business = _world.Businesses.BuildingNext[business] - 1)
        {
            byte trade = _world.Businesses.Kind[business];

            said.Add($"    {_names.BusinessKind(trade) ?? $"trade {trade}"}");

            if (--trades == 0)
            {
                break;
            }
        }

        said.Add($"Lot {lots.Rows.IdAt(onLot):N0}, zone 0x{lots.Zone[onLot]:X4}");
    }

    /// <summary>What the ground under the cursor is, and only what it actually has a row for.</summary>
    private void Underfoot(
        System.Collections.Generic.List<string> said, Cells east, Cells north)
    {
        said.Add($"ground: {_world.Layers.Terrain.At(east, north).ToString().ToLowerInvariant()}");

        if (_world.WaterInCells.IsWet(east, north))
        {
            said.Add("under a Water Body");
        }

        int risk = _world.FloodInCells.DepthAt(_world.Flood, east, north);

        if (risk > 0)
        {
            // ⚠ A DEPTH IS THE FLOOD LEVEL MINUS THE GROUND, so a LARGE one is LOW ground. Saying
            // "below the flood line" rather than printing the number keeps the polarity legible.
            // ⚠ NOT THE SAME FACT AS THE TERRAIN LINE ABOVE, however alike they read.
            // TerrainKind.Floodplain is a quantile of a noise field (TerrainGenerator); this is the
            // Hazard Region, which is the water generator's flood level against the height field.
            // ***Two mechanisms that share a word***, so the wording has to separate them.
            said.Add($"AT FLOOD RISK — {risk:N0} below the flood line");
        }

        int layer = _world.Layers.Residency.Slot(east, north);

        if (layer != CellResidency.NotResident)
        {
            LayerCellTable cells = _world.Layers.Cells;

            said.Add(
                $"pollution {cells.Pollution[layer]:N0}    "
                + $"land value {cells.LandValue[layer]:N0}    "
                + $"sealing {cells.Sealing[layer]:N0}");
        }

        // 🔴 THROUGH `Of` AND NOT THROUGH `Slot`, AND THE TWO RETURN SLOTS IN DIFFERENT TABLES.
        // DistrictResidency.Slot answers with a row of the MEMBERSHIP table -- one row per Cell in
        // a District, of which there are thousands -- and this line was handing it to the DISTRICT
        // table, of which there are two. It threw IndexOutOfRangeException on the first frame the
        // cursor rested on a District Cell, once a frame, for ever. ⚠ NO WORLD COULD REACH IT until
        // rulesets/pictured.toml: a Ruleset must state [districts] for the index to hold anything,
        // three shipped files do, and nobody had pointed the shell at one of them.
        Handle<District> district = _world.DistrictsInCells.Of(_world.DistrictCells, east, north);

        if (_world.Districts.Rows.TryResolve(district, out int seat))
        {
            said.Add($"District {_world.Districts.Rows.IdAt(seat):N0}");
        }
    }

    /// <summary>
    /// Which Segment a Street click would lay or bulldoze, <b>and whether one is already there.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE STREET TOOL WAS THE ONE VERB THAT CHANGED THE GROUND WITH NOTHING TO HOVER.</b>
    /// <see cref="Virgin"/> gives <c>Zone</c> a line, <see cref="Raise"/>'s Lot line gives
    /// <c>Service</c> one and <see cref="Built"/> gives <c>Demolish</c> the Building it will
    /// actually clear — and <c>Connect</c> got the Tile coordinate, which is where the cursor is and
    /// not what the click does. ***A verb you cannot aim is a verb you cannot test***, and until
    /// this line existed the only way to know which edge a click had chosen was to click and look.
    /// </para>
    /// <para>
    /// ⚠ <b>It resolves the aim through <see cref="StreetGrid.NearestEdge"/>, which is what
    /// <see cref="Lay"/> sends</b> — the same sharing <see cref="NearestIn"/> has with
    /// <see cref="Clear"/>, and for the same reason: ***a panel that named one edge while the verb
    /// acted on another would be worse than no panel at all.***
    /// </para>
    /// <para>
    /// ⚠ <b>Both outcomes of a plain click are stated, because one of them is nothing.</b>
    /// <c>RoadGraph.LayStreet</c> returns <c>false</c> on an edge that already carries a Street and
    /// <c>BulldozeStreet</c> returns it on one that carries none, and <c>ApplyConnect</c> treats
    /// either as a Tick with no edit rather than as a refusal — so there is no sentence in
    /// <see cref="Sentence"/> to reach and this is the only place a person can be told.
    /// </para>
    /// </remarks>
    private void Aiming(System.Collections.Generic.List<string> said, (Tiles East, Tiles North) at)
    {
        StreetGrid streets = _world.Roads.Streets;


        if (streets.BlockTiles <= 0)
        {
            said.Add("no Street lattice in this world — this Ruleset states no [roads] block_tiles");

            return;
        }

        (int column, int row, StreetAxis axis) = streets.NearestEdge(at.East, at.North);
        (Tiles east, Tiles north) = streets.IntersectionTile(column, row);
        int segment = streets.SegmentOn(column, row, axis);
        string edge = $"the edge running {(axis == StreetAxis.East ? "EAST" : "NORTH")} from "
            + $"({east.Raw:N0}, {north.Raw:N0})";

        said.Add(segment == Rows.NoSlot
            ? $"click LAYS a Street on {edge} — nothing stands there, so shift-click does nothing"
            : $"shift-click BULLDOZES Segment {_world.Roads.Segments.Rows.IdAt(segment):N0} on "
                + $"{edge} — a plain click does nothing, it is already built");

        Crossing(said, at, streets);
    }

    /// <summary>
    /// The roads through this block that the lattice does not hold — <b>the diagonal a player can
    /// see, named, and said not to be a Street.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE GENERATED WORLD CONTAINS DIAGONALS AND NO TOOL LAYS ONE.</b>
    /// <c>[roads] foot_paths_per_thousand_blocks</c> lays a <see cref="RoadKind.FootPath"/> corner to
    /// corner across a block, which is why <c>--morphology</c> reports six occupied compass bins
    /// where a pure lattice has four. ***A player who has seen a diagonal on screen will reasonably
    /// ask for the tool that made it***, and the honest answer is that no tool did —
    /// <c>plans/0045</c> row 23. Until this line existed the hover talked about an east–north edge
    /// while the cursor sat on the very thing the question was about.
    /// </para>
    /// <para>
    /// ⚠ <b>Through the block's own bucket rather than a walk.</b>
    /// <see cref="StreetGrid.OffLatticeHead"/> buckets each off-lattice Segment at the block of its
    /// first endpoint, so this is a short chain and not the <c>O(Lots)</c> scan
    /// <see cref="Raise"/> is stuck with. ⚠ <b>A Segment can reach out of its bucket</b>
    /// (<see cref="StreetGrid.OffLatticeReachBlocks"/>), so what this reports is what <em>starts</em>
    /// here — enough to answer <em>what is that</em>, and not a claim about everything crossing the
    /// block.
    /// </para>
    /// </remarks>
    private void Crossing(
        System.Collections.Generic.List<string> said,
        (Tiles East, Tiles North) at,
        StreetGrid streets)
    {
        int column = streets.Lattice.LineAt(at.East.Raw);
        int row = streets.Lattice.LineAt(at.North.Raw);
        int paths = 0;
        int others = 0;

        for (int slot = streets.OffLatticeHead(column, row);
             slot != Rows.NoSlot;
             slot = streets.OffLatticeNext(slot))
        {
            if ((RoadKind)_world.Roads.Segments.Kind[slot] == RoadKind.FootPath)
            {
                paths++;
            }
            else
            {
                others++;
            }
        }

        if (paths > 0)
        {
            said.Add(paths == 1
                ? "a FOOT PATH cuts this block corner to corner — foot only, laid when the world "
                    + "was made. No tool lays one: a Street runs east or north"
                : $"{paths:N0} FOOT PATHS cut this block — foot only, laid when the world was made. "
                    + "No tool lays one: a Street runs east or north");
        }

        if (others > 0)
        {
            said.Add($"{others:N0} road(s) here are off the lattice — an Arterial is a route rather "
                + "than one click (adr/0077), so no tool lays one either");
        }
    }

    /// <summary>
    /// How much of the block under the cursor is still virgin frontage. <b>Whether a click would do
    /// anything.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>Zone</c>'s commonest misuse is silent, which is why this line exists.</b> A block
    /// whose four faces are all claimed accepts the command, creates nothing and reports nothing —
    /// so the panel counts the faces that are present <em>and</em> unclaimed, which is exactly what
    /// <c>LotSubdivider.Face</c> tests. ***The alternative was a refusal, and a refusal would be
    /// wrong***: subdividing three of four faces is a real edit, and only the zero case is a no-op.
    /// </remarks>
    private void Virgin(System.Collections.Generic.List<string> said, (Tiles East, Tiles North) at)
    {
        StreetGrid streets = _world.Roads.Streets;
        if (_zoneParcels)
        {
            var parcel = System.Linq.Enumerable.FirstOrDefault(SelectedParcels(at));
            said.Add(parcel.Wide.Raw > 0
                ? $"parcel {parcel.Wide.Raw * MetresPerTile:0} × {parcel.Deep.Raw * MetresPerTile:0} m — {ZoneName()}"
                : "choose a parcel beside a Street");
            return;
        }

        if (streets.BlockTiles <= 0)
        {
            said.Add("no Street lattice in this world");

            return;
        }

        int column = streets.Lattice.LineAt(at.East.Raw);
        int row = streets.Lattice.LineAt(at.North.Raw);
        int free = 0;
        int faces = 0;

        // The four faces and the side of each that belongs to THIS block, which is
        // LotSubdivider.SubdivideBlock's own four constants rather than a second derivation.
        ReadOnlySpan<(int Segment, StreetSide Side)> around =
        [
            (streets.Horizontal(column, row), StreetSide.Left),
            (streets.Horizontal(column, row + 1), StreetSide.Right),
            (streets.Vertical(column, row), StreetSide.Right),
            (streets.Vertical(column + 1, row), StreetSide.Left),
        ];

        foreach ((int segment, StreetSide side) in around)
        {
            if (segment == Rows.NoSlot)
            {
                continue;
            }

            faces++;

            if (!_world.Frontage.Claimed(segment, side))
            {
                free++;
            }
        }

        said.Add(free > 0
            ? $"block ({column}, {row}) — {free} of {faces} faces still to subdivide"
            : $"block ({column}, {row}) — nothing to subdivide, a click does nothing");
    }

    /// <summary>The block under the cursor, so a person can see what they are aiming at.</summary>
    /// <remarks>
    /// 🔴 <b>THIS DREW A CELL UNTIL 2026-09-01 AND LOOKED CORRECT FOR AS LONG AS A CELL WAS A
    /// BLOCK.</b> A Cell is 32 Tiles by design and never moves; <c>block_tiles</c> is Ruleset data
    /// and does. They were the same number in every shipped file, so a highlight denominated in the
    /// wrong unit sat exactly on the thing a click acts on and nobody could tell. The moment
    /// <c>block_tiles</c> went to 16 the box covered four blocks.
    /// <para>
    /// ⚠ <b>What a click acts on is the BLOCK</b> — <see cref="Virgin"/> and
    /// <c>LotSubdivider.SubdivideBlock</c> both take <c>FloorDiv(tile, BlockTiles)</c> — so that is
    /// what the box has to be. A world with no lattice keeps the Cell, because there is no block to
    /// draw and the Cell is still an honest unit of ground.
    /// </para>
    /// </remarks>
    private void Cursor()
    {
        _zones.Visible = _verb == Verb.Zone && _washing == Wash.None;
        _plots.Visible = !_zones.Visible;
        if (_verb == Verb.Look || OverInformation(GetViewport().GetMousePosition()))
        {
            _cursor.Multimesh.VisibleInstanceCount = 0;
            return;
        }
        if (Aim() is not { } at)
        {
            _cursor.Multimesh.VisibleInstanceCount = 0;

            return;
        }

        int block = _world.Roads.Streets.BlockTiles;
        if (_verb == Verb.Zone && block > 0)
        {
            ZonePreview(at);
            return;
        }

        // 🔴 THE STREET TOOL GETS THE EDGE AND EVERY OTHER VERB GETS THE BLOCK, because those are
        // the things the two act on. Connect edits ONE lattice edge (adr/0077) and the block was the
        // only thing drawn, so the ghost agreed with the click on which block and said nothing at
        // all about which of its four sides -- the half of row 22 a sentence in the hover cannot fix,
        // since a person aiming is looking at the ground rather than at the panel.
        _cursor.Multimesh.SetInstanceTransform(
            0,
            _verb == Verb.Connect && block > 0
                ? Edge(at, _world.Roads.Streets.Lattice)
                : block > 0
                    ? Block(at, _world.Roads.Streets.Lattice, 0.03f)
                    : Tile(CellGrid.ToCells(at.East), CellGrid.ToCells(at.North), 0.03f));
        _cursor.Multimesh.SetInstanceColor(0, new Color(.95f, .80f, .25f).SrgbToLinear());
        _cursor.Multimesh.VisibleInstanceCount = 1;
    }

    // Shared typography retains each label’s role when the text-size preference changes.
    private void Retype()
    {
        _readout.LabelSettings = new LabelSettings { FontSize = Typed(ReadoutPoints) };
        _hover.LabelSettings = null;
        _hover.AddThemeFontSizeOverride("font_size", Typed(HoverPoints));
        _type.DefaultFontSize = Typed(ControlPoints);
        _type.SetFontSize("font_size", "TooltipLabel", Typed(ControlPoints));
        foreach (Node node in InformationDescendants(_hud))
        {
            if (node is Label label && label.HasMeta("type_points"))
                label.AddThemeFontSizeOverride("font_size", Typed((int)label.GetMeta("type_points")));
            if (node is TextureRect icon && icon.HasMeta("reading_icon"))
            {
                icon.CustomMinimumSize = new Vector2(Typed(24), Typed(24));
                icon.Texture = UiIcons.Texture((string)icon.GetMeta("reading_icon"), size: Typed(24));
            }
            if (node is Button button) UiIcons.Refresh(button);
        }
        LayoutInformation();
    }

    /// <summary>The readout's size, in points.</summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b> under the amnesty's standing order 4 — chosen by taste, no ratifier, no
    /// <c>plans/0002</c> §D row. 🔴 <b>IT WENT TO 20 AND CAME BACK, on the same afternoon.</b> The
    /// player asked for larger, got 20, and said it was too big — <b>32 point on a 1,834-pixel
    /// window</b>. ***The base was never the thing that was wrong***: what inflated it was a window
    /// multiplier <see cref="Typed"/> no longer applies.
    /// </remarks>
    private const int ReadoutPoints = 18;

    /// <summary>The hover panel's size, in points.</summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b>, as <see cref="ReadoutPoints"/>, and it made the same round trip.
    /// <b>It stays two points under the readout</b> so the two panels keep their order of
    /// importance — the city's state is the one you read without looking for it.
    /// </remarks>
    private const int HoverPoints = BodyPoints;

    /// <summary>Every Control panel's size, in points.</summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b>, as <see cref="ReadoutPoints"/>. Godot's own default is <b>16</b> and
    /// this is that number said out loud, so that <see cref="Retype"/> has something to scale. The
    /// tool strip and both Ruleset panels sat at the default while the two Labels beside them grew,
    /// which is how a size nobody stated becomes a size nobody can change. <b>It matches
    /// <see cref="HoverPoints"/> rather than the readout's</b>: these are things you look at when
    /// you want them, which is the hover's standing and not the readout's.
    /// </remarks>
    private const int ControlPoints = BodyPoints;

    /// <summary>
    /// A point size for this window, from the sizes stated above.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE WINDOW NO LONGER BUYS TYPE, AND THAT IS THE DECISION.</b> Sizes were stated at a
    /// 1,080-pixel reference and multiplied by up to 1.3× on a taller window, so <b>100% meant 21
    /// point on the machine it was read on</b> — a text-size control whose own 100% was not the
    /// size anybody had chosen. ***A size the reader did not ask for is not a default***, so the
    /// only multiplier left is the one in Settings.
    /// </remarks>
    private int Typed(int points) => points * _textPercent / 100;

    /// <summary>The panel, which is every string a human reads (<c>adr/0002</c>).</summary>
    private void Readout()
    {
        _readout = new Label
        {
            Position = new Vector2(16f, 12f),
            LabelSettings = new LabelSettings { FontSize = Typed(ReadoutPoints) },

            // ⚠ STATED RATHER THAN INHERITED. A Label defaults to Ignore, so this changes nothing
            // today -- and a verb that stopped working because somebody set a theme's mouse filter
            // would be indistinguishable from the trackpad defect this file just fixed.
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        _hover = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        _hud = new CanvasLayer();
        _type.DefaultFontSize = Typed(ControlPoints);

        // Panel widths and the narrow breakpoint are read off the viewport, so a drag has to
        // re-run the layout even though the type itself no longer depends on the window.
        GetViewport().SizeChanged += LayoutInformation;

        _ruler = new MapRuler { Camera = _camera, MouseFilter = Control.MouseFilterEnum.Ignore };
        _hud.AddChild(_ruler);
        _hud.AddChild(_readout);
        _readout.Visible = false;
        AddChild(_hud);
        Tuner(_hud);

        // ⚠ INFORMATION FIRST, and the order is load-bearing now. Palette() builds into the
        // console's tool slot, and the console is built by Information(). It used to be the other
        // way round because the palette owned its own corner of the screen and nothing owned it.
        Information();
        Panels();
    }
    private static string Weekday(ulong tick) =>
        new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }
            [Borough.Core.Rules.WeeklyHours.DayOf((long)tick)];

    private string ShoppingCaption()
    {
        if (!_world.Rules.Shopping.Runs) { return string.Empty; }
        int outNow = 0;
        long carried = 0;
        int example = -1;
        var shopping = _world.Shopping;
        for (int row = 0; row < shopping.Rows.SlotCount; row++)
        {
            if (!shopping.Rows.IsLive(row) || shopping.Stage[row] == 0) { continue; }
            outNow++;
            carried += shopping.Cargo[row];
            if (shopping.Cargo[row] > 0) { example = row; }
        }
        int atWork = 0;
        for (int citizen = 0; citizen < _world.Citizens.Rows.SlotCount; citizen++)
        {
            if (_world.Citizens.Rows.IsLive(citizen)
                && (Borough.Core.Entities.CitizenActivity)_world.Citizens.Activity[citizen]
                    == Borough.Core.Entities.CitizenActivity.AtWork) { atWork++; }
        }
        string sample = example < 0 ? string.Empty
            : $" — bringing home {shopping.Cargo[example]} {_names.Resource(shopping.Good[example])}";
        return $"\nAt work {atWork:N0}   Shopping {outNow:N0} Households   Goods being carried {carried:N0}{sample}";
    }
}
