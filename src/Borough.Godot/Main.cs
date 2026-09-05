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

/// <summary>
/// The shell: a camera over the city, and the simulation stepping under it.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>The scene is built in code and the <c>.tscn</c> holds one node.</b> Everything drawn here
/// is derived from the world every frame, so an authored scene would be a second description of the
/// city that could disagree with the first.
/// </para>
/// <para>
/// ⚠ <b>One <see cref="MultiMeshInstance3D"/> per kind of thing, not per Chunk.</b> <c>05 §2</c>
/// asks for per-Chunk and that is a claim about a million Citizens; at the sizes this shell has ever
/// been run at it would be structure with nothing behind it.
/// </para>
/// <para>
/// <b>The clock is the host's and the Tick is not.</b> <c>05</c>'s fixed-Tick/interpolated-render
/// split lives in <see cref="_Process"/>: wall time accumulates, whole Ticks are stepped from it,
/// and the leftover is the <c>alpha</c> handed to <see cref="VisibleAgents"/>.
/// </para>
/// </remarks>
public partial class Main : Node3D
{
    /// <summary>Metres per Tile, so the camera can be placed in something a human reads.</summary>
    private const float MetresPerTile = 4f;

    /// <summary>
    /// How far the eye is tipped above the ground — <b>the isometric angle, and it never moves.</b>
    /// </summary>
    /// <remarks>
    /// <c>atan(1/sqrt(2))</c>, which is the angle at which a cube's three visible faces are drawn
    /// equal. The camera is a perspective one, so this is the framing rather than the projection.
    /// </remarks>
    private const float PitchRadians = 0.61547971f;

    /// <summary>The lowest the eye may be tipped, in radians — <b>4°.</b></summary>
    /// <remarks>
    /// ⚠ <b>Not zero, and the reason is the ground rather than taste.</b> The terrain is one
    /// <c>PlaneMesh</c> 65.5 km across at <c>y = 0.01</c>; at a horizontal eye it is edge-on, the
    /// far half of the map converges into a single row of pixels, and what fills the frame is the
    /// sky's ground colour. A few degrees is enough to keep a surface under the city.
    /// </remarks>
    private const float LowestRadians = 0.0698f;

    /// <summary>The highest — <b>85°, and deliberately not 90.</b></summary>
    /// <remarks>
    /// ⚠ <b>Straight down would make <see cref="Orbit"/>'s up-vector degenerate</b>, and a
    /// <c>LookAt</c> whose forward is parallel to its up produces a basis with no defined yaw — so
    /// the city would spin on the last frame before the top. The five degrees are the guard.
    /// </remarks>
    private const float HighestRadians = 1.4835f;

    /// <summary>A quarter turn of the compass, which is what one press of the rotate key is.</summary>
    private const float YawStepRadians = Mathf.Pi * 0.25f;

    /// <summary>What one notch of zoom in multiplies the eye's standoff by.</summary>
    private const float DollyPerStep = 0.92f;

    /// <summary>
    /// <b>How wide a Street's whole right of way is drawn</b> — kerb to kerb <em>and</em> the
    /// pavements, which together spend it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS WAS <c>RoadWidthMetres</c> AND IT WAS DOING TWO JOBS.</b> It was the width of the
    /// asphalt <em>and</em> the width of the ground a Street claims, and those were the same number
    /// only because nothing but asphalt was drawn on a Street. <see cref="Footways"/> put something
    /// else there, so they separated — and the split is what makes the row cost no ground: the
    /// carriageway gives up what the pavements take, and 8 m of street stays 8 m of street.
    /// </para>
    /// <para>
    /// ⚠ <b>It is what <see cref="YardMargin"/> and <see cref="Edge"/> meant all along.</b> Both
    /// asked <em>how far from the centre line is the road's edge</em>, which is this and never the
    /// carriageway. Leaving them on the carriageway would have moved a scatter margin and a cursor
    /// as a side effect of repainting a kerb.
    /// </para>
    /// <para>
    /// ⚠ <b>8 m is a Tile and a Tile is what the setback moves in.</b> <c>[lots] setback_tiles</c>
    /// is 2 on every shipped file and each side is drawn on <c>[0, 2]</c> Tiles, so a Building's
    /// front wall stands at <b>0, 4 or 8 m</b> from the centre line — and a half-street of 4 m puts
    /// the back of the pavement exactly on the middle rung. ***The street's edge and the setback's
    /// quantum are the same length on purpose.***
    /// </para>
    /// </remarks>
    private const float StreetWidthMetres = 8f;

    /// <summary>How wide a <b>Street's</b> asphalt is drawn — the right of way, less two pavements.</summary>
    /// <remarks>
    /// ⚠ <b>Derived rather than chosen</b>, so the two cannot drift apart and leave the street a
    /// different width than the ground it claims.
    /// </remarks>
    private const float CarriagewayWidthMetres =
        StreetWidthMetres - (2f * FootwayWidthMetres);

    /// <summary>
    /// How wide the whole <b>foot half</b> of one side of a Segment is — <b>kerb and pavement
    /// together</b>, which between them spend it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>INVENTED, and it is spent out of <see cref="StreetWidthMetres"/> rather than added to
    /// it.</b> See <see cref="Footways"/> for what is invented here and what is not: the width is,
    /// and <em>which Segments have one</em> is not. ***The budget is the rule this row keeps at every
    /// step***: the pavement was spent out of the street, and the kerb is spent out of the pavement.
    /// </remarks>
    private const float FootwayWidthMetres = 1.5f;

    /// <summary>How wide the <b>kerb</b> is — the stone band against the asphalt.</summary>
    /// <remarks>
    /// ⚠ <b>Its TOP is the pavement's top and not its own height</b>, so the kerb and the paving
    /// behind it are flush and the only step in the street is the one at the channel. See
    /// <see cref="Kerbs"/>.
    /// </remarks>
    private const float KerbWidthMetres = 0.4f;

    /// <summary>What is left of the foot half once the kerb has taken its band. <b>Derived.</b></summary>
    private const float PavementWidthMetres = FootwayWidthMetres - KerbWidthMetres;

    /// <summary>
    /// How far a <b>dropped</b> kerb bites into the carriageway beyond the kerb line.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE DROP HAS TO READ AS A SHAPE, BECAUSE ITS REAL CUE IS A HEIGHT NOBODY CAN SEE.</b> A
    /// dropped kerb is the same stone at the road's level, and the difference is <b>0.1 m</b> on a
    /// city drawn from hundreds of metres up — invisible, at every zoom this camera offers. Giving it
    /// a colour of its own would have been inventing a material to stand in for a height. **Giving it
    /// an apron across the channel is the thing a dropped kerb actually has**, and it is legible
    /// because it changes the *outline* of the kerb rather than its tone.
    /// </remarks>
    private const float KerbDropBiteMetres = 1.0f;

    /// <summary>How long one <b>dropped</b> kerb is drawn, along the Street.</summary>
    /// <remarks>
    /// ⚠ <b>One Tile, and that is the only thing recommending it.</b> A real drop is 1.2 m at a door
    /// and 3–4 m at a driveway, and the city holds neither — an Address is a point
    /// (<c>adr/0074</c>), so ***the length is pure invention where the POSITION is not***. Adjacent
    /// drops that would overlap are merged rather than drawn twice; see <see cref="Kerbs"/>.
    /// </remarks>
    private const float KerbDropLengthMetres = 4f;

    /// <summary>The same as <see cref="StreetWidthMetres"/>, for an <see cref="RoadKind.Arterial"/>.</summary>
    /// <remarks>
    /// ⚠ <b>INVENTED, like <see cref="StreetWidthMetres"/>, and the RATIO is the only part of it doing
    /// any work.</b> The city holds no width for any road: <c>[roads]</c> states a block size, an
    /// Arterial count, a junction spacing and two pedestrian keys, and not one metre of carriageway.
    /// What the city <em>does</em> hold is <see cref="RoadSegmentTable.Kind"/>, and free-flow and
    /// capacity are derived from it — an Arterial carries <b>3.3×</b> a Street's Vehicles a Day —
    /// so ***that these three differ is a fact and how much they differ is not***.
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>An Arterial spends none of it on a pavement</b>, and that is not a width decision —
    /// see <see cref="Footways"/>.
    /// </remarks>
    private const float ArterialWidthMetres = 14f;

    /// <inheritdoc cref="ArterialWidthMetres"/>
    /// <summary>The same, for a <see cref="RoadKind.FootPath"/> — a cut-through, and not a road.</summary>
    private const float FootPathWidthMetres = 2.5f;

    /// <summary>What a Street's carriageway is painted. <b>sRGB</b>; see <see cref="Paving"/>.</summary>
    private static readonly Color Carriageway = new(0.30f, 0.30f, 0.33f);

    /// <summary>An Arterial, darker than a Street because it carries more and is resurfaced less.</summary>
    /// <remarks>
    /// 🔴 ⚠ <b>THREE SHIPPED RULESETS LAY ONE AND THIS COMMENT SAID NONE DID.</b>
    /// <c>severance.toml</c>, <c>bordered.toml</c> and <c>crowded.toml</c> all state
    /// <c>arterial_count = 16</c>; the other 37 files state 0. Measured rather than read:
    /// <c>--roads</c> on <c>crowded.toml</c> reports <b>730</b> Arterial Segments of 530,914. So this
    /// colour is reachable, <see cref="Footways"/>'s refusal to draw a pavement beside one is
    /// reachable, and <c>plans/0049</c> <b>F51</b>'s refusal of a crossing rests on the same false
    /// premise — filed there.
    /// </remarks>
    private static readonly Color Arterial = new(0.26f, 0.26f, 0.29f);

    /// <summary>
    /// <b>Paving — what a FootPath is made of, and what a pavement beside a Street is made of.</b>
    /// It is not asphalt and must not read as a road.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>ONE colour for both, and that is the claim rather than a saving.</b> A cut-through and a
    /// kerbside pavement are the same surface and the same permission — <see cref="TravelMode.Foot"/>
    /// on a Segment — so the foot network is one colour on the map wherever it runs. ***What the eye
    /// picks out is where you may walk, which is a fact the city holds per Segment.***
    /// </remarks>
    private static readonly Color Flagstone = new(0.52f, 0.50f, 0.46f);

    /// <summary>
    /// <b>Kerbstone — the band against the channel, and the dropped one at an Address.</b> Paler and
    /// cooler than <see cref="Flagstone"/>, because a kerb is a different stone from the paving.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>ONE colour for the band AND the drop, deliberately.</b> A dropped kerb is the same stone
    /// laid lower — ***so tinting the drop would be inventing a material to say a height with***, and
    /// what says it instead is <see cref="KerbDropBiteMetres"/>'s apron. The two are told apart in a
    /// <c>draw</c> list by their height and their width, which is where a machine should be reading
    /// them from anyway.
    /// </remarks>
    private static readonly Color Kerbstone = new(0.62f, 0.62f, 0.60f);

    // 🔴 BuildingFillLow/High AND DepthFillLow/High STOOD HERE AND ARE NOW `[lots] setback_tiles`.
    // Four drawing constants -- 0.55-1.00 of the frontage and 0.45-0.85 of the depth -- deciding how
    // much of its parcel a Building covered. THE SIMULATION DID NOT AGREE WITH THEM: World.CreateBuilding
    // sealed the WHOLE parcel, so Sealing was about twice the ground this file put a wall on, and the
    // two quantities were the same quantity. ***A number the shell invents about the city is a number
    // the city cannot see.*** adr/0015 says a number a designer would want to change is Ruleset data;
    // these had escaped that by being called drawing constants.
    //
    // What replaced them is ONE key and it is a LENGTH, so coverage rises with the parcel instead of
    // being the same share of every plot -- which is what DepthFillLow's own remark said the old
    // arrangement got wrong. The shell reads LotTable.Footprint* and invents nothing.

    /// <summary>How far a pitched roof rises, as a share of the Building's depth.</summary>
    /// <remarks>
    /// ⚠ <b><see cref="DepthFillLow"/>' class of thing and labelled as one</b> — the city holds
    /// no roof. A silhouette is all that reads at a high-level camera, so this is the cheapest
    /// variety in the drawing and the one that costs the simulation nothing at all.
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>It is a share of the span the roof CROSSES, so the band is an ANGLE.</b> A gable rises
    /// over half its span, so a share <c>s</c> is <c>atan(2s)</c> off the horizontal and
    /// 0.18–0.45 is <b>20° to 42°</b> — a low pantile at one end and a steep slate at the other,
    /// which is the band English streets are actually built in. 🔴 <b>It was 0.30–0.75 and the
    /// band was never the reason every roof looked alike</b>: the roof was cut flush with its own
    /// wall, so there was no eave line for the eye to measure an angle against. See
    /// <see cref="EavesMetres"/> — <i>widening a band that nothing renders is not a change.</i>
    /// </remarks>
    private const float RoofRiseLow = 0.18f;

