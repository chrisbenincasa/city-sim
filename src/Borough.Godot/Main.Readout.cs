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
    private (Tiles East, Tiles North)? Aim()
    {
        // A driven run names a Tile rather than a pixel, so there is no ray to cast: plans/0048's
        // whole finding is that a wall-clock channel addresses moments and the city is addressed in
        // Ticks, and the same is true of a place -- a screen position is a property of the camera.
        if (_aimed is { } driven)
        {
            return driven;
        }

        Vector2 at = GetViewport().GetMousePosition();
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

        if (streets.BlockTiles <= 0)
        {
            said.Add("no Street lattice in this world");

            return;
        }

        int column = IntegerMath.FloorDiv(at.East.Raw, streets.BlockTiles);
        int row = IntegerMath.FloorDiv(at.North.Raw, streets.BlockTiles);
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
        if (Aim() is not { } at)
        {
            _cursor.Multimesh.VisibleInstanceCount = 0;

            return;
        }

        int block = _world.Roads.Streets.BlockTiles;

        _cursor.Multimesh.SetInstanceTransform(
            0,
            block > 0
                ? Block(at, block, 0.03f)
                : Tile(CellGrid.ToCells(at.East), CellGrid.ToCells(at.North), 0.03f));
        _cursor.Multimesh.VisibleInstanceCount = 1;
    }

    /// <summary>Re-states every panel's point size against the window as it now is.</summary>
    /// <remarks>
    /// ⚠ <b>It replaces <see cref="LabelSettings"/> rather than editing it</b>, because the two
    /// Labels are given separate instances and a shared one would tie the hover's size to the
    /// readout's the first time somebody reached for the obvious tidy-up. The Controls are the
    /// other way round on purpose — <b>one shared <see cref="Theme"/></b>, because there is no
    /// reason a Button in the tool strip should ever be a different size from a Button in the
    /// Policy panel, and a shared object is what makes that unsayable rather than merely untrue.
    /// </remarks>
    private void Retype()
    {
        _readout.LabelSettings = new LabelSettings { FontSize = Typed(ReadoutPoints) };
        _hover.LabelSettings = new LabelSettings { FontSize = Typed(HoverPoints) };
        _type.DefaultFontSize = Typed(ControlPoints);
    }

    /// <summary>The readout's size at the reference window, in points.</summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b> under the amnesty's standing order 4 — chosen by taste, no ratifier, no
    /// <c>plans/0002</c> §D row. 🔴 <b>IT WENT TO 20 AND CAME BACK, on the same afternoon.</b> The
    /// player asked for larger, got 20 against a 1.6× ceiling, and said it was too big — <b>32 point
    /// on a 1,834-pixel window</b>. ***The base was never the thing that was wrong***: 18 at the
    /// reference size is the size it always was, and what the window buys it is the ceiling's
    /// business. So the growth moved to <see cref="LargestScale"/> and this came back to 18.
    /// </remarks>
    private const int ReadoutPoints = 18;

    /// <summary>The hover panel's size at the reference window, in points.</summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b>, as <see cref="ReadoutPoints"/>, and it made the same round trip.
    /// <b>It stays two points under the readout</b> so the two panels keep their order of
    /// importance — the city's state is the one you read without looking for it.
    /// </remarks>
    private const int HoverPoints = 16;

    /// <summary>Every Control panel's size at the reference window, in points.</summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b>, as <see cref="ReadoutPoints"/>. Godot's own default is <b>16</b> and
    /// this is that number said out loud, so that <see cref="Retype"/> has something to scale. The
    /// tool strip and both Ruleset panels sat at the default while the two Labels beside them grew,
    /// which is how a size nobody stated becomes a size nobody can change. <b>It matches
    /// <see cref="HoverPoints"/> rather than the readout's</b>: these are things you look at when
    /// you want them, which is the hover's standing and not the readout's.
    /// </remarks>
    private const int ControlPoints = 16;

    /// <summary>The window height every size above is stated at.</summary>
    private const float ReferenceHeight = 1080f;

    /// <summary>How far the type may grow, and that it may never shrink.</summary>
    /// <remarks>
    /// 🔴 <b>THE FLOOR IS 1 AND NOT A SYMMETRIC BAND, WHICH IS THE WHOLE DECISION.</b> Scaling both
    /// ways would make a small window ILLEGIBLE to solve a large window being roomy, and the two are
    /// not equally bad — ***the failure of small type is that you cannot read it, and the failure of
    /// large type is that it takes up space***. So the reference size is a MINIMUM and the window
    /// may only buy more.
    /// </remarks>
    private const float SmallestScale = 1f;

    /// <summary>⚠ <b>PROVISIONAL.</b> Past this the panel eats the frame it is describing.</summary>
    /// <remarks>
    /// 🔴 <b>IT WAS 1.6 AND THE PLAYER SAID SO WITHIN THE HOUR</b>, on 2026-09-04, on a
    /// 3,024×1,834 window — 1.698 clamped to 1.6, which put the readout at <b>32 point</b> and the
    /// tool strip at 26. ⚠ <b>This number is the whole of what a large window buys</b>, because the
    /// point sizes are stated at <see cref="ReferenceHeight"/> and a 1080 window sees none of it.
    /// ***So a complaint about the type on a big screen is a complaint about THIS constant and
    /// never about the base sizes***, which is the mistake the first patch made in the other
    /// direction. At 1.3 the same window reads 23 / 21 / 21.
    /// </remarks>
    private const float LargestScale = 1.3f;

    /// <summary>
    /// A point size for this window, from a size stated at <see cref="ReferenceHeight"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scaled rather than fixed, because this shell is photographed at resolutions nobody chose.</b>
    /// A driven run takes a <c>shoot</c> at whatever the window happens to be, and 18 points that
    /// read on a laptop are a hairline on a 4K capture — ***so a fixed size is not one decision, it
    /// is a different decision at every resolution, taken by whoever set the window.***
    /// </para>
    /// <para>
    /// ⚠ <b>HEIGHT AND NOT AREA OR DIAGONAL.</b> Text is read line by line, so what decides whether a
    /// panel crowds the frame is how many lines fit down it. A width term would shrink the type on a
    /// narrow tall window, which is the window with the most vertical room.
    /// </para>
    /// <para>
    /// ⚠ <b>A window with no height is the reference size and not zero.</b> <c>--headless</c> has no
    /// meaningful viewport, and a font of 0 points would make every driven <c>readout</c> come back
    /// empty while reporting success — ***the failure mode a screenshot renders as a blank panel and
    /// a caption renders as nothing at all.***
    /// </para>
    /// </remarks>
    private int Typed(int points)
    {
        float tall = GetViewport()?.GetVisibleRect().Size.Y ?? 0f;

        if (tall <= 0f)
        {
            return points;
        }

        return Mathf.RoundToInt(
            points * Mathf.Clamp(tall / ReferenceHeight, SmallestScale, LargestScale));
    }

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

        // TOP RIGHT AND GROWING DOWNWARD, moved there from the bottom left on 2026-09-04 at the
        // player's request. The two panels now sit in opposite top corners: what the CITY is doing
        // on the left, what the CURSOR is over on the right, and the eye travels sideways along one
        // line rather than diagonally across the frame.
        //
        // 🔴 THE COMMENT THIS REPLACES ARGUED THE OPPOSITE AND ITS ARGUMENT READS INVERTED. It said
        // the panel grew upward because "a top-anchored panel would make every line move whenever
        // the one above it appeared". Pointing() APPENDS -- the Tile/Cell line is always first and
        // every conditional line goes after it -- so a top anchor pins the one line that is always
        // there and a bottom anchor pins the one that is not, which moves the header on every sweep.
        // ⚠ THIS IS REASONED FROM Pointing()'s ORDER AND NOT FROM WATCHING IT. The old note was
        // written by somebody who had the shell in front of them, so treat this as the claim to
        // check first if the panel turns out to jump: ***a comment that disagrees with a comment is
        // not evidence, it is two people to ask.***
        //
        // ⚠ OFFSETS AND NOT Position, WHICH IS WHAT CLIPPED THE LAST LINE at the old corner. On an
        // anchored Control, Position writes OffsetLeft/OffsetTop -- so setting it pinned the panel
        // against the wrong edge and the stack grew off the screen. The pinned edge has to be the
        // one the stack grows AWAY from, which here is the top right.
        _hover = new Label
        {
            AnchorLeft = 1f,
            AnchorRight = 1f,
            GrowHorizontal = Control.GrowDirection.Begin,
            HorizontalAlignment = HorizontalAlignment.Right,

            OffsetRight = -16f,
            OffsetTop = 12f,
            LabelSettings = new LabelSettings { FontSize = Typed(HoverPoints) },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        _hud = new CanvasLayer();
        _type.DefaultFontSize = Typed(ControlPoints);

        // 🔴 RE-TYPED ON A RESIZE, because Typed() reads the viewport ONCE and a window is dragged.
        // Without this the scale is a property of the size the shell HAPPENED TO OPEN AT, which is
        // the class of bug that looks like it works: every fresh run is right and only a resized one
        // is wrong, so nobody meets it until they are mid-session and not looking for it.
        GetViewport().SizeChanged += Retype;

        _hud.AddChild(_readout);
        _hud.AddChild(_hover);
        AddChild(_hud);
        Tuner(_hud);
        Panels();
    }
}
