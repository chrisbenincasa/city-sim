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

// ---- the ground and the layers -- everything drawn that is not a Building --------------------
//
// Terrain, water, flood, hazard, plots, yards, scatter, the paved extent, the Cell lattice, and
// the Map Layer washes. The washes are here rather than in a file of their own because they are
// a TEXTURE ON THE GROUND: Skin() paints the terrain and Washing() paints over it, and the two
// share the same texel arithmetic.
//
// The overlay reads the layer's own cadence rather than a constant here (adr/0044), so a Ruleset
// that slows its diffusion slows the picture with it.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    /// <summary>
    /// Every <b>vacant</b> Lot, as a flat pad on the kerb. <b>What <c>Zone</c> actually produces.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Zone creates Lots and never a Building</b> (<c>adr/0069</c> — construction houses
    /// nobody, and <c>02 §2.2</c> — Lots are generated rather than painted), so before this layer
    /// existed the verb's entire visible result was <em>nothing</em>. On a world whose Unplaced Pool
    /// is empty — which is every shipped file that condemns nothing — the simulation never builds on
    /// a new Lot either, so the nothing was permanent. ***A verb whose success and whose failure
    /// look identical cannot be learned.***
    /// </para>
    /// <para>
    /// ⚠ <b>Vacant only, and that is the informative half.</b> An occupied Lot already has a
    /// Building standing on it; drawing a pad under one would be a marker nobody can see saying
    /// something the Building already says.
    /// </para>
    /// <para>
    /// 🔴 <b>IT IS THE LOT'S PARCEL, DRAWN WHOLE</b> (<c>plans/0052</c> stage 1) — the same
    /// rectangle <see cref="Buildings"/> stands a wall on a share of. ⚠ <b>Before this the two
    /// layers invented DIFFERENT ground</b>: a Lot's coordinate is a point on a Segment
    /// (<c>adr/0078</c>) and each of them stepped off it by its own setback, so a pad and the
    /// Building that replaced it did not cover the same patch. <b>They cannot disagree now, because
    /// neither of them is deciding.</b>
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)>
        Plots()
    {
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.IsVacant(slot))
            {
                continue;
            }

            // ⚠ THE PAD IS THE LOT'S PARCEL, which is what a vacant Lot has always been trying
            // to draw -- the ground the city would build on. Before plans/0052 stage 1 the shell
            // had to invent it, and it invented a DIFFERENT rectangle here than Buildings() did.
            int wideTiles = lots.ParcelWide[slot].Raw;
            int deepTiles = lots.ParcelDeep[slot].Raw;

            if (wideTiles <= 0 || deepTiles <= 0)
            {
                continue;
            }

            float parcelWide = wideTiles * MetresPerTile;
            float parcelDeep = deepTiles * MetresPerTile;

            if (Mathf.Min(parcelWide, parcelDeep) < MinFrontageMetres)
            {
                continue;
            }

            // The pad IS the parcel, drawn whole -- which is the one thing in this shell that is
            // allowed to be, because a pad is not a Building and the ground under it is exactly
            // what the verb produced.
            float east = (lots.ParcelEast[slot].Raw * MetresPerTile) + (parcelWide * 0.5f);
            float north = (lots.ParcelNorth[slot].Raw * MetresPerTile) + (parcelDeep * 0.5f);

            // A hand's breadth off the ground -- above the carriageway at 0.1 m so a pad on a kerb
            // is not hidden by the road it hangs on, and far below anything that stands up.
            Vector3 plan = new(parcelWide, 0.4f, parcelDeep);

            yield return (
                lots.Rows.IdAt(slot),
                new Transform3D(Basis.FromScale(plan), new Vector3(east, 0.2f, -north)),
                new Color(0.42f, 0.52f, 0.30f).SrgbToLinear());
        }
    }

    /// <summary>
    /// One slab under the whole map, so that dry land is a surface rather than the absence of one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One instance, laid once, and it never changes</b> — the map is a fixed size
    /// (<c>CellGrid.WorldTiles</c>), so this is a single box rather than a Cell grid. A per-Cell
    /// ground would be 262,144 instances to say one thing.
    /// </para>
    /// <para>
    /// ⚠ <b>It sits BELOW the water and below the carriageway</b>, at a depth chosen to clear both
    /// without z-fighting: the sea is drawn at <see cref="WaterMetres"/> from zero and the roads at a
    /// tenth of a metre, so the ground's top face is under the lower of the two.
    /// </para>
    /// <para>
    /// ⚠ <b>It carries no terrain and must not be read as any.</b> <c>rulesets/varied.toml</c> is the
    /// only shipped file that states <c>[[terrain]]</c>, and nothing here asks it — this is one flat
    /// colour, and the day the ground means something it becomes a per-Cell layer that reads it.
    /// </para>
    /// </remarks>
    private void Ground()
    {
        const float depth = 4f;
        float side = CellGrid.WorldTiles * MetresPerTile;

        _ground.Multimesh.InstanceCount = 1;
        _ground.Multimesh.SetInstanceTransform(
            0,
            new Transform3D(
                Basis.FromScale(new Vector3(side, depth, side)),
                new Vector3(side * 0.5f, -depth * 0.5f, -side * 0.5f)));

        // ⚠ WITHOUT THIS THE SLAB IS PLACED AND NOT DRAWN. A MultiMesh has a buffer size and a
        // visible count, Layer leaves the second at zero, and Fill is what normally raises it -- so
        // a layer written by hand rather than filled from an enumeration silently renders nothing.
        _ground.Multimesh.VisibleInstanceCount = 1;
    }

    /// <summary>Every Cell a Disaster has under water right now.</summary>
    /// <remarks>
    /// <b>Refilled every frame, unlike <see cref="Flood"/>'s sea</b> — these rows are created as a
    /// surge rises and freed as it recedes, which is the whole of what there is to watch.
    /// </remarks>
    private System.Collections.Generic.IEnumerable<Transform3D> Inundated()
    {
        InundationTable wet = _world.Inundations;

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (wet.Rows.IsLive(slot))
            {
                yield return Tile(wet.East[slot], wet.North[slot], WaterMetres * 1.4f);
            }
        }
    }

    /// <summary>One Cell-sized flat slab, centred on the Cell.</summary>
    private static Transform3D Tile(Cells east, Cells north, float height) =>
        new(
            Basis.FromScale(new Vector3(CellMetres, height, CellMetres)),
            new Vector3(
                (east.Raw + 0.5f) * CellMetres,
                height * 0.5f,
                -(north.Raw + 0.5f) * CellMetres));

    /// <summary>Every Traveller the last query placed.</summary>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> Travellers(
        int found)
    {
        for (int agent = 0; agent < found; agent++)
        {
            // ⚠ RESOLVED RATHER THAN READ. A Handle's index is internal to the core on purpose --
            // it is a recycled slot, and 05 §4 folds the monotonic id precisely because the slot
            // is not identity. A draw list keyed on a slot would name a different Citizen after a
            // collection, which is the one thing a list meant for diffing must never do.
            ulong id = _world.Citizens.Rows.TryResolve(_agents[agent].Citizen, out int slot)
                ? _world.Citizens.Rows.IdAt(slot)
                : 0UL;

            yield return (
                id,
                new Transform3D(
                Basis.Identity,
                new Vector3(
                    _agents[agent].East.Raw * MetresPerTile / 65_536f,
                    4f,
                    -_agents[agent].North.Raw * MetresPerTile / 65_536f)));
        }
    }

    /// <summary>One block-sized flat slab, centred on the block the point falls in.</summary>
    private static Transform3D Block((Tiles East, Tiles North) at, int blockTiles, float height)
    {
        float span = blockTiles * MetresPerTile;
        int column = IntegerMath.FloorDiv(at.East.Raw, blockTiles);
        int row = IntegerMath.FloorDiv(at.North.Raw, blockTiles);

        return new Transform3D(
            Basis.FromScale(new Vector3(span, height, span)),
            new Vector3((column + 0.5f) * span, height * 0.5f, -(row + 0.5f) * span));
    }

    /// <summary>
    /// The lattice edge a Street click would act on, <b>drawn as the Segment it would lay.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>NARROWER THAN THE CARRIAGEWAY AND FLOATING OVER IT, AND BOTH HALVES ARE LOAD-BEARING.</b>
    /// A ghost as wide as a Street would <em>hide</em> the Street it is aimed at — on exactly the
    /// edges shift-click can act on, which are the ones where knowing there is one is the whole
    /// question. So it is a spine down the middle at <see cref="AimAboveMetres"/>: virgin ground
    /// gets a yellow bar on green, and an edge that already carries a Street gets a grey road with a
    /// yellow stripe down it. ***The ghost says where the click lands and the picture says which of
    /// the two things it will do.***
    /// </para>
    /// <para>
    /// ⚠ <b>The height was measured against the carriageway's mesh rather than assumed.</b>
    /// <see cref="Pave"/>'s Y scale is 1 and reads as half a metre — but the roads layer's box is
    /// <c>0.1</c> m tall to begin with, so a Street's roof is at <b>0.05 m</b>. The first version of
    /// this was a wide band sunk to 0.2 m <em>to pass under that roof</em> and let the road show
    /// through the middle of it; the roof was ten times lower than the scale suggested, so the band
    /// floated over the Street and hid it. ***A height derived from a scale rather than from the
    /// mesh it scales is a number about the wrong object***, and the screenshot that would have
    /// caught it is the second one — the first had no Street under the aim.
    /// </para>
    /// <para>
    /// ⚠ <b>It asks <see cref="StreetGrid.NearestEdge"/> rather than deriving the edge itself</b>,
    /// which is <see cref="Lay"/>'s query and <see cref="Aiming"/>'s. ***Three derivations of one
    /// aim is three chances for the ghost, the panel and the verb to disagree*** — and the ghost is
    /// the one nobody would ever check against the other two.
    /// </para>
    /// <para>
    /// ⚠ <b>Half the CARRIAGEWAY and not half the street</b>, which is the quantity that kept the
    /// ghost a spine when <see cref="Footways"/> narrowed the asphalt. Half the right of way is now
    /// wider than the asphalt it is meant to sit inside, so it would have covered the kerb on both
    /// sides — ***the same 4 m, meaning a different thing.***
    /// </para>
    /// </remarks>
    private Transform3D Edge((Tiles East, Tiles North) at, int blockTiles)
    {
        float span = blockTiles * MetresPerTile;
        float wide = CarriagewayWidthMetres * 0.5f;
        (int column, int row, StreetAxis axis) = _world.Roads.Streets.NearestEdge(at.East, at.North);

        return axis == StreetAxis.East
            ? new Transform3D(
                Basis.FromScale(new Vector3(span, AimThickMetres, wide)),
                new Vector3((column + 0.5f) * span, AimAboveMetres, -row * span))
            : new Transform3D(
                Basis.FromScale(new Vector3(wide, AimThickMetres, span)),
                new Vector3(column * span, AimAboveMetres, -(row + 0.5f) * span));
    }

    /// <summary>How high the Street tool's ghost floats, in metres. <b>Clear of the carriageway.</b></summary>
    /// <remarks>
    /// ⚠ <b>PROVISIONAL</b> under the amnesty's standing order 4 — chosen by eye, no ratifier. It is
    /// a drawing height and not a quantity the city holds, so <c>adr/0015</c> does not reach it: no
    /// designer tunes how far a cursor floats over a road.
    /// </remarks>
    private const float AimAboveMetres = 0.2f;

    /// <summary>How thick the Street tool's ghost is drawn. <b>PROVISIONAL</b>, as its height.</summary>
    private const float AimThickMetres = 0.2f;

    /// <summary>
    /// The Hazard Region, laid once — <b><c>01 §5.3</c>'s posted price, and the shell's first
    /// overlay of a thing that has not happened.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Everything else this shell draws is a thing that exists; this is a thing that MIGHT.</b>
    /// <c>01 §5.3</c> asks for the floodplain to be visible from the first Tick, so that riverside
    /// land is a decision the player made rather than an ambush the world sprang — ***a price
    /// somebody paid without being shown it is not a price, it is a surprise.*** The paragraph on
    /// <see cref="Flood"/> called this a gap for as long as it was one.
    /// </para>
    /// <para>
    /// <b>Laid once, for <see cref="Flood"/>'s reason and the same one.</b>
    /// <see cref="FloodCellTable"/> is generator output under <c>adr/0157</c> — written before the
    /// first Tick from a height field that is then thrown away — so a Cell's depth never moves and a
    /// per-frame refill would be the same rows every frame for ever. ⚠ <b>Re-laid on a rebuild</b>
    /// (<c>n</c>, in the tuner), because a new seed is a new coastline.
    /// </para>
    /// <para>
    /// ⚠ <b>The DEPTH is not drawn and that is a decision rather than an omission.</b> A
    /// <see cref="FloodCellTable"/> row's depth is <em>the flood level minus the ground</em>, so a
    /// large one is LOW ground — the polarity that made <c>flooded.toml</c>'s worst-looking seed the
    /// one that ruined nothing. ***A shade ramp on a quantity that reads backwards teaches the wrong
    /// thing faster than no ramp at all***, so this is one flat colour saying <em>at risk</em>, and
    /// <c>--flood</c> keeps the numbers.
    /// </para>
    /// <para>
    /// ⚠ <b>Only <c>coastal.toml</c> and <c>flooded.toml</c> have any</b>, and on every other shipped
    /// world this lays nothing. ⚠ <b>It is bounded by <see cref="Layer"/>'s buffer</b> — 65,536 Cells
    /// against a 262,144-Cell map, so a world with more than a quarter of its ground at risk would
    /// truncate. <see cref="Fill(MultiMeshInstance3D, System.Collections.Generic.IEnumerable{Transform3D})"/>
    /// stops rather than throwing, which is <see cref="MapLayers.LayerCells"/>' disposition, and the
    /// measured worlds sit at 3–9%.
    /// </para>
    /// </remarks>
    private void Hazard()
    {
        Fill(_hazard, Anonymous(AtRisk()));

        // OFF, on `_cells`' own bargain: it is an instrument rather than scenery, and since the
        // skin carries the floodplain the box layer's only remaining reader is `draw`.
        _hazard.Visible = false;
    }

    /// <summary>
    /// Give a run of placements no identity, for the layers that draw <b>ground</b> rather than
    /// entities.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A flooded Cell and a Hazard Region Cell are not rows in a table</b> — they are painted
    /// from a grid, and the honest key for one is where it is, which the row already carries. So
    /// the id column is <c>-</c> for them rather than a number invented to fill it.
    /// </remarks>
    private static System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> Anonymous(
        System.Collections.Generic.IEnumerable<Transform3D> places)
    {
        foreach (Transform3D place in places)
        {
            yield return (0UL, place);
        }
    }

    /// <summary>Every Cell a flood could reach, whether or not one ever does.</summary>
    private System.Collections.Generic.IEnumerable<Transform3D> AtRisk()
    {
        FloodCellTable plain = _world.Flood;

        for (int slot = 0; slot < plain.Rows.SlotCount; slot++)
        {
            if (plain.Rows.IsLive(slot))
            {
                // A FIFTH of a road's height, so the overlay sits on the ground rather than on top of
                // the city. Anything at or above 0.1f swallows the carriageway and the floodplain
                // reads as a hole in the road network.
                yield return Tile(plain.East[slot], plain.North[slot], 0.02f);
            }
        }
    }

    /// <summary>
    /// The sea, laid once — <b>the first thing the shell has ever drawn that is not the city.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0157</c> makes the water graph generator output, computed from a height field that is
    /// then thrown away, so a Water Body's Cells are written before the first Tick and never move.
    /// ⚠ <b>Only <c>coastal.toml</c> and <c>flooded.toml</c> have any</b>; every other shipped world
    /// is inland and this lays nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>This is the water and not the risk.</b> The floodplain is <see cref="Hazard"/>'s, laid
    /// on the same occasion and under this — the two are separate layers because they are separate
    /// claims: ground that <em>is</em> wet, and ground that <em>can be</em>.
    /// </para>
    /// </remarks>
    private void Flood()
    {
        WaterCellTable cells = _world.WaterCells;
        int drawn = 0;

        for (int slot = 0;
             slot < cells.Rows.SlotCount && drawn < _water.Multimesh.InstanceCount;
             slot++)
        {
            if (cells.Rows.IsLive(slot))
            {
                _water.Multimesh.SetInstanceTransform(
                    drawn++, Tile(cells.East[slot], cells.North[slot], WaterMetres));
            }
        }

        _water.Multimesh.VisibleInstanceCount = drawn;
    }

    /// <summary>
    /// Lays the ground's own skin: one texel a Cell, terrain blended toward Woodland.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A TEXTURE AND NOT 262,144 COLOURED SQUARES, AND THE REASON IS THE FILTER.</b> The world
    /// holds one <see cref="TerrainKind"/> and one Woodland count per <b>Cell</b>, which is 128 m —
    /// so an instance apiece would draw the map as a chequerboard of hard 128 m tiles. A texel apiece
    /// on one quad costs one draw call and gets <b>bilinear blending for free</b>, which is what a
    /// shoreline and a field edge want. ⚠ <b>It is a smoothing of the DRAWING and not of the data</b>:
    /// the hover still reads the Cell, and a Rule still gets the step.
    /// </para>
    /// <para>
    /// ⚠ <b>Written in sRGB and NOT converted</b>, unlike every instance colour in this file — an
    /// albedo texture is sRGB by contract and <see cref="Color.SrgbToLinear"/> here would wash the
    /// whole map out. <see cref="Image.CreateFromData"/> rather than <c>SetPixel</c>, because a
    /// quarter of a million interop calls at world creation is a visible pause.
    /// </para>
    /// <para>
    /// ⚠ <b>Both tables are on EVERY world.</b> <c>TerrainGenerator</c> and <c>WoodlandGenerator</c>
    /// run from the <c>WorldKey</c> alone — <em>"a file that states no <c>[terrain]</c> still gets
    /// forest"</em> — so this is not a <c>varied.toml</c> feature. ***Every shipped Ruleset has had a
    /// landscape since milestone 24 and not one of them has ever shown it.***
    /// </para>
    /// </remarks>
    private void Skin()
    {
        const int side = CellGrid.WorldCells;
        TerrainCellTable ground = _world.Layers.Terrain;
        WoodlandCellTable trees = _world.Layers.Woodland;
        byte[] texels = new byte[side * side * 3];

        for (int north = 0; north < side; north++)
        {
            // The image's first row is the map's NORTH edge, because a texture's V runs down and the
            // world's north runs up. Getting this wrong mirrors the landscape against the city.
            int row = (side - 1 - north) * side * 3;

            for (int east = 0; east < side; east++)
            {
                var where = new Cells(east);
                var up = new Cells(north);
                int kind = (int)ground.At(where, up);
                Color paint = Land[kind >= 0 && kind < Land.Length ? kind : 0].Lerp(
                    Forest,
                    Mathf.Clamp(trees.At(where, up) / (float)CellGrid.TilesInCell, 0f, 1f));

                texels[row + (east * 3)] = (byte)(paint.R * 255f);
                texels[row + (east * 3) + 1] = (byte)(paint.G * 255f);
                texels[row + (east * 3) + 2] = (byte)(paint.B * 255f);
            }
        }

        // ⚠ THE FLOODPLAIN IS GROUND AND GOES IN THE GROUND, UNDER the sea so water wins a Cell
        // that is both. Drawn as 11,063 boxes a fifth of a metre up it STRIPED at map distance --
        // depth precision at 30 km against a 0.02 m lift -- and read as corrupted terrain rather
        // than as risk. ⚠ The box layer SURVIVES and is hidden, because `plans/0048`'s answer is the
        // draw list and a texture is invisible to one: `draw` still counts the Cells.
        FloodCellTable plain = _world.Flood;

        for (int slot = 0; slot < plain.Rows.SlotCount; slot++)
        {
            if (!plain.Rows.IsLive(slot))
            {
                continue;
            }

            int at = Texel(plain.East[slot].Raw, plain.North[slot].Raw, side);

            if (at < 0)
            {
                continue;
            }

            var wet = new Color(texels[at] / 255f, texels[at + 1] / 255f, texels[at + 2] / 255f)
                .Lerp(Silt, SiltShare);

            texels[at] = (byte)(wet.R * 255f);
            texels[at + 1] = (byte)(wet.G * 255f);
            texels[at + 2] = (byte)(wet.B * 255f);
        }

        // 🔴 THE SEA GOES IN THE SKIN AND NOT ONLY IN ITS OWN LAYER. A Water Body is 5,058 Cells on
        // `coastal.toml` and a bigger one would pass the 65,536 an instance buffer holds -- and a
        // box per Cell gives a STEPPED shoreline, which is the one edge on the map anybody looks at.
        // The raised surface stays; this is what makes it read from three kilometres up.
        WaterCellTable sea = _world.WaterCells;

        for (int slot = 0; slot < sea.Rows.SlotCount; slot++)
        {
            if (!sea.Rows.IsLive(slot))
            {
                continue;
            }

            int wet = Texel(sea.East[slot].Raw, sea.North[slot].Raw, side);

            if (wet < 0)
            {
                continue;
            }

            texels[wet] = (byte)(Shallows.R * 255f);
            texels[wet + 1] = (byte)(Shallows.G * 255f);
            texels[wet + 2] = (byte)(Shallows.B * 255f);
        }

        Image image = Image.CreateFromData(side, side, false, Image.Format.Rgb8, texels);

        if (_skin is null)
        {
            _skin = ImageTexture.CreateFromImage(image);
        }
        else
        {
            _skin.Update(image);
        }
    }

    /// <summary>
    /// Trees and boulders, scattered over the Cells around the city.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE BLOCK INTERIOR IS WHERE THESE LAND, AND THAT IS THE POINT.</b> A Lot is an address —
    /// a distance along a Segment and a side — so nothing in the city can name a point in the middle
    /// of a block (<c>adr/0078</c>), and at the shipped <c>block_tiles = 32</c> that middle is most
    /// of the block. ***It is the one large surface the renderer may dress without lying***, and
    /// bare it read as countryside that had been fenced off.
    /// </para>
    /// <para>
    /// ⚠ <b>The count comes from the world and the positions do not.</b> Woodland is
    /// <c>(saved AND hashed)</c>, one count a Cell, and <see cref="MapLayers.Seal"/> takes it back as
    /// ground is built on — so a Cell's tree count is the city's own number and a built Cell thins
    /// out by itself. Where each crown stands is <see cref="Scramble"/>'s, which is the shell's
    /// stream and <b>never a <c>purpose_tag</c></b>: a shape nobody in the city can perceive is not a
    /// decision.
    /// </para>
    /// <para>
    /// ⚠ <b>Inside the laid city a crown must clear <see cref="YardMargin"/> of every block
    /// edge</b>, which is what keeps one out of the carriageway and out of somebody's front room.
    /// Outside it there is no lattice to avoid and the test is not applied — ***a regular gap in open
    /// country would draw the road grid where there are no roads.***
    /// </para>
    /// </remarks>
    private void Scatter()
    {
        int block = _world.Roads.Streets.BlockTiles;
        int lots = _world.Rules.Lots.LotsPerSegment;
        float reach = Mathf.Max(_span * 1.6f, 2_048f);
        Rect2 window = _laid.Grow(reach);

        // ⚠ THE YARD TEST FOLLOWS THE PAVING AND NOT THE LOTS. `SyntheticCity` lays a lattice larger
        // than it subdivides (`plans/0049` F8), so a rectangle drawn round the Lots leaves paved
        // blocks outside it -- and a crown scattered freely there stands in a carriageway.
        //
        // 🔴 IT IS THE ROAD GRAPH'S OWN EXTENT AND NO LONGER A MARGIN PROPORTIONAL TO THE CITY.
        // `_laid.Grow(_span * 0.5f)` suppressed a city-width of countryside in every direction, so
        // the bigger the city the barer its surroundings -- plans/0051 F7. One block of slack is
        // kept because a Node is a centreline and a carriageway has width.
        Rect2 paved = _paved.Grow(block * MetresPerTile);
        WoodlandCellTable trees = _world.Layers.Woodland;
        TerrainCellTable ground = _world.Layers.Terrain;
        int crowns = 0;
        int stones = 0;

        int fromEast = Mathf.Max(0, (int)(window.Position.X / CellGrid.MetresPerCell));
        int fromNorth = Mathf.Max(0, (int)(window.Position.Y / CellGrid.MetresPerCell));
        int toEast = Mathf.Min(CellGrid.WorldCells - 1, (int)(window.End.X / CellGrid.MetresPerCell));
        int toNorth = Mathf.Min(CellGrid.WorldCells - 1, (int)(window.End.Y / CellGrid.MetresPerCell));

        for (int north = fromNorth; north <= toNorth; north++)
        {
            for (int east = fromEast; east <= toEast; east++)
            {
                var at = new Cells(east);
                var up = new Cells(north);
                ulong key = Scramble(((ulong)(uint)north << 20) | (uint)east);
                int wooded = trees.At(at, up);
                int wants = wooded * CrownsPerCell / CellGrid.TilesInCell;
                TerrainKind kind = ground.At(at, up);
                int rocky = kind is TerrainKind.Rock or TerrainKind.ThinSoil ? 3 : 0;

                for (int i = 0; i < wants + rocky; i++)
                {
                    ulong draw = Scramble(key + (ulong)i);
                    float x = (east * CellGrid.MetresPerCell) + ((draw & 0xFFFu) / 4095f * CellGrid.MetresPerCell);
                    float z = (north * CellGrid.MetresPerCell) + (((draw >> 12) & 0xFFFu) / 4095f * CellGrid.MetresPerCell);

                    if (paved.HasPoint(new Vector2(x, z)) && !InAYard(x, z, block, lots))
                    {
                        continue;
                    }

                    if (i < wants)
                    {
                        if (crowns >= _trees.Multimesh.InstanceCount)
                        {
                            continue;
                        }

                        float tall = 7f + (((draw >> 24) & 0xFFu) / 255f * 9f);
                        float broad = tall * (0.45f + (((draw >> 32) & 0x3Fu) / 63f * 0.3f));

                        _trees.Multimesh.SetInstanceTransform(
                            crowns++,
                            new Transform3D(
                                Basis.FromScale(new Vector3(broad, tall, broad)),
                                new Vector3(x, tall * 0.42f, -z)));
                    }
                    else if (stones < _rocks.Multimesh.InstanceCount)
                    {
                        float lump = 1.6f + (((draw >> 24) & 0xFFu) / 255f * 3.4f);

                        _rocks.Multimesh.SetInstanceTransform(
                            stones++,
                            new Transform3D(
                                Basis.FromScale(new Vector3(lump, lump * 0.6f, lump * 0.8f)),
                                new Vector3(x, lump * 0.3f, -z)));
                    }
                }
            }
        }

        _trees.Multimesh.VisibleInstanceCount = crowns;
        _rocks.Multimesh.VisibleInstanceCount = stones;
    }

    /// <summary>Whether a point stands far enough into a block to be behind the Buildings.</summary>
    private static bool InAYard(float east, float north, int block, int lots)
    {
        if (block <= 0)
        {
            return true;
        }

        float span = block * MetresPerTile;
        float margin = YardMargin(block, lots);
        float alongEast = Mathf.PosMod(east, span);
        float alongNorth = Mathf.PosMod(north, span);

        return alongEast > margin && alongEast < span - margin
            && alongNorth > margin && alongNorth < span - margin;
    }

    /// <summary>Where a Cell's three bytes start in the skin, or <c>-1</c> if it is off the map.</summary>
    /// <remarks>
    /// ⚠ <b>The image's first row is the map's NORTH edge</b>, because a texture's V runs down and
    /// the world's north runs up. Getting this wrong mirrors the landscape against the city, which
    /// is a defect that looks like a plausible different world.
    /// </remarks>
    private static int Texel(int east, int north, int side) =>
        east >= 0 && east < side && north >= 0 && north < side
            ? ((side - 1 - north) * side * 3) + (east * 3)
            : -1;

    /// <summary>Paint the map by a Map Layer, or stop and give the city back.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE CITY DROPS TO AN UNLIT BASE AND THE LAYER OWNS ALL THE COLOUR</b>
    /// (<c>docs/07 §3.1</c>). Every other layer takes a shared flat grey through
    /// <c>MaterialOverride</c> and the ground goes <b>unshaded</b> — which is the half a person
    /// forgets, and the important half: ***a field lit by a raking sunset is a field multiplied by
    /// the time of day***, and an instrument that reads differently at 18:00 than at noon is not an
    /// instrument. The sun keeps moving; nothing in the frame answers to it.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a VIEW.</b> Nothing here reaches the world (<c>adr/0007</c>, <c>05 §4</c>) and
    /// two runs differing only in what was tinted produce the same State Hash.
    /// </para>
    /// <para>
    /// ⚠ <b>An unknown name is a refusal a person reads</b>, on <see cref="Hold"/>'s reasoning —
    /// a script that misspells its layer would otherwise turn the overlay off and report a city
    /// with nothing in it.
    /// </para>
    /// </remarks>
    /// <param name="layer">The layer's name, or <c>off</c>.</param>
    private void Washing(string layer)
    {
        _refused = string.Empty;

        Wash want = layer switch
        {
            "off" or "none" => Wash.None,
            "pollution" => Wash.Pollution,
            "value" or "land" or "land-value" => Wash.Value,
            "sealing" or "sealed" => Wash.Sealed,
            "rung" or "pattern" => Wash.Rung,
            "age" or "vintage" => Wash.Age,
            _ => Wash.None,
        };

        if (want == Wash.None && layer is not ("off" or "none"))
        {
            _refused = $"there is no overlay called '{layer}'. There is off, pollution, value, "
                + "sealing, and the two debug ones, rung and age.";
        }

        _washing = want;

        // Forces the next Draw to rebuild rather than trusting a cadence that has not come round.
        _washedAt = 0;

        _instrument ??= new StandardMaterial3D
        {
            // 🔴 UNSHADED, AND THIS IS THE LINE THE OVERLAY EXISTS FOR. See the remarks.
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
        };

        _muted ??= new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = Muted,
        };

        if (_washing == Wash.Rung)
        {
            _rungOf = new int[BlockPatterns.Count];

            for (int i = 0; i < BlockPatterns.Count; i++)
            {
                _rungOf[i] = BlockPatterns.Rung(
                    (BlockPattern)i,
                    _world.Roads.Streets.BlockTiles,
                    _world.Rules.Lots.LotsPerSegment);
            }
        }

        _categorical ??= new StandardMaterial3D
        {
            // The same unshaded argument the ground overlay makes, spent on the MASSING instead:
            // a rung read against a sunset is a rung multiplied by the time of day. Instance
            // colour has to be the albedo, because a MultiMesh's per-instance colour is the only
            // channel that survives one material over thousands of Buildings.
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
        };

        foreach ((string name, MultiMeshInstance3D over, bool _, List<ulong>? _) in Layers())
        {
            over.MaterialOverride = _washing switch
            {
                Wash.None => null,

                // ⚠ THE MASSING KEEPS ITS OWN COLOUR UNDER A BUILDING WASH, which is the whole
                // difference between these two and the three ground ones. A ground overlay mutes
                // everything and lets the plane carry the reading; a building overlay mutes
                // everything EXCEPT the thing being measured.
                Wash.Rung or Wash.Age when name is "building" or "roof" => _categorical,
                _ => _muted,
            };
        }

        _cursor.MaterialOverride = _washing == Wash.None ? null : _muted;

        if (_washing == Wash.None)
        {
            ((PlaneMesh)_land.Mesh).Material = _skinned;

            return;
        }

        // A building wash reads nothing off a Cell, so there is no texture to build and the ground
        // goes dark rather than staying under the last layer's tint -- which would be a stale
        // instrument sitting beside a live one, and the reader has no way to tell.
        if (_washing is Wash.Rung or Wash.Age)
        {
            _washPeak = 0;
            _washCells = 0;
            ((PlaneMesh)_land.Mesh).Material = _muted;

            return;
        }

        Rewash();

        _instrument.AlbedoTexture = _wash;
        ((PlaneMesh)_land.Mesh).Material = _instrument;
    }

    /// <summary>Write the layer in force into <see cref="_wash"/>, one texel a Cell.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It walks the RESIDENT ROWS and not the grid.</b> A Cell gets a row when something
    /// happens on it, so on a 20,000-Citizen world there are about a thousand of them against the
    /// map's 262,144 — and the loop that matters is over the thousand. The clear is a
    /// <c>byte[]</c> fill, which is a memset.
    /// </para>
    /// <para>
    /// 🔴 <b>MAGNITUDE, AND LAND VALUE IS THE REASON.</b> Desirability has no positive term until
    /// amenity exists (<c>adr/0123</c>), so the whole field is at or below zero and the interesting
    /// quantity is how far below. The readout says so rather than leaving a person to read
    /// <em>bright means good</em> off a ramp that means the opposite.
    /// </para>
    /// <para>
    /// ⚠ <b>Normalised against the peak, and the peak is PRINTED.</b> A ramp fitted to whatever
    /// the worst Cell holds looks identical on a filthy city and a clean one; the number the top
    /// of the ramp stands for is what makes the picture a measurement.
    /// </para>
    /// </remarks>
    private void Rewash()
    {
        const int side = CellGrid.WorldCells;
        LayerCellTable cells = _world.Layers.Cells;
        byte[] texels = new byte[side * side * 3];

        for (int at = 0; at < texels.Length; at += 3)
        {
            texels[at] = (byte)(Unmeasured.R * 255f);
            texels[at + 1] = (byte)(Unmeasured.G * 255f);
            texels[at + 2] = (byte)(Unmeasured.B * 255f);
        }

        _washPeak = 0;
        _washCells = 0;

        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot))
            {
                continue;
            }

            _washCells++;
            _washPeak = Math.Max(_washPeak, Math.Abs(Reading(cells, slot)));
        }

        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot))
            {
                continue;
            }

            int at = Texel(cells.East[slot].Raw, cells.North[slot].Raw, side);

            if (at < 0)
            {
                continue;
            }

            // ⚠ A MEASURED ZERO IS THE RAMP'S BOTTOM AND NOT `Unmeasured`. The two are different
            // claims -- "this Cell is clean" against "nothing has ever been read here" -- and the
            // whole reason the background is a separate colour is that they must not merge.
            float share = _washPeak == 0
                ? 0f
                : Math.Abs(Reading(cells, slot)) / (float)_washPeak;

            Color paint = Shade(share);

            texels[at] = (byte)(paint.R * 255f);
            texels[at + 1] = (byte)(paint.G * 255f);
            texels[at + 2] = (byte)(paint.B * 255f);
        }

        Image image = Image.CreateFromData(side, side, false, Image.Format.Rgb8, texels);

        if (_wash is null)
        {
            _wash = ImageTexture.CreateFromImage(image);
        }
        else
        {
            _wash.Update(image);
        }

        _washedAt = _world.Tick.Raw;
    }

    /// <summary>How often the layer in force actually moves, in Ticks.</summary>
    /// <remarks>
    /// ⚠ <b>Sealing has no diffusion pass of its own</b> — it is a count of built Tiles, written at
    /// the footprint (<c>02 §2.4</c>) — so it is redrawn on the pollution cadence rather than being
    /// given a number of its own, which would be a schedule this file invented.
    /// </remarks>
    private int Cadence()
    {
        LayerSchedule when = _world.Layers.Schedule;

        return _washing == Wash.Value
            ? when.LandValue.Period
            : when.IndustrialPollution.Period;
    }

    /// <summary>The number the overlay in force is drawing, for one Cell row.</summary>
    /// <remarks>
    /// 🔴 <b>THE STORED COLUMN IS NOT THE NUMBER, AND THE FIRST VERSION OF THIS PRINTED THE
    /// STORAGE.</b> Pollution is held in <em>kernel units</em> — pre-normalised, which is what buys
    /// exact superposition — and the single stated rounding of the whole scheme happens at the
    /// point of use, which is here. Land value is <b>Q16.16</b>. Read raw they came out as
    /// <b>1,955,195</b> and <b>17,642,822</b>, which are 5 and 269, and ***a number printed in its
    /// storage units is the disqualifier registry's own shape*** — digits that look like a
    /// quantity, carrying none of the clause that says what they are.
    /// </remarks>
    private int Reading(LayerCellTable cells, int slot) => _washing switch
    {
        Wash.Pollution => _world.Layers.PollutionKernel.Normalise(cells.Pollution[slot]),
        Wash.Value => cells.LandValue[slot],
        Wash.Sealed => cells.Sealing[slot],
        _ => 0,
    };

    /// <summary>A Q16.16 value as whole units and hundredths, which is what a reader compares.</summary>
    private static string Whole(int fixedValue)
    {
        int whole = fixedValue >> 16;
        int hundredths = ((fixedValue & 0xFFFF) * 100) >> 16;

        return string.Create(CultureInfo.InvariantCulture, $"{whole}.{hundredths:00}");
    }

    /// <summary>A share of the peak, as a colour off <see cref="Bands"/>, blended between stops.</summary>
    /// <remarks>
    /// ⚠ <b>Blended rather than stepped</b>, because eight bands on a 128 m grid is a contour map
    /// and a contour is a claim about a threshold that nothing in the city holds.
    /// </remarks>
    private static Color Shade(float share)
    {
        float along = Mathf.Clamp(share, 0f, 1f) * (Bands.Length - 1);
        int stop = Mathf.Clamp((int)along, 0, Bands.Length - 2);

        return Bands[stop].Lerp(Bands[stop + 1], along - stop);
    }

    /// <summary>What the overlay is saying, for the readout. Empty when there is none.</summary>
    /// <remarks>
    /// 🔴 <b>THE LEGEND IS NOT DECORATION.</b> A map tinted by a number nobody names is the fastest
    /// way in the genre to make somebody confident and wrong — <c>01 §7</c> ranks overlays as a
    /// primary view for exactly the reason they must carry their axis. This names the layer, what
    /// the bright end is worth, and how many Cells have a reading at all.
    /// </remarks>
    private string Legend() => _washing switch
    {
        Wash.None => string.Empty,
        Wash.Pollution =>
            $"\nOVERLAY pollution — dark 0 to bright {_washPeak:N0}, {_washCells:N0} Cells read",
        Wash.Value =>
            $"\nOVERLAY land value — MAGNITUDE, bright is WORSE: 0 to {Whole(_washPeak)}. "
            + $"Desirability has no positive term yet (adr/0123), so every Cell is at or below "
            + $"zero. {_washCells:N0} Cells read",
        Wash.Sealed => $"\nOVERLAY sealing — dark 0 to bright {_washPeak:N0} Tiles a Cell, "
            + $"{_washCells:N0} Cells read",
        Wash.Rung =>
            "\nOVERLAY rung — DEBUG. The block pattern a Building's Lot was carved by, "
            + "0 detached / 1 perimeter / 2 back-to-back / 3 courtyard / 4 slab / 5 tower, "
            + "sparsest to densest, ordered by how many people stand behind one door. Read off the "
            + "block and not off the height: a rung names a plot ratio, so two Buildings of one "
            + "height can be on different rungs",
        _ => $"\nOVERLAY age — DEBUG. Bright is OLD: raised at Tick 0, dark is raised now, "
            + $"at Tick {_world.Tick.Raw:N0}. ⚠ A generated city is one vintage — "
            + "SyntheticCity raises everything in one call — so a flat wash here is the world "
            + "saying so and not the overlay failing",
    };

    /// <summary>A Building's rung, read off the pattern its block was carved by.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT READ THE HEIGHT BACK AND <c>plans/0058</c> MADE THAT IMPOSSIBLE.</b> This was
    /// <c>(storeys - 2) / step</c>, which worked only while the rung <em>was</em> the storey count.
    /// A rung now names a plot ratio and the storeys divide it by the ground the pattern claims —
    /// ***so two Buildings of one height can be on different rungs and two on one rung can be
    /// different heights***, which is the entire point of the change and the exact thing a recovery
    /// from height cannot survive. It asks the block instead.
    /// </para>
    /// <para>
    /// ⚠ <b>Cold path, and it is three lookups a Building.</b> The Lot's frontage names its block,
    /// the block names its pattern, and <see cref="_rungOf"/> holds the ladder so the sort is not
    /// re-run per Building. It runs when the massing is rebuilt and never inside a Tick.
    /// </para>
    /// <para>
    /// ⚠ <b>A Lot whose block cannot be found reads as rung 0</b> rather than refusing. This is a
    /// debug view over a picture that is already drawn; a Building the wash cannot classify is
    /// better shown in the sparsest colour than not shown at all, and nothing downstream reads it.
    /// </para>
    /// </remarks>
    private int RungOf(int lot)
    {
        if (_rungOf is null
            || !Frontage.BlockOf(
                _world.Roads.Streets, _world.Lots.East[lot], _world.Lots.North[lot],
                (StreetSide)_world.Lots.Side[lot], out int column, out int row)
            || !_world.BlockIndex.Contains(column, row))
        {
            return 0;
        }

        int blockSlot = _world.BlockIndex.Slot(column, row);

        if (blockSlot == BlockResidency.NotResident)
        {
            return 0;
        }

        int rung = _rungOf[(int)_world.PatternOf(blockSlot, out _)];

        return rung < 0 ? 0 : rung >= Patterns.Length ? Patterns.Length - 1 : rung;
    }

    /// <summary>Each pattern's position on this world's ladder, built once when a wash is set.</summary>
    /// <remarks>
    /// ⚠ <b>The ladder is a function of two WORLD-CREATION keys</b> — <c>block_tiles</c> and
    /// <c>lots_per_segment</c> — so within one world it is a constant and building it per Building
    /// would be re-running a sort a few hundred times for one answer.
    /// </remarks>
    private int[]? _rungOf;

    /// <summary>How old a Building is, as a share of the run so far.</summary>
    /// <remarks>
    /// ⚠ <b>Normalised against the Tick now and not against the oldest Building</b>, so 1.0 always
    /// means <em>here since Tick 0</em> rather than <em>oldest thing on screen</em>. A ramp fitted
    /// to the oldest would look identical on a city of one vintage and a city of twenty, which is
    /// the exact distinction this view exists to make.
    /// </remarks>
    private float Vintage(int slot)
    {
        ulong now = _world.Tick.Raw;

        return now == 0 ? 1f : 1f - Mathf.Clamp(
            (now - _world.Buildings.StandingFor(slot, _world.Tick)) / (float)now, 0f, 1f);
    }

    /// <summary>The lit surface of the world, one quad carrying <see cref="Skin"/>.</summary>
    /// <remarks>
    /// ⚠ <b>A <c>PlaneMesh</c> and not the ground slab's box</b>, because a plane's UV runs 0..1
    /// across it and a box's is an atlas over six faces. The slab stays underneath as the map's
    /// visible thickness; this is the metre of it anybody looks at.
    /// </remarks>
    private void Surface()
    {
        float side = CellGrid.WorldTiles * MetresPerTile;

        Skin();

        // ⚠ HELD IN A FIELD BECAUSE AN OVERLAY TAKES THE GROUND AND HAS TO GIVE IT BACK. The
        // instrument swaps the plane's material for an unshaded one carrying the layer; this is
        // what it swaps back to, and rebuilding it on the way out would lose the texture with it.
        _skinned = new StandardMaterial3D
        {
            AlbedoTexture = _skin,
            Roughness = 1f,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
        };

        _land = new MeshInstance3D
        {
            Mesh = new PlaneMesh
            {
                Size = new Vector2(side, side),
                Material = _skinned,
            },
            Position = new Vector3(side * 0.5f, 0.01f, -side * 0.5f),

            // 🔴 THE ONE CASTER THAT PUT THE WHOLE MAP IN SHADOW. A 65.5 km plane at y = 0.01
            // casts onto the ground it IS, and the only thing keeping that off the screen is the
            // depth bias — which is denominated in shadow-map texels, and a cascade stretched over
            // kilometres has texels metres wide. ***So it looked right at a street and turned the
            // entire city black from a kilometre up***, which is a lighting bug that reads exactly
            // like the sun having gone out. A flat plane cannot shadow anything but itself.
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        AddChild(_land);
    }

    /// <summary>
    /// The Road Graph, laid once — the lattice is generated before the first frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT DRAWS THREE KINDS AND USED TO DRAW ONE.</b> <see cref="RoadSegmentTable.Kind"/> is a
    /// <c>(saved AND hashed)</c> column and the drawing ignored it, so on <c>rulesets/pictured.toml</c>
    /// the <b>6</b> <see cref="RoadKind.FootPath"/> Segments of its 200 were painted as 8 m of
    /// carriageway in road grey, identical to the 194 Streets beside them — ***a cut-through drawn as
    /// a road***. That is <c>docs/07 §1.3</c> failing on a fact the city already held, which is the
    /// cheapest kind of failure to have and the easiest to not see.
    /// </para>
    /// <para>
    /// ⚠ <b>The WIDTHS are invented and the DISTINCTION is not</b>, and the two halves are labelled
    /// apart on purpose (<c>plans/0049</c> rule 2). See <see cref="ArterialWidthMetres"/> for what the
    /// city does and does not state.
    /// </para>
    /// <para>
    /// ✅ <b>THE CROSSING IS DRAWN, AND IT IS NOT A STRIPE ACROSS A ROAD — IT IS A WHOLE STREET KEPT
    /// FOR FEET.</b> <c>RoadGenerator.ApplySeverance</c> takes every severed Street and either frees
    /// it or, at <c>[roads] foot_crossing_every</c>, sets its Kind to
    /// <see cref="RoadKind.FootPath"/> and both its mode masks to <see cref="TravelMode.Foot"/> —
    /// the <em>whole lattice edge</em>, not a short bar over the asphalt. So the thing this method
    /// was asked to invent already had a row, and has been drawn in <see cref="Flagstone"/> since
    /// step 1. ⚠ <b>Nothing here paints a zebra</b>, and that is now a measured absence of a fact
    /// rather than an assumption about one: <c>rulesets/severance.toml</c>'s <b>10</b> FootPath
    /// Segments are <b>1,024 m and 1,448 m long</b>, which are one lattice edge and one block
    /// diagonal. <c>plans/0049</c> <b>F63</b> closes <b>F58</b>'s reopening of <b>F51</b>.
    /// </para>
    /// <para>
    /// <b>The paragraph this replaced, kept because it was the corpus's third sighting of one
    /// mistake:</b> it said every shipped file states <c>arterial_count = 0</c>. <b>Three state
    /// 16</b> — <c>severance.toml</c>, <c>bordered.toml</c>, <c>crowded.toml</c>. Filed as a Cause
    /// in <c>plans/0012</c>.
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)> Paving()
    {
        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot)
                || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            var from = new Vector3(
                nodes.East[a].Raw * MetresPerTile, 0f, -nodes.North[a].Raw * MetresPerTile);
            var to = new Vector3(
                nodes.East[b].Raw * MetresPerTile, 0f, -nodes.North[b].Raw * MetresPerTile);

            if (from.IsEqualApprox(to))
            {
                continue;
            }

            (float wide, Color paint) = (RoadKind)segments.Kind[slot] switch
            {
                RoadKind.Arterial => (ArterialWidthMetres, Arterial),
                RoadKind.FootPath => (FootPathWidthMetres, Flagstone),
                _ => (CarriagewayWidthMetres, Carriageway),
            };

            // Scaled along its own length and turned to face the other end: one unit cube per
            // Segment, which is why the Road Graph costs one draw call however large the city is.
            //
            // ⚠ THE SCALE IS COMPOSED IN THE SEGMENT'S OWN FRAME AND NOT THE WORLD'S. Basis.Scaled
            // scales the basis in the PARENT frame, so the first spelling gave every east-west
            // Segment 8 m of length and its whole length of width -- and the lattice rendered as
            // north-south lines with the cross-streets missing. Invisible in the ASCII dump, which
            // rasterises the line itself and never asks for a transform.
            var basis = new Basis(Quaternion.FromEuler(
                    new Vector3(0f, Mathf.Atan2(to.X - from.X, to.Z - from.Z), 0f)))
                * Basis.FromScale(new Vector3(wide, 1f, from.DistanceTo(to)));

            // ⚠ LINEAR, because a MultiMesh instance colour is multiplied into albedo in linear
            // space and the constants above are stated in sRGB like every other palette entry here.
            yield return (
                segments.Rows.IdAt(slot),
                new Transform3D(basis, from.Lerp(to, 0.5f)),
                paint.SrgbToLinear());
        }
    }

    /// <summary>
    /// <b>The pavements</b> — one strip either side of every Segment that carries feet <em>and</em>
    /// cars.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE MASK DECIDES AND THE KIND DOES NOT, AND THAT IS THE WHOLE ROW.</b>
    /// <see cref="RoadSegmentTable.ModesForward"/> and <c>ModesBackward</c> are
    /// <c>(saved AND hashed)</c> per Segment, and <c>TravelMode.cs</c> says in as many words that
    /// <b>this is where Severance lives</b>: an Arterial's Arcs carry <see cref="TravelMode.Car"/>
    /// and not <see cref="TravelMode.Foot"/>, so a pedestrian route across one does not exist.
    /// ***So the drawing reads the permission rather than the label***, and the three cases fall out
    /// of the union of the two masks with nothing invented in between:
    /// </para>
    /// <para>
    /// <b>Foot and Car</b> — a carriageway with a pavement each side. <b>Foot alone</b> — the whole
    /// Segment is already the pavement, which is the <see cref="Flagstone"/>
    /// <see cref="RoadKind.FootPath"/> <see cref="Paving"/> draws, so nothing is added beside it.
    /// <b>Car alone</b> — <b>no pavement at all</b>, because there is nowhere to walk. That last one
    /// is Severance, drawn: an Arterial is a wall of asphalt with no way across it except at a
    /// crossing, and until now the drawing said nothing about it.
    /// </para>
    /// <para>
    /// ⚠ <b>WHAT IS INVENTED IS THE WIDTH AND WHAT IS NOT IS WHICH SEGMENTS HAVE ONE</b>
    /// (<c>plans/0049</c> rule 2). <see cref="FootwayWidthMetres"/> is chosen by eye and is spent
    /// out of <see cref="StreetWidthMetres"/> rather than added to it, so ***the street claims
    /// exactly the ground it claimed before this method existed*** — which is why the row moves no
    /// Building, no Lot pad and no scatter margin.
    /// </para>
    /// <para>
    /// 🔴 <b>IT STOPS AT THE MITRE AND NO LONGER AT A CONSTANT.</b> A Segment runs node to node, so a
    /// pavement drawn its full length would cross the carriageway of every Segment meeting it at
    /// either end — a raised paving bar laid across each junction, on the exact ground a vehicle
    /// drives over. Step 2 trimmed half a carriageway at each end unconditionally, which is right
    /// only where a Street crosses at a right angle; <see cref="Mitre"/> asks what is actually there.
    /// </para>
    /// <para>
    /// ⚠ <b>It is the PAVEMENT and not the whole foot half</b> — <see cref="Kerbs"/> took
    /// <see cref="KerbWidthMetres"/> out of it against the channel, at the same top, so the two are
    /// flush and the strip simply starts further from the road.
    /// </para>
    /// <para>
    /// ⚠ <b>Two instances a Segment, so the layer is sized for two.</b> A pavement cannot be one box
    /// — it is an annulus round the asphalt — and the alternative was a shader on a wider slab, which
    /// buys nothing a second instance does not and costs a material nothing else here needs.
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)>
        Footways()
    {
        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (!segments.Rows.IsLive(slot)
                || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            if ((Modes(segments, slot) & (TravelMode.Foot | TravelMode.Car))
                != (TravelMode.Foot | TravelMode.Car))
            {
                continue;
            }

            var from = new Vector3(
                nodes.East[a].Raw * MetresPerTile, 0f, -nodes.North[a].Raw * MetresPerTile);
            var to = new Vector3(
                nodes.East[b].Raw * MetresPerTile, 0f, -nodes.North[b].Raw * MetresPerTile);

            float half = ((RoadKind)segments.Kind[slot] == RoadKind.Arterial
                ? ArterialWidthMetres
                : CarriagewayWidthMetres) * 0.5f;

            float length = from.DistanceTo(to);
            float yaw = Mathf.Atan2(to.X - from.X, to.Z - from.Z);
            var run = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));

            // Perpendicular to the run, in the world's frame. Composing the offset here rather than
            // as a local translation on the basis keeps the two strips symmetric about the centre
            // line whichever way round the Segment's nodes were created.
            var across = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));
            Color paint = Flagstone.SrgbToLinear();
            ulong id = segments.Rows.IdAt(slot);

            for (int lateral = 1; lateral >= -1; lateral -= 2)
            {
                Vector3 side = across * lateral;

                // The pavement's own two edges, measured out from the carriageway: it begins where
                // the kerb band ends and ends at the whole footway's width.
                float low = Trim(
                    Mitre(a, slot, run, side, half, KerbWidthMetres, FootwayWidthMetres), length);
                float high = length - Trim(
                    Mitre(b, slot, -run, side, half, KerbWidthMetres, FootwayWidthMetres), length);

                if (high <= low)
                {
                    continue;
                }

                var basis = new Basis(Quaternion.FromEuler(new Vector3(0f, yaw, 0f)))
                    * Basis.FromScale(new Vector3(PavementWidthMetres, 1f, high - low));

                Vector3 middle = from + (run * ((low + high) * 0.5f))
                    + (side * (half + KerbWidthMetres + (PavementWidthMetres * 0.5f)));

                yield return (id, new Transform3D(basis, middle), paint);
            }
        }
    }

    /// <summary>
    /// <b>Every Address in the city, on the ground</b> — the kerb band along each channel, broken
    /// wherever a Lot steps onto it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A DROPPED KERB IS AN ADDRESS DRAWN, AND AN ADDRESS HAD NEVER BEEN DRAWN AT ALL.</b>
    /// <c>CONTEXT.md</c> → Address: the word was chosen <i>"because a street address is literally
    /// this triple — a distance along a street plus an odd or even side"</i>. Both halves are the
    /// city's own: <see cref="LotTable.FrontageOffset"/> is the distance and
    /// <see cref="LotTable.Side"/> is the side, one derived and one <c>(saved AND hashed)</c>. Until
    /// this method there was nothing anywhere in the picture at the place a Lot actually meets its
    /// Street — the Building was drawn on its <em>parcel</em>, and the parcel is a different
    /// quantity.
    /// </para>
    /// <para>
    /// ⚠ <b>IT IS VISIBLE FROM THE HALF OF THE COMPASS THE DOOR IS NOT.</b> <c>plans/0049</c>
    /// <b>F30</b> — the front door reaches the shader as the street face's outward normal, the
    /// camera's pitch never moves, and <em>the fronts facing away are simply not in the frame</em>.
    /// A mark on the ground has no facing. ***So the two marks say one thing and fail on opposite
    /// halves of the city***, which is why this is worth drawing beside a door that already exists.
    /// </para>
    /// <para>
    /// 🔴 <b>THE DROP AND THE DOOR DO NOT AGREE ABOUT WHERE ALONG THE FRONTAGE THE ENTRANCE IS, AND
    /// THE DROP IS THE ONE THAT IS RIGHT.</b> <c>buildings.gdshader</c> puts the door on the middle
    /// bay of the wall plus a draw of one bay; this puts the drop on the Address. They coincide only
    /// where the Address happens to sit at the middle of its own parcel, and it generally does not —
    /// a face's ground divides evenly among the Addresses <em>on it</em>
    /// (<c>BlockPatterns.Carve</c>) while an offset is the midpoint of an equal share of the
    /// <em>whole</em> face (<c>Frontage.OffsetOf</c>), and those are two different partitions.
    /// ***The door's bay is the shell's invention and the drop's offset is the city's number***, so
    /// nothing here moves to meet the shader. Measured and filed as <c>plans/0049</c> <b>F61</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>THE SIDE IS READ FROM A CANONICAL DIRECTION AND NOT FROM THE NODE ORDER.</b>
    /// <see cref="StreetSide"/> is <em>left of A→B</em>, and <c>BlockPatterns.SideOf</c> states it in
    /// world terms — a horizontal Segment runs eastward so Left is its north side. The generator
    /// happens to create every Street from its lower intersection, so A→B already runs east or
    /// north; <b>relying on that would be relying on the generator's loop order</b>, and
    /// <c>CommandKind.Connect</c> is a second writer. So the run is oriented here, and the two
    /// strips <see cref="Footways"/> draws stay symmetric precisely so that they never had to care.
    /// </para>
    /// <para>
    /// ⚠ <b>Overlapping drops MERGE rather than stack.</b> <see cref="KerbDropLengthMetres"/> is
    /// invented, so two Addresses closer together than it would otherwise draw two boxes on one patch
    /// of stone — and a merged run is both cheaper and what a pair of adjacent driveways looks like.
    /// ***The cost is that a drop is no longer one-to-one with an Address in the <c>draw</c> list***,
    /// which is stated at <see cref="_kerbIds"/> rather than left for a reader to discover by
    /// counting.
    /// </para>
    /// <para>
    /// ⚠ <b>Same <see cref="Mitre"/> as the pavement, one step nearer the road</b>, so a band never
    /// runs across the junction it ends at, and a drop is clipped to the band rather than allowed to
    /// escape it.
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)>
        Kerbs()
    {
        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;

        // Every Address in the city, in Segment order, so the walk below merges against the Segment
        // loop rather than searching. Built per repave, which is rare -- Pave() runs on a road edit
        // and not on a frame.
        List<(int Segment, byte Side, float Along)> addresses = Addresses();

        int at = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            while (at < addresses.Count && addresses[at].Segment < slot)
            {
                at++;
            }

            int first = at;

            while (at < addresses.Count && addresses[at].Segment == slot)
            {
                at++;
            }

            if (!segments.Rows.IsLive(slot)
                || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            if ((Modes(segments, slot) & (TravelMode.Foot | TravelMode.Car))
                != (TravelMode.Foot | TravelMode.Car))
            {
                continue;
            }

            var from = new Vector3(
                nodes.East[a].Raw * MetresPerTile, 0f, -nodes.North[a].Raw * MetresPerTile);
            var to = new Vector3(
                nodes.East[b].Raw * MetresPerTile, 0f, -nodes.North[b].Raw * MetresPerTile);

            // A→B oriented east, then north -- see the remark. World +Z is SOUTH, so northward is
            // the direction in which Z DECREASES. ⚠ The NODE SLOTS turn over with the ends, because
            // the mitre below is asked of the node the run leaves.
            if (to.X < from.X || (Mathf.IsEqualApprox(to.X, from.X) && to.Z > from.Z))
            {
                (from, to) = (to, from);
                (a, b) = (b, a);
            }

            float carriageway = (RoadKind)segments.Kind[slot] == RoadKind.Arterial
                ? ArterialWidthMetres
                : CarriagewayWidthMetres;

            float half = carriageway * 0.5f;
            float length = from.DistanceTo(to);
            float yaw = Mathf.Atan2(to.X - from.X, to.Z - from.Z);
            var run = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));

            // +across is LEFT of A→B once the run is oriented: for an eastward Segment yaw is a
            // quarter turn and this is world north, and BlockPatterns.SideOf says Left is the north
            // side of one. For a northward Segment it comes out west, which is that same rule's
            // other half.
            var across = new Vector3(Mathf.Cos(yaw), 0f, -Mathf.Sin(yaw));

            ulong id = segments.Rows.IdAt(slot);

            for (byte side = 0; side <= 1; side++)
            {
                Vector3 lateral = across * (side == (byte)StreetSide.Left ? 1f : -1f);

                // The band's own two edges: it starts at the channel and is KerbWidthMetres deep.
                float low = Trim(
                    Mitre(a, slot, run, lateral, half, 0f, KerbWidthMetres), length);
                float high = length - Trim(
                    Mitre(b, slot, -run, lateral, half, 0f, KerbWidthMetres), length);

                if (high <= low)
                {
                    continue;
                }

                foreach ((Transform3D where, Color paint) in Kerb(
                    from, run, lateral, yaw, carriageway, low, high,
                    addresses, first, at, side))
                {
                    yield return (id, where, paint);
                }
            }
        }
    }

    /// <summary>
    /// <b>THE JUNCTION</b> — how far from a node a strip down one side of a Segment stops, given
    /// what else actually meets the Segment there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT IS THE MITRE — WHERE THIS STRIP'S EDGE CROSSES THE NEIGHBOURING ARM'S.</b> With the
    /// node at the origin, this Segment leaving along <b>a</b> and the neighbour along <b>b</b> at
    /// an angle <c>φ</c> measured round <em>toward the side being drawn</em>, two lines at offsets
    /// <c>p</c> and <c>q</c> from their own centrelines meet at
    /// <c>(p·cos φ + q) / sin φ</c> along <b>a</b>. That one expression is the whole of this row's
    /// fourth step, and every case below is it evaluated rather than a case handled:
    /// </para>
    /// <para>
    /// <b>φ = 90°, two Streets</b> — <c>q</c>, half a carriageway: the constant step 2 hard-coded,
    /// which is why an interior crossroads was already right. <b>φ = 90°, an Arterial</b> — seven
    /// metres, not two and a half; step 2 laid <b>4.5 m of raised kerb and pavement across the
    /// Arterial's asphalt</b> at every such node, ***the pavement drawn on top of the motorway***.
    /// <b>φ = 180°</b>, the arm straight ahead — <c>sin φ</c> is zero, there is no intersection, and
    /// the two strips meet at the node: this is the through side of a T-junction and of every
    /// straight-through node, where step 2 bit 2.5 m out of a pavement <em>nothing crosses</em>.
    /// <b>φ &gt; 180°</b>, the nearest arm round that way is behind — <c>sin φ</c> is negative, the
    /// mitre is <em>past</em> the node, and the strip wraps the outside of a bend. <b>No arm at
    /// all</b> — zero, and the strip ends flush.
    /// </para>
    /// <para>
    /// 🔴 <b>A FOOTPATH IS NOT A CARRIAGEWAY AND IS NOT COUNTED.</b> The question this asks is
    /// <em>what drives across the ground my pavement wants</em>, so the arm list is filtered on
    /// <see cref="TravelMode.Car"/> — and a pavement therefore runs straight past the mouth of a
    /// cut-through instead of being broken by it. <b>That is the same column step 2 read</b>, asked
    /// at the node rather than along the Segment.
    /// </para>
    /// <para>
    /// ⚠ <b>NEAR EDGE TO NEAR EDGE, which is what <paramref name="near"/> and
    /// <paramref name="far"/> are for.</b> On the side an arm crosses, the corner is <em>inside</em>
    /// both strips and it is their inner edges that meet; where the nearest arm is behind, the
    /// corner is <em>outside</em> both and it is their outer edges. Getting this wrong is not
    /// cosmetic: with the inner edge used on both sides the pavement of one arm is drawn over the
    /// kerb of the next, coplanar and in a different colour, on all four corners of every crossroads
    /// in the city.
    /// </para>
    /// <para>
    /// ⚠ <b>The neighbour's step is zero when the neighbour has no footway</b>, because an Arterial
    /// is asphalt out to its edge and nothing further. So a Street's pavement dead-ends flush
    /// against it — <b>Severance, at the junction</b>, and the one place in the drawing where you
    /// can see that the way across is not here.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads the city's own adjacency and builds none.</b>
    /// <see cref="RoadNodeTable.ArcStart"/> and <c>ArcCount</c> are a CSR slice over
    /// <see cref="RoadArcs"/>, <c>(derived AND rebuilt)</c> — so the arms at a node are a lookup,
    /// and the alternative was a shell-side index of Segments by node, which is a second copy of a
    /// structure the core already keeps correct.
    /// </para>
    /// <para>
    /// ✅ <b>AT A RIGHT ANGLE THE FOUR STRIPS TILE THE CORNER EXACTLY, WITH NO HOLE AND NO OVERLAP</b>
    /// — worked through rather than assumed, because the near-edge rule above is the only reason it
    /// comes out that way and the obvious spelling does not. Inside a crossroads the crossing arm's
    /// kerb owns the square its own band sweeps and this Segment's pavement starts beyond it;
    /// outside a bend both strips reach past the node far enough to cover the square between them,
    /// and the kerb stops exactly where the neighbour's pavement begins. ⚠ <b>A right angle</b>: an
    /// oblique junction leaves slivers, because a strip is a box and ends square rather than on the
    /// mitre line. Streets are grid-snapped by definition (<see cref="RoadKind.Street"/>) and an
    /// Arterial carries no footway, so every corner a footway can turn today is a right angle.
    /// </para>
    /// <para>
    /// ⚠ <b>What is not drawn is the END of a dead end</b>, and it is generated rather than
    /// hypothetical: a node with one arm has no neighbour to mitre against, so the two strips stop
    /// flush across the Segment's end and leave the square between them bare. Counted off the
    /// <c>draw</c> list, <c>rulesets/pictured.toml</c> has <b>0</b> and <c>rulesets/severance.toml</c>
    /// has <b>22</b> — the Street stubs its Arterials cut off. Closing one is a cap and not a mitre,
    /// so it is a third shape rather than another evaluation of the expression above.
    /// </para>
    /// </remarks>
    /// <param name="node">The node's slot — the end the strip is being measured from.</param>
    /// <param name="self">This Segment's slot, which is the one arm at the node to ignore.</param>
    /// <param name="outward">Unit vector, this Segment's direction <em>leaving</em> the node.</param>
    /// <param name="side">Unit vector, perpendicular, pointing at the side being drawn.</param>
    /// <param name="half">Half this Segment's carriageway, in metres.</param>
    /// <param name="near">The strip's inner edge, out from the carriageway's edge.</param>
    /// <param name="far">The strip's outer edge, out from the carriageway's edge.</param>
    private float Mitre(
        int node, int self, Vector3 outward, Vector3 side, float half, float near, float far)
    {
        RoadNodeTable nodes = _world.Roads.Nodes;
        RoadArcs arcs = _world.Roads.Arcs;
        RoadSegmentTable segments = _world.Roads.Segments;

        int start = nodes.ArcStart[node];
        int end = Math.Min(start + nodes.ArcCount[node], arcs.Count);
        float turn = Mathf.Tau;
        int nearest = -1;

        for (int arc = start; arc < end; arc++)
        {
            int other = arcs.Segment[arc];

            if (other == self || (Modes(segments, other) & TravelMode.Car) == 0)
            {
                continue;
            }

            int target = arcs.Target[arc];
            var away = new Vector3(
                (nodes.East[target].Raw - nodes.East[node].Raw) * MetresPerTile,
                0f,
                -(nodes.North[target].Raw - nodes.North[node].Raw) * MetresPerTile);

            if (away.LengthSquared() <= 0f)
            {
                continue;
            }

            away = away.Normalized();

            // The bearing from this Segment round TOWARD the side being drawn, in [0, τ). The arm
            // with the smallest one is the neighbour this side's edge actually runs into; anything
            // further round is hidden behind it.
            float bearing = Mathf.Atan2(side.Dot(away), outward.Dot(away));

            if (bearing < 0f)
            {
                bearing += Mathf.Tau;
            }

            if (bearing < turn)
            {
                turn = bearing;
                nearest = other;
            }
        }

        float sine = Mathf.Sin(turn);

        // No arm, or one straight ahead or directly behind: parallel edges have no intersection and
        // the two strips simply meet at the node. The tolerance also catches the near-parallel case,
        // where the true mitre runs off down the Segment.
        if (nearest < 0 || Mathf.Abs(sine) < 0.05f)
        {
            return 0f;
        }

        bool crosses = sine > 0f;
        float mine = half + (crosses ? near : far);
        float theirs = ((RoadKind)segments.Kind[nearest] == RoadKind.Arterial
            ? ArterialWidthMetres
            : CarriagewayWidthMetres) * 0.5f;

        if ((Modes(segments, nearest) & TravelMode.Foot) != 0)
        {
            theirs += crosses ? near : far;
        }

        return ((mine * Mathf.Cos(turn)) + theirs) / sine;
    }

    /// <summary>Hold a mitre inside the Segment it belongs to.</summary>
    /// <remarks>
    /// ⚠ <b>The bound is symmetric and the mitre is not.</b> A mitre may legitimately be negative —
    /// that is the outside of a bend — so this clamps the magnitude rather than flooring at zero,
    /// and the caller drops the strip outright when the two ends cross over.
    /// </remarks>
    private static float Trim(float mitre, float length) =>
        Mathf.Clamp(mitre, -length * 0.5f, length * 0.5f);

    /// <summary>
    /// A Segment's own travel modes — <b>the union of its two directions</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A union rather than one direction</b>, because a one-way street carries cars one way and
    /// pedestrians both, and the question every caller here asks is <em>what is this Segment for</em>
    /// rather than <em>what may leave this end</em>.
    /// </remarks>
    private static TravelMode Modes(RoadSegmentTable segments, int slot) =>
        (TravelMode)(segments.ModesForward[slot] | segments.ModesBackward[slot]);

    /// <summary>
    /// One side of one Segment's kerb — the band runs and the drops between them, in order along
    /// the Street.
    /// </summary>
    /// <remarks>
    /// <b>A cursor walk and not an interval library.</b> The Addresses arrive sorted, so the band is
    /// whatever the cursor has not reached and a drop is whatever the next Address claims; a drop
    /// starting behind the cursor extends the one already open, which is the merge.
    /// </remarks>
    private static System.Collections.Generic.IEnumerable<(Transform3D Where, Color What)> Kerb(
        Vector3 from, Vector3 run, Vector3 across, float yaw, float carriageway,
        float low, float high,
        List<(int Segment, byte Side, float Along)> addresses, int first, int last, byte side)
    {
        float cursor = low;
        float half = KerbDropLengthMetres * 0.5f;

        for (int index = first; index <= last; index++)
        {
            // The sentinel pass past the end closes the band off at `high`; every other pass opens
            // a drop first. Written as one loop because the tail run is the same expression.
            bool closing = index == last;

            float start = high;
            float end = high;

            if (!closing)
            {
                if (addresses[index].Side != side)
                {
                    continue;
                }

                start = Mathf.Clamp(addresses[index].Along - half, low, high);
                end = Mathf.Clamp(addresses[index].Along + half, low, high);

                if (end <= cursor)
                {
                    continue;
                }
            }

            if (start > cursor)
            {
                yield return Piece(
                    from, run, across, yaw, cursor, start, KerbWidthMetres,
                    (carriageway * 0.5f) + (KerbWidthMetres * 0.5f), KerbTopMetres);

                cursor = start;
            }

            if (closing)
            {
                yield break;
            }

            // THE DROP, and it is wider than the band it replaces because it reaches across the
            // channel. Its centre therefore steps toward the road by half the bite.
            yield return Piece(
                from, run, across, yaw, cursor, end, KerbWidthMetres + KerbDropBiteMetres,
                (carriageway * 0.5f) + (KerbWidthMetres * 0.5f) - (KerbDropBiteMetres * 0.5f),
                KerbDropTopMetres);

            cursor = end;
        }
    }

    /// <summary>One box of kerb, from <paramref name="start"/> to <paramref name="end"/> along a run.</summary>
    /// <remarks>
    /// ⚠ <b>The Y scale is a SHARE OF THE MESH and not a height in metres.</b> The kerb layer's box
    /// is 0.3 m tall and drawn centred on <c>y = 0</c>, so a roof at <paramref name="top"/> wants a
    /// scale of <c>top / 0.15</c> and an origin of zero — which is <see cref="Edge"/>'s finding kept
    /// rather than re-learned: ***a height derived from a scale rather than from the mesh it scales
    /// is a number about the wrong object.***
    /// </remarks>
    private static (Transform3D Where, Color What) Piece(
        Vector3 from, Vector3 run, Vector3 across, float yaw,
        float start, float end, float wide, float offset, float top)
    {
        const float half = KerbBoxMetres * 0.5f;

        var basis = new Basis(Quaternion.FromEuler(new Vector3(0f, yaw, 0f)))
            * Basis.FromScale(new Vector3(wide, top / half, end - start));

        return (
            new Transform3D(basis, from + (run * ((start + end) * 0.5f)) + (across * offset)),
            Kerbstone.SrgbToLinear());
    }

    /// <summary>Every Lot's Address as a Segment, a side and a distance, sorted for one walk.</summary>
    /// <remarks>
    /// ⚠ <b><see cref="LotTable.FrontageSlot"/> is the Segment's slot PLUS ONE</b>, so that zero
    /// reads as <em>no Street</em> — <c>Frontage.Rebuild</c>'s own convention, and a Lot whose Street
    /// was demolished keeps its Building and loses its Address (<c>adr/0079</c>). ⚠ <b>The offset is
    /// measured from the lattice edge's LOWER end</b>, which is what <c>Frontage.Locate</c> computes
    /// and why <see cref="Kerbs"/> orients the run rather than trusting the node order.
    /// </remarks>
    private List<(int Segment, byte Side, float Along)> Addresses()
    {
        LotTable lots = _world.Lots;
        List<(int Segment, byte Side, float Along)> found = [];

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.FrontageSlot[slot] <= 0)
            {
                continue;
            }

            found.Add((
                lots.FrontageSlot[slot] - 1,
                lots.Side[slot],
                lots.FrontageOffset[slot].Raw * MetresPerTile));
        }

        found.Sort(static (one, other) =>
        {
            int by = one.Segment.CompareTo(other.Segment);

            if (by != 0)
            {
                return by;
            }

            by = one.Side.CompareTo(other.Side);

            return by != 0 ? by : one.Along.CompareTo(other.Along);
        });

        return found;
    }

    /// <summary>The kerb layer's mesh height, in metres. <b>Read by <see cref="Piece"/>'s scale.</b></summary>
    private const float KerbBoxMetres = 0.3f;

    /// <summary>How high the kerb band's top is — <b>flush with the pavement behind it</b>.</summary>
    private const float KerbTopMetres = 0.15f;

    /// <summary>
    /// How high a <b>dropped</b> kerb's top is — the carriageway's <b>0.05 m</b>, plus two
    /// centimetres so the two faces are not coplanar and the depth test has something to choose on.
    /// </summary>
    private const float KerbDropTopMetres = 0.07f;

    /// <summary>Writes the carriageway, the pavements and the kerbs into their three layers.</summary>
    /// <remarks>
    /// ⚠ <b>Three layers rather than one, and the reason is the draw list.</b> A pavement is
    /// distinguishable from a carriageway by its width and its colour, which is a filter a reader
    /// has to know to apply; a layer of its own is a row somebody can count by name
    /// (<c>plans/0048</c> §5). It is also what makes the pavements a thing that can be counted
    /// against <c>--roads</c>' census of what carries feet.
    /// </remarks>
    private void Pave()
    {
        Fill(_roads, Paving(), _roadIds);
        Fill(_footways, Footways(), _footwayIds);
        Fill(_kerbs, Kerbs(), _kerbIds);
    }

    /// <summary>The Cell lattice, drawn over the ground the city stands on.</summary>
    /// <remarks>
    /// <para>
    /// <b>A Cell is 32×32 Tiles — 128 m — and it is the unit nearly every derived quantity in the
    /// simulation is denominated in</b>: a Map Layer holds one value per Cell, Stress is per Cell,
    /// the residency index buckets per Cell. So this is the answer to <i>how much of the city does
    /// one number cover?</i>, which is a question you cannot ask a street.
    /// </para>
    /// <para>
    /// ⚠ <b>It is deliberately NOT the street lattice</b>, and the two are easy to confuse because
    /// at the shipped <c>block_tiles = 32</c> they coincide exactly. Turn <c>block_tiles</c> to 16
    /// in the tuner and they part company — four blocks to a Cell — which is the clearest available
    /// demonstration that the road generator and the Cell grid have nothing to do with each other.
    /// </para>
    /// <para>
    /// ⚠ <b>It spans the Lots and not the map.</b> The map is 512 Cells a side, so a full lattice
    /// would be 1,026 lines for a city that occupies eleven of them.
    /// </para>
    /// </remarks>
    private void Cells()
    {
        const float width = 2f;
        const float above = 0.2f;

        int first = Mathf.FloorToInt(_laid.Position.X / CellMetres);
        int last = Mathf.CeilToInt(_laid.End.X / CellMetres);
        int lowest = Mathf.FloorToInt(_laid.Position.Y / CellMetres);
        int highest = Mathf.CeilToInt(_laid.End.Y / CellMetres);

        float left = first * CellMetres;
        float right = last * CellMetres;
        float bottom = lowest * CellMetres;
        float top = highest * CellMetres;

        int drawn = 0;

        for (int column = first; column <= last && drawn + 1 < _cells.Multimesh.InstanceCount;
             column++)
        {
            _cells.Multimesh.SetInstanceTransform(
                drawn++,
                new Transform3D(
                    Basis.FromScale(new Vector3(width, 1f, top - bottom)),
                    new Vector3(column * CellMetres, above, -(bottom + top) * 0.5f)));
        }

        for (int row = lowest; row <= highest && drawn + 1 < _cells.Multimesh.InstanceCount; row++)
        {
            _cells.Multimesh.SetInstanceTransform(
                drawn++,
                new Transform3D(
                    Basis.FromScale(new Vector3(right - left, 1f, width)),
                    new Vector3((left + right) * 0.5f, above, -row * CellMetres)));
        }

        _cells.Multimesh.VisibleInstanceCount = drawn;
    }
}