    /// <inheritdoc cref="RoofRiseLow"/>
    private const float RoofRiseHigh = 0.45f;

    /// <summary>The tallest a gable rises, whatever its span. <b>A cap and not a band.</b></summary>
    /// <remarks>
    /// 🔴 <b>A SHARE OF THE SPAN ALONE PUT A MOUNTAIN ON A DEEP BUILDING.</b> The share is an
    /// angle, and an angle held over a 36 m span is a 16 m roof — taller than the house under it.
    /// Real buildings do not solve this by flattening the pitch; they stop using one ridge and
    /// break the roof up, which is geometry this shell does not have. So the pitch is held and the
    /// rise is clamped, which reads as the shallow roof a deep building actually has.
    /// </remarks>
    private const float RoofRiseCeilingMetres = 7.5f;

    /// <summary>How far a roof oversails its wall. <b>What makes a pitch legible.</b></summary>
    /// <remarks>
    /// <see cref="DepthFillLow"/>' class of thing and labelled as one — the city holds no eaves.
    /// A real overhang is 300–600 mm; this is larger because the camera is fifty metres up and a
    /// half-metre line is a pixel. What it buys is a <b>shadow at the wall head</b>, which is the
    /// edge a roof's angle is actually read against.
    /// </remarks>
    private const float EavesMetres = 0.9f;

    /// <summary>The two roofing tones a Building draws between, in sRGB.</summary>
    /// <remarks>
    /// ⚠ <b>Two tones and a jitter, rather than one colour</b> — a terrace roofed in exactly one
    /// red reads as moulded plastic, and the variation costs a draw off the same scramble the
    /// massing already took. <b>Neither is a state</b>: an abandoned Building is painted
    /// <see cref="Derelict"/> over the top of whichever it drew, because a shell that kept a warm
    /// roof would be the liveliest thing on the street.
    /// </remarks>
    private static readonly Color[] Roofs =
    [
        new(0.55f, 0.33f, 0.26f),   // clay tile, and the shell's original
        new(0.36f, 0.34f, 0.36f),   // slate
    ];

    /// <summary>How much of a Cell's Woodland becomes a drawn tree, at most.</summary>
    /// <remarks>
    /// ⚠ <b>A drawing density and not the city's.</b> A Cell holds up to
    /// <see cref="CellGrid.TilesInCell"/> = 1,024 wooded Tiles and drawing one crown per Tile would
    /// be a quarter of a million trees around a small town. This is the count a <em>fully</em> wooded
    /// Cell gets, and the Cell's own share scales it.
    /// </remarks>
    private const int CrownsPerCell = 22;

    /// <summary>How far into a block a scattered thing must be to count as being in the yard.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT IS THE BLOCK'S OWN CARVE NOW AND NO LONGER A CONSTANT</b> (<c>plans/0052</c> stage
    /// 1). It was half a carriageway plus an invented 26 m at its jitter's top — the same band on a
    /// 32-Tile block and on a 256-Tile one, so a coarse city kept its trees in a ring that meant
    /// nothing. <b><see cref="BlockPatterns.StripTiles"/> is the depth the core actually carves</b>,
    /// called rather than restated, because a second copy of the city's arithmetic in the renderer
    /// is <c>plans/0012</c> <b>Cause 1</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>It is the DETACHED strip and not the deepest a parcel can be</b>, which is deliberate
    /// and is a floor rather than a guarantee: a back-to-back block's parcels meet at the centre
    /// line and have no yard at all, so a margin honest about <em>that</em> would suppress the
    /// courtyard on every block in the city. <b>What covers the denser patterns is the Woodland
    /// itself</b> — <see cref="MapLayers.Seal"/> takes a Cell's trees back as its ground is built
    /// on, and stage 1 seals the whole parcel, so a terraced block thins itself out and needs no
    /// margin to do it.
    /// </para>
    /// </remarks>
    private static float YardMargin(int block, int lots) =>
        (StreetWidthMetres * 0.5f) + (BlockPatterns.StripTiles(block, lots) * MetresPerTile) + 1f;

    /// <summary>Above this, a Building gets a flat roof. <b>Tall things are not gabled.</b></summary>
    private const float PitchCeilingMetres = 24f;

    /// <summary>
    /// How square a plan has to be before it gets a <b>hip</b> rather than a gable — the short side
    /// as a share of the long one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A ridge needs a long axis to run along, and a square plan does not have one.</b> Putting a
    /// gable on a square building is picking one of two identical directions and committing to it,
    /// which is why real square-planned buildings — a school, a market hall, a villa — are hipped
    /// or pavilioned instead. So this is not a taste about roofs; it is the question <em>is there
    /// an axis</em>, asked of a rectangle.
    /// </para>
    /// <para>
    /// ⚠ <b>IT IS A CHOSEN NUMBER AND NOT A DERIVED ONE</b>, and it is the only one this change
    /// adds. 0.8 says <i>within a fifth of square</i>. It is not welded to the <c>crossed</c>
    /// refusal's <c>1.2</c> one screen down, which asks a different question — that one asks whether
    /// turning a ridge would put it over too long a span, this one asks whether there is a ridge
    /// direction worth choosing at all. ***A constant welded to two decisions is governed by
    /// whichever of them is louder*** (<c>05 §4</c>), so they are two constants.
    /// </para>
    /// <para>
    /// ⚠ <b>No <c>plans/0002</c> §D ratifier is owed</b> and that is not an oversight: appearance is
    /// composed in the shell and never enters the <c>World</c> (<c>adr/0150</c>), so this number
    /// moves no State Hash and <c>adr/0052</c> does not reach it. It is refutable by looking:
    /// a city whose square buildings read as gabled sheds, or whose terraces have lost their ridges.
    /// </para>
    /// </remarks>
    private const float HipSquareness = 0.8f;

    /// <summary>
    /// The narrowest run of kerb worth standing a Building on, in metres.
    /// </summary>
    /// <remarks>
    /// <b>A terrace house is about 6 m wide</b>, so below this there is no Building to draw — the
    /// face has given its ground to the two faces that took the corners and has none left. ⚠ <b>It
    /// is a drawing floor and not a rule about the city</b>: the Lot is still there, still
    /// subdivided and still countable, and what the gap says is that the block was subdivided on
    /// four faces when it has room for two.
    /// </remarks>
    private const float MinFrontageMetres = 6f;

    /// <summary>How tall one storey is drawn. A drawing height, and no kind states one.</summary>
    private const float StoreyMetres = 3.5f;

    /// <summary>
    /// The tallest a Building is drawn before jitter — <b>what the setback is derived against.</b>
    /// </summary>
    /// <remarks>
    /// Three storeys is the height the shell was framed and lit for. It is not a cap: a Lot whose
    /// pattern gives it more is drawn taller, and <c>plans/0053</c>'s ladder runs well past three.
    /// ⚠ <b>This sentence named <c>[[building]] occupants = 3</c> until step 3 retired the key</b>;
    /// the framing did not move, so the number is kept and its false derivation is gone.
    /// </remarks>
    private const float BuildingHeightMetres = 3f * StoreyMetres;

    /// <summary>What a Building in use is drawn as. Warm stone, and the shell's original.</summary>
    /// <remarks>
    /// ⚠ <b>It is the CENTRE of a band and no longer the whole of it</b> — see
    /// <see cref="Rendered"/>. The value is unchanged, so a Building that draws the middle of the
    /// band is painted exactly what every Building was painted before.
    /// </remarks>
    private static readonly Color Standing = new(0.55f, 0.52f, 0.45f);

    /// <summary>How far a wall wanders off <see cref="Standing"/>, each way.</summary>
    /// <remarks>
    /// 🔴 <b>A TERRACE IN ONE COLOUR IS ONE EXTRUDED SLAB, whatever its silhouette does.</b> Five
    /// Buildings share a Segment (<c>adr/0078</c>) and stand shoulder to shoulder, so the only
    /// thing separating two of them in the picture was a shadow — and a shadow between two
    /// surfaces of identical albedo reads as a crease in one surface rather than as a gap between
    /// two. ⚠ <b>Narrow on purpose</b>: it must never approach <see cref="Derelict"/>, because
    /// abandonment is the one thing a Building's colour is allowed to MEAN. At ±10% of value a
    /// standing wall is 0.24–0.29 in linear and a shell is 0.033, which is the gap the wall
    /// shader's own ruin test sits in.
    /// </remarks>
    private const float RenderedSpread = 0.10f;

    /// <summary>
    /// What an <b>abandoned</b> Building is drawn as — <b>the state the picture could not show.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A ruin looked exactly like a house until this landed, and a flood is what made that
    /// unbearable.</b> In the frame at Tick 6,101 on <c>flooded.toml</c> the water is unmistakable
    /// and the <b>235 ruined Buildings standing in it are indistinguishable from the dry ones on the
    /// bank</b>. The readout said the number; the picture did not. ***That is the same shape as the
    /// hexadecimal <c>CLAUDE.md</c>'s Definition of done was amended over*** — a state the city knows
    /// and the eye cannot find.
    /// </para>
    /// <para>
    /// ⚠ <b>It is <see cref="BuildingTable.IsAbandoned"/> and not <em>flooded</em>, which is wider
    /// than the thing that prompted it and deliberately so.</b> A Building abandoned by
    /// <c>adr/0053</c>'s failure pressure and one ruined by a flood are the same state — <c>02
    /// §4.3</c>'s derelict — and the renderer has no business knowing which verb put it there. The
    /// visible consequence is that <c>declining.toml</c> now greys out as it decays, which nobody
    /// asked for and is the point: ***one colour, every mechanism that reaches the state.***
    /// </para>
    /// <para>
    /// ⚠ <b>Desaturated as well as darkened.</b> Darker alone reads as shadow at this sun angle, and
    /// the boxes are already lit from one side.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>Both colours are converted with <c>SrgbToLinear</c> at the write site, and the first
    /// spelling was not.</b> A MultiMesh instance colour is multiplied into albedo in <b>linear</b>
    /// space, so an sRGB value written straight through renders far brighter than it reads —
    /// <see cref="Standing"/>'s warm stone came out near white and the contrast this exists to
    /// create was most of the way washed out. ***The colours were right and the space they were
    /// written in was not***, which looks in a screenshot exactly like a badly chosen palette.
    /// </para>
    /// </remarks>
    private static readonly Color Derelict = new(0.20f, 0.19f, 0.18f);

    /// <summary>A pitched roof, warm against the wall so the silhouette has an edge.</summary>
    private static readonly Color Roofing = Roofs[0];

    /// <summary>What the five <see cref="TerrainKind"/>s look like, in sRGB.</summary>
    /// <remarks>
    /// ⚠ <b>Indexed by the enum's own value</b>, which <c>TerrainKind</c>'s remarks call a
    /// re-baseline to renumber — so this array is ordered by that numbering rather than by taste,
    /// and a sixth kind lands at the end or not at all.
    /// </remarks>
    private static readonly Color[] Land =
    [
        new(0.36f, 0.44f, 0.24f),   // Ordinary   — pasture
        new(0.47f, 0.45f, 0.41f),   // Rock       — bare stone
        new(0.44f, 0.47f, 0.27f),   // Floodplain — silt, greener than pasture
        new(0.27f, 0.35f, 0.28f),   // Marsh      — standing water in the grass
        new(0.55f, 0.52f, 0.34f),   // ThinSoil   — dry, sandy
    ];

    /// <summary>Woodland, which the land is blended toward by how much of a Cell is wooded.</summary>
    private static readonly Color Forest = new(0.16f, 0.29f, 0.15f);

    /// <summary>Water, in the ground's own skin. <b>The bed and not the surface</b>.</summary>
    private static readonly Color Shallows = new(0.13f, 0.26f, 0.36f);

    /// <summary>Ground a flood can reach, which is a tint on whatever is already there.</summary>
    /// <remarks>
    /// ⚠ <b>A TINT AND NOT A COLOUR, because a floodplain is a claim ABOUT ground rather than a kind
    /// of it</b> — a wooded floodplain and a rocky one must still read as wood and rock.
    /// </remarks>
    private static readonly Color Silt = new(0.38f, 0.33f, 0.24f);

    /// <inheritdoc cref="Silt"/>
    private const float SiltShare = 0.28f;

