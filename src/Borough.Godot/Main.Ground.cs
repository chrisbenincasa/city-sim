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
    /// <remarks>
    /// ⚠ <b>THE BLOCK'S OWN TWO EXTENTS AND NOT ONE SPAN.</b> The ghost is what row 22 built so a
    /// player can see where a click lands, and a ghost drawn at the mean size on a lattice whose
    /// blocks are not the mean is a cursor that lies about the ground it covers. <c>plans/0045</c>
    /// row 25.
    /// </remarks>
    private static Transform3D Block(
        (Tiles East, Tiles North) at, BlockLattice lattice, float height)
    {
        int column = lattice.LineAt(at.East.Raw);
        int row = lattice.LineAt(at.North.Raw);

        float wide = lattice.WidthOf(column) * MetresPerTile;
        float deep = lattice.WidthOf(row) * MetresPerTile;
        float west = lattice.EdgeOf(column) * MetresPerTile;
        float south = lattice.EdgeOf(row) * MetresPerTile;

        return new Transform3D(
            Basis.FromScale(new Vector3(wide, height, deep)),
            new Vector3(west + (wide * 0.5f), height * 0.5f, -(south + (deep * 0.5f))));
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
    /// </remarks>
    private Transform3D Edge((Tiles East, Tiles North) at, BlockLattice lattice)
    {
        float thin = RoadWidthMetres * 0.5f;
        (int column, int row, StreetAxis axis) = _world.Roads.Streets.NearestEdge(at.East, at.North);

        // ⚠ THE EDGE'S OWN LENGTH. It was one `span` for both axes and both ends, which draws a
        // ghost the wrong length on any lattice whose lines are not evenly spaced -- and the ghost
        // is the whole of what row 22 built. plans/0045 row 25.
        float west = lattice.EdgeOf(column) * MetresPerTile;
        float south = lattice.EdgeOf(row) * MetresPerTile;

        return axis == StreetAxis.East
            ? new Transform3D(
                Basis.FromScale(
                    new Vector3(lattice.WidthOf(column) * MetresPerTile, AimThickMetres, thin)),
                new Vector3(
                    west + (lattice.WidthOf(column) * MetresPerTile * 0.5f), AimAboveMetres, -south))
            : new Transform3D(
                Basis.FromScale(
                    new Vector3(thin, AimThickMetres, lattice.WidthOf(row) * MetresPerTile)),
                new Vector3(
                    west, AimAboveMetres, -(south + (lattice.WidthOf(row) * MetresPerTile * 0.5f))));
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
    /// ⚠ <b>A CROSSING IS NOT DRAWN AND THAT IS A REFUSAL RATHER THAN AN OMISSION.</b>
    /// <c>[roads] foot_crossing_every</c> is read only where an Arterial severs the lattice, every
    /// shipped file states <c>arterial_count = 0</c>, and so <b>no crossing exists in the model
    /// anywhere</b>. Drawing one would be inventing a fact rather than under-drawing a held one —
    /// <c>plans/0049</c> <b>F51</b>.
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
                _ => (RoadWidthMetres, Carriageway),
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

    /// <summary>Writes <see cref="Paving"/> into the road layer.</summary>
    private void Pave() => Fill(_roads, Paving(), _roadIds);

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
