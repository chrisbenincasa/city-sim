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
    private System.Collections.Generic.IEnumerable<Massing> Buildings()
    {
        BuildingTable table = _world.Buildings;
        LotTable lots = _world.Lots;
        BlockLattice lattice = _world.Roads.Streets.Lattice;

        for (int slot = 0; slot < table.Rows.SlotCount; slot++)
        {
            if (!table.Rows.IsLive(slot) || !lots.Rows.TryResolve(table.Lot[slot], out int lot))
            {
                continue;
            }

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

            // ⚠ A GABLE IS A PRISM EXTRUDED ALONG ITS OWN Z, so the ridge runs north-south as
            // authored and the horizontal case is the one that turns. Rotation composed on the LEFT
            // of the scale, because R*S scales first and then turns the result -- the other order
            // scales an already-turned box on the wrong axes, which is Fill's own warning one
            // transform up.
            // ⚠ WHICH WAY THE RIDGE RUNS IS A DRAW, and it is the one roof variation that changes
            // the SILHOUETTE rather than the slope. A ridge along the Street is what makes a
            // terrace read as a terrace, so it stays the common case; a minority turn the gable to
            // face the kerb, which is what a row of them actually looks like. It is refused on a
            // Building much wider than it is deep, because turning that gable puts the ridge over
            // a fifty-metre span and the roof ends up taller than the house.
            bool crossed = ((shape >> 34) & 7u) == 0u && along <= deep * 1.2f;
            float slope = crossed ? along : deep;
            float ridge = crossed ? deep : along;

            // ⚠ THE RISE IS A SHARE OF THE SPAN THE ROOF CROSSES AND NOT OF THE DEPTH, which is
            // what makes the band an ANGLE rather than a height: 0.25 to 0.85 of a half-span is
            // 27° to 60° off the horizontal, and it has to be measured against the span the ridge
            // is actually turned across or a cross-gabled roof reads at the wrong pitch entirely.
            //
            // 🔴 THE UNCLAMPED RISE IS KEPT AND IT IS EVIDENCE. CapFor needs to know whether the
            // clamp WOULD have bitten, and a clamped number cannot report that it clamped -- so the
            // ceiling is applied after the question is asked rather than inside the expression.
            // ***The value that gets thrown away is the one carrying the finding.***
            float wanted = slope * (RoofRiseLow
                + (((shape >> 40) & 0xFFu) / 255f * (RoofRiseHigh - RoofRiseLow)));

            Cap cap = CapFor(tall, along, deep, wanted);
            float rise = Mathf.Min(wanted, RoofRiseCeilingMetres);

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
            float off = (deep * 0.5f) + 5f + (shed * 0.5f);
            Vector3 hut = horizontal
                ? new Vector3(wide, shed * 0.8f, shed)
                : new Vector3(shed, shed * 0.8f, wide);

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

            // HOW MUCH OF THE KIND'S ROOM IS TAKEN, which the panel already prints as "3 of 4
            // occupied" and which nothing in the picture has ever said. ⚠ The ceiling counts
            // tenants of any kind (adr/0147), so a shop takes one of them.
            int room = _world.DeclaredOccupancy(slot);
            float taken = room > 0
                ? Mathf.Clamp(_world.Occupants.Length(slot) / (float)room, 0f, 1f)
                : 1f;

            // 🔴 THE ONE PAINT DERIVATION, and the two debug washes take it over here rather than
            // anywhere downstream -- both Massing construction sites read these two locals, so a
            // second site would be a second answer to "what colour is this Building".
            Color paint = _washing switch
            {
                Wash.Rung => Patterns[RungOf(lot)].SrgbToLinear(),
                Wash.Age => Shade(Vintage(slot)).SrgbToLinear(),
                _ => (table.IsAbandoned(slot) ? Derelict : Rendered(shape)).SrgbToLinear(),
            };

            // ⚠ The roof takes the SAME colour under a debug wash. A slate that stayed slate would
            // put a second, meaningless hue on top of every reading, and from the shallow tilt the
            // shell opens at the roof is most of what a tall Building shows.
            Color slate = _washing is Wash.Rung or Wash.Age
                ? paint
                : Slate(shape).SrgbToLinear();
            float lit = table.IsAbandoned(slot) ? 0f : taken;
            float draw = ((shape >> 56) & 0xFFu) / 255f;

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
                    Cap shaftCap = CapFor(tall, shaftWide, shaftDeep, shaftWanted);
                    float shaftRise = Mathf.Min(shaftWanted, RoofRiseCeilingMetres);

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
                        reads,
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
                        shed * 0.4f,
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

            // 🔴 A WING ASKS FOR ITS OWN ROOF AND DOES NOT INHERIT THE RING'S, which is the one
            // place this change had a choice to make. The ring's own plan is big and close to
            // square, so CapFor would call it a HIP -- and a hip is the right answer for the
            // building and the wrong one for the strip: each wing is a corridor about 16 m across,
            // and four hipped corridors is not what a mansion block looks like. ***The plan a roof
            // is derived from has to be the plan the roof actually sits on.*** The Flat answer is
            // still shared, because it comes from `tall` and every wing has the ring's height.
            //
            // ⚠ IN PRACTICE A WING IS ALWAYS GABLED OR FLAT and the call is still made rather than
            // shortcut: the ratio that would make a wing square is reachable -- a small courtyard on
            // a deep parcel -- and a rule written as a rule reports that case instead of hiding it.
            Cap cap = CapFor(tall, wingWide, wingDeep, wanted);
            float rise = Mathf.Min(wanted, RoofRiseCeilingMetres);

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

        /// <summary>Four slopes to a point. <b>What a plan with no long axis gets.</b></summary>
        Hip,

        /// <summary>Four slopes to a flat top. <b>What a span too wide for one pitch gets.</b></summary>
        Mansard,
    }

    /// <summary>
    /// Which roof a Building asks for. <b>Three questions, in order</b> — and 🔴 <b>only the first
    /// two are about the rectangle.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE THIRD QUESTION CARRIES A DRAW, AND THE FIRST TWO DO NOT.</b> Flat and Hip are
    /// decided by the plan alone, so every Building with the same footprint gets the same answer.
    /// The Gable/Mansard split is decided by <paramref name="rise"/>, which is the span crossed with
    /// that Building's own pitch share — so <b>two Buildings with identical footprints can cap
    /// differently</b>, and the observed city does exactly that. ⚠ <b>That is deliberate and it is
    /// not the oatmeal failure it resembles.</b> The pitch share was always a per-Building draw and
    /// always varied the roof; what changed is that its extreme is now <i>announced</i> as a
    /// different roof instead of being silently flattened. ***A category that reports a clamp is
    /// saying something; a category rolled for its own sake is not.*** The distinction is the whole
    /// argument for archetypes over jitter, so the line between them is worth stating and not
    /// blurring.
    /// </para>
    /// <para>
    /// <b>1. Can it be pitched at all?</b> <see cref="PitchCeilingMetres"/>, unchanged and the only
    /// rule that survives from the coin flip this replaces.
    /// </para>
    /// <para>
    /// <b>2. Is there a long axis for a ridge to run along?</b> <see cref="HipSquareness"/>. If the
    /// plan is within a fifth of square there is no direction worth choosing, and the answer real
    /// buildings give is a hip.
    /// </para>
    /// <para>
    /// 🔴 <b>3. Would the pitch be CLAMPED? — and this question was already in the file, asked and
    /// unanswered.</b> <see cref="RoofRiseCeilingMetres"/>'s own remark says it: <i>"Real buildings
    /// do not solve this by flattening the pitch; they stop using one ridge and break the roof up,
    /// which is geometry this shell does not have."</i> ***The mansard IS that geometry.*** So the
    /// third branch costs no new number — it fires exactly when the existing clamp would have bitten,
    /// which is the shell admitting it has flattened a slope it should have broken. A remark
    /// describing a defect is a specification nobody had read as one.
    /// </para>
    /// <para>
    /// ⚠ <b><paramref name="rise"/> is the UNCLAMPED rise</b> and has to be, because the clamped one
    /// cannot report that it clamped.
    /// </para>
    /// </remarks>
    private static Cap CapFor(float tall, float along, float deep, float rise)
    {
        if (tall > PitchCeilingMetres)
        {
            return Cap.Flat;
        }

        float longer = Mathf.Max(along, deep);

        if (longer <= 0f)
        {
            return Cap.Flat;
        }

        if (Mathf.Min(along, deep) / longer >= HipSquareness)
        {
            return Cap.Hip;
        }

        return rise > RoofRiseCeilingMetres ? Cap.Mansard : Cap.Gable;
    }

    /// <summary>
    /// The transform basis a roof of this shape wants. <b>One place, because both call sites
    /// build the same roof.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE TWO FAMILIES COMPOSE THEIR ROTATION IN OPPOSITE ORDERS, AND GETTING IT BACKWARDS
    /// IS SILENT.</b> A gable is scaled in its OWN frame — slope across X, ridge along Z — and then
    /// turned into the world, so the turn goes on the left: <c>R * S</c>. A cone has to be turned
    /// FIRST, to bring its faces off the diagonal, and only then scaled along the world's axes:
    /// <c>S * R</c>. ***Scaling a shape and then turning it 45° makes a rectangle into a
    /// rhombus***, which is the failure this ordering exists to avoid and which would read as roofs
    /// that do not sit on their walls.
    /// </para>
    /// <para>
    /// ⚠ <b><paramref name="turned"/> means the same thing to both and is answered once</b>, by the
    /// caller, because it is a question about the STREET rather than about the roof — which way the
    /// Building faces. A cone has no ridge to turn, so it uses the answer only to work out which of
    /// its two spans is the world's X.
    /// </para>
    /// <para>
    /// ⚠ <b>The eaves are added to every family</b>, for <see cref="EavesMetres"/>' own reason: the
    /// overhang is the line an angle is read against, and a hip cut flush with its wall is as mute
    /// as a gable was.
    /// </para>
    /// </remarks>
    private static Basis CapBasis(Cap cap, bool turned, float slope, float ridge, float rise)
    {
        float eaves = EavesMetres * 2f;

        if (cap == Cap.Gable)
        {
            var wedge = Basis.FromScale(new Vector3(slope + eaves, rise, ridge + eaves));

            return turned ? Quarter * wedge : wedge;
        }

        // The quarter turn swaps the two plan axes, so asking it here is how a span in the
        // Building's frame becomes a span in the world's without the caller measuring twice.
        float spanX = (turned ? ridge : slope) + eaves;
        float spanZ = (turned ? slope : ridge) + eaves;

        // ⚠ THE DIAGONAL IS NOT A FUDGE. Eighth leaves the squared-up base spanning 1/√2 rather
        // than 1, so every span owes the reciprocal of that — which is √2 — or the roof sits inside
        // its own walls. See Diagonal.
        return Basis.FromScale(new Vector3(spanX * Diagonal, rise, spanZ * Diagonal)) * Eighth;
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
        bool Outhoused);

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
        int mansards = 0;
        int yards = 0;
        int buildings = 0;
        ulong last = 0;

        _buildingIds.Clear();
        _roofIds.Clear();
        _hipIds.Clear();
        _mansardIds.Clear();
        _yardIds.Clear();

        foreach (Massing one in massing)
        {
            if (bodies >= _buildings.Multimesh.InstanceCount)
            {
                break;
            }

            if (bodies == 0 || one.Id != last)
            {
                buildings++;
                last = one.Id;
            }

            _buildingIds.Add(one.Id);
            _buildings.Multimesh.SetInstanceTransform(bodies, one.Body);
            _buildings.Multimesh.SetInstanceColor(bodies, one.Paint);
            _buildings.Multimesh.SetInstanceCustomData(bodies++, one.Reads);

            if (one.Outhoused && yards < _yards.Multimesh.InstanceCount)
            {
                _yardIds.Add(one.Id);
                _yards.Multimesh.SetInstanceTransform(yards, one.Yard);
                _yards.Multimesh.SetInstanceColor(
                    yards++,
                    one.Paint == Derelict.SrgbToLinear() ? one.Paint : Outbuilding.SrgbToLinear());
            }

            // 🔴 A ROOF FAMILY IS A MESH, SO IT IS A DIFFERENT MULTIMESH AND A DIFFERENT COUNTER.
            // A MultiMesh holds one mesh and many transforms, which is the whole reason 241
            // Buildings cost two draw calls -- so the three pitched families cannot share a buffer
            // however alike their transforms look. ***What varies per instance is a transform;
            // what varies per family is a mesh.***
            (MultiMeshInstance3D layer, List<ulong> ids, int at) = one.Cap switch
            {
                Cap.Gable => (_roofs, _roofIds, roofs),
                Cap.Hip => (_hips, _hipIds, hips),
                Cap.Mansard => (_mansards, _mansardIds, mansards),
                _ => (null!, null!, 0),
            };

            if (one.Cap == Cap.Flat || at >= layer.Multimesh.InstanceCount)
            {
                continue;
            }

            ids.Add(one.Id);
            layer.Multimesh.SetInstanceTransform(at, one.Roof);

            // ⚠ THE ROOF OF A SHELL IS THE SHELL'S COLOUR AND NOT THE ROOFING. An abandoned
            // Building that kept a warm red roof would read as the liveliest thing on the street.
            layer.Multimesh.SetInstanceColor(
                at, one.Paint == Derelict.SrgbToLinear() ? one.Paint : one.Slate);

            switch (one.Cap)
            {
                case Cap.Gable: roofs++; break;
                case Cap.Hip: hips++; break;
                default: mansards++; break;
            }
        }

        _buildings.Multimesh.VisibleInstanceCount = bodies;
        _roofs.Multimesh.VisibleInstanceCount = roofs;
        _hips.Multimesh.VisibleInstanceCount = hips;
        _mansards.Multimesh.VisibleInstanceCount = mansards;
        _yards.Multimesh.VisibleInstanceCount = yards;

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
        MultiMeshInstance3D into,
        System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)> places,
        List<ulong>? ids = null)
    {
        int painted = 0;

        ids?.Clear();

        foreach ((ulong id, Transform3D where, Color what) in places)
        {
            if (painted >= into.Multimesh.InstanceCount)
            {
                break;
            }

            ids?.Add(id);
            into.Multimesh.SetInstanceTransform(painted, where);
            into.Multimesh.SetInstanceColor(painted++, what);
        }

        into.Multimesh.VisibleInstanceCount = painted;

        return painted;
    }

    /// <inheritdoc cref="Fill(MultiMeshInstance3D, System.Collections.Generic.IEnumerable{ValueTuple{Transform3D, Color}})"/>
    /// <summary>The same, for a layer whose colour belongs to the layer rather than the box.</summary>
    private static int Fill(
        MultiMeshInstance3D into,
        System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> places,
        List<ulong>? ids = null)
    {
        int count = 0;

        ids?.Clear();

        foreach ((ulong id, Transform3D place) in places)
        {
            if (count >= into.Multimesh.InstanceCount)
            {
                break;
            }

            ids?.Add(id);
            into.Multimesh.SetInstanceTransform(count++, place);
        }

        into.Multimesh.VisibleInstanceCount = count;

        return count;
    }
}