    /// <summary>A tree's crown, and an outbuilding's walls.</summary>
    private static readonly Color Crown = new(0.20f, 0.34f, 0.17f);

    /// <inheritdoc cref="Crown"/>
    private static readonly Color Outbuilding = new(0.42f, 0.39f, 0.34f);

    /// <summary>A boulder, on the ground that has them.</summary>
    private static readonly Color Boulder = new(0.44f, 0.42f, 0.40f);

    /// <summary>Which Map Layer the map is tinted by, or none of them.</summary>
    /// <remarks>
    /// 🔴 <b>AN OVERLAY IS A SECOND RENDER PATH AND NOT A MATERIAL</b>
    /// (<c>docs/07 §3.1</c>): the city drops to an unlit base and the layer owns all the colour.
    /// A tint over the ordinary picture would be read against a sunset, a roof colour and a wall
    /// band, none of which is data — ***and the one thing an instrument may not do is compete
    /// with the scene for the eye.***
    /// </remarks>
    private enum Wash
    {
        /// <summary>The city, drawn as a city.</summary>
        None,

        /// <summary>Air pollution, the one Layer a Rule may emit into.</summary>
        Pollution,

        /// <summary>Land value, the stored column that chases desirability.</summary>
        Value,

        /// <summary>Sealing — how much of a Cell is under paving and roof.</summary>
        Sealed,

        /// <summary>
        /// A Building's rung on <c>BlockPatterns.Ladder</c>. <b>A DEBUG VIEW AND NOT A SHIPPING
        /// ONE.</b>
        /// </summary>
        /// <remarks>
        /// ⚠ <b>Categorical, so it is the one wash the <see cref="Bands"/> reasoning does not
        /// govern.</b> Five patterns are five things and not five amounts, and a monotone ramp
        /// over them would invite exactly the reading — <em>Slab is more than Courtyard</em> —
        /// that the ladder does not support. It gets its own hues and the legend names them.
        /// ⚠ <b>Read off the BLOCK and not off the height, since <c>plans/0058</c>.</b> A rung names
        /// a plot ratio now and the storeys divide it by the ground the pattern claims, so height
        /// no longer identifies a rung at all — see <see cref="RungOf"/>.
        /// </remarks>
        Rung,

        /// <summary>
        /// How long a Building has stood, against the Tick now. <b>A DEBUG VIEW AND NOT A SHIPPING
        /// ONE.</b>
        /// </summary>
        /// <remarks>
        /// 🔴 <b>A GENERATED CITY IS ONE VINTAGE AND THIS VIEW IS HOW YOU SEE THAT.</b>
        /// <c>SyntheticCity</c> raises everything it lays inside one call at one Tick, so on a
        /// fresh world every Building is the same age and the whole city washes flat at the top of
        /// the ramp. That is a true reading of a real absence rather than a broken overlay — see
        /// <c>plans/0057</c>. It only says anything once a run has raised Buildings at different
        /// Ticks.
        /// </remarks>
        Age,
    }

    /// <summary>The five hues <see cref="Wash.Rung"/> is drawn with, sparsest to densest.</summary>
    /// <remarks>
    /// ⚠ <b>Hues and not a ramp, and that is the opposite of <see cref="Bands"/> on purpose.</b>
    /// The ladder orders patterns by how many addresses they fit, which makes rung an ordinal and
    /// not a quantity — <em>Slab is one place above Courtyard</em> is true and <em>Slab is 25%
    /// more than Courtyard</em> is not. A monotone ramp asserts the second. ⚠ <b>Six, and the
    /// count is <c>BlockPatterns.Count</c>'s</b>; a seventh pattern needs a seventh colour here and
    /// <see cref="RungOf"/> clamps rather than crashing until it gets one. ⚠ <b>The top one is
    /// near-white on purpose</b>: <see cref="BlockPattern.Tower"/> is the one form whose <em>height</em>
    /// is its identity, and a light colour is what reads on a thin shape against a dark ground.
    /// ⚠ <b>Named for the PATTERN and not the rung</b>, because this shell already spells
    /// <c>Rungs</c> as the speed ladder's names, and two <c>Color[]</c> a line apart under one word
    /// is how a wrong index gets written.
    /// </remarks>
    private static readonly Color[] Patterns =
    [
        new(0.42f, 0.62f, 0.35f),
        new(0.28f, 0.55f, 0.72f),
        new(0.88f, 0.74f, 0.28f),
        new(0.82f, 0.42f, 0.62f),
        new(0.90f, 0.34f, 0.24f),
        new(0.96f, 0.96f, 0.94f),
    ];

    /// <summary>
    /// The ramp an overlay is drawn with, dark to bright. <b>Perceptual and not a rainbow.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Monotone in lightness on purpose.</b> A hue-cycling ramp reads as categories rather
    /// than as a quantity — a person picks out the green band and the red band and stops seeing
    /// the gradient between them — and it is the standard way a map of one number is made to lie.
    /// This one only ever gets lighter and warmer, so a brighter Cell is a larger number at every
    /// point on it and nothing has to be looked up.
    /// </remarks>
    private static readonly Color[] Bands =
    [
        new(0.06f, 0.09f, 0.16f),
        new(0.13f, 0.20f, 0.36f),
        new(0.20f, 0.36f, 0.45f),
        new(0.31f, 0.52f, 0.42f),
        new(0.58f, 0.65f, 0.32f),
        new(0.85f, 0.66f, 0.24f),
        new(0.93f, 0.44f, 0.20f),
        new(0.94f, 0.24f, 0.26f),
    ];

    /// <summary>Ground with no Cell row at all, which is not the same as a Cell reading zero.</summary>
    /// <remarks>
    /// 🔴 <b>THE DISTINCTION IS THE WHOLE HONESTY OF THE PICTURE.</b> The only thing in the
    /// build that gives a Cell a row is a pollution emission, so on a world whose Rules emit
    /// nothing <em>every</em> Cell is unmeasured — and an overlay that painted those at the ramp's
    /// bottom would report a clean city where it should report <em>no reading</em>. ⚠ It is
    /// deliberately DARKER and greyer than <see cref="Bands"/>'s first entry, which is a measured
    /// zero, and the two must never be confused by eye.
    /// </remarks>
    private static readonly Color Unmeasured = new(0.045f, 0.045f, 0.05f);

    /// <summary>What the city is painted while an overlay is up.</summary>
    /// <remarks>
    /// 🔴 <b>DARKER THAN EVERY BAND OF THE RAMP, AND THE FIRST TRY WAS BRIGHTER THAN MOST OF
    /// THEM.</b> An overlay is read from above, where roofs cover most of the ground — so a base
    /// grey at 0.17 put the CITY at the top of the picture's contrast range and left the field
    /// showing through the gaps between buildings. ***The thing being measured came second to the
    /// thing it was measured over.*** The base is a silhouette: enough to say where the streets and
    /// the massing are, and never enough to compete with a reading.
    /// </remarks>
    private static readonly Color Muted = new(0.055f, 0.055f, 0.062f);

    /// <summary>A quarter turn about the vertical, for the gable whose ridge runs east-west.</summary>
    private static readonly Basis Quarter = Basis.FromEuler(new Vector3(0f, Mathf.Pi * 0.5f, 0f));

    /// <summary>An eighth turn, which is what squares up a four-sided <c>CylinderMesh</c>.</summary>
    /// <remarks>
    /// 🔴 <b>A FOUR-SEGMENT CYLINDER IS A DIAMOND AND NOT A SQUARE.</b> Godot places a
    /// <see cref="CylinderMesh"/>'s vertices at 0°, 90°, 180° and 270° on a circle, so at four
    /// segments the corners land on the axes and the FACES point diagonally — a roof turned 45° to
    /// the building under it. The eighth turn puts the faces back on the axes, and
    /// <see cref="Diagonal"/> is the scale correction that turn owes.
    /// </remarks>
    private static readonly Basis Eighth = Basis.FromEuler(new Vector3(0f, Mathf.Pi * 0.25f, 0f));

    /// <summary>
    /// What a turned four-sided cone's radius has to be scaled by to span a unit square.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The radius is a HALF-DIAGONAL and the span wanted is a SIDE.</b> Once <see cref="Eighth"/>
    /// has turned the faces onto the axes, a base of radius <c>r</c> is a square of side
    /// <c>r√2</c> — so a mesh authored at radius 0.5 spans 0.707 and every hipped roof would sit
    /// inside its own walls by three tenths of its width. ***A rotation that changes what a
    /// dimension MEANS owes a scale***, and this is it.
    /// </remarks>
    private static readonly float Diagonal = Mathf.Sqrt(2f);

    /// <summary>How near an edge the pointer has to be, in pixels, before the camera follows.</summary>
    /// <remarks>
    /// ⚠ <b>Pixels and not a share of the window</b>, because it is sized by how accurately a hand
    /// parks a cursor rather than by how big the screen is — a share would make the band enormous on
    /// the 1920-wide default and unusable on a small window.
    /// </remarks>
    private const float EdgeMarginPixels = 72f;

    /// <summary>How fast the edge scroll pans, in drag-pixels a second at full push.</summary>
    private const float EdgePixelsPerSecond = 900f;

    /// <summary>How thick the water is drawn. Flat, and floating just clear of the ground.</summary>
    private const float WaterMetres = 0.6f;

    /// <summary>A Cell's side in the shell's metres, which is what one water quad covers.</summary>
    private const float CellMetres = CellGrid.TilesPerCell * MetresPerTile;

    /// <summary>In-world seconds a Tick is worth, which is what turns a rung into a rate.</summary>
    private const double SecondsPerTick = 86_400.0 / Ticks.PerDay;

    /// <summary>
    /// The speed ladder, in Ticks a second. 🔴 <b>A PLAYTEST INSTRUMENT AND NOT A RATIFIED LADDER.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This disagrees with <c>01 §1</c> on purpose, and <c>01 §1</c> is still the design.</b>
    /// Every rung here is <b>half</b> that table's Ticks/s, so 1× is 8 rather than 16 and a Day is
    /// 4m16s rather than 2m08s. The tick rate is host-side and runtime-only (<c>CLAUDE.md</c> →
    /// Constants), so nothing here moves a hash or settles anything. ***It is a dial to play with,
    /// and what it is for is producing the playtest <c>adr/0094</c>'s revisit trigger asks for.***
    /// </para>
    /// <para>
    /// 🔴 <b>THE LADDER'S SLOWEST SHIPPED RUNG IS ~340× TOO FAST TO SEE A PERSON MOVE.</b> A Tick is
    /// 42.19 s of in-world time, so <c>01 §1</c>'s 1× runs the world at <b>675× real time</b>: a
    /// 20-minute commute is <b>1.8 real seconds</b> and a car crosses a 128 m block in <b>0.014 s</b>,
    /// under a frame at 60 Hz. Visual truthfulness is 1× <em>real</em> time — <b>0.0237 Ticks/s</b>.
    /// Filed against <c>01 §1</c> and <c>§7</c> in <c>plans/0012</c>.
    /// </para>
    /// <para>
    /// <b>1 Tick/s is the rung at which a walker is watchable</b> — a block in 2.2 s, a commute in
    /// 28 s, a Day in 34 minutes — and it is kept as <c>1/8×</c> because it is the one a person
    /// actually looked at and liked.
    /// </para>
    /// </remarks>
    private static readonly double[] Ladder = [0.0, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0, 24.0, 32.0];

    /// <summary>What each rung is called. <c>1×</c> is this shell's, at half <c>01 §1</c>'s rate.</summary>
    private static readonly string[] Rungs =
        ["paused", "1/16x", "1/8x", "1/4x", "0.5x", "1x", "2x", "3x", "4x"];

    /// <summary>The rung <c>space</c> returns to, and the one a fresh shell opens at.</summary>
    private const int DesignSpeed = 5;

    private Simulation _simulation = null!;
    private World _world = null!;
    private MultiMeshInstance3D _buildings = null!;
    private MultiMeshInstance3D _roofs = null!;
    private MultiMeshInstance3D _hips = null!;
    private MultiMeshInstance3D _mansards = null!;
    private MultiMeshInstance3D _yards = null!;
    private MultiMeshInstance3D _trees = null!;
    private MultiMeshInstance3D _rocks = null!;
    private MultiMeshInstance3D _travellers = null!;
    private MultiMeshInstance3D _cars = null!;
    private readonly List<ulong> _carIds = [];
    private MultiMeshInstance3D _roads = null!;
    private MultiMeshInstance3D _footways = null!;
    private MultiMeshInstance3D _kerbs = null!;
    private MultiMeshInstance3D _plots = null!;
    private PanelContainer _tuner = null!;

