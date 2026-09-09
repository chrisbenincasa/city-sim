using System;
using System.Collections.Generic;
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

// ---- the massing -- a Building's shape, its roof, and the instance buffers all of it is written into 
//
// Buildings() and Wings() derive a shape from the city and invent nothing: every figure they use
// comes off LotTable or BuildingTable. Cap/CapFor/CapBasis choose a roof from the footprint that
// is already there, which is why they are static and take no world.
//
// Fill() is here rather than with the layers because a MultiMesh's buffer is a massing problem:
// what it costs is the transform written per instance.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    private (ulong Tick, Wash Washing) _built = (ulong.MaxValue, Wash.None);
    private int _drawnBuildings, _vacantLots, _underWater;

    /// <summary>What one Building's walls are, which is <see cref="Standing"/> and a wander.</summary>
    /// <remarks>
    /// ⚠ <b>Value and warmth, and not hue.</b> Moving the hue gives a painted street; moving how
    /// light a wall is and how warm gives a street of the same stone weathered differently, which
    /// is what a terrace is. Both draws come off the same scramble the massing took, so a Building
    /// keeps its colour for as long as it stands and a rebuilt one on the same Lot is visibly new.
    /// </remarks>
    private static Color Rendered(ulong shape)
    {
        float shade = 1f + ((((shape >> 44) & 0xFu) / 15f) - 0.5f) * (RenderedSpread * 2f);
        float warm = 1f + ((((shape >> 37) & 7u) / 7f) - 0.5f) * 0.09f;

        return new Color(
            Mathf.Min(Standing.R * shade * warm, 1f),
            Standing.G * shade,
            Standing.B * shade / warm);
    }

    /// <summary>What one Building's roof is covered in — a tone, and a jitter on it.</summary>
    /// <remarks>
    /// ⚠ <b>Weighted, and the weighting is the whole point.</b> A fair coin between two tones
    /// gives a chequerboard; one roofing in five puts a slate roof in a street of tile, which is
    /// what a street of them looks like. It is <see cref="DepthFillLow"/>' class of thing and
    /// draws on no <c>purpose_tag</c>.
    /// </remarks>
    private static Color Slate(ulong shape)
    {
        Color tone = Roofs[((shape >> 24) & 7u) == 0u ? 1 : 0];
        float shade = 0.86f + (((shape >> 28) & 0xFu) / 15f * 0.28f);

        return new Color(tone.R * shade, tone.G * shade, tone.B * shade);
    }

    /// <summary>Every standing Building, at its Lot, at the size its kind implies.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>THE HEIGHT IS THE LOT'S STOREY COUNT AND THE JITTER IS THE RENDERER'S.</b> The block's
    /// pattern states how tall to build (<c>plans/0053</c>), so the shell reads a number the city
    /// already holds rather than inferring one. 🔴 <b>IT READ <c>[[building]] occupants</c> UNTIL
    /// step 3 RETIRED THAT KEY</b>, and the read did not fail — <c>BuildingKindDefinition</c> simply
    /// stopped being written and every kind answered <b>zero</b>, so the whole city would have drawn
    /// one storey tall. ***A dead read returns a plausible number***, which is why the member was
    /// deleted rather than left defaulting. ⚠ <b>The earlier correction here is kept because its
    /// lesson outlived its subject</b>: this remark once said *every shipped kind declares 3* and the
    /// files said <b>4</b> in 28 declarations, 3 in three and 1 in three — ***a number quoted from
    /// memory about a file nobody re-read.***
    /// </para>
    /// <para>
    /// <b>The jitter is <see cref="DepthFillLow"/>' class of thing and is labelled as one</b> — a
    /// thickness the city does not have, invented so the picture reads as a city rather than as a
    /// bar chart. It is keyed on the Building's monotonic row id, so a Building keeps its shape for
    /// as long as it stands and a rebuilt one on the same Lot is visibly a different building.
    /// ⚠ <b>It draws on no <c>purpose_tag</c> and must not</b>: the simulation's stream is for
    /// decisions, and a shape nobody in the city can perceive is not one.
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<Massing> Buildings(int only = -1)
    {
        BuildingTable table = _world.Buildings;
        LotTable lots = _world.Lots;
        BlockLattice lattice = _world.Roads.Streets.Lattice;

        for (int slot = only < 0 ? 0 : only; slot < (only < 0 ? table.Rows.SlotCount : only + 1); slot++)
        {
            if (!table.Rows.IsLive(slot) || !lots.Rows.TryResolve(table.Lot[slot], out int lot)) continue;

            // 🔴 THE GROUND IS THE CITY'S NOW AND THE SHELL NO LONGER INVENTS IT
            // (plans/0052 stage 1). A Lot carries a PARCEL -- a rectangle of Tiles derived on the
            // epoch from the block's own pattern, and a partition of that block by construction.
            // FIVE separate inventions stood here: a setback, a stretch of kerb, a corner reserve,
            // a depth, and a re-centring onto the stretch. They are one read.
            int wideTiles = lots.ParcelWide[lot].Raw;
            int deepTiles = lots.ParcelDeep[lot].Raw;

            // ⚠ NO GROUND IS THE GEOMETRY REPORTING RATHER THAN FAILING. A Lot whose Street is gone
            // keeps its Building and loses its Address (adr/0079) and therefore its parcel; and a
            // pattern that carries no Building on this face leaves its Lots as ADDRESSES WITH
            // NOWHERE TO STAND, which is the block saying it was subdivided on four faces when it
            // holds two (plans/0049 F21). Drawing a sliver would be drawing a Building the block
            // cannot hold.
            if (wideTiles <= 0 || deepTiles <= 0)
            {
                continue;
            }

            // 🔴 THE FOOTPRINT, READ AND NOT DERIVED. Where the parcel is the Lot's holding, this
            // is the part with a wall on it -- the same rectangle World.CreateBuilding seals, so the
            // drawing and the Sealing Layer cannot disagree about the same building. The centre and
            // the plan both fall straight out of it and there is no draw left in this block.
            int footWide = lots.FootprintWide[lot].Raw;
            int footDeep = lots.FootprintDeep[lot].Raw;

            if (footWide <= 0 || footDeep <= 0)
            {
                continue;
            }

            float east = (lots.FootprintEast[lot].Raw + (footWide * 0.5f)) * MetresPerTile;
            float north = (lots.FootprintNorth[lot].Raw + (footDeep * 0.5f)) * MetresPerTile;
            float eastWest = footWide * MetresPerTile;
            float southNorth = footDeep * MetresPerTile;

            var side = (StreetSide)lots.Side[lot];

            // Which of the parcel's two axes runs ALONG the Street. Read the way
            // BlockPatterns.SideOf writes it: on a horizontal Street Left is the north side, on a
            // vertical one Right is the east side.
            // ⚠ ON A LINE, which is what the modulo was asking. It reads the same on an evenly
            // spaced lattice and is the question rather than an arithmetic that answers it.
            bool horizontal = lattice.Nominal > 0
                && lattice.EdgeOf(lattice.LineAt(lots.North[lot].Raw)) == lots.North[lot].Raw;

            float along = horizontal ? eastWest : southNorth;
            float deep = horizontal ? southNorth : eastWest;

            if (along < MinFrontageMetres)
            {
                continue;
            }

            ulong id = table.Rows.IdAt(slot);
            ulong shape = Scramble(id);

            byte kind = table.Kind[slot];

            // 🔴 THE HEIGHT IS THE LOT'S OWN STOREY COUNT NOW, AND IT WAS THE KIND'S OCCUPANCY
            // (plans/0053 step 3). That derivation had the shell reading a Ruleset number to guess
            // at a shape; the block's pattern states the storeys directly, and a Building's floor
            // area -- the thing its occupancy is now DERIVED from -- is this number times the
            // footprint. So the picture and the capacity are the same two multiplicands rather than
            // two guesses that happened to agree. 🔴 AND THE SHELL DID NOT COMPILE UNTIL THIS EDIT:
            // step 3 deleted `BuildingKindDefinition.Occupants` outright, so the three reads of it
            // in this file were a build error nobody saw. Borough.Godot is NOT IN THE SOLUTION, so
            // `dotnet build` and the whole 2,679-test lane were green over a shell that could not
            // start. ***A project outside the build is a project outside every gate*** -- see the
            // `drive` skill's F26, which is this failure arriving through a stale binary instead.
            int storeys = Math.Max(1, (int)lots.Storeys[lot]);

            // 🔴 THE HEIGHT IS THE STOREY COUNT AND NOTHING ELSE NOW. A `* (0.55f + draw * 1.3f)`
            // stood on this line -- 0.55x to 1.85x, which at the ladder's top is plus or minus three
            // storeys -- and it was inherited from the era when the height was INFERRED from a
            // kind's `occupants` and there was no storey count to draw. There is one now, and
            // LotRuleset.StoreysOn's own remark says what this jitter was doing to it: ***one storey
            // of variation and no more, because two storeys of jitter would blur the ladder, which
            // is the thing the height is supposed to say***. The shell was applying six.
            //
            // ⚠ TWO MECHANISMS BOTH CLAIMED TO BE WHAT STOPS A BLOCK READING AS A WALL, which is
            // plans/0012 Cause 1 arriving in geometry: the city's is a deterministic draw of one
            // storey off the parcel's corner, and the shell's was a continuous multiplier that
            // swamped it. The city's is the one that survives, because it is the one a Building's
            // FLOOR AREA -- and therefore its occupancy -- is derived from.
            float tall = storeys * StoreyMetres;

            // The long side runs ALONG the Street, which is what makes a row of them read as a
            // street rather than as a field of blocks -- so the plan is swapped with the axis the
            // setback above already had to know about.
            Vector3 plan = horizontal
                ? new Vector3(along, tall, deep)
                : new Vector3(deep, tall, along);

            // Put ridges along the long axis so the roof crosses the shorter span.
            bool crossed = deep > along;
            float slope = crossed ? along : deep;
            float ridge = crossed ? deep : along;

            float wanted = slope * (RoofRiseLow
                + (((shape >> 40) & 0xFFu) / 255f * (RoofRiseHigh - RoofRiseLow)));

            Cap cap = CapFor(tall, along, deep);
            float rise = RoofHeight(cap, wanted);

            // THE OUTBUILDING, standing further from the Street than its Building is. `back` is
            // which way that is -- the same sign the setback above already chose, kept rather than
            // re-derived. ⚠ It is a SHED and not an address: nothing in the city knows it is here,
            // and a Rule can no more reach it than it can reach the roof.
            float back = horizontal
                ? (side == StreetSide.Left ? 1f : -1f)
                : (side == StreetSide.Right ? 1f : -1f);
            bool outhoused = ((shape >> 48) & 3u) != 0u;
            float shed = 4f + (((shape >> 52) & 0xFu) / 15f * 5f);
            float wide = Mathf.Min(along * 0.45f, 14f);
            float gap = 5f;
            float shedHeight = shed * .8f;
            if (_world.Rules.Lots.Plots.Applies(lots.PatternOf(lot)))
            {
                float plotLow = (horizontal ? lots.ParcelNorth[lot].Raw : lots.ParcelEast[lot].Raw) * MetresPerTile;
                float plotHigh = plotLow + (horizontal ? deepTiles : wideTiles) * MetresPerTile;
                float bodyCentre = horizontal ? north : east;
                float space = back > 0 ? plotHigh - (bodyCentre + deep * .5f)
                    : bodyCentre - deep * .5f - plotLow;
                gap = .5f;
                shed = Math.Max(0, Math.Min(shed, space - gap));
                outhoused &= shed >= 2f;
                shedHeight = Math.Min(3f, shed * .8f);
            }
            float off = (deep * 0.5f) + gap + (shed * 0.5f);
            Vector3 hut = horizontal
                ? new Vector3(wide, shedHeight, shed)
                : new Vector3(shed, shedHeight, wide);

            // ⚠ THE TURN IS AN EXCLUSIVE OR AND THAT IS NOT A TRICK. A PrismMesh slopes across its
            // own X and runs its ridge along its own Z, so the quarter turn is owed whenever the
            // ridge is supposed to run east–west — which is a Building on a horizontal Street with
            // its ridge along the kerb, OR one on a vertical Street with its gable turned to face
            // the kerb, and not both at once.
            Basis capped = CapBasis(cap, horizontal != crossed, slope, ridge, rise);

            // WHICH KERB THE BUILDING FACES, as the outward normal of its own street face, which
            // is Side read one more time rather than re-derived. `back` is already the direction
            // AWAY from the Street, so the frontage is its negation. ⚠ It goes into the drawing in
            // WORLD axes and not the Lot's, because that is the frame the shader meets it in --
            // and world +Z is SOUTH, since a position is composed with -north.
            float faceEast = horizontal ? 0f : -back;
            float faceSouth = horizontal ? back : 0f;

            float taken = FacadeAppearance.Occupancy(_world, slot);

            // 🔴 THE ONE PAINT DERIVATION, and the two debug washes take it over here rather than
            // anywhere downstream -- both Massing construction sites read these two locals, so a
            // second site would be a second answer to "what colour is this Building".
            Color paint = _washing switch
            {
                Wash.Health => HealthColour(slot).SrgbToLinear(),
                Wash.Trouble => TroubleColour(slot).SrgbToLinear(),
                Wash.Rung => Patterns[RungOf(lot)].SrgbToLinear(),
                Wash.Age => Shade(Vintage(slot)).SrgbToLinear(),
                _ => Rendered(shape).SrgbToLinear(),
            };

            // ⚠ The roof takes the SAME colour under a debug wash. A slate that stayed slate would
            // put a second, meaningless hue on top of every reading, and from the shallow tilt the
            // shell opens at the roof is most of what a tall Building shows.
            // ⚠ THE COVERING FOLLOWS THE FAMILY AND NOT THE SCRAMBLE, for a parapet only. See
            // Membrane: the two tones Slate() draws between are both PITCHED coverings, so a flat
            // deck reaching into that draw comes up clay tile one time in five and slate the rest,
            // and neither is what is on it.
            Color slate = _washing is Wash.Rung or Wash.Age or Wash.Health or Wash.Trouble
                ? paint
                : (cap == Cap.Parapet ? Membrane : Slate(shape)).SrgbToLinear();
            float lit = table.IsAbandoned(slot) ? 0f : taken;
            float draw = FacadeAppearance.Pack((byte)(shape >> 56),
                FacadeAppearance.HasShopfront(_world, slot), table.IsAbandoned(slot));

            // A TOWER IS TWO BODIES FROM THE SAME PLAN THE CITY COUNTS. The Lot owns the whole
            // site; its low podium closes the street wall, and the centred half-plan shaft rises
            // above it. Encoding the setback by shrinking the parcel left the block centre
            // ownerless, while drawing this only in the shell would give the city floor it could
            // not show. BuildingPlan.Tower is therefore the sole partition read here and by
            // LotTable.FloorTiles. plans/0062.
            if (lots.PatternOf(lot) == BlockPattern.Tower)
            {
                BuildingPlan.TowerForm tower = BuildingPlan.Tower(footWide, footDeep, storeys);
                float podiumTall = tower.PodiumStoreys * StoreyMetres;
                float shaftTall = tower.ShaftStoreys * StoreyMetres;
                Color reads = new(
                    (faceEast + 1f) * 0.5f, (faceSouth + 1f) * 0.5f, lit, draw);

                yield return new Massing(
                    id,
                    new Transform3D(
                        Basis.FromScale(new Vector3(eastWest, podiumTall, southNorth)),
                        new Vector3(east, podiumTall * 0.5f, -north)),
                    default,
                    default,
                    paint,
                    slate,
                    reads,
                    Cap.Flat,
                    false);

                if (tower.ShaftStoreys > 0)
                {
                    float shaftWide = tower.ShaftWide * MetresPerTile;
                    float shaftDeep = tower.ShaftDeep * MetresPerTile;
                    float shaftEast = (lots.FootprintEast[lot].Raw + tower.ShaftEast
                        + (tower.ShaftWide * 0.5f)) * MetresPerTile;
                    float shaftNorth = (lots.FootprintNorth[lot].Raw + tower.ShaftNorth
                        + (tower.ShaftDeep * 0.5f)) * MetresPerTile;
                    float shaftWanted = Math.Min(shaftWide, shaftDeep) * (RoofRiseLow
                        + (((shape >> 40) & 0xFFu) / 255f * (RoofRiseHigh - RoofRiseLow)));
                    Cap shaftCap = CapFor(tall, shaftWide, shaftDeep);
                    float shaftRise = RoofHeight(shaftCap, shaftWanted);

                    yield return new Massing(
                        id,
                        new Transform3D(
                            Basis.FromScale(new Vector3(shaftWide, shaftTall, shaftDeep)),
                            new Vector3(
                                shaftEast,
                                podiumTall + (shaftTall * 0.5f),
                                -shaftNorth)),
                        new Transform3D(
                            CapBasis(
                                shaftCap,
                                shaftWide > shaftDeep,
                                Math.Min(shaftWide, shaftDeep),
                                Math.Max(shaftWide, shaftDeep),
                                shaftRise),
                            new Vector3(shaftEast, tall + (shaftRise * 0.5f), -shaftNorth)),
                        default,
                        paint,
                        slate,
                        new Color(reads.R, reads.G, reads.B, reads.A + FacadeAppearance.UpperPart),
                        shaftCap,
                        false);
                }

                continue;
            }

            // 🔴 A RING IS FOUR WINGS AND NOT ONE BOX (plans/0053 step 4). BuildingPlan.Hollow is
            // the same call LotTable.FloorTiles subtracts, so the courtyard the city counted OUT of
            // this Building's capacity is the courtyard the picture leaves open. ***A Building that
            // counted a hole and drew a solid roof would be the parcel-against-footprint defect
            // arriving one level in***, which is what plans/0052 stage 1 was.
            if (BuildingPlan.Hollow(
                    lots.PatternOf(lot), footWide, footDeep,
                    out int holeWide, out int holeDeep))
            {
                foreach (Massing wing in Wings(
                    id,
                    lots.FootprintEast[lot].Raw * MetresPerTile,
                    lots.FootprintNorth[lot].Raw * MetresPerTile,
                    eastWest,
                    southNorth,
                    holeWide * MetresPerTile,
                    holeDeep * MetresPerTile,
                    tall,
                    shape,
                    paint,
                    slate,
                    lit,
                    draw))
                {
                    yield return wing;
                }

                continue;
            }

            yield return new Massing(
                id,
                new Transform3D(Basis.FromScale(plan), new Vector3(east, tall * 0.5f, -north)),
                new Transform3D(capped, new Vector3(east, tall + (rise * 0.5f), -north)),
                new Transform3D(
                    Basis.FromScale(hut),
                    new Vector3(
                        east + (horizontal ? 0f : back * off),
                        shedHeight * 0.5f,
                        -(north + (horizontal ? back * off : 0f)))),
                paint,
                slate,
                new Color((faceEast + 1f) * 0.5f, (faceSouth + 1f) * 0.5f, lit, draw),
                cap,
                outhoused);
        }
    }

    /// <summary>
    /// One courtyard Building, as the <b>four wings</b> its floor area is actually made of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0053</c> step 4, and the shape is READ rather than invented.</b>
    /// <see cref="BuildingPlan.Hollow"/> answers both this and <c>LotTable.FloorTiles</c>, so the
    /// wings drawn here are exactly the Tiles the city counted and the courtyard is exactly the ones
    /// it did not. ***The alternative was a third rectangle in a project that has now twice paid for
    /// having two.***
    /// </para>
    /// <para>
    /// <b>The partition is south wing, north wing, then the two flanks BETWEEN them</b> — so the
    /// corners belong to the east–west wings and nothing is drawn twice. ⚠ <b>Z-fighting is not the
    /// reason</b>; overlapping wings would put two window walls in one place and the openings would
    /// interfere, which reads as a smear rather than as a seam.
    /// </para>
    /// <para>
    /// <b>Each wing's ridge runs along its own long axis, and that is not a draw.</b> The solid case
    /// turns a minority of gables to face the kerb because a terrace really does that; a ring's roof
    /// runs <em>round</em> the ring, and a wing gabled across itself would be a roof pitched over a
    /// 16 m span pretending to be a building.
    /// </para>
    /// <para>
    /// 🔴 <b>FOUR DOORS, ONE PER WING, ON ITS OWN OUTER FACE.</b> The two custom channels the shader
    /// reads are <em>which way the front is</em> (<c>adr/0074</c>), and a ring has four fronts —
    /// which is what a mansion block is. The Lot's own <c>Side</c> is not used here at all, and that
    /// is the one place in this file where the drawing knows something the Address does not.
    /// </para>
    /// <para>
    /// ⚠ <b>NO OUTBUILDING, and the reason is geometric rather than a taste.</b> The shed stands in
    /// the back garden and a ring has no back garden — its open ground is <em>inside</em> it. ***An
    /// outbuilding in a courtyard is a different building type***, and inventing one would be the
    /// shell asserting a thing about the city again.
    /// </para>
    /// </remarks>
    private static System.Collections.Generic.IEnumerable<Massing> Wings(
        ulong id,
        float westEdge,
        float southEdge,
        float wide,
        float deep,
        float holeWide,
        float holeDeep,
        float tall,
        ulong shape,
        Color paint,
        Color slate,
        float lit,
        float draw)
    {
        float thick = BuildingPlan.DaylightTiles * MetresPerTile;

        // south, north, west, east -- and the flanks are shortened to the hole so the corners are
        // the east-west wings' and no Tile is drawn twice.
        (float East, float North, float Wide, float Deep, float FaceEast, float FaceNorth)[] wings =
        [
            (westEdge + (wide * 0.5f), southEdge + (thick * 0.5f), wide, thick, 0f, -1f),
            (westEdge + (wide * 0.5f), southEdge + deep - (thick * 0.5f), wide, thick, 0f, 1f),
            (westEdge + (thick * 0.5f), southEdge + thick + (holeDeep * 0.5f), thick, holeDeep, -1f, 0f),
            (westEdge + wide - (thick * 0.5f), southEdge + thick + (holeDeep * 0.5f), thick, holeDeep,
                1f, 0f),
        ];

        foreach ((float east, float north, float wingWide, float wingDeep, float faceEast,
                  float faceNorth) in wings)
        {
            // ⚠ THE RIDGE RUNS ALONG THE WING, so the span the roof CROSSES is its short axis and
            // the rise stays an angle rather than a height -- the same reading the solid case takes,
            // against a span this wing decides rather than a draw.
            bool ridgeEastWest = wingWide >= wingDeep;
            float slope = ridgeEastWest ? wingDeep : wingWide;
            float ridge = ridgeEastWest ? wingWide : wingDeep;

            float wanted = slope * (RoofRiseLow
                + (((shape >> 40) & 0xFFu) / 255f * (RoofRiseHigh - RoofRiseLow)));

            // Each courtyard wing chooses a roof from its own footprint.
            Cap cap = CapFor(tall, wingWide, wingDeep);
            float rise = RoofHeight(cap, wanted);

            // A PrismMesh runs its ridge along its own Z, which is north-south here because a
            // position is composed with -north. So the quarter turn is owed exactly when the ridge
            // is meant to run east-west, and never otherwise -- the solid case's exclusive-or is
            // two questions and this is one.
            yield return new Massing(
                id,
                new Transform3D(
                    Basis.FromScale(new Vector3(wingWide, tall, wingDeep)),
                    new Vector3(east, tall * 0.5f, -north)),
                new Transform3D(
                    CapBasis(cap, ridgeEastWest, slope, ridge, rise),
                    new Vector3(east, tall + (rise * 0.5f), -north)),
                Transform3D.Identity,
                paint,
                slate,
                new Color((faceEast + 1f) * 0.5f, (-faceNorth + 1f) * 0.5f, lit, draw),
                cap,
                false);
        }
    }

    /// <summary>
    /// A 64-bit mix, so that neighbouring row ids do not produce neighbouring shapes.
    /// </summary>
    /// <remarks>
    /// <b>splitmix64's finaliser.</b> Row ids are allocated in sequence, and the low bits of a
    /// counter are a terrible source of variety — a street of Buildings created one after another
    /// would step through the jitter range in order and read as a ramp rather than as a city.
    /// </remarks>
    private static ulong Scramble(ulong id)
    {
        ulong mixed = id + 0x9E3779B97F4A7C15UL;

        mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
        mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;

        return mixed ^ (mixed >> 31);
    }

    /// <summary>What shape a Building's roof is. <b>An appearance family, and each costs a mesh.</b></summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE ONE THING A SHADER CANNOT DO IS CHANGE A SILHOUETTE</b>, so this is the whole of
    /// the drawing's variety that <c>buildings.gdshader</c> is structurally incapable of. Everything
    /// the shader draws — storeys, bays, openings, doors, reveals, lit windows — happens INSIDE the
    /// box's outline. The outline is here, in four meshes, or it is nowhere.
    /// </para>
    /// <para>
    /// ⚠ <b>IT IS DERIVED FROM THE PLAN AND NOT DRAWN FROM A HASH</b>, which is the whole point and
    /// is the opposite of what stood here. A roof was <c>((shape >> 32) &amp; 3u) != 0u</c> — a
    /// three-in-four coin flip off the Building's own scramble — so the roofline said nothing about
    /// the Building and a street of them was gabled at random. ***Jittering a continuous parameter
    /// on one template gives objects that are each unique and collectively identical***, and a roof
    /// drawn from a hash is that failure in the one channel that reads from a kilometre up.
    /// </para>
    /// <para>
    /// 🔴 <b>AND IT IS NOT A KIND LOOKUP, WHICH <c>adr/0150</c> FORBIDS BY NAME.</b> Aliasing a
    /// <c>[[building]]</c> kind to a mesh would size the city's visual vocabulary to a byte chosen
    /// for the Rule engine's convenience and would reintroduce <c>adr/0025</c>'s mutually-exclusive
    /// asset pools through the art door. What decides a roof here is the <b>plan</b> — how square it
    /// is and how wide a span it has to cross — which is that ADR's own <i>a terrace and a tower
    /// differ physically, not by tier</i> applied to the fifth elevation. Kinds still announce
    /// themselves, because a school HAS a big square footprint and a terrace house HAS a narrow
    /// one; the proportion is the kind's signature and the shell reads it rather than being told.
    /// </para>
    /// </remarks>
    private enum Cap
    {
        /// <summary>No roof instance at all. <b>The tall case, and the cheap one.</b></summary>
        Flat,

        /// <summary>One ridge along the long axis. <b>The terrace, and the common case.</b></summary>
        Gable,

        /// <summary>Four slopes meeting a short ridge.</summary>
        Hip,

        /// <summary>Two parallel ridges across a broad footprint.</summary>
        PairedGable,

        /// <summary>
        /// A coping round a flat tray. <b>The broad low body, which cannot wear a pitch.</b>
        /// </summary>
        /// <remarks>
        /// <para>
        /// 🔴 <b>IT IS THE ONE CAP THAT WAS FOUND BY LOOKING RATHER THAN BY READING.</b> Research
        /// pass 03 reconstructed the shopping fixture and named G001 — 36×20 m, two floors, 7 m
        /// walls — as the strongest test of whether a wide body reads as a building family. Driven
        /// at Tick 600 it came back wearing <see cref="PairedGable"/>: 2.68 m of pitched tile on a
        /// 7 m wall, a roof 38% the height of what it sits on, over a 36 m frontage. ***A shed in
        /// a hat.*** 42 of the fixture's 52 Buildings were in one, and 16 of those were two
        /// storeys — so the family meant for the broad exception was the city's ordinary case.
        /// </para>
        /// <para>
        /// 🔴 <b>IT SAYS NOTHING ABOUT WHAT THE BUILDING IS FOR, AND THE FIRST VERSION DID.</b> This
        /// began as *the broad low body is a workplace*, carrying a workplace facade with it —
        /// structural bays, a receiving door at the back. ⚠ <b>Driving <c>minimal.toml</c> refuted
        /// it in one frame</b>: 41 of 88 Buildings matched, and half a residential demonstration
        /// city read as an industrial estate. ***The footprints are identical*** — <c>minimal</c>
        /// draws 36×20 and 52×24 at two storeys exactly as <c>shopping</c> does — so no reading of
        /// the plan can separate a workplace from a dwelling in this build, because the city does
        /// not draw them differently. ⚠ <b>And nothing in the content set is a workplace</b>: across
        /// all 41 shipped Rulesets declaring a <c>[[building]]</c>, <b>zero</b> kinds set
        /// <c>premises</c> without <c>houses</c>. pass 03's *wide game bodies should use
        /// non-domestic types* is a hypothesis about content, and what survives here is the
        /// narrower claim the geometry supports on its own — ***a span this broad on a wall this
        /// low takes no pitch***, whoever is inside.
        /// </para>
        /// </remarks>
        Parapet,
    }

    /// <summary>Which roof family a plan implies. <b>Five now, and the fifth is not a pitch.</b></summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE BROAD CASE SPLITS ON THE STOREY COUNT, AND IT IS THE PLAN ANSWERING.</b> Past
    /// <see cref="BroadSpanMetres"/> a body is too wide for one domestic span, and this used to
    /// answer that in one way — halve it and pitch twice. That is right for a body deep enough to
    /// BE two ranges and wrong for one that is a single low volume, which is the difference between
    /// a perimeter block and a workshop. ***The storeys are what tell them apart***, and they are
    /// the city's own number (<c>LotTable.Storeys</c>), reached the same way the shader reaches it.
    /// </para>
    /// <para>
    /// ⚠ <b>IT IS NOT A KIND LOOKUP</b>, which <c>adr/0150</c> forbids by name — and it is not an
    /// inference about use either. It asks how wide the shortest span is and how many floors stack
    /// on it, and answers <em>what roof</em>. See <see cref="Cap.Parapet"/> for what asking it to
    /// answer <em>what building</em> cost.
    /// </para>
    /// </remarks>
    private static Cap CapFor(float tall, float along, float deep)
    {
        if (tall > PitchCeilingMetres || Mathf.Min(along, deep) <= 0) return Cap.Flat;
        if (Mathf.Min(along, deep) > BroadSpanMetres)
        {
            return tall < RangeStoreysLeast * StoreyMetres ? Cap.Parapet : Cap.PairedGable;
        }

        return Mathf.Min(along, deep) / Mathf.Max(along, deep) >= HipSquareness ? Cap.Hip : Cap.Gable;
    }

    private static Color RoofWall(Massing one) => new(one.Paint.R, one.Paint.G, one.Paint.B, one.Reads.A);

    /// <remarks>
    /// ⚠ <b>A PARAPET IS A HEIGHT AND NOT A RISE, so it ignores what the span asked for.</b> Every
    /// other family here scales with the span it crosses, because a pitch is an angle. A parapet
    /// crosses nothing — it is an upstand at an edge, and a 60 m body's is the same height as a
    /// 20 m body's.
    /// </remarks>
    private static float RoofHeight(Cap cap, float wanted) => cap switch
    {
        Cap.Parapet => ParapetMetres,
        Cap.PairedGable => Mathf.Min(wanted / 2f, RoofRiseCeilingMetres),
        _ => Mathf.Min(wanted, RoofRiseCeilingMetres),
    };

    /// <remarks>
    /// ⚠ <b>A PARAPET HAS NO EAVES AND THE SIGN IS THE POINT.</b> An eave throws water clear of the
    /// wall and overhangs to do it; a parapet stands the wall up PAST the roof and drains behind
    /// itself. Handing it <see cref="EavesMetres"/> would put a 0.35 m lip round a flat roof, which
    /// is a pitched roof's detail on an assembly that refuses it — pass 03's *low membrane range
    /// fitted with steep-roof clay details* in one line of arithmetic. It stands slightly proud
    /// instead, which is what a coping does.
    /// </remarks>
    private static Basis CapBasis(Cap cap, bool turned, float slope, float ridge, float rise)
    {
        float out_ = cap == Cap.Parapet ? CopingMetres : EavesMetres;
        var basis = Basis.FromScale(new Vector3(slope + out_ * 2f, rise, ridge + out_ * 2f));
        return turned ? Quarter * basis : basis;
    }

    /// <summary>One Building's drawing: a body, a roof it may not have, and the paint for both.</summary>
    /// <remarks>
    /// ⚠ <b>One derivation and two meshes, rather than two iterators over one table.</b> The body
    /// and the gable share a setback, a footprint and a scramble, and a second walk deriving them
    /// again is <c>plans/0012</c> <b>Cause 1</b> with a frame between the copies.
    /// </remarks>
    /// <param name="Slate">
    /// What the roof is covered in, <b>before the shell's paint is allowed to overrule it</b>.
    /// </param>
    /// <param name="Reads">
    /// <b>The four per-instance channels the wall shader reads</b>, and two of them are the city's:
    /// <c>r</c> and <c>g</c> are the street face's outward normal remapped from −1…1, which is the
    /// Lot's Side and therefore where the front door goes; <c>b</c> is occupancy as a share of the
    /// kind's declared room; <c>a</c> is the Building's own draw so two identical boxes are not
    /// identically fenestrated. ⚠ <b>It is a <see cref="Color"/> because that is the type
    /// <c>MultiMesh</c> takes, and not one of them is a colour.</b>
    /// </param>
    private readonly record struct Massing(
        ulong Id,
        Transform3D Body,
        Transform3D Roof,
        Transform3D Yard,
        Color Paint,
        Color Slate,
        Color Reads,
        Cap Cap,
        bool Outhoused)
    {
        public bool Abandoned => ((int)Reads.A & 512) != 0;
    }

    private Color RoofPaint(Massing one) => one.Abandoned && _washing is not (Wash.Rung or Wash.Age or Wash.Health or Wash.Trouble)
        ? one.Slate.Darkened(0.35f) : one.Slate;

    private static Color YardPaint(Massing one) => one.Abandoned
        ? Outbuilding.SrgbToLinear().Darkened(0.35f) : Outbuilding.SrgbToLinear();

    /// <summary>Fills the body layer and the roof layer from one walk. Returns the Buildings.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>FIVE COUNTS AND THE RETURN IS NONE OF THEM.</b> A flat-roofed Building writes no roof
    /// instance at all and every other one writes into <b>one of three</b> roof layers by its shape,
    /// so no roof count is the Building count and the three do not sum to it either; and since
    /// <c>plans/0053</c> step 4 a courtyard Building writes <b>four</b> bodies, so the body count is
    /// long. ***The readout says <em>Buildings</em> and has to mean Buildings***, so what is returned
    /// is the number of distinct ones.
    /// </para>
    /// <para>
    /// <b>A change of Id and not a set</b>, because <see cref="Wings"/> emits a ring's four wings
    /// consecutively — so counting the transitions is exact here and costs nothing. ⚠ <b>It is exact
    /// only under that ordering</b>, which is why the ordering is stated at the one place that
    /// produces it rather than assumed here.
    /// </para>
    /// </remarks>
    private int Massings(System.Collections.Generic.IEnumerable<Massing> massing)
    {
        int bodies = 0;
        int roofs = 0;
        int hips = 0;
        int pairedRoofs = 0;
        int parapets = 0;
        int yards = 0;
        int buildings = 0;
        ulong last = 0;
        int footprints = 0;

        _buildingIds.Clear();
        _roofIds.Clear();
        _hipIds.Clear();
        _pairedRoofIds.Clear();
        _parapetIds.Clear();
        _yardIds.Clear();

        foreach (Massing one in massing)
        {
            if (bodies == 0 || one.Id != last)
            {
                buildings++;
                last = one.Id;
            }

            FoliageFootprint(one.Body, footprints++);
            if (one.Outhoused) FoliageFootprint(one.Yard, footprints++);
            _buildings.Multimesh.Identity(bodies, one.Id);
            _buildingIds.Add(one.Id);
            _buildings.Multimesh.SetInstanceTransform(bodies, one.Body);
            _buildings.Multimesh.SetInstanceColor(bodies, one.Paint);
            _buildings.Multimesh.SetInstanceCustomData(bodies++, one.Reads);

            if (one.Outhoused)
            {
                _yards.Multimesh.Identity(yards, one.Id);
                _yardIds.Add(one.Id);
                _yards.Multimesh.SetInstanceTransform(yards, one.Yard);
                _yards.Multimesh.SetInstanceColor(
                    yards++,
                    YardPaint(one));
            }

            // 🔴 A ROOF FAMILY IS A MESH, SO IT IS A DIFFERENT MULTIMESH AND A DIFFERENT COUNTER.
            // A MultiMesh holds one mesh and many transforms, which is the whole reason 241
            // Buildings cost two draw calls -- so the three pitched families cannot share a buffer
            // however alike their transforms look. ***What varies per instance is a transform;
            // what varies per family is a mesh.***
            (InstanceLayer layer, List<ulong> ids, int at) = one.Cap switch
            {
                Cap.Gable => (_roofs, _roofIds, roofs),
                Cap.Hip => (_hips, _hipIds, hips),
                Cap.PairedGable => (_pairedRoofs, _pairedRoofIds, pairedRoofs),
                Cap.Parapet => (_parapets, _parapetIds, parapets),
                _ => (null!, null!, 0),
            };

            if (one.Cap == Cap.Flat)
            {
                continue;
            }

            layer.Multimesh.Identity(at, one.Id);
            ids.Add(one.Id);
            layer.Multimesh.SetInstanceTransform(at, one.Roof);
            layer.Multimesh.SetInstanceCustomData(at, RoofWall(one));

            // ⚠ THE ROOF OF A SHELL IS THE SHELL'S COLOUR AND NOT THE ROOFING. An abandoned
            // Building that kept a warm red roof would read as the liveliest thing on the street.
            layer.Multimesh.SetInstanceColor(
                at, RoofPaint(one));

            switch (one.Cap)
            {
                case Cap.Gable: roofs++; break;
                case Cap.Hip: hips++; break;
                case Cap.Parapet: parapets++; break;
                default: pairedRoofs++; break;
            }
        }

        _buildings.Multimesh.VisibleInstanceCount = bodies;
        _roofs.Multimesh.VisibleInstanceCount = roofs;
        _hips.Multimesh.VisibleInstanceCount = hips;
        _pairedRoofs.Multimesh.VisibleInstanceCount = pairedRoofs;
        _parapets.Multimesh.VisibleInstanceCount = parapets;
        _yards.Multimesh.VisibleInstanceCount = yards;

        RefreshFoliage(footprints);
        return buildings;
    }

    /// <summary>Writes transforms into a MultiMesh and returns how many there were.</summary>
    /// <remarks>
    /// ⚠ <b>A whole transform and not a position, since Buildings vary in size.</b> The scale is
    /// composed with <see cref="Basis.FromScale"/> in the instance's own frame — <c>Basis.Scaled</c>
    /// scales in the PARENT frame, which is what drew the first road network as north–south lines
    /// with no cross-streets.
    /// </remarks>
    private static int Fill(
        InstanceLayer into,
        System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)> places,
        List<ulong>? ids = null)
    {
        int painted = 0;

        ids?.Clear();

        foreach ((ulong id, Transform3D where, Color what) in places)
        {
            into.Multimesh.Identity(painted, id);
            ids?.Add(id);
            into.Multimesh.SetInstanceTransform(painted, where);
            into.Multimesh.SetInstanceColor(painted++, what);
        }

        into.Multimesh.VisibleInstanceCount = painted;

        return painted;
    }

    /// <inheritdoc cref="Fill(InstanceLayer, System.Collections.Generic.IEnumerable{ValueTuple{Transform3D, Color}})"/>
    /// <summary>The same, for a layer whose colour belongs to the layer rather than the box.</summary>
    private static int Fill(
        InstanceLayer into,
        System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> places,
        List<ulong>? ids = null)
    {
        int count = 0;

        ids?.Clear();

        foreach ((ulong id, Transform3D place) in places)
        {
            into.Multimesh.Identity(count, id);
            ids?.Add(id);
            into.Multimesh.SetInstanceTransform(count++, place);
        }

        into.Multimesh.VisibleInstanceCount = count;

        return count;
    }
}