    private LineEdit[] _fields = [];

    private Label _tunerStatus = null!;

    /// <summary>The Ruleset's TEXT, kept because the tuner rewrites text and re-parses it.</summary>
    private string _toml = string.Empty;

    private int _citizens;

    /// <summary>
    /// Whether <c>--empty</c> was given, and so whether this shell declines to generate a city.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A FIELD RATHER THAN A LOCAL BECAUSE THE TUNER REBUILDS THE WORLD.</b> Retuning a
    /// Ruleset in the shell constructs a new <c>World</c> and re-issues the boot Commands, so a
    /// setting read once in <c>_Ready</c> and dropped would let one tune silently repopulate a
    /// world the player had been building by hand. ***A boot decision that a rebuild does not
    /// carry is a decision the second world does not have.***
    /// </remarks>
    private bool _empty;

    private ulong _seed;

    private MultiMeshInstance3D _cells = null!;

    /// <summary>The sun, kept because it follows the clock (<c>plans/0051</c> row 2).</summary>
    private DirectionalLight3D _light = null!;

    /// <summary>The sky, kept for the same reason: its colours are the hour's.</summary>
    private ProceduralSkyMaterial _above = null!;

    /// <summary>The environment, kept because ambient light is the hour's too.</summary>
    private Godot.Environment _air = null!;

    /// <summary>The ground the city actually stands on, in metres, from the last framing.</summary>
    private Rect2 _laid;

    /// <summary>
    /// The ground the Road Graph covers, in metres, from the last framing. <b>Larger than
    /// <see cref="_laid"/> and not by a knowable factor.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>It is measured off the Nodes rather than guessed from the city's size, and the guess it
    /// replaces was <c>_laid.Grow(_span * 0.5f)</c>.</b> That margin scaled with the city, so the
    /// larger a city grew the further it pushed the woodland away from itself — and at 20,000
    /// Citizens on <c>rulesets/pictured.toml</c> ***there was not one tree anywhere near the town***
    /// (<c>plans/0051</c> <b>F7</b>). A paved rectangle is a fact the Road Graph already holds, so
    /// asking it costs one walk of the Nodes on a framing and nothing at all per frame.
    /// </remarks>
    private Rect2 _paved;

    private MultiMeshInstance3D _ground = null!;
    private MeshInstance3D _land = null!;
    private ImageTexture _skin = null!;

    /// <summary>The overlay in force, the texture it is drawn into, and what it read.</summary>
    /// <remarks>
    /// ⚠ <b>A SECOND TEXTURE RATHER THAN A REWRITE OF <see cref="_skin"/>.</b> The skin is the
    /// terrain, the woodland, the floodplain and the sea folded together over a quarter of a
    /// million Cells; rebuilding it to leave an overlay would cost that walk on every switch, and
    /// swapping which texture the ground plane wears costs a field assignment.
    /// </remarks>
    private ImageTexture? _wash;

    /// <inheritdoc cref="_wash"/>
    private Wash _washing = Wash.None;

    /// <inheritdoc cref="_wash"/>
    private StandardMaterial3D? _instrument;

    /// <inheritdoc cref="_wash"/>
    private StandardMaterial3D? _muted;

    /// <inheritdoc cref="_wash"/>
    private StandardMaterial3D? _categorical;

    /// <summary>The ground's ordinary material, kept so an overlay can be taken off again.</summary>
    private Material _skinned = null!;

    /// <summary>The Tick the wash was last built from, and the reading that anchors its ramp.</summary>
    /// <remarks>
    /// 🔴 <b>THE PEAK IS PART OF THE PICTURE AND NOT A DETAIL OF IT.</b> A ramp normalised
    /// to whatever the largest Cell happens to hold is a picture that looks identical on a filthy
    /// city and a clean one — ***a relative scale hides the only thing the player wanted to
    /// know*** — so the number the top of the ramp stands for goes in the readout beside it. That
    /// is <c>01 §7</c>'s <em>an overlay must never be sharper than the simulation underneath it</em>
    /// spent on the axis rather than on the resolution.
    /// </remarks>
    private ulong _washedAt;

    /// <inheritdoc cref="_washedAt"/>
    private int _washPeak;

    /// <inheritdoc cref="_washedAt"/>
    private int _washCells;

    private MultiMeshInstance3D _water = null!;
    private MultiMeshInstance3D _flood = null!;
    private MultiMeshInstance3D _hazard = null!;
    private MultiMeshInstance3D _cursor = null!;

    /// <summary>Every human-readable name in the Ruleset. <b>The shell owns these, not the core.</b></summary>
    private RulesetNames _names = RulesetNames.None;

    /// <summary>What is under the cursor, stacked most specific first.</summary>
    private Label _hover = null!;

    /// <summary>Which verb the next click issues. <b><c>01 §2</c>'s, and never an instrument's.</b></summary>
    private Verb _verb = Verb.Look;

    /// <summary>Which declared <c>[[zone_rule]]</c> the <see cref="Verb.Zone"/> brush paints for.</summary>
    private int _zoneChoice;

    /// <summary>Which <c>serves</c> kind <see cref="Verb.Service"/> would raise.</summary>
    private byte _serviceKind;

    /// <summary>Whether the governing panel is open.</summary>
    private bool _governing;

    /// <summary>The governing panel — one row per declared <c>[[policy]]</c>.</summary>
    private PanelContainer _policyPanel = null!;

    /// <summary>The tool palette: what a click will do, and which one of it.</summary>
    /// <remarks>
    /// 🔴 <b><c>plans/0045</c> row 15g, raised by the player unprompted.</b> Five verbs and two
    /// cycling sub-selections were reported through <b>one line of text</b>, and a cycle is the least
    /// discoverable control there is — ***you cannot see what you are not holding***, so a Ruleset's
    /// second Zone Rule existed only for somebody who pressed <c>z</c> twice on the chance.
    /// </remarks>
    private PanelContainer _palette = null!;

    private HBoxContainer _tools = null!;
    private HBoxContainer _choices = null!;
    private Button _policiesButton = null!;
    private CanvasLayer _hud = null!;

    /// <summary>
    /// The one <see cref="Theme"/> every panel built out of Controls is given, so that
    /// <see cref="Retype"/> has a single place to state their size.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A Theme is inherited by a Control's whole subtree</b>, so this is set on the three
    /// roots and reaches every Button and Label under them. The alternative was a font-size
    /// override per Button, which is the same decision written once per widget and forgotten on
    /// the next widget.
    /// </remarks>
    private readonly Theme _type = new();

    /// <summary>The amount field for each Policy, by declaration position.</summary>
    private LineEdit[] _policyFields = [];

    /// <summary>What the panel says about the last governing act.</summary>
    private Label _policyStatus = null!;

    /// <summary>
    /// Commands raised this frame, drained into the next <c>Step</c>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A verb NEVER touches the world directly, and this list is what enforces it.</b>
    /// <c>Simulation.Apply</c> is the single door every state change goes through, so a shell that
    /// called <c>World</c> for a click would produce a city no replay reproduces and no State Hash
    /// divergence explains. ***That is the sentence <c>Populate</c> and <c>Arrive</c> each already
    /// carry in their own remark***, arriving on the player's side of it.
    /// </remarks>
    private readonly System.Collections.Generic.List<Command> _queued = [];

    /// <summary>A Street was laid or bulldozed, so the Road Graph has to be re-drawn.</summary>
    private bool _repave;

    /// <summary>
    /// <b>The session, as the file that reproduces it.</b> Every <see cref="Command"/> the shell
    /// issues is appended here at the Tick it applies.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>Populate</c> IS THE FIRST ENTRY, and that is the whole of why this is not a
    /// bolt-on.</b> The shell used to call <c>SyntheticCity.PopulateInto</c> directly, which put an
    /// entire city into the world without going through <see cref="Simulation.Apply"/> — ***state
    /// arriving by a door no log accounts for***, at Tick 0, before a player had touched anything.
    /// <c>Borough.Headless</c> has recorded the population as a Command since slice 6 for exactly
    /// this reason (<c>Session.Load</c>), and the shell simply did not.
    /// </remarks>
    private InputLogBuilder _log = null!;

    /// <summary>Why the last click did nothing, or empty. <b>Shown, never thrown.</b></summary>
    private string _refused = string.Empty;

    /// <summary>
    /// Where a driven run is pointing, or null when the mouse is. <b>A hand's motion clears it.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It PERSISTS after the click rather than being cleared with it</b>, so that the hover, the
    /// cursor marker and any <c>shoot</c> afterwards all describe the Tile the script named. A driven
    /// run has no mouse, and an aim that lasted one call would leave every picture of a driven click
    /// pointing at the corner of the map.
    /// </remarks>
    private (Tiles East, Tiles North)? _aimed;

    /// <summary>
    /// Where the last press landed, while the button is still down — <b>the first half of a drag,
    /// and null the rest of the time.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE PRESS STILL ACTS AND THIS CHANGES NOTHING ABOUT THAT.</b> Acting on release with a
    /// slop test is what made a trackpad click unusable in the first place (see
    /// <see cref="_UnhandledInput"/>'s own remark), so the release is a <em>reader</em> here: it can
    /// add a sentence and it can never lay, bulldoze or withhold a Street.
    /// </para>
    /// <para>
    /// ⚠ <b>Only <see cref="Verb.Connect"/> sets it</b>, because it is the only tool a drag means
    /// anything to. Zoning, demolishing and raising a service are all one plot, and a drag across
    /// them is a click.
    /// </para>
    /// </remarks>
    private (Tiles East, Tiles North)? _pressed;
    private Label _readout = null!;
    private VisibleAgent[] _agents = new VisibleAgent[8192];
    private double _owed;
    private int _rung = DesignSpeed;
    private int _resume = DesignSpeed;
    /// <summary>Frames drawn since the shell opened. Read only by the screenshot trigger.</summary>
    private int _frame;
    private string _rulesetPath = "rulesets/minimal.toml";

    /// <summary>The script this run is driven by, in Tick order. Empty when nobody is driving.</summary>
    private DriveCommand[] _drive = [];

    /// <summary>How far into <see cref="_drive"/> the run has got.</summary>
    private int _next;

    /// <summary>This frame's interpolation, kept so a written caption can be recomposed.</summary>
    private Ratio _alpha;

    /// <summary>Set once the run has been refused, so no frame draws a world nobody built.</summary>
    private bool _stopping;

    /// <summary>A picture asked for and not yet taken, because the frame it wants is not drawn.</summary>
    /// <remarks>
    /// 🔴 <b>THE PICTURE WAS A WHOLE FRAME BEHIND ITS OWN CAPTION, and <c>plans/0048</c> <b>F8</b>
    /// recorded that as fixed.</b> <c>Drive</c> runs AFTER <c>Draw</c> in a frame, so F8 had
    /// <see cref="Shoot"/> recompose before the shutter — which repaired the caption and could not
    /// repair the picture, because a <c>Control</c>'s new text is not submitted to the renderer until
    /// the engine draws the frame, and that happens after <c>_Process</c> returns.
    /// <c>RenderingServer.ForceDraw</c> does not bring it forward; two of them do not either.
    /// ***The half that was still stale was the half nobody could read a Tick off.***
    /// </remarks>
    private string? _pendingShot;

    /// <summary>Whether the run ends as soon as that picture is taken.</summary>
    /// <remarks>
    /// ⚠ <b><c>GetTree().Quit()</c> is deferred to the end of the frame, so there is no next frame
    /// after a <c>quit</c>.</b> A <c>shoot</c> and a <c>quit</c> on one Tick would therefore lose the
    /// picture outright — so the quit waits for the shutter rather than the shutter racing the quit.
    /// </remarks>
    private bool _quitAfterShot;

    /// <summary>Which Building each drawn instance is, in instance order. See <see cref="DrawList"/>.</summary>
    private readonly List<ulong> _buildingIds = [];

    private readonly List<ulong> _roofIds = [];
    private readonly List<ulong> _hipIds = [];
    private readonly List<ulong> _mansardIds = [];

    private readonly List<ulong> _yardIds = [];

    /// <summary>Which Citizen each drawn Traveller is, in instance order.</summary>
    private readonly List<ulong> _travellerIds = [];

    /// <summary>Which Segment each road instance is, so the <c>draw</c> list can be joined to the
    /// Road Graph. <b>Monotonic row ids, never slots.</b></summary>
    private readonly List<ulong> _roadIds = [];

    /// <summary>
    /// Which Segment each pavement instance is. ⚠ <b>Every id appears TWICE</b> — a Segment gets a
    /// strip on each side — so a reader counting distinct Segments here must say so.
    /// </summary>
    private readonly List<ulong> _footwayIds = [];

    /// <summary>
    /// Which <b>Segment</b> each kerb instance runs along — <b>bands and drops alike</b>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A DROP IS AN ADDRESS AND THIS COLUMN NAMES ITS SEGMENT INSTEAD</b>, which is a real loss
    /// and is recorded rather than papered over: the Lot is what a reader would want to join on, and
    /// one list cannot carry two kinds of id. ⚠ **Recover the Lot by matching the drop's centre to
    /// `LotTable.FrontageOffset` on the Segment named here** — one join rather than none, and see
    /// <see cref="Kerbs"/> for why merging makes even that approximate where two Addresses are closer
    /// together than <see cref="KerbDropLengthMetres"/>.
    /// </remarks>
    private readonly List<ulong> _kerbIds = [];

    /// <summary>Which vacant Lot each drawn pad is, in instance order.</summary>
    private readonly List<ulong> _plotIds = [];

    /// <summary>Lines that have arrived over the socket and not yet been applied.</summary>
    private readonly System.Collections.Concurrent.BlockingCollection<string> _asked = [];

    /// <summary>Replies waiting for the socket thread to send. One per line taken.</summary>
    private readonly System.Collections.Concurrent.BlockingCollection<string> _answered = [];

    /// <summary>The listening socket, or null when nobody is driving live.</summary>
    private System.Net.Sockets.Socket? _listener;

    /// <summary>Where every applied command is spelled back out, or null when nothing records.</summary>
    private string? _record;

    /// <summary>The socket's path, kept so it can be unlinked on the way out.</summary>
    private string? _door;
    private Camera3D _camera = null!;
    private float _span = 512f;

    /// <summary>The ground point the eye orbits. A drag moves this and nothing else does.</summary>
    private Vector3 _focus;

    /// <summary>How far the eye stands off the focus, bounded by <see cref="Dolly"/>.</summary>
    private float _distance = 512f;

    /// <summary>Which corner the city is seen from, clockwise from due south in radians.</summary>
    private float _yaw = YawStepRadians;

    /// <summary>
    /// How far the eye is tipped above the ground. <b>Opens at the isometric angle and moves.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>It was a <c>const</c> until <c>plans/0051</c> row 3.</b> Every picture this shell had
    /// ever produced was taken from the same 35.26°, which is a drawing convention rather than a
    /// camera — and a city seen only from one angle is a diagram of itself. ***What a low eye buys
    /// is the SKYLINE***: a roof line against the sky is the one thing a plan view cannot show, and
    /// it is what a person photographs.
    /// </remarks>
    private float _pitch = PitchRadians;

    /// <summary>The lens, kept because its focus plane rides the orbit's standoff.</summary>
    private CameraAttributesPractical _lens = null!;

    /// <summary>Whether the lens is on. <b>Read by <see cref="Orbit"/> and nothing else.</b></summary>
    private bool _photographing;

    /// <summary>The Buildings' shader, kept because the hour is a uniform on it.</summary>
    private ShaderMaterial? _paint;

    public override void _Ready()
    {
        (_rulesetPath, int citizens, ulong startAt, bool govern, _empty, string? drive,
            ulong quitAt, string? listen, string? record) = Arguments();

        if (!Driven(drive, quitAt))
        {
            Stop(2);

            return;
        }

        if (record is not null)
        {
            _record = Globalize(record);

            File.WriteAllText(_record, string.Empty);
        }

        if (listen is not null && !Listen(Globalize(listen)))
        {
            Stop(2);

            return;
        }

        string path = Globalize(_rulesetPath);

        if (!File.Exists(path))
        {
            GD.PrintErr($"no Ruleset at {path}. Pass one after Godot's own --, as in:");
            GD.PrintErr("  godot --path src/Borough.Godot -- --ruleset rulesets/congested.toml "
                + "--citizens 4000");
            Stop(2);

            return;
        }

        _toml = File.ReadAllText(path);

        RulesetLoadResult loaded = RulesetLoader.Parse(_toml, Path.GetFileName(path));

        if (loaded.Ruleset is null)
        {
            GD.PrintErr(loaded.Describe());
            Stop(2);

            return;
        }

        _names = loaded.Names;
        _citizens = citizens;
        _seed = 0;

        var key = WorldKey.FromSeed(_seed);

        _world = new World(citizens, loaded.Ruleset, key);
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };

        _log = new InputLogBuilder(
            _seed,
            new WorldConfiguration(citizens),
            RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml)));

        // 🔴 THE CITY ARRIVES THROUGH Simulation.Apply AND NOT BESIDE IT. This was
        // SyntheticCity.PopulateInto called on the world directly, which is thousands of rows
        // entering by a door the log does not account for -- and every hand-played session would
        // therefore have replayed against an EMPTY world and diverged at Tick 0, with nothing in
        // the file to explain it. Borough.Headless has recorded the population as a Command since
        // slice 6 (Session.Load); the shell just did not.
        //
        // ⚠ IT COSTS ONE TICK, AND THAT IS THE HEADLESS RUNNER'S BEHAVIOUR RATHER THAN A CHARGE.
        // A Command applies at the top of a Tick, so a world populated by one is populated during
        // Tick 0 and the readout opens at Tick 1. A shell that opened at Tick 0 with a city in it
        // would be one Tick ahead of the runner for the whole session.
        //
        // 🔴 AND IT IS BEHIND --empty, BECAUSE UNTIL IT WAS THERE WAS NO PATH TO A WORLD A PLAYER
        // BUILT. adr/0090 gives the generator terrain, Woodland, hazard and the Outside Connections
        // with their stubs -- "nothing else" -- and gives the player every road; 00-vision pillar 3
        // makes `connects` a player verb. Populate is the OTHER thing: Command.cs calls it a verb no
        // player has and expects it to be deleted. It ran here unconditionally, so every screenshot,
        // every balance run and every judgement anyone has made about how this city reads was taken
        // on a synthetic lattice the design says will not exist, and CommandKind.Connect has never
        // been WATCHED working.
        //
        // ⚠ THE DEFAULT IS THE WRONG WAY ROUND AND STAYS THAT WAY UNTIL SOMEBODY HAS PLAYED THE
        // OTHER ONE. Flipping it re-points every drive script, every recipe in .claude/skills/drive
        // and every reading that assumes a city is standing at Tick 1 -- so the opt-in is the honest
        // first move and the flip is a separate decision with evidence behind it.
        // ***A default nobody has looked past is not a default anybody chose.***
        //
        // ⚠ WHAT --empty IS FOR IS A READING RATHER THAN A PICTURE. plans/0002 §D2: a generated
        // city's paved extent scales with the population it serves, so the same number sizes both
        // the demand and the supply, and v/c peaks at 0.44 at 4,000, 16,000 and 64,000 Citizens
        // alike -- which is why [traffic]'s three hash-bearing numbers named a ratifier, ran it, and
        // could not fire. The question to put to a hand-built city is whether v/c ever exceeds it.
        // ***A hand-drawn network can be WRONG, and a generated one structurally cannot be.***
        //
        // ⚠ AND --empty IS A DIFFERENT VERB RATHER THAN NO VERB. It used to skip the Command
        // outright, which gave a world with no ground in it at all -- measured on flooded.toml at
        // 500 Citizens: hazard 0, water 0, tree 0, where the populated world reports 11,063, 5,058
        // and 3,512. That is not adr/0090's world either; that ADR gives the generator terrain,
        // Woodland, hazard and the Outside Connections. CommandKind.Ground is the ground half on
        // its own, so BOTH branches lay ground and only one lays a city -- and both spend exactly
        // one Tick, which is why the fast-forward below counts from 1 either way.
        var boot = new Command(
            _empty ? CommandKind.Ground : CommandKind.Populate, default, default);

        _log.Append(Ticks.Zero, boot);
        _simulation.Step(new TickInput([boot], 0));

        // FAST-FORWARD BEFORE THE FIRST FRAME, and it is not a rung. The ladder is what a person
        // watches at; this is how they get to the part worth watching. A flood on flooded.toml
        // begins at Tick 4,096, which at the top rung is two and a half minutes of staring at a dry
        // city -- and on a machine with no screen it is the difference between a photograph of a
        // flood and a photograph of the coast.
        //
        // ⚠ IT STEPS THE SIMULATION AND SKIPS NOTHING. Every Tick runs, which is why it is slow and
        // why it is correct: a world jumped to is a different world (adr/0003), and the whole point
        // of a shell is to look at the one the headless runner would produce.
        // FROM ONE, because the Populate Command above already ran Tick 0. A loop from zero would
        // put the shell one Tick past the runner on every --start-at, which is the class of
        // off-by-one a State Hash comparison finds and a photograph never does.
        //
        // ⚠ AND FROM ONE UNDER --empty TOO, WHICH IT WAS NOT WHEN THAT FLAG SKIPPED THE COMMAND.
        // A boot that issues no verb steps no Tick, so the loop had to start from 0 and the start
        // index was a conditional. CommandKind.Ground made it a constant again: both branches issue
        // exactly one Command at Tick 0, so both have spent one Tick by the time this runs.
        // ***A branch that changes WHICH verb boots costs nothing here; one that changes WHETHER a
        // verb boots costs an off-by-one at every --start-at.***
        for (ulong tick = 1; tick < startAt; tick++)
        {
            _simulation.Step(default);
        }

        // THE SEA FIRST AND THE FLOOD ON TOP OF IT, and the order is the draw order. A Hazard
        // Region Cell is dry ground that a flood reaches, so the two never cover the same Cell --
        // 🔴 THE GROUND IS PAINTED FIRST AND IT IS NOT DECORATION. Without it dry land is the
        // BACKGROUND -- the viewport's clear colour -- so a block's interior reads as a hole rather
        // than as land, the only thing on screen with a surface is the carriageway, and an 8-metre
        // Street beside a 128-metre block looks like the widest thing in the city. ***Two separate
        // readings of this shell as broken traced back to the same absence***: a flood covering the
        // frame read as the camera drifting out to sea, and a correctly-scaled Street read as huge.
        _ground = Layer(new Color(0.16f, 0.17f, 0.14f), new Vector3(1f, 1f, 1f), casts: false);

        // 01 §5.3'S POSTED PRICE, and it is drawn UNDER everything the city puts on top of it --
        // under the roads at a tenth of a metre and under the sea at WaterMetres. That is the whole
        // point of the height: the risk is a property of the GROUND, so a Street laid across a
        // floodplain must read as a Street on a floodplain and not as a floodplain interrupted.
        _hazard = Layer(new Color(0.34f, 0.25f, 0.17f), new Vector3(1f, 1f, 1f), casts: false);

        // WHAT THE CURSOR IS OVER, and it is a CELL rather than a Tile on purpose. A Tile is 4 m
        // and vanishes at any zoom that shows a neighbourhood; a Cell is 128 m and is the box the
        // pick actually resolves Buildings against (BuildingResidency.In). ***A marker that is not
        // the thing the query used is a marker that lies at the edges.***
        _cursor = Layer(new Color(0.95f, 0.80f, 0.25f), new Vector3(1f, 1f, 1f), casts: false);

        // but they sit at almost the same height, and painting the standing water last is what makes
        // a rising tide read as arriving rather than as flickering.
        _water = Layer(new Color(0.10f, 0.22f, 0.42f), new Vector3(1f, 1f, 1f), casts: false);
        _flood = Layer(new Color(0.20f, 0.48f, 0.78f), new Vector3(1f, 1f, 1f), casts: false);
        _roads = Layer(Carriageway, new Vector3(1f, 0.1f, 1f), perInstance: true, casts: false);

        // THE PAVEMENT STANDS 0.1 m PROUD OF THE ASPHALT, and that step is the kerb. The roads box
        // above is 0.1 m tall and its instances are drawn at y = 0, so a Street's roof is at 0.05 m
        // (Edge()'s remark measured it); this box is 0.3 m tall, so a pavement's roof is at 0.15 m.
        //
        // ⚠ A HEIGHT MEASURED AGAINST THE OTHER MESH AND NOT AGAINST A SCALE. That is Edge()'s own
        // finding one level up -- a height derived from a scale rather than from the mesh it scales
        // is a number about the wrong object -- and it applies here for the same reason: 0.1 in the
        // roads layer is a MESH size and 1.0 in Paving()'s basis is a SCALE on it.
        //
        // ⚠ THE STEP AT THE CHANNEL IS THE KERB'S FACE AND THE BAND IS `_kerbs` BELOW. This layer
        // is the paving BEHIND the kerb, which is why it is PavementWidthMetres and not the whole
        // foot half: the kerb took its band out of it, at the same top, so the two are flush.
        _footways = Layer(Flagstone, new Vector3(1f, 0.3f, 1f), perInstance: true, casts: false,

            // FOUR A SEGMENT, which is a bound and not a count: two strips, plus a cap across the
            // head at each end that is a DEAD END. It was twice the road layer's while two was the
            // whole story, and the ceiling has to hold the bound rather than the ordinary case --
            // on bordered.toml the roads layer is ALREADY at its ceiling with 535,817 Segments, and
            // a pavement layer that stopped at half of what the asphalt covered would be the
            // failure §5 names: a layer at capacity has silently dropped what did not fit, and the
            // picture shows a smaller city with nothing to say so.
            instances: 4 * LayerInstances);

        // THE KERB, AND IT IS ONE MESH FOR TWO THINGS AT TWO HEIGHTS. A band is drawn with its top
        // flush with the pavement -- 0.15 m, so the only step in the street is at the channel -- and
        // a DROP is drawn with its top at 0.07 m, two centimetres over the asphalt. The instance
        // transform carries the difference, which is why one 0.3 m box serves both: a drop is scaled
        // to 0.07/0.15 of it and sits lower.
        //
        // ⚠ TWO CENTIMETRES AND NOT ZERO, because a drop flush with the carriageway is two coplanar
        // faces and the depth test picks between them per pixel. ***A dropped kerb that z-fights
        // reads as a rendering fault and not as a kerb.***
        _kerbs = Layer(Kerbstone, new Vector3(1f, 0.3f, 1f), perInstance: true, casts: false,

            // FOUR TIMES, and it is a bound rather than a count: two band runs a Segment before any
            // Address breaks them, plus one run and one drop for every Address that does, plus a
            // cap across each end that is a dead end. On pictured.toml that is ~800 for 194 Streets
            // and 208 Lots.
            instances: 4 * LayerInstances);

        // 🔴 A VACANT LOT DREW NOTHING AT ALL, so Zone -- which creates Lots and never a Building --
        // had NO visible result on any world. Buildings() walks the Building table, and a Lot with
        // nothing on it is not in it. ***The subdivision is the thing the verb does***, and until
        // this layer existed the only way to see one was to wait for the simulation to build on it,
        // which on a world with an empty Unplaced Pool never happens.
        _plots = Layer(new Color(0.42f, 0.52f, 0.30f), Vector3.One, perInstance: true, casts: false);

        // OFF by default. It is an instrument rather than scenery -- it answers "how big is a Cell
        // against this Building", and a person who has not asked that question does not want a
        // 128-metre lattice drawn over their city.
        _cells = Layer(new Color(0.55f, 0.45f, 0.25f), new Vector3(1f, 0.1f, 1f), casts: false);
        _cells.Visible = false;

        // A UNIT BOX, with the size composed per instance rather than baked into the mesh, which is
        // what lets one draw call hold Buildings of different shapes.
        //
        // ⚠ THE OPENINGS ARE PAINTED ON AND NOT MODELLED, which is the only way 65,536 of them fit
        // in one draw call. buildings.gdshader recovers the box's metres from the instance
        // transform, so a Building drawn taller gets more storeys without anybody counting them --
        // and it reads two things the CITY knows out of the fourth per-instance channel: which
        // kerb the Lot steps to, which is where the front door goes (adr/0074), and how much of
        // the kind's declared room is occupied, which is how many openings are shuttered.
        _buildings = Layer(
            Standing, new BoxMesh { Size = Vector3.One }, true, "res://buildings.gdshader");
        _paint!.SetShaderParameter("storey_metres", StoreyMetres);

        // A SECOND MESH AND NOT A SECOND BOX. A roof is the only part of a Building that is not a
        // cuboid, and a silhouette is what reads at the camera this game is played at -- so the
        // gable is a PrismMesh off the shelf (adr/0018) rather than geometry anybody wrote.
        //
        // 🔴 THREE MESHES NOW, AND THE COUNT IS THE POINT. `docs/07 §1.4` prices this exactly --
        // ***an appearance family costs a mesh; a building costs a transform*** -- so the whole of
        // the roofline's variety is three draw calls whatever the city's size. A fourth family
        // would be a fourth call and no more; a per-Building roof would be a mesh per Building and
        // is what that pillar exists to refuse.
        //
        // ⚠ ALL THREE ARE OFF THE SHELF (adr/0018) AND THE LAST TWO ARE CONES. Godot ships no
        // pyramid, and a CylinderMesh at four segments IS one: `TopRadius = 0` closes it to a
        // point for the hip, and a top radius that is not zero truncates it into the mansard. So
        // the two new families cost one parameter between them rather than any authored geometry.
        //
        // ⚠ A HIP ON A RECTANGLE IS A PAVILION AND NOT A TRUE HIP, and the difference is stated
        // rather than hidden: a real hip keeps a short ridge where the two half-hips meet, and a
        // stretched cone comes to a point instead. It is PlotDepthMetres' class of thing -- geometry
        // the city does not hold, invented so the picture reads -- and at fifty metres up what
        // reads is that the roof has four slopes rather than two.
        _roofs = Layer(Roofing, new PrismMesh { Size = Vector3.One }, perInstance: true);

        _hips = Layer(
            Roofing,
            new CylinderMesh
            {
                TopRadius = 0f,
                BottomRadius = 0.5f,
                Height = 1f,
                RadialSegments = 4,
                Rings = 0,
            },
            perInstance: true);

        // ⚠ THE TOP RADIUS IS HALF THE BOTTOM AND THAT IS THE ONLY DIFFERENCE FROM THE HIP. A
        // mansard's upper slope is shallow to the point of being flat from above, so what has to
        // read is the BREAK -- the line where the steep lower pitch stops. Half the span is where
        // that line falls on a real one.
        _mansards = Layer(
            Roofing,
            new CylinderMesh
            {
                TopRadius = 0.25f,
                BottomRadius = 0.5f,
                Height = 1f,
                RadialSegments = 4,
                Rings = 0,
            },
            perInstance: true);

        // THE COURTYARD. A block's middle cannot hold a Lot (adr/0078) and these are not Lots: an
        // outbuilding belongs to the Building in front of it and is drawn from its scramble, which
        // is DepthFillLow's own bargain -- geometry the city does not have, invented so the
        // picture reads as a city, and labelled so nobody promotes it to a Ruleset key.
        _yards = Layer(Outbuilding, Vector3.One, perInstance: true);
        _trees = Layer(Colors.White, TreeMesh(), perInstance: true);
        _rocks = Layer(Boulder, new SphereMesh { Radius = 0.5f, Height = 1f, RadialSegments = 7, Rings = 3 });
        _travellers = Layer(Colors.White, WalkerMesh(), perInstance: true);
        _cars = Layer(Colors.White, CarMesh(), perInstance: true);
        DressSurfaces();

        Ground();
        Hazard();
        Surface();
        Scatter();
        Flood();
        Pave();
        Sun();
        Look();
        Cells();
        Readout();

        // ⚠ AFTER Readout(), which is what builds the panel. Opening it from the command line is the
        // same bargain BOROUGH_SHOT strikes -- a machine with no hands cannot press `p`, and a panel
        // nobody can photograph is a panel nobody reviews.
        if (govern)
        {
            _governing = true;
            _policyPanel.Visible = true;
            ShowPolicies();
        }
    }

    public override void _Process(double delta)
    {
        // FIRST, AND ABOVE THE STOPPING CHECK. The frame this picture wants has now been drawn, and
        // a run ending on the same Tick it photographed must still get its photograph.
        if (_pendingShot is not null)
        {
            Capture(_pendingShot);
            _pendingShot = null;

            if (_quitAfterShot)
            {
                Quit();

                return;
            }
        }

        if (_stopping)
        {
            return;
        }

        _owed += delta * Ladder[_rung];

        // 🔴 A QUEUED COMMAND BUYS ONE TICK, EVEN PAUSED, and that is a decision rather than a slip.
        // A Command applies at the top of a Tick because Simulation.Apply is the single door, so a
        // verb pressed at rung 0 would otherwise sit and look broken until somebody started the
        // clock. ***Every reference builder lets you edit while paused***, and the cost is that
        // acting is the one input that moves a paused world -- by exactly one Tick, and the readout
        // says which Tick it is.
        if (_queued.Count > 0 && _owed < 1.0)
        {
            _owed = 1.0;
        }

        // 🔴 THE CLOCK IS CLAMPED AT THE NEXT COMMAND'S TICK, AND THAT IS WHAT MAKES A DRIVEN RUN
        // REPRODUCIBLE. A frame steps as many Ticks as the rung and the frame time between them
        // ask for, so a command drained on "the first frame at or past Tick T" lands on a
        // different Tick on a different machine, at a different rung, or under a different load --
        // which is plans/0048 F4, the defect tier 1 could not fix. Stepping no further than T
        // makes the Tick a command lands on a property of the SCRIPT rather than of the host.
        ulong until = _next < _drive.Length ? _drive[_next].At : ulong.MaxValue;

        while (_owed >= 1.0 && _world.Tick.Raw < until)
        {
            _simulation.Step(Ordered());
            _owed -= 1.0;
        }

        // ⚠ CLAMPED, because holding the clock at a command's Tick lets the debt run past a whole
        // Tick where the loop above used to guarantee it could not. An alpha over 1 would place a
        // Traveller past the Address it is walking to.
        _alpha = new Ratio((int)(Math.Min(_owed, 0.999_99) * 65_536));

        // AFTER the loop and not inside it, because a frame that steps many Ticks may lay many
        // Streets and the Road Graph only has to be correct once a frame. Pave() is O(Segments) and
        // bordered.toml has 535,817 of them, so this is a walk worth doing on the frames that
        // earned it rather than on all of them.
        if (_repave)
        {
            Pave();
            _repave = false;
        }

        // A MACHINE WITH NO HANDS HAS ITS CURSOR AT (0,0), which is the sky or a corner of the map,
        // so every photograph would show the pick refusing rather than the pick working. Warping to
        // the middle of the viewport is BOROUGH_SHOT's own bargain -- the shot exists because there
        // is nobody to press the key -- and it is confined to the same env var, so a person's cursor
        // is never moved out from under them.
        if (System.Environment.GetEnvironmentVariable("BOROUGH_SHOT") is not null)
        {
            Input.WarpMouse(GetViewport().GetVisibleRect().Size * 0.5f);
        }

        Edge(delta);

        Daylight();
        Draw(_alpha);
        Drive();
        Answer();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button is { Pressed: true, ButtonIndex: MouseButton.WheelUp })
            {
                Dolly(1f);

                return;
            }

            if (button is { Pressed: true, ButtonIndex: MouseButton.WheelDown })
            {
                Dolly(-1f);

                return;
            }

            // 🔴 ON PRESS, AND THE LEFT BUTTON DOES NOTHING ELSE. It used to act on RELEASE, with a
            // four-pixel slop test to tell a click from a drag, because panning was bound to the same
            // button -- and on a macOS trackpad that combination ate the click and panned instead.
            // Tap-to-click and force-click both emit motion while the button is down, so the pointer
            // drifts past four pixels before the release arrives: _mayAct was cleared, Act never ran,
            // and the same drift moved the camera. ***A conditional binding on one button is what
            // made the input feel unreliable***, so the condition is gone rather than widened -- a
            // larger slop radius would have been the same defect with a longer fuse.
            if (button is { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                // ⚠ THROUGH Apply, so --record spells the click out as the Tile it resolved to.
                // A recording holding a screen position would be a recording of a camera, and a
                // camera is not an input (adr/0007). _aimed is null here, so Aim() casts the ray.
                if (Aim() is { } at)
                {
                    Apply(new DriveCommand(
                        _world.Tick.Raw,
                        DriveVerb.Click,
                        button.ShiftPressed ? 1 : 0,
                        null,
                        at.East.Raw,
                        at.North.Raw));
                }
                else
                {
                    _refused = "that is not on the map.";
                }
            }

            // The other half of a drag, and it is a READER: it says what the gesture asked the
            // lattice for and never acts. plans/0045 row 23 -- there are no diagonal Streets, and
            // until this branch existed a drag laid one Segment near where it started and reported
            // nothing at all. ⚠ Nothing is applied unless Dragging finds something worth a
            // sentence, so an ordinary click records exactly what it always did.
            if (button is { Pressed: false, ButtonIndex: MouseButton.Left })
            {
                if (Dragging(Aim()) is { } upTo)
                {
                    Apply(new DriveCommand(
                        _world.Tick.Raw,
                        DriveVerb.Release,
                        0,
                        null,
                        upTo.East.Raw,
                        upTo.North.Raw));
                }

                _pressed = null;
            }

            return;
        }

        // A trackpad reaches none of that. macOS turns a two-finger scroll into a pan gesture and a
        // pinch into a magnify gesture, so the wheel branch above never sees a finger at all.
        //
        // 🔴 IT PANS, AND IT USED TO ZOOM. A two-finger scroll was wired to Dolly while the pinch
        // gesture below was ALSO wired to Dolly -- so a trackpad had two ways to zoom and no way to
        // move, which is the native mapping of both gestures exactly inverted. ***A gesture named
        // PanGesture doing something other than panning is a mapping nobody can learn***, and for a
        // trackpad this alone is the whole camera.
        if (@event is InputEventPanGesture scroll)
        {
            // Negated on both axes: the platform reports where the CONTENT went, and the camera is
            // the thing that moves here, so following the sign would send the city the wrong way.
            Pan(-scroll.Delta * 12f);

            return;
        }

        if (@event is InputEventMagnifyGesture pinch)
        {
            // Factor is a ratio about 1, and a pinch arrives as many small ones rather than one big.
            Dolly((pinch.Factor - 1f) * 8f);

            return;
        }

        // A hand that moves the mouse is pointing again, and takes the aim back off the script.
        if (@event is InputEventMouseMotion)
        {
            _aimed = null;
        }

        // 🔴 RIGHT OR MIDDLE, NEVER LEFT. Panning shared the left button with every verb until
        // 2026-08-31, which is what made a trackpad click unusable -- see the button branch above.
        // A two-finger click-drag is the native pan on a Mac trackpad and middle-drag is the mouse's,
        // so the two masks here are one gesture on two devices rather than a preference.
        if (@event is InputEventMouseMotion drag
            && (drag.ButtonMask & (MouseButtonMask.Right | MouseButtonMask.Middle)) != 0)
        {
            Pan(drag.Relative);

            return;
        }

        if (@event is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        // The keys with no DriveVerb behind them, which is what puts them ahead of the command
        // switch rather than in it. The tuner's two are an editing UI rather than a view of the
        // city, and a script has no panel to open.
        //
        // 🔴 THE FOUR VERB KEYS USED TO BE AMONG THEM, ON THE GROUND THAT HOLDING A TOOL MOVES
        // NOTHING IN THE WORLD. That was true and it made a recorded session UNREPLAYABLE: a click
        // means whatever is held, so a session recording the click and not the choice replays as a
        // different verb. ***What a recording needs is not everything that changed the city, it is
        // everything a replay has to know*** -- and the tool is the second half of every click.
        switch (key.Keycode)
        {
            case Key.Tab:
                _tuner.Visible = !_tuner.Visible;

                return;

            case Key.Enter:
            case Key.KpEnter:
                // Only while the panel is open, so the key is free everywhere else.
                if (_tuner.Visible)
                {
                    Regenerate();
                }

                return;

            case Key.V:
                Apply(Held("look", 0));

                return;

            case Key.Z:
                // ⚠ PRESSING IT AGAIN CYCLES THE ZONE rather than doing nothing, which is what makes
                // a Ruleset with two [[zone_rule]]s reachable without a second key. A zone word is a
                // BITMASK of which rules may build (ZoneRuleDefinition.Admits), so what the brush
                // paints is one rule's permission and never a category the city knows about.
                Apply(Held(
                    "zone",
                    _verb == Verb.Zone && _world.Rules.ZoneRules.Length > 0
                        ? (_zoneChoice + 1) % _world.Rules.ZoneRules.Length
                        : 0));

                return;

            case Key.X:
                Apply(Held("street", 0));

                return;

            case Key.B:
                Apply(Held("demolish", 0));

                return;

            case Key.S:
                // ⚠ CYCLES ON REPEAT, exactly as Z does, because a Ruleset may declare more than one
                // `serves` kind and there is no second key to spend on choosing between them.
                Apply(Held(
                    "service", NextService(_verb == Verb.Service ? _serviceKind : (byte)0)));

                return;

            case Key.P:
                // THE GOVERNING PANEL, and it is deliberately NOT the tuner. The tuner regenerates a
                // world from Ruleset text -- world-creation, a NEW city -- while this sets a declared
                // Policy's amount on the city that is running, through a Command, at a Tick, in the
                // order a replay reproduces. ***One panel edits the world's premises and the other
                // plays the game***, and putting them on one key would blur exactly that line.
                Govern();

                return;

            case Key.W:
                Record();

                return;
        }

        // 🔴 A KEY TOGGLES AND A COMMAND IS ABSOLUTE, SO THE CURRENT STATE IS READ HERE AND NEVER
        // IN Apply. g asks for the opposite of what the carriageway is doing; space asks to resume
        // if the clock is stopped. That reading is the whole difference between the two surfaces,
        // and keeping it on this side is what leaves exactly one applier to go wrong.
        //
        // ⚠ The Tick is the world's rather than zero, and it is unused today. It is what tier 4
        // needs: a socket that stamps each arriving command with the Tick it landed on writes a
        // drive script, which is what makes an interactive session replayable as a batch one.
        DriveCommand? command = key.Keycode switch
        {
            Key.Q => Made(DriveVerb.Turn, -1),
            Key.E => Made(DriveVerb.Turn, 1),

            // ⚠ THE KEYBOARD STEPS AND THE VERB IS STILL ABSOLUTE, which is this file's rule for
            // every toggle: read what is held, work out where it goes, and ask for that. So a
            // recorded session replays the ANGLE and not the keypress, and a step size changed
            // later cannot re-aim somebody's old script.
            Key.R => Made(DriveVerb.Tilt, Tipped(5)),
            Key.F => Made(DriveVerb.Tilt, Tipped(-5)),
            Key.L => Made(DriveVerb.Lens, _photographing ? 0 : 1),
            Key.G => Made(DriveVerb.Roads, _roads.Visible ? 0 : 1),
            Key.C => Made(DriveVerb.Cells, _cells.Visible ? 0 : 1),

            // ⚠ THE KEY CYCLES AND THE VERB IS ABSOLUTE, which is this file's rule for every
            // toggle. `o` asks for the next overlay by NAME, so a recorded session replays the
            // layer somebody was looking at and not the number of times they pressed a key.
            Key.O => new DriveCommand(_world.Tick.Raw, DriveVerb.Overlay, 0, Next()),
            Key.Equal or Key.KpAdd => Made(DriveVerb.Zoom, 4),
            Key.Minus or Key.KpSubtract => Made(DriveVerb.Zoom, -4),
            Key.Space => Made(_rung == 0 ? DriveVerb.Resume : DriveVerb.Pause),
            Key.Bracketleft => Made(DriveVerb.Speed, Math.Max(1, _rung - 1)),
            Key.Bracketright => Made(DriveVerb.Speed, Math.Min(Ladder.Length - 1, _rung + 1)),
            Key.Key1 => Made(DriveVerb.Speed, DesignSpeed),
            Key.Key2 => Made(DriveVerb.Speed, DesignSpeed + 1),
            Key.Key3 => Made(DriveVerb.Speed, DesignSpeed + 2),
            Key.Key4 => Made(DriveVerb.Speed, DesignSpeed + 3),
            Key.Escape => Made(DriveVerb.Quit),
            _ => null,
        };

        if (command is not null)
        {
            Apply(command.Value);
        }

        DriveCommand Made(DriveVerb verb, int amount = 0) =>
            new(_world.Tick.Raw, verb, amount, null);

        string Next() => _washing switch
        {
            Wash.None => "pollution",
            Wash.Pollution => "value",
            Wash.Value => "sealing",
            Wash.Sealed => "rung",
            Wash.Rung => "age",
            _ => "off",
        };
    }

    /// <summary>
    /// <c>--ruleset PATH</c>, <c>--citizens N</c>, <c>--start-at TICK</c>, <c>--govern</c>,
    /// <c>--empty</c>, <c>--drive PATH</c>, <c>--quit-at TICK</c>, <c>--listen PATH</c> and
    /// <c>--record PATH</c>, after Godot's <c>--</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A shell reads the command line and the core does not.</b> Every string here is this
    /// project's (<c>adr/0002</c>), and a bad one is reported rather than defaulted, because a
    /// silently-substituted world is a picture of somewhere else.
    /// </remarks>
    private static (string Ruleset, int Citizens, ulong StartAt, bool Govern, bool Empty,
        string? Drive, ulong QuitAt, string? Listen, string? Record) Arguments()
    {
        string ruleset = "rulesets/minimal.toml";
        int citizens = 1_000;

        // 🔴 THE SHELL OPENS AT 08:00 AND THE SIMULATION'S DAY STILL BEGINS AT 05:00, AND KEEPING
        // THOSE TWO APART IS THE WHOLE OF THIS DEFAULT.
        //
        // Ticks.DayBeginsAtHour is 5, so Tick 0 is 05:00 and a run opened at Tick 0 opened BEFORE
        // SUNRISE -- a photograph of the dark, taken by a shell working perfectly. That was found
        // driving plans/0049 row 8: the first three frames came back black and nothing anywhere
        // said why.
        //
        // ⚠ THE OBVIOUS FIX IS TO MOVE DayBeginsAtHour AND IT IS REFUSED. That constant carries
        // TWO jobs -- the Day's PHASE, which is hash-bearing, and what a fresh run opens on, which
        // is free -- and its own remark states the constraint the first one is under: it must sit
        // before the earliest Shift a Ruleset may declare, so that no commute is cut by the Day
        // boundary it belongs to. `shift_start_earliest_hour` is 6 in 42 of the 45 entries across
        // the shipped files, so 5 is nearly at that ceiling already and 8 is well past it: every
        // 06:00 Shift would wrap to the LATE end of its Day. ***So the phase may not move and the
        // opening view may, and this is the half that may.***
        //
        // ⚠ It steps the world 256 Ticks at boot rather than jumping. --start-at skips nothing,
        // and a world jumped to is a different world.
        ulong startAt = (ulong)Ticks.AtClock(8);
        string? drive = null;
        ulong quitAt = 0;
        string? listen = null;
        string? record = null;
        string[] given = OS.GetCmdlineUserArgs();

        // ⚠ A FLAG AND NOT A PAIR, so it is read over the whole array rather than inside the loop
        // below, which stops one short to read a value after each name.
        bool govern = Array.IndexOf(given, "--govern") >= 0;

        // 🔴 THE ONE ARGUMENT THAT CHANGES WHAT THE SHELL IS A PICTURE OF. Everything else here
        // sizes or times a generated city; --empty declines to generate one. See the Populate call
        // in _Ready for why the DEFAULT is the wrong way round and stays that way for now.
        bool empty = Array.IndexOf(given, "--empty") >= 0;

        for (int at = 0; at + 1 < given.Length; at++)
        {
            if (given[at] == "--ruleset")
            {
                ruleset = given[at + 1];
            }
            else if (given[at] == "--citizens"
                && int.TryParse(given[at + 1], out int asked) && asked > 0)
            {
                citizens = asked;
            }
            else if (given[at] == "--start-at"
                && ulong.TryParse(given[at + 1], out ulong from))
            {
                startAt = from;
            }
            else if (given[at] == "--drive")
            {
                drive = given[at + 1];
            }
            else if (given[at] == "--quit-at"
                && ulong.TryParse(given[at + 1], out ulong until))
            {
                quitAt = until;
            }
            else if (given[at] == "--listen")
            {
                listen = given[at + 1];
            }
            else if (given[at] == "--record")
            {
                record = given[at + 1];
            }
        }

        return (ruleset, citizens, startAt, govern, empty, drive, quitAt, listen, record);
    }

    /// <summary>
    /// Refuse to run, <b>and stop this frame as well as the next one</b>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>GetTree().Quit()</c> IS DEFERRED TO THE END OF THE FRAME, so <c>_Process</c> still
    /// runs once against a world <c>_Ready</c> never built</b> — and the refusal a person is meant
    /// to read scrolls past under a <c>NullReferenceException</c> and forty lines of Godot
    /// backtrace. ⚠ <b>This was already true of both Ruleset refusals</b>; the drive script only
    /// made it happen often enough to notice. ***A message printed above a stack trace is a message
    /// nobody reads***, which is <c>adr/0015</c>'s standard for a refusal failing at the last step.
    /// </remarks>
    private void Stop(int code)
    {
        _stopping = true;

        GetTree().Quit(code);
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        // The socket file outlives the process, so leaving it behind makes the NEXT run fail to
        // bind and report an address in use -- a message about this run that reads as one about that
        // one.
        _listener?.Dispose();
        _listener = null;

        if (_door is not null)
        {
            File.Delete(_door);
        }
    }

    /// <summary>A path as the operator typed it, resolved against the repository root.</summary>
    /// <remarks>
    /// ⚠ <b>The shell's working directory is the Godot project and the operator's is the
    /// repository</b>, which is why <c>--ruleset rulesets/minimal.toml</c> works at all. A written
    /// file resolves the same way, so a script and its output land where they were asked for.
    /// </remarks>
    private static string Globalize(string path) =>
        Path.IsPathRooted(path) ? path : ProjectSettings.GlobalizePath($"res://../../{path}");

    /// <summary>Reads the world into the two meshes that change, and writes the readout.</summary>
    private void Draw(Ratio alpha)
    {
        int drawn = Massings(Buildings());
        int vacant = Fill(_plots, Plots(), _plotIds);
        int moving = VisibleAgents.In(_world, CellRect.World, alpha, _agents);
        int under = Fill(_flood, Anonymous(Inundated()));

        TravellerHeadings();
        Fill(_travellers, Travellers(moving, false), _travellerIds);
        Fill(_cars, Travellers(moving, true), _carIds);
        Cursor();

        ulong tick = _world.Tick.Raw;

        // 🔴 THE OVERLAY IS REBUILT ON THE LAYER'S OWN CADENCE AND NOT PER FRAME. A Map Layer moves
        // when its diffusion pass runs -- every 64 Ticks for pollution and 256 for land value, the
        // designer's numbers rather than the profiler's (adr/0044) -- so a texture rebuilt every
        // frame would re-upload an unchanged image sixty times a second. ***The instrument reads
        // the same schedule the city does***, which is also what stops the picture claiming a
        // resolution the simulation underneath does not have (01 §7).
        //
        // ⚠ It is the layer's period and not a constant here, so a Ruleset that slows its
        // diffusion slows the picture with it rather than the two drifting apart.
        if (_washing != Wash.None && tick - _washedAt >= (ulong)Math.Max(1, Cadence()))
        {
            Rewash();
        }

        // ⚠ THE MINUTE COMES FROM Ticks AND IS NO LONGER DERIVED HERE. A Day begins at 05:00, so
        // `ofDay * 24 / PerDay` is off by five hours -- and this was one of four copies of that
        // expression, which is exactly why it now lives in one place.
        int minute = Ticks.MinuteOfDay(tick);

        _readout.Text =
            $"{System.IO.Path.GetFileName(_rulesetPath)}   Tick {tick:N0}   "
            + $"Day {tick / (ulong)Ticks.PerDay}   "
            + $"{minute / 60:00}:{minute % 60:00}\n"
            + $"Citizens {_world.Citizens.Rows.LiveCount:N0}   Buildings {drawn:N0}   "
            + $"vacant Lots {vacant:N0}   "
            + $"travelling {moving:N0}{Weather(under)}\n"
            + $"speed {Pace(_rung)}   "
            + $"mode {Holding()}   "
            + "[ ] speed, space pause, w write log, g roads, c cells, tab tune\n"
            + "click acts   right-drag or two-finger scroll pans   edge of screen pans   "
            + "pinch or -/= zooms   q/e turns   r/f tilts   l lens   o overlay"
            + Legend()
            + (_refused.Length > 0 ? $"\nREFUSED — {_refused}" : string.Empty);

        _hover.Text = Pointing();
    }

    /// <summary>
    /// What a rung is called, <b>how long it makes a Day</b>, and its multiple of real time.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>THE DAY'S LENGTH IS HERE BECAUSE THE MULTIPLE OF REAL TIME WAS MISLEADING ON ITS OWN.</b>
    /// <c>1×</c> is 8 Ticks/s and reads as <em>338× real time</em>, which is correct and invites a
    /// person to expect tomorrow shortly; a Day is 2048 Ticks and therefore <b>4m16s</b>, and the
    /// reassuring number is the one that was on screen. It was read as the <c>Day</c> counter being
    /// stuck. ***A speed is a rate and a person waiting is holding a duration***, which is
    /// <c>adr/0059</c>'s *state the duration, derive the rate* arriving in a readout — and
    /// <c>adr/0094</c>'s reason for a Day being a sampling rate rather than a length of life.
    /// </remarks>
    private static string Pace(int rung)
    {
        if (Ladder[rung] <= 0.0)
        {
            return Rungs[rung];
        }

        int seconds = (int)Math.Round(Ticks.PerDay / Ladder[rung]);
        string day = seconds < 60 ? $"{seconds}s" : $"{seconds / 60}m{seconds % 60:00}s";

        return $"{Rungs[rung]} — a Day in {day}, {Ladder[rung] * SecondsPerTick:N0}x real time";
    }

    /// <summary>One MultiMesh of boxes, coloured, sized, and ready to be filled per frame.</summary>
    /// <remarks>
    /// ⚠ <b><paramref name="perInstance"/> makes the layer's colour a property of each box rather
    /// than of the layer</b>, which needs three things set together: the mesh's albedo goes to white
    /// so the instance colour is not tinted by it, the material reads the instance colour as albedo,
    /// and the MultiMesh is told to carry one. ⚠ <b><c>UseColors</c> is set BEFORE
    /// <c>InstanceCount</c></b> — Godot allocates the instance buffer on the count, and a format
    /// changed afterwards is a resize the engine declines to do.
    /// </remarks>
    private MultiMeshInstance3D Layer(
        Color colour, Vector3 size, bool perInstance = false, bool casts = true,
        int instances = LayerInstances) =>
        Layer(colour, new BoxMesh { Size = size }, perInstance, casts: casts, instances: instances);

    /// <summary>
    /// How many instances a layer holds unless it says otherwise — <b>a ceiling and not a count</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A layer at its ceiling has dropped whatever did not fit and the picture cannot say
    /// so</b>, which is why <c>draw</c> prints this beside the instance count
    /// (<c>plans/0048</c> §5). It is <b>reached</b> on <c>bordered.toml</c>, whose 535,817 Segments
    /// are more than eight times it.
    /// </remarks>
    private const int LayerInstances = 65_536;

    /// <inheritdoc cref="Layer(Color, Vector3, bool)"/>
    /// <summary>The same, for a layer whose instances are not boxes.</summary>
    /// <remarks>
    /// ⚠ <b><paramref name="painted"/> is a SHADER and not a colour, and it brings the fourth
    /// per-instance channel with it.</b> A <c>MultiMesh</c>'s custom data costs nothing until
    /// something declares it, and nothing but the Buildings has anything to put there — so it is
    /// switched on by the same argument that switches on the shader rather than by a flag of its
    /// own, and a layer drawn by the stock material cannot be handed data no material will read.
    /// </remarks>
    /// <param name="casts">
    /// 🔴 <b>Whether the layer is in the SHADOW PASS, and a flat one must not be.</b> Everything
    /// lying on the ground — the ground, the water, the flood, the Hazard Region, the
    /// carriageway, a vacant Lot's pad, the Cell lattice, the cursor — is a slab a fraction of a
    /// metre thick, casting a shadow onto the surface it is already covering. It is invisible and
    /// it is not free: a cascaded directional shadow rasterises every caster once per split, and
    /// on <c>bordered.toml</c> that is <b>535,817</b> Segments drawn four extra times a frame to
    /// produce nothing. ⚠ <b>It is stated per layer rather than inferred from the mesh</b>,
    /// because the flat ones are unit boxes squashed by their instance transforms and the mesh
    /// does not know.
    /// </param>
    /// <param name="instances">
    /// The layer's ceiling. Defaults to <see cref="LayerInstances"/>; raised only by a layer that
    /// draws more than one instance per thing, which today is the pavements.
    /// </param>
    private MultiMeshInstance3D Layer(
        Color colour,
        Mesh mesh,
        bool perInstance = false,
        string? painted = null,
        bool casts = true,
        int instances = LayerInstances)
    {
        Material material;

        if (painted is null)
        {
            material = new StandardMaterial3D
            {
                AlbedoColor = perInstance ? Colors.White : colour,
                VertexColorUseAsAlbedo = perInstance,
                Roughness = 0.9f,
            };
        }
        else
        {
            // Kept because the hour is a uniform on it. It is one layer -- the Buildings -- and a
            // second painted layer would need a list rather than a field; there is no second one,
            // and inventing the list before there is would be a guess about what it holds.
            _paint = new ShaderMaterial { Shader = GD.Load<Shader>(painted) };
            material = _paint;
        }

        if (mesh is PrimitiveMesh primitive)
        {
            primitive.Material = material;
        }
        else if (mesh is ArrayMesh assembled)
        {
            assembled.SurfaceSetMaterial(0, material);
        }

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = perInstance,
            UseCustomData = painted is not null,
        };

        multi.Mesh = mesh;
        multi.InstanceCount = instances;
        multi.VisibleInstanceCount = 0;

        var node = new MultiMeshInstance3D { Multimesh = multi };

        if (!casts)
        {
            node.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }

        AddChild(node);

        return node;
    }

    /// <summary>End the run, <b>writing the session first where <c>BOROUGH_LOG</c> asked</b>.</summary>
    /// <remarks>
    /// ⚠ <b>On <c>BOROUGH_SHOT</c>'s own bargain, and for a stronger reason than the photograph
    /// has.</b> A machine with no hands cannot press <c>w</c>, so without this the round trip — play,
    /// write, replay, compare — could only ever be run by a person, and ***a verification nobody can
    /// run in a script is one nobody runs twice***.
    /// <para>
    /// ⚠ <b>It hangs on the end of the run rather than beside the shutter</b>, which is where it was
    /// written: every ending arrives here, since <c>--quit-at</c> is a <c>quit</c> command on the end
    /// of the script (<see cref="Driven"/>) and <c>esc</c> is the same command from a keyboard. A
    /// driven run that never takes a picture still writes its log.
    /// </para>
    /// </remarks>
    private int Quit()
    {
        // ⚠ THE SHUTTER FIRST. A shoot on this Tick has not been taken yet -- the frame it wants is
        // drawn after this method returns -- and GetTree().Quit() is deferred to the end of the
        // frame, so there would be no next _Process to take it in.
        if (_pendingShot is not null)
        {
            _quitAfterShot = true;

            return _rung;
        }

        if (System.Environment.GetEnvironmentVariable("BOROUGH_LOG") is not null)
        {
            Record();
        }

        GetTree().Quit();

        return _rung;
    }

}

/// <summary>
/// What a click does. <b><c>01 §2</c>'s player verbs, and deliberately not the instruments.</b>
/// </summary>
/// <remarks>
/// <c>Populate</c>, <c>Trip</c> and <c>Arrive</c> are <see cref="CommandKind"/> members and are
/// <em>not</em> here: each says in its own remark that it is <b>an instrument rather than one of
/// <c>01 §2</c>'s five</b> and that it is expected to be deleted. ***A verb list assembled from the
/// enum would have handed the player three doors the design intends to close.***
/// </remarks>
internal enum Verb : byte
{
    /// <summary>Read the city and change nothing. The default, and where every mode returns to.</summary>
    Look = 0,

    /// <summary>
    /// Subdivide the block under the cursor into Lots admitting one <c>[[zone_rule]]</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>SUBDIVIDE and not paint, however the verb is named in <c>01 §2</c>.</b>
    /// <c>LotSubdivider.Face</c> returns zero on frontage <c>World.Frontage</c> has already claimed,
    /// so this works on virgin faces and can never repaint a block that has Lots.
    /// <c>PlayerVerbTests.Zoning_ground_that_is_already_subdivided_changes_nothing</c> holds it.
    /// </remarks>
    Zone = 1,

    /// <summary>Lay or bulldoze one Street on the lattice edge leaving the nearest intersection.</summary>
    Connect = 2,

    /// <summary>Clear abandoned stock, and only that.</summary>
    Demolish = 3,

    /// <summary>
    /// Raise one service Building on the vacant Lot under the cursor.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>01 §5</c>'s ONE placement exception, and the only verb here that puts a Building on
    /// the ground.</b> Every other kind arrives through a Zone Rule filling in a permission set the
    /// player painted; <c>Simulation.ApplyService</c> refuses a kind declaring no <c>serves</c> by
    /// name, which is what stops the exception becoming a general <em>place anything</em> verb.
    /// </remarks>
    Service = 4,
}
